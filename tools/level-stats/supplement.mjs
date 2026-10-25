// Adds to tools/level-stats/out/levels/*.json the winning route found by search played on the live timeline twice:
// patiently (each tap after all work has settled: the model's reference rhythm, the same player for every level) and
// as fast as possible (each tap the moment it can be made): whether the outcome depends on tempo.
// measure.mjs does the same since; this patches files measured before.
// node tools/level-stats/supplement.mjs [--part i/n]
import fs from 'node:fs';import path from 'node:path';
import {allRecords,root} from './levels.mjs';import {engine} from './engines.mjs';import {run} from './trace.mjs';import {fromTrace} from './metrics.mjs';import {solver} from './solve.mjs';
const arg=k=>{const i=process.argv.indexOf(k);return i>0?process.argv[i+1]:null;};const dir=path.join(root,'tools/level-stats/out/levels');
let recs=allRecords().filter(r=>r.model);if(arg('--part')){const [i,n]=arg('--part').split('/').map(Number);recs=recs.filter((r,k)=>k%n===i);}
for(const r of recs){const f=path.join(dir,r.key+'.json'),res=JSON.parse(fs.readFileSync(f,'utf8'));if(!res.checks)continue;
 const E=await engine(r.game),S=solver(E,60000),cap=r.model.capacity||5,route=S.route(r.model,cap);if(!route.route)continue;
 const cmds=route.route.map(a=>({t:0,action:a}));
 const pat=run(E,r.model,{commands:cmds,patient:true}),fast=run(E,r.model,{commands:cmds});
 res.checks.routeLive=fromTrace(pat);res.checks.routeLive.kind='winning route, each tap after the work settles';
 const fm=fromTrace(fast);res.checks.routeFast={status:fast.status,end:fm.end,taps:fm.capsules.taps,routeTaps:route.route.length,cleared:Math.round(100*fm.clearedCells/fast.cells),zeroFreeShare:fm.buffer?.zeroFreeShare??null,meanFree:fm.buffer?.meanFree??null};
 fs.writeFileSync(f,JSON.stringify(res));process.stderr.write(`${r.key} patient ${pat.status} fast ${fast.status}\n`);}

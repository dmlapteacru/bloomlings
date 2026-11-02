// Timing and output snapshot of the measures of measure.mjs on a few levels (the owner, 2026-11-02: generation is too
// slow; tools/rounds/perf/README.md). Writes every measure's result and its time, so a run before and a run after an
// optimisation can be compared result by result (`--compare <old.json>`): the times may differ, the results never.
//   node tools/rounds/perf/bench.mjs --levels 20,27,50,26 [--bank P0025-Hlinks+hidden-21] [--only randomTest,calm]
//        [--out out.json] [--compare old.json]
// Levels come from content/rounds/levels/L####.json (definition), bank candidates from content/rounds/bank/*.json.
import fs from 'node:fs';import path from 'node:path';import {fileURLToPath} from 'node:url';
import * as M from '../measure.mjs';

const HERE=path.dirname(fileURLToPath(import.meta.url)),ROOT=path.resolve(HERE,'../../..');
const ROUNDS=process.env.ROUNDS_DIR?path.resolve(process.env.ROUNDS_DIR):path.join(ROOT,'content/rounds');
const arg=k=>{const i=process.argv.indexOf(k);return i>=0?process.argv[i+1]:null;};
const levels=(arg('--levels')||'20,27,50,26').split(',').filter(Boolean).map(Number);
const bankIds=(arg('--bank')||'').split(',').filter(Boolean);
const only=arg('--only')?new Set(arg('--only').split(',')):null;

const items=[];
for(const n of levels){const L=JSON.parse(fs.readFileSync(path.join(ROUNDS,'levels',`L${String(n).padStart(4,'0')}.json`),'utf8'));
 items.push({name:`L${n}`,level:L.definition,cls:L.cls,route:L.solution&&L.solution.route,bridge:L.random&&L.random.bridge!=null?L.random.bridge:null});}
if(bankIds.length){for(const f of fs.readdirSync(path.join(ROUNDS,'bank'))){const b=JSON.parse(fs.readFileSync(path.join(ROUNDS,'bank',f),'utf8'));
 for(const c of b.candidates||[])if(bankIds.includes(c.id))items.push({name:c.id,level:c.level,cls:c.cls,route:c.solution&&c.solution.route,bridge:c.random&&c.random.bridge!=null?c.random.bridge:null});}}

const out={node:process.version,date:new Date().toISOString(),items:[]};
const time=async(rec,name,f)=>{if(only&&!only.has(name))return;const t0=performance.now();const r=await f();const ms=Math.round(performance.now()-t0);rec.ms[name]=ms;rec.results[name]=r;
 console.log(rec.name.padEnd(28),name.padEnd(14),String(ms).padStart(7),'ms');};
for(const it of items){const {level}=it,rec={name:it.name,cells:level.rowsBottomFirst.join('').replace(/[.#]/g,'').length,ms:{},results:{}};
 const opt=it.bridge===null?{}:{bridge:it.bridge};
 await time(rec,'quickShare',()=>M.quickShare(level,40,11,it.bridge));
 await time(rec,'randomTest',()=>M.randomTest(level,{...opt,full:true}));
 await time(rec,'randomEarly',()=>M.randomTest(level,{...opt,early:true}));
 await time(rec,'calm',()=>M.calmTest(level,opt));
 await time(rec,'forced',()=>M.forcedShare(level));
 await time(rec,'greedy',()=>M.greedy(level));
 await time(rec,'opening',()=>M.openingTest(level,it.cls==='Normal'?'Hard':it.cls));
 await time(rec,'strategies',()=>M.strategies(level));
 await time(rec,'pressure',()=>M.pressure(level,30));
 await time(rec,'solve',async()=>{const s=await M.solve(level);return {win:s.win,route:s.route,nodes:s.nodes,replay:s.replay,fast:s.fast,decisions:s.decisions,rhythm:s.trace?M.rhythm(s.trace,rec.cells):null};});
 if(level.hero){await time(rec,'bridgeWin',()=>M.bridgeChoicesWin(level));
  await time(rec,'bridgeChoices',()=>M.bridgeChoices(level,60,5,rec.results.bridgeWin?rec.results.bridgeWin.each:null));
  await time(rec,'bridgeSingle',()=>M.bridgeSingle(level,60,3,rec.results.bridgeWin?rec.results.bridgeWin.each:null));
  await time(rec,'bridgeLoss',()=>M.bridgeLoss(level,0));}
 if(level.latches&&it.route)await time(rec,'latch',()=>M.latchCheck(level,it.route,it.cls));
 if(level.pods.some(p=>p.mystery)&&it.route)await time(rec,'fairness',()=>M.fairness(level,it.route));
 if(level.hiddenCells&&it.route)await time(rec,'cellFairness',()=>M.cellFairness(level,it.route));
 rec.total=Object.values(rec.ms).reduce((a,b)=>a+b,0);console.log(rec.name.padEnd(28),'total'.padEnd(14),String(rec.total).padStart(7),'ms');
 out.items.push(rec);}
if(arg('--out'))fs.writeFileSync(arg('--out'),JSON.stringify(out,null,1)+'\n');
if(arg('--compare')){const old=JSON.parse(fs.readFileSync(arg('--compare'),'utf8'));let diff=0,same=0;
 console.log('\nmeasure'.padEnd(44),'old ms'.padStart(8),'new ms'.padStart(8),' speedup  result');
 for(const rec of out.items){const o=old.items.find(x=>x.name===rec.name);if(!o)continue;
  for(const k of Object.keys(rec.ms)){if(o.ms[k]==null)continue;
   // randomEarly is compared with the old full test's class only (early stopping may play fewer games)
   const eq=k==='randomEarly'?(o.results.randomTest&&o.results.randomTest.cls===rec.results[k].cls?'same class':'CLASS DIFFERS'):JSON.stringify(o.results[k])===JSON.stringify(rec.results[k])?'identical':'DIFFERENT';
   if(/DIFF/.test(eq))diff++;else same++;
   console.log(`${rec.name} ${k}`.padEnd(44),String(o.ms[k]).padStart(8),String(rec.ms[k]).padStart(8),(o.ms[k]/Math.max(1,rec.ms[k])).toFixed(1).padStart(7)+'×',' '+eq);}
  console.log(`${rec.name} total`.padEnd(44),String(o.total).padStart(8),String(rec.total).padStart(8),(o.total/Math.max(1,rec.total)).toFixed(1).padStart(7)+'×');}
 console.log(`\n${same} identical, ${diff} different`);if(diff)process.exitCode=1;}

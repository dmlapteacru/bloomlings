// Random play (any legal tap, tapping as soon as possible, no hidden information) 100 times on every level that runs on
// our engine, with the reason of each loss read from the final position:
// - "buffer full of capsules without reachable work": no free place, nothing working;
// - "a linked group needs more free places than there are";
// - "a padlock closes the columns" (Colony Flow);
// - "a linked partner is not on top";
// - "nothing tappable" (other).
// node tools/level-stats/random-reasons.mjs [--part i/n] [--runs 100] → tools/level-stats/out/random/<key>.json
import fs from 'node:fs';import path from 'node:path';
import {allRecords,root} from './levels.mjs';import {engine} from './engines.mjs';import {run} from './trace.mjs';import {STRATEGIES} from './strategies.mjs';
const arg=k=>{const i=process.argv.indexOf(k);return i>0?process.argv[i+1]:null;};const RUNS=+(arg('--runs')||100),out=path.join(root,'tools/level-stats/out/random');fs.mkdirSync(out,{recursive:true});
let recs=allRecords().filter(r=>r.model);if(arg('--only'))recs=recs.filter(r=>arg('--only').split(',').includes(r.key));if(arg('--part')){const [i,n]=arg('--part').split('/').map(Number);recs=recs.filter((r,k)=>k%n===i);}
const saved=k=>{const f=path.join(root,'tools/level-stats/out/levels',k+'.json');return fs.existsSync(f)?JSON.parse(fs.readFileSync(f,'utf8')):null;};
for(const r of recs){const s=saved(r.key);if(s&&!s.model)continue;const E=await engine(r.game),R=E.R;let level=r.model;
 if(r.models&&s){const pick=r.models.find(m=>s.modelNote?.startsWith(m.modelNote));if(pick)level=pick.model;}
 const reasons={},cleared=[];let wins=0;
 for(let i=0;i<RUNS;i++){let last=null;const player=STRATEGIES.random.make(1000+i*7919,E);
  const tr=run(E,level,{player:(t,m)=>{last=t;return player(t,m);},capacity:level.capacity||5});
  if(tr.status==='won'){wins++;continue;}
  cleared.push(Math.round(100*(tr.samples.at(-1)?.[5]||0)/level.pods.reduce((n,p)=>n+(p.count||0),0)));
  // the final position, rebuilt by replaying the taps (the live timeline of the run is gone)
  const t=new E.T.Timeline(R.create(level,level.capacity||5));for(const x of tr.taps){if(x.t>t.now)t.advance(x.t-t.now);t.commit(x.action);}t.advance(1e9);
  const st=t.state,tops=R.topPods(st),why=tops.map(p=>R.canTap(st,p));let reason;
  if(!st.slots.includes(-1))reason='буфер заполнен капсулами без доступной работы';
  else if(why.includes('pair-needs-two-slots'))reason='связке не хватает свободных мест';
  else if(why.includes('padlock'))reason='замок закрывает колонку';
  else if(why.includes('partner-not-exposed'))reason='партнёр связки не наверху';
  else reason='нечего тапнуть ('+[...new Set(why)].join(', ')+')';
  reasons[reason]=(reasons[reason]||0)+1;}
 fs.writeFileSync(path.join(out,r.key+'.json'),JSON.stringify({key:r.key,runs:RUNS,wins,reasons,clearedAtLoss:cleared}));process.stderr.write(`${r.key} ${wins}/${RUNS} ${JSON.stringify(reasons)}\n`);}

// Measures one level (or a share of all levels) and writes tools/level-stats/out/levels/<key>.json: the static picture,
// every recorded attempt (our engine's replay: exact for v8E, approximate for Colony Flow; the video observation for
// Colony Flow), the full-information checks and the strategies. assemble.mjs merges the files.
// node tools/level-stats/measure.mjs [--part i/n] [--only CF-19,v8E-10] [--random 100]
import fs from 'node:fs';import path from 'node:path';
import {allRecords,root,parseToken} from './levels.mjs';import {engine} from './engines.mjs';import {run} from './trace.mjs';
import {fromTrace,fromVideo} from './metrics.mjs';import {staticOf} from './static.mjs';import {solver} from './solve.mjs';import {STRATEGIES,ORDER} from './strategies.mjs';
const arg=k=>{const i=process.argv.indexOf(k);return i>0?process.argv[i+1]:null;};
const RANDOM=+(arg('--random')||100),BUDGET=+(arg('--budget')||60000),out=path.join(root,arg('--out')||'tools/level-stats/out/levels');fs.mkdirSync(out,{recursive:true});
// --cf-gap k: Colony Flow's ants leave their capsule k times slower (the calibration of validate-cf.mjs); written apart (--out)
const CF_GAP=+(arg('--cf-gap')||1);
let recs=allRecords();if(arg('--only'))recs=recs.filter(r=>arg('--only').split(',').includes(r.key));
if(arg('--part')){const [i,n]=arg('--part').split('/').map(Number);recs=recs.filter((r,k)=>k%n===i);}
const observedDir=path.join(root,'tools/level-stats/observed');
// a Colony Flow attempt's taps → engine commands (a tap on a column takes its top capsule and that capsule's group)
function cfCommands(r,a,model=r.model){const tray=model.tray,pods=model.pods,heads=tray.map(()=>0),cmds=[],ms=t=>Math.max(0,Math.round((t-a.start)*1000));
 // padlock openings as recorded, in time order with the taps (the model's own key rule may open them at other times)
 const evs=[...a.taps.map(x=>({t:x.t,c:+x.column.slice(1)-1})),...(a.unlocks||[]).map(u=>({t:u.t,c:+u.column.slice(1)-1,unlock:true}))].sort((x,y)=>x.t-y.t||!!y.unlock-!!x.unlock);
 for(const e of evs){if(e.unlock){heads[e.c]++;cmds.push({t:ms(e.t),action:{type:'unlock',column:e.c}});continue;}
  const p=tray[e.c][heads[e.c]];if(p==null||pods[p].lock)return null;const g=pods[p].group?pods.map((q,i)=>i).filter(i=>pods[i].group===pods[p].group):[p];
  for(const q of g){const qc=tray.findIndex(s=>s.includes(q));heads[qc]++;}cmds.push({t:ms(e.t),action:{type:'tap',pod:g[0]}});}
 return cmds;}
// Colony Flow levels with board objects have two reconstructions (levels.mjs): keep the one on which the recorded won
// attempt replays to a win and the search finds a win; else the first that is winnable; else none
function chooseModel(r,E){if(!r.models||r.models.length<2)return;const won=r.attempts.find(a=>a.outcome==='won');let pick=null;
 for(const m of r.models){const S=solver(E,30000);if(S.route(m.model,m.model.capacity||5).win!==true){m.reject='not winnable on the model';continue;}
  const cmds=won&&cfCommands(r,won,m.model),ok=cmds?run(E,m.model,{commands:cmds}).status==='won':null;m.replays=ok;if(ok){pick=m;break;}pick??=m;}
 if(pick){Object.assign(r,{model:pick.model,modelNote:pick.modelNote+(pick.replays?'; the recorded attempt replays to a win':'; the recorded attempt does not replay to a win (physics or reconstruction)')});}
 else{r.model=null;r.modelNote='board objects reconstructed, but neither reconstruction is winnable on the model';}}
for(const r of recs){const t0=Date.now(),res={key:r.key,game:r.game,set:r.set,level:r.level,difficulty:r.difficulty,title:r.title,app:r.app,source:r.source,capacity:r.capacity,capacityNote:r.capacityNote||null,
  mechanicsText:r.mechanicsText,uncertain:r.uncertain,boxes:r.boxes||false,modelNote:r.modelNote,model:!!r.model,board:r.board,tray:r.tray,static:staticOf(r),attempts:[],checks:null,strategies:null};
 if(r.game==='CF'&&CF_GAP!==1){if(r.model)r.model={...r.model,gapScale:CF_GAP};if(r.models)r.models=r.models.map(m=>({...m,model:{...m.model,gapScale:CF_GAP}}));}
 if(r.models)chooseModel(r,await engine(r.game));res.model=!!r.model;res.modelNote=r.modelNote;
 const E=r.model?await engine(r.game):null,S=E?solver(E,BUDGET):null,cap=r.model?(r.model.capacity||5):r.capacity;
 // recorded attempts
 const obs=fs.existsSync(path.join(observedDir,r.key.replace(/^CF-/,'L')+'.json'))?JSON.parse(fs.readFileSync(path.join(observedDir,r.key.replace(/^CF-/,'L')+'.json'),'utf8')):null;
 for(const a of r.attempts){const A={attempt:a.attempt,outcome:a.outcome,start:a.start,end:a.end,videos:a.videos||null,taps:(a.taps||a.commands).length,speed:a.speed||null,rating:a.rating||null,interest:a.interest||null,note:a.note||null,timeBuckets:a.timeBuckets||null,
   tapTimes:a.taps?a.taps.map(x=>Math.round((x.t-a.start)*100)/100):a.commands.map(c=>c.real!=null?Math.round(c.real/10)/100:null),columns:a.taps?a.taps.map(x=>x.column):null};
  if(r.game==='v8E'){const tr=run(E,r.model,{commands:a.commands});A.replay={kind:'exact',...fromTrace(tr)};A.replayMatches=tr.status===a.outcome;
   A.fatal=S.fatal(r.model,cap,a.commands);A.realSeconds=Math.round(a.end*10)/10;}
  else if(r.game==='CF'){
   if(r.model){const cmds=cfCommands(r,a);if(cmds){const tr=run(E,r.model,{commands:cmds});A.replay={kind:'model (approximate physics)',...fromTrace(tr)};A.replayMatches=a.outcome==='unfinished'?null:tr.status===a.outcome;A.fatal=S.fatal(r.model,cap,cmds);}}
   const oa=obs?.attempts?.find(x=>x.attempt===a.attempt);if(oa){const cp=oa.capacity||cap;A.video={kind:'video',...fromVideo(oa,a.taps,cp),validation:oa.validation||null};}}
  res.attempts.push(A);}
 // full-information checks and strategies on our engine
 if(r.model){const route=S.route(r.model,cap);res.checks={capacity:cap,winnable:route.win,searchNodes:route.nodes};
  if(route.route){const dec=S.decisions(r.model,cap,route.route);res.checks.route={taps:route.route.length,decisions:dec.decisions,risky:dec.risky,tight:dec.tight,unknown:dec.unknown,safeFromTap:dec.safeFrom,steps:dec.steps};
   // the route on the live timeline: patiently (the reference rhythm, same player everywhere) and as fast as possible
   const cmds=route.route.map(a=>({t:0,action:a})),pat=run(E,r.model,{commands:cmds,patient:true}),fast=run(E,r.model,{commands:cmds}),fm=fromTrace(fast);
   res.checks.routeLive=fromTrace(pat);res.checks.routeLive.kind='winning route, each tap after the work settles';
   res.checks.routeFast={status:fast.status,end:fm.end,taps:fm.capsules.taps,routeTaps:route.route.length,cleared:Math.round(100*fm.clearedCells/fast.cells),zeroFreeShare:fm.buffer?.zeroFreeShare??null,meanFree:fm.buffer?.meanFree??null};}
  res.checks.sequences=S.count(r.model,cap,+(arg("--count-budget")||5000));res.checks.minCapacity=S.minCapacity(r.model,cap);
  // what is open at the start: tops that can be tapped, and those whose colour the ants can reach now
  {const s=E.R.create(r.model,cap),reach=new Set(E.R.reach(s).targets.map(i=>s.board[i])),tops=E.R.topPods(s);res.checks.start={tops:tops.length,tappable:E.R.legal(s).filter(a=>a.type==='tap').length,useful:tops.filter(p=>reach.has(s.meta.colours[p])).length,reachableCells:E.R.reach(s).targets.length};}
  res.strategies={};
  for(const k of ORDER){const st=STRATEGIES[k],n=st.random?RANDOM:1,runs=[];
   for(let i=0;i<n;i++){const tr=run(E,r.model,{player:st.make(1000+i*7919,E),capacity:cap});const m=fromTrace(tr);
    runs.push({status:tr.status,end:m.end,maxHeld:m.buffer?.maxHeld??null,zeroFree:m.buffer?.zeroFree??null,longStops:m.capsules.stopSeconds.n?m.capsules.stopSeconds.max:0,cleared:tr.cells?Math.round(100*m.clearedCells/tr.cells):null,taps:m.capsules.taps,left:tr.left});}
   const wins=runs.filter(x=>x.status==='won');
   res.strategies[k]={label:st.label,runs:n,wins:wins.length,rate:Math.round(100*wins.length/n)/100,endMedian:wins.length?wins.map(x=>x.end).sort((a,b)=>a-b)[wins.length>>1]:null,
    maxHeld:Math.max(...runs.map(x=>x.maxHeld??0)),zeroFreeMedian:runs.map(x=>x.zeroFree??0).sort((a,b)=>a-b)[runs.length>>1],longestStop:Math.max(...runs.map(x=>x.longStops)),
    stuckAt:runs.filter(x=>x.status!=='won').map(x=>x.cleared),stuckLeft:runs.filter(x=>x.status!=='won').map(x=>x.left)};}
  // the same planner order with and without waiting for the current work (representative levels only: see assemble)
  const pat=run(E,r.model,{player:STRATEGIES.planner.make(1,E),capacity:cap,reactionMs:1e9});res.strategies.plannerPatient={label:'с планированием, ждёт окончания работы',runs:1,wins:pat.status==='won'?1:0,rate:pat.status==='won'?1:0,endMedian:pat.status==='won'?Math.round(pat.end/100)/10:null};}
 res.seconds=Math.round((Date.now()-t0)/100)/10;
 fs.writeFileSync(path.join(out,r.key+'.json'),JSON.stringify(res));process.stderr.write(`${r.key} ${res.seconds}s\n`);}

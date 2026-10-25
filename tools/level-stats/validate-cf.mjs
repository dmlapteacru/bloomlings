// How well the model reproduces Colony Flow: every recorded attempt on a level that runs on the model is replayed with
// its taps at their recorded times (padlocks open when the video shows; a tap the model cannot take yet waits until it
// can), and the model's filled places are compared with the video's, frame by frame (every 0.5 s of the attempt).
// Measures: the outcome (unfinished recordings left out), the delay the model forced on each tap, mean free places and
// the full-buffer share (video against model), the mean absolute difference of filled places, the first moment the two
// differ by 2 or more places for at least 3 s, and the "slot-seconds" ratio (area under the filled-places curve).
// Repeated with the ants' departures slowed (gapScale) and trips slowed (tripScale) to test the main suspected cause.
// node tools/level-stats/validate-cf.mjs [--scales 1x1,1.5x1,...] → tools/level-stats/out/validate-cf.json
import fs from 'node:fs';import path from 'node:path';
import {allRecords,root} from './levels.mjs';import {engine} from './engines.mjs';import {run} from './trace.mjs';
const arg=k=>{const i=process.argv.indexOf(k);return i>0?process.argv[i+1]:null;};
const SCALES=(arg('--scales')||'1x1,1.5x1,2x1,2.5x1,3x1,1x1.5,2x1.5').split(',').map(s=>s.split('x').map(Number));
const E=await engine('CF'),obsDir=path.join(root,'tools/level-stats/observed');
const r2=x=>x==null||!Number.isFinite(x)?null:Math.round(x*100)/100,med=a=>{a=a.filter(Number.isFinite).sort((x,y)=>x-y);return a.length?a[a.length>>1]:null;};
function commands(model,a){const tray=model.tray,pods=model.pods,heads=tray.map(()=>0),cmds=[],ms=t=>Math.max(0,Math.round((t-a.start)*1000));
 const evs=[...a.taps.map(x=>({t:x.t,c:+x.column.slice(1)-1})),...(a.unlocks||[]).map(u=>({t:u.t,c:+u.column.slice(1)-1,unlock:true}))].sort((x,y)=>x.t-y.t||!!y.unlock-!!x.unlock);
 for(const e of evs){if(e.unlock){heads[e.c]++;cmds.push({t:ms(e.t),action:{type:'unlock',column:e.c}});continue;}
  const p=tray[e.c][heads[e.c]];if(p==null||pods[p].lock)return null;const g=pods[p].group?pods.map((q,i)=>i).filter(i=>pods[i].group===pods[p].group):[p];
  for(const q of g)heads[tray.findIndex(s=>s.includes(q))]++;cmds.push({t:ms(e.t),action:{type:'tap',pod:g[0]},size:g.length});}
 return cmds;}
// filled places at time t (seconds of the attempt) in a step series [[t, filled], ...]
const at=(series,t)=>{let v=null;for(const [x,y] of series){if(x<=t+1e-9)v=y;else break;}return v;};
function compare(level,a,obs,scale){const model={...level,gapScale:scale[0],tripScale:scale[1]},cmds=commands(model,a);if(!cmds)return {error:'tray does not match the taps'};
 const tr=run(E,model,{commands:cmds}),cap=tr.capacity,tapCmds=cmds.filter(c=>c.action.type==='tap');
 const delays=tr.taps.map((x,i)=>tapCmds[i]?(x.t-tapCmds[i].t)/1000:null).filter(x=>x!=null);
 const mFilled=tr.samples.map(s=>[s[0]/1000,cap-s[1]]),vFilled=obs.slots.filter(s=>s[1]!=null).map(([t,f])=>[t-a.start,f]);
 const end=Math.min(a.end-a.start,tr.end/1000);let diff=0,n=0,first=null,run3=0,areaV=0,areaM=0;
 for(let t=0;t<=end;t+=0.5){const v=at(vFilled,t),m=at(mFilled,t);if(v==null||m==null)continue;diff+=Math.abs(v-m);n++;areaV+=v;areaM+=m;
  if(Math.abs(v-m)>=2){run3+=0.5;if(run3>=3&&first==null)first=r2(t-2.5);}else run3=0;}
 const vCap=Math.max(cap,...obs.slots.map(s=>s[2]?.length||0));
 const share0=(series,c)=>{let z=0,tot=0;for(let i=0;i<series.length;i++){const t0=series[i][0],t1=i+1<series.length?series[i+1][0]:end;if(t0>=end)break;const d=Math.min(t1,end)-t0;tot+=d;if(series[i][1]>=c)z+=d;}return tot?z/tot:null;};
 const meanFree=(series,c)=>{let s=0,tot=0;for(let i=0;i<series.length;i++){const t0=series[i][0],t1=i+1<series.length?series[i+1][0]:end;if(t0>=end)break;const d=Math.min(t1,end)-t0;tot+=d;s+=(c-series[i][1])*d;}return tot?s/tot:null;};
 return {outcome:a.outcome,model:tr.status,taps:tapCmds.length,taken:tr.taps.length,delayed:delays.filter(d=>d>1).length,maxDelay:r2(Math.max(0,...delays)),firstDelayAt:r2(tr.taps.find((x,i)=>tapCmds[i]&&x.t-tapCmds[i].t>1000)?.t/1000??null),
  mae:r2(diff/Math.max(1,n)),firstDivergence:first,slotSecondsRatio:r2(areaM/Math.max(1e-9,areaV)),videoMeanFree:r2(meanFree(vFilled,vCap)),modelMeanFree:r2(meanFree(mFilled,cap)),videoFull:r2(share0(vFilled,vCap)),modelFull:r2(share0(mFilled,cap)),
  videoEnd:r2(a.end-a.start),modelEnd:r2(tr.end/1000)};}
const out={scales:SCALES.map(s=>s.join('x')),levels:[]};
for(const r of allRecords().filter(r=>r.game==='CF'&&r.model)){const obs=fs.existsSync(path.join(obsDir,`L${r.level}.json`))?JSON.parse(fs.readFileSync(path.join(obsDir,`L${r.level}.json`),'utf8')):null;if(!obs)continue;
 // the reconstruction measure.mjs kept (the first, or the one the recorded attempt replays on)
 const savedLevel=JSON.parse(fs.readFileSync(path.join(root,'tools/level-stats/out/levels',r.key+'.json'),'utf8'));if(!savedLevel.model)continue;// measure.mjs found no winnable reconstruction
 let level=r.model;if(r.models){const saved=JSON.parse(fs.readFileSync(path.join(root,'tools/level-stats/out/levels',r.key+'.json'),'utf8'));const pick=r.models.find(m=>saved.modelNote?.startsWith(m.modelNote));if(pick)level=pick.model;}
 const mech=[level.pods.some(p=>p.group)?'связки':null,level.pods.some(p=>p.mystery)?'скрытые':null,level.pods.some(p=>p.lock)?'замки':null,(level.objects||[]).length?'предметы':null].filter(Boolean);
 for(const a of r.attempts){const oa=obs.attempts.find(x=>x.attempt===a.attempt);if(!oa)continue;
  const res={key:r.key,attempt:a.attempt,difficulty:r.difficulty,mechanics:mech,videos:a.videos,byScale:{}};
  for(const s of SCALES)res.byScale[s.join('x')]=compare(level,a,oa,s);out.levels.push(res);process.stderr.write(`${r.key}#${a.attempt} ${JSON.stringify(Object.fromEntries(Object.entries(res.byScale).map(([k,v])=>[k,[v.model,v.mae,v.slotSecondsRatio]])))}\n`);}}
// per scale: outcome matches (finished recordings), median MAE, median slot-seconds ratio, median free-places difference
out.summary=Object.fromEntries(out.scales.map(k=>{const v=out.levels.map(l=>l.byScale[k]).filter(x=>!x.error),fin=v.filter(x=>x.outcome!=='unfinished');
 return [k,{attempts:v.length,finished:fin.length,outcomeMatches:fin.filter(x=>x.outcome===x.model).length,maeMedian:r2(med(v.map(x=>x.mae))),slotSecondsRatioMedian:r2(med(v.map(x=>x.slotSecondsRatio))),
  meanFreeDiffMedian:r2(med(v.map(x=>x.modelMeanFree-x.videoMeanFree))),fullDiffMedian:r2(med(v.map(x=>x.modelFull-x.videoFull))),delayedTaps:v.reduce((n,x)=>n+x.delayed,0),endRatioMedian:r2(med(v.map(x=>x.modelEnd/x.videoEnd)))}];}));
fs.writeFileSync(path.join(root,'tools/level-stats/out/validate-cf.json'),JSON.stringify(out));console.log(JSON.stringify(out.summary,null,1));

// Speed calibration of our engine against Colony Flow recordings with a confirmed speed (the owner's recordings: the ×2
// button is green with a running timer when on; the timer readings show when it was on). Each reference round is
// replayed with its taps at their recorded times (a tap the model cannot take yet waits; the reason and delay are kept)
// and compared with the video: times from the first accepted tap to 20, 40, 60, 80 and 100 % of the board cleared (100 %
// = the last cell gone; the win screen apart), the slot releases, the filled places over time and the final tail.
// One setting of the ants' departures, walks, clearing and slot release is fitted on two references and checked on the
// third. Our engine's timeline is what the pilot page animates (couriers, slots), so its times are the animation times.
// node tools/level-stats/calibrate.mjs → tools/level-stats/out/calibrate.json
import fs from 'node:fs';import path from 'node:path';
import {allRecords,root} from './levels.mjs';import {engine} from './engines.mjs';import {run} from './trace.mjs';
const E=await engine('CF'),recs=Object.fromEntries(allRecords().map(r=>[r.key,r]));
const r2=x=>x==null||!Number.isFinite(x)?null:Math.round(x*100)/100;
// references: level, attempt, the ×2 stretches in video seconds (from the timer readings), the role
const REFS=[{key:'CF-2',attempt:1,x2:[],role:'подбор',note:'level-1-4.mp4 35–72,5 с; ×1 целиком (таймер ×2 не тикал между L1 и L5)'},
 {key:'CF-26',attempt:1,x2:[],role:'подбор',note:'XRecorder_20261024_03.mp4; ×1 целиком (кнопка серая всю запись)'},
 {key:'CF-20',attempt:1,x2:[[9,15]],role:'проверка',note:'XRecorder_20261023_02.mp4; ×1, кроме ×2 примерно 9–15 с (таймер 17:47 → 17:41 к L25)'}];
const MS=[.2,.4,.6,.8,1];
// video seconds ↔ model ms with ×2 stretches (each video second at ×2 is two model seconds)
const toModel=(t,x2)=>1000*(t+x2.reduce((n,[a,b])=>n+Math.max(0,Math.min(t,b)-a),0));
const toVideo=(ms,x2)=>{const t=ms/1000;let extra=0;for(const [a,b] of x2.slice().sort((p,q)=>p[0]-q[0])){const ma=a+extra,mb=ma+2*(b-a);if(t<=ma)break;if(t<=mb)return a+(t-ma)/2;extra+=b-a;}return t-extra;};
function commands(model,a,x2){const tray=model.tray,pods=model.pods,heads=tray.map(()=>0),cmds=[];
 for(const x of a.taps){const c=+x.column.slice(1)-1,p=tray[c][heads[c]];if(p==null)return null;const g=pods[p].group?pods.map((q,i)=>i).filter(i=>pods[i].group===pods[p].group):[p];
  for(const q of g)heads[tray.findIndex(s=>s.includes(q))]++;cmds.push({t:Math.round(toModel(x.t-a.start,x2)),action:{type:'tap',pod:g[0]},video:x.t-a.start});}
 return cmds;}
const obs=Object.fromEntries(REFS.map(ref=>[ref.key,JSON.parse(fs.readFileSync(path.join(root,'tools/level-stats/observed',ref.key.replace('CF-','L')+'.json'),'utf8')).attempts.find(x=>x.attempt===ref.attempt)]));
// the video's measures, from the first tap
function videoMeasures(ref){const a=recs[ref.key].attempts.find(x=>x.attempt===ref.attempt),o=obs[ref.key],t0=a.taps[0].t;
 const board=o.board.filter(b=>b[1]!=null).map(([t,v])=>[t-t0,1-v]),filled=o.slots.filter(s=>s[1]!=null).map(([t,f])=>[t-t0,f]);
 const ms=MS.map(m=>{const p=board.find(([,v])=>v>=(m===1?.995:m));return p?p[0]:null;});
 const rel=[];for(let i=1;i<filled.length;i++)if(filled[i][1]<filled[i-1][1])rel.push([filled[i][0],filled[i-1][1]-filled[i][1]]);
 return {milestones:ms,lastTap:a.taps.at(-1).t-t0,releases:rel,filled,taps:a.taps.length};}
function modelMeasures(ref,set){const r=recs[ref.key],a=r.attempts.find(x=>x.attempt===ref.attempt),level={...r.model,gapScale:set.gap,tripScale:set.trip,actScale:set.act??set.trip,releaseLagMs:set.release};
 const cmds=commands(level,a,ref.x2);const tr0=run(E,level,{commands:cmds}),total=level.pods.reduce((n,p)=>n+(p.count||0),0);
 // the page plays the tail (the tray empty) at its tail speed: page time is the model's time with the tail compressed
 const ts=tr0.status==='won'?tr0.taps.at(-1).t:Infinity,tk=set.tail||1,pg=ms=>ms<=ts?ms:ts+(ms-ts)/tk;
 const tr={...tr0,taps:tr0.taps.map(x=>({...x,t:pg(x.t)})),samples:tr0.samples.map(s=>[pg(s[0]),...s.slice(1)]),releases:tr0.releases.map(x=>({...x,t:pg(x.t)}))};
 const t0=tr.taps[0]?toVideo(tr.taps[0].t,ref.x2):0;
 const cl=tr.samples.map(s=>[toVideo(s[0],ref.x2)-t0,s[5]/total]);const ms=MS.map(m=>{const p=cl.find(([,v])=>v>=m-1e-9);return p?p[0]:null;});
 const delays=tr.taps.map((x,i)=>(x.t-cmds[i].t)/1000);
 const filled=tr.samples.map(s=>[toVideo(s[0],ref.x2)-t0,tr.capacity-s[1]]);
 return {status:tr.status,cleared:cl,tapTimes:tr.taps.map(x=>toVideo(x.t,ref.x2)-t0),milestones:ms,lastTap:toVideo(tr.taps.at(-1).t,ref.x2)-t0,releases:tr.releases.map(x=>[toVideo(x.t,ref.x2)-t0,x.pods.length]),filled,
  delayed:delays.filter(d=>d>0.25).length,maxDelay:r2(Math.max(0,...delays)),refused:tr.refused.map(x=>x.reason),taken:tr.taps.length,taps:cmds.length};}
const at=(series,t)=>{let v=null;for(const [x,y] of series){if(x<=t+1e-9)v=y;else break;}return v;};
function compare(v,m){const tol=t=>Math.max(.15*t,2);const ms=MS.map((p,i)=>{const a=v.milestones[i],b=m.milestones[i];return {share:p,video:r2(a),model:r2(b),diff:a!=null&&b!=null?r2(b-a):null,rel:a&&b!=null?r2((b-a)/a):null,ok:a!=null&&b!=null?(p===1?Math.abs(b-a)<=.1*a:Math.abs(b-a)<=tol(a)):null};});
 let mae=0,n=0;const end=Math.min(v.milestones[4]??1e9,m.milestones[4]??1e9);for(let t=0;t<=end;t+=.5){const a=at(v.filled,t),b=at(m.filled,t);if(a==null||b==null)continue;mae+=Math.abs(a-b);n++;}
 const relN=x=>x.reduce((s,[,k])=>s+k,0);
 return {milestones:ms,total:{video:r2(v.milestones[4]),model:r2(m.milestones[4]),rel:ms[4].rel,ok:ms[4].ok},tail:{video:r2(v.milestones[4]!=null?v.milestones[4]-v.lastTap:null),model:r2(m.milestones[4]!=null?m.milestones[4]-m.lastTap:null)},
  filledMae:r2(mae/Math.max(1,n)),releases:{video:relN(v.releases),model:relN(m.releases)},taps:{taken:m.taken,of:m.taps,delayed:m.delayed,maxDelay:m.maxDelay,refused:[...new Set(m.refused)]},status:m.status};}
// milestones, then the filled places, then the taps the model could not take when the player did (a slot was free in
// the video at each tap, so a waiting tap means the model frees its slots too late)
const score=c=>c.milestones.reduce((s,x)=>s+(x.rel==null?1:Math.min(1,Math.abs(x.rel))),0)/5+0.15*(c.filledMae??2)+0.5*c.taps.delayed/Math.max(1,c.taps.of);
const V=Object.fromEntries(REFS.map(ref=>[ref.key,videoMeasures(ref)]));
// v8E as played: departures 250 ms apart, walks 100/75 ms a cell, 100 ms clearing, the slot freed 300 ms after the
// last departure, the tail at ×3
const BASE={gap:1,trip:1,act:1,release:300,tail:3};
const grid=[];for(const gap of [1,1.25,1.5,1.75,2])for(const trip of [.75,1,1.25,1.5])for(const act of [1,2])for(const release of [300,800])for(const tail of [1,1.5,2,3])grid.push({gap,trip,act,release,tail});
const fit=REFS.filter(r=>r.role==='подбор'),check=REFS.filter(r=>r.role==='проверка');
let best=null;const tried=[];
// each setting's parts on the fitting rounds (milestone error, filled-places error, share of taps kept waiting) and its
// score on the check round, so the near-ties and their effect on the check round can be shown
const parts=cs=>({ms:r2(cs.reduce((n,c)=>n+c.milestones.reduce((s,x)=>s+(x.rel==null?1:Math.min(1,Math.abs(x.rel))),0)/5,0)/cs.length),mae:r2(cs.reduce((n,c)=>n+(c.filledMae??2),0)/cs.length),delayed:r2(cs.reduce((n,c)=>n+c.taps.delayed/Math.max(1,c.taps.of),0)/cs.length)});
for(const set of grid){const cs=fit.map(ref=>compare(V[ref.key],modelMeasures(ref,set)));const s=cs.reduce((n,c)=>n+score(c),0)/cs.length;tried.push({set,score:r2(s),fit:parts(cs)});if(!best||s<best.score)best={set,score:s};}
// near-ties (within 0.01 of the best score on the fitting rounds) are told apart without the check round: fewest taps
// kept waiting on the fitting rounds (the hardest evidence: a slot was free in the video), then fewest changes from v8E
tried.sort((a,b)=>a.score-b.score);const changes=x=>Object.keys(BASE).filter(k=>x[k]!==BASE[k]).length;
best={set:tried.filter(t=>t.score<=tried[0].score+0.01+1e-9).sort((a,b)=>a.fit.delayed-b.fit.delayed||changes(a.set)-changes(b.set)||a.score-b.score)[0].set,score:tried[0].score};
best.score=tried.find(t=>t.set===best.set).score;
for(const t of tried.slice(0,12)){const cs=check.map(ref=>compare(V[ref.key],modelMeasures(ref,t.set)));t.check={score:r2(cs.reduce((n,c)=>n+score(c),0)/cs.length),...parts(cs)};}
const scoreOf=(set,refs)=>{const cs=refs.map(ref=>compare(V[ref.key],modelMeasures(ref,set)));return {score:r2(cs.reduce((n,c)=>n+score(c),0)/cs.length),...parts(cs)};};
tried.sort((a,b)=>a.score-b.score);
const result={refs:REFS,base:BASE,best:best.set,rule:'лучший счёт на раундах подбора; при почти равных (±0,01) — меньше нажатий, ждущих место, затем меньше изменений от v8E; раунд проверки в выборе не участвует',grid:grid.length,top:tried.slice(0,12),scores:{before:{fit:scoreOf(BASE,fit),check:scoreOf(BASE,check)},after:{fit:scoreOf(best.set,fit),check:scoreOf(best.set,check)}},byRef:{}};
for(const ref of REFS)result.byRef[ref.key]={role:ref.role,note:ref.note,video:{milestones:V[ref.key].milestones.map(r2),lastTap:r2(V[ref.key].lastTap),taps:V[ref.key].taps},
 before:compare(V[ref.key],modelMeasures(ref,BASE)),after:compare(V[ref.key],modelMeasures(ref,best.set)),
 series:(()=>{const a=recs[ref.key].attempts.find(x=>x.attempt===ref.attempt),t0=a.taps[0].t,thin=x=>x.filter((p,i,arr)=>i===0||i===arr.length-1||p[1]!==arr[i-1][1]);
  const model=set=>{const m=modelMeasures(ref,set),cl=[];let last=-1;for(const [t,v] of m.cleared){const k=Math.round(v*100);if(k!==last){cl.push([r2(t),k/100]);last=k;}}
   return {cleared:cl,filled:thin(m.filled).map(([t,f])=>[r2(t),f]),taps:m.tapTimes.map(r2),releases:m.releases.map(([t,k])=>[r2(t),k])};};
  return {video:{cleared:obs[ref.key].board.filter(b=>b[1]!=null).map(([t,v])=>[r2(t-t0),r2(1-v)]),filled:thin(V[ref.key].filled).map(([t,f])=>[r2(t),f]),taps:a.taps.map(x=>r2(x.t-t0)),x2:ref.x2.map(([u,v])=>[r2(u-t0),r2(v-t0)])},before:model(BASE),after:model(best.set)};})()};
fs.writeFileSync(path.join(root,'tools/level-stats/out/calibrate.json'),JSON.stringify(result));
console.log('best',JSON.stringify(best.set),'score',r2(best.score));console.log('top',JSON.stringify(tried.slice(0,12)));console.log('scores',JSON.stringify(result.scores));
for(const [k,v] of Object.entries(result.byRef)){console.log('\n##',k,v.role,'video milestones',JSON.stringify(v.video.milestones));for(const w of ['before','after']){const c=v[w];console.log(' ',w,'total',JSON.stringify(c.total),'tail',JSON.stringify(c.tail),'mae',c.filledMae,'releases',JSON.stringify(c.releases),'taps',JSON.stringify(c.taps),'ms',c.milestones.map(x=>`${x.share*100}%:${x.model}(${x.ok?'ok':'x'})`).join(' '));}}

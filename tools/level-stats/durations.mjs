// Round durations of the v8E levels on our engine, at the speed as played and at the calibrated speed (calibrate.mjs),
// for ×1 and ×2. Page time: the model's time divided by the mode's speed until the tray is empty, then by the tail speed
// (the page plays the tail at its own speed whatever the mode). A round is timed from the first accepted tap to the last
// cell gone; the win screen apart. Automatic players: the solver's winning route tapped as soon as possible, the same
// route tapped only once all work has settled, and the planner waiting 1.5 s of page time before each tap (3 seeds).
// The owner's own rounds are listed apart, as played (v8E speed), by the mode they were played in.
// node tools/level-stats/durations.mjs → tools/level-stats/out/durations.json
import fs from 'node:fs';import path from 'node:path';
import {allRecords,root} from './levels.mjs';import {engine} from './engines.mjs';import {run} from './trace.mjs';import {solver} from './solve.mjs';import {STRATEGIES} from './strategies.mjs';
const out=path.join(root,'tools/level-stats/out'),D=JSON.parse(fs.readFileSync(path.join(out,'data.json'),'utf8')),CAL=JSON.parse(fs.readFileSync(path.join(out,'calibrate.json'),'utf8'));
const SETS={before:CAL.base,after:CAL.best},MODES=[1,2],REACT=1500;
const E=await engine('v8E'),rows=Object.fromEntries(D.rows.map(r=>[r.key,r]));
const sizeBin=c=>c<=300?'до 300 клеток':c<=600?'301–600 клеток':'больше 600 клеток';
const runs=[],res=JSON.parse(fs.readFileSync(path.join(root,'product/playtests/pilot-v8e-owner-2026-10-24/results.json'),'utf8'));
// page seconds from the first tap to the last cell gone
function pageSeconds(tr,mode,tail){if(tr.status!=='won'||!tr.taps.length)return null;const ts=tr.taps.at(-1).t,pg=ms=>ms<=ts?ms/mode:ts/mode+(ms-ts)/tail;
 const end=Math.max(...tr.clearAt);return Math.round((pg(end)-pg(tr.taps[0].t))/100)/10;}
for(const r of allRecords().filter(x=>x.game==='v8E'&&x.model)){const row=rows[r.key];if(!row)continue;
 const kinds=[r.model.pods.some(p=>p.mystery)?'скрытые':null,r.model.pods.some(p=>p.group!=null)?'связки':null].filter(Boolean).join(' + ')||'без допов';
 const base={key:r.key,level:r.level,difficulty:row.difficulty,cells:row.cells,size:sizeBin(row.cells),combo:kinds};
 const cap=r.model.capacity||5,route=solver(E,30000).route(r.model,cap);
 for(const [setName,set] of Object.entries(SETS)){const level={...r.model,gapScale:set.gap,tripScale:set.trip,actScale:set.act??set.trip,releaseLagMs:set.release},tail=set.tail||3;
  if(route.route){const cmds=route.route.map(a=>({t:0,action:a}));const fast=run(E,level,{commands:cmds}),pat=run(E,level,{commands:cmds,patient:true});
   for(const mode of MODES){runs.push({...base,set:setName,mode,player:'маршрут решателя, без пауз',status:fast.status,seconds:pageSeconds(fast,mode,tail)});
    runs.push({...base,set:setName,mode,player:'маршрут решателя, после окончания работы',status:pat.status,seconds:pageSeconds(pat,mode,tail)});}}
  for(const mode of MODES)for(const seed of [1,2,3]){const tr=run(E,level,{player:STRATEGIES.planner.make(seed,E),capacity:cap,reactionMs:REACT*mode});
   runs.push({...base,set:setName,mode,player:'планировщик, 1,5 с на решение',seed,status:tr.status,seconds:pageSeconds(tr,mode,tail)});}
  // the owner's won rounds: the same taps at the same page moments, in the mode they were played in (a tap the engine
  // cannot take yet waits); at the speed as played this reproduces the recording, at the calibrated speed it shows
  // what the same play would take
  for(const a of r.attempts.filter(x=>x.outcome==='won')){const raw=res.attempts.filter(x=>x.level===r.level&&x.outcome==='won')[r.attempts.filter(x=>x.outcome==='won').indexOf(a)];
   if(!raw||raw.speedEvents?.some(e=>e.type==='manual-speed')||a.commands.some(c=>c.real==null))continue;const mode=raw.config.initialSettings.effectiveSpeed===2?2:1,r0=a.commands[0].real;
   const tr=run(E,level,{commands:a.commands.map(c=>({t:Math.round((c.real-r0)*mode),action:c.action}))});
   runs.push({...base,set:setName,mode,player:'твои нажатия в те же секунды страницы',attempt:a.attempt,status:tr.status,seconds:pageSeconds(tr,mode,tail),recorded:Math.round((raw.elapsedMs-raw.inputEvents.find(e=>e.accepted).atMs)/100)/10});}}
 process.stderr.write(r.key+' ');}
// the owner's rounds as played: page time from the first tap to the last cell, by the mode of the round
const human=res.attempts.filter(a=>a.outcome==='won').map(a=>{const row=rows['v8E-'+a.level],first=a.inputEvents.find(e=>e.accepted)?.atMs??0,manual=(a.speedEvents||[]).filter(e=>e.type==='manual-speed'),mode=!manual.length?(a.config.initialSettings.effectiveSpeed===2?2:1):'смешанный';
 return {key:'v8E-'+a.level,level:a.level,difficulty:row?.difficulty,cells:row?.cells,size:row?sizeBin(row.cells):null,mode,seconds:Math.round((a.elapsedMs-first)/100)/10,tailSeconds:a.autoFastAtMs!=null?Math.round((a.elapsedMs-a.autoFastAtMs)/100)/10:null};});
const isNum=x=>typeof x==='number'&&Number.isFinite(x);
const q=(a,p)=>{const i=(a.length-1)*p,k=Math.floor(i);return a[k]+(a[Math.min(k+1,a.length-1)]-a[k])*(i-k);};
const summary=v=>{v=v.filter(isNum).sort((x,y)=>x-y);if(!v.length)return {n:0};const r1=x=>Math.round(x*10)/10;return {n:v.length,mean:r1(v.reduce((s,x)=>s+x,0)/v.length),median:r1(q(v,.5)),p25:r1(q(v,.25)),p75:r1(q(v,.75)),min:v[0],max:v.at(-1)};};
const groups=[['все уровни',()=>'все'],['размер',x=>x.size],['сложность',x=>x.difficulty],['допы',x=>x.combo]];
const auto=[];for(const set of Object.keys(SETS))for(const mode of MODES)for(const player of [...new Set(runs.map(x=>x.player))])for(const [g,f] of groups){
 const sel=runs.filter(x=>x.set===set&&x.mode===mode&&x.player===player);for(const v of [...new Set(sel.map(f))])auto.push({set,mode,player,group:g,value:v,lost:sel.filter(x=>f(x)===v&&x.status!=='won').length,...summary(sel.filter(x=>f(x)===v).map(x=>x.seconds))});}
const hum=[];for(const mode of [1,2,'смешанный'])for(const [g,f] of groups){const sel=human.filter(x=>x.mode===mode);for(const v of [...new Set(sel.map(f))])hum.push({mode,group:g,value:v,...summary(sel.filter(x=>f(x)===v).map(x=>x.seconds))});}
const RP='твои нажатия в те же секунды страницы',replay=runs.filter(x=>x.player===RP&&x.set==='before').map(x=>{const y=runs.find(z=>z.player===RP&&z.set==='after'&&z.key===x.key&&z.attempt===x.attempt);return {key:x.key,level:x.level,attempt:x.attempt,mode:x.mode,recorded:x.recorded,before:x.seconds,after:y?.seconds??null,afterStatus:y?.status??null};});
fs.writeFileSync(path.join(out,'durations.json'),JSON.stringify({sets:SETS,reactionMs:REACT,runs,human,auto,humanSummary:hum,replay}));
{const rp=runs.filter(x=>x.player==='твои нажатия в те же секунды страницы'&&x.set==='before'&&x.seconds!=null);console.log('replay check (before vs recorded): n',rp.length,'median |diff| s',(()=>{const d=rp.map(x=>Math.abs(x.seconds-x.recorded)).sort((a,b)=>a-b);return d[d.length>>1];})(),'max',Math.max(...rp.map(x=>Math.abs(x.seconds-x.recorded))));}
console.log('\nruns',runs.length,'won',runs.filter(x=>x.status==='won').length,'human',human.length);
for(const a of auto.filter(x=>x.group==='все уровни'))console.log(a.set,'×'+a.mode,a.player,JSON.stringify({n:a.n,lost:a.lost,mean:a.mean,median:a.median,p25:a.p25,p75:a.p75,min:a.min,max:a.max}));
for(const h of hum.filter(x=>x.group==='все уровни'))console.log('human ×'+h.mode,JSON.stringify(h));

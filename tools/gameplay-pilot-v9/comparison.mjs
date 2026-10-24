// Reproduce v8E/v9 model diagnostics. Never interpret bot policy wins as human difficulty or retention.
import fs from 'node:fs';import {compareRhythm} from './rhythm.mjs';
const read=p=>JSON.parse(fs.readFileSync(p));
const a=read('content/pilot-v9/baseline-v8e-c6b0bfd4/levels.json'),ac=read('content/pilot-v9/baseline-v8e-c6b0bfd4/certificates.json'),b=read('content/pilot-v9/levels.json'),bc=read('content/pilot-v9/certificates.json'),q=read('content/pilot-v9/quality.json');
const refs=read('product/audits/colony-flow-rounds/levels.json');
const shape=l=>({coloredCells:l.pods.reduce((n,p)=>n+p.count,0),colors:new Set(l.pods.map(p=>p.color)).size,lots:l.pods.length,medianQuota:l.pods.map(p=>p.count).sort((a,b)=>a-b)[Math.floor(l.pods.length/2)],maxQuota:Math.max(...l.pods.map(p=>p.count)),links:new Set(l.pods.map(p=>p.group).filter(Boolean)).size,mysteries:l.pods.filter(p=>p.mystery).length});
const brief=r=>Object.fromEntries(Object.entries(r).map(([key,x])=>[key,{won:x.won,status:x.status,...(x.won?{seconds:x.seconds,tailSeconds:x.tailSeconds,maxNoChoiceSeconds:x.maxNoChoiceSeconds,maxForcedLowProductivitySeconds:x.maxForcedLowProductivitySeconds,completedLinkedHeldSlotSeconds:x.completedLinkedHeldSlotSeconds}:{refusedAt:x.refusedAt,reason:x.reason})}]));
const levels=[];
for(let i=0;i<30;i++){
 const prev=compareRhythm(a.levels[i],ac[i].actions),next=compareRhythm(b.levels[i],bc[i].actions);
 levels.push({id:i+1,changed:b.changedLevels.includes(i+1),before:{title:a.levels[i].title,...shape(a.levels[i]),rhythm:brief(prev)},after:{title:b.levels[i].title,...shape(b.levels[i]),rhythm:brief(next)},bots:q.levels[i].bots,heldOut:q.levels[i].heldOut});
 console.log('L'+(i+1),b.changedLevels.includes(i+1)?'changed':'control',JSON.stringify({before:brief(prev).brisk1x,after:brief(next).brisk1x}));
}
const observed=refs.levels.map(l=>{
 const quotas=Object.values(l.tray).flat().map(s=>Number(s.match(/^.([0-9]+)/)[1])).sort((a,b)=>a-b);
 return {level:l.level,title:l.title,difficulty:l.difficulty,coloredCellsKnown:l.board.rows.join('').replace(/[.?]/g,'').length,unresolvedCells:l.board.rows.join('').split('?').length-1,lots:quotas.length,totalQuota:quotas.reduce((n,q)=>n+q,0),quotaMinusRecognizedCells:quotas.reduce((n,q)=>n+q,0)-l.board.rows.join('').replace(/[.?]/g,'').length,medianQuota:quotas[Math.floor(quotas.length/2)],maxQuota:Math.max(...quotas),rounds:(l.rounds||[]).map(r=>{
 const times=r.taps.map(t=>t.t),gaps=times.slice(1).map((t,i)=>t-times[i]).sort((a,b)=>a-b);
 return {attempt:r.attempt,outcome:r.outcome,seconds:r.end-r.start,taps:r.taps.length,afterLastTapSeconds:r.end-times.at(-1),medianInterTapSeconds:gaps[Math.floor(gaps.length/2)],maximumInterTapSeconds:Math.max(...gaps)};})};
});
const result={baseHash:a.hash,catalogHash:b.hash,changed:b.changedLevels,assumptions:['Model wall time at stated reaction delay1200ms; not measured player time.','Patient and brisk schedules are different policies. Failed brisk schedules have no finished duration.','Productivity refers to occupied places, excludes released pods carrying work outside places.','Legal choice is not proof of a strategically safe move.','Observed competitor gaps include both thinking and forced waiting; these sources cannot separate them exactly.'],observedColony:observed,levels};
fs.writeFileSync('product/audits/pilot-v9-comparison-2026-10-24.json',JSON.stringify(result,null,2)+'\n');

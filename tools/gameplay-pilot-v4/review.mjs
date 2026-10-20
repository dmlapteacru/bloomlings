/* Review a recorded v4 export. Never changes frozen pilot/game definitions. */
import assert from 'node:assert/strict';
import {readFile,writeFile,mkdir,copyFile} from 'node:fs/promises';
import {createHash} from 'node:crypto';
import {resolve,dirname} from 'node:path';import {fileURLToPath} from 'node:url';
import R from './rules.cjs';import T from './timeline.cjs';import {solve,work} from './search.mjs';
const ROOT=resolve(dirname(fileURLToPath(import.meta.url)),'../..'),arg=(k,d)=>process.argv.find(s=>s.startsWith(`--${k}=`))?.slice(k.length+3)||d;
const source=arg('results',null);assert(source,'--results=/path/to/export.json required');const OUT=resolve(ROOT,arg('out','product/playtests/pilot-v4-owner-2026-10-20'));
const raw=await readFile(source),d=JSON.parse(raw),catalog=JSON.parse(await readFile(resolve(ROOT,'content/pilot-v4/levels.json'))),levels=catalog.levels;
const sha=x=>createHash('sha256').update(x).digest('hex'),near=(a,b,label)=>assert(Math.abs(a-b)<.002,`${label}: ${a} != ${b}`);
assert.equal(d.format,'bloomlings-pilot-results-4');assert.equal(d.catalogHash,catalog.hash);assert.equal(d.catalogHash,sha(JSON.stringify(levels)));
for(const [key,file] of [['rulesHash','rules.cjs'],['timelineHash','timeline.cjs'],['paletteHash','palette.cjs']])assert.equal(d[key],sha(await readFile(resolve(ROOT,'tools/gameplay-pilot-v4',file))),key);
assert.deepEqual(d.presentation.trips,T.PROFILE);
const all=[],errors=[],unknowns=[],speedEvents=[];let accepted=0,rejected=0,plans=0,bucketsOK=0;const reasons={};
for(const a of d.attempts){
 const l=levels.find(l=>l.id===a.level);assert(l);assert.equal(a.capacity,5);assert.deepEqual(a.config.trips,T.PROFILE);
 let t=new T.Timeline(R.create(l,a.capacity)),at=0,lastMs=0,expectedPlans=[],firstFatal=null,states=[];
 const physicalSamples=[];const decisions=[];
 for(const e of a.inputEvents){
  assert(e.atMs>=lastMs);lastMs=e.atMs;assert(e.modelTimeMs>=t.now-.002,'Model time regressed');t.advance(Math.max(0,e.modelTimeMs-t.now));near(t.now,e.modelTimeMs,'Model time');
  near(t.free,5-e.visualHeld,'Physical free');assert.deepEqual(Array.from(t.shown.slots),e.visualSlots);assert.deepEqual(Array.from(t.shown.slots,p=>p<0?null:t.shown.remaining[p]),e.visualCounts);
  assert.equal(t.hasChoice,e.hasChoice);assert.equal(t.pendingCells,e.pendingCells);assert.equal(t.jobs.size,e.pendingTrips);
  const before=R.clone(t.state),result=t.commit(e.action);assert.equal(result.accepted,e.accepted);assert.equal(result.reason||null,e.reason);
  if(e.accepted){assert.deepEqual(e.action,a.commands[at++]);accepted++;expectedPlans.push(...t.lastPlans);
   const r=solve(t.state,{budget:100000});if(r.winning===null)unknowns.push({attempt:a.id,afterAction:at});
   if(firstFatal===null&&r.winning===false){
    const alternatives=R.legal(before).filter(x=>JSON.stringify(x)!==JSON.stringify(e.action)).map(action=>({action,result:solve(R.step(before,action).state,{budget:100000}),clears:work(before)-work(R.step(before,action).state)}));
    const winning=alternatives.filter(x=>x.result.winning===true);assert(winning.length,'No proven winning predecessor');
    firstFatal={action:at,chosen:e.action,immediateClears:work(before)-work(t.state),statusAfter:t.state.status,atMs:e.atMs,winningAlternatives:winning.map(x=>({action:x.action,immediateClears:x.clears,continuation:x.result.actions})),unknownAlternatives:alternatives.filter(x=>x.result.winning===null).map(x=>x.action),scope:'Full actual state, not proof that consequences were inferable from the visible queue.'};
   }
   const active=Array.from(t.state.slots).filter(p=>p>=0);states.push({logicalHeld:active.length,finishedPairedHalves:active.filter(p=>t.state.remaining[p]===0).length,logicalStatus:t.state.status});
  }else{rejected++;reasons[e.reason]=(reasons[e.reason]||0)+1;}
  physicalSamples.push({logicalHeld:e.logicalHeld,visualHeld:e.visualHeld,hasChoice:e.hasChoice});
 }
 assert.equal(at,a.commands.length);assert.equal(expectedPlans.length,a.tripPlans.length);for(let i=0;i<expectedPlans.length;i++){assert.equal(expectedPlans[i].length,a.tripPlans[i].length);for(let j=0;j<expectedPlans[i].length;j++)near(expectedPlans[i][j],a.tripPlans[i][j],`Trip ${i}/${j}`);}plans+=expectedPlans.length;
 t.advance(1e9);assert.equal(t.state.status.toLowerCase(),a.logicalOutcome);assert.equal(t.status.toLowerCase(),a.outcome);assert(!t.pending);R.assertAccounting(t.state);near(Object.values(a.timeBuckets).reduce((x,y)=>x+y,0),a.elapsedMs,'Bucket sum');bucketsOK++;
 let end=0;for(const interval of a.intervals){near(interval.fromMs,end,'Interval continuity');assert(interval.toMs>=interval.fromMs);end=interval.toMs;}near(end,a.elapsedMs,'Intervals sum');
 const autoAt=a.commands.findIndex((_,i)=>{let s=R.create(l,a.capacity);for(const c of a.commands.slice(0,i+1))s=R.step(s,c).state;return R.queueEmpty(s);});assert.equal(autoAt<0?null:autoAt+1,a.autoFastAtAction);
 for(const e of a.telemetry)if(e.type==='manual-speed')speedEvents.push({attempt:a.id,level:a.level,atMs:e.atMs,fast:e.fast});
 const clearable=l.rowsBottomFirst.join('').replace(/[.#]/g,'').length;
 all.push({id:a.id,level:a.level,title:l.title,difficulty:l.difficulty,outcome:a.outcome,elapsedMs:a.elapsedMs,actions:a.commands.length,rejectedInputs:a.inputEvents.filter(e=>!e.accepted).length,timeBuckets:a.timeBuckets,workActiveMs:a.workActiveMs,autoFastAtMs:a.autoFastAtMs,autoTailShare:a.timeBuckets.autoTail/a.elapsedMs,blockedOrTailShare:(a.timeBuckets.blockedWorking+a.timeBuckets.autoTail)/a.elapsedMs,ratings:{difficulty:a.rating,interest:a.interest,art:a.art,planning:a.planning,pace:a.pace},note:a.note,peakLogicalHeld:Math.max(0,...states.map(s=>s.logicalHeld)),peakPhysicalHeld:Math.max(0,...physicalSamples.map(s=>s.visualHeld)),finishedPairHalfActionStates:states.filter(s=>s.finishedPairedHalves>0).length,boardCells:clearable,tripCount:expectedPlans.length,firstFatal,waitIntervals5s:a.intervals.filter(i=>['blockedWorking','autoTail'].includes(i.kind)&&i.toMs-i.fromMs>=5000)});
}
const wins=all.filter(a=>a.outcome==='won'),losses=all.filter(a=>['jammed','stuck'].includes(a.outcome)),sum=(xs,fn)=>xs.reduce((n,x)=>n+fn(x),0),counts=xs=>Object.fromEntries([...new Set(xs.filter(x=>x!==null))].map(x=>[x,xs.filter(y=>y===x).length])),median=xs=>{xs=xs.slice().sort((a,b)=>a-b);return xs.length%2?xs[(xs.length-1)/2]:(xs[xs.length/2-1]+xs[xs.length/2])/2;};
const perLevel=levels.map(l=>{const aa=all.filter(a=>a.level===l.id),win=aa.filter(a=>a.outcome==='won').at(-1);return {level:l.id,title:l.title,difficulty:l.difficulty,attempts:aa.length,losses:aa.filter(a=>['jammed','stuck'].includes(a.outcome)).length,recordedMs:sum(aa,a=>a.elapsedMs),win,notes:aa.filter(a=>a.note).map(a=>({id:a.id,note:a.note}))};});
const summary={participantCount:1,attempts:all.length,wins:wins.length,losses:losses.length,unfinished:all.filter(a=>!['won','jammed','stuck'].includes(a.outcome)).length,firstAttemptWins:perLevel.filter(l=>l.attempts===1&&l.win).length,recordedMs:sum(all,a=>a.elapsedMs),winningRecordedMs:sum(wins,a=>a.elapsedMs),medianWinningRecordedMs:median(wins.map(a=>a.elapsedMs)),winningTimeBuckets:Object.fromEntries(Object.keys(wins[0].timeBuckets).map(k=>[k,sum(wins,a=>a.timeBuckets[k])])),ratings:Object.fromEntries(Object.keys(wins[0].ratings).map(k=>[k,counts(wins.map(a=>a.ratings[k]))])),funLevels:wins.filter(a=>a.ratings.interest==='fun').map(a=>a.level),boringLevels:wins.filter(a=>a.ratings.interest==='boring').map(a=>a.level),unrecognizedArt:wins.filter(a=>a.ratings.art==='no').map(a=>a.level),planningLevels:wins.filter(a=>a.ratings.planning==='planned').map(a=>a.level),speedEvents};
const report={format:'bloomlings-pilot-review-4',sourceSha256:sha(raw),catalogHash:catalog.hash,rulesHash:d.rulesHash,timelineHash:d.timelineHash,paletteHash:d.paletteHash,validation:{attemptsReplayed:all.length,acceptedActions:accepted,rejectedInputs:rejected,rejectionReasons:reasons,tripPlansReproduced:plans,physicalInputsAndClockReproduced:true,outcomesMatch:true,automaticSpeedTriggersMatch:true,timeBucketSumsMatch:bucketsOK===all.length,unknownSearches:unknowns},summary,levels:perLevel,attempts:all,limits:['One returning player, not audience win rates or retention. Prior familiarity affects retained L15/L22.','Manual 2x enabled during L9 and persists afterwards; raw seconds are not normalized 1x comparisons.','clockTick(true) on visibility return may include hidden-tab dwell as active time; export has no visibility log to subtract it exactly. Times are recorded browser values, not verified pure active play or thinking.','choice means any legally admissible input, not proof that a useful/winning input existed. blockedWorking + autoTail is structurally unavailable input, not automatically boredom.','Full-state fatal-branch diagnosis does not prove the player could predict hidden-depth consequences. Difficulty, interest, art and pace are separate ratings; pace supplied for only five levels.']};
assert.equal(unknowns.length,0,'Incomplete full-state search');await mkdir(OUT,{recursive:true});await copyFile(source,resolve(OUT,'results.json'));await writeFile(resolve(OUT,'review.json'),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify({validation:report.validation,summary},null,2));

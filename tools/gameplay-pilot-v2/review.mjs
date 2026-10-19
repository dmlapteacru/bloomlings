// Read-only replay of the owner's v2 export. Does not modify pilot rules/data.
import assert from 'node:assert/strict';
import {readFile,writeFile,mkdir} from 'node:fs/promises';
import {createHash} from 'node:crypto';
import {resolve,dirname} from 'node:path';
import {fileURLToPath} from 'node:url';
import R from './rules.cjs';
import {solve,held,work} from './search.mjs';
import {select,rng} from './policies.mjs';

const ROOT=resolve(dirname(fileURLToPath(import.meta.url)),'../..');
const arg=(key,fallback)=>process.argv.find(a=>a.startsWith(`--${key}=`))?.slice(key.length+3)||fallback;
const input=arg('results'),OUT=resolve(arg('out','/tmp/bloomlings-pilot-v2-review'));
assert(input,'Pass --results=/path/to/export.json');
const raw=await readFile(input),results=JSON.parse(raw);
const data=JSON.parse(await readFile(resolve(ROOT,'content/pilot-v2/levels.json')));
const v1=JSON.parse(await readFile(resolve(ROOT,'product/playtests/pilot-v1-owner-2026-10-19/review.json')));
const sha=value=>createHash('sha256').update(value).digest('hex');
assert.equal(results.format,'bloomlings-pilot-results-2');
assert.equal(results.pilotVersion,data.version);assert.equal(results.catalogHash,data.hash);
assert.equal(data.hash,sha(JSON.stringify(data.levels)));
assert.equal(results.rulesHash,sha(await readFile(resolve(ROOT,'tools/gameplay-pilot-v2/rules.cjs'))));
assert.equal(new Set(results.attempts.map(a=>a.id)).size,results.attempts.length);
const levels=new Map(data.levels.map(l=>[l.id,l]));
const median=values=>{const s=values.slice().sort((a,b)=>a-b);return s.length%2?s[(s.length-1)/2]:(s[s.length/2-1]+s[s.length/2])/2;};
const active=s=>Array.from(s.slots).filter(p=>p>=0).map(p=>({pod:p,color:R.view(s).colours[p],remaining:s.remaining[p],group:s.level.pods[p].group||null}));
const actionInfo=(s,a)=>a.type==='bridge'?{type:'bridge',choice:a.choice}:({type:'tap',pod:a.pod,members:s.meta.groups[a.pod].map(p=>({pod:p,color:R.view(s).colours[p],count:s.remaining[p],group:s.level.pods[p].group||null})),column:s.level.tray.findIndex((stack,c)=>stack[s.heads[c]]===a.pod)});

function replay(record){
 const level=levels.get(record.level);assert(level);let state=R.create(level),auto=null;const trace=[];
 for(const [i,action] of record.commands.entries()){
  const sig=R.signature(state),move=R.step(state,action,true);assert(move.accepted,`Invalid attempt ${record.id} turn ${i+1}`);assert.equal(R.signature(state),sig);
  const next=move.state;R.assertAccounting(next);
  const group=action.type==='tap'?state.meta.groups[action.pod]:[];
  const unknown=group.filter(p=>!state.revealed[p]);
  trace.push({turn:i+1,action,publicAction:actionInfo(state,action),occupiedBefore:held(state),occupiedAfter:held(next),cleared:work(state)-work(next),slotsBefore:active(state),slotsAfter:active(next),completedHalvesHeld:Array.from(next.slots).filter(p=>p>=0&&next.remaining[p]===0).length,pair:group.length===2,unknownSelection:unknown.length>0,reveals:unknown.map(p=>({pod:p,color:next.level.pods[p].color})),queueLeft:level.tray.reduce((n,s,c)=>n+s.length-next.heads[c],0),remaining:work(next),bridgeReadyBefore:R.bridgeChoices(state).map(b=>b.choice)});
  state=next;if(auto===null&&R.queueEmpty(state))auto=i+1;
 }
 assert.equal(state.status.toLowerCase(),record.outcome,`Outcome mismatch attempt ${record.id}`);
 assert.equal(record.autoFastAtAction,auto,`Wrong automatic ×2 point attempt ${record.id}`);
 return {id:record.id,level:record.level,outcome:record.outcome,rating:record.rating,art:record.art,planning:record.planning,note:record.note||'',elapsedMs:record.elapsedMs,moves:trace.length,peakHeld:Math.max(0,...trace.map(t=>t.occupiedAfter)),decisionsWithOneFree:trace.filter(t=>t.occupiedBefore===4).length,decisionsWithTwoFree:trace.filter(t=>t.occupiedBefore===3).length,turnsWithCompletedHalfHeld:trace.filter(t=>t.completedHalvesHeld>0).length,turnsWithoutClearing:trace.filter(t=>t.cleared===0).length,pairSelections:trace.filter(t=>t.pair).length,unknownSelections:trace.filter(t=>t.unknownSelection).length,autoFastAtAction:auto,trace};
}

const attempts=results.attempts.map(replay),wins=attempts.filter(a=>a.outcome==='won'),losses=attempts.filter(a=>a.outcome!=='won');
assert.equal(wins.length,30);assert.equal(new Set(wins.map(a=>a.level)).size,30);
const count=(list,field,value)=>list.filter(a=>a[field]===value).length;
const summary={attempts:attempts.length,completedLevels:30,jammed:count(attempts,'outcome','jammed'),stuck:count(attempts,'outcome','stuck'),firstAttemptWins:data.levels.filter(l=>attempts.find(a=>a.level===l.id).outcome==='won').length,ratings:Object.fromEntries(['easy','good','hard'].map(k=>[k,count(wins,'rating',k)])),recognition:Object.fromEntries(['yes','no'].map(k=>[k,count(wins,'art',k)])),planning:Object.fromEntries(['colour-only','planned'].map(k=>[k,count(wins,'planning',k)])),medianWinningElapsedSeconds:median(wins.map(a=>a.elapsedMs/1000)),totalReportedActiveSeconds:attempts.reduce((n,a)=>n+a.elapsedMs/1000,0),winningLevelsAtMostTwoHeld:wins.filter(a=>a.peakHeld<=2).length,winningLevelsReachingFourHeld:wins.filter(a=>a.peakHeld>=4).map(a=>a.level),winningDecisionsWithOneFree:wins.reduce((n,a)=>n+a.decisionsWithOneFree,0),winningDecisionsWithTwoFree:wins.reduce((n,a)=>n+a.decisionsWithTwoFree,0),winningMoves:wins.reduce((n,a)=>n+a.moves,0),acceptedMoves:attempts.reduce((n,a)=>n+a.moves,0),winningTurnsWithCompletedHalfHeld:wins.reduce((n,a)=>n+a.turnsWithCompletedHalfHeld,0),autoFastMatchesEveryAttempt:true};
console.log('Human replay',JSON.stringify(summary));

// Diagnose where the actual recorded state first becomes unwinnable. This is
// full-information counterfactual analysis, never a model of player knowledge.
const failures=[];
for(const record of results.attempts.filter(a=>a.outcome!=='won')){
 let s=R.create(levels.get(record.level));const prefixes=[];let fatal=null;
 for(const [i,a] of record.commands.entries()){
  const after=R.step(s,a).state,proof=solve(after,{budget:50000});prefixes.push({turn:i+1,winning:proof.winning,nodes:proof.nodes});
  if(proof.winning===false){
   const alternatives=R.legal(s).map(action=>{const child=R.step(s,action).state,p=solve(child,{budget:50000});return {action,publicAction:actionInfo(s,action),cleared:work(s)-work(child),occupiedAfter:held(child),winning:p.winning,nodes:p.nodes,winningActions:p.winning?p.actions:[]};});
   fatal={attempt:record.id,level:record.level,firstProvenFatalTurn:i+1,earlierUnknown:prefixes.slice(0,-1).some(p=>p.winning===null),chosen:a,chosenPublic:actionInfo(s,a),hiddenChoice:a.type==='tap'&&s.meta.groups[a.pod].some(p=>!s.revealed[p]),slotsBefore:active(s),slotsAfter:active(after),cleared:work(s)-work(after),remainingMovesUntilDisplayedLoss:record.commands.length-i-1,alternatives,prefixes};break;
  }
  s=after;
 }
 assert(fatal,`Missing diagnostic for attempt ${record.id}`);assert(fatal.alternatives.some(a=>a.winning===true)||fatal.earlierUnknown,'Fatal frontier has no proven winning alternative');failures.push(fatal);
 console.log(`L${record.level} loss #${record.id}: first proven fatal turn ${fatal.firstProvenFatalTurn}, hidden ${fatal.hiddenChoice}, delay ${fatal.remainingMovesUntilDisplayedLoss}`);
}

const bridges=[];
for(const record of results.attempts.filter(a=>levels.get(a.level).hero)){
 let s=R.create(levels.get(record.level));
 for(const [i,a] of record.commands.entries()){
  if(a.type==='bridge'){
   const choices=R.bridgeChoices(s).map(choice=>{
    const child=R.step(s,{type:'bridge',choice:choice.choice}).state,p=solve(child,{budget:50000});let same=child,refused=false;
    for(const next of record.commands.slice(i+1)){const move=R.step(same,next);if(!move.accepted){refused=true;break;}same=move.state;}
    return {choice:choice.choice,winning:p.winning,nodes:p.nodes,sameRemainingOrderWins:!refused&&same.status==='Won'};
   });bridges.push({attempt:record.id,level:record.level,outcome:record.outcome,turn:i+1,selected:a.choice,occupiedBefore:held(s),choices});
  }
  s=R.step(s,a).state;
 }
}

function withCapacity(level,capacity){const s=R.create(level);s.slots=new Int16Array(capacity).fill(-1);assert(R.legal(s).length>0);return s;}
const capacity=[];
for(const level of data.levels.filter(l=>l.id<=10||l.difficulty==='Hard')){
 const human=results.attempts.find(a=>a.level===level.id&&a.outcome==='won');const checks=[];
 for(const places of [3,4,5]){
  let s=withCapacity(level,places),sameAccepted=true,firstRefused=null;
  for(const [i,a] of human.commands.entries()){const move=R.step(s,a);if(!move.accepted){sameAccepted=false;firstRefused=i+1;break;}s=move.state;}
  const exact=solve(withCapacity(level,places),{budget:50000});
  const greedy={};for(const mode of ['leftmost','largest','smallest']){let state=withCapacity(level,places),taps=0;while(state.status==='Playing'){const a=select(state,mode,rng(1),taps);state=R.step(state,a).state;if(a.type==='tap')taps++;}greedy[mode]=state.status==='Won';}
  checks.push({places,sameHumanOrderWins:sameAccepted&&s.status==='Won',firstRefused,winning:exact.winning,nodes:exact.nodes,greedy});
 }
 capacity.push({level:level.id,difficulty:level.difficulty,checks});console.log('Capacity',JSON.stringify(capacity.at(-1)));
}

const perLevel=data.levels.map(l=>{const played=attempts.filter(a=>a.level===l.id),won=played.find(a=>a.outcome==='won');return {level:l.id,title:l.title,difficulty:l.difficulty,attempts:played.length,losses:played.length-1,finalRating:won.rating,art:won.art,planning:won.planning,winningPeakHeld:won.peakHeld,totalActiveSeconds:played.reduce((n,a)=>n+a.elapsedMs/1000,0),winningActiveSeconds:won.elapsedMs/1000,notes:played.filter(a=>a.note).map(a=>({attempt:a.id,note:a.note}))};});
const comparison={v1:{attempts:v1.summary.attempts,losses:v1.summary.jammed,firstAttemptWins:v1.summary.firstCompletedAttemptWins,ratings:v1.summary.ratings,recognition:v1.summary.recognition,medianWinningElapsedSeconds:v1.summary.medianWinningElapsedSeconds,winningDecisionsWithOneFree:v1.summary.winningDecisionsWithOneFree,winningDecisionsWithTwoFree:v1.summary.winningDecisionsWithTwoFree,winningLevelsAtMostTwoHeld:v1.summary.winningLevelsAtMostTwoHeld,winningMoves:v1.summary.winningMoves},v2:summary};
const report={format:'bloomlings-pilot-human-review-2',pilotVersion:data.version,catalogHash:data.hash,rulesHash:results.rulesHash,sourceHash:sha(raw),participants:1,summary,comparison,perLevel,failures,bridges,capacity,attempts,limits:['One returning participant, not population difficulty or retention evidence.','Per-level ratings and recognition use winning attempts; loss feedback is retained separately.','Active time is client-reported, excludes some background/modal time and does not measure pure thought time.','Occupancy is after full automatic settle; it excludes temporarily working teams and rapid overlapping commits.','v2 UI blocks all further taps during visual settle; Unity instead checks physically empty on-screen slots.','Fatal-prefix and reduced-capacity solver results use the actual full state, including hidden colours. They do not establish what the player could know.','null means budget exhausted, never proven loss.','Capacity counterfactuals use the same fixed queues/art and instantaneous settle; they are not redesigned or playable 3/4-slot variants.']};
await mkdir(OUT,{recursive:true});await writeFile(resolve(OUT,'results.json'),raw);await writeFile(resolve(OUT,'review.json'),JSON.stringify(report,null,2)+'\n');console.log(`Saved verified review: ${OUT}`);

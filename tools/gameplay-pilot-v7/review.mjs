// Read-only analysis of the owner's v7 recording; never changes pilot data.
import fs from 'node:fs';import assert from 'node:assert/strict';import crypto from 'node:crypto';
import R from './rules.cjs';import T from './timeline.cjs';
const source='content/pilot-v7/Bloomlings-pilot-v7-results-2026-10-21.json';
const out='product/playtests/pilot-v7-owner-2026-10-21';
const raw=fs.readFileSync(source),d=JSON.parse(raw),catalog=JSON.parse(fs.readFileSync('content/pilot-v7/levels.json'));
const hash=x=>crypto.createHash('sha256').update(x).digest('hex'),near=(a,b,label)=>assert(Math.abs(a-b)<.002,`${label}: ${a} vs ${b}`);
assert.equal(d.format,'bloomlings-pilot-results-7');assert.equal(d.catalogHash,catalog.hash);
for(const [key,file]of [['rulesHash','rules.cjs'],['timelineHash','timeline.cjs'],['paletteHash','palette.cjs'],['clockHash','clock.cjs']])assert.equal(d[key],hash(fs.readFileSync('tools/gameplay-pilot-v7/'+file)));
assert.deepEqual(d.presentation.trips,T.PROFILE);
const attempts=[],reasons={},speedEvents=[];let accepted=0,rejected=0,tripCount=0,terminal=0;
for(const a of d.attempts){
 const l=catalog.levels[a.level-1],t=new T.Timeline(R.create(l)),plans=[],reveals=[],hiddenTaps=[];let at=0,firstExhaustion=null;
 const total=l.pods.reduce((n,p)=>n+p.count,0),collect=()=>plans.push(...t.lastPlans);
 for(const e of a.inputEvents){
  assert(e.modelTimeMs>=t.now-.002);t.advance(Math.max(0,e.modelTimeMs-t.now));collect();near(t.now,e.modelTimeMs,'input time');
  assert.deepEqual([...t.state.slots],e.visualSlots);assert.deepEqual([...t.state.slots].map(p=>p<0?null:t.state.remaining[p]),e.visualCounts);
  assert.equal(5-t.free,e.visualHeld);assert.equal(t.pendingCells,e.pendingCells);assert.equal(t.jobs.size,e.pendingTrips);assert.equal(t.hasChoice,e.hasChoice);
  if(e.accepted&&e.action.type==='tap'&&l.pods[e.action.pod].mystery)hiddenTaps.push({pod:e.action.pod,atMs:e.atMs,modelMs:t.now,revealedBeforeTap:Boolean(t.state.revealed[e.action.pod]),freeBefore:t.free,clearedFraction:t.processedCells/total,sourceLeft:l.tray.reduce((n,s,c)=>n+s.length-t.state.heads[c],0)});
  const result=t.commit(e.action);assert.equal(result.accepted,e.accepted);assert.equal(result.reason||null,e.reason);
  if(result.accepted){
   const cmd=a.commands[at++];near(cmd.modelTimeMs,t.now,'command time');const {modelTimeMs,...action}=cmd;assert.deepEqual(action,e.action);accepted++;collect();
   for(const event of result.events.filter(x=>x.type==='reveal'))reveals.push({pod:event.pod,color:l.pods[event.pod].color,count:l.pods[event.pod].count,afterAction:at,atMs:e.atMs,modelMs:t.now,clearedFraction:t.processedCells/total,freeAfter:t.free,sourceLeft:l.tray.reduce((n,s,c)=>n+s.length-t.state.heads[c],0)});
   if(firstExhaustion===null&&R.queueEmpty(t.state))firstExhaustion=at;
  }else{rejected++;reasons[e.reason]=(reasons[e.reason]||0)+1;}
 }
 assert.equal(at,a.commands.length);assert.equal(firstExhaustion,a.autoFastAtAction);
 if(['won','jammed','stuck'].includes(a.outcome)){t.advance(1e9);collect();assert.equal(t.status.toLowerCase(),a.outcome);terminal++;}
 else{const endModel=Math.max(t.now,...a.telemetry.map(e=>e.modelTimeMs||0));t.advance(endModel-t.now);collect();}
 // A pagehide may leave an abandoned run with telemetry after its last input.
 assert.equal(plans.length,a.tripPlans.length,`A${a.id} trip count`);
 for(let i=0;i<plans.length;i++)for(let j=0;j<plans[i].length;j++)near(plans[i][j],a.tripPlans[i][j],`A${a.id} trip ${i}/${j}`);
 tripCount+=plans.length;near(Object.values(a.timeBuckets).reduce((n,x)=>n+x,0),a.elapsedMs,'bucket sum');
 let end=0;for(const interval of a.intervals){near(end,interval.fromMs,'interval continuity');assert(interval.toMs>=interval.fromMs);end=interval.toMs;}near(end,a.elapsedMs,'interval sum');
 for(const e of a.telemetry)if(e.type==='manual-speed')speedEvents.push({attempt:a.id,level:a.level,atMs:e.atMs,manualFast:e.manualFast,effectiveSpeed:e.effectiveSpeed});
 attempts.push({id:a.id,level:a.level,outcome:a.outcome,elapsedMs:a.elapsedMs,actions:at,ratings:{difficulty:a.rating,interest:a.interest,art:a.art,planning:a.planning,pace:a.pace},note:a.note,timeBuckets:a.timeBuckets,initialFast:a.config.initialSettings.manualFast,reveals,hiddenTaps});
}
const counts=xs=>xs.reduce((r,x)=>(r[x]=(r[x]||0)+1,r),{}),sum=(xs,f)=>xs.reduce((n,x)=>n+f(x),0),wins=attempts.filter(a=>a.outcome==='won'),times=wins.map(a=>a.elapsedMs).sort((a,b)=>a-b);
const hidden=catalog.levels.filter(l=>l.pods.some(p=>p.mystery)).map(l=>{const aa=attempts.filter(a=>a.level===l.id),win=aa.find(a=>a.outcome==='won');return {level:l.id,title:l.title,hard:l.difficulty==='Hard',hiddenCount:l.pods.filter(p=>p.mystery).length,hiddenWork:l.pods.filter(p=>p.mystery).reduce((n,p)=>n+p.count,0),totalWork:l.pods.reduce((n,p)=>n+p.count,0),attempts:aa.length,ratings:win.ratings,reveals:win.reveals,taps:win.hiddenTaps};});
const report={source,sourceSHA256:hash(raw),catalogHash:catalog.hash,validation:{replayedAttempts:attempts.length,terminalOutcomesMatched:terminal,acceptedInputs:accepted,rejectedInputs:rejected,rejectionReasons:reasons,tripPlansMatched:tripCount,physicalInputStatesMatched:true,timeBucketSumsMatch:true},summary:{participantCount:1,attempts:attempts.length,outcomes:counts(attempts.map(a=>a.outcome)),firstAttemptWins:catalog.levels.filter(l=>attempts.find(a=>a.level===l.id)?.outcome==='won').length,recordedMinutes:sum(attempts,a=>a.elapsedMs)/60000,winningMinutes:sum(wins,a=>a.elapsedMs)/60000,medianWinSeconds:(times[14]+times[15])/2000,ratings:Object.fromEntries(Object.keys(wins[0].ratings).map(k=>[k,counts(wins.map(a=>a.ratings[k]))])),fun:wins.filter(a=>a.ratings.interest==='fun').map(a=>a.level),boring:wins.filter(a=>a.ratings.interest==='boring').map(a=>a.level),unrecognized:wins.filter(a=>a.ratings.art==='no').map(a=>a.level),winningTimeSeconds:Object.fromEntries(Object.keys(wins[0].timeBuckets).map(k=>[k,sum(wins,a=>a.timeBuckets[k])/1000])),speedEvents},hiddenLevels:hidden,attempts,limits:['One returning owner, not audience win rates. Familiarity especially affects L22.','Recorded active time mixes 1x and 2x; not normalized durations or measured thinking time.','Hidden slots reveal on reaching the top, before selection. Reveal timing is descriptive, not proof that a choice mattered.','Abandoned attempts are checked through recorded telemetry, not counted as losses.','logicalOutcome is a stale legacy field in this export; actual terminal outcomes are replayed from the authoritative timeline.']};
fs.mkdirSync(out,{recursive:true});fs.writeFileSync(out+'/review.json',JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify({validation:report.validation,summary:report.summary,hiddenLevels:hidden},null,2));

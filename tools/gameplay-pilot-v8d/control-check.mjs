// Replay the owner's real, timed inputs on every unchanged v7 control in v8D. v8D's auto-tail (no departure gap once
// the queue is empty) is new, so trips and places are compared up to the moment the tail starts; outcomes in full.
import fs from 'node:fs';import assert from 'node:assert/strict';import crypto from 'node:crypto';
import R from './rules.cjs';import T from './timeline.cjs';
const source='content/pilot-v7/Bloomlings-pilot-v7-results-2026-10-21.json',raw=fs.readFileSync(source),d=JSON.parse(raw),old=JSON.parse(fs.readFileSync('content/pilot-v7/levels.json')).levels,data=JSON.parse(fs.readFileSync('content/pilot-v8d/levels.json'));
const controls=data.levels.filter(l=>JSON.stringify(l)===JSON.stringify(old[l.id-1])).map(l=>l.id),near=(a,b)=>assert(Math.abs(a-b)<.002);
let attempts=0,inputs=0,trips=0,tails=0;
for(const a of d.attempts.filter(a=>controls.includes(a.level))){
 const t=new T.Timeline(R.create(data.levels[a.level-1])),plans=[];let tailAt=Infinity;
 for(const e of a.inputEvents){t.advance(Math.max(0,e.modelTimeMs-t.now));plans.push(...t.lastPlans);near(t.now,e.modelTimeMs);if(!t.tail){assert.deepEqual([...t.state.slots],e.visualSlots);assert.deepEqual([...t.state.slots].map(p=>p<0?null:t.state.remaining[p]),e.visualCounts);}const result=t.commit(e.action);if(t.tail&&tailAt===Infinity)tailAt=t.now;assert.equal(result.accepted,e.accepted);assert.equal(result.reason||null,e.reason);if(result.accepted)plans.push(...t.lastPlans);inputs++;}
 if(['won','jammed','stuck'].includes(a.outcome)){t.advance(1e9);assert.equal(t.status.toLowerCase(),a.outcome);}else t.advance(Math.max(t.now,...a.telemetry.map(e=>e.modelTimeMs||0))-t.now);
 plans.push(...t.lastPlans);const before=plans.filter(p=>p[3]<tailAt-1e-7),recorded=a.tripPlans.filter(p=>p[3]<tailAt-1e-7);assert.equal(before.length,recorded.length);for(let i=0;i<before.length;i++)for(let j=0;j<before[i].length;j++)near(before[i][j],recorded[i][j]);if(tailAt<Infinity){tails++;assert.equal(plans.length,a.tripPlans.length,'Same trips, sooner');assert(t.now<=Math.max(...a.tripPlans.map(p=>p[7]))+1e-7||!['won'].includes(a.outcome),'The tail never ends later');}trips+=before.length;attempts++;
}
const report={passed:true,catalogHash:data.hash,sourceSHA256:crypto.createHash('sha256').update(raw).digest('hex'),unchangedControls:controls,realOwnerAttemptsReplayed:attempts,inputsMatched:inputs,tripPlansMatchedBeforeTail:trips,attemptsWithFasterTail:tails,limits:['Only unchanged levels are compared. This is a deterministic regression, not a new human playtest.']};
fs.mkdirSync('/tmp/bloomlings-pilot-v8d-verification',{recursive:true});fs.writeFileSync('/tmp/bloomlings-pilot-v8d-verification/controls.json',JSON.stringify(report,null,2)+'\n');console.log(report);

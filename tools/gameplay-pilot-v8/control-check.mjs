// Replay the owner's real, timed inputs on every unchanged v7 control in v8.
import fs from 'node:fs';import assert from 'node:assert/strict';import crypto from 'node:crypto';
import R from './rules.cjs';import T from './timeline.cjs';
const source='content/pilot-v7/Bloomlings-pilot-v7-results-2026-10-21.json',raw=fs.readFileSync(source),d=JSON.parse(raw),old=JSON.parse(fs.readFileSync('content/pilot-v7/levels.json')).levels,data=JSON.parse(fs.readFileSync('content/pilot-v8/levels.json'));
const controls=data.levels.filter(l=>JSON.stringify(l)===JSON.stringify(old[l.id-1])).map(l=>l.id),near=(a,b)=>assert(Math.abs(a-b)<.002);
let attempts=0,inputs=0,trips=0;
for(const a of d.attempts.filter(a=>controls.includes(a.level))){
 const t=new T.Timeline(R.create(data.levels[a.level-1])),plans=[];
 for(const e of a.inputEvents){t.advance(Math.max(0,e.modelTimeMs-t.now));plans.push(...t.lastPlans);near(t.now,e.modelTimeMs);assert.deepEqual([...t.state.slots],e.visualSlots);assert.deepEqual([...t.state.slots].map(p=>p<0?null:t.state.remaining[p]),e.visualCounts);const result=t.commit(e.action);assert.equal(result.accepted,e.accepted);assert.equal(result.reason||null,e.reason);if(result.accepted)plans.push(...t.lastPlans);inputs++;}
 if(['won','jammed','stuck'].includes(a.outcome)){t.advance(1e9);assert.equal(t.status.toLowerCase(),a.outcome);}else t.advance(Math.max(t.now,...a.telemetry.map(e=>e.modelTimeMs||0))-t.now);
 plans.push(...t.lastPlans);assert.equal(plans.length,a.tripPlans.length);for(let i=0;i<plans.length;i++)for(let j=0;j<plans[i].length;j++)near(plans[i][j],a.tripPlans[i][j]);trips+=plans.length;attempts++;
}
const report={passed:true,catalogHash:data.hash,sourceSHA256:crypto.createHash('sha256').update(raw).digest('hex'),unchangedControls:controls,realOwnerAttemptsReplayed:attempts,inputsMatched:inputs,tripPlansMatched:trips,limits:['Only unchanged levels are compared. This is a deterministic regression, not a new human playtest.']};
fs.mkdirSync('/tmp/bloomlings-pilot-v8-verification',{recursive:true});fs.writeFileSync('/tmp/bloomlings-pilot-v8-verification/controls.json',JSON.stringify(report,null,2)+'\n');console.log(report);

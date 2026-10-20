import assert from 'node:assert/strict';
import {readFile,writeFile,mkdir} from 'node:fs/promises';
import R from './rules.cjs';import T from './timeline.cjs';
const dataset=JSON.parse(await readFile(new URL('../../content/pilot-v4/levels.json',import.meta.url))),certs=JSON.parse(await readFile(new URL('../../content/pilot-v4/certificates.json',import.meta.url)));
let paths=0,commands=0,restores=0,busyRefusals=0,fullWorkingRows=0,readyHalves=0,bridgeVisibilityWaits=0,routeChecks=0,moving=0,returns=0;const timings=[];
function sameWork(t){assert.deepEqual(t.shown.board,t.state.board);assert.deepEqual(t.shown.remaining,t.state.remaining);assert(!t.pending);t.assert();}
function restored(t){const snapshot=JSON.parse(JSON.stringify(t.snapshot())),copy=new T.Timeline(t.state,snapshot);assert.deepEqual(copy.snapshot(),snapshot);restores++;return copy;}
function jobs(t){const workers=new Set();for(const j of t.jobs.values()){
 assert(j.start<=j.arrive&&j.arrive<j.clear&&j.clear<j.done);assert(t.state.meta.entries.includes(j.route[0]));assert.equal(j.route.at(-1),j.cell);
 for(let i=1;i<j.route.length;i++){assert(t.state.meta.neighbours.subarray(j.route[i-1]*4,j.route[i-1]*4+4).includes(j.route[i]));assert(t.ready[j.route[i-1]]<=j.start);}
 routeChecks++;
 if(j.start<=t.now&&t.now<j.done){const key=j.pod+':'+j.worker;assert(!workers.has(key),'Worker double booked');workers.add(key);assert(t.shown.slots.includes(j.pod));if(t.now>=j.clear)returns++;else moving++;}
 }assert(t.couriers.length<=10);}
function run(level,actions,burst){let t=new T.Timeline(R.create(level)),at=0,wall=0,lastRestored=-1;
 for(let guard=0;guard<100000&&(at<actions.length||t.pending);guard++){
  assert(guard<99999,'Work queue stalled');
  if(at<actions.length){const a=actions[at],reason=t.check(a);if(!reason){assert(t.commit(a).accepted);commands++;at++;jobs(t);t=restored(t);assert(R.topPods(t.state).every(p=>t.shown.revealed[p]));if(!burst){const dt=t.steps.at(-1)?.at-t.now||0;t.advance(dt);wall+=dt;sameWork(t);}}
   else{assert(t.pending,`No future work to unblock ${reason}`);const before=JSON.stringify(t.snapshot());assert(!t.commit(a).accepted);assert.equal(JSON.stringify(t.snapshot()),before);if(reason==='visual-places-busy')busyRefusals++;if(reason==='visible-bridge-not-ready')bridgeVisibilityWaits++;}}
  if(t.pending){if(t.free===0)fullWorkingRows++;const dt=Math.max(1,Math.min(103,t.steps[t.cursor].at-t.now)),speed=R.queueEmpty(t.state)?2:1;t.advance(dt/speed,speed);wall+=dt/speed;if(guard%101===0){jobs(t);if(lastRestored!==at){t=restored(t);lastRestored=at;}}}
  if(t.shown.slots.some(p=>p>=0&&t.shown.remaining[p]===0))readyHalves++;
 }assert.equal(at,actions.length);assert.equal(t.status,'Won');sameWork(t);paths++;return Math.round(wall);
}
for(const l of dataset.levels){const c=certs.find(c=>c.id===l.id),serialMs=run(l,c.actions,false),burstMs=run(l,c.actions,true);timings.push({level:l.id,serialMs,burstMs,note:'Automated zero-think reference routes; burst includes auto2x tail, serial waits at 1x after each command. Not human timings.'});}
assert(busyRefusals&&fullWorkingRows&&readyHalves&&bridgeVisibilityWaits&&moving&&returns);
const fixture={id:9001,w:7,h:8,entries:[3],capacity:3,rowsBottomFirst:['GGGGGGG','CCCCCCC','BBBBBBB',...Array(5).fill('.......')],pods:[{color:'G',count:7,group:'A'},{color:'B',count:7,group:'A'},{color:'C',count:7,mystery:true}],tray:[[0,2],[1]]};
let t=new T.Timeline(R.create(fixture));assert.equal(t.shown.revealed[2],0);t.commit({type:'tap',pod:1});assert.equal(t.shown.revealed[2],1);assert.equal(t.free,1);t.advance(100000);assert.equal(t.shown.remaining[0],0);assert(t.shown.slots.includes(0));assert.equal(t.free,1,'Finished half released early');
t.commit({type:'tap',pod:2});assert.equal(t.state.status,'Won');assert.equal(t.status,'Working');assert.equal(t.free,0);
const half=restored(t);t.advance(89);half.advance(44);half.advance(45);assert.deepEqual(t.snapshot(),half.snapshot());t.advance(100000);assert.equal(t.status,'Won');assert.equal(t.free,3);
let a=new T.Timeline(R.create(fixture)),b=new T.Timeline(R.create(fixture));a.commit({type:'tap',pod:0});b.commit({type:'tap',pod:0});a.advance(90,1);b.advance(45,2);assert.deepEqual(a.snapshot(),b.snapshot());assert.throws(()=>a.advance(1,3));assert.throws(()=>new T.Timeline(a.state,{...a.snapshot(),logicalSignature:'stale'}));
// A zero count alone must not release a place before the return leg finishes.
const single={id:9003,w:7,h:8,entries:[3],capacity:5,rowsBottomFirst:['...V...',...Array(7).fill('.......')],pods:[{color:'V',count:1}],tray:[[0],[]]};t=new T.Timeline(R.create(single));t.commit({type:'tap',pod:0});const job=[...t.jobs.values()][0];t.advance(job.clear);assert.equal(t.pendingCells,0);assert.equal(t.free,4);assert.equal(t.status,'Working');assert(t.couriers[0].carrying);t.advance(job.done-job.clear);assert.equal(t.free,5);assert.equal(t.status,'Won');
const bridge={id:9002,w:7,h:8,entries:[3],capacity:5,rowsBottomFirst:['GGGGGGG','#######',...Array(6).fill('BBBBBBB')],pods:[{color:'B',count:8},{color:'B',count:8},{color:'B',count:8},{color:'B',count:8},{color:'B',count:10},{color:'G',count:7}],tray:[[0],[1],[2],[3],[4],[5]],hero:{choices:[{cell:9,anchor:2},{cell:11,anchor:4}]}};
t=new T.Timeline(R.create(bridge));t.commit({type:'tap',pod:5});assert.equal(t.check({type:'bridge',choice:0}),'visible-bridge-not-ready');t.advance(100000);for(const pod of [0,1,2,3,4])assert(t.commit({type:'tap',pod}).accepted);assert.equal(t.free,0);assert(t.commit({type:'bridge',choice:1}).accepted);t.advance(100000);assert.equal(t.status,'Won');
const out='/tmp/bloomlings-pilot-v4-verification';await mkdir(out,{recursive:true});const report={passed:true,profile:T.PROFILE,winningTimelinePaths:paths,acceptedCommands:commands,checkpointsRestored:restores,routeChecks,busyRefusals,fullWorkingRowStates:fullWorkingRows,completedHalfStates:readyHalves,bridgeVisibilityWaits,movingCourierStates:moving,returningCourierStates:returns,maxCouriers:10,frameChunkingInvariant:true,twoTimesSameWork:true,pendingWinDelayedUntilReturn:true,pairedPlacesHeld:true,bridgeRescuesFullRow:true,newTopRevealsImmediately:true,staleCheckpointRejected:true,timings};await writeFile(out+'/timeline.json',JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify({...report,timings:undefined}));

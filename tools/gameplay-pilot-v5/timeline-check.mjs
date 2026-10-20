import assert from 'node:assert/strict';
import {readFile,writeFile,mkdir} from 'node:fs/promises';
import R from './rules.cjs';import T from './timeline.cjs';
const dataset=JSON.parse(await readFile(new URL('../../content/pilot-v5/levels.json',import.meta.url))),certs=JSON.parse(await readFile(new URL('../../content/pilot-v5/certificates.json',import.meta.url)));
let paths=0,commands=0,restores=0,busyRefusals=0,fullWorkingRows=0,readyHalves=0,bridgeVisibilityWaits=0,routeChecks=0,moving=0,returns=0;const timings=[];
function sameWork(t){assert.deepEqual(t.shown.board,t.state.board);assert.deepEqual(t.shown.remaining,t.state.remaining);assert(!t.pending);t.assert();}
function restored(t){const snapshot=JSON.parse(JSON.stringify(t.snapshot())),copy=new T.Timeline(t.state,snapshot);assert.deepEqual(copy.snapshot(),snapshot);restores++;return copy;}
let maxCouriers=0,maxSamePod=0,overlappingDepartures=0;
function jobs(t){const targets=new Set(),flying=new Int16Array(t.state.level.pods.length),reserved=new Map();
 for(const j of t.jobs.values()){
  if(j.start===null){assert(!targets.has(j.cell));targets.add(j.cell);continue;}
  assert(j.assigned<=j.start&&j.start<=j.arrive&&j.arrive<j.clear&&j.clear<j.done);assert(t.state.meta.entries.includes(j.route[0]));assert.equal(j.route.at(-1),j.cell);
  for(let i=1;i<j.route.length;i++){assert(t.state.meta.neighbours.subarray(j.route[i-1]*4,j.route[i-1]*4+4).includes(j.route[i]));assert(t.ready[j.route[i-1]]<=j.start);}
  if(j.clear>t.now){assert(!targets.has(j.cell));targets.add(j.cell);}
  routeChecks++;
  if(j.start<=t.now&&t.now<j.done){flying[j.pod]++;assert(t.shown.slots.includes(j.pod));if(t.now<j.clear)reserved.set(j.cell,j.id);if(t.now>=j.clear)returns++;else moving++;}
 }
 assert.deepEqual(flying,t.inFlight);for(let i=0;i<t.reserved.length;i++)assert.equal(t.reserved[i],reserved.get(i)??-1);
 for(const cell of R.reach(t.shown).targets){const id=t.byCell.get(cell),job=id===undefined?null:t.jobs.get(id);if(job?.start===null&&t.reserved[cell]<0)assert(t.departReady[job.pod]>t.now+1e-7,'Reachable target left idle despite launch credit');}
 maxCouriers=Math.max(maxCouriers,t.couriers.length);maxSamePod=Math.max(maxSamePod,...flying);
}
function run(level,actions,burst){let t=new T.Timeline(R.create(level)),at=0,wall=0,lastRestored=-1,lastRefusal=null;
 for(let guard=0;guard<100000&&(at<actions.length||t.pending);guard++){
  assert(guard<99999,'Work queue stalled');
  if(at<actions.length){const a=actions[at],reason=t.check(a);if(!reason){assert(t.commit(a).accepted);commands++;at++;jobs(t);t=restored(t);assert(R.topPods(t.state).every(p=>t.shown.revealed[p]));if(!burst){const before=t.now;t.advance(1e9);wall+=t.now-before;sameWork(t);}}
   else{assert(t.pending,`No future work to unblock ${reason}`);const key=at+':'+reason+':'+t.free;if(lastRefusal!==key){const before=JSON.stringify(t.snapshot());assert(!t.commit(a).accepted);assert.equal(JSON.stringify(t.snapshot()),before);lastRefusal=key;}if(reason==='visual-places-busy')busyRefusals++;if(reason==='visible-bridge-not-ready')bridgeVisibilityWaits++;}}
  if(t.pending){if(t.free===0)fullWorkingRows++;const dt=Math.max(1,Math.min(103,t.steps[t.cursor].at-t.now)),speed=R.queueEmpty(t.state)?2:1;t.advance(dt/speed,speed);wall+=dt/speed;if(guard%101===0){jobs(t);if(lastRestored!==at){t=restored(t);lastRestored=at;}}}
  if(t.shown.slots.some(p=>p>=0&&t.shown.remaining[p]===0))readyHalves++;
 }assert.equal(at,actions.length);assert.equal(t.status,'Won');sameWork(t);paths++;return Math.round(wall);
}
for(const l of dataset.levels){const c=certs.find(c=>c.id===l.id),serialMs=run(l,c.actions,false),burstMs=run(l,c.actions,true);timings.push({level:l.id,serialMs,burstMs,note:'Automated zero-think reference routes; burst includes auto2x tail, serial waits at 1x after each command. Not human timings.'});}
assert(busyRefusals&&fullWorkingRows&&readyHalves&&bridgeVisibilityWaits&&moving&&returns);assert(maxSamePod>2);
const fixture={id:9001,w:7,h:8,entries:[3],capacity:3,rowsBottomFirst:['GGGGGGG','CCCCCCC','BBBBBBB',...Array(5).fill('.......')],pods:[{color:'G',count:7,group:'A'},{color:'B',count:7,group:'A'},{color:'C',count:7,mystery:true}],tray:[[0,2],[1]]};
let t=new T.Timeline(R.create(fixture));assert.equal(t.shown.revealed[2],0);t.commit({type:'tap',pod:1});assert.equal(t.shown.revealed[2],1);assert.equal(t.free,1);t.advance(100000);assert.equal(t.shown.remaining[0],0);assert(t.shown.slots.includes(0));assert.equal(t.free,1,'Finished half released early');
t.commit({type:'tap',pod:2});assert.equal(t.state.status,'Won');assert.equal(t.status,'Working');assert.equal(t.free,0);
const half=restored(t);t.advance(89);half.advance(44);half.advance(45);assert.deepEqual(t.snapshot(),half.snapshot());t.advance(100000);assert.equal(t.status,'Won');assert.equal(t.free,3);
let a=new T.Timeline(R.create(fixture)),b=new T.Timeline(R.create(fixture));a.commit({type:'tap',pod:0});b.commit({type:'tap',pod:0});a.advance(90,1);b.advance(45,2);assert.deepEqual(a.snapshot(),b.snapshot());assert.throws(()=>a.advance(1,3));assert.throws(()=>new T.Timeline(a.state,{...a.snapshot(),logicalSignature:'stale'}));
// A zero count alone must not release a place before the return leg finishes.
const single={id:9003,w:7,h:8,entries:[3],capacity:5,rowsBottomFirst:['...V...',...Array(7).fill('.......')],pods:[{color:'V',count:1}],tray:[[0],[]]};t=new T.Timeline(R.create(single));t.commit({type:'tap',pod:0});const job=[...t.jobs.values()][0];t.advance(job.clear);assert.equal(t.pendingCells,0);assert.equal(t.free,4);assert.equal(t.status,'Working');assert(t.couriers[0].carrying);t.advance(job.done-job.clear);assert.equal(t.free,5);assert.equal(t.status,'Won');
const bridge={id:9002,w:7,h:8,entries:[3],capacity:5,rowsBottomFirst:['GGGGGGG','#######',...Array(6).fill('BBBBBBB')],pods:[{color:'B',count:8},{color:'B',count:8},{color:'B',count:8},{color:'B',count:8},{color:'B',count:10},{color:'G',count:7}],tray:[[0],[1],[2],[3],[4],[5]],hero:{choices:[{cell:9,anchor:2},{cell:11,anchor:4}]}};
t=new T.Timeline(R.create(bridge));t.commit({type:'tap',pod:5});assert.equal(t.check({type:'bridge',choice:0}),'visible-bridge-not-ready');t.advance(100000);for(const pod of [0,1,2,3,4])assert(t.commit({type:'tap',pod}).accepted);assert.equal(t.free,0);assert(t.commit({type:'bridge',choice:1}).accepted);t.advance(100000);assert.equal(t.status,'Won');
// A reachable wide front launches >2 before the first courier returns.
const wide={id:9010,w:7,h:8,entries:[3],capacity:5,rowsBottomFirst:['.......','GGGGGGG',...Array(6).fill('.......')],pods:[{color:'G',count:7}],tray:[[0],[]]};
t=new T.Timeline(R.create(wide));t.commit({type:'tap',pod:0});t.advance(255);assert.equal(t.inFlight[0],4);assert.equal(t.shown.remaining[0],7);assert.equal(t.reserved.filter(n=>n>=0).length,4);jobs(t);
const streamPlans=t.lastPlans;assert(streamPlans[2][4]<streamPlans[0][7]);overlappingDepartures++;
const narrow={...wide,id:9011,rowsBottomFirst:Array(7).fill('###G###').concat('###.###')};
t=new T.Timeline(R.create(narrow));t.commit({type:'tap',pod:0});const narrowPlans=t.lastPlans.slice();while(t.pending){t.advance(Math.max(0,t.steps[t.cursor].at-t.now));narrowPlans.push(...t.lastPlans);assert(t.reserved.filter(n=>n>=0).length<=1);jobs(t);}for(let i=1;i<narrowPlans.length;i++)assert(narrowPlans[i][4]>=narrowPlans[i-1][6]);assert.equal(t.status,'Won');
const competition={...wide,id:9012,pods:[{color:'G',count:4},{color:'G',count:3}],tray:[[0],[1]]};
t=new T.Timeline(R.create(competition));t.commit({type:'tap',pod:0});t.commit({type:'tap',pod:1});assert.equal(t.free,3);assert.equal(new Set([...t.jobs.values()].map(j=>j.cell)).size,7);t.advance(100000);assert.equal(t.status,'Won');assert.equal(t.processedCells,7);
const out='/tmp/bloomlings-pilot-v5-verification';await mkdir(out,{recursive:true});const report={passed:true,profile:T.PROFILE,winningTimelinePaths:paths,acceptedCommands:commands,checkpointsRestored:restores,routeChecks,busyRefusals,fullWorkingRowStates:fullWorkingRows,completedHalfStates:readyHalves,bridgeVisibilityWaits,movingCourierStates:moving,returningCourierStates:returns,maxCouriers,maxSamePod,streamExceedsTwoWorkers:true,wideFrontFourBeforeFirstClear:true,narrowFrontOnlyOneReserved:true,sameMaterialPodsNoDoubleAssignment:true,frameChunkingInvariant:true,twoTimesSameWork:true,pendingWinDelayedUntilReturn:true,pairedPlacesHeld:true,bridgeRescuesFullRow:true,newTopRevealsImmediately:true,staleCheckpointRejected:true,timings};await writeFile(out+'/timeline.json',JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify({...report,timings:undefined}));

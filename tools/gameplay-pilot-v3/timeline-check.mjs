import assert from 'node:assert/strict';
import {readFile,writeFile,mkdir} from 'node:fs/promises';
import R from './rules.cjs';
import T from './timeline.cjs';
const dataset=JSON.parse(await readFile(new URL('../../content/pilot-v3/levels.json',import.meta.url))),certs=JSON.parse(await readFile(new URL('../../content/pilot-v3/certificates.json',import.meta.url)));
let paths=0,commands=0,restores=0,busyRefusals=0,fullWorkingRows=0,readyHalves=0,bridgeVisibilityWaits=0;
function sameWork(t){assert.deepEqual(t.shown.board,t.state.board);assert.deepEqual(t.shown.remaining,t.state.remaining);assert(!t.pending);t.assert();}
function restored(t){const snapshot=JSON.parse(JSON.stringify(t.snapshot())),copy=new T.Timeline(t.state,snapshot);assert.deepEqual(copy.snapshot(),snapshot);restores++;return copy;}
function run(level,capacity,actions,burst){
 let t=new T.Timeline(R.create(level,capacity)),at=0;
 for(let guard=0;guard<20000&&(at<actions.length||t.pending);guard++){
  assert(guard<19999,'Work queue stalled');
  if(at<actions.length){
   const a=actions[at],reason=t.check(a);
   if(!reason){assert(t.commit(a).accepted);commands++;at++;t=restored(t);assert(R.topPods(t.state).every(p=>t.shown.revealed[p]));if(!burst){t.advance(1000000);sameWork(t);}}
   else{
    assert(t.pending,`No future work to unblock ${reason}`);const before=JSON.stringify(t.snapshot()),logical=R.signature(t.state);assert(!t.commit(a).accepted);assert.equal(JSON.stringify(t.snapshot()),before);assert.equal(R.signature(t.state),logical);
    if(reason==='visual-places-busy')busyRefusals++;if(reason==='visible-bridge-not-ready')bridgeVisibilityWaits++;
   }
  }
  if(t.pending){assert.equal(t.status,'Working');if(t.free===0)fullWorkingRows++;t.advance(burst?53:1000000,R.queueEmpty(t.state)?2:1);if(guard%37===0)t=restored(t);}
  if(t.shown.slots.some(p=>p>=0&&t.shown.remaining[p]===0))readyHalves++;
 }
 assert.equal(at,actions.length);assert.equal(t.status,'Won');sameWork(t);paths++;
}
for(const l of dataset.levels){const c=certs.find(c=>c.id===l.id);for(const capacity of l.id<=9?[3,4]:[5]){const actions=l.id<=9?c.capacities[capacity].actions:c.actions;run(l,capacity,actions,false);run(l,capacity,actions,true);}}
assert(busyRefusals>0);assert(fullWorkingRows>0);assert(readyHalves>0);assert(bridgeVisibilityWaits>0);

const fixture={id:9001,w:7,h:8,entries:[3],capacity:3,rowsBottomFirst:['GGGGGGG','CCCCCCC','BBBBBBB',...Array(5).fill('.......')],pods:[{color:'G',count:7,group:'A'},{color:'B',count:7,group:'A'},{color:'C',count:7,mystery:true}],tray:[[0,2],[1]]};
let t=new T.Timeline(R.create(fixture));assert.equal(t.shown.revealed[2],0);t.commit({type:'tap',pod:1});assert.equal(t.shown.revealed[2],1,'New top not revealed immediately');assert.equal(t.free,1);
assert.equal(t.state.remaining[0],0);assert.equal(t.shown.remaining[0],7);t.advance(7*T.WORK_MS);assert.equal(t.shown.remaining[0],0);assert(t.shown.slots.includes(0));assert.equal(t.free,1,'Finished half released early');
t.commit({type:'tap',pod:2});assert.equal(t.state.status,'Won');assert.equal(t.status,'Working','Premature win');assert.equal(t.free,0);
const half=restored(t);t.advance(89);half.advance(44);half.advance(45);assert.deepEqual(t.snapshot(),half.snapshot(),'Frame chunking changed the outcome');t.advance(10000);assert.equal(t.status,'Won');assert.equal(t.free,3);
let a=new T.Timeline(R.create(fixture)),b=new T.Timeline(R.create(fixture));a.commit({type:'tap',pod:0});b.commit({type:'tap',pod:0});a.advance(90,1);b.advance(45,2);assert.deepEqual(a.snapshot(),b.snapshot(),'×2 changed allocation');
assert.throws(()=>new T.Timeline(a.state,{...a.snapshot(),logicalSignature:'stale'}));

const bridge={id:9002,w:7,h:8,entries:[3],capacity:5,rowsBottomFirst:['GGGGGGG','#######',...Array(6).fill('BBBBBBB')],pods:[{color:'B',count:8},{color:'B',count:8},{color:'B',count:8},{color:'B',count:8},{color:'B',count:10},{color:'G',count:7}],tray:[[0],[1],[2],[3],[4],[5]],hero:{choices:[{cell:9,anchor:2},{cell:11,anchor:4}]}};
t=new T.Timeline(R.create(bridge));t.commit({type:'tap',pod:5});assert.equal(t.check({type:'bridge',choice:0}),'visible-bridge-not-ready');t.advance(1000);for(const pod of [0,1,2,3,4])assert(t.commit({type:'tap',pod}).accepted);assert.equal(t.free,0);assert.equal(t.status,'Playing');assert(t.commit({type:'bridge',choice:1}).accepted);assert.equal(t.status,'Working');t.advance(100000);assert.equal(t.status,'Won');
const out='/tmp/bloomlings-pilot-v3-verification';await mkdir(out,{recursive:true});const report={passed:true,winningTimelinePaths:paths,acceptedCommands:commands,checkpointsRestored:restores,busyRefusals,fullWorkingRowStates:fullWorkingRows,completedHalfStates:readyHalves,bridgeVisibilityWaits,frameChunkingInvariant:true,twoTimesSameWork:true,pendingWinDelayed:true,pairedPlacesHeld:true,bridgeRescuesFullRow:true,newTopRevealsImmediately:true,staleCheckpointRejected:true};await writeFile(out+'/timeline.json',JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify(report));

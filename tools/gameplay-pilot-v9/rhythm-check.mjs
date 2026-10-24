import assert from 'node:assert/strict';import {replayRhythm} from './rhythm.mjs';
const grid=['...G...',...Array(7).fill('.......')];
const one={id:901,w:7,h:8,capacity:5,entries:[3],rowsBottomFirst:grid,pods:[{color:'G',count:1}],tray:[[0],[],[]]};
const solo=replayRhythm(one,[{type:'tap',pod:0}],{thinkMs:0});
assert(solo.won);assert.equal(solo.seconds,.46);assert.equal(solo.tailSeconds,.46,'last command at t=0 still has a tail');
assert.equal(solo.completedLinkedHeldSlotSeconds,0,'ordinary release lag is not a stalled linked half');
assert.equal(solo.zeroQuotaHeldSlotSeconds,.12);
const two={...one,rowsBottomFirst:['..GG...',...Array(7).fill('.......')],entries:[2,3],pods:[{color:'G',count:1},{color:'G',count:1}],tray:[[0,1],[],[]]};
for(const [speed,seconds] of [[1,4.01],[2,3.435]]){
 const r=replayRhythm(two,[{type:'tap',pod:0},{type:'tap',pod:1}],{manualSpeed:speed});
 assert(r.won);assert.equal(r.seconds,seconds,'thinking is wall time, ordinary work uses selected speed, tail uses 2.5');
 const sum=a=>a.reduce((s,n)=>s+n,0);assert(Math.abs(sum(r.occupationSeconds)-r.seconds)<.003,'occupancy includes idle thinking');
 assert(Math.abs(sum(r.productiveSeconds)-r.seconds)<.003,'productive occupied places include idle intervals');
 assert(Math.abs(sum(Object.values(r.buckets))-r.seconds)<.003,'buckets partition wall time');
}
const bad=replayRhythm(two,[{type:'tap',pod:1}]);assert(!bad.won);assert.equal(bad.reason,'not-exposed');
assert(!('tailSeconds' in bad),'an invalid schedule cannot acquire a fake finished duration');
console.log('rhythm checks: OK (known 1x/2x/2.5 timing, zero-time command, idle occupancy, failed schedule)');

// A reserve strategy can deliberately withhold a legal tap; report that waiting explicitly.
const many={...one,rowsBottomFirst:['..GGGGG',...Array(7).fill('.......')],entries:[2,3,4,5,6],pods:Array.from({length:5},()=>({color:'G',count:1})),tray:[[0,3],[1,4],[2]]};
const reserved=replayRhythm(many,[0,1,2,3,4].map(pod=>({type:'tap',pod})),{schedule:'reserve',thinkMs:0});
assert(reserved.won);assert(reserved.reserveGuardSeconds>0);assert(reserved.maxReserveGuardSeconds>0);assert(reserved.reserveGuardSeconds<=reserved.seconds);
assert.equal(replayRhythm(many,[0,1,2,3,4].map(pod=>({type:'tap',pod})),{schedule:'brisk',thinkMs:0}).reserveGuardSeconds,0);
console.log('Reserve guard waiting is explicit: OK');

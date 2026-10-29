import test from 'node:test';
import assert from 'node:assert/strict';
import {fairness,fairnessObservation,observationPolicy,randomTest,greedyBeaten} from '../measure.mjs';
import {engine} from '../engine.mjs';

const level=()=>({id:1,w:7,h:8,entries:[3],capacity:5,
 rowsBottomFirst:['.......','.GGGGG.','.GGGGG.','.GGGGG.','.GGGGG.','.GGGG..','.......','.......'],
 pods:[1,1,1,1,2,3,4,5,6].map((count,i)=>({color:'G',count,...(i>=3?{mystery:'depth'}:{})})),
 tray:[[0,3,4],[1,5,6],[2,7,8]]});
const route=[0,3,4,1,5,6,2,7,8].map(pod=>({type:'tap',pod}));

test('fairness proves every one of 720 distinct hidden worlds, not the first 24',async()=>{
 const f=await fairness(level(),route);assert.equal(f.fair,true);assert.equal(f.complete,true);assert.equal(f.worlds,720);
 assert.equal(f.checked,720);assert.equal(f.proofVersion,2);assert.match(f.definitionHash,/^[a-f0-9]{64}$/);
 const limited=await fairness(level(),route,{worldLimit:24});assert.equal(limited.fair,false);assert.equal(limited.complete,false);assert.equal(limited.unknown,1);
});

test('missing solution and exhausted policy budget never certify fairness',async()=>{
 assert.equal((await fairness(level(),null)).fair,false);
 const f=await fairness(level(),route,{budget:0});assert.equal(f.fair,false);assert.equal(f.complete,false);assert.equal(f.unknown,1);
});

test('hidden counts as well as materials are absent from proof observations',async()=>{
 const {R}=await engine(),a=level(),b=level();[b.pods[3],b.pods[4]]=[b.pods[4],b.pods[3]];
 const x=R.create(a),y=R.create(b);assert.notEqual(R.observation(x),R.observation(y),'raw view leaks remaining counts');
 assert.equal(fairnessObservation(R,x),fairnessObservation(R,y));
 const sx=R.step(x,{type:'tap',pod:0}).state,sy=R.step(y,{type:'tap',pod:0}).state;
 assert.notEqual(fairnessObservation(R,sx),fairnessObservation(R,sy),'visible revealed count may guide a later decision');
});

// Four worlds: the first revelation identifies a pair, but the later choice still depends on an unknown second bit.
// Every world has a full-information winning route. Only the optional second safe revelation makes a fair policy.
function ambiguousWorlds(canReveal){const R={
 signature:s=>JSON.stringify(s),
 legal:s=>s.status!=='Playing'?[]:s.stage===0?[{type:'first'}]:s.stage===1?
  [{type:'choose',bit:0},{type:'choose',bit:1},...(canReveal?[{type:'second'}]:[])]:[{type:'choose',bit:0},{type:'choose',bit:1}],
 step:(s,a)=>({accepted:true,state:a.type==='first'?{...s,stage:1}:a.type==='second'?{...s,stage:2}:{...s,status:a.bit===s.second?'Won':'Jammed'}})
 };return {R,initial:[0,1,2,3].map(world=>({world,state:{stage:0,first:world>>1,second:world%2,status:'Playing'}})),
 observe:s=>JSON.stringify([s.stage,s.stage? s.first:null,s.stage===2?s.second:null,s.status])};}

test('fairness does not stop at the first revelation or use full knowledge for later choices',()=>{
 const x=ambiguousWorlds(false),p=observationPolicy(x.R,x.initial,{observe:x.observe});assert.equal(p.win,false);assert.equal(p.complete,true);
 const y=ambiguousWorlds(true),q=observationPolicy(y.R,y.initial,{observe:y.observe});assert.equal(q.win,true);assert.equal(q.complete,true);
});

test('random test retains reproducible seeds and rejects invalid sampling parameters',async()=>{
 const a=await randomTest(level(),{seed:19}),b=await randomTest(level(),{seed:19});assert.deepEqual(a,b);assert.equal(a.seedBase,19);assert.equal(a.wins,a.runs);
 await assert.rejects(randomTest(level(),{seed:NaN}),/integer seed/);
 await assert.rejects(randomTest(level(),{first:0}),/first/);
});

test('partial Hard diagnostics cannot certify the stricter Super Hard class',()=>{
 const hard={'most-now|left':false,'most-now|right':false};assert.equal(greedyBeaten(hard,'Hard'),true);
 assert.equal(greedyBeaten(hard,'Super Hard'),false);assert.equal(greedyBeaten({},'Hard'),false);
 assert.equal(greedyBeaten({...hard,'fast-release|left':false,'fast-release|right':false},'Super Hard'),true);
});

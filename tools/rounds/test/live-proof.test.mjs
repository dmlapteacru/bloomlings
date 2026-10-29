import test from 'node:test';
import assert from 'node:assert/strict';
import {bridgeLiveVerdict,gateCutCertificate} from '../live-proof.mjs';
import {engine} from '../engine.mjs';

// One corridor, a chosen bridge, then a D gate in front of six G cells.
// All six G pods precede D; none can dispatch before D is admitted.
function corridor({gateFirst=false,near=0,linked=false}={}){
 const w=7,h=8,rows=Array.from({length:h},()=>Array(w).fill('#'));
 rows[0][3]='.';rows[2][3]='D';for(let y=3;y<h;y++)rows[y][3]='G';rows[7][4]='G';
 for(let x=0;x<near;x++)rows[0][x]='G';
 const pods=Array.from({length:6+near},()=>({color:'G',count:1}));pods.push({color:'D',count:1});
 const gate=pods.length-1;
 const level={id:1,w,h,rowsBottomFirst:rows.map(r=>r.join('')),entries:[3],capacity:5,pods,
  tray:[gateFirst?[gate,...Array.from({length:gate},(_,i)=>i)]:Array.from({length:gate+1},(_,i)=>i),[]],
  hero:{id:'sprig-bridge',choices:[{cell:w+3,anchor:3,label:'test bridge'}]}};
 if(linked){pods[0].group='pair';pods[1].group='pair';level.tray=[[0,...Array.from({length:gate-2},(_,i)=>i+2),gate],[1]];}
 return level;
}

test('gate cut proves live loss even with free bridge, free time and optimistic clearing',async()=>{
 const level=corridor(),E=await engine();assert.deepEqual(E.R.validate(level),[]);
 const result=await bridgeLiveVerdict(level,0);
 assert.equal(result.win,false);assert.equal(result.method,'gate-slot-cut');assert.equal(result.certificate.gate,'D');
 assert.equal(result.certificate.attempts.length,1);
 assert.equal(result.certificate.attempts[0].heldAtLeast,6);
 assert.ok(result.certificate.attempts.every(a=>a.heldAtLeast+a.placesNeeded>level.capacity));
});

test('an exposed gate is not declared losing and the patient witness wins live',async()=>{
 const level=corridor({gateFirst:true});assert.equal(gateCutCertificate(level,0),null);
 const result=await bridgeLiveVerdict(level,0);assert.equal(result.win,true);assert.equal(result.method,'live-replayed-witness');
 const E=await engine(),t=new E.T.Timeline(E.R.create(level));
 for(const action of result.route){t.advance(1e9);assert.equal(t.commit(action).accepted,true);}
 t.advance(1e9);assert.equal(t.state.status,'Won');
});

test('search budget exhaustion remains unknown, never a live loss',async()=>{
 const result=await bridgeLiveVerdict(corridor({gateFirst:true}),0,0);
 assert.equal(result.win,null);assert.equal(result.method,'patient-budget-exhausted');
});

test('a linked predecessor includes both members in the cut lower bound',async()=>{
 const level=corridor({linked:true}),E=await engine();assert.deepEqual(E.R.validate(level),[]);
 const cert=gateCutCertificate(level,0);assert.equal(cert.gate,'D');
 assert.deepEqual(cert.attempts[0].prerequisites,[0,1,2,3,4,5]);assert.equal(cert.attempts[0].heldAtLeast,6);
});

test('reachable work can release predecessor pods, so a deep gate alone is not a loss',async()=>{
 const level=corridor({near:3}),gate=level.pods.length-1;
 level.tray=[[0,1,2,3,4,5,gate,6,7,8],[]];
 assert.equal(gateCutCertificate(level,0),null);
 const result=await bridgeLiveVerdict(level,0);assert.equal(result.win,true);
});

test('a winning witness keeps the original bridge index after restricted search',async()=>{
 const level=corridor({gateFirst:true}),winning=level.hero.choices[0];
 level.hero.choices=[{cell:level.w+4,anchor:level.w+3,label:'closed approach'},winning];
 const result=await bridgeLiveVerdict(level,1);assert.equal(result.win,true);
 assert.deepEqual(result.route.filter(a=>a.type==='bridge'),[{type:'bridge',choice:1}]);
});

test('a bridge with permanently disconnected work has a topology certificate',async()=>{
 const level=corridor({gateFirst:true});level.rowsBottomFirst[7]='#G#GG##';level.pods.push({color:'G',count:1});level.tray[1].push(level.pods.length-1);
 const result=await bridgeLiveVerdict(level,0);assert.equal(result.win,false);assert.equal(result.method,'disconnected-work');
 assert.ok(result.certificate.cells.includes(7*level.w+1));
});

test('invalid inputs are unknown rather than evidence of impossibility',async()=>{
 assert.equal((await bridgeLiveVerdict(corridor(),1)).win,null);
 const level=corridor();level.pods[0].count++;
 const result=await bridgeLiveVerdict(level,0);assert.equal(result.win,null);assert.equal(result.method,'invalid-level');
});

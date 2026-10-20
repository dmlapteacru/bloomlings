import assert from 'node:assert/strict';import fs from 'node:fs';import crypto from 'node:crypto';
import R from './rules.cjs';import T from './timeline.cjs';import {hiddenWorlds} from './search.mjs';import {rng} from './policies.mjs';
const {Timeline,PROFILE}=T,out='/tmp/bloomlings-pilot-v6-verification';fs.mkdirSync(out,{recursive:true});
let routes=0,moves=0,restores=0,randomRuns=0,frameChecks=0,fairPolicies=0,liveClears=0;const timings=[];
function clone(t){return new Timeline(R.create(t.state.level),t.snapshot());}
function stepEvent(t){t.advance(t.steps[0].at-t.now);}
function drain(t){t.advance(1e9);assert(!t.pending);t.assert();}
function inspect(t){
 t.assert();for(const j of t.jobs.values())if(j.start===t.now){for(const c of j.route.slice(0,-1))assert.equal(t.state.board[c],R.OPEN);}
 assert(R.topPods(t.state).every(p=>t.state.revealed[p]));
 const startBoard=t.state.meta.board.reduce((n,c)=>n+(c>=0),0);assert.equal(startBoard-t.state.board.reduce((n,c)=>n+(c>=0),0),t.processedCells);
 if(t.pending)assert.equal(t.state.status,'Playing','Premature win/jam');
}
function execute(l,actions){let t=new Timeline(R.create(l));for(const a of actions){assert.equal(t.check(a),null,`L${l.id}`);const before=R.signature(t.state);assert(!t.commit({type:'tap',pod:-1}).accepted);assert.equal(R.signature(t.state),before);t.commit(a);moves++;inspect(t);
 if(t.pending){t.advance(137);let copy=clone(t);assert.deepEqual(copy.snapshot(),t.snapshot());restores++;copy.advance(1e9);drain(t);assert.deepEqual(copy.snapshot(),t.snapshot());}else drain(t);
 }assert.equal(t.status,'Won',`L${l.id}`);assert(t.state.slots.every(p=>p<0));assert(R.queueEmpty(t.state));routes++;return t;}
// Broad-front same-colour pods share targets at their own cadence.
const base={id:9000,w:7,h:8,title:'fixture',subject:'fixture',entries:[0,1,2,3,4,5,6],capacity:5};
const broad={...base,rowsBottomFirst:['GGGGGGG','GGGGGGG',...Array(6).fill('.......')],pods:[{color:'G',count:2},{color:'G',count:12}],tray:[[0],[1]]};
let t=new Timeline(R.create(broad));t.commit({type:'tap',pod:0});t.commit({type:'tap',pod:1});assert.equal(t.state.remaining[0],1);assert.equal(t.state.remaining[1],11);assert.equal(t.processedCells,0);assert.equal(t.jobs.size,2);
t.advance(250);assert.equal(t.state.remaining[0],0);assert.equal(t.state.remaining[1],10);assert.equal(t.processedCells,0);assert(t.state.slots.includes(0));t.advance(300);assert(!t.state.slots.includes(0));assert(t.inFlight[0]>0);assert.equal(t.processedCells,0,'Slot must be reusable even before first arrival');
let c=clone(t);t.advance(3210);for(let i=0;i<321;i++)c.advance(10);assert.deepEqual(c.snapshot(),t.snapshot());frameChecks++;
drain(t);assert.equal(t.status,'Won');
// Pair holds both until BOTH unsent quotas reach zero, then releases together.
const pair=structuredClone(broad);pair.pods.forEach(p=>p.group='A');t=new Timeline(R.create(pair));t.commit({type:'tap',pod:1});t.advance(550);assert.equal(t.state.remaining[0],0);assert(t.state.remaining[1]>0);assert.equal(t.free,3);let released=[];while(t.pending){stepEvent(t);released.push(...t.lastEvents.filter(e=>e.type==='complete'));}assert.equal(released.length,2);assert.equal(released[0].at,released[1].at);assert.equal(t.status,'Won');
// Narrow front cannot release a stream toward unopened cells.
const narrow={...base,entries:[3],rowsBottomFirst:[...Array(7).fill('###G###'),'###.###'],pods:[{color:'G',count:4},{color:'G',count:3}],tray:[[0],[1]]};t=new Timeline(R.create(narrow));t.commit({type:'tap',pod:0});t.commit({type:'tap',pod:1});assert.equal(t.jobs.size,1);t.advance(749);assert.equal(t.pendingCells,1);assert.equal(t.state.remaining[1],3);drain(t);assert.equal(t.status,'Won');
// Last released slot may be reused by a new pod while old couriers still walk.
const reuse={...base,rowsBottomFirst:['GGGGGGG',...Array(7).fill('.......')],pods:[{color:'G',count:1},{color:'G',count:6}],tray:[[0,1],[]]};t=new Timeline(R.create(reuse));t.commit({type:'tap',pod:0});t.advance(300);assert(t.inFlight[0]>0);assert.equal(t.free,5);t.commit({type:'tap',pod:1});assert.equal(t.inFlight[0],1);assert.equal(t.inFlight[1],1);drain(t);assert.equal(t.status,'Won');
// Full row with future clears must stay alive, even when nobody can depart now.
const full={...base,rowsBottomFirst:['GGGGGGG','BBBBBBB',...Array(6).fill('.......')],pods:[{color:'G',count:7},...Array.from({length:4},(_,i)=>({color:'B',count:i===3?1:2}))],tray:[[0,1],[2,3],[4]]};t=new Timeline(R.create(full));for(const p of [0,2,4,1,3])t.commit({type:'tap',pod:p});assert.equal(t.free,0);assert.equal(t.status,'Working');const immutable=t.snapshot();assert(!t.commit({type:'tap',pod:999}).accepted);assert.deepEqual(t.snapshot(),immutable);drain(t);assert.equal(t.status,'Won');
// Explicit terminal loss and bridge rescue (no premature Jammed during work).
const jam={...base,rowsBottomFirst:['GGGGGGG',...Array(7).fill('.......')],pods:[...Array.from({length:5},()=>({color:'B',count:1})),{color:'G',count:7}],tray:[[0,3,5],[1,4],[2]]}; // intentionally inconsistent quota: use inaccessible B cells
jam.rowsBottomFirst[7]='BBBBB..';t=new Timeline(R.create(jam));for(const p of [0,1,2,3,4])t.commit({type:'tap',pod:p});assert.equal(t.status,'Jammed');assert.equal(t.pending,false);
const data=JSON.parse(fs.readFileSync('content/pilot-v6/levels.json')),certs=JSON.parse(fs.readFileSync('content/pilot-v6/certificates.json')),old=JSON.parse(fs.readFileSync('content/pilot-v5/levels.json')).levels,quality=JSON.parse(fs.readFileSync('content/pilot-v6/quality.json'));
assert.equal(data.hash,crypto.createHash('sha256').update(JSON.stringify(data.levels)).digest('hex'));assert.equal(quality.complete,true);assert.deepEqual(data.levels.map(l=>l.id),Array.from({length:30},(_,i)=>i+1));assert.equal(new Set(data.levels.map(l=>l.subject)).size,30);
for(const l of data.levels){assert.deepEqual(R.validate(l),[]);if(!quality.changedLevels.includes(l.id))assert.deepEqual(l,old[l.id-1]);const cert=certs[l.id-1];const result=execute(l,cert.actions);timings.push({level:l.id,serialReferenceSeconds:result.now/1000});
 if(cert.fairness){const worlds=hiddenWorlds(l),policy=new Map();for(let w=0;w<worlds.length;w++){let t=new Timeline(R.create(worlds[w]));for(const a of cert.fairness.routes[w]){const obs=R.observation(t.state);if(policy.has(obs))assert.deepEqual(a,policy.get(obs),'Hidden-information oracle');else policy.set(obs,a);assert(t.commit(a).accepted);drain(t);}assert.equal(t.status,'Won');routes++;}fairPolicies++;}
 // Random concurrent inputs, with real reservation/slot semantics and mixed waits.
 for(let trial=0;trial<8;trial++){const random=rng(l.id*713+trial*991);let t=new Timeline(R.create(l)),count=0,events=0;
  while(t.state.status==='Playing'){
   assert(++events<10000,'Simulation stalled');const actions=R.legal(t.state);
   if(actions.length&&(!t.pending||random()<.6)){const a=actions[Math.floor(random()*actions.length)];assert(t.commit(a).accepted);count++;assert(count<=l.pods.length+1);}
   else if(t.pending){t.advance(13+random()*700);}
   else throw Error('Playing without input or pending work');inspect(t);
   if(events%53===0){const restored=clone(t);assert.deepEqual(restored.snapshot(),t.snapshot());t=restored;restores++;}
  }assert(['Won','Jammed','Stuck'].includes(t.status));liveClears+=t.processedCells;randomRuns++;
 }
}
const report={passed:true,catalogHash:data.hash,winningRoutes:routes,acceptedCertificateActions:moves,hiddenInformationPolicies:fairPolicies,concurrentRandomRuns:randomRuns,liveClears,restoredCheckpoints:restores,frameInvarianceChecks:frameChecks,retainedDefinitions:30-quality.changedLevels.length,ownerFunControls:quality.controls,slotReusedBeforeArrival:true,sameColourParallelDispatch:true,pairReleaseAtomic:true,fullWorkingRowSurvives:true,conservation:true,limits:['Winning certificates use idle decisions. They do not cover all possible real-time choices.','Policy rates are not human difficulty. No native Core equivalence claim.']};fs.writeFileSync(out+'/engine.json',JSON.stringify(report,null,2)+'\n');fs.writeFileSync(out+'/reference-times.json',JSON.stringify(timings,null,2)+'\n');console.log(report);

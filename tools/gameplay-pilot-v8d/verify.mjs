import assert from 'node:assert/strict';import fs from 'node:fs';import crypto from 'node:crypto';
import R from './rules.cjs';import Rv from './idle-rules.cjs';import T from './timeline.cjs';import {hiddenWorlds,fairSolve,solve} from './search.mjs';import {rng} from './policies.mjs';import {plan,playPlanner,ROWS,FULL_ROWS} from './horizon.mjs';import {MYSTERY,isKey,reveals,revealsFit} from './fix-lib.mjs';
const {Timeline,PROFILE,TAIL}=T,out='/tmp/bloomlings-pilot-v8d-verification';fs.mkdirSync(out,{recursive:true});
let routes=0,moves=0,restores=0,randomRuns=0,frameChecks=0,fairPolicies=0,liveClears=0;const timings=[];
function clone(t){return new Timeline(R.create(t.state.level),t.snapshot());}
function stepEvent(t){t.advance(t.steps[0].at-t.now);}
function drain(t){t.advance(1e9);assert(!t.pending);t.assert();}
function inspect(t){
 t.assert();for(const j of t.jobs.values())if(j.start===t.now){for(const c of j.route.slice(0,-1))assert.equal(t.state.board[c],R.OPEN);}
 assert(R.topPods(t.state).every(p=>t.state.revealed[p]||t.state.level.pods[p].mystery==='sealed'));
 const startBoard=t.state.meta.board.reduce((n,c)=>n+(c>=0),0);assert.equal(startBoard-t.state.board.reduce((n,c)=>n+(c>=0),0),t.processedCells);
 if(t.pending)assert.equal(t.state.status,'Playing','Premature win/jam');
}
function execute(l,actions){let t=new Timeline(R.create(l));for(const a of actions){assert.equal(t.check(a),null,`L${l.id}`);const before=R.signature(t.state);assert(!t.commit({type:'tap',pod:-1}).accepted);assert.equal(R.signature(t.state),before);t.commit(a);moves++;inspect(t);
 if(t.pending){t.advance(137);let copy=clone(t);assert.deepEqual(copy.snapshot(),t.snapshot());restores++;copy.advance(1e9);drain(t);assert.deepEqual(copy.snapshot(),t.snapshot());}else drain(t);
 }assert.equal(t.status,'Won',`L${l.id}`);assert(t.state.slots.every(p=>p<0));assert(R.queueEmpty(t.state));routes++;return t;}
// Broad-front same-colour pods share targets at their own cadence.
const base={id:9000,w:7,h:8,title:'fixture',subject:'fixture',entries:[0,1,2,3,4,5,6],capacity:5};
// v8D: a far B capsule stays in the queue, so these cadence fixtures run before the auto-tail.
const broad={...base,rowsBottomFirst:['GGGGGGG','GGGGGGG',...Array(5).fill('.......'),'B......'],pods:[{color:'G',count:2},{color:'G',count:12},{color:'B',count:1}],tray:[[0],[1],[2]]};
let t=new Timeline(R.create(broad));t.commit({type:'tap',pod:0});t.commit({type:'tap',pod:1});assert.equal(t.state.remaining[0],1);assert.equal(t.state.remaining[1],11);assert.equal(t.processedCells,0);assert.equal(t.jobs.size,2);
t.advance(250);assert.equal(t.state.remaining[0],0);assert.equal(t.state.remaining[1],10);assert.equal(t.processedCells,0);assert(t.state.slots.includes(0));t.advance(300);assert(!t.state.slots.includes(0));assert(t.inFlight[0]>0);assert.equal(t.processedCells,0,'Slot must be reusable even before first arrival');
let c=clone(t);t.advance(3210);for(let i=0;i<321;i++)c.advance(10);assert.deepEqual(c.snapshot(),t.snapshot());frameChecks++;
assert(!t.tail);t.commit({type:'tap',pod:2});drain(t);assert.equal(t.status,'Won');
// Pair holds both until BOTH unsent quotas reach zero, then releases together.
const pair=structuredClone(broad);pair.pods.slice(0,2).forEach(p=>p.group='A');t=new Timeline(R.create(pair));t.commit({type:'tap',pod:1});t.advance(550);assert.equal(t.state.remaining[0],0);assert(t.state.remaining[1]>0);assert.equal(t.free,3);let released=[];while(t.pending){stepEvent(t);released.push(...t.lastEvents.filter(e=>e.type==='complete'));}assert.equal(released.length,2);assert.equal(released[0].at,released[1].at);t.commit({type:'tap',pod:2});drain(t);assert.equal(t.status,'Won');
// Narrow front cannot release a stream toward unopened cells.
const narrow={...base,entries:[3],rowsBottomFirst:[...Array(7).fill('###G###'),'###.###'],pods:[{color:'G',count:4},{color:'G',count:3}],tray:[[0],[1]]};t=new Timeline(R.create(narrow));t.commit({type:'tap',pod:0});t.commit({type:'tap',pod:1});assert.equal(t.jobs.size,1);t.advance(749);assert.equal(t.pendingCells,1);assert.equal(t.state.remaining[1],3);drain(t);assert.equal(t.status,'Won');
// Last released slot may be reused by a new pod while old couriers still walk.
const reuse={...base,rowsBottomFirst:['GGGGGGG',...Array(7).fill('.......')],pods:[{color:'G',count:1},{color:'G',count:6}],tray:[[0,1],[]]};t=new Timeline(R.create(reuse));t.commit({type:'tap',pod:0});t.advance(300);assert(t.inFlight[0]>0);assert.equal(t.free,5);t.commit({type:'tap',pod:1});assert.equal(t.inFlight[0],1);assert(t.tail);assert.equal(t.inFlight[1],6,'The last capsule empties the queue: the auto-tail sends its whole quota');drain(t);assert.equal(t.status,'Won');
// Full row with future clears must stay alive, even when nobody can depart now.
const full={...base,rowsBottomFirst:['GGGGGGG','BBBBBBB',...Array(6).fill('.......')],pods:[{color:'G',count:7},...Array.from({length:4},(_,i)=>({color:'B',count:i===3?1:2}))],tray:[[0,1],[2,3],[4]]};t=new Timeline(R.create(full));for(const p of [0,2,4,1,3])t.commit({type:'tap',pod:p});assert.equal(t.free,0);assert.equal(t.status,'Working');const immutable=t.snapshot();assert(!t.commit({type:'tap',pod:999}).accepted);assert.deepEqual(t.snapshot(),immutable);drain(t);assert.equal(t.status,'Won');
// Explicit terminal loss and bridge rescue (no premature Jammed during work).
const jam={...base,rowsBottomFirst:['GGGGGGG',...Array(7).fill('.......')],pods:[...Array.from({length:5},()=>({color:'B',count:1})),{color:'G',count:7}],tray:[[0,3,5],[1,4],[2]]}; // intentionally inconsistent quota: use inaccessible B cells
jam.rowsBottomFirst[7]='BBBBB..';t=new Timeline(R.create(jam));for(const p of [0,1,2,3,4])t.commit({type:'tap',pod:p});assert.equal(t.status,'Jammed');assert.equal(t.pending,false);
// A sealed top hides both colour and hero until successful admission.
const sealed={...base,rowsBottomFirst:['GGGGGGG','BBBBBBB',...Array(6).fill('.......')],pods:[{color:'G',count:7,mystery:'sealed'},{color:'B',count:7,mystery:'sealed'}],tray:[[0],[1]]};
assert.deepEqual(R.validate(sealed),[]);let worlds=hiddenWorlds(sealed);assert.equal(R.observation(R.create(worlds[0])),R.observation(R.create(worlds[1])));t=new Timeline(R.create(sealed));assert.deepEqual([...t.state.revealed],[0,0]);assert.deepEqual(R.view(t.state).colours,[null,null]);
let admitted=t.commit({type:'tap',pod:0});assert.deepEqual(admitted.events.filter(e=>e.type==='reveal').map(e=>[e.pod,e.mode,e.color]),[[0,'sealed','G']]);assert.deepEqual([...t.state.revealed],[1,0]);assert.equal(t.state.slots[0],0);let restored=clone(t);assert.deepEqual(restored.snapshot(),t.snapshot());assert.equal(restored.state.revealed[1],0);t.commit({type:'tap',pod:1});drain(t);assert.equal(t.status,'Won');assert.equal(fairSolve(sealed,{budget:1000}).winning,true);
const depth=structuredClone(sealed);depth.pods.forEach(p=>p.mystery='depth');assert.deepEqual([...R.create(depth).revealed],[1,1]);
// Refusing an atomic pair for lack of two places must NOT reveal it.
const sealedPair={...base,rowsBottomFirst:['GGGGGGG','GGGGGGG','B......',...Array(5).fill('.......')],pods:[...[3,3,3,4].map(count=>({color:'G',count})),{color:'G',count:1,mystery:'sealed',group:'A'},{color:'B',count:1,mystery:'sealed',group:'A'}],tray:[[0,4],[1,5],[2],[3]]};
t=new Timeline(R.create(sealedPair));for(const pod of [0,1,2,3])assert(t.commit({type:'tap',pod}).accepted);const untouched=t.snapshot();assert.equal(t.commit({type:'tap',pod:4}).reason,'pair-needs-two-slots');assert.deepEqual(t.snapshot(),untouched);drain(t);admitted=t.commit({type:'tap',pod:4});assert(admitted.accepted);assert.equal(admitted.events.filter(e=>e.type==='reveal').length,2);assert.equal(t.free,3);drain(t);assert.equal(t.status,'Won');
// Reject a guess-only puzzle even though an omniscient solver wins each world.
const guessTrap={...base,capacity:3,rowsBottomFirst:['GGGGGGG','BBBBBBB','CC.....',...Array(5).fill('.......')],pods:[{color:'C',count:1},{color:'C',count:1},{color:'G',count:7,mystery:'sealed'},{color:'B',count:7,mystery:'sealed'}],tray:[[0,2],[1,3]]};
assert.deepEqual(R.validate(guessTrap),[]);worlds=hiddenWorlds(guessTrap);assert(worlds.every(l=>solve(R.create(l),{budget:1000}).winning===true));assert.equal(fairSolve(guessTrap,{budget:1000}).winning,false,'Independent per-world solutions cannot certify hidden-information fairness');
// v8D auto-tail: once the queue is empty, a pod sends every reachable courier at once (no departure gap).
const tailFixture={...base,rowsBottomFirst:['GGGGGGG',...Array(7).fill('.......')],pods:[{color:'G',count:3},{color:'G',count:4}],tray:[[0,1],[]]};
t=new Timeline(R.create(tailFixture));t.commit({type:'tap',pod:0});assert(!t.tail);assert.equal(t.jobs.size,1,'Gap kept while the queue has capsules');drain(t);
t=new Timeline(R.create({...tailFixture,tray:[[0],[1]]}));t.commit({type:'tap',pod:0});assert.equal(t.jobs.size,1);t.commit({type:'tap',pod:1});assert(t.tail);assert.equal(t.jobs.size,1+4,'Tail sends the whole quota at once');assert.equal(t.state.remaining[1],0);
const tailSnap=clone(t);assert.deepEqual(tailSnap.snapshot(),t.snapshot());drain(t);assert.equal(t.status,'Won');
assert.deepEqual({...TAIL},{departureGap:0,speed:4,when:'queue-empty-and-no-bridge-to-choose'});
const data=JSON.parse(fs.readFileSync('content/pilot-v8d/levels.json')),certs=JSON.parse(fs.readFileSync('content/pilot-v8d/certificates.json')),old=JSON.parse(fs.readFileSync('content/pilot-v8c/levels.json')).levels,quality=JSON.parse(fs.readFileSync('content/pilot-v8d/quality.json'));
assert.deepEqual(PROFILE,(await import('../gameplay-pilot-v7/timeline.cjs')).default.PROFILE);assert.equal(fs.readFileSync('tools/gameplay-pilot-v8d/clock.cjs','utf8'),fs.readFileSync('tools/gameplay-pilot-v7/clock.cjs','utf8'));
assert.equal(data.hash,crypto.createHash('sha256').update(JSON.stringify(data.levels)).digest('hex'));assert.equal(quality.complete,true);assert.deepEqual(data.levels.map(l=>l.id),Array.from({length:30},(_,i)=>i+1));assert.equal(new Set(data.levels.map(l=>l.subject)).size,30);
const materials=l=>[...new Set(l.rowsBottomFirst.join('').replace(/[.#]/g,''))].sort();
assert.equal(data.levels.find(l=>materials(l).includes('Y')).id,4);assert.equal(data.levels.find(l=>materials(l).includes('O')).id,5);
const strip=l=>({...l,pods:l.pods.map(({mystery,...p})=>p)});let plannerRoutes=0,leakChecks=0,mysteryLevels=0;
for(const l of data.levels){const prior=old[l.id-1];assert.deepEqual(R.validate(l),[]);
 // v8D keeps v8C's levels; only the "?" capsules move.
 assert.deepEqual(strip(l),strip(prior),'Only mysteries change L'+l.id);if(!quality.changedLevels.includes(l.id))assert.deepEqual(l,prior);
 const cert=certs[l.id-1];const result=execute(l,cert.actions);timings.push({level:l.id,serialReferenceSeconds:result.now/1000});
 // Fairness: the planner that sees only the visible rows replays the certificate exactly.
 assert.equal(cert.planner.rows,ROWS);const replay=playPlanner(l,cert.planner);assert(replay.won,'Planner L'+l.id);assert.deepEqual(replay.actions,cert.actions,'Planner route L'+l.id);plannerRoutes++;
 // ...and never reads what the player cannot see: swapping two unseen capsules (colour and count) never changes its command.
 {let s=Rv.create(l);for(let i=0;i<cert.actions.length;i++){
  if(i%3===0){const seen=new Set(l.tray.flatMap((st,c)=>st.slice(s.heads[c],s.heads[c]+ROWS).filter(p=>s.revealed[p])));
   const unseen=l.tray.flatMap((st,c)=>st.slice(s.heads[c])).filter(p=>!seen.has(p)&&!l.pods[p].group);
   for(let k=0;k+1<unseen.length&&k<4;k+=2){const [a,b]=[unseen[k],unseen[k+1]];if(l.pods[a].color===l.pods[b].color&&l.pods[a].count===l.pods[b].count)continue;
    const swapped={...l,pods:l.pods.map((p,j)=>j===a?{...l.pods[b],mystery:p.mystery}:j===b?{...l.pods[a],mystery:p.mystery}:p)};
    let w=Rv.create(swapped);for(const x of cert.actions.slice(0,i))w=Rv.step(w,x).state;assert.deepEqual(plan(w,cert.planner),plan(s,cert.planner),'Planner reads unseen capsules L'+l.id);leakChecks++;}}
  s=Rv.step(s,cert.actions[i]).state;}}
 // Rare, important "?": 1-3 on the listed levels only, below the 3 full rows at the start, at least one key colour, revealing mid-level.
 const hidden=l.pods.map((p,i)=>p.mystery?i:-1).filter(i=>i>=0);assert.equal(hidden.length,MYSTERY[l.id]||0,'Mystery count L'+l.id);
 if(hidden.length){mysteryLevels++;assert(hidden.every(p=>l.pods[p].mystery==='depth'&&l.tray.find(st=>st.includes(p)).indexOf(p)>=FULL_ROWS));assert(hidden.some(p=>isKey(strip(l),p)),'Key colour L'+l.id);
  const list=reveals(l,cert.actions);assert.equal(list.length,hidden.length);assert(revealsFit(l,list),'Mid-level reveal L'+l.id);}
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
// v8C role gates (unchanged in v8D: the bots only look at the top, which v8D does not hide): every level from L5 keeps its role's windows (fixed seeds, the same bots as fix.mjs).
const bots=await import('./botsc.mjs');const gates=[];
for(const q of quality.levels){if(q.role==='tutorial')continue;const l=data.levels[q.id-1],t=quality.roles[q.role],b=q.bots;assert(b,'Bot report L'+q.id);
 const within=(v,[lo,hi])=>v>=lo-.03&&v<=hi+.03;
 if(!quality.controls.includes(q.id)){assert(within(b.random,t.random),'Random window L'+q.id);assert(within(b.visible,t.visible),'Visible window L'+q.id);assert(b.patternWins.length<=t.patterns,'Pattern wins L'+q.id);assert(b.sensibleWins<=t.sensible,'Sensible wins L'+q.id);}
 assert.deepEqual(bots.patternWins(l),b.patternWins,'Pattern replay L'+q.id);assert.equal(bots.sensible(l),b.sensibleWins,'Sensible replay L'+q.id);
 gates.push({id:q.id,role:q.role,random:b.random,visible:b.visible,patternWins:b.patternWins.length,sensibleWins:b.sensibleWins});}
// Whole-pilot pattern limits: no pattern wins most of the game, nor more than one Hard/Super Hard level.
const counted=quality.levels.filter(q=>q.bots),share={},hardShare={};for(const q of counted)for(const p of q.bots.patternWins){share[p]=(share[p]||0)+1;if(['hard','superHard'].includes(q.role))hardShare[p]=(hardShare[p]||0)+1;}
for(const [p,n] of Object.entries(share))assert(n<=quality.patternShare*counted.length,'Pattern wins too much of the game: '+p);for(const [p,n] of Object.entries(hardShare))assert(n<=quality.hardPatternLevels,'Pattern wins several Hard levels: '+p);
const report={passed:true,botGates:gates,patternShare:share,hardPatternShare:hardShare,catalogHash:data.hash,winningRoutes:routes,acceptedCertificateActions:moves,visibleHorizonPlannerRoutes:plannerRoutes,plannerLeakChecks:leakChecks,mysteryLevels,visibleRows:ROWS,autoTail:TAIL,concurrentRandomRuns:randomRuns,liveClears,restoredCheckpoints:restores,frameInvarianceChecks:frameChecks,retainedDefinitions:30-quality.changedLevels.length,ownerFunControls:quality.controls,slotReusedBeforeArrival:true,sameColourParallelDispatch:true,pairReleaseAtomic:true,fullWorkingRowSurvives:true,conservation:true,sealedAdmissionAndCheckpointVerified:true,rejectedPairKeepsSecrets:true,legacyDepthRevealPreserved:true,guessOnlyPuzzleRejected:true,limits:['Winning certificates use idle decisions. They do not cover all possible real-time choices.','Policy rates are not human difficulty. No native Core equivalence claim.']};fs.writeFileSync(out+'/engine.json',JSON.stringify(report,null,2)+'\n');fs.writeFileSync(out+'/reference-times.json',JSON.stringify(timings,null,2)+'\n');console.log(report);

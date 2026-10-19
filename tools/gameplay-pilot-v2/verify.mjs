import assert from 'node:assert/strict';
import {readFile,writeFile,mkdir} from 'node:fs/promises';
import {createHash} from 'node:crypto';
import {resolve,dirname} from 'node:path';
import {fileURLToPath} from 'node:url';
import R from './rules.cjs';
import {solve,hiddenWorlds,held,work,bridgeProof} from './search.mjs';
import {rng,select,MODES} from './policies.mjs';

const ROOT=resolve(dirname(fileURLToPath(import.meta.url)),'../..');
const OUT=process.argv.find(a=>a.startsWith('--out='))?.slice(6)||'/tmp/bloomlings-pilot-v2-verification';
const dataset=JSON.parse(await readFile(resolve(ROOT,'content/pilot-v2/levels.json')));
const certs=JSON.parse(await readFile(resolve(ROOT,'content/pilot-v2/certificates.json')));
const quality=JSON.parse(await readFile(resolve(ROOT,'content/pilot-v2/quality.json')));
const sha=v=>createHash('sha256').update(v).digest('hex');
const cases=[],fixtures=[];let moves=0,proofs=0,hiddenPolicies=0,bridgeChanges=0,zeroReservations=0;
function transcript(level,actions,expected){
 let s=R.create(level);const signatures=[R.signature(s)];R.assertAccounting(s);
 for(const action of actions){
  const before=R.signature(s),result=R.step(s,action,true);assert(result.accepted,`Rejected L${level.id}: ${JSON.stringify(action)}`);assert.equal(R.signature(s),before,'Input mutated');
  s=result.state;R.assertAccounting(s);signatures.push(R.signature(s));moves++;
  if(s.slots.some(p=>p>=0&&s.remaining[p]===0))zeroReservations++;
 }
 if(expected)assert.equal(s.status,expected,`L${level.id}`);
 cases.push({level:level.id,actions,signatures});return s;
}
function stateAfter(level,actions){let s=R.create(level);for(const a of actions){const n=R.step(s,a);assert(n.accepted);s=n.state;}return s;}
function truth(result,message){assert.notEqual(result.winning,null,`Search incomplete: ${message}`);assert.equal(result.winning,false,message);}

assert.deepEqual(dataset.levels.map(l=>l.id),Array.from({length:30},(_,i)=>i+1));
assert.equal(dataset.hash,sha(JSON.stringify(dataset.levels)));assert.equal(quality.catalogHash,dataset.hash);assert.equal(quality.complete,true);
assert.equal(new Set(dataset.levels.map(l=>l.rowsBottomFirst.join(''))).size,30);assert.equal(new Set(dataset.levels.map(l=>l.subject)).size,30);
assert.deepEqual(dataset.levels.filter(l=>l.difficulty==='Hard').map(l=>l.id),[15,22,25,29]);
for(const level of dataset.levels){
 assert.deepEqual(R.validate(level),[],`Invalid L${level.id}`);
 const groups=new Set(level.pods.map(p=>p.group).filter(Boolean));
 assert(level.pods.length>=9);assert(level.pods.length-groups.size>=8,'Too few selectable units for five slots');
 assert(level.w>=16&&level.h>=18);assert.equal(groups.size>0,level.id>=10);
 assert.equal(Boolean(level.hero),[18,19,22,24,27,29].includes(level.id));
 for(const p of level.pods){if(level.id<3)assert('GB'.includes(p.color));if(level.id<5)assert('GPB'.includes(p.color));if(level.id<8)assert(p.color!=='C');if(level.id<20)assert(p.color!=='D');}
 const cert=certs.find(c=>c.id===level.id);assert(cert);
 const won=transcript(level,cert.actions,'Won');assert(R.queueEmpty(won));assert(won.slots.every(p=>p<0));
 let s=R.create(level);for(const [i,a] of cert.actions.entries()){
  s=R.step(s,a).state;if(i<5)assert(!R.queueEmpty(s),'Queue exhausted in first five choices');
 }
 if(cert.losing)assert.notEqual(transcript(level,cert.losing).status,'Won');
 if(level.difficulty==='Hard'){
  truth(solve(R.create(level),{heldLimit:2,budget:100000}),`L${level.id} should require >2 occupied slots after settling`);
  assert.equal(cert.decisionProofs.length,2);assert(cert.decisionProofs.some(p=>p.sameColourChoice));
  assert(cert.decisionProofs[1].at-cert.decisionProofs[0].at>=3);
  for(const proof of cert.decisionProofs){
   transcript(level,[...proof.prefix,...proof.continuation],'Won');
   const prior=stateAfter(level,proof.prefix),bad=transcript(level,[...proof.prefix,proof.wrong]);
   assert.equal(bad.status,'Playing');assert(work(prior)>work(bad),'Wrong alternative should do visible work');
   truth(solve(bad,{budget:100000}),`L${level.id} false losing branch`);proofs++;
  }
 }
 if(cert.fairness){
  const worlds=hiddenWorlds(level),publicPolicy=new Map(),indices=level.pods.map((p,i)=>p.mystery?i:-1).filter(i=>i>=0);
  // Enumerate all colours consistent with publicly visible exact work totals.
  const totals=Object.fromEntries([...R.CHANNELS].map(c=>[c,0]));
  for(const c of level.rowsBottomFirst.join(''))if(c in totals)totals[c]++;
  for(const p of level.pods)if(!p.mystery)totals[p.color]-=p.count;
  const valid=[];for(const a of R.CHANNELS)for(const b of R.CHANNELS){const rest={...totals};rest[a]-=level.pods[indices[0]].count;rest[b]-=level.pods[indices[1]].count;if(Object.values(rest).every(n=>n===0))valid.push(a+b);}
  assert.equal(valid.length,2,'Uncertainty domain incomplete');
  assert.deepEqual(new Set(valid),new Set(worlds.map(l=>indices.map(p=>l.pods[p].color).join(''))));
  assert.equal(cert.fairness.routes.length,2);
  for(const [i,world] of worlds.entries()){
   const copy={...world,id:10000+level.id*2+i};fixtures.push(copy);let state=R.create(copy);
   for(const a of cert.fairness.routes[i]){const obs=R.observation(state);if(publicPolicy.has(obs))assert.deepEqual(a,publicPolicy.get(obs),'Hidden-colour oracle in policy');publicPolicy.set(obs,a);state=R.step(state,a).state;}
   transcript(copy,cert.fairness.routes[i],'Won');
  }
  for(const mode of MODES){
   const states=worlds.map(R.create),randoms=[rng(912),rng(912)];
   for(let i=0;i<level.pods.length;i++){
    if(R.observation(states[0])!==R.observation(states[1])||states[0].status!=='Playing')break;
    const actions=states.map((s,j)=>select(s,mode,randoms[j],i));assert.deepEqual(actions[0],actions[1],`Policy ${mode} leaked a hidden colour`);
    states.forEach((s,j)=>{states[j]=R.step(s,actions[j]).state;});
   }
  }
  hiddenPolicies++;
 }
 if(level.hero?.choices.length===2){
  const bridge=bridgeProof(level,cert.actions);assert(bridge?.bothReady);assert.equal(bridge.sameOrderWins,false);assert.notEqual(bridge.alternateWinning,null);
  if(bridge.alternateWinning)transcript(level,[...cert.actions.slice(0,bridge.at),{type:'bridge',choice:bridge.alternative},...bridge.alternateActions],'Won');
  bridgeChanges++;
 }
 const random=rng(level.id*812739);
 for(let trial=0;trial<12;trial++){
  let state=R.create(level);const actions=[];
  while(state.status==='Playing'){assert(actions.length<level.pods.length+2);const options=R.legal(state);assert(options.length);const a=options[Math.floor(random()*options.length)];actions.push(a);state=R.step(state,a).state;}
  transcript(level,actions,state.status);
 }
 const initial=R.create(level),signature=R.signature(initial);
 assert(!R.step(initial,{type:'tap',pod:-1}).accepted);assert(!R.step(initial,{type:'bridge',choice:99}).accepted);
 const future=level.tray.find(s=>s.length>1)?.[1];assert(!R.step(initial,{type:'tap',pod:future}).accepted);assert.equal(R.signature(initial),signature);
}

const base={w:7,h:8,title:'Fixture',subject:'fixture',entries:[3]};
const pairFixture={...base,id:1001,rowsBottomFirst:['GGGGGGG','CCCCCCC','...B...',...Array(5).fill('.......')],pods:[{color:'G',count:7,group:'A'},{color:'B',count:1,group:'A'},{color:'C',count:7}],tray:[[0,2],[1]]};
assert.deepEqual(R.validate(pairFixture),[]);
const pending=transcript(pairFixture,[{type:'tap',pod:1}],'Playing');assert.equal(held(pending),2);assert.equal(pending.remaining[0],0);assert(pending.slots.includes(0),'Completed half released early');assert.deepEqual(Array.from(pending.heads),[1,1]);
transcript(pairFixture,[{type:'tap',pod:0},{type:'tap',pod:2}],'Won');
const capacityFixture={...base,id:1002,rowsBottomFirst:['GGGGGGG','CCCCCCC','..BBB..','..BB...',...Array(4).fill('.......')],pods:[{color:'G',count:7,group:'A'},{color:'B',count:1,group:'A'},...Array.from({length:4},()=>({color:'B',count:1})),{color:'C',count:7}],tray:[[0,6],[1],[2],[3],[4],[5]]};
assert.deepEqual(R.validate(capacityFixture),[]);
const four=transcript(capacityFixture,[2,3,4,5].map(pod=>({type:'tap',pod})),'Stuck');assert.equal(held(four),4);assert.equal(R.canTap(four,0),'pair-needs-two-slots');assert(!R.step(four,{type:'tap',pod:0}).accepted);
const blocked=R.clone(R.create(pairFixture));blocked.heads[1]=1;assert.equal(R.canTap(blocked,0),'partner-not-exposed');
const bridgeFixture={...base,id:1003,rowsBottomFirst:['GGGGGGG','#######',...Array(6).fill('BBBBBBB')],pods:[{color:'B',count:8},{color:'B',count:8},{color:'B',count:8},{color:'B',count:8},{color:'B',count:10},{color:'G',count:7}],tray:[[0],[1],[2],[3],[4],[5]],hero:{id:'sprig-bridge',choices:[{cell:9,anchor:2},{cell:11,anchor:4}]}};
for(const choice of [0,1]){
 const actions=[5,0,1,2,3,4].map(pod=>({type:'tap',pod})),full=transcript(bridgeFixture,actions,'Playing');assert.equal(held(full),5);assert.equal(R.legal(full).length,2);transcript(bridgeFixture,[...actions,{type:'bridge',choice}],'Won');
}
const exactFixture={...base,id:1004,rowsBottomFirst:['...B...','...C...',...Array(6).fill('.......')],pods:[{color:'B',count:1},{color:'C',count:1}],tray:[[0],[1]]};
const wait=transcript(exactFixture,[{type:'tap',pod:1}],'Playing');assert.equal(wait.remaining[1],1);transcript(exactFixture,[{type:'tap',pod:1},{type:'tap',pod:0}],'Won');
fixtures.push(pairFixture,capacityFixture,bridgeFixture,exactFixture);
assert(zeroReservations>0);
await mkdir(OUT,{recursive:true});
await writeFile(resolve(OUT,'transcripts.json'),JSON.stringify({levels:[...dataset.levels,...fixtures],cases}));
const report={version:dataset.version,catalogHash:dataset.hash,rulesHash:sha(await readFile(resolve(ROOT,'tools/gameplay-pilot-v2/rules.cjs'))),levels:30,winningCertificates:30,minimumCapsules:9,minimumSelectableUnits:8,firstFiveChoicesCannotEmptyQueue:true,hardLevels:[15,22,25,29],hardMinimumOccupiedSlotsAtLeast:3,provenLosingBranches:proofs,mysteryInformationPolicies:hiddenPolicies,nonCosmeticBridgeChoices:bridgeChanges,zeroWorkReservationStates:zeroReservations,transcriptCases:cases.length,acceptedActions:moves,jsAccounting:true,inputImmutability:true,atomicPairAdmission:true,coReservation:true,exactMaterial:true,bridgeRescuesFullSlots:true,limits:['Policy samples do not measure human difficulty.','Co-reservation and bridge are explicit experimental adapters; production Core and catalog are unchanged.','No real phone or APK performance test.']};
await writeFile(resolve(OUT,'javascript.json'),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify(report));

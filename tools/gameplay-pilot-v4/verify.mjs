import assert from 'node:assert/strict';
import {readFile,writeFile,mkdir} from 'node:fs/promises';
import {createHash} from 'node:crypto';
import {resolve,dirname} from 'node:path';
import {fileURLToPath} from 'node:url';
import R from './rules.cjs';
import {solve,hiddenWorlds,work,bridgeProof} from './search.mjs';
import {rng,select,MODES} from './policies.mjs';
const ROOT=resolve(dirname(fileURLToPath(import.meta.url)),'../..'),OUT=process.argv.find(a=>a.startsWith('--out='))?.slice(6)||'/tmp/bloomlings-pilot-v4-verification';
const data=JSON.parse(await readFile(resolve(ROOT,'content/pilot-v4/levels.json'))),certs=JSON.parse(await readFile(resolve(ROOT,'content/pilot-v4/certificates.json'))),quality=JSON.parse(await readFile(resolve(ROOT,'content/pilot-v4/quality.json'))),old=JSON.parse(await readFile(resolve(ROOT,'content/pilot-v3/levels.json'))).levels;
const sha=v=>createHash('sha256').update(v).digest('hex'),cases=[],fixtures=[];let moves=0,proofs=0,earlyProofs=0,policies=0,bridges=0,configs=0;
function transcript(level,actions,expected){let s=R.create(level);const signatures=[R.signature(s)];for(const a of actions){const before=R.signature(s),n=R.step(s,a);assert(n.accepted,`Rejected L${level.id}`);assert.equal(R.signature(s),before);s=n.state;R.assertAccounting(s);assert(R.topPods(s).every(p=>s.revealed[p]),'Unrevealed selectable top');signatures.push(R.signature(s));moves++;}if(expected)assert.equal(s.status,expected,`L${level.id}`);cases.push({level:level.id,actions,signatures});return s;}
function proofFalse(state,label){const answer=solve(state,{budget:100000});assert.equal(answer.winning,false,label);}
function fairness(level,policy){
 if(!policy)return;const map=new Map(),worlds=hiddenWorlds(level);assert.equal(policy.routes.length,worlds.length);
 for(const [i,world] of worlds.entries()){
  const l={...world,id:20000+level.id*2+i};fixtures.push(l);let s=R.create(l);
  for(const a of policy.routes[i]){const obs=R.observation(s);if(map.has(obs))assert.deepEqual(a,map.get(obs),'Hidden-colour oracle');map.set(obs,a);s=R.step(s,a).state;}transcript(l,policy.routes[i],'Won');
 }
 for(const mode of MODES){const states=worlds.map(l=>R.create(l)),randoms=worlds.map(()=>rng(912));for(let i=0;i<level.pods.length;i++){if(states[0].status!=='Playing'||!states.every(s=>R.observation(s)===R.observation(states[0])))break;const a=states.map((s,j)=>select(s,mode,randoms[j],i));assert(a.every(v=>JSON.stringify(v)===JSON.stringify(a[0])));states.forEach((s,j)=>{states[j]=R.step(s,a[j]).state;});}}
 policies++;
}
assert.deepEqual(data.levels.map(l=>l.id),Array.from({length:30},(_,i)=>i+1));assert.equal(data.hash,sha(JSON.stringify(data.levels)));assert.equal(quality.catalogHash,data.hash);assert(quality.complete);assert.equal(new Set(data.levels.map(l=>l.subject)).size,30);
const changed=new Set(Array.from({length:30},(_,i)=>i+1).filter(id=>![15,22].includes(id)));
for(const level of data.levels){
 assert.deepEqual(R.validate(level),[]);assert.equal(level.capacity,5);assert(level.pods.length>=9);
 const codes=new Set(level.rowsBottomFirst.join('').replace(/[.#]/g,''));assert(codes.size>=3);if(level.id<3)assert(!codes.has('C'));if(level.id<4)assert(!codes.has('D'));if(level.id<11)assert(!codes.has('V'));
 if(level.id===4)assert.equal(codes.size,6);if(level.id===11)assert(codes.has('V'));if(level.hero)assert([20,21,22,24,29].includes(level.id));
 if(level.id<6)assert(!level.pods.some(p=>p.mystery));if(level.id<8)assert(!level.pods.some(p=>p.group));
 if(level.id===8){const pair=level.pods.filter(p=>p.group);assert.equal(pair.length,2);assert.equal(pair[0].color,pair[1].color);}
 if(level.id===9){const pair=level.pods.filter(p=>p.group);assert.equal(pair.length,2);assert.notEqual(pair[0].color,pair[1].color);}
 const groups=new Set(level.pods.map(p=>p.group).filter(Boolean));assert(level.pods.length-groups.size>=8);
 if(!changed.has(level.id))for(const field of ['w','h','rowsBottomFirst','pods','tray','entries','hero'])assert.deepEqual(level[field],old[level.id-1][field],`Control L${level.id} changed ${field}`);
 const cert=certs.find(c=>c.id===level.id);assert(cert);const start=R.create(level);assert(R.topPods(start).every(p=>start.revealed[p]));const won=transcript(level,cert.actions,'Won');assert(R.queueEmpty(won));assert(won.slots.every(p=>p<0));configs++;
 let s=start;for(const [i,a]of cert.actions.entries()){s=R.step(s,a).state;if(i<5)assert(!R.queueEmpty(s));}
 fairness(level,cert.fairness);
 if(level.difficulty==='Hard'){
  const low=solve(R.create(level),{heldLimit:2,budget:100000});assert.equal(low.winning,false,`L${level.id} lacks sustained pressure`);
 }
 for(const p of cert.decisionProofs||[]){
  transcript(level,[...p.prefix,...p.continuation],'Won');const bad=transcript(level,[...p.prefix,p.wrong]);proofFalse(bad,`L${level.id} losing branch`);
  if(level.difficulty==='Hard'){assert.equal(bad.status,'Playing');proofs++;}else earlyProofs++;
 }
 if(level.difficulty==='Hard'){assert.equal(cert.decisionProofs.length,2);assert(cert.decisionProofs.some(p=>p.sameColourChoice));assert(cert.decisionProofs[1].at-cert.decisionProofs[0].at>=3);}
 if(level.hero?.choices.length===2){const b=bridgeProof(level,cert.actions);assert(b?.bothReady);assert.equal(b.sameOrderWins,false);assert.notEqual(b.alternateWinning,null);bridges++;}
 const random=rng(level.id*812739);for(let trial=0;trial<10;trial++){let s=R.create(level);const actions=[];while(s.status==='Playing'){assert(actions.length<level.pods.length+2);const options=R.legal(s);assert(options.length);const a=options[Math.floor(random()*options.length)];actions.push(a);s=R.step(s,a).state;}transcript(level,actions,s.status);}
 const sig=R.signature(start);assert(!R.step(start,{type:'tap',pod:-1}).accepted);assert(!R.step(start,{type:'bridge',choice:99}).accepted);const future=level.tray.find(s=>s.length>1)?.[1];assert(!R.step(start,{type:'tap',pod:future}).accepted);assert.equal(R.signature(start),sig);
}
const base={w:7,h:8,title:'Fixture',subject:'fixture',entries:[3],capacity:3};
const reveal={...base,id:5001,rowsBottomFirst:['GGGGGGG','CCCCCCC','BBBBBBB',...Array(5).fill('.......')],pods:[{color:'G',count:7,group:'A',mystery:true},{color:'B',count:7,group:'A',mystery:true},{color:'C',count:7}],tray:[[0,2],[1]]};
assert.deepEqual(R.validate(reveal),[]);
assert.deepEqual(Array.from(R.create(reveal).revealed),[1,1,1]);transcript(reveal,[{type:'tap',pod:1},{type:'tap',pod:2}],'Won');fixtures.push(reveal);
const depth={...base,id:5002,rowsBottomFirst:['GGGGGGG','BBBB...',...Array(6).fill('.......')],pods:[{color:'G',count:3},{color:'G',count:4,mystery:true},{color:'B',count:4,mystery:true}],tray:[[0,1],[2]]};
assert.deepEqual(R.validate(depth),[]);
assert.deepEqual(Array.from(R.create(depth).revealed),[1,0,1]);const moved=transcript(depth,[{type:'tap',pod:0}],'Playing');assert.equal(moved.revealed[1],1);transcript(depth,[{type:'tap',pod:0},{type:'tap',pod:1},{type:'tap',pod:2}],'Won');fixtures.push(depth);
await mkdir(OUT,{recursive:true});await writeFile(resolve(OUT,'transcripts.json'),JSON.stringify({levels:[...data.levels,...fixtures],cases}));
const report={version:data.version,catalogHash:data.hash,rulesHash:sha(await readFile(resolve(ROOT,'tools/gameplay-pilot-v4/rules.cjs'))),timelineHash:sha(await readFile(resolve(ROOT,'tools/gameplay-pilot-v4/timeline.cjs'))),levels:30,winningConfigurations:configs,retainedControlLevels:30-changed.size,hardLosingBranches:proofs,earlyTeachingBranches:earlyProofs,informationPolicies:policies,nonCosmeticBridgeChoices:bridges,transcriptCases:cases.length,acceptedActions:moves,allTopsKnownBeforeSelection:true,inputImmutable:true,limits:['Human enjoyment and timing still need the next playtest.','Capacity, co-reservation, top reveal and bridge are explicit experimental adapters; shipping Core/catalog unchanged.']};
await writeFile(resolve(OUT,'javascript.json'),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify(report));

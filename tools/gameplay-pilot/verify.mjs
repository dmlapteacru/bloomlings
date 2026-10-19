import assert from 'node:assert/strict';
import {readFile,writeFile,mkdir} from 'node:fs/promises';
import {createHash} from 'node:crypto';
import {resolve,dirname} from 'node:path';
import {fileURLToPath} from 'node:url';
import R from './rules.cjs';

const ROOT=resolve(dirname(fileURLToPath(import.meta.url)),'../..');
const OUT=process.argv.find(a=>a.startsWith('--out='))?.slice(6)||'/tmp/bloomlings-pilot-verification';
const dataset=JSON.parse(await readFile(resolve(ROOT,'content/pilot-v1/levels.json'),'utf8'));
const certificates=JSON.parse(await readFile(resolve(ROOT,'content/pilot-v1/certificates.json'),'utf8'));
const quality=JSON.parse(await readFile(resolve(ROOT,'content/pilot-v1/quality.json'),'utf8'));
const sha=value=>createHash('sha256').update(value).digest('hex');
const rng=seed=>{let s=seed>>>0;return()=>{s^=s<<13;s^=s>>>17;s^=s<<5;return(s>>>0)/4294967296;};};
const cases=[];let verifiedActions=0,proofs=0;

function transcript(level,actions,expected){
  let state=R.create(level);const signatures=[R.signature(state)];
  R.assertAccounting(state);
  for(const action of actions){
    const before=R.signature(state),result=R.step(state,action,true);
    assert.equal(result.accepted,true,`Rejected witness L${level.id}: ${JSON.stringify(action)}`);
    assert.equal(R.signature(state),before,'Move mutated its input');
    state=result.state;R.assertAccounting(state);signatures.push(R.signature(state));verifiedActions++;
    for(const p of state.remaining)assert(p>=0,'Negative work');
  }
  if(expected)assert.equal(state.status,expected,`L${level.id}`);
  cases.push({level:level.id,actions,signatures});return state;
}
function canWin(start){
  const memo=new Map();let nodes=0;
  function visit(state){
    if(state.status==='Won')return true;
    if(state.status!=='Playing')return false;
    const key=R.signature(state);if(memo.has(key))return memo.get(key);
    assert(++nodes<=100000,'Decision proof exceeded verification budget');
    for(const action of R.legal(state))if(visit(R.step(state,action).state)){memo.set(key,true);return true;}
    memo.set(key,false);return false;
  }
  return {winning:visit(start),nodes};
}

assert.deepEqual(dataset.levels.map(l=>l.id),Array.from({length:30},(_,i)=>i+1));
assert.equal(dataset.hash,sha(JSON.stringify(dataset.levels)));
assert.equal(quality.catalogHash,dataset.hash);assert.equal(quality.complete,true);
assert.equal(new Set(dataset.levels.map(l=>l.rowsBottomFirst.join(''))).size,30,'Repeated artwork');
assert.equal(new Set(dataset.levels.map(l=>l.subject)).size,30,'Repeated subject');
assert.deepEqual(dataset.levels.filter(l=>l.difficulty==='Hard').map(l=>l.id),[16,23,29]);
for(const level of dataset.levels){
  assert.deepEqual(R.validate(level),[],`Level definition L${level.id}`);
  assert.equal(Boolean(level.hero),level.id>=25,'Bridge introduction timing');
  assert.equal(level.hero?.choices.length||0,level.id<25?0:level.id<28?1:2);
  for(const pod of level.pods){if(level.id<4)assert('GB'.includes(pod.color));if(level.id<7)assert('GPB'.includes(pod.color));if(level.id<12)assert(pod.color!=='C');if(level.id<20)assert(pod.color!=='D');}
  const cert=certificates.find(c=>c.id===level.id);
  transcript(level,cert.actions,'Won');
  if(cert.losing)assert.notEqual(transcript(level,cert.losing).status,'Won');
  for(const proof of cert.decisionProofs){
    transcript(level,[...proof.prefix,...proof.continuation],'Won');
    const bad=transcript(level,[...proof.prefix,proof.wrong]);
    const check=canWin(bad);assert.equal(check.winning,false,`False decision proof L${level.id}`);proofs++;
  }
  const random=rng(level.id*812739);
  for(let trial=0;trial<20;trial++){
    let state=R.create(level);const actions=[];
    for(let i=0;state.status==='Playing';i++){
      assert(i<level.pods.length+2);
      const legal=R.legal(state);assert(legal.length>0,'Playing without any command');
      const action=legal[Math.floor(random()*legal.length)];actions.push(action);state=R.step(state,action).state;
    }
    transcript(level,actions,state.status);
  }
  const initial=R.create(level),sig=R.signature(initial);
  assert.equal(R.step(initial,{type:'tap',pod:-1}).accepted,false);
  assert.equal(R.step(initial,{type:'bridge',choice:99}).accepted,false);
  const future=level.tray.find(s=>s.length>1)?.[1];
  if(future!==undefined)assert.equal(R.step(initial,{type:'tap',pod:future}).accepted,false);
  assert.equal(R.signature(initial),sig,'Refused action mutated state');
}

// Minimal fixtures exercise rules that may be rare in generated winning routes.
const bridgeFixture={id:1001,w:7,h:8,title:'Fixture',subject:'fixture-bridge',entries:[3],rowsBottomFirst:['GGGGGGG','#######',...Array(6).fill('BBBBBBB')],pods:[{color:'B',count:8},{color:'B',count:8},{color:'B',count:8},{color:'B',count:8},{color:'B',count:10},{color:'G',count:7}],tray:[[0],[1],[2],[3],[4],[5]],hero:{id:'sprig-bridge',choices:[{cell:9,anchor:2},{cell:11,anchor:4}]}};
assert.deepEqual(R.validate(bridgeFixture),[]);
assert.equal(R.step(R.create(bridgeFixture),{type:'bridge',choice:0}).accepted,false);
for(const choice of [0,1]){
  const actions=[{type:'tap',pod:5},...[0,1,2,3,4].map(pod=>({type:'tap',pod}))];
  const full=transcript(bridgeFixture,actions,'Playing');assert(!full.slots.includes(-1));
  assert.equal(R.legal(full).length,2,'A ready bridge must rescue a full slot row');
  const won=transcript(bridgeFixture,[...actions,{type:'bridge',choice}],'Won');
  assert.equal(R.bridgeChoices(won).length,0,'Only one bridge per attempt');
  assert.equal(won.board.filter(c=>c===R.WALL).length,6,'Only selected wall cell opened');
}
const exactFixture={id:1002,w:7,h:8,title:'Fixture',subject:'fixture-material',entries:[3],rowsBottomFirst:['...B...','...C...',...Array(6).fill('.......')],pods:[{color:'B',count:1},{color:'C',count:1}],tray:[[0],[1]]};
const wait=transcript(exactFixture,[{type:'tap',pod:1}],'Playing');
assert.equal(wait.remaining[1],1,'Same hero does not imply matching material');
assert.equal(wait.board[10],4);
const completed=transcript(exactFixture,[{type:'tap',pod:1},{type:'tap',pod:0}],'Won');
assert(completed.slots.every(p=>p===-1));
const eventCase=R.step(wait,{type:'tap',pod:0},true);
assert.deepEqual(eventCase.events.filter(e=>e.type==='clear').map(e=>e.pod),[0,1],'New reach must be recomputed between rounds');

await mkdir(OUT,{recursive:true});
await writeFile(resolve(OUT,'transcripts.json'),JSON.stringify({levels:[...dataset.levels,bridgeFixture,exactFixture],cases}));
const report={version:dataset.version,catalogHash:dataset.hash,rulesHash:sha(await readFile(resolve(ROOT,'tools/gameplay-pilot/rules.cjs'))),levels:30,winningCertificates:30,decisionProofs:proofs,transcriptCases:cases.length,acceptedActions:verifiedActions,jsAccounting:true,refusedMovesImmutable:true,bridgeFullSlots:true,exactMaterial:true,limits:'Human difficulty is not measured by automated checks.'};
await writeFile(resolve(OUT,'javascript.json'),JSON.stringify(report,null,2)+'\n');
console.log(`PASS: 30 winning certificates, ${proofs} independently enumerated losing branches, ${cases.length} transcript cases / ${verifiedActions} actions; accounting, input immutability, bridge and material edge cases.`);
console.log(`Native comparison input: ${resolve(OUT,'transcripts.json')}`);

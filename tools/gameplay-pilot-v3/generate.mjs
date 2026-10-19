import {readFile,writeFile,mkdir} from 'node:fs/promises';
import {createHash} from 'node:crypto';
import {resolve,dirname} from 'node:path';
import {fileURLToPath} from 'node:url';
import R from './rules.cjs';
import {makeLevelArt} from './art.mjs';
import {definition} from './author.mjs';
import {solve,fairSolve,held,work,decisionProofs,bridgeProof} from './search.mjs';
import {rng,sample,MODES} from './policies.mjs';
const ROOT=resolve(dirname(fileURLToPath(import.meta.url)),'../..');
const arg=(k,d)=>process.argv.find(a=>a.startsWith(`--${k}=`))?.slice(k.length+3)||d;
const OUT=resolve(ROOT,arg('out','content/pilot-v3')),ONLY=Number(arg('only',0)),MAX=Number(arg('attempts',4000));
const previous=JSON.parse(await readFile(resolve(ROOT,'content/pilot-v2/levels.json'))).levels;
const oldCerts=JSON.parse(await readFile(resolve(ROOT,'content/pilot-v2/certificates.json')));
const CHANGED=new Set([1,2,3,4,5,6,7,8,9,17,25,26]),HARD=new Set([15,22,25,29]);
const sha=v=>createHash('sha256').update(v).digest('hex');
function replay(l,actions){let s=R.create(l),peak=0,prep=0;for(const a of actions){const next=R.step(s,a);if(!next.accepted)return null;if(a.type==='tap'&&work(next.state)===work(s)&&s.revealed[a.pod])prep++;s=next.state;peak=Math.max(peak,held(s));}return s.status==='Won'?{peak,prep}:null;}
function earlyProof(l,actions){let state=R.create(l);for(let at=0;at<actions.length;at++){const correct=actions[at];for(const wrong of R.legal(state)){if(JSON.stringify(wrong)===JSON.stringify(correct))continue;const bad=R.step(state,wrong).state,check=solve(bad,{budget:15000});if(check.winning===false)return [{at,prefix:actions.slice(0,at),continuation:actions.slice(at),wrong,correct,badClears:work(state)-work(bad),delayed:bad.status==='Playing'}];}state=R.step(state,correct).state;}return [];}
function certificate(l,actions,decisions=[],fair=null){return {id:l.id,actions,decisionProofs:decisions,bridge:bridgeProof(l,actions),fairness:fair?{worlds:fair.worlds,routes:fair.routes}:null};}
await mkdir(OUT,{recursive:true});const levels=[],certs=[],quality=[];
for(const id of (ONLY?[ONLY]:Array.from({length:30},(_,i)=>i+1))){
 let accepted=null;const rejected={},reject=k=>{rejected[k]=(rejected[k]||0)+1;};
 if(!CHANGED.has(id)){
  const level={...previous[id-1],capacity:5},old=oldCerts.find(c=>c.id===id);const route=replay(level,old.actions);if(!route)throw Error('Control no longer wins');
  const fair=level.pods.some(p=>p.mystery)?fairSolve(level,{budget:50000}):null;if(fair&&fair.winning!==true)throw Error(`Control information failure L${id}`);
  accepted={level,cert:certificate(level,old.actions,old.decisionProofs,fair),metrics:{id,change:'v2 control',referencePeak:route.peak,preparatoryTaps:route.prep}};
 }else for(let attempt=0;attempt<MAX;attempt++){
  if(attempt&&attempt%200===0)console.log(`L${id} search ${attempt}: ${JSON.stringify(rejected)}`);
  const level=definition(makeLevelArt(id),rng(id*935671+attempt*7727));if(!level||R.validate(level).length){reject('definition');continue;}
  const early=id<=9,isHard=HARD.has(id),greedy=Object.fromEntries(['leftmost','largest','smallest'].map(m=>[m,sample(level,m,1,id).wins]));
  if(isHard&&Object.values(greedy).some(Boolean)){reject('simple-hard');continue;}
  if([7,9].includes(id)&&greedy.largest&&greedy.leftmost){reject('early-simple');continue;}
  const solved=solve(R.create(level),{budget:isHard?20000:10000});if(solved.winning!==true){reject(solved.winning===null?'unknown':'unsolved');continue;}
  let fair=null,actions=solved.actions;
  if(level.pods.some(p=>p.mystery)){fair=fairSolve(level,{budget:25000});if(fair.winning!==true){reject('unfair');continue;}actions=fair.routes[0];}
  const route=replay(level,actions);if(!route)throw Error('Bad witness');
  if(id>=3&&early&&route.peak<1){reject('no-reserve');continue;}
  if([5,7,9].includes(id)&&route.prep<1){reject('no-preparation');continue;}
  if(isHard){const low=solve(R.create(level),{heldLimit:2,budget:30000});if(low.winning!==false){reject('weak-pressure');continue;}}
  const quick=sample(level,'random',24,id*997+attempt);
  if(id<=2&&quick.rate<.9){reject('intro-hard');continue;}
  if(early&&id>=3&&quick.rate<.2){reject('early-hard');continue;}
  if([5,7,9].includes(id)&&quick.rate>.85){reject('early-no-consequence');continue;}
  const decisions=isHard?decisionProofs(level,actions,{wanted:2,gap:3,budget:25000}):[5,7,9].includes(id)?earlyProof(level,actions):[];
  if(isHard&&(decisions.length<2||!decisions.some(p=>p.sameColourChoice))){reject('weak-decisions');continue;}
  if([5,7,9].includes(id)&&!decisions.length){reject('early-no-decision');continue;}
  const capacities={};if(early){
   capacities['3']={actions,fairness:fair?{worlds:fair.worlds,routes:fair.routes}:null};
   const four={...level,capacity:4},f4=level.pods.some(p=>p.mystery)?fairSolve(four,{budget:25000}):null,s4=f4||solve(R.create(four),{budget:10000});
   if(s4.winning!==true){reject('four-unproven');continue;}capacities['4']={actions:f4?f4.routes[0]:s4.actions,fairness:f4?{worlds:f4.worlds,routes:f4.routes}:null};
  }
  const policies=Object.fromEntries(MODES.map(m=>{const q=sample(level,m,64,id*913+attempt);return [m,{wins:q.wins,trials:q.trials,rate:q.rate}];}));
  if(isHard&&(policies.random.rate>.2||policies.release.rate>.45)){reject('weak-hard-policy');continue;}
  const losing=sample(level,'random',64,id*913+attempt).firstLoss;
  accepted={level,cert:{...certificate(level,actions,decisions,fair),capacities,losing},metrics:{id,attempt,change:early?'early queue and capacity':'redrawn picture',referencePeak:route.peak,preparatoryTaps:route.prep,policies}};break;
 }
 if(!accepted)throw Error(`L${id} no candidate: ${JSON.stringify(rejected)}`);
 levels.push(accepted.level);certs.push(accepted.cert);quality.push(accepted.metrics);
 const hash=sha(JSON.stringify(levels));
 await writeFile(resolve(OUT,'levels.json'),JSON.stringify({version:'pilot-30-v3',hash,levels},null,2)+'\n');
 await writeFile(resolve(OUT,'certificates.json'),JSON.stringify(certs,null,2)+'\n');
 await writeFile(resolve(OUT,'quality.json'),JSON.stringify({version:'pilot-30-v3',catalogHash:hash,complete:levels.length===30,limits:['Policy samples are construction diagnostics, not human win probabilities.','Control puzzles are retained; 3/4 places are an early experimental configuration.'],levels:quality},null,2)+'\n');
 console.log(`L${id}: ${accepted.metrics.change}, peak ${accepted.metrics.referencePeak}, preparation ${accepted.metrics.preparatoryTaps}, attempt ${accepted.metrics.attempt??'retained'}`);
}

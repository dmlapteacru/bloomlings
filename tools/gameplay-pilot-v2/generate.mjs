import {readFile,writeFile,mkdir} from 'node:fs/promises';
import {createHash} from 'node:crypto';
import {resolve,dirname} from 'node:path';
import {fileURLToPath} from 'node:url';
import R from './rules.cjs';
import {makeLevelArt} from './art.mjs';
import {solve,fairSolve,held,work,decisionProofs,bridgeProof,hiddenWorlds} from './search.mjs';
import {rng,sample,MODES} from './policies.mjs';

const ROOT=resolve(dirname(fileURLToPath(import.meta.url)),'../..');
const arg=(name,fallback)=>process.argv.find(a=>a.startsWith(`--${name}=`))?.slice(name.length+3)||fallback;
const OUT=resolve(ROOT,arg('out','content/pilot-v2'));
const ONLY=Number(arg('only','0')),FROM=ONLY||Number(arg('from','1')),TO=ONLY||Number(arg('to','30')),MAX=Number(arg('attempts','4000'));
const HARD=new Set([15,22,25,29]),MYSTERY=new Set([6,7,9,11,13,14,17,21,23,26,30]),RELIEF=new Set([16,20,28,30]);
const sha=s=>createHash('sha256').update(s).digest('hex');
const pick=(a,r)=>a[Math.floor(r()*a.length)];
const shuffle=(a,r)=>{a=a.slice();for(let i=a.length-1;i>0;i--){const j=Math.floor(r()*(i+1));[a[i],a[j]]=[a[j],a[i]];}return a;};
function groupCount(id){return id<10?0:id===10?1:id<15?2:HARD.has(id)?3:RELIEF.has(id)||id===18?1:2;}
function podCount(id){return id<=2?9:id<=4?10:id===5?12:id<10?14:id<15?18:id===15?20:RELIEF.has(id)?18:id<22?20:22;}

function definition(art,r){
 const id=art.id,n=podCount(id),counts=Object.fromEntries(R.CHANNELS.split('').map(c=>[c,0]));
 for(const c of art.rowsBottomFirst.join(''))if(c!=='#'&&c!=='.')counts[c]++;
 const colours=R.CHANNELS.split('').filter(c=>counts[c]>0),alloc=Object.fromEntries(colours.map(c=>[c,1])),minimum=id<5?10:6;
 while(Object.values(alloc).reduce((a,b)=>a+b,0)<n){
  const options=colours.filter(c=>counts[c]>=minimum*(alloc[c]+1));if(!options.length)throw new Error('Not enough work for capsule budget');
  const weights=options.map(c=>counts[c]/(alloc[c]+.4)**1.5),total=weights.reduce((a,b)=>a+b,0);let point=r()*total,chosen=options.at(-1);
  for(let i=0;i<options.length;i++){point-=weights[i];if(point<=0){chosen=options[i];break;}}alloc[chosen]++;
 }
 let mysteryColours=[],mysteryCount=0;
 if(MYSTERY.has(id)){
  const options=colours.filter(c=>alloc[c]>=2&&counts[c]>=18+minimum*(alloc[c]-1));
  if(options.length<2)return null;
  mysteryColours=shuffle(options,r).slice(0,2);mysteryCount=8+Math.floor(r()*9);
 }
 const pods=[];
 for(const color of colours){
  let total=counts[color],number=alloc[color];
  if(mysteryColours.includes(color)){pods.push({color,count:mysteryCount,mystery:true});total-=mysteryCount;number--;}
  const floor=Math.min(minimum,Math.floor(total/number));
  const weights=Array.from({length:number},()=>id<5?.7+r()*.6:HARD.has(id)?.18+r()**2*4:.35+r()*1.8),sum=weights.reduce((a,b)=>a+b,0);let remaining=total;
  for(let i=0;i<number;i++){const count=i===number-1?remaining:floor+Math.floor((total-floor*number)*weights[i]/sum);pods.push({color,count});remaining-=count;}
 }
 if(pods.length!==n||pods.some(p=>p.count<1))return null;
 const candidates=shuffle(pods.map((p,i)=>!p.mystery?i:-1).filter(i=>i>=0),r),groups=[];
 for(let g=0;g<groupCount(id);g++){
  if(candidates.length<2)return null;const a=candidates.pop(),bIndex=candidates.findIndex(p=>pods[p].color!==pods[a].color);if(bIndex<0)return null;
  const [b]=candidates.splice(bIndex,1),name=String.fromCharCode(65+g);pods[a].group=pods[b].group=name;groups.push([a,b].sort((x,y)=>x-y));
 }
 const singles=shuffle(pods.map((p,i)=>p.group?null:i).filter(i=>i!==null),r),pendingGroups=shuffle(groups,r);
 if(id===6)singles.sort((a,b)=>Number(Boolean(pods[a].mystery))-Number(Boolean(pods[b].mystery)));
 const columns=id<=5?3:4,tray=Array.from({length:columns},()=>[]);
 // Fill complete rows. Native Core requires both pair members at the same
 // original depth; after play begins their heads can advance independently.
 while(singles.length||pendingGroups.length){
  const target=Math.min(columns,singles.length+pendingGroups.length*2),patterns=[];
  for(let g=0;g<=Math.min(pendingGroups.length,Math.floor(target/2));g++)if(target-g*2<=singles.length)patterns.push(g);
  if(!patterns.length)return null;const g=pick(patterns,r),row=new Array(target).fill(-1);
  if(g===2){const a=pendingGroups.pop(),b=pendingGroups.pop();row.splice(0,4,...a,...b);}
  else if(g===1){const at=Math.floor(r()*(target-1)),pair=pendingGroups.pop();row[at]=pair[0];row[at+1]=pair[1];}
  for(let c=0;c<target;c++){if(row[c]<0)row[c]=singles.pop();tray[c].push(row[c]);}
 }
 return {...art,pods,tray,difficulty:HARD.has(id)?'Hard':'Normal'};
}

function replay(level,actions){let s=R.create(level),peak=0,turns=0,pairWaiting=0;for(const a of actions){const next=R.step(s,a);if(!next.accepted)return null;s=next.state;peak=Math.max(peak,held(s));if(held(s))turns++;if(a.type==='tap'&&s.meta.groups[a.pod].length===2&&s.meta.groups[a.pod].some(p=>s.remaining[p]>0))pairWaiting++;}return s.status==='Won'?{peak,turns,pairWaiting}:null;}

await mkdir(OUT,{recursive:true});let levels=[],certificates=[],quality=[];
if(FROM>1&&!ONLY){levels=JSON.parse(await readFile(resolve(OUT,'levels.json'))).levels.filter(l=>l.id<FROM);certificates=JSON.parse(await readFile(resolve(OUT,'certificates.json'))).filter(l=>l.id<FROM);quality=JSON.parse(await readFile(resolve(OUT,'quality.json'))).levels.filter(l=>l.id<FROM);}
for(let id=FROM;id<=TO;id++){
 const art=makeLevelArt(id),isHard=HARD.has(id);let accepted=null;const rejected={};
 const reject=reason=>{rejected[reason]=(rejected[reason]||0)+1;};
 for(let attempt=0;attempt<MAX;attempt++){
  if(attempt&&attempt%100===0)console.log(`L${id} search ${attempt}: ${JSON.stringify(rejected)}`);
  const r=rng(id*935671+attempt*7727),level=definition(art,r);if(!level||R.validate(level).length){reject('definition');continue;}
  const greedy=Object.fromEntries(['largest','leftmost','smallest'].map(m=>[m,sample(level,m,1,id)]));
  if(isHard&&Object.values(greedy).some(v=>v.wins)){reject('simple-policy');continue;}
  if(id>=11&&!RELIEF.has(id)&&!isHard&&id!==18&&greedy.largest.wins&&greedy.leftmost.wins){reject('both-simple');continue;}
  const witness=solve(R.create(level),{budget:isHard?15000:6000,requireBothBridges:Boolean(level.hero&&id!==18)});
  if(witness.winning!==true){reject(witness.winning===null?'solve-unknown':'unsolved');continue;}
  const route=replay(level,witness.actions);if(!route)throw new Error('Solver witness did not replay');
  if(id>=10&&!RELIEF.has(id)&&id!==18&&(!route.pairWaiting||route.peak<2)){reject('no-waiting');continue;}
  if(isHard){const low=solve(R.create(level),{budget:22000,heldLimit:2});if(low.winning!==false){reject(low.winning?'low-pressure-path':'pressure-unknown');continue;}}
  const quick=sample(level,'random',16,id*997+attempt),release=sample(level,'release',12,id*313+attempt);
  if(id<=3&&quick.rate<.85){reject('intro-too-hard');continue;}
  if(id>=4&&id<=9&&quick.rate<.3){reject('early-too-hard');continue;}
  if([7,9].includes(id)&&quick.rate>.8){reject('mystery-practice-too-easy');continue;}
  if(isHard&&(quick.rate>.25||release.rate>.4)){reject('weak-hard');continue;}
  if(!isHard&&id>=10&&quick.rate<.12){reject('normal-too-hard');continue;}
  let actions=witness.actions,fair=null;
  if(MYSTERY.has(id)){
   fair=fairSolve(level,{budget:16000});if(fair.winning!==true){reject(fair.winning===null?'fair-unknown':'unfair');continue;}actions=fair.routes[0];
   const firstReveal=actions.findIndex(a=>a.type==='tap'&&level.pods[a.pod].mystery);
   if(firstReveal<0||firstReveal>actions.length*.7){reject('late-mystery');continue;}
   if(id!==6){let s=R.create(level),waited=false;for(const a of actions){s=R.step(s,a).state;if(a.type==='tap'&&level.pods[a.pod].mystery&&s.remaining[a.pod]>0)waited=true;}if(!waited){reject('mystery-has-no-slot-cost');continue;}}
  }
  const bridge=bridgeProof(level,actions);
  if(level.hero&&id!==18&&(!bridge||bridge.sameOrderWins||bridge.alternateWinning===null)){reject('cosmetic-bridge');continue;}
  const decisions=isHard?decisionProofs(level,actions,{wanted:2,gap:3,budget:18000}):[];
  if(isHard&&(decisions.length<2||!decisions.some(d=>d.sameColourChoice))){reject('no-real-decisions');continue;}
  const metrics=Object.fromEntries(MODES.map(mode=>{const s=sample(level,mode,64,id*913+attempt);return [mode,{wins:s.wins,trials:s.trials,rate:s.rate}];}));
  if(isHard&&(metrics.random.rate>.2||metrics.release.rate>.45)){reject('full-weak-hard');continue;}
  const original=sample(level,'random',64,id*913+attempt),played=replay(level,actions);
  accepted={level,cert:{id,actions,losing:original.firstLoss,decisionProofs:decisions,bridge,fairness:fair?{worlds:fair.worlds,nodes:fair.nodes,routes:fair.routes}:null},metrics:{id,attempt,pods:level.pods.length,units:level.pods.length-groupCount(id),groups:groupCount(id),mystery:level.pods.filter(p=>p.mystery).length,referencePeak:played.peak,referenceWaitingTurns:played.turns,minimumPeakAtLeast:isHard?3:null,decisionEpisodes:decisions.length,bridgeChoiceChangesContinuation:bridge?!bridge.sameOrderWins:null,policies:metrics}};break;
 }
 if(!accepted)throw new Error(`L${id} no accepted candidate in ${MAX}: ${JSON.stringify(rejected)}`);
 levels.push(accepted.level);certificates.push(accepted.cert);quality.push(accepted.metrics);
 const hash=sha(JSON.stringify(levels));
 await writeFile(resolve(OUT,'levels.json'),JSON.stringify({version:'pilot-30-v2',hash,levels},null,2)+'\n');
 await writeFile(resolve(OUT,'certificates.json'),JSON.stringify(certificates,null,2)+'\n');
 await writeFile(resolve(OUT,'quality.json'),JSON.stringify({version:'pilot-30-v2',catalogHash:hash,complete:levels.length===30,limits:['Policy samples are not human win probabilities.','Held-slot lower bounds and decision losses are exact only when the bounded search completes.','Mystery fairness covers every assignment consistent with the two equal-count hidden capsules and public exact work totals.'],levels:quality},null,2)+'\n');
 console.log(`L${id} ${accepted.level.difficulty}: ${accepted.metrics.pods} caps / ${accepted.metrics.units} choices, pairs ${accepted.metrics.groups}, hidden ${accepted.metrics.mystery}, peak ${accepted.metrics.referencePeak}, random ${accepted.metrics.policies.random.rate.toFixed(2)}, decisions ${accepted.metrics.decisionEpisodes}, bridge ${accepted.metrics.bridgeChoiceChangesContinuation}; seed ${accepted.metrics.attempt}`);
}
console.log(`Saved ${levels.length} levels to ${OUT}`);

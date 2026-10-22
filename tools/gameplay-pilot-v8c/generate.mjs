// Fast settled screening, then acceptance and evidence using the live engine.
import fs from 'node:fs';import crypto from 'node:crypto';
import R from './rules.cjs';import T from './timeline.cjs';const {Timeline}=T;import {definition} from './author.mjs';import {makeLevelArt} from './art.mjs';
import {sample as screenSample,rng} from './screen-policies.mjs';import {solve as screenSolve,fairSolve as screenFair} from './screen-search.mjs';
import {solve,fairSolve,hiddenWorlds} from './search.mjs';
const old=JSON.parse(fs.readFileSync('content/pilot-v7/levels.json')),oldCerts=JSON.parse(fs.readFileSync('content/pilot-v7/certificates.json'));
const out='content/pilot-v8c';fs.mkdirSync(out,{recursive:true});
export const CHANGED=new Set([4,5,6,7,8,9,12,14,15,16,17,19,23,25,26,28,30]);
const CONTROL=[10,20,22,29],bounds={4:[.9,1],5:[.35,.8],6:[.7,1],7:[.3,.7],8:[.3,.65],9:[.2,.6],12:[.2,.65],14:[.25,.65],15:[.1,.45],16:[.4,.75],17:[.35,.75],19:[.2,.6],23:[.35,.7],25:[.02,.2],26:[.2,.55],28:[.35,.7],30:[.45,.8]};
function replay(l,actions){const t=new Timeline(R.create(l)),reveals=[];let preparations=0,partialTurns=0,peak=0;
 for(let i=0;i<actions.length;i++){const a=actions[i];if(t.check(a))return null;const before=t.processedCells,free=t.free,r=t.commit(a);for(const e of r.events.filter(e=>e.type==='reveal'))reveals.push({pod:e.pod,at:i+1,mode:e.mode,freeBefore:free,clearedFraction:before/l.pods.reduce((n,p)=>n+p.count,0)});peak=Math.max(peak,5-t.free);t.advance(1e9);if(t.processedCells===before)preparations++;if(t.state.slots.some(p=>p>=0&&t.state.remaining[p]>0&&t.state.remaining[p]<l.pods[p].count))partialTurns++;}
 return t.status==='Won'?{modelMs:t.now,preparations,partialTurns,peak,reveals}:null;
}
function policy(l,mode,seed,{burst=false}={}){
 const t=new Timeline(R.create(l)),random=rng(seed);let turns=0;
 while(t.state.status==='Playing'&&turns++<10000){let moves=R.legal(t.state);if(!moves.length){if(!t.pending)break;t.advance(t.steps[0].at-t.now);continue;}
  const bridges=moves.filter(a=>a.type==='bridge');if(bridges.length)moves=bridges;
  else if(mode==='visible'){
   const front=new Set(R.reach(t.state).targets.filter(c=>t.reserved[c]<0).map(c=>t.state.board[c]));
   const match=moves.filter(a=>t.state.meta.groups[a.pod].some(p=>t.state.revealed[p]&&front.has(t.state.meta.colours[p])));if(match.length)moves=match;
  }
  const a=mode==='left'?moves[0]:mode==='right'?moves.at(-1):mode==='large'?moves.reduce((a,b)=>a.type==='bridge'||t.state.remaining[a.pod]>=t.state.remaining[b.pod]?a:b):moves[Math.floor(random()*moves.length)];
  t.commit(a);if(!burst)t.advance(1e9);
 }
 t.advance(1e9);return t.status==='Won';
}
const levels=[],certs=[],quality=[];
for(const original of old.levels){
 let level=structuredClone(original),actions=oldCerts[level.id-1].actions,attempt=-1,fairness=null;
 if(CHANGED.has(level.id)){
  const art=makeLevelArt(level.id),[lo,hi]=bounds[level.id];let found=false;
  for(attempt=0;attempt<2500;attempt++){
   const candidate=definition(art,rng(level.id*91753+attempt*9127));if(!candidate||R.validate(candidate).length)continue;
   const q=screenSample(candidate,'random',24,level.id*97+attempt);if(q.rate<lo||q.rate>hi)continue;
   if([8,16,23,26,28].includes(level.id)&&screenSample(candidate,'leftmost',1).wins&&screenSample(candidate,'largest',1).wins)continue;
   const s=screenSolve(R.create(candidate),{budget:4000});if(s.winning!==true)continue;let route=s.actions;
   if(candidate.pods.some(p=>p.mystery)){let f;try{f=screenFair(candidate,{budget:8000});}catch{continue;}if(f.winning!==true)continue;route=f.routes[0];}
   let reference=replay(candidate,route);if(!reference)continue;
   if(![4,6].includes(level.id)&&reference.preparations===0&&reference.partialTurns===0)continue;
   // Reveal must occur before the solved tail, while there are still decisions.
   if(reference.reveals.length&&reference.reveals.some(e=>e.clearedFraction>.7||e.at>route.length-3))continue;
   let liveWins=0;for(let i=0;i<16;i++)liveWins+=policy(candidate,'random',level.id*197+i*7919);
   if(liveWins/16<lo||liveWins/16>hi)continue;
   if([8,16,23,26,28].includes(level.id)){
    let randomWins=0,visibleWins=0;for(let i=0;i<32;i++){randomWins+=policy(candidate,'random',level.id*999+i*7919);visibleWins+=policy(candidate,'visible',level.id*999+i*7919);}
    if(randomWins/32<lo||randomWins/32>hi||visibleWins/32>(level.id===26?.8:.85))continue;
   }
   if(candidate.pods.some(p=>p.mystery)){
    let f;try{f=fairSolve(candidate,{budget:6000});}catch{continue;}if(f.winning!==true)continue;
    fairness={scope:'common observation policy for both equal-count hidden assignments; idle decisions',worlds:f.worlds,routes:f.routes};route=f.routes[0];reference=replay(candidate,route);
    if(!reference||reference.reveals.some(e=>e.clearedFraction>.7||e.at>route.length-3||e.freeBefore<2))continue;
    const worlds=hiddenWorlds(candidate);if(f.routes.some((route,w)=>{const q=replay(worlds[w],route);return !q||q.reveals.some(e=>e.freeBefore<2||e.clearedFraction>.7||e.at>route.length-3);}))continue;
    // Beyond the introductory level, at least one reveal is chosen with a
    // meaningful reserve budget (two/three places, or a pair with room for it).
    if(level.id!==6&&!reference.reveals.some(e=>e.freeBefore<=3&&e.freeBefore>=2))continue;
   }
   level=candidate;actions=route;found=true;break;
  }
  if(!found)throw Error('No accepted candidate for L'+level.id);
 }
 const reference=replay(level,actions);if(!reference)throw Error('No live route L'+level.id);
 const metrics={};for(const mode of ['random','visible','left','right','large']){const trials=['left','right','large'].includes(mode)?1:32;let wins=0;for(let i=0;i<trials;i++)wins+=policy(level,mode,level.id*999+i*7919);metrics[mode]={wins,trials,rate:wins/trials};}
 let burstWins=0;for(let i=0;i<16;i++)burstWins+=policy(level,'random',level.id*313+i*7919,{burst:true});
 levels.push(level);certs.push({id:level.id,actions,schedule:'wait until work idle between commands',fairness});quality.push({id:level.id,changed:CHANGED.has(level.id),ownerFunControl:CONTROL.includes(level.id),attempt,reference,policies:metrics,burstRandom:{wins:burstWins,trials:16,rate:burstWins/16}});
 const hash=crypto.createHash('sha256').update(JSON.stringify(levels)).digest('hex');
 fs.writeFileSync(out+'/levels.json',JSON.stringify({version:'pilot-30-v8',hash,levels},null,2)+'\n');fs.writeFileSync(out+'/certificates.json',JSON.stringify(certs,null,2)+'\n');fs.writeFileSync(out+'/quality.json',JSON.stringify({version:'pilot-30-v8',catalogHash:hash,sourceHash:old.hash,complete:levels.length===30,changedLevels:[...CHANGED],controls:CONTROL,limits:['Construction policy samples are not human win rates.','Fairness covers the two possible equal-count hidden-colour assignments; requires idle decisions, not arbitrary real-time input.','Different input timing can change same-colour allocation. No native C# parity claimed.'],levels:quality},null,2)+'\n');
 console.log('L'+level.id,CHANGED.has(level.id)?'new':'retained','candidate',attempt,'random',metrics.random.rate,'visible',metrics.visible.rate,'sealed',level.pods.filter(p=>p.mystery==='sealed').length,'fair',fairness?.worlds||0);
}

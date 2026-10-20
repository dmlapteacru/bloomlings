// v5 analytical rules cheaply screen candidates. Acceptance and metrics run on
// the authoritative v6 timeline. Never interpret an idle-only failure as a
// proof of impossibility with concurrent timed inputs.
import fs from 'node:fs';import crypto from 'node:crypto';
import R from './rules.cjs';import T from './timeline.cjs';const {Timeline}=T;
import {definition} from './author.mjs';
import {solve as screenSolve,fairSolve as screenFair} from '../gameplay-pilot-v5/search.mjs';
import {sample as screenSample,rng} from '../gameplay-pilot-v5/policies.mjs';
import {solve,fairSolve} from './search.mjs';
const old=JSON.parse(fs.readFileSync('content/pilot-v5/levels.json')),oldCerts=JSON.parse(fs.readFileSync('content/pilot-v5/certificates.json'));
const out='content/pilot-v6';fs.mkdirSync(out,{recursive:true});
const CHANGED=new Set([4,5,7,13,16,18,19]),CONTROL=[10,15,21,22,25,29];
function replay(l,actions,{burst=false}={}){const t=new Timeline(R.create(l));let stalled=0,prep=0,peak=0;
 for(const a of actions){while(t.check(a)&&t.pending)t.advance(t.steps[0].at-t.now);if(t.check(a))return null;const before=t.processedCells;t.commit(a);peak=Math.max(peak,5-t.free);if(!burst){t.advance(1e9);if(t.processedCells===before&&a.type==='tap')prep++;}if(t.free<5&&!t.pending)stalled++;}
 t.advance(1e9);return t.state.status==='Won'?{modelMs:t.now,peak,stalled,preparations:prep}:null;
}
function runPolicy(l,mode,seed,{burst=false}={}){
 const t=new Timeline(R.create(l)),random=rng(seed),actions=[];let guard=0;
 while(t.state.status==='Playing'&&guard++<30000){
  let moves=R.legal(t.state);if(!moves.length){if(!t.pending)break;t.advance(t.steps[0].at-t.now);continue;}
  const bridges=moves.filter(a=>a.type==='bridge');if(bridges.length)moves=bridges;
  else if(mode!=='random'){
   const visible=new Set(R.reach(t.state).targets.filter(c=>t.reserved[c]<0).map(c=>t.state.board[c]));
   const matching=moves.filter(a=>t.state.meta.groups[a.pod].some(p=>visible.has(t.state.meta.colours[p])));if(matching.length)moves=matching;
  }
  let action;
  if(mode==='left')action=moves[0];else if(mode==='right')action=moves.at(-1);else if(mode==='large')action=moves.reduce((a,b)=>a.type==='bridge'||t.state.remaining[a.pod]>=t.state.remaining[b.pod]?a:b);else action=moves[Math.floor(random()*moves.length)];
  actions.push(action);t.commit(action);if(!burst)t.advance(1e9);
 }
 t.advance(1e9);return {win:t.state.status==='Won',actions,modelMs:t.now};
}
const levels=[],certs=[],quality=[];
for(const original of old.levels){let level=structuredClone(original),actions=oldCerts[original.id-1].actions,attempt=-1;
 if(CHANGED.has(level.id)){
  const art={...original};delete art.pods;delete art.tray;
  const bounds=({4:[.9,1],5:[.35,.8],7:[.25,.7],13:[.3,.75],16:[.8,1],18:[.25,.6],19:[.25,.65]})[level.id];
  let found=false;
  for(attempt=0;attempt<2500;attempt++){
   const c=definition(art,rng(level.id*71293+attempt*9127));if(!c||R.validate(c).length)continue;
   const sample=screenSample(c,'random',24,level.id*97+attempt);if(sample.rate<bounds[0]||sample.rate>bounds[1])continue;
   if([5,7,13,18,19].includes(level.id)&&screenSample(c,'leftmost',1).wins&&screenSample(c,'largest',1).wins)continue;
   const solved=screenSolve(R.create(c),{budget:6000});if(solved.winning!==true)continue;
   let route=solved.actions;
   if(c.pods.some(p=>p.mystery)){const f=screenFair(c,{budget:10000});if(f.winning!==true)continue;route=f.routes[0];}
   const reference=replay(c,route);if(!reference)continue;
   if([5,7,13,18,19].includes(level.id)&&reference.preparations===0)continue;
   // Screen with actual timed rules, not v5 numerical proxies.
   let wins=0;for(let n=0;n<16;n++)wins+=runPolicy(c,'random',level.id*197+n*7919).win;
   if(wins/16<bounds[0]||wins/16>bounds[1])continue;
   if([13,18,19].includes(level.id)){let visibleWins=0;for(let n=0;n<16;n++)visibleWins+=runPolicy(c,'visible',level.id*197+n*7919).win;
    if(visibleWins<8||visibleWins>14)continue;}

   level=c;actions=route;found=true;break;
  }
  if(!found)throw Error(`No candidate L${original.id}`);
 }
 if(!replay(level,actions)){const s=solve(R.create(level),{budget:2000});if(!s.winning)throw Error(`No v6 route L${level.id}`);actions=s.actions;}
 // A hidden-world information policy must exist under the actual new rules.
 let fairness=null;if(level.pods.some(p=>p.mystery)){const f=fairSolve(level,{budget:12000});if(f.winning!==true)throw Error(`No fair v6 policy L${level.id}`);fairness={scope:'two equal-count hidden assignments; idle decisions',worlds:f.worlds,routes:f.routes};actions=f.routes[0];}
 const metrics={};for(const mode of ['random','visible','left','right','large']){
  const trials=['left','right','large'].includes(mode)?1:32;let wins=0;for(let i=0;i<trials;i++)wins+=runPolicy(level,mode,level.id*999+i*7919).win;
  metrics[mode]={wins,trials,rate:wins/trials};
 }
 let burstWins=0;for(let i=0;i<16;i++)burstWins+=runPolicy(level,'random',level.id*313+i*7919,{burst:true}).win;
 const reference=replay(level,actions),burstReference=replay(level,actions,{burst:true});
 levels.push(level);certs.push({id:level.id,actions,schedule:'wait until work idle between commands',fairness});
 quality.push({id:level.id,changed:CHANGED.has(level.id),ownerFunControl:CONTROL.includes(level.id),attempt,reference,burstReference,policies:metrics,burstRandom:{wins:burstWins,trials:16,rate:burstWins/16}});
 const hash=crypto.createHash('sha256').update(JSON.stringify(levels)).digest('hex');
 fs.writeFileSync(out+'/levels.json',JSON.stringify({version:'pilot-30-v6',hash,levels},null,2)+'\n');fs.writeFileSync(out+'/certificates.json',JSON.stringify(certs,null,2)+'\n');fs.writeFileSync(out+'/quality.json',JSON.stringify({version:'pilot-30-v6',catalogHash:hash,complete:levels.length===30,sourceHash:old.hash,changedLevels:[...CHANGED],controls:CONTROL,limits:['Policies are construction diagnostics, not human win rates.','Idle certificates prove existence of a real v6 winning route; they do not prove every timing wins.','Same-colour scheduling can change outcomes with concurrent input. No native C# parity claimed.'],levels:quality},null,2)+'\n');
 console.log('L'+level.id,CHANGED.has(level.id)?'new':'retained','candidate',attempt,'random',metrics.random.rate,'visible',metrics.visible.rate,'reference',Math.round(reference.modelMs/1000),'burst',burstReference?Math.round(burstReference.modelMs/1000):'different schedule required');
}

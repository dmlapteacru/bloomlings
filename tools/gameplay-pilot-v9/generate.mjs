// v9: six isolated experiments. Do not rewrite or regenerate a v8 catalog.
import fs from 'node:fs';import crypto from 'node:crypto';
import R from './rules.cjs';import T from './timeline.cjs';import {sceneLevel} from './scenes.mjs';
import {definition} from './author.mjs';import {rng,rate,patternWins,sensible,earlyRisk,report} from './botsc.mjs';
import {planner,withMysteries,MYSTERY,reveals,revealsFit} from './fix-lib.mjs';import {replayRhythm,compareRhythm} from './rhythm.mjs';import {playPlanner} from './horizon.mjs';
export const CHANGED=[4,10,12,13,16,25];
export const TARGETS={
 4:{maxQuota:40,role:'tutorial',random:[.95,1],visible:[.98,1],patterns:9,maxWait:16,maxSeconds:90,order:[0,.4]},
 10:{maxQuota:60,role:'superHard',random:[.05,.12],visible:[0,.12],patterns:2,maxWait:45,maxSeconds:285,order:[.4,3]},
 12:{maxQuota:64,role:'normal',random:[.30,.60],visible:[.60,.88],patterns:4,maxWait:28,maxSeconds:120,order:[.3,4]},
 13:{maxQuota:64,role:'normal',random:[.25,.65],visible:[.55,.90],patterns:5,maxWait:28,maxSeconds:120,order:[.3,4]},
 16:{maxQuota:40,role:'relief',random:[.65,1],visible:[.85,1],patterns:9,maxWait:16,maxSeconds:70,order:[0,2]},
 25:{maxQuota:50,role:'hard',random:[.10,.20],visible:[.10,.30],patterns:3,maxWait:35,maxSeconds:210,order:[.5,4]},
};
const label=role=>role==='hard'?'Hard':role==='superHard'?'Super Hard':'Normal';
const inside=(x,[l,h],slack=0)=>x>=l-slack&&x<=h+slack;
const root='content/pilot-v9';fs.mkdirSync(root+'/work',{recursive:true});
export function search(id,{from=0,limit=1500,banned=[]}={}){
 const bans=[...new Set([...(id===13?['самое маленькое число']:id===25?['самое маленькое число']:[]),...banned])];
 const cfg=TARGETS[id];if(!cfg)throw Error('v9 only authors explicitly changed experiments');
 const art={...sceneLevel(id),id,subject:'v9-'+id},stats={tried:0,bots:0,planner:0,mysteries:0,rhythm:0};
 for(let attempt=from;attempt<from+limit;attempt++){
  const rand=rng(id*23017+attempt*97),order=cfg.order[0]+rand()*(cfg.order[1]-cfg.order[0]);
  const extra=[0,1,-1,2][Math.floor(attempt/100)%4];
  const l=definition(art,rand,{role:cfg.role,difficulty:label(cfg.role),extra,order});if(!l||R.validate(l).length)continue;stats.tried++;
  if(attempt%25===0)console.error('L'+id,'attempt',attempt,JSON.stringify(stats));
  if(!inside(rate(l,'random',40,id*113),cfg.random,.10)||!inside(rate(l,'visible',40,id*127),cfg.visible,.10)){stats.bots++;continue;}
  const patterns=patternWins(l);if(patterns.length>cfg.patterns||patterns.some(p=>bans.includes(p))){stats.bots++;continue;}
  const random=rate(l,'random',300),visible=rate(l,'visible',300);
  if(!inside(random,cfg.random)||!inside(visible,cfg.visible)){stats.bots++;continue;}
  console.error('L'+id,'candidate',attempt,'rates',random,visible,'planner');
  let chosen=l,won=planner(l),mystery=null;if(!won){stats.planner++;continue;}
  if(MYSTERY[id]){try{const m=withMysteries(l,MYSTERY[id],{tries:25});chosen=m.level;won=m.planner;mystery={hidden:m.hidden,keys:m.keys,reveals:m.reveals};}catch{stats.mysteries++;continue;}}
  console.error('L'+id,'candidate',attempt,'live replay');
  let brisk=null;
  for(const opts of [won.opts,...[1,2,3].flatMap(depth=>[false,true].map(reverse=>({rows:3,depth,weight:12,reverse}))),{rows:3,depth:4,weight:30,stall:4},{rows:3,depth:4,weight:50,stall:8}]){
   const route=JSON.stringify(opts)===JSON.stringify(won.opts)?won:playPlanner(chosen,opts);
   if(!route.actions||route.won===false)continue;
   const attemptRhythm=replayRhythm(chosen,route.actions,{schedule:'brisk'});
   if(!attemptRhythm.won||attemptRhythm.maxNoChoiceSeconds>cfg.maxWait||attemptRhythm.seconds>cfg.maxSeconds){console.error('L'+id,'route rejected',JSON.stringify(opts),JSON.stringify({won:attemptRhythm.won,why:attemptRhythm.reason,seconds:attemptRhythm.seconds,wait:attemptRhythm.maxNoChoiceSeconds}));continue;}
   if(!replayRhythm(chosen,route.actions,{schedule:'brisk',manualSpeed:2}).won)continue;
   if(MYSTERY[id]){const list=reveals(chosen,route.actions);if(!list||!revealsFit(chosen,list))continue;mystery.reveals=list;}
   won={opts,actions:route.actions};brisk=attemptRhythm;break;
  }
  if(!brisk){console.error("L"+id,"candidate",attempt,"rejected live schedule");stats.rhythm++;continue;}
  console.error('L'+id,'candidate',attempt,'early risk / finalist');
  fs.writeFileSync(root+'/work/eligible-'+id+'.json',JSON.stringify({id,role:cfg.role,attempt,level:chosen,actions:won.actions,planner:won.opts,mystery,stats}));
  const early=['hard','superHard'].includes(cfg.role)?earlyRisk(chosen,1000):null;
  if(early!==null&&early>.10){stats.bots++;continue;}
  return {id,role:cfg.role,attempt,banned:bans,level:chosen,actions:won.actions,planner:won.opts,mystery,earlyRisk:early,
   bots:{...report(chosen,300),patternWins:patterns},heldOut:{seed:970003+id*31,trials:200,random:rate(chosen,'random',200,970003+id*31),visible:rate(chosen,'visible',200,970003+id*31)},rhythm:compareRhythm(chosen,won.actions),stats,
   plannerTopRowOnly:playPlanner(chosen,{...won.opts,rows:1}).won};
 }
 throw Error('Search budget reached L'+id+' '+JSON.stringify(stats));
}
function assemble(){
 const base=JSON.parse(fs.readFileSync('content/pilot-v9/baseline-v8e-c6b0bfd4/levels.json')),bc=JSON.parse(fs.readFileSync('content/pilot-v9/baseline-v8e-c6b0bfd4/certificates.json')),bq=JSON.parse(fs.readFileSync('content/pilot-v9/baseline-v8e-c6b0bfd4/quality.json'));
 const found=new Map(CHANGED.map(id=>[id,JSON.parse(fs.readFileSync(root+'/work/L'+id+'.json'))]));
 for(const [id,f] of found){const cfg=TARGETS[id];if(!inside(f.bots.random,cfg.random)||!inside(f.bots.visible,cfg.visible)||f.bots.patternWins.length>cfg.patterns||f.level.pods.some(p=>p.count>cfg.maxQuota)||!replayRhythm(f.level,f.actions,{schedule:'brisk'}).won||!replayRhythm(f.level,f.actions,{schedule:'brisk',manualSpeed:2}).won||([13,25].includes(id)&&f.bots.patternWins.includes('самое маленькое число')))throw Error('Stale or invalid finalist L'+id);}
 const levels=base.levels.map(l=>found.get(l.id)?.level||l),hash=crypto.createHash('sha256').update(JSON.stringify(levels)).digest('hex');
 const certificates=bc.map(c=>found.has(c.id)?{id:c.id,actions:found.get(c.id).actions,planner:found.get(c.id).planner,schedule:'patient live replay; no access beyond 3 visible rows',fairness:{scope:'visible-horizon policy witness, not universal no-guess proof'}}:c);
 const quality={version:'pilot-30-v9',complete:true,catalogHash:hash,base:{version:base.version,hash:base.hash},changed:CHANGED,targets:TARGETS,tail:T.TAIL,roleOf:bq.roleOf,
  limits:['All bot rates and wall times describe the specified pilot policy, not human probabilities or competitor physics.','Brisk schedule validated separately: a patient win does not guarantee a concurrent win.','earlyRisk retains inherited optimistic budget handling; report it as a diagnostic, not a universal fairness proof.'],
  levels:levels.map(l=>{const c=certificates.find(c=>c.id===l.id),f=found.get(l.id);return {id:l.id,title:l.title,role:bq.roleOf[l.id],changed:Boolean(f),controlHash:!f?crypto.createHash('sha256').update(JSON.stringify(l)).digest('hex'):null,
   cells:l.pods.reduce((n,p)=>n+p.count,0),colours:new Set(l.pods.map(p=>p.color)).size,pods:l.pods.length,maxQuota:Math.max(...l.pods.map(p=>p.count)),
   bots:f?.bots||bq.levels.find(q=>q.id===l.id).bots,heldOut:f?.heldOut||null,earlyRisk:f?.earlyRisk??bq.levels.find(q=>q.id===l.id).earlyRisk,
   attempt:f?.attempt??null,banned:f?.banned||[],earlyRiskNodeBudget:f?1000:60000,earlyRiskIsExact:false,planner:c.planner,plannerTopRowOnly:f?.plannerTopRowOnly??bq.levels.find(q=>q.id===l.id).planner.winsSeeingTopRowOnly,mystery:f?.mystery??bq.levels.find(q=>q.id===l.id).mystery,
   rhythm:compareRhythm(l,c.actions)};})};
 fs.writeFileSync(root+'/levels.json',JSON.stringify({version:'pilot-30-v9',hash,baseVersion:base.version,baseHash:base.hash,changedLevels:CHANGED,levels},null,2)+'\n');
 fs.writeFileSync(root+'/certificates.json',JSON.stringify(certificates,null,2)+'\n');fs.writeFileSync(root+'/quality.json',JSON.stringify(quality,null,2)+'\n');
 console.log('Assembled v9',hash);
}
if(import.meta.url===`file://${process.argv[1]}`){const [cmd,arg,arg2,arg3]=process.argv.slice(2);if(cmd==='level'){const f=search(+arg,{from:+(arg2||0),banned:JSON.parse(arg3||'[]')});fs.writeFileSync(root+'/work/L'+arg+'.json',JSON.stringify(f,null,2)+'\n');console.log('L'+arg,f.attempt,f.bots.random,f.bots.visible,f.rhythm.brisk1x.seconds);}else if(cmd==='assemble')assemble();else throw Error('Usage: generate.mjs level ID [from] | assemble');}

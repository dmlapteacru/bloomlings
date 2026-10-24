// v8E levels: the new pictures (scenes.mjs) with capsules built to each level's role (author.mjs).
// Rules agreed with the owner on 2026-10-23 after the Colony Flow comparison (product/audits/colony-flow-vs-v8d-bots-2026-10-23.md):
// - Super Hard ~6-12% random taps (CF L10 and L20: 11-12%) and visible-colour play winning at most 30%: after the
//   v8E playtest (L10 and L29 felt like Hard; L22, at 7% and 18%, was "a good level");
// - Hard up to 3 mindless patterns, Super Hard up to 2; no pattern wins more than 40% of the pilot from L5;
// - Hard and Super Hard: random taps make the level unwinnable within the first two commands at most 10% of the time;
// - links and "?" mostly on the hard levels; each Super Hard on its own axis (L10 "?", L22 links, L29 big capsules);
// - every level is won by the planner that sees only the visible rows (v8D fairness);
// - round length at 1× (the default once ×2 is paid): Normal up to ~1.5 min, Hard ~2.5, Super Hard 3.5-4 (ROLES.seconds1x).
// Usage: node fix.mjs level <id> [from]   (writes content/pilot-v8e/work/L<id>.json)
//        node fix.mjs assemble            (pattern limits, then levels.json, certificates.json, quality.json)
import fs from 'node:fs';import crypto from 'node:crypto';
import R from './rules.cjs';import T from './timeline.cjs';const {Timeline}=T;
import {sceneLevel,ROLE_OF} from './scenes.mjs';import {definition} from './author.mjs';
import {rng,rate,sensible,report,earlyRisk,patternWins} from './botsc.mjs';import {ROWS,playPlanner} from './horizon.mjs';
import {MYSTERY,PLANNERS,planner,withMysteries,strip} from './fix-lib.mjs';
const out='content/pilot-v8e',work=out+'/work';fs.mkdirSync(work,{recursive:true});
export const ROLES={
 // seconds1x: the round length window at 1× (the default once ×2 is paid; the owner's targets of 2026-10-23: Normal
 // 1-1.5 min, Hard 2.5, Super Hard 3.5-4; checked at 1× since the path round the picture lengthened the walks on
 // 2026-10-24); small boards may be short, so only Hard and Super Hard have a lower bound. order: the author's
 // outside-in dial range (author.mjs).
 tutorial:{label:'Normal',random:[.95,1],visible:[.98,1],patterns:9,sensible:4,seconds1x:[0,60],order:[0,.3]},
 relief:{label:'Normal',random:[.60,.90],visible:[.85,1],patterns:6,sensible:4,seconds1x:[0,60],order:[0,2]},
 normal:{label:'Normal',random:[.35,.65],visible:[.70,.95],patterns:4,sensible:3,seconds1x:[0,100],order:[0,4]},
 task:{label:'Normal',random:[.20,.40],visible:[.45,.75],patterns:3,sensible:2,seconds1x:[0,130],order:[0,4]},
 hard:{label:'Hard',random:[.15,.25],visible:[.20,.65],patterns:3,sensible:2,seconds1x:[130,180],order:[0,3]},
 superHard:{label:'Super Hard',random:[.06,.12],visible:[0,.30],patterns:2,sensible:2,seconds1x:[190,250],order:[0,3]},
};
export const PATTERN_SHARE=.40,HARD_PATTERN_LEVELS=2,EARLY_LIMIT=.10;
const roleOf=id=>ROLE_OF[id]||'tutorial',isHard=id=>['hard','superHard'].includes(roleOf(id));
const inside=(v,[lo,hi],slack=0)=>v>=lo-slack-1e-9&&v<=hi+slack+1e-9;
// Round length as the owner plays (×2, commands as soon as a place frees, ~1.2 s to decide): calibrated on the owner's
// v8C wins (0.6 of a brisk 1× replay); the auto-tail runs at ×4.
export function seconds(level,actions){const t=new Timeline(R.create(level));
 for(const a of actions){t.advance(1200);while(t.check(a)&&t.pending)t.advance(Math.max(1,t.steps[0].at-t.now));if(t.check(a))return null;t.commit(a);}
 const main=t.now;t.advance(1e9);if(t.status!=='Won')return null;return Math.round((.6*main+(t.now-main)/4)/100)/10;}
// The same brisk replay at 1× (the default speed once ×2 is paid), the auto-tail at ×4.
export function secondsAt1x(level,actions){const t=new Timeline(R.create(level));
 for(const a of actions){t.advance(1200);while(t.check(a)&&t.pending)t.advance(Math.max(1,t.steps[0].at-t.now));if(t.check(a))return null;t.commit(a);}
 const main=t.now;t.advance(1e9);return t.status==='Won'?Math.round((main+(t.now-main)/4)/1000):null;}
// 1× length; when the brisk replay plays out differently, from the patient route (as in quality.json).
const at1x=(level,actions)=>secondsAt1x(level,actions)??Math.round((patientSeconds(level,actions)??0)*.25/.6);
function patientSeconds(level,actions){const t=new Timeline(R.create(level));for(const a of actions){if(t.check(a))return null;t.commit(a);t.advance(1e9);}return t.status==='Won'?Math.round(t.now/100)/10:null;}
function fits(level,role,id,banned=[]){const t=ROLES[role];
 if(!inside(rate(level,'random',48,id*31),t.random,.08))return null;
 if(sensible(level)>t.sensible)return null;
 if(!inside(rate(level,'visible',48,id*37),t.visible,.08))return null;
 const patterns=patternWins(level);if(patterns.length>t.patterns||patterns.some(p=>banned.includes(p)))return null;
 const random=rate(level,'random',300),visible=rate(level,'visible',300);if(!inside(random,t.random,.02)||!inside(visible,t.visible,.02))return null;
 return {random,visible,patterns};}
export function search(id,{banned=[],from=0,limit=4000}={}){const role=roleOf(id),t=ROLES[role],art={...sceneLevel(id),id,subject:'v8e-'+id};
 const stats={tried:0,bots:0,early:0,planner:0,mystery:0,time:0};
 for(let attempt=from;attempt<from+limit;attempt++){
  const extra=[0,1,-1,2,-2][Math.floor((attempt-from)/400)%5];
  const r=rng(id*7177+attempt*131),order=t.order[0]+(t.order[1]-t.order[0])*r();
  const d=definition(art,r,{role,difficulty:t.label,extra,order});if(!d||R.validate(d).length)continue;stats.tried++;
  if(stats.tried%100===0)console.error('L'+id,'attempt',attempt,JSON.stringify(stats));
  if(!fits(d,role,id,banned)){stats.bots++;continue;}
  let risk=null;if(isHard(id)){risk=earlyRisk(d);if(risk>EARLY_LIMIT){stats.early++;continue;}}
  let level=d,won=planner(d),mystery=null;if(!won){stats.planner++;continue;}
  if(MYSTERY[id]){try{const m=withMysteries(d,MYSTERY[id]);level=m.level;won=m.planner;mystery={hidden:m.hidden,keys:m.keys,reveals:m.reveals};}catch{stats.mystery++;continue;}}
  const s1=at1x(level,won.actions);if(!inside(s1,t.seconds1x)){stats.time++;continue;}
  const s=seconds(level,won.actions)??(patientSeconds(level,won.actions)??0)*.25;
  return {id,role,attempt,level,actions:won.actions,planner:won.opts,mystery,seconds:s,earlyRisk:risk??earlyRisk(d),stats};
 }
 throw Error('No candidate for L'+id+' '+JSON.stringify(stats)+(banned.length?' without '+banned.join(', '):''));}
function finish(found){const {level,actions}=found,top=playPlanner(level,{...found.planner,rows:1}).won;
 return {...found,bots:roleOf(found.id)==='tutorial'?null:{...report(level,300),patternWins:patternWins(level)},plannerTopRowOnly:top,patient:patientSeconds(level,actions)};}
const [cmd,arg,arg2]=process.argv.slice(2);
if(import.meta.url===`file://${process.argv[1]}`&&cmd==='level'){const id=+arg,t0=Date.now(),f=finish(search(id,{from:+(arg2||0)}));fs.writeFileSync(`${work}/L${id}.json`,JSON.stringify(f)+'\n');
 console.log('L'+id,f.role,'attempt',f.attempt,`${f.level.w}x${f.level.h}`,'pods',f.level.pods.length,f.bots?`random ${(f.bots.random*100).toFixed(0)}% visible ${(f.bots.visible*100).toFixed(0)}% patterns ${f.bots.patternWins.length}`:'',`~${f.seconds}s`,'early risk',f.earlyRisk,((Date.now()-t0)/1000).toFixed(0)+'s');}
// One balancing step by hand (what assemble does for its pick), so several candidates can be searched in parallel:
// node fix.mjs rebalance <id> <pattern[|pattern...]>   (writes work/L<id>.out.json; move it over L<id>.json to adopt it)
if(import.meta.url===`file://${process.argv[1]}`&&cmd==='rebalance'){const id=+arg,f=JSON.parse(fs.readFileSync(`${work}/L${id}.json`)),banned=[...(f.banned||[]),...arg2.split('|')];
 const next={...finish(search(id,{banned,from:f.attempt+1})),banned};fs.writeFileSync(`${work}/L${id}.out.json`,JSON.stringify(next)+'\n');console.log('L'+id,'patterns',next.bots.patternWins.join(', '));}
if(import.meta.url===`file://${process.argv[1]}`&&cmd==='assemble'){
 const found=Array.from({length:30},(_,i)=>JSON.parse(fs.readFileSync(`${work}/L${i+1}.json`)));
 for(let round=0;round<40;round++){
  const counted=found.filter(f=>f.bots&&f.id>=5),share=new Map(),hard=new Map();
  for(const f of counted)for(const p of f.bots.patternWins){share.set(p,(share.get(p)||0)+1);if(isHard(f.id))hard.set(p,(hard.get(p)||0)+1);}
  const over=[...share].find(([,n])=>n>PATTERN_SHARE*counted.length)||[...hard].find(([,n])=>n>HARD_PATTERN_LEVELS);if(!over)break;
  const [pattern]=over,hardOver=(hard.get(pattern)||0)>HARD_PATTERN_LEVELS&&!((share.get(pattern)||0)>PATTERN_SHARE*counted.length);
  const pick=counted.filter(f=>f.bots.patternWins.includes(pattern)&&(!hardOver||isHard(f.id))).sort((a,b)=>b.bots.patternWins.length-a.bots.patternWins.length||a.id-b.id)[0];
  const banned=[...(pick.banned||[]),pattern],next={...finish(search(pick.id,{banned,from:pick.attempt+1})),banned};found[pick.id-1]=next;fs.writeFileSync(`${work}/L${pick.id}.json`,JSON.stringify(next)+'\n');
  console.log('balance',pattern,'→ L'+pick.id,'patterns',next.bots.patternWins.length);
 }
 const levels=found.map(f=>f.level),hash=crypto.createHash('sha256').update(JSON.stringify(levels)).digest('hex');
 fs.writeFileSync(out+'/levels.json',JSON.stringify({version:'pilot-30-v8e',hash,levels},null,2)+'\n');
 fs.writeFileSync(out+'/certificates.json',JSON.stringify(found.map(f=>({id:f.id,actions:f.actions,schedule:'wait until work idle between commands',planner:f.planner,fairness:{scope:`visible-horizon planner: plans only with the ${ROWS} visible rows (the 4th is the edge), never a hidden colour or count; its route is the certificate`}})),null,2)+'\n');
 fs.writeFileSync(out+'/quality.json',JSON.stringify({version:'pilot-30-v8e',catalogHash:hash,complete:levels.length===30,roles:ROLES,roleOf:Object.fromEntries(found.map(f=>[f.id,f.role])),patternShare:PATTERN_SHARE,hardPatternLevels:HARD_PATTERN_LEVELS,earlyLimit:EARLY_LIMIT,mysteryLevels:MYSTERY,planners:PLANNERS,tail:T.TAIL,
  limits:['Bot rates are construction diagnostics, not human win rates.','seconds: an estimate of the owner\'s ×2 play, calibrated on the owner\'s v8C wins; secondsAt1x: the same brisk play at 1× (the default once ×2 is paid).','Patient player for the bots: every command settles before the next.'],
  levels:found.map(f=>({id:f.id,role:f.role,title:f.level.title,board:`${f.level.w}x${f.level.h}`,pods:f.level.pods.length,attempt:f.attempt,banned:f.banned||[],seconds:f.seconds,secondsAt1x:secondsAt1x(f.level,f.actions)??Math.round(f.patient*.25/.6),secondsAt1xFrom:secondsAt1x(f.level,f.actions)===null?'patient route (a brisk replay of the same commands plays out differently)':'brisk replay',patientSeconds:f.patient,planner:{...f.planner,commands:f.actions.length,winsSeeingTopRowOnly:f.plannerTopRowOnly},mystery:f.mystery,earlyRisk:f.earlyRisk,bots:f.bots,search:f.stats}))},null,2)+'\n');
 console.log('done',hash);
}

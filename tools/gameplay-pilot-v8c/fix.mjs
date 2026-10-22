// v8C = v8 rebuilt to level roles. Every level from L5 gets a role with a window for random taps, visible-colour
// taps and "mindless" pattern taps (column by column, row by row, snake, biggest number…). A v8 level is kept
// when it already fits its role; otherwise its capsules are split and ordered again on the same picture, or on
// a new scene (artc.mjs). The owner's fun controls L10, L22 and L29 stay as they are, labelled Super Hard.
import fs from 'node:fs';import crypto from 'node:crypto';
import R from './rules.cjs';import T from './timeline.cjs';const {Timeline}=T;import {definition} from './author.mjs';import {makeLevelArt,NEW_ART} from './artc.mjs';
import {solve,fairSolve,hiddenWorlds} from './search.mjs';import {rng,rate,sensible,report,earlyFatal,patternWins} from './botsc.mjs';
const v8=JSON.parse(fs.readFileSync('content/pilot-v8/levels.json')),v8certs=JSON.parse(fs.readFileSync('content/pilot-v8/certificates.json')),v8q=JSON.parse(fs.readFileSync('content/pilot-v8/quality.json'));
const out='content/pilot-v8c';fs.mkdirSync(out,{recursive:true});
// Roles agreed with the owner on 2026-10-21. random / visible: [min, max] win share; patterns: most of the 9 mindless bots
// allowed to win; sensible: most of the 4 sensible visible-colour strategies allowed to win.
export const ROLES={
 relief:{label:'Normal',random:[.60,.90],visible:[.85,1],patterns:6,sensible:4},
 normal:{label:'Normal',random:[.35,.65],visible:[.70,.95],patterns:4,sensible:3},
 task:{label:'Normal',random:[.20,.40],visible:[.45,.75],patterns:2,sensible:2},
 hard:{label:'Hard',random:[.15,.25],visible:[.20,.50],patterns:1,sensible:1},
 superHard:{label:'Super Hard',random:[.06,.13],visible:[0,.25],patterns:1,sensible:0},
};
export const ROLE_OF={5:'normal',6:'relief',7:'normal',8:'task',9:'normal',10:'superHard',11:'relief',12:'normal',13:'normal',14:'task',15:'hard',16:'relief',17:'normal',18:'task',19:'normal',20:'task',21:'normal',22:'superHard',23:'relief',24:'task',25:'hard',26:'normal',27:'task',28:'relief',29:'superHard',30:'normal'};
export const CONTROLS=new Set([10,22,29]);
// Whole-pilot limits (the owner, 2026-10-22): one winning pattern does not spoil a level; one pattern that wins most of
// the game does. From L5 any pattern may win at most PATTERN_SHARE of the levels, and at most one Hard/Super Hard level.
export const PATTERN_SHARE=.40,HARD_PATTERN_LEVELS=1;
// The owner, 2026-10-21: a hidden capsule reveals its colour when it reaches the top row, as in Colony Flow;
// v8's sealed capsules (revealed only after admission) become depth capsules.
const toDepth=l=>({...l,pods:l.pods.map(p=>p.mystery?{...p,mystery:'depth'}:p)});
const inside=(v,[lo,hi],slack=0)=>v>=lo-slack-1e-9&&v<=hi+slack+1e-9;
function replay(l,actions){const t=new Timeline(R.create(l)),reveals=[];let preparations=0,partialTurns=0,peak=0;
 for(let i=0;i<actions.length;i++){const a=actions[i];if(t.check(a))return null;const before=t.processedCells,free=t.free,r=t.commit(a);for(const e of r.events.filter(e=>e.type==='reveal'))reveals.push({pod:e.pod,at:i+1,mode:e.mode,freeBefore:free,clearedFraction:before/l.pods.reduce((n,p)=>n+p.count,0)});peak=Math.max(peak,5-t.free);t.advance(1e9);if(t.processedCells===before)preparations++;if(t.state.slots.some(p=>p>=0&&t.state.remaining[p]>0&&t.state.remaining[p]<l.pods[p].count))partialTurns++;}
 return t.status==='Won'?{modelMs:t.now,preparations,partialTurns,peak,reveals}:null;}
function policy(l,mode,seed,{burst=false}={}){const t=new Timeline(R.create(l)),random=rng(seed);let turns=0;
 while(t.state.status==='Playing'&&turns++<10000){let moves=R.legal(t.state);if(!moves.length){if(!t.pending)break;t.advance(t.steps[0].at-t.now);continue;}
  const bridges=moves.filter(a=>a.type==='bridge');if(bridges.length)moves=bridges;
  else if(mode==='visible'){const front=new Set(R.reach(t.state).targets.filter(c=>t.reserved[c]<0).map(c=>t.state.board[c]));const match=moves.filter(a=>t.state.meta.groups[a.pod].some(p=>t.state.revealed[p]&&front.has(t.state.meta.colours[p])));if(match.length)moves=match;}
  const a=mode==='left'?moves[0]:mode==='right'?moves.at(-1):mode==='large'?moves.reduce((a,b)=>a.type==='bridge'||t.state.remaining[a.pod]>=t.state.remaining[b.pod]?a:b):moves[Math.floor(random()*moves.length)];
  t.commit(a);if(!burst)t.advance(1e9);}
 t.advance(1e9);return t.status==='Won';}
// Bot screen for a role: cheap samples first, then the full counts.
function fits(level,role,id,banned=[]){const t=ROLES[role];
 if(!inside(rate(level,'random',48,id*31),t.random,.08))return null;
 if(sensible(level)>t.sensible)return null;
 if(!inside(rate(level,'visible',48,id*37),t.visible,.08))return null;
 const patterns=patternWins(level);if(patterns.length>t.patterns||patterns.some(p=>banned.includes(p)))return null;
 // The same samples as the recorded report (300 runs, default seed), so verify sees the numbers that were accepted.
 const random=rate(level,'random',300),visible=rate(level,'visible',300);if(!inside(random,t.random,.02)||!inside(visible,t.visible,.02))return null;
 return {random,visible,patterns};}
function certify(candidate,id){let route,fairness=null;
 if(candidate.pods.some(p=>p.mystery)){let f;try{f=fairSolve(candidate,{budget:8000});}catch{return null;}if(f.winning!==true)return null;route=f.routes[0];fairness={scope:'common observation policy for both equal-count hidden assignments; idle decisions',worlds:f.worlds,routes:f.routes};
  const worlds=hiddenWorlds(candidate);if(f.routes.some((rt,w)=>{const q=replay(worlds[w],rt);return !q||q.reveals.some(e=>e.clearedFraction>.7);}))return null;}
 else{const s=solve(R.create(candidate),{budget:6000});if(s.winning!==true)return null;route=s.actions;}
 return replay(candidate,route)?{actions:route,fairness}:null;}
// The first candidate for a level that fits its role (and loses every banned pattern): v8's own level when its picture is
// kept, then new splits and orders, seeds from `from`.
function search(original,role,id,{actions,fairness},banned=[],from=0){const label=ROLES[role].label,spiky=['task','hard','superHard'].includes(role);
 const art=makeLevelArt(id);
 if(!NEW_ART.has(id)&&!from){const keep=toDepth({...original,difficulty:label});if(fits(keep,role,id,banned))return {level:keep,actions,fairness,source:'v8',attempt:-1};}
 for(let attempt=from;attempt<from+3000;attempt++){const d=definition(art,rng(id*7177+attempt*131),{spiky,difficulty:label});if(!d||R.validate(d).length)continue;const candidate=toDepth({...d,title:art.title});
  if(!fits(candidate,role,id,banned))continue;
  if(['hard','superHard'].includes(role)){const ef=earlyFatal(candidate,30);if(ef.lost&&ef.early/ef.lost>.5)continue;}
  const c=certify(candidate,id);if(!c)continue;return {level:candidate,...c,source:NEW_ART.has(id)?'v8C scene':'v8C tray',attempt};}
 throw Error('No candidate for L'+id+' as '+role+(banned.length?' without '+banned.join(', '):''));}
const levels=[],certs=[],quality=[];
for(const original of v8.levels){const id=original.id,role=ROLE_OF[id];let level=original,actions=v8certs[id-1].actions,fairness=v8certs[id-1].fairness||null,attempt=-1,source='v8';
 if(role){const label=ROLES[role].label,spiky=['task','hard','superHard'].includes(role);
  if(CONTROLS.has(id)){level={...original,difficulty:label};source='v8 control';}
  else{const found=search(original,role,id,{actions,fairness});({level,actions,fairness,source,attempt}=found);}}
 const reference=replay(level,actions);if(!reference)throw Error('No live route L'+id);
 const metrics={};for(const mode of ['random','visible','left','right','large']){const trials=['left','right','large'].includes(mode)?1:32;let wins=0;for(let i=0;i<trials;i++)wins+=policy(level,mode,id*999+i*7919);metrics[mode]={wins,trials,rate:wins/trials};}
 let burstWins=0;for(let i=0;i<16;i++)burstWins+=policy(level,'random',id*313+i*7919,{burst:true});
 const bots=role?{...report(level,300),patternWins:patternWins(level)}:null;
 levels.push(level);certs.push({id,actions,schedule:'wait until work idle between commands',fairness});
 quality.push({id,role:role||'tutorial',source,changed:JSON.stringify(level)!==JSON.stringify(original),attempt,reference,policies:metrics,burstRandom:{wins:burstWins,trials:16,rate:burstWins/16},bots});
 console.log('L'+id,role||'tutorial',source,'attempt',attempt,bots?`random ${(bots.random*100).toFixed(0)}% visible ${(bots.visible*100).toFixed(0)}% patterns ${bots.patternWins.length}/9 sensible ${bots.sensibleWins}`:'');
}
// Balance: while a pattern wins too many levels, rebuild one non-control level it wins so that it loses there too.
const isHard=id=>['hard','superHard'].includes(ROLE_OF[id]);
for(let round=0;round<40;round++){
 const counted=quality.filter(q=>q.bots),share=new Map(),hard=new Map();
 for(const q of counted)for(const p of q.bots.patternWins){share.set(p,(share.get(p)||0)+1);if(isHard(q.id))hard.set(p,(hard.get(p)||0)+1);}
 const over=[...share].find(([p,n])=>n>PATTERN_SHARE*counted.length)||[...hard].find(([p,n])=>n>HARD_PATTERN_LEVELS);if(!over)break;
 const [pattern]=over,hardOver=(hard.get(pattern)||0)>HARD_PATTERN_LEVELS&&!((share.get(pattern)||0)>PATTERN_SHARE*counted.length);
 // Prefer levels with the most winning patterns (least loss of variety), never a control; for the Hard limit, a Hard level.
 const pick=counted.filter(q=>!CONTROLS.has(q.id)&&q.bots.patternWins.includes(pattern)&&(!hardOver||isHard(q.id))).sort((a,b)=>b.bots.patternWins.length-a.bots.patternWins.length||a.id-b.id)[0];
 if(!pick)throw Error('Cannot balance '+pattern);
 const id=pick.id,keepBanned=(pick.banned||[]).concat(pattern),found=search(v8.levels[id-1],ROLE_OF[id],id,{actions:v8certs[id-1].actions,fairness:v8certs[id-1].fairness||null},keepBanned,3000+round*3000);
 const level=found.level,reference=replay(level,found.actions);if(!reference)throw Error('No live route L'+id);
 const metrics={};for(const mode of ['random','visible','left','right','large']){const trials=['left','right','large'].includes(mode)?1:32;let wins=0;for(let i=0;i<trials;i++)wins+=policy(level,mode,id*999+i*7919);metrics[mode]={wins,trials,rate:wins/trials};}
 let burstWins=0;for(let i=0;i<16;i++)burstWins+=policy(level,'random',id*313+i*7919,{burst:true});
 levels[id-1]=level;certs[id-1]={id,actions:found.actions,schedule:'wait until work idle between commands',fairness:found.fairness};
 quality[id-1]={id,role:ROLE_OF[id],source:found.source,changed:JSON.stringify(level)!==JSON.stringify(v8.levels[id-1]),attempt:found.attempt,banned:keepBanned,reference,policies:metrics,burstRandom:{wins:burstWins,trials:16,rate:burstWins/16},bots:{...report(level,300),patternWins:patternWins(level)}};
 console.log('balance',pattern,'→ L'+id,'random',(quality[id-1].bots.random*100).toFixed(0)+'%','patterns',quality[id-1].bots.patternWins.length+'/9');
}
const hash=crypto.createHash('sha256').update(JSON.stringify(levels)).digest('hex');
fs.writeFileSync(out+'/levels.json',JSON.stringify({version:'pilot-30-v8c',hash,levels},null,2)+'\n');fs.writeFileSync(out+'/certificates.json',JSON.stringify(certs,null,2)+'\n');
fs.writeFileSync(out+'/quality.json',JSON.stringify({version:'pilot-30-v8c',catalogHash:hash,sourceHash:v8.hash,complete:levels.length===30,changedLevels:quality.filter(q=>q.changed).map(q=>q.id),controls:[...CONTROLS],roles:ROLES,roleOf:ROLE_OF,patternShare:PATTERN_SHARE,hardPatternLevels:HARD_PATTERN_LEVELS,limits:['Bot rates are construction diagnostics, not human win rates.','Patient player: every command settles before the next; live concurrent input can change same-colour allocation.','Controls L10, L22, L29 are kept from v8 at the owner\'s request; good levels are not rebuilt for a neater percentage.','Bridge levels: bots build a bridge as soon as one is offered.'],levels:quality},null,2)+'\n');
console.log('done',hash);

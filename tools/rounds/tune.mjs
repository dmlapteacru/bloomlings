// The level generator's queue tuning (the owner, 2026-11-02: «один генератор = сегодняшний сборщик плюс …»; README «The
// generator (2026-11-02)», calibration: calib/README.md). One job = one picture × class × recipe × slot:
//
//  1. Plan from the model. The calibration's share model (calib/model.json, a logistic regression of the quick share on
//     picture features, the blend t, columns, noise and doses) gives the blend t of levels.mjs candidate that lands the
//     class zone with the highest probability; a picture that already landed (its memory, bank/<P####>.json `tune`)
//     starts from its t instead.
//  2. Short measurements. At most five quick tests (40 games of the §5 random policy): Newton steps on t with the model's
//     slope until both sides of the zone were seen, then bisection inside that bracket. The t that lands is remembered.
//  3. Links one pair at a time (chooseLinks): Hard and Super Hard against the greedy strategy ("most work now" gets least
//     far), a trap pair first (17 §8); every class under the forced-decision limit of §5 (≤ 35 %): a pair that pushes the
//     forced share over it is refused; short holds (partners reach the top at about the same time), no two links blocking
//     the same columns at once, links spread along the queue.
//  4. Information overlays after the class is set (the calibration: hidden pods and hidden cells change none of the §5
//     tests): hidden pods or hidden cells by the recipe's dose, no re-tuning. A breather (17 §5 «Передышка», the owner,
//     2026-11-02) whose queue stays above its zone even reversed takes one plain link pair in place of its light mechanic.
//  5. levels.mjs evaluate (the full checks; a breather only the mandatory ones, 17 §5 «Передышка») and the placer's
//     filters of the slot.
import {CODES,columnsFor,measure,overlaid,boardOf} from './lib.mjs';
import {fitness} from './fitness.mjs';
import * as MD from './calib/model.mjs';
import * as LV from './levels.mjs';
import * as M from './measure.mjs';
import {engine} from './engine.mjs';
import {STRATEGIES} from '../level-stats/strategies.mjs';
import {pictureFeatures,access} from './calib/queue.mjs';
import {breatherRecipeOk,REGULAR_ZONE} from './wave.mjs';
import fs from 'node:fs';

export const rnd=seed=>{let s=seed>>>0||1;return()=>{s^=s<<13;s^=s>>>17;s^=s<<5;return(s>>>0)/4294967296;};};
// the zones a job aims at (17 §5): a regular Normal 50–65 % aiming at 56 %, the middle of the median's 50–60 % (the owner,
// 2026-11-02: the median of any 20 levels in 50–60 %, so a regular Normal is accepted near the middle, not anywhere in
// 50–75 %), the first Normal after a Hard 75–95 %, a breather 75–90 % (17 §5 «Передышка», the owner, 2026-11-02), an
// introduction 90–100 %, Hard 20–50 % (preferred 25–40 %), Super Hard 5–20 % (preferred 7–15 %)
export const ZONES={regular:[...REGULAR_ZONE,.56],'after-hard':[.75,.95,.85],breather:[.75,.9,.82],intro:[.9,1,.97],Hard:[.2,.5,.33],'Super Hard':[.05,.2,.12]};
export const zoneFor=(cls,slot)=>cls==='Normal'?ZONES[slot]||ZONES.regular:ZONES[cls];
// the class's queue noise (levels.mjs candidate's default sigma)
export const SIGMA={Normal:.06,Hard:.16,'Super Hard':.26};
let MODEL=null;export const loadModel=()=>MODEL||(MODEL=JSON.parse(fs.readFileSync(new URL('./calib/model.json',import.meta.url),'utf8')));
const Phi=x=>{const t=1/(1+.2316419*Math.abs(x)),d=.3989423*Math.exp(-x*x/2),p=d*t*(.3193815+t*(-.3565638+t*(1.781478+t*(-1.821256+t*1.330274))));return x>0?1-p:p;};
const clamp=(x,a,b)=>Math.max(a,Math.min(b,x)),r2=x=>Math.round(x*100)/100;

// ---- 1. the model's plan ----
// the model's row for a parameter set (the features the calibration recorded; hidden pods and cells 0: they come after)
const rowOf=(f,p)=>({f,t:p.t,sigma:p.sigma,cols:p.cols,P:p.P,hold:p.hold,linkStyle:p.trap?'trap':'similar',hiddenStyle:'spread',accentsPainted:!!p.accents,
 qf:{linkShare:p.links/p.P,hidden:0,cellsHidden:0,latches:p.latch||0,rankCorr:1-2*p.t,deepTop:0,frontTop:1}});
// the t on a grid of 0.02 whose predicted share lands the zone with the highest probability (the model's residual spread on
// the logit scale), ties by the distance to the zone's aim; offset = the picture's correction (logit observed − predicted)
export function planT(f,p,zone,{model=loadModel(),offset=0}={}){const [lo,hi,aim]=zone,s=model.logitResidualSd||1.9;let best=null;
 for(let t=0;t<=1.0001;t+=.02){const q={...p,t:r2(t)},e=MD.eta(model,rowOf(f,q))+offset,pin=Phi((MD.logit(Math.min(.999,hi))-e)/s)-Phi((MD.logit(Math.max(.001,lo))-e)/s),dist=Math.abs(e-MD.logit(aim));
  if(!best||pin>best.pin+1e-9||(Math.abs(pin-best.pin)<=1e-9&&dist<best.dist))best={t:q.t,pin,dist,eta:e,pred:MD.sigmoid(e)};}
 return best;}
// d eta / d t of the model at a parameter set (numerical)
export const slopeT=(f,p,model=loadModel())=>{const a=Math.max(0,p.t-.05),b=Math.min(1,p.t+.05);return (MD.eta(model,rowOf(f,{...p,t:b}))-MD.eta(model,rowOf(f,{...p,t:a})))/(b-a);};
// the next t of the correction loop: bisection (interpolated on the logit, kept in the middle 20–80 % of the bracket) once
// an easy side (above the zone) and a hard side (below it) were seen with tEasy < tHard; else a Newton step with the
// model's slope (at most −0.5, steps up to ±0.35)
export function nextT({t,q,aim,slope,easy,hard}){
 if(easy&&hard&&easy.t<hard.t){const eA=MD.logit(clamp(easy.q,.02,.98)),eB=MD.logit(clamp(hard.q,.02,.98)),k=clamp((MD.logit(aim)-eA)/(eB-eA),.2,.8);return r2(clamp(easy.t+k*(hard.t-easy.t),0,1));}
 const sl=Math.min(-.5,slope),dt=clamp((MD.logit(aim)-MD.logit(clamp(q,.025,.975)))/sl,-.35,.35);return r2(clamp(t+dt,0,1));}

// ---- 3. links one pair at a time ----
// how far the greedy strategies get (processed share of the board; 1.5 = they win): "most work now", for Super Hard also
// "release a place fastest" (17 §5, §11), on the live timeline
export async function greedyReach(level,cls){const E=await engine(),{R,T}=E;let best=0;
 for(const k of cls==='Super Hard'?['most-now','fast-release']:['most-now']){const pol=STRATEGIES[k].make(1,E),t=new T.Timeline(R.create(level,level.capacity||5)),total=t.state.board.reduce((n,c)=>n+(c>=0),0);let g=0;
  while(g++<100000){if(t.state.status==='Won')break;const m=R.legal(t.state).filter(a=>!t.check(a));if(m.length){const a=pol(t,m);if(!a||t.check(a)){if(t.pending){t.advance(t.steps[0].at-t.now);continue;}break;}t.commit(a);continue;}if(!t.pending)break;t.advance(t.steps[0].at-t.now);}
  best=Math.max(best,t.state.status==='Won'?1.5:t.processedCells/total);}
 return best;}
// the pods a link may take: not hidden, not at or behind a latch of its column (a group behind a latch waits for its key)
function linkable(level){const ok=new Array(level.pods.length).fill(true);level.pods.forEach((p,i)=>{if(p.mystery||p.group)ok[i]=false;});
 for(const l of level.latches||[])for(const p of level.tray[l.column].slice(l.depth))ok[p]=false;return ok;}
// the access key of each pod (its slice of its colour's cells, shallow first, over the deepest) and each colour's
// shallowest depth (> 0: the colour has no cell on the start front — a member of such a pair makes a trap link, 17 §8)
export function podKeys(level){const {d,g}=access(level),by={},minD={};for(let i=0;i<g.length;i++)if(CODES.includes(g[i])){(by[g[i]]=by[g[i]]||[]).push(d[i]);minD[g[i]]=Math.min(minD[g[i]]??Infinity,d[i]);}
 for(const a of Object.values(by))a.sort((x,y)=>x-y);const maxD=Math.max(1,...Object.values(by).flat().filter(x=>x<Infinity)),used={};
 const key=level.pods.map(p=>{const ds=by[p.color]||[],k=used[p.color]||0,sl=ds.slice(k,k+p.count).filter(x=>x<Infinity);used[p.color]=k+p.count;return sl.length?sl.reduce((a,b)=>a+b,0)/sl.length/maxD:1;});
 return {key,minD};}
// The columns blocked at each launch by a set of pairs, from a reference game's timing (top[c][row]: the launches made
// when column c's head reaches row): a pair's earlier member waits on top of its column, blocking it, until the partner
// comes on top. Returns the most columns blocked at once.
export function maxBlocked(pairs,top,cols){const ev=[];for(const x of pairs){const ta=top[x.c][x.row],tb=top[x.c2][x.row];if(!(ta<Infinity)||!(tb<Infinity)||ta===tb)continue;const [c,from,to]=ta<tb?[x.c,ta,tb]:[x.c2,tb,ta];ev.push({c,from,to});}
 let most=0;for(const e of ev){const at=e.from;const cs=new Set(ev.filter(x=>x.from<=at&&x.to>at).map(x=>x.c));most=Math.max(most,cs.size);}return Math.min(most,cols);}
// Links (pairs of two columns at one start depth, the engine's rule) one pair at a time until `want` linked pods.
// Each candidate pair is scored by the heuristics above (lower is better): Normal pairs of near access keys, Hard and
// Super Hard a pod useful now with a later one, a trap pair first; a long hold (|top[c][row] − top[c2][row]|), a pair
// that leaves at most one column free at a moment, a row next to a pair already chosen cost. Hard and Super Hard then try
// the best K by greedyReach (least far first); every class takes the first that keeps the forced share (measure.mjs
// forcedShare, `quick` games, then the full 30 of evaluate at the end) at most `limit`. Returns {level, pairs, forced,
// greedy, checked} or {error}.
// noTrap: a breather takes no trap link (17 §5 «Передышка»)
// trap: a Normal takes the Hard style (a pod useful now with a later one, a trap pair first) when its queue alone stays above
// the zone (tuneJob)
export async function chooseLinks(level,want,cls,r,{limit=.35,quick=10,K=5,exact=false,noTrap=false,trap:trapStyle=false}={}){if(want<=0)return {level,pairs:[],forced:null,checked:0};
 const cols=level.tray.length,ok=linkable(level),{key,minD}=podKeys(level),trapOf=i=>(minD[level.pods[i].color]??0)>0,hard=cls!=='Normal'||trapStyle;
 const {top}=LV.latchTiming(level,level.tray,level.pods,[]);
 const pairs=[];let cur=level,checked=0,gr=null;
 const forcedOf=async lv=>{checked++;return M.forcedShare(lv,exact?30:quick);};
 while(pairs.length*2<want){const used=new Set(pairs.flatMap(x=>[x.a,x.b])),opts=[];
  for(let c=0;c<cols;c++)for(let c2=c+1;c2<cols;c2++)for(let row=0;row<Math.min(level.tray[c].length,level.tray[c2].length);row++){const a=level.tray[c][row],b=level.tray[c2][row];
   if(!ok[a]||!ok[b]||used.has(a)||used.has(b))continue;const trap=trapOf(a)||trapOf(b);if(noTrap&&trap)continue;const ta=top[c][row],tb=top[c2][row],hold=Math.min(12,Math.abs((ta<Infinity?ta:40)-(tb<Infinity?tb:40)));
   const x={a,b,c,c2,row,trap},mb=maxBlocked([...pairs,x],top,cols),pen=mb>=cols-1?1:mb>=2?.3:0,spread=pairs.filter(p=>Math.abs(p.row-row)<=1).length*.4,dk=Math.abs(key[a]-key[b]);
   x.score=(hard?-dk-(trap?.5:0)-(row<=2?.2:0):dk)+hold*.05+pen+spread+r()*(hard?.2:.1);opts.push(x);}
  let pool=opts.sort((x,y)=>x.score-y.score);
  // a Hard or Super Hard has a trap link (17 §8): the first pair is a trap when one exists
  if(hard&&!pairs.some(p=>p.trap)&&pool.some(p=>p.trap))pool=pool.filter(p=>p.trap);
  const mk=x=>{const name=String.fromCharCode(65+pairs.length%26)+(pairs.length>=26?String(Math.floor(pairs.length/26)):'');return {...cur,pods:cur.pods.map((p,k)=>k===x.a||k===x.b?{...p,group:name}:p)};};
  let order=pool.slice(0,hard?K:8).map(x=>({x,lv:mk(x)}));
  if(hard){for(const o of order)o.g=await greedyReach(o.lv,cls==='Normal'?'Hard':cls);order.sort((u,v)=>u.g-v.g||u.x.score-v.x.score);}
  // the forced limit: the first that keeps it, then the next pairs of the pool (up to 12 tried)
  let pick=null;for(const o of [...order,...pool.slice(order.length,12).map(x=>({x,lv:mk(x)}))]){const f=await forcedOf(o.lv);if(f<=limit){pick={...o,f};break;}}
  if(!pick)return {error:`связки: каждая следующая пара поднимает вынужденные решения выше ${Math.round(limit*100)} % (§5)`,pairs:pairs.length,checked};
  cur=pick.lv;pairs.push(pick.x);gr=pick.g??gr;}
 // the full forced share (the 30 games evaluate measures, the same seed) must hold too; else once more with it per pair
 const f=await M.forcedShare(cur);checked++;
 if(f>limit){if(!exact)return chooseLinks(level,want,cls,r,{limit,quick,K,exact:true,noTrap,trap:trapStyle});return {error:`связки: вынужденных ${Math.round(f*100)} % после всех пар (§5)`,checked};}
 return {level:cur,pairs:pairs.map(({a,b,row,trap})=>({a,b,row,trap})),forced:f,greedy:gr,checked};}

// ---- 4. information overlays (hidden pods, hidden cells) by the recipe's dose ----
// hidden pods: from the second row, never in a group, never at or behind a latch (§7); an introduction's first in the
// second row (§8); hidden cells: levels.mjs pickHiddenCells, never on an accent cell. pos = the lead's dose place.
export function addInfo(level,recipe,cls,{intro=false,pos=null,r=rnd(1)}={}){const types=recipe==='none'?[]:recipe.split('+'),pods=level.pods.map(p=>({...p})),N=[...level.rowsBottomFirst.join('')].filter(ch=>CODES.includes(ch)).length;
 const dz=LV.doses(recipe,cls,pods.length,intro,N);if(dz.error)return {error:dz.error};let hiddenCells=level.hiddenCells||null;
 if(types.includes('hidden')){const [a,b]=dz.hidden,lead0=types.filter(m=>m!=='bridge')[0]==='hidden',want=pos===null||intro||!lead0?a+Math.floor(r()*(b-a+1)):a+Math.round(pos*(b-a));
  const behind=new Set((level.latches||[]).flatMap(l=>level.tray[l.column].slice(l.depth)));
  let opts=level.tray.flatMap(st=>st.map((p,row)=>[p,row])).filter(([p,row])=>row>=1&&!pods[p].group&&!behind.has(p)).sort(()=>r()-.5);
  if(intro){const k=opts.findIndex(([,row])=>row===1);if(k<0)return {error:'нет подходящего места во втором ряду для скрытого'};opts=[opts[k],...opts.filter((_,i)=>i!==k)];}
  if(opts.length<want)return {error:'не хватило подов для скрытых'};opts.slice(0,want).forEach(([p])=>pods[p].mystery='depth');}
 if(types.includes('cells')){const [a,b]=dz.cells,lead0=types.filter(m=>m!=='bridge')[0]==='cells',want=pos===null||intro||!lead0?a+Math.floor(r()*(b-a+1)):a+Math.round(pos*(b-a));
  const avoid=new Set((level.recolor||[]).map(([x,y])=>(level.h-1-(y+2))*level.w+x+2));
  hiddenCells=LV.pickHiddenCells(level,want,r,{intro,avoid});if(!hiddenCells)return {error:'не хватило глубоких клеток для скрытых клеток'};}
 return {level:{...level,pods,...(hiddenCells?{hiddenCells}:{})}};}

// ---- the placer's filters of a slot (levels.mjs place: greedyOk, mechOk, fitOk, the holding pod, the calm rule, trap
// links), so a job stops on a level the placer will take ----
// planRecipe (a breather): the plan's recipe, which the breather keeps or replaces by one plain link pair
export function slotProblems(e,level,cls,slot,pic,planRecipe=null){const k=e.checks||{},why=[],types=(level.recipe||'none')==='none'?0:level.recipe.split('+').length;
 if(e.random.cls!==cls)why.push(`класс ${e.random.cls}`);
 if(slot==='breather'){if(types>1)why.push('передышка: больше одной механики');else if(!breatherRecipeOk(level.recipe||'none',planRecipe))why.push('передышка: механика не лёгкая или не по плану');
  if(k.traps&&k.traps.traps)why.push('передышка: связка-ловушка');if(level.pods.filter(p=>p.group).length>2)why.push('передышка: больше одной связки');return why;}
 if(!(k.forced<=.35))why.push('вынужденных > 35 %');if(cls!=='Normal'&&!(k.opening&&k.opening.ok))why.push('дебют + цвета');if(k.latch&&!k.latch.ok)why.push('засов');
 if(slot!=='intro'){const f=fitness(overlaid(pic,level.overlay||null)),weak=f.hiddenColours<=1||f.visible>=6;
  if(cls!=='Normal'&&f.hiddenColours<3)why.push('картинка слабая для Hard');if(cls==='Normal'&&weak&&!types)why.push('слабая картинка без допа');}
 if(slot==='regular'&&!k.hold)why.push('нет пода, держащего место (§6)');const cf=M.calmFails(e.random.cls,k.calm,slot==='regular');if(cf&&slot!=='intro')why.push(cf);
 if(cls!=='Normal'&&k.traps&&k.traps.links&&!k.traps.traps)why.push('нет связки-ловушки');return why;}

// ---- the job ----
// pic, cls, recipe, slot (regular | after-hard | breather | intro | Hard | Super Hard), want (levels in the zone that pass
// the slot's filters), wantLoss (a regular Normal keeps going until one that "most work now" loses, 17 §5), attempts and
// minutes (the budget; 17 §13.5: at most 64 candidates), maxColours (accents never take the level beyond), memory (the picture's bank.tune, read and written), seed,
// accentMode (levels.mjs build --accents), save() after each attempt, log. Returns {banked, accepted, stats, memory}:
// banked = every candidate that passed evaluate (any measured class, for the bank), accepted = those in the zone that
// pass the slot's filters.
export async function tuneJob({pic,cls,recipe,slot=null,intro=false,want=1,wantLoss=false,attempts=64,minutes=Infinity,memory={},seed=1,accentMode='auto',maxColours=10,save=()=>{},log=()=>{},model=loadModel()}){
 slot=slot||(intro?'intro':cls==='Normal'?'regular':cls);const zone=zoneFor(cls,slot),[lo,hi,aim]=zone,t0=Date.now(),breather=slot==='breather',planRecipe=recipe;
 const m=measure(pic),board=boardOf(pic),f=pictureFeatures(pic),base=LV.splitPods(m.volumes,m.N,null);let types=recipe==='none'?[]:recipe.split('+');
 // a breather whose share stays above its zone with hidden information alone (or none) takes one plain link pair instead
 // (17 §5 «Передышка», the owner, 2026-11-02: hidden pods and cells do not move the bot test); the picture remembers it
 const toPair=()=>{recipe='links';types=['links'];mem.pair=true;};
 const stats={attempts:0,quick:0,corrections:0,linkChecks:0,evaluated:0,passed:0,accepted:0,reasons:{}},no=k=>{stats.reasons[k]=(stats.reasons[k]||0)+1;};
 if(base.error){no(base.error);return {banked:[],accepted:[],stats,memory};}
 const mem=memory;const banked=[],accepted=[];let offset=mem.offset||0,nOff=mem.offset!=null?2:0,tooEasy=0,tooHard=0,landed=false,trapMode=!!mem.trap;
 if(breather&&mem.pair&&!types.includes('links'))toPair();
 for(let k=0;k<Math.min(64,attempts)&&(Date.now()-t0)<minutes*60000;k++){
  // (wantLoss keeps going for a level "most work now" loses, up to four accepted)
  if(accepted.length>=want&&(!wantLoss||accepted.length>=Math.max(want,4)||accepted.some(x=>x.e.checks.greedyLoses)))break;
  stats.attempts++;const cs=columnsFor(m.N),cols=mem.cols&&cs.includes(mem.cols)&&k%3!==2?mem.cols:cs[k%cs.length],cseed=seed*1000003+k*7919+['Normal','Hard','Super Hard'].indexOf(cls)*31+(intro?17:0);
  const pos=recipe==='none'||intro||breather?null:trapMode?1:LV.DOSE_CYCLE[k%LV.DOSE_CYCLE.length],rc=recipe;
  // accents (17 §6), as levels.mjs build: this attempt paints 1–2 accent materials (the level's recolouring); never on a breather
  const av=breather?null:LV.accentVariant(pic,null,m,null,cls,intro,k,accentMode,cseed,no,maxColours),cm=av?av.m:m,cboard=av?av.board:board,cpods=av?av.pods:base.pods;
  // the links dose (the lead its dose place, an additional one its minimum, a breather the lead's minimum)
  const dz=LV.doses(recipe,cls,cpods.length,intro,cm.N,{breather});if(dz.error){no(dz.error);break;}
  let linkWant=types.includes('links')?(breather||pos===null?dz.links[0]:dz.links[0]+2*Math.round(pos*(dz.links[1]-dz.links[0])/2)):0,posUsed=pos;
  const p={sigma:SIGMA[cls],cols,P:cpods.length,hold:slot==='regular'?1:0,links:linkWant,trap:cls!=='Normal'||trapMode,latch:types.includes('latch')?dz.latch[0]:0,accents:!!av};
  // the start t: the picture's memory (± 0.03), else the model's plan; a breather starts from the access order (t = 0)
  let t=breather&&mem.t==null?0:mem.t!=null?clamp(r2(mem.t+(rnd(cseed)()-.5)*.06),0,1):planT(f,p,zone,{model,offset}).t;
  const make=async t=>{const c=LV.candidate(pic,cboard,cpods,cols,recipe,cls,cseed,intro,pos,{...(cls==='Normal'?{t}:{tStrong:t}),sigma:p.sigma,noLinks:!intro,noInfo:true,noHold:slot!=='regular',...(av?{accents:av.accents.map(x=>x.code)}:{})});
   if(c.error)return c;if(!linkWant||intro)return c;let ch=await chooseLinks(c.level,linkWant,cls,rnd(cseed^0x77),{noTrap:breather,trap:trapMode});stats.linkChecks+=ch.checked||0;
   // the dose place cannot hold the forced limit on this queue: the lead's minimum dose instead (the placer weighs the place
   // softly, 17 §7), recorded as dose place 0
   if(ch.error&&linkWant>dz.links[0]){no('связки: доза снижена до минимума (вынужденные решения)');linkWant=dz.links[0];posUsed=0;p.links=linkWant;ch=await chooseLinks(c.level,linkWant,cls,rnd(cseed^0x77),{noTrap:breather,trap:trapMode});stats.linkChecks+=ch.checked||0;}
   return ch.error?{error:ch.error}:{level:ch.level,links:ch};};
  let easy=null,hard=null,q=null,level=null,links=null;const steps=[];
  for(let step=0;step<=5;step++){const b=await make(t);if(b.error){no(b.error);level=null;break;}level=b.level;links=b.links||null;q=await M.quickShare(level,40,11);stats.quick++;
   const etaP=MD.eta(model,rowOf(f,{...p,t})),etaO=MD.logit(clamp(q,.025,.975));steps.push({t,q});offset=(offset*nOff+(etaO-etaP))/(nOff+1);nOff=Math.min(nOff+1,4);
   if(q>=lo&&q<=hi){landed=true;mem.t=t;mem.cols=cols;mem.offset=r2(offset);mem.quick=q;mem.at=new Date().toISOString().slice(0,10);break;}if(step===5)break;
   if(q>hi&&(!easy||t>easy.t))easy={t,q};if(q<lo&&(!hard||t<hard.t))hard={t,q};
   const nt=nextT({t,q,aim,slope:slopeT(f,{...p,t},model),easy,hard});if(nt===t||steps.some(s=>s.t===nt))break;t=nt;stats.corrections++;}
  save();
  // a picture that stays above the zone even with the reverse order (t ≈ 1), or below it with the access order (t ≈ 0),
  // in three attempts (six on Hard and Super Hard, whose queues vary more) and never landed in the zone cannot reach this
  // class: stop early (the runner swaps the picture)
  if(!(q>=lo&&q<=hi)){if(steps.some(s=>s.t>=.98&&s.q>hi))tooEasy++;if(steps.some(s=>s.t<=.02&&s.q<lo))tooHard++;}
  // a Normal with links whose queue stays above the zone even reversed: the links are the difficulty knob (the calibration:
  // trap links at 30 % cost about 32 points), so the next attempts take them in the Hard style at the top of the dose range
  if(tooEasy>=1&&!trapMode&&cls==='Normal'&&!breather&&!intro&&types.includes('links')&&(slot==='regular'||slot==='after-hard')){trapMode=true;mem.trap=true;tooEasy=0;no('связки ловушками: очередь сама остаётся выше зоны');}
  // a breather with hidden information (or none) above its zone even reversed: one plain link pair from the next attempt
  if(tooEasy>=1&&breather&&!types.includes('links')){toPair();tooEasy=0;no('передышка: одна обычная связка вместо скрытых (доля выше зоны)');}
  const stopAt=cls==='Normal'?3:6;if(!landed&&(tooEasy>=stopAt||tooHard>=stopAt)){no(tooEasy>=stopAt?'картинка слишком лёгкая для зоны: выше даже с обратным порядком (t = 1)':'картинка слишком трудная для зоны: ниже даже в порядке доступа (t = 0)');break;}
  if(!level)continue;
  log(`#${k} кол ${cols} связ ${linkWant} замеры ${steps.map(s=>`t ${s.t}: ${Math.round(s.q*100)} %`).join(', ')}${links&&links.forced!=null?` · вынужд. ${Math.round(links.forced*100)} %`:''}`);
  if(q<lo-.15||q>hi+.15){no('быстрая оценка вне зоны после поправок');continue;}
  // the class is set by the queue and its links: now the information overlays, without re-tuning
  const info=addInfo(level,rc,cls,{intro,pos,r:rnd(cseed^0x9e37)});if(info.error){no(info.error);continue;}level=info.level;
  stats.evaluated++;const e=await LV.evaluate(level,{m:cm,cls,intro,fixed:null,breather});if(e.reason){no('evaluate: '+e.reason);log(`   evaluate: ${e.reason}`);continue;}
  if(av)e.checks.accents=await LV.accentCheck(level,pic,av.accents,e.checks.pressure||null);
  stats.passed++;const x={k,t,cols,pos:posUsed,steps,level,m:cm,e,acc:av,recipe:rc,links:links&&{pairs:links.pairs.length,traps:links.pairs.filter(p=>p.trap).length,forced:links.forced,greedy:links.greedy},seconds:(Date.now()-t0)/1000};banked.push(x);
  const why=slotProblems(e,level,cls,slot,pic,breather?planRecipe:null),inZone=e.random.share>=lo&&e.random.share<=hi;
  if(why.length){no('расстановка: '+why[0]);log(`   отказ расстановки: ${why.join('; ')}`);continue;}
  if(!inZone){no('вне зоны позиции (проходит фильтры)');log(`   проходит, но вне зоны: ${e.random.share}`);continue;}
  stats.accepted++;accepted.push(x);log(`   ПРИНЯТ: ${e.random.share} за ${((Date.now()-t0)/1000).toFixed(0)} с (попытка ${k+1})`);}
 // a breather that stays above its zone even with the reverse order (a small picture without a mechanic): the first
 // fitting layout nearest the zone is taken (17 §5: built from the first fitting layout; the placer weighs the zone)
 if(breather&&!accepted.length){const dist=x=>{const s=x.e.random.share;return s<lo?lo-s:s>hi?s-hi:0;};
  const near=banked.filter(x=>x.e.random.cls==='Normal'&&!slotProblems(x.e,x.level,cls,slot,pic,planRecipe).length).sort((u,v)=>dist(u)-dist(v)||(u.recipe!==planRecipe)-(v.recipe!==planRecipe)||Math.abs(u.e.random.share-aim)-Math.abs(v.e.random.share-aim))[0];
  if(near){near.zoneMiss=true;accepted.push(near);stats.accepted++;log(`   ПРИНЯТ (ближе всего к зоне передышки): ${near.e.random.share}`);}}
 mem.offset=r2(offset);save();return {banked,accepted,stats,memory:mem,seconds:(Date.now()-t0)/1000};}

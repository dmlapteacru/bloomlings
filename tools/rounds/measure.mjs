// Checks of a candidate level on the round engine (product/17_LEVEL_GENERATION_RULES.md §1, §5, §8, §10, §11):
// the saved solution (search, then replay on the live timeline), the extended random test with waiting and the
// Wilson interval, the simple strategies, the fairness of hidden pods, the rhythm by fifths of done work.
import {engine} from './engine.mjs';
import {solver} from '../level-stats/solve.mjs';
import {run} from '../level-stats/trace.mjs';
import {STRATEGIES,rng} from '../level-stats/strategies.mjs';
import {createHash} from 'node:crypto';
import {bridgeLiveVerdict} from './live-proof.mjs';

const isStateEvent=e=>e.type!=='return'&&e.type!=='arrive';
// one game of the extended random policy (§5): before each choice the events of the moment are processed; the bot
// picks uniformly among all legal actions (a pod, a whole group, each bridge place) and, when a state-changing event
// is scheduled, one extra option: wait until that event. With no action and a scheduled event, time moves on by itself.
export function randomGame(E,level,seed,keep=null){const {R,T}=E,t=new T.Timeline(R.create(level,level.capacity||5)),r=rng(seed);let guard=0;
 const settleNow=()=>{while(t.pending&&t.steps[0].at<=t.now+1e-7)t.advance(0);};
 const waitEvent=()=>{while(t.pending){const e=t.steps[0];t.advance(Math.max(0,e.at-t.now));if(isStateEvent(e))break;}settleNow();};
 while(guard++<200000){settleNow();if(t.state.status==='Won')return true;
  const moves=R.legal(t.state).filter(a=>!t.check(a)&&(!keep||keep(a))),canWait=t.steps.some(isStateEvent);
  if(!moves.length){if(t.pending){waitEvent();continue;}return false;}
  const k=Math.floor(r()*(moves.length+(canWait?1:0)));
  if(k===moves.length){waitEvent();continue;}
  t.commit(moves[k]);}
 throw Error('random policy exceeded its event budget; outcome is unknown');}
export function wilson(w,n,z=1.96){if(!n)return [0,1];const p=w/n,d=1+z*z/n,c=p+z*z/(2*n),h=z*Math.sqrt(p*(1-p)/n+z*z/(4*n*n));return [Math.max(0,(c-h)/d),Math.min(1,(c+h)/d)];}
export const classOfShare=p=>p>=.5?'Normal':p>=.2?'Hard':p>=.05?'Super Hard':'too hard';
// 100 games; widened to 400 in total when the 95 % Wilson interval crosses a class border (§5)
export async function randomTest(level,{seed=1,first=100,max=400}={}){
 if(!Number.isSafeInteger(seed)||!Number.isInteger(first)||!Number.isInteger(max)||first<1||max<first)throw Error('random test requires an integer seed and 1 <= first <= max');
 const E=await engine();let wins=0,n=0;
 const play=k=>{for(let i=0;i<k;i++){if(randomGame(E,level,seed*7919+n*104729+1))wins++;n++;}};
 play(first);let ci=wilson(wins,n);const crosses=([a,b])=>[.05,.2,.5].some(x=>a<x&&b>=x);
 if(crosses(ci)&&max>first){play(max-first);ci=wilson(wins,n);}
 return {runs:n,wins,share:Math.round(wins/n*1000)/1000,wilson95:ci.map(x=>Math.round(x*1000)/1000),cls:classOfShare(wins/n),borderline:crosses(ci),protocol:'§5 extended: uniform over legal actions + wait before a scheduled event, ×1',seedBase:seed};}
// a quick estimate for screening candidates before the full test
export async function quickShare(level,games=40,seed=11){const E=await engine();let w=0;for(let i=0;i<games;i++)if(randomGame(E,level,seed*31+i*977))w++;return w/games;}

// the saved winning solution: full-information search on the patient model, replayed on the live timeline (§1, §13.7)
export async function solve(level,budget=200000){const E=await engine(),S=solver(E,budget),cap=level.capacity||5;
 const r=S.route(level,cap);if(!r.route)return {win:r.win,route:null,nodes:r.nodes};
 const tr=run(E,level,{commands:r.route.map(a=>({t:0,action:a})),patient:true});
 const dec=S.decisions(level,cap,r.route);
 // the same actions tapped the moment each is accepted (no waiting for work to settle): a lower bound of the duration;
 // it may lose, which is allowed (§6: the solution may need a reserve of places or a pause)
 const fast=run(E,level,{commands:r.route.map(a=>({t:0,action:a}))});
 return {win:true,route:r.route,nodes:r.nodes,replay:tr.status,trace:tr,fast:{status:fast.status,seconds:Math.round(fast.end/100)/10},decisions:{forks:dec.risky,single:dec.tight,unknown:dec.unknown}};}

// The public rule view exposes remaining[] even for hidden pods. Mask both material AND count for this proof.
export function fairnessObservation(R,s){const v=R.view(s);return JSON.stringify({...v,
 remaining:v.remaining.map((n,p)=>s.revealed[p]?n:null),colours:v.colours.map((c,p)=>s.revealed[p]?c:null)});}
export function fairnessDefinitionHash(level){const {id,difficulty,...definition}=level;return createHash('sha256').update(JSON.stringify(definition)).digest('hex');}

// A common policy for each information set, including later revelations. Each step settles on the actual timeline,
// so true is a playable patient-policy witness; false only means no patient policy, not a proof of live impossibility.
// Worlds are never merged by state signature alone: that signature omits their hidden colour assignments.
export function observationPolicy(R,initial,{budget=60000,preferred=[],observe=s=>fairnessObservation(R,s)}={}){
 const memo=new Map(),rank=new Map(preferred.map((a,i)=>[JSON.stringify(a),i]));let nodes=0;
 const LIMIT=Symbol('policy budget');
 const go=belief=>{
  if(belief.every(w=>w.state.status==='Won'))return true;
  if(belief.some(w=>w.state.status!=='Playing'&&w.state.status!=='Won'))return false;
  const key=belief.map(w=>`${w.world}:${R.signature(w.state)}`).join('\n');if(memo.has(key))return memo.get(key);
  if(++nodes>budget)throw LIMIT;
  const groups=new Map();for(const w of belief){const k=observe(w.state);if(!groups.has(k))groups.set(k,[]);groups.get(k).push(w);}
  // Different visible information may choose different continuations; identical observations must share an action.
  if(groups.size>1){for(const group of groups.values())if(!go(group)){memo.set(key,false);return false;}memo.set(key,true);return true;}
  const moves=R.legal(belief[0].state).filter(a=>belief.every(w=>R.legal(w.state).some(b=>JSON.stringify(a)===JSON.stringify(b))));
  moves.sort((a,b)=>(rank.get(JSON.stringify(a))??Infinity)-(rank.get(JSON.stringify(b))??Infinity));
  for(const a of moves){const next=[];for(const w of belief){const step=R.step(w.state,a);if(!step.accepted)break;next.push({world:w.world,state:step.state});}
   if(next.length===belief.length&&go(next)){memo.set(key,true);return true;}}
  memo.set(key,false);return false;};
 try{return {win:go(initial),nodes,complete:true};}catch(e){if(e!==LIMIT)throw e;return {win:null,nodes,complete:false};}}

// §8 fairness: enumerate every DISTINCT permutation of (material,count), conserving the exact balance. Prove one
// observation-based policy over the whole domain, rather than checking 24 worlds only until their first difference.
export async function fairness(level,route,options={}){const E=await engine(),{idle:R}=E,hid=level.pods.map((p,i)=>p.mystery?i:-1).filter(i=>i>=0);
 const {budget=60000,worldLimit=720}=typeof options==='number'?{worldLimit:options}:options;
 const base={proofVersion:2,definitionHash:fairnessDefinitionHash(level),method:'complete hidden-permutation observation policy on live-settled steps'};
 if(!Array.isArray(route))return {...base,checked:0,worlds:0,complete:false,unknown:1,fair:false,note:'нет подтверждённого решения'};
 const vals=hid.map(i=>[level.pods[i].color,level.pods[i].count]),worlds=[];let truncated=false;
 const perm=(a,k=0)=>{if(k===a.length){if(worlds.length>=worldLimit){truncated=true;return;}worlds.push(a.map(v=>v.slice()));return;}
  const seen=new Set();for(let i=k;i<a.length;i++){const key=JSON.stringify(a[i]);if(seen.has(key))continue;seen.add(key);[a[k],a[i]]=[a[i],a[k]];perm(a,k+1);[a[k],a[i]]=[a[i],a[k]];}};
 perm(vals.slice());
 if(truncated)return {...base,checked:0,worlds:worlds.length,complete:false,unknown:1,fair:false,note:'домен скрытых перестановок превышает бюджет; честность не проверена'};
 const initial=worlds.map((values,world)=>{const pods=level.pods.map(p=>({...p}));values.forEach(([color,count],i)=>Object.assign(pods[hid[i]],{color,count}));
  return {world,state:R.create({...level,pods},level.capacity||5)};});
 const proof=observationPolicy(R,initial,{budget,preferred:route});
 return {...base,checked:worlds.length,worlds:worlds.length,complete:proof.complete,nodes:proof.nodes,
  unknown:proof.win===null?1:0,fair:proof.win===true,note:proof.win===true?'единая безопасная стратегия при каждой одинаковой видимой информации':proof.win===null?'бюджет стратегии исчерпан; честность не проверена':'безопасная стратегия с ожиданием не найдена'};}

// the simple strategies of §11, each with the leftmost and the rightmost hero choice, tapping as soon as possible
export async function strategies(level){const E=await engine(),{R,T}=E,out={};
 for(const [k,st] of Object.entries(STRATEGIES)){if(st.random||k==='planner'||k==='big-first')continue;
  for(const side of level.hero?['left','right']:['left']){const pol=st.make(1,E),t=new T.Timeline(R.create(level,level.capacity||5));let g=0;
   while(g++<100000){if(t.state.status==='Won')break;const m=R.legal(t.state).filter(a=>!t.check(a));
    if(m.length){const br=m.filter(a=>a.type==='bridge');const a=br.length?(side==='left'?br[0]:br.at(-1)):pol(t,m);if(!a||t.check(a)){if(t.pending){t.advance(t.steps[0].at-t.now);continue;}break;}t.commit(a);continue;}
    if(!t.pending)break;t.advance(t.steps[0].at-t.now);}
   out[`${k}|${side}`]=t.state.status==='Won';}if(!level.hero)out[`${k}|right`]=out[`${k}|left`];}
 return out;}

// the rhythm on the saved solution (§10): per fifth of done work W, mean and minimum free places (time-weighted),
// share of time with a full buffer, most pods waiting without work; launches left after 80 % W and the last launch's W
export function rhythm(tr,W){const S=tr.samples,end=tr.end,seg=[0,1,2,3,4].map(()=>({t:0,free:0,min:Infinity,full:0,stalled:0}));
 for(let i=0;i<S.length;i++){const [t,free,,stalled,,done]=S[i],dt=(i+1<S.length?S[i+1][0]:end)-t,k=Math.min(4,Math.floor(5*done/W));const s=seg[k];
  s.t+=dt;s.free+=free*dt;s.min=Math.min(s.min,free);if(free===0)s.full+=dt;s.stalled=Math.max(s.stalled,stalled);}
 const doneAt=t=>{let d=0;for(const s of S){if(s[0]<=t)d=s[5];else break;}return d;};
 const after80=tr.taps.filter(x=>doneAt(x.t)>=.8*W).length,lastShare=tr.taps.length?Math.round(100*doneAt(tr.taps.at(-1).t)/W):null;
 return {segments:seg.map((s,i)=>({from:i*20,to:i*20+20,meanFree:s.t?Math.round(s.free/s.t*100)/100:null,minFree:s.min===Infinity?null:s.min,fullShare:s.t?Math.round(s.full/s.t*100)/100:null,maxWaiting:s.stalled})),launchesAfter80:after80,lastLaunchAtW:lastShare,seconds:Math.round(end/100)/10};}

// §9: on a bridge introduction every shown bridge keeps a winning continuation: the level is solved with only that bridge
// (a bridge needs its approach cleared first, §8, so it cannot simply be taken at the start)
// Each true has a live-replayed witness; each false has a schedule-independent cut certificate. A failed patient
// search or exhausted budget is UNKNOWN, because overlapping live actions can win when patient actions cannot.
export async function bridgeChoicesWin(level){const proofs=[];
 for(let i=0;i<(level.hero?level.hero.choices.length:0);i++)proofs.push(await bridgeLiveVerdict(level,i));
 const each=proofs.map(p=>p.win);return {each,proofs,ok:each.length>0&&each.every(x=>x===true)};}

// the bridge as a choice (the owner, 2026-10-27: "надо выбрать, какой мост открывать"): for each bridge, whether it keeps a
// winning continuation and the random test's share when only that bridge may be built; gap = best share − worst share
// (win: each bridge's solver verdict when already known)
export async function bridgeChoices(level,games=60,seed=5,known=null){const E=await engine(),win=known||(await bridgeChoicesWin(level)).each,each=[];
 for(let i=0;i<win.length;i++){let w=0;for(let g=0;g<games;g++)if(randomGame(E,level,seed*7919+g*104729+i,a=>a.type!=='bridge'||a.choice===i))w++;each.push({label:level.hero.choices[i].label,win:win[i],share:Math.round(w/games*100)/100});}
 const sh=each.map(x=>x.share);return {each,gap:Math.round((Math.max(...sh)-Math.min(...sh))*100)/100,better:sh.indexOf(Math.max(...sh))};}

// the greedy strategies of 17 §5 (the owner's amendment of 2026-10-27): "most work now" must lose a Hard, and with
// "release a place fastest" a Super Hard, at both hero choices; returns which of them win
export async function greedy(level,cls=null){const E=await engine(),{R,T}=E,out={};
 for(const k of cls==='Hard'?['most-now']:['most-now','fast-release']){for(const side of level.hero?['left','right']:['left']){const pol=STRATEGIES[k].make(1,E),t=new T.Timeline(R.create(level,level.capacity||5));let g=0;
  while(g++<100000){if(t.state.status==='Won')break;const m=R.legal(t.state).filter(a=>!t.check(a));
   if(m.length){const br=m.filter(a=>a.type==='bridge');const a=br.length?(side==='left'?br[0]:br.at(-1)):pol(t,m);if(!a||t.check(a)){if(t.pending){t.advance(t.steps[0].at-t.now);continue;}break;}t.commit(a);continue;}
   if(!t.pending)break;t.advance(t.steps[0].at-t.now);}
  out[`${k}|${side}`]=t.state.status==='Won';}if(!level.hero)out[`${k}|right`]=out[`${k}|left`];}
 return out;}
export const greedyBeaten=(g,cls)=>cls==='Hard'?['most-now|left','most-now|right'].every(k=>g[k]===false):
 cls==='Super Hard'?['most-now|left','most-now|right','fast-release|left','fast-release|right'].every(k=>g[k]===false):true;

// ---- the owner's second amendment of 2026-10-27 (after pilot v11) ----
// "opening + colours" (§5): every opening of k first actions, each taken as soon as one is possible, then the greedy
// strategies; Hard: "most work now", Super Hard: also "release a place fastest", at both hero choices. No 1-action
// opening may win; after 3-action openings at most 35 % may win. Distinct action sequences are counted once.
const asap=(T,t)=>{let g=0;while(g++<100000){if(t.state.status==='Won')return [];const m=E0.R.legal(t.state).filter(a=>!t.check(a));if(m.length)return m;if(!t.pending)return [];t.advance(t.steps[0].at-t.now);}return [];};
let E0=null;
function greedyFrom(E,level,opening,k,side){const {R,T}=E,pol=STRATEGIES[k].make(1,E),t=new T.Timeline(R.create(level,level.capacity||5));
 for(const a of opening){const m=asap(T,t),x=m.find(y=>JSON.stringify(y)===a);if(!x)return false;t.commit(x);}
 let g=0;while(g++<100000){if(t.state.status==='Won')return true;const m=R.legal(t.state).filter(a=>!t.check(a));
  if(m.length){const br=m.filter(a=>a.type==='bridge');const a=br.length?(side==='left'?br[0]:br.at(-1)):pol(t,m);if(!a||t.check(a)){if(t.pending){t.advance(t.steps[0].at-t.now);continue;}break;}t.commit(a);continue;}
  if(!t.pending)break;t.advance(t.steps[0].at-t.now);}
 return t.state.status==='Won';}
export async function openingTest(level,cls,{depth=3,limit=0.35}={}){const E=await engine(),{R,T}=E;E0=E;
 const strats=cls==='Super Hard'?['most-now','fast-release']:['most-now'],sides=level.hero?['left','right']:['left'];
 const wins=seq=>strats.some(k=>sides.some(s=>greedyFrom(E,level,seq,k,s)));
 // all distinct openings of exactly k actions (shorter ones when the game ends earlier)
 const openings=k=>{let out=[[]];for(let j=0;j<k;j++){const nx=[];for(const s of out){const t=new T.Timeline(R.create(level,level.capacity||5));let ok=true;
   for(const a of s){const m=asap(T,t),x=m.find(y=>JSON.stringify(y)===a);if(!x){ok=false;break;}t.commit(x);}
   const m=ok?asap(T,t):[];if(!m.length){nx.push(s);continue;}for(const a of m)nx.push([...s,JSON.stringify(a)]);}out=nx;}return out;};
 const o1=openings(1),w1=o1.filter(wins).length,o3=openings(depth),w3=o3.filter(wins).length;
 return {first:{openings:o1.length,wins:w1},three:{openings:o3.length,wins:w3,share:Math.round(w3/o3.length*100)/100},ok:w1===0&&w3/o3.length<=limit,strategies:strats};}

// forced decisions (§5): along random games (a launch as soon as one is possible), the share of decisions where exactly
// one pod or group can be launched; at most 35 %
export async function forcedShare(level,games=30,seed=12345){const E=await engine(),{R,T}=E,r=rng(seed);let one=0,all=0;
 for(let g=0;g<games;g++){const t=new T.Timeline(R.create(level,level.capacity||5));let k=0;
  while(k++<100000){if(t.state.status==='Won')break;const m=R.legal(t.state).filter(a=>!t.check(a)),taps=m.filter(a=>a.type!=='bridge');
   if(m.length){if(taps.length){all++;if(taps.length===1)one++;}t.commit(m[Math.floor(r()*m.length)]);continue;}
   if(!t.pending)break;t.advance(t.steps[0].at-t.now);}}
 return all?Math.round(one/all*100)/100:0;}

// a pod holds a place (§6): a visible pod in the first two rows of a column whose colour has no reachable cell at the start
export async function holdPod(level){const {R}=await engine(),s=R.create(level,level.capacity||5),reach=new Set(R.reach(s).targets.map(i=>s.board[i]));
 return level.tray.some(st=>st.slice(0,2).some(p=>!level.pods[p].mystery&&!reach.has(R.CHANNELS.indexOf(level.pods[p].color))));}

// a hidden pod matters (§8, introductions): on the saved solution some hidden pod reveals in the first half of the
// launches, brings a colour no other top pod shows at that moment, and is launched with at most two other launches
// between its reveal and its own launch (a place had to be kept for a colour the player could not plan for)
export async function hiddenMatters(level,route){const E=await engine(),{idle:R}=E,hid=level.pods.map((p,i)=>p.mystery?i:-1).filter(i=>i>=0);
 if(!hid.length||!route)return {ok:false,pods:[]};let s=R.create(level,level.capacity||5),launches=0;const at={},fresh={},out=[];
 const total=route.filter(a=>a.type!=='bridge').length;
 const tops=()=>level.tray.map((st,c)=>st[s.heads[c]]).filter(p=>p!==undefined);
 const seen=()=>{for(const p of hid)if(s.revealed[p]&&at[p]===undefined){at[p]=launches;fresh[p]=!tops().some(q=>q!==p&&level.pods[q].color===level.pods[p].color);}};seen();
 for(const a of route){const st=R.step(s,a);if(!st.accepted)break;s=st.state;if(a.type!=='bridge'){const ps=a.pods||[a.pod];launches++;for(const p of ps)if(hid.includes(p)&&at[p]!==undefined)out.push({pod:p,revealAt:at[p],gap:launches-1-at[p],newColour:fresh[p]});}seen();}
 return {ok:out.some(x=>x.gap<=2&&x.newColour&&x.revealAt<total/2),pods:out};}

// The bridge as a decision (§8): exactly one bridge has a live winning witness and the other a loss certificate;
// each site must actually be offered in some §5 random games BEFORE a bridge is chosen. A legal bridge is optional:
// waiting or launching another pod can expose its alternative later, so first appearance is not the end of the trial.
export async function bridgeSingle(level,games=60,seed=3,known=null){const E=await engine(),{R,T}=E,win=known||(await bridgeChoicesWin(level)).each,seen=win.map(()=>0);
 for(let g=0;g<games;g++){const t=new T.Timeline(R.create(level,level.capacity||5)),r=rng(seed*977+g),offered=new Set();let k=0;
  const settleNow=()=>{while(t.pending&&t.steps[0].at<=t.now+1e-7)t.advance(0);};
  const waitEvent=()=>{while(t.pending){const e=t.steps[0];t.advance(Math.max(0,e.at-t.now));if(isStateEvent(e))break;}settleNow();};
  while(k++<200000){settleNow();if(t.state.status==='Won'||t.state.bridge>=0)break;
   const moves=R.legal(t.state).filter(a=>!t.check(a)),canWait=t.steps.some(isStateEvent);
   for(const a of moves)if(a.type==='bridge')offered.add(a.choice);
   if(!moves.length){if(t.pending){waitEvent();continue;}break;}
   const j=Math.floor(r()*(moves.length+(canWait?1:0)));if(j===moves.length){waitEvent();continue;}t.commit(moves[j]);}
  if(k>=200000)throw Error('bridge observation policy exceeded its event budget; outcome is unknown');
  for(const i of offered)seen[i]++;}
 const wins=win.filter(x=>x===true).length,loses=win.filter(x=>x===false).length;
 return {each:win,offered:seen,games,seed,protocol:'§5 extended policy; games offering each site before an actual bridge choice',ok:wins===1&&loses===win.length-1&&seen.every(x=>x>0)};}

// the loss with the other bridge comes later (17 §8, the owner, 2026-10-28: "проигрыш наступает не сразу при выборе, а
// позже, через материалы и места"): random games of the extended policy (§5; with restricted, games that may build only
// that bridge); of the games that build it, the launches still made after the choice until the game ends (chosen: built
// while another move was open; forced: built as the only move, with full places; won: such games won, 0 for a losing
// bridge). ok: some game builds it, and at least half of them launch at least `min` pods after the choice (the random
// policy taps whenever it can, so most bridges are built with full places; the launches after show whether the places
// still turn over for a while)
export async function bridgeLoss(level,choice,{games=120,seed=9,min=1,restricted=false}={}){const E=await engine(),{R,T}=E,after=[],forced=[];let won=0;
 for(let g=0;g<games;g++){const t=new T.Timeline(R.create(level,level.capacity||5)),r=rng(seed*7919+g*104729+1);let k=0,built=false,free=false,n=0;
  const settleNow=()=>{while(t.pending&&t.steps[0].at<=t.now+1e-7)t.advance(0);};
  const waitEvent=()=>{while(t.pending){const e=t.steps[0];t.advance(Math.max(0,e.at-t.now));if(isStateEvent(e))break;}settleNow();};
  while(k++<200000){settleNow();if(t.state.status==='Won'){if(built)won++;break;}
   const moves=R.legal(t.state).filter(a=>!t.check(a)&&(!restricted||a.type!=='bridge'||a.choice===choice)),canWait=t.steps.some(isStateEvent);
   if(!moves.length){if(t.pending){waitEvent();continue;}break;}
   const j=Math.floor(r()*(moves.length+(canWait?1:0)));if(j===moves.length){waitEvent();continue;}
   const a=moves[j];if(a.type==='bridge'){if(a.choice===choice){built=true;free=moves.length>1;}}else if(built)n++;t.commit(a);}
  if(built)(free?after:forced).push(n);}
 const all=[...after,...forced].sort((a,b)=>a-b),median=all.length?all[Math.floor((all.length-1)/2)]:null;
 return {choice,games,built:after.length+forced.length,chosen:after.length,forced:forced.length,won,medianLaunchesAfter:median,instant:all.length?Math.round(all.filter(x=>x===0).length/all.length*100)/100:null,ok:all.length>0&&median>=min};}

// slot pressure (layer B of 17 §13, content/rounds/quality.json; the owner's note to v12 L9: full places with cells still
// deep in the picture make a round interesting): along random games (a launch as soon as one is possible, uniform among
// launches, else the next event), time-weighted, the mean number of placed pods whose colour has no reachable cell
// ("stuck") and the share of time with at most one free place ("tight")
export async function pressure(level,games=30,seed=777){const E=await engine(),{R,T}=E,r=rng(seed);let tight=0,idle=0,total=0;
 for(let g=0;g<games;g++){const t=new T.Timeline(R.create(level,level.capacity||5));let k=0;
  while(k++<100000&&t.state.status!=='Won'){const m=R.legal(t.state).filter(a=>!t.check(a));if(m.length){t.commit(m[Math.floor(r()*m.length)]);continue;}if(!t.pending)break;
   const dt=t.steps[0].at-t.now,s=t.state,free=[...s.slots].filter(x=>x<0).length,reach=new Set(R.reach(s).targets.map(i=>s.board[i]));
   const stuck=[...s.slots].filter(p=>p>=0&&s.remaining[p]>0&&!reach.has(s.meta.colours[p])).length;
   if(dt>0){total+=dt;if(free<=1)tight+=dt;idle+=stuck*dt;}t.advance(dt);}}
 return {stuck:total?Math.round(idle/total*100)/100:0,tight:total?Math.round(tight/total*100)/100:0};}

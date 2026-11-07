// Checks of a candidate level on the round engine (product/17_LEVEL_GENERATION_RULES.md §1, §5, §8, §10, §11):
// the saved solution (search, then replay on the live timeline), the extended random test with waiting and the
// Wilson interval, the simple strategies, the fairness of hidden pods, the rhythm by fifths of done work.
import {rulesSync} from './engine.mjs';
import {solver} from '../level-stats/solve.mjs';
import {run} from '../level-stats/trace.mjs';
import {STRATEGIES,rng} from '../level-stats/strategies.mjs';
// speed (tools/rounds/perf/README.md, the owner, 2026-11-02): the games run on the fast twin of the reference engine
// (perf/engines.mjs, the same games byte for byte) and the independent games of a test on worker threads (perf/pool.mjs,
// ROUNDS_THREADS), put back in index order, so every result is the one the reference gives in one thread
import {engine} from './perf/engines.mjs';
import {games} from './perf/pool.mjs';
import {isStateEvent,asap,greedyFrom,heroChoices} from './perf/games.mjs';
// one game of the extended random policy (§5; perf/games.mjs): before each choice the events of the moment are processed;
// the bot picks uniformly among all legal actions (a pod, a whole group, each bridge place) and, when a state-changing
// event is scheduled, one extra option: wait until that event. With no action and a scheduled event, time moves on by itself.
export {randomGame} from './perf/games.mjs';
export {engine};
export function wilson(w,n,z=1.96){if(!n)return [0,1];const p=w/n,d=1+z*z/n,c=p+z*z/(2*n),h=z*Math.sqrt(p*(1-p)/n+z*z/(4*n*n));return [Math.max(0,(c-h)/d),Math.min(1,(c+h)/d)];}
export const classOfShare=p=>p>=.5?'Normal':p>=.2?'Hard':p>=.05?'Super Hard':'too hard';
// 100 games; widened to 400 in total when the 95 % Wilson interval crosses a class border (§5)
// bridge: the class of a bridge level is measured with its best bridge only (17 §5, the owner, 2026-10-30: the bridge adds
// a choice, not difficulty; a player who loses with the wrong one takes the right one after the free retry)
// exit: likewise a deciding-exit water channel level (a hand Super Hard, channelKind single: exactly one exit wins) is
// measured with its winning exit only (17 §5, the owner, 2026-11-05); the bot's channel actions keep that exit, every
// other action stays. Without it (Normal and Hard) the exit stays one of the bot's uniform actions.
// early (the owner, 2026-11-02; off by default): the sequential test stops at the first checkpoint (every `step` games
// from `min`, and at `first` and `max`) where the Wilson interval crosses no class border (and, with `zone` [lo, hi], neither
// of the zone's ends); runs, wins, share and class are those of the games played. Such a result is NOT a §5 result: the
// game's proofs accept 100 or 400 runs only (round-proof.schema.json, RoundSetValidator), so a placed level needs the full
// test. ROUNDS_RANDOM_EARLY=1 turns it on for every call that does not say full: true.
const CLASS_BORDERS=[.05,.2,.5];
export const crossesAny=(borders,[a,b])=>borders.some(x=>a<x&&b>=x);
export async function randomTest(level,{seed=1,first=100,max=400,bridge=null,exit=null,early=null,full=false,step=20,min=40,zone=null}={}){
 const keep={...(bridge===null?{}:{bridge}),...(exit===null?{}:{exit})},seq=!full&&(early??process.env.ROUNDS_RANDOM_EARLY==='1');
 const crosses=ci=>crossesAny(CLASS_BORDERS,ci),p=exit===null?{seed,bridge}:{seed,bridge,exit};let res;
 if(!seq){res=await games('random',level,p,{count:max>first?max:first,checkpoints:[first],stop:r=>!crosses(wilson(r.filter(Boolean).length,r.length))});
  if(max<=first)res=res.slice(0,first);}
 else{const borders=zone?[...CLASS_BORDERS,...zone]:CLASS_BORDERS,points=new Set([first,max]);for(let n=min;n<max;n+=step)points.add(n);
  res=await games('random',level,p,{count:Math.max(first,max),checkpoints:[...points].filter(n=>n<=Math.max(first,max)).sort((a,b)=>a-b),stop:r=>!crossesAny(borders,wilson(r.filter(Boolean).length,r.length))});}
 const n=res.length,wins=res.filter(Boolean).length,ci=wilson(wins,n);
 return {runs:n,wins,share:Math.round(wins/n*1000)/1000,wilson95:ci.map(x=>Math.round(x*1000)/1000),cls:classOfShare(wins/n),borderline:crosses(ci),protocol:'§5 extended: uniform over legal actions + wait before a scheduled event, ×1',seedBase:seed,...keep,...(seq?{early:{step,min,...(zone?{zone}:{})}}:{})};}
// the calm test (17 §5, the owner's amendment of 2026-10-31, after the Colony Flow patterns): the same choices as the random
// test (a pod, a whole group, the best bridge of a bridge level), but before every one the board plays until nothing more
// changes (the idle engine), so only the order of the choices counts, never the tempo. Hard ≤ 50 %, Super Hard ≤ 25 %,
// a regular Normal ≥ 50 % (CF's exact models: Hard 25–27 %, Super Hard 12–16 %, Normal 68–100 %).
export async function calmTest(level,{first=200,max=400,seed=3,bridge=null,exit=null,borders=[.25,.5]}={}){
 // 200 games, widened to 400 when the 95 % Wilson interval crosses a threshold of the calm rule (as the class test does)
 const crosses=([a,b])=>borders.some(x=>a<x&&b>=x);
 const res=(await games('calm',level,exit===null?{seed,bridge}:{seed,bridge,exit},{count:max>first?max:first,checkpoints:[first],stop:r=>!crosses(wilson(r.filter(Boolean).length,r.length))})).slice(0,max>first?max:first);
 const n=res.length,wins=res.filter(Boolean).length,ci=wilson(wins,n);
 return {runs:n,wins,share:Math.round(wins/n*1000)/1000,wilson95:ci.map(x=>Math.round(x*1000)/1000),borderline:crosses(ci),protocol:'§5 calm: uniform over legal actions, the board settled before each',seedBase:seed,...(bridge!==null?{bridge}:{}),...(exit!==null?{exit}:{})};}
// the restriction a recorded random test was measured with (its best bridge, its winning channel exit), for the tests that
// repeat it (calmTest in audit.mjs and levels.mjs place / checks): {} when every action was open
export const classKeep=r=>({...(r&&r.bridge!=null?{bridge:r.bridge}:{}),...(r&&r.exit!=null?{exit:r.exit}:{})});
// the calm rule of a class (17 §5): null when it holds, else the reason. A result whose Wilson interval still crosses the
// class's own threshold after 400 games (Super Hard 25 %, Hard and Normal 50 %) is borderline: it holds, as a borderline
// class does, and is left to a person
export const calmThreshold=cls=>cls==='Super Hard'?.25:.5;
export function calmBorderline(cls,calm){if(!calm||!calm.wilson95)return false;const t=calmThreshold(cls),[a,b]=calm.wilson95;return a<t&&b>=t;}
export function calmFails(cls,calm,regularNormal=false){if(!calm||calmBorderline(cls,calm))return null;
 if(cls==='Hard'&&calm.share>.5)return `спокойная игра: Hard выигрывается в ${Math.round(calm.share*100)} % (не больше 50 %, §5)`;
 if(cls==='Super Hard'&&calm.share>.25)return `спокойная игра: Super Hard выигрывается в ${Math.round(calm.share*100)} % (не больше 25 %, §5)`;
 if(regularNormal&&cls==='Normal'&&calm.share<.5)return `спокойная игра: Normal выигрывается в ${Math.round(calm.share*100)} % (не меньше 50 %, §5)`;
 return null;}
// trap links (17 §8, the owner's amendment of 2026-10-31): a linked group with a member whose material has no cell on the
// open front at the start (its access depth is above 0, or a strip keeps it behind a bridge), so the group holds its places
// long; row is the group's highest start row in its column (1 = top)
const CODES16='GPBACDVYOLMRKNSETUFHXWZJ'; // the 24 materials (name kept from the 16-colour palette)
export function trapLinks(level){const {w,h,rowsBottomFirst:rows,entries}=level,g=rows.join(''),d=new Float64Array(w*h).fill(Infinity),dq=[];for(const e of entries){d[e]=0;dq.push(e);}
 while(dq.length){const c=dq.shift(),x=c%w,y=Math.floor(c/w);for(const [dx,dy] of [[1,0],[-1,0],[0,1],[0,-1]]){const u=x+dx,v=y+dy;if(u<0||v<0||u>=w||v>=h)continue;const n=v*w+u;if(g[n]==='#'||g[n]==='b')continue;
  const cost=CODES16.includes(g[c])?1:0,nd=d[c]+cost;if(nd<d[n]){d[n]=nd;cost?dq.push(n):dq.unshift(n);}}}
 const minD={};for(let i=0;i<g.length;i++)if(CODES16.includes(g[i]))minD[g[i]]=Math.min(minD[g[i]]??Infinity,d[i]);
 const groups=new Map();level.pods.forEach((p,i)=>{if(p.group){if(!groups.has(p.group))groups.set(p.group,[]);groups.get(p.group).push(i);}});
 const rowOf=i=>{for(const st of level.tray){const r=st.indexOf(i);if(r>=0)return r+1;}return null;};
 return [...groups].map(([group,m])=>({group,trap:m.some(i=>(minD[level.pods[i].color]??0)>0),row:Math.min(...m.map(rowOf))}));}
// a quick estimate for screening candidates before the full test (bridge, exit: the class's best bridge / winning exit)
export async function quickShare(level,count=40,seed=11,bridge=null,exit=null){return (await games('quick',level,exit===null?{seed,bridge}:{seed,bridge,exit},{count})).filter(Boolean).length/count;}

// the saved winning solution: full-information search on the patient model, replayed on the live timeline (§1, §13.7)
// (a water channel: the search tries an exit as soon as one may be chosen, exit 0 first, then exit 1, so the saved
// solution shows the choice; R.legal offers the exits after the taps)
export async function solve(level,budget=200000){const E=await engine(),S=solver(E,budget,null,level.channel?a=>a.type==='channel':null),cap=level.capacity||5;
 const r=S.route(level,cap);if(!r.route)return {win:r.win,route:null,nodes:r.nodes};
 const tr=run(E,level,{commands:r.route.map(a=>({t:0,action:a})),patient:true});
 const dec=S.decisions(level,cap,r.route);
 // the same actions tapped the moment each is accepted (no waiting for work to settle): a lower bound of the duration;
 // it may lose, which is allowed (§6: the solution may need a reserve of places or a pause)
 const fast=run(E,level,{commands:r.route.map(a=>({t:0,action:a}))});
 return {win:true,route:r.route,nodes:r.nodes,replay:tr.status,trace:tr,fast:{status:fast.status,seconds:Math.round(fast.end/100)/10},decisions:{forks:dec.risky,single:dec.tight,unknown:dec.unknown}};}

// fairness of hidden pods (§8): in every world where the hidden pods carry each other's material and count, following
// the saved solution until the first differing pod shows must still leave a winning continuation
// (a fresh solver for each world: the memo is keyed by a state signature that does not hold the pods' colours, so
// worlds that swap colours of equal counts would share answers)
export async function fairness(level,route,limit=24){const E=await engine(),{idle:R}=E,hid=level.pods.map((p,i)=>p.mystery?i:-1).filter(i=>i>=0);
 if(hid.length<2||!route)return {checked:0,fair:true,note:hid.length<2?'одна скрытая — нечего переставлять':'нет решения'};
 const vals=hid.map(i=>[level.pods[i].color,level.pods[i].count]),worlds=[];
 const perm=(a,k=0)=>{if(worlds.length>=limit)return;if(k===a.length){if(a.some((v,i)=>v!==i))worlds.push(a.slice());return;}for(let i=k;i<a.length;i++){[a[k],a[i]]=[a[i],a[k]];perm(a,k+1);[a[k],a[i]]=[a[i],a[k]];}};
 perm(hid.map((_,i)=>i));let bad=0,unknown=0;
 for(const w of worlds){const pods=level.pods.map(p=>({...p}));w.forEach((src,j)=>{pods[hid[j]].color=vals[src][0];pods[hid[j]].count=vals[src][1];});
  const lv={...level,pods},diff=hid.filter((_,j)=>w[j]!==j&&(vals[w[j]][0]!==vals[j][0]||vals[w[j]][1]!==vals[j][1]));if(!diff.length)continue;
  // the world must keep the materials' balance; a swap between different colours changes it, so such worlds are not consistent
  const bal=c=>pods.filter(p=>p.color===c).reduce((n,p)=>n+p.count,0),orig=c=>level.pods.filter(p=>p.color===c).reduce((n,p)=>n+p.count,0);
  if([...new Set(vals.map(v=>v[0]))].some(c=>bal(c)!==orig(c)))continue;
  let s=R.create(lv,lv.capacity||5),k=0;
  while(k<route.length&&!diff.some(p=>s.revealed[p])){const st=R.step(s,route[k]);if(!st.accepted){s=null;break;}s=st.state;k++;}
  if(!s){bad++;continue;}const ok=solver(E,60000).winnable(s);if(ok===false)bad++;else if(ok===null)unknown++;}
 return {checked:worlds.length,unfair:bad,unknown,fair:bad===0&&unknown===0,note:'перестановки материала и числа между скрытыми подами с тем же балансом'};}

// hidden cells (17 §8 «Скрытые клетки», the owner, 2026-11-01): the cells still hidden after the start reveal (the dose of
// §8 counts only these); 0 on a level without hidden cells
export const cellsHiddenAtStart=level=>level.hiddenCells&&level.hiddenCells.length?rulesSync().create(level,level.capacity||5).veiledLeft:0;
// fairness of hidden cells (17 §8: hidden cells and hidden pods are two separate sources of the unknown, each checked):
// worlds where two cells still hidden at the start, of different materials, swap their materials (the balance holds);
// following the saved solution until one of them shows must still leave a winning continuation. Until a swapped cell
// reveals, the two worlds play alike (a hidden cell is never a target), so the player could not have told them apart.
// Up to `limit` swaps, spread over the pairs by a fixed seed; a fresh solver for each world.
export async function cellFairness(level,route,limit=12,seed=7){const E=await engine(),{idle:R}=E;
 if(!level.hiddenCells||!level.hiddenCells.length||!route)return {checked:0,fair:true,note:'нет скрытых клеток'};
 const s0=R.create(level,level.capacity||5),hid=level.hiddenCells.filter(i=>s0.veiled[i]),g=level.rowsBottomFirst.join(''),pairs=[];
 for(let a=0;a<hid.length;a++)for(let b=a+1;b<hid.length;b++)if(g[hid[a]]!==g[hid[b]])pairs.push([hid[a],hid[b]]);
 if(!pairs.length)return {checked:0,fair:true,note:'все скрытые клетки одного материала — нечего переставлять'};
 const r=rng(seed),pick=[];const pool=pairs.slice();while(pick.length<limit&&pool.length)pick.push(pool.splice(Math.floor(r()*pool.length),1)[0]);
 let bad=0,unknown=0;
 for(const [a,b] of pick){const cells=[...g];[cells[a],cells[b]]=[cells[b],cells[a]];const rows=[];for(let y=0;y<level.h;y++)rows.push(cells.slice(y*level.w,(y+1)*level.w).join(''));
  const lv={...level,rowsBottomFirst:rows};let s=R.create(lv,lv.capacity||5),k=0;
  while(k<route.length&&s.veiled[a]&&s.veiled[b]){const st=R.step(s,route[k]);if(!st.accepted){s=null;break;}s=st.state;k++;}
  if(!s){bad++;continue;}const ok=s.status==='Won'?true:solver(E,60000).winnable(s);if(ok===false)bad++;else if(ok===null)unknown++;}
 return {checked:pick.length,unfair:bad,unknown,fair:bad===0&&unknown===0,note:'перестановки материалов двух скрытых клеток разного цвета'};}

// the simple strategies of §11, each with the leftmost and the rightmost hero choice, tapping as soon as possible
export async function strategies(level){const E=await engine(),{R,T}=E,out={};
 for(const [k,st] of Object.entries(STRATEGIES)){if(st.random||k==='planner'||k==='big-first')continue;
  for(const side of ['left','right']){const pol=st.make(1,E),t=new T.Timeline(R.create(level,level.capacity||5));let g=0;
   while(g++<100000){if(t.state.status==='Won')break;const m=R.legal(t.state).filter(a=>!t.check(a));
    if(m.length){const br=heroChoices(m);const a=br.length?(side==='left'?br[0]:br.at(-1)):pol(t,m);if(!a||t.check(a)){if(t.pending){t.advance(t.steps[0].at-t.now);continue;}break;}t.commit(a);continue;}
    if(!t.pending)break;t.advance(t.steps[0].at-t.now);}
   out[`${k}|${side}`]=t.state.status==='Won';}}
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
// a fresh solver for each bridge: the solver memo is keyed by the state signature, which does not say which bridges the
// level offers, so one memo across the two restricted levels answered the second from the first (before 2026-10-28 every
// bridge level measured [x, x], and "exactly one bridge wins" could never be found)
export async function bridgeChoicesWin(level){const E=await engine(),out=[];
 for(let i=0;i<(level.hero?level.hero.choices.length:0);i++){const lv={...level,hero:{...level.hero,choices:[level.hero.choices[i]]}};out.push(solver(E,200000).winnable(E.idle.create(lv,level.capacity||5)));}
 return {each:out,ok:out.length>0&&out.every(x=>x===true)};}

// the water channel as a choice (17 §8 «Drop Водный канал», §9; the owner, 2026-11-05): for each exit, whether the level is
// won with only that exit (a fresh solver, its legal actions filtered) and the random test's share when only that exit may
// be chosen (n games); decides = exactly one exit wins (a hand Super Hard's choice), gap = best share − worst share. The
// class test itself (randomTest) plays the exit as one of the bot's uniform actions (17 §5, as RoundRandomPolicy does),
// except on a deciding-exit Super Hard (channelKind single, the owner, 2026-11-05): randomTest(level, {exit}) with the exit
// that alone wins (winningExit).
export async function channelExits(level,n=60,seed=5){if(!level.channel)return null;const E=await engine(),each=[];
 for(let i=0;i<level.channel.exits.length;i++){const win=solver(E,200000,a=>a.type!=='channel'||a.exit===i).winnable(E.idle.create(level,level.capacity||5));
  const sh=(await games('exit',level,{seed,exit:i},{count:n})).filter(Boolean).length;each.push({exit:i,win,share:Math.round(sh/n*100)/100});}
 const sh=each.map(x=>x.share);return {each,gap:Math.round((Math.max(...sh)-Math.min(...sh))*100)/100,decides:each.filter(x=>x.win===true).length===1&&each.filter(x=>x.win===false).length===each.length-1,better:sh.indexOf(Math.max(...sh))};}
// the island behind water (17 §8 «Остров за водой», the owner, 2026-11-07): the colour cells the entries never reach without
// the channel (every cell but '#' passable, the channel never chosen) are the island; its share of N is 25–40 %
// (ISLAND_SHARE), both exits stand on it and the spring off it, and with either exit as the only new entry every colour
// cell is reachable (outside from the entries, the island from that exit). On the saved solution, replayed patiently on
// the timeline (as shutterCheck), the spring is reached (channelReady) at 20–30 % W (CHANNEL_READY) and the exit chosen
// at once or later. Returns {island: {cells, share}, exitsOn, ready: {atW}, open: {exit, atW}, ok, problems, notes}.
export const ISLAND_SHARE=[.25,.4],CHANNEL_READY=[.2,.3];
export function islandOf(level){const {w,h,rowsBottomFirst:rows,entries}=level,g=rows.join(''),n=w*h,col=i=>g[i]!=='.'&&g[i]!=='#';
 const flood=src=>{const seen=new Uint8Array(n),q=[];for(const s of src)if(g[s]!=='#'&&!seen[s]){seen[s]=1;q.push(s);}
  for(let k=0;k<q.length;k++){const i=q[k],x=i%w;for(const j of [i-w,i+w,x>0?i-1:-1,x<w-1?i+1:-1])if(j>=0&&j<n&&!seen[j]&&g[j]!=='#'){seen[j]=1;q.push(j);}}return seen;};
 const a0=flood(entries),N=[...g].filter((c,i)=>col(i)).length,island=[];for(let i=0;i<n;i++)if(col(i)&&!a0[i])island.push(i);
 const ch=level.channel,each=ch?ch.exits.map(e=>{const f=flood([...entries,e.cell]);let miss=0;for(let i=0;i<n;i++)if(col(i)&&!f[i])miss++;return {on:!a0[e.cell],miss};}):[];
 return {cells:island.length,share:N?Math.round(island.length/N*1000)/1000:0,N,each,springOn:ch?!a0[ch.spring]:null};}
export async function channelCheck(level,route){const ch=level.channel;if(!ch)return null;const E=await engine(),{R,T}=E;
 const W=level.pods.reduce((n,p)=>n+p.count,0),t=new T.Timeline(R.create(level,level.capacity||5));let ready=null,open=null;
 if(R.channelChoices(t.state).length)ready={atW:0};
 const watch=events=>{for(const e of events){if(e.type==='channelReady'&&!ready)ready={atW:Math.round(t.processedCells/W*1000)/1000};if(e.type==='channelOpen'&&!open)open={exit:e.exit,atW:Math.round(t.processedCells/W*1000)/1000};}};
 const settle=()=>{while(t.pending){t.advance(Math.max(0,t.steps[0].at-t.now));watch(t.lastEvents);}};
 for(const a of route||[]){settle();const r=t.commit(a);if(!r.accepted)break;watch(r.events);watch(t.lastEvents);}
 settle();const isl=islandOf(level),problems=[],notes=[];
 if(isl.share<ISLAND_SHARE[0]||isl.share>ISLAND_SHARE[1])problems.push(`канал: остров за водой ${Math.round(isl.share*100)} % N, нужно 25–40 % (§8 «Остров за водой»)`);
 isl.each.forEach((e,i)=>{if(!e.on)problems.push(`канал: выход ${i+1} не на острове (§8)`);if(e.miss)problems.push(`канал: с выходом ${i+1} недоступно цветных клеток ${e.miss} (§8)`);});
 if(isl.springOn)problems.push('канал: родник на острове (§8)');
 if(!ready)problems.push('канал: родник не достигнут на сохранённом решении (§8)');
 else if(ready.atW<CHANNEL_READY[0]||ready.atW>CHANNEL_READY[1])problems.push(`канал: родник открыт при ${Math.round(ready.atW*100)} % W на сохранённом решении, нужно 20–30 % (§8 «Остров за водой»)`);
 if(!open)problems.push('канал: выход не выбран на сохранённом решении (§8)');
 return {island:{cells:isl.cells,share:isl.share},exitsOn:isl.each.map(e=>e.on),ready,open,ok:!problems.length,problems,notes};}
// the exit that alone wins when a channel decides (channelExits(...).decides), else null
export const winningExit=ch=>ch&&ch.decides?ch.each.findIndex(x=>x.win===true):null;
// wooden shutters on the saved solution (17 §8 «Twig Деревянные створки»): the solution replayed patiently on the timeline
// (as latchCheck): when each shutter opens (done work W as a share, the command after which), and whether at that moment a
// cell under it is a target at once (a new front: its cubes are reachable work); problems (mandatory): a shutter that never
// opens or opens at 80 % W or later; notes: no new front at the opening.
export async function shutterCheck(level,route){const ss=level.shutters||[];if(!ss.length)return null;const E=await engine(),{R,T}=E;
 const W=level.pods.reduce((n,p)=>n+p.count,0),t=new T.Timeline(R.create(level,level.capacity||5)),opened=ss.map(()=>null);let k=0;
 const watch=events=>{for(const e of events)if(e.type==='shutterOpen'&&!opened[e.shutter]){const tg=new Set(R.reach(t.state).targets);opened[e.shutter]={atW:Math.round(t.processedCells/W*1000)/1000,command:k,front:ss[e.shutter].cells.filter(c=>tg.has(c)).length};}};
 const settle=()=>{while(t.pending){t.advance(Math.max(0,t.steps[0].at-t.now));watch(t.lastEvents);}};
 for(const a of route||[]){settle();const r=t.commit(a);if(!r.accepted)break;k++;watch(r.events);watch(t.lastEvents);}
 settle();const problems=[],notes=[],rows=ss.map((s,i)=>({shutter:i,material:s.material,count:s.count,cells:s.cells.length,...(opened[i]||{atW:null})}));
 for(const x of rows){const tag=`створки ${x.shutter+1} (${x.material} ${x.count})`;if(x.atW===null)problems.push(`${tag}: не открылись на сохранённом решении (§8)`);
  else{if(x.atW>=.8)problems.push(`${tag}: открылись при ${Math.round(x.atW*100)} % W на сохранённом решении, нужно до 80 % (§8)`);if(!x.front)notes.push(`${tag}: при открытии ни одна клетка под ними не доступна сразу (§8: новый фронт)`);}}
 const ats=rows.map(x=>x.atW).filter(x=>x!=null);return {shutters:rows,lastOpenAtW:ats.length?Math.max(...ats):null,ok:!problems.length,problems,notes};}

// the bridge as a choice (the owner, 2026-10-27: "надо выбрать, какой мост открывать"): for each bridge, whether it keeps a
// winning continuation and the random test's share when only that bridge may be built; gap = best share − worst share
// (win: each bridge's solver verdict when already known)
export async function bridgeChoices(level,n=60,seed=5,known=null){const win=known||(await bridgeChoicesWin(level)).each,each=[];
 const shares=await Promise.all(win.map((_,i)=>games('choice',level,{seed,choice:i},{count:n})));
 for(let i=0;i<win.length;i++){const w=shares[i].filter(Boolean).length;each.push({label:level.hero.choices[i].label,win:win[i],share:Math.round(w/n*100)/100});}
 const sh=each.map(x=>x.share);return {each,gap:Math.round((Math.max(...sh)-Math.min(...sh))*100)/100,better:sh.indexOf(Math.max(...sh))};}

// the greedy strategies of 17 §5 (the owner's amendment of 2026-10-27): "most work now" must lose a Hard, and with
// "release a place fastest" a Super Hard, at both hero choices; returns which of them win
export async function greedy(level){const E=await engine(),{R,T}=E,out={};
 for(const k of ['most-now','fast-release'])for(const side of ['left','right']){const pol=STRATEGIES[k].make(1,E),t=new T.Timeline(R.create(level,level.capacity||5));let g=0;
  while(g++<100000){if(t.state.status==='Won')break;const m=R.legal(t.state).filter(a=>!t.check(a));
   if(m.length){const br=heroChoices(m);const a=br.length?(side==='left'?br[0]:br.at(-1)):pol(t,m);if(!a||t.check(a)){if(t.pending){t.advance(t.steps[0].at-t.now);continue;}break;}t.commit(a);continue;}
   if(!t.pending)break;t.advance(t.steps[0].at-t.now);}
  out[`${k}|${side}`]=t.state.status==='Won';}
 return out;}
export const greedyBeaten=(g,cls)=>cls==='Hard'?!g['most-now|left']&&!g['most-now|right']:cls==='Super Hard'?!Object.values(g).some(Boolean):true;

// ---- the owner's second amendment of 2026-10-27 (after pilot v11) ----
// "opening + colours" (§5): every opening of k first actions, each taken as soon as one is possible, then the greedy
// strategies; Hard: "most work now", Super Hard: also "release a place fastest", at both hero choices. No 1-action
// opening may win; after 3-action openings at most 35 % may win. Distinct action sequences are counted once.
export async function openingTest(level,cls,{depth=3,limit=0.35}={}){const E=await engine(),{R,T}=E;
 const strats=cls==='Super Hard'?['most-now','fast-release']:['most-now'],sides=level.hero?['left','right']:['left'];
 // the openings are played on the game workers (perf/games.mjs openingWins), counted in order
 const wins=async list=>(await games('opening',level,{openings:list,strats,sides},{count:list.length})).filter(Boolean).length;
 // all distinct openings of exactly k actions (shorter ones when the game ends earlier)
 const openings=k=>{let out=[[]];for(let j=0;j<k;j++){const nx=[];for(const s of out){const t=new T.Timeline(R.create(level,level.capacity||5));let ok=true;
   for(const a of s){const m=asap(E,t),x=m.find(y=>JSON.stringify(y)===a);if(!x){ok=false;break;}t.commit(x);}
   const m=ok?asap(E,t):[];if(!m.length){nx.push(s);continue;}for(const a of m)nx.push([...s,JSON.stringify(a)]);}out=nx;}return out;};
 const o1=openings(1),w1=await wins(o1),o3=openings(depth),w3=await wins(o3);
 return {first:{openings:o1.length,wins:w1},three:{openings:o3.length,wins:w3,share:Math.round(w3/o3.length*100)/100},ok:w1===0&&w3/o3.length<=limit,strategies:strats};}

// forced decisions (§5): along random games (a launch as soon as one is possible), the share of decisions where exactly
// one pod or group can be launched; at most 35 %
export async function forcedShare(level,games=30,seed=12345){const E=await engine(),{R,T}=E,r=rng(seed);let one=0,all=0;
 for(let g=0;g<games;g++){const t=new T.Timeline(R.create(level,level.capacity||5));let k=0;
  while(k++<100000){if(t.state.status==='Won')break;const m=R.legal(t.state).filter(a=>!t.check(a)),taps=m.filter(a=>a.type!=='bridge'&&a.type!=='channel');
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
 // the column tops the player sees (a pod behind a closed latch is not one, 17 §8)
 const tops=()=>R.topPods(s);
 const seen=()=>{for(const p of hid)if(s.revealed[p]&&at[p]===undefined){at[p]=launches;fresh[p]=!tops().some(q=>q!==p&&level.pods[q].color===level.pods[p].color);}};seen();
 for(const a of route){const st=R.step(s,a);if(!st.accepted)break;s=st.state;if(a.type!=='bridge'){const ps=a.pods||[a.pod];launches++;for(const p of ps)if(hid.includes(p)&&at[p]!==undefined)out.push({pod:p,revealAt:at[p],gap:launches-1-at[p],newColour:fresh[p]});}seen();}
 return {ok:out.some(x=>x.gap<=2&&x.newColour&&x.revealAt<total/2),pods:out};}

// the bridge as a decision (§8, from the second introduction): exactly one bridge keeps a winning continuation, proven
// for the other, and random games reach each bridge site in some games (else there is no choice): offered counts, per
// bridge, the games where it is buildable at the first moment any bridge is. The games are those of the random test
// (§5, policy 'random': uniform over legal actions plus waiting before a scheduled event; the coordinator's decision of
// 2026-10-30, 17 §8 "в части партий случайного теста"). policy 'tap' is the older measure (launch whenever possible,
// no waiting), kept for comparison: on a Super Hard queue its games jam before either approach is cleared
export async function bridgeSingle(level,count=60,seed=3,known=null,{policy='random'}={}){const win=known||(await bridgeChoicesWin(level)).each,seen=win.map(()=>0);
 for(const offered of await games('single',level,{seed,policy},{count}))for(const c of offered)seen[c]++;
 const wins=win.filter(x=>x===true).length,loses=win.filter(x=>x===false).length;
 return {each:win,offered:seen,policy,ok:wins===1&&loses===win.length-1&&seen.every(x=>x>0)};}

// the loss with the other bridge comes later (17 §8, the owner, 2026-10-28: "проигрыш наступает не сразу при выборе, а
// позже, через материалы и места"): random games of the extended policy (§5; with restricted, games that may build only
// that bridge); of the games that build it, the launches still made after the choice until the game ends (chosen: built
// while another move was open; forced: built as the only move, with full places; won: such games won, 0 for a losing
// bridge). ok: some game builds it, and at least half of them launch at least `min` pods after the choice (the random
// policy taps whenever it can, so most bridges are built with full places; the launches after show whether the places
// still turn over for a while)
export async function bridgeLoss(level,choice,{games:count=120,seed=9,min=1,restricted=false}={}){const after=[],forced=[];let won=0;
 for(const [built,free,n,w] of await games('loss',level,{seed,choice,restricted},{count})){if(w)won++;if(built)(free?after:forced).push(n);}
 const all=[...after,...forced].sort((a,b)=>a-b),median=all.length?all[Math.floor((all.length-1)/2)]:null;
 return {choice,games:count,built:after.length+forced.length,chosen:after.length,forced:forced.length,won,medianLaunchesAfter:median,instant:all.length?Math.round(all.filter(x=>x===0).length/all.length*100)/100:null,ok:all.length>0&&median>=min};}

// the colours with a reachable target cell (the fast twin keeps the targets by colour: the same set without the sorted list)
const reachColours=(R,s)=>{if(!R.targetsByColour)return new Set(R.reach(s).targets.map(i=>s.board[i]));const out=new Set();R.targetsByColour(s).forEach((l,c)=>{if(l&&l.length)out.add(c);});return out;};
// slot pressure (layer B of 17 §13, content/rounds/quality.json; the owner's note to v12 L9: full places with cells still
// deep in the picture make a round interesting): along random games (a launch as soon as one is possible, uniform among
// launches, else the next event), time-weighted, the mean number of placed pods whose colour has no reachable cell
// ("stuck") and the share of time with at most one free place ("tight")
export async function pressure(level,games=30,seed=777){const E=await engine(),{R,T}=E,r=rng(seed);let tight=0,idle=0,total=0;
 for(let g=0;g<games;g++){const t=new T.Timeline(R.create(level,level.capacity||5));let k=0;
  while(k++<100000&&t.state.status!=='Won'){const m=R.legal(t.state).filter(a=>!t.check(a));if(m.length){t.commit(m[Math.floor(r()*m.length)]);continue;}if(!t.pending)break;
   const dt=t.steps[0].at-t.now,s=t.state,free=[...s.slots].filter(x=>x<0).length,reach=reachColours(R,s);
   const stuck=[...s.slots].filter(p=>p>=0&&s.remaining[p]>0&&!reach.has(s.meta.colours[p])).length;
   if(dt>0){total+=dt;if(free<=1)tight+=dt;idle+=stuck*dt;}t.advance(dt);}}
 return {stuck:total?Math.round(idle/total*100)/100:0,tight:total?Math.round(tight/total*100)/100:0};}

// ---- latch + acorn key (17 §8 «Засов и жёлудь-ключ», the owner, 2026-11-01; README «Latch and key») ----
// the depth of a latch (pods of its column before it) by class, and the pods behind it: wanted 2–8, at most 12
export const LATCH_DEPTH={Normal:[3,6],Hard:[2,6],'Super Hard':[2,8]},LATCH_BEHIND={min:1,max:12,want:[2,8]};
// at most this many columns blocked (a closed latch on top) at once: 3 columns 1, 4 columns 2, 5 columns 3
export const latchLimit=cols=>Math.max(1,cols-2);
// the saved solution replayed patiently on the timeline (each command once all earlier work has settled, as the proof):
// when each latch opens (done work W as a share, the command after which, whether it stood closed on top first and for
// how long), the most columns blocked at once; and over `games` random games (a launch as soon as one is possible,
// uniform, as `pressure`) the share that ever block more columns than the limit (a note). problems are §8's mandatory
// rules for the class `cls` (depth, pods behind, every latch opens on the solution, the last before 80 % W, the column
// limit on the solution), notes its soft ones.
export async function latchCheck(level,route,cls,{games=30,seed=4242}={}){const ls=level.latches||[];if(!ls.length)return null;const E=await engine(),{R,T}=E;
 const W=level.pods.reduce((n,p)=>n+p.count,0),limit=latchLimit(level.tray.length),t=new T.Timeline(R.create(level,level.capacity||5));
 const opened=ls.map((_,i)=>t.state.latched[i]?null:{atW:0,command:0,waitMs:0,atStart:true}),topAt=ls.map(()=>null);let maxBlocked=0,k=0;
 const watch=events=>{for(const e of events)if(e.type==='latchOpen'&&!opened[e.latch])opened[e.latch]={atW:Math.round(t.processedCells/W*1000)/1000,command:k,waitMs:topAt[e.latch]===null?0:Math.round(t.now-topAt[e.latch])};
  const blocked=R.blockedColumns(t.state);maxBlocked=Math.max(maxBlocked,blocked.length);
  ls.forEach((l,i)=>{if(t.state.latched[i]&&blocked.includes(l.column)&&t.state.heads[l.column]===l.depth&&topAt[i]===null)topAt[i]=t.now;});};
 watch([]);
 const settle=()=>{while(t.pending){t.advance(Math.max(0,t.steps[0].at-t.now));watch(t.lastEvents);}};
 for(const a of route||[]){settle();const r=t.commit(a);if(!r.accepted)break;k++;watch(r.events);watch(t.lastEvents);}
 settle();
 // random games: how often the columns blocked at once exceed the limit
 let over=0;const r=rng(seed);
 for(let g=0;g<games;g++){const u=new T.Timeline(R.create(level,level.capacity||5));let n=0,hit=R.blockedColumns(u.state).length>limit;
  while(n++<100000&&u.state.status!=='Won'&&!hit){const m=R.legal(u.state).filter(a=>!u.check(a));if(m.length)u.commit(m[Math.floor(r()*m.length)]);else if(u.pending)u.advance(u.steps[0].at-u.now);else break;if(R.blockedColumns(u.state).length>limit)hit=true;}
  if(hit)over++;}
 const [lo,hi]=LATCH_DEPTH[cls]||LATCH_DEPTH.Normal,problems=[],notes=[];
 const rows=ls.map((l,i)=>({latch:i,column:l.column,depth:l.depth,behind:level.tray[l.column].length-l.depth,key:l.key,...(opened[i]||{atW:null})}));
 for(const x of rows){const tag=`засов ${x.latch+1} (колонка ${x.column+1})`;
  if(x.depth<lo||x.depth>hi)problems.push(`${tag}: глубина ${x.depth} вне ${lo}–${hi} для ${cls} (§8)`);
  if(x.behind<LATCH_BEHIND.min||x.behind>LATCH_BEHIND.max)problems.push(`${tag}: за ним подов ${x.behind}, нужно ${LATCH_BEHIND.min}–${LATCH_BEHIND.max} (§8)`);
  else if(x.behind<LATCH_BEHIND.want[0]||x.behind>LATCH_BEHIND.want[1])notes.push(`${tag}: за ним подов ${x.behind} (желательно ${LATCH_BEHIND.want.join('–')}, §8)`);
  if(x.atW===null)problems.push(`${tag}: не открылся на сохранённом решении (§8)`);
  else if(x.atW>=.8)problems.push(`${tag}: открылся при ${Math.round(x.atW*100)} % W на сохранённом решении, нужно до 80 % (§8)`);}
 if(maxBlocked>limit)problems.push(`на сохранённом решении засовы перекрыли сразу ${maxBlocked} колонки из ${level.tray.length} (не больше ${limit}, §8)`);
 if(over)notes.push(`в ${over} из ${games} случайных партий засовы перекрывали больше ${limit} колонок сразу (§8)`);
 const ats=rows.map(x=>x.atW).filter(x=>x!=null);
 return {latches:rows,maxBlocked,limit,lastOpenAtW:ats.length?Math.max(...ats):null,waits:rows.some(x=>x.waitMs>0),randomOverLimit:Math.round(over/games*100)/100,ok:!problems.length,problems,notes};}

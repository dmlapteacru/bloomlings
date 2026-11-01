// Route-first level generator (PROTOTYPE, the owner's idea with Yuri of 2026-11-01; README.md here, REPORT.md the
// comparison with the current builder). Nothing here changes the engine, the rules or the content: it reads an approved
// picture, builds candidate levels for one class and recipe and runs the same acceptance as levels.mjs build + place.
//
//   node tools/rounds/proto/route-first.mjs <P####> --cls Normal|Hard|"Super Hard" --recipe links+hidden
//        [--slot regular|after-hard] [--accents auto|on|off] [--want 2] [--minutes 20] [--seed 1] [--label L0022]
//        [--out content/work/proto-route/out]
//
// ROUNDS_DIR defaults to the private copy content/work/proto-route/rounds-copy (the content is never written).
//
// 1. Base: the picture's volumes split into pods (levels.mjs splitPods, the §3 budget; columns by N), dealt into the
//    columns at random (accent pods in the second row to the middle). A winning route is looked for with calm playouts
//    (the board settles before every launch: the idle engine, the solver's model), the least "canonical" winning route
//    kept (not column after column, not round-robin rows); the solver only if no playout wins.
// 2. Tune on the route: local moves (swap neighbouring pods in a column, swap pods across columns at about the same
//    depth, move counts between pods of one colour inside §3) that keep the known route winning (replayed, a few ms) and
//    bring the places held without work before each launch in the first 70 % of the route to a target (Normal low, Hard
//    and Super Hard higher; the target is steered by the class the quick screen measures); a regular Normal keeps its
//    holding pod (§6).
// 3. Overlays that keep the route (each replayed on the engine): links by the §7 dose (pairs whose second member is on
//    top when the first is launched, so the pair launches together; Hard prefers a long gap and a trap link, §8),
//    hidden pods (row 2+, preferring pods that reveal a colour no other top shows and launch soon after; runs allowed on
//    Hard), hidden cells as one contiguous chunk the route clears in its middle (never with hidden pods), the latch
//    (a column at the class depth whose pod behind the route takes late, the key in a picture hole the route reaches a
//    few launches before it needs that pod; the latch stands closed on top meanwhile).
// 4. Quick screen (20 random games), then levels.mjs evaluate and the placer's filters of the slot (calm test, holding
//    pod, trap link, forced, opening, doses …): the same acceptance as today.
import fs from 'node:fs';import path from 'node:path';
const HERE=path.dirname(new URL(import.meta.url).pathname),ROOT=path.resolve(HERE,'../../..');
process.env.ROUNDS_DIR=process.env.ROUNDS_DIR||path.join(ROOT,'content/work/proto-route/rounds-copy');
const ARGV=process.argv.slice(2);process.argv.splice(2,process.argv.length,'noop');
const LV=await import('../levels.mjs'),L=await import('../lib.mjs'),M=await import('../measure.mjs'),{engine}=await import('../engine.mjs');
const {fitness}=await import('../fitness.mjs');const {STRATEGIES}=await import('../../level-stats/strategies.mjs');
const E=await engine(),{R,idle:I}=E;
const opt=(k,d=null)=>{const i=ARGV.indexOf(k);return i>=0?ARGV[i+1]:d;};
const rnd=seed=>{let s=seed>>>0||1;return()=>{s^=s<<13;s^=s>>>17;s^=s<<5;return(s>>>0)/4294967296;};};
const CLASSES=['Normal','Hard','Super Hard'];
export const BAND={Normal:[.5,.75],'after-hard':[.75,.95],Hard:[.2,.5],'Super Hard':[.05,.2]};
// places held without work before a launch (idle model), first 70 % of the route: starting targets by class
const TARGET0={Normal:1.0,Hard:2.0,'Super Hard':2.8};

// ---- access depth (levels.mjs depths for a board without a bridge): colour cells between a cell and the entry ----
function access(board){const {w,h,rowsBottomFirst:rows,entries}=board,g=rows.join(''),d=new Float64Array(w*h).fill(Infinity),dq=[];for(const e of entries){d[e]=0;dq.push(e);}
 while(dq.length){const c=dq.shift(),x=c%w,y=Math.floor(c/w);for(const [dx,dy] of [[1,0],[-1,0],[0,1],[0,-1]]){const u=x+dx,v=y+dy;if(u<0||v<0||u>=w||v>=h)continue;const n=v*w+u;if(g[n]==='#')continue;
  const cost=L.CODES.includes(g[c])?1:0,nd=d[c]+cost;if(nd<d[n]){d[n]=nd;cost?dq.push(n):dq.unshift(n);}}}
 return {d,g};}

// ---- the context of one picture, class and recipe ----
function context(picId,cls,recipe,{accents=false,seed=1}={}){const pic=L.picture(picId);if(!pic)throw Error('нет картинки '+picId);
 let recolor=null,acc=null;
 if(accents){const ar=rnd(seed^0x2545f491),m0=L.measure(pic),room=(m0.N<400?6:10)-m0.colours;
  const a=room>0?LV.pickAccents(pic,null,ar,{materials:room>=2&&m0.colours<=7&&ar()<.4?2:1}):null;if(a){recolor=a.recolor;acc=a.accents;}}
 const m=L.measure(L.overlaid(pic,null,recolor)),board=L.boardOf(pic,null,recolor),base=LV.splitPods(m.volumes,m.N,null);if(base.error)throw Error(base.error);
 let pods=base.pods;if(acc){const ps=LV.accentPods(pods,acc,rnd(seed^0x51));if(ps.length<=L.budget(m.N)[1])pods=ps;}
 const {d,g}=access(board),minD={};for(let i=0;i<g.length;i++)if(L.CODES.includes(g[i]))minD[g[i]]=Math.min(minD[g[i]]??Infinity,d[i]);
 return {pic,cls,recipe,types:recipe==='none'?[]:recipe.split('+'),m,board,pods,recolor,accents:acc,accentSet:new Set((acc||[]).map(a=>a.code)),depth:d,minD,frontier:new Set(Object.keys(minD).filter(c=>minD[c]===0))};}
const mkLevel=(ctx,pods,tray,extra={})=>({id:1,title:ctx.pic.title,subject:ctx.pic.id,w:ctx.board.w,h:ctx.board.h,rowsBottomFirst:ctx.board.rowsBottomFirst,entries:ctx.board.entries,capacity:5,pods,tray,recipe:ctx.recipe,...(ctx.recolor?{recolor:ctx.recolor}:{}),...extra});

// ---- 1. a random deal: pods into columns (heights equal ±1), accent pods in rows 2..middle, a launch with work at the start ----
function deal(ctx,cols,r){const pods=ctx.pods.map(p=>({...p})),ord=pods.map((_,i)=>i).sort(()=>r()-.5),tray=Array.from({length:cols},()=>[]);
 ord.forEach((p,k)=>tray[k%cols].push(p));
 if(ctx.accentSet.size){const rows=Math.max(...tray.map(t=>t.length)),lo=1,hi=Math.max(1,Math.floor((rows-1)/2));
  for(const i of pods.map((p,i)=>i).filter(i=>ctx.accentSet.has(pods[i].color))){const c=tray.findIndex(t=>t.includes(i)),k=tray[c].indexOf(i);if(k>=lo&&k<=hi)continue;
   const to=[];tray.forEach((t,c2)=>t.forEach((q,k2)=>{if(k2>=lo&&k2<=hi&&!ctx.accentSet.has(pods[q].color))to.push([c2,k2]);}));if(!to.length)break;const [c2,k2]=to[Math.floor(r()*to.length)];[tray[c][k],tray[c2][k2]]=[tray[c2][k2],tray[c][k]];}}
 startOk(ctx,pods,tray,r);return {pods,tray};}
// §6: at least one top pod with reachable work; else a frontier pod swaps with a column top
function startOk(ctx,pods,tray,r){if(tray.some(t=>t.length&&ctx.frontier.has(pods[t[0]].color)))return;
 const cand=[];tray.forEach((t,c)=>t.forEach((p,k)=>{if(k>0&&ctx.frontier.has(pods[p].color))cand.push([c,k]);}));if(!cand.length)return;
 const [c,k]=cand[Math.floor(r()*cand.length)],c0=Math.floor(r()*tray.length);[tray[c0][0],tray[c][k]]=[tray[c][k],tray[c0][0]];}
const hasHold=(ctx,pods,tray)=>tray.some(t=>t.slice(0,2).some(p=>!pods[p].mystery&&!ctx.frontier.has(pods[p].color)));

// ---- replays on the idle engine (each launch settles before the next: the solver's and the calm test's model) ----
// a column route: each step taps the top of that column (before links: every pod alone)
function replayCols(level,cols){let s=I.create(level,5);const occ=[];
 for(const c of cols){if(s.status!=='Playing')break;occ.push(s.slots.reduce((n,p)=>n+(p>=0),0));const p=level.tray[c][s.heads[c]];if(p===undefined)return {won:false};const x=I.step(s,{type:'tap',pod:p});if(!x.accepted)return {won:false};s=x.state;}
 return {won:s.status==='Won',occ};}
// a pod route (groups launch on their first member); returns the timeline of the route for the overlays
function replayPods(level,route,{profile=false,holes=null}={}){let s=I.create(level,5);const total=s.board.reduce((n,c)=>n+(c>=0),0),done=[0],occ=[],launch={},topAt=level.tray.map(t=>new Array(t.length+1).fill(Infinity)),clearAt=new Int32Array(s.board.length).fill(-1),reachAt=holes?holes.map(()=>Infinity):null,topsAt=[];
 const mark=n=>{if(!profile)return;level.tray.forEach((_,c)=>{if(topAt[c][s.heads[c]]===Infinity)topAt[c][s.heads[c]]=n;});if(holes){const dist=R.reach(s).distance;holes.forEach((h,k)=>{if(reachAt[k]===Infinity&&dist[h.key]>0)reachAt[k]=n;});}topsAt.push(R.topPods(s));};
 mark(0);let n=0;
 for(const p of route){if(s.status!=='Playing')break;occ.push(s.slots.reduce((k,q)=>k+(q>=0),0));const before=profile?s.board.slice():null;const x=I.step(s,{type:'tap',pod:p});if(!x.accepted)return {won:false,at:n};s=x.state;
  for(const q of s.meta.groups[p])launch[q]=n;n++;if(profile){for(let i=0;i<before.length;i++)if(before[i]>=0&&s.board[i]<0)clearAt[i]=n;done.push(1-s.board.reduce((k,c)=>k+(c>=0),0)/total);}mark(n);}
 return {won:s.status==='Won',occ,launch,topAt,clearAt,reachAt,topsAt,launches:n,done};}
const toCols=(level,route)=>{let s=I.create(level,5);const out=[];for(const p of route){out.push(level.tray.findIndex((t,c)=>t[s.heads[c]]===p));s=I.step(s,{type:'tap',pod:p}).state;}return out;};

// calm playouts: uniform launches, half of them leaning to the launch with the most reachable work now; winning routes
function playouts(level,games,r,stopAt=8,stat=null){const wins=[];let uni=0,uniWins=0;
 for(let g=0;g<games&&wins.length<stopAt;g++){let s=I.create(level,5);const route=[],lean=g%2===1;let k=0;
  while(s.status==='Playing'&&k++<400){const mv=I.legal(s).filter(a=>a.type==='tap');if(!mv.length)break;let a;
   if(lean&&r()<.6){const t=R.reach(s).targets,work=p=>t.reduce((n,i)=>n+(s.board[i]===s.meta.colours[p]),0);a=mv.slice().sort((x,y)=>work(y.pod)-work(x.pod))[0];}else a=mv[Math.floor(r()*mv.length)];
   const x=I.step(s,a);if(!x.accepted)break;s=x.state;route.push(a.pod);}
  if(s.status==='Won')wins.push(route);if(!lean){uni++;if(s.status==='Won')uniWins++;}}
 if(stat){stat.calm=uni?uniWins/uni:null;stat.games=uni;}return wins;}
// how canonical a route is: at each launch, would "leftmost column", "rightmost column", round robin left→right or
// right→left have chosen the same column? the largest share of agreement (1 = a column sweep or a row sweep)
function canon(level,route){let s=I.create(level,5);const hit={colL:0,colR:0,rrL:0,rrR:0};let last=-1,n=0;
 for(const p of route){const tops=I.legal(s).filter(a=>a.type==='tap').map(a=>level.tray.findIndex((t,c)=>t[s.heads[c]]===a.pod)).filter(c=>c>=0).sort((a,b)=>a-b);const c=level.tray.findIndex((t,k)=>t[s.heads[k]]===p),C=level.tray.length;
  if(tops.length>1){n++;if(c===tops[0])hit.colL++;if(c===tops.at(-1))hit.colR++;const nx=[...Array(C).keys()].map(k=>(last+1+k)%C).find(k=>tops.includes(k)),pv=[...Array(C).keys()].map(k=>((last-1-k)%C+C)%C).find(k=>tops.includes(k));if(c===nx)hit.rrL++;if(c===pv)hit.rrR++;}
  last=c;s=I.step(s,{type:'tap',pod:p}).state;}
 const share=Object.fromEntries(Object.entries(hit).map(([k,v])=>[k,n?Math.round(v/n*100)/100:1]));return {...share,max:Math.max(...Object.values(share)),decisions:n};}

// how far "most work now" (and for Super Hard "release a place fastest") gets on the live timeline, tapping as soon as it
// can (measure.mjs greedy): the share of cells it clears, 1.5 when it wins; the largest over the variants
function greedyReach(level,cls){const {T}=E;let best=0;for(const k of cls==='Super Hard'?['most-now','fast-release']:['most-now']){const pol=STRATEGIES[k].make(1,E),t=new T.Timeline(R.create(level,5)),total=t.state.board.reduce((n,c)=>n+(c>=0),0);let g=0;
  while(g++<100000){if(t.state.status==='Won')break;const m=R.legal(t.state).filter(a=>!t.check(a));if(m.length){const a=pol(t,m);if(!a||t.check(a)){if(t.pending){t.advance(t.steps[0].at-t.now);continue;}break;}t.commit(a);continue;}if(!t.pending)break;t.advance(t.steps[0].at-t.now);}
  best=Math.max(best,t.state.status==='Won'?1.5:t.processedCells/total);}
 return best;}

// ---- 2. tuning on the route ----
// the score of a queue for the route: places held without work before each launch in the first 70 % of the route, near
// the target; a regular Normal keeps a holding pod in its first two rows
function occStat(occ){const k=Math.max(1,Math.round(occ.length*.7)),w=occ.slice(1,k);return w.length?w.reduce((a,b)=>a+b,0)/w.length:0;}
const score=(ctx,pods,tray,occ,target,needHold)=>-Math.abs(occStat(occ)-target)+(needHold&&!hasHold(ctx,pods,tray)?-1:0);
// §3 for a colour's pods after a count move: 1–60, max min(60, ceil(v/2)) for 20+, near the even split (±25 %, integer slack)
function countsOk(counts,v){if(counts.some(n=>n<1||n>60))return false;if(v>=20){const max=Math.min(60,Math.ceil(v/2));if(counts.some(n=>n>max))return false;}
 const mean=v/counts.length;return counts.every(n=>n>=Math.floor(mean*.75)&&n<=Math.ceil(mean*1.25));}
// Hard and Super Hard also push down how far the greedy strategy gets (greedyReach, weight gw): the route keeps winning
// while "most work now" runs into the places
function tune(ctx,base,cols,target,r,{iters=90,needHold=false,gw=0}={}){let pods=base.pods.map(p=>({...p})),tray=base.tray.map(t=>t.slice());
 const J=(P,T,occ)=>score(ctx,P,T,occ,target,needHold)-(gw?gw*greedyReach(mkLevel(ctx,P,T),ctx.cls):0);
 let rep=replayCols(mkLevel(ctx,pods,tray),cols);if(!rep.won)return null;let cur=J(pods,tray,rep.occ),best={pods,tray,occ:rep.occ,s:cur},moves={swapCol:0,swapRow:0,count:0,tried:0};
 for(let it=0;it<iters;it++){const P=pods.map(p=>({...p})),T=tray.map(t=>t.slice()),u=r();let kind;moves.tried++;
  if(u<.4){kind='swapCol';const c=Math.floor(r()*T.length);if(T[c].length<2)continue;const k=Math.floor(r()*(T[c].length-1));[T[c][k],T[c][k+1]]=[T[c][k+1],T[c][k]];}
  else if(u<.8){kind='swapRow';const a=Math.floor(r()*T.length),b=(a+1+Math.floor(r()*(T.length-1)))%T.length,k=Math.floor(r()*T[a].length),k2=Math.max(0,Math.min(T[b].length-1,k+Math.floor(r()*3)-1));if(T[b][k2]===undefined)continue;[T[a][k],T[b][k2]]=[T[b][k2],T[a][k]];}
  else{kind='count';const byC={};P.forEach((p,i)=>(byC[p.color]=byC[p.color]||[]).push(i));const cs=Object.keys(byC).filter(c=>byC[c].length>=2&&!ctx.accentSet.has(c));if(!cs.length)continue;const c=cs[Math.floor(r()*cs.length)],ix=byC[c].sort(()=>r()-.5),dlt=1+Math.floor(r()*3),v=ix.reduce((n,i)=>n+P[i].count,0);
   P[ix[0]].count-=dlt;P[ix[1]].count+=dlt;if(!countsOk(ix.map(i=>P[i].count),v))continue;}
  if(!T.some(t=>t.length&&ctx.frontier.has(P[t[0]].color)))continue;
  const rr=replayCols(mkLevel(ctx,P,T),cols);if(!rr.won)continue;const sc=J(P,T,rr.occ);
  if(sc>=cur-(r()<.15?.15:0)){pods=P;tray=T;cur=sc;moves[kind]++;if(sc>best.s)best={pods,tray,occ:rr.occ,s:sc};}}
 return {...best,occStat:occStat(best.occ),moves,greedyReach:gw?greedyReach(mkLevel(ctx,best.pods,best.tray),ctx.cls):null};}

// ---- 3. overlays that keep the route ----
// links (17 §7 dose, §8; the engine plays pairs of two columns at the same start depth only, rules.cjs validate): a
// pair (a, b) at one depth where b is on top when the route launches a launches together at a's step; Hard and Super
// Hard prefer a long gap (b launched much later on the route: "link the 4th 3 with the 2nd 2, so the player has to
// think") and a trap (a member whose material has no cell on the open front at the start); Normal a short gap. A pair is
// kept when the changed route still wins; failing that, when calm playouts still find a winning route (then that route
// is the level's route from here on: counted in relinked).
function addLinks(ctx,level,route,want,r){let lv=level,rt=route.slice(),relinked=0;const groups=[];
 for(let tries=0;groups.length*2<want&&tries<6*want+20;tries++){const pr=replayPods(lv,rt,{profile:true});if(!pr.won)return null;const opts=[];
  for(let i=0;i<rt.length;i++){const a=rt[i];if(lv.pods[a].group||lv.pods[a].mystery)continue;const ca=lv.tray.findIndex(t=>t.includes(a)),ka=lv.tray[ca].indexOf(a);
   for(const b of pr.topsAt[i]){if(b===a||lv.pods[b].group||lv.pods[b].mystery)continue;const cb=lv.tray.findIndex(t=>t.includes(b));if(cb===ca||lv.tray[cb].indexOf(b)!==ka)continue;
    const j=pr.launch[b];if(!(j>i))continue;const gap=j-i,trap=(ctx.minD[lv.pods[a].color]??0)>0||(ctx.minD[lv.pods[b].color]??0)>0;
    const sc=ctx.cls==='Normal'?-Math.abs(gap-2)*.6+(trap?.3:0):Math.min(gap,10)*.4+(trap?2:0)+(ka<=1?.5:0);opts.push({a,b,i,gap,trap,sc:sc+r()*1.2});}}
  // pairs at one depth whose second member is not on top yet when the first launches (the route must change)
  const late=[];lv.tray.forEach((t,c)=>t.forEach((a,k)=>{if(lv.pods[a].group||lv.pods[a].mystery)return;lv.tray.forEach((t2,c2)=>{const b=t2[k];if(c2<=c||b===undefined||lv.pods[b].group||lv.pods[b].mystery)return;
   if(opts.some(o=>(o.a===a&&o.b===b)||(o.a===b&&o.b===a)))return;const trap=(ctx.minD[lv.pods[a].color]??0)>0||(ctx.minD[lv.pods[b].color]??0)>0;late.push({a,b,trap,sc:(trap&&ctx.cls!=='Normal'?1:0)+r()});});}));
  opts.sort((x,y)=>y.sc-x.sc);late.sort((x,y)=>y.sc-x.sc);let ok=false;
  const tryPair=(o,keep)=>{const name=String.fromCharCode(65+groups.length%26),pods=lv.pods.map((p,k)=>k===o.a||k===o.b?{...p,group:name}:p),nl={...lv,pods},first=Math.min(o.a,o.b);
   if(keep){const nr=rt.filter(p=>p!==o.b&&p!==o.a).slice();const ia=rt.indexOf(o.a);nr.splice(rt.slice(0,ia).filter(p=>p!==o.b).length,0,first);if(replayPods(nl,nr).won){lv=nl;rt=nr;groups.push([o.a,o.b]);return true;}return false;}
   const w=playouts(nl,16,r,1);if(w.length){lv=nl;rt=w[0];groups.push([o.a,o.b]);relinked++;return true;}return false;};
  if(ctx.cls!=='Normal'){
   // Hard and Super Hard: of the pairs that keep a winning route, the one after which the greedy strategy gets least far
   // (a trap first while the level has none, §8)
   const hasTrap=groups.some(([a,b])=>(ctx.minD[lv.pods[a].color]??0)>0||(ctx.minD[lv.pods[b].color]??0)>0),save={lv,rt,rl:relinked,gl:groups.length};let bestO=null;
   for(const o of [...opts.slice(0,10).map(o=>({...o,keep:true})),...late.slice(0,5).map(o=>({...o,keep:false}))]){if(!tryPair(o,o.keep))continue;const gr=greedyReach(lv,ctx.cls)+(!hasTrap&&!o.trap?.6:0);
    if(!bestO||gr<bestO.gr)bestO={o,gr,lv,rt,rl:relinked};lv=save.lv;rt=save.rt;relinked=save.rl;groups.length=save.gl;}
   if(bestO){lv=bestO.lv;rt=bestO.rt;relinked=bestO.rl;groups.push([bestO.o.a,bestO.o.b]);ok=true;}}
  else{for(const o of opts.slice(0,8))if(tryPair(o,true)){ok=true;break;}
   if(!ok)for(const o of [...opts.slice(0,3),...late.slice(0,3)])if(tryPair(o,false)){ok=true;break;}}
  if(!ok)break;}
 return groups.length*2>=want?{level:lv,route:rt,groups,relinked}:null;}
// hidden pods (17 §8, additional): row 2 and deeper, never in a group; prefer pods that reveal a colour no other top shows
// and are launched within two launches of their reveal (they matter); Hard and Super Hard may take a run in one column
function addHidden(ctx,level,route,want,r){const pr=replayPods(level,route,{profile:true});const opts=[];
 level.tray.forEach((t,c)=>t.forEach((p,k)=>{if(k<1||level.pods[p].group)return;const rev=pr.topAt[c][k],j=pr.launch[p],fresh=!pr.topsAt[rev]?.some(q=>q!==p&&level.pods[q].color===level.pods[p].color);
  opts.push({p,c,k,sc:(fresh?1:0)+(j-rev<=2?1:0)+(rev<route.length/2?.5:0)+r()*.8});}));
 opts.sort((a,b)=>b.sc-a.sc);const pick=[];for(const o of opts){if(pick.length>=want)break;if(ctx.cls==='Normal'&&pick.some(x=>x.c===o.c&&Math.abs(x.k-o.k)===1))continue;pick.push(o);}
 if(pick.length<want)return null;const pods=level.pods.map((p,i)=>pick.some(o=>o.p===i)?{...p,mystery:'depth'}:p);return {level:{...level,pods},pods:pick.map(o=>o.p)};}
// hidden cells (17 §8): one contiguous chunk, off the start front, none an accent cube, grown from a seed the route
// clears in its middle, preferring cells the route clears between 30 % and 75 % of its launches
function addCells(ctx,level,route,want,r){const pr=replayPods(level,route,{profile:true}),{w}=level,n=level.w*level.h,K=pr.launches,g=level.rowsBottomFirst.join('');
 const avoid=new Set((ctx.recolor||[]).map(([x,y])=>(level.h-1-(y+2))*w+x+2));
 const ok=i=>L.CODES.includes(g[i])&&ctx.depth[i]>=1&&ctx.depth[i]<Infinity&&!avoid.has(i),nb=i=>{const x=i%w,o=[];if(i>=w)o.push(i-w);if(i+w<n)o.push(i+w);if(x>0)o.push(i-1);if(x<w-1)o.push(i+1);return o;};
 const mid=c=>Math.abs(pr.clearAt[c]/K-.5);const seeds=[];for(let i=0;i<n;i++)if(ok(i)&&pr.clearAt[i]>=0&&mid(i)<.15)seeds.push(i);if(!seeds.length)return null;
 for(let attempt=0;attempt<6;attempt++){const chunk=[seeds[Math.floor(r()*seeds.length)]],set=new Set(chunk);
  while(chunk.length<want){const front=[...new Set(chunk.flatMap(nb))].filter(j=>ok(j)&&!set.has(j));if(!front.length)break;let best=null,bs=-1e9;
   for(const j of front){const sc=nb(j).filter(q=>set.has(q)).length-2*Math.max(0,mid(j)-.25)+r()*.4;if(sc>bs){bs=sc;best=j;}}chunk.push(best);set.add(best);}
  if(chunk.length<want)continue;const lv={...level,hiddenCells:chunk.sort((a,b)=>a-b)};if(LV.cellsOf&&replayPods(lv,route).won)return {level:lv,cells:chunk};}
 return null;}
// the latch (17 §8): a column at the class depth whose pod behind it the route launches late, its key in a picture hole the
// route reaches a few launches before it needs that pod (the latch waits closed on top meanwhile: LATCH_WAIT launches)
function addLatch(ctx,level,route,want,r){const holes=LV.keyHoles(ctx.board);if(holes.length<want)return {error:holes.length?'ключевых ям меньше засовов':'нет пустой клетки для ключа (яма) — засов не ставится'};
 const pr=replayPods(level,route,{profile:true,holes}),[lo,hi]=M.LATCH_DEPTH[ctx.cls],band=LV.LATCH_WAIT[ctx.cls],K=pr.launches,opts=[];
 level.tray.forEach((t,c)=>{for(let d=lo;d<=hi;d++){const behind=t.length-d;if(behind<M.LATCH_BEHIND.want[0]||behind>M.LATCH_BEHIND.max)continue;if(t.slice(d).some(p=>level.pods[p].mystery))continue;
  const top=pr.topAt[c][d],J=pr.launch[t[d]];holes.forEach((h,k)=>{const reach=pr.reachAt[k];if(!(reach<=J)||!(pr.done[reach]<=.65))return;const wait=reach-top,slack=J-reach;
   const sc=(wait>=band[0]&&wait<=band[1]?2:-Math.abs(wait-(band[0]+band[1])/2)*.5)+(slack<=3?1:-.3*slack)+J/K+r()*.5;opts.push({column:c,depth:d,key:h.key,k,sc});});}});
 opts.sort((a,b)=>b.sc-a.sc);const out=[];
 for(const o of opts){if(out.length>=want)break;if(out.some(x=>x.column===o.column||x.key===o.key))continue;const ls=[...out,{column:o.column,depth:o.depth,key:o.key}].sort((a,b)=>a.column-b.column),lv={...level,latches:ls};
  if(R.latchKeysBlocked(lv).length)continue;if(!replayPods(lv,route).won)continue;out.push({column:o.column,depth:o.depth,key:o.key});}
 if(out.length<want)return {error:'засов: ни одной пары (колонка, ключ), при которой маршрут остаётся выигрышным'};
 return {level:{...level,latches:out.sort((a,b)=>a.column-b.column)}};}

// ---- 3b. Hard and Super Hard: the greedy strategies must lose (§5). Moves that keep the route (a column route: a pair
// launches on its first member's column) and keep a pair at one depth, accepted when fewer greedy variants win ----
// (the score also counts the first launches after which "most work now" wins: the opening test of §5 at depth 1)
async function breakGreedy(ctx,level,route,r,iters=40){const cols=toCols(level,route),badG=g=>Object.entries(g).filter(([k,v])=>v&&(ctx.cls==='Super Hard'||k.startsWith('most-now'))).length;
 const judge=async lv=>{const g=await M.greedy(lv),b=badG(g);if(b)return {g,b:b+10};const o=await M.openingTest(lv,ctx.cls,{depth:1});return {g,b:o.first.wins};};
 let lv=level,{g,b:cur}=await judge(lv),moves=0;const need=()=>cur===0;if(need())return {level:lv,route,greedy:g,moves};
 for(let it=0;it<iters&&!need();it++){const T=lv.tray.map(t=>t.slice()),P=lv.pods.map(p=>({...p})),u=r();
  if(u<.45){const c=Math.floor(r()*T.length);if(T[c].length<2)continue;const k=Math.floor(r()*(T[c].length-1));if(P[T[c][k]].group||P[T[c][k+1]].group)continue;[T[c][k],T[c][k+1]]=[T[c][k+1],T[c][k]];}
  else if(u<.85){const a=Math.floor(r()*T.length),b=(a+1+Math.floor(r()*(T.length-1)))%T.length,k=Math.floor(r()*T[a].length),k2=P[T[a][k]].group?k:Math.max(0,Math.min(T[b].length-1,k+Math.floor(r()*3)-1));if(T[b][k2]===undefined||(P[T[b][k2]].group&&k2!==k))continue;[T[a][k],T[b][k2]]=[T[b][k2],T[a][k]];}
  else{const byC={};P.forEach((p,i)=>(byC[p.color]=byC[p.color]||[]).push(i));const cs=Object.keys(byC).filter(c=>byC[c].length>=2&&!ctx.accentSet.has(c));if(!cs.length)continue;const c=cs[Math.floor(r()*cs.length)],ix=byC[c].sort(()=>r()-.5),d=1+Math.floor(r()*3),v=ix.reduce((n,i)=>n+P[i].count,0);P[ix[0]].count-=d;P[ix[1]].count+=d;if(!countsOk(ix.map(i=>P[i].count),v))continue;}
  if(P.some((p,i)=>p.group&&P.some((q,j)=>j!==i&&q.group===p.group&&(T.findIndex(t=>t.includes(i))===T.findIndex(t=>t.includes(j))||T.find(t=>t.includes(i)).indexOf(i)!==T.find(t=>t.includes(j)).indexOf(j)))))continue;
  if(!T.some(t=>t.length&&ctx.frontier.has(P[t[0]].color))||T.some(t=>t.length&&P[t[0]].mystery))continue;
  const nl={...lv,pods:P,tray:T};if(lv.latches&&R.validate(nl).length)continue;if(!replayCols(nl,cols).won)continue;const {g:ng,b:nb}=await judge(nl);
  if(nb<=cur){lv=nl;g=ng;cur=nb;moves++;}}
 if(!need())return null;let s=I.create(lv,5);const rt=[];for(const c of cols){if(s.status!=='Playing')break;const p=lv.tray[c][s.heads[c]];rt.push(Math.min(...s.meta.groups[p]));s=I.step(s,{type:'tap',pod:p}).state;}
 return {level:lv,route:rt,greedy:g,moves};}

// ---- 4. the placer's filters of the slot (levels.mjs place: greedyOk, mechOk, fitOk, hold, calm, trap links) ----
export function placerFilters(e,level,cls,slot,pic){const k=e.checks||{},why=[];
 if(e.random.cls!==cls)why.push(`класс ${e.random.cls}`);
 if(!(k.forced<=.35))why.push('вынужденных > 35 %');
 if(cls!=='Normal'&&!(k.opening&&k.opening.ok))why.push('дебют + цвета');
 if(k.latch&&!k.latch.ok)why.push('засов');
 const f=fitness(L.overlaid(pic,null,null)),weak=f.hiddenColours<=1||f.visible>=6,types=(level.recipe||'none')==='none'?0:level.recipe.split('+').length;
 if(cls!=='Normal'&&f.hiddenColours<3)why.push('картинка слабая для Hard');if(cls==='Normal'&&weak&&!types)why.push('слабая картинка без допа');
 if(slot==='regular'&&!k.hold)why.push('нет пода, держащего место (§6)');
 const cf=M.calmFails(e.random.cls,k.calm,slot==='regular');if(cf)why.push(cf);
 if(cls!=='Normal'&&k.traps&&k.traps.links&&!k.traps.traps)why.push('нет связки-ловушки');
 return why;}
// the zone of the slot (levels.mjs place's preference): a regular Normal 50–75 % (it aims at 55 %), the first Normal after
// a Hard 75–95 %, Hard 20–50 % (preferred 25–40 %), Super Hard 5–20 %
export const zoneOf=(cls,slot)=>cls==='Normal'?(slot==='after-hard'?BAND['after-hard']:BAND.Normal):BAND[cls];
export const inZone=(share,cls,slot)=>{const [a,b]=zoneOf(cls,slot);return share>=a&&share<=b;};

// ---- the search ----
export async function routeFirst(picId,cls,recipe,{slot='regular',accentMode='auto',want=2,minutes=20,seed=1,log=()=>{}}={}){const t0=Date.now(),stats={bases:0,routes:0,solverRoutes:0,trivial:0,tuned:0,overlaid:0,screened:0,evaluated:0,accepted:0,reasons:{}},no=k=>{stats.reasons[k]=(stats.reasons[k]||0)+1;};
 const band=slot==='after-hard'&&cls==='Normal'?BAND['after-hard']:BAND[cls];let target=TARGET0[cls]+(slot==='after-hard'?-.4:0);const accepted=[],passedOut=[],shares=[];
 for(let k=0;(Date.now()-t0)<minutes*60000&&accepted.length<want;k++){stats.bases++;const r=rnd(seed*1000003+k*7919+CLASSES.indexOf(cls)*31);
  const useAcc=accentMode==='on'||(accentMode==='auto'&&(cls!=='Normal'||k%3===1));let ctx;try{ctx=context(picId,cls,recipe,{accents:useAcc,seed:seed*31+k});}catch(err){return {error:err.message,stats};}
  const colsOpt=L.columnsFor(ctx.m.N),cols=colsOpt[k%colsOpt.length],base=deal(ctx,cols,r),needHold=cls==='Normal'&&slot==='regular';
  if(needHold&&!hasHold(ctx,base.pods,base.tray)){const deep=[];base.tray.forEach((t,c)=>t.forEach((p,row)=>{if(row>=2&&!ctx.frontier.has(base.pods[p].color))deep.push([c,row]);}));if(deep.length){const [dc,dr]=deep[Math.floor(r()*deep.length)],c=Math.floor(r()*cols);[base.tray[c][1],base.tray[dc][dr]]=[base.tray[dc][dr],base.tray[c][1]];}}
  // a winning route: calm playouts first (the least canonical one), the solver when none wins
  const lv0=mkLevel(ctx,base.pods,base.tray);const ps={};let wins=playouts(lv0,30,r,8,ps);if(!wins.length){const sol=await M.solve(lv0,40000);if(sol.win===true){wins=[sol.route.map(a=>a.pod)];stats.solverRoutes++;}}
  if(!wins.length){no('нет выигрышного маршрута у случайной раскладки');continue;}stats.routes++;
  const ranked=wins.map(rt=>({rt,c:canon(lv0,rt)})).sort((a,b)=>a.c.max-b.c.max),pick=ranked[0];if(pick.c.max>(cls==='Normal'?.85:.75)){stats.trivial++;no('маршрут канонический (колонками или рядами)');continue;}
  const cs=toCols(lv0,pick.rt),tn=tune(ctx,base,cs,target,r,{needHold});if(!tn){no('настройка: маршрут не выигрывает');continue;}stats.tuned++;
  // overlays by the recipe's doses (levels.mjs doses: the lead its dose place, an additional one its minimum)
  let lv=mkLevel(ctx,tn.pods,tn.tray);let route=(()=>{let s=I.create(lv,5);const out=[];for(const c of cs){const p=lv.tray[c][s.heads[c]];out.push(p);s=I.step(s,{type:'tap',pod:p}).state;}return out;})();
  const pos=recipe==='none'?null:LV.DOSE_CYCLE[k%LV.DOSE_CYCLE.length],dz=LV.doses(recipe,cls,lv.pods.length,false,ctx.m.N);if(dz.error){no('дозы: '+dz.error);continue;}
  const lead=ctx.types[0];
  let fail=null,groups=[],hid=[],cells=null,relinked=0;
  for(const m of ctx.types){if(fail)break;
   // the lead's dose place of §7 (the cycle) on Normal; Hard and Super Hard take the bottom of the range: pairs make a level
   // hard by tempo (two places at once), the route-first Hard is meant to be hard by choice (the calm test)
   if(m==='links'){let w=lead==='links'&&pos!==null&&cls==='Normal'?dz.links[0]+2*Math.round(pos*(dz.links[1]-dz.links[0])/2):dz.links[0];const x=addLinks(ctx,lv,route,w,r);if(!x){fail='связки: маршрут не держит дозу';break;}lv=x.level;route=x.route;groups=x.groups;relinked=x.relinked;}
   else if(m==='hidden'){const w=Math.min(6,dz.hidden[0]);const x=addHidden(ctx,lv,route,w,r);if(!x){fail='скрытые: нет места';break;}lv=x.level;hid=x.pods;}
   else if(m==='cells'){const [a,b]=dz.cells,w=lead==='cells'&&pos!==null?a+Math.round(pos*(b-a)):a;const x=addCells(ctx,lv,route,w,r);if(!x){fail='скрытые клетки: нет куска';break;}lv=x.level;cells=x.cells;}
   else if(m==='latch'){const [a,b]=dz.latch,w=lead==='latch'&&pos!==null?a+Math.round(pos*(b-a)):a;const x=addLatch(ctx,lv,route,w,r);if(x.error){fail=x.error;break;}lv=x.level;}}
  if(fail){no(fail);continue;}stats.overlaid++;let broke=0;
  if(cls!=='Normal'){const b=await breakGreedy(ctx,lv,route,r);if(!b){no('жадная стратегия выигрывает и после подстройки');log(`#${k} жадная выигрывает и после подстройки`);continue;}lv=b.level;route=b.route;broke=b.moves;}
  // quick screen: 20 random games of the §5 policy; the occupancy target follows the measured share
  const q=await M.quickShare(lv,20,11);stats.screened++;shares.push(q);const before=target;
  // Hard and Super Hard steer by the calm share too (40 calm games): calm too high → a higher target (less slack on the
  // route), random too low with a calm share in range → a lower one
  const c40=cls!=='Normal'?(await M.calmTest(lv,{first:40,max:40})).share:null,calmLim=cls==='Hard'?.5:.25;
  if(c40!==null){if(c40>calmLim+.05)target=Math.min(4,target+.3);else if(q<band[0])target=Math.max(.2,target-.3);else if(q>band[1])target=Math.min(4,target+.3);}
  else if(q>band[1])target=Math.min(4,target+.3);else if(q<band[0])target=Math.max(.2,target-.3);
  log(`#${k} cols ${cols} canon ${pick.c.max} occ ${tn.occStat.toFixed(2)}${tn.greedyReach!=null?' жадная '+tn.greedyReach.toFixed(2):''} (цель ${before.toFixed(2)}) быстрая ${q}${c40!==null?' спокойно '+c40:''}${ctx.accents?' акценты':''}`);
  if(q<band[0]-.15||q>band[1]+.15){no('быстрый отсев: не тот класс');continue;}
  // cheap pre-screens of what evaluate refuses most (§5): forced decisions; Hard and Super Hard also the calm share
  // (40 calm games) and the whole opening test
  {const f=await M.forcedShare(lv);if(f>.35){no('отсев: вынужденных > 35 %');log(`   отсев: вынужденных ${f}`);continue;}
   if(cls!=='Normal'){if(c40>calmLim+.15){no('отсев: спокойная игра выше порога');log(`   отсев: спокойно ${c40}`);continue;}
    const o=await M.openingTest(lv,cls);if(!o.ok){no('отсев: дебют + цвета');log(`   отсев: дебют ${JSON.stringify(o.first)} ${o.three.share}`);continue;}}}
  stats.evaluated++;const te=Date.now(),e=await LV.evaluate(lv,{m:ctx.m,cls,intro:false,fixed:null,tick:()=>{}});
  if(e.reason){no('evaluate: '+e.reason);log(`   evaluate: ${e.reason} (${((Date.now()-te)/1000).toFixed(1)} с)`);continue;}
  const why=placerFilters(e,lv,cls,slot,ctx.pic);if(why.length){no('расстановка: '+why[0]);log(`   отказ расстановки: ${why.join('; ')}`);continue;}
  const acc=ctx.accents?await LV.accentCheck(lv,ctx.pic,ctx.accents,e.checks.pressure):null;if(acc)e.checks.accents=acc;
  const zone=inZone(e.random.share,cls,slot),at=(Date.now()-t0)/1000;stats.passed=(stats.passed||0)+1;
  if(!zone){no('вне зоны позиции (проходит фильтры)');log(`   проходит, но вне зоны: ${e.random.share} за ${at.toFixed(0)} с`);passedOut.push({id:`${picId}-route-${cls[0]}${recipe}-${k}`,at,attempt:k,pattern:{canon:pick.c,occStat:Math.round(tn.occStat*100)/100,target:before},level:lv,m:ctx.m,e,accents:ctx.accents});continue;}
  stats.accepted++;log(`   ПРИНЯТ: ${e.random.share} за ${at.toFixed(0)} с`);
  accepted.push({id:`${picId}-route-${cls[0]}${recipe}-${k}`,at,attempt:k,pattern:{canon:pick.c,occStat:Math.round(tn.occStat*100)/100,target:before,moves:tn.moves,route:pick.rt.length,groups,relinked,broke,hidden:hid,cells:cells?cells.length:0},level:lv,m:ctx.m,e,accents:ctx.accents});}
 return {accepted,passedOut,stats,shares,seconds:(Date.now()-t0)/1000};}

// the placed-level shape (levels.mjs place) of an accepted candidate, for the pilot's --preview
export function asPlaced(x,n,cls,picId,intro=null){const {level:lv,e,m}=x;
 return {level:n,cls,intro,picture:picId,candidate:x.id,recipe:lv.recipe,N:m.N,W:m.N,colours:m.colours,P:lv.pods.length,columns:lv.tray.length,linked:lv.pods.filter(p=>p.group).length,hidden:lv.pods.filter(p=>p.mystery).length,
  ...(lv.hiddenCells?{cells:M.cellsHiddenAtStart(lv)}:{}),...(lv.latches?{latches:lv.latches.length}:{}),random:e.random,solution:e.solution,fairness:e.fairness,strategies:e.strategies,rhythm:e.rhythm,dosePos:null,checks:e.checks,engine:'tools/rounds/engine.mjs (route-first prototype)',pattern:x.pattern,definition:{...lv,id:n,difficulty:cls}};}

if(ARGV[0]&&/^P\d+$/.test(ARGV[0])){const pic=ARGV[0],cls=opt('--cls','Normal'),recipe=opt('--recipe','links+hidden'),label=opt('--label',pic),out=path.resolve(ROOT,opt('--out','content/work/proto-route/out'));
 const res=await routeFirst(pic,cls,recipe,{slot:opt('--slot','regular'),accentMode:opt('--accents','auto'),want:+opt('--want',2),minutes:+opt('--minutes',20),seed:+opt('--seed',1),log:s=>console.log(label,s)});
 fs.mkdirSync(out,{recursive:true});fs.writeFileSync(path.join(out,label+'.json'),JSON.stringify({picture:pic,cls,recipe,slot:opt('--slot','regular'),seconds:res.seconds,stats:res.stats,shares:res.shares,error:res.error||null,accepted:res.accepted.map(x=>({...asPlaced(x,0,cls,pic),at:x.at})),passedOut:res.passedOut.map(x=>({...asPlaced(x,0,cls,pic),at:x.at}))},null,1));
 console.log(label,'итог',JSON.stringify({seconds:Math.round(res.seconds),accepted:res.accepted.map(x=>[x.e.random.share,Math.round(x.at)]),stats:res.stats}));}

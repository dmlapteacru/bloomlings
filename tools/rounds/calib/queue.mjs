// Calibration of the queue (the owner, 2026-11-01: «расставлять поды по формуле, а не случайно, попадать сразу в нужный
// диапазон сложности, а потом добавлять механики и подстройки»). README.md here.
//
// This module: the picture's features (computed once) and a queue built from EXPLICIT parameters instead of the class:
//   t      blend between the access order (0: shallow colours first, easy) and its reverse (1: deep colours first, hard),
//          the same blend levels.mjs candidate draws at random per class (opts.t / opts.tStrong)
//   sigma  the noise on the access key (candidate: Normal .06, Hard .16, Super Hard .26)
//   cols   columns (17 §3 by N), P the pod count inside the §3 budget by W, hold the §6 holding pod of a regular Normal
//   links  linked pods (pairs, an even count) and style: 'similar' (today's Normal: pairs of near keys) or 'trap'
//          (today's Hard: pairs of far keys, a member hidden at the start first, 17 §8 trap link)
//   hidden hidden pods (rows 2+, never in a group), style 'spread' or 'run' (consecutive in one column)
//   cells  hidden cells as one compact chunk (17 §8; never with hidden pods), as a share of N
//   latch  latches with their acorn key (levels.mjs pickLatches), accents (levels.mjs pickAccents, 17 §6)
// Nothing here writes content; it reads pictures through lib.mjs (ROUNDS_DIR = a private copy).
import * as L from '../lib.mjs';
import * as LV from '../levels.mjs';
import {fitness} from '../fitness.mjs';
import {rulesSync} from '../engine.mjs';

export const rnd=seed=>{let s=seed>>>0||1;return()=>{s^=s<<13;s^=s>>>17;s^=s<<5;return(s>>>0)/4294967296;};};

// access depth of every cell (levels.mjs depths for a board without a bridge): colour cells to clear before it
export function access(board){const {w,h,rowsBottomFirst:rows,entries}=board,g=rows.join(''),d=new Float64Array(w*h).fill(Infinity),dq=[];for(const e of entries){d[e]=0;dq.push(e);}
 while(dq.length){const c=dq.shift(),x=c%w,y=Math.floor(c/w);for(const [dx,dy] of [[1,0],[-1,0],[0,1],[0,-1]]){const u=x+dx,v=y+dy;if(u<0||v<0||u>=w||v>=h)continue;const n=v*w+u;if(g[n]==='#'||g[n]==='b')continue;
  const cost=L.CODES.includes(g[c])?1:0,nd=d[c]+cost;if(nd<d[n]){d[n]=nd;cost?dq.push(n):dq.unshift(n);}}}
 return {d,g};}

const mean=a=>a.length?a.reduce((x,y)=>x+y,0)/a.length:0,sd=a=>{const m=mean(a);return Math.sqrt(mean(a.map(x=>(x-m)**2)));};

// ---- 1. features of a picture (once) ----
export function pictureFeatures(pic,recolor=null){const board=L.boardOf(pic,null,recolor),{d,g}=access(board),m=L.measure(L.overlaid(pic,null,recolor)),f=fitness(L.overlaid(pic,null,recolor));
 const byC={};for(let i=0;i<g.length;i++)if(L.CODES.includes(g[i])&&d[i]<Infinity)(byC[g[i]]=byC[g[i]]||[]).push(d[i]);
 const maxD=Math.max(1,...Object.values(byC).flat()),vols=Object.values(m.volumes),first=Object.values(byC).map(a=>Math.min(...a)/maxD),meanC=Object.values(byC).map(a=>mean(a)/maxD);
 const cells=Object.values(byC).flat();let holes=0;try{holes=LV.keyHoles(board).length;}catch{}
 return {N:m.N,logN:Math.log(m.N),colours:m.colours,w:pic.rows[0].length,h:pic.rows.length,volCV:sd(vols)/mean(vols),maxShare:Math.max(...vols)/m.N,smallColours:vols.filter(v=>v<20).length,
  visible:f.visible,hiddenColours:f.hiddenColours,hiddenShare:f.hiddenColours/m.colours,openShare:f.openShare,shallowShare:f.shallowShare,layers:maxD,deepWork:f.deepWork,
  meanFirst:mean(first),sdFirst:sd(first),meanDepth:mean(cells)/maxD,sdColourDepth:sd(meanC),holes,weak:f.hiddenColours<=1||f.visible>=6?1:0};}

// ---- 2. pods: the volumes split into a target count inside the §3 budget ----
// the §3 minimum per colour (levels.mjs splitPods), then one more pod to the colour with the largest average pod while
// its pods stay ≥ 6 (17 §3: no splitting a big colour into many 1–5 pods), up to `target`
export function podsFor(volumes,W,target){const k={};for(const [c,v] of Object.entries(volumes)){if(v<20)k[c]=1;else{const max=Math.min(60,Math.ceil(v/2));k[c]=Math.max(2,Math.ceil(v/max),v>=60?3:0);}}
 const P=()=>Object.values(k).reduce((a,b)=>a+b,0);
 while(P()<target){const c=Object.keys(k).filter(c=>volumes[c]/(k[c]+1)>=6&&!(volumes[c]<20&&k[c]>=2)).sort((a,b)=>volumes[b]/k[b]-volumes[a]/k[a])[0];if(!c)break;k[c]++;}
 const even=(v,n)=>Array.from({length:n},(_,i)=>Math.floor(v/n)+(i<v%n?1:0));
 return Object.entries(k).flatMap(([c,n])=>even(volumes[c],n).map(x=>({color:c,count:x})));}
// the P of a budget level: 0 = the budget's low end, .5 the middle, 1 the high end (clipped by what the colours allow)
export function podTarget(W,level){const [lo,hi]=L.budget(W);return Math.round(lo+level*(hi-lo));}

// ---- 3. the context of one picture (optionally with accents painted, 17 §6) ----
export function context(pic,{accents=false,seed=1}={}){let recolor=null,acc=null;
 if(accents){const ar=rnd(seed^0x2545f491),m0=L.measure(pic),room=(m0.N<400?6:10)-m0.colours;const a=room>0?LV.pickAccents(pic,null,ar,{materials:1}):null;if(a){recolor=a.recolor;acc=a.accents;}}
 const m=L.measure(L.overlaid(pic,null,recolor)),board=L.boardOf(pic,null,recolor),{d,g}=access(board),minD={};
 for(let i=0;i<g.length;i++)if(L.CODES.includes(g[i]))minD[g[i]]=Math.min(minD[g[i]]??Infinity,d[i]);
 const byColour={};for(let i=0;i<g.length;i++)if(L.CODES.includes(g[i]))(byColour[g[i]]=byColour[g[i]]||[]).push(d[i]);for(const a of Object.values(byColour))a.sort((x,y)=>x-y);
 const maxD=Math.max(1,...Object.values(byColour).flat().filter(x=>x<Infinity));
 return {pic,m,board,d,g,minD,byColour,maxD,recolor,accents:acc,accentSet:new Set((acc||[]).map(a=>a.code)),frontier:new Set(Object.keys(minD).filter(c=>minD[c]===0))};}

// ---- 4. a queue from explicit parameters ----
// params: {t, sigma, cols, P (target pod count), hold, links, linkStyle, hidden, hiddenStyle, cells (share of N), latch, seed}
export function build(ctx,p){const r=rnd(p.seed||1),N=ctx.m.N;let pods=podsFor(ctx.m.volumes,N,p.P);
 if(ctx.accents){const ps=LV.accentPods(pods,ctx.accents,rnd((p.seed||1)^0x51));if(ps.length<=L.budget(N)[1])pods=ps;}
 // the access key of each pod: the mean depth of its slice of its colour's cells (sorted shallow first), / the deepest
 const used={};for(const q of pods){const ds=ctx.byColour[q.color],k=used[q.color]||0,sl=ds.slice(k,k+q.count);used[q.color]=k+q.count;q.key=Math.min(1,mean(sl.filter(x=>x<Infinity))/ctx.maxD);}
 for(const q of pods){const z=Math.sqrt(-2*Math.log(r()+1e-9))*Math.cos(2*Math.PI*r());q.order=(1-p.t)*(q.key+p.sigma*z)+p.t*(1-q.key);}
 const cols=p.cols,ord=pods.map((_,i)=>i).sort((a,b)=>pods[a].order-pods[b].order),tray=Array.from({length:cols},()=>[]);
 for(let i=0;i<ord.length;i+=cols){const row=ord.slice(i,i+cols),cs=[...Array(cols).keys()].sort(()=>r()-.5).sort((a,b)=>tray[a].length-tray[b].length);row.forEach((q,j)=>tray[cs[j]].push(q));}
 // accents (17 §6): accent pods in the second row to the middle
 if(ctx.accentSet.size){const rows=Math.max(...tray.map(t=>t.length)),lo=1,hi=Math.max(1,Math.floor((rows-1)/2));
  for(const i of pods.map((q,i)=>i).filter(i=>ctx.accentSet.has(pods[i].color))){const c=tray.findIndex(t=>t.includes(i)),k=tray[c].indexOf(i);if(k>=lo&&k<=hi)continue;
   const to=[];tray.forEach((t,c2)=>t.forEach((q,k2)=>{if(k2>=lo&&k2<=hi&&!ctx.accentSet.has(pods[q].color))to.push([c2,k2]);}));if(!to.length)break;const [c2,k2]=to[Math.floor(r()*to.length)];[tray[c][k],tray[c2][k2]]=[tray[c2][k2],tray[c][k]];}}
 // §6: a launch with reachable work at the start
 if(!tray.some(t=>t.length&&ctx.frontier.has(pods[t[0]].color))){const best=ord.filter(i=>ctx.frontier.has(pods[i].color)).sort((a,b)=>pods[a].key-pods[b].key)[0];
  if(best!==undefined){const c=Math.floor(r()*cols),bc=tray.findIndex(t=>t.includes(best)),bi=tray[bc].indexOf(best);[tray[c][0],tray[bc][bi]]=[tray[bc][bi],tray[c][0]];}}
 // §6: a regular Normal's holding pod (a deep colour in the first two rows)
 if(p.hold&&!tray.some(t=>t.slice(0,2).some(q=>!ctx.frontier.has(pods[q].color)))){const deep=[];tray.forEach((t,c)=>t.forEach((q,row)=>{if(row>=2&&!ctx.frontier.has(pods[q].color))deep.push([c,row]);}));
  deep.sort((a,b)=>pods[tray[a[0]][a[1]]].key-pods[tray[b[0]][b[1]]].key);const cs=[...Array(cols).keys()].filter(c=>tray[c].length>=2);
  if(deep.length&&cs.length){const [dc,dr]=deep[0],c=cs[Math.floor(r()*cs.length)];[tray[c][1],tray[dc][dr]]=[tray[dc][dr],tray[c][1]];}}
 // links: pairs of two columns at one start depth (the engine's rule), never a hidden pod
 const trapOf=i=>(ctx.minD[pods[i].color]??0)>0;let pairs=[];
 if(p.links>0){const want=p.links-(p.links%2),opts=[];for(let c=0;c<cols;c++)for(let c2=c+1;c2<cols;c2++)for(let row=0;row<Math.min(tray[c].length,tray[c2].length);row++){const a=tray[c][row],b=tray[c2][row];
   const dk=Math.abs(pods[a].key-pods[b].key),trap=trapOf(a)||trapOf(b),adj=c2===c+1?0:.05;
   opts.push({a,b,row,trap,sc:p.linkStyle==='trap'?-(dk+(trap?.5:0)+(row<=1?.15:0))+adj+(r()-.5)*.2:dk+adj+(r()-.5)*.2});}
  opts.sort((x,y)=>x.sc-y.sc);const used=new Set();
  // a trap style puts a trap pair first (17 §8: a Hard with links has at least one)
  if(p.linkStyle==='trap'){const t0=opts.findIndex(o=>o.trap);if(t0>0)opts.unshift(opts.splice(t0,1)[0]);}
  for(const o of opts){if(pairs.length*2>=want)break;if(used.has(o.a)||used.has(o.b))continue;used.add(o.a);used.add(o.b);pairs.push(o);}
  pairs.forEach((o,i)=>{const n=String.fromCharCode(65+i%26)+(i>=26?String(Math.floor(i/26)):'');pods[o.a].group=n;pods[o.b].group=n;});}
 // hidden pods: rows 2+, never in a group; a run stands consecutive in one column
 if(p.hidden>0){const want=Math.min(6,p.hidden);let pick=[];
  if(p.hiddenStyle==='run'){const cs=[...Array(cols).keys()].sort(()=>r()-.5);for(const c of cs){const free=tray[c].map((q,row)=>[q,row]).filter(([q,row])=>row>=1&&!pods[q].group);
    for(let s=0;s+want<=free.length&&!pick.length;s++){const run=free.slice(s,s+want);if(run.every(([,row],j)=>row===run[0][1]+j))pick=run.map(([q])=>q);}if(pick.length)break;}}
  if(!pick.length){const opts=tray.flatMap((t,c)=>t.map((q,row)=>({q,row,c}))).filter(o=>o.row>=1&&!pods[o.q].group).sort(()=>r()-.5);
   for(const o of opts){if(pick.length>=want)break;if(pick.some(q=>tray[o.c].includes(q))&&opts.filter(x=>!pick.includes(x.q)).some(x=>!pick.some(q=>tray[x.c].includes(q))))continue;pick.push(o.q);}}
  pick.forEach(q=>pods[q].mystery='depth');}
 // hidden cells: one compact chunk round a seed of middle depth, off the start front, no accent cube
 let hiddenCells=null;
 if(p.cells>0){const want=Math.max(1,Math.round(p.cells*N)),{w}=ctx.board,n=ctx.g.length,avoid=new Set((ctx.recolor||[]).map(([x,y])=>(ctx.board.h-1-(y+2))*w+x+2));
  const ok=i=>L.CODES.includes(ctx.g[i])&&ctx.d[i]>=1&&ctx.d[i]<Infinity&&!avoid.has(i),nb=i=>{const x=i%w,o=[];if(i>=w)o.push(i-w);if(i+w<n)o.push(i+w);if(x>0)o.push(i-1);if(x<w-1)o.push(i+1);return o;};
  const seeds=[];for(let i=0;i<n;i++)if(ok(i)&&ctx.d[i]>=.3*ctx.maxD&&ctx.d[i]<=.7*ctx.maxD)seeds.push(i);
  for(let a=0;a<6&&!hiddenCells&&seeds.length;a++){const chunk=[seeds[Math.floor(r()*seeds.length)]],set=new Set(chunk);
   while(chunk.length<want){const front=[...new Set(chunk.flatMap(nb))].filter(j=>ok(j)&&!set.has(j));if(!front.length)break;let best=null,bs=-1;for(const j of front){const sc=nb(j).filter(q=>set.has(q)).length+r()*.5;if(sc>bs){bs=sc;best=j;}}chunk.push(best);set.add(best);}
   if(chunk.length>=want)hiddenCells=chunk.sort((x,y)=>x-y);}
  if(!hiddenCells)return {error:'скрытые клетки: нет куска нужного размера'};}
 const level={id:1,title:ctx.pic.title,subject:ctx.pic.id,w:ctx.board.w,h:ctx.board.h,rowsBottomFirst:ctx.board.rowsBottomFirst,entries:ctx.board.entries,capacity:5,pods:pods.map(({key,order,...q})=>q),tray,recipe:recipeOf(p),
  ...(ctx.recolor?{recolor:ctx.recolor}:{}),...(hiddenCells?{hiddenCells}:{})};
 if(p.latch>0){const lp=LV.pickLatches(ctx.board,tray,level.pods,p.latch,p.latchClass||'Hard',r);if(lp.error)return {error:lp.error};level.latches=lp.latches;}
 const ve=rulesSync().validate(level);if(ve&&ve.length)return {error:'движок: '+ve[0]};
 return {level,keys:pods.map(q=>q.key),pairs:pairs.map(o=>({row:o.row,trap:o.trap}))};}
export function recipeOf(p){const t=[];if(p.links>0)t.push('links');if(p.hidden>0)t.push('hidden');if(p.cells>0)t.push('cells');if(p.latch>0)t.push('latch');return t.length?t.join('+'):'none';}

// ---- 5. structural features of a built queue (no simulation) ----
// rankCorr: Spearman correlation of the pods' tray row with their access key (+1 = shallow on top, the easy order);
// deepTop: pods in the first two rows whose colour has no cell on the open front, per column; frontTop: top pods with work
export function queueFeatures(ctx,built){const {level,keys}=built,rowOf=new Array(level.pods.length);level.tray.forEach(t=>t.forEach((q,k)=>rowOf[q]=k));
 const rank=a=>{const o=a.map((v,i)=>[v,i]).sort((x,y)=>x[0]-y[0]),rk=new Array(a.length);let i=0;while(i<o.length){let j=i;while(j+1<o.length&&o[j+1][0]===o[i][0])j++;for(let k=i;k<=j;k++)rk[o[k][1]]=(i+j)/2;i=j+1;}return rk;};
 const ra=rank(rowOf),rb=rank(keys),ma=mean(ra),mb=mean(rb),cov=mean(ra.map((x,i)=>(x-ma)*(rb[i]-mb))),rc=cov/(sd(ra)*sd(rb)||1);
 const cols=level.tray.length,deepTop=level.tray.reduce((n,t)=>n+t.slice(0,2).filter(q=>!ctx.frontier.has(level.pods[q].color)).length,0)/cols,frontTop=level.tray.filter(t=>t.length&&ctx.frontier.has(level.pods[t[0]].color)).length;
 const P=level.pods.length;
 return {rankCorr:rc,deepTop,frontTop,P,cols,rows:Math.ceil(P/cols),podMean:ctx.m.N/P,linked:level.pods.filter(q=>q.group).length,linkShare:level.pods.filter(q=>q.group).length/P,traps:built.pairs.filter(x=>x.trap).length,
  hidden:level.pods.filter(q=>q.mystery).length,cellsHidden:level.hiddenCells?rulesSync().create(level,5).veiledLeft:0,latches:(level.latches||[]).length};}

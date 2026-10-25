// Measures of one attempt from its trace (trace.mjs: a model run, exact for our levels, approximate for Colony Flow) or
// from the video (tools/level-stats/observed: filled places and the board's remaining share, frame by frame).
// Every mean over time weights each state by how long it lasted. Full buffer = 0 free places; nearly full = exactly 1.
const r2=x=>x==null||!Number.isFinite(x)?null:Math.round(x*100)/100,r1=x=>x==null||!Number.isFinite(x)?null:Math.round(x*10)/10;
const median=a=>{if(!a.length)return null;const b=a.slice().sort((x,y)=>x-y),m=b.length>>1;return b.length%2?b[m]:(b[m-1]+b[m])/2;};
const mean=a=>a.length?a.reduce((x,y)=>x+y,0)/a.length:null;
export const stats=a=>a.length?{n:a.length,min:Math.min(...a),mean:r2(mean(a)),median:r2(median(a)),max:Math.max(...a)}:{n:0};
// a step series [[t, value], ...] (value holds until the next point) cut to [from, to): duration-weighted pieces
function pieces(series,from,to){const out=[];for(let i=0;i<series.length;i++){const a=Math.max(series[i][0],from),b=Math.min(i+1<series.length?series[i+1][0]:Infinity,to);if(b>a)out.push([a,b,series[i]]);}return out;}
// the buffer of one stretch: free places over time, full and nearly-full time, episodes of a full buffer
export function buffer(free,from,to,cap){const p=pieces(free,from,to),dur=to-from;if(dur<=0||!p.length)return null;
 let sum=0,min=Infinity,max=-Infinity,t0=0,t1=0;const full=[];let open=null;
 for(const [a,b,row] of p){const f=row[1];sum+=f*(b-a);min=Math.min(min,f);max=Math.max(max,f);if(f===0){t0+=b-a;if(open==null)open=a;}else{if(f===1)t1+=b-a;if(open!=null){full.push(a-open);open=null;}}}
 if(open!=null)full.push(to-open);
 return {duration:r1(dur),meanFree:r2(sum/dur),minFree:min,maxFree:max,maxHeld:cap-min,meanHeld:r2(cap-sum/dur),oneFree:r1(t1),oneFreeShare:r2(t1/dur),zeroFree:r1(t0),zeroFreeShare:r2(t0/dur),fullEpisodes:full.length,fullEpisodeSeconds:full.map(r1)};}
const SEG=[0,.2,.4,.6,.8,1];
// a model trace → measures (times in seconds of model time at ×1)
export function fromTrace(tr,{start=0}={}){
 const S=x=>(x-start)/1000,cap=tr.capacity,end=S(tr.end),cells=tr.cells;
 const free=tr.samples.map(s=>[S(s[0]),s[1]]),cleared=tr.samples.map(s=>[S(s[0]),s[5]]),working=tr.samples.map(s=>[S(s[0]),s[2]]),stalled=tr.samples.map(s=>[S(s[0]),s[3]]),waiting=tr.samples.map(s=>[S(s[0]),s[4]]);
 const tapT=tr.taps.map(x=>S(x.t)),relT=tr.releases.map(x=>S(x.t));
 const clearT=tr.clearAt.filter(x=>x>=0).map(S).sort((a,b)=>a-b);
 const at=share=>{const k=Math.ceil(share*cells)-1;return k<0?0:k<clearT.length?clearT[k]:null;};
 const avg=(series,a,b)=>{const p=pieces(series,a,b);const d=p.reduce((n,[x,y])=>n+y-x,0);return d?r2(p.reduce((n,[x,y,row])=>n+row[1]*(y-x),0)/d):null;};
 const segment=(a,b,label)=>{if(a==null)return {label,reached:false};const bb=b??end,buf=buffer(free,a,bb,cap);
  const resumes=tr.pods.reduce((n,p)=>n+p.stops.filter(s=>!s.open&&S(s.to)>=a&&S(s.to)<bb).length,0);
  return {label,reached:true,from:r1(a),to:r1(bb),...buf,taps:tapT.filter(t=>t>=a&&t<bb).length,releases:tr.releases.filter(x=>S(x.t)>=a&&S(x.t)<bb).reduce((n,x)=>n+x.pods.length,0),
   working:avg(working,a,bb),stalled:avg(stalled,a,bb),resumes};};
 const byCleared=SEG.slice(0,5).map((f,i)=>segment(i===0?0:at(f),i===4?(tr.status==='won'?end:at(1)):at(SEG[i+1]),`${f*100}–${SEG[i+1]*100}%`));
 const byTime=SEG.slice(0,5).map((f,i)=>segment(f*end,SEG[i+1]*end,`${f*100}–${SEG[i+1]*100}%`));
 // capsules: tap → release, stops with what was left, resumes
 const pods=tr.pods.map((p,i)=>({i,count:p.count,tap:p.tap==null?null:r2(S(p.tap)),release:p.release==null?null:r2(S(p.release)),held:p.tap!=null&&p.release!=null?r2(S(p.release)-S(p.tap)):null,
  stops:p.stops.map(s=>({from:r1(S(s.from)),to:r1(S(s.to)),left:s.left,open:!!s.open})),resumes:p.resumes})).filter(p=>p.tap!=null);
 const stops=pods.flatMap(p=>p.stops);
 // releases: how many places came free together, and between two taps without a tap (a cascade)
 const releaseSizes=tr.releases.map(x=>x.pods.length),between=[];for(let k=0;k<=tapT.length;k++){const a=k?tapT[k-1]:-1,b=k<tapT.length?tapT[k]:Infinity;between.push(relT.filter((t,i)=>t>a&&t<=b).reduce((n,t)=>n+tr.releases[relT.indexOf(t)].pods.length,0));}
 // first place free after the buffer first filled
 const firstFull=free.find(s=>s[1]===0)?.[0]??null,afterFull=firstFull==null?null:free.find(s=>s[0]>firstFull&&s[1]>0);
 const gaps=tapT.slice(1).map((t,i)=>t-tapT[i]);
 const busyAt=t=>{const p=pieces(working,t,t+1e-6);return p.length&&p[0][2][1]>0;};
 // pauses without progress: stretches over 5 s with no cell cleared (while the attempt runs)
 const pauses=[];for(let i=1;i<clearT.length;i++)if(clearT[i]-clearT[i-1]>5)pauses.push([r1(clearT[i-1]),r1(clearT[i])]);if(clearT.length&&clearT[0]>5)pauses.unshift([0,r1(clearT[0])]);
 return {status:tr.status,end:r1(end),capacity:cap,cells,clearedCells:clearT.length,
  buffer:buffer(free,0,end,cap),segmentsByCleared:byCleared,segmentsByTime:byTime,
  time:{firstTap:r2(tapT[0]??null),firstClear:r2(clearT[0]??null),firstRelease:r2(relT[0]??null),lastTapToEnd:tapT.length?r1(end-tapT.at(-1)):null,tapGaps:stats(gaps.map(r2)),
   tapsWhileWorking:tapT.length?r2(tapT.filter(busyAt).length/tapT.length):null,pausesOver5s:pauses.length,pauseSeconds:r1(pauses.reduce((n,[a,b])=>n+b-a,0)),pauses:pauses.slice(0,40),
   tail90:at(.9)==null?null:r1(end-at(.9))},
  capsules:{taps:tapT.length,heldSeconds:stats(pods.map(p=>p.held).filter(x=>x!=null)),stops:stops.length,stopSeconds:stats(stops.map(s=>s.to-s.from).map(r1)),stopLeft:stats(stops.map(s=>s.left)),
   resumes:pods.reduce((n,p)=>n+p.resumes,0),podsStopped:pods.filter(p=>p.stops.length).length,stuckAtEnd:stops.filter(s=>s.open).map(s=>s.left)},
  releases:{events:releaseSizes.length,places:releaseSizes.reduce((a,b)=>a+b,0),multi:releaseSizes.filter(x=>x>1).length,cascades:between.filter(x=>x>=2).length,maxCascade:Math.max(0,...between),
   firstFreeAfterFull:afterFull?r1(afterFull[0]-firstFull):null},
  series:{free:thin(free,800),cleared:thin(cleared.map(([t,c])=>[t,r2(c/cells)]),400),working:thin(working,400),stalled:thin(stalled,400),taps:tapT.map(r2),releases:tr.releases.map(x=>[r2(S(x.t)),x.pods.length]),
   progressByTap:tr.taps.map(x=>r2(cleared.filter(c=>c[0]<=S(x.t)).at(-1)?.[1]/cells??0))},
  pattern:pattern(tr,S)};}
// a step series thinned to about n points, keeping every change of value at the coarsest time step that fits
export function thin(series,n){if(series.length<=n)return series.map(([t,v])=>[r2(t),v]);const span=series.at(-1)[0]-series[0][0],step=span/n,out=[];let lastT=-Infinity;
 for(const [t,v] of series){if(t-lastT>=step||!out.length){out.push([r2(t),v]);lastT=t;}else out[out.length-1]=[out.at(-1)[0],Math.min(out.at(-1)[1],v)];}return out;}
// the clearing pattern: when each cell went (tenths of the attempt, '.' none), from which side, what went last
function pattern(tr,S){const w=tr.w,h=tr.h,end=S(tr.end);if(!tr.cells)return null;
 const t=tr.clearAt.map(x=>x<0?null:S(x)),cleared=t.filter(x=>x!=null).sort((a,b)=>a-b),q=k=>cleared[Math.min(cleared.length-1,Math.floor(k*cleared.length))];
 const xs=tr.cellIndex.map(i=>i%w),ys=tr.cellIndex.map(i=>h-1-Math.floor(i/w));const minX=Math.min(...xs),maxX=Math.max(...xs),minY=Math.min(...ys),maxY=Math.max(...ys);
 const grid=Array.from({length:maxY-minY+1},()=>Array(maxX-minX+1).fill(' '));
 t.forEach((v,k)=>{grid[ys[k]-minY][xs[k]-minX]=v==null?'.':String(Math.min(9,Math.floor(10*v/Math.max(end,1e-9))));});
 // the order cells went in, by fifths: their mean position (0 = left / top, 1 = right / bottom)
 const order=t.map((v,k)=>[v,k]).filter(x=>x[0]!=null).sort((a,b)=>a[0]-b[0]),fifth=[];
 for(let f=0;f<5;f++){const part=order.slice(Math.floor(f*order.length/5),Math.floor((f+1)*order.length/5));if(!part.length){fifth.push(null);continue;}
  fifth.push({x:r2(mean(part.map(([,k])=>(xs[k]-minX)/Math.max(1,maxX-minX)))),y:r2(mean(part.map(([,k])=>(ys[k]-minY)/Math.max(1,maxY-minY))))});}
 const last=order.slice(-Math.max(1,Math.round(order.length*.05)));
 const edge=k=>xs[k]===minX||xs[k]===maxX||ys[k]===minY||ys[k]===maxY;
 // last 5% of the cells: how many separate clumps (4-neighbour) they make
 const lastSet=new Set(last.map(([,k])=>xs[k]+','+ys[k]));let clumps=0;const seen=new Set();
 for(const key of lastSet){if(seen.has(key))continue;clumps++;const st=[key];seen.add(key);while(st.length){const [x,y]=st.pop().split(',').map(Number);for(const [dx,dy] of [[1,0],[-1,0],[0,1],[0,-1]]){const k2=(x+dx)+','+(y+dy);if(lastSet.has(k2)&&!seen.has(k2)){seen.add(k2);st.push(k2);}}}}
 const colours={};for(const [,k] of last){const c=tr.colours[k];colours[c]=(colours[c]||0)+1;}
 return {grid:grid.map(r=>r.join('')),fifths:fifth,lastCells:last.length,lastClumps:clumps,lastOnEdge:r2(last.filter(([,k])=>edge(k)).length/last.length),lastMeanY:r2(mean(last.map(([,k])=>(ys[k]-minY)/Math.max(1,maxY-minY)))),lastColours:colours,
  quartiles:[q(.25),q(.5),q(.75),q(.9),q(1)].map(r1)};}
// a video observation (observed/<id>.json attempt): filled places and the board's remaining share over time
export function fromVideo(a,taps,cap){
 const free=a.slots.map(([t,f])=>[t-a.start,cap-f]),end=a.end-a.start,progress=a.board?.map(([t,v])=>[t-a.start,1-v])||[];
 const at=share=>{const p=progress.find(([,v])=>v>=share);return p?p[0]:null;};
 const tapT=taps.map(x=>x.t-a.start);
 const seg=(x,y,label)=>{if(x==null)return {label,reached:false};const b=buffer(free,x,y??end,cap);return {label,reached:true,from:r1(x),to:r1(y??end),...b,taps:tapT.filter(t=>t>=x&&t<(y??end)).length};};
 const byTime=SEG.slice(0,5).map((f,i)=>seg(f*end,SEG[i+1]*end,`${f*100}–${SEG[i+1]*100}%`));
 const byCleared=progress.length?SEG.slice(0,5).map((f,i)=>seg(i===0?0:at(f),i===4?end:at(SEG[i+1]),`${f*100}–${SEG[i+1]*100}%`)):null;
 const gaps=tapT.slice(1).map((t,i)=>t-tapT[i]);
 return {status:a.outcome,end:r1(end),capacity:cap,buffer:buffer(free,0,end,cap),segmentsByTime:byTime,segmentsByCleared:byCleared,
  time:{firstTap:r2(tapT[0]??null),lastTapToEnd:tapT.length?r1(end-tapT.at(-1)):null,tapGaps:stats(gaps.map(r2))},
  series:{free:thin(free,800),cleared:progress.length?thin(progress.map(([t,v])=>[t,r2(v)]),400):null,taps:tapT.map(r2)}};}

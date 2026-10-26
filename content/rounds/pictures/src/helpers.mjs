// Shared drawing helpers of the round picture modules (skill bloomlings-round-pictures).
// Layers of [shape, colour] drawn in order; a colour may be a function of the cell (stripes, patterns).
export const raster=(W,H,bg,parts)=>Array.from({length:H},(_,y)=>Array.from({length:W},(_,x)=>{let c=bg;for(const [shape,color] of parts)if(shape(x+.5,y+.5)<0)c=typeof color==='function'?color(x,y,c):color;return c;}));
// A shape drawn in base units, scaled by k round the origin.
export const scale=(k,shape)=>(x,y)=>shape(x/k,y/k)*k;
export const count=(g,c)=>g.flat().filter(v=>v===c).length;
export const N4=[[1,0],[-1,0],[0,1],[0,-1]];
// Bring colour c to exactly n cells by trading edge cells with colour `from`: grow at the cells of `from` touching c
// nearest c's centre, shrink at the cells of c touching `from` farthest from it. Deterministic (ties by row, column).
export function fit(g,c,n,from){if(process.env.DRYFIT)return g;const H=g.length,W=g[0].length;
 for(let guard=0;guard<2000;guard++){const have=count(g,c);if(have===n)return g;
  let sx=0,sy=0;g.forEach((r,y)=>r.forEach((v,x)=>{if(v===c){sx+=x;sy+=y;}}));const cx=sx/have,cy=sy/have,grow=have<n;let best=null;
  for(let y=0;y<H;y++)for(let x=0;x<W;x++){if(g[y][x]!==(grow?from:c))continue;
   if(!N4.some(([dx,dy])=>g[y+dy]?.[x+dx]===(grow?c:from)))continue;
   const d=Math.hypot(x-cx,(y-cy)*1.05);if(!best||(grow?d<best.d:d>best.d))best={x,y,d};}
  if(!best)throw Error(`fit ${c}: no edge cell`);g[best.y][best.x]=grow?c:from;}
 throw Error('fit: no convergence');}
export const blank=(g,cells)=>{for(const [x,y] of cells)g[y][x]='.';return g;};
export const str=g=>g.map(r=>r.join(''));
export const seg=(x1,y1,x2,y2,r)=>(x,y)=>{const ex=x2-x1,ey=y2-y1,t=Math.max(0,Math.min(1,((x-x1)*ex+(y-y1)*ey)/(ex*ex+ey*ey)));return Math.hypot(x-x1-ex*t,y-y1-ey*t)-r;};
export const mask=(shape,pred)=>(x,y)=>pred(Math.floor(x),Math.floor(y))?shape(x,y):1;

// Paint every cell of `from` that touches one of `inner` (4 sides) with c: a ring that shuts the inner colours in.
export function ring4(g,inner,c,from){const H=g.length,W=g[0].length,hit=[];
 for(let y=0;y<H;y++)for(let x=0;x<W;x++)if(g[y][x]===from&&N4.some(([dx,dy])=>inner.includes(g[y+dy]?.[x+dx])))hit.push([x,y]);
 for(const [x,y] of hit)g[y][x]=c;return g;}
export const put=(g,c,cells)=>{for(const [x,y] of cells)g[y][x]=c;return g;};

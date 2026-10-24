// v9 picture painter: scenes made of smooth shapes, rasterised straight to the board's cells, so outlines stay
// one cell thick at any size and objects get volume from a light in the upper left (light tone on the lit side,
// shade tone on the far side), after Colony Flow's look. Pure functions, no randomness: same scene, same board.
export const LIGHT_DIR=(()=>{const v=[-.55,-.65,.55],n=Math.hypot(...v);return v.map(x=>x/n);})();
// Signed distances in cell units (negative inside).
export const circle=(cx,cy,r)=>(x,y)=>Math.hypot(x-cx,y-cy)-r;
export const ellipse=(cx,cy,rx,ry)=>(x,y)=>(Math.hypot((x-cx)/rx,(y-cy)/ry)-1)*Math.min(rx,ry);
export const box=(cx,cy,hw,hh,r=0)=>(x,y)=>{const qx=Math.abs(x-cx)-hw+r,qy=Math.abs(y-cy)-hh+r;return Math.hypot(Math.max(qx,0),Math.max(qy,0))+Math.min(Math.max(qx,qy),0)-r;};
export function poly(pts){return (x,y)=>{let d=Infinity,s=1;for(let i=0,j=pts.length-1;i<pts.length;j=i++){const [ax,ay]=pts[j],[bx,by]=pts[i],ex=bx-ax,ey=by-ay,wx=x-ax,wy=y-ay,t=Math.max(0,Math.min(1,(wx*ex+wy*ey)/(ex*ex+ey*ey)));d=Math.min(d,Math.hypot(wx-ex*t,wy-ey*t));
 if((ay>y)!==(by>y)&&x<(bx-ax)*(y-ay)/(by-ay)+ax)s=-s;}return s*d;};}
export const union=(...f)=>(x,y)=>Math.min(...f.map(g=>g(x,y)));
export const cut=(a,b)=>(x,y)=>Math.max(a(x,y),-b(x,y));
export const meet=(a,b)=>(x,y)=>Math.max(a(x,y),b(x,y));
// A part: {shape, color, light?, shade?, outline?: true (default), group?: parts of one group get no outline between
// them, sphere?: [cx, cy, r] (round volume), flat?: true (volume from its edges)}. Later parts are drawn on top.
export function paint(W,H,{background,parts,outline='D',allowed=null}){
 const ok=c=>!allowed||allowed.includes(c);
 const id=Array.from({length:H},()=>new Int16Array(W).fill(-1)),col=Array.from({length:H},(_,y)=>Array.from({length:W},(_,x)=>background(x,y)));
 parts.forEach((p,k)=>{for(let y=0;y<H;y++)for(let x=0;x<W;x++)if(p.shape(x+.5,y+.5)<0)id[y][x]=k;});
 const area=parts.map(()=>0);for(let y=0;y<H;y++)for(let x=0;x<W;x++)if(id[y][x]>=0)area[id[y][x]]++;
 const group=k=>k<0?-1:(parts[k].group??k);
 for(let y=0;y<H;y++)for(let x=0;x<W;x++){const k=id[y][x];if(k<0)continue;const p=parts[k];let c=p.color;
  // Outline: a cell of an outlined part whose 4-neighbour belongs to another group drawn below it (or the background).
  // Small parts (under 14 cells) keep their colour: a one-cell ring would eat them.
  const edge=p.outline!==false&&area[k]>=(p.minOutline??14)&&[[1,0],[-1,0],[0,1],[0,-1]].some(([dx,dy])=>{const u=x+dx,v=y+dy;if(u<0||v<0||u>=W||v>=H)return false;const n=id[v][u];return group(n)!==group(k)&&(n<k);});
  if(edge){col[y][x]=outline;continue;}
  // Volume: light near the lit edge (and a gloss on round parts), shade only in a thin band on the far edge.
  const depth=-p.shape(x+.5,y+.5),gx=p.shape(x+1,y+.5)-p.shape(x,y+.5),gy=p.shape(x+.5,y+1)-p.shape(x+.5,y),facing=(gx*LIGHT_DIR[0]+gy*LIGHT_DIR[1])/Math.max(1e-6,Math.hypot(gx,gy));
  let lit=false,dark=false;
  if(p.sphere){const [cx,cy,r]=p.sphere,nx=(x+.5-cx)/r,ny=(y+.5-cy)/r,nz=Math.sqrt(Math.max(0,1-nx*nx-ny*ny)),lum=nx*LIGHT_DIR[0]+ny*LIGHT_DIR[1]+nz*LIGHT_DIR[2];
   lit=lum>(p.lightAt??.8)||(depth<1.5&&facing>.55);dark=depth<(p.band??2.2)&&facing<-.3;}
  else if(p.flat){lit=depth<(p.band??1.6)&&facing>.35;dark=depth<(p.band??1.6)&&facing<-.35;}
  if(p.light&&lit&&ok(p.light))c=p.light;else if(p.shade&&dark&&ok(p.shade))c=p.shade;
  col[y][x]=c;}
 return col;
}
// Backgrounds: sky that lightens toward the horizon (water tone above, light sky below), and grass with a darker foot.
export function sky(W,H,{horizon=.55,ground=null}={}){const seam=Math.round(H*horizon);
 return (x,y)=>{if(ground!==null&&y>=ground)return y>=H-2?'M':((x*7+y*3)%11===0?'L':'G');return y>seam||(y===seam&&x%2===0)?'S':'B';};}
export const rows=(col)=>col.map(r=>r.join(''));

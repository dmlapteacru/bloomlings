// SVG drafts → cell pictures (17 §13, the owner's decision of 2026-10-27: the mass of pictures comes from flat vector
// drafts I draw, converted here; special pictures — L1–3, streams with bridges — stay hand-made matrices).
// A draft module: export default {theme, pictures:[{title, family, direction?, subject?, source?, cells: 900,
//   colours?: 8, svg: `<svg viewBox="0 0 W H">…</svg>`}]} with fills from tools/rounds/svg-palette.mjs (C.G, C.P, …);
//   transparent areas are empty cells. The converter renders each SVG once in Chromium at high resolution, maps every
//   pixel to the nearest palette colour (ΔE), then picks the grid whose coloured cell count is closest to `cells`
//   (each cell takes the colour covering most of it), folds patches of 1–2 cells into their neighbour and, over
//   `colours`, the smallest colour into its closest neighbour. Output: a picture module for pictures.mjs / screen.mjs.
// node tools/rounds/svg2cells.mjs <drafts.mjs> --out <batch.mjs> [--sheet <png> [--level N]]
import fs from 'node:fs';import path from 'node:path';import {pathToFileURL} from 'node:url';import {createRequire} from 'node:module';import {execFileSync} from 'node:child_process';
import {CODES,PALETTE,rgbOf,arg} from './lib.mjs';
const codes=[...CODES],lab=hex=>{const c=rgbOf(hex).map(v=>{v/=255;return v<=.04045?v/12.92:((v+.055)/1.055)**2.4;});
 const x=(c[0]*.4124+c[1]*.3576+c[2]*.1805)/.9505,y=c[0]*.2126+c[1]*.7152+c[2]*.0722,z=(c[0]*.0193+c[1]*.1192+c[2]*.9505)/1.089;const f=t=>t>.008856?Math.cbrt(t):7.787*t+16/116;return [116*f(y)-16,500*(f(x)-f(y)),200*(f(y)-f(z))];};
const LAB=codes.map(c=>lab(PALETTE[c].color)),dE=(a,b)=>Math.hypot(a[0]-b[0],a[1]-b[1],a[2]-b[2]);
// nearest palette index of an RGB pixel, cached
const cache=new Map();const nearest=(r,g,b)=>{const k=(r<<16)|(g<<8)|b;let v=cache.get(k);if(v===undefined){const l=lab('#'+[r,g,b].map(x=>x.toString(16).padStart(2,'0')).join(''));v=0;let best=1e9;LAB.forEach((q,i)=>{const d=dE(l,q);if(d<best){best=d;v=i;}});cache.set(k,v);}return v;};

async function rasterize(svgs,long=960){const req=createRequire(import.meta.url);let pw;try{pw=req('../heroanim/node_modules/playwright-core');}catch{pw=req('playwright');}
 const browser=await pw.chromium.launch({args:['--no-sandbox']});const page=await browser.newPage();const out=[];
 for(const svg of svgs){const r=await page.evaluate(async([svg,long])=>{const vb=(svg.match(/viewBox="([^"]+)"/)||[])[1];const [,, w,h]=vb?vb.split(/[ ,]+/).map(Number):[0,0,100,100];
   const W=w>=h?long:Math.round(long*w/h),H=w>=h?Math.round(long*h/w):long,img=new Image();img.src='data:image/svg+xml;base64,'+btoa(unescape(encodeURIComponent(svg)));await img.decode();
   const cv=document.createElement('canvas');cv.width=W;cv.height=H;const g=cv.getContext('2d');g.drawImage(img,0,0,W,H);return {W,H,data:Array.from(g.getImageData(0,0,W,H).data)};},[svg,long]);out.push(r);}
 await browser.close();return out;}

// palette index per pixel (−1 transparent)
const toIndex=({W,H,data})=>{const idx=new Int8Array(W*H);for(let i=0;i<W*H;i++)idx[i]=data[i*4+3]<128?-1:nearest(data[i*4],data[i*4+1],data[i*4+2]);return idx;};
// a grid of gw × gh cells: each takes the value covering most of its block
function grid(idx,W,H,gw,gh){const rows=[];for(let j=0;j<gh;j++){let row='';for(let i=0;i<gw;i++){const cnt=new Map();
  for(let y=Math.floor(j*H/gh);y<Math.floor((j+1)*H/gh);y++)for(let x=Math.floor(i*W/gw);x<Math.floor((i+1)*W/gw);x++){const v=idx[y*W+x];cnt.set(v,(cnt.get(v)||0)+1);}
  let best=-1,bn=-1;for(const [v,n] of cnt)if(n>bn){bn=n;best=v;}row+=best<0?'.':codes[best];}rows.push(row);}return rows;}
const countN=rows=>rows.join('').replace(/\./g,'').length;
// patches of 1–2 cells fold into the colour around them; over `max` colours the smallest folds into its closest neighbour
function clean(rows,max){let g=rows.map(r=>[...r]);const H=g.length,W=g[0].length,nb=[[1,0],[-1,0],[0,1],[0,-1]];
 for(let pass=0;pass<3;pass++){const seen=new Uint8Array(W*H);for(let y=0;y<H;y++)for(let x=0;x<W;x++){const c=g[y][x];if(c==='.'||seen[y*W+x])continue;const comp=[[x,y]],q=[[x,y]];seen[y*W+x]=1;
   while(q.length){const [u,v]=q.pop();for(const [dx,dy] of nb){const a=u+dx,b=v+dy;if(a<0||b<0||a>=W||b>=H||seen[b*W+a]||g[b][a]!==c)continue;seen[b*W+a]=1;q.push([a,b]);comp.push([a,b]);}}
   if(comp.length>2)continue;const around=new Map();for(const [u,v] of comp)for(const [dx,dy] of nb){const a=u+dx,b=v+dy;if(a<0||b<0||a>=W||b>=H)continue;const d=g[b][a];if(d!==c&&d!=='.')around.set(d,(around.get(d)||0)+1);}
   if(around.size){const to=[...around].sort((p,q)=>q[1]-p[1])[0][0];for(const [u,v] of comp)g[v][u]=to;}}}
 const vol=()=>{const o={};for(const r of g)for(const c of r)if(c!=='.')o[c]=(o[c]||0)+1;return o;};
 while(max&&Object.keys(vol()).length>max){const v=vol(),small=Object.keys(v).sort((a,b)=>v[a]-v[b])[0],si=codes.indexOf(small),to=Object.keys(v).filter(c=>c!==small).sort((a,b)=>dE(LAB[si],LAB[codes.indexOf(a)])-dE(LAB[si],LAB[codes.indexOf(b)]))[0];g=g.map(r=>r.map(c=>c===small?to:c));}
 // trim empty border rows and columns
 let rs=g.map(r=>r.join(''));while(rs.length&&/^\.+$/.test(rs[0]))rs.shift();while(rs.length&&/^\.+$/.test(rs.at(-1)))rs.pop();
 let l=0,r=rs[0].length;while(l<r&&rs.every(x=>x[l]==='.'))l++;while(r>l&&rs.every(x=>x[r-1]==='.'))r--;return rs.map(x=>x.slice(l,r));}

export async function convert(pics){const ras=await rasterize(pics.map(p=>p.svg)),out=[];
 for(const [k,p] of pics.entries()){const {W,H}=ras[k],idx=toIndex(ras[k]),target=p.cells||900,aspect=W/H;let best=null;
  for(let gw=8;gw<=44;gw++){const gh=Math.max(4,Math.round(gw/aspect));if(gh>44)break;const rows=clean(grid(idx,W,H,gw,gh),p.colours||10),n=countN(rows);if(!best||Math.abs(n-target)<Math.abs(best.n-target))best={rows,n,gw,gh};}
  const {svg,cells,colours,...meta}=p;out.push({...meta,rows:best.rows});console.log(`#${k+1} «${p.title}»: сетка ${best.rows[0].length}×${best.rows.length}, ${best.n} клеток (цель ${target}), цветов ${new Set(best.rows.join('').replace(/\./g,'')).size}`);}
 return out;}

if(import.meta.url===pathToFileURL(path.resolve(process.argv[1])).href){const src=process.argv[2],dst=arg('--out');if(!src||!dst){console.log(fs.readFileSync(new URL(import.meta.url),'utf8').split('\n').filter(l=>l.startsWith('//')).join('\n'));process.exit(1);}
 const b=(await import(pathToFileURL(path.resolve(src)).href)).default,pics=await convert(b.pictures);
 fs.mkdirSync(path.dirname(path.resolve(dst)),{recursive:true});
 fs.writeFileSync(dst,`// Converted by tools/rounds/svg2cells.mjs from ${path.relative(path.dirname(path.resolve(dst)),path.resolve(src))}; edit the SVG draft and convert again rather than this file.\nexport default ${JSON.stringify({theme:b.theme,pictures:pics},null,1)};\n`);
 console.log('→',dst);
 // the contact sheet straight from the matrices, checked against one stage (--level, default 30); a rules failure is reported, not thrown
 if(arg('--sheet'))try{execFileSync(process.execPath,[new URL('./pictures.mjs',import.meta.url).pathname,'verify',dst,'--level',arg('--level','30'),'--out',arg('--sheet')],{stdio:'inherit'});}catch{}}

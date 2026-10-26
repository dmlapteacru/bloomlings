// Bring read-back pictures to the owner's stated cells and colours (the captions of the reference sheet): the fine
// matrix is resampled onto a coarser grid (each coarse cell takes the material, or emptiness, that covers most of it),
// the grid size and offset closest to the target are kept, and the last few cells are added or taken at the edge.
// node tools/rounds/from-image/fit-size.mjs <in.mjs> <out.mjs>
import fs from 'node:fs';import path from 'node:path';import {pathToFileURL} from 'node:url';
const [,,src,dst]=process.argv;
const b=(await import(pathToFileURL(path.resolve(src)).href)).default;
// the reference's captions, and how its extra shades fold into the stated colour count
const TARGET={4:{N:320,merge:{O:'A'}},5:{N:380},6:{N:432},7:{N:360},8:{N:484},9:{N:576,merge:{S:'B'}},10:{N:480,merge:{A:'E'}}};
const N4=[[1,0],[-1,0],[0,1],[0,-1]];
// hand fixes after resampling, where a thin detail was lost: whole rows (rows: {index: text}, the picture's last rows
// cut by cut), single cells ([x, y, material]); the count must still match the target, which the script checks
const EDITS={
 4:{cut:1,rows:{0:'.......YYBRCC.......',7:'.RRYYYYBBBRRRCCCBBB.',8:'.RRYYYYBBBRRRCCCBBB.',9:'.RRYYYYBBBRRRCCCBBB.',10:'.RRRYYYBBBRRRCCCBBB.',
  16:'.....RRYYBRCCBB.....',17:'......AAAAAAAA......',18:'......A......A......',19:'......A......A......',20:'.....AAAAAAAAAA.....',
  21:'.....AACAACAACA.....',22:'.....CAACAACAAC.....',23:'.....AACAACAACA.....',24:'......AAAAAAAA......'}},
 6:{cells:[[6,16,'C'],[5,18,'C'],[5,19,'C'],[5,20,'C']]},
 9:{cells:[...[16,17,18,19,20,21].flatMap(y=>[[8,y,'C'],[20,y,'B'],[21,y,'B'],[22,y,'C'],[23,y,'B'],[24,y,'B'],[25,y,'C']]),
  ...[6,7,9,10,20,21,23,24].map(x=>[x,18,'C']),[17,19,'C']]}};
// rarer materials weigh more, so thin details (ropes, stripes, crossbars) survive the smaller grid
function boost(rows,k){const n={};rows.flat().forEach(c=>{if(c!=='.')n[c]=(n[c]||0)+1;});const t=Object.values(n).reduce((a,v)=>a+v,0),w={};
 for(const c in n)w[c]=Math.pow(t/n[c]/Object.keys(n).length,k);return w;}
function resample(rows,f,ox,oy,wt){const H=rows.length,W=rows[0].length,h=Math.ceil((H+oy)/f),w=Math.ceil((W+ox)/f),g=[];
 for(let j=0;j<h;j++){const r=[];for(let i=0;i<w;i++){const wsum={};
  const x0=i*f-ox,x1=x0+f,y0=j*f-oy,y1=y0+f;
  for(let y=Math.max(0,Math.floor(y0));y<Math.min(H,Math.ceil(y1));y++)for(let x=Math.max(0,Math.floor(x0));x<Math.min(W,Math.ceil(x1));x++){
   const a=(Math.min(x+1,x1)-Math.max(x,x0))*(Math.min(y+1,y1)-Math.max(y,y0));if(a>0)wsum[rows[y][x]]=(wsum[rows[y][x]]||0)+a;}
  const filled=Object.entries(wsum).filter(([c])=>c!=='.').reduce((s,[,v])=>s+v,0);let best='.',bw=-1;
  if(filled>=f*f*.5)for(const [c,v] of Object.entries(wsum))if(c!=='.'&&v*wt[c]>bw){best=c;bw=v*wt[c];}r.push(best);}g.push(r);}return g;}
const count=g=>g.flat().filter(c=>c!=='.').length;
function adjust(g,N){const H=g.length,W=g[0].length,at=(x,y)=>g[y]?.[x]??'.';
 for(let guard=0;guard<500;guard++){const n=count(g);if(n===N)return g;let best=null;
  for(let y=0;y<H;y++)for(let x=0;x<W;x++){const nb=N4.map(([dx,dy])=>at(x+dx,y+dy)),filled=nb.filter(c=>c!=='.').length;
   if(n>N&&g[y][x]!=='.'&&filled<4){const s=filled*10+(nb.filter(c=>c===g[y][x]).length);if(!best||s<best.s)best={x,y,s};}
   if(n<N&&g[y][x]==='.'&&filled>0){const s=-filled;if(!best||s<best.s){const tally={};nb.forEach(c=>{if(c!=='.')tally[c]=(tally[c]||0)+1;});best={x,y,s,c:Object.entries(tally).sort((a,b)=>b[1]-a[1])[0][0]};}}}
  g[best.y][best.x]=n>N?'.':best.c;}return g;}
const trim=g=>{const ys=g.map((r,j)=>r.some(c=>c!=='.')?j:-1).filter(j=>j>=0),xs=g[0].map((_,i)=>g.some(r=>r[i]!=='.')?i:-1).filter(i=>i>=0);return g.slice(ys[0],ys.at(-1)+1).map(r=>r.slice(xs[0],xs.at(-1)+1));};
const out=b.pictures.map(p=>{const t=TARGET[p.forLevel];let rows=p.rows.map(r=>[...r].map(c=>t.merge?.[c]||c));
 const n0=count(rows),wt=boost(rows,.6);let best=null;
 for(let f=Math.sqrt(n0/t.N)*.9;f<=Math.sqrt(n0/t.N)*1.1;f+=.01)for(const ox of [0,.25,.5,.75])for(const oy of [0,.25,.5,.75]){
  const g=resample(rows,f,ox*f,oy*f,wt),d=Math.abs(count(g)-t.N);if(!best||d<best.d)best={g,d,f};}
 let g=trim(adjust(best.g,t.N));const e=EDITS[p.forLevel];
 if(e){if(e.cut)g=g.slice(0,g.length-e.cut);for(const [j,r] of Object.entries(e.rows||{}))g[j]=[...r];for(const [x,y,c] of e.cells||[])g[y][x]=c;
  if(count(g)!==t.N)throw Error(`L${p.forLevel}: hand fixes give ${count(g)} cells, target ${t.N}`);}
 console.log(p.forLevel,p.title,'scale 1/'+best.f.toFixed(2),'adjusted',best.d,'cells');
 return {title:p.title,forLevel:p.forLevel,rows:g.map(r=>r.join(''))};});
fs.writeFileSync(dst,`// The owner's example sheet of 2026-10-26 at its stated cells and colours: ${path.basename(src)} resampled by\n// tools/rounds/from-image/fit-size.mjs.\nexport default {theme:'reference',pictures:${JSON.stringify(out,null,1)}};\n`);

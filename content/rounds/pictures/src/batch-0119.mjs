// Batch 119: a replacement for level 9 (the geode was rejected): a nest of hungry chicks in dark outlines.
import {circle,ellipse,box,poly,union,cut,meet} from '../../../../tools/gameplay-pilot-v9/draw.mjs';
import {raster,str,put} from './helpers.mjs';

const grow=(s,d)=>(x,y)=>s(x,y)-d;
// a sprite of rows ('.' transparent) laid on the grid with its top-left cell at (x0,y0)
const sprite=(g,x0,y0,rows)=>rows.forEach((r,y)=>[...r].forEach((c,x)=>{if(c!=='.')g[y0+y][x0+x]=c;}));

// three chicks with wide open beaks in a woven nest, two leaves at its sides; every part in a dark outline
function nest(){const W=31,H=25;
 const chick=(cx,cy,r)=>union(circle(cx,cy,r),ellipse(cx+0.5,cy-r-0.3,0.9,1.6));
 const L=chick(6.5,10.6,4.6),R=chick(24.5,10.6,4.6),Mid=chick(15.5,7.6,5.1);
 const rim=ellipse(15.5,15.9,13.4,1.3);
 const bowl=union(meet(ellipse(15.5,15.9,12.8,7.9),box(15.5,21,16,5.1)),rim);
 const g=raster(W,H,'.',[
  [grow(L,1),'D'],[L,'Y'],
  [grow(R,1),'D'],[R,'Y'],
  [grow(Mid,1),'D'],[Mid,'Y'],
  [grow(bowl,1),'D'],
  [bowl,'A'],
  [rim,'E'],
  // the weave: short dark twigs in alternate rows, offset
  [meet(bowl,box(15.5,21.5,16,4)),(x,y,c)=>{const d=Math.abs(x-15)%4;return y%2===0&&(y%4===0?d===1||d===2:d===0||d===3)?'E':c;}],
 ]);
 // faces: eyes, rosy cheeks, the open beak with its red mouth
 const face=(cx,ey)=>{put(g,'D',[[cx-2,ey],[cx-2,ey+1],[cx+2,ey],[cx+2,ey+1]]);put(g,'K',[[cx-3,ey+2],[cx+3,ey+2]]);
  sprite(g,cx-2,ey+2,[
   'OOOOO',
   'ORRRO',
   '.OOO.',
  ]);};
 face(6,9);face(24,9);face(15,6);
 // the rim runs out to the outline; a leaf hangs out of each side of the nest (left half drawn, mirrored)
 const side=['DDDDD','DEEEE','DDEEE','DGGDA','DGGGD','DMGGD','.DMMD','..DDD'];
 side.forEach((r,y)=>[...r].forEach((c,x)=>{if(c!=='.'){g[14+y][x]=c;g[14+y][W-1-x]=c;}}));
 // the side chicks' tufts
 put(g,'D',[[5,3],[8,3],[23,3],[26,3]]);put(g,'Y',[[6,3],[7,3],[24,3],[25,3]]);
 return str(g);}

export default {theme:'batch-0119',pictures:[
 {title:'Гнездо с птенцами',forLevel:9,family:'Птицы',subject:'три голодных птенца с открытыми клювами в плетёном гнезде',source:'оригинальный',rows:nest()},
]};

// Batch 1, version 2 (P0001–P0010): the owner's board-picture brief of 2026-10-26 (skill bloomlings-board-picture)
// and their example sheet for levels 4–10. One object per picture, flat colour areas without outlines, the space
// round the silhouette left empty, shading as a solid second material. Each picture is drawn in base units and scaled
// by k to its target size; L1–L3 keep the exact volumes of rule 17 §4.
import {circle,ellipse,box,poly,union,cut,meet} from '../../../../tools/gameplay-pilot-v9/draw.mjs';
import {raster,fit,str,put,ring4,scale} from './helpers.mjs';

// draw(w, h, k, layers): a w×h base design scaled by k; layers are [shape, colour] in base units
const draw=(w,h,k,layers)=>raster(Math.round(w*k),Math.round(h*k),'.',layers.map(([s,c])=>[scale(k,s),typeof c==='function'?(x,y,u)=>c(x/k,y/k,u):c]));
const seg=(x1,y1,x2,y2,r)=>(x,y)=>{const ex=x2-x1,ey=y2-y1,t=Math.max(0,Math.min(1,((x-x1)*ex+(y-y1)*ey)/(ex*ex+ey*ey)));return Math.hypot(x-x1-ex*t,y-y1-ey*t)-r;};
const mirror=(cx,pts)=>pts.map(([x,y])=>[2*cx-x,y]);
// a fin: dark blue with a blue inside, so its edge reads as a darker band
const fin=shape=>[[shape,'N'],[(x,y)=>shape(x,y)+1.1,'B']];

// ---- L1 orange slice: peel O 63 outside, pith C 37 (a ring and four segment lines), flesh Y 38 inside. The layers
// nest, so the peel opens first, then the pith, then the flesh (§4) ----
function orange(){const g=raster(13,13,'.',[[circle(6.5,6.5,6.6),'O'],[circle(6.5,6.5,3.9),'Y']]);
 const lines=[[6,6],[6,3],[6,4],[6,5],[6,7],[6,8],[6,9],[3,6],[4,6],[5,6],[7,6],[8,6],[9,6]].filter(([x,y])=>g[y][x]==='Y');
 fit(g,'Y',38+lines.length,'O');put(g,'C',lines);ring4(g,['Y','C'],'C','O');fit(g,'C',37,'O');fit(g,'O',63,'.');return str(g);}

// ---- L2 watermelon slice: flesh R 120, rind G 97, pale inner rind C 19, seeds D 4 ----
function watermelon(){const half=r=>(x,y)=>Math.hypot(x-12.5,y)-r;const g=raster(25,13,'.',[[half(12.4),'G'],[half(9.7),'C'],[half(8.9),'R']]);
 put(g,'D',[[8,3],[16,3],[10,6],[14,6]]);fit(g,'R',120,'C');fit(g,'C',19,'G');fit(g,'G',97,'.');return str(g);}

// ---- L3 scarlet macaw on a branch, hand-laid: red R 76, blue B 70, branch E 69, yellow wing Y 51, face and beak C 13,
// eye D 2 ----
const macaw=['.......RRRRR.........',
 '......RRRRRRR........',
 '.....RRRRRRRRR.......',
 '.....RRRRRRCDCC......',
 '.....RRRRRRCDCCC.....',
 '....RRRRRRRRCCCC.....',
 '....RRRRRRRRRRCC.....',
 '....YYYYYYRRRR.......',
 '..YYYYYYYYYRRR.......',
 '..YYYYYYYYYRRR.......',
 '..YYYYYYYYYRRR.......',
 '..BBYYYYYYYRRR.......',
 '..BBYYYYYYRRR........',
 '..BBBBYYYYRRR........',
 '..BBBBBBYRRR.........',
 '...BBBBBB.........E..',
 '....BBBBBB.......EE..',
 'EEEEEBBBBBEEEEEEEEEEE',
 'EEEEEEBBBBBEEEEEEEEEE',
 'EEEEEEEBBBEEEEEEEEEEE',
 '.EEEEEEBBBEEEEEEEEEE.',
 '.......BBBB..........',
 '.......BBBB..........',
 '.......BBBB..........',
 '........BBB..........',
 '........BBB..........',
 '........BBB..........',
 '........BBB..........',
 '.........BB..........',
 '.........BB..........'];

// ---- L4 hot-air balloon: six meridian stripes, ropes, a woven basket ----
function balloon(){const k=1.12,cx=12,env=union(circle(cx,11.4,10.6),poly([[2,13.4],[22,13.4],[15.8,24.4],[8.2,24.4]]));
 // the stripe of a cell: its place across the envelope's width at that height, so the stripes bend like meridians
 const half=y=>y<13.4?Math.sqrt(Math.max(4,10.6**2-(y-11.4)**2)):10-(y-13.4)*(10-3.8)/11,stripes='RYBRCB';
 const band=(x,y)=>y<2.2?'B':stripes[Math.max(0,Math.min(5,Math.floor((x-cx)/Math.max(half(y),1)*3+3)))];
 return str(draw(24,34,k,[
  [env,band],
  [box(cx,24.9,3.6,.6),'A'],
  [union(seg(8.9,25.4,9.6,29.2,.5),seg(15.1,25.4,14.4,29.2,.5)),'E'],
  [box(cx,31.4,3.6,2.4,.5),'A'],
  [meet(box(cx,31.4,3.6,2.4,.5),(x,y)=>(Math.floor(y-29)%2===0)!==(Math.floor(x)%2===0)?-1:1),'E'],
  [box(cx,29.4,4.2,.55),'E']]));}

// ---- L5 rocket: red nose and fins, a porthole with a blue rim, a band, a two-tone flame ----
function rocket(){const k=1.3,cx=12,hull=ellipse(cx,15,6.6,11.4);
 return str(draw(24,34,k,[
  [union(poly([[6,15.4],[1.4,24.4],[1.4,27.4],[6,24.4]]),poly(mirror(cx,[[6,15.4],[1.4,24.4],[1.4,27.4],[6,24.4]]))),'R'],
  [poly([[8.4,24.6],[15.6,24.6],[14.8,28.6],[12,33.4],[9.2,28.6]]),'O'],
  [poly([[10,24.6],[14,24.6],[13.4,27.6],[12,30.6],[10.6,27.6]]),'Y'],
  [meet(hull,box(cx,16,7,8.6)),'C'],
  [meet(hull,(x,y)=>y-7.6),'R'],
  [meet(hull,box(cx,24.6,7,.7)),'B'],
  [circle(cx,13.4,3.6),'B'],
  [circle(cx,13.4,2.3),'N'],
  [box(cx,24.6,1,.7),'B']]));}

// ---- L6 fly agaric: a red cap with big spots over a cream gill band, a shaded stem, a tuft of grass ----
function mushroom(){const k=.98,cx=15,cap=cut(ellipse(cx,14.6,14.4,13.4),box(cx,21.4,16,5.8));
 return str(draw(30,30,k,[
  [union(box(cx,22.4,5.6,5.6,2.6),ellipse(cx,27,6.6,1.8)),'C'],
  [meet(union(box(cx,22.4,5.6,5.6,2.6),ellipse(cx,27,6.6,1.8)),(x,y)=>cx+2.2-x),'A'],
  [union(ellipse(cx-5.8,28,4.4,1.6),ellipse(cx+6,28,4.6,1.6),ellipse(cx,28.6,8,1.2)),'G'],
  [meet(union(ellipse(cx-5.8,28,4.4,1.6),ellipse(cx+6,28,4.6,1.6),ellipse(cx,28.6,8,1.2)),(x,y)=>28.4-y),'M'],
  [ellipse(cx,15.8,12.6,2),'A'],
  [cap,'R'],
  [meet(cap,union(circle(cx-6.8,8.4,3),circle(cx+.8,4.6,3.2),circle(cx+7.8,9.4,2.8),circle(cx-1.8,11.8,2),circle(cx+12.4,13.4,1.8),circle(cx-12,13,1.8))),'C']]));}

// ---- L7 ice cream: three scoops (strawberry, mint, vanilla) with a cherry on a waffle cone ----
function iceCream(){const k=1.3,cx=12;
 const cone=poly([[4.4,19],[19.6,19],[12,34]]);
 return str(draw(24,34,k,[
  [cone,(x,y)=>Math.floor((x-y+40)/1.15)%3===0?'A':'Y'],
  [union(circle(6.6,16.2,4.6),ellipse(6.6,19,4.8,1.6)),'P'],
  [union(circle(17.4,16.2,4.6),ellipse(17.4,19,4.8,1.6)),'G'],
  [circle(cx,10.4,5.2),'C'],
  [circle(cx,3.6,2.1),'R'],
  [seg(cx+.6,1.6,cx+2.6,-.4,.45),'M']]));}

// ---- L8 striped fish: orange body with white and red bands, blue fins and tail with dark stripes ----
function fish(){const k=1.2,body=ellipse(12.6,14,10.6,8.4);
 return str(draw(34,28,k,[
  ...fin(poly([[21,14],[32.4,4.6],[30,14],[32.4,23.4]])),
  ...fin(union(poly([[8.4,6.4],[13.6,1.4],[19.6,2.6],[18,7]]),poly([[11,21.4],[15.4,26.4],[18.6,21]]))),
  [body,'O'],
  [meet(body,(x,y)=>Math.abs(x-10.6)-1.7),'C'],
  [meet(body,(x,y)=>Math.abs(x-15.4)-1.6),'R'],
  [meet(body,(x,y)=>Math.abs(x-19.4)-1.2),'C'],
  [poly([[12.4,16],[17,18.4],[13.4,20.6]]),'B'],
  [circle(6.4,12,1.9),'C'],
  [circle(6,12.2,1.05),'N']]));}

// ---- L9 cottage: a red roof with a dark eave, a chimney, cream walls, two windows, a door, bushes ----
function cottage(){const k=1.12,cx=14;
 return str(draw(28,26,k,[
  [box(19.6,5.4,1.4,2.8),'A'],
  [box(cx,17.4,9.6,6.4),'C'],
  [poly([[1.6,12.4],[cx,1.4],[26.4,12.4]]),'R'],
  [poly([[1.6,12.4],[26.4,12.4],[25.4,13.6],[2.6,13.6]]),'E'],
  [box(cx,19.6,2.4,4.2,1),'E'],
  [circle(cx+1.4,20,.5),'C'],
  ...[7.6,20.4].map(x=>[box(x,17.4,2.4,2.4),'B']),
  ...[7.6,20.4].map(x=>[union(box(x,17.4,.35,2.4),box(x,17.4,2.4,.35)),'C']),
  [union(ellipse(3.4,23.2,3.6,2),ellipse(24.6,23.2,3.6,2),box(cx,24.4,13.4,.8)),'G'],
  [meet(union(ellipse(3.4,23.2,3.6,2),ellipse(24.6,23.2,3.6,2),box(cx,24.4,13.4,.8)),(x,y)=>24-y),'M']]));}

// ---- L10 sailboat: a striped cream sail and a blue sail with a yellow band, a mast and flag, a hull, waves ----
function sailboat(){const k=1.38,mast=15;
 return str(draw(30,30,k,[
  [poly([[mast-.6,4],[mast-.6,21.4],[3,21.4]]),(x,y)=>Math.floor(y/2.2)%3===2?'R':'C'],
  [poly([[mast+.6,2.4],[mast+.6,21.4],[27,21.4]]),(x,y)=>Math.abs(y-15)<1.1?'Y':'B'],
  [box(mast,12.6,.6,10.6),'E'],
  [poly([[mast+.6,.6],[mast+4.6,1.6],[mast+.6,2.6]]),'R'],
  [poly([[1.4,22.6],[28.6,22.6],[25,27.4],[5,27.4]]),'E'],
  [box(mast,24.4,12,.55),'A'],
  [box(15,28.6,14.6,1.6),(x,y)=>y<27.9+.8*Math.sin(x*1.25)?'.':y<28.8?'B':'N']]));}

export default {theme:'board-v2',pictures:[
 {title:'Долька апельсина',theme:'food',forLevel:1,rows:orange()},
 {title:'Долька арбуза',theme:'food',forLevel:2,rows:watermelon()},
 {title:'Попугай ара',theme:'animals',forLevel:3,rows:macaw},
 {title:'Воздушный шар',theme:'objects',forLevel:4,rows:balloon()},
 {title:'Ракета',theme:'objects',forLevel:5,rows:rocket()},
 {title:'Мухомор',theme:'plants',forLevel:6,rows:mushroom()},
 {title:'Мороженое',theme:'food',forLevel:7,rows:iceCream()},
 {title:'Рыбка',theme:'animals',forLevel:8,rows:fish()},
 {title:'Домик',theme:'scenes',forLevel:9,rows:cottage()},
 {title:'Кораблик',theme:'objects',forLevel:10,rows:sailboat()}]};

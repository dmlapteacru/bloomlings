// Batch 0011, part 03: clockwork owl, harvest basket, tent by the campfire, desk globe, octopus conductor (L21–L25).
import {circle,ellipse,box,poly,union,cut,meet} from '../../../../../tools/gameplay-pilot-v9/draw.mjs';
import {raster,fit,str,put,ring4,scale,seg} from '../helpers.mjs';
const draw=(w,h,k,layers)=>raster(Math.round(w*k),Math.round(h*k),'.',layers.map(([s,c])=>[scale(k,s),typeof c==='function'?(x,y,u)=>c(x/k,y/k,u):c]));
// A tube along a polyline whose radius goes from r0 to r1.
const tube=(pts,r0,r1)=>{const n=pts.length-1;return union(...pts.slice(0,-1).map((p,i)=>seg(p[0],p[1],pts[i+1][0],pts[i+1][1],r0+(r1-r0)*(i+.5)/n)));};
// Drop empty border rows and columns.
const trim=r=>{r=r.filter((_,i)=>true);while(r.length&&!/[^.]/.test(r[0]))r.shift();while(r.length&&!/[^.]/.test(r[r.length-1]))r.pop();
 while(r.every(x=>x[0]==='.'))r=r.map(x=>x.slice(1));while(r.every(x=>x[x.length-1]==='.'))r=r.map(x=>x.slice(0,-1));return r;};
const half=(pred)=>(x,y)=>pred(x,y)?-1:1;

function owl(){
 const W=36,H=37,cx=15;
 const body=union(ellipse(cx,21.5,11,12.5),box(cx,11,11,8.5,5),poly([[4,7],[5.5,0.5],[11,4.5]]),poly([[26,7],[24.5,0.5],[19,4.5]]));
 const g=raster(W,H,'.',[
  // wind-up key behind the right side: shaft and butterfly bow
  [box(28.5,21,3.5,1.1),'O'],
  [union(ellipse(32.5,16.5,2.4,3.4),ellipse(32.5,25.5,2.4,3.4),box(32.5,21,1.2,2.5)),'Y'],
  [union(circle(32.5,16,.9),circle(32.5,26,.9)),'.'],
  [body,'A'],
  // wings: dark wood
  [meet(ellipse(4.5,22.5,4.5,10),body),'E'],[meet(ellipse(25.5,22.5,4.5,10),body),'E'],
  // belly: brass plates with seams
  [ellipse(cx,24.5,7,8.5),(x,y)=>((y-17)%3===0)?'O':'Y'],
  // face disc, big eyes, beak
  [box(cx,11,10,6,4.5),'O'],
  [circle(10,11,4.6),'Y'],[circle(20,11,4.6),'Y'],
  [circle(10,11,3.4),'C'],[circle(20,11,3.4),'C'],
  [circle(10,11,1.9),'D'],[circle(20,11,1.9),'D'],
  [poly([[13,14.5],[17,14.5],[15,18.2]]),'D'],
  // perch and claws
  [box(cx,36,12,1),'E'],
  [union(box(11,34,1.5,.6),box(19,34,1.5,.6)),'O'],
 ]);
 put(g,'C',[[9,10],[19,10]]);
 return str(g);
}

function basket(){
 const W=34,H=31;
 const leaves=(x0,y0,tips)=>union(...tips.map(([x,y])=>seg(x0,y0,x,y,1)));
 const g=raster(W,H,'.',[
  // two carrots leaning left with leafy tops
  [union(leaves(3,9.5,[[0.5,4],[3.5,3],[6,5]]),leaves(9.5,7,[[7,1.5],[10,0.5],[13,2.5]])),'G'],
  [union(poly([[0.5,10],[5.5,9],[8,18],[5,18.5]]),poly([[7,7],[12,7],[12.5,18],[9.5,18]])),'O'],
  // corn cob with its husk
  [ellipse(29.5,10,3,7.5),(x,y)=>((x+y)%2===0)?'Y':'C'],
  [poly([[25.5,18],[26.5,9],[29.5,15]]),'G'],[poly([[33.5,18],[33,8],[29.5,15]]),'M'],
  // pumpkin with ribs and stem
  [ellipse(20,12.5,6.8,6),(x,y)=>(x===16||x===23)?'A':'O'],
  [box(20,5.5,1,1.6),'M'],[box(22,5,1.6,.6),'M'],
  // basket: weave and rim
  [poly([[1,17],[33,17],[29,31],[5,31]]),(x,y)=>((Math.floor(x/2)+Math.floor((y-1)/2))%2)?'A':'E'],
  [box(17,17.5,16.5,1.5,.6),'E'],
 ]);
 return str(g);
}

function tent(){
 const W=44,H=34;
 const g=raster(W,H,'.',[
  // tent: front triangle, door, folded flap, pole tips
  [union(box(14.5,1,.7,1.5),box(17.5,1,.7,1.5)),'A'],
  [poly([[16,1.5],[-1,32],[33,32]]),'B'],
  [poly([[16,6],[10,32],[22,32]]),'D'],
  [poly([[16,6],[10,32],[4,32]]),'S'],
  // campfire: flames and crossed logs
  [union(ellipse(38,24.5,5,6),poly([[33,24],[38,10],[43,24]])),'R'],
  [union(ellipse(38,26,3.5,4.2),poly([[34.8,25],[38.5,15],[41.5,25]])),'O'],
  [union(ellipse(38,27.5,2,2.6),poly([[36.2,27],[37.6,20],[39.8,27]])),'Y'],
  [seg(33,31,43,27.5,1.2),'A'],[seg(33,27.5,43,31,1.2),'A'],
  // grass strip
  [box(21.5,33,22.5,1),'G'],
 ]);
 return str(g);
}

function globe(){
 const W=30,H=38,cx=15,cy=14,r=12;
 const sphere=circle(cx,cy,r);
 const land=union(ellipse(9.5,8,4.5,3.6),poly([[6,9],[12,9],[11,14],[8,12]]),ellipse(12.5,19,2.8,4.3),poly([[10.5,19],[14.5,19],[12.5,25.5]]),
  ellipse(20,7,3.5,2.6),ellipse(21,15,3.4,4.5),poly([[18.5,15],[23.5,15],[21,21.5]]),ellipse(24,7,2.5,2));
 const g=raster(W,H,'.',[
  [sphere,'B'],
  [cut(sphere,circle(cx-2.6,cy-2.6,r+.4)),'N'],
  [meet(land,sphere),'G'],
  // meridian ring on the left, pivots at the poles
  [meet(cut(circle(cx,cy,r+3.2),circle(cx,cy,r+1.2)),half((x,y)=>x<cx+1.2)),'Y'],
  [union(box(cx,cy-r-.5,1,1.5),box(cx,cy+r+.8,1,1.6)),'Y'],
  // stand: neck and base
  [box(cx,31,1.6,2.2),'A'],
  [box(cx,34.5,8,1.6,1.2),'A'],
  [box(cx,36.5,9,.9,.5),'E'],
 ]);
 return str(g);
}

function octopus(){
 const W=44,H=38;
 const T=[
  tube([[10,19],[5,21],[2,25],[2,30],[4,33],[7,32.5]],2.8,1.3),
  tube([[13,21],[11,26],[9.5,31],[10,35],[13,36]],2.8,1.3),
  tube([[17,22],[17,28],[17,33],[19,36],[21.5,35]],2.8,1.3),
  tube([[21,21],[24,26],[26,31],[27,35],[30,36.5],[32,35]],2.8,1.3),
  tube([[24,19],[29,22],[33,26],[35,31],[37.5,33.5],[40,32.5]],2.8,1.3),
  // raised tentacle gripping the baton
  tube([[24,17],[29,17.5],[33,15],[35,10],[34.5,6.5]],2.9,1.7),
 ];
 const head=union(ellipse(17,11,10,10),ellipse(17,17,8.5,5));
 const g=raster(W,H,'.',[
  ...T.map(t=>[t,'V']),
  [head,'V'],
  // suckers on the tentacles
  [union(...T.map(t=>(x,y)=>t(x,y)<-1.6?-1:1)),(x,y)=>((x+y)%3===0)?'P':'V'],
  // eyes
  [ellipse(13,12,2.5,3),'C'],[ellipse(21,12,2.5,3),'C'],
  [box(14,11,1,1),'D'],[box(22,11,1,1),'D'],
  [box(10.5,16,1.2,.6),'K'],[box(23.5,16,1.2,.6),'K'],
  // bow tie
  [union(poly([[11.5,18.5],[11.5,24],[17,21.2]]),poly([[22.5,18.5],[22.5,24],[17,21.2]])),'R'],
  [box(17,21.2,1.1,1.1),'D'],
 ]);
 // the baton: a two-cell white stick with a dark edge, pointing up right from the raised tip
 for(let y=0;y<=5;y++){const x=34+(6-y);put(g,'C',[[x,y],[x+1,y]]);}
 ring4(g,['C'],'D','.');
 return str(g);
}

export default {theme:'batch-0011',pictures:[
 {title:'Заводная сова',forLevel:21,family:'Птицы',direction:'Заводная сказка',subject:'сова из латуни и дерева с заводным ключом в спине',source:'оригинальный',rows:trim(owl())},
 {title:'Корзина урожая',forLevel:22,family:'Овощи и урожай',subject:'плетёная корзина с тыквой, морковью и початком кукурузы',source:'оригинальный',rows:trim(basket())},
 {title:'Палатка у костра',forLevel:23,family:'Походы и отдых на природе',subject:'треугольная палатка с открытым пологом рядом с маленьким костром',source:'оригинальный',rows:trim(tent())},
 {title:'Глобус',forLevel:24,family:'Книги, учёба и открытия',subject:'настольный глобус на подставке с материками и меридианным кольцом',source:'оригинальный',rows:trim(globe())},
 {title:'Осьминог-дирижёр',forLevel:25,family:'Морские и пресноводные обитатели',direction:'Обмен ролями',subject:'осьминог дирижирует палочкой, одно щупальце поднято',source:'оригинальный',rows:trim(octopus())},
]};

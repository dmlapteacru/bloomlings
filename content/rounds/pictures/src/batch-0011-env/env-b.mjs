// Batch 0011 with environment, part b: the clockwork owl under a night sky, the tent by the campfire in an evening meadow.
import {circle,ellipse,box,poly,union,cut,meet} from '../../../../../tools/gameplay-pilot-v9/draw.mjs';
import {raster,fit,str,put,ring4,scale,seg} from '../helpers.mjs';
// A shape drawn in the original picture's units, scaled by k and moved to (ox,oy) in the new picture.
const at=(k,ox,oy)=>s=>(x,y)=>s((x-ox)/k,(y-oy)/k)*k;
const all=()=>-1;
// A big five-cell star (a plus) centred on cell (x,y).
const star=(g,c,x,y)=>put(g,c,[[x,y],[x-1,y],[x+1,y],[x,y-1],[x,y+1]]);

function owl(){
 const W=31,H=30,k=.7,T=at(k,5.5,2.3),cx=15;
 const body=union(ellipse(cx,21.5,11,12.5),box(cx,11,11,8.5,5),poly([[4,7],[5.5,0.5],[11,4.5]]),poly([[26,7],[24.5,0.5],[19,4.5]]));
 const g=raster(W,H,'.',[
  // night sky over the whole picture, a big moon in the upper left
  [all,'N'],
  [circle(4.6,5.6,3.6),'C'],
  // wind-up key behind the right side: shaft and butterfly bow
  [T(box(28.5,21,3.5,1.1)),'O'],
  [T(union(ellipse(32.5,16.5,2.4,3.4),ellipse(32.5,25.5,2.4,3.4),box(32.5,21,1.2,2.5))),'Y'],
  [T(union(circle(32.5,16,.9),circle(32.5,26,.9))),'N'],
  // the branch across the picture, behind the owl's feet
  [union(poly([[-1,25.6],[24,26],[24,29],[-1,29]]),poly([[23,26],[29.2,26.6],[29.2,28.4],[23,29]]),circle(29,27.5,.95)),'E'],
  [T(body),'A'],
  // wings: dark wood
  [T(meet(ellipse(4.5,22.5,4.5,10),body)),'E'],[T(meet(ellipse(25.5,22.5,4.5,10),body)),'E'],
  // belly: brass plates with seams
  [T(ellipse(cx,24.5,7,8.5)),(x,y)=>(y%2===0)?'O':'Y'],
  // face disc, big eyes, beak
  [T(box(cx,11,10,6,4.5)),'O'],
  [T(circle(10,11,4.6)),'Y'],[T(circle(20,11,4.6)),'Y'],
  [T(circle(10,11,3.4)),'C'],[T(circle(20,11,3.4)),'C'],
  [T(circle(10,11,1.9)),'D'],[T(circle(20,11,1.9)),'D'],
  [T(poly([[13,14.5],[17,14.5],[15,18.2]])),'D'],
  // claws gripping the branch
  [T(union(box(11,34.3,1.6,.9),box(19,34.3,1.6,.9))),'O'],
 ]);
 // a few big stars
 star(g,'Y',16,1);star(g,'Y',2,16);star(g,'Y',27,4);
 return str(g);
}

function tent(){
 const W=32,H=28,T=at(.62,.6,4.16),F=at(.78,-2.54,.04);
 const g=raster(W,H,'.',[
  // evening sky above, dark night meadow below
  [all,'V'],
  [box(16,21.5,20,6.5),'M'],
  [circle(4.2,4.8,3.1),'C'],
  // tent: front triangle, door, folded flap (lit lining), pole tips
  [T(union(box(14.5,1,.9,1.6),box(17.5,1,.9,1.6))),'D'],
  [T(poly([[16,1.5],[-1,32],[33,32]])),'B'],
  [T(poly([[16,6],[10,32],[22,32]])),'D'],
  [T(poly([[16,6],[10,32],[4,32]])),'C'],
  // campfire: flames and crossed birch logs
  [F(union(ellipse(38,24.5,5,6),poly([[33,24],[38,10],[43,24]]))),'R'],
  [F(union(ellipse(38,26,3.5,4.2),poly([[34.8,25],[38.5,15],[41.5,25]]))),'O'],
  [F(union(ellipse(38,27.5,2,2.6),poly([[36.2,27],[37.6,20],[39.8,27]]))),'Y'],
  [F(seg(33,31,43,27.5,1.4)),'C'],[F(seg(33,27.5,43,31,1.4)),'C'],
 ]);
 star(g,'C',17,2);star(g,'C',22,7);star(g,'C',29,3);star(g,'C',2,12);
 return str(g);
}

export default {theme:'batch-0011-env',pictures:[
 {title:'Заводная сова',forLevel:21,family:'Птицы',direction:'Заводная сказка',subject:'сова из латуни и дерева с заводным ключом в спине',source:'оригинальный',rows:owl()},
 {title:'Палатка у костра',forLevel:23,family:'Походы и отдых на природе',subject:'треугольная палатка с открытым пологом рядом с маленьким костром',source:'оригинальный',rows:tent()},
]};

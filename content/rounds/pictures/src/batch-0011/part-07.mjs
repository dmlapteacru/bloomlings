// Batch 0011, part 07: yarn ball, astronaut on an asteroid, book tree, skating penguin, chocolate chip cookie.
import {circle,ellipse,box,poly,union,cut,meet} from '../../../../../tools/gameplay-pilot-v9/draw.mjs';
import {raster,fit,str,put,ring4,scale,seg} from '../helpers.mjs';
const draw=(w,h,k,layers)=>raster(Math.round(w*k),Math.round(h*k),'.',layers.map(([s,c])=>[scale(k,s),typeof c==='function'?(x,y,u)=>c(x/k,y/k,u):c]));

// 1. Ball of yarn with two needles and a loose thread (hand-drawn: curved strands, shade on the far side).
function yarn(){return [
 '....YYY.........YYY........',
 '....YYY.........YYY........',
 '......AA.......AA..........',
 '.......AA.....AA...........',
 '........AA...AA............',
 '.........AA.AA.............',
 '..........AAA..............',
 '..........AAA..............',
 '.........AA.AA.............',
 '........AABBBAA............',
 '.......BBSBBBBBBB..........',
 '.....BBBSBBBBBBBBN.........',
 '....BBBSBSBBBBBSSNN........',
 '....BBSBBBSSSSSBBNN........',
 '...BBBSBBBBBBBBBBBNN.......',
 '...BBSBBBBBBBBBBBBNN.......',
 '..BBBSSSBBBBBBBBBSSNN......',
 '..BBBSBBSSSSSSSSSBBNN......',
 '..BBBSBBBBBBBBBBBBBNN......',
 '..BBBSBBBBBBBBBBBBBNN......',
 '..BBBSSSBBBBBBBBBSSNN......',
 '...BBBSBSSSSSSSSSBNN.......',
 '...BBBSBBBBBBBBBBNNN.......',
 '....BBBSBSBBBBBSNNNBB......',
 '....BBBBSBSSSSSNNNNBB.....B',
 '.....BBBBSBBBNNNNN..BB...BB',
 '.......BBBNNNNNN.....BBBBB.',
 '.........NNNNN.............'];}

// 2. A small astronaut on a round asteroid, watering one flower with a can.
function astronaut(){
 const rock=union(ellipse(21,32,17,8.5),circle(12,28,6),circle(29,28.5,5.5));
 const craters=union(ellipse(9,31,2.2,1.4),ellipse(20,35,2.8,1.6),ellipse(31,32.5,2,1.3),ellipse(25,29.5,1.6,1));
 const helmet=circle(14,10,5.6),visor=ellipse(15.2,10.4,3.6,2.9);
 const torso=box(14,18.5,4.2,4.4,1.6),pack=box(8.6,17.5,2,4.2,0.8);
 const legs=union(box(12,24.2,1.6,2.6,0.6),box(16.2,24.2,1.6,2.6,0.6));
 const armR=union((x,y)=>Math.hypot(x-(17.5),y-16)-1.5,(x,y)=>{const ex=4,ey=-1.5,t=Math.max(0,Math.min(1,((x-17.5)*ex+(y-16)*ey)/(ex*ex+ey*ey)));return Math.hypot(x-17.5-ex*t,y-16-ey*t)-1.4;});
 const armL=(x,y)=>{const ex=-1.2,ey=5,t=Math.max(0,Math.min(1,((x-10.8)*ex+(y-16)*ey)/(ex*ex+ey*ey)));return Math.hypot(x-10.8-ex*t,y-16-ey*t)-1.35;};
 const can=box(24.5,14.5,2.8,2.4,0.8),handle=cut(circle(24.2,11.6,2.4),circle(24.2,11.6,1.2));
 const spout=(x,y)=>{const ex=4,ey=3.2,t=Math.max(0,Math.min(1,((x-26.5)*ex+(y-15)*ey)/(ex*ex+ey*ey)));return Math.hypot(x-26.5-ex*t,y-15-ey*t)-0.8;};
 const rose=circle(31,18.6,1.3);
 const drops=union(box(32,20.6,0.5,0.5),box(33.5,21.6,0.5,0.5),box(31.5,22.6,0.5,0.5));
 const stem=box(34,25.5,0.5,3),leaf=ellipse(36,26.2,1.6,0.8),petals=union(circle(34,22.2,2.6)),centre=circle(34,22.2,1.1);
 const g=raster(40,42,'.',[
  [rock,'A'],[cut(rock,ellipse(19.5,30.5,17,8.5)),'E'],[meet(rock,craters),'E'],
  [pack,'S'],[legs,'C'],[cut(legs,box(14.1,22.4,4,3.4)),'S'],[torso,'C'],[cut(torso,box(13.2,18.1,4.2,4.6,1.6)),'S'],
  [armL,'C'],[armR,'C'],
  [helmet,'C'],[cut(helmet,circle(13.3,9.3,5.6)),'S'],[visor,'N'],[box(13.6,8.9,0.5,0.5),'S'],[box(14.6,8.9,0.5,0.5),'S'],
  [box(14,18.5,1.5,1.2),'O'],
  [handle,'O'],[can,'O'],[spout,'O'],[rose,'O'],[drops,'S'],
  [stem,'G'],[leaf,'G'],[petals,'P'],[centre,'O'],
 ]);
 return str(g);
}

// 5. Round cookie with chocolate chips and a bite out of its upper right.
function cookie(){
 const cx=11,cy=11.5,r=10.4,bite=union(circle(20.6,7.0,3.4),circle(16.8,2.4,3.4));
 const body=cut(circle(cx,cy,r),bite);
 const chips=union(box(7,6,1,1),box(12,6,1,1),box(5,12,1,1),box(11,11,1,1),box(16,12,1,1),box(8,17,1,1),box(14,17,1,1),
  box(4,9,0.6,0.6),box(11,19,0.6,0.6),box(8,13,0.6,0.6),box(13,14,0.6,0.6));
 const g=raster(22,23,'.',[
  [body,'O'],
  [meet(body,cut(circle(cx,cy,r),circle(cx-1.4,cy-1.4,r))),'A'],
  [meet(body,cut(circle(cx,cy,r),circle(cx+1.2,cy+1.2,r))),'Y'],
  [meet(body,(x,y)=>bite(x,y)-1),'A'],
  [meet(body,chips),'E'],
 ]);
 return str(g);
}

export default {theme:'batch-0011',pictures:[
 {title:'Клубок с вязанием',forLevel:41,family:'Рисование и ремёсла',device:'простой предмет',subject:'клубок пряжи со спицами',source:'оригинальный',rows:yarn()},
 {title:'Космонавт поливает цветок на астероиде',forLevel:42,family:'Космос',direction:'Космическая повседневность',subject:'космонавт поливает цветок на астероиде',source:'оригинальный',rows:astronaut()},
 {title:'Печенье с шоколадом',forLevel:45,family:'Хлеб и выпечка',device:'простой предмет',subject:'печенье с шоколадной крошкой',source:'оригинальный',rows:cookie()},
]};

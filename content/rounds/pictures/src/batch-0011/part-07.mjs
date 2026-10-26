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
 const rock=ellipse(22,34.5,15,9.5);
 const craters=union(ellipse(13,32,2.2,1.3),ellipse(21,37.5,3,1.6),ellipse(28.5,31,1.8,1.1),ellipse(15,39,1.6,1));
 const helmet=circle(15,8,6),visor=ellipse(16.4,8.4,4,3.2);
 const torso=box(15,18.2,4.6,4.6,1.8),pack=box(8.6,17,2.2,4.6,0.9);
 const legs=union(box(12.8,24.4,1.8,2.6,0.6),box(17.2,24.4,1.8,2.6,0.6));
 const armR=union(seg(18.5,15.5,21.5,12.5,1.5),circle(22,12,1.6));
 const armL=seg(11,16,10.2,21.5,1.45);
 const can=box(24.4,10.6,2.8,2.5,0.8),handle=cut(circle(24.2,7.6,2.3),circle(24.2,7.6,1.1));
 const spout=seg(26.5,11.5,30,14.2,0.85),rose=circle(30.6,14.6,1.4);
 const drops=union(box(32,17,0.5,0.5),box(33.5,16.4,0.5,0.5),box(31,18.6,0.5,0.5),box(34.6,17.8,0.5,0.5));
 const stem=box(33,25,0.6,3),leaf=ellipse(35,25.8,1.8,0.9),petals=union(circle(33,21.2,2.9)),centre=circle(33,21.2,1.1);
 const g=draw(40,45,1.0,[
  [rock,'A'],[cut(rock,ellipse(20.6,33.2,15,9.5)),'E'],[meet(rock,craters),'E'],
  [pack,'S'],[legs,'C'],[cut(legs,box(15.5,23.4,5,3.6)),'S'],[torso,'C'],[cut(torso,box(14.2,17.6,4.6,4.8,1.8)),'S'],
  [armL,'C'],[armR,'C'],
  [helmet,'C'],[cut(helmet,circle(14.2,7.2,6)),'S'],[visor,'N'],[box(15,6.6,0.5,0.5),'S'],[box(16,6.6,0.5,0.5),'S'],[box(15,7.6,0.5,0.5),'S'],
  [box(15,18.4,1.6,1.2),'O'],
  [handle,'O'],[can,'O'],[spout,'O'],[rose,'O'],[drops,'S'],
  [stem,'G'],[leaf,'G'],[petals,'P'],[centre,'Y'],
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

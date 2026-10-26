// Batch 0011, part 4: levels 26–30 (cocoa mug, teapot rocket, hatching dino, acorn, house in a flower bud).
import {circle,ellipse,box,poly,union,cut,meet} from '../../../../../tools/gameplay-pilot-v9/draw.mjs';
import {raster,fit,str,put,ring4,scale,seg} from '../helpers.mjs';
const draw=(w,h,k,layers)=>raster(Math.round(w*k),Math.round(h*k),'.',layers.map(([s,c])=>[scale(k,s),typeof c==='function'?(x,y,u)=>c(x/k,y/k,u):c]));
const half=(f)=>(x,y)=>f(x,y)?-1:1; // a half-plane / predicate as a shape
const tr=(f,dx,dy)=>(x,y)=>f(x-dx,y-dy); // a shape moved by (dx, dy)

function mug(){
 return [
  '...........SS............',
  '..........SSS............',
  '..........SS.............',
  '..........SSS............',
  '...........SSS...........',
  '.....CCCCC..SS...........',
  '.....CCCCC..SS...........',
  '....BCCCCCBBBBBB.........',
  '..BBECCCCCEEEEEEBB.......',
  '..BEEEEEEEEEEEEEEB.......',
  '..BBEEEEEEEEEEEENN.......',
  '..BBBBEEEEEEEEBNNN.......',
  '..BSSBBBBBBBBBBNNNNNNN...',
  '..BSSBBBBBBBBBBNNNNNNNN..',
  '..BSSBBBBBBBBBBNNN...NN..',
  '..BSSBBBBBBBBBBNNN...NN..',
  '..BSSBBBBBBBBBBNNN...NN..',
  '..BSSBBBBBBBBBBNNN...NN..',
  '..BSSBBBBBBBBBBNNN...NN..',
  '..BBBBBBBBBBBBBNNNNNNNN..',
  '..BBBBBBBBBBBBBNNNNNNN...',
  '..BBBBBBBBBBBBBNNN.......',
  '..BBBBBBBBBBBBBNNN.......',
  '...BBBBBBBBBBBNNN........',
  '....NNNNNNNNNNNN.........',
 ];
}

function rocket(){
 const body=ellipse(22,21.5,14.3,13);
 const spout=union(seg(12,25,5.5,16,2.6),seg(5.5,16,3.5,11,2));
 const handle=meet(cut(ellipse(35.5,20.5,6.5,8),ellipse(35.5,20.5,3.3,4.6)),half((x)=>x>34));
 const finL=poly([[13,24],[3.5,34],[3.5,42],[14.5,33]]),finR=poly([[31,24],[40.5,34],[40.5,42],[29.5,33]]);
 const g=raster(44,44,'.',[
  [handle,'N'],[finL,'R'],[finR,'R'],
  [poly([[17,31.5],[27,31.5],[28.5,36],[15.5,36]]),'N'],
  [union(circle(22,38,4.5),poly([[17.5,38],[26.5,38],[22,44.5]])),'O'],
  [union(circle(22,37.5,2.6),poly([[19.4,37.5],[24.6,37.5],[22,42.5]])),'Y'],
  [spout,'B'],[meet(spout,half((x,y)=>y<12)),'R'],
  [body,'B'],
  [cut(body,tr(body,-1.8,-1.6)),'N'],
  [meet(cut(ellipse(22,21,12,10.5),ellipse(23.3,22.3,12,10.5)),half((x,y)=>x<18&&y<20)),'C'],
  [poly([[13,12.5],[31,12.5],[22,2]]),'R'],
  [circle(22,1.6,1.3),'Y'],
  [box(22,12.8,10.8,1.2,0.5),'N'],
  [circle(21,21,5.3),'Y'],[circle(21,21,3.7),'S'],
 ]);
 put(g,'C',[[19,19],[20,19],[19,20]]);
 return str(g);
}

function dino(){
 const zig=(x)=>22+Math.abs((x%6)-3)*1.0;
 const egg=ellipse(18,29.5,13.5,10.5);
 const neck=box(18,21,6.5,6,3.5);
 const head=union(ellipse(16.5,11,8.5,6.8),ellipse(23.5,13.5,7,4.8));
 const cap=poly([[7.5,7.5],[7.8,5],[9.5,3],[12.5,1.6],[16,1.5],[18.5,2.4],[20.3,4],[20.8,6.5],[19.2,7.5],[17.6,5.3],[16,7.8],[14.4,5.5],[12.8,8],[11.2,5.8],[9.6,8.3],[8.6,6.2]]);
 const g=raster(36,40,'.',[
  [meet(egg,half((x,y)=>y>zig(x)-2.6)),'Y'],
  [poly([[10,8],[4.5,9.5],[10,13]]),'M'],[poly([[9.5,13],[4,15.5],[12,18]]),'M'],[poly([[12,18.5],[6.5,21.5],[12,23.5]]),'M'],
  [neck,'G'],[head,'G'],
  [meet(neck,half((x,y)=>x>22.5&&y>18)),'M'],
  [ellipse(20.5,22,3.8,5),'L'],
  [meet(egg,half((x,y)=>y>zig(x))),'C'],
  [meet(egg,cut(half((x,y)=>y>zig(x)),tr(egg,-1.8,-1.4))),'Y'],
  [union(circle(10,29,2),circle(18.5,33.5,2.5),circle(26,28.5,2),circle(13,37,1.6),circle(24.5,36.5,1.7)),'O'],
  [union(ellipse(8.6,23.2,2.3,1.6),ellipse(25.8,23.2,2.3,1.6)),'G'],
  [cap,'C'],[union(circle(11.5,4,1.3),circle(16.5,3.8,1.2)),'O'],
 ]);
 put(g,'D',[[21,9],[22,9],[21,10],[22,10],[21,11],[22,11],[28,12],[21,16],[22,17],[23,17],[24,17],[25,17],[26,16]]);
 put(g,'C',[[21,9]]);
 return str(g);
}

function acorn(){
 return [
  '........EE..MGGG....',
  '.........EEGGMGGGG..',
  '.........EEGGGMMGGG.',
  '.........EE.GGGGMMG.',
  '.....AAEEEEAAEE.....',
  '...EEAAEEAAEEAAEE...',
  '..AAEEAAEEAAEEAAEE..',
  '.AAEEAAEEAAEEAAEEAA.',
  '.EEAAEEAAEEAAEEAAEE.',
  '.EEAAEEAAEEAAEEAAEE.',
  '..EEEEEEEEEEEEEEEE..',
  '...OOOOOOOOOOOAAA...',
  '...OOOOOOOOOOOAAA...',
  '...OOOOOOOOOOOAAA...',
  '...OOOOOOOOOOOAAA...',
  '...OOOOOOOOOOOAAA...',
  '...OOOOOOOOOOOAAA...',
  '....OOOOOOOOOOAA....',
  '.....OOOOOOOOAAA....',
  '......OOOOOOOAA.....',
  '.......OOOOOAA......',
  '........OOOAA.......',
  '.........EE.........',
 ];
}

function budHouse(){
 const stem=seg(22,30,22,44,1.6);
 const petalL=poly([[6.5,25],[3.8,14],[5.5,1.5],[11.5,7],[16.5,15],[18.5,24]]);
 const petalR=poly([[37.5,25],[40.2,14],[38.5,1.5],[32.5,7],[27.5,15],[25.5,24]]);
 const cup=meet(ellipse(22,23.5,16,8.5),half((x,y)=>y>23.5));
 const g=raster(44,44,'.',[
  [stem,'G'],[meet(stem,half((x)=>x>22.3)),'M'],
  [poly([[21,42],[14,40.5],[8,37],[2.5,30.5],[10,31.5],[16,35]]),'G'],[seg(20,41.2,7,32.5,0.55),'M'],
  [poly([[23,40],[29.5,37.5],[36,34],[41.5,28.5],[34,29.5],[27,33]]),'G'],[seg(24,39.3,38,30.5,0.55),'M'],
  [poly([[11,24],[15,9],[22,0.3],[29,9],[33,24]]),'K'],
  [petalL,'P'],[petalR,'P'],[cut(petalR,tr(petalR,-1.8,0)),'R'],
  [box(22,18.5,6.5,5,0.6),'C'],
  [poly([[13,14.5],[31,14.5],[22,6]]),'R'],
  [union(box(19.5,20.8,2,2.8,0),circle(19.5,18,2)),'A'],
  [circle(25.5,17.5,2.8),'A'],[circle(25.5,17.5,1.9),'S'],
  [cup,'P'],[cut(cup,tr(cup,-2,-1.5)),'R'],
 ]);
 put(g,'C',[[20,20]]);
 return str(g);
}

export default {theme:'batch-0011',pictures:[
 {title:'Кружка какао',forLevel:26,family:'Посуда и кухонные вещи',subject:'кружка какао с маршмеллоу',source:'оригинальный',rows:mug()},
 {title:'Чайник-ракета',forLevel:27,family:'Посуда и кухонные вещи',direction:'Предметные гибриды',subject:'чайник-ракета',source:'оригинальный',rows:rocket()},
 {title:'Динозаврик из яйца',forLevel:28,family:'Доисторический мир',subject:'динозаврик вылупляется из яйца',source:'оригинальный',rows:dino()},
 {title:'Жёлудь',forLevel:29,family:'Грибы и лесные находки',subject:'жёлудь с листком',source:'оригинальный',rows:acorn()},
 {title:'Дом в цветочном бутоне',forLevel:30,family:'Сказочные жилища',direction:'Живая архитектура',subject:'домик в бутоне тюльпана',source:'оригинальный',rows:budHouse()},
]};

// Batch 0061, part 09: L101–L105, cartoon universes (SpongeBob, Bill Cipher, BMO, Pickle Rick, a Toy Story alien).
import {circle,ellipse,box,poly,union,cut,meet} from '../../../../../tools/gameplay-pilot-v9/draw.mjs';
import {raster,fit,str,put,ring4,scale,seg} from '../helpers.mjs';
const draw=(w,h,k,layers)=>raster(Math.round(w*k),Math.round(h*k),'.',layers.map(([s,c])=>[scale(k,s),typeof c==='function'?(x,y,u)=>c(x/k,y/k,u):c]));
// Cell helpers: a filled rectangle (inclusive corners) and a stamp of hand rows ('_' keeps what is below).
const rect=(g,x0,y0,x1,y1,c)=>{for(let y=y0;y<=y1;y++)for(let x=x0;x<=x1;x++)g[y][x]=c;return g;};
const stamp=(g,x0,y0,rows)=>{rows.forEach((r,dy)=>[...r].forEach((c,dx)=>{if(c!=='_')g[y0+dy][x0+dx]=c;}));return g;};
const grid=(W,H,f)=>Array.from({length:H},(_,y)=>Array.from({length:W},(_,x)=>f(x,y)));

// 1. SpongeBob under water: porous yellow square, big blue eyes, buck teeth, white shirt, red tie, brown pants.
function sponge(){
 const W=30,H=34;
 const g=grid(W,H,(x,y)=>y>=31?'C':'B');
 // bubbles
 for(const [x,y] of [[2,3],[26,5],[3,13]])stamp(g,x,y,['_S_','S_S','_S_']);
 // arms: sleeves, thin arms, hands
 rect(g,5,18,6,19,'C');rect(g,23,18,24,19,'C');
 rect(g,5,20,6,23,'Y');rect(g,23,20,24,23,'Y');
 rect(g,4,23,6,24,'Y');rect(g,23,23,25,24,'Y');
 // body with a wavy sponge edge
 rect(g,7,3,22,19,'Y');
 for(const x of [7,12,17,22])g[3][x]='B';
 for(const y of [8,14])g[y][7]='B';
 for(const y of [5,11,17])g[y][22]='B';
 // pores
 for(const [x,y] of [[8,4],[19,3],[20,15],[8,16],[16,17]])stamp(g,x,y,['LL','LL']);
 for(const [x,y] of [[21,9],[8,10]])stamp(g,x,y,['L','L']);
 // eyes with lashes
 const eye=['__CCC__','_CCCCC_','CCBBBCC','CCBDBCC','CCBBBCC','_CCCCC_','__CCC__'];
 stamp(g,8,5,eye);stamp(g,15,5,eye);
 put(g,'D',[[9,3],[9,4],[11,3],[11,4],[13,4],[16,4],[18,3],[18,4],[20,3],[20,4]]);
 // nose, cheeks, smile, buck teeth
 put(g,'L',[[14,12],[15,12]]);
 stamp(g,8,13,['KK__________KK','KK__________KK']);
 stamp(g,9,13,[
  'D__________D',
  '_D________D_',
  '__DDDDDDDD__',
  '___CC__CC___',
  '___CC__CC___']);
 // shirt, collar, tie
 rect(g,7,18,22,19,'C');
 rect(g,14,18,15,18,'R');rect(g,13,19,16,21,'R');rect(g,14,22,15,22,'R');
 put(g,'Y',[[12,18],[17,18]]);
 // pants and belt
 rect(g,7,20,22,24,'A');
 for(const x of [8,11,17,20])rect(g,x,21,x+1,21,'D');
 rect(g,13,20,16,21,'R');rect(g,14,22,15,22,'R');
 rect(g,9,25,12,25,'A');rect(g,17,25,20,25,'A');
 // legs, socks, shoes
 rect(g,10,26,11,26,'Y');rect(g,18,26,19,26,'Y');
 rect(g,10,27,11,28,'C');rect(g,18,27,19,28,'C');
 rect(g,10,28,11,28,'R');rect(g,18,28,19,28,'R');
 rect(g,9,29,12,30,'D');rect(g,7,30,8,30,'D');
 rect(g,17,29,20,30,'D');rect(g,21,30,22,30,'D');
 return str(g);}

// 2. Bill Cipher: a yellow triangle with one eye, brick lower part, top hat, bow tie, thin black arms and legs.
function bill(){
 const W=38,H=42,top=10,base=36;
 const tri=poly([[19,top],[37,base],[1,base]]);
 const inner=poly([[19,top+2.2],[35.3,base-1],[2.7,base-1]]);
 const eyeO=meet(circle(19,25.5,8.5),circle(19,14.5,8.5));
 const eyeI=meet(circle(19,25.5,7.3),circle(19,14.5,7.3));
 const bricks=(x,y)=>{const yy=Math.floor(y),xx=Math.floor(x);if((yy-27)%3===0)return 'A';const row=Math.floor((yy-27)/3);return (xx+row*2)%4===0?'A':'O';};
 const g=draw(W,H,1,[
  [union(seg(9,26,3,20,1),seg(29,26,35,20,1)),'D'],                 // arms
  [union(circle(3,19.5,1.6),circle(35,19.5,1.6)),'D'],               // hands
  [union(seg(15,36,14,40,1),seg(23,36,24,40,1)),'D'],                // legs
  [union(box(12.5,40.5,2.5,1,.5),box(25.5,40.5,2.5,1,.5)),'D'],      // feet
  [tri,'D'],[inner,'Y'],
  [meet(inner,box(19,32,20,5)),bricks],
  [eyeO,'D'],[eyeI,'C'],[box(19,20,1,3),'D'],
  [union(poly([[19,27],[14,25],[14,29.5]]),poly([[19,27],[24,25],[24,29.5]])),'D'],  // bow tie
  [box(19,4.5,4,4.5),'D'],[box(19,9.3,7,.8),'D'],                    // top hat
 ]);
 return str(g);}

// 3. BMO: the teal console robot with a smiling screen face, a yellow cross pad, coloured buttons, little arms and legs.
function bmo(){
 const W=30,H=40;
 const g=grid(W,H,()=>'.');
 // arms and legs
 rect(g,2,15,4,16,'B');rect(g,1,17,2,21,'B');rect(g,0,22,2,23,'B');
 rect(g,25,15,27,16,'B');rect(g,27,17,28,21,'B');rect(g,27,22,29,23,'B');
 rect(g,9,31,10,35,'B');rect(g,19,31,20,35,'B');
 rect(g,7,36,10,37,'N');rect(g,19,36,22,37,'N');
 // body box with a shaded right side
 rect(g,5,1,24,30,'B');rect(g,23,2,24,30,'N');rect(g,6,30,24,30,'N');
 g[1][5]='.';g[1][24]='.';g[30][5]='.';g[30][24]='.';g[1][23]='B';
 // screen and face
 rect(g,7,3,21,14,'L');
 g[3][7]='B';g[3][21]='B';g[14][7]='B';g[14][21]='B';
 rect(g,10,6,11,7,'D');rect(g,17,6,18,7,'D');
 stamp(g,10,10,['DDDDDDDDD','_DDDGGDD_','__DDGDD__']);
 // cartridge slot and a small round button
 rect(g,7,16,15,16,'D');rect(g,19,16,20,17,'N');
 // cross pad
 rect(g,7,21,13,22,'Y');rect(g,9,19,11,24,'Y');rect(g,10,19,10,24,'Y');
 rect(g,7,21,8,22,'Y');
 // triangle, small green and big red buttons
 stamp(g,15,20,['_N_','NNN']);
 stamp(g,19,19,['GG','GG']);
 stamp(g,16,23,['_RRR_','RRRRR','RRRRR','_RRR_']);
 return str(g);}

// 4. Pickle Rick: a bumpy green pickle lying diagonally, Rick's face with a unibrow and a wide toothy grin.
function pickle(){
 const W=40,H=36;
 const body=seg(9,27,31,9,8.3);
 const lit=cut(body,seg(10.6,28.6,32.6,10.6,8.3));
 const dark=cut(body,seg(7.6,25.4,29.6,7.4,8.3));
 const bumps=(x,y)=>{const xx=Math.floor(x),yy=Math.floor(y);return ((xx+2*yy)%7===0&&yy%3===1)?-1:1;};
 const g=draw(W,H,1,[
  [body,'G'],[meet(body,bumps),'M'],[lit,'M'],[dark,'L'],
 ]);
 // face, upright, in the upper part of the pickle
 stamp(g,17,6,[
  '_DDD______DDD_',
  'DDDDDD__DDDDDD',
  '___DDDDDDDD___',
 ]);
 stamp(g,17,9,[
  '_CCCC___CCCC_',
  'CCCCCC_CCCCCC',
  'CCCDDC_CDDCCC',
  'CCCDDC_CDDCCC',
  'CCCCCC_CCCCCC',
  '_CCCC___CCCC_',
 ]);
 stamp(g,15,17,[
  'D______________D',
  'DDDDDDDDDDDDDDDD',
  '_DCCDCCDCCDCCDD_',
  '__DDDDDDDDDDDD__',
  '___DCCDCCDCCD___',
  '____DDDDDDDD____',
 ]);
 return str(g);}

// 5. A three-eyed green toy alien with an antenna, in a blue suit with a red planet emblem.
function alien(){
 const k=1.12;
 const head=union(ellipse(15,13,11,7.6),poly([[10,8],[20,8],[15,2.5]]));
 const suit=box(15,26.5,7.5,6,2.5);
 const eye=(cx,cy)=>ellipse(cx,cy,2.7,3);
 const g=draw(30,39,k,[
  [box(15,3,.9,3),'G'],[circle(15,1.8,1.7),'G'],                     // antenna
  [union(seg(8.5,22.5,4,29,1.6),seg(21.5,22.5,26,29,1.6)),'B'],       // sleeves
  [union(circle(3.6,30,2),circle(26.4,30,2)),'G'],                     // hands
  [union(box(11.5,34.5,2.2,2.5),box(18.5,34.5,2.2,2.5)),'B'],          // legs
  [union(box(11,37.4,3,1.3,.6),box(19,37.4,3,1.3,.6)),'N'],             // boots
  [suit,'B'],[meet(suit,box(20.5,26.5,2,7)),'N'],[box(15,32,7.5,.7),'N'],
  [box(15,20.6,6,1.3,.6),'N'],                                         // collar
  [circle(13,26.5,2.6),'R'],[meet(cut(ellipse(13,26.5,4.3,1.3),ellipse(13,26.5,3,.6)),(x,y)=>y>26.3||Math.abs(x-13)>2.4?-1:1),'C'],  // planet emblem with a ring
  [head,'G'],[meet(head,cut(ellipse(15,13,11,7.6),ellipse(14.2,12.4,10.4,7.2))),'M'],
  [union(eye(8.6,12),eye(15,10.4),eye(21.4,12)),'C'],
  [union(box(9.2,12.5,1,1.2),box(15.6,10.9,1,1.2),box(22,12.5,1,1.2)),'D'],
  [meet(cut(circle(15,13,5.6),circle(15,12.2,5.2)),box(15,18,3.6,1.5)),'D'],   // smile
 ]);
 return str(g);}

const REF='сторонняя вселенная';
export default {theme:'batch-0061',pictures:[
 {title:'Губка Боб',forLevel:101,family:'Морские и пресноводные обитатели',subject:'Губка Боб под водой: жёлтая губка, рубашка, галстук, штаны',source:REF,reference:'Губка Боб Квадратные Штаны — SpongeBob',rows:sponge()},
 {title:'Билл Шифр',forLevel:102,family:'Волшебные существа',subject:'жёлтый треугольник с глазом, цилиндром и бабочкой',source:REF,reference:'Гравити Фолз — Bill Cipher',rows:bill()},
 {title:'БиМО',forLevel:103,family:'Роботы и механизмы',subject:'робот-консоль с лицом на экране и кнопками',source:REF,reference:'Время приключений — BMO',rows:bmo()},
 {title:'Огурчик Рик',forLevel:104,family:'Овощи и урожай',subject:'огурец с лицом Рика: моно-бровь и широкая ухмылка',source:REF,reference:'Рик и Морти — Pickle Rick',rows:pickle()},
 {title:'Инопланетянин из «Истории игрушек»',forLevel:105,family:'Игрушки и настольные игры',subject:'трёхглазый зелёный инопланетянин с антенной в синем костюме',source:REF,reference:'История игрушек — Little Green Men (Aliens)',rows:alien()},
]};

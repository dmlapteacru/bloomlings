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
 const W=30,H=36;
 const g=grid(W,H,(x,y)=>y>=33?'C':'B');
 for(const [x,y] of [[0,6],[27,11]])stamp(g,x,y,['_S_','S_S','_S_']);   // bubbles
 // arms: sleeves, thin arms, hands
 rect(g,2,19,3,20,'C');rect(g,26,19,27,20,'C');
 rect(g,2,21,3,25,'Y');rect(g,26,21,27,25,'Y');
 rect(g,1,25,3,26,'Y');rect(g,26,25,28,26,'Y');
 // body with a wavy sponge edge
 rect(g,4,2,25,20,'Y');
 for(const x of [4,25])g[2][x]='B';
 for(const y of [7,13])g[y][4]='B';
 for(const y of [5,10,16])g[y][25]='B';
 // pores
 for(const [x,y] of [[5,3],[22,9],[5,15],[22,15]])stamp(g,x,y,['LL','LL','LL']);
 // eyes with lashes
 const eye=['__CCC__','_CCCCC_','CCBBBCC','CCBDBCC','CCBBBCC','_CCCCC_','__CCC__'];
 stamp(g,8,5,eye);stamp(g,15,5,eye);
 put(g,'D',[[9,3],[9,4],[11,3],[11,4],[13,4],[16,4],[18,3],[18,4],[20,3],[20,4]]);
 // nose, cheeks, smile, buck teeth
 put(g,'L',[[14,12],[15,12]]);
 stamp(g,6,13,['KK______________KK','KK______________KK']);
 stamp(g,8,14,[
  'D____________D',
  '_D__________D_',
  '__DDDDDDDDDD__',
  '____CC__CC____',
  '____CC__CC____']);
 // shirt and tie
 rect(g,4,19,25,20,'C');
 // pants and belt
 rect(g,4,21,25,26,'A');
 for(const x of [5,8,11,17,20,23])rect(g,x,22,x+1,22,'D');
 rect(g,14,19,15,19,'R');rect(g,13,20,16,22,'R');rect(g,14,23,15,23,'R');
 put(g,'Y',[[12,19],[17,19]]);
 rect(g,7,27,11,27,'A');rect(g,18,27,22,27,'A');
 // legs, socks, shoes
 rect(g,9,28,10,28,'Y');rect(g,19,28,20,28,'Y');
 rect(g,9,29,10,29,'C');rect(g,19,29,20,29,'C');
 rect(g,9,30,10,30,'R');rect(g,19,30,20,30,'R');
 rect(g,8,31,11,32,'D');rect(g,6,32,7,32,'D');
 rect(g,18,31,21,32,'D');rect(g,22,32,23,32,'D');
 return str(g);}

// 2. Bill Cipher: a yellow triangle with one eye, brick lower part, top hat, bow tie, thin black arms and legs.
function bill(){
 const W=40,H=44,top=8,base=37;
 const tri=poly([[20,top],[37.5,base],[2.5,base]]);
 const eyeO=meet(circle(20,26.7,8.2),circle(20,19.3,8.2));
 const eyeI=meet(circle(20,26.7,7.1),circle(20,19.3,7.1));
 const bricks=(x,y)=>{const yy=Math.floor(y),xx=Math.floor(x);if((yy-31)%3===0)return 'A';const row=Math.floor((yy-31)/3);return (xx+row*2)%4===0?'A':'O';};
 const g=draw(W,H,1,[
  [union(seg(10,25,3,19,1),seg(30,25,37,19,1)),'D'],                 // arms
  [union(circle(3,18.5,1.7),circle(37,18.5,1.7)),'D'],               // hands
  [union(seg(15,37,14,41,1),seg(25,37,26,41,1)),'D'],                // legs
  [union(box(12.5,41.8,2.5,1,.5),box(27.5,41.8,2.5,1,.5)),'D'],      // feet
  [tri,'Y'],
  [meet(tri,box(20,34.5,20,3.5)),bricks],
 ]);
 ring4(g,['Y','O','A'],'D','.');
 const g2=raster(W,H,'.',[[eyeO,'D'],[eyeI,'C'],[box(20,23,1,3),'D'],
  [union(poly([[20,30.5],[14.2,28],[14.2,33]]),poly([[20,30.5],[25.8,28],[25.8,33]]),box(20,30.5,1.2,1.2)),'D'],   // bow tie
  [box(20,3.5,4,3.5),'D'],[box(20,7.2,7,.8),'D']]);                // top hat
 g2.forEach((r,y)=>r.forEach((c,x)=>{if(c!=='.')g[y][x]=c;}));
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
 // widen: repeat the middle column and the row under the screen
 g.forEach(r=>r.splice(14,0,r[14]));g.splice(15,0,[...g[15]]);
 return str(g);}

// 4. Pickle Rick: a bumpy green pickle lying diagonally, Rick's face with a unibrow and a wide toothy grin.
function pickle(){
 const W=40,H=36;
 const body=seg(9,27,31,9,8.3);
 const lit=cut(body,seg(10.6,28.6,32.6,10.6,8.3));
 const dark=cut(body,seg(7.6,25.4,29.6,7.4,8.3));
 const bumps=(x,y)=>{const xx=Math.floor(x),yy=Math.floor(y);return ((xx+2*yy)%7===0&&yy%3===1)?-1:1;};
 const g=draw(W,H,1,[
  [body,'G'],[meet(body,bumps),'M'],[lit,'L'],[dark,'M'],
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
  [suit,'B'],[meet(suit,box(21.6,26.5,1.2,7)),'N'],[box(15,32,7.5,.7),'N'],
  [box(15,20.6,6,1.3,.6),'N'],                                         // collar
  [circle(15,26.5,2.8),'R'],[box(15,26.5,4.6,.5),'C'],  // planet emblem with a ring
  [head,'G'],
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

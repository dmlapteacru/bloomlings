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
const grow=(f,d)=>(x,y)=>f(x,y)-d;
function astronaut(){
 const rock=union(ellipse(22,33.5,16,9.2),circle(9.8,31,4.6),circle(34.3,31.5,4.3),circle(22,38,6.5));
 const craterAt=(cx,cy,rx,ry)=>[[ellipse(cx,cy,rx,ry),'E']];
 const helmet=circle(15,7,6),visor=ellipse(16.4,7.4,4,3.2);
 const torso=box(15,17.2,4.6,4.6,1.8),pack=box(8.8,16,2.4,4.6,0.9);
 const legL=box(12.6,23.4,1.9,2.6,0.6),legR=box(17,23.4,1.9,2.6,0.6);
 const armR=union(seg(18.5,14.5,21.5,11.5,1.5),circle(22,11,1.6));
 const can=box(24.4,9.6,2.8,2.5,0.8),handle=cut(circle(24.2,6.6,2.3),circle(24.2,6.6,1.1));
 const spout=seg(26.5,10.5,30,13.2,0.85),rose=circle(30.6,13.6,1.4);
 const suit=part=>[[grow(part,1),'D'],[part,'C']];
 const g=draw(40,44,1.0,[
  [rock,'A'],[cut(rock,union(ellipse(20.2,31.8,16,9.2),circle(8.2,29.8,4.6),circle(32.6,30,4.3),circle(20.5,36.4,6.5))),'E'],
  ...craterAt(13,31.5,2.6,1.6),...craterAt(24.5,36.5,3,1.8),...craterAt(29.5,29.8,1.9,1.2),...craterAt(15.5,38,1.5,1.1),
  ...suit(pack),...suit(legL),...suit(legR),...suit(torso),...suit(armR),
  [box(15,17.4,1.6,1.2),'O'],
  ...suit(helmet),[visor,'D'],
  [handle,'O'],[can,'O'],[spout,'O'],[rose,'O'],
 ]);
 // the flower, by hand: four round petals round an orange heart, on a two-cell stem with a leaf
 const flower=['.PP.PP.','PPPPPPP','PPOOOPP','.POOOP.','PPOOOPP','PPPPPPP','.PP.PP.','...GG..','...GGGG','..GGGG.','...GG..','...GG..'];
 flower.forEach((r,j)=>[...r].forEach((c,i)=>{if(c!=='.')g[17+j][30+i]=c;}));
 put(g,'B',[[15,5],[16,5],[15,6]]);
 put(g,'B',[[32,14],[32,15],[34,15],[34,16]]);
 return str(g);
}

// 3. An open book with a small round leafy tree growing out of its pages.
function bookTree(){
 const sx=21.5;                                 // the spine (between columns 21 and 22)
 const T=x=>Math.min(1,Math.abs(x-sx)/19.5);
 const top=x=>{const t=T(x);return 33.4-6.2*Math.sin(t*Math.PI*0.62)/Math.sin(Math.PI*0.62)+0.3*t;},bot=x=>39.6-2.4*T(x);
 const inBook=(x,w)=>Math.abs(x-sx)<w;
 const pages=(x,y)=>inBook(x,19.6)?Math.max(top(x)-y,y-bot(x)):1;
 const edges=(x,y)=>inBook(x,19.6)?Math.max(bot(x)-y,y-(bot(x)+1.1)):1;
 const cover=(x,y)=>inBook(x,20.6)?Math.max(top(x)+1.5-y,y-(bot(x)+2.7)):1;
 const fold=(x,y)=>Math.abs(x-sx)<0.6&&y>top(x)?-1:1;
 const crown=ellipse(sx,12.2,13.4,12);
 const stem=poly([[sx-2.2,21],[sx+2.2,21],[sx+2.2,30],[sx+4.6,33.8],[sx-4.6,33.8],[sx-2.2,30]]);
 const g=raster(44,44,'.',[
  [cover,'N'],[pages,'C'],[edges,'A'],[fold,'A'],
  [stem,'E'],[meet(stem,(x,y)=>x-(sx-0.6)),'A'],
  [crown,'G'],[cut(crown,ellipse(sx-1.8,10.4,13.4,12)),'M'],[meet(crown,union(circle(sx-6.5,5.8,2.5),circle(sx-9.5,11,1.6),circle(sx-1.5,2.8,1.6))),'L'],
 ]);
 const apples=[[17,9],[28,6],[25,15],[12,16],[31,13],[20,20]];
 for(const [x,y] of apples)put(g,'R',[[x,y],[x+1,y],[x,y+1],[x+1,y+1]]);
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
 {title:'Книга выращивает дерево',forLevel:43,family:'Книги, учёба и открытия',direction:'Домашняя магия',subject:'из страниц раскрытой книги растёт дерево',source:'оригинальный',rows:bookTree()},
 {title:'Печенье с шоколадом',forLevel:45,family:'Хлеб и выпечка',device:'простой предмет',subject:'печенье с шоколадной крошкой',source:'оригинальный',rows:cookie()},
]};

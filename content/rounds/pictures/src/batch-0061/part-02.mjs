// Batch 0061, part 02: levels 66–70 (croissant, birthday cake with candles, cosy bookshelf, firefly lantern at night, boot with a flower)
import {circle,ellipse,box,poly,union,cut,meet} from '../../../../../tools/gameplay-pilot-v9/draw.mjs';
import {raster,fit,str,put,ring4,scale} from '../helpers.mjs';
// crop the empty rows and columns round the subject
const trim=rows=>{const r=rows.filter(l=>/[^.]/.test(l)),xs=r.flatMap(l=>[...l].map((c,i)=>c==='.'?-1:i).filter(i=>i>=0)),a=Math.min(...xs),b=Math.max(...xs);return r.map(l=>l.slice(a,b+1));};
const draw=(w,h,k,layers)=>raster(Math.round(w*k),Math.round(h*k),'.',layers.map(([s,c])=>[scale(k,s),typeof c==='function'?(x,y,u)=>c(x/k,y/k,u):c]));
const grow=(s,d)=>(x,y)=>s(x,y)-d;
const mv=(dx,dy,f)=>(x,y)=>f(x-dx,y-dy);
// an ellipse turned by angle a (radians) round its centre
const rot=(cx,cy,a,rx,ry)=>(x,y)=>{const dx=x-cx,dy=y-cy,c=Math.cos(a),s=Math.sin(a);return ellipse(0,0,rx,ry)(dx*c+dy*s,-dx*s+dy*c);};
const grid=(W,H,c='.')=>Array.from({length:H},()=>Array(W).fill(c));
const rect=(g,x0,y0,x1,y1,c)=>{for(let y=y0;y<=y1;y++)for(let x=x0;x<=x1;x++)if(g[y]&&x>=0&&x<g[0].length)g[y][x]=c;return g;};

function croissant(){
 const segs=[ellipse(3.6,15.2,2.8,2.4),ellipse(30.4,15.2,2.8,2.4),
  ellipse(7.2,11.6,3.6,4),ellipse(26.8,11.6,3.6,4),
  ellipse(11.6,8.6,4.2,5),ellipse(22.4,8.6,4.2,5),
  ellipse(17,7.6,4.6,6)];
 const layers=[];
 for(const s of segs){
  layers.push([grow(s,1),'E'],[s,'O'],[meet(s,mv(0,3,s)),'A'],[meet(grow(s,-1),mv(-.5,-3,s)),'Y']);}
 const g=draw(34,21,1,layers);
 put(g,'C',[[15,4],[16,4],[15,5]]);
 return trim(str(g));}

function cake(){
 const plate=box(20,38.2,18,1.1,1);
 const low=box(20,30.5,15,6.5,1),lowShade=meet(low,box(7,30.5,2,8));
 const top=box(20,18.5,11,5.5,1),topShade=meet(top,box(11,18.5,2,6));
 const drips=(y,xs)=>union(...xs.map(([x,l])=>box(x,y+l/2,1,l/2+.3,1)));
 const lowIce=union(box(20,25,15.4,1.5,1),drips(25,[[7,4.5],[11.5,2.5],[16,5],[21,3],[26,5],[30.5,3]]));
 const topIce=union(box(20,13.6,11.4,1.5,1),drips(13.6,[[12,4.5],[16.5,2.5],[21,5],[25.5,3]]));
 const bx=[[7.5,22.3],[12,22.3],[28,22.3],[32.5,22.3],[11,10.8],[29,10.8]];
 const berries=union(...bx.map(([x,y])=>circle(x,y,2)));
 const cand=[15,20,25];
 const candles=union(...cand.map(x=>box(x,9,1,4)));
 const flames=union(...cand.map(x=>union(circle(x,3.9,1.7),poly([[x-1.3,3.6],[x,0],[x+1.3,3.6]]))));
 const g=draw(40,40,1.07,[[plate,'B'],[low,'P'],[lowShade,'K'],[top,'P'],[topShade,'K'],[lowIce,'C'],[topIce,'C'],[berries,'R'],
  [candles,(x,y)=>Math.floor(y*1.07)%3===0?'C':'B'],[flames,'Y']]);
 const k=1.07;
 for(const x of cand){const c=Math.round(x*k);put(g,'O',[[c-1,4],[c,4],[c-1,5],[c,5]]);put(g,'D',[[c-1,6],[c,6]]);}
 for(const [x,y] of bx)put(g,'C',[[Math.round(x*k)-1,Math.round(y*k)-1]]);
 return trim(str(g));}

function bookshelf(){
 const W=34,H=41,g=grid(W,H);
 const X0=1,X1=32; // shelf body columns
 // plant on top (rows 0..9), body rows 10..38, feet 39..40
 rect(g,X0,10,X1,38,'A');
 rect(g,X0-1,10,X1+1,11,'A');
 const comps=[[12,18],[21,27],[30,36]]; // inner rows; boards between
 for(const [a,b] of comps)rect(g,X0+2,a,X1-2,b,'E');
 rect(g,X0+1,39,X0+3,40,'A');rect(g,X1-3,39,X1-1,40,'A');
 // books: [x0,width,height,colour] standing on the compartment floor
 const book=(fl,x,w,h,c,band='C')=>{rect(g,x,fl-h+1,x+w-1,fl,c);rect(g,x,fl-h+2,x+w-1,fl-h+2,band);};
 // top compartment
 let fl=18;
 book(fl,3,3,6,'R');book(fl,6,2,7,'B','Y');book(fl,8,3,5,'Y','R');book(fl,11,2,6,'V');book(fl,13,3,7,'G','C');
 // leaning book
 for(let i=0;i<7;i++){const x=20-Math.round(i*0.6);rect(g,x,fl-i,x+1,fl-i,'B');}
 book(fl,24,2,6,'R','Y');book(fl,26,3,7,'V','C');book(fl,29,2,5,'Y','B');
 // middle compartment: books and a clock
 fl=27;
 book(fl,3,2,6,'Y','R');book(fl,5,3,7,'G');book(fl,8,2,5,'R','C');book(fl,10,3,6,'B','Y');book(fl,13,2,7,'V','C');book(fl,15,2,5,'Y','V');
 const clk=raster(9,7,'.',[[circle(4.5,3.4,3.6),'Y'],[circle(4.5,3.4,2.3),'C']]);
 clk.forEach((r,y)=>r.forEach((c,x)=>{if(c!=='.')g[21+y][20+x]=c;}));
 put(g,'E',[[24,22],[24,23],[24,24],[25,24],[26,24]]);
 // bottom compartment: a lying stack and standing books
 fl=36;
 rect(g,3,34,13,36,'B');rect(g,3,35,13,35,'C');
 rect(g,4,31,12,33,'R');rect(g,4,32,12,32,'C');
 rect(g,5,30,11,30,'Y');
 book(fl,16,3,7,'V','Y');book(fl,19,2,6,'G','C');book(fl,21,3,5,'Y','R');book(fl,24,2,7,'R','C');book(fl,26,3,6,'B','Y');book(fl,29,2,7,'G','Y');
 // plant in a pot on the top board
 const lv=[rot(7,3.4,0,1.7,3.4),rot(3.6,4.6,-1.0,1.5,3.4),rot(10.4,4.6,1.0,1.5,3.4)];
 const plant=raster(14,10,'.',[...lv.map(l=>[l,'G']),[meet(lv[0],box(8.5,3,1,4)),'M'],[meet(lv[1],box(3,7,4,1.6)),'M'],[meet(lv[2],box(11,7,4,1.6)),'M'],
  [poly([[3.2,6],[10.8,6],[9.8,10],[4.2,10]]),'R'],[box(7,6.6,4.2,.7),'R']]);
 plant.forEach((r,y)=>r.forEach((c,x)=>{if(c!=='.')g[y][x+2]=c;}));
 return trim(str(g));}

function lantern(){
 const W=28,H=32;
 const handle=cut(circle(14,5.2,3.8),circle(14,5.2,2.2));
 const cap=union(meet(ellipse(14,11,6.3,3.6),box(14,8,8,3)),box(14,7.2,1.2,1));
 const lid=box(14,11.5,7.5,1,0.5);
 const glass=box(14,18.5,6,6);
 const frameL=box(7.4,18.5,1.2,6.2),frameR=box(20.6,18.5,1.2,6.2);
 const base=union(box(14,25.5,7.5,1.1,0.5),poly([[9,26.5],[19,26.5],[17.5,28.6],[10.5,28.6]]));
 const ground=(x,y)=>y>28.6+(Math.floor(x)%7<3?0:-0.6)?-1:1;
 const g=raster(W,H,'.',[[box(14,16,20,20),'N'],[ground,'M'],[handle,'A'],[cap,'A'],[meet(cap,box(19,9,2.2,3)),'E'],[glass,'B'],[lid,'A'],[frameL,'A'],[frameR,'E'],[base,'A'],[meet(base,box(19,27,2.2,3)),'E']]);
 // a firefly: Y heart with an L halo
 const fly=(x,y,big)=>{put(g,'L',[[x-1,y],[x+1+(big?1:0),y],[x,y-1],[x,y+1+(big?1:0)]].concat(big?[[x+1,y-1],[x-1,y+1],[x+2,y+1],[x+1,y+2]]:[]));put(g,'Y',big?[[x,y],[x+1,y],[x,y+1],[x+1,y+1]]:[[x,y]]);};
 fly(11,15,true);fly(16,19,true);fly(11,21,false);fly(17,15,false);
 fly(3,4,false);fly(24,4,false);fly(3,12,false);fly(2,21,false);fly(25,18,false);fly(24,11,false);fly(20,1,false);
 return str(g);}

function boot(){
 const shaft=box(9,19.5,5.5,8.5,1);
 const toe=union(box(12,27.4,7.5,1.1,.6),ellipse(14.6,25.8,6.4,3.2),ellipse(17.5,26.2,3.5,2.6));
 const body=union(shaft,toe);
 const shade=meet(shaft,box(14,18.5,1.5,7.2));
 const toeShade=meet(toe,box(12,28,10,.8));
 const rim=box(9,11.8,6,1.2,0.6);
 const soil=box(9,10.8,4.4,0.6);
 const sole=box(12,29.4,9,1,0.5);
 const stem=box(9,8,0.95,5.5);
 const leafL=rot(5.8,9.2,-1.05,1.5,3.3),leafR=rot(12.2,8.8,1.05,1.5,3.3);
 const petals=union(...[0,1,2,3,4].map(i=>circle(9+2.8*Math.sin(i*1.2566),3.8-2.8*Math.cos(i*1.2566),2.1)));
 const low=f=>mv(0,2,f);
 const g=draw(22,33,1.03,[[low(body),'Y'],[low(shade),'O'],[low(toeShade),'O'],[low(sole),'E'],[low(rim),'O'],[low(soil),'E'],[stem,'G'],[leafL,'G'],[leafR,'G'],[meet(leafL,box(5,11.1,4,1.5)),'M'],[meet(leafR,box(13,10.7,4,1.6)),'M'],[petals,'P'],[circle(9,3.8,1.5),'Y']]);
 return trim(str(g));}

export default {theme:'batch-0061',pictures:[
 {title:'Круассан',forLevel:66,family:'Хлеб и выпечка',subject:'золотистый круассан-полумесяц со слоистыми сегментами',source:'оригинальный',rows:croissant()},
 {title:'Торт со свечами',forLevel:67,family:'Десерты и сладости',subject:'двухъярусный праздничный торт с подтёками глазури, ягодами и тремя горящими свечами',source:'оригинальный',rows:cake()},
 {title:'Уютная книжная полка',forLevel:68,family:'Мебель и уютные уголки',subject:'деревянная книжная полка с цветными корешками книг, цветком в горшке и маленькими часами',source:'оригинальный',rows:bookshelf()},
 {title:'Фонарь со светлячками',forLevel:69,family:'Свет и домашний декор',direction:'Домашняя магия',subject:'стеклянный фонарь со светлячками внутри на фоне ночи, несколько светлячков снаружи',source:'оригинальный',rows:lantern()},
 {title:'Сапог с цветком',forLevel:70,family:'Одежда и аксессуары',direction:'Живая архитектура',subject:'резиновый сапог, из которого растёт цветок',source:'оригинальный',rows:boot()},
]};

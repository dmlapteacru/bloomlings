// Batch 0011, part 01: levels 11–15 (whale, whistling kettle, fox postman, lighthouse on a rock, astronaut in a fishbowl)
import {circle,ellipse,box,poly,union,cut,meet} from '../../../../../tools/gameplay-pilot-v9/draw.mjs';
import {raster,fit,str,put,ring4,scale} from '../helpers.mjs';
// crop the empty rows and columns round the subject
const trim=rows=>{const r=rows.filter(l=>/[^.]/.test(l)),xs=r.flatMap(l=>[...l].map((c,i)=>c==='.'?-1:i).filter(i=>i>=0)),a=Math.min(...xs),b=Math.max(...xs);return r.map(l=>l.slice(a,b+1));};
// stamp hand-drawn rows onto a grid at (x0,y0); '.' leaves the cell as it is
const stamp=(g,x0,y0,rows)=>{rows.forEach((l,j)=>[...l].forEach((c,i)=>{if(c!=='.')g[y0+j][x0+i]=c;}));return g;};
const mv=(dx,dy,f)=>(x,y)=>f(x-dx,y-dy);
const draw=(w,h,k,layers)=>raster(Math.round(w*k),Math.round(h*k),'.',layers.map(([s,c])=>[scale(k,s),typeof c==='function'?(x,y,u)=>c(x/k,y/k,u):c]));

function whale(){
 const body=union(ellipse(16,20,14,9),poly([[22,13],[31,15],[34,12],[36,14],[33,22],[24,28]]));
 const fluke=poly([[33,15],[28,9],[27,5],[30,6],[34,10],[38,4],[41,4],[39,9],[35,15]]);
 const back=cut(body,ellipse(17,24.5,16,10.5));
 const belly=meet(body,ellipse(14,29,15,6.5));
 const spout=union(box(11,8.5,1,3.5),ellipse(11,4,5.5,1.6),box(6.2,5.5,1,1.3),box(15.8,5.5,1,1.3));
 const g=draw(42,30,1.1,[[spout,'S'],[fluke,'N'],[body,'B'],[back,'N'],[belly,'C'],[ellipse(15.5,24,3.5,1.8),'N']]);
 put(g,'D',[[8,19],[9,19],[8,20],[9,20]]);put(g,'C',[[8,19]]);put(g,'K',[[11,22],[12,22],[13,22]]);
 return trim(str(g));}

function kettle(){
 const body=cut(ellipse(18,22,13,10),box(18,34,20,3));
 const base=box(18,31,10,1);
 const spout=poly([[7,23],[5,23],[1,15],[4,14],[9,19]]);
 const handle=cut(meet(ellipse(18,14,10,9),box(18,9,12,6)),ellipse(18,14,8,7));
 const lid=ellipse(18,12.5,6,2.5),knob=circle(18,9.5,1.6);
 const steam=union(circle(2.8,10.2,1.6),circle(4.6,7.4,2.3),circle(2.2,6.4,1.7),circle(7,6,1.8));
 const shine=cut(meet(ellipse(18,22,11.5,8.5),box(9,18,5,6)),ellipse(19.5,23,11,8));
 const flower=union(circle(20,22.5,1.4),circle(23,22.5,1.4),circle(21.5,20.5,1.4),circle(21.5,24.5,1.4));
 const g=draw(36,33,1.05,[[steam,'S'],[handle,'D'],[spout,'R'],[box(2.5,14,1.8,1.2),'Y'],[body,'R'],[shine,'K'],[flower,'C'],[circle(21.5,22.5,1),'Y'],[base,'D'],[lid,'C'],[knob,'D']].map(([f,c])=>[mv(3,0,f),c]));
 return trim(str(g));}

function fox(){
 const head=union(ellipse(17,15,9,6.5),poly([[7,15],[17,24],[27,15]]));
 const ears=union(poly([[7.5,13],[8.5,0.5],[15,9]]),poly([[26.5,13],[25.5,0.5],[19,9]]));
 const inner=union(poly([[9.8,9],[10,3.5],[13.2,9]]),poly([[24.2,9],[24,3.5],[20.8,9]]));
 const muzzle=poly([[7,16],[17,24],[27,16],[22,16],[17,19],[12,16]]);
 const cap=union(box(17,7,4.8,2.2,1),box(17,5.2,5.5,1,0.5)),visor=box(17,9.8,5.5,0.8);
 const torso=ellipse(17,32,7.5,8),collar=poly([[13,24],[21,24],[17,29]]);
 const legs=union(box(14,40,2,2.5),box(20,40,2,2.5));
 const arm=box(10,31.5,1.6,4,1.5),paw=box(10,35.3,1.6,1,0.8);
 const tail=union(circle(10.5,37.5,2.3),circle(7,35,3.5),circle(4.5,31,4.2),circle(4,26.5,3.8),circle(5.5,22.5,2.6),circle(7.5,20.5,1.5),poly([[9,40],[13,40],[13,35]]));
 const tip=meet(tail,box(5,18,6,3.8));
 const strap=(x,y)=>{const ax=11,ay=25,bx=25,by=32,ex=bx-ax,ey=by-ay,t=Math.max(0,Math.min(1,((x-ax)*ex+(y-ay)*ey)/(ex*ex+ey*ey)));return Math.hypot(x-ax-ex*t,y-ay-ey*t)-1;};
 const bag=box(26,34,5,4.2,1),env=box(26,28.6,3.5,2.3);
 const g=draw(40,43,1,[[tail,'O'],[tip,'C'],[legs,'D'],[torso,'N'],[collar,'C'],[arm,'N'],[paw,'O'],[ears,'O'],[inner,'C'],[head,'O'],[muzzle,'C'],[cap,'N'],[visor,'D'],[box(17,6.8,0.9,1),'Y'],[strap,'A'],[env,'C'],[bag,'A'],[box(26,29,0.9,0.9),'R']].map(([f,c])=>[mv(4,0,f),c]));
 const P=(x,y)=>[x+4,y];
 put(g,'D',[P(13,13),P(13,14),P(21,13),P(21,14),P(16,22),P(17,22)]);put(g,'Y',[P(17,31),P(17,34)]);
 return trim(str(g));}

function lighthouse(){
 const tower=poly([[7,38],[23,38],[20.5,17],[9.5,17]]);
 const stripe=(x,y)=>Math.floor((y-17)/4.2)%2===0?'R':'C';
 const rock=poly([[1,45],[3,40],[7,37],[12,36],[19,36],[24,37.5],[28,41],[30,45]]);
 const shade=meet(rock,poly([[18,47],[23,39],[30,39],[31,47]]));
 const water=union(box(15.5,45.5,15.5,1),box(3,44,2.5,0.8),box(28,44,2.5,0.8));
 const beams=union(poly([[15,12],[4,9],[4,15]]),poly([[15,12],[26,9],[26,15]]));
 const g=draw(31,47,1,[[beams,'Y'],[rock,'A'],[shade,'E'],[water,'B'],[tower,stripe],[box(15,35.5,1.6,2.5,1),'D'],[box(15,25,1,1.2),'D'],[box(15,16,8.5,1),'D'],[box(15,12.5,5.5,2.5),'D'],[box(15,12.5,3.5,1.5),'Y'],[poly([[8.5,10],[21.5,10],[15,4]]),'R'],[circle(15,4.5,1.2),'D']]);
 return trim(str(g));}

function fishbowl(){
 const outer=cut(cut(circle(18,19,16.5),box(18,0,20,5)),box(18,40,20,5.5));
 const inner=meet(outer,circle(18,19,15.5));
 const water=meet(inner,box(18,30,20,20));
 const sand=meet(inner,box(18,38,20,8.5));
 const fish=union(ellipse(22,13.5,6,4),poly([[26,13.5],[31.5,9.5],[31.5,17.5]]));
 const fin=poly([[19,10.5],[23,7],[26,10.5]]);
 const weed=meet(inner,union(box(25,26,1,6),box(28,28.5,1,4)));
 const g=draw(36,36,1,[[outer,'D'],[inner,'S'],[water,'B'],[sand,'Y'],[weed,'G'],[fin,'Y'],[fish,'O'],[box(18,5,10.5,1),'D']]);
 put(g,'D',[[19,12]]);
 stamp(g,7,15,[
  '.....CCCCC.....',
  '....CCCCCCC....',
  '...CCNNNNNCC...',
  '...CNSSNNNNC.DD',
  '...CNSNNNNNC.CC',
  '...CNNNNNNNC.CC',
  '...CCNNNNNCC.CC',
  '....CCCCCCC.CC.',
  '..CCCCCCCCCCC..',
  '.CCCCOOCCCCC...',
  '.CC.CCCCCCC....',
  '.DD.CCCCCCC....',
  '....CCC.CCC....',
  '....CCC.CCC....',
  '....DDD.DDD....']);
 return trim(str(g));}

export default {theme:'batch-0011',pictures:[
 {title:'Кит',forLevel:11,family:'Морские и пресноводные обитатели',subject:'голубой кит с фонтаном',source:'оригинальный',rows:whale()},
 {title:'Чайник со свистком',forLevel:12,family:'Посуда и кухонные вещи',subject:'круглый чайник со свистком',source:'оригинальный',rows:kettle()},
 {title:'Лиса-почтальон',forLevel:13,family:'Лесные и горные животные',direction:'Обмен ролями',subject:'лиса-почтальон с сумкой и письмом',source:'оригинальный',rows:fox()},
 {title:'Маяк на скале',forLevel:14,family:'Дома и архитектура',subject:'полосатый маяк на скале',source:'оригинальный',rows:lighthouse()},
 {title:'Космонавт в аквариуме',forLevel:15,family:'Космос',direction:'Миры внутри предметов',device:'маленький мир внутри обычного предмета',subject:'космонавт в круглом аквариуме с большой рыбой',source:'оригинальный',rows:fishbowl()},
]};

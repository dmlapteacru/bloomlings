// Batch 0061, part 08: L96–L100 (rotary telephone, flying retro bus, moon on a washing line, fish catching a fisherman, chess knight).
import {circle,ellipse,box,poly,union,cut,meet} from '../../../../../tools/gameplay-pilot-v9/draw.mjs';
import {raster,fit,str,put,ring4,scale,seg} from '../helpers.mjs';
const draw=(w,h,k,layers)=>raster(Math.round(w*k),Math.round(h*k),'.',layers.map(([s,c])=>[scale(k,s),typeof c==='function'?(x,y,u)=>c(x/k,y/k,u):c]));

// 1. A red rotary-dial telephone: the handset across the top, a cream dial with finger holes, a curly cord at the side.
function phone(){
 const body=union(poly([[9,14],[31,14],[35.5,31],[4.5,31]]),ellipse(20,15,11.5,4.5));
 const lit=meet(body,cut(poly([[8.6,14],[13,14],[9.6,31],[5,31]]),poly([[0,0],[7.2,0],[7.2,40],[0,40]])));
 const handle=box(20,5.2,11,2.2,1.5);
 const cups=union(ellipse(8,8,4.6,3.4),ellipse(32,8,4.6,3.4));
 const cord=(x,y)=>{const t=(y-10)/(28-10);if(t<0||t>1)return 1;const cx=35+4*Math.sin(t*Math.PI)+(Math.floor(y)%2?.6:-.6);return Math.abs(x-cx)-1;};
 const holes=(x,y)=>{let d=9;for(let i=0;i<9;i++){const a=-Math.PI*0.4+i*Math.PI*1.5/8,hx=Math.round(20+4.6*Math.cos(a)),hy=Math.round(23+4.6*Math.sin(a));d=Math.min(d,Math.hypot(x-hx,y-hy)-1);}return d;};
 const g=draw(44,35,1,[
  [cord,'D'],
  [box(20,10.5,8,1.2),'D'],                                  // cradle
  [body,'R'],[lit,'K'],
  [union(box(9,31.5,3,1.2,.5),box(31,31.5,3,1.2,.5)),'D'],   // feet
  [union(handle,cups),'R'],[box(20,4.4,9,.8,.4),'K'],        // handset with a gleam
  [circle(20,23,7.6),'D'],[circle(20,23,6.6),'C'],
  [holes,'D'],
  [circle(20,23,2),'Y'],
 ]);
 return str(g);}

// 2. A rounded retro bus with a propeller on the roof and little wings, flying through a blue sky among cream clouds.
function bus(){
 const W=38,H=33;
 const cloud=(cx,cy,s)=>union(ellipse(cx,cy,3.6*s,2*s),circle(cx-2*s,cy-1*s,1.9*s),circle(cx+1.2*s,cy-1.6*s,2.2*s));
 const body=union(box(17.5,18,12,7.5,3),box(22,18,11,7.5,6));
 const g=draw(W,H,1,[
  [box(W/2,H/2,W/2,H/2),'B'],
  [union(cloud(5.5,5.5,1.1),cloud(33,4.5,.9),cloud(4.5,30,1.2),cloud(31,31,1.3)),'C'],
  [poly([[14,18],[2.5,12.5],[1.5,15],[5,17.5],[14,22.5]]),'R'],   // swept wing
  [box(19,8.5,1,2.5),'D'],                                  // mast
  [union(ellipse(13,6.5,5.4,1.3),ellipse(25,6.5,5.4,1.3)),'A'],[circle(19,6.5,1.4),'D'],
  [body,'Y'],[meet(body,box(19,23.5,20,2.5)),'O'],
  [meet(body,box(19,20.5,20,.7)),'R'],                       // stripe
  [union(box(9.5,15.3,2.2,2.3,.6),box(15.5,15.3,2.2,2.3,.6),box(21.5,15.3,2.2,2.3,.6)),'S'],
  [poly([[26.5,13],[29.5,13],[32.5,17.6],[26.5,17.6]]),'S'], // windscreen
  [box(26,20.5,.5,4.4),'D'],                                 // door edge
  [circle(32.2,22.4,1.2),'C'],                               // headlight
  [union(circle(10,26,3),circle(26.5,26,3)),'D'],[union(circle(10,26,1.1),circle(26.5,26,1.1)),'C'],
 ]);
 return str(g);}

// 3. A crescent moon pegged on a washing line between two poles, beside a red sock and a star, in the night sky.
function moonLine(){
 const W=32,H=30;
 const sag=x=>5.2+2*Math.sin(Math.PI*(x-3)/26);
 const line=(x,y)=>Math.abs(y-sag(x))-.5;
 const moonO=circle(16,15.5,8.2),moon=cut(moonO,circle(20.6,13,7.2));
 const star=(()=>{const p=[];for(let i=0;i<10;i++){const a=-Math.PI/2+i*Math.PI/5,r=i%2?1.9:4.3;p.push([26+r*Math.cos(a),11.6+r*Math.sin(a)]);}return poly(p);})();
 const tiny=(cx,cy)=>union(box(cx,cy,1.5,.5),box(cx,cy,.5,1.5));
 const g=draw(W,H,1,[
  [box(W/2,H/2,W/2,H/2),'N'],
  [union(tiny(9.5,23.5),tiny(24.5,22.5),tiny(16.5,2.5)),'Y'],
  [box(W/2,28.5,W/2,1.5),'M'],
  [union(box(2.5,15,1,14),box(29.5,15,1,14)),'A'],[union(box(2.5,3,2,.7),box(29.5,3,2,.7)),'A'],
  [meet(line,box(16,10,13,10)),'C'],
  [moon,'Y'],[meet(moon,cut(moonO,circle(17.1,14.4,8.2))),'O'],
  [star,'Y'],[meet(star,(x,y)=>x+y>39.4?-1:1),'O'],
  [union(box(7.5,11.5,2,3.6),box(5.9,14.6,3.6,1.7,1)),'R'],[box(7.5,8.4,2,.9),'C'],[box(7.5,11.5,2,.5),'C'],   // sock: cuff, stripe
  [union(box(7,7.4,1,1.5),box(18.5,7.4,1,1.5),box(26,7.4,1,1.5)),'A'],                // pegs
 ]);
 put(g,'A',[[11,14],[12,15]]);                                                   // sleepy eye
 return str(g);}

// 4. A big orange fish in the sea holding a fishing rod; the hook dangles a boot above the surface, a boat's bow at the edge.
function fishFisher(){
 const W=34,H=32,sea=12;
 const g=draw(W,H,1,[
  [box(W/2,H/2,W/2,H/2),(x,y)=>y<sea?'S':'B'],
  [poly([[23,9.5],[34,9.5],[34,14],[26.5,14]]),'R'],[poly([[23,9],[34,9],[34,10.2],[23.6,10.2]]),'A'],   // boat bow
  [(x,y)=>y>=sea&&y<sea+1&&(Math.floor(x)%4)<2?-1:1,'S'],
  [box(4.5,6.5,.5,3.5),'D'],                                                      // line
  [union(box(4.5,10,1.5,2),box(6,12,3,1.1,.5)),'D'],                              // boot
  [poly([[25.5,21],[31.5,15],[32,27.5],[25.5,22.5]]),'R'],                        // tail
  [ellipse(16.5,21.5,10.5,7),'O'],[meet(ellipse(16.5,21.5,10.5,7),box(16.5,27,13,2.4)),'Y'],
  [poly([[11,15.2],[17,12.4],[21.5,15.6]]),'R'],                                   // dorsal fin
  [seg(16.5,23.5,4.5,3,.55),'A'],                                                  // rod
  [ellipse(16.5,23.5,2.6,1.7),'R'],                                                // fin holding the rod
  [circle(10,19,1.9),'C'],[box(10,19,.5,1),'D'],
  [box(7.5,23,1.5,.5),'R'],
 ]);
 return str(g);}

// 5. A two-tone chess knight (cream lit side, amber shade) on a stepped round base, dark outline.
function knight(){
 const head=poly([[9.5,1],[11.8,3.4],[14.4,5.2],[15.8,8.8],[16.1,13.5],[15,16.8],[5,16.8],[5.8,14],[7.8,12],[8.3,11],[3.4,11],[1.9,9.3],[2.8,7],[6.3,4.4],[8.2,2.8]]);
 const base=union(box(9,17.9,6.2,1,.3),box(9,20,7.4,1.2,.5),box(9,22.4,8.4,1.3,.6));
 const shade=(x,y)=>x-.62*y>8.6?-1:1;
 const g=draw(19,24,1,[
  [head,'C'],[meet(head,shade),'A'],
  [base,'C'],[meet(base,box(15.5,20,3,5)),'A'],
 ]);
 put(g,'E',[[6,6],[3,9],[4,9]]);
 put(g,'E',[[11,5],[13,8],[14,11]]);                         // mane notches
 ring4(g,['C','A','E'],'E','.');
 return str(g);}

export default {theme:'batch-0061',pictures:[
 {title:'Старый телефон',forLevel:96,family:'Ретро-техника и ностальгия',subject:'дисковый телефон с витым шнуром',source:'оригинальный',rows:phone()},
 {title:'Летающий ретроавтобус',forLevel:97,family:'Воздушный транспорт',direction:'Ретрофутуристические приключения',subject:'ретроавтобус с пропеллером среди облаков',source:'оригинальный',rows:bus()},
 {title:'Луна на бельевой верёвке',forLevel:98,family:'Времена года и погода',direction:'Уютный сюрреализм',subject:'месяц на прищепках рядом с носком и звездой',source:'оригинальный',rows:moonLine()},
 {title:'Рыба ловит рыбака',forLevel:99,family:'Морские и пресноводные обитатели',direction:'Визуальные каламбуры',subject:'рыба с удочкой, на крючке ботинок',source:'оригинальный',rows:fishFisher()},
 {title:'Шахматный конь',forLevel:100,family:'Игрушки и настольные игры',subject:'шахматный конь',source:'оригинальный',rows:knight()},
]};

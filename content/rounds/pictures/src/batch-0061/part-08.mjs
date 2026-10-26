// Batch 0061, part 08: L96–L100 (rotary telephone, flying retro bus, moon on a washing line, fish catching a fisherman, chess knight).
import {circle,ellipse,box,poly,union,cut,meet} from '../../../../../tools/gameplay-pilot-v9/draw.mjs';
import {raster,fit,str,put,ring4,scale,seg} from '../helpers.mjs';
const draw=(w,h,k,layers)=>raster(Math.round(w*k),Math.round(h*k),'.',layers.map(([s,c])=>[scale(k,s),typeof c==='function'?(x,y,u)=>c(x/k,y/k,u):c]));

// 1. A red rotary-dial telephone: the handset across the top, a cream dial with finger holes, a curly cord at the side.
function phone(){
 const bodyAt=(dx)=>union(poly([[9+dx,14],[31+dx,14],[35.5+dx,31],[4.5+dx,31]]),ellipse(20+dx,15,11.5,4.5));
 const body=bodyAt(0);
 const lit=meet(cut(body,bodyAt(2.2)),box(8,22,6,8));
 const handle=box(20,5.2,11,2.2,1.5);
 const cups=union(ellipse(8,8,4.6,3.4),ellipse(32,8,4.6,3.4));
 const cord=(x,y)=>{const t=(y-10)/(28-10);if(t<0||t>1)return 1;const cx=35+4*Math.sin(t*Math.PI)+(Math.floor(y)%2?.6:-.6);return Math.abs(x-cx)-1;};
 const holes=(x,y)=>{let d=9;for(let i=0;i<8;i++){const a=-Math.PI*0.3+i*Math.PI*1.45/7,hx=Math.round(20+5*Math.cos(a)),hy=Math.round(23+5*Math.sin(a));d=Math.min(d,Math.hypot(x-hx,y-hy)-1);}return d;};
 const g=draw(44,35,1,[
  [cord,'D'],
  [box(20,10.5,8,1.2),'D'],                                  // cradle
  [body,'R'],[lit,'K'],
  [union(box(9,31.5,3,1.2,.5),box(31,31.5,3,1.2,.5)),'D'],   // feet
  [union(handle,cups),'R'],[box(20,4.4,9,.8,.4),'K'],        // handset with a gleam
  [circle(20,23,8.4),'D'],[circle(20,23,7.4),'C'],
  [holes,'D'],
  [circle(20,23,2.1),'Y'],[box(25.5,29,1,1),'D'],
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
  [poly([[14,16.5],[1,10],[1,13.5],[4.5,16.5],[14,22.5]]),'R'],[poly([[14,19.5],[1,13.5],[4.5,16.5],[14,22.5]]),'O'],   // swept wing
  [box(19,8.5,1,2.5),'D'],                                  // mast
  [union(ellipse(13,6.5,5.4,1.3),ellipse(25,6.5,5.4,1.3)),'A'],[circle(19,6.5,1.4),'D'],
  [body,'Y'],[meet(body,box(19,23.5,20,2.5)),'O'],
  [meet(body,box(19,20.5,20,.7)),'R'],                       // stripe
  [union(box(10,15,2,2.5,.3),box(16,15,2,2.5,.3),box(22,15,2,2.5,.3)),'S'],
  [box(29,15,2.5,2.5,.3),'S'],[box(31,15,.5,2.5),'S'],    // windscreen
  [circle(32.2,22.4,1.2),'C'],                               // headlight
  [union(circle(10,26,3),circle(26.5,26,3)),'D'],[union(circle(10,26,1.1),circle(26.5,26,1.1)),'C'],
 ]);
 return str(g);}

// 3. A crescent moon pegged on a washing line between two poles, beside a red sock and a star, in the night sky.
function moonLine(){
 const W=36,H=27;
 const sag=x=>4.5+2*Math.sin(Math.PI*(x-2)/32);
 const line=(x,y)=>Math.abs(y-sag(x))-.5;
 const moonO=circle(17.5,15.5,8),moon=cut(moonO,circle(21.5,14.5,7));
 const star=(()=>{const p=[];for(let i=0;i<10;i++){const a=-Math.PI/2+i*Math.PI/5,r=i%2?2.1:4.2;p.push([28.5+r*Math.cos(a),11.8+r*Math.sin(a)]);}return poly(p);})();
 const tiny=(cx,cy)=>union(box(cx,cy,1.5,.5),box(cx,cy,.5,1.5));
 const g=draw(W,H,1,[
  [box(W/2,H/2,W/2,H/2),'N'],
  [union(tiny(11.5,2.5),tiny(26.5,22.5),tiny(5.5,20.5)),'Y'],
  [box(W/2,25.5,W/2,1.5),'M'],
  [union(box(2,14,1,13),box(34,14,1,13)),'A'],[union(box(2,2,2,1),box(34,2,2,1)),'A'],
  [meet(line,box(18,10,15,10)),'C'],
  [moon,'Y'],[meet(moon,cut(moonO,circle(18.7,14.3,8))),'O'],
  [star,'Y'],
  [union(box(7,10.5,2,3.5),box(5.5,13.5,3.5,1.5,.8)),'R'],[box(7,7.5,2,1),'C'],[box(7,11,2,.5),'C'],   // sock: cuff, stripe
  [union(box(7,6,1,1.5),box(19.5,7,1,1.5),box(28.5,7,1,1.5)),'A'],                // pegs
 ]);
 put(g,'A',[[11,15],[12,16],[13,15]]);                                           // sleepy eye
 return str(g);}

// 4. A big orange fish in the sea holding a fishing rod; the hook dangles a boot above the surface, by a boat's bow.
function fishFisher(){
 const W=34,H=32,sea=12;
 const body=ellipse(14.5,23,10.5,7);
 const g=draw(W,H,1,[
  [box(W/2,H/2,W/2,H/2),(x,y)=>y<sea?'S':'B'],
  [poly([[27.5,9.4],[34,9.4],[34,14],[30,14]]),'R'],[poly([[27,8.6],[34,8.6],[34,10],[27.8,10]]),'A'],   // boat bow
  [(x,y)=>y>=sea&&y<sea+1&&(Math.floor(x)%4)<2?-1:1,'S'],
  [box(24.5,4.5,.5,1.5),'D'],                                                     // line
  [union(box(24.5,8,1.5,2),box(23,10.5,3,1,.5)),'D'],                             // boot, toe to the left
  [poly([[5,22.5],[.5,16.5],[.5,29.5],[5,23.5]]),'R'],                            // tail
  [body,'O'],[meet(body,box(14.5,28.5,13,2.4)),'Y'],
  [poly([[6,17],[9.5,13.5],[14,16]]),'R'],                                         // dorsal fin
  [seg(12.5,25,24.5,2.5,.6),'A'],                                                  // rod
  [ellipse(12.5,25,2.8,1.8),'R'],                                                  // fin holding the rod
  [circle(19.5,20.5,2),'C'],[box(20,20.5,.5,1),'D'],
  [box(23,24,1.5,.5),'D'],[box(22,23,.5,.5),'D'],                                  // smile
 ]);
 return str(g);}

// 5. A two-tone chess knight (cream lit side, amber shade) on a stepped base, dark outline.
function knight(){
 const R=[
  '........CA........',
  '.......CCA........',
  '......CCCAA.......',
  '.....CCCCEAA......',
  '....CCCCCCCAA.....',
  '...CCCCCCCCEAA....',
  '..CCCEECCCCCAA....',
  '.CCCCCCCCCCCCAA...',
  '.ECCCCCCCCCCEAA...',
  '.CCCCCCCCCCCCAA...',
  '..EEECCCCCCCCAA...',
  '.....CCCCCCCEAA...',
  '......CCCCCCCAA...',
  '.....CCCCCCCCAA...',
  '....CCCCCCCCCAAA..',
  '...CCCCCCCCCCAAA..',
  '..CCCCCCCCCCCAAA..',
  '..EEEEEEEEEEEEEE..',
  '..CCCCCCCCCCCAAA..',
  '.EEEEEEEEEEEEEEEE.',
  '.CCCCCCCCCCCCCAAA.',
  'CCCCCCCCCCCCCCCAAA',
  'CCCCCCCCCCCCCCCAAA'];
 const g=[Array(20).fill('.'),...R.map(r=>['.',...r,'.']),Array(20).fill('.')];
 ring4(g,['C','A','E'],'E','.');
 return str(g);}

export default {theme:'batch-0061',pictures:[
 {title:'Старый телефон',forLevel:96,family:'Ретро-техника и ностальгия',subject:'дисковый телефон с витым шнуром',source:'оригинальный',rows:phone()},
 {title:'Летающий ретроавтобус',forLevel:97,family:'Воздушный транспорт',direction:'Ретрофутуристические приключения',subject:'ретроавтобус с пропеллером среди облаков',source:'оригинальный',rows:bus()},
 {title:'Луна на бельевой верёвке',forLevel:98,family:'Времена года и погода',direction:'Уютный сюрреализм',subject:'месяц на прищепках рядом с носком и звездой',source:'оригинальный',rows:moonLine()},
 {title:'Рыба ловит рыбака',forLevel:99,family:'Морские и пресноводные обитатели',direction:'Визуальные каламбуры',subject:'рыба с удочкой, на крючке ботинок',source:'оригинальный',rows:fishFisher()},
 {title:'Шахматный конь',forLevel:100,family:'Игрушки и настольные игры',subject:'шахматный конь',source:'оригинальный',rows:knight()},
]};

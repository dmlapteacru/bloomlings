// Batch 0061, part 4: levels 76–80 (steam locomotive, yellow submarine in the sea, little plane in the sky, brass compass, cat at the keyboard).
import {circle,ellipse,box,poly,union,cut,meet} from '../../../../../tools/gameplay-pilot-v9/draw.mjs';
import {raster,fit,str,put,ring4,scale,seg} from '../helpers.mjs';
const all=()=>-1;
const draw=(w,h,k,layers)=>raster(Math.round(w*k),Math.round(h*k),'.',layers.map(([s,c])=>[scale(k,s),typeof c==='function'?(x,y,u)=>c(x/k,y/k,u):c]));
const half=f=>(x,y)=>f(x,y)?-1:1; // a predicate as a shape
const ring=(cx,cy,r1,r2)=>cut(circle(cx,cy,r1),circle(cx,cy,r2));
// lay hand-drawn rows over a grid; '.' keeps the cell underneath
const over=(g,x0,y0,rows)=>{rows.forEach((l,j)=>[...l].forEach((c,i)=>{if(c!=='.'&&g[y0+j]&&x0+i<g[0].length)g[y0+j][x0+i]=c;}));return g;};

function locomotive(){
 const W=42,H=34,k=1.06;
 const wheel=(cx,cy,r)=>[[circle(cx,cy,r),'D'],[circle(cx,cy,r-1),'R'],[circle(cx,cy,1.3),'Y']];
 const g=draw(W,H,k,[
  // the big chimney puff: soft clouds rising to the left
  [union(circle(30.6,5.6,3.6),circle(25.2,4.2,3.2),circle(20.2,3.4,2.6),circle(35,3.6,2.6),circle(28,2.4,2.4)),'S'],
  // the chimney: a flared top on a short stem
  [union(poly([[26.5,8.6],[35.5,8.6],[33.5,11.8],[28.5,11.8]]),box(31,13.8,2.4,3)),'D'],
  // the boiler with its round front, a brass dome and two bands
  [box(27.5,19.5,12,4.5,2),'G'],
  [meet(box(27.5,19.5,12,4.5,2),half((x,y)=>y>21.6)),'M'],
  [union(box(22,19.5,.5,4.5),box(30,19.5,.5,4.5)),'M'],
  [box(37.5,19.5,2,4.5,2),'D'],
  [box(25.5,14,2.2,1.6,1.2),'Y'],
  [circle(39,16.4,1.3),'Y'],
  // the cab with its roof and a big window
  [box(10,16.5,6,8),'B'],
  [box(10,21.7,6,2.8),'N'],
  [box(10,8,7.5,1.5,.6),'R'],
  [box(10,13.5,3.4,3,.5),'S'],
  // the chassis, the cowcatcher and the wheels joined by a rod
  [box(21.5,25,19.5,1.5),'D'],
  [poly([[37.4,23.6],[39.8,23.6],[41.8,31.6],[36.4,31.6]]),'R'],
  ...wheel(10,27,5.6),...wheel(24.5,28.4,4.2),...wheel(33.5,28.4,4.2),
  [box(22,28.4,11.5,.6),'Y'],
 ]);
 return str(g);}

function submarine(){
 const W=34,H=32;
 const sea=(x,y)=>y>=29?'C':'B';
 const g=raster(W,H,'.',[
  [all,sea],
  [union(circle(4,30,2.6),circle(29.5,29.6,2.2)),'C'],
  // seaweed fronds
  [union(seg(3,29,2,23,1.1),seg(2,23,3.5,18.5,1)),'G'],[union(seg(30,29,31.2,24,1.1),seg(31.2,24,30,20.5,1)),'G'],
  // bubbles from the propeller
  [union(circle(4.6,10.5,1.5),circle(2.6,6.2,1.1),circle(5.5,3,1.4)),'S'],
  // periscope
  [union(box(18.5,5,1,4),box(20.5,2,3,1.1)),'D'],
  // tower, body and tail
  [box(16.5,10.5,5.5,3.2,1.5),'Y'],
  [poly([[3,13],[7,15],[7,21],[3,23]]),'O'],
  [ellipse(17.5,18,12.2,6.6),'Y'],
  [meet(ellipse(17.5,18,12.2,6.6),half((x,y)=>y>21.3)),'O'],
  [box(1.8,18,1,4),'R'],[box(3.2,18,1.2,.8),'D'],
  // three portholes
  ...[11.5,18,24.5].flatMap(x=>[[circle(x,17.5,2.6),'D'],[circle(x,17.5,1.6),'S']]),
  // two fish
  [union(ellipse(28,6,2.8,1.8),poly([[30,6],[33,3.8],[33,8.2]])),'O'],
  [union(ellipse(9,26,2.8,1.8),poly([[11,26],[14,23.8],[14,28.2]])),'R'],
 ]);
 put(g,'D',[[26,5],[7,25]]);
 return str(g);}

function plane(){
 const W=31,H=29;
 const g=raster(W,H,'.',[
  [all,'S'],
  // two big clouds, one above the nose and one under the tail
  [union(circle(20.5,5.4,3),circle(25,3.6,3.6),circle(29.5,5.6,2.8),box(25,7,8,1.5,1.4)),'C'],
  [union(circle(3,26,2.6),circle(7,24.8,3.2),circle(11,26.4,2.2),box(6.5,27.4,8,1.2,1.1)),'C'],
 ]);
 // the plane, nose to the right: tail fin, pilot with goggles and a scarf, cockpit rim, near wing, spinner, propeller, wheel
 over(g,0,6,[
'...............................',//6
'...............................',//7
'..RR...........AAAA............',//8
'..RRR.........AAAAAA...........',//9
'..RRRR........AADDCD.......DD..',//10
'..RRRRR.......AAKKKK.......DD..',//11
'..RRRRRR...CCCAKKKKK.......DD..',//12
'..RRRRRRRRCCRDDDDDDDDRRRR..DD..',//13
'...RRRRRRRRRRRRRRRRRRRRRRY.DD..',//14
'....RRRRRRRRRRRRRRRRRRRRRYYDD..',//15
'.....RRRRRRRRRRRRRRRRRRRRYYDD..',//16
'.YYYYYYYRRRRRRRRRRRRRRRRRYYDD..',//17
'.YYYYYYRRRRRYYYYYYYYYYRRRYYDD..',//18
'...YYY..RRRYYYYYYYYYYRRRRY.DD..',//19
'.........RYYYYYYYYYYRRRRR..DD..',//20
'.........YYYYYYYYYY....D...DD..',//21
'........YYYYYYYYYY....DDD..DD..',//22
'.......YYYYYYYYYY.....DDD......',//23
 ]);
 return str(g);}

function compass(){
 const W=21,H=25,cx=10.5,cy=14.5;
 const g=raster(W,H,'.',[
  // hinge: a loop and a knob on top
  [ring(cx,3,2.7,1.2),'O'],
  [box(cx,6,2,1.2,.4),'Y'],
  // brass case, its shade, the dark face
  [circle(cx,cy,10),'Y'],
  [meet(circle(cx,cy,10),cut(half(()=>true),circle(cx-1.2,cy-1.2,9.6))),'O'],
  [circle(cx,cy,7.4),'O'],
  [circle(cx,cy,6.6),'N'],
  // four cream marks
  [union(box(cx,cy-5.4,.5,.9),box(cx,cy+5.4,.5,.9),box(cx-5.4,cy,.9,.5),box(cx+5.4,cy,.9,.5)),'C'],
  // the needle: red north, cream south
  [poly([[cx,cy-4.9],[cx+1.6,cy],[cx-1.6,cy]]),'R'],
  [poly([[cx,cy+4.9],[cx+1.6,cy],[cx-1.6,cy]]),'C'],
  [circle(cx,cy,.8),'Y'],
 ]);
 return str(g);}

function cat(){
 const W=40,H=34;
 const g=raster(W,H,'.',[
  // monitor at the left, turned toward the cat, on a stand
  [poly([[1,4],[14,7],[14,22],[1,24]]),'D'],
  [poly([[2.6,6.2],[12.6,8.6],[12.6,20.6],[2.6,22]]),'B'],
  [union(box(6,25.5,1.2,2.5),box(6,28.5,4,.8,.4)),'D'],
  // the cat: tail, body, head, ears
  [union(seg(36,29,38,21,1.3),seg(38,21,36.5,16,1.3)),'O'],
  [ellipse(25,24,9.5,8.5),'O'],
  [ellipse(25,24.8,4.6,6.3),'C'],
  [union(ellipse(25,12.8,8.4,6.8),poly([[17.2,10],[17.8,2.6],[22.6,7]]),poly([[32.8,10],[32.2,2.6],[27.4,7]])),'O'],
  [union(poly([[18.8,8],[19.2,4.8],[21.4,7]]),poly([[31.2,8],[30.8,4.8],[28.6,7]])),'P'],
  [ellipse(25,15.6,3.6,2.4),'C'],
  // keyboard on the desk with three rows of keys, paws on it
  [box(24,29.5,14,2.8,.8),'D'],
  [(x,y)=>((Math.floor(x)+Math.floor(y))%2===0&&Math.abs(y-29.5)<2.2&&Math.abs(x-24)<13.2)?-1:1,'C'],
  [union(ellipse(19.5,27.6,2.6,1.7),ellipse(30.5,27.6,2.6,1.7)),'O'],
 ]);
 // eyes looking left at the screen, nose, frown lines
 put(g,'D',[[20,12],[21,12],[20,13],[21,13],[27,12],[28,12],[27,13],[28,13]]);
 put(g,'C',[[22,12],[22,13],[29,12],[29,13]]);
 put(g,'P',[[24,15],[25,15]]);put(g,'D',[[24,16],[25,16],[23,17],[26,17]]);
 put(g,'A',[[24,7],[25,7],[24,8],[25,8],[21,7],[28,7]]);
 return str(g);}

export default {theme:'batch-0061',pictures:[
 {title:'Паровозик',forLevel:76,family:'Наземный транспорт',subject:'паровозик с клубом дыма из трубы, кабиной и красными колёсами',source:'оригинальный',rows:locomotive()},
 {title:'Подводная лодка',forLevel:77,family:'Водный транспорт',subject:'круглая жёлтая подводная лодка с иллюминаторами и перископом под водой, рыбки и пузыри',source:'оригинальный',rows:submarine()},
 {title:'Самолётик',forLevel:78,family:'Воздушный транспорт',subject:'маленький винтовой самолёт с пилотом в кабине среди облаков',source:'оригинальный',rows:plane()},
 {title:'Компас',forLevel:79,family:'Багаж и навигация',subject:'круглый латунный компас с красно-белой стрелкой и петлёй крышки',source:'оригинальный',rows:compass()},
 {title:'Кот за клавиатурой',forLevel:80,family:'Мемные животные и бытовые ситуации',subject:'кот за клавиатурой у монитора, сосредоточенный, лапы на клавишах',source:'оригинальный',rows:cat()},
]};

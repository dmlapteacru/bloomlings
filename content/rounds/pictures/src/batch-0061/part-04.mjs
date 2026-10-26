// Batch 0061, part 4: levels 76–80 (steam locomotive, yellow submarine in the sea, little plane in the sky, brass compass, cat at the keyboard).
import {circle,ellipse,box,poly,union,cut,meet} from '../../../../../tools/gameplay-pilot-v9/draw.mjs';
import {raster,fit,str,put,ring4,scale,seg} from '../helpers.mjs';
const all=()=>-1;
// drop empty border rows and columns
const trim=g=>{const H=g.length,W=g[0].length,rs=[...Array(H).keys()].filter(y=>g[y].some(c=>c!=='.')),cs=[...Array(W).keys()].filter(x=>g.some(r=>r[x]!=='.'));return g.slice(rs[0],rs.at(-1)+1).map(r=>r.slice(cs[0],cs.at(-1)+1));};
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
  [union(circle(31,5,3.4),circle(34.4,6.2,2.2),circle(28.4,6.4,2),circle(24,3.6,2.5),circle(21.6,4.8,1.7),circle(18.2,3.6,1.5)),'S'],
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
  [poly([[38.4,23.6],[40,23.6],[42.2,31.6],[38.4,31.6]]),'R'],
  ...wheel(10,27,5.45),...wheel(24,28.4,4.1),...wheel(32.6,28.4,4.1),
  [box(21.5,28.4,11,.6),'Y'],
 ]);
 return str(trim(g));}

function submarine(){
 const W=34,H=32;
 const sea=(x,y)=>y>=29?'C':'B';
 const hull=union(ellipse(18.5,18,11.6,6.6),poly([[10,13.4],[4.4,16],[4.4,20],[10,22.6]]));
 const g=raster(W,H,'.',[
  [all,sea],
  [union(circle(4,30,2.6),circle(29.5,29.6,2.2)),'C'],
  // seaweed fronds
  [union(seg(3.5,29,2.6,25,1.05),seg(2.6,25,3.8,22.6,1)),'G'],[union(seg(7.5,29,8.4,26.4,1),seg(8.4,26.4,7.4,24.8,.9)),'G'],
  // bubbles rising from the propeller
  [union(circle(4.4,11,1.6),circle(2.6,6.6,1.25),circle(5,2.6,1.6)),'S'],
  // periscope
  [union(box(19,5,1,4),box(21,2,3,1.1)),'D'],
  // tail fins, tower, hull with a shaded belly
  [union(poly([[5,11.4],[8,11.4],[10.4,15],[6,15]]),poly([[5,24.6],[8,24.6],[10.4,21],[6,21]])),'O'],
  [box(17.5,10.5,5.5,3.2,1.5),'Y'],
  [box(17.5,7.8,4.6,.6,.3),'O'],
  [hull,'Y'],
  [meet(hull,half((x,y)=>y>21.3)),'O'],
  // propeller: a hub and two blades
  [box(3.6,18,1,.8),'D'],[box(2,18,1,4,.6),'R'],
  // three portholes with a glint
  ...[12.5,18.5,24.5].flatMap(x=>[[circle(x,17.5,2.6),'D'],[circle(x,17.5,1.6),'S']]),
 ]);
 // two fish: an orange one swimming left, a red one swimming right
 const fish=['..OOOO...O','.OOOOOO.OO','ODOOOOOOOO','.OOOOOO.OO','..OOOO...O'];
 over(g,23,3,fish);
 over(g,23,24,fish.map(r=>[...r].reverse().join('').replace(/O/g,'R')));
 put(g,'C',[[11,16],[17,16],[23,16]]);
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
  // brass case with a solid shade on the lower right, a thin bezel, the dark face
  [circle(cx,cy,10),'Y'],
  [meet(circle(cx,cy,10),half((x,y)=>(x-cx)+(y-cy)>5.2)),'O'],
  [circle(cx,cy,7.4),'O'],
  [circle(cx,cy,6.6),'N'],
 ]);
 // the needle, turned to the north-east: red half up and right, cream half down and left, a brass pin in the middle
 put(g,'R',[[11,13],[12,13],[11,12],[12,12],[13,12],[12,11],[13,11],[14,11],[13,10],[14,10]]);
 put(g,'C',[[9,15],[8,15],[9,16],[8,16],[7,16],[8,17],[7,17],[6,17],[7,18],[6,18]]);
 put(g,'Y',[[10,14],[11,14],[10,13],[9,14],[10,15]]);
 put(g,'C',[[10,8],[10,20],[4,14],[16,14]]);
 return str(trim(g));}

function cat(){
 const W=40,H=34;
 const g=raster(W,H,'.',[
  // monitor at the left, turned toward the cat, on a stand
  [union(box(7,27,1.2,3),box(7,29.6,4.2,.8,.4)),'D'],
  [poly([[.6,6],[14,4],[14,25],[.6,23]]),'D'],
  [poly([[2,7.6],[12.6,6],[12.6,23],[2,21.6]]),'B'],
  // lines of code on the screen
  [(x,y)=>{const r=Math.floor(y),xs=Math.floor(x);return ([10,13,16,19].includes(r)&&xs>=3+(r%2)&&xs<=(r===13?9:r===19?7:11))?-1:1;},'S'],
  // the cat: tail, body with a white chest, head, ears
  [union(seg(36.6,29,38.4,22,1.3),seg(38.4,22,36.8,17.4,1.3)),'O'],
  [ellipse(26,24,10,8.6),'O'],
  [ellipse(26,23.6,3.9,5),'C'],
  [union(ellipse(26,13.4,9.2,7),poly([[18,11],[18.4,2.8],[23.6,7.4]]),poly([[34,11],[33.6,2.8],[28.4,7.4]])),'O'],
  [union(poly([[19.4,8.4],[19.6,5.4],[22,7.4]]),poly([[32.6,8.4],[32.4,5.4],[30,7.4]])),'P'],
  [ellipse(26,16.4,3.8,2.5),'C'],
  // the desk in front
  [box(20,32.5,20,1.4),'A'],
 ]);
 // keyboard with three rows of keys and a space bar, two paws on it
 over(g,15,26,[
'DDDDDDDDDDDDDDDDDDDDDDD',
'DSSDSSDSSDSSDSSDSSDSSDD',
'DDSSDSSDSSDSSDSSDSSDSSD',
'DSSDSSSSSSSSSSSSSSSDSSD',
'DDDDDDDDDDDDDDDDDDDDDDD',
 ]);
 over(g,18,26,['.OOOO.','OOOOOO','OEOOEO']);
 over(g,30,26,['.OOOO.','OOOOOO','OEOOEO']);
 // tabby stripes on the forehead and the sides
 put(g,'E',[[25,7],[26,7],[25,8],[26,8],[22,8],[29,8],[17,21],[17,22],[18,23],[34,21],[34,22],[33,23]]);
 // focused eyes glancing at the screen, nose, mouth and the tip of the tongue
 put(g,'D',[[20,12],[21,12],[22,12],[20,13],[21,13],[22,13],[29,12],[30,12],[31,12],[29,13],[30,13],[31,13]]);
 put(g,'C',[[22,12],[31,12]]);
 put(g,'E',[[20,10],[21,10],[22,11],[31,10],[30,10],[29,11]]);
 put(g,'P',[[25,15],[26,15]]);
 put(g,'D',[[25,16],[26,16],[24,17],[27,17]]);
 put(g,'P',[[25,17],[26,17],[25,18]]);
 return str(trim(g));}

export default {theme:'batch-0061',pictures:[
 {title:'Паровозик',forLevel:76,family:'Наземный транспорт',subject:'паровозик с клубом дыма из трубы, кабиной и красными колёсами',source:'оригинальный',rows:locomotive()},
 {title:'Подводная лодка',forLevel:77,family:'Водный транспорт',subject:'круглая жёлтая подводная лодка с иллюминаторами и перископом под водой, рыбки и пузыри',source:'оригинальный',rows:submarine()},
 {title:'Самолётик',forLevel:78,family:'Воздушный транспорт',subject:'маленький винтовой самолёт с пилотом в кабине среди облаков',source:'оригинальный',rows:plane()},
 {title:'Компас',forLevel:79,family:'Багаж и навигация',subject:'круглый латунный компас с красно-белой стрелкой и петлёй крышки',source:'оригинальный',rows:compass()},
 {title:'Кот за клавиатурой',forLevel:80,family:'Мемные животные и бытовые ситуации',subject:'кот за клавиатурой у монитора, сосредоточенный, лапы на клавишах',source:'оригинальный',rows:cat()},
]};

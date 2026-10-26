// Batch 0011, part 06: L36–L40 (retro radio, medallion, potion cauldron, ghost with a cup, sushi train).
import {circle,ellipse,box,poly,union,cut,meet} from '../../../../../tools/gameplay-pilot-v9/draw.mjs';
import {raster,fit,str,put,ring4,scale,seg} from '../helpers.mjs';
const draw=(w,h,k,layers)=>raster(Math.round(w*k),Math.round(h*k),'.',layers.map(([s,c])=>[scale(k,s),typeof c==='function'?(x,y,u)=>c(x/k,y/k,u):c]));
const R=(W,H,layers)=>raster(W,H,'.',layers);
const trim=g=>{while(g.length&&g[0].every(c=>c==='.'))g.shift();while(g.length&&g[g.length-1].every(c=>c==='.'))g.pop();
 while(g.every(r=>r[0]==='.'))g.forEach(r=>r.shift());while(g.every(r=>r[r.length-1]==='.'))g.forEach(r=>r.pop());return g;};

// 1. Retro table radio: domed wooden body with a dark rim, cream grille with slats, a yellow dial with a needle, two knobs.
function radio(){
 const W=36,H=28;
 const dome=(cx,cy,hw,hh,top,r)=>union(box(cx,cy+top/2,hw,hh-top/2,r),ellipse(cx,cy-hh+top,hw,top));
 const body=dome(18,13,16.5,11.5,6.5,3);
 const inner=dome(18,12.6,15,10.1,5.5,2.5);
 const grille=dome(11.5,13,7,7.5,3,1);
 const cloth=dome(11.5,13.2,5.5,6.1,2.5,0.5);
 const g=R(W,H,[
  [body,'E'],
  [inner,'A'],
  [box(18,22.5,15,1.5,1),'E'],                      // base trim
  [box(6.5,25.5,2.5,1.5,0),'E'],[box(29.5,25.5,2.5,1.5,0),'E'], // feet
  [grille,'E'],                                    // grille frame
  [cloth,'C'],                                     // grille cloth
  [meet(cloth,(x,y)=>(Math.floor(y)%3===0)?-1:1),'A'], // slats
  [circle(26.5,10,4.9),'O'],                       // dial bezel
  [circle(26.5,10,3.7),'Y'],                       // dial face
  [circle(23.5,18.5,2.3),'D'],[circle(29.5,18.5,2.3),'D'], // knobs
 ]);
 put(g,'D',[[26,10],[27,9],[28,8],[29,7]]);        // needle
 put(g,'D',[[25,10]]);
 put(g,'C',[[23,17],[29,17]]);                     // knob highlights
 return str(trim(g));
}

// 2. Golden medallion: a loop on top, a gold disc with an orange rim, a red gem, two notched ribbon tails behind.
function medallion(){
 const W=22,H=26;
 const g=R(W,H,[
  [circle(11,12,9),'O'],                           // rim
  [circle(11,12,7),'Y'],                           // disc
  [poly([[11,7.2],[15.2,12],[11,16.8],[6.8,12]]),'R'], // gem
 ]);
 put(g,'O',[[9,0],[10,0],[11,0],[12,0],[8,1],[9,1],[12,1],[13,1],[8,2],[9,2],[12,2],[13,2]]); // loop
 put(g,'C',[[10,9],[9,10],[10,10]]);               // gem shine
 const tail={18:[4,6],19:[4,7],20:[3,7],21:[3,7],22:[2,6],23:[2,6],24:[1,5],25:[1,5]};
 for(const [y,[a,b]] of Object.entries(tail))for(let x=a;x<=b;x++){if(+y>=24&&x===3)continue;for(const u of [x,21-x])if(g[y][u]==='.')g[y][u]='N';}
 return str(trim(g));
}

// 3. Black cauldron with bubbling green potion, a wooden spoon and big bubbles.
function cauldron(){
 const W=40,H=38;
 const g=R(W,H,[
  [seg(28,16,34.5,3,1.4),'A'],                      // spoon handle (behind the rim)
  [ellipse(34.8,2.8,2.1,2.4),'A'],
  [box(9.5,35.5,2.5,2.5,0.5),'D'],[box(30.5,35.5,2.5,2.5,0.5),'D'], // feet
  [ellipse(20,25.5,16.5,10.5),'D'],                // pot
  [meet(cut(ellipse(20,25.5,14.5,8.5),ellipse(22,24.3,14.5,8.5)),(x,y)=>x<16&&y<30?-1:1),'V'], // shine
  [ellipse(20,14.5,16.5,3),'G'],                   // potion surface
  [ellipse(20,14,12.5,1.6),'L'],
  [box(20,17,19.5,2,1),'D'],                       // rim
  [circle(12,9,2.8),'G'],[circle(12,9,1.6),'L'],
  [circle(21,5.5,3.6),'G'],[circle(21,5.5,2.3),'L'],
  [circle(17.5,11,1.6),'G'],
  [circle(27,9.5,1.8),'G'],
 ]);
 return str(trim(g));
}

// 4. A friendly white ghost holding out a cup of tea with steam.
function ghost(){
 const W=38,H=35;
 const wave=(x,y)=>y-(30+2*Math.cos((x-2)*Math.PI/4));
 const shape=(dx)=>(x,y)=>union(circle(14,13,12),meet(box(14,22,12,12,0),wave))(x+dx,y);
 const bodyS=shape(0);
 const g=R(W,H,[
  [bodyS,'C'],
  [meet(cut(bodyS,shape(3)),(x,y)=>y-28.5),'S'],                        // shaded far side
  [seg(23,22.5,27,22.5,1.7),'C'],                   // arm
  [box(31,23.5,4.5,4.5,1.5),'P'],                  // cup
  [cut(circle(36,23.5,2.5),circle(36,23.5,1)),'P'],// handle
  [box(31,19.6,4.5,0.7,0),'A'],                    // tea
  [box(26.2,23.5,1.7,1.7,1),'C'],                   // hand
  [(x,y)=>(y>10&&y<18.5)?Math.abs(x-29-0.9*Math.sin(y*0.9))-1:1,'S'], // steam
  [(x,y)=>(y>12&&y<18.5)?Math.abs(x-33.5-0.9*Math.sin(y*0.9))-1:1,'S'],
 ]);
 put(g,'D',[[9,12],[9,13],[10,12],[10,13],[16,12],[16,13],[17,12],[17,13]]); // eyes
 put(g,'D',[[11,16],[12,17],[13,17],[14,17],[15,16]]); // smile
 put(g,'K',[[7,15],[8,15],[18,15],[19,15]]);      // cheeks
 for(let x=0;x<g[0].length;x++)if(g[31][x]!=='.'&&g[31][x-1]==='.'&&g[31][x+1]==='.')g[31][x]='.'; // lone wave tips
 return str(trim(g));
}

// 5. Sushi train: a salmon nigiri wagon, a standing maki wagon, a locomotive (rice cab under a tamago roof, lying maki
// boiler with a funnel), wooden chassis and wheels.
function sushiTrain(){
 const W=44,H=37;
 const salmon=(x,y)=>((x+y)%4===0)?'C':'O';
 const ae=(x,y)=>y>=29?'E':'A';
 const g=R(W,H,[
  // wagon 1: salmon nigiri
  [box(5.5,20.5,5.2,6.5,2),'C'],
  [box(5.5,11.5,6,3.5,2),salmon],
  // wagon 2: standing maki roll
  [union(box(17.5,18.25,5.5,7.25,0),ellipse(17.5,25.5,5.5,1.3)),'D'],
  [ellipse(17.5,11.5,5.5,2.6),'C'],[ellipse(17.5,11.5,2.6,1.3),'R'],
  // locomotive
  [box(38,19,5.5,7.6,2),'D'],                      // boiler: lying maki
  [ellipse(41.3,19,2.1,5.5),'C'],[ellipse(41.3,19,1,2.7),'O'], // cut end: rice and salmon
  [box(37,8.5,1.5,3,0),'D'],[box(37,5,2.5,1,0.4),'C'], // funnel
  [box(29.5,15.25,3.5,11.75,1),'C'],                   // cab: rice block
  [box(29.5,19.5,3.5,1,0),'D'],                        // nori belt
  [box(29.5,3,4.5,1.6,1),'Y'],                         // tamago roof
  [box(29.5,10,1.5,2,0),'D'],                          // window
  // chassis
  [box(5.5,28.5,5.5,1.6,0),ae],[box(17.5,28.5,5.5,1.6,0),ae],[box(34,28.5,9.5,1.6,0),ae],
 ]);
 put(g,'E',[[11,28],[23,28],[24,28]]);     // couplers
 const stamp=(rows,x0,y0)=>rows.forEach((r,dy)=>[...r].forEach((c,dx)=>{if(c!=='.')g[y0+dy][x0+dx]=c;}));
 const wheel=['.DDD.','DDDDD','DDCDD','DDDDD','.DDD.'];
 const big=['..DDD..','.DDDDD.','DDDDDDD','DDDCDDD','DDDDDDD','.DDDDD.','..DDD..'];
 for(const x0 of [0,6,12,18])stamp(wheel,x0,30);
 for(const x0 of [26,35])stamp(big,x0,30);
 return str(trim(g));
}

export default {theme:'batch-0011',pictures:[
 {title:'Ретро-радио',forLevel:36,family:'Ретро-техника и ностальгия',subject:'ретро-радио',source:'оригинальный',rows:radio()},
 {title:'Медальон',forLevel:37,family:'Фигуры, знаки и эмблемы',subject:'медальон',source:'оригинальный',rows:medallion()},
 {title:'Котёл с зельем',forLevel:38,family:'Магические предметы',direction:'Домашняя магия',subject:'котёл с зельем',source:'оригинальный',rows:cauldron()},
 {title:'Привидение с чашкой',forLevel:39,family:'Волшебные существа',direction:'Уютная мистика',subject:'привидение с чашкой',source:'оригинальный',rows:ghost()},
 {title:'Поезд из суши',forLevel:40,family:'Блюда и напитки',direction:'Кулинарная фантастика',subject:'поезд из суши',source:'оригинальный',rows:sushiTrain()},
]};

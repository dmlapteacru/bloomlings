// Batch 0061, part 10: L106–L110, cartoon universes (Mike Wazowski, Totoro in the rain, WALL-E, Nemo, the Up house).
import {circle,ellipse,box,poly,union,cut,meet} from '../../../../../tools/gameplay-pilot-v9/draw.mjs';
import {raster,fit,str,put,ring4,scale,seg} from '../helpers.mjs';
const draw=(w,h,k,layers)=>raster(Math.round(w*k),Math.round(h*k),'.',layers.map(([s,c])=>[scale(k,s),typeof c==='function'?(x,y,u)=>c(x/k,y/k,u):c]));

// 1. Mike Wazowski: a round green ball with one big eye, two small horns, a wide grin, thin arms and legs.
function mike(){
 const body=circle(18,17,13);
 const g=draw(36,40,1,[
  [union(poly([[10.5,7],[13.5,5],[11,1.2]]),poly([[22.5,5],[25.5,7],[25,1.2]])),'C'],   // horns
  [union(seg(7,20,2.5,27,1.1),seg(29,20,33.5,27,1.1)),'G'],                            // arms
  [union(circle(2.5,28,1.8),circle(33.5,28,1.8)),'G'],                                  // hands
  [union(seg(13,28,12.5,35,1.3),seg(23,28,23.5,35,1.3)),'G'],                           // legs
  [union(ellipse(11.5,36.3,3.2,1.5),ellipse(24.5,36.3,3.2,1.5)),'M'],                   // feet
  [body,'G'],
  [cut(body,circle(16.3,15.3,13)),'M'],                                                 // shade, lower right
  [circle(18,13,6.6),'C'],[circle(18,13,3.6),'B'],[circle(18,13,1.7),'D'],
  [meet(cut(ellipse(18,21.5,8.5,5),ellipse(18,19.5,9.5,4.4)),box(18,24,9,4)),'D'],   // grin
  [meet(ellipse(18,24.6,3.2,1.4),cut(ellipse(18,21.5,7.5,4),ellipse(18,19.5,9.5,4.4))),'P'],  // tongue
 ]);
 put(g,'C',[[16,10],[17,10],[16,11]]);                                                  // gleam
 return str(g);}

// 2. Totoro in the rain: the big blue-grey Totoro with a pale belly, chevrons and pointed ears, a leaf over its head.
function totoro(){
 const W=34,H=37;
 const rain=(x,y)=>((x*2+y)%9===0&&y%6<4)?'B':'S';
 const body=union(ellipse(17,26.5,13.5,10),ellipse(17,19,10.5,10));
 const ears=union(poly([[9,14],[13.5,12],[10.5,5]]),poly([[20.5,12],[25,14],[23.5,5]]));
 const leaf=union(ellipse(18,4.2,12,3),poly([[6,4.2],[2.5,5.2],[6.5,6]]));
 const g=draw(W,H,1,[
  [box(W/2,H/2,W/2,H/2),rain],
  [box(W/2,35.5,W/2,1.5),'M'],                                // wet grass
  [seg(18,4,28,15,0.6),'M'],                                  // leaf stalk
  [ears,'N'],
  [body,'N'],
  [seg(26,21,28.5,14.5,1.6),'N'],                             // raised arm holding the stalk
  [circle(28.5,14,1.9),'N'],
  [leaf,'G'],[cut(leaf,ellipse(17,3.4,12.5,2.7)),'M'],[box(17,4.6,9,.45),'M'],
  [ellipse(17,27.5,10,8),'C'],                               // belly
  [union(ellipse(11.5,35.2,2.8,1.2),ellipse(22.5,35.2,2.8,1.2)),'N'],   // feet
 ]);
 // chevrons on the belly
 const chev=(cx,cy)=>put(g,'N',[[cx-1,cy+1],[cx,cy],[cx+1,cy+1]]);
 chev(13,22);chev(17,21);chev(21,22);chev(11,25);chev(15,25);chev(19,25);chev(23,25);
 // eyes, nose, whiskers
 put(g,'C',[[11,15],[12,15],[11,16],[12,16],[22,15],[23,15],[22,16],[23,16]]);
 put(g,'D',[[12,16],[22,16]]);
 put(g,'D',[[16,15],[17,15],[18,15],[17,16]]);
 put(g,'D',[[4,17],[5,17],[6,17],[4,19],[5,19],[6,19],[28,17],[29,17],[30,17],[28,19],[29,19],[30,19]]);
 return str(g);}

// 3. WALL-E: the boxy yellow robot on caterpillar tracks, binocular eyes on a neck, holding a small green sprout.
function walle(){
 const W=40,H=37;
 const trackL=poly([[3,21],[11,21],[12.5,35],[1.5,35]]),trackR=poly([[29,21],[37,21],[38.5,35],[27.5,35]]);
 const treads=(x,y)=>(Math.floor(y)%3===0)?'E':'D';
 const g=draw(W,H,1,[
  [union(trackL,trackR),treads],
  [union(circle(7,25.5,2.2),circle(7,31,2.2),circle(33,25.5,2.2),circle(33,31,2.2)),'A'],
  [box(20,7.5,1.2,4),'D'],                                   // neck
  [box(20,12,3.5,1.2,.4),'A'],                               // neck joint
  [union(box(13.5,5.5,5.5,3.8,1.4),box(26.5,5.5,5.5,3.8,1.4)),'C'],  // eye housings
  [union(box(13.5,5.5,4.5,2.8,1),box(26.5,5.5,4.5,2.8,1)),'D'],
  [union(circle(14,5.5,2.2),circle(26,5.5,2.2)),'N'],        // lenses
  [box(20,22,9.5,8.5,.6),'Y'],                                // body
  [box(28.3,22,1.2,8.5),'O'],                                 // shade side
  [box(19,15.6,8.5,1.2),'O'],                                 // top panel
  [box(20,29.5,9.5,1),'A'],                                   // lower seam
  [union(box(8.5,23,2.6,1.3,.4),box(31.5,23,2.6,1.3,.4)),'A'],  // arm stubs
  [union(seg(9,23,15,25.5,1.2),seg(31,23,25,25.5,1.2)),'A'],   // forearms reaching in
  [union(box(15.5,25.5,1.6,1.6,.4),box(24.5,25.5,1.6,1.6,.4)),'D'], // claws
  [box(20,26.2,3.2,2.2,.4),'A'],                              // soil clod / shoe
  [box(20,22,.7,3.6),'M'],                                    // sprout stem
  [union(ellipse(17.3,19.6,2.6,1.4),ellipse(22.7,19.6,2.6,1.4)),'G'],
 ]);
 put(g,'C',[[13,4],[25,4]]);                                  // lens glints
 put(g,'D',[[12,19],[13,19],[14,19],[15,19],[24,19],[25,19],[26,19],[27,19]]);  // grille hint
 return str(g);}

// 4. Nemo: the orange clownfish with three white black-edged bands and a little lucky fin, by a pink sea anemone.
function nemo(){
 const W=33,H=31;
 const water=(x,y)=>'B';
 const body=ellipse(14,12.5,10.5,6.8);
 const tail=union(poly([[22,12.5],[30,5.5],[31.5,9],[29,12.5],[31.5,16],[30,19.5]]));
 const top=poly([[9,7],[13,2.6],[19,3.5],[21,8]]);
 const low=poly([[11,18],[14,22],[18,18.5]]);
 const band=(cx,w)=>meet(union(body,tail,top,low),(x,y)=>Math.abs(x-cx-(y-12.5)*0.12)-w);
 const g=draw(W,H,1,[
  [box(W/2,H/2,W/2,H/2),water],
  [box(W/2,29.5,W/2,1.5),'Y'],                                // sand
  // anemone: a plum foot and pink fingers with blush tips
  [ellipse(24,28.5,8,2.6),'V'],
  [union(...[17,20,23,26,29,32].map((x,i)=>seg(x,28,x+(i%2?1:-1),21.5+(i%3),1.1))),'P'],
  [union(...[17,20,23,26,29,32].map((x,i)=>circle(x+(i%2?1:-1),21.5+(i%3),1.3))),'K'],
  [union(body,tail,top,low),'D'],
  [cut(body,ellipse(14,12.5,11.5,7.8)),'D'],
  [cut(union(body,tail,top,low),(x,y)=>-((union(body,tail,top,low))(x,y)+1)),'O'],
  [meet(band(8.6,1.8),(x,y)=>(union(body,tail,top,low))(x,y)+1),'D'],
  [meet(band(8.6,1),(x,y)=>(union(body,tail,top,low))(x,y)+1),'C'],
  [meet(band(15.5,2),(x,y)=>(union(body,tail,top,low))(x,y)+1),'D'],
  [meet(band(15.5,1.1),(x,y)=>(union(body,tail,top,low))(x,y)+1),'C'],
  [meet(band(22.3,1.6),(x,y)=>(union(body,tail,top,low))(x,y)+1),'D'],
  [meet(band(22.3,.8),(x,y)=>(union(body,tail,top,low))(x,y)+1),'C'],
  [ellipse(12.5,14.5,2.2,1.3),'O'],                           // the little lucky fin
  [circle(6.8,10.8,2),'C'],[circle(6.4,10.8,1.1),'D'],
 ]);
 put(g,'S',[[4,3],[5,3],[4,4],[5,4],[2,6],[27,1],[28,1],[27,2],[28,2]]);   // bubbles
 return str(g);}

// 5. The Up house: a little colourful house lifted into the sky by a big bunch of many-coloured balloons.
function upHouse(){
 const W=34,H=40;
 const cols=['R','Y','B','G','P','O'];
 const balloons=[];
 const rowsY=[3.5,7.5,11.5,15.5,19];
 const span=[[9,25],[5,29],[3,31],[5,29],[10,24]];
 rowsY.forEach((y,r)=>{const [a,b]=span[r],off=r%2?2.25:0;for(let x=a+off;x<=b+.01;x+=4.5)balloons.push([x,y]);});
 const g=draw(W,H,1,[
  [box(W/2,H/2,W/2,H/2),'S'],
  [union(ellipse(5,27,4.2,1.8),circle(3.5,25.6,1.8),circle(6.5,25.3,2.1)),'C'],   // cloud left
  [union(ellipse(29,37,4.6,1.9),circle(27.5,35.4,2),circle(30.6,35.2,2.3)),'C'],   // cloud right
  [(x,y)=>{if(y<19||y>27)return 1;let d=9;for(const sx of [11,15,19,23]){const t=(y-19)/(27-19),cx=sx+(19-sx)*t;d=Math.min(d,Math.abs(x-cx)-.5);}return d;},'D'],   // strings
 ]);
 // balloons in order, each a round with its tie
 const order=[0,2,4,1,3,5,2,0,5,3,1,4];
 balloons.forEach(([bx,by],i)=>{const c=cols[order[i%order.length]];
  const s=circle(bx,by,2.35);for(let y=0;y<H;y++)for(let x=0;x<W;x++)if(s(x+.5,y+.5)<0)g[y][x]=c;});
 const house=raster(W,H,'.',[
  [box(20,26,1.3,2),'R'],                                      // chimney
  [poly([[9,30.5],[19,24],[29,30.5]]),'D'],                    // roof
  [box(19,34,8,3.6),'P'],                                      // walls
  [box(19,30.2,8.4,.5),'C'],                                   // eave trim
  [union(box(14,33.5,1.6,1.6),box(24,33.5,1.6,1.6)),'C'],      // windows
  [union(box(14,33.5,.6,.6),box(24,33.5,.6,.6)),'B'],
  [box(19,35,1.5,2.6),'O'],                                    // door
  [box(19,37.9,9.5,.6),'D'],                                   // porch
  [circle(19,27.7,1),'Y'],                                     // attic round window
 ]);
 house.forEach((r,y)=>r.forEach((v,x)=>{if(v!=='.')g[y][x]=v;}));
 return str(g);}

export default {theme:'batch-0061',pictures:[
 {title:'Майк Вазовски',forLevel:106,family:'Волшебные существа',subject:'круглый зелёный одноглазый монстр с маленькими рожками, широкой улыбкой, тонкими руками и ногами',source:'сторонняя вселенная',reference:'Корпорация монстров — Mike Wazowski',rows:mike()},
 {title:'Тоторо под дождём',forLevel:107,family:'Лесные и горные животные',subject:'большой Тоторо со светлым животом и шевронами, острыми ушами, держит лист над головой под дождём',source:'сторонняя вселенная',reference:'Мой сосед Тоторо — Totoro',rows:totoro()},
 {title:'ВАЛЛ-И',forLevel:108,family:'Роботы и механизмы',subject:'квадратный жёлтый робот с глазами-биноклем на шее, гусеницами и руками, держит маленький росток',source:'сторонняя вселенная',reference:'ВАЛЛ-И — WALL-E',rows:walle()},
 {title:'Немо',forLevel:109,family:'Морские и пресноводные обитатели',subject:'оранжево-белая рыба-клоун с чёрной окантовкой и маленьким счастливым плавником у актинии',source:'сторонняя вселенная',reference:'В поисках Немо — Nemo',rows:nemo()},
 {title:'Дом на воздушных шарах',forLevel:110,family:'Воздушный транспорт',subject:'маленький цветной домик, поднятый в небо большой связкой разноцветных воздушных шаров, пара облаков',source:'сторонняя вселенная',reference:'Вверх — the house with balloons',rows:upHouse()},
]};

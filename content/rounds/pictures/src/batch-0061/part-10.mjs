// Batch 0061, part 10: L106–L110, cartoon universes (Mike Wazowski, Totoro in the rain, WALL-E, Nemo, the Up house).
import {circle,ellipse,box,poly,union,cut,meet} from '../../../../../tools/gameplay-pilot-v9/draw.mjs';
import {raster,fit,str,put,ring4,scale,seg} from '../helpers.mjs';
// Drop empty outer rows and columns.
const trim=rows=>{const ys=rows.map((r,y)=>/[^.]/.test(r)?y:-1).filter(y=>y>=0),xs=[...rows[0]].map((_,x)=>rows.some(r=>r[x]!=='.')?x:-1).filter(x=>x>=0);
 return rows.slice(ys[0],ys.at(-1)+1).map(r=>r.slice(xs[0],xs.at(-1)+1));};
const draw=(w,h,k,layers)=>raster(Math.round(w*k),Math.round(h*k),'.',layers.map(([s,c])=>[scale(k,s),typeof c==='function'?(x,y,u)=>c(x/k,y/k,u):c]));

// 1. Mike Wazowski: a round green ball with one big eye, two small horns, a wide grin, thin arms and legs.
function mike(){
 const X=19,body=circle(X,18,13.8);
 const horn=s=>poly([[X+s*8.2,7.2],[X+s*4.4,5.4],[X+s*7.6,1.8]]);
 const g=draw(38,42,1,[
  [union(horn(-1),horn(1)),'C'],                                                        // horns
  [union(seg(X-12,21,X-16.3,28.5,1.1),seg(X+12,21,X+16.3,28.5,1.1)),'G'],               // arms
  [union(circle(X-16.3,29.5,1.8),circle(X+16.3,29.5,1.8)),'G'],                         // hands
  [union(seg(X-5.5,29,X-6,37,1.3),seg(X+5.5,29,X+6,37,1.3)),'G'],                       // legs
  [union(ellipse(X-7,38.4,3.3,1.5),ellipse(X+7,38.4,3.3,1.5)),'M'],                     // feet
  [body,'G'],
  [cut(body,circle(X-1.8,16.2,13.8)),'M'],                                              // shade, lower right
  [circle(X,13.5,7.2),'C'],[circle(X,13.5,4.3),'B'],[circle(X,13.5,2.1),'D'],
  [meet(cut(ellipse(X,22.5,9,5.2),ellipse(X,20.4,10,4.6)),box(X,25,10,4)),'D'],        // grin
  [meet(ellipse(X,25.8,3.4,1.5),cut(ellipse(X,22.5,8,4.2),ellipse(X,20.4,10,4.6))),'P'],  // tongue
 ]);
 put(g,'C',[[17,11],[18,11],[17,12]]);                                                  // gleam
 return str(g);}

// 2. Totoro in the rain: the big blue-grey Totoro with a pale belly, chevrons and pointed ears, a leaf over its head.
function totoro(){
 const W=34,H=37;
 const rain=(x,y)=>{x=Math.floor(x);y=Math.floor(y);return ((x+y)%7===0&&(y+3*Math.floor((x+y)/7))%5<2)?'B':'S';};
 const body=union(ellipse(17,26.5,13.5,10),ellipse(17,19,10.5,10));
 const ears=union(poly([[9,14],[13.5,12],[10.5,5]]),poly([[20.5,12],[25,14],[23.5,5]]));
 const leaf=union(meet(ellipse(18,6.5,12.5,5),box(18,3,14,3.6)),poly([[6.5,5.5],[3,6.8],[7,6.6]]));
 const g=draw(W,H,1,[
  [box(W/2,H/2,W/2,H/2),rain],
  [box(W/2,35.5,W/2,1.5),'M'],                                // wet grass
  [seg(18,4,28,15,0.6),'M'],                                  // leaf stalk
  [ears,'N'],
  [body,'N'],
  [seg(26,21,28.5,14.5,1.6),'N'],                             // raised arm holding the stalk
  [circle(28.5,14,1.9),'N'],
  [leaf,'G'],[meet(leaf,box(18,6,14,.5)),'M'],[union(seg(18,2,18,6,.45),seg(18,4,12,6,.45),seg(18,4,24,6,.45)),'M'],
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
 const W=42,H=40;
 const treads=(x,y)=>(Math.floor(y)%3===0)?'E':'D';
 const housing=(cx,tilt)=>poly([[cx-6,3-tilt],[cx+6,3+tilt],[cx+6,10],[cx-6,10]]);
 const g=draw(W,H,1,[
  [union(box(5.5,30,4.5,9,1.5),box(36.5,30,4.5,9,1.5)),treads],       // tracks
  [box(21,12.5,1.2,3),'D'],                                            // neck
  [union(housing(14,.7),housing(28,-.7)),'C'],                           // eye housings, sad tilt
  [union(meet(housing(14,.7),box(14,6.6,5,2.6,.5)),meet(housing(28,-.7),box(28,6.6,5,2.6,.5))),'D'],
  [union(circle(14,6.6,2.4),circle(28,6.6,2.4)),'N'],             // lenses
  [box(21,9.6,2,1),'A'],                                               // eye hinge
  [box(21,24,12,9,.6),'Y'],                                            // body
  [box(31.5,24,1.5,9),'O'],                                            // shade side
  [box(21,32,12,1),'A'],                                               // rusty foot rim
  [box(21,16.5,12,.5),'O'],                                               // lid seam
  [box(13,19,2.6,1),'D'],                                              // little gauge
  [union(box(8.3,23.5,1.6,1.8,.3),box(33.7,23.5,1.6,1.8,.3)),'A'],     // shoulders
  [union(seg(9,24,15.5,27,1.15),seg(33,24,26.5,27,1.15)),'A'],         // forearms
  [box(21,28.6,3.6,1.8,.5),'E'],                                       // a clod of soil
  [union(box(16.3,27.6,1.4,1.7,.3),box(25.7,27.6,1.4,1.7,.3)),'D'],    // claws
  [box(21,24.5,.8,3.6),'G'],                                           // sprout stem
  [union(ellipse(17.6,21.6,3.4,1.7),ellipse(24.4,21.6,3.4,1.7)),'G'],
 ]);
 put(g,'C',[[13,5],[26,5]]);                                           // lens glints
 put(g,'Y',[[11,19],[12,19],[13,19]]);                                 // gauge bars
 return str(g);}

// 4. Nemo: the orange clownfish with three white black-edged bands and a little lucky fin, by a pink sea anemone.
function nemo(){
 const W=36,H=29;
 const cy=12.5;
 const body=ellipse(15,cy,12,7.8);
 const tail=poly([[25,cy],[32.5,4.8],[34.8,8.5],[32,cy],[34.8,16.5],[32.5,20.2]]);
 const top=poly([[9,6.5],[12.5,1.6],[19.5,2.2],[23,8]]);
 const low=poly([[11,19.5],[14,23.3],[19,19.8]]);
 const fish=union(body,tail,top,low);
 const fins=union(tail,top,low);
 const finEdge=cut(fins,union(body,(x,y)=>fins(x,y)+1.1));      // dark rims on the fin tips only
 // crisp bands: whole columns bent a little round the head, white in the middle, one dark cell each side
 const bands=[[10,1],[18,1.5],[25.5,.5]];
 const banded=(x,y,u)=>{const o=Math.round(1.3*((y-cy+.5)/8)**2);for(const [cx,h] of bands){const d=Math.abs(x+o-cx-(h%1?.5:0))-(h%1?.5:0);
   if(d<=Math.floor(h))return 'C';if(d<=Math.floor(h)+1)return 'D';}return u;};
 const g=draw(W,H,1,[
  [box(W/2,H/2,W/2,H/2),'B'],
  [box(W/2,27.5,W/2,1.5),'Y'],                                 // sand
  [ellipse(26,26.8,8.5,2.2),'V'],                              // anemone foot
  [union(...[19,21.6,24.2,26.8,29.4,32].map((x,i)=>seg(x,26,x+(i%2?1:-1),22.2+(i%3)*.8,1.05))),'P'],
  [union(...[19,21.6,24.2,26.8,29.4,32].map((x,i)=>circle(x+(i%2?1:-1),22.2+(i%3)*.8,1.35))),'K'],
  [fish,'O'],
  [finEdge,'D'],
  [fish,banded],
  [ellipse(14.5,15.6,2,1.5),'D'],[ellipse(14.7,15.4,1.1,.7),'O'],   // the little lucky fin
  [circle(6,11,2.1),'C'],[circle(5.6,11,1.2),'D'],
 ]);
 return str(g);}

// 5. The Up house: a little colourful house lifted into the sky by a big bunch of many-coloured balloons.
function upHouse(){
 const W=34,H=38;
 const cols=['R','Y','B','G','P','O'];
 const balloons=[];
 const rowsY=[2.6,6.6,10.6,14.6,18];
 const span=[[9,25],[5,29],[3,31],[5,29],[10,24]];
 rowsY.forEach((y,r)=>{const [a,b]=span[r],off=r%2?2.25:0;for(let x=a+off;x<=b+.01;x+=4.5)balloons.push([x,y]);});
 const g=draw(W,H,1,[
  [box(W/2,H/2,W/2,H/2),'S'],
  [union(ellipse(5,34,4.2,1.8),circle(3.5,32.6,1.8),circle(6.5,32.3,2.1)),'C'],   // cloud left
  [union(ellipse(29.5,26,4.4,1.9),circle(28,24.4,2),circle(31,24.2,2.2)),'C'],   // cloud right
  [(x,y)=>{if(y<18||y>24)return 1;let d=9;for(const sx of [11,15,19,23]){const t=(y-18)/(24-18),cx=sx+(22.5-sx)*t;d=Math.min(d,Math.abs(x-cx)-.5);}return d;},'D'],   // strings
 ]);
 // balloons in order, each a round with its tie
 // colours in turn, skipping any a touching balloon already wears, so no two neighbours merge
 const worn=[];
 balloons.forEach(([bx,by],i)=>{const near=new Set(balloons.slice(0,i).flatMap(([qx,qy],j)=>Math.hypot(qx-bx,qy-by)<6?[worn[j]]:[]));
  let n=i%cols.length;while(near.has(cols[n]))n=(n+1)%cols.length;const c=worn[i]=cols[n];
  const s=circle(bx,by,2.35);for(let y=0;y<H;y++)for(let x=0;x<W;x++)if(s(x+.5,y+.5)<0)g[y][x]=c;});
 const house=raster(W,H,'.',[
  [box(23,25.5,1,2),'R'],                                      // chimney
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
 {title:'Майк Вазовски',forLevel:106,family:'Волшебные существа',subject:'круглый зелёный одноглазый монстр с маленькими рожками, широкой улыбкой, тонкими руками и ногами',source:'сторонняя вселенная',reference:'Корпорация монстров — Mike Wazowski',rows:trim(mike())},
 {title:'Тоторо под дождём',forLevel:107,family:'Лесные и горные животные',subject:'большой Тоторо со светлым животом и шевронами, острыми ушами, держит лист над головой под дождём',source:'сторонняя вселенная',reference:'Мой сосед Тоторо — Totoro',rows:totoro()},
 {title:'ВАЛЛ-И',forLevel:108,family:'Роботы и механизмы',subject:'квадратный жёлтый робот с глазами-биноклем на шее, гусеницами и руками, держит маленький росток',source:'сторонняя вселенная',reference:'ВАЛЛ-И — WALL-E',rows:trim(walle())},
 {title:'Немо',forLevel:109,family:'Морские и пресноводные обитатели',subject:'оранжево-белая рыба-клоун с чёрной окантовкой и маленьким счастливым плавником у актинии',source:'сторонняя вселенная',reference:'В поисках Немо — Nemo',rows:nemo()},
 {title:'Дом на воздушных шарах',forLevel:110,family:'Воздушный транспорт',subject:'маленький цветной домик, поднятый в небо большой связкой разноцветных воздушных шаров, пара облаков',source:'сторонняя вселенная',reference:'Вверх — the house with balloons',rows:upHouse()},
]};

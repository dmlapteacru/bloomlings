// Batch 0011, part 08: L46–L50 (bathyscaphe, lighthouse in a bulb, walking suitcase, crystal druse, elephant with flower).
import {circle,ellipse,box,poly,union,cut,meet} from '../../../../../tools/gameplay-pilot-v9/draw.mjs';
import {raster,fit,str,put,ring4,scale,seg} from '../helpers.mjs';
const draw=(w,h,k,layers)=>raster(Math.round(w*k),Math.round(h*k),'.',layers.map(([s,c])=>[scale(k,s),typeof c==='function'?(x,y,u)=>c(x/k,y/k,u):c]));

// 1. A round yellow bathyscaphe: propeller behind, big porthole, a lamp throwing a cream beam forward, skids below.
function bathyscaphe(){
 const hull=ellipse(21,18.5,15.5,13);
 const shade=cut(hull,ellipse(19.2,16.8,15.5,13));
 const tower=box(18,5.5,5,3,1);
 const g=draw(44,36,1,[
  [box(5,18.5,3,1.3),'D'],                                   // shaft
  [union(ellipse(2.6,12.5,2,4.4),ellipse(2.6,24.5,2,4.4)),'N'],  // blades
  [box(4,18.5,2.4,2.4,.8),'D'],                              // hub
  [union(box(14,32,1,2.5),box(28,32,1,2.5)),'D'],            // skid legs
  [box(21,34.5,11.5,1,.9),'D'],                              // skid bar
  [tower,'Y'],[box(18,2.2,2.8,1,.5),'D'],                    // tower and hatch
  [box(18,.8,1,1.2),'R'],                                    // beacon
  [hull,'Y'],[shade,'O'],
  [box(21,18.5,15.6,.9),(x,y)=>x<6||x>35?'.':'O'],           // seam band
  [poly([[37,22],[43.6,17],[43.6,33],[37,28]]),'C'],   // light beam
  [box(35.8,25,2,3.2,.6),'D'],                             // lamp
  [circle(23,14,7.6),'D'],[circle(23,14,5.5),'S'],           // porthole
  [union(circle(21,12,1.6),circle(24.6,11,.8)),'C'],         // glint
 ]);
 return str(g);}

// 2. A glass bulb with a tiny striped lighthouse on a rock in a wave, screw base below.
function bulbLighthouse(){
 const bulb=union(ellipse(17,17,16,17),poly([[8.5,28],[25.5,28],[23,35.5],[11,35.5]]));
 const glass=union(ellipse(17,17,14,15),poly([[10.2,27],[23.8,27],[21.4,33.6],[12.6,33.6]]));
 const sea=(x,y)=>glass(x,y)<0&&y>26?-1:1;                         // crests and foam added below
 const tower=poly([[14.2,11.5],[19.8,11.5],[21,26.5],[13,26.5]]);
 const g=draw(34,45,1,[
  [bulb,'B'],[glass,'S'],
  [sea,'N'],
  [union(ellipse(17,27,5.6,2.2),ellipse(18.5,28,3.6,1.8)),'E'],    // rock
  [tower,(x,y)=>Math.floor((y-11.5)/3.5)%2===0?'R':'C'],          // stripes
  [box(17,10,3.8,1,.3),'E'],                                      // gallery
  [box(17,7.7,2.2,1.6),'Y'],                                      // lamp room
  [poly([[13.6,6.5],[20.4,6.5],[17,3.1]]),'R'],                   // roof
  [box(17,38.5,7,3.5),(x,y)=>(y-35)%3===2?'E':'A'],                   // screw base
  [box(17,43,3.5,1,.5),'E'],                                      // contact
 ]);
 put(g,'C',[[7,10],[7,11],[7,12],[8,8],[8,9],[9,7],[10,6]]);      // glass highlight
 const r=g.slice(1,44);
 for(let x=0;x<34;x++){if(r[23][x]==='S'&&[2,3].includes(x%6))r[23][x]='C';if(r[24][x]==='S'&&[1,2,3,4].includes(x%6))r[24][x]='N';}
 return str(r);}

// 3. A cheerful suitcase with a face, stickers and a handle, walking on little legs.
function suitcase(){
 const body=box(17.5,17.5,15.5,11.5,2.2);
 const g=draw(35,37,1,[
  [cut(box(17.5,4.5,6,3,1.5),box(17.5,5.4,3.6,2.2)),'E'],         // handle
  [union(seg(10.5,28,7.5,34,1.4),seg(24.5,28,27.5,33,1.4)),'E'],  // legs mid-step
  [union(ellipse(6.5,34.6,3,1.6),ellipse(29,33.4,3,1.6)),'D'],    // shoes
  [body,'O'],
  [cut(body,box(16,16.3,15.5,11.5,2.2)),'A'],                     // shade side
  [union(box(9.5,17.5,1,11.5),box(25.5,17.5,1,11.5)),'A'],        // straps
  [circle(5.8,9.6,3.4),'B'],[circle(5.8,9.6,1.6),'C'],            // round sticker
  [poly([[29,6],[30.5,8.6],[33.1,8.9],[31.2,10.7],[31.7,13.4],[29,12],[26.3,13.4],[26.8,10.7],[24.9,8.9],[27.5,8.6]]),'Y'], // star sticker
  [box(5.5,24,3,2.4,.5),'P'],                                     // pink sticker
  [union(box(14,15,1,1.5),box(21,15,1,1.5)),'D'],                 // eyes
  [cut(ellipse(17.5,20,4.4,3.2),box(17.5,18,6,2)),'D'],           // smile
 ]);
 put(g,'C',[[13,14],[20,14]]);
 put(g,'P',[[11,19],[12,19],[23,19],[24,19]]);
 return str(g);}

// 4. A cluster of faceted crystals on a rock: light, middle and dark facets.
function druse(){
 const R=Math.PI/180;
 const crystal=(bx,by,deg,L,w,lite,mid,dark)=>{const dx=Math.sin(deg*R),dy=-Math.cos(deg*R),nx=Math.cos(deg*R),ny=Math.sin(deg*R),T=w*1.5;
  const loc=(x,y)=>[(x-bx)*dx+(y-by)*dy,(x-bx)*nx+(y-by)*ny];
  const shape=(x,y)=>{const [u,v]=loc(x,y);if(u<-4||u>L+T)return 1;const lim=u<=L?w:w*(1-(u-L)/T);return Math.abs(v)<lim?-1:1;};
  const col=(x,y)=>{const [u,v]=loc(x+.5,y+.5);const k=u<=L?1:1-(u-L)/T;return mid?(v<-w*.3*k?lite:v>w*.3*k?dark:mid):(v<0?lite:dark);};
  return [shape,col];};
 const cs=[crystal(8.5,29,-30,9,3,'K',null,'V'),crystal(31.5,29,30,9,3,'K',null,'V'),
  crystal(13.5,28,-16,13,3.4,'S','B','N'),crystal(26.5,28,16,14,3.4,'S','B','N'),crystal(20,28,0,18,4.3,'S','B','N')];
 const rock=poly([[2.5,31.5],[6,27.5],[12,26.2],[28,26.2],[34,27.5],[37.5,31.5],[34.5,35],[5.5,35]]);
 const g=draw(40,36,1.02,[...cs,[rock,(x,y)=>(x+.5-20)*.32+(y+.5-31)>1.6?'E':'A']]);
 return str(g.filter(r=>r.some(c=>c!=='.')));}

// 5. A baby elephant raising its trunk and showering a flower with water drops.
function elephant(){
 const body=ellipse(14,25,12.8,9.4);
 const head=circle(24,15.3,9);
 const trunk=union(seg(29.5,20,32.5,14,2.3),seg(32.5,14,33.5,8,2),seg(33.5,8,35.8,4.8,1.7));
 const far=union(box(7.5,31.5,2.6,3.5,.8),box(20,31.5,2.6,3.5,.8));
 const near=union(box(11.5,32.5,2.7,3.5,.8),box(25,32,2.7,3.5,.8));
 const ear=ellipse(18,15.5,6.4,7.6);
 const g=draw(44,37,1,[
  [far,'P'],[body,'K'],[cut(body,ellipse(14.6,23.4,12.8,9.4)),'P'],[near,'K'],
  [box(1.2,23,1.2,.6),'K'],[circle(.8,24.2,1),'P'],                // tail
  [head,'K'],[trunk,'K'],
  [ear,'K'],[ellipse(17.8,15.8,4.5,5.6),'P'],                     // ear and its inside
  [seg(40,36,40,22,.95),'G'],[union(ellipse(37.4,30,2.6,1.3),ellipse(42.4,28,2.4,1.3)),'G'], // stem and leaves
  [union(circle(40,15.8,2.2),circle(37.6,18,2.2),circle(42.4,18,2.2),circle(38.5,20.7,2.2),circle(41.5,20.7,2.2)),'R'],
  [circle(40,18.3,1.6),'Y'],
  [box(22,36,22,.9),'G'],                                         // grass strip
 ]);
 put(g,'D',[[26,13],[26,14],[25,19],[26,20],[27,20]]);
 const drop=['.B.','BBB','BBB'];                                  // falling drops, tail up
 for(const [ox,oy] of [[37,0],[40,3],[41,7],[39,10]])drop.forEach((r,j)=>[...r].forEach((c,i)=>{if(c==='B')g[oy+j][ox+i]='B';}));
 return str(g);}

export default {theme:'batch-0011',pictures:[
 {title:'Батискаф',forLevel:46,family:'Подводные экспедиции',subject:'батискаф с иллюминатором, винтом и лучом прожектора',source:'оригинальный',rows:bathyscaphe()},
 {title:'Маяк внутри лампы',forLevel:47,family:'Свет и домашний декор',direction:'Миры внутри предметов',subject:'лампочка с маяком на скале и волной внутри',source:'оригинальный',rows:bulbLighthouse()},
 {title:'Чемодан-путешественник',forLevel:48,family:'Багаж и навигация',direction:'Ожившие вещи',subject:'весёлый чемодан с лицом, наклейками и ножками',source:'оригинальный',rows:suitcase()},
 {title:'Кристаллическая друза',forLevel:49,family:'Камни, минералы и кристаллы',direction:'Геометрическая фантазия',subject:'друза гранёных кристаллов на камне',source:'оригинальный',rows:druse()},
 {title:'Слонёнок с лейкой',forLevel:50,family:'Саванна и джунгли',subject:'слонёнок поливает цветок из хобота',source:'оригинальный',rows:elephant()},
]};

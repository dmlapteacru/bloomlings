// Batch 0061, part 06: levels 86–90 (volcano and dinosaur, diver, unicorn, crystal ball with a castle, treehouse)
import {circle,ellipse,box,poly,union,cut,meet} from '../../../../../tools/gameplay-pilot-v9/draw.mjs';
import {raster,fit,str,put,ring4,scale,seg} from '../helpers.mjs';
const all=()=>-1;
const below=y0=>(x,y)=>y0-y;   // inside where y > y0
const above=y0=>(x,y)=>y-y0;   // inside where y < y0
const draw=(w,h,k,layers)=>raster(Math.round(w*k),Math.round(h*k),'.',layers.map(([s,c])=>[scale(k,s),typeof c==='function'?(x,y,u)=>c(x/k,y/k,u):c]));
const trim=rows=>{const r=rows.filter(l=>/[^.]/.test(l)),xs=r.flatMap(l=>[...l].map((c,i)=>c==='.'?-1:i).filter(i=>i>=0)),a=Math.min(...xs),b=Math.max(...xs);return r.map(l=>l.slice(a,b+1));};

function volcanoDino(){
 const W=38,H=34;
 const smoke=union(circle(26,5.2,3.2),circle(22.2,3,2.9),circle(29,2.4,2.7),circle(18,1.4,2.6),circle(25,0.4,2.4),circle(13.8,-0.2,2.1));
 const volcano=poly([[7,30],[42,30],[30.5,7.5],[21.5,7.5]]);
 const shade=meet(volcano,poly([[26,7],[43,7],[43,30],[34,30]]));
 const crater=box(26,8.2,4.4,1.1);
 const glow=box(26,8.2,2.6,0.75);
 const lava=union(seg(23,8.5,19.5,15,1),seg(19.5,15,18.5,19,1),seg(29,8.5,31,14,1),seg(31,14,33.5,21,1),seg(26,9,26.5,12,1));
 const ground=below(27.2);
 const body=ellipse(13.5,21.6,7.6,4.7);
 const neck=union(seg(9,20,6,8,1.8),poly([[6.5,22],[13,19],[9,14.5]]));
 const head=ellipse(5,7,3.3,2);
 const tail=union(seg(19.5,21.5,24.5,25,1.9),seg(23.5,24.5,30.5,26.8,1.2),seg(29.5,26.5,33.5,27.3,0.75));
 const legs=union(box(8.5,27.4,1.8,4.2),box(17,27.4,1.8,4.2));
 const farLegs=union(box(11.6,27,1.6,3.9),box(20,27,1.6,3.9));
 const spots=union(circle(11,18.6,1.15),circle(15,18.3,1.2),circle(18.6,19.6,1.05));
 const leaf=(bx,by,ex,ey)=>seg(bx,by,ex,ey,1.05);
 const ferns=union(leaf(2,35,0.5,29),leaf(2,35,4.5,30),
  leaf(28.5,35,25,30.5),leaf(28.5,35,28.5,29.5),leaf(28.5,35,32,30.5),leaf(36.5,35,35,30),leaf(36.5,35,38.5,30.5));
 const g=raster(W,H,'.',[[all,'S'],[smoke,'C'],[volcano,'A'],[shade,'E'],[lava,'R'],[crater,'R'],[glow,'O'],[ground,'L'],
  [tail,'G'],[farLegs,'M'],[body,'G'],[spots,'M'],[legs,'G'],[neck,'G'],[head,'G'],[ferns,'M']]);
 put(g,'E',[[4,6]]);
 return str(g);}

function diver(){
 const W=32,H=33;
 const sand=union(below(28.6),ellipse(24,30,10,3));
 const weed=(x0)=>union(seg(x0,30,x0-1,26,0.8),seg(x0-1,26,x0+0.5,22,0.8),seg(x0+0.5,22,x0-0.5,18,0.8));
 const weeds=union(weed(2.5),weed(5));
 const hose=union(seg(17,6,20,3,0.9),seg(20,3,21,-1,0.9));
 const bubbles=union(circle(7.5,3.5,1.3),circle(5,0.8,1),circle(9.5,0.6,0.8),circle(25,7,1.1),circle(27.5,3,1.4),circle(25,0.5,0.9));
 const helmet=circle(13,8.5,5.2);
 const shine=meet(helmet,cut(circle(11.5,7,5.2),circle(13.3,8.8,5)));
 const port=circle(12.3,9,3.1),glass=circle(12.3,9,2.2);
 const corselet=box(13,14,6.2,1.6,1);
 const suit=union(box(13,18.5,4.6,4,1),seg(9,15.5,7,20.5,1.7),seg(17,15.5,19.5,20,1.7));
 const hands=union(circle(6.6,21.4,1.45),circle(20,20.8,1.45));
 const belt=box(13,21.8,4.7,0.8);
 const legs=union(seg(11,22,9.5,27,1.8),seg(15.5,22,17,27,1.8));
 const boots=union(box(8.8,28.6,2.6,1.3,0.5),box(18,28.6,2.6,1.3,0.5));
 const chest=box(26,25.6,4.2,2.6),lid=poly([[22,21.8],[30.6,21.8],[32,18.6],[23.4,18.6]]),gold=ellipse(26,22.6,3.9,1.9),band=box(26,24.4,4.4,0.6),lock=box(26,25.3,1,1);
 const g=raster(W,H,'.',[[all,'B'],[bubbles,'S'],[sand,'Y'],[weeds,'G'],[hose,'D'],[lid,'A'],[gold,'Y'],[chest,'A'],[band,'D'],[lock,'Y'],
  [legs,'C'],[boots,'D'],[suit,'C'],[hands,'D'],[belt,'D'],[corselet,'O'],[helmet,'O'],[shine,'Y'],[port,'D'],[glass,'S']]);
 return str(g);}

function unicorn(){
 const RB=['R','O','Y','G','B','V'];
 const rb=(x,y)=>RB[((Math.floor((x+y)/2.2)%6)+6)%6];
 const horn=poly([[8.4,9.2],[11.4,8.2],[6,0.6]]);
 const hornStripe=(x,y)=>(Math.floor((x-y*0.6)*0.8)%2===0)?'O':'Y';
 const ear=poly([[12,10],[16.4,10],[15.4,3.2]]);
 const head=(x,y)=>poly([[10,7.6],[14.5,9.5],[15,13.8],[11.5,17],[6,18.2],[3.2,16.8],[3.4,14],[7.4,9.8]])(x,y)-0.6;
 const neck=poly([[13,9],[18.5,11.5],[25,18],[18,26.5],[14.6,21.5],[13.4,16.5]]);
 const body=ellipse(23.5,23,11.2,7);
 const mane=poly([[15,7.2],[18.6,6.8],[23.6,10.6],[28.4,16],[24.4,19.6],[18.8,14.8],[15.6,12],[14.6,10]]);
 const tail=union(seg(33.5,18.5,37,22,2.3),seg(37,22,36.6,29.5,2.5),seg(36.6,29.5,38.2,33.6,1.9));
 const legsNear=union(box(15.8,31,2,5.6),box(28.2,31,2,5.6));
 const legsFar=union(box(19.4,30.6,1.9,5.2),box(31.8,30.6,1.9,5.2));
 const hooves=union(box(15.8,35.8,2.1,1.1),box(28.2,35.8,2.1,1.1),box(19.4,35,2,1.1),box(31.8,35,2,1.1));
 const g=draw(40,38,1.17,[[legsFar,'K'],[tail,rb],[body,'C'],[legsNear,'C'],[hooves,'V'],[neck,'C'],[head,'C'],[mane,rb],[ear,'C'],[horn,hornStripe],
  [box(10,12.6,0.85,0.85),'D'],[circle(4.6,16.2,0.7),'K']]);
 return trim(str(g));}

function crystalBall(){
 const W=34,H=42,cx=17,cy=16,r=15.5;
 const ball=circle(cx,cy,r),inner=circle(cx,cy,r-1);
 const hill=meet(inner,ellipse(cx,28,13.5,6.5));
 const wall=box(cx,21,7.5,2.6);
 const merlons=union(...[-7,-4.5,4.5,7].map(d=>box(cx+d,18,0.9,0.9)));
 const keep=box(cx,17.6,3.4,6),towerL=box(cx-6.4,18.6,2.1,5),towerR=box(cx+6.4,18.6,2.1,5);
 const roofs=union(poly([[cx-4.6,12],[cx+4.6,12],[cx,5.2]]),poly([[cx-9.3,14],[cx-3.5,14],[cx-6.4,8.6]]),poly([[cx+3.5,14],[cx+9.3,14],[cx+6.4,8.6]]));
 const door=box(cx,21.6,1.5,2,0.9);
 const windows=union(box(cx,15.2,0.6,1),box(cx-6.4,17.2,0.6,1),box(cx+6.4,17.2,0.6,1));
 const pole=box(cx+0.5,4.2,0.6,1.6),flag=box(cx+2.2,3.4,1.2,0.8);
 const shine=meet(cut(circle(cx,cy,r-2),circle(cx,cy,r-4)),poly([[cx,cy],[cx-30,cy-4],[cx-8,cy-30]]));
 const cup=cut(poly([[cx-10.5,27.6],[cx+10.5,27.6],[cx+6.5,33.5],[cx-6.5,33.5]]),circle(cx,cy-0.2,r-0.2));
 const rim=meet(cup,box(cx,30.6,12,0.7));
 const stem=box(cx,34.8,3,1.6);
 const base=poly([[cx-10.5,40.8],[cx+10.5,40.8],[cx+7,36],[cx-7,36]]);
 const band=box(cx,38.3,8.9,0.65);
 const g=raster(W,H,'.',[[ball,'B'],[inner,'S'],[shine,'C'],[hill,'G'],[wall,'P'],[merlons,'P'],[towerL,'P'],[towerR,'P'],[keep,'P'],[roofs,'V'],[door,'V'],[windows,'V'],[pole,'V'],[flag,'Y'],
  [cup,'A'],[rim,'Y'],[stem,'Y'],[base,'A'],[band,'Y']]);
 return trim(str(g));}

function treehouse(){
 const W=36,H=38;
 const ground=below(33.4);
 const trunk=union(poly([[13,35],[23,35],[20,18],[16,18]]),seg(17,22,7,16,1.4),seg(20,21,28,17,1.3));
 const trunkShade=meet(trunk,poly([[18.5,17],[30,17],[30,36],[21,36]]));
 const canopy=union(circle(18,9.5,9),circle(8.5,11.5,6.6),circle(27.5,11.5,6.6),circle(12,5.5,5.6),circle(24,5.5,5.6));
 const canShade=cut(canopy,union(circle(17.3,8,9),circle(8,10,6.5),circle(26,10,6.5),circle(11,4,5.5),circle(23,4,5.5)));
 const platform=box(22,20.2,7.5,0.8);
 const house=box(22,16.3,5.5,3.3);
 const roof=poly([[15,13.6],[29,13.6],[22,7.5]]);
 const win=box(20,16.3,1.2,1.2),door=box(25,17,1.2,2.4);
 const ladder=(x,y)=>(x===25||x===28)||((y%3)===0)?'E':'.';
 const ladderArea=box(26.5,26.8,1.9,6.2);
 const ropes=union(box(6.5,22,0.6,5),box(10.5,22,0.6,5));
 const seat=box(8.5,27.5,3.2,0.75);
 const g=raster(W,H,'.',[[all,'S'],[canopy,'G'],[canShade,'M'],[ground,'L'],[trunk,'A'],[trunkShade,'E'],[ropes,'D'],[seat,'R'],
  [house,'O'],[roof,'R'],[win,'E'],[door,'E'],[platform,'E']]);
 for(let y=21;y<34;y++)for(const x of [25,28])g[y][x]='E';
 for(let y=22;y<34;y+=3)for(const x of [26,27])g[y][x]='E';
 return str(g);}

export default {theme:'batch-0061',pictures:[
 {title:'Вулкан и динозавр',forLevel:86,family:'Доисторический мир',subject:'длинношеий динозавр среди папоротников на фоне дымящегося вулкана',source:'оригинальный',rows:volcanoDino()},
 {title:'Водолаз',forLevel:87,family:'Подводные экспедиции',subject:'водолаз в круглом латунном шлеме на морском дне с сундучком и пузырями',source:'оригинальный',rows:diver()},
 {title:'Единорог',forLevel:88,family:'Волшебные существа',subject:'белый единорог с радужной гривой и золотым рогом',source:'оригинальный',rows:unicorn()},
 {title:'Хрустальный шар с замком',forLevel:89,family:'Магические предметы',direction:'Миры внутри предметов',subject:'хрустальный шар на резной подставке с крошечным замком внутри',source:'оригинальный',rows:crystalBall()},
 {title:'Дом на дереве',forLevel:90,family:'Сказочные жилища',subject:'домик на большом дереве с лестницей и верёвочными качелями',source:'оригинальный',rows:treehouse()},
]};

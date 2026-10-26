// Batch 0061, part 03: levels 71–75 (teddy bear, windmill on a hill, flower shop, park fountain, stacked gifts)
import {circle,ellipse,box,poly,union,cut,meet} from '../../../../../tools/gameplay-pilot-v9/draw.mjs';
import {raster,fit,str,put,ring4,scale,seg} from '../helpers.mjs';
// crop the empty rows and columns round the subject
const trim=rows=>{const r=rows.filter(l=>/[^.]/.test(l)),xs=r.flatMap(l=>[...l].map((c,i)=>c==='.'?-1:i).filter(i=>i>=0)),a=Math.min(...xs),b=Math.max(...xs);return r.map(l=>l.slice(a,b+1));};
// Paint every empty cell with the environment colour function.
const fill=(g,f)=>{g.forEach((r,y)=>r.forEach((v,x)=>{if(v==='.')r[x]=f(x,y);}));return g;};
const mirror=(f,c)=>(x,y)=>Math.min(f(x,y),f(2*c-x,y));

function teddy(){
 const C0=18;
 const head=circle(18,12,9);
 const ears=mirror(circle(10.5,4.8,3.6),C0),earIn=mirror(circle(10.8,5.2,1.8),C0);
 const body=ellipse(18,29,10.5,10);
 const armL=ellipse(7.8,26,3.4,6.2),armR=ellipse(28.2,26,3.4,6.2),arms=union(armL,armR);
 const armRim=meet(union(ellipse(7.8,26,4.4,7.2),ellipse(28.2,26,4.4,7.2)),body);
 const feet=mirror(ellipse(10,37,5,4),C0),pads=mirror(ellipse(9.4,37.4,2.6,2.6),C0);
 const fur=union(head,ears,body,feet);
 const shade=cut(union(fur,arms),union(circle(16.6,10.6,9.2),ellipse(16.2,27.6,11.2,11),ellipse(6.6,24.8,3.6,6.4),ellipse(27.2,24.8,3.2,6.2),ellipse(9,35.8,5.4,4.3),ellipse(25,35.8,5,4.3),circle(9.6,3.8,3.8),circle(25.6,3.8,3.8)));
 const belly=ellipse(18,30,6.4,6.6);
 const muzzle=ellipse(18,15.2,4.6,3.4);
 const tie=union(poly([[18,20.5],[11.5,17.6],[11.5,23.4]]),poly([[18,20.5],[24.5,17.6],[24.5,23.4]]));
 const patch=box(20.2,31.2,2.4,2.4);
 const g=raster(36,42,'.',[[fur,'A'],[armRim,'E'],[arms,'A'],[shade,'E'],[earIn,'C'],[belly,'C'],[pads,'C'],[muzzle,'C'],[patch,'B'],[tie,'R'],[box(18,20.5,1.2,1.2),'D'],[ellipse(18,13.6,1.9,1.2),'D']]);
 put(g,'D',[[13,10],[13,11],[22,10],[22,11]]);
 put(g,'D',[[18,15],[17,16],[19,16]]);
 // stitches round the patch
 put(g,'D',[[17,29],[17,31],[17,33],[23,29],[23,31],[23,33]]);
 return trim(str(g));}

function windmill(){
 const W=36,H=36,hx=18,hy=11.5;
 const tower=poly([[12,31],[24,31],[21.2,12],[14.8,12]]);
 const towerShade=meet(tower,poly([[20.4,31],[24,31],[21.2,12],[19.4,12]]));
 const cap=cut(ellipse(18,12.4,5.2,6.8),box(18,18.4,7,5.2));
 const capShade=cut(cap,ellipse(17,11.6,5,4.8));
 // a sail: the spoke from the hub to the tip, and its cloth on one side
 const sail=a=>{const ux=Math.cos(a),uy=Math.sin(a),vx=-uy,vy=ux,p=(r,s)=>[hx+ux*r+vx*s,hy+uy*r+vy*s];
  return {spoke:seg(hx,hy,hx+ux*15.6,hy+uy*15.6,.8),frame:poly([p(3.6,0),p(15.6,0),p(15.6,4.6),p(3.6,4.6)]),cloth:poly([p(4.8,1),p(14.6,1),p(14.6,3.6),p(4.8,3.6)])};};
 const sails=[-3*Math.PI/4,-Math.PI/4,Math.PI/4,3*Math.PI/4].map(sail);
 const door=union(box(18,28.5,2.1,2.5),circle(18,26,2.1));
 const g=raster(W,H,'.',[
  [tower,'C'],[towerShade,'A'],[cap,'R'],
  [door,'E'],[box(18,20.5,1.1,1.6),'D'],
  ...sails.map(s=>[s.frame,'A']),...sails.map(s=>[s.cloth,'C']),...sails.map(s=>[s.spoke,'E']),
  [circle(hx,hy,1.7),'D'],
 ]);
 return str(fill(g,(x,y)=>{const cx=x+.5,cy=y+.5,top=26.5+Math.pow((cx-18)/18,2)*5;
  if(cy>top)return (cy>top+2&&y>=30&&(y-30)%3===2)?'M':'G';
  return 'S';}));
}

function shop(){
 const W=34,H=37;
 const flower=(cx,cy,c)=>[[box(cx,cy,1,1),c]];
 const bucket=cx=>poly([[cx-2.6,30],[cx+2.6,30],[cx+2,35],[cx-2,35]]);
 const g=raster(W,H,'.',[
  [box(17,19,16,17),'M'],
  [box(17,1.5,17,1.5),'A'],
  [union(box(17,5.5,17,3),...Array.from({length:6},(_,i)=>circle(2.5+i*5.8,8.6,2.9))),(x,y)=>Math.floor((x+1)/3)%2?'C':'R'],
  // the shop window with a planter box of flowers on its sill
  [box(10,17.5,7.5,5),'C'],[box(10,17.5,6.2,3.8),'S'],[box(10,15.5,6.2,.5),'C'],[box(10,17.5,.5,3.8),'C'],
  [ellipse(10,21.4,6.6,1.8),'G'],
  ...flower(5,20,'P'),...flower(8.5,19.5,'Y'),...flower(11.5,19.5,'R'),...flower(15,20,'P'),
  [box(10,23.3,7.8,1.4),'A'],
  // the door
  [box(26.5,23,5,11),'C'],[box(26.5,23.5,4,10.5),'A'],[box(26.5,18.2,2.6,4),'S'],[box(26.5,28.6,2.6,2.6),'C'],[box(26.5,28.6,1.6,1.6),'A'],
  // two buckets of flowers and a pot on the pavement
  [union(ellipse(4.5,29.2,3,2),ellipse(15.5,29.2,3,2)),'G'],
  ...flower(3,27.8,'R'),...flower(6,27.8,'Y'),...flower(4.5,26,'P'),
  ...flower(14,27.8,'P'),...flower(17,27.8,'C'),...flower(15.5,26,'R'),
  [bucket(4.5),'S'],[bucket(15.5),'S'],[box(4.5,30.5,2.6,.5),'C'],[box(15.5,30.5,2.6,.5),'C'],
  [union(circle(10,27.4,2.4),circle(8.8,29,1.8),circle(11.2,29,1.8)),'G'],[poly([[7.8,30.5],[12.2,30.5],[11.6,35],[8.4,35]]),'O'],
  [box(17,36,17,1),'A'],
 ]);
 put(g,'Y',[[23,24],[23,25]]);
 return str(g);}

function fountain(){
 const W=30,H=30;
 const jet=meet(cut(ellipse(15,11,7.2,7.6),ellipse(15,11,5.8,6.2)),box(15,5,9,5.4));
 const fall=(x,y)=>(Math.abs(x-15)>7.6&&y>12.4&&y<20.6)?cut(ellipse(15,21,10.8,9),ellipse(15,21,9.4,7.8))(x,y):1;
 const bush=cx=>union(circle(cx,17,3.4),circle(cx-2.4,19.4,2.6),circle(cx+2.4,19.4,2.6),circle(cx,20,3));
 const g=raster(W,H,'.',[
  [union(bush(3.2),bush(26.8)),'M'],
  [box(15,24.5,12.5,3.2),'C'],[box(15,27.3,12.5,.5),'A'],
  [(x,y)=>(y>22.5&&y<27&&Math.floor(x)%5===2)?-1:1,'A'],
  [ellipse(15,21,13.4,2.4),'C'],[ellipse(15,21,11.2,1.2),'B'],
  [box(15,17,1.6,4.2),'C'],[box(16,17,.6,4.2),'A'],
  [cut(ellipse(15,11.4,7.4,3.4),box(15,8,9,3.4)),'C'],[cut(ellipse(15,11.4,7.4,3.4),union(box(15,8,9,3.4),ellipse(14.2,11,7.2,3))),'A'],
  [ellipse(15,11.4,6.2,.9),'B'],
  [box(15,7,.9,4.6),'B'],[jet,'B'],[fall,'B'],
 ]);
 return str(fill(g,(x,y)=>(y+.5)>15?'G':'S'));
}

function gifts(){
 const bow=(cx,cy,rx,ry,c,k)=>[[union(ellipse(cx-rx*.95,cy,rx,ry),ellipse(cx+rx*.95,cy,rx,ry),poly([[cx-.6,cy+.5],[cx-2.6,cy+ry+2.4],[cx-1.2,cy+ry+2.4],[cx,cy+1]]),poly([[cx+.6,cy+.5],[cx+2.6,cy+ry+2.4],[cx+1.2,cy+ry+2.4],[cx,cy+1]])),c],[box(cx,cy,1,1),k]];
 const g=raster(32,42,'.',[
  [box(16,34.5,13,6),'N'],[box(16,26.8,14,1.8),'B'],[box(24,34.5,1.5,6),'Y'],[box(24,26.8,1.5,1.8),'Y'],
  ...bow(24,23.3,2.1,1.5,'Y','O'),
  [box(12,20.5,8,4.6),'R'],[box(12,15.2,9,1.6),'P'],[box(6.5,20.5,1.3,4.6),'C'],[box(6.5,15.2,1.3,1.6),'C'],
  ...bow(6.5,11.6,1.9,1.4,'C','Y'),
  [box(16,9.5,5.5,4),'Y'],[box(16,5.3,6.2,1.2),'O'],[box(16,9.5,1,4),'R'],[box(16,5.3,1,1.2),'R'],
  ...bow(16,2.2,2.6,1.7,'R','P'),
 ]);
 return trim(str(g));}

export default {theme:'batch-0061',pictures:[
 {title:'Плюшевый мишка',forLevel:71,family:'Игрушки и настольные игры',subject:'сидящий плюшевый мишка с галстуком-бабочкой и заплаткой на животе',source:'оригинальный',rows:teddy()},
 {title:'Ветряная мельница',forLevel:72,family:'Дома и архитектура',subject:'ветряная мельница с четырьмя крыльями на холме, небо и поле вокруг',source:'оригинальный',rows:windmill()},
 {title:'Цветочная лавка',forLevel:73,family:'Магазины и витрины',subject:'фасад маленькой лавки с полосатым навесом, горшками и вёдрами цветов, дверью и витриной',source:'оригинальный',rows:shop()},
 {title:'Фонтан в парке',forLevel:74,family:'Городские пространства',subject:'круглый каменный фонтан со струями воды, газон и два куста вокруг',source:'оригинальный',rows:fountain()},
 {title:'Подарки с бантами',forLevel:75,family:'Ярмарки и праздники',subject:'стопка из трёх подарочных коробок с лентами и бантами',source:'оригинальный',rows:gifts()},
]};

// Batch 0011, part 10: L56–L60 (6–7 meme digits, walnut-shell workshop, raccoon in a scarf, grumpy cloud, gardening robot).
import {circle,ellipse,box,poly,union,cut,meet} from '../../../../../tools/gameplay-pilot-v9/draw.mjs';
import {raster,fit,str,put,ring4,scale} from '../helpers.mjs';
const draw=(w,h,k,layers)=>raster(Math.round(w*k),Math.round(h*k),'.',layers.map(([s,c])=>[scale(k,s),typeof c==='function'?(x,y,u)=>c(x/k,y/k,u):c]));
// a colour that turns to `dark` in a band of width s on the far (lower right) side of shape f
const shade=(f,lit,dark,s=1.6,dx=1,dy=1)=>(x,y)=>f(x+.5+s*dx,y+.5+s*dy)>=0?dark:lit;
const seg=(x1,y1,x2,y2,r)=>(x,y)=>{const ex=x2-x1,ey=y2-y1,t=Math.max(0,Math.min(1,((x-x1)*ex+(y-y1)*ey)/(ex*ex+ey*ey)));return Math.hypot(x-x1-ex*t,y-y1-ey*t)-r;};
const rows=(g,list)=>{for(const [y,x,s] of list)for(let i=0;i<s.length;i++)if(s[i]!==' ')g[y][x+i]=s[i];return g;};

// 1. «6–7»: two chunky block digits as characters with faces and stubby legs; the 6 shrugs.
const ext=(f,dx=1,dy=1)=>(x,y)=>f(x-dx,y-dy);
function sixSeven(){
 const belly=circle(13,20.5,8.6),hole=circle(13.4,23.3,2.9);
 const hook=meet(cut(circle(17.5,14.8,12.6),circle(17.5,14.8,6.4)),(x,y)=>Math.max(y-15,x-21));
 const six=cut(union(belly,hook),hole);
 const seven=union(box(34,7,8.1,4.5,1),poly([[35,10],[42.1,10],[32.9,30.2],[25.8,30.2]]));
 const arms=union(seg(5.5,19,2.3,22.2,1),seg(2.3,22.2,1.5,17.4,1),box(1.5,16,1.5,1),
  seg(21.5,19,24.7,22.2,1),seg(24.7,22.2,25.5,17.4,1),box(25.5,16,1.5,1));
 const legs=union(box(10,30.5,1,1.6),box(16,30.5,1,1.6),box(9.5,32.5,2,.9,.5),box(16.5,32.5,2,.9,.5),
  box(28,30.5,1.1,1.6),box(32,30.5,1.1,1.6),box(27.5,32.5,2,.9,.5),box(32.5,32.5,2,.9,.5));
 const g=raster(44,34,'.',[[legs,'D'],[arms,'D'],[ext(six),'A'],[six,'O'],[ext(seven),'N'],[seven,'B']]);
 rows(g,[
  // the 6: brows raised high, wide eyes, a crooked "dunno" mouth
  [12,7,'DDD'],[12,14,'DDD'],
  [14,8,'DD     DD'],[15,8,'DD     DD'],[14,8,'C'],[14,15,'C'],
  [18,10,'DDD'],[17,13,'DD'],
  // the 7: content eyes and a smile on its bar
  [5,29,'DD     DD'],[6,29,'DD     DD'],[5,29,'C'],[5,36,'C'],
  [8,30,'D     D'],[9,31,'DDDDD'],
 ]);
 return str(g);}

// 2. «Мастерская в ореховой скорлупе»: a half walnut lying like a bowl, a tiny workshop standing in it.
function walnutShop(){
 const cx=22,ry=24;
 const bowl=meet(union(ellipse(cx,ry,21.6,14),poly([[cx-3,36],[cx,39.2],[cx+3,36]])),(x,y)=>ry-y);
 const rimAll=ellipse(cx,ry,21.6,2.7),inner=ellipse(cx,ry+.2,19.6,1.7);
 // walnut ridges: wavy lobes that run down the shell to its tip; light on the upper left
 const shellCol=(x,y)=>{const px=x+.5,py=y+.5,hw=21.6*Math.sqrt(Math.max(.02,1-((py-ry)/14)**2)),u=(px-cx)/hw,v=(py-ry)/14;
  const lobe=Math.abs(((u*3.2+.35*Math.sin(v*11+u*3)+8.5)%1)-.5);if(lobe>.37&&v>.08)return 'E';
  return u<-.2&&v>.2&&v<.6&&lobe<.22?'O':'A';};
 const wall=box(22,14,15.6,10,4),frame=cut(wall,box(22,14.5,13.6,9.4,2.8));
 const g=raster(44,40,'.',[[rimAll,'O'],[inner,'E'],[wall,'C'],[frame,'A'],
  [box(22,19.5,13.5,.6),'O'],[box(22,21,13.5,1),'A'],[box(11,22.5,1,1.6),'A'],[box(33,22.5,1,1.6),'A'],
  [cut(bowl,inner),shellCol],[meet(cut(rimAll,inner),(x,y)=>ry-y),'O']]);
 rows(g,[
  // tools on the wall: a saw, a hammer, a wrench
  [9,10,'AAA'],[10,10,'A.ASSSSSS'],[11,10,'AAASSSSSS'],[12,10,'AAASSSSS'],[13,13,'D D D'],
  [8,19,'DDDDDD'],[9,19,'DDDDDD'],[10,21,'AA'],[11,21,'AA'],[12,21,'AA'],[13,21,'AA'],[14,21,'AA'],[15,21,'AA'],[16,21,'AA'],
  [8,25,'DD.DD'],[9,25,'DD.DD'],[10,25,'DDDDD'],[11,26,'DDD'],[12,26,'DDD'],[13,26,'DDD'],[14,26,'DDD'],[15,25,'DDDDD'],[16,25,'DD.DD'],
  // a desk lamp on the bench: base, arm, red shade, yellow bulb
  [18,32,'DDDDD'],[17,34,'D'],[16,34,'DD'],[15,34,'DD'],[14,35,'DD'],[13,35,'DD'],[12,34,'DD'],[11,33,'DD'],
  [10,31,'RRR'],[11,30,'RRR'],[12,29,'RRRR'],[13,29,'RRRRR'],[14,30,'YYY'],[15,31,'Y'],
  // a little yellow block being carved on the bench
  [17,13,'YYYY'],[18,13,'YYYY'],
 ]);
 return str(g);}

export default {theme:'batch-0011',pictures:[
 {title:'6–7',forLevel:56,family:'Числовой и фразовый абсурд',device:'персонаж из предмета',subject:'цифры 6 и 7 с лицами',
  source:'интернет-мем/тренд',reference:'6–7 / six-seven [T1]',rows:sixSeven()},
 {title:'Мастерская в ореховой скорлупе',forLevel:57,family:'Кафе и мастерские',direction:'Миниатюрные мастерские',
  subject:'мастерская в половинке грецкого ореха',source:'оригинальный',rows:walnutShop()},
]};

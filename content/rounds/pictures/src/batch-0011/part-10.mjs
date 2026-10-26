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

// 2. «Мастерская в ореховой скорлупе»: a half walnut stood on its edge, a tiny workshop inside.
function walnutShop(){
 const cx=22,cy=20;
 const outer=meet(union(ellipse(cx,cy,20.6,17.8),poly([[cx-4.5,4.5],[cx,.4],[cx+4.5,4.5]])),(x,y)=>y-37);
 const rim=meet(ellipse(cx,cy+1.4,16.4,14.6),(x,y)=>y-34.8);
 const hollow=meet(ellipse(cx,cy+1.4,15,13.2),(x,y)=>y-33.6);
 // walnut ridges: a wavy groove running round the shell and short grooves across it; light on the upper left
 const shellCol=(x,y)=>{const px=x+.5,py=y+.5,t=Math.atan2(py-cy,px-cx),d=-outer(px,py),w=rim(px,py);
  const along=Math.abs(d-(w>2.5?2.6:1.9)-.9*Math.sin(t*9));if(along<.55)return 'E';
  if(Math.cos(t*9+1.4)>.93&&d>1.2)return 'E';
  return px+py<30&&d<1.7?'O':'A';};
 const floor=meet(hollow,(x,y)=>y-29.5<0?1:-1);
 const g=raster(44,38,'.',[[outer,shellCol],[rim,'O'],[hollow,'C'],[floor,'E']]);
 rows(g,[
  // the hanging lamp: cord, red shade, yellow bulb
  [8,21,'DD'],[9,21,'DD'],[10,21,'DD'],
  [11,19,'RRRRRR'],[12,18,'RRRRRRRR'],[13,17,'RRRRRRRRRR'],[14,17,'RRRRRRRRRR'],[15,20,'YYYY'],[16,21,'YY'],
  // tools on the wall: a saw at the left, a hammer and a wrench at the right
  [14,7,'AA'],[15,7,'AASSSSSSS'],[16,7,'AASSSSSSS'],[17,7,'AASSSSSSS'],[18,9,'DSDSDSD'],
  [13,25,'DDDDDD'],[14,25,'DDDDDD'],[15,27,'AA'],[16,27,'AA'],[17,27,'AA'],[18,27,'AA'],[19,27,'AA'],[20,27,'AA'],[21,27,'AA'],
  [13,31,'DD DD'],[14,31,'DD DD'],[15,31,'DDDDD'],[16,32,'DDD'],[17,32,'DDD'],[18,32,'DDD'],[19,32,'DDD'],[20,31,'DDDDD'],[21,31,'DD DD'],
  // the workbench with a vise, a little toy block on it, its shadow on the floor
  [22,30,'DDD'],[23,29,'DDDD'],
  [24,8,'AAAAAAAAAAAAAAAAAAAAAAAAAAAA'],[25,8,'AAAAAAAAAAAAAAAAAAAAAAAAAAAA'],[26,8,'EEEEEEEEEEEEEEEEEEEEEEEEEEEE'],
  [22,13,'YYYY'],[23,13,'YYYY'],
  [27,9,'AA'],[28,9,'AA'],[29,9,'AA'],[30,9,'AA'],[31,9,'AA'],[32,9,'AA'],
  [27,33,'AA'],[28,33,'AA'],[29,33,'AA'],[30,33,'AA'],[31,33,'AA'],[32,33,'AA'],
 ]);
 return str(g);}

export default {theme:'batch-0011',pictures:[
 {title:'6–7',forLevel:56,family:'Числовой и фразовый абсурд',device:'персонаж из предмета',subject:'цифры 6 и 7 с лицами',
  source:'интернет-мем/тренд',reference:'6–7 / six-seven [T1]',rows:sixSeven()},
 {title:'Мастерская в ореховой скорлупе',forLevel:57,family:'Кафе и мастерские',direction:'Миниатюрные мастерские',
  subject:'мастерская в половинке грецкого ореха',source:'оригинальный',rows:walnutShop()},
]};

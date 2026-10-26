// Batch 0011, part 10: L56–L60 (6–7 meme digits, walnut-shell workshop, raccoon in a scarf, grumpy cloud, gardening robot).
import {circle,ellipse,box,poly,union,cut,meet} from '../../../../../tools/gameplay-pilot-v9/draw.mjs';
import {raster,fit,str,put,ring4,scale} from '../helpers.mjs';
const draw=(w,h,k,layers)=>raster(Math.round(w*k),Math.round(h*k),'.',layers.map(([s,c])=>[scale(k,s),typeof c==='function'?(x,y,u)=>c(x/k,y/k,u):c]));
// a colour that turns to `dark` in a band of width s on the far (lower right) side of shape f
const shade=(f,lit,dark,s=1.6,dx=1,dy=1)=>(x,y)=>f(x+.5+s*dx,y+.5+s*dy)>=0?dark:lit;
const seg=(x1,y1,x2,y2,r)=>(x,y)=>{const ex=x2-x1,ey=y2-y1,t=Math.max(0,Math.min(1,((x-x1)*ex+(y-y1)*ey)/(ex*ex+ey*ey)));return Math.hypot(x-x1-ex*t,y-y1-ey*t)-r;};
// arc length along a polyline of the point nearest (x,y)
const along=(pts,x,y)=>{let best=1e9,at=0,acc=0;for(let i=1;i<pts.length;i++){const [ax,ay]=pts[i-1],[bx,by]=pts[i],ex=bx-ax,ey=by-ay,L=Math.hypot(ex,ey),t=Math.max(0,Math.min(1,((x-ax)*ex+(y-ay)*ey)/(L*L))),d=Math.hypot(x-ax-ex*t,y-ay-ey*t);if(d<best){best=d;at=acc+t*L;}acc+=L;}return at;};
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
 const cx=22,ry=23,dep=16;
 const lumpy=(x,y)=>ellipse(cx,ry,21.6,dep)(x,y)-.55*Math.max(0,Math.sin(Math.atan2(y-ry,x-cx)*16));
 const bowl=meet(union(lumpy,poly([[cx-3,ry+dep-2],[cx,ry+dep+1.6],[cx+3,ry+dep-2]])),(x,y)=>ry-y);
 const rimAll=ellipse(cx,ry,21.6,2.7),inner=ellipse(cx,ry+.2,19.6,1.7);
 // walnut ridges: wavy lobes that run down the shell to its tip
 const shellCol=(x,y)=>{const px=x+.5,py=y+.5,hw=21.6*Math.sqrt(Math.max(.02,1-((py-ry)/dep)**2)),u=(px-cx)/hw,v=(py-ry)/dep;
  const lobe=Math.abs(((u*3.2+.3*Math.sin(v*10+u*3)+8.5)%1)-.5);return lobe>.36&&v>.12?'E':'A';};
 const wall=box(22,13.5,15,9.5,4),frame=cut(wall,box(22,14,13,8.9,2.6));
 const g=raster(44,41,'.',[[rimAll,'O'],[inner,'E'],[wall,'C'],[frame,'E'],
  [box(22,18.5,16.4,.6),'O'],[box(22,20,16.4,1),'A'],[box(9,22.5,1,2.4),'A'],[box(35,22.5,1,2.4),'A'],
  [cut(bowl,inner),shellCol],[meet(cut(rimAll,inner),(x,y)=>ry-y),'O']]);
 rows(g,[
  // tools on the wall: a saw, a screwdriver, a hammer
  [7,9,'AAAA'],[8,9,'A..ASSSSSSSS'],[9,9,'A..ASSSSSSSS'],[10,9,'AAAASSSSSSS'],[11,13,'DSDSDSD'],
  [14,9,'RRRR'],[15,9,'RRRRSSSSSS'],[16,9,'RRRRSSSSS'],[17,9,'RRRR'],
  [6,20,'DDDDDDD'],[7,20,'DDDDDDD'],[8,20,'DDDDDDD'],[9,22,'AAA'],[10,22,'AAA'],[11,22,'AAA'],[12,22,'AAA'],[13,22,'AAA'],[14,22,'AAA'],[15,22,'AAA'],[16,22,'AAA'],
  // a desk lamp on the bench: base, arm, red shade, yellow bulb
  [17,30,'DDDDDD'],[16,32,'DD'],[15,32,'DD'],[14,33,'DD'],[13,33,'DD'],[12,32,'DD'],[11,31,'DD'],[10,30,'DD'],
  [9,27,'RRR'],[10,26,'RRRR'],[11,26,'RRRRR'],[12,26,'RRRRRR'],[13,27,'YYYY'],[14,28,'YY'],
 ]);
 return str(g);}

// 3. «Енот в шарфе»: a sitting raccoon with a bandit mask, a ringed tail and a striped scarf.
function raccoon(){
 const head=union(ellipse(16,12,9.6,7.4),poly([[7,10],[2.6,15.6],[9,17.4]]),poly([[25,10],[29.4,15.6],[23,17.4]]));
 const ears=union(poly([[6.6,9.5],[7.6,1.8],[13.4,5.6]]),poly([[25.4,9.5],[24.4,1.8],[18.6,5.6]]));
 const inEars=union(poly([[8.6,7.6],[9,4.4],[11.6,6]]),poly([[23.4,7.6],[23,4.4],[20.4,6]]));
 const body=ellipse(16,29,10.4,9);
 const tailPath=union(seg(25,34.4,30.6,31.4,3),seg(30.6,31.4,32,18.6,3.5),circle(31.8,17.2,3.2));
 const tailCol=(x,y)=>y<17?'D':Math.floor((y-17)/3)%2?'A':'D';
 const scarf=union(box(16,19.8,9.6,1.9,1.4),box(21.6,25.5,2.3,5,.6));
 const scarfCol=(x,y)=>y>=22?(Math.floor(y/2)%2?'R':'Y'):(Math.floor(x/2)%2?'R':'Y');
 const g=raster(38,39,'.',[[tailPath,tailCol],[ears,'A'],[inEars,'K'],[body,shade(body,'A','E',1.8,1,.3)],
  [ellipse(16,31,6.2,6.6),'C'],[ellipse(10.4,37.2,3.2,1.6),'D'],[ellipse(21.6,37.2,3.2,1.6),'D'],
  [head,'A'],[union(ellipse(11.4,9,3.4,1.4),ellipse(20.6,9,3.4,1.4)),'C'],
  [union(ellipse(11.2,12.4,4.2,2.4),ellipse(20.8,12.4,4.2,2.4),box(16,10,1,3)),'D'],
  [ellipse(16,16.2,5,2.8),'C'],[scarf,scarfCol],[union(ellipse(12.2,27,2,1.5),ellipse(16.2,27.4,2,1.5)),'D']]);
 rows(g,[
  [11,10,'CC'],[12,10,'CD'],[11,20,'CC'],[12,20,'DC'],
  [14,15,'DD'],[15,15,'DD'],[16,14,'D'],[16,17,'D'],[17,15,'DD'],
 ]);
 return str(g);}

export default {theme:'batch-0011',pictures:[
 {title:'6–7',forLevel:56,family:'Числовой и фразовый абсурд',device:'персонаж из предмета',subject:'цифры 6 и 7 с лицами',
  source:'интернет-мем/тренд',reference:'6–7 / six-seven [T1]',rows:sixSeven()},
 {title:'Мастерская в ореховой скорлупе',forLevel:57,family:'Кафе и мастерские',direction:'Миниатюрные мастерские',
  subject:'мастерская в половинке грецкого ореха',source:'оригинальный',rows:walnutShop()},
 {title:'Енот в шарфе',forLevel:58,family:'Лесные и горные животные',subject:'енот в полосатом шарфе',source:'оригинальный',rows:raccoon()},
]};

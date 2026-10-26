// Batch 0011, part 09: honey jar, carousel, coffee van, record player, bonsai (levels 51–55).
import {circle,ellipse,box,poly,union,cut,meet} from '../../../../../tools/gameplay-pilot-v9/draw.mjs';
import {raster,fit,str,put,ring4,scale,seg} from '../helpers.mjs';
const draw=(w,h,k,layers)=>raster(Math.round(w*k),Math.round(h*k),'.',layers.map(([s,c])=>[scale(k,s),typeof c==='function'?(x,y,u)=>c(x/k,y/k,u):c]));

// 1. Honey jar: round golden jar, red dotted cloth lid tied with string, a ribbed dipper with a drip beside it.
function honey(){
 const jar=union(box(10,16.6,8.2,6.2,4.4),box(10,10.6,5.2,1.6,0.6));
 const shade=cut(jar,box(8.2,15.6,8.2,6.4,4.4));
 const dome=box(10,3.4,5.6,2.0,1.8);
 const skirt=union(poly([[3.6,5.2],[16.4,5.2],[18.2,8.6],[1.8,8.6]]),...[2.9,6.0,10,14,17.1].map(x=>circle(x,8.6,1.45)));
 const head=union(ellipse(21,18.6,2.5,3.4));
 const g=draw(24,24,1,[
  [jar,'Y'],[shade,'O'],[box(10,10.6,5.2,1.6,0.6),'O'],
  [box(4.6,15.2,1,3.2,1),'C'],[box(4.6,20.0,1,0.55),'C'],
  [dome,'R'],[skirt,'R'],[box(10,5.4,6.2,0.55),'A'],
  [seg(21,5.2,21,15.6,0.95),'A'],[head,'A'],
  [meet(head,(x,y)=>(Math.floor(y)%2===1)?-1:1),'E'],
 ]);
 put(g,'C',[[6,3],[10,2],[14,3],[8,4],[12,4],[4,7],[8,7],[12,7],[16,7],[6,8],[10,8],[14,8]]);
 put(g,'Y',[[20,22],[21,22],[21,23]]);
 return str(g);
}

// 2. Carousel: striped cone roof with a flag, gold poles, two horses, round base.
const HORSE=[ // facing right, pole through columns 5–6
 '...........ACC...',
 '..........AACCCC.',
 '..........ACCCDCC',
 '.........AACCCCCC',
 '.........ACCCC...',
 '........AACCC....',
 'A.......ACCCC....',
 'AA......ACCCC....',
 'AACCPPPPCCCCC....',
 'AACCPPPPCCCCCC...',
 '.ACCCCCCCCCCCCC..',
 '..CCC.....CCCCC..',
 '.CCC.........CC..',
 'CC..........CC...'];
function carousel(){
 const W=44,cx=22,ap=4,rb=18,half=21;
 const roof=poly([[cx,ap],[cx-half,rb],[cx+half,rb]]);
 const stripe=(x,y)=>{const t=(x+.5-cx)/Math.max(.01,(y+.5-ap)/(rb-ap)*half);return Math.floor((t+1)*3.5)%2===0?'R':'C';};
 const pw=42/11,pen=(x,y)=>{const u=(x-1)/pw,i=Math.floor(u),t=(u-i)*2-1;return (i<0||i>10)?1:(y-(rb+2)-4.2*(1-Math.abs(t)));};
 const penCol=x=>Math.floor((x+.5-1)/pw)%2===0?'R':'C';
 const g=raster(W,44,'.',[
  [box(cx,3,1,3),'D'],[poly([[23,0],[29.5,1.7],[23,3.4]]),'Y'],
  [box(cx,29.5,2,9.5),'V'],[box(cx,29.5,1,9.5),'P'],
  [box(12,29.5,1,9.5),'Y'],[box(32,29.5,1,9.5),'Y'],
  [meet(pen,(x,y)=>y>rb?-1:1),penCol],[roof,stripe],[box(cx,rb+1,half,1),'Y'],[box(cx,5.7,2,2.1,0.9),'Y'],
  [box(cx,40,21,1,0.5),'B'],[poly([[1,41],[43,41],[41,44],[3,44]]),'N'],
 ]);
 const stamp=(x0,y0,flip)=>HORSE.forEach((r,y)=>[...r].forEach((c,x)=>{if(c!=='.')g[y0+y][flip?x0+16-x:x0+x]=c;}));
 stamp(1,24,true);stamp(26,24,false);
 return str(g);
}

// 3. Coffee van: rounded retro van, striped awning over the serving window, a big cup sign on the roof.
function van(){
 const body=union(box(21,20.5,19,9.5,5),box(36,23,5,7,3));
 const g=raster(44,36,'.',[
  [box(19,10,1,1.5),'A'],[box(19,8.5,7,0.9,0.5),'A'],
  [poly([[13,0],[25,0],[23.6,7.6],[14.4,7.6]]),'R'],[cut(circle(25.4,3.4,3.2),circle(25.4,3.4,1.5)),'R'],
  [box(19,0.6,6,0.9),'C'],
  [body,'B'],[meet(body,(x,y)=>y<20?-1:1),'C'],
  [box(21,29.6,19,1.0,0.5),'N'],
  [box(17,17,8.5,4.5),'D'],[box(17,22.4,9.6,0.9),'A'],
  [box(34.6,16.5,4.2,3.4,1.2),'S'],[box(4.5,17,1.6,3,0.8),'S'],
  [box(40.4,24.5,1,1.4),'Y'],
  [union(box(17,14,10.5,1.5),...Array.from({length:7},(_,i)=>circle(8+i*3,15.6,1.5))),x=>Math.floor((x+.5-6.5)/3)%2===0?'R':'C'],[box(17,12,11,0.6),'R'],
  [circle(10,30.5,4.4),'D'],[circle(33,30.5,4.4),'D'],[circle(10,30.5,1.7),'C'],[circle(33,30.5,1.7),'C'],
 ]);
 return str(g);
}

// 4. Record player: wooden case seen from above, black record with a red label, tone arm.
function player(){
 const c=[12,11.4];
 const g=raster(30,25,'.',[
  [box(15,11.2,14.6,10.8,1.6),'A'],[box(15,23,14.6,1.8,1),'E'],
  [circle(...c,10.4),'D'],
  [cut(circle(...c,7.4),circle(...c,6.5)),'E'],
  [circle(...c,3.4),'R'],
  [box(26,2.4,1.5,1.2,0.4),'D'],[circle(26,5.2,2.4),'Y'],
  [seg(26,5.2,26,15.5,0.95),'C'],[seg(26,15.5,21.6,19.4,0.95),'C'],[box(20.6,19.4,1.6,1.5,0.5),'C'],
  [circle(23.5,23,1.2),'Y'],[circle(27,23,1.2),'Y'],
 ]);
 put(g,'D',[[11,10]]);
 return str(g);
}

// 5. Bonsai: curved trunk, three cloud pads, shallow blue pot.
function bonsai(){
 const pad=(cx,cy,rx,ry)=>union(ellipse(cx,cy,rx,ry),circle(cx-rx*0.45,cy-ry*0.55,ry*0.9),circle(cx+rx*0.35,cy-ry*0.6,ry*0.95));
 const pads=[pad(9.5,17.3,8,3.7),pad(30,13,8.6,3.9),pad(19,5.8,7,3.5)];
 const trunk=union(seg(20,30,20,26,2.6),seg(20,26,14,21,2.1),seg(14,21,18,15,1.7),seg(18,15,23,11,1.3),seg(23,11,19,6.5,1.1),
  seg(14,21,10,18,1.0),seg(23,11,28,12,1.0),poly([[15.5,30],[24.5,30],[22,27],[18,27]]));
 const T=f=>(x,y)=>f(x+1.4,y+0.6);
 const g=draw(37.6,36.3,1.15,[
  [trunk,'E'],[meet(trunk,(x,y)=>(x<19.4&&y>23)||(x<15.4&&y<=23&&y>17)?-1:1),'A'],
  ...pads.map(p=>[p,'G']),
  ...pads.map(p=>[meet(p,(x,y)=>p(x,y+1.0)>0?-1:1),'M']),
  [box(20,30.4,12.5,0.9,0.4),'M'],
  [box(20,31.6,14.5,0.9,0.4),'B'],[poly([[6,32.4],[34,32.4],[31.6,35.2],[8.4,35.2]]),'N'],
  [box(10.5,35.8,1.6,0.6),'N'],[box(29.5,35.8,1.6,0.6),'N'],
 ].map(([s,c])=>[T(s),c]));
 return str(g);
}

export default {theme:'batch-0011',pictures:[
 {title:'Баночка мёда',forLevel:51,family:'Десерты и сладости',device:'compact',subject:'баночка мёда с тканевой крышкой и ложкой-веретеном',source:'оригинальный',rows:honey()},
 {title:'Карусель',forLevel:52,family:'Ярмарки и праздники',subject:'карусель с полосатой крышей и лошадками',source:'оригинальный',rows:carousel()},
 {title:'Фургончик-кофейня',forLevel:53,family:'Кафе и мастерские',subject:'ретро-фургон с кофейным окном и чашкой на крыше',source:'оригинальный',rows:van()},
 {title:'Проигрыватель',forLevel:54,family:'Музыка',subject:'проигрыватель с пластинкой в деревянном корпусе',source:'оригинальный',rows:player()},
 {title:'Бонсай',forLevel:55,family:'Деревья, листья и плоды',subject:'бонсай с изогнутым стволом в плоском горшке',source:'оригинальный',rows:bonsai()},
]};

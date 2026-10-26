// Batch 0011, part 05: levels 31–35 (bicycle with flowers, boat in a teacup, snow globe, doughnut planet, floating island).
import {circle,ellipse,box,poly,union,cut,meet} from '../../../../../tools/gameplay-pilot-v9/draw.mjs';
import {raster,fit,str,put,ring4,scale,seg} from '../helpers.mjs';
const draw=(w,h,k,layers)=>raster(Math.round(w*k),Math.round(h*k),'.',layers.map(([s,c])=>[scale(k,s),typeof c==='function'?(x,y,u)=>c(x/k,y/k,u):c]));
const ring=(cx,cy,ro,ri)=>cut(circle(cx,cy,ro),circle(cx,cy,ri));
// drop empty border rows and columns
const trim=g=>{const H=g.length,W=g[0].length,on=(x,y)=>g[y][x]!=='.';let t=0,b=H-1,l=0,r=W-1;
 while(g[t].every(v=>v==='.'))t++;while(g[b].every(v=>v==='.'))b--;
 while(g.every(row=>row[l]==='.'))l++;while(g.every(row=>row[r]==='.'))r--;return g.slice(t,b+1).map(row=>row.slice(l,r+1));};

// 1. A retro bicycle, side view, with a flower basket in front.
function bicycle(){
 const W1=[10,24],W2=[34,24];
 const spokes=(cx,cy)=>[[meet(circle(cx,cy,7),union(seg(cx-6,cy-6,cx+6,cy+6,.85),seg(cx-6,cy+6,cx+6,cy-6,.85))),'C']];
 const tyre=(cx,cy)=>[[ring(cx,cy,9.7,7.2),'D'],[ring(cx,cy,7.2,6.2),'C']];
 const fender=(cx,cy)=>[[meet(ring(cx,cy,11.5,9.5),(x,y)=>y<cy-2?-1:1),'R']];
 const t=1.15;
 const g=raster(44,35,'.',[
  ...spokes(...W1),...spokes(...W2),
  ...fender(...W1),...fender(...W2),
  // frame
  [union(seg(10,24,17,12,t),seg(10,24,20,24.5,t),seg(17,11,20,24.5,t),seg(17,12.2,29.6,11.4,t),seg(20,24.5,30.4,14,t),seg(29.4,10,30.6,15,t+.1),seg(30.6,15,34,24,t)),'R'],
  ...tyre(...W1),...tyre(...W2),
  [circle(...W1,2),'Y'],[circle(...W2,2),'Y'],
  // seat post and saddle
  [seg(17,12,16.2,8.5,1),'E'],
  [union(ellipse(15.6,7.6,3.6,1.5),box(13.6,8.4,1.5,1)),'E'],
  // stem and handlebar
  [union(seg(29.4,10.5,28.6,6.6,1),seg(28.6,6.2,24.6,6.2,1)),'D'],
  [box(24.4,6.2,1.6,1.1,.5),'E'],
  // chainring and crank
  [circle(20,24.5,2.8),'Y'],[circle(20,24.5,1),'D'],
  [seg(20,24.5,22.5,27.5,.8),'E'],[box(23.4,28,1.5,.8),'D'],
  // basket
  [box(36.5,11,5.6,4,.8),'A'],
  [meet(box(36.5,11.6,4.6,2.6),(x,y)=>(Math.floor(x)%3===1?-1:1)),'E'],
  [box(36.5,7.4,6,1,.4),'E'],
  // flowers and leaves
  [union(ellipse(31.6,6,2,1.1),ellipse(41.6,6,2,1.1),ellipse(36.6,6.2,2.4,1.1)),'G'],
  [union(circle(32.6,4.4,2.2),circle(40.6,4.4,2.2)),'P'],
  [circle(36.6,3,2.4),'C'],
  [union(box(32.6,4.4,.6,.6),box(40.6,4.4,.6,.6),box(36.6,3,.9,.9)),'Y'],
 ]);
 return str(trim(g));
}

// 2. A tiny sailboat floating on tea in a big teacup on a saucer.
function teacup(){
 const cx=21,ty=16.4;
 const bowl=meet(ellipse(cx,ty,16,16.4),(x,y)=>y>ty?-1:1),cup=union(bowl,ellipse(cx,ty,16,4));
 const handle=cut(ellipse(36.4,21.4,5.8,5.4),ellipse(36.4,21.4,3,2.6));
 const g=raster(44,41,'.',[
  // saucer and the cup's foot
  [ellipse(cx,35,20.6,3.8),'S'],
  [meet(ellipse(cx,35,20.6,3.8),(x,y)=>y>36.2?-1:1),'B'],
  [box(cx,33,7.6,1.8,.6),'B'],
  // handle, cup body with its shade side and a band
  [handle,'S'],
  [cup,'S'],
  [meet(bowl,(x,y)=>x>cx+10-(y-ty)*.36?-1:1),'B'],
  [meet(bowl,(x,y)=>y>ty+3.6&&y<ty+6?-1:1),'N'],
  // tea in the cup, its far edge darker
  [ellipse(cx,ty,14.2,3),'A'],
  [meet(ellipse(cx,ty,14.2,3),(x,y)=>y<ty-.8?-1:1),'E'],
  // the little boat
  [poly([[15,13.6],[28,13.6],[26,17],[17,17]]),'R'],
  [box(22,7.6,1,6.4),'E'],
  [poly([[23,3.2],[23,12.6],[29.6,12.6]]),'C'],
  [poly([[21,4.8],[21,12.6],[14.6,12.6]]),'Y'],
  [poly([[23,1],[27.4,2.1],[23,3.2]]),'R'],
 ]);
 return str(trim(g));
}

// 3. A snow globe with a little fir tree on a wooden base.
function snowGlobe(){
 const g=raster(19,23,'.',[
  [circle(9.5,9.2,9),'S'],
  [meet(circle(9.5,9.2,9),(x,y)=>y>13.4?-1:1),'C'],
  [poly([[9.5,2],[13.2,7.2],[11.6,7.2],[14.6,11.8],[4.4,11.8],[7.4,7.2],[5.8,7.2]]),'G'],
  [meet(poly([[9.5,2],[13.2,7.2],[11.6,7.2],[14.6,11.8],[4.4,11.8],[7.4,7.2],[5.8,7.2]]),(x,y)=>x>9.5?-1:1),'M'],
  [box(9.5,12.8,1.5,1),'E'],
  [poly([[2.4,16.6],[16.6,16.6],[18,22.6],[1,22.6]]),'A'],
  [box(9.5,19.6,8.4,1),'E'],
 ]);
 put(g,'C',[[4,5],[14,4],[3,9],[15,9],[6,2]]);
 return str(trim(g));
}

// 4. A glazed doughnut planet with a ring of sprinkles and a tiny flag on top.
function donutPlanet(){
 const cx=22,cy=24.4,ry0=cy-3.6,rot=-.26,ca=Math.cos(rot),sa=Math.sin(rot);
 const R=(x,y)=>[(x-cx)*ca-(y-ry0)*sa,(x-cx)*sa+(y-ry0)*ca];
 const ringE=(rx,ry)=>(x,y)=>{const [u,v]=R(x,y);return (Math.hypot(u/rx,v/ry)-1)*Math.min(rx,ry);};
 const band=cut(ringE(23,9.4),ringE(18.8,5.8));
 const back=meet(band,(x,y)=>R(x,y)[1]<0?-1:1),front=meet(band,(x,y)=>R(x,y)[1]>=0?-1:1);
 const sprinkle=(x,y)=>{const [u]=R(x+.5,y+.5);return 'BBY'[((Math.floor((u+40)/2.6))%3+3)%3];};
 const top=ellipse(cx,cy-1.6,16.4,13.2),body=union(top,ellipse(cx,cy+2.2,16.4,13));
 const glaze=(x,y)=>Math.max(ellipse(cx,cy-2.2,15,11.8)(x,y),y-(cy+4.8+1.3*Math.sin(x*.9)));
 const hole=ellipse(cx,cy-2.6,5.8,3.7);
 const g=raster(44,43,'.',[
  [back,sprinkle],
  [body,'O'],
  [cut(body,ellipse(cx-2.2,cy-1,16.8,13.4)),'A'],
  [glaze,'P'],
  [ellipse(cx,cy-2.6,7,4.7),'O'],
  [hole,'A'],
  [meet(hole,(x,y)=>y>cy-3.9?-1:1),'.'],
  [front,sprinkle],
  [box(cx+1,5.6,1,4.6),'D'],
  [poly([[cx+2,1],[cx+7.6,2.9],[cx+2,4.8]]),'R'],
 ]);
 const sp=[[12,16,'Y',1],[16,12,'B',0],[27,12,'L',1],[31,15,'Y',0],[10,22,'L',0],[34,20,'B',1],[16,26,'Y',0],[28,26,'L',1],[21,10,'Y',0],[12,27,'L',1],[32,27,'Y',0],[24,30,'L',0]];
 for(const [x,y,c,v] of sp){if(g[y][x]==='P')g[y][x]=c;const [u,w]=v?[x,y+1]:[x+1,y];if(g[w]?.[u]==='P')g[w][u]=c;}
 return str(trim(g));
}

// 5. A floating island of earth and grass with a small house, a tree and roots below.
function island(){
 const under=poly([[1.5,26],[42.5,26],[38.5,32],[32,37.5],[24,42.2],[15.5,38],[7,32.5]]);
 const g=draw(44,44,1.02,[
  // earth underside and its shade side
  [union(under,ellipse(22,26.6,20.6,2.4)),'A'],
  [meet(under,(x,y)=>x>24.5+(y-26)*.05?-1:1),'E'],
  // roots
  [union(seg(12,34,9.4,40.4,.85),seg(31,36,34,41.4,.85)),'E'],
  [seg(23.6,41,23.6,43.8,.85),'A'],
  // grass top
  [ellipse(22,23.4,21.4,3.6),'G'],
  [meet(ellipse(22,23.4,21.4,3.6),(x,y)=>y>24.8?-1:1),'M'],
  // house
  [box(12.6,16.6,7,5.6),'C'],
  [poly([[2.6,12],[12.6,2],[22.6,12]]),'R'],
  [box(16.6,5.4,1.4,2.6),'E'],
  [box(9.4,18.6,2,3.6,.4),'O'],
  [box(16,15.8,2,2),'B'],
  // tree
  [box(32.4,17.6,1.6,5.4),'E'],
  [union(circle(32.4,10,7.6),circle(27.6,13,4.8),circle(37.2,13,4.8)),'M'],
  [union(circle(30.6,8,4),circle(27.2,11.8,2.6)),'G'],
 ]);
 return str(trim(g));
}

export default {theme:'batch-0011',pictures:[
 {title:'Велосипед с корзиной цветов',forLevel:31,family:'Наземный транспорт',subject:'ретро-велосипед с корзиной цветов',source:'оригинальный',rows:bicycle()},
 {title:'Лодка в чашке',forLevel:32,family:'Посуда и кухонные вещи',direction:'Перевёрнутый масштаб',subject:'парусник на чае в чашке с блюдцем',source:'оригинальный',rows:teacup()},
 {title:'Снежный шар',forLevel:33,family:'Времена года и погода',subject:'снежный шар с ёлочкой на деревянной подставке',source:'оригинальный',rows:snowGlobe()},
 {title:'Планета-пончик',forLevel:34,family:'Десерты и сладости',direction:'Кулинарная фантастика',subject:'глазированный пончик-планета с кольцом посыпки и флажком',source:'оригинальный',rows:donutPlanet()},
 {title:'Остров в облаках',forLevel:35,family:'Дома и архитектура',direction:'Невесомый мир',subject:'парящий остров с домиком и деревом',source:'оригинальный',rows:island()},
]};

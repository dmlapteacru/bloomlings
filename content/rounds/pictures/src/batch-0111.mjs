// Batch 111: two stream pictures for the Sprig bridge introductions (L16, L18): one stream row of '#' with two bridge
// sites 'b' (left and right third), a near bank below with a path to each bridge, the main subject on the far bank.
import {circle,ellipse,box,poly,union,cut,meet} from '../../../../tools/gameplay-pilot-v9/draw.mjs';
import {raster,str,put,seg} from './helpers.mjs';

// shared: turn row `sy` into the stream with bridges at bx, clear a path from each anchor to the bottom edge
function stream(g,sy,bxs,paths){const W=g[0].length;for(let x=0;x<W;x++)g[sy][x]='#';
 for(const x of bxs){g[sy][x]='b';g[sy-1][x]='.';}
 for(const p of paths)for(const [x,y] of p)g[y][x]='.';return g;}
const col=(x,y0,y1)=>Array.from({length:y1-y0+1},(_,i)=>[x,y0+i]);

function deer(){const W=36,H=40,SY=33;
 const ground=y=>y>=27&&y<SY;
 const g=raster(W,H,'.',[
  // far bank: grass with a low hill at the right under the bush, darker lip along the stream
  [union(box(18,30,18,3),ellipse(30,28.5,8,3)),'G'],
  [box(18,SY-0.5,18,0.5),'M'],
  // bush on the hill
  [union(circle(32.6,21.4,3.8),circle(30.8,25,2.6),circle(34.6,24.4,2.8),circle(32.6,26,2.8)),'M'],
  [union(circle(31.6,20.6,1.4),circle(30.4,24.4,1),circle(34,23.6,1)),'G'],
  // far legs (shadow side)
  [union(box(15,23.5,1,4.5),box(26,23.5,1,4.5)),'E'],
  // body, neck, head
  [ellipse(19.5,18.5,8.6,4.6),'A'],
  [poly([[10.5,19],[16,15],[12.2,8.8],[8.2,10.6]]),'A'],
  [ellipse(8.4,10.8,3.4,2.5),'A'],
  [ellipse(4.6,12,2.4,1.6),'A'],
  // near legs with a thigh
  [union(box(12,24,1,4),ellipse(25,20.5,3,3.5),box(23,24,1,4)),'A'],
  // hooves
  [union(box(12,28.5,1,0.5),box(15,28.5,1,0.5),box(23,28.5,1,0.5),box(26,28.5,1,0.5)),'D'],
  // cream belly, throat, muzzle and tail
  [meet(ellipse(19.5,18.5,8.6,4.6),box(19.5,22.6,6,1.2)),'C'],
  [ellipse(3.9,12.6,1.6,0.9),'C'],
  [union(ellipse(28.2,15.4,1.4,1.3),ellipse(27.6,16.6,1.2,1.5)),'C'],
  // antlers: two beams with tines
  [union(seg(9.6,9,11.2,5.4,0.95),seg(11.2,5.4,13.4,2.2,0.9),seg(10.6,6.6,7.4,3.6,0.85),seg(7.4,3.6,6.6,0.6,0.8),seg(12.4,3.8,11.4,0.6,0.8),
   seg(11.8,9,14.4,6.2,0.95),seg(14.4,6.2,17.6,3.6,0.9),seg(17.6,3.6,18.6,0.6,0.8),seg(15.6,5.2,15.6,1.2,0.8)),'E'],
  // ear, laid back under the antlers
  [poly([[11,10.2],[15.4,8.6],[14.6,10.4],[12,11.2]]),'A'],
 ]);
 put(g,'D',[[2,11],[2,12],[8,10]]);put(g,'.',[[4,13]]);
 // near bank: grass, darker tufts and two cream pebbles
 for(let y=SY+1;y<H;y++)for(let x=0;x<W;x++)g[y][x]='G';
 put(g,'M',[[1,35],[2,35],[2,36],[15,37],[16,37],[16,36],[33,36],[34,36],[33,37],[23,38],[24,38]]);
 put(g,'C',[[10,36],[11,36],[10,37],[11,37],[12,37],[26,37],[27,37],[27,36],[28,37]]);
 put(g,'Y',[[3,38],[4,38],[19,35],[20,35],[33,38],[34,38],[8,29],[9,29],[19,30],[20,30],[2,29],[3,29]]);
 stream(g,SY,[6,29],[col(6,SY+1,H-1),col(29,SY+1,H-1)]);
 return str(g);}

const tilt=(cx,cy,rx,ry,a)=>(x,y)=>{const c=Math.cos(a),s=Math.sin(a),u=(x-cx)*c+(y-cy)*s,v=-(x-cx)*s+(y-cy)*c;return ellipse(0,0,rx,ry)(u,v);};
// stamp a hand-drawn sprite (rows, '.' transparent) at x0,y0
const stamp=(g,x0,y0,rows)=>{rows.forEach((r,j)=>{for(let i=0;i<r.length;i++)if(r[i]!=='.')g[y0+j][x0+i]=r[i];});return g;};
// a reed: a one-cell stalk with a two-cell cattail and its tip
const reed=(g,x,top,bottom)=>{g[top][x]='M';for(let y=top+1;y<top+5;y++){g[y][x]='E';g[y][x+1]='E';}for(let y=top+5;y<=bottom;y++)g[y][x]='M';return g;};
function beaver(){const W=38,H=39,SY=33;
 const g=raster(W,H,'.',[
  // far bank grass with a darker lip
  [box(19,30.5,19,2.5),'G'],
  [box(19,SY-0.5,19,0.5),'M'],
 ]);
 reed(g,1,8,27);reed(g,4,5,27);reed(g,33,4,18);reed(g,36,7,18);
 // log dam: a staggered pyramid of logs, cream cut ends with a ring, bark with a grain line
 const log=(x,y)=>{const n=W-x-3;stamp(g,x,y,['.CC'+'E'.repeat(n),'CAC'+'A'.repeat(n),'.CC'+'E'.repeat(n)]);};
 log(26,25);log(28,22);log(30,19);
 // tail: a flat paddle on the ground behind, with a scale pattern
 const t=raster(W,H,'.',[[tilt(6,25,6,2.5,0.25),(x,y)=>((x+y)%3===0&&(x-y+60)%3===0)?'E':'D']]);
 t.forEach((r,y)=>r.forEach((c,x)=>{if(c!=='.')g[y][x]=c;}));
 // the beaver, sitting up and gnawing a stick held in its front paws
 stamp(g,8,7,[
  '......EE.............',
  '.....EAAAAA..........',
  '....AAAAAAAAA........',
  '...AAAAAAAAAAA.......',
  '...AAAAAAAADAAAA.....',
  '...AAAAAAAAAAACCDD...',
  '...AAAAAAAAAACCCCDD..',
  '....AAAAAAAAACCCCC...',
  '.....AAAAAAAAAOOCC...',
  '....AAAAAAAAAAOO.....',
  '..AAAAEEDDEEEEDDEEEE.',
  '.AAAAAEEDDEEEEDDEEEE.',
  '.AAAAAAAAAAAAAAAA....',
  'AAAAAAAAAAAAAAAAAA...',
  'AAAAAAAAAAAAAAAAAA...',
  'AAAAAAAAAAAAAAAAAA...',
  'AAAAAAAAAAAAAAAAAA...',
  '.AAAAAAAAAAAAAAAAA...',
  '.AAAAAAAAAAAAAAAA....',
  '..AAAAAAAAAAAAAAA....',
  '..AAAAAAAAAAAAAA.....',
  '.EEEEEE....EEEEEE....',
 ]);
 // near bank: grass with darker tufts and a few cream pebbles
 for(let y=SY+1;y<H;y++)for(let x=0;x<W;x++)g[y][x]='G';
 put(g,'M',[[1,35],[2,35],[2,36],[15,37],[16,37],[16,36],[34,35],[35,35],[35,36],[23,37],[24,37]]);
 put(g,'C',[[10,36],[11,36],[10,37],[11,37],[12,37],[27,37],[28,37],[28,36],[29,37]]);
 stream(g,SY,[6,31],[col(6,SY+1,H-1),col(31,SY+1,H-1)]);
 return str(g);}

export default {theme:'batch-0111',pictures:[
 {title:'Олень у ручья',forLevel:16,family:'Лесные и горные животные',subject:'олень с ветвистыми рогами стоит на дальнем берегу ручья, ближний берег с травой и камнями',source:'оригинальный',rows:deer()},
 {title:'Бобр строит плотину',forLevel:18,family:'Лесные и горные животные',subject:'бобр с веткой на берегу ручья у плотины из брёвен',source:'оригинальный',rows:beaver()},
]};

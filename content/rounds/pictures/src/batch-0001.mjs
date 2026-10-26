// Batch 1 (P0001–P0010), skill bloomlings-round-pictures, rules product/17_LEVEL_GENERATION_RULES.md §2 and §4.
// P0001–P0003 are the fixed first three: crown 138 cells (63 / 37 / 38), ladybug 2C 240 (120 / 97 / 19 / 4) and bird
// 3B 281 (76 / 70 / 69 / 51 / 13 / 2). The crown is hand-drawn in three nested layers, so its colours open one after
// another (§4): the sky round it first, then the gold rim, then the red cap inside. The ladybug and the bird are
// hand-drawn to their exact volumes too. P0004–P0010 are drawn with the shape painter and follow the stage ranges.
import {circle,ellipse,box,poly,union,cut,meet,paint,rows} from '../../../../tools/gameplay-pilot-v9/draw.mjs';

// ---- helpers ----
const raster=(W,H,bg,parts)=>Array.from({length:H},(_,y)=>Array.from({length:W},(_,x)=>{let c=bg;for(const [shape,color] of parts)if(shape(x+.5,y+.5)<0)c=color;return c;}));
const count=(g,c)=>g.flat().filter(v=>v===c).length;
const N4=[[1,0],[-1,0],[0,1],[0,-1]];
// Bring colour c to exactly n cells by trading edge cells with colour `from`: grow at the cells of `from` touching c
// nearest c's centre, shrink at the cells of c touching `from` farthest from it. Deterministic (ties by row, column).
function fit(g,c,n,from){const H=g.length,W=g[0].length;
 for(let guard=0;guard<2000;guard++){const have=count(g,c);if(have===n)return g;
  let sx=0,sy=0;g.forEach((r,y)=>r.forEach((v,x)=>{if(v===c){sx+=x;sy+=y;}}));const cx=sx/have,cy=sy/have,grow=have<n;let best=null;
  for(let y=0;y<H;y++)for(let x=0;x<W;x++){if(g[y][x]!==(grow?from:c))continue;
   if(!N4.some(([dx,dy])=>g[y+dy]?.[x+dx]===(grow?c:from)))continue;
   const d=Math.hypot(x-cx,(y-cy)*1.05);if(!best||(grow?d<best.d:d>best.d))best={x,y,d};}
  if(!best)throw Error(`fit ${c}: no edge cell`);g[best.y][best.x]=grow?c:from;}
 throw Error('fit: no convergence');}
const blank=(g,cells)=>{for(const [x,y] of cells)g[y][x]='.';return g;};
const str=g=>g.map(r=>r.join(''));
const seg=(x1,y1,x2,y2,r)=>(x,y)=>{const ex=x2-x1,ey=y2-y1,t=Math.max(0,Math.min(1,((x-x1)*ex+(y-y1)*ey)/(ex*ex+ey*ey)));return Math.hypot(x-x1-ex*t,y-y1-ey*t)-r;};
const mask=(shape,pred)=>(x,y)=>pred(Math.floor(x),Math.floor(y))?shape(x,y):1;

// ---- P0001 crown: sky S 63 (outer), gold rim Y 37, red cap R 38 (inside the rim only) ----
const crown=['..SSSSSSSSS..',
 'SSSSSSYSSSSSS',
 'SYSSSSYSSSSYS',
 'SYYSSYRYSSYYS',
 'SYRYSYRYSYRYS',
 'SYRRYRRRYRRYS',
 'SYRRRRRRRRRYS',
 'SYRRRRRRRRRYS',
 'SYRRRRRRRRRYS',
 'SYYYYYYYYYYYS',
 '.SSSSSSSSSSSS'];

// ---- P0002 ladybug 2C on a leaf: body R 120, leaf G 97, head and spots D 19, eyes C 4 ----
const ladybug=['GGGGGGGGGGGGGGG',
 'GGGGGGDDDGGGGGG',
 'GGGGGDCDCDGGGGG',
 'GGGGGRCDCRGGGGG',
 'GGGGRRRRRRRGGGG',
 'GGGRRRRRRRRRGGG',
 'GGRRRRRRRRRRRGG',
 'GRRRDDRRRDDRRRG',
 'GRRRDDRRRDDRRRG',
 'GRRRRRRRRRRRRRG',
 'GRRRRRRRRRRRRRG',
 'GRRRDRRRRRDRRRG',
 'GRRRDRRRRRDRRRG',
 'GGRRRRRRRRRRRGG',
 'GGGRRRRRRRRRGGG',
 'GGGGGRRRRRGGGGG'];

// ---- P0003 bird 3B on a hill: sky S 76, body B 70, hill G 69, belly C 51, beak and feet O 13, eye D 2 ----
const bird=['...SSSSSBBBBSS...',
 '.SSSSSSBBBBBBSSS.',
 'SSSSSSBBBBBBDBOOS',
 'SSSSSSBBBBBBDBOOO',
 'SBSSSBBBBBBBCCOOS',
 'SBBSBBBBBBBCCCCSS',
 'SBBBBBBBBCCCCCCSS',
 'SSBBBBBBBCCCCCCCS',
 'SSSBBBBBCCCCCCCCS',
 'SSSBBBBCCCCCCCCSS',
 'SSSSBBBCCCCCCCSSS',
 'SSSSSBBCCCCCCCSSS',
 'GGSGSSSOCCOSSSGSG',
 'GGGGGGOOGGOOGGGGG',
 'GGGGGGGGGGGGGGGGG',
 'GGGGGGGGGGGGGGGGG',
 'GGGGGGGGGGGGGGGGG'];

// ---- P0004 fly agaric on the grass ----
function mushroom(){const W=18,H=20;return rows(paint(W,H,{
 background:(x,y)=>y>=16?(y>=H-1?'M':'G'):'S',
 parts:[
  {shape:union(box(9,13.5,2.6,3.6,1.2),ellipse(9,16.6,3.6,1.2)),color:'C',shade:'A',flat:true},
  {shape:cut(ellipse(9,8.4,7.8,6),box(9,14.5,9,3.4)),color:'R',light:'C',sphere:[7,6,8],lightAt:.93},
  ...[[5.4,6.6,1.2],[10,4.6,1.3],[12.8,8.2,1.1],[7.6,9.6,.9],[3.6,9.8,.8],[14.6,10.4,.7]].map(([x,y,r])=>({shape:circle(x,y,r),color:'C',outline:false})),
  {shape:union(ellipse(3,17.4,1.4,.9),ellipse(15.2,17.2,1.5,1)),color:'M',outline:false}]}));}

// ---- P0005 strawberry on a pink cloth ----
function strawberry(){const W=19,H=21,berry=union(poly([[2.6,7.4],[16.4,7.4],[13,16.8],[9.5,19.6],[6,16.8]]),ellipse(6,9,4.2,3.2),ellipse(13,9,4.2,3.2));
 return rows(paint(W,H,{background:(x,y)=>(x+2*y)%7===0&&(y%3===1)?'P':'K',
 parts:[
  {shape:berry,color:'R',shade:'P',flat:true,band:1.2},
  ...[[5,10],[8.5,9.4],[12,10],[15,9.4],[6.6,12.6],[10,12.4],[13.4,12.6],[8.2,15.4],[11,15.4],[9.6,18]].map(([x,y])=>({shape:box(x,y,.45,.6),color:'Y',outline:false})),
  {shape:union(...[0,1,2,3,4].map(i=>{const a=Math.PI*(1.1+i*.2);return ellipse(9.5+3.4*Math.cos(a),6.4+1.8*Math.sin(a)+1.4,2.4,1.2);})),color:'G',shade:'M',flat:true},
  {shape:seg(9.5,6.2,10.8,1.4,.7),color:'M',outline:false}]}));}

// ---- P0006 snail on a lawn ----
function snail(){const W=22,H=18,ox=9.6,oy=8.2;
 // the shell's spiral: a band that winds out from the middle
 const spiral=(x,y)=>{const dx=x-ox,dy=y-oy,r=Math.hypot(dx,dy),a=Math.atan2(dy,dx)+Math.PI;const t=r/1.9-a/(2*Math.PI);return Math.abs(t-Math.round(t))<.2&&r<5.6?-1:1;};
 return rows(paint(W,H,{background:(x,y)=>y>=14?(y>=H-1?'M':'G'):'S',
 parts:[
  {shape:union(box(11.4,13.1,8.8,1.5,1.4),circle(18.4,10.6,2.4),box(18.4,12,2.4,1.6),seg(17.6,8.8,16.4,3.8,.5),seg(19.2,8.8,20.4,4,.5)),color:'C',shade:'A',flat:true,band:1.1},
  {shape:circle(ox,oy,5.8),color:'O',shade:'A',sphere:[ox-1,oy-1.5,6.2]},
  {shape:meet(circle(ox,oy,5.4),spiral),color:'A',outline:false},
  {shape:union(circle(16.3,3.6,.75),circle(20.5,3.8,.75)),color:'D',outline:false},
  {shape:circle(19.2,10.2,.55),color:'D',outline:false}]}));}

// ---- P0007 sailboat on the sea ----
function sailboat(){const W=20,H=22;
 return rows(paint(W,H,{background:(x,y)=>y>=15?((y+Math.floor(x/3))%4===0?'N':'B'):(y===14&&x%3!==1?'B':'S'),
 parts:[
  {shape:poly([[10.4,2.4],[10.4,14],[17.6,14]]),color:'C',shade:'S',flat:true},
  {shape:poly([[9.4,4.4],[9.4,14],[3.2,14]]),color:'C',shade:'S',flat:true},
  {shape:box(9.9,8.4,.45,7),color:'E',outline:false},
  {shape:poly([[10.4,1],[13.6,1.9],[10.4,2.8]]),color:'R',outline:false},
  {shape:poly([[1.6,14.6],[18.4,14.6],[15.6,18.6],[4.4,18.6]]),color:'E',light:'A',flat:true},
  {shape:box(10,16.2,6.4,.45),color:'R',outline:false}]}));}

// ---- P0008 ginger cat face ----
function cat(){const W=20,H=20,head=union(ellipse(10,11.4,8.2,6.8),poly([[2.6,8.4],[3.4,1.6],[8.4,5.6]]),poly([[17.4,8.4],[16.6,1.6],[11.6,5.6]]));
 return rows(paint(W,H,{background:(x,y)=>(x+y)%6===0?'P':'V',
 parts:[
  {shape:head,color:'O',light:'Y',flat:true},
  {shape:union(poly([[4,7.2],[4.4,3.4],[7.2,5.6]]),poly([[16,7.2],[15.6,3.4],[12.8,5.6]])),color:'P',outline:false},
  {shape:union(ellipse(7.6,14.4,2.8,2.2),ellipse(12.4,14.4,2.8,2.2)),color:'C',group:2},
  {shape:union(ellipse(6.6,10.4,1.5,1.8),ellipse(13.4,10.4,1.5,1.8)),color:'G',outline:false},
  {shape:union(box(6.6,10.4,.4,1.2),box(13.4,10.4,.4,1.2)),color:'D',outline:false},
  {shape:poly([[8.8,12.2],[11.2,12.2],[10,13.6]]),color:'P',outline:false}]}));}

// ---- P0009 striped fish with seaweed ----
function fish(){const W=22,H=18,body=ellipse(10,8.6,6.6,4.6);
 return rows(paint(W,H,{background:(x,y)=>((x*3+y*5)%17===0&&y<12)?'S':'B',
 parts:[
  {shape:union(seg(2,17.6,2.6,12,.7),seg(4,17.6,3.4,13.4,.6),seg(19.4,17.6,20,11.6,.7)),color:'G',outline:false},
  {shape:poly([[15.4,8.6],[20.6,4],[19.6,8.6],[20.6,13.2]]),color:'O'},
  {shape:poly([[7.4,4.6],[11,2],[13,4.6]]),color:'O'},
  {shape:body,color:'Y',light:'C',sphere:[8,6.6,7]},
  {shape:meet(body,(x,y)=>Math.abs(((x-.5*y)%4+4)%4-2)<.7&&x>7&&x<15?-1:1),color:'O',outline:false},
  {shape:circle(6.6,7.6,1.1),color:'C',outline:false},
  {shape:circle(6.4,7.6,.5),color:'D',outline:false}]}));}

// ---- P0010 cottage with a garden ----
function cottage(){const W=24,H=26;
 return rows(paint(W,H,{background:(x,y)=>y>=21?((x*7+y*3)%11===0?'C':'G'):'S',
 parts:[
  {shape:union(ellipse(5,3.6,3.4,1.5),circle(4,2.8,1.6),circle(6.2,2.6,1.9)),color:'C',outline:false},
  {shape:box(17,6.6,1.2,2.6),color:'A',shade:'E',flat:true},
  {shape:box(12,15.6,8,5.8),color:'C',shade:'A',flat:true},
  {shape:poly([[1.6,10.6],[12,2.8],[22.4,10.6]]),color:'R',shade:'E',flat:true},
  {shape:box(12,17.4,1.8,3.6,.6),color:'E',outline:false},
  {shape:box(6.6,14.2,1.8,1.6),color:'B',light:'S',flat:true},
  {shape:box(17.4,14.2,1.8,1.6),color:'B',light:'S',flat:true},
  {shape:box(12,7.4,1.3,1.3),color:'B',outline:false},
  {shape:union(circle(2.6,19.4,1.8),circle(21.4,19.4,1.8)),color:'G',shade:'E',sphere:[2,18.6,2.2]}]}));}

export default {theme:'mixed',pictures:[
 {title:'Корона',theme:'objects',rows:crown},
 {title:'Божья коровка 2C',theme:'animals',rows:ladybug},
 {title:'Птичка 3B',theme:'animals',rows:bird},
 {title:'Мухомор',theme:'plants',rows:mushroom()},
 {title:'Клубника',theme:'food',rows:strawberry()},
 {title:'Улитка',theme:'animals',rows:snail()},
 {title:'Парусник',theme:'objects',rows:sailboat()},
 {title:'Рыжий кот',theme:'animals',rows:cat()},
 {title:'Рыбка',theme:'animals',rows:fish()},
 {title:'Домик',theme:'scenes',rows:cottage()}]};

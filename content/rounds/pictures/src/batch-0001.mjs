// Batch 1 (P0001–P0010), skill bloomlings-round-pictures, rules product/17_LEVEL_GENERATION_RULES.md §2 and §4.
// P0001–P0003 are the fixed first three: crown 138 cells (63 / 37 / 38), ladybug 2C 240 (120 / 97 / 19 / 4) and bird
// 3B 281 (76 / 70 / 69 / 51 / 13 / 2). The crown is hand-drawn in three nested layers, so its colours open one after
// another (§4): the sky round it first, then the gold rim, then the red cap inside. The ladybug and the bird are
// hand-drawn to their exact volumes too. P0004–P0010 are drawn with the shape painter and follow the stage ranges.
import {circle,ellipse,box,poly,union,cut,meet,paint,rows} from '../../../../tools/gameplay-pilot-v9/draw.mjs';
import {raster,count,fit,blank,str,seg,mask} from './helpers.mjs';

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

// ---- P0004 fly agaric with a small one beside it, on a lawn with daisies ----
function mushroom(){const W=22,H=26;
 const daisy=(cx,cy)=>({shape:union(circle(cx-.9,cy,.62),circle(cx+.9,cy,.62),circle(cx,cy-.9,.62),circle(cx,cy+.9,.62)),color:'C',outline:false});
 return rows(paint(W,H,{
 background:(x,y)=>y>=20?(y>=H-2?'M':((x*5+y*3)%13===0?'M':'G')):((x===3&&y===4)||(x===17&&y===2)||(x===19&&y===9)?'C':'S'),
 parts:[
  // big stem with its ring
  {shape:union(box(10,16.4,2.6,4.6,1.2),ellipse(10,20.8,3.8,1.2)),color:'C',shade:'A',flat:true,band:1.4},
  {shape:ellipse(10,13.4,3.9,1.1),color:'C',shade:'A',flat:true},
  // big cap: red dome over a cream rim of gills
  {shape:ellipse(10,11,7.6,1.3),color:'C',shade:'A',flat:true},
  {shape:cut(ellipse(10,10,8.8,7.2),box(10,15.6,10,5)),color:'R',light:'C',sphere:[7.4,5.6,8.6],lightAt:.97},
  ...[[6,6.4,1.3],[11.4,4.4,1.4],[14.8,7.8,1.2],[9,8.6,1],[4,9.4,.85],[16.6,10,.75],[12.2,9.6,.8]].map(([x,y,r])=>({shape:circle(x,y,r),color:'C',outline:false})),
  // the small one
  {shape:box(18,20.6,1.4,2.2,.6),color:'C',shade:'A',flat:true,minOutline:8},
  {shape:cut(ellipse(18,18.4,3.6,3),box(18,21.6,4.4,2.2)),color:'R',flat:true,minOutline:8},
  {shape:union(circle(16.8,17.4,.7),circle(19.2,16.6,.55)),color:'C',outline:false},
  daisy(3,22.4),daisy(6.4,23.6),daisy(15,23.2),
  {shape:union(circle(3,22.4,.4),circle(6.4,23.6,.4),circle(15,23.2,.4)),color:'A',outline:false}]}));}

// ---- P0005 strawberry on a gingham picnic cloth ----
function strawberry(){const W=23,H=25,berry=union(poly([[3,8.8],[20,8.8],[15.8,19.8],[11.5,23.2],[7.2,19.8]]),ellipse(7.2,10.4,5,3.6),ellipse(15.8,10.4,5,3.6));
 const seeds=[];for(let y=11;y<22;y+=2.6)for(let x=4.6+(Math.round(y)%2)*1.6;x<19;x+=3.2)if(berry(x,y)<-1.1)seeds.push([x,y]);
 return rows(paint(W,H,{background:(x,y)=>{const a=Math.floor(x/3)%2,b=Math.floor(y/3)%2;return a&&b?'P':(a||b)?'K':'C';},
 parts:[
  {shape:berry,color:'R',light:'C',sphere:[8.6,10,11],lightAt:.96},
  ...seeds.map(([x,y])=>({shape:box(x,y,.45,.62),color:'Y',outline:false})),
  {shape:union(...[0,1,2,3,4,5].map(i=>{const a=Math.PI*(1.0+i*.2);return ellipse(11.5+4.4*Math.cos(a),8.6+1.6*Math.sin(a),2.8,1.25);})),color:'G'},
  {shape:seg(11.5,7.4,13.2,1.6,.75),color:'G',outline:false},
  {shape:union(ellipse(16.4,3.6,2.6,1.2)),color:'G',minOutline:6}]}));}

// ---- P0006 snail on a lawn under a leaf ----
function snail(){const W=26,H=22,ox=11,oy=9.6;
 // the shell's spiral: a band that winds out from the middle
 const spiral=(x,y)=>{const dx=x-ox,dy=y-oy,r=Math.hypot(dx,dy),a=Math.atan2(dy,dx)+Math.PI;const t=r/2.6-a/(2*Math.PI);return Math.abs(t-Math.round(t))<.2&&r<6.8?-1:1;};
 return rows(paint(W,H,{background:(x,y)=>y>=17?(y>=H-1?'M':((x*7+y)%10===0?'M':'G')):'S',
 parts:[
  {shape:union(ellipse(4.6,3.4,3.6,1.4),circle(3.6,2.6,1.6),circle(5.8,2.4,1.9)),color:'C',outline:false},
  {shape:union(box(13.6,16,10.4,1.6,1.5),circle(21.6,13,2.8),box(21.6,14.6,2.8,1.8),seg(20.6,10.6,19,4.6,.55),seg(22.6,10.6,24,4.8,.55)),color:'C',shade:'A',flat:true,band:1.2},
  {shape:circle(ox,oy,7.2),color:'O',shade:'A',sphere:[ox-1.6,oy-2,7.6]},
  {shape:meet(circle(ox,oy,6.8),spiral),color:'A',outline:false},
  {shape:union(circle(18.9,4.4,.85),circle(24.1,4.6,.85)),color:'D',outline:false},
  {shape:circle(22.6,12.6,.6),color:'D',outline:false},
  {shape:box(23,14.6,.9,.3),color:'D',outline:false}]}));}

// ---- P0007 sailboat with a striped sail on the sea ----
function sailboat(){const W=24,H=25;
 const sea=(x,y)=>y>=18?((y+Math.floor((x+(y%2)*2)/3))%4===0?'N':'B'):(y===17&&(x%4)!==1?'B':null);
 const cloud=(cx,cy,s)=>({shape:union(ellipse(cx,cy,3.4*s,1.3*s),circle(cx-1.2*s,cy-.8*s,1.4*s),circle(cx+1*s,cy-1*s,1.7*s)),color:'C',outline:false});
 return rows(paint(W,H,{background:(x,y)=>sea(x,y)||'S',
 parts:[
  cloud(4.6,4,1),cloud(19.6,2.6,.8),
  {shape:poly([[12.6,3],[12.6,17],[21.4,17]]),color:'C',shade:'S',flat:true},
  {shape:meet(poly([[12.6,3],[12.6,17],[21.4,17]]),(x,y)=>Math.floor(y/2.4)%2?-1:1),color:'R',outline:false},
  {shape:poly([[11.4,5.6],[11.4,17],[3.6,17]]),color:'C',shade:'S',flat:true},
  {shape:box(12,10,.5,8),color:'E',outline:false},
  {shape:poly([[12.6,1.2],[16.4,2.2],[12.6,3.2]]),color:'R',outline:false},
  {shape:poly([[1.8,17.6],[22.2,17.6],[18.8,22.4],[5.2,22.4]]),color:'E',light:'A',flat:true},
  {shape:box(12,19.4,8.2,.45),color:'C',outline:false},
  ...[7,10.5,14,17.5].map(x=>({shape:circle(x,20.9,.5),color:'S',outline:false}))]}));}

// ---- P0008 ginger tabby cat face ----
function cat(){const W=22,H=24,head=union(ellipse(11,14,9,7.4),poly([[3,10.6],[3.6,2.6],[9.4,7.4]]),poly([[19,10.6],[18.4,2.6],[12.6,7.4]]));
 return rows(paint(W,H,{background:(x,y)=>(x+y)%6===0&&y%2===0?'P':'V',
 parts:[
  {shape:head,color:'O',shade:'A',flat:true,band:1.2},
  {shape:union(poly([[4.4,9],[4.8,4.4],[8,7.2]]),poly([[17.6,9],[17.2,4.4],[14,7.2]])),color:'P',outline:false},
  {shape:meet(head,union(box(11,8.2,.5,1.6),box(9,8.6,.45,1.2),box(13,8.6,.45,1.2),box(3.4,15,1.4,.45),box(18.6,15,1.4,.45),box(3.4,17,1.2,.45),box(18.6,17,1.2,.45))),color:'A',outline:false},
  {shape:union(ellipse(8.6,17.6,3,2.4),ellipse(13.4,17.6,3,2.4),ellipse(11,19.6,2.2,1.6)),color:'C',group:2},
  {shape:union(ellipse(7.4,13,1.8,2.1),ellipse(14.6,13,1.8,2.1)),color:'G',outline:false},
  {shape:union(box(7.4,13,.45,1.5),box(14.6,13,.45,1.5)),color:'D',outline:false},
  {shape:union(circle(6.9,12.1,.4),circle(14.1,12.1,.4)),color:'C',outline:false},
  {shape:poly([[9.6,15.2],[12.4,15.2],[11,16.8]]),color:'P',outline:false},
  {shape:union(box(11,17.6,.4,.9),box(9.6,18.5,1.2,.35),box(12.4,18.5,1.2,.35)),color:'D',outline:false}]}));}

// ---- P0009 striped fish over a sandy bottom with seaweed ----
function fish(){const W=26,H=22,body=ellipse(11.6,9.6,7.6,5.2);
 return rows(paint(W,H,{background:(x,y)=>y>=19-Math.round(Math.sin(x/3)*.8)?((x*3+y)%7===0?'Y':'C'):(((x*3+y*5)%19===0&&y<15)?'S':'B'),
 parts:[
  {shape:union(seg(2,20,3,13,.75),seg(4.2,20,3.6,15,.65),seg(22.4,20,23.2,12.4,.75),seg(24.4,20,24,15.2,.6)),color:'G',outline:false},
  ...[[21,3,.8],[22.4,1.2,.55],[19.6,5.4,.5]].map(([x,y,r])=>({shape:circle(x,y,r),color:'S',outline:false})),
  {shape:poly([[18,9.6],[24.4,4.2],[23,9.6],[24.4,15]]),color:'O'},
  {shape:union(poly([[8,5],[12.4,1.8],[15.2,5.2]]),poly([[10,14],[13,17.4],[14.4,13.8]])),color:'O'},
  {shape:body,color:'Y',light:'C',shade:'O',sphere:[9.4,7.4,8.4],lightAt:.9},
  {shape:meet(cut(body,circle(6.2,8.6,2.6)),(x,y)=>Math.abs(((x-.45*y)%4.4+4.4)%4.4-2.2)<.75&&x>8.6&&x<18?-1:1),color:'O',outline:false},
  {shape:circle(7.2,8.4,1.4),color:'C',outline:false},
  {shape:circle(7,8.4,.75),color:'D',outline:false},
  {shape:box(4.8,11,1,.35),color:'O',outline:false}]}));}

// ---- P0010 cottage with a garden, fence and tree ----
function cottage(){const W=30,H=30;
 const roof=poly([[6,13],[17,3.6],[28,13]]);
 return rows(paint(W,H,{background:(x,y)=>y>=24?((x*7+y*3)%11===0?'C':'G'):(y===23&&x%2===0?'G':'S'),
 parts:[
  {shape:union(ellipse(6,4.4,3.8,1.6),circle(4.8,3.4,1.8),circle(7.4,3.2,2.2)),color:'C',outline:false},
  {shape:union(ellipse(24,6.6,3,1.3),circle(25,5.8,1.6)),color:'C',outline:false},
  // tree on the left
  {shape:box(3.4,20,.9,4),color:'E',outline:false},
  {shape:union(circle(3.4,14.6,3),circle(2,17,2.2),circle(5,16.6,2.4)),color:'G',outline:true},
  // chimney, walls, roof with tiles
  {shape:box(23,7.6,1.3,2.8),color:'A',shade:'E',flat:true},
  {shape:box(17,18.4,8.6,5.6),color:'C',shade:'A',flat:true},
  {shape:roof,color:'R',shade:'E',flat:true},
  {shape:meet(roof,(x,y)=>((Math.floor(y)%2===0)&&((Math.floor(x)+Math.floor(y))%3===0))?-1:1),color:'E',outline:false},
  {shape:circle(17,9.4,1.6),color:'B',light:'S',flat:true,minOutline:6},
  // door and windows
  {shape:box(17,20.4,2,3.6,.8),color:'E',light:'A',flat:true},
  {shape:circle(18.3,20.8,.35),color:'C',outline:false},
  ...[11.6,22.4].map(x=>({shape:box(x,17.2,2.4,2),color:'B',light:'S',flat:true})),
  ...[11.6,22.4].map(x=>({shape:box(x,17.2,.3,2),color:'C',outline:false})),
  ...[11.6,22.4].map(x=>({shape:box(x,19.6,2.4,.5),color:'R',outline:false})),
  // path and fence
  {shape:poly([[15.2,24],[18.8,24],[20.8,30],[13.2,30]]),color:'A',outline:false},
  ...[0,1,2,3,4,5].flatMap(i=>[21.6+i*1.6,1.2+i*2.2].map(x=>({shape:box(x,24.6,.4,1.6),color:'C',outline:false}))),
  {shape:union(box(7,24.4,6,.3),box(25.6,24.4,4.4,.3)),color:'C',outline:false},
  ...[[4,27.4],[7,28.2],[22,27.6],[25.6,28.4],[9.6,26.8],[20.4,26.6]].map(([x,y])=>({shape:circle(x,y,.6),color:'R',outline:false}))]}));}

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

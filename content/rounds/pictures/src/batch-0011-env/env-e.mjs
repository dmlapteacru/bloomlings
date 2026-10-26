// Batch 0011 with environment, part E: L50 elephant in the savanna, L58 raccoon in a forest clearing.
import {circle,ellipse,box,poly,union,cut,meet} from '../../../../../tools/gameplay-pilot-v9/draw.mjs';
import {raster,fit,str,put,ring4,scale,seg} from '../helpers.mjs';
const draw=(w,h,k,layers)=>raster(Math.round(w*k),Math.round(h*k),'.',layers.map(([s,c])=>[scale(k,s),typeof c==='function'?(x,y,u)=>c(x/k,y/k,u):c]));
const mv=(f,dx,dy)=>(x,y)=>f(x-dx,y-dy);
const all=()=>-1;
// a colour that turns to `dark` in a band of width s on the far (lower right) side of shape f
const shade=(f,lit,dark,s=1.6,dx=1,dy=1)=>(x,y)=>f(x+.5+s*dx,y+.5+s*dy)>=0?dark:lit;

// 1. A baby elephant raising its trunk and showering a flower, under an orange evening sky with a low sun, on savanna grass.
function elephant(){
 const k=.75,DX=1,DY=3;
 const body=ellipse(14,25,12.8,9.4);
 const head=circle(24,15.3,9);
 const trunk=union(seg(29.5,20,32.5,14,2.3),seg(32.5,14,33.5,8,2),seg(33.5,8,35.8,4.8,1.7));
 const far=union(box(7.5,31.5,2.6,3.5,.8),box(20,31.5,2.6,3.5,.8));
 const near=union(box(11.5,32.5,2.7,3.5,.8),box(25,32,2.7,3.5,.8));
 const ear=ellipse(18,15.5,6.4,7.6);
 const S=[
  [far,'P'],[body,'K'],[cut(body,ellipse(14.6,23.4,12.8,9.4)),'P'],[near,'K'],
  [box(1.2,23,1.2,.6),'K'],[circle(.8,24.2,1),'P'],
  [head,'K'],[trunk,'K'],
  [ear,'K'],[ellipse(17.8,15.8,4.5,5.6),'P'],
  [seg(40,37,40,22,1),'G'],[union(ellipse(37.4,30,2.6,1.3),ellipse(42.4,28,2.4,1.3)),'G'],
  [union(circle(40,15.8,2.2),circle(37.6,18,2.2),circle(42.4,18,2.2),circle(38.5,20.7,2.2),circle(41.5,20.7,2.2)),'R'],
  [circle(40,18.3,1.6),'Y'],
 ].map(([s,c])=>[mv(s,DX,DY),c]);
 const g=draw(47,43,k,[
  [all,'O'],                                                       // evening sky
  [circle(7.5,9,5.2),'Y'],                                         // low sun
  [(x,y)=>36.2-y+.6*Math.sin(x*.55),'M'],                          // savanna grass with a soft wavy edge
  ...S,
 ]);
 // eyes, smile and the falling drops, set on the scaled grid
 const W=g[0].length;
 put(g,'D',[[20,12],[20,13],[19,16],[20,17],[21,17]]);
 const drop=['.B.','BBB','BBB'];
 for(const [ox,oy] of [[28,1],[31,4],[30,8]])drop.forEach((r,j)=>[...r].forEach((c,i)=>{if(c==='B'&&ox+i<W)g[oy+j][ox+i]='B';}));
 return str(g);}

// 2. A sitting raccoon with a bandit mask, a ringed tail and a striped scarf, in a forest clearing: sky, two trunks, grass.
function raccoon(){
 const k=.8,DX=3.5,DY=1;
 const head=union(ellipse(16,12,9.6,7.4),poly([[7,10],[2.6,15.6],[9,17.4]]),poly([[25,10],[29.4,15.6],[23,17.4]]));
 const ears=union(poly([[6.6,9.5],[7.6,1.8],[13.4,5.6]]),poly([[25.4,9.5],[24.4,1.8],[18.6,5.6]]));
 const inEars=union(poly([[8.2,8.2],[8.6,3.6],[12.4,6.2]]),poly([[23.8,8.2],[23.4,3.6],[19.6,6.2]]));
 const body=ellipse(16,29,10.4,9);
 const tailPath=union(seg(25,34.4,30.6,31.4,3),seg(30.6,31.4,32,18.6,3.5),circle(31.8,17.2,3.2));
 const tailCol=(x,y)=>y<16?'D':Math.floor((y-16)/3)%2?'D':'A';
 const scarf=union(box(16,19.8,9.6,1.9,1.4),box(21.6,25.5,2.3,5,.6));
 const scarfCol=(x,y)=>y>=22?(Math.floor(y/2)%2?'R':'Y'):(Math.floor(x/2)%2?'R':'Y');
 const sub=[[tailPath,tailCol],[ears,'A'],[inEars,'K'],[body,shade(body,'A','E',1.8,1,.3)],
  [ellipse(16,31,6.2,6.6),'C'],[ellipse(10.4,37.2,3.2,1.6),'D'],[ellipse(21.6,37.2,3.2,1.6),'D'],
  [head,'A'],[union(ellipse(11.4,9,3.4,1.4),ellipse(20.6,9,3.4,1.4)),'C'],
  [union(ellipse(11.2,12.4,4.2,2.4),ellipse(20.8,12.4,4.2,2.4),box(16,10,1,3)),'D'],
  [ellipse(16,16.2,5,2.8),'C'],[scarf,scarfCol],[union(ellipse(11.4,27,1.8,1.4),ellipse(17,27,1.8,1.4)),'D']]
  .map(([s,c])=>[mv(s,DX,DY),typeof c==='function'?(x,y,u)=>c(x-DX,y-DY,u):c]);
 const g=draw(44,41,k,[
  [all,'S'],                                                       // sky over the clearing
  [union(box(1.4,14,2,15),box(42.4,14,2,15)),'E'],                // two tree trunks at the edges
  [union(ellipse(1.4,29.4,3.4,1.2),ellipse(42.4,29.4,3.4,1.2)),'E'], // their root flares
  [(x,y)=>29.6-y+.5*Math.sin(x*.6),'G'],                           // grass of the clearing
  ...sub,
 ]);
 put(g,'C',[[11,10],[20,10]]);                                    // eyes shining in the mask
 put(g,'D',[[15,12],[16,12],[15,13],[16,13]]);                    // nose
 return str(g);}

export default {theme:'batch-0011-env',pictures:[
 {title:'Слонёнок с лейкой',forLevel:50,family:'Саванна и джунгли',subject:'слонёнок поливает цветок из хобота',source:'оригинальный',rows:elephant()},
 {title:'Енот в шарфе',forLevel:58,family:'Лесные и горные животные',subject:'енот в полосатом шарфе',source:'оригинальный',rows:raccoon()},
]};

// Batch 0011 with environment, part D: the astronaut on an asteroid in space, the skating penguin on a rink (L42, L44).
import {circle,ellipse,box,poly,union,cut,meet} from '../../../../../tools/gameplay-pilot-v9/draw.mjs';
import {raster,fit,str,put,ring4,scale,seg} from '../helpers.mjs';
const draw=(w,h,k,layers)=>raster(Math.round(w*k),Math.round(h*k),'.',layers.map(([s,c])=>[scale(k,s),typeof c==='function'?(x,y,u)=>c(x/k,y/k,u):c]));
const grow=(f,d)=>(x,y)=>f(x,y)-d;
const stamp=(g,x0,y0,rows)=>rows.forEach((r,j)=>[...r].forEach((c,i)=>{if(c!=='.'&&g[y0+j]&&g[y0+j][x0+i]!==undefined)g[y0+j][x0+i]=c;}));
// A big four-pointed star: a plus of five cells.
const star=(g,x,y,c)=>put(g,c,[[x,y],[x-1,y],[x+1,y],[x,y-1],[x,y+1]]);

// 1. A small astronaut on a round asteroid, watering one flower with a can, in dark space with big stars and a ringed planet.
function astronaut(){
 const W=36,H=32;
 const rock=union(ellipse(17,25.6,13.6,4.8),circle(6.4,24.2,3.4),circle(27.8,24.4,3.2),circle(17,27.8,4.2));
 const rockLit=union(ellipse(15.8,24.8,13.6,4.8),circle(5.2,23.4,3.4),circle(26.6,23.4,3.2),circle(15.8,26.8,4.2));
 const helmet=circle(11.5,7.6,4.5),visor=ellipse(12.7,7.9,3,2.3);
 const torso=box(11.5,14.6,3.5,3.3,1.3),pack=box(6.7,13.8,1.8,3.4,0.7);
 const legL=box(9.7,19.2,1.45,2.1,0.5),legR=box(13.3,19.2,1.45,2.1,0.5);
 const armR=union(seg(14.2,12.4,16.8,10.2,1.15),circle(17.3,9.8,1.25));
 const can=box(19.6,8.6,2.2,1.9,0.6),handle=cut(circle(19.4,6.3,1.8),circle(19.4,6.3,0.75));
 const spout=seg(21.4,9.2,24.6,11.2,0.7),rose=circle(25.1,11.5,1.1);
 const planet=circle(31,5,2.7),ring=seg(26.2,7.2,35.8,2.8,0.55);
 const suit=part=>[[grow(part,0.95),'D'],[part,'C']];
 const g=raster(W,H,'N',[
  [rock,'E'],[meet(rock,rockLit),'A'],
  [ellipse(11,24.5,2.1,1.2),'E'],[ellipse(20,27.4,2.4,1.3),'E'],[ellipse(24,23.6,1.5,0.9),'E'],[ellipse(13.5,28.4,1.3,0.8),'E'],
  ...suit(pack),...suit(legL),...suit(legR),...suit(torso),...suit(armR),
  [box(11.5,14.8,1.3,1),'O'],
  ...suit(helmet),[visor,'D'],
  [handle,'O'],[can,'O'],[spout,'O'],[rose,'O'],
  // a small ringed planet in the top right corner
  [planet,'P'],[ring,'C'],
 ]);
 // the flower, by hand: four round petals round an orange heart, on a stem with a leaf, growing from the rock
 const flower=['.PP.PP.','PPPPPPP','PPOOOPP','.POOOP.','PPOOOPP','PPPPPPP','.PP.PP.','...GG..','...GGGG','..GGGG.','...GG..'];
 stamp(g,27,14,flower);
  put(g,'C',[[25,14],[25,15],[23,13],[23,14]]);              // falling water drops
 for(const [x,y] of [[3,3],[23,2],[2,20],[33,10],[32,28],[2,29]])star(g,x,y,'Y');
 return str(g);
}

// 2. A penguin skating in a scarf on a rink: winter sky behind, light ice with skate lines below.
function penguin(){
 const W=33,H=35,k=0.95,ox=0.6,oy=0.4,horizon=22;
 const T=s=>(x,y)=>s((x-ox)/k,(y-oy)/k)*k;
 const body=union(ellipse(16,20,8.6,10.4),circle(18.4,8.6,6.4),poly([[11,14],[24.4,10],[24,20],[10,20]]));
 const belly=union(ellipse(19,21.4,5.8,8.2),circle(20.8,9.6,3.8),poly([[17,10],[24,10],[24.4,20],[15,20]]));
 const flipBack=seg(10,18.5,3,18.8,1.7);
 const flipFront=seg(21.5,17.5,27,23,1.6);
 const beak=poly([[24.2,7.8],[29.2,9.4],[24.2,11]]);
 const knot=box(21.6,16.4,1.3,2.4,0.4);
 const scarf=union(meet(body,(x,y)=>Math.abs(y-(13.4-0.12*(x-17)))-1.05),knot);
 const tail=union(seg(12,13.2,6.5,11.2,1.15),seg(6.5,11.2,2.6,12.6,1.15));
 // the rink: two long curved skate lines on the ice
 const lineA=(x,y)=>Math.abs(y-(26.2+0.010*(x-4)*(x-4)))-0.5;
 const lineB=(x,y)=>Math.abs(y-(32.6-0.008*(x-30)*(x-30)))-0.5;
 const env=(x,y)=>y<horizon?'B':(lineA(x+.5,y+.5)<0&&x<30)||(lineB(x+.5,y+.5)<0&&x>3)?'B':'S';
 const g=raster(W,H,'.',[
  [()=>-1,(x,y)=>env(x,y)],
  [T(tail),'R'],[meet(T(tail),(x,y)=>x-(3.6*k+ox)>0?1:-1),'Y'],
  [T(flipBack),'D'],
  [T(body),'D'],[T(belly),'C'],[T(flipFront),'D'],
  [T(scarf),'R'],[meet(T(knot),(x,y)=>y-(17.9*k+oy)>0?-1:1),'Y'],
  [T(beak),'O'],
 ]);
 // the skates, by hand: deep blue boots on dark blades with a curled toe; the back one raised behind
 stamp(g,11,28,['....OO......','...NNNN.....','...NNNNNNN..','...NNNNNNN..','....D....D.D','..DDDDDDDDD.']);
 stamp(g,0,22,['........OO','....NNNNO.','NNNNNNNN..','NNNNNNNN..','.D....D...','DDDDDDDDD.']);
 ring4(g,['C'],'D','S');                                   // a dark rim where the white front meets the ice
 for(const [x,y] of [[4,4],[11,2],[30,3],[29,12],[5,15]])star(g,x,y,'C');   // big snowflakes in the winter sky
 return {g,eye:[20,6]};
}
function penguinRows(){const {g,eye}=penguin();put(g,'D',[eye,[eye[0],eye[1]+1]]);return str(g);}

export default {theme:'batch-0011-env',pictures:[
 {title:'Космонавт поливает цветок на астероиде',forLevel:42,family:'Космос',direction:'Космическая повседневность',subject:'космонавт поливает цветок на астероиде',source:'оригинальный',rows:astronaut()},
 {title:'Пингвин на коньках',forLevel:44,family:'Птицы',subject:'пингвин катается на коньках в шарфе',source:'оригинальный',rows:penguinRows()},
]};

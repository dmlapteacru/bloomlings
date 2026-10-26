// Batch 0011 with environment, part C: the octopus conductor under the sea and the bathyscaphe in the deep sea (L25, L46).
import {circle,ellipse,box,poly,union,cut,meet} from '../../../../../tools/gameplay-pilot-v9/draw.mjs';
import {raster,fit,str,put,ring4,scale,seg} from '../helpers.mjs';
// A shape drawn in base units, scaled by k and moved to (ox,oy) on the board.
const place=(k,ox,oy,s)=>(x,y)=>s((x-ox)/k,(y-oy)/k)*k;
// A tube along a polyline whose radius goes from r0 to r1.
const tube=(pts,r0,r1)=>{const n=pts.length-1;return union(...pts.slice(0,-1).map((p,i)=>seg(p[0],p[1],pts[i+1][0],pts[i+1][1],r0+(r1-r0)*(i+.5)/n)));};
// Paint every empty cell with the environment colour function.
const fill=(g,f)=>{g.forEach((r,y)=>r.forEach((v,x)=>{if(v==='.')r[x]=f(x,y);}));return g;};

function octopus(){
 const W=36,H=32,k=.67,ox=3.2,oy=1.4,P=s=>place(k,ox,oy,s),X=v=>Math.floor(ox+v*k),Y=v=>Math.floor(oy+v*k);
 const T=[
  tube([[10,19],[5,21],[2,25],[2,30],[4,33],[7,32.5]],3.1,1.6),
  tube([[13,21],[11,26],[9.5,31],[10,35],[13,36]],3.1,1.6),
  tube([[17,22],[17,28],[17,33],[19,36],[21.5,35]],3.1,1.6),
  tube([[21,21],[24,26],[26,31],[27,35],[30,36.5],[32,35]],3.1,1.6),
  tube([[24,19],[29,22],[33,26],[35,31],[37.5,33.5],[40,32.5]],3.1,1.6),
  tube([[24,17],[29,17.5],[33,15],[35,10],[34.5,6.5]],3.2,2.2),   // raised tentacle gripping the baton
 ].map(P);
 const head=P(union(ellipse(17,11,10,10),ellipse(17,17,8.5,5)));
 const g=raster(W,H,'.',[
  ...T.map(t=>[t,'V']),
  [head,'V'],
  [union(...T.map(t=>(x,y)=>t(x,y)<-.95?-1:1)),(x,y)=>((x+y)%3===0)?'P':'V'],   // suckers
  [P(union(ellipse(12.6,12,3,3.5),ellipse(21.4,12,3,3.5))),'C'],                  // eyes
  [P(union(poly([[11.5,18.5],[11.5,24],[17,21.2]]),poly([[22.5,18.5],[22.5,24],[17,21.2]]))),'R'],  // bow tie
  [P(box(17,21.2,1.3,1.3)),'D'],
 ]);
 // pupils looking up at the baton
 for(const ex of [13.6,22.4])put(g,'D',[[X(ex),Y(10.5)],[X(ex),Y(10.5)+1]]);
 // the baton: a two-cell white stick up and to the right of the raised tip, with a dark edge
 const bx=X(34.5),by=Y(6.5);
 for(let i=0;i<=by-1;i++)put(g,'C',[[bx+i,by-i],[bx+i+1,by-i]]);
 ring4(g,['C'],'D','.');
 // sea floor: sand with a gentle wavy top and ripples, two seaweed fronds, big bubbles
 const sandTop=x=>27.5+Math.sin(x*.45)*.6;
 const weed=union(tube([[2.5,32],[2,27.5],[3,24],[2.2,20.5],[3,18]],1.15,.9),tube([[33.5,32],[34.1,28],[33.2,24.5],[34,21.5]],1.15,.9));
 const bubbles=[[4.5,6.5,2.6],[3,13,2],[31,15,2.6],[33,9,2],[26,2,2]];
 return str(fill(g,(x,y)=>{const cx=x+.5,cy=y+.5;
  if(weed(cx,cy)<0)return 'M';
  if(cy>sandTop(x))return (y===30&&x%7>=2&&x%7<=4)?'C':'Y';
  for(const [bx,by,r] of bubbles){if(Math.hypot(cx-bx,cy-by)<r)return (r>2.3&&x===Math.floor(bx-1)&&y===Math.floor(by-1))?'C':'S';}
  return 'B';}));
}

function bathyscaphe(){
 const W=36,H=31,k=.7,ox=.4,oy=1.6,P=s=>place(k,ox,oy,s);
 const hull=ellipse(21,18.5,15.5,13);
 const shade=cut(hull,ellipse(19.2,16.8,15.5,13));
 const tower=box(18,5.8,5.4,3.2,1);
 const g=raster(W,H,'.',[
  [P(box(5,18.5,3,1.6)),'D'],                                      // shaft
  [P(union(ellipse(2.8,12,2.6,5),ellipse(2.8,25,2.6,5))),'S'],     // blades
  [P(box(4,18.5,2.8,2.8,.8)),'D'],                                 // hub
  [P(union(box(14,32,1.6,2.8),box(28,32,1.6,2.8))),'D'],           // skid legs
  [P(box(21,34.6,12,1.5,1)),'D'],                                  // skid bar
  [P(tower),'Y'],[P(box(18,2.2,3,1.4,.5)),'D'],                     // tower and hatch
  [P(box(18,.6,1.5,1.5)),'R'],                                     // beacon
  [P(hull),'Y'],[P(shade),'O'],
  [P(meet(hull,box(21,18.5,15.6,1.3))),'O'],                        // seam band
  [P(box(35.8,25,2.4,3.4,.6)),'D'],                                // lamp
  [P(circle(23,14,7.8)),'D'],[P(circle(23,14,5.6)),'S'],            // porthole
  [P(union(circle(21,12,1.7),circle(24.8,10.8,.9))),'C'],          // glint
 ]);
 // the light cone from the lamp to the right edge, the deep sea round it, the sea floor below
 const lx=ox+38*k, ly=oy+25*k;
 const beam=poly([[lx-.5,ly-2],[W+1,ly-9],[W+1,ly+6],[lx-.5,ly+2]]);
 const floorTop=x=>27.4+Math.sin(x*.5+1)*.6;
 const rocks=union(ellipse(3.5,27.8,3.4,2.2),ellipse(31.5,28,2.8,1.8));
 const bubbles=[[7,3,2],[4,8.5,1.5],[29,3.5,2.6],[33,8,2]];
 return str(fill(g,(x,y)=>{const cx=x+.5,cy=y+.5;
  if(rocks(cx,cy)<0)return 'E';
  if(cy>floorTop(x))return 'A';
  if(beam(cx,cy)<0)return 'C';
  for(const [bx,by,r] of bubbles)if(Math.hypot(cx-bx,cy-by)<r)return 'S';
  return 'N';}));
}

export default {theme:'batch-0011-env',pictures:[
 {title:'Осьминог-дирижёр',forLevel:25,family:'Морские и пресноводные обитатели',direction:'Обмен ролями',subject:'осьминог дирижирует палочкой, одно щупальце поднято',source:'оригинальный',rows:octopus()},
 {title:'Батискаф',forLevel:46,family:'Подводные экспедиции',subject:'батискаф с иллюминатором, винтом и лучом прожектора',source:'оригинальный',rows:bathyscaphe()},
]};

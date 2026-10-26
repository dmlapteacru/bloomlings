// Batch 0061, part 05: levels 81–85 (marching drum, easel with palette, football, hedgehog chef, microscope).
import {circle,ellipse,box,poly,union,cut,meet} from '../../../../../tools/gameplay-pilot-v9/draw.mjs';
import {raster,fit,str,put,ring4,scale,seg} from '../helpers.mjs';
const draw=(w,h,k,layers)=>raster(Math.round(w*k),Math.round(h*k),'.',layers.map(([s,c])=>[scale(k,s),typeof c==='function'?(x,y,u)=>c(x/k,y/k,u):c]));
// drop empty border rows and columns
const trim=g=>{let t=0,b=g.length-1,l=0,r=g[0].length-1;
 while(g[t].every(v=>v==='.'))t++;while(g[b].every(v=>v==='.'))b--;
 while(g.every(row=>row[l]==='.'))l++;while(g.every(row=>row[r]==='.'))r--;return g.slice(t,b+1).map(row=>row.slice(l,r+1));};
const band=(cx,y0,y1,rx,ry)=>union(ellipse(cx,y0,rx,ry),box(cx,(y0+y1)/2,rx,(y1-y0)/2),ellipse(cx,y1,rx,ry));

// 1. A marching drum: red rims, cream head, blue shell with a yellow zigzag rope, two crossed sticks above.
function drum(){
 const cx=17,rx=14.5,ry=3.6,top=15,bot=34;
 const front=y0=>union(ellipse(cx,y0,rx+.6,ry),(x,y)=>y-y0);
 const shell=band(cx,top,bot,rx,ry);
 const rimT=band(cx,top,top+2.6,rx+.6,ry),rimB=cut(band(cx,bot-2.6,bot,rx+.6,ry),front(bot-2.6));
 const head=ellipse(cx,top,rx-1.6,ry-1.1);
 // zigzag rope: a polyline between the rims
 const y0=top+2.6+ry+.4,y1=bot-2.6+ry-1.6,pts=[];for(let i=-4;i<=4;i++)pts.push([cx+i*4,i%2?y1:y0]);
 const rope=union(...pts.slice(1).map((p,i)=>seg(...pts[i],...p,.8)));
 const stickA=seg(4,3.5,26,14.5,1.05),stickB=seg(30,3.5,8,14.5,1.05);
 const g=raster(36,40,'.',[
  [shell,'B'],[meet(shell,(x,y)=>x>cx+9?-1:1),'N'],[meet(rope,shell),'Y'],
  [rimB,'R'],[rimT,'R'],[head,'C'],
  [stickA,'A'],[stickB,'A'],[circle(4,3.5,1.9),'R'],[circle(30,3.5,1.9),'R']]);
 return str(trim(g));}

// 2. An easel with a small flower painting, and a palette with paint blobs and a brush in front.
function easel(){
 const g=raster(44,46,'.',[
  // legs: two front legs and the back one, the cross bar
  [union(seg(8,42,14.5,6,1.1),seg(30,42,23.5,6,1.1)),'E'],[seg(19,2.5,19,38,1),'E'],
  [box(19,33,11,1),'E'],
  // canvas with a frame and a ledge
  [box(19,15.5,12.5,11),'E'],[box(19,15.5,11.5,10),'C'],[box(19,27.6,13.5,1.1),'E'],
  // the painted flower
  [seg(19,16,19,24,.9),'G'],[union(ellipse(15.2,21,2.8,1.4),ellipse(22.8,20.4,2.8,1.4)),'G'],
  [union(circle(19,8.4,2.2),circle(15.4,11.4,2.2),circle(22.6,11.4,2.2),circle(16.8,15.3,2.2),circle(21.2,15.3,2.2)),'P'],
  [circle(19,12.2,1.8),'Y'],
  // palette with a thumb hole, paint blobs
  [cut(union(ellipse(32,37.5,10.5,6.4),ellipse(25.5,36,5,4.6)),circle(25,39.2,1.6)),'A'],
  [circle(27.6,34.2,1.6),'R'],[circle(31.8,33,1.6),'Y'],[circle(39,35.4,1.6),'B'],[circle(37.6,39.8,1.6),'G'],[circle(32.6,41.2,1.6),'V'],
  // brush across the palette
  [seg(26,43.5,37,33.5,.95),'E'],[seg(37,33.5,38.8,31.8,1.05),'C'],[seg(38.8,31.8,40.8,30,1.1),'P']]);
 return str(trim(g));}

// 3. A classic football: cream ball, a black pentagon in the middle and five at the rim, a light-blue shadow side.
function football(){return [
 '........DDDDD........',
 '.....DDDDDDDDDDD.....',
 '....DCCCDDDDDCCCD....',
 '...DCCCCCDDDCCCCCD...',
 '..DCCCCCCCDCCCCCCCD..',
 '.DCCCCCCCCCCCCCCCCCD.',
 '.DDCCCCCCCCCCCCCCCDD.',
 '.DDDCCCCCDDDCCCCCDDD.',
 'DDDDDCCCDDDDDCCCDDDDD',
 'DDDDCCCDDDDDDDCCCDDDD',
 'DDDCCCCDDDDDDDCCCCDDD',
 'DCCCCCCCDDDDDCCCCSSSD',
 'DCCCCCCCCDDDCCCCSSSSD',
 '.DCCCCCCCCCCCCCSSSSD.',
 '.DCCCDCCCCCCCCCDSSSD.',
 '.DCCDDCCCCCCCCSDDSSD.',
 '..DDDDDCCCCCCCDDDDD..',
 '...DDDDCCCCCCSDDDD...',
 '....DCCCCCCCCSSSD....',
 '.....DDDCCCCSDDD.....',
 '........DDDDD........',
];}

// 4. A hedgehog chef in a toque and a neckerchief stirring a red pot with a ladle.
function hedgehog(){
 const cx=14,cy=22,r0=10,spikes=[];for(let i=0;i<7;i++){const a=Math.PI*(0.68+i*0.155);
  spikes.push(poly([[cx+r0*Math.cos(a-.22),cy+r0*1.2*Math.sin(a-.22)],[cx+(r0+5)*Math.cos(a),cy+(r0+5)*1.2*Math.sin(a)],[cx+r0*Math.cos(a+.22),cy+r0*1.2*Math.sin(a+.22)]]));}
 const spines=union(ellipse(cx,cy,10.5,12.6),...spikes);
 const g=raster(44,46,'.',[
  [spines,'E'],
  // head, snout, belly
  [union(circle(21,17.5,5.6),ellipse(25.5,19,5,2.6)),'C'],
  [ellipse(20,30.5,5.2,7),'C'],
  // ear
  [circle(18.2,12.4,1.6),'K'],
  // neckerchief
  [poly([[16,22.4],[24.5,22.4],[21,26.6]]),'O'],
  // feet
  [union(ellipse(15,38.6,3.2,1.6),ellipse(22,38.6,3.2,1.6)),'E'],
  // toque
  [union(circle(17,4.6,3.8),circle(12.6,6.6,3.2),circle(21.4,6.6,3.2),box(17,8.4,5.8,2.2)),'C'],
  [box(17,10.9,6.6,1.2),'S'],
  // ladle in the pot, the arm holding it
  [seg(25,24,31,28.5,.85),'D'],
  [seg(19,28,24.6,24.2,1.3),'E'],[circle(25,24,1.6),'E'],
  // pot with rim, soup and handles
  [box(32,34.5,7.6,5.5,1.5),'R'],[box(32,29,8.6,1.1,.5),'D'],[ellipse(32,28,7,1.3),'O'],
  [box(23.4,32,1.2,1.4),'D'],[box(40.6,32,1.2,1.4),'D'],[box(32,39.6,5.6,.9),'D'],
  // steam
  [union(circle(35,24.4,1.4),circle(36.4,21.6,1.4),circle(35.2,19,1.3)),'S']]);
 put(g,'D',[[22,15],[22,16],[29,18],[30,18],[29,19],[30,19]]);put(g,'K',[[23,19],[24,19]]);
 return str(trim(g));}

// 5. A classic microscope: dark base, blue arm, light-blue tube, eyepiece, lens turret, a slide on the stage.
function microscope(){
 const t0=[9.5,6],t1=[14,19.5];
 const arm=union(seg(25,37,25.5,24,3.4),seg(25.5,24,22.5,13,3.2),seg(22.5,13,15,9.5,2.8));
 const tube=seg(...t0,...t1,3.2);
 const sh=(f,d)=>(x,y)=>f(x-d,y);
 const g=draw(35,42,1.04,[
  // base
  [box(17.5,39.6,14,2.4,1.2),'D'],
  // arm with a light front edge
  [arm,'B'],[sh(arm,1.4),'N'],
  // stage: top surface, slide with a sample, front edge
  [poly([[2.5,26.6],[25,26.6],[27.4,30],[4.9,30]]),'N'],[poly([[8,27.4],[19.6,27.4],[20.6,29.2],[9,29.2]]),'S'],[box(14.3,28.3,1.3,.9),'P'],
  [box(15,31.3,12.4,1.3),'D'],
  // tube with a dark back edge
  [tube,'N'],[sh(tube,-1.5),'B'],
  // eyepiece
  [seg(8.2,3.6,8.8,5.2,3.4),'D'],[box(7.9,1.3,3.4,1.2,.5),'C'],
  // turret and objectives
  [ellipse(14.4,21,5.4,2),'D'],[box(12,23.6,1.1,1.5),'C'],[box(16.8,23.4,1.1,1.3),'C'],
  // focus knob
  [circle(27.5,22,2.7),'D'],[circle(27.5,22,1.2),'C']]);
 return str(trim(g));}

export default {theme:'batch-0061',pictures:[
 {title:'Барабан',forLevel:81,family:'Музыка',subject:'маршевый барабан с зигзагом шнура и двумя скрещёнными палочками',source:'оригинальный',rows:drum()},
 {title:'Мольберт и палитра',forLevel:82,family:'Рисование и ремёсла',subject:'мольберт с картиной-цветком, палитра с красками и кисть',source:'оригинальный',rows:easel()},
 {title:'Футбольный мяч',forLevel:83,family:'Спорт и активные игры',subject:'классический чёрно-белый футбольный мяч с пятиугольниками',source:'оригинальный',rows:football()},
 {title:'Ёжик-повар',forLevel:84,family:'Профессии и увлечения',direction:'Обмен ролями',subject:'ёжик в поварском колпаке мешает суп в кастрюле половником',source:'оригинальный',rows:hedgehog()},
 {title:'Микроскоп',forLevel:85,family:'Лаборатории и изобретения',subject:'классический микроскоп с окуляром, револьвером объективов и предметным стеклом',source:'оригинальный',rows:microscope()},
]};

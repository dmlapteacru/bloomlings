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
 const front=y0=>union(ellipse(cx,y0,rx+.6,ry),(x,y)=>y0-y);
 const shell=band(cx,top,bot,rx,ry);
 const rimT=band(cx,top,top+2.6,rx+.6,ry),rimB=cut(band(cx,bot-2.6,bot,rx+.6,ry),front(bot-2.6));
 const head=ellipse(cx,top,rx-1.6,ry-1.1);
 // zigzag rope: a triangle wave between the rims
 const tri=x=>{const p=x/6.5-Math.floor(x/6.5);return Math.abs(p*2-1);};
 const rope=(x,y)=>{const y0=top+2.6+ry+.6,y1=bot-2.6+ry*.2-1.4;const yy=y0+(y1-y0)*tri(x-cx+3.25);return Math.abs(y-yy)-1.1;};
 const stickA=seg(4,3.5,26,14.5,1.05),stickB=seg(30,3.5,8,14.5,1.05);
 const g=raster(36,40,'.',[
  [shell,'B'],[meet(shell,(x,y)=>x>cx+9?-1:1),'N'],[meet(rope,shell),'Y'],
  [rimB,'R'],[rimT,'R'],[head,'C'],
  [stickA,'A'],[stickB,'A'],[circle(4,3.5,1.9),'C'],[circle(30,3.5,1.9),'C']]);
 return str(trim(g));}

// 2. An easel with a small flower painting, and a palette with paint blobs and a brush in front.
function easel(){
 const g=raster(42,44,'.',[
  // legs: two front legs and the back one, the cross bar
  [union(seg(9,40,15,6,1.1),seg(29,40,23,6,1.1)),'E'],[seg(19,3,19,36,1),'E'],
  [box(19,31,10.5,1),'E'],
  // canvas with a frame and a ledge
  [box(19,15,11.5,10),'E'],[box(19,15,10.5,9),'C'],[box(19,26,12.5,1),'E'],
  // the painted flower
  [union(seg(19,16,19,23,.8)),'G'],[union(ellipse(15.5,20,2.6,1.3),ellipse(22.5,19.5,2.6,1.3)),'G'],
  [union(circle(19,8.6,2),circle(15.6,11.4,2),circle(22.4,11.4,2),circle(16.9,15,2),circle(21.1,15,2)),'P'],
  [circle(19,12.2,1.7),'Y'],
  // palette with a thumb hole, paint blobs
  [cut(union(ellipse(31,36,10,6),ellipse(25,34.5,5,4.4)),circle(24.5,37.5,1.6)),'A'],
  [circle(28,32.6,1.5),'R'],[circle(32.6,32.4,1.5),'Y'],[circle(36.8,34.4,1.5),'B'],[circle(36.2,38.8,1.5),'G'],[circle(31.2,39.6,1.5),'V'],
  // brush across the palette
  [seg(25,42,37,28,.9),'R'],[seg(37,28,38.6,26,1),'C'],[seg(38.6,26,40.2,24,.95),'B']]);
 return str(trim(g));}

// 3. A classic football: cream ball, black pentagon patches, a light-blue shadow side, dark outline.
function football(){
 const c=10.5,R=10.5;
 const pent=(px,py,r,a0)=>poly([0,1,2,3,4].map(i=>[px+r*Math.cos(a0+i*2*Math.PI/5),py+r*Math.sin(a0+i*2*Math.PI/5)]));
 const up=-Math.PI/2,inside=circle(c,c,R-.9),parts=[[circle(c,c,R),'D'],[inside,'C'],[meet(cut(inside,circle(c-1.3,c-1.6,R-1.3)),(x,y)=>x+y>2*c+3?-1:1),'S']];
 parts.push([pent(c,c+.4,3.7,up),'D']);
 for(let i=0;i<5;i++){const a=up+i*2*Math.PI/5;
  parts.push([meet(seg(c+3*Math.cos(a),c+.4+3*Math.sin(a),c+8*Math.cos(a),c+.4+8*Math.sin(a),.55),inside),'D']);
  parts.push([meet(pent(c+10.4*Math.cos(a),c+.4+10.4*Math.sin(a),4.6,a+Math.PI),circle(c,c,R)),'D']);}
 const g=raster(21,21,'.',parts);
 return str(trim(g));}

// 4. A hedgehog chef in a toque stirring a red pot with a ladle.
function hedgehog(){
 const cx=14,cy=21,spikes=[];for(let i=0;i<8;i++){const a=Math.PI*(0.66+i*0.135),r0=10.5;
  spikes.push(poly([[cx+r0*Math.cos(a-.19),cy+r0*1.22*Math.sin(a-.19)],[cx+(r0+4.4)*Math.cos(a),cy+(r0+4.4)*1.22*Math.sin(a)],[cx+r0*Math.cos(a+.19),cy+r0*1.22*Math.sin(a+.19)]]));}
 const spines=union(ellipse(cx,cy,10.5,12.8),...spikes);
 const g=raster(44,46,'.',[
  [spines,'E'],
  // face and belly
  [union(ellipse(21,17.5,6,5.6),poly([[21,13.2],[29.5,18],[21,22.6]])),'K'],
  [ellipse(19,30,6.2,8),'K'],
  // feet
  [union(ellipse(14.5,38.6,3.2,1.6),ellipse(21.5,38.6,3.2,1.6)),'E'],
  // toque
  [union(circle(17,5,3.6),circle(12.8,6.8,3.1),circle(21.2,6.8,3.1),box(17,8.6,5.6,2.2)),'C'],
  [box(17,11,6.4,1.2),'S'],
  // arm reaching to the ladle
  [seg(19.5,26.5,25,23.5,1.6),'K'],
  // ladle in the pot
  [seg(25,23.5,31,28.5,.85),'D'],[circle(25,23.4,1.7),'K'],
  // pot with rim, soup and handles
  [box(32,34.5,7.6,5.5,1.5),'R'],[box(32,29,8.6,1.1,.5),'D'],[ellipse(32,28,7,1.3),'O'],
  [box(23.4,32,1.2,1.4),'D'],[box(40.6,32,1.2,1.4),'D'],[box(32,39.6,5.6,.9),'D'],
  // steam
  [union(circle(35,24.4,1.4),circle(36.4,21.6,1.4),circle(35.2,19,1.3)),'S']]);
 put(g,'D',[[22,15],[22,16],[29,17],[29,18],[28,17],[28,18]]);put(g,'P',[[23,19],[24,19],[23,20],[24,20]]);put(g,'D',[[20,21],[21,21]]);
 return str(trim(g));}

// 5. A classic microscope: dark base, blue arm and tube, eyepiece, lens turret, a slide on the stage.
function microscope(){
 const t0=[10,6],t1=[15.5,20];
 const nrm=Math.hypot(t1[0]-t0[0],t1[1]-t0[1]),side=(x,y)=>((x-t0[0])*(t1[1]-t0[1])-(y-t0[1])*(t1[0]-t0[0]))/nrm;
 const tube=seg(...t0,...t1,3.3);
 const arcO=ellipse(18,21,11,14),arcI=ellipse(16,21.5,5.8,9.5);
 const arm=meet(cut(arcO,arcI),(x,y)=>x>16&&y<34?-1:1);
 const g=raster(36,46,'.',[
  // base and pillar
  [box(18,41,13,2.4,1.2),'D'],[box(27.4,36,3.4,3.6),'N'],
  // arm: a thick curve from the pillar up to the tube, a light outer edge
  [arm,'N'],[meet(arm,cut(arcO,ellipse(18,21,9.4,12.4))),'B'],
  // tube with a highlight stripe
  [tube,'N'],[meet(tube,(x,y)=>side(x,y)>1.2?-1:1),'B'],
  // eyepiece
  [seg(8.6,3.6,9.4,5.2,3.6),'D'],[box(8.4,1.4,3.6,1.2,.5),'C'],
  // turret and objectives
  [ellipse(16.2,21.4,5.4,2),'D'],[box(13.6,24,1.1,1.6),'C'],[box(18.4,23.8,1.1,1.4),'C'],
  // stage: top surface, slide with a sample, front edge
  [poly([[3,28],[27,28],[29,31],[5,31]]),'B'],[box(15.5,29.5,6,1),'S'],[box(15.5,29.5,1.1,1),'P'],
  [box(16.5,32.2,12.6,1),'D'],
  // focus knob and mirror
  [circle(28.6,25,2.6),'D'],[circle(28.6,25,1.2),'C'],
  [ellipse(15,36,3.6,1.6),'C'],[seg(15,33.2,15,34.4,.9),'D']]);
 return str(trim(g));}

export default {theme:'batch-0061',pictures:[
 {title:'Барабан',forLevel:81,family:'Музыка',subject:'маршевый барабан с зигзагом шнура и двумя скрещёнными палочками',source:'оригинальный',rows:drum()},
 {title:'Мольберт и палитра',forLevel:82,family:'Рисование и ремёсла',subject:'мольберт с картиной-цветком, палитра с красками и кисть',source:'оригинальный',rows:easel()},
 {title:'Футбольный мяч',forLevel:83,family:'Спорт и активные игры',subject:'классический чёрно-белый футбольный мяч с пятиугольниками',source:'оригинальный',rows:football()},
 {title:'Ёжик-повар',forLevel:84,family:'Профессии и увлечения',direction:'Обмен ролями',subject:'ёжик в поварском колпаке мешает суп в кастрюле половником',source:'оригинальный',rows:hedgehog()},
 {title:'Микроскоп',forLevel:85,family:'Лаборатории и изобретения',subject:'классический микроскоп с окуляром, револьвером объективов и предметным стеклом',source:'оригинальный',rows:microscope()},
]};

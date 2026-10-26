// batch-0011 part 02: pineapple, ramen, cloud asleep in an armchair, skateboard, robot watering a flower (levels 16–20)
import {circle,ellipse,box,poly,union,cut,meet} from '../../../../../tools/gameplay-pilot-v9/draw.mjs';
import {raster,fit,str,put,ring4,scale,seg} from '../helpers.mjs';
const draw=(w,h,k,layers)=>raster(Math.round(w*k),Math.round(h*k),'.',layers.map(([s,c])=>[scale(k,s),typeof c==='function'?(x,y,u)=>c(x/k,y/k,u):c]));
const rot=(shape,a,cx,cy)=>{const c=Math.cos(a),s=Math.sin(a);return (x,y)=>{const dx=x-cx,dy=y-cy;return shape(cx+dx*c+dy*s,cy-dx*s+dy*c);};};
const hand=(g,rows,ox,oy)=>{rows.forEach((r,j)=>[...r].forEach((c,i)=>{if(c!==' ')g[oy+j][ox+i]=c;}));return g;};

function pineapple(){const W=26,H=42,cx=13,cy=27.5;
 const body=ellipse(cx,cy,11.2,13.6);
 const L1=poly([[13,-.2],[16.2,7],[15.6,16],[10.4,16],[9.8,7]]);             // middle
 const L2=poly([[4.6,2.2],[10,7],[13.4,16],[9,16.4],[5.6,9]]);               // left
 const L3=poly([[21.4,2.2],[16,7],[12.6,16],[17,16.4],[20.4,9]]);            // right
 const L4=poly([[.2,8.6],[6.5,10.4],[11,16.2],[6.4,16.6],[2.6,12.6]]);       // outer left
 const L5=poly([[25.8,8.6],[19.5,10.4],[15,16.2],[19.6,16.6],[23.4,12.6]]);  // outer right
 const skin=(x,y)=>{const d1=((x+y)%6+6)%6,d2=((x-y)%6+6)%6;if(d1===0||d2===0)return 'A';const shade=ellipse(cx-3.2,cy-3,10.5,13)(x+.5,y+.5)>0;return shade?'O':'Y';};
 const g=raster(W,H,'.',[[L4,'G'],[L5,'G'],[L2,'M'],[L3,'M'],[L1,'G'],[meet(L1,(x,y)=>13.6-x),'G'],[body,skin]]);
 return str(g);}

function ramen(){const W=42,H=31,cx=20,rimY=12;
 const outer=ellipse(cx,rimY,19.5,8);
 const bowlLow=meet(ellipse(cx,rimY,19.5,15),(x,y)=>rimY-y);
 const foot=box(cx,27.6,7.5,1.6,.6);
 const soup=ellipse(cx,rimY,17.4,6.2),bed=ellipse(cx,rimY+.3,15.4,5);
 const chop1=seg(41.2,.6,16,12.4,.75),chop2=seg(41.2,3.6,14.4,13.6,.75);
 const egg=ellipse(27.4,11.6,4.7,3.7),yolk=ellipse(27.7,11.8,2.5,1.9);
 const naruto=ellipse(12.6,11.4,4.4,3.4);
 const swirl=(x,y)=>{const dx=x-12.6,dy=(y-11.4)*1.3,r=Math.hypot(dx,dy),a=Math.atan2(dy,dx);const t=((r-a*0.5)%2.8+2.8)%2.8;return (r<3.6&&r>.4&&t<1.2)||r<.9?-1:1;};
 const noodle=(x,y)=>{const v=((y+Math.round(Math.sin(x*0.85)*0.9))%3+3)%3;return v===0?'A':'Y';};
 const g=raster(W,H,'.',[[foot,'N'],[bowlLow,'B'],[meet(bowlLow,(x,y)=>rimY+9.6-y),'N'],[outer,'N'],
  [soup,'A'],[bed,noodle],[naruto,'C'],[swirl,'P'],[egg,'C'],[yolk,'O'],[chop1,'E'],[chop2,'E']]);
 return str(g);}

function cloudChair(){const W=35,H=33;
 const back=box(17.5,11.4,12.3,10.4,5.4);
 const armL=box(5,20.2,4.4,6.8,3),armR=box(30,20.2,4.4,6.8,3);
 const seat=box(17.5,24.6,11.4,3.4,1.6);
 const front=box(17.5,27.8,15,2.2,1.2);
 const legs=union(box(5,31.2,1.3,1.6,.3),box(30,31.2,1.3,1.6,.3));
 const g=raster(W,H,'.',[[legs,'E'],[back,'O'],[front,'A'],[seat,'O'],[armL,'A'],[armR,'A']]);
 // the cloud: a scalloped ring of puffs over a round body, a sky-blue belly shade on the lower right
 const puffs=[circle(7.4,14.6,3.4),circle(10.4,9.8,3.6),circle(17.5,7,4.6),circle(24.6,9.8,3.6),circle(27.6,14.6,3.4),circle(5.6,19.4,3),circle(29.4,19.4,3)];
 const cloud=union(...puffs,box(17.5,16.6,11.6,6,4));
 const cg=raster(W,H,'.',[[cloud,'C'],[meet(cloud,(x,y)=>Math.min(21.2-y,(x-29.2)+(y-14)*.3>0?-1:1)),'S']]);
 cg.forEach((r,y)=>r.forEach((c,x)=>{if(c!=='.')g[y][x]=c;}));
 put(g,'D',[[11,14],[12,15],[13,15],[14,14],[20,14],[21,15],[22,15],[23,14],[16,17],[17,17],[18,17]]);
 put(g,'K',[[9,16],[10,16],[24,16],[25,16]]);
 return g;}

function skateboard(){const W=44,H=37,a=-0.6,cx=21.5,cy=18,hh=6.3,L=21.4;
 // seen from above, turned on the board: the deck in its own frame (u along, v across), wheels peeking out at its sides
 const place=(sh)=>(x,y)=>{const c=Math.cos(a),s=Math.sin(a),dx=x-cx,dy=y-cy;return sh(dx*c+dy*s,-dx*s+dy*c);};
 const deck=place(box(0,0,L,hh,hh)),rim=(x,y)=>Math.max(deck(x,y),-1-deck(x,y));
 const wheels=place(union(...[-14,14].flatMap(u=>[-1,1].map(sv=>box(u,sv*(hh+1.2),2.6,2.2,1)))));
 const side=(x,y)=>deck(x-.9,y-1.6);
 const bolt=place(poly([[-14.5,2.5],[.4,-4.5],[-.7,-.8],[15,-2.5],[-.4,4.5],[.7,.8]]));
 const g=raster(W,H,'.',[[wheels,'O'],[side,'D'],[deck,'B'],[rim,'N'],[bolt,'Y']]);
 return str(g);}

function robotFlower(){const W=44,H=37;
 const head=circle(11,10,7.2),ear=union(box(3.4,10,1.2,2.2,.5),box(18.6,10,1.2,2.2,.5));
 const antenna=box(11,2.4,.6,1.6),bulb=circle(11,1.6,1.4);
 const visor=box(11,10.6,5,2.6,2.2);
 const neck=box(11,17.8,2.4,1);
 const body=box(11,25.5,8,7.2,4.5);
 const panel=box(11,25.5,3.4,2.6,1);
 const legs=union(box(7,34.2,2,2.2,.6),box(15,34.2,2,2.2,.6));
 const armL=seg(3.6,22,2.4,29,1.3),armR=seg(17,22,23,17,1.3);
 const can=box(26,16,4.6,3.8,1.4),handle=cut(circle(26,11.8,3),circle(26,11.8,1.7)),
  spout=seg(29.4,16.4,33.6,11.4,1.05),rose=box(34.4,10.8,1.6,1.6,.5);
 const drops=union(box(35.5,14.4,.6,1),box(37.5,16.6,.6,1),box(34.5,17.6,.6,1));
 const pot=poly([[32.6,29.2],[42.4,29.2],[41.2,36.4],[33.8,36.4]]),potRim=box(37.5,29.4,5.4,1.3,.4);
 const stem=box(37.5,25.6,.65,3.6);
 const leafL=ellipse(35,26.4,2.3,1.2),leafR=ellipse(40,25.6,2.3,1.2);
 const P=[0,1,2,3,4].map(i=>{const t=-Math.PI/2+i*2*Math.PI/5;return circle(37.5+Math.cos(t)*2.9,21.6+Math.sin(t)*2.9,2.1);});
 const petals=union(...P),centre=circle(37.5,21.6,1.6);
 const g=raster(W,H,'.',[[legs,'B'],[antenna,'B'],[bulb,'O'],[ear,'B'],[head,'S'],[meet(head,(x,y)=>x-14+(y-10)*.4>0?-1:1),'B'],
  [visor,'D'],[neck,'B'],[armL,'B'],[body,'S'],[meet(body,(x,y)=>15.5-x),'B'],[panel,'B'],
  [handle,'O'],[spout,'O'],[rose,'O'],[can,'O'],[armR,'B'],
  [drops,'B'],[stem,'G'],[leafL,'G'],[leafR,'G'],[petals,'P'],[centre,'Y'],[pot,'A'],[potRim,'A']]);
 put(g,'S',[[8,10],[9,10],[13,10],[14,10]]);
 return str(g);}

export default {theme:'batch-0011',pictures:[
 {title:'Ананас',forLevel:16,family:'Фрукты и ягоды',subject:'ананас с короной листьев',source:'оригинальный',rows:pineapple()},
 {title:'Рамен',forLevel:17,family:'Блюда и напитки',subject:'миска рамена с яйцом, нарутомаки и палочками',source:'оригинальный',rows:ramen()},
 {title:'Облако спит в кресле',forLevel:18,family:'Мебель и уютные уголки',direction:'Уютный сюрреализм',subject:'спящее облако в кресле',source:'оригинальный',rows:str(cloudChair())},
 {title:'Скейтборд',forLevel:19,family:'Спорт и активные игры',subject:'скейтборд с молнией на деке',source:'оригинальный',rows:skateboard()},
 {title:'Робот поливает цветок',forLevel:20,family:'Роботы и механизмы',direction:'Природа и технология',subject:'робот с лейкой поливает цветок в горшке',source:'оригинальный',rows:robotFlower()},
]};

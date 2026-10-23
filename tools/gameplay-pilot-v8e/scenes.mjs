// v8E scenes, drawn at their role's board size (draw.mjs). First proofs: L7, L10, L15, L22.
import {circle,ellipse,box,poly,union,cut,paint,sky,rows} from './draw.mjs';
const grass=(W,H)=>(x,y)=>y>=H-2?'M':'G';
const flower=(cx,cy,petal='P',light='K',shade='R',middle='Y')=>[
 {shape:union(circle(cx-1.4,cy,1.3),circle(cx+1.4,cy,1.3),circle(cx,cy-1.4,1.3),circle(cx,cy+1.4,1.3)),color:petal,light,shade,sphere:[cx,cy,2.6],group:'f'+cx,outline:false},
 {shape:circle(cx,cy,1),color:middle,outline:false}];
const cloud=(cx,cy,s=1)=>({shape:union(ellipse(cx,cy,4.4*s,1.9*s),circle(cx-2*s,cy-1.1*s,2*s),circle(cx+1.4*s,cy-1.6*s,2.5*s)),color:'C',shade:'S',sphere:[cx,cy-1.2*s,4.6*s],outline:false,band:1.2});
export const SCENES={
 7:{title:'Клубника на лужайке',w:28,h:30,draw(W,H){const ground=24,bg=sky(W,H,{horizon:.5}),g=grass(W,H);
  const body=union(ellipse(14,13.5,9.6,7.2),poly([[4.6,14],[23.4,14],[17.5,24.5],[10.5,24.5]]),circle(14,23.6,3.2));
  const seeds=[[10,11],[14,10],[18,11],[8,15],[12,14.5],[16,14.5],[20,15],[10,18.5],[14,18],[18,18.5],[12,21.5],[16,21.5]].map(([x,y])=>({shape:ellipse(x+.5,y+.5,.55,.8),color:'Y',outline:false}));
  return paint(W,H,{background:(x,y)=>y>=ground?g(x,y):bg(x,y),parts:[cloud(5,4),cloud(22,6,.8),
   ...flower(3.5,26),...flower(24.5,27,'Y','C','O','O'),
   {shape:body,color:'R',light:'P',shade:'V',sphere:[12.5,12.5,12],lightAt:.72,shadeAt:-.1},...seeds,
   {shape:union(poly([[14,7.5],[8.5,5.2],[11.5,8.6]]),poly([[14,7.5],[19.5,5.2],[16.5,8.6]]),poly([[14,7.5],[10.5,3.6],[14.5,6]]),poly([[14,7.5],[17.5,3.6],[13.5,6]]),ellipse(14,7.6,4,1.4)),color:'G',light:'L',shade:'M',flat:true,group:'leaf'},
   {shape:box(14.5,3.2,.6,1.8),color:'A',group:'leaf'}]});}},
 10:{title:'Золотая рыбка',w:34,h:36,draw(W,H){
  const water=(x,y)=>y>=H-3?'C':y>=H-5?'N':y<H*.18?'S':'B';
  const weed=(x0,h)=>({shape:union(...Array.from({length:h},(_,i)=>circle(x0+Math.sin(i*.7)*1.3,H-3-i,1.5))),color:'M',light:'G',flat:true,group:'w'+x0});
  const body=ellipse(15,17,9.5,6.8),tail=poly([[23,17],[31.5,10],[29.5,17],[31.5,24]]);
  return paint(W,H,{background:water,parts:[weed(3,11),weed(30,8),weed(26,6),
   ...[[6,6,1.4],[8,3,1],[25,4,1.2],[28,8,.8],[4,11,.9]].map(([x,y,r])=>({shape:circle(x,y,r),color:'S',outline:true})),
   {shape:tail,color:'O',light:'Y',shade:'A',flat:true,group:'fish'},
   {shape:poly([[11,11],[18,8],[20,11.5]]),color:'O',light:'Y',shade:'A',flat:true,group:'fish'},
   {shape:poly([[13,22.5],[17,26.5],[19,22]]),color:'O',light:'Y',shade:'A',flat:true,group:'fish'},
   {shape:body,color:'O',light:'Y',shade:'A',sphere:[13,15,11],group:'fish'},
   {shape:ellipse(16.5,17,1,5.5),color:'Y',outline:false},{shape:ellipse(20,17,.9,4.5),color:'Y',outline:false},
   {shape:circle(10,15.2,2),color:'C',group:'eye'},{shape:circle(9.6,15.4,1),color:'D',outline:false},
   {shape:ellipse(6.4,18.6,1.1,.6),color:'R',outline:false}]});}},
 15:{title:'Пчёлка над цветком',w:32,h:34,draw(W,H){const ground=28,bg=sky(W,H,{horizon:.5}),g=grass(W,H);
  return paint(W,H,{background:(x,y)=>y>=ground?g(x,y):bg(x,y),parts:[cloud(26,4,.9),
   {shape:box(16,29,.8,5),color:'M',group:'stem'},{shape:ellipse(12.5,29.5,3,1.3),color:'G',light:'L',shade:'M',flat:true,group:'stem'},{shape:ellipse(19.5,30.5,3,1.3),color:'G',light:'L',shade:'M',flat:true,group:'stem'},
   {shape:union(...[0,1,2,3,4,5].map(i=>circle(16+3.4*Math.cos(i*Math.PI/3),24+3.4*Math.sin(i*Math.PI/3),2.3))),color:'P',light:'K',shade:'R',sphere:[16,24,5.6],group:'fl'},
   {shape:circle(16,24,2.2),color:'O',light:'Y',sphere:[16,24,2.2],group:'fl2'},
   {shape:ellipse(10.5,6.8,4.2,3.4),color:'S',light:'C',flat:true,group:'wing'},{shape:ellipse(17,5.8,4.6,3.6),color:'S',light:'C',flat:true,group:'wing'},
   {shape:ellipse(14,13,8.4,5.6),color:'Y',light:'C',shade:'O',sphere:[12,11.5,9],group:'bee'},
   {shape:(x,y)=>Math.max(ellipse(14,13,7.6,4.8)(x,y),union(box(11,13,.9,8),box(15.2,13,.9,8),box(19.2,13,.9,8))(x,y)),color:'D',outline:false},
   {shape:poly([[22,12],[25,13],[22,14.2]]),color:'D',group:'bee'},
   {shape:circle(6.6,12.6,1.1),color:'D',outline:false},{shape:ellipse(5.4,14.8,.8,.5),color:'R',outline:false},
   {shape:poly([[7,7.5],[5.4,3.4],[6.2,3.2],[8,7.2]]),color:'D',group:'ant'}]});}},
 22:{title:'Домик у ручья',w:34,h:36,draw(W,H){const bg=sky(W,H,{horizon:.45});
  const hill=(x,y)=>y>=22+Math.round(2*Math.sin(x/5)),stream=(x,y)=>y>=28&&y<=30;
  return paint(W,H,{background:(x,y)=>stream(x,y)?(y===28?'S':'B'):hill(x,y)?(y>=H-2?'M':'G'):bg(x,y),parts:[
   {shape:circle(28,5.5,3.2),color:'Y',light:'C',shade:'O',sphere:[28,5.5,3.2]},cloud(7,5),
   {shape:box(27,13.5,1.4,3),color:'A',shade:'E',flat:true,group:'house'},
   {shape:box(16,20,8.5,5.5),color:'K',shade:'P',flat:true,group:'house'},
   {shape:poly([[5,15],[16,6],[27,15]]),color:'R',light:'P',shade:'V',flat:true,group:'roof'},
   {shape:box(16,21.5,2.1,3.6,.6),color:'A',light:'O',shade:'E',flat:true,group:'door'},{shape:circle(17.3,21.8,.5),color:'Y',outline:false},
   {shape:box(10.5,19.5,2.2,2),color:'S',light:'C',flat:true,group:'w1'},{shape:box(21.5,19.5,2.2,2),color:'S',light:'C',flat:true,group:'w2'},
   {shape:box(10.5,19.5,.35,2),color:'D',outline:false},{shape:box(21.5,19.5,.35,2),color:'D',outline:false},
   ...flower(4,25),...flower(29,25,'Y','C','O','O'),...flower(8,33,'V','P','N','Y'),...flower(25,33)]});}},
};
export function sceneLevel(id){const s=SCENES[id];if(!s)return null;const col=s.draw(s.w,s.h);return {title:s.title,w:s.w,h:s.h,rowsBottomFirst:rows(col).reverse()};}

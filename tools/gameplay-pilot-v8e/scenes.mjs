// v8E scenes (draw.mjs), one per pilot level, at the role's board. The owner's direction of 2026-10-23: the subject
// fills most of the board (70-90%), objects carry 3-4 layers of detail, and the background is work with a pattern of
// its own, after Colony Flow's pictures. Colours arrive one level at a time (INTRO), so a picture only uses colours
// the player has met. Bridge levels keep v8's stream: a wall row across the board, a divider above it, and Sprig's
// bridge sites on the wall with their anchors on the near bank.
import {circle,ellipse,box,poly,union,cut,meet,paint,rows} from './draw.mjs';
export const SIZE={relief:[24,26],normal:[28,30],task:[30,32],hard:[32,34],superHard:[34,36]};
export const ROLE_OF={5:'normal',6:'relief',7:'normal',8:'task',9:'normal',10:'superHard',11:'relief',12:'normal',13:'normal',14:'task',15:'hard',16:'relief',17:'normal',18:'task',19:'normal',20:'task',21:'normal',22:'superHard',23:'relief',24:'task',25:'hard',26:'normal',27:'task',28:'relief',29:'superHard',30:'normal'};
const TUTORIAL={1:[16,18],2:[18,20],3:[20,22],4:[24,26]};
export const sizeOf=id=>TUTORIAL[id]||SIZE[ROLE_OF[id]];
// The board each level is shown on (the owner, 2026-10-23: not every board grows; small relief boards and tall or
// wide ones come and go, as in Colony Flow's first 30, where sizes run from 18x21 to 38x28).
export const BOARD={1:[16,18],2:[18,20],3:[20,22],4:[24,26],5:[26,28],6:[20,22],7:[24,28],8:[30,32],9:[28,26],10:[34,36],
 11:[20,20],12:[28,30],13:[26,30],14:[30,30],15:[32,34],16:[18,22],17:[28,28],18:[30,32],19:[24,28],20:[30,32],
 21:[28,30],22:[34,36],23:[20,24],24:[30,32],25:[32,34],26:[26,30],27:[32,28],28:[22,22],29:[34,36],30:[26,28]};
// Object-only pictures (no background, the empty cells are open ground), as in about a third to a half of Colony Flow's first
// 30. Not L1 (its lesson needs three colours) nor the bridge levels (their stream is part of the picture).
export const BARE=new Set([3,5,7,16,23,28,30]);
// The level that introduces each colour: v8's nine by L11, the seven new tones one per level from L6.
export const INTRO={G:1,B:1,A:1,P:2,C:3,D:4,Y:4,O:5,S:6,R:7,M:8,K:9,N:10,V:11,L:12,E:13};
export const allowedAt=id=>Object.keys(INTRO).filter(c=>INTRO[c]<=id);
// Bridge levels: how many of Sprig's bridge sites each one has (v8's).
const BRIDGES={20:1,21:2,22:2,24:2,29:2};

// ---- shapes ----
const seg=(x1,y1,x2,y2,r)=>(x,y)=>{const ex=x2-x1,ey=y2-y1,t=Math.max(0,Math.min(1,((x-x1)*ex+(y-y1)*ey)/(ex*ex+ey*ey)));return Math.hypot(x-x1-ex*t,y-y1-ey*t)-r;};
const path=(pts,r)=>union(...pts.slice(1).map((p,i)=>seg(pts[i][0],pts[i][1],p[0],p[1],r)));
const leaf=(cx,cy,len,wid,ang)=>{const r=(len*len/4+wid*wid)/(2*wid),o=r-wid,ux=Math.cos(ang),uy=Math.sin(ang);return meet(circle(cx-uy*o,cy+ux*o,r),circle(cx+uy*o,cy-ux*o,r));};
const ring=(cx,cy,r,n,pr,a0=-Math.PI/2)=>union(...Array.from({length:n},(_,i)=>circle(cx+r*Math.cos(a0+i*2*Math.PI/n),cy+r*Math.sin(a0+i*2*Math.PI/n),pr)));
// A shape that keeps only the cells where pred(x, y) holds (stripes, checks, dots inside an object).
const mask=(shape,pred)=>(x,y)=>pred(Math.floor(x),Math.floor(y))?shape(x,y):1;
const dots=(pts,r,color)=>pts.map(([x,y])=>({shape:circle(x,y,r),color,outline:false}));
// ---- reusable parts ----
const cloud=(cx,cy,s=1,color='C',shade='S')=>({deco:true,shape:union(ellipse(cx,cy,4.4*s,1.9*s),circle(cx-2*s,cy-1.1*s,2*s),circle(cx+1.4*s,cy-1.6*s,2.5*s)),color,shade,sphere:[cx,cy-1.2*s,4.6*s],outline:false,band:1.2});
const sun=(cx,cy,r)=>[{deco:true,shape:ring(cx,cy,r+1.5,8,.9),color:'Y',outline:false},{deco:true,shape:circle(cx,cy,r),color:'Y',light:'C',shade:'O',sphere:[cx,cy,r]}];
const daisy=(cx,cy,petal='C',middle='Y',s=1)=>[{deco:true,shape:ring(cx,cy,1.3*s,5,1*s),color:petal,outline:false},{deco:true,shape:circle(cx,cy,.8*s),color:middle,outline:false}];
const tulip=(cx,cy,petal='R',s=1)=>[{deco:true,shape:seg(cx,cy,cx,cy+3.5*s,.55),color:'G',outline:false},{deco:true,shape:union(ellipse(cx,cy,1.5*s,1.4*s),poly([[cx-1.5*s,cy-.2],[cx-1.1*s,cy-2*s],[cx,cy-.9*s],[cx+1.1*s,cy-2*s],[cx+1.5*s,cy-.2]])),color:petal,outline:false}];
// ---- backgrounds (a function of the cell; null lets the next layer decide) ----
const layer=(...fs)=>(x,y)=>{for(const f of fs){const c=f(x,y);if(c)return c;}return 'B';};
const skyBg=(horizon,ok)=>(x,y)=>ok('S')&&(y>horizon||(y===horizon&&x%2===0))?'S':'B';
const grassBg=(W,H,top,ok,wave=0)=>(x,y)=>{const t=top+Math.round(wave*Math.sin(x/4));if(y<t)return null;
 if(y>=H-1&&ok('M'))return 'M';if(ok('L')&&y===t&&x%5===2)return 'L';if(ok('M')&&(x*3+y*7)%17===0&&y>t+1)return 'M';return 'G';};
const checker=(a,b,size=2,top=0)=>(x,y)=>y<top?null:((Math.floor(x/size)+Math.floor((y-top)/size))%2?a:b);
const planks=(a,b,top,h=3)=>(x,y)=>y<top?null:((y-top)%h===h-1||((x+(Math.floor((y-top)/h)*7))%11===0)?b:a);
const stripesBg=(a,b,every=4)=>(x)=>x%every===0?b:a;
const dotsBg=(a,b,step=4)=>(x,y)=>(x%step===1&&y%step===(Math.floor(x/step)%2?1:3))?b:a;
const nightBg=(stars)=>(x,y)=>stars.some(([sx,sy])=>sx===x&&sy===y)?'Y':'N';

export const SCENES={
 1:{title:'Большой лист',outline:'A',draw(W,H,ok){
  return {background:layer(grassBg(W,H,H-3,ok)),parts:[
   {shape:leaf(8,8.5,15,4.6,-Math.PI/4),color:'G',group:'leaf'},
   {shape:union(seg(3.2,13.8,12.6,4.2,.45),seg(6,11,4.6,8.4,.38),seg(8.6,8.4,7.8,5.6,.38),seg(7,11.6,9.8,12.4,.38),seg(9.8,8.8,12.2,9.4,.38)),color:'A',outline:false},
   {shape:seg(1.5,15.8,3.6,13.4,.7),color:'A',group:'leaf'}]};}},
 2:{title:'Капелька в траве',outline:'A',draw(W,H,ok){
  return {background:layer(()=> 'G'),parts:[...daisy(2.5,3,'P','B',.8),...daisy(15.5,4,'P','B',.8),...daisy(3,17.5,'B','P',.8),...daisy(15.5,17,'B','P',.8),
   {shape:union(circle(9,12.6,6.4),poly([[3,11],[15,11],[9,1.4]])),color:'B'},
   {shape:ellipse(6.6,12.2,.9,1.2),color:'A',outline:false},{shape:ellipse(11.4,12.2,.9,1.2),color:'A',outline:false},
   {shape:ellipse(5,14.6,1.1,.7),color:'P',outline:false},{shape:ellipse(13,14.6,1.1,.7),color:'P',outline:false},
   {shape:seg(7.8,15.8,10.2,15.8,.55),color:'A',outline:false}]};}},
 3:{title:'Цветок Bloom',outline:'A',draw(W,H,ok){
  return {background:layer(grassBg(W,H,H-4,ok)),parts:[
   {shape:box(10,17,.9,5),color:'G',group:'stem'},{shape:leaf(6.5,17.5,6,2.2,-.5),color:'G',group:'stem'},{shape:leaf(13.5,18,6,2.2,.5),color:'G',group:'stem'},
   {shape:ring(10,9,5.2,6,3.5),color:'P',group:'fl'},
   {shape:circle(10,9,3),color:'C',group:'mid'},
   ...dots([[9,8],[11,8.4],[10,10],[8.6,9.8],[11.4,10.2]],.5,'A'),
   ...daisy(3,19.5,'C','P',.9),...daisy(17,20,'P','C',.9)]};}},
 4:{title:'Бабочка на лугу',draw(W,H,ok){
  const wingL=union(ellipse(7,9,6,5.5),ellipse(7.6,17.4,4.6,4.4)),wingR=union(ellipse(17,9,6,5.5),ellipse(16.4,17.4,4.6,4.4));
  return {background:layer(grassBg(W,H,H-4,ok)),parts:[
   {shape:wingL,color:'P',group:'wl'},{shape:wingR,color:'P',group:'wr'},
   {shape:circle(6.4,8.4,2.7),color:'Y',outline:false},{shape:circle(17.6,8.4,2.7),color:'Y',outline:false},
   {shape:circle(7.6,17.4,1.9),color:'C',outline:false},{shape:circle(16.4,17.4,1.9),color:'C',outline:false},
   {shape:circle(6.2,8.2,1.1),color:'D',outline:false},{shape:circle(17.8,8.2,1.1),color:'D',outline:false},
   ...dots([[3,6],[3.4,12],[21,6],[20.6,12]],.7,'C'),
   {shape:union(ellipse(12,13,1.5,7.5),circle(12,5.2,1.9)),color:'D',group:'body'},
   {shape:union(seg(11.2,4,8.4,1,.55),seg(12.8,4,15.6,1,.55)),color:'D',outline:false},
   ...daisy(3,23.5,'Y','C'),...daisy(20.5,24,'C','Y'),...daisy(12,24.6,'P','Y',.8)]};}},
 5:{title:'Жёлудь Twig',draw(W,H,ok){
  const cap=union(ellipse(14,11,10.6,5.2),box(14,13.4,10.2,2.2,1.6));
  return {background:layer(grassBg(W,H,H-4,ok,.6)),parts:[cloud(5,4,.8),cloud(23,6,.7),
   {shape:seg(15,1.6,16.4,5.4,.8),color:'A',group:'stem'},
   {shape:leaf(20.5,4.2,8,2.6,-.3),color:'G',group:'leafa'},
   {shape:union(ellipse(14,19,9,8.6),poly([[6,18],[22,18],[14,28.6]])),color:'O',light:'Y',shade:'A',sphere:[12,16,11],lightAt:.72,group:'nut'},
   {shape:cap,color:'A',light:'O',flat:true,group:'cap'},
   {shape:mask(union(ellipse(14,11,9.2,4),box(14,13.2,8.8,1.2,1)),(x,y)=>(x+y)%3===0||(x-y+99)%3===0),color:'D',outline:false}]};}},
 6:{title:'Грибы на поляне',draw(W,H,ok){
  return {background:layer(grassBg(W,H,H-6,ok,.8),skyBg(11,ok)),parts:[cloud(19,3,.8),
   {shape:box(8.8,17.6,2.6,4.8,1.2),color:'C',shade:'Y',flat:true,group:'s1'},
   {shape:union(ellipse(9,9.6,8.4,5.6),box(9,12.4,8,1.4,1.2)),color:'P',light:'C',sphere:[7,7,10],group:'c1'},
   ...dots([[5.6,8.4],[10,7],[13.2,10],[7.6,11.6]],1.2,'C'),
   {shape:box(18.5,20.4,1.6,2.4,.8),color:'C',shade:'Y',flat:true,group:'s2'},
   {shape:union(ellipse(18.5,16.8,4,2.8),box(18.5,18.2,3.8,.8,.6)),color:'O',light:'Y',shade:'A',sphere:[17.5,15.5,5],group:'c2'},
   ...dots([[17.4,16.2]],.9,'C'),
   ...daisy(3,22.6,'Y','O',.9),...daisy(22.4,23.2,'C','Y',.9),...daisy(13.6,24,'P','Y',.8)]};}},
 7:{title:'Клубника на скатерти',draw(W,H,ok){
  const body=union(ellipse(14,12.6,11.2,8.4),poly([[3,13],[25,13],[17.6,26.4],[10.4,26.4]]),circle(14,25.4,3.4));
  return {background:layer(checker('R','C',2,22),skyBg(12,ok)),parts:[
   {shape:body,color:'R',light:'P',sphere:[11,10,13],lightAt:.66,group:'berry'},
   ...dots([[9,9.5],[14,8.6],[19,9.5],[6.4,13.4],[11.4,12.8],[16.6,12.8],[21.6,13.4],[8.6,17.4],[14,16.8],[19.4,17.4],[11.2,21],[16.8,21],[14,24.4]].map(([x,y])=>[x+.5,y+.5]),.7,'Y'),
   {shape:union(poly([[14,6.4],[7,3.6],[10.6,7.8]]),poly([[14,6.4],[21,3.6],[17.4,7.8]]),poly([[14,6.4],[10.4,1.6],[15,5.2]]),poly([[14,6.4],[17.6,1.6],[13,5.2]]),ellipse(14,6.6,5,1.6)),color:'G',group:'leaf'},
   {shape:box(14.4,1.4,.7,1.4),color:'A',group:'leaf'}]};}},
 8:{title:'Чашка на завтраке',draw(W,H,ok){
  return {background:layer(checker('R','C',2,26),stripesBg('S','C',4)),parts:[
   ...[[10.5,1,6],[15.5,.5,6.6],[20.5,1.4,5]].map(([x,y,h])=>({shape:path([[x,y],[x+.9,y+h/3],[x-.6,y+2*h/3],[x+.4,y+h]],.75),color:'C',shade:'S',flat:true,outline:false})),
   {shape:ellipse(15,26.4,13.4,2.6),color:'C',shade:'Y',flat:true,group:'saucer'},
   {shape:union(box(13.5,18,9.5,7.4,2.6),ellipse(13.5,11.2,9.5,2)),color:'C',shade:'Y',flat:true,group:'cup'},
   {shape:cut(circle(24.6,17.4,4.4),circle(24.6,17.4,2.2)),color:'C',shade:'Y',flat:true,group:'handle'},
   {shape:ellipse(13.5,11.2,8.3,1.2),color:'A',outline:false},
   {shape:ring(13.5,18.4,2.8,5,2),color:'P',outline:false},{shape:circle(13.5,18.4,1.4),color:'Y',outline:false},
   ...[[6.4,15],[20.6,15],[6.4,21.6],[20.6,21.6]].flatMap(([x,y])=>[{shape:leaf(x,y,4,1.4,.8),color:'M',outline:false}]),
   {shape:mask(box(13.5,24.6,9.2,.6),(x)=>x%2===0),color:'R',outline:false}]};}},
 9:{title:'Письмо с печатью',draw(W,H,ok){
  return {background:layer(planks('A','D',0,4)),parts:[
   {shape:seg(3,25.5,9,19.5,1.1),color:'Y',light:'C',shade:'O',flat:true,group:'pencil'},{shape:seg(2.2,26.3,3,25.5,1),color:'K',group:'pencil'},
   {shape:leaf(22,4.5,7,2.4,-.6),color:'G',shade:'M',flat:true,group:'twig'},{shape:leaf(25,8,6,2,.9),color:'G',shade:'M',flat:true,group:'twig'},{shape:seg(19,8,26.5,2,.6),color:'A',group:'twig'},
   {shape:box(14,15,12,8.4,1),color:'C',shade:'Y',flat:true,group:'env'},
   {shape:union(seg(2.4,7.2,14,16.2,.55),seg(25.6,7.2,14,16.2,.55),seg(2.4,22.8,11,15.6,.5),seg(25.6,22.8,17,15.6,.5)),color:'A',outline:false},
   {shape:box(22.5,10.4,2.4,2.6,.3),color:'K',group:'stamp'},{shape:circle(22.5,10.4,1.4),color:'Y',outline:false},
   {shape:union(circle(12.8,16.4,2),circle(15.2,16.4,2),poly([[11,17.2],[17,17.2],[14,20.6]])),color:'R',light:'P',sphere:[13,15.5,4],group:'seal'}]};}},
 10:{title:'Золотая рыбка',draw(W,H,ok){
  const water=(x,y)=>y>=H-3?((x*5+y)%9===0?'P':'C'):y>=H-6?'N':y<5&&(x+y*2)%7===0?'S':'B';
  const weed=(x0,h,c='M')=>({shape:union(...Array.from({length:h},(_,i)=>circle(x0+Math.sin(i*.7)*1.3,H-3-i,1.5))),color:c,light:'G',flat:true,group:'w'+x0});
  const body=ellipse(15,16,10.5,7.4),tail=poly([[24,16],[33,8],[30.6,16],[33,24]]);
  return {background:water,parts:[weed(3,13),weed(31,10),weed(27,7,'G'),weed(7,6,'G'),
   {shape:union(ellipse(18,H-4.5,4,2.2),circle(18,H-6,1.6)),color:'K',light:'C',shade:'P',sphere:[17,H-6,3.6],group:'shell'},
   ...[[6,5,1.5],[8.4,2,1],[26,3,1.4],[29,7,.9],[4,10,1]].map(([x,y,r])=>({shape:circle(x,y,r),color:'S',outline:false})),
   {shape:tail,color:'O',light:'Y',shade:'A',flat:true,group:'fish'},
   {shape:poly([[10,9.6],[18,6.4],[21,10.6]]),color:'O',light:'Y',shade:'A',flat:true,group:'fish'},
   {shape:poly([[12,22],[16.6,27],[19,21.6]]),color:'O',light:'Y',shade:'A',flat:true,group:'fish'},
   {shape:body,color:'O',light:'Y',shade:'A',sphere:[13,13.5,12],lightAt:.7,group:'fish'},
   {shape:mask(body,(x,y)=>x>=15&&(x-15)%3===0),color:'Y',outline:false},
   {shape:circle(9.2,14.2,2.2),color:'C',group:'eye'},{shape:circle(8.8,14.6,1.1),color:'D',outline:false},
   {shape:ellipse(5.4,17.6,1.2,.7),color:'R',outline:false}]};}},
 11:{title:'Черепаха у пруда',draw(W,H,ok){
  const pond=(x,y)=>ellipse(15,23.4,8,2.2)(x+.5,y+.5)<0?((x+y)%4===0?'S':'B'):null;
  return {background:layer(pond,grassBg(W,H,H-6,ok,.6),skyBg(10,ok)),parts:[...sun(21,2.8,1.8),cloud(6,2.6,.7),
   {shape:union(ellipse(3.6,13.4,3,2.6),seg(4.4,14.8,6.6,16.4,1.6)),color:'G',shade:'M',flat:true,group:'head'},...dots([[2.6,12.8]],.7,'D'),
   ...[[7,19.6],[18.6,19.8]].map(([x,y])=>({shape:ellipse(x,y,2,1.6),color:'G',shade:'M',flat:true,group:'leg'+x})),
   {shape:union(ellipse(13,13.8,10.2,7.4),box(13,17.4,10,1.6,1)),color:'M',light:'G',sphere:[10.6,11,11.6],group:'shell'},
   {shape:mask(ellipse(13,13.4,8.4,6),(x,y)=>(x%4===1)||(y%3===0)),color:'V',outline:false},
   {shape:ellipse(13,18.8,10,.8),color:'Y',outline:false},
   {shape:ellipse(19,23.6,2,1),color:'G',outline:false},...daisy(18,23,'P','Y',.7),...daisy(2.5,23.4,'Y','O',.8)]};}},
 12:{title:'Сова ночью',draw(W,H,ok){
  const moon=cut(circle(23,5,3.4),circle(24.6,4,3));
  return {background:layer(nightBg([[2,3],[6,7],[10,2],[15,4],[27,10],[1,12],[26,15],[3,20],[19,2],[13,9]])),parts:[{shape:moon,color:'C',outline:false},
   {shape:path([[0,25],[10,24],[20,25.2],[28,24]],1.4),color:'A',light:'O',flat:true,group:'branch'},
   ...[[4,22.6,-.5],[24,22.4,.5],[18,27,1.2]].map(([x,y,a])=>({shape:leaf(x,y,5,1.8,a),color:'G',light:'L',shade:'M',flat:true,group:'lf'+x})),
   {shape:union(ellipse(14,15,9,9.4),poly([[6,8],[8,3.4],[11,7]]),poly([[22,8],[20,3.4],[17,7]])),color:'A',light:'O',sphere:[12,12,11],group:'owl'},
   {shape:ellipse(14,18.6,5.6,5.4),color:'C',group:'belly'},
   {shape:mask(ellipse(14,18.8,4.6,4.4),(x,y)=>(x+y*2)%4===0&&y%2===0),color:'Y',outline:false},
   ...[10,18].flatMap(x=>[{shape:circle(x,11.6,3.2),color:'C',group:'eye'+x},{shape:circle(x,11.8,2),color:'O',outline:false},{shape:circle(x,12,1),color:'D',outline:false}]),
   {shape:poly([[12.8,13.8],[15.2,13.8],[14,16.4]]),color:'O',outline:false},
   ...[11,14,17].map(x=>({shape:seg(x,23.4,x,25.2,.55),color:'O',outline:false}))]};}},
 13:{title:'Зонтик под дождём',draw(W,H,ok){
  const drops=[[2,8],[5,13],[3,18],[25,8],[22,12],[26,17],[8,3],[20,3],[1,24],[27,23]];
  return {background:layer(grassBg(W,H,H-3,ok),(x,y)=>ellipse(14,H-4.2,10,1.8)(x+.5,y+.5)<0?(y===H-5?'S':'N'):null,(x,y)=>drops.some(([a,b])=>a===x&&(y===b||y===b+1))?'S':'B'),parts:[cloud(4,2.6,.9,'V','N'),cloud(24,2.4,.8,'V','N'),
   {shape:union(cut(ellipse(14,13,12.6,9.6),box(14,19.6,14,6.6)),...[3.6,9,14,19,24.4].map(x=>circle(x,13,2.6)).slice(0,0)),color:'R',light:'P',flat:true,group:'umb'},
   {shape:mask(cut(ellipse(14,13,12.4,9.4),box(14,19.6,14,6.6)),(x)=>Math.floor((x-1)/5)%2===0),color:'K',outline:false},
   {shape:seg(14,3.2,14,1.6,.7),color:'E',group:'umb'},
   {shape:union(seg(14,13,14,23,.75),path([[14,23],[14.4,24.6],[16,25],[17.2,24]],.75)),color:'E',outline:false},
   ...tulip(4,22,'P'),...tulip(24,22.4,'Y'),...tulip(7.5,24,'V',.8)]};}},
 14:{title:'Чайник с узором',draw(W,H,ok){
  return {background:layer(checker('N','C',2,27),dotsBg('K','P',4)),parts:[
   ...[[8,1,5],[11,0,6]].map(([x,y,h])=>({shape:path([[x,y],[x+.8,y+h/3],[x-.6,y+2*h/3],[x+.4,y+h]],.75),color:'C',outline:false})),
   {shape:poly([[3.4,15],[1.2,9.6],[3,9.2],[6.4,13.4]]),color:'C',shade:'S',flat:true,group:'pot'},
   {shape:cut(circle(23.8,17,4.8),circle(23.8,17,2.6)),color:'C',shade:'S',flat:true,group:'handle'},
   {shape:union(ellipse(13.6,17.4,9.6,8.4),box(13.6,24.6,7.4,1.6,1)),color:'C',shade:'S',sphere:[11,14,11],group:'pot'},
   {shape:mask(ellipse(13.6,17.4,8.4,7.2),(x,y)=>((x+y)%5===0||(x-y+99)%5===0)),color:'B',outline:false},
   {shape:ellipse(13.6,17.4,3.4,3),color:'N',outline:false},{shape:circle(13.6,17.4,1.6),color:'C',outline:false},
   {shape:ellipse(13.6,9.6,5.4,1.6),color:'N',light:'B',flat:true,group:'lid'},{shape:circle(13.6,7.4,1.4),color:'N',group:'lid'},
   {shape:union(box(25.4,24.6,3.2,2.6,1),cut(circle(29,24.6,1.8),circle(29,24.6,.8))),color:'C',shade:'S',flat:true,group:'cup'},
   {shape:ellipse(15,27.4,13.4,1.2),color:'S',outline:false}]};}},
 15:{title:'Пчёлка над цветком',draw(W,H,ok){
  const honey=(x,y)=>x>=W-8&&y<15?(((x+(y%2)*1)%3===0||y%3===0)?'E':'Y'):null;
  return {background:layer(honey,grassBg(W,H,H-5,ok,.5),skyBg(16,ok)),parts:[cloud(5,3.6,.9),
   {shape:box(16,28,.9,5),color:'M',group:'stem'},{shape:leaf(12,29,6,2,-.3),color:'G',light:'L',shade:'M',flat:true,group:'stem'},{shape:leaf(20,30,6,2,.3),color:'G',light:'L',shade:'M',flat:true,group:'stem'},
   {shape:ring(16,23.6,4,8,2.5),color:'P',light:'K',shade:'R',sphere:[15,22.6,7.2],group:'fl'},
   {shape:circle(16,23.6,2.6),color:'O',light:'Y',shade:'A',sphere:[15.4,23,2.8],group:'fl2'},
   {shape:ellipse(9.6,7.4,4.6,3.6),color:'S',light:'C',flat:true,group:'wing'},{shape:ellipse(16.4,6.2,5,3.8),color:'S',light:'C',flat:true,group:'wing'},
   {shape:ellipse(14,13.6,9.6,6.2),color:'Y',light:'C',shade:'O',sphere:[12,11.6,10],group:'bee'},
   {shape:meet(ellipse(14,13.6,8.8,5.4),union(box(10.6,13,1,8),box(14.8,13,1,8),box(19,13,1,8))),color:'D',outline:false},
   {shape:poly([[23.4,12.6],[27,13.6],[23.4,14.8]]),color:'D',group:'bee'},
   {shape:circle(6.4,13,1.2),color:'D',outline:false},{shape:ellipse(5.2,15.4,.9,.6),color:'R',outline:false},
   {shape:union(seg(7,8.4,5,3.6,.55),seg(9,8,8.6,3.2,.55)),color:'D',outline:false},...dots([[5,3.6],[8.6,3.2]],.9,'D')]};}},
 16:{title:'Змей над холмом',draw(W,H,ok){
  const hill=(x,y)=>y>=19-Math.round(3*Math.exp(-(((x-15)/6)**2)))?(y>=H-1?'M':(y===19-Math.round(3*Math.exp(-(((x-15)/6)**2)))&&x%4===1?'L':'G')):null;
  return {background:layer(hill,skyBg(12,ok)),parts:[cloud(3,2.4,.6),cloud(21,16,.6),
   {shape:path([[12,18],[10,20],[12,22],[10,24]],.45),color:'D',outline:false},
   ...[[10.6,20],[11.2,23]].map(([x,y])=>({shape:union(poly([[x-1.4,y-1],[x,y],[x-1.4,y+1]]),poly([[x+1.4,y-1],[x,y],[x+1.4,y+1]])),color:'R',outline:false})),
   {shape:poly([[12,.6],[22,8.6],[12,18.4],[2,8.6]]),color:'Y',light:'C',shade:'O',flat:true,group:'kite'},
   {shape:meet(poly([[12,.6],[22,8.6],[12,18.4],[2,8.6]]),(x,y)=>((x<12)===(y<8.6))?-1:1),color:'R',light:'P',flat:true,group:'kite'},
   {shape:union(seg(12,.6,12,18.4,.45),seg(2,8.6,22,8.6,.45)),color:'E',outline:false},
   ...daisy(4,22,'C','Y',.8),...daisy(20,22.6,'P','Y',.8)]};}},
 17:{title:'Садовая лейка',draw(W,H,ok){
  return {background:layer(grassBg(W,H,H-6,ok,.6),skyBg(12,ok)),parts:[...sun(4,4,2.2),
   ...[[24,7],[25.6,9.4],[23.4,10.4],[26.6,12.4],[24.6,13.6],[26.4,16]].map(([x,y])=>({shape:ellipse(x,y,.7,1),color:'S',outline:false})),
   {shape:union(seg(15,12.6,21.4,5.8,1.2),circle(22.4,4.8,1.9)),color:'N',light:'B',flat:true,group:'can'},
   {shape:cut(circle(5.2,11,4.4),circle(5.2,11,2.4)),color:'N',light:'B',flat:true,group:'handle'},
   {shape:box(9.4,15.6,7,6.6,1.6),color:'N',light:'B',flat:true,group:'can'},
   {shape:box(9.4,11.6,7,.8),color:'S',outline:false},
   ...daisy(9.4,16.6,'C','Y',1.4),
   ...tulip(19,19,'R'),...tulip(23,19.6,'V'),...tulip(26,18.6,'P'),...daisy(3,25,'Y','O',.9),...daisy(14,26,'K','Y',.9)]};}},
 18:{title:'Грибной домик',draw(W,H,ok){
  const pathBg=(x,y)=>y>=H-6&&Math.abs(x-15-(y-H+6)*.6)<1.6+(y-H+6)*.4?((x+y)%3===0?'E':'C'):null;
  return {background:layer(pathBg,grassBg(W,H,H-6,ok,.5),skyBg(13,ok)),parts:[cloud(5,3,.8),cloud(26,5,.7),
   {shape:box(15,20,7.4,6.6,1.4),color:'C',shade:'Y',flat:true,group:'stem'},
   {shape:box(15,22.6,2.2,4,.8),color:'E',light:'A',flat:true,group:'door'},{shape:circle(16.4,22.6,.55),color:'Y',outline:false},
   ...[[10.6,18.4],[19.4,18.4]].flatMap(([x,y])=>[{shape:circle(x,y,1.8),color:'Y',light:'C',sphere:[x-.5,y-.5,1.8],group:'w'+x},{shape:box(x,y,.35,1.8),color:'E',outline:false}]),
   {shape:union(ellipse(15,10.6,13.4,8),box(15,14.2,13,1.6,1.4)),color:'R',light:'P',shade:'V',sphere:[12,7,14],group:'cap'},
   ...dots([[7,8.6],[13,5.6],[19.4,7],[24.4,10.8],[10.4,12.6],[17.6,11.6],[4.6,13]],1.5,'C'),
   ...tulip(5,23,'V'),...tulip(25,23.4,'P'),...daisy(9,28,'Y','O',.8),...daisy(23,28.4,'C','Y',.8)]};}},
 19:{title:'Цветок на окне',draw(W,H,ok){
  const win=(x,y)=>x>=3&&x<=24&&y>=2&&y<=19?((x===13||x===14||y===10)?'C':(y<12?'B':'S')):null;
  return {background:layer(win,stripesBg('K','P',5)),parts:[
   {shape:box(14,21,13.4,1.4,.4),color:'C',shade:'Y',flat:true,group:'sill'},
   {shape:poly([[8,21],[20,21],[18.4,28.6],[9.6,28.6]]),color:'O',light:'Y',shade:'A',flat:true,group:'pot'},{shape:box(14,21.6,6.6,1.2,.4),color:'A',shade:'E',flat:true,group:'pot2'},
   {shape:box(14,16,.9,5),color:'M',group:'stem'},{shape:leaf(10,16.6,7,2.4,-.5),color:'G',light:'L',shade:'M',flat:true,group:'l1'},{shape:leaf(18,15,7,2.4,.5),color:'G',light:'L',shade:'M',flat:true,group:'l2'},
   {shape:ring(14,8.6,4.6,6,3),color:'P',light:'K',shade:'R',sphere:[13,7.6,8],group:'fl'},
   {shape:circle(14,8.6,2.6),color:'Y',light:'C',shade:'O',sphere:[13.4,8,2.8],group:'mid'}]};}},
 20:{title:'Тыква у ручья',bridge:true,draw(W,H,ok){
  return {background:layer(grassBg(W,H,H-11,ok,.5),skyBg(10,ok)),parts:[...sun(25,3.6,2.2),cloud(6,3.2,.8),
   {shape:leaf(6,17.4,7,2.4,-.3),color:'G',light:'L',shade:'M',flat:true,group:'pl'},{shape:leaf(24.6,18,7,2.4,.3),color:'G',light:'L',shade:'M',flat:true,group:'pr'},
   {shape:path([[5,19.4],[8,18],[10,19.6]],.5),color:'M',outline:false},
   {shape:union(ellipse(15,14.4,11,7.2),ellipse(9.4,14.4,5.4,6.6),ellipse(20.6,14.4,5.4,6.6)),color:'O',light:'Y',shade:'A',sphere:[12,11.6,12],lightAt:.74,group:'pump'},
   {shape:union(seg(15,8.4,15,20,.5),seg(10.4,8.8,10,19.6,.45),seg(19.6,8.8,20,19.6,.45)),color:'A',outline:false},
   {shape:path([[15,8.2],[15.6,5.6],[17,4.4]],.85),color:'E',group:'stemp'}]};}},
 21:{title:'Груша у ручья',bridge:true,draw(W,H,ok){
  return {background:layer(grassBg(W,H,H-10,ok,.5),skyBg(9,ok)),parts:[cloud(4,3,.8),cloud(23,4,.7),
   {shape:union(circle(14,15,8),circle(14,7.4,4.6),poly([[9.6,7],[18.4,7],[21.8,13],[6.2,13]])),color:'L',light:'C',shade:'G',sphere:[11.6,10,11],lightAt:.76,group:'pear'},
   ...dots([[10.4,13],[16.6,10.4],[16,17],[11.6,18.4],[18.6,14.4],[13,9.6]],.55,'M'),
   {shape:path([[14,3],[14.4,1.4],[15.6,.4]],.75),color:'E',group:'stem'},{shape:leaf(19,2,6,2,-.4),color:'G',shade:'M',flat:true,group:'leaf'},
   ...tulip(3,17,'R'),...tulip(25,17.4,'K')]};}},
 22:{title:'Домик у ручья',bridge:true,draw(W,H,ok){
  return {background:layer(grassBg(W,H,H-12,ok,.5),skyBg(12,ok)),parts:[...sun(29,4,2.4),cloud(6,4,.9),
   ...[[2.6,16],[5.6,16]].map(([x])=>({shape:box(x,20,.5,2.4),color:'C',outline:false})),{shape:box(4,19,3.6,.4),color:'C',outline:false},
   {shape:union(circle(28.6,11.6,4.2),circle(26,14,3),circle(31,14.4,2.8)),color:'G',light:'L',shade:'M',sphere:[27,11,5.4],group:'tree'},{shape:box(28.6,19.4,.9,2.6),color:'E',group:'trunk'},
   {shape:box(22.4,7.4,1.4,2.8),color:'A',shade:'E',flat:true,group:'chim'},...[[23,4.2,1],[24,2.4,1.3]].map(([x,y,r])=>({shape:circle(x,y,r),color:'C',outline:false})),
   {shape:box(15,16.6,8.6,5.4),color:'K',shade:'P',flat:true,group:'house'},
   {shape:mask(box(15,16.6,8.4,5.2),(x,y)=>y%2===0&&(x+y/2)%4===0),color:'P',outline:false},
   {shape:poly([[4.4,11.6],[15,3.6],[25.6,11.6]]),color:'R',light:'P',shade:'V',flat:true,group:'roof'},
   {shape:mask(poly([[6,10.8],[15,4.4],[24,10.8]]),(x,y)=>y%2===1&&(x+y)%3===0),color:'V',outline:false},
   {shape:box(15,18.6,2.2,3.4,.6),color:'E',light:'A',flat:true,group:'door'},{shape:circle(16.3,18.8,.55),color:'Y',outline:false},
   ...[[9.4,15.6],[20.6,15.6]].flatMap(([x,y])=>[{shape:box(x,y,2,1.8),color:'Y',light:'C',flat:true,group:'w'+x},{shape:union(box(x,y,.3,1.8),box(x,y,2,.3)),color:'E',outline:false}]),
   ...tulip(8,20,'V',.8),...tulip(21.4,20.4,'Y',.8)]};}},
 23:{title:'Воздушный шар',draw(W,H,ok){
  return {background:layer(grassBg(W,H,H-3,ok,.4),skyBg(15,ok)),parts:[cloud(4,6,.8),cloud(20,16,.8),cloud(3,18,.6),
   ...[[19,4],[21.4,3]].map(([x,y])=>({shape:union(seg(x-1,y,x,y+.6,.4),seg(x,y+.6,x+1,y,.4)),color:'D',outline:false})),
   {shape:union(circle(12,10,8.4),poly([[5,13.6],[19,13.6],[14,19.4],[10,19.4]])),color:'R',light:'P',shade:'V',sphere:[10,8,10],group:'bal'},
   {shape:mask(union(circle(12,10,8.2),poly([[5.2,13.6],[18.8,13.6],[14,19.2],[10,19.2]])),(x)=>Math.abs(x-11.5)<1.6||Math.abs(x-11.5)>4.6&&Math.abs(x-11.5)<6.4),color:'Y',outline:false},
   {shape:union(seg(10.2,19.4,10.6,21.4,.4),seg(13.8,19.4,13.4,21.4,.4)),color:'D',outline:false},
   {shape:box(12,22.6,2.2,1.4,.4),color:'A',shade:'E',flat:true,group:'basket'}]};}},
 24:{title:'Улитка у ручья',bridge:true,draw(W,H,ok){
  return {background:layer(grassBg(W,H,H-12,ok,.5),skyBg(11,ok)),parts:[...sun(4,4,2.2),cloud(22,3.4,.8),
   {shape:union(ellipse(15,18.4,11,1.8),poly([[24,17],[27.6,13.4],[28.4,14.6],[26,18.4]])),color:'L',shade:'G',flat:true,group:'body'},
   {shape:union(seg(26.4,14,25.6,10.6,.5),seg(27.8,14.4,28.6,11,.5),circle(25.6,10.4,.8),circle(28.6,10.8,.8)),color:'L',outline:false},
   {shape:circle(14,12.4,7),color:'O',light:'Y',shade:'A',sphere:[12,10,8],group:'shell'},
   {shape:path(Array.from({length:22},(_,i)=>{const a=i*.55,r=5.6-i*.24;return [14+r*Math.cos(a),12.4+r*Math.sin(a)];}),.5),color:'E',outline:false},
   {shape:union(ellipse(4,16.6,2,2.6),box(4,18.6,.7,1.2)),color:'R',outline:false},...dots([[3.4,16],[4.8,17]],.5,'C'),
   ...daisy(23,15,'P','Y',.7)]};}},
 25:{title:'Котёнок с клубком',draw(W,H,ok){
  return {background:layer(checker('V','K',2,27),dotsBg('S','C',4)),parts:[
   {shape:union(circle(25.4,26.4,4.2)),color:'P',light:'K',shade:'R',sphere:[24.4,25.4,4.2],group:'yarn'},
   {shape:mask(circle(25.4,26.4,3.6),(x,y)=>(x+y)%2===0&&(x-y+99)%3!==0),color:'R',outline:false},
   {shape:path([[21.4,27.4],[18,28.6],[14,28]],.5),color:'P',outline:false},
   {shape:union(ellipse(13.4,22.6,8,6.8),path([[21,24],[24.6,20.6],[25,16]],1.2)),color:'O',light:'Y',shade:'A',sphere:[11,19,10],group:'body'},
   {shape:ellipse(13.4,24,4,4),color:'C',outline:false},
   {shape:union(ellipse(13.4,11.6,9,7.4),poly([[5,8],[6.4,0.6],[11,4.6]]),poly([[21.8,8],[20.4,.6],[15.8,4.6]])),color:'O',light:'Y',shade:'A',sphere:[11,9,11],group:'head'},
   {shape:union(poly([[6.6,6],[7.4,2.6],[9.6,4.8]]),poly([[20.2,6],[19.4,2.6],[17.2,4.8]])),color:'K',outline:false},
   {shape:mask(ellipse(13.4,10,8,5.4),(x,y)=>y<9&&(x===10||x===13||x===16)),color:'E',outline:false},
   {shape:ellipse(13.4,15.4,4.4,2.6),color:'C',outline:false},
   ...[9.6,17.2].flatMap(x=>[{shape:ellipse(x,11.6,1.6,1.9),color:'L',outline:false},{shape:ellipse(x,11.8,.6,1.4),color:'D',outline:false}]),
   {shape:poly([[12.4,13.8],[14.4,13.8],[13.4,15]]),color:'K',outline:false},
   {shape:union(seg(4,13.4,8.4,14.2,.4),seg(4,15.6,8.4,15,.4),seg(22.8,13.4,18.4,14.2,.4),seg(22.8,15.6,18.4,15,.4)),color:'D',outline:false}]};}},
 26:{title:'Подсолнух у забора',draw(W,H,ok){
  return {background:layer(grassBg(W,H,H-4,ok,.4),(x,y)=>y>=14&&y<H-4&&(x%5===1||x%5===2||y===16||y===20)?'C':null,skyBg(12,ok)),parts:[cloud(4,3,.8),
   {shape:box(14,22,1,8),color:'M',group:'stem'},{shape:leaf(9.6,21,8,2.8,-.4),color:'G',light:'L',shade:'M',flat:true,group:'l1'},{shape:leaf(18.4,23.6,8,2.8,.4),color:'G',light:'L',shade:'M',flat:true,group:'l2'},
   {shape:ring(14,10,7,12,2.6),color:'Y',light:'C',shade:'O',sphere:[13,9,10],group:'petals'},
   {shape:circle(14,10,5.4),color:'E',light:'A',sphere:[13,9,5.6],group:'mid'},
   {shape:mask(circle(14,10,4.6),(x,y)=>(x+y)%2===0),color:'A',outline:false},
   ...tulip(3,22,'R'),...tulip(25,22.6,'V')]};}},
 27:{title:'Кит с садом',draw(W,H,ok){
  const sea=(x,y)=>y<26?null:(y===26&&x%4!==1)||(y===27&&x%4===2)?'S':y>H-3?'N':'B';
  return {background:layer(sea,skyBg(9,ok)),parts:[...sun(26,4,2.4),cloud(6,3.6,.8),
   ...[[11,5.6],[13,3.4],[15,5.6]].map(([x,y])=>({shape:circle(x,y,1.2),color:'S',outline:false})),{shape:seg(13,8.6,13,6,.6),color:'S',outline:false},
   {shape:union(ellipse(14,18,12.4,6.8),poly([[24,18],[29.6,11.6],[30.6,14],[27,19.4],[30.6,23],[29.4,24.6]])),color:'N',light:'B',sphere:[12,15,13],group:'whale'},
   {shape:meet(ellipse(14,18,12.4,6.8),(x,y)=>y>19.6?-1:1),color:'C',outline:false},
   {shape:mask(meet(ellipse(14,18,11.6,6.2),(x,y)=>y>20.2?-1:1),(x,y)=>y%2===0),color:'K',outline:false},
   {shape:circle(7,16.4,1.1),color:'D',outline:false},{shape:ellipse(5,19.4,1.6,.6),color:'K',outline:false},
   {shape:ellipse(15,11.4,7,1.6),color:'G',light:'L',flat:true,group:'garden'},
   ...tulip(12,8.6,'R',.8),...tulip(15,8,'Y',.8),...tulip(18,8.6,'V',.8)]};}},
 28:{title:'Яблоко на столе',draw(W,H,ok){
  return {background:layer(planks('A','E',19,3),dotsBg('S','C',4)),parts:[
   {shape:union(circle(9.4,11.6,7),circle(14.6,11.6,7),circle(12,15.4,6.4)),color:'R',light:'P',shade:'V',sphere:[9.6,9.6,10],lightAt:.72,group:'apple'},
   {shape:path([[12,5.4],[12.4,3],[13.4,1.6]],.7),color:'E',group:'stem'},{shape:leaf(16.4,3.2,6,2.2,-.3),color:'G',light:'L',shade:'M',flat:true,group:'leaf'},
   {shape:ellipse(12,21.2,8.6,1),color:'E',outline:false}]};}},
 29:{title:'Садовый фонарь',bridge:true,draw(W,H,ok){
  return {background:layer(grassBg(W,H,H-12,ok,.5),nightBg([[2,2],[6,5],[11,1],[27,3],[31,7],[3,10],[29,13],[17,2]])),parts:[{shape:cut(circle(28,4.6,2.8),circle(29.4,3.8,2.4)),color:'C',outline:false},
   {shape:path([[0,2.6],[9,3.4],[18,3]],1.1),color:'E',light:'A',flat:true,group:'branch'},
   ...[[4,5,.6],[10,1,-.6]].map(([x,y,a])=>({shape:leaf(x,y,4.6,1.6,a),color:'G',light:'L',shade:'M',flat:true,group:'lf'+x})),
   {shape:seg(17,3.4,17,5.4,.5),color:'D',outline:false},
   {shape:poly([[6.4,10.6],[17,5],[27.6,10.6]]),color:'R',light:'P',shade:'V',flat:true,group:'roof'},
   {shape:box(17,17.4,7.6,6.8,.6),color:'E',light:'A',flat:true,group:'frame'},
   {shape:box(17,17.4,6.2,5.4),color:'Y',light:'C',shade:'O',sphere:[15.6,16,8],group:'glass'},
   {shape:circle(17,18,2),color:'C',outline:false},
   {shape:union(box(17,17.4,.45,5.4),box(17,17.4,6.2,.45)),color:'E',outline:false},
   {shape:box(17,24.8,5,.9),color:'E',group:'frame'},
   ...[[3,13],[31,11],[31,17],[3,19],[26,5]].map(([x,y])=>({shape:circle(x,y,.75),color:'Y',outline:false})),
   ...tulip(4,23,'V',.8),...tulip(30,23.4,'K',.8)]};}},
 30:{title:'Зайчик с морковкой',draw(W,H,ok){
  return {background:layer(grassBg(W,H,H-5,ok,.5),skyBg(14,ok)),parts:[cloud(4,3,.8),...sun(24,3.6,2.2),
   {shape:union(ellipse(9.8,6.8,2.8,6.6),ellipse(18.2,6.8,2.8,6.6)),color:'C',shade:'K',flat:true,group:'ears'},
   {shape:union(ellipse(9.8,7,1.2,4.8),ellipse(18.2,7,1.2,4.8)),color:'K',outline:false},
   {shape:ellipse(14,22.6,8.6,6.4),color:'C',shade:'K',sphere:[12,20,10],group:'body'},
   {shape:ellipse(14,23.4,4.6,4.2),color:'K',outline:false},
   {shape:ellipse(14,14.2,7.6,6.8),color:'C',shade:'K',sphere:[12,12,9],group:'head'},
   ...[11,17].map(x=>({shape:ellipse(x,13.4,1,1.4),color:'D',outline:false})),...[8.8,19.2].map(x=>({shape:ellipse(x,16.2,1.3,.8),color:'K',outline:false})),
   {shape:poly([[13,15.8],[15,15.8],[14,17]]),color:'P',outline:false},
   {shape:poly([[19,19],[24.4,24.4],[22.6,25.6]]),color:'O',light:'Y',shade:'A',flat:true,group:'carrot'},
   {shape:mask(poly([[19,19],[24.4,24.4],[22.6,25.6]]),(x,y)=>(x+y)%3===0),color:'A',outline:false},
   ...[[18,17.6,-.9],[19.6,17,-.3]].map(([x,y,a])=>({shape:leaf(x,y,3.6,1.1,a),color:'G',outline:false})),
   ...daisy(3,26,'Y','O',.8),...daisy(25,27,'P','Y',.8)]};}},
};
// Bridge levels: the stream wall, the divider above it, and the bridge sites (cell on the wall, anchor below it).
function streamLayout(id,W,H){const wall=H-6,mid=Math.floor(W/2),n=BRIDGES[id],xs=n===1?[Math.round(W*.25)]:[Math.round(W*.25),Math.round(W*.75)];
 const walls=new Set();for(let x=0;x<W;x++)walls.add(wall*W+x);for(let y=wall-3;y<wall;y++)for(const x of [mid-1,mid])walls.add(y*W+x);
 const idx=(x,y)=>(H-1-y)*W+x;
 return {walls,hero:{id:'sprig-bridge',choices:xs.map((x,i)=>({cell:idx(x,wall),anchor:idx(x,wall+1),label:n===1?'Левый мостик':i?'Правый мостик':'Левый мостик'}))}};}
// Draw at the authored size and sample at the board's: shapes and backgrounds are scaled, outlines stay one cell.
// window: the authored rectangle [x0, y0, width, height] shown on the board.
function scaled(spec,[x0,y0,w,h],W,H,bare){const sx=w/W,sy=h/H,k=(sx+sy)/2,X=x=>x0+x*sx,Y=y=>y0+y*sy;
 return {background:bare?()=> '.':(x,y)=>spec.background(Math.floor(X(x+.5)),Math.floor(Y(y+.5))),
  parts:spec.parts.filter(p=>!(bare&&p.deco)).map(p=>({...p,shape:(x,y)=>p.shape(X(x),Y(y))/k,...(p.sphere?{sphere:[(p.sphere[0]-x0)/sx,(p.sphere[1]-y0)/sy,p.sphere[2]/k]}:{})}))};}
export function sceneLevel(id){const s=SCENES[id];if(!s)return null;const [w0,h0]=sizeOf(id),allowed=allowedAt(id),ok=c=>allowed.includes(c),bare=BARE.has(id);
 let [W,H]=BOARD[id],win=[0,0,w0,h0];
 if(bare){// An object-only picture fills its board: crop to the object (one cell of open ground round it), keep the height.
  const full=paint(w0,h0,{...scaled(s.draw(w0,h0,ok),win,w0,h0,true),outline:s.outline||'D',allowed});let x1=w0,y1=h0,x2=-1,y2=-1;
  full.forEach((r,y)=>r.forEach((c,x)=>{if(c!=='.'){x1=Math.min(x1,x);x2=Math.max(x2,x);y1=Math.min(y1,y);y2=Math.max(y2,y);}}));
  win=[x1-1,y1-1,x2-x1+3,y2-y1+3];W=Math.max(12,Math.round(H*win[2]/win[3]));}
 const spec=scaled(s.draw(w0,h0,ok),win,W,H,bare),col=paint(W,H,{...spec,outline:s.outline||'D',allowed});let hero;
 if(s.bridge){const st=streamLayout(id,W,H);for(let y=0;y<H;y++)for(let x=0;x<W;x++)if(st.walls.has(y*W+x))col[y][x]='#';hero=st.hero;
  // The near bank (below the wall) is grass with a few flowers, as in v8.
  for(let y=H-5;y<H;y++)for(let x=0;x<W;x++)col[y][x]=y===H-1&&ok('M')?'M':((x*7+y*3)%13===0?'Y':'G');}
 const used=new Set(col.flat());for(const c of used)if(c!=='#'&&c!=='.'&&!allowed.includes(c))throw Error(`L${id} uses ${c} before it is introduced`);
 return {title:s.title,w:W,h:H,rowsBottomFirst:rows(col).reverse(),entries:[Math.floor(W/2)],...(hero?{hero}:{})};}

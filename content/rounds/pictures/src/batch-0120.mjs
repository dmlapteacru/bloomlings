// Wave L21–L50 bridge pictures (the owner's plan of 2026-10-28, product/plans/wave-L21-L50-mechanics-plan-2026-10-28.md):
// each carries its own strip (a stream drawn into the picture) and two Sprig bridge sites, crossing up, left, right or,
// on the two Super Hard pictures, from two different strips into one closed corner (17 §8 as amended on 2026-10-28).
// The approach to each site is colour cells of the near bank, so no bridge is free at the start; the two approaches
// are different materials where the picture allows.
import {paint,circle,ellipse,box,poly,union,cut,meet,rows} from '../../../../tools/gameplay-pilot-v9/draw.mjs';

const grid=(W,H,f)=>Array.from({length:H},(_,y)=>Array.from({length:W},(_,x)=>f(x,y)));
const put=(g,x,y,c)=>{if(y>=0&&y<g.length&&x>=0&&x<g[0].length)g[y][x]=c;};
const blit=(g,sub,ox,oy)=>sub.forEach((r,y)=>r.forEach((c,x)=>put(g,ox+x,oy+y,c)));
const flat=a=>()=>a;
// a thick line from a to b, radius r (cells)
const seg=(ax,ay,bx,by,r)=>(x,y)=>{const ex=bx-ax,ey=by-ay,t=Math.max(0,Math.min(1,((x-ax)*ex+(y-ay)*ey)/(ex*ex+ey*ey)));return Math.hypot(x-ax-ex*t,y-ay-ey*t)-r;};
// a few scattered tufts: deterministic, never in lines
const tufts=(g,c,n,x0,y0,x1,y1,seed=1)=>{let s=seed*2654435761>>>0;const r=()=>(s=(s*1103515245+12345)>>>0)/2**32;for(let i=0;i<n;i++){const x=x0+Math.floor(r()*(x1-x0)),y=y0+Math.floor(r()*(y1-y0));put(g,x,y,c);put(g,x+1,y,c);}};
const strip=(g,cells)=>cells.forEach(([x,y])=>put(g,x,y,'#'));
const site=(g,x,y)=>put(g,x,y,'b');

// 1 · up (L23): mushrooms on the far bank of a forest stream; stones lead to the left bridge, lime moss to the right
function mushrooms(){const W=28,H=29,g=grid(W,H,flat('M'));
 const far=paint(W,20,{background:flat('G'),parts:[
  {shape:box(9,14.5,2.6,4.5,1.2),color:'C',shade:'A',flat:true},
  {shape:meet(ellipse(9,9,8.5,6),box(9,5.5,9.5,4.5)),color:'R',shade:'E',flat:true},
  {shape:circle(6,6,1.4),color:'C',outline:false},{shape:circle(12,5,1.3),color:'C',outline:false},{shape:circle(9.5,8,1.2),color:'C',outline:false},{shape:circle(14.5,8,1),color:'C',outline:false},{shape:circle(4,8.6,.9),color:'C',outline:false},
  {shape:box(21,15.5,1.6,3,.8),color:'C',shade:'A',flat:true},
  {shape:meet(ellipse(21,12.5,5.5,3.8),box(21,10.5,6,2.8)),color:'A',shade:'E',flat:true},
  {shape:union(ellipse(2.5,18.6,2.5,1.2),ellipse(15.5,18.7,2.6,1.1)),color:'L',outline:false}]});
 blit(g,far,0,0);
 tufts(g,'L',5,1,1,27,3,3);
 strip(g,Array.from({length:W},(_,x)=>[x,20]));site(g,6,20);site(g,21,20);
 for(let y=21;y<24;y++)for(let x=4;x<9;x++)put(g,x,y,'C');put(g,4,23,'M');put(g,8,23,'M');
 for(let y=21;y<23;y++)for(let x=19;x<24;x++)put(g,x,y,'L');
 tufts(g,'G',6,0,24,27,29,7);
 return rows(g);}


const vstrip=(g,x,y0,y1)=>{for(let y=y0;y<=y1;y++)put(g,x,y,'#');};
const hstrip=(g,y,x0,x1)=>{for(let x=x0;x<=x1;x++)put(g,x,y,'#');};
const fillBox=(g,x0,y0,x1,y1,c)=>{for(let y=y0;y<=y1;y++)for(let x=x0;x<=x1;x++)put(g,x,y,c);};

// 2 · left (L26): a puppy on the near bank; across a stream on the left, the flower bed it is not allowed into.
// Yellow flowers lead to the upper bridge, pebbles to the lower one.
function puppy(){const W=32,H=30,g=grid(W,H,flat('G'));
 const bed=paint(10,H,{background:flat('G'),parts:[
  ...[2,7].flatMap((cx,i)=>[3,10,17,24].map((cy,j)=>({shape:box(cx,cy+2.5,.5,2.5),color:'M',outline:false}))),
  ...[2,7].flatMap((cx,i)=>[3,10,17,24].map((cy,j)=>({shape:circle(cx,cy,1.9),color:(i+j)%3===0?'Y':(i+j)%3===1?'P':'K',shade:'R',flat:true})))]});
 blit(g,bed,0,0);
 const dog=paint(21,H,{background:flat('G'),parts:[
  {shape:union(box(5,21,1.2,5,.6),box(14,21,1.2,5,.6)),color:'A',shade:'E',flat:true},
  {shape:union(seg(16.5,16,18.5,12,1.1),seg(18.5,12,19.2,9.5,1)),color:'A',shade:'E',flat:true},
  {shape:ellipse(10,18,7.5,5),color:'A',light:'C',shade:'E',flat:true},
  {shape:box(10,14.5,4.5,.9,.4),color:'P',outline:false},
  {shape:circle(10,9.5,5.5),color:'A',shade:'E',flat:true},
  {shape:ellipse(4.6,9.5,1.8,4),color:'E',flat:true},{shape:ellipse(15.4,9.5,1.8,4),color:'E',flat:true},
  {shape:ellipse(10,12,2.8,2),color:'C',outline:false},
  {shape:circle(10,11.2,1),color:'D',outline:false},{shape:circle(7.8,8.4,.8),color:'D',outline:false},{shape:circle(12.2,8.4,.8),color:'D',outline:false}]});
 blit(g,dog,11,0);
 vstrip(g,10,0,H-1);site(g,10,7);site(g,10,22);
 fillBox(g,11,6,13,8,'Y');fillBox(g,11,21,13,23,'C');
 return rows(g);}

// 3 · right (L29): an apple tree across a river on the right; on the near bank a basket of picked apples.
// The upper bridge starts from the sky, the lower one from the grass.
function appleTree(){const W=30,H=32,sky=(x,y)=>y<20?'S':'G',g=grid(W,H,sky);
 const tree=paint(12,H,{background:sky,parts:[
  {shape:union(box(6,25,1.7,6.5,.5),seg(6,22,9.5,19.5,.8)),color:'E',shade:'D',flat:true},
  {shape:union(circle(6,9.5,5.8),circle(2.6,13.5,3.9),circle(9.4,13.5,3.9),circle(6,15,4)),color:'G',light:'L',shade:'M',flat:true},
  ...[[3.5,8],[8,7.5],[5.5,12],[9.5,14.5],[2.5,15],[6.5,17]].map(([x,y])=>({shape:box(x,y,1,1),color:'R',outline:false}))]});
 blit(g,tree,18,0);
 const basket=paint(17,H,{background:sky,parts:[
  {shape:union(circle(4.5,19,2),circle(8.5,18,2.2),circle(12.5,19,2),circle(6.5,16.2,1.9),circle(10.5,16,1.9)),color:'R',flat:true},
  {shape:poly([[1.5,20],[15.5,20],[13.5,27],[3.5,27]]),color:'A',shade:'E',flat:true},
  {shape:union(box(8.5,22.3,6,.35),box(8.5,24.7,5.2,.35)),color:'E',outline:false},
  {shape:cut(ellipse(8.5,20,6.5,6),union(ellipse(8.5,20,5.3,4.8),box(8.5,24,8,4))),color:'E'}]});
 blit(g,basket,0,0);
 vstrip(g,17,0,H-1);site(g,17,10);site(g,17,25);
 return rows(g);}

// 4 · two strips (L30): a crystal cave closed off in the upper left by an L of rock streams; one bridge crosses up
// into it from the cave floor, the other crosses left from the miner's side. A mine cart on rails and a lantern.
function crystalCave(){const W=32,H=32,rock=(x,y)=>y>26?'A':'E',g=grid(W,H,rock);
 const cave=paint(15,15,{background:flat('N'),parts:[
  {shape:poly([[.5,15],[1.5,8],[3.5,6],[5.5,8],[6.5,15]]),color:'V',light:'K',flat:true},
  {shape:poly([[5.5,15],[6.5,5],[9,2],[11.5,5],[12.5,15]]),color:'S',light:'C',flat:true},
  {shape:poly([[11,15],[11.8,10],[13.2,8.5],[14.6,10],[15.5,15]]),color:'K',light:'C',shade:'V',flat:true}]});
 blit(g,cave,0,0);
 const near=paint(W,H,{background:rock,parts:[
  {shape:union(box(16,27.6,16,.45),box(16,29.4,16,.45)),color:'D',outline:false},
  {shape:poly([[18.5,19.5],[29.5,19.5],[28.5,25.5],[19.5,25.5]]),color:'D',light:'N',flat:true},
  {shape:union(circle(21.5,26.6,1.7),circle(26.5,26.6,1.7)),color:'D',outline:false},
  {shape:poly([[19.5,19.6],[21,16],[22.5,19.6]]),color:'V',light:'K',flat:true,outline:false},
  {shape:poly([[22.5,19.6],[24.2,15],[26,19.6]]),color:'S',light:'C',flat:true,outline:false},
  {shape:poly([[25.8,19.6],[27.2,16.5],[28.6,19.6]]),color:'K',light:'C',flat:true,outline:false},
  {shape:box(24,3,.35,3),color:'D',outline:false},{shape:box(24,9,2.8,3.4,.8),color:'Y',shade:'A',flat:true},
  {shape:union(ellipse(5,23,4.5,3.2),ellipse(10.5,25,3.2,2.3),ellipse(2.5,28,2.5,1.6)),color:'A',shade:'D',flat:true},
  {shape:seg(9,19,13.5,23.5,.6),color:'E',outline:false},{shape:seg(7.5,20.5,10.5,17.5,.75),color:'C',outline:false}]});
 for(let y=0;y<H;y++)for(let x=0;x<W;x++)if(x>15||y>15)put(g,x,y,near[y][x]);
 hstrip(g,15,0,15);vstrip(g,15,0,15);site(g,6,15);site(g,15,7);
 return rows(g);}

// 5 · right (L35): a flower stall across a canal on the right; on the near bank a bicycle with a basket of flowers.
function flowerStall(){const W=30,H=32,street=(x,y)=>y<20?'S':'C',g=grid(W,H,street);
 const stall=paint(13,H,{background:street,parts:[
  {shape:box(6.5,23.5,6,4.5,.5),color:'E',flat:true},
  ...[0,1,2,3,4,5].map(i=>({shape:box(1.5+i*2.1,8.5,1.05,2),color:i%2?'C':'R',outline:false})),
  {shape:box(6.5,6,6.3,.8),color:'R',outline:false},
  {shape:union(box(.8,14,.45,5),box(12.2,14,.45,5)),color:'E',outline:false},
  ...[[2.7,'P'],[6.5,'Y'],[10.3,'V']].flatMap(([x,c])=>[{shape:box(x,17.6,1.6,1.9,.4),color:'N',outline:false},{shape:circle(x,14.5,2),color:c,shade:'R',flat:true}])]});
 blit(g,stall,17,0);
 const bike=paint(16,H,{background:street,parts:[
  {shape:cut(circle(3.8,22.5,3.6),circle(3.8,22.5,2.2)),color:'D',outline:false},{shape:cut(circle(12.2,22.5,3.6),circle(12.2,22.5,2.2)),color:'D',outline:false},
  {shape:union(seg(3.8,22.5,6.8,16.5,.75),seg(6.8,16.5,8.2,22.5,.75),seg(8.2,22.5,3.8,22.5,.75),seg(6.8,16.5,11.4,16.5,.75),seg(11.4,15,12.2,22.5,.75),seg(8.2,22.5,11.4,16.5,.75)),color:'N',outline:false},
  {shape:box(6.4,15.3,1.6,.6,.3),color:'E',outline:false},{shape:seg(10.4,14.4,12.6,14.4,.6),color:'D',outline:false},
  {shape:box(13.4,12.6,2.2,1.6,.5),color:'E',outline:false},{shape:union(circle(12.6,10.6,1.2),circle(14.4,10.4,1.1)),color:'P',outline:false}]});
 blit(g,bike,0,0);
 vstrip(g,16,0,H-1);site(g,16,12);site(g,16,26);
 return rows(g);}

// 6 · two strips (L45): an inventor's flask shelf closed off in the upper right by an L of streams; one bridge crosses
// right from the workshop wall, the other crosses up from the workbench. A chalkboard and gears on the near side.
function lab(){const W=32,H=32,room=(x,y)=>y>24?'A':'S',g=grid(W,H,room);
 const shelf=paint(15,14,{background:flat('C'),parts:[
  {shape:union(box(7.5,5.3,7,.6),box(7.5,12,7,.6)),color:'E',outline:false},
  {shape:union(circle(3,3,1.7),box(3,1,.7,1)),color:'B',flat:true},
  {shape:box(7.5,2.9,1.5,1.9,.4),color:'V',flat:true},
  {shape:union(circle(12,3,1.7),box(12,1,.7,1)),color:'L',flat:true},
  {shape:box(3,9.6,1.5,1.9,.4),color:'L',flat:true},
  {shape:union(circle(8,9.9,1.7),box(8,7.9,.7,1)),color:'B',flat:true},
  {shape:box(12.5,9.6,1.5,1.9,.4),color:'V',flat:true}]});
 blit(g,shelf,17,0);
 const near=paint(W,H,{background:room,parts:[
  {shape:box(7,8,5.5,4),color:'D',outline:false},
  {shape:union(box(4.5,6.5,2,.35),box(8.5,6.5,1.5,.35),box(5.5,9,2.5,.35),box(9.5,9.5,1.2,.35)),color:'C',outline:false},
  {shape:box(10,21,8,1.2,.4),color:'E',light:'A',flat:true},{shape:union(box(3.5,23.5,.8,3),box(16.5,23.5,.8,3)),color:'E',outline:false},
  {shape:cut(circle(6.5,16.8,2.9),circle(6.5,16.8,1.1)),color:'O',shade:'E',flat:true},
  {shape:cut(circle(11.5,17.8,2.1),circle(11.5,17.8,.8)),color:'O',shade:'E',flat:true},
  {shape:box(15,18,1.4,1.6,.3),color:'B',outline:false},
  {shape:union(box(24,27.5,5.5,1.6,.5)),color:'E',light:'O',flat:true}]});
 for(let y=0;y<H;y++)for(let x=0;x<W;x++)if(x<16||y>14)put(g,x,y,near[y][x]);
 vstrip(g,16,0,14);hstrip(g,14,16,31);site(g,16,6);site(g,24,14);
 return rows(g);}

export default {theme:'wave-0021-bridges',pictures:[
 {title:'Грибная поляна за ручьём',forLevel:23,family:'Грибы и лесные находки',device:'ручей поперёк, мосты вверх',subject:'мухомор и подберёзовик на дальнем берегу лесного ручья; к левому мосту ведут камни, к правому мох',source:'оригинальный',rows:mushrooms()},
 {title:'Щенок у ручья',forLevel:26,family:'Домашние животные',device:'ручей вдоль слева, мосты влево',subject:'щенок на ближнем берегу, за ручьём слева клумба; к верхнему мосту ведут жёлтые цветы, к нижнему камешки',source:'оригинальный',rows:puppy()},
 {title:'Яблоня за рекой',forLevel:29,family:'Деревья, листья и плоды',device:'река вдоль справа, мосты вправо',subject:'яблоня с яблоками на дальнем берегу справа, на ближнем корзина с собранными яблоками',source:'оригинальный',rows:appleTree()},
 {title:'Пещера с кристаллами',forLevel:30,family:'Камни, минералы и кристаллы',device:'две полосы буквой Г, мосты вверх и влево',subject:'кристаллы в закрытом углу пещеры; с ближней стороны вагонетка с камнями и фонарь',source:'оригинальный',rows:crystalCave()},
 {title:'Цветочная лавка за каналом',forLevel:35,family:'Магазины и витрины',device:'канал вдоль справа, мосты вправо',subject:'цветочная лавка с полосатым навесом за каналом, на ближнем берегу велосипед с корзинкой',source:'оригинальный',rows:flowerStall()},
 {title:'Полка изобретателя',forLevel:45,family:'Лаборатории и изобретения',device:'две полосы буквой Г, мосты вправо и вверх',subject:'колбы с жидкостями на полке в закрытом углу; с ближней стороны верстак, шестерёнки и лампа',source:'оригинальный',rows:lab()},
]};

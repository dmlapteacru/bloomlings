// Batch 138: fourteen pictures with depth for wave L21–L50, one per theme family the wave still needs (the owner,
// 2026-10-29: «убедись, что они не повторяются с первыми 20 и между собой. Генерируй согласно тематикам»).
// Every picture nests its colours: a background or a thick shell round the subject, and two or three inner colours
// behind it, each with real volume, so some materials open only after their neighbours are eaten (17 §6).
import {circle,ellipse,box,poly,union,cut,meet} from '../../../../tools/gameplay-pilot-v9/draw.mjs';
import {raster,str,put,seg,ring4} from './helpers.mjs';

// polar angle and radius of a cell centre round (cx, cy)
const polar=(x,y,cx,cy)=>{const dx=x+.5-cx,dy=y+.5-cy;return {r:Math.hypot(dx,dy),a:Math.atan2(dy,dx)};};

// 1. binoculars lying on an old map: two brown barrels with eyepieces and a hinge, dark lens rims, deep-blue glass
function binoculars(){const W=34,H=32;
 const g=raster(W,H,'C',[
  [union(ellipse(1,2,7,5),ellipse(33,30,8,5),ellipse(32,1,5,3),ellipse(0,30,4,4)),'L'],
  [union(box(9.5,12.6,5.6,7,2.6),box(24.5,12.6,5.6,7,2.6),circle(9.5,20.8,7.6),circle(24.5,20.8,7.6)),'E'],
  [union(box(9.5,5.2,4.4,2,.8),box(24.5,5.2,4.4,2,.8),box(17,11.6,2.6,4.2,1)),'D'],
  [union(circle(9.5,20.8,6.4),circle(24.5,20.8,6.4)),'D'],
  [union(circle(9.5,20.8,5.1),circle(24.5,20.8,5.1)),'N'],
  [union(circle(8,19.3,2.9),circle(23,19.3,2.9)),'B'],
 ]);
 return str(g);}

// 2. a yeti waving at the mouth of its ice cave on a snowy night
function yeti(){const W=30,H=30;
 const g=raster(W,H,'N',[
  [ellipse(15,30,14.2,26),'B'],
  [ellipse(15,30,10.6,21.5),'D'],
  [union(box(15,28.2,16,2.2),ellipse(3.5,26.8,6.5,1.8),ellipse(26.5,26.8,6.5,1.8)),'S'],
  [union(circle(15,14.6,5.3),ellipse(15,22.4,7.1,6.3),seg(9.4,20.5,5.6,13.6,2.1),seg(20.6,20.5,23.4,25.4,2.1),box(12,26.4,2.1,1.8,.8),box(18,26.4,2.1,1.8,.8)),'C'],
  [ellipse(15,15.4,3.7,3.1),'K'],
  [ellipse(15,23.2,4,4.3),'K'],
  [circle(5.5,13.2,1.7),'K'],
 ]);
 put(g,'D',[[13,14],[13,15],[16,14],[16,15],[13,17],[14,17],[15,17],[16,17],[12,16],[17,16]]);
 return str(g);}

// 3. two fly agarics on a mossy hummock in the grass
function agarics(){const W=33,H=32;
 const g=raster(W,H,'S',[
  [union(box(16.5,28,17.5,4.2),ellipse(16.5,23.6,20,4.4)),'G'],
  [ellipse(17,23.8,11.4,3.4),'M'],
  [union(box(14,19.6,3,5,1),ellipse(14,23.4,4.2,2)),'C'],
  [meet(ellipse(14,14.6,10.2,9.8),box(14,9.2,10.8,5.6)),'R'],
  [meet(ellipse(14,14.8,8.8,2.6),box(14,16.2,9.4,1.6)),'Y'],
  [union(circle(9,10.6,1.6),circle(14,7.6,1.6),circle(19,9.4,1.6),circle(16.6,12.8,1.5),circle(11,13.4,1.4),circle(21.4,13,1.3),circle(6.6,13.4,1.2)),'C'],
  [box(26,22.6,1.6,2.6,.6),'C'],
  [meet(ellipse(26,19.6,3.9,4.2),box(26,17.2,4.3,2.6)),'R'],
  [box(26,20,3,.8),'Y'],
  [union(circle(24.8,17.4,1.1),circle(27.4,18,1)),'C'],
 ]);
 // a dark outline round both mushrooms, over the sky, the grass and the moss
 for(const from of ['S','G','M'])ring4(g,['R','C','Y'],'D',from);
 return str(g);}

// 4. an apple tree: dark rim of leaves, a lighter crown full of red apples, a stout trunk on the lawn
function appleTree(){const W=32,H=34;
 const crown=k=>union(circle(16,12.6,9.8-k),circle(8,15,6.2-k),circle(24,15,6.2-k),circle(11.4,7.6,5.6-k),circle(20.6,7.6,5.6-k));
 const apples=[[9,11],[14,7],[20,8],[24,14],[18,13],[12,15],[7,16],[21,18],[15,19],[25,10],[13,11],[17,16],[10,19]];
 const g=raster(W,H,'S',[
  [union(box(16,31.5,17,3),ellipse(16,28.6,20,2.6)),'L'],
  [union(box(16,24.4,3.6,4.6),poly([[10.4,28.4],[21.6,28.4],[19,25.4],[13,25.4]])),'E'],
  [crown(0),'M'],
  [crown(2.1),'G'],
  [union(...apples.map(([x,y])=>circle(x,y,1.75))),'R'],
 ]);
 return str(g);}

// 5. a heart-shaped box of chocolates on a pink tablecloth: red rim, gold paper, dark and white sweets
function chocolates(){const W=30,H=28;
 const outer=union(circle(8.5,9,7.9),circle(21.5,9,7.9),poly([[1.0,11.2],[29.0,11.2],[15,27.4]]));
 const tray=union(circle(8.5,9,6.3),circle(21.5,9,6.3),poly([[2.9,11.2],[27.1,11.2],[15,25.0]]));
 // sweets on rows 4.2 apart, every other row shifted by half a step; dark and white alternate
 const sweets=[];let row=0;
 for(let y=6;y<25;y+=4.2,row++)for(let i=-4;i<=4;i++){const x=15+i*4.5+(row%2?2.25:0);if(tray(x,y)<-1.5)sweets.push([x,y,(i+row+12)%2]);}
 const kinds=['E','C'];
 const g=raster(W,H,'K',[
  [outer,'R'],
  [tray,'Y'],
  ...sweets.map(([x,y,k])=>[circle(x,y,1.85),kinds[k]]),
 ]);
 return str(g);}

// 6. a cut ruby resting on a velvet cushion with gold piping and tassels
function ruby(){const W=30,H=27;
 const g=raster(W,H,'N',[
  [union(box(15,19.6,13.6,5.4,4.2),seg(2.6,23.6,1.6,25.4,.9),seg(27.4,23.6,28.4,25.4,.9),circle(1.6,25.6,1.2),circle(28.4,25.6,1.2)),'Y'],
  [box(15,19.6,12.4,4.2,3.4),'V'],
  [union(poly([[10.2,2.6],[19.8,2.6],[24.6,8.8],[5.4,8.8]]),poly([[5.4,8.8],[24.6,8.8],[15,21.8]])),'R'],
  [union(poly([[10.2,2.6],[12.8,2.6],[10.9,8.8],[5.4,8.8]]),poly([[5.4,8.8],[10.9,8.8],[15,21.8]])),'K'],
  [union(poly([[12.8,2.6],[17.2,2.6],[19.1,8.8],[10.9,8.8]]),poly([[10.9,8.8],[15.6,8.8],[15,21.8]])),'P'],
 ]);
 return str(g);}

// 7. a round-bottom flask of bubbling green liquid on the lab bench, cork in the neck
function flask(){const W=26,H=32;
 const g=raster(W,H,'C',[
  [box(13,30.5,14,1.6),'E'],
  [union(circle(13,19.5,11),box(13,5.5,2.8,4.6)),'D'],
  [union(circle(13,19.5,9.8),box(13,6,1.6,5.2)),'S'],
  [meet(circle(13,19.5,9.8),box(13,24.5,11,7.2)),'G'],
  [union(circle(8.5,21,1.8),circle(15.5,19.5,1.7),circle(11.5,26,1.8),circle(17.5,24.5,1.9),circle(13,22,1.5),circle(6.5,25,1.4),circle(19.5,21,1.3)),'L'],
  [union(circle(11.5,13.5,1.5),circle(14.5,10.5,1.2),circle(13,7.5,1)),'L'],
  [box(13,1.6,2.4,1.6,.5),'E'],
 ]);
 return str(g);}

// 8. a jester under a three-pointed cap with bells, ruff collar below the smiling face
function jester(){const W=32,H=34;
 // harlequin: the left half red, the right half plum (hood, cap points, tunic diamonds swap by the diagonal checker)
 const half=x=>x<16?'R':'V';
 const diamonds=(x,y)=>{const u=Math.floor((x+y)/4),v=Math.floor((x-y+40)/4);return (u+v)%2?'V':'R';};
 const g=raster(W,H,'N',[
  [box(16,32.4,17,1.8),'E'],
  [poly([[5,30.6],[27,30.6],[26,27],[21,25.4],[11,25.4],[6,27]]),diamonds],
  [union(...[8,12,16,20,24].map(x=>circle(x,25.6,2.3))),'Y'],
  [union(ellipse(16,19.2,9.6,8.4),poly([[7.6,12.6],[13.8,12.6],[10.8,6.8],[6,5.6],[4.4,10]]),poly([[24.4,12.6],[18.2,12.6],[21.2,6.8],[26,5.6],[27.6,10]]),poly([[12.4,12.6],[19.6,12.6],[17.8,6],[16,3.4],[14.2,6]])),half],
  [box(16,13.4,8.6,1.6,.8),'Y'],
  [ellipse(16,19.8,6.8,5.8),'K'],
  [union(circle(5,11.2,2.1),circle(27,11.2,2.1),circle(16,4.2,1.9)),'Y'],
 ]);
 put(g,'V',[[13,18],[13,19],[18,18],[18,19]]);
 put(g,'R',[[12,21],[13,22],[14,22],[15,22],[16,22],[17,22],[18,22],[19,21]]);
 return str(g);}

// 9. a wooden garden box cut open: carrot, beet and potatoes in the soil under their leaves
function gardenBed(){const W=32,H=32;
 const g=raster(W,H,'S',[
  [box(16,22.2,16,9.8),'E'],
  [box(16,21.4,13.6,8.6),'A'],
  [union(seg(8,13,5,6,1.2),seg(8,13,8,4.6,1.2),seg(8,13,11,6,1.2)),'M'],
  [union(seg(16,13,13,6.2,1.3),seg(16,13,19,6.2,1.3),seg(16,13,16,4.6,1.2)),'M'],
  [union(seg(24,13,21.2,6.6,1.2),seg(24,13,26.8,6.6,1.2),seg(24,13,24,5.4,1.2)),'M'],
  [poly([[3.8,13.8],[12.2,13.8],[8.8,27.6],[7.2,27.6]]),'O'],
  [union(circle(16,18.6,4.8),seg(16,22,16,27.2,.9)),'V'],
  [union(ellipse(22.6,19.4,2.8,2.2),ellipse(26.6,22.4,2.8,2.3),ellipse(22.6,25.6,2.8,2.1),ellipse(26.6,17.2,2.4,1.9),ellipse(26.8,27.2,2.2,1.6)),'Y'],
 ]);
 return str(g);}

// 10. a firefighter in a red helmet with a gold badge, big moustache, coat with reflective stripes
function firefighter(){const W=30,H=30;
 const g=raster(W,H,'B',[
  [poly([[0.5,30],[29.5,30],[28,24.4],[20,22.2],[10,22.2],[2,24.4]]),(x,y)=>y>=26&&y<=27?'Y':'N'],
  [poly([[11,22.4],[19,22.4],[17,25.6],[15,24],[13,25.6]]),'R'],
  [box(15,21.6,3,1.4),'K'],
  [union(ellipse(15,16,7,6.6),circle(7.8,16.2,1.8),circle(22.2,16.2,1.8)),'K'],
  [union(meet(ellipse(15,10.2,10.6,9),box(15,5.4,11,5.4)),ellipse(15,10.6,14,2.2),box(15,1.6,1.3,1.2)),'R'],
  [poly([[12.4,3.6],[17.6,3.6],[17.6,7.4],[15,9.6],[12.4,7.4]]),'Y'],
 ]);
 put(g,'E',[[10,13],[11,13],[12,13],[17,13],[18,13],[19,13],[12,15],[12,16],[17,15],[17,16],
  [8,12],[8,13],[8,14],[21,12],[21,13],[21,14],
  [9,19],[10,19],[11,19],[12,19],[13,19],[14,19],[15,19],[16,19],[17,19],[18,19],[19,19],[20,19],
  [8,20],[9,20],[11,20],[12,20],[13,20],[14,20],[15,20],[16,20],[17,20],[18,20],[20,20],[21,20]]);
 // a short beard on the chin under the moustache
 for(let y=21;y<=22;y++)for(let x=9;x<=20;x++)if(g[y][x]==='K')g[y][x]='E';
 return str(g);}

// 11. a round stained-glass rose window in a stone wall: lead lines, blue and plum outer panes, red ring, gold heart
function roseWindow(){const W=32,H=32,cx=16,cy=16;
 const glass=(x,y)=>{const {r,a}=polar(x,y,cx,cy),t=(a+Math.PI)/(2*Math.PI);
  if(r>=13.4)return 'D';
  if(r>=9.2&&r<10.1)return 'D';
  if(r>=5.1&&r<6)return 'D';
  if(r>=10.1){const s=t*12,d=Math.abs(s-Math.round(s))*2*Math.PI*r/12;if(d<.5)return 'D';return Math.floor(s)%2?'V':'B';}
  if(r>=6){const s=t*8+.5,d=Math.abs(s-Math.round(s))*2*Math.PI*r/8;if(d<.5)return 'D';return 'R';}
  return 'Y';};
 const g=raster(W,H,'A',[
  [circle(cx,cy,15.3),'C'],
  [circle(cx,cy,14.2),glass],
 ]);
 return str(g);}

// 12. a lattice-top cherry pie on a white plate on a blue tablecloth
function pie(){const W=30,H=30,cx=15,cy=15;
 const fluted=(x,y)=>{const dx=x-cx,dy=y-cy;return Math.hypot(dx,dy)-(12.4+.55*Math.cos(12*Math.atan2(dy,dx)));};
 const lattice=(x,y)=>[-6,0,6].some(k=>Math.abs(x+.5-cx-k)<1.3||Math.abs(y+.5-cy-k)<1.3)?'Y':'R';
 const g=raster(W,H,'B',[
  [circle(cx,cy,14.3),'C'],
  [fluted,'A'],
  [circle(cx,cy,10.4),lattice],
 ]);
 return str(g);}

// 13. a city tram: red skirt, cream upper body with blue windows, dark roof, pantograph and wheels
function tram(){const W=36,H=26;
 const g=raster(W,H,'S',[
  [box(18,24,19,2.1),'E'],
  [box(18,12.6,16,7,1.5),(x,y)=>y<12?'C':'R'],
  [union(...[6,12,18,24,30].map(x=>box(x,9,2,2.3,.5))),'B'],
  [union(box(18,4.6,14.5,1,.5),seg(14,4,18,1.2,.5),seg(22,4,18,1.2,.5),box(18,1,3.2,.5)),'D'],
  [union(...[8,12,24,28].map(x=>circle(x,20.4,1.8))),'D'],
 ]);
 put(g,'C',[[2,15],[2,16],[33,15],[33,16]]);
 return str(g);}

// 14. a wooden abacus on two feet: four rows of beads in red, yellow, green and blue on a cream panel
function abacus(){const W=36,H=27;
 // beads 4 cells wide and 3 tall on a stride of 5, slid to the left and right ends of each rod
 const rows=[[5.5,'R',4,2],[10.5,'Y',3,3],[15.5,'G',5,1],[20.5,'B',2,4]];
 const beads=[];for(const [y,c,left,right] of rows){for(let i=0;i<left;i++)beads.push([4+5*i,y,c]);for(let i=0;i<right;i++)beads.push([32-5*i,y,c]);}
 const g=raster(W,H,'.',[
  [union(box(18,12.5,17.5,11.5,1.2),box(5,25,2.5,1.6,.5),box(31,25,2.5,1.6,.5)),'E'],
  [box(18,12.5,16.2,9.7),'C'],
  [union(...rows.map(([y])=>box(18,y,16.2,.5))),'E'],
  ...beads.map(([x,y,c])=>[box(x,y,2,1.5,.6),c]),
 ]);
 return str(g);}

export default {theme:'batch-0138',pictures:[
 {title:'Бинокль на карте',forLevel:30,family:'Багаж и навигация',direction:null,device:'предмет на своём фоне',subject:'бинокль с тёмными ободами и синими линзами лежит на старой карте',source:'оригинальный',rows:binoculars()},
 {title:'Йети у ледяной пещеры',forLevel:22,family:'Волшебные существа',direction:'Фольклорная фантазия',device:'герой в проёме',subject:'пушистый йети машет лапой у входа в ледяную пещеру снежной ночью',source:'оригинальный',rows:yeti()},
 {title:'Мухоморы на поляне',forLevel:30,family:'Грибы и лесные находки',direction:null,device:'предмет на своём фоне',subject:'большой и маленький мухомор на моховой кочке в траве',source:'оригинальный',rows:agarics()},
 {title:'Яблоня с яблоками',forLevel:30,family:'Деревья, листья и плоды',direction:null,device:'предмет на своём фоне',subject:'яблоня с тёмной каймой кроны, красными яблоками и толстым стволом на лужайке',source:'оригинальный',rows:appleTree()},
 {title:'Коробка конфет-сердце',forLevel:22,family:'Десерты и сладости',direction:null,device:'вместилище с рядами',subject:'коробка конфет в форме сердца: красный борт, золотая бумага, тёмные и белые конфеты',source:'оригинальный',rows:chocolates()},
 {title:'Рубин на бархатной подушке',forLevel:22,family:'Камни, минералы и кристаллы',direction:null,device:'предмет на подставке',subject:'гранёный рубин на бархатной подушке с золотым кантом и кистями',source:'оригинальный',rows:ruby()},
 {title:'Колба с пузырями',forLevel:22,family:'Лаборатории и изобретения',direction:null,device:'вместилище',subject:'круглодонная колба с бурлящей зелёной жидкостью и пробкой на лабораторном столе',source:'оригинальный',rows:flask()},
 {title:'Шут в колпаке с бубенцами',forLevel:30,family:'Маски и костюмы',direction:null,device:'портрет',subject:'улыбчивый шут в трёхрогом колпаке с бубенцами и воротником-жабо',source:'оригинальный',rows:jester()},
 {title:'Грядка в разрезе',forLevel:30,family:'Овощи и урожай',direction:null,device:'разрез',subject:'морковь, свёкла и картошка в земле под своей ботвой',source:'оригинальный',rows:gardenBed()},
 {title:'Пожарный в каске',forLevel:22,family:'Профессии и увлечения',direction:null,device:'портрет',subject:'пожарный в красной каске с золотой эмблемой, с пышными усами и в куртке со светоотражающими полосами',source:'оригинальный',rows:firefighter()},
 {title:'Витражная роза',forLevel:30,family:'Фигуры, знаки и эмблемы',direction:'Геометрическая фантазия',device:'орнамент в рамке',subject:'круглое витражное окно-роза в каменной стене: свинцовые переплёты, синие и сливовые стёкла, красное кольцо, золотая середина',source:'оригинальный',rows:roseWindow()},
 {title:'Вишнёвый пирог',forLevel:22,family:'Хлеб и выпечка',direction:null,device:'вместилище с решёткой',subject:'пирог с вишней под решёткой из теста на белой тарелке, на голубой скатерти',source:'оригинальный',rows:pie()},
 {title:'Трамвай',forLevel:22,family:'Наземный транспорт',direction:null,device:'предмет на своём фоне',subject:'городской трамвай: красный низ, кремовый верх с синими окнами, токоприёмник и колёса',source:'оригинальный',rows:tram()},
 {title:'Счёты',forLevel:22,family:'Книги, учёба и открытия',direction:null,device:'вместилище с рядами',subject:'деревянные счёты на ножках: четыре ряда костяшек — красные, жёлтые, зелёные и синие',source:'оригинальный',rows:abacus()},
]};

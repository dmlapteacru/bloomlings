// Batch 116: four nested pictures for the early levels 4–9 (hive in the sky, lion in the savanna, matryoshka, geode).
import {circle,ellipse,box,poly,union,cut,meet} from '../../../../tools/gameplay-pilot-v9/draw.mjs';
import {raster,str,put} from './helpers.mjs';

// a sprite of rows ('.' transparent) laid on the grid with its top-left cell at (x0,y0)
const sprite=(g,x0,y0,rows)=>rows.forEach((r,y)=>[...r].forEach((c,x)=>{if(c!=='.')g[y0+y][x0+x]=c;}));

// a skep beehive hanging from a branch in the sky, bees in dark outlines flying out of it
function hive(){const W=27,H=22;
 const g=raster(W,H,'S',[
  // branch from the left edge, leaves on it
  [box(7,1.7,7.6,1.1,0.6),'A'],
  [union(ellipse(3.6,3.4,1.9,1.1),ellipse(15.6,1.6,2,1.1)),'G'],
  // hanger
  [box(11,3.2,1,1.2),'A'],
  // the hive: a thick straw rim, the straw, bands inset from the rim
  [meet(ellipse(11,13.4,7.4,9.6),box(11,11,10,8.6)),'E'],
  [meet(ellipse(11,13.4,5.4,7.6),box(11,11,10,7)),'Y'],
  [meet(ellipse(11,13.4,4.4,6.6),box(11,11,10,7)),(x,y,c)=>(y%2===1?'O':c)],
  // entrance with a wooden lip
  [ellipse(11,15.2,3.1,1.9),'A'],
  [ellipse(11,15.5,1.9,1),'D'],
 ]);
 const bee=(x,y)=>sprite(g,x,y,[
  '.DD.DD.',
  'DCCDCCD',
  '.DDDDD.',
  'DYDYDYD',
  'DYDYDYD',
  '.DDDDD.',
 ]);
 bee(19,1);bee(20,8);bee(19,15);
 return str(g);}

// a lion's head in the savanna: sky, grass, mane, face, muzzle
function lion(){const W=24,H=24;
 const g=raster(W,H,'S',[
  [box(12,21,13,3.2),'G'],
  // mane: dark outer ring and orange inner
  [union(circle(12,12.4,9.2),ellipse(12,19.6,6.4,2.6)),'A'],
  [circle(12,12.4,7.6),'O'],
  // ears
  [union(circle(6.2,6,2.4),circle(17.8,6,2.4)),'Y'],
  [union(box(6,5.6,1,1),box(18,5.6,1,1)),'K'],
  // face
  [ellipse(12,12.8,5.7,5.6),'Y'],
  // muzzle
  [union(ellipse(10.5,15.6,2.3,1.8),ellipse(13.5,15.6,2.3,1.8),ellipse(12,16.8,2,1.4)),'C'],
 ]);
 // eyes, nose, mouth
 put(g,'D',[[9,11],[9,12],[14,11],[14,12]]);
 put(g,'E',[[11,14],[12,14],[11,13],[12,13],[10,13],[13,13]]);
 put(g,'D',[[11,16],[12,16]]);
 // rounded ear tips
 put(g,'S',[[4,4],[19,4]]);
 return str(g);}

// a matryoshka doll: dark outline, red headscarf and dress, face, apron with a flower
function doll(){const W=26,H=30;
 const g=raster(W,H,'.',[
  [union(circle(13,8.4,7.6),ellipse(13,19.4,12.6,10.4),box(13,28.4,8.6,1.6,0.5)),'D'],
  [cut(union(circle(13,8.4,6.6),ellipse(13,19.4,11.6,9.4)),box(13,30,13,2.6)),'R'],
  // face oval with hair
  [ellipse(13,8.6,4.8,4.5),'A'],
  [ellipse(13,9.5,4.3,3.6),'C'],
  // apron
  [ellipse(13,20.6,6.8,6.9),'Y'],
  // flower on the apron
  [union(circle(13,17.4,1.7),circle(10.1,20.3,1.7),circle(15.9,20.3,1.7),circle(13,23.2,1.7)),'P'],
  [circle(13,20.3,1.4),'V'],
 ]);
 put(g,'D',[[11,9],[14,9]]);
 put(g,'P',[[10,11],[15,11]]);
 put(g,'G',[[12,25],[13,25],[12,26],[13,26]]);
 return str(g);}

// a geode split open: rough rock rim, cream quartz, blue agate bands, plum crystals in the middle
function geode(){const W=30,H=24;
 const rock=union(ellipse(15,12.4,14.6,11.4),circle(5,7,4.6),circle(24.4,17,5));
 const g=raster(W,H,'.',[
  [rock,'E'],
  [meet(rock,ellipse(13.4,10.6,14,10.2)),'A'],
  [ellipse(15,12.4,11.6,8.8),'C'],
  [ellipse(15,12.4,9.6,7),'S'],
  [ellipse(15,12.4,7.6,5.2),'B'],
  [ellipse(15,12.4,5.6,3.6),'N'],
  [ellipse(15,12.4,3.4,1.7),'V'],
 ]);
 return str(g);}

export default {theme:'batch-0116',pictures:[
 {title:'Улей с пчёлами',forLevel:4,family:'Насекомые и маленькие существа',subject:'плетёный улей висит на ветке в небе, вокруг летают пчёлы',source:'оригинальный',rows:hive()},
 {title:'Голова льва',forLevel:6,family:'Саванна и джунгли',subject:'добродушная морда льва с гривой на фоне саванны и солнца',source:'оригинальный',rows:lion()},
 {title:'Матрёшка',forLevel:7,family:'Игрушки и настольные игры',subject:'матрёшка в красном платке с цветком на фартуке',source:'оригинальный',rows:doll()},
 {title:'Жеода',forLevel:9,family:'Камни, минералы и кристаллы',subject:'расколотая жеода: каменная корка, полосы агата и кристаллы внутри',source:'оригинальный',rows:geode()},
]};

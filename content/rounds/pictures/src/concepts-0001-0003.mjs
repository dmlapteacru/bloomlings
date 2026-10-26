// Concepts for the redraw of P0001–P0003 (the owner rejected the crown, ladybug and bird on 2026-10-26 and asked for
// other subjects). Each one keeps the fixed volumes of rule 17 §4: L1 138 cells 63 / 37 / 38 in three nested layers
// (the outer colour opens first, then the ring, then the middle), L2 240 cells 120 / 97 / 19 / 4, L3 281 cells
// 76 / 70 / 69 / 51 / 13 / 2. The owner picks one per level; it is then redrawn in place with `replace --index k`.
import {circle,ellipse,box,poly,union,cut} from '../../../../tools/gameplay-pilot-v9/draw.mjs';
import {raster,fit,blank,str,ring4,put} from './helpers.mjs';

// L3 boards: 17×17 with the two top corners open (8 cells)
const corners17=[[0,0],[1,0],[2,0],[14,0],[15,0],[16,0],[0,1],[16,1]];
const disc=(r,bg)=>(x,y)=>Math.hypot(x-6.5,y-6.5)-r;

// ---- L1 (13×13 round pictures: the open cells round the circle do not count) ----
// 1A orange slice: cloth K 63, peel and segment lines O 37, flesh Y 38
function orange(){const g=raster(13,13,'.',[[disc(6.3),'K'],[disc(3.9),'Y']]);
 const lines=[[6,6],[6,4],[6,5],[6,7],[6,8],[4,6],[5,6],[7,6],[8,6]];
 fit(g,'Y',38+lines.filter(([x,y])=>g[y][x]==='Y').length,'K');put(g,'O',lines);ring4(g,['Y','O'],'O','K');
 fit(g,'O',37,'K');fit(g,'K',63,'.');return str(g);}
// 1B doughnut: sky B 63, dough A 37 with chocolate sprinkles, pink icing P 38, the hole open
function doughnut(){const g=raster(12,12,'B',[[circle(6,6,4),'P']]);const hole=[[5,5],[6,5],[5,6],[6,6]];
 put(g,'.',hole);const sprinkles=[[4,3],[8,4],[3,7],[7,8]];fit(g,'P',38+sprinkles.length,'B');put(g,'A',sprinkles);
 ring4(g,['P','A'],'A','B');fit(g,'A',37,'B');return str(blank(g,[[0,0],[11,0]]));}
// 1C sunflower: sky S 63, petals Y 37, seed centre E 38
function sunflower(){const g=raster(13,13,'.',[[disc(6.4),'S'],[disc(3.5),'E']]);fit(g,'E',38,'S');ring4(g,['E'],'Y','S');
 const tips=[[6,0],[6,1],[0,6],[1,6],[12,6],[11,6],[6,12],[6,11],[2,2],[10,2],[2,10],[10,10],[3,2],[9,10]];
 put(g,'Y',tips.filter(([x,y])=>g[y][x]==='S'));fit(g,'Y',37,'S');fit(g,'S',63,'.');return str(g);}

// ---- L2 (240 cells, no open cells) ----
// 2A apple: cream C 97, apple R 120, leaf G 19, stem E 4
function apple(){const g=raster(15,16,'C',[
 [cut(union(circle(5.4,9.8,5.4),circle(9.6,9.8,5.4)),circle(7.5,3.6,1.3)),'R'],
 [cut(ellipse(10.8,2.6,2.8,1.3),circle(7.5,3.6,1)),'G']]);
 put(g,'E',[[7,1],[7,2],[7,3],[8,0]]);fit(g,'G',19,'C');fit(g,'R',120,'C');return str(g);}
// 2B watermelon slice (20×12): sky S 97, flesh R 120, rind G 19, seeds D 4
function watermelon(){const half=r=>(x,y)=>y<.6?1:Math.hypot(x-10,y-.6)-r;const g=raster(20,12,'S',[[half(9.7),'G'],[half(8.5),'R']]);
 put(g,'D',[[6,3],[13,3],[9,6],[11,6]]);fit(g,'R',120,'G');fit(g,'G',19,'S');return str(g);}
// 2C balloon: sky S 97, balloon R 120, shine C 19, string D 4 (it leaves the picture at the bottom)
function balloon(){const g=raster(15,16,'S',[[ellipse(7.5,6,6.6,6),'R'],[cut(ellipse(6.6,5.2,4.6,4.6),ellipse(8,6.6,4.6,4.6)),'C']]);
 put(g,'R',[[7,12],[6,12],[8,12]]);put(g,'D',[[7,13],[7,14],[7,15],[8,15]]);
 fit(g,'C',19,'R');fit(g,'R',120,'S');return str(g);}
// 2D rubber duck: water B 97, duck Y 120, beak O 19, eye D 4
function duck(){const g=raster(15,16,'B',[
 [union(ellipse(7.2,11.6,6.6,3.6),circle(8.6,5.6,3.4),poly([[.6,7.6],[3.4,9],[1.4,11.6]])),'Y'],
 [union(ellipse(12.8,6.8,2.2,1),ellipse(12.4,7.6,1.8,.7)),'O']]);
 put(g,'D',[[9,4],[10,4],[9,5],[10,5]]);fit(g,'O',19,'B');fit(g,'Y',120,'B');return str(g);}

// ---- L3 (17×17, eight open corner cells) ----
// 3A owl on a branch at night: owl A 76, night N 70, face and belly C 69, branch and beak E 51, eyes and stars Y 13,
// pupils D 2
function owl(){const g=raster(17,17,'N',[
 [union(ellipse(8.5,8.4,5.8,5.6),poly([[3.2,5],[3.6,1.2],[6.6,3.4]]),poly([[13.8,5],[13.4,1.2],[10.4,3.4]])),'A'],
 [union(ellipse(6.4,6.6,2.4,2.2),ellipse(10.6,6.6,2.4,2.2),ellipse(8.5,11,3.4,2.6)),'C']]);
 blank(g,corners17);
 for(let y=14;y<17;y++)for(let x=0;x<17;x++)g[y][x]='E';
 put(g,'N',[[16,14],[16,15]]);put(g,'E',[[8,8],[8,9]]);
 put(g,'Y',[[5,6],[6,6],[7,6],[5,7],[6,7],[7,7],[9,6],[10,6],[11,6],[9,7],[10,7],[11,7],[2,9]]);
 put(g,'D',[[6,7],[10,7]]);put(g,'Y',[[15,3],[1,11]]);fit(g,'Y',13,'N');
 fit(g,'C',69,'A');fit(g,'A',76,'N');return str(g);}
// 3B frog on a lily pad: frog G 76, pond B 70, pad M 69, belly L 51, cheeks and mouth P 13, pupils D 2
function frog(){const g=raster(17,17,'B',[
 [ellipse(8.5,13.6,8.4,3.2),'M'],
 [union(ellipse(8.5,9.6,6,4.2),ellipse(8.5,6.4,5.4,2.8),circle(5.2,4.4,2),circle(11.8,4.4,2)),'G'],
 [ellipse(8.5,10.6,3.8,2.8),'L'],
 [union(circle(4.6,7.2,1),circle(12.4,7.2,1)),'P']]);
 blank(g,corners17);
 put(g,'P',[[6,7],[7,8],[8,8],[9,8],[10,7]]);put(g,'D',[[5,4],[11,4]]);
 fit(g,'P',13,'G');fit(g,'M',69,'B');fit(g,'L',51,'G');fit(g,'G',76,'B');return str(g);}
// 3C turtle on the grass: shell lines M 76, sky S 70, grass G 69, shell plates L 51, skin Y 13, eye D 2
const turtleRows=['...SSSSSSSSSSS...',
 '.SSSSSSSSSSSSSSS.',
 'SSSSSSSSSSSSSSSSS',
 'SSSSMMMMMMMMSSSSS',
 'SSSMLLLMLLLMLMSSS',
 'SSMLLLMLLLMLLMSSS',
 'SMMMMMMMMMMMMMSSS',
 'SMLLLMLLLMLLLMYYS',
 'MLLMLLLMLLLMLMYDY',
 'MMMMMMMMMMMMMMYDY',
 'MLLLMLLLMLLLMMYYY',
 'MLLMLLLMLLLMLMSGG',
 'MMMMMMMMMMMMMMGGG',
 'GGYYGGGGGGYYGGGGG',
 'GGGGGGGGGGGGGGGGG',
 'GGGGGGGGGGGGGGGGG',
 'GGGGGGGGGGGGGGGGG'];
const turtle=()=>turtleRows;
// 3D rocket over the clouds: space N 76, hull C 70, smoke S 69, nose and fins R 51, flame O 13, stars Y 2 (the
// porthole is space)
const rocketRows=['...NNNNNRNNNNN...',
 '.NYNNNNRRRNNNNNN.',
 'NNNNNNRRRRRNNNNNN',
 'NNNNRRRRRRRRRNNNN',
 'NNNNCCCCCCCCCNNYN',
 'NNNNCCCCCCCCCNNNN',
 'NNNRCCCNNNCCCRNNN',
 'SNNRCCCNNNCCCRNNS',
 'SSSRCCCNNNCCCRNSS',
 'SSSRCCCCCCCCCRSSS',
 'SSRRCCCCCCCCCRRSS',
 'SRRRCCCCCCCCCRRRS',
 'RRRRRCCCCCCCRRRRR',
 'SRRSSSOOOOOSSSRRR',
 'SSSSSSOOOOOSSSSSS',
 'SSSSSSSOOOSSSSSSS',
 'SSSSSSSSSSSSSSSSS'];
const rocket=()=>rocketRows;

export default {theme:'concepts',pictures:[
 {label:'1A',title:'Долька апельсина',rows:orange()},
 {label:'1B',title:'Пончик с глазурью',rows:doughnut()},
 {label:'1C',title:'Подсолнух',rows:sunflower()},
 {label:'2A',title:'Яблоко',rows:apple()},
 {label:'2B',title:'Долька арбуза',rows:watermelon()},
 {label:'2C',title:'Воздушный шар',rows:balloon()},
 {label:'2D',title:'Резиновый утёнок',rows:duck()},
 {label:'3A',title:'Сова на ветке ночью',rows:owl()},
 {label:'3B',title:'Лягушка на кувшинке',rows:frog()},
 {label:'3C',title:'Черепаха на траве',rows:turtle()},
 {label:'3D',title:'Ракета в космосе',rows:rocket()}]};

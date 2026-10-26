// Options for the second redraw of P0001–P0002 (the owner rejected the orange and watermelon slices on 2026-10-26), in
// the style of the owner's reference sheet: one object, flat colour areas, empty space round it. Exact volumes of rule
// 17 §4: L1 138 cells 63 / 37 / 38 in three nested layers (the outer colour opens first, then the middle one, the
// inner one last); L2 240 cells 120 / 97 / 19 / 4.
import {circle,ellipse,box,poly,union,cut,meet} from '../../../../tools/gameplay-pilot-v9/draw.mjs';
import {raster,fit,str,put,ring4,scale} from './helpers.mjs';
// draw(w, h, k, layers): a w×h base design scaled by k
const draw=(w,h,k,layers)=>raster(Math.round(w*k),Math.round(h*k),'.',layers.map(([s,c])=>[scale(k,s),c]));

const petals=(cx,cy,r,n,pr,a0=0)=>union(...Array.from({length:n},(_,i)=>circle(cx+r*Math.cos(a0+i*2*Math.PI/n),cy+r*Math.sin(a0+i*2*Math.PI/n),pr)));

// ---- L1 ----
// 1A sunflower: petals Y 63 (outside), dark ring E 37, seed centre A 38
function sunflower(){const c=7.5,g=raster(15,15,'.',[[union(circle(c,c,5.5),petals(c,c,5.9,8,1.5,Math.PI/8)),'Y'],[circle(c,c,4.85),'E'],[circle(c,c,3.5),'A']]);
 ring4(g,['A'],'E','Y');fit(g,'A',38,'E');fit(g,'E',37,'Y');fit(g,'Y',63,'.');return str(g);}
// 1B fried egg: white C 63 (outside, with a shine on the yolk), yolk rim O 37, yolk Y 38
function friedEgg(){const g=raster(16,15,'.',[[union(circle(8,8,5.6),circle(4.6,9.6,3.4),circle(11.4,6.4,3.4),circle(10.6,10.8,3)),'C'],[circle(7.6,7.4,4.85),'O'],[circle(7.6,7.4,3.55),'Y']]);
 ring4(g,['Y'],'O','C');const shine=[[6,5],[6,6],[7,5]];fit(g,'Y',38+shine.length,'O');put(g,'C',shine);fit(g,'O',37,'C');fit(g,'C',63,'.');return str(g);}
// 1C pizza: crust A 63 (outside), sauce ring and pepperoni R 37, cheese Y 38
function pizza(){const c=7,g=raster(14,14,'.',[[circle(c,c,6.6),'A'],[circle(c,c,4.6),'R'],[circle(c,c,3.7),'Y']]);
 const pep=[[5,5],[6,5],[5,6],[8,7],[9,7],[8,8],[5,9],[6,9]];put(g,'R',pep);ring4(g,['Y','R'],'R','A');
 fit(g,'Y',38,'R');fit(g,'R',37,'A');fit(g,'A',63,'.');return str(g);}

// ---- L2 ----
// 2A penguin: back, head and flippers N 120, white front C 97, beak and feet O 19, eyes D 4
function penguin(){const g=raster(18,22,'.',[
 [union(ellipse(9,13,6.8,7.8),circle(9,5.4,5),ellipse(2.4,12.6,1.8,4.6),ellipse(15.6,12.6,1.8,4.6)),'N'],
 [union(ellipse(9,14.4,4.4,5.8),ellipse(7.2,6,1.9,2.2),ellipse(10.8,6,1.9,2.2)),'C'],
 [union(ellipse(6,21,2.4,1),ellipse(12,21,2.4,1)),'O'],
 [poly([[7.6,7.6],[10.4,7.6],[9,9.8]]),'O']]);
 put(g,'D',[[7,6],[7,7],[10,6],[10,7]]);fit(g,'O',19,'.');fit(g,'C',97,'N');fit(g,'N',120,'.');return str(g);}
// 2B cupcake: pink frosting P 120, wrapper A 97 (with fluting), cherry R 19, sprinkles Y 4
function cupcake(){const g=draw(17,20,1.12,[
 [poly([[2.1,11.4],[14.9,11.4],[13.3,19.8],[3.7,19.8]]),'A'],
 [union(ellipse(8.5,10.4,7.4,2.6),ellipse(8.5,7.6,5.8,2.6),ellipse(8.5,5.2,3.8,2.2)),'P'],
 [circle(8.5,2.2,2.2),'R']]);
 put(g,'Y',[[6,10],[12,10],[8,7],[11,8]]);fit(g,'R',19,'.');fit(g,'A',97,'.');fit(g,'P',120,'.');return str(g);}
// 2C cactus in a pot: cactus G 120, pot O 97, flower P 19, spines C 4
function cactus(){const g=draw(17,22,1.13,[
 [union(box(8.5,9.6,2.6,7,2.4),box(3.4,9,1.4,3.2,1.2),box(5,11.2,2.4,1.1),box(13.6,7.6,1.4,3,1.2),box(12,9.6,2.4,1.1)),'G'],
 [union(poly([[3.4,15.4],[13.6,15.4],[12.4,21.6],[4.6,21.6]]),box(8.5,15.6,5.8,1.2)),'O'],
 [union(circle(8.5,2.4,1.3),circle(6.8,2.9,1.2),circle(10.2,2.9,1.2),circle(8.5,1,1.2)),'P']]);
 put(g,'C',[[8,8],[11,10],[8,12],[11,14]]);fit(g,'P',19,'.');fit(g,'O',97,'.');fit(g,'G',120,'.');return str(g);}

export default {theme:'concepts',pictures:[
 {label:'1A',title:'Подсолнух',forLevel:1,rows:sunflower()},
 {label:'1B',title:'Яичница',forLevel:1,rows:friedEgg()},
 {label:'1C',title:'Пицца',forLevel:1,rows:pizza()},
 {label:'2A',title:'Пингвин',forLevel:2,rows:penguin()},
 {label:'2B',title:'Капкейк',forLevel:2,rows:cupcake()},
 {label:'2C',title:'Кактус в горшке',forLevel:2,rows:cactus()}]};

// Native-grid artwork: broad silhouettes and semantic details, not a picture
// downsampled into arbitrary colours. v6 dimensions and material unlocks stay.
import fs from 'node:fs';
const old=JSON.parse(fs.readFileSync(new URL('../../content/pilot-v6/levels.json',import.meta.url))).levels;
export function makeLevelArt(id){
 const source=old[id-1],{w,h}=source;
 const cells=Array.from({length:h},()=>Array(w).fill(id===2?'G':id===8?'.':'B'));
 const paint=(f,c)=>{for(let y=0;y<h;y++)for(let x=0;x<w;x++)if(f((x+.5)*100/w,(y+.5)*100/h))cells[y][x]=c;};
 const el=(cx,cy,rx,ry,c)=>paint((x,y)=>((x-cx)/rx)**2+((y-cy)/ry)**2<=1,c);
 const box=(x1,y1,x2,y2,c)=>paint((x,y)=>x>=x1&&x<=x2&&y>=y1&&y<=y2,c);
 const poly=(points,c)=>paint((x,y)=>{let hit=false;for(let i=0,j=points.length-1;i<points.length;j=i++){const a=points[i],b=points[j];if((a[1]>y)!==(b[1]>y)&&x<(b[0]-a[0])*(y-a[1])/(b[1]-a[1])+a[0])hit=!hit;}return hit;},c);
 const line=(x1,y1,x2,y2,r,c)=>paint((x,y)=>{const dx=x2-x1,dy=y2-y1,t=Math.max(0,Math.min(1,((x-x1)*dx+(y-y1)*dy)/(dx*dx+dy*dy)));return (x-x1-t*dx)**2+(y-y1-t*dy)**2<=r*r;},c);
 const flower=(x,y,r)=>{for(let k=0;k<5;k++){const a=k*Math.PI*2/5-Math.PI/2;el(x+Math.cos(a)*r*.66,y+Math.sin(a)*r*.66,r*.38,r*.38,'P');}el(x,y,r*.31,r*.31,'C');};
 switch(id){
 case 1: // Pointed leaf, petiole, uninterrupted midrib and two vein pairs.
  poly([[13,91],[21,71],[16,53],[23,33],[39,19],[62,12],[87,8],[85,32],[79,56],[64,73],[41,80],[26,78]],'G');
  poly([[25,70],[22,52],[29,36],[42,25],[65,18],[81,14],[78,34],[72,53],[60,66],[40,74]],'G');
  // Bresenham on the native 16×18 board keeps the midrib connected.
  for(const [start,end]of [[[2,16],[12,3]],[[5,12],[5,8]],[[8,8],[8,5]],[[5,12],[9,12]],[[8,8],[11,8]]]){
   let [x,y]=start;const [xx,yy]=end,dx=Math.abs(xx-x),dy=-Math.abs(yy-y),sx=x<xx?1:-1,sy=y<yy?1:-1;let error=dx+dy;
   for(;;){cells[y][x]='A';if(x===xx&&y===yy)break;const e=2*error;if(e>=dy){error+=dy;x+=sx;}if(e<=dx){error+=dx;y+=sy;}}
  }break;
 case 2: // One unmistakable water silhouette; a small face, no flower overlay.
  poly([[50,5],[43,23],[31,39],[20,54],[14,67],[15,78],[23,90],[37,96],[63,96],[77,90],[85,78],[86,67],[80,54],[69,39],[57,23]],'B');
  poly([[50,11],[47,25],[34,43],[25,57],[20,69],[21,78],[28,86],[39,91],[61,91],[72,86],[79,78],[80,69],[75,57],[65,43],[54,25]],'B');
  line(32,52,26,66,2.8,'G');el(36,71,3,4,'A');el(64,71,3,4,'A');
  line(44,81,50,84,1.7,'A');line(50,84,56,81,1.7,'A');el(26,78,4.5,3,'P');el(74,78,4.5,3,'P');break;
 case 3: // Five individually readable petals, a large centre and two leaves.
  line(50,46,50,91,3.2,'G');
  poly([[48,79],[35,64],[17,64],[24,77],[38,82]],'G');poly([[52,87],[63,71],[83,69],[77,82],[63,89]],'G');
  for(const [x,y]of [[50,12],[69,23],[69,45],[50,56],[31,45],[31,23]])el(x,y,12,12,'P');el(50,34,10,10,'C');el(50,34,3.5,3.5,'A');break;
 case 4: // A narrow body separates scalloped upper and lower wings.
  el(26,33,21,27,'D');el(74,33,21,27,'D');el(30,71,18,22,'D');el(70,71,18,22,'D');
  el(26,33,17,23,'P');el(74,33,17,23,'P');el(30,71,14,18,'P');el(70,71,14,18,'P');
  el(25,30,9,13,'C');el(75,30,9,13,'C');el(29,72,7,9,'G');el(71,72,7,9,'G');
  el(50,52,5,32,'D');line(48,24,40,9,2,'D');line(52,24,60,9,2,'D');el(50,22,5,5,'A');break;
 case 5: // A squat acorn cup, warm nut and pointed tip. No pink cap stripe.
  poly([[23,40],[77,40],[74,69],[65,83],[50,94],[35,83],[26,69]],'D');
  poly([[28,43],[72,43],[69,68],[61,79],[50,88],[39,79],[31,68]],'A');
  line(36,51,38,68,3,'C');
  line(49,25,46,9,3,'D');line(49,24,47,10,1.7,'A');
  poly([[53,17],[60,7],[79,6],[73,19],[61,23]],'G');
  el(50,35,35,16,'D');el(50,34,30,12,'A');box(18,34,82,43,'D');box(22,34,78,39,'A');
  line(32,24,37,32,1.5,'C');line(46,23,51,32,1.5,'C');line(60,25,65,32,1.5,'C');
  // Small fallen blossom, separate from the acorn's identifying geometry.
  el(84,87,8,6,'P');el(84,87,2.5,2.5,'C');break;
 case 6: // Cream spots and thick cream stem, recognisable at a glance.
  poly([[39,46],[61,46],[60,70],[69,87],[63,92],[36,92],[31,86],[40,69]],'D');
  poly([[44,49],[56,49],[55,71],[62,86],[39,86],[45,70]],'C');box(48,66,54,84,'A');
  paint((x,y)=>y<=55&&((x-50)/44)**2+((y-55)/44)**2<=1,'D');
  paint((x,y)=>y<=50&&((x-50)/39)**2+((y-53)/37)**2<=1,'P');
  el(29,37,6,6,'C');el(55,24,7,7,'C');el(73,41,6,5,'C');
  poly([[13,91],[19,79],[24,89],[32,82],[31,93]],'G');break;
 case 7: // Heart-shaped berry with a five-point calyx and cream seeds.
  poly([[10,33],[15,24],[29,18],[43,20],[50,26],[57,20],[72,18],[85,24],[90,34],[87,54],[77,72],[61,88],[50,95],[39,88],[23,72],[13,54]],'D');
  poly([[15,34],[20,28],[30,23],[43,25],[50,31],[58,25],[72,23],[81,28],[85,35],[82,52],[72,69],[58,84],[50,90],[42,84],[28,69],[18,52]],'P');
  line(50,10,54,4,2,'A');poly([[50,12],[35,7],[38,22],[20,18],[31,32],[43,29],[50,39],[57,29],[70,32],[81,18],[62,22],[66,7]],'G');
  for(const [x,y]of [[30,43],[52,47],[73,43],[37,63],[63,63],[50,78]])el(x,y,2.7,3.3,'C');break;
 case 8: // Curved cup, hollow handle and large five-petal printed emblem.
  el(76,51,19,20,'D');el(76,51,15,16,'A');el(76,51,10,11,'.');
  box(14,27,70,69,'D');el(42,68,28,17,'D');box(19,31,65,68,'C');el(42,68,23,12,'C');
  el(42,29,29,7,'A');el(42,28,23,3,'D');
  flower(42,54,15);line(31,17,34,9,2,'B');line(48,17,51,7,2,'B');
  el(42,88,34,4,'G');break;
 case 9: // A readable folded envelope with a heart seal and leaf sprig.
  box(9,29,91,85,'D');box(13,33,87,81,'C');
  line(14,79,44,54,1.8,'A');line(86,79,56,54,1.8,'A');
  poly([[13,33],[50,62],[87,33]],'A');poly([[18,33],[50,56],[82,33]],'C');
  el(45,58,6,6,'P');el(55,58,6,6,'P');poly([[39,58],[61,58],[50,72]],'P');
  line(66,25,83,10,1.8,'A');poly([[71,20],[64,14],[67,5],[76,12]],'G');poly([[77,17],[82,6],[94,8],[88,17]],'G');break;
 default:throw Error('v7 has no replacement for L'+id);
 }
 const art={...source,rowsBottomFirst:cells.map(r=>r.join('')).reverse()};delete art.pods;delete art.tray;delete art.difficulty;delete art.capacity;
 const allowed=id===1?'GBA':id===2?'GPBA':id===3?'GPBAC':'GPBACD';
 for(const c of art.rowsBottomFirst.join(''))if(c!=='.'&&!allowed.includes(c))throw Error(`L${id}: unexpected material ${c}`);
 return art;
}

// v8 uses two new semantic colours, not extra lighting shades.
import fs from 'node:fs';
const old=JSON.parse(fs.readFileSync(new URL('../../content/pilot-v7/levels.json',import.meta.url))).levels;
export function makeLevelArt(id){
 const original=old[id-1],{w,h}=original,art=structuredClone(original);for(const k of ['pods','tray','capacity','difficulty'])delete art[k];
 let cells=original.rowsBottomFirst.slice().reverse().map(r=>r.split(''));
 const paint=(f,c)=>{for(let y=0;y<h;y++)for(let x=0;x<w;x++)if(f((x+.5)*100/w,(y+.5)*100/h))cells[y][x]=c;};
 const el=(cx,cy,rx,ry,c)=>paint((x,y)=>((x-cx)/rx)**2+((y-cy)/ry)**2<=1,c);
 const box=(x1,y1,x2,y2,c)=>paint((x,y)=>x>=x1&&x<=x2&&y>=y1&&y<=y2,c);
 const poly=(ps,c)=>paint((x,y)=>{let hit=false;for(let i=0,j=ps.length-1;i<ps.length;j=i++){const a=ps[i],b=ps[j];if((a[1]>y)!==(b[1]>y)&&x<(b[0]-a[0])*(y-a[1])/(b[1]-a[1])+a[0])hit=!hit;}return hit;},c);
 const line=(x1,y1,x2,y2,r,c)=>paint((x,y)=>{const dx=x2-x1,dy=y2-y1,t=Math.max(0,Math.min(1,((x-x1)*dx+(y-y1)*dy)/(dx*dx+dy*dy)));return (x-x1-t*dx)**2+(y-y1-t*dy)**2<=r*r;},c);
 const replace=(from,to,test=()=>true)=>{for(let y=0;y<h;y++)for(let x=0;x<w;x++)if(cells[y][x]===from&&test((x+.5)*100/w,(y+.5)*100/h))cells[y][x]=to;};
 if(id===4){replace('C','Y');el(50,22,2.5,2.5,'C');}
 if(id===5){replace('A','O',(x,y)=>y>44);replace('C','Y',(x,y)=>y<44);}
 if(id===14){replace('P','Y');}
 if(id===15){
  art.subject='bee';art.title='Пчёлка над цветком';cells=Array.from({length:h},()=>Array(w).fill('B'));
  // Broad paired wings, striped abdomen and a separately readable head.
  el(40,29,16,19,'D');el(64,29,15,19,'D');el(40,29,12,15,'C');el(64,29,11,15,'C');
  el(54,53,29,20,'D');el(54,53,25,16,'Y');
  paint((x,y)=>((x-54)/25)**2+((y-53)/16)**2<=1&&((x>49&&x<56)||(x>65&&x<72)),'D');
  poly([[78,49],[92,55],[78,59]],'D');
  el(28,47,17,18,'D');el(28,47,13,14,'Y');el(23,44,4,5,'D');el(23,42,1.8,2,'C');
  line(23,31,17,20,2,'A');line(32,31,37,19,2,'A');el(16,19,3,3,'D');el(38,18,3,3,'D');
  line(42,68,38,77,2,'A');line(60,69,65,77,2,'A');
  line(54,86,54,98,2,'G');poly([[53,96],[40,90],[39,95],[52,99]],'G');
  for(let k=0;k<5;k++){const a=k*Math.PI*2/5;el(54+Math.cos(a)*9,86+Math.sin(a)*6,5,4,'P');}el(54,86,4,4,'O');
 }
 if(id===16){replace('.','B');replace('C','Y');replace('V','O');}
 if(id===17){
  cells=Array.from({length:h},()=>Array(w).fill('B'));
  // Arched handle above/right, wide can and long diagonal spout to a shower rose.
  el(71,44,23,28,'D');el(71,44,18,23,'O');el(71,44,12,17,'B');
  poly([[38,67],[28,70],[12,37],[20,33]],'D');poly([[36,63],[30,64],[17,38],[20,37]],'G');
  poly([[6,32],[19,25],[25,35],[10,43]],'D');poly([[9,33],[18,29],[21,34],[12,39]],'C');
  el(12,33,1.8,1.8,'D');el(17,36,1.8,1.8,'D');
  box(32,38,74,80,'D');el(53,80,21,9,'D');box(37,42,69,79,'G');el(53,79,16,6,'G');
  el(53,39,21,6,'O');el(53,38,13,3,'D');line(40,49,40,69,2,'C');
  for(let k=0;k<5;k++){const a=k*Math.PI*2/5;el(55+Math.cos(a)*7,62+Math.sin(a)*7,4,4,'Y');}el(55,62,3,3,'P');
 }
 if(id===19)replace('A','O');
 if(id===23){replace('.','B');replace('V','Y');replace('P','O',(x,y)=>x>61);}
 if(id===26){replace('A','Y');el(50,35,11,11,'A');el(46,31,3,4,'C');}
 if(id===28)replace('.','B');
 art.rowsBottomFirst=cells.map(r=>r.join('')).reverse();return art;
}

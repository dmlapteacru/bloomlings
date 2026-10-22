// v8C: scenes instead of a lone icon on plain sky. Each picture keeps its size and entry, and some colours
// appear at a second depth (ground and subject, clouds and spots), so a capsule can finish its outer part
// and then wait for the inner one.
import fs from 'node:fs';
const base=JSON.parse(fs.readFileSync(new URL('../../content/pilot-v8/levels.json',import.meta.url))).levels;
export const NEW_ART=new Set([6,8,11,13,16,28]);
export function makeLevelArt(id){
 const original=base[id-1],{w,h}=original,art=structuredClone(original);for(const k of ['pods','tray','capacity','difficulty'])delete art[k];
 if(!NEW_ART.has(id))return art;
 const top=original.rowsBottomFirst.slice().reverse(),cells=top.map(r=>r.split(''));
 // A sprite: rows of characters placed with its top-left at (x, y); '.' keeps the cell underneath.
 const put=(x,y,rows)=>rows.forEach((row,j)=>[...row].forEach((c,i)=>{if(c!=='.'&&x+i>=0&&x+i<w&&y+j>=0&&y+j<h)cells[y+j][x+i]=c;}));
 const each=f=>{for(let y=0;y<h;y++)for(let x=0;x<w;x++)f(x,y);};
 const sun=(x,y)=>put(x,y,['.YYY.','YYOYY','YOOOY','YYOYY','.YYY.']);
 const cloud=(x,y,c='C')=>put(x,y,['..CCC...','.CCCCCC.','CCCCCCCC'].map(r=>r.replaceAll('C',c)));
 const flower=(x,y,petal='Y',middle='O')=>put(x,y,['.'+petal+'.',petal+middle+petal,'.G.']);
 if(id===6){ // forest clearing: sky, clouds, sun, grass with flowers, a small mushroom beside the big one
  art.title='Грибы на поляне';
  each((x,y)=>{if(cells[y][x]==='G')cells[y][x]='B';if(y>=20&&cells[y][x]==='B')cells[y][x]='G';});
  cloud(1,0);sun(18,0);
  put(1,16,['.DDDD.','DPPCPD','DDDDDD','.DCCD.','.DCCD.']);
  flower(19,20);flower(5,22,'P','Y');flower(20,23,'P','Y');
 }
 if(id===8){ // breakfast: checked cloth on a table edge, a cookie beside the mug
  art.title='Чашка на завтраке';
  each((x,y)=>{if(cells[y][x]==='G')cells[y][x]='.';});
  each((x,y)=>{if(y===h-4)cells[y][x]='D';else if(y>h-4)cells[y][x]=((x>>1)+(y>>1))%2?'P':'C';});
  put(18,18,['.DDD.','DADAD','DAAAD','.DDD.']);
 }
 if(id===11){ // turtle in a meadow: sky, sun, a cloud, grass with flowers, a small pond with a lily pad
  art.title='Черепаха у пруда';
  each((x,y)=>{if(cells[y][x]==='C')cells[y][x]='Y';if(cells[y][x]==='B'&&y>=h-7)cells[y][x]='G';});
  sun(1,1);cloud(14,1);
  put(16,22,['GDDDDDDG','DBBBGGBD','DBBPBBBD','GDDDDDDG']);
  flower(0,23,'P','Y');flower(10,22,'Y','O');
 }
 if(id===13){ // rain: violet clouds, drops, yellow stripes on the canopy, a puddle with ripples
  art.title='Зонтик под дождём';
  each((x,y)=>{if(cells[y][x]==='P'&&y>=4&&y<=14&&(x===8||x===18))cells[y][x]='Y';});
  cloud(1,0,'V');cloud(17,0,'V');
  for(const [x,y] of [[1,5],[0,9],[1,13],[25,5],[24,9],[25,13],[3,17],[22,17],[5,21],[20,21],[2,24],[23,24]])cells[y][x]='B';
  put(1,26,['..BBBBB.......BBBBBBBBB..','.BBCCBBB.....BBBCCBBBCBB.']);
 }
 if(id===16){ // kite day: a cloud, sun, a green hill with a path and flowers, the string down to the hill
  art.title='Змей над холмом';
  each((x,y)=>{if(y>=19&&['G','A'].includes(cells[y][x]))cells[y][x]='B';});
  each((x,y)=>{const hill=21-Math.round(3*Math.exp(-(((x-14)/7)**2)));if(y>=hill&&cells[y][x]==='B')cells[y][x]='G';});
  cloud(1,1);sun(18,0);
  for(const [x,y] of [[12,18],[12,19],[13,20],[13,21]])cells[y][x]='D';
  for(const [x,y] of [[14,22],[15,23],[15,24],[16,25]])cells[y][x]='A';
  flower(3,21,'P','Y');flower(19,22,'Y','O');flower(8,23,'P','Y');
 }
 if(id===28){ // the apple on a wooden table, a cloud in the sky
  art.title='Яблоко на столе';
  each((x,y)=>{if(y>=h-2&&cells[y][x]==='B')cells[y][x]=y===h-2?'D':'A';});each((x,y)=>{if(y===h-1)cells[y][x]='A';});
  cloud(1,1);
 }
 art.rowsBottomFirst=cells.map(r=>r.join('')).reverse();
 const grid=art.rowsBottomFirst.join('');for(const e of art.entries)if(grid[e]==='#')throw Error('Entry blocked L'+id);
 return art;
}

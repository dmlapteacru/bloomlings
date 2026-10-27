// How well a picture can carry a round (the owner, 2026-10-27: "подходят ли нам картинки … все 6 цветов видны сразу"):
// the structure of access from the open ring, measured on the board the level would use, and, where the picture was
// built, what the generator achieved from it. A picture where almost every colour is open at the start and the work
// lies in one or two shallow layers cannot make an interesting queue: the random test stays near 100 % whatever is done.
// node tools/rounds/fitness.mjs [ids…|--approved] [--out file.md]
import fs from 'node:fs';import path from 'node:path';
import {CODES,BANK,manifest,picture,measure,boardOf,readJSON,arg,flag} from './lib.mjs';
// access depth per cell: how many colour cells must be cleared before it (0 = on the open front)
export function access(pic){const b=boardOf(pic),{w,h,rowsBottomFirst:rows,entries}=b,sites=new Set((b.hero?b.hero.choices:[]).map(c=>c.cell));
 const g=[...rows.join('')].map((c,i)=>sites.has(i)?'.':c),d=new Float64Array(w*h).fill(Infinity),dq=[];for(const e of entries){d[e]=0;dq.push(e);}
 while(dq.length){const c=dq.shift(),x=c%w,y=Math.floor(c/w);for(const [dx,dy] of [[1,0],[-1,0],[0,1],[0,-1]]){const u=x+dx,v=y+dy;if(u<0||v<0||u>=w||v>=h)continue;const n=v*w+u;if(g[n]==='#')continue;
  const cost=CODES.includes(g[c])?1:0,nd=d[c]+cost;if(nd<d[n]){d[n]=nd;cost?dq.push(n):dq.unshift(n);}}}
 const cells=[];for(let i=0;i<g.length;i++)if(CODES.includes(g[i]))cells.push({c:g[i],d:d[i]});return cells;}
export function fitness(pic){const cells=access(pic),N=cells.length,cols=[...new Set(cells.map(x=>x.c))],maxD=Math.max(...cells.map(x=>x.d));
 const front=new Set(cells.filter(x=>x.d===0).map(x=>x.c)),open=cells.filter(x=>x.d===0).length;
 // a colour is "hidden at the start" when none of its cells is on the front; its first cell lies that many layers deep
 const first=Object.fromEntries(cols.map(c=>[c,Math.min(...cells.filter(x=>x.c===c).map(x=>x.d))]));
 const shallow=cells.filter(x=>x.d<=2).length/N;
 return {N,colours:cols.length,visible:front.size,hiddenColours:cols.filter(c=>first[c]>0).length,openShare:open/N,shallowShare:shallow,layers:maxD,
  deepWork:cells.filter(x=>first[x.c]>=2).length/N};}
// what the bank says: the lowest Normal share and whether Hard / Super Hard came out
function achieved(id){const b=readJSON(path.join(BANK,id+'.json'));if(!b)return null;const cs=b.candidates.concat((b.placed||[]).map(()=>null)).filter(Boolean);
 const nor=cs.filter(c=>c.cls==='Normal'&&!c.intro).map(c=>c.random.share);return {normalMin:nor.length?Math.min(...nor):null,hard:cs.some(c=>c.cls==='Hard'),superHard:cs.some(c=>c.cls==='Super Hard')};}
// the command line part runs only when this file is the script (fitness() is imported by other tools)
if(import.meta.url===new URL(process.argv[1],'file://'+process.cwd()+'/').href){
const ids=flag('--approved')?manifest().pictures.filter(p=>p.status==='approved').map(p=>p.id):process.argv.slice(2).filter(a=>/^P\d+$/.test(a));
const rows=[];for(const id of ids){const p=picture(id),f=fitness(p),a=achieved(id);
  // measured on the 111 approved pictures (2026-10-27, potential.mjs): with ≤1 colour hidden at the start or ≥6 colours
 // visible, 34 of 37 stayed near 100 % whatever the queue; with ≥4 hidden, 36 of 42 gave Hard or Super Hard
 const weak=f.hiddenColours<=1||f.visible>=6,strong=f.hiddenColours>=4;
 rows.push({id,title:p.title,...f,a,verdict:weak?'слабая':strong?'сильная':'средняя'});}
const pct=x=>Math.round(x*100)+' %';
const lines=['| Картинка | Клетки / цвета | Видно цветов сразу | Скрыто цветов | Работа на фронте | Работа ≤2 слоёв | Слоёв | Оценка | Банк: мин. Normal · Hard · SH |','|---|---|---|---|---|---|---|---|---|'];
for(const r of rows)lines.push(`| ${r.id} «${r.title}» | ${r.N} / ${r.colours} | ${r.visible} | ${r.hiddenColours} | ${pct(r.openShare)} | ${pct(r.shallowShare)} | ${r.layers} | ${r.verdict} | ${r.a?`${r.a.normalMin===null?'—':pct(r.a.normalMin)} · ${r.a.hard?'да':'нет'} · ${r.a.superHard?'да':'нет'}`:'не собиралась'} |`);
const sum=v=>rows.filter(r=>r.verdict===v).length;lines.push('',`Итого: сильных ${sum('сильная')}, средних ${sum('средняя')}, слабых ${sum('слабая')} из ${rows.length}.`);
const text=lines.join('\n')+'\n';if(arg('--out'))fs.writeFileSync(arg('--out'),text);console.log(text);
}

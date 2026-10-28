// The automatic screen of pictures before the owner's contact sheet (17 §13, the owner's decision of 2026-10-27: every
// picture is still approved on the sheet, but only screened ones reach it). Each picture gets:
//   rules  — the §2 check of pictures.mjs (stage range, colours, cut-off cells, strips of any direction with two bridge
//     sites whose anchors resolve to colour cells on the near bank, §8); a failure stops it;
//   strength — colours hidden at the start (fitness.mjs): weak pictures carry introductions only, strong ones Hard;
//   potential — a quick queue test with no mechanics: can it give a Normal of 50–75 % and a Hard under 50 %;
//   readability — tiny patches (cells of a colour cut off in pieces under 3 cells; over 7 % of the cells, the top tenth
//     of the 116 approved pictures of 2026-10-27), neighbouring colours too close
//     (ΔE under 15 along a long shared edge), a colour of under 8 cells;
//   similarity — the closest approved picture by a 12×12 reduction (share of equal reduced cells);
//   motifs — a motif the pool already has 3 or more times.
// node tools/rounds/screen.mjs <batch.mjs | ids… | --status draft> [--level N] [--games 20] [--no-potential] [--out report.md]
//   a module is screened straight from its matrices (before render); --level sets the stage checked (default: each
//   picture's forLevel, else 30). Exit code 1 when a picture fails.
import fs from 'node:fs';import path from 'node:path';import {pathToFileURL} from 'node:url';
import {CODES,PALETTE,manifest,picture,measure,boardOf,columnsFor,arg,flag,motifsOf,rgbOf} from './lib.mjs';
import {fitness} from './fitness.mjs';
process.argv.splice(2,0,'noop');const {check}=await import('./pictures.mjs');const {splitPods,candidate}=await import('./levels.mjs');process.argv.splice(2,1);
import {quickShare} from './measure.mjs';

const lab=hex=>{const c=rgbOf(hex).map(v=>{v/=255;return v<=.04045?v/12.92:((v+.055)/1.055)**2.4;});
 const x=(c[0]*.4124+c[1]*.3576+c[2]*.1805)/.9505,y=c[0]*.2126+c[1]*.7152+c[2]*.0722,z=(c[0]*.0193+c[1]*.1192+c[2]*.9505)/1.089;
 const f=t=>t>.008856?Math.cbrt(t):7.787*t+16/116;return [116*f(y)-16,500*(f(x)-f(y)),200*(f(y)-f(z))];};
const LAB=Object.fromEntries([...CODES].map(c=>[c,lab(PALETTE[c].color)]));
const dE=(a,b)=>Math.hypot(LAB[a][0]-LAB[b][0],LAB[a][1]-LAB[b][1],LAB[a][2]-LAB[b][2]);

// readability: patches per colour, close neighbours, tiny colours
export function readability(p){const H=p.rows.length,W=p.rows[0].length,at=(x,y)=>x<0||y<0||x>=W||y>=H?'.':p.rows[y][x],seen=new Uint8Array(W*H),tiny={},edges={};
 for(let y=0;y<H;y++)for(let x=0;x<W;x++){const c=at(x,y);if(!CODES.includes(c))continue;
  for(const [dx,dy] of [[1,0],[0,1]]){const d=at(x+dx,y+dy);if(CODES.includes(d)&&d!==c){const k=[c,d].sort().join('');edges[k]=(edges[k]||0)+1;}}
  if(seen[y*W+x])continue;let n=0;const q=[[x,y]];seen[y*W+x]=1;while(q.length){const [u,v]=q.pop();n++;for(const [dx,dy] of [[1,0],[-1,0],[0,1],[0,-1]]){const a=u+dx,b=v+dy;if(a<0||b<0||a>=W||b>=H||seen[b*W+a]||at(a,b)!==c)continue;seen[b*W+a]=1;q.push([a,b]);}}
  if(n<3)tiny[c]=(tiny[c]||0)+n;}
 const m=measure(p),tinyCells=Object.values(tiny).reduce((a,b)=>a+b,0),close=Object.entries(edges).filter(([k,n])=>dE(k[0],k[1])<15&&n>=Math.max(6,.01*m.N)).map(([k,n])=>`${PALETTE[k[0]].name}/${PALETTE[k[1]].name} ΔE ${dE(k[0],k[1]).toFixed(0)} (${n} гр.)`);
 const small=Object.entries(m.volumes).filter(([,v])=>v<8).map(([c,v])=>`${PALETTE[c].name} ${v}`);
 return {tinyShare:tinyCells/m.N,tinyCells,close,small};}

// a 12×12 reduction: each block's most common cell ('.' counts), for the similarity to approved pictures
const reduce=p=>{const H=p.rows.length,W=p.rows[0].length,G=12,out=[];for(let j=0;j<G;j++)for(let i=0;i<G;i++){const cnt={};
 for(let y=Math.floor(j*H/G);y<Math.max(Math.floor(j*H/G)+1,Math.floor((j+1)*H/G));y++)for(let x=Math.floor(i*W/G);x<Math.max(Math.floor(i*W/G)+1,Math.floor((i+1)*W/G));x++){const c=p.rows[y][x];cnt[c]=(cnt[c]||0)+1;}
 out.push(Object.entries(cnt).sort((a,b)=>b[1]-a[1])[0][0]);}return out;};
const sim=(a,b)=>{let s=0;for(let i=0;i<a.length;i++){if(a[i]===b[i])s++;else if((a[i]==='.')===(b[i]==='.'))s+=.5;}return s/a.length;};

// potential: four Normal and four Hard queues without mechanics (a picture with a strip of any direction: with its bridge
// group only), a quick random test of each
async function potential(p,games){const m=measure(p),board=boardOf(p),base=splitPods(m.volumes,m.N,null);if(base.error)return {error:base.error};const out={Normal:[],Hard:[]};
 for(let k=0;k<8;k++){const cls=k<4?'Normal':'Hard',c=candidate(p,board,base.pods,columnsFor(m.N)[k%columnsFor(m.N).length],board.hero?'bridge':'none',cls,k*7919+303,false);if(c.error)continue;out[cls].push(await quickShare(c.level,games));}
 return {normalZone:out.Normal.concat(out.Hard).some(s=>s>=.5&&s<=.75),hard:out.Hard.some(s=>s>=.15&&s<.5),shares:out};}

const levelArg=arg('--level')?+arg('--level'):null,games=+arg('--games',20);
const mod=process.argv.slice(2).find(a=>a.endsWith('.mjs'));let pics;
if(mod){const b=(await import(pathToFileURL(path.resolve(mod)).href)).default;pics=b.pictures.map((p,i)=>({id:'#'+(i+1),title:p.title,subject:p.subject,family:p.family,direction:p.direction,source:p.source,reference:p.reference,forLevel:levelArg||p.forLevel||30,rows:p.rows,w:p.rows[0].length,h:p.rows.length}));}
else{const st=arg('--status'),ids=st?manifest().pictures.filter(e=>e.status===st).map(e=>e.id):process.argv.slice(2).filter(a=>/^P\d+$/.test(a));pics=ids.map(id=>{const p=picture(id);return levelArg?{...p,forLevel:levelArg}:p;});}
const pool=manifest().pictures.filter(e=>e.status==='approved').map(e=>picture(e.id)),poolRed=pool.map(p=>({id:p.id,title:p.title,r:reduce(p)})),motifCount={};
for(const p of pool)for(const m of motifsOf(p))motifCount[m]=(motifCount[m]||0)+1;
const rows=[];let failed=0;
for(const p of pics){const rc=check(p),f=fitness(p),weak=f.hiddenColours<=1||f.visible>=6,strong=f.hiddenColours>=3&&!weak,rd=readability(p),red=reduce(p);
 const near=poolRed.filter(x=>x.id!==p.id).map(x=>({...x,s:sim(red,x.r)})).sort((a,b)=>b.s-a.s)[0],motifs=motifsOf(p).filter(m=>(motifCount[m]||0)-(pool.some(x=>x.id===p.id)?1:0)>=3);
 const pot=rc.ok&&!flag('--no-potential')?await potential(p,games):null,stop=[],warn=[];
 if(!rc.ok)stop.push(...rc.issues);if(rd.tinyShare>.07)warn.push(`мелкие обрывки ${rd.tinyCells} кл. (${Math.round(rd.tinyShare*100)} %)`);if(rd.close.length)warn.push('близкие соседи: '+rd.close.join(', '));if(rd.small.length)warn.push('маленькие цвета: '+rd.small.join(', '));
 if(near&&near.s>=.85)warn.push(`похожа на ${near.id} «${near.title}» (${Math.round(near.s*100)} %)`);if(motifs.length)warn.push('мотив уже 3+ раз: '+motifs.join(', '));
 if(pot&&!pot.error&&!pot.normalZone&&!pot.hard)warn.push('быстрый замер: без механик очередь не дала ни Normal 50–75 %, ни Hard (подсказка, не приговор)');
 if(stop.length)failed++;
 rows.push({id:p.id,title:p.title,N:measure(p).N,colours:measure(p).colours,strength:weak?'слабая':strong?'сильная':'средняя',hidden:f.hiddenColours,visible:f.visible,pot,near,verdict:stop.length?'НЕТ':warn.length?'внимание':'OK',stop,warn});}
const lines=[`# Отсев картинок (${new Date().toISOString().slice(0,10)})`,'','Скрипт `tools/rounds/screen.mjs` (17 §13). НЕТ — не идёт на лист; «внимание» — идёт на лист с пометкой.','',
 '| Картинка | Клетки / цвета | Сила (скрыто / видно) | Очередь: Normal 50–75 / Hard | Ближайшая утверждённая | Итог | Причины |','|---|---|---|---|---|---|---|',
 ...rows.map(r=>`| ${r.id} «${r.title}» | ${r.N} / ${r.colours} | ${r.strength} (${r.hidden} / ${r.visible}) | ${r.pot?(r.pot.error?r.pot.error:`${r.pot.normalZone?'да':'нет'} / ${r.pot.hard?'да':'нет'}`):'—'} | ${r.near?`${r.near.id} ${Math.round(r.near.s*100)} %`:'—'} | ${r.verdict} | ${[...r.stop,...r.warn].join('; ')||'—'} |`),'',
 `Итог: ${rows.filter(r=>r.verdict==='OK').length} OK, ${rows.filter(r=>r.verdict==='внимание').length} с вниманием, ${failed} НЕТ.`,''];
const text=lines.join('\n');if(arg('--out'))fs.writeFileSync(arg('--out'),text);console.log(text);process.exitCode=failed?1:0;

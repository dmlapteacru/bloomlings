// Audit of placed round levels against product/17_LEVEL_GENERATION_RULES.md: everything is recomputed from the level
// file and the picture, nothing is taken on trust from the builder. Mandatory rules (НЕТ fails the level) are kept
// apart from the soft targets of §5, §10 and §12 (замечания).
// node tools/rounds/audit.mjs <from> <to> [--out <file.md>] [--preview <folder>]
//   --preview: a folder of L####.json for positions content/rounds/levels/ does not fill (draft pictures).
import fs from 'node:fs';import path from 'node:path';
import {LEVELS,CODES,readJSON,lid,arg,picture,measure,fitsStage,stageOf,budget,columnsFor,slotOf,SUPPORTED,FIXED_VOLUMES,motifsOf,boardOf} from './lib.mjs';
import {engine} from './engine.mjs';
import {run} from '../level-stats/trace.mjs';
import {bridgeChoicesWin,openingTest,forcedShare,holdPod,hiddenMatters,bridgeSingle} from './measure.mjs';
import {INTRO} from './lib.mjs';
import {fitness} from './fitness.mjs';
const from=+process.argv[2]||1,to=+process.argv[3]||20,preview=arg('--preview'),out=arg('--out');
const E=await engine(),{R}=E,FIX={1:[[26,26,26,25,25],[16,16,16],[5]],2:[[40,40,40],[33,32,32],[10,9],[4]],3:[[26,25,25],[24,23,23],[35,34],[26,25],[13],[2]]};
const rows=[],levels={};
for(let n=from;n<=to;n++){const L=readJSON(path.join(LEVELS,lid(n)+'.json'))||(preview&&readJSON(path.join(preview,lid(n)+'.json')));levels[n]=L||null;}
const typesOf=L=>(L.recipe||'none')==='none'?[]:L.recipe.split('+');
for(let n=from;n<=to;n++){const L=levels[n],bad=[],soft=[];
 if(!L){rows.push({n,missing:true});continue;}
 const d=L.definition,pic=picture(L.picture),slot=slotOf(n),pods=d.pods,P=pods.length,types=typesOf(L);
 // picture (§2, §13.1): approved, the stage range, locked pictures only on L1–3; the matrix on the board is the picture's
 if(!pic)bad.push('нет картинки');else{
  if(pic.status!=='approved')bad.push(`картинка ${L.picture} не утверждена (${pic.status})`);
  const m=measure(pic);if(!fitsStage(m,n))bad.push(`картинка вне диапазона этапа §2 (${m.N} кл., ${m.colours} цв.; ${JSON.stringify(stageOf(n))})`);
  if((pic.locked||null)!==(n<=3?n:null))bad.push('закреплённая картинка не на своём уровне');
  const b=boardOf(pic);if(b.rowsBottomFirst.join('')!==d.rowsBottomFirst.join(''))bad.push('поле уровня не совпадает с картинкой');
  if(['сторонняя вселенная','мем'].includes(pic.source))bad.push('картинка по сторонней вселенной: не для выпуска без прав');}
 // balance (§3): work of each material on the board = the sum of its pods
 const work={};for(const ch of d.rowsBottomFirst.join(''))if(CODES.includes(ch))work[ch]=(work[ch]||0)+1;const W=Object.values(work).reduce((a,b)=>a+b,0);
 const sum={};for(const p of pods)sum[p.color]=(sum[p.color]||0)+p.count;
 for(const c of new Set([...Object.keys(work),...Object.keys(sum)]))if((work[c]||0)!==(sum[c]||0))bad.push(`баланс ${c}: клеток ${work[c]||0}, в подах ${sum[c]||0}`);
 // pods (§3, §4)
 for(const p of pods)if(!(Number.isInteger(p.count)&&p.count>=1&&p.count<=60))bad.push(`под ${p.color}${p.count} вне 1–60`);
 if(FIX[n]){const got=Object.values(Object.groupBy(pods,p=>p.color)).map(ps=>ps.map(p=>p.count).sort((a,b)=>b-a)).sort((a,b)=>b.reduce((x,y)=>x+y)-a.reduce((x,y)=>x+y));
  if(JSON.stringify(got)!==JSON.stringify(FIX[n]))bad.push(`разбиение §4 ${JSON.stringify(got)} ≠ ${JSON.stringify(FIX[n])}`);}
 else{for(const [c,v] of Object.entries(work)){const ps=pods.filter(p=>p.color===c);
   if(v>=20){if(ps.length<2)bad.push(`${c}: объём ${v} ≥ 20, подов ${ps.length}`);const max=Math.min(60,Math.ceil(v/2));if(ps.some(p=>p.count>max))bad.push(`${c}: под больше ${max}`);}
   else if(ps.length>2)bad.push(`${c}: объём ${v} < 20, подов ${ps.length}`);
   const mean=v/ps.length;if(ps.some(p=>Math.abs(p.count-mean)>Math.max(1,.25*mean)+1e-9))soft.push(`${c}: разброс подов больше ±25 % (§3)`);}
  const [lo,hi]=budget(W);if(P<lo||P>hi)bad.push(`подов ${P} вне бюджета ${lo}–${hi} по W=${W} (§3)`);}
 // queue (§1, §6): 5 places, 3–4 columns (3 on L1–3), the columns by N, start heights within 2, every pod once
 if((d.capacity||5)!==5)bad.push(`мест ${d.capacity}, нужно 5 (§1)`);
 const cols=d.tray.length;if(n<=3?cols!==3:!columnsFor(W).includes(cols))bad.push(`колонок ${cols} (§1, §3: ${n<=3?3:columnsFor(W).join(' или ')})`);
 const hs=d.tray.map(t=>t.length);if(Math.max(...hs)-Math.min(...hs)>2)bad.push(`высоты колонок ${hs.join('/')} — перепад больше 2 (§6)`);
 const flat=d.tray.flat().sort((a,b)=>a-b);if(flat.length!==P||flat.some((x,i)=>x!==i))bad.push('очередь не содержит каждый под ровно один раз');
 const errs=R.validate(d);if(errs&&errs.length)bad.push('движок: '+errs.join('; '));
 // start (§6): at least one visible launch with reachable work
 const s0=R.create(d,5),g=d.rowsBottomFirst.join(''),seen=new Set(d.entries),q=[...d.entries],front=new Set();
 while(q.length){const i=q.pop(),x=i%d.w,y=Math.floor(i/d.w);for(const [dx,dy] of [[1,0],[-1,0],[0,1],[0,-1]]){const u=x+dx,v=y+dy;if(u<0||v<0||u>=d.w||v>=d.h)continue;const j=v*d.w+u;if(seen.has(j))continue;if(g[j]==='.'){seen.add(j);q.push(j);}else if(CODES.includes(g[j]))front.add(g[j]);}}
 if(!d.tray.some(t=>t.length&&front.has(pods[t[0]].color)&&!pods[t[0]].mystery))bad.push('на старте нет видимого запуска с доступной работой (§6)');
 // mechanics (§7, §9, §14): supported, allowed by the calendar, introductions alone and at teaching doses
 for(const t of types){if(!SUPPORTED.has(t))bad.push(`механика ${t} не поддерживается движком (§14)`);if(!slot.allowed.includes(t))bad.push(`механика ${t} не разрешена календарём на L${n} (§9)`);}
 const hidden=pods.filter(p=>p.mystery).length,groups=Object.values(Object.groupBy(pods.filter(p=>p.group),p=>p.group));
 if(types.includes('hidden')!==(hidden>0))bad.push('рецепт и скрытые поды не совпадают');if(types.includes('links')!==(groups.length>0))bad.push('рецепт и связки не совпадают');
 if(groups.some(g=>g.length!==2))bad.push('связка не парная (обычная генерация — пары, §8)');
 if(types.includes('bridge')!==!!d.hero)bad.push('рецепт и мост не совпадают');
 if(L.cls!==slot.cls)bad.push(`класс ${L.cls}, календарь ждёт ${slot.cls} (§5)`);
 const secondIntro=!!slot.intro&&INTRO.some(([m,,b])=>m===slot.intro&&b===n);
 if(slot.intro){if(L.intro!==slot.intro||L.recipe!==slot.intro)bad.push(`знакомство с ${slot.intro}: рецепт ${L.recipe} (§9)`);
  // the second bridge introduction has one winning bridge, so its target is the Normal zone (§9)
  if(L.random.share<.9&&!(slot.intro==='bridge'&&secondIntro))bad.push(`знакомство: случайный тест ${Math.round(L.random.share*100)} % < 90 % (§9)`);
  if(slot.intro==='hidden'){if(hidden<2||hidden>3)bad.push(`знакомство: скрытых ${hidden}, нужно 2–3 (§9)`);const rowOf=p=>d.tray.map(t=>t.indexOf(p)).find(i=>i>=0);if(pods.some((p,i)=>p.mystery&&rowOf(i)<1))bad.push('скрытый под в первом ряду на знакомстве (§8)');
   if(!pods.some((p,i)=>p.mystery&&rowOf(i)===1))bad.push('знакомство: нет скрытого пода во втором ряду (§8)');
   const hm=await hiddenMatters(d,L.solution.route);if(!hm.ok)bad.push('знакомство: ни один скрытый под не влияет на выбор (§8)');}
  if(slot.intro==='links'&&groups.length!==1)bad.push(`знакомство: пар ${groups.length}, нужна 1 (§9)`);}
 if(n<=5&&types.length)bad.push('L1–L5 без допов (§9)');
 // the bridge (§8, §9): not free at the start, every shown bridge keeps a win on an introduction, the choice matters
 if(d.hero){if(R.bridgeChoices(s0).length)bad.push('мост доступен сразу, без расчищенного подхода (§8)');
  if(slot.intro==='bridge'&&!secondIntro){const w=await bridgeChoicesWin(d);if(!w.ok)bad.push(`первое знакомство с мостом: не каждый мост ведёт к победе ${JSON.stringify(w.each)} (§9)`);
   if(L.bridge&&L.bridge.gap<.1)bad.push(`выбор моста почти не важен (разница ${Math.round(L.bridge.gap*100)} пунктов, §8)`);}
  else{const b=await bridgeSingle(d);if(!b.ok)bad.push(`мост: решением должен быть ровно один мост и оба места должны встречаться (${JSON.stringify(b.each)}, предложено ${b.offered.join('/')}; §8)`);}}
 // the owner's amendment of 2026-10-27: greedy strategies lose Hard / Super Hard (§5), recipes by class (§7), picture fitness (§2)
 // the owner's second amendment of 2026-10-27: "opening + colours" for Hard / Super Hard, forced decisions ≤ 35 %, a pod
 // holding a place in a regular Normal (§5, §6)
 if(L.cls!=='Normal'){const o=await openingTest(d,L.cls);if(o.first.wins)bad.push(`дебют + цвета: жадная выигрывает после ${o.first.wins} из ${o.first.openings} первых запусков (§5)`);
  if(o.three.share>.35)bad.push(`дебют + цвета: после трёх запусков выигрывает ${Math.round(o.three.share*100)} % > 35 % (§5)`);L._opening=o;}
 if(n>=4){const f=await forcedShare(d);if(f>.35)bad.push(`вынужденных решений ${Math.round(f*100)} % > 35 % (§5)`);L._forced=f;}
 const prevL=levels[n-1]||readJSON(path.join(LEVELS,lid(n-1)+'.json'));L._regular=L.cls==='Normal'&&n>3&&!L.intro&&!(prevL&&prevL.cls!=='Normal');
 if(L._regular&&!(await holdPod(d)))bad.push('обычный Normal без пода, держащего место (§6)');
 if(!slot.intro){if(L.cls==='Super Hard'&&types.length<Math.min(2,slot.allowed.length))bad.push(`Super Hard: типов допов ${types.length}, нужно ведущая и дополнительная (§7)`);
  if(L.cls==='Hard'&&n!==5&&slot.allowed.length&&!types.length)bad.push('Hard без механики — допустимо только на L5 (§7)');}
 if(n>3&&pic){const f=fitness(pic),weak=f.hiddenColours<=1||f.visible>=6;
  if(L.cls!=='Normal'&&f.hiddenColours<3)bad.push(`картинка для ${L.cls}: скрыто цветов на старте ${f.hiddenColours}, нужно 3+ (§2)`);
  if(L.cls==='Normal'&&weak&&!types.length)bad.push(`слабая картинка (скрыто ${f.hiddenColours}, видно ${f.visible}) без механики (§2)`);}
 if(!slot.intro&&types.length&&L.dosePos==null)soft.push('место дозы не записано (уровень собран до поправки)');
 // the class (§5): the random test, its protocol, the Wilson interval
 const sh=L.random.share,band={Normal:[.5,1.01],Hard:[.2,.5],'Super Hard':[.05,.2]}[L.cls];
 if(!(sh>=band[0]&&sh<band[1]))bad.push(`случайный тест ${Math.round(sh*100)} % не в классе ${L.cls} (§5)`);
 if(!(L.random.runs===100||L.random.runs===400))bad.push(`прогонов ${L.random.runs} (§5: 100 или 400)`);
 if(L.random.borderline)soft.push(`класс на границе по Wilson ${L.random.wilson95.map(x=>Math.round(x*100)+' %').join('–')} — нужно решение владельца (§5)`);
 if(n===1&&(sh<1||L.solution.forks>0))bad.push('L1: есть проигрышный выбор (§4)');if((n===2||n===3)&&sh<.9)bad.push('L2–L3: случайный тест ниже 90 % (§4)');
 // the saved solution (§1, §13.7, §14): replayed now on the live timeline, without boosters, 5 places
 const tr=run(E,d,{commands:L.solution.route.map(a=>({t:0,action:a})),patient:true});if(tr.status!=='won')bad.push(`сохранённое решение не выигрывает при воспроизведении (${tr.status})`);
 if(!L.fairness||L.fairness.fair!==true)bad.push('честность скрытых не подтверждена (§8)');
 const att=+String(L.candidate).split('-').filter(x=>/^\d+$/.test(x)).pop();if(att>=64)bad.push(`кандидат №${att}: бюджет поиска 64 (§13.5)`);
 // soft targets: §5 extras, §10 rhythm, §11 simple strategies, §12 durations
 const fk={Normal:[0,2],Hard:[2,5],'Super Hard':[3,8]}[L.cls];if(L.solution.forks<fk[0]||L.solution.forks>fk[1])soft.push(`опасных развилок ${L.solution.forks} (ориентир ${fk.join('–')}, §5)`);
 if(n>3){const a=L.rhythm.launchesAfter80,lim=L.cls==='Super Hard'?6:4;if(a<1||a>lim)soft.push(`после 80 % W запусков ${a} (ориентир 1–${lim}, §10)`);if(L.rhythm.lastLaunchAtW<80||L.rhythm.lastLaunchAtW>92)soft.push(`последний запуск при ${L.rhythm.lastLaunchAtW} % W (ориентир 80–92 %, §10)`);}
 const won=Object.values(L.strategies).filter(Boolean).length,tot=Object.keys(L.strategies).length;if(L.cls!=='Normal'&&won===tot)soft.push('все простые стратегии выигрывают Hard/Super Hard (§11)');
 const tband=W<400?[20,60]:W<700?[40,100]:W<1000?[70,160]:[100,210],fast=L.solution.fast?.seconds;if(fast&&(fast<tband[0]||fast>tband[1]))soft.push(`время быстрых нажатий ${fast} с (ориентир ${tband.join('–')} с, §12)`);
 if(L.cls==='Normal'&&sh>.95)soft.push(`Normal ${Math.round(sh*100)} % — выше предпочтительной зоны 60–90 % (§5)`);
 rows.push({n,L,bad,soft});}
// sequence rules (§11, motif rule of the owner): lead mechanic and recipe at most 2 in a row, motifs once in 10, 10–11 colours
const seq=[],lead=L=>typesOf(L)[0]||null;
// §5 Normal median 50–60 % over the regular Normals (not L1–3, not introductions, not the first Normal after a Hard / Super Hard)
{const reg=[];for(let n=Math.max(4,from);n<=to;n++){const L=levels[n],p=levels[n-1];if(L&&L.cls==='Normal'&&!L.intro&&!(p&&p.cls!=='Normal'))reg.push(L);}
 if(reg.length>=3){const v=reg.map(L=>L.random.share).sort((a,b)=>a-b),med=v[Math.floor((v.length-1)/2)];if(med<.5||med>.6)seq.push(`медиана обычных Normal ${Math.round(med*100)} % вне 50–60 % (§5)`);
  for(let i=0;i<reg.length;i++){const win=reg.filter(L=>L.level>reg[i].level-10&&L.level<=reg[i].level&&L.random.share>=.95);if(win.length>1){seq.push(`Normal на 95–100 % больше одного на десять: ${win.map(L=>'L'+L.level).join(', ')} (§5)`);break;}}}
 // §5 (second amendment): "most work now" loses about one in three regular Normals on L4–24, one in two from L25
 const lost=L=>{const s=L.strategies||{};return !s['most-now|left']&&!s['most-now|right'];};
 for(const [a,b,q] of [[4,24,1/3],[25,1e9,1/2]]){const r=reg.filter(L=>L.level>=a&&L.level<=b);if(r.length>=3){const k=r.filter(lost).length;if(k<Math.floor(q*r.length))seq.push(`«больше работы сейчас» проигрывает ${k} из ${r.length} обычных Normal L${Math.max(a,from)}–${Math.min(b,to)}, нужно не меньше ${Math.floor(q*r.length)} (§5)`);}}}
for(let n=from+2;n<=to;n++){const a=levels[n-2],b=levels[n-1],c=levels[n];if(!a||!b||!c)continue;
 if(lead(c)&&lead(a)===lead(b)&&lead(b)===lead(c)&&!slotOf(n).intro&&!a.intro&&!b.intro)seq.push(`L${n-2}–L${n}: одна ведущая механика ${lead(c)} 3 раза подряд (§11)`);}
for(let n=from;n<=to;n++)for(let k=n+1;k<=Math.min(to,n+9);k++){const a=levels[n],b=levels[k];if(!a||!b)continue;const both=motifsOf(picture(a.picture)).filter(m=>motifsOf(picture(b.picture)).includes(m));if(both.length)seq.push(`L${n} и L${k}: мотив «${both.join(', ')}» ближе 10 уровней`);}
const wide=Object.values(levels).filter(L=>L&&L.colours>=10).length;if(wide>2)seq.push(`картинок с 10–11 цветами ${wide} > 2 в 20 (§2)`);
const pics=Object.values(levels).filter(Boolean).map(L=>L.picture);if(new Set(pics).size!==pics.length)seq.push('одна картинка на двух уровнях');
// report
const lines=[`# Проверка уровней ${from}–${to} по правилам 17`,'',`| Уровень | Класс | Картинка | Вынужденных | «По цветам» | Дебют + цвета (1 / 3 запуска) | Обязательные правила | Замечания (мягкие ориентиры) |`,'|---|---|---|---|---|---|---|---|'];
let fails=0;for(const r of rows){if(r.missing){lines.push(`| ${r.n} | — | — | нет уровня | |`);fails++;continue;}if(r.bad.length)fails++;
 const st=r.L.strategies||{},gl=!st['most-now|left']&&!st['most-now|right'],o=r.L._opening;
 lines.push(`| ${r.n} | ${r.L.cls}${r.L.intro?' · знакомство':''} | ${r.L.picture} «${picture(r.L.picture)?.title||''}» | ${r.L._forced!=null?Math.round(r.L._forced*100)+' %':'—'} | ${gl?'проигрывает':'выигрывает'} | ${o?`${o.first.wins} из ${o.first.openings} / ${Math.round(o.three.share*100)} %`:'—'} | ${r.bad.length?'НЕТ: '+r.bad.join('; '):'OK'} | ${r.soft.join('; ')||'—'} |`);}
lines.push('',`Последовательность (§2, §11, мотивы): ${seq.length?seq.join('; '):'OK'}`,'',fails||seq.length?`Итог: нарушений обязательных правил — ${fails} уровней${seq.length?`, последовательность — ${seq.length}`:''}.`:'Итог: все обязательные правила соблюдены.');
const text=lines.join('\n')+'\n';if(out)fs.writeFileSync(out,text);console.log(text);process.exit(fails||seq.length?1:0);

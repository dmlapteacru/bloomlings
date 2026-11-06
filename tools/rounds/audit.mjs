// Audit of placed round levels against product/17_LEVEL_GENERATION_RULES.md: everything is recomputed from the level
// file and the picture, nothing is taken on trust from the builder. Mandatory rules (НЕТ fails the level) are kept
// apart from the soft targets of §5, §10 and §12 (замечания).
// node tools/rounds/audit.mjs <from> <to> [--out <file.md>] [--json <file.json>] [--preview <folder>]
//   --preview: a folder of L####.json for positions content/rounds/levels/ does not fill (draft pictures).
import fs from 'node:fs';import path from 'node:path';
import {franchiseBlocked,LEVELS,CODES,readJSON,lid,arg,picture,measure,fitsStage,stageOf,budget,columnsFor,slotOf,SUPPORTED,FIXED_VOLUMES,motifsOf,boardOf,overlaid,recolorErrors} from './lib.mjs';
import {engine} from './engine.mjs';
import {run} from '../level-stats/trace.mjs';
import {channelExits,shutterCheck,latchCheck,cellsHiddenAtStart,bridgeChoicesWin,openingTest,forcedShare,holdPod,hiddenMatters,bridgeSingle,pressure,calmTest,calmFails,calmBorderline,trapLinks,classKeep} from './measure.mjs';
import {INTRO,planOf,repeatWindow} from './lib.mjs';
import {fitness} from './fitness.mjs';
import {breatherRecipeOk,BREATHER_ZONE,repeatClash,REPEAT_GAP,REPEAT_PER_WAVE,medianWindows,median,greedyQuota} from './wave.mjs';
const from=+process.argv[2]||1,to=+process.argv[3]||20,preview=arg('--preview'),out=arg('--out');
// the builder's doses (levels.mjs; 'noop' keeps its command line quiet), for the hidden-cell dose of 17 §8
process.argv.splice(2,0,'noop');const LV=await import('./levels.mjs');process.argv.splice(2,1);
const QUALITY=readJSON(path.join(path.dirname(LEVELS),'quality.json'),{pressure:{min:{},games:30,mode:'soft'},launchesAfter80:{}});
const E=await engine(),{R}=E,FIX={1:[[26,26,26,25,25],[16,16,16],[5]],2:[[40,40,40],[33,32,32],[10,9],[4]],3:[[26,25,25],[24,23,23],[35,34],[26,25],[13],[2]]};
const rows=[],levels={};
for(let n=from;n<=to;n++){const L=readJSON(path.join(LEVELS,lid(n)+'.json'))||(preview&&readJSON(path.join(preview,lid(n)+'.json')));levels[n]=L||null;}
// the level before the range too: the first Normal after a Hard / Super Hard is no regular Normal (§5), also at the range's start
const before=from>1?readJSON(path.join(LEVELS,lid(from-1)+'.json'))||null:null,prevOf=n=>n-1>=from?levels[n-1]:before;
const typesOf=L=>(L.recipe||'none')==='none'?[]:L.recipe.split('+');
for(let n=from;n<=to;n++){const L=levels[n],bad=[],soft=[];
 if(!L){rows.push({n,missing:true});continue;}
 const d=L.definition,pic=picture(L.picture),slot=slotOf(n),pods=d.pods,P=pods.length,types=typesOf(L);
 // picture (§2, §13.1): approved, the stage range, locked pictures only on L1–3; the matrix on the board is the picture's
 // (with the generator's wall, definition.overlay, 17 §8: N is counted on the board with the wall; with the level's
 // recolouring, definition.recolor, 17 §3 and §6 of 2026-11-01: N, W and the colours are the recoloured board's, every
 // entry a colour cell of the picture painted another material; the §2 picture fitness below reads the picture with its
 // wall but without the recolouring, so accents never make a weak picture strong)
 let opic=null;const rcErr=pic?recolorErrors(pic,d.recolor):[];
 if(pic){if(rcErr.length)bad.push(...rcErr.map(e=>e+' (§3)'));else try{opic=overlaid(pic,d.overlay,d.recolor);}catch(e){bad.push(e.message);}}
 if(!pic)bad.push('нет картинки');else{
  if(pic.status!=='approved')bad.push(`картинка ${L.picture} не утверждена (${pic.status})`);
  if(opic){const m=measure(opic);if(!fitsStage(m,n))bad.push(`картинка вне диапазона этапа §2 (${m.N} кл., ${m.colours} цв.; ${JSON.stringify(stageOf(n))})`);
   const b=boardOf(pic,d.overlay,d.recolor);if(b.rowsBottomFirst.join('')!==d.rowsBottomFirst.join(''))bad.push(`поле уровня не совпадает с картинкой${d.recolor?' и её перекраской':''}`);}
  if((pic.locked||null)!==(n<=3?n:null))bad.push('закреплённая картинка не на своём уровне');
  // a third-party universe or a meme only with the owner's confirmed rights (rights 'owner', pictures.mjs rights)
  if(franchiseBlocked(pic))bad.push('картинка по сторонней вселенной: не для выпуска без прав');}
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
 // hidden cells (17 §8 «Скрытые клетки», the owner, 2026-11-01): colour cells, never with hidden pods, the dose counted
 // after the start reveal by class and role (an introduction 5–10 cells, §9)
 {const hc=d.hiddenCells||[],grid=d.rowsBottomFirst.join('');
  if(types.includes('cells')!==(hc.length>0))bad.push('рецепт и скрытые клетки не совпадают');
  if(hc.some(i=>!CODES.includes(grid[i])))bad.push('скрытая клетка не цветная (§8)');
  if(hc.length&&hidden>0)bad.push('скрытые клетки вместе со скрытыми подами (§8: на уровне либо одно, либо другое)');
  if(types.includes('hidden')&&types.includes('cells'))bad.push('рецепт со скрытыми подами и скрытыми клетками (§8)');
  if(hc.length){const k=cellsHiddenAtStart(d);L._cells=k;
   if(slot.intro==='cells'){if(k<5||k>10)bad.push(`знакомство: скрытых клеток после стартового раскрытия ${k}, нужно 5–10 (§9)`);}
   else if(types.includes('cells')){const dz=LV.doses(L.recipe,L.cls,P,false,W);if(dz.cells&&(k<dz.cells[0]||k>Math.max(dz.cells[0],dz.cells[1])))bad.push(`скрытых клеток после стартового раскрытия ${k} вне ${dz.cells.join('–')} для ${L.cls} (${types.filter(t=>t!=='bridge')[0]==='cells'?'ведущая':'дополнительная'}, §7, §8)`);}
   if(k<hc.length)soft.push(`скрытых клеток ${hc.length}, из них ${hc.length-k} раскрыты сразу на старте (в дозу не входят, §8)`);}}
 // latch + acorn key (17 §8, §9, the owner, 2026-11-01): the recipe and the latches agree, an introduction has exactly one,
 // the dose for class and role, and §8 on the saved solution replayed now (measure.mjs latchCheck: depth for the class,
 // pods behind, every latch open and the last before 80 % W, the columns blocked at once)
 {const ls=d.latches||[];if(types.includes('latch')!==(ls.length>0))bad.push('рецепт и засовы не совпадают');
  if(ls.length){if(slot.intro==='latch'){if(ls.length!==1)bad.push(`знакомство: засовов ${ls.length}, нужен один с ключом (§9)`);}
   else if(types.includes('latch')){const dz=LV.doses(L.recipe,L.cls,P,false,W);if(dz.latch&&(ls.length<dz.latch[0]||ls.length>Math.max(...dz.latch)))bad.push(`засовов ${ls.length} вне ${dz.latch.join('–')} для ${L.cls} (§7)`);}
   const lc=await latchCheck(d,L.solution.route,L.cls);L._latch=lc;bad.push(...lc.problems);soft.push(...lc.notes);
   if(ls.some(l=>d.tray[l.column]&&d.tray[l.column].slice(l.depth).some(p=>pods[p]&&pods[p].mystery)))soft.push('скрытый под за засовом (§7: в стартовых рецептах так не ставят)');}}
 // the Drop water channel (17 §8, §9, the owner, 2026-11-05): the recipe and the channel agree, the spring behind work (not
 // ready at the start), on both introductions each exit keeps a winning continuation; each exit's verdict is noted
 {const ch=d.channel||null;if(types.includes('canal')!==!!ch)bad.push('рецепт и канал не совпадают');
  if(ch){if(R.channelChoices(s0).length)bad.push('канал: родник доступен сразу, без расчищенного подхода (§8)');
   const ce=await channelExits(d);L._channel=ce;
   if(slot.intro==='canal'&&!ce.each.every(x=>x.win===true))bad.push(`знакомство с каналом: не каждый выход ведёт к победе ${JSON.stringify(ce.each.map(x=>x.win))} (§9)`);
   else soft.push(`канал: выходы ${ce.each.map(x=>`${x.exit+1} ${x.win===true?'выигрывает':x.win===false?'проигрывает':'не решён'}`).join(', ')}${ce.decides?' — решает ровно один':''} (§8)`);}
  // a deciding-exit Super Hard (the wave plan's channelKind single, recorded as checks.channelKind and random.exit; the
  // owner, 2026-11-05): exactly one exit wins, and the class (below, by L.random) is the one measured with that exit only
  // (17 §5, as a bridge level's best bridge)
  {const better=((planOf(n)||{}).channelKind==='better')||(L.checks&&L.checks.channelKind==='better');
   // 'better' (the owner, 2026-11-06): both exits win, one 10+ points better, the class measured with the better exit
   if(better){if(!ch)bad.push('канал better: на уровне нет канала');else{const ce=L._channel;
    if(!ce.each.every(x=>x.win===true))bad.push(`канал better: выигрывают не оба выхода ${JSON.stringify(ce.each.map(x=>x.win))} (§8)`);
    else if(L.random.exit!==ce.better)bad.push(`канал better: класс измерен ${L.random.exit==null?'без ограничения выхода':'с выходом '+(L.random.exit+1)}, нужен лучший выход ${ce.better+1} (§5)`);
    if(ce.gap<.1)soft.push(`канал better: выходы различаются на ${Math.round(ce.gap*100)} пунктов (при сборке было 10+; 60 партий на выход, §8)`);
    if(L.cls!=='Super Hard')bad.push(`канал better на ${L.cls}: только для Super Hard (§5)`);}}
   const single=!better&&(((planOf(n)||{}).channelKind==='single')||(L.checks&&L.checks.channelKind==='single')||(L.random&&L.random.exit!=null));
   if(single){if(!ch)bad.push('канал single: на уровне нет канала');else{const ce=L._channel;
    if(!ce.decides)bad.push(`канал: решает не ровно один выход (single) ${JSON.stringify(ce.each.map(x=>x.win))} (§8)`);
    else if(L.random.exit!==ce.each.findIndex(x=>x.win===true))bad.push(`канал single: класс измерен ${L.random.exit==null?'без ограничения выхода':'с выходом '+(L.random.exit+1)}, нужен выигрывающий выход ${ce.each.findIndex(x=>x.win===true)+1} (§5)`);
    if(!(L.checks&&L.checks.channelKind==='single'))bad.push('канал single: checks.channelKind не записан (evaluate channelKind single)');
    if(L.cls!=='Super Hard')bad.push(`канал single на ${L.cls}: класс с одним выходом только для Super Hard (§5)`);}}}}
 // the Twig wooden shutters (17 §7, §8, §9, the owner, 2026-11-05): the recipe and the shutters agree, an introduction has
 // one, the number and the counters for class and role (levels.mjs dosesOk), and on the saved solution every shutter opens
 // before 80 % W (measure.mjs shutterCheck; no new front at the opening is a note)
 {const sh=d.shutters||[];if(types.includes('shutters')!==(sh.length>0))bad.push('рецепт и створки не совпадают');
  if(sh.length){if(slot.intro==='shutters'){if(sh.length!==1)bad.push(`знакомство: створок ${sh.length}, нужны одни (§9)`);}
   else{const dz=LV.doses(L.recipe,L.cls,P,false,W);if(dz.shutters&&(sh.length<dz.shutters[0]||sh.length>Math.max(...dz.shutters)))bad.push(`створок ${sh.length} вне ${dz.shutters.join('–')} для ${L.cls} (§7)`);}
   bad.push(...LV.shutterCountErrors(d,slot.intro==='shutters'?'Normal':L.cls));
   const sc=await shutterCheck(d,L.solution.route);L._shutters=sc;bad.push(...sc.problems);soft.push(...sc.notes);}}
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
  // later bridge levels: the wave plan's bridgeKind (the owner, 2026-10-28, second decision: Normal any, Hard half single
  // and half better, Super Hard single); any takes whichever the level measured
  else if(!slot.intro&&((k=>k==='better'||(k==='any'&&L.checks&&L.checks.bridgeKind==='better'))((planOf(L.level)||{}).bridgeKind))){const w=await bridgeChoicesWin(d);if(!w.ok)bad.push(`мост (план better): должны выигрывать оба моста ${JSON.stringify(w.each)} (§8)`);
   if(!L.bridge||L.bridge.gap<.1)bad.push(`мост (план better): один мост должен быть лучше не меньше чем на 10 пунктов (разница ${L.bridge?Math.round(L.bridge.gap*100):'—'}, §8)`);}
  else{const b=await bridgeSingle(d);if(!b.ok)bad.push(`мост: решением должен быть ровно один мост и оба места должны встречаться (${JSON.stringify(b.each)}, предложено ${b.offered.join('/')}; §8)`);
   const bl=L.checks&&L.checks.bridgeLoss;if(bl&&!bl.ok&&L.cls!=='Super Hard')bad.push(`мост: с другим мостом проигрыш сразу при выборе (медиана запусков после выбора ${bl.medianLaunchesAfter}; §8)`);}}
 // the owner's amendment of 2026-10-27: greedy strategies lose Hard / Super Hard (§5), recipes by class (§7), picture fitness (§2)
 // the owner's second amendment of 2026-10-27: "opening + colours" for Hard / Super Hard, forced decisions ≤ 35 %, a pod
 // holding a place in a regular Normal (§5, §6)
 if(L.cls!=='Normal'){const o=await openingTest(d,L.cls);if(o.first.wins)bad.push(`дебют + цвета: жадная выигрывает после ${o.first.wins} из ${o.first.openings} первых запусков (§5)`);
  if(o.three.share>.35)bad.push(`дебют + цвета: после трёх запусков выигрывает ${Math.round(o.three.share*100)} % > 35 % (§5)`);L._opening=o;}
 // a breather (17 §5 «Передышка», the owner, 2026-11-02): a Normal on a picture up to ~550 cells or a position the wave plan
 // marks; zero or one light mechanic (hidden pods or hidden cells, or one plain link pair) without a trap link, zone 75–90 %
 // (a miss is the generator's nearest layout: a note), only the mandatory checks (no forced, calm, holding pod, pressure)
 const br=!!L.breather;if(br){const pl=planOf(n);if(L.cls!=='Normal'||slot.intro||n<=3)bad.push('передышка не на обычной позиции Normal (§5)');
  if(!(W<=LV.BREATHER_N||(pl&&pl.breather)))bad.push(`передышка на картинке ${W} клеток без пометки плана (§5: до ~${LV.BREATHER_N})`);
  if(types.length>1)bad.push(`передышка: механик ${types.length}, допустимо не больше одной (§5)`);else if(!breatherRecipeOk(L.recipe||'none'))bad.push(`передышка: механика ${L.recipe} не лёгкая (§5: скрытые поды, скрытые клетки или одна обычная связка)`);
  if(trapLinks(d).some(x=>x.trap))bad.push('передышка: связка-ловушка (§5)');if(groups.length>1)bad.push(`передышка: связок ${groups.length}, лёгкая механика — одна пара (§5)`);
  if(pl&&!breatherRecipeOk(L.recipe||'none',pl.recipe))soft.push(`передышка: рецепт ${L.recipe}, по плану ${pl.recipe} (§5)`);
  if(L.random.share<BREATHER_ZONE[0]||L.random.share>BREATHER_ZONE[1])soft.push(`передышка: случайный тест ${Math.round(L.random.share*100)} % вне 75–90 % — ближайшая раскладка (§5)`);}
 if(n>=4){const f=await forcedShare(d);if(f>.35&&!br)bad.push(`вынужденных решений ${Math.round(f*100)} % > 35 % (§5)`);L._forced=f;}
 const prevL=levels[n-1]||readJSON(path.join(LEVELS,lid(n-1)+'.json'));L._regular=L.cls==='Normal'&&n>3&&!L.intro&&!br&&!(prevL?prevL.cls!=='Normal':slotOf(n-1).cls!=='Normal');
 if(L._regular&&!(L.recipe||'').includes('bridge')&&!(await holdPod(d)))bad.push('обычный Normal без пода, держащего место (§6)');
 // §5 the calm test and §8 trap links (the owner's amendment of 2026-10-31): with the board settled before every tap a Hard
 // wins at most 50 %, a Super Hard at most 25 %, a regular Normal at least 50 % (the class's best bridge); a Hard or Super
 // Hard with links has a group with a member whose material is hidden at the start
 if(!slot.intro&&n>3&&!br){const calm=await calmTest(d,classKeep(L.random));L._calm=calm.share;const cf=calmFails(L.cls,calm,L._regular),ex=L.exception&&typeof L.exception==='object'&&/calm/i.test(L.exception.rule||'')?L.exception:null;
  // an owner's exception to the calm rule (the level's exception field) turns the failure into a note
  if(cf&&ex)soft.push(`исключение владельца от ${ex.decidedAt}: ${cf}`);else if(cf)bad.push(cf);else if(calmBorderline(L.cls,calm)&&(L.cls!=='Normal'||L._regular))soft.push(`спокойная игра ${Math.round(calm.share*100)} % на границе по Wilson — решение человека (§5)`);
  const tl=trapLinks(d);if((L.cls==='Hard'||L.cls==='Super Hard')&&tl.length&&!tl.some(x=>x.trap))bad.push('нет связки-ловушки: у каждой связки материалы открыты с самого начала (§8)');}
 // layer B (§13, content/rounds/quality.json): pods without work at least the class's bound; soft notes unless mode 'hard'
 {const pm=slot.intro?null:L._regular?QUALITY.pressure.min.regularNormal:L.cls!=='Normal'?QUALITY.pressure.min[L.cls]:null;
  if(pm!=null){const pr=await pressure(d,QUALITY.pressure.games);L._pressure=pr.stuck;if(pr.stuck<pm)(QUALITY.pressure.mode==='hard'?bad:soft).push(`слой Б: стоят без работы ${pr.stuck} < ${pm} (§13, quality.json)`);}}
 if(!slot.intro){if(L.cls==='Super Hard'&&types.length<Math.min(2,slot.allowed.length))bad.push(`Super Hard: типов допов ${types.length}, нужно ведущая и дополнительная (§7)`);
  if(L.cls==='Hard'&&n!==5&&slot.allowed.length&&!types.length)bad.push('Hard без механики — допустимо только на L5 (§7)');}
 if(n>3&&pic&&opic){const f=fitness(overlaid(pic,d.overlay)),weak=f.hiddenColours<=1||f.visible>=6;
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
 if(!L.fairness||L.fairness.fair!==true)bad.push((d.hiddenCells&&d.hiddenCells.length?'честность скрытых клеток':'честность скрытых')+' не подтверждена (§8)');
 const att=+String(L.candidate).split('-').filter(x=>/^\d+$/.test(x)).pop();if(att>=64)bad.push(`кандидат №${att}: бюджет поиска 64 (§13.5)`);
 // soft targets: §5 extras, §10 rhythm, §11 simple strategies, §12 durations
 const fk={Normal:[0,2],Hard:[2,5],'Super Hard':[3,8]}[L.cls];if(L.solution.forks<fk[0]||L.solution.forks>fk[1])soft.push(`опасных развилок ${L.solution.forks} (ориентир ${fk.join('–')}, §5)`);
 if(n>3){const a=L.rhythm.launchesAfter80,lim=L.cls==='Super Hard'?6:4;if(a<1||a>lim)soft.push(`после 80 % W запусков ${a} (ориентир 1–${lim}, §10)`);if(L.rhythm.lastLaunchAtW<80||L.rhythm.lastLaunchAtW>92)soft.push(`последний запуск при ${L.rhythm.lastLaunchAtW} % W (ориентир 80–92 %, §10)`);}
 const won=Object.values(L.strategies).filter(Boolean).length,tot=Object.keys(L.strategies).length;if(L.cls!=='Normal'&&won===tot)soft.push('все простые стратегии выигрывают Hard/Super Hard (§11)');
 const tband=W<400?[20,60]:W<700?[40,100]:W<1000?[70,160]:[100,210],fast=L.solution.fast?.seconds;if(fast&&(fast<tband[0]||fast>tband[1]))soft.push(`время быстрых нажатий ${fast} с (ориентир ${tband.join('–')} с, §12)`);
 if(L.cls==='Normal'&&sh>.95&&!br)soft.push(`Normal ${Math.round(sh*100)} % — выше предпочтительной зоны 60–90 % (§5)`);
 rows.push({n,L,bad,soft});}
// sequence rules (§11, motif rule of the owner): lead mechanic and recipe at most 2 in a row, motifs once in 10, 10–11 colours
const seq=[],lead=L=>typesOf(L)[0]||null;
// §5 Normal median 50–60 % in any 20 consecutive levels over the regular Normals (not L1–3, not introductions, not the first
// Normal after a Hard / Super Hard, not a breather): every 20-level window of the range with at least 5 of them
// (wave.mjs medianWindows; the whole range when it is shorter)
{const reg=[];for(let n=Math.max(4,from);n<=to;n++){const L=levels[n],p=prevOf(n);if(L&&L.cls==='Normal'&&!L.intro&&!L.breather&&!(p&&p.cls!=='Normal')&&!(!p&&slotOf(n-1).cls!=='Normal'))reg.push(L);}
 if(reg.length>=3){const ws=medianWindows(reg.map(L=>({level:L.level,share:L.random.share})),Math.max(4,from),to,{min:Math.min(5,reg.length)}),bw=ws.filter(w=>w.out);
  if(bw.length){const runs=[];for(const w of bw){const r=runs.at(-1);if(r&&w.from===r.last+1){r.last=w.from;r.to=w.to;r.med.push(w.median);}else runs.push({from:w.from,last:w.from,to:w.to,med:[w.median]});}
   seq.push(`медиана обычных Normal вне 50–60 % в окнах по 20 уровней: ${runs.map(r=>`L${r.from}–L${r.to} (${Math.round(Math.min(...r.med)*100)}–${Math.round(Math.max(...r.med)*100)} %)`).join(', ')}; по диапазону ${Math.round(median(reg.map(L=>L.random.share))*100)} % (§5)`);}
  for(let i=0;i<reg.length;i++){const win=reg.filter(L=>L.level>reg[i].level-10&&L.level<=reg[i].level&&L.random.share>=.95);if(win.length>1){seq.push(`Normal на 95–100 % больше одного на десять: ${win.map(L=>'L'+L.level).join(', ')} (§5)`);break;}}}
 // §5 (second amendment): "most work now" loses about one in three regular Normals on L4–24, one in two from L25
 const lost=L=>{const s=L.strategies||{};return !s['most-now|left']&&!s['most-now|right'];};
 for(const g of greedyQuota(reg.map(L=>({level:L.level,lost:lost(L)})),from,to))if(g.count>=3&&g.lost<g.need)seq.push(`«больше работы сейчас» проигрывает ${g.lost} из ${g.count} обычных Normal L${g.from}–${g.to}, нужно не меньше ${g.need} (§5)`);}
for(let n=from+2;n<=to;n++){const a=levels[n-2],b=levels[n-1],c=levels[n];if(!a||!b||!c)continue;
 if(lead(c)&&lead(c)!=='links'&&lead(a)===lead(b)&&lead(b)===lead(c)&&!slotOf(n).intro&&!a.intro&&!b.intro)seq.push(`L${n-2}–L${n}: одна ведущая механика ${lead(c)} 3 раза подряд (§11)`);}
const placedAt=k=>k>=from&&k<=to?levels[k]:readJSON(path.join(LEVELS,lid(k)+'.json'));
// picture repeats (17 §11 as amended by the owner on 2026-11-02): a theme family, and a motif, at most twice in a wave and not
// within 10 levels of another appearance (wave.mjs repeatClash); the levels just before the range count for the distance
{const keys=L=>{const p=picture(L.picture);return [...(p&&p.family?[['семейство тем',p.family]]:[]),...(p?motifsOf(p):[]).map(m=>['мотив',m])];};
 const near=new Set(),waves=new Set();
 for(let n=from;n<=to;n++){const a=levels[n];if(!a)continue;const ka=keys(a);
  for(let k=Math.max(1,n-REPEAT_GAP+1);k<n;k++){const b=placedAt(k);if(!b)continue;for(const [kind,key] of keys(b))if(ka.some(([k2,v])=>k2===kind&&v===key)){const t=`L${k} и L${n}: ${kind} «${key}» ближе ${REPEAT_GAP} уровней (§11, 02.11)`;if(!near.has(t)){near.add(t);seq.push(t);}}}
  // (the appearances in the wave counted inside the audited range: the audit of a whole wave counts the wave)
  const [w0,w1]=repeatWindow(n);for(const [kind,key] of ka){const same=[];for(let k=Math.max(1,w0+REPEAT_GAP,from);k<=Math.min(w1,to);k++){const b=placedAt(k);if(b&&keys(b).some(([k2,v])=>k2===kind&&v===key))same.push(k);}
   const t=`${kind} «${key}» ${same.length} раза в волне: L${same.join(', L')} (§11, 02.11: не больше ${REPEAT_PER_WAVE})`;if(same.length>REPEAT_PER_WAVE&&!waves.has(t)){waves.add(t);seq.push(t);}}}}
// §2: at most two pictures of 10+ colours in any 20 consecutive levels, the pictures' own colours (an accent colour of §6
// does not count, the owner, 2026-11-02)
{const own=k=>{const L=placedAt(k),p=L&&picture(L.picture);return p?measure(p).colours:0;},wide=[];for(let k=Math.max(1,from-19);k<=to;k++)if(own(k)>=10)wide.push(k);
 const hit=[];for(let s0=from-19;s0<=to;s0++){const w=wide.filter(k=>k>=s0&&k<s0+20);if(w.length>2&&w.some(k=>k>=from))hit.push(w.join(','));}
 for(const w of [...new Set(hit)])seq.push(`картинок на 10+ цветов (без акцентов) ${w.split(',').length} в 20 уровнях: L${w.split(',').join(', L')} (§2)`);}
const pics=Object.values(levels).filter(Boolean).map(L=>L.picture);if(new Set(pics).size!==pics.length)seq.push('одна картинка на двух уровнях');
// report
const lines=[`# Проверка уровней ${from}–${to} по правилам 17`,'',`| Уровень | Класс | Картинка | Вынужденных | «По цветам» | Дебют + цвета (1 / 3 запуска) | Стоят без работы | Спокойно | Обязательные правила | Замечания (мягкие ориентиры) |`,'|---|---|---|---|---|---|---|---|---|---|'];
let fails=0;for(const r of rows){if(r.missing){lines.push(`| ${r.n} | — | — | нет уровня | |`);fails++;continue;}if(r.bad.length)fails++;
 const st=r.L.strategies||{},gl=!st['most-now|left']&&!st['most-now|right'],o=r.L._opening;
 lines.push(`| ${r.n} | ${r.L.cls}${r.L.intro?' · знакомство':''} | ${r.L.picture} «${picture(r.L.picture)?.title||''}» | ${r.L._forced!=null?Math.round(r.L._forced*100)+' %':'—'} | ${gl?'проигрывает':'выигрывает'} | ${o?`${o.first.wins} из ${o.first.openings} / ${Math.round(o.three.share*100)} %`:'—'} | ${r.L._pressure!=null?r.L._pressure:'—'} | ${r.L._calm!=null?Math.round(r.L._calm*100)+' %':'—'} | ${r.bad.length?'НЕТ: '+r.bad.join('; '):'OK'} | ${r.soft.join('; ')||'—'} |`);}
lines.push('',`Последовательность (§2, §11, мотивы): ${seq.length?seq.join('; '):'OK'}`,'',fails||seq.length?`Итог: нарушений обязательных правил — ${fails} уровней${seq.length?`, последовательность — ${seq.length}`:''}.`:'Итог: все обязательные правила соблюдены.');
const text=lines.join('\n')+'\n';if(out)fs.writeFileSync(out,text);
// --json <file>: the same verdicts as data, for the exporter's entry check (tools/rounds/export.mjs)
if(arg('--json'))fs.writeFileSync(arg('--json'),JSON.stringify({from,to,levels:rows.map(r=>r.missing?{level:r.n,missing:true,bad:['нет уровня'],soft:[]}:{level:r.n,bad:r.bad,soft:r.soft}),sequence:seq},null,1)+'\n');console.log(text);process.exit(fails||seq.length?1:0);

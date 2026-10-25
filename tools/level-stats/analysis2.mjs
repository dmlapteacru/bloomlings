// Additional checks before the final conclusions (the owner's request of 2026-10-25): comparisons on comparable levels,
// how one colour's volume is split between capsules, when the player's choices end, and the link shapes. Reads
// tools/level-stats/out/data.json (assemble.mjs) and adds `extra` to it; report.mjs shows it in the existing tabs.
// Model runs here (v8E and v9 with the same player, for the end-of-choices table) use the engines of measure.mjs.
// node tools/level-stats/analysis2.mjs
import fs from 'node:fs';import path from 'node:path';
import {allRecords,root,parseToken} from './levels.mjs';import {engine} from './engines.mjs';import {run} from './trace.mjs';import {fromTrace} from './metrics.mjs';import {STRATEGIES} from './strategies.mjs';
const file=path.join(root,'tools/level-stats/out/data.json'),D=JSON.parse(fs.readFileSync(file,'utf8'));
const isNum=x=>typeof x==='number'&&Number.isFinite(x),r2=x=>isNum(x)?Math.round(x*100)/100:null;
const q=(a,p)=>{a=a.filter(isNum).sort((x,y)=>x-y);if(!a.length)return null;const i=(a.length-1)*p,k=Math.floor(i);return a[k]+(a[Math.min(k+1,a.length-1)]-a[k])*(i-k);};
const mean=a=>{a=a.filter(isNum);return a.length?a.reduce((x,y)=>x+y,0)/a.length:null;};
// a summary cell: n, median, spread (quartiles 25–75%), mean where asked
const sum=(a,withMean=false)=>{const v=a.filter(isNum);return {n:v.length,median:r2(q(v,.5)),p25:r2(q(v,.25)),p75:r2(q(v,.75)),...(withMean?{mean:r2(mean(v))}:{})};};
const LV=Object.fromEntries(D.levels.map(l=>[l.key,l])),ROWS=D.rows,GAMES=['CF','v8E','v9'];
const caps=l=>Object.values(l.tray).flat().filter(t=>!/^#[a-z]/.test(t)).map(parseToken);

// ---- per-level derived measures ----
const derived={};
for(const r of ROWS){const l=LV[r.key],c=caps(l),total=c.reduce((s,p)=>s+p.count,0),byType={};for(const p of c)(byType[p.color]=byType[p.color]||[]).push(p.count);
 const types=Object.values(byType),biggest=Math.max(...c.map(p=>p.count));
 const shareOfColour=types.map(t=>Math.max(...t)/t.reduce((a,b)=>a+b,0));
 const links=l.static.tray.links.list;
 derived[r.key]={capsulesPerColour:r2(c.length/types.length),biggestShareOfColourMedian:r2(q(shareOfColour,.5)),biggestShareOfColourMax:r2(Math.max(...shareOfColour)),
  biggestShareOfWork:r2(biggest/total),over60Share:r2(c.filter(p=>p.count>60).reduce((s,p)=>s+p.count,0)/total),
  linkShare:r2(c.filter(p=>p.link).length/c.length),hiddenShare:r2(c.filter(p=>p.hidden).length/c.length),
  linkShapes:links.map(s=>({shape:s.vertical?'vertical':new Set(s.rows).size===1?'horizontal':'diagonal',adjacent:s.length===1,sameColour:s.sameColour,size:s.size})),
  simple:!r.linkSets&&!r.hidden&&!r.padlocks&&!r.boxes};}

// ---- groups that have enough examples ----
const BINS=[['до 400 клеток',0,400],['400–699',400,700],['700–999',700,1000],['1000 и больше',1000,1e9]];
const GROUPS=[['все уровни',r=>true],['ранние уровни (№1–30)',r=>r.level<=30],...BINS.map(([lab,a,b])=>[lab,r=>isNum(r.cells)&&r.cells>=a&&r.cells<b]),
 ['простые (без связок, скрытых, замков и предметов)',r=>derived[r.key].simple],['со связками',r=>r.linkSets>0],['со скрытыми капсулами',r=>r.hidden>0]];
const METRICS=[['cells','клеток на поле'],['types','типов клеток'],['capsules','капсул (лотов)'],['countMedian','типичное число на капсуле (медиана уровня)'],['countSd','разброс чисел (ст. откл.)'],
 ['linkShare','доля капсул в связках',r=>derived[r.key].linkShare],['linkSets','связок (наборов)'],['hiddenShare','доля скрытых капсул',r=>derived[r.key].hiddenShare],
 ['o_zeroFreeShare','время без свободных мест, человек'],['o_meanFree','свободных мест в среднем, человек'],['m_zeroFreeShare','время без свободных мест, модель (один игрок для всех)'],
 ['wonSeconds','время победной попытки, с (человек; видео или часы игрока)',null,true]];
const val=(r,m)=>m[2]?m[2](r):r[m[0]];
const groups=GROUPS.map(([label,p])=>{const out={label,games:{}};for(const g of GAMES){const rs=ROWS.filter(r=>r.game===g&&p(r));out.games[g]={levels:rs.length,keys:rs.map(r=>r.key),metrics:Object.fromEntries(METRICS.map(m=>[m[0],sum(rs.map(r=>val(r,m)),m[3])]))};}
 out.preliminary=GAMES.some(g=>out.games[g].levels>0&&out.games[g].levels<4);return out;});
// do the CF-vs-pilot differences hold in each group? (CF median against each pilot's median, only where both have ≥3 levels)
const CLAIMS=[['cells','поле больше'],['capsules','капсул больше'],['linkShare','больше капсул в связках'],['types','типов меньше',-1],['countSd','числа ровнее (меньше разброс)',-1]];
const holds=CLAIMS.map(([k,label,sign=1])=>({claim:label,metric:k,groups:groups.map(gr=>{const cf=gr.games.CF.metrics[k],res={group:gr.label,cf:cf.median,n:gr.games.CF.levels};
  for(const g of ['v8E','v9']){const o=gr.games[g].metrics[k];res[g]=o.median;res[g+'n']=gr.games[g].levels;res[g+'holds']=gr.games.CF.levels>=3&&gr.games[g].levels>=3&&isNum(cf.median)&&isNum(o.median)?(sign*(cf.median-o.median)>0):null;}
  return res;})}));
// v8E against v9, level by level (same number = the same picture and board, different queue)
const pairs=[];for(let i=1;i<=30;i++){const a=ROWS.find(r=>r.key==='v8E-'+i),b=ROWS.find(r=>r.key==='v9-'+i);if(!a||!b)continue;
 const da=derived[a.key],db=derived[b.key];pairs.push({level:i,difficulty:a.difficulty,sameBoard:a.cells===b.cells,capsules:[a.capsules,b.capsules],countMax:[a.countMax,b.countMax],countSd:[a.countSd,b.countSd],
  biggestShareOfWork:[da.biggestShareOfWork,db.biggestShareOfWork],over60Share:[da.over60Share,db.over60Share],linkSets:[a.linkSets,b.linkSets],hidden:[a.hidden,b.hidden],
  risky:[a.risky,b.risky],random:[a.s_random,b.s_random],routeSeconds:[a.m_seconds,b.m_seconds],types:[a.types,b.types]});}
const pairSum=Object.fromEntries(['capsules','countMax','countSd','biggestShareOfWork','over60Share','linkSets','hidden','risky','random','routeSeconds','types'].map(k=>{const d=pairs.map(p=>isNum(p[k][0])&&isNum(p[k][1])?p[k][1]-p[k][0]:null).filter(isNum);
 return [k,{n:d.length,changed:d.filter(x=>x!==0).length,up:d.filter(x=>x>0).length,down:d.filter(x=>x<0).length,medianChange:r2(q(d,.5)),v8E:r2(q(pairs.map(p=>p[k][0]),.5)),v9:r2(q(pairs.map(p=>p[k][1]),.5))}];}));

// ---- colour volume, per game and by board size ----
const VOL=[['capsulesPerColour','капсул на один цвет'],['biggestShareOfColourMedian','доля своего цвета у самой большой капсулы цвета (медиана по цветам)'],['biggestShareOfWork','доля всей работы у самой большой капсулы уровня'],['over60Share','доля всей работы в капсулах больше 60']];
const volume=[['все уровни',r=>true],...BINS.map(([lab,a,b])=>[lab,r=>isNum(r.cells)&&r.cells>=a&&r.cells<b])].map(([label,p])=>({label,games:Object.fromEntries(GAMES.map(g=>{const rs=ROWS.filter(r=>r.game===g&&p(r));return [g,{levels:rs.length,...Object.fromEntries(VOL.map(([k])=>[k,sum(rs.map(r=>derived[r.key][k]))]))}];}))}));
const linkShapes=Object.fromEntries(GAMES.map(g=>{const s=ROWS.filter(r=>r.game===g).flatMap(r=>derived[r.key].linkShapes);const c=(f)=>s.filter(f).length;
 return [g,{sets:s.length,vertical:c(x=>x.shape==='vertical'),horizontal:c(x=>x.shape==='horizontal'),diagonal:c(x=>x.shape==='diagonal'),adjacent:c(x=>x.adjacent),farther:c(x=>!x.adjacent),sameColour:c(x=>x.sameColour),mixed:c(x=>!x.sameColour),triplesOrMore:c(x=>x.size>2)}];}));

// ---- when the player's choices end (won attempts) ----
// cleared share at the last tap, seconds from the last tap to the win, that share of the attempt, taps after 80%
// cleared, the stretch with the fullest buffer; a full buffer split into "something still works" and "all stopped"
// where the source allows it (our replays and model runs; a video shows filled places, not whether they work)
function endOfChoices(m,{real=null}={}){if(!m||!m.series)return null;const cl=m.series.cleared||[],taps=m.series.taps||[];if(!taps.length||!cl.length)return null;
 const at=t=>{let v=0;for(const [x,y] of cl){if(x<=t+1e-9)v=y;else break;}return v;};const last=taps.at(-1),t80=(cl.find(([,v])=>v>=.8)||[null])[0];
 const segs=(m.segmentsByCleared||[]).filter(s=>s&&s.reached);const peak=segs.length?segs.reduce((a,s)=>(s.meanFree??9)<(a.meanFree??9)?s:a):null;
 const out={clearedAtLastTap:r2(at(last)),lastTapToWin:r2(m.end-last),tailShare:r2((m.end-last)/m.end),tapsAfter80:t80==null?null:taps.filter(t=>t>t80).length,peakStretch:peak?peak.label:null,end:m.end};
 if(real)Object.assign(out,real);
 // full buffer split, from the free/working/stalled series (model and exact replays only)
 if(m.series.working&&m.series.free){const fr=m.series.free,wk=m.series.working;let anyWork=0,allStopped=0;for(let i=0;i<fr.length;i++){const t0=fr[i][0],t1=i+1<fr.length?fr[i+1][0]:m.end;if(fr[i][1]!==0)continue;
   let w=0;for(const [x,y] of wk){if(x<=t0+1e-9)w=y;else break;}if(w>0)anyWork+=t1-t0;else allStopped+=t1-t0;}out.fullWorking=r2(anyWork/m.end);out.fullAllStopped=r2(allStopped/m.end);}
 return out;}
const choices=[];
for(const l of D.levels){const won=l.attempts.find(a=>a.outcome==='won');
 if(l.game==='CF'&&won?.video)choices.push({key:l.key,game:'CF',source:'видео',attempt:won.attempt,videos:won.videos,...endOfChoices(won.video)});
 if(l.game==='v8E'&&won?.replay){// the owner's clock: real time of the last accepted tap and of the end (×2 with fast on; auto-tail ×2.5)
  const realLast=won.tapTimes?.filter(isNum).at(-1)??null,realEnd=won.realSeconds??null;
  choices.push({key:l.key,game:'v8E',source:'точный повтор (модельные секунды ×1)',attempt:won.attempt,...endOfChoices(won.replay,{real:{realLastTapToWin:isNum(realLast)&&isNum(realEnd)?r2(realEnd-realLast):null,realTailShare:isNum(realLast)&&isNum(realEnd)?r2((realEnd-realLast)/realEnd):null}})});}}
// the same model player on v8E and v9 (and the CF levels on the model): the planner, tapping as soon as it can
const recs=Object.fromEntries(allRecords().map(r=>[r.key,r]));const modelChoices=[];
for(const r of ROWS){const rec=recs[r.key];if(!rec?.model)continue;const E=await engine(r.game);const tr=run(E,rec.model,{player:STRATEGIES.planner.make(1,E),capacity:rec.model.capacity||5});
 if(tr.status!=='won'){modelChoices.push({key:r.key,game:r.game,won:false});continue;}modelChoices.push({key:r.key,game:r.game,won:true,...endOfChoices(fromTrace(tr))});}
const choiceSummary=(list,src)=>Object.fromEntries(GAMES.map(g=>{const v=list.filter(x=>x.game===g&&x.won!==false);if(!v.length)return [g,null];
 return [g,{attempts:v.length,source:src,clearedAtLastTap:sum(v.map(x=>x.clearedAtLastTap)),lastTapToWin:sum(v.map(x=>x.lastTapToWin),true),tailShare:sum(v.map(x=>x.tailShare)),tapsAfter80:sum(v.map(x=>x.tapsAfter80)),
  realLastTapToWin:sum(v.map(x=>x.realLastTapToWin),true),realTailShare:sum(v.map(x=>x.realTailShare)),fullWorking:sum(v.map(x=>x.fullWorking)),fullAllStopped:sum(v.map(x=>x.fullAllStopped)),
  peak:v.reduce((a,x)=>(x.peakStretch&&(a[x.peakStretch]=(a[x.peakStretch]||0)+1),a),{})}];}));
D.extra={built:'tools/level-stats/analysis2.mjs',bins:BINS.map(b=>b[0]),groups,holds,pairs,pairSum,volume,linkShapes,choices,modelChoices,
 choiceSummary:{human:choiceSummary(choices),model:choiceSummary(modelChoices,'модель: планировщик, тап сразу')},derived};
fs.writeFileSync(file,JSON.stringify(D));
// a short print for the conclusions
for(const h of holds){console.log('#',h.claim);for(const g of h.groups)console.log('  ',g.group.padEnd(48),'CF',g.cf,'('+g.n+')','v8E',g.v8E,'('+g.v8En+')',g.v8Eholds,'v9',g.v9,'('+g.v9n+')',g.v9holds);}
console.log('volume',JSON.stringify(volume.map(v=>[v.label,Object.fromEntries(GAMES.map(g=>[g,[v.games[g].levels,v.games[g].capsulesPerColour.median,v.games[g].biggestShareOfColourMedian.median,v.games[g].biggestShareOfWork.median,v.games[g].over60Share.median]]))])));
console.log('links',JSON.stringify(linkShapes));console.log('pairs',JSON.stringify(pairSum));
console.log('choices human',JSON.stringify(D.extra.choiceSummary.human));console.log('choices model',JSON.stringify(D.extra.choiceSummary.model));

// ---- the model check, the plans, the calibration, and the texts that go with them ----
const rd=p=>fs.existsSync(path.join(root,p))?JSON.parse(fs.readFileSync(path.join(root,p),'utf8')):null;
D.extra.validation=rd('tools/level-stats/out/validate-cf.json');
const PLAN_SUMMARY={
 'CF-25':'Настоящая альтернатива в начале. План 1: сначала белая пара и тёмная 30 вместе с красной 34. Красная клубника открывается, потом тёмный котёнок, розовый и зелёный, и только после этого берётся пара «тёмная 30 + зелёная 22». План 2: обе красные (34 и 30) раньше тёмной. Открываются сразу тёмный и красный, пара «тёмная + зелёная» берётся раньше и открывает розовый. Общее у обоих: белая пара держит 2 места с самого начала, а пару с зелёной нельзя брать, пока зелёному нечего есть. Так ты проиграл первую попытку: пара взята на 52-й секунде.',
 'v8E-15':'Содержательно другой план в начале не найден в выполненном поиске (30 вариантов первых четырёх ходов). Найденные пути расходятся только на 8-м открытом цвете (земля и ягода меняются местами), начало у всех одно.',
 'v9-22':'Содержательно другой план в начале не найден в выполненном поиске. Пути расходятся только на 10-м открытом цвете, когда главное уже решено.',
 'v9-25':'Настоящая альтернатива в начале. План 1: сначала небо (41, 38) — открываются тыква, солнце, янтарь и лепесток. План 2: первыми жемчуг 35 и слива 43 — за 4 тапа открываются сразу шесть цветов и освобождаются 3 места, а небо берётся позже. Дальше планы сходятся.'};
const plans=rd('tools/level-stats/out/plans.json');if(plans){for(const p of plans)p.summary=PLAN_SUMMARY[p.key]||null;D.extra.plans=plans;}
// calibration: the same Colony Flow checks with the ants leaving their capsule 1.5 times slower
{const calDir=path.join(root,'tools/level-stats/out/levels-cal');if(fs.existsSync(calDir)){const base=Object.fromEntries(D.levels.filter(l=>l.game==='CF').map(l=>[l.key,l]));const cal=fs.readdirSync(calDir).map(f=>JSON.parse(fs.readFileSync(path.join(calDir,f),'utf8'))).filter(l=>l.checks&&base[l.key]?.checks);
 const avg=(ls,f)=>{const v=ls.map(f).filter(isNum);return v.length?r2(mean(v)):null;},medn=(ls,f)=>r2(q(ls.map(f).filter(isNum),.5));
 const pairsC=cal.map(c=>[base[c.key],c]);const same=(f)=>pairsC.filter(([a,b])=>f(a)===f(b)).length;
 const rows=[['уровней с моделью в обеих версиях',String(pairsC.length),String(pairsC.length)],
  ['доля побед случайного выбора, среднее',String(avg(pairsC.map(x=>x[0]),l=>l.strategies?.random?.rate)),String(avg(pairsC.map(x=>x[1]),l=>l.strategies?.random?.rate))],
  ['доля побед планировщика, среднее',String(avg(pairsC.map(x=>x[0]),l=>l.strategies?.planner?.rate)),String(avg(pairsC.map(x=>x[1]),l=>l.strategies?.planner?.rate))],
  ['доля побед «слева направо», среднее',String(avg(pairsC.map(x=>x[0]),l=>l.strategies?.['left-to-right']?.rate)),String(avg(pairsC.map(x=>x[1]),l=>l.strategies?.['left-to-right']?.rate))],
  ['минимум мест, медиана',String(medn(pairsC.map(x=>x[0]),l=>l.checks.minCapacity.min)),String(medn(pairsC.map(x=>x[1]),l=>l.checks.minCapacity.min))],
  ['уровней с тем же минимумом мест','—',`${same(l=>l.checks.minCapacity.min)} из ${pairsC.length}`],
  ['рискованных решений на пути, медиана',String(medn(pairsC.map(x=>x[0]),l=>l.checks.route?.risky)),String(medn(pairsC.map(x=>x[1]),l=>l.checks.route?.risky))],
  ['путь без ожидания проигрывает, уровней',String(pairsC.filter(([a])=>a.checks.routeFast?.status&&a.checks.routeFast.status!=='won').length),String(pairsC.filter(([,b])=>b.checks.routeFast?.status&&b.checks.routeFast.status!=='won').length)],
  ['стратегий с другим исходом (уровень × стратегия, кроме случайной)','—',String(pairsC.reduce((n,[a,b])=>n+Object.keys(a.strategies||{}).filter(k=>k!=='random'&&b.strategies?.[k]&&a.strategies[k].wins!==b.strategies[k].wins).length,0))+' из '+pairsC.reduce((n,[a])=>n+Object.keys(a.strategies||{}).filter(k=>k!=='random').length,0)]];
 D.extra.calibration={rows,note:'Обе версии — приближённая модель Colony Flow. Медленный выход муравьёв приближает средний буфер к видео. То, что меняется вместе с ним, нельзя считать свойством оригинальной игры.'};}}
const li=a=>'<ul>'+a.map(x=>`<li>${x}</li>`).join('')+'</ul>';
D.extra.fixesHtml='<h3>Что исправлено при сверке (25.10)</h3>'+li([
 'Доля капсул Colony Flow с числом от 10 до 50 — <b>94 %</b> (1196 из 1272). В выводах было «97 %».',
 'Попыток Colony Flow изучено <b>39</b> (36 побед, 2 поражения, 1 неполная запись) на 37 уровнях. В выводах было «40».',
 'Уровней с моделью: <b>88 из 97</b> (Colony Flow 28, v8E 30, v9 30).',
 'Сверка модели с записью: исход совпал в <b>26 из 29</b> завершённых попыток. Неполная запись L5 в сверку исходов не входит, было «27 из 30». L115 не входит, потому что у него модели нет.',
 'Число выигрышных последовательностей неизвестно (перебор не уложился в бюджет) на <b>55 из 88</b> уровней с моделью. Минимум мест не доказан на <b>16 из 88</b>. В списке неизвестного было «43 из 76» и «11 из 76».',
 'Уровень без поля (L350, «неизвестно» клеток) попадал в группу «до 150 клеток» во вкладке «Время» и считался полем из одной клетки при подсчёте похожести. Теперь он в группе «клеток неизвестно» и в сравнения размеров не входит.',
 'Группы по размеру поля во вкладке «Время» заменены на общие для всего отчёта: до 400, 400–699, 700–999, 1000+ клеток.',
 'Стратегии переименованы по действию: «цвет выше на поле — первым» (было «сверху вниз»), «цвет ниже на поле — первым» (было «снизу вверх»), «колонки по кругу» (было «по диагонали»).']);
D.extra.modelLimitsHtml='<h3>Что модель Colony Flow воспроизводит, что упрощает, чего нет</h3>'+li([
 '<b>Воспроизводит</b> (правила проверены по видео): точное совпадение цвета; 5 мест (6 на L3 после бустера); капсула освобождает место после ухода последнего муравья; связки всех форм, включая вертикальную пару (нижняя выходит с верхней); скрытые капсулы открываются наверху колонки; замок держит колонку, ключ в ямке 2×2; ящик с камнем считает работу своего цвета, шоколад — любого; кувшин 2×2 забирает работу своего цвета; клетка «?» — обычная клетка скрытого цвета.',
 '<b>Упрощает</b>: скорость муравьёв взята из наших пилотов, а у Colony Flow они выходят из капсулы примерно в 1,5 раза реже (калибровка по видео ниже). Муравьи заходят на поле по краю. Ключ снимает замок через 1 с после того, как до ямки можно дойти. В повторах записей замки открываются в секунду из записи. Клетки под ящиками и шоколадом розданы по недостающим цветам в порядке чтения. Цвет кувшинов L185, L192 и одного из L358 угадан. Поле восстановлено по обложке, и мелкие участки цвета местами распознаны неверно.',
 '<b>Не поддерживает</b>: L790 (17 цветов, в движке 16); L27, L145, L214, L358, L1170, L1270 (поле расходится с капсулами больше чем на 4 % клеток); L115 (ни одно восстановление клеток под шоколадом не проходится на модели); L350 (нет поля). Скорость игры ×1/×2 на видео с сайта неизвестна.',
 '<b>Можно доверять</b>: исходам записанных последовательностей (26 из 29) и среднему буферу с поправкой скорости. <b>Только гипотезы</b>: поведение в конкретную секунду (|разница| около 0,8 места), стратегии, минимум мест и роковые ходы на уровнях Colony Flow — особенно с предметами на поле и с мелкими участками цвета.']);
D.extra.validationNotes=li([
 '<b>Простой — CF L2</b> (попытка 1, <code>level-1-4.mp4</code>, 35–72,5 с, ×1). Все 9 тапов приняты в свои секунды. Базовая модель освобождает места раньше: свободно 2,81 против 2,13 на видео, полный буфер 0 % против 23 %. Расходиться на 2 места начинает на 19,5 с. С выходом муравьёв ×1,5 совпадает: 2,17 и 17 %, |разница| 0,46. Причина — скорость выхода муравьёв, она исправима.',
 '<b>Скрытые капсулы — CF L10</b> (попытки 1 и 2, <code>level-10-*.mp4</code>). Исходы совпали: проигрыш и победа. |Разница| 0,37 и 0,74, с ×1,5 — 0,21 и 0,51. В победной попытке задержан 1 тап (202 с), расхождение на 2 места появляется на 14 с. Здесь модели можно доверять.',
 '<b>Связки — CF L25</b> (<code>XRecorder_20261024_02.mp4</code>). Проигрыш (попытка 1) повторяется. Перебор при этом говорит, что позицию ещё можно было спасти: оригинал строже модели. В победе (попытка 2) задержаны 4 тапа начиная с 213,6 с. |Разница| 0,82, с ×1,5 — 0,39. Полный буфер: видео 69 %, модель 47 %, с ×1,5 — 71 %.',
 '<b>Связки и скрытые — CF L20</b> (<code>XRecorder_20261023_02.mp4</code>). Принято 36 тапов из 36, 4 задержаны начиная с 114,6 с. |Разница| 0,71, с ×1,5 — 0,65.',
 '<b>Большое расхождение буфера — CF L533 и L109</b> (видео с сайта). На видео полный буфер 88 % и 72 % времени, в модели 9 % и 8 % (с ×1,5 — 22 % и 36 %). Длительность совпадает: 180 и 190 с у L533. Капсулы в записи держат места заметно дольше, на L533 модели нужен выход ×3. Возможная причина — скорость игры на видео с сайта, она неизвестна.',
 '<b>Разный исход — CF L757</b> (<code>videoplayback_757_hard.mp4</code>). Модель застревает на 31-м тапе из 43: 230 с модели, 262 с видео. Все 5 мест держат капсулы, чьи последние клетки (слива 2, жёлтый 2, зелёный 3, оранжевый 10) в восстановленном поле закрыты белыми клетками. В игре до них дошли. Причина — восстановление мелких участков поля по обложке, а не правило игры.',
 '<b>Разный исход — CF L165</b> (<code>videoplayback_165.mp4</code>). Модель останавливается на 12-м тапе из 29. Тёмные клетки для капсул 25 и 26 в модели недоступны. В записи игрок тоже ждал 80 с (39–121 с), но потом места освободились. Причина та же: доступность клеток в восстановленном поле.',
 '<b>Чувствительность к скорости — CF L185</b>. С базовой скоростью модель проигрывает на 43-м тапе из 46, с выходом ×1,5 — выигрывает, как в записи.']);
D.extra.strategyDefsHtml='<h3>Что делает каждая стратегия</h3><p class="small">Все стратегии видят то же, что игрок: верхние капсулы (цвет и число) и поле. Скрытую капсулу ниже верхней они не читают. Темп у всех одинаковый: тап в тот момент, когда он возможен (есть свободное место и капсула сверху), кроме последней строки. Ничьи решаются в пользу левой колонки. Стратегии проверены на нашем движке. Для Colony Flow это приближённая модель.</p>'+li([
 '<b>слева направо / справа налево</b> — самая левая (правая) колонка, где можно тапнуть;',
 '<b>цвет выше на поле — первым / цвет ниже на поле — первым</b> — капсула, чьи клетки в среднем лежат выше (ниже) на поле;',
 '<b>колонки по кругу</b> — c1, c2, c3, c1, … пропуская колонки, где тапнуть нельзя;',
 '<b>случайно</b> — 100 запусков с разным случаем, показана доля побед;',
 '<b>маленькие / большие сначала</b> — по числу на капсуле (у связки — по сумме);',
 '<b>больше всего очистит сейчас</b> — по числу клеток своего цвета, до которых муравьи дойдут прямо сейчас;',
 '<b>быстрее освободит место</b> — капсула, которая доделает всю работу из доступного сейчас, и самая маленькая из таких;',
 '<b>с планированием (на ход вперёд)</b> — пробует каждый ход на копии и даёт работе закончиться без новых тапов, смотрит на победу, свободные места и очищенные клетки (скрытое не читает);',
 '<b>с планированием, ждёт окончания работы</b> — то же, но следующий тап только после окончания текущей работы.'])+
 '<p class="small"><b>С полным знанием скрытых капсул</b> работают только проверки перебором: проходимость, минимум мест, рискованные решения на пути, число выигрышных последовательностей, разные планы. Перебор идёт на терпеливой модели, где каждый тап делается после окончания работы.</p>';
fs.writeFileSync(file,JSON.stringify(D));console.log('extra texts merged; validation',!!D.extra.validation,'plans',D.extra.plans?.length,'calibration',!!D.extra.calibration);

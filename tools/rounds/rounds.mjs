// The round runner (product/17_LEVEL_GENERATION_RULES.md §13; skill bloomlings-round-wave): one entry point over
// pictures.mjs, levels.mjs, audit.mjs and the pilot, so a wave of levels runs as scripts, not by hand.
//
// node tools/rounds/rounds.mjs plan <from> <to>
//      the positions of the wave (class, introduction, stage, allowed mechanics), what the bank and the free approved
//      pictures already cover, and the picture order for the rest → content/rounds/runs/L####-L####/plan.{md,json}.
//      With the owner-approved mechanics breakdown runs/L####-L####/mechanics.json (17 §13 step 1): the breakdown first
//      (per level lead, additional, doses, bridge side and strip; shares per mechanic), then the order for what it needs:
//      new strip pictures by crossing side and size stage, pool pictures for the rest (an overlay level takes a free
//      pool picture with the generator's wall; a gap level a split of a free pool picture, bridge-scene.mjs make --mode
//      split, listed with its command and suggested sources)
// node tools/rounds/rounds.mjs run <from> <to> [--jobs 4] [--candidates 64] [--no-build] [--no-sample] [--no-bridge] [--per-position N] [--all]
//      dry placement on a copy; candidates only for what stays open (one process per picture, resumable: a combination
//      already searched is skipped; under a mechanics plan only each position's planned recipe, on a wall of the planned
//      side for an overlay position, on pictures with their own strip of that side for a new-strip one); missing checks;
//      placement (a planned position takes only its recipe and bridge side); audit; summary; the owner's sample as a pilot
//      → content/rounds/runs/L####-L####/{summary.md,audit.md,sample/,build/}. Exit code 1 when a position stays open
//      or a mandatory rule fails.
// node tools/rounds/rounds.mjs review <results.json> [--preview <folder>]
//      the owner's pilot results next to the metrics: ratings, notes, mismatches with quality.json, a draft for
//      content/rounds/owner-notes.md → content/rounds/reports/<pilot>-review-<date>.md; a compact copy of the results
//      goes to product/playtests/<pilot>-owner-<date>/results.json
// node tools/rounds/rounds.mjs confirm <from> <to> --sample <results.json> [--date YYYY-MM-DD]
//      the owner's confirmation of the wave's sample, played in the pilot on the current engine (spec 006 FR-050):
//      → content/rounds/runs/L####-L####/confirmed.json, the entry check of tools/rounds/export.mjs
// node tools/rounds/rounds.mjs confirm <from> <to> --owner-word "<the owner's words>" [--date YYYY-MM-DD]
//      the owner told us to move the wave into the game before playing its sample: the words are recorded as they were
//      said, with no sample (sample [], results null); the phone check after publishing is then the first play
import fs from 'node:fs';import path from 'node:path';import os from 'node:os';import {spawn,execFileSync} from 'node:child_process';
import {root,ROUNDS,LEVELS,BANK,readJSON,writeJSON,lid,arg,flag,manifest,picture,measure,stageOf,fitsStage,sizeBin,slotOf,classOf,INTRO,MECH_RU,SUPPORTED,usage,motifsOf,boardOf,overlaid,bridgeSide,wallOverlay,mechanicsPlans,planOf} from './lib.mjs';
import {fitness} from './fitness.mjs';
import {fingerprints,sameEngine} from './conformance.mjs';
const cmd=process.argv[2],HERE=path.dirname(new URL(import.meta.url).pathname),NODE=process.execPath;
process.argv.splice(2,0,'noop');const LV=await import('./levels.mjs');const BS=await import('./bridge-scene.mjs');process.argv.splice(2,1);
const PILOT=path.join(root,'tools/gameplay-pilot-v12/build.mjs');
const QUALITY=readJSON(path.join(ROUNDS,'quality.json'),{pressure:{min:{}},sample:{},mismatch:{noteWords:[]}});
const pct=x=>x==null?'—':Math.round(x*100)+' %';
const waveDir=(a,b)=>path.join(ROUNDS,'runs',`${lid(a)}-${lid(b)}`);
const placedLevels=()=>{const o={};if(fs.existsSync(LEVELS))for(const f of fs.readdirSync(LEVELS).filter(f=>/^L\d+\.json$/.test(f))){const L=readJSON(path.join(LEVELS,f));o[L.level]=L;}return o;};
const FRANCHISE=new Set(['сторонняя вселенная','мем']);
// a picture usable for rounds: approved, not locked to L1–3, not a franchise reference, not yet in a round
function freePictures(){const u=usage();return manifest().pictures.filter(e=>e.status==='approved'&&!u[e.id]).map(e=>picture(e.id)).filter(p=>p&&!p.locked&&!FRANCHISE.has(p.source));}
const weakOf=f=>f.hiddenColours<=1||f.visible>=6;
const regularAt=(n,placed)=>classOf(n)==='Normal'&&n>3&&!slotOf(n).intro&&classOf(n-1)==='Normal';
const run1=(args,env={})=>new Promise(res=>{const p=spawn(NODE,args,{cwd:root,env:{...process.env,...env},stdio:['ignore','pipe','pipe']});let out='';p.stdout.on('data',d=>out+=d);p.stderr.on('data',d=>out+=d);p.on('close',code=>res({code,out}));});

// ---- plan ----
// the mechanics breakdown (17 §13 step 1, amendment of 2026-10-28): the owner-approved content/rounds/runs/<wave>/mechanics.json
const MRU={none:'без допов',links:'связки',hidden:'скрытые',bridge:'мост'},SIDE_RU={up:'вверх',left:'влево',right:'вправо',two:'две стороны'},STRIP_RU={new:'новая картинка с полосой',overlay:'стена поверх утверждённой',gap:'разделённая картинка (полоса в цвет фона)'};
const pr=x=>+(x*100).toFixed(1),pctR=([a,b])=>a===b?`${pr(a)} %`:`${pr(a)}–${pr(b)} %`;
function doseText(recipe,cls){if(recipe==='none')return 'без допов';const d=LV.doses(recipe,cls,24,false),n=k=>d[k][0]===d[k][1]?d[k][0]:d[k].join('–');
 return LV.doseShares(recipe,cls).map(x=>x.choice?'мост: 1 выбор из 2 мест':`${MRU[x.m]} ${x.role==='lead'?'':'доп. '}${pctR(x.range)} P (${n(x.m)} при P = 24)`).join('; ');}
const stageKey=s=>s.fixed?'фикс.':`${s.cells[0]}–${s.cells[1]} клеток, ${s.colours[0]}–${s.colours[1]} цветов${s.compact?' (или компактная 200–399 / 3–6)':''}`;
// the breakdown of the wave's planned levels: per level lead, additional, doses, bridge side and strip; shares per mechanic
function breakdown(levels){if(!levels.length)return null;const n=levels.length,has=m=>levels.filter(l=>l.recipe.split('+').includes(m)),cnt={};
 const normal=levels.filter(l=>l.cls==='Normal'),types=k=>levels.filter(l=>(l.recipe==='none'?0:l.recipe.split('+').length)===k).length;
 for(const l of levels)if(l.bridgeSide)cnt[l.bridgeSide]=(cnt[l.bridgeSide]||0)+1;
 return {levels:levels.map(l=>({level:l.level,cls:l.cls,lead:l.lead||null,add:l.add||[],recipe:l.recipe,doses:doseText(l.recipe,l.cls),...(l.bridgeSide?{bridgeSide:l.bridgeSide,strip:l.strip||null}:{})})),
  shares:Object.fromEntries(['bridge','links','hidden'].map(m=>[m,{levels:has(m).length,share:Math.round(has(m).length/n*100)/100,lead:levels.filter(l=>l.lead===m).length}]).concat([['none',{levels:levels.filter(l=>l.recipe==='none').length,share:Math.round(levels.filter(l=>l.recipe==='none').length/n*100)/100}]])),
  normalWithMechanic:[normal.filter(l=>l.recipe!=='none').length,normal.length],types:{one:types(1),two:types(2),three:types(3)},sides:cnt};}
function plan(a,b){const placed=placedLevels(),positions=[],blockers=[],plans=mechanicsPlans();
 for(let n=a;n<=b;n++){const s=slotOf(n),st=stageOf(n),pl=planOf(n,plans);positions.push({level:n,cls:s.cls,intro:s.intro,allowed:s.allowed.filter(m=>SUPPORTED.has(m)),stage:st,placed:!!placed[n],picture:placed[n]?.picture||null,...(pl?{plan:pl}:{})});
  if(s.intro&&!SUPPORTED.has(s.intro))blockers.push(`L${n}: знакомство «${MECH_RU[s.intro]}» — движок и пилот этой механики ещё не готовы (§14)`);}
 const mech=breakdown(positions.filter(p=>p.plan).map(p=>p.plan));
 const open=positions.filter(p=>!p.placed),pics=freePictures().map(p=>{const m=measure(p),f=fitness(p),board=boardOf(p);return {id:p.id,title:p.title,pic:p,N:m.N,colours:m.colours,bin:sizeBin(m.N),strong:f.hiddenColours>=3&&!weakOf(f),weak:weakOf(f),stream:!!board.hero,side:bridgeSide(board),family:p.family||null,motifs:motifsOf(p),fits:open.filter(q=>fitsStage(m,q.level)).map(q=>q.level)};});
 // under a mechanics plan: a bridge position of strip "new" needs a picture with its own strip crossing to the planned
 // side; an overlay position takes a free pool picture with the generator's wall; the rest take free pool pictures
 // a gap position (17 §8 «Одна цельная картинка», 2026-10-29) a split of one whole free pool picture
 const isNew=q=>q.plan&&q.plan.bridgeSide&&q.plan.strip!=='overlay'&&q.plan.strip!=='gap',isOverlay=q=>q.plan&&q.plan.bridgeSide&&q.plan.strip==='overlay',isGap=q=>q.plan&&q.plan.bridgeSide&&q.plan.strip==='gap';
 const poolOpen=open.filter(q=>!isNew(q)&&!isGap(q)),poolPics=pics.filter(p=>!p.stream&&p.fits.length);
 const fitPics=pics.filter(p=>p.fits.length);
 // overlay supply per side: free pool pictures where the wall lies and the board with it fits the position's stage
 // (Hard and Super Hard: 3+ colours hidden at the start on that board)
 const ovSupply={};for(const q of open.filter(isOverlay)){const side=q.plan.bridgeSide,k=`L${q.level} ${SIDE_RU[side]}`;ovSupply[k]=0;
  for(const p of pics.filter(p=>!p.stream)){const ov=overlayOf(p.pic,side);if(!ov)continue;const op=overlaid(p.pic,ov),m=measure(op);if(!fitsStage(m,q.level))continue;if(q.cls!=='Normal'&&fitness(op).hiddenColours<3)continue;ovSupply[k]++;}}
 // the bank: candidates whose picture is still free, by measured class and introduction
 const free=new Set(fitPics.map(p=>p.id)),bank={};for(const f of fs.existsSync(BANK)?fs.readdirSync(BANK):[]){const bk=readJSON(path.join(BANK,f));if(!free.has(bk.picture))continue;for(const c of bk.candidates){const k=c.intro?`знакомство: ${c.recipe}`:c.cls;bank[k]=(bank[k]||0)+1;}}
 const need={Normal:poolOpen.filter(p=>p.cls==='Normal'&&!p.intro).length,Hard:poolOpen.filter(p=>p.cls==='Hard').length,'Super Hard':poolOpen.filter(p=>p.cls==='Super Hard').length,intro:open.filter(p=>p.intro).length,bridge:open.filter(p=>p.intro==='bridge').length};
 // the order (17 §13): 1.5–2 pictures a position; strong ones (3+ colours hidden at the start) twice the Hard and Super
 // Hard positions; three stream pictures for each bridge introduction; the size mix of §2 per 30 (6 / 15 / 9)
 const want=Math.ceil(1.75*poolOpen.length),wantStrong=2*(need.Hard+need['Super Hard']),wantStream=3*need.bridge;
 const have=poolPics.length,haveStrong=poolPics.filter(p=>p.strong).length,haveStream=fitPics.filter(p=>p.stream).length;
 const total=Math.max(0,want-have),strong=Math.max(0,wantStrong-haveStrong),streams=Math.max(0,wantStream-haveStream);
 // new strip pictures by crossing side and size stage, 1.75 a position, less the free ones with their own strip that fit
 const strips=[];for(const q of open.filter(isNew)){const side=q.plan.bridgeSide,sk=stageKey(q.stage);let e=strips.find(x=>x.side===side&&x.stage===sk);if(!e)strips.push(e={side,stage:sk,levels:[],strongLevels:[]});e.levels.push(q.level);if(q.cls!=='Normal')e.strongLevels.push(q.level);}
 for(const e of strips){e.have=fitPics.filter(p=>p.stream&&p.side===e.side&&e.levels.some(n=>p.fits.includes(n))).length;e.count=Math.max(0,Math.ceil(1.75*e.levels.length)-e.have);e.strong=Math.min(e.count,Math.max(0,2*e.strongLevels.length-fitPics.filter(p=>p.stream&&p.strong&&p.side===e.side).length));}
 const mix={'200–499':6/30,'500–999':15/30,'1000–1400':9/30},byStage={};
 for(const q of poolOpen){const k=q.stage.fixed?`L${q.level} (фиксированная)`:stageKey(q.stage);byStage[k]=(byStage[k]||0)+1;}
 // the order split by stage in proportion to the open positions, rounded so the parts add up to the total
 const parts=Object.entries(byStage).map(([k,v])=>[k,v/poolOpen.length*total]),orderByStage=Object.fromEntries(parts.map(([k,x])=>[k,Math.floor(x)]));
 let rest=total-Object.values(orderByStage).reduce((x,y)=>x+y,0);for(const [k] of parts.sort((x,y)=>(y[1]%1)-(x[1]%1))){if(rest<=0)break;orderByStage[k]++;rest--;}
 for(const k of Object.keys(orderByStage))if(!orderByStage[k])delete orderByStage[k];
 const all=manifest().pictures.filter(e=>e.status==='approved').map(e=>picture(e.id)),motifCount={};for(const p of all)for(const m of motifsOf(p))motifCount[m]=(motifCount[m]||0)+1;
 const avoid=Object.entries(motifCount).filter(([,n])=>n>=3).map(([m,n])=>`${m} (${n})`);
 const themes=readJSON(path.join(HERE,'themes.json'),{groups:[]}),famCount={};for(const p of all)if(p.family)famCount[p.family]=(famCount[p.family]||0)+1;
 const families=themes.groups.flatMap(g=>g.families.map(f=>f.name)).sort((x,y)=>(famCount[x]||0)-(famCount[y]||0)).slice(0,12).map(f=>`${f} (${famCount[f]||0})`);
 const stripTotal=strips.reduce((x,e)=>x+e.count,0);
 // bridge scenes (tools/rounds/bridge-scene.mjs, the main path since 2026-10-28): each new-strip bridge position takes a
 // scene composed from a smaller free pool picture (the far side behind a wall, crossed strips, near-bank beds) and searched
 // until its planned bridge kind holds; approved free scene pictures of that side and stage already count
 const sceneSources=pics.filter(p=>!p.stream&&p.N>=200&&p.N<=800).length;
 const scenes=open.filter(isNew).map(q=>{const side=q.plan.bridgeSide,kind=q.plan.bridgeKind||'single',have=fitPics.filter(p=>p.stream&&p.pic.scene&&p.pic.scene.mode!=='split'&&p.side===side&&p.fits.includes(q.level)).length;
  return {level:q.level,cls:q.cls,side,kind,recipe:q.plan.recipe,have,command:`node tools/rounds/bridge-scene.mjs make --side ${side} --kind ${kind} --cls "${q.cls}" --recipe ${q.plan.recipe} --level ${q.level} --free --out content/rounds/pictures/src/scenes-NNNN.mjs`};});
 const sceneTotal=scenes.filter(x=>!x.have).length;
 // split pictures (bridge-scene.mjs make --mode split, the owner, 2026-10-29): each gap position takes one whole free pool
 // picture of its stage, split by a neutral strip on the planned side and searched until its bridge kind holds. Suggested
 // sources: free pool pictures without a strip that fit the stage and split to that side (Hard and Super Hard: 3+ colours
 // hidden at the start on the split board), up to three, a family and motifs not yet first choice of another gap position
 // first (the owner, 2026-10-29: a family or motif once per wave);
 // approved free split pictures of that side and stage already count
 const firstFam=new Set(),firstMotif=new Set(),splitCache=new Map(),canSplit=(p,side)=>{const k=p.id+'|'+side;if(!splitCache.has(k))splitCache.set(k,BS.splittable(p.pic,side));return splitCache.get(k);};
 const splits=open.filter(isGap).map(q=>{const side=q.plan.bridgeSide,kind=q.plan.bridgeKind||'single',strongNeed=q.cls!=='Normal',have=fitPics.filter(p=>p.stream&&p.pic.scene&&p.pic.scene.mode==='split'&&p.side===side&&p.fits.includes(q.level)).length;
  const taken=p=>(firstFam.has(p.family)?1:0)+(p.motifs.some(m=>firstMotif.has(m))?1:0),fit=pics.filter(p=>!p.stream&&p.fits.includes(q.level)).sort((x,y)=>taken(x)-taken(y)||x.id.localeCompare(y.id)),sources=[];
  for(const p of fit){if(sources.length>=3)break;const sp=canSplit(p,side);if(!sp||(strongNeed&&sp.hiddenColours<3))continue;sources.push(p);}
  if(sources[0]){if(sources[0].family)firstFam.add(sources[0].family);for(const m of sources[0].motifs)firstMotif.add(m);}
  return {level:q.level,cls:q.cls,side,kind,recipe:q.plan.recipe,have,sources:sources.map(p=>p.id),command:`node tools/rounds/bridge-scene.mjs make --mode split --from ${sources.map(p=>p.id).join(',')||'P####'} --side ${side} --kind ${kind} --cls "${q.cls}" --recipe ${q.plan.recipe} --level ${q.level} --out content/rounds/pictures/src/split-${lid(q.level)}.mjs`};});
 const splitTotal=splits.filter(x=>!x.have).length;
 const out={range:[a,b],created:new Date().toISOString(),positions:positions.map(({plan:pl,...p})=>p),open:open.map(p=>p.level),blockers,...(mech?{mechanics:mech}:{}),need,supply:{pictures:have,strong:haveStrong,stream:haveStream,bank,...(mech?{overlay:ovSupply}:{})},
  order:{total,strong,streams,byStage:orderByStage,sizeMix:Object.fromEntries(Object.entries(mix).map(([k,v])=>[k,Math.round(v*total)])),...(mech?{strips:strips.map(({levels,strongLevels,...e})=>({...e,levels})),scenes,sceneSources,splits}:{}),avoidMotifs:avoid,suggestFamilies:families}};
 const dir=waveDir(a,b);writeJSON(path.join(dir,'plan.json'),out);
 const mechMd=mech?['## Разбор допов','',`Утверждён владельцем (\`runs/${lid(a)}-${lid(b)}/mechanics.json\`, 17 §13 шаг 1). Расстановка берёт на позицию только кандидата с этим рецептом и, для моста, с этой стороной перехода. Дозы по §7: ведущая — свой диапазон (при трёх типах нижняя половина), дополнительная — минимум; место дозы по кругу §7.`,'',
  '| Ур. | Класс | Ведущая | Дополнительные | Дозы | Мост: сторона | Полоса |','|---|---|---|---|---|---|---|',
  ...mech.levels.map(l=>`| ${l.level} | ${l.cls} | ${l.lead?MRU[l.lead]:'—'} | ${l.add.map(m=>MRU[m]).join(', ')||'—'} | ${l.doses} | ${l.bridgeSide?SIDE_RU[l.bridgeSide]:''} | ${l.strip?STRIP_RU[l.strip]||l.strip:''} |`),'',
  `- Доли: ${['bridge','links','hidden'].map(m=>`${MRU[m]} — ${mech.shares[m].levels} (${Math.round(mech.shares[m].share*100)} %, ведущая в ${mech.shares[m].lead})`).join('; ')}; без допов — ${mech.shares.none.levels} (${Math.round(mech.shares.none.share*100)} %).`,
  `- Normal с допом: ${mech.normalWithMechanic[0]} из ${mech.normalWithMechanic[1]}. Типов на уровне: один — ${mech.types.one}, два — ${mech.types.two}, три — ${mech.types.three}.`,
  `- Стороны мостов: ${Object.entries(mech.sides).map(([s,n])=>`${SIDE_RU[s]} — ${n}`).join('; ')||'—'}.`,'']:[];
 const md=[`# План волны L${a}–L${b}`,'',`Создан ${out.created.slice(0,16).replace('T',' ')}. Правила: 17 §13. Позиций ${positions.length}, открыто ${open.length}.`,'',
  blockers.length?`**Не собираются, пока движок не поддержит механику:** ${blockers.join('; ')}. Остальные позиции волны собираются.`:'Все механики открытых позиций движок поддерживает.','',
  ...mechMd,
  '## Позиции','','| Уровень | Класс | Знакомство | Разрешено | Этап | Занято |','|---|---|---|---|---|---|',
  ...positions.map(p=>`| ${p.level} | ${p.cls} | ${p.intro?MECH_RU[p.intro]:''} | ${p.allowed.map(m=>MECH_RU[m]).join(', ')||'без допов'} | ${p.stage.fixed?'фикс.':`${p.stage.cells.join('–')} / ${p.stage.colours.join('–')}`} | ${p.placed?p.picture:''} |`),
  '','## Что уже есть','',`- Свободных утверждённых картинок под этапы волны: ${fitPics.length} (без полосы ${have}, из них сильных ${haveStrong}; с полосой ${haveStream}).`,
  `- Кандидаты в банке на свободных картинках: ${Object.entries(bank).map(([k,v])=>`${k} — ${v}`).join('; ')||'нет'}.`,
  `- Открытые позиции на картинках пула: Normal ${need.Normal}, Hard ${need.Hard}, Super Hard ${need['Super Hard']}, знакомства ${need.intro} (мост ${need.bridge}).`,
  ...(mech&&Object.keys(ovSupply).length?[`- Стена поверх картинки пула (сколько свободных картинок её берут и подходят этапу; Hard/SH — сильные с ней): ${Object.entries(ovSupply).map(([k,v])=>`${k} — ${v}`).join('; ')}.`]:[]),
  '','## Заказ картинок','',total+strong+streams+stripTotal+splitTotal?(total?`- Картинки пула: **${total}** (ориентир 1,75 на позицию).`:'- Картинок пула хватает.'):'- Заказ не нужен: картинок хватает.',
  ...(total?[`- По этапам: ${Object.entries(orderByStage).map(([k,v])=>`${k} — ${v}`).join('; ')}.`,`- По размеру (§2, на 30: 6 / 15 / 9): ${Object.entries(out.order.sizeMix).map(([k,v])=>`${k} — ${v}`).join('; ')}.`]:[]),
  ...(strong?[`- Из них сильных (3+ скрытых цвета на старте, под Hard и Super Hard): **${strong}**.`]:[]),
  ...(streams?[`- С ручьём и двумя местами моста: **${streams}**. За каждым местом свой участок дальнего берега, чтобы со второго знакомства решал один мост (§8).`]:[]),
  ...(scenes.length?[`- **Сцены моста: нужно ${sceneTotal}** из ${scenes.length} позиций (\`bridge-scene.mjs make\`: картинка пула поменьше на дальнем берегу за стеной, две полосы за местами, грядки на ближнем берегу; поиск до вида моста по плану; черновик модуля → лист владельцу; источников в пуле — ${sceneSources}):`,
   ...scenes.map(x=>`  - L${x.level} ${x.cls}, ${x.recipe}, переход ${SIDE_RU[x.side]}, ${x.kind==='better'?'оба выигрывают, один лучше на 10+':x.kind==='any'?'ровно один или оба (один лучше на 10+)':'решает ровно один'}${x.have?` — уже есть ${x.have}`:`: \`${x.command}\``}`),
   '  - Вместо сцены можно нарисовать картинку с полосой вручную (ниже — сколько их понадобилось бы).']:[]),
  ...(splits.length?[`- **Разделённые картинки: нужно ${splitTotal}** из ${splits.length} позиций (\`bridge-scene.mjs make --mode split\`: одна цельная картинка пула, разделённая вставленной полосой в цвет фона, два места моста на полосе, клетки картинки не теряются; поиск разреза и очереди до вида моста по плану; черновик модуля → лист владельцу; 17 §8 «Одна цельная картинка»):`,
   ...splits.map(x=>`  - L${x.level} ${x.cls}, ${x.recipe}, переход ${SIDE_RU[x.side]}, ${x.kind==='better'?'оба выигрывают, один лучше на 10+':x.kind==='any'?'ровно один или оба (один лучше на 10+)':'решает ровно один'}${x.have?` — уже есть ${x.have}`:`${x.sources.length?'':' (подходящей свободной картинки пула нет)'}: \`${x.command}\``}`)]:[]),
  ...(stripTotal?[`- **Новые картинки с полосой и двумя местами моста: ${stripTotal}** (полоса — ручей, забор, стена; опора каждого места на ближнем берегу — цветная клетка; подходы из разных материалов; §8):`,
   ...strips.filter(e=>e.count).map(e=>`  - переход ${SIDE_RU[e.side]}${e.side==='two'?' (два места на разных полосах или в разные стороны)':''}, ${e.stage}: **${e.count}** для L${e.levels.join(', L')}${e.strong?`, из них сильных ${e.strong}`:''}${e.have?` (уже есть ${e.have})`:''}`)]:[]),
  `- Не рисовать (мотив 3+ раз): ${avoid.join(', ')||'—'}.`,`- Реже всего в пуле семьи: ${families.join(', ')}.`,''].join('\n');
 fs.writeFileSync(path.join(dir,'plan.md'),md);console.log(md);return out;}
const ovCache=new Map();const overlayOf=(p,side)=>{const k=p.id+'|'+side;if(!ovCache.has(k))ovCache.set(k,wallOverlay(p,side,1));return ovCache.get(k);};

// ---- run ----
async function runWave(a,b){const dir=waveDir(a,b),jobs=+arg('--jobs',Math.max(1,Math.min(4,os.cpus().length))),t0=Date.now();fs.mkdirSync(path.join(dir,'build'),{recursive:true});
 const state=readJSON(path.join(dir,'state.json'),{steps:[]}),step=(name,extra={})=>{state.steps.push({name,at:new Date().toISOString(),...extra});writeJSON(path.join(dir,'state.json'),state);console.log(`[${((Date.now()-t0)/60000).toFixed(1)} мин] ${name}`,Object.keys(extra).length?JSON.stringify(extra):'');};
 // positions whose introduction the engine does not play stay open and are reported (17 §14); the rest of the wave runs
 const p=plan(a,b);if(p.blockers.length)step('позиции без поддержки движка остаются открытыми',{blockers:p.blockers});
 // bridge positions of strip "new" take approved scene pictures (bridge-scene.mjs); those without one stay open until made
 const noScene=(p.order.scenes||[]).filter(x=>!x.have);if(noScene.length)step('сцены моста ещё не сделаны и не утверждены (bridge-scene.mjs make)',{levels:noScene.map(x=>x.level)});
 // gap positions take approved split pictures (bridge-scene.mjs make --mode split); those without one stay open until made
 const noSplit=(p.order.splits||[]).filter(x=>!x.have);if(noSplit.length)step('разделённые картинки ещё не сделаны и не утверждены (bridge-scene.mjs make --mode split)',{levels:noSplit.map(x=>x.level)});
 // 1. what the bank fills already: a placement on a copy of content/rounds (with the waves' mechanics plans)
 const dry=async()=>{const tmp=fs.mkdtempSync(path.join(os.tmpdir(),'rounds-dry-'));fs.cpSync(ROUNDS,tmp,{recursive:true,filter:s=>!s.includes(`${path.sep}runs${path.sep}`)||s.endsWith(`${path.sep}mechanics.json`)||fs.statSync(s).isDirectory()});
  await run1([path.join(HERE,'levels.mjs'),'place',String(a),String(b)],{ROUNDS_DIR:tmp});const needs=(readJSON(path.join(tmp,'needs.json'),{needs:[]}).needs||[]).filter(x=>x.level>=a&&x.level<=b);fs.rmSync(tmp,{recursive:true,force:true});return needs;};
 let needs=await dry();step('пробная расстановка',{open:needs.map(x=>x.level)});
 // --all: candidates for every planned position, not only the open ones (when the wave's sequence checks fail on what
 // the bank already holds, e.g. the Normal median or the greedy quota, fresh candidates give the placement a choice)
 if(flag('--all')){const plan=readJSON(path.join(dir,'mechanics.json'),null);if(plan)for(const e of plan.levels){if(e.level<a||e.level>b||needs.some(x=>x.level===e.level))continue;
  needs.push({level:e.level,cls:e.cls,recipe:e.recipe,...(e.bridgeSide?{bridgeSide:e.bridgeSide,strip:e.strip}:{}),stage:null});}}
 // 2. candidates only for what stays open: free pictures of those stages, the classes and recipes those positions need;
 // a position of the mechanics plan only its planned recipe: a bridge on a wall of the planned side over a free pool
 // picture (strip "overlay") or on a picture with its own strip crossing to that side (strip "new"). One build process
 // per picture (its bank file), with the exact class:recipe(@side) pairs
 if(needs.length&&!flag('--no-build')){const pics=freePictures().map(pc=>{const board=boardOf(pc);return {pc,m:measure(pc),f:fitness(pc),stream:!!board.hero,side:bridgeSide(board)};}),byPic=new Map();
  const add=(id,variant,cls,recipe)=>{let t=byPic.get(id);if(!t)byPic.set(id,t={id,variants:new Set(),pairs:new Set(),classes:new Set(),recipes:new Set()});t.variants.add(variant);t.pairs.add(`${cls}:${recipe}${variant==='none'?'':'@'+variant}`);t.classes.add(cls);t.recipes.add(recipe);};
  const strong=(x,f)=>x.cls==='Normal'||f.hiddenColours>=3;
  // --no-bridge leaves the planned bridge positions for later (they need scene pictures); --per-position N tries at most
  // N pictures per planned position (the strongest first for Hard and Super Hard, else spread by the level number)
  const perPos=+arg('--per-position','0'),noBridge=flag('--no-bridge');
  for(const x of needs.filter(x=>x.recipe)){const bridge=x.recipe.split('+').includes('bridge');if(bridge&&noBridge)continue;
   let pool=pics;if(perPos>0){pool=pics.filter(q=>bridge?true:!q.stream&&fitsStage(q.m,x.level)&&strong(x,q.f));
    pool=x.cls==='Normal'?pool.map((q,k)=>({q,k:(k*7919+x.level*104729)%9973})).sort((u,v)=>u.k-v.k).map(u=>u.q):pool.slice().sort((u,v)=>v.f.hiddenColours-u.f.hiddenColours);
    if(!bridge)pool=pool.slice(0,perPos);}
   for(const {pc,m,f,stream,side} of pool){
    if(bridge&&x.strip==='overlay'){if(stream)continue;const ov=overlayOf(pc,x.bridgeSide);if(!ov)continue;const op=overlaid(pc,ov);if(!fitsStage(measure(op),x.level)||!strong(x,fitness(op)))continue;add(pc.id,x.bridgeSide,x.cls,x.recipe);}
    else{if(bridge?!(stream&&side===x.bridgeSide):stream)continue;if(bridge&&x.strip==='gap'&&!(pc.scene&&pc.scene.mode==='split'))continue;if(!fitsStage(m,x.level)||!strong(x,f))continue;add(pc.id,'none',x.cls,x.recipe);}}}
  // positions without a plan: as before, every allowed recipe of the classes those positions need
  for(const {pc,m,f,stream} of pics){const mine=needs.filter(x=>!x.recipe&&fitsStage(m,x.level)&&strong(x,f)&&(x.intro!=='bridge'||stream));if(!mine.length)continue;
   const classes=[...new Set(mine.map(x=>x.cls))],allowed=new Set(mine.flatMap(x=>x.intro?[x.intro]:x.allowed));
   const rec=[];for(const r of ['none','hidden','links','links+hidden',...(stream?['bridge','bridge+links']:[])])if(r.split('+').every(t=>t==='none'||allowed.has(t)))rec.push(r);
   for(const r of rec.filter(r=>classes.includes('Normal')||(r!=='none')))for(const c of classes)add(pc.id,'none',c,r);}
  const tasks=[...byPic.values()],noIntro=!needs.some(x=>x.intro);
  step('сборка кандидатов',{pictures:tasks.length,jobs});let i=0,done=0;
  await Promise.all(Array.from({length:jobs},async()=>{while(i<tasks.length){const t=tasks[i++],ov=[...t.variants];
   const r=await run1([path.join(HERE,'levels.mjs'),'build',t.id,'--classes',[...t.classes].join(','),'--recipes',[...t.recipes].join(','),'--pairs',[...t.pairs].join(','),...(ov.some(v=>v!=='none')?['--overlay',ov.join(',')]:[]),...(noIntro?['--no-intro']:[]),'--candidates',arg('--candidates','64')]);
   fs.writeFileSync(path.join(dir,'build',t.id+'.log'),r.out);done++;if(done%5===0||done===tasks.length)console.log(`  собрано ${done}/${tasks.length}`);}}));
  step('сборка готова');}
 // 3. checks missing on older candidates, then 4. the placement
 await Promise.all(Array.from({length:jobs},(_,k)=>run1([path.join(HERE,'levels.mjs'),'checks','--shard',`${k}/${jobs}`])));step('проверки кандидатов');
 const pl=await run1([path.join(HERE,'levels.mjs'),'place',String(a),String(b)]);fs.writeFileSync(path.join(dir,'place.log'),pl.out);
 needs=(readJSON(path.join(ROUNDS,'needs.json'),{needs:[]}).needs||[]).filter(x=>x.level>=a&&x.level<=b);step('расстановка',{open:needs.map(x=>x.level)});
 // 5. the audit of everything placed in the wave (17 §14)
 const au=await run1([path.join(HERE,'audit.mjs'),String(a),String(b),'--out',path.join(dir,'audit.md')]);fs.copyFileSync(path.join(dir,'audit.md'),path.join(ROUNDS,`audit-${lid(a)}-${lid(b)}.md`));
 const failed=(au.out.match(/\| НЕТ: /g)||[]).length+(/Последовательность[^\n]*: OK/.test(au.out)?0:1);step('проверка правил',{failed});
 // 6. the summary and 7. the owner's sample
 const placed=placedLevels(),rows=[];for(let n=a;n<=b;n++)if(placed[n])rows.push(placed[n]);
 const sample=flag('--no-sample')?[]:pickSample(rows,placed);
 if(sample.length){const r=await run1([PILOT,'--levels',sample.join(','),'--out',path.join(dir,'sample'),'--name',`wave-${a}-${b}`]);fs.writeFileSync(path.join(dir,'sample.log'),r.out);step('выборка',{levels:sample});}
 fs.writeFileSync(path.join(dir,'summary.md'),summary(a,b,rows,needs,au.out,sample,placed,(Date.now()-t0)/60000));
 step('готово',{minutes:Math.round((Date.now()-t0)/60000)});console.log(fs.readFileSync(path.join(dir,'summary.md'),'utf8'));
 return needs.length||failed?1:0;}

// the owner's sample (quality.json "sample"): every Super Hard, two Hard, every introduction, two random regular Normals
// and the two levels closest to a bound of the metrics (outliers); a fixed seed so a rerun picks the same
function pickSample(rows,placed){const S=QUALITY.sample||{},out=new Set(),rnd=(()=>{let s=rows.length*7919+(rows[0]?.level||1);return()=>{s^=s<<13;s^=s>>>17;s^=s<<5;return (s>>>0)/4294967296;};})();
 const take=(list,k)=>{const l=list.filter(L=>!out.has(L.level));for(let i=0;i<k&&l.length;i++)out.add(l.splice(Math.floor(rnd()*l.length),1)[0].level);};
 const margin=L=>{const pm=L.cls==='Normal'?QUALITY.pressure.min.regularNormal:QUALITY.pressure.min[L.cls],st=L.checks?.pressure?.stuck;return pm&&st!=null?(st-pm)/pm:9;};
 if(S.superHard==='all')rows.filter(L=>L.cls==='Super Hard').forEach(L=>out.add(L.level));take(rows.filter(L=>L.cls==='Hard'),S.hard??2);
 if(S.introductions==='all')rows.filter(L=>L.intro).forEach(L=>out.add(L.level));take(rows.filter(L=>regularAt(L.level,placed)),S.normal??2);
 rows.filter(L=>!L.intro&&!out.has(L.level)&&(L.cls!=='Normal'||regularAt(L.level,placed))).sort((x,y)=>margin(x)-margin(y)).slice(0,S.outliers??2).forEach(L=>out.add(L.level));
 return [...out].sort((x,y)=>x-y);}

function summary(a,b,rows,needs,audit,sample,placed,minutes){const reg=rows.filter(L=>regularAt(L.level,placed)),v=reg.map(L=>L.random.share).sort((x,y)=>x-y),med=v.length?v[Math.floor((v.length-1)/2)]:null;
 const cnt=f=>{const o={};for(const L of rows){const k=f(L);o[k]=(o[k]||0)+1;}return Object.entries(o).map(([k,n])=>`${k} — ${n}`).join('; ');};
 const lost=reg.filter(L=>{const s=L.strategies||{};return !s['most-now|left']&&!s['most-now|right'];}).length,itog=(audit.match(/Итог:[^\n]*/)||['Итог: —'])[0];
 return [`# Волна L${a}–L${b}`,'',`Прогон ${minutes.toFixed(0)} мин. ${itog}`,'',needs.length?`**Не заполнено ${needs.length}:** ${needs.map(x=>`L${x.level} ${x.cls}${x.intro?' ('+MECH_RU[x.intro]+')':''}`).join(', ')} → заказ картинок (\`rounds.mjs plan ${a} ${b}\`).`:'Все позиции заполнены.','',
  '| Ур. | Класс | Картинка | Механики | Бот | Стоят без работы | Вынужденных | Дебют + цвета | «По цветам» |','|---|---|---|---|---|---|---|---|---|',
  ...rows.map(L=>{const k=L.checks||{},s=L.strategies||{};return `| ${L.level} | ${L.cls}${L.intro?' · знакомство':''} | ${L.picture} «${picture(L.picture)?.title||''}» | ${L.recipe}${L.linked?` (связанных ${L.linked})`:''}${L.hidden?` (скрытых ${L.hidden})`:''}${L.definition?.hero?` (мост ${SIDE_RU[bridgeSide(L.definition)]||''}${L.definition.overlay?', стена':''})`:''} | ${pct(L.random.share)} | ${k.pressure?.stuck??'—'} | ${pct(k.forced)} | ${k.opening?`${k.opening.first.wins}/${k.opening.first.openings} · ${pct(k.opening.three.share)}`:'—'} | ${!s['most-now|left']&&!s['most-now|right']?'проигрывает':'выигрывает'} |`;}),
  '','## Распределения','',`- Классы: ${cnt(L=>L.cls)}.`,`- Рецепты: ${cnt(L=>L.recipe)}.`,`- Размеры: ${cnt(L=>sizeBin(L.N))}.`,`- Медиана бота обычных Normal: ${pct(med)} (нужно 50–60 %); «по цветам» проигрывает ${lost} из ${reg.length}.`,
  `- Семьи: ${cnt(L=>picture(L.picture)?.family||'без тега')}.`,'',
  '## Выборка для владельца','',sample.length?`${sample.map(n=>'L'+n).join(', ')} — пилот \`content/rounds/runs/${lid(a)}-${lid(b)}/sample/index.html\`. После игры: \`node tools/rounds/rounds.mjs review <results.json>\`.`:'—','',
  `Проверка правил: [audit.md](audit.md). Лог расстановки: place.log, сборки: build/.`,''].join('\n');}

// ---- review ----
function review(file){const res=readJSON(file),preview=arg('--preview'),date=(res.exportedAt||new Date().toISOString()).slice(0,10),pilot=(res.pilotVersion||'pilot').replace(/-rounds$/,'');
 const keep=['id','level','capacity','config','startedAt','commands','elapsedMs','outcome','logicalOutcome','timeBuckets','workActiveMs','rating','interest','art','planning','depthNeed','pace','waiting','uncertainty','note','endedAt'];
 const pt=path.join(root,'product/playtests',`${pilot}-owner-${date}`);fs.mkdirSync(pt,{recursive:true});
 fs.writeFileSync(path.join(pt,'results.json'),JSON.stringify({...res,note:'Compact copy (rounds.mjs review): telemetry, input events, work events, trip plans and intervals dropped.',attempts:res.attempts.map(x=>Object.fromEntries(keep.filter(k=>k in x).map(k=>[k,x[k]])))}));
 const RU={easy:'легко',normal:'нормально',hard:'трудно',okay:'нормально',fun:'интересно',boring:'скучно'},PLAN={colours:'по цветам','when-stuck':'когда застревал','start-then-colours':'начало, потом цвета','whole-game':'весь уровень'};
 const placed=placedLevels(),levelOf=n=>placed[n]||(preview&&readJSON(path.join(preview,lid(n)+'.json')));const M=QUALITY.mismatch||{},words=(M.noteWords||[]).map(w=>w.toLowerCase());
 const rows=[],mis=[],notes=[];
 for(const n of [...new Set(res.attempts.map(x=>x.level))].sort((x,y)=>x-y)){const A=res.attempts.filter(x=>x.level===n),w=A.filter(x=>x.outcome==='won').at(-1)||A.at(-1),L=levelOf(n);if(!L)continue;
  const k=L.checks||{},reg=regularAt(n,placed),blocked=w.timeBuckets&&w.elapsedMs?Math.round((w.timeBuckets.blockedWorking||0)/w.elapsedMs*100):null;
  const why=[];if(M.hardRatedEasy&&L.cls!=='Normal'&&w.rating==='easy')why.push(`${L.cls} оценён «легко»`);if(M.regularNormalRatedBoring&&reg&&w.interest==='boring')why.push('обычный Normal «скучно»');
  const hit=w.note&&words.filter(x=>w.note.toLowerCase().includes(x));if(hit&&hit.length)why.push(`в заметке: ${hit.join(', ')}`);
  if(why.length)mis.push({n,why,L,k});if(w.note)notes.push({n,note:w.note,rating:`${RU[w.rating]||'—'} · ${RU[w.interest]||'—'}`,L});
  rows.push(`| ${n} | ${L.cls}${L.intro?' · знакомство':''} | ${L.picture} «${picture(L.picture)?.title||''}» | ${A.map(x=>({won:'победа',jammed:'завал',abandoned:'брошена'}[x.outcome]||x.outcome)).join(', ')} | ${Math.round(w.elapsedMs/1000)} с | ${RU[w.rating]||'—'} · ${RU[w.interest]||'—'} | ${PLAN[w.planning]||w.planning||'—'} | ${pct(L.random.share)} | ${k.pressure?.stuck??'—'} | ${blocked??'—'} % | ${k.opening?`${k.opening.first.wins}/${k.opening.first.openings} · ${pct(k.opening.three.share)}`:'—'} | ${w.note||''} |`);}
 const out=path.join(ROUNDS,'reports',`${pilot}-review-${date}.md`);
 const md=[`# Разбор результатов ${pilot} (${date})`,'',`Скрипт: \`rounds.mjs review\`. Копия результатов: \`${path.relative(root,pt)}/results.json\`. Границы слоя Б: \`content/rounds/quality.json\`.`,'',
  '| Ур. | Класс | Картинка | Попытки | Время | Оценка | План | Бот | Стоят без работы | Места заняты (вы) | Дебют + цвета | Заметка |','|---|---|---|---|---|---|---|---|---|---|---|---|',...rows,'',
  '## Расхождения с метриками','',mis.length?mis.map(x=>`- **L${x.n}** (${x.L.cls}): ${x.why.join('; ')}. Метрики: бот ${pct(x.L.random.share)}, стоят без работы ${x.k.pressure?.stuck??'—'}${x.k.opening?`, дебют + цвета после трёх ${pct(x.k.opening.three.share)}`:''}. Что поправить: найти метрику, которая это не поймала, и сдвинуть её границу в quality.json; перепроверить волну.`).join('\n'):'Нет: оценки владельца согласуются с метриками.','',
  '## Черновик для журнала заметок (`content/rounds/owner-notes.md`)','',notes.length?['| Ур. | Оценка | Заметка | Что берём в генерацию |','|---|---|---|---|',...notes.map(x=>`| ${x.n} | ${x.rating} | «${x.note}» | (дописать) |`)].join('\n'):'Заметок нет.',''].join('\n');
 fs.mkdirSync(path.dirname(out),{recursive:true});fs.writeFileSync(out,md);console.log(md);console.log(`→ ${path.relative(root,out)}`);return mis.length?1:0;}

// ---- confirm ----
// the sample is every level of the wave the results file has attempts on; the results must be played on this engine
function confirm(a,b,file,date=new Date().toISOString().slice(0,10),word=null){const fail=m=>{console.error(`confirm ${a}–${b} refused: ${m}`);return 1;};
 if(!(Number.isInteger(a)&&Number.isInteger(b)&&a>=1&&b>=a))return fail('usage: rounds.mjs confirm <from> <to> --sample <results.json> | --owner-word "<words>"');
 if(file&&word)return fail('give --sample or --owner-word, not both');
 if(word!==null){if(!word.trim())return fail('--owner-word needs the owner\'s words');
  const placed=placedLevels(),open=[];for(let n=a;n<=b;n++)if(!placed[n])open.push(n);if(open.length)return fail(`levels not placed: ${open.map(lid).join(', ')}`);
  const out={format:'round-confirmation.v1',wave:[a,b],sample:[],results:null,pilot:null,resultsExportedAt:null,attempts:0,won:[],engine:fingerprints(),confirmedBy:'owner',date,
   ownerWord:word.trim(),note:'the owner asked to move the wave into the game before playing its sample; the phone check after publishing is the first play'};
  const dest=path.join(waveDir(a,b),'confirmed.json');writeJSON(dest,out);
  console.log(`→ ${path.relative(root,dest)}: L${a}–L${b} on the owner's word «${out.ownerWord}» (no sample played)`);return 0;}
 if(!file)return fail('--sample <results.json> or --owner-word "<words>" is required');
 const f=path.resolve(root,file),res=readJSON(f);if(!res||!Array.isArray(res.attempts))return fail(`no pilot results in ${file}`);
 const sample=[...new Set(res.attempts.map(x=>x.level).filter(n=>n>=a&&n<=b))].sort((x,y)=>x-y);if(!sample.length)return fail(`${file} has no attempt on L${a}–L${b}`);
 const placed=placedLevels(),open=sample.filter(n=>!placed[n]);if(open.length)return fail(`sample levels not placed: ${open.map(lid).join(', ')}`);
 const engine=fingerprints(),played={rules:res.rulesHash,timeline:res.timelineHash,palette:res.paletteHash,clock:res.clockHash};
 const stale=sameEngine(played,engine,a,b);if(stale.length)return fail(`the sample was played on another engine (${stale.join(', ')} differ)`);
 const attempts=res.attempts.filter(x=>x.level>=a&&x.level<=b);
 const out={format:'round-confirmation.v1',wave:[a,b],sample,results:path.relative(root,f).split(path.sep).join('/'),pilot:res.pilotVersion||null,resultsExportedAt:res.exportedAt||null,
  attempts:attempts.length,won:[...new Set(attempts.filter(x=>x.outcome==='won').map(x=>x.level))].sort((x,y)=>x-y),engine,confirmedBy:'owner',date};
 const dest=path.join(waveDir(a,b),'confirmed.json');writeJSON(dest,out);
 console.log(`→ ${path.relative(root,dest)}: L${a}–L${b}, sample ${sample.map(n=>'L'+n).join(', ')} (${attempts.length} attempts, ${out.won.length} levels won)`);return 0;}

if(cmd==='plan'){plan(+process.argv[3],+process.argv[4]);}
else if(cmd==='confirm'){process.exitCode=confirm(+process.argv[3],+process.argv[4],arg('--sample'),arg('--date')||undefined,arg('--owner-word'));}
else if(cmd==='run'){process.exitCode=await runWave(+process.argv[3],+process.argv[4]);}
else if(cmd==='review'){process.exitCode=review(process.argv[3]);}
else console.log(fs.readFileSync(new URL(import.meta.url),'utf8').split('\n').filter(l=>l.startsWith('//')).join('\n'));

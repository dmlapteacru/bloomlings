// Pilot v12: the first round levels of product/17_LEVEL_GENERATION_RULES.md (skill bloomlings-round-levels) as one
// offline HTML: this folder's page, UI and style with the round engine (tools/rounds/engine/: rules, timeline, palette,
// clock), so the page plays exactly what the levels were solved and measured on.
//
// node tools/gameplay-pilot-v12/build.mjs [--from 1] [--to 20] [--levels 21,25,30] [--out <folder>] [--name <label>] [--preview <folder>]
//   --levels: only these levels (a wave's owner sample, tools/rounds/rounds.mjs run); --out: write there instead;
//   --name: the label on the page and the ZIP's name (default v12)
//   --preview: a folder of L####.json built the same way from draft pictures (ROUNDS_DIR trial run), used only for
//   positions content/rounds/levels/ does not fill yet; such levels are marked «ЧЕРНОВИК» in the level menu.
// Writes product/previews/gameplay-pilot-v12/index.html, Bloomlings-pilot-<name>.zip and levels.json (the embedded data).
// Does not install levels into the game.
import fs from 'node:fs';import path from 'node:path';import crypto from 'node:crypto';import zlib from 'node:zlib';
import {createRequire} from 'node:module';
import {root,LEVELS,PICTURES,readJSON,lid,arg,boardOf} from '../rounds/lib.mjs';
const SRC=path.dirname(new URL(import.meta.url).pathname),ENGINE=path.join(root,'tools/rounds/engine'),OUT=arg('--out')?path.resolve(arg('--out')):path.join(root,'product/previews/gameplay-pilot-v12');
const list=arg('--levels')?arg('--levels').split(',').map(Number).sort((a,b)=>a-b):null,from=list?list[0]:+arg('--from',1),to=list?list.at(-1):+arg('--to',20),preview=arg('--preview'),NAME=arg('--name','v12');
const IDS=list||Array.from({length:to-from+1},(_,i)=>from+i);
const rangeLabel=list?IDS.map(n=>'L'+n).join(', '):`L${from}–L${to}`;
const sha=t=>crypto.createHash('sha256').update(t).digest('hex'),read=f=>fs.readFileSync(f,'utf8');
const P=createRequire(import.meta.url)(path.join(ENGINE,'palette.cjs')).palette('normal');
const HERO={sprig:'Sprig',bloom:'Bloom',drop:'Drop',twig:'Twig'};

// ---- levels ----
const levels=[],cards=[],drafts=[],exceptions=[],stone={};
for(const n of IDS){let L=readJSON(path.join(LEVELS,lid(n)+'.json')),draft=false;
 if(!L&&preview){L=readJSON(path.join(preview,lid(n)+'.json'));draft=!!L&&!L.exception;}
 // a preview level may stand in for an open position as an explicit exception to a rule (it says which)
 if(L&&L.exception)exceptions.push({level:n,note:L.exception});
 if(!L)throw Error(`нет уровня ${n}: ни в content/rounds/levels, ни в --preview`);
 const d=L.definition;
 // a level on the generator's wall (definition.overlay, 17 §8): its board cells, drawn as stone (kept out of the level
 // objects, whose shape the exporter's catalog hash reads)
 if(d.overlay)stone[n]=boardOf(readJSON(path.join(PICTURES,L.picture+'.json')),d.overlay).stone;
 else if(readJSON(path.join(PICTURES,L.picture+'.json'))?.scene){
  // Generated bridge scenes use stone enclosures; only authored streams keep the water appearance.
  stone[n]=[...d.rowsBottomFirst.join('')].flatMap((c,i)=>c==='#'&&i%d.w>0&&i%d.w<d.w-1&&i>=d.w&&i<d.w*(d.h-1)?[i]:[]);
 }
 levels.push({id:n,title:d.title,subject:L.picture,w:d.w,h:d.h,rowsBottomFirst:d.rowsBottomFirst,entries:d.entries,capacity:d.capacity,pods:d.pods,tray:d.tray,difficulty:L.cls,...(d.hero?{hero:d.hero}:{})});
 // a level stands on a draft when its picture is not approved in the repository (a trial folder may approve drafts)
 {const real=readJSON(path.join(PICTURES,L.picture+'.json'));if(!real||real.status!=='approved')draft=true;}
 if(draft)drafts.push(n);
 cards.push({level:n,bridgeKind:L.checks&&L.checks.bridgeKind||null,bridgeSide:L.bridgeSide||null,overlay:!!d.overlay,cls:L.cls,intro:L.intro,picture:L.picture,recipe:L.recipe,N:L.N,colours:L.colours,P:L.P,columns:L.columns,linked:L.linked,hidden:L.hidden,random:L.random.share,runs:L.random.runs,borderline:!!L.random.borderline,seconds:L.rhythm.seconds,draft});}

// ---- lessons: L1, each new colour, each mechanic's introductions (the second as a reminder), the first Hard and Super Hard ----
const lessons={},tags={},seen=new Set(),introduced=new Set();
const colourText=c=>`${P[c].name} собирает ${HERO[P[c].hero]}.`;
for(const L of levels){const used=[...new Set(L.pods.map(p=>p.color))],fresh=used.filter(c=>!seen.has(c));used.forEach(c=>seen.add(c));
 const card=cards.find(c=>c.level===L.id),texts=[];let title=null,hero=null,swatch=null;
 if(L.id===from){title=from===1?'Первая картинка':'Новая волна';hero=P[used[0]].hero;swatch=used[0];
  texts.push('Нажимай верхние капсулы. Команда выходит из ворот внизу, идёт по дорожке вокруг картинки и собирает доступные клетки своего точного цвета, снаружи внутрь. Число на капсуле — сколько клеток она соберёт. Пять мест ожидания: можно отправлять следующие команды, пока работают прежние.');
  texts.push('Материалы: '+used.map(c=>`${P[c].name} — ${HERO[P[c].hero]}`).join(', ')+'.');}
 else if(fresh.length){title=fresh.length>1?'Новые цвета':`Новый цвет: ${P[fresh[0]].name}`;hero=P[fresh[0]].hero;swatch=fresh[0];
  texts.push(fresh.map(colourText).join(' ')+' Это отдельные материалы: команда собирает только свой точный цвет.');}
 const intro=card.intro&&!introduced.has(card.intro)?card.intro:null,again=card.intro&&introduced.has(card.intro)?card.intro:null;if(card.intro)introduced.add(card.intro);
 // v12 (the owner's second amendment of 2026-10-27): the second introduction repeats the mechanic in a short reminder
 if(again==='hidden'){title='Снова скрытые капсулы';hero='drop';swatch=swatch||'B';texts.unshift('Капсула ? откроется, когда станет верхней в своём столбце. Её цвет заранее не узнать: держи место свободным.');}
 if(again==='links'){title='Снова связки';hero='bloom';swatch=swatch||L.pods.find(p=>p.group).color;texts.unshift('Связка уходит вдвоём: нужны два свободных места, и напарник должен быть доступен.');}
 if(again==='bridge'){title='Снова мостик';hero='sprig';swatch=swatch||'G';texts.unshift(card.bridgeKind==='single'?'Теперь решает выбор: только один мостик ведёт к победе. Посмотри, цвета какого берега нужны ближайшим капсулам.':'Мест снова два, построить можно один. Посмотри, цвета какого берега нужны ближайшим капсулам: с одним из мостиков пройти заметно легче.');}
 if(intro==='hidden'){title='Скрытая капсула';hero='drop';swatch=swatch||'B';
  texts.unshift('Капсула ? прячет цвет и число, пока не станет верхней в своём столбце; тогда она открывается сама, ещё до выбора. Держи свободное место для команды, которая может понадобиться.');}
 if(intro==='links'){title='Связка';hero='bloom';swatch=swatch||L.pods.find(p=>p.group).color;
  texts.unshift('Трубка связывает две капсулы. Тап отправляет обе сразу: нужны два свободных места, а напарник должен быть доступен — наверху своего столбца или прямо под капсулой своей же связки. Оба места освободятся вместе, когда закончит вся пара.');}
 if(intro==='bridge'){title='Мостик Sprig';hero='sprig';swatch=swatch||'G';
  texts.unshift('Ручей не перейти. Когда путь к отмеченному месту у берега открыт, нажми кнопку под полем: Sprig вырастит мостик. Мест два, построить можно один, выбор постоянный. Мостик не тратит капсулу и место.');}
 const firstOf=cls=>L.id===(cls==='Hard'?5:15);
 if(firstOf('Hard')){tags[L.id]='ПЕРВЫЙ HARD';title=title||'Первый Hard';texts.push('Этот уровень сложнее: подумай, какие команды отправить раньше и где оставить место. Ретрай бесплатный.');}
 if(firstOf('Super Hard')){tags[L.id]='ПЕРВЫЙ SUPER HARD';title=title||'Первый Super Hard';texts.push('Самый трудный тип уровня: очередь и места нужно планировать заранее. Ретрай бесплатный.');}
 if(title){hero=hero||P[used[0]].hero;swatch=swatch||used[0];lessons[L.id]=[title,hero,swatch,texts.join(' ')];}}

const dataset={version:'pilot-v12-rounds',baseVersion:'pilot v9 UI · round engine (tools/rounds/engine.mjs)',changedLevels:drafts,levels,lessons,lessonTags:tags,cards,stone};
dataset.hash=sha(JSON.stringify(levels));
const [rules,timeline,palette,clock]=['rules','timeline','palette','clock'].map(n=>read(path.join(ENGINE,n+'.cjs')));
Object.assign(dataset,{rulesHash:sha(rules),timelineHash:sha(timeline),paletteHash:sha(palette),clockHash:sha(clock)});

// ---- page: the data-driven texts go into this folder's page.html markers ----
const ui=read(path.join(SRC,'ui.js'));
const intro=cards.filter(c=>c.intro),hard=cards.filter(c=>c.cls!=='Normal');
const MECH={hidden:'капсулы ?',links:'связки',bridge:'мостик Sprig'};
const welcome=`<h2>${list?'Выборка волны: ':'Волна '}${rangeLabel}</h2><div class="welcome-heroes" id="welcomeHeroes"></div><p>Выбирай любой уровень в меню. Отправляй команды, открывай доступ к нужным цветам и оставляй свободные места для следующих капсул.</p>${drafts.length?`<p>Картинки уровней ${drafts.map(n=>'L'+n).join(', ')} — черновики для просмотра, отмеченные в меню.</p>`:''}<ol><li>Можно отправить несколько верхних капсул подряд, пока есть пустые места.</li><li>Команда собирает только точный материал своей капсулы.</li><li>Пока работа недоступна, остаток занимает место. Для пары нужны 2 места; оба освободятся после завершения всей пары.</li><li>Мост строится один раз. Перед выбором посмотри на цвета за ним и на ближайшие капсулы.</li></ol><p>После игры открой «Результаты и заметки»: там можно оценить уровни и скачать результаты.</p>${exceptions.length?`<p>${exceptions.map(e=>`L${e.level}: ${e.note}`).join(' ')}</p>`:''}`;
let page=read(path.join(SRC,'page.html'));
for(const [k,v] of Object.entries({'/*__TITLE__*/':`Bloomlings · ${NAME} · ${levels.length} раундов`,'/*__LABEL__*/':`${NAME.toUpperCase()} · РАУНДЫ · ${levels.length} УРОВНЕЙ`,'/*__FIRST__*/':rangeLabel,'/*__WELCOME__*/':welcome})){if(page.split(k).length!==2)throw Error('page.html: '+k);page=page.split(k).join(v);}
const uri=(f,m)=>`data:${m};base64,`+fs.readFileSync(f).toString('base64'),json=v=>JSON.stringify(v).replace(/</g,'\\u003c');
const heroes=path.join(root,'client/Assets/Bloomlings/Art/Heroes/Resources/HeroMotion'),fonts=path.join(root,'client/Assets/Bloomlings/UI/Fonts/Resources');
const assets=Object.fromEntries(['sprig','bloom','drop','twig'].map(n=>[n,uri(path.join(heroes,`homehero-${n}-001.png`),'image/png')]));
assets.garden=uri(path.join(root,'client/Assets/Bloomlings/Art/Backgrounds/Resources/Backgrounds/gameplay-orchard.jpg'),'image/jpeg');
const css=read(path.join(SRC,'style.css')).replace('__FONT__',uri(path.join(fonts,'Nunito-SemiBold.ttf'),'font/ttf')).replace('__FONT_BOLD__',uri(path.join(fonts,'Nunito-ExtraBold.ttf'),'font/ttf'));
const parts={'/*__STYLE__*/':css,'/*__ASSETS__*/':json(assets),'/*__DATA__*/':json(dataset),'/*__RULES__*/':rules,'/*__UI__*/':ui,'/*__TIMELINE__*/':timeline,'/*__PALETTE__*/':palette,'/*__CLOCK__*/':clock};
for(const [k,v] of Object.entries(parts)){if(page.split(k).length!==2)throw Error('page.html: '+k);page=page.split(k).join(v);}
if(page.includes('/*__'))throw Error('page.html: marker left');
fs.mkdirSync(OUT,{recursive:true});fs.writeFileSync(path.join(OUT,'index.html'),page);
fs.writeFileSync(path.join(OUT,'levels.json'),JSON.stringify(dataset,null,1)+'\n');
// README: the level table from the cards (17 §14), for the owner
const pics=Object.fromEntries(cards.map(c=>[c.picture,readJSON(path.join(PICTURES,c.picture+'.json'))]));
const MECH_ONE={none:'—',hidden:'скрытые',links:'связки',bridge:'мост'},SIDE_RU={up:'вверх',left:'влево',right:'вправо',down:'вниз',two:'две стороны'};
const MECH_RU=new Proxy({},{get:(_,r)=>String(r).split('+').map(m=>MECH_ONE[m]||m).join(' + ')});
const rows=cards.map(c=>`| ${c.level} | ${c.cls}${c.intro?` · знакомство: ${MECH_RU[c.intro]}`:''} | ${c.picture} «${pics[c.picture]?.title||''}»${c.draft?' (черновик)':''} | ${c.N} / ${c.colours} | ${c.P} / ${c.columns} | ${MECH_RU[c.recipe]||c.recipe}${c.bridgeSide?` (${SIDE_RU[c.bridgeSide]||c.bridgeSide}${c.overlay?', стена':''})`:''}${c.linked?` (связанных ${c.linked})`:''}${c.hidden?` (скрытых ${c.hidden})`:''} | ${Math.round(c.random*100)} % из ${c.runs}${c.borderline?' (на границе)':''} | ${c.seconds} с |`).join('\n');
fs.writeFileSync(path.join(OUT,'README.md'),`# Bloomlings ${NAME} — ${rangeLabel}

[Скачать ZIP для телефона](Bloomlings-pilot-${NAME}.zip). Распакуй и открой [index.html](index.html). Сеть не нужна, все уровни открыты в меню. После игры: Уровни → Результаты → скачать JSON.

Уровни построены скилом \`bloomlings-round-levels\` по [правилам 17](../../17_LEVEL_GENERATION_RULES.md) с сохранёнными решениями без бустеров. Результаты проверки находятся в отчёте волны. Страница играет тем же движком раундов (\`tools/rounds/engine/\`), на котором уровни решены и измерены.${drafts.length?` Уровни ${drafts.map(n=>'L'+n).join(', ')} собраны на картинках-черновиках, которые ждут утверждения, и помечены в меню «ЧЕРНОВИК».`:''}

| Уровень | Класс | Картинка | Клетки / цвета | Поды / колонки | Механики | Случайный тест (§5) | Сохранённое решение, ×1 |
|---|---|---|---|---|---|---|---|
${rows}

Случайный тест — доля побед бота, выбирающего равновероятно среди допустимых действий и ожидания (§5); это не человеческий винрейт. Время — сохранённое решение с ожиданием окончания работы (§12).

Исходники: [\`tools/gameplay-pilot-v12\`](../../../tools/gameplay-pilot-v12/build.mjs), уровни: [\`content/rounds/levels\`](../../../content/rounds/levels/). Игра и APK не менялись.
`);

// ---- a stored ZIP (deflate) with the page and a short readme, fixed dates so the archive is reproducible ----
const readme=Buffer.from(`Bloomlings ${NAME} — ${rangeLabel} (правила 17)\n\nРаспакуй ZIP и открой index.html в браузере телефона. Сеть не нужна, все уровни открыты в меню.\nПосле игры: Уровни → Результаты → скачать JSON.\nЭто отдельный пилот: игра и предыдущие версии не меняются.\n`,'utf8');
const crcT=new Uint32Array(256).map((_,n)=>{let c=n;for(let k=0;k<8;k++)c=c&1?0xedb88320^(c>>>1):c>>>1;return c>>>0;});
const crc=b=>{let c=0xffffffff;for(const x of b)c=crcT[(c^x)&255]^(c>>>8);return (c^0xffffffff)>>>0;};
const files=[['index.html',Buffer.from(page,'utf8')],['README.txt',readme]],local=[],central=[];let off=0;
const DOS_T=0,DOS_D=((2026-1980)<<9)|(10<<5)|26;
for(const [name,body] of files){const data=zlib.deflateRawSync(body,{level:9}),nm=Buffer.from(name),c=crc(body);
 const h=Buffer.alloc(30);h.writeUInt32LE(0x04034b50,0);h.writeUInt16LE(20,4);h.writeUInt16LE(0x0800,6);h.writeUInt16LE(8,8);h.writeUInt16LE(DOS_T,10);h.writeUInt16LE(DOS_D,12);h.writeUInt32LE(c,14);h.writeUInt32LE(data.length,18);h.writeUInt32LE(body.length,22);h.writeUInt16LE(nm.length,26);
 local.push(h,nm,data);const e=Buffer.alloc(46);e.writeUInt32LE(0x02014b50,0);e.writeUInt16LE(0x031e,4);e.writeUInt16LE(20,6);e.writeUInt16LE(0x0800,8);e.writeUInt16LE(8,10);e.writeUInt16LE(DOS_T,12);e.writeUInt16LE(DOS_D,14);e.writeUInt32LE(c,16);e.writeUInt32LE(data.length,20);e.writeUInt32LE(body.length,24);e.writeUInt16LE(nm.length,28);e.writeUInt32LE((0o100644<<16)>>>0,38);e.writeUInt32LE(off,42);
 central.push(e,nm);off+=30+nm.length+data.length;}
const cd=Buffer.concat(central),end=Buffer.alloc(22);end.writeUInt32LE(0x06054b50,0);end.writeUInt16LE(files.length,8);end.writeUInt16LE(files.length,10);end.writeUInt32LE(cd.length,12);end.writeUInt32LE(off,16);
fs.writeFileSync(path.join(OUT,`Bloomlings-pilot-${NAME}.zip`),Buffer.concat([...local,cd,end]));
console.log(`${path.relative(root,OUT)}/index.html: ${fs.statSync(path.join(OUT,'index.html')).size.toLocaleString('en')} bytes, ${levels.length} уровней${drafts.length?`, черновики: ${drafts.join(', ')}`:''}; каталог ${dataset.hash.slice(0,12)}, правила ${dataset.rulesHash.slice(0,12)}`);

// Pilot v10: the first round levels of product/17_LEVEL_GENERATION_RULES.md (skill bloomlings-round-levels) as one
// offline HTML. It is the pilot v9 page and UI with the round engine's rules (tools/rounds/engine.mjs roundRules: the
// §8 link rule, boards up to 48×48, 4–5 places), so the page plays exactly what the levels were solved and measured on.
// The v9 sources stay untouched: this script copies them with the patches listed below (each must match once).
//
// node tools/gameplay-pilot-v10/build.mjs [--from 1] [--to 20] [--preview <folder>]
//   --preview: a folder of L####.json built the same way from draft pictures (ROUNDS_DIR trial run), used only for
//   positions content/rounds/levels/ does not fill yet; such levels are marked «ЧЕРНОВИК» in the level menu.
// Writes product/previews/gameplay-pilot-v10/index.html, Bloomlings-pilot-v10.zip and levels.json (the embedded data).
// Does not install levels into the game.
import fs from 'node:fs';import path from 'node:path';import crypto from 'node:crypto';import zlib from 'node:zlib';
import {createRequire} from 'node:module';
import {root,LEVELS,readJSON,lid,arg} from '../rounds/lib.mjs';
import {roundRules} from '../rounds/engine.mjs';
const V9=path.join(root,'tools/gameplay-pilot-v9'),OUT=path.join(root,'product/previews/gameplay-pilot-v10');
const from=+arg('--from',1),to=+arg('--to',20),preview=arg('--preview');
const sha=t=>crypto.createHash('sha256').update(t).digest('hex'),read=f=>fs.readFileSync(f,'utf8');
const P=createRequire(import.meta.url)(path.join(V9,'palette.cjs')).palette('normal');
const HERO={sprig:'Sprig',bloom:'Bloom',drop:'Drop',twig:'Twig'};

// ---- levels ----
const levels=[],cards=[],drafts=[];
for(let n=from;n<=to;n++){let L=readJSON(path.join(LEVELS,lid(n)+'.json')),draft=false;
 if(!L&&preview){L=readJSON(path.join(preview,lid(n)+'.json'));draft=!!L;}
 if(!L)throw Error(`нет уровня ${n}: ни в content/rounds/levels, ни в --preview`);
 const d=L.definition;levels.push({id:n,title:d.title,subject:L.picture,w:d.w,h:d.h,rowsBottomFirst:d.rowsBottomFirst,entries:d.entries,capacity:d.capacity,pods:d.pods,tray:d.tray,difficulty:L.cls,...(d.hero?{hero:d.hero}:{})});
 if(draft)drafts.push(n);
 cards.push({level:n,cls:L.cls,intro:L.intro,picture:L.picture,recipe:L.recipe,N:L.N,colours:L.colours,P:L.P,columns:L.columns,linked:L.linked,hidden:L.hidden,random:L.random.share,runs:L.random.runs,borderline:!!L.random.borderline,seconds:L.rhythm.seconds,draft});}

// ---- lessons: L1, each new colour, each mechanic's first introduction, the first Hard and Super Hard ----
const lessons={},tags={},seen=new Set(),introduced=new Set();
const colourText=c=>`${P[c].name} собирает ${HERO[P[c].hero]}.`;
for(const L of levels){const used=[...new Set(L.pods.map(p=>p.color))],fresh=used.filter(c=>!seen.has(c));used.forEach(c=>seen.add(c));
 const card=cards.find(c=>c.level===L.id),texts=[];let title=null,hero=null,swatch=null;
 if(L.id===from){title='Первая картинка';hero=P[used[0]].hero;swatch=used[0];
  texts.push('Нажимай верхние капсулы. Команда выходит из ворот внизу, идёт по дорожке вокруг картинки и собирает доступные клетки своего точного цвета, снаружи внутрь. Число на капсуле — сколько клеток она соберёт. Пять мест ожидания: можно отправлять следующие команды, пока работают прежние.');
  texts.push('Материалы: '+used.map(c=>`${P[c].name} — ${HERO[P[c].hero]}`).join(', ')+'.');}
 else if(fresh.length){title=fresh.length>1?'Новые цвета':`Новый цвет: ${P[fresh[0]].name}`;hero=P[fresh[0]].hero;swatch=fresh[0];
  texts.push(fresh.map(colourText).join(' ')+' Это отдельные материалы: команда собирает только свой точный цвет.');}
 const intro=card.intro&&!introduced.has(card.intro)?card.intro:null;if(card.intro)introduced.add(card.intro);
 if(intro==='hidden'){title='Скрытая капсула';hero='drop';swatch=swatch||'B';
  texts.unshift('Капсула ? прячет цвет и число, пока не станет верхней в своём столбце; тогда она открывается сама, ещё до выбора. Держи свободное место для команды, которая может понадобиться.');}
 if(intro==='links'){title='Связка';hero='bloom';swatch=swatch||L.pods.find(p=>p.group).color;
  texts.unshift('Трубка связывает две капсулы. Тап отправляет обе сразу: нужны два свободных места, а напарник должен быть доступен — наверху своего столбца или прямо под капсулой своей же связки. Оба места освободятся вместе, когда закончит вся пара.');}
 if(intro==='bridge'){title='Мостик Sprig';hero='sprig';swatch=swatch||'G';
  texts.unshift('Ручей не перейти. Когда путь к отмеченному месту у берега открыт, нажми кнопку под полем: Sprig вырастит мостик. Мест два, построить можно один, выбор постоянный. Мостик не тратит капсулу и место.');}
 const firstOf=cls=>levels.find(x=>x.difficulty===cls)===L;
 if(firstOf('Hard')){tags[L.id]='ПЕРВЫЙ HARD';title=title||'Первый Hard';texts.push('Этот уровень сложнее: подумай, какие команды отправить раньше и где оставить место. Ретрай бесплатный.');}
 if(firstOf('Super Hard')){tags[L.id]='ПЕРВЫЙ SUPER HARD';title=title||'Первый Super Hard';texts.push('Самый трудный тип уровня: очередь и места нужно планировать заранее. Ретрай бесплатный.');}
 if(title){hero=hero||P[used[0]].hero;swatch=swatch||used[0];lessons[L.id]=[title,hero,swatch,texts.join(' ')];}}

const dataset={version:'pilot-v10-rounds',baseVersion:'pilot v9 UI · round engine (tools/rounds/engine.mjs)',changedLevels:drafts,levels,lessons,lessonTags:tags,cards};
dataset.hash=sha(JSON.stringify(levels));
const rules=roundRules(read(path.join(V9,'rules.cjs'))),timeline=read(path.join(V9,'timeline.cjs')),palette=read(path.join(V9,'palette.cjs')),clock=read(path.join(V9,'clock.cjs'));
Object.assign(dataset,{rulesHash:sha(rules),timelineHash:sha(timeline),paletteHash:sha(palette),clockHash:sha(clock)});

// ---- the v9 UI and page with the v10 patches ----
const swapAll=(text,pairs,name)=>{for(const [a,b] of pairs){const k=text.split(a).length-1;if(k<1)throw Error(`${name}: не найдено «${a.slice(0,60)}»`);text=text.split(a).join(b);}return text;};
let ui=read(path.join(V9,'ui.js'));
const a=ui.indexOf(' const LESSONS={'),b=ui.indexOf('\n };\n',a);if(a<0||b<0)throw Error('ui.js: LESSONS moved');
ui=ui.slice(0,a)+' const LESSONS=data.lessons; // v10: built from the levels (build.mjs)'+ui.slice(b+4);
const count=n=>'${data.levels.length}';
ui=swapAll(ui,[
 ['bloomlings-pilot-v9:','bloomlings-pilot-v10:'],
 ["${id===10?'ПЕРВЫЙ SUPER HARD':'НОВЫЙ ШАГ'}","${(data.lessonTags&&data.lessonTags[id])||'НОВЫЙ ШАГ'}"],
 ["state.level.id===30?'Все 30 доступны в выборе уровней.","state.level.id===data.levels.length?`Все ${data.levels.length} доступны в выборе уровней.`+'"],
 ['state.level.id===30','state.level.id===data.levels.length'],
 ['`Пройдено ${won.size} из 30.','`Пройдено ${won.size} из '+count()+'.'],
 ["tag.textContent='НОВОЕ'","tag.textContent='ЧЕРНОВИК'"],
 ["', новый вариант'","', картинка-черновик'"],
 ["'bloomlings-pilot-results-9'","'bloomlings-pilot-results-10'"],
 ['Bloomlings-pilot-v9-synthesis-results-','Bloomlings-pilot-v10-results-']],'ui.js');
const intro=cards.filter(c=>c.intro),hard=cards.filter(c=>c.cls!=='Normal');
const MECH={hidden:'капсулы ?',links:'связки',bridge:'мостик Sprig'};
const welcome=`<h2>Первые ${levels.length} уровней по правилам 17</h2><div class="welcome-heroes" id="welcomeHeroes"></div><p>Новые раунды: картинки из утверждённого набора, очереди собраны генератором и решены. Класс каждого уровня измерен случайным тестом (100–400 партий): Normal — бот выигрывает от 50 %, Hard — 20–50 %, Super Hard — 5–20 %. Календарь: каждый 5-й уровень Hard, 15-й — Super Hard.</p><p>Знакомства: ${[...new Set(intro.map(c=>c.intro))].map(m=>`${MECH[m]} — L${intro.filter(c=>c.intro===m).map(c=>c.level).join(' и L')}`).join('; ')}. Сложные: ${hard.map(c=>`L${c.level} ${c.cls}`).join(', ')}.${drafts.length?` Уровни ${drafts.map(n=>'L'+n).join(', ')} собраны на картинках-черновиках (ждут утверждения) — в меню «ЧЕРНОВИК».`:''}</p><ol><li>Можно отправить несколько верхних капсул подряд, пока есть пустые места.</li><li>Команда собирает только точный материал своей капсулы.</li><li>Пока работа недоступна, остаток занимает место. Для пары нужны 2 места; оба освободятся после завершения всей пары.</li></ol><p class="subtle">Три ряда, пять мест, автоматическое ускорение ×2,5 в конце — как в v9. Внешний вид временный.</p>`;
let page=read(path.join(V9,'page.html'));
page=swapAll(page,[
 ['<title>Bloomlings · v9 сводная · 30 уровней</title>',`<title>Bloomlings · v10 · первые ${levels.length} раундов</title>`],
 ['V9 · СВОДНАЯ · 30 УРОВНЕЙ',`V10 · РАУНДЫ · ${levels.length} УРОВНЕЙ`],
 ['ПЕРВЫЕ 30',`ПЕРВЫЕ ${levels.length}`]],'page.html');
const w0=page.indexOf('<h2>Сводная итерация 9</h2>'),w1=page.indexOf('<button class="primary" id="startButton"');if(w0<0||w1<0)throw Error('page.html: welcome moved');
page=page.slice(0,w0)+welcome+page.slice(w1);

const uri=(f,m)=>`data:${m};base64,`+fs.readFileSync(f).toString('base64'),json=v=>JSON.stringify(v).replace(/</g,'\\u003c');
const heroes=path.join(root,'client/Assets/Bloomlings/Art/Heroes/Resources/HeroMotion'),fonts=path.join(root,'client/Assets/Bloomlings/UI/Fonts/Resources');
const assets=Object.fromEntries(['sprig','bloom','drop','twig'].map(n=>[n,uri(path.join(heroes,`homehero-${n}-001.png`),'image/png')]));
assets.garden=uri(path.join(root,'client/Assets/Bloomlings/Art/Backgrounds/Resources/Backgrounds/gameplay-orchard.jpg'),'image/jpeg');
const css=read(path.join(V9,'style.css')).replace('__FONT__',uri(path.join(fonts,'Nunito-SemiBold.ttf'),'font/ttf')).replace('__FONT_BOLD__',uri(path.join(fonts,'Nunito-ExtraBold.ttf'),'font/ttf'));
const parts={'/*__STYLE__*/':css,'/*__ASSETS__*/':json(assets),'/*__DATA__*/':json(dataset),'/*__RULES__*/':rules,'/*__UI__*/':ui,'/*__TIMELINE__*/':timeline,'/*__PALETTE__*/':palette,'/*__CLOCK__*/':clock};
for(const [k,v] of Object.entries(parts)){if(page.split(k).length!==2)throw Error('page.html: '+k);page=page.split(k).join(v);}
if(page.includes('/*__'))throw Error('page.html: marker left');
fs.mkdirSync(OUT,{recursive:true});fs.writeFileSync(path.join(OUT,'index.html'),page);
fs.writeFileSync(path.join(OUT,'levels.json'),JSON.stringify(dataset,null,1)+'\n');

// ---- a stored ZIP (deflate) with the page and a short readme, fixed dates so the archive is reproducible ----
const readme=Buffer.from(`Bloomlings v10 — первые ${levels.length} уровней раундов (правила 17)\n\nРаспакуй ZIP и открой index.html в браузере телефона. Сеть не нужна, все уровни открыты в меню.\nПосле игры: Уровни → Результаты → скачать JSON.\nЭто отдельный пилот: игра и предыдущие версии не меняются.\n`,'utf8');
const crcT=new Uint32Array(256).map((_,n)=>{let c=n;for(let k=0;k<8;k++)c=c&1?0xedb88320^(c>>>1):c>>>1;return c>>>0;});
const crc=b=>{let c=0xffffffff;for(const x of b)c=crcT[(c^x)&255]^(c>>>8);return (c^0xffffffff)>>>0;};
const files=[['index.html',Buffer.from(page,'utf8')],['README.txt',readme]],local=[],central=[];let off=0;
const DOS_T=0,DOS_D=((2026-1980)<<9)|(10<<5)|26;
for(const [name,body] of files){const data=zlib.deflateRawSync(body,{level:9}),nm=Buffer.from(name),c=crc(body);
 const h=Buffer.alloc(30);h.writeUInt32LE(0x04034b50,0);h.writeUInt16LE(20,4);h.writeUInt16LE(0x0800,6);h.writeUInt16LE(8,8);h.writeUInt16LE(DOS_T,10);h.writeUInt16LE(DOS_D,12);h.writeUInt32LE(c,14);h.writeUInt32LE(data.length,18);h.writeUInt32LE(body.length,22);h.writeUInt16LE(nm.length,26);
 local.push(h,nm,data);const e=Buffer.alloc(46);e.writeUInt32LE(0x02014b50,0);e.writeUInt16LE(0x031e,4);e.writeUInt16LE(20,6);e.writeUInt16LE(0x0800,8);e.writeUInt16LE(8,10);e.writeUInt16LE(DOS_T,12);e.writeUInt16LE(DOS_D,14);e.writeUInt32LE(c,16);e.writeUInt32LE(data.length,20);e.writeUInt32LE(body.length,24);e.writeUInt16LE(nm.length,28);e.writeUInt32LE((0o100644<<16)>>>0,38);e.writeUInt32LE(off,42);
 central.push(e,nm);off+=30+nm.length+data.length;}
const cd=Buffer.concat(central),end=Buffer.alloc(22);end.writeUInt32LE(0x06054b50,0);end.writeUInt16LE(files.length,8);end.writeUInt16LE(files.length,10);end.writeUInt32LE(cd.length,12);end.writeUInt32LE(off,16);
fs.writeFileSync(path.join(OUT,'Bloomlings-pilot-v10.zip'),Buffer.concat([...local,cd,end]));
console.log(`product/previews/gameplay-pilot-v10/index.html: ${fs.statSync(path.join(OUT,'index.html')).size.toLocaleString('en')} bytes, ${levels.length} уровней${drafts.length?`, черновики: ${drafts.join(', ')}`:''}; каталог ${dataset.hash.slice(0,12)}, правила ${dataset.rulesHash.slice(0,12)}`);

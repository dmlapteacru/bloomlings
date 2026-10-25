// Builds product/audits/colony-flow-rounds/site/: the Colony Flow levels of the colony-flow.com walkthrough videos
// (28 videos the owner gave on 2026-10-25) as text another agent can read: levels.json + one storyboard per level.
// Sources, all in tools/colony-deep-research/rounds/site/:
// - L<n>.json: the tray, the attempts, taps, padlock openings, events and mechanics, read from the video's event
//   storyboards (ytsheets.py) and checked with check-round.mjs;
// - boards/L<n>.json: the board from the site's cover picture (coverboard.py → boards/L<n>.cover.json), its colours
//   given the round's letters (map-colours.py);
// - events/L<n>.json: when the tray moved (ytsheets.py), the storyboard's frame times.
// node tools/colony-deep-research/rounds/build-site.mjs
import fs from 'node:fs';
const root=new URL('../../../',import.meta.url),src=new URL('tools/colony-deep-research/rounds/site/',root),out=new URL('product/audits/colony-flow-rounds/site/',root);
const read=(u)=>JSON.parse(fs.readFileSync(u,'utf8'));
const BUCKET='https://pub-dc09455922e746b0979cdaa7317edd10.r2.dev/';
const TITLES={19:'красная птица',27:'машина',34:'пряничный домик',40:'мешок с апельсином',50:'кошечка с гитарой',55:'кусок торта',
 67:'супергерой в маске',100:'ананас',109:'жёлтая утка',113:'корги с напитком',115:'курица',145:'чёрный кот в цветке',165:'часовая башня',
 185:'медвежонок',192:'снеговик',194:'принц в плаще',214:'сладости',245:'кошка на лугу',266:'стакан бабл-ти',290:'домик-цветок',
 350:'бабушка',358:'ковбой',533:'торт со свечами',757:'веер',790:'собачка из шариков',1170:'зверёк в норе',1270:'ягоды',37:'капибара'};
// The site's cover of L350 is another picture (a tiger); the video plays an old woman with a 700 chocolate bar and a 50
// crate, so its board is not taken from the cover.
const COVER_MISMATCH={L350:'the colony-flow.com cover of L350 shows a tiger, the video an old woman with a chocolate bar 700 and a crate 50: the board is not read from the cover'};
const parse=t=>{const k=/^#([a-z]+)$/.exec(t);if(k)return {lock:k[1],token:t};const m=/^([A-Za-z])(\d+)(\?)?(?::([a-z]))?(?:!(\w+))?$/.exec(t);if(!m)throw Error('token '+t);return {color:m[1],count:+m[2],mystery:!!m[3],link:m[4]||null,flag:m[5]||null,token:t};};
const levels=[],report=[];
const names=fs.readdirSync(src).filter(f=>/^L\d+\.json$/.test(f)).sort((a,b)=>parseInt(a.slice(1))-parseInt(b.slice(1)));
for(const f of names){const id=f.slice(0,-5),R=read(new URL(f,src));
 const bu=new URL(`boards/${id}.json`,src),vu=new URL(`boards/${id}.video.json`,src);
 let B=fs.existsSync(bu)&&!COVER_MISMATCH[id]?read(bu):null,cover=B?read(new URL(`boards/${id}.cover.json`,src)):null;
 if(!B&&fs.existsSync(vu)){const V=read(vu),caps={};   // no cover: the board read from a video frame (videoboard.py)
  for(const t of Object.values(R.columns).flat()){const m=/^([A-Za-z])(\d+)/.exec(t);if(m)caps[m[1]]=(caps[m[1]]||0)+ +m[2];}
  const ks=Object.keys(caps),accounting=Object.fromEntries(ks.map(k=>[k,{board:V.cells[k]||0,capsules:caps[k]}]));
  B={w:V.w,h:V.h,rows:V.rows,legend:Object.fromEntries([...ks.map(k=>[k,{name:R.legend[k],hex:V.palette[k]}]),...(V.cells['?']?[['?',{name:'mystery tile (grey "?")',hex:V.palette['?']}]]:[])]),
   accounting,offBy:ks.reduce((n,k)=>n+Math.abs(accounting[k].board-accounting[k].capsules),0),mysteryTiles:V.cells['?']||0,fromVideo:'frame at 2 s, videoboard.py'};}
 const ev=read(new URL(`events/${id}.json`,src));
 // replay every attempt: taps take top capsules (linked groups whole), padlocks leave on their unlock
 const rounds=R.rounds.map(r=>{const cols=Object.fromEntries(Object.entries(R.columns).map(([c,col])=>[c,col.map(parse)])),heads=Object.fromEntries(Object.keys(cols).map(c=>[c,0]));
  const steps=[...r.taps.map(([t,column,note])=>({t,column,note:note||null})),...(r.unlocks||[]).map(([t,column,note])=>({t,column,note,unlock:true}))].sort((a,b)=>a.t-b.t||!!b.unlock-!!a.unlock);
  const taps=[];
  for(const s of steps){const top=cols[s.column][heads[s.column]];if(!top)throw Error(`${id} attempt ${r.attempt}: ${s.column} empty at ${s.t}`);
   if(s.unlock){if(!top.lock)throw Error(`${id} at ${s.t}: unlock without a padlock on top of ${s.column}`);heads[s.column]++;continue;}
   if(top.lock)throw Error(`${id} at ${s.t}: tap on a padlock`);
   const group=top.link?Object.entries(cols).flatMap(([c,col])=>col.map((p,i)=>({c,i,p}))).filter(x=>x.p.link===top.link):[{c:s.column,i:heads[s.column],p:top}];
   group.sort((x,y)=>x.i-y.i);for(const g of group){if(g.i!==heads[g.c])throw Error(`${id} attempt ${r.attempt} at ${s.t}: ${g.c} row ${g.i+1} is not on top`);heads[g.c]++;}
   taps.push({t:s.t,column:s.column,take:group.map(g=>`${g.c} ${g.p.color}${g.p.count}`),note:s.note});}
  const trayLeft=Object.entries(cols).reduce((n,[c,col])=>n+col.length-heads[c],0);if(r.outcome==='won'&&trayLeft)throw Error(`${id}: won with ${trayLeft} left`);
  return {attempt:r.attempt,outcome:r.outcome,start:r.start,end:r.end,taps,unlocks:(r.unlocks||[]).map(([t,column,note])=>({t,column,note})),events:r.events,trayLeft};});
 const caps=Object.values(R.columns).flat().map(parse).filter(p=>!p.lock),totals={};for(const p of caps)totals[p.color]=(totals[p.color]||0)+p.count;
 const level={level:R.level??null,id,difficulty:R.difficulty,title:TITLES[R.level]||null,
  source:{video:BUCKET+R.video,site:'colony-flow.com walkthrough video (YouTube, 640x360)',given:'2026-10-25',cover:fs.existsSync(new URL(`../../../../product/audits/colony-flow-rounds/site/covers/${id}.jpg`,src))?`covers/${id}.jpg`:null,coverNote:COVER_MISMATCH[id]||null,start:`start/${id}.jpg`},
  board:B?{w:B.w,h:B.h,rows:B.rows,legend:B.legend,accounting:B.accounting,offBy:B.offBy,...(B.mysteryTiles?{mysteryTiles:B.mysteryTiles}:{}),...(cover?{fromCover:{field:cover.field,pitch:cover.pitch,watermarkCells:cover.band,clusters:B.clusters}}:{fromVideo:B.fromVideo})}:null,
  capacity:5,legend:R.legend,tray:R.columns,capsules:caps.length,padlocks:Object.values(R.columns).flat().filter(t=>t.startsWith('#')).length,totals,
  mechanics:R.mechanics,notes:R.notes||[],uncertain:R.uncertain||[],rounds,storyboard:{fps:ev.fps,trayMoves:ev.events}};
 levels.push(level);
 report.push({id,difficulty:R.difficulty,board:B?`${B.w}x${B.h}`:'-',columns:Object.keys(R.columns).length,capsules:caps.length,cells:caps.reduce((s,p)=>s+p.count,0),padlocks:level.padlocks,
  links:new Set(caps.filter(p=>p.link).map(p=>p.link)).size,hidden:caps.filter(p=>p.mystery).length,boardOff:B?B.offBy:null,rounds:rounds.map(r=>`${r.outcome} ${r.taps.length} taps ${Math.round(r.end-r.start)} s`).join('; ')});}
fs.mkdirSync(out,{recursive:true});
fs.writeFileSync(new URL('levels.json',out),JSON.stringify({format:'colony-flow-rounds-2',built:'tools/colony-deep-research/rounds/build-site.mjs',
 tokens:'Tray columns list the column top first: colour letter + count (cells it clears); "?" after the count = shown as "?" until it reaches the top; ":x" = linked group x, one tap takes every capsule of the group (each on top of its column, or right under another member in the same column); "#<colour>" = a padlock: never tapped, it blocks its column and leaves when its key on the board is reached (rounds[].unlocks). Board rows are top first, in the tray\'s colour letters; "." = no cube.',
 levels},null,1)+'\n');
const pad=(s,n)=>String(s).padStart(n);
for(const L of levels){const cols=Object.keys(L.tray),depth=Math.max(...cols.map(c=>L.tray[c].length)),w=Math.max(7,...Object.values(L.tray).flat().map(t=>t.length+2));
 const lines=[`# Colony Flow L${L.level}${L.title?' — '+L.title:''} (${L.difficulty})`,'',
  `Видео: [${L.source.video.split('/').pop()}](${L.source.video}) (видео с colony-flow.com, 640×360).`,'',
  ...(L.source.cover?[`Обложка уровня с сайта: [covers/${L.id}.jpg](covers/${L.id}.jpg).${L.source.coverNote?' **На обложке другая картинка** (тигр): поле по ней не восстановлено.':''}`]:[]),`Первый кадр: [start/${L.id}.jpg](start/${L.id}.jpg).`,''];
 if(L.board){lines.push(`## Поле ${L.board.w}×${L.board.h} (${L.board.fromVideo?'по кадру видео':'по обложке'})`,'','```',...L.board.rows,'```','',
  '| Буква | Цвет | Клеток на поле | В капсулах |','|---|---|---:|---:|',
  ...Object.entries(L.board.accounting).map(([k,a])=>`| \`${k}\` | ${L.board.legend[k].name}${L.board.legend[k].hex?' '+L.board.legend[k].hex:''} | ${a.board} | ${a.capsules} |`),'',
  L.board.fromVideo?`Поле прочитано с кадра видео (обложку сайта скачать не удалось). На поле ${L.board.mysteryTiles} клеток «?»: под ними то, чего видимому полю не хватает до сумм капсул. Расхождение по цветам — ${L.board.offBy} клеток, из них ${L.board.mysteryTiles} под «?».`
  :`Расхождение с капсулами: ${L.board.offBy} клеток из ${Object.values(L.totals).reduce((a,b)=>a+b,0)}. Поле распознано по обложке (кубики, водяной знак по середине восстановлен); коробки, шоколадки и ящики на поле прочитаны как клетки, а под ними лежит работа капсул — см. механики.`,'');}
 else lines.push('## Поле','',L.source.coverNote?'Обложка сайта не от этого уровня; поле видно только на первом кадре (мелко), не восстановлено.':'Обложки нет; поле видно только на первом кадре (мелко), не восстановлено.','');
 lines.push(`## Очередь: ${L.capsules} капсул${L.padlocks?`, ${L.padlocks} замков`:''}, ${cols.length} колонок, ${L.capacity} мест ожидания`,'',
  'Верх колонки — первая строка. `?` — капсула закрыта, пока не станет верхней; `:x` — связка x (один тап берёт всю связку); `#цвет` — замок (не тапается, уходит, когда собран ключ на поле).','',
  '| Буква | Цвет | Клеток в капсулах |','|---|---|---:|',...Object.entries(L.totals).map(([k,n])=>`| \`${k}\` | ${L.legend[k]} | ${n} |`),'',
  '```',`ряд ${cols.map(c=>pad(c,w)).join('')}`,...Array.from({length:depth},(_,i)=>`${pad(i+1,3)} ${cols.map(c=>pad(L.tray[c][i]||'',w)).join('')}`),'```','',
  '## Механики','',...L.mechanics.map(m=>`- ${m}`),...L.notes.map(m=>`- ${m}`),'');
 for(const r of L.rounds){lines.push(`## Попытка ${r.attempt}: ${r.outcome==='won'?'победа':r.outcome==='lost'?'проигрыш':r.outcome}, ${r.start}–${r.end} с, ${r.taps.length} тапов`,'',
  '| Время, с | Колонка | Берёт | Заметка |','|---:|---|---|---|',...r.taps.map(t=>`| ${t.t} | ${t.column} | ${t.take.join(' + ')} | ${t.note||''} |`),'');
  if(r.unlocks.length)lines.push('Замки:','',...r.unlocks.map(u=>`- ${u.t} с — ${u.column}: ${u.note}`),'');
  lines.push('События:','',...r.events.slice().sort((a,b)=>a[0]-b[0]).map(([t,e])=>`- ${t} с — ${e}`),'');}
 if(L.uncertain.length)lines.push('## Неуверенно','',...L.uncertain.map(u=>`- ${u}`),'');
 lines.push(`Раскадровка: лоток двигался ${L.storyboard.trayMoves.length} раз (кадры 4 в секунду, \`ytsheets.py\`); времена в \`levels.json\` → \`storyboard.trayMoves\`.`,'');
 fs.writeFileSync(new URL(`${L.id}.md`,out),lines.join('\n'));}
console.table(report);
fs.writeFileSync(new URL('checks.json',out),JSON.stringify(report,null,1)+'\n');

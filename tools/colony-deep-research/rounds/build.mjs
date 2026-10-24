// Builds product/audits/colony-flow-rounds/: every Colony Flow level reconstructed from the owner's recordings, as text
// another agent can read (levels.json + one storyboard per level), and checks it:
// - accounting: the capsules of each colour hold exactly the board's cells of that colour (board "?" tiles aside);
// - every recorded tap takes capsules that are on top of their columns (linked groups whole), and a won round empties
//   the tray.
// Sources: L1-L5, L10 (2026-10-20): owner-video/boards.json, sim/levels.json and the storyboards of
// colony-flow-levels-deep-dive-2026-10-20.md; L20 (2026-10-23): sim/l20.json with tap times from align20.mjs;
// L25, L26 (2026-10-24): data-l25-l26.mjs.
// node tools/colony-deep-research/rounds/build.mjs
import fs from 'node:fs';
import {L25,L26} from './data-l25-l26.mjs';
const root=new URL('../../../',import.meta.url),rd=p=>fs.readFileSync(new URL(p,root),'utf8'),out='product/audits/colony-flow-rounds/';
fs.mkdirSync(new URL(out,root),{recursive:true});
const boards=JSON.parse(rd('product/audits/colony-flow-2026-10-20/owner-video/boards.json'));
const trays=JSON.parse(rd('tools/colony-deep-research/sim/levels.json'));
const l20=JSON.parse(rd('tools/colony-deep-research/sim/l20.json')).L20,l20taps=JSON.parse(rd('tools/colony-deep-research/rounds/l20-taps.json'));
const dive=rd('product/audits/colony-flow-levels-deep-dive-2026-10-20.md');
const parse=t=>{const m=/^([A-Za-z#])(\d+)(\?)?(?::([a-z]))?$/.exec(t);if(!m)throw Error('token '+t);return {color:m[1],count:+m[2],mystery:!!m[3],link:m[4]||null};};
// The storyboard text of an earlier level from the deep dive: its "how the round went" part.
function diveStory(n){const head=dive.indexOf(`## ${n+3}. Уровень ${n} `)>=0?dive.indexOf(`## ${n+3}. Уровень ${n} `):dive.indexOf(`. Уровень ${n} `);
 const sec=dive.slice(head,dive.indexOf('\n## ',head+5));const a=Math.max(sec.indexOf('**Как шла партия**'),sec.indexOf('**Две партии рядом**'));
 if(a<0)return null;const b=sec.slice(a).search(/\n\*\*(Симулятор|Что делает уровень|Что взять|Наш L)/);return sec.slice(a,b<0?undefined:a+b).trim();}
const L10_HIDDEN={c1:[2,5,6,7],c2:[1,4,5],c3:[2,6,7]}; // deep dive §9: hidden capsules (0-based rows)
const levels=[];
const NAMES={1:'звезда',2:'сердце',3:'яблоко',4:'грибной домик',5:'кольца',10:'замок',20:'торт'};
for(const n of [1,2,3,4,5,10]){const b=boards['L'+n],t=trays['L'+n];
 const tray=Object.fromEntries(Object.entries(t.pods).map(([c,col])=>[c,col.map((tok,i)=>tok+(n===10&&L10_HIDDEN[c].includes(i)?'?':''))]));
 levels.push({level:n,difficulty:n===5?'Hard':n===10?'Super Hard':'Normal',title:NAMES[n],
  source:{video:n===10?'level-10-fail-part1/2.mp4, level-10-done-part1/2/3.mp4':n===5?'level-5.mp4':n===4?'level-1-4.mp4, level-4~2.mp4':'level-1-4.mp4',app:'Colony Flow v1.0.79',recorded:'2026-10-20',note:'transcribed in product/audits/colony-flow-levels-deep-dive-2026-10-20.md'},
  board:{w:b.w,h:b.h,rows:b.rows,legend:Object.fromEntries(Object.entries(b.legend).map(([k,v])=>[k,{name:v.name,hex:v.hex}]))},
  capacity:t.capacity,tray,rounds:null,storyboardText:diveStory(n)});}
levels.push({level:20,difficulty:'Super Hard',title:'торт с клубникой',source:{video:'XRecorder_20261023_02.mp4',app:'Colony Flow v1.0.79',recorded:'2026-10-23',note:'won in one attempt (about 390 s); tap times detected automatically (slottaps.py, traytaps.py, align20.mjs), ±0.5 s'},
 board:{w:l20.board.w,h:l20.board.h,rows:l20.board.rows,legend:Object.fromEntries(Object.entries(l20.legend).map(([k,v])=>[k,{name:v}]))},
 capacity:l20.capacity,tray:l20.pods,linkNote:l20.notes[0],
 rounds:[{attempt:1,outcome:'won',start:0,end:397.75,taps:l20taps.taps.map(t=>({t:t.t,column:'c'+t.column,take:t.pods.map(p=>p.replace(/^(c\d):/,'$1 ')),note:t.shared?'detected together with the next tap':null})),
  events:[[397.75,'Victory screen'],[0,'G8 (link a) waited about 250 s for green to open; D25+V10 (link c) about 180 s (deep dive of 2026-10-23)']]}]});
for(const L of [L25,L26]){const map=L.boardLetters,rows=L.boardRows.map(r=>[...r].map(c=>map[c]??(c==='.'?'.':'!')).join(''));
 levels.push({level:L.level,difficulty:L.difficulty,title:L.title,picture:L.picture,source:L.source,board:{w:rows[0].length,h:rows.length,rows,legend:L.legend,note:L.boardNote},capacity:L.capacity,tray:L.tray,linkNote:L.linkNote,rounds:L.rounds.map(r=>({...r,taps:r.taps.map(([t,column,note])=>({t,column,take:null,note:note||null})),events:r.events}))});}
// ---- checks ----
const report=[];
for(const L of levels){const cells={},pods={};for(const r of L.board.rows)for(const c of r)if(c!=='.')cells[c]=(cells[c]||0)+1;
 for(const col of Object.values(L.tray))for(const tok of col){const p=parse(tok);pods[p.color]=(pods[p.color]||0)+p.count;}
 const hidden=cells['?']||0;delete cells['?'];
 const colours=[...new Set([...Object.keys(cells),...Object.keys(pods)])].sort();
 L.accounting=Object.fromEntries(colours.map(c=>[c,{board:cells[c]||0,capsules:pods[c]||0}]));
 const off=colours.filter(c=>(cells[c]||0)!==(pods[c]||0)),extra=off.reduce((s,c)=>s+(pods[c]||0)-(cells[c]||0),0);
 L.board.cells=Object.values(cells).reduce((a,b)=>a+b,0)+hidden;if(hidden)L.board.mysteryTiles=hidden;
 // replay the taps
 for(const r of L.rounds||[]){const cols=Object.fromEntries(Object.entries(L.tray).map(([c,col])=>[c,col.map(parse)])),heads=Object.fromEntries(Object.keys(cols).map(c=>[c,0]));
  for(const tap of r.taps){const top=cols[tap.column][heads[tap.column]];if(!top)throw Error(`L${L.level} attempt ${r.attempt}: ${tap.column} empty at ${tap.t}`);
   const group=top.link?Object.entries(cols).flatMap(([c,col])=>col.map((p,i)=>({c,i,p}))).filter(x=>x.p.link===top.link):[{c:tap.column,i:heads[tap.column],p:top}];
   group.sort((x,y)=>x.i-y.i);for(const g of group){if(g.i!==heads[g.c])throw Error(`L${L.level} attempt ${r.attempt} at ${tap.t}: ${g.c} row ${g.i+1} is not on top`);heads[g.c]++;}
   const took=group.map(g=>`${g.c} ${g.p.color}${g.p.count}`);if(tap.take&&tap.take.join()!==group.map(g=>`${g.c} ${L.tray[g.c][g.i]}`).join())throw Error(`L${L.level}: tap ${tap.t} takes ${took} not ${tap.take}`);tap.take=took;}
  r.trayLeft=Object.entries(cols).reduce((s,[c,col])=>s+col.length-heads[c],0);if(r.outcome==='won'&&r.trayLeft)throw Error(`L${L.level}: won with ${r.trayLeft} capsules left`);}
 report.push({level:L.level,title:L.title,difficulty:L.difficulty,board:`${L.board.w}x${L.board.h}`,cells:L.board.cells,colours:colours.length,capsules:Object.values(L.tray).flat().length,columns:Object.keys(L.tray).length,accounting:off.length?off.map(c=>`${c} ${cells[c]||0}/${pods[c]||0}`).join(', ')+(hidden?` (board "?" ${hidden}, capsules +${extra})`:''):'exact',rounds:(L.rounds||[]).map(r=>`${r.outcome} ${r.taps.length} taps`).join('; ')||'storyboard text only'});}
// ---- write ----
fs.writeFileSync(new URL(out+'levels.json',root),JSON.stringify({format:'colony-flow-rounds-1',built:'tools/colony-deep-research/rounds/build.mjs',
 tokens:'Tray columns list capsules top first: colour letter + count; "?" after the count = shown as "?" until it reaches the top; ":x" = linked group x, one tap takes every capsule of the group. Board rows are top first; "." = no cube (open ground), "?" = a mystery tile on the board.',
 levels},null,1)+'\n');
const grid=rows=>'```\n'+rows.map(r=>[...r].join(' ')).join('\n')+'\n```';
for(const L of levels){const name=`L${String(L.level).padStart(2,'0')}.md`,cols=Object.keys(L.tray),depth=Math.max(...cols.map(c=>L.tray[c].length));
 const lines=[`# Colony Flow L${L.level} — ${L.title} (${L.difficulty})`,'',...(L.picture?[`Картинка: ${L.picture}.`,'']:[]),`Кадр начала: [frames/board-l${L.level}.jpg](frames/board-l${L.level}.jpg).`,'',
  `Источник: запись владельца \`${L.source.video}\`, ${L.source.app}, ${L.source.recorded}.${L.source.note?' '+L.source.note+'.':''}`,'',
  `## Поле ${L.board.w}×${L.board.h}, ${L.board.cells} клеток, ${Object.keys(L.accounting).length} цветов`,'',grid(L.board.rows),'',
  '| Буква | Цвет | Клеток на поле | В капсулах |','|---|---|---:|---:|',
  ...Object.entries(L.accounting).map(([c,a])=>`| \`${c}\` | ${L.board.legend[c]?.name||'?'}${L.board.legend[c]?.hex?' '+L.board.legend[c].hex:''} | ${a.board} | ${a.capsules} |`),'',
  L.board.note||'',L.board.mysteryTiles?`На поле ${L.board.mysteryTiles} клеток «?».`:'','',
  `## Очередь: ${Object.values(L.tray).flat().length} капсул, ${cols.length} колонки, ${L.capacity} мест ожидания`,'',
  'Верх колонки — первая строка. `?` — капсула закрыта, пока не станет верхней; `:x` — связка x (один тап берёт всю связку).','',
  '```',`ряд  ${cols.map(c=>c.padStart(7)).join('')}`,...Array.from({length:depth},(_,i)=>`${String(i+1).padStart(3)}  ${cols.map(c=>(L.tray[c][i]||'').padStart(7)).join('')}`),'```','',
  L.linkNote?`Связки: ${L.linkNote}`:'',''];
 if(L.rounds)for(const r of L.rounds){lines.push(`## Раунд ${r.attempt}: ${r.outcome==='won'?'победа':'проигрыш'}, ${r.start}–${r.end} с, ${r.taps.length} тапов`,'','| Время, с | Колонка | Берёт | Заметка |','|---:|---|---|---|',
  ...r.taps.map(t=>`| ${t.t} | ${t.column} | ${t.take.join(' + ')} | ${t.note||''} |`),'','События:','',...r.events.slice().sort((a,b)=>a[0]-b[0]).map(([t,e])=>`- ${t} с — ${e}`),'');}
 else lines.push('## Как шли партии','',L.storyboardText||'Нет.','');
 fs.writeFileSync(new URL(out+name,root),lines.filter((l,i,a)=>!(l===''&&a[i-1]==='')).join('\n'));}
console.table(report);
fs.writeFileSync(new URL(out+'checks.json',root),JSON.stringify(report,null,1)+'\n');

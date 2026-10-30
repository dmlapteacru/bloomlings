// A readable report (Russian, for the owner) of cf-pattern-tree.mjs results: per round the recorded win, the counts, the
// decision tree of the first taps, the ordering rules and where the choice matters.
// node tools/rounds/analysis/cf-pattern-report.mjs <dir> <CF-key>… > report.md
const fs=await import('node:fs'),path=await import('node:path');
const [dir,...keys]=process.argv.slice(2);const out=[];
const pct=x=>`${x} %`;
function tree(n,indent,lines){const opts=n.options.slice().sort((a,b)=>b.wins-a.wins);
 const head=n.prefix?`после «${n.prefix}»`:'первый тап';
 lines.push(`${indent}- **${head}** (побед в выборке: ${n.winsThrough}): `+opts.map(o=>o.wins?`**${o.tap}** ${pct(o.share)}`:`~~${o.tap}~~ ${o.check}`).join(' · '));
 for(const c of n.children||[])tree(c,indent+'  ',lines);}
for(const key of keys){const r=JSON.parse(fs.readFileSync(path.join(dir,key+'.json'),'utf8'));
 out.push(`## L${r.level} · ${r.difficulty}`,'',
  `${r.columns} колонки (высоты ${r.trayHeights.join('/')}), ${r.pods} подов, связок ${r.links}, скрытых ${r.hidden}. Случайные тапы выигрывают ${pct(r.randomWinShare)} (${r.wins} побед из ${r.games} партий).`,'',
  `**Твоя победа в записи:** \`${r.recorded?r.recorded.pattern:'—'}\`${r.recorded?` (модель: ${r.recorded.model==='Won'?'выигрывает':r.recorded.model})`:''}`,'',
  '### Дерево решений первых тапов','',
  'Для каждой позиции — какие тапы ведут к победе (доля выигрышных партий выборки через этот тап) и какие не выигрывают (зачёркнуты, с итогом проверки: случайные продолжения, затем точный поиск с ограничением). Ветки показаны, если через них проходит хотя бы 5 % побед.','');
 const lines=[];tree(r.tree,'',lines);out.push(...lines,'');
 out.push('### Правила порядка','');
 if(r.rules.length){out.push('Что соблюдается во всех (или почти всех) выигрышных партиях выборки и часто нарушается случайными тапами — это и есть «паттерн» раунда:','','| Правило | В победах | Нарушают случайные тапы |','|---|---:|---:|');
  for(const x of r.rules.slice(0,25))out.push(`| ${x.text} | ${x.inWins} | ${x.brokenInRandom} |`);if(r.rules.length>25)out.push('',`…и ещё ${r.rules.length-25} в JSON.`);}
 else out.push('Жёстких правил порядка нет: у победы нет обязательного «раньше/позже» между подами, которое случайные тапы часто нарушали бы.');
 out.push('','### Где выбор важен','','По номеру тапа на выигрышных путях выборки: в скольких позициях был выбор и в скольких часть вариантов проигрывает (по проверкам случайными продолжениями).','','| Тап | Позиций | С выбором | Выбор решает | Проигрышных вариантов |','|---:|---:|---:|---:|---:|');
 for(const p of r.profile)if(p)out.push(`| ${p.tap} | ${p.positions} | ${p.choice} | ${p.matters} | ${p.losing} |`);
 out.push('','### Примеры выигрышных паттернов','','```',...r.examples.slice(0,30),'```',`(${r.examples.length} разных в JSON.)`,'');}
console.log(out.join('\n'));

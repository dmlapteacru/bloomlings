// Builds the offline statistics report: one self-contained HTML page (inline CSS and JS, the data embedded as JSON,
// hand-drawn SVG charts; no network, no server) for Colony Flow (CF), our pilot v8E and our pilot v9.
// Reads tools/level-stats/out/data.json (assemble.mjs) and, if they exist, product/audits/level-stats/conclusions.md
// and unknowns.md (rendered by the small Markdown renderer below).
// node tools/level-stats/report.mjs → product/audits/level-stats/report.html
import fs from 'node:fs';import path from 'node:path';import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'../..');
const dataPath=path.join(root,'tools/level-stats/out/data.json'),outDir=path.join(root,'product/audits/level-stats'),outPath=path.join(outDir,'report.html');
const data=JSON.parse(fs.readFileSync(dataPath,'utf8'));

// ---- a small Markdown renderer: headings, lists, tables, bold, italics, links, code, paragraphs, rules ----
const esc=s=>String(s).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
function inline(s){const codes=[];s=s.replace(/`([^`]+)`/g,(m,c)=>{codes.push(c);return `\u0000${codes.length-1}\u0000`;});s=esc(s);
 s=s.replace(/\[([^\]]+)\]\(([^)\s]+)\)/g,(m,t,u)=>`<a href="${/^(javascript|data):/i.test(u)?'#':u}">${t}</a>`).replace(/\*\*([^*]+)\*\*/g,'<strong>$1</strong>').replace(/(^|[^*\w])\*([^*\s][^*]*)\*(?!\w)/g,'$1<em>$2</em>');
 return s.replace(/\u0000(\d+)\u0000/g,(m,i)=>`<code>${esc(codes[+i])}</code>`);}
export function markdown(src){const lines=src.replace(/\r/g,'').split('\n'),out=[];let i=0;
 const cells=l=>l.trim().replace(/^\||\|$/g,'').split('|').map(c=>c.trim());
 while(i<lines.length){const l=lines[i];
  if(/^```/.test(l)){const buf=[];i++;while(i<lines.length&&!/^```/.test(lines[i]))buf.push(lines[i++]);i++;out.push(`<pre><code>${esc(buf.join('\n'))}</code></pre>`);continue;}
  const h=/^(#{1,6})\s+(.*)$/.exec(l);if(h){const n=Math.min(6,h[1].length+1);out.push(`<h${n}>${inline(h[2])}</h${n}>`);i++;continue;}
  if(/^\s*([-*_])\s*\1\s*\1[\s\-*_]*$/.test(l)){out.push('<hr>');i++;continue;}
  if(/^\s*\|/.test(l)&&i+1<lines.length&&/^\s*\|?\s*:?-{2,}/.test(lines[i+1])){const head=cells(l);i+=2;const body=[];while(i<lines.length&&/^\s*\|/.test(lines[i]))body.push(cells(lines[i++]));
   out.push(`<div class="tablewrap"><table><thead><tr>${head.map(c=>`<th>${inline(c)}</th>`).join('')}</tr></thead><tbody>${body.map(r=>`<tr>${r.map(c=>`<td>${inline(c)}</td>`).join('')}</tr>`).join('')}</tbody></table></div>`);continue;}
  if(/^\s*([-*+]|\d+[.)])\s+/.test(l)){// a list, nested by indentation
   const items=[];while(i<lines.length&&(/^\s*([-*+]|\d+[.)])\s+/.test(lines[i])||(/^\s{2,}\S/.test(lines[i])&&items.length))){const m=/^(\s*)([-*+]|\d+[.)])\s+(.*)$/.exec(lines[i]);
    if(m)items.push({ind:m[1].length,ord:/\d/.test(m[2]),text:m[3]});else items[items.length-1].text+=' '+lines[i].trim();i++;}
   let html='';const stack=[];for(const it of items){while(stack.length&&it.ind<stack.at(-1).ind){html+=`</li></${stack.pop().tag}>`;}
    if(!stack.length||it.ind>stack.at(-1).ind){const tag=it.ord?'ol':'ul';stack.push({ind:it.ind,tag});html+=`<${tag}><li>`;}else html+='</li><li>';html+=inline(it.text);}
   while(stack.length)html+=`</li></${stack.pop().tag}>`;out.push(html);continue;}
  if(!l.trim()){i++;continue;}
  const buf=[];while(i<lines.length&&lines[i].trim()&&!/^(#{1,6}\s|```|\s*\||\s*([-*+]|\d+[.)])\s+)/.test(lines[i]))buf.push(lines[i++].trim());
  if(!buf.length){buf.push(lines[i++].trim());}out.push(`<p>${inline(buf.join(' '))}</p>`);}
 return out.join('\n');}
const md=name=>{const p=path.join(outDir,name);return fs.existsSync(p)?{name,html:markdown(fs.readFileSync(p,'utf8'))}:{name,html:null};};
const docs=[md('conclusions.md'),md('unknowns.md')];

// ---- the page ----
const CSS=`
:root{color-scheme:light;--bg:#fcfcfb;--panel:#f4f3f0;--panel2:#ebe9e4;--line:#dcdad4;--grid:#e9e7e2;--text:#1a1a19;--text2:#52514e;--muted:#7a7974;
 --accent:#2a78d6;--g-CF:#2a78d6;--g-v8E:#eb6834;--g-v9:#1baf7a;--s-a:#6250d6;--s-b:#e87ba4;--s-c:#eda100;
 --full:#d03b3b;--near:#fab219;--good:#0ca30c;--unk:#9a9890;--hatch:#c9c6bd;--wall:#5d5a55;--ground:#efece4;
 --q0:#cde2fb;--q1:#b7d3f6;--q2:#9ec5f4;--q3:#86b6ef;--q4:#6da7ec;--q5:#5598e7;--q6:#3987e5;--q7:#2a78d6;--q8:#256abf;--q9:#1c5cab;--q10:#104281;}
@media (prefers-color-scheme:dark){:root:not([data-theme="light"]){color-scheme:dark;--bg:#1a1a19;--panel:#232321;--panel2:#2d2d2a;--line:#3d3d39;--grid:#2e2e2b;--text:#f0efec;--text2:#c3c2b7;--muted:#9a998f;
 --accent:#3987e5;--g-CF:#3987e5;--g-v8E:#d95926;--g-v9:#199e70;--s-a:#9085e9;--s-b:#d55181;--s-c:#c98500;--unk:#77766e;--hatch:#4a4944;--wall:#8a867e;--ground:#2a2926;
 --q0:#0d366b;--q1:#104281;--q2:#184f95;--q3:#1c5cab;--q4:#256abf;--q5:#2a78d6;--q6:#3987e5;--q7:#5598e7;--q8:#6da7ec;--q9:#86b6ef;--q10:#b7d3f6;}}
:root[data-theme="dark"]{color-scheme:dark;--bg:#1a1a19;--panel:#232321;--panel2:#2d2d2a;--line:#3d3d39;--grid:#2e2e2b;--text:#f0efec;--text2:#c3c2b7;--muted:#9a998f;
 --accent:#3987e5;--g-CF:#3987e5;--g-v8E:#d95926;--g-v9:#199e70;--s-a:#9085e9;--s-b:#d55181;--s-c:#c98500;--unk:#77766e;--hatch:#4a4944;--wall:#8a867e;--ground:#2a2926;
 --q0:#0d366b;--q1:#104281;--q2:#184f95;--q3:#1c5cab;--q4:#256abf;--q5:#2a78d6;--q6:#3987e5;--q7:#5598e7;--q8:#6da7ec;--q9:#86b6ef;--q10:#b7d3f6;}
*{box-sizing:border-box}html{-webkit-text-size-adjust:100%}
body{margin:0;background:var(--bg);color:var(--text);font:15px/1.5 system-ui,-apple-system,"Segoe UI",Roboto,"Helvetica Neue",Arial,sans-serif;overflow-x:hidden}
header,main{max-width:1240px;margin:0 auto;padding:0 16px}
header{padding-top:18px}h1{font-size:22px;line-height:1.25;margin:0 0 4px}h2{font-size:19px;margin:28px 0 8px}h3{font-size:16px;margin:22px 0 6px}h4{font-size:15px;margin:16px 0 4px}
p{margin:6px 0 10px}.sub{color:var(--text2);margin:0 0 10px}small,.small{font-size:13px;color:var(--text2)}
a{color:var(--accent)}code{font:13px/1.4 ui-monospace,SFMono-Regular,Menlo,Consolas,monospace;background:var(--panel);padding:1px 4px;border-radius:4px}
pre{overflow:auto;background:var(--panel);padding:10px;border-radius:6px}
nav.tabs{position:sticky;top:0;z-index:5;background:var(--bg);border-bottom:1px solid var(--line);margin:10px -16px 0;padding:0 16px;overflow-x:auto;white-space:nowrap;scrollbar-width:thin}
nav.tabs button{font:inherit;font-size:15px;background:none;border:0;border-bottom:3px solid transparent;color:var(--text2);padding:10px 12px 8px;cursor:pointer}
nav.tabs button[aria-selected="true"]{color:var(--text);border-bottom-color:var(--accent);font-weight:600}
button,select,input{font:inherit;font-size:14px;color:var(--text)}
.btn{background:var(--panel);border:1px solid var(--line);border-radius:6px;padding:5px 10px;cursor:pointer}.btn:hover{background:var(--panel2)}
.btn.small{font-size:12px;padding:2px 8px}
select,input[type=number]{background:var(--bg);border:1px solid var(--line);border-radius:6px;padding:4px 6px}input[type=number]{width:5.2em}
details.filters{background:var(--panel);border:1px solid var(--line);border-radius:8px;margin:12px 0;padding:0 12px}
details.filters summary{cursor:pointer;padding:10px 0;font-weight:600}
.fgrid{display:grid;grid-template-columns:repeat(auto-fill,minmax(220px,1fr));gap:10px 18px;padding-bottom:12px}
.fgrid fieldset{border:0;margin:0;padding:0;min-width:0}.fgrid legend{font-size:13px;color:var(--text2);padding:0;margin-bottom:3px}
.fgrid label{margin-right:10px;white-space:nowrap;font-size:14px}.range{display:flex;gap:6px;align-items:center}
.count{font-weight:600}.banner{font-size:13px;color:var(--text2);margin:6px 0}
.tablewrap{overflow-x:auto;max-width:100%;border:1px solid var(--line);border-radius:8px;margin:8px 0 14px}
table{border-collapse:collapse;font-size:13px;font-variant-numeric:tabular-nums;width:100%}
th,td{padding:5px 8px;border-bottom:1px solid var(--grid);text-align:left;vertical-align:top}
th{background:var(--panel);font-weight:600;position:sticky;top:0;z-index:1}
td.n,th.n{text-align:right;white-space:nowrap}tr:last-child td{border-bottom:0}
table.main th{cursor:pointer;white-space:nowrap}table.main th[data-sort]::after{content:" ↕";color:var(--muted)}
table.main tbody tr{cursor:pointer}table.main tbody tr:hover{background:var(--panel)}
.unk{color:var(--unk);font-style:italic}.zero{color:var(--text)}
.tag{display:inline-block;font-size:11px;line-height:1.3;padding:0 5px;border-radius:9px;border:1px solid var(--line);color:var(--text2);white-space:nowrap;vertical-align:1px}
.gdot{display:inline-block;width:9px;height:9px;border-radius:50%;margin-right:5px;vertical-align:0}
.grid2{display:grid;grid-template-columns:repeat(auto-fit,minmax(min(100%,460px),1fr));gap:16px}
.grid3{display:grid;grid-template-columns:repeat(auto-fit,minmax(min(100%,330px),1fr));gap:14px}
figure.chart{margin:12px 0 18px;padding:12px;border:1px solid var(--line);border-radius:8px;background:var(--bg);min-width:0}
figure.chart figcaption{display:flex;flex-wrap:wrap;gap:6px 10px;align-items:baseline;justify-content:space-between;margin-bottom:6px}
figure.chart .t{font-weight:600;font-size:15px}figure.chart .meta{font-size:12px;color:var(--text2)}
figure.chart .acts{display:flex;gap:6px}figure.chart svg{display:block;width:100%;height:auto;overflow:visible}
.legend{display:flex;flex-wrap:wrap;gap:4px 14px;font-size:12px;color:var(--text2);margin:6px 0 0}
.legend i{display:inline-block;width:12px;height:12px;border-radius:3px;margin-right:5px;vertical-align:-2px}
.note{font-size:12px;color:var(--text2);margin:6px 0 0}
svg text{fill:var(--text2);font:11px system-ui,-apple-system,"Segoe UI",Roboto,sans-serif}
svg .ax{stroke:var(--line);stroke-width:1}svg .gl{stroke:var(--grid);stroke-width:1}
#tip{position:fixed;z-index:20;pointer-events:none;background:var(--text);color:var(--bg);font-size:12px;line-height:1.35;padding:6px 8px;border-radius:6px;max-width:300px;white-space:pre-line;display:none;box-shadow:0 2px 8px rgba(0,0,0,.2)}
.kv{display:grid;grid-template-columns:max-content 1fr;gap:2px 14px;font-size:14px;margin:6px 0}.kv dt{color:var(--text2)}.kv dd{margin:0}
.cards{display:grid;grid-template-columns:repeat(auto-fill,minmax(150px,1fr));gap:8px;margin:8px 0}
.card{background:var(--panel);border-radius:8px;padding:8px 10px}.card b{display:block;font-size:20px;font-variant-numeric:tabular-nums}.card span{font-size:12px;color:var(--text2)}
.levelbar{display:flex;flex-wrap:wrap;gap:8px;align-items:center;margin:12px 0}
.levelbar select{max-width:100%;min-width:0}
.tray{display:flex;gap:10px;overflow-x:auto;padding:4px 0 8px}.col{display:flex;flex-direction:column;gap:4px;min-width:62px}
.col h5{margin:0;font-size:12px;color:var(--text2);font-weight:600;text-align:center}
.chip{position:relative;display:flex;align-items:center;justify-content:center;gap:3px;height:30px;border-radius:6px;font-size:13px;font-weight:600;border:2px solid transparent;padding:0 6px}
.chip.hid{border-style:dashed;border-color:var(--text2)}.chip .lk{position:absolute;right:-5px;top:-7px;font-size:10px;background:var(--text);color:var(--bg);border-radius:8px;padding:0 4px;font-weight:600}
.chip.lock{background:var(--panel2);color:var(--text);border-color:var(--line)}
.steps{display:flex;flex-wrap:wrap;gap:2px;margin:6px 0}.steps span{width:14px;height:14px;border-radius:3px}
.callout{border-left:3px solid var(--accent);background:var(--panel);padding:8px 12px;border-radius:0 8px 8px 0;margin:10px 0;font-size:14px}
.warn{border-left-color:var(--near)}
.doc{max-width:900px}.doc table{font-size:14px}
.hide{display:none!important}
input[type=range]{width:min(100%,420px)}
@media (max-width:600px){body{font-size:14px}h1{font-size:19px}.kv{grid-template-columns:1fr}.kv dt{margin-top:4px}}
`;

function client(){
 const D=JSON.parse(document.getElementById('data').textContent),DOCS=JSON.parse(document.getElementById('docs').textContent);
 const $=s=>document.querySelector(s),GAMES=['CF','v8E','v9'],GAME_NAME={CF:'Colony Flow',v8E:'пилот v8E',v9:'пилот v9'},EXPECTED={CF:37,v8E:30,v9:30};
 const DIFFS=['Normal','Hard','Super Hard'],DIFF_RU={Normal:'обычный',Hard:'сложный','Super Hard':'очень сложный'};
 const STRATS=['left-to-right','right-to-left','top-down','bottom-up','diagonal','random','small-first','big-first','most-now','fast-release','planner','plannerPatient'];
 const STRAT_RU={'left-to-right':'слева направо','right-to-left':'справа налево','top-down':'сверху вниз','bottom-up':'снизу вверх','diagonal':'по диагонали','random':'случайно (100 запусков)','small-first':'маленькие сначала','big-first':'большие сначала','most-now':'больше очистит сейчас','fast-release':'быстрее освободит место','planner':'с планированием','plannerPatient':'планирование + ожидание'};
 const LV=Object.fromEntries(D.levels.map(l=>[l.key,l])),ROWS=D.rows,RK=Object.fromEntries(ROWS.map(r=>[r.key,r]));
 const esc=s=>String(s??'').replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
 const isNum=x=>typeof x==='number'&&Number.isFinite(x);
 const UNK='<span class="unk">неизвестно</span>';
 const nf=(x,d=2)=>isNum(x)?x.toLocaleString('ru-RU',{maximumFractionDigits:d}):null;
 const f=(x,d=2,u='')=>isNum(x)?nf(x,d)+(u?' '+u:''):UNK;
 const ft=(x,d=2,u='')=>isNum(x)?nf(x,d)+(u?' '+u:''):'неизвестно';// plain text
 const pct=(x,d=0)=>isNum(x)?nf(x*100,d)+' %':UNK;
 const pctT=(x,d=0)=>isNum(x)?nf(x*100,d)+' %':'неизвестно';
 const yn=x=>x===true?'да':x===false?'нет':UNK;
 const median=a=>{a=a.filter(isNum).sort((x,y)=>x-y);if(!a.length)return null;const m=a.length>>1;return a.length%2?a[m]:(a[m-1]+a[m])/2;};
 const mean=a=>{a=a.filter(isNum);return a.length?a.reduce((x,y)=>x+y,0)/a.length:null;};
 const quant=(a,q)=>{a=a.filter(isNum).sort((x,y)=>x-y);if(!a.length)return null;const p=(a.length-1)*q,i=Math.floor(p);return a[i]+(a[Math.min(i+1,a.length-1)]-a[i])*(p-i);};
 const mmss=s=>isNum(s)?`${Math.floor(s/60)}:${String(Math.floor(s%60)).padStart(2,'0')}${s%1?','+String(Math.round((s%1)*100)).padStart(2,'0'):''}`:'неизвестно';
 const OUT_RU={won:'победа',lost:'поражение',unfinished:'неполная запись',w:'победа',l:'поражение',u:'неполная запись'};
 const gdot=g=>`<span class="gdot" style="background:var(--g-${g})"></span>`;
 const lvName=k=>{const r=RK[k];return r?`${r.game==='CF'?'CF':r.game} L${r.level}`:k;};
 // sources: what a number rests on
 const SRC={video:['запись','увидено в записи (видео)'],replay:['повтор','точный повтор записанных нажатий на движке v8E'],approx:['модель≈','модель нашего движка по нажатиям записи; для Colony Flow приближённо'],route:['модель','модель: выигрышный маршрут, нажатия без пауз'],routeApprox:['модель≈','модель (приближённо для Colony Flow): выигрышный маршрут, нажатия без пауз'],strategy:['стратегия','прогон простой стратегии на модели'],static:['раскладка','поле и лоты по уровню или расшифровке кадра']};
 const srcTag=k=>k&&SRC[k]?`<span class="tag" title="${esc(SRC[k][1])}">${SRC[k][0]}</span>`:'';
 const refKind=s=>s==null?null:/exact/.test(s)?'replay':/^video/.test(s)?'video':/model replay/.test(s)?'approx':/route/.test(s)?'route':null;
 function reference(l){const won=l.attempts.find(a=>a.outcome==='won');
  if(l.game==='v8E'&&won)return {src:'replay',m:won.replay,att:won.attempt};
  if(l.game==='CF'&&won&&won.video)return {src:'video',m:won.video,att:won.attempt};
  if(l.game==='CF'&&won&&won.replay)return {src:'approx',m:won.replay,att:won.attempt};
  if(l.checks&&l.checks.routeLive)return {src:l.game==='CF'?'routeApprox':'route',m:l.checks.routeLive,att:null};
  return {src:null,m:null,att:null};}
 const rowRefSrc=r=>{const k=refKind(r.refSource);return k==='route'&&r.game==='CF'?'routeApprox':k;};
 function noteRu(s){if(!s)return '';const T=[[/^approximation: one hole under the middle of the board, ants walk round a ground border/,'приближение: один вход под серединой поля, муравьи обходят по краю из земли'],[/; (\d+) board "\?" tiles given the capsules' extra colours in reading order/g,'; $1 клеток «?» на поле получили недостающие цвета капсул по порядку чтения'],[/; (\d+) board cells recoloured to match the capsules/g,'; $1 клеток поля перекрашено под капсулы'],[/; (\d+) cells trimmed \(capsule counts or board cells\) to match/g,'; $1 клеток убрано (из чисел капсул или поля) для сходимости'],
  [/^no board \(the site cover shows another picture\)$/,'нет поля (обложка на сайте показывает другую картинку)'],[/^padlocks and keys are not modelled$/,'замки и ключи не моделируются'],[/^board objects are not modelled \((.*)\)$/,'объекты на поле не моделируются ($1)'],[/^board colour (.) has no capsules$/,'у цвета поля $1 нет капсул'],[/^more board "\?" tiles than missing capsule cells$/,'клеток «?» на поле больше, чем недостающих клеток капсул'],[/^board and capsules differ by (\d+) cells \(([\d.]+)%\)$/,'поле и капсулы расходятся на $1 клеток ($2 %)'],[/^more colours than the engine has$/,'цветов больше, чем в движке'],[/^exact: the engine the level was built for$/,'точно: движок, для которого сделан уровень']];
  let o=s;for(const [a,b] of T)o=o.replace(a,b);return o;}
 const modelRu=r=>r.model==='exact'?'точная':r.model==='approximate'?'приближённая':'нет';
 // board colours: ours from the pilot palette, Colony Flow by the transcribed colour name
 const OURS={G:['#6DBE45','Лист'],P:['#EB799C','Лепесток'],B:['#36B7DA','Вода'],A:['#A45F28','Янтарь'],C:['#F5E6BD','Жемчуг'],D:['#334B48','Земля'],V:['#7851A5','Слива'],Y:['#F6CB3E','Солнце'],O:['#EF8A32','Тыква'],L:['#CDEB5E','Лайм'],M:['#2F7D45','Мох'],R:['#D8394B','Ягода'],K:['#F9C6D6','Румянец'],N:['#2C5AA8','Глубина'],S:['#A8E2F5','Небо'],E:['#6A3A1E','Кора']};
 const NAMED=[[/dark green|тёмно-зел|grass shade/i,'#2e6a36'],[/navy|indigo|dark blue|mountains/i,'#2f3f8f'],[/purple-black|near-black purple|dark purple|navy\/purple/i,'#3b2547'],[/black|near.black|^dark|тёмн/i,'#2a2a2e'],[/crimson \(dark|crimson \(dark red\)/i,'#a5202f'],[/maroon|brown-red|red-brown|brick|rust|reddish brown/i,'#82352a'],[/brown-orange|orange-brown/i,'#b8642a'],[/chocolate|brown|коричн/i,'#7a4a26'],[/ice|pale light blue|light blue|light cyan|light aqua|pale cyan|голуб|небо|sky/i,'#9fd8f0'],[/cyan|aqua|turquoise|teal|бирюз/i,'#1fb2c6'],[/violet blue/i,'#5a55d0'],[/blue|син/i,'#2f6fd0'],[/lavender/i,'#b9a2e3'],[/fuchsia|magenta|hot pink|малинов|пурпур/i,'#d63a95'],[/violet|purple|фиолет/i,'#7a4fb0'],[/light pink|pale pink|светло-роз/i,'#f6bfd0'],[/salmon|coral|лосос/i,'#f08a7a'],[/peach|персик|skin/i,'#f5b48c'],[/pink|роз/i,'#ec7aa6'],[/crimson|red|красн/i,'#d63b3b'],[/light orange|orange-yellow|amber|golden orange|yellow-orange/i,'#f2a93b'],[/orange|оранж/i,'#ef7f22'],[/cream|light yellow|pale yellow|кремов/i,'#f3e3a6'],[/yellow|gold|жёлт|желт/i,'#f4cd2a'],[/lime|light green|лайм|салат/i,'#a8d64a'],[/green|зелён|зелен/i,'#3d9a40'],[/white|бел|light grey/i,'#f4f2ec'],[/grey|gray|сер/i,'#9a9a9a']];
 const HASH=['#8e6bbf','#d9822b','#3a8fb7','#c4515c','#6e9c3a','#b58b2a','#5b6bd1','#c06aa0'];
 function colourOf(l,ch){if(ch==='?')return {hex:'#c9c5b8',name:'клетка «?» (скрытый тип)'};
  if(l.game!=='CF'){const o=OURS[ch];return o?{hex:o[0],name:o[1]}:{hex:HASH[ch.charCodeAt(0)%8],name:ch};}
  const name=l.board&&l.board.legend&&l.board.legend[ch];if(name){for(const [re,hex] of NAMED)if(re.test(name))return {hex,name};}
  if(ch.length>1){for(const [re,hex] of NAMED)if(re.test(ch))return {hex,name:ch};}
  return {hex:HASH[ch.charCodeAt(0)%8],name:name||ch};}
 const ink=hex=>{const n=parseInt(hex.slice(1),16),r=n>>16,g=(n>>8)&255,b=n&255;return (r*299+g*587+b*114)/1000>150?'#1a1a19':'#ffffff';};

 // ---------- tooltip ----------
 const tip=$('#tip');let tipOn=null;
 const showTip=(el,x,y)=>{tip.textContent=el.getAttribute('data-tip');tip.style.display='block';const w=tip.offsetWidth,h=tip.offsetHeight;tip.style.left=Math.max(4,Math.min(innerWidth-w-4,x+12))+'px';tip.style.top=Math.max(4,y-h-10<0?y+16:y-h-10)+'px';};
 document.addEventListener('pointermove',e=>{const el=e.target.closest&&e.target.closest('[data-tip]');if(el){tipOn=el;showTip(el,e.clientX,e.clientY);}else if(tipOn){tipOn=null;tip.style.display='none';}});
 document.addEventListener('scroll',()=>{tip.style.display='none';},{passive:true});

 // ---------- SVG helpers ----------
 const scale=(d0,d1,r0,r1)=>{const k=d1===d0?0:(r1-r0)/(d1-d0);const s=v=>r0+(v-d0)*k;s.d=[d0,d1];return s;};
 function ticks(a,b,n=5){if(!isNum(a)||!isNum(b))return [];if(a===b){b=a+1;}const span=b-a,step0=span/n,p=Math.pow(10,Math.floor(Math.log10(step0))),e=step0/p,step=(e>=5?10:e>=2?5:e>=1?2:1)*p;const out=[];for(let v=Math.ceil(a/step)*step;v<=b+step*1e-9;v+=step)out.push(+v.toFixed(10));return out;}
 const niceMax=(m,n=5)=>{if(!isNum(m)||m<=0)return 1;const t=ticks(0,m,n);return t.at(-1)<m?t.at(-1)+(t[1]-t[0]):t.at(-1);};
 const T=(x,y,s,a='start',extra='')=>`<text x="${x.toFixed(1)}" y="${y.toFixed(1)}" text-anchor="${a}" ${extra}>${esc(s)}</text>`;
 function axes({x,y,W,H,m,xt,yt,xl,yl,xf=v=>nf(v,2),yf=v=>nf(v,2),grid=true}){let s='';
  for(const v of yt){const py=y(v);if(grid)s+=`<line class="gl" x1="${m.l}" x2="${W-m.r}" y1="${py}" y2="${py}"/>`;s+=T(m.l-5,py+4,yf(v),'end');}
  for(const v of xt){const px=x(v);s+=`<line class="ax" x1="${px}" x2="${px}" y1="${H-m.b}" y2="${H-m.b+4}"/>`+T(px,H-m.b+16,xf(v),'middle');}
  s+=`<line class="ax" x1="${m.l}" x2="${W-m.r}" y1="${H-m.b}" y2="${H-m.b}"/>`;
  if(xl)s+=T((m.l+W-m.r)/2,H-4,xl,'middle');if(yl)s+=T(m.l-m.l+2,m.t-8,yl,'start');return s;}
 const svg=(W,H,body,label)=>`<svg viewBox="0 0 ${W} ${H}" width="${W}" height="${H}" role="img" aria-label="${esc(label||'')}" xmlns="http://www.w3.org/2000/svg">${body}</svg>`;
 let figN=0;
 function fig({title,n,note,body,legend,wide}){const id='fig'+(++figN);
  return `<figure class="chart" id="${id}" data-title="${esc(title)}"><figcaption><span><span class="t">${esc(title)}</span> <span class="meta">${n||''}</span></span><span class="acts"><button class="btn small" data-save="svg" title="Скачать график в SVG">SVG</button><button class="btn small" data-save="png" title="Скачать график в PNG">PNG</button></span></figcaption>${body}${legend?`<div class="legend">${legend}</div>`:''}${note?`<p class="note">${note}</p>`:''}</figure>`;}
 const leg=(col,label)=>`<span><i style="background:${col}"></i>${esc(label)}</span>`;
 const gameLegend=games=>games.map(g=>leg(`var(--g-${g})`,GAME_NAME[g])).join('');
 const width=el=>Math.max(300,Math.min(1180,(el||$('main')).clientWidth-26));
 const stepPath=(pts,x,y)=>{if(!pts.length)return '';let d=`M${x(pts[0][0]).toFixed(1)},${y(pts[0][1]).toFixed(1)}`;for(let i=1;i<pts.length;i++){d+=`H${x(pts[i][0]).toFixed(1)}V${y(pts[i][1]).toFixed(1)}`;}return d;};
 const hatchDef=`<defs><pattern id="hatch" width="6" height="6" patternUnits="userSpaceOnUse" patternTransform="rotate(45)"><rect width="6" height="6" fill="var(--panel)"/><line x1="0" y1="0" x2="0" y2="6" stroke="var(--hatch)" stroke-width="2"/></pattern></defs>`;

 // saving charts: inline the CSS variables, add the title, offer SVG or PNG
 document.addEventListener('click',e=>{const b=e.target.closest('[data-save]');if(!b)return;const figEl=b.closest('figure');const s=figEl.querySelector('svg');if(!s)return;
  const cs=getComputedStyle(document.documentElement),vb=s.viewBox.baseVal,W=vb.width,H=vb.height,title=figEl.dataset.title+' '+(figEl.querySelector('.meta')?.textContent||'');
  const cl=s.cloneNode(true);cl.querySelectorAll('[data-tip]').forEach(n=>n.removeAttribute('data-tip'));const xs=new XMLSerializer();
  let inner=[...cl.childNodes].map(n=>xs.serializeToString(n)).join('').replace(/ xmlns="http:\/\/www.w3.org\/2000\/svg"/g,'').replace(/var\((--[\w-]+)\)/g,(m,v)=>cs.getPropertyValue(v).trim()||'#888');
  const font='font-family="system-ui,-apple-system,Segoe UI,Roboto,sans-serif"';
  const out=`<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${W} ${H+34}" width="${W}" height="${H+34}" ${font}><rect width="100%" height="100%" fill="${cs.getPropertyValue('--bg').trim()}"/><text x="4" y="20" font-size="14" font-weight="600" fill="${cs.getPropertyValue('--text').trim()}">${esc(title)}</text><g transform="translate(0,34)" font-size="11" fill="${cs.getPropertyValue('--text2').trim()}">${inner.replace(/<text /g,`<text fill="${cs.getPropertyValue('--text2').trim()}" `)}</g></svg>`;
  const TR={а:'a',б:'b',в:'v',г:'g',д:'d',е:'e',ё:'e',ж:'zh',з:'z',и:'i',й:'y',к:'k',л:'l',м:'m',н:'n',о:'o',п:'p',р:'r',с:'s',т:'t',у:'u',ф:'f',х:'h',ц:'ts',ч:'ch',ш:'sh',щ:'sch',ъ:'',ы:'y',ь:'',э:'e',ю:'yu',я:'ya'};
  const name='chart-'+[...(figEl.dataset.title||'chart').toLowerCase()].map(c=>TR[c]??c).join('').replace(/[^a-z0-9]+/g,'-').replace(/^-|-$/g,'').slice(0,60);
  if(b.dataset.save==='svg')return download(new Blob([out],{type:'image/svg+xml'}),name+'.svg');
  const img=new Image();img.onload=()=>{const c=document.createElement('canvas'),k=2;c.width=W*k;c.height=(H+34)*k;const g=c.getContext('2d');g.scale(k,k);g.drawImage(img,0,0);c.toBlob(bl=>download(bl,name+'.png'),'image/png');};
  img.onerror=()=>alert('PNG не получилось сохранить — сохраните SVG');img.src='data:image/svg+xml;charset=utf-8,'+encodeURIComponent(out);});
 function download(blob,name){const a=document.createElement('a');a.href=URL.createObjectURL(blob);a.download=name;document.body.appendChild(a);a.click();setTimeout(()=>{URL.revokeObjectURL(a.href);a.remove();},500);}

 // ---------- filters ----------
 const F={games:new Set(GAMES),lvMin:null,lvMax:null,cMin:null,cMax:null,tMin:null,tMax:null,pMin:null,pMax:null,links:'any',hidden:'any',diffs:new Set(DIFFS),outcome:'any',source:'any'};
 function pass(r){if(!F.games.has(r.game))return false;if(!F.diffs.has(r.difficulty))return false;
  const rng=(v,a,b)=>(a==null||(isNum(v)&&v>=a))&&(b==null||(isNum(v)&&v<=b));
  if(!rng(r.level,F.lvMin,F.lvMax)||!rng(r.cells,F.cMin,F.cMax)||!rng(r.types,F.tMin,F.tMax)||!rng(r.capsules,F.pMin,F.pMax))return false;
  if(F.links==='yes'&&!(r.linkSets>0)||F.links==='no'&&r.linkSets>0)return false;if(F.hidden==='yes'&&!(r.hidden>0)||F.hidden==='no'&&r.hidden>0)return false;
  const o=r.outcomes||'';if(F.outcome==='won'&&!o.includes('w')||F.outcome==='lost'&&!o.includes('l')||F.outcome==='unf'&&!o.includes('u')||F.outcome==='none'&&o.length||F.outcome==='firstwin'&&r.firstWinAttempt!==1||F.outcome==='retry'&&!(r.firstWinAttempt>1||o.length>1))return false;
  if(F.source==='observed'&&!r.attempts||F.source==='model'&&r.attempts||F.source==='hasmodel'&&r.model==='none'||F.source==='nomodel'&&r.model!=='none')return false;return true;}
 const rows=()=>ROWS.filter(pass);
 function filterPanel(){const chk=(name,vals,set,lab)=>vals.map(v=>`<label><input type="checkbox" data-f="${name}" value="${esc(v)}" ${set.has(v)?'checked':''}> ${lab(v)}</label>`).join('');
  const num=(k,ph)=>`<input type="number" data-f="${k}" placeholder="${ph}" value="${F[k]??''}" inputmode="numeric" aria-label="${ph}">`;
  const sel=(k,opts)=>`<select data-f="${k}">${opts.map(([v,t])=>`<option value="${v}" ${F[k]===v?'selected':''}>${t}</option>`).join('')}</select>`;
  return `<div class="fgrid">
  <fieldset><legend>Игра</legend>${chk('games',GAMES,F.games,g=>gdot(g)+GAME_NAME[g])}</fieldset>
  <fieldset><legend>Сложность (обозначение игры)</legend>${chk('diffs',DIFFS,F.diffs,d=>DIFF_RU[d])}</fieldset>
  <fieldset><legend>Номер уровня</legend><div class="range">${num('lvMin','от')}–${num('lvMax','до')}</div></fieldset>
  <fieldset><legend>Клеток на поле</legend><div class="range">${num('cMin','от')}–${num('cMax','до')}</div></fieldset>
  <fieldset><legend>Типов клеток</legend><div class="range">${num('tMin','от')}–${num('tMax','до')}</div></fieldset>
  <fieldset><legend>Капсул (лотов)</legend><div class="range">${num('pMin','от')}–${num('pMax','до')}</div></fieldset>
  <fieldset><legend>Связанные капсулы</legend>${sel('links',[['any','любые'],['yes','есть'],['no','нет']])}</fieldset>
  <fieldset><legend>Скрытые капсулы «?»</legend>${sel('hidden',[['any','любые'],['yes','есть'],['no','нет']])}</fieldset>
  <fieldset><legend>Исход и попытки</legend>${sel('outcome',[['any','любые'],['won','есть победа'],['lost','есть поражение'],['unf','есть неполная запись'],['firstwin','победа в 1-й записанной попытке'],['retry','больше одной попытки'],['none','нет записанных попыток']])}</fieldset>
  <fieldset><legend>Источник</legend>${sel('source',[['any','любой'],['observed','есть прохождение человека'],['model','только модель (нет попыток)'],['hasmodel','есть модель'],['nomodel','нет модели']])}</fieldset>
  <fieldset><legend>&nbsp;</legend><button class="btn" id="freset">Сбросить фильтры</button></fieldset></div>`;}
 function readFilters(e){const el=e.target,k=el.dataset.f;if(!k)return;
  if(el.type==='checkbox'){F[k][el.checked?'add':'delete'](el.value);}else if(el.type==='number'){F[k]=el.value===''?null:+el.value;}else F[k]=el.value;
  updateCount();render();}
 function updateCount(){const n=rows().length;$('#fcount').textContent=`${n} из ${ROWS.length}`;}

 // ---------- tabs ----------
 const TABS=[['overview','Обзор'],['level','Уровень'],['strategies','Стратегии'],['compare','Сравнение'],['similar','Похожесть'],['time','Время'],['complete','Полнота'],['findings','Выводы']];
 const S={tab:'overview',key:null,meas:0,heat:'meanFree',basis:'segmentsByCleared',sort:null,dir:1,cmpMetric:'cells',simA:null,simB:null,clearK:10,cmpBy:'game'};
 function readHash(){const p=new URLSearchParams(location.hash.slice(1));if(p.get('tab')&&TABS.some(t=>t[0]===p.get('tab')))S.tab=p.get('tab');if(p.get('key')&&LV[p.get('key')])S.key=p.get('key');}
 function writeHash(){const p=new URLSearchParams();p.set('tab',S.tab);if(S.tab==='level'&&S.key)p.set('key',S.key);history.replaceState(null,'','#'+p);}
 function render(){figN=0;document.querySelectorAll('nav.tabs button').forEach(b=>b.setAttribute('aria-selected',b.dataset.tab===S.tab));
  $('#filters').classList.toggle('hide',S.tab==='findings');
  const el=$('#view');try{el.innerHTML=VIEWS[S.tab]();}catch(err){el.innerHTML=`<p class="callout warn">Ошибка отрисовки: ${esc(err.message)}</p>`;console.error(err);}writeHash();}
 const banner=(n,what='уровней')=>`<p class="banner">С учётом фильтров: <b>${n}</b> ${what} из ${ROWS.length}.</p>`;

 // ---------- overview ----------
 const COLS=[['key','Уровень'],['difficulty','Сложн.'],['title','Название'],['cells','Клетки','n'],['types','Типы','n'],['capsules','Капсулы','n'],['columns','Колонки','n'],['countMin','Число мин','n'],['countMedian','медиана','n'],['countMax','макс','n'],['linkSets','Наборы связей','n'],['linkCapsules','капсул в них','n'],['hidden','Скрытые «?»','n'],['padlocks','Замки','n'],['model','Модель'],['winnable','Проходим (модель)'],['minCapacity','Мин. буфер','n'],['risky','Рискованные / единств. решения','n'],['s_random','Случайно, побед','n'],['s_planner','Планир.','n'],['outcomes','Попытки'],['wonSeconds','Победа, с','n'],['meanFree','Свободно в ср.','n'],['zeroFreeShare','Полный буфер, доля врем.','n']];
 function cell(r,k){const v=r[k];switch(k){
  case 'key':return `${gdot(r.game)}<a href="#tab=level&key=${esc(r.key)}" data-go="${esc(r.key)}">${esc(lvName(r.key))}</a>`;
  case 'difficulty':return esc(DIFF_RU[v]||v);case 'title':return esc(v);
  case 'model':return modelRu(r);case 'winnable':return r.model==='none'?'<span class="unk" title="нет модели">нет модели</span>':yn(v);
  case 'minCapacity':return r.model==='none'?'<span class="unk">—</span>':isNum(v)?nf(v)+(r.minCapacityProven?' <span class="tag" title="меньшая вместимость проверена и не проходит">доказано</span>':' <span class="tag" title="меньшая вместимость не проверена до конца">не доказано</span>'):UNK;
  case 'risky':return r.model==='none'?'<span class="unk">—</span>':`${f(r.risky,0)} / ${f(r.tight,0)} <span class="small">из ${f(r.decisions,0)}</span>`;
  case 's_random':case 's_planner':return r.model==='none'?'<span class="unk">—</span>':pct(v);
  case 'outcomes':return r.attempts?[...v].map(c=>`<span title="${OUT_RU[c]}">${c==='w'?'П':c==='l'?'Х':'Н'}</span>`).join(' '):'<span class="unk" title="нет записанных попыток людей">нет попыток</span>';
  case 'wonSeconds':return isNum(v)?nf(v,1):r.attempts?UNK:'<span class="unk">—</span>';
  case 'meanFree':return isNum(v)?nf(v)+' '+srcTag(rowRefSrc(r)):UNK;case 'zeroFreeShare':return isNum(v)?pct(v)+' '+srcTag(rowRefSrc(r)):UNK;
  default:return isNum(v)?nf(v):v==null?UNK:esc(v);}}
 function mainTable(rs){const sorted=rs.slice();if(S.sort){const k=S.sort;sorted.sort((a,b)=>{const x=a[k],y=b[k];if(x==null&&y==null)return 0;if(x==null)return 1;if(y==null)return -1;return (x<y?-1:x>y?1:0)*S.dir;});}
  return `<div class="tablewrap" style="max-height:70vh"><table class="main"><thead><tr>${COLS.map(([k,t,c])=>`<th data-sort="${k}" class="${c||''}">${t}${S.sort===k?(S.dir>0?' ▲':' ▼'):''}</th>`).join('')}</tr></thead><tbody>${sorted.map(r=>`<tr data-go="${esc(r.key)}">${COLS.map(([k,,c])=>`<td class="${c||''}">${cell(r,k)}</td>`).join('')}</tr>`).join('')}</tbody></table></div>`;}
 function cellsByLevel(rs){const W=width(),gs=GAMES.filter(g=>rs.some(r=>r.game===g));if(!gs.length)return '';
  const cols=W>760?gs.length:1,pw=(W-(cols-1)*14)/cols,ph=210,m={l:44,r:10,t:22,b:34},ymax=niceMax(Math.max(...rs.map(r=>r.cells).filter(isNum),10));let body='',parts=[];
  gs.forEach((g,i)=>{const pr=rs.filter(r=>r.game===g).sort((a,b)=>a.level-b.level),ok=pr.filter(r=>isNum(r.cells)),lv=pr.map(r=>r.level);const x0=Math.min(...lv),x1=Math.max(...lv);
   const x=scale(x0===x1?x0-1:x0,x0===x1?x1+1:x1,m.l,pw-m.r),y=scale(0,ymax,ph-m.b,m.t);let s=axes({x,y,W:pw,H:ph,m,xt:ticks(x.d[0],x.d[1],cols>1?4:6).filter(v=>v>=x.d[0]&&Number.isInteger(v)),yt:ticks(0,ymax,4),xl:'номер уровня',yl:'клеток',xf:v=>nf(v,0),yf:v=>nf(v,0)});
   s+=T(pw-m.r,m.t-8,`${GAME_NAME[g]}: n = ${ok.length}${pr.length>ok.length?`, без поля ${pr.length-ok.length}`:''}`,'end','font-weight="600"');
   if(ok.length>1)s+=`<path d="${ok.map((r,j)=>(j?'L':'M')+x(r.level).toFixed(1)+','+y(r.cells).toFixed(1)).join('')}" fill="none" stroke="var(--g-${g})" stroke-width="1.5" opacity=".5"/>`;
   for(const r of ok)s+=`<circle cx="${x(r.level)}" cy="${y(r.cells)}" r="4.5" fill="var(--g-${g})" stroke="var(--bg)" stroke-width="2" data-tip="${esc(`${lvName(r.key)} «${r.title}»\n${r.cells} клеток, ${r.types} типов\nисточник: раскладка`)}"/>`;
   const ox=cols>1?i*(pw+14):0,oy=cols>1?0:i*(ph+8);parts.push(`<g transform="translate(${ox},${oy})">${s}</g>`);});
  const H=cols>1?ph:gs.length*(ph+8);
  return fig({title:'Сколько клеток на поле и как это меняется с номером уровня?',n:`n = ${rs.filter(r=>isNum(r.cells)).length} уровней`,body:svg(W,H,parts.join(''),'клетки по номеру уровня'),note:'Фактическое число занятых клеток (не ширина × высота). Каждая игра — своя ось номеров. Источник: раскладка поля (кадр или файл уровня).'});}
 function podCounts(l){const out=[];for(const col of Object.values(l.tray||{}))for(const t of col){const m=/^([A-Za-z#])(\d+)/.exec(t);if(m&&!/^#[a-z]+$/.test(t))out.push(+m[2]);}return out;}
 function capsuleRanges(rs){const W=width(),gs=GAMES.filter(g=>rs.some(r=>r.game===g));if(!gs.length)return '';const ph=200,m={l:40,r:8,t:22,b:30},ymax=niceMax(Math.max(...rs.map(r=>r.countMax).filter(isNum),10));const parts=[];let oy=0;
  for(const g of gs){const pr=rs.filter(r=>r.game===g).sort((a,b)=>a.level-b.level),n=pr.length,bw=(W-m.l-m.r)/Math.max(n,1);const y=scale(0,ymax,ph-m.b,m.t);
   let s=axes({x:v=>v,y,W,H:ph,m,xt:[],yt:ticks(0,ymax,4),yl:'число на капсуле',yf:v=>nf(v,0)});s+=T(W-m.r,m.t-8,`${GAME_NAME[g]}: n = ${n} уровней, ${pr.reduce((a,r)=>a+(r.capsules||0),0)} капсул`,'end','font-weight="600"');
   const every=Math.ceil(n/(W/34));
   pr.forEach((r,i)=>{const cx=m.l+bw*(i+.5);if(i%every===0)s+=T(cx,ph-m.b+14,'L'+r.level,'middle');
    s+=`<g data-tip="${esc(`${lvName(r.key)}: ${r.capsules} капсул\nмин ${r.countMin}, медиана ${r.countMedian}, среднее ${nf(r.countMean)}, макс ${r.countMax}\nразных чисел ${r.countValues}, шаг ${r.countStep}`)}"><rect x="${cx-bw/2}" y="${m.t}" width="${bw}" height="${ph-m.b-m.t}" fill="transparent"/>`+
     `<line x1="${cx}" x2="${cx}" y1="${y(r.countMin)}" y2="${y(r.countMax)}" stroke="var(--g-${g})" stroke-width="2" stroke-linecap="round"/>`+
     `<line x1="${cx-Math.min(7,bw/2.5)}" x2="${cx+Math.min(7,bw/2.5)}" y1="${y(r.countMean)}" y2="${y(r.countMean)}" stroke="var(--text)" stroke-width="1.5"/>`+
     `<circle cx="${cx}" cy="${y(r.countMedian)}" r="3.5" fill="var(--bg)" stroke="var(--g-${g})" stroke-width="2"/></g>`;});
   parts.push(`<g transform="translate(0,${oy})">${s}</g>`);oy+=ph+6;}
  return fig({title:'Какие числа стоят на капсулах уровня: от минимума до максимума?',n:`n = ${rs.length} уровней`,body:svg(W,oy,parts.join(''),'числа на капсулах'),legend:`<span>линия: минимум–максимум</span><span>○ медиана</span><span>— среднее</span>`,note:'Источник: раскладка лотов (расшифровка видео для Colony Flow, файл уровня для пилотов). Максимум — максимум изученной выборки, не предел игры. Замки (#цвет) не считаются капсулами.'});}
 function capsuleHist(rs){const W=width(),gs=GAMES.filter(g=>rs.some(r=>r.game===g));if(!gs.length)return '';const vals={};let mx=0;for(const g of gs){vals[g]=rs.filter(r=>r.game===g).flatMap(r=>podCounts(LV[r.key]));mx=Math.max(mx,...vals[g]);}
  const bin=mx>60?10:mx>30?5:2,nb=Math.ceil((mx+1)/bin),cols=W>760?gs.length:1,pw=(W-(cols-1)*14)/cols,ph=170,m={l:40,r:8,t:22,b:34};const parts=[];
  gs.forEach((g,i)=>{const h=Array(nb).fill(0);for(const v of vals[g])h[Math.floor(v/bin)]++;const tot=vals[g].length,ymax=niceMax(Math.max(...h,1),3);
   const x=scale(0,nb*bin,m.l,pw-m.r),y=scale(0,ymax,ph-m.b,m.t);let s=axes({x,y,W:pw,H:ph,m,xt:ticks(0,nb*bin,cols>1?4:8),yt:ticks(0,ymax,3),xl:'число на капсуле',yl:'капсул',xf:v=>nf(v,0),yf:v=>nf(v,0)});
   s+=T(pw-m.r,m.t-8,`${GAME_NAME[g]}: ${tot} капсул`,'end','font-weight="600"');
   h.forEach((c,j)=>{if(!c)return;const x0=x(j*bin)+1,x1=x((j+1)*bin)-1;s+=`<rect x="${x0}" y="${y(c)}" width="${Math.max(1,x1-x0)}" height="${y(0)-y(c)}" rx="2" fill="var(--g-${g})" data-tip="${esc(`${j*bin}–${(j+1)*bin-1}: ${c} капсул (${nf(100*c/tot,1)} %)`)}"/>`;});
   const ox=cols>1?i*(pw+14):0,oy=cols>1?0:i*(ph+8);parts.push(`<g transform="translate(${ox},${oy})">${s}</g>`);});
  return fig({title:'Как часто встречается каждое число на капсуле?',n:`n = ${gs.map(g=>`${GAME_NAME[g]} ${vals[g].length}`).join(', ')} капсул`,body:svg(W,cols>1?ph:gs.length*(ph+8),parts.join(''),'распределение чисел'),note:`Столбец = диапазон чисел шириной ${bin}. Каждая капсула уровня считается один раз (повторные попытки не дублируют раскладку). Источник: раскладка.`});}
 function scatterCells(rs){const W=width(),H=300,m={l:48,r:14,t:20,b:40},ok=rs.filter(r=>isNum(r.cells)&&isNum(r.capsules));if(!ok.length)return '';
  const x=scale(0,niceMax(Math.max(...ok.map(r=>r.cells))),m.l,W-m.r),y=scale(0,niceMax(Math.max(...ok.map(r=>r.capsules))),H-m.b,m.t);
  let s=axes({x,y,W,H,m,xt:ticks(...x.d,6),yt:ticks(...y.d,5),xl:'клеток на поле',yl:'капсул',xf:v=>nf(v,0),yf:v=>nf(v,0)});
  for(const r of ok)s+=`<circle cx="${x(r.cells)}" cy="${y(r.capsules)}" r="5" fill="var(--g-${r.game})" fill-opacity=".85" stroke="var(--bg)" stroke-width="2" data-tip="${esc(`${lvName(r.key)} «${r.title}»\n${r.cells} клеток, ${r.capsules} капсул в ${r.columns} колонках\nсреднее число ${nf(r.countMean)}`)}"/>`;
  const gs=GAMES.filter(g=>ok.some(r=>r.game===g));
  return fig({title:'Сколько капсул приходится на поле такого размера?',n:`n = ${ok.length} уровней`,body:svg(W,H,s,'капсулы и клетки'),legend:gameLegend(gs),note:`Источник: раскладка. ${rs.length-ok.length?`Без поля или капсул: ${rs.length-ok.length} ур. (не показаны).`:''}`});}
 function linksHidden(rs){const W=width(),gs=GAMES.filter(g=>rs.some(r=>r.game===g));if(!gs.length)return '';const ph=170,m={l:34,r:8,t:22,b:30},ymax=niceMax(Math.max(...rs.flatMap(r=>[r.linkCapsules,r.hidden,r.padlocks]).filter(isNum),3),3);const parts=[];let oy=0;
  const ser=[['linkSets','наборов связей','var(--s-a)'],['linkCapsules','капсул в связях','var(--s-b)'],['hidden','скрытых «?»','var(--s-c)']];
  for(const g of gs){const pr=rs.filter(r=>r.game===g).sort((a,b)=>a.level-b.level),n=pr.length,bw=(W-m.l-m.r)/Math.max(n,1),y=scale(0,ymax,ph-m.b,m.t);
   let s=axes({x:v=>v,y,W,H:ph,m,xt:[],yt:ticks(0,ymax,3),yl:'штук',yf:v=>nf(v,0)});s+=T(W-m.r,m.t-8,`${GAME_NAME[g]}: n = ${n}`,'end','font-weight="600"');const every=Math.ceil(n/(W/34));
   pr.forEach((r,i)=>{const x0=m.l+bw*i,inner=Math.max(1.5,(bw-4)/3);if(i%every===0)s+=T(x0+bw/2,ph-m.b+14,'L'+r.level,'middle');
    const tipTxt=`${lvName(r.key)}\nнаборов связей: ${r.linkSets}, капсул в них: ${r.linkCapsules}, самый большой набор: ${r.linkMaxSize}\nскрытых капсул: ${r.hidden}\nзамков: ${r.padlocks}`;
    s+=`<g data-tip="${esc(tipTxt)}"><rect x="${x0}" y="${m.t}" width="${bw}" height="${ph-m.b-m.t}" fill="transparent"/>`;
    ser.forEach(([k,,c],j)=>{const v=r[k]||0;if(v>0)s+=`<rect x="${x0+2+j*inner}" y="${y(v)}" width="${Math.max(1,inner-1)}" height="${y(0)-y(v)}" rx="1.5" fill="${c}"/>`;});s+='</g>';});
   parts.push(`<g transform="translate(0,${oy})">${s}</g>`);oy+=ph+6;}
  return fig({title:'Где появляются связи и скрытые капсулы, и сколько их?',n:`n = ${rs.length} уровней`,body:svg(W,oy,parts.join(''),'связи и скрытые'),legend:ser.map(([,t,c])=>leg(c,t)).join(''),note:'Наборы и капсулы в наборах показаны раздельно: три пары и одна связь из шести капсул — разные ограничения. Пусто = 0 (не «неизвестно»). Источник: раскладка.'});}
 const HEAT={meanFree:['среднее число свободных мест','мест',false],minFree:['минимум свободных мест','мест',false],zeroFreeShare:['доля времени с 0 свободных (полный)','%',true],oneFreeShare:['доля времени ровно с 1 свободным','%',true]};
 function heatmap(rs){const W=width(),list=rs.map(r=>{const l=LV[r.key],ref=reference(l);return {r,l,ref,seg:ref.m?ref.m[S.basis]:null};});
  const lab=110,cw=Math.max(40,Math.min(110,(W-lab-10)/5)),rh=20,H=30+list.length*rh+4,[name,unit,share]=HEAT[S.heat];
  const vals=list.flatMap(x=>(x.seg||[]).filter(s=>s&&s.reached).map(s=>s[S.heat])).filter(isNum),vmax=share?1:Math.max(5,...vals);
  const colour=v=>{const k=Math.round(10*Math.min(1,v/vmax));return share?`var(--q${k})`:`var(--q${10-k})`;};
  let s=hatchDef;['0–20 %','20–40 %','40–60 %','60–80 %','80–100 %'].forEach((t,i)=>s+=T(lab+cw*(i+.5),18,t,'middle'));
  list.forEach((x,j)=>{const y0=26+j*rh;s+=`<text x="4" y="${y0+14}" data-tip="${esc(`${lvName(x.r.key)} «${x.r.title}»\nисточник: ${x.ref.src?SRC[x.ref.src][1]:'нет данных'}`)}"><tspan fill="var(--g-${x.r.game})">●</tspan> ${esc(lvName(x.r.key))} <tspan font-size="9">${x.ref.src?SRC[x.ref.src][0]:''}</tspan></text>`;
   for(let i=0;i<5;i++){const sg=x.seg&&x.seg[i],xx=lab+cw*i;
    if(!x.seg){s+=`<rect x="${xx+1}" y="${y0+1}" width="${cw-2}" height="${rh-2}" fill="url(#hatch)" data-tip="${esc(`${lvName(x.r.key)}: нет данных по отрезкам\n${x.r.model==='none'?'нет модели: '+noteRu(x.r.modelNote):x.seg===null&&x.ref.m?'видео не даёт остатка поля — разбивка по очистке неизвестна':''}`)}"/>`;continue;}
    if(!sg||!sg.reached){s+=`<rect x="${xx+1}" y="${y0+1}" width="${cw-2}" height="${rh-2}" fill="var(--panel)" data-tip="${esc(`${lvName(x.r.key)}, отрезок ${i+1}: не достигнут`)}"/>`+T(xx+cw/2,y0+14,'не дост.','middle','font-size="9"');continue;}
    const v=sg[S.heat];if(!isNum(v)){s+=`<rect x="${xx+1}" y="${y0+1}" width="${cw-2}" height="${rh-2}" fill="url(#hatch)"/>`;continue;}
    const c=colour(v),txt=share?nf(v*100,0):nf(v,1),k=Math.round(10*Math.min(1,v/vmax)),dark=share?k>=6:10-k>=6;
    s+=`<rect x="${xx+1}" y="${y0+1}" width="${cw-2}" height="${rh-2}" rx="2" fill="${c}" data-tip="${esc(`${lvName(x.r.key)}, отрезок ${sg.label} (${S.basis==='segmentsByCleared'?'по доле очищенных клеток':'по доле времени'})\nдлительность ${ft(sg.duration,1,'с')}\nсвободно в среднем ${ft(sg.meanFree)}, минимум ${ft(sg.minFree,0)}\n0 свободных: ${ft(sg.zeroFree,1,'с')} (${pctT(sg.zeroFreeShare)})\n1 свободное: ${ft(sg.oneFree,1,'с')} (${pctT(sg.oneFreeShare)})\nнажатий ${ft(sg.taps,0)}\nисточник: ${SRC[x.ref.src][1]}`)}"/>`+`<text x="${xx+cw/2}" y="${y0+14}" text-anchor="middle" style="fill:${dark?'#fff':'#1a1a19'}" font-size="10">${txt}</text>`;}});
  const btn=(k,v,t)=>`<label style="margin-right:12px"><input type="radio" name="${k}" value="${v}" ${S[k]===v?'checked':''} data-s="${k}"> ${t}</label>`;
  const controls=`<div class="small" style="margin:4px 0 8px">Показатель: ${Object.entries(HEAT).map(([k,[t]])=>btn('heat',k,t)).join('')}<br>Отрезки: ${btn('basis','segmentsByCleared','по доле очищенных клеток')}${btn('basis','segmentsByTime','по доле времени попытки')}</div>`;
  const nData=list.filter(x=>x.seg).length;
  return fig({title:'Насколько тесно в буфере на каждом из пяти отрезков уровня?',n:`n = ${nData} уровней с данными из ${list.length}`,body:controls+svg(W,H,s,'тепловая карта буфера'),legend:`<span>${share?'темнее = больше времени без мест':'темнее = меньше свободных мест'}</span>${leg('var(--panel)','не достигнут (проигрыш или неполная запись)')}${leg('url(#hatch)','нет данных')}`,
   note:`Для каждого уровня одна опорная попытка с меткой источника: «запись» — увидено в видео; «повтор» — точный повтор нажатий v8E; «модель≈» — модель по нажатиям записи (для Colony Flow приближённо) или выигрышный маршрут модели. Среднее учитывает длительность состояний. Время модели — секунды при скорости ×1; время видео — секунды записи. Полный (0) и почти полный (1) не складываются.`});}
 VIEWS={};
 VIEWS.overview=()=>{const rs=rows();const byG=GAMES.map(g=>[g,rs.filter(r=>r.game===g).length]);
  return `<h2>Обзор</h2>${banner(rs.length)}
  <div class="cards">${byG.map(([g,n])=>`<div class="card">${gdot(g)}<span>${GAME_NAME[g]}</span><b>${n}</b><span>измерено ${ROWS.filter(r=>r.game===g).length} из ${EXPECTED[g]}</span></div>`).join('')}
  <div class="card"><span>с попытками людей</span><b>${rs.filter(r=>r.attempts).length}</b><span>v9 — только модель</span></div><div class="card"><span>с моделью</span><b>${rs.filter(r=>r.model!=='none').length}</b><span>без модели: ${rs.filter(r=>r.model==='none').length}</span></div></div>
  <div style="display:flex;gap:8px;flex-wrap:wrap;align-items:center"><button class="btn" id="csv">Скачать таблицу (CSV, ${rs.length} строк)</button><span class="small">Строка таблицы открывает уровень. Заголовок столбца сортирует. «П» победа, «Х» поражение, «Н» неполная запись.</span></div>
  ${mainTable(rs)}
  ${heatmap(rs)}${cellsByLevel(rs)}${scatterCells(rs)}${capsuleRanges(rs)}${capsuleHist(rs)}${linksHidden(rs)}`;};
 function csv(){const rs=rows(),cols=Object.keys(ROWS[0]||{}),e=v=>v==null?'':Array.isArray(v)?e(v.join(' ')):/[",\n;]/.test(String(v))?'"'+String(v).replace(/"/g,'""')+'"':String(v);
  download(new Blob(['﻿'+[cols.join(','),...rs.map(r=>cols.map(c=>e(r[c])).join(','))].join('\n')+'\n'],{type:'text/csv;charset=utf-8'}),'levels-filtered.csv');}

 // ---------- level detail ----------
 function measurements(l){const out=[];for(const a of l.attempts){
   if(a.video)out.push({label:`Попытка ${a.attempt} (${OUT_RU[a.outcome]}) — увидено в записи`,m:a.video,src:'video',a});
   if(a.replay)out.push({label:`Попытка ${a.attempt} (${OUT_RU[a.outcome]}) — ${l.game==='v8E'?'точный повтор нажатий':'модель по нажатиям записи (≈)'}`,m:a.replay,src:l.game==='v8E'?'replay':'approx',a});}
  if(l.checks&&l.checks.routeLive)out.push({label:'Модель: выигрышный маршрут без пауз'+(l.game==='CF'?' (≈)':''),m:l.checks.routeLive,src:l.game==='CF'?'routeApprox':'route',a:null});return out;}
 function boardSvg(l,maxW){const rows=l.board.rows,h=rows.length,w=Math.max(...rows.map(r=>r.length)),cs=Math.max(4,Math.min(22,Math.floor(maxW/w))),W=w*cs,H=h*cs;let s='';
  const hasHashColour=l.game==='CF'&&l.board.legend&&l.board.legend['#'];
  rows.forEach((row,y)=>[...row].forEach((ch,x)=>{if(ch===' ')return;let fill,tipT;
   if(ch==='.'){if(l.game==='CF')return;fill='var(--ground)';tipT=null;}else if(ch==='#'&&!hasHashColour){fill='var(--wall)';tipT='стена';}else{const c=colourOf(l,ch);fill=c.hex;tipT=`${ch}: ${c.name}`;}
   s+=`<rect x="${x*cs}" y="${y*cs}" width="${cs-(cs>6?1:0)}" height="${cs-(cs>6?1:0)}" rx="${cs>10?2:0}" fill="${fill}"${tipT?` data-tip="${esc(tipT)}"`:''}/>`;
   if(ch==='?'&&cs>=10)s+=`<text x="${x*cs+cs/2}" y="${y*cs+cs*.72}" text-anchor="middle" style="fill:#1a1a19" font-size="${cs*.7}">?</text>`;}));
  if(l.game!=='CF'&&l.board.walls){/* entries are gaps in the bottom wall */}
  return svg(W,H,s,'поле');}
 function compositionChart(l){const bt=l.static.board.byType,ent=Object.entries(bt),W=width(),rh=24,lab=Math.min(200,W*.35),H=ent.length*rh+30,max=Math.max(...ent.map(([,v])=>v.cells));const x=scale(0,niceMax(max),lab,W-60);
  let s='';ticks(0,niceMax(max),4).forEach(v=>{s+=`<line class="gl" x1="${x(v)}" x2="${x(v)}" y1="4" y2="${H-24}"/>`+T(x(v),H-8,nf(v,0),'middle');});
  ent.forEach(([k,v],i)=>{const c=colourOf(l,k),y0=6+i*rh;s+=`<rect x="4" y="${y0+4}" width="12" height="12" rx="3" fill="${c.hex}" stroke="var(--line)"/>`+T(22,y0+15,`${k} ${c.name}`.slice(0,30));
   s+=`<rect x="${lab}" y="${y0+3}" width="${Math.max(1,x(v.cells)-lab)}" height="${rh-8}" rx="3" fill="${c.hex}" stroke="var(--line)" data-tip="${esc(`${k} ${c.name}: ${v.cells} клеток (${nf(v.share*100,0)} %)\nучастков ${v.patches}, крупнейший ${v.largest}, островков ≤3 клеток: ${v.islands}`)}"/>`+T(x(v.cells)+5,y0+15,`${v.cells} · ${nf(v.share*100,0)} %`);});
  return fig({title:'Из каких типов клеток состоит поле?',n:`n = ${l.static.board.cells} клеток, ${l.static.board.types} типов${l.static.board.mysteryTiles?`, клеток «?» ${l.static.board.mysteryTiles}`:''}`,body:svg(W,H,s,'состав поля'),note:'Количество и доля каждого типа. Клетки «?» на поле считаются отдельно и не приписываются цвету. Источник: раскладка.'});}
 function trayHtml(l){const cols=Object.entries(l.tray||{});if(!cols.length)return '<p class="unk">нет раскладки лотов</p>';
  return `<div class="tray">${cols.map(([c,toks])=>`<div class="col"><h5>${esc(c)} ↓</h5>${toks.map((t,i)=>{const k=/^#([a-z]+)$/.exec(t);if(k)return `<div class="chip lock" data-tip="${esc(`замок цвета «${k[1]}» (${i+1}-я сверху)`)}">🔒 ${esc(k[1])}</div>`;
   const m=/^([A-Za-z#])(\d+)(\?)?(?::([a-z]))?/.exec(t);if(!m)return `<div class="chip">${esc(t)}</div>`;const col=colourOf(l,m[1]);
   const tipT=`${i+1}-я сверху: ${m[1]} ${col.name}, число ${m[2]}${m[3]?'\nскрытая капсула: игрок видит «?», пока она не станет верхней (по записи/файлу: '+m[1]+m[2]+')':''}${m[4]?'\nсвязь «'+m[4]+'»: выбирается вместе с остальными капсулами набора':''}`;
   return `<div class="chip${m[3]?' hid':''}" style="background:${col.hex};color:${ink(col.hex)}" data-tip="${esc(tipT)}">${m[3]?'? ':''}${esc(m[1])}${m[2]}${m[4]?`<span class="lk">${esc(m[4])}</span>`:''}</div>`;}).join('')}</div>`).join('')}</div>
  <p class="note">Колонки слева направо, верхняя капсула — первая. Пунктир и «?» — скрытая капсула (цвет и число скрыты от игрока). Метка в углу — буква набора связей. 🔒 — замок.</p>`;}
 const kv=pairs=>`<dl class="kv">${pairs.filter(Boolean).map(([k,v])=>`<dt>${k}</dt><dd>${v}</dd>`).join('')}</dl>`;
 function staticBlock(l){const b=l.static.board,t=l.static.tray,acc=l.static.accounting;
  const bp=b?kv([['Клеток занято',f(b.cells,0)],['Ширина × высота (рамка)',`${b.w}×${b.h} (занятая область ${b.box[0]}×${b.box[1]})`],['Заполненность рамки',pct(b.fill)],['Типов (цветов)',f(b.types,0)],['Клеток «?» на поле',f(b.mysteryTiles,0)],['Участков (4-соседство)',f(b.patches,0)],['Островков ≤3 клеток',f(b.islands,0)],['Крупнейший участок',f(b.largestPatch,0)+' клеток'],['Вход',b.holeNote==='hole assumed under the middle of the board'?'принят под серединой поля (так во всех видео)':'входы уровня']]):'<p class="unk">неизвестно: нет кадра поля</p>';
  const tp=kv([['Капсул',f(t.capsules,0)],['Колонок, глубина',`${t.columns}: ${t.depth.join(', ')}`],['Сумма чисел',f(t.cells,0)],['Числа: мин / среднее / медиана / макс',`${f(t.counts.min,0)} / ${f(t.counts.mean)} / ${f(t.counts.median)} / ${f(t.counts.max,0)}`],['Разброс (СКО)',f(t.sd)],['Разных чисел, общий делитель',`${t.values}, ${t.step}`],['Частые числа (число×раз)',t.frequent.map(([n,k])=>`${n}×${k}`).join(', ')||UNK],['Доля объёма в капсулах меньше медианы',pct(t.smallVolumeShare)],['Средняя капсула / поле',pct(t.meanToBoard,1)],['Верхних капсул в начале',f(t.startTops,0)],
   ['Связи: наборов / капсул',`${t.links.sets} / ${t.links.capsules}`+(t.links.sets?` (размеры ${t.links.sizes.join(', ')}; длина связи макс ${f(t.links.lengths.max,0)}, ср. ${f(t.links.lengths.mean)}; одноцветных наборов ${t.links.sameColour}; вертикальных ${t.links.vertical}; со скрытыми ${t.links.withHidden})`:'')],
   ['Скрытых капсул',f(t.hidden.capsules,0)+(t.hidden.capsules?` (ряды сверху: ${t.hidden.byRow.map(x=>x+1).join(', ')}); скрыты и цвет, и число`:'')],['Замков',f(t.padlocks,0)],['Сходимость поле ↔ капсулы',acc?(acc.off===0?'совпадает по каждому типу':`расходится на ${acc.off} клеток`):UNK]]);
  const types=b?Object.entries(b.byType):[];
  const typeTable=types.length?`<div class="tablewrap"><table><thead><tr><th>Тип</th><th class="n">Клеток</th><th class="n">Доля</th><th class="n">Участков</th><th class="n">Крупнейший</th><th class="n">Островков ≤3</th><th class="n">У края</th><th class="n">В центре</th><th class="n">Верх / низ</th><th class="n">У входа</th><th class="n">Капсул</th><th class="n">Сумма чисел</th><th>Числа</th></tr></thead><tbody>${types.map(([k,v])=>{const c=colourOf(l,k),tt=t.byType[k];return `<tr><td><span class="gdot" style="background:${c.hex};border-radius:3px"></span>${esc(k)} ${esc(c.name)}</td><td class="n">${v.cells}</td><td class="n">${nf(v.share*100,0)} %</td><td class="n">${v.patches}</td><td class="n">${v.largest}</td><td class="n">${v.islands}</td><td class="n">${v.zone.edge}</td><td class="n">${v.zone.centre}</td><td class="n">${v.zone.top} / ${v.zone.bottom}</td><td class="n">${v.zone.nearHole}</td><td class="n">${tt?tt.capsules:k==='?'?'—':0}</td><td class="n">${tt?tt.cells:k==='?'?'—':0}</td><td>${tt?tt.counts.join(', '):''}</td></tr>`;}).join('')}</tbody></table></div><p class="note">«У входа» — ближайшие 20 % клеток к входу. Сумма чисел капсул типа сравнивается с клетками этого типа на поле (сходимость).</p>`:'';
  return `<div class="grid2"><div><h4>Поле</h4>${bp}</div><div><h4>Лоты (капсулы)</h4>${tp}</div></div>${typeTable}`;}
 function attemptsTable(l){if(!l.attempts.length)return `<p class="callout">Записанных попыток людей нет. ${l.game==='v9'?'Пилот v9 ещё не игрался людьми: все показатели — модель.':''}</p>`;
  return `<div class="tablewrap"><table><thead><tr><th>Попытка</th><th>Исход</th><th class="n">Длительность</th><th class="n">Нажатий</th><th>Условия</th><th>Видео и таймкоды</th><th>Модель совпала?</th><th>Неисправимая ошибка (модель)</th></tr></thead><tbody>${l.attempts.map(a=>{
   const dur=a.realSeconds??(isNum(a.end)&&isNum(a.start)?a.end-a.start:null);
   const sp=a.speed?`быстрая игра в настройках: ${a.speed.fastSetting==null?'неизвестно':a.speed.fastSetting?'вкл':'выкл'}; автоускорение с ${isNum(a.speed.autoFastAtMs)?nf(a.speed.autoFastAtMs/1000,1)+' с':'—'}${a.rating?`; оценка: ${esc(a.rating)}`:''}${a.interest?`, интерес: ${esc(a.interest)}`:''}`:l.game==='CF'?'скорость ×1/×2 и помощь: см. заметки ниже':'';
   const vid=a.videos?`${a.videos.map(esc).join(', ')}<br><span class="small">${mmss(a.start)} – ${mmss(a.end)}</span>`:l.game==='v8E'?'<span class="small">экспорт игры, видео нет</span>':'';
   let fat=UNK;if(a.fatal){const w=a.fatal.wins||[];if(a.fatal.error)fat=`повтор отклонён: ${esc(a.fatal.error)}`;else if(a.fatal.fatalTap)fat=`после нажатия ${a.fatal.fatalTap} из ${a.taps} (${ft(a.fatal.fatalAtMs/1000,1,'с')} модели); видимый проигрыш ${isNum(a.fatal.lossAtMs)?ft(a.fatal.lossAtMs/1000,1,'с'):'—'}; нажатий после ошибки: ${a.fatal.tapsAfterFatal}`;else if(a.outcome==='won')fat='—';else if(w.includes(null))fat='неизвестно: поиск не уложился в бюджет';else fat='не найдена: позиция оставалась выигрышной';}
   else if(!l.model)fat='неизвестно: нет модели';
   return `<tr><td>${a.attempt}</td><td>${OUT_RU[a.outcome]||esc(a.outcome)}</td><td class="n">${ft(dur,1,'с')}</td><td class="n">${a.taps}</td><td class="small">${sp}${a.note?`<br>${esc(a.note)}`:''}</td><td>${vid}</td><td>${a.replayMatches===true?'да':a.replayMatches===false?'нет':'<span class="unk">—</span>'}</td><td class="small">${fat}</td></tr>`;}).join('')}</tbody></table></div>
  <p class="note">Длительность: Colony Flow — секунды видео (скорость игры ×1/×2 не всегда видна); v8E — реальное время игрока. Победная запись не доказывает прохождение с первой попытки. «Неисправимая ошибка» — первое нажатие, после которого модель (с ожиданием окончания работы) уже не находит выигрыша.</p>`;}
 function timeCharts(meas,l){const m=meas.m,ser=m.series||{},end=m.end||1,cap=m.capacity||5,W=width(),ml={l:44,r:12,t:16,b:26},x=scale(0,end,ml.l,W-ml.r),xt=ticks(0,end,W>600?10:5);const out=[];
  // buffer
  {const H=190,y=scale(0,cap,H-ml.b,ml.t+10),free=ser.free||[];let s=axes({x,y,W,H,m:ml,xt,yt:[...Array(cap+1).keys()],yl:'свободных мест',xf:v=>nf(v,1),yf:v=>nf(v,0)});
   for(let i=0;i<free.length;i++){const v=free[i][1];if(v>1)continue;const a=free[i][0],b=i+1<free.length?free[i+1][0]:end;if(b<=a)continue;s+=`<rect x="${x(a)}" y="${ml.t+10}" width="${Math.max(.5,x(b)-x(a))}" height="${H-ml.b-ml.t-10}" fill="${v===0?'var(--full)':'var(--near)'}" opacity=".16"/>`;}
   const fp=free.concat(free.length?[[end,free.at(-1)[1]]]:[]);s+=`<path d="${stepPath(fp,x,y)}" fill="none" stroke="var(--accent)" stroke-width="2"/>`;
   for(const t of ser.taps||[])s+=`<line x1="${x(t)}" x2="${x(t)}" y1="${ml.t}" y2="${ml.t+8}" stroke="var(--text)" stroke-width="1.5" data-tip="нажатие, ${nf(t,2)} с"/>`;
   for(const [t,k] of ser.releases||[])s+=`<path d="M${x(t)},${H-ml.b-1}l-4,-7h8z" fill="var(--good)" data-tip="${esc(`освобождено мест: ${k}, ${nf(t,2)} с`)}"/>`;
   out.push(fig({title:'Сколько мест в буфере свободно в каждый момент попытки?',n:`1 попытка · ${(ser.taps||[]).length} нажатий · вместимость ${cap}`,body:svg(W,H,s,'буфер'),legend:`${leg('var(--accent)','свободных мест')}<span>│ сверху — нажатия</span><span>▲ снизу — освобождения мест</span>${leg('var(--full)','0 свободных (полный)')}${leg('var(--near)','ровно 1 свободное')}`,note:`Ось x — секунды (${meas.src==='video'?'видео':meas.src==='replay'?'модели ×1 по точному повтору':'модели ×1'}). Источник: ${SRC[meas.src][1]}.${ser.releases?'':' Освобождения в видео не размечены.'}`}));}
  // progress by time
  if(ser.cleared){const H=170,y=scale(0,1,H-ml.b,ml.t);let s=axes({x,y,W,H,m:ml,xt,yt:[0,.25,.5,.75,1],yl:'очищено поля',xf:v=>nf(v,1),yf:v=>nf(v*100,0)+' %'});
   s+=`<path d="${stepPath(ser.cleared.concat([[end,ser.cleared.at(-1)[1]]]),x,y)}" fill="none" stroke="var(--accent)" stroke-width="2"/>`;for(const t of ser.taps||[])s+=`<line x1="${x(t)}" x2="${x(t)}" y1="${ml.t-6}" y2="${ml.t+2}" stroke="var(--text)" stroke-width="1.5"/>`;
   out.push(fig({title:'Как быстро очищается поле по времени?',n:`1 попытка`,body:svg(W,H,s,'прогресс по времени'),note:`Доля очищенных исходных клеток. Плоские участки — остановки без прогресса. Источник: ${SRC[meas.src][1]}.`}));}
  // work and waiting
  if(ser.working){const H=170,mx=Math.max(1,...ser.working.map(p=>p[1]),...(ser.stalled||[]).map(p=>p[1])),y=scale(0,mx,H-ml.b,ml.t);let s=axes({x,y,W,H,m:ml,xt,yt:ticks(0,mx,Math.min(mx,4)).filter(Number.isInteger),yl:'капсул',xf:v=>nf(v,1),yf:v=>nf(v,0)});
   s+=`<path d="${stepPath(ser.working.concat([[end,ser.working.at(-1)[1]]]),x,y)}" fill="none" stroke="var(--s-a)" stroke-width="2"/>`;if(ser.stalled)s+=`<path d="${stepPath(ser.stalled.concat([[end,ser.stalled.at(-1)[1]]]),x,y)}" fill="none" stroke="var(--s-c)" stroke-width="2"/>`;
   // cascades: releases between two taps adding to 2+ places
   const taps=ser.taps||[],rel=ser.releases||[];for(let k=0;k<=taps.length;k++){const a=k?taps[k-1]:-1,b=k<taps.length?taps[k]:Infinity,g=rel.filter(([t])=>t>a&&t<=b),n=g.reduce((p,[,c])=>p+c,0);if(n>=2){const x0=x(g[0][0]),x1=x(g.at(-1)[0]);s+=`<rect x="${x0-2}" y="${ml.t-8}" width="${Math.max(4,x1-x0+4)}" height="5" rx="2" fill="var(--good)" data-tip="${esc(`каскад: ${n} мест освобождено без нового нажатия (${nf(g[0][0],1)}–${nf(g.at(-1)[0],1)} с)`)}"/>`;}}
   out.push(fig({title:'Сколько капсул работает и сколько ждёт работы?',n:`1 попытка · остановок ${f(m.capsules&&m.capsules.stops,0)} · каскадов ${f(m.releases&&m.releases.cascades,0)}`,body:svg(W,H,s,'работа и ожидание'),legend:`${leg('var(--s-a)','работают')}${leg('var(--s-c)','остановились без доступной работы')}${leg('var(--good)','каскад: 2+ места освободились без нажатия')}`,note:`Источник: ${SRC[meas.src][1]}. Отдельные интервалы каждой капсулы в данных не сохранены — показаны количества.`}));}
  // progress by tap
  const pbt=ser.progressByTap||(ser.cleared&&ser.taps?ser.taps.map(t=>{const p=ser.cleared.filter(c=>c[0]<=t).at(-1);return p?p[1]:0;}):null);
  if(pbt&&pbt.length){const H=170,n=pbt.length,xx=scale(1,Math.max(2,n),ml.l,W-ml.r),y=scale(0,1,H-ml.b,ml.t);let s=axes({x:xx,y,W,H,m:ml,xt:ticks(1,Math.max(2,n),Math.min(n,10)).filter(Number.isInteger),yt:[0,.25,.5,.75,1],xl:'номер нажатия',yl:'очищено к нажатию',xf:v=>nf(v,0),yf:v=>nf(v*100,0)+' %'});
   s+=`<path d="${pbt.map((v,i)=>(i?'L':'M')+xx(i+1).toFixed(1)+','+y(v||0).toFixed(1)).join('')}" fill="none" stroke="var(--accent)" stroke-width="2"/>`;pbt.forEach((v,i)=>s+=`<circle cx="${xx(i+1)}" cy="${y(v||0)}" r="4" fill="var(--accent)" stroke="var(--bg)" stroke-width="2" data-tip="нажатие ${i+1}: очищено ${nf((v||0)*100,0)} %"/>`);
   out.push(fig({title:'Какая доля поля очищена к каждому нажатию?',n:`${n} нажатий`,body:svg(W,H,s,'прогресс по нажатиям'),note:`Источник: ${SRC[meas.src][1]}.`}));}
  return out.join('');}
 function stagesTable(m,basis,label){const seg=m[basis];if(!seg)return `<h4>${label}</h4><p class="unk">неизвестно: ${basis==='segmentsByCleared'?'в этом источнике нет остатка поля по времени':'нет данных'}</p>`;
  const rowsDef=[['Время, с',s=>`${f(s.from,1)}–${f(s.to,1)}`],['Длительность, с',s=>f(s.duration,1)],['Свободно в среднем',s=>f(s.meanFree)],['Свободно минимум',s=>f(s.minFree,0)],['1 свободное: с (доля)',s=>`${f(s.oneFree,1)} (${pct(s.oneFreeShare)})`],['0 свободных: с (доля)',s=>`${f(s.zeroFree,1)} (${pct(s.zeroFreeShare)})`],['Эпизодов полного буфера',s=>f(s.fullEpisodes,0)],['Нажатий',s=>f(s.taps,0)],['Работают в среднем',s=>f(s.working)],['Ждут в среднем',s=>f(s.stalled)],['Возобновлений',s=>f(s.resumes,0)],['Освобождено мест',s=>f(s.releases,0)]];
  return `<h4>${label}</h4><div class="tablewrap"><table><thead><tr><th>Показатель</th>${seg.map(s=>`<th class="n">${esc(s.label)}</th>`).join('')}</tr></thead><tbody>${rowsDef.map(([t,fn])=>`<tr><td>${t}</td>${seg.map(s=>`<td class="n">${s.reached?fn(s):'<span class="unk">не достигнут</span>'}</td>`).join('')}</tr>`).join('')}</tbody></table></div>`;}
 function clearMap(meas,l){const p=meas.m.pattern;if(!p)return `<h3>Карта очистки</h3><p class="unk">Нет карты для этого источника: ${meas.src==='video'?'видео не даёт время удаления каждой клетки — выберите повтор или модель':'нет данных'}.</p>`;
  const W=Math.min(width(),560),g=p.grid,h=g.length,w=Math.max(...g.map(r=>r.length)),cs=Math.max(4,Math.min(20,Math.floor(W/w))),K=S.clearK;let s=hatchDef;
  g.forEach((row,y)=>[...row].forEach((ch,x)=>{if(ch===' ')return;const X=x*cs,Y=y*cs,sz=cs-(cs>6?1:0);if(ch==='.'){s+=`<rect x="${X}" y="${Y}" width="${sz}" height="${sz}" fill="url(#hatch)" stroke="var(--full)" stroke-width="1" data-tip="не очищена до конца попытки"/>`;return;}
   const d=+ch;if(d<K)s+=`<rect x="${X}" y="${Y}" width="${sz}" height="${sz}" rx="${cs>10?2:0}" fill="var(--q${d+1})" data-tip="очищена в ${d*10}–${d*10+10} % времени попытки"/>`;else s+=`<rect x="${X}" y="${Y}" width="${sz}" height="${sz}" rx="${cs>10?2:0}" fill="var(--panel2)" stroke="var(--line)" data-tip="ещё на поле (очистится в ${d*10}–${d*10+10} %)"/>`;}));
  const ser=meas.m.series||{},t=K/10*meas.m.end,at=(arr)=>{if(!arr)return null;const q=arr.filter(c=>c[0]<=t).at(-1);return q?q[1]:null;};
  const f5=p.fifths||[],side=v=>v==null?'—':`x ${nf(v.x)} · y ${nf(v.y)}`;
  return `<h3>Карта очистки</h3>`+fig({title:'Когда убрана каждая клетка поля?',n:`${meas.m.cells||'?'} клеток · 1 попытка`,body:`<div class="small">Момент: <input type="range" min="0" max="10" step="1" value="${K}" data-s="clearK" aria-label="момент попытки"> <b>${K*10} %</b> времени (${nf(t,1)} с): очищено ${pctT(at(ser.cleared))}, свободно мест ${ft(at(ser.free),0)}${ser.working?`, работают ${ft(at(ser.working),0)}`:''}</div>`+svg(w*cs,h*cs,s,'карта очистки'),
   legend:`${leg('var(--q1)','рано')}${leg('var(--q6)','середина')}${leg('var(--q10)','поздно')}${leg('var(--panel2)','ещё на поле в выбранный момент')}<span><i style="background:var(--panel);outline:1px solid var(--full)"></i>не очищена</span>`,
   note:`Цвет = десятая доля времени попытки, когда клетка исчезла. Средняя позиция исчезающих клеток по пятым долям (0 = лево/верх, 1 = право/низ): ${f5.map((v,i)=>`${i+1}) ${side(v)}`).join('; ')}. Последние ${p.lastCells} клеток: ${p.lastClumps} отдельных групп, у края ${pctT(p.lastOnEdge)}, высота ${ft(p.lastMeanY)}. Квартили времени очистки: ${(p.quartiles||[]).map(v=>ft(v,1)).join(' / ')} с. Источник: ${SRC[meas.src][1]}.`});}
 function measuresBlock(m){const tm=m.time||{},c=m.capsules||{},r=m.releases||{},b=m.buffer||{},g=tm.tapGaps||{};
  return `<div class="grid2"><div><h4>Буфер (вся попытка)</h4>${kv([['Вместимость',f(m.capacity,0)],['Свободно: среднее / мин / макс',`${f(b.meanFree)} / ${f(b.minFree,0)} / ${f(b.maxFree,0)}`],['Занято максимум',f(b.maxHeld,0)],['1 свободное',`${f(b.oneFree,1,'с')} (${pct(b.oneFreeShare)})`],['0 свободных',`${f(b.zeroFree,1,'с')} (${pct(b.zeroFreeShare)})`],['Эпизодов полного буфера',`${f(b.fullEpisodes,0)}${b.fullEpisodeSeconds&&b.fullEpisodeSeconds.length?' — '+b.fullEpisodeSeconds.map(v=>nf(v,1)).join(', ')+' с':''}`],['Первое место после заполнения через',f(r.firstFreeAfterFull,1,'с')]])}</div>
  <div><h4>Время</h4>${kv([['Длительность',f(m.end,1,'с')],['Первое нажатие / очистка / освобождение',`${f(tm.firstTap,1)} / ${f(tm.firstClear,1)} / ${f(tm.firstRelease,1)} с`],['Интервал между нажатиями: медиана (мин–макс)',`${f(g.median)} (${f(g.min)}–${f(g.max)}) с`],['Доля нажатий во время очистки',pct(tm.tapsWhileWorking)],['Пауз без прогресса > 5 с',`${f(tm.pausesOver5s,0)} (${f(tm.pauseSeconds,1,'с')})`],['От последнего нажатия до конца',f(tm.lastTapToEnd,1,'с')],['Хвост: последние 10 % клеток',f(tm.tail90,1,'с')]])}</div>
  <div><h4>Капсулы</h4>${kv([['Нажатий',f(c.taps,0)],['От выбора до освобождения места: медиана (мин–макс)',c.heldSeconds?`${f(c.heldSeconds.median)} (${f(c.heldSeconds.min)}–${f(c.heldSeconds.max)}) с`:UNK],['Остановок / капсул с остановками',`${f(c.stops,0)} / ${f(c.podsStopped,0)}`],['Длительность остановки: медиана, макс',c.stopSeconds?`${f(c.stopSeconds.median)}, ${f(c.stopSeconds.max)} с`:UNK],['Остаток на капсуле при остановке: медиана, макс',c.stopLeft?`${f(c.stopLeft.median)}, ${f(c.stopLeft.max,0)}`:UNK],['Возобновлений',f(c.resumes,0)],['Остались незавершёнными (остаток)',c.stuckAtEnd?(c.stuckAtEnd.length?c.stuckAtEnd.join(', '):'нет'):UNK]])}</div>
  <div><h4>Освобождения мест</h4>${kv([['Событий / мест',`${f(r.events,0)} / ${f(r.places,0)}`],['Событий сразу на 2+ места',f(r.multi,0)],['Каскадов (2+ места без нажатия)',f(r.cascades,0)],['Крупнейший каскад',f(r.maxCascade,0)+' мест']])}</div></div>`;}
 function checksBlock(l){const ch=l.checks;if(!ch)return `<p class="callout warn">Нет модели: ${esc(noteRu(l.modelNote))}. Есть только раскладка${l.attempts.some(a=>a.video)?' и наблюдения по видео':' и расшифровка нажатий'}; проверки проходимости, мин. вместимости и стратегий неизвестны.</p>`;
  const rt=ch.route,mc=ch.minCapacity,sq=ch.sequences,st=ch.start;
  const steps=rt&&rt.steps?`<div class="steps" aria-label="решения на маршруте">${rt.steps.map((d,i)=>{const c=d.legal<=1?'var(--panel2)':d.unknown?'url(#)':d.good===1?'var(--full)':d.good<d.legal?'var(--near)':'var(--q3)';return `<span style="background:${d.unknown?'repeating-linear-gradient(45deg,var(--panel),var(--panel) 3px,var(--hatch) 3px,var(--hatch) 5px)':c}" data-tip="${esc(`нажатие ${i+1}: вариантов ${d.legal}, сохраняют победу ${d.good}${d.unknown?`, неизвестно ${d.unknown}`:''}`)}"></span>`;}).join('')}</div><div class="legend">${leg('var(--panel2)','один вариант')}${leg('var(--q3)','все варианты сохраняют победу')}${leg('var(--near)','рискованно: часть вариантов проигрывает')}${leg('var(--full)','только один верный')}</div>`:'';
  return kv([['Проходим (модель)',yn(ch.winnable)+(ch.winnable==null?' — поиск не уложился в бюджет':'')+` ${srcTag(l.game==='CF'?'routeApprox':'route')}`],['Вместимость при проверке',f(ch.capacity,0)],
   ['Найденный маршрут',rt?`${rt.taps} нажатий; развилок ${rt.decisions}, рискованных ${rt.risky}, с единственным верным ${rt.tight}${rt.unknown?`, неизвестно ${rt.unknown}`:''}; риск исчезает с нажатия ${rt.safeFromTap+1} из ${rt.taps}`:UNK],
   ['Мин. вместимость буфера',mc?`${f(mc.min,0)} — ${mc.provenBelow?'меньшая проверена и не проходит (доказано)':'меньшая не проверена до конца (не доказано)'}; проверено: ${Object.entries(mc.tried).map(([k,v])=>`${k}: ${v===true?'да':v===false?'нет':'?'}`).join(', ')}`:UNK],
   ['Выигрышных последовательностей',sq?(isNum(sq.sequences)?`${nf(sq.sequences,0)} (10^${nf(sq.log10)})`:'неизвестно: подсчёт не уложился в бюджет')+' <span class="small">— перестановки независимых капсул считаются разными, это не число замыслов</span>':UNK],
   ['В начале',st?`верхних капсул ${st.tops}, можно нажать ${st.tappable}, с доступной работой сейчас ${st.useful}; доступных клеток ${st.reachableCells}`:UNK]])+steps+`<p class="note">Проверки — полный перебор на модели с ожиданием окончания работы после каждого нажатия (знает скрытые капсулы). Один найденный путь не доказывает единственность; «неизвестно» — поиск не успел.</p>`;}
 function strategiesTable(l){if(!l.strategies)return '';return `<div class="tablewrap"><table><thead><tr><th>Стратегия</th><th class="n">Побед / запусков</th><th class="n">Время победы (медиана), с</th><th class="n">Макс. занято</th><th class="n">0 свободных (медиана), с</th><th class="n">Самая долгая остановка, с</th><th>Где застревает (очищено %)</th></tr></thead><tbody>${STRATS.filter(k=>l.strategies[k]).map(k=>{const s=l.strategies[k];
  return `<tr><td>${esc(STRAT_RU[k]||s.label)}</td><td class="n">${s.wins} / ${s.runs}</td><td class="n">${f(s.endMedian,1)}</td><td class="n">${s.maxHeld!=null?f(s.maxHeld,0):'<span class="unk">—</span>'}</td><td class="n">${s.zeroFreeMedian!=null?f(s.zeroFreeMedian,1):'<span class="unk">—</span>'}</td><td class="n">${s.longestStop!=null?f(s.longestStop,1):'<span class="unk">—</span>'}</td><td class="small">${s.stuckAt&&s.stuckAt.length?summ(s.stuckAt):s.stuckAt?'—':'<span class="unk">не записано</span>'}</td></tr>`;}).join('')}</tbody></table></div><p class="note">Источник: стратегия на модели${l.game==='CF'?' (приближённо для Colony Flow)':''}; нажатие сразу, как только можно; стратегии не видят скрытые капсулы. Проигрыш стратегии не доказывает сложность для людей.</p>`;}
 const summ=a=>{const v=a.filter(isNum);if(!v.length)return '—';return v.length===1?`${v[0]} %`:`медиана ${nf(median(v),0)} % (от ${Math.min(...v)} до ${Math.max(...v)} %, n = ${v.length})`;};
 function summary(l,r){const ref=reference(l),m=ref.m,parts=[];
  if(l.static.board)parts.push(`Поле: ${r.cells} клеток, ${r.types} типов${r.mysteryTiles?`, ${r.mysteryTiles} клеток «?»`:''}.`);else parts.push('Поле: неизвестно (нет кадра).');
  parts.push(`Лоты: ${r.capsules} капсул в ${r.columns} колонках, числа ${r.countMin}–${r.countMax} (медиана ${nf(r.countMedian)}).${r.linkSets?` Связи: ${r.linkSets} наборов, ${r.linkCapsules} капсул.`:''}${r.hidden?` Скрытых: ${r.hidden}.`:''}${r.padlocks?` Замков: ${r.padlocks}.`:''}`);
  if(m&&m.buffer){const seg=(m.segmentsByCleared||m.segmentsByTime||[]).filter(s=>s&&s.reached&&isNum(s.meanFree));const tight=seg.length?seg.reduce((a,b)=>b.meanFree<a.meanFree?b:a):null;
   parts.push(`Буфер (${SRC[ref.src][0]}): свободно в среднем ${ft(m.buffer.meanFree)} из ${m.capacity}; полон ${pctT(m.buffer.zeroFreeShare)} времени, ровно одно место ${pctT(m.buffer.oneFreeShare)}.${tight?` Теснее всего на отрезке ${tight.label} (${m.segmentsByCleared?'по очистке':'по времени'}): в среднем ${ft(tight.meanFree)} свободных.`:''}`);}
  if(l.checks&&l.checks.route){const rt=l.checks.route;parts.push(`Модель: развилок ${rt.decisions}, рискованных ${rt.risky}, с единственным верным ${rt.tight}; риск исчезает с нажатия ${rt.safeFromTap+1} из ${rt.taps}. Мин. вместимость ${ft(l.checks.minCapacity&&l.checks.minCapacity.min,0)}${l.checks.minCapacity&&l.checks.minCapacity.provenBelow?' (доказано)':' (не доказано)'}.`);}
  if(l.strategies){const won=STRATS.filter(k=>k!=='random'&&k!=='plannerPatient'&&l.strategies[k]&&l.strategies[k].rate===1).length;parts.push(`Простые стратегии: проходят ${won} из 10 детерминированных; случайный выбор — ${pctT(l.strategies.random&&l.strategies.random.rate)} побед.`);}
  return `<div class="callout"><b>Что создаёт задачу</b> <span class="small">(автоматическая сводка по измерениям, не вывод о замысле)</span><br>${parts.map(esc).join(' ')}</div>`;}
 function tapsList(meas){const a=meas.a,m=meas.m,ser=m.series||{};const n=Math.max(a?a.taps:0,(ser.taps||[]).length);if(!n)return '';const pbt=ser.progressByTap;
  return `<details><summary>Нажатия по порядку (${n})</summary><div class="tablewrap" style="max-height:340px"><table><thead><tr><th class="n">#</th>${a&&a.columns?'<th>Колонка</th>':''}${a?`<th class="n">Время записи, с</th>`:''}<th class="n">Время в этом источнике, с</th>${pbt?'<th class="n">Очищено к нажатию</th>':''}</tr></thead><tbody>${[...Array(n).keys()].map(i=>`<tr><td class="n">${i+1}</td>${a&&a.columns?`<td>${esc(a.columns[i]||'')}</td>`:''}${a?`<td class="n">${f(a.tapTimes&&a.tapTimes[i],2)}</td>`:''}<td class="n">${f(ser.taps&&ser.taps[i],2)}</td>${pbt?`<td class="n">${pct(pbt[i])}</td>`:''}</tr>`).join('')}</tbody></table></div><p class="note">«Время записи» — от начала попытки по видео (Colony Flow) или по часам игрока (v8E). Время модели может отличаться: модель играет ×1 и принимает нажатие, как только оно возможно.</p></details>`;}
 VIEWS.level=()=>{const rs=rows(),keys=rs.length?rs.map(r=>r.key):ROWS.map(r=>r.key);if(!S.key||!LV[S.key])S.key=keys[0];const l=LV[S.key],r=RK[S.key];if(!l)return '<p>Нет уровней.</p>';
  const i=keys.indexOf(S.key),ms=measurements(l);let mi=S.meas;if(mi==null||mi>=ms.length){const ref=reference(l);mi=Math.max(0,ms.findIndex(x=>x.m===ref.m));}S.meas=mi;const meas=ms[mi];
  const opts=keys.map(k=>`<option value="${esc(k)}" ${k===S.key?'selected':''}>${esc(lvName(k))} — ${esc(RK[k].title)}</option>`).join('');
  const notIn=i<0?`<span class="small">(уровень вне текущего фильтра)</span>`:'';
  const src=l.source||{};
  return `<div class="levelbar"><button class="btn" data-nav="-1" aria-label="предыдущий">←</button><select id="lvsel" aria-label="уровень">${i<0?`<option selected>${esc(lvName(S.key))}</option>`:''}${opts}</select><button class="btn" data-nav="1" aria-label="следующий">→</button>${notIn}</div>
  <h2>${gdot(l.game)}${esc(lvName(l.key))} «${esc(l.title)}»</h2>
  ${kv([['Игра, версия',`${GAME_NAME[l.game]} — ${esc(l.app||'')}`],['Сложность',esc(DIFF_RU[l.difficulty]||l.difficulty)],['Набор',l.set==='owner'?'записи владельца':l.set==='site'?'видео colony-flow.com':esc(l.set)],['Вместимость буфера',`${l.capacity}${l.capacityNote?` — ${esc(l.capacityNote)}`:''}`],['Модель',`${modelRu(r)} — ${esc(noteRu(l.modelNote))}`],src.videos?['Видео',src.videos.map(esc).join(', ')+(src.recorded?` · записано ${esc(src.recorded)}`:'')]:null,src.cover?['Обложка сайта',esc(src.cover)+(src.coverNote?` — ${esc(src.coverNote)}`:'')]:null,l.boxes?['Объекты на поле',esc(l.boxes)]:null])}
  ${summary(l,r)}
  <h3>Попытки и условия</h3>${attemptsTable(l)}
  <h3>Поле и лоты</h3>
  ${l.board?`<div class="grid2"><div>${fig({title:'Раскладка поля',n:`${l.board.w}×${l.board.h}`,body:boardSvg(l,Math.min(width()-30,520)),legend:Object.entries(l.static.board?l.static.board.byType:{}).map(([k,v])=>leg(colourOf(l,k).hex,`${k} ${colourOf(l,k).name}: ${v.cells}`)).join('')+(l.game!=='CF'?leg('var(--wall)','стена')+leg('var(--ground)','земля'):''),note:l.game==='CF'?(l.board.fromVideo?'Поле восстановлено по кадру видео.':l.set==='owner'?'Поле по стартовому кадру записи.':'Поле по обложке сайта.'):'Поле из файла уровня; вход — проём в нижней стене.'})}</div><div><h4>Лоты по колонкам</h4>${trayHtml(l)}</div></div>${l.static.board?compositionChart(l):''}`:`<p class="callout warn">Поле неизвестно: ${esc(noteRu(l.modelNote))}.</p><h4>Лоты по колонкам</h4>${trayHtml(l)}`}
  ${staticBlock(l)}
  ${l.mechanicsText&&l.mechanicsText.length?`<h4>Механики и заметки расшифровки</h4><ul>${l.mechanicsText.map(t=>`<li>${esc(t)}</li>`).join('')}</ul>`:''}
  <h3>Попытка во времени</h3>
  ${ms.length?`<p><label>Источник измерений: <select id="meassel">${ms.map((x,j)=>`<option value="${j}" ${j===mi?'selected':''}>${esc(x.label)}</option>`).join('')}</select></label> ${srcTag(meas.src)}</p>${tapsList(meas)}${timeCharts(meas,l)}<h3>Пять отрезков</h3>${stagesTable(meas.m,'segmentsByCleared','По доле очищенных исходных клеток')}${stagesTable(meas.m,'segmentsByTime','По доле времени попытки')}<h3>Показатели попытки</h3>${measuresBlock(meas.m)}${clearMap(meas,l)}`:'<p class="unk">Нет измерений во времени: нет модели и нет наблюдений по видео.</p>'}
  <h3>Проверки на модели</h3>${checksBlock(l)}
  ${l.strategies?`<h3>Стратегии</h3>${strategiesTable(l)}`:''}
  <h3>Что неизвестно</h3>${unknownsOf(l)}`;};
 function unknownsOf(l){const u=[];if(!l.board)u.push('поле: нет кадра поля');if(!l.model)u.push('проходимость, мин. вместимость, решения, стратегии, неисправимая ошибка: нет модели ('+noteRu(l.modelNote)+')');
  if(l.game==='CF'&&!l.attempts.some(a=>a.video))u.push('буфер по видео: наблюдение по кадрам ещё не готово (показатели буфера — по модели)');if(!l.attempts.length)u.push('поведение людей: попыток не записано');
  if(l.checks&&l.checks.sequences&&!isNum(l.checks.sequences.sequences))u.push('число выигрышных последовательностей: подсчёт не уложился в бюджет');if(l.checks&&l.checks.minCapacity&&!l.checks.minCapacity.provenBelow)u.push('достаточна ли меньшая вместимость: не доказано');
  return `<ul>${u.map(t=>`<li>${esc(t)}</li>`).join('')}</ul>${(l.uncertain||[]).length?`<h4>Неточности расшифровки</h4><ul>${l.uncertain.map(t=>`<li class="small">${esc(t)}</li>`).join('')}</ul><p class="note">Заметки расшифровщика (на английском, как в исходных файлах).</p>`:''}`;}

 // ---------- strategies matrix ----------
 VIEWS.strategies=()=>{const rs=rows(),withM=rs.filter(r=>LV[r.key].strategies),without=rs.filter(r=>!LV[r.key].strategies);
  const cellS=(r,k)=>{const s=LV[r.key].strategies[k];if(!s)return `<td class="n"><span class="unk">—</span></td>`;const v=s.rate,kq=Math.round(v*10),dark=kq>=6;
   return `<td class="n" style="background:var(--q${kq});color:${dark?'#fff':'#1a1a19'}" data-tip="${esc(`${lvName(r.key)} · ${STRAT_RU[k]}\nпобед ${s.wins} из ${s.runs}\n${s.endMedian!=null?'время победы (медиана) '+nf(s.endMedian,1)+' с\n':''}${s.stuckAt&&s.stuckAt.length?'застревает при очистке: '+summ(s.stuckAt):''}`)}">${k==='random'?`${s.wins}/${s.runs}`:s.rate===1?'✓':s.rate===0?'✗':pctT(s.rate)}</td>`;};
  const sumRow=STRATS.map(k=>{const v=withM.map(r=>LV[r.key].strategies[k]).filter(Boolean);if(!v.length)return '<td></td>';return k==='random'?`<td class="n">медиана ${pctT(median(v.map(s=>s.rate)))}</td>`:`<td class="n">${v.filter(s=>s.rate===1).length} / ${v.length}</td>`;}).join('');
  const perGame=GAMES.map(g=>{const v=withM.filter(r=>r.game===g);if(!v.length)return '';return `<tr><td>${gdot(g)}${GAME_NAME[g]} <span class="small">(${v.length})</span></td>${STRATS.map(k=>{const a=v.map(r=>LV[r.key].strategies[k]).filter(Boolean);if(!a.length)return '<td></td>';return k==='random'?`<td class="n">${pctT(median(a.map(s=>s.rate)))}</td>`:`<td class="n">${a.filter(s=>s.rate===1).length} / ${a.length}</td>`;}).join('')}</tr>`;}).join('');
  return `<h2>Стратегии</h2>${banner(rs.length)}
  <p>Строки — уровни с моделью, столбцы — простые стратегии. ✓ стратегия прошла, ✗ не прошла; для случайного выбора — побед из 100 запусков. Цвет — доля побед (темнее = больше). Все стратегии нажимают, как только можно (кроме «планирование + ожидание»), и не знают скрытых капсул. Colony Flow — на приближённой модели.</p>
  <div class="tablewrap" style="max-height:75vh"><table><thead><tr><th>Уровень</th>${STRATS.map(k=>`<th class="n" style="white-space:normal;min-width:70px">${STRAT_RU[k]}</th>`).join('')}</tr></thead><tbody>
  ${withM.map(r=>`<tr><td style="white-space:nowrap">${gdot(r.game)}<a href="#tab=level&key=${esc(r.key)}" data-go="${esc(r.key)}">${esc(lvName(r.key))}</a> <span class="small">${esc(DIFF_RU[r.difficulty])}</span></td>${STRATS.map(k=>cellS(r,k)).join('')}</tr>`).join('')}
  <tr><td><b>Пройдено уровней</b> <span class="small">(n = ${withM.length})</span></td>${sumRow}</tr>${perGame}</tbody></table></div>
  ${without.length?`<h3>Без модели (${without.length})</h3><p class="small">Для них стратегии не проверялись — есть только раскладка и наблюдения.</p><ul>${without.map(r=>`<li>${gdot(r.game)}<a href="#tab=level&key=${esc(r.key)}" data-go="${esc(r.key)}">${esc(lvName(r.key))}</a>: ${esc(noteRu(r.modelNote))}</li>`).join('')}</ul>`:''}
  <p class="note">Проигрыш стратегии не доказывает сложность уровня для людей и не доказывает нерешаемость.</p>`;};

 // ---------- comparison ----------
 const CMP=[['cells','клеток на поле','раскладка'],['types','типов клеток','раскладка'],['capsules','капсул','раскладка'],['columns','колонок','раскладка'],['countMean','среднее число на капсуле','раскладка'],['countMedian','медиана числа','раскладка'],['countMax','макс. число','раскладка'],['countSd','разброс чисел (СКО)','раскладка'],['linkSets','наборов связей','раскладка'],['linkCapsules','капсул в связях','раскладка'],['hidden','скрытых капсул','раскладка'],['minCapacity','мин. вместимость буфера','модель'],['risky','рискованных решений на маршруте','модель'],['tight','решений с единственным верным','модель'],['startUseful','полезных капсул в начале','модель'],['meanFree','свободных мест в среднем','опорная попытка'],['zeroFreeShare','доля времени с полным буфером','опорная попытка',true],['oneFreeShare','доля времени с 1 свободным','опорная попытка',true],['fullEpisodes','эпизодов полного буфера','опорная попытка'],['tapGapMedian','интервал между нажатиями (медиана), с','опорная попытка'],['refSeconds','время опорной попытки, с','опорная попытка'],['wonSeconds','время победной попытки, с','запись'],['s_random','случайный выбор: доля побед','стратегия',true],['s_planner','планирование: доля побед','стратегия',true],['s_left-to-right','слева направо: доля побед','стратегия',true],['s_most-now','больше очистит сейчас: доля побед','стратегия',true],['s_fast-release','быстрее освободит: доля побед','стратегия',true]];
 function groupStats(rs,k){const v=rs.map(r=>r[k]).filter(isNum);return {n:v.length,median:median(v),mean:mean(v),min:v.length?Math.min(...v):null,max:v.length?Math.max(...v):null,q1:quant(v,.25),q3:quant(v,.75),unk:rs.length-v.length};}
 function stripChart({title,groups,share,xl,note,nLabel}){const W=width(),lab=Math.min(170,W*.32),rh=40,H=groups.length*rh+40,all=groups.flatMap(g=>g.vals.map(v=>v.v)).filter(isNum);if(!all.length)return '';
  const lo=Math.min(0,...all),hi=share?1:niceMax(Math.max(...all)),x=scale(lo,hi,lab,W-16);let s='';
  for(const v of ticks(lo,hi,W>600?6:4)){s+=`<line class="gl" x1="${x(v)}" x2="${x(v)}" y1="6" y2="${H-30}"/>`+T(x(v),H-16,share?nf(v*100,0)+' %':nf(v,2),'middle');}
  if(xl)s+=T((lab+W-16)/2,H-2,xl,'middle');
  groups.forEach((g,i)=>{const y0=10+i*rh,yc=y0+rh/2-4,ok=g.vals.filter(v=>isNum(v.v));s+=T(4,yc-2,g.label,'start','font-weight="600"')+T(4,yc+11,`n = ${ok.length}${g.vals.length-ok.length?`, неизв. ${g.vals.length-ok.length}`:''}`,'start','font-size="10"');
   s+=`<line x1="${lab}" x2="${W-16}" y1="${yc}" y2="${yc}" class="gl"/>`;
   ok.forEach((v,j)=>{const jit=((j*37)%11-5)*1.6;s+=`<circle cx="${x(v.v)}" cy="${yc+jit}" r="4" fill="${g.color}" fill-opacity=".75" stroke="var(--bg)" stroke-width="1" data-tip="${esc(v.tip)}"/>`;});
   const md=median(ok.map(v=>v.v));if(isNum(md))s+=`<line x1="${x(md)}" x2="${x(md)}" y1="${yc-13}" y2="${yc+13}" stroke="var(--text)" stroke-width="2.5" data-tip="${esc(`${g.label}: медиана ${share?pctT(md):nf(md,2)}`)}"/>`;});
  return fig({title,n:nLabel,body:svg(W,H,s,title),legend:'<span>● уровень</span><span>┃ медиана</span>',note});}
 VIEWS.compare=()=>{const rs=rows(),gs=GAMES.filter(g=>rs.some(r=>r.game===g));const groups=[];for(const g of gs){groups.push({key:g,label:GAME_NAME[g],game:g,rs:rs.filter(r=>r.game===g)});}
  const sub=[];for(const g of gs)for(const d of DIFFS){const v=rs.filter(r=>r.game===g&&r.difficulty===d);if(v.length)sub.push({key:g+'|'+d,label:`${GAME_NAME[g]} · ${DIFF_RU[d]}`,game:g,rs:v});}
  const tableFor=(gr,title)=>`<h3>${title}</h3><div class="tablewrap"><table><thead><tr><th>Показатель</th><th>Источник</th>${gr.map(g=>`<th class="n">${gdot(g.game)}${esc(g.label)}<br><span class="small">${g.rs.length} ур.</span></th>`).join('')}</tr></thead><tbody>${CMP.map(([k,t,src,sh])=>`<tr><td>${t}</td><td class="small">${src}</td>${gr.map(g=>{const st=groupStats(g.rs,k);return `<td class="n" data-tip="${esc(`${g.label} · ${t}\nn = ${st.n}${st.unk?`, неизвестно ${st.unk}`:''}\nмедиана ${sh?pctT(st.median):ft(st.median)}, среднее ${sh?pctT(st.mean):ft(st.mean)}\nмин ${sh?pctT(st.min):ft(st.min)}, макс ${sh?pctT(st.max):ft(st.max)}`)}">${st.n?(sh?pct(st.median):f(st.median)):'<span class="unk">неизв.</span>'} <span class="small">n=${st.n}</span></td>`;}).join('')}</tr>`).join('')}</tbody></table></div>`;
  const [k,t,src,sh]=CMP.find(c=>c[0]===S.cmpMetric)||CMP[0];const gr=S.cmpBy==='game'?groups:sub;
  const chart=stripChart({title:`Как распределено «${t}» по играм${S.cmpBy==='game'?'':' и сложности'}?`,groups:gr.map(g=>({label:g.label,color:`var(--g-${g.game})`,vals:g.rs.map(r=>({v:r[k],tip:`${lvName(r.key)} «${r.title}»: ${sh?pctT(r[k]):ft(r[k])}${src==='опорная попытка'?`\nисточник: ${SRC[rowRefSrc(r)]?SRC[rowRefSrc(r)][1]:'нет данных'}`:''}`}))})),share:sh,xl:t,nLabel:`n = ${gr.reduce((a,g)=>a+g.rs.filter(r=>isNum(r[k])).length,0)} уровней`,note:`Источник: ${src}.${src==='опорная попытка'?' Опорная попытка смешивает источники (видео CF, точный повтор v8E, модель для v9 и CF без видео) — смотрите метки в подсказках.':''}${k==='wonSeconds'?' Время CF — секунды видео, v8E — часы игрока; v9 людьми не игрался.':''}`});
  const small=['cells','capsules','countMean','linkCapsules','hidden','meanFree','zeroFreeShare','minCapacity','s_random','s_planner'].map(kk=>{const c=CMP.find(x=>x[0]===kk);return stripChart({title:`${c[1][0].toUpperCase()+c[1].slice(1)}: как различаются игры?`,groups:groups.map(g=>({label:g.label,color:`var(--g-${g.game})`,vals:g.rs.map(r=>({v:r[kk],tip:`${lvName(r.key)}: ${c[3]?pctT(r[kk]):ft(r[kk])}`}))})),share:c[3],nLabel:`n = ${groups.reduce((a,g)=>a+g.rs.filter(r=>isNum(r[kk])).length,0)}`,note:`Источник: ${c[2]}.`});}).join('');
  const firsts={};for(const g of gs){const f0={};for(const r of rs.filter(x=>x.game===g).sort((a,b)=>a.level-b.level))for(const [kk,on] of [['links',r.linkSets>0],['hidden',r.hidden>0],['padlocks',r.padlocks>0],['boxes',!!r.boxes],['boardMystery',r.mysteryTiles>0]])if(on&&f0[kk]==null)f0[kk]=r.level;firsts[g]=f0;}
  const FN={links:'связи',hidden:'скрытые капсулы',padlocks:'замки',boxes:'объекты на поле',boardMystery:'клетки «?» на поле'};
  return `<h2>Сравнение: Colony Flow, v8E, v9</h2>${banner(rs.length)}
  <p class="callout">Медианы и распределения считаются по уровням (каждый уровень — одна точка; повторные попытки не дублируют раскладку). Разные игры и разные источники не усредняются вместе. v9 людьми не игрался — его «опорная попытка» всегда модель. Для уровней Colony Flow без модели есть только раскладка и наблюдения по видео.</p>
  <div class="small" style="margin:8px 0">Показатель: <select data-s="cmpMetric">${CMP.map(([kk,tt])=>`<option value="${kk}" ${kk===k?'selected':''}>${tt}</option>`).join('')}</select> &nbsp; Группы: <select data-s="cmpBy"><option value="game" ${S.cmpBy==='game'?'selected':''}>по играм</option><option value="diff" ${S.cmpBy==='diff'?'selected':''}>по играм и сложности</option></select></div>
  ${chart}${tableFor(groups,'Медианы по играм')}${tableFor(sub,'Медианы по играм и сложности')}
  <h3>Когда впервые появляется механика (среди изученных уровней)</h3><div class="tablewrap"><table><thead><tr><th>Механика</th>${gs.map(g=>`<th>${gdot(g)}${GAME_NAME[g]}</th>`).join('')}</tr></thead><tbody>${Object.entries(FN).map(([kk,t])=>`<tr><td>${t}</td>${gs.map(g=>`<td>${firsts[g][kk]!=null?'L'+firsts[g][kk]:'<span class="small">не встречается</span>'}</td>`).join('')}</tr>`).join('')}</tbody></table></div><p class="note">Первое появление в изученной выборке, а не в игре: у Colony Flow изучены не все уровни подряд.</p>
  <h3>Ключевые показатели по играм</h3><div class="grid2">${small}</div>
  <h3>Буфер по отрезкам, по играм</h3>${gs.map(g=>heatmap(rs.filter(r=>r.game===g))).join('')}`;};

 // ---------- similarity ----------
 const FEAT={logic:[['ln клеток',r=>Math.log(r.cells||1)],['типов',r=>r.types],['капсул',r=>r.capsules],['колонок',r=>r.columns],['среднее число',r=>r.countMean],['разброс чисел',r=>r.countSd],['наборов связей',r=>r.linkSets],['капсул в связях',r=>r.linkCapsules],['скрытых',r=>r.hidden],['рискованных решений',r=>r.risky],['единств. верных',r=>r.tight]],
  rhythm:[...[0,1,2,3,4].map(i=>[`свободно, отрезок ${i+1}`,r=>{const m=reference(LV[r.key]).m;const s=m&&m.segmentsByTime&&m.segmentsByTime[i];return s?s.meanFree??null:null;}]),...[0,1,2,3,4].map(i=>[`доля 0 свободных, отрезок ${i+1}`,r=>{const m=reference(LV[r.key]).m;const s=m&&m.segmentsByTime&&m.segmentsByTime[i];return s?s.zeroFreeShare??null:null;}]),['интервал нажатий',r=>{const m=reference(LV[r.key]).m;return m&&m.time&&m.time.tapGaps?m.time.tapGaps.median??null:null;}]]};
 const ZS={};for(const [name,fs] of Object.entries(FEAT)){const vec=ROWS.map(r=>fs.map(([,fn])=>fn(r)));ZS[name]={mu:fs.map((_,j)=>mean(vec.map(v=>v[j]))),sd:fs.map((_,j)=>{const mu=mean(vec.map(v=>v[j]));return Math.sqrt(mean(vec.map(v=>isNum(v[j])?(v[j]-mu)**2:null))||1)||1;})};}
 const zOf=(name,r)=>FEAT[name].map(([,fn],j)=>{const v=fn(r);return isNum(v)?(v-ZS[name].mu[j])/ZS[name].sd[j]:null;});
 VIEWS.similar=()=>{const rs=rows();const keys=ROWS.map(r=>r.key);if(!S.simA||!RK[S.simA])S.simA=(rs[0]||ROWS[0]).key;if(!S.simB||!RK[S.simB]){const n=D.similar[S.simA]&&D.similar[S.simA].logic[0];S.simB=n?n[0]:keys[1]||keys[0];}
  const A=RK[S.simA],B=RK[S.simB],opt=sel=>keys.map(k=>`<option value="${esc(k)}" ${k===sel?'selected':''}>${esc(lvName(k))} — ${esc(RK[k].title)}</option>`).join('');
  const near=(k,kind)=>{const v=D.similar[k]&&D.similar[k][kind];return v&&v.length?v.map(([kk,d])=>`<a href="#" data-sim="${esc(kk)}">${esc(lvName(kk))}</a> <span class="small">${nf(d,2)}</span>`).join(', '):'<span class="unk">неизвестно</span>';};
  const cmpTable=name=>{const za=zOf(name,A),zb=zOf(name,B),fs=FEAT[name];const diffs=fs.map((f0,j)=>({t:f0[0],a:f0[1](A),b:f0[1](B),d:isNum(za[j])&&isNum(zb[j])?Math.abs(za[j]-zb[j]):null}));const ok=diffs.filter(x=>isNum(x.d));
   const dist=ok.length?Math.sqrt(mean(ok.map(x=>x.d**2))):null,close=ok.slice().sort((a,b)=>a.d-b.d).slice(0,3),far=ok.slice().sort((a,b)=>b.d-a.d).filter(x=>x.d>.5).slice(0,3);
   return `<h4>${name==='logic'?'Логика: поле, лоты, связи, скрытые, риск':'Ритм: свободные места и полный буфер по отрезкам времени, темп нажатий'}</h4><p>Расстояние ${ft(dist)} <span class="small">(среднеквадратичная разница стандартизованных признаков; 0 — одинаково, около 1 — обычная разница между уровнями; по ${ok.length} из ${fs.length} признаков)</span>. ${close.length?`Ближе всего: ${close.map(x=>x.t).join(', ')}.`:''} ${far.length?`Сильнее всего различаются: ${far.map(x=>x.t).join(', ')}.`:'Заметных различий нет.'}</p>
   <div class="tablewrap"><table><thead><tr><th>Признак</th><th class="n">${esc(lvName(A.key))}</th><th class="n">${esc(lvName(B.key))}</th><th>Разница (в ст. откл.)</th></tr></thead><tbody>${diffs.map(x=>`<tr><td>${x.t}</td><td class="n">${f(x.a)}</td><td class="n">${f(x.b)}</td><td>${isNum(x.d)?`<span style="display:inline-block;height:9px;border-radius:2px;background:var(--accent);width:${Math.min(120,x.d*40)}px;vertical-align:0"></span> ${nf(x.d,2)}`:UNK}</td></tr>`).join('')}</tbody></table></div>`;};
  const mutual=kind=>{const pairs=[];const seen=new Set();for(const r of rs){const n=D.similar[r.key]&&D.similar[r.key][kind]&&D.similar[r.key][kind][0];if(!n)continue;const back=D.similar[n[0]]&&D.similar[n[0]][kind]&&D.similar[n[0]][kind][0];if(back&&back[0]===r.key){const id=[r.key,n[0]].sort().join('|');if(!seen.has(id)){seen.add(id);pairs.push([r.key,n[0],n[1]]);}}}
   pairs.sort((a,b)=>a[2]-b[2]);return pairs.length?`<ul>${pairs.map(([a,b,d])=>`<li><a href="#" data-pair="${esc(a)}|${esc(b)}">${esc(lvName(a))} ↔ ${esc(lvName(b))}</a> <span class="small">расстояние ${nf(d,2)}${RK[a].game!==RK[b].game?' · разные игры':''}</span></li>`).join('')}</ul>`:'<p class="small">нет</p>';};
  const segs=(r)=>{const m=reference(LV[r.key]).m;return m&&m.segmentsByTime?m.segmentsByTime.map(s=>s&&s.reached?s.meanFree:null):null;};
  const lineCmp=()=>{const W=Math.min(width(),640),H=200,m={l:40,r:14,t:16,b:34},x=scale(0,4,m.l,W-m.r),y=scale(0,5,H-m.b,m.t);let s=axes({x,y,W,H,m,xt:[0,1,2,3,4],yt:[0,1,2,3,4,5],xl:'отрезок времени попытки',yl:'свободно в среднем',xf:v=>['0–20','20–40','40–60','60–80','80–100'][v]+' %',yf:v=>nf(v,0)});
   [[A,'var(--s-a)'],[B,'var(--s-b)']].forEach(([r,c])=>{const v=segs(r);if(!v)return;const pts=v.map((q,i)=>isNum(q)?[x(i),y(q)]:null).filter(Boolean);s+=`<path d="${pts.map((p,i)=>(i?'L':'M')+p[0].toFixed(1)+','+p[1].toFixed(1)).join('')}" fill="none" stroke="${c}" stroke-width="2"/>`;v.forEach((q,i)=>{if(isNum(q))s+=`<circle cx="${x(i)}" cy="${y(q)}" r="4.5" fill="${c}" stroke="var(--bg)" stroke-width="2" data-tip="${esc(`${lvName(r.key)}, отрезок ${i+1}: ${nf(q)} свободно`)}"/>`;});});
   return fig({title:'Похож ли ритм: как меняется запас мест по ходу попытки?',n:'2 уровня, по одной опорной попытке',body:svg(W,H,s,'ритм двух уровней'),legend:leg('var(--s-a)',`${lvName(A.key)} (${SRC[reference(LV[A.key]).src]?SRC[reference(LV[A.key]).src][0]:'нет'})`)+leg('var(--s-b)',`${lvName(B.key)} (${SRC[reference(LV[B.key]).src]?SRC[reference(LV[B.key]).src][0]:'нет'})`),note:'Источники опорных попыток могут различаться (видео, точный повтор, модель).'});};
  const boards=[A,B].map(r=>{const l=LV[r.key];return `<div><h4>${gdot(r.game)}${esc(lvName(r.key))} «${esc(r.title)}»</h4>${l.board?boardSvg(l,Math.min(width()/2-30,300)):'<p class="unk">поле неизвестно</p>'}${trayHtml(l)}</div>`;}).join('');
  return `<h2>Похожесть уровней</h2>${banner(rs.length)}
  <p class="callout">Сходство логики (поле, лоты, связи, скрытые, риск на маршруте модели) и ритма (запас мест и полный буфер по отрезкам времени, темп нажатий) считается раздельно. Сходство картинки не измеряется — показаны только названия. Переименование цветов может сохранить задачу, поворот поля — изменить входы; автоматически такие случаи не объединяются.</p>
  <h3>Сравнить два уровня</h3><div class="levelbar"><select data-s="simA" aria-label="уровень A">${opt(S.simA)}</select><span>и</span><select data-s="simB" aria-label="уровень B">${opt(S.simB)}</select></div>
  <div class="grid2">${boards}</div>${cmpTable('logic')}${cmpTable('rhythm')}${lineCmp()}
  <h3>Взаимно ближайшие пары</h3><div class="grid2"><div><h4>По логике</h4>${mutual('logic')}</div><div><h4>По ритму</h4>${mutual('rhythm')}</div></div>
  <h3>Три ближайших для каждого уровня</h3><div class="tablewrap" style="max-height:60vh"><table><thead><tr><th>Уровень</th><th>Название</th><th>Ближайшие по логике (расстояние)</th><th>Ближайшие по ритму (расстояние)</th></tr></thead><tbody>${rs.map(r=>`<tr><td style="white-space:nowrap">${gdot(r.game)}<a href="#" data-sim="${esc(r.key)}">${esc(lvName(r.key))}</a></td><td>${esc(r.title)}</td><td>${near(r.key,'logic')}</td><td>${near(r.key,'rhythm')}</td></tr>`).join('')}</tbody></table></div>
  <p class="note">Ближайшие соседи ищутся среди всех измеренных уровней (фильтр влияет только на список). Уровень без нужных признаков (меньше 60 %) не попадает в соседи.</p>`;};

 // ---------- time ----------
 VIEWS.time=()=>{const rs=rows(),gs=GAMES.filter(g=>rs.some(r=>r.game===g));const route=r=>{const l=LV[r.key];return l.checks&&l.checks.routeLive?l.checks.routeLive.end:null;};
  const strip=(k,title,fn,note)=>stripChart({title,groups:gs.map(g=>({label:GAME_NAME[g],color:`var(--g-${g})`,vals:rs.filter(r=>r.game===g).map(r=>({v:fn(r),tip:`${lvName(r.key)} «${r.title}»: ${ft(fn(r),1,'с')}`}))})),xl:'секунды',nLabel:`n = ${rs.filter(r=>isNum(fn(r))).length} уровней`,note});
  const statsRow=(label,v)=>{const st={n:v.filter(isNum).length,median:median(v),mean:mean(v),q1:quant(v,.25),q3:quant(v,.75),min:v.filter(isNum).length?Math.min(...v.filter(isNum)):null,max:v.filter(isNum).length?Math.max(...v.filter(isNum)):null};return `<tr><td>${label}</td><td class="n">${st.n}</td><td class="n">${f(st.median,1)}</td><td class="n">${f(st.mean,1)}</td><td class="n">${st.n?`${nf(st.q1,1)}–${nf(st.q3,1)}`:UNK}</td><td class="n">${st.n?`${nf(st.min,1)}–${nf(st.max,1)}`:UNK}</td></tr>`;};
  const tbl=(title,fn,src)=>{const lines=[];for(const g of gs){const gr=rs.filter(r=>r.game===g);lines.push(statsRow(`${gdot(g)}<b>${GAME_NAME[g]}</b>`,gr.map(fn)));for(const d of DIFFS){const v=gr.filter(r=>r.difficulty===d);if(v.length)lines.push(statsRow(`&nbsp;&nbsp;${DIFF_RU[d]}`,v.map(fn)));}
    for(const [lab,p] of [['&nbsp;&nbsp;до 150 клеток',r=>r.cells<150],['&nbsp;&nbsp;150–299 клеток',r=>r.cells>=150&&r.cells<300],['&nbsp;&nbsp;300+ клеток',r=>r.cells>=300],['&nbsp;&nbsp;со связями',r=>r.linkSets>0],['&nbsp;&nbsp;со скрытыми',r=>r.hidden>0],['&nbsp;&nbsp;без связей и скрытых',r=>!r.linkSets&&!r.hidden]]){const v=gr.filter(p);if(v.length)lines.push(statsRow(lab,v.map(fn)));}}
   return `<h3>${title}</h3><p class="small">${src}</p><div class="tablewrap"><table><thead><tr><th>Группа</th><th class="n">n</th><th class="n">Медиана, с</th><th class="n">Среднее, с</th><th class="n">Q1–Q3, с</th><th class="n">Мин–макс, с</th></tr></thead><tbody>${lines.join('')}</tbody></table></div>`;};
  const W=width(),H=300,m={l:48,r:14,t:20,b:40},ok=rs.filter(r=>isNum(r.cells)&&isNum(route(r)));let sc='';
  if(ok.length){const x=scale(0,niceMax(Math.max(...ok.map(r=>r.cells))),m.l,W-m.r),y=scale(0,niceMax(Math.max(...ok.map(route))),H-m.b,m.t);sc=axes({x,y,W,H,m,xt:ticks(...x.d,6),yt:ticks(...y.d,5),xl:'клеток на поле',yl:'секунд модели',xf:v=>nf(v,0),yf:v=>nf(v,0)});
   for(const r of ok)sc+=`<circle cx="${x(r.cells)}" cy="${y(route(r))}" r="5" fill="var(--g-${r.game})" fill-opacity=".85" stroke="var(--bg)" stroke-width="2" data-tip="${esc(`${lvName(r.key)}: ${r.cells} клеток, маршрут модели ${nf(route(r),1)} с`)}"/>`;}
  return `<h2>Время</h2>${banner(rs.length)}
  <p class="callout">Время из разных источников не смешивается. <b>Время победной попытки</b> — длительность записанной победы: Colony Flow — секунды видео (скорость ×1/×2 видна не всегда), v8E — часы игрока (×2 после автоускорения или с быстрой игрой). <b>До первой победы</b> — сумма наблюдаемых попыток до первой победы включительно; по короткой победной записи нельзя узнать полное время освоения. <b>Маршрут модели</b> — один и тот же способ для всех игр: модель нажимает без пауз при скорости ×1. v9 людьми не игрался.</p>
  <div class="grid2">${strip('wonSeconds','Сколько длится победная попытка?',r=>r.wonSeconds,'Источник: запись (видео CF / часы игрока v8E).')}${strip('secondsToFirstWin','Сколько наблюдаемого времени ушло до первой победы?',r=>r.secondsToFirstWin,'Сумма записанных попыток до первой победы включительно. Источник: запись.')}</div>
  ${strip('route','Сколько длится уровень, если играть без пауз (модель)?',route,'Источник: модель, выигрышный маршрут; для Colony Flow приближённо. Сопоставимо между играми.')}
  ${ok.length?fig({title:'Растёт ли время уровня с размером поля?',n:`n = ${ok.length} уровней`,body:svg(W,H,sc,'время и клетки'),legend:gameLegend(GAMES.filter(g=>ok.some(r=>r.game===g))),note:'Время выигрышного маршрута модели (без пауз, ×1). Источник: модель.'}):''}
  ${tbl('Время победной попытки',r=>r.wonSeconds,'Источник: запись. Группы по сложности, размеру поля и механикам внутри каждой игры.')}
  ${tbl('Наблюдаемое время до первой победы',r=>r.secondsToFirstWin,'Источник: запись.')}
  ${tbl('Маршрут модели без пауз',route,'Источник: модель.')}`;};

 // ---------- completeness ----------
 VIEWS.complete=()=>{const rs=rows(),keys=new Set(rs.map(r=>r.key)),C=D.completeness.filter(c=>keys.has(c.key));
  const cnt=(g,p)=>C.filter(c=>RK[c.key].game===g&&p(c)).length;
  const sumT=`<div class="tablewrap"><table><thead><tr><th>Игра</th><th class="n">Измерено / ожидается</th><th class="n">С полем</th><th class="n">С попытками</th><th class="n">Попыток</th><th class="n">Попыток с видео-наблюдением</th><th class="n">Попыток с повтором на модели</th><th class="n">С моделью</th><th class="n">Без модели</th></tr></thead><tbody>${GAMES.map(g=>`<tr><td>${gdot(g)}${GAME_NAME[g]}</td><td class="n">${cnt(g,()=>1)} / ${EXPECTED[g]}</td><td class="n">${cnt(g,c=>c.board!=='missing')}</td><td class="n">${cnt(g,c=>c.attempts>0)}</td><td class="n">${C.filter(c=>RK[c.key].game===g).reduce((a,c)=>a+c.attempts,0)}</td><td class="n">${C.filter(c=>RK[c.key].game===g).reduce((a,c)=>a+c.video,0)}</td><td class="n">${C.filter(c=>RK[c.key].game===g).reduce((a,c)=>a+c.replay,0)}</td><td class="n">${cnt(g,c=>c.model!=='none')}</td><td class="n">${cnt(g,c=>c.model==='none')}</td></tr>`).join('')}</tbody></table></div>`;
  const BRU={'video frame':'кадр видео','start frame':'стартовый кадр записи','site cover':'обложка сайта','level definition':'файл уровня','missing':'нет'},TRU={'transcribed from video':'расшифровка видео','level definition':'файл уровня'};
  const reasons={};for(const c of C)if(c.model==='none'){const k=noteRu(c.modelNote).replace(/\(.*\)/,'(…)').replace(/\d+ клеток \([\d.]+ %\)/,'N клеток');reasons[k]=(reasons[k]||0)+1;}
  const cc=D.crossCheck.filter(c=>keys.has(c.key));let ccHtml;
  if(!cc.length)ccHtml='<p class="callout">Сверки пока нет: наблюдения буфера по кадрам видео Colony Flow ещё не готовы. Когда они появятся, здесь будет сравнение «увидено в записи» с моделью по тем же нажатиям.</p>';
  else{const W=Math.min(width(),560),H=W*0.8,m={l:44,r:14,t:16,b:40},mx=5,x=scale(0,mx,m.l,W-m.r),y=scale(0,mx,H-m.b,m.t);let s=axes({x,y,W,H,m,xt:[0,1,2,3,4,5],yt:[0,1,2,3,4,5],xl:'свободно в среднем: модель',yl:'видео',xf:v=>nf(v,0),yf:v=>nf(v,0)});
   s+=`<line x1="${x(0)}" y1="${y(0)}" x2="${x(mx)}" y2="${y(mx)}" stroke="var(--muted)" stroke-width="1"/>`;for(const c of cc)if(isNum(c.videoMeanFree)&&isNum(c.modelMeanFree))s+=`<circle cx="${x(c.modelMeanFree)}" cy="${y(c.videoMeanFree)}" r="5" fill="var(--g-CF)" stroke="var(--bg)" stroke-width="2" data-tip="${esc(`${lvName(c.key)}, попытка ${c.attempt}\nвидео ${nf(c.videoMeanFree)} · модель ${nf(c.modelMeanFree)}`)}"/>`;
   ccHtml=fig({title:'Совпадает ли буфер в записи с моделью по тем же нажатиям?',n:`n = ${cc.length} попыток`,body:svg(W,H,s,'сверка видео и модели'),note:'Точка на диагонали — модель и запись совпадают. Модель Colony Flow приближённая (один вход, обход по краю).'})+
    `<div class="tablewrap"><table><thead><tr><th>Уровень</th><th class="n">Попытка</th><th>Исход</th><th class="n">Свободно в ср.: видео / модель</th><th class="n">Доля 0 свободных: видео / модель</th><th class="n">Длительность: видео / модель, с</th><th>Исход модели</th></tr></thead><tbody>${cc.map(c=>`<tr><td><a href="#tab=level&key=${esc(c.key)}" data-go="${esc(c.key)}">${esc(lvName(c.key))}</a></td><td class="n">${c.attempt}</td><td>${OUT_RU[c.outcome]||esc(c.outcome)}</td><td class="n">${f(c.videoMeanFree)} / ${f(c.modelMeanFree)}</td><td class="n">${pct(c.videoZero)} / ${pct(c.modelZero)}</td><td class="n">${f(c.videoEnd,1)} / ${f(c.modelEnd,1)}</td><td>${esc({Won:'победа',Jammed:'поражение',Playing:'не закончена'}[c.modelOutcome]||c.modelOutcome)}</td></tr>`).join('')}</tbody></table></div>`;}
  return `<h2>Полнота исследования</h2>${banner(rs.length)}${sumT}
  <h3>Почему у части уровней нет модели</h3>${Object.keys(reasons).length?`<ul>${Object.entries(reasons).sort((a,b)=>b[1]-a[1]).map(([k,n])=>`<li>${esc(k)} — ${n} ур.</li>`).join('')}</ul><p class="note">У таких уровней есть только раскладка и наблюдения по видео: проходимость, мин. вместимость, решения и стратегии для них неизвестны.</p>`:'<p class="small">У всех выбранных уровней есть модель.</p>'}
  <h3>Сверка: запись против модели (Colony Flow)</h3>${ccHtml}
  <h3>По уровням</h3><div class="tablewrap" style="max-height:70vh"><table><thead><tr><th>Уровень</th><th>Поле</th><th>Лоты</th><th class="n">Попыток</th><th class="n">С видео</th><th class="n">С повтором</th><th>Модель</th><th>Неточности</th></tr></thead><tbody>${C.map(c=>`<tr><td style="white-space:nowrap">${gdot(RK[c.key].game)}<a href="#tab=level&key=${esc(c.key)}" data-go="${esc(c.key)}">${esc(lvName(c.key))}</a></td><td>${BRU[c.board]||esc(c.board)}</td><td>${TRU[c.tray]||esc(c.tray)}</td><td class="n">${c.attempts}</td><td class="n">${c.video}</td><td class="n">${c.replay}</td><td class="small">${c.model==='exact'?'точная':c.model==='approximate'?'приближённая':'нет'} — ${esc(noteRu(c.modelNote))}</td><td class="small">${c.unknown.length?`<details><summary>${c.unknown.length}</summary><ul>${c.unknown.map(u=>`<li>${esc(u)}</li>`).join('')}</ul></details>`:'—'}</td></tr>`).join('')}</tbody></table></div>
  <p class="note">Данные собраны ${esc(D.date)} (${esc(D.built)}). Ещё не измеренные уровни появятся после следующего запуска сборки.</p>`;};

 // ---------- findings ----------
 VIEWS.findings=()=>DOCS.map(d=>`<section class="doc">${d.html?d.html:`<h2>${d.name==='conclusions.md'?'Выводы':'Неизвестные показатели'}</h2><p class="unk">Файл product/audits/level-stats/${esc(d.name)} ещё не написан.</p>`}</section>`).join('<hr>');

 // ---------- events ----------
 $('#filters').innerHTML=`<details class="filters" ${innerWidth>760?'open':''}><summary>Фильтры · показано <span class="count" id="fcount"></span> уровней</summary>${filterPanel()}</details>`;
 $('#filters').addEventListener('input',readFilters);$('#filters').addEventListener('change',e=>{if(e.target.type==='checkbox'||e.target.tagName==='SELECT')return;});
 $('#filters').addEventListener('click',e=>{if(e.target.id==='freset'){Object.assign(F,{games:new Set(GAMES),lvMin:null,lvMax:null,cMin:null,cMax:null,tMin:null,tMax:null,pMin:null,pMax:null,links:'any',hidden:'any',diffs:new Set(DIFFS),outcome:'any',source:'any'});$('#filters .fgrid').outerHTML=filterPanel();updateCount();render();}});
 $('nav.tabs').innerHTML=TABS.map(([k,t])=>`<button data-tab="${k}" role="tab">${t}</button>`).join('');
 $('nav.tabs').addEventListener('click',e=>{const b=e.target.closest('[data-tab]');if(!b)return;S.tab=b.dataset.tab;render();window.scrollTo({top:Math.min(scrollY,$('nav.tabs').offsetTop)});});
 const goLevel=k=>{S.key=k;S.meas=null;S.tab='level';render();window.scrollTo({top:0});};
 $('#view').addEventListener('click',e=>{const t=e.target;
  const sim=t.closest('[data-sim]');if(sim){e.preventDefault();S.simA=sim.dataset.sim;S.simB=null;render();return;}
  const pr=t.closest('[data-pair]');if(pr){e.preventDefault();[S.simA,S.simB]=pr.dataset.pair.split('|');render();return;}
  const g=t.closest('[data-go]');if(g){e.preventDefault();goLevel(g.dataset.go);return;}
  const th=t.closest('th[data-sort]');if(th){const k=th.dataset.sort;if(S.sort===k)S.dir=-S.dir;else{S.sort=k;S.dir=1;}render();return;}
  const nv=t.closest('[data-nav]');if(nv){const keys=rows().map(r=>r.key),list=keys.length?keys:ROWS.map(r=>r.key);let i=list.indexOf(S.key);i=(i+ +nv.dataset.nav+list.length)%list.length;S.key=list[i];S.meas=null;render();return;}
  if(t.id==='csv')csv();});
 $('#view').addEventListener('change',e=>{const t=e.target;if(t.id==='lvsel'){S.key=t.value;S.meas=null;render();return;}if(t.id==='meassel'){S.meas=+t.value;render();return;}
  if(t.dataset.s){const k=t.dataset.s;S[k]=t.type==='range'?+t.value:t.value;if(k==='simA')S.simB=null;render();}});
 $('#view').addEventListener('input',e=>{const t=e.target;if(t.type==='range'&&t.dataset.s){S[t.dataset.s]=+t.value;const y=scrollY;render();window.scrollTo({top:y});const r=$('#view input[type=range]');if(r)r.focus();}});
 window.addEventListener('hashchange',()=>{readHash();render();});
 let rt=null,lastW=innerWidth;window.addEventListener('resize',()=>{if(Math.abs(innerWidth-lastW)<40)return;lastW=innerWidth;clearTimeout(rt);rt=setTimeout(render,200);});
 readHash();updateCount();render();
}

const counts={};for(const r of data.rows)counts[r.game]=(counts[r.game]||0)+1;
const json=JSON.stringify(data).replace(/</g,'\\u003c').replace(/[\u2028\u2029]/g,c=>'\\u'+c.charCodeAt(0).toString(16));
const docsJson=JSON.stringify(docs).replace(/</g,'\\u003c').replace(/[\u2028\u2029]/g,c=>'\\u'+c.charCodeAt(0).toString(16));
const html=`<!doctype html>
<html lang="ru"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Статистика уровней</title>
<meta name="description" content="Статистика уровней Colony Flow, пилотов Bloomlings v8E и v9: поле, лоты, буфер, стратегии, время, сравнение.">
<style>${CSS}</style></head><body>
<header><h1>Статистика уровней: Colony Flow, пилот v8E, пилот v9</h1>
<p class="sub">Данные от ${esc(data.date)} · ${['CF','v8E','v9'].map(g=>`${g==='CF'?'Colony Flow':g}: ${counts[g]||0}`).join(' · ')} уровней. Работает без сети.</p>
<details><summary class="small">Как читать отчёт</summary><div class="small">
<p><b>Источник каждого числа</b> отмечен меткой: <span class="tag">раскладка</span> — поле и лоты по файлу уровня или расшифровке кадра; <span class="tag">запись</span> — увидено в видео; <span class="tag">повтор</span> — точный повтор записанных нажатий на движке v8E; <span class="tag">модель≈</span> — модель нашего движка (для Colony Flow приближённо: один вход под серединой поля, обход по краю; замки, коробки и шоколад не моделируются); <span class="tag">модель</span> — точная модель пилота; <span class="tag">стратегия</span> — прогон простой стратегии на модели.</p>
<p><b>«Неизвестно»</b> — данных нет или поиск не успел; это не ноль. Ноль пишется цифрой. <b>Полный буфер</b> — 0 свободных мест, <b>почти полный</b> — ровно 1; они не складываются. Среднее число свободных мест взвешено по длительности состояний.</p>
<p><b>Время</b>: модель — секунды при скорости ×1; Colony Flow — секунды видео (игра может идти ×2); v8E — часы игрока. <b>Отрезки</b> 0–20 % … 80–100 %: по доле очищенных исходных клеток (все три игры выигрываются полной очисткой) и отдельно по доле времени попытки.</p>
<p><b>v9</b> людьми не игрался: все его показатели — модель. <b>Капсула</b> = лот задания, <b>место</b> = слот буфера.</p></div></details>
<nav class="tabs" role="tablist"></nav></header>
<main><div id="filters"></div><div id="view"></div></main><div id="tip" role="tooltip"></div>
<script type="application/json" id="data">${json}</script>
<script type="application/json" id="docs">${docsJson}</script>
<script>"use strict";let VIEWS;(${client.toString()})();</script>
</body></html>`;
fs.mkdirSync(outDir,{recursive:true});fs.writeFileSync(outPath,html);
console.log(path.relative(root,outPath),(Buffer.byteLength(html)/1024/1024).toFixed(2)+' MB,',data.rows.length,'levels');

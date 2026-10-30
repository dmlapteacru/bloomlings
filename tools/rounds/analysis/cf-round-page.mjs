// A playable page of one Colony Flow round with winning tap patterns to follow (the owner, 2026-10-30: «сделай мне этот
// раунд в html … дай мне 10 паттернов … чтоб я смог смотреть и следовать им»). The round is the CF model of
// tools/level-stats (its tray of columns and rows as the storyboard has it) on the v8E pilot page (the engine the CF engine
// grew from) with Colony Flow's link rule put in (a linked capsule may also leave when its partner lies right under it in the
// same column, "4-4"; the rule tools/level-stats/engines.mjs gives the CF engine), its colours turned back to the CF ones.
// The patterns: the recorded win first, then evenly spread ones from cf-patterns.mjs (<key>-patterns.txt.gz) when the round
// has its full list, else wins found by random play (bots); each is checked in the CF engine with the player waiting for
// the board to settle between taps. A panel follows the
// page's tray (window.bloomlingsPilot.snapshot().visual.heads): the next tap of the chosen pattern, the taps done, a tap off
// the pattern.
// node tools/rounds/analysis/cf-round-page.mjs <CF-key> [--count 10] [--colours G:R,P:D,B:C,A:Y] [--title "…"]
//   [--total "…": the line on how many winning patterns the round has, when it has no full list] [--out <file>]
const ROOT=new URL('../../../',import.meta.url).pathname;
const fs=await import('node:fs'),path=await import('node:path'),zlib=await import('node:zlib'),{execSync}=await import('node:child_process');
const {allRecords,V8E_COMMIT}=await import(ROOT+'tools/level-stats/levels.mjs');const {engine}=await import(ROOT+'tools/level-stats/engines.mjs');
const arg=(k,d)=>{const i=process.argv.indexOf(k);return i>0?process.argv[i+1]:d;};
const key=process.argv[2],COUNT=+arg('--count',10),DIR=ROOT+'product/audits/colony-flow-patterns';
const rec=allRecords().find(r=>r.key===key);if(!rec||!rec.model)throw Error('no CF model '+key);
const map=Object.fromEntries(String(arg('--colours','')).split(',').filter(Boolean).map(x=>x.split(':')));
const recolour=c=>map[c]||c;
const level={...rec.model,rowsBottomFirst:rec.model.rowsBottomFirst.map(r=>[...r].map(recolour).join('')),pods:rec.model.pods.map(p=>({...p,color:recolour(p.color)}))};
const title=arg('--title',`Colony Flow L${rec.level}`);
// the recorded win (storyboard) in the pattern notation, then evenly spread patterns of the full list
const E=await engine('CF'),R=E.R,I=E.idle;
const colOf=p=>level.tray.findIndex(s=>s.includes(p))+1;
const lab=(s,pod)=>s.meta.groups[pod].map(colOf).sort((a,b)=>a-b).join('-');
function recorded(){const files=[ROOT+'product/audits/colony-flow-rounds/levels.json',ROOT+'product/audits/colony-flow-rounds/site/levels.json'];
 for(const f of files){const d=JSON.parse(fs.readFileSync(f,'utf8')),arr=Array.isArray(d.levels||d)?(d.levels||d):Object.values(d.levels||d),lv=arr.find(x=>x.level===rec.level);
  const r=lv&&(lv.rounds||lv.attempts||[]).find(r=>r.outcome==='won');if(!r)continue;let s=R.create(level,rec.capacity||5);const out=[];
  for(const t of r.taps){const c=+String(t.column).replace(/\D/g,'')-1,p=level.tray[c][s.heads[c]],g=s.meta.groups[p];out.push(lab(s,g[0]));s=I.step(s,{type:'tap',pod:g[0]}).state;}
  return out.join(' ');}return null;}
// the full list when cf-patterns.mjs could make it, else wins of random play (uniform over the taps on offer, waiting
// between taps), distinct and sorted so an even spread starts differently
const listFile=path.join(DIR,key+'-patterns.txt.gz'),full=fs.existsSync(listFile);
let all,randomShare=null;
if(full)all=zlib.gunzipSync(fs.readFileSync(listFile)).toString().trim().split('\n');
else{let seed=4242+rec.level*7919;const rnd=()=>{seed^=seed<<13;seed>>>=0;seed^=seed>>17;seed^=seed<<5;seed>>>=0;return seed/4294967296;};
 const wins=new Set();let games=0;while(wins.size<COUNT*40&&games<COUNT*4000){games++;let s=R.create(level,rec.capacity||5);const seq=[];
  while(s.status==='Playing'){const mv=R.legal(s).filter(m=>m.type==='tap');if(!mv.length)break;const m=mv[Math.floor(rnd()*mv.length)];seq.push(lab(s,m.pod));s=I.step(s,m).state;}
  if(s.status==='Won')wins.add(seq.join(' '));}
 all=[...wins].sort();randomShare=Math.round(wins.size/games*100);}
const rp=recorded(),picks=[];if(rp)picks.push({pattern:rp,note:'как в видео'});
// spread over the whole list (sorted by the first taps), so the picks start differently
const want=COUNT-picks.length;for(let i=0;i<want;i++){const p=all[Math.floor((i+0.5)*all.length/want)];if(!picks.some(x=>x.pattern===p))picks.push({pattern:p,note:null});}
// the spread above can run short of COUNT: fill evenly from the rest
for(let i=0;picks.length<COUNT&&i<all.length;i+=Math.max(1,Math.floor(all.length/COUNT/7)))if(!picks.some(x=>x.pattern===all[i]))picks.push({pattern:all[i],note:null});
// each pattern replayed: it must win with the player waiting between taps
function play(pattern){let s=R.create(level,rec.capacity||5);for(const tok of pattern.split(' ')){const c=+tok.split('-')[0]-1,p=level.tray[c][s.heads[c]];if(p==null)return 'no pod';const g=s.meta.groups[p];if(lab(s,g[0])!==tok)return 'not on top';const r=I.step(s,{type:'tap',pod:g[0]});if(!r.accepted)return r.reason;s=r.state;}return s.status;}
for(const x of picks){x.check=play(x.pattern);if(x.check!=='Won')throw Error(`pattern does not win (${x.check}): ${x.pattern}`);}
// the v8E page with this level only, Colony Flow's link rule, and the panel
let page0=execSync(`git show ${V8E_COMMIT}:product/previews/gameplay-pilot-v8e/index.html`,{cwd:ROOT,maxBuffer:1<<28}).toString();
{const old="if(group.some(p=>!heads.includes(p)))return 'partner-not-exposed';",
  rule="if(group.some(p=>!heads.includes(p)&&!state.level.tray.some((stack,c)=>{const at=stack.indexOf(p);return at>=state.heads[c]&&stack.slice(state.heads[c],at).every(q=>group.includes(q));})))return 'partner-not-exposed';";
 if(page0.split(old).length!==2)throw Error('link rule moved in the v8E page');page0=page0.replace(old,rule);}
const data=JSON.parse(/<script id="pilotData" type="application\/json">([\s\S]*?)<\/script>/.exec(page0)[1]);
data.levels=[{...level,id:1,title,difficulty:rec.difficulty}];
let html=page0.replace(/(<script id="pilotData" type="application\/json">)[\s\S]*?(<\/script>)/,(m,a,b)=>a+JSON.stringify(data).replace(/</g,'\\u003c')+b);
html=html.replace(/<title>[^<]*<\/title>/,`<title>${title} · паттерны</title>`);
const panel=`
<style>
#cfp{position:fixed;left:6px;right:6px;top:6px;z-index:9999;max-width:560px;margin:0 auto;background:#fffdf5;border:2px solid #6a3a1e;border-radius:12px;box-shadow:0 4px 16px rgba(0,0,0,.25);font:13px/1.3 system-ui,sans-serif;color:#2b2118}
#cfp .bar{display:flex;align-items:center;gap:8px;padding:5px 8px}
#cfp .next{flex:1;font-size:17px;font-weight:700;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}
#cfp .step{color:#6b5d4f;font-size:12px;white-space:nowrap}
#cfp .btn{border:1px solid #b8901b;background:#fff3c4;border-radius:8px;padding:3px 8px;font:600 12px system-ui;color:#2b2118;cursor:pointer}
#cfp .chips{display:flex;flex-wrap:wrap;gap:3px;padding:0 8px 6px}
#cfp .chip{min-width:20px;text-align:center;padding:1px 4px;border-radius:6px;background:#efe6d2;border:1px solid #d9c9a6;font-size:12px}
#cfp .chip.link{background:#e9dcf7;border-color:#a98bd0}
#cfp .chip.done{background:#cfe8c6;border-color:#8cbf7c;color:#3d6b30}
#cfp .chip.cur{background:#f6cb3e;border-color:#b8901b;font-weight:700}
#cfp .more{display:none;padding:6px 8px 8px;border-top:1px solid #e8dcc4;max-height:46vh;overflow:auto}
#cfp.open .more{display:block}
#cfp .list{display:grid;gap:4px;margin:6px 0}
#cfp .list button{display:flex;gap:6px;align-items:flex-start;text-align:left;padding:4px 6px;border-radius:8px;border:1px solid #d9c9a6;background:#fff;font:12px/1.3 system-ui,sans-serif;color:#2b2118;cursor:pointer}
#cfp .list button.sel{background:#fff3c4;border-color:#b8901b;box-shadow:inset 3px 0 0 #b8901b}
#cfp .list button.off{opacity:.38}
#cfp .list .num{font-weight:700;min-width:22px;padding-top:1px}
#cfp .list .row{display:flex;flex-wrap:wrap;gap:2px}
#cfp .list .chip{min-width:16px;padding:0 3px;font-size:11px}
#cfp .list .note{color:#6b5d4f;font-size:11px;padding-top:1px}
#cfp .warn{color:#b3261e}#cfp .ok{color:#2e7d32}
#cfp small{color:#6b5d4f;display:block;margin-top:4px}
</style>
<div id="cfp"><div class="bar"><span id="cfpNext" class="next"></span><span id="cfpStep" class="step"></span><button id="cfpMore" class="btn">Паттерны ▾</button></div>
<div id="cfpChips" class="chips"></div>
<div class="more"><div id="cfpList" class="list"></div>
<small>По мере тапов в каждом паттерне, который начинается ровно с твоих тапов, пройденные шаги зелёные, следующий — жёлтый; паттерны, от которых ты отошёл, бледнеют. Подходящие паттерны всегда сверху списка. Нажми паттерн, чтобы вести его в строке сверху.</small>
<small>Цифра — колонка слева направо (номера исходные, даже когда колонка опустела). «2-3» — связка: нажми любую из двух капсул. Перед следующим тапом дождись, пока поле успокоится (муравьи вернулись) — так паттерн выигрывает всегда. Начать заново — кнопка игры «Заново».</small>
<small>${full?`Всего выигрышных паттернов у раунда: ${all.length.toLocaleString('ru-RU')} (при спокойной игре выигрывает любой порядок). Здесь ${picks.length}.`:`${arg('--total','')} Выигрывает не любой порядок: случайные тапы выигрывают около ${randomShare} %. Здесь ${picks.length} проверенных побед.`}</small></div></div>
<script>
(function(){const P=${JSON.stringify(picks.map(x=>({p:x.pattern.split(' '),note:x.note})))},COLS=${level.tray.length};
let sel=0,last='';const $=id=>document.getElementById(id);
const exp=P.map(x=>{const h=[Array(COLS).fill(0)];for(const t of x.p){const n=h[h.length-1].slice();for(const c of t.split('-'))n[+c-1]++;h.push(n);}return h;});
// the taps made so far, in order, read from the page's column heads: each change is one tap ("2", a link "2-3", a pair
// stacked in one column "2-2"); a restart (all heads back to 0) clears it
let hist=[],prev=Array(COLS).fill(0),prevCmd=0;
function track(h,cmd){if(h.every(v=>v===0)){hist=[];prev=h.slice();prevCmd=cmd;return;}
 const inc=[];h.forEach((v,c)=>{for(let k=prev[c];k<v;k++)inc.push(c+1);});
 if(inc.length){const taps=Math.max(1,cmd-prevCmd);if(taps===1)hist.push(inc.sort((a,b)=>a-b).join('-'));else for(const c of inc)hist.push(String(c));}
 prev=h.slice();prevCmd=cmd;}
// the step each pattern has reached: its first taps equal the taps made (-1: the taps left it)
const stepOf=i=>{const p=P[i].p;if(hist.length>p.length)return -1;for(let j=0;j<hist.length;j++)if(p[j]!==hist[j])return -1;return hist.length;};
const chips=(p,k)=>p.map((t,i)=>'<span class="chip'+(t.includes('-')?' link':'')+(k>=0&&i<k?' done':'')+(k>=0&&i===k?' cur':'')+'">'+t+'</span>').join('');
function render(h,st){const ks=P.map((x,i)=>stepOf(i));
 // the patterns that still fit come first (their numbers stay)
 const order=P.map((x,i)=>i).sort((a,b)=>(ks[a]<0)-(ks[b]<0)||a-b);
 $('cfpList').innerHTML=order.map(i=>{const x=P[i];return '<button data-i="'+i+'" class="'+(i===sel?'sel':'')+(ks[i]<0?' off':'')+'"><span class="num">'+(i+1)+'.</span><span><span class="row">'+chips(x.p,ks[i])+'</span>'+(x.note?'<span class="note">'+x.note+'</span>':'')+'</span></button>';}).join('');
 for(const b of $('cfpList').querySelectorAll('button'))b.onclick=()=>{sel=+b.dataset.i;last='';tick();};
 const x=P[sel],k=ks[sel];
 $('cfpChips').innerHTML=chips(x.p,k);
 $('cfpStep').textContent='№'+(sel+1)+' · '+(k<0?'?':Math.min(k+1,x.p.length))+'/'+x.p.length;
 if(st==='Won'){$('cfpNext').innerHTML='<span class="ok">Победа</span>';return;}
 if(st&&st!=='Playing'){$('cfpNext').innerHTML='<span class="warn">Проигрыш — «Заново», спокойнее</span>';return;}
 if(k<0){const fit=ks.map((v,i)=>v>=0?i+1:0).filter(Boolean);$('cfpNext').innerHTML='<span class="warn">Не по №'+(sel+1)+(fit.length?' — подходят №'+fit.join(', '):' — «Заново»')+'</span>';return;}
 const t=x.p[k];$('cfpNext').textContent=k>=x.p.length?'Все тапы сделаны':(t.includes('-')?'Связка: '+t.replace('-',' и '):'Колонка '+t);}
$('cfpMore').onclick=()=>{const o=$('cfp').classList.toggle('open');$('cfpMore').textContent=o?'Скрыть ▴':'Паттерны ▾';};
function tick(){let snap=null;try{snap=window.bloomlingsPilot&&window.bloomlingsPilot.snapshot();}catch(e){}
 const h=snap?snap.visual.heads:Array(COLS).fill(0),st=snap?snap.status:'',cmd=snap?snap.commands:0,sig=h.join(',')+'|'+st+'|'+sel;
 if(sig===last)return;track(h,cmd);last=sig;render(h,st);}
tick();setInterval(tick,60);})();
</script>`;
html=html.replace(/<\/body>/,panel+'\n</body>');
const out=arg('--out',path.join(DIR,`L${rec.level}.html`));fs.writeFileSync(out,html);
fs.writeFileSync(out.replace(/\.html$/,'.patterns.txt'),picks.map((x,i)=>`${i+1}. ${x.pattern}${x.note?'   ('+x.note+')':''}`).join('\n')+'\n');
console.log(out,html.length,'bytes;',picks.length,'patterns, all win:',picks.every(x=>x.check==='Won'));

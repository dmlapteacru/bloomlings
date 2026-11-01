// The comparison of the route-first prototype with the current builder on the same pictures, classes and recipes
// (REPORT.md). Works on the private copy content/work/proto-route/rounds-copy; outputs in content/work/proto-route/.
//
//   node tools/rounds/proto/compare.mjs proto <job…>   route-first on these jobs (indices or labels), one after another
//   node tools/rounds/proto/compare.mjs old <job…>     the current builder (levels.mjs build, a fresh search tagged cmp)
//   node tools/rounds/proto/compare.mjs report         the table (out/table.md, out/table.json) and the preview levels
//        (preview/L1001… : odd = the level as built today, even = the route-first level on the same picture)
import fs from 'node:fs';import path from 'node:path';import {spawnSync} from 'node:child_process';
const HERE=path.dirname(new URL(import.meta.url).pathname),ROOT=path.resolve(HERE,'../../..'),WORK=path.join(ROOT,'content/work/proto-route');
const COPY=path.join(WORK,'rounds-copy');process.env.ROUNDS_DIR=process.env.ROUNDS_DIR||COPY;
const [cmd,...rest]=process.argv.slice(2);
const RF=await import('./route-first.mjs');const L=await import('../lib.mjs');
// 8 placed non-bridge levels of L21–L50 (+ L20) with their class and recipe, and two wave-51 recipes on free pictures
// (L54 links+cells with accents as the plan asks; L66 links+latch, the first Normal after the Hard L65)
export const JOBS=[
 {label:'L0020',level:20,pic:'P0078',cls:'Hard',recipe:'links+hidden',slot:'hard'},
 {label:'L0025',level:25,pic:'P0166',cls:'Hard',recipe:'links+hidden',slot:'hard'},
 {label:'L0040',level:40,pic:'P0165',cls:'Hard',recipe:'links+hidden',slot:'hard'},
 {label:'L0022',level:22,pic:'P0015',cls:'Normal',recipe:'none',slot:'regular'},
 {label:'L0027',level:27,pic:'P0060',cls:'Normal',recipe:'links+hidden',slot:'regular'},
 {label:'L0032',level:32,pic:'P0012',cls:'Normal',recipe:'links+hidden',slot:'regular'},
 {label:'L0038',level:38,pic:'P0160',cls:'Normal',recipe:'links+hidden',slot:'regular'},
 {label:'L0044',level:44,pic:'P0164',cls:'Normal',recipe:'links+hidden',slot:'regular'},
 {label:'L0054',level:54,pic:'P0210',cls:'Normal',recipe:'links+cells',slot:'regular',accents:'on',fresh:true},
 {label:'L0066',level:66,pic:'P0317',cls:'Normal',recipe:'links+latch',slot:'after-hard',fresh:true},
];
const pick=a=>a.length?a.map(x=>JOBS.find((j,i)=>String(i)===x||j.label===x)).filter(Boolean):JOBS;
const MIN=+(process.env.PROTO_MINUTES||20),WANT=+(process.env.PROTO_WANT||2);
const out=f=>path.join(WORK,'out',f);const save=(f,o)=>{fs.mkdirSync(path.dirname(out(f)),{recursive:true});fs.writeFileSync(out(f),JSON.stringify(o,null,1));};

if(cmd==='proto'){for(const j of pick(rest)){console.log('==',j.label,j.pic,j.cls,j.recipe);
  const res=await RF.routeFirst(j.pic,j.cls,j.recipe,{slot:j.slot,accentMode:j.accents||'auto',want:WANT,minutes:MIN,seed:1,log:s=>console.log(j.label,s)});
  save(`${j.label}-proto.json`,{job:j,seconds:res.seconds,stats:res.stats,shares:res.shares,error:res.error||null,accepted:res.accepted.map(x=>({...RF.asPlaced(x,j.level,j.cls,j.pic),at:x.at})),passedOut:(res.passedOut||[]).map(x=>({...RF.asPlaced(x,j.level,j.cls,j.pic),at:x.at}))});
  console.log(j.label,'итог',Math.round(res.seconds),'с',JSON.stringify(res.stats));}}

else if(cmd==='old'){for(const j of pick(rest)){const bf=path.join(COPY,'bank',j.pic+'.json'),before=new Set((L.readJSON(bf,{candidates:[]}).candidates||[]).map(c=>c.id));
  const args=[path.join(ROOT,'tools/rounds/levels.mjs'),'build',j.pic,'--classes',j.cls,'--recipes',j.recipe,'--no-intro','--accents',j.accents||'auto'];
  console.log('==',j.label,'node',args.slice(1).join(' '));const t0=Date.now();
  const r=spawnSync(process.execPath,args,{env:{...process.env,ROUNDS_DIR:COPY,ROUNDS_FRESH:'cmp',ROUNDS_TRACE:'1'},encoding:'utf8',maxBuffer:1<<28});const seconds=(Date.now()-t0)/1000;
  // per-candidate times from the trace: the elapsed seconds at each candidate's last line
  const trace=(r.stderr||'').split('\n').filter(Boolean),last={};for(const l of trace){const m=/#(\d+).* ([\d.]+) s$/.exec(l);if(m)last[+m[1]]=+m[2];}
  const bank=L.readJSON(bf,{candidates:[]}),fresh=bank.candidates.filter(c=>!before.has(c.id)&&/-cmp/.test(c.id)),pic=L.picture(j.pic);
  const rows=fresh.map(c=>{const why=RF.placerFilters({random:c.random,checks:c.checks},c.level,j.cls,j.slot==='hard'?'hard':j.slot,pic);return {id:c.id,attempt:c.attempt,share:c.random.share,cls:c.cls,why,zone:RF.inZone(c.random.share,j.cls,j.slot)};});
  const searched=(bank.searched||[]).filter(s=>s.tag==='cmp').at(-1)||null;
  save(`${j.label}-old.json`,{job:j,seconds,stdout:r.stdout,searched,perCandidate:last,candidates:rows,passed:rows.filter(x=>!x.why.length).length,accepted:rows.filter(x=>!x.why.length&&x.zone).length,levels:fresh.map(c=>c.id)});
  console.log(j.label,'текущий сборщик',Math.round(seconds),'с',(r.stdout||'').trim().split('\n').at(-1),'проходят',rows.filter(x=>!x.why.length).length,'в зоне',rows.filter(x=>!x.why.length&&x.zone).length);}}

else if(cmd==='report'){const table=[],prev=path.join(WORK,'preview');fs.mkdirSync(prev,{recursive:true});let n=1001;
 const row=(src,L0)=>{if(!L0)return null;const k=L0.checks||{},st=L0.strategies||{};const won=Object.entries(st).filter(([,v])=>v).map(([x])=>x);
  return {src,cls:L0.cls,share:L0.random.share,wilson:L0.random.wilson95,runs:L0.random.runs,P:L0.P,cols:L0.columns,linked:L0.linked,traps:k.traps?`${k.traps.traps}/${k.traps.links}`:'—',hidden:L0.hidden,cells:L0.cells??null,latches:L0.latches??null,
   stuck:k.pressure?k.pressure.stuck:null,forced:k.forced,opening:k.opening?`${k.opening.first.wins}/${k.opening.first.openings}, ${Math.round(k.opening.three.share*100)} %`:'—',calm:k.calm?k.calm.share:null,hold:k.hold??null,
   strategies:`${won.length}/${Object.keys(st).length}`,greedyLoses:!st['most-now|left']&&!st['most-now|right'],fast:L0.solution&&L0.solution.fast?`${L0.solution.fast.seconds} с ${L0.solution.fast.status}`:'—',seconds:L0.rhythm?L0.rhythm.seconds:null,accents:k.accents?k.accents.cells:0};};
 for(const j of JOBS){const placed=L.readJSON(path.join(ROOT,'content/rounds/levels',j.label+'.json')),old=L.readJSON(out(`${j.label}-old.json`)),pro=L.readJSON(out(`${j.label}-proto.json`));
  // the level as built today: the placed one; for the wave-51 positions (not placed yet) the current builder's best
  // candidate of this comparison run (the placer's preference: in zone, then nearest the aim)
  let today=placed;if(!today&&old){const bank=L.readJSON(path.join(COPY,'bank',j.pic+'.json'),{candidates:[]}),ok=old.candidates.filter(c=>!c.why.length).sort((a,b)=>(b.zone-a.zone)||Math.abs(a.share-.6)-Math.abs(b.share-.6))[0];
   const c=ok&&bank.candidates.find(x=>x.id===ok.id);if(c)today={level:j.level,cls:c.cls,intro:null,picture:c.picture,candidate:c.id,recipe:c.recipe,N:c.N,W:c.W,colours:c.colours,P:c.P,columns:c.columns,linked:c.linked,hidden:c.hidden,...(c.cells!=null?{cells:c.cells}:{}),...(c.latches!=null?{latches:c.latches}:{}),random:c.random,solution:c.solution,fairness:c.fairness,strategies:c.strategies,rhythm:c.rhythm,checks:c.checks,engine:c.engine,definition:{...c.level,id:j.level,difficulty:c.cls}};}
  const best=pro&&(pro.accepted[0]||null);
  const oldT=old?{seconds:Math.round(old.seconds),tried:old.searched?old.searched.tried:null,found:old.searched?old.searched.found:null,passed:old.passed,accepted:old.accepted,perAccepted:old.accepted?Math.round(old.seconds/old.accepted):null}:null;
  const proT=pro?{seconds:Math.round(pro.seconds),bases:pro.stats.bases,evaluated:pro.stats.evaluated,passed:pro.stats.passed||0,accepted:pro.accepted.length,first:pro.accepted[0]?Math.round(pro.accepted[0].at):null,perAccepted:pro.accepted.length?Math.round(pro.seconds/pro.accepted.length):null,reasons:pro.stats.reasons}:null;
  table.push({job:j,today:row(placed?'размещён':'текущий сборщик',today),proto:row('route-first',best),pattern:best?best.pattern:null,old:oldT,pro:proT});
  // preview levels for the pilot: the level as built today, then the route-first one
  const put=(L0,tag)=>{if(!L0)return;const lv={...L0,level:n,candidate:(L0.candidate||'')+tag,definition:{...L0.definition,id:n,title:`${L0.definition.title} · ${j.label} ${tag}`}};fs.writeFileSync(path.join(prev,`L${n}.json`),JSON.stringify(lv));};
  put(today,'сегодня');n++;put(best?{...best,level:n}:null,'route-first');n++;}
 save('table.json',table);
 const f=x=>x==null?'—':typeof x==='number'?String(Math.round(x*100)/100):String(x),pct=x=>x==null?'—':Math.round(x*100)+' %';
 const lines=['| Уровень | | Класс | Случайный (Wilson) | Поды/кол. | Связки (ловушки) | Скрытые | Стоят без работы | Вынужд. | Дебют | Спокойно | Стратегии выигр. | Решение (быстрые тапы) | Время на уровень |','|---|---|---|---|---|---|---|---|---|---|---|---|---|---|'];
 for(const t of table){for(const [k,r] of [['today',t.today],['proto',t.proto]]){if(!r){lines.push(`| ${t.job.label} ${t.job.pic} | ${k==='today'?'сегодня':'route-first'} | — | нет уровня | | | | | | | | | | ${k==='today'?(t.old?`сборщик ${t.old.seconds} с, в зоне 0`:'—'):(t.pro?`${t.pro.seconds} с, принято 0`:'—')} |`);continue;}
   const time=k==='today'?(t.old?`сборщик: ${t.old.seconds} с, ${t.old.tried} канд., проходят ${t.old.passed}, в зоне ${t.old.accepted}${t.old.perAccepted?`, ${t.old.perAccepted} с/ур.`:''}`:'—'):(t.pro?`${t.pro.seconds} с, ${t.pro.bases} основ, ${t.pro.evaluated} полных, в зоне ${t.pro.accepted}${t.pro.first?`, первый ${t.pro.first} с`:''}`:'—');
   const hid=[r.hidden?`${r.hidden} пода`:null,r.cells?`${r.cells} клеток`:null,r.latches?`засов ${r.latches}`:null,r.accents?`акценты ${r.accents}`:null].filter(Boolean).join(', ')||'—';
   lines.push(`| ${t.job.label} ${t.job.pic} | ${k==='today'?r.src:'route-first'} | ${r.cls} | ${pct(r.share)} (${pct(r.wilson[0])}–${pct(r.wilson[1])}, ${r.runs}) | ${r.P}/${r.cols} | ${r.linked} (${r.traps}) | ${hid} | ${f(r.stuck)} | ${pct(r.forced)} | ${r.opening} | ${pct(r.calm)} | ${r.strategies}${r.greedyLoses?', жадная проигр.':''} | ${r.fast} | ${time} |`);}}
 fs.writeFileSync(out('table.md'),lines.join('\n')+'\n');console.log(lines.join('\n'));}
else console.log(fs.readFileSync(new URL(import.meta.url),'utf8').split('\n').filter(l=>l.startsWith('//')).join('\n'));

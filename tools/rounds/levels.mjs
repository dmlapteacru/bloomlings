// Round levels from approved pictures (skill bloomlings-round-levels; rules: product/17_LEVEL_GENERATION_RULES.md
// §1, §3–§8, §10–§11, §13–§14).
//
// node tools/rounds/levels.mjs calendar <from> <to>        class, mechanic introduction and allowed mechanics per level
// node tools/rounds/levels.mjs build <pictureId…|--approved> [--classes Normal,Hard,"Super Hard"] [--candidates 64]
//      [--recipes none,links,hidden,links+hidden,bridge] [--seed 1]
//      candidates for each picture, recipe and class: balance → queue → saved solution → random test → bank
// node tools/rounds/levels.mjs place <from> <to>           fill calendar positions from the bank (variety rules),
//      write levels and cards, list what is still missing in content/rounds/needs.json
// node tools/rounds/levels.mjs bank                        the bank: candidates by picture and measured class
// node tools/rounds/levels.mjs card <level>                the level card (§14)
import fs from 'node:fs';import path from 'node:path';
import {ROUNDS,PICTURES,LEVELS,BANK,CODES,manifest,saveManifest,picture,readJSON,writeJSON,lid,pid,arg,flag,measure,stageOf,fitsStage,sizeBin,budget,columnsFor,classOf,slotOf,INTRO,MECH_RU,SUPPORTED,boardOf} from './lib.mjs';
import {randomTest,quickShare,solve,fairness,strategies,rhythm} from './measure.mjs';
const cmd=process.argv[2];const CLASSES=['Normal','Hard','Super Hard'];
const rnd=seed=>{let s=seed>>>0||1;return()=>{s^=s<<13;s^=s>>>17;s^=s<<5;return(s>>>0)/4294967296;};};

// ---- §3: pods of each material ----
const FIXED={1:{split:[[26,26,26,25,25],[16,16,16],[5]]},2:{split:[[40,40,40],[33,32,32],[10,9],[4]]},3:{split:[[26,25,25],[24,23,23],[35,34],[26,25],[13],[2]]}};
const even=(v,k)=>Array.from({length:k},(_,i)=>Math.floor(v/k)+(i<v%k?1:0));
export function splitPods(volumes,W,level){
 if(FIXED[level]){const vs=Object.entries(volumes).sort((a,b)=>b[1]-a[1]),fx=FIXED[level].split.slice().sort((a,b)=>b.reduce((x,y)=>x+y)-a.reduce((x,y)=>x+y));
  if(vs.length!==fx.length||vs.some(([,v],i)=>fx[i].reduce((x,y)=>x+y)!==v))return {error:`объёмы цветов не совпадают с §4 для уровня ${level}`};
  return {pods:vs.flatMap(([c],i)=>fx[i].map(n=>({color:c,count:n})))};}
 const k={};for(const [c,v] of Object.entries(volumes)){if(v<20)k[c]=1;else{const max=Math.min(60,Math.ceil(v/2));k[c]=Math.max(2,Math.ceil(v/max),v>=60?3:0);}}
 const [lo,hi]=budget(W),P=()=>Object.values(k).reduce((a,b)=>a+b,0);
 // more pods inside the budget: the colour with the largest average pod first, never below ~8 per pod (§3)
 while(P()<lo){const c=Object.keys(k).filter(c=>volumes[c]/(k[c]+1)>=8&&!(volumes[c]<20&&k[c]>=2)).sort((a,b)=>volumes[b]/k[b]-volumes[a]/k[a])[0];if(!c)break;k[c]++;}
 if(P()>hi)return {error:`минимум подов ${P()} больше бюджета ${lo}–${hi} по W=${W}`};
 if(P()<lo)return {error:`подов ${P()} меньше бюджета ${lo}–${hi}: цвета слишком мелкие для этой площади`};
 return {pods:Object.entries(k).flatMap(([c,n])=>even(volumes[c],n).map(x=>({color:c,count:x})))};}
export function checkPods(pods,volumes){const errs=[];const sum={};for(const p of pods){sum[p.color]=(sum[p.color]||0)+p.count;if(!(Number.isInteger(p.count)&&p.count>=1&&p.count<=60))errs.push(`под ${p.color}${p.count} вне 1–60`);}
 for(const [c,v] of Object.entries(volumes)){if(sum[c]!==v)errs.push(`баланс ${c}: поды ${sum[c]||0} ≠ работа ${v}`);const ps=pods.filter(p=>p.color===c);
  if(v>=20){if(ps.length<2)errs.push(`${c}: объём ${v} ≥ 20 требует минимум 2 пода`);const max=Math.min(60,Math.ceil(v/2));if(ps.some(p=>p.count>max))errs.push(`${c}: под больше ${max}`);}
  else if(ps.length>2)errs.push(`${c}: объём ${v} < 20 — не больше 2 подов`);}
 return errs;}

// ---- access depth of each cell: how many coloured cells stand between it and the entry (0-1 search) ----
function depths(board){const {w,h,rowsBottomFirst:rows,entries}=board,g=rows.join(''),d=new Float64Array(w*h).fill(Infinity),dq=[];
 for(const e of entries){d[e]=0;dq.push(e);}
 while(dq.length){const c=dq.shift(),x=c%w,y=Math.floor(c/w);for(const [dx,dy] of [[1,0],[-1,0],[0,1],[0,-1]]){const u=x+dx,v=y+dy;if(u<0||v<0||u>=w||v>=h)continue;const n=v*w+u,ch=g[n];if(ch==='#')continue;
  const cost=CODES.includes(g[c])?1:0,nd=d[c]+cost;if(nd<d[n]){d[n]=nd;cost?dq.push(n):dq.unshift(n);}}}
 return {d,g};}

// ---- doses (§7, §9): ranges by class and role; a small queue (< 16 pods) has its own table ----
function doseRange(m,cls,role,P){const i=CLASSES.indexOf(cls);
 if(P<16)return m==='hidden'?[[1,2],[2,3],[3,4]][i]:m==='links'?[[2,2],[4,4],[4,6]][i]:[1,1];
 const T={links:{lead:[[.1,.3],[.2,.45],[.35,.6]],add:[[.05,.15],[.1,.2],[.15,.25]]},hidden:{lead:[[.05,.15],[.1,.25],[.15,.35]],add:[[.05,.1],[.05,.1],[.05,.15]]}};
 if(!T[m])return [1,1];const [a,b]=T[m][role][i];return [Math.ceil(a*P),Math.floor(b*P)];}
// recipe → concrete doses: one type uses its full range, two types: the additional at its minimum, three: the lead in
// the lower half (§7); the introduction levels use the minimum teaching doses (§9)
function doses(recipe,cls,P,intro){const types=recipe==='none'?[]:recipe.split('+'),out={};
 types.forEach((m,j)=>{if(m==='bridge'){out.bridge=1;return;}const role=j===0?'lead':'add';let [a,b]=doseRange(m,cls,role,P);
  if(intro){a=b=m==='links'?2:1;}else if(j>0){b=a;}else if(types.length>=3){b=Math.floor((a+b)/2);}
  if(m==='links'){a+=a%2;b-=b%2;}if(a>b)out.error=`нет целых доз ${m} для ${cls} при P=${P}`;out[m]=[a,b];});
 return out;}
export function dosesOk(level,cls){const P=level.pods.length,hidden=level.pods.filter(p=>p.mystery).length,linked=level.pods.filter(p=>p.group).length,types=(level.recipe||'none')==='none'?[]:level.recipe.split('+');
 const d=doses(level.recipe||'none',cls,P,false);if(d.error)return d.error;
 if(types.includes('links')&&(linked<d.links[0]||linked>Math.max(d.links[1],d.links[0])))return `связанных ${linked} вне ${d.links.join('–')} для ${cls}`;
 if(types.includes('hidden')&&(hidden<d.hidden[0]||hidden>Math.max(d.hidden[1],d.hidden[0])))return `скрытых ${hidden} вне ${d.hidden.join('–')} для ${cls}`;
 const max=cls==='Normal'?2:3;if(types.length>max)return `типов ${types.length} больше ${max} для ${cls}`;if(P<16&&types.length>1)return 'очередь меньше 16 подов — только один тип';return null;}

// ---- one candidate: order pods by access depth with noise, deal them into columns, add the recipe's mechanics ----
function candidate(pic,board,base,cols,recipe,cls,seed,intro){const r=rnd(seed),{d,g}=depths(board),pods=base.map(p=>({...p}));
 const byColour={};for(let i=0;i<g.length;i++)if(CODES.includes(g[i]))(byColour[g[i]]=byColour[g[i]]||[]).push(d[i]);
 const maxD=Math.max(1,...Object.values(byColour).flat());
 for(const [c,ds] of Object.entries(byColour)){ds.sort((a,b)=>a-b);const ps=pods.filter(p=>p.color===c);let k=0;for(const p of ps){const slice=ds.slice(k,k+p.count);k+=p.count;p.key=slice.reduce((a,b)=>a+b,0)/slice.length/maxD;}}
 const sigma={Normal:.06,Hard:.16,'Super Hard':.26}[cls];
 // harder classes push some early-needed colour deeper (§6.1) and split returns to a colour (§6.2)
 const deep=cls==='Normal'?null:Object.keys(byColour)[Math.floor(r()*Object.keys(byColour).length)];
 for(const p of pods){const z=Math.sqrt(-2*Math.log(r()+1e-9))*Math.cos(2*Math.PI*r());p.order=p.key+sigma*z+(p.color===deep&&r()<.5?.25:0);}
 const ord=pods.map((p,i)=>i).sort((a,b)=>pods[a].order-pods[b].order),tray=Array.from({length:cols},()=>[]);
 for(let i=0;i<ord.length;i+=cols){const row=ord.slice(i,i+cols),cs=[...Array(cols).keys()].sort(()=>r()-.5).sort((a,b)=>tray[a].length-tray[b].length);row.forEach((p,j)=>tray[cs[j]].push(p));}
 const types=recipe==='none'?[]:recipe.split('+'),dz=doses(recipe,cls,pods.length,intro);if(dz.error)return {error:dz.error};
 if(types.includes('links')){const want=dz.links[0]+2*Math.floor(r()*((dz.links[1]-dz.links[0])/2+1)),pairs=[];const used=new Set();
  // pairs of neighbouring columns at the same depth; harder classes prefer a useful-now pod with a later one (§6.3)
  const opts=[];for(let c=0;c+1<cols;c++)for(let row=0;row<Math.min(tray[c].length,tray[c+1].length);row++)opts.push([tray[c][row],tray[c+1][row],row]);
  opts.sort((x,y)=>(cls==='Normal'?Math.abs(pods[x[0]].key-pods[x[1]].key)-Math.abs(pods[y[0]].key-pods[y[1]].key):Math.abs(pods[y[0]].key-pods[y[1]].key)-Math.abs(pods[x[0]].key-pods[x[1]].key))+(r()-.5)*.2);
  for(const [a,b,row] of opts){if(pairs.length*2>=want)break;if(used.has(a)||used.has(b)||(intro&&row<1))continue;used.add(a);used.add(b);pairs.push([a,b]);}
  if(pairs.length*2<want)return {error:'не хватило пар соседних подов для дозы связок'};pairs.forEach(([a,b],i)=>{const gname=String.fromCharCode(65+i%26)+(i>=26?String(Math.floor(i/26)):'');pods[a].group=gname;pods[b].group=gname;});}
 if(types.includes('hidden')){const want=dz.hidden[0]+Math.floor(r()*(dz.hidden[1]-dz.hidden[0]+1));
  // from the second row down, never in a group (the engine reveals a pod on reaching the top only)
  const opts=tray.flatMap(st=>st.map((p,row)=>[p,row])).filter(([p,row])=>row>=1&&!pods[p].group).sort(()=>r()-.5);
  if(opts.length<want)return {error:'не хватило подов для скрытых'};opts.slice(0,want).forEach(([p])=>pods[p].mystery='depth');}
 if(types.includes('bridge')&&!board.hero)return {error:'на картинке нет мест моста (b)'};
 const level={id:1,title:pic.title,subject:pic.id,w:board.w,h:board.h,rowsBottomFirst:board.rowsBottomFirst,entries:board.entries,capacity:5,pods:pods.map(({key,order,...p})=>p),tray,recipe,...(types.includes('bridge')?{hero:board.hero}:{})};
 return {level};}

// ---- build: candidates for each picture, recipe and class, measured and banked ----
async function build(ids){const classes=(arg('--classes','Normal,Hard,Super Hard')).split(','),budgetN=+arg('--candidates',64),seed0=+arg('--seed',1);
 for(const id of ids){const pic=picture(id);if(!pic){console.log(id,'нет картинки');continue;}if(pic.status!=='approved'){console.log(id,'не утверждена — пропуск');continue;}
  const m=measure(pic),board=boardOf(pic),hasBridge=!!board.hero,W=m.N;
  const fixed=pic.forLevel<=3?pic.forLevel:null,base=splitPods(m.volumes,W,fixed);const bankFile=path.join(BANK,id+'.json'),bank=readJSON(bankFile,{picture:id,candidates:[],rejected:[]});
  if(base.error){bank.rejected.push({reason:base.error});writeJSON(bankFile,bank);console.log(id,'отклонена:',base.error);continue;}
  const P=base.pods.length,recipes=(arg('--recipes')||(fixed?'none':['none','links','hidden','links+hidden',...(hasBridge?['bridge','bridge+links']:[])].join(','))).split(',').filter(x=>x.split('+').every(t=>t==='none'||SUPPORTED.has(t)));
  for(const cls of fixed?['Normal']:classes)for(const recipe of recipes){if(fixed&&recipe!=='none')continue;
   for(const intro of recipe!=='none'&&!recipe.includes('+')&&cls==='Normal'?[false,true]:[false]){
    let tried=0,found=0;const reasons={};
    for(let k=0;k<budgetN&&found<2;k++){tried++;const cols=fixed?3:columnsFor(m.N)[k%columnsFor(m.N).length];
     const c=candidate(pic,board,base.pods,cols,recipe,cls,seed0*1000003+k*7919+CLASSES.indexOf(cls)*31+recipes.indexOf(recipe)*131+(intro?17:0),intro);
     if(c.error){reasons[c.error]=(reasons[c.error]||0)+1;continue;}
     const errs=checkPods(c.level.pods,m.volumes);if(errs.length){reasons['баланс']=(reasons['баланс']||0)+1;continue;}
     const q=await quickShare(c.level,40);const target=intro?[.9,1]:fixed===1?[1,1]:fixed?[.9,1]:{Normal:[.5,1],Hard:[.2,.5],'Super Hard':[.05,.2]}[cls];
     if(q<target[0]-.15||q>target[1]+.15){reasons['не тот класс по быстрой оценке']=(reasons['не тот класс по быстрой оценке']||0)+1;continue;}
     const sol=await solve(c.level);if(sol.win!==true||sol.replay!=='won'){reasons[sol.win===null?'решение не найдено за бюджет (не проверено)':'нет решения']=(reasons[sol.win===null?'решение не найдено за бюджет (не проверено)':'нет решения']||0)+1;continue;}
     const rt=await randomTest(c.level);if(rt.cls==='too hard'){reasons['ниже 5 %']=(reasons['ниже 5 %']||0)+1;continue;}
     if(intro&&rt.share<.9){reasons['знакомство ниже 90 %']=(reasons['знакомство ниже 90 %']||0)+1;continue;}
     const dErr=dosesOk(c.level,rt.cls);if(dErr&&!intro){reasons['дозы не подходят фактическому классу: '+dErr]=(reasons['дозы не подходят фактическому классу: '+dErr]||0)+1;continue;}
     const fair=c.level.pods.some(p=>p.mystery)?await fairness(c.level,sol.route):{fair:true,checked:0};if(!fair.fair){reasons['честность скрытых не подтверждена']=(reasons['честность скрытых не подтверждена']||0)+1;continue;}
     const st=await strategies(c.level),rh=rhythm(sol.trace,W);
     bank.candidates.push({id:`${id}-${cls[0]}${recipe}-${k}${intro?'-intro':''}`,picture:id,requested:cls,cls:rt.cls,intro,recipe,level:c.level,N:m.N,W,colours:m.colours,P:c.level.pods.length,columns:cols,
      linked:c.level.pods.filter(p=>p.group).length,hidden:c.level.pods.filter(p=>p.mystery).length,random:rt,solution:{route:sol.route,forks:sol.decisions.forks,singleSafe:sol.decisions.single,unknown:sol.decisions.unknown,fast:sol.fast},fairness:fair,strategies:st,rhythm:rh,engine:'tools/rounds/engine.mjs (pilot v9 + §8 link rule)',seed:seed0,attempt:k});
     found++;}
    console.log(id,cls.padEnd(10),recipe.padEnd(13),intro?'знакомство':'',`найдено ${found} из ${tried}`,Object.keys(reasons).length?JSON.stringify(reasons):'');}}
  writeJSON(bankFile,bank);}}

// ---- place: calendar positions from the bank, with the variety rules of §11 and the windows of §2/§13 ----
const lead=c=>(c.recipe||'none').split('+')[0];
function place(from,to){const placed={};for(const f of fs.existsSync(LEVELS)?fs.readdirSync(LEVELS).filter(f=>/^L\d+\.json$/.test(f)):[]){const L=readJSON(path.join(LEVELS,f));placed[L.level]=L;}
 const banks=fs.existsSync(BANK)?fs.readdirSync(BANK).map(f=>readJSON(path.join(BANK,f))):[];const pool=banks.flatMap(b=>b.candidates);const m=manifest(),usedPic=new Set(m.pictures.filter(p=>p.usedBy).map(p=>p.id));const needs=[];
 for(let n=from;n<=to;n++){if(placed[n])continue;const slot=slotOf(n),prev=[placed[n-1],placed[n-2]].filter(Boolean);
  const window=Object.values(placed).filter(L=>L.level>n-20&&L.level<n+20);
  const ok=pool.filter(c=>!usedPic.has(c.picture)&&c.cls===slot.cls&&fitsStage({N:c.N,colours:c.colours},n)
   &&(slot.intro?(c.intro&&c.recipe===slot.intro):!c.intro&&(c.recipe==='none'||c.recipe.split('+').every(t=>slot.allowed.includes(t))))
   &&(n>3||picture(c.picture).forLevel===n)
   &&!(prev.length===2&&!slot.intro&&lead(c)!=='none'&&prev.every(L=>lead(L)===lead(c)))
   &&!(prev.length===2&&!slot.intro&&prev.every(L=>L.recipe===c.recipe&&c.recipe!=='none'))
   &&!(c.colours>=10&&window.filter(L=>L.colours>=10).length>=2));
  // preference: the preferred zone of the class (first Normal after Hard/Super Hard: 75–95 %), then the size mix per 30
  const zone=slot.cls==='Normal'?(prev[0]&&prev[0].cls!=='Normal'?[.75,.95]:[.6,.9]):slot.cls==='Hard'?[.25,.4]:[.07,.15];
  const bins=Object.values(placed).filter(L=>Math.floor((L.level-1)/30)===Math.floor((n-1)/30)).map(L=>sizeBin(L.N)),want={'200–499':6,'500–999':15,'1000–1400':9};
  const score=c=>{const s=c.random.share;return (s>=zone[0]&&s<=zone[1]?0:Math.min(Math.abs(s-zone[0]),Math.abs(s-zone[1])))+(bins.filter(b=>b===sizeBin(c.N)).length>=want[sizeBin(c.N)]?.3:0);};
  ok.sort((a,b)=>score(a)-score(b));const c=ok[0];
  if(!c){needs.push({level:n,cls:slot.cls,intro:slot.intro,allowed:slot.allowed,stage:stageOf(n)});continue;}
  const L={level:n,cls:slot.cls,intro:slot.intro,picture:c.picture,candidate:c.id,recipe:c.recipe,N:c.N,W:c.W,colours:c.colours,P:c.P,columns:c.columns,linked:c.linked,hidden:c.hidden,random:c.random,solution:c.solution,fairness:c.fairness,strategies:c.strategies,rhythm:c.rhythm,engine:c.engine,definition:{...c.level,id:n,difficulty:slot.cls}};
  writeJSON(path.join(LEVELS,lid(n)+'.json'),L);placed[n]=L;usedPic.add(c.picture);
  const e=m.pictures.find(p=>p.id===c.picture);if(e)e.usedBy=lid(n);const pf=path.join(PICTURES,c.picture+'.json'),pj=readJSON(pf);pj.usedBy=lid(n);writeJSON(pf,pj);
  for(const b of banks)if(b.picture===c.picture){b.candidates=b.candidates.filter(x=>x.id!==c.id);b.placed=(b.placed||[]).concat([c.id]);writeJSON(path.join(BANK,b.picture+'.json'),b);}
  console.log(lid(n),slot.cls.padEnd(10),slot.intro?'знакомство: '+MECH_RU[slot.intro]:'',c.picture,c.recipe,`случайный тест ${Math.round(c.random.share*100)} %`);}
 saveManifest(m);const nf=path.join(ROUNDS,'needs.json'),old=readJSON(nf,{needs:[]});const keep=old.needs.filter(x=>x.level<from||x.level>to);writeJSON(nf,{updated:new Date().toISOString().slice(0,10),needs:keep.concat(needs).sort((a,b)=>a.level-b.level)});
 if(needs.length)console.log(`не заполнено ${needs.length}: ${needs.map(x=>`L${x.level} ${x.cls}${x.intro?' ('+x.intro+')':''}`).join(', ')} → content/rounds/needs.json`);}

function card(n){const L=readJSON(path.join(LEVELS,lid(n)+'.json'));if(!L)return console.log('нет уровня',n);
 console.log(`${lid(n)} · ${L.cls}${L.intro?' · знакомство: '+MECH_RU[L.intro]:''} · картинка ${L.picture} «${L.definition.title}»\nN ${L.N} · W ${L.W} · материалов ${L.colours} · подов ${L.P} · колонок ${L.columns} · мест ${L.definition.capacity}\nрецепт: ${L.recipe} (связанных ${L.linked}, скрытых ${L.hidden})\nслучайный тест: ${Math.round(L.random.share*100)} % из ${L.random.runs}, Wilson95 ${L.random.wilson95.map(x=>Math.round(x*100)+' %').join('–')}\nрешение: ${L.solution.route.length} действий, опасных развилок ${L.solution.forks}, с одним безопасным ${L.solution.singleSafe}\nпосле 80 % W запусков: ${L.rhythm.launchesAfter80}, последний при ${L.rhythm.lastLaunchAtW} % W, время решения ${L.rhythm.seconds} с (×1, после окончания работы), тапы сразу: ${L.solution.fast?L.solution.fast.seconds+' с ('+L.solution.fast.status+')':'—'}\nэтапы: ${L.rhythm.segments.map(s=>`${s.from}–${s.to}%: своб. ${s.meanFree}/${s.minFree}, полный ${Math.round((s.fullShare||0)*100)} %`).join(' · ')}\nпростые стратегии: выиграли ${Object.values(L.strategies).filter(Boolean).length} из ${Object.keys(L.strategies).length}`);}

if(cmd==='calendar'){const a=+process.argv[3]||1,b=+process.argv[4]||30;for(let n=a;n<=b;n++){const s=slotOf(n);console.log(lid(n),s.cls.padEnd(10),s.intro?('знакомство: '+MECH_RU[s.intro]+(SUPPORTED.has(s.intro)?'':' — НЕ ПОДДЕРЖИВАЕТСЯ ДВИЖКОМ')).padEnd(46):''.padEnd(46),'разрешены:',s.allowed.map(x=>MECH_RU[x]+(SUPPORTED.has(x)?'':' (нет в движке)')).join(', ')||'без допов');}}
else if(cmd==='build'){const ids=flag('--approved')?manifest().pictures.filter(p=>p.status==='approved'&&!p.usedBy).map(p=>p.id):process.argv.slice(3).filter(a=>/^P\d+$/.test(a));await build(ids);}
else if(cmd==='place'){place(+process.argv[3],+process.argv[4]);}
else if(cmd==='bank'){for(const f of fs.existsSync(BANK)?fs.readdirSync(BANK):[]){const b=readJSON(path.join(BANK,f));const by={};for(const c of b.candidates){const k=c.cls+(c.intro?' (знакомство)':'');(by[k]=by[k]||[]).push(`${c.recipe} ${Math.round(c.random.share*100)}%`);}console.log(b.picture,JSON.stringify(by),b.rejected.length?'отклонения: '+b.rejected.map(x=>x.reason).join('; '):'');}}
else if(cmd==='card'){card(+process.argv[3]);}
else{console.log(fs.readFileSync(new URL(import.meta.url),'utf8').split('\n').filter(l=>l.startsWith('//')).join('\n'));}

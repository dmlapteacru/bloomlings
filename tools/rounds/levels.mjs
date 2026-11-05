// Round levels from approved pictures (skill bloomlings-round-levels; rules: product/17_LEVEL_GENERATION_RULES.md
// §1, §3–§8, §10–§11, §13–§14).
//
// node tools/rounds/levels.mjs calendar <from> <to>        class, mechanic introduction and allowed mechanics per level
// node tools/rounds/levels.mjs build <pictureId…|--approved> [--classes Normal,Hard,"Super Hard"] [--candidates 64]
//      [--recipes none,links,hidden,links+hidden,bridge,…] [--seed 1] [--fresh: search again a combination already searched]
//      [--overlay up,left,right,two: on a wall the generator lays over the picture, lib.mjs wallOverlay; none = the
//      picture's own board] [--pairs Normal:links,Hard:bridge+links@up: only these class:recipe(@overlay side) pairs]
//      [--no-intro] [--accents auto|on|off: accents of 17 §6 (2026-11-01) painted as the level's recolouring, auto = every
//      Hard and Super Hard attempt and every third regular Normal attempt]
//      [--slot regular|after-hard|breather] [--want n] [--want-loss] [--minutes m] [--intro] [--max-colours n] [--legacy]: every non-bridge level of a
//      free picture is built by the generator's tuning (tune.mjs, README «The generator (2026-11-02)»): the model's blend, ≤ 5
//      quick tests, links under the forced limit, hidden pods / cells after the class; --legacy the old random blend
//      [--channel-kind single]: a Super Hard with a water channel must decide (exactly one exit wins) and its class is
//      measured with that exit only (17 §5, the owner, 2026-11-05; evaluate's channelKind)
//      candidates for each picture, recipe and class: balance → queue → saved solution → random test → bank. Recipes put
//      the lead first (17 §7): none, links, hidden, bridge, links+hidden, hidden+links, bridge+links, bridge+hidden,
//      hidden+bridge, links+bridge, links+bridge+hidden, bridge+links+hidden; hidden cells (17 §8, 2026-11-01) as 'cells':
//      cells, links+cells, cells+links, bridge+cells, bridge+links+cells … (never with hidden); the latch with its acorn key
//      (17 §8, 2026-11-01) as 'latch': latch, links+latch, latch+links, latch+cells, links+cells+latch …; a board with a strip takes bridge recipes only,
//      a board without one the others
// node tools/rounds/levels.mjs place <from> <to> [--pick <level>:<candidate>,…]  fill calendar positions from the bank (variety rules),
//      write levels and cards, list what is still missing in content/rounds/needs.json; a position of an approved
//      mechanics plan (content/rounds/runs/<wave>/mechanics.json) takes only its planned recipe and bridge side; the wave's
//      pinned pictures (mechanics.json pin, reserved by rounds.mjs plan) go to their positions first; no two neighbouring
//      levels from one picture pack while another candidate fits (lib.mjs packOf); a franchise picture only with the
//      owner's rights (rights 'owner')
// node tools/rounds/levels.mjs unplace <from> <to>         put placed levels back into the bank (to place again)
// node tools/rounds/levels.mjs adopt <level> <preview.json> place a hand-built level (17 §13 «Кто строит уровни»: the
//      experiment's full.mjs output, checked by evaluate) on its calendar position and so lock it: place keeps it; its
//      picture must be approved, its class the calendar's and its recipe and bridge side the mechanics plan's
// node tools/rounds/levels.mjs bank                        the bank: candidates by picture and measured class
// node tools/rounds/levels.mjs card <level>                the level card (§14)
// node tools/rounds/levels.mjs checks [--shard i/n] [--levels] [--calm]  add missing checks (17 §5–§9, §13 layer B) to banked candidates and placed
//      levels (the §5 calm test only with --calm: place measures the candidates it weighs)
import fs from 'node:fs';import path from 'node:path';
import {franchiseBlocked,packOf,waveRange,siteChoice,ROUNDS,PICTURES,LEVELS,BANK,CODES,PALETTE,manifest,saveManifest,picture,readJSON,writeJSON,lid,pid,arg,flag,measure,stageOf,fitsStage,sizeBin,budget,columnsFor,classOf,slotOf,INTRO,MECH_RU,SUPPORTED,recipeAllowed,boardOf,usage,motifsOf,overlaid,bridgeSide,wallOverlay,planOf,fitsPlan,mechanicsPlans} from './lib.mjs';import {execFileSync} from 'node:child_process';
import {fitness} from './fitness.mjs';import {deltaE,labOf} from './colour-distance.mjs';
import {tuneJob,ZONES} from './tune.mjs';
import {GEN,breatherRecipeOk,BREATHER_ZONE,repeatClash,wideClash,medianPenalty,greedyQuota,MEDIAN} from './wave.mjs';
import {channelExits,winningExit,shutterCheck,latchCheck,LATCH_DEPTH,LATCH_BEHIND,cellsHiddenAtStart,cellFairness,randomTest,quickShare,solve,fairness,strategies,rhythm,bridgeChoicesWin,bridgeChoices,greedy,greedyBeaten,openingTest,forcedShare,holdPod,hiddenMatters,bridgeSingle,bridgeLoss,pressure,calmTest,calmFails,calmBorderline,trapLinks,classKeep} from './measure.mjs';
// layer B of 17 §13: the bounds of the metrics of interest (content/rounds/quality.json)
const QUALITY=readJSON(path.join(ROUNDS,'quality.json'),{pressure:{min:{},games:30,mode:'soft'}});import {engine,rulesSync} from './engine.mjs';import {createRequire as _cr} from 'node:module';
// the engine synchronously (the latch timing runs inside candidate, which is synchronous)
const _req=_cr(import.meta.url),engineSync=()=>({R:_req('./engine/rules.cjs'),idle:_req('./engine/idle-rules.cjs')});
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
// a bridge site counts as a path here (one bridge is built before the far bank opens); `only` keeps one bridge, so the
// queue is ordered for that bridge and the other one opens the far bank in a less handy order (a real choice)
function depths(board,only=null){const {w,h,rowsBottomFirst:rows,entries}=board,sites=new Set((board.hero?board.hero.choices:[]).filter((c,i)=>only===null||i===only).map(c=>c.cell)),g=[...rows.join('')].map((ch,i)=>sites.has(i)?'.':ch).join(''),d=new Float64Array(w*h).fill(Infinity),dq=[];
 for(const e of entries){d[e]=0;dq.push(e);}
 while(dq.length){const c=dq.shift(),x=c%w,y=Math.floor(c/w);for(const [dx,dy] of [[1,0],[-1,0],[0,1],[0,-1]]){const u=x+dx,v=y+dy;if(u<0||v<0||u>=w||v>=h)continue;const n=v*w+u,ch=g[n];if(ch==='#')continue;
  const cost=CODES.includes(g[c])?1:0,nd=d[c]+cost;if(nd<d[n]){d[n]=nd;cost?dq.push(n):dq.unshift(n);}}}
 return {d,g};}

// with a water channel (17 §8): the access depth when exit `exit` is chosen the moment the spring is reached: a cell is
// reached either from the entries or, after the spring's depth, from the exit (depth from the exit as an entry)
export function channelDepths(board,exit,only=null){const a=depths(board,only),ch=board.channel,ds=a.d[ch.spring];if(!(ds<Infinity))return a;
 const b=depths({...board,entries:[ch.exits[exit].cell]},only),d=a.d.map((x,i)=>Math.min(x,ds+b.d[i]));return {d,g:a.g};}
// ---- doses (§7, §9): ranges by class and role; a small queue (< 16 pods) has its own table ----
// hidden cells ('cells', 17 §8 «Скрытые клетки», 2026-11-01): shares of N (the colour cells), counting only the cells still
// hidden after the start reveal; the small-queue table (P < 16) gives them the lead range
const DOSE_T={links:{lead:[[.1,.3],[.2,.45],[.35,.6]],add:[[.05,.15],[.1,.2],[.15,.25]]},hidden:{lead:[[.05,.15],[.1,.25],[.15,.35]],add:[[.05,.1],[.05,.1],[.05,.15]]},
 cells:{lead:[[.05,.1],[.1,.2],[.15,.3]],add:[[.05,.08],[.05,.08],[.05,.08]]}};
// latch + acorn key (17 §7 table, §8; the owner's amendment of 2026-11-03): counts of latch/key pairs, Normal 1, Hard 2–3,
// Super Hard exactly 3, lead and additional alike; a small queue 1
const LATCH_T={lead:[[1,1],[2,3],[3,3]],add:[[1,1],[2,3],[3,3]]};
// wooden shutters (17 §7 table, §8 «Twig Деревянные створки», the owner, 2026-11-05): objects, lead Normal 1, Hard 1–2,
// Super Hard 2–3, an additional one 1; a small queue 1. Their area and counters: SHUTTER below. The water channel (17 §7,
// §8 «Drop Водный канал») is always one channel with two exits.
export const SHUTTER_T={lead:[[1,1],[1,2],[2,3]],add:[[1,1],[1,1],[1,1]]};
function doseRange(m,cls,role,P,N=0){const i=CLASSES.indexOf(cls);
 if(m==='latch')return P<16?[1,1]:LATCH_T[role][i].slice();
 if(m==='shutters')return P<16?[1,1]:SHUTTER_T[role][i].slice();
 if(m==='canal')return [1,1];
 if(m==='cells'){const [a,b]=DOSE_T.cells[P<16?'lead':role][i];return [Math.ceil(a*N),Math.floor(b*N)];}
 if(P<16)return m==='hidden'?[[1,2],[2,3],[3,4]][i]:m==='links'?[[2,2],[4,4],[4,6]][i]:[1,1];
 const T=DOSE_T;
 if(!T[m])return [1,1];const [a,b]=T[m][role][i];return [Math.ceil(a*P),Math.floor(b*P)];}
// the roles of a recipe's pod mechanics (17 §7): the difficulty is the pods' (the owner, 2026-10-30), so a bridge takes no
// part: the pod mechanics get the doses of the recipe without it (links lead, hidden never leads after a bridge)
function podRoles(types){const pod=types.filter(m=>m!=='bridge'),bridge=pod.length<types.length;
 return {count:pod.length,role:m=>(bridge&&m==='hidden')||pod.indexOf(m)>0?'add':'lead',first:m=>pod.indexOf(m)===0};}
// the doses of a recipe as shares of P (17 §7, a queue of 16+ pods), for the wave plan's breakdown: the lead its range
// (three pod types: its lower half), an additional one its minimum (alone after a bridge: its range), a bridge one choice
// of two sites
export function doseShares(recipe,cls){const types=recipe==='none'?[]:recipe.split('+'),i=CLASSES.indexOf(cls),pr=podRoles(types);
 return types.map(m=>{if(m==='canal')return {m,role:pr.role(m),count:[1,1]};
  if(m==='shutters'){const role=pr.role(m);let [a,b]=SHUTTER_T[role][i];if(!pr.first(m))b=a;else if(role==='lead'&&pr.count>=3)b=Math.floor((a+b)/2);return {m,role,count:[a,b]};}
  if(m==='latch'){const role=pr.role(m);let [a,b]=LATCH_T[role][i];if(!pr.first(m))b=a;else if(role==='lead'&&pr.count>=3)b=Math.floor((a+b)/2);return {m,role,count:[a,b]};}
  if(!DOSE_T[m])return {m,role:'lead',choice:1};const role=pr.role(m);let [a,b]=DOSE_T[m][role][i];if(!pr.first(m))b=a;else if(role==='lead'&&pr.count>=3)b=(a+b)/2;return {m,role,range:[a,b]};});}
export {doses};
// recipe → concrete doses: one type uses its full range, two types: the additional at its minimum, three: the lead in
// the lower half (§7); the introduction levels use the minimum teaching doses (§9)
// the dose cycle of 17 §7 (the owner's amendment of 2026-10-27): the lead dose sits at these places of its range
export const DOSE_CYCLE=[.5,.8,.4,.7,1,.6];
// the dose place a banked candidate was built for, read back from its attempt number (P####-…-k)
export const posOf=(id,recipe,intro)=>{if(intro||(recipe||'none')==='none')return null;const k=+String(id).split('-').pop();return Number.isInteger(k)?DOSE_CYCLE[k%DOSE_CYCLE.length]:null;};
// N: the colour cells of the board (the doses of hidden cells are shares of N)
// breather (17 §5 «Передышка», the owner, 2026-11-02): its link dose is one plain pair
function doses(recipe,cls,P,intro,N=0,{breather=false}={}){const types=recipe==='none'?[]:recipe.split('+'),out={},pr=podRoles(types);
 // hidden pods and hidden cells never on one level (17 §8, the owner, 2026-11-01)
 if(!recipeAllowed(recipe))out.error='скрытые поды и скрытые клетки не бывают на одном уровне (§8)';
 types.forEach(m=>{if(m==='bridge'){out.bridge=1;return;}const role=pr.role(m),j=pr.first(m)?0:1;let [a,b]=doseRange(m,cls,role,P,N);
  // the engine plays at most six hidden (depth) pods
  if(m==='hidden'){a=Math.min(a,6);b=Math.min(b,6);}
  // teaching doses (§9): 1 pair of links, 2–3 hidden pods (the owner's second amendment of 2026-10-27)
  // an additional mechanic takes the minimum admissible dose of its range: for links the smallest even count in it (pairs,
  // §7: 10 % of 24 pods is 2.4 → 3 → 4 participants, still inside 10–20 %)
  // 5–10 really hidden cells on a hidden-cell introduction (§9); one latch with its key on a latch introduction
  // latches keep their whole count range in any role (the owner's amendment of 2026-11-03: Hard 2–3, Super Hard 3)
  if(intro){if(m==='hidden'){a=2;b=3;}else if(m==='cells'){a=5;b=10;}else a=b=m==='links'?2:1;}else if(m==='latch'){}else if(j>0){if(m==='links')a+=a%2;b=a<=b?a:b;}else if(role==='lead'&&pr.count>=3){b=Math.floor((a+b)/2);}
  if(m==='links'){a+=a%2;b-=b%2;}if(breather&&m==='links')a=b=2;if(a>b)out.error=`нет целых доз ${m} для ${cls} при P=${P}`;out[m]=[a,b];});
 return out;}
// the colour cells of a level's board (N)
export const cellsOf=level=>{let n=0;for(const ch of level.rowsBottomFirst.join(''))if(CODES.includes(ch))n++;return n;};
export function dosesOk(level,cls,{breather=false}={}){const P=level.pods.length,hidden=level.pods.filter(p=>p.mystery).length,linked=level.pods.filter(p=>p.group).length,types=(level.recipe||'none')==='none'?[]:level.recipe.split('+');
 const d=doses(level.recipe||'none',cls,P,false,cellsOf(level),{breather});if(d.error)return d.error;
 if(types.includes('cells')!==!!(level.hiddenCells&&level.hiddenCells.length))return 'рецепт и скрытые клетки не совпадают';
 if(types.includes('cells')){const k=cellsHiddenAtStart(level);if(k<d.cells[0]||k>Math.max(d.cells[1],d.cells[0]))return `скрытых клеток после стартового раскрытия ${k} вне ${d.cells.join('–')} для ${cls}`;}
 if(types.includes('links')&&(linked<d.links[0]||linked>Math.max(d.links[1],d.links[0])))return `связанных ${linked} вне ${d.links.join('–')} для ${cls}`;
 if(types.includes('hidden')&&(hidden<d.hidden[0]||hidden>Math.max(d.hidden[1],d.hidden[0])))return `скрытых ${hidden} вне ${d.hidden.join('–')} для ${cls}`;
 // latch + key (17 §7, §8): the recipe and the level's latches agree, their count in the dose for the class and role
 {const nl=(level.latches||[]).length;if(types.includes('latch')!==(nl>0))return 'рецепт и засовы не совпадают';
  if(types.includes('latch')&&(nl<d.latch[0]||nl>Math.max(d.latch[1],d.latch[0])))return `засовов ${nl} вне ${d.latch.join('–')} для ${cls}`;}
 // the water channel (17 §7, §8): the recipe and the level's channel agree (one channel, two exits: R.validate)
 if(types.includes('canal')!==!!level.channel)return 'рецепт и канал не совпадают';
 // wooden shutters (17 §7, §8): the recipe and the level's shutters agree, their number in the dose, each counter of its
 // material's work in the class range (a multiple of 5, at least 10, the material at least 40 units)
 {const sh=level.shutters||[];if(types.includes('shutters')!==(sh.length>0))return 'рецепт и створки не совпадают';
  if(sh.length){if(sh.length<d.shutters[0]||sh.length>Math.max(d.shutters[1],d.shutters[0]))return `створок ${sh.length} вне ${d.shutters.join('–')} для ${cls}`;
   const role=podRoles(types).role('shutters'),bad=shutterCountErrors(level,cls,role);if(bad.length)return bad[0];}}
 // a Normal takes two types, three when one is the latch (the owner's amendment of 2026-11-03: keys, links and hidden)
 const max=cls==='Normal'&&!types.includes('latch')?2:3;if(types.length>max)return `типов ${types.length} больше ${max} для ${cls}`;if(P<16&&types.length>1)return 'очередь меньше 16 подов — только один тип';return null;}

// ---- one candidate: order pods by access depth with noise, deal them into columns, add the recipe's mechanics ----
// a water channel (17 §8): the queue is ordered for one exit (opts.exit, else drawn), as if that exit were chosen as soon as
// the spring is reached (channelDepths), so the pods of its region come earlier and with the other exit they wait
export function candidate(pic,board,base,cols,recipe,cls,seed,intro,pos=null,opts={}){const r=rnd(seed),intended=recipe.includes('bridge')&&board.hero?(k=>Number.isInteger(opts.intended)?opts.intended:k)(Math.floor(r()*board.hero.choices.length)):null,pods=base.map(p=>({...p}));
 const exitOf=board.channel?(Number.isInteger(opts.exit)?opts.exit:Math.floor(r()*board.channel.exits.length)):null,{d,g}=exitOf!==null?channelDepths(board,exitOf,intended):depths(board,intended);
 const byColour={};for(let i=0;i<g.length;i++)if(CODES.includes(g[i]))(byColour[g[i]]=byColour[g[i]]||[]).push(d[i]);
 const maxD=Math.max(1,...Object.values(byColour).flat());
 for(const [c,ds] of Object.entries(byColour)){ds.sort((a,b)=>a-b);const ps=pods.filter(p=>p.color===c);let k=0;for(const p of ps){const slice=ds.slice(k,k+p.count);k+=p.count;p.key=slice.reduce((a,b)=>a+b,0)/slice.length/maxD;}}
 // a bridge that alone is the solution (§8, from the second introduction): pods whose cells are near the intended bridge
 // but far from the other one come earlier, so with the other bridge their pods wait in places without work and jam
 // (every non-introduction bridge level needs one deciding bridge, so it always leans; an introduction in 60 % of candidates)
 let lean=0;if(intended!==null&&board.hero.choices.length>1&&(r()<.6||!intro)){lean=.5+2.5*r();const o=depths(board,1-intended).d,byO={};
  for(let i=0;i<g.length;i++)if(CODES.includes(g[i]))(byO[g[i]]=byO[g[i]]||[]).push(o[i]);
  for(const [c,ds] of Object.entries(byO)){ds.sort((a,b)=>a-b);let k=0;for(const p of pods.filter(p=>p.color===c)){const sl=ds.slice(k,k+p.count);k+=p.count;p.keyO=sl.reduce((a,b)=>a+b,0)/sl.length/maxD;}}}
 const sigma=opts.sigma??{Normal:.06,Hard:.16,'Super Hard':.26}[cls];
 for(const p of pods){const z=Math.sqrt(-2*Math.log(r()+1e-9))*Math.cos(2*Math.PI*r());p.order=p.key-lean*Math.max(0,(p.keyO??p.key)-p.key)+sigma*z;}
 // Normal blends a little towards the reverse access order too, so its random test lands in 50–75 % (§5, median 50–60 %)
 // (a bridge level leans to its intended bridge instead; a split search, bridge-scene.mjs, may still steer it with opts.t)
 // (the generator's tuning, tune.mjs, gives t for every Normal, an introduction and a breather too)
 if(cls==='Normal'&&(!intro||opts.t!=null)&&(!lean||opts.t!=null)){const t0=r(),t=opts.t??t0;for(const p of pods)p.order=(1-t)*p.order+t*(1-p.key);}
 // harder classes (§6.1): the queue is blended between the access order and its reverse by t, drawn per candidate, so
 // outer colours move deeper and inner ones come earlier and wait in places; a careless tap jams the places. The
 // measured class decides where a candidate may go (§5), the saved solution proves it stays winnable.
 // (opts.tStrong: a split search, bridge-scene.mjs, steers this blend by the shares it measures)
 if(cls!=='Normal'){const t=opts.tStrong??(cls==='Hard'?.25+.65*r():.45+.55*r());for(const p of pods)p.order=(1-t)*p.order+t*(1-p.key);}
 const ord=pods.map((p,i)=>i).sort((a,b)=>pods[a].order-pods[b].order),tray=Array.from({length:cols},()=>[]);
 for(let i=0;i<ord.length;i+=cols){const row=ord.slice(i,i+cols),cs=[...Array(cols).keys()].sort(()=>r()-.5).sort((a,b)=>tray[a].length-tray[b].length);row.forEach((p,j)=>tray[cs[j]].push(p));}
 // a bridge scene (bridge-scene.mjs; opts.gate = a row of the tray, the owner, 2026-10-28: "подшаманить с лотами в
 // начале, чтобы был только один выигрышный мост"): the strip right behind the other bridge is a gate of one colour; that
 // colour's pods (but its first one when the colour also lies on the near bank, where that pod clears its bed) stand at least that deep, so with the other
 // bridge the places fill with pods that find no work before the gate's pods come up
 if(opts.gate&&intended!==null&&board.hero.choices.length===2){const o=board.hero.choices[1-intended],gc=g[2*o.cell-o.anchor];
  if(CODES.includes(gc)){const d0=depths(board,-1).d,nearHas=[...g].some((ch,i)=>ch===gc&&d0[i]<Infinity),late=pods.map((p,i)=>i).filter(i=>pods[i].color===gc).sort((a,b)=>pods[a].key-pods[b].key).slice(nearHas?1:0);
   for(const i of late){const c=tray.findIndex(st=>st.includes(i)),k=tray[c].indexOf(i);if(k>=opts.gate)continue;
    const to=[];tray.forEach((st,c2)=>st.forEach((q,k2)=>{if(k2>=opts.gate&&pods[q].color!==gc)to.push([c2,k2]);}));if(!to.length)break;
    const [c2,k2]=to[Math.floor(r()*to.length)];[tray[c][k],tray[c2][k2]]=[tray[c2][k2],tray[c][k]];}
   // opts.delay (a row): the pods of the other bridge's approach (its anchor's colour) stand at least that deep, so the
   // winning bridge tends to be offered first and the other one later, in part of the games
   const wc=g[o.anchor];if(opts.delay&&CODES.includes(wc)&&wc!==gc)for(const i of pods.map((p,i)=>i).filter(i=>pods[i].color===wc)){const c=tray.findIndex(st=>st.includes(i)),k=tray[c].indexOf(i);if(k>=opts.delay)continue;
    const to=[];tray.forEach((st,c2)=>st.forEach((q,k2)=>{if(k2>=opts.delay&&k2<opts.gate&&pods[q].color!==gc&&pods[q].color!==wc)to.push([c2,k2]);}));if(!to.length)break;
    const [c2,k2]=to[Math.floor(r()*to.length)];[tray[c][k],tray[c2][k2]]=[tray[c2][k2],tray[c][k]];}}}
 // opts.expose (a split search for a single deciding bridge, bridge-scene.mjs): a pod of the other bridge's approach colour
 // stands in the second row, so random games also clear that approach first in part of the games (17 §8: both sites
 // reachable at the choice; measure.mjs bridgeSingle)
 if(opts.expose&&intended!==null&&board.hero.choices.length===2){const o=board.hero.choices[1-intended],wc=g[o.anchor],gc=g[2*o.cell-o.anchor];
  if(CODES.includes(wc)&&wc!==gc&&!tray.some(st=>st.slice(0,2).some(p=>pods[p].color===wc))){const i=pods.map((p,i)=>i).filter(i=>pods[i].color===wc).sort((a,b)=>pods[a].key-pods[b].key)[0];
   const cs=[...Array(cols).keys()].filter(c=>tray[c].length>=2);if(i!==undefined&&cs.length){const c=tray.findIndex(st=>st.includes(i)),k=tray[c].indexOf(i),c2=cs[Math.floor(r()*cs.length)];[tray[c2][1],tray[c][k]]=[tray[c][k],tray[c2][1]];}}}
 // accents (17 §6 «Акценты для давления на места», the owner, 2026-11-01; opts.accents = the accent materials): each accent
 // pod stands in an early or middle row (the second row to the middle of the tray), so it comes up before its few deep
 // cubes are reachable and holds a place while the rest is played
 if(opts.accents&&opts.accents.length){const acc=new Set(opts.accents),rows=Math.max(...tray.map(t=>t.length)),lo=1,hi=Math.max(1,Math.floor((rows-1)/2));
  for(const i of pods.map((p,i)=>i).filter(i=>acc.has(pods[i].color))){const c=tray.findIndex(st=>st.includes(i)),k=tray[c].indexOf(i);if(k>=lo&&k<=hi)continue;
   const to=[];tray.forEach((st,c2)=>st.forEach((q,k2)=>{if(k2>=lo&&k2<=hi&&!acc.has(pods[q].color))to.push([c2,k2]);}));if(!to.length)break;
   const [c2,k2]=to[Math.floor(r()*to.length)];[tray[c][k],tray[c2][k2]]=[tray[c2][k2],tray[c][k]];}}
 // §6: at the start at least one visible launch has reachable work; else the shallowest pod swaps with a column's top
 const frontier=new Set();for(let i=0;i<g.length;i++)if(CODES.includes(g[i])&&d[i]===0)frontier.add(g[i]);
 if(!tray.some(st=>st.length&&frontier.has(pods[st[0]].color))){const best=ord.filter(i=>frontier.has(pods[i].color)).sort((a,b)=>pods[a].key-pods[b].key)[0];
  if(best!==undefined){const c=Math.floor(r()*cols),[bc,bi]=(()=>{for(let k=0;k<cols;k++){const j=tray[k].indexOf(best);if(j>=0)return [k,j];}})();[tray[c][0],tray[bc][bi]]=[tray[bc][bi],tray[c][0]];}}
 // §6 (the owner's second amendment of 2026-10-27): a Normal keeps a pod of a deep colour (no reachable cell at the
 // start) in the first two rows, so it holds a place until its work opens; the shallowest deep pod moves up to row 2
 // (opts.noHold: a breather or the first Normal after a Hard, 17 §5, needs none)
 if(cls==='Normal'&&!intro&&!opts.noHold&&!pic.locked&&!tray.some(st=>st.slice(0,2).some(p=>!frontier.has(pods[p].color)))){
  const deep=[];tray.forEach((st,c)=>st.forEach((p,row)=>{if(row>=2&&!frontier.has(pods[p].color))deep.push([c,row]);}));deep.sort((a,b)=>pods[tray[a[0]][a[1]]].key-pods[tray[b[0]][b[1]]].key);
  const cs=[...Array(cols).keys()].filter(c=>tray[c].length>=2);if(deep.length&&cs.length){const [dc,dr]=deep[0],c=cs[Math.floor(r()*cs.length)];[tray[c][1],tray[dc][dr]]=[tray[dc][dr],tray[c][1]];}}
 const types=recipe==='none'?[]:recipe.split('+'),dz=doses(recipe,cls,pods.length,intro,[...g].filter(ch=>CODES.includes(ch)).length);if(dz.error)return {error:dz.error};
 // opts.noLinks: the generator chooses the links itself, one pair at a time (tune.mjs chooseLinks)
 if(types.includes('links')&&!opts.noLinks){const want=pos===null||intro?dz.links[0]+2*Math.floor(r()*((dz.links[1]-dz.links[0])/2+1)):dz.links[0]+2*Math.round(pos*(dz.links[1]-dz.links[0])/2),pairs=[];const used=new Set();
  // pairs of neighbouring columns at the same depth; harder classes prefer a useful-now pod with a later one (§6.3)
  const opts=[];for(let c=0;c+1<cols;c++)for(let row=0;row<Math.min(tray[c].length,tray[c+1].length);row++)opts.push([tray[c][row],tray[c+1][row],row]);
  opts.sort((x,y)=>(cls==='Normal'?Math.abs(pods[x[0]].key-pods[x[1]].key)-Math.abs(pods[y[0]].key-pods[y[1]].key):Math.abs(pods[y[0]].key-pods[y[1]].key)-Math.abs(pods[x[0]].key-pods[x[1]].key))+(r()-.5)*.2);
  for(const [a,b,row] of opts){if(pairs.length*2>=want)break;if(used.has(a)||used.has(b)||(intro&&row<1))continue;used.add(a);used.add(b);pairs.push([a,b]);}
  if(pairs.length*2<want)return {error:'не хватило пар соседних подов для дозы связок'};pairs.forEach(([a,b],i)=>{const gname=String.fromCharCode(65+i%26)+(i>=26?String(Math.floor(i/26)):'');pods[a].group=gname;pods[b].group=gname;});}
 // opts.noInfo: hidden pods and hidden cells come after the class is set (tune.mjs addInfo; the calibration: they change
 // none of the §5 tests)
 if(types.includes('hidden')&&!opts.noInfo){const want=pos===null||intro||types[0]!=='hidden'?dz.hidden[0]+Math.floor(r()*(dz.hidden[1]-dz.hidden[0]+1)):dz.hidden[0]+Math.round(pos*(dz.hidden[1]-dz.hidden[0]));
  // from the second row down, never in a group (the engine reveals a pod on reaching the top only)
  let opts=tray.flatMap(st=>st.map((p,row)=>[p,row])).filter(([p,row])=>row>=1&&!pods[p].group).sort(()=>r()-.5);
  // an introduction puts its first hidden pod in the second row, to reveal within the first launches (§8)
  if(intro){const k=opts.findIndex(([,row])=>row===1);if(k<0)return {error:'нет подходящего места во втором ряду для скрытого'};opts=[opts[k],...opts.filter((_,i)=>i!==k)];}
  if(opts.length<want)return {error:'не хватило подов для скрытых'};opts.slice(0,want).forEach(([p])=>pods[p].mystery='depth');}
 // hidden cells (17 §8, 2026-11-01): the lead its dose place, an additional one its minimum, an introduction 5–10
 let hiddenCells=null;
 if(types.includes('cells')&&!opts.noInfo){const [a,b]=dz.cells,lead0=types.filter(m=>m!=='bridge')[0]==='cells',want=pos===null||intro||!lead0?a+Math.floor(r()*(b-a+1)):a+Math.round(pos*(b-a));
  // never a recoloured cell (an accent must be seen to read as the reason its pod waits)
  const avoid=new Set([...(board.recolor||[]).map(([x,y])=>(board.h-1-(y+2))*board.w+x+2),...shutterCells(board)]);
  hiddenCells=pickHiddenCells(board,want,r,{intro,avoid});if(!hiddenCells)return {error:'не хватило глубоких клеток для скрытых клеток'};}
 // latch + acorn key (17 §8, 2026-11-01): the lead its dose place, an additional one its minimum, an introduction one
 let latches=null;
 if(types.includes('latch')){const [a,b]=dz.latch,lead0=types.filter(m=>m!=='bridge')[0]==='latch',want=opts.latchWant!=null?opts.latchWant:pos===null||intro||!lead0?a+Math.floor(r()*(b-a+1)):a+Math.round(pos*(b-a));
  const lp=pickLatches(board,tray,pods,want,cls,r,{intro});if(lp.error)return {error:lp.error};latches=lp.latches;}
 if(types.includes('canal')!==!!board.channel)return {error:types.includes('canal')?'канал: на поле нет канала (placeChannel)':'на поле канал, а в рецепте его нет'};
 if(types.includes('shutters')!==!!(board.shutters&&board.shutters.length))return {error:types.includes('shutters')?'створки: на поле нет створок (placeShutters)':'на поле створки, а в рецепте их нет'};
 if(types.includes('bridge')&&!board.hero)return {error:'на картинке нет мест моста (b)'};
 // a strip cuts a part off: without a bridge its cells cannot be reached
 if(board.hero&&!types.includes('bridge'))return {error:'на поле полоса с местами моста — нужен рецепт с мостом'};
 // a level on the generator's wall keeps the wall (picture coordinates), so its board is rebuilt from the approved picture
 const level={id:1,title:pic.title,subject:pic.id,w:board.w,h:board.h,rowsBottomFirst:board.rowsBottomFirst,entries:board.entries,capacity:5,pods:pods.map(({key,keyO,order,...p})=>p),tray,recipe,...(types.includes('bridge')?{hero:board.hero}:{}),...(board.overlay?{overlay:board.overlay}:{}),...(board.recolor?{recolor:board.recolor}:{}),...(board.strip?{strip:board.strip}:{}),...(hiddenCells?{hiddenCells}:{}),...(latches?{latches}:{}),...objectsOf(board)};
 return {level};}

// ---- latch + acorn key (17 §8 «Засов и жёлудь-ключ», the owner, 2026-11-01; README «Latch and key») ----
// The holes a key may lie in: 4-connected areas of empty '.' cells that the start board does not reach from an entry
// (open cells only, bridge sites closed) but that clearing colour cells reaches without a bridge (access depth finite with
// every site a wall, so a key never hangs on the bridge choice). Each hole: its cells, its depth (the fewest colour cells
// to clear on the way, depths()), the key cell (its cell nearest the hole's centre, ties by index). Sorted by depth.
// A hole holding a cell of `skip` (board indices; default the board's water channel: its spring and exits, 17 §8) is no
// key hole (a key never stands on the channel's cells).
export function keyHoles(board,skip=channelCells(board)){const {w,h,rowsBottomFirst:rows,entries}=board,g=rows.join(''),n=w*h,{d}=depths(board,-1),start=new Uint8Array(n),q=[];
 for(const e of entries)if(g[e]==='.'){start[e]=1;q.push(e);}
 const nb=i=>{const x=i%w,o=[];if(i>=w)o.push(i-w);if(i+w<n)o.push(i+w);if(x>0)o.push(i-1);if(x<w-1)o.push(i+1);return o;};
 while(q.length){const i=q.pop();for(const j of nb(i))if(!start[j]&&g[j]==='.'){start[j]=1;q.push(j);}}
 const seen=new Uint8Array(n),holes=[];
 for(let i=0;i<n;i++){if(seen[i]||g[i]!=='.'||start[i]||!(d[i]<Infinity))continue;const cells=[i];seen[i]=1;
  for(let k=0;k<cells.length;k++)for(const j of nb(cells[k]))if(!seen[j]&&g[j]==='.'&&!start[j]){seen[j]=1;cells.push(j);}
  const cx=cells.reduce((s,c)=>s+c%w,0)/cells.length,cy=cells.reduce((s,c)=>s+Math.floor(c/w),0)/cells.length;
  const key=cells.slice().sort((a,b)=>Math.hypot(a%w-cx,Math.floor(a/w)-cy)-Math.hypot(b%w-cx,Math.floor(b/w)-cy)||a-b)[0];
  if(skip&&cells.some(c=>skip.has(c)))continue;
  holes.push({cells:cells.sort((a,b)=>a-b),depth:Math.min(...cells.map(c=>d[c])),key});}
 return holes.sort((a,b)=>a.depth-b.depth||a.key-b.key);}
// When the candidate's queue reaches what (latchTiming): one patient game on the queue without latches (each launch
// settles before the next; the launch with the most reachable work now, a buildable bridge first): top[c][d] = the
// launches made when column c's head reaches d (a latch at depth d would be on top from then), hole[k] = the launches made
// when hole k's key is first reached (Infinity if never in that game).
export function latchTiming(board,tray,pods,holes){const {idle:R}=engineSync(),lv={id:1,title:'t',w:board.w,h:board.h,rowsBottomFirst:board.rowsBottomFirst,entries:board.entries,capacity:5,pods,tray,...(board.hero?{hero:board.hero}:{}),...objectsOf(board)};
 let s=R.create(lv,5),n=0;const top=tray.map(st=>new Array(st.length+1).fill(Infinity)),hole=holes.map(()=>Infinity);
 const mark=()=>{tray.forEach((_,c)=>{if(top[c][s.heads[c]]===Infinity)top[c][s.heads[c]]=n;});const d=R.reach(s).distance;holes.forEach((h,k)=>{if(hole[k]===Infinity&&d[h.key]>0)hole[k]=n;});};
 mark();
 for(let g=0;g<4*pods.length&&s.status==='Playing';g++){const mv=R.legal(s);if(!mv.length)break;let a=mv.find(x=>x.type==='bridge'||x.type==='channel');
  if(!a){const t=R.reach(s).targets,work=c=>t.reduce((k,i)=>k+(s.board[i]===c),0);a=mv.slice().sort((x,y)=>work(s.meta.colours[y.pod])-work(s.meta.colours[x.pod]))[0];}
  s=R.step(s,a).state;if(a.type==='tap')n++;mark();}
 return {top,hole};}
// `want` latches with their keys on a candidate's tray: each in its own column (17 §8: one active latch per column), at a
// depth of the class's range (LATCH_DEPTH: Normal 3–6, Hard 2–6, Super Hard 2–8 pods before it) with 2–8 pods behind
// (at most 12), never with a hidden pod behind it (§7), its key in a hole of its own (keyHoles). The key opens partway:
// on the candidate's queue (latchTiming) its hole is reached a few launches after the latch comes on top, so the latch
// waits closed on top for a while (an introduction 1–3 launches, Normal 0–3, Hard 1–5, Super Hard 2–8; the pair nearest
// the middle of that band, a little noise); without such a pair the nearest. Refuses a set the cheap key check
// (rules.cjs latchKeysBlocked) finds unreachable; up to 12 draws. Returns {latches:[{column, depth, key}]} sorted by
// column, or {error}.
// Key spots carved out of the picture (17 §8 «Засов», the owner's amendment of 2026-11-03: «чтобы ставить где-то 3 ключа,
// мы спокойно можем убирать какие-то 4 кубика и вместо них ставить ключ в пустое пространство… в 3 разных местах
// картинки»): when the picture has fewer holes than `need` keys, 2×2 blocks of colour cells (the level's overlay.keys,
// picture coordinates; the approved picture stays as it is) become empty, each enclosed by colour all round, in the middle
// band of the access depth (opens partway, never on the start front), only out of colours of 30+ cells, spread over the
// picture (at least 5 cells from another spot or hole). Returns the overlay with its keys (walls and sites kept), the
// overlay unchanged (null when none) when the picture has holes enough, or false when no such blocks can be found.
// avoid: picture cells [x, y] the level already took for something else (a water channel's spring and exits, levels.mjs
// placeChannel): never a key hole, and a carved spot keeps 5 cells from each like from another spot.
export function carveKeySpots(pic,overlay,need,r=Math.random,{avoid=[]}={}){if(need<=0)return overlay;
 const skipOf=b=>new Set(avoid.map(([x,y])=>(b.h-1-(y+2))*b.w+x+2));
 const base=boardOf(pic,overlay),holes=keyHoles(base,skipOf(base));if(holes.length>=need)return overlay;
 const p=overlaid(pic,overlay),rows=p.rows,pw=rows[0].length,ph=rows.length,vol=measure(p).volumes,{d}=depths(base,-1),w=base.w,h=base.h;
 const di=(x,y)=>d[(h-1-(y+2))*w+x+2],cell=(x,y)=>x>=0&&y>=0&&x<pw&&y<ph?rows[y][x]:'.';
 let maxD=0;for(let y=0;y<ph;y++)for(let x=0;x<pw;x++)if(CODES.includes(rows[y][x])&&di(x,y)<Infinity)maxD=Math.max(maxD,di(x,y));
 const lo=Math.max(2,Math.floor(maxD*.3)),hi=Math.max(lo,Math.ceil(maxD*.85));
 const taken=holes.map(hh=>{const i=hh.key,bx=i%w,by=h-1-Math.floor(i/w);return [bx-2,by-2];});
 const opts=[];for(let y=1;y+2<ph;y++)for(let x=1;x+2<pw;x++){let ok=true,dsum=0;
  for(let yy=y;yy<y+2&&ok;yy++)for(let xx=x;xx<x+2&&ok;xx++){const c=cell(xx,yy),dd=di(xx,yy);if(!CODES.includes(c)||!((vol[c]||0)>=30)||!(dd>=lo&&dd<=hi))ok=false;else dsum+=dd;}
  // the ring round the block is colour all round, so the spot is a hole of its own (never open to the start front)
  for(let yy=y-1;yy<=y+2&&ok;yy++)for(let xx=x-1;xx<=x+2&&ok;xx++){if(yy>=y&&yy<y+2&&xx>=x&&xx<x+2)continue;if(!CODES.includes(cell(xx,yy)))ok=false;}
  if(ok)opts.push({x,y,depth:dsum/4});}
 const keys=[...((overlay&&overlay.keys)||[])],spots=[...taken,...avoid.map(([x,y])=>[x,y])];
 for(let k=holes.length;k<need;k++){const dist=o=>spots.length?Math.min(...spots.map(([sx,sy])=>Math.max(Math.abs(o.x+.5-sx),Math.abs(o.y+.5-sy)))):99;
  const cand=opts.filter(o=>dist(o)>=5);if(!cand.length)return false;
  const mid=(lo+hi)/2,best=cand.map(o=>({o,sc:Math.min(dist(o),14)-Math.abs(o.depth-mid)*.6+r()*1.5})).sort((a,b)=>b.sc-a.sc)[0].o;
  for(let yy=best.y;yy<best.y+2;yy++)for(let xx=best.x;xx<best.x+2;xx++)keys.push([xx,yy]);spots.push([best.x+.5,best.y+.5]);}
 const out={walls:(overlay&&overlay.walls)||[],sites:(overlay&&overlay.sites)||[],keys};
 const made=boardOf(pic,out);return keyHoles(made,skipOf(made)).length>=need?out:false;}
export const LATCH_WAIT={intro:[1,3],Normal:[0,3],Hard:[1,5],'Super Hard':[2,8]};
export function pickLatches(board,tray,pods,want,cls,r,{intro=false}={}){const holes=keyHoles(board);
 if(holes.length<want)return {error:holes.length?`мест для ключа ${holes.length} — меньше ${want} засовов`:'нет пустой клетки для ключа, закрытой работой (засов)'};
 const [lo,hi]=LATCH_DEPTH[cls],band=LATCH_WAIT[intro?'intro':cls],mid=(band[0]+band[1])/2,R=rulesSync(),tm=latchTiming(board,tray,pods,holes);
 for(let attempt=0;attempt<12;attempt++){const used=new Set(),usedCols=new Set(),out=[];
  for(let j=0;j<want;j++){const opts=[];
   for(let c=0;c<tray.length;c++){if(usedCols.has(c))continue;const len=tray[c].length,dmin=Math.max(lo,len-LATCH_BEHIND.want[1],len-LATCH_BEHIND.max),dmax=Math.min(hi,len-LATCH_BEHIND.want[0]);
    for(let dep=dmin;dep<=dmax;dep++){if(tray[c].slice(dep).some(p=>pods[p].mystery))continue;
     // a second latch of a three-column tray stands deeper than the first, so the two tend to block one after the other
     if(out.length&&tray.length<=3&&dep<Math.max(...out.map(l=>l.depth))+2)continue;
     holes.forEach((h,k)=>{if(used.has(h.key))return;const wait=tm.hole[k]-tm.top[c][dep],inside=wait>=band[0]&&wait<=band[1];
      opts.push({c,dep,k,score:(inside?0:10+Math.min(50,Math.abs(wait-mid)))+Math.abs(wait-mid)*.3+r()*(attempt?1.5:.6)});});}}
   if(!opts.length)break;opts.sort((a,b)=>a.score-b.score);const o=opts[0];used.add(holes[o.k].key);usedCols.add(o.c);out.push({column:o.c,depth:o.dep,key:holes[o.k].key});}
  if(out.length<want)continue;out.sort((a,b)=>a.column-b.column);
  const lv={w:board.w,h:board.h,rowsBottomFirst:board.rowsBottomFirst,entries:board.entries,pods,tray,latches:out,...(board.hero?{hero:board.hero}:{}),...objectsOf(board)};
  if(!R.latchKeysBlocked(lv).length)return {latches:out};}
 return {error:'засов: ключ недоступен (материал пути заперт за засовом) или не хватило колонок нужной глубины'};}

// ---- hidden cells (17 §8 «Скрытые клетки», the owner, 2026-11-01) ----
// `want` colour cells of the board, none on the start front (so each stays hidden after the start reveal and counts in the
// dose), none behind a bridge (the far bank: the bridge choice must not hang on a colour the player cannot know yet,
// §8) and no bridge anchor; in one to three compact patches grown from deep seeds, so the «?» reads as fog over a part of
// the picture. An introduction takes one patch round a seed of middle depth, so it reveals in the course of the round.
// Returns the sorted cell indices (rows bottom first, y*w+x) or null when the picture has too few such cells.
export function pickHiddenCells(board,want,r,{intro=false,avoid=null}={}){const {d,g}=depths(board,-1),w=board.w,n=g.length,anchors=new Set((board.hero?board.hero.choices:[]).map(c=>c.anchor));
 const ok=i=>CODES.includes(g[i])&&d[i]>=1&&d[i]<Infinity&&!anchors.has(i)&&!(avoid&&avoid.has(i)),nb=i=>{const x=i%w,out=[];if(i>=w)out.push(i-w);if(i+w<n)out.push(i+w);if(x>0)out.push(i-1);if(x<w-1)out.push(i+1);return out;};
 const pool=[];for(let i=0;i<n;i++)if(ok(i))pool.push(i);if(want<1||pool.length<want)return null;
 const maxD=Math.max(...pool.map(i=>d[i])),lo=Math.max(1,Math.floor(maxD*(intro?.3:.5))),hi=intro?Math.max(lo,Math.ceil(maxD*.6)):maxD;
 const patches=intro||want<=12?1:want<=40?2:3,chosen=new Set();
 for(let k=0;chosen.size<want&&k<patches+6;k++){const size=Math.min(want-chosen.size,k<patches-1?Math.round(want/patches):want);
  const free=i=>!chosen.has(i)&&!nb(i).some(j=>chosen.has(j));let seeds=pool.filter(i=>free(i)&&d[i]>=lo&&d[i]<=hi);if(!seeds.length)seeds=pool.filter(free);if(!seeds.length)seeds=pool.filter(i=>!chosen.has(i));if(!seeds.length)break;
  const patch=[seeds[Math.floor(r()*seeds.length)]];chosen.add(patch[0]);
  // grow: the frontier cell with the most patch neighbours first (a compact patch), ties at random
  while(patch.length<size){const front=[...new Set(patch.flatMap(nb))].filter(j=>ok(j)&&!chosen.has(j));if(!front.length)break;
   let best=null,bs=-1;for(const j of front){const sc=nb(j).filter(q=>chosen.has(q)).length+r()*.5;if(sc>bs){bs=sc;best=j;}}patch.push(best);chosen.add(best);}}
 return chosen.size>=want?[...chosen].slice(0,want).sort((a,b)=>a-b):null;}

// ---- the Drop water channel and the Twig wooden shutters on the board (17 §8, §9; the owner, 2026-11-05; README
// «Channel», «Shutters», «Channel and shutters in the builder») ----
// A board may carry `channel` ({spring, exits: [{cell, route, rideUs} × 2]}, board indices) and `shutters` ([{cells,
// material, count}]) in the engine's format (rules.cjs); a candidate copies them into its level (objectsOf).
export const objectsOf=b=>({...(b&&b.channel?{channel:b.channel}:{}),...(b&&b.shutters&&b.shutters.length?{shutters:b.shutters}:{})});
export const channelCells=b=>b&&b.channel?new Set([b.channel.spring,...b.channel.exits.map(e=>e.cell)]):null;
export const shutterCells=b=>b&&b.shutters?b.shutters.flatMap(s=>s.cells):[];
export function withObjects(board,{channel=board.channel,shutters=board.shutters}={}){const b={...board};delete b.channel;delete b.shutters;if(channel)b.channel=channel;if(shutters&&shutters.length)b.shutters=shutters;return b;}
// The channel's places (picture coordinates): the spring's access depth (colour cells to clear before it, depths()) in
// spring[0]–spring[1] of the deepest colour cell's (and at least 2: it sits behind work, the player clears an approach
// first, 17 §8); each exit at least `exit` of it and 3 deeper than the spring (inner regions far from the entry); the two
// exits at least apart × √(picture area) from each other and turned at least `angle` degrees from each other as seen from
// the spring (different parts of the picture, so the choice matters); channel cells at least `gap` apart (Chebyshev). The
// ride: stepMs a step of the route, at least minRideMs (README «Channel»).
export const CHANNEL={spring:[.08,.35],exit:.5,apart:.4,angle:80,gap:3,minDepth:6,stepMs:60,minRideMs:400};
export const rideUsOf=route=>Math.max(CHANNEL.minRideMs,CHANNEL.stepMs*(route.length-1))*1000;
// an orthogonal route from a to b (picture or board coordinates [x, y]): one leg along a row and one along a column, the
// column first when vertFirst; both ends included, no repeat
export function channelRoute([x0,y0],[x1,y1],vertFirst=false){const out=[[x0,y0]];let x=x0,y=y0;const sx=Math.sign(x1-x0),sy=Math.sign(y1-y0);
 const vert=()=>{while(y!==y1){y+=sy;out.push([x,y]);}},hor=()=>{while(x!==x1){x+=sx;out.push([x,y]);}};if(vertFirst){vert();hor();}else{hor();vert();}return out;}
// Places the water channel on a picture (with its overlay): the spring and two exits, each a colour cell enclosed by colour
// on its four sides that the level takes out (overlay.keys: a pocket of its own, so the start never reaches it and the cubes
// round an exit are its new front) or an empty pocket of the picture the start does not reach (its holes); the spring in
// its band, near the middle of it and of the picture's width (the entry's column), the exits the deepest, farthest apart
// pair, materials round them preferably different; each route two straight legs, the pair overlapping least. Deterministic
// by r. Returns {overlay (walls and sites kept, keys + the carved cells), at: {spring, exits, routes} (picture
// coordinates), depth: {spring, exits, max}} or null when the picture has no such places.
// A picture too thin for the strict pair (no two deep exits that far apart and turned that much) is tried once more with
// CHANNEL_RELAXED (the result carries relaxed: true).
export const CHANNEL_RELAXED={exit:.4,apart:.28,angle:55};
export function placeChannel(pic,overlay=null,r=Math.random,{relaxed=false}={}){const C={...CHANNEL,...(relaxed?CHANNEL_RELAXED:{})},p=overlaid(pic,overlay),rows=p.rows,pw=rows[0].length,ph=rows.length,base=boardOf(pic,overlay),{d}=depths(base,-1),w=base.w,h=base.h;
 const bi=(x,y)=>(h-1-(y+2))*w+x+2,cell=(x,y)=>x<0||y<0||x>=pw||y>=ph?'.':rows[y][x],col=(x,y)=>CODES.includes(cell(x,y)),g=base.rowsBottomFirst.join('');
 const start=new Uint8Array(w*h),q=[];for(const e of base.entries)if(g[e]==='.'){start[e]=1;q.push(e);}
 while(q.length){const i=q.pop(),x=i%w;for(const j of [i-w,i+w,x>0?i-1:-1,x<w-1?i+1:-1])if(j>=0&&j<w*h&&!start[j]&&g[j]==='.'){start[j]=1;q.push(j);}}
 const spots=[];let maxD=0;
 for(let y=0;y<ph;y++)for(let x=0;x<pw;x++){const dd=d[bi(x,y)];if(!(dd<Infinity))continue;if(col(x,y))maxD=Math.max(maxD,dd);
  if(col(x,y)&&col(x-1,y)&&col(x+1,y)&&col(x,y-1)&&col(x,y+1))spots.push({x,y,d:dd,carve:true});else if(cell(x,y)==='.'&&!start[bi(x,y)])spots.push({x,y,d:dd,carve:false});}
 if(maxD<C.minDepth)return null;
 const sLo=Math.max(2,Math.round(C.spring[0]*maxD)),sHi=Math.max(sLo+1,Math.round(C.spring[1]*maxD)),eLo=Math.max(sLo+3,Math.round(C.exit*maxD));
 const ex=Math.floor((pw+4)/2)-2,aimS=(sLo+sHi)/2,springs=spots.filter(s=>s.d>=sLo&&s.d<=sHi).map(s=>({...s,sc:Math.abs(s.d-aimS)/Math.max(1,sHi-sLo)+.8*Math.abs(s.x-ex)/pw+.3*r()})).sort((a,b)=>a.sc-b.sc).slice(0,6);
 let exits=spots.filter(s=>s.d>=eLo);if(!springs.length||exits.length<2)return null;
 // (a big picture: a fixed sample of the exit places keeps the pair search quick)
 if(exits.length>320){const keep=exits.map(s=>({s,k:r()})).sort((a,b)=>a.k-b.k).slice(0,320).map(x=>x.s);exits=keep;}
 const scale=Math.sqrt(pw*ph),cheb=(a,b)=>Math.max(Math.abs(a.x-b.x),Math.abs(a.y-b.y)),ring=s=>{const m={};for(const [dx,dy] of [[1,0],[-1,0],[0,1],[0,-1]]){const c=cell(s.x+dx,s.y+dy);if(CODES.includes(c))m[c]=(m[c]||0)+1;}return Object.entries(m).sort((a,b)=>b[1]-a[1])[0]?.[0]||null;};
 let best=null;
 for(const s of springs){const es=exits.filter(e=>cheb(e,s)>=C.gap);
  for(let i=0;i<es.length;i++)for(let j=i+1;j<es.length;j++){const a=es[i],b=es[j],sep=Math.hypot(a.x-b.x,a.y-b.y);if(sep<C.apart*scale||cheb(a,b)<C.gap)continue;
   const va=[a.x-s.x,a.y-s.y],vb=[b.x-s.x,b.y-s.y],cos=(va[0]*vb[0]+va[1]*vb[1])/(Math.hypot(...va)*Math.hypot(...vb)||1);if(cos>Math.cos(C.angle*Math.PI/180))continue;
   const len=(Math.abs(va[0])+Math.abs(va[1])+Math.abs(vb[0])+Math.abs(vb[1]))/(pw+ph);
   const sc=-(a.d+b.d)/(2*maxD)-Math.min(1,sep/(.7*scale))-(ring(a)!==ring(b)?.3:0)+.15*len+s.sc*.3+.1*r();
   if(!best||sc<best.sc)best={sc,s,a,b};}}
 if(!best)return relaxed?null:placeChannel(pic,overlay,r,{relaxed:true});
 // the routes: of the four leg orders, the pair sharing fewest cells beyond the spring, never through the other exit
 const {s,a,b}=best,P=o=>[o.x,o.y];let rt=null;
 for(const va of [false,true])for(const vb of [false,true]){const ra=channelRoute(P(s),P(a),va),rb=channelRoute(P(s),P(b),vb),ka=new Set(ra.slice(1).map(c=>c+'')),shared=rb.slice(1).filter(c=>ka.has(c+'')).length;
  const through=ra.some(c=>c[0]===b.x&&c[1]===b.y)||rb.some(c=>c[0]===a.x&&c[1]===a.y)?100:0,sc=shared+through+(va===vb?0:.1);if(!rt||sc<rt.sc)rt={sc,ra,rb};}
 const carved=[s,a,b].filter(o=>o.carve).map(P),ov={walls:(overlay&&overlay.walls)||[],sites:(overlay&&overlay.sites)||[],keys:[...((overlay&&overlay.keys)||[]),...carved]};
 return {overlay:ov,at:{spring:P(s),exits:[P(a),P(b)],routes:[rt.ra,rt.rb]},depth:{spring:s.d,exits:[a.d,b.d],max:maxD},...(relaxed?{relaxed:true}:{})};}
// the channel of placeChannel's `at` on a board of that picture, in the engine's format (board indices, rideUs)
export function channelOf(board,at){const bi=([x,y])=>(board.h-1-(y+2))*board.w+x+2;
 return {spring:bi(at.spring),exits:at.exits.map((e,i)=>({cell:bi(e),route:at.routes[i].map(bi),rideUs:rideUsOf(at.routes[i])}))};}
// the picture cells of a channel (spring and exits; never a key's place, levels.mjs carveKeySpots avoid)
export const channelSpots=at=>at?[at.spring,...at.exits]:[];

// Wooden shutters (17 §8 «Twig Деревянные створки», the owner, 2026-11-05): the closed area of all shutters as a share of N
// (lead by class, an additional one 2–5 %; an introduction Normal's minimum, 17 §9), each a rectangle with sides 3–8; its
// counter a share of its material's work (the material's cells on the board) by class, a multiple of 5, at least 10, the
// material at least 40 units, an additional shutter and an introduction at the class's lower bound; at most `outMax` of
// the material's work outside every shutter (so it opens while that work goes on).
export const SHUTTER={area:{lead:[[.03,.08],[.05,.12],[.08,.15]],add:[[.02,.05],[.02,.05],[.02,.05]]},count:[[.2,.35],[.3,.5],[.4,.6]],minWork:40,minCount:10,step:5,side:[3,8],outMax:.85,depth:[.15,.8],gap:2};
// the counter values a material of W units admits for a class: multiples of 5 in the class range, at least 10
export function shutterCounts(cls,W){const [lo,hi]=SHUTTER.count[CLASSES.indexOf(cls)],out=[];if(W<SHUTTER.minWork)return out;
 for(let c=Math.max(SHUTTER.minCount,Math.ceil(lo*W/SHUTTER.step-1e-9)*SHUTTER.step);c<=hi*W+1e-9;c+=SHUTTER.step)out.push(c);return out;}
// the dose errors of a level's shutters for a class (dosesOk): each counter one of shutterCounts of its material's work
export function shutterCountErrors(level,cls){const g=level.rowsBottomFirst.join(''),errs=[];
 for(const [i,s] of (level.shutters||[]).entries()){const W=[...g].filter(c=>c===s.material).length,ok=shutterCounts(cls,W);
  if(W<SHUTTER.minWork)errs.push(`створки ${i+1}: работы материала ${s.material} ${W} < ${SHUTTER.minWork} (§8)`);
  else if(!ok.includes(s.count))errs.push(`створки ${i+1}: счётчик ${s.count} из ${W} ${s.material} вне ${ok.length?ok[0]+'–'+ok.at(-1):'—'} для ${cls} (§8: доля по классу, кратно 5, минимум 10)`);}
 return errs;}
// the access depth of every cell with `walls` (board indices) closed, as depths()
function depthsWith(board,walls){const {w,h,rowsBottomFirst:rows,entries}=board,g=rows.join(''),wall=new Uint8Array(w*h),d=new Float64Array(w*h).fill(Infinity),dq=[];for(const i of walls)wall[i]=1;
 for(const e of entries){d[e]=0;dq.push(e);}
 while(dq.length){const c=dq.shift(),x=c%w,y=Math.floor(c/w);for(const [dx,dy] of [[1,0],[-1,0],[0,1],[0,-1]]){const u=x+dx,v=y+dy;if(u<0||v<0||u>=w||v>=h)continue;const n=v*w+u;if(g[n]==='#'||g[n]==='b'||wall[n])continue;
  const cost=CODES.includes(g[c])?1:0,nd=d[c]+cost;if(nd<d[n]){d[n]=nd;cost?dq.push(n):dq.unshift(n);}}}
 return d;}
// Places `n` shutters on a board (17 §7, §8): each a full rectangle of colour cells (sides 3–8, aspect at most 2, the area
// of the dose), never an entry, a cell of `avoid` (board indices: a channel's cells and routes, accents), off the start front
// (every cell at access depth 1+), its mean depth near 45 % of the deepest colour cell's (in depth[0]–depth[1]), never
// closing another colour cell off (with the rectangle as a wall every colour cell outside stays reachable), gap 2 from
// another shutter; its material the one whose counter (shutterCounts: the lead its dose place pos in the range, an
// additional one and an introduction the lower bound) is at most outMax of its work outside every shutter, preferably a
// material scarce under the rectangle (so the player opens it working elsewhere). role: 'lead' | 'add'. Deterministic by r.
// Returns [{cells (sorted), material, count, rect: [x, y, w, h] (board, bottom first)}] or null.
export function placeShutters(board,{n=1,cls='Normal',role='lead',intro=false,pos=null,r=Math.random,avoid=null}={}){const {w,h}=board,g=board.rowsBottomFirst.join(''),{d}=depths(board,-1),ci=CLASSES.indexOf(intro?'Normal':cls),bad=new Set(avoid||[]);
 const isCol=i=>CODES.includes(g[i]),N=[...g].filter(c=>CODES.includes(c)).length,vol={};for(const c of g)if(CODES.includes(c))vol[c]=(vol[c]||0)+1;
 let maxD=0;for(let i=0;i<g.length;i++)if(isCol(i)&&d[i]<Infinity)maxD=Math.max(maxD,d[i]);if(maxD<4)return null;
 const [aLo,aHi]=SHUTTER.area[intro?'lead':role][ci],share=intro?aLo:aLo+(aHi-aLo)*(pos??r()),area=Math.max(9,share*N/n);
 const shapes=[];for(let a=SHUTTER.side[0];a<=SHUTTER.side[1];a++)for(let b=SHUTTER.side[0];b<=SHUTTER.side[1];b++)if(Math.max(a,b)<=2*Math.min(a,b))shapes.push([a,b,Math.abs(a*b-area)/area]);
 shapes.sort((u,v)=>u[2]-v[2]);const near=shapes.filter(s=>s[2]<=shapes[0][2]+.15).slice(0,4);
 const placed=[],under=new Set(),near2=(rc,o)=>rc[0]<o[0]+o[2]+SHUTTER.gap&&o[0]<rc[0]+rc[2]+SHUTTER.gap&&rc[1]<o[1]+o[3]+SHUTTER.gap&&o[1]<rc[1]+rc[3]+SHUTTER.gap;
 const [dLo,dHi]=SHUTTER.depth,aim=.45*maxD;
 for(let k=0;k<n;k++){const opts=[];
  for(const [sw,sh] of near)for(let y=2;y+sh<=h-2;y++)for(let x=2;x+sw<=w-2;x++){const rc=[x,y,sw,sh];if(placed.some(o=>near2(rc,o.rect)))continue;let ok=true,sum=0;
   for(let yy=y;yy<y+sh&&ok;yy++)for(let xx=x;xx<x+sw;xx++){const i=yy*w+xx;if(!isCol(i)||bad.has(i)||under.has(i)||board.entries.includes(i)||!(d[i]>=1&&d[i]<Infinity)){ok=false;break;}sum+=d[i];}
   if(!ok)continue;const md=sum/(sw*sh);if(md<dLo*maxD||md>dHi*maxD)continue;opts.push({rc,sc:Math.abs(md-aim)/maxD+.15*r()});}
  opts.sort((u,v)=>u.sc-v.sc);let pick=null;
  for(const o of opts.slice(0,60)){const [x,y,sw,sh]=o.rc,cells=[];for(let yy=y;yy<y+sh;yy++)for(let xx=x;xx<x+sw;xx++)cells.push(yy*w+xx);
   const all=[...under,...cells],dw=depthsWith(board,all),inside=new Set(all);let closes=false;for(let i=0;i<g.length;i++)if(isCol(i)&&!inside.has(i)&&d[i]<Infinity&&!(dw[i]<Infinity)){closes=true;break;}if(closes)continue;
   // the material and its counter
   const inRect={};for(const i of cells)inRect[g[i]]=(inRect[g[i]]||0)+1;let mat=null;
   for(const [m,W] of Object.entries(vol)){const counts=shutterCounts(intro?'Normal':cls,W);if(!counts.length)continue;const out=W-all.filter(i=>g[i]===m).length,t=intro||role==='add'?0:(pos??r());
    const want=counts[Math.round(t*(counts.length-1))],c=[...counts].sort((u,v)=>Math.abs(u-want)-Math.abs(v-want)).find(c=>c<=SHUTTER.outMax*out);if(c==null)continue;
    const sc=(inRect[m]||0)/cells.length+.5*(placed.some(s=>s.material===m)?1:0)-.2*Math.min(1,W/(N/4))+.1*r();if(!mat||sc<mat.sc)mat={m,c,sc};}
   if(!mat)continue;pick={cells:cells.sort((u,v)=>u-v),material:mat.m,count:mat.c,rect:o.rc};break;}
  if(!pick)return null;placed.push(pick);for(const i of pick.cells)under.add(i);}
 return placed;}
// The same shutters on a board whose materials changed a little (an accent painted over a counted material, 17 §6): each
// counter kept when shutterCounts of the material's work there still holds it, else the nearest that does (at most outMax
// of the work outside every shutter); null when one has none.
export function refitShutters(board,shutters,cls){const g=board.rowsBottomFirst.join(''),under=new Set(shutters.flatMap(s=>s.cells));
 const out=[];for(const s of shutters){const W=[...g].filter(c=>c===s.material).length,outside=W-[...under].filter(i=>g[i]===s.material).length;
  const c=shutterCounts(cls,W).filter(c=>c<=SHUTTER.outMax*outside).sort((a,b)=>Math.abs(a-s.count)-Math.abs(b-s.count))[0];if(c==null)return null;out.push({...s,count:c});}
 return out;}
// the shutters in the engine's format (a level's `shutters`)
export const shuttersOf=list=>list.map(s=>({cells:s.cells.slice(),material:s.material,count:s.count}));

// ---- accents for pressure on the places (17 §6 «Акценты для давления на места», §3; the owner, 2026-11-01) ----
// Colony Flow's main trick on Hard and Super Hard (sometimes Normal): a few small elements of the picture — 2–4 cubes of
// stars, decorations, details in the middle or on the background — in a material of their own whose pods come early or
// mid queue, so the player parks them and plays the rest for a while. The builder paints them as the level's recolouring
// (definition.recolor, picture coordinates); the approved picture stays as it is.
// The cluster shapes (2–4 cells, 4-connected), offsets [dx, dy]
const ACCENT_SHAPES=[[[0,0],[1,0]],[[0,0],[0,1]],[[0,0],[1,0],[2,0]],[[0,0],[0,1],[0,2]],[[0,0],[1,0],[0,1]],[[0,0],[1,0],[1,1]],[[0,0],[0,1],[1,1]],[[1,0],[0,1],[1,1]],
 [[0,0],[1,0],[0,1],[1,1]],[[0,0],[1,0],[2,0],[1,1]],[[1,0],[0,1],[1,1],[2,1]],[[0,0],[0,1],[0,2],[1,1]],[[1,0],[1,1],[1,2],[0,1]]];
// an accent sits in an area of one colour of at least this many cells (so it reads as a decoration of that area), at
// least this access depth share of the deepest colour cell (and 2), with at least this CIEDE2000 to its ground
export const ACCENT={area:30,depthShare:.3,minDepth:2,minDE:10,gap:3};
// what reads as a decoration on a ground (the owner's examples: Солнце / Снег stars on a dark sky, Фуксия / Лаванда
// flowers on green): stars and snow on a dark or blue ground, flowers on green, dark or saturated dots on a light one,
// a bright spot elsewhere; 0–1
export function decorScore(ground,a){const [L,A,B]=labOf(ground),h=(Math.atan2(B,A)*180/Math.PI+360)%360,ch=Math.hypot(A,B),La=labOf(a)[0];
 if(L<42)return ({Y:1,W:1}[a])??(La>75?.5:0);
 if(ch>18&&h>=95&&h<=180)return ({F:1,H:1,K:.8,P:.8,Y:.7,R:.6,W:.4}[a])??0;
 if(ch>15&&h>=190&&h<=290)return ({W:1,Y:.9,C:.6}[a])??(La>80?.4:0);
 if(L>80)return ({R:.8,F:.8,N:.6,V:.6,E:.5}[a])??0;
 return ({Y:.7,W:.7,F:.6,R:.5}[a])??0;}
// Picks `materials` (1–2) accent materials absent from the picture (with its overlay) and paints each in 1–maxClusters
// clusters of 2–4 cells: every cluster cell and its eight neighbours in one colour area of ACCENT.area+ cells (so the
// silhouette never changes and the accent reads as a detail of that area), deep (access depth ≥ ACCENT.minDepth and
// ACCENT.depthShare of the deepest colour cell), at least ACCENT.gap cells (Chebyshev) from any other accent cell, the
// clusters of one material spread apart. One material is painted over one ground colour (so the card's comparison can
// put it back), its CIEDE2000 to every neighbour at least ACCENT.minDE (17 §3). Deterministic by r; returns
// {recolor: [[x, y, code], …] (reading order), accents: [{code, from, cells, clusters: [[[x, y], …], …]}]} or null.
export function pickAccents(pic,overlay,r,{materials=1,maxClusters=3}={}){const p=overlaid(pic,overlay),rows=p.rows,pw=rows[0].length,ph=rows.length,board=boardOf(pic,overlay),{d}=depths(board,null);
 const at=(x,y)=>x<0||y<0||x>=pw||y>=ph?'.':rows[y][x],dep=(x,y)=>d[(board.h-1-(y+2))*board.w+x+2],present=new Set([...rows.join('')].filter(c=>CODES.includes(c)));
 // one-colour areas (4-connected) and their sizes
 const comp=new Int32Array(pw*ph).fill(-1),size=[];
 for(let y0=0;y0<ph;y0++)for(let x0=0;x0<pw;x0++){if(comp[y0*pw+x0]>=0||!CODES.includes(at(x0,y0)))continue;const k=size.length,c=at(x0,y0),q=[[x0,y0]];comp[y0*pw+x0]=k;let n=0;
  while(q.length){const [x,y]=q.pop();n++;for(const [dx,dy] of [[1,0],[-1,0],[0,1],[0,-1]]){const u=x+dx,v=y+dy;if(at(u,v)===c&&comp[v*pw+u]<0){comp[v*pw+u]=k;q.push([u,v]);}}}size.push(n);}
 let maxD=0;for(let y=0;y<ph;y++)for(let x=0;x<pw;x++)if(CODES.includes(at(x,y))&&dep(x,y)<Infinity)maxD=Math.max(maxD,dep(x,y));
 const dmin=Math.max(ACCENT.minDepth,Math.round(ACCENT.depthShare*maxD));
 // every placement of every shape that fits, by ground colour
 const spots={};
 for(let y=0;y<ph;y++)for(let x=0;x<pw;x++){const g=at(x,y);if(!CODES.includes(g))continue;
  for(const sh of ACCENT_SHAPES){const cells=sh.map(([dx,dy])=>[x+dx,y+dy]),inside=new Set(cells.map(([u,v])=>u+','+v));
   if(!cells.every(([u,v])=>at(u,v)===g&&comp[v*pw+u]===comp[y*pw+x]&&dep(u,v)>=dmin&&dep(u,v)<Infinity))continue;if(size[comp[y*pw+x]]<ACCENT.area)continue;
   let ring=true;for(const [u,v] of cells){for(let dy=-1;dy<=1&&ring;dy++)for(let dx=-1;dx<=1;dx++){if(inside.has((u+dx)+','+(v+dy)))continue;if(at(u+dx,v+dy)!==g){ring=false;break;}}if(!ring)break;}
   if(ring)(spots[g]=spots[g]||[]).push(cells);}}
 // the materials: absent from the picture, readable on the ground, ranked by how much they look like a decoration there
 const opts=[];for(const a of CODES)if(!present.has(a))for(const g of Object.keys(spots)){const de=deltaE(g,a);if(de<ACCENT.minDE)continue;opts.push({a,g,score:25*decorScore(g,a)+Math.min(de,50)+4*r()});}
 opts.sort((x,y)=>y.score-x.score);
 const taken=[],out=[],far=(cells)=>Math.min(Infinity,...taken.flatMap(([u,v])=>cells.map(([x,y])=>Math.max(Math.abs(x-u),Math.abs(y-v)))));
 for(const o of opts){if(out.length>=materials)break;if(out.some(m=>m.code===o.a))continue;
  const want=1+Math.floor(r()*maxClusters),clusters=[];
  for(let k=0;k<want;k++){const len=2+Math.floor(r()*3),free=spots[o.g].filter(c=>far(c)>=ACCENT.gap);if(!free.length)break;
   // spread: the first cluster anywhere, the next ones among the farthest from what is painted
   const mine=clusters.flat(),dist=c=>mine.length?Math.min(...mine.flatMap(([u,v])=>c.map(([x,y])=>Math.max(Math.abs(x-u),Math.abs(y-v))))):0;
   let pool=free.filter(c=>c.length===len);if(!pool.length)pool=free;if(mine.length){const top=Math.max(...pool.map(dist));pool=pool.filter(c=>dist(c)>=.6*top);}
   const c=pool[Math.floor(r()*pool.length)];clusters.push(c);taken.push(...c);}
  if(!clusters.length)continue;
  // readable next to every neighbour (17 §3: CIEDE2000 ≥ 10), the other accents included
  const cells=clusters.flat(),inside=new Set(cells.map(([x,y])=>x+','+y));let readable=true;
  for(const [x,y] of cells)for(const [dx,dy] of [[1,0],[-1,0],[0,1],[0,-1],[1,1],[1,-1],[-1,1],[-1,-1]]){const n=at(x+dx,y+dy);if(inside.has((x+dx)+','+(y+dy))||!CODES.includes(n))continue;if(deltaE(n,o.a)<ACCENT.minDE)readable=false;}
  if(!readable){taken.splice(taken.length-cells.length,cells.length);continue;}
  out.push({code:o.a,from:o.g,cells:cells.length,clusters});}
 if(!out.length)return null;
 const recolor=out.flatMap(m=>m.clusters.flat().map(([x,y])=>[x,y,m.code])).sort((a,b)=>a[1]-b[1]||a[0]-b[0]);
 return {recolor,accents:out};}
// the pods of the accent materials: the quota is the accent's volume (always below 20 cells, so 1–2 pods, 17 §3); a
// volume of 4+ is split into two pods in about half the candidates. Returns the pods (the other materials' as given)
export function accentPods(pods,accents,r){const acc=new Set(accents.map(a=>a.code)),out=[];
 for(const p of pods){if(!acc.has(p.color)){out.push(p);continue;}const v=pods.filter(q=>q.color===p.color).reduce((s,q)=>s+q.count,0);if(out.some(q=>q.color===p.color))continue;
  for(const n of v>=4&&r()<.5?even(v,2):[v])out.push({color:p.color,count:n});}
 return out;}
// The same level without its accents (the card's comparison, «стоят без работы» with / without): the accent cubes back in
// the colour they were painted over, each accent pod playing that colour; the same queue, the same places, the same
// mechanics. null when the level has no recolouring or one of its materials is not an accent (also on the picture, or
// painted over more than one colour, so a pod could not tell which).
export function accentTwin(level,pic){const rc=level.recolor;if(!rc||!rc.length)return null;const from={},present=new Set([...pic.rows.join('')].filter(c=>CODES.includes(c)));
 for(const [x,y,c] of rc){const was=pic.rows[y][x];if(present.has(c)||(from[c]&&from[c]!==was))return null;from[c]=was;}
 const g=level.rowsBottomFirst.map(r=>[...r]);for(const [x,y] of rc)g[level.h-1-(y+2)][x+2]=pic.rows[y][x];
 const {recolor:_,...rest}=level;return {...rest,rowsBottomFirst:g.map(r=>r.join('')),pods:level.pods.map(p=>from[p.color]?{...p,color:from[p.color]}:p)};}
// the tray row (0 = the top) of each pod of a level
const rowOfPod=(level,i)=>{for(const st of level.tray){const k=st.indexOf(i);if(k>=0)return k;}return -1;};

// ---- the checks of one candidate level (17 §5–§9, §13 layer B), shared by build and the bridge scene search ----
// Returns {reason} for the first failed check, else the measured parts of a bank entry: random, solution, fairness,
// strategies, rhythm, bridge, checks. kind (the wave plan's bridgeKind, single | better) asks for that bridge kind and
// tests it early (the two solver runs come before the random test). A bridge level is 'first' (an introduction: both
// win, one 10+ points better, 90 %+), 'single' (exactly one bridge wins, proven for the other, and the loss with the
// other comes later through materials and places, not at the choice: bridgeLoss), 'better' (not an introduction: both
// win, one 10+ points better by the random test) or 'any' (single or better). The owner, 2026-10-28 (second decision):
// Normal any, Hard half single and half better, Super Hard single.
// channelKind 'single' (the wave plan's channelKind of a hand Super Hard with a water channel, the owner, 2026-11-05): the
// channel must decide (exactly one exit wins, measure.mjs channelExits), and the class is measured with that exit only, as a
// bridge level's with its best bridge (17 §5): the quick estimate, the random test and the calm test keep the winning exit,
// the test over both exits stays as randomAll; checks.channelKind records it. Without it nothing changes.
export async function evaluate(level,{m,cls,intro=false,fixed=null,kind=null,channelKind=null,breather=false,tick=()=>{}}){const W=m.N,recipe=level.recipe||'none';
 const errs=checkPods(level.pods,m.volumes);if(errs.length)return {reason:'баланс'};
 {const {R}=await engine(),ve=R.validate(level);if(ve&&ve.length)return {reason:'движок отклоняет: '+ve[0]};}
 // hidden cells (17 §8, §9): never with hidden pods; an introduction has 5–10 cells still hidden after the start reveal
 if(!recipeAllowed(recipe))return {reason:'скрытые поды и скрытые клетки на одном уровне (§8)'};
 if(intro&&recipe==='cells'){const k=cellsHiddenAtStart(level);if(k<5||k>10)return {reason:`знакомство: скрытых клеток после стартового раскрытия ${k}, нужно 5–10 (§9)`};}
 // latch + key (17 §9): an introduction has exactly one latch with its key
 if(intro&&recipe==='latch'&&(level.latches||[]).length!==1)return {reason:`знакомство: засовов ${(level.latches||[]).length}, нужен один с ключом (§9)`};
 if(level.hero){const {R}=await engine();if(R.bridgeChoices(R.create(level,level.capacity||5)).length)return {reason:'мост доступен сразу — нужен расчищенный подход (§8)'};}
 // the water channel (17 §8): its spring sits behind work, the player clears an approach first (never ready at the start)
 if(level.channel){const {R}=await engine();if(R.channelChoices(R.create(level,level.capacity||5)).length)return {reason:'канал: родник доступен сразу — нужен расчищенный подход (§8)'};}
 // wooden shutters (17 §9): an introduction has exactly one
 if(intro&&recipe==='shutters'&&(level.shutters||[]).length!==1)return {reason:`знакомство: створок ${(level.shutters||[]).length}, нужны одни (§9)`};
 // a breather (17 §5 «Передышка», the owner, 2026-11-02): the mandatory checks only
 if(breather)return evaluateBreather(level,{m,tick});
 // a deciding-exit channel (channelKind single, 17 §5, the owner, 2026-11-05): each exit solved alone first; exactly one wins
 let ch=null,exit=null;if(channelKind!==null){if(channelKind!=='single')throw Error('channelKind: только single');if(!level.channel)return {reason:'канал: план single, а на уровне нет канала'};if(cls!=='Super Hard')return {reason:'канал: single только на Super Hard (§5)'};
  ch=await channelExits(level);tick('выходы канала '+JSON.stringify(ch.each));if(!ch.decides)return {reason:'канал: решает не ровно один выход (single)'};exit=winningExit(ch);}
 // a bridge introduction may be the second one, where one bridge only wins: its target is the Normal zone (§9)
 // a bridge level's class is its best bridge's (17 §5, the owner, 2026-10-30): the quick estimate takes the better bridge
 const q=level.hero?Math.max(...await Promise.all(level.hero.choices.map((_,i)=>quickShare(level,40,11,i)))):exit!==null?await quickShare(level,40,11,null,exit):await quickShare(level,40);tick('быстрая оценка '+q);const target=intro&&recipe==='bridge'?[.5,1]:intro?[.9,1]:fixed===1?[1,1]:fixed?[.9,1]:{Normal:[.5,.75],Hard:[.2,.5],'Super Hard':[.05,.2]}[cls];
 if(q<target[0]-.15||q>target[1]+.15)return {reason:'не тот класс по быстрой оценке',quick:q};
 // the planned bridge kind first: each bridge alone solved (17 §8)
 let win=null,early=null;if(kind&&level.hero){win=(await bridgeChoicesWin(level)).each;const t=win.filter(x=>x===true).length,f=win.filter(x=>x===false).length;tick('мосты '+JSON.stringify(win));
  const one=t===1&&f===win.length-1;
  if(kind==='single'&&!one)return {reason:'мост: решает не ровно один (план single)'};
  if(kind==='better'&&t!==win.length)return {reason:'мост: выигрывают не оба (план better)'};
  if(kind==='any'&&!one&&t!==win.length)return {reason:'мост: ни ровно один, ни оба (план any)'};
  // the owner, 2026-10-29: on a Super Hard the wrong bridge may lose right at the choice (17 §8)
  if((kind==='single'||(kind==='any'&&one))&&cls!=='Super Hard'){early=await bridgeLoss(level,win.indexOf(false));if(!early.ok)return {reason:'мост: с другим мостом проигрыш сразу при выборе (§8)'};}}
 // §5: a Hard or Super Hard that the greedy strategies win is not that class, whatever the random test says
 const gr=cls==='Normal'?null:await greedy(level);if(gr&&!greedyBeaten(gr,cls))return {reason:'жадная стратегия выигрывает (§5)'};
 // §5: at most 35 % of the decisions may offer a single launch
 const forced=await forcedShare(level);if(!fixed&&forced>.35)return {reason:'вынужденных решений больше 35 % (§5)'};
 const sol=await solve(level);tick('решение '+sol.win);if(sol.win!==true||sol.replay!=='won')return {reason:sol.win===null?'решение не найдено за бюджет (не проверено)':'нет решения'};
 // the water channel (17 §8, §9): each exit solved alone and its random share (the class test itself takes the exit as one
 // of the bot's uniform actions, §5); on both introductions each exit keeps a winning continuation
 if(!ch&&level.channel){ch=await channelExits(level);tick('выходы канала '+JSON.stringify(ch.each));}
 if(ch&&intro&&recipe==='canal'&&!ch.each.every(x=>x.win===true))return {reason:'знакомство с каналом: не каждый выход ведёт к победе (§9)'};
 // wooden shutters (17 §8): every shutter opens on the saved solution before 80 % W
 const sc=level.shutters?await shutterCheck(level,sol.route):null;if(sc)tick('створки '+JSON.stringify(sc.shutters.map(x=>x.atW)));if(sc&&!sc.ok)return {reason:'створки: '+sc.problems[0]};
 // §5 for a bridge level (the owner, 2026-10-30): the class is measured with the best bridge, the one that alone wins or,
 // when both win, the one the random games win more often; the test over both bridges stays as randomAll
 let best=null,bc0=null,rtAll=null;if(level.hero){win=win||(await bridgeChoicesWin(level)).each;const t=win.filter(x=>x===true).length;
  if(t===1)best=win.indexOf(true);else{bc0=await bridgeChoices(level,60,5,win);best=bc0.better;}}
 // a deciding-exit channel (channelKind single): with the winning exit only, the test over both exits as randomAll
 const only={...(best===null?{}:{bridge:best}),...(exit===null?{}:{exit})};
 const rt=await randomTest(level,only);tick('случайный тест '+rt.share+(best===null?'':` (мост ${best})`)+(exit===null?'':` (выход ${exit})`));if(rt.cls==='too hard')return {reason:'ниже 5 %'};
 if(best!==null){rtAll=await randomTest(level);tick('случайный тест с обоими мостами '+rtAll.share);}
 if(exit!==null){rtAll=await randomTest(level);tick('случайный тест с обоими выходами '+rtAll.share);}
 if(rt.cls!=='Normal'){const g2=gr||await greedy(level);if(!greedyBeaten(g2,rt.cls))return {reason:'жадная стратегия выигрывает измеренный класс (§5)'};}
 // §5 "opening + colours" (the owner's second amendment of 2026-10-27): no first launch then greedy wins, ≤ 35 % after three
 const opening=rt.cls==='Normal'?null:await openingTest(level,rt.cls);tick('дебют '+JSON.stringify(opening));if(opening&&!opening.ok)return {reason:opening.first.wins?'дебют + цвета: выигрывает после первого запуска (§5)':'дебют + цвета: больше 35 % после трёх запусков (§5)'};
 // §5 the calm test (the owner's amendment of 2026-10-31): a Hard or Super Hard stays hard when the board settles before
 // every tap (only the order of choices counts); a Normal's calm share is kept for the placer, which holds a regular
 // Normal to 50 % and more
 const calm=await calmTest(level,only);tick('спокойно '+calm.share);{const cf=calmFails(rt.cls,calm);if(cf&&!intro)return {reason:cf};}
 // §8 trap links (2026-10-31): a Hard or Super Hard with links has a group with a member whose material is hidden at the
 // start, so the group holds two places long
 const traps=trapLinks(level),trapN=traps.filter(x=>x.trap).length;
 if(!intro&&(rt.cls==='Hard'||rt.cls==='Super Hard')&&traps.length&&!trapN)return {reason:'нет связки-ловушки: у каждой связки материалы открыты с самого начала (§8)'};
 if(intro&&recipe!=='bridge'&&rt.share<.9)return {reason:'знакомство ниже 90 %'};
 const dErr=dosesOk(level,rt.cls);if(dErr&&!intro)return {reason:'дозы не подходят фактическому классу: '+dErr};
 // latch + key (17 §8): the depth for the measured class, the pods behind, every latch open on the saved solution before
 // 80 % W, at most the allowed columns blocked at once on it
 const lc=level.latches?await latchCheck(level,sol.route,rt.cls):null;if(lc)tick('засовы '+JSON.stringify({maxBlocked:lc.maxBlocked,last:lc.lastOpenAtW}));if(lc&&!lc.ok)return {reason:'засов: '+lc.problems[0]};
 // hidden pods and hidden cells are separate sources of the unknown (17 §8), each with its own fairness check
 const fair=level.pods.some(p=>p.mystery)?await fairness(level,sol.route):level.hiddenCells?await cellFairness(level,sol.route):{fair:true,checked:0};if(!fair.fair)return {reason:level.hiddenCells?'честность скрытых клеток не подтверждена':'честность скрытых не подтверждена'};
 // §8: on a hidden-pod introduction one hidden pod matters (reveals early with a new colour and is launched soon)
 const hm=level.pods.some(p=>p.mystery)?await hiddenMatters(level,sol.route):null;if(intro&&recipe==='hidden'&&!hm.ok)return {reason:'скрытый под не влияет на выбор (§8)'};
 // a bridge level measures each bridge (§8, §9): the first introduction needs both to win and one clearly better; the
 // second introduction and three quarters of the later bridge levels exactly one winning bridge, the rest both winning
 // with one 10+ points better (the wave plan's bridgeKind decides which a position takes)
 const bc=level.hero?bc0||await bridgeChoices(level,60,5,win):null,bs=level.hero?await bridgeSingle(level,60,3,win||bc.each.map(x=>x.win)):null;let bridgeKind=null,bl=null;
 if(bc){const both=bc.each.every(x=>x.win===true)&&bc.gap>=.1;
  if(intro&&both&&rt.share>=.9)bridgeKind='first';else if(bs.ok)bridgeKind='single';else if(!intro&&both)bridgeKind='better';
  else return {reason:intro?'мост: ни «оба выигрывают, один лучше на 10 пунктов при 90 %+», ни «решает ровно один мост» (§8, §9)':'мост: ни «решает ровно один мост», ни «оба выигрывают, один лучше на 10 пунктов» (§8)'};
  if(kind&&kind!=='any'&&bridgeKind!==kind)return {reason:`мост: вид ${bridgeKind}, по плану ${kind} (§8)`};
  // §8: with the other bridge the loss comes later, through materials and places, not at the choice
  if(bridgeKind==='single'){bl=early||await bridgeLoss(level,bs.each.indexOf(false));tick('проигрыш с другим мостом '+JSON.stringify(bl));if(!bl.ok&&cls!=='Super Hard'&&rt.cls!=='Super Hard')return {reason:'мост: с другим мостом проигрыш сразу при выборе (§8)'};}}
 const st=await strategies(level),rh=rhythm(sol.trace,W),greedyLoses=!st['most-now|left']&&!st['most-now|right'],hold=await holdPod(level);
 return {random:rt,...(rtAll?{randomAll:rtAll}:{}),solution:{route:sol.route,forks:sol.decisions.forks,singleSafe:sol.decisions.single,unknown:sol.decisions.unknown,fast:sol.fast},fairness:fair,strategies:st,rhythm:rh,...(bc?{bridge:bc}:{}),
  checks:{forced,opening,hold,greedyLoses,hiddenMatters:hm,bridgeKind,bridgeSingle:bs,...(bl?{bridgeLoss:bl}:{}),pressure:await pressure(level,QUALITY.pressure.games),calm,traps:{links:traps.length,traps:trapN,top:traps.filter(x=>x.trap&&x.row<=2).length},...(lc?{latch:lc}:{}),...(ch?{channel:ch}:{}),...(exit!==null?{channelKind:'single'}:{}),...(sc?{shutters:sc}:{})}};}

// A breather (17 §5 «Передышка», the owner, 2026-11-02: «некоторые играются как "передышка", особенно те что < 500–550
// клеток»): a Normal on a picture up to ~550 cells (BREATHER_N) or a position the wave plan marks `breather`. Zero or one
// light mechanic (hidden pods or hidden cells, or one plain link pair: wave.mjs LIGHT), its zone 75–90 % (the owner,
// 2026-11-02; was 75–95 %), and only the mandatory checks of layer A: the engine's validation and
// the exact balance (evaluate's start), the saved solution, the doses, the class by the random test (Normal), the §8
// rules of a latch on the solution and the fairness of hidden information. No calm test, opening test, «по цветам»,
// holding pod, pressure or accents; forced decisions are recorded, not required. Not in the Normal median nor the
// «по цветам» quota (levels.mjs place, audit.mjs).
export const BREATHER_N=550;
async function evaluateBreather(level,{m,tick=()=>{}}){const W=m.N,types=(level.recipe||'none')==='none'?[]:level.recipe.split('+');
 if(types.length>1)return {reason:'передышка: не больше одной механики (§5)'};if(!breatherRecipeOk(level.recipe||'none'))return {reason:`передышка: механика ${level.recipe} не лёгкая (§5: скрытые поды, скрытые клетки или одна связка)`};
 const traps=trapLinks(level),trapN=traps.filter(x=>x.trap).length;if(trapN)return {reason:'передышка: связка-ловушка (§5)'};
 const q=await quickShare(level,40);tick('быстрая оценка '+q);if(q<.6)return {reason:'не тот класс по быстрой оценке',quick:q};
 const sol=await solve(level);tick('решение '+sol.win);if(sol.win!==true||sol.replay!=='won')return {reason:sol.win===null?'решение не найдено за бюджет (не проверено)':'нет решения'};
 const rt=await randomTest(level);tick('случайный тест '+rt.share);if(rt.cls!=='Normal')return {reason:'передышка: случайный тест ниже 50 % (§5)'};
 const dErr=dosesOk(level,'Normal',{breather:true});if(dErr)return {reason:'дозы не подходят: '+dErr};
 const lc=level.latches?await latchCheck(level,sol.route,'Normal'):null;if(lc&&!lc.ok)return {reason:'засов: '+lc.problems[0]};
 const fair=level.pods.some(p=>p.mystery)?await fairness(level,sol.route):level.hiddenCells?await cellFairness(level,sol.route):{fair:true,checked:0};if(!fair.fair)return {reason:level.hiddenCells?'честность скрытых клеток не подтверждена':'честность скрытых не подтверждена'};
 return {random:rt,solution:{route:sol.route,forks:sol.decisions.forks,singleSafe:sol.decisions.single,unknown:sol.decisions.unknown,fast:sol.fast},fairness:fair,strategies:{},rhythm:rhythm(sol.trace,W),
  checks:{breather:true,forced:await forcedShare(level),opening:null,hold:null,greedyLoses:null,hiddenMatters:null,bridgeKind:null,bridgeSingle:null,pressure:await pressure(level,QUALITY.pressure.games),traps:{links:traps.length,traps:0,top:0},...(lc?{latch:lc}:{})}};}

// a bridge scene (bridge-scene.mjs, a picture whose record carries scene.win): the queue leans to the winning site and
// the gate of the other strip stands deep (candidate opts), one of the last three rows of the tray by attempt
// (a split picture, bridge-scene.mjs make --mode split: its scene.win is the bridge its search leaned to; planned
// 'better', both bridges must win, so the gate stands only in every other attempt and not as deep)
export function sceneOpts(pic,board,P,cols,k){if(!pic.scene||!board.hero||!Array.isArray(pic.scene.win))return {};const rows=Math.ceil(P/cols),i=siteChoice(board,pic.scene.win);
 const gate=pic.scene.mode==='split'&&pic.scene.kind==='better'?(k%2?0:Math.max(2,rows-4)):Math.max(3,rows-1-k%3);
 return {...(gate?{gate}:{}),...(i>=0?{intended:i}:{})};}

// ---- build: candidates for each picture, recipe and class, measured and banked ----
const SIDES=['up','left','right','two'];
// which attempts try accents (--accents auto|on|off; 17 §6, the owner, 2026-11-01): auto = every Hard and Super Hard
// attempt and every third attempt of a regular Normal; on = every attempt; never an introduction or L1–3
export const accentWanted=(mode,cls,intro,fixed,k)=>!intro&&!fixed&&mode!=='off'&&(mode==='on'||cls!=='Normal'||k%3===1);
// the card's record of a candidate's accents: materials, cells, clusters, their pods' tray rows, and «стоят без работы»
// with them and without (accentTwin: the accent cubes back in their ground, the accent pods playing it)
export async function accentCheck(level,pic,accents,withP=null){const twin=accentTwin(level,pic),games=QUALITY.pressure.games;
 const w=withP||await pressure(level,games),wo=twin?await pressure(twin,games):null;
 return {materials:accents.map(a=>({code:a.code,name:PALETTE[a.code].name,from:a.from,cells:a.cells,clusters:a.clusters.length,
   pods:level.pods.map((p,i)=>p.color===a.code?i:-1).filter(i=>i>=0).map(i=>({pod:i,count:level.pods[i].count,row:rowOfPod(level,i)}))})),
  cells:accents.reduce((s,a)=>s+a.cells,0),pressure:{with:w.stuck,without:wo?wo.stuck:null}};}
// One attempt's accents (17 §6, the owner, 2026-11-01), shared by build and the generator's tuning (tune.mjs): when the
// attempt wants them (accentWanted), 1–2 accent materials painted over the picture (the level's recolouring) and the pods
// of the recoloured board; null when the attempt wants none or the picture has no room (the reason counted by no)
// maxColours: the colours the level may reach (the stage's palette, 10; an accent colour does not count in the §2 window of
// 10+ colour pictures, the owner, 2026-11-02, so the runner no longer lowers it)
export function accentVariant(pic,overlay,m,fixed,cls,intro,k,mode,cseed,no=()=>{},maxColours=10){if(!accentWanted(mode,cls,intro,fixed,k))return null;
 const ar=rnd(cseed^0x2545f491),room=(m.N<400?6:Math.min(10,maxColours))-m.colours,a=room>0?pickAccents(pic,overlay,ar,{materials:room>=2&&m.colours<=7&&ar()<.4?2:1}):null;
 if(!a){no(room>0?'акценты: нет места в картинке (строим без них)':'акценты: цветов уже предел (строим без них)');return null;}
 const am=measure(overlaid(pic,overlay,a.recolor)),ab=splitPods(am.volumes,am.N,fixed);if(ab.error){no('акценты: '+ab.error+' (строим без них)');return null;}
 let ps=accentPods(ab.pods,a.accents,ar);if(ps.length>budget(am.N)[1])ps=ab.pods;return {accents:a.accents,recolor:a.recolor,m:am,pods:ps,board:boardOf(pic,overlay,a.recolor)};}
// the channelKind of a build (--channel-kind single): only a Super Hard whose recipe has the water channel takes it
const channelKindOf=(cls,recipe)=>{const k=arg('--channel-kind');if(!k)return null;if(k!=='single')throw Error('--channel-kind: только single');return cls==='Super Hard'&&recipe.split('+').includes('canal')?k:null;};
async function build(ids){const accentMode=arg('--accents','auto');if(!['auto','on','off'].includes(accentMode))throw Error('--accents: auto, on или off');const classes=(arg('--classes','Normal,Hard,Super Hard')).split(','),seed0=+arg('--seed',+(process.env.ROUNDS_SEED||1)),sides=arg('--overlay')?arg('--overlay').split(','):null;
 if(sides&&sides.some(x=>x!=='none'&&!SIDES.includes(x)))throw Error(`--overlay: стороны none, ${SIDES.join(', ')}`);
 // --pairs (rounds.mjs run under a mechanics plan): only the planned class:recipe pairs, @side for a wall's side
 const pairs=arg('--pairs')?new Set(arg('--pairs').split(',')):null;
 // §13.5: the start budget is up to 64 candidates for one picture and requested class
 const budgetN=Math.min(64,+arg('--candidates',64));if(+arg('--candidates',64)>64)console.log('бюджет поиска ограничен 64 кандидатами (17 §13.5)');
 for(const id of ids){const pic=picture(id);if(!pic){console.log(id,'нет картинки');continue;}if(pic.status!=='approved'){console.log(id,'не утверждена — пропуск');continue;}
  const bankFile=path.join(BANK,id+'.json'),bank=readJSON(bankFile,{picture:id,candidates:[],rejected:[]});
  // the boards to build on: the picture's own, or with --overlay a wall the generator lays over it for each side (17 §8)
  const variants=sides?sides.map(side=>{if(side==='none')return {side:null,overlay:null};const ov=wallOverlay(pic,side,seed0);if(!ov)console.log(id,`стена «${side}» не ложится (25–45 % за стеной, подходы цветные)`);return ov?{side,overlay:{walls:ov.walls,sites:ov.sites}}:null;}).filter(Boolean):[{side:null,overlay:null}];
  for(const v of variants){
  const m=measure(overlaid(pic,v.overlay)),board=boardOf(pic,v.overlay),hasBridge=!!board.hero,side=bridgeSide(board),W=m.N,tag=v.overlay?`стена ${v.side}`:'';
  const fixed=pic.locked||null,base=splitPods(m.volumes,W,fixed);
  if(base.error){bank.rejected.push({reason:base.error,...(v.overlay?{overlay:v.side}:{})});writeJSON(bankFile,bank);console.log(id,tag,'отклонена:',base.error);continue;}
  // a board with a strip takes bridge recipes only (its far part needs a bridge), a board without one no bridge recipe
  const all=(arg('--recipes')||(fixed?'none':hasBridge?'bridge,bridge+links':'none,links,hidden,links+hidden')).split(','),recipes=all.filter(x=>x.split('+').every(t=>t==='none'||SUPPORTED.has(t))&&recipeAllowed(x)&&x.split('+').includes('bridge')===hasBridge);
  for(const cls of fixed?['Normal']:classes)for(const recipe of recipes){if(fixed&&recipe!=='none')continue;if(pairs&&!pairs.has(`${cls}:${recipe}${v.overlay?'@'+v.side:''}`))continue;
   for(const intro of recipe!=='none'&&!recipe.includes('+')&&cls==='Normal'&&!flag('--no-intro')?(flag('--intro')?[true]:[false,true]):[false]){
    // resumable: a combination already searched (bank.searched) is skipped; the bank is saved after each one
    // ROUNDS_FRESH=1 (or --fresh) searches again; ROUNDS_FRESH=<tag> searches again once per tag (a resumable re-search)
    const fresh=process.env.ROUNDS_FRESH,runTag=fresh&&fresh!=='1'?fresh:null;
    // the generator's tuning (tune.mjs, the owner, 2026-11-02) builds every non-bridge level of a free picture; --legacy keeps
    // the random / steered blend below. A Normal's slot (--slot regular | after-hard | breather) sets its zone; a picture
    // of up to BREATHER_N cells is a breather (17 §5 «Передышка»)
    const tuned=!fixed&&!recipe.includes('bridge')&&!v.overlay&&!flag('--legacy'),slot=intro?'intro':cls!=='Normal'?cls:arg('--slot')||(m.N<=BREATHER_N?'breather':'regular');
    // (a tuned combination carries the generator's revision, wave.mjs GEN: a new revision searches it again)
    const combo=`${cls}|${recipe}|${intro?'intro':''}${v.overlay?'|'+v.side:''}${tuned?'|tune'+(cls==='Normal'&&!intro&&slot!=='regular'?'|'+slot:'')+'|'+GEN:''}`;bank.searched=bank.searched||[];if(runTag?bank.searched.some(x=>x.combo===combo&&x.tag===runTag):!flag('--fresh')&&!fresh&&bank.searched.some(x=>x.combo===combo)){console.log(id,tag,cls.padEnd(10),recipe.padEnd(13),intro?'знакомство':'','уже искали — пропуск');continue;}
    if(tuned){await buildTuned({pic,id,cls,recipe,intro,slot,bank,bankFile,combo,runTag,seed0,accentMode,budgetN});continue;}
    let tried=0,found=0,foundLoss=0;const reasons={};
    // a regular Normal steers its blend t towards the reverse access order (candidate opts.t) by the shares measured on
    // this picture and recipe so far, aiming at the middle of the median band (17 §5: Normal 50–75 %, median 50–60 %).
    // On most pictures the share falls steeply with t (the access order ≈ 100 %, half way a random order, the reverse
    // often below 50 %), so a t drawn uniformly spent most attempts on 95–100 % or on Hard; ROUNDS_STEER=0 keeps that
    const steer=cls==='Normal'&&!intro&&!fixed&&!recipe.includes('bridge')&&process.env.ROUNDS_STEER!=='0'?{t:+(process.env.ROUNDS_T0||.55),aim:+(process.env.ROUNDS_AIM||.56),n:0}:null;const no=k=>{reasons[k]=(reasons[k]||0)+1;};
    // a Normal also looks for a candidate that "most work now" loses (§5: part of the regular Normals), up to 4 found;
    // ROUNDS_WANT_LOSS=n keeps a regular Normal search going until n such candidates (or ROUNDS_MAX_FOUND found, or the
    // budget), for a wave whose greedy quota fails on what the bank holds
    const wantLoss=+(process.env.ROUNDS_WANT_LOSS||1),maxFound=+(process.env.ROUNDS_MAX_FOUND||4);
    const enough=()=>cls==='Normal'&&!intro&&!fixed?(process.env.ROUNDS_WANT_LOSS?foundLoss>=wantLoss||found>=maxFound:found>=4||(found>=2&&foundLoss>=1)):found>=2;
    for(let k=0;k<budgetN&&!enough();k++){tried++;const cols=fixed?3:columnsFor(m.N)[k%columnsFor(m.N).length];
     const pos=recipe==='none'||intro?null:DOSE_CYCLE[k%DOSE_CYCLE.length];
     const cseed=seed0*1000003+k*7919+CLASSES.indexOf(cls)*31+all.indexOf(recipe)*131+(intro?17:0)+(v.overlay?(SIDES.indexOf(v.side)+1)*523:0);
     const blend=steer?Math.min(1,Math.max(0,steer.t+(rnd(cseed^0x5bd1e995)()-.5)*.12)):null;
     // accents (17 §6, the owner, 2026-11-01): this attempt paints 1–2 accent materials over the picture (the level's
     // recolouring) and puts their pods early or mid queue; a picture without room for them is built without
     const av=accentVariant(pic,v.overlay,m,fixed,cls,intro,k,accentMode,cseed,no),acc=av,cm=av?av.m:m,cboard=av?av.board:board,cpods=av?av.pods:base.pods;
     const c=candidate(pic,cboard,cpods,cols,recipe,cls,cseed,intro,pos,{...sceneOpts(pic,cboard,cpods.length,cols,k),...(blend!==null?{t:blend}:{}),...(acc?{accents:acc.accents.map(x=>x.code)}:{})});
     if(c.error){reasons[c.error]=(reasons[c.error]||0)+1;continue;}
     let quick=null;const trace=process.env.ROUNDS_TRACE?(l=>console.error(`  ${id} ${cls} ${recipe} #${k}${blend!==null?' t '+blend.toFixed(2):''} ${l} ${((Date.now()-t0)/1000).toFixed(1)} s`)):()=>{},t0=Date.now();
     const tick=l=>{const q=/^быстрая оценка ([\d.]+)/.exec(l);if(q)quick=+q[1];trace(l);};
     const ck=channelKindOf(cls,recipe),e=await evaluate(c.level,{m:cm,cls,intro,fixed,tick,...(ck?{channelKind:ck}:{})});
     // the steering step shrinks with the attempts (a noisy share: the same t gives 10–90 % on some pictures)
     if(steer){const q=e.random?e.random.share:quick;if(q!==null){steer.n++;steer.t=Math.min(1,Math.max(.3,steer.t+.45/Math.sqrt(steer.n)*(q-steer.aim)));}}
     if(e.reason){no(e.reason);continue;}
     // the accents and «стоят без работы» with / without them (the same queue, the accent pods playing their ground)
     if(acc)e.checks.accents=await accentCheck(c.level,pic,acc.accents,e.checks.pressure);
     bank.candidates.push({id:`${id}-${cls[0]}${recipe}${v.overlay?'~'+v.side:''}${runTag?'-'+runTag:''}${acc?'-a':''}-${k}${intro?'-intro':''}`,picture:id,requested:cls,cls:e.random.cls,intro,recipe,level:c.level,...(v.overlay?{overlay:v.overlay}:{}),...(side?{bridgeSide:side}:{}),N:cm.N,W:cm.N,colours:cm.colours,P:c.level.pods.length,columns:cols,
      linked:c.level.pods.filter(p=>p.group).length,hidden:c.level.pods.filter(p=>p.mystery).length,...(c.level.hiddenCells?{cells:cellsHiddenAtStart(c.level)}:{}),...(c.level.latches?{latches:c.level.latches.length}:{}),...(c.level.channel?{channel:true}:{}),...(c.level.shutters?{shutters:c.level.shutters.length}:{}),random:e.random,...(e.randomAll?{randomAll:e.randomAll}:{}),solution:e.solution,fairness:e.fairness,strategies:e.strategies,rhythm:e.rhythm,dosePos:pos,...(blend!==null?{blend:Math.round(blend*1000)/1000}:{}),...(e.bridge?{bridge:e.bridge}:{}),checks:e.checks,engine:'tools/rounds/engine.mjs (pilot v9 + §8 link rule)',seed:seed0,attempt:k});
     found++;if(e.checks.greedyLoses)foundLoss++;}
    console.log(id,tag,cls.padEnd(10),recipe.padEnd(13),intro?'знакомство':'',`найдено ${found} из ${tried}`,Object.keys(reasons).length?JSON.stringify(reasons):'');
    bank.searched.push({combo,...(runTag?{tag:runTag}:{}),...(steer?{steer:{t:Math.round(steer.t*1000)/1000,aim:steer.aim}}:{}),tried,found,reasons});writeJSON(bankFile,bank);}}}
  writeJSON(bankFile,bank);}}

// one picture × class × recipe × slot by the generator's tuning (tune.mjs): plan from the model, ≤ 5 quick tests on the blend
// t (Newton, then bisection), links one pair at a time under the forced limit, hidden pods / cells after the class is set,
// evaluate and the slot's filters. The picture remembers the t that landed (bank.tune). --want n levels in the zone that
// pass the slot's filters (default 2), --want-loss also one that "most work now" loses (17 §5), --minutes m a time
// budget on top of --candidates (17 §13.5). The last console line is `@@tune {json}` for rounds.mjs run.
async function buildTuned({pic,id,cls,recipe,intro,slot,bank,bankFile,combo,runTag,seed0,accentMode,budgetN}){
 const want=+arg('--want',2),wantLoss=flag('--want-loss')&&slot==='regular',minutes=+arg('--minutes',Infinity),key=`${cls}|${recipe}|${slot}`;
 bank.tune=bank.tune||{};const memory=bank.tune[key]=bank.tune[key]||{};const trace=process.env.ROUNDS_TRACE?l=>console.error(`  ${id} ${cls} ${recipe} ${slot} ${l}`):()=>{};
 const ck=channelKindOf(cls,recipe),res=await tuneJob({pic,cls,recipe,slot,intro,...(ck?{channelKind:ck}:{}),want,wantLoss,attempts:budgetN,minutes,memory,seed:seed0,accentMode,maxColours:+arg('--max-colours',10),save:()=>writeJSON(bankFile,bank),log:trace});
 for(const x of res.banked){const lv=x.level,e=x.e,inZone=res.accepted.includes(x)&&!x.zoneMiss;
  const rc=x.recipe||recipe;bank.candidates.push({id:`${id}-${cls[0]}${rc}-g${GEN.slice(5).replace('-','')}${runTag?'-'+runTag:''}${x.acc?'-a':''}${slot==='breather'?'-br':''}-${x.k}${intro?'-intro':''}`,picture:id,requested:cls,cls:e.random.cls,intro,recipe:rc,gen:GEN,level:lv,N:x.m.N,W:x.m.N,colours:x.m.colours,P:lv.pods.length,columns:lv.tray.length,
   linked:lv.pods.filter(p=>p.group).length,hidden:lv.pods.filter(p=>p.mystery).length,...(lv.hiddenCells?{cells:cellsHiddenAtStart(lv)}:{}),...(lv.latches?{latches:lv.latches.length}:{}),...(lv.channel?{channel:true}:{}),...(lv.shutters?{shutters:lv.shutters.length}:{}),...(slot==='breather'?{breather:true}:{}),slot,inZone,...(x.zoneMiss?{zoneMiss:true}:{}),
   random:e.random,solution:e.solution,fairness:e.fairness,strategies:e.strategies,rhythm:e.rhythm,dosePos:x.pos,blend:x.t,tune:{steps:x.steps,...(x.links?{links:x.links}:{}),seconds:Math.round(x.seconds)},checks:e.checks,engine:'tools/rounds/engine.mjs (pilot v9 + §8 link rule)',seed:seed0,attempt:x.k});}
 const st=res.stats;console.log(id,cls.padEnd(10),recipe.padEnd(13),slot,`принято ${res.accepted.length} из ${st.attempts} попыток (в банк ${res.banked.length}) за ${Math.round(res.seconds)} с`,JSON.stringify(st.reasons));
 bank.searched.push({combo,...(runTag?{tag:runTag}:{}),tune:{slot,t:memory.t??null,attempts:st.attempts,quick:st.quick,evaluated:st.evaluated,seconds:Math.round(res.seconds)},tried:st.attempts,found:res.accepted.length,reasons:st.reasons});writeJSON(bankFile,bank);
 console.log('@@tune '+JSON.stringify({picture:id,cls,recipe,slot,accepted:res.accepted.length,banked:res.banked.length,attempts:st.attempts,quick:st.quick,evaluated:st.evaluated,seconds:Math.round(res.seconds),first:res.accepted[0]?Math.round(res.accepted[0].seconds):null,share:res.accepted[0]?res.accepted[0].e.random.share:null,reasons:st.reasons}));}

// ---- place: calendar positions from the bank, with the variety rules of §11 and the windows of §2/§13 ----
const typesOf=c=>(c.recipe||'none')==='none'?[]:c.recipe.split('+');
// §7 as amended on 2026-10-27: Super Hard a lead and an additional mechanic once two types are open, Hard a mechanic
// (L5 is built by its queue, §9), a regular Normal a mechanic when needMech
function recipeOk(c,n,slot,needMech){const t=typesOf(c).length;if(slot.intro)return true;
 if(slot.cls==='Super Hard')return t>=Math.min(2,slot.allowed.length);if(slot.cls==='Hard')return n===5||!slot.allowed.length||t>=1;return !needMech||t>=1;}
// §5 (the owner's second amendment of 2026-10-27): a Hard or Super Hard passes "opening + colours" (which includes the
// greedy strategies losing from the start); every level from L4 offers a single launch in at most 35 % of decisions
// a breather (17 §5 «Передышка»): forced decisions are not required
function greedyOk(c,n,br=false){const k=c.checks||{};if(br)return c.cls==='Normal';if(n>=4&&!(k.forced<=.35))return false;if(c.cls==='Normal')return true;return !!(k.opening&&k.opening.ok);}
// §8, §9: the bridge introductions (first: both bridges win, one clearly better; second: exactly one wins), every later
// bridge level the wave plan's bridgeKind (single: exactly one wins and the loss with the other comes later; better: both
// win, one 10+ points better; any: either of the two), single without a plan; a hidden-pod introduction needs a hidden pod that matters
export function mechOk(c,n,slot,plan=null){const k=c.checks||{},second=slot.intro&&INTRO.some(([m,,b])=>m===slot.intro&&b===n);
 if(c.recipe.includes('bridge')){if(slot.intro==='bridge'&&!second)return k.bridgeKind==='first';const want=(!slot.intro&&plan&&plan.bridgeKind)||'single';
  const ok=want==='any'?['single','better'].includes(k.bridgeKind):k.bridgeKind===want;
  return ok&&(k.bridgeKind!=='single'||!k.bridgeLoss||k.bridgeLoss.ok||c.cls==='Super Hard');}
 if(slot.intro==='hidden')return c.hidden>=2&&c.hidden<=3&&!!(k.hiddenMatters&&k.hiddenMatters.ok);
 // a hidden-cell introduction: 5–10 cells still hidden after the start reveal (§9)
 if(slot.intro==='cells')return c.cells>=5&&c.cells<=10;
 // a latch introduction: one latch with its key (§9), its §8 checks passed
 if(slot.intro==='latch')return c.latches===1&&(!k.latch||k.latch.ok);
 // a water channel introduction: each exit keeps a winning continuation (§9)
 if(slot.intro==='canal')return !!(k.channel&&k.channel.each.every(x=>x.win===true));
 // a shutters introduction: one shutter (§9), its §8 check passed
 if(slot.intro==='shutters')return c.shutters===1&&!!(k.shutters&&k.shutters.ok);
 // any level with shutters: every one opens on the saved solution before 80 % W (§8)
 if(k.shutters&&!k.shutters.ok)return false;
 // any level with latches: the §8 checks of its class
 if(k.latch&&!k.latch.ok)return false;return true;}
const greedyLost=L=>{const s=L.strategies||{};return !s['most-now|left']&&!s['most-now|right'];};
// §2 picture fitness: Hard and Super Hard need 3+ colours hidden at the start; a weak picture carries a Normal only with a mechanic
// measured on the board the level plays: a level on the generator's wall is measured with the wall
const fitCache={};const fitOf=c=>{const ov=c.overlay||(c.level||c.definition||{}).overlay||null,k=c.picture+(ov?JSON.stringify(ov):'');return fitCache[k]||(fitCache[k]=fitness(overlaid(picture(c.picture),ov)));};
function fitOk(c,n){if(n<=3)return true;const f=fitOf(c),weak=f.hiddenColours<=1||f.visible>=6;
 if(c.cls!=='Normal')return f.hiddenColours>=3;return !weak||typesOf(c).length>=1;}
// a regular Normal for the median of §5: not L1–3, not an introduction, not the first Normal after a Hard or Super Hard
// (a breather, 17 §5 «Передышка», is not in the median nor in the «по цветам» quota)
function regularNormal(L,placed){const p=placed[L.level-1];return L.cls==='Normal'&&L.level>3&&!L.intro&&!L.breather&&!(p&&p.cls!=='Normal');}
const lead=c=>(c.recipe||'none').split('+')[0];
export function reservedFamily(id,n){const w=mechanicsPlans().find(p=>n>=p.wave[0]&&n<=p.wave[1]);return !!(w&&(w.bridgeFamilies||[]).includes(picture(id).family));}
// a picture's family, motifs and own colours (accents aside: the 10+ window counts the picture's colours, 17 §2 as amended on
// 2026-11-02)
const infoCache=new Map();export const picInfo=id=>{if(!infoCache.has(id)){const p=picture(id);infoCache.set(id,{family:(p&&p.family)||null,motifs:p?motifsOf(p):[],colours:p?measure(p).colours:0});}return infoCache.get(id);};
// picture repeats (17 §11 as amended by the owner on 2026-11-02): a family or a motif at most twice in the wave and not
// within 10 levels of another appearance (wave.mjs repeatClash); the rule broken, else null
export function repeats(id,n,placed){return repeatClash(picInfo(id),n,Object.values(placed).filter(L=>L.level!==n&&L.picture!==id).map(L=>({level:L.level,...picInfo(L.picture)})),waveRange(n));}
// the pack rule of place (17 §11, 2026-11-01): candidates whose pack is a neighbour's are dropped while another candidate
// (or a pinned one) is left; a pinned candidate always stays
export function packRule(cands,nearPacks,isPinned,packOfCand){
 return cands.some(c=>!isPinned(c)&&!nearPacks.has(packOfCand(c)))?cands.filter(c=>isPinned(c)||!nearPacks.has(packOfCand(c))):cands;}
// ROUNDS_HOLD_OPEN=59,62,…: positions the placement leaves open (rounds.mjs run --rebuild: the dry placement, so those
// positions are built again whatever the bank holds)
const HOLD=new Set(String(process.env.ROUNDS_HOLD_OPEN||'').split(',').filter(Boolean).map(Number));
const PICKS=new Map(String(arg('--pick','')).split(',').filter(Boolean).map(x=>{const [n,id]=x.split(':');return [+n,id];}));
async function place(from,to){const placed={};for(const f of fs.existsSync(LEVELS)?fs.readdirSync(LEVELS).filter(f=>/^L\d+\.json$/.test(f)):[]){const L=readJSON(path.join(LEVELS,f));placed[L.level]=L;}
 const banks=fs.existsSync(BANK)?fs.readdirSync(BANK).map(f=>readJSON(path.join(BANK,f))):[];const pool=banks.flatMap(b=>b.candidates);const m=manifest(),usedPic=new Set(Object.keys(usage()));const needs=[];
 const plans=mechanicsPlans();
 // pinned pictures (mechanics.json `pin`, the owner): they must go into their wave. The planner (rounds.mjs plan) reserves
 // each for a fitting position (runs/<wave>/plan.json pins[].reserved); a pin is kept off every position before its
 // reserved one, takes its reserved position (or, if it has no passing candidate there, the next position it fits)
 // with a strong preference, and is exempt from the pack rule below. Those left unused are reported.
 const waveOf=n=>plans.find(p=>n>=p.wave[0]&&n<=p.wave[1])||null,reservedOf=new Map();
 // the picture-first wave (rounds.mjs plan, the owner's principle 17): plan.json `assign` gives a position ONE picture;
 // such a position takes only that picture (or its recorded replacement), the others never take an assigned picture
 const assignOf=new Map(),assignedPics=new Set();
 for(const p of plans){const pj=readJSON(path.join(ROUNDS,'runs',`${lid(p.wave[0])}-${lid(p.wave[1])}`,'plan.json'));for(const x of (pj&&pj.pins)||[])if(x.reserved)reservedOf.set(x.id,x.reserved);
  for(const x of (pj&&pj.assign)||[])if(x.picture){assignOf.set(x.level,x);assignedPics.add(x.picture);}}
 const pinsOf=n=>{const w=waveOf(n);return new Set(w&&Array.isArray(w.pin)?w.pin:[]);};
 for(let n=from;n<=to;n++){if(placed[n])continue;const slot=slotOf(n),prev=[placed[n-1],placed[n-2]].filter(Boolean);
  // an approved mechanics plan (17 §13 step 1, amendment of 2026-10-28) fixes the position's recipe and bridge side
  const plan=planOf(n,plans);
  // a position the plan gives to hand building (17 §13 «Кто строит уровни», 2026-10-30: by 'hand') is left to adopt
  if(plan&&plan.by==='hand'){console.log(lid(n),plan.cls,'— вручную по плану (levels.mjs adopt), генератор не ставит');continue;}
  if(HOLD.has(n)){needs.push({level:n,cls:slot.cls,intro:slot.intro,allowed:slot.allowed,stage:stageOf(n),held:true,...(plan?{recipe:plan.recipe}:{})});continue;}
  // Normal after the introductions: a mechanic in two of three (17 §7); a third may go without when the last two had one
  const lastRegular=Object.values(placed).filter(L=>L.level<n&&L.cls==='Normal'&&!L.intro&&slotOf(L.level).allowed.length).sort((a,b)=>b.level-a.level).slice(0,2);
  const needMech=!plan&&slot.cls==='Normal'&&!slot.intro&&slot.allowed.length>0&&!(lastRegular.length===2&&lastRegular.every(L=>(L.recipe||'none')!=='none'));
  const regularSlot=slot.cls==='Normal'&&n>3&&!slot.intro&&!(placed[n-1]&&placed[n-1].cls!=='Normal')&&!(!placed[n-1]&&n>1&&classOf(n-1)!=='Normal');
  // a breather (17 §5 «Передышка», 2026-11-02): a Normal on a picture up to BREATHER_N cells or a position the plan marks
  // breather; its zone 75–90 %, zero or one light mechanic (the plan's, or one plain link pair in its place) without a trap
  // link, only the mandatory checks
  const brOf=c=>slot.cls==='Normal'&&!slot.intro&&n>3&&(!!(plan&&plan.breather)||c.N<=BREATHER_N),asg=assignOf.get(n)||null;
  const brBad=c=>!breatherRecipeOk(c.recipe,plan?plan.recipe:null)||(c.checks&&c.checks.traps&&c.checks.traps.traps)||(c.linked||0)>2;
  const placedInfo=Object.values(placed).map(L=>({level:L.level,...picInfo(L.picture)}));
  let ok=pool.filter(c=>!usedPic.has(c.picture)&&c.cls===slot.cls&&fitsStage({N:c.N,colours:c.colours},n)
   &&(asg?c.picture===asg.picture:!assignedPics.has(c.picture))
   &&!(c.breather&&!brOf(c))&&!(brOf(c)&&brBad(c))
   &&(slot.intro?(c.intro&&c.recipe===slot.intro):!c.intro&&(c.recipe==='none'||c.recipe.split('+').every(t=>slot.allowed.includes(t))))
   &&((picture(c.picture).locked||null)===(n<=3?n:null))
   // a third-party universe or a meme is not for release without rights (the audit's mandatory rule, theme catalog §4),
   // unless the owner confirmed them for this picture (rights 'owner')
   &&!franchiseBlocked(picture(c.picture))
   // a pinned picture reserved for a later position waits for it
   &&!(pinsOf(n).has(c.picture)&&reservedOf.get(c.picture)>n&&!placed[reservedOf.get(c.picture)])
   // §11: no lead mechanic or identical recipe 3 times in a row; the two teaching appearances do not count
   // (the owner, 2026-10-29: links dominate, so a links lead and links+hidden may repeat any number of times)
   &&!(prev.length===2&&!slot.intro&&lead(c)!=='none'&&lead(c)!=='links'&&prev.every(L=>!L.intro&&lead(L)===lead(c)))
   &&!(prev.length===2&&!slot.intro&&c.recipe!=='links+hidden'&&prev.every(L=>!L.intro&&L.recipe===c.recipe&&c.recipe!=='none'))
   // picture repeats (17 §11 as amended on 2026-11-02): a family or a motif at most twice in the wave, 10 levels apart
   &&!repeats(c.picture,n,placed)
   // the wave plan keeps some families for its bridge levels (bridgeFamilies): other positions leave them
   &&!(plan&&!plan.bridgeSide&&reservedFamily(c.picture,n))
   // §2: at most two pictures of 10+ colours in any 20 levels, the picture's own colours (an accent colour does not count,
   // the owner, 2026-11-02)
   &&!wideClash(n,picInfo(c.picture).colours,placedInfo)
   // the owner's amendment of 2026-10-27: recipes by class (§7), greedy strategies (§5), picture fitness (§2)
   &&recipeOk(c,n,slot,needMech)&&greedyOk(c,n,brOf(c))&&fitOk(c,n)&&mechOk(c,n,slot,plan)
   // §6: a regular Normal holds a place with a deep-colour pod in its first two rows
   // (a bridge level's task is the bridge choice; a gap split can leave no colour off the start front, so a bridge
   // level does not need the holding pod, 2026-10-29)
   &&!(regularSlot&&!brOf(c)&&!c.recipe.includes('bridge')&&!(c.checks&&c.checks.hold))
   &&(plan&&brOf(c)?breatherRecipeOk(c.recipe,plan.recipe):fitsPlan(c,plan)));
  // §5 calm test and §8 trap links (the owner's amendment of 2026-10-31), measured for the candidates that passed the other
  // filters when the bank has them not yet (kept in the bank): a Hard ≤ 50 %, a Super Hard ≤ 25 %, a regular Normal ≥ 50 %
  // calm; a Hard or Super Hard with links has a trap link. Hand positions are adopted, not placed
  for(const c of ok)if((!c.checks.calm&&!brOf(c))||!c.checks.traps){const lv=c.level;if(!c.checks.calm&&!brOf(c))c.checks.calm=await calmTest(lv,classKeep(c.random));
   if(!c.checks.traps){const t=trapLinks(lv);c.checks.traps={links:t.length,traps:t.filter(x=>x.trap).length,top:t.filter(x=>x.trap&&x.row<=2).length};}
   const b=banks.find(b=>b.picture===c.picture);if(b)writeJSON(path.join(BANK,b.picture+'.json'),b);}
  ok=ok.filter(c=>(brOf(c)||!calmFails(c.cls,c.checks.calm,regularSlot))&&!(c.cls!=='Normal'&&c.checks.traps.links&&!c.checks.traps.traps));
  // packs (17 §11, the owner's second amendment of 2026-11-01): no two neighbouring levels from one pack (lib.mjs packOf)
  // while another fitting candidate exists; a pinned picture is exempt
  const pins=pinsOf(n),pinned=c=>pins.has(c.picture),packAt=k=>placed[k]?packOf(picture(placed[k].picture)):null,near=new Set([packAt(n-1),packAt(n+1)].filter(Boolean));
  ok=packRule(ok,near,pinned,c=>packOf(picture(c.picture)));
  // preference: the preferred zone of the class (first Normal after Hard/Super Hard: 75–95 %), then the size mix per 30
  // Normal: the first after a Hard or Super Hard 75–95 %; otherwise steered so the median of the regular Normals stays 50–60 %
  // the median of §5 over any 20 consecutive levels (wave.mjs medianPenalty): a regular Normal is weighed by how far it moves
  // the windows of the wave that hold it out of 50–60 %, and aims at the middle of the zone itself
  // a hand position before it (a Super Hard built later, 17 §13) counts by the calendar
  const afterHard=prev[0]?prev[0].cls!=='Normal':(n>1&&!placed[n-1]&&classOf(n-1)!=='Normal'),wr=waveRange(n),regW=Object.values(placed).filter(L=>regularNormal(L,placed)&&L.level>=wr[0]&&L.level<=wr[1]);
  const high=Object.values(placed).filter(L=>regularNormal(L,placed)&&L.level>n-10&&L.random.share>=.95).length;
  const medPen=c=>regularSlot&&!brOf(c)?medianPenalty(n,c.random.share,regW.map(L=>({level:L.level,share:L.random.share})),wr):0;
  const zone=slot.cls==='Normal'?(afterHard?[.75,.95]:[.5,.65]):slot.cls==='Hard'?[.25,.4]:[.07,.15];
  // the dose cycle: the j-th placed level with a mechanic aims at DOSE_CYCLE[j]
  const j=Object.values(placed).filter(L=>L.level<n&&!L.intro&&(L.recipe||'none')!=='none').length,target=DOSE_CYCLE[j%DOSE_CYCLE.length];
  const bins=Object.values(placed).filter(L=>Math.floor((L.level-1)/30)===Math.floor((n-1)/30)).map(L=>sizeBin(L.N)),want={'200–499':6,'500–999':15,'1000–1400':9};
  // a borderline class (the Wilson interval crosses a border, §5) needs a manual decision: taken last
  // a picture that also has Hard/Super Hard candidates is kept for those scarcer positions when the slot is Normal
  const hardSlots=[...Array(to-n+1).keys()].map(k=>n+k).filter(k=>!placed[k]&&classOf(k)!=='Normal').length,hardPics=new Set(pool.filter(x=>x.cls!=='Normal'&&!usedPic.has(x.picture)).map(x=>x.picture)).size;
  const scarce=c=>slot.cls==='Normal'&&hardPics<2*hardSlots&&pool.some(x=>x.picture===c.picture&&x.cls!=='Normal')?.15:0;
  const doseScore=c=>(c.recipe==='none'||slot.intro)?0:c.dosePos==null?.1:Math.abs(c.dosePos-target)*.25;
  // an introduction prefers a weak picture (its mechanic carries the task) and keeps strong ones for regular levels
  const introScore=c=>{if(!slot.intro)return 0;const f=fitOf(c);return f.hiddenColours<=1||f.visible>=6?0:.1;};
  // §5: "most work now" loses about one in three regular Normals on L4–24 and one in two from L25, counted in the wave
  // (wave.mjs greedyQuota, this position counted as one it wins): a preference of 0.6, 0.2 more for each level the wave
  // is behind beyond the first (at most 1.2)
  const gq=greedyQuota([...regW.filter(L=>L.level!==n).map(L=>({level:L.level,lost:greedyLost(L)})),{level:n,lost:false}],wr[0],wr[1]).find(x=>n>=x.from&&n<=x.to),deficit=gq?gq.target-gq.lost:0;
  const needLoss=regularSlot&&deficit>0,lossW=Math.min(1.2,.6+.2*Math.max(0,deficit-1));
  // layer B (17 §13, quality.json): pods without work at least the class's bound; soft: a preference, hard: a filter
  const pMin=slot.intro?null:regularSlot?QUALITY.pressure.min.regularNormal:slot.cls!=='Normal'?QUALITY.pressure.min[slot.cls]:null;
  const pressureLow=c=>pMin!=null&&!brOf(c)&&!(c.checks&&c.checks.pressure&&c.checks.pressure.stuck>=pMin);
  const normalScore=c=>introScore(c)+(pressureLow(c)?.5:0)+(slot.cls!=='Normal'||slot.intro||brOf(c)?0:(c.random.share>=.95&&high>=1?.6:0)+(!needMech&&c.recipe!=='none'?.2:0)+(needLoss&&!greedyLost(c)?lossW:0));
  // a regular Normal aims at the middle of 50–60 % itself (MEDIAN.aim), not just anywhere in the zone
  const aim=slot.cls==='Normal'&&!slot.intro&&!afterHard?MEDIAN.aim:null;
  // layer B soft preference (quality.json pressure.prefer, 2026-11-01): more pods without work is better, up to a cap
  // the tuning zone of a regular Normal (tune.mjs ZONES.regular, 50–65 %): a candidate outside it only as a fallback; the
  // dry placement of rounds.mjs run (ROUNDS_DRY) leaves the position open instead, so it is built
  const RZ=ZONES.regular,outZone=c=>regularSlot&&!brOf(c)&&!(c.random.share>=RZ[0]-1e-9&&c.random.share<=RZ[1]+1e-9);
  if(process.env.ROUNDS_DRY)ok=ok.filter(c=>!outZone(c));
  const pPref=QUALITY.pressure.prefer,prefer=c=>pPref&&!slot.intro&&c.checks&&c.checks.pressure?-pPref.weight*Math.min(c.checks.pressure.stuck,pPref.cap)/pPref.cap:0;
  // packs spread over the wave (soft): each level of the wave already from the candidate's pack, and the same pack two
  // levels back, cost a little; a pinned picture is strongly preferred (it must be used); the first latch introduction
  // prefers a latch that stands closed on top for a while on the saved solution (the player sees it wait for its key)
  const w0=waveOf(n),packUse={};for(const L of Object.values(placed))if(w0&&L.level>=w0.wave[0]&&L.level<=w0.wave[1]){const k=packOf(picture(L.picture));packUse[k]=(packUse[k]||0)+1;}
  const packScore=c=>{const k=packOf(picture(c.picture));return .12*(packUse[k]||0)+(packAt(n-2)===k?.15:0);};
  const pinScore=c=>pinned(c)?-3:0,latchScore=c=>slot.intro==='latch'&&!INTRO.some(([m,,b])=>m==='latch'&&b===n)&&c.checks&&c.checks.latch&&c.checks.latch.waits?-.2:0;
  const score=c=>{const s=c.random.share,br=brOf(c),z=br?BREATHER_ZONE:zone,a=br?null:aim;return 4*medPen(c)+(outZone(c)?1.5:0)+pinScore(c)+packScore(c)+latchScore(c)+(br?0:prefer(c))+doseScore(c)+normalScore(c)+scarce(c)+(c.random.borderline?1:0)+(calmBorderline(c.cls,c.checks.calm)?.5:0)+(a!==null?2*Math.abs(s-a):s>=z[0]&&s<=z[1]?0:Math.min(Math.abs(s-z[0]),Math.abs(s-z[1])))+(bins.filter(b=>b===sizeBin(c.N)).length>=want[sizeBin(c.N)]?.3:0);};
  if(QUALITY.pressure.mode==='hard')for(let i=ok.length-1;i>=0;i--)if(pressureLow(ok[i]))ok.splice(i,1);
  ok.sort((a,b)=>score(a)-score(b));
  // --pick n:id takes that candidate at position n when it passes every filter above (a sequence rule of §5 that the
  // score cannot meet, e.g. the Normal median with the greedy quota); a pick that fails a filter leaves the position open
  const pick=PICKS.get(n);if(pick&&!ok.some(x=>x.id===pick))console.log(lid(n),'--pick',pick,'не проходит фильтры позиции');
  const c=pick?ok.find(x=>x.id===pick):ok[0];
  if(process.env.ROUNDS_TRACE)console.error(lid(n),'кандидатов',ok.length,ok.slice(0,6).map(c=>`${c.picture} ${c.recipe} ${Math.round(c.random.share*100)}% поз ${c.dosePos??'—'} балл ${score(c).toFixed(2)}`).join(' | '));
  if(!c){needs.push({level:n,cls:slot.cls,intro:slot.intro,allowed:slot.allowed,stage:stageOf(n),...(plan?{recipe:plan.recipe,...(plan.bridgeSide?{bridgeSide:plan.bridgeSide,strip:plan.strip||null,bridgeKind:plan.bridgeKind||'single'}:{})}:{})});continue;}
  const L={level:n,cls:slot.cls,intro:slot.intro,...(brOf(c)?{breather:true}:{}),...(c.zoneMiss?{zoneMiss:true}:{}),...(c.gen?{gen:c.gen}:{}),picture:c.picture,candidate:c.id,recipe:c.recipe,N:c.N,W:c.W,colours:c.colours,P:c.P,columns:c.columns,linked:c.linked,hidden:c.hidden,...(c.cells!=null?{cells:c.cells}:{}),...(c.latches!=null?{latches:c.latches}:{}),random:c.random,...(c.randomAll?{randomAll:c.randomAll}:{}),solution:c.solution,fairness:c.fairness,strategies:c.strategies,rhythm:c.rhythm,dosePos:c.dosePos??null,...(c.bridge?{bridge:c.bridge,bridgeSide:c.bridgeSide??bridgeSide(c.level)}:{}),checks:c.checks,engine:c.engine,definition:{...c.level,id:n,difficulty:slot.cls}};
  writeJSON(path.join(LEVELS,lid(n)+'.json'),L);placed[n]=L;usedPic.add(c.picture);
  const e=m.pictures.find(p=>p.id===c.picture);if(e)e.usedBy=lid(n);const pf=path.join(PICTURES,c.picture+'.json'),pj=readJSON(pf);pj.usedBy=lid(n);writeJSON(pf,pj);
  for(const b of banks)if(b.picture===c.picture){b.candidates=b.candidates.filter(x=>x.id!==c.id);b.placed=(b.placed||[]).concat([c.id]);writeJSON(path.join(BANK,b.picture+'.json'),b);}
  console.log(lid(n),slot.cls.padEnd(10),slot.intro?'знакомство: '+MECH_RU[slot.intro]:'',c.picture,c.recipe,L.bridgeSide?`мост ${L.bridgeSide}${c.level.overlay?' (стена)':''}`:'',`случайный тест ${Math.round(c.random.share*100)} %`);}
 // pinned pictures of the waves in from–to still not in a round
 const pinsLeft=[];for(const p of plans){if(p.wave[1]<from||p.wave[0]>to||to<p.wave[1]||!Array.isArray(p.pin))continue;for(const id of p.pin)if(!usedPic.has(id))pinsLeft.push({id,wave:p.wave,reserved:reservedOf.get(id)||null,reason:franchiseBlocked(picture(id))?'сторонняя вселенная без прав владельца':picture(id)&&picture(id).status!=='approved'?'не утверждена':'нет прошедшего кандидата на подходящей позиции'});}
 if(pinsLeft.length)console.log(`закреплённые картинки не поставлены: ${pinsLeft.map(x=>`${x.id}${x.reserved?` (резерв L${x.reserved})`:''} — ${x.reason}`).join('; ')}`);
 saveManifest(m);if(!process.env.ROUNDS_NO_PUBLISH)execFileSync(process.execPath,[new URL('./pictures.mjs',import.meta.url).pathname,'publish'],{stdio:'inherit',env:process.env});const nf=path.join(ROUNDS,'needs.json'),old=readJSON(nf,{needs:[]});const keep=old.needs.filter(x=>x.level<from||x.level>to);
 const pinsKeep=(old.pins||[]).filter(x=>x.wave[1]<from||x.wave[0]>to);writeJSON(nf,{updated:new Date().toISOString().slice(0,10),needs:keep.concat(needs).sort((a,b)=>a.level-b.level),...(pinsKeep.length||pinsLeft.length?{pins:pinsKeep.concat(pinsLeft)}:{})});
 if(needs.length)console.log(`не заполнено ${needs.length}: ${needs.map(x=>`L${x.level} ${x.cls}${x.intro?' ('+x.intro+')':''}${x.recipe?' '+x.recipe+(x.bridgeSide?' '+x.bridgeSide+(x.strip?'/'+x.strip:''):''):''}`).join(', ')} → content/rounds/needs.json`);}

function card(n){const L=readJSON(path.join(LEVELS,lid(n)+'.json'));if(!L)return console.log('нет уровня',n);
 console.log(`${lid(n)} · ${L.cls}${L.intro?' · знакомство: '+MECH_RU[L.intro]:''} · картинка ${L.picture} «${L.definition.title}»\nN ${L.N} · W ${L.W} · материалов ${L.colours} · подов ${L.P} · колонок ${L.columns} · мест ${L.definition.capacity}\nрецепт: ${L.recipe} (связанных ${L.linked}, скрытых ${L.hidden}${L.cells!=null?`, скрытых клеток ${L.cells} из ${L.definition.hiddenCells.length}`:''}${L.definition.latches?`, засовов ${L.definition.latches.length}: ${L.definition.latches.map(l=>`колонка ${l.column+1} после ${l.depth} подов, ключ в клетке ${l.key}`).join('; ')}`:''})${L.definition.hero?` · мост: ${bridgeSide(L.definition)} (${L.definition.hero.choices.map(c=>c.label).join(', ')})${L.definition.overlay?', стена генератора поверх картинки':''}`:''}\nслучайный тест: ${Math.round(L.random.share*100)} % из ${L.random.runs}, Wilson95 ${L.random.wilson95.map(x=>Math.round(x*100)+' %').join('–')}${L.random.bridge!=null&&L.definition.hero?` (с мостом «${L.definition.hero.choices[L.random.bridge].label}»${L.randomAll?`; с обоими мостами ${Math.round(L.randomAll.share*100)} %`:''})`:''}${L.random.exit!=null&&L.definition.channel?` (с выходом канала ${L.random.exit+1} — решает ровно один${L.randomAll?`; с обоими выходами ${Math.round(L.randomAll.share*100)} %`:''})`:''}\nрешение: ${L.solution.route.length} действий, опасных развилок ${L.solution.forks}, с одним безопасным ${L.solution.singleSafe}\nпосле 80 % W запусков: ${L.rhythm.launchesAfter80}, последний при ${L.rhythm.lastLaunchAtW} % W, время решения ${L.rhythm.seconds} с (×1, после окончания работы), тапы сразу: ${L.solution.fast?L.solution.fast.seconds+' с ('+L.solution.fast.status+')':'—'}\nэтапы: ${L.rhythm.segments.map(s=>`${s.from}–${s.to}%: своб. ${s.meanFree}/${s.minFree}, полный ${Math.round((s.fullShare||0)*100)} %`).join(' · ')}\nпростые стратегии: выиграли ${Object.values(L.strategies).filter(Boolean).length} из ${Object.keys(L.strategies).length}; «больше работы сейчас» ${greedyLost(L)?'проигрывает':'выигрывает'}\nвынужденных решений: ${L.checks?Math.round(L.checks.forced*100)+' %':'—'}${L.checks&&L.checks.opening?` · дебют + цвета: после первого запуска ${L.checks.opening.first.wins} из ${L.checks.opening.first.openings}, после трёх ${Math.round(L.checks.opening.three.share*100)} %`:''}${L.checks&&L.checks.hold!=null?' · под держит место: '+(L.checks.hold?'да':'нет'):''}${L.checks&&L.checks.pressure?' · стоят без работы: '+L.checks.pressure.stuck:''}${L.checks&&L.checks.bridgeKind?' · мост: '+(L.checks.bridgeKind==='single'?'решает ровно один':'оба выигрывают, один лучше'):''}${accentLine(L)}${L.checks&&L.checks.latch?`\nзасовы на решении: ${L.checks.latch.latches.map(x=>`${x.latch+1} открыт при ${x.atW==null?'—':Math.round(x.atW*100)+' % W'}${x.waitMs?` (ждал ${Math.round(x.waitMs/100)/10} с)`:''}`).join(', ')} · перекрыто колонок сразу до ${L.checks.latch.maxBlocked} из ${L.checks.latch.limit}`:''}`);}
// the card's accents (17 §6, 2026-11-01): each material, its clusters over which ground, its pods' rows, and «стоят без
// работы» with and without the accents; a recolouring that is no builder's accent is listed cell by cell
function accentLine(L){const d=L.definition,a=L.checks&&L.checks.accents;if(!a&&!(d.recolor&&d.recolor.length))return '';
 if(!a)return `\nперекраска: ${d.recolor.map(([x,y,c])=>`(${x}, ${y}) → ${PALETTE[c].name}`).join(', ')}`;
 const pr=a.pressure||{},diff=pr.without!=null?` (без акцентов ${pr.without}, ${pr.with-pr.without>=0?'+':''}${Math.round((pr.with-pr.without)*100)/100})`:'';
 return `\nакценты: ${a.materials.map(m=>`${m.name} ×${m.cells} (${m.clusters} скопл. на «${PALETTE[m.from].name}», поды ${m.pods.map(p=>`${m.code}${p.count} в ряду ${p.row+1}`).join(', ')})`).join('; ')} · стоят без работы ${pr.with}${diff}`;}

if(cmd==='calendar'){const a=+process.argv[3]||1,b=+process.argv[4]||30;for(let n=a;n<=b;n++){const s=slotOf(n);console.log(lid(n),s.cls.padEnd(10),s.intro?('знакомство: '+MECH_RU[s.intro]+(SUPPORTED.has(s.intro)?'':' — НЕ ПОДДЕРЖИВАЕТСЯ ДВИЖКОМ')).padEnd(46):''.padEnd(46),'разрешены:',s.allowed.map(x=>MECH_RU[x]+(SUPPORTED.has(x)?'':' (нет в движке)')).join(', ')||'без допов');}}
else if(cmd==='build'){const ids=flag('--approved')?(u=>manifest().pictures.filter(p=>p.status==='approved'&&!u[p.id]).map(p=>p.id))(usage()):process.argv.slice(3).filter(a=>/^P\d+$/.test(a));await build(ids);}
else if(cmd==='place'){await place(+process.argv[3],+process.argv[4]);}
else if(cmd==='unplace'){const a=+process.argv[3],b=+process.argv[4],m=manifest();
 // puts the placed candidates of levels a–b back into their banks and frees their pictures (to place again)
 for(let n=a;n<=b;n++){const f=path.join(LEVELS,lid(n)+'.json'),L=readJSON(f);if(!L)continue;
  // a hand-built level (adopt) has no bank: it is only taken off its position and its picture freed
  if(L.hand){fs.unlinkSync(f);const e=m.pictures.find(p=>p.id===L.picture);if(e)delete e.usedBy;const pf=path.join(PICTURES,L.picture+'.json'),pj=readJSON(pf);delete pj.usedBy;writeJSON(pf,pj);console.log(lid(n),'снят (ручной):',L.picture);continue;}
  const bf=path.join(BANK,L.picture+'.json'),bank=readJSON(bf,{picture:L.picture,candidates:[],rejected:[]});
  const {id,difficulty,...level}=L.definition;bank.candidates.push({id:L.candidate,picture:L.picture,requested:L.cls,cls:L.cls,intro:!!L.intro,recipe:L.recipe,level:{id:1,...level},N:L.N,W:L.W,colours:L.colours,P:L.P,columns:L.columns,linked:L.linked,hidden:L.hidden,...(L.cells!=null?{cells:L.cells}:{}),...(L.latches!=null?{latches:L.latches}:{}),random:L.random,solution:L.solution,fairness:L.fairness,strategies:L.strategies,rhythm:L.rhythm,dosePos:L.dosePos??posOf(L.candidate,L.recipe,L.intro),...(L.gen?{gen:L.gen}:{}),...(L.zoneMiss?{zoneMiss:true}:{}),...(L.breather?{breather:true}:{}),...(L.bridge?{bridge:L.bridge}:{}),...(level.overlay?{overlay:level.overlay}:{}),...(L.bridgeSide?{bridgeSide:L.bridgeSide}:{}),checks:L.checks,engine:L.engine});
  bank.placed=(bank.placed||[]).filter(x=>x!==L.candidate);writeJSON(bf,bank);fs.unlinkSync(f);
  const e=m.pictures.find(p=>p.id===L.picture);if(e)delete e.usedBy;const pf=path.join(PICTURES,L.picture+'.json'),pj=readJSON(pf);delete pj.usedBy;writeJSON(pf,pj);console.log(lid(n),'снят:',L.picture,L.candidate);}
 saveManifest(m);execFileSync(process.execPath,[new URL('./pictures.mjs',import.meta.url).pathname,'publish'],{stdio:'inherit',env:process.env});}
else if(cmd==='adopt'){const n=+process.argv[3],src=readJSON(path.resolve(process.argv[4]));if(!src||!src.definition)throw new Error('нужен уровень full.mjs (definition, random, checks)');
 const slot=slotOf(n),plan=planOf(n,mechanicsPlans()),pic=manifest().pictures.find(p=>p.id===src.picture),f=path.join(LEVELS,lid(n)+'.json'),errs=[];
 if(!pic||pic.status!=='approved')errs.push(`картинка ${src.picture} не утверждена`);if(pic&&pic.usedBy&&pic.usedBy!==lid(n))errs.push(`картинка ${src.picture} уже в ${pic.usedBy}`);
 if(src.random.cls!==slot.cls)errs.push(`класс ${src.random.cls}, по календарю ${slot.cls}`);if(fs.existsSync(f))errs.push(`${lid(n)} уже поставлен (сначала unplace)`);
 const side=src.definition.hero?bridgeSide(src.definition):null;if(plan&&!fitsPlan({recipe:src.recipe,bridgeSide:side,level:src.definition},plan))errs.push(`рецепт ${src.recipe}/${side} не по плану ${plan.recipe}/${plan.bridgeSide}`);
 // a hand Super Hard with a deciding channel (the plan's channelKind single, the owner, 2026-11-05): measured with its winning exit
 if(plan&&plan.channelKind==='single'){const ch=src.checks&&src.checks.channel;if(!src.definition.channel)errs.push('по плану канал single, а канала нет');
  else if(!ch||!ch.decides)errs.push('канал: решает не ровно один выход (single)');
  else if(src.random.exit!==ch.each.findIndex(x=>x.win===true)||!src.checks||src.checks.channelKind!=='single')errs.push('канал single: класс не измерен с выигрывающим выходом (evaluate channelKind single, §5)');}
 if(errs.length){console.error('НЕТ:',errs.join('; '));process.exit(1);}
 const {level:_l,cls:_c,intro:_i,definition,...rest}=src,{id:_id,difficulty:_d,...def}=definition;
 const L={level:n,cls:slot.cls,intro:slot.intro,...rest,dosePos:null,...(side?{bridgeSide:side}:{}),hand:true,definition:{...def,id:n,difficulty:slot.cls}};
 writeJSON(f,L);const m=manifest(),e=m.pictures.find(p=>p.id===src.picture);e.usedBy=lid(n);const pf=path.join(PICTURES,src.picture+'.json'),pj=readJSON(pf);pj.usedBy=lid(n);writeJSON(pf,pj);saveManifest(m);
 execFileSync(process.execPath,[new URL('./pictures.mjs',import.meta.url).pathname,'publish'],{stdio:'inherit',env:process.env});
 console.log(lid(n),slot.cls,'вручную:',src.picture,src.recipe,side?'мост '+side:'',`случайный тест ${Math.round(src.random.share*100)} %${src.random.bridge!=null?' (с решающим мостом)':''}${src.random.exit!=null?' (с решающим выходом канала)':''}`);}
else if(cmd==='bank'){for(const f of fs.existsSync(BANK)?fs.readdirSync(BANK):[]){const b=readJSON(path.join(BANK,f));const by={};for(const c of b.candidates){const k=c.cls+(c.intro?' (знакомство)':'');(by[k]=by[k]||[]).push(`${c.recipe} ${Math.round(c.random.share*100)}%`);}console.log(b.picture,JSON.stringify(by),b.rejected.length?'отклонения: '+b.rejected.map(x=>x.reason).join('; '):'');}}
else if(cmd==='card'){card(+process.argv[3]);}
else if(cmd==='checks'){
 // the checks of 17 §5–§9 and §13 (layer B) for banked candidates built before them, and with --levels for the placed
 // levels; a check already stored is kept. --shard i/n splits the bank files between processes
 const [si,sn]=(arg('--shard','0/1')).split('/').map(Number);
 const fill=async c=>{const lv=typeof c.level==='object'?c.level:c.definition,st=c.strategies||{};let n=0;
  if(!c.checks){const opening=c.cls==='Normal'?null:await openingTest(lv,c.cls),hm=lv.pods.some(p=>p.mystery)?await hiddenMatters(lv,c.solution.route):null;
   const bs=lv.hero?await bridgeSingle(lv):null,bc=c.bridge||null;let bridgeKind=null;
   if(bc){const both=bc.each.every(x=>x.win===true)&&bc.gap>=.1;if(c.intro&&both&&c.random.share>=.9)bridgeKind='first';else if(bs.ok)bridgeKind='single';else if(!c.intro&&both)bridgeKind='better';}
   const bl=bridgeKind==='single'?await bridgeLoss(lv,bs.each.indexOf(false)):null;
   c.checks={forced:await forcedShare(lv),opening,hold:await holdPod(lv),greedyLoses:!st['most-now|left']&&!st['most-now|right'],hiddenMatters:hm,bridgeKind,bridgeSingle:bs,...(bl?{bridgeLoss:bl}:{})};n++;}
  if(!c.checks.pressure){c.checks.pressure=await pressure(lv,QUALITY.pressure.games);n++;}
  if(lv.latches&&!c.checks.latch&&c.solution){c.checks.latch=await latchCheck(lv,c.solution.route,c.cls);n++;}
  // §8 trap links (2026-10-31); the §5 calm test only with --calm (a few seconds a candidate: the placer measures the
  // candidates it weighs and keeps the result, so a whole bank need not be measured)
  if(flag('--calm')&&!c.checks.calm){c.checks.calm=await calmTest(lv,classKeep(c.random));n++;}
  if(!c.checks.traps){const t=trapLinks(lv);c.checks.traps={links:t.length,traps:t.filter(x=>x.trap).length,top:t.filter(x=>x.trap&&x.row<=2).length};n++;}return n;};
 const files=(fs.existsSync(BANK)?fs.readdirSync(BANK):[]).sort().filter((f,i)=>i%sn===si);
 for(const f of files){const bf=path.join(BANK,f),b=readJSON(bf);let changed=0;for(const c of b.candidates)changed+=await fill(c);if(changed)writeJSON(bf,b);console.log(b.picture,'проверок добавлено',changed);}
 if(flag('--levels')&&si===0)for(const f of fs.existsSync(LEVELS)?fs.readdirSync(LEVELS).filter(f=>/^L\d+\.json$/.test(f)):[]){const lf=path.join(LEVELS,f),L=readJSON(lf);if(await fill(L)){writeJSON(lf,L);console.log(f,'проверки дополнены');}}}
else if(cmd!=='noop'){console.log(fs.readFileSync(new URL(import.meta.url),'utf8').split('\n').filter(l=>l.startsWith('//')).join('\n'));}

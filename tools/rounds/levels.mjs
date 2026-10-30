// Round levels from approved pictures (skill bloomlings-round-levels; rules: product/17_LEVEL_GENERATION_RULES.md
// §1, §3–§8, §10–§11, §13–§14).
//
// node tools/rounds/levels.mjs calendar <from> <to>        class, mechanic introduction and allowed mechanics per level
// node tools/rounds/levels.mjs build <pictureId…|--approved> [--classes Normal,Hard,"Super Hard"] [--candidates 64]
//      [--recipes none,links,hidden,links+hidden,bridge,…] [--seed 1] [--fresh: search again a combination already searched]
//      [--overlay up,left,right,two: on a wall the generator lays over the picture, lib.mjs wallOverlay; none = the
//      picture's own board] [--pairs Normal:links,Hard:bridge+links@up: only these class:recipe(@overlay side) pairs]
//      [--no-intro]
//      candidates for each picture, recipe and class: balance → queue → saved solution → random test → bank. Recipes put
//      the lead first (17 §7): none, links, hidden, bridge, links+hidden, hidden+links, bridge+links, bridge+hidden,
//      hidden+bridge, links+bridge, links+bridge+hidden, bridge+links+hidden; a board with a strip takes bridge recipes only,
//      a board without one the others
// node tools/rounds/levels.mjs place <from> <to>           fill calendar positions from the bank (variety rules),
//      write levels and cards, list what is still missing in content/rounds/needs.json; a position of an approved
//      mechanics plan (content/rounds/runs/<wave>/mechanics.json) takes only its planned recipe and bridge side
// node tools/rounds/levels.mjs unplace <from> <to>         put placed levels back into the bank (to place again)
// node tools/rounds/levels.mjs bank                        the bank: candidates by picture and measured class
// node tools/rounds/levels.mjs card <level>                the level card (§14)
// node tools/rounds/levels.mjs checks [--shard i/n] [--levels]  add missing checks (17 §5–§9, §13 layer B) to banked candidates and placed levels
import fs from 'node:fs';import path from 'node:path';
import {repeatWindow,siteChoice,ROUNDS,PICTURES,LEVELS,BANK,CODES,manifest,saveManifest,picture,readJSON,writeJSON,lid,pid,arg,flag,measure,stageOf,fitsStage,sizeBin,budget,columnsFor,classOf,slotOf,INTRO,MECH_RU,SUPPORTED,boardOf,usage,motifsOf,overlaid,bridgeSide,wallOverlay,planOf,fitsPlan,mechanicsPlans} from './lib.mjs';import {execFileSync} from 'node:child_process';
import {fitness} from './fitness.mjs';
import {randomTest,quickShare,solve,fairness,strategies,rhythm,bridgeChoicesWin,bridgeChoices,greedy,greedyBeaten,openingTest,forcedShare,holdPod,hiddenMatters,bridgeSingle,bridgeLoss,pressure} from './measure.mjs';
// layer B of 17 §13: the bounds of the metrics of interest (content/rounds/quality.json)
const QUALITY=readJSON(path.join(ROUNDS,'quality.json'),{pressure:{min:{},games:30,mode:'soft'}});import {engine} from './engine.mjs';
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

// ---- doses (§7, §9): ranges by class and role; a small queue (< 16 pods) has its own table ----
const DOSE_T={links:{lead:[[.1,.3],[.2,.45],[.35,.6]],add:[[.05,.15],[.1,.2],[.15,.25]]},hidden:{lead:[[.05,.15],[.1,.25],[.15,.35]],add:[[.05,.1],[.05,.1],[.05,.15]]}};
function doseRange(m,cls,role,P){const i=CLASSES.indexOf(cls);
 if(P<16)return m==='hidden'?[[1,2],[2,3],[3,4]][i]:m==='links'?[[2,2],[4,4],[4,6]][i]:[1,1];
 const T=DOSE_T;
 if(!T[m])return [1,1];const [a,b]=T[m][role][i];return [Math.ceil(a*P),Math.floor(b*P)];}
// the doses of a recipe as shares of P (17 §7, a queue of 16+ pods), for the wave plan's breakdown: the lead its range
// (three types: its lower half), an additional one its minimum, a bridge one choice of two sites
export function doseShares(recipe,cls){const types=recipe==='none'?[]:recipe.split('+'),i=CLASSES.indexOf(cls);
 return types.map((m,j)=>{const role=j?'add':'lead';if(!DOSE_T[m])return {m,role,choice:1};let [a,b]=DOSE_T[m][role][i];if(j>0)b=a;else if(types.length>=3)b=(a+b)/2;return {m,role,range:[a,b]};});}
export {doses};
// recipe → concrete doses: one type uses its full range, two types: the additional at its minimum, three: the lead in
// the lower half (§7); the introduction levels use the minimum teaching doses (§9)
// the dose cycle of 17 §7 (the owner's amendment of 2026-10-27): the lead dose sits at these places of its range
export const DOSE_CYCLE=[.5,.8,.4,.7,1,.6];
// the dose place a banked candidate was built for, read back from its attempt number (P####-…-k)
export const posOf=(id,recipe,intro)=>{if(intro||(recipe||'none')==='none')return null;const k=+String(id).split('-').pop();return Number.isInteger(k)?DOSE_CYCLE[k%DOSE_CYCLE.length]:null;};
function doses(recipe,cls,P,intro){const types=recipe==='none'?[]:recipe.split('+'),out={};
 types.forEach((m,j)=>{if(m==='bridge'){out.bridge=1;return;}const role=j===0?'lead':'add';let [a,b]=doseRange(m,cls,role,P);
  // the engine plays at most six hidden (depth) pods
  if(m==='hidden'){a=Math.min(a,6);b=Math.min(b,6);}
  // teaching doses (§9): 1 pair of links, 2–3 hidden pods (the owner's second amendment of 2026-10-27)
  // an additional mechanic takes the minimum admissible dose of its range: for links the smallest even count in it (pairs,
  // §7: 10 % of 24 pods is 2.4 → 3 → 4 participants, still inside 10–20 %)
  if(intro){if(m==='hidden'){a=2;b=3;}else a=b=m==='links'?2:1;}else if(j>0){if(m==='links')a+=a%2;b=a<=b?a:b;}else if(types.length>=3){b=Math.floor((a+b)/2);}
  if(m==='links'){a+=a%2;b-=b%2;}if(a>b)out.error=`нет целых доз ${m} для ${cls} при P=${P}`;out[m]=[a,b];});
 return out;}
export function dosesOk(level,cls){const P=level.pods.length,hidden=level.pods.filter(p=>p.mystery).length,linked=level.pods.filter(p=>p.group).length,types=(level.recipe||'none')==='none'?[]:level.recipe.split('+');
 const d=doses(level.recipe||'none',cls,P,false);if(d.error)return d.error;
 if(types.includes('links')&&(linked<d.links[0]||linked>Math.max(d.links[1],d.links[0])))return `связанных ${linked} вне ${d.links.join('–')} для ${cls}`;
 if(types.includes('hidden')&&(hidden<d.hidden[0]||hidden>Math.max(d.hidden[1],d.hidden[0])))return `скрытых ${hidden} вне ${d.hidden.join('–')} для ${cls}`;
 const max=cls==='Normal'?2:3;if(types.length>max)return `типов ${types.length} больше ${max} для ${cls}`;if(P<16&&types.length>1)return 'очередь меньше 16 подов — только один тип';return null;}

// ---- one candidate: order pods by access depth with noise, deal them into columns, add the recipe's mechanics ----
export function candidate(pic,board,base,cols,recipe,cls,seed,intro,pos=null,opts={}){const r=rnd(seed),intended=recipe.includes('bridge')&&board.hero?(k=>Number.isInteger(opts.intended)?opts.intended:k)(Math.floor(r()*board.hero.choices.length)):null,{d,g}=depths(board,intended),pods=base.map(p=>({...p}));
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
 if(cls==='Normal'&&!intro&&(!lean||opts.t!=null)){const t0=r(),t=opts.t??t0;for(const p of pods)p.order=(1-t)*p.order+t*(1-p.key);}
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
 // §6: at the start at least one visible launch has reachable work; else the shallowest pod swaps with a column's top
 const frontier=new Set();for(let i=0;i<g.length;i++)if(CODES.includes(g[i])&&d[i]===0)frontier.add(g[i]);
 if(!tray.some(st=>st.length&&frontier.has(pods[st[0]].color))){const best=ord.filter(i=>frontier.has(pods[i].color)).sort((a,b)=>pods[a].key-pods[b].key)[0];
  if(best!==undefined){const c=Math.floor(r()*cols),[bc,bi]=(()=>{for(let k=0;k<cols;k++){const j=tray[k].indexOf(best);if(j>=0)return [k,j];}})();[tray[c][0],tray[bc][bi]]=[tray[bc][bi],tray[c][0]];}}
 // §6 (the owner's second amendment of 2026-10-27): a Normal keeps a pod of a deep colour (no reachable cell at the
 // start) in the first two rows, so it holds a place until its work opens; the shallowest deep pod moves up to row 2
 if(cls==='Normal'&&!intro&&!pic.locked&&!tray.some(st=>st.slice(0,2).some(p=>!frontier.has(pods[p].color)))){
  const deep=[];tray.forEach((st,c)=>st.forEach((p,row)=>{if(row>=2&&!frontier.has(pods[p].color))deep.push([c,row]);}));deep.sort((a,b)=>pods[tray[a[0]][a[1]]].key-pods[tray[b[0]][b[1]]].key);
  const cs=[...Array(cols).keys()].filter(c=>tray[c].length>=2);if(deep.length&&cs.length){const [dc,dr]=deep[0],c=cs[Math.floor(r()*cs.length)];[tray[c][1],tray[dc][dr]]=[tray[dc][dr],tray[c][1]];}}
 const types=recipe==='none'?[]:recipe.split('+'),dz=doses(recipe,cls,pods.length,intro);if(dz.error)return {error:dz.error};
 if(types.includes('links')){const want=pos===null||intro?dz.links[0]+2*Math.floor(r()*((dz.links[1]-dz.links[0])/2+1)):dz.links[0]+2*Math.round(pos*(dz.links[1]-dz.links[0])/2),pairs=[];const used=new Set();
  // pairs of neighbouring columns at the same depth; harder classes prefer a useful-now pod with a later one (§6.3)
  const opts=[];for(let c=0;c+1<cols;c++)for(let row=0;row<Math.min(tray[c].length,tray[c+1].length);row++)opts.push([tray[c][row],tray[c+1][row],row]);
  opts.sort((x,y)=>(cls==='Normal'?Math.abs(pods[x[0]].key-pods[x[1]].key)-Math.abs(pods[y[0]].key-pods[y[1]].key):Math.abs(pods[y[0]].key-pods[y[1]].key)-Math.abs(pods[x[0]].key-pods[x[1]].key))+(r()-.5)*.2);
  for(const [a,b,row] of opts){if(pairs.length*2>=want)break;if(used.has(a)||used.has(b)||(intro&&row<1))continue;used.add(a);used.add(b);pairs.push([a,b]);}
  if(pairs.length*2<want)return {error:'не хватило пар соседних подов для дозы связок'};pairs.forEach(([a,b],i)=>{const gname=String.fromCharCode(65+i%26)+(i>=26?String(Math.floor(i/26)):'');pods[a].group=gname;pods[b].group=gname;});}
 if(types.includes('hidden')){const want=pos===null||intro||types[0]!=='hidden'?dz.hidden[0]+Math.floor(r()*(dz.hidden[1]-dz.hidden[0]+1)):dz.hidden[0]+Math.round(pos*(dz.hidden[1]-dz.hidden[0]));
  // from the second row down, never in a group (the engine reveals a pod on reaching the top only)
  let opts=tray.flatMap(st=>st.map((p,row)=>[p,row])).filter(([p,row])=>row>=1&&!pods[p].group).sort(()=>r()-.5);
  // an introduction puts its first hidden pod in the second row, to reveal within the first launches (§8)
  if(intro){const k=opts.findIndex(([,row])=>row===1);if(k<0)return {error:'нет подходящего места во втором ряду для скрытого'};opts=[opts[k],...opts.filter((_,i)=>i!==k)];}
  if(opts.length<want)return {error:'не хватило подов для скрытых'};opts.slice(0,want).forEach(([p])=>pods[p].mystery='depth');}
 if(types.includes('bridge')&&!board.hero)return {error:'на картинке нет мест моста (b)'};
 // a strip cuts a part off: without a bridge its cells cannot be reached
 if(board.hero&&!types.includes('bridge'))return {error:'на поле полоса с местами моста — нужен рецепт с мостом'};
 // a level on the generator's wall keeps the wall (picture coordinates), so its board is rebuilt from the approved picture
 const level={id:1,title:pic.title,subject:pic.id,w:board.w,h:board.h,rowsBottomFirst:board.rowsBottomFirst,entries:board.entries,capacity:5,pods:pods.map(({key,keyO,order,...p})=>p),tray,recipe,...(types.includes('bridge')?{hero:board.hero}:{}),...(board.overlay?{overlay:board.overlay}:{}),...(board.strip?{strip:board.strip}:{})};
 return {level};}

// ---- the checks of one candidate level (17 §5–§9, §13 layer B), shared by build and the bridge scene search ----
// Returns {reason} for the first failed check, else the measured parts of a bank entry: random, solution, fairness,
// strategies, rhythm, bridge, checks. kind (the wave plan's bridgeKind, single | better) asks for that bridge kind and
// tests it early (the two solver runs come before the random test). A bridge level is 'first' (an introduction: both
// win, one 10+ points better, 90 %+), 'single' (exactly one bridge wins, proven for the other, and the loss with the
// other comes later through materials and places, not at the choice: bridgeLoss), 'better' (not an introduction: both
// win, one 10+ points better by the random test) or 'any' (single or better). The owner, 2026-10-28 (second decision):
// Normal any, Hard half single and half better, Super Hard single.
export async function evaluate(level,{m,cls,intro=false,fixed=null,kind=null,tick=()=>{}}){const W=m.N,recipe=level.recipe||'none';
 const errs=checkPods(level.pods,m.volumes);if(errs.length)return {reason:'баланс'};
 {const {R}=await engine(),ve=R.validate(level);if(ve&&ve.length)return {reason:'движок отклоняет: '+ve[0]};}
 if(level.hero){const {R}=await engine();if(R.bridgeChoices(R.create(level,level.capacity||5)).length)return {reason:'мост доступен сразу — нужен расчищенный подход (§8)'};}
 // a bridge introduction may be the second one, where one bridge only wins: its target is the Normal zone (§9)
 const q=await quickShare(level,40);tick('быстрая оценка '+q);const target=intro&&recipe==='bridge'?[.5,1]:intro?[.9,1]:fixed===1?[1,1]:fixed?[.9,1]:{Normal:[.5,.75],Hard:[.2,.5],'Super Hard':[.05,.2]}[cls];
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
 const rt=await randomTest(level);tick('случайный тест '+rt.share);if(rt.cls==='too hard')return {reason:'ниже 5 %'};
 if(rt.cls!=='Normal'){const g2=gr||await greedy(level);if(!greedyBeaten(g2,rt.cls))return {reason:'жадная стратегия выигрывает измеренный класс (§5)'};}
 // §5 "opening + colours" (the owner's second amendment of 2026-10-27): no first launch then greedy wins, ≤ 35 % after three
 const opening=rt.cls==='Normal'?null:await openingTest(level,rt.cls);tick('дебют '+JSON.stringify(opening));if(opening&&!opening.ok)return {reason:opening.first.wins?'дебют + цвета: выигрывает после первого запуска (§5)':'дебют + цвета: больше 35 % после трёх запусков (§5)'};
 if(intro&&recipe!=='bridge'&&rt.share<.9)return {reason:'знакомство ниже 90 %'};
 const dErr=dosesOk(level,rt.cls);if(dErr&&!intro)return {reason:'дозы не подходят фактическому классу: '+dErr};
 const fair=level.pods.some(p=>p.mystery)?await fairness(level,sol.route):{fair:true,checked:0};if(!fair.fair)return {reason:'честность скрытых не подтверждена'};
 // §8: on a hidden-pod introduction one hidden pod matters (reveals early with a new colour and is launched soon)
 const hm=level.pods.some(p=>p.mystery)?await hiddenMatters(level,sol.route):null;if(intro&&recipe==='hidden'&&!hm.ok)return {reason:'скрытый под не влияет на выбор (§8)'};
 // a bridge level measures each bridge (§8, §9): the first introduction needs both to win and one clearly better; the
 // second introduction and three quarters of the later bridge levels exactly one winning bridge, the rest both winning
 // with one 10+ points better (the wave plan's bridgeKind decides which a position takes)
 const bc=level.hero?await bridgeChoices(level,60,5,win):null,bs=level.hero?await bridgeSingle(level,60,3,win||bc.each.map(x=>x.win)):null;let bridgeKind=null,bl=null;
 if(bc){const both=bc.each.every(x=>x.win===true)&&bc.gap>=.1;
  if(intro&&both&&rt.share>=.9)bridgeKind='first';else if(bs.ok)bridgeKind='single';else if(!intro&&both)bridgeKind='better';
  else return {reason:intro?'мост: ни «оба выигрывают, один лучше на 10 пунктов при 90 %+», ни «решает ровно один мост» (§8, §9)':'мост: ни «решает ровно один мост», ни «оба выигрывают, один лучше на 10 пунктов» (§8)'};
  if(kind&&kind!=='any'&&bridgeKind!==kind)return {reason:`мост: вид ${bridgeKind}, по плану ${kind} (§8)`};
  // §8: with the other bridge the loss comes later, through materials and places, not at the choice
  if(bridgeKind==='single'){bl=early||await bridgeLoss(level,bs.each.indexOf(false));tick('проигрыш с другим мостом '+JSON.stringify(bl));if(!bl.ok&&cls!=='Super Hard'&&rt.cls!=='Super Hard')return {reason:'мост: с другим мостом проигрыш сразу при выборе (§8)'};}}
 const st=await strategies(level),rh=rhythm(sol.trace,W),greedyLoses=!st['most-now|left']&&!st['most-now|right'],hold=await holdPod(level);
 return {random:rt,solution:{route:sol.route,forks:sol.decisions.forks,singleSafe:sol.decisions.single,unknown:sol.decisions.unknown,fast:sol.fast},fairness:fair,strategies:st,rhythm:rh,...(bc?{bridge:bc}:{}),
  checks:{forced,opening,hold,greedyLoses,hiddenMatters:hm,bridgeKind,bridgeSingle:bs,...(bl?{bridgeLoss:bl}:{}),pressure:await pressure(level,QUALITY.pressure.games)}};}

// a bridge scene (bridge-scene.mjs, a picture whose record carries scene.win): the queue leans to the winning site and
// the gate of the other strip stands deep (candidate opts), one of the last three rows of the tray by attempt
// (a split picture, bridge-scene.mjs make --mode split: its scene.win is the bridge its search leaned to; planned
// 'better', both bridges must win, so the gate stands only in every other attempt and not as deep)
export function sceneOpts(pic,board,P,cols,k){if(!pic.scene||!board.hero||!Array.isArray(pic.scene.win))return {};const rows=Math.ceil(P/cols),i=siteChoice(board,pic.scene.win);
 const gate=pic.scene.mode==='split'&&pic.scene.kind==='better'?(k%2?0:Math.max(2,rows-4)):Math.max(3,rows-1-k%3);
 return {...(gate?{gate}:{}),...(i>=0?{intended:i}:{})};}

// ---- build: candidates for each picture, recipe and class, measured and banked ----
const SIDES=['up','left','right','two'];
async function build(ids){const classes=(arg('--classes','Normal,Hard,Super Hard')).split(','),seed0=+arg('--seed',+(process.env.ROUNDS_SEED||1)),sides=arg('--overlay')?arg('--overlay').split(','):null;
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
  const all=(arg('--recipes')||(fixed?'none':hasBridge?'bridge,bridge+links':'none,links,hidden,links+hidden')).split(','),recipes=all.filter(x=>x.split('+').every(t=>t==='none'||SUPPORTED.has(t))&&x.split('+').includes('bridge')===hasBridge);
  for(const cls of fixed?['Normal']:classes)for(const recipe of recipes){if(fixed&&recipe!=='none')continue;if(pairs&&!pairs.has(`${cls}:${recipe}${v.overlay?'@'+v.side:''}`))continue;
   for(const intro of recipe!=='none'&&!recipe.includes('+')&&cls==='Normal'&&!flag('--no-intro')?[false,true]:[false]){
    // resumable: a combination already searched (bank.searched) is skipped; the bank is saved after each one
    // ROUNDS_FRESH=1 (or --fresh) searches again; ROUNDS_FRESH=<tag> searches again once per tag (a resumable re-search)
    const fresh=process.env.ROUNDS_FRESH,runTag=fresh&&fresh!=='1'?fresh:null;
    const combo=`${cls}|${recipe}|${intro?'intro':''}${v.overlay?'|'+v.side:''}`;bank.searched=bank.searched||[];if(runTag?bank.searched.some(x=>x.combo===combo&&x.tag===runTag):!flag('--fresh')&&!fresh&&bank.searched.some(x=>x.combo===combo)){console.log(id,tag,cls.padEnd(10),recipe.padEnd(13),intro?'знакомство':'','уже искали — пропуск');continue;}
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
     const c=candidate(pic,board,base.pods,cols,recipe,cls,cseed,intro,pos,{...sceneOpts(pic,board,base.pods.length,cols,k),...(blend!==null?{t:blend}:{})});
     if(c.error){reasons[c.error]=(reasons[c.error]||0)+1;continue;}
     let quick=null;const trace=process.env.ROUNDS_TRACE?(l=>console.error(`  ${id} ${cls} ${recipe} #${k}${blend!==null?' t '+blend.toFixed(2):''} ${l} ${((Date.now()-t0)/1000).toFixed(1)} s`)):()=>{},t0=Date.now();
     const tick=l=>{const q=/^быстрая оценка ([\d.]+)/.exec(l);if(q)quick=+q[1];trace(l);};
     const e=await evaluate(c.level,{m,cls,intro,fixed,tick});
     // the steering step shrinks with the attempts (a noisy share: the same t gives 10–90 % on some pictures)
     if(steer){const q=e.random?e.random.share:quick;if(q!==null){steer.n++;steer.t=Math.min(1,Math.max(.3,steer.t+.45/Math.sqrt(steer.n)*(q-steer.aim)));}}
     if(e.reason){no(e.reason);continue;}
     bank.candidates.push({id:`${id}-${cls[0]}${recipe}${v.overlay?'~'+v.side:''}${runTag?'-'+runTag:''}-${k}${intro?'-intro':''}`,picture:id,requested:cls,cls:e.random.cls,intro,recipe,level:c.level,...(v.overlay?{overlay:v.overlay}:{}),...(side?{bridgeSide:side}:{}),N:m.N,W,colours:m.colours,P:c.level.pods.length,columns:cols,
      linked:c.level.pods.filter(p=>p.group).length,hidden:c.level.pods.filter(p=>p.mystery).length,random:e.random,solution:e.solution,fairness:e.fairness,strategies:e.strategies,rhythm:e.rhythm,dosePos:pos,...(blend!==null?{blend:Math.round(blend*1000)/1000}:{}),...(e.bridge?{bridge:e.bridge}:{}),checks:e.checks,engine:'tools/rounds/engine.mjs (pilot v9 + §8 link rule)',seed:seed0,attempt:k});
     found++;if(e.checks.greedyLoses)foundLoss++;}
    console.log(id,tag,cls.padEnd(10),recipe.padEnd(13),intro?'знакомство':'',`найдено ${found} из ${tried}`,Object.keys(reasons).length?JSON.stringify(reasons):'');
    bank.searched.push({combo,...(runTag?{tag:runTag}:{}),...(steer?{steer:{t:Math.round(steer.t*1000)/1000,aim:steer.aim}}:{}),tried,found,reasons});writeJSON(bankFile,bank);}}}
  writeJSON(bankFile,bank);}}

// ---- place: calendar positions from the bank, with the variety rules of §11 and the windows of §2/§13 ----
const typesOf=c=>(c.recipe||'none')==='none'?[]:c.recipe.split('+');
// §7 as amended on 2026-10-27: Super Hard a lead and an additional mechanic once two types are open, Hard a mechanic
// (L5 is built by its queue, §9), a regular Normal a mechanic when needMech
function recipeOk(c,n,slot,needMech){const t=typesOf(c).length;if(slot.intro)return true;
 if(slot.cls==='Super Hard')return t>=Math.min(2,slot.allowed.length);if(slot.cls==='Hard')return n===5||!slot.allowed.length||t>=1;return !needMech||t>=1;}
// §5 (the owner's second amendment of 2026-10-27): a Hard or Super Hard passes "opening + colours" (which includes the
// greedy strategies losing from the start); every level from L4 offers a single launch in at most 35 % of decisions
function greedyOk(c,n){const k=c.checks||{};if(n>=4&&!(k.forced<=.35))return false;if(c.cls==='Normal')return true;return !!(k.opening&&k.opening.ok);}
// §8, §9: the bridge introductions (first: both bridges win, one clearly better; second: exactly one wins), every later
// bridge level the wave plan's bridgeKind (single: exactly one wins and the loss with the other comes later; better: both
// win, one 10+ points better; any: either of the two), single without a plan; a hidden-pod introduction needs a hidden pod that matters
export function mechOk(c,n,slot,plan=null){const k=c.checks||{},second=slot.intro&&INTRO.some(([m,,b])=>m===slot.intro&&b===n);
 if(c.recipe.includes('bridge')){if(slot.intro==='bridge'&&!second)return k.bridgeKind==='first';const want=(!slot.intro&&plan&&plan.bridgeKind)||'single';
  const ok=want==='any'?['single','better'].includes(k.bridgeKind):k.bridgeKind===want;
  return ok&&(k.bridgeKind!=='single'||!k.bridgeLoss||k.bridgeLoss.ok||c.cls==='Super Hard');}
 if(slot.intro==='hidden')return c.hidden>=2&&c.hidden<=3&&!!(k.hiddenMatters&&k.hiddenMatters.ok);return true;}
const greedyLost=L=>{const s=L.strategies||{};return !s['most-now|left']&&!s['most-now|right'];};
// §2 picture fitness: Hard and Super Hard need 3+ colours hidden at the start; a weak picture carries a Normal only with a mechanic
// measured on the board the level plays: a level on the generator's wall is measured with the wall
const fitCache={};const fitOf=c=>{const ov=c.overlay||(c.level||c.definition||{}).overlay||null,k=c.picture+(ov?JSON.stringify(ov):'');return fitCache[k]||(fitCache[k]=fitness(overlaid(picture(c.picture),ov)));};
function fitOk(c,n){if(n<=3)return true;const f=fitOf(c),weak=f.hiddenColours<=1||f.visible>=6;
 if(c.cls!=='Normal')return f.hiddenColours>=3;return !weak||typesOf(c).length>=1;}
// a regular Normal for the median of §5: not L1–3, not an introduction, not the first Normal after a Hard or Super Hard
function regularNormal(L,placed){const p=placed[L.level-1];return L.cls==='Normal'&&L.level>3&&!L.intro&&!(p&&p.cls!=='Normal');}
const lead=c=>(c.recipe||'none').split('+')[0];
export function reservedFamily(id,n){const w=mechanicsPlans().find(p=>n>=p.wave[0]&&n<=p.wave[1]);return !!(w&&(w.bridgeFamilies||[]).includes(picture(id).family));}
export function repeats(id,n,placed){const p=picture(id),[lo,hi]=repeatWindow(n),mo=motifsOf(p);
 return Object.values(placed).some(L=>L.level!==n&&L.level>=lo&&L.level<=hi&&L.picture!==id&&(()=>{const q=picture(L.picture);return (p.family&&q.family===p.family)||motifsOf(q).some(m=>mo.includes(m));})());}
function place(from,to){const placed={};for(const f of fs.existsSync(LEVELS)?fs.readdirSync(LEVELS).filter(f=>/^L\d+\.json$/.test(f)):[]){const L=readJSON(path.join(LEVELS,f));placed[L.level]=L;}
 const banks=fs.existsSync(BANK)?fs.readdirSync(BANK).map(f=>readJSON(path.join(BANK,f))):[];const pool=banks.flatMap(b=>b.candidates);const m=manifest(),usedPic=new Set(Object.keys(usage()));const needs=[];
 const plans=mechanicsPlans();
 for(let n=from;n<=to;n++){if(placed[n])continue;const slot=slotOf(n),prev=[placed[n-1],placed[n-2]].filter(Boolean);
  // an approved mechanics plan (17 §13 step 1, amendment of 2026-10-28) fixes the position's recipe and bridge side
  const plan=planOf(n,plans);
  const window=Object.values(placed).filter(L=>L.level>n-20&&L.level<n+20);
  // Normal after the introductions: a mechanic in two of three (17 §7); a third may go without when the last two had one
  const lastRegular=Object.values(placed).filter(L=>L.level<n&&L.cls==='Normal'&&!L.intro&&slotOf(L.level).allowed.length).sort((a,b)=>b.level-a.level).slice(0,2);
  const needMech=!plan&&slot.cls==='Normal'&&!slot.intro&&slot.allowed.length>0&&!(lastRegular.length===2&&lastRegular.every(L=>(L.recipe||'none')!=='none'));
  const regularSlot=slot.cls==='Normal'&&n>3&&!slot.intro&&!(placed[n-1]&&placed[n-1].cls!=='Normal');
  const ok=pool.filter(c=>!usedPic.has(c.picture)&&c.cls===slot.cls&&fitsStage({N:c.N,colours:c.colours},n)
   &&(slot.intro?(c.intro&&c.recipe===slot.intro):!c.intro&&(c.recipe==='none'||c.recipe.split('+').every(t=>slot.allowed.includes(t))))
   &&((picture(c.picture).locked||null)===(n<=3?n:null))
   // §11: no lead mechanic or identical recipe 3 times in a row; the two teaching appearances do not count
   // (the owner, 2026-10-29: links dominate, so a links lead and links+hidden may repeat any number of times)
   &&!(prev.length===2&&!slot.intro&&lead(c)!=='none'&&lead(c)!=='links'&&prev.every(L=>!L.intro&&lead(L)===lead(c)))
   &&!(prev.length===2&&!slot.intro&&c.recipe!=='links+hidden'&&prev.every(L=>!L.intro&&L.recipe===c.recipe&&c.recipe!=='none'))
   // picture repeats (the owner, 2026-10-29): family and motif once per wave and not in the 20 levels before it
   &&!repeats(c.picture,n,placed)
   // the wave plan keeps some families for its bridge levels (bridgeFamilies): other positions leave them
   &&!(plan&&!plan.bridgeSide&&reservedFamily(c.picture,n))
   &&!(c.colours>=10&&window.filter(L=>L.colours>=10).length>=2)
   // motif variety (the owner, 2026-10-26): a motif at most once in any 10 levels
   &&!motifsOf(picture(c.picture)).some(m=>Object.values(placed).some(L=>Math.abs(L.level-n)<10&&motifsOf(picture(L.picture)).includes(m)))
   // the owner's amendment of 2026-10-27: recipes by class (§7), greedy strategies (§5), picture fitness (§2)
   &&recipeOk(c,n,slot,needMech)&&greedyOk(c,n)&&fitOk(c,n)&&mechOk(c,n,slot,plan)
   // §6: a regular Normal holds a place with a deep-colour pod in its first two rows
   // (a bridge level's task is the bridge choice; a gap split can leave no colour off the start front, so a bridge
   // level does not need the holding pod, 2026-10-29)
   &&!(regularSlot&&!c.recipe.includes('bridge')&&!(c.checks&&c.checks.hold))
   &&fitsPlan(c,plan));
  // preference: the preferred zone of the class (first Normal after Hard/Super Hard: 75–95 %), then the size mix per 30
  // Normal: the first after a Hard or Super Hard 75–95 %; otherwise steered so the median of the regular Normals stays 50–60 %
  const afterHard=prev[0]&&prev[0].cls!=='Normal',regular=Object.values(placed).filter(L=>regularNormal(L,placed)).map(L=>L.random.share).sort((a,b)=>a-b);
  const medN=regular.length?regular[Math.floor((regular.length-1)/2)]:.55,high=Object.values(placed).filter(L=>regularNormal(L,placed)&&L.level>n-10&&L.random.share>=.95).length;
  const zone=slot.cls==='Normal'?(afterHard?[.75,.95]:medN>.6?[.5,.62]:[.52,.75]):slot.cls==='Hard'?[.25,.4]:[.07,.15];
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
  // §5: "most work now" loses about one in three regular Normals on L4–24 and one in two from L25 (a preference of 0.6,
  // about 25 share points: a greedy-losing candidate far above the aim would push the Normal median out of 50–60 %)
  const regs=Object.values(placed).filter(L=>regularNormal(L,placed)),lostN=regs.filter(greedyLost).length,q=n<25?1/3:1/2,needLoss=regularSlot&&lostN<Math.round(q*(regs.length+1));
  // layer B (17 §13, quality.json): pods without work at least the class's bound; soft: a preference, hard: a filter
  const pMin=slot.intro?null:regularSlot?QUALITY.pressure.min.regularNormal:slot.cls!=='Normal'?QUALITY.pressure.min[slot.cls]:null;
  const pressureLow=c=>pMin!=null&&!(c.checks&&c.checks.pressure&&c.checks.pressure.stuck>=pMin);
  const normalScore=c=>introScore(c)+(pressureLow(c)?.5:0)+(slot.cls!=='Normal'||slot.intro?0:(c.random.share>=.95&&high>=1?.6:0)+(!needMech&&c.recipe!=='none'?.2:0)+(needLoss&&!greedyLost(c)?.6:0));
  // a regular Normal aims at 55 % itself (not just anywhere in the zone), so the median lands in 50–60 %
  const aim=slot.cls==='Normal'&&!slot.intro&&!afterHard?(medN>.6?.52:medN<.5?.58:.55):null;
  const score=c=>{const s=c.random.share;return doseScore(c)+normalScore(c)+scarce(c)+(c.random.borderline?1:0)+(aim!==null?2*Math.abs(s-aim):s>=zone[0]&&s<=zone[1]?0:Math.min(Math.abs(s-zone[0]),Math.abs(s-zone[1])))+(bins.filter(b=>b===sizeBin(c.N)).length>=want[sizeBin(c.N)]?.3:0);};
  if(QUALITY.pressure.mode==='hard')for(let i=ok.length-1;i>=0;i--)if(pressureLow(ok[i]))ok.splice(i,1);
  ok.sort((a,b)=>score(a)-score(b));const c=ok[0];
  if(process.env.ROUNDS_TRACE)console.error(lid(n),'кандидатов',ok.length,ok.slice(0,6).map(c=>`${c.picture} ${c.recipe} ${Math.round(c.random.share*100)}% поз ${c.dosePos??'—'} балл ${score(c).toFixed(2)}`).join(' | '));
  if(!c){needs.push({level:n,cls:slot.cls,intro:slot.intro,allowed:slot.allowed,stage:stageOf(n),...(plan?{recipe:plan.recipe,...(plan.bridgeSide?{bridgeSide:plan.bridgeSide,strip:plan.strip||null,bridgeKind:plan.bridgeKind||'single'}:{})}:{})});continue;}
  const L={level:n,cls:slot.cls,intro:slot.intro,picture:c.picture,candidate:c.id,recipe:c.recipe,N:c.N,W:c.W,colours:c.colours,P:c.P,columns:c.columns,linked:c.linked,hidden:c.hidden,random:c.random,solution:c.solution,fairness:c.fairness,strategies:c.strategies,rhythm:c.rhythm,dosePos:c.dosePos??null,...(c.bridge?{bridge:c.bridge,bridgeSide:c.bridgeSide??bridgeSide(c.level)}:{}),checks:c.checks,engine:c.engine,definition:{...c.level,id:n,difficulty:slot.cls}};
  writeJSON(path.join(LEVELS,lid(n)+'.json'),L);placed[n]=L;usedPic.add(c.picture);
  const e=m.pictures.find(p=>p.id===c.picture);if(e)e.usedBy=lid(n);const pf=path.join(PICTURES,c.picture+'.json'),pj=readJSON(pf);pj.usedBy=lid(n);writeJSON(pf,pj);
  for(const b of banks)if(b.picture===c.picture){b.candidates=b.candidates.filter(x=>x.id!==c.id);b.placed=(b.placed||[]).concat([c.id]);writeJSON(path.join(BANK,b.picture+'.json'),b);}
  console.log(lid(n),slot.cls.padEnd(10),slot.intro?'знакомство: '+MECH_RU[slot.intro]:'',c.picture,c.recipe,L.bridgeSide?`мост ${L.bridgeSide}${c.level.overlay?' (стена)':''}`:'',`случайный тест ${Math.round(c.random.share*100)} %`);}
 saveManifest(m);execFileSync(process.execPath,[new URL('./pictures.mjs',import.meta.url).pathname,'publish'],{stdio:'inherit',env:process.env});const nf=path.join(ROUNDS,'needs.json'),old=readJSON(nf,{needs:[]});const keep=old.needs.filter(x=>x.level<from||x.level>to);writeJSON(nf,{updated:new Date().toISOString().slice(0,10),needs:keep.concat(needs).sort((a,b)=>a.level-b.level)});
 if(needs.length)console.log(`не заполнено ${needs.length}: ${needs.map(x=>`L${x.level} ${x.cls}${x.intro?' ('+x.intro+')':''}${x.recipe?' '+x.recipe+(x.bridgeSide?' '+x.bridgeSide+(x.strip?'/'+x.strip:''):''):''}`).join(', ')} → content/rounds/needs.json`);}

function card(n){const L=readJSON(path.join(LEVELS,lid(n)+'.json'));if(!L)return console.log('нет уровня',n);
 console.log(`${lid(n)} · ${L.cls}${L.intro?' · знакомство: '+MECH_RU[L.intro]:''} · картинка ${L.picture} «${L.definition.title}»\nN ${L.N} · W ${L.W} · материалов ${L.colours} · подов ${L.P} · колонок ${L.columns} · мест ${L.definition.capacity}\nрецепт: ${L.recipe} (связанных ${L.linked}, скрытых ${L.hidden})${L.definition.hero?` · мост: ${bridgeSide(L.definition)} (${L.definition.hero.choices.map(c=>c.label).join(', ')})${L.definition.overlay?', стена генератора поверх картинки':''}`:''}\nслучайный тест: ${Math.round(L.random.share*100)} % из ${L.random.runs}, Wilson95 ${L.random.wilson95.map(x=>Math.round(x*100)+' %').join('–')}\nрешение: ${L.solution.route.length} действий, опасных развилок ${L.solution.forks}, с одним безопасным ${L.solution.singleSafe}\nпосле 80 % W запусков: ${L.rhythm.launchesAfter80}, последний при ${L.rhythm.lastLaunchAtW} % W, время решения ${L.rhythm.seconds} с (×1, после окончания работы), тапы сразу: ${L.solution.fast?L.solution.fast.seconds+' с ('+L.solution.fast.status+')':'—'}\nэтапы: ${L.rhythm.segments.map(s=>`${s.from}–${s.to}%: своб. ${s.meanFree}/${s.minFree}, полный ${Math.round((s.fullShare||0)*100)} %`).join(' · ')}\nпростые стратегии: выиграли ${Object.values(L.strategies).filter(Boolean).length} из ${Object.keys(L.strategies).length}; «больше работы сейчас» ${greedyLost(L)?'проигрывает':'выигрывает'}\nвынужденных решений: ${L.checks?Math.round(L.checks.forced*100)+' %':'—'}${L.checks&&L.checks.opening?` · дебют + цвета: после первого запуска ${L.checks.opening.first.wins} из ${L.checks.opening.first.openings}, после трёх ${Math.round(L.checks.opening.three.share*100)} %`:''}${L.checks&&L.checks.hold!=null?' · под держит место: '+(L.checks.hold?'да':'нет'):''}${L.checks&&L.checks.pressure?' · стоят без работы: '+L.checks.pressure.stuck:''}${L.checks&&L.checks.bridgeKind?' · мост: '+(L.checks.bridgeKind==='single'?'решает ровно один':'оба выигрывают, один лучше'):''}`);}

if(cmd==='calendar'){const a=+process.argv[3]||1,b=+process.argv[4]||30;for(let n=a;n<=b;n++){const s=slotOf(n);console.log(lid(n),s.cls.padEnd(10),s.intro?('знакомство: '+MECH_RU[s.intro]+(SUPPORTED.has(s.intro)?'':' — НЕ ПОДДЕРЖИВАЕТСЯ ДВИЖКОМ')).padEnd(46):''.padEnd(46),'разрешены:',s.allowed.map(x=>MECH_RU[x]+(SUPPORTED.has(x)?'':' (нет в движке)')).join(', ')||'без допов');}}
else if(cmd==='build'){const ids=flag('--approved')?(u=>manifest().pictures.filter(p=>p.status==='approved'&&!u[p.id]).map(p=>p.id))(usage()):process.argv.slice(3).filter(a=>/^P\d+$/.test(a));await build(ids);}
else if(cmd==='place'){place(+process.argv[3],+process.argv[4]);}
else if(cmd==='unplace'){const a=+process.argv[3],b=+process.argv[4],m=manifest();
 // puts the placed candidates of levels a–b back into their banks and frees their pictures (to place again)
 for(let n=a;n<=b;n++){const f=path.join(LEVELS,lid(n)+'.json'),L=readJSON(f);if(!L)continue;const bf=path.join(BANK,L.picture+'.json'),bank=readJSON(bf,{picture:L.picture,candidates:[],rejected:[]});
  const {id,difficulty,...level}=L.definition;bank.candidates.push({id:L.candidate,picture:L.picture,requested:L.cls,cls:L.cls,intro:!!L.intro,recipe:L.recipe,level:{id:1,...level},N:L.N,W:L.W,colours:L.colours,P:L.P,columns:L.columns,linked:L.linked,hidden:L.hidden,random:L.random,solution:L.solution,fairness:L.fairness,strategies:L.strategies,rhythm:L.rhythm,dosePos:L.dosePos??posOf(L.candidate,L.recipe,L.intro),...(L.bridge?{bridge:L.bridge}:{}),...(level.overlay?{overlay:level.overlay}:{}),...(L.bridgeSide?{bridgeSide:L.bridgeSide}:{}),checks:L.checks,engine:L.engine});
  bank.placed=(bank.placed||[]).filter(x=>x!==L.candidate);writeJSON(bf,bank);fs.unlinkSync(f);
  const e=m.pictures.find(p=>p.id===L.picture);if(e)delete e.usedBy;const pf=path.join(PICTURES,L.picture+'.json'),pj=readJSON(pf);delete pj.usedBy;writeJSON(pf,pj);console.log(lid(n),'снят:',L.picture,L.candidate);}
 saveManifest(m);execFileSync(process.execPath,[new URL('./pictures.mjs',import.meta.url).pathname,'publish'],{stdio:'inherit',env:process.env});}
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
  if(!c.checks.pressure){c.checks.pressure=await pressure(lv,QUALITY.pressure.games);n++;}return n;};
 const files=(fs.existsSync(BANK)?fs.readdirSync(BANK):[]).sort().filter((f,i)=>i%sn===si);
 for(const f of files){const bf=path.join(BANK,f),b=readJSON(bf);let changed=0;for(const c of b.candidates)changed+=await fill(c);if(changed)writeJSON(bf,b);console.log(b.picture,'проверок добавлено',changed);}
 if(flag('--levels')&&si===0)for(const f of fs.existsSync(LEVELS)?fs.readdirSync(LEVELS).filter(f=>/^L\d+\.json$/.test(f)):[]){const lf=path.join(LEVELS,f),L=readJSON(lf);if(await fill(L)){writeJSON(lf,L);console.log(f,'проверки дополнены');}}}
else if(cmd!=='noop'){console.log(fs.readFileSync(new URL(import.meta.url),'utf8').split('\n').filter(l=>l.startsWith('//')).join('\n'));}

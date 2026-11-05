// The round runner (product/17_LEVEL_GENERATION_RULES.md §13; skill bloomlings-round-wave): one entry point over
// pictures.mjs, levels.mjs, audit.mjs and the pilot, so a wave of levels runs as scripts, not by hand.
//
// node tools/rounds/rounds.mjs plan <from> <to>
//      the positions of the wave (class, introduction, stage, allowed mechanics), what the bank and the free approved
//      pictures already cover, and the picture order for the rest → content/rounds/runs/L####-L####/plan.{md,json}.
//      With the owner-approved mechanics breakdown runs/L####-L####/mechanics.json (17 §13 step 1): the breakdown first
//      (per level lead, additional, doses, bridge side and strip; shares per mechanic), then the order for what it needs:
//      new strip pictures by crossing side and size stage, pool pictures for the rest (an overlay level takes a free
//      pool picture with the generator's wall; a gap level a split of a free pool picture, bridge-scene.mjs make --mode
//      split, listed with its command and suggested sources); the pinned pictures of mechanics.json `pin` reserved for fitting positions
//      (plan.json pins), the free pool by picture pack, the pictures with a hole for the acorn key the latch positions need
// node tools/rounds/rounds.mjs plan <from> <to> [--reassign]: under a mechanics plan also the picture-first assignment (wave.mjs; the owner's
//      principle 17): one free picture per open non-bridge position by the size mix, spread evenly → plan.json assign
// node tools/rounds/rounds.mjs run <from> <to> [--jobs 3] [--limit N] [--minutes M] [--swaps 2] [--sweep]: picture-first, each assigned
//      position builds exactly its picture × class × recipe (a failed picture swapped within its size band); --sweep the old path below
// node tools/rounds/rounds.mjs run <from> <to> [--jobs 4] [--candidates 64] [--no-build] [--no-sample] [--no-bridge] [--per-position N] [--all]
//      dry placement on a copy; candidates only for what stays open (one process per picture, resumable: a combination
//      already searched is skipped; under a mechanics plan only each position's planned recipe, on a wall of the planned
//      side for an overlay position, on pictures with their own strip of that side for a new-strip one); missing checks;
//      placement (a planned position takes only its recipe and bridge side); audit; summary; the owner's sample as a pilot
//      → content/rounds/runs/L####-L####/{summary.md,audit.md,sample/,build/}. Exit code 1 when a position stays open
//      or a mandatory rule fails.
// node tools/rounds/rounds.mjs review <results.json> [--preview <folder>]
//      the owner's pilot results next to the metrics: ratings, notes, mismatches with quality.json, a draft for
//      content/rounds/owner-notes.md → content/rounds/reports/<pilot>-review-<date>.md; a compact copy of the results
//      goes to product/playtests/<pilot>-owner-<date>/results.json
// node tools/rounds/rounds.mjs bridges <from> <to>
//      which bridge sites each bridge level of the wave shows over the cubes from its start (17 §8 as amended on
//      2026-10-31): Normal both, Hard alternating both / none in calendar order, Super Hard none but a proven losing
//      bridge as a lure → the level's bridgeShown (run by run after the placement; export.mjs needs it)
// node tools/rounds/rounds.mjs confirm <from> <to> --sample <results.json> [--date YYYY-MM-DD]
//      the owner's confirmation of the wave's sample, played in the pilot on the current engine (spec 006 FR-050):
//      → content/rounds/runs/L####-L####/confirmed.json, the entry check of tools/rounds/export.mjs
// node tools/rounds/rounds.mjs confirm <from> <to> --owner-word "<the owner's words>" [--date YYYY-MM-DD]
//      the owner told us to move the wave into the game before playing its sample: the words are recorded as they were
//      said, with no sample (sample [], results null); the phone check after publishing is then the first play
import fs from 'node:fs';import path from 'node:path';import os from 'node:os';import {spawn,execFileSync} from 'node:child_process';
import {franchiseBlocked,packOf,interleaveByPack,root,ROUNDS,LEVELS,BANK,readJSON,writeJSON,lid,arg,flag,manifest,picture,measure,stageOf,fitsStage,sizeBin,slotOf,classOf,INTRO,MECH_RU,SUPPORTED,recipeAllowed,usage,motifsOf,boardOf,overlaid,bridgeSide,wallOverlay,mechanicsPlans,planOf,bridgeShowing} from './lib.mjs';
import {fitness} from './fitness.mjs';
import {fingerprints,sameEngine} from './conformance.mjs';
import {assignPictures,replacement,bandsOf,bandCounts,isBreather,recipeFitsPods,latchFits,SIZE_MIX,BREATHER_N,BREATHER_ZONE,REGULAR_ZONE,GEN,repeatClash,wideClash,rebuildSet,medianWindows,median,greedyQuota,MEDIAN} from './wave.mjs';
const cmd=process.argv[2],HERE=path.dirname(new URL(import.meta.url).pathname),NODE=process.execPath;
process.argv.splice(2,0,'noop');const LV=await import('./levels.mjs');const BS=await import('./bridge-scene.mjs');process.argv.splice(2,1);
const PILOT=path.join(root,'tools/gameplay-pilot-v12/build.mjs');
const QUALITY=readJSON(path.join(ROUNDS,'quality.json'),{pressure:{min:{}},sample:{},mismatch:{noteWords:[]}});
const pct=x=>x==null?'—':Math.round(x*100)+' %';
const waveDir=(a,b)=>path.join(ROUNDS,'runs',`${lid(a)}-${lid(b)}`);
const placedLevels=()=>{const o={};if(fs.existsSync(LEVELS))for(const f of fs.readdirSync(LEVELS).filter(f=>/^L\d+\.json$/.test(f))){const L=readJSON(path.join(LEVELS,f));o[L.level]=L;}return o;};
// a picture usable for rounds: approved, not locked to L1–3, not a franchise reference (unless the owner confirmed its
// rights: rights 'owner', pictures.mjs rights), not yet in a round
function freePictures(){const u=usage();return manifest().pictures.filter(e=>e.status==='approved'&&!u[e.id]).map(e=>picture(e.id)).filter(p=>p&&!p.locked&&!franchiseBlocked(p));}
// latch + acorn key (17 §8): how many holes for a key a picture's board offers (levels.mjs keyHoles: empty cells closed in
// by colour work, reached without a bridge)
// the keys a picture can take (17 §8): its holes, or up to three where the level may carve the missing key spots out of it
// (the owner's amendment of 2026-11-03, levels.mjs carveKeySpots)
const holeCache=new Map(),holesOf=p=>{if(!holeCache.has(p.id)){let k=0;try{k=LV.keyHoles(boardOf(p)).length;if(k<3)for(let n=3;n>k;n--){if(LV.carveKeySpots(p,null,n,()=>.5)!==false){k=n;break;}}}catch{k=0;}holeCache.set(p.id,k);}return holeCache.get(p.id);};
// the latch/key pairs a planned recipe needs at least (levels.mjs doses: Super Hard lead 2, otherwise 1)
const latchNeed=(recipe,cls,intro)=>recipe.split('+').includes('latch')?LV.doses(recipe,cls,24,!!intro,700).latch[0]:0;
// a free picture fits a planned position: its stage; a bridge position only a picture with its own strip crossing that
// side (strip new), other positions a picture without a strip; Hard and Super Hard a strong picture (3+ colours hidden at
// the start); a latch recipe enough holes for its keys
function fitsPosition(p,q){const pl=q.plan;if(!pl||q.level<=3||!p.fits.includes(q.level))return false;const bridge=pl.recipe.split('+').includes('bridge');
 if(bridge?!(p.stream&&p.side===pl.bridgeSide&&(pl.strip||'new')==='new'):p.stream)return false;
 if(q.cls!=='Normal'&&!p.strong3)return false;
 // a queue under 16 pods takes one mechanic type only (17 §7, levels.mjs dosesOk): a two-type recipe needs a picture with 16+ pods
 if(!recipeFitsPods(pl.recipe,podsOf(p.pic)))return false;if(!latchFits(pl.recipe,q.cls,podsOf(p.pic),p.N<1000?3:4))return false;return holesOf(p.pic)>=latchNeed(pl.recipe,q.cls,q.intro);}
// the pods of a picture by §3 (levels.mjs splitPods; 0 when its volumes do not split)
const podCache=new Map(),podsOf=p=>{if(!podCache.has(p.id)){const m=measure(p),b=LV.splitPods(m.volumes,m.N,null);podCache.set(p.id,b.error?0:b.pods.length);}return podCache.get(p.id);};
// pinned pictures (mechanics.json `pin`, the owner): each reserved for one fitting open position of the wave, the pins with
// the fewest fitting positions first; a position another pin also fits, a neighbour of a pin of the same pack, or (for a
// strong picture) a Normal position costs a little. status: free (to build and place), placed (already in this wave),
// used (in another level), blocked (not approved, locked, a franchise without the owner's rights, no fitting position)
// (prev: picture → level of the earlier plan's assignment, kept when that position is still open and fits: a pin taken off
// by a rebuild returns to its own position)
function reservePins(ids,open,pics,placed,a,b,prev=new Map()){const by=new Map(pics.map(p=>[p.id,p])),out=[];
 for(const id of ids){const pic=picture(id),e={id,title:pic?.title||null,pack:pic?packOf(pic):null,positions:[],reserved:null,status:'free',reason:null};out.push(e);
  if(!pic){Object.assign(e,{status:'blocked',reason:'нет картинки'});continue;}
  const at=Object.values(placed).find(L=>L.picture===id);if(at){Object.assign(e,at.level>=a&&at.level<=b?{status:'placed',reserved:at.level}:{status:'used',reason:`уже в ${lid(at.level)}`});continue;}
  if(pic.status!=='approved'){Object.assign(e,{status:'blocked',reason:`не утверждена (${pic.status})`});continue;}
  if(pic.locked){Object.assign(e,{status:'blocked',reason:`закреплена за L${pic.locked}`});continue;}
  if(franchiseBlocked(pic)){Object.assign(e,{status:'blocked',reason:`${pic.source} без подтверждённых прав (pictures.mjs rights ${id} --owner "…")`});continue;}
  const p=by.get(id);e.positions=p?open.filter(q=>fitsPosition(p,q)).map(q=>q.level):[];
  if(!e.positions.length)Object.assign(e,{status:'blocked',reason:'не подходит ни одной открытой позиции волны (этап, класс, рецепт, место для ключа)'});}
 const taken=new Map();
 for(const e of out.filter(e=>e.status==='free').sort((x,y)=>x.positions.length-y.positions.length||x.id.localeCompare(y.id))){
  const p=by.get(e.id),strong=p.strong3,holes=holesOf(p.pic)>0,latchPos=n=>{const q=open.find(q=>q.level===n);return !!(q&&q.plan&&q.plan.recipe.split('+').includes('latch'));},anyLatch=e.positions.some(latchPos);
  const hash=n=>((n*2654435761)^[...e.id].reduce((h,c)=>h*31+c.charCodeAt(0),7))>>>0;
  const cost=n=>(prev.get(e.id)===n?-100:0)+.1*out.filter(o=>o!==e&&o.status==='free'&&!o.reserved&&o.positions.includes(n)).length+([n-1,n+1].some(k=>taken.has(k)&&taken.get(k).pack===e.pack)?5:0)+(strong&&classOf(n)==='Normal'?.5:0)
   +(holes&&anyLatch&&!latchPos(n)?1:0)+(slotOf(n).intro?.6:0)+[...taken.keys()].reduce((c,k)=>c+(Math.abs(k-n)<=3?.4:0),0)+(hash(n)%1000)/1e4;
  // the hard rules of the assignment (families and motifs twice a wave 10 apart, the 10+ colour window) against the placed
  // levels and the pins reserved before it
  const pc=picture(e.id),pe={family:pc.family||null,motifs:motifsOf(pc),colours:measure(pc).colours},live=[...Object.values(placed).filter(L=>L.level>=a-20&&L.level<=b).map(L=>{const q=picture(L.picture);return {level:L.level,family:(q&&q.family)||null,motifs:q?motifsOf(q):[],colours:q?measure(q).colours:0};}),
   ...[...taken].map(([k,o])=>{const q=picture(o.id);return {level:k,family:(q&&q.family)||null,motifs:q?motifsOf(q):[],colours:q?measure(q).colours:0};})];
  const free=e.positions.filter(n=>!taken.has(n)&&!repeatClash(pe,n,live,[a,b])&&!wideClash(n,pe.colours,live)).sort((x,y)=>cost(x)-cost(y)||x-y);
  if(!free.length){Object.assign(e,{status:'blocked',reason:`подходящие позиции (${e.positions.map(n=>'L'+n).join(', ')}) заняты другими закреплёнными или нарушили бы повтор семейства, мотива или окно 10+ цветов`});continue;}
  e.reserved=free[0];taken.set(free[0],e);}
 return out;}
// ---- picture-first (wave.mjs) ----
// the slot of an assigned position: an introduction, Hard / Super Hard, a breather (17 §5: the plan's mark or a picture up
// to ~550 cells), the first Normal after a Hard or Super Hard, else a regular Normal; every second regular Normal (every
// third before L25) also looks for a level "most work now" loses (17 §5 «по цветам»)
const slotOfPos=(q,N)=>q.intro?'intro':q.cls!=='Normal'?q.cls:isBreather({...q,breather:!!(q.plan&&q.plan.breather)},N)?'breather':classOf(q.level-1)!=='Normal'?'after-hard':'regular';
// the free pool as wave.mjs reads it (no strip; the stage of each open position it fits)
let PF_POOL=null;const pfPool=pics=>PF_POOL||(PF_POOL=pics.filter(p=>!p.stream&&p.fits.length).map(p=>({id:p.id,title:p.title,N:p.N,colours:p.colours,pack:packOf(p.pic)||'—',hidden:fitness(p.pic).hiddenColours,weak:p.weak,holes:holesOf(p.pic),P:podsOf(p.pic),family:p.family,motifs:p.motifs,fits:new Set(p.fits)})));
const posOfQ=q=>({level:q.level,cls:q.cls,intro:!!q.intro,recipe:q.plan.recipe,breather:!!q.plan.breather,accents:!!q.plan.accents,after:q.cls==='Normal'&&classOf(q.level-1)!=='Normal',latchNeed:latchNeed(q.plan.recipe,q.cls,q.intro)});
const isHandPos=q=>!q.plan||q.plan.by==='hand'||!!q.plan.bridgeSide||q.plan.recipe.split('+').includes('bridge');
function pictureFirst(a,b,open,pics,pins,placed){const pool=pfPool(pics),dir=waveDir(a,b),prev=flag('--reassign')?null:readJSON(path.join(dir,'plan.json'),null);
 const positions=open.filter(q=>q.level>3&&!isHandPos(q)),hand=open.filter(q=>q.level>3&&isHandPos(q)).map(q=>q.level),poolIds=new Set(pool.map(p=>p.id));
 const pinned=new Map(pins.filter(e=>e.status==='free'&&e.reserved).map(e=>[e.reserved,e.id]));
 const picOf=id=>{const p=picture(id);return p?{id,family:p.family||null,motifs:motifsOf(p),pack:packOf(p),colours:measure(p).colours}:{id};};
 const before=Object.values(placed).filter(L=>L.level>=a-20&&L.level<=b).map(L=>({level:L.level,picture:L.picture}));
 // the earlier assignment, kept while its picture is still free (resumable runs) and still within the hard rules of the
 // assignment (families and motifs twice a wave 10 apart, the 10+ colour window; wave.mjs) next to the placed levels, the
 // pins and the assignments kept before it; a position the rebuild sends to another picture (rebuild.json reassign) is
 // drawn anew. Kept ones are treated like a pin
 const reb=readJSON(path.join(dir,'rebuild.json'),{levels:[]}),reassign=new Set(reb.levels.filter(x=>x.reassign&&!x.done).map(x=>x.level));
 const ent=id=>{const p=picOf(id);return {family:p.family||null,motifs:p.motifs||[],colours:p.colours||0};},live=[...before.map(x=>({level:x.level,...ent(x.picture)})),...[...pinned].map(([n,id])=>({level:n,...ent(id)}))];
 const keep=new Map();for(const x of ((prev&&prev.assign)||[]).slice().sort((u,v)=>u.level-v.level)){if(!positions.some(q=>q.level===x.level)||!poolIds.has(x.picture)||pinned.has(x.level)||[...pinned.values()].includes(x.picture)||reassign.has(x.level))continue;
  const e=ent(x.picture);if(repeatClash(e,x.level,live,[a,b])||wideClash(x.level,e.colours,live))continue;keep.set(x.level,x);live.push({level:x.level,...e});}
 for(const [n,x] of keep)pinned.set(n,x.picture);
 // a position the rebuild sends to another picture keeps its size band (as a replacement does)
 const prevBand=new Map(((prev&&prev.assign)||[]).map(x=>[x.level,x.band]));
 const r=assignPictures(positions.map(q=>({...posOfQ(q),...(reassign.has(q.level)&&prevBand.get(q.level)?{band:prevBand.get(q.level)}:{})})),pool,{pinned,before,picOf,waveStart:a,waveEnd:b});
 const byId=new Map(pool.map(p=>[p.id,p]));
 const assign=r.assign.map(x=>{const q=positions.find(q=>q.level===x.level),p=byId.get(x.picture),old=keep.get(x.level),slot=slotOfPos(q,p.N),was=((prev&&prev.assign)||[]).find(y=>y.level===x.level);
  const tried=old?old.tried:was&&was.picture!==x.picture?[...(was.tried||[]),was.picture]:null;
  return {level:x.level,cls:q.cls,intro:q.intro||null,recipe:q.plan.recipe,slot,picture:x.picture,title:p.title,N:p.N,band:old?old.band:x.band,pack:p.pack,hidden:p.hidden,P:p.P,...(pins.some(e=>e.id===x.picture)?{pin:true}:{}),...((r=>r.length?{relaxed:r}:{})((x.relaxed||old?.relaxed||[]).filter(k=>k==='band'||k==='pack'))),...(tried&&tried.length?{tried}:{})};});
 // «по цветам» (17 §5): "most work now" loses about one in three regular Normals on L4–24 and one in two from L25; the
 // regular positions still to build look for a level it loses (--want-loss) as many as the wave's placed regular Normals
 // leave short of that, spread evenly over them
 const regs=Object.values(placed).filter(L=>L.level>=a&&L.level<=b&&regularAt(L.level,placed)).map(L=>({level:L.level,lost:(s=>!s['most-now|left']&&!s['most-now|right'])(L.strategies||{})}));
 for(const part of [[4,24,1/3],[25,1e9,1/2]]){const open=assign.filter(x=>x.slot==='regular'&&x.level>=part[0]&&x.level<=part[1]),placedP=regs.filter(x=>x.level>=part[0]&&x.level<=part[1]);if(!open.length)continue;
  const short=Math.max(0,Math.round(part[2]*(placedP.length+open.length))-placedP.filter(x=>x.lost).length),k=Math.min(open.length,short);
  open.forEach((x,i)=>{if(Math.floor((i+1)*k/open.length)>Math.floor(i*k/open.length))x.wantLoss=true;});}
 // the earlier plan's entries of the positions placed since stay in plan.json (the whole wave's assignment is kept: a
 // rebuild that takes a level back finds its picture there), marked placed
 const carried=((prev&&prev.assign)||[]).filter(x=>!assign.some(y=>y.level===x.level)&&placed[x.level]&&placed[x.level].picture===x.picture).map(({relaxed,...x})=>{const r=(relaxed||[]).filter(k=>k==='band'||k==='pack');return {...x,...(r.length?{relaxed:r}:{}),placed:true};});
 return {assign:[...assign,...carried].sort((u,v)=>u.level-v.level),missing:r.missing,quota:r.quota,hand};}
function pfMd(pf){const cnt={};for(const x of pf.assign)cnt[x.band]=(cnt[x.band]||0)+1;const SL={regular:'обычный',breather:'передышка','after-hard':'после Hard',intro:'знакомство',Hard:'Hard','Super Hard':'Super Hard'};
 return ['','## Картинки волны (сначала картинка)','',`Принцип владельца 17 (01–02.11.2026): каждая позиция без моста получает одну картинку свободного пула заранее; \`rounds.mjs run\` строит ровно эту картинку × класс × рецепт (\`levels.mjs build\` с настройкой очереди, tune.mjs), при неудаче берёт замену той же размерной полосы. Размеры (17 §2, на ${pf.assign.length+pf.missing.length} позиций): ${SIZE_MIX.map(b=>`${b.band} — ${cnt[b.band]||0} из ${pf.quota[b.band]}`).join('; ')}. Вручную (мосты): ${pf.hand.length?pf.hand.map(n=>'L'+n).join(', '):'—'}.`,'',
  '| Ур. | Класс | Позиция | Рецепт | Картинка | Клеток | Полоса | Пак | Скрыто цветов | Подов | Замечание |','|---|---|---|---|---|---|---|---|---|---|---|',
  ...pf.assign.map(x=>`| ${x.level} | ${x.cls} | ${SL[x.slot]||x.slot}${x.wantLoss?', «по цветам» проигрывает':''} | ${x.recipe} | ${x.picture} «${x.title||''}»${x.pin?' (закреплена)':''} | ${x.N} | ${x.band} | ${x.pack} | ${x.hidden} | ${x.P} | ${[x.relaxed?`ослаблено: ${x.relaxed.join(', ')}`:'',x.tried?`замена после ${x.tried.join(', ')}`:''].filter(Boolean).join('; ')} |`),
  ...(pf.missing.length?['',`Без картинки: ${pf.missing.map(x=>`L${x.level} (${x.band}: ${x.reason})`).join('; ')}.`]:[]),''];}
const weakOf=f=>f.hiddenColours<=1||f.visible>=6;
// (a breather, 17 §5 «Передышка», is no regular Normal)
const regularAt=(n,placed)=>classOf(n)==='Normal'&&n>3&&!slotOf(n).intro&&classOf(n-1)==='Normal'&&!(placed&&placed[n]&&placed[n].breather);
// each child process plays its games on worker threads (perf/README.md); with --jobs N children the cores are shared out
const childThreads=()=>process.env.ROUNDS_THREADS??String(Math.max(1,Math.floor((os.cpus().length-1)/Math.max(1,+arg('--jobs',1)||1))));
const run1=(args,env={})=>new Promise(res=>{const p=spawn(NODE,args,{cwd:root,env:{...process.env,ROUNDS_THREADS:childThreads(),...env},stdio:['ignore','pipe','pipe']});let out='';p.stdout.on('data',d=>out+=d);p.stderr.on('data',d=>out+=d);p.on('close',code=>res({code,out}));});

// ---- plan ----
// the mechanics breakdown (17 §13 step 1, amendment of 2026-10-28): the owner-approved content/rounds/runs/<wave>/mechanics.json
const MRU={none:'без допов',links:'связки',hidden:'скрытые',bridge:'мост',cells:'скрытые клетки',latch:'засов и ключ',canal:'канал',shutters:'створки'},SIDE_RU={up:'вверх',left:'влево',right:'вправо',two:'две стороны'},STRIP_RU={new:'новая картинка с полосой',overlay:'стена поверх утверждённой',gap:'разделённая картинка (полоса в цвет фона)'};
const pr=x=>+(x*100).toFixed(1),pctR=([a,b])=>a===b?`${pr(a)} %`:`${pr(a)}–${pr(b)} %`;
// hidden cells (17 §8): shares of N, the cells still hidden after the start reveal (counted at N = 700)
function doseText(recipe,cls){if(recipe==='none')return 'без допов';const d=LV.doses(recipe,cls,24,false,700),n=k=>d[k][0]===d[k][1]?d[k][0]:d[k].join('–');
 return LV.doseShares(recipe,cls).map(x=>x.choice?'мост: 1 выбор из 2 мест':x.m==='canal'?'канал: 1 канал, 2 выхода':x.m==='shutters'?`створки${x.role==='lead'?'':' доп.'} ${x.count[0]===x.count[1]?x.count[0]:x.count.join('–')} шт., счётчик ${pctR(x.role==='lead'?LV.SHUTTER.count[['Normal','Hard','Super Hard'].indexOf(cls)]:[LV.SHUTTER.count[['Normal','Hard','Super Hard'].indexOf(cls)][0],LV.SHUTTER.count[['Normal','Hard','Super Hard'].indexOf(cls)][0]])} работы материала`:x.count?`${MRU[x.m]}${x.role==='lead'?'':' доп.'} ${x.count[0]===x.count[1]?x.count[0]:x.count.join('–')} ${x.count[1]===1?'пара':'пары'}`:x.m==='cells'?`${MRU[x.m]} ${x.role==='lead'?'':'доп. '}${pctR(x.range)} N (${n(x.m)} при N = 700)`:`${MRU[x.m]} ${x.role==='lead'?'':'доп. '}${pctR(x.range)} P (${n(x.m)} при P = 24)`).join('; ');}
const stageKey=s=>s.fixed?'фикс.':`${s.cells[0]}–${s.cells[1]} клеток, ${s.colours[0]}–${s.colours[1]} цветов${s.compact?' (или компактная 200–399 / 3–6)':''}`;
// the breakdown of the wave's planned levels: per level lead, additional, doses, bridge side and strip; shares per mechanic
function breakdown(levels){if(!levels.length)return null;const n=levels.length,has=m=>levels.filter(l=>l.recipe.split('+').includes(m)),cnt={};
 const normal=levels.filter(l=>l.cls==='Normal'),types=k=>levels.filter(l=>(l.recipe==='none'?0:l.recipe.split('+').length)===k).length;
 for(const l of levels)if(l.bridgeSide)cnt[l.bridgeSide]=(cnt[l.bridgeSide]||0)+1;
 return {levels:levels.map(l=>({level:l.level,cls:l.cls,lead:l.lead||null,add:l.add||[],recipe:l.recipe,doses:doseText(l.recipe,l.cls),...(l.bridgeSide?{bridgeSide:l.bridgeSide,strip:l.strip||null}:{})})),
  shares:Object.fromEntries(['bridge','links','hidden','cells','latch','canal','shutters'].map(m=>[m,{levels:has(m).length,share:Math.round(has(m).length/n*100)/100,lead:levels.filter(l=>l.lead===m).length}]).concat([['none',{levels:levels.filter(l=>l.recipe==='none').length,share:Math.round(levels.filter(l=>l.recipe==='none').length/n*100)/100}]])),
  normalWithMechanic:[normal.filter(l=>l.recipe!=='none').length,normal.length],types:{one:types(1),two:types(2),three:types(3)},sides:cnt};}
function plan(a,b){const placed=placedLevels(),positions=[],blockers=[],plans=mechanicsPlans();
 for(let n=a;n<=b;n++){const s=slotOf(n),st=stageOf(n),pl=planOf(n,plans);positions.push({level:n,cls:s.cls,intro:s.intro,allowed:s.allowed.filter(m=>SUPPORTED.has(m)),stage:st,placed:!!placed[n],picture:placed[n]?.picture||null,...(pl?{plan:pl}:{})});
  if(s.intro&&!SUPPORTED.has(s.intro))blockers.push(`L${n}: знакомство «${MECH_RU[s.intro]}» — движок и пилот этой механики ещё не готовы (§14)`);
  // hidden pods and hidden cells never on one level (17 §8, the owner, 2026-11-01)
  if(pl&&!recipeAllowed(pl.recipe))blockers.push(`L${n}: рецепт плана ${pl.recipe} — скрытые поды и скрытые клетки не бывают на одном уровне (§8)`);}
 const mech=breakdown(positions.filter(p=>p.plan).map(p=>p.plan));
 const open=positions.filter(p=>!p.placed),pics=freePictures().map(p=>{const m=measure(p),f=fitness(p),board=boardOf(p);return {id:p.id,title:p.title,pic:p,N:m.N,colours:m.colours,bin:sizeBin(m.N),strong:f.hiddenColours>=3&&!weakOf(f),strong3:f.hiddenColours>=3,weak:weakOf(f),stream:!!board.hero,side:bridgeSide(board),family:p.family||null,motifs:motifsOf(p),fits:open.filter(q=>fitsStage(m,q.level)).map(q=>q.level)};});
 // under a mechanics plan: a bridge position of strip "new" needs a picture with its own strip crossing to the planned
 // side; an overlay position takes a free pool picture with the generator's wall; the rest take free pool pictures
 // a gap position (17 §8 «Одна цельная картинка», 2026-10-29) a split of one whole free pool picture
 const isNew=q=>q.plan&&q.plan.bridgeSide&&q.plan.strip!=='overlay'&&q.plan.strip!=='gap',isOverlay=q=>q.plan&&q.plan.bridgeSide&&q.plan.strip==='overlay',isGap=q=>q.plan&&q.plan.bridgeSide&&q.plan.strip==='gap';
 const poolOpen=open.filter(q=>!isNew(q)&&!isGap(q)),poolPics=pics.filter(p=>!p.stream&&p.fits.length);
 const fitPics=pics.filter(p=>p.fits.length);
 // overlay supply per side: free pool pictures where the wall lies and the board with it fits the position's stage
 // (Hard and Super Hard: 3+ colours hidden at the start on that board)
 const ovSupply={};for(const q of open.filter(isOverlay)){const side=q.plan.bridgeSide,k=`L${q.level} ${SIDE_RU[side]}`;ovSupply[k]=0;
  for(const p of pics.filter(p=>!p.stream)){const ov=overlayOf(p.pic,side);if(!ov)continue;const op=overlaid(p.pic,ov),m=measure(op);if(!fitsStage(m,q.level))continue;if(q.cls!=='Normal'&&fitness(op).hiddenColours<3)continue;ovSupply[k]++;}}
 // the bank: candidates whose picture is still free, by measured class and introduction
 const free=new Set(fitPics.map(p=>p.id)),bank={};for(const f of fs.existsSync(BANK)?fs.readdirSync(BANK):[]){const bk=readJSON(path.join(BANK,f));if(!free.has(bk.picture))continue;for(const c of bk.candidates){const k=c.intro?`знакомство: ${c.recipe}`:c.cls;bank[k]=(bank[k]||0)+1;}}
 const need={Normal:poolOpen.filter(p=>p.cls==='Normal'&&!p.intro).length,Hard:poolOpen.filter(p=>p.cls==='Hard').length,'Super Hard':poolOpen.filter(p=>p.cls==='Super Hard').length,intro:open.filter(p=>p.intro).length,bridge:open.filter(p=>p.intro==='bridge').length};
 // the order (17 §13): 1.5–2 pictures a position; strong ones (3+ colours hidden at the start) twice the Hard and Super
 // Hard positions; three stream pictures for each bridge introduction; the size mix of §2 per 30 (6 / 15 / 9)
 const want=Math.ceil(1.75*poolOpen.length),wantStrong=2*(need.Hard+need['Super Hard']),wantStream=3*need.bridge;
 const have=poolPics.length,haveStrong=poolPics.filter(p=>p.strong).length,haveStream=fitPics.filter(p=>p.stream).length;
 const total=Math.max(0,want-have),strong=Math.max(0,wantStrong-haveStrong),streams=Math.max(0,wantStream-haveStream);
 // new strip pictures by crossing side and size stage, 1.75 a position, less the free ones with their own strip that fit
 const strips=[];for(const q of open.filter(isNew)){const side=q.plan.bridgeSide,sk=stageKey(q.stage);let e=strips.find(x=>x.side===side&&x.stage===sk);if(!e)strips.push(e={side,stage:sk,levels:[],strongLevels:[]});e.levels.push(q.level);if(q.cls!=='Normal')e.strongLevels.push(q.level);}
 for(const e of strips){e.have=fitPics.filter(p=>p.stream&&p.side===e.side&&e.levels.some(n=>p.fits.includes(n))).length;e.count=Math.max(0,Math.ceil(1.75*e.levels.length)-e.have);e.strong=Math.min(e.count,Math.max(0,2*e.strongLevels.length-fitPics.filter(p=>p.stream&&p.strong&&p.side===e.side).length));}
 const mix={'200–499':6/30,'500–999':15/30,'1000–1400':9/30},byStage={};
 for(const q of poolOpen){const k=q.stage.fixed?`L${q.level} (фиксированная)`:stageKey(q.stage);byStage[k]=(byStage[k]||0)+1;}
 // the order split by stage in proportion to the open positions, rounded so the parts add up to the total
 const parts=Object.entries(byStage).map(([k,v])=>[k,v/poolOpen.length*total]),orderByStage=Object.fromEntries(parts.map(([k,x])=>[k,Math.floor(x)]));
 let rest=total-Object.values(orderByStage).reduce((x,y)=>x+y,0);for(const [k] of parts.sort((x,y)=>(y[1]%1)-(x[1]%1))){if(rest<=0)break;orderByStage[k]++;rest--;}
 for(const k of Object.keys(orderByStage))if(!orderByStage[k])delete orderByStage[k];
 const all=manifest().pictures.filter(e=>e.status==='approved').map(e=>picture(e.id)),motifCount={};for(const p of all)for(const m of motifsOf(p))motifCount[m]=(motifCount[m]||0)+1;
 const avoid=Object.entries(motifCount).filter(([,n])=>n>=3).map(([m,n])=>`${m} (${n})`);
 const themes=readJSON(path.join(HERE,'themes.json'),{groups:[]}),famCount={};for(const p of all)if(p.family)famCount[p.family]=(famCount[p.family]||0)+1;
 const families=themes.groups.flatMap(g=>g.families.map(f=>f.name)).sort((x,y)=>(famCount[x]||0)-(famCount[y]||0)).slice(0,12).map(f=>`${f} (${famCount[f]||0})`);
 const stripTotal=strips.reduce((x,e)=>x+e.count,0);
 // bridge scenes (tools/rounds/bridge-scene.mjs, the main path since 2026-10-28): each new-strip bridge position takes a
 // scene composed from a smaller free pool picture (the far side behind a wall, crossed strips, near-bank beds) and searched
 // until its planned bridge kind holds; approved free scene pictures of that side and stage already count
 const sceneSources=pics.filter(p=>!p.stream&&p.N>=200&&p.N<=800).length;
 const scenes=open.filter(isNew).map(q=>{const side=q.plan.bridgeSide,kind=q.plan.bridgeKind||'single',have=fitPics.filter(p=>p.stream&&p.pic.scene&&p.pic.scene.mode!=='split'&&p.side===side&&p.fits.includes(q.level)).length;
  return {level:q.level,cls:q.cls,side,kind,recipe:q.plan.recipe,have,command:`node tools/rounds/bridge-scene.mjs make --side ${side} --kind ${kind} --cls "${q.cls}" --recipe ${q.plan.recipe} --level ${q.level} --free --out content/rounds/pictures/src/scenes-NNNN.mjs`};});
 const sceneTotal=scenes.filter(x=>!x.have).length;
 // split pictures (bridge-scene.mjs make --mode split, the owner, 2026-10-29): each gap position takes one whole free pool
 // picture of its stage, split by a neutral strip on the planned side and searched until its bridge kind holds. Suggested
 // sources: free pool pictures without a strip that fit the stage and split to that side (Hard and Super Hard: 3+ colours
 // hidden at the start on the split board), up to three, a family and motifs not yet first choice of another gap position
 // first (the owner, 2026-10-29: a family or motif once per wave);
 // approved free split pictures of that side and stage already count
 const firstFam=new Set(),firstMotif=new Set(),splitCache=new Map(),canSplit=(p,side)=>{const k=p.id+'|'+side;if(!splitCache.has(k))splitCache.set(k,BS.splittable(p.pic,side));return splitCache.get(k);};
 const splits=open.filter(isGap).map(q=>{const side=q.plan.bridgeSide,kind=q.plan.bridgeKind||'single',strongNeed=q.cls!=='Normal',have=fitPics.filter(p=>p.stream&&p.pic.scene&&p.pic.scene.mode==='split'&&p.side===side&&p.fits.includes(q.level)).length;
  const taken=p=>(firstFam.has(p.family)?1:0)+(p.motifs.some(m=>firstMotif.has(m))?1:0),fit=pics.filter(p=>!p.stream&&p.fits.includes(q.level)).sort((x,y)=>taken(x)-taken(y)||x.id.localeCompare(y.id)),sources=[];
  for(const p of fit){if(sources.length>=3)break;const sp=canSplit(p,side);if(!sp||(strongNeed&&sp.hiddenColours<3))continue;sources.push(p);}
  if(sources[0]){if(sources[0].family)firstFam.add(sources[0].family);for(const m of sources[0].motifs)firstMotif.add(m);}
  return {level:q.level,cls:q.cls,side,kind,recipe:q.plan.recipe,have,sources:sources.map(p=>p.id),command:`node tools/rounds/bridge-scene.mjs make --mode split --from ${sources.map(p=>p.id).join(',')||'P####'} --side ${side} --kind ${kind} --cls "${q.cls}" --recipe ${q.plan.recipe} --level ${q.level} --out content/rounds/pictures/src/split-${lid(q.level)}.mjs`};});
 const splitTotal=splits.filter(x=>!x.have).length;
 // latch positions (17 §8): the acorn key lies in an empty area closed in by work, so such a position needs a picture with
 // one (levels.mjs keyHoles; a Super Hard lead two); order the rest
 const latchOpen=open.filter(q=>q.plan&&q.plan.recipe.split('+').includes('latch')),latchPics=pics.filter(p=>!p.stream&&latchOpen.some(q=>fitsPosition(p,q)));
 const latchOrder=Math.max(0,Math.ceil(1.75*latchOpen.length)-latchPics.length);
 // packs (17 §11, 2026-11-01): the free pool by pack, so the wave can be spread over them (levels.mjs place)
 const packs={};for(const p of pics.filter(p=>!p.stream&&p.fits.length)){const k=packOf(p.pic)||'—';packs[k]=(packs[k]||0)+1;}
 // pinned pictures (mechanics.json `pin`): reserved positions, or why a pin cannot go into the wave
 const prevPlan=flag('--reassign')?null:readJSON(path.join(waveDir(a,b),'plan.json'),null),prevAt=new Map(((prevPlan&&prevPlan.assign)||[]).map(x=>[x.picture,x.level]));
 const wavePlan=plans.find(p=>p.wave[0]===a&&p.wave[1]===b)||plans.find(p=>a>=p.wave[0]&&a<=p.wave[1]),pins=wavePlan&&Array.isArray(wavePlan.pin)?reservePins(wavePlan.pin,open,pics,placed,a,b,prevAt):[];
 // picture-first (the owner's principle 17, 17 §2 «Размеры на волну 50», 2026-11-01/02): each open non-bridge position of a
 // planned wave gets ONE picture from the free pool by the size mix, spread evenly, packs interleaved, pins kept,
 // Hard / Super Hard the pictures with more colours hidden at the start, breathers the small ones (wave.mjs
 // assignPictures); an earlier plan's assignment is kept while its picture is still free (--reassign draws it anew).
 // Bridge and hand positions are skipped and reported.
 const pf=mech?pictureFirst(a,b,open,pics,pins,placed):null;
 const out={range:[a,b],created:new Date().toISOString(),positions:positions.map(({plan:pl,...p})=>p),open:open.map(p=>p.level),blockers,...(mech?{mechanics:mech}:{}),need,supply:{pictures:have,strong:haveStrong,stream:haveStream,bank,packs,...(latchOpen.length?{latch:{positions:latchOpen.map(q=>q.level),pictures:latchPics.length}}:{}),...(mech?{overlay:ovSupply}:{})},...(pins.length?{pins}:{}),...(pf?{assign:pf.assign,assignMissing:pf.missing,assignQuota:pf.quota,hand:pf.hand}:{}),
  order:{total,strong,streams,latch:latchOrder,byStage:orderByStage,sizeMix:Object.fromEntries(Object.entries(mix).map(([k,v])=>[k,Math.round(v*total)])),...(mech?{strips:strips.map(({levels,strongLevels,...e})=>({...e,levels})),scenes,sceneSources,splits}:{}),avoidMotifs:avoid,suggestFamilies:families}};
 const dir=waveDir(a,b);writeJSON(path.join(dir,'plan.json'),out);
 const mechMd=mech?['## Разбор допов','',`Утверждён владельцем (\`runs/${lid(a)}-${lid(b)}/mechanics.json\`, 17 §13 шаг 1). Расстановка берёт на позицию только кандидата с этим рецептом и, для моста, с этой стороной перехода. Дозы по §7: ведущая — свой диапазон (при трёх типах нижняя половина), дополнительная — минимум; место дозы по кругу §7.`,'',
  '| Ур. | Класс | Ведущая | Дополнительные | Дозы | Мост: сторона | Полоса |','|---|---|---|---|---|---|---|',
  ...mech.levels.map(l=>`| ${l.level} | ${l.cls} | ${l.lead?MRU[l.lead]:'—'} | ${l.add.map(m=>MRU[m]).join(', ')||'—'} | ${l.doses} | ${l.bridgeSide?SIDE_RU[l.bridgeSide]:''} | ${l.strip?STRIP_RU[l.strip]||l.strip:''} |`),'',
  `- Доли: ${['bridge','links','hidden','cells','latch','canal','shutters'].map(m=>`${MRU[m]} — ${mech.shares[m].levels} (${Math.round(mech.shares[m].share*100)} %, ведущая в ${mech.shares[m].lead})`).join('; ')}; без допов — ${mech.shares.none.levels} (${Math.round(mech.shares.none.share*100)} %).`,
  `- Normal с допом: ${mech.normalWithMechanic[0]} из ${mech.normalWithMechanic[1]}. Типов на уровне: один — ${mech.types.one}, два — ${mech.types.two}, три — ${mech.types.three}.`,
  `- Стороны мостов: ${Object.entries(mech.sides).map(([s,n])=>`${SIDE_RU[s]} — ${n}`).join('; ')||'—'}.`,'']:[];
 const md=[`# План волны L${a}–L${b}`,'',`Создан ${out.created.slice(0,16).replace('T',' ')}. Правила: 17 §13. Позиций ${positions.length}, открыто ${open.length}.`,'',
  blockers.length?`**Не собираются, пока движок не поддержит механику:** ${blockers.join('; ')}. Остальные позиции волны собираются.`:'Все механики открытых позиций движок поддерживает.','',
  ...mechMd,
  '## Позиции','','| Уровень | Класс | Знакомство | Разрешено | Этап | Занято |','|---|---|---|---|---|---|',
  ...positions.map(p=>`| ${p.level} | ${p.cls} | ${p.intro?MECH_RU[p.intro]:''} | ${p.allowed.map(m=>MECH_RU[m]).join(', ')||'без допов'} | ${p.stage.fixed?'фикс.':`${p.stage.cells.join('–')} / ${p.stage.colours.join('–')}`} | ${p.placed?p.picture:''} |`),
  '','## Что уже есть','',`- Свободных утверждённых картинок под этапы волны: ${fitPics.length} (без полосы ${have}, из них сильных ${haveStrong}; с полосой ${haveStream}).`,
  `- Кандидаты в банке на свободных картинках: ${Object.entries(bank).map(([k,v])=>`${k} — ${v}`).join('; ')||'нет'}.`,
  `- Открытые позиции на картинках пула: Normal ${need.Normal}, Hard ${need.Hard}, Super Hard ${need['Super Hard']}, знакомства ${need.intro} (мост ${need.bridge}).`,
  `- Паки свободного пула (17 §11: волна берёт картинки из разных паков, соседние уровни — из разных): ${Object.entries(packs).sort((x,y)=>y[1]-x[1]).map(([k,v])=>`${k} — ${v}`).join('; ')||'—'}.`,
  ...(latchOpen.length?[`- Позиции с засовом: ${latchOpen.length} (L${latchOpen.map(q=>q.level).join(', L')}); свободных картинок с местом для ключа (пустая область внутри картинки, закрытая работой), подходящих им: ${latchPics.length}.`]:[]),
  ...(pins.length?['','## Закреплённые картинки','','Владелец закрепил их за волной (`mechanics.json` `pin`): расстановка держит каждую для её позиции (\`levels.mjs place\`), сборка строит под неё кандидатов (\`rounds.mjs run\`).','','| Картинка | Пак | Подходит позициям | Резерв | Состояние |','|---|---|---|---|---|',
   ...pins.map(e=>`| ${e.id} «${e.title||''}» | ${e.pack||'—'} | ${e.positions.map(n=>'L'+n).join(', ')||'—'} | ${e.reserved?'L'+e.reserved:'—'} | ${{free:'свободна',placed:'поставлена',used:'уже в раунде',blocked:'не встаёт'}[e.status]}${e.reason?': '+e.reason:''} |`),'']:[]),
  ...(pf?pfMd(pf):[]),
  ...(mech&&Object.keys(ovSupply).length?[`- Стена поверх картинки пула (сколько свободных картинок её берут и подходят этапу; Hard/SH — сильные с ней): ${Object.entries(ovSupply).map(([k,v])=>`${k} — ${v}`).join('; ')}.`]:[]),
  '','## Заказ картинок','',total+strong+streams+stripTotal+splitTotal?(total?`- Картинки пула: **${total}** (ориентир 1,75 на позицию).`:'- Картинок пула хватает.'):'- Заказ не нужен: картинок хватает.',
  ...(total?[`- По этапам: ${Object.entries(orderByStage).map(([k,v])=>`${k} — ${v}`).join('; ')}.`,`- По размеру (§2, на 30: 6 / 15 / 9): ${Object.entries(out.order.sizeMix).map(([k,v])=>`${k} — ${v}`).join('; ')}.`]:[]),
  ...(strong?[`- Из них сильных (3+ скрытых цвета на старте, под Hard и Super Hard): **${strong}**.`]:[]),
  ...(streams?[`- С ручьём и двумя местами моста: **${streams}**. За каждым местом свой участок дальнего берега, чтобы со второго знакомства решал один мост (§8).`]:[]),
  ...(scenes.length?[`- **Сцены моста: нужно ${sceneTotal}** из ${scenes.length} позиций (\`bridge-scene.mjs make\`: картинка пула поменьше на дальнем берегу за стеной, две полосы за местами, грядки на ближнем берегу; поиск до вида моста по плану; черновик модуля → лист владельцу; источников в пуле — ${sceneSources}):`,
   ...scenes.map(x=>`  - L${x.level} ${x.cls}, ${x.recipe}, переход ${SIDE_RU[x.side]}, ${x.kind==='better'?'оба выигрывают, один лучше на 10+':x.kind==='any'?'ровно один или оба (один лучше на 10+)':'решает ровно один'}${x.have?` — уже есть ${x.have}`:`: \`${x.command}\``}`),
   '  - Вместо сцены можно нарисовать картинку с полосой вручную (ниже — сколько их понадобилось бы).']:[]),
  ...(splits.length?[`- **Контурные картинки с мостом: нужно ${splitTotal}** из ${splits.length} позиций (17 §8 «Только контур, без разреза», владелец 01.11.2026: одна осмысленная картинка, предмет обведён нейтральным контуром из её смысла, два места моста на контуре со своими подходами; рисуются скриптом по \`content/rounds/experiments/contour-bridges*/contour.py\`, очередь ищется вручную; черновик → лист владельцу; разрез \`--mode split\` больше не применяется):`,
   ...splits.map(x=>`  - L${x.level} ${x.cls}, ${x.recipe}, переход ${SIDE_RU[x.side]}, ${x.kind==='better'?'оба выигрывают, один лучше на 10+':x.kind==='any'?'ровно один или оба (один лучше на 10+)':'решает ровно один'}${x.have?` — уже есть ${x.have}`:' — нарисовать контурную картинку'}`)]:[]),
  ...(stripTotal?[`- **Новые картинки с полосой и двумя местами моста: ${stripTotal}** (полоса — ручей, забор, стена; опора каждого места на ближнем берегу — цветная клетка; подходы из разных материалов; §8):`,
   ...strips.filter(e=>e.count).map(e=>`  - переход ${SIDE_RU[e.side]}${e.side==='two'?' (два места на разных полосах или в разные стороны)':''}, ${e.stage}: **${e.count}** для L${e.levels.join(', L')}${e.strong?`, из них сильных ${e.strong}`:''}${e.have?` (уже есть ${e.have})`:''}`)]:[]),
  ...(latchOrder?[`- **Под засов: ${latchOrder}** картинок с пустой областью внутри (окно, кольцо, просвет между частями), закрытой цветной работой: там лежит жёлудь-ключ (17 §8).`]:[]),
  `- Не рисовать (мотив 3+ раз): ${avoid.join(', ')||'—'}.`,`- Реже всего в пуле семьи: ${families.join(', ')}.`,''].join('\n');
 fs.writeFileSync(path.join(dir,'plan.md'),md);console.log(md);return out;}
const ovCache=new Map();const overlayOf=(p,side)=>{const k=p.id+'|'+side;if(!ovCache.has(k))ovCache.set(k,wallOverlay(p,side,1));return ovCache.get(k);};

// ---- the rebuild of a wave already built (the owner's decisions of 2026-11-02) ----
// --rebuild auto: the levels wave.mjs rebuildSet sends back (picture repeats and the 10+ colour window on another picture,
// breathers out of 75–90 % or off their light recipe, regular Normals that keep a 20-level median out of 50–60 % or the
// «по цветам» quota short); --rebuild 59,62,…: those levels. Each is taken off (levels.mjs unplace: its candidate goes back
// to the bank as a fallback) and recorded in runs/<wave>/rebuild.json; the dry placement then leaves them open
// (ROUNDS_HOLD_OPEN), so they are built again by the generator of wave.mjs GEN (its combinations are new), and the
// placement chooses among old and new candidates. Resumable: a recorded level not yet placed stays held; one placed after
// its rebuild is marked done and left alone.
const lostOf=L=>{const s=L.strategies||{};return !s['most-now|left']&&!s['most-now|right'];};
function rebuildStep(a,b,step){const dir=waveDir(a,b),f=path.join(dir,'rebuild.json'),reb=readJSON(f,{tag:GEN,levels:[]}),placed=placedLevels(),wp=mechanicsPlans().find(p=>p.wave[0]===a&&p.wave[1]===b),v=String(arg('--rebuild'));
 const info=L=>{const p=picture(L.picture);return {family:(p&&p.family)||null,motifs:p?motifsOf(p):[],colours:p?measure(p).colours:0};};
 let S;if(v==='auto'){const lv=Object.values(placed).filter(L=>L.level>=a&&L.level<=b).map(L=>({level:L.level,cls:L.cls,intro:!!L.intro,breather:!!L.breather,zoneMiss:!!L.zoneMiss,regular:regularAt(L.level,placed),share:L.random.share,lost:lostOf(L),recipe:L.recipe||'none',gen:L.gen||null,hand:!!L.hand,pinned:!!(wp&&(wp.pin||[]).includes(L.picture)),...info(L)}));
  // the levels a rebuild already took back and not yet placed count as sent back: at the aim and losing «по цветам»
  for(const x of reb.levels)if(x.tag===GEN&&!x.done&&!placed[x.level]&&x.level>=a&&x.level<=b)lv.push({level:x.level,cls:classOf(x.level),intro:false,breather:false,regular:regularAt(x.level,placed)&&!(((wp&&wp.levels)||[]).find(l=>l.level===x.level)||{}).breather,share:MEDIAN.aim,lost:true,recipe:'none',gen:GEN,hand:false,pinned:false,family:null,motifs:[],colours:0});
  const context=Object.values(placed).filter(L=>L.level>=a-20&&L.level<a).map(L=>({level:L.level,...info(L)})),planMap=new Map(((wp&&wp.levels)||[]).map(l=>[l.level,{recipe:l.recipe,breather:!!l.breather}]));
  // (a level a rebuild placed is left alone, unless it was placed as a regular Normal outside its tuning zone: a fallback)
  S=rebuildSet(lv,{from:a,to:b,context,plan:planMap,done:new Set(reb.levels.filter(x=>x.done&&x.tag===GEN&&!(x.to&&regularAt(x.level,placed)&&!(x.to.share>=REGULAR_ZONE[0]&&x.to.share<=REGULAR_ZONE[1]))).map(x=>x.level))});}
 else S=v.split(',').map(Number).filter(n=>n>=a&&n<=b).map(n=>({level:n,why:['по списку'],reassign:false}));
 const take=[];for(const x of S){const L=placed[x.level],old=reb.levels.find(y=>y.level===x.level&&y.tag===GEN);if(old&&old.done&&v!=='auto')continue;if(old)old.done=false;
  const e={level:x.level,tag:GEN,why:x.why,reassign:!!x.reassign,at:new Date().toISOString().slice(0,10),...(L?{from:{picture:L.picture,candidate:L.candidate,share:L.random.share,recipe:L.recipe}}:{})};
  if(old)Object.assign(old,e,{from:old.from||e.from});else reb.levels.push(e);if(L)take.push(x.level);}
 reb.tag=GEN;reb.levels.sort((u,v)=>u.level-v.level);writeJSON(f,reb);
 for(const n of take)execFileSync(NODE,[path.join(HERE,'levels.mjs'),'unplace',String(n),String(n)],{cwd:root,env:process.env,stdio:'ignore'});
 step('пересборка по решениям 02.11.2026',{levels:S.map(x=>`L${x.level}${x.reassign?' (другая картинка)':''}: ${x.why.join('; ')}`),taken:take});}
// after the placement: a recorded level placed again is done (its new candidate, or the old one when nothing new fits)
function rebuildDone(a,b){const f=path.join(waveDir(a,b),'rebuild.json'),reb=readJSON(f,null);if(!reb)return;const placed=placedLevels();
 for(const x of reb.levels){const L=placed[x.level];if(L&&!x.done){x.done=true;x.to={picture:L.picture,candidate:L.candidate,share:L.random.share,recipe:L.recipe,lost:lostOf(L),gen:L.gen||null};}}writeJSON(f,reb);}

// ---- run ----
async function runWave(a,b){const dir=waveDir(a,b),jobs=+arg('--jobs',Math.max(1,Math.min(4,os.cpus().length))),t0=Date.now();fs.mkdirSync(path.join(dir,'build'),{recursive:true});
 const state=readJSON(path.join(dir,'state.json'),{steps:[]}),step=(name,extra={})=>{state.steps.push({name,at:new Date().toISOString(),...extra});writeJSON(path.join(dir,'state.json'),state);console.log(`[${((Date.now()-t0)/60000).toFixed(1)} мин] ${name}`,Object.keys(extra).length?JSON.stringify(extra):'');};
 if(arg('--rebuild'))rebuildStep(a,b,step);
 const held=(readJSON(path.join(dir,'rebuild.json'),{levels:[]}).levels||[]).filter(x=>!x.done&&x.tag===GEN).map(x=>x.level);
 // positions whose introduction the engine does not play stay open and are reported (17 §14); the rest of the wave runs
 const p=plan(a,b);if(p.blockers.length)step('позиции без поддержки движка остаются открытыми',{blockers:p.blockers});
 // bridge positions of strip "new" take approved scene pictures (bridge-scene.mjs); those without one stay open until made
 const noScene=(p.order.scenes||[]).filter(x=>!x.have);if(noScene.length)step('сцены моста ещё не сделаны и не утверждены (bridge-scene.mjs make)',{levels:noScene.map(x=>x.level)});
 // gap positions take approved split pictures (bridge-scene.mjs make --mode split); those without one stay open until made
 const noSplit=(p.order.splits||[]).filter(x=>!x.have);if(noSplit.length)step('контурные картинки с мостом ещё не сделаны и не утверждены (17 §8)',{levels:noSplit.map(x=>x.level)});
 // 1. what the bank fills already: a placement on a copy of content/rounds (with the waves' mechanics plans)
 // (a light copy: no approved/ sheet, no Daily pictures, of runs/ only the plans; the placement skips the publish of
 // approved/ there, ROUNDS_NO_PUBLISH; the copy is deleted whatever happens)
 const dry=async()=>{const tmp=fs.mkdtempSync(path.join(os.tmpdir(),'rounds-dry-')),skip=['approved','daily-2027'].map(d=>path.join(ROUNDS,d));
  try{fs.cpSync(ROUNDS,tmp,{recursive:true,filter:s=>!skip.some(d=>s===d||s.startsWith(d+path.sep))&&(!s.includes(`${path.sep}runs${path.sep}`)||s.endsWith(`${path.sep}mechanics.json`)||s.endsWith(`${path.sep}plan.json`)||s.endsWith(`${path.sep}rebuild.json`)||fs.statSync(s).isDirectory())});
   await run1([path.join(HERE,'levels.mjs'),'place',String(a),String(b)],{ROUNDS_DIR:tmp,ROUNDS_NO_PUBLISH:'1',ROUNDS_DRY:'1',...(held.length?{ROUNDS_HOLD_OPEN:held.join(',')}:{})});return (readJSON(path.join(tmp,'needs.json'),{needs:[]}).needs||[]).filter(x=>x.level>=a&&x.level<=b);}
  finally{fs.rmSync(tmp,{recursive:true,force:true});}};
 let needs=await dry();step('пробная расстановка',{open:needs.map(x=>x.level)});
 // picture-first (plan.json assign; --sweep: the old path below, every free picture for every open position)
 const pfMode=!!(p.assign&&p.assign.length)&&!flag('--sweep');let timing=null;
 if(pfMode&&!flag('--no-build'))timing=await buildPictureFirst(a,b,p,dir,jobs,step,needs);
 // --all: candidates for every planned position, not only the open ones (when the wave's sequence checks fail on what
 // the bank already holds, e.g. the Normal median or the greedy quota, fresh candidates give the placement a choice)
 if(flag('--all')){const plan=readJSON(path.join(dir,'mechanics.json'),null);if(plan)for(const e of plan.levels){if(e.level<a||e.level>b||needs.some(x=>x.level===e.level))continue;
  needs.push({level:e.level,cls:e.cls,recipe:e.recipe,...(e.bridgeSide?{bridgeSide:e.bridgeSide,strip:e.strip}:{}),stage:null});}}
 // 2. candidates only for what stays open: free pictures of those stages, the classes and recipes those positions need;
 // a position of the mechanics plan only its planned recipe: a bridge on a wall of the planned side over a free pool
 // picture (strip "overlay") or on a picture with its own strip crossing to that side (strip "new"). One build process
 // per picture (its bank file), with the exact class:recipe(@side) pairs
 // pinned pictures still free (plan.json pins, reservePins): candidates for their reserved position (or every position they fit)
 const pinBuild=(p.pins||[]).filter(e=>e.status==='free');if(pinBuild.length)step('закреплённые картинки',{pins:pinBuild.map(e=>`${e.id}→${e.reserved?'L'+e.reserved:'—'}`)});
 if(!pfMode&&(needs.length||pinBuild.length)&&!flag('--no-build')){const pics=freePictures().map(pc=>{const board=boardOf(pc);return {pc,m:measure(pc),f:fitness(pc),stream:!!board.hero,side:bridgeSide(board)};}),byPic=new Map();
  const add=(id,variant,cls,recipe)=>{let t=byPic.get(id);if(!t)byPic.set(id,t={id,variants:new Set(),pairs:new Set(),classes:new Set(),recipes:new Set()});t.variants.add(variant);t.pairs.add(`${cls}:${recipe}${variant==='none'?'':'@'+variant}`);t.classes.add(cls);t.recipes.add(recipe);};
  const strong=(x,f)=>x.cls==='Normal'||f.hiddenColours>=3;
  // --no-bridge leaves the planned bridge positions for later (they need scene pictures); --per-position N tries at most
  // N pictures per planned position (the strongest first for Hard and Super Hard, else spread by the level number)
  const perPos=+arg('--per-position','0'),noBridge=flag('--no-bridge');
  for(const x of needs.filter(x=>x.recipe)){const bridge=x.recipe.split('+').includes('bridge');if(bridge&&noBridge)continue;
   // a latch recipe (17 §8) only on pictures with holes for its keys
   const keys=latchNeed(x.recipe,x.cls,x.intro);
   let pool=pics.filter(q=>holesOf(q.pc)>=keys);if(perPos>0){pool=pool.filter(q=>bridge?true:!q.stream&&fitsStage(q.m,x.level)&&strong(x,q.f));
    pool=x.cls==='Normal'?pool.map((q,k)=>({q,k:(k*7919+x.level*104729)%9973})).sort((u,v)=>u.k-v.k).map(u=>u.q):pool.slice().sort((u,v)=>v.f.hiddenColours-u.f.hiddenColours);
    // spread over the packs (17 §11, 2026-11-01): consecutive pictures of the pool from different packs
    pool=interleaveByPack(pool,q=>packOf(q.pc));
    if(!bridge)pool=pool.slice(0,perPos);}
   for(const {pc,m,f,stream,side} of pool){
    if(bridge&&x.strip==='overlay'){if(stream)continue;const ov=overlayOf(pc,x.bridgeSide);if(!ov)continue;const op=overlaid(pc,ov);if(!fitsStage(measure(op),x.level)||!strong(x,fitness(op)))continue;add(pc.id,x.bridgeSide,x.cls,x.recipe);}
    else{if(bridge?!(stream&&side===x.bridgeSide):stream)continue;if(bridge&&x.strip==='gap'&&!(pc.scene&&pc.scene.mode==='split'))continue;if(!fitsStage(m,x.level)||!strong(x,f))continue;add(pc.id,'none',x.cls,x.recipe);}}}
  for(const e of pinBuild){const pc=pics.find(q=>q.pc.id===e.id);if(!pc)continue;for(const n of e.reserved?[e.reserved]:e.positions){const pl=planOf(n);if(pl)add(e.id,'none',pl.cls,pl.recipe);}}
  // positions without a plan: as before, every allowed recipe of the classes those positions need
  for(const {pc,m,f,stream} of pics){const mine=needs.filter(x=>!x.recipe&&fitsStage(m,x.level)&&strong(x,f)&&(x.intro!=='bridge'||stream));if(!mine.length)continue;
   const classes=[...new Set(mine.map(x=>x.cls))],allowed=new Set(mine.flatMap(x=>x.intro?[x.intro]:x.allowed));
   const rec=[];for(const r of ['none','hidden','links','links+hidden','cells','links+cells',...(holesOf(pc)?['latch','links+latch']:[]),...(stream?['bridge','bridge+links']:[])])if(r.split('+').every(t=>t==='none'||allowed.has(t)))rec.push(r);
   for(const r of rec.filter(r=>classes.includes('Normal')||(r!=='none')))for(const c of classes)add(pc.id,'none',c,r);}
  const tasks=[...byPic.values()],noIntro=!needs.some(x=>x.intro)&&!pinBuild.some(e=>(e.reserved?[e.reserved]:e.positions).some(n=>slotOf(n).intro));
  step('сборка кандидатов',{pictures:tasks.length,jobs});let i=0,done=0;
  await Promise.all(Array.from({length:jobs},async()=>{while(i<tasks.length){const t=tasks[i++],ov=[...t.variants];
   const r=await run1([path.join(HERE,'levels.mjs'),'build',t.id,'--classes',[...t.classes].join(','),'--recipes',[...t.recipes].join(','),'--pairs',[...t.pairs].join(','),...(ov.some(v=>v!=='none')?['--overlay',ov.join(',')]:[]),...(noIntro?['--no-intro']:[]),'--candidates',arg('--candidates','64')]);
   fs.writeFileSync(path.join(dir,'build',t.id+'.log'),r.out);done++;if(done%5===0||done===tasks.length)console.log(`  собрано ${done}/${tasks.length}`);}}));
  step('сборка готова');}
 // 3. checks missing on older candidates, then 4. the placement
 if(!pfMode){await Promise.all(Array.from({length:jobs},(_,k)=>run1([path.join(HERE,'levels.mjs'),'checks','--shard',`${k}/${jobs}`])));step('проверки кандидатов');}
 const pl=await run1([path.join(HERE,'levels.mjs'),'place',String(a),String(b)]);fs.writeFileSync(path.join(dir,'place.log'),pl.out);
 needs=(readJSON(path.join(ROUNDS,'needs.json'),{needs:[]}).needs||[]).filter(x=>x.level>=a&&x.level<=b);step('расстановка',{open:needs.map(x=>x.level)});rebuildDone(a,b);
 // which bridge sites each bridge level shows from its start (17 §8 as amended on 2026-10-31)
 bridges(a,b);step('видимость мостов');
 // 5. the audit of everything placed in the wave (17 §14)
 const au=await run1([path.join(HERE,'audit.mjs'),String(a),String(b),'--out',path.join(dir,'audit.md')]);fs.copyFileSync(path.join(dir,'audit.md'),path.join(ROUNDS,`audit-${lid(a)}-${lid(b)}.md`));
 const failed=(au.out.match(/\| НЕТ: /g)||[]).length+(/Последовательность[^\n]*: OK/.test(au.out)?0:1);step('проверка правил',{failed});
 // 6. the summary and 7. the owner's sample
 const placed=placedLevels(),rows=[];for(let n=a;n<=b;n++)if(placed[n])rows.push(placed[n]);
 const sample=flag('--no-sample')?[]:pickSample(rows,placed);
 if(sample.length){const r=await run1([PILOT,'--levels',sample.join(','),'--out',path.join(dir,'sample'),'--name',`wave-${a}-${b}`]);fs.writeFileSync(path.join(dir,'sample.log'),r.out);step('выборка',{levels:sample});}
 fs.writeFileSync(path.join(dir,'summary.md'),summary(a,b,rows,needs,au.out,sample,placed,(Date.now()-t0)/60000,timing,p.hand||[]));
 step('готово',{minutes:Math.round((Date.now()-t0)/60000)});console.log(fs.readFileSync(path.join(dir,'summary.md'),'utf8'));
 return needs.length||failed?1:0;}

// ---- picture-first build (the owner's principle 17): each assigned position builds exactly its picture × class × recipe
// (levels.mjs build with the generator's tuning, one process per position, --jobs in parallel); a picture that brings no
// level in the zone within its budget is swapped for a free picture of the same size band (wave.mjs replacement, at
// most --swaps 2 times; never a sweep of the pool) and that one is built. --limit N builds only the first N open
// positions; --minutes M the budget per position (default by slot). Returns the timing per position (timing.json).
const PF_MIN={regular:8,wantLoss:10,'after-hard':6,breather:4,intro:6,Hard:15,'Super Hard':15};
async function buildPictureFirst(a,b,p,dir,jobs,step,needs){const open=new Set(needs.map(x=>x.level)),limit=+arg('--limit',0),swaps=+arg('--swaps',2),placed=placedLevels();
 let todo=p.assign.filter(x=>open.has(x.level));if(limit)todo=todo.slice(0,limit);
 step('сначала картинка: сборка по позициям',{positions:todo.map(x=>`L${x.level} ${x.picture}`),hand:p.hand||[],jobs});
 const inBank=x=>{const bk=readJSON(path.join(BANK,x.picture+'.json'),{candidates:[]});return bk.candidates.filter(c=>c.cls===x.cls&&c.recipe===x.recipe&&!!c.intro===!!x.intro&&(c.inZone||c.inZone===undefined)).length;};
 // (an accent colour does not count in the §2 window of 10+ colour pictures, the owner, 2026-11-02: the accents keep the
 // stage's palette of 10 and the runner no longer lowers it)
 const one=async x=>{const t0=Date.now(),args=[path.join(HERE,'levels.mjs'),'build',x.picture,'--classes',x.cls,'--recipes',x.recipe,'--pairs',`${x.cls}:${x.recipe}`,'--slot',x.slot,'--want','1',...(x.wantLoss?['--want-loss']:[]),...(x.pinRetry?['--fresh','--seed','2']:[]),
   '--minutes',String(arg('--minutes')||(x.wantLoss&&x.slot==='regular'?PF_MIN.wantLoss:PF_MIN[x.slot])||8),x.intro?'--intro':'--no-intro','--candidates',arg('--candidates','64')];
  const r=await run1(args);fs.appendFileSync(path.join(dir,'build',x.picture+'.log'),r.out);const m=/@@tune (.*)/.exec(r.out),res=m?JSON.parse(m[1]):null;
  return {level:x.level,picture:x.picture,cls:x.cls,slot:x.slot,recipe:x.recipe,seconds:Math.round((Date.now()-t0)/1000),accepted:res?res.accepted:inBank(x),attempts:res?.attempts??null,quick:res?.quick??null,evaluated:res?.evaluated??null,first:res?.first??null,share:res?.share??null,skipped:!res,reasons:res?res.reasons:null};};
 const used=()=>new Set([...p.assign.map(x=>x.picture),...Object.values(placedLevels()).map(L=>L.picture)]),timing=[],queue=todo.slice();
 const before=Object.values(placed).filter(L=>L.level>=a-20&&L.level<=b).map(L=>({level:L.level,picture:L.picture})),picOf=id=>{const q=picture(id);return q?{id,family:q.family||null,motifs:motifsOf(q),pack:packOf(q),colours:measure(q).colours}:{id};};
 await Promise.all(Array.from({length:jobs},async()=>{while(queue.length){const x=queue.shift(),t=await one(x);timing.push({...t,seq:timing.length,...(x.swappedFrom?{swappedFrom:x.swappedFrom}:{}),...(x.pinRetry?{pinRetry:true}:{})});console.log(`  L${x.level} ${x.picture} ${x.cls}/${x.slot}: принято ${t.accepted} за ${t.seconds} с`);
  // (swaps counted in this run: a position the rebuild sent back gets its own two)
  if(t.accepted||(x.swaps||0)>=swaps)continue;
  // a pinned picture (the owner's) is not given up at once: once more, searched afresh (--fresh), before a swap
  if(x.pin&&!x.pinRetry){x.pinRetry=true;step(`L${x.level}: закреплённая ${x.picture} — ещё раз заново`);queue.push(x);continue;}
  // the fallback: a free picture of the same size band that fits the position (the plan's rules), then build it
  const q=p.positions.find(y=>y.level===x.level),pl=planOf(x.level),rp=PF_POOL&&replacement({level:x.level,cls:x.cls,intro:!!x.intro,recipe:x.recipe,breather:!!(pl&&pl.breather),accents:!!(pl&&pl.accents),latchNeed:latchNeed(x.recipe,x.cls,x.intro)},x.band,PF_POOL,{used:used(),tried:[...(x.tried||[]),x.picture],before:[...Object.values(placedLevels()).filter(L=>L.level>=a-20&&L.level<=b).map(L=>({level:L.level,picture:L.picture})),...p.assign.filter(y=>y.level!==x.level&&!placedLevels()[y.level]).map(y=>({level:y.level,picture:y.picture}))],picOf,waveStart:a,waveEnd:b});
  if(!rp){step(`L${x.level}: замены той же полосы ${x.band} нет`);continue;}
  const e=PF_POOL.find(z=>z.id===rp.picture);if(x.pin){delete x.pin;delete x.pinRetry;}x.swappedFrom=x.picture;x.swaps=(x.swaps||0)+1;x.tried=[...(x.tried||[]),x.picture];Object.assign(x,{picture:rp.picture,title:e.title,N:e.N,pack:e.pack,hidden:e.hidden,P:e.P,slot:slotOfPos({...q,plan:pl},e.N)});if(rp.relaxed)x.relaxed=rp.relaxed;else delete x.relaxed;
  writeJSON(path.join(dir,'plan.json'),p);step(`L${x.level}: ${x.swappedFrom} не дала уровня класса — замена ${x.picture} (${x.band})`);queue.push(x);}}));
 timing.sort((u,v)=>u.level-v.level||u.seq-v.seq);writeJSON(path.join(dir,'timing.json'),timing);writeJSON(path.join(dir,'plan.json'),p);
 step('сборка готова',{built:timing.length,accepted:timing.filter(t=>t.accepted).length,seconds:timing.reduce((s,t)=>s+t.seconds,0)});return timing;}

// the owner's sample (quality.json "sample"): every Super Hard, two Hard, every introduction, two random regular Normals
// and the two levels closest to a bound of the metrics (outliers); a fixed seed so a rerun picks the same
function pickSample(rows,placed){const S=QUALITY.sample||{},out=new Set(),rnd=(()=>{let s=rows.length*7919+(rows[0]?.level||1);return()=>{s^=s<<13;s^=s>>>17;s^=s<<5;return (s>>>0)/4294967296;};})();
 const take=(list,k)=>{const l=list.filter(L=>!out.has(L.level));for(let i=0;i<k&&l.length;i++)out.add(l.splice(Math.floor(rnd()*l.length),1)[0].level);};
 const margin=L=>{const pm=L.cls==='Normal'?QUALITY.pressure.min.regularNormal:QUALITY.pressure.min[L.cls],st=L.checks?.pressure?.stuck;return pm&&st!=null?(st-pm)/pm:9;};
 if(S.superHard==='all')rows.filter(L=>L.cls==='Super Hard').forEach(L=>out.add(L.level));take(rows.filter(L=>L.cls==='Hard'),S.hard??2);
 if(S.introductions==='all')rows.filter(L=>L.intro).forEach(L=>out.add(L.level));take(rows.filter(L=>regularAt(L.level,placed)),S.normal??2);
 rows.filter(L=>!L.intro&&!out.has(L.level)&&(L.cls!=='Normal'||regularAt(L.level,placed))).sort((x,y)=>margin(x)-margin(y)).slice(0,S.outliers??2).forEach(L=>out.add(L.level));
 return [...out].sort((x,y)=>x-y);}

function summary(a,b,rows,needs,audit,sample,placed,minutes,timing=null,hand=[]){const reg=rows.filter(L=>regularAt(L.level,placed)),med=median(reg.map(L=>L.random.share));
 // §5 the median over any 20 consecutive levels (wave.mjs medianWindows) and «по цветам» from L25 (greedyQuota)
 const ws=medianWindows(reg.map(L=>({level:L.level,share:L.random.share})),a,b),wOut=ws.filter(w=>w.out),wr=ws.length?[Math.min(...ws.map(w=>w.median)),Math.max(...ws.map(w=>w.median))]:null;
 const gq=greedyQuota(reg.map(L=>({level:L.level,lost:lostOf(L)})),a,b),br=rows.filter(L=>L.breather);
 const fam={};for(const L of rows){const f=picture(L.picture)?.family;if(f)(fam[f]=fam[f]||[]).push(L.level);}const twice=Object.entries(fam).filter(([,v])=>v.length>1);
 const cnt=f=>{const o={};for(const L of rows){const k=f(L);o[k]=(o[k]||0)+1;}return Object.entries(o).map(([k,n])=>`${k} — ${n}`).join('; ');};
 const lost=reg.filter(L=>{const s=L.strategies||{};return !s['most-now|left']&&!s['most-now|right'];}).length,itog=(audit.match(/Итог:[^\n]*/)||['Итог: —'])[0];
 return [`# Волна L${a}–L${b}`,'',`Прогон ${minutes.toFixed(0)} мин. ${itog}`,'',needs.length?`**Не заполнено ${needs.length}:** ${needs.map(x=>`L${x.level} ${x.cls}${x.intro?' ('+MECH_RU[x.intro]+')':''}`).join(', ')} → заказ картинок (\`rounds.mjs plan ${a} ${b}\`).`:'Все позиции заполнены.','',
  '| Ур. | Класс | Картинка | Механики | Бот | Стоят без работы | Вынужденных | Дебют + цвета | «По цветам» |','|---|---|---|---|---|---|---|---|---|',
  ...rows.map(L=>{const k=L.checks||{},s=L.strategies||{};return `| ${L.level} | ${L.cls}${L.intro?' · знакомство':''}${L.breather?' · передышка':''} | ${L.picture} «${picture(L.picture)?.title||''}» | ${L.recipe}${L.linked?` (связанных ${L.linked})`:''}${L.hidden?` (скрытых ${L.hidden})`:''}${L.cells?` (скрытых клеток ${L.cells})`:''}${L.latches?` (засовов ${L.latches})`:''}${L.definition?.hero?` (мост ${SIDE_RU[bridgeSide(L.definition)]||''}${L.definition.overlay?', стена':''})`:''} | ${pct(L.random.share)} | ${k.pressure?.stuck??'—'} | ${pct(k.forced)} | ${k.opening?`${k.opening.first.wins}/${k.opening.first.openings} · ${pct(k.opening.three.share)}`:'—'} | ${!s['most-now|left']&&!s['most-now|right']?'проигрывает':'выигрывает'} |`;}),
  '','## Распределения','',`- Классы: ${cnt(L=>L.cls)}.`,`- Рецепты: ${cnt(L=>L.recipe)}.`,`- Размеры: ${cnt(L=>sizeBin(L.N))}.`,`- Медиана бота обычных Normal: ${pct(med)} по волне; в окнах по 20 уровней ${wr?`${pct(wr[0])}–${pct(wr[1])}`:'—'}${wOut.length?`, вне 50–60 % — ${wOut.length} из ${ws.length} окон (L${wOut[0].from}–L${wOut[0].to} ${pct(wOut[0].median)}…)`:', все окна в 50–60 %'} (§5).`,
  `- «По цветам» проигрывает: ${gq.map(q=>`L${q.from}–L${Math.min(q.to,b)} — ${q.lost} из ${q.count} (нужно около ${q.target}, не меньше ${q.need})`).join('; ')||'—'}.`,
  `- Передышки (75–90 %): ${br.length?br.map(L=>`L${L.level} ${pct(L.random.share)} ${L.recipe}${L.zoneMiss||L.random.share<BREATHER_ZONE[0]||L.random.share>BREATHER_ZONE[1]?' (ближайшая раскладка вне зоны)':''}`).join(', '):'—'}.`,
  `- Семьи тем: ${Object.keys(fam).length} разных, дважды — ${twice.length?twice.map(([f,v])=>`«${f}» L${v.join(', L')}`).join('; '):'нет'} (не больше двух раз в волне и не ближе 10 уровней, §11).`,'',
  '## Выборка для владельца','',sample.length?`${sample.map(n=>'L'+n).join(', ')} — пилот \`content/rounds/runs/${lid(a)}-${lid(b)}/sample/index.html\`. После игры: \`node tools/rounds/rounds.mjs review <results.json>\`.`:'—','',
  ...(timing?['## Сборка по позициям (сначала картинка)','',`Вручную (мосты): ${hand.length?hand.map(n=>'L'+n).join(', '):'—'}.`,'','| Ур. | Класс · позиция | Рецепт | Картинка | Секунд | Попыток | Быстрых замеров | Полных | Принято | Первый, с | Доля |','|---|---|---|---|---|---|---|---|---|---|---|',
   ...timing.map(x=>`| ${x.level} | ${x.cls} · ${x.slot} | ${x.recipe} | ${x.picture}${x.swappedFrom?` (замена ${x.swappedFrom})`:''}${x.pinRetry?' (закреплена, повтор)':''} | ${x.seconds} | ${x.attempts??'—'} | ${x.quick??'—'} | ${x.evaluated??'—'} | ${x.accepted}${x.skipped?' (из банка)':''} | ${x.first??'—'} | ${pct(x.share)} |`),'']:[]),
  `Проверка правил: [audit.md](audit.md). Лог расстановки: place.log, сборки: build/.`,''].join('\n');}

// ---- review ----
function review(file){const res=readJSON(file),preview=arg('--preview'),date=(res.exportedAt||new Date().toISOString()).slice(0,10),pilot=(res.pilotVersion||'pilot').replace(/-rounds$/,'');
 const keep=['id','level','capacity','config','startedAt','commands','elapsedMs','outcome','logicalOutcome','timeBuckets','workActiveMs','rating','interest','art','planning','depthNeed','pace','waiting','uncertainty','note','endedAt'];
 const pt=path.join(root,'product/playtests',`${pilot}-owner-${date}`);fs.mkdirSync(pt,{recursive:true});
 fs.writeFileSync(path.join(pt,'results.json'),JSON.stringify({...res,note:'Compact copy (rounds.mjs review): telemetry, input events, work events, trip plans and intervals dropped.',attempts:res.attempts.map(x=>Object.fromEntries(keep.filter(k=>k in x).map(k=>[k,x[k]])))}));
 const RU={easy:'легко',normal:'нормально',hard:'трудно',okay:'нормально',fun:'интересно',boring:'скучно'},PLAN={colours:'по цветам','when-stuck':'когда застревал','start-then-colours':'начало, потом цвета','whole-game':'весь уровень'};
 const placed=placedLevels(),levelOf=n=>placed[n]||(preview&&readJSON(path.join(preview,lid(n)+'.json')));const M=QUALITY.mismatch||{},words=(M.noteWords||[]).map(w=>w.toLowerCase());
 const rows=[],mis=[],notes=[];
 for(const n of [...new Set(res.attempts.map(x=>x.level))].sort((x,y)=>x-y)){const A=res.attempts.filter(x=>x.level===n),w=A.filter(x=>x.outcome==='won').at(-1)||A.at(-1),L=levelOf(n);if(!L)continue;
  const k=L.checks||{},reg=regularAt(n,placed),blocked=w.timeBuckets&&w.elapsedMs?Math.round((w.timeBuckets.blockedWorking||0)/w.elapsedMs*100):null;
  const why=[];if(M.hardRatedEasy&&L.cls!=='Normal'&&w.rating==='easy')why.push(`${L.cls} оценён «легко»`);if(M.regularNormalRatedBoring&&reg&&w.interest==='boring')why.push('обычный Normal «скучно»');
  const hit=w.note&&words.filter(x=>w.note.toLowerCase().includes(x));if(hit&&hit.length)why.push(`в заметке: ${hit.join(', ')}`);
  if(why.length)mis.push({n,why,L,k});if(w.note)notes.push({n,note:w.note,rating:`${RU[w.rating]||'—'} · ${RU[w.interest]||'—'}`,L});
  rows.push(`| ${n} | ${L.cls}${L.intro?' · знакомство':''} | ${L.picture} «${picture(L.picture)?.title||''}» | ${A.map(x=>({won:'победа',jammed:'завал',abandoned:'брошена'}[x.outcome]||x.outcome)).join(', ')} | ${Math.round(w.elapsedMs/1000)} с | ${RU[w.rating]||'—'} · ${RU[w.interest]||'—'} | ${PLAN[w.planning]||w.planning||'—'} | ${pct(L.random.share)} | ${k.pressure?.stuck??'—'} | ${blocked??'—'} % | ${k.opening?`${k.opening.first.wins}/${k.opening.first.openings} · ${pct(k.opening.three.share)}`:'—'} | ${w.note||''} |`);}
 const out=path.join(ROUNDS,'reports',`${pilot}-review-${date}.md`);
 const md=[`# Разбор результатов ${pilot} (${date})`,'',`Скрипт: \`rounds.mjs review\`. Копия результатов: \`${path.relative(root,pt)}/results.json\`. Границы слоя Б: \`content/rounds/quality.json\`.`,'',
  '| Ур. | Класс | Картинка | Попытки | Время | Оценка | План | Бот | Стоят без работы | Места заняты (вы) | Дебют + цвета | Заметка |','|---|---|---|---|---|---|---|---|---|---|---|---|',...rows,'',
  '## Расхождения с метриками','',mis.length?mis.map(x=>`- **L${x.n}** (${x.L.cls}): ${x.why.join('; ')}. Метрики: бот ${pct(x.L.random.share)}, стоят без работы ${x.k.pressure?.stuck??'—'}${x.k.opening?`, дебют + цвета после трёх ${pct(x.k.opening.three.share)}`:''}. Что поправить: найти метрику, которая это не поймала, и сдвинуть её границу в quality.json; перепроверить волну.`).join('\n'):'Нет: оценки владельца согласуются с метриками.','',
  '## Черновик для журнала заметок (`content/rounds/owner-notes.md`)','',notes.length?['| Ур. | Оценка | Заметка | Что берём в генерацию |','|---|---|---|---|',...notes.map(x=>`| ${x.n} | ${x.rating} | «${x.note}» | (дописать) |`)].join('\n'):'Заметок нет.',''].join('\n');
 fs.mkdirSync(path.dirname(out),{recursive:true});fs.writeFileSync(out,md);console.log(md);console.log(`→ ${path.relative(root,out)}`);return mis.length?1:0;}

// ---- confirm ----
// the sample is every level of the wave the results file has attempts on; the results must be played on this engine
// 17 §8 as amended on 2026-10-31: which bridge sites each bridge level of the wave shows over the cubes from its start
// (lib.mjs bridgeShowing), written to the level as bridgeShown (outside its definition: the pilot's catalog stays the
// same); export.mjs carries it to round-level.v1 (a site not shown is "shown": false)
function bridges(a,b){if(!(Number.isInteger(a)&&Number.isInteger(b)&&a>=1&&b>=a)){console.error('usage: rounds.mjs bridges <from> <to>');return 1;}
 const placed=placedLevels(),rows=[];for(let n=a;n<=b;n++)if(placed[n])rows.push(placed[n]);
 const show=bridgeShowing(rows),word=v=>v.every(Boolean)?'оба видны':v.some(Boolean)?`виден ${v.map((x,i)=>x?i:null).filter(i=>i!==null).join(', ')} (приманка)`:'оба скрыты';
 for(const L of rows){if(!show[L.level])continue;const f=path.join(LEVELS,lid(L.level)+'.json');const cur=readJSON(f);
  if(JSON.stringify(cur.bridgeShown)!==JSON.stringify(show[L.level])){cur.bridgeShown=show[L.level];writeJSON(f,cur);}
  console.log(`${lid(L.level)} ${L.cls}: ${word(show[L.level])}`);}
 if(!Object.keys(show).length)console.log(`L${a}–L${b}: no bridge level`);return 0;}

function confirm(a,b,file,date=new Date().toISOString().slice(0,10),word=null){const fail=m=>{console.error(`confirm ${a}–${b} refused: ${m}`);return 1;};
 if(!(Number.isInteger(a)&&Number.isInteger(b)&&a>=1&&b>=a))return fail('usage: rounds.mjs confirm <from> <to> --sample <results.json> | --owner-word "<words>"');
 if(file&&word)return fail('give --sample or --owner-word, not both');
 if(word!==null){if(!word.trim())return fail('--owner-word needs the owner\'s words');
  const placed=placedLevels(),open=[];for(let n=a;n<=b;n++)if(!placed[n])open.push(n);if(open.length)return fail(`levels not placed: ${open.map(lid).join(', ')}`);
  const out={format:'round-confirmation.v1',wave:[a,b],sample:[],results:null,pilot:null,resultsExportedAt:null,attempts:0,won:[],engine:fingerprints(),confirmedBy:'owner',date,
   ownerWord:word.trim(),note:'the owner asked to move the wave into the game before playing its sample; the phone check after publishing is the first play'};
  const dest=path.join(waveDir(a,b),'confirmed.json');writeJSON(dest,out);
  console.log(`→ ${path.relative(root,dest)}: L${a}–L${b} on the owner's word «${out.ownerWord}» (no sample played)`);return 0;}
 if(!file)return fail('--sample <results.json> or --owner-word "<words>" is required');
 const f=path.resolve(root,file),res=readJSON(f);if(!res||!Array.isArray(res.attempts))return fail(`no pilot results in ${file}`);
 const sample=[...new Set(res.attempts.map(x=>x.level).filter(n=>n>=a&&n<=b))].sort((x,y)=>x-y);if(!sample.length)return fail(`${file} has no attempt on L${a}–L${b}`);
 const placed=placedLevels(),open=sample.filter(n=>!placed[n]);if(open.length)return fail(`sample levels not placed: ${open.map(lid).join(', ')}`);
 const engine=fingerprints(),played={rules:res.rulesHash,timeline:res.timelineHash,palette:res.paletteHash,clock:res.clockHash};
 const stale=sameEngine(played,engine,a,b);if(stale.length)return fail(`the sample was played on another engine (${stale.join(', ')} differ)`);
 const attempts=res.attempts.filter(x=>x.level>=a&&x.level<=b);
 const out={format:'round-confirmation.v1',wave:[a,b],sample,results:path.relative(root,f).split(path.sep).join('/'),pilot:res.pilotVersion||null,resultsExportedAt:res.exportedAt||null,
  attempts:attempts.length,won:[...new Set(attempts.filter(x=>x.outcome==='won').map(x=>x.level))].sort((x,y)=>x-y),engine,confirmedBy:'owner',date};
 const dest=path.join(waveDir(a,b),'confirmed.json');writeJSON(dest,out);
 console.log(`→ ${path.relative(root,dest)}: L${a}–L${b}, sample ${sample.map(n=>'L'+n).join(', ')} (${attempts.length} attempts, ${out.won.length} levels won)`);return 0;}

if(cmd==='plan'){plan(+process.argv[3],+process.argv[4]);}
else if(cmd==='confirm'){process.exitCode=confirm(+process.argv[3],+process.argv[4],arg('--sample'),arg('--date')||undefined,arg('--owner-word'));}
else if(cmd==='run'){process.exitCode=await runWave(+process.argv[3],+process.argv[4]);}
else if(cmd==='review'){process.exitCode=review(process.argv[3]);}
else if(cmd==='bridges'){process.exitCode=bridges(+process.argv[3],+process.argv[4]);}
else console.log(fs.readFileSync(new URL(import.meta.url),'utf8').split('\n').filter(l=>l.startsWith('//')).join('\n'));

// Picture-first wave (the owner's principle 17 of 2026-11-01/02, 17 §2 «Размеры на волну 50», §11 packs): `rounds.mjs plan`
// gives each non-bridge position of a wave ONE picture from the free approved pool up front, and `rounds.mjs run` builds
// exactly that picture × class × recipe. Pure functions here (no files), so the tests can check the rules.
//
// Size mix per 50 positions (the owner, 2026-11-01): 30 × 800–1200, 10 × 700–900, 5 × 400–700, 5 × 1200–1500 cells,
// scaled to the positions assigned (largest remainders). The mix is the wave's composition, not its order: the bands
// are spread evenly over the wave (the owner, 2026-11-02: «их поровну правильно раскидать надо»), no run of one band.
export const SIZE_MIX=[{band:'800–1200',lo:800,hi:1200,per50:30},{band:'700–900',lo:700,hi:899,per50:10},{band:'400–700',lo:0,hi:699,per50:5},{band:'1200–1500',lo:1201,hi:1500,per50:5}];
// a breather (17 §5 «Передышка»): a Normal on a picture up to ~550 cells or a position marked breather in the plan
export const BREATHER_N=550;
// the bands a picture of N cells belongs to (800–899 is in both 800–1200 and 700–900; the small band also takes the
// compact pictures under 400, 17 §2)
export const bandsOf=N=>SIZE_MIX.filter(b=>N>=b.lo&&N<=b.hi).map(b=>b.band);
// how many positions of n take each band: per50 × n / 50, rounded by largest remainders so they add up to n
export function bandCounts(n){const raw=SIZE_MIX.map(b=>({band:b.band,x:b.per50*n/50})),out=Object.fromEntries(raw.map(r=>[r.band,Math.floor(r.x)]));
 let rest=n-Object.values(out).reduce((a,b)=>a+b,0);for(const r of raw.slice().sort((a,b)=>(b.x%1)-(a.x%1)||SIZE_MIX.findIndex(m=>m.band===a.band)-SIZE_MIX.findIndex(m=>m.band===b.band))){if(rest<=0)break;out[r.band]++;rest--;}
 return out;}
// a position is a breather when its plan marks it (plan.breather) or, for a Normal that is no introduction, its picture
// has at most BREATHER_N cells
export const isBreather=(pos,N=null)=>pos.cls==='Normal'&&!pos.intro&&pos.level>3&&(!!pos.breather||(N!=null&&N<=BREATHER_N));
// the pods of a recipe (17 §7): a queue under 16 pods takes one mechanic type only
export const recipeFitsPods=(recipe,P)=>P>=16||(recipe||'none').split('+').filter(t=>t!=='none').length<=1;
// a latch (17 §8) stands at a depth of its class's range (Normal 3–6, Hard 2–6, Super Hard 2–8 pods before it) with at
// least 2 pods behind (levels.mjs pickLatches): its column needs that many pods, so the queue at least cols × that
export const LATCH_MIN={Normal:3,Hard:2,'Super Hard':2};
export const latchFits=(recipe,cls,P,cols=3)=>!(recipe||'none').split('+').includes('latch')||P>=cols*(LATCH_MIN[cls]+2);
// the generator's revision (README «The generator»): banked candidates and placed levels carry it as `gen`; the wave's
// rebuild (rebuildSet) leaves levels built under it alone. 2026-11-05: the Drop water channel ('canal') and the Twig
// wooden shutters ('shutters') in the builder (levels.mjs placeChannel, placeShutters; the channel's exit as a choice in the
// saved solution and the strategies). 2026-11-03c: latches keep their count range in any role (a
// Hard's additional latch 2–3). 2026-11-03b: key spots carved out of the picture (overlay.keys),
// a latch Normal with three types. 2026-11-03: the owner's amendment of 2026-11-03 (latches Hard
// 2–3 and Super Hard exactly 3; Hard links+latch+cells; latch Normals with hidden cells or pods in turn). 2026-11-02c: the placer keeps a regular Normal in the tuning
// zone 50–65 % (out of it only as a fallback; the dry placement leaves such a position open to build). 2026-11-02b: the owner's decisions of 2026-11-02 (families
// twice a wave 10 apart, breathers 75–90 % with one light mechanic, accents outside the 10+ window, the regular Normal
// aimed at the middle of its zone)
export const GEN='2026-11-05';
// a breather's light mechanics (17 §5 «Передышка», the owner, 2026-11-02): hidden pods or hidden cells by default (never
// both), one plain link pair when the share stays above the zone; a recipe of at most one of them
export const LIGHT=['hidden','cells','links'];
// may a breather carry this recipe? planRecipe (the wave plan's): the same recipe, or one plain link pair in place of a
// light mechanic (or of none) when hidden information alone cannot bring the share into the zone
export function breatherRecipeOk(recipe,planRecipe=null){const t=typesOf(recipe);if(t.length>1||t.some(x=>!LIGHT.includes(x)))return false;
 if(planRecipe==null||recipe===(planRecipe||'none'))return true;const pt=typesOf(planRecipe);return recipe==='links'&&pt.length<=1&&pt.every(x=>LIGHT.includes(x));}
// the breather's zone of the random test (17 §5, the owner, 2026-11-02; was 75–95 %)
export const BREATHER_ZONE=[.75,.9];

// ---- sequence rules shared by the planner, the placer and the audit ----
// picture repeats (17 §11 as amended by the owner on 2026-11-02, «Семейства — согласен»): a theme family, and a motif, at
// most twice in a wave and never closer than 10 levels to another appearance (was: once a wave and not in the 20 levels
// before it). p: {family, motifs}; entries: [{level, family, motifs}] of the other levels (placed or assigned), the wave's
// and the levels just before it; range: [first, last] level of the wave. Returns the first rule broken, else null.
export const REPEAT_GAP=10,REPEAT_PER_WAVE=2;
export function repeatClash(p,n,entries,range){const keys=[...(p&&p.family?[['family',p.family]]:[]),...((p&&p.motifs)||[]).map(m=>['motif',m])];
 for(const [kind,key] of keys){const same=entries.filter(e=>e.level!==n&&(kind==='family'?e.family===key:(e.motifs||[]).includes(key)));
  const near=same.filter(e=>Math.abs(e.level-n)<REPEAT_GAP);if(near.length)return {kind,key,rule:'near',with:near.map(e=>e.level)};
  const inWave=same.filter(e=>e.level>=range[0]&&e.level<=range[1]);if(inWave.length>=REPEAT_PER_WAVE)return {kind,key,rule:'wave',with:inWave.map(e=>e.level)};}
 return null;}
// the 10+ colour window (17 §2): at most two pictures of 10+ colours in any 20 consecutive levels. The picture's own colours
// count; an accent colour of §6 (2–4 recoloured cubes) does not (the owner, 2026-11-02, «окно — вариант а»). colours: the
// picture at n; others: [{level, colours}] (the pictures' own). True when n would break the window.
export function wideClash(n,colours,others,{limit=2,span=20}={}){if(!(colours>=10))return false;const w=others.filter(e=>e.level!==n&&e.colours>=10).map(e=>e.level);
 for(let s=n-span+1;s<=n;s++)if(w.filter(k=>k>=s&&k<s+span).length>=limit)return true;return false;}
// the median of a list (the mean of the two middle values for an even count)
export const median=v=>{if(!v.length)return null;const a=[...v].sort((x,y)=>x-y),k=a.length>>1;return a.length%2?a[k]:(a[k-1]+a[k])/2;};
// §5 the Normal median (amendment of 2026-10-27): in any 20 consecutive levels the median random-test share of the regular
// Normals is 50–60 %. regs: [{level, share}]; the windows [s, s+19] inside from–to (the whole range when shorter) holding
// at least `min` regular Normals. Returns every window with its median and whether it is out of range.
export const MEDIAN={lo:.5,hi:.6,aim:.56,span:20,min:5};
// the tuning zone of a regular Normal (tune.mjs ZONES.regular; the placer takes a candidate outside it only as a fallback)
export const REGULAR_ZONE=[.5,.65];
export function medianWindows(regs,from,to,{lo=MEDIAN.lo,hi=MEDIAN.hi,span=MEDIAN.span,min=MEDIAN.min}={}){const out=[];
 for(let s=from;s<=Math.max(from,to-span+1);s++){const e=Math.min(to,s+span-1),v=regs.filter(r=>r.level>=s&&r.level<=e).map(r=>r.share);if(v.length<min)continue;const m=median(v);out.push({from:s,to:e,n:v.length,median:m,out:m<lo-1e-9||m>hi+1e-9});}
 return out;}
// the placer's preference: how far the windows of the wave that hold n move out of 50–60 % with this share at n (the
// largest distance, 0 when every window stays in range)
export function medianPenalty(n,share,regs,range,opts={}){const all=[...regs.filter(r=>r.level!==n),{level:n,share}],span=opts.span||MEDIAN.span;
 const ws=medianWindows(all,Math.max(range[0],n-span+1),Math.min(range[1],n+span-1),opts).filter(w=>n>=w.from&&n<=w.to);
 return ws.reduce((d,w)=>Math.max(d,w.median>(opts.hi??MEDIAN.hi)?w.median-(opts.hi??MEDIAN.hi):w.median<(opts.lo??MEDIAN.lo)?(opts.lo??MEDIAN.lo)-w.median:0),0);}
// §5 «по цветам» (second amendment of 2026-10-27): "most work now" loses about one in three regular Normals on L4–24 and one
// in two from L25. regs: [{level, lost}]; returns per part {from, to, count, lost, need} (need = the audit's floor)
export function greedyQuota(regs,from,to){return [[4,24,1/3],[25,1e9,1/2]].map(([a,b,q])=>{const r=regs.filter(x=>x.level>=Math.max(a,from)&&x.level<=Math.min(b,to));
 return {from:Math.max(a,from),to:Math.min(b,to),q,count:r.length,lost:r.filter(x=>x.lost).length,need:Math.floor(q*r.length),target:Math.round(q*r.length)};}).filter(x=>x.count);}

// Which placed levels of a wave the owner's decisions of 2026-11-02 send back to the generator (rounds.mjs run --rebuild
// auto). levels: [{level, cls, intro, breather, regular, share, lost, recipe, gen, hand, pinned, family, motifs, colours}]
// of the wave (colours: the picture's own); context: [{level, family, motifs, colours}] of the levels before the wave;
// plan: level → {recipe, breather}; done: levels a rebuild already placed (left alone). A level built under GEN, a hand
// level and (for the repeat and colour rules) a pinned picture stay. In this order:
//  1. picture repeats (family or motif twice a wave, 10 levels apart) and the 10+ colour window: the later, cheaper level of
//     a clash is rebuilt on another picture (reassign; Normal before Hard before Super Hard);
//  2. breathers out of 75–90 %, with a recipe off the plan, or with two mechanics;
//  3. regular Normals while a 20-level window's median is out of 50–60 % (the furthest share first; a rebuilt level counts
//     at the tuning's aim);
//  4. regular Normals while «по цветам» loses fewer than about every second from L25 (a rebuilt level counts as one that
//     loses: it is built with --want-loss), those «по цветам» wins with the share furthest from the aim first.
// Returns [{level, why, reassign}].
export function rebuildSet(levels,{from,to,context=[],plan=new Map(),done=new Set()}={}){const S=new Map(),by=new Map(levels.map(L=>[L.level,L]));
 const free=L=>L&&!L.hand&&L.gen!==GEN&&!done.has(L.level),add=(L,why,reassign=false)=>{const x=S.get(L.level);if(x){x.why.push(why);x.reassign=x.reassign||reassign;}else S.set(L.level,{level:L.level,why:[why],reassign});};
 const cost=L=>(L.cls==='Normal'?0:L.cls==='Hard'?10:20)+(L.breather?1:0)-L.level/1000;
 // 1. repeats and the colour window, against the levels before the wave and the wave's levels not yet sent back
 for(let guard=0;guard<200;guard++){let hit=null;const live=[...context,...levels.filter(L=>!(S.get(L.level)||{}).reassign)];
  for(const L of levels){if((S.get(L.level)||{}).reassign)continue;const c=repeatClash(L,L.level,live,[from,to]);
   if(c){const pool=[L,...c.with.map(k=>by.get(k))].filter(x=>x&&free(x)&&!x.pinned);if(pool.length){hit={L:pool.sort((u,v)=>cost(u)-cost(v))[0],why:`${c.kind==='family'?'семейство':'мотив'} «${c.key}» ${c.rule==='near'?'ближе 10 уровней':'третий раз в волне'} (L${[L.level,...c.with].sort((u,v)=>u-v).join(', L')})`};break;}}
   if(wideClash(L.level,L.colours,live)){const ws=live.filter(e=>e.colours>=10&&Math.abs(e.level-L.level)<20).map(e=>by.get(e.level)).filter(x=>x&&free(x)&&!x.pinned);
    if(free(L)&&!L.pinned)ws.push(L);if(ws.length){hit={L:ws.sort((u,v)=>cost(u)-cost(v))[0],why:'третья картинка на 10+ цветов в 20 уровнях (§2)'};break;}}}
  if(!hit)break;add(hit.L,hit.why,true);}
 // 2. breathers
 // (a breather already at its last fallback, one plain link pair or the generator's nearest layout, stays)
 for(const L of levels){if(!L.breather||!free(L)||L.zoneMiss||L.recipe==='links')continue;const pl=plan.get(L.level)||{},why=[];
  if(!(L.share>=BREATHER_ZONE[0]-1e-9&&L.share<=BREATHER_ZONE[1]+1e-9))why.push(`передышка ${Math.round(L.share*100)} % вне 75–90 %`);
  if(!breatherRecipeOk(L.recipe,pl.recipe??null))why.push(`передышка: рецепт ${L.recipe}, по плану ${pl.recipe??'—'}`);
  for(const w of why)add(L,w);}
 // 3. the median windows (a level sent back counts at the aim)
 const regs=()=>levels.filter(L=>L.regular).map(L=>({level:L.level,share:S.has(L.level)?MEDIAN.aim:L.share}));
 for(let guard=0;guard<100;guard++){const bad=medianWindows(regs(),from,to).find(w=>w.out);if(!bad)break;const high=bad.median>MEDIAN.hi;
  const pick=levels.filter(L=>L.regular&&!S.has(L.level)&&free(L)&&L.level>=bad.from&&L.level<=bad.to&&(high?L.share>MEDIAN.hi:L.share<MEDIAN.lo)).sort((u,v)=>high?v.share-u.share:u.share-v.share)[0];
  if(!pick)break;add(pick,`медиана L${bad.from}–L${bad.to} ${Math.round(bad.median*100)} % вне 50–60 % (бот ${Math.round(pick.share*100)} %)`);}
 // 4. the «по цветам» quota (a level sent back is built to lose it)
 for(let guard=0;guard<100;guard++){const q=greedyQuota(levels.filter(L=>L.regular).map(L=>({level:L.level,lost:S.has(L.level)||L.lost})),from,to).find(x=>x.lost<x.target);if(!q)break;
  const pick=levels.filter(L=>L.regular&&!S.has(L.level)&&free(L)&&!L.lost&&L.level>=q.from&&L.level<=q.to).sort((u,v)=>Math.abs(v.share-MEDIAN.aim)-Math.abs(u.share-MEDIAN.aim))[0];
  if(!pick)break;add(pick,`«по цветам» проигрывает ${q.lost} из ${q.count} (нужно около ${q.target})`);}
 return [...S.values()].sort((u,v)=>u.level-v.level);}

const hash=(s,n)=>(([...s].reduce((h,c)=>(h*31+c.charCodeAt(0))>>>0,7)^Math.imul(n,2654435761))>>>0)/4294967296;
const typesOf=r=>(r||'none')==='none'?[]:r.split('+');

// May picture p take position q (band aside)? p: {id, N, colours, pack, hidden (colours hidden at the start), weak,
// holes (key holes), P (pods by §3), family, motifs, fits (levels whose stage it fits)}; q: {level, cls, intro, recipe,
// breather, after (the first Normal after a Hard), latchNeed}. Hard and Super Hard need 3+ colours hidden at the start (17 §2); a weak picture only with a
// mechanic; two mechanic types need 16+ pods (§7); a latch needs key holes; a breather position takes a picture up to
// ~550 cells first, other Normal positions never one (it would be a breather by 17 §5).
export function eligible(p,q){if(!p.fits.has(q.level))return false;const t=typesOf(q.recipe);
 if(q.cls!=='Normal'&&p.hidden<3)return false;if(q.cls==='Normal'&&!q.intro&&p.weak&&!t.length)return false;
 if(!recipeFitsPods(q.recipe,p.P))return false;if((q.latchNeed||0)>(p.holes||0))return false;if(!latchFits(q.recipe,q.cls,p.P,p.N<1000?3:4))return false;
 // a small picture on a Normal position makes it a breather (17 §5): only a light recipe (none, hidden pods, hidden cells,
 // a link pair) fits it
 if(!q.breather&&q.cls==='Normal'&&!q.intro&&q.level>3&&p.N<=BREATHER_N&&!breatherRecipeOk(q.recipe))return false;
 if(q.breather&&!breatherRecipeOk(q.recipe))return false;return true;}

// The assignment. positions: the wave's open non-bridge positions in level order; pool: the free pictures (above);
// pinned: Map level → picture id (the pins reserved by rounds.mjs reservePins, and earlier assignments kept: kept on their
// positions); context: {before: [{level, picture}] placed levels before and in the wave, picOf(id) → pool entry or a
// picture record with family / motifs / pack / colours (the picture's own)}. Hard rules (never relaxed): a theme family
// and a motif at most twice in the wave and not within 10 levels of another appearance (17 §11 as amended on 2026-11-02),
// at most two pictures of 10+ colours in any 20 levels (§2; the picture's own colours, accents do not count). Soft rules,
// relaxed in this order only when no picture is left: neighbours from different packs (17 §11), then the band of the size
// mix spread evenly (each free position takes the band furthest behind its even share; never three in a row of one band
// while another has quota). Scores: Hard / Super Hard more colours hidden at the start first; a strong picture costs a
// little on a Normal; an introduction prefers a weak picture; a breather the smallest picture up to ~550 cells (another
// Normal avoids one: it would become a breather); a pack already used in the wave costs a little. Returns {assign:
// [{level, picture, band, N, relaxed}], missing: [{level, reason}]}.
// waveStart, waveEnd: the wave's first and last level (the repeat rule counts appearances in the wave)
export function assignPictures(positions,pool,{pinned=new Map(),before=[],picOf=null,seed=1,waveStart=null,waveEnd=null}={}){
 const byId=new Map(pool.map(p=>[p.id,p])),info=id=>byId.get(id)||(picOf&&picOf(id))||{id},used=new Set(before.map(x=>x.picture)),at=new Map(before.map(x=>[x.level,x.picture]));
 const out=new Map(),bandAt=new Map(),n=positions.length,quota=bandCounts(n),count=Object.fromEntries(SIZE_MIX.map(b=>[b.band,0]));
 const bandFor=(N,prefer)=>{const bs=bandsOf(N);return bs.find(b=>b===prefer)||bs.find(b=>count[b]<quota[b])||bs[0]||SIZE_MIX[0].band;};
 // pins first: on their positions, their band counted
 for(const q of positions){const id=pinned.get(q.level);if(!id||!byId.has(id))continue;const p=byId.get(id),b=bandFor(p.N);out.set(q.level,{level:q.level,picture:id,band:b,N:p.N,pinned:true});bandAt.set(q.level,b);count[b]++;used.add(id);at.set(q.level,id);}
 // a position with its band given (a replacement) keeps it; breathers next: the small band
 for(const q of positions)if(q.band&&!out.has(q.level)&&!bandAt.has(q.level)){bandAt.set(q.level,q.band);count[q.band]=(count[q.band]||0)+1;}
 const small=SIZE_MIX[2].band;for(const q of positions)if(q.breather&&!out.has(q.level)&&count[small]<quota[small]){bandAt.set(q.level,small);count[small]++;}
 // the bands of the other positions: the free ones share the remaining quota evenly (deficit order), no run of three
 const free=positions.filter(q=>!bandAt.has(q.level)),left=Object.fromEntries(SIZE_MIX.map(b=>[b.band,Math.max(0,quota[b.band]-count[b.band])])),got=Object.fromEntries(SIZE_MIX.map(b=>[b.band,0]));
 // stride scheduling: band b's k-th free position ideally at (k + ½) · F / left_b; all ideal points merged in order, so the
 // bands interleave evenly; then a run of three of one band is broken by swapping with the nearest later free position
 // of another band that makes no new run
 const F=free.length,pts=[];for(const b of SIZE_MIX)for(let k=0;k<left[b.band];k++)pts.push({b:b.band,x:(k+.5)*F/left[b.band],o:SIZE_MIX.indexOf(b)});
 pts.sort((u,v)=>u.x-v.x||v.o-u.o);free.forEach((q,j)=>{const b=(pts[j]||{b:SIZE_MIX[0].band}).b;bandAt.set(q.level,b);got[b]++;});
 const seq=positions.map(q=>q.level),isFree=new Set(free.map(q=>q.level)),bd=i=>bandAt.get(seq[i]),run=i=>i>=2&&bd(i)&&bd(i)===bd(i-1)&&bd(i)===bd(i-2);
 const makesRun=i=>[i,i+1,i+2].some(k=>k<seq.length&&run(k));
 for(let i=2;i<seq.length;i++){if(!run(i))continue;const cands=isFree.has(seq[i])?[i]:isFree.has(seq[i-1])?[i-1]:[i-2].filter(k=>isFree.has(seq[k]));
  let done=false;for(const a of cands){for(let j=i+1;j<seq.length&&!done;j++){if(!isFree.has(seq[j])||bd(j)===bd(a))continue;const A=bd(a),B=bd(j);bandAt.set(seq[a],B);bandAt.set(seq[j],A);
    if(makesRun(a-2)||makesRun(j-2)){bandAt.set(seq[a],A);bandAt.set(seq[j],B);}else done=true;}if(done)break;}}
 // the pictures, position by position, in level order
 const packUse={};for(const x of out.values()){const k=info(x.picture).pack;packUse[k]=(packUse[k]||0)+1;}
 const lo=waveStart??Math.min(...positions.map(q=>q.level),Infinity),hi=waveEnd??Math.max(lo+49,...positions.map(q=>q.level)),missing=[];
 const entries=()=>[...at.entries()].map(([k,id])=>{const p=info(id);return {level:k,family:p.family||null,motifs:p.motifs||[],colours:p.colours||0};});
 for(const q of positions){if(out.has(q.level))continue;const band=bandAt.get(q.level),nb=[at.get(q.level-1),at.get(q.level+1)].filter(Boolean).map(id=>info(id).pack),es=entries();
  // the hard rules: repeats (families, motifs) and the 10+ colour window
  const base=pool.filter(p=>!used.has(p.id)&&eligible(p,q)&&!repeatClash(p,q.level,es,[lo,hi])&&!wideClash(q.level,p.colours||0,es));
  const rules=[p=>bandsOf(p.N).includes(band),p=>!nb.includes(p.pack)];
  // relax from the end: packs, then the band
  const names=['band','pack'];let cand=[],relaxed=[];for(let k=rules.length;k>=0&&!cand.length;k--){cand=base.filter(p=>rules.slice(0,k).every(f=>f(p)));relaxed=names.slice(k);}
  if(!cand.length){missing.push({level:q.level,band,reason:'нет свободной картинки, которая подходит позиции (этап, класс, рецепт, место для ключа, повторы семейств и мотивов, окно 10+ цветов)'});continue;}
  // a regular Normal aims at 50–75 %: the calibration's weak pictures (0–1 colours hidden at the start) stay near 100 % with
  // any queue, so it prefers 2–4 hidden; the first Normal after a Hard (75–95 %), a breather and an introduction the weaker
  const fit=p=>q.cls!=='Normal'?-p.hidden:q.intro||q.breather||q.after?(p.hidden>=3?.5:0):p.hidden<2?(2-p.hidden)*.4:p.hidden>4?(p.hidden-4)*.2:0;
  const score=p=>fit(p)+(q.intro?(p.weak?-.3:0):0)+(q.breather?(p.N<=BREATHER_N?p.N/2000:1+p.N/2000):q.cls==='Normal'&&!q.intro&&p.N<=BREATHER_N?1:0)+.12*(packUse[p.pack]||0)+hash(p.id,q.level+seed)*.05;
  const p=cand.sort((a,b)=>score(a)-score(b))[0];out.set(q.level,{level:q.level,picture:p.id,band,N:p.N,...(relaxed.length?{relaxed}:{})});used.add(p.id);at.set(q.level,p.id);packUse[p.pack]=(packUse[p.pack]||0)+1;}
 return {assign:positions.filter(q=>out.has(q.level)).map(q=>out.get(q.level)),missing,quota};}

// A replacement for a position whose picture failed its class after the tuning budget: the best free picture of the same
// band that fits it (never a sweep of the pool), with the same rules as the assignment; tried = the pictures already tried.
export function replacement(q,band,pool,{used=new Set(),tried=[],before=[],picOf=null,seed=1,waveStart=null,waveEnd=null}={}){
 const skip=new Set([...used,...tried]),r=assignPictures([{...q,band}],pool.filter(p=>!skip.has(p.id)&&bandsOf(p.N).includes(band)),{before,picOf,seed:seed+tried.length,waveStart,waveEnd});
 return r.assign[0]||null;}

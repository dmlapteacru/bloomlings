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
 if(!q.breather&&q.cls==='Normal'&&!q.intro&&p.N<=BREATHER_N&&t.length>1)return false;
 if(q.breather&&t.length>1)return false;return true;}

// The assignment. positions: the wave's open non-bridge positions in level order; pool: the free pictures (above);
// pinned: Map level → picture id (the pins reserved by rounds.mjs reservePins: kept on their positions); context:
// {before: [{level, picture}] placed levels before and in the wave, picOf(id) → pool entry or a picture record with
// family / motifs / pack / colours}. Rules, hard first and relaxed in this order only when no picture is left: the band
// of the size mix spread evenly (each free position takes the band furthest behind its even share; never three in a
// row of one band while another has quota), neighbours from different packs (17 §11), a theme family once in the wave
// and not in the 20 levels before it, a motif not within 10 levels (§11, 29.10), at most 2 pictures of 10+ colours in
// any 20 levels (§2). Scores: Hard / Super Hard more colours hidden at the start first; a strong picture costs a
// little on a Normal; an introduction prefers a weak picture; a breather the smallest picture up to ~550 cells (another
// Normal avoids one: it would become a breather); a pack
// already used in the wave costs a little. Returns {assign: [{level, picture, band, N, relaxed}], missing: [{level, reason}]}.
// waveStart: the wave's first level (a family is not repeated from 20 levels before it, 17 §11); before entries may carry
// the placed level's colours (with its accents)
export function assignPictures(positions,pool,{pinned=new Map(),before=[],picOf=null,seed=1,waveStart=null}={}){
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
 const lo=waveStart??Math.min(...positions.map(q=>q.level),Infinity),missing=[],colAt=new Map(before.filter(x=>x.colours!=null).map(x=>[x.level,x.colours]));
 // a picture that may take accents (Hard, Super Hard, a position the plan marks accents) gains a colour: 9 counts as 10
 const effCol=(p,q)=>(p.colours||0)+((q.cls!=='Normal'||q.accents)&&(p.colours||0)===9?1:0);
 const famIn=(fam,n,from)=>fam&&[...at.entries()].some(([k,id])=>k!==n&&k>=from&&info(id).family===fam);
 const motifNear=(ms,n)=>ms&&ms.length&&[...at.entries()].some(([k,id])=>k!==n&&Math.abs(k-n)<10&&(info(id).motifs||[]).some(m=>ms.includes(m)));
 const wideNear=(n)=>[...at.entries()].filter(([k,id])=>k!==n&&Math.abs(k-n)<20&&(colAt.get(k)??(info(id).colours||0))>=10).length;
 for(const q of positions){if(out.has(q.level))continue;const band=bandAt.get(q.level),nb=[at.get(q.level-1),at.get(q.level+1)].filter(Boolean).map(id=>info(id).pack);
  const base=pool.filter(p=>!used.has(p.id)&&eligible(p,q));
  const rules=[p=>bandsOf(p.N).includes(band),p=>!nb.includes(p.pack),p=>!famIn(p.family,q.level,lo-20),p=>!motifNear(p.motifs,q.level),p=>!(effCol(p,q)>=10&&wideNear(q.level)>=2)];
  // relax from the end: the 10-colour window, then motifs, families, packs; the band last
  const names=['band','pack','family','motif','colours'];let cand=[],relaxed=[];for(let k=rules.length;k>=0&&!cand.length;k--){cand=base.filter(p=>rules.slice(0,k).every(f=>f(p)));relaxed=names.slice(k);}
  if(!cand.length){missing.push({level:q.level,band,reason:'нет свободной картинки, которая подходит позиции (этап, класс, рецепт, место для ключа)'});continue;}
  // a regular Normal aims at 50–75 %: the calibration's weak pictures (0–1 colours hidden at the start) stay near 100 % with
  // any queue, so it prefers 2–4 hidden; the first Normal after a Hard (75–95 %), a breather and an introduction the weaker
  const fit=p=>q.cls!=='Normal'?-p.hidden:q.intro||q.breather||q.after?(p.hidden>=3?.5:0):p.hidden<2?(2-p.hidden)*.4:p.hidden>4?(p.hidden-4)*.2:0;
  const score=p=>fit(p)+(q.intro?(p.weak?-.3:0):0)+(q.breather?(p.N<=BREATHER_N?p.N/2000:1+p.N/2000):q.cls==='Normal'&&!q.intro&&p.N<=BREATHER_N?1:0)+.12*(packUse[p.pack]||0)+hash(p.id,q.level+seed)*.05;
  const p=cand.sort((a,b)=>score(a)-score(b))[0];out.set(q.level,{level:q.level,picture:p.id,band,N:p.N,...(relaxed.length?{relaxed}:{})});used.add(p.id);at.set(q.level,p.id);colAt.set(q.level,effCol(p,q));packUse[p.pack]=(packUse[p.pack]||0)+1;}
 return {assign:positions.filter(q=>out.has(q.level)).map(q=>out.get(q.level)),missing,quota};}

// A replacement for a position whose picture failed its class after the tuning budget: the best free picture of the same
// band that fits it (never a sweep of the pool), with the same rules as the assignment; tried = the pictures already tried.
export function replacement(q,band,pool,{used=new Set(),tried=[],before=[],picOf=null,seed=1,waveStart=null}={}){
 const skip=new Set([...used,...tried]),r=assignPictures([{...q,band}],pool.filter(p=>!skip.has(p.id)&&bandsOf(p.N).includes(band)),{before,picOf,seed:seed+tried.length,waveStart});
 return r.assign[0]||null;}

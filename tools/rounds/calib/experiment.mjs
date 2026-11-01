// The calibration experiment (README.md here): ~30 free approved non-bridge pictures, stratified by size and strength;
// on each the queue parameters swept over their whole range (a fractional design), each configuration measured with the
// cheap tests. Raw rows (JSONL) in content/work/calib/raw/. Never writes content/rounds (ROUNDS_DIR = a private copy).
//
//   node tools/rounds/calib/experiment.mjs pictures            the chosen pictures and their features (raw/pictures.json)
//   node tools/rounds/calib/experiment.mjs run <i>/<n>         shard i of n (resumable: rows already written are skipped)
//   node tools/rounds/calib/experiment.mjs design              the configuration count per picture and part
//
// Parts of the design per picture:
//   A (21) t ∈ 0, 1/6 … 1 × P at the budget's low / middle / high end; sigma, columns and the holding pod in a Latin square
//   B (16 × 3) at t = 1/2, 5/6, 1 one base queue (fixed seed) and its overlays on the SAME queue: links 12 / 30 / 50 % of P
//      × similar / trap; hidden pods 2 spread, 4 spread, 4 run; hidden cells 5 / 10 % of N; a latch (where key holes
//      exist); links 30 % + 2 hidden; links 30 % trap + 5 % cells; accents (one material, 17 §6). (A pilot showed that
//      below t = 1/2 almost every queue without links is near 100 %, so the overlays sit where a change can show.)
//   C (6)  replicates: t = 1/2, 5/6 × 3 seeds at fixed other parameters (the spread a formula cannot remove)
// Measured: the quick share (50 games of the §5 random policy, measure.mjs quickShare), the calm share (30 calm games),
// "most work now" / "release fastest" (measure.mjs greedy); on every fourth row and on the base, mid-link, 4-hidden,
// 10 %-cells, latch and accent rows of B also the forced share and «стоят без работы» (pressure, 30 games each).
import fs from 'node:fs';import path from 'node:path';
const HERE=path.dirname(new URL(import.meta.url).pathname),ROOT=path.resolve(HERE,'../../..');
process.env.ROUNDS_DIR=process.env.ROUNDS_DIR||path.join(ROOT,'content/work/calib/rounds-copy');
const ARGV=process.argv.slice(2);process.argv.splice(2,process.argv.length,'noop');
const Q=await import('./queue.mjs'),L=await import('../lib.mjs'),M=await import('../measure.mjs');
const OUT=path.join(ROOT,'content/work/calib/raw');fs.mkdirSync(OUT,{recursive:true});

// ---- the pictures: free (no placed level uses them), approved, no wall or bridge site ('#', 'b'), not L1–3 ----
const TARGET={'200-399':{weak:2,mid:2,strong:2},'400-699':{weak:2,mid:1,strong:0},'700-999':{weak:3,mid:3,strong:4},'1000+':{weak:2,mid:3,strong:6}};
const binOf=N=>N<400?'200-399':N<700?'400-699':N<1000?'700-999':'1000+';
export function choosePictures(){const u=L.usage(),out=[];const byBin={};
 for(const e of L.manifest().pictures){if(e.status!=='approved'||u[e.id])continue;const p=L.picture(e.id);if(!p||p.locked||/[#b]/.test(p.rows.join('')))continue;
  const f=Q.pictureFeatures(p),s=f.weak?'weak':f.hiddenColours>=4?'strong':'mid',k=binOf(f.N)+'|'+s;(byBin[k]=byBin[k]||[]).push({id:e.id,f});}
 for(const [bin,ss] of Object.entries(TARGET))for(const [s,n] of Object.entries(ss)){const pool=(byBin[bin+'|'+s]||[]).sort((a,b)=>a.f.N-b.f.N);if(!n||!pool.length)continue;
  // evenly spread over the bin's sizes; with key holes preferred at equal spacing (the latch needs one)
  for(let j=0;j<Math.min(n,pool.length);j++){const at=Math.floor((j+.5)*pool.length/n),win=pool.slice(Math.max(0,at-2),at+3).filter(x=>!out.some(o=>o.id===x.id)),pick=win.find(x=>x.f.holes>0)||win[0]||pool[at];out.push({id:pick.id,bin,strength:s,features:pick.f});}}
 return out;}

// ---- the design ----
const T=[0,1/6,2/6,3/6,4/6,5/6,1];
export function design(pic,f){const colsOpt=L.columnsFor(f.N),out=[],base={sigma:.16,hold:0,links:0,linkStyle:'similar',hidden:0,hiddenStyle:'spread',cells:0,latch:0,accents:0};
 const P=lev=>Q.podTarget(f.N,lev),add=(part,o)=>out.push({id:`${pic}|${part}|${out.length}`,pic,part,...base,...o});
 for(let ti=0;ti<7;ti++)for(let pi=0;pi<3;pi++)add('A',{t:T[ti],Plev:[0,.5,1][pi],P:P([0,.5,1][pi]),sigma:[.06,.16,.26][(ti+pi)%3],cols:colsOpt[(ti+2*pi)%colsOpt.length],hold:(ti+pi)%2,seed:1000+ti*10+pi});
 for(const ti of [3,5,6]){const b={t:T[ti],Plev:.5,P:P(.5),cols:colsOpt[ti%colsOpt.length],seed:2000+ti},Pm=P(.5),lk=s=>Math.max(2,2*Math.round(s*Pm/2));
  add('B',{...b,overlay:'base'});
  for(const [nm,s] of [['low',.12],['mid',.3],['high',.5]])for(const st of ['similar','trap'])add('B',{...b,overlay:`links-${nm}-${st}`,links:lk(s),linkStyle:st});
  for(const [h,st] of [[2,'spread'],[4,'spread'],[4,'run']])add('B',{...b,overlay:`hidden-${h}-${st}`,hidden:h,hiddenStyle:st});
  for(const c of [.05,.1])add('B',{...b,overlay:`cells-${c}`,cells:c});
  if(f.holes>0)add('B',{...b,overlay:'latch-1',latch:1});
  add('B',{...b,overlay:'links-mid+hidden-2',links:lk(.3),hidden:2});
  add('B',{...b,overlay:'links-mid-trap+cells-0.05',links:lk(.3),linkStyle:'trap',cells:.05});
  add('B',{...b,overlay:'accents',accents:1});}
 for(const ti of [3,5])for(let s=0;s<3;s++)add('C',{t:T[ti],Plev:.5,P:P(.5),cols:colsOpt[0],seed:3000+ti*10+s});
 return out;}

// ---- one configuration measured ----
const ctxCache={},GAMES=50;
export async function measureOne(cfg,{full=false}={}){const t0=Date.now(),key=cfg.pic+(cfg.accents?'+a':'');
 const ctx=ctxCache[key]||(ctxCache[key]=Q.context(L.picture(cfg.pic),{accents:!!cfg.accents,seed:cfg.seed}));
 const b=Q.build(ctx,cfg);if(b.error)return {...cfg,error:b.error,ms:Date.now()-t0};
 const qf=Q.queueFeatures(ctx,b),q=await M.quickShare(b.level,GAMES,11),calm=(await M.calmTest(b.level,{first:30,max:30})).share,g=await M.greedy(b.level);
 const row={...cfg,P:qf.P,recipe:b.level.recipe,q,wins:Math.round(q*GAMES),games:GAMES,calm,mostNow:g['most-now|left']||g['most-now|right'],fastRelease:g['fast-release|left']||g['fast-release|right'],qf,accentsPainted:!!ctx.accents};
 if(full){row.forced=await M.forcedShare(b.level);row.pressure=(await M.pressure(b.level,30)).stuck;}
 row.ms=Date.now()-t0;return row;}

const cmd=ARGV[0];
if(cmd==='pictures'){const ps=choosePictures();fs.writeFileSync(path.join(OUT,'pictures.json'),JSON.stringify(ps,null,1));
 for(const p of ps)console.log(p.id,p.bin,p.strength,`N ${p.features.N} цв ${p.features.colours} скрыто ${p.features.hiddenColours} видно ${p.features.visible} слоёв ${p.features.layers} ям ${p.features.holes}`);console.log(ps.length,'картинок');}
else if(cmd==='design'){const ps=JSON.parse(fs.readFileSync(path.join(OUT,'pictures.json'),'utf8'));let n=0;for(const p of ps){const d=design(p.id,p.features);n+=d.length;console.log(p.id,d.length);}console.log('всего',n);}
else if(cmd==='run'){const [si,sn]=(ARGV[1]||'0/1').split('/').map(Number),ps=JSON.parse(fs.readFileSync(path.join(OUT,'pictures.json'),'utf8')),only=ARGV[2]?new Set(ARGV[2].split(',')):null;
 const all=ps.filter(p=>!only||only.has(p.id)).flatMap(p=>design(p.id,p.features)),file=path.join(OUT,`rows-${si}.jsonl`);
 const done=new Set();for(const f of fs.readdirSync(OUT).filter(f=>f.startsWith('rows-')))for(const l of fs.readFileSync(path.join(OUT,f),'utf8').split('\n'))if(l.trim())try{done.add(JSON.parse(l).id);}catch{}
 // interleave the pictures so a shard's rows cover all of them early (a partial run is still usable)
 const mine=all.filter((c,i)=>i%sn===si&&!done.has(c.id)).sort((a,b)=>(+a.id.split('|')[2])-(+b.id.split('|')[2])||a.pic.localeCompare(b.pic));
 console.log(`шард ${si}/${sn}: ${mine.length} конфигураций`);const t0=Date.now();let k=0;
 for(const c of mine){const row=await measureOne(c,{full:(+c.id.split('|')[2])%4===0||c.part==='B'&&/^(base|links-mid|hidden-4-spread|cells-0.1|latch|accents)/.test(c.overlay||'')});fs.appendFileSync(file,JSON.stringify(row)+'\n');k++;
  if(k%20===0)console.log(`${k}/${mine.length} ${((Date.now()-t0)/60000).toFixed(1)} мин`);}
 console.log('готово',((Date.now()-t0)/60000).toFixed(1),'мин');}
else console.log(fs.readFileSync(new URL(import.meta.url),'utf8').split('\n').filter(l=>l.startsWith('//')).join('\n'));

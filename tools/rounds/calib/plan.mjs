// The formula-driven builder (README.md here): plan(picture, class, recipe) → queue parameters predicted by the fitted
// share model (content/work/calib/model.json) to land in the class zone; a short correction loop on the quick share
// (≤ 5 quick measurements: Newton steps on the blend t with the model's slope, then bisection once both sides of the zone
// were seen; a picture keeps the pods, columns and t of its last attempt that measured in the zone); for Hard and Super Hard the route-first prototype's mechanic
// step (links chosen one pair at a time so that "most work now" gets least far, a trap pair first); then the full
// levels.mjs evaluate and the placer's filters of the slot, as today.
//
//   node tools/rounds/calib/plan.mjs plan <P####> <Normal|Hard|"Super Hard"> <recipe> [--slot regular|after-hard|hard]
//   node tools/rounds/calib/plan.mjs build <P####> --cls Hard --recipe links+hidden [--slot hard] [--minutes 15] [--want 1] [--label L0020]
//   node tools/rounds/calib/plan.mjs validate [label…]   the validation jobs (JOBS below), one after another
//   node tools/rounds/calib/plan.mjs today <label…>      today's builder on those jobs (levels.mjs build on the copy) for the time comparison
//   node tools/rounds/calib/plan.mjs report              content/work/calib/validation.md + preview levels L1101… (odd: the
//        placed level, even: the formula level) for tools/gameplay-pilot-v12/build.mjs --preview content/work/calib/preview
// ROUNDS_DIR defaults to the private copy content/work/calib/rounds-copy: nothing is written into content/rounds.
import fs from 'node:fs';import path from 'node:path';
const HERE=path.dirname(new URL(import.meta.url).pathname),ROOT=path.resolve(HERE,'../../..'),W=path.join(ROOT,'content/work/calib');
process.env.ROUNDS_DIR=process.env.ROUNDS_DIR||path.join(W,'rounds-copy');
const ARGV=process.argv.slice(2);process.argv.splice(2,process.argv.length,'noop');
const Q=await import('./queue.mjs'),MD=await import('./model.mjs'),L=await import('../lib.mjs'),LV=await import('../levels.mjs'),M=await import('../measure.mjs');
const {engine}=await import('../engine.mjs'),{STRATEGIES}=await import('../../level-stats/strategies.mjs'),{fitness}=await import('../fitness.mjs');
const E=await engine(),{R}=E;
const opt=(k,d=null)=>{const i=ARGV.indexOf(k);return i>=0?ARGV[i+1]:d;};

// the class zones the placer uses (levels.mjs place, route-first BAND; 17 §5): a regular Normal 50–75 % aiming at 56–60 %,
// the first Normal after a Hard 75–95 %, Hard 20–50 % (preferred 25–40 %), Super Hard 5–20 % (preferred 7–15 %)
export const ZONE={regular:[.5,.75,.58],'after-hard':[.75,.95,.85],Hard:[.2,.5,.33],'Super Hard':[.05,.2,.12]};
const zoneOf=(cls,slot)=>cls==='Normal'?ZONE[slot==='after-hard'?'after-hard':'regular']:ZONE[cls];
export const loadModel=(f=path.join(W,'model.json'))=>JSON.parse(fs.readFileSync(f,'utf8'));
const Phi=x=>{const t=1/(1+.2316419*Math.abs(x)),d=.3989423*Math.exp(-x*x/2),p=d*t*(.3193815+t*(-.3565638+t*(1.781478+t*(-1.821256+t*1.330274))));return x>0?1-p:p;};

// ---- the recipe's doses for one P (levels.mjs doses: the lead its dose place of the §7 cycle, an additional one its minimum) ----
function dosesFor(recipe,cls,P,N,pos){const types=recipe==='none'?[]:recipe.split('+'),dz=LV.doses(recipe,cls,P,false,N);if(dz.error)return null;const lead=types[0],o={links:0,hidden:0,cells:0,latch:0};
 for(const m of types){const [a,b]=dz[m];const at=m===lead&&pos!=null?pos:0;
  if(m==='links')o.links=a+2*Math.round(at*(b-a)/2);else if(m==='hidden')o.hidden=Math.min(6,a+Math.round(at*(b-a)));else if(m==='cells')o.cellsN=a+Math.round(at*(b-a));else if(m==='latch')o.latch=a+Math.round(at*(b-a));}
 return o;}
// the model's row for a parameter set (the same features the experiment recorded)
function rowOf(f,p){return {f,t:p.t,sigma:p.sigma,cols:p.cols,P:p.P,hold:p.hold,linkStyle:p.linkStyle,hiddenStyle:p.hiddenStyle,accentsPainted:!!p.accents,
 qf:{linkShare:p.links/p.P,hidden:p.hidden,cellsHidden:p.cellsN||0,latches:p.latch,rankCorr:1-2*p.t,deepTop:0,frontTop:1}};}
// plan: the parameter set whose predicted share lands in the zone with the highest probability (residual spread of the
// model on the logit scale, measured by cross-validation), ties broken by the distance to the zone's aim; offset is a
// per-picture correction learned from earlier measurements (logit observed − logit predicted)
export function plan(pic,cls,recipe,{slot='regular',model=loadModel(),attempt=0,offset=0,accents=null,fixed={}}={}){const f=Q.pictureFeatures(pic),[lo,hi,aim]=zoneOf(cls,slot),s=model.logitResidualSd||1;
 const pos=recipe==='none'?null:LV.DOSE_CYCLE[attempt%LV.DOSE_CYCLE.length],acc=accents??(cls!=='Normal'||attempt%3===1),best=[];
 for(const Plev of fixed.Plev!=null?[fixed.Plev]:[0,.5,1]){const P=Q.podTarget(f.N,Plev),d=dosesFor(recipe,cls,P,f.N,pos);if(!d)continue;
  for(const cols of fixed.cols?[fixed.cols]:L.columnsFor(f.N))for(const sigma of [.06,.12])for(let t=0;t<=1.0001;t+=.02){
   const p={t:Math.round(t*100)/100,sigma,cols,P,Plev,hold:cls==='Normal'&&slot==='regular'?1:0,linkStyle:cls==='Normal'?'similar':'trap',hiddenStyle:'spread',accents:acc,...d,cells:d.cellsN?d.cellsN/f.N:0};
   const e=MD.eta(model,rowOf(f,p))+offset,pin=Phi((MD.logit(hi)-e)/s)-Phi((MD.logit(lo)-e)/s);best.push({p,eta:e,pred:MD.sigmoid(e),pin,dist:Math.abs(e-MD.logit(aim))});}}
 best.sort((a,b)=>b.pin-a.pin||a.dist-b.dist);return best[0]?{...best[0],f,zone:[lo,hi,aim]}:null;}
// d eta / d t of the model at a parameter set (numerical)
const slopeT=(model,f,p)=>{const a=MD.eta(model,rowOf(f,{...p,t:Math.max(0,p.t-.05)})),b=MD.eta(model,rowOf(f,{...p,t:Math.min(1,p.t+.05)}));return (b-a)/(Math.min(1,p.t+.05)-Math.max(0,p.t-.05));};

// ---- the route-first mechanic step for Hard and Super Hard: links chosen so that the greedy strategies get least far ----
// (tools/rounds/proto/route-first.mjs addLinks/greedyReach, without the route: winnability is the solver's in evaluate)
function greedyReach(level,cls){const {T}=E;let best=0;for(const k of cls==='Super Hard'?['most-now','fast-release']:['most-now']){const pol=STRATEGIES[k].make(1,E),t=new T.Timeline(R.create(level,5)),total=t.state.board.reduce((n,c)=>n+(c>=0),0);let g=0;
  while(g++<100000){if(t.state.status==='Won')break;const m=R.legal(t.state).filter(a=>!t.check(a));if(m.length){const a=pol(t,m);if(!a||t.check(a)){if(t.pending){t.advance(t.steps[0].at-t.now);continue;}break;}t.commit(a);continue;}if(!t.pending)break;t.advance(t.steps[0].at-t.now);}
  best=Math.max(best,t.state.status==='Won'?1.5:t.processedCells/total);}return best;}
function greedyLinks(ctx,level,want,cls,r){const pods=level.pods.map(({group,...p})=>p),lv={...level,pods},trapOf=i=>(ctx.minD[pods[i].color]??0)>0,keyOf={};
 // the access key of each pod again (its colour's slice), for the pair score
 const used={};pods.forEach((q,i)=>{const ds=ctx.byColour[q.color],k=used[q.color]||0,sl=ds.slice(k,k+q.count).filter(x=>x<Infinity);used[q.color]=k+q.count;keyOf[i]=sl.reduce((a,b)=>a+b,0)/(sl.length||1)/ctx.maxD;});
 const groups=[];let cur=lv;
 for(let n=0;groups.length*2<want&&n<want;n++){const opts=[];const inG=new Set(groups.flat());
  for(let c=0;c<level.tray.length;c++)for(let c2=c+1;c2<level.tray.length;c2++)for(let row=0;row<Math.min(level.tray[c].length,level.tray[c2].length);row++){const a=level.tray[c][row],b=level.tray[c2][row];if(inG.has(a)||inG.has(b)||pods[a].mystery||pods[b].mystery)continue;
   const trap=trapOf(a)||trapOf(b);opts.push({a,b,trap,sc:Math.abs(keyOf[a]-keyOf[b])+(trap?.5:0)+(row<=2?.2:0)+r()*.3});}
  opts.sort((x,y)=>y.sc-x.sc);const hasTrap=groups.some(([a,b])=>trapOf(a)||trapOf(b));let bestO=null;
  for(const o of opts.slice(0,10)){const name=String.fromCharCode(65+groups.length),ps=cur.pods.map((p,k)=>k===o.a||k===o.b?{...p,group:name}:p),nl={...cur,pods:ps};if(R.validate(nl).length)continue;
   const gr=greedyReach(nl,cls)+(!hasTrap&&!o.trap?.6:0);if(!bestO||gr<bestO.gr)bestO={o,gr,nl};}
  if(!bestO)break;cur=bestO.nl;groups.push([bestO.o.a,bestO.o.b]);}
 return groups.length*2>=want?cur:null;}

// ---- the placer's filters of the slot (levels.mjs place; the same as route-first.mjs placerFilters) ----
function placerFilters(e,level,cls,slot,pic){const k=e.checks||{},why=[];
 if(e.random.cls!==cls)why.push(`класс ${e.random.cls}`);if(!(k.forced<=.35))why.push('вынужденных > 35 %');if(cls!=='Normal'&&!(k.opening&&k.opening.ok))why.push('дебют + цвета');if(k.latch&&!k.latch.ok)why.push('засов');
 const f=fitness(L.overlaid(pic,null,null)),weak=f.hiddenColours<=1||f.visible>=6,types=(level.recipe||'none')==='none'?0:level.recipe.split('+').length;
 if(cls!=='Normal'&&f.hiddenColours<3)why.push('картинка слабая для Hard');if(cls==='Normal'&&weak&&!types)why.push('слабая картинка без допа');
 if(slot==='regular'&&!k.hold)why.push('нет пода, держащего место (§6)');const cf=M.calmFails(e.random.cls,k.calm,slot==='regular');if(cf)why.push(cf);
 if(cls!=='Normal'&&k.traps&&k.traps.links&&!k.traps.traps)why.push('нет связки-ловушки');return why;}

// ---- the builder ----
export async function formulaBuild(picId,cls,recipe,{slot=cls==='Normal'?'regular':'hard',minutes=15,want=1,seed=1,model=loadModel(),log=()=>{}}={}){
 const pic=L.picture(picId),t0=Date.now(),stats={attempts:0,quick:0,corrections:0,mechStep:0,screened:0,evaluated:0,passed:0,accepted:0,reasons:{}},no=k=>{stats.reasons[k]=(stats.reasons[k]||0)+1;};
 const accepted=[],passedOut=[],trace=[];let offset=0,nOff=0,good=null;const [lo,hi,aim]=zoneOf(cls,slot);
 for(let k=0;(Date.now()-t0)<minutes*60000&&accepted.length<want;k++){stats.attempts++;const pl=plan(pic,cls,recipe,{slot,model,attempt:k,offset,fixed:good?{Plev:good.Plev,cols:good.cols}:{}});if(!pl){no('план: нет допустимых доз');break;}
  const ctx=Q.context(pic,{accents:pl.p.accents,seed:seed*31+k});let p={...pl.p,seed:seed*1000003+k*7919,latchClass:cls},q=null,level=null,steps=[];
  const make=()=>{const b=Q.build(ctx,p);if(b.error)return b;let lv=b.level;
   // Hard / Super Hard: the links chosen against the greedy strategies (the route-first mechanic step)
   if(cls!=='Normal'&&p.links>0){const g=greedyLinks(ctx,lv,p.links,cls,Q.rnd(p.seed^0x77));if(g){lv=g;stats.mechStep++;}}
   return {level:lv};};
  // a picture remembers where it landed: once an attempt measured in the zone, the next ones keep its pods and columns
  // and start from its t (± 0.03), since the share is a cliff in t whose place the picture features only roughly know
  if(good){p={...p,t:Math.max(0,Math.min(1,Math.round((good.t+(Q.rnd(p.seed)()-.5)*.06)*100)/100))};}
  // the correction loop (≤ 5 quick measurements): a Newton step on t with the model's slope until both sides of the zone
  // have been seen, then bisection inside that bracket (interpolated on the logit, kept in its middle 20–80 %)
  let tEasy=null,tHard=null,qEasy=null,qHard=null;
  for(let step=0;step<=5;step++){const b=make();if(b.error){no(b.error);level=null;break;}level=b.level;q=await M.quickShare(level,40,11);stats.quick++;
   const etaP=MD.eta(model,rowOf(pl.f,p)),etaO=MD.logit(Math.min(.975,Math.max(.025,q)));steps.push({t:p.t,pred:Math.round(MD.sigmoid(etaP+offset)*100)/100,q});
   offset=(offset*nOff+(etaO-etaP))/(nOff+1);nOff=Math.min(nOff+1,4);
   if(q>=lo&&q<=hi){good={t:p.t,Plev:p.Plev,cols:p.cols};break;}if(step===5)break;
   if(q>hi&&(tEasy===null||p.t>tEasy)){tEasy=p.t;qEasy=q;}if(q<lo&&(tHard===null||p.t<tHard)){tHard=p.t;qHard=q;}
   let nt;if(tEasy!==null&&tHard!==null&&tEasy<tHard){const eA=MD.logit(Math.min(.98,Math.max(.02,qEasy))),eB=MD.logit(Math.min(.98,Math.max(.02,qHard))),f=Math.min(.8,Math.max(.2,(MD.logit(aim)-eA)/(eB-eA)));nt=tEasy+f*(tHard-tEasy);}
   else{const sl=Math.min(-.5,slopeT(model,pl.f,p));let dt=(MD.logit(aim)-etaO)/sl;dt=Math.max(-.35,Math.min(.35,dt));nt=p.t+dt;}
   nt=Math.max(0,Math.min(1,Math.round(nt*100)/100));if(nt===p.t||steps.some(s=>s.t===nt))break;p={...p,t:nt};stats.corrections++;}
  if(!level)continue;trace.push({k,plan:{t:pl.p.t,P:pl.p.P,cols:pl.p.cols,sigma:pl.p.sigma,links:pl.p.links,hidden:pl.p.hidden,pred:Math.round(pl.pred*100)/100,pin:Math.round(pl.pin*100)/100},steps});
  log(`#${k} план t ${pl.p.t} P ${pl.p.P} кол ${pl.p.cols} σ ${pl.p.sigma} связ ${pl.p.links} скр ${pl.p.hidden} → прогноз ${Math.round(pl.pred*100)} % (в зоне ${Math.round(pl.pin*100)} %); замеры ${steps.map(s=>`t ${s.t}: ${Math.round(s.q*100)} %`).join(', ')}`);
  if(q<lo-.15||q>hi+.15){no('быстрая оценка вне зоны после поправок');continue;}
  // cheap pre-screens of what evaluate refuses most (§5)
  {stats.screened++;const f=await M.forcedShare(level);if(f>.35){no('отсев: вынужденных > 35 %');continue;}
   if(cls!=='Normal'){const g=await M.greedy(level);if(!M.greedyBeaten(g,cls)){no('отсев: жадная выигрывает');continue;}
    const c40=(await M.calmTest(level,{first:40,max:40})).share;if(c40>(cls==='Hard'?.5:.25)+.15){no('отсев: спокойная выше порога');continue;}
    const o=await M.openingTest(level,cls);if(!o.ok){no('отсев: дебют + цвета');continue;}}}
  stats.evaluated++;const e=await LV.evaluate(level,{m:ctx.m,cls,intro:false,fixed:null,tick:()=>{}});
  if(e.reason){no('evaluate: '+e.reason);log(`   evaluate: ${e.reason}`);continue;}
  const why=placerFilters(e,level,cls,slot==='hard'?'hard':slot,pic);if(why.length){no('расстановка: '+why[0]);log(`   отказ расстановки: ${why.join('; ')}`);continue;}
  if(ctx.accents)e.checks.accents=await LV.accentCheck(level,pic,ctx.accents,e.checks.pressure);
  const at=(Date.now()-t0)/1000,zone=e.random.share>=lo&&e.random.share<=hi;stats.passed++;
  const x={id:`${picId}-formula-${cls[0]}${recipe}-${k}`,at,attempt:k,params:p,plan:{pred:pl.pred,pin:pl.pin},steps,level,m:ctx.m,e};
  if(!zone){no('вне зоны позиции (проходит фильтры)');log(`   проходит, но вне зоны: ${e.random.share}`);passedOut.push(x);continue;}
  stats.accepted++;log(`   ПРИНЯТ: ${e.random.share} за ${at.toFixed(0)} с (попытка ${k+1})`);accepted.push(x);}
 return {accepted,passedOut,stats,trace,seconds:(Date.now()-t0)/1000};}

// the placed-level shape (levels.mjs place) for the pilot's --preview (as route-first.mjs asPlaced)
export function asPlaced(x,n,cls,picId){const {level:lv,e,m}=x;
 return {level:n,cls,intro:null,picture:picId,candidate:x.id,recipe:lv.recipe,N:m.N,W:m.N,colours:m.colours,P:lv.pods.length,columns:lv.tray.length,linked:lv.pods.filter(p=>p.group).length,hidden:lv.pods.filter(p=>p.mystery).length,
  ...(lv.hiddenCells?{cells:M.cellsHiddenAtStart(lv)}:{}),...(lv.latches?{latches:lv.latches.length}:{}),random:e.random,solution:e.solution,fairness:e.fairness,strategies:e.strategies,rhythm:e.rhythm,dosePos:null,checks:e.checks,
  engine:'tools/rounds/engine.mjs (calibration formula builder)',params:x.params,definition:{...lv,id:n,difficulty:cls}};}

// ---- validation jobs: the prototype comparison's three and five more placed non-bridge levels ----
export const JOBS=[
 {label:'L0020',pic:'P0078',cls:'Hard',recipe:'links+hidden',slot:'hard'},
 {label:'L0025',pic:'P0166',cls:'Hard',recipe:'links+hidden',slot:'hard'},
 {label:'L0027',pic:'P0060',cls:'Normal',recipe:'links+hidden',slot:'regular'},
 {label:'L0040',pic:'P0165',cls:'Hard',recipe:'links+hidden',slot:'hard'},
 {label:'L0022',pic:'P0015',cls:'Normal',recipe:'none',slot:'regular'},
 {label:'L0032',pic:'P0012',cls:'Normal',recipe:'links+hidden',slot:'regular'},
 {label:'L0044',pic:'P0164',cls:'Normal',recipe:'links+hidden',slot:'regular'},
 {label:'L0036',pic:'P0079',cls:'Normal',recipe:'none',slot:'regular'},
];
const OUT=path.join(W,'validation');
const cmd=ARGV[0];
if(cmd==='plan'){const pl=plan(L.picture(ARGV[1]),ARGV[2],ARGV[3]||'none',{slot:opt('--slot','regular')});console.log(JSON.stringify({params:pl.p,predicted:pl.pred,inZone:pl.pin,zone:pl.zone},null,1));}
else if(cmd==='build'||cmd==='validate'){const jobs=cmd==='build'?[{label:opt('--label',ARGV[1]),pic:ARGV[1],cls:opt('--cls','Normal'),recipe:opt('--recipe','none'),slot:opt('--slot',null)}]:JOBS.filter(j=>ARGV.length<2||ARGV.slice(1).includes(j.label));
 fs.mkdirSync(OUT,{recursive:true});
 for(const j of jobs){console.log('==',j.label,j.pic,j.cls,j.recipe);const res=await formulaBuild(j.pic,j.cls,j.recipe,{slot:j.slot||(j.cls==='Normal'?'regular':'hard'),minutes:+opt('--minutes',process.env.CALIB_MINUTES||15),want:+opt('--want',process.env.CALIB_WANT||1),log:s=>console.log(j.label,s)});
  fs.writeFileSync(path.join(OUT,j.label+'.json'),JSON.stringify({job:j,seconds:res.seconds,stats:res.stats,trace:res.trace,accepted:res.accepted.map(x=>({...asPlaced(x,0,j.cls,j.pic),at:x.at,steps:x.steps,plan:x.plan})),passedOut:res.passedOut.map(x=>({...asPlaced(x,0,j.cls,j.pic),at:x.at,steps:x.steps}))},null,1));
  console.log(j.label,'итог',Math.round(res.seconds),'с',JSON.stringify(res.stats));}}
// today's builder on the same job (levels.mjs build on the private copy, a fresh search tagged 'calib', as
// tools/rounds/proto/compare.mjs old does), its candidates passed through the same placer filters and zone
else if(cmd==='today'){const {spawnSync}=await import('node:child_process');fs.mkdirSync(OUT,{recursive:true});
 for(const j of JOBS.filter(j=>ARGV.slice(1).includes(j.label))){const bf=path.join(process.env.ROUNDS_DIR,'bank',j.pic+'.json'),before=new Set((L.readJSON(bf,{candidates:[]}).candidates||[]).map(c=>c.id)),t0=Date.now();
  const r=spawnSync(process.execPath,[path.join(ROOT,'tools/rounds/levels.mjs'),'build',j.pic,'--classes',j.cls,'--recipes',j.recipe,'--no-intro'],{env:{...process.env,ROUNDS_FRESH:'calib'},encoding:'utf8',maxBuffer:1<<28});
  const seconds=(Date.now()-t0)/1000,bank=L.readJSON(bf,{candidates:[]}),fresh=bank.candidates.filter(c=>!before.has(c.id)),pic=L.picture(j.pic),[lo,hi]=zoneOf(j.cls,j.slot);
  const rows=fresh.map(c=>({id:c.id,share:c.random.share,why:placerFilters({random:c.random,checks:c.checks},c.level,j.cls,j.slot,pic),zone:c.random.share>=lo&&c.random.share<=hi}));
  const searched=(bank.searched||[]).filter(s=>s.tag==='calib').at(-1)||null;
  fs.writeFileSync(path.join(OUT,j.label+'-today.json'),JSON.stringify({job:j,seconds,searched,candidates:rows,passed:rows.filter(x=>!x.why.length).length,accepted:rows.filter(x=>!x.why.length&&x.zone).length,stdout:r.stdout},null,1));
  console.log(j.label,'сегодняшний сборщик',Math.round(seconds),'с',(r.stdout||'').trim().split('\n').at(-1),'в зоне и проходят',rows.filter(x=>!x.why.length&&x.zone).length);}}
// the generator of 2026-11-02 (tune.mjs tuneJob: model plan, ≤ 5 quick tests, forced-aware links, information overlays
// after the class) on the same jobs, want 2 in 15 minutes each; a Normal picture of up to 550 cells is a breather (17 §5)
//   ROUNDS_DIR=content/work/gen2/rounds-copy node tools/rounds/calib/plan.mjs gen2 [label…]   → content/work/gen2/validation/<label>.json
//   node tools/rounds/calib/plan.mjs gen2-report                                               → content/work/gen2/validation.md
else if(cmd==='gen2'){const T=await import('../tune.mjs'),G=path.join(ROOT,'content/work/gen2/validation');fs.mkdirSync(G,{recursive:true});
 for(const j of JOBS.filter(j=>ARGV.length<2||ARGV.slice(1).includes(j.label))){const pic=L.picture(j.pic),N=L.measure(pic).N,slot=j.cls==='Normal'?(N<=550?'breather':'regular'):j.cls;console.log('==',j.label,j.pic,j.cls,j.recipe,slot);
  const res=await T.tuneJob({pic,cls:j.cls,recipe:j.recipe,slot,want:+opt('--want',2),minutes:+opt('--minutes',process.env.CALIB_MINUTES||15),memory:{},log:s=>console.log(j.label,s)});
  const one=x=>({...asPlaced({level:x.level,e:x.e,m:x.m,id:`${j.pic}-gen2-${x.k}`,params:{t:x.t,cols:x.cols,pos:x.pos,links:x.links}},0,j.cls,j.pic),at:x.seconds,attempt:x.k,steps:x.steps});
  fs.writeFileSync(path.join(G,j.label+'.json'),JSON.stringify({job:{...j,slot},seconds:res.seconds,stats:res.stats,accepted:res.accepted.map(one),banked:res.banked.filter(x=>!res.accepted.includes(x)).map(one)},null,1));
  console.log(j.label,'итог',Math.round(res.seconds),'с',JSON.stringify(res.stats));}}
else if(cmd==='gen2-report'){const G=path.join(ROOT,'content/work/gen2/validation'),pct=x=>x==null?'—':Math.round(x*100)+' %',rows=[];
 rows.push('| Job | Today | Route-first | Formula (calib) | **Generator 2026-11-02** | Checks of the first accepted level (generator) |','|---|---|---|---|---|---|');
 const cal=L.readJSON(path.join(W,'validation.json'),[]);
 for(const j of JOBS){const g=L.readJSON(path.join(G,j.label+'.json')),c=cal.find(x=>x.job.label===j.label)||{};
  const old=c.old?`${c.old.accepted||0} of ${c.old.tried??'?'} in ${Math.round(c.old.seconds)} s`:'—',pro=c.proto?`${c.proto.accepted} in ${Math.round(c.proto.seconds)} s${c.proto.first?`, first ${Math.round(c.proto.first)} s`:''}`:'—',fo=c.formula?`${c.formula.accepted} in ${Math.round(c.formula.seconds)} s${c.formula.first?`, first ${Math.round(c.formula.first)} s (attempt ${c.formula.attemptsToFirst})`:''}`:'—';
  const a=g&&g.accepted[0],k=a&&a.checks||{},st=a&&a.strategies||{};
  const gen=g?`${g.accepted.length} in ${Math.round(g.seconds)} s${g.accepted.length?` → ${Math.round(g.seconds/g.accepted.length)} s/level; first ${Math.round(a.at)} s (attempt ${a.attempt+1}); ${g.stats.attempts} attempts, ${g.stats.quick} quick tests, ${g.stats.evaluated} full`:`; ${g.stats.attempts} attempts, ${g.stats.evaluated} full`}`:'not run';
  const chk=a?`${a.cls}${g.job.slot==='breather'?' (breather)':''} ${pct(a.random.share)} (${pct(a.random.wilson95[0])}–${pct(a.random.wilson95[1])}, ${a.random.runs}); calm ${pct(k.calm&&k.calm.share)}; forced ${pct(k.forced)}; opening ${k.opening?`${k.opening.first.wins}/${k.opening.first.openings}, ${pct(k.opening.three.share)}`:'—'}; ${Object.keys(st).length?(!st['most-now|left']&&!st['most-now|right']?'greedy loses':'greedy wins'):'—'}; pressure ${k.pressure?k.pressure.stuck:'—'}; ${a.P} pods / ${a.columns} cols; links ${a.linked} (traps ${k.traps?k.traps.traps:'—'}/${k.traps?k.traps.links:'—'}); hidden ${a.hidden||0}${a.cells?`, cells ${a.cells}`:''}${k.accents?`; accents ${k.accents.cells}`:''}`:(g?Object.entries(g.stats.reasons).sort((x,y)=>y[1]-x[1]).slice(0,3).map(([r,n])=>`${r} ×${n}`).join('; '):'');
  rows.push(`| ${j.label} ${j.pic} ${j.cls} ${j.recipe} | ${old} | ${pro} | ${fo} | ${gen} | ${chk} |`);}
 fs.writeFileSync(path.join(ROOT,'content/work/gen2/validation.md'),rows.join('\n')+'\n');console.log(rows.join('\n'));}
else if(cmd==='report'){const lines=[],prev=path.join(W,'preview');fs.mkdirSync(prev,{recursive:true});let n=1101;const levelsUsed=[];
 const pct=x=>x==null?'—':Math.round(x*100)+' %';
 const row=(src,X,time)=>{if(!X)return `| ${src} | нет уровня | | | | | | | | | | ${time} |`;const k=X.checks||{},st=X.strategies||{},won=Object.values(st).filter(Boolean).length;
  return `| ${src} | ${X.cls} | ${pct(X.random.share)} (${pct(X.random.wilson95[0])}–${pct(X.random.wilson95[1])}, ${X.random.runs}) | ${pct(k.calm&&k.calm.share)} | ${pct(k.forced)} | ${k.opening?`${k.opening.first.wins}/${k.opening.first.openings}, ${Math.round(k.opening.three.share*100)} %`:'—'} | ${won}/${Object.keys(st).length}${!st['most-now|left']&&!st['most-now|right']?', жадная проигр.':''} | ${k.pressure?k.pressure.stuck:'—'} | ${X.P}/${X.columns} | ${X.linked} (${k.traps?`${k.traps.traps}/${k.traps.links}`:'—'}) | ${X.hidden||0}${k.accents?`, акц. ${k.accents.cells}`:''} | ${time} |`;};
 lines.push('| Уровень · сборка | Класс | Случайный (Wilson, партий) | Спокойно | Вынужд. | Дебют (1-й / 3 запуска) | Стратегии выигр. | Стоят без работы | Поды/кол. | Связки (ловушки) | Скрытые | Попыток и время на принятый уровень |','|---|---|---|---|---|---|---|---|---|---|---|---|');
 const summary=[];
 for(const j of JOBS){const placed=L.readJSON(path.join(ROOT,'content/rounds/levels',j.label+'.json')),me=L.readJSON(path.join(OUT,j.label+'.json')),old=L.readJSON(path.join(ROOT,'content/work/proto-route/out',j.label+'-old.json'))||L.readJSON(path.join(OUT,j.label+'-today.json')),pro=L.readJSON(path.join(ROOT,'content/work/proto-route/out',j.label+'-proto.json'));
  const oldT=old?`сегодняшний сборщик: ${old.searched?old.searched.tried:'?'} канд. за ${Math.round(old.seconds)} с, в зоне и проходят ${old.accepted}${old.accepted?`, ${Math.round(old.seconds/old.accepted)} с/ур.`:''}`:'сегодняшний сборщик: замера нет';
  lines.push(row(`${j.label} ${j.pic} · размещён`,placed,oldT));
  if(pro){const b=pro.accepted[0];lines.push(row(`${j.label} · route-first`,b&&{...b,cls:b.cls},`${pro.stats.bases} основ, ${pro.stats.evaluated} полных, принято ${pro.accepted.length} за ${Math.round(pro.seconds)} с${b?`, первый ${Math.round(b.at)} с`:''}`));}
  const b=me&&me.accepted[0];const kOf=x=>+x.candidate.split('-').pop()+1,tm=me?`${me.stats.attempts} попыток, ${me.stats.evaluated} полных; принято ${me.accepted.length} за ${Math.round(me.seconds)} с${me.accepted.length?` → ${Math.round(me.seconds/me.accepted.length)} с/ур.; первый на попытке ${kOf(b)} за ${Math.round(b.at)} с`:''}`:'не запускался';
  lines.push(row(`${j.label} · формула`,b,tm));
  summary.push({job:j,formula:me?{seconds:me.seconds,stats:me.stats,first:b?b.at:null,attemptsToFirst:b?+b.candidate.split('-').pop()+1:null,perAccepted:me.accepted.length?me.seconds/me.accepted.length:null,accepted:me.accepted.length,share:b?b.random.share:null}:null,old:old?{seconds:old.seconds,tried:old.searched&&old.searched.tried,accepted:old.accepted}:null,proto:pro?{seconds:pro.seconds,first:pro.accepted[0]?pro.accepted[0].at:null,accepted:pro.accepted.length}:null});
  const put=(X,tag)=>{if(!X)return;fs.writeFileSync(path.join(prev,`L${n}.json`),JSON.stringify({...X,level:n,candidate:(X.candidate||'')+'-'+tag,definition:{...X.definition,id:n,title:`${X.definition.title} · ${j.label} ${tag}`}}));levelsUsed.push(n);};
  put(placed,'сегодня');n++;put(b,'формула');n++;}
 fs.writeFileSync(path.join(W,'validation.md'),lines.join('\n')+'\n');fs.writeFileSync(path.join(W,'validation.json'),JSON.stringify(summary,null,1));console.log(lines.join('\n'));console.log('preview levels',levelsUsed.join(','));}
else console.log(fs.readFileSync(new URL(import.meta.url),'utf8').split('\n').filter(l=>l.startsWith('//')).join('\n'));

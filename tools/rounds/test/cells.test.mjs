// node --test tools/rounds/test/ — hidden cells (17 §8 «Скрытые клетки», the owner, 2026-11-01): the engine's reveal
// (order, events, the timeline's times, the view, a restored checkpoint, validation), identity for levels without hidden
// cells and for the targeting of levels with them, the builder's doses and choice of cells, the audit's rules. Writes only
// to temporary copies of content/rounds.
import test from 'node:test';import assert from 'node:assert/strict';
import fs from 'node:fs';import os from 'node:os';import path from 'node:path';import {spawnSync} from 'node:child_process';import {fileURLToPath} from 'node:url';

const HERE=path.dirname(fileURLToPath(import.meta.url)),TOOLS=path.dirname(HERE),ROOT=path.resolve(TOOLS,'../..');
const L=await import('../lib.mjs');
const {engine}=await import('../engine.mjs');const E=await engine(),{R,T,idle}=E;
const M=await import('../measure.mjs');
process.argv.splice(2,0,'noop');const LV=await import('../levels.mjs');process.argv.splice(2,1);
const C=await import('../conformance.mjs');
const readJSON=f=>JSON.parse(fs.readFileSync(f,'utf8'));
const placed=n=>readJSON(path.join(ROOT,'content/rounds/levels',`L${String(n).padStart(4,'0')}.json`));

// a small level: a far part (A) behind a full-width strip with one site, the near bank (G); hidden cells on both sides
const pic={id:'T',title:'test',rows:['AAAAAAA','AAAAAAA','##b####','GGGGGGG','GGGGGGG','GGGGGGG'],w:7,h:6};
const board=L.boardOf(pic);
const idx=(x,y)=>(board.h-1-(y+2))*board.w+x+2;// picture (x, y), top first → bottom-first board index
const far=[idx(2,1),idx(3,1),idx(2,0)],deep=idx(3,4),front=idx(0,3);
const level=(hiddenCells=[...far,deep,front].sort((a,b)=>a-b))=>({id:1,title:'t',w:board.w,h:board.h,rowsBottomFirst:board.rowsBottomFirst,entries:board.entries,capacity:5,
 pods:[{color:'G',count:21},{color:'A',count:14}],tray:[[0],[1]],hero:board.hero,...(hiddenCells?{hiddenCells}:{})});
// every state: no target is still hidden, every hidden cell left is a colour cell that is not a target
function invariant(s){const t=new Set(R.reach(s).targets);if(!s.veiled)return;
 for(let i=0;i<s.board.length;i++){if(s.veiled[i]){assert.ok(s.board[i]>=0,`veiled ${i} is a colour cell`);assert.ok(!t.has(i),`veiled ${i} is no target`);}}
 for(const i of t)assert.equal(s.veiled[i],0,`target ${i} revealed`);}

test('create reveals the start front silently; deep and far cells stay hidden; the view shows them as HIDDEN',()=>{
 const lv=level();assert.deepEqual(R.validate(lv),[]);const s=R.create(lv);
 assert.equal(s.veiled[front],0);assert.equal(s.veiled[deep],1);for(const c of far)assert.equal(s.veiled[c],1);assert.equal(s.veiledLeft,4);
 const v=R.view(s);assert.equal(v.board[deep],R.HIDDEN);assert.equal(v.board[front],s.board[front]);assert.equal(R.known(s,deep),false);invariant(s);
 assert.equal(M.cellsHiddenAtStart(lv),4);});

test('rules step: each reveal follows the round of clears that opened it, in increasing cell order, before completes',()=>{
 let s=R.create(level());const all=[];
 for(const a of [{type:'tap',pod:0},{type:'bridge',choice:0},{type:'tap',pod:1}]){const r=R.step(s,a,true);assert.ok(r.accepted,JSON.stringify(a));s=r.state;invariant(s);all.push(...r.events);
  // within the action: cellReveal events come in increasing order inside each run, and right after a clear round or the bridge
  for(let i=0;i<r.events.length;i++){const e=r.events[i];if(e.type!=='cellReveal')continue;const prev=r.events[i-1];
   assert.ok(prev.type==='clear'||prev.type==='bridge'||(prev.type==='cellReveal'&&prev.cell<e.cell),`event before ${JSON.stringify(e)}: ${JSON.stringify(prev)}`);
   const later=r.events.slice(i+1).find(x=>x.type!=='cellReveal');if(later&&prev.type==='clear')assert.ok(later.type==='clear'?later.round>prev.round:true);}}
 const reveals=all.filter(e=>e.type==='cellReveal').map(e=>e.cell);
 assert.deepEqual([...reveals].sort((a,b)=>a-b),[deep,...far].sort((a,b)=>a-b),'every hidden cell revealed once');
 // the bridge reveals the far cell next to its site, right after the bridge event
 const b=all.findIndex(e=>e.type==='bridge');assert.deepEqual(all[b+1],{type:'cellReveal',cell:idx(2,1)});
 assert.equal(s.status,'Won');assert.equal(s.veiledLeft,0);});

test('timeline: a cellReveal follows its clear at the same model time; the bridge reveal carries the commit time; checkpoints recompute',()=>{
 const t=new T.Timeline(R.create(level()));let seen=0;
 const check=()=>{const ev=t.lastEvents;for(let i=0;i<ev.length;i++)if(ev[i].type==='cellReveal'){seen++;let k=i-1;while(ev[k].type==='cellReveal'){assert.ok(ev[k].cell<ev[i].cell);k--;}
   assert.equal(ev[k].type,'clear');assert.equal(ev[i].at,ev[k].at);}invariant(t.state);};
 t.commit({type:'tap',pod:0});check();while(t.pending){t.advance(100);check();
  if(t.pending){const c=new T.Timeline(R.create(level()),t.snapshot());assert.deepEqual(Array.from(c.state.veiled),Array.from(t.state.veiled));assert.equal(c.state.veiledLeft,t.state.veiledLeft);}}
 const r=t.commit({type:'bridge',choice:0});assert.ok(r.accepted);
 assert.deepEqual(r.events.slice(-1),[{type:'cellReveal',at:t.now,cell:idx(2,1)}]);
 t.commit({type:'tap',pod:1});while(t.pending){t.advance(100);check();}
 assert.equal(t.state.status,'Won');assert.equal(seen,3);// deep + two far cells by clears, the third far cell by the bridge
 // the conformance log writes them as `t cellReveal cell=<c>`
 assert.equal(C.lineOf({type:'cellReveal',at:2.5,cell:7},null),'2500 cellReveal cell=7');assert.equal(C.lineOf({type:'cellReveal',cell:7},900),'900 cellReveal cell=7');});

test('validate: sorted unique colour cells, never with hidden pods',()=>{
 assert.ok(R.validate(level([Math.max(deep,front),Math.min(deep,front)])).includes('hidden cells'),'unsorted');
 assert.ok(R.validate(level([deep,deep])).includes('hidden cells'),'duplicate');
 assert.ok(R.validate(level([])).includes('hidden cells'),'empty');
 assert.ok(R.validate(level([0])).includes('hidden cell not a colour cell'),'a wall');
 const lv=level([deep]);lv.pods[1].mystery='depth';assert.ok(R.validate(lv).includes('hidden cells with hidden pods'));});

test('identity: levels without hidden cells play unchanged; hidden cells change no event but cellReveal and no signature',async()=>{
 for(const n of [5,16,21]){const L0=placed(n),d=L0.definition;assert.equal(d.hiddenCells,undefined);
  const s0=R.create(d);assert.equal(s0.veiled,null);assert.equal(R.observation(s0),JSON.stringify(R.view(s0)));
  // the same level with hidden cells picked by the builder: the same events once cellReveal is left out, the same signatures
  const hc=LV.pickHiddenCells(d,12,(()=>{let k=0;return()=>((k=k*69069+1)>>>0)/4294967296;})());assert.ok(hc&&hc.length===12,`L${n} cells`);
  const play=lv=>{const t=new T.Timeline(R.create(lv)),log=[];for(const a of L0.solution.route){while(t.pending){t.advance(1e9);log.push(...t.lastEvents);}const r=t.commit(a);log.push(...r.events,...t.lastEvents,R.signature(t.state));}
   while(t.pending){t.advance(1e9);log.push(...t.lastEvents);}log.push(R.signature(t.state),t.state.status);return log;};
  const a=play(d),b=play({...d,hiddenCells:hc});
  assert.ok(b.some(e=>e.type==='cellReveal'),`L${n}: cells revealed`);
  assert.deepEqual(b.filter(e=>e.type!=='cellReveal'),a,`L${n}: targeting unchanged`);
  // the patient search model too
  let x=R.create(d),y=R.create({...d,hiddenCells:hc});for(const act of L0.solution.route){x=idle.step(x,act).state;y=idle.step(y,act).state;assert.equal(R.signature(y),R.signature(x));invariant(y);}}});

test('builder doses: shares of N after the start reveal by class and role, 5–10 on an introduction, never with hidden pods',()=>{
 assert.deepEqual(LV.doses('cells','Normal',24,false,600).cells,[30,60]);
 assert.deepEqual(LV.doses('cells','Hard',24,false,600).cells,[60,120]);
 assert.deepEqual(LV.doses('cells','Super Hard',24,false,600).cells,[90,180]);
 assert.deepEqual(LV.doses('links+cells','Hard',24,false,600).cells,[30,30],'additional: its minimum, 5 % of N');
 assert.deepEqual(LV.doses('bridge+cells','Normal',24,false,600).cells,[30,60],'after a bridge the lead keeps its range');
 assert.deepEqual(LV.doses('cells','Normal',24,true,600).cells,[5,10]);
 assert.deepEqual(LV.doses('cells','Normal',12,false,600).cells,[30,60],'a small queue: the lead table');
 assert.match(LV.doses('links+hidden+cells','Hard',24,false,600).error,/не бывают на одном уровне/);
 assert.equal(L.recipeAllowed('hidden+cells'),false);assert.equal(L.recipeAllowed('links+cells'),true);
 assert.ok(L.SUPPORTED.has('cells'));assert.deepEqual(L.INTRO.find(([m])=>m==='cells'),['cells',51,53]);assert.deepEqual(L.INTRO.find(([m])=>m==='latch'),['latch',61,63]);
 assert.equal(L.slotOf(51).intro,'cells');assert.equal(L.slotOf(52).allowed.includes('cells'),false);assert.equal(L.slotOf(54).allowed.includes('cells'),true);});

test('builder candidates: the hidden cells lie off the start front, the dose holds, an introduction has 5–10',()=>{
 const p=L.picture('P0024'),m=L.measure(p),b=L.boardOf(p),base=LV.splitPods(m.volumes,m.N,null);assert.ok(!base.error);
 for(const [cls,intro,pos,recipe] of [['Normal',true,null,'cells'],['Normal',false,.5,'cells'],['Hard',false,.8,'cells'],['Normal',false,.5,'links+cells']]){
  let c=null;for(let seed=1;seed<20&&!(c&&c.level);seed++)c=LV.candidate(p,b,base.pods,3,recipe,cls,seed,intro,pos);assert.ok(c.level,`${cls} ${recipe}: ${c.error}`);
  const lv=c.level,hc=lv.hiddenCells,k=M.cellsHiddenAtStart(lv);assert.deepEqual(R.validate(lv),[]);
  assert.equal(k,hc.length,'none revealed at the start');assert.deepEqual(hc,[...hc].sort((x,y)=>x-y));
  const d=LV.doses(recipe,cls,lv.pods.length,intro,m.N).cells;assert.ok(k>=d[0]&&k<=d[1],`${cls} ${recipe} ${k} in ${d}`);
  if(intro)assert.ok(k>=5&&k<=10);
  if(!intro)assert.equal(LV.dosesOk({...lv,recipe},cls),null);}
 // with hidden pods: refused
 assert.match(LV.candidate(p,b,base.pods,3,'cells+hidden','Normal',1,false,.5).error||'',/не бывают на одном уровне/);});

// the audit on a sandbox copy: a level given hidden cells next to its hidden pods, and a regular Normal given too few
function sandbox(){const dir=fs.mkdtempSync(path.join(os.tmpdir(),'rounds-cells-'));
 for(const p of ['levels','pictures'])fs.cpSync(path.join(ROOT,'content/rounds',p),path.join(dir,p),{recursive:true});
 fs.copyFileSync(path.join(ROOT,'content/rounds/quality.json'),path.join(dir,'quality.json'));return dir;}
const audit=(dir,a,b)=>{const j=path.join(dir,'audit.json');spawnSync(process.execPath,[path.join(TOOLS,'audit.mjs'),String(a),String(b),'--json',j],{cwd:ROOT,env:{...process.env,ROUNDS_DIR:dir},encoding:'utf8',maxBuffer:1<<26});return readJSON(j);};
test('audit: hidden cells with hidden pods, the recipe, the dose after the start reveal',()=>{const dir=sandbox();
 const f=n=>path.join(dir,'levels',`L${String(n).padStart(4,'0')}.json`);
 // L21 (links + hidden pods): two deep cells added
 const a=readJSON(f(21)),hc=LV.pickHiddenCells(a.definition,2,()=>.37);a.definition.hiddenCells=hc;fs.writeFileSync(f(21),JSON.stringify(a));
 // L22 (a regular Normal without extras): recipe cells with only three hidden cells
 const b=readJSON(f(22));b.recipe='cells';b.definition.recipe='cells';b.definition.hiddenCells=LV.pickHiddenCells(b.definition,3,()=>.61);fs.writeFileSync(f(22),JSON.stringify(b));
 const r=audit(dir,21,22),bad=n=>r.levels.find(x=>x.level===n).bad.join(' | ');
 assert.match(bad(21),/скрытые клетки вместе со скрытыми подами/);assert.match(bad(21),/рецепт и скрытые клетки не совпадают/);
 assert.match(bad(22),/скрытых клеток после стартового раскрытия 3 вне \d+–\d+ для Normal \(ведущая/);
 assert.match(bad(22),/механика cells не разрешена календарём на L22/);});

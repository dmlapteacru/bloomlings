// node --test tools/rounds/test/ — the latch with its acorn key (17 §8 «Засов и жёлудь-ключ», §9 as amended on 2026-11-01;
// README «Latch and key»): the engine's blocking and opening (order, events, the timeline's times, a restored checkpoint,
// validation), identity for levels without latches, the builder (doses, key holes, an introduction), the §8 checks on the
// saved solution and the audit, the export and conformance lines; and the picture selection of 2026-11-01 (pins, packs,
// the owner's rights). Writes only to temporary copies of content/rounds.
import test from 'node:test';import assert from 'node:assert/strict';
import fs from 'node:fs';import os from 'node:os';import path from 'node:path';import {spawnSync} from 'node:child_process';import {fileURLToPath} from 'node:url';

const HERE=path.dirname(fileURLToPath(import.meta.url)),TOOLS=path.dirname(HERE),ROOT=path.resolve(TOOLS,'../..');
const L=await import('../lib.mjs');
const {engine}=await import('../engine.mjs');const E=await engine(),{R,T,idle}=E;
const M=await import('../measure.mjs');
process.argv.splice(2,0,'noop');const LV=await import('../levels.mjs');process.argv.splice(2,1);
const C=await import('../conformance.mjs');const X=await import('../export.mjs');const {validate}=await import('../schema.mjs');
const readJSON=f=>JSON.parse(fs.readFileSync(f,'utf8'));
const placed=n=>readJSON(path.join(ROOT,'content/rounds/levels',`L${String(n).padStart(4,'0')}.json`));

// a small level: G (20 cells) over A (14), one hole in the G area at picture (1, 1); the latch in column 2 after one pod
function toy({rows=['GGGGGGG','G.GGGGG','GGGGGGG','AAAAAAA','AAAAAAA'],pods=[{color:'A',count:7},{color:'A',count:7},{color:'G',count:10},{color:'G',count:10}],tray=[[0],[1],[2,3]],latches}={}){
 const b=L.boardOf({id:'T',title:'t',rows}),at=(x,y)=>(b.h-1-(y+2))*b.w+x+2;
 return {at,b,level:{id:1,title:'t',w:b.w,h:b.h,rowsBottomFirst:b.rowsBottomFirst,entries:b.entries,capacity:5,pods,tray,...(latches?{latches:latches(at,b)}:{latches:[{column:2,depth:1,key:at(1,1)}]})}};}
// every state: no pod behind a closed latch is top; latched follows from heads and the board
function invariant(s){if(!s.latched)return;const d=R.reach(s).distance;
 s.meta.latches.forEach((l,i)=>{const open=s.heads[l.column]>l.depth||(s.heads[l.column]===l.depth&&d[l.key]>0);assert.equal(s.latched[i],open?0:1,`latch ${i} state`);});
 for(const p of R.topPods(s)){const c=s.level.tray.findIndex(st=>st.includes(p));assert.ok(s.level.tray[c].indexOf(p)<R.blockAt(s,c),`top ${p} not behind a latch`);}}

test('blocking: the pods behind a closed latch are not top, cannot be tapped, a partner behind it is not exposed; view shows latched',()=>{
 const {level}=toy();assert.deepEqual(R.validate(level),[]);let s=R.create(level);
 assert.deepEqual(Array.from(s.latched),[1]);assert.deepEqual(R.topPods(s),[0,1,2]);assert.equal(R.blockAt(s,2),1);assert.deepEqual(R.blockedColumns(s),[]);
 s=R.step(s,{type:'tap',pod:0}).state;s=R.step(s,{type:'tap',pod:1}).state;invariant(s);
 // a pair whose second member stands behind the latch waits for it
 const lv={...level,pods:[{color:'A',count:7},{color:'A',count:7,group:'P'},{color:'G',count:10},{color:'G',count:10,group:'P'}],tray:[[0,1],[2,3]],latches:[{column:1,depth:1,key:level.latches[0].key}]};
 assert.deepEqual(R.validate(lv),[]);let u=R.create(lv);u=R.step(u,{type:'tap',pod:0}).state;
 assert.equal(R.canTap(u,1),'partner-not-exposed','the partner stands behind the latch');
 assert.deepEqual(R.view(R.create(level)).latched,[1]);assert.equal(R.view(R.create(placed(5).definition)).latched,undefined);});

test('rules step: latchOpen after the round of clears that reached the key, then the depth reveal it exposes, before completes; after the commits when the key is already reached',()=>{
 const {level}=toy({pods:[{color:'A',count:7},{color:'A',count:7},{color:'G',count:10},{color:'G',count:10,mystery:'depth'}]});
 let s=R.create(level);assert.equal(s.revealed[3],0);
 for(const a of [{type:'tap',pod:0},{type:'tap',pod:1}]){s=R.step(s,a).state;invariant(s);}
 const r=R.step(s,{type:'tap',pod:2},true);s=r.state;invariant(s);
 const ev=r.events,k=ev.findIndex(e=>e.type==='latchOpen');assert.ok(k>0,'the latch opened in this action');assert.deepEqual(ev[k],{type:'latchOpen',latch:0});
 assert.ok(['clear','cellReveal'].includes(ev[k-1].type),'right after the clears of its round');
 assert.deepEqual(ev[k+1],{type:'reveal',pod:3},'the exposed hidden pod reveals right after');
 const round=ev.slice(0,k).filter(e=>e.type==='clear').at(-1).round;assert.ok(!ev.slice(0,k).some(e=>e.type==='complete'&&e.round===round),'before that round\'s completes');
 assert.deepEqual(R.topPods(s),[3]);
 // the key reached before the latch comes on top: it opens right after the commit that brings it there
 const c=toy({tray:[[0],[2],[1,3]]}).level;let t=R.create(c);
 for(const a of [{type:'tap',pod:0},{type:'tap',pod:2}])t=R.step(t,a).state;assert.equal(t.latched[0],1);assert.ok(R.reach(t).distance[c.latches[0].key]>0,'key reached');
 const r2=R.step(t,{type:'tap',pod:1},true);assert.deepEqual(r2.events.slice(0,2),[{type:'commit',pod:1,slot:r2.events[0].slot},{type:'latchOpen',latch:0}]);
 // a latch on top at the start with its key reached opens silently at create
 const open=toy({latches:(at,b)=>[{column:2,depth:0,key:b.w+1}]}).level;const o=R.create(open);assert.deepEqual(Array.from(o.latched),[0]);assert.equal(o.latchedLeft,0);});

test('timeline: latchOpen at the time of the clear that reached the key, the depth reveal at that time after it; a commit opens it at the commit time; checkpoints recompute',()=>{
 const {level}=toy({pods:[{color:'A',count:7},{color:'A',count:7},{color:'G',count:10},{color:'G',count:10,mystery:'depth'}]});
 const t=new T.Timeline(R.create(level));for(const a of [{type:'tap',pod:0},{type:'tap',pod:1},{type:'tap',pod:2}])assert.ok(t.commit(a).accepted);
 let seen=0;
 while(t.pending){t.advance(50);const ev=t.lastEvents;invariant(t.state);
  for(let i=0;i<ev.length;i++)if(ev[i].type==='latchOpen'){seen++;let k=i-1;while(ev[k].type==='cellReveal')k--;assert.equal(ev[k].type,'clear');assert.equal(ev[i].at,ev[k].at);
   assert.deepEqual(ev[i+1],{type:'reveal',at:ev[i].at,pod:3,mode:'depth',color:'G'});}
  if(t.pending){const c=new T.Timeline(R.create(level),t.snapshot());assert.deepEqual(Array.from(c.state.latched),Array.from(t.state.latched));assert.equal(R.signature(c.state),R.signature(t.state));}}
 assert.equal(seen,1);
 const c=toy({tray:[[0],[2],[1,3]]}).level,u=new T.Timeline(R.create(c));for(const a of [{type:'tap',pod:0},{type:'tap',pod:2}]){u.commit(a);while(u.pending)u.advance(1e9);}
 const r=u.commit({type:'tap',pod:1});assert.deepEqual(r.events.filter(e=>e.type==='latchOpen'),[{type:'latchOpen',at:u.now,latch:0}]);
 assert.equal(C.lineOf({type:'latchOpen',at:2.5,latch:1},null),'2500 latchOpen latch=1');assert.equal(C.lineOf({type:'latchOpen',latch:0},900),'900 latchOpen latch=0');
 assert.equal(C.lineOf({type:'reveal',at:3.25,pod:4,mode:'depth'},null),'3250 reveal pod=4 mode=depth');assert.equal(C.lineOf({type:'reveal',pod:4,mode:'sealed'},77),'77 reveal pod=4 mode=sealed');});

test('validate: columns, depths, keys, unreachable keys and columns blocked at the start',()=>{
 const {level,at,b}=toy(),v=ls=>R.validate({...level,latches:ls});
 assert.deepEqual(v([]),['latches']);
 assert.ok(v([{column:3,depth:0,key:at(1,1)}]).includes('latch'),'no such column');
 assert.ok(v([{column:2,depth:2,key:at(1,1)}]).includes('latch'),'nothing behind it');
 assert.ok(v([{column:2,depth:1,key:at(1,1)},{column:2,depth:1,key:b.w+1}]).includes('latch'),'one depth twice in a column');
 assert.ok(v([{column:2,depth:1,key:at(0,0)}]).includes('latch key'),'a colour cell');
 assert.ok(v([{column:2,depth:1,key:b.entries[0]}]).includes('latch key'),'an entry');
 assert.ok(v([{column:2,depth:1,key:0}]).includes('latch key'),'a wall');
 assert.ok(v([{column:1,depth:0,key:at(1,1)},{column:2,depth:1,key:at(1,1)}]).includes('latch key'),'one key twice');
 // the key enclosed by G and every G pod behind its own latch: never reachable
 assert.ok(v([{column:2,depth:0,key:at(1,1)}]).includes('latch key unreachable'));
 assert.deepEqual(R.latchKeysBlocked({...level,latches:[{column:2,depth:0,key:at(1,1)}]}),[0]);
 // two columns of three blocked at the start (two holes, two latches at depth 0)
 const two=toy({rows:['GGGGGGG','G.GGG.G','GGGGGGG','AAAAAAA','AAAAAAA'],pods:[{color:'A',count:7},{color:'A',count:7},{color:'G',count:10},{color:'G',count:9}],tray:[[0,2],[1],[3]],latches:at=>[{column:1,depth:0,key:at(1,1)},{column:2,depth:0,key:at(5,1)}]}).level;
 assert.deepEqual(R.validate(two),['latches block too many columns']);
 assert.deepEqual(R.validate({...two,latches:[two.latches[0]]}),[]);});

test('identity: levels without latches have none; a latch open from the start changes no event, state or signature',async()=>{
 for(const n of [5,16,21,30]){const L0=placed(n),d=L0.definition;assert.equal(d.latches,undefined);const s0=R.create(d);assert.equal(s0.latched,null);assert.equal(R.observation(s0),JSON.stringify(R.view(s0)));
  // a latch in front of column 0 whose key is a ring cell reached from the start: it opens silently at create
  const lv={...d,latches:[{column:0,depth:0,key:d.w+1}]};assert.deepEqual(R.validate(lv),[]);assert.equal(R.create(lv).latched[0],0);
  const play=x=>{const t=new T.Timeline(R.create(x)),log=[];for(const a of L0.solution.route){while(t.pending){t.advance(1e9);log.push(...t.lastEvents);}const r=t.commit(a);log.push(...r.events,...t.lastEvents,R.signature(t.state));}
   while(t.pending){t.advance(1e9);log.push(...t.lastEvents);}log.push(R.signature(t.state),t.state.status);return JSON.stringify(log);};
  assert.equal(play(lv),play(d),`L${n}: the same play`);
  let x=R.create(d),y=R.create(lv);for(const a of L0.solution.route){const rx=R.step(x,a,true),ry=R.step(y,a,true);assert.deepEqual(ry.events,rx.events);x=idle.step(x,a).state;y=idle.step(y,a).state;assert.equal(R.signature(y),R.signature(x));}
  for(let k=0;k<4;k++)assert.equal(M.randomGame(E,lv,k*7919+1),M.randomGame(E,d,k*7919+1));}});

test('doses, calendar and the builder: one latch on an introduction, latches in key holes at the class depth',()=>{
 assert.deepEqual(LV.doses('latch','Normal',24,false).latch,[1,1]);assert.deepEqual(LV.doses('latch','Hard',24,false).latch,[1,2]);assert.deepEqual(LV.doses('latch','Super Hard',24,false).latch,[2,3]);
 assert.deepEqual(LV.doses('links+latch','Hard',24,false).latch,[1,1],'additional: 1');assert.deepEqual(LV.doses('latch+links+cells','Super Hard',24,false,700).latch,[2,2],'three types: the lead\'s lower half');
 assert.deepEqual(LV.doses('latch','Normal',24,true).latch,[1,1],'introduction');assert.deepEqual(LV.doses('latch','Super Hard',12,false).latch,[1,1],'small queue');
 assert.deepEqual(LV.doseShares('links+latch','Hard').find(x=>x.m==='latch'),{m:'latch',role:'add',count:[1,1]});
 assert.ok(L.SUPPORTED.has('latch'));assert.deepEqual(L.INTRO.find(([m])=>m==='latch'),['latch',61,63]);assert.equal(L.MECH_RU.latch,'засов и ключ');
 assert.equal(L.slotOf(61).intro,'latch');assert.equal(L.slotOf(63).intro,'latch');assert.equal(L.slotOf(62).allowed.includes('latch'),false);assert.equal(L.slotOf(64).allowed.includes('latch'),true);
 // key holes: empty areas closed in by work, reached without a bridge
 const {b,at}=toy();assert.deepEqual(LV.keyHoles(b).map(h=>h.key),[at(1,1)]);
 const none=L.boardOf({id:'N',title:'n',rows:['GGGGGGG','GGGGGGG','AAAAAAA']});assert.deepEqual(LV.keyHoles(none),[]);
 assert.match(LV.pickLatches(none,[[0],[1],[2,3]],[{color:'A',count:7},{color:'A',count:7},{color:'G',count:7},{color:'G',count:7}],1,'Normal',()=>.5).error,/нет пустой клетки для ключа/);
 const p=L.picture('P0296'),m=L.measure(p),board=L.boardOf(p),base=LV.splitPods(m.volumes,m.N,null),holes=new Set(LV.keyHoles(board).map(h=>h.key));
 for(const [cls,intro,recipe,pos] of [['Normal',true,'latch',null],['Hard',false,'latch',.8],['Normal',false,'links+latch',.5]]){
  let c=null;for(let seed=1;seed<20&&!(c&&c.level);seed++)c=LV.candidate(p,board,base.pods,3,recipe,cls,seed,intro,pos);assert.ok(c.level,`${cls} ${recipe}: ${c.error}`);
  const lv=c.level;assert.deepEqual(R.validate(lv),[]);const d=LV.doses(recipe,cls,lv.pods.length,intro).latch;assert.ok(lv.latches.length>=d[0]&&lv.latches.length<=d[1]);
  if(intro)assert.equal(lv.latches.length,1);
  for(const l of lv.latches){const [lo,hi]=M.LATCH_DEPTH[cls],behind=lv.tray[l.column].length-l.depth;assert.ok(l.depth>=lo&&l.depth<=hi,`depth ${l.depth}`);assert.ok(behind>=2&&behind<=8,`behind ${behind}`);assert.ok(holes.has(l.key));}
  assert.equal(new Set(lv.latches.map(l=>l.column)).size,lv.latches.length,'one latch per column');
  if(!intro)assert.equal(LV.dosesOk({...lv,recipe},cls),null);}});

// a level whose key is reached late: 84 cells of A before 34 of G, the hole high in the G area
function late(){return toy({rows:['GGGGGGG','GGG.GGG','GGGGGGG','GGGGGGG','GGGGGGG',...Array(12).fill('AAAAAAA')],pods:[{color:'A',count:42},{color:'A',count:42},{color:'G',count:30},{color:'G',count:4}],tray:[[0],[1,2],[3]],latches:at=>[{column:2,depth:0,key:at(3,1)}]});}
test('latchCheck: depth by class, pods behind, every latch opens on the saved solution, the last before 80 % W, the columns blocked at once',async()=>{
 const {level}=toy(),route=[0,1,2,3].map(pod=>({type:'tap',pod}));
 const n=await M.latchCheck(level,route,'Normal',{games:4});assert.equal(n.latches[0].atW>0&&n.latches[0].atW<.8,true);assert.equal(n.maxBlocked,1);assert.equal(n.limit,1);
 assert.match(n.problems.join(' | '),/засов 1 \(колонка 3\): глубина 1 вне 3–6 для Normal/);assert.match(n.notes.join(' | '),/за ним подов 1 \(желательно 2–8/);
 const h=await M.latchCheck(level,route,'Hard',{games:4});assert.match(h.problems.join(' | '),/глубина 1 вне 2–6 для Hard/);
 const lt=late(),lr=[0,1,2,3].map(pod=>({type:'tap',pod}));assert.deepEqual(R.validate(lt.level),[]);
 const x=await M.latchCheck(lt.level,lr,'Super Hard',{games:4});assert.ok(x.latches[0].atW>=.8,`opens at ${x.latches[0].atW}`);assert.match(x.problems.join(' | '),/открылся при \d+ % W на сохранённом решении, нужно до 80 %/);
 assert.ok(x.latches[0].waitMs>0,'it stood closed on top');
 const y=await M.latchCheck(lt.level,lr.slice(0,1),'Super Hard',{games:4});assert.match(y.problems.join(' | '),/не открылся на сохранённом решении/);});

// the audit on a sandbox copy
function sandbox(parts=['levels','pictures']){const dir=fs.mkdtempSync(path.join(os.tmpdir(),'rounds-latch-'));
 for(const p of parts)fs.cpSync(path.join(ROOT,'content/rounds',p),path.join(dir,p),{recursive:true});
 fs.copyFileSync(path.join(ROOT,'content/rounds/quality.json'),path.join(dir,'quality.json'));return dir;}
const node=(dir,args)=>spawnSync(process.execPath,args,{cwd:ROOT,env:{...process.env,ROUNDS_DIR:dir},encoding:'utf8',maxBuffer:1<<26});
test('audit: the recipe and the latches, the calendar, the §8 rules on the saved solution',()=>{const dir=sandbox();
 const f=n=>path.join(dir,'levels',`L${String(n).padStart(4,'0')}.json`);
 // L22 (a regular Normal without extras): a latch at depth 1 with its key on a ring cell, the recipe unchanged
 const a=readJSON(f(22)),d=a.definition;d.latches=[{column:0,depth:1,key:d.w+1}];fs.writeFileSync(f(22),JSON.stringify(a));
 const j=path.join(dir,'audit.json');node(dir,[path.join(TOOLS,'audit.mjs'),'22','22','--json',j]);const bad=readJSON(j).levels[0].bad.join(' | ');
 assert.match(bad,/рецепт и засовы не совпадают/);assert.match(bad,/засов 1 \(колонка 1\): глубина 1 вне 3–6 для Normal/);
 // the recipe too: the calendar does not allow it on L22
 a.recipe='latch';a.definition.recipe='latch';fs.writeFileSync(f(22),JSON.stringify(a));node(dir,[path.join(TOOLS,'audit.mjs'),'22','22','--json',j]);
 const bad2=readJSON(j).levels[0].bad.join(' | ');assert.match(bad2,/механика latch не разрешена календарём на L22/);assert.doesNotMatch(bad2,/рецепт и засовы не совпадают/);});

test('export: round-level.v1 latches and mechanic, the schema, the conformance log with latchOpen lines',async()=>{
 const {level}=toy({pods:[{color:'A',count:7},{color:'A',count:7},{color:'G',count:10},{color:'G',count:10,mystery:'depth'}]});
 const L0={level:61,cls:'Normal',intro:'latch',picture:'P0296',recipe:'latch',definition:{...level,id:61,difficulty:'Normal'}};
 const fp=C.fingerprints(),lv=X.levelV1(L0,fp);lv.picture.version=1;
 assert.deepEqual(lv.latches,level.latches);assert.deepEqual(lv.mechanics,['latch']);assert.deepEqual(lv.introduction,{mechanic:'latch',second:false});
 assert.deepEqual(validate('round-level',lv),[]);assert.notDeepEqual(validate('round-level',{...lv,latches:[{column:0,depth:0}]}),[]);
 assert.deepEqual(C.engineLevel(lv).latches,level.latches);
 const route=[0,1,2,3].map(pod=>({type:'tap',pod})),tr=await C.solutionTrace(lv,route),lines=tr.windows.flatMap(w=>w.lines);
 const k=lines.findIndex(l=>/ latchOpen latch=0$/.test(l));assert.ok(k>0);const tUs=lines[k].split(' ')[0];
 assert.match(lines[k-1],new RegExp(`^${tUs} (clear|cellReveal) `),'at the time of the clear before it');assert.equal(lines[k+1],`${tUs} reveal pod=3 mode=depth`);
 const fx=await C.solutionFixture(lv,route);assert.equal(fx.outcome,'Won');assert.deepEqual(fx.nearTies,[]);
 const g=await C.randomFixture(lv,{seedBase:1,runs:5});assert.equal(g.games.length,5);});

test('picture selection: packs, the pack rule, interleaving; pins reserved by the plan; the owner\'s rights pass the franchise filters',()=>{
 assert.equal(L.packOf({batch:'packs-0268-0467',theme:'Ретрофутуризм'}),'packs-0268-0467 · Ретрофутуризм');assert.equal(L.packOf({batch:'batch-0061',theme:'batch-0061'}),'batch-0061');
 assert.equal(L.packOf({batch:'batch-0168-C',theme:'Сказочные герои'}),'batch-0168-C');assert.equal(L.packOf({pack:'X',batch:'b'}),'X');
 assert.deepEqual(L.interleaveByPack(['a1','a2','a3','b1','c1','b2'],x=>x[0]),['a1','b1','c1','a2','b2','a3']);
 const cands=[{id:1,p:'A'},{id:2,p:'B'},{id:3,p:'A',pin:true}],pk=c=>c.p,pin=c=>!!c.pin;
 assert.deepEqual(LV.packRule(cands,new Set(['A']),pin,pk).map(c=>c.id),[2,3],'a neighbour\'s pack out, a pin stays');
 assert.deepEqual(LV.packRule([{id:1,p:'A'}],new Set(['A']),pin,pk).map(c=>c.id),[1],'no other candidate: kept');
 // rights: P0425 and P0465 carry the owner's rights and pass; a franchise picture without them does not
 for(const id of ['P0425','P0465']){const p=L.picture(id);assert.equal(p.rights,'owner');assert.equal(p.rightsNote,'владелец 01.11.2026: взять в волну 51–100');assert.equal(L.franchiseBlocked(p),false);
  assert.equal(L.manifest().pictures.find(e=>e.id===id).rights,'owner');}
 assert.equal(L.franchiseBlocked({source:'сторонняя вселенная'}),true);assert.equal(L.franchiseBlocked({source:'интернет-мем/тренд'}),true);assert.equal(L.franchiseBlocked({source:'оригинальный'}),false);
 // the plan on a sandbox: every pin of the wave L51–L100 reserved on its own fitting position
 const dir=sandbox(['levels','pictures','runs']),r=node(dir,[path.join(TOOLS,'rounds.mjs'),'plan','51','100']);assert.equal(r.status,0,r.stderr);
 const plan=readJSON(path.join(dir,'runs/L0051-L0100/plan.json')),pins=plan.pins;assert.deepEqual(pins.map(x=>x.id),readJSON(path.join(ROOT,'content/rounds/runs/L0051-L0100/mechanics.json')).pin);
 for(const x of pins){assert.equal(x.status,'free',`${x.id}: ${x.reason}`);assert.ok(x.positions.includes(x.reserved));}
 assert.equal(new Set(pins.map(x=>x.reserved)).size,pins.length,'distinct positions');
 const latchPos=new Set(readJSON(path.join(dir,'runs/L0051-L0100/mechanics.json')).levels.filter(l=>l.recipe.split('+').includes('latch')).map(l=>l.level));
 assert.ok(pins.filter(x=>LV.keyHoles(L.boardOf(L.picture(x.id))).length).every(x=>latchPos.has(x.reserved)),'a pin with key holes goes to a latch position');
 assert.match(r.stdout,/## Закреплённые картинки/);assert.match(r.stdout,/Паки свободного пула/);assert.match(r.stdout,/Позиции с засовом: 16/);
 // without the rights a franchise pin is blocked with the reason; the rights command records them (and --clear takes them back)
 assert.equal(node(dir,[path.join(TOOLS,'pictures.mjs'),'rights','P0425','--clear']).status,0);
 node(dir,[path.join(TOOLS,'rounds.mjs'),'plan','51','100']);const p2=readJSON(path.join(dir,'runs/L0051-L0100/plan.json')).pins.find(x=>x.id==='P0425');
 assert.equal(p2.status,'blocked');assert.match(p2.reason,/без подтверждённых прав/);
 assert.equal(node(dir,[path.join(TOOLS,'pictures.mjs'),'rights','P0425','--owner','тест']).status,0);
 const back=readJSON(path.join(dir,'pictures/P0425.json'));assert.equal(back.rights,'owner');assert.equal(back.rightsNote,'тест');assert.equal(readJSON(path.join(dir,'pictures/manifest.json')).pictures.find(e=>e.id==='P0425').rightsNote,'тест');
 assert.notEqual(node(dir,[path.join(TOOLS,'pictures.mjs'),'rights','--owner','x']).status,0,'no ids: refused');});

// node --test tools/rounds/test/ — the Drop water channel and the Twig wooden shutters (17 §8, the owner, 2026-11-05;
// README «Channel», «Shutters»; spec 006 FR-069, FR-070): the engine's rules on small hand-made levels (readiness, the
// choice, the new entry, the ride, the unchosen exit; the shutters' blocking, counter and opening at the exact time),
// the events' order, a restored checkpoint, validation, the fast twin's parity, the conformance lines and the export
// format. Levels without a channel or shutters are proven identical elsewhere (the replay of every game fixture and the
// regenerated C# reference fixture).
import test from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';import {fileURLToPath} from 'node:url';import fs from 'node:fs';

const HERE=path.dirname(fileURLToPath(import.meta.url)),ROOT=path.resolve(HERE,'../../..');
const L=await import('../lib.mjs');
const {engine}=await import('../engine.mjs');const {R,T,idle}=await engine();
const C=await import('../conformance.mjs');const X=await import('../export.mjs');const {validate}=await import('../schema.mjs');
const {REF,FAST,replay,randomGame,calmGame}=await import('../perf/identity.mjs');
const placed=n=>JSON.parse(fs.readFileSync(path.join(ROOT,'content/rounds/levels',`L${String(n).padStart(4,'0')}.json`),'utf8'));

// the channel level: B (26 cells) round the spring at picture (4, 4), over G with a pocket at (1, 1) and R with a pocket at
// (7, 1); exit 0 is the G pocket, exit 1 the R pocket; both pockets are enclosed (no entry reaches them by itself)
export function channelToy(){const b=L.boardOf({id:'T',title:'t',rows:['GGGGGRRRR','G.GGGRR.R','GGGGGRRRR','BBBBBBBBB','BBBB.BBBB','BBBBBBBBB']}),at=(x,y)=>(b.h-1-(y+2))*b.w+x+2;
 const route=pts=>pts.map(([x,y])=>at(x,y));
 return {at,b,level:{id:1,w:b.w,h:b.h,rowsBottomFirst:b.rowsBottomFirst,entries:b.entries,capacity:5,pods:[{color:'B',count:26},{color:'G',count:14},{color:'R',count:11}],tray:[[0],[1],[2]],
  channel:{spring:at(4,4),exits:[{cell:at(1,1),route:route([[4,4],[4,3],[4,2],[3,2],[2,2],[1,2],[1,1]]),rideUs:600000},{cell:at(7,1),route:route([[4,4],[5,4],[6,4],[7,4],[7,3],[7,2],[7,1]]),rideUs:800000}]}}};}
// the shutters level: an A frame round a G rectangle (10 cells) under a shutter that counts 5 units of B (7 cells below)
export function shutterToy(count=5,material='B'){const b=L.boardOf({id:'T',title:'t',rows:['AAAAAAA','AGGGGGA','AGGGGGA','AAAAAAA','BBBBBBB']}),at=(x,y)=>(b.h-1-(y+2))*b.w+x+2;
 const cells=[];for(let y=1;y<=2;y++)for(let x=1;x<=5;x++)cells.push(at(x,y));cells.sort((p,q)=>p-q);
 return {at,b,cells,level:{id:1,w:b.w,h:b.h,rowsBottomFirst:b.rowsBottomFirst,entries:b.entries,capacity:5,pods:[{color:'A',count:18},{color:'B',count:7},{color:'G',count:10}],tray:[[0],[1],[2]],
  shutters:[{cells,material,count}]}};}
// both: the channel level with a shutter over the G cells at picture x 2–4, y 0–2 that counts 10 units of B
export function bothToy(){const {level,at}=channelToy(),cells=[];for(let y=0;y<=2;y++)for(let x=2;x<=4;x++)cells.push(at(x,y));
 return {...level,shutters:[{cells:cells.sort((p,q)=>p-q),material:'B',count:10}]};}
const beside=(w,a,c)=>Math.abs(a-c)===w||(Math.abs(a-c)===1&&Math.floor(a/w)===Math.floor(c/w));
// play the timeline in small steps, collecting every event but assign
function run(t,until=()=>false){const ev=[];while(t.pending){t.advance(50);for(const e of t.lastEvents)if(e.type!=='assign')ev.push(e);if(until(ev))break;}return ev;}

test('channel: not choosable before the spring is reached; channelReady at the time of the clear that reached it',()=>{
 const {level,at,b}=channelToy();assert.deepEqual(R.validate(level),[]);
 const s=R.create(level);assert.deepEqual(R.channelChoices(s),[]);assert.ok(!R.legal(s).some(a=>a.type==='channel'));
 assert.equal(R.check(s,{type:'channel',exit:0}),'channel-not-ready');assert.equal(R.view(s).channel,-1);assert.equal(R.view(s).channelReady,false);
 const t=new T.Timeline(R.create(level));t.commit({type:'tap',pod:0});
 const ev=run(t,e=>e.some(x=>x.type==='channelReady')),k=ev.findIndex(e=>e.type==='channelReady');assert.ok(k>0);
 let j=k-1;while(['cellReveal','latchOpen'].includes(ev[j].type))j--;
 assert.equal(ev[j].type,'clear','right after the clear that reached it');assert.equal(ev[k].at,ev[j].at);
 const cleared=t.jobs.get(ev[j].id)?.cell??null;if(cleared!==null)assert.ok(beside(b.w,cleared,at(4,4)));
 assert.deepEqual(R.legal(t.state).filter(a=>a.type==='channel'),[{type:'channel',exit:0},{type:'channel',exit:1}]);
 assert.equal(R.view(t.state).channelReady,true);assert.equal(C.lineOf(ev[k],null),`${C.us(ev[k].at)} channelReady`);
 // the idle rules: channelReady after the round of clears that reached the spring, before that round's completes
 const r=R.step(R.create(level),{type:'tap',pod:0},true),q=r.events.findIndex(e=>e.type==='channelReady');
 assert.ok(q>0&&r.events[q-1].type==='clear');assert.ok(!r.events.slice(0,q).some(e=>e.type==='complete'));
 assert.equal(r.events.filter(e=>e.type==='channelReady').length,1);});

test('channel: the choice is free, makes the chosen exit an active entry, never the other; status stays Playing while it waits',()=>{
 const {level,at}=channelToy(),t=new T.Timeline(R.create(level));t.commit({type:'tap',pod:0});t.advance(1e9);
 // a jam does not end the attempt while the channel can still be chosen (17 §1)
 assert.equal(t.state.status,'Playing');assert.deepEqual(Array.from(t.state.slots),[-1,-1,-1,-1,-1]);
 const free=t.free,r=t.commit({type:'channel',exit:0});assert.ok(r.accepted);assert.deepEqual(r.events,[{type:'channelOpen',exit:0}]);
 assert.equal(t.free,free,'no place taken');assert.equal(t.state.channel,0);assert.deepEqual(R.channelChoices(t.state),[]);
 assert.equal(t.check({type:'channel',exit:1}),'channel-not-ready','the choice is final');
 const d=R.reach(t.state).distance;assert.equal(d[at(1,1)],1,'the chosen exit is a source');assert.ok(!(d[at(7,1)]>0),'the unchosen pocket stays closed');
 assert.ok(R.reach(t.state).targets.includes(at(1,0))&&R.reach(t.state).targets.includes(at(2,1)),'the cubes round the exit are targets');
 assert.equal(C.lineOf(r.events[0],1234),'1234 channelOpen exit=0');
 // every route starts at an entry or the chosen exit, never at the other exit
 t.commit({type:'tap',pod:1});t.commit({type:'tap',pod:2});const starts=new Set();
 while(t.pending){t.advance(100);for(const e of t.lastEvents)if(e.type==='depart'){const j=t.jobs.get(e.id);starts.add(j.route[0]);}}
 assert.equal(t.state.status,'Won');assert.ok(!starts.has(at(7,1)));assert.ok(starts.has(at(1,1)));for(const s of starts)assert.ok(s===at(1,1)||level.entries.includes(s));
 // the unchosen pocket joined the open area by clearing is a passage, still no source
 assert.ok(R.reach(t.state).distance[at(7,1)]>1);});

test('channel: a walker from the exit rides rideUs out and back; couriers show it riding at the exit',()=>{
 const {level,at}=channelToy(),t=new T.Timeline(R.create(level));t.commit({type:'tap',pod:0});t.advance(1e9);t.commit({type:'channel',exit:0});
 t.commit({type:'tap',pod:1});const dep=t.lastEvents.find(e=>e.type==='depart'),j=t.jobs.get(dep.id);
 assert.equal(j.route[0],at(1,1));assert.equal(j.ride,600);const dist=j.route.length-1,start=j.start;
 assert.equal(j.arrive,start+600+Math.max(650,dist*100));assert.equal(j.clear,j.arrive+100);assert.equal(j.done,j.clear+Math.max(400,dist*75)+600);
 const line=C.lineOf(dep,null);assert.match(line,new RegExp(` arrive=${C.us(j.arrive)} clear=${C.us(j.clear)} done=${C.us(j.done)} route=${j.route.length}$`));
 const c=t.couriers.find(x=>x.id===dep.id);assert.equal(c.riding,true);assert.equal(c.x,at(1,1)%level.w);
 t.advance(650);const c2=t.couriers.find(x=>x.id===dep.id);assert.equal(c2.riding,false,'out of the water after the ride');
 // a walker from an ordinary entry has no ride
 const plain=new T.Timeline(R.create(level));plain.commit({type:'tap',pod:0});const p=plain.jobs.values().next().value;assert.equal(p.ride,undefined);
 assert.equal(plain.couriers[0].riding,undefined);});

test('channel: the signature carries the chosen exit; a restored checkpoint keeps it and recomputes the rest',()=>{
 const {level}=channelToy(),t=new T.Timeline(R.create(level));t.commit({type:'tap',pod:0});t.advance(700);
 const before=R.signature(t.state).split('|');assert.equal(before.at(-2),'-1');
 t.advance(1e9);t.commit({type:'channel',exit:1});t.commit({type:'tap',pod:2});t.advance(1500);
 assert.equal(R.signature(t.state).split('|').at(-2),'1');
 const snap=JSON.parse(JSON.stringify(t.snapshot()));assert.equal(snap.state.channel,1);
 const u=new T.Timeline(R.create(level),snap);assert.equal(R.signature(u.state),R.signature(t.state));assert.equal(u.state.springReached,1);
 assert.deepEqual(R.view(u.state),R.view(t.state));t.advance(1e9);u.advance(1e9);assert.equal(R.signature(u.state),R.signature(t.state));
 assert.throws(()=>new T.Timeline(R.create(level),{...snap,state:{...snap.state,channel:2}}),/Invalid checkpoint/);
 // a level without a channel: no channel in its signature, view or checkpoint
 const lv=placed(5).definition,s=R.create(lv);assert.equal(R.view(s).channel,undefined);assert.equal(R.signature(s).split('|').length,8);
 assert.equal(new T.Timeline(s).snapshot().state.channel,undefined);});

test('channel: validation',()=>{
 const {level,at}=channelToy(),ch=level.channel,v=c=>R.validate({...level,channel:c});
 assert.deepEqual(v({...ch,spring:at(0,0)}),['channel'],'the spring is an empty cell');
 assert.deepEqual(v({...ch,spring:level.entries[0]}),['channel'],'never an entry');
 assert.deepEqual(v({...ch,exits:[ch.exits[0]]}),['channel'],'two exits');
 assert.deepEqual(v({...ch,exits:[ch.exits[0],{...ch.exits[0]}]}),['channel exit'],'exits differ');
 assert.deepEqual(v({...ch,exits:[{...ch.exits[0],route:[ch.spring,ch.exits[0].cell]},ch.exits[1]]}),['channel route'],'orthogonal steps');
 assert.deepEqual(v({...ch,exits:[{...ch.exits[0],route:ch.exits[0].route.slice(1)},ch.exits[1]]}),['channel route'],'from the spring');
 assert.deepEqual(v({...ch,exits:[{...ch.exits[0],rideUs:1500},ch.exits[1]]}),['channel ride'],'whole milliseconds');
 assert.deepEqual(v({...ch,exits:[{...ch.exits[0],rideUs:0},ch.exits[1]]}),['channel ride']);
 assert.deepEqual(R.validate({...level,latches:[{column:0,depth:0,key:ch.spring}]}).includes('channel'),true,'never a latch key');
 // a spring walled in can never be reached
 const rows=level.rowsBottomFirst.slice(),y=Math.floor(ch.spring/level.w),x=ch.spring%level.w;
 const wall=(r,i)=>r.slice(0,i)+'#'+r.slice(i+1);rows[y-1]=wall(rows[y-1],x);rows[y+1]=wall(rows[y+1],x);rows[y]=wall(wall(rows[y],x-1),x+1);
 const walled={...level,rowsBottomFirst:rows,pods:[{color:'B',count:22},{color:'G',count:14},{color:'R',count:11}]};
 assert.deepEqual(R.validate(walled),['channel spring unreachable']);});

test('shutters: closed cells are no targets and no passage; the counter falls with work done anywhere; opens at the exact time of the clear',()=>{
 const {level,cells}=shutterToy();assert.deepEqual(R.validate(level),[]);
 let s=R.create(level);assert.deepEqual(Array.from(s.shut),[1]);assert.equal(R.shutterLeft(s,0),5);assert.deepEqual(R.view(s).shutters,[5]);
 const t=new T.Timeline(R.create(level));t.commit({type:'tap',pod:0});t.advance(1e9);
 const targets=new Set(R.reach(t.state).targets);for(const c of cells){assert.ok(!targets.has(c),`cell ${c} under the shutter is no target`);assert.ok(R.shutAt(t.state,c));}
 assert.ok(cells.some(c=>R.reach(t.state).distance.some((d,i)=>d>0&&Math.abs(i-c)===level.w)),'an open reached cell touches the shutter');
 t.commit({type:'tap',pod:2});assert.equal(t.lastEvents.filter(e=>e.type==='depart').length,0,'its work waits');
 t.commit({type:'tap',pod:1});const left=[];let opened=null;
 while(t.pending){t.advance(50);for(const e of t.lastEvents){if(e.type==='clear'&&opened===null)left.push(R.view(t.state).shutters[0]);if(e.type==='shutterOpen')opened=e;}}
 assert.ok(opened);assert.deepEqual(C.lineOf(opened,null),`${C.us(opened.at)} shutterOpen shutter=0`);
 assert.equal(t.state.status,'Won');assert.equal(t.state.shut[0],0);assert.equal(R.shutterLeft(t.state,0),0);
 // at its time: the fifth B clear; the G pod departs at that moment
 const u=new T.Timeline(R.create(level));u.commit({type:'tap',pod:0});u.advance(1e9);u.commit({type:'tap',pod:2});u.commit({type:'tap',pod:1});
 const ev=run(u),k=ev.findIndex(e=>e.type==='shutterOpen'),clears=ev.slice(0,k).filter(e=>e.type==='clear');
 assert.equal(clears.length,5);assert.equal(ev[k-1].type,'clear');assert.equal(ev[k].at,ev[k-1].at);
 const firstG=ev.find(e=>e.type==='depart'&&e.plan[1]===2);assert.equal(firstG.at,ev[k].at,'the waiting pod leaves at that moment');
 // the idle rules: shutterOpen after the round's clears, before its completes
 const r=R.step(R.step(R.step(R.create(level),{type:'tap',pod:0}).state,{type:'tap',pod:2}).state,{type:'tap',pod:1},true),q=r.events.findIndex(e=>e.type==='shutterOpen');
 assert.ok(q>0&&r.events[q-1].type==='clear');assert.deepEqual(r.events[q],{type:'shutterOpen',shutter:0});assert.equal(r.state.status,'Won');});

test('shutters: a restored checkpoint recomputes them from the board; levels without shutters have no shut',()=>{
 const {level}=shutterToy(),t=new T.Timeline(R.create(level));t.commit({type:'tap',pod:0});t.advance(1e9);t.commit({type:'tap',pod:1});
 for(let g=0;g<200&&t.pending;g++){t.advance(100);const u=new T.Timeline(R.create(level),JSON.parse(JSON.stringify(t.snapshot())));
  assert.deepEqual(Array.from(u.state.shut),Array.from(t.state.shut));assert.deepEqual(R.view(u.state),R.view(t.state));assert.deepEqual(R.reach(u.state).targets,R.reach(t.state).targets);}
 assert.equal(R.create(placed(5).definition).shut,null);assert.equal(R.view(R.create(placed(5).definition)).shutters,undefined);});

test('shutters: validation (rectangle of colour cells, count reachable without the cells still closed, no cycle)',()=>{
 const {level,cells,at}=shutterToy(),v=sh=>R.validate({...level,shutters:sh});
 assert.deepEqual(v([{cells,material:'B',count:8}]),['shutter count unreachable'],'more than the B outside');
 assert.deepEqual(v([{cells,material:'G',count:1}]),['shutter count unreachable'],'never its own cells only');
 assert.deepEqual(v([{cells:cells.slice(1),material:'B',count:1}]),['shutter'],'a full rectangle');
 assert.deepEqual(v([{cells:[...cells].reverse(),material:'B',count:1}]),['shutter'],'sorted');
 assert.deepEqual(v([{cells,material:'Q',count:1}]),['shutter']);assert.deepEqual(v([{cells,material:'B',count:0}]),['shutter']);
 assert.deepEqual(v([{cells:[at(0,0),at(1,0)],material:'B',count:1},{cells:[at(1,0)],material:'B',count:1}]),['shutter'],'never over another shutter');
 assert.deepEqual(v([]),['shutters']);
 // two shutters: the inner A row under one that counts G, a corner G cell under one that counts A (a cycle when the first
 // needs that G cell) or B (a chain)
 const b=L.boardOf({id:'T',title:'t',rows:['GGGGGGG','GAAAAAG','GGGGGGG','BBBBBBB']}),p=(x,y)=>(b.h-1-(y+2))*b.w+x+2;
 const lv={id:1,w:b.w,h:b.h,rowsBottomFirst:b.rowsBottomFirst,entries:b.entries,capacity:5,pods:[{color:'G',count:16},{color:'A',count:5},{color:'B',count:7}],tray:[[0],[1],[2]]};
 const inner=[1,2,3,4,5].map(x=>p(x,1)).sort((x,y)=>x-y),corner=[p(0,0)];
 assert.deepEqual(R.validate({...lv,shutters:[{cells:inner,material:'G',count:16}]}),[]);
 assert.deepEqual(R.validate({...lv,shutters:[{cells:inner,material:'G',count:16},{cells:corner,material:'A',count:1}]}),['shutter count unreachable'],'a cycle');
 assert.deepEqual(R.validate({...lv,shutters:[{cells:inner,material:'G',count:15},{cells:corner,material:'A',count:1}]}),[],'the first opens without the corner, then the second');
 assert.deepEqual(R.validate({...lv,shutters:[{cells:inner,material:'G',count:16},{cells:corner,material:'B',count:7}]}),[],'a chain opens');
 // a shutter over an entry or a hidden cell
 assert.deepEqual(R.validate({...level,hiddenCells:[cells[0]]}),['shutter']);});

test('the fast twin plays the channel and shutter levels exactly as the reference',()=>{
 const cases=[['channel',channelToy().level],['shutters',shutterToy().level],['both',bothToy()]];
 for(const [name,lv] of cases){assert.deepEqual(R.validate(lv),[],name);
  for(let g=0;g<12;g++){const s=7919+g*104729+1;assert.deepEqual(randomGame(FAST,lv,s,null),randomGame(REF,lv,s,null),`${name} random game ${g}`);}
  for(let g=0;g<4;g++){const s=3*7919+g*104729+5;assert.deepEqual(calmGame(FAST,lv,s,null),calmGame(REF,lv,s,null),`${name} calm game ${g}`);}}
 const ch=channelToy().level,sh=shutterToy().level;
 for(const patient of [true,false]){
  assert.deepEqual(replay(FAST,ch,[{type:'tap',pod:0},{type:'channel',exit:0},{type:'tap',pod:1},{type:'tap',pod:2}],patient),replay(REF,ch,[{type:'tap',pod:0},{type:'channel',exit:0},{type:'tap',pod:1},{type:'tap',pod:2}],patient),'channel route');
  assert.deepEqual(replay(FAST,sh,[{type:'tap',pod:0},{type:'tap',pod:2},{type:'tap',pod:1}],patient),replay(REF,sh,[{type:'tap',pod:0},{type:'tap',pod:2},{type:'tap',pod:1}],patient),'shutter route');}
 assert.deepEqual(FAST.R.validate({...sh,shutters:[{cells:sh.shutters[0].cells,material:'B',count:8}]}),REF.R.validate({...sh,shutters:[{cells:sh.shutters[0].cells,material:'B',count:8}]}));});

// a level that needs the channel: the G area is walled in; only exit 0 (a pocket inside it) opens it, exit 1 is on the edge
export function needToy(){const b=L.boardOf({id:'T',title:'t',rows:['#######','#GGGG.#','#GGGGG#','#######','BBB.BBB','BBBBBB.']}),at=(x,y)=>(b.h-1-(y+2))*b.w+x+2;
 const route=pts=>pts.map(([x,y])=>at(x,y));
 return {at,level:{id:1,w:b.w,h:b.h,rowsBottomFirst:b.rowsBottomFirst,entries:b.entries,capacity:5,pods:[{color:'B',count:12},{color:'G',count:9}],tray:[[0],[1]],
  channel:{spring:at(3,4),exits:[{cell:at(5,1),route:route([[3,4],[3,3],[3,2],[4,2],[5,2],[5,1]]),rideUs:500000},{cell:at(6,5),route:route([[3,4],[4,4],[5,4],[6,4],[6,5]]),rideUs:400000}]}}};}

test('the patient solver finds the channel choice and the random policy offers each exit once ready',async()=>{
 const {solver}=await import('../../level-stats/solve.mjs');const {level:lv}=needToy();assert.deepEqual(R.validate(lv),[]);
 const r=solver(REF,100000).route(lv,5);assert.equal(r.win,true);assert.ok(r.route.some(a=>a.type==='channel'&&a.exit===0),JSON.stringify(r.route));
 // with exit 1 the walled G area is never reached: the attempt sticks
 let s=idle.create(lv,5);s=idle.step(s,{type:'tap',pod:0}).state;assert.deepEqual(idle.legal(s).filter(a=>a.type==='channel'),[{type:'channel',exit:0},{type:'channel',exit:1}]);
 let w=idle.step(s,{type:'channel',exit:1}).state;w=idle.step(w,{type:'tap',pod:1}).state;assert.equal(w.status,'Stuck');
 let v=idle.step(s,{type:'channel',exit:0}).state;v=idle.step(v,{type:'tap',pod:1}).state;assert.equal(v.status,'Won');
 assert.deepEqual(solver(FAST,100000).route(lv,5),r,'the fast twin searches the same');
 const g=await C.randomFixture({format:'round-level.v1',number:1,board:{w:lv.w,h:lv.h,rowsBottomFirst:lv.rowsBottomFirst,entries:lv.entries,channel:lv.channel},capacity:5,pods:lv.pods.map(p=>({code:p.color,count:p.count})),tray:lv.tray},{seedBase:1,runs:20});
 assert.equal(g.games.length,20);assert.ok(g.wins>0);});

test('export and conformance: board.channel and board.shutters in round-level.v1, the schema, engineLevel back, the channel command in a fixture',async()=>{
 const ch=channelToy().level,sh=shutterToy().level;
 const L1={level:1,cls:'Normal',recipe:'canal',picture:'P0001',definition:{...ch,capacity:5},solution:{route:[]}};
 const lv=X.levelV1(L1,C.fingerprints());lv.picture.version=1;
 assert.deepEqual(lv.board.channel,ch.channel);assert.deepEqual(lv.mechanics,['canal']);assert.deepEqual(validate('round-level',lv),[]);
 const back=C.engineLevel(lv);assert.deepEqual(back.channel,ch.channel);assert.deepEqual(R.validate(back),[]);
 const L2={level:2,cls:'Normal',recipe:'shutters',picture:'P0001',definition:{...sh,capacity:5},solution:{route:[]}};
 const lv2=X.levelV1(L2,C.fingerprints());lv2.picture.version=1;
 assert.deepEqual(lv2.board.shutters,[{cells:sh.shutters[0].cells,code:'B',count:5}]);assert.deepEqual(validate('round-level',lv2),[]);
 assert.deepEqual(C.engineLevel(lv2).shutters,sh.shutters);
 const sol=await C.solutionFixture(lv,[{type:'tap',pod:0},{type:'channel',exit:0},{type:'tap',pod:1},{type:'tap',pod:2}]);
 assert.equal(sol.outcome,'Won');assert.deepEqual(sol.commands[1],{type:'channel',exit:0,atUs:sol.commands[1].atUs});
 const tr=await C.solutionTrace(lv,[{type:'tap',pod:0},{type:'channel',exit:0},{type:'tap',pod:1},{type:'tap',pod:2}]);
 assert.ok(tr.windows[0].lines.some(l=>/ channelReady$/.test(l)));assert.match(tr.windows[1].lines[0],/^\d+ channelOpen exit=0$/);
 assert.deepEqual(validate('round-proof',{format:'round-proof.v1',number:1,definitionVersion:1,solution:{commands:sol.commands,outcome:'Won',endUs:sol.endUs},
  randomTest:{policy:X.POLICY,seedBase:1,runs:100,wins:100,share:1,wilson95:[0.96,1],classOfShare:'Normal'},checks:{},audit:{result:'OK',report:'x',notes:[]},engine:C.fingerprints()}).filter(e=>/command|solution/.test(e)),[]);
 assert.deepEqual(X.fixtureShape({format:'round-conformance.v1',number:1,definitionVersion:1,engine:C.fingerprints(),solution:{windows:sol.windows,outcome:'Won',endUs:sol.endUs,finalSignature:sol.finalSignature},random:{policy:X.FIXTURE_POLICY,seedBase:1,runs:100,games:Array.from({length:100},()=>({seed:1,outcome:'Won',endUs:1,finalSignature:sol.finalSignature})),wins:100},owner:[]}),[]);});

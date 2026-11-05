// node --test tools/rounds/test/ — the class of a deciding-exit water channel level (17 §5, the owner, 2026-11-05): a hand
// Super Hard where exactly one exit wins (the wave plan's channelKind single) is measured with that exit only, as a bridge
// level is with its best bridge: randomTest / calmTest / quickShare {exit}, evaluate channelKind single, the conformance
// fixture's random games and the export's proof and fixture shape. Without an exit every test plays exactly as before.
import test from 'node:test';
import assert from 'node:assert/strict';

const L=await import('../lib.mjs');
const M=await import('../measure.mjs');
const C=await import('../conformance.mjs');const X=await import('../export.mjs');const {validate}=await import('../schema.mjs');
const {games}=await import('../perf/pool.mjs');const {keepOf,keepBridge}=await import('../perf/games.mjs');
process.argv.splice(2,0,'noop');const LV=await import('../levels.mjs');process.argv.splice(2,1);

// the «need» level of channel-shutters.test.mjs: the G area is walled in; only exit 0 (a pocket inside it) opens it, exit 1
// is on the edge (won with exit 0, stuck with exit 1)
function needToy(){const b=L.boardOf({id:'T',title:'t',rows:['#######','#GGGG.#','#GGGGG#','#######','BBB.BBB','BBBBBB.']}),at=(x,y)=>(b.h-1-(y+2))*b.w+x+2;
 const route=pts=>pts.map(([x,y])=>at(x,y));
 return {id:1,w:b.w,h:b.h,rowsBottomFirst:b.rowsBottomFirst,entries:b.entries,capacity:5,pods:[{color:'B',count:12},{color:'G',count:9}],tray:[[0],[1]],recipe:'canal',
  channel:{spring:at(3,4),exits:[{cell:at(5,1),route:route([[3,4],[3,3],[3,2],[4,2],[5,2],[5,1]]),rideUs:500000},{cell:at(6,5),route:route([[3,4],[4,4],[5,4],[6,4],[6,5]]),rideUs:400000}]}};}
// the channel level of channel-shutters.test.mjs with B split in two pods (17 §3): both exits win
function bothWinToy(){const b=L.boardOf({id:'T',title:'t',rows:['GGGGGRRRR','G.GGGRR.R','GGGGGRRRR','BBBBBBBBB','BBBB.BBBB','BBBBBBBBB']}),at=(x,y)=>(b.h-1-(y+2))*b.w+x+2;
 const route=pts=>pts.map(([x,y])=>at(x,y));
 return {id:1,w:b.w,h:b.h,rowsBottomFirst:b.rowsBottomFirst,entries:b.entries,capacity:5,pods:[{color:'B',count:13},{color:'B',count:13},{color:'G',count:14},{color:'R',count:11}],tray:[[0,1],[2],[3]],recipe:'canal',
  channel:{spring:at(4,4),exits:[{cell:at(1,1),route:route([[4,4],[4,3],[4,2],[3,2],[2,2],[1,2],[1,1]]),rideUs:600000},{cell:at(7,1),route:route([[4,4],[5,4],[6,4],[7,4],[7,3],[7,2],[7,1]]),rideUs:800000}]}};}
const volumes=lv=>{const v={};for(const ch of lv.rowsBottomFirst.join(''))if(L.CODES.includes(ch))v[ch]=(v[ch]||0)+1;return v;};
const mOf=lv=>{const v=volumes(lv);return {N:Object.values(v).reduce((a,b)=>a+b,0),volumes:v};};
const defOf=lv=>({format:'round-level.v1',number:1,board:{w:lv.w,h:lv.h,rowsBottomFirst:lv.rowsBottomFirst,entries:lv.entries,channel:lv.channel},capacity:5,pods:lv.pods.map(p=>({code:p.color,count:p.count})),tray:lv.tray});

test('keepOf: no exit is keepBridge itself; an exit keeps only that channel choice',()=>{
 assert.equal(keepOf(null,null),null);assert.equal(keepOf(undefined,undefined),null);
 const kb=keepOf(1,null),ref=keepBridge(1);for(const a of [{type:'bridge',choice:0},{type:'bridge',choice:1},{type:'tap',pod:0}])assert.equal(kb(a),ref(a));
 const k=keepOf(null,0);assert.equal(k({type:'channel',exit:0}),true);assert.equal(k({type:'channel',exit:1}),false);assert.equal(k({type:'tap',pod:3}),true);assert.equal(k({type:'bridge',choice:1}),true);
 const kk=keepOf(1,0);assert.equal(kk({type:'bridge',choice:0}),false);assert.equal(kk({type:'channel',exit:1}),false);assert.equal(kk({type:'bridge',choice:1}),true);assert.equal(kk({type:'channel',exit:0}),true);});

test('randomTest, calmTest and quickShare with the winning exit; without it the results are those of before',async()=>{
 const lv=needToy(),ch=await M.channelExits(lv,20);
 assert.equal(ch.decides,true);assert.equal(M.winningExit(ch),0);assert.equal(M.winningExit({...ch,decides:false}),null);
 const all=await M.randomTest(lv),win=await M.randomTest(lv,{exit:0}),lose=await M.randomTest(lv,{exit:1});
 assert.equal(all.exit,undefined,'no exit key without the restriction');assert.equal('exit' in all,false);
 // unchanged: the uniform test is the old games call ({seed, bridge} only)
 const old=await games('random',lv,{seed:1,bridge:null},{count:all.runs});assert.equal(all.wins,old.filter(Boolean).length);
 assert.equal(win.exit,0);assert.equal(win.share,1);assert.equal(win.runs,100);assert.equal(lose.exit,1);assert.equal(lose.share,0);
 assert.ok(all.share>0&&all.share<1,'uniform over both exits some games choose the losing one');
 const calm=await M.calmTest(lv,{exit:0});assert.equal(calm.exit,0);assert.equal(calm.share,1);assert.equal('exit' in await M.calmTest(lv),false);
 assert.equal(await M.quickShare(lv,40,11,null,0),1);assert.equal(await M.quickShare(lv,40,11,null,1),0);
 assert.deepEqual(M.classKeep({bridge:1}),{bridge:1});assert.deepEqual(M.classKeep({exit:0}),{exit:0});assert.deepEqual(M.classKeep({seedBase:1}),{});assert.deepEqual(M.classKeep(null),{});});

test('the conformance fixture plays the exit-restricted games of measure.mjs, game for game',async()=>{
 const lv=needToy(),def=defOf(lv),rt=await M.randomTest(lv,{exit:0});
 const fx=await C.randomFixture(def,{seedBase:rt.seedBase,runs:rt.runs,exit:0});assert.equal(fx.wins,rt.wins);assert.equal(fx.games.length,rt.runs);
 const free=await C.randomFixture(def,{seedBase:1,runs:20});assert.ok(free.wins<20,'without the restriction some game loses');
 const res=await games('random',lv,{seed:1,bridge:null,exit:1},{count:20});const lost=await C.randomFixture(def,{seedBase:1,runs:20,exit:1});
 assert.deepEqual(lost.games.map(g=>g.outcome==='Won'),res);
 // the proof's randomTest.exit is in the schema; the fixture shape takes random.exit
 const sol=await C.solutionFixture(def,[{type:'tap',pod:0},{type:'channel',exit:0},{type:'tap',pod:1}]);assert.equal(sol.outcome,'Won');
 const proof={format:'round-proof.v1',number:1,definitionVersion:1,solution:{commands:sol.commands,outcome:'Won',endUs:sol.endUs},
  randomTest:{policy:X.POLICY,seedBase:1,runs:100,wins:100,share:1,wilson95:[.963,1],classOfShare:'Normal',exit:0},checks:{},audit:{result:'OK',report:'x'},engine:C.fingerprints()};
 assert.deepEqual(validate('round-proof',proof),[]);assert.notDeepEqual(validate('round-proof',{...proof,randomTest:{...proof.randomTest,exit:-1}}),[]);
 const sh={format:'round-conformance.v1',number:1,definitionVersion:1,engine:C.fingerprints(),solution:{windows:[{command:{type:'tap',pod:0,atUs:0},events:'sha256:'+'0'.repeat(64),signature:'sha256:'+'0'.repeat(64)}],outcome:'Won',endUs:1,finalSignature:'sha256:'+'0'.repeat(64)},
  random:{policy:X.FIXTURE_POLICY,seedBase:1,runs:100,exit:0,games:Array.from({length:100},(_,i)=>({seed:i,outcome:'Won',endUs:1,finalSignature:'sha256:'+'0'.repeat(64)})),wins:100},owner:[]};
 assert.deepEqual(X.fixtureShape(sh),[]);sh.random.exit=-1;assert.deepEqual(X.fixtureShape(sh),['random']);});

test('evaluate channelKind single: only on a channel Super Hard, and the channel must decide',async()=>{
 const need=needToy(),both=bothWinToy(),lines=[];
 const r1=await LV.evaluate(both,{m:mOf(both),cls:'Super Hard',channelKind:'single'});assert.equal(r1.reason,'канал: решает не ровно один выход (single)');
 const r2=await LV.evaluate(need,{m:mOf(need),cls:'Hard',channelKind:'single'});assert.equal(r2.reason,'канал: single только на Super Hard (§5)');
 const {channel:_,...plain}=need;const r3=await LV.evaluate({...plain,recipe:'none'},{m:mOf(need),cls:'Super Hard',channelKind:'single'});assert.equal(r3.reason,'канал: план single, а на уровне нет канала');
 // the deciding channel passes to the quick estimate, which takes the winning exit: 100 %, too easy for Super Hard
 const r4=await LV.evaluate(need,{m:mOf(need),cls:'Super Hard',channelKind:'single',tick:l=>lines.push(l)});
 assert.equal(r4.reason,'не тот класс по быстрой оценке');assert.equal(r4.quick,1);assert.match(lines[0],/^выходы канала /);
 // without channelKind the quick estimate stays uniform over both exits
 const r5=await LV.evaluate(need,{m:mOf(need),cls:'Super Hard'});assert.ok(r5.quick<1,String(r5.quick));});

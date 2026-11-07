// node --test tools/rounds/test/ — the builder places the Drop water channel and the Twig wooden shutters (17 §7, §8, §9,
// the owner, 2026-11-05; README «Channel and shutters in the builder»): the doses, the channel's spring behind work and two
// exits deep in different parts (carved out of the picture as overlay.keys), the shutters' rectangle and counter, the
// candidate carrying both, the checks of the saved solution, the strategies and the wave plan L101–L150.
import test from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';import fs from 'node:fs';import {fileURLToPath} from 'node:url';
const HERE=path.dirname(fileURLToPath(import.meta.url)),ROOT=path.resolve(HERE,'../../..');
process.argv.splice(2,0,'noop');const LV=await import('../levels.mjs');process.argv.splice(2,1);
const L=await import('../lib.mjs');const M=await import('../measure.mjs');const X=await import('../export.mjs');const {validate}=await import('../schema.mjs');
const {engine}=await import('../engine.mjs');const {R,T}=await engine();
const rnd=s=>{s=s>>>0||1;return()=>{s^=s<<13;s^=s>>>17;s^=s<<5;return(s>>>0)/4294967296;};};
// a filled 20 × 18 picture of four materials (quadrants), deep enough for a channel (access depth up to 9)
const TOY={id:'TOY',title:'toy',rows:Array.from({length:18},(_,y)=>Array.from({length:20},(_,x)=>x<10?(y<9?'G':'B'):(y<9?'R':'Y')).join(''))};
// a 22 × 20 picture with an object in its upper middle (blue and yellow halves round a red core) on a green and a cyan
// ground: the island behind water of the channel (17 §8 «Остров за водой», 2026-11-07)
const ISL={id:'ISL',title:'island',rows:Array.from({length:20},(_,y)=>Array.from({length:22},(_,x)=>x>=5&&x<=16&&y>=2&&y<=12?(x>=9&&x<=12&&y>=5&&y<=9?'R':x<11?'B':'Y'):(y<14?'G':'C')).join(''))};
const pods=b=>{const v={};for(const c of b.rowsBottomFirst.join(''))if(L.CODES.includes(c))v[c]=(v[c]||0)+1;const ps=[];for(const [c,n] of Object.entries(v)){const a=Math.ceil(n/2);ps.push({color:c,count:a},{color:c,count:n-a});}return ps;};
const levelOf=(b,ps=pods(b))=>({id:1,w:b.w,h:b.h,rowsBottomFirst:b.rowsBottomFirst,entries:b.entries,capacity:5,pods:ps,tray:[ps.map((_,i)=>i).filter(i=>i%2===0),ps.map((_,i)=>i).filter(i=>i%2===1)],...LV.objectsOf(b)});

test('the calendar and the doses: canal and shutters supported, introductions 101/103 and 121/123, one channel, shutters by class and role',()=>{
 assert.ok(L.SUPPORTED.has('canal')&&L.SUPPORTED.has('shutters'));
 assert.equal(L.slotOf(101).intro,'canal');assert.equal(L.slotOf(103).intro,'canal');assert.equal(L.slotOf(102).allowed.includes('canal'),false);assert.equal(L.slotOf(104).allowed.includes('canal'),true);
 assert.equal(L.slotOf(121).intro,'shutters');assert.equal(L.slotOf(124).allowed.includes('shutters'),true);assert.equal(L.classOf(110),'Super Hard');assert.equal(L.classOf(115),'Hard');
 assert.deepEqual(LV.doses('canal','Normal',24,true).canal,[1,1]);assert.deepEqual(LV.doses('links+canal','Hard',24,false).canal,[1,1]);
 assert.deepEqual(LV.doses('shutters','Normal',24,false).shutters,[1,1]);assert.deepEqual(LV.doses('shutters','Hard',24,false).shutters,[1,2]);assert.deepEqual(LV.doses('shutters','Super Hard',24,false).shutters,[2,3]);
 assert.deepEqual(LV.doses('links+shutters','Super Hard',24,false).shutters,[1,1],'additional: one');assert.deepEqual(LV.doses('shutters','Normal',24,true).shutters,[1,1],'introduction');
 assert.deepEqual(LV.doses('shutters+links+latch','Super Hard',24,false,700).shutters,[2,2],'three types: the lead\'s lower half');
 assert.deepEqual(LV.doseShares('links+shutters','Hard').find(x=>x.m==='shutters'),{m:'shutters',role:'add',count:[1,1]});
 // counters: multiples of 5 in the class range of the material's work, at least 10, the material at least 40
 assert.deepEqual(LV.shutterCounts('Normal',100),[20,25,30,35]);assert.deepEqual(LV.shutterCounts('Hard',41),[15,20]);assert.deepEqual(LV.shutterCounts('Normal',39),[]);
 assert.deepEqual(LV.shutterCounts('Super Hard',50),[20,25,30]);});

test('placeIsland: an island of 25–40 % ringed by water, both exits on it on different sides, the spring outside, routes of two straight legs, the ride',()=>{
 const pc=LV.placeChannel(ISL,null,rnd(5)),again=LV.placeIsland(ISL,null,rnd(5));assert.ok(pc);assert.deepEqual(again,pc,'deterministic by r');
 const {at,overlay,island}=pc;assert.ok(overlay.water.length>0,'the ring is water');assert.ok(island.colour>=LV.ISLAND.share[0]&&island.colour<=LV.ISLAND.share[1],`island ${island.colour}`);
 // the water lies round the object (the ring of the blue-yellow-red block), the approved picture stays as it is
 for(const [x,y] of overlay.water){assert.ok(x>=4&&x<=17&&y>=1&&y<=13);assert.ok(!(x>=5&&x<=16&&y>=2&&y<=12));}
 for(const [i,r] of at.routes.entries()){assert.deepEqual(r[0],at.spring);assert.deepEqual(r.at(-1),at.exits[i]);assert.equal(new Set(r.map(c=>c+'')).size,r.length);
  for(let k=1;k<r.length;k++)assert.equal(Math.abs(r[k][0]-r[k-1][0])+Math.abs(r[k][1]-r[k-1][1]),1);
  const turns=r.slice(2).filter((c,k)=>(c[0]-r[k+1][0])!==(r[k+1][0]-r[k][0])).length;assert.ok(turns<=1,'two straight legs');}
 let b0=L.boardOf(ISL,overlay);const ch=LV.channelOf(b0,at);b0=LV.withObjects(b0,{channel:ch});
 assert.deepEqual(b0.overlay.water,overlay.water,'the board keeps the water');assert.equal(b0.stone.length,0,'water is no stone');
 const g=b0.rowsBottomFirst.join('');assert.equal(g[ch.spring],'.');for(const e of ch.exits){assert.equal(g[e.cell],'.');assert.equal(e.rideUs,Math.max(400,60*(e.route.length-1))*1000);assert.ok(!b0.entries.includes(e.cell));}
 const lv=levelOf(b0);assert.deepEqual(R.validate(lv),[]);assert.deepEqual(R.channelChoices(R.create(lv)),[],'not ready at the start');
 // the island: unreachable without the channel, both exits on it, the spring off it, everything reachable with either exit
 const isl=M.islandOf(lv);assert.ok(isl.share>=.25&&isl.share<=.4,`island ${isl.share}`);assert.deepEqual(isl.each.map(e=>e.on),[true,true]);assert.deepEqual(isl.each.map(e=>e.miss),[0,0]);assert.equal(isl.springOn,false);
 // the exits on different sides of the island
 const [p,q]=at.exits,cx=10.5,cy=7;let da=Math.abs(Math.atan2(p[1]-cy,p[0]-cx)-Math.atan2(q[1]-cy,q[0]-cx));if(da>Math.PI)da=2*Math.PI-da;assert.ok(da*180/Math.PI>=90);
 // the key holes never take the channel's cells; carved key spots keep the water and keep away from the channel
 assert.deepEqual(LV.keyHoles(b0),[]);const ks=LV.carveKeySpots(ISL,overlay,1,rnd(3),{avoid:LV.channelSpots(at)});
 if(ks){assert.deepEqual(ks.water,overlay.water,'the water stays');const kb=LV.withObjects(L.boardOf(ISL,ks),{channel:ch});const holes=LV.keyHoles(kb);assert.equal(holes.length,1);assert.ok(!LV.channelCells(kb).has(holes[0].key));}
 // a picture without depth or without an inner part has no island
 assert.equal(LV.placeIsland({id:'S',title:'s',rows:['GGGG','GGGG','GGGG']},null,rnd(1)),null);
 // a picture of few big areas touching its edges takes a round blob cut through them (scored below an outline)
 const blob=LV.placeIsland(TOY,null,rnd(1));if(blob)assert.ok(blob.island.colour>=.25&&blob.island.colour<=.4);});

test('placeShutters: a full rectangle of colour cells off the start front, nothing closed off, the counter of its material by class',()=>{
 const b=L.boardOf(TOY),g=b.rowsBottomFirst.join('');
 for(const [cls,role,intro] of [['Normal','lead',true],['Hard','lead',false],['Super Hard','add',false]]){
  const sh=LV.placeShutters(b,{n:1,cls,role,intro,r:rnd(7)});assert.ok(sh,`${cls} ${role}`);const [s]=sh,[x,y,w,h]=s.rect;
  assert.equal(s.cells.length,w*h);assert.ok(w>=3&&h>=3&&Math.max(w,h)<=2*Math.min(w,h));assert.ok(s.cells.every(i=>L.CODES.includes(g[i])));
  const W=[...g].filter(c=>c===s.material).length;assert.ok(LV.shutterCounts(intro?'Normal':cls,W).includes(s.count),`${s.count} of ${W}`);
  if(intro||role==='add')assert.equal(s.count,LV.shutterCounts(intro?'Normal':cls,W)[0],'the class\'s lower bound');
  const lv=levelOf(LV.withObjects(b,{shutters:LV.shuttersOf(sh)}));assert.deepEqual(R.validate(lv),[]);
  // (the toy's queue has 8 pods, a small queue: one shutter in any class, 17 §7)
  assert.equal(LV.dosesOk({...lv,recipe:'shutters'},intro?'Normal':cls),null);}
 // two shutters keep apart; the avoid cells are never covered
 const two=LV.placeShutters(b,{n:2,cls:'Hard',role:'lead',r:rnd(9)});if(two){const [p,q]=two.map(s=>s.rect);assert.ok(p[0]+p[2]+2<=q[0]||q[0]+q[2]+2<=p[0]||p[1]+p[3]+2<=q[1]||q[1]+q[3]+2<=p[1]);}
 const first=LV.placeShutters(b,{n:1,cls:'Normal',r:rnd(7)})[0],moved=LV.placeShutters(b,{n:1,cls:'Normal',r:rnd(7),avoid:first.cells});assert.ok(!moved||!moved[0].cells.some(i=>first.cells.includes(i)));
 // an accent over the counted material: the counter is fitted again to that material's recoloured work
 const rc=[];for(let i=0;i<g.length&&rc.length<12;i++)if(g[i]===first.material&&!first.cells.includes(i)){const x=i%b.w-2,y=b.h-1-Math.floor(i/b.w)-2;rc.push([x,y,'W']);}
 const fit=LV.refitShutters(L.boardOf(TOY,null,rc),[first],'Normal'),W2=[...L.boardOf(TOY,null,rc).rowsBottomFirst.join('')].filter(c=>c===first.material).length;
 assert.ok(fit&&LV.shutterCounts('Normal',W2).includes(fit[0].count));assert.deepEqual(fit[0].cells,first.cells);
 // a counter out of its class's range is a dose error
 const lv=levelOf(LV.withObjects(b,{shutters:[{...LV.shuttersOf([first])[0],count:first.count+1}]}));assert.match(LV.dosesOk({...lv,recipe:'shutters'},'Normal'),/счётчик/);});

test('candidate: the board\'s channel and shutters go into the level; the recipe must match them; hidden cells keep off the shutters',()=>{
 const pc=LV.placeChannel(ISL,null,rnd(5));let b=L.boardOf(ISL,pc.overlay);b=LV.withObjects(b,{channel:LV.channelOf(b,pc.at)});
 const sh=LV.placeShutters(b,{n:1,cls:'Normal',r:rnd(2),avoid:[...LV.channelCells(b)]});b=LV.withObjects(b,{shutters:LV.shuttersOf(sh)});
 const base=LV.splitPods(L.measure(L.overlaid(ISL,pc.overlay)).volumes,L.measure(L.overlaid(ISL,pc.overlay)).N,null).pods;
 const c=LV.candidate(ISL,b,base,3,'canal+shutters','Normal',3,false,null,{});assert.ok(c.level,c.error);assert.deepEqual(c.level.channel,b.channel);assert.deepEqual(c.level.shutters,b.shutters);
 assert.deepEqual(R.validate(c.level),[]);
 assert.match(LV.candidate(ISL,b,base,3,'canal','Normal',3,false).error,/створки/);assert.match(LV.candidate(ISL,L.boardOf(ISL),base,3,'canal','Normal',3,false).error,/канал/);
 const hc=LV.candidate(ISL,b,base,3,'shutters+cells','Normal',4,false);if(hc.level&&hc.level.hiddenCells)assert.ok(!hc.level.hiddenCells.some(i=>sh[0].cells.includes(i)));
 // the export carries both (round-level.v1, the schema)
 const out=X.levelV1({level:101,cls:'Normal',picture:'ISL',recipe:'canal+shutters',definition:c.level},{});assert.ok(out.board.channel&&out.board.shutters);assert.deepEqual(out.mechanics,['canal','shutters']);
 assert.deepEqual(out.board.overlay.keys,pc.overlay.keys);assert.deepEqual(out.board.overlay.water,pc.overlay.water,'the export carries the water');});

test('the saved solution shows the exit choice, each exit is solved alone, the shutters open on the solution, the strategies take the exit',async()=>{
 const pc=LV.placeChannel(ISL,null,rnd(5));let b=L.boardOf(ISL,pc.overlay);b=LV.withObjects(b,{channel:LV.channelOf(b,pc.at)});
 const sh=LV.placeShutters(b,{n:1,cls:'Normal',r:rnd(2),avoid:[...LV.channelCells(b)]});b=LV.withObjects(b,{shutters:LV.shuttersOf(sh)});
 const base=LV.splitPods(L.measure(L.overlaid(ISL,pc.overlay)).volumes,L.measure(L.overlaid(ISL,pc.overlay)).N,null).pods,lv=LV.candidate(ISL,b,base,3,'canal+shutters','Normal',3,false).level;
 const sol=await M.solve(lv);assert.equal(sol.win,true);assert.equal(sol.replay,'won');assert.ok(sol.route.some(a=>a.type==='channel'),'the search tries an exit as soon as one may be chosen');
 const ce=await M.channelExits(lv,10);assert.equal(ce.each.length,2);assert.ok(ce.each.every(x=>typeof x.win==='boolean'||x.win===null));assert.ok(ce.each.some(x=>x.win===true));
 const sc=await M.shutterCheck(lv,sol.route);assert.equal(sc.shutters.length,1);assert.ok(sc.shutters[0].atW>0);assert.equal(sc.ok,sc.shutters[0].atW<.8);
 assert.match((await M.shutterCheck(lv,[])).problems[0],/не открылись на сохранённом решении/);
 // the island on the saved solution (17 §8 «Остров за водой»): the spring reached and an exit chosen
 const cc=await M.channelCheck(lv,sol.route);assert.ok(cc.ready&&cc.open,'ready and chosen');assert.ok(cc.island.share>=.25&&cc.island.share<=.4);assert.deepEqual(cc.exitsOn,[true,true]);
 assert.match((await M.channelCheck(lv,[])).problems.join(' '),/выход не выбран/);
 const st=await M.strategies(lv),gr=await M.greedy(lv);assert.equal(Object.keys(st).length>0,true);assert.equal(Object.keys(gr).length,4);
 const f=await M.forcedShare(lv,4);assert.ok(f>=0&&f<=1);
 // the random test plays the exit as one of the uniform actions (17 §5): some games choose each exit
 const {randomGame}=await import('../perf/games.mjs');const E=await engine();let seen=new Set();
 for(let k=0;k<12;k++){const t=new T.Timeline(R.create(lv));const r=rnd(k*7919+1);let g=0;while(g++<20000&&t.state.status!=='Won'){const m=R.legal(t.state).filter(a=>!t.check(a));if(m.length){const a=m[Math.floor(r()*m.length)];if(a.type==='channel')seen.add(a.exit);t.commit(a);}else if(t.pending)t.advance(t.steps[0].at-t.now);else break;}}
 assert.deepEqual([...seen].sort(),[0,1]);assert.equal(typeof randomGame(E,lv,1),'boolean');});

test('the wave plan L101–L150: one entry per position, the calendar\'s class, the introductions, the hand Super Hards with a channel',()=>{
 const p=JSON.parse(fs.readFileSync(path.join(ROOT,'content/rounds/runs/L0101-L0150/mechanics.json'),'utf8'));
 assert.deepEqual(p.wave,[101,150]);assert.ok(p.approved);assert.equal(p.levels.length,50);
 for(const e of p.levels){assert.equal(e.cls,L.classOf(e.level),`L${e.level}`);assert.equal(e.recipe,[e.lead,...e.add].join('+'));assert.ok(L.recipeAllowed(e.recipe),`L${e.level} hidden with cells`);
  const t=e.recipe.split('+'),slot=L.slotOf(e.level);assert.ok(t.every(m=>L.SUPPORTED.has(m)));
  if(slot.intro){assert.equal(e.recipe,slot.intro);assert.deepEqual(e.intro,{mechanic:slot.intro,second:L.INTRO.find(([m])=>m===slot.intro)[2]===e.level});}
  else assert.ok(t.every(m=>slot.allowed.includes(m)),`L${e.level}: ${e.recipe}`);
  if(e.cls==='Normal')assert.ok(t.length<=2||(t.length===3&&t.includes('latch')),`L${e.level}: Normal types`);}
 assert.deepEqual(p.levels.filter(e=>e.by==='hand').map(e=>e.level),[110,130,150]);assert.ok(p.levels.filter(e=>e.by==='hand').every(e=>e.recipe.includes('canal')));
 assert.deepEqual(p.levels.filter(e=>e.breather).map(e=>e.level),[109,119,132,147]);
 const cnt=m=>p.levels.filter(e=>e.recipe.split('+').includes(m)).length;assert.equal(cnt('canal'),15);assert.equal(cnt('shutters'),11);assert.equal(cnt('latch'),12);assert.equal(cnt('cells'),16);
 assert.equal(L.planOf(130).by,'hand');assert.equal(L.planOf(125).recipe,'links+shutters');});

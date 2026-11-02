// node --test tools/rounds/test/ — the generator of 2026-11-02 (README «The generator (2026-11-02)»): the picture-first
// wave (wave.mjs: the size mix spread evenly, packs, pins, Hard on strong pictures, breathers on small ones, the §7 pod
// rule of a two-type recipe), breathers (17 §5 «Передышка»: detection and their mandatory-only checks), the queue
// tuning's steps (tune.mjs nextT) and the forced-aware link chooser (tune.mjs chooseLinks). Reads content/rounds only.
import test from 'node:test';import assert from 'node:assert/strict';
const L=await import('../lib.mjs');const W=await import('../wave.mjs');const M=await import('../measure.mjs');
const {engine}=await import('../engine.mjs');const {R}=await engine();
process.argv.splice(2,0,'noop');const LV=await import('../levels.mjs');const T=await import('../tune.mjs');process.argv.splice(2,1);

test('size mix: 30 / 10 / 5 / 5 per 50, scaled by largest remainders',()=>{
 assert.deepEqual(W.bandCounts(50),{'800–1200':30,'700–900':10,'400–700':5,'1200–1500':5});
 for(const n of [10,20,47,100]){const c=W.bandCounts(n);assert.equal(Object.values(c).reduce((a,b)=>a+b,0),n);for(const b of W.SIZE_MIX)assert.ok(Math.abs(c[b.band]-b.per50*n/50)<1,`${n} ${b.band}`);}
 assert.deepEqual(W.bandsOf(850),['800–1200','700–900']);assert.deepEqual(W.bandsOf(375),['400–700']);assert.deepEqual(W.bandsOf(1300),['1200–1500']);assert.deepEqual(W.bandsOf(1600),[]);});

// a synthetic pool: N spread over 380–1500, ten packs, a family each, colours hidden at the start 0–7
const rnd=T.rnd(7),pool=Array.from({length:400},(_,i)=>{const N=380+Math.floor(rnd()*1120),hidden=Math.floor(rnd()*8);
 return {id:'P'+String(5000+i),title:'t',N,colours:7,pack:'pack'+(i%10),hidden,weak:hidden<=1,holes:i%3,P:N<400?13:24,family:'fam'+i,motifs:[],fits:new Set(Array.from({length:50},(_,k)=>51+k))};});
const recipes=['links','links+cells','cells+links','none','latch+links'],positions=Array.from({length:50},(_,k)=>{const n=51+k,cls=L.classOf(n);const breather=n===59||n===72;return {level:n,cls,intro:false,recipe:breather?'none':cls==='Normal'?recipes[k%recipes.length]:'links+cells',breather,latchNeed:0};});

test('picture assignment: the size mix exactly, spread evenly (no run of three), neighbours from different packs, pins kept',()=>{
 const pinned=new Map([[61,'P5003'],[90,pool.find(p=>p.hidden>=3&&p.N>1200).id]]),r=W.assignPictures(positions,pool,{pinned});
 assert.equal(r.missing.length,0);assert.equal(r.assign.length,50);
 const by=new Map(r.assign.map(x=>[x.level,x])),ids=r.assign.map(x=>x.picture);assert.equal(new Set(ids).size,50,'every picture once');
 assert.equal(by.get(61).picture,'P5003');assert.equal(by.get(90).picture,pinned.get(90));
 const cnt={};for(const x of r.assign){cnt[x.band]=(cnt[x.band]||0)+1;assert.ok(W.bandsOf(x.N).includes(x.band),`L${x.level}: ${x.N} cells in ${x.band}`);}
 assert.deepEqual(cnt,r.quota);
 for(let n=53;n<=100;n++){const a=by.get(n-2).band,b=by.get(n-1).band,c=by.get(n).band;assert.ok(!(a===b&&b===c),`L${n-2}–L${n}: three of ${c} in a row`);}
 // even spread: every band appears in each half of the wave in proportion (±1.5)
 for(const b of W.SIZE_MIX){const first=r.assign.filter(x=>x.level<=75&&x.band===b.band).length;assert.ok(Math.abs(first-r.quota[b.band]/2)<=1.5,`${b.band} first half ${first} of ${r.quota[b.band]}`);}
 const pk=id=>pool.find(p=>p.id===id).pack;for(let n=52;n<=100;n++)assert.notEqual(pk(by.get(n).picture),pk(by.get(n-1).picture),`L${n - 1}/L${n} one pack`);});

test('picture assignment: Hard and Super Hard on strong pictures (more colours hidden first), breathers on the small ones, the §7 pod rule',()=>{
 const r=W.assignPictures(positions,pool),by=new Map(r.assign.map(x=>[x.level,x])),P=id=>pool.find(p=>p.id===id);
 const strongMed=pool.map(p=>p.hidden).sort((a,b)=>a-b)[200];
 for(const q of positions){const p=P(by.get(q.level).picture);
  if(q.cls!=='Normal'){assert.ok(p.hidden>=3,`L${q.level} ${q.cls}: hidden ${p.hidden}`);assert.ok(p.hidden>=strongMed,`L${q.level} prefers more hidden colours`);}
  if(q.breather)assert.ok(p.N<=W.BREATHER_N,`breather L${q.level}: ${p.N} cells`);
  else if(q.cls==='Normal'&&q.recipe.includes('+'))assert.ok(p.N>W.BREATHER_N,`L${q.level}: a two-type Normal on a breather-size picture`);
  assert.ok(W.recipeFitsPods(q.recipe,p.P));}
 // the planner bug of P0341 on L54: a picture whose pods give P < 16 cannot take a two-mechanic recipe
 assert.equal(W.recipeFitsPods('links+cells',13),false);assert.equal(W.recipeFitsPods('cells',13),true);assert.equal(W.recipeFitsPods('none',9),true);assert.equal(W.recipeFitsPods('links+cells',16),true);
 const small={...pool[0],N:375,P:13,hidden:2,weak:false,fits:new Set([54])};assert.equal(W.eligible(small,{level:54,cls:'Normal',intro:false,recipe:'links+cells',breather:false}),false);
 assert.equal(W.eligible(small,{level:54,cls:'Normal',intro:false,recipe:'none',breather:true}),true);
 // a replacement keeps the band and never returns a tried or used picture
 const x=by.get(52),rp=W.replacement(positions[1],x.band,pool,{used:new Set(r.assign.map(y=>y.picture)),tried:[x.picture]});
 assert.ok(rp&&rp.picture!==x.picture&&W.bandsOf(P(rp.picture).N).includes(x.band)&&!r.assign.some(y=>y.picture===rp.picture));});

test('breather detection (17 §5 «Передышка»): a Normal up to ~550 cells or marked by the plan; never Hard, an introduction or L1–3',()=>{
 const q={level:59,cls:'Normal',intro:false};
 assert.equal(W.isBreather(q,400),true);assert.equal(W.isBreather(q,550),true);assert.equal(W.isBreather(q,551),false);assert.equal(W.isBreather({...q,breather:true},1100),true);
 assert.equal(W.isBreather({...q,cls:'Hard'},400),false);assert.equal(W.isBreather({...q,intro:'cells'},400),false);assert.equal(W.isBreather({...q,level:3},300),false);
 assert.equal(LV.BREATHER_N,W.BREATHER_N);assert.deepEqual(T.zoneFor('Normal','breather').slice(0,2),[.75,.95]);});

test('breather checks: the mandatory ones only (no calm, opening, holding pod), one mechanic at most, no trap link',async()=>{
 const pic=L.picture('P0079'),m=L.measure(pic),board=L.boardOf(pic),base=LV.splitPods(m.volumes,m.N,null);
 const c=LV.candidate(pic,board,base.pods,3,'none','Normal',11,false,null,{t:.3,noHold:true});assert.ok(c.level);
 const e=await LV.evaluate(c.level,{m,cls:'Normal',breather:true});
 if(!e.reason){assert.equal(e.checks.breather,true);assert.equal(e.checks.calm,undefined);assert.equal(e.checks.opening,null);assert.equal(e.checks.hold,null);assert.equal(e.random.cls,'Normal');assert.ok(e.solution.route.length);
  assert.deepEqual(T.slotProblems(e,c.level,'Normal','breather',pic),[]);
  assert.deepEqual(T.slotProblems({...e,checks:{...e.checks,traps:{links:1,traps:1}}},{...c.level,recipe:'links'},'Normal','breather',pic),['передышка: связка-ловушка']);}
 else assert.match(e.reason,/класс|50 %/);
 const two=await LV.evaluate({...c.level,recipe:'links+cells'},{m,cls:'Normal',breather:true});assert.match(two.reason,/не больше одной механики/);});

test('tuning steps: Newton with the model slope until both sides were seen, then bisection inside the bracket',()=>{
 const n1=T.nextT({t:.3,q:.95,aim:.58,slope:-4,easy:null,hard:null});assert.ok(n1>.3&&n1<=.65,`newton ${n1}`);
 const b=T.nextT({t:.8,q:.1,aim:.58,slope:-4,easy:{t:.4,q:.9},hard:{t:.8,q:.1}});assert.ok(b>.4&&b<.8,`bisection ${b}`);
 assert.ok(T.nextT({t:.5,q:.2,aim:.58,slope:2,easy:null,hard:null})<.5,'a positive model slope is treated as falling');
 const f=T.loadModel();assert.ok(Array.isArray(f.beta)&&f.names.includes('t'));});

test('forced-aware link chooser: pairs at one depth in two columns, the dose, a trap first on Hard, forced ≤ 35 %, no hidden pod',async()=>{
 const pic=L.picture('P0166'),m=L.measure(pic),board=L.boardOf(pic),base=LV.splitPods(m.volumes,m.N,null);
 const c=LV.candidate(pic,board,base.pods,4,'links+hidden','Hard',21,false,.5,{tStrong:.5,noLinks:true,noInfo:true});
 assert.ok(c.level&&!c.level.pods.some(p=>p.group||p.mystery),'noLinks / noInfo leave the queue bare');
 const dz=LV.doses('links+hidden','Hard',c.level.pods.length,false,m.N),want=dz.links[0];
 const ch=await T.chooseLinks(c.level,want,'Hard',T.rnd(5));
 if(ch.error){assert.match(ch.error,/вынужденн/);return;}
 const lv=ch.level,groups=Object.values(Object.groupBy(lv.pods.map((p,i)=>({...p,i})).filter(p=>p.group),p=>p.group));
 assert.equal(groups.length*2,want);assert.deepEqual(R.validate(lv),[]);
 const where=i=>{for(let k=0;k<lv.tray.length;k++){const r=lv.tray[k].indexOf(i);if(r>=0)return [k,r];}};
 for(const g of groups){assert.equal(g.length,2);const [a,b]=g.map(p=>where(p.i));assert.notEqual(a[0],b[0],'two columns');assert.equal(a[1],b[1],'one depth');assert.ok(!g.some(p=>p.mystery));}
 const {minD}=T.podKeys(lv),trapAny=lv.tray.some((st,c1)=>lv.tray.some((st2,c2)=>c2>c1&&st.some((p,r)=>st2[r]!==undefined&&((minD[lv.pods[p].color]??0)>0||(minD[lv.pods[st2[r]].color]??0)>0))));
 if(trapAny)assert.ok(ch.pairs.some(x=>x.trap),'a Hard takes a trap link when one exists (17 §8)');
 assert.ok(ch.forced<=.35);assert.equal(await M.forcedShare(lv),ch.forced,'the confirmed forced share is evaluate\'s');
 // the limit is respected: an impossible limit refuses instead of overshooting
 const none=await T.chooseLinks(c.level,want,'Hard',T.rnd(5),{limit:0});assert.ok(none.error);
 // the information overlays come after: hidden pods by the dose, never in a group, from the second row
 const info=T.addInfo(lv,'links+hidden','Hard',{r:T.rnd(3)});assert.ok(info.level);const hid=info.level.pods.map((p,i)=>p.mystery?i:-1).filter(i=>i>=0);
 assert.ok(hid.length>=dz.hidden[0]&&hid.length<=dz.hidden[1]);for(const i of hid){assert.ok(!info.level.pods[i].group);assert.ok(where(i)[1]>=1);}});

test('columns blocked at once: a pair waits from its first member on top until the partner comes',()=>{
 const top=[[0,2,4,6],[0,3,6,9],[0,1,2,3]];
 assert.equal(T.maxBlocked([{c:0,c2:1,row:1}],top,3),1);
 assert.equal(T.maxBlocked([{c:0,c2:1,row:1},{c:1,c2:2,row:2}],top,3),2);
 assert.equal(T.maxBlocked([{c:0,c2:1,row:1},{c:2,c2:1,row:3}],top,3),1);
 assert.equal(T.maxBlocked([{c:0,c2:1,row:2},{c:2,c2:1,row:3}],top,3),2);
 assert.equal(T.maxBlocked([{c:0,c2:2,row:1}],[[0,5],[0,5],[0,5]],3),0);});

// node --test tools/rounds/test/ — bridge scenes (tools/rounds/bridge-scene.mjs, 17 §8, the owner, 2026-10-28): a
// smaller approved picture on the far side behind a wall with two Sprig sites, strips behind the sites and beds on the near
// bank, so that exactly one bridge wins. Composition (closed far side, anchors, sides, sizes), strips laid over a drawn
// strip picture, the per-bridge solver verdicts (a fresh solver memo for each bridge), the placement by the plan's
// bridgeKind and the plan's scene order. Writes only to temporary copies of content/rounds.
import test from 'node:test';import assert from 'node:assert/strict';
import fs from 'node:fs';import os from 'node:os';import path from 'node:path';import {spawnSync} from 'node:child_process';import {fileURLToPath,pathToFileURL} from 'node:url';

const HERE=path.dirname(fileURLToPath(import.meta.url)),TOOLS=path.dirname(HERE),ROOT=path.resolve(TOOLS,'../..');
const L=await import('../lib.mjs');
process.argv.splice(2,0,'noop');const LV=await import('../levels.mjs');const BS=await import('../bridge-scene.mjs');process.argv.splice(2,1);
const M=await import('../measure.mjs');const {engine}=await import('../engine.mjs');const {solver}=await import('../../level-stats/solve.mjs');
const readJSON=f=>JSON.parse(fs.readFileSync(f,'utf8'));
const grid=b=>b.rowsBottomFirst.slice().reverse();
// cells reachable from the entry through non-wall cells (colour cells pass), with the given bridge cells open
function reach(b,open=[]){const {w,h}=b,g=b.rowsBottomFirst.join(''),seen=new Set(b.entries),q=[...b.entries];
 while(q.length){const i=q.pop(),x=i%w,y=Math.floor(i/w);for(const [dx,dy] of [[1,0],[-1,0],[0,1],[0,-1]]){const u=x+dx,v=y+dy,j=v*w+u;if(u<0||v<0||u>=w||v>=h||seen.has(j)||(g[j]==='#'&&!open.includes(j)))continue;seen.add(j);q.push(j);}}return seen;}
const P=L.picture('P0012');
// the scene the proof found for "up" (Normal, single); the queue below is the one the test checks (its D strip touches
// the teapot's D base, so the geometry tests skip the strip contrast check, tested on its own below)
const UP={x:'K',y:'S',w:'C',v:'D',z:'.',strip:2,divider:1,ground:'L',fill:true,margin:2,bedDepth:1,bedBreadth:3,bedDepthY:3,bedBreadthY:5};

test('scene composition: every side gives a board with two sites on that side, a shut far side and colour anchors',()=>{
 for(const side of BS.SIDES)for(const z of ['.','L']){const sc=BS.scene(P,{...UP,side,z,ground:'L'}),ck=BS.checkScene(sc,null,{contrast:false});
  assert.ok(ck.ok,`${side} z=${z}: ${ck.issues.join('; ')}`);const b=ck.board,g=b.rowsBottomFirst.join('');
  assert.equal(b.hero.choices.length,2);assert.equal(L.bridgeSide(b),side);
  assert.equal(b.entries[0],Math.floor(b.w/2),'entry in the bottom middle');
  for(const c of b.hero.choices){assert.ok(L.CODES.includes(g[c.anchor]),`${side}: anchor is a colour cell`);assert.equal(g[c.cell],'#');}
  // nothing beyond the wall is reachable through any cells while both sites are walls; each bridge alone opens every cell
  const r0=reach(b);for(const c of b.hero.choices)assert.ok(!r0.has(2*c.cell-c.anchor),`${side}: far side shut`);
  for(const c of b.hero.choices){const r=reach(b,[c.cell]);for(let i=0;i<g.length;i++)if(L.CODES.includes(g[i]))assert.ok(r.has(i),`${side}: cell ${i} cut off`);}
  // the far side's edges facing the ring are walls, so the ring next to them is closed outward
  const top=grid(b);if(side==='up'||side==='two'||side==='left')assert.equal(top[1][2],'#','ring closed above the far corner');
  // the winning site is the x bed's: its anchor is x, the cell behind it the y strip
  const win=b.hero.choices[BS.choiceOf(sc,b,sc.win)],lose=b.hero.choices[BS.choiceOf(sc,b,sc.lose)];
  assert.equal(g[win.anchor],'K');assert.equal(g[2*win.cell-win.anchor],'S');assert.equal(g[lose.anchor],'C');assert.equal(g[2*lose.cell-lose.anchor],'D');}
 // crossed (the old pilots): without w and v the other site's bed is y and its strip x
 const cr=BS.scene(P,{side:'up',x:'K',y:'S',z:'.'}),b=L.boardOf(cr),g=b.rowsBottomFirst.join(''),lose=b.hero.choices[BS.choiceOf(cr,b,cr.lose)];
 assert.equal(g[lose.anchor],'S');assert.equal(g[2*lose.cell-lose.anchor],'K');
 // right is left mirrored, the picture itself not: its first row reads the same in the far box
 const rt=BS.scene(P,{...UP,side:'right'});assert.ok(rt.rows.some(r=>r.includes(P.rows[1].replace(/^\.+|\.+$/g,''))));});

test('scene sizes: the proof scene fits the stage of L26 with at most 9 colours, the environment fill and crop',()=>{
 const sc=BS.scene(P,{...UP,side:'up'}),ck=BS.checkScene(sc,26,{contrast:false});assert.ok(ck.ok,ck.issues.join('; '));
 assert.ok(ck.measure.N>=550&&ck.measure.N<=1200&&ck.measure.colours<=9);
 // a filled environment: the outer empty space takes z, enclosed holes stay empty, the crop drops empty rows
 const far=BS.farPicture({rows:['.....','.RRR.','.R.R.','.RRR.','.....']},{z:'S'});assert.deepEqual(far.map(r=>r.join('')),['RRR','R.R','RRR']);
 const pad=BS.farPicture({rows:['....','.RR.','....']},{z:'S',pad:1});assert.deepEqual(pad.map(r=>r.join('')),['SSSS','SRRS','SSSS']);
 assert.throws(()=>BS.scene(P,{...UP,side:'up',x:'K',y:'K'}),/x ≠ y/);
 assert.throws(()=>BS.farPicture({rows:['#b','RR']},{z:'S'}),/полоса/);});

test('strips over a drawn strip picture (batch-0120): each site gets its own strip, a divider between them',async()=>{
 const mod=(await import(pathToFileURL(path.join(ROOT,'content/rounds/pictures/src/batch-0120.mjs')).href)).default;
 for(const [k,a,b] of [[0,'R','D'],[1,'A','D'],[2,'G','D'],[3,'K','S'],[4,'R','E'],[5,'B','V']]){const p=mod.pictures[k],sc=BS.stripsOver(p,{a,b,depth:2}),ck=BS.checkScene(sc,null,{maxColours:11});
  assert.ok(ck.ok,`${p.title}: ${ck.issues.join('; ')}`);const bd=ck.board,g=bd.rowsBottomFirst.join(''),cs=bd.hero.choices;
  assert.equal(g[2*cs[0].cell-cs[0].anchor],a,`${p.title}: strip a behind the first site`);assert.equal(g[2*cs[1].cell-cs[1].anchor],b,`${p.title}: strip b behind the second`);
  assert.equal(L.bridgeSide(bd),L.bridgeSide(L.boardOf(p)),`${p.title}: same side`);}});

test('per-bridge verdicts use a fresh solver each: a scene where exactly one bridge wins measures [true, false]',async()=>{
 const sc=BS.scene(P,{...UP,side:'up'}),b=L.boardOf(sc),m=L.measure(sc),base=LV.splitPods(m.volumes,m.N,null);
 const c=LV.candidate({id:'P0012',title:'',rows:sc.rows,locked:null},b,base.pods,L.columnsFor(m.N)[0],'bridge','Normal',1000,false,.5,{gate:6,delay:2,intended:BS.choiceOf(sc,b,sc.win)});
 assert.ok(c.level,c.error);const w=(await M.bridgeChoicesWin(c.level)).each;assert.deepEqual(w,[true,false]);
 // the old way (one memo for both restricted levels) answered the second bridge from the first
 const E=await engine(),S=solver(E,200000),old=c.level.hero.choices.map((x,i)=>S.winnable(E.idle.create({...c.level,hero:{...c.level.hero,choices:[x]}},5)));
 assert.deepEqual(old,[true,true]);
 // the gate: every pod of the losing bridge's strip colour (D, no D on the near bank) stands at least 6 deep
 const lose=b.hero.choices[BS.choiceOf(sc,b,sc.lose)],gc=b.rowsBottomFirst.join('')[2*lose.cell-lose.anchor];
 for(const st of c.level.tray)st.forEach((p,k)=>{if(c.level.pods[p].color===gc)assert.ok(k>=6,`gate pod at row ${k}`);});});

test('placement by the plan\'s bridgeKind: single wants exactly one winning bridge, better both winning and 10+ points apart',()=>{
 const slot=L.slotOf(29),c=k=>({recipe:'bridge',checks:{bridgeKind:k,bridgeLoss:{ok:true}}});
 assert.ok(LV.mechOk(c('single'),29,slot,{bridgeKind:'single'}));assert.ok(!LV.mechOk(c('better'),29,slot,{bridgeKind:'single'}));
 assert.ok(LV.mechOk(c('better'),29,slot,{bridgeKind:'better'}));assert.ok(!LV.mechOk(c('single'),29,slot,{bridgeKind:'better'}));
 assert.ok(LV.mechOk(c('single'),29,slot,{bridgeKind:'any'})&&LV.mechOk(c('better'),29,slot,{bridgeKind:'any'}),'any: either');
 assert.ok(!LV.mechOk({recipe:'bridge',checks:{bridgeKind:null}},29,slot,{bridgeKind:'any'}));
 assert.ok(LV.mechOk(c('single'),29,slot,null),'no plan: single');assert.ok(!LV.mechOk(c('better'),29,slot,null));
 assert.ok(!LV.mechOk({recipe:'bridge',checks:{bridgeKind:'single',bridgeLoss:{ok:false}}},29,slot,{bridgeKind:'single'}),'the loss comes at the choice');
 assert.ok(LV.mechOk({recipe:'bridge',cls:'Super Hard',checks:{bridgeKind:'single',bridgeLoss:{ok:false}}},29,slot,{bridgeKind:'single'}),'Super Hard may lose at the choice (2026-10-29)');
 // a scene picture's record carries scene.win: the builder leans to that site and keeps the gate deep
 const sc=BS.scene(P,{...UP,side:'left'}),b=L.boardOf(sc),o=LV.sceneOpts({scene:{win:sc.win}},b,24,3,0);
 assert.equal(o.intended,BS.choiceOf(sc,b,sc.win));assert.equal(o.gate,7);assert.deepEqual(LV.sceneOpts({},b,24,3,0),{});});

test('the wave plan lists the bridge scenes to make for the new-strip positions, with the command',()=>{
 const dir=fs.mkdtempSync(path.join(os.tmpdir(),'rounds-scenes-'));fs.cpSync(path.join(ROOT,'content/rounds'),dir,{recursive:true,filter:s=>!s.includes(`${path.sep}approved${path.sep}`)&&!s.includes(`${path.sep}bank${path.sep}`)});
 const r=spawnSync(process.execPath,[path.join(TOOLS,'rounds.mjs'),'plan','21','50'],{cwd:ROOT,env:{...process.env,ROUNDS_DIR:dir},encoding:'utf8',maxBuffer:1<<26});assert.equal(r.status,0,r.stderr);
 const plan=readJSON(path.join(dir,'runs/L0021-L0050/plan.json')),mech=readJSON(path.join(dir,'runs/L0021-L0050/mechanics.json')).levels.filter(l=>l.bridgeSide&&l.strip==='new');
 assert.deepEqual(plan.order.scenes.map(s=>s.level),mech.map(l=>l.level));
 for(const s of plan.order.scenes){const m=mech.find(l=>l.level===s.level);assert.equal(s.side,m.bridgeSide);assert.equal(s.kind,m.bridgeKind);assert.match(s.command,new RegExp(`--side ${m.bridgeSide} --kind ${m.bridgeKind}`));}
 assert.match(fs.readFileSync(path.join(dir,'runs/L0021-L0050/plan.md'),'utf8'),/Сцены моста/);});

test('search: the up proof scene passes the builder with exactly one winning bridge (slow, ROUNDS_SLOW=1)',{skip:!process.env.ROUNDS_SLOW},async()=>{
 const r=await BS.search(P,{side:'up',kind:'single',cls:'Normal',recipes:['bridge'],level:26,scenes:1,candidates:8,seed:7,opts:UP});
 assert.ok(r&&!r.fail,JSON.stringify(r&&r.tried));assert.equal(r.eval.checks.bridgeKind,'single');assert.equal(r.eval.random.cls,'Normal');assert.ok(r.eval.checks.bridgeLoss.ok);});

test('strip contrast: a strip of the colour of the picture cells it touches is refused, a contrasting one passes (2026-10-28)',()=>{
 const bad=BS.checkScene(BS.scene(P,{...UP,side:'up'}),null);
 assert.ok(bad.issues.some(i=>i.includes('сливается')),'the D strip under the teapot\'s D base merges');
 const good=BS.checkScene(BS.scene(P,{...UP,side:'up',w:'C',v:'K',x:'Y'}),null);
 assert.ok(!good.issues.some(i=>i.includes('сливается')),good.issues.join('; '));});

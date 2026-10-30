// node --test tools/rounds/test/ — Sprig bridges on any strip (17 §8 "Где стоит мост", amendment of 2026-10-28): the
// board of a picture with or without the generator's wall (boardOf: ring closure, anchors, crossing sides), the engine's
// anchor rule, wallOverlay, the doses of two- and three-type recipes and the wave plan's placement filter. Writes only to
// temporary copies of content/rounds.
import test from 'node:test';import assert from 'node:assert/strict';
import fs from 'node:fs';import os from 'node:os';import path from 'node:path';import {spawnSync} from 'node:child_process';import {fileURLToPath} from 'node:url';

const HERE=path.dirname(fileURLToPath(import.meta.url)),TOOLS=path.dirname(HERE),ROOT=path.resolve(TOOLS,'../..');
const L=await import('../lib.mjs');
const {engine}=await import('../engine.mjs');const {R}=await engine();
process.argv.splice(2,0,'noop');const LV=await import('../levels.mjs');process.argv.splice(2,1);
const {validate}=await import('../schema.mjs');const X=await import('../export.mjs');
const readJSON=f=>JSON.parse(fs.readFileSync(f,'utf8'));
const placed=fs.readdirSync(path.join(ROOT,'content/rounds/levels')).filter(f=>/^L\d+\.json$/.test(f)).map(f=>readJSON(path.join(ROOT,'content/rounds/levels',f)));
// a picture of rows (top first)
const pic=rows=>({id:'T',title:'test',rows,w:rows[0].length,h:rows.length});
const grid=b=>b.rowsBottomFirst.slice().reverse();// top first, board coordinates
const at=(b,x,y)=>grid(b)[y][x];
// cells reachable from the entry through non-wall cells (colour cells pass), optionally with the bridges built
function reach(b,built=[]){const {w,h}=b,g=b.rowsBottomFirst.join(''),open=new Set(built),seen=new Set(b.entries),q=[...b.entries];
 while(q.length){const i=q.pop(),x=i%w,y=Math.floor(i/w);for(const [dx,dy] of [[1,0],[-1,0],[0,1],[0,-1]]){const u=x+dx,v=y+dy,j=v*w+u;if(u<0||v<0||u>=w||v>=h||seen.has(j)||(g[j]==='#'&&!open.has(j)))continue;seen.add(j);q.push(j);}}return seen;}
const idx=(b,x,y)=>(b.h-1-y)*b.w+x;// board top-first (x, y) → bottom-first index

test('boardOf keeps the boards of every placed level byte-identical, and the bridge cells and anchors of L16 and L18',()=>{
 for(const lv of placed){const b=L.boardOf(L.picture(lv.picture),lv.definition.overlay);
  assert.equal(b.rowsBottomFirst.join('\n'),lv.definition.rowsBottomFirst.join('\n'),`L${lv.level}`);assert.deepEqual(b.entries,lv.definition.entries);
  if(lv.definition.hero)assert.deepEqual(b.hero.choices.map(c=>[c.cell,c.anchor,c.dir]),lv.definition.hero.choices.map(c=>[c.cell,c.anchor,'up']),`L${lv.level} bridges`);}
 // every approved picture with a strip: the full-width stream closes the ring on both sides, as before
 // (bridge scenes of the scene generator carry their own crossing side and are checked in scenes.test.mjs)
 for(const e of L.manifest().pictures){const p=L.picture(e.id);if(!/[#b]/.test(p.rows.join(''))||p.scene)continue;const b=L.boardOf(p);
  p.rows.forEach((r,y)=>{if(/^[#b]+$/.test(r)){assert.equal(at(b,1,y+2),'#');assert.equal(at(b,b.w-2,y+2),'#');}});
  assert.equal(L.bridgeSide(b),'up');}});

test('ring closure: a vertical wall closes the ring at the top and the bottom, an L-shaped one at its two edge ends',()=>{
 const rows=Array.from({length:9},()=>'G'.repeat(9));
 // vertical wall at x = 2, left part closed, two sites crossing left
 const v=L.boardOf(pic(rows),{walls:Array.from({length:9},(_,y)=>[2,y]),sites:[[2,2],[2,6]]});
 assert.equal(at(v,4,1),'#');assert.equal(at(v,4,v.h-2),'#');assert.equal(at(v,1,5),'.');
 const r0=reach(v);assert.ok(!r0.has(idx(v,3,5))&&!r0.has(idx(v,1,1)),'left part closed while both sites are walls');
 const r1=reach(v,[v.hero.choices[0].cell]);assert.ok(r1.has(idx(v,3,5))&&r1.has(idx(v,1,1)),'one bridge opens the whole closed part');
 // L-shaped: horizontal arm y = 4 for x 0..5, vertical arm x = 5 for y 0..4 closes the top-left corner
 const walls=[...Array.from({length:6},(_,x)=>[x,4]),...Array.from({length:4},(_,y)=>[5,y])];
 const l=L.boardOf(pic(rows),{walls,sites:[[2,4],[5,1]]});
 assert.equal(at(l,1,6),'#','left ring closed beside the horizontal arm');assert.equal(at(l,7,1),'#','top ring closed above the vertical arm');
 assert.equal(at(l,1,1),'.');assert.equal(at(l,9,1),'.');
 const rl=reach(l);assert.ok(!rl.has(idx(l,3,3))&&!rl.has(idx(l,1,1)),'corner closed');assert.ok(rl.has(idx(l,9,1)));
 assert.deepEqual(l.hero.choices.map(c=>c.dir).sort(),['left','up']);assert.equal(L.bridgeSide(l),'two');
 // stone: the overlay's walls and the ring cells they close
 assert.ok(l.stone.includes(idx(l,1,6))&&l.stone.includes(idx(l,7,1))&&l.stone.includes(idx(l,4,6)));});

test('anchors: up, left and right crossings, the engine accepts them, and an unclear site is a named error',()=>{
 const rows=Array.from({length:9},()=>'G'.repeat(9));
 const up=L.boardOf(pic(rows),{walls:Array.from({length:9},(_,x)=>[x,3]),sites:[[1,3],[6,3]]});
 for(const c of up.hero.choices){assert.equal(c.dir,'up');assert.equal(c.anchor,c.cell-up.w);}
 assert.match(up.hero.choices[0].label,/^Мостик вверх слева$/);assert.match(up.hero.choices[1].label,/^Мостик вверх справа$/);
 const left=L.boardOf(pic(rows),{walls:Array.from({length:9},(_,y)=>[2,y]),sites:[[2,1],[2,7]]});
 for(const c of left.hero.choices){assert.equal(c.dir,'left');assert.equal(c.anchor,c.cell+1);}
 assert.deepEqual(left.hero.choices.map(c=>c.label),['Мостик влево сверху','Мостик влево снизу']);
 const right=L.boardOf(pic(rows),{walls:Array.from({length:9},(_,y)=>[6,y]),sites:[[6,1],[6,7]]});
 for(const c of right.hero.choices){assert.equal(c.dir,'right');assert.equal(c.anchor,c.cell-1);assert.equal(L.bridgeDir({cell:c.cell,anchor:c.anchor},right.w),'right');}
 assert.equal(L.bridgeSide(right),'right');
 // two parallel streams, one site on each: the upper site cannot be reached while the lower one is a wall (no anchor)
 assert.throws(()=>L.boardOf(pic(rows),{walls:[...Array.from({length:9},(_,x)=>[x,2]),...Array.from({length:9},(_,x)=>[x,5])],sites:[[1,2],[6,5]]}),/опора не определена — достижимых с входа соседей 0/);
 // the engine: an orthogonal anchor is valid, a diagonal one or one across a row end is not
 const level=b=>({id:1,w:b.w,h:b.h,rowsBottomFirst:b.rowsBottomFirst,entries:b.entries,capacity:5,pods:[{color:'G',count:1},{color:'G',count:1}],tray:[[0],[1]],hero:b.hero});
 for(const b of [up,left,right])assert.ok(!R.validate(level(b)).includes('bridge'));
 const c=left.hero.choices[0];assert.ok(R.validate(level({...left,hero:{id:'sprig-bridge',choices:[{...c,anchor:c.cell+left.w+1}]}})).includes('bridge'),'diagonal');
 const edge=left.w*5;// a row start (the frame wall): cell − 1 is the previous row's end
 assert.ok(R.validate(level({...left,hero:{id:'sprig-bridge',choices:[{cell:edge,anchor:edge-1,label:'x'}]}})).includes('bridge'),'across a row end');
 // a site whose two sides are both reachable has no anchor
 assert.throws(()=>L.boardOf(pic(['GGGGG','GGbGG','GGGGG','GGGGG','GGGGG','GGGGG','GGGGG'])),e=>e.name==='BridgeAnchorError'&&e instanceof L.BridgeAnchorError);
 assert.throws(()=>L.boardOf(pic(rows),{walls:[],sites:[[1,1],[2,2]]}),/не на стене/);});

test('wallOverlay: deterministic by seed, a straight or L wall with the closed part 25–45 %, sites with colour approaches',()=>{
 const p=L.picture('P0012');
 for(const side of ['up','left','right','two']){const a=L.wallOverlay(p,side,1),b=L.wallOverlay(p,side,1);assert.ok(a,side);assert.deepEqual(a,b,`${side} deterministic`);
  const board=L.boardOf(p,a),op=L.overlaid(p,a),pw=p.rows[0].length,ph=p.rows.length,N=L.measure(op).N;
  assert.equal(L.bridgeSide(board),side);assert.equal(board.hero.choices.length,2);
  const walls=new Set(a.walls.map(c=>c.join(',')));for(const s of a.sites)assert.ok(walls.has(s.join(',')),'site on the wall');
  if(side==='up')assert.ok(new Set(a.walls.map(c=>c[1])).size===1&&a.walls.length===pw);
  if(side==='left'||side==='right')assert.ok(new Set(a.walls.map(c=>c[0])).size===1&&a.walls.length===ph);
  // the share closed behind the wall, counted on the board with the wall
  const r=reach(board),g=board.rowsBottomFirst.join('');let closed=0;for(let i=0;i<g.length;i++)if(L.CODES.includes(g[i])&&!r.has(i))closed++;
  assert.ok(closed/N>=.25&&closed/N<=.45,`${side} share ${closed/N}`);
  for(const c of board.hero.choices){assert.ok(L.CODES.includes(g[c.anchor]),'colour approach');const far=2*c.cell-c.anchor;assert.notEqual(g[far],'#','far cell not a wall');}
  const [s1,s2]=a.sites;assert.ok(Math.abs(s1[0]-s2[0])+Math.abs(s1[1]-s2[1])>=3);
  // §8: the bridge is not reachable at the start
  const lv={id:1,w:board.w,h:board.h,rowsBottomFirst:board.rowsBottomFirst,entries:board.entries,capacity:5,pods:[],tray:[],hero:board.hero};
  assert.deepEqual(R.bridgeChoices(R.create({...lv,pods:[{color:'G',count:1}],tray:[[0]]})),[]);}
 assert.equal(L.wallOverlay(L.picture('P0114'),'up',1),null,'a picture with its own sites');});

test('doses: one additional at its minimum (links an even count), three types the lead in its lower half; a bridge takes no part',()=>{
 const d=(r,c)=>LV.doses(r,c,24,false);
 assert.deepEqual(d('hidden+links','Hard'),{hidden:[3,6],links:[4,4]},'10 % of 24 = 2.4 → 3 → 4 participants');
 assert.deepEqual(d('links+hidden','Super Hard'),{links:[10,14],hidden:[2,2]});
 // the difficulty is the pods' (17 §7, the owner, 2026-10-30): a bridge recipe has the doses of the recipe without the
 // bridge, links as the lead, hidden never the lead
 assert.deepEqual(d('bridge+links','Normal'),{bridge:1,links:[4,6]});
 assert.deepEqual(d('links+bridge','Hard'),{links:[6,10],bridge:1});
 assert.deepEqual(d('links+bridge+hidden','Super Hard'),{links:[10,14],bridge:1,hidden:[2,2]});
 assert.deepEqual(d('bridge+links+hidden','Super Hard'),{bridge:1,links:[10,14],hidden:[2,2]});
 assert.deepEqual(d('bridge+hidden','Normal'),{bridge:1,hidden:[2,2]});
 assert.deepEqual(LV.doses('bridge+hidden','Normal',30,false),{bridge:1,hidden:[2,3]},'alone after a bridge: its additional range');
 assert.deepEqual(LV.doseShares('links+bridge+hidden','Super Hard').map(x=>x.range||x.choice),[[.35,.6],1,[.05,.05]]);
 assert.deepEqual(LV.doseShares('bridge+hidden','Normal').map(x=>x.range||x.choice),[1,[.05,.1]]);
 // every recipe of the plan builds a candidate (overlay board for the bridge ones) with doses inside its range
 const p=L.picture('P0012'),ov=L.wallOverlay(p,'up',1),m=L.measure(L.overlaid(p,ov)),m0=L.measure(p);
 for(const recipe of ['none','links','hidden','bridge','links+hidden','hidden+links','bridge+links','bridge+hidden','hidden+bridge','links+bridge','links+bridge+hidden','bridge+links+hidden']){
  const bridge=recipe.includes('bridge'),board=bridge?L.boardOf(p,ov):L.boardOf(p),mm=bridge?m:m0,base=LV.splitPods(mm.volumes,mm.N,null),cls=recipe.split('+').length===3?'Super Hard':'Normal';
  let c=null;for(let k=0;k<20&&!(c&&c.level);k++)c=LV.candidate(p,board,base.pods,3,recipe,cls,1000+k,false,.5);
  assert.ok(c.level,`${recipe}: ${c.error}`);assert.deepEqual(R.validate(c.level),[],recipe);
  assert.equal(!!c.level.hero,bridge);assert.equal(!!c.level.overlay,bridge);
  const dz=LV.doses(recipe,cls,c.level.pods.length,false),linked=c.level.pods.filter(x=>x.group).length,hidden=c.level.pods.filter(x=>x.mystery).length;
  if(dz.links)assert.ok(linked>=dz.links[0]&&linked<=dz.links[1],`${recipe} links ${linked}`);else assert.equal(linked,0);
  if(dz.hidden)assert.ok(hidden>=dz.hidden[0]&&hidden<=dz.hidden[1],`${recipe} hidden ${hidden}`);else assert.equal(hidden,0);}
 // a board with a strip refuses a recipe without the bridge
 assert.match(LV.candidate(p,L.boardOf(p,ov),LV.splitPods(m.volumes,m.N,null).pods,3,'links','Normal',1,false).error,/нужен рецепт с мостом/);});

test('the mechanics plan: planOf reads the approved mechanics.json, fitsPlan wants its recipe and bridge side',()=>{
 assert.deepEqual(L.planOf(33),{level:33,cls:'Normal',lead:'bridge',add:['hidden'],recipe:'bridge+hidden',bridgeSide:'left',strip:'gap',bridgeKind:'better'});
 assert.equal(L.planOf(20),null);
 const p=L.picture('P0012'),lvl=side=>{const ov=L.wallOverlay(p,side,1),b=L.boardOf(p,ov);return {w:b.w,h:b.h,rowsBottomFirst:b.rowsBottomFirst,hero:b.hero};};
 const plan={recipe:'bridge+hidden',bridgeSide:'left'};
 assert.ok(L.fitsPlan({recipe:'bridge+hidden',level:lvl('left')},plan));
 assert.ok(!L.fitsPlan({recipe:'bridge+hidden',level:lvl('right')},plan),'other side');
 assert.ok(!L.fitsPlan({recipe:'hidden+bridge',level:lvl('left')},plan),'other lead');
 assert.ok(L.fitsPlan({recipe:'links+bridge+hidden',level:lvl('two')},{recipe:'links+bridge+hidden',bridgeSide:'two'}));
 assert.ok(L.fitsPlan({recipe:'links'},{recipe:'links'}));assert.ok(L.fitsPlan({recipe:'links'},null));});

test('placement under the plan takes only the planned recipe, and the open positions say what they need',()=>{
 const dir=fs.mkdtempSync(path.join(os.tmpdir(),'rounds-plan-'));fs.cpSync(path.join(ROOT,'content/rounds'),dir,{recursive:true,filter:s=>!s.includes(`${path.sep}approved${path.sep}`)});
 const r=spawnSync(process.execPath,[path.join(TOOLS,'levels.mjs'),'place','21','30'],{cwd:ROOT,env:{...process.env,ROUNDS_DIR:dir},encoding:'utf8',maxBuffer:1<<26});assert.equal(r.status,0,r.stderr);
 const plans=readJSON(path.join(dir,'runs/L0021-L0050/mechanics.json')).levels;
 for(let n=21;n<=30;n++){const f=path.join(dir,'levels',`L${String(n).padStart(4,'0')}.json`),want=plans.find(x=>x.level===n);
  if(fs.existsSync(f)){const lv=readJSON(f);assert.equal(lv.recipe,want.recipe,`L${n}`);if(want.bridgeSide)assert.equal(L.bridgeSide(lv.definition),want.bridgeSide);}
  else{const need=readJSON(path.join(dir,'needs.json')).needs.find(x=>x.level===n);assert.ok(need,`L${n} in needs`);assert.equal(need.recipe,want.recipe);if(want.bridgeSide){assert.equal(need.bridgeSide,want.bridgeSide);assert.equal(need.strip,want.strip);}}}});

test('round-level.v1 carries the overlay in board.overlay, and the schema checks its shape',()=>{
 const lv=placed.find(x=>x.level===16);
 // a level on a wall over a picture without strips: P0012 with an up wall
 const q=L.picture('P0012'),ov=L.wallOverlay(q,'up',1),b=L.boardOf(q,ov);
 const def={...lv,picture:'P0012',definition:{...lv.definition,w:b.w,h:b.h,rowsBottomFirst:b.rowsBottomFirst,entries:b.entries,hero:b.hero,overlay:b.overlay}};
 const out=X.levelV1(def,{rules:'0'.repeat(64),timeline:'0'.repeat(64),palette:'0'.repeat(64),clock:'0'.repeat(64)});
 out.picture.version=1;
 assert.deepEqual(out.board.overlay,{walls:ov.walls,sites:ov.sites});assert.deepEqual(validate('round-level',out),[]);
 assert.deepEqual(Object.keys(out.board.hero.choices[0]),['cell','anchor','label']);
 const bad={...out,board:{...out.board,overlay:{walls:ov.walls,sites:[...ov.sites,[1,1]]}}};assert.ok(validate('round-level',bad).some(e=>/more than 2/.test(e)));
 const extra={...out,board:{...out.board,overlay:{...out.board.overlay,side:'up'}}};assert.ok(validate('round-level',extra).some(e=>/unexpected side/.test(e)));
 // the schema copy embedded in the game is byte-identical
 assert.equal(fs.readFileSync(path.join(ROOT,'core/src/Bloomlings.Content/Schemas/round-level.schema.json'),'utf8'),fs.readFileSync(path.join(ROOT,'specs/006-round-levels-in-game/contracts/round-level.schema.json'),'utf8'));});

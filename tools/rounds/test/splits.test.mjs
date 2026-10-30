// node --test tools/rounds/test/ — split pictures (tools/rounds/bridge-scene.mjs make --mode split; 17 §8 «Одна цельная
// картинка», the owner, 2026-10-29): one whole approved picture split by an inserted neutral strip with two Sprig sites.
// The geometry for up, left, right and two (an L, corners tl and tr): no picture cell lost, the ring closed at the strip's
// ends, the far part reachable only by a bridge, the anchors on the near bank; the gap contrast, the trim to 48×48, the
// level's strip marker and the plan filter. Reads content/rounds, writes nothing.
import test from 'node:test';import assert from 'node:assert/strict';

const L=await import('../lib.mjs');
process.argv.splice(2,0,'noop');const LV=await import('../levels.mjs');const BS=await import('../bridge-scene.mjs');process.argv.splice(2,1);
const {engine}=await import('../engine.mjs');const {R}=await engine();
const grid=b=>b.rowsBottomFirst.slice().reverse();// top first, board coordinates
const at=(b,x,y)=>grid(b)[y][x];
const idx=(b,x,y)=>(b.h-1-y)*b.w+x;// board top-first (x, y) → bottom-first index
// cells reachable from the entry through non-wall cells (colour cells pass), with the given bridge cells open
function reach(b,open=[]){const {w,h}=b,g=b.rowsBottomFirst.join(''),o=new Set(open),seen=new Set(b.entries),q=[...b.entries];
 while(q.length){const i=q.pop(),x=i%w,y=Math.floor(i/w);for(const [dx,dy] of [[1,0],[-1,0],[0,1],[0,-1]]){const u=x+dx,v=y+dy,j=v*w+u;if(u<0||v<0||u>=w||v>=h||seen.has(j)||(g[j]==='#'&&!o.has(j)))continue;seen.add(j);q.push(j);}}return seen;}
// a synthetic picture: colour bands without Жемчуг (C), so the gap contrast never refuses it
const SYN={id:'T',title:'синтетика',rows:Array.from({length:14},(_,y)=>Array.from({length:16},(_,x)=>'GRBYMPOV'[(Math.floor(x/4)+2*Math.floor(y/5))%8]).join(''))};
// every split shape: [side, options (positions along the strip in sites)]
const SHAPES=[['up',{at:6,sites:[3,11]}],['left',{at:4,sites:[3,10]}],['right',{at:12,sites:[2,11]}],['two',{at:[9,8],sites:[3,2],corner:'tl'}],['two',{at:[7,8],sites:[13,3],corner:'tr'}]];

test('split geometry: no picture cell is lost — every source cell stands where the split moved it, the strip is only # and two b',()=>{
 for(const [side,o] of SHAPES){const sp=BS.splitOf(SYN,{side,...o}),src=SYN.rows,{dw,dh}=BS.growth(side);
  assert.equal(sp.rows[0].length,src[0].length+dw,`${side}: one column more`);assert.equal(sp.rows.length,src.length+dh,`${side}: one row more`);
  src.forEach((r,y)=>{for(let x=0;x<r.length;x++){const [u,v]=sp.map(x,y);assert.equal(sp.rows[v][u],r[x],`${side} ${o.corner||''}: cell (${x}, ${y})`);}});
  assert.deepEqual(L.measure(sp).volumes,L.measure(SYN).volumes,`${side}: the same volumes`);
  for(const [x,y] of sp.strip)assert.ok('#b'.includes(sp.rows[y][x]),`${side}: strip cell (${x}, ${y})`);
  assert.equal(sp.rows.join('').split('b').length-1,2,`${side}: two sites`);
  // the cells that are neither a moved source cell nor the strip are empty margin (an L's moved corner leaves an edge row
  // and column)
  const used=new Set([...sp.strip.map(([x,y])=>x+','+y)]);src.forEach((r,y)=>{for(let x=0;x<r.length;x++)used.add(sp.map(x,y).join(','));});
  sp.rows.forEach((r,y)=>{for(let x=0;x<r.length;x++)if(!used.has(x+','+y))assert.equal(r[x],'.',`${side}: margin (${x}, ${y})`);});}
 // a real approved picture keeps all its cells too, and a source with a strip is refused
 const p=L.picture('P0074'),[s]=BS.splitsFor(p,'up');const sp=BS.splitOf(p,s);
 assert.equal(L.measure(sp).N,L.measure(p).N);assert.deepEqual(L.measure(sp).volumes,L.measure(p).volumes);
 assert.throws(()=>BS.splitOf({rows:['GG#G','GGGG']},{side:'up',at:1}),/полоса/);});

test('split geometry: the ring is closed at both ends of the strip, and the entry stays on the near side',()=>{
 for(const [side,o] of SHAPES){const sp=BS.splitOf(SYN,{side,...o}),b=L.boardOf(sp),W=b.w,H=b.h;
  if(side==='up'){assert.equal(at(b,1,o.at+2),'#');assert.equal(at(b,W-2,o.at+2),'#');}
  if(side==='left'||side==='right'){assert.equal(at(b,o.at+2,1),'#');assert.equal(at(b,o.at+2,H-2),'#');}
  if(side==='two'){const [x0,y0]=o.at;assert.equal(at(b,x0+2,1),'#','top ring closed above the vertical leg');assert.equal(at(b,o.corner==='tl'?1:W-2,y0+2),'#','side ring closed beside the horizontal leg');
   assert.equal(at(b,o.corner==='tl'?W-2:1,y0+2),'.','the other side ring stays open');}
  // the entry (bottom middle) is open and joins the near bank
  assert.equal(at(b,b.entries[0],H-1),'.');assert.equal(at(b,b.entries[0],H-2),'.');}});

test('split geometry: the far part is reachable only by a bridge, and each bridge alone opens every colour cell',()=>{
 for(const [side,o] of SHAPES){const sp=BS.splitOf(SYN,{side,...o}),b=L.boardOf(sp),g=b.rowsBottomFirst.join(''),r0=reach(b);let far=0;
  sp.rows.forEach((r,y)=>{for(let x=0;x<r.length;x++)if(sp.far(x,y)&&!'#b'.includes(r[x])){far++;assert.ok(!r0.has(idx(b,x+2,y+2)),`${side}: far cell (${x}, ${y}) open without a bridge`);}});
  assert.ok(far>0,`${side}: a far part`);
  // the far ring segment belongs to the far part too: the board's top-left ring corner for up, left and tl
  if(side==='up'||side==='left'||(side==='two'&&o.corner==='tl'))assert.ok(!r0.has(idx(b,1,1)));
  if(side==='right'||(side==='two'&&o.corner==='tr'))assert.ok(!r0.has(idx(b,b.w-2,1)));
  for(const c of b.hero.choices){const r=reach(b,[c.cell]);for(let i=0;i<g.length;i++)if(L.CODES.includes(g[i]))assert.ok(r.has(i),`${side}: cell ${i} cut off with one bridge`);}
  assert.ok(BS.checkSplit(sp,SYN).ok,`${side}: ${BS.checkSplit(sp,SYN).issues.join('; ')}`);}});

test('split geometry: anchors on the near bank — below for up, beside for left and right, one of each for two',()=>{
 for(const [side,o] of SHAPES){const sp=BS.splitOf(SYN,{side,...o}),b=L.boardOf(sp),g=b.rowsBottomFirst.join(''),cs=b.hero.choices;
  // the engine accepts the board and the bridges (its own anchor rule)
  const errs=R.validate({id:1,...b,tray:[[0],[1]],pods:[{color:'G',count:1},{color:'G',count:1}],capacity:5});assert.equal(cs.length,2);
  for(const e of ['grid','dimensions/id','entry','bridge'])assert.ok(!errs.includes(e),`${side}: engine ${e}`);
  for(const c of cs){assert.ok(L.CODES.includes(g[c.anchor]),`${side}: colour anchor`);assert.equal(g[c.cell],'#');}
  const dirs=cs.map(c=>c.dir).sort();
  if(side==='up'){assert.deepEqual(dirs,['up','up']);for(const c of cs)assert.equal(c.anchor,c.cell-b.w);}
  if(side==='left'){assert.deepEqual(dirs,['left','left']);for(const c of cs)assert.equal(c.anchor,c.cell+1);}
  if(side==='right'){assert.deepEqual(dirs,['right','right']);for(const c of cs)assert.equal(c.anchor,c.cell-1);}
  if(side==='two')assert.deepEqual(dirs,o.corner==='tl'?['left','up']:['right','up']);
  assert.equal(L.bridgeSide(b),side);
  // the sites are where the split put them
  assert.deepEqual(cs.map(c=>[c.cell%b.w-2,b.h-3-Math.floor(c.cell/b.w)]).sort(),sp.sites.map(s=>[...s]).sort());}});

test('splitsFor: real pictures split to every side they allow, each split passes checkSplit, the best first',()=>{
 for(const [id,side] of [['P0074','up'],['P0062','left'],['P0011','right'],['P0035','two']]){const p=L.picture(id),tr=BS.trimmed(p.rows,side),src={...p,rows:tr.rows};
  const list=BS.splitsFor(src,side,{per:2});assert.ok(list.length>0,`${id} ${side}`);
  for(const s of list.slice(0,6)){const sp=BS.splitOf(src,s),ck=BS.checkSplit(sp,src);assert.ok(ck.ok,`${id} ${s.key}: ${ck.issues.join('; ')}`);
   assert.ok(ck.share>=BS.GAP_SHARE[0]&&ck.share<=BS.GAP_SHARE[1]);assert.equal(ck.board.strip,'gap');}
  for(let i=1;i<list.length;i++)if(list[i].key===list[0].key)assert.fail('keys unique');
  assert.deepEqual(BS.splitsFor(src,side,{per:2}).map(s=>s.key),list.map(s=>s.key),'deterministic');}});

test('gap contrast: a split with Жемчуг beside the strip is refused (it is nearly the board\'s colour), unless an explicit tolerance allows it',()=>{
 // C beside the strip: rows of C above and below the cut
 const pic={rows:[...Array(4).fill('GGGGGGGGGG'),'CCCCCCCCCC','RRRRRRRRRR',...Array(5).fill('BBBBBBBBBB')]},sp=BS.splitOf(pic,{side:'up',at:5,sites:[2,7]});
 const gc=BS.gapContrast(sp);assert.equal(gc.code,'C');assert.ok(gc.worst<BS.GAP_CONTRAST);assert.equal(gc.low,10);assert.equal(gc.cells,20);
 assert.ok(!BS.checkSplit(sp,pic).ok);assert.ok(BS.checkSplit(sp,pic).issues.some(i=>/почти цвета фона/.test(i)));
 assert.ok(!BS.gapOk(gc,.25)&&BS.gapOk(gc,.5),'tolerance: at most that share of the colour cells beside the strip');
 // every other palette colour is at least GAP_CONTRAST away from both surfaces
 for(const c of L.CODES)if(c!=='C'){const s=BS.splitOf({rows:['GGGG','GGGG',c.repeat(4),'GGGG','GGGG']},{side:'up',at:2,sites:[1,3]});assert.ok(BS.gapContrast(s).worst>=BS.GAP_CONTRAST,c);}
 // splitsFor skips the C position
 assert.ok(BS.splitsFor(pic,'up',{share:[0,1]}).every(s=>s.at!==5));});

test('trim: only empty edge rows and columns go, as many as the board with the strip needs; a full edge refuses',()=>{
 const row=w=>'.'+'G'.repeat(w-2)+'.',pic=Array.from({length:44},(_,y)=>y===0?'.'.repeat(44):row(44));
 assert.equal(BS.trimmed(pic,'up').rows.length,43);assert.equal(BS.trimmed(pic,'up').top,1);assert.equal(BS.trimmed(pic,'up').rows[0].length,44);
 const t=BS.trimmed(pic,'two');assert.equal(t.rows.length,43);assert.equal(t.rows[0].length,43);
 assert.equal(L.measure({rows:t.rows}).N,L.measure({rows:pic}).N);
 assert.equal(BS.trimmed(Array(44).fill('G'.repeat(44)),'up'),null);assert.equal(BS.trimmed(Array(30).fill('G'.repeat(30)),'two').rows.length,30);});

test('the level of a split picture carries strip gap; a gap position of the plan takes only such a level',()=>{
 const sp=BS.splitOf(SYN,{side:'up',at:6,sites:[3,11]}),pic={id:'T',title:'t',rows:sp.rows,scene:{mode:'split'}},b=L.boardOf(pic);
 assert.equal(b.strip,'gap');assert.equal(L.boardOf({rows:sp.rows}).strip,undefined,'a strip picture without the split record stays water');
 const m=L.measure(pic),base=LV.splitPods(m.volumes,m.N,null);let c=null;for(let k=0;k<20&&!(c&&c.level);k++)c=LV.candidate(pic,b,base.pods,3,'bridge+hidden','Normal',100+k,false,.5);
 assert.ok(c.level,c.error);assert.equal(c.level.strip,'gap');
 const plan={recipe:'bridge+hidden',bridgeSide:'up',strip:'gap'};
 assert.ok(L.fitsPlan({recipe:'bridge+hidden',level:c.level},plan));
 assert.ok(!L.fitsPlan({recipe:'bridge+hidden',level:{...c.level,strip:undefined}},plan),'a level on a stream is not a gap');
 assert.ok(L.fitsPlan({recipe:'bridge+hidden',level:{...c.level,strip:undefined}},{recipe:'bridge+hidden',bridgeSide:'up'}),'without strip gap in the plan any strip');});

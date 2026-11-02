// node --test tools/rounds/test/ — a level's recolouring of its picture and the builder's accents (17 §3 «кубики можно
// докрасить или перекрасить», §6 «Акценты для давления на места», the owner, 2026-11-01): the format (picture
// coordinates, applied before the overlay, the refusals), the board ↔ picture identity with it, the export and the
// schema, an accent candidate on a free picture (clusters deep inside one-colour areas, a material absent from the
// picture, its pods early or mid queue, the twin without accents), the audit. Writes only to temporary copies.
import test from 'node:test';import {after} from 'node:test';
// every sandbox under the system's temp folder is deleted when this file's tests end (and on exit): nothing is left behind
const TMP=[],tmpDir=p=>{const d=fs.mkdtempSync(path.join(os.tmpdir(),p));TMP.push(d);return d;},rmTmp=()=>{for(const d of TMP.splice(0))fs.rmSync(d,{recursive:true,force:true});};after(rmTmp);process.on('exit',rmTmp);
import assert from 'node:assert/strict';
import fs from 'node:fs';import os from 'node:os';import path from 'node:path';import {spawnSync} from 'node:child_process';import {fileURLToPath} from 'node:url';

const HERE=path.dirname(fileURLToPath(import.meta.url)),TOOLS=path.dirname(HERE),ROOT=path.resolve(TOOLS,'../..');
const L=await import('../lib.mjs');
const {engine}=await import('../engine.mjs');const {R}=await engine();
const X=await import('../export.mjs');const {validate}=await import('../schema.mjs');const {deltaE}=await import('../colour-distance.mjs');
process.argv.splice(2,0,'noop');const LV=await import('../levels.mjs');process.argv.splice(2,1);
const readJSON=f=>JSON.parse(fs.readFileSync(f,'utf8'));
const rnd=seed=>{let s=seed>>>0||1;return()=>{s^=s<<13;s^=s>>>17;s^=s<<5;return(s>>>0)/4294967296;};};
const ZERO={rules:'0'.repeat(64),timeline:'0'.repeat(64),palette:'0'.repeat(64),clock:'0'.repeat(64)};

// a small picture: a green field (G) with a brown trunk (E), an empty corner
const pic={id:'T',title:'t',rows:['..GGGGG','GGGGGGG','GGGEGGG','GGGEGGG','GGGEGGG','GGGGGGG'],w:7,h:6};

test('recolor: applied in picture coordinates before the overlay; N, W and the colours count the recoloured board',()=>{
 const rc=[[3,1,'F'],[4,1,'F'],[1,4,'Y']],b=L.boardOf(pic,null,rc),top=[...b.rowsBottomFirst].reverse();
 assert.equal(top[1+2].slice(2,-2),'GGGFFGG');assert.equal(top[4+2].slice(2,-2),'GYGEGGG');assert.deepEqual(b.recolor,rc);
 const m=L.measure(L.overlaid(pic,null,rc));assert.equal(m.N,L.measure(pic).N);assert.equal(m.colours,4);assert.equal(m.volumes.F,2);assert.equal(m.volumes.Y,1);
 // with an overlay: the wall is laid on the recoloured picture
 const ov={walls:[[0,3],[1,3],[2,3],[3,3],[4,3],[5,3],[6,3]],sites:[[1,3],[5,3]]},bo=L.boardOf(pic,ov,rc);
 assert.equal([...bo.rowsBottomFirst].reverse()[1+2].slice(2,-2),'GGGFFGG');assert.ok(bo.hero&&bo.overlay&&bo.recolor);
 // no recolouring: the same objects as before (no recolor key, the picture itself)
 assert.equal(L.overlaid(pic,null,null),pic);assert.equal(L.overlaid(pic,null,[]),pic);assert.equal('recolor' in L.boardOf(pic),false);
 assert.deepEqual(L.boardOf(pic,null,[]),L.boardOf(pic));});

test('recolor: the refusals (outside, empty, strip, the same colour, no material, twice, under the wall)',()=>{
 const strip={...pic,rows:['GGGGGGG','#######','GGGGGGG','GGGGGGG']};
 for(const [p,rc,re] of [[pic,[[7,0,'F']],/вне картинки 7×6/],[pic,[[0,0,'F']],/не цветная/],[strip,[[2,1,'F']],/не цветная/],[pic,[[2,2,'G']],/тот же цвет/],
  [pic,[[2,2,'q']],/не материал палитры/],[pic,[[2,2,'F'],[2,2,'Y']],/дважды/],[pic,[[2,2]],/нужно \[x, y, код\]/],[pic,[],/непустой список/]]){
  assert.match(L.recolorErrors(p,rc).join(' | '),re,JSON.stringify(rc));if(rc.length)assert.throws(()=>L.boardOf(p,null,rc),re);}
 assert.throws(()=>L.boardOf(pic,{walls:[[0,3],[1,3],[2,3],[3,3],[4,3],[5,3],[6,3]],sites:[[1,3],[5,3]]},[[2,3,'F']]),/под стеной/);
 assert.deepEqual(L.recolorErrors(pic,null),[]);});

test('export: board.recolor in round-level.v1, valid by the schema; a level without one is written as before',()=>{
 const src=readJSON(path.join(ROOT,'content/rounds/levels/L0021.json')),p=L.picture(src.picture),c=Object.keys(L.measure(p).volumes)[0],abs=L.CODES.split('').find(x=>!(x in L.measure(p).volumes));
 // every placed level exports as the game holds it (no recolor key; the fingerprints and versions aside)
 for(const f of fs.readdirSync(path.join(ROOT,'content/game/rounds/levels')).filter(f=>/^L\d+\.json$/.test(f))){const n=+f.slice(1,5),Lv=readJSON(path.join(ROOT,'content/rounds/levels',f)),g=readJSON(path.join(ROOT,'content/game/rounds/levels',f));
  const out=X.levelV1(Lv,g.engine,g.definitionVersion);out.picture.version=g.picture.version;assert.deepEqual(out,g,`L${n}`);assert.equal('recolor' in out.board,false);}
 // one picture cell painted: the board is that picture's with the recolouring, the schema takes it
 let at=null;p.rows.forEach((r,y)=>{for(let x=0;x<r.length;x++)if(!at&&r[x]===c)at=[x,y];});
 const rc=[[at[0],at[1],abs]],b=L.boardOf(p,src.definition.overlay,rc),def={...src,definition:{...src.definition,rowsBottomFirst:b.rowsBottomFirst,recolor:rc}};
 const out=X.levelV1(def,ZERO);out.picture.version=1;assert.deepEqual(out.board.recolor,rc);assert.deepEqual(validate('round-level',out),[]);
 for(const [bad,re] of [[[[1,2]],/fewer than 3/],[[[1,2,'q']],/does not match/],[[[-1,2,'G']],/below minimum/],[[[1,2,'G',3]],/more than 3/],[[],/fewer than 1/],[[['1',2,'G']],/string, expected integer/]])
  assert.ok(validate('round-level',{...out,board:{...out.board,recolor:bad}}).some(e=>re.test(e)),JSON.stringify(bad));
 assert.equal(fs.readFileSync(path.join(ROOT,'core/src/Bloomlings.Content/Schemas/round-level.schema.json'),'utf8'),fs.readFileSync(path.join(ROOT,'specs/006-round-levels-in-game/contracts/round-level.schema.json'),'utf8'));});

// a free, non-franchise approved picture with a dark sky (P0042 «Космонавт поливает цветок на астероиде»)
test('accents: 1–2 absent materials in clusters of 2–4 cells deep inside one-colour areas, readable on their ground',()=>{
 const p=L.picture('P0042'),m=L.measure(p),b=L.boardOf(p);
 for(let seed=1;seed<=12;seed++){const a=LV.pickAccents(p,null,rnd(seed),{materials:2});assert.ok(a,`seed ${seed}`);
  assert.ok(a.accents.length>=1&&a.accents.length<=2);assert.deepEqual(a.recolor,[...a.recolor].sort((u,v)=>u[1]-v[1]||u[0]-v[0]));
  const all=new Set(a.recolor.map(([x,y])=>x+','+y));assert.equal(all.size,a.recolor.length);
  for(const ac of a.accents){assert.ok(!(ac.code in m.volumes),'absent from the picture');assert.ok(ac.clusters.length>=1&&ac.clusters.length<=3);assert.ok(deltaE(ac.code,ac.from)>=10);
   for(const cl of ac.clusters){assert.ok(cl.length>=2&&cl.length<=4);
    for(const [x,y] of cl){assert.equal(p.rows[y][x],ac.from,'painted over its ground');
     // the eight neighbours: the same ground or the same cluster (the silhouette never changes)
     for(let dy=-1;dy<=1;dy++)for(let dx=-1;dx<=1;dx++){const k=(x+dx)+','+(y+dy);if(cl.some(([u,v])=>u===x+dx&&v===y+dy))continue;assert.ok(!all.has(k),'apart from other accents');assert.equal(p.rows[y+dy][x+dx],ac.from);}}}}
  // on the dark sky (Глубина) the stars come first: Снег or Солнце
  if(a.accents[0].from==='N')assert.ok(['W','Y'].includes(a.accents[0].code));}
 // decorations: stars on dark, flowers on green
 assert.ok(LV.decorScore('N','W')>LV.decorScore('N','F'));assert.ok(LV.decorScore('G','F')>LV.decorScore('G','W'));
 // a picture without a one-colour area big and deep enough has no place for accents
 assert.equal(LV.pickAccents(pic,null,rnd(1)),null);});

test('accent candidate: the recoloured board, the pods early or mid queue, the twin without accents',()=>{
 const p=L.picture('P0042');let done=0;
 for(let seed=1;seed<=8;seed++){const r=rnd(seed),a=LV.pickAccents(p,null,r,{materials:2}),am=L.measure(L.overlaid(p,null,a.recolor)),ab=LV.splitPods(am.volumes,am.N,null);assert.ok(!ab.error,ab.error);
  const pods=LV.accentPods(ab.pods,a.accents,r),board=L.boardOf(p,null,a.recolor),codes=a.accents.map(x=>x.code);
  for(const ac of a.accents){const ps=pods.filter(q=>q.color===ac.code);assert.ok(ps.length>=1&&ps.length<=2);assert.equal(ps.reduce((s,q)=>s+q.count,0),ac.cells,'quota = the accent volume');}
  assert.deepEqual(LV.checkPods(pods,am.volumes),[]);
  for(const [recipe,cls] of [['links','Hard'],['none','Hard'],['links','Super Hard']]){const c=LV.candidate(p,board,pods,4,recipe,cls,seed*31+7,false,.5,{accents:codes});assert.ok(c.level,c.error);
   const lv=c.level;assert.deepEqual(R.validate(lv),[]);assert.deepEqual(lv.recolor,a.recolor);assert.deepEqual(L.boardOf(p,null,lv.recolor).rowsBottomFirst,lv.rowsBottomFirst);
   const rows=Math.max(...lv.tray.map(t=>t.length)),hi=Math.max(1,Math.floor((rows-1)/2));
   lv.pods.forEach((q,i)=>{if(!codes.includes(q.color))return;const row=lv.tray.map(t=>t.indexOf(i)).find(k=>k>=0);assert.ok(row>=1&&row<=hi,`accent pod in row ${row} of ${rows}`);});
   // at the start no accent cube is reachable: each accent pod holds its place until the heroes get there
   const s=R.create(lv,5),reach=new Set(R.reach(s).targets.map(i=>s.board[i]));for(const code of codes)assert.ok(!reach.has(s.meta.colours[lv.pods.findIndex(q=>q.color===code)]));
   // the twin: the accent cubes back in their ground, the accent pods playing it, the same queue
   const tw=LV.accentTwin(lv,p);assert.deepEqual(tw.rowsBottomFirst,L.boardOf(p).rowsBottomFirst);assert.equal('recolor' in tw,false);assert.deepEqual(tw.tray,lv.tray);
   assert.deepEqual(R.validate(tw),[]);const sum={};for(const q of tw.pods)sum[q.color]=(sum[q.color]||0)+q.count;assert.deepEqual(sum,L.measure(p).volumes);done++;}}
 assert.equal(done,24);
 // a recolouring onto a material already in the picture is no accent: no twin
 assert.equal(LV.accentTwin({rowsBottomFirst:[],recolor:[[0,0,'N']]},p),null);});

test('accent attempts: auto = every Hard and Super Hard attempt, every third regular Normal, never an introduction or L1–3',()=>{
 assert.equal(LV.accentWanted('auto','Hard',false,null,0),true);assert.equal(LV.accentWanted('auto','Super Hard',false,null,5),true);
 assert.deepEqual([0,1,2,3,4].map(k=>LV.accentWanted('auto','Normal',false,null,k)),[false,true,false,false,true]);
 assert.equal(LV.accentWanted('auto','Normal',true,null,1),false);assert.equal(LV.accentWanted('on','Normal',false,2,1),false);
 assert.equal(LV.accentWanted('off','Hard',false,null,0),false);assert.equal(LV.accentWanted('on','Normal',false,null,0),true);});

// the audit on a sandbox copy: a valid recolouring (rows and pods follow it), a bad entry, rows that ignore it
function sandbox(){const dir=tmpDir('rounds-recolor-');
 for(const p of ['levels','pictures'])fs.cpSync(path.join(ROOT,'content/rounds',p),path.join(dir,p),{recursive:true});
 fs.copyFileSync(path.join(ROOT,'content/rounds/quality.json'),path.join(dir,'quality.json'));return dir;}
const audit=(dir,a,b)=>{const j=path.join(dir,'audit.json');spawnSync(process.execPath,[path.join(TOOLS,'audit.mjs'),String(a),String(b),'--json',j],{cwd:ROOT,env:{...process.env,ROUNDS_DIR:dir},encoding:'utf8',maxBuffer:1<<26});return readJSON(j);};
test('audit: accepts a recolouring the board and the pods follow; refuses a bad entry and rows that ignore it',()=>{const dir=sandbox(),f=n=>path.join(dir,'levels',`L${String(n).padStart(4,'0')}.json`);
 // L22: two cells of a deep accent painted, the board rebuilt, a pod of the accent taken from the ground's pods
 const a=readJSON(f(22)),p=L.picture(a.picture),acc=LV.pickAccents(p,a.definition.overlay,rnd(3)),rc=acc.recolor.slice(0,2),code=rc[0][2],from=p.rows[rc[0][1]][rc[0][0]];
 for(const e of rc)e[2]=code;const d=a.definition;d.recolor=rc;d.rowsBottomFirst=L.boardOf(p,d.overlay,rc).rowsBottomFirst;
 const k=d.pods.findIndex(q=>q.color===from&&q.count>2);d.pods[k].count-=2;d.pods.push({color:code,count:2});d.tray[0].push(d.pods.length-1);fs.writeFileSync(f(22),JSON.stringify(a));
 // L23: a recolouring onto an empty cell (or, on a picture without one, outside it); L24: a recolouring the rows do not show
 const b=readJSON(f(23)),pb=L.picture(b.picture),y0=pb.rows.findIndex(r=>r.includes('.'));b.definition.recolor=[y0>=0?[pb.rows[y0].indexOf('.'),y0,'F']:[pb.rows[0].length,0,'F']];fs.writeFileSync(f(23),JSON.stringify(b));
 const c=readJSON(f(24)),pc=L.picture(c.picture),y1=pc.rows.findIndex(r=>/[A-Z]/.test(r)),x1=pc.rows[y1].search(/[A-Z]/);c.definition.recolor=[[x1,y1,pc.rows[y1][x1]==='F'?'Y':'F']];fs.writeFileSync(f(24),JSON.stringify(c));
 const r=audit(dir,22,24),bad=n=>r.levels.find(x=>x.level===n).bad.join(' | ');
 assert.doesNotMatch(bad(22),/перекраск|поле уровня не совпадает|баланс/);
 assert.match(bad(23),/перекраска .*(клетка картинки не цветная|вне картинки)/);
 assert.match(bad(24),/поле уровня не совпадает с картинкой и её перекраской/);});

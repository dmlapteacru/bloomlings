// node --test tools/rounds/test/ — the exporter of spec 006 (T013): entry-check refusals, the written formats, the
// conformance reference (event lines, signature, random policy) and the transfer log. Runs on a copy of content/rounds
// (ROUNDS_DIR), never on the repository's own content.
import test from 'node:test';import assert from 'node:assert/strict';
import fs from 'node:fs';import os from 'node:os';import path from 'node:path';import {spawnSync} from 'node:child_process';import {fileURLToPath} from 'node:url';

const HERE=path.dirname(fileURLToPath(import.meta.url)),TOOLS=path.dirname(HERE),ROOT=path.resolve(TOOLS,'../..');
const RESULTS='product/playtests/pilot-v12-owner-2026-10-27/results.json';
const {validate}=await import('../schema.mjs');
const C=await import('../conformance.mjs');
const X=await import('../export.mjs');
const {randomGame}=await import('../measure.mjs');
const {engine}=await import('../engine.mjs');
const T=await import('../transfer-log.mjs');
const readJSON=f=>JSON.parse(fs.readFileSync(f,'utf8'));
const level=n=>readJSON(path.join(ROOT,'content/rounds/levels',`L${String(n).padStart(4,'0')}.json`));

// a fresh copy of what the exporter reads, and the owner's pilot results as if played on the current engine (the
// sandbox's results.json: the engine changes between pilots, e.g. the sideways bridge anchor of 2026-10-28, while these
// tests are about the exporter; refusing another engine is tested on its own below)
function sandbox(){const dir=fs.mkdtempSync(path.join(os.tmpdir(),'rounds-export-'));
 for(const p of ['levels','pictures'])fs.cpSync(path.join(ROOT,'content/rounds',p),path.join(dir,p),{recursive:true});
 fs.copyFileSync(path.join(ROOT,'content/rounds/quality.json'),path.join(dir,'quality.json'));
 const res=readJSON(path.join(ROOT,RESULTS)),fp=C.fingerprints();Object.assign(res,{rulesHash:fp.rules,timelineHash:fp.timeline,paletteHash:fp.palette,clockHash:fp.clock});
 fs.writeFileSync(path.join(dir,'results-now.json'),JSON.stringify(res));return dir;}
const now=dir=>path.join(dir,'results-now.json'),relNow=dir=>path.relative(ROOT,now(dir)).split(path.sep).join('/');
const node=(dir,args)=>spawnSync(process.execPath,args,{cwd:ROOT,env:{...process.env,ROUNDS_DIR:dir},encoding:'utf8',maxBuffer:1<<26});
const exportCli=(dir,from,to,extra=[])=>node(dir,[path.join(TOOLS,'export.mjs'),String(from),String(to),'--out',path.join(dir,'out'),'--game',path.join(dir,'game'),...extra]);
const confirmCli=(dir,from,to,sample=now(dir))=>node(dir,[path.join(TOOLS,'rounds.mjs'),'confirm',String(from),String(to),'--sample',sample]);

test('refuses without the owner confirmation, and writes nothing',()=>{const dir=sandbox();
 const r=exportCli(dir,1,2);assert.equal(r.status,1);assert.match(r.stderr,/no owner confirmation: .*runs\/L0001-L0002\/confirmed\.json/);
 assert.equal(fs.existsSync(path.join(dir,'out')),false);});

test('refuses an unapproved picture',()=>{const dir=sandbox();assert.equal(confirmCli(dir,1,2).status,0);
 const f=path.join(dir,'pictures','P0002.json'),p=readJSON(f);p.status='draft';fs.writeFileSync(f,JSON.stringify(p));
 const r=exportCli(dir,1,2);assert.equal(r.status,1);assert.match(r.stderr,/picture not approved: P0002 of L0002 \(draft\)/);});

test('refuses a position that is not placed',()=>{const dir=sandbox();fs.rmSync(path.join(dir,'levels','L0002.json'));
 const r=exportCli(dir,1,2);assert.equal(r.status,1);assert.match(r.stderr,/not placed: L0002/);});

test('refuses an audit НЕТ that no exception names',()=>{const dir=sandbox();assert.equal(confirmCli(dir,1,2).status,0);
 const f=path.join(dir,'levels','L0002.json'),L=readJSON(f);L.cls='Hard';fs.writeFileSync(f,JSON.stringify(L));
 const r=exportCli(dir,1,2);assert.equal(r.status,1);assert.match(r.stderr,/audit НЕТ: L0002: класс Hard, календарь ждёт Normal/);
 assert.equal(fs.existsSync(path.join(dir,'out')),false);});

test('refuses an exception string without a recorded owner decision',()=>{const dir=sandbox();assert.equal(confirmCli(dir,1,2).status,0);
 const f=path.join(dir,'levels','L0002.json'),L=readJSON(f);L.exception='временно';fs.writeFileSync(f,JSON.stringify(L));
 const r=exportCli(dir,1,2);assert.equal(r.status,1);assert.match(r.stderr,/unknown exception: L0002/);});

test('confirm refuses results played on another engine',()=>{const dir=sandbox(),res=readJSON(path.join(ROOT,RESULTS));res.rulesHash='0'.repeat(64);
 const f=path.join(dir,'results.json');fs.writeFileSync(f,JSON.stringify(res));
 const r=confirmCli(dir,1,2,f);assert.equal(r.status,1);assert.match(r.stderr,/another engine \(rules differ\)/);
 assert.equal(fs.existsSync(path.join(dir,'runs','L0001-L0002','confirmed.json')),false);});

test('exports L1–L2 in the v1 formats with fixtures',()=>{const dir=sandbox();
 const c=confirmCli(dir,1,2);assert.equal(c.status,0,c.stderr);const conf=readJSON(path.join(dir,'runs','L0001-L0002','confirmed.json'));
 assert.deepEqual(conf.wave,[1,2]);assert.deepEqual(conf.sample,[1,2]);assert.equal(conf.results,relNow(dir));assert.match(conf.date,/^\d{4}-\d{2}-\d{2}$/);
 const r=exportCli(dir,1,2,['--owner-results',now(dir)]);assert.equal(r.status,0,r.stderr+r.stdout);
 const out=path.join(dir,'out'),m=readJSON(path.join(out,'export.json')),fp=C.fingerprints();
 assert.equal(m.format,'round-export.v1');assert.deepEqual(m.wave,[1,2]);assert.deepEqual(m.levels,[1,2]);assert.deepEqual(m.engine,fp);assert.deepEqual(m.fixtureErrors,[]);
 assert.deepEqual(m.counts,{levels:2,pictures:2,proofs:2,conformance:2,randomGames:200,ownerAttempts:2});
 for(const n of [1,2]){const id=`L000${n}`,src=level(n);
  const lv=readJSON(path.join(out,'levels',id+'.json')),pf=readJSON(path.join(out,'proofs',id+'.json')),fx=readJSON(path.join(out,'conformance',id+'.json'));
  assert.deepEqual(validate('round-level',lv),[]);assert.deepEqual(validate('round-proof',pf),[]);assert.deepEqual(X.fixtureShape(fx),[]);
  assert.equal(lv.number,n);assert.equal(lv.definitionVersion,1);assert.equal(lv.class,src.cls);assert.deepEqual(lv.engine,fp);
  assert.deepEqual(lv.pods.map(p=>p.code+p.count),src.definition.pods.map(p=>p.color+p.count));assert.deepEqual(lv.board.rowsBottomFirst,src.definition.rowsBottomFirst);
  const pic=readJSON(path.join(out,'pictures',lv.picture.id+'.json'));assert.deepEqual(validate('round-picture',pic),[]);assert.equal(pic.version,lv.picture.version);
  // the proof: the patient solution, the level's random record, the audit verdict
  assert.equal(pf.solution.outcome,'Won');assert.equal(pf.solution.commands[0].atUs,0);assert.ok(pf.solution.commands.every((c,i,a)=>Number.isInteger(c.atUs)&&(!i||c.atUs>=a[i-1].atUs)));
  assert.deepEqual(pf.solution.commands.map(({atUs,...a})=>a),src.solution.route);assert.equal(pf.randomTest.wins,src.random.wins);assert.equal(pf.audit.result,'OK');
  // the fixture: one window per command, every random game, the owner's attempt
  assert.deepEqual(fx.solution.windows.map(w=>w.command),pf.solution.commands);assert.equal(fx.solution.endUs,pf.solution.endUs);
  assert.equal(fx.random.games.length,pf.randomTest.runs);assert.equal(fx.random.wins,pf.randomTest.wins);assert.equal(fx.random.games.filter(g=>g.outcome==='Won').length,fx.random.wins);
  assert.deepEqual(fx.random.games.slice(0,3).map(g=>g.seed),[7920,112649,217378]);
  assert.equal(fx.owner.length,1);assert.equal(fx.owner[0].outcome,'won');assert.equal(fx.owner[0].source,relNow(dir));}
 // the transfer log takes the export, then its stages in order
 const log=path.join(dir,'transfers.jsonl');
 assert.equal(T.stateOf(T.record('exported',{exportDir:out,file:log})),'exported');
 assert.throws(()=>T.record('published',{from:1,to:2,contentVersion:'v',apk:'a.apk',file:log}),/is exported; "published" needs "validated"/);
 T.record('validated',{from:1,to:2,file:log});T.record('published',{from:1,to:2,contentVersion:'r1',apk:'playtest.apk',file:log});
 assert.equal(T.stateOf(T.record('confirmed',{from:1,to:2,file:log})),'confirmed');
 T.record('exported',{exportDir:out,file:log});const all=T.read(log);assert.equal(all.length,2);assert.equal(T.stateOf(all[1]),'exported');
 assert.deepEqual(Object.keys(all[0]),['wave','levels','exceptions','contentVersion','engine','exportedAt','validatedAt','apk','ownerConfirmedAt','note']);});

test('L18 carries the owner exception in the level format',()=>{const lv=X.levelV1(level(18),C.fingerprints());
 assert.deepEqual(lv.exception,{rule:'one deciding bridge from the second introduction',reason:'owner kept both winning bridges',decidedBy:'owner',decidedAt:'2026-10-27'});
 lv.picture.version=2;assert.deepEqual(validate('round-level',lv),[]);assert.deepEqual(lv.introduction,{mechanic:'bridge',second:true});assert.deepEqual(lv.mechanics,['bridge']);
 assert.equal(lv.board.hero.choices.length,2);assert.ok(X.RULE_PATTERNS[lv.exception.rule].test('мост: решением должен быть ровно один мост и оба места должны встречаться'));});

test('schemas refuse the contract cases',()=>{const lv=X.levelV1(level(1),C.fingerprints());lv.picture.version=1;assert.deepEqual(validate('round-level',lv),[]);
 const bad=f=>{const x=structuredClone(lv);f(x);return validate('round-level',x);};
 assert.ok(bad(x=>x.board.w=49).length);assert.ok(bad(x=>x.capacity=4).length);assert.ok(bad(x=>x.pods[0].count=61).length);assert.ok(bad(x=>x.pods[0].code='X').length);
 assert.ok(bad(x=>x.extra=1).length);assert.ok(bad(x=>delete x.engine).length);});

test('canonical event lines and the extended signature',async()=>{const def=X.levelV1(level(1),C.fingerprints()),tr=await C.solutionTrace(def,level(1).solution.route);
 const lines=tr.windows.flatMap(w=>w.lines),re=[/^\d+ commit pod=\d+ slot=\d+$/,/^\d+ reveal pod=\d+ mode=(depth|sealed)$/,/^\d+ bridge cell=\d+$/,/^\d+ depart id=\d+ pod=\d+ cell=\d+ arrive=\d+ clear=\d+ done=\d+ route=\d+$/,/^\d+ arrive id=\d+$/,/^\d+ clear id=\d+ cell=\d+$/,/^\d+ return id=\d+$/,/^\d+ complete pod=\d+$/];
 for(const l of lines)assert.ok(re.some(r=>r.test(l)),l);
 assert.equal(tr.windows[0].lines[0],'0 commit pod=5 slot=0');assert.equal(tr.windows[0].lines[1],'0 depart id=1 pod=5 cell=45 arrive=650000 clear=750000 done=1150000 route=3');
 // times never go back within a window; every depart's courier arrives, clears and returns once
 for(const w of tr.windows){const t=w.lines.map(l=>+l.split(' ')[0]);assert.ok(t.every((x,i)=>!i||x>=t[i-1]));}
 for(const k of ['arrive','clear','return'])assert.equal(lines.filter(l=>l.includes(` ${k} id=`)).length,lines.filter(l=>l.includes(' depart ')).length);
 const sig=tr.windows[0].signature.split('|');assert.equal(sig.length,10);assert.equal(sig[7],'Playing');assert.equal(sig[8],'0');assert.match(sig[9],/^(\d+:(arrive|clear|return|release|check):\d+)(,\d+:(arrive|clear|return|release|check):\d+)*$/);
 const end=tr.final.split('|');assert.equal(end[7],'Won');assert.equal(end[9],'');
 // fixtures are reproducible
 const a=await C.solutionFixture(def,level(1).solution.route),b=await C.solutionFixture(def,level(1).solution.route);assert.deepEqual(a.windows.map(w=>w.events),b.windows.map(w=>w.events));});

test('random policy: xorshift32 and seeds of research R5, the same games as measure.mjs',async()=>{
 const r=C.rng(1);assert.equal(r(),(270369>>>0)/4294967296);assert.equal(C.rng(0)(),C.rng(1)());assert.equal(C.gameSeed(1,0),7920);assert.equal(C.gameSeed(1,1),112649);
 // index = floor(r × n) equals (s × n) >> 32 in integers
 const s=C.rng(12345);for(let i=0;i<50;i++){const x=s(),u=BigInt(Math.round(x*4294967296));for(const n of [1,2,3,7,11])assert.equal(Math.floor(x*n),Number((u*BigInt(n))>>32n));}
 const E=await engine();for(const n of [5,10]){const L=level(n),def=X.levelV1(L,C.fingerprints()),lv=C.engineLevel(def);
  for(let i=0;i<12;i++){const seed=C.gameSeed(1,i),g=await C.randomGameFixture(def,seed);assert.equal(g.outcome==='Won',randomGame(E,lv,seed),`L${n} seed ${seed}`);}}});

test('a bridge level\'s random games build only its test bridge, the same games as measure.mjs with keep',async()=>{
 // L30 of wave L21–L50: the class test plays its deciding bridge only (17 §5 as amended on 2026-10-30)
 const L=level(30);assert.ok(Number.isInteger(L.random.bridge));const def=X.levelV1(L,C.fingerprints()),lv=C.engineLevel(def),E=await engine(),only=L.random.bridge;
 const keep=a=>a.type!=='bridge'||a.choice===only;let differs=false;
 for(let i=0;i<8;i++){const seed=C.gameSeed(L.random.seedBase,i),g=await C.randomGameFixture(def,seed,only);assert.equal(g.outcome==='Won',randomGame(E,lv,seed,keep),`seed ${seed}`);
  const free=await C.randomGameFixture(def,seed);if(free.finalSignature!==g.finalSignature)differs=true;}
 assert.ok(differs,'without the restriction some game changes');
 // the proof and the fixture carry it; the fixture shape takes it
 const sh={format:'round-conformance.v1',number:30,definitionVersion:1,engine:C.fingerprints(),solution:{windows:[{command:{type:'tap',pod:0,atUs:0},events:'sha256:'+'0'.repeat(64),signature:'sha256:'+'0'.repeat(64)}],outcome:'Won',endUs:1,finalSignature:'sha256:'+'0'.repeat(64)},
  random:{policy:X.FIXTURE_POLICY,seedBase:1,runs:100,bridge:only,games:Array.from({length:100},(_,i)=>({seed:i,outcome:'Won',endUs:1,finalSignature:'sha256:'+'0'.repeat(64)})),wins:100},owner:[]};
 assert.deepEqual(X.fixtureShape(sh),[]);sh.random.bridge=-1;assert.deepEqual(X.fixtureShape(sh),['random']);});

test('confirm on the owner\'s word records the words and no sample',()=>{const dir=sandbox();
 const c=node(dir,[path.join(TOOLS,'rounds.mjs'),'confirm','1','2','--owner-word','Интегрируй 1-2 в игру.','--date','2026-10-31']);assert.equal(c.status,0,c.stderr);
 const conf=readJSON(path.join(dir,'runs','L0001-L0002','confirmed.json'));
 assert.deepEqual(conf.wave,[1,2]);assert.deepEqual(conf.sample,[]);assert.equal(conf.results,null);assert.equal(conf.ownerWord,'Интегрируй 1-2 в игру.');assert.equal(conf.confirmedBy,'owner');assert.equal(conf.date,'2026-10-31');
 const both=node(dir,[path.join(TOOLS,'rounds.mjs'),'confirm','1','2','--owner-word','x','--sample',now(dir)]);assert.equal(both.status,1);assert.match(both.stderr,/not both/);
 const none=node(dir,[path.join(TOOLS,'rounds.mjs'),'confirm','1','2']);assert.equal(none.status,1);assert.match(none.stderr,/--owner-word/);});

test('owner attempts replay at whole microseconds without drift',async()=>{const res=readJSON(path.join(ROOT,RESULTS)),a=res.attempts.find(x=>x.level===5&&x.outcome==='jammed');
 const f=await C.ownerFixture(X.levelV1(level(5),C.fingerprints()),a.commands);assert.equal(f.outcome,'jammed');assert.deepEqual(f.drift,[]);assert.deepEqual(f.nearTies,[]);
 assert.ok(f.commands.every(c=>Number.isInteger(c.atUs)&&!c.refused));assert.equal(f.commands[1].atUs,Math.round(a.commands[1].modelTimeMs*1000));});

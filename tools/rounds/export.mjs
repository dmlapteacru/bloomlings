// The exporter of spec 006 (contracts/pipeline-rounds-cli.md, FR-050, FR-051): a confirmed wave of round levels from
// content/rounds/ into the game's formats, with the reference conformance fixtures. Generation stays here, next to the
// reference engine; the game side (core/src/Bloomlings.Pipeline `rounds import / validate`) only reads what this writes.
//
// node tools/rounds/export.mjs <from> <to> --out content/game/rounds/export/L####-L####
//      [--owner-results product/playtests/<pilot>/results.json]... [--game content/game/rounds] [--repack]
//
// Entry checks (each failure named, exit 1, nothing written):
//   every position from–to placed (content/rounds/levels/); every picture approved and not a franchise reference; a level's
//   exception known and well formed; content/rounds/runs/L####-L####/confirmed.json (rounds.mjs confirm); then
//   audit.mjs from to (report → content/rounds/audit-L####-L####.md) with no НЕТ, except one a level's exception names.
// Writes into --out (its levels/, pictures/, proofs/, conformance/ are replaced):
//   levels/L####.json (round-level.v1), pictures/P####.json (round-picture.v1), proofs/L####.json (round-proof.v1),
//   conformance/L####.json (round-conformance.v1), export.json (wave, fingerprints, counts, fixture errors).
// Every written level, picture and proof is checked against its schema. A fixture error (a random-test win count that
// differs from the level's record, an owner attempt whose outcome differs from the results file, a refused owner command,
// a near-tie of model times) is listed in export.json and on the console, and the exit code is 1.
//
// --game: the game's current round content; a level whose definition changed against it gets definitionVersion + 1.
import fs from 'node:fs';import path from 'node:path';import {spawnSync} from 'node:child_process';import {fileURLToPath,pathToFileURL} from 'node:url';
import {root,ROUNDS,LEVELS,PICTURES,readJSON,lid,boardOf,INTRO,CODES,franchiseBlocked} from './lib.mjs';
import {fingerprints,sameEngine,solutionFixture,randomFixture,ownerFixture,engineLevel,sha256} from './conformance.mjs';
import {validate} from './schema.mjs';

const HERE=path.dirname(fileURLToPath(import.meta.url));
export const POLICY='17§5 extended: uniform over legal actions + wait before a state event';
export const FIXTURE_POLICY='17§5 extended';
const MECHANICS=['hidden','links','bridge','cells','latch','canal','shutters'];
// owner decisions behind a level's exception string (content/rounds/levels keeps them as a note); the rule names the
// audit failure it excuses, matched on the audit's own wording
export const OWNER_EXCEPTIONS={
 18:{picture:'P0115',exception:{rule:'one deciding bridge from the second introduction',reason:'owner kept both winning bridges',decidedBy:'owner',decidedAt:'2026-10-27'}}};
export const RULE_PATTERNS={'one deciding bridge from the second introduction':/^мост: решением должен быть ровно один мост/,
 // the calm rule (17 §5, the owner's amendment of 2026-10-31): audit.mjs itself turns an owner's calm exception into a note
 // «исключение владельца от <date>: спокойная игра: …»
 'calm test (17 §5, amendment of 2026-10-31)':/^(исключение владельца от [0-9-]+: )?спокойная игра: /};
// notes the audit already turned into an owner's exception (it reads the level's exception object itself)
const OWNER_NOTE=/^исключение владельца от /;
const rel=f=>path.relative(root,f).split(path.sep).join('/');
const write=(f,o)=>{fs.mkdirSync(path.dirname(f),{recursive:true});fs.writeFileSync(f,JSON.stringify(o,null,1)+'\n');};
// a fixture keeps one window, game or command per line, so a diff shows what moved
export function pretty(v,ind=''){const flat=JSON.stringify(v);if(flat.length<=240||v===null||typeof v!=='object')return flat;const next=ind+' ';
 if(Array.isArray(v))return '[\n'+v.map(x=>next+pretty(x,next)).join(',\n')+'\n'+ind+']';
 return '{\n'+Object.entries(v).map(([k,x])=>next+JSON.stringify(k)+':'+pretty(x,next)).join(',\n')+'\n'+ind+'}';}
const compactWrite=(f,o)=>{fs.mkdirSync(path.dirname(f),{recursive:true});fs.writeFileSync(f,pretty(o)+'\n');};

// ---- the formats ----
export function exceptionOf(L){if(!L.exception)return null;
 if(typeof L.exception==='object')return L.exception;
 const known=OWNER_EXCEPTIONS[L.level];return known&&known.picture===L.picture?known.exception:undefined;}
export function levelV1(L,engine,definitionVersion=1){const d=L.definition,n=L.level,ex=exceptionOf(L);
 const intro=L.intro?INTRO.find(([m])=>m===L.intro):null;
 const recipe=(L.recipe||'none')==='none'?[]:L.recipe.split('+');
 return {format:'round-level.v1',number:n,definitionVersion,class:L.cls,
  ...(intro?{introduction:{mechanic:L.intro,second:intro[2]===n}}:{}),
  picture:{id:L.picture,version:null},
  // a site hidden at the start (17 §8 as amended on 2026-10-31, rounds.mjs bridges) is "shown": false; absent means shown
  board:{w:d.w,h:d.h,rowsBottomFirst:d.rowsBottomFirst,entries:d.entries,...(d.hero?{hero:{id:d.hero.id,choices:d.hero.choices.map((c,i)=>({cell:c.cell,anchor:c.anchor,label:c.label,...(L.bridgeShown&&L.bridgeShown[i]===false?{shown:false}:{})}))}}:{}),
   // the generator's wall over the approved picture (17 §8), picture coordinates: the board is that picture plus this
   ...(d.overlay?{overlay:{walls:(d.overlay.walls||[]).map(c=>[c[0],c[1]]),sites:(d.overlay.sites||[]).map(c=>[c[0],c[1]]),...(d.overlay.keys&&d.overlay.keys.length?{keys:d.overlay.keys.map(c=>[c[0],c[1]])}:{})}}:{}),
   // the level's recolouring of its picture (17 §3, §6 accents, 2026-11-01), picture coordinates, applied before the overlay
   ...(d.recolor&&d.recolor.length?{recolor:d.recolor.map(e=>[e[0],e[1],e[2]])}:{}),
   // hidden cells (17 §8 «Скрытые клетки», 2026-11-01): sorted cell indices of the rows (bottom first, y*w+x)
   ...(d.hiddenCells&&d.hiddenCells.length?{hiddenCells:[...d.hiddenCells].map(Number).sort((a,b)=>a-b)}:{}),
   // the Drop water channel (17 §8, 2026-11-05): the spring and two exits, each with its route (spring … exit) and ride
   ...(d.channel?{channel:{spring:d.channel.spring,exits:d.channel.exits.map(e=>({cell:e.cell,route:e.route.slice(),rideUs:e.rideUs}))}}:{}),
   // the Twig wooden shutters (17 §8, 2026-11-05): each a rectangle of cells, its material and its count
   ...(d.shutters&&d.shutters.length?{shutters:d.shutters.map(s=>({cells:[...s.cells].sort((a,b)=>a-b),code:s.material,count:s.count}))}:{})},
  capacity:d.capacity??5,
  pods:d.pods.map(p=>({code:p.color,count:p.count,...(p.mystery?{mystery:p.mystery}:{}),...(p.group?{group:p.group}:{})})),
  tray:d.tray,
  // latch + acorn key (17 §8, 2026-11-01): {column, depth, key} each, key a cell index of the rows (bottom first, y*w+x)
  ...(d.latches&&d.latches.length?{latches:d.latches.map(l=>({column:l.column,depth:l.depth,key:l.key}))}:{}),
  mechanics:MECHANICS.filter(m=>recipe.includes(m)),...(ex?{exception:ex}:{}),engine};}
export function pictureV1(pic){const tag=k=>typeof pic[k]==='string'&&pic[k]?{[k]:pic[k]}:{};
 return {format:'round-picture.v1',id:pic.id,version:pic.version,title:pic.title,...tag('family'),...tag('direction'),...tag('subject'),...tag('source'),
  w:pic.w,h:pic.h,rows:pic.rows,approval:{status:'approved',by:'owner',decidedAt:pic.decidedAt||pic.approvedAt||null},
  // 'owner': a franchise or meme picture the owner took in by his own decision (pictures.mjs rights --owner); else the project's own
  rights:pic.rights==='owner'?'owner':'own'};}
// the definition without its version and fingerprints, for "did it change since the game's copy"
const definitionKey=lv=>JSON.stringify({...lv,definitionVersion:0,engine:null});

// ---- entry checks ----
export function entryChecks(from,to){const failures=[],levels={},pictures={};
 for(let n=from;n<=to;n++){const f=path.join(LEVELS,lid(n)+'.json'),L=readJSON(f);
  if(!L){failures.push(`not placed: ${lid(n)} (no ${rel(f)})`);continue;}levels[n]=L;
  const pic=readJSON(path.join(PICTURES,L.picture+'.json'));
  if(!pic)failures.push(`picture missing: ${L.picture} of ${lid(n)}`);
  if(L.definition&&L.definition.hero&&!(Array.isArray(L.bridgeShown)&&L.bridgeShown.length===L.definition.hero.choices.length))
   failures.push(`${lid(n)}: which bridge sites it shows is not decided (node tools/rounds/rounds.mjs bridges ${from} ${to}; 17 §8 as amended on 2026-10-31)`);
  else{if(pic.status!=='approved')failures.push(`picture not approved: ${L.picture} of ${lid(n)} (${pic.status})`);
   // a franchise reference only with the owner's confirmed rights (rights 'owner', pictures.mjs rights)
   if(franchiseBlocked(pic))failures.push(`picture is a franchise reference: ${L.picture} of ${lid(n)} (${pic.source})`);
   if(!Number.isInteger(pic.version)||pic.version<1)failures.push(`picture without a version: ${L.picture}`);
   let made=null;try{made=boardOf(pic,L.definition.overlay,L.definition.recolor);}catch(e){failures.push(`board of its picture: ${lid(n)} / ${L.picture}: ${e.message}`);}
   if(made&&made.rowsBottomFirst.join('\n')!==L.definition.rowsBottomFirst.join('\n'))failures.push(`board does not match its picture: ${lid(n)} / ${L.picture}`);
   pictures[L.picture]=pic;}
  const ex=exceptionOf(L);
  if(ex===undefined)failures.push(`unknown exception: ${lid(n)} carries «${L.exception}» but no owner decision is recorded for it (export.mjs OWNER_EXCEPTIONS)`);
  else if(ex&&!RULE_PATTERNS[ex.rule])failures.push(`exception names an unknown rule: ${lid(n)} «${ex.rule}»`);}
 const confirmed=path.join(ROUNDS,'runs',`${lid(from)}-${lid(to)}`,'confirmed.json'),c=readJSON(confirmed);
 if(!c)failures.push(`no owner confirmation: ${rel(confirmed)} (node tools/rounds/rounds.mjs confirm ${from} ${to} --sample <results.json>)`);
 else if(!c.wave||c.wave[0]!==from||c.wave[1]!==to)failures.push(`owner confirmation is for another wave: ${rel(confirmed)}`);
 return {failures,levels,pictures,confirmed:c?rel(confirmed):null};}

// audit.mjs from to: НЕТ fails, except the rule a level's exception names
export function auditCheck(from,to,levels){const report=path.join(ROUNDS,`audit-${lid(from)}-${lid(to)}.md`),json=path.join(ROUNDS,'runs',`${lid(from)}-${lid(to)}`,'audit.json');
 fs.mkdirSync(path.dirname(json),{recursive:true});
 const r=spawnSync(process.execPath,[path.join(HERE,'audit.mjs'),String(from),String(to),'--out',report,'--json',json],{cwd:root,env:process.env,encoding:'utf8',maxBuffer:1<<26});
 const a=readJSON(json);if(!a||a.from!==from||a.to!==to)return {failures:[`audit did not run: ${(r.stderr||'').trim().split('\n').slice(-3).join(' / ')}`]};
 const failures=[],results={};
 for(const row of a.levels){const L=levels[row.level],ex=L?exceptionOf(L):null,pat=ex?RULE_PATTERNS[ex.rule]:null;
  const excused=[...row.bad.filter(b=>pat&&pat.test(b)),...row.soft.filter(b=>pat&&OWNER_NOTE.test(b)&&pat.test(b))],left=row.bad.filter(b=>!excused.includes(b));
  for(const b of left)failures.push(`audit НЕТ: ${lid(row.level)}: ${b}`);
  if(ex&&!excused.length)failures.push(`exception not needed: ${lid(row.level)} names «${ex.rule}», which the audit does not fail`);
  results[row.level]={result:excused.length?'OK with exception':'OK',report:rel(report),notes:[...excused.map(b=>`exception (${ex.rule}): ${b}`),...row.soft.filter(b=>!excused.includes(b))]};}
 for(const s of a.sequence)failures.push(`audit sequence: ${s}`);
 return {failures,results,report:rel(report)};}

// ---- the pilot's catalog hash (tools/gameplay-pilot-v12/build.mjs): did the owner play these very definitions? ----
function pilotCatalogHash(levelsInOrder){return sha256(JSON.stringify(levelsInOrder.map(L=>{const d=L.definition;
 return {id:L.level,title:d.title,subject:L.picture,w:d.w,h:d.h,rowsBottomFirst:d.rowsBottomFirst,entries:d.entries,capacity:d.capacity,pods:d.pods,tray:d.tray,difficulty:L.cls,...(d.hero?{hero:d.hero}:{}),...(d.hiddenCells?{hiddenCells:d.hiddenCells}:{}),...(d.latches?{latches:d.latches}:{}),...(d.channel?{channel:d.channel}:{}),...(d.shutters?{shutters:d.shutters}:{})};})));}

// ---- export ----
// --repack (the owner, 2026-11-02: «не перегенерировать… записать какое-то исключение»): an already transferred wave,
// unchanged, packed again for a newer reference engine (the engine's fingerprints are one per fixture set). The levels are
// not rebuilt and not re-audited against rules made after their transfer: the audit results of their current proofs
// are carried over; everything is still replayed on the current engine (any changed outcome is refused, as always), the
// owner's results need an engine-equivalence record, and the packed levels and pictures must equal the previous
// export's byte for byte (engine fields aside).
function repackAudit(from,to,game){const failures=[],results={},t=(fs.existsSync(path.join(game,'transfers.jsonl'))?fs.readFileSync(path.join(game,'transfers.jsonl'),'utf8').trim().split('\n').filter(Boolean).map(l=>JSON.parse(l)):[]).find(x=>x.wave&&x.wave[0]===from&&x.wave[1]===to);
 if(!t||!t.exportedAt||!t.validatedAt)return {failures:[`repack: L${from}–L${to} was never transferred (no exported + validated record in transfers.jsonl)`]};
 let report=null;for(let n=from;n<=to;n++){const pr=readJSON(path.join(game,'proofs',`${lid(n)}.json`));if(!pr||!pr.audit){failures.push(`repack: no proof with an audit for ${lid(n)} in ${rel(game)}`);continue;}results[n]=pr.audit;report=report||pr.audit.report;}
 return {failures,results,report};}
function stripEngine(o){if(Array.isArray(o))return o.map(stripEngine);if(o&&typeof o==='object'){const r={};for(const k of Object.keys(o))if(k!=='engine')r[k]=stripEngine(o[k]);return r;}return o;}
function repackSame(from,to,out,game){const prev=path.join(game,'export',`${lid(from)}-${lid(to)}`),diffs=[];
 for(const d of ['levels','pictures']){const a=path.join(prev,d),b=path.join(out,d);if(!fs.existsSync(a)){diffs.push(`repack: no previous export ${rel(a)}`);continue;}
  const names=new Set([...fs.readdirSync(a),...fs.readdirSync(b)]);for(const f of names){const x=readJSON(path.join(a,f)),y=readJSON(path.join(b,f));
   if(!x||!y||JSON.stringify(stripEngine(x))!==JSON.stringify(stripEngine(y)))diffs.push(`repack: ${d}/${f} differs from the previous export`);}}
 return diffs;}
export async function exportWave(from,to,{out,ownerResults=[],game=path.join(root,'content/game/rounds'),log=console.log,repack=false}={}){
 if(!(Number.isInteger(from)&&Number.isInteger(to)&&from>=1&&to>=from))return {ok:false,failures:['usage: export.mjs <from> <to> --out <dir>']};
 if(!out)return {ok:false,failures:['--out <dir> is required']};
 const owner=[];for(const f of ownerResults){const res=readJSON(path.resolve(root,f));if(!res||!Array.isArray(res.attempts))return {ok:false,failures:[`owner results unreadable: ${f}`]};owner.push({file:path.resolve(root,f),res});}
 const entry=entryChecks(from,to);if(entry.failures.length)return {ok:false,failures:entry.failures};
 log(repack?`repack ${from}–${to}: audit carried over from the transferred proofs`:`audit ${from}–${to} …`);const audit=repack?repackAudit(from,to,game):auditCheck(from,to,entry.levels);if(audit.failures.length)return {ok:false,failures:audit.failures};
 const engine=fingerprints(),errors=[],dirs=['levels','pictures','proofs','conformance'];
 for(const d of dirs)fs.rmSync(path.join(out,d),{recursive:true,force:true});
 // the owner's results: same engine, same definitions as the pilot played
 for(const o of owner){const r=o.res,same=!sameEngine({rules:r.rulesHash,timeline:r.timelineHash,palette:r.paletteHash,clock:r.clockHash},engine,from,to).length;
  if(!same)errors.push(`owner results ${rel(o.file)}: played on another engine (rules ${String(r.rulesHash).slice(0,12)} …)`);
  const played=[...new Set(r.attempts.map(a=>a.level))].sort((a,b)=>a-b),all=Array.from({length:to-from+1},(_,i)=>from+i);
  o.catalogMatches=JSON.stringify(played)===JSON.stringify(all)?pilotCatalogHash(all.map(n=>entry.levels[n]))===r.catalogHash:null;
  if(o.catalogMatches===false)errors.push(`owner results ${rel(o.file)}: the pilot's catalog hash differs from these definitions`);}
 const written={levels:[],pictures:[],proofs:[],conformance:[]},exceptions=[];let randomGames=0,ownerAttempts=0;
 for(let n=from;n<=to;n++){const L=entry.levels[n],pic=entry.pictures[L.picture],t0=Date.now();
  // the level, versioned against the game's copy
  const lv=levelV1(L,engine);lv.picture.version=pic.version;
  const prev=readJSON(path.join(game,'levels',lid(n)+'.json'));
  if(prev)lv.definitionVersion=definitionKey({...prev})===definitionKey(lv)?prev.definitionVersion:prev.definitionVersion+1;
  if(lv.exception)exceptions.push({level:n,rule:lv.exception.rule});
  const pv=pictureV1(pic),def=lv;
  // the saved solution: the patient replay on the reference
  const sol=await solutionFixture(def,L.solution.route);
  if(sol.outcome!=='Won')errors.push(`${lid(n)}: the saved solution ends ${sol.outcome}`);
  for(const x of sol.nearTies)errors.push(`${lid(n)} solution: near-tie of model times ${x}`);
  const rt=L.random;
  // a bridge level's class test plays its best bridge only (17 §5 as amended on 2026-10-30): the proof and the fixture
  // name that bridge, and the replay (here and in C#) leaves every other bridge out of the bot's moves
  const bridge=rt.bridge??null;
  if(bridge!==null&&!(Number.isInteger(bridge)&&lv.board.hero&&bridge>=0&&bridge<lv.board.hero.choices.length))errors.push(`${lid(n)}: the random test's bridge ${bridge} is not a bridge choice of the level`);
  const proof={format:'round-proof.v1',number:n,definitionVersion:lv.definitionVersion,
   solution:{commands:sol.commands,outcome:'Won',endUs:sol.endUs},
   randomTest:{policy:POLICY,seedBase:rt.seedBase,runs:rt.runs,wins:rt.wins,share:rt.share,wilson95:rt.wilson95,classOfShare:rt.cls,...(rt.borderline!=null?{borderline:rt.borderline}:{}),...(bridge!==null?{bridge}:{})},
   checks:L.checks||{},audit:audit.results[n],engine};
  // the random test replayed game by game (research R5)
  const rnd=await randomFixture(def,{seedBase:rt.seedBase,runs:rt.runs,bridge});randomGames+=rnd.games.length;
  if(rnd.wins!==rt.wins)errors.push(`${lid(n)} random test: ${rnd.wins} of ${rt.runs} won on replay, the level records ${rt.wins}`);
  for(const x of rnd.nearTies)errors.push(`${lid(n)} random game: near-tie of model times ${x}`);
  // the owner's attempts on this level
  const own=[];for(const o of owner)for(const a of o.res.attempts.filter(a=>a.level===n)){const f=await ownerFixture(def,a.commands);ownerAttempts++;
   for(const c of f.commands)if(c.refused)errors.push(`${lid(n)} owner attempt ${a.id} (${rel(o.file)}): command ${JSON.stringify({type:c.type,pod:c.pod,choice:c.choice,exit:c.exit})} at ${c.atUs} µs refused on replay (${c.refused})`);
   if(f.outcome!==a.outcome)errors.push(`${lid(n)} owner attempt ${a.id} (${rel(o.file)}): replay ends ${f.outcome}, the results file says ${a.outcome}`);
   for(const x of f.nearTies)errors.push(`${lid(n)} owner attempt ${a.id}: near-tie of model times ${x}`);
   for(const x of f.drift)errors.push(`${lid(n)} owner attempt ${a.id}: ${x}`);
   own.push({source:rel(o.file),attempt:String(a.id),commands:f.commands,outcome:f.outcome,endUs:f.endUs,finalSignature:f.finalSignature});}
  const fixture={format:'round-conformance.v1',number:n,definitionVersion:lv.definitionVersion,engine,
   solution:{windows:sol.windows.map(w=>({command:w.command,events:w.events,signature:w.signature})),outcome:sol.outcome,endUs:sol.endUs,finalSignature:sol.finalSignature},
   random:{policy:FIXTURE_POLICY,seedBase:rt.seedBase,runs:rt.runs,...(bridge!==null?{bridge}:{}),games:rnd.games,wins:rnd.wins},owner:own};
  // schemas
  for(const [name,doc,label] of [['round-level',lv,`levels/${lid(n)}`],['round-picture',pv,`pictures/${pv.id}`],['round-proof',proof,`proofs/${lid(n)}`]]){
   const e=validate(name,doc);if(e.length)return {ok:false,failures:e.map(x=>`schema ${name}: ${label}${x}`)};}
  const fx=fixtureShape(fixture);if(fx.length)return {ok:false,failures:fx.map(x=>`fixture ${lid(n)}: ${x}`)};
  write(path.join(out,'levels',lid(n)+'.json'),lv);write(path.join(out,'pictures',pv.id+'.json'),pv);write(path.join(out,'proofs',lid(n)+'.json'),proof);
  compactWrite(path.join(out,'conformance',lid(n)+'.json'),fixture);
  written.levels.push(n);written.pictures.push(pv.id);written.proofs.push(n);written.conformance.push(n);
  log(`${lid(n)}: ${sol.commands.length} commands, ${sol.endUs/1e6} s; random ${rnd.wins}/${rt.runs} (record ${rt.wins}); owner ${own.length} (${own.map(x=>x.outcome).join(', ')||'—'}); ${((Date.now()-t0)/1000).toFixed(1)} s`);}
 const manifest={format:'round-export.v1',wave:[from,to],levels:written.levels,pictures:written.pictures,exceptions,engine,
  counts:{levels:written.levels.length,pictures:written.pictures.length,proofs:written.proofs.length,conformance:written.conformance.length,randomGames,ownerAttempts},
  confirmed:entry.confirmed,audit:audit.report,ownerResults:owner.map(o=>({source:rel(o.file),attempts:o.res.attempts.filter(a=>a.level>=from&&a.level<=to).length,catalogMatches:o.catalogMatches})),
  exportedAt:new Date().toISOString(),fixtureErrors:errors};
 write(path.join(out,'export.json'),manifest);
 return {ok:!errors.length,failures:[],errors,manifest};}

// the conformance fixture has no JSON schema; its contract shape is checked here
export function fixtureShape(f){const e=[],sig=/^sha256:[0-9a-f]{64}$/,int=x=>Number.isInteger(x)&&x>=0,cmd=c=>c&&(c.type==='tap'?int(c.pod):c.type==='bridge'?int(c.choice):c.type==='channel'?int(c.exit):false)&&int(c.atUs);
 if(f.format!=='round-conformance.v1')e.push('format');if(!int(f.number)||!int(f.definitionVersion))e.push('number / definitionVersion');
 if(!f.engine||['rules','timeline','palette','clock'].some(k=>!/^[0-9a-f]{64}$/.test(f.engine[k])))e.push('engine');
 const s=f.solution;if(!s||!Array.isArray(s.windows)||!s.windows.length)e.push('solution.windows');
 else s.windows.forEach((w,i)=>{if(!cmd(w.command)||!sig.test(w.events)||!sig.test(w.signature))e.push(`solution.windows[${i}]`);});
 if(!s||!['Won','Jammed','Stuck','Playing'].includes(s.outcome)||!int(s.endUs)||!sig.test(s.finalSignature))e.push('solution end');
 const r=f.random;if(!r||r.policy!==FIXTURE_POLICY||!Number.isInteger(r.seedBase)||![100,400].includes(r.runs)||!Array.isArray(r.games)||r.games.length!==r.runs||!int(r.wins)||(r.bridge!==undefined&&!int(r.bridge)))e.push('random');
 else r.games.forEach((g,i)=>{if(!int(g.seed)||!['Won','Jammed','Stuck','Playing'].includes(g.outcome)||!int(g.endUs)||!sig.test(g.finalSignature))e.push(`random.games[${i}]`);});
 if(!Array.isArray(f.owner))e.push('owner');else f.owner.forEach((o,i)=>{if(typeof o.source!=='string'||typeof o.attempt!=='string'||!Array.isArray(o.commands)||!o.commands.every(cmd)||typeof o.outcome!=='string'||!int(o.endUs)||!sig.test(o.finalSignature))e.push(`owner[${i}]`);});
 return e;}

// ---- command line ----
if(process.argv[1]&&import.meta.url===pathToFileURL(path.resolve(process.argv[1])).href){
 const a=process.argv.slice(2),opt=k=>{const i=a.indexOf(k);return i>=0?a[i+1]:null;},all=k=>a.flatMap((x,i)=>x===k&&a[i+1]?[a[i+1]]:[]);
 const from=+a[0],to=+a[1],out=opt('--out')?path.resolve(opt('--out')):null;
 const game=opt('--game')?path.resolve(opt('--game')):path.join(root,'content/game/rounds'),repack=a.includes('--repack');
 const r=await exportWave(from,to,{out,ownerResults:all('--owner-results'),game,repack});
 if(repack&&!r.failures.length){const d=repackSame(from,to,out,game);if(d.length){console.error(`export ${from}–${to} --repack refused:`);for(const x of d)console.error(`- ${x}`);process.exit(1);}console.log('repack: levels and pictures equal the previous export (engine fields aside)');}
 if(r.failures.length){console.error(`export ${from}–${to} refused:`);for(const f of r.failures)console.error(`- ${f}`);process.exit(1);}
 const m=r.manifest;console.log(`→ ${rel(out)}: ${m.counts.levels} levels, ${m.counts.pictures} pictures, ${m.counts.proofs} proofs, ${m.counts.conformance} fixtures (${m.counts.randomGames} random games, ${m.counts.ownerAttempts} owner attempts)`);
 if(r.errors.length){console.error(`fixture errors (${r.errors.length}):`);for(const e of r.errors)console.error(`- ${e}`);process.exit(1);}}

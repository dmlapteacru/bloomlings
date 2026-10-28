// The reference side of the conformance gate (spec 006 FR-030–FR-033, contracts/conformance-fixture.md): the round
// engine of tools/rounds/engine/ played on the game's round-level.v1 definitions, with the canonical event log, the
// extended state signature and the §5 random policy written out exactly as the C# port (Bloomlings.Core.Rounds) must
// reproduce them. The engine files are only read here, never changed: their SHA-256 are the fingerprints.
//
// Times: the engine counts model milliseconds as doubles; everything written here is integer microseconds,
// us(ms) = Math.round(ms × 1000). A command at atUs is fed to the engine as atUs / 1000.
//
// Readings of the contract taken here (spec 006 contracts/conformance-fixture.md), so both sides agree:
// - window k = command k's commit events, then everything the timeline emits until command k+1 is committed (the
//   last window runs until nothing is pending); `signature` of a window is taken right after its command is committed
//   (after the commit's own dispatch, before time moves on); the end state is `finalSignature`;
// - commit, reveal and bridge lines carry the timeline's time at the commit;
// - depart `route=` is the route's cell count (entry … target, both included), the pilot's routeLength;
// - a step line's time is the step's own time; complete carries the release time;
// - `steps` in the signature are `atUs:type:idOrPod`, check steps included, in the timeline's step order;
// - the engine's clock does not move while nothing is pending: a command given while the timeline is idle is committed
//   at the time of the last processed step (that is also what the pilot recorded as modelTimeMs).
import fs from 'node:fs';import path from 'node:path';import crypto from 'node:crypto';
import {fileURLToPath} from 'node:url';
import {engine} from './engine.mjs';

const HERE=path.dirname(fileURLToPath(import.meta.url));
export const ENGINE_FILES={rules:'engine/rules.cjs',timeline:'engine/timeline.cjs',palette:'engine/palette.cjs',clock:'engine/clock.cjs'};
export const sha256=x=>crypto.createHash('sha256').update(x).digest('hex');
// the reference fingerprints (research R12): SHA-256 of each engine file's bytes
// engine fingerprints the owner recorded as playing the listed levels identically (content/rounds/engine-equivalence.json)
export function sameEngine(played,current,from,to){const rec=(()=>{try{return JSON.parse(fs.readFileSync(path.join(HERE,'../../content/rounds/engine-equivalence.json'),'utf8')).entries||[];}catch{return [];}})();
 return Object.keys(current).filter(k=>played[k]!==current[k]&&!rec.some(e=>e.part===k&&e.from===played[k]&&e.to===current[k]&&e.levels[0]<=from&&to<=e.levels[1]));}
export function fingerprints(){return Object.fromEntries(Object.entries(ENGINE_FILES).map(([k,f])=>[k,sha256(fs.readFileSync(path.join(HERE,f)))]));}

// model ms (double) → integer µs; a value that is not within 1 ns of a whole µs means a time was not fed as whole µs
export function us(ms){const v=ms*1000,r=Math.round(v);if(!Number.isFinite(v)||Math.abs(v-r)>1e-3)throw Error(`time ${ms} ms is not a whole µs`);return r;}

// the engine's level object from a round-level.v1 definition (pods carry `code` on the wire, `color` in the engine)
export function engineLevel(def){return {id:def.number,w:def.board.w,h:def.board.h,rowsBottomFirst:def.board.rowsBottomFirst,entries:def.board.entries,capacity:def.capacity,
 pods:def.pods.map(p=>({color:p.code,count:p.count,...(p.mystery?{mystery:p.mystery}:{}),...(p.group?{group:p.group}:{})})),tray:def.tray,...(def.board.hero?{hero:def.board.hero}:{})};}

// ---- canonical event log (contract table) ----
// commit, reveal and bridge come from Timeline.commit's result; depart, arrive, clear, return and complete from the
// timeline's lastEvents (assign is left out: it always precedes its depart; check steps emit nothing)
export function lineOf(e,nowUs){
 switch(e.type){
  case 'commit':return `${nowUs} commit pod=${e.pod} slot=${e.slot}`;
  case 'reveal':return `${nowUs} reveal pod=${e.pod} mode=${e.mode}`;
  case 'bridge':return `${nowUs} bridge cell=${e.cell}`;
  case 'depart':{const [id,pod,cell,,start,arrive,clear,done,routeLength]=e.plan;return `${us(start)} depart id=${id} pod=${pod} cell=${cell} arrive=${us(arrive)} clear=${us(clear)} done=${us(done)} route=${routeLength}`;}
  case 'arrive':return `${us(e.at)} arrive id=${e.id}`;
  case 'clear':return `${us(e.at)} clear id=${e.id} cell=${e.jobCell}`;
  case 'return':return `${us(e.at)} return id=${e.id}`;
  case 'complete':return `${us(e.at)} complete pod=${e.pod}`;
  default:return null;}}
export const hashLines=lines=>'sha256:'+sha256(lines.map(l=>l+'\n').join(''));
export const hashText=s=>'sha256:'+sha256(s);

// the extended signature: rules.cjs signature(state) + |now|steps
export function signatureOf(E,t){const steps=t.steps.map(e=>`${us(e.at)}:${e.type}:${e.id??e.pod}`).join(',');return `${E.R.signature(t.state)}|${us(t.now)}|${steps}`;}

// xorshift32 of research R5 (= tools/level-stats/strategies.mjs rng): seed 0 → 1, r = s / 2^32
export function rng(seed){let s=seed>>>0||1;return()=>{s^=s<<13;s^=s>>>17;s^=s<<5;return (s>>>0)/4294967296;};}
export const gameSeed=(seedBase,n)=>seedBase*7919+n*104729+1;
const isStateStep=e=>e.type!=='return'&&e.type!=='arrive';

// A timeline that records the canonical log and watches for near-ties: two pending steps whose times differ by less
// than 1 ns but are not equal would be one moment in integer µs and two moments in the engine's doubles. None may occur.
async function recorder(def){const E=await engine(),{R,T}=E;
 class Recorded extends T.Timeline{
  sort(){super.sort();if(!this.nearTies)this.nearTies=[];for(let i=1;i<this.steps.length;i++){const d=this.steps[i].at-this.steps[i-1].at;if(d!==0&&Math.abs(d)<1e-6)this.nearTies.push([this.steps[i-1],this.steps[i]].map(e=>`${e.at}:${e.type}:${e.id??e.pod}`).join(' ~ '));}}}
 const level=engineLevel(def),t=new Recorded(R.create(level,level.capacity||5));t.nearTies=[];
 let lines=[];const cellOf=new Map();
 // a clear's cell is its courier's target, known from the depart (the courier may be gone when the log is read)
 const take=()=>{for(const e of t.lastEvents){if(e.type==='depart')cellOf.set(e.id,e.plan[2]);
   if(e.type==='clear'&&!cellOf.has(e.id))throw Error(`clear without courier ${e.id}`);
   const l=lineOf(e.type==='clear'?{...e,jobCell:cellOf.get(e.id)}:e,null);if(l)lines.push(l);}};
 return {E,R,T,t,level,
  lines:()=>lines,cut:()=>{const l=lines;lines=[];return l;},
  commit(action){const r=t.commit(action);if(!r.accepted)return r;const now=us(t.now);for(const e of r.events)lines.push(lineOf(e,now));take();return r;},
  // advance by at most `ms` of model time (the engine's own advance: nothing moves while nothing is pending)
  advance(ms){t.advance(Math.max(0,ms));take();},
  advanceTo(ms){if(t.pending&&ms>t.now)this.advance(ms-t.now);},
  settle(){while(t.pending){t.advance(1e9);take();}},
  signature:()=>signatureOf(E,t)};}

// ---- the saved solution: the patient replay, each command when all earlier work has settled ----
// route: [{type:'tap',pod}|{type:'bridge',choice}] → commands with atUs, the windows, outcome, endUs, final signature
export async function solutionFixture(def,route){const X=await recorder(def),{t}=X,windows=[],commands=[];
 for(const a of route){X.settle();if(windows.length)windows.at(-1).lines.push(...X.cut());
  const atUs=us(t.now),action=a.type==='bridge'?{type:'bridge',choice:a.choice}:{type:'tap',pod:a.pod};
  const r=X.commit(action);if(!r.accepted)throw Error(`L${def.number}: solution command ${commands.length+1} ${JSON.stringify(action)} refused (${r.reason})`);
  const command={...action,atUs};commands.push(command);
  windows.push({command,lines:X.cut(),signature:hashText(X.signature())});}
 X.settle();windows.at(-1).lines.push(...X.cut());
 for(const w of windows)w.events=hashLines(w.lines);
 return {commands,windows,outcome:t.state.status,endUs:us(t.now),finalSignature:hashText(X.signature()),nearTies:t.nearTies};}

// ---- one game of the §5 extended random policy (measure.mjs randomGame, research R5), step for step ----
// loop: settle the moment (process every step due at now); Won → stop; moves = R.legal (for each column in order its
// top pod if tappable, given as its group's first member, each group once; then every buildable bridge by choice
// index); canWait = some pending step is not arrive/return; no move: wait if anything is pending, else stop;
// k = floor(r × (moves + canWait)); k = moves.length means wait: repeat { e = first pending step; advance to e.at
// (that whole moment); stop if e is not arrive/return }, then settle the moment.
export async function randomGameFixture(def,seed){const X=await recorder(def),{R,t}=X,r=rng(seed);let guard=0;
 const settleNow=()=>{while(t.pending&&t.steps[0].at<=t.now+1e-7)X.advance(0);};
 const waitEvent=()=>{while(t.pending){const e=t.steps[0];X.advance(Math.max(0,e.at-t.now));if(isStateStep(e))break;}settleNow();};
 while(guard++<200000){settleNow();if(t.state.status==='Won')break;
  const moves=R.legal(t.state).filter(a=>!t.check(a)),canWait=t.steps.some(isStateStep);
  if(!moves.length){if(t.pending){waitEvent();continue;}break;}
  const k=Math.floor(r()*(moves.length+(canWait?1:0)));
  if(k===moves.length){waitEvent();continue;}
  X.commit(moves[k]);X.cut();}
 X.cut();return {seed,outcome:t.state.status,endUs:us(t.now),finalSignature:hashText(X.signature()),nearTies:t.nearTies};}

export async function randomFixture(def,{seedBase,runs}){const games=[],nearTies=[];let wins=0;
 for(let n=0;n<runs;n++){const g=await randomGameFixture(def,gameSeed(seedBase,n));if(g.outcome==='Won')wins++;nearTies.push(...g.nearTies);delete g.nearTies;games.push(g);}
 return {games,wins,nearTies};}

// ---- an owner attempt: commands at atUs = round(modelTimeMs × 1000), fed back as atUs / 1000 ----
// `drift` lists commands the timeline committed at another µs than atUs (it would mean the engine's clock stood still
// or ran past the recorded moment); the pilot recorded modelTimeMs as the timeline's time at the commit, so none may occur
export async function ownerFixture(def,commands){const X=await recorder(def),{t}=X,out=[],drift=[];
 for(const c of commands){const atUs=Math.round(c.modelTimeMs*1000),action=c.type==='bridge'?{type:'bridge',choice:c.choice}:{type:'tap',pod:c.pod};
  X.advanceTo(atUs/1000);if(us(t.now)!==atUs)drift.push(`${JSON.stringify(action)} at ${atUs} µs committed at ${us(t.now)} µs`);
  const r=X.commit(action);X.cut();
  out.push({...action,atUs,...(r.accepted?{}:{refused:r.reason})});}
 X.settle();X.cut();
 return {commands:out,outcome:t.state.status.toLowerCase(),endUs:us(t.now),finalSignature:hashText(X.signature()),nearTies:t.nearTies,drift};}

// ---- for debugging a port: the reference's own logs and signature strings ----
// node tools/rounds/conformance.mjs solution <export>/levels/L####.json [--window k]   (route from ../proofs/L####.json)
// node tools/rounds/conformance.mjs random <export>/levels/L####.json <seed>
export async function solutionTrace(def,route){const X=await recorder(def),{t}=X,out=[];
 for(const a of route){X.settle();if(out.length)out.at(-1).lines.push(...X.cut());const r=X.commit(a.type==='bridge'?{type:'bridge',choice:a.choice}:{type:'tap',pod:a.pod});
  if(!r.accepted)throw Error(`refused ${JSON.stringify(a)}: ${r.reason}`);out.push({command:a,lines:X.cut(),signature:X.signature()});}
 X.settle();out.at(-1).lines.push(...X.cut());return {windows:out,final:X.signature()};}
if(process.argv[1]&&import.meta.url===(await import('node:url')).pathToFileURL(path.resolve(process.argv[1])).href){
 const [mode,file,x]=process.argv.slice(2),def=JSON.parse(fs.readFileSync(file,'utf8')),k=process.argv.includes('--window')?+process.argv[process.argv.indexOf('--window')+1]:null;
 if(mode==='solution'){const proof=JSON.parse(fs.readFileSync(path.join(path.dirname(file),'..','proofs',path.basename(file)),'utf8'));
  const tr=await solutionTrace(def,proof.solution.commands);
  tr.windows.forEach((w,i)=>{if(k!=null&&k!==i+1)return;console.log(`# window ${i+1}: ${JSON.stringify(w.command)}  events ${hashLines(w.lines)}`);for(const l of w.lines)console.log(l);console.log(`# signature ${hashText(w.signature)}\n${w.signature}`);});
  if(k==null)console.log(`# final ${hashText(tr.final)}\n${tr.final}`);}
 else if(mode==='random'){const g=await randomGameFixture(def,+x);delete g.nearTies;console.log(JSON.stringify(g));}
 else console.log('node tools/rounds/conformance.mjs solution <levels/L####.json> [--window k] | random <levels/L####.json> <seed>');}

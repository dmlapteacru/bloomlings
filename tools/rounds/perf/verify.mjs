// The identity proof of the fast twin (tools/rounds/perf/README.md): the reference engine (tools/rounds/engine/) and the
// fast twin (fast-rules.cjs, fast-timeline.cjs, fast-idle.cjs) play the same games and every event, every state and every
// step list must be the same, byte for byte. Per level:
// - the saved solution replayed patiently (all work settled before each command, as measure.mjs solve) and tapped as
//   soon as each command is accepted: per command the commit's events, then after every advance the timeline's events,
//   its clears, its step list, every courier's route and the extended state signature, hashed;
// - the games of the recorded random test (measure.mjs randomGame, the recorded seed base and number of games), each
//   hashed the same way, and the wins compared with the recorded result;
// - calm games (the idle engine, measure.mjs calmTest): the signature after every step;
// - the solver (level-stats/solve.mjs route, budget 200000): the route and the nodes searched.
//   node tools/rounds/perf/verify.mjs [--levels 1-50] [--bank 40] [--fixtures] [--games N] [--calm 20] [--no-solver]
// --bank k takes k bank candidates spread over content/rounds/bank (fixed order), --games caps the random games per level.
import fs from 'node:fs';import path from 'node:path';import {fileURLToPath} from 'node:url';
import {solver} from '../../level-stats/solve.mjs';
import {REF,FAST,replay,randomGame,calmGame} from './identity.mjs';
const HERE=path.dirname(fileURLToPath(import.meta.url)),ROOT=path.resolve(HERE,'../../..');
const ROUNDS=process.env.ROUNDS_DIR?path.resolve(process.env.ROUNDS_DIR):path.join(ROOT,'content/rounds');
const arg=k=>{const i=process.argv.indexOf(k);return i>=0?process.argv[i+1]:null;};
const [a,b]=(arg('--levels')||'1-50').split('-').map(Number),bankN=+(arg('--bank')||0),capGames=arg('--games')?+arg('--games'):Infinity,calmN=+(arg('--calm')||20);

const items=[];
for(let n=a;n<=b;n++){const f=path.join(ROUNDS,'levels',`L${String(n).padStart(4,'0')}.json`);if(!fs.existsSync(f))continue;const L=JSON.parse(fs.readFileSync(f,'utf8'));
 items.push({name:`L${n}`,level:L.definition,route:L.solution&&L.solution.route,random:L.random});}
if(bankN){const all=[];for(const f of fs.readdirSync(path.join(ROUNDS,'bank')).sort()){const B=JSON.parse(fs.readFileSync(path.join(ROUNDS,'bank',f),'utf8'));for(const c of B.candidates||[])all.push(c);}
 const step=all.length/bankN;for(let k=0;k<bankN&&k*step<all.length;k++){const c=all[Math.floor(k*step)];items.push({name:c.id,level:c.level,route:c.solution&&c.solution.route,random:c.random});}}
// the hidden-cell, latch, channel and shutter cases of the C# conformance fixture (small levels and placed levels given cells or latches by a
// fixed rule; export-reference.cjs), each with its route and 40 random games
if(process.argv.includes('--fixtures')){const F=JSON.parse(fs.readFileSync(path.join(ROOT,'core/tests/Bloomlings.Core.Tests/Rounds/Fixtures/reference-rounds.json'),'utf8'));
 for(const c of [...F.cells,...F.latches,...(F.channels||[]),...(F.shutters||[])])items.push({name:'fixture '+c.name,level:{id:1,...c.definition},route:c.route,random:{seedBase:1,runs:40,wins:null}});}

let bad=0,games=0,calms=0,routes=0,solved=0;const t0=performance.now();
const fail=(it,what,x,y)=>{bad++;console.log(`DIFFERENT ${it.name} ${what}: reference ${JSON.stringify(x)} fast ${JSON.stringify(y)}`);};
for(const it of items){const {level,random}=it;let tr=0,tf=0;
 if(it.route)for(const patient of [true,false]){const x=replay(REF,level,it.route,patient),y=replay(FAST,level,it.route,patient);routes++;if(JSON.stringify(x)!==JSON.stringify(y))fail(it,patient?'solution':'solution (fast taps)',x,y);}
 if(random){const seed=random.seedBase||1,bridge=random.bridge??null,keep=bridge===null?null:a=>a.type!=='bridge'||a.choice===bridge,runs=Math.min(random.runs,capGames);let wins=0;
  for(let n=0;n<runs;n++){const s=seed*7919+n*104729+1;let q=performance.now();const x=randomGame(REF,level,s,keep);tr+=performance.now()-q;q=performance.now();const y=randomGame(FAST,level,s,keep);tf+=performance.now()-q;games++;
   if(x.won!==y.won||x.hash!==y.hash)fail(it,`random game ${n}`,x,y);if(x.won)wins++;}
  if(runs===random.runs&&random.wins!==null&&wins!==random.wins)console.log(`note ${it.name}: the recorded random test has ${random.wins} wins, the reference plays ${wins} now`);}
 for(let n=0;n<calmN;n++){const s=3*7919+n*104729+5,bridge=random&&random.bridge!=null?random.bridge:null,x=calmGame(REF,level,s,bridge),y=calmGame(FAST,level,s,bridge);calms++;if(x.hash!==y.hash)fail(it,`calm game ${n}`,x,y);}
 if(!process.argv.includes('--no-solver')){const x=solver(REF,200000).route(level,level.capacity||5),y=solver(FAST,200000).route(level,level.capacity||5);solved++;if(JSON.stringify(x)!==JSON.stringify(y))fail(it,'solver',{win:x.win,nodes:x.nodes},{win:y.win,nodes:y.nodes});}
 console.log(`${it.name.padEnd(28)} ok so far: ${bad?bad+' differences':'identical'}; random games reference ${Math.round(tr)} ms, fast ${Math.round(tf)} ms`);}
console.log(`\n${items.length} levels: ${routes} solution replays, ${games} random games, ${calms} calm games, ${solved} solver runs; ${bad} differences (${Math.round((performance.now()-t0)/1000)} s)`);
if(bad)process.exitCode=1;

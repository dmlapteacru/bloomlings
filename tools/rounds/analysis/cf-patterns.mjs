// Every winning tap pattern of a Colony Flow round (the owner, 2026-10-30): the player only ever chooses a top pod, so a
// pattern is the sequence of chosen columns ("1 1 2 3"); a linked group goes in one tap whichever column is pressed, so it
// is written once as its sorted columns ("1-3"), never twice for "1-3" and "3-1". Model: the CF engine (tools/level-stats/
// cf-engine) with the player waiting for the board to settle between taps (idle-rules: the live walkers), which replays
// the owner's recorded wins; the engine's instant settle (R.step) clears other cells and disagrees on many sequences.
// States are memoised, so the count is exact (BigInt) without walking every sequence; it suits small rounds only (L20,
// L25, L26 have 1e11–1e15 winning patterns and millions of states: for them see cf-pattern-tree.mjs).
// node tools/rounds/analysis/cf-patterns.mjs <CF-key> [--list N] [--out <dir>]
//   writes <dir>/<key>.json (counts, the winning first moves, the forced moves, the first patterns) and, when there are at
//   most N patterns, all of them to <dir>/<key>-patterns.txt.gz (one a line)
const ROOT=new URL('../../../',import.meta.url).pathname;
const fs=await import('node:fs'),path=await import('node:path'),crypto=await import('node:crypto'),zlib=await import('node:zlib');
const {allRecords}=await import(ROOT+'tools/level-stats/levels.mjs');const {engine}=await import(ROOT+'tools/level-stats/engines.mjs');
const E=await engine('CF'),R=E.R,STEP=E.idle.step;
const arg=(k,d)=>{const i=process.argv.indexOf(k);return i>0?process.argv[i+1]:d;};
const key=process.argv[2],LIST=+arg('--list',20000),OUT=arg('--out',ROOT+'product/audits/colony-flow-patterns');
const rec=allRecords().find(r=>r.key===key);if(!rec||!rec.model)throw Error('no CF model '+key);
const level=rec.model,cap=rec.capacity||5;
// the label of a tap: the 1-based columns of the pods it takes, sorted ("2", "1-3"; a pair stacked in one column "2-2")
function label(state,move){const cols=state.meta.groups[move.pod].map(p=>level.tray.findIndex(s=>s.includes(p))+1).sort((a,b)=>a-b);return cols.join('-');}
// the memo key: R.signature without the place numbers (which place holds a pod never matters, only the pods' order of
// arrival), hashed to 20 bytes so millions of states fit in memory
const keyOf=state=>{const active=Array.from(state.slots).filter(p=>p>=0).sort((a,b)=>state.ages[a]-state.ages[b]);
 return crypto.createHash('sha1').update(state.board).update(Int16Array.from(state.remaining)).update(state.heads).update(active.join(',')+'|'+state.status).update(state.revealed).update(state.objN).digest('base64');};
const memo=new Map();let visited=0;const t0=Date.now();
function solve(state){
 if(state.status==='Won')return 1n;if(state.status!=='Playing')return 0n;
 const sig=keyOf(state);const m=memo.get(sig);if(m)return m.n;
 visited++;if(visited%20000===0)process.stderr.write(`  ${visited} states, ${((Date.now()-t0)/1000).toFixed(0)} s\n`);
 const kids=[];let n=0n;
 for(const mv of R.legal(state)){if(mv.type!=='tap')continue;const r=STEP(state,mv);if(!r.accepted)continue;const k=solve(r.state);if(k>0n)kids.push([label(state,mv),r.state.status==='Won'?null:keyOf(r.state),k,r.state.status==='Won']);n+=k;}
 memo.set(sig,n>0n?{n,kids}:ZERO);return n;}
const ZERO={n:0n,kids:[]};
const s0=R.create(level,cap),total=solve(s0);
// all sequences when few enough; else the counts by prefix
function* walk(sig,prefix){const m=memo.get(sig);for(const [lab,ks,,won] of m.kids){const p=[...prefix,lab];if(won)yield p;else yield* walk(ks,p);}}
const sig0=keyOf(s0),patterns=total<=BigInt(LIST)?[...walk(sig0,[])].map(p=>p.join(' ')):null;
// the decision points along all winning patterns: how many states offer one winning tap (forced), how many several
let forced=0,choice=0;for(const [,v] of memo)if(v.n>0n){if(v.kids.length===1)forced++;else choice++;}
const firsts=memo.get(sig0).kids.map(([lab,,k])=>({tap:lab,patterns:String(k)}));
const taps=patterns?patterns.map(p=>p.split(' ').length):null;
const res={key,level:rec.level,difficulty:rec.difficulty,model:'CF engine, idle (the player waits for the board to settle)',capacity:cap,columns:level.tray.length,pods:level.pods.length,
 linked:level.pods.filter(p=>p.group).length,hidden:level.pods.filter(p=>p.mystery).length,winningPatterns:String(total),statesWithWin:[...memo.values()].filter(v=>v.n>0n).length,statesVisited:visited,
 allSequencesWin:[...memo.values()].every(v=>v.n>0n),decisions:{forced,choice},firstTaps:firsts,tapsPerPattern:taps?{min:taps.reduce((a,b)=>Math.min(a,b)),max:taps.reduce((a,b)=>Math.max(a,b))}:null,seconds:Math.round((Date.now()-t0)/1000),
 patternsFile:patterns?key+'-patterns.txt.gz':null,firstPatterns:patterns?patterns.slice(0,50):null};
fs.mkdirSync(OUT,{recursive:true});fs.writeFileSync(path.join(OUT,key+'.json'),JSON.stringify(res,null,1));
if(patterns)fs.writeFileSync(path.join(OUT,key+'-patterns.txt.gz'),zlib.gzipSync(patterns.join('\n')+'\n'));
console.log(JSON.stringify({...res,firstPatterns:undefined,listed:patterns?patterns.length:'not listed (more than '+LIST+')'},null,1));

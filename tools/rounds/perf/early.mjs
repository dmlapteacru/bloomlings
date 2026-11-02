// Early stopping of the random test (measure.mjs randomTest early: true) against the full §5 test (100 games, 400 when
// the Wilson interval crosses a class border), on placed levels and bank candidates (tools/rounds/perf/README.md). The
// full test's games are played once (the pool, in index order); the sequential rule is replayed on their prefix, so both
// see the same games. Reports per item the full and the early class, runs and share, and in total the agreement and the
// games saved; also checks that the full test reproduces each item's recorded result (runs and wins).
//   node tools/rounds/perf/early.mjs [--levels 1-50] [--bank all|N] [--step 20] [--min 40] [--out file.json]
import fs from 'node:fs';import path from 'node:path';import {fileURLToPath} from 'node:url';
import {wilson,classOfShare,crossesAny} from '../measure.mjs';
import {games} from './pool.mjs';

const HERE=path.dirname(fileURLToPath(import.meta.url)),ROOT=path.resolve(HERE,'../../..');
const ROUNDS=process.env.ROUNDS_DIR?path.resolve(process.env.ROUNDS_DIR):path.join(ROOT,'content/rounds');
const arg=(k,d=null)=>{const i=process.argv.indexOf(k);return i>=0?process.argv[i+1]:d;};
const [a,b]=(arg('--levels','1-50')).split('-').map(Number),bankArg=arg('--bank','all'),step=+arg('--step',20),min=+arg('--min',40);
const BORDERS=[.05,.2,.5];

const items=[];
for(let n=a;n<=b;n++){const f=path.join(ROUNDS,'levels',`L${String(n).padStart(4,'0')}.json`);if(!fs.existsSync(f))continue;const L=JSON.parse(fs.readFileSync(f,'utf8'));items.push({name:`L${n}`,level:L.definition,random:L.random,placed:true});}
if(bankArg!=='0'){const all=[];for(const f of fs.readdirSync(path.join(ROUNDS,'bank')).sort()){const B=JSON.parse(fs.readFileSync(path.join(ROUNDS,'bank',f),'utf8'));for(const c of B.candidates||[])if(c.random&&c.level)all.push({name:c.id,level:c.level,random:c.random,placed:false});}
 if(bankArg==='all')items.push(...all);else{const k=+bankArg,st=all.length/k;for(let i=0;i<k&&Math.floor(i*st)<all.length;i++)items.push(all[Math.floor(i*st)]);}}

// the sequential rule on a list of game results: the first checkpoint where the interval crosses no class border
function early(res){const points=new Set([100,400]);for(let n=min;n<400;n+=step)points.add(n);
 for(const n of [...points].sort((x,y)=>x-y)){if(n>res.length)break;const w=res.slice(0,n).filter(Boolean).length;if(!crossesAny(BORDERS,wilson(w,n))||n===res.length&&n===400)return {runs:n,wins:w};}
 return null;}
const rows=[];let agree=0,full=0,saved=0,mismatchRecorded=0,unresolved=0;const t0=performance.now();
for(const it of items){const seed=it.random.seedBase||1,bridge=it.random.bridge??null;
 const res=await games('random',it.level,{seed,bridge},{count:400,checkpoints:[100],stop:r=>!crossesAny(BORDERS,wilson(r.filter(Boolean).length,r.length))});
 const fw=res.filter(Boolean).length,fc=classOfShare(fw/res.length),e=early(res);
 if(!e){unresolved++;continue;}
 const ec=classOfShare(e.wins/e.runs),same=ec===fc;agree+=same;full+=res.length;saved+=res.length-e.runs;
 if(res.length!==it.random.runs||fw!==it.random.wins)mismatchRecorded++;
 rows.push({name:it.name,placed:it.placed,full:{runs:res.length,wins:fw,cls:fc},early:{runs:e.runs,wins:e.wins,cls:ec,share:Math.round(e.wins/e.runs*1000)/1000},recorded:{runs:it.random.runs,wins:it.random.wins},same});
 if(!same)console.log(`DIFFERENT CLASS ${it.name}: full ${fc} ${fw}/${res.length}, early ${ec} ${e.wins}/${e.runs}`);}
const n=rows.length,placed=rows.filter(r=>r.placed),byRuns={};for(const r of rows){const k=`${r.full.runs}→${r.early.runs}`;byRuns[k]=(byRuns[k]||0)+1;}
const sum={items:n,placed:placed.length,bank:n-placed.length,step,min,agree,agreement:Math.round(agree/n*10000)/100,gamesFull:full,gamesEarly:full-saved,saved,savedShare:Math.round(saved/full*1000)/10,
 placedAgree:placed.filter(r=>r.same).length,differing:rows.filter(r=>!r.same).map(r=>({name:r.name,full:r.full,early:r.early})),meanAbsShareDiff:Math.round(rows.reduce((s,r)=>s+Math.abs(r.early.share-r.full.wins/r.full.runs),0)/n*1000)/1000,
 recordedMismatch:mismatchRecorded,unresolved,seconds:Math.round((performance.now()-t0)/1000),byRuns};
console.log(JSON.stringify(sum,null,1));
if(arg('--out'))fs.writeFileSync(arg('--out'),JSON.stringify({summary:sum,rows},null,1)+'\n');

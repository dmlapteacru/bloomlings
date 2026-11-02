// Parallel games for the measures (tools/rounds/perf/README.md; the owner, 2026-11-02). The games of one test are
// independent (games.mjs playOne: each depends only on the level, the test's parameters and its own index), so they are
// played on worker threads and put back in index order: a test's result is the same, byte for byte, on any number of
// threads, and the same as in one thread.
//   ROUNDS_THREADS=n   worker threads (default: the machine's cores − 1, at least 1; 0 plays every game in this thread)
// A test may stop at checkpoints (games(...).checkpoints with stop): the decision at checkpoint n sees exactly games
// 0…n−1, whatever the threads computed beyond it (those results are dropped).
import {Worker} from 'node:worker_threads';import os from 'node:os';import crypto from 'node:crypto';
import {playOne} from './games.mjs';
import {engine} from './engines.mjs';

const cores=typeof os.availableParallelism==='function'?os.availableParallelism():os.cpus().length;
export const threads=()=>{const v=process.env.ROUNDS_THREADS;return v!==undefined&&v!==''&&Number.isFinite(+v)?Math.max(0,Math.floor(+v)):Math.max(1,cores-1);};

let pool=null,jobSeq=0;
const queue=[],jobs=new Map();
function startPool(n){
 pool=[];for(let k=0;k<n;k++){const w=new Worker(new URL('./worker.mjs',import.meta.url),{env:process.env});w.unref();
  const me={w,busy:null,levels:new Set()};
  w.on('message',m=>{const task=me.busy;me.busy=null;
   const job=jobs.get(m.job);if(job){if(m.error)job.fail(new Error(m.error));else job.done(m.from,m.results);}
   if(task)next();});
  w.on('error',e=>{const task=me.busy;me.busy=null;const job=task&&jobs.get(task.job);if(job)job.fail(e);pool=pool.filter(x=>x!==me);if(!pool.length){for(const t of queue.splice(0)){const j=jobs.get(t.job);if(j)j.fail(e);}}else next();});
  pool.push(me);}
}
// hand queued tasks to idle workers; a worker holds the process open only while it works
function next(){for(const me of pool){if(me.busy)continue;let t=null;while(queue.length){const c=queue.shift();if(jobs.has(c.job)){t=c;break;}}
  if(!t){me.w.unref();continue;}
  me.busy=t;me.w.ref();const job=jobs.get(t.job),send={job:t.job,key:job.key,kind:job.kind,params:job.params,from:t.from,to:t.to,reference:job.reference};
  if(!me.levels.has(job.key)){if(me.levels.size>=64){me.levels.clear();send.reset=true;}send.level=job.json;me.levels.add(job.key);}
  me.w.postMessage(send);}}

// play games of `kind` on `level`: indices 0…count−1, or with checkpoints (ascending, the last one = count) until
// stop(results) answers true at one; returns the results of the games up to that checkpoint, in index order
export async function games(kind,level,params,{count,checkpoints=null,stop=null,chunk=null}){
 const points=checkpoints&&checkpoints.length?checkpoints.filter(x=>x<=count):[count];if(points.at(-1)!==count)points.push(count);
 const n=threads();
 if(n===0||count<=1){const E=await engine(),out=[];
  for(const p of points){while(out.length<p)out.push(playOne(E,level,kind,params,out.length));if(stop&&stop(out))break;}
  return out;}
 if(!pool)startPool(n);
 const json=JSON.stringify(level),key=crypto.createHash('sha1').update(json).digest('hex'),id=++jobSeq;
 const size=chunk||Math.max(1,Math.min(4,Math.floor(count/(pool.length*8))||1)),ahead=pool.length*size*2;
 const reference=process.env.ROUNDS_ENGINE==='reference';
 return new Promise((resolve,reject)=>{const res=new Array(count);let issued=0,pi=0,ready=0;
  const issue=()=>{const limit=Math.min(count,points[pi]+ahead);while(issued<limit){const to=Math.min(limit,issued+size);queue.push({job:id,from:issued,to});issued=to;}next();};
  const finish=v=>{jobs.delete(id);resolve(v);};
  const job={key,json,kind,params,reference,
   done(from,results){for(let k=0;k<results.length;k++)res[from+k]=results[k];
    while(ready<count&&res[ready]!==undefined)ready++;
    while(pi<points.length&&ready>=points[pi]){const p=points[pi];if(pi===points.length-1||(stop&&stop(res.slice(0,p)))){finish(res.slice(0,p));return;}pi++;}
    issue();},
   fail(e){jobs.delete(id);reject(e);}};
  jobs.set(id,job);issue();});}

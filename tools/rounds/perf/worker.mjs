// A game worker of pool.mjs: plays the games it is handed (games.mjs playOne) on the same engine as measure.mjs
// (engines.mjs) and sends back their results in index order. Levels arrive once per worker, as JSON, and are kept.
import {parentPort} from 'node:worker_threads';
import {playOne} from './games.mjs';
import {engineSync} from './engines.mjs';
import {engine as referenceEngine} from '../engine.mjs';
const levels=new Map();let fast,ref;
parentPort.on('message',async m=>{
 // the engine of the caller's thread at the time of the call (ROUNDS_ENGINE=reference there plays the reference here)
 try{const E=m.reference?(ref||(ref=await referenceEngine())):(fast===undefined?(fast=engineSync()):fast)||(ref||(ref=await referenceEngine()));
  if(m.reset)levels.clear();
  if(m.level!==undefined)levels.set(m.key,JSON.parse(m.level));
  const level=levels.get(m.key);if(!level)throw Error('level not sent to this worker');
  const results=[];for(let i=m.from;i<m.to;i++)results.push(playOne(E,level,m.kind,m.params,i));
  parentPort.postMessage({job:m.job,from:m.from,results});}
 catch(e){parentPort.postMessage({job:m.job,from:m.from,error:String(e&&e.stack||e)});}});

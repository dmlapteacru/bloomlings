// The engine the measures play on (tools/rounds/perf/README.md; the owner, 2026-11-02): the fast twin of the reference
// (fast-rules.cjs, fast-timeline.cjs, fast-idle.cjs: the same games byte for byte, proven by verify.mjs) while the reference
// files keep the fingerprints it was verified against (fast-engine.cjs VERIFIED); else, or with ROUNDS_ENGINE=reference,
// the reference engine of tools/rounds/engine.mjs. Shared by measure.mjs and the game workers (worker.mjs).
import {engine as referenceEngine} from '../engine.mjs';
import fastTwin from './fast-engine.cjs';
let warned=false;
export function engineSync(){
 if(process.env.ROUNDS_ENGINE!=='reference'){const f=fastTwin.fastEngine();if(f)return f;
  if(!warned){warned=true;console.error(`measure.mjs: the reference engine changed (${fastTwin.stale().join(', ')}) since its fast twin was verified; playing on the reference (port the change to tools/rounds/perf/fast-*.cjs and run tools/rounds/perf/verify.mjs)`);}}
 return null;}
export async function engine(){return engineSync()||referenceEngine();}

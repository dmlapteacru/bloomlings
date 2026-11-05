/* The fast twin of the round engine for the measures (tools/rounds/perf/README.md; the owner, 2026-11-02). It plays
 * exactly what the reference in tools/rounds/engine/ plays (verify.mjs proves it on every placed level and on bank
 * candidates), only faster. It is tied to the reference files it was verified against: when any of them changes, the
 * fingerprints below no longer match and fastEngine() answers null, so measure.mjs plays on the reference again until the
 * change is ported here and verified (node tools/rounds/perf/verify.mjs; then update VERIFIED with the new SHA-256). */
'use strict';
const fs=require('fs'),path=require('path'),crypto=require('crypto');
const REF=path.join(__dirname,'..','engine');
// SHA-256 of the reference files this twin was verified against (verify.mjs, 2026-11-05: the water channel and the
// shutters; L1–L100, the fixture's cell, latch, channel and shutter cases, 0 differences)
const VERIFIED={
 'rules.cjs':'3b0db65d328185592842db0d610b788e5751aa361c3949bd1cecec78f038f1da',
 'timeline.cjs':'e07412da4e294971c2efced807ad558783a67416b5e31d33caf209e6b7de8002',
 'idle-rules.cjs':'f72b5310ac4be2e475a2e225f2557ca1d242442a302719852d8fbeb12883eeca'};
const sha=f=>crypto.createHash('sha256').update(fs.readFileSync(path.join(REF,f))).digest('hex');
// the reference files whose fingerprints differ from VERIFIED (empty: the twin may be used)
function stale(){return Object.keys(VERIFIED).filter(f=>sha(f)!==VERIFIED[f]);}
let cached;
function fastEngine(){if(cached===undefined)cached=stale().length?null:{R:require('./fast-rules.cjs'),T:require('./fast-timeline.cjs'),idle:require('./fast-idle.cjs'),fast:true};return cached;}
module.exports={VERIFIED,stale,fastEngine};

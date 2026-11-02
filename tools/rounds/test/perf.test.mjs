// node --test tools/rounds/test/ — the speed-ups of the measures (tools/rounds/perf/README.md, the owner, 2026-11-02): the
// fast twin plays exactly what the reference engine plays (the full proof is perf/verify.mjs), the games of a test give the
// same result on any number of threads, early stopping replays the sequential rule on the same games, and measure.mjs falls
// back to the reference when the reference changed since the twin was verified.
import test from 'node:test';import assert from 'node:assert/strict';
import fs from 'node:fs';import path from 'node:path';import {fileURLToPath} from 'node:url';

const HERE=path.dirname(fileURLToPath(import.meta.url)),ROOT=path.resolve(HERE,'../../..');
const readJSON=f=>JSON.parse(fs.readFileSync(f,'utf8'));
const placed=n=>readJSON(path.join(ROOT,'content/rounds/levels',`L${String(n).padStart(4,'0')}.json`));
const {REF,FAST,replay,randomGame,calmGame}=await import('../perf/identity.mjs');
const twin=(await import('../perf/fast-engine.cjs')).default;
const M=await import('../measure.mjs');const {games}=await import('../perf/pool.mjs');
const keep=b=>b===null||b===undefined?null:a=>a.type!=='bridge'||a.choice===b;

test('the fast twin is verified against the current reference engine (else port the change and run perf/verify.mjs)',()=>{
 assert.deepEqual(twin.stale(),[],'the reference engine changed since the fast twin was verified: port the change to tools/rounds/perf/fast-*.cjs, run node tools/rounds/perf/verify.mjs, then update VERIFIED in fast-engine.cjs');
 assert.ok(twin.fastEngine());});

test('fast twin = reference: saved solutions, random and calm games, byte for byte',()=>{
 for(const n of [5,13,16]){const L=placed(n),lv=L.definition;
  for(const patient of [true,false])assert.deepEqual(replay(FAST,lv,L.solution.route,patient),replay(REF,lv,L.solution.route,patient),`L${n} solution`);
  for(let g=0;g<6;g++){const s=7919+g*104729+1;assert.deepEqual(randomGame(FAST,lv,s,keep(L.random.bridge)),randomGame(REF,lv,s,keep(L.random.bridge)),`L${n} random game ${g}`);}
  for(let g=0;g<3;g++){const s=3*7919+g*104729+5;assert.deepEqual(calmGame(FAST,lv,s,L.random.bridge??null),calmGame(REF,lv,s,L.random.bridge??null),`L${n} calm game ${g}`);}}});

test('fast twin = reference on the hidden-cell and latch cases of the conformance fixture',()=>{
 const F=readJSON(path.join(ROOT,'core/tests/Bloomlings.Core.Tests/Rounds/Fixtures/reference-rounds.json'));
 for(const c of [...F.cells,...F.latches]){const lv={id:1,...c.definition};
  assert.deepEqual(replay(FAST,lv,c.route,true),replay(REF,lv,c.route,true),c.name+' solution');
  for(let g=0;g<4;g++){const s=7919+g*104729+1;assert.deepEqual(randomGame(FAST,lv,s,null),randomGame(REF,lv,s,null),`${c.name} random game ${g}`);}}});

test('a test gives the same result in one thread and on workers, and the recorded one',async()=>{
 const L=placed(5),old=process.env.ROUNDS_THREADS;
 try{process.env.ROUNDS_THREADS='0';const one=await M.randomTest(L.definition,{full:true});
  process.env.ROUNDS_THREADS='2';const many=await M.randomTest(L.definition,{full:true});
  assert.deepEqual(many,one);assert.equal(one.runs,L.random.runs);assert.equal(one.wins,L.random.wins);assert.deepEqual(one.wilson95,L.random.wilson95);
  const a=await games('random',L.definition,{seed:1,bridge:null},{count:30,chunk:1}),b=await games('random',L.definition,{seed:1,bridge:null},{count:30,chunk:7});
  process.env.ROUNDS_THREADS='0';const c=await games('random',L.definition,{seed:1,bridge:null},{count:30});
  assert.deepEqual(a,c);assert.deepEqual(b,c);}
 finally{if(old===undefined)delete process.env.ROUNDS_THREADS;else process.env.ROUNDS_THREADS=old;}});

test('early stopping: off by default; on, it stops at the first checkpoint whose interval crosses no class border, on the same games',async()=>{
 const L=placed(5),lv=L.definition,full=await M.randomTest(lv);assert.equal(full.early,undefined);assert.equal(full.runs,L.random.runs);
 const e=await M.randomTest(lv,{early:true});assert.ok(e.early);assert.ok(e.runs<=400&&(e.runs===100||e.runs===400||(e.runs>=40&&e.runs%20===0)));
 const all=await games('random',lv,{seed:1,bridge:null},{count:e.runs});assert.equal(e.wins,all.filter(Boolean).length);
 const cross=n=>M.crossesAny([.05,.2,.5],M.wilson(all.slice(0,n).filter(Boolean).length,n));
 assert.ok(e.runs===400||!cross(e.runs));for(let n=40;n<e.runs;n+=20)assert.ok(cross(n),`checkpoint ${n} should not have stopped`);
 // a trivially won level stops at the first checkpoint
 const easy=await M.randomTest(placed(1).definition,{early:true});assert.equal(easy.runs,40);assert.equal(easy.cls,'Normal');});

test('measure.mjs plays on the reference with ROUNDS_ENGINE=reference, with the same results',async()=>{
 const lv=placed(16).definition,fast=await M.forcedShare(lv),old=process.env.ROUNDS_ENGINE;
 try{process.env.ROUNDS_ENGINE='reference';const E=await M.engine();assert.equal(E.fast,undefined);assert.equal(await M.forcedShare(lv),fast);}
 finally{if(old===undefined)delete process.env.ROUNDS_ENGINE;else process.env.ROUNDS_ENGINE=old;}});

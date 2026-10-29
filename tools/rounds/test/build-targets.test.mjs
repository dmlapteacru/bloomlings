import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
import * as LV from '../levels.mjs';
import * as L from '../lib.mjs';
import * as BS from '../bridge-scene.mjs';

const ROOT=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'../../..');
const read=f=>JSON.parse(fs.readFileSync(f,'utf8'));

test('opening holes do not shift the position dose cycle; relief and regular Normal target different zones',()=>{
 const plans=[{approved:'2026-10-29',wave:[21,25],levels:[{level:21,recipe:'hidden'},{level:22,recipe:'none'},{level:23,recipe:'bridge'},{level:24,recipe:'hidden'},{level:25,recipe:'links+hidden'}]}];
 const placed={19:{level:19,recipe:'links'},20:{level:20,recipe:'links'}};
 assert.equal(LV.dosePositionAt(24,plans,placed),1);
 assert.equal(LV.dosePositionAt(25,plans,placed),.6);
 assert.deepEqual(LV.targetAt(26,plans).shareTarget,[.75,.95]);
 assert.deepEqual(LV.targetAt(29,plans).shareTarget,[.5,.75]);
});

test('a banked wrong class, kind, range or unresolved confidence interval cannot complete a requested search',()=>{
 const make=(cls,share,kind='better',borderline=false)=>({random:{cls,share,borderline},checks:{bridgeKind:kind}});
 const target={cls:'Hard',bridgeKind:'single'};
 assert.equal(LV.matchesSearchTarget(make('Normal',.6,'single'),target),false);
 assert.equal(LV.matchesSearchTarget(make('Hard',.3,'better'),target),false);
 assert.equal(LV.matchesSearchTarget(make('Hard',.3,'single'),target),true);
 assert.equal(LV.matchesSearchTarget(make('Normal',.81),{cls:'Normal',shareTarget:[.5,.75]}),false);
 assert.equal(LV.matchesSearchTarget(make('Normal',.81),{cls:'Normal',shareTarget:[.75,.95]}),true);
 assert.equal(LV.matchesSearchTarget(make('Normal',.51,'better',true),{cls:'Normal'}),false);
});

test('legacy searched flags and incomplete records are not a completed search',()=>{
 assert.equal(LV.searchComplete({combo:'Normal|bridge',tried:64},'x',64),false);
 assert.equal(LV.searchComplete({key:'x',complete:false,tried:64},'x',64),false);
 assert.equal(LV.searchComplete({key:'x',complete:true,tried:64},'different-seed',64),false);
 assert.equal(LV.searchComplete({key:'x',complete:true,tried:64},'x',64),true);
 assert.equal(LV.searchComplete({key:'x',complete:true,tried:3,satisfied:true},'x',64),true);
});

test('placement evidence is bound to both the exact definition and the validator source',()=>{
 const c={level:{pods:[{color:'R',count:20}],tray:[[0]]},validationFingerprint:'validator-a'};
 assert.equal(LV.evidenceCurrent(c,'validator-a'),false);
 c.validationStamp={source:'validator-a',definition:LV.searchKey(c.level),passed:true};
 assert.equal(LV.evidenceCurrent(c,'validator-a'),true);assert.equal(LV.evidenceCurrent(c,'validator-b'),false);
 c.level.pods[0].count=21;assert.equal(LV.evidenceCurrent(c,'validator-a'),false);
});

test('explicit queue reversal changes both Hard and Normal bridge queues without changing pod balance',()=>{
 const p=L.picture('P0012'),sc=BS.scene(p,{side:'up',x:'K',y:'S',w:'C',v:'D',z:'.',strip:2,divider:1,ground:'L',fill:true,margin:2,bedDepth:1,bedBreadth:3,bedDepthY:3,bedBreadthY:5});
 const b=L.boardOf(sc),m=L.measure(sc),base=LV.splitPods(m.volumes,m.N);
 for(const cls of ['Normal','Hard']){
  const a=LV.candidate(p,b,base.pods,3,'bridge',cls,1000,false,.5,{t:0,intended:0}),z=LV.candidate(p,b,base.pods,3,'bridge',cls,1000,false,.5,{t:1,intended:0});
  assert.notDeepEqual(a.level.tray,z.level.tray,cls);assert.deepEqual(LV.checkPods(a.level.pods,m.volumes),[]);assert.deepEqual(LV.checkPods(z.level.pods,m.volumes),[]);
 }
});

test('engine hidden cap never lowers the required minimum; three-type lower half is rounded from percentages',()=>{
 assert.match(LV.doses('hidden+links+bridge','Super Hard',48,false).error,/нет целых доз hidden/);
 assert.deepEqual(LV.doses('links+bridge+hidden','Super Hard',29,false).links,[12,12]);
});

test('compact multi-mechanic queues may use the budget above its lower bound without changing work',()=>{
 const volumes={R:100,G:100,B:100},small=LV.splitPods(volumes,300,null),multi=LV.splitPods(volumes,300,null,{minPods:16});
 assert.equal(small.pods.length,9);assert.equal(multi.pods.length,16);assert.deepEqual(LV.checkPods(multi.pods,volumes),[]);
 assert.ok(multi.pods.every(p=>p.count>=8));assert.match(LV.splitPods(volumes,300,null,{minPods:25}).error,/вне бюджета/);
});

test('scene generation retains the winning queue parameters and reconstructs the identical queue',()=>{
 const p=L.picture('P0012'),sc=BS.scene(p,{side:'up',x:'K',y:'S',w:'C',v:'D',z:'.',strip:2,divider:1,ground:'L',fill:true,margin:2,bedDepth:1,bedBreadth:3,bedDepthY:3,bedBreadthY:5});
 const b=L.boardOf(sc),m=L.measure(sc),base=LV.splitPods(m.volumes,m.N),columns=L.columnsFor(m.N)[0];
 const generation={seed:1000,columns,dosePos:.5,opts:{gate:6,delay:2,intended:BS.choiceOf(sc,b,sc.win)},cls:'Normal',recipe:'bridge'};
 const text=BS.moduleText([{source:p,scene:sc,opts:{},generation,kind:'single',cls:'Normal',recipe:'bridge',forLevel:26}],{name:'test'});
 const module=JSON.parse(text.slice(text.indexOf('export default ')+15).trim().replace(/;$/,''));
 const rec=module.pictures[0];assert.deepEqual(rec.scene.generation,generation);
 const opts=LV.sceneOpts(rec,b,base.pods.length,columns,0);assert.deepEqual(opts,generation.opts);
 const original=LV.candidate(p,b,base.pods,columns,'bridge','Normal',1000,false,.5,generation.opts);
 const replay=LV.candidate(p,b,base.pods,columns,'bridge','Normal',generation.seed,false,generation.dosePos,opts);
 assert.deepEqual(replay.level,original.level);
});

test('build CLI cache is seed-aware, old successful search flags cannot suppress it, and draft mode stays isolated',()=>{
 const dir=fs.mkdtempSync(path.join(os.tmpdir(),'round-build-'));
 try{
  fs.mkdirSync(path.join(dir,'pictures'));fs.mkdirSync(path.join(dir,'bank'));fs.mkdirSync(path.join(dir,'levels'));
  fs.copyFileSync(path.join(ROOT,'content/rounds/pictures/P0012.json'),path.join(dir,'pictures/P0012.json'));
  fs.writeFileSync(path.join(dir,'bank/P0012.json'),JSON.stringify({picture:'P0012',candidates:[],rejected:[],searched:[{combo:'Normal|none|',tried:64,found:4}]}));
  const args=['tools/rounds/levels.mjs','build','P0012','--level','22','--recipes','none','--candidates','1','--no-intro'];
  const run=seed=>spawnSync(process.execPath,[...args,'--seed',String(seed)],{cwd:ROOT,env:{...process.env,ROUNDS_DIR:dir},encoding:'utf8',maxBuffer:1<<22});
  const first=run(1043);assert.equal(first.status,0,first.stderr);let bank=read(path.join(dir,'bank/P0012.json'));assert.equal(bank.searched.length,2);
  const repeated=run(1043);assert.equal(repeated.status,0,repeated.stderr);assert.equal(read(path.join(dir,'bank/P0012.json')).searched.length,2);
  const another=run(1044);assert.equal(another.status,0,another.stderr);bank=read(path.join(dir,'bank/P0012.json'));assert.equal(bank.searched.length,3);assert.notEqual(bank.searched[1].key,bank.searched[2].key);
  const denied=spawnSync(process.execPath,[...args,'--allow-draft'],{cwd:ROOT,env:{...process.env,ROUNDS_DIR:path.join(ROOT,'content/rounds')},encoding:'utf8'});
  assert.notEqual(denied.status,0);assert.match(denied.stderr,/isolated ROUNDS_DIR/);
 }finally{fs.rmSync(dir,{recursive:true,force:true});}
});

test('refresh records exact-definition failure without deleting a placed level or changing outside the range',()=>{
 const dir=fs.mkdtempSync(path.join(os.tmpdir(),'round-refresh-'));
 try{
  for(const d of ['pictures','bank','levels'])fs.mkdirSync(path.join(dir,d));
  const level=read(path.join(ROOT,'content/rounds/levels/L0022.json'));
  fs.copyFileSync(path.join(ROOT,'content/rounds/pictures',level.picture+'.json'),path.join(dir,'pictures',level.picture+'.json'));
  level.definition.pods[0].count++;level.generation={seed:123,opts:{}};
  const file=path.join(dir,'levels/L0022.json');fs.writeFileSync(file,JSON.stringify(level));
  const first=fs.readFileSync(path.join(ROOT,'content/rounds/levels/L0001.json'),'utf8');fs.writeFileSync(path.join(dir,'levels/L0001.json'),first);
  const r=spawnSync(process.execPath,['tools/rounds/levels.mjs','checks','--refresh','--levels','--from','21','--to','50'],{cwd:ROOT,env:{...process.env,ROUNDS_DIR:dir},encoding:'utf8'});
  assert.equal(r.status,0,r.stderr);const changed=read(file);
  assert.equal(changed.validationFailure,'баланс');assert.equal(changed.validationStamp.passed,false);
  assert.equal(changed.validationStamp.definition,LV.searchKey(level.definition));assert.deepEqual(changed.generation,level.generation);
  assert.deepEqual(changed.definition,level.definition);assert.equal(fs.readFileSync(path.join(dir,'levels/L0001.json'),'utf8'),first);
 }finally{fs.rmSync(dir,{recursive:true,force:true});}
});

import test from 'node:test';
import assert from 'node:assert/strict';
import {planIssues,borderlineDecision,windowIssues,rarePaletteFits} from '../audit-rules.mjs';
import {boardOf} from '../lib.mjs';
import {dosesOk} from '../levels.mjs';

const normal=(level,share=.55,colours=7)=>({level,cls:'Normal',recipe:'none',colours,random:{share}});
const wave=(from,to)=>Object.fromEntries(Array.from({length:to-from+1},(_,i)=>[from+i,normal(from+i)]));

test('10 colours are ordinary; three 11-colour pictures fail even across a wave boundary',()=>{
 const levels=wave(1,40);for(const L of Object.values(levels))L.colours=10;
 assert.deepEqual(windowIssues(levels,21,40),[]);
 for(const n of [12,20,21])levels[n].colours=11;
 assert.ok(windowIssues(levels,21,40).some(s=>s.includes('11 цветами 3 > 2')));
 assert.equal(rarePaletteFits({colours:11},21,{12:levels[12],20:levels[20]}),false);
 assert.equal(rarePaletteFits({colours:10},21,{12:levels[12],20:levels[20]}),true);
 assert.equal(rarePaletteFits({colours:11},32,{12:levels[12],20:levels[20]}),true);
});

test('rare palettes spread across a long wave pass when every 20-level window contains at most two',()=>{
 const levels=wave(1,60);for(const n of [1,21,41])levels[n].colours=11;
 assert.deepEqual(windowIssues(levels,1,60),[]);
});

test('rolling Normal median catches a high local window despite a passing overall median',()=>{
 const levels=wave(1,60);for(let n=21;n<=40;n++)levels[n].random.share=.75;
 assert.ok(windowIssues(levels,21,50).some(s=>s.includes('L21–L40: медиана обычных Normal 75 %')));
});

test('high-win limit is ten regular Normals, not ten numbered levels',()=>{
 const levels=wave(1,40);levels[22].random.share=.97;levels[33].random.share=.97;
 assert.ok(windowIssues(levels,21,40).some(s=>s.includes('больше одного на десять обычных Normal')));
});

test('borderline requires an explicit owner decision or the narrow single-bridge exception',()=>{
 const L={cls:'Normal',random:{runs:400,wins:210,borderline:true}};
 assert.equal(borderlineDecision(L).ok,false);
 assert.equal(borderlineDecision({...L,classDecision:{decidedBy:'agent',cls:'Normal',decidedAt:'2026-10-29',reason:'looks close',source:'chat'}}).ok,false);
 assert.equal(borderlineDecision({...L,classDecision:{decidedBy:'owner',cls:'Normal',decidedAt:'2026-10-29',reason:'accepted borderline',source:'owner-results.json'}}).ok,true);
 const bridge={...L,checks:{bridgeKind:'single',bridgeSingle:{ok:true,each:[true,false]}}};
 assert.equal(borderlineDecision(bridge).ok,true);
 assert.equal(borderlineDecision({...bridge,random:{...L.random,runs:100,wins:52}}).ok,false);
 assert.equal(borderlineDecision({...bridge,random:{...L.random,wins:199}}).ok,false);
 assert.equal(borderlineDecision({...bridge,cls:'Hard'}).ok,false);
});

test('mechanics plan is checked from the actual board, including overlay provenance',()=>{
 const rows=Array.from({length:9},()=>'G'.repeat(9));
 const overlay={walls:Array.from({length:9},(_,x)=>[x,3]),sites:[[1,3],[6,3]]};
 const definition={...boardOf({rows},overlay),overlay};
 const plan={recipe:'bridge',bridgeSide:'up',strip:'overlay',bridgeKind:'better'};
 const L={recipe:'bridge',definition,checks:{bridgeKind:'better'}};
 assert.deepEqual(planIssues(L,plan),[]);
 assert.ok(planIssues({...L,recipe:'bridge+links'},plan).some(s=>s.includes('рецепт')));
 assert.ok(planIssues({...L,bridgeSide:'left'}, {...plan,bridgeSide:'left'}).some(s=>s.includes('сторона моста up')));
 const {overlay:omit,...plain}=definition;
 assert.ok(planIssues({...L,definition:plain},plan).some(s=>s.includes('полоса new, план требует overlay')));
 assert.ok(planIssues({...L,checks:{bridgeKind:'single'}},plan).some(s=>s.includes('вид моста')));
});

test('dose validation detects an undersized Hard lead despite a valid mechanism name',()=>{
 const pods=Array.from({length:24},()=>({color:'G',count:20}));pods[0].group=pods[1].group='g';
 assert.match(dosesOk({recipe:'links',pods},'Hard'),/связанных 2 вне/);
 for(let i=2;i<8;i++)pods[i].group='g'+Math.floor(i/2);
 assert.equal(dosesOk({recipe:'links',pods},'Hard'),null);
});

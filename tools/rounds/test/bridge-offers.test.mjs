import test from 'node:test';
import assert from 'node:assert/strict';
import {engine} from '../engine.mjs';
import {bridgeSingle} from '../measure.mjs';

// The first G approach also blocks the corridor to the second P approach. Thus bridge 0 is ALWAYS the first
// available site; waiting after it opens lets a P courier clear the second approach before committing any bridge.
const level=()=>({id:1,w:7,h:8,entries:[1],capacity:5,
 rowsBottomFirst:['#.#####','#.#####','#.#####','#G...P#','#######','#GGPPP#','#.....#','#######'],
 pods:[{color:'G',count:1},{color:'P',count:1},{color:'G',count:2},{color:'P',count:3}],tray:[[0,2],[1,3]],
 hero:{id:'sprig-bridge',choices:[{cell:29,anchor:22,label:'first'},{cell:33,anchor:26,label:'later'}]}});

test('bridge observation continues through legal waiting until a bridge is actually chosen',async()=>{
 const E=await engine(),lv=level();assert.deepEqual(E.R.validate(lv),[]);
 const t=new E.T.Timeline(E.R.create(lv));t.commit({type:'tap',pod:0});t.commit({type:'tap',pod:1});
 while(!E.R.bridgeChoices(t.state).length)t.advance(t.steps[0].at-t.now);
 assert.deepEqual(E.R.bridgeChoices(t.state).map(x=>x.choice),[0],'only the first site is initially available');
 while(E.R.bridgeChoices(t.state).length<2&&t.pending)t.advance(t.steps[0].at-t.now);
 assert.deepEqual(E.R.bridgeChoices(t.state).map(x=>x.choice),[0,1],'the other site is available after legal waiting');
 const result=await bridgeSingle(lv,60,3,[true,true]);
 assert.ok(result.offered[0]>0&&result.offered[1]>0,JSON.stringify(result.offered));
 assert.ok(result.offered.every(n=>n<=60),'offered counts games, not repeated observation ticks');
 assert.equal(result.ok,false,'observing both sites does not waive the exactly-one-winner requirement');
 assert.deepEqual(await bridgeSingle(lv,60,3,[true,true]),result,'fixed seeds remain reproducible');
});

test('an unobserved alternative is not credited merely because another bridge could expose it',async()=>{
 const lv=level();lv.hero.choices[1].anchor=40; // far-bank neighbour, only accessible after a bridge is built
 const result=await bridgeSingle(lv,60,3,[true,false]);
 assert.ok(result.offered[0]>0);assert.equal(result.offered[1],0);assert.equal(result.ok,false);
});

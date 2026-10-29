import test from 'node:test';
import assert from 'node:assert/strict';
import {STRATEGIES} from '../../level-stats/strategies.mjs';
import {engine} from '../engine.mjs';

const tap=pod=>({type:'tap',pod});
function state({board,colors,counts,groups,tray,w=3,h=3,revealed}){
 return {board,remaining:counts,meta:{colours:colors,groups},level:{w,h,tray},heads:tray.map(()=>0),revealed:revealed||counts.map(()=>1)};
}
const choose=(key,s,m,E={})=>STRATEGIES[key].make(1,E)({state:s,reserved:s.board.map(()=>-1)},m);

test('most-now counts a same-color linked pair against a shared work budget',()=>{
 const s=state({board:[0,0,1,1,1],colors:[0,0,1],counts:[2,2,3],groups:[[0,1],[0,1],[2]],tray:[[0],[1],[2]]});
 const E={R:{reach:()=>({targets:[0,1,2,3,4]})}};
 assert.deepEqual(choose('most-now',s,[tap(0),tap(2)],E),tap(2),'two G cells are two work units, not four');
});

test('spatial strategies use the full group, deduplicate colors, and put no known work last',()=>{
 // Nine rows from bottom: G at top, two P cells below, and B in the middle. G+P is below B on average.
 const board=Array(27).fill(-1);board[24]=0;board[0]=1;board[1]=1;board[15]=2;
 const s=state({board,colors:[0,1,2,3],counts:[1,2,1,1],groups:[[0,1],[0,1],[2],[3]],tray:[[0],[1],[2],[3]],w:3,h:9});
 assert.deepEqual(choose('top-down',s,[tap(0),tap(2),tap(3)]),tap(2));
 assert.deepEqual(choose('bottom-up',s,[tap(0),tap(2),tap(3)]),tap(0));
 const duplicate=state({board,colors:[0,0,2],counts:[1,1,1],groups:[[0,1],[0,1],[2]],tray:[[0],[1],[2]],w:3,h:9});
 assert.deepEqual(choose('top-down',duplicate,[tap(0),tap(2)]),tap(0));
});

test('diagonal uses original row plus column and remains separate from round-robin',()=>{
 const s=state({board:[0],colors:[0,0,0,0,0,0],counts:[1,1,1,1,1,1],groups:[[0],[1],[2],[3],[4],[5]],tray:[[0,1,2,3,4],[5]]});
 s.heads=[4,0];assert.deepEqual(choose('diagonal',s,[tap(4),tap(5)]),tap(5),'row4 col0 follows row0 col1');
 const round=STRATEGIES['round-robin'].make();assert.deepEqual(round({state:s},[tap(4),tap(5)]),tap(4));
 assert.deepEqual(round({state:s},[tap(4),tap(5)]),tap(5));
});

test('ties prefer the leftmost member, independent of representative pod ID or action array order',()=>{
 const s=state({board:[0,1],colors:[0,1,0],counts:[1,1,1],groups:[[0,2],[1],[0,2]],tray:[[2],[1],[0]]});
 assert.deepEqual(choose('left-to-right',s,[tap(1),tap(0)]),tap(0));
 assert.deepEqual(choose('right-to-left',s,[tap(1),tap(0)]),tap(0));
 const E={R:{reach:()=>({targets:[0,1]})}};assert.deepEqual(choose('most-now',s,[tap(1),tap(0)],E),tap(0));
});

test('fast-release measures common group release in the live engine, not total quota',async()=>{
 const E=await engine(),level={id:1,w:9,h:8,entries:[4],capacity:5,
  rowsBottomFirst:['.........','.GGGPPPB.','.......B.','.......B.','.......B.','.........','.........','.........'],
  pods:[{color:'G',count:3,group:'a'},{color:'P',count:3,group:'a'},{color:'B',count:4}],tray:[[0],[1],[2]]};
 assert.deepEqual(E.R.validate(level),[]);const t=new E.T.Timeline(E.R.create(level));
 const moves=E.R.legal(t.state),before=t.snapshot(),chosen=STRATEGIES['fast-release'].make(1,E)(t,moves);
 assert.deepEqual(chosen,tap(0),'two parallel quotas of three release sooner than the singleton quota of four');
 assert.deepEqual(t.snapshot(),before,'evaluating a strategy cannot change the live game');
 const times=[];for(const a of moves){const c=new E.T.Timeline(t.state,before);c.commit(a);c.advance(1e9);
  times.push(Math.max(...c.lastEvents.filter(e=>e.type==='complete'&&c.state.meta.groups[a.pod].includes(e.pod)).map(e=>e.at)));}
 assert.ok(times[0]<times[1]);
});

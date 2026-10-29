import test from 'node:test';
import assert from 'node:assert/strict';
import {overlayGateOptions} from '../analysis/overlay-gates.mjs';
import {picture,overlaid,boardOf,bridgeSide} from '../lib.mjs';

test('overlay shortlist changes only wall cells on the approved source and keeps bridge geometry legal',()=>{
 const p=picture('P0022'),options=overlayGateOptions(p,'up',12);
 assert.ok(options.length>1);
 for(const o of options){
  const q=overlaid(p,o.overlay),walls=new Set(o.overlay.walls.map(c=>c.join(',')));
  for(let y=0;y<p.rows.length;y++)for(let x=0;x<p.rows[0].length;x++)if(!walls.has(x+','+y))assert.equal(q.rows[y][x],p.rows[y][x]);
  assert.equal(bridgeSide(boardOf(p,o.overlay)),'up');assert.ok(o.share>=.25&&o.share<=.45);
 }
});

test('basket has a genuine asymmetric gate without repainting its materials',()=>{
 const options=overlayGateOptions(picture('P0022'),'up',30);
 const gate=options.find(o=>o.gate==='A'&&o.overlay.walls[0][1]===15);
 assert.ok(gate);assert.deepEqual(gate.availableBeforeGate.lose,{E:164});
 assert.ok(gate.availableBeforeGate.win.O>0&&gate.availableBeforeGate.win.G>0);
 assert.ok(gate.fullyBlocked.includes('O')&&gate.fullyBlocked.includes('G'));
 assert.equal(gate.N,722);
});

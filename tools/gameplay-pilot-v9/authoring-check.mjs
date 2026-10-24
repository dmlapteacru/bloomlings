// Verify authoring memo against the untouched v8E patient bot battery on identical definitions.
import assert from 'node:assert/strict';import fs from 'node:fs';
import * as cached from './botsc.mjs';import * as baseline from '../gameplay-pilot-v8e/botsc.mjs';
const data=JSON.parse(fs.readFileSync('content/pilot-v9/levels.json'));let comparisons=0;
for(const id of [12,25]){const level=data.levels[id-1],before=JSON.stringify(level);
 for(const mode of Object.keys(cached.POLICIES)){
  const expected=baseline.rate(level,mode,30,81837),actual=cached.rate(level,mode,30,81837);
  assert.equal(actual,expected,`Memo changed ${mode} L${id}`);assert.equal(cached.rate(level,mode,30,81837),expected);comparisons++;
 }
 assert.deepEqual(cached.patternWins(level),baseline.patternWins(level));comparisons++;assert.equal(JSON.stringify(level),before);
}
console.log('Authoring memo equivalence: OK',comparisons,'policy comparisons');

import assert from 'node:assert/strict';import {mkdir,writeFile} from 'node:fs/promises';import C from './clock.cjs';
const clock=new C.ActiveClock(0);let elapsed=0;const transition=(value,now)=>elapsed+=clock.setRunning(value,now),tick=now=>elapsed+=clock.sample(now);
tick(1000);assert.equal(elapsed,0);transition(true,1000);tick(1100);assert.equal(elapsed,100);
transition(false,1200);assert.equal(elapsed,200);tick(30000);transition(true,61200);assert.equal(elapsed,200,'Hidden dwell was charged');tick(61300);assert.equal(elapsed,300);
transition(false,61400);tick(100000);transition(true,120000);tick(120250);assert.equal(elapsed,650,'Dialog pause was charged');
transition(false,120300);const restored=new C.ActiveClock(5000);assert.equal(restored.sample(60000),0,'Reload downtime was charged');assert.equal(elapsed,700);assert.throws(()=>clock.sample(1));
const out='/tmp/bloomlings-pilot-v6-verification';await mkdir(out,{recursive:true});const report={passed:true,hiddenDwellExcluded:true,dialogDwellExcluded:true,previousStateUsedOnTransition:true,reloadStartsPaused:true,clockRegressionRejected:true};await writeFile(out+'/clock.json',JSON.stringify(report,null,2)+'\n');console.log(report);

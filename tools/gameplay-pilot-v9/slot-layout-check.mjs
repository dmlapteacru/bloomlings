// Regression: gaps survive release/single admission; only accepted pairs compact left.
import assert from 'node:assert/strict';import fs from 'node:fs';import http from 'node:http';import crypto from 'node:crypto';
import R from './rules.cjs';import T from './timeline.cjs';const {Timeline}=T;import {chromium} from '../heroanim/node_modules/playwright-core/index.mjs';
const fixture={id:1,title:'Slot layout fixture',difficulty:'Normal',w:7,h:24,entries:[3],rowsBottomFirst:['###.###','#..G..#',...Array(20).fill('#PPPPP#'),'#BB...#','#######'],pods:[{color:'P',count:25},{color:'G',count:1},{color:'P',count:25},{color:'P',count:25},{color:'P',count:25},{color:'B',count:1,group:'test'},{color:'B',count:1,group:'test'}],tray:[[0,4],[1],[2],[3],[5],[6]]};
assert.deepEqual(R.validate(fixture),[]);
const t=new Timeline(R.create(fixture));for(const pod of [0,1,2,3])assert(t.commit({type:'tap',pod}).accepted);
t.advance(300);assert.deepEqual([...t.state.slots],[0,-1,2,3,-1],'Release leaves a hole');
const restore=()=>new Timeline(R.create(fixture),t.snapshot());
const single=restore();assert(single.commit({type:'tap',pod:4}).accepted);assert.deepEqual([...single.state.slots],[0,4,2,3,-1],'Single uses first hole without moving others');
const before=single.snapshot();assert(!single.commit({type:'tap',pod:5}).accepted);assert.deepEqual(single.snapshot(),before,'Rejected pair never compacts');
const pair=restore();assert(pair.commit({type:'tap',pod:5}).accepted);assert.deepEqual([...pair.state.slots],[0,2,3,5,6],'Pair compacts in existing left-to-right order');
assert.deepEqual([...new Timeline(R.create(fixture),pair.snapshot()).state.slots],[0,2,3,5,6],'Pair layout survives restore');
const secondHole={slots:Int16Array.of(2,-1,0,3,-1)};
// The shared helper preserves physical order, not admission age.
R.compactSlots(secondHole);assert.deepEqual([...secondHole.slots],[2,0,3,-1,-1]);
const html=fs.readFileSync('product/previews/gameplay-pilot-v9/index.html','utf8');
const testHtml=html.replace(/(<script id="pilotData" type="application\/json">)(.*?)(<\/script>)/s,(_,a,b,c)=>{const data=JSON.parse(b);data.levels[0]=fixture;data.hash='slot-layout-fixture';return a+JSON.stringify(data)+c;});assert.notEqual(testHtml,html);
const server=http.createServer((req,res)=>{res.setHeader('Content-Type','text/html; charset=utf-8');res.end(testHtml);});await new Promise(r=>server.listen(0,'127.0.0.1',r));
const browser=await chromium.launch({executablePath:'/usr/bin/chromium',args:['--no-sandbox','--disable-dev-shm-usage']});
try{const page=await browser.newPage({viewport:{width:390,height:844},reducedMotion:'reduce'});const epoch=new Date('2026-10-25T12:00:00Z');await page.clock.install({time:epoch});await page.clock.pauseAt(new Date(+epoch+1000));await page.goto(`http://127.0.0.1:${server.address().port}`);const click=s=>page.locator(s).evaluate(n=>n.click()),slots=()=>page.locator('#slots .slot').evaluateAll(ns=>ns.map(n=>n.dataset.slotPod===undefined?-1:+n.dataset.slotPod)),snap=()=>page.evaluate(()=>window.bloomlingsPilot.snapshot());await click('#startButton');await click('#lessonButton');
 const trayEdge=await page.locator('#tray').evaluate(c=>{const b=c.getBoundingClientRect(),l=c.querySelector('svg.tubes line');return {y:b.top+ +l.getAttribute('y1'),top:c.querySelector('[data-pod=\"5\"]').getBoundingClientRect().top};});assert(Math.abs(trayEdge.y-trayEdge.top)<1,'Tray link meets top edge');
 for(const pod of [0,1,2,3])await click(`[data-pod="${pod}"]`);await page.clock.runFor(350);assert.deepEqual(await slots(),[0,-1,2,3,-1]);
 await click('#levelsButton');await page.reload();assert.deepEqual(await slots(),[0,-1,2,3,-1],'Gap survives browser reload');await click('[data-close="levelsDialog"]');
 await click('[data-pod="4"]');assert.deepEqual(await slots(),[0,4,2,3,-1]);const commands=(await snap()).commands;await click('[data-pod="5"]');assert.equal((await snap()).commands,commands);assert.deepEqual(await slots(),[0,4,2,3,-1]);
 await click('#retryButton');await click('#confirmRetryButton');if(await page.locator('#lessonDialog').evaluate(n=>n.open))await click('#lessonButton');for(const pod of [0,1,2,3])await click(`[data-pod="${pod}"]`);await page.clock.runFor(350);await click('[data-pod="5"]');assert.deepEqual(await slots(),[0,2,3,5,6]);
 for(const selector of ['#slots','#tray']){const checks=await page.locator(selector).evaluate(container=>{const box=container.getBoundingClientRect();return [...container.querySelectorAll('svg.tubes line')].filter(l=>l.getAttribute('stroke')==='#5d452c').map(l=>({lineY:box.top+ +l.getAttribute('y1'),cardTop:[...container.querySelectorAll('.slot.filled,.pod-top,.pod-behind')].find(n=>Math.abs(n.getBoundingClientRect().left+n.getBoundingClientRect().width/2-(box.left+ +l.getAttribute('x1')))<1)?.getBoundingClientRect().top}));});if(selector==='#slots')assert(checks.length);for(const check of checks)assert(Math.abs(check.lineY-check.cardTop)<1,'Link meets top edge');}
 await page.screenshot({path:'/tmp/bloomlings-v9-slot-layout.png'});
 const out='product/previews/gameplay-pilot-v9/validation/slot-layout.json';fs.writeFileSync(out,JSON.stringify({passed:true,htmlSHA256:crypto.createHash('sha256').update(html).digest('hex'),fixtureOnly:true,releasePreservesGaps:true,singleUsesFirstHole:true,pairCompactsOnlyOnAcceptedAdmission:true,existingLeftToRightOrderPreserved:true,rejectedPairDoesNotReorder:true,browserReloadPreservesGaps:true,linksAtTopEdge:true},null,2)+'\n');console.log('Fixed-slot positions / pair-only compaction / top-edge links: OK');
}finally{await browser.close();await new Promise(r=>server.close(r));}

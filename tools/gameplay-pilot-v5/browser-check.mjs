import {chromium} from '../heroanim/node_modules/playwright-core/index.mjs';
import {readFile,writeFile,mkdir} from 'node:fs/promises';
import http from 'node:http';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {createRequire} from 'node:module';
import {resolve,dirname} from 'node:path';
import {fileURLToPath} from 'node:url';

const ROOT=resolve(dirname(fileURLToPath(import.meta.url)),'../..');
const OUT=process.argv.find(a=>a.startsWith('--out='))?.slice(6)||'/tmp/bloomlings-pilot-v5-browser';
const require=createRequire(import.meta.url),R=require('./rules.cjs');
await mkdir(OUT,{recursive:true});
const html=await readFile(resolve(ROOT,'product/previews/gameplay-pilot-v5/index.html'));
const levels=JSON.parse(await readFile(resolve(ROOT,'content/pilot-v5/levels.json'),'utf8')).levels;
const certificates=JSON.parse(await readFile(resolve(ROOT,'content/pilot-v5/certificates.json'),'utf8'));
// Extend a proven fatal choice to a complete losing replay (controls need no
// separately sampled losing route in their authoring certificates).
const losingLevel=levels.find(l=>l.id===15),fatal=certificates.find(c=>c.id===15).decisionProofs[0];
const losingActions=[...fatal.prefix,fatal.wrong];let losingState=R.create(losingLevel);
for(const a of losingActions)losingState=R.step(losingState,a).state;
while(losingState.status==='Playing'){const a=R.legal(losingState)[0];losingActions.push(a);losingState=R.step(losingState,a).state;}
assert(['Jammed','Stuck'].includes(losingState.status));
const server=http.createServer((req,res)=>{res.setHeader('Content-Type','text/html; charset=utf-8');res.end(html);});
await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
const url=`http://127.0.0.1:${server.address().port}/`;
const browser=await chromium.launch({executablePath:'/usr/bin/chromium',headless:true,args:['--no-sandbox','--disable-dev-shm-usage']});
const errors=[],external=[];let screenshots=0,playthroughs=0,layoutChecks=0,reservationRestored=false,topRevealedDuringWork=false,delayedFinishes=0;
const context=await browser.newContext({viewport:{width:390,height:844},deviceScaleFactor:2,reducedMotion:'reduce',acceptDownloads:true});
const page=await context.newPage();
function inspect(p){p.on('pageerror',e=>errors.push(e.message));p.on('request',r=>{if(!r.url().startsWith(url)&&!r.url().startsWith('data:')&&!r.url().startsWith('blob:'))external.push(r.url());});}
inspect(page);
const snap=()=>page.evaluate(()=>{const s=window.bloomlingsPilot.snapshot();delete s.timerRunning;delete s.elapsedMs;return s;});
const data=()=>page.evaluate(()=>window.bloomlingsPilot.exportObject());
// Tests use real DOM handlers and a browser clock; no writable game test API.
const click=selector=>page.locator(selector).evaluate(n=>n.click());
const open=id=>page.locator('#'+id).evaluate(n=>n.open);
async function dismiss(){if(await open('lessonDialog'))await click('#lessonButton');}
async function drain(){await page.clock.fastForward(1000000);await page.clock.runFor(32);}
async function select(id){
  if(await open('endDialog'))await click('#endMapButton');else await click('#levelsButton');
  await click(`[data-level="${id}"]`);await dismiss();
}
async function action(a){
  const selector=a.type==='tap'?`.pod-top[data-pod="${a.pod}"]`:`[data-bridge="${a.choice}"]`;
  if(await page.locator(selector).evaluate(n=>n.disabled||n.getAttribute('aria-disabled')==='true'))await drain();
  assert.equal(await page.locator(selector).evaluate(n=>n.disabled||n.getAttribute('aria-disabled')==='true'),false,`Unavailable L${(await snap()).level}: ${JSON.stringify(a)}`);
  const before=await snap();await click(selector);const after=await snap();assert.equal(after.commands,before.commands+1);return after;
}
async function screenshot(name){await page.clock.runFor(32);await page.screenshot({path:resolve(OUT,name),animations:'disabled'});screenshots++;}
async function layout(){
  const issues=await page.evaluate(()=>{
    const issues=[],visible=e=>e.getBoundingClientRect().width>0&&e.getBoundingClientRect().height>0;
    if(document.documentElement.scrollWidth>innerWidth+1)issues.push('horizontal overflow');
    if(document.getElementById('game').getBoundingClientRect().bottom>innerHeight+1)issues.push('game exceeds viewport');
    const board=document.getElementById('board').getBoundingClientRect();if(board.height<90||board.width<150)issues.push('board collapsed');
    for(const b of document.querySelectorAll('#game button'))if(visible(b)){const r=b.getBoundingClientRect();if(r.width<43.5||r.height<43.5)issues.push(`${b.id||b.dataset.pod||b.dataset.bridge} target ${r.width}×${r.height}`);}
    for(const column of document.querySelectorAll('.column')){const box=column.getBoundingClientRect();for(const pod of column.children)if(pod.getBoundingClientRect().bottom>box.bottom+1)issues.push('queue clipped');}
    for(const img of document.querySelectorAll('img'))if(visible(img)&&img.src&&(!img.complete||img.naturalWidth===0))issues.push('image not decoded');
    return issues;
  });
  assert.deepEqual(issues,[]);layoutChecks++;
}
try{
  const epoch=new Date('2026-10-20T12:00:00Z');await page.clock.install({time:epoch});await page.clock.pauseAt(new Date(+epoch+1000));
  await page.goto(url);await click('#startButton');await dismiss();await page.evaluate(()=>document.fonts.ready);await page.clock.runFor(32);
  await layout();
  // Fill the physical row without advancing even one animation frame.
  const first=certificates.find(c=>c.id===1).actions;
  for(const a of first.slice(0,5))await action(a);
  let s=await snap();assert.equal(s.commands,5);assert.equal(s.physicalFree,0);assert(s.pendingCells>0);assert.equal(s.displayStatus,'Working');assert.equal(s.status,'Playing');assert.equal(await open('endDialog'),false);
  await click(`[data-pod="${first[5].pod}"]`);assert.equal((await snap()).commands,5);
  assert((await data()).attempts.at(-1).inputEvents.some(e=>!e.accepted&&e.reason==='visual-places-busy'));
  // Modal pauses visual work; reload restores the exact in-flight reservation.
  await page.clock.runFor(120);s=await snap();assert(s.couriers.length>0);await click('#levelsButton');s=await snap();await page.clock.fastForward(30000);assert.deepEqual(await snap(),s);await click('[data-close="levelsDialog"]');
  await page.reload();await dismiss();assert.deepEqual(await snap(),s);
  await screenshot('rapid-five-places.png');await drain();await action(first[5]);
  await click('#retryButton');await click('#confirmRetryButton');assert.equal((await snap()).commands,0);

  // All thirty use five places; drive the final certificate through real DOM.
  for(let id=1;id<=30;id++){
    if(id!==1)await select(id);
    const level=levels.find(l=>l.id===id),places=5,cert=certificates.find(c=>c.id===id);
    assert.equal((await snap()).capacity,places);assert.equal(await page.locator('#slots .slot').count(),places);assert.equal(await page.locator('.entry-marker').count(),level.entries.length);
    assert.equal(await page.locator('#speedButton').getAttribute('data-auto'),'false');
    await page.clock.runFor(32);await layout();
    if([1,4,6,8,10,15,17,21,25,26].includes(id))await screenshot(`level${id}.png`);
    if(id===6){
      assert.equal(await page.locator('.pod-top.mystery').count(),0);
      const hidden=await page.locator('.pod-behind.mystery').evaluateAll(ns=>ns.map(n=>({label:n.getAttribute('aria-label'),image:n.querySelector('img')?.getAttribute('src')})));
      assert.equal(hidden.length,2);assert(hidden.every(n=>n.label.startsWith('Скрытый цвет')&&!n.image));
    }
    const actions=cert.actions;let expected=R.create(level,places);
    for(const a of actions){
      const before=await snap();s=await action(a);expected=R.step(expected,a).state;assert.equal(s.signature,R.signature(expected));
      if(s.pendingCells&&s.visual.revealed.some((v,i)=>v&&!before.visual.revealed[i]))topRevealedDuringWork=true;
      if(s.pendingCells&&s.status==='Won'){assert.equal(s.displayStatus,'Working');assert.equal(await open('endDialog'),false);delayedFinishes++;}
      if(!reservationRestored&&await page.locator('.pair-ready').count()){
        assert((await page.locator('.pair-ready').first().getAttribute('aria-label')).includes('готово'));
        // Save while a completed half still holds a physical place for its partner.
        await click('#levelsButton');const saved=await snap();await click('[data-close="levelsDialog"]');await page.reload();assert.deepEqual(await snap(),saved);
        assert(await page.locator('.pair-ready').count());reservationRestored=true;await screenshot('pair-waits.png');
      }
    }
    await drain();s=await snap();assert.equal(s.status,'Won');assert.equal(s.displayStatus,'Won');assert.equal(s.pendingCells,0);assert(await open('endDialog'));assert.equal(await page.locator('#speedButton').getAttribute('data-auto'),'true');playthroughs++;
    await click('[data-rating="normal"]');await click('[data-interest="fun"]');await click('[data-art="yes"]');await click('[data-planning="planned"]');await click('[data-pace="okay"]');
    await page.locator('#levelNote').fill(`QA L${id}/${places}`);
    if(id===30)await screenshot('finished30.png');
    if(id%5===0)console.log(`Browser v5 L1–${id} passed`);
  }
  assert(topRevealedDuringWork);assert(delayedFinishes>=30);
  await select(15);await click('#endRetryButton');
  for(const a of losingActions)await action(a);
  await drain();assert(['Jammed','Stuck'].includes((await snap()).status));assert(await open('endDialog'));await click('#endRetryButton');assert.equal((await snap()).commands,0);
  for(const a of certificates.find(c=>c.id===15).actions.slice(0,3))await action(a);
  await click('#levelsButton');const partial=await snap();await click('[data-level="17"]');await select(15);assert.deepEqual(await snap(),partial);
  await click('#retryButton');await click('[data-close="retryDialog"]');assert.deepEqual(await snap(),partial);await click('#retryButton');await click('#confirmRetryButton');assert.equal((await snap()).commands,0);
  // Manual speed changes timing only and remains selected across level changes.
  await click('#speedButton');assert.equal(await page.locator('#speedButton').textContent(),'2×');await select(17);if(await open('endDialog'))await click('#endRetryButton');assert.equal(await page.locator('#speedButton').textContent(),'2×');await click('#speedButton');
  for(const [width,height] of [[320,640],[360,640],[390,844],[430,932],[768,1024],[1280,900]]){
    await page.setViewportSize({width,height});
    for(const id of [1,10,15,19,30]){await select(id);if(await open('endDialog'))await click('#endRetryButton');await page.clock.runFor(32);await layout();}
  }
  await page.setViewportSize({width:390,height:844});await select(21);await screenshot('level21-bridge.png');
  await click('#levelsButton');await click('[data-palette="accessible"]');await click('[data-close="levelsDialog"]');assert.equal((await snap()).palette,'accessible');assert((await snap()).symbols);await screenshot('accessible-palette.png');
  await click('#levelsButton');await click('#resultsButton');
  const download=page.waitForEvent('download');await click('#exportButton');await (await download).saveAs(resolve(OUT,'results.json'));
  const report=JSON.parse(await readFile(resolve(OUT,'results.json'),'utf8'));
  assert.equal(report.format,'bloomlings-pilot-results-5');assert(report.timelineHash);assert(report.paletteHash);assert.equal(report.presentation.trips.dispatch,'reachable-target-stream');assert(report.clockHash);assert(report.presentation.releaseAfterReturn);
  assert.equal(report.attempts.filter(a=>a.outcome==='won').length,30);
  assert(report.attempts.every(a=>a.capacity===5));
  assert(report.attempts.filter(a=>a.outcome==='won').every(a=>Math.abs(Object.values(a.timeBuckets).reduce((x,y)=>x+y,0)-a.elapsedMs)<.01&&a.tripPlans.length>0));
  assert(report.attempts.filter(a=>a.outcome==='won').every(a=>a.config.initialSettings&&a.workEvents.length>0));
  assert(report.attempts.some(a=>a.note==='QA L15/5'&&a.rating==='normal'&&a.interest==='fun'&&a.planning==='planned'));
  assert(report.attempts.some(a=>['jammed','stuck'].includes(a.outcome)));assert(report.attempts.some(a=>a.outcome==='abandoned'));
  assert(report.attempts.some(a=>a.inputEvents.some(e=>e.reason==='visual-places-busy'&&!e.accepted)));
  assert(report.attempts.some(a=>a.telemetry.some(e=>e.type==='places-released')));
  assert(report.attempts.filter(a=>a.outcome==='won').every(a=>a.autoFastAtAction>=7&&a.workFinishedAtMs>=a.autoFastAtMs));
  // Normal real clock and touch input: a second command can be sent during work.
  const real=await browser.newContext({viewport:{width:390,height:844},hasTouch:true,isMobile:true});const touch=await real.newPage();inspect(touch);
  await touch.goto(url);await touch.locator('#startButton').tap();await touch.locator('#lessonButton').tap();
  await touch.locator(`[data-pod="${first[0].pod}"]`).tap();const one=await touch.evaluate(()=>window.bloomlingsPilot.snapshot());assert(one.pendingCells>0);
  await touch.locator(`[data-pod="${first[1].pod}"]`).tap();const two=await touch.evaluate(()=>window.bloomlingsPilot.snapshot());assert.equal(two.commands,2);assert(two.pendingCells>0);
  await touch.screenshot({path:resolve(OUT,'touch-working.png')});screenshots++;await real.close();
  assert.deepEqual(errors,[]);assert.deepEqual(external,[]);
  const summary={passed:true,reachableTargetStream:true,initialAttemptSettingsExported:true,actualWorkEventsExported:true,htmlSha256:createHash('sha256').update(html).digest('hex'),playthroughs,layoutChecks,rapidAdmission:true,fullWorkingRowWaits:true,pendingWorkRestored:true,modalPausesWork:true,pairReservationRestored:true,topRevealsBeforeTap:true,topRevealedDuringWork,delayedFinishes,capacityFive:true,accessiblePalette:true,levelSwitch:true,freeRetry:true,realResultsDownload:true,autoFastOnExhaustionOnly:true,manualSpeedPersists:true,realTouchDuringWork:true,noExternalRequests:true,noBrowserErrors:true,screenshots};
  await writeFile(resolve(OUT,'browser.json'),JSON.stringify(summary,null,2)+'\n');console.log(JSON.stringify(summary));
}finally{await browser.close();await new Promise(resolve=>server.close(resolve));}

import {chromium} from '../heroanim/node_modules/playwright-core/index.mjs';
import fs from 'node:fs';import http from 'node:http';import assert from 'node:assert/strict';import crypto from 'node:crypto';
import R from './rules.cjs';import T from './timeline.cjs';
const out='/tmp/bloomlings-pilot-v8-browser';fs.mkdirSync(out,{recursive:true});
const html=fs.readFileSync('product/previews/gameplay-pilot-v8/index.html'),levels=JSON.parse(fs.readFileSync('content/pilot-v8/levels.json')).levels,certs=JSON.parse(fs.readFileSync('content/pilot-v8/certificates.json'));
const server=http.createServer((req,res)=>{res.setHeader('Content-Type','text/html; charset=utf-8');res.end(html)});await new Promise(r=>server.listen(0,'127.0.0.1',r));const url=`http://127.0.0.1:${server.address().port}/`;
const browser=await chromium.launch({executablePath:'/usr/bin/chromium',args:['--no-sandbox','--disable-dev-shm-usage']});let errors=[],external=[],layouts=0,wins=0,checkpoints=0,earlyRelease=false;let exported;let sealedSelections=0,sealedReloads=0;
try{
 const page=await browser.newPage({viewport:{width:390,height:844},deviceScaleFactor:1,reducedMotion:'reduce',acceptDownloads:true});page.on('pageerror',e=>errors.push(e.message));page.on('request',r=>{if(!r.url().startsWith(url)&&!r.url().startsWith('data:')&&!r.url().startsWith('blob:'))external.push(r.url());});
 const epoch=new Date('2026-10-20T12:00:00Z');await page.clock.install({time:epoch});await page.clock.pauseAt(new Date(+epoch+1000));await page.goto(url);
 const click=s=>page.locator(s).evaluate(n=>n.click()),snap=()=>page.evaluate(()=>window.bloomlingsPilot.snapshot()),report=()=>page.evaluate(()=>window.bloomlingsPilot.exportObject()),open=id=>page.locator('#'+id).evaluate(n=>n.open);
 const dismiss=async()=>{if(await open('lessonDialog'))await click('#lessonButton');};
 const drain=async()=>{await page.clock.fastForward(1000000);await page.clock.runFor(32);};
 async function layout(){const issues=await page.evaluate(()=>{const result=[];if(document.documentElement.scrollWidth>innerWidth+1)result.push('horizontal overflow');if(document.getElementById('game').getBoundingClientRect().bottom>innerHeight+1)result.push('vertical overflow');for(const b of document.querySelectorAll('#game button')){const r=b.getBoundingClientRect();if(r.width&&r.height&&(r.width<43.5||r.height<43.5))result.push('small touch target '+b.id);}for(const c of document.querySelectorAll('.column'))for(const pod of c.children)if(pod.getBoundingClientRect().bottom>c.getBoundingClientRect().bottom+1)result.push('clipped queue');for(const img of document.querySelectorAll('img'))if(img.getBoundingClientRect().width&&img.src&&(!img.complete||!img.naturalWidth))result.push('missing asset');return result;});if(issues.length){console.log('LAYOUT',await snap(),await page.locator('.column').evaluateAll(cs=>cs.map(c=>({box:c.getBoundingClientRect().toJSON(),peek:c.style.getPropertyValue('--peek-height'),children:[...c.children].map(n=>({tag:n.className,box:n.getBoundingClientRect().toJSON()}))}))));await page.screenshot({path:out+'/layout-failure.png'});}assert.deepEqual(issues,[]);layouts++;}
 await click('#startButton');await dismiss();await page.evaluate(()=>document.fonts.ready);await page.clock.runFor(32);
 // Fill five slots rapidly; a sixth command is refused without mutation.
 let chosen=[];for(let i=0;i<5;i++){let s=await snap();const live=new T.Timeline(R.create(levels[0]));const save=(await report()).presentations;const active=(await report()).attempts.at(-1);const t=new T.Timeline(R.create(levels[0]),save[active.id]);const a=R.legal(t.state)[0];assert(a);chosen.push(a);await click(`[data-pod="${a.pod}"]`);}
 let s=await snap();assert.equal(s.physicalFree,0);assert.equal(s.displayStatus,'Working');assert.equal(s.commands,5);const top=await page.locator('.pod-top').first().getAttribute('data-pod');await click(`[data-pod="${top}"]`);assert.equal((await snap()).commands,5);assert((await report()).attempts.at(-1).inputEvents.some(e=>!e.accepted));
 await page.clock.runFor(500);await click('#levelsButton');const saved=await snap();await page.clock.fastForward(60000);assert.equal((await snap()).elapsedMs,saved.elapsedMs);await page.reload();s=await snap();for(const k of ['signature','modelTimeMs','reservations','inFlight','commands'])assert.deepEqual(s[k],saved[k]);checkpoints++;
 // Reset through actual retry UI, no writable game API.
 await click('#retryButton');await click('#confirmRetryButton');await dismiss();
 for(let id=1;id<=30;id++){
  if(id!==1){await click('#endMapButton');await click(`[data-level="${id}"]`);await dismiss();}
  await page.clock.runFor(32);await layout();
  if([1,4,5,6,7,9,15,17,25,26].includes(id))await page.screenshot({path:out+`/level-${id}.png`});
  const l=levels[id-1],t=new T.Timeline(R.create(l));let count=0;
  for(const a of certs[id-1].actions){
   const before=await snap(),hidden=a.type==='tap'?t.state.meta.groups[a.pod].filter(p=>!before.visual.revealed[p]):[];
   for(const p of hidden){const node=page.locator(`.pod-top[data-pod="${p}"]`);assert(await node.evaluate(n=>n.classList.contains('mystery')&&n.querySelector('img').hidden&&n.querySelector('.channel-mark').textContent==='?'));assert((await node.getAttribute('aria-label')).includes('Скрытый'));}
   if(hidden.length&&!sealedReloads){await click('#levelsButton');await page.reload();const afterReload=await snap();assert.deepEqual(afterReload.visual.revealed,before.visual.revealed);assert.deepEqual(afterReload.signature,before.signature);sealedReloads++;}
   const selector=a.type==='tap'?`.pod-top[data-pod="${a.pod}"]`:`[data-bridge="${a.choice}"]`;assert.equal(await page.locator(selector).evaluate(n=>n.disabled||n.getAttribute('aria-disabled')==='true'),false,`L${id} command ${count}`);
   await click(selector);assert(t.commit(a).accepted);count++;
   if(hidden.length){const after=await snap(),active=(await report()).attempts.at(-1);for(const p of hidden){assert.equal(after.visual.revealed[p],1);assert(active.reveals.some(r=>r.pod===p&&r.mode==='sealed'&&r.command===count));sealedSelections++;}}assert.equal((await snap()).commands,count);
   if(id===1&&count===1){await page.clock.runFor(500);await click('#levelsButton');const p=await snap();await page.reload();const q=await snap();for(const key of ['signature','reservations','inFlight','commands'])assert.deepEqual(q[key],p[key]);checkpoints++;}
   await drain();t.advance(1e9);s=await snap();assert.equal(s.signature,R.signature(t.state),`physical replay L${id}/${count}`);
   if(t.state.status==='Playing')assert.equal(await open('endDialog'),false);
  }
  assert.equal((await snap()).status,'Won');assert.equal((await snap()).pendingTrips,0);assert(await open('endDialog'));wins++;console.log('DOM L'+id+' passed');
  await click('[data-rating="normal"]');await click('[data-interest="okay"]');await click('[data-art="yes"]');await click('[data-planning="planned"]');await click('[data-pace="okay"]');if(l.pods.some(p=>p.mystery==='sealed'))await click('[data-uncertainty="planned"]');
 }
 // Responsive frame and deepest new queues. Switch away from the result dialog.
 await click('#endMapButton');await click('[data-level="18"]');await click('#endRetryButton');await dismiss();
 for(const [width,height]of [[320,640],[360,780],[390,844],[430,932],[768,1024],[1280,800]]){await page.setViewportSize({width,height});await page.clock.runFor(32);await layout();}
 await page.setViewportSize({width:390,height:844});await click('#levelsButton');await click('[data-palette="accessible"]');await click('[data-close="levelsDialog"]');assert.equal((await snap()).palette,'accessible');
 await click('#speedButton');assert.equal(await page.locator('#speedButton').textContent(),'2×');await click('#speedButton');assert.equal(await page.locator('#speedButton').textContent(),'1×');
 await click('#levelsButton');await click('#resultsButton');const downloadPromise=page.waitForEvent('download');await click('#exportButton');const download=await downloadPromise;await download.saveAs(out+'/results.json');exported=JSON.parse(fs.readFileSync(out+'/results.json'));assert.equal(exported.format,'bloomlings-pilot-results-8');assert.equal(exported.presentation.releaseAfterReturn,false);assert.equal(exported.attempts.filter(a=>a.outcome==='won').length,30);
 for(const a of exported.attempts.filter(a=>a.outcome==='won')){
  assert.equal(a.rating,'normal');assert.equal(a.interest,'okay');assert.equal(a.planning,'planned');assert(Math.abs(Object.values(a.timeBuckets).reduce((x,y)=>x+y,0)-a.elapsedMs)<.01);assert(!a.timeBuckets.stalled);
  const l=levels[a.level-1],events=a.workEvents,plans=new Map(a.tripPlans.map(p=>[p[0],p]));assert.equal(plans.size,l.pods.reduce((n,p)=>n+p.count,0));assert.equal(a.assignments.length,plans.size);
  for(const [id,p]of plans){assert.equal(p[3],p[4]);assert(p[9]>0);for(const code of [0,1,2,3,5])assert.equal(events.filter(e=>e[1]===code&&e[2]===id).length,1);}
  for(const e of events.filter(e=>e[1]===4)){const lastDeparture=Math.max(...a.tripPlans.filter(p=>l.pods[p[1]].group?l.pods[p[1]].group===l.pods[e[2]].group:p[1]===e[2]).map(p=>p[4]));assert.equal(e[0],lastDeparture+T.PROFILE.releaseLag);if(a.tripPlans.some(p=>p[1]===e[2]&&p[7]>e[0]))earlyRelease=true;}
  // Replay exact command model timestamps, including concurrent choices.
  const t=new T.Timeline(R.create(l));for(const c of a.commands){if(c.modelTimeMs>t.now)t.advance(c.modelTimeMs-t.now);assert.equal(t.now,c.modelTimeMs);assert(t.commit(c).accepted);}t.advance(1e9);assert.equal(t.status,'Won');
 }
 assert(earlyRelease);assert(sealedSelections>=20);assert.equal(sealedReloads,1);assert.deepEqual(errors,[]);assert.deepEqual(external,[]);
 const result={passed:true,htmlSHA256:crypto.createHash('sha256').update(html).digest('hex'),domWinningPlaythroughs:wins,responsiveLayouts:layouts,checkpointReloads:checkpoints,noExternalRequests:true,noBrowserErrors:true,exportReplayed:true,dispatchCountsAndReleaseTimesVerified:true,releasesBeforeReturnObserved:earlyRelease,sealedSelectionsVerified:sealedSelections,sealedCheckpointReloads:sealedReloads,limits:['Browser clock accelerated for QA; these are not human playtest timings.']};fs.writeFileSync(out+'/browser.json',JSON.stringify(result,null,2)+'\n');console.log(result);
}finally{await browser.close();await new Promise(r=>server.close(r));}

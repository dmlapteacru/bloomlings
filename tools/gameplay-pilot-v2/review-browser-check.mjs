import {chromium} from '../heroanim/node_modules/playwright-core/index.mjs';
import {readFile,writeFile,mkdir} from 'node:fs/promises';
import {createHash} from 'node:crypto';
import http from 'node:http';
import assert from 'node:assert/strict';
import {resolve,dirname} from 'node:path';
import {fileURLToPath} from 'node:url';
import R from './rules.cjs';

const ROOT=resolve(dirname(fileURLToPath(import.meta.url)),'../..'),folder=resolve(ROOT,'product/playtests/pilot-v2-owner-2026-10-19');
const html=await readFile(resolve(folder,'index.html')),report=JSON.parse(await readFile(resolve(folder,'review.json'))),data=JSON.parse(await readFile(resolve(ROOT,'content/pilot-v2/levels.json')));
const OUT=process.argv.find(a=>a.startsWith('--out='))?.slice(6)||'/tmp/bloomlings-pilot-v2-review-browser';await mkdir(OUT,{recursive:true});
const server=http.createServer((req,res)=>{res.setHeader('Content-Type','text/html; charset=utf-8');res.end(html);});await new Promise(r=>server.listen(0,'127.0.0.1',r));
const url=`http://127.0.0.1:${server.address().port}/`,browser=await chromium.launch({executablePath:'/usr/bin/chromium',headless:true,args:['--no-sandbox','--disable-dev-shm-usage']});
const context=await browser.newContext({viewport:{width:390,height:844},deviceScaleFactor:2,acceptDownloads:true}),page=await context.newPage();const errors=[],external=[];
page.on('pageerror',e=>errors.push(e.message));page.on('request',r=>{if(!r.url().startsWith(url)&&!r.url().startsWith('data:')&&!r.url().startsWith('blob:'))external.push(r.url());});
const snap=()=>page.evaluate(()=>window.bloomlingsPilotReview.snapshot());
async function turn(n){await page.locator('#turn').evaluate((e,n)=>{e.value=n;e.dispatchEvent(new Event('input',{bubbles:true}));},n);}
let checked=0,layouts=0,observedReady=false;
try{
 await page.goto(url);assert.equal(await page.locator('[data-level]').count(),30);assert.equal(await page.locator('#recognized').textContent(),'27 / 30');
 for(const a of report.attempts){
  await page.locator(`[data-level="${a.level}"]`).click();await page.locator('#attempt').selectOption(String(a.id));await turn(a.moves);
  let s=R.create(data.levels.find(l=>l.id===a.level));for(const t of a.trace)s=R.step(s,t.action).state;
  assert.deepEqual(await snap(),{level:a.level,attempt:a.id,turn:a.moves,signature:R.signature(s)});checked++;
  const expectedReady=Array.from(s.slots).filter(p=>p>=0&&s.remaining[p]===0).length;
  assert.equal(await page.locator('#slots .slot.ready').count(),expectedReady);observedReady||=expectedReady>0;
  const fatal=report.failures.find(f=>f.attempt===a.id);
  if(fatal){await page.locator('#fatalButton').click();assert.equal((await snap()).turn,fatal.firstProvenFatalTurn-1);}
 }
 assert(observedReady,'No completed-half reservation was exercised');
 await page.locator('[data-level="6"]').click();await turn(0);
 const unknown=await page.locator('#queue .capsule').evaluateAll(nodes=>nodes.filter(n=>n.getAttribute('aria-label').startsWith('Скрытый цвет')).map(n=>n.textContent));assert.equal(unknown.length,2);assert(unknown.every(t=>t.startsWith('?')));
 await page.locator('#inspect22').click();assert.equal((await snap()).attempt,30);assert.equal((await snap()).turn,4);
 await page.locator('#art').check();await page.locator('#art').uncheck();await page.locator('#forward').click();assert.equal((await snap()).turn,5);await page.locator('#back').click();assert.equal((await snap()).turn,4);
 for(const [width,height] of [[320,640],[390,844],[430,932],[1280,900]]){
  await page.setViewportSize({width,height});
  const issues=await page.evaluate(()=>{const issues=[];if(document.documentElement.scrollWidth>innerWidth+1)issues.push('horizontal overflow');for(const e of document.querySelectorAll('button,select')){const r=e.getBoundingClientRect();if(r.width>0&&r.height>0&&(r.width<43.5||r.height<43.5))issues.push('small control');}const r=document.getElementById('board').getBoundingClientRect();if(r.width<150||r.height<200)issues.push('collapsed canvas');return issues;});assert.deepEqual(issues,[]);layouts++;
 }
 await page.setViewportSize({width:390,height:844});await page.locator('#levelTitle').scrollIntoViewIfNeeded();await page.screenshot({path:resolve(OUT,'review-mobile.png')});
 const download=page.waitForEvent('download');await page.locator('#planDownload').click();const file=await download;await file.saveAs(resolve(OUT,'plan.md'));assert.equal(await readFile(resolve(OUT,'plan.md'),'utf8'),await readFile(resolve(ROOT,'product/plans/gameplay-pilot-v3-plan.md'),'utf8'));
 assert.deepEqual(errors,[]);assert.deepEqual(external,[]);
 const check={passed:true,htmlSha256:createHash('sha256').update(html).digest('hex'),attemptsReplayed:checked,layouts,unknownColoursStayHidden:true,completedHalfRendered:true,fatalJumpAndStepControls:true,planDownloadMatches:true,noBrowserErrors:true,noExternalRequests:true};
 await writeFile(resolve(folder,'viewer-check.json'),JSON.stringify(check,null,2)+'\n');console.log(JSON.stringify(check));
}finally{await browser.close();await new Promise(r=>server.close(r));}

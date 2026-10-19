import {chromium} from '../heroanim/node_modules/playwright-core/index.mjs';
import {readFile,writeFile,mkdir} from 'node:fs/promises';
import http from 'node:http';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {resolve,dirname} from 'node:path';
import {fileURLToPath} from 'node:url';

const ROOT=resolve(dirname(fileURLToPath(import.meta.url)),'../..');
const OUT=process.argv.find(a=>a.startsWith('--out='))?.slice(6)||'/tmp/bloomlings-pilot-browser';
await mkdir(OUT,{recursive:true});
const html=await readFile(resolve(ROOT,'product/previews/gameplay-pilot/index.html'));
const certificates=JSON.parse(await readFile(resolve(ROOT,'content/pilot-v1/certificates.json'),'utf8'));
const server=http.createServer((req,res)=>{res.setHeader('Content-Type','text/html; charset=utf-8');res.end(html);});
await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
const url=`http://127.0.0.1:${server.address().port}/`;
const browser=await chromium.launch({executablePath:'/usr/bin/chromium',headless:true,args:['--no-sandbox','--disable-dev-shm-usage']});
const errors=[],external=[];let screenshots=0,playthroughs=0,layoutChecks=0;
const context=await browser.newContext({viewport:{width:390,height:844},deviceScaleFactor:2,reducedMotion:'reduce',acceptDownloads:true});
const page=await context.newPage();
page.on('pageerror',e=>errors.push(e.message));page.on('request',r=>{if(!r.url().startsWith(url)&&!r.url().startsWith('data:')&&!r.url().startsWith('blob:'))external.push(r.url());});
async function dismiss(){if(await page.locator('#lessonDialog').evaluate(n=>n.open))await page.locator('#lessonButton').click();}
async function select(id){
  if(await page.locator('#endDialog').evaluate(n=>n.open))await page.locator('#endMapButton').click();else await page.locator('#levelsButton').click();
  await page.locator(`[data-level="${id}"]`).click();await dismiss();
}
async function action(a){await page.locator(a.type==='tap'?`.pod-top[data-pod="${a.pod}"]`:`[data-bridge="${a.choice}"]`).click();await page.waitForFunction(()=>!window.bloomlingsPilot.snapshot().busy);}
async function layout(){
  const issues=await page.evaluate(()=>{
    const issues=[],visible=e=>e.getBoundingClientRect().width>0&&e.getBoundingClientRect().height>0;
    if(document.documentElement.scrollWidth>innerWidth+1)issues.push('horizontal overflow');
    const game=document.getElementById('game').getBoundingClientRect();if(game.bottom>innerHeight+1)issues.push('game exceeds viewport');
    const board=document.getElementById('board').getBoundingClientRect();if(board.height<90||board.width<150)issues.push('board collapsed');
    for(const b of document.querySelectorAll('#game button'))if(visible(b)){const r=b.getBoundingClientRect();if(r.width<43.5||r.height<43.5)issues.push(`${b.id||b.dataset.pod||b.dataset.bridge} target ${r.width}×${r.height}`);}
    for(const column of document.querySelectorAll('.column')){const box=column.getBoundingClientRect();for(const pod of column.children)if(pod.getBoundingClientRect().bottom>box.bottom+1)issues.push('queue clipped');}
    for(const img of document.querySelectorAll('img'))if(visible(img)&&img.src&&!img.complete)issues.push('image not decoded');
    return issues;
  });
  assert.deepEqual(issues,[]);layoutChecks++;
}
try{
  await page.goto(url);await page.locator('#startButton').click();await dismiss();
  await page.evaluate(()=>document.fonts.ready);await layout();
  for(let id=1;id<=30;id++){
    if(id!==1)await select(id);
    const cert=certificates.find(c=>c.id===id);
    if(id===16){await page.screenshot({path:resolve(OUT,'level16.png')});screenshots++;}
    for(const a of cert.actions){
      if(id===25&&a.type==='bridge'){await page.screenshot({path:resolve(OUT,'level25-bridge.png')});screenshots++;}
      await action(a);
    }
    assert.equal((await page.evaluate(()=>window.bloomlingsPilot.snapshot())).status,'Won');playthroughs++;
    await page.locator('[data-rating="good"]').click();await page.locator('[data-art="yes"]').click();await page.locator('#levelNote').fill(`QA L${id}`);
    if(id===30){await page.screenshot({path:resolve(OUT,'finished30.png')});screenshots++;}
  }
  // A complete certified losing path must produce the retry flow in the UI.
  await select(16);await page.locator('#endRetryButton').click();
  for(const a of certificates.find(c=>c.id===16).losing)await action(a);
  assert.equal((await page.evaluate(()=>window.bloomlingsPilot.snapshot())).status,'Jammed');
  await page.locator('#endRetryButton').click();assert.equal((await page.evaluate(()=>window.bloomlingsPilot.snapshot())).commands,0);
  // Save/reload and level switching must preserve the exact partially solved board.
  const part=certificates.find(c=>c.id===16).actions.slice(0,3);for(const a of part)await action(a);
  const saved=await page.evaluate(()=>window.bloomlingsPilot.snapshot());await page.reload();await dismiss();
  assert.deepEqual(await page.evaluate(()=>window.bloomlingsPilot.snapshot()),saved);
  await select(17);await select(16);assert.deepEqual(await page.evaluate(()=>window.bloomlingsPilot.snapshot()),saved);
  await page.locator('#retryButton').click();await page.locator('[data-close="retryDialog"]').click();assert.deepEqual(await page.evaluate(()=>window.bloomlingsPilot.snapshot()),saved);
  await page.locator('#retryButton').click();await page.locator('#confirmRetryButton').click();assert.equal((await page.evaluate(()=>window.bloomlingsPilot.snapshot())).commands,0);
  // Every future capsule must fit; touch targets stay usable on short phones.
  for(const [width,height] of [[320,640],[360,640],[390,844],[430,932],[768,1024],[1280,900]]){
    await page.setViewportSize({width,height});
    for(const id of [1,16,25,28,30]){
      await select(id);
      if(await page.locator('#endDialog').evaluate(n=>n.open))await page.locator('#endRetryButton').click();
      await layout();
    }
  }
  await page.setViewportSize({width:390,height:844});
  // Export is an actual browser download, with the full replay and optional feedback.
  await page.locator('#levelsButton').click();await page.locator('#resultsButton').click();
  const download=page.waitForEvent('download');await page.locator('#exportButton').click();const file=await download;await file.saveAs(resolve(OUT,'results.json'));
  const report=JSON.parse(await readFile(resolve(OUT,'results.json'),'utf8'));
  assert.equal(report.format,'bloomlings-pilot-results-1');assert.equal(new Set(report.attempts.filter(a=>a.outcome==='won').map(a=>a.level)).size,30);
  assert(report.attempts.some(a=>a.note==='QA L16'&&a.rating==='good'&&a.art==='yes'));
  assert(report.attempts.some(a=>a.outcome==='jammed'));assert(report.attempts.some(a=>a.outcome==='abandoned'));
  await page.locator('[data-close="resultsDialog"]').click();
  // Normal animation blocks repeated taps while saving the logical turn first.
  await select(16);await page.emulateMedia({reducedMotion:'no-preference'});
  if((await page.evaluate(()=>window.bloomlingsPilot.snapshot())).commands) {await page.locator('#retryButton').click();await page.locator('#confirmRetryButton').click();}
  const first=certificates.find(c=>c.id===16).actions[0];
  const top=page.locator(`[data-pod="${first.pod}"]`);await top.click();
  assert.equal((await page.evaluate(()=>window.bloomlingsPilot.snapshot())).commands,1);
  await page.evaluate(p=>{const b=document.querySelector(`[data-pod="${p}"]`);b?.click();},first.pod);
  assert.equal((await page.evaluate(()=>window.bloomlingsPilot.snapshot())).commands,1);
  await page.reload();assert.equal((await page.evaluate(()=>window.bloomlingsPilot.snapshot())).commands,1);
  assert.equal((await page.evaluate(()=>window.bloomlingsPilot.snapshot())).busy,false);
  assert.deepEqual(errors,[]);assert.deepEqual(external,[]);
  const summary={passed:true,htmlSha256:createHash('sha256').update(html).digest('hex'),playthroughs,layoutChecks,saveRestore:true,levelSwitch:true,freeRetry:true,realResultsDownload:true,animationInputGuard:true,saveBeforeAnimation:true,noExternalRequests:true,noBrowserErrors:true,screenshots};
  await writeFile(resolve(OUT,'browser.json'),JSON.stringify(summary,null,2)+'\n');console.log(JSON.stringify(summary));
}finally{await browser.close();await new Promise(resolve=>server.close(resolve));}

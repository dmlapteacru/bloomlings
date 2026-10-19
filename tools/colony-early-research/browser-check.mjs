import {chromium} from '../heroanim/node_modules/playwright-core/index.mjs';
import {readFile,writeFile,mkdir} from 'node:fs/promises';
import {createHash} from 'node:crypto';
import assert from 'node:assert/strict';
import http from 'node:http';
const root=new URL('../../',import.meta.url),out='/tmp/colony-v4-plan-browser';await mkdir(out,{recursive:true});
const html=await readFile(new URL('product/previews/gameplay-v4-plan/index.html',root));
const server=http.createServer((req,res)=>{res.setHeader('Content-Type','text/html; charset=utf-8');res.end(html);});await new Promise(r=>server.listen(0,'127.0.0.1',r));
const origin=`http://127.0.0.1:${server.address().port}/`,browser=await chromium.launch({executablePath:'/usr/bin/chromium',args:['--no-sandbox','--disable-dev-shm-usage']});
const page=await browser.newPage({viewport:{width:390,height:844},deviceScaleFactor:2});const errors=[],external=[];
page.on('pageerror',e=>errors.push(e.message));page.on('request',r=>{if(!r.url().startsWith(origin)&&!r.url().startsWith('data:'))external.push(r.url());});
try{
 await page.goto(origin);await page.waitForFunction(()=>document.getElementById('caseImage').complete);
 assert.equal(await page.locator('#levelCards article').count(),30);assert.equal(await page.locator('#levelCards .shown').count(),10);
 assert.equal(await page.locator('#sourceList a').count(),30);assert.equal(await page.locator('#swatches .swatch').count(),7);
 for(const n of [1,4,5,8,15]){await page.locator(`#caseTabs [data-value="${n}"]`).click();await page.waitForFunction(()=>document.getElementById('caseImage').complete&&document.getElementById('caseImage').naturalWidth>0);assert((await page.locator('#caseCaption').textContent()).startsWith('L'+n+' '));if([4,8,15].includes(n)){await page.locator('#queueDetails summary').click();await page.waitForFunction(()=>document.getElementById('queueImage').complete&&document.getElementById('queueImage').naturalWidth>0);}}
 await page.locator('#visionTabs [data-value="protan"]').click();assert((await page.locator('#paletteRisk').textContent()).includes('G/A'));assert((await page.locator('#paletteRisk').textContent()).includes('1.41'));
 const protan=await page.locator('#swatches .chip').first().evaluate(n=>n.style.backgroundColor);await page.locator('#visionTabs [data-value="normal"]').click();assert.notEqual(await page.locator('#swatches .chip').first().evaluate(n=>n.style.backgroundColor),protan);
 for(const mode of ['deutan','tritan','normal']){await page.locator(`#visionTabs [data-value="${mode}"]`).click();assert.equal(await page.locator('#swatches .swatch').count(),7);}
 for(const n of [3,6]){await page.locator(`[data-colors="${n}"]`).click();assert.equal(await page.locator('#conceptBoard').getAttribute('data-materials'),String(n));}
 for(const n of [1,11,21]){await page.locator(`#rangeTabs [data-value="${n}"]`).click();assert.equal(await page.locator('#levelCards .shown').count(),10);assert.equal(await page.locator('#levelCards .shown').first().getAttribute('data-level'),String(n));}
 await page.locator('#units').evaluate(n=>{n.value=30;n.dispatchEvent(new Event('input'));});await page.locator('#cycle').evaluate(n=>{n.value=1.2;n.dispatchEvent(new Event('input'));});assert((await page.locator('#tempoEstimate').textContent()).includes('36.0 с'));
 await page.locator('#walkers').selectOption('2');assert((await page.locator('#tempoEstimate').textContent()).includes('18.0 с'));
 await page.locator('#playTempo').click();await page.waitForFunction(()=>document.getElementById('tempoClock').textContent==='5,0 с',{},{timeout:10000});assert.equal(await page.locator('#fastLeft').textContent(),'0 осталось');assert.notEqual(await page.locator('#routeLeft').textContent(),'0 осталось');await page.locator('#resetTempo').click();assert.equal(await page.locator('#tempoClock').textContent(),'0,0 с');
 let layouts=0;for(const [width,height] of [[320,640],[390,844],[430,932],[1280,900]]){await page.setViewportSize({width,height});assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1),JSON.stringify(await page.evaluate(()=>Array.from(document.querySelectorAll('main *')).map(n=>({tag:n.tagName,id:n.id,cls:n.className,left:n.getBoundingClientRect().left,right:n.getBoundingClientRect().right})).filter(n=>n.left<0||n.right>innerWidth+1).slice(0,18))));const bad=await page.locator('button').evaluateAll(ns=>ns.filter(n=>n.getBoundingClientRect().width>0&&(n.getBoundingClientRect().height<43.5||n.getBoundingClientRect().width<43.5)).map(n=>n.textContent));assert.deepEqual(bad,[]);layouts++;}
 await page.setViewportSize({width:390,height:844});await page.locator('#caseTabs [data-value="4"]').click();await page.evaluate(()=>scrollTo(0,0));await page.screenshot({path:out+'/mobile-top.png'});await page.locator('#palette').scrollIntoViewIfNeeded();await page.screenshot({path:out+'/mobile-palette.png'});
 assert.deepEqual(errors,[]);assert.deepEqual(external,[]);
 const report={passed:true,htmlSha256:createHash('sha256').update(html).digest('hex'),caseImages:5,queueSheets:3,proposalLevels:30,sourceLinks:30,paletteModes:4,protanRiskVisible:true,threeSixColourIllustration:true,tempoArithmetic:true,tempoAnimation:true,roadmapFilters:true,layouts,noExternalRequests:true,noBrowserErrors:true,limits:['Browser inspection of an analysis page, not a playtest of new levels.','Colour simulation is diagnostic, not a real-device accessibility certification.']};await writeFile(out+'/validation.json',JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify(report));
}finally{await browser.close();await new Promise(r=>server.close(r));}

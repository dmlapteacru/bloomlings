// Phone-size initial boards and level map, through public UI only.
import fs from 'node:fs';import http from 'node:http';import {chromium} from '../heroanim/node_modules/playwright-core/index.mjs';
const root='product/previews/gameplay-pilot-v9',html=fs.readFileSync(root+'/index.html');
const server=http.createServer((req,res)=>{res.setHeader('Content-Type','text/html; charset=utf-8');res.end(html);});await new Promise(r=>server.listen(0,'127.0.0.1',r));
const browser=await chromium.launch({executablePath:'/usr/bin/chromium',args:['--no-sandbox','--disable-dev-shm-usage']});
try{const page=await browser.newPage({viewport:{width:390,height:844},deviceScaleFactor:1,reducedMotion:'reduce'});await page.goto(`http://127.0.0.1:${server.address().port}/`);const click=s=>page.locator(s).evaluate(n=>n.click());await click('#startButton');
 for(const id of [4,8,10,12,13,16,25,29]){
  if(await page.locator('#lessonDialog').evaluate(n=>n.open))await click('#lessonButton');
  await click('#levelsButton');await click(`[data-level="${id}"]`);if(await page.locator('#lessonDialog').evaluate(n=>n.open))await click('#lessonButton');
  await page.evaluate(()=>document.fonts.ready);await page.screenshot({path:`${root}/screen-l${id}.png`});
 }
 await click('#levelsButton');await page.screenshot({path:root+'/screen-map.png'});
}finally{await browser.close();await new Promise(r=>server.close(r));}

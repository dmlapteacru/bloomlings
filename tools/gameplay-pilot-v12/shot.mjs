// Screenshots of chosen levels at their start, for looking at the board: node tools/gameplay-pilot-v12/shot.mjs <out-dir> <level…>
// [--dir <built pilot>] (build.mjs --out; default product/previews/gameplay-pilot-v12)
import {createRequire} from 'node:module';import http from 'node:http';import fs from 'node:fs';import path from 'node:path';
import {root} from '../rounds/lib.mjs';
const argv=process.argv.slice(2),di=argv.indexOf('--dir'),dir=di>=0?path.resolve(argv.splice(di,2)[1]):path.join(root,'product/previews/gameplay-pilot-v12');
const [out,...lv]=argv,html=fs.readFileSync(path.join(dir,'index.html'));
const req=createRequire(import.meta.url);let pw;try{pw=req('../heroanim/node_modules/playwright-core');}catch{pw=req('playwright');}
const server=http.createServer((q,s)=>{s.setHeader('Content-Type','text/html;charset=utf-8');s.end(html);});await new Promise(r=>server.listen(0,'127.0.0.1',r));
const browser=await pw.chromium.launch({args:['--no-sandbox']});const page=await browser.newPage({viewport:{width:390,height:844},deviceScaleFactor:2});
await page.goto(`http://127.0.0.1:${server.address().port}`);const click=s=>page.locator(s).first().evaluate(n=>n.click()),open=id=>page.evaluate(i=>document.getElementById(i).open,id);
await click('#startButton');fs.mkdirSync(out,{recursive:true});
for(const n of lv){if(await open('lessonDialog'))await click('#lessonButton');await click('#levelsButton');await click(`[data-level="${n}"]`);if(await open('lessonDialog'))await click('#lessonButton');await page.waitForTimeout(400);await page.screenshot({path:path.join(out,`L${n}.png`)});}
await browser.close();server.close();

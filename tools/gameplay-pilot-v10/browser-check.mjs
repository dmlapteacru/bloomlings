// Plays every level of the built pilot v10 in Chromium with its saved solution (content/rounds/levels/L####.json, or
// the --preview folder for draft levels): opens it from the level menu, taps each action through the page's own buttons
// and lets the work settle before the next one (the solution's patient model), then expects the win dialog.
// node tools/gameplay-pilot-v10/browser-check.mjs [--preview <folder>] [--from N]
import {createRequire} from 'node:module';import http from 'node:http';import fs from 'node:fs';import path from 'node:path';
import {LEVELS,readJSON,lid,arg,root} from '../rounds/lib.mjs';
const html=fs.readFileSync(path.join(root,'product/previews/gameplay-pilot-v10/index.html')),preview=arg('--preview'),from=+arg('--from',1);
const data=JSON.parse(fs.readFileSync(path.join(root,'product/previews/gameplay-pilot-v10/levels.json'),'utf8'));
const server=http.createServer((q,s)=>{s.setHeader('Content-Type','text/html;charset=utf-8');s.end(html);});await new Promise(r=>server.listen(0,'127.0.0.1',r));
// Playwright: the repository's copy for the heroes if installed, else the machine's
const req=createRequire(import.meta.url);let pw;try{pw=req('../heroanim/node_modules/playwright-core');}catch{pw=req('playwright');}const {chromium}=pw;
let browser,failed=0;
try{browser=await chromium.launch({args:['--no-sandbox','--disable-dev-shm-usage']});
 const page=await browser.newPage({viewport:{width:390,height:844}}),errors=[];page.on('pageerror',e=>errors.push(e.message));
 const epoch=new Date('2026-10-26T12:00:00Z');await page.clock.install({time:epoch});await page.clock.pauseAt(new Date(+epoch+1000));await page.goto(`http://127.0.0.1:${server.address().port}`);
 const click=s=>page.locator(s).first().evaluate(n=>n.click()),snap=()=>page.evaluate(()=>window.bloomlingsPilot.snapshot());
 const isOpen=id=>page.evaluate(i=>document.getElementById(i).open,id);
 const settle=async()=>{for(let k=0;k<400;k++){const s=await snap();if(s.status!=='Playing'&&!s.pendingTrips)return s;if(!s.pendingTrips&&!s.pendingCells&&!s.pendingBeats)return s;await page.clock.runFor(1000);}return snap();};
 await click('#startButton');
 for(const L of data.levels.filter(l=>l.id>=from)){const file=readJSON(path.join(LEVELS,lid(L.id)+'.json'))||(preview&&readJSON(path.join(preview,lid(L.id)+'.json')));
  if(await isOpen('lessonDialog'))await click('#lessonButton');
  await click('#levelsButton');await click(`[data-level="${L.id}"]`);if(await isOpen('lessonDialog'))await click('#lessonButton');
  let s=await snap();if(s.level!==L.id){console.log(`L${L.id}: не открылся`);failed++;continue;}
  for(const a of file.solution.route){if(a.type==='bridge')await click(`[data-bridge="${a.choice}"]`);else await click(`#tray [data-pod="${a.pod}"]`);await page.clock.runFor(300);s=await settle();}
  for(let k=0;k<600&&s.status==='Playing';k++){await page.clock.runFor(1000);s=await snap();}
  const won=s.status==='Won';if(!won)failed++;
  console.log(`L${L.id} ${L.difficulty.padEnd(10)} ${won?'победа':'НЕТ: '+s.status} · ${s.commands} команд · ${Math.round(s.modelTimeMs/1000)} с модели`);
  if(await isOpen('endDialog'))await page.keyboard.press('Escape');}
 if(errors.length){console.log('ошибки страницы:',errors);failed++;}
 await page.screenshot({path:'/tmp/bloomlings-pilot-v10-check.png'});
}finally{if(browser)await browser.close();server.close();}
console.log(failed?`НЕ ПРОЙДЕНО: ${failed}`:'PASS: все уровни выиграны сохранённым решением в браузере, ошибок страницы нет');process.exit(failed?1:0);

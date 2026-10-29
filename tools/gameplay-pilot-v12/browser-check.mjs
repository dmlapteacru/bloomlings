// Plays every level of the built pilot v12 in Chromium with its saved solution (content/rounds/levels/L####.json, or
// the --preview folder for draft levels): opens it from the level menu, taps each action through the page's own buttons
// and lets the work settle before the next one (the solution's patient model), then expects the win dialog.
// node tools/gameplay-pilot-v12/browser-check.mjs [--pilot <folder>] [--preview <folder>] [--from N] [--to M] [--screenshots <folder>] [--frames]
import {createRequire} from 'node:module';import http from 'node:http';import fs from 'node:fs';import path from 'node:path';
import {LEVELS,readJSON,lid,arg,flag,root} from '../rounds/lib.mjs';
const pilot=arg('--pilot',path.join(root,'product/previews/gameplay-pilot-v12'));
const html=fs.readFileSync(path.join(pilot,'index.html')),preview=arg('--preview'),from=+arg('--from',1),to=+arg('--to',1e9);
const data=JSON.parse(fs.readFileSync(path.join(pilot,'levels.json'),'utf8'));
const screenshots=arg('--screenshots');if(screenshots)fs.mkdirSync(screenshots,{recursive:true});
const server=http.createServer((q,s)=>{s.setHeader('Content-Type','text/html;charset=utf-8');s.end(html);});await new Promise(r=>server.listen(0,'127.0.0.1',r));
// Playwright: the repository's copy for the heroes if installed, else the machine's
const req=createRequire(import.meta.url);let pw;try{pw=req('../heroanim/node_modules/playwright-core');}catch{pw=req('playwright');}const {chromium}=pw;
let browser,failed=0;
try{browser=await chromium.launch({...(process.env.CHROMIUM_PATH?{executablePath:process.env.CHROMIUM_PATH}:{}),args:['--no-sandbox','--disable-dev-shm-usage']});
 const page=await browser.newPage({viewport:{width:390,height:844}}),errors=[];page.on('pageerror',e=>errors.push(e.message));
 const epoch=new Date('2026-10-26T12:00:00Z');await page.clock.install({time:epoch});await page.clock.pauseAt(new Date(+epoch+1000));await page.goto(`http://127.0.0.1:${server.address().port}`);
 // A skipped animation frame still processes every model event in syncTime. Use --frames for full visual pacing.
 const advance=ms=>flag('--frames')?page.clock.runFor(ms):page.clock.fastForward(ms);
 const click=s=>page.locator(s).first().evaluate(n=>n.click()),snap=()=>page.evaluate(()=>window.bloomlingsPilot.snapshot());
 const isOpen=id=>page.evaluate(i=>document.getElementById(i).open,id);
 const settle=async()=>{for(let k=0;k<400;k++){const s=await snap();if(s.status!=='Playing'&&!s.pendingTrips)return s;if(!s.pendingTrips&&!s.pendingCells&&!s.pendingBeats)return s;await advance(1000);}return snap();};
 await click('#startButton');
 for(const L of data.levels.filter(l=>l.id>=from&&l.id<=to)){const file=readJSON(path.join(LEVELS,lid(L.id)+'.json'))||(preview&&readJSON(path.join(preview,lid(L.id)+'.json')));
  if(await isOpen('lessonDialog'))await click('#lessonButton');
  await click('#levelsButton');await click(`[data-level="${L.id}"]`);if(await isOpen('lessonDialog'))await click('#lessonButton');
  let s=await snap();if(s.level!==L.id){console.log(`L${L.id}: не открылся`);failed++;continue;}
  if(screenshots){await advance(20);await page.screenshot({path:path.join(screenshots,lid(L.id)+'.png')});}
  for(const a of file.solution.route){if(a.type==='bridge')await click(`[data-bridge="${a.choice}"]`);else await click(`#tray [data-pod="${a.pod}"]`);await advance(300);s=await settle();}
  for(let k=0;k<600&&s.status==='Playing';k++){await advance(1000);s=await snap();}
  const won=s.status==='Won';if(!won)failed++;
  console.log(`L${L.id} ${L.difficulty.padEnd(10)} ${won?'победа':'НЕТ: '+s.status} · ${s.commands} команд · ${Math.round(s.modelTimeMs/1000)} с модели`);
  if(won&&await isOpen('endDialog')){
   await click('#nextButton');
   const next=data.levels[data.levels.findIndex(x=>x.id===L.id)+1];
   if(next){if((await snap()).level!==next.id){console.log(`L${L.id}: Next должен открыть L${next.id}`);failed++;}}
   else if(!await isOpen('resultsDialog')){console.log(`L${L.id}: последний уровень не открыл результаты`);failed++;}
   if(await isOpen('resultsDialog'))await page.keyboard.press('Escape');
  }else if(await isOpen('endDialog'))await page.keyboard.press('Escape');}
 if(errors.length){console.log('ошибки страницы:',errors);failed++;}
 await page.screenshot({path:'/tmp/bloomlings-pilot-v12-check.png'});
}finally{if(browser)await browser.close();server.close();}
console.log(failed?`НЕ ПРОЙДЕНО: ${failed}`:'PASS: все уровни выиграны сохранённым решением в браузере, ошибок страницы нет');process.exit(failed?1:0);

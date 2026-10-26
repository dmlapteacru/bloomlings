// The speed check in the pilot app itself: the v8E page as played (product/previews/gameplay-pilot-v8e/index.html at
// 8892a6f4) with a Colony Flow reference level in place of its catalogue, driven in Chromium through its real DOM
// (capsule taps) on the browser's own clock (Playwright clock: timers, animation frames and performance.now). Taps
// come at their recorded seconds from the first tap; a tap the page does not take yet is retried every 100 ms. Every
// 250 ms of page time the page's own inspection (window.bloomlingsPilot.snapshot: the drawn board, the slots, the
// page's elapsed time) is sampled. The same taps are played on our engine (trace.mjs) and turned into page time (the
// tail at the page's tail speed), and both are compared with the video.
// node tools/level-stats/app-check.mjs [--tail 3] → tools/level-stats/out/app-check.json
import fs from 'node:fs';import path from 'node:path';import {execSync} from 'node:child_process';import {createRequire} from 'node:module';
import {allRecords,root,V8E_COMMIT} from './levels.mjs';import {engine} from './engines.mjs';import {run} from './trace.mjs';
const {chromium}=createRequire(import.meta.url)('playwright');
const arg=k=>{const i=process.argv.indexOf(k);return i>0?process.argv[i+1]:null;};const TAIL=+(arg('--tail')||3);
// CF L26 is not here: it has a vertical and two diagonal links, which only Colony Flow's link rule takes (the pilot
// refuses a linked capsule whose partner is not on top); the timeline the calibration uses is the same either way
const REFS=[['CF-2',1],['CF-10',2]].filter(([k])=>!arg('--only')||arg('--only').split(',').includes(k));
const page0=execSync(`git show ${V8E_COMMIT}:product/previews/gameplay-pilot-v8e/index.html`,{cwd:root,maxBuffer:1<<28}).toString();
const recs=Object.fromEntries(allRecords().map(r=>[r.key,r])),E=await engine('v8E');
const r2=x=>x==null||!Number.isFinite(x)?null:Math.round(x*100)/100;
function commands(model,a){const tray=model.tray,heads=tray.map(()=>0),t0=a.taps[0].t,out=[];
 for(const x of a.taps){const c=+x.column.slice(1)-1,p=tray[c][heads[c]];heads[c]++;out.push({t:Math.round(1000*(x.t-t0)),pod:p});}return out;}
function pageHtml(level){const d=JSON.parse(/<script id="pilotData" type="application\/json">([\s\S]*?)<\/script>/.exec(page0)[1]);
 d.levels=[{...level,id:1,title:'Colony Flow reference',difficulty:'Normal'}];
 let h=page0.replace(/(<script id="pilotData" type="application\/json">)[\s\S]*?(<\/script>)/,(m,a,b)=>a+JSON.stringify(d).replace(/</g,'\\u003c')+b);
 if(TAIL!==3){const k="const TAIL=Object.freeze({departureGap:250,speed:3,";if(h.split(k).length!==2)throw Error('tail constant moved');h=h.replace(k,k.replace('speed:3','speed:'+TAIL));}
 return h;}
const MS=[.2,.4,.6,.8,1];
const at=(cl,m)=>{const p=cl.find(([,v])=>v>=m-1e-9);return p?p[0]:null;};
const browser=await chromium.launch({args:['--disable-dev-shm-usage']});browser.on('disconnected',()=>console.error('BROWSER DISCONNECTED'));const out={tail:TAIL,commit:V8E_COMMIT,refs:{}};
try{for(const [key,attempt] of REFS){const r=recs[key],a=r.attempts.find(x=>x.attempt===attempt),level={...r.model};delete level.objects;
 const cmds=commands(level,a),html=pageHtml(level),file=path.join(root,'tools/level-stats/out/app-'+key+'-tail'+TAIL+'.html');fs.writeFileSync(file,html);
 const page=await browser.newPage({viewport:{width:390,height:844}}),errors=[];page.on('pageerror',e=>errors.push(e.message));page.on('crash',()=>console.error('PAGE CRASH',key));page.on('close',()=>console.error('PAGE CLOSE',key));
 const epoch=new Date('2026-10-20T12:00:00Z');await page.clock.install({time:epoch});await page.clock.pauseAt(new Date(+epoch+1000));await page.goto('file://'+file);
 for(const s of ['#startButton','#lessonButton'])if(await page.locator(s).count())await page.locator(s).evaluate(n=>n.click()).catch(()=>{});
 // the page's own inspection, reduced in the page: cells still drawn, filled slots, elapsed time, commands, status
 const snap=()=>page.evaluate(()=>{const x=window.bloomlingsPilot.snapshot();return {left:x.visual.board.filter(v=>v>=0).length,filled:x.visual.slots.filter(v=>v>=0).length,elapsedMs:x.elapsedMs,commands:x.commands,status:x.status};});let s=await snap();
 const total=s.left,e0=s.elapsedMs,samples=[],taps=[];let shown=0;
 const sample=async()=>{s=await snap();samples.push([(s.elapsedMs-e0)/1000,1-s.left/total,s.filled,s.status]);if(s.elapsedMs-e0-shown>30000){shown=s.elapsedMs-e0;process.stderr.write(`${key} ${Math.round(shown/1000)} s\n`);}};
 const step=async ms=>{let left=ms;while(left>0){const d=Math.min(250,left);await page.clock.runFor(d);left-=d;await sample();}};
 for(const c of cmds){const now=s.elapsedMs-e0;if(c.t>now)await step(c.t-now);let tries=0;
  while(true){const n0=s.commands;await page.locator(`[data-pod="${c.pod}"]`).first().evaluate(n=>n.click()).catch(()=>{});s=await snap();if(s.commands>n0){taps.push([(s.elapsedMs-e0)/1000,(s.elapsedMs-e0-c.t)/1000]);break;}if(++tries>3000)throw Error(key+': tap refused for good');await step(100);}}
 let guard=0;while(!/won|lost|jam/i.test(String(s.status))&&guard++<4000)await step(250);
 await page.close();
 // the engine: the same taps at the same seconds, page time with the tail at TAIL
 const tr=run(E,level,{commands:cmds.map(c=>({t:c.t,action:{type:'tap',pod:c.pod}}))}),ts=tr.taps.at(-1).t,pg=ms=>ms<=ts?ms:ts+(ms-ts)/TAIL,cells=tr.cells;
 const eng=tr.samples.map(x=>[pg(x[0])/1000,x[5]/cells,tr.capacity-x[1]]);
 // the video: board share and filled slots from tools/level-stats/observed
 const o=JSON.parse(fs.readFileSync(path.join(root,'tools/level-stats/observed',key.replace('CF-','L')+'.json'),'utf8')).attempts.find(x=>x.attempt===attempt),t0=a.taps[0].t;
 const vid=o.board.filter(b=>b[1]!=null).map(([t,v])=>[t-t0,1-v]);
 const vms=MS.map(m=>{const p=vid.find(([,v])=>v>=(m===1?.995:m));return p?p[0]:null;});
 const app=MS.map(m=>at(samples.map(x=>[x[0],x[1]]),m===1?1:m)),en=MS.map(m=>at(eng,m));
 // filled slots: app vs engine, mean |difference| on a 0.25 s grid
 const val=(ser,t,k)=>{let v=null;for(const x of ser){if(x[0]<=t+1e-9)v=x[k];else break;}return v;};let d=0,n=0;const end=Math.min(app[4]??0,en[4]??0);for(let t=0;t<=end;t+=.25){const p=val(samples,t,2),q=val(eng,t,2);if(p!=null&&q!=null){d+=Math.abs(p-q);n++;}}
 out.refs[key]={attempt,cells:total,taps:cmds.length,errors,milestones:MS.map((m,i)=>({share:m,video:r2(vms[i]),app:r2(app[i]),engine:r2(en[i]),appMinusEngine:r2(app[i]!=null&&en[i]!=null?app[i]-en[i]:null)})),
  tapDelays:{app:taps.filter(x=>x[1]>0.25).length,appMax:r2(Math.max(0,...taps.map(x=>x[1]))),engine:tr.taps.filter((x,i)=>x.t-cmds[i].t>250).length,engineMax:r2(Math.max(0,...tr.taps.map((x,i)=>(x.t-cmds[i].t)/1000)))},
  slotsMaeAppVsEngine:r2(d/Math.max(1,n)),status:{app:s.status,engine:tr.status},
  series:{app:samples.filter((x,i)=>i%4===0).map(x=>[r2(x[0]),r2(x[1]),x[2]]),engine:eng.filter((x,i,arr)=>i===0||x[2]!==arr[i-1][2]||i%20===0).map(x=>[r2(x[0]),r2(x[1]),x[2]])}};
 console.log(key,JSON.stringify(out.refs[key].milestones.map(x=>[x.share,x.video,x.app,x.engine])),'delays',JSON.stringify(out.refs[key].tapDelays),'slotsMae',out.refs[key].slotsMaeAppVsEngine,JSON.stringify(out.refs[key].status),errors.length?'ERR '+errors[0]:'');}}
finally{await browser.close();}
fs.writeFileSync(path.join(root,'tools/level-stats/out/app-check'+(TAIL!==3?'-tail'+TAIL:'')+'.json'),JSON.stringify(out));

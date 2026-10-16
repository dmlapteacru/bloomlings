// Build a self-contained offline HTML viewer; verify and render the actual GLB.
// node tools/heroanim/review-sprig-outfit.mjs content/work/sprig-outfit-study/build
import fs from 'node:fs';
import path from 'node:path';
import http from 'node:http';
import {fileURLToPath} from 'node:url';
import {createHash} from 'node:crypto';
import {spawnSync} from 'node:child_process';
import {chromium} from 'playwright-core';
const here=path.dirname(fileURLToPath(import.meta.url));
const out=path.resolve(process.argv[2]??'content/work/sprig-outfit-study/build');
const review=path.resolve(here,'../../output/sprig-outfit-study');
const three=path.join(here,'node_modules/three');
const sha=b=>createHash('sha256').update(b).digest('hex');
const model=fs.readFileSync(path.join(out,'sprig-outfit.glb'));
const build=JSON.parse(fs.readFileSync(path.join(out,'build-report.json')));
if(sha(model)!==build.glbSha256)throw Error('Model changed after build validation');
fs.mkdirSync(review,{recursive:true});
const uri=code=>'data:text/javascript;base64,'+Buffer.from(code).toString('base64');
const utils=uri(fs.readFileSync(path.join(three,'examples/jsm/utils/BufferGeometryUtils.js'),'utf8'));
const loader=uri(fs.readFileSync(path.join(three,'examples/jsm/loaders/GLTFLoader.js'),'utf8').replace('../utils/BufferGeometryUtils.js',utils));
const imports={three:uri(fs.readFileSync(path.join(three,'build/three.module.js'),'utf8')),
 'outfit-loader':loader,'outfit-orbit':uri(fs.readFileSync(path.join(three,'examples/jsm/controls/OrbitControls.js'),'utf8'))};
const license=fs.readFileSync(path.join(three,'LICENSE'),'utf8');
const html=`<!doctype html><html lang="ru"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Sprig — пример кастомизации</title><style>
*{box-sizing:border-box}body{margin:0;background:#f1eee5;color:#344b38;font:16px system-ui,sans-serif}main{max-width:1100px;margin:auto;padding:20px;display:grid;grid-template-columns:minmax(0,1fr) 290px;gap:20px}h1{font-size:23px;margin:0 0 8px}p{line-height:1.5}#stage{min-height:480px;border-radius:24px;overflow:hidden;background:#f1eee5}canvas{display:block;width:100%;touch-action:none}aside{padding:22px;background:#fffdf5;border:1px solid #d8decd;border-radius:24px;align-self:center}label{display:block;margin:17px 0}input{accent-color:#56813a;width:19px;height:19px;vertical-align:middle;margin-right:9px}select,button{font:inherit;color:inherit;background:#e6eddc;border:1px solid #cad5bd;border-radius:12px;padding:10px;cursor:pointer}select{width:100%;margin:8px 0 14px}.buttons{display:flex;flex-wrap:wrap;gap:7px}small{display:block;font-size:12px;line-height:1.5;color:#68765f;margin-top:18px}#status{padding:0 20px 15px;text-align:center;font-size:13px}details{max-width:1050px;margin:0 auto 20px;padding:0 20px;font-size:12px}pre{white-space:pre-wrap}@media(max-width:720px){main{grid-template-columns:1fr;padding:10px;gap:10px}#stage{min-height:360px}aside{padding:18px}}
</style><script type="importmap">${JSON.stringify({imports})}</script>
<main><div id="stage"></div><aside><h1>Sprig</h1><p>Пример смены головы и аксессуаров</p>
<label><input id="cap" type="checkbox" checked>Голова в кепке</label>
<label><input id="glasses" type="checkbox" checked>Очки</label>
<label><input id="backpack" type="checkbox" checked>Рюкзак</label>
<label for="motion">Тестовое движение</label><select id="motion"><option value="Study_Idle">Ожидание</option><option value="Study_Look">Повороты головы</option><option value="Study_Step">Шаги на месте</option><option value="Study_Joy">Радость</option></select>
<div class="buttons"><button id="play">Пауза</button><button id="speed">1×</button><button id="front">Спереди</button><button id="quarter">¾</button><button id="back">Сзади</button></div>
<small>Вращение — перетаскиванием. Масштаб — колёсиком или жестом.<br>Кепка включает подготовленную форму CapOn. Исходная причёска возвращается при снятии.<br>Study_* — новые проверочные движения. В присланном FBX клипы оказались неподвижными.</small></aside></main>
<div id="status">Загрузка встроенной модели…</div><details><summary>Three.js — лицензия MIT</summary><pre>${license.replaceAll('&','&amp;').replaceAll('<','&lt;')}</pre></details>
<script type="module">
import * as T from 'three';import {GLTFLoader} from 'outfit-loader';import {OrbitControls} from 'outfit-orbit';
const status=document.querySelector('#status');
try{
 const stage=document.querySelector('#stage');
 const renderer=new T.WebGLRenderer({antialias:true,preserveDrawingBuffer:true});renderer.setPixelRatio(1);renderer.setClearColor(0xf1eee5);renderer.outputColorSpace=T.SRGBColorSpace;renderer.toneMapping=T.NoToneMapping;stage.append(renderer.domElement);
 const scene=new T.Scene();scene.add(new T.HemisphereLight(0xffffff,0xc7d4bc,2));for(const [p,k] of [[[-3,4,4],1.4],[[3,2,4],.8],[[1,3,-3],1]]){const l=new T.DirectionalLight(0xffffff,k);l.position.set(...p);scene.add(l);}
 const camera=new T.OrthographicCamera(-1.35,1.35,1.35,-1.35,.1,30);camera.position.set(.043,1,5);camera.lookAt(.043,1,0);
 const orbit=new OrbitControls(camera,renderer.domElement);orbit.target.set(.043,1,0);orbit.enablePan=false;orbit.minZoom=.6;orbit.maxZoom=2.5;orbit.update();
 const b=Uint8Array.from(atob('${model.toString('base64')}'),c=>c.charCodeAt(0));
 const gltf=await new GLTFLoader().parseAsync(b.buffer,'');scene.add(gltf.scene);
 gltf.scene.traverse(o=>{if(o.isMesh)o.frustumCulled=false;});
 const rig=gltf.scene.getObjectByName('SprigHeroRig'),hero=gltf.scene.getObjectByName('SprigHero');
 if(!hero?.isSkinnedMesh||hero.morphTargetDictionary?.CapOn===undefined)throw Error('Missing owner head variant');
 const pieces={cap:gltf.scene.getObjectByName('SprigHero_Cap'),glasses:gltf.scene.getObjectByName('SprigHero_Glasses'),backpack:gltf.scene.getObjectByName('SprigHero_Backpack')};
 if(Object.values(pieces).some(o=>!o))throw Error('Missing outfit piece');
 const mixer=new T.AnimationMixer(gltf.scene);let current,playing=true,speed=1,manual=false;
 const state={cap:true,glasses:true,backpack:true,clip:'Study_Idle'};
 const clips=gltf.animations.filter(c=>c.name.startsWith('Study_'));
 if(clips.length!==4)throw Error('Expected four test motions');
 const span=c=>Math.max(...c.tracks.map(t=>t.times[t.times.length-1]))-Math.min(...c.tracks.map(t=>t.times[0]));
 function select(name){const clip=clips.find(c=>c.name===name);if(!clip)throw Error('Missing clip '+name);mixer.stopAllAction();current=mixer.clipAction(clip).reset();current.play();state.clip=name;}
 function apply(){for(const k of ['cap','glasses','backpack'])pieces[k].visible=state[k];hero.morphTargetInfluences[hero.morphTargetDictionary.CapOn]=state.cap?1:0;gltf.scene.updateMatrixWorld(true);gltf.scene.traverse(o=>{if(o.isSkinnedMesh)o.skeleton.update();});}
 function resize(w,h){renderer.setSize(w,h,false);camera.left=-1.35*w/h;camera.right=1.35*w/h;camera.top=1.35;camera.bottom=-1.35;camera.updateProjectionMatrix();}
 function yaw(deg){const a=deg*Math.PI/180;camera.position.set(.043+5*Math.sin(a),1,5*Math.cos(a));camera.lookAt(.043,1,0);orbit.target.set(.043,1,0);orbit.update();}
 function sync(){for(const k of ['cap','glasses','backpack'])document.getElementById(k).checked=state[k];document.querySelector('#motion').value=state.clip;}
 for(const k of ['cap','glasses','backpack'])document.getElementById(k).onchange=e=>{state[k]=e.target.checked;apply();};
 document.querySelector('#motion').onchange=e=>select(e.target.value);
 document.querySelector('#play').onclick=()=>{playing=!playing;document.querySelector('#play').textContent=playing?'Пауза':'Играть';};
 document.querySelector('#speed').onclick=()=>{speed=speed===1?.5:1;document.querySelector('#speed').textContent=speed+'×';};
 for(const [id,a]of [['front',0],['quarter',35],['back',180]])document.getElementById(id).onclick=()=>yaw(a);
 select('Study_Idle');apply();
 let last=performance.now();function loop(now){const dt=Math.min((now-last)/1000,.05);last=now;if(!manual){if(playing)mixer.update(dt*speed);apply();const w=Math.round(stage.clientWidth),h=Math.max(400,Math.min(750,w));if(renderer.domElement.width!==w||renderer.domElement.height!==h)resize(w,h);orbit.update();renderer.render(scene,camera);}requestAnimationFrame(loop);}requestAnimationFrame(loop);
 function sample(name){const object=pieces[name]??hero,points=[];object.traverse(o=>{if(o.isSkinnedMesh){const count=o.geometry.attributes.position.count;for(let j=0;j<24;j++){const point=new T.Vector3();o.getVertexPosition(Math.floor(j*(count-1)/23),point);o.localToWorld(point);points.push(...point.toArray());}}});return points;}
 function frame(opts={}){manual=true;Object.assign(state,Object.fromEntries(['cap','glasses','backpack'].filter(k=>k in opts).map(k=>[k,opts[k]])));select(opts.clip??state.clip);mixer.setTime((opts.time??0)+1/24);apply();resize(opts.width??512,opts.height??576);camera.zoom=1;camera.updateProjectionMatrix();yaw(opts.yaw??0);renderer.render(scene,camera);sync();return renderer.domElement.toDataURL('image/png').split(',')[1];}
 window.outfitStudy={frame,state,clips:clips.map(c=>({name:c.name,duration:c.duration,span:span(c)})),allClips:gltf.animations.map(c=>c.name),snapshot(){return{...state,morph:hero.morphTargetInfluences[hero.morphTargetDictionary.CapOn],visible:Object.fromEntries(Object.entries(pieces).map(([k,o])=>[k,o.visible])),vertices:Object.fromEntries(['hero','cap','glasses','backpack'].map(n=>[n,sample(n)])),head:hero.skeleton.bones.find(b=>b.name==='Head').matrixWorld.elements.slice()};},resume(){manual=false;}};
 status.textContent='Готово · модель и библиотеки встроены, интернет не требуется';window.outfitReady=true;
}catch(e){status.textContent='Ошибка: '+e.message;console.error(e);window.outfitError=e.message;}
</script></html>`;
const viewer=path.join(out,'sprig-outfit-viewer.html');fs.writeFileSync(viewer,html);
if(process.argv.includes('--viewer-only')){console.log('Created self-contained viewer '+viewer);process.exit(0);}
// Managed Chromium blocks file:// navigation. Validate the unchanged self-contained
// HTML over a loopback document; its modules, GLB and textures remain embedded.
const server=http.createServer((req,res)=>{if(req.url!=='/'){res.writeHead(404);res.end();return;}res.setHeader('Content-Type','text/html; charset=utf-8');res.end(html);});
await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
const reviewUrl='http://127.0.0.1:'+server.address().port+'/';
let browser;
try{
 browser=await chromium.launch({executablePath:process.env.HERO_ANIM_CHROMIUM??'/usr/bin/chromium',args:['--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist']});
 const page=await browser.newPage({viewport:{width:1100,height:820}});const errors=[],network=[];
 page.on('pageerror',e=>errors.push(String(e)));page.on('request',r=>{if(/^https?:/.test(r.url())&&r.url()!==reviewUrl)network.push(r.url());});
 await page.goto(reviewUrl);await page.waitForFunction(()=>window.outfitReady||window.outfitError,{},{timeout:120000});
 if(await page.evaluate(()=>window.outfitError))throw Error(await page.evaluate(()=>window.outfitError));
 const info=await page.evaluate(()=>({clips:window.outfitStudy.clips,allClips:window.outfitStudy.allClips}));
 const captures=[['base',{cap:false,glasses:false,backpack:false}],['cap',{cap:true,glasses:false,backpack:false}],['glasses',{cap:false,glasses:true,backpack:false}],['backpack-back',{cap:false,glasses:false,backpack:true,yaw:180}],['all-front',{cap:true,glasses:true,backpack:true}],['all-quarter',{cap:true,glasses:true,backpack:true,yaw:35}],['all-back',{cap:true,glasses:true,backpack:true,yaw:180}],['all-look',{cap:true,glasses:true,backpack:true,clip:'Study_Look',time:.7}]];
 for(const [name,o]of captures){const png=await page.evaluate(o=>window.outfitStudy.frame({...o,width:768,height:768}),o);fs.writeFileSync(path.join(review,name+'.png'),Buffer.from(png,'base64'));}
 await page.evaluate(()=>window.outfitStudy.frame({cap:true,glasses:true,backpack:true,clip:'Study_Look',time:.5,width:700,height:700}));
 await page.screenshot({path:path.join(review,'viewer.png'),fullPage:true});
 const states=[];
 for(let mask=0;mask<8;mask++){const options={cap:!!(mask&1),glasses:!!(mask&2),backpack:!!(mask&4),clip:'Study_Look',time:.7};await page.evaluate(o=>window.outfitStudy.frame(o),options);const s=await page.evaluate(()=>window.outfitStudy.snapshot());if(s.morph!==Number(options.cap)||Object.keys(s.visible).some(k=>s.visible[k]!==options[k]))throw Error('Toggle state mismatch');states.push(s);}
 // Test actual UI events, not just the test API.
 await page.locator('#cap').uncheck();await page.locator('#glasses').uncheck();await page.locator('#backpack').uncheck();
 const off=await page.evaluate(()=>window.outfitStudy.snapshot());if(off.morph!==0||Object.values(off.visible).some(Boolean))throw Error('UI off failed');
 await page.locator('#cap').check();await page.locator('#glasses').check();await page.locator('#backpack').check();
 const on=await page.evaluate(()=>window.outfitStudy.snapshot());if(on.morph!==1||Object.values(on.visible).some(x=>!x))throw Error('UI on failed');
 const motionChecks=[];
 for(const clip of info.clips){let samples=[];for(const t of [0,clip.span*.23,clip.span*.61]){await page.evaluate(o=>window.outfitStudy.frame(o),{clip:clip.name,time:t,cap:true,glasses:true,backpack:true});samples.push(await page.evaluate(()=>window.outfitStudy.snapshot()));}const d=(a,b)=>Math.hypot(...a.map((x,i)=>x-b[i]));const moved=Math.max(...samples.slice(1).map(s=>d(s.vertices.hero,samples[0].vertices.hero)));if(!(moved>1e-5))throw Error('Clip did not deform mesh: '+clip.name);motionChecks.push({clip:clip.name,sampledVertexMovement:moved,samples});}
 const frames=path.join(out,'review-frames');fs.mkdirSync(frames,{recursive:true});let index=0;
 for(const clip of info.clips){const count=Math.round(clip.span*24);for(let i=0;i<count;i++){const phase=index/(24*11);const png=await page.evaluate(o=>window.outfitStudy.frame(o),{cap:true,glasses:true,backpack:true,clip:clip.name,time:i/24,yaw:clip.name==='Study_Step'?145:clip.name==='Study_Look'?25:0});fs.writeFileSync(path.join(frames,String(++index).padStart(4,'0')+'.png'),Buffer.from(png,'base64'));}console.log(clip.name+': '+count+' actual GLB frames');}
 const run=(exe,args)=>{const p=spawnSync(exe,args,{encoding:'utf8'});if(p.status!==0)throw Error(p.stderr||p.stdout);return p.stdout;};
 run('ffmpeg',['-y','-v','error','-framerate','24','-start_number','1','-i',path.join(frames,'%04d.png'),'-an','-c:v','libx264','-preset','fast','-crf','19','-pix_fmt','yuv420p','-movflags','+faststart',path.join(review,'review.mp4')]);
 const probe=JSON.parse(run('ffprobe',['-v','error','-select_streams','v:0','-count_frames','-show_entries','stream=width,height,avg_frame_rate,nb_read_frames','-of','json',path.join(review,'review.mp4')]));
 if(Number(probe.streams[0].nb_read_frames)!==index)throw Error('Video frame count mismatch');
 if(errors.length||network.length)throw Error('Viewer errors/network requests: '+JSON.stringify({errors,network}));
 const report={renderer:'Three.js r160, actual exported GLB / Chromium SwiftShader',viewerValidation:'Unchanged self-contained HTML served as one loopback document; file navigation blocked by managed browser policy',glbSha256:sha(model),scriptSha256:sha(fs.readFileSync(fileURLToPath(import.meta.url))),viewerSha256:sha(fs.readFileSync(viewer)),externalNetworkRequests:network,errors,info,states,uiChecks:{off,on},motionChecks,frames:index,video:probe.streams[0]};
 fs.writeFileSync(path.join(review,'review-report.json'),JSON.stringify(report,null,2)+'\n');
 console.log('Verified 8 outfit combinations, UI controls, 4 moving clips and '+index+' video frames');
}finally{if(browser)await browser.close();server.close();}

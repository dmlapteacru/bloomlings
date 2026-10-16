// Actual exported GLB, actual skeletal clips; no changes to production art.
// node tools/heroanim/render-plant-study.mjs content/work/plant-rig
import fs from 'node:fs';
import path from 'node:path';
import http from 'node:http';
import { fileURLToPath } from 'node:url';
import { createHash } from 'node:crypto';
import { spawnSync } from 'node:child_process';
import { chromium } from 'playwright-core';

const here=path.dirname(fileURLToPath(import.meta.url));
const out=path.resolve(process.argv[2]??'content/work/plant-rig');
const model=path.join(out,'sprig-plant-rigged.glb');
const three=path.join(here,'node_modules/three');
const sha=b=>createHash('sha256').update(b).digest('hex');
const recipeSha=sha(fs.readFileSync(fileURLToPath(import.meta.url)));
if(!fs.existsSync(path.join(out,'roundtrip-report.json')))throw new Error('Run the Blender roundtrip check first');
const checked=JSON.parse(fs.readFileSync(path.join(out,'roundtrip-report.json')));
if(checked.glbSha256!==sha(fs.readFileSync(model)))throw new Error('GLB changed after roundtrip validation');

function encode(){
 const review=path.resolve(here,'../../output/plant-rig-study');fs.mkdirSync(review,{recursive:true});
 const font='/usr/share/fonts/truetype/open-sans/OpenSans-Semibold.ttf';
 const clips=[['idle','Ожидание'],['wave','Приветствие'],['walk','Ходьба на месте'],['joy','Радость']];
 const run=(exe,args)=>{const p=spawnSync(exe,args,{encoding:'utf8'});if(p.status!==0)throw new Error(p.stderr||p.stdout);return p.stdout;};
 let expectedTotal=0;const movies=[];
 for(const [clip,label] of clips){
  const folder=path.join(out,'frames',clip);
  const count=fs.readdirSync(folder).filter(f=>/^\d{4}\.png$/.test(f)).length;
  const movie=path.join(review,clip+'.mp4');const frames=count*(clip==='walk'?2:1);expectedTotal+=frames;
  const args=['-y','-v','error'];if(clip==='walk')args.push('-stream_loop','1');
  args.push('-framerate','24','-start_number','1','-i',path.join(folder,'%04d.png'),
   '-vf',"pad=iw:ih+64:0:0:color=0xdce2dd,drawtext=fontfile="+font+":text='"+label+"':fontcolor=0x394b40:fontsize=21:x=(w-text_w)/2:y=h-43",
   '-an','-c:v','libx264','-preset','fast','-crf','19','-pix_fmt','yuv420p','-movflags','+faststart',movie);
  run('ffmpeg',args);movies.push({clip,frames,bytes:fs.statSync(movie).size});
 }
 const list=path.join(review,'clips.txt');fs.writeFileSync(list,clips.map(([c])=>"file '"+c+".mp4'").join('\n')+'\n');
 const movie=path.join(review,'review.mp4');
 run('ffmpeg',['-y','-v','error','-f','concat','-safe','0','-i',list,'-c','copy','-movflags','+faststart',movie]);
 fs.unlinkSync(list);
 const probe=JSON.parse(run('ffprobe',['-v','error','-select_streams','v:0','-count_frames','-show_entries','stream=width,height,avg_frame_rate,nb_read_frames','-of','json',movie]));
 if(Number(probe.streams[0].nb_read_frames)!==expectedTotal)throw new Error('Movie frame count mismatch');
 fs.writeFileSync(path.join(review,'video-report.json'),JSON.stringify({sourceGlbSha256:checked.glbSha256,movies,review:probe.streams[0]},null,2)+'\n');
 console.log('Encoded '+expectedTotal+' review frames, '+(expectedTotal/24).toFixed(2)+' seconds');
}
if(process.argv.includes('--encode-only')){encode();process.exit(0);}

const html=`<!doctype html><meta charset="utf-8">
<script type="importmap">{"imports":{"three":"/three/build/three.module.js"}}</script>
<body style="margin:0"><script type="module">
import * as THREE from 'three';
import {GLTFLoader} from '/three/examples/jsm/loaders/GLTFLoader.js';
const renderer=new THREE.WebGLRenderer({antialias:true,preserveDrawingBuffer:true});
renderer.setPixelRatio(1);renderer.setSize(512,576);
renderer.setClearColor(0xdce2dd);renderer.outputColorSpace=THREE.SRGBColorSpace;
renderer.toneMapping=THREE.NoToneMapping;
document.body.appendChild(renderer.domElement);
const scene=new THREE.Scene();
scene.add(new THREE.HemisphereLight(0xffffff,0xd2dacc,2.0));
for(const [loc,power] of [[[ -3,4,4],1.6],[[3,2,3],.7],[[1,3,-3],.9]]){
 const l=new THREE.DirectionalLight(0xffffff,power);l.position.set(...loc);scene.add(l);
}
const gltf=await new GLTFLoader().loadAsync('/model.glb');
const obj=gltf.scene;scene.add(obj);
obj.traverse(o=>{if(o.isMesh)o.frustumCulled=false;});
const mixer=new THREE.AnimationMixer(obj);
const camera=new THREE.OrthographicCamera(-1.18,1.18,2.65/2,-2.65/2,.1,20);
camera.position.set(0,1.07,5);camera.lookAt(0,1.07,0);
const span=c=>{let start=Infinity,end=0;for(const t of c.tracks){start=Math.min(start,t.times[0]);end=Math.max(end,t.times[t.times.length-1]);}return{start,seconds:end-start};};
window.info={clips:gltf.animations.map(c=>({name:c.name,...span(c)})),meshes:[]};
obj.traverse(o=>{if(o.isSkinnedMesh)window.info.meshes.push({name:o.name,vertices:o.geometry.attributes.position.count,bones:o.skeleton.bones.length});});
window.renderFrame=({clip,time,yaw=0,width=512,height=576})=>{
 renderer.setSize(width,height,false);
 const half=2.65/2;camera.left=-half*width/height;camera.right=half*width/height;camera.top=half;camera.bottom=-half;camera.updateProjectionMatrix();
 obj.rotation.y=yaw*Math.PI/180;
 mixer.stopAllAction();
 const selected=gltf.animations.find(c=>c.name==='SprigStudy_'+clip);
 if(!selected)throw new Error('Missing clip '+clip);
 const action=mixer.clipAction(selected).reset().setLoop(THREE.LoopOnce,1);
 action.clampWhenFinished=true;action.play();mixer.setTime(span(selected).start+time);
 obj.updateMatrixWorld(true);obj.traverse(o=>{if(o.isSkinnedMesh)o.skeleton.update();});
 renderer.render(scene,camera);
 return renderer.domElement.toDataURL('image/png').split(',')[1];
};
window.ready=true;
</script>`;
const server=http.createServer((req,res)=>{
 const pathname=new URL(req.url,'http://localhost').pathname;
 if(pathname==='/'){res.setHeader('Content-Type','text/html');res.end(html);return;}
 let file;
 if(pathname==='/model.glb')file=model;
 else if(pathname.startsWith('/three/')){
  file=path.resolve(three,pathname.slice(7));
  if(!file.startsWith(three+path.sep)){res.writeHead(403);res.end();return;}
 }
 if(!file||!fs.existsSync(file)||!fs.statSync(file).isFile()){res.writeHead(404);res.end();return;}
 res.setHeader('Content-Type',file.endsWith('.js')?'text/javascript':'application/octet-stream');
 fs.createReadStream(file).pipe(res);
});
await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
let browser;
try{
 browser=await chromium.launch({executablePath:process.env.HERO_ANIM_CHROMIUM??'/usr/bin/chromium',
  args:['--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist']});
 const page=await browser.newPage({viewport:{width:768,height:768}});
 page.on('pageerror',e=>console.error(e));
 await page.goto('http://127.0.0.1:'+server.address().port);
 await page.waitForFunction(()=>window.ready,{},{timeout:120000});
 const info=await page.evaluate(()=>window.info);
 for(const clip of ['Idle','Wave','Walk','Joy']){
  const c=info.clips.find(c=>c.name==='SprigStudy_'+clip);if(!c)throw new Error('Missing '+clip);
  const folder=path.join(out,'frames',clip.toLowerCase());fs.mkdirSync(folder,{recursive:true});
  const count=Math.round(c.seconds*24);
  for(const file of fs.readdirSync(folder))if(/^\d{4}\.png$/.test(file)&&Number(file.slice(0,4))>count)fs.unlinkSync(path.join(folder,file));
  for(let i=0;i<count;i++){
   const png=await page.evaluate(o=>window.renderFrame(o),{clip,time:i/24,yaw:clip==='Walk'?35:0});
   fs.writeFileSync(path.join(folder,String(i+1).padStart(4,'0')+'.png'),Buffer.from(png,'base64'));
  }
  console.log(clip+': '+count+' actual GLB frames');
 }
 for(const [name,clip,time,yaw] of [['idle','Idle',0,0],['wave','Wave',35/24,0],['walk','Walk',12/24,0],['joy','Joy',20/24,0],['wave-three-quarter','Wave',35/24,35],['wave-back','Wave',35/24,180]]){
  const png=await page.evaluate(o=>window.renderFrame(o),{clip,time,yaw,width:768,height:768});
  fs.writeFileSync(path.join(out,name+'.png'),Buffer.from(png,'base64'));
 }
 const report={renderer:'Three.js r160 / Chromium SwiftShader',fps:24,cell:[512,576],glbSha256:checked.glbSha256,
  scriptSha256:recipeSha,...info};
 fs.writeFileSync(path.join(out,'render-report.json'),JSON.stringify(report,null,2)+'\n');
 console.log(JSON.stringify(report));
}finally{if(browser)await browser.close();server.close();}
encode();

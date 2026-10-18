// Actual skinned before/after renders. Models and WebGL stay in offline authoring tools.
import fs from 'node:fs';
import path from 'node:path';
import http from 'node:http';
import {fileURLToPath} from 'node:url';
import {chromium} from 'playwright-core';

const here=path.dirname(fileURLToPath(import.meta.url));
const repo=path.resolve(here,'../../..');
const family=process.argv[2]||'twig';
if(!['twig','sprig','bloom','drop'].includes(family))throw Error('Unknown family');
const root=path.join(repo,'content/work/rigging',family);
const three=path.resolve(here,'../node_modules/three');
const out=path.join(root,'review');fs.mkdirSync(out,{recursive:true});
const title=family[0].toUpperCase()+family.slice(1);
const partKeys=JSON.parse(fs.readFileSync(path.join(root,'assembly.json'))).parts.map(p=>p.key);
const video=process.argv.includes('--video');
const quick=process.argv.includes('--quick');
const stress=process.argv.includes('--stress');
const html=`<script type="importmap">{"imports":{"three":"/three/build/three.module.js"}}</script><script type="module">
import * as T from 'three';
import {GLTFLoader} from '/three/examples/jsm/loaders/GLTFLoader.js';
const r=new T.WebGLRenderer({antialias:true,alpha:false,preserveDrawingBuffer:true});
r.outputColorSpace=T.SRGBColorSpace;r.toneMapping=T.NoToneMapping;r.setSize(600,720);r.setClearColor(0xf3ead9);
const scene=new T.Scene();scene.add(new T.HemisphereLight(0xffffff,0xffffff,1.7));
for(const [p,i] of [[[-3,4,5],1.1],[[3,2,4],.5]]){let l=new T.DirectionalLight(0xffffff,i);l.position.set(...p);scene.add(l);}
let model,mixer,clips,parts;
window.load=async (version)=>{
 if(model){scene.remove(model);mixer.stopAllAction();model.traverse(o=>{if(o.geometry)o.geometry.dispose();});}
 const g=await new GLTFLoader().loadAsync('/'+version+'.glb');model=g.scene;scene.add(model);mixer=new T.AnimationMixer(model);clips=g.animations;
 parts=Object.fromEntries(${JSON.stringify(partKeys)}.map(key=>[key,model.getObjectByName('${title}HeroV2_'+key)]));
 if(Object.values(parts).some(o=>!o))throw Error('Missing part root');model.traverse(o=>{
  if(o.isMesh){const multi=Array.isArray(o.material);const mats=(multi?o.material:[o.material]).map(m=>new T.MeshLambertMaterial({map:m.map,color:m.color.clone().multiplyScalar(.25),emissiveMap:m.map,emissive:m.color.clone().multiplyScalar(.75),side:T.DoubleSide,transparent:m.opacity<1,opacity:m.opacity}));o.material=multi?mats:mats[0];o.frustumCulled=false;}});
 return {parts:Object.keys(parts),clips:clips.map(a=>({name:a.name,duration:a.duration}))};
};
const cam=new T.OrthographicCamera(-.64,.64,.768,-.768,.01,30);
window.resize=(w,h)=>r.setSize(w,h);
window.frame=({body='Jacket',head='',scarf='none',glasses=true,clip='Wave',time=1.5,yaw=15,weights=false}={})=>{
 const stock={twig:'PumpkinHat',sprig:'HairLeafy',bloom:'HairPetals',drop:'NativeDrop'}['${family}'];head=head||stock;
 for(const [key,o] of Object.entries(parts))o.visible=key==='base'||key.startsWith('Base_')&&key!=='Base_Head';
 if(parts.Base_Head)parts.Base_Head.visible=head!=='NativeDrop';
 for(const key of [head,body,scarf,glasses?'Glasses':'none'])if(parts[key])parts[key].visible=true;
 for(const [key,o]of Object.entries(parts))if(key.startsWith('Scarf'))o.traverse(o=>{if(o.morphTargetInfluences){o.morphTargetInfluences.fill(0);const i=o.morphTargetDictionary['Over_'+body];if(i!==undefined)o.morphTargetInfluences[i]=1;}});
 mixer.stopAllAction();if(clip!=='Rest'){const c=clips.find(c=>c.name==='${title}_'+clip||c.name.endsWith('${title}_'+clip));if(!c)throw Error('Missing clip '+clip);mixer.clipAction(c).reset().setLoop(T.LoopOnce,1).play();mixer.setTime(time);}
 const a=(yaw+${family==='drop'?90:0})*Math.PI/180;cam.position.set(Math.sin(a)*5,.64,Math.cos(a)*5);cam.lookAt(0,.64,0);cam.updateProjectionMatrix();model.updateMatrixWorld(true);r.render(scene,cam);return r.domElement.toDataURL().split(',')[1];
};window.ready=true;
</script>`;
const server=http.createServer((q,s)=>{const url=new URL(q.url,'http://local').pathname;if(url==='/'){s.end(html);return;}
 const file=['/before.glb','/after.glb'].includes(url)?path.join(root,url.slice(1)):url.startsWith('/three/')?path.resolve(three,url.slice(7)):null;
 if(!file||!file.startsWith(root+'/')&&!file.startsWith(three+'/')||!fs.existsSync(file)){s.writeHead(404);s.end();return;}
 s.setHeader('Content-Type',file.endsWith('.js')?'text/javascript':'application/octet-stream');fs.createReadStream(file).pipe(s);});
await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));let browser;
try{
 browser=await chromium.launch({executablePath:'/usr/bin/chromium',args:['--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist']});
 const page=await browser.newPage();page.on('pageerror',e=>console.error(e.message));await page.goto('http://127.0.0.1:'+server.address().port);await page.waitForFunction(()=>window.ready,{},{timeout:120000});
 const captures=[];let modelInfo;
 for(const version of video||process.argv.includes('--after-only')?['after']:['before','after']){
  modelInfo=await page.evaluate(v=>window.load(v),version);console.log('Loaded',family,version,modelInfo.parts.length,'part roots');
  if(video){
   await page.evaluate(()=>window.resize(360,432));const folder=path.join(root,'video');fs.mkdirSync(folder,{recursive:true});let index=0;const sequences=[];
   for(const [clip,seconds,body]of [['Idle',4,'Jacket'],['Walk',40/24,'Overalls'],['Wave',77/24,'Jacket'],['Joy',2.5,'Coat'],['Sad',91/24,'Coat'],['Curiosity',82/24,'Overalls']]){
    const start=index;for(let frame=0;frame<seconds*12;frame++){
     const file=String(index++).padStart(5,'0')+'.png';const options={clip,time:frame/12,body,yaw:15};
     fs.writeFileSync(path.join(folder,file),Buffer.from(await page.evaluate(o=>window.frame(o),options),'base64'));
    }sequences.push({clip,body,start,end:index-1});console.log(family,clip,'captured');
   }fs.writeFileSync(path.join(folder,'sequences.json'),JSON.stringify(sequences,null,2)+'\n');
  }else{
   for(const body of (stress?['Jacket']:['none','Jacket','Coat','Overalls']))for(const [clip,time]of (stress?[['QA_ArmLift',1]]:quick?[['Rest',0],['QA_ArmLift',1],['Wave',1.6]]:[['Rest',0],['QA_ArmLift',1],['QA_Forward',1],['Walk',.35],['Wave',1.6],['Joy',.8],['Sad',1.8]]))for(const yaw of (quick||stress?[0,45]:[0,45,180])){
    const options={clip,time,body,yaw};const file=version+'-'+body+'-'+clip+'-'+yaw+'.png';
    fs.writeFileSync(path.join(out,file),Buffer.from(await page.evaluate(o=>window.frame(o),options),'base64'));captures.push({file,...options,version});if(captures.length%21===0)console.log('Captured',captures.length,'poses');
   }
  }
 }
 fs.writeFileSync(path.join(out,video?'video.json':'review.json'),JSON.stringify({family,modelInfo,captures},null,2)+'\n');console.log('Rendered',family,captures.length,'actual rig frames');
}finally{if(browser)await browser.close();server.close();}

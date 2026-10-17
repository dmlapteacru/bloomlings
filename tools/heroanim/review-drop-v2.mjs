// Render the actual common-space Drop assembly / skinned animation export, never synthesized illustrations.
import fs from 'node:fs';import crypto from 'node:crypto';import path from 'node:path';import http from 'node:http';import{fileURLToPath}from'node:url';
import{chromium}from'playwright-core';
const here=path.dirname(fileURLToPath(import.meta.url)),root=path.resolve(here,'../../content/work/drop-v2'),three=path.join(here,'node_modules/three');
const animated=process.argv.includes('--animated'),model=path.join(root,animated?'DropHeroV2.glb':'assembly.glb');
const out=path.join(root,animated?'motion-review':'fit-review');fs.mkdirSync(out,{recursive:true});
const html=`<script type="importmap">{"imports":{"three":"/three/build/three.module.js"}}</script><script type="module">
import * as T from 'three';import{GLTFLoader}from'/three/examples/jsm/loaders/GLTFLoader.js';
const r=new T.WebGLRenderer({antialias:true,alpha:false,preserveDrawingBuffer:true});r.outputColorSpace=T.SRGBColorSpace;r.setClearColor(0xf4f0e6);r.setSize(800,1050);r.toneMapping=T.NoToneMapping;
const scene=new T.Scene();scene.add(new T.HemisphereLight(0xffffff,0xffffff,2));for(const[p,k]of[[[-3,4,5],1],[[3,2,4],.4]]){const l=new T.DirectionalLight(0xffffff,k);l.position.set(...p);scene.add(l);}
const g=await new GLTFLoader().loadAsync('/model.glb');scene.add(g.scene);const parts={};
for(const key of ['Base_Body','Base_Neck','Base_Head','Base_Arm_L','Base_Arm_R','Base_Leg_L','Base_Leg_R','Base_Tail','NativeDrop','HairSideWave','HairTwinRipples','HairWhirlpool','BucketHat','Cap','Beret','EarmuffHair','XmasHat','Glasses','ScarfKnit','ScarfStripe','Jacket','Coat','Overalls']){parts[key]=g.scene.getObjectByName('DropHeroV2_'+key);if(!parts[key])throw Error('Missing '+key);}
g.scene.traverse(o=>{if(o.isMesh){const multi=Array.isArray(o.material),mats=(multi?o.material:[o.material]).map(m=>new T.MeshLambertMaterial({map:m.map,color:m.color.clone().multiplyScalar(.25),emissiveMap:m.map,emissive:m.color.clone().multiplyScalar(.75),side:T.DoubleSide,transparent:m.opacity<1,opacity:m.opacity}));o.material=multi?mats:mats[0];o.frustumCulled=false;}});
const mixer=new T.AnimationMixer(g.scene);const camera=new T.OrthographicCamera(-.66,.66,.86625,-.86625,.01,30);
window.resize=(w,h)=>r.setSize(w,h);window.clips=g.animations.map(a=>({name:a.name,duration:a.duration}));
window.frame=({head='NativeDrop',body='none',scarf='none',glasses=false,clip='',time=0,yaw=90}={})=>{
 for(const[key,o]of Object.entries(parts))o.visible=key.startsWith('Base_')&&key!=='Base_Head';
 parts.Base_Head.visible=head!=='NativeDrop';parts[head].visible=true;if(body!=='none')parts[body].visible=true;if(scarf!=='none')parts[scarf].visible=true;parts.Glasses.visible=glasses;
 for(const name of ['ScarfKnit','ScarfStripe'])parts[name].traverse(o=>{if(o.isMesh&&o.morphTargetInfluences){o.morphTargetInfluences.fill(0);const i=o.morphTargetDictionary['Over_'+body];if(i!==undefined)o.morphTargetInfluences[i]=1;}});
 mixer.stopAllAction();if(clip){const c=g.animations.find(c=>c.name.includes('Drop_'+clip));if(!c)throw Error('Missing clip '+clip);mixer.clipAction(c).reset().play();mixer.setTime(time);}
 const a=yaw*Math.PI/180;camera.position.set(Math.sin(a)*5,.62,Math.cos(a)*5);camera.lookAt(0,.62,0);camera.updateProjectionMatrix();g.scene.updateMatrixWorld(true);r.render(scene,camera);return r.domElement.toDataURL().split(',')[1];
};window.ready=true;</script>`;
const server=http.createServer((q,s)=>{const u=new URL(q.url,'http://x').pathname;if(u==='/'){s.end(html);return;}const f=u==='/model.glb'?model:u.startsWith('/three/')?path.resolve(three,u.slice(7)):null;if(!f||f!==model&&!f.startsWith(three+'/')||!fs.existsSync(f)){s.writeHead(404);s.end();return;}s.setHeader('Content-Type',f.endsWith('.js')?'text/javascript':'application/octet-stream');fs.createReadStream(f).pipe(s);});
await new Promise(r=>server.listen(0,'127.0.0.1',r));let browser;
try{browser=await chromium.launch({executablePath:'/usr/bin/chromium',args:['--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist']});const p=await browser.newPage();p.on('pageerror',e=>{throw e;});await p.goto('http://127.0.0.1:'+server.address().port);await p.waitForFunction(()=>window.ready,{},{timeout:120000});
 const heads=['NativeDrop','HairSideWave','HairTwinRipples','HairWhirlpool','BucketHat','Cap','Beret','EarmuffHair','XmasHat'];
 const recipes=(process.argv.includes('--motion-only')||process.argv.includes('--video-only'))?[]:[...heads.map(head=>({head,glasses:true,key:head})),...['Jacket','Coat','Overalls'].flatMap(body=>['none','ScarfKnit','ScarfStripe'].map(scarf=>({head:'NativeDrop',body,scarf,glasses:true,key:body+'-'+scarf})))];
 const captures=[];for(const recipe of recipes)for(const yaw of[90,125]){const bytes=Buffer.from(await p.evaluate(o=>window.frame(o),{...recipe,yaw}),'base64'),file=recipe.key+'-'+yaw+'.png';fs.writeFileSync(path.join(out,file),bytes);captures.push({file,...recipe,yaw});}
 if(animated&&!process.argv.includes('--video-only')){for(const clip of ['Idle','Walk','Wave','Joy','Sad'])for(const body of ['Jacket','Coat','Overalls'])for(const phase of [.2,.45,.7]){const options={head:'NativeDrop',body,scarf:'ScarfKnit',glasses:true,clip,time:phase*({Walk:2,Wave:4,Sad:4}[clip]||3),yaw:90};const file=clip+'-'+body+'-'+phase+'.png';fs.writeFileSync(path.join(out,file),Buffer.from(await p.evaluate(o=>window.frame(o),options),'base64'));captures.push({file,...options});}}
 if(process.argv.includes('--video-only')){
  await p.evaluate(()=>window.resize(400,525));let index=0;const sequences=[];const folder=path.join(root,'video-frames');fs.mkdirSync(folder,{recursive:true});
  for(const[clip,seconds,body]of[['Idle',3,'Jacket'],['Walk',2,'Overalls'],['Wave',4,'Jacket'],['Joy',3,'Coat'],['Sad',4,'Overalls']]){
   const start=index;for(let i=0;i<seconds*10;i++){const file=String(index++).padStart(4,'0')+'.png';fs.writeFileSync(path.join(folder,file),Buffer.from(await p.evaluate(o=>window.frame(o),{head:'NativeDrop',body,scarf:'none',glasses:true,clip,time:i/10,yaw:105}),'base64'));}
   sequences.push({clip,start,end:index-1,body});console.log('Video',clip,'captured');
  }fs.writeFileSync(path.join(folder,'sequences.json'),JSON.stringify(sequences,null,2));
 }
 fs.writeFileSync(path.join(out,process.argv.includes('--video-only')?'video-review.json':'review.json'),JSON.stringify({modelSha256:crypto.createHash('sha256').update(fs.readFileSync(model)).digest('hex'),clips:await p.evaluate(()=>window.clips),captures},null,2));console.log('Rendered',captures.length,'actual Drop frames');
}finally{if(browser)await browser.close();server.close();}

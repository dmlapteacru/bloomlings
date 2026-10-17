import fs from 'node:fs';import http from 'node:http';import path from 'node:path';
import {chromium} from 'playwright-core';import {fileURLToPath} from 'node:url';
const here=path.dirname(fileURLToPath(import.meta.url)),root=path.resolve(here,'../../content/work/drop-clothing-fit'),three=path.join(here,'node_modules/three');
const html=`<script type="importmap">{"imports":{"three":"/three/build/three.module.js"}}</script><script type="module">
import * as T from 'three';import{GLTFLoader}from'/three/examples/jsm/loaders/GLTFLoader.js';
const r=new T.WebGLRenderer({antialias:true,alpha:false,preserveDrawingBuffer:true});r.outputColorSpace=T.SRGBColorSpace;r.setClearColor(0xf4f0e6);r.setSize(850,1000);r.toneMapping=T.NoToneMapping;
const scene=new T.Scene();scene.add(new T.HemisphereLight(0xffffff,0xffffff,2));for(const [p,k]of[[[-3,4,5],1],[[3,2,4],.4]]){const l=new T.DirectionalLight(0xffffff,k);l.position.set(...p);scene.add(l);}
const camera=new T.OrthographicCamera(-.56,.56,.66,-.66,.01,30);let current;
window.frame=async(file,yaw=0)=>{
 if(current)scene.remove(current);
 const g=await new GLTFLoader().loadAsync('/'+file);const b=new T.Box3().setFromObject(g.scene),s=b.getSize(new T.Vector3()),c=b.getCenter(new T.Vector3());
 current=new T.Group();current.add(g.scene);g.scene.position.sub(c);current.scale.setScalar(1/Math.max(s.y,Math.max(s.x,s.z)/.85));current.position.y=.5;scene.add(current);
 g.scene.traverse(o=>{if(o.isMesh){const multi=Array.isArray(o.material);const mats=(multi?o.material:[o.material]).map(m=>new T.MeshLambertMaterial({map:m.map,color:m.color.clone().multiplyScalar(.25),emissiveMap:m.map,emissive:m.color.clone().multiplyScalar(.75),side:T.DoubleSide}));o.material=multi?mats:mats[0];}});
 const a=yaw*Math.PI/180;camera.position.set(Math.sin(a)*5,.5,Math.cos(a)*5);camera.lookAt(0,.5,0);camera.updateProjectionMatrix();r.render(scene,camera);return r.domElement.toDataURL().split(',')[1];
};window.ready=true;</script>`;
const server=http.createServer((q,s)=>{const u=new URL(q.url,'http://x').pathname;if(u==='/'){s.end(html);return;}const f=u.startsWith('/three/')?path.resolve(three,u.slice(7)):path.resolve(root,u.slice(1));if(!f.startsWith(root+'/')&&!f.startsWith(three+'/')||!fs.existsSync(f)){s.writeHead(404);s.end();return;}s.setHeader('Content-Type',f.endsWith('.js')?'text/javascript':'application/octet-stream');fs.createReadStream(f).pipe(s);});
await new Promise(r=>server.listen(0,'127.0.0.1',r));let browser;
try{browser=await chromium.launch({executablePath:'/usr/bin/chromium',args:['--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist']});const p=await browser.newPage();p.on('pageerror',e=>{throw e;});await p.goto('http://127.0.0.1:'+server.address().port);await p.waitForFunction(()=>window.ready);for(const file of process.argv.slice(2).length?process.argv.slice(2):['a.glb','unfitted.glb','fitted.glb','garment.glb'])for(const yaw of [90,125,180,270]){const image=await p.evaluate(([file,yaw])=>window.frame(file,yaw),[file,yaw]);fs.writeFileSync(path.join(root,file.replace('.glb','')+'-'+yaw+'.png'),Buffer.from(image,'base64'));console.log(file,yaw);}}
finally{if(browser)await browser.close();server.close();}

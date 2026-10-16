// First in-game modular Sprig: six depth-masked layers and three shop icons, not outfit combinations.
import fs from 'node:fs';import path from 'node:path';import http from 'node:http';
import {fileURLToPath} from 'node:url';import {createHash} from 'node:crypto';
import {chromium} from 'playwright-core';import {PNG} from 'pngjs';import {palette,indexed,writePng8} from './png8.mjs';
const here=path.dirname(fileURLToPath(import.meta.url)),repo=path.resolve(here,'../..');
const model=path.join(here,'models/sprig-outfit-study.glb'),three=path.join(here,'node_modules/three');
const dir=path.join(repo,'client/Assets/Bloomlings/Art/Heroes/Resources/HeroMotion');
const dataFile=path.join(repo,'client/Assets/Bloomlings/UI/Design/SprigGearData.cs');
const hash=b=>createHash('sha256').update(b).digest('hex');
const html=`<!doctype html><script type="importmap">{"imports":{"three":"/three/build/three.module.js"}}</script><script type="module">
import * as T from 'three';import {GLTFLoader} from '/three/examples/jsm/loaders/GLTFLoader.js';
const renderer=new T.WebGLRenderer({antialias:true,alpha:true,preserveDrawingBuffer:true});renderer.setPixelRatio(1);renderer.setClearColor(0,0);renderer.outputColorSpace=T.SRGBColorSpace;renderer.toneMapping=T.NoToneMapping;
const scene=new T.Scene();scene.add(new T.HemisphereLight(0xffffff,0xc7d4bc,2));for(const [p,k]of [[[-3,4,4],1.4],[[3,2,4],.8],[[1,3,-3],1]]){const l=new T.DirectionalLight(0xffffff,k);l.position.set(...p);scene.add(l);}
const gltf=await new GLTFLoader().loadAsync('/model.glb');scene.add(gltf.scene);
const hero=gltf.scene.getObjectByName('SprigHero'),cap=gltf.scene.getObjectByName('SprigHero_Cap'),glasses=gltf.scene.getObjectByName('SprigHero_Glasses'),bag=gltf.scene.getObjectByName('SprigHero_Backpack');
const parts={hero,cap,glasses,bag},original=new Map();gltf.scene.traverse(o=>{if(o.isMesh){o.frustumCulled=false;original.set(o,o.material);}});
const depth=new T.MeshBasicMaterial({colorWrite:false,depthWrite:true,side:T.DoubleSide});
const camera=new T.OrthographicCamera(-1.14,1.14,1.275,-1.275,.1,30);camera.position.set(0,.995,5);camera.lookAt(0,.995,0);
const mixer=new T.AnimationMixer(gltf.scene);mixer.clipAction(gltf.animations.find(c=>c.name==='Study_Idle')).play();mixer.setTime(1/24);
window.render=({layer,hat=false,icon=false})=>{
 gltf.scene.rotation.y=0;
 for(const [k,o]of Object.entries(parts)){o.visible=true;o.traverse(m=>{if(m.isMesh){m.material=original.get(m);m.renderOrder=0;}});}
 hero.morphTargetInfluences[hero.morphTargetDictionary.CapOn]=hat?1:0;
 cap.visible=hat;glasses.visible=layer==='glasses';bag.visible=layer==='backpack';
 if(layer==='base'){glasses.visible=false;bag.visible=false;}
 else if(!icon){for(const o of [hero,...(hat?[cap]:[])])o.traverse(m=>{if(m.isMesh){m.material=depth;m.renderOrder=-10;}});}
 if(icon){hero.visible=false;cap.visible=layer==='hat';glasses.visible=layer==='glasses';bag.visible=layer==='backpack';if(layer==='backpack')gltf.scene.rotation.y=Math.PI;}
 gltf.scene.updateMatrixWorld(true);gltf.scene.traverse(o=>{if(o.isSkinnedMesh)o.skeleton.update();});
 if(icon){const object=layer==='hat'?cap:layer==='glasses'?glasses:bag;const box=new T.Box3().setFromObject(object,true),center=box.getCenter(new T.Vector3()),size=box.getSize(new T.Vector3());const half=Math.max(size.y,size.x)*.68;camera.left=-half;camera.right=half;camera.top=half;camera.bottom=-half;camera.position.set(center.x+1.8,center.y+1.,center.z+5);camera.lookAt(center);renderer.setSize(320,320);}
 else{camera.left=-1.275*448/504;camera.right=1.275*448/504;camera.top=1.275;camera.bottom=-1.275;camera.position.set(0,1.02,5);camera.lookAt(0,1.02,0);renderer.setSize(448,504);}
 camera.updateProjectionMatrix();renderer.render(scene,camera);return renderer.domElement.toDataURL('image/png').split(',')[1];
};window.ready=true;</script>`;
const server=http.createServer((req,res)=>{const u=new URL(req.url,'http://localhost').pathname;if(u==='/'){res.setHeader('Content-Type','text/html');res.end(html);return;}const f=u==='/model.glb'?model:u.startsWith('/three/')?path.resolve(three,u.slice(7)):null;if(!f||(!f.startsWith(three+path.sep)&&f!==model)||!fs.existsSync(f)){res.writeHead(404);res.end();return;}res.setHeader('Content-Type',f.endsWith('.js')?'text/javascript':'application/octet-stream');fs.createReadStream(f).pipe(res);});
await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));let browser;
const recipes=[['base','base',false,false],['cap','base',true,false],['glasses','glasses',false,false],['cap-glasses','glasses',true,false],['backpack','backpack',false,false],['cap-backpack','backpack',true,false],['icon-hat','hat',false,true],['icon-glasses','glasses',false,true],['icon-backpack','backpack',false,true]];
const images=[];
try{browser=await chromium.launch({executablePath:process.env.HERO_ANIM_CHROMIUM??'/usr/bin/chromium',args:['--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist']});const page=await browser.newPage();page.on('pageerror',e=>{throw e;});await page.goto('http://127.0.0.1:'+server.address().port);await page.waitForFunction(()=>window.ready,{},{timeout:120000});
 for(const [name,layer,hat,icon]of recipes){const b64=await page.evaluate(o=>window.render(o),{layer,hat,icon});images.push({name,png:PNG.sync.read(Buffer.from(b64,'base64')),icon});}
}finally{if(browser)await browser.close();server.close();}
const pal=palette(images.map(x=>x.png));const outputs=[];
for(const {name,png,icon}of images){let l=png.width,t=png.height,r=-1,b=-1;for(let y=0;y<png.height;y++)for(let x=0;x<png.width;x++)if(png.data[(y*png.width+x)*4+3]>2){l=Math.min(l,x);t=Math.min(t,y);r=Math.max(r,x);b=Math.max(b,y);}if(r<l)throw Error('Empty '+name);l=Math.max(0,l-1);t=Math.max(0,t-1);r=Math.min(png.width-1,r+1);b=Math.min(png.height-1,b+1);const w=r-l+1,h=b-t+1,full=indexed(png,pal,{dither:.35}),idx=new Uint8Array(w*h);for(let y=0;y<h;y++)idx.set(full.subarray((t+y)*png.width+l,(t+y)*png.width+l+w),y*w);const bytes=writePng8(w,h,idx,pal),file='sprigmod-'+name+'.png';fs.writeFileSync(path.join(dir,file),bytes);outputs.push({key:name,file,crop:[l,t,w,h],icon,bytes:bytes.length,sha256:hash(bytes)});}
let cs='// Generated by tools/heroanim/bake-sprig-gear.mjs.\nnamespace Bloomlings.Client.UI.Design\n{\n    public static partial class SprigGearArt\n    {\n        private static readonly HeroFrame[] Frames = new[]\n        {\n';
for(const f of outputs)cs+='            new HeroFrame("'+f.file.slice(0,-4)+'", '+f.crop.join(', ')+', .5f, .68f, .5f, .35f),\n';
cs+='        };\n    }\n}\n';fs.writeFileSync(dataFile,cs);
const manifest={model:'models/sprig-outfit-study.glb',modelSha256:hash(fs.readFileSync(model)),data:path.relative(repo,dataFile),dataSha256:hash(Buffer.from(cs)),cell:[448,504],outputs};fs.writeFileSync(path.join(here,'sprig-gear.json'),JSON.stringify(manifest,null,2)+'\n');
const folder=JSON.parse(fs.readFileSync(path.join(dir,'manifest.json')));folder.files=folder.files.filter(x=>!x.path.startsWith('sprigmod-'));for(const f of outputs)folder.files.push({path:f.file,width:f.crop[2],height:f.crop[3],sha256:f.sha256,slot:'char.hero3d.motion.sprig',source:'owner',record:'tools/heroanim/SOURCE.md'});fs.writeFileSync(path.join(dir,'manifest.json'),JSON.stringify(folder,null,1)+'\n');
console.log('Baked '+outputs.length+' Sprig layers/icons, '+outputs.reduce((s,x)=>s+x.bytes,0)+' bytes');

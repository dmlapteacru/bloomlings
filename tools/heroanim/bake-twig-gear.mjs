// Five small runtime images from the owner's Twig geometry, not a runtime 3D model.
// First run: blender -b --factory-startup -t 2 --python-exit-code 1 --python tools/heroanim/prepare-twig-gear.py
// Then: node tools/heroanim/bake-twig-gear.mjs
import fs from 'node:fs';
import path from 'node:path';
import http from 'node:http';
import {fileURLToPath} from 'node:url';
import {createHash} from 'node:crypto';
import {chromium} from 'playwright-core';
import {PNG} from 'pngjs';
import {palette,indexed,writePng8} from './png8.mjs';
const here=path.dirname(fileURLToPath(import.meta.url)),repo=path.resolve(here,'../..');
const buildDir=path.join(repo,'content/work/twig-gear');
const model=path.join(buildDir,'TwigHero.glb'),report=JSON.parse(fs.readFileSync(path.join(buildDir,'build-report.json')));
const three=path.join(here,'node_modules/three');
const dir=path.join(repo,'client/Assets/Bloomlings/Art/Heroes/Resources/HeroMotion');
const dataFile=path.join(repo,'client/Assets/Bloomlings/UI/Design/TwigGearData.cs');
const sha=b=>createHash('sha256').update(b).digest('hex');
if(sha(fs.readFileSync(model))!==report.derivedGlbSha256)throw Error('Derived Twig model changed; prepare it again');
const html=`<!doctype html><script type="importmap">{"imports":{"three":"/three/build/three.module.js"}}</script><script type="module">
import * as T from 'three';import {GLTFLoader} from '/three/examples/jsm/loaders/GLTFLoader.js';
const renderer=new T.WebGLRenderer({antialias:true,alpha:true,preserveDrawingBuffer:true});renderer.setPixelRatio(1);renderer.setClearColor(0,0);renderer.outputColorSpace=T.SRGBColorSpace;renderer.toneMapping=T.NoToneMapping;
const scene=new T.Scene();scene.add(new T.HemisphereLight(0xffffff,0xc7d4bc,2));for(const [p,k]of [[[-3,4,4],1.4],[[3,2,4],.8],[[1,3,-3],1]]){const l=new T.DirectionalLight(0xffffff,k);l.position.set(...p);scene.add(l);}
const gltf=await new GLTFLoader().loadAsync('/model.glb');scene.add(gltf.scene);
const hero=gltf.scene.getObjectByName('TwigHero'),acorn=gltf.scene.getObjectByName('TwigHero_AcornHat'),cap=gltf.scene.getObjectByName('TwigHero_FlowerCap');
gltf.scene.traverse(o=>{if(o.isMesh)o.frustumCulled=false;});
const camera=new T.OrthographicCamera(-1,1,1,-1,.1,30);
const mixer=new T.AnimationMixer(gltf.scene);mixer.clipAction(gltf.animations.find(c=>c.name.includes('Preview'))).play();mixer.setTime(.4);
window.render=({state,icon=false})=>{
 hero.visible=!icon;acorn.visible=state==='acorn';cap.visible=state==='cap';
 gltf.scene.updateMatrixWorld(true);gltf.scene.traverse(o=>{if(o.isSkinnedMesh)o.skeleton.update();});
 if(icon){const item=state==='acorn'?acorn:cap,box=new T.Box3().setFromObject(item,true),center=box.getCenter(new T.Vector3()),size=box.getSize(new T.Vector3()),half=Math.max(size.y,size.x)*.7;
 camera.left=-half;camera.right=half;camera.top=half;camera.bottom=-half;camera.position.set(center.x+1.3,center.y+.8,center.z+5);camera.lookAt(center);renderer.setSize(320,320);}
 else{const half=1.065;camera.left=-half*448/504;camera.right=half*448/504;camera.top=half;camera.bottom=-half;camera.position.set(0,.852,5);camera.lookAt(0,.852,0);renderer.setSize(448,504);}
 camera.updateProjectionMatrix();renderer.render(scene,camera);return renderer.domElement.toDataURL('image/png').split(',')[1];
};window.ready=true;</script>`;
const server=http.createServer((req,res)=>{
 const u=new URL(req.url,'http://localhost').pathname;
 if(u==='/'){res.setHeader('Content-Type','text/html');res.end(html);return;}
 const f=u==='/model.glb'?model:u.startsWith('/three/')?path.resolve(three,u.slice(7)):null;
 if(!f||(!f.startsWith(three+path.sep)&&f!==model)||!fs.existsSync(f)){res.writeHead(404);res.end();return;}
 res.setHeader('Content-Type',f.endsWith('.js')?'text/javascript':'application/octet-stream');fs.createReadStream(f).pipe(res);
});
await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));let browser;
const recipes=[['base','base',false],['acorn','acorn',false],['cap','cap',false],['icon-acorn','acorn',true],['icon-cap','cap',true]];
const images=[];
try{
 browser=await chromium.launch({executablePath:process.env.HERO_ANIM_CHROMIUM??'/usr/bin/chromium',args:['--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist']});
 const page=await browser.newPage();page.on('pageerror',e=>{throw e;});await page.goto('http://127.0.0.1:'+server.address().port);await page.waitForFunction(()=>window.ready,{},{timeout:120000});
 for(const [name,state,icon]of recipes){const b64=await page.evaluate(o=>window.render(o),{state,icon});images.push({name,png:PNG.sync.read(Buffer.from(b64,'base64')),icon});}
}finally{if(browser)await browser.close();server.close();}
const pal=palette(images.map(x=>x.png)),outputs=[];
for(const {name,png,icon}of images){
 let l=png.width,t=png.height,r=-1,b=-1;
 for(let y=0;y<png.height;y++)for(let x=0;x<png.width;x++)if(png.data[(y*png.width+x)*4+3]>2){l=Math.min(l,x);t=Math.min(t,y);r=Math.max(r,x);b=Math.max(b,y);}
 if(r<l)throw Error('Empty '+name);if(!icon&&(l===0||t===0||r===png.width-1||b===png.height-1))throw Error('Clipped Twig state '+name);
 l=Math.max(0,l-1);t=Math.max(0,t-1);r=Math.min(png.width-1,r+1);b=Math.min(png.height-1,b+1);
 const w=r-l+1,h=b-t+1,full=indexed(png,pal,{dither:.25}),idx=new Uint8Array(w*h);
 for(let y=0;y<h;y++)idx.set(full.subarray((t+y)*png.width+l,(t+y)*png.width+l+w),y*w);
 const bytes=writePng8(w,h,idx,pal),file='twigmod-'+name+'.png';fs.writeFileSync(path.join(dir,file),bytes);
 outputs.push({key:name,file,crop:[l,t,w,h],icon,bytes:bytes.length,sha256:sha(bytes)});
}
let cs='// Generated by tools/heroanim/bake-twig-gear.mjs.\nnamespace Bloomlings.Client.UI.Design\n{\n    public static partial class TwigGearArt\n    {\n        private static readonly HeroFrame[] Frames = new[]\n        {\n';
for(const f of outputs)cs+='            new HeroFrame("'+f.file.slice(0,-4)+'", '+f.crop.join(', ')+', .5f, .68f, .5f, .35f),\n';
cs+='        };\n    }\n}\n';fs.writeFileSync(dataFile,cs);
const sources=[...report.inputs.map(f=>({path:f.path,sha256:f.sha256})),...['prepare-twig-gear.py','bake-twig-gear.mjs','twig-materials.json'].map(file=>({path:path.relative(repo,path.join(here,file)),sha256:sha(fs.readFileSync(path.join(here,file)))}))];
fs.writeFileSync(path.join(here,'twig-gear.json'),JSON.stringify({version:1,sources,derivedGlbSha256:report.derivedGlbSha256,data:path.relative(repo,dataFile),dataSha256:sha(Buffer.from(cs)),cell:[448,504],outputs},null,2)+'\n');
fs.writeFileSync(path.join(here,'models/twig/build-report.json'),JSON.stringify(report,null,2)+'\n');
const folderFile=path.join(dir,'manifest.json'),folder=JSON.parse(fs.readFileSync(folderFile));folder.files=folder.files.filter(x=>!x.path.startsWith('twigmod-'));
for(const f of outputs)folder.files.push({path:f.file,width:f.crop[2],height:f.crop[3],sha256:f.sha256,slot:'char.hero3d.motion.twig',source:'owner',record:'tools/heroanim/SOURCE.md'});
fs.writeFileSync(folderFile,JSON.stringify(folder,null,1)+'\n');
console.log('Baked '+outputs.length+' Twig states/icons, '+outputs.reduce((n,x)=>n+x.bytes,0)+' bytes');

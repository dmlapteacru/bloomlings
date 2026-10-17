// Active Twig bake: five small 2D images from the segmented Tripo base and fitted hats.
// First: blender -b --factory-startup -t 2 --python-exit-code 1 --python tools/heroanim/twig-tripo-study.py
// Then: node tools/heroanim/bake-twig-tripo.mjs
import fs from 'node:fs';
import path from 'node:path';
import http from 'node:http';
import {fileURLToPath} from 'node:url';
import {createHash} from 'node:crypto';
import {chromium} from 'playwright-core';
import {PNG} from 'pngjs';
const here=path.dirname(fileURLToPath(import.meta.url)),repo=path.resolve(here,'../..');
const buildDir=path.join(repo,'content/work/twig-tripo-study/build');
const model=path.join(buildDir,'TwigHero_Review.glb'),report=JSON.parse(fs.readFileSync(path.join(buildDir,'build-report.json')));
const three=path.join(here,'node_modules/three');
const dir=path.join(repo,'client/Assets/Bloomlings/Art/Heroes/Resources/HeroMotion');
const dataFile=path.join(repo,'client/Assets/Bloomlings/UI/Design/TwigGearData.cs');
const look={ground:'#f0e7d5',hemisphere:1.2,key:1.2,fill:.6,environment:.45,halfHeight:.7,cameraY:.59,supersample:3,headWidthScale:.88};
const review=process.argv.includes('--review');
const reviewDir=path.join(repo,'content/work/twig-tripo-runtime');
fs.mkdirSync(reviewDir,{recursive:true});
const sha=b=>createHash('sha256').update(b).digest('hex');
const modelHash=report.exports.find(f=>f.file==='TwigHero_Review.glb').sha256;
if(sha(fs.readFileSync(model))!==modelHash)throw Error('Derived Twig model changed; prepare it again');
const html=`<!doctype html><script type="importmap">{"imports":{"three":"/three/build/three.module.js"}}</script><script type="module">
import * as T from 'three';import {GLTFLoader} from '/three/examples/jsm/loaders/GLTFLoader.js';import {RoomEnvironment} from '/three/examples/jsm/environments/RoomEnvironment.js';
const renderer=new T.WebGLRenderer({antialias:true,alpha:true,preserveDrawingBuffer:true});renderer.setPixelRatio(1);renderer.setClearColor(0,0);renderer.outputColorSpace=T.SRGBColorSpace;renderer.toneMapping=T.NoToneMapping;
const scene=new T.Scene();scene.add(new T.HemisphereLight(0xffffff,'${look.ground}',${look.hemisphere}));for(const [p,k]of [[[-3,4,5],${look.key}],[[3,2,4],${look.fill}]]){const l=new T.DirectionalLight(0xffffff,k);l.position.set(...p);scene.add(l);}
const room=new RoomEnvironment(),pmrem=new T.PMREMGenerator(renderer),environment=pmrem.fromScene(room,.04);scene.environment=environment.texture;room.dispose();pmrem.dispose();
const gltf=await new GLTFLoader().loadAsync('/model.glb');scene.add(gltf.scene);
// Correct the broad Tripo head on the render copy only. Its ears, mouth and both hats share this pivot.
const head=gltf.scene.getObjectByName('TwigHero_HeadPivot');
if(!head)throw Error('Shared head pivot missing');
head.scale.x*=${look.headWidthScale};
const bodyParts=[];gltf.scene.traverse(o=>{if(o.name.startsWith('TwigHero_base_part_'))bodyParts.push(o);});
const acorn=gltf.scene.getObjectByName('TwigHero_AcornHat'),cap=gltf.scene.getObjectByName('TwigHero_FlowerCap');
gltf.scene.traverse(o=>{if(o.isMesh){o.frustumCulled=false;for(const m of (Array.isArray(o.material)?o.material:[o.material]))m.envMapIntensity=${look.environment};}});
const camera=new T.OrthographicCamera(-1,1,1,-1,.1,30);
if(bodyParts.length!==8||!acorn||!cap)throw Error('Incomplete segmented Twig model');
window.render=({state,icon=false})=>{
 for(const part of bodyParts)part.visible=!icon;acorn.visible=state==='acorn';cap.visible=state==='cap';
 gltf.scene.updateMatrixWorld(true);gltf.scene.traverse(o=>{if(o.isSkinnedMesh)o.skeleton.update();});
 if(icon){const item=state==='acorn'?acorn:cap,box=new T.Box3().setFromObject(item,true),center=box.getCenter(new T.Vector3()),size=box.getSize(new T.Vector3()),half=Math.max(size.y,size.x)*.7;
 camera.left=-half;camera.right=half;camera.top=half;camera.bottom=-half;camera.position.set(center.x+1.3,center.y+.8,center.z+5);camera.lookAt(center);renderer.setSize(320,320);}
 else{const half=${look.halfHeight};camera.left=-half*448/504;camera.right=half*448/504;camera.top=half;camera.bottom=-half;camera.position.set(0,${look.cameraY}+.02,5);camera.lookAt(0,${look.cameraY},0);renderer.setSize(448,504);}
 renderer.setSize(renderer.domElement.width*${look.supersample},renderer.domElement.height*${look.supersample});
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
 for(const [name,state,icon]of recipes){const b64=await page.evaluate(o=>window.render(o),{state,icon});const bytes=Buffer.from(b64,'base64');fs.writeFileSync(path.join(reviewDir,name+'-master.png'),bytes);images.push({name,png:reduce(PNG.sync.read(bytes),look.supersample),icon});}
}finally{if(browser)await browser.close();server.close();}
// Average linear-light premultiplied samples: transparent borders cannot darken the smooth silhouette.
function reduce(source,scale){
 const png=new PNG({width:source.width/scale,height:source.height/scale}),linear=new Float64Array(256);
 for(let i=0;i<256;i++){const c=i/255;linear[i]=c<=.04045?c/12.92:((c+.055)/1.055)**2.4;}
 const encode=c=>Math.round(255*(c<=.0031308?12.92*c:1.055*c**(1/2.4)-.055));
 for(let y=0;y<png.height;y++)for(let x=0;x<png.width;x++){
  const sum=[0,0,0];let alpha=0;
  for(let sy=0;sy<scale;sy++)for(let sx=0;sx<scale;sx++){const i=((y*scale+sy)*source.width+x*scale+sx)*4,a=source.data[i+3]/255;alpha+=a;for(let c=0;c<3;c++)sum[c]+=linear[source.data[i+c]]*a;}
  const i=(y*png.width+x)*4;png.data[i+3]=Math.round(alpha*255/(scale*scale));for(let c=0;c<3;c++)png.data[i+c]=alpha>0?encode(sum[c]/alpha):0;
 }
 return png;
}
const outputs=[];
for(const {name,png,icon}of images){
 let l=png.width,t=png.height,r=-1,b=-1;
 for(let y=0;y<png.height;y++)for(let x=0;x<png.width;x++)if(png.data[(y*png.width+x)*4+3]>2){l=Math.min(l,x);t=Math.min(t,y);r=Math.max(r,x);b=Math.max(b,y);}
 if(r<l)throw Error('Empty '+name);if(!icon&&(l===0||t===0||r===png.width-1||b===png.height-1))throw Error('Clipped Twig state '+name);
 l=Math.max(0,l-1);t=Math.max(0,t-1);r=Math.min(png.width-1,r+1);b=Math.min(png.height-1,b+1);
 const w=r-l+1,h=b-t+1,cropped=new PNG({width:w,height:h});
 PNG.bitblt(png,cropped,l,t,w,h,0,0);
 const bytes=PNG.sync.write(cropped,{colorType:6}),file='twigmod-'+name+'.png';fs.writeFileSync(path.join(review?reviewDir:dir,file),bytes);
 outputs.push({key:name,file,crop:[l,t,w,h],icon,bytes:bytes.length,sha256:sha(bytes)});
}
if(review){console.log('Twig review: '+outputs.reduce((n,x)=>n+x.bytes,0)+' bytes; '+reviewDir);process.exit(0);}
let cs='// Generated by tools/heroanim/bake-twig-tripo.mjs.\nnamespace Bloomlings.Client.UI.Design\n{\n    public static partial class TwigGearArt\n    {\n        private static readonly HeroFrame[] Frames = new[]\n        {\n';
for(const f of outputs)cs+='            new HeroFrame("'+f.file.slice(0,-4)+'", '+f.crop.join(', ')+', .5f, .68f, .5f, .35f),\n';
cs+='        };\n    }\n}\n';fs.writeFileSync(dataFile,cs);
const sourceFiles=[...report.sources.map(f=>'tools/heroanim/models/twig-tripo/source/'+f.file),
 'tools/heroanim/twig-tripo-study.py','tools/heroanim/twig-tripo-fit.json','tools/heroanim/bake-twig-tripo.mjs'];
const sources=sourceFiles.map(file=>({path:file,sha256:sha(fs.readFileSync(path.join(repo,file)))}));
fs.writeFileSync(path.join(here,'twig-gear.json'),JSON.stringify({version:2,sourceKind:'segmented Tripo; shared base/head; static pose without rig',sources,derivedGlbSha256:modelHash,data:path.relative(repo,dataFile),dataSha256:sha(Buffer.from(cs)),cell:[448,504],encoding:'RGBA8 lossless PNG; no palette or dithering',supersample:look.supersample,render:look,outputs},null,2)+'\n');
const folderFile=path.join(dir,'manifest.json'),folder=JSON.parse(fs.readFileSync(folderFile));folder.files=folder.files.filter(x=>!x.path.startsWith('twigmod-'));
for(const f of outputs)folder.files.push({path:f.file,width:f.crop[2],height:f.crop[3],sha256:f.sha256,slot:'char.hero3d.motion.twig',source:'owner',record:'tools/heroanim/SOURCE.md'});
fs.writeFileSync(folderFile,JSON.stringify(folder,null,1)+'\n');
console.log('Baked '+outputs.length+' Twig states/icons, '+outputs.reduce((n,x)=>n+x.bytes,0)+' bytes');

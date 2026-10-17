// Owner's modular V2: eight shared-base states, depth-masked glasses / scarf, and nine Shop icons.
import fs from 'node:fs';
import path from 'node:path';
import http from 'node:http';
import {fileURLToPath} from 'node:url';
import {createHash} from 'node:crypto';
import {chromium} from 'playwright-core';
import {PNG} from 'pngjs';
const here=path.dirname(fileURLToPath(import.meta.url)),repo=path.resolve(here,'../..');
const build=path.join(repo,'content/work/twig-v2/build'),report=JSON.parse(fs.readFileSync(path.join(build,'build-report.json')));
const model=path.join(build,report.export.file),three=path.join(here,'node_modules/three');
const sha=b=>createHash('sha256').update(b).digest('hex');
if(sha(fs.readFileSync(model))!==report.export.sha256||sha(fs.readFileSync(path.join(here,'prepare-twig-v2.py')))!==report.scriptSha256)throw Error('Rebuild Twig V2 first');
const review=process.argv.includes('--review'),reviewDir=path.join(repo,'content/work/twig-v2/review');fs.mkdirSync(reviewDir,{recursive:true});
const dir=review?reviewDir:path.join(repo,'client/Assets/Bloomlings/Art/Heroes/Resources/HeroMotion');
const heads=['none','PumpkinHat','BucketHat','Hair','HatHair','Beret','EarmuffHair','XmasHat'];
const icons=[...heads.slice(1),'Glasses','Scarf'];
const html=`<script type="importmap">{"imports":{"three":"/three/build/three.module.js"}}</script><script type="module">
import * as T from 'three';import{GLTFLoader}from'/three/examples/jsm/loaders/GLTFLoader.js';
const renderer=new T.WebGLRenderer({antialias:true,alpha:true,preserveDrawingBuffer:true});renderer.setPixelRatio(1);renderer.outputColorSpace=T.SRGBColorSpace;renderer.setClearColor(0,0);renderer.toneMapping=T.NoToneMapping;
const scene=new T.Scene();scene.add(new T.HemisphereLight(0xffffff,0xffffff,2));for(const[p,k]of[[[-3,4,5],1],[[3,2,4],.4]]){const l=new T.DirectionalLight(0xffffff,k);l.position.set(...p);scene.add(l);}
const gltf=await new GLTFLoader().loadAsync('/model.glb');scene.add(gltf.scene);
const parts={},original=new Map();for(const key of ['base',...${JSON.stringify(icons)}]){parts[key]=gltf.scene.getObjectByName('TwigV2_'+key);if(!parts[key])throw Error('Missing '+key);}
gltf.scene.traverse(o=>{if(o.isMesh){o.frustumCulled=false;const mats=(Array.isArray(o.material)?o.material:[o.material]).map(m=>new T.MeshLambertMaterial({map:m.map,color:m.color.clone().multiplyScalar(.25),emissiveMap:m.map,emissive:m.color.clone().multiplyScalar(.75),side:T.DoubleSide,transparent:m.transparent,opacity:m.opacity,depthWrite:!m.transparent}));o.material=Array.isArray(o.material)?mats:mats[0];original.set(o,o.material);}});
const depth=new T.MeshBasicMaterial({colorWrite:false,depthWrite:true,side:T.DoubleSide});
const camera=new T.OrthographicCamera(-.64,.64,1,0,.01,30);
window.render=({head='none',layer='base',icon=false,combined=false})=>{
 for(const[key,obj]of Object.entries(parts)){obj.visible=false;obj.traverse(m=>{if(m.isMesh){m.material=original.get(m);m.renderOrder=0;}});}
 const show=key=>{parts[key].visible=true;};
 if(icon){show(layer);const box=new T.Box3().setFromObject(parts[layer]),center=box.getCenter(new T.Vector3()),size=box.getSize(new T.Vector3()),half=Math.max(size.x,size.y)*.70;camera.left=-half;camera.right=half;camera.top=half;camera.bottom=-half;camera.position.set(center.x+(layer==='Glasses'?.6:1.1),center.y+.6,center.z+5);camera.lookAt(center);renderer.setSize(320*3,320*3);}
 else{
  show('base');if(head!=='none')show(head);if(layer!=='base')show(layer);if(combined){show('Glasses');show('Scarf');}
  if(layer!=='base'&&!combined)for(const obj of [parts.base,...(head==='none'?[]:[parts[head]])])obj.traverse(m=>{if(m.isMesh){m.material=depth;m.renderOrder=-10;}});
  // Pad above the 448×504 logical cell; the tall Xmas hat keeps the exact base scale and foot line.
  const top=1.296+96/350,bottom=-.144,center=(top+bottom)/2;
  camera.left=-.64;camera.right=.64;camera.top=(top-bottom)/2;camera.bottom=-(top-bottom)/2;camera.position.set(0,center,5);camera.lookAt(0,center,0);renderer.setSize(448*3,600*3);
 }
 gltf.scene.updateMatrixWorld(true);camera.updateProjectionMatrix();renderer.render(scene,camera);return renderer.domElement.toDataURL().split(',')[1];
};window.ready=true;</script>`;
const server=http.createServer((req,res)=>{const u=new URL(req.url,'http://x').pathname;if(u==='/'){res.end(html);return;}const f=u==='/model.glb'?model:u.startsWith('/three/')?path.resolve(three,u.slice(7)):null;if(!f||(f!==model&&!f.startsWith(three+path.sep))||!fs.existsSync(f)){res.writeHead(404);res.end();return;}res.setHeader('Content-Type',f.endsWith('.js')?'text/javascript':'application/octet-stream');fs.createReadStream(f).pipe(res);});
await new Promise(r=>server.listen(0,'127.0.0.1',r));let browser;const images=[];
try{
 browser=await chromium.launch({executablePath:'/usr/bin/chromium',args:['--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist']});
 const page=await browser.newPage();page.on('pageerror',e=>{throw e;});await page.goto('http://127.0.0.1:'+server.address().port);await page.waitForFunction(()=>window.ready,{},{timeout:120000});
 for(const layer of ['base','Scarf','Glasses'])for(const head of heads){const raw=await page.evaluate(o=>window.render(o),{head,layer});images.push({key:layer.toLowerCase()+'-'+head.toLowerCase(),png:reduce(PNG.sync.read(Buffer.from(raw,'base64')),3),icon:false});}
 for(const layer of icons){const raw=await page.evaluate(o=>window.render(o),{layer,icon:true});images.push({key:'icon-'+layer.toLowerCase(),png:reduce(PNG.sync.read(Buffer.from(raw,'base64')),3),icon:true});}
 if(review)for(const head of heads){const raw=await page.evaluate(o=>window.render(o),{head,combined:true});fs.writeFileSync(path.join(reviewDir,'combined-'+head.toLowerCase()+'.png'),Buffer.from(raw,'base64'));}
}finally{if(browser)await browser.close();server.close();}
// Alpha-safe linear-light reduction keeps bright edges and facial gradients intact.
function reduce(source,scale){
 const png=new PNG({width:source.width/scale,height:source.height/scale}),linear=new Float64Array(256);
 for(let i=0;i<256;i++){const c=i/255;linear[i]=c<=.04045?c/12.92:((c+.055)/1.055)**2.4;}
 const encode=c=>Math.round(255*(c<=.0031308?12.92*c:1.055*c**(1/2.4)-.055));
 for(let y=0;y<png.height;y++)for(let x=0;x<png.width;x++){
  const sum=[0,0,0];let alpha=0;
  for(let sy=0;sy<scale;sy++)for(let sx=0;sx<scale;sx++){const i=((y*scale+sy)*source.width+x*scale+sx)*4,a=source.data[i+3]/255;alpha+=a;for(let c=0;c<3;c++)sum[c]+=linear[source.data[i+c]]*a;}
  const i=(y*png.width+x)*4;png.data[i+3]=Math.round(alpha*255/(scale*scale));for(let c=0;c<3;c++)png.data[i+c]=alpha>0?encode(sum[c]/alpha):0;
 }return png;
}
const outputs=[];
for(const{key,png,icon}of images){
 let l=png.width,t=png.height,r=-1,b=-1;for(let y=0;y<png.height;y++)for(let x=0;x<png.width;x++)if(png.data[(y*png.width+x)*4+3]>2){l=Math.min(l,x);t=Math.min(t,y);r=Math.max(r,x);b=Math.max(b,y);}
 if(r<l||l===0||t===0||r===png.width-1||b===png.height-1)throw Error('Empty / clipped '+key);
 l--;t--;r++;b++;const w=r-l+1,h=b-t+1,crop=new PNG({width:w,height:h});PNG.bitblt(png,crop,l,t,w,h,0,0);
 const bytes=PNG.sync.write(crop,{colorType:6}),file='twigv2-'+key+'.png';fs.writeFileSync(path.join(dir,file),bytes);
 outputs.push({key,file,crop:[l,t-(icon?0:96),w,h],icon,bytes:bytes.length,sha256:sha(bytes)});
}
if(review){fs.writeFileSync(path.join(reviewDir,'review.json'),JSON.stringify({outputs},null,2));console.log('V2 review:',outputs.length,'sprites');process.exit(0);}
const dataFile=path.join(repo,'client/Assets/Bloomlings/UI/Design/TwigV2Data.cs');
let cs='// Generated by tools/heroanim/bake-twig-v2.mjs.\nnamespace Bloomlings.Client.UI.Design\n{\n    public static partial class TwigV2Art\n    {\n        private static readonly HeroFrame[] Frames = new[]\n        {\n';
for(const f of outputs)cs+='            new HeroFrame("'+f.file.slice(0,-4)+'", '+f.crop.join(', ')+', .5f, .68f, .5f, .35f),\n';cs+='        };\n    }\n}\n';fs.writeFileSync(dataFile,cs);
const sourceFiles=['tools/heroanim/models/twig-v2/source/TwigHeroV2.zip','tools/heroanim/models/twig-v2/source-manifest.json','tools/heroanim/prepare-twig-v2.py','tools/heroanim/bake-twig-v2.mjs'];
const sources=sourceFiles.map(file=>({path:file,sha256:sha(fs.readFileSync(path.join(repo,file)))}));
fs.writeFileSync(path.join(here,'twig-v2.json'),JSON.stringify({version:1,sources,data:path.relative(repo,dataFile),dataSha256:sha(Buffer.from(cs)),cell:[448,504],paddingTop:96,encoding:'RGBA8 lossless PNG',heads,icons,outputs},null,2)+'\n');
fs.writeFileSync(path.join(here,'models/twig-v2/build-report.json'),JSON.stringify(report,null,2)+'\n');
const file=path.join(dir,'manifest.json'),manifest=JSON.parse(fs.readFileSync(file));manifest.files=manifest.files.filter(f=>!f.path.startsWith('twigv2-'));for(const f of outputs)manifest.files.push({path:f.file,width:f.crop[2],height:f.crop[3],sha256:f.sha256,slot:'char.hero3d.motion.twig',source:'owner',record:'tools/heroanim/SOURCE.md'});fs.writeFileSync(file,JSON.stringify(manifest,null,1)+'\n');
console.log('Baked',outputs.length,'V2 sprites,',outputs.reduce((n,f)=>n+f.bytes,0),'bytes');

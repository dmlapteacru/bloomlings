if (!process.argv.includes('--review')) throw new Error('Retired Twig publisher: use tools/heroanim/bake-twig-v2.mjs. --review is offline only.');
// New shared bald Twig and two headwear surfaces, rendered offline with the approved light paint treatment.
import fs from 'node:fs';import path from 'node:path';import http from 'node:http';
import {fileURLToPath} from 'node:url';import {createHash} from 'node:crypto';
import {chromium} from 'playwright-core';import {PNG} from 'pngjs';
const here=path.dirname(fileURLToPath(import.meta.url)),repo=path.resolve(here,'../..');
const build=path.join(repo,'content/work/twig-modular/build'),report=JSON.parse(fs.readFileSync(path.join(build,'build-report.json')));
const model=path.join(build,'TwigHero_Modular.glb'),three=path.join(here,'node_modules/three');
const fit=JSON.parse(fs.readFileSync(path.join(here,'twig-modular-fit.json'))),look=fit.render;
const review=process.argv.includes('--review'),reviewDir=path.join(repo,'content/work/twig-modular/review');fs.mkdirSync(reviewDir,{recursive:true});
const dir=review?reviewDir:path.join(repo,'client/Assets/Bloomlings/Art/Heroes/Resources/HeroMotion');
const sha=b=>createHash('sha256').update(b).digest('hex');
if(report.scriptSha256!==sha(fs.readFileSync(path.join(here,'prepare-twig-modular.py')))||report.fitSha256!==sha(fs.readFileSync(path.join(here,'twig-modular-fit.json'))))throw Error('Rebuild modular Twig after changing the fit / importer');
for(const s of report.sources)if(sha(fs.readFileSync(path.join(here,'models/twig-modular/source',s.file)))!==s.sha256)throw Error('Source changed: '+s.file);
for(const e of report.exports)if(sha(fs.readFileSync(path.join(build,e.file)))!==e.sha256)throw Error('Prepared export changed: '+e.file);
const html=`<script type="importmap">{"imports":{"three":"/three/build/three.module.js"}}</script><script type="module">
import * as T from 'three';import{GLTFLoader}from'/three/examples/jsm/loaders/GLTFLoader.js';
const renderer=new T.WebGLRenderer({antialias:true,alpha:true,preserveDrawingBuffer:true});renderer.setPixelRatio(1);renderer.outputColorSpace=T.SRGBColorSpace;renderer.setClearColor(0,0);renderer.toneMapping=T.NoToneMapping;
const scene=new T.Scene();scene.add(new T.HemisphereLight(0xffffff,0xffffff,2));for(const[p,k]of[[[-3,4,5],1],[[3,2,4],.4]]){const l=new T.DirectionalLight(0xffffff,k);l.position.set(...p);scene.add(l);}
const gltf=await new GLTFLoader().loadAsync('/model.glb');scene.add(gltf.scene);gltf.scene.traverse(o=>{if(o.isMesh){o.frustumCulled=false;const materials=(Array.isArray(o.material)?o.material:[o.material]).map(m=>new T.MeshLambertMaterial({map:m.map,color:m.color.clone().multiplyScalar(1-${look.albedoShare}),emissiveMap:m.map,emissive:m.color.clone().multiplyScalar(${look.albedoShare}),side:T.DoubleSide}));o.material=Array.isArray(o.material)?materials:materials[0];}});
const base=gltf.scene.getObjectByName('TwigModular_base'),acorn=gltf.scene.getObjectByName('TwigModular_acorn'),panama=gltf.scene.getObjectByName('TwigModular_panama');if(!base||!acorn||!panama)throw Error('Missing shared base / headwear');
const camera=new T.OrthographicCamera(-1,1,1,-1,.01,30);
window.render=({state,icon=false,angle=0})=>{base.visible=!icon;acorn.visible=state==='acorn';panama.visible=state==='panama';gltf.scene.updateMatrixWorld(true);
 if(icon){const box=new T.Box3().setFromObject(state==='acorn'?acorn:panama),center=box.getCenter(new T.Vector3()),size=box.getSize(new T.Vector3()),half=Math.max(size.x,size.y)*.72;camera.left=-half;camera.right=half;camera.top=half;camera.bottom=-half;camera.position.set(center.x+1.3,center.y+.8,center.z+5);camera.lookAt(center);renderer.setSize(320*${look.supersample},320*${look.supersample});}
 else{const half=${look.halfHeight};camera.left=-half*448/504;camera.right=half*448/504;camera.top=half;camera.bottom=-half;camera.position.set(Math.sin(angle)*5,${look.cameraY},Math.cos(angle)*5);camera.lookAt(0,${look.cameraY},0);renderer.setSize(448*${look.supersample},504*${look.supersample});}
 camera.updateProjectionMatrix();renderer.render(scene,camera);return renderer.domElement.toDataURL().split(',')[1];};window.ready=true;</script>`;
const server=http.createServer((req,res)=>{const u=new URL(req.url,'http://x').pathname;if(u==='/'){res.end(html);return;}const file=u==='/model.glb'?model:u.startsWith('/three/')?path.resolve(three,u.slice(7)):null;if(!file||(file!==model&&!file.startsWith(three+path.sep))||!fs.existsSync(file)){res.writeHead(404);res.end();return;}res.setHeader('Content-Type',file.endsWith('.js')?'text/javascript':'application/octet-stream');fs.createReadStream(file).pipe(res);});
await new Promise(r=>server.listen(0,'127.0.0.1',r));let browser;const images=[];
const recipes=[['base','base',false],['acorn','acorn',false],['panama','panama',false],['icon-acorn','acorn',true],['icon-panama','panama',true]];
try{browser=await chromium.launch({executablePath:'/usr/bin/chromium',args:['--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist']});const page=await browser.newPage();page.on('pageerror',e=>{throw e;});await page.goto('http://127.0.0.1:'+server.address().port);await page.waitForFunction(()=>window.ready,{},{timeout:120000});
for(const[name,state,icon]of recipes){const b64=await page.evaluate(o=>window.render(o),{state,icon});images.push({name,png:reduce(PNG.sync.read(Buffer.from(b64,'base64')),look.supersample),icon});}
if(review)for(const state of['acorn','panama'])for(const[view,angle]of[['side',Math.PI/2],['back',Math.PI]]){const b64=await page.evaluate(o=>window.render(o),{state,angle});fs.writeFileSync(path.join(reviewDir,state+'-'+view+'.png'),Buffer.from(b64,'base64'));}
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
const checks={};
for(const image of images.slice(1,3)){
 let faceDifferent=0,bodyDifferent=0;const original=images[0].png;
 for(let y=210;y<320;y++)for(let x=160;x<288;x++)for(let c=0;c<4;c++){const i=(y*448+x)*4+c;if(image.png.data[i]!==original.data[i])faceDifferent++;}
 for(let i=360*448*4;i<original.data.length;i++)if(image.png.data[i]!==original.data[i])bodyDifferent++;
 checks[image.name]={faceDifferent,bodyDifferent};
}
const outputs=[];
for(const{name,png,icon}of images){let l=png.width,t=png.height,r=-1,b=-1;for(let y=0;y<png.height;y++)for(let x=0;x<png.width;x++)if(png.data[(y*png.width+x)*4+3]>2){l=Math.min(l,x);t=Math.min(t,y);r=Math.max(r,x);b=Math.max(b,y);}
 if(r<l||l===0||t===0||r===png.width-1||b===png.height-1)throw Error('Clipped / empty '+name);l--;t--;r++;b++;const w=r-l+1,h=b-t+1,cropped=new PNG({width:w,height:h});PNG.bitblt(png,cropped,l,t,w,h,0,0);const bytes=PNG.sync.write(cropped,{colorType:6}),file='twigfit-'+name+'.png';fs.writeFileSync(path.join(dir,file),bytes);outputs.push({key:name,file,crop:[l,t,w,h],icon,bytes:bytes.length,sha256:sha(bytes)});}
console.log('Shared-base pixel checks',checks);
if(review){console.log('Review written to '+reviewDir);process.exit(0);}
for(const[state,check]of Object.entries(checks))if(check.faceDifferent||check.bodyDifferent)throw Error('Accessory generation changed visible shared face / body: '+state);
const dataFile=path.join(repo,'client/Assets/Bloomlings/UI/Design/TwigModularData.cs');
let cs='// Generated by tools/heroanim/bake-twig-modular.mjs.\nnamespace Bloomlings.Client.UI.Design\n{\n    public static partial class TwigModularArt\n    {\n        private static readonly HeroFrame[] Frames = new[]\n        {\n';
for(const f of outputs)cs+='            new HeroFrame("'+f.file.slice(0,-4)+'", '+f.crop.join(', ')+', .5f, .68f, .5f, .35f),\n';cs+='        };\n    }\n}\n';fs.writeFileSync(dataFile,cs);
const sourceFiles=[...report.sources.map(s=>'tools/heroanim/models/twig-modular/source/'+s.file),'tools/heroanim/models/twig-modular/source-manifest.json','tools/heroanim/prepare-twig-modular.py','tools/heroanim/twig-modular-fit.json','tools/heroanim/bake-twig-modular.mjs'];
const sources=sourceFiles.map(file=>({path:file,sha256:sha(fs.readFileSync(path.join(repo,file)))}));
fs.writeFileSync(path.join(here,'twig-modular.json'),JSON.stringify({version:1,sources,data:path.relative(repo,dataFile),dataSha256:sha(Buffer.from(cs)),cell:[448,504],encoding:'RGBA8 lossless PNG',render:look,sharedBaseChecks:checks,outputs},null,2)+'\n');
fs.writeFileSync(path.join(here,'models/twig-modular/build-report.json'),JSON.stringify(report,null,2)+'\n');
const manifestFile=path.join(dir,'manifest.json'),manifest=JSON.parse(fs.readFileSync(manifestFile));manifest.files=manifest.files.filter(f=>!f.path.startsWith('twigfit-'));for(const f of outputs)manifest.files.push({path:f.file,width:f.crop[2],height:f.crop[3],sha256:f.sha256,slot:'char.hero3d.motion.twig',source:'owner',record:'tools/heroanim/SOURCE.md'});fs.writeFileSync(manifestFile,JSON.stringify(manifest,null,1)+'\n');
console.log('Baked five modular Twig states/icons, '+outputs.reduce((n,f)=>n+f.bytes,0)+' bytes');

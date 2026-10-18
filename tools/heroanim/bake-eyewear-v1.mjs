// Add owner-fitted eyewear as small depth-masked layers; keep accepted hero/garment bakes intact.
import fs from 'node:fs';import path from 'node:path';import http from 'node:http';
import {fileURLToPath} from 'node:url';import{createHash}from'node:crypto';
import{chromium}from'playwright-core';import{PNG}from'pngjs';
const here=path.dirname(fileURLToPath(import.meta.url)),repo=path.resolve(here,'../..');
const read=f=>JSON.parse(fs.readFileSync(path.join(repo,f))),sha=b=>createHash('sha256').update(b).digest('hex');
const sourceRoot='tools/heroanim/models/eyewear-v1',build='content/work/eyewear-v1/build';
const source=read(sourceRoot+'/source-manifest.json'),prepared=read(build+'/build-report.json');
if(prepared.scriptSha256!==sha(fs.readFileSync(path.join(here,'prepare-eyewear-v1.py')))||prepared.manifestSha256!==sha(fs.readFileSync(path.join(repo,sourceRoot,'source-manifest.json'))))throw Error('Prepare eyewear first');
const review=process.argv.includes('--review'),reviewDir=path.join(repo,'content/work/eyewear-v1/review');fs.mkdirSync(reviewDir,{recursive:true});
const outputDir=path.join(repo,'client/Assets/Bloomlings/Art/Heroes/Resources/HeroMotion');
const outputs=[],sets=[],sources=new Map(),seen=new Map();
function pin(file){sources.set(file,sha(fs.readFileSync(path.join(repo,file))));}
for(const file of [sourceRoot+'/source-manifest.json','tools/heroanim/prepare-eyewear-v1.py','tools/heroanim/bake-eyewear-v1.mjs','tools/heroanim/home-heroes-look.json'])pin(file);
const linear=Float64Array.from({length:256},(_,i)=>{const c=i/255;return c<=.04045?c/12.92:((c+.055)/1.055)**2.4;});
function reduced(raw){
 const src=PNG.sync.read(Buffer.from(raw,'base64')),png=new PNG({width:src.width/3,height:src.height/3});
 const encode=c=>Math.round(255*(c<=.0031308?12.92*c:1.055*c**(1/2.4)-.055));
 for(let y=0;y<png.height;y++)for(let x=0;x<png.width;x++){
  const sum=[0,0,0];let alpha=0;
  for(let dy=0;dy<3;dy++)for(let dx=0;dx<3;dx++){const i=((y*3+dy)*src.width+x*3+dx)*4,a=src.data[i+3]/255;alpha+=a;for(let c=0;c<3;c++)sum[c]+=linear[src.data[i+c]]*a;}
  const i=(y*png.width+x)*4;png.data[i+3]=Math.round(alpha*255/9);for(let c=0;c<3;c++)png.data[i+c]=alpha?encode(sum[c]/alpha):0;
 }return png;
}
function save(raw,family,variant,view){
 const p=reduced(raw);let l=p.width,t=p.height,r=-1,b=-1;
 for(let y=0;y<p.height;y++)for(let x=0;x<p.width;x++)if(p.data[(y*p.width+x)*4+3]>2){l=Math.min(l,x);t=Math.min(t,y);r=Math.max(r,x);b=Math.max(b,y);}
 if(r<l||l===0||t===0||r===p.width-1||b===p.height-1)throw Error('Empty / clipped '+variant+'/'+view);
 l--;t--;r++;b++;const w=r-l+1,h=b-t+1,crop=new PNG({width:w,height:h});PNG.bitblt(p,crop,l,t,w,h,0,0);
 const bytes=PNG.sync.write(crop,{colorType:6}),hash=sha(bytes),coords=[l,t-(view==='icon'?0:96),w,h],key=family+'|'+view+'|'+hash+'|'+coords.join(',');
 if(seen.has(key))return seen.get(key);
 const prefix=view==='home'?'homehero-'+family+'-eyewear-':'eyewear-'+family+'-';
 const file=prefix+String(outputs.length).padStart(3,'0')+'.png',index=outputs.length;
 outputs.push({family,view,file,crop:coords,bytes:bytes.length,sha256:hash});seen.set(key,index);fs.writeFileSync(path.join(outputDir,file),bytes);return index;
}
let browser;
try{
 browser=await chromium.launch({executablePath:'/usr/bin/chromium',args:['--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist']});
 for(const family of ['sprig','bloom','twig']){
  const metaFile='tools/heroanim/'+family+'-v2.json',lookFile='tools/heroanim/'+family+'-v2-look.json',proof='tools/heroanim/models/'+family+'-v2/build-report.json';
  [metaFile,lookFile,proof].forEach(pin);const meta=read(metaFile),look=read(lookFile),report=read('content/work/'+family+'-v2/build/build-report.json');
  const model=path.join(repo,'content/work/'+family+'-v2/build',report.export.file);if(sha(fs.readFileSync(model))!==report.export.sha256)throw Error('Stale '+family+' model');
  const variants=source.variants.filter(v=>v.family===family),files=new Map([['/model.glb',model]]);
  for(const variant of variants){pin(sourceRoot+'/'+variant.source);const p=prepared.variants.find(p=>p.id===variant.id),file=path.join(repo,build,p.file);if(sha(fs.readFileSync(file))!==p.sha256)throw Error('Stale eyewear');files.set('/'+variant.key+'.glb',file);}
  const homeYaw=read('tools/heroanim/home-heroes-look.json').yawDegrees[family],prefix=family[0].toUpperCase()+family.slice(1)+'V2_';
  const scarves=meta.scarves??['none','Scarf'];
  const html=`<script type="importmap">{"imports":{"three":"/three/build/three.module.js"}}</script><script type="module">
import * as T from 'three';import{GLTFLoader}from'/three/examples/jsm/loaders/GLTFLoader.js';
const renderer=new T.WebGLRenderer({antialias:true,alpha:true,preserveDrawingBuffer:true});renderer.setPixelRatio(1);renderer.outputColorSpace=T.SRGBColorSpace;renderer.setClearColor(0,0);renderer.toneMapping=T.NoToneMapping;
const scene=new T.Scene(),subject=new T.Group(),studio=new T.Group();scene.add(subject,studio);scene.add(new T.HemisphereLight(0xffffff,0xffffff,2));
for(const[p,k]of[[[-3,4,5],1],[[3,2,4],.4]]){const l=new T.DirectionalLight(0xffffff,k);l.position.set(...p);studio.add(l);}
const loader=new GLTFLoader(),gltf=await loader.loadAsync('/model.glb');subject.add(gltf.scene);
const parts={},prefix=${JSON.stringify(prefix)},look=${JSON.stringify(look)},original=new Map();
for(const key of ['base',...${JSON.stringify(meta.icons)}])parts[key]=gltf.scene.getObjectByName(prefix+key);
const eyewear={};for(const key of ${JSON.stringify(variants.map(v=>v.key))}){const g=await loader.loadAsync('/'+key+'.glb');eyewear[key]=g.scene;subject.add(g.scene);}
subject.traverse(o=>{if(!o.isMesh)return;o.frustumCulled=false;
let part=o;while(part.parent&&!part.name.startsWith(prefix)&&!Object.values(eyewear).includes(part))part=part.parent;
const mats=(Array.isArray(o.material)?o.material:[o.material]).map(m=>{
 const color=m.color.clone(),opacity=m.opacity;
 const n=new T.MeshLambertMaterial({map:m.map,color:color.clone().multiplyScalar(.25),emissiveMap:m.map,emissive:color.clone().multiplyScalar(.75),side:T.DoubleSide,transparent:opacity<1,opacity,depthWrite:opacity>=1});
 if(look.panama&&part.name===prefix+'BucketHat'&&m.name.startsWith(look.panama.materialPrefix)){
  const warm='vec3 warmPanama(vec3 c){float mask=1.0-smoothstep(.35,.60,c.b/max(max(c.r,c.g),.0001));float value=dot(c,vec3(.2126,.7152,.0722));return mix(c,vec3('+look.panama.linearHue.join(',')+')*value*'+look.panama.brightness.toFixed(3)+',mask);}';
  n.onBeforeCompile=s=>{s.fragmentShader=warm+'\\n'+s.fragmentShader;s.fragmentShader=s.fragmentShader.replace('#include <map_fragment>','#include <map_fragment>\\ndiffuseColor.rgb=warmPanama(diffuseColor.rgb);').replace('#include <emissivemap_fragment>','#include <emissivemap_fragment>\\ntotalEmissiveRadiance=warmPanama(totalEmissiveRadiance);');};n.customProgramCacheKey=()=>warm;
 }return n;
});o.material=Array.isArray(o.material)?mats:mats[0];original.set(o,o.material);});
const canvasSize=new T.Vector2();function resize(w,h){renderer.getSize(canvasSize);if(canvasSize.x!==w||canvasSize.y!==h)renderer.setSize(w,h);}
const depth=new T.MeshBasicMaterial({colorWrite:false,depthWrite:true,side:T.DoubleSide}),camera=new T.OrthographicCamera(-.64,.64,1,0,.01,30),cache=new Map();
window.render=({variant,head,clothes='none',scarf='none',view='front',combined=false,yaw=null})=>{
 const angle=yaw??(view==='home'?${homeYaw}:0);subject.rotation.y=studio.rotation.y=angle*Math.PI/180;
 for(const obj of [...Object.values(parts),...Object.values(eyewear)]){obj.visible=false;obj.traverse(o=>{if(o.isMesh){o.material=original.get(o);o.renderOrder=0;}});}
 for(const key of ${JSON.stringify(scarves.filter(s=>s!=='none'))})parts[key].traverse(o=>{if(o.isMesh&&o.morphTargetInfluences){o.morphTargetInfluences.fill(0);if(clothes!=='none'){const i=o.morphTargetDictionary['Over_'+clothes];if(i!==undefined)o.morphTargetInfluences[i]=1;}}});
 const target=eyewear[variant];target.visible=true;const selected=[parts.base,parts[head],...(clothes==='none'?[]:[parts[clothes]]),...(scarf==='none'?[]:[parts[scarf]])];
 if(view==='icon'){
  subject.rotation.y=studio.rotation.y=0;subject.updateMatrixWorld(true);const box=new T.Box3().setFromObject(target),center=box.getCenter(new T.Vector3()),size=box.getSize(new T.Vector3()),half=Math.max(size.x,size.y)*.70;
  camera.left=-half;camera.right=half;camera.top=half;camera.bottom=-half;camera.position.set(center.x+.6,center.y+.6,center.z+5);camera.lookAt(center);resize(320*3,320*3);
 }else{
  for(const o of selected)o.visible=true;subject.updateMatrixWorld(true);
  const scale=${family==='bloom'?'.94':'1'},top=(1.296+96/350)/scale,bottom=-.144/scale,center=(top+bottom)/2;
  camera.left=-.64/scale;camera.right=.64/scale;camera.top=(top-bottom)/2;camera.bottom=-(top-bottom)/2;camera.position.set(0,center,5);camera.lookAt(0,center,0);resize(448*3,600*3);
  if(!combined){
   const box=new T.Box3().setFromObject(target),masks=selected.filter(o=>{const b=new T.Box3().setFromObject(o);return b.max.x>=box.min.x&&b.min.x<=box.max.x&&b.max.y>=box.min.y&&b.min.y<=box.max.y;});
   const key=variant+'|'+angle+'|'+(masks.some(o=>o.name.includes('Scarf'))?clothes:'')+'|'+masks.map(o=>o.name).sort().join(',');if(cache.has(key))return cache.get(key);
   for(const o of selected)o.traverse(m=>{if(m.isMesh){m.material=depth;m.renderOrder=-10;}});
   camera.updateProjectionMatrix();camera.updateMatrixWorld();
   // Only eyewear writes color. Restrict depth rasterization to its projected bounds without changing pixels.
   let left=Infinity,right=-Infinity,bottom=Infinity,top=-Infinity;
   for(const x of [box.min.x,box.max.x])for(const y of [box.min.y,box.max.y])for(const z of [box.min.z,box.max.z]){
    const p=new T.Vector3(x,y,z).project(camera);left=Math.min(left,(p.x+1)*672);right=Math.max(right,(p.x+1)*672);bottom=Math.min(bottom,(p.y+1)*900);top=Math.max(top,(p.y+1)*900);
   }
   const l=Math.max(0,Math.floor(left)-6),b=Math.max(0,Math.floor(bottom)-6),r=Math.min(1344,Math.ceil(right)+6),t=Math.min(1800,Math.ceil(top)+6);
   renderer.setScissorTest(false);renderer.clear();renderer.setScissor(l,b,r-l,t-b);renderer.setScissorTest(true);
   renderer.render(scene,camera);renderer.setScissorTest(false);const raw=renderer.domElement.toDataURL().split(',')[1];cache.set(key,raw);return raw;
  }
 }
 camera.updateProjectionMatrix();renderer.render(scene,camera);return renderer.domElement.toDataURL().split(',')[1];
};window.ready=true;</script>`;
  const three=path.join(here,'node_modules/three');
  const server=http.createServer((req,res)=>{const u=new URL(req.url,'http://x').pathname;if(u==='/'){res.end(html);return;}const file=files.get(u)??(u.startsWith('/three/')?path.resolve(three,u.slice(7)):null);if(!file||(!files.has(u)&&!file.startsWith(three+path.sep))||!fs.existsSync(file)){res.writeHead(404);res.end();return;}res.setHeader('Content-Type',file.endsWith('.js')?'text/javascript':'application/octet-stream');fs.createReadStream(file).pipe(res);});
  await new Promise(r=>server.listen(0,'127.0.0.1',r));const page=await browser.newPage();
  try{
   page.on('pageerror',e=>console.error(e));await page.goto('http://127.0.0.1:'+server.address().port);await page.waitForFunction(()=>window.ready,{},{timeout:120000});
   for(const variant of variants){
    const stock=meta.heads[1];
    if(review){
     for(const yaw of new Set([0,homeYaw,75,-75])){const raw=await page.evaluate(o=>window.render(o),{variant:variant.key,head:stock,combined:true,yaw});fs.writeFileSync(path.join(reviewDir,variant.key+'-'+yaw+'.png'),PNG.sync.write(reduced(raw),{colorType:6}));}
     const raw=await page.evaluate(o=>window.render(o),{variant:variant.key,head:stock,view:'icon'});fs.writeFileSync(path.join(reviewDir,variant.key+'-icon.png'),PNG.sync.write(reduced(raw),{colorType:6}));continue;
    }
    const set={id:variant.id,family,front:[],home:[]},raws=new Map();
    for(const view of ['front','home'])for(const head of meta.heads.slice(1))for(const clothes of meta.clothes)for(const scarf of scarves){
     const raw=await page.evaluate(o=>window.render(o),{variant:variant.key,head,clothes,scarf,view});const key=view+'|'+raw;
     let index=raws.get(key);if(index===undefined){index=save(raw,family,variant.key,view);raws.set(key,index);}set[view].push(index);
    }
    set.icon=save(await page.evaluate(o=>window.render(o),{variant:variant.key,head:stock,view:'icon'}),family,variant.key,'icon');sets.push(set);
    console.log(variant.id+': '+set.front.length+' front / '+set.home.length+' Home outfit masks');
   }
  }finally{await page.close();await new Promise(r=>server.close(r));}
 }
}finally{if(browser)await browser.close();}
if(review){console.log('Eyewear review in '+reviewDir);process.exit(0);}
const data='client/Assets/Bloomlings/UI/Design/EyewearData.cs';
let cs='// Generated by tools/heroanim/bake-eyewear-v1.mjs.\nusing System.Collections.Generic;\nusing Bloomlings.Core.Variants;\nnamespace Bloomlings.Client.UI.Design\n{\n    public static partial class EyewearArt\n    {\n        private static readonly HeroFrame[] Frames = new[]\n        {\n';
for(const f of outputs)cs+='            new HeroFrame("'+f.file.slice(0,-4)+'", '+f.crop.join(', ')+', .5f, .68f, .5f, .35f),\n';
cs+='        };\n        public static readonly IReadOnlyList<HeroFrame> HomeFrames = new[] { '+outputs.flatMap((f,i)=>f.view==='home'?['Frames['+i+']']:[]).join(', ')+' };\n        private static readonly IReadOnlyDictionary<string, ItemSet> Items = new Dictionary<string, ItemSet>\n        {\n';
for(const set of sets){const title=set.family[0].toUpperCase()+set.family.slice(1),scarf=set.family==='bloom'?'BloomV2Art.ScarfIds':'new[] { '+title+'V2Art.ScarfId }';cs+='            ["'+set.id+'"] = new ItemSet(Family.'+title+', '+title+'V2Art.HeadIds, '+title+'V2Art.ClothesIds, '+scarf+', new[] { '+set.front.join(', ')+' }, new[] { '+set.home.join(', ')+' }, '+set.icon+'),\n';}
cs+='        };\n    }\n}\n';fs.writeFileSync(path.join(repo,data),cs);
fs.copyFileSync(path.join(repo,build,'build-report.json'),path.join(repo,sourceRoot,'build-report.json'));pin(sourceRoot+'/build-report.json');
const record={version:1,sources:[...sources].map(([path,sha256])=>({path,sha256})),data,dataSha256:sha(Buffer.from(cs)),cell:[448,504],paddingTop:96,sets,outputs};fs.writeFileSync(path.join(here,'eyewear-v1.json'),JSON.stringify(record,null,2)+'\n');
const manifestFile=path.join(outputDir,'manifest.json'),manifest=JSON.parse(fs.readFileSync(manifestFile));
const old=manifest.files.filter(f=>f.source==='owner-eyewear');for(const f of old)if(!outputs.some(o=>o.file===f.path))fs.unlinkSync(path.join(outputDir,f.path));
manifest.files=manifest.files.filter(f=>f.source!=='owner-eyewear');for(const f of outputs)manifest.files.push({path:f.file,width:f.crop[2],height:f.crop[3],sha256:f.sha256,slot:'char.hero3d.motion.'+f.family,source:'owner-eyewear',record:'tools/heroanim/models/eyewear-v1/README.md'});fs.writeFileSync(manifestFile,JSON.stringify(manifest,null,1)+'\n');
console.log('Eyewear:',outputs.length,'unique sprites,',outputs.reduce((n,f)=>n+f.bytes,0),'bytes');

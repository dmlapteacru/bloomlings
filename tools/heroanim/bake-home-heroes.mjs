// Home-only model views. Reuse the approved geometry, materials, cell and outfit recipes;
// rotate every fitted part together, depth-mask in that view, and store identical layers once.
import fs from 'node:fs';
import path from 'node:path';
import http from 'node:http';
import {fileURLToPath} from 'node:url';
import {createHash} from 'node:crypto';
import {chromium} from 'playwright-core';
import {PNG} from 'pngjs';
const here=path.dirname(fileURLToPath(import.meta.url)),repo=path.resolve(here,'../..');
const read=f=>JSON.parse(fs.readFileSync(path.join(repo,f))),sha=b=>createHash('sha256').update(b).digest('hex');
const look=read('tools/heroanim/home-heroes-look.json'),review=process.argv.includes('--review');
const dir=path.join(repo,review?'content/work/home-heroes/review':'client/Assets/Bloomlings/Art/Heroes/Resources/HeroMotion');fs.mkdirSync(dir,{recursive:true});
const families=['sprig','bloom','drop','twig'],outputs=[],mappings=[],sources=new Map(),models=[];
function pin(file){sources.set(file,sha(fs.readFileSync(path.join(repo,file))));}
pin('tools/heroanim/bake-home-heroes.mjs');pin('tools/heroanim/home-heroes-look.json');
// Keep the alpha-safe linear-light reduction identical to the frontal bakes.
const linear=Float64Array.from({length:256},(_,i)=>{const c=i/255;return c<=.04045?c/12.92:((c+.055)/1.055)**2.4;});
function reduce(source){
 const s=3,png=new PNG({width:source.width/s,height:source.height/s});
 const encode=c=>Math.round(255*(c<=.0031308?12.92*c:1.055*c**(1/2.4)-.055));
 for(let y=0;y<png.height;y++)for(let x=0;x<png.width;x++){
  const sum=[0,0,0];let alpha=0;
  for(let dy=0;dy<s;dy++)for(let dx=0;dx<s;dx++){const i=((y*s+dy)*source.width+x*s+dx)*4,a=source.data[i+3]/255;alpha+=a;for(let c=0;c<3;c++)sum[c]+=linear[source.data[i+c]]*a;}
  const i=(y*png.width+x)*4;png.data[i+3]=Math.round(alpha*255/(s*s));for(let c=0;c<3;c++)png.data[i+c]=alpha?encode(sum[c]/alpha):0;
 }return png;
}
function cropped(raw){
 const png=reduce(PNG.sync.read(Buffer.from(raw,'base64')));let l=png.width,t=png.height,r=-1,b=-1;
 for(let y=0;y<png.height;y++)for(let x=0;x<png.width;x++)if(png.data[(y*png.width+x)*4+3]>2){l=Math.min(l,x);t=Math.min(t,y);r=Math.max(r,x);b=Math.max(b,y);}
 if(r<l||l===0||t===0||r===png.width-1||b===png.height-1)throw Error('Empty / clipped Home layer');
 l--;t--;r++;b++;const w=r-l+1,h=b-t+1,crop=new PNG({width:w,height:h});PNG.bitblt(png,crop,l,t,w,h,0,0);
 return {bytes:PNG.sync.write(crop,{colorType:6}),crop:[l,t-96,w,h]};
}
function recipe(key,meta){
 const [kind,...bits]=key.split('-'),name=(v,choices)=>choices.find(k=>k.toLowerCase()===v)??'none';
 let head='none',clothes='none',scarf='none',layer;
 if(kind==='base'){head=name(bits[0],meta.heads);layer='base';}
 else if(kind==='clothes'){head=bits.length>1?name(bits[0],meta.heads):'none';clothes=name(bits.at(-1),meta.clothes);layer=clothes;}
 else if(kind==='scarf'){
  head=bits.length>1?name(bits[0],meta.heads):'none';clothes=name(bits.length>1?bits[1]:bits[0],meta.clothes);
  scarf=meta.scarves?name(bits[2],meta.scarves):'Scarf';layer=scarf;
 }else if(kind==='glasses'){
  head=name(bits[0],meta.heads);clothes=name(bits[1],meta.clothes);
  scarf=meta.scarves?name(bits[2],meta.scarves):bits[2]==='scarf'?'Scarf':'none';layer='Glasses';
 }else throw Error('Unexpected outfit layer '+key);
 return {head,clothes,scarf,layer};
}
let browser;
try{
 browser=await chromium.launch({executablePath:'/usr/bin/chromium',args:['--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist']});
 for(const family of families){
  const title=family[0].toUpperCase()+family.slice(1),metaFile='tools/heroanim/'+family+'-v2.json',lookFile='tools/heroanim/'+family+'-v2-look.json';
  const meta=read(metaFile),materials=read(lookFile);pin(metaFile);pin(lookFile);
  const build='content/work/'+family+'-v2/'+(family==='drop'?'game':'build'),report=read(build+'/build-report.json');
  const model=path.join(repo,build,report.export.file),prefix=family==='drop'?'DropHeroV2_':title+'V2_';
  if(sha(fs.readFileSync(model))!==report.export.sha256)throw Error('Rebuild '+family+' GLB first');
  // Pin the tracked preparation proof, whose sources are independently checked by check.mjs.
  pin('tools/heroanim/models/'+family+'-v2/'+(family==='drop'?'game-build.json':'build-report.json'));
  models.push({family,sha256:report.export.sha256,yawDegrees:look.yawDegrees[family]});
  const html=`<script type="importmap">{"imports":{"three":"/three/build/three.module.js"}}</script><script type="module">
import * as T from 'three';import{GLTFLoader}from'/three/examples/jsm/loaders/GLTFLoader.js';
const renderer=new T.WebGLRenderer({antialias:true,alpha:true,preserveDrawingBuffer:true});renderer.setPixelRatio(1);renderer.outputColorSpace=T.SRGBColorSpace;renderer.setClearColor(0,0);renderer.toneMapping=T.NoToneMapping;
const scene=new T.Scene();scene.add(new T.HemisphereLight(0xffffff,0xffffff,2));
// Rotate the studio with the subject: keep the approved bright finish independent of its Home yaw.
const studio=new T.Group();scene.add(studio);studio.rotation.y=${look.yawDegrees[family]}*Math.PI/180;
for(const[p,k]of[[[-3,4,5],1],[[3,2,4],.4]]){const l=new T.DirectionalLight(0xffffff,k);l.position.set(...p);studio.add(l);}
const gltf=await new GLTFLoader().loadAsync('/model.glb');scene.add(gltf.scene);gltf.scene.rotation.y=${look.yawDegrees[family]}*Math.PI/180;
const parts={},original=new Map(),prefix=${JSON.stringify(prefix)},look=${JSON.stringify(materials)};
${family==='drop'?`parts.base=new T.Group();gltf.scene.add(parts.base);for(const key of ['Body','Neck','Arm_L','Arm_R','Leg_L','Leg_R','Tail','Head']){const o=gltf.scene.getObjectByName(prefix+'Base_'+key);if(!o)throw Error('Missing base '+key);parts.base.add(o);}`:`parts.base=gltf.scene.getObjectByName(prefix+'base');`}
for(const key of ${JSON.stringify(meta.icons)}){parts[key]=gltf.scene.getObjectByName(prefix+key);if(!parts[key])throw Error('Missing '+key);}
gltf.scene.traverse(o=>{if(o.isMesh){o.frustumCulled=false;let part=o;while(part.parent&&!part.name.startsWith(prefix))part=part.parent;
 const mats=(Array.isArray(o.material)?o.material:[o.material]).map(m=>{
  const override=part.name===prefix+'Glasses'?(${family==='drop'?'look.glasses':'look.glasses[m.name]'}):null;
  const color=override?new T.Color(override.color):m.color.clone(),opacity=override?override.opacity:m.opacity;
  const n=new T.MeshLambertMaterial({map:m.map,color:color.clone().multiplyScalar(.25),emissiveMap:m.map,emissive:color.clone().multiplyScalar(.75),side:T.DoubleSide,transparent:opacity<1,opacity,depthWrite:opacity>=1});
  if(look.panama&&part.name===prefix+'BucketHat'&&m.name.startsWith(look.panama.materialPrefix)){
   const warm='vec3 warmPanama(vec3 c){float mask=1.0-smoothstep(.35,.60,c.b/max(max(c.r,c.g),.0001));float value=dot(c,vec3(.2126,.7152,.0722));return mix(c,vec3('+look.panama.linearHue.join(',')+')*value*'+look.panama.brightness.toFixed(3)+',mask);}';
   n.onBeforeCompile=s=>{s.fragmentShader=warm+'\\n'+s.fragmentShader;s.fragmentShader=s.fragmentShader.replace('#include <map_fragment>','#include <map_fragment>\\ndiffuseColor.rgb=warmPanama(diffuseColor.rgb);').replace('#include <emissivemap_fragment>','#include <emissivemap_fragment>\\ntotalEmissiveRadiance=warmPanama(totalEmissiveRadiance);');};n.customProgramCacheKey=()=>warm;
  }return n;
 });o.material=Array.isArray(o.material)?mats:mats[0];original.set(o,o.material);
}});
const scale=${family==='bloom'?'.94':'1'},top=(1.296+96/350)/scale,bottom=-.144/scale,center=(top+bottom)/2;
const camera=new T.OrthographicCamera(-.64/scale,.64/scale,(top-bottom)/2,-(top-bottom)/2,.01,30);camera.position.set(0,center,5);camera.lookAt(0,center,0);renderer.setSize(448*3,600*3);
const depth=new T.MeshBasicMaterial({colorWrite:false,depthWrite:true,side:T.DoubleSide}),cache=new Map();
window.render=({head,clothes,scarf,layer,combined=false})=>{
 for(const obj of Object.values(parts)){obj.visible=false;obj.traverse(m=>{if(m.isMesh){m.material=original.get(m);m.renderOrder=0;}});}
 ${family==='drop'?`gltf.scene.getObjectByName(prefix+'Base_Head').visible=head!=='NativeDrop';`:''}
 for(const key of ['Scarf','ScarfKnit','ScarfStripe'])parts[key]?.traverse(o=>{if(o.isMesh&&o.morphTargetInfluences){o.morphTargetInfluences.fill(0);if(clothes!=='none'){const i=o.morphTargetDictionary['Over_'+clothes];if(i!==undefined)o.morphTargetInfluences[i]=1;}}});
 const selected=['base',...(head==='none'?[]:[head]),...(layer==='base'?[]:clothes==='none'?[]:[clothes]),...((layer==='Glasses'||layer.startsWith('Scarf'))&&scarf!=='none'?[scarf]:[]),...(layer==='Glasses'?['Glasses']:[])];
 if(combined){if(clothes!=='none')selected.push(clothes);if(scarf!=='none')selected.push(scarf);selected.push('Glasses');}
 for(const key of selected)parts[key].visible=true;
 gltf.scene.updateMatrixWorld(true);
 let cacheKey=null;
 if(layer!=='base'&&!combined){
  const target=parts[layer],box=new T.Box3().setFromObject(target);
  const masks=[...new Set(selected)].filter(k=>k!==layer).filter(k=>{const b=new T.Box3().setFromObject(parts[k]);return b.max.x>=box.min.x&&b.min.x<=box.max.x&&b.max.y>=box.min.y&&b.min.y<=box.max.y;});
  cacheKey=layer+'|'+(layer.startsWith('Scarf')?clothes:'')+'|'+masks.map(k=>k==='base'?'base:'+${family==='drop'?`(head==='NativeDrop'?'native':'round')`:`'shared'`}:k).sort().join(',');
  if(cache.has(cacheKey))return cache.get(cacheKey);
  for(const key of selected)if(key!==layer)parts[key].traverse(m=>{if(m.isMesh){m.material=depth;m.renderOrder=-10;}});
 }
 renderer.render(scene,camera);const raw=renderer.domElement.toDataURL().split(',')[1];if(cacheKey)cache.set(cacheKey,raw);return raw;
};window.ready=true;</script>`;
  const three=path.join(here,'node_modules/three');
  const server=http.createServer((req,res)=>{const u=new URL(req.url,'http://x').pathname;if(u==='/'){res.end(html);return;}const f=u==='/model.glb'?model:u.startsWith('/three/')?path.resolve(three,u.slice(7)):null;if(!f||(f!==model&&!f.startsWith(three+path.sep))||!fs.existsSync(f)){res.writeHead(404);res.end();return;}res.setHeader('Content-Type',f.endsWith('.js')?'text/javascript':'application/octet-stream');fs.createReadStream(f).pipe(res);});
  await new Promise(r=>server.listen(0,'127.0.0.1',r));const page=await browser.newPage();
  try{
   page.on('pageerror',e=>console.error(e));await page.goto('http://127.0.0.1:'+server.address().port);await page.waitForFunction(()=>window.ready,{},{timeout:120000});
   const seen=new Map(),reduced=new Map();let count=0;
   for(const f of meta.outputs.filter(f=>!f.icon)){
    if(review&&!['base-hairleafy','base-hairpetals','base-nativedrop','base-pumpkinhat'].includes(f.key))continue;
    const args=recipe(f.key,meta),raw=await page.evaluate(o=>window.render(o),args);let result=reduced.get(raw);
    if(!result){result=cropped(raw);reduced.set(raw,result);}
    const hash=sha(result.bytes),identity=hash+'|'+result.crop.join(',');let output=seen.get(identity);
    if(!output){const file='homehero-'+family+'-'+String(seen.size).padStart(3,'0')+'.png';output={family,file,crop:result.crop,bytes:result.bytes.length,sha256:hash};seen.set(identity,output);outputs.push(output);fs.writeFileSync(path.join(dir,file),result.bytes);}
    mappings.push({front:f.file.slice(0,-4),home:output.file.slice(0,-4)});count++;
   }
   console.log(family+': '+seen.size+' unique Home layers / '+count+' recipes');
  }finally{await page.close();await new Promise(r=>server.close(r));}
 }
}finally{if(browser)await browser.close();}
if(review){fs.writeFileSync(path.join(dir,'review.json'),JSON.stringify({models,outputs,mappings},null,2)+'\n');process.exit(0);}
const data='client/Assets/Bloomlings/UI/Design/HomeHeroData.cs';
let cs='// Generated by tools/heroanim/bake-home-heroes.mjs.\nusing System.Collections.Generic;\nnamespace Bloomlings.Client.UI.Design\n{\n    public static partial class HomeHeroArt\n    {\n        private static readonly HeroFrame[] Frames = new[]\n        {\n';
for(const f of outputs)cs+='            new HeroFrame("'+f.file.slice(0,-4)+'", '+f.crop.join(', ')+', .5f, .68f, .5f, .35f),\n';
cs+='        };\n        private static readonly Dictionary<string, int> FrontToHome = new Dictionary<string, int>\n        {\n';
for(const m of mappings)cs+='            { "'+m.front+'", '+outputs.findIndex(f=>f.file.slice(0,-4)===m.home)+' },\n';
cs+='        };\n    }\n}\n';fs.writeFileSync(path.join(repo,data),cs);
const record={version:1,sources:[...sources].map(([path,sha256])=>({path,sha256})),models,data,dataSha256:sha(Buffer.from(cs)),cell:[448,504],paddingTop:96,encoding:'RGBA8 lossless PNG; identical layers stored once',outputs,mappings};
fs.writeFileSync(path.join(here,'home-heroes.json'),JSON.stringify(record,null,2)+'\n');
const manifestFile=path.join(dir,'manifest.json'),manifest=JSON.parse(fs.readFileSync(manifestFile));
manifest.files=manifest.files.filter(f=>!f.path.startsWith('homehero-'));
for(const f of outputs)manifest.files.push({path:f.file,width:f.crop[2],height:f.crop[3],sha256:f.sha256,slot:'char.hero3d.motion.'+f.family,source:'owner',record:'tools/heroanim/SOURCE.md'});
for(const f of fs.readdirSync(dir))if(f.startsWith('homehero-')&&f.endsWith('.png')&&!outputs.some(o=>o.file===f))fs.unlinkSync(path.join(dir,f));
fs.writeFileSync(manifestFile,JSON.stringify(manifest,null,1)+'\n');
console.log('Home: '+outputs.length+' PNGs, '+outputs.reduce((n,f)=>n+f.bytes,0)+' bytes for '+mappings.length+' frontal-layer equivalents');

// Owner-aligned Bloom V2: shared bases, outfit-aware masks, two scarf alternatives and Wardrobe icons.
import fs from 'node:fs';
import path from 'node:path';
import http from 'node:http';
import {fileURLToPath} from 'node:url';
import {createHash} from 'node:crypto';
import {chromium} from 'playwright-core';
import {PNG} from 'pngjs';
const here=path.dirname(fileURLToPath(import.meta.url)),repo=path.resolve(here,'../..');
const build=path.join(repo,'content/work/bloom-v2/build'),report=JSON.parse(fs.readFileSync(path.join(build,'build-report.json')));
const model=path.join(build,report.export.file),three=path.join(here,'node_modules/three');
const sha=b=>createHash('sha256').update(b).digest('hex');
if(sha(fs.readFileSync(model))!==report.export.sha256||sha(fs.readFileSync(path.join(here,'prepare-bloom-v2.py')))!==report.scriptSha256)throw Error('Rebuild Bloom V2 first');
const review=process.argv.includes('--review'),reviewDir=path.join(repo,'content/work/bloom-v2/review');fs.mkdirSync(reviewDir,{recursive:true});
const dir=review?reviewDir:path.join(repo,'client/Assets/Bloomlings/Art/Heroes/Resources/HeroMotion');
const look=JSON.parse(fs.readFileSync(path.join(here,'bloom-v2-look.json')));
const heads=['none','HairPetals','HairBob','HairPeach','HairRose','BucketHat','Cap','Beret','EarmuffHair','XmasHat'];
const clothes=['none','Jacket','Coat','Overalls'];
const scarves=['none','ScarfKnit','ScarfStripe'];
const icons=[...heads.slice(1),'Glasses',...scarves.slice(1),...clothes.slice(1)];
// Bloom masks are conditioned on headwear, clothing and scarf to preserve intersecting parts.
const html=`<script type="importmap">{"imports":{"three":"/three/build/three.module.js"}}</script><script type="module">
import * as T from 'three';import{GLTFLoader}from'/three/examples/jsm/loaders/GLTFLoader.js';
const renderer=new T.WebGLRenderer({antialias:true,alpha:true,preserveDrawingBuffer:true});renderer.setPixelRatio(1);renderer.outputColorSpace=T.SRGBColorSpace;renderer.setClearColor(0,0);renderer.toneMapping=T.NoToneMapping;
const scene=new T.Scene();scene.add(new T.HemisphereLight(0xffffff,0xffffff,2));for(const[p,k]of[[[-3,4,5],1],[[3,2,4],.4]]){const l=new T.DirectionalLight(0xffffff,k);l.position.set(...p);scene.add(l);}
const gltf=await new GLTFLoader().loadAsync('/model.glb');scene.add(gltf.scene);
const parts={},original=new Map();for(const key of ['base',...${JSON.stringify(icons)}]){parts[key]=gltf.scene.getObjectByName('BloomV2_'+key);if(!parts[key])throw Error('Missing '+key);}
const look=${JSON.stringify(look)};
gltf.scene.traverse(o=>{if(o.isMesh){
 o.frustumCulled=false;let part=o;while(part.parent&&!part.name.startsWith('BloomV2_'))part=part.parent;
 const mats=(Array.isArray(o.material)?o.material:[o.material]).map(m=>{
  const override=part.name==='BloomV2_Glasses'?look.glasses[m.name]:null;
  const color=override?new T.Color(override.color):m.color.clone(),opacity=override?override.opacity:m.opacity;
  const n=new T.MeshLambertMaterial({map:m.map,color:color.clone().multiplyScalar(.25),emissiveMap:m.map,emissive:color.clone().multiplyScalar(.75),side:T.DoubleSide,transparent:opacity<1,opacity,depthWrite:opacity>=1});
  return n;
 });o.material=Array.isArray(o.material)?mats:mats[0];original.set(o,o.material);
}});
const depth=new T.MeshBasicMaterial({colorWrite:false,depthWrite:true,side:T.DoubleSide});
const camera=new T.OrthographicCamera(-.64,.64,1,0,.01,30);
window.render=({head='none',clothes='none',layer='base',icon=false,combined=false,glasses=false,scarf='none'})=>{
 for(const[key,obj]of Object.entries(parts)){obj.visible=false;obj.traverse(m=>{if(m.isMesh){m.material=original.get(m);m.renderOrder=0;}});}
 const show=key=>{parts[key].visible=true;};
 for(const key of ['ScarfKnit','ScarfStripe'])parts[key].traverse(o=>{if(o.isMesh&&o.morphTargetInfluences){o.morphTargetInfluences.fill(0);if(clothes!=='none'){const i=o.morphTargetDictionary['Over_'+clothes];if(i!==undefined)o.morphTargetInfluences[i]=1;}}});
 if(icon){show(layer);const box=new T.Box3().setFromObject(parts[layer]),center=box.getCenter(new T.Vector3()),size=box.getSize(new T.Vector3()),half=Math.max(size.x,size.y)*.70;camera.left=-half;camera.right=half;camera.top=half;camera.bottom=-half;camera.position.set(center.x+(layer==='Glasses'?.6:1.1),center.y+.6,center.z+5);camera.lookAt(center);renderer.setSize(320*3,320*3);}
 else{
  show('base');
  if(head!=='none')show(head);
  if(clothes!=='none'&&(layer!=='base'||combined))show(clothes);
  if(scarf!=='none'&&(layer==='scarf'||layer==='Glasses'||combined))show(scarf);
  if(layer==='Glasses'||(combined&&glasses))show('Glasses');
  const occluders=[parts.base,...(head==='none'?[]:[parts[head]]),
   ...((layer==='scarf'||layer==='Glasses')&&clothes!=='none'?[parts[clothes]]:[]),
   ...(layer==='Glasses'&&scarf!=='none'?[parts[scarf]]:[])];
  if(layer!=='base'&&!combined)for(const obj of occluders)obj.traverse(m=>{if(m.isMesh){m.material=depth;m.renderOrder=-10;}});
  // Bloom's stock petal halo is taller than Sprig. Fit every part uniformly, preserving the shared foot line.
  const scale=.94,top=(1.296+96/350)/scale,bottom=-.144/scale,center=(top+bottom)/2;
  camera.left=-.64/scale;camera.right=.64/scale;camera.top=(top-bottom)/2;camera.bottom=-(top-bottom)/2;camera.position.set(0,center,5);camera.lookAt(0,center,0);renderer.setSize(448*3,600*3);
 }
 gltf.scene.updateMatrixWorld(true);camera.updateProjectionMatrix();renderer.render(scene,camera);return renderer.domElement.toDataURL().split(',')[1];
};window.ready=true;</script>`;
const server=http.createServer((req,res)=>{const u=new URL(req.url,'http://x').pathname;if(u==='/'){res.end(html);return;}const f=u==='/model.glb'?model:u.startsWith('/three/')?path.resolve(three,u.slice(7)):null;if(!f||(f!==model&&!f.startsWith(three+path.sep))||!fs.existsSync(f)){res.writeHead(404);res.end();return;}res.setHeader('Content-Type',f.endsWith('.js')?'text/javascript':'application/octet-stream');fs.createReadStream(f).pipe(res);});
await new Promise(r=>server.listen(0,'127.0.0.1',r));let browser;const images=[];
try{
 browser=await chromium.launch({executablePath:'/usr/bin/chromium',args:['--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist']});
 const page=await browser.newPage();page.on('pageerror',e=>{throw e;});await page.goto('http://127.0.0.1:'+server.address().port);await page.waitForFunction(()=>window.ready,{},{timeout:120000});
 const recipes=[...heads.map(head=>({head,layer:'base',key:'base-'+head.toLowerCase()})),
  ...heads.flatMap(head=>clothes.slice(1).map(clothes=>({head,clothes,layer:'clothes',key:'clothes-'+head.toLowerCase()+'-'+clothes.toLowerCase()}))),
  ...heads.flatMap(head=>clothes.flatMap(clothes=>scarves.slice(1).map(scarf=>({head,clothes,scarf,layer:'scarf',key:'scarf-'+head.toLowerCase()+'-'+clothes.toLowerCase()+'-'+scarf.toLowerCase()})))),
  ...heads.flatMap(head=>clothes.flatMap(clothes=>scarves.map(scarf=>({head,clothes,scarf,layer:'Glasses',key:'glasses-'+head.toLowerCase()+'-'+clothes.toLowerCase()+'-'+scarf.toLowerCase()}))))];
 for(const recipe of recipes){const raw=await page.evaluate(o=>window.render(o),recipe);images.push({key:recipe.key,png:reduce(PNG.sync.read(Buffer.from(raw,'base64')),3),icon:false});}
 for(const layer of icons){const raw=await page.evaluate(o=>window.render(o),{layer,icon:true});images.push({key:'icon-'+layer.toLowerCase(),png:reduce(PNG.sync.read(Buffer.from(raw,'base64')),3),icon:true});}
 if(review)for(const head of heads)for(const scarf of scarves){const raw=await page.evaluate(o=>window.render(o),{head,clothes:'Jacket',scarf,glasses:true,combined:true});fs.writeFileSync(path.join(reviewDir,'combined-'+head.toLowerCase()+'-'+scarf.toLowerCase()+'.png'),Buffer.from(raw,'base64'));}
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
if(!review)for(const file of fs.readdirSync(dir))if(file.startsWith('bloomv2-')&&file.endsWith('.png')&&!images.some(i=>'bloomv2-'+i.key+'.png'===file))fs.unlinkSync(path.join(dir,file));
for(const{key,png,icon}of images){
 let l=png.width,t=png.height,r=-1,b=-1;for(let y=0;y<png.height;y++)for(let x=0;x<png.width;x++)if(png.data[(y*png.width+x)*4+3]>2){l=Math.min(l,x);t=Math.min(t,y);r=Math.max(r,x);b=Math.max(b,y);}
 if(r<l||l===0||t===0||r===png.width-1||b===png.height-1)throw Error('Empty / clipped '+key);
 l--;t--;r++;b++;const w=r-l+1,h=b-t+1,crop=new PNG({width:w,height:h});PNG.bitblt(png,crop,l,t,w,h,0,0);
 const bytes=PNG.sync.write(crop,{colorType:6}),file='bloomv2-'+key+'.png';fs.writeFileSync(path.join(dir,file),bytes);
 outputs.push({key,file,crop:[l,t-(icon?0:96),w,h],icon,bytes:bytes.length,sha256:sha(bytes)});
}
if(review){fs.writeFileSync(path.join(reviewDir,'review.json'),JSON.stringify({outputs},null,2));console.log('V2 review:',outputs.length,'sprites');process.exit(0);}
const dataFile=path.join(repo,'client/Assets/Bloomlings/UI/Design/BloomV2Data.cs');
let cs='// Generated by tools/heroanim/bake-bloom-v2.mjs.\nnamespace Bloomlings.Client.UI.Design\n{\n    public static partial class BloomV2Art\n    {\n        private static readonly HeroFrame[] Frames = new[]\n        {\n';
for(const f of outputs)cs+='            new HeroFrame("'+f.file.slice(0,-4)+'", '+f.crop.join(', ')+', .5f, .68f, .5f, .35f),\n';cs+='        };\n    }\n}\n';fs.writeFileSync(dataFile,cs);
const sourceFiles=[...report.archives.map(a=>'tools/heroanim/models/bloom-v2/'+a.path),'tools/heroanim/models/bloom-v2/source-manifest.json','tools/heroanim/prepare-bloom-v2.py','tools/heroanim/bake-bloom-v2.mjs','tools/heroanim/bloom-v2-look.json',...report.additions.map(a=>'tools/heroanim/models/bloom-v2/'+a.path)];
const sources=sourceFiles.map(file=>({path:file,sha256:sha(fs.readFileSync(path.join(repo,file)))}));
fs.writeFileSync(path.join(here,'bloom-v2.json'),JSON.stringify({version:1,sources,data:path.relative(repo,dataFile),dataSha256:sha(Buffer.from(cs)),cell:[448,504],paddingTop:96,encoding:'RGBA8 lossless PNG',heads,clothes,scarves,icons,outputs},null,2)+'\n');
fs.writeFileSync(path.join(here,'models/bloom-v2/build-report.json'),JSON.stringify(report,null,2)+'\n');
const file=path.join(dir,'manifest.json'),manifest=JSON.parse(fs.readFileSync(file));manifest.files=manifest.files.filter(f=>!f.path.startsWith('bloomv2-'));for(const f of outputs)manifest.files.push({path:f.file,width:f.crop[2],height:f.crop[3],sha256:f.sha256,slot:'char.hero3d.motion.bloom',source:'owner',record:'tools/heroanim/SOURCE.md'});fs.writeFileSync(file,JSON.stringify(manifest,null,1)+'\n');
console.log('Baked',outputs.length,'V2 sprites,',outputs.reduce((n,f)=>n+f.bytes,0),'bytes');

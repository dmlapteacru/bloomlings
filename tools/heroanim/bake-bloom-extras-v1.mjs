// Additional fitted Bloom items. Existing accepted hero and eyewear bakes stay unchanged.
import fs from 'node:fs';import path from 'node:path';import http from 'node:http';
import {fileURLToPath}from'node:url';import{createHash}from'node:crypto';
import{chromium}from'playwright-core';import{PNG}from'pngjs';
const here=path.dirname(fileURLToPath(import.meta.url)),repo=path.resolve(here,'../..');
const read=f=>JSON.parse(fs.readFileSync(path.join(repo,f))),sha=b=>createHash('sha256').update(b).digest('hex');
const sourceRoot='tools/heroanim/models/bloom-extras-v1',build='content/work/bloom-extras-v1/build';
const fit=read(sourceRoot+'/fit.json'),proof=read(build+'/build-report.json'),meta=read('tools/heroanim/bloom-v2.json');
if(proof.fitSha256!==sha(fs.readFileSync(path.join(repo,sourceRoot,'fit.json')))||proof.scriptSha256!==sha(fs.readFileSync(path.join(here,'prepare-bloom-extras-v1.py'))))throw Error('Prepare Bloom extras first');
const extraHeads=fit.items.filter(i=>i.slot==='Head'),extraBodies=fit.items.filter(i=>i.slot==='Body');
const heads=[...meta.heads.slice(1),...extraHeads.map(i=>i.key)],bodies=[...meta.clothes,...extraBodies.map(i=>i.key)],scarves=meta.scarves;
const headIds=['hat.bloom_petals','hat.bloom_bob','hat.bloom_peach','hat.bloom_rose','hat.bloom_panama','hat.bloom_cap','hat.bloom_beret','hat.bloom_earmuffs','hat.bloom_xmas',...extraHeads.map(i=>i.id)];
const bodyIds=['','clothes.bloom_jacket','clothes.bloom_coat','clothes.bloom_overalls',...extraBodies.map(i=>i.id)],scarfIds=['','scarf.bloom_knit','scarf.bloom_stripe'];
const sources=new Map(),outputs=[],seen=new Map(),front=[],home=[];
const pin=f=>sources.set(f,sha(fs.readFileSync(path.join(repo,f))));
for(const f of [sourceRoot+'/fit.json','tools/heroanim/prepare-bloom-extras-v1.py','tools/heroanim/bake-bloom-extras-v1.mjs','tools/heroanim/models/bloom-v2/build-report.json','tools/heroanim/bloom-v2.json','tools/heroanim/bloom-v2-look.json','tools/heroanim/home-heroes-look.json','tools/heroanim/models/eyewear-v1/build-report.json',...fit.items.map(i=>sourceRoot+'/'+i.source)])pin(f);
const models=new Map([['/model.glb',path.join(repo,build,proof.export.file)]]);
if(sha(fs.readFileSync(models.get('/model.glb')))!==proof.export.sha256)throw Error('Stale fitted GLB');
const eyes=read('tools/heroanim/models/eyewear-v1/build-report.json');
for(const [key,id]of [['Round','glasses.bloom_round'],['Heart','glasses.bloom_heart']]){const p=eyes.variants.find(v=>v.id===id),f=path.join(repo,'content/work/eyewear-v1/build',p.file);if(sha(fs.readFileSync(f))!==p.sha256)throw Error('Prepare eyewear first');models.set('/'+key+'.glb',f);}
const dir=path.join(repo,'client/Assets/Bloomlings/Art/Heroes/Resources/HeroMotion');
const linear=Float64Array.from({length:256},(_,i)=>{const c=i/255;return c<=.04045?c/12.92:((c+.055)/1.055)**2.4;});
function publish(raw,view){
 const s=PNG.sync.read(Buffer.from(raw,'base64')),p=new PNG({width:s.width/3,height:s.height/3});
 const encode=c=>Math.round(255*(c<=.0031308?12.92*c:1.055*c**(1/2.4)-.055));
 for(let y=0;y<p.height;y++)for(let x=0;x<p.width;x++){const sum=[0,0,0];let alpha=0;
  for(let dy=0;dy<3;dy++)for(let dx=0;dx<3;dx++){const i=((y*3+dy)*s.width+x*3+dx)*4,a=s.data[i+3]/255;alpha+=a;for(let c=0;c<3;c++)sum[c]+=linear[s.data[i+c]]*a;}
  const i=(y*p.width+x)*4;p.data[i+3]=Math.round(alpha*255/9);for(let c=0;c<3;c++)p.data[i+c]=alpha?encode(sum[c]/alpha):0;
 }
 let l=p.width,t=p.height,r=-1,b=-1;for(let y=0;y<p.height;y++)for(let x=0;x<p.width;x++)if(p.data[(y*p.width+x)*4+3]>2){l=Math.min(l,x);t=Math.min(t,y);r=Math.max(r,x);b=Math.max(b,y);}
 if(r<l||l===0||t===0||r===p.width-1||b===p.height-1)throw Error('Empty or clipped '+view);
 l--;t--;r++;b++;const crop=[l,t-(view==='icon'?0:96),r-l+1,b-t+1],png=new PNG({width:crop[2],height:crop[3]});PNG.bitblt(p,png,l,t,crop[2],crop[3],0,0);
 const bytes=PNG.sync.write(png,{colorType:6}),hash=sha(bytes),key=view+'|'+hash+'|'+crop.join(',');if(seen.has(key))return seen.get(key);
 const index=outputs.length,file=(view==='home'?'homehero-bloom-extra-':'bloomextra-')+String(index).padStart(3,'0')+'.png';
 fs.writeFileSync(path.join(dir,file),bytes);outputs.push({file,view,crop,bytes:bytes.length,sha256:hash});seen.set(key,index);return index;
}
const html=`<script type="importmap">{"imports":{"three":"/three/build/three.module.js"}}</script><script type="module">
import * as T from'three';import{GLTFLoader}from'/three/examples/jsm/loaders/GLTFLoader.js';
const renderer=new T.WebGLRenderer({antialias:true,alpha:true,preserveDrawingBuffer:true});renderer.setPixelRatio(1);renderer.outputColorSpace=T.SRGBColorSpace;renderer.setClearColor(0,0);renderer.toneMapping=T.NoToneMapping;
const scene=new T.Scene(),subject=new T.Group(),studio=new T.Group();scene.add(subject,studio);scene.add(new T.HemisphereLight(0xffffff,0xffffff,2));for(const[p,k]of[[[-3,4,5],1],[[3,2,4],.4]]){const l=new T.DirectionalLight(0xffffff,k);l.position.set(...p);studio.add(l);}
const loader=new GLTFLoader(),g=await loader.loadAsync('/model.glb');subject.add(g.scene);const parts={},original=new Map();
for(const key of ['base',...${JSON.stringify(meta.icons)}])parts[key]=g.scene.getObjectByName('BloomV2_'+key);
for(const key of ${JSON.stringify(fit.items.map(i=>i.key))})parts[key]=g.scene.getObjectByName('BloomExtra_'+key);
for(const key of ['Round','Heart']){parts[key]=(await loader.loadAsync('/'+key+'.glb')).scene;subject.add(parts[key]);}
for(const [key,o]of Object.entries(parts))if(!o)throw Error('Missing '+key);
subject.traverse(o=>{if(!o.isMesh)return;o.frustumCulled=false;const mats=(Array.isArray(o.material)?o.material:[o.material]).map(m=>new T.MeshLambertMaterial({map:m.map,color:m.color.clone().multiplyScalar(.25),emissiveMap:m.map,emissive:m.color.clone().multiplyScalar(.75),side:T.DoubleSide,transparent:m.opacity<1,opacity:m.opacity,depthWrite:m.opacity>=1}));o.material=Array.isArray(o.material)?mats:mats[0];original.set(o,o.material);});
const depth=new T.MeshBasicMaterial({colorWrite:false,depthWrite:true,side:T.DoubleSide}),camera=new T.OrthographicCamera(-1,1,1,-1,.01,30),size=new T.Vector2(),cache=new Map();
function resize(w,h){renderer.getSize(size);if(size.x!==w||size.y!==h)renderer.setSize(w,h);}
window.draw=({head,body='none',scarf='none',layer,view='front',icon=false})=>{
 for(const o of Object.values(parts)){o.visible=false;o.traverse(m=>{if(m.isMesh){m.material=original.get(m);m.renderOrder=0;}});}
 subject.rotation.y=studio.rotation.y=(view==='home'?${read('tools/heroanim/home-heroes-look.json').yawDegrees.bloom}:0)*Math.PI/180;
 for(const key of ['ScarfKnit','ScarfStripe'])parts[key].traverse(m=>{if(m.isMesh&&m.morphTargetInfluences){m.morphTargetInfluences.fill(0);const name=${JSON.stringify(extraBodies.map(i=>i.key))}.includes(body)?'Over_Overalls':'Over_'+body;const i=m.morphTargetDictionary[name];if(i!==undefined)m.morphTargetInfluences[i]=1;}});
 const selected=icon?[layer]:['base',head,...(layer!=='base'&&body!=='none'?[body]:[]),...((layer==='scarf'||layer==='Round'||layer==='Heart')&&scarf!=='none'?[scarf]:[]),...(['Round','Heart'].includes(layer)?[layer]:[])];
 for(const key of selected)parts[key].visible=true;
 const target=icon?parts[layer]:layer==='clothes'?parts[body]:layer==='scarf'?parts[scarf]:parts[layer];
 if(icon){subject.rotation.y=studio.rotation.y=0;subject.updateMatrixWorld(true);const box=new T.Box3().setFromObject(target),center=box.getCenter(new T.Vector3()),s=box.getSize(new T.Vector3()),half=Math.max(s.x,s.y)*.70;camera.left=-half;camera.right=half;camera.top=half;camera.bottom=-half;camera.position.set(center.x+1.1,center.y+.6,center.z+5);camera.lookAt(center);resize(960,960);}
 else{const top=(1.296+96/350)/.94,bottom=-.144/.94,center=(top+bottom)/2;camera.left=-.64/.94;camera.right=.64/.94;camera.top=(top-bottom)/2;camera.bottom=-(top-bottom)/2;camera.position.set(0,center,5);camera.lookAt(0,center,0);resize(1344,1800);}
 camera.updateProjectionMatrix();camera.updateMatrixWorld();subject.updateMatrixWorld(true);
 const baseKey=view+'|base|'+head;if(!icon&&layer==='base'&&cache.has(baseKey))return cache.get(baseKey);
 if(!icon&&layer!=='base'){
  const box=new T.Box3().setFromObject(target),masks=[...new Set(selected)].filter(k=>parts[k]!==target),overlaps=masks.filter(k=>{const b=new T.Box3().setFromObject(parts[k]);return b.max.x>=box.min.x&&b.min.x<=box.max.x&&b.max.y>=box.min.y&&b.min.y<=box.max.y;});
  const key=view+'|'+target.uuid+'|'+(layer==='scarf'||overlaps.some(k=>k.startsWith('Scarf'))?body:'')+'|'+overlaps.sort().join(',');if(cache.has(key))return cache.get(key);
  for(const k of masks)parts[k].traverse(m=>{if(m.isMesh){m.material=depth;m.renderOrder=-10;}});
  let l=Infinity,r=-Infinity,b=Infinity,t=-Infinity;for(const x of [box.min.x,box.max.x])for(const y of [box.min.y,box.max.y])for(const z of [box.min.z,box.max.z]){const p=new T.Vector3(x,y,z).project(camera);l=Math.min(l,(p.x+1)*672);r=Math.max(r,(p.x+1)*672);b=Math.min(b,(p.y+1)*900);t=Math.max(t,(p.y+1)*900);}
  const left=Math.max(0,Math.floor(l)-6),bottom=Math.max(0,Math.floor(b)-6),right=Math.min(1344,Math.ceil(r)+6),top=Math.min(1800,Math.ceil(t)+6);
  renderer.setScissorTest(false);renderer.clear();renderer.setScissor(left,bottom,right-left,top-bottom);renderer.setScissorTest(true);renderer.render(scene,camera);renderer.setScissorTest(false);const raw=renderer.domElement.toDataURL().split(',')[1];cache.set(key,raw);return raw;
 }
 renderer.render(scene,camera);const raw=renderer.domElement.toDataURL().split(',')[1];if(!icon&&layer==='base')cache.set(baseKey,raw);return raw;
};window.ready=true;</script>`;
const three=path.join(here,'node_modules/three');const server=http.createServer((req,res)=>{const u=new URL(req.url,'http://x').pathname;if(u==='/'){res.end(html);return;}const file=models.get(u)??(u.startsWith('/three/')?path.resolve(three,u.slice(7)):null);if(!file||(!models.has(u)&&!file.startsWith(three+path.sep))||!fs.existsSync(file)){res.writeHead(404);res.end();return;}res.setHeader('Content-Type',file.endsWith('.js')?'text/javascript':'application/octet-stream');fs.createReadStream(file).pipe(res);});
await new Promise(r=>server.listen(0,'127.0.0.1',r));let browser;const icons={},rawCache=new Map();
try{
 browser=await chromium.launch({executablePath:'/usr/bin/chromium',args:['--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist']});const page=await browser.newPage();page.on('pageerror',e=>console.error(e));await page.goto('http://127.0.0.1:'+server.address().port);await page.waitForFunction(()=>window.ready,{},{timeout:120000});
 async function frame(args){const raw=await page.evaluate(o=>window.draw(o),args),key=args.view+'|'+raw;let result=rawCache.get(key);if(result===undefined){result=publish(raw,args.icon?'icon':args.view);rawCache.set(key,result);}return result;}
 for(const view of ['front','home']){
  const table=view==='front'?front:home;
  for(const head of heads)for(const body of bodies)for(const scarf of scarves){
   const newHead=extraHeads.some(i=>i.key===head),affected=newHead||extraBodies.some(i=>i.key===body);
   if(!affected){table.push(null);continue;}
   const args={head,body,scarf,view};
   table.push([newHead?await frame({...args,layer:'base'}):-1,body==='none'?-1:await frame({...args,layer:'clothes'}),scarf==='none'?-1:await frame({...args,layer:'scarf'}),await frame({...args,layer:'Round'}),await frame({...args,layer:'Heart'})]);
  }
  console.log(view+': '+table.filter(Boolean).length+' fitted outfits');
 }
 for(const item of fit.items)icons[item.id]=await frame({head:heads[0],layer:item.key,view:'icon',icon:true});
}finally{if(browser)await browser.close();server.close();}
const data='client/Assets/Bloomlings/UI/Design/BloomExtrasData.cs';
let cs='// Generated by tools/heroanim/bake-bloom-extras-v1.mjs.\nnamespace Bloomlings.Client.UI.Design\n{\n    public static partial class BloomExtrasArt\n    {\n        private static readonly HeroFrame[] Frames = new[]\n        {\n';
for(const f of outputs)cs+='            new HeroFrame("'+f.file.slice(0,-4)+'", '+f.crop.join(', ')+', .5f, .68f, .5f, .35f),\n';cs+='        };\n';
for(const [name,ids]of [['Heads',headIds],['Bodies',bodyIds],['Scarves',scarfIds],['HeadIds',extraHeads.map(i=>i.id)],['ClothesIds',extraBodies.map(i=>i.id)]])cs+='        '+(name.endsWith('Ids')?'public':'private')+' static readonly string[] '+name+' = new[] { '+ids.map(JSON.stringify).join(', ')+' };\n';
cs+='        public static readonly HeroFrame[] HomeFrames = new[] { '+outputs.flatMap((f,i)=>f.view==='home'?['Frames['+i+']']:[]).join(', ')+' };\n';
for(const [name,table]of [['Front',front],['Home',home]])cs+='        private static readonly Recipe?[] '+name+' = new Recipe?[] {\n'+table.map(row=>'            '+(row?'new Recipe('+row.join(', ')+')':'null')).join(',\n')+'\n        };\n';
cs+='        private static readonly System.Collections.Generic.IReadOnlyDictionary<string, int> Icons = new System.Collections.Generic.Dictionary<string, int>\n        {\n'+Object.entries(icons).map(([id,i])=>'            ["'+id+'"] = '+i).join(',\n')+'\n        };\n    }\n}\n';fs.writeFileSync(path.join(repo,data),cs);
fs.copyFileSync(path.join(repo,build,'build-report.json'),path.join(repo,sourceRoot,'build-report.json'));pin(sourceRoot+'/build-report.json');
const product='tools/heroanim/products/BloomHeroV2_Extras_Fitted.zip';fs.copyFileSync(path.join(repo,build,'BloomHeroV2_Extras_Fitted.zip'),path.join(repo,product));pin(product);
fs.writeFileSync(path.join(here,'bloom-extras-v1.json'),JSON.stringify({version:1,sources:[...sources].map(([path,sha256])=>({path,sha256})),data,dataSha256:sha(Buffer.from(cs)),heads,headIds,bodies,bodyIds,scarves,scarfIds,front,home,icons,outputs},null,2)+'\n');
const mf=path.join(dir,'manifest.json'),manifest=JSON.parse(fs.readFileSync(mf)),old=manifest.files.filter(f=>f.source==='owner-bloom-extras');
for(const f of old)if(!outputs.some(o=>o.file===f.path)&&fs.existsSync(path.join(dir,f.path)))fs.unlinkSync(path.join(dir,f.path));
manifest.files=manifest.files.filter(f=>f.source!=='owner-bloom-extras');for(const f of outputs)manifest.files.push({path:f.file,width:f.crop[2],height:f.crop[3],sha256:f.sha256,slot:'char.hero3d.motion.bloom',source:'owner-bloom-extras',record:sourceRoot+'/README.md'});fs.writeFileSync(mf,JSON.stringify(manifest,null,1)+'\n');
console.log('Bloom extras:',outputs.length,'unique sprites,',outputs.reduce((s,f)=>s+f.bytes,0),'bytes');

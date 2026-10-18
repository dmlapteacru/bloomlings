// Repaired garments for every accepted hero and fitted Drop additions; original skin/head bakes stay intact.
import fs from 'node:fs';import path from 'node:path';import http from 'node:http';
import {fileURLToPath}from'node:url';import{createHash}from'node:crypto';
import{chromium}from'playwright-core';import{PNG}from'pngjs';
const here=path.dirname(fileURLToPath(import.meta.url)),repo=path.resolve(here,'../..');
const read=f=>JSON.parse(fs.readFileSync(path.join(repo,f))),sha=b=>createHash('sha256').update(b).digest('hex');
const sourceRoot='tools/heroanim/models/wardrobe-finish-v1',build='content/work/wardrobe-finish-v1';
const fit=read(sourceRoot+'/fit.json'),sources=new Map(),outputs=[],seen=new Map(),sets=[],icons={};let bakingFamily;
const pin=f=>sources.set(f,sha(fs.readFileSync(path.join(repo,f))));
for(const f of [sourceRoot+'/fit.json','tools/heroanim/models/drop-extras-v1/fit.json','tools/heroanim/garment_cleanup.py','tools/heroanim/prepare-wardrobe-finish-v1.py','tools/heroanim/bake-wardrobe-finish-v1.mjs','tools/heroanim/home-heroes-look.json','tools/heroanim/models/eyewear-v1/build-report.json'])pin(f);
for(const item of fit.items)pin(item.source??item.archive);
for(const item of fit.newItems)pin('tools/heroanim/models/drop-extras-v1/'+item.source);
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
 const bytes=PNG.sync.write(png,{colorType:6}),hash=sha(bytes),key=bakingFamily+'|'+view+'|'+hash+'|'+crop.join(',');if(seen.has(key))return seen.get(key);
 const index=outputs.length,file=(view==='home'?'homehero-'+bakingFamily+'-finish-':'wardrobefinish-'+bakingFamily+'-')+String(index).padStart(3,'0')+'.png';
 fs.writeFileSync(path.join(dir,file),bytes);outputs.push({family:bakingFamily,file,view,crop,bytes:bytes.length,sha256:hash});seen.set(key,index);return index;
}

if(!process.argv.includes('--family'))throw Error('Bake one --family at a time');
const chosen=process.argv.includes('--family')?[process.argv[process.argv.indexOf('--family')+1]]:Object.keys(fit.families);
// Each family is independently prepared/baked so build caches can be discarded on small machines.
for(const family of chosen){
 bakingFamily=family;
 const axis=fit.families[family],proof=read(build+'/'+family+'/build-report.json'),look=read('tools/heroanim/'+family+'-v2-look.json');
 if(proof.fitSha256!==sha(fs.readFileSync(path.join(repo,sourceRoot,'fit.json')))||proof.dropFitSha256!==sha(fs.readFileSync(path.join(here,'models/drop-extras-v1/fit.json')))||proof.scriptSha256!==sha(fs.readFileSync(path.join(here,'prepare-wardrobe-finish-v1.py')))||proof.cleanupSha256!==sha(fs.readFileSync(path.join(here,'garment_cleanup.py'))))throw Error('Prepare repaired garments first');
 const familyItems=[...fit.items.filter(i=>i.family===family),...(family==='drop'?fit.newItems:[])],extraHeads=familyItems.filter(i=>i.slot==='Head'),extraBodies=familyItems.filter(i=>i.slot==='Body');
 const {heads,bodies,scarves,headIds,bodyIds,scarfIds}=axis,front=[],home=[];
 const prefix='Finish_',model=path.join(repo,build,family,proof.export.file),models=new Map([['/model.glb',model]]);
 if(sha(fs.readFileSync(model))!==proof.export.sha256)throw Error('Stale repaired GLB');
 const eyes=read('tools/heroanim/models/eyewear-v1/build-report.json');
 if(family!=='drop')for(const [index,key]of ['EyeA','EyeB'].entries()){const p=eyes.variants.find(v=>v.id===axis.faces[index]),f=path.join(repo,'content/work/eyewear-v1/build',p.file);if(sha(fs.readFileSync(f))!==p.sha256)throw Error('Prepare eyewear first');models.set('/'+key+'.glb',f);}
const html=`<script type="importmap">{"imports":{"three":"/three/build/three.module.js"}}</script><script type="module">
import * as T from'three';import{GLTFLoader}from'/three/examples/jsm/loaders/GLTFLoader.js';
const renderer=new T.WebGLRenderer({antialias:true,alpha:true,preserveDrawingBuffer:true});renderer.setPixelRatio(1);renderer.outputColorSpace=T.SRGBColorSpace;renderer.setClearColor(0,0);renderer.toneMapping=T.NoToneMapping;
const scene=new T.Scene(),subject=new T.Group(),studio=new T.Group();scene.add(subject,studio);scene.add(new T.HemisphereLight(0xffffff,0xffffff,2));for(const[p,k]of[[[-3,4,5],1],[[3,2,4],.4]]){const l=new T.DirectionalLight(0xffffff,k);l.position.set(...p);studio.add(l);}
const loader=new GLTFLoader(),g=await loader.loadAsync('/model.glb');subject.add(g.scene);const parts={},original=new Map(),prefix=${JSON.stringify(prefix)},look=${JSON.stringify(look)};
for(const key of ['base',...${JSON.stringify([...axis.heads,...axis.bodies.filter(k=>k!=='none'),...axis.scarves.filter(k=>k!=='none')])}])parts[key]=g.scene.getObjectByName(prefix+key)??g.scene.getObjectByName('DropHeroV2_'+key);
const legacy=g.scene.getObjectByName(prefix+'Glasses');if(legacy)parts.LegacyGlasses=legacy;
let bald=null;
if(${JSON.stringify(family==='drop')}){
 parts.base=new T.Group();g.scene.add(parts.base);
 for(const key of ['Body','Neck','Head','Arm_L','Arm_R','Leg_L','Leg_R','Tail']){const o=g.scene.getObjectByName('DropHeroV2_Base_'+key);if(!o)throw Error('Missing Drop base '+key);parts.base.add(o);if(key==='Head')bald=o;}
 parts.EyeA=g.scene.getObjectByName('DropHeroV2_Glasses');parts.EyeB=parts.EyeA;
}else for(const key of ['EyeA','EyeB']){parts[key]=(await loader.loadAsync('/'+key+'.glb')).scene;subject.add(parts[key]);}
for(const [key,o]of Object.entries(parts))if(!o)throw Error('Missing '+key);
subject.traverse(o=>{if(!o.isMesh)return;o.frustumCulled=false;let part=o;while(part.parent&&!part.name.startsWith(prefix)&&!part.name.startsWith('Addition_')&&!part.name.startsWith('DropHeroV2_'))part=part.parent;
const mats=(Array.isArray(o.material)?o.material:[o.material]).map(m=>{const n=new T.MeshLambertMaterial({map:m.map,color:m.color.clone().multiplyScalar(.25),emissiveMap:m.map,emissive:m.color.clone().multiplyScalar(.75),side:T.DoubleSide,transparent:m.opacity<1,opacity:m.opacity,depthWrite:m.opacity>=1});
if(${JSON.stringify(family==='drop')}&&part.name==='DropHeroV2_Glasses'){const cream=new T.Color(look.glasses.color);n.color.copy(cream).multiplyScalar(.25);n.emissive.copy(cream).multiplyScalar(.75);}
if(look.panama&&part.name===prefix+'BucketHat'&&m.name.startsWith(look.panama.materialPrefix)){
 const warm='vec3 warmPanama(vec3 c){float mask=1.0-smoothstep(.35,.60,c.b/max(max(c.r,c.g),.0001));float value=dot(c,vec3(.2126,.7152,.0722));return mix(c,vec3('+look.panama.linearHue.join(',')+')*value*'+look.panama.brightness.toFixed(3)+',mask);}';
 n.onBeforeCompile=s=>{s.fragmentShader=warm+'\\n'+s.fragmentShader;s.fragmentShader=s.fragmentShader.replace('#include <map_fragment>','#include <map_fragment>\\ndiffuseColor.rgb=warmPanama(diffuseColor.rgb);').replace('#include <emissivemap_fragment>','#include <emissivemap_fragment>\\ntotalEmissiveRadiance=warmPanama(totalEmissiveRadiance);');};n.customProgramCacheKey=()=>warm;
}return n;});o.material=Array.isArray(o.material)?mats:mats[0];original.set(o,o.material);});
const depth=new T.MeshBasicMaterial({colorWrite:false,depthWrite:true,side:T.DoubleSide}),camera=new T.OrthographicCamera(-1,1,1,-1,.01,30),size=new T.Vector2(),cache=new Map();
function resize(w,h){renderer.getSize(size);if(size.x!==w||size.y!==h)renderer.setSize(w,h);}
window.draw=({head,body='none',scarf='none',layer,view='front',icon=false,combined=false,yaw=null})=>{
 for(const o of Object.values(parts)){o.visible=false;o.traverse(m=>{if(m.isMesh){m.material=original.get(m);m.renderOrder=0;}});}
 subject.rotation.y=studio.rotation.y=(yaw??(view==='home'?${read('tools/heroanim/home-heroes-look.json').yawDegrees[family]}:0))*Math.PI/180;
 for(const key of ${JSON.stringify(scarves.filter(k=>k!=='none'))})parts[key].traverse(m=>{if(m.isMesh&&m.morphTargetInfluences){m.morphTargetInfluences.fill(0);const name=${JSON.stringify(extraBodies.filter(i=>!['Coat','Jacket','Overalls'].includes(i.key)).map(i=>i.key))}.includes(body)?'Over_Overalls':'Over_'+body;const i=m.morphTargetDictionary[name];if(i!==undefined)m.morphTargetInfluences[i]=1;}});
 const selected=icon?[layer]:['base',head,...((layer!=='base'||combined)&&body!=='none'?[body]:[]),...((layer==='scarf'||layer==='EyeA'||layer==='EyeB'||combined)&&scarf!=='none'?[scarf]:[]),...(['EyeA','EyeB'].includes(layer)?[layer]:[])];
 for(const key of selected)parts[key].visible=true;
 if(bald)bald.visible=head!=='NativeDrop';
 const target=icon?parts[layer]:layer==='clothes'?parts[body]:layer==='scarf'?parts[scarf]:parts[layer];
 if(icon){subject.rotation.y=studio.rotation.y=0;subject.updateMatrixWorld(true);const box=new T.Box3().setFromObject(target),center=box.getCenter(new T.Vector3()),s=box.getSize(new T.Vector3()),half=Math.max(s.x,s.y)*.70;camera.left=-half;camera.right=half;camera.top=half;camera.bottom=-half;camera.position.set(center.x+1.1,center.y+.6,center.z+5);camera.lookAt(center);resize(960,960);}
 else{const top=(1.296+96/350)/${axis.scale},bottom=-.144/${axis.scale},center=(top+bottom)/2;camera.left=-.64/${axis.scale};camera.right=.64/${axis.scale};camera.top=(top-bottom)/2;camera.bottom=-(top-bottom)/2;camera.position.set(0,center,5);camera.lookAt(0,center,0);resize(1344,1800);}
 camera.updateProjectionMatrix();camera.updateMatrixWorld();subject.updateMatrixWorld(true);
 const baseKey=view+'|base|'+head;if(!icon&&!combined&&layer==='base'&&cache.has(baseKey))return cache.get(baseKey);
 if(!icon&&!combined&&layer!=='base'){
  const box=new T.Box3().setFromObject(target),masks=[...new Set(selected)].filter(k=>parts[k]!==target),overlaps=masks.filter(k=>{const b=new T.Box3().setFromObject(parts[k]);return b.max.x>=box.min.x&&b.min.x<=box.max.x&&b.max.y>=box.min.y&&b.min.y<=box.max.y;});
  const key=view+'|'+target.uuid+'|'+(layer==='scarf'||overlaps.some(k=>k.startsWith('Scarf'))?body:'')+'|'+overlaps.sort().join(',');if(cache.has(key))return cache.get(key);
  for(const k of masks)parts[k].traverse(m=>{if(m.isMesh){m.material=depth;m.renderOrder=-10;}});
  let l=Infinity,r=-Infinity,b=Infinity,t=-Infinity;for(const x of [box.min.x,box.max.x])for(const y of [box.min.y,box.max.y])for(const z of [box.min.z,box.max.z]){const p=new T.Vector3(x,y,z).project(camera);l=Math.min(l,(p.x+1)*672);r=Math.max(r,(p.x+1)*672);b=Math.min(b,(p.y+1)*900);t=Math.max(t,(p.y+1)*900);}
  const left=Math.max(0,Math.floor(l)-6),bottom=Math.max(0,Math.floor(b)-6),right=Math.min(1344,Math.ceil(r)+6),top=Math.min(1800,Math.ceil(t)+6);
  renderer.setScissorTest(false);renderer.clear();renderer.setScissor(left,bottom,right-left,top-bottom);renderer.setScissorTest(true);renderer.render(scene,camera);renderer.setScissorTest(false);const raw=renderer.domElement.toDataURL().split(',')[1];cache.set(key,raw);return raw;
 }
 renderer.render(scene,camera);const raw=renderer.domElement.toDataURL().split(',')[1];if(!icon&&!combined&&layer==='base')cache.set(baseKey,raw);return raw;
};window.ready=true;</script>`;
const three=path.join(here,'node_modules/three');const server=http.createServer((req,res)=>{const u=new URL(req.url,'http://x').pathname;if(u==='/'){res.end(html);return;}const file=models.get(u)??(u.startsWith('/three/')?path.resolve(three,u.slice(7)):null);if(!file||(!models.has(u)&&!file.startsWith(three+path.sep))||!fs.existsSync(file)){res.writeHead(404);res.end();return;}res.setHeader('Content-Type',file.endsWith('.js')?'text/javascript':'application/octet-stream');fs.createReadStream(file).pipe(res);});
await new Promise(r=>server.listen(0,'127.0.0.1',r));let browser;const rawCache=new Map(),requests=new Map();
try{
 browser=await chromium.launch({executablePath:'/usr/bin/chromium',args:['--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist']});const page=await browser.newPage();page.on('pageerror',e=>console.error(e));await page.goto('http://127.0.0.1:'+server.address().port);await page.waitForFunction(()=>window.ready,{},{timeout:120000});
 if(process.argv.includes('--review')){
  const folder=path.join(repo,build,family,'review');fs.mkdirSync(folder,{recursive:true});
  for(const item of familyItems)for(const yaw of [0,75]){
   const args={head:item.slot==='Head'?item.key:heads[0],body:item.slot==='Body'?item.key:'none',scarf:'none',view:'front',layer:'base',combined:true,yaw};
   fs.writeFileSync(path.join(folder,item.key+'-'+yaw+'.png'),Buffer.from(await page.evaluate(o=>window.draw(o),args),'base64'));
  }
  for(const item of familyItems.filter(i=>i.slot==='Body'))fs.writeFileSync(path.join(folder,item.key+'-icon.png'),Buffer.from(await page.evaluate(o=>window.draw(o),{head:heads[0],layer:item.key,view:'icon',icon:true}),'base64'));
  console.log('Reviewed '+family);continue;
 }
 async function frame(args){const request=JSON.stringify([args.view,args.head,args.layer==='base'?'':args.body,['base','clothes'].includes(args.layer)?'':args.scarf,args.layer,args.icon]);if(requests.has(request))return requests.get(request);const raw=await page.evaluate(o=>window.draw(o),args),key=args.view+'|'+raw;let result=rawCache.get(key);if(result===undefined){result=publish(raw,args.icon?'icon':args.view);rawCache.set(key,result);}requests.set(request,result);return result;}
 for(const view of ['front','home']){
  const table=view==='front'?front:home;
  for(const head of heads)for(const body of bodies)for(const scarf of scarves){
   const newHead=extraHeads.some(i=>i.key===head),affected=newHead||extraBodies.some(i=>i.key===body);
   if(!affected){table.push(null);continue;}
   const args={head,body,scarf,view};
   table.push([newHead?await frame({...args,layer:'base'}):-1,body==='none'?-1:await frame({...args,layer:'clothes'}),scarf==='none'?-1:await frame({...args,layer:'scarf'}),await frame({...args,layer:'EyeA'}),await frame({...args,layer:'EyeB'})]);
  }
  console.log(family+' '+view+': '+table.filter(Boolean).length+' fitted outfits');
 }
 for(const item of familyItems)icons[item.id]=await frame({head:heads[0],layer:item.key,view:'icon',icon:true});
}finally{if(browser)await browser.close();server.close();}

 fs.copyFileSync(path.join(repo,build,family,'build-report.json'),path.join(repo,sourceRoot,family+'-build-report.json'));pin(sourceRoot+'/'+family+'-build-report.json');
 sets.push({family,heads,headIds,bodies,bodyIds,scarves,scarfIds,faces:axis.faces,front,home});
}

if(process.argv.includes('--review'))process.exit(0);
for(const set of sets){const familyOutputs=outputs.filter(f=>f.family===set.family);fs.writeFileSync(path.join(repo,build,set.family,'bake.json'),JSON.stringify({set,icons:Object.fromEntries(Object.entries(icons).filter(([id])=>id.startsWith('hat.'+set.family+'_')||id.startsWith('clothes.'+set.family+'_'))),outputs:familyOutputs},null,2));}
// Reindex family-local cache records into one stable compiled table.
sets.length=0;outputs.length=0;for(const key of Object.keys(icons))delete icons[key];
for(const family of Object.keys(fit.families)){
 const p=path.join(repo,build,family,'bake.json');if(!fs.existsSync(p)){console.log('Baked '+chosen.join(', ')+'. Remaining families still need baking.');process.exit(0);}
 const cached=JSON.parse(fs.readFileSync(p)),offset=outputs.length;
 for(const table of [cached.set.front,cached.set.home])for(const row of table)if(row)for(let i=0;i<row.length;i++)if(row[i]>=0)row[i]+=offset;
 for(const[id,index]of Object.entries(cached.icons))icons[id]=index+offset;sets.push(cached.set);outputs.push(...cached.outputs);
 pin(sourceRoot+'/'+family+'-build-report.json');
}
const data='client/Assets/Bloomlings/UI/Design/WardrobeFinishData.cs';
let cs='// Generated by tools/heroanim/bake-wardrobe-finish-v1.mjs.\nusing Bloomlings.Core.Variants;\nnamespace Bloomlings.Client.UI.Design\n{\n    public static partial class WardrobeFinishArt\n    {\n        private static readonly HeroFrame[] Frames = new[]\n        {\n';
for(const f of outputs)cs+='            new HeroFrame("'+f.file.slice(0,-4)+'", '+f.crop.join(', ')+', .5f, .68f, .5f, .35f),\n';cs+='        };\n';
for(const [name,slot]of [['HeadIds','Head'],['ClothesIds','Body']])cs+='        public static readonly string[] '+name+' = new[] { '+[...fit.items,...fit.newItems].filter(i=>i.slot===slot).map(i=>JSON.stringify(i.id)).join(', ')+' };\n';
cs+='        public static readonly HeroFrame[] HomeFrames = new[] { '+outputs.flatMap((f,i)=>f.view==='home'?['Frames['+i+']']:[]).join(', ')+' };\n';
cs+='        private static readonly System.Collections.Generic.IReadOnlyDictionary<Family, FamilySet> Sets = new System.Collections.Generic.Dictionary<Family, FamilySet>\n        {\n';
for(const set of sets){
 cs+='            [Family.'+set.family[0].toUpperCase()+set.family.slice(1)+'] = new FamilySet(\n';
 for(const ids of [set.headIds,set.bodyIds,set.scarfIds,set.faces])cs+='                new[] { '+ids.map(i=>JSON.stringify(i)).join(', ')+' },\n';
 for(const [n,table]of [set.front,set.home].entries())cs+='                new Recipe?[] {\n'+table.map(row=>'                    '+(row?'new Recipe('+row.join(', ')+')':'null')).join(',\n')+'\n                }'+(n===0?',\n':'),\n');
}
cs+='        };\n        private static readonly System.Collections.Generic.IReadOnlyDictionary<string, int> Icons = new System.Collections.Generic.Dictionary<string, int>\n        {\n'+Object.entries(icons).map(([id,i])=>'            ["'+id+'"] = '+i).join(',\n')+'\n        };\n    }\n}\n';fs.writeFileSync(path.join(repo,data),cs);
const product='tools/heroanim/products/Wardrobe_Clothes_Cleaned_Drop_Additions.zip';pin(product);pin(sourceRoot+'/base-validation.json');pin('tools/heroanim/validate-wardrobe-finish-v1.py');
fs.writeFileSync(path.join(here,'wardrobe-finish-v1.json'),JSON.stringify({version:1,sources:[...sources].map(([path,sha256])=>({path,sha256})),data,dataSha256:sha(Buffer.from(cs)),sets,icons,outputs},null,2)+'\n');
const mf=path.join(dir,'manifest.json'),manifest=JSON.parse(fs.readFileSync(mf)),old=manifest.files.filter(f=>f.source==='owner-wardrobe-finish');
const published=new Set(outputs.map(o=>o.file));for(const file of fs.readdirSync(dir))if((file.startsWith('wardrobefinish-')||/^homehero-[^-]+-finish-/.test(file))&&file.endsWith('.png')&&!published.has(file))fs.unlinkSync(path.join(dir,file));
for(const f of old)if(!outputs.some(o=>o.file===f.path)&&fs.existsSync(path.join(dir,f.path)))fs.unlinkSync(path.join(dir,f.path));
manifest.files=manifest.files.filter(f=>f.source!=='owner-wardrobe-finish');for(const f of outputs)manifest.files.push({path:f.file,width:f.crop[2],height:f.crop[3],sha256:f.sha256,slot:'char.hero3d.motion.'+f.family,source:'owner-wardrobe-finish',record:sourceRoot+'/README.md'});fs.writeFileSync(mf,JSON.stringify(manifest,null,1)+'\n');
console.log('Wardrobe additions:',outputs.length,'unique sprites,',outputs.reduce((s,f)=>s+f.bytes,0),'bytes');

// Render the four owner-supplied simplified Tripo heroes as small full-color 2D game sprites.
// blender -b --factory-startup -t 2 --python-exit-code 1 --python tools/heroanim/prepare-simple-heroes.py
// node tools/heroanim/bake-simple-heroes.mjs
import fs from 'node:fs';
import path from 'node:path';
import http from 'node:http';
import {fileURLToPath} from 'node:url';
import {createHash} from 'node:crypto';
import {chromium} from 'playwright-core';
import {PNG} from 'pngjs';
const here=path.dirname(fileURLToPath(import.meta.url)),repo=path.resolve(here,'../..');
const build=path.join(repo,'content/work/simple-tripo/build'),report=JSON.parse(fs.readFileSync(path.join(build,'build-report.json')));
const three=path.join(here,'node_modules/three'),dir=path.join(repo,'client/Assets/Bloomlings/Art/Heroes/Resources/HeroMotion');
const dataFile=path.join(repo,'client/Assets/Bloomlings/UI/Design/SimpleHeroData.cs');
const sha=b=>createHash('sha256').update(b).digest('hex');
if(report.scriptSha256!==sha(fs.readFileSync(path.join(here,'prepare-simple-heroes.py'))))throw Error('Rebuild the prepared models after changing the importer');
for(const m of report.models)if(sha(fs.readFileSync(path.join(build,m.file)))!==m.sha256)throw Error('Prepared model changed: '+m.file);
for(const s of report.sources)if(sha(fs.readFileSync(path.join(here,'models/simple-tripo/source',s.file)))!==s.sha256)throw Error('Source archive changed: '+s.file);
const look={halfHeight:1/(2*.84),cameraY:(.9-.5)/.84,supersample:3,shading:'albedo-with-soft-diffuse',albedoShare:.75,hemisphere:2,key:1,fill:.4};
const html=`<!doctype html><script type="importmap">{"imports":{"three":"/three/build/three.module.js"}}</script><script type="module">
import * as T from 'three';import {GLTFLoader} from '/three/examples/jsm/loaders/GLTFLoader.js';
const renderer=new T.WebGLRenderer({antialias:true,alpha:true,preserveDrawingBuffer:true});renderer.setPixelRatio(1);renderer.setSize(448*${look.supersample},504*${look.supersample});renderer.setClearColor(0,0);renderer.outputColorSpace=T.SRGBColorSpace;renderer.toneMapping=T.NoToneMapping;
const scene=new T.Scene();scene.add(new T.HemisphereLight(0xffffff,0xffffff,${look.hemisphere}));
for(const [position,intensity]of[[[-3,4,5],${look.key}],[[3,2,4],${look.fill}]]){const light=new T.DirectionalLight(0xffffff,intensity);light.position.set(...position);scene.add(light);}
const models={};for(const family of ['sprig','bloom','drop']){const g=await new GLTFLoader().loadAsync('/model/'+family+'.glb');scene.add(g.scene);models[family]=g.scene;g.scene.visible=false;g.scene.traverse(o=>{if(o.isMesh){o.frustumCulled=false;// Preserve the painted illumination; only a small diffuse share adds body volume without dark PBR/specular shading.
 const materials=(Array.isArray(o.material)?o.material:[o.material]).map(m=>new T.MeshLambertMaterial({map:m.map,color:m.color.clone().multiplyScalar(1-${look.albedoShare}),emissiveMap:m.map,emissive:m.color.clone().multiplyScalar(${look.albedoShare}),side:m.side,transparent:m.transparent,opacity:m.opacity,alphaTest:m.alphaTest}));o.material=Array.isArray(o.material)?materials:materials[0];}});}
const half=${look.halfHeight},camera=new T.OrthographicCamera(-half*448/504,half*448/504,half,-half,.01,30);camera.position.set(0,${look.cameraY},5);camera.lookAt(0,${look.cameraY},0);
window.render=family=>{for(const [key,m]of Object.entries(models))m.visible=key===family;renderer.render(scene,camera);return renderer.domElement.toDataURL('image/png').split(',')[1];};window.ready=true;</script>`;
const server=http.createServer((req,res)=>{const u=new URL(req.url,'http://x').pathname;if(u==='/'){res.end(html);return;}
 const file=u.startsWith('/model/')?path.resolve(build,u.slice(7)):u.startsWith('/three/')?path.resolve(three,u.slice(7)):null;
 if(!file||(!file.startsWith(build+path.sep)&&!file.startsWith(three+path.sep))||!fs.existsSync(file)){res.writeHead(404);res.end();return;}
 res.setHeader('Content-Type',file.endsWith('.js')?'text/javascript':'application/octet-stream');fs.createReadStream(file).pipe(res);
});
await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));let browser;const images=[];
try{browser=await chromium.launch({executablePath:process.env.HERO_ANIM_CHROMIUM??'/usr/bin/chromium',args:['--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist']});
 const page=await browser.newPage();page.on('pageerror',e=>{throw e;});await page.goto('http://127.0.0.1:'+server.address().port);await page.waitForFunction(()=>window.ready,{},{timeout:120000});
 for(const family of ['sprig','bloom','drop']){const b64=await page.evaluate(f=>window.render(f),family);images.push({family,png:reduce(PNG.sync.read(Buffer.from(b64,'base64')),look.supersample)});}
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
for(const {family,png}of images){
 let l=png.width,t=png.height,r=-1,b=-1;
 for(let y=0;y<png.height;y++)for(let x=0;x<png.width;x++)if(png.data[(y*png.width+x)*4+3]>2){l=Math.min(l,x);t=Math.min(t,y);r=Math.max(r,x);b=Math.max(b,y);}
 if(r<l||l===0||t===0||r===png.width-1||b===png.height-1)throw Error('Empty or clipped hero: '+family);
 l--;t--;r++;b++;const w=r-l+1,h=b-t+1,cropped=new PNG({width:w,height:h});PNG.bitblt(png,cropped,l,t,w,h,0,0);
 const bytes=PNG.sync.write(cropped,{colorType:6}),file='simplehero-'+family+'.png';fs.writeFileSync(path.join(dir,file),bytes);
 outputs.push({family,file,crop:[l,t,w,h],bytes:bytes.length,sha256:sha(bytes)});
}
let cs='// Generated by tools/heroanim/bake-simple-heroes.mjs.\nusing Bloomlings.Core.Variants;\nnamespace Bloomlings.Client.UI.Design\n{\n    public static class SimpleHeroArt\n    {\n        public static HeroFrame Frame(Family family) => family switch\n        {\n';
for(const f of outputs)cs+='            Family.'+f.family[0].toUpperCase()+f.family.slice(1)+' => new HeroFrame("'+f.file.slice(0,-4)+'", '+f.crop.join(', ')+', .5f, .62f, .5f, .30f),\n';
cs+='            Family.Twig => TwigV2Art.Layer(null, 0)!.Value,\n';
cs+='            _ => throw new System.ArgumentOutOfRangeException(nameof(family)),\n        };\n    }\n}\n';fs.writeFileSync(dataFile,cs);
const sourceFiles=[...report.sources.map(s=>'tools/heroanim/models/simple-tripo/source/'+s.file),'tools/heroanim/models/simple-tripo/source-manifest.json','tools/heroanim/prepare-simple-heroes.py','tools/heroanim/bake-simple-heroes.mjs'];
const sources=sourceFiles.map(file=>({path:file,sha256:sha(fs.readFileSync(path.join(repo,file)))}));
fs.writeFileSync(path.join(here,'simple-heroes.json'),JSON.stringify({version:1,sources,models:report.models,data:path.relative(repo,dataFile),dataSha256:sha(Buffer.from(cs)),cell:[448,504],encoding:'RGBA8 lossless PNG',render:look,outputs},null,2)+'\n');
fs.writeFileSync(path.join(here,'models/simple-tripo/build-report.json'),JSON.stringify(report,null,2)+'\n');
const folderFile=path.join(dir,'manifest.json'),folder=JSON.parse(fs.readFileSync(folderFile));folder.files=folder.files.filter(x=>!x.path.startsWith('simplehero-'));
for(const f of outputs)folder.files.push({path:f.file,width:f.crop[2],height:f.crop[3],sha256:f.sha256,slot:'char.hero3d.motion.'+f.family,source:'owner',record:'tools/heroanim/SOURCE.md'});
fs.writeFileSync(folderFile,JSON.stringify(folder,null,1)+'\n');console.log('Baked three simplified heroes, '+outputs.reduce((n,x)=>n+x.bytes,0)+' bytes');

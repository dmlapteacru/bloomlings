// Blank-face fallbacks from accepted V2 geometry, never the retired procedural hero placeholders.
import fs from 'node:fs';
import path from 'node:path';
import http from 'node:http';
import {fileURLToPath} from 'node:url';
import {createHash} from 'node:crypto';
import {chromium} from 'playwright-core';
const here=path.dirname(fileURLToPath(import.meta.url)),repo=path.resolve(here,'../..'),three=path.join(here,'node_modules/three');
const sha=b=>createHash('sha256').update(b).digest('hex'),out=path.join(here,'references/current-identity/blanks');fs.mkdirSync(out,{recursive:true});
const records=[];let browser;
try{
 browser=await chromium.launch({executablePath:'/usr/bin/chromium',args:['--use-angle=swiftshader','--enable-unsafe-swiftshader']});
 for(const [family,head,color] of [['sprig','HairLeafy',[.76,.92,.34]],['bloom','HairPetals',[1,.79,.79]],['drop','NativeDrop',[.35,.77,1]]]){
  const title=family[0].toUpperCase()+family.slice(1),prefix=family==='drop'?'DropHeroV2_':title+'V2_',sub=family==='drop'?'game':'build';
  const report=JSON.parse(fs.readFileSync(path.join(repo,'content/work/'+family+'-v2/'+sub+'/build-report.json'))),model=path.join(repo,'content/work/'+family+'-v2/'+sub,report.export.file);
  if(sha(fs.readFileSync(model))!==report.export.sha256)throw Error('Stale '+family+' model');
  const html=`<script type="importmap">{"imports":{"three":"/three/build/three.module.js"}}</script><script type="module">
import * as T from 'three';import{GLTFLoader}from'/three/examples/jsm/loaders/GLTFLoader.js';
const r=new T.WebGLRenderer({antialias:true,alpha:true,preserveDrawingBuffer:true});r.outputColorSpace=T.SRGBColorSpace;r.setClearColor(0,0);r.setSize(1536,1728);
const s=new T.Scene();s.add(new T.HemisphereLight(0xffffff,0xffffff,2));for(const[p,k]of[[[-3,4,5],1],[[3,2,4],.4]]){const l=new T.DirectionalLight(0xffffff,k);l.position.set(...p);s.add(l);}
const g=(await new GLTFLoader().loadAsync('/model.glb')).scene;s.add(g);const prefix=${JSON.stringify(prefix)},head=${JSON.stringify(head)};
g.traverse(o=>{if(o.name.startsWith(prefix)){o.visible=o.name===prefix+'base'||o.name===prefix+head||o.name.startsWith(prefix+'Base_')&&!o.name.endsWith('_Head');}});
g.traverse(o=>{if(!o.isMesh)return;let part=o;while(part.parent&&!part.name.startsWith(prefix))part=part.parent;
 const blank=part.name===prefix+'base'||part.name===prefix+'NativeDrop';
 const mats=(Array.isArray(o.material)?o.material:[o.material]).map(m=>{const n=new T.MeshLambertMaterial({map:m.map,color:m.color.clone().multiplyScalar(.25),emissiveMap:m.map,emissive:m.color.clone().multiplyScalar(.75),side:T.DoubleSide});
 if(blank)n.onBeforeCompile=s=>{s.vertexShader='varying vec3 blankPosition;\\n'+s.vertexShader.replace('#include <worldpos_vertex>','#include <worldpos_vertex>\\nblankPosition=(modelMatrix*vec4(transformed,1.0)).xyz;');s.fragmentShader='varying vec3 blankPosition;\\n'+s.fragmentShader;s.fragmentShader=s.fragmentShader.replace('#include <dithering_fragment>','#include <dithering_fragment>\\nif(blankPosition.y>.40)gl_FragColor.rgb=mix(gl_FragColor.rgb,vec3(${color.join(',')})*(.96+.04*clamp((blankPosition.y-.46)/.6-blankPosition.x*.2,0.0,1.0)),smoothstep(.40,.58,blankPosition.y));');};return n;});o.material=Array.isArray(o.material)?mats:mats[0];
});const scale=${family==='bloom'?'.94':'1'},c=new T.OrthographicCamera(-.64/scale,.64/scale,.72/scale,-.72/scale,.01,30);c.position.set(0,.576/scale,5);c.lookAt(0,.576/scale,0);r.render(s,c);window.png=r.domElement.toDataURL().split(',')[1];</script>`;
  const server=http.createServer((req,res)=>{const u=new URL(req.url,'http://x').pathname;if(u==='/'){res.end(html);return;}const f=u==='/model.glb'?model:u.startsWith('/three/')?path.resolve(three,u.slice(7)):null;if(!f||(f!==model&&!f.startsWith(three+path.sep))||!fs.existsSync(f)){res.writeHead(404);res.end();return;}res.setHeader('Content-Type',f.endsWith('.js')?'text/javascript':'application/octet-stream');fs.createReadStream(f).pipe(res);});await new Promise(r=>server.listen(0,'127.0.0.1',r));const p=await browser.newPage();
  try{p.on('pageerror',e=>console.error(e));await p.goto('http://127.0.0.1:'+server.address().port);await p.waitForFunction(()=>window.png,{},{timeout:120000});const bytes=Buffer.from(await p.evaluate(()=>window.png),'base64'),file=path.join(out,family+'.png');fs.writeFileSync(file,bytes);records.push({family,modelSha256:report.export.sha256,path:path.relative(repo,file),sha256:sha(bytes)});console.log('Rendered current '+family+' blank');}finally{await p.close();await new Promise(r=>server.close(r));}
 }
}finally{await browser?.close();}
fs.writeFileSync(path.join(here,'current-blanks.json'),JSON.stringify({version:1,scriptSha256:sha(fs.readFileSync(fileURLToPath(import.meta.url))),outputs:records},null,2)+'\n');

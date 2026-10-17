// Render and inspect the actual segmented Tripo meshes. No image-generation replacement and no runtime asset writes.
import fs from 'node:fs';
import path from 'node:path';
import http from 'node:http';
import {fileURLToPath} from 'node:url';
import {createHash} from 'node:crypto';
import {chromium} from 'playwright-core';

const here=path.dirname(fileURLToPath(import.meta.url)),root=path.resolve(here,'../..');
const build=path.join(root,'content/work/twig-tripo-study/build'),out=path.join(root,'content/work/twig-tripo-study/review');
const three=path.join(here,'node_modules/three'),model=path.join(build,'TwigHero_Review.glb');
fs.mkdirSync(out,{recursive:true});
const html=`<!doctype html><script type="importmap">{"imports":{"three":"/three/build/three.module.js"}}</script><script type="module">
import * as T from 'three';import {GLTFLoader} from '/three/examples/jsm/loaders/GLTFLoader.js';import {RoomEnvironment} from '/three/examples/jsm/environments/RoomEnvironment.js';
const renderer=new T.WebGLRenderer({antialias:true,alpha:true,preserveDrawingBuffer:true});renderer.setSize(900,1080);renderer.setClearColor(0,0);renderer.outputColorSpace=T.SRGBColorSpace;
const scene=new T.Scene();scene.add(new T.HemisphereLight(0xffffff,0xf0e7d5,1.2));for(const [p,k]of[[[-3,4,5],1.2],[[3,2,4],.6]]){const l=new T.DirectionalLight(0xffffff,k);l.position.set(...p);scene.add(l);}
const room=new RoomEnvironment(),pmrem=new T.PMREMGenerator(renderer),env=pmrem.fromScene(room,.04);scene.environment=env.texture;room.dispose();pmrem.dispose();
const gltf=await new GLTFLoader().loadAsync('/model.glb');scene.add(gltf.scene);
gltf.scene.traverse(o=>{if(o.isMesh){o.frustumCulled=false;for(const m of Array.isArray(o.material)?o.material:[o.material])m.envMapIntensity=.45;}});
const acorn=gltf.scene.getObjectByName('TwigHero_AcornHat'),cap=gltf.scene.getObjectByName('TwigHero_FlowerCap');
if(!acorn||!cap)throw Error('Headwear groups missing');
const camera=new T.OrthographicCamera(-.65,.65,.78,-.78,.01,50),center=new T.Vector3(0,.59,0),half=.7;
window.render=(variant,angle=0)=>{acorn.visible=variant==='acorn';cap.visible=variant==='cap';camera.left=-half*900/1080;camera.right=half*900/1080;camera.top=half;camera.bottom=-half;camera.position.set(Math.sin(angle)*5,center.y+.02,Math.cos(angle)*5);camera.lookAt(center);camera.updateProjectionMatrix();renderer.render(scene,camera);return renderer.domElement.toDataURL('image/png').split(',')[1];};
window.ready=true;</script>`;
const server=http.createServer((req,res)=>{const u=new URL(req.url,'http://x').pathname;if(u==='/'){res.end(html);return;}const file=u==='/model.glb'?model:u.startsWith('/three/')?path.resolve(three,u.slice(7)):null;
 if(!file||(file!==model&&!file.startsWith(three+path.sep))||!fs.existsSync(file)){res.writeHead(404);res.end();return;}res.setHeader('Content-Type',file.endsWith('.js')?'text/javascript':'application/octet-stream');fs.createReadStream(file).pipe(res);
});
await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));let browser;
try{
 browser=await chromium.launch({executablePath:process.env.HERO_ANIM_CHROMIUM??'/usr/bin/chromium',args:['--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist']});
 const page=await browser.newPage();page.on('pageerror',e=>{throw e;});await page.goto('http://127.0.0.1:'+server.address().port);await page.waitForFunction(()=>window.ready,{},{timeout:120000});
 for(const variant of ['base','acorn','cap'])for(const [label,angle]of [['front',0],['angle',.6],['side',Math.PI/2],['back',Math.PI]]){
  const b64=await page.evaluate(([v,a])=>window.render(v,a),[variant,angle]);fs.writeFileSync(path.join(out,variant+'-'+label+'.png'),Buffer.from(b64,'base64'));
 }
 const destination=path.join(root,'output/twig-tripo-study');fs.mkdirSync(destination,{recursive:true});
 for(const view of ['front','angle']){
  const pictures=[['base','База'],['acorn','Шапка жёлудя'],['cap','Кепка']].map(([key,title])=>`<section><h2>${title}</h2><img src="data:image/png;base64,${fs.readFileSync(path.join(out,key+'-'+view+'.png')).toString('base64')}"></section>`).join('');
  await page.setViewportSize({width:1440,height:790});
  await page.setContent(`<!doctype html><meta charset="utf-8"><style>*{box-sizing:border-box}body{margin:0;padding:30px 36px;background:#faf1df;color:#513f29;font:19px system-ui,sans-serif}h1{font-size:30px;margin:0 0 10px}p{margin:0 0 22px}main{display:flex;gap:18px}section{flex:1;border:1px solid #dccaaa;border-radius:18px;background:#fffbf1;padding:16px 10px;text-align:center}h2{font-size:23px;margin:0 0 8px}img{display:block;width:100%;height:530px;object-fit:contain}.note{font-size:16px;margin-top:17px;color:#7f6b4f}</style><h1>Одна база — три варианта</h1><p>Рендер реальных моделей Tripo. Лицо, уши, тело и масштаб сохраняются.</p><main>${pictures}</main><p class="note">Уборы выделены из сегментов и подогнаны к общей голове. Текстуры Tripo пока пробные; риг в исходниках отсутствует.</p>`);
  await page.locator('img').evaluateAll(images=>Promise.all(images.map(i=>i.decode())));
  await page.screenshot({path:path.join(destination,view==='front'?'comparison.png':'angles.png'),fullPage:true});
 }
}finally{if(browser)await browser.close();server.close();}
const sha=bytes=>createHash('sha256').update(bytes).digest('hex');
fs.writeFileSync(path.join(out,'render-report.json'),JSON.stringify({modelSha256:sha(fs.readFileSync(model)),rendererSha256:sha(fs.readFileSync(fileURLToPath(import.meta.url))),cell:[900,1080],sameCameraForAllStates:true,files:fs.readdirSync(out).filter(n=>n.endsWith('.png')).map(file=>({file,sha256:sha(fs.readFileSync(path.join(out,file)))}))},null,2)+'\n');
console.log('Rendered all three shared-face variants from front, angle, side and back: '+out);

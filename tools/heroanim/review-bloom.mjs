// Appearance experiment on the real skinned model, rendered/quantized like production.
// node tools/heroanim/review-bloom.mjs [--quick] [--angles]
// Writes only content/work/bloom-texture/review; never changes runtime frames or source models.
import fs from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {execFileSync} from 'node:child_process';
import {chromium} from 'playwright-core';
import {serve} from './serve.mjs';
import {PNG} from 'pngjs';
import {finish} from './post.mjs';
import {palette,indexed,writePng8} from './png8.mjs';
import {sha256} from './bake.mjs';
const here=path.dirname(fileURLToPath(import.meta.url));
const root=path.resolve(here,'../..');
const config=JSON.parse(fs.readFileSync(path.join(here,'heroes.json')));
const hero=config.heroes.find(h=>h.family==='bloom');
const source=path.join(here,'textures/bloom-study');
const look=JSON.parse(fs.readFileSync(path.join(source,'look.json')));
const baseline=JSON.parse(fs.readFileSync(path.join(source,'baseline.json')));
const texture=fs.readFileSync(path.join(source,'basecolor.png'));
const quick=process.argv.includes('--quick'),angles=process.argv.includes('--angles');
const out=path.join(root,'content/work/bloom-texture/review',angles?'angles':quick?'quick':'motion');
fs.mkdirSync(out,{recursive:true});
const job={...hero,win:null,fps:quick||angles?1:config.fps,
  idleSeconds:hero.idleSeconds,reactSeconds:hero.reactSeconds,
  idleBlend:config.idleBlend,reactIn:config.reactIn,reactOut:config.reactOut,
  pitch:config.pitch,fov:config.fov,color:baseline.color,light:baseline.light,cell:config.cell,feet:config.feet,
  fill:config.fill,margin:config.margin,width:Math.round(config.cell[0]*config.supersample),height:Math.round(config.cell[1]*config.supersample)};
const variants=[{id:'before',changes:{},finish:config.home},
  {id:'after',changes:{albedo:'data:image/png;base64,'+texture.toString('base64'),color:look.color,light:look.light},finish:look.finish}];
const manifest={model:'tools/heroanim/models/'+hero.file,modelSha256:sha256(fs.readFileSync(path.join(here,'models',hero.file))),
  textureSha256:sha256(texture),lookSha256:sha256(fs.readFileSync(path.join(source,'look.json'))),fps:job.fps,cell:job.cell,variants:[]};
const {server,port}=await serve();let browser;
try{
 browser=await chromium.launch({executablePath:process.env.HERO_LAB_CHROMIUM||undefined,args:['--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist']});
 const page=await browser.newPage();page.on('pageerror',e=>{throw e;});
 await page.goto(`http://127.0.0.1:${port}/page.html`);await page.waitForFunction(()=>window.ready);
 const poses=new Map();
 for(const yaw of angles?[-25,0,25]:[hero.yaw])for(const v of variants){
  // Use the actual shipped pixels as the full-motion baseline; avoid rendering the same source twice.
  if(!quick&&!angles&&v.id==='before'){
   const historical=file=>execFileSync('git',['show',`${baseline.commit}:${file}`],{cwd:root,maxBuffer:16*1024*1024});
   const baked=JSON.parse(historical('tools/heroanim/manifest.json')).heroes.find(h=>h.family==='bloom');
   const dir=path.join(out,'before');fs.mkdirSync(dir,{recursive:true});
   const records=baked.frames.map((f,i)=>{
    const bytes=historical('client/Assets/Bloomlings/Art/Heroes/Resources/HeroMotion/'+f.name+'.png');
    if(sha256(bytes)!==f.sha256)throw new Error('Baseline hash mismatch: '+f.name);
    const crop=PNG.sync.read(bytes),p=new PNG({width:job.cell[0],height:job.cell[1]});
    for(let y=0;y<crop.height;y++)crop.data.copy(p.data,((y+f.crop.y)*p.width+f.crop.x)*4,y*crop.width*4,(y+1)*crop.width*4);
    const file=String(i).padStart(3,'0')+'.png';fs.writeFileSync(path.join(dir,file),PNG.sync.write(p));
    return {file,clip:f.clip,head:f.head.map((n,k)=>n*job.cell[k]),top:f.top.map((n,k)=>n*job.cell[k]),bytes:bytes.length};
   });
   poses.set(yaw,records);
   manifest.variants.push({id:'before',frames:records,bytes:records.reduce((s,f)=>s+f.bytes,0)});
   console.log('before:',records.length,'existing production frames, hash checked');
   continue;
  }
  const r=await page.evaluate(j=>window.bake(j),{...job,...v.changes,yaw});
  const id=v.id+(angles?'-'+yaw:'');
  const dir=path.join(out,id);fs.mkdirSync(dir,{recursive:true});
  const pictures=r.frames.map(f=>finish(PNG.sync.read(Buffer.from(f.png.split(',')[1],'base64')),v.finish));
  const pal=palette(pictures);
  let total=0;
  const records=r.frames.map((f,i)=>{
   const p=pictures[i];const bytes=writePng8(p.width,p.height,indexed(p,pal,{dither:v.id==='after'?look.dither:1}),pal);
   const name=String(i).padStart(3,'0')+'.png';fs.writeFileSync(path.join(dir,name),bytes);total+=bytes.length;
   // Full-size lossless reference plus exactly the production shared-palette path, useful for checking detail loss.
   if(i===0)fs.writeFileSync(path.join(dir,'rgba-reference.png'),PNG.sync.write(p));
   return {file:name,clip:f.clip,head:f.head,top:f.top,sha256:sha256(bytes),bytes:bytes.length};
  });
  const geometry=r.frames.map(f=>({clip:f.clip,head:f.head,top:f.top}));
  if(v.id==='before')poses.set(yaw,geometry);
  else {
   const baseline=poses.get(yaw);
   if(geometry.length!==baseline.length||geometry.some((f,i)=>f.clip!==baseline[i].clip||
    [...f.head,...f.top].some((n,k)=>Math.abs(n-[...baseline[i].head,...baseline[i].top][k])>.025)))
    throw new Error('Texture study changed motion/anchors');
  }
  manifest.variants.push({id,frames:records,bytes:total});
  console.log(id,records.length,'frames,',total,'bytes');
 }
 fs.writeFileSync(path.join(out,'manifest.json'),JSON.stringify(manifest,null,1)+'\n');
 console.log('Review only:',out);
}finally{if(browser)await browser.close();server.close();}

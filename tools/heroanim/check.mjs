// Proves the committed hero frames, Home layers and backgrounds still come from this tool: every file listed in
// manifest.json, layers.json (in backgrounds/, the sources) and backdrops.json (its sources, and what the game shows in the
// Backgrounds folder) exists with its SHA-256, nothing else sits in the frames or Backgrounds folder, the generated kit
// files match, and the models and heroes.json are the ones baked. Needs no npm packages. Usage: node check.mjs
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));
const repo = path.resolve(here, '..', '..');
const framesDir = path.join(repo, 'client', 'Assets', 'Bloomlings', 'Art', 'Heroes', 'Resources', 'HeroMotion');
const backgroundsDir = path.join(repo, 'client', 'Assets', 'Bloomlings', 'Art', 'Backgrounds', 'Resources', 'Backgrounds');
const sourcesDir = path.join(here, 'backgrounds');
const sha = file => crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
const problems = [];
const expect = (ok, message) => { if (!ok) problems.push(message); };

const manifest = JSON.parse(fs.readFileSync(path.join(here, 'manifest.json'), 'utf8'));
expect(sha(path.join(here, manifest.config)) === manifest.configSha256, `${manifest.config} changed since the bake: run node bake.mjs`);
expect(sha(path.join(repo, manifest.data)) === manifest.dataSha256, `${manifest.data} does not match the bake`);
const listed = new Set();
let frames = 0;
for (const hero of manifest.heroes) {
  const model = path.join(here, 'models', hero.model);
  expect(fs.existsSync(model) && sha(model) === hero.modelSha256, `models/${hero.model} is not the model baked`);
  for (const source of [hero.appearance, hero.texture].filter(Boolean)) {
    const file = path.join(here, source.path);
    expect(fs.existsSync(file) && sha(file) === source.sha256, `${source.path} changed since the bake`);
  }
  for (const f of hero.frames) {
    const file = path.join(framesDir, f.name + '.png');
    listed.add(f.name + '.png');
    frames++;
    expect(fs.existsSync(file) && sha(file) === f.sha256, `${f.name}.png is missing or changed`);
  }
}
const gearFile=path.join(here,'sprig-gear.json');
if(fs.existsSync(gearFile)){
  const gear=JSON.parse(fs.readFileSync(gearFile));
  expect(sha(path.join(here,gear.model))===gear.modelSha256,'Sprig gear source model changed');
  expect(sha(path.join(repo,gear.data))===gear.dataSha256,'SprigGearData does not match bake');
  for(const f of gear.outputs){listed.add(f.file);expect(fs.existsSync(path.join(framesDir,f.file))&&sha(path.join(framesDir,f.file))===f.sha256, f.file+' changed after gear bake');}
}
const simpleFile=path.join(here,'simple-heroes.json');
if(fs.existsSync(simpleFile)){
 const simple=JSON.parse(fs.readFileSync(simpleFile));
 const referenceFolder=path.join(here,'references/simple-heroes');
 const references=JSON.parse(fs.readFileSync(path.join(referenceFolder,'manifest.json')));
 for(const f of references.files)expect(fs.existsSync(path.join(referenceFolder,f.path))&&sha(path.join(referenceFolder,f.path))===f.sha256,'Simplified hero reference '+f.path+' is missing or changed');
 for(const source of simple.sources)expect(fs.existsSync(path.join(repo,source.path))&&sha(path.join(repo,source.path))===source.sha256,source.path+' changed since simplified hero bake');
 expect(sha(path.join(repo,simple.data))===simple.dataSha256,'SimpleHeroData does not match bake');
 for(const f of simple.outputs){
  listed.add(f.file);const file=path.join(framesDir,f.file);
  expect(fs.existsSync(file)&&sha(file)===f.sha256,f.file+' changed after simplified hero bake');
  if(fs.existsSync(file)){const png=fs.readFileSync(file);expect(png[24]===8&&png[25]===6&&png.readUInt32BE(16)===f.crop[2]&&png.readUInt32BE(20)===f.crop[3],f.file+' must retain full-color RGBA and declared crop size');}
 }
}
const v2File=path.join(here,'twig-v2.json');
const sprigV2File=path.join(here,'sprig-v2.json');
const bloomV2File=path.join(here,'bloom-v2.json');
if(fs.existsSync(bloomV2File)){
 const v2=JSON.parse(fs.readFileSync(bloomV2File));
 expect(v2.outputs.length===255&&v2.heads.length===10&&v2.clothes.length===4&&v2.scarves.length===3&&v2.icons.length===15,'Bloom V2 must include all delivered items and outfit-aware masks for both scarves');
 for(const source of v2.sources)expect(fs.existsSync(path.join(repo,source.path))&&sha(path.join(repo,source.path))===source.sha256,source.path+' changed since Bloom V2 bake');
 expect(sha(path.join(repo,v2.data))===v2.dataSha256,'BloomV2Data is stale');
 const build=JSON.parse(fs.readFileSync(path.join(here,'models/bloom-v2/build-report.json')));
 expect(build.scriptSha256===sha(path.join(here,'prepare-bloom-v2.py'))&&build.parts.length===16&&build.parts.every(p=>p.worldGeometryUnchanged&&p.uvUnchanged),'Bloom source alignment / UV proof is stale');
 expect(build.animationSource.actions.length===4&&build.parts.find(p=>p.key==='ScarfKnit').shapeKeys.length===4,'Bloom source actions and knitted scarf garment fits must be retained');
 for(const f of v2.outputs){const file=path.join(framesDir,f.file);listed.add(f.file);expect(fs.existsSync(file)&&sha(file)===f.sha256,f.file+' changed since Bloom V2 bake');if(fs.existsSync(file)){const png=fs.readFileSync(file);expect(png[24]===8&&png[25]===6&&png.readUInt32BE(16)===f.crop[2]&&png.readUInt32BE(20)===f.crop[3],f.file+' must retain RGBA pixels and crop size');}}
}
if(fs.existsSync(sprigV2File)){
 const v2=JSON.parse(fs.readFileSync(sprigV2File));
 expect(v2.outputs.length===157&&v2.heads.length===9&&v2.clothes.length===4&&v2.icons.length===13,'Sprig V2 must include every delivered part and outfit-aware mask');
 for(const source of v2.sources)expect(fs.existsSync(path.join(repo,source.path))&&sha(path.join(repo,source.path))===source.sha256,source.path+' changed since Sprig V2 bake');
 expect(sha(path.join(repo,v2.data))===v2.dataSha256,'SprigV2Data is stale');
 const build=JSON.parse(fs.readFileSync(path.join(here,'models/sprig-v2/build-report.json')));
 expect(build.scriptSha256===sha(path.join(here,'prepare-sprig-v2.py'))&&build.parts.length===14&&build.parts.every(p=>p.worldGeometryUnchanged&&p.uvUnchanged),'Sprig source alignment / UV proof is stale');
 expect(build.animationSource.actions.length===4&&build.parts.find(p=>p.key==='Scarf').shapeKeys.length===4,'Sprig source actions and scarf fits must be retained');
 for(const f of v2.outputs){const file=path.join(framesDir,f.file);listed.add(f.file);expect(fs.existsSync(file)&&sha(file)===f.sha256,f.file+' changed since Sprig V2 bake');}
}
if(fs.existsSync(v2File)){
 const v2=JSON.parse(fs.readFileSync(v2File));
 expect(v2.outputs.length===35&&v2.heads.length===8&&v2.clothes.length===4&&v2.icons.length===12,'Twig V2 must include all delivered parts and masked layers');
 for(const source of v2.sources)expect(fs.existsSync(path.join(repo,source.path))&&sha(path.join(repo,source.path))===source.sha256,source.path+' changed since V2 bake');
 expect(sha(path.join(repo,v2.data))===v2.dataSha256,'TwigV2Data is stale');
 const build=JSON.parse(fs.readFileSync(path.join(here,'models/twig-v2/build-report.json')));
 expect(build.scriptSha256===sha(path.join(here,'prepare-twig-v2.py'))&&build.parts.length===13&&build.parts.every(p=>p.worldGeometryUnchanged&&p.uvUnchanged),'Twig V2 source alignment / UV proof is stale');
 expect(build.layerSeparation.headwearClearsClothesAndScarf&&build.layerSeparation.glassesClearClothes,'Twig garment / scarf / eyewear layer separation changed');
 expect(build.animationSource.actions.length===4,'Twig V2 source actions must be retained');
 for(const f of v2.outputs){const file=path.join(framesDir,f.file);listed.add(f.file);expect(fs.existsSync(file)&&sha(file)===f.sha256,f.file+' changed since V2 bake');if(fs.existsSync(file)){const png=fs.readFileSync(file);expect(png[24]===8&&png[25]===6&&png.readUInt32BE(16)===f.crop[2]&&png.readUInt32BE(20)===f.crop[3],f.file+' must retain RGBA pixels and crop size');}}
}
// Current references, derived identity outputs and the offline retirement record are pinned independently.
for (const relative of ['references/twig-v2/manifest.json','references/sprig-v2/manifest.json','references/bloom-v2/manifest.json','references/drop-v2/manifest.json','archive/twig/manifest.json','archive/sprig/manifest.json','archive/bloom/manifest.json','models/drop-clothing-study/manifest.json','models/drop-v2/manifest.json']) {
 const recordPath=path.join(here,relative),record=JSON.parse(fs.readFileSync(recordPath));
 for(const f of record.files){const file=path.join(path.dirname(recordPath),f.path);expect(fs.existsSync(file)&&sha(file)===f.sha256,relative+': '+f.path+' changed');}
}
const identity=JSON.parse(fs.readFileSync(path.join(here,'twig-identity.json')));
for(const f of [...identity.sources,...identity.outputs]){const file=path.join(repo,f.path);expect(fs.existsSync(file)&&sha(file)===f.sha256,'Twig identity '+f.path+' changed');}
expect(!manifest.heroes.some(h=>h.family==='twig'),'Legacy motion publisher must not include Twig');
expect(!manifest.heroes.some(h=>h.family==='bloom'),'Legacy motion publisher must not include Bloom');
expect(!fs.readdirSync(framesDir).some(n=>/^bloom-(idle|react|win)/.test(n)),'Retired Bloom motion must stay outside runtime');
expect(!fs.readdirSync(framesDir).some(n=>/^(twig-(idle|react|win)|twigmod-|twigfit-|simplehero-twig)/.test(n)),'Retired Twig resources must stay outside runtime');
const folder = JSON.parse(fs.readFileSync(path.join(framesDir, 'manifest.json'), 'utf8'));
const tripoStudy=path.join(here,'models/twig-tripo/study-manifest.json');
if(fs.existsSync(tripoStudy)){
  const study=JSON.parse(fs.readFileSync(tripoStudy));
  for(const entry of [...study.sources,...study.outputs])
    expect(fs.existsSync(path.join(repo,entry.path))&&sha(path.join(repo,entry.path))===entry.sha256,`Twig Tripo study ${entry.path} is missing or changed`);
}
const folderFiles = new Map(folder.files.map(f => [f.path, f.sha256]));
if(fs.existsSync(sprigV2File))for(const f of JSON.parse(fs.readFileSync(sprigV2File)).outputs)expect(folderFiles.get(f.file)===f.sha256,'HeroMotion/manifest.json: '+f.file);
if(fs.existsSync(bloomV2File))for(const f of JSON.parse(fs.readFileSync(bloomV2File)).outputs)expect(folderFiles.get(f.file)===f.sha256,'HeroMotion/manifest.json: '+f.file);
if(fs.existsSync(v2File))for(const f of JSON.parse(fs.readFileSync(v2File)).outputs)expect(folderFiles.get(f.file)===f.sha256,'HeroMotion/manifest.json: '+f.file);
expect(folderFiles.size === listed.size && [...listed].every(n => folderFiles.has(n)), 'HeroMotion/manifest.json does not list the baked frames');
if(fs.existsSync(gearFile))for(const f of JSON.parse(fs.readFileSync(gearFile)).outputs)
  expect(folderFiles.get(f.file)===f.sha256,`HeroMotion/manifest.json: ${f.file}`);
if(fs.existsSync(simpleFile))for(const f of JSON.parse(fs.readFileSync(simpleFile)).outputs)
 expect(folderFiles.get(f.file)===f.sha256,'HeroMotion/manifest.json: '+f.file);
for (const hero of manifest.heroes) for (const f of hero.frames) expect(folderFiles.get(f.name + '.png') === f.sha256, `HeroMotion/manifest.json: ${f.name}.png`);
expect(folder.files.every(f => f.record && fs.existsSync(path.join(repo, f.record))), 'HeroMotion/manifest.json names a missing source record');
for (const f of fs.readdirSync(framesDir)) {
  if (f.endsWith('.meta') || f === 'manifest.json') continue;
  expect(listed.has(f), `HeroMotion/${f} is not in manifest.json`);
}

const layers = JSON.parse(fs.readFileSync(path.join(here, 'layers.json'), 'utf8'));
expect(sha(path.join(repo, layers.data)) === layers.dataSha256, `${layers.data} does not match layers.json`);
for (const o of layers.outputs) {
  const file = path.join(sourcesDir, o.name);
  expect(fs.existsSync(file) && sha(file) === o.sha256, `backgrounds/${o.name} is missing or changed since layers.mjs`);
}

// The backgrounds' finish (backdrops.mjs, spec 005 FR-049): each shown picture from its source as delivered.
const backdrops = JSON.parse(fs.readFileSync(path.join(here, 'backdrops.json'), 'utf8'));
const finished = new Set(backdrops.outputs.map(o => o.name));
for (const o of backdrops.outputs) {
  const source = path.join(sourcesDir, o.name);
  const file = path.join(backgroundsDir, o.name);
  expect(fs.existsSync(source) && sha(source) === o.sourceSha256, `backgrounds/${o.name} changed since backdrops.mjs: run node backdrops.mjs`);
  expect(fs.existsSync(file) && sha(file) === o.sha256, `Backgrounds/${o.name} is missing or changed: run node backdrops.mjs`);
}
for (const f of fs.readdirSync(backgroundsDir)) {
  if (f.endsWith('.meta') || f === '.gitkeep') continue;
  expect(finished.has(f), `Backgrounds/${f} is not in backdrops.json`);
}
for (const f of fs.readdirSync(sourcesDir)) expect(finished.has(f), `backgrounds/${f} is not in backdrops.json`);

if (problems.length) {
  for (const p of problems) console.error('FAIL ' + p);
  process.exit(1);
}
console.log(`heroanim check: OK (${frames} hero frames of ${manifest.heroes.length} heroes, ${layers.outputs.length} Home layers, ${backdrops.outputs.length} backgrounds finished)`);

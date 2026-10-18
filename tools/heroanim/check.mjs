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
const v2File=path.join(here,'twig-v2.json');
const sprigV2File=path.join(here,'sprig-v2.json');
const bloomV2File=path.join(here,'bloom-v2.json');
const dropV2File=path.join(here,'drop-v2.json');
if(fs.existsSync(dropV2File)){
 const v2=JSON.parse(fs.readFileSync(dropV2File));
 expect(v2.outputs.length===231&&v2.heads.length===9&&v2.clothes.length===4&&v2.scarves.length===3&&v2.icons.length===15,'Drop V2 must include all 15 fitted items and complete outfit masks');
 for(const source of v2.sources)expect(fs.existsSync(path.join(repo,source.path))&&sha(path.join(repo,source.path))===source.sha256,source.path+' changed since Drop V2 bake');
 expect(sha(path.join(repo,v2.data))===v2.dataSha256,'DropV2Data is stale');
 const build=JSON.parse(fs.readFileSync(path.join(here,'models/drop-v2/game-build.json')));
 expect(build.scriptSha256===sha(path.join(here,'prepare-drop-v2-game.py'))&&build.parts.length===23&&build.parts.every(p=>p.sourceGeometryUnderGlobalRotation&&p.uvUnchanged),'Drop FBX handoff geometry / UV proof is stale');
 expect(build.animationClips.length===5&&build.parts.find(p=>p.key==='ScarfKnit').shapeKeys.length===4,'Drop handoff actions and scarf garment fits must remain available');
 for(const f of v2.outputs){const file=path.join(framesDir,f.file);listed.add(f.file);expect(fs.existsSync(file)&&sha(file)===f.sha256,f.file+' changed since Drop V2 bake');if(fs.existsSync(file)){const png=fs.readFileSync(file);expect(png[24]===8&&png[25]===6&&png.readUInt32BE(16)===f.crop[2]&&png.readUInt32BE(20)===f.crop[3],f.file+' must retain RGBA pixels and crop size');}}
}
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
// Home has an angled counterpart for every current outfit layer; icons stay frontal.
const homeHeroes=JSON.parse(fs.readFileSync(path.join(here,'home-heroes.json')));
for(const source of homeHeroes.sources)expect(fs.existsSync(path.join(repo,source.path))&&sha(path.join(repo,source.path))===source.sha256,source.path+' changed since Home bake');
expect(sha(path.join(repo,homeHeroes.data))===homeHeroes.dataSha256,'HomeHeroData is stale');
const homeLook=JSON.parse(fs.readFileSync(path.join(here,'home-heroes-look.json')));
expect(homeHeroes.models.length===4,'Home must retain all four families');
for(const model of homeHeroes.models){
 const report=JSON.parse(fs.readFileSync(path.join(here,'models',model.family+'-v2',model.family==='drop'?'game-build.json':'build-report.json')));
 expect(model.sha256===report.export.sha256&&model.yawDegrees===homeLook.yawDegrees[model.family],'Home '+model.family+' model / yaw provenance changed');
}
const homeByName=new Map(homeHeroes.outputs.map(f=>[f.file.slice(0,-4),f]));
const frontLayers=[v2File,sprigV2File,bloomV2File,dropV2File].flatMap(file=>JSON.parse(fs.readFileSync(file)).outputs.filter(f=>!f.icon).map(f=>f.file.slice(0,-4)));
expect(homeHeroes.mappings.length===frontLayers.length&&new Set(homeHeroes.mappings.map(m=>m.front)).size===frontLayers.length&&frontLayers.every(n=>homeHeroes.mappings.some(m=>m.front===n&&homeByName.has(m.home))),'Every fitted layer needs a Home view');
for(const f of homeHeroes.outputs){
 const file=path.join(framesDir,f.file);listed.add(f.file);
 expect(fs.existsSync(file)&&sha(file)===f.sha256,f.file+' changed since Home bake');
 if(fs.existsSync(file)){const png=fs.readFileSync(file);expect(png[24]===8&&png[25]===6&&png.readUInt32BE(16)===f.crop[2]&&png.readUInt32BE(20)===f.crop[3],f.file+' must retain RGBA and its declared crop');}
}
// Current accepted references, identity outputs and source proofs are pinned independently.
for (const relative of ['references/twig-v2/manifest.json','references/sprig-v2/manifest.json','references/bloom-v2/manifest.json','references/drop-v2/manifest.json','references/current-identity/manifest.json','models/drop-clothing-study/manifest.json','models/drop-v2/manifest.json','models/drop-v2/game-review.json','models/home-heroes/manifest.json']) {
 const recordPath=path.join(here,relative),record=JSON.parse(fs.readFileSync(recordPath));
 for(const f of record.files){const file=path.join(path.dirname(recordPath),f.path);expect(fs.existsSync(file)&&sha(file)===f.sha256,relative+': '+f.path+' changed');}
}
const currentIdentity=JSON.parse(fs.readFileSync(path.join(here,'current-identity.json')));
for(const f of [...currentIdentity.sources,...currentIdentity.outputs])expect(fs.existsSync(path.join(repo,f.path))&&sha(path.join(repo,f.path))===f.sha256,'Accepted hero identity '+f.path+' changed');
const blanks=JSON.parse(fs.readFileSync(path.join(here,'current-blanks.json')));
expect(blanks.scriptSha256===sha(path.join(here,'bake-current-blanks.mjs')),'Current blank render recipe changed');
for(const f of blanks.outputs){
 const report=JSON.parse(fs.readFileSync(path.join(here,'models',f.family+'-v2',f.family==='drop'?'game-build.json':'build-report.json')));
 expect(f.modelSha256===report.export.sha256&&sha(path.join(repo,f.path))===f.sha256,'Current blank '+f.family+' source changed');
}
for(const f of ['heroes.glb','sprig-lab.glb','sprig-outfit-study.glb','sprig-plant-study.glb'])expect(!fs.existsSync(path.join(here,'models',f)),'Retired model must not return: '+f);
expect(!fs.readdirSync(framesDir).some(n=>/^(sprigmod-|simplehero-|(?:sprig|bloom|drop|twig)-(?:idle|react|win))/.test(n)),'Runtime can contain only accepted V2 and Home view sprites');
const identity=JSON.parse(fs.readFileSync(path.join(here,'twig-identity.json')));
for(const f of [...identity.sources,...identity.outputs]){const file=path.join(repo,f.path);expect(fs.existsSync(file)&&sha(file)===f.sha256,'Twig identity '+f.path+' changed');}
expect(!manifest.heroes.some(h=>h.family==='twig'),'Legacy motion publisher must not include Twig');
expect(!manifest.heroes.some(h=>h.family==='drop'),'Legacy motion publisher must not include Drop');
expect(!fs.readdirSync(framesDir).some(n=>/^drop-(idle|react|win)|^simplehero-drop/.test(n)),'Retired Drop resources must stay outside runtime');
expect(!manifest.heroes.some(h=>h.family==='bloom'),'Legacy motion publisher must not include Bloom');
expect(!fs.readdirSync(framesDir).some(n=>/^bloom-(idle|react|win)/.test(n)),'Retired Bloom motion must stay outside runtime');
expect(!fs.readdirSync(framesDir).some(n=>/^(twig-(idle|react|win)|twigmod-|twigfit-|simplehero-twig)/.test(n)),'Retired Twig resources must stay outside runtime');
// Separate owner eyewear has its own complete front/Home mask table; base bakes remain pinned.
const eyewear=JSON.parse(fs.readFileSync(path.join(here,'eyewear-v1.json')));
expect(eyewear.sets.length===6,'All six owner eyewear variants must be available');
for(const source of eyewear.sources)expect(fs.existsSync(path.join(repo,source.path))&&sha(path.join(repo,source.path))===source.sha256,source.path+' changed since eyewear bake');
expect(sha(path.join(repo,eyewear.data))===eyewear.dataSha256,'EyewearData is stale');
const eyeProof=JSON.parse(fs.readFileSync(path.join(here,'models/eyewear-v1/build-report.json')));
expect(eyeProof.variants.length===6&&eyeProof.variants.every(v=>v.rigidHeadWeights&&v.uvUnchanged&&v.geometryError<1e-6),'Eyewear Head binding / geometry / UV proof is invalid');
for(const set of eyewear.sets){
 const base=JSON.parse(fs.readFileSync(path.join(here,set.family+'-v2.json'))),count=(base.heads.length-1)*base.clothes.length*(base.scarves?.length??2);
 expect(set.front.length===count&&set.home.length===count,'Incomplete eyewear masks: '+set.id);
 for(const view of ['front','home'])expect(set[view].every(i=>eyewear.outputs[i]?.family===set.family&&eyewear.outputs[i]?.view===view),'Wrong eyewear view / family: '+set.id);
 expect(eyewear.outputs[set.icon]?.view==='icon','Missing isolated eyewear icon: '+set.id);
}
for(const f of eyewear.outputs){
 const file=path.join(framesDir,f.file);listed.add(f.file);expect(fs.existsSync(file)&&sha(file)===f.sha256,f.file+' changed since eyewear bake');
 if(fs.existsSync(file)){const png=fs.readFileSync(file);expect(png[24]===8&&png[25]===6&&png.readUInt32BE(16)===f.crop[2]&&png.readUInt32BE(20)===f.crop[3],f.file+' must retain RGBA and its declared crop');}
}
// Additional Bloom Head / Body items include masks for every affected scarf and eyewear combination.
const bloomExtras=JSON.parse(fs.readFileSync(path.join(here,'bloom-extras-v1.json')));
for(const source of bloomExtras.sources)expect(fs.existsSync(path.join(repo,source.path))&&sha(path.join(repo,source.path))===source.sha256,source.path+' changed since Bloom extras bake');
expect(sha(path.join(repo,bloomExtras.data))===bloomExtras.dataSha256,'BloomExtrasData is stale');
const extrasFit=JSON.parse(fs.readFileSync(path.join(here,'models/bloom-extras-v1/fit.json'))),extrasProof=JSON.parse(fs.readFileSync(path.join(here,'models/bloom-extras-v1/build-report.json')));
expect(extrasFit.items.length===9&&extrasFit.items.filter(i=>i.slot==='Head').length===7&&extrasFit.items.filter(i=>i.slot==='Body').length===2,'Bloom extras must retain seven Head and two Body items');
expect(extrasProof.acceptedGeometryUnchanged&&extrasProof.acceptedModelSha256===JSON.parse(fs.readFileSync(path.join(here,'models/bloom-v2/build-report.json'))).export.sha256,'Bloom accepted geometry changed under fitted additions');
expect(extrasProof.scriptSha256===sha(path.join(here,'prepare-bloom-extras-v1.py'))&&extrasProof.fitSha256===sha(path.join(here,'models/bloom-extras-v1/fit.json')),'Bloom fitting proof is stale');
expect(extrasProof.items.length===9&&extrasProof.items.every(i=>i.uvUnchanged&&i.weightsUnchanged&&i.fbxRoundTripError<1e-5),'Bloom fitted FBX geometry / UV / weight proof failed');
expect(bloomExtras.heads.length===16&&bloomExtras.bodies.length===6&&bloomExtras.scarves.length===3,'Bloom extras outfit axes are incomplete');
for(const view of ['front','home']){
 expect(bloomExtras[view].length===288&&bloomExtras[view].filter(Boolean).length===180,'Bloom extras '+view+' outfit coverage is incomplete');
 for(const row of bloomExtras[view].filter(Boolean))expect(row.length===5&&row.every(i=>i===-1||bloomExtras.outputs[i]?.view===view),'Bloom extras '+view+' has a wrong layer');
}
expect(Object.keys(bloomExtras.icons).length===9&&Object.values(bloomExtras.icons).every(i=>bloomExtras.outputs[i]?.view==='icon'),'Bloom extras must have nine isolated item icons');
for(const f of bloomExtras.outputs){
 const file=path.join(framesDir,f.file);listed.add(f.file);expect(fs.existsSync(file)&&sha(file)===f.sha256,f.file+' changed since Bloom extras bake');
 if(fs.existsSync(file)){const png=fs.readFileSync(file);expect(png[24]===8&&png[25]===6&&png.readUInt32BE(16)===f.crop[2]&&png.readUInt32BE(20)===f.crop[3],f.file+' must retain RGBA and its crop');}
}
// Archive-delivered additions for Sprig and Twig retain all fitted outfit/view counterparts.
const additions=JSON.parse(fs.readFileSync(path.join(here,'wardrobe-additions-v1.json'))),additionFit=JSON.parse(fs.readFileSync(path.join(here,'models/wardrobe-additions-v1/fit.json')));
for(const source of additions.sources)expect(fs.existsSync(path.join(repo,source.path))&&sha(path.join(repo,source.path))===source.sha256,source.path+' changed since wardrobe additions bake');
expect(sha(path.join(repo,additions.data))===additions.dataSha256,'WardrobeAdditionsData is stale');
expect(additionFit.items.length===17&&additionFit.archives.length===2&&additions.sets.length===2,'All 17 archive-delivered additions must be retained');
for(const set of additions.sets){
 const proof=JSON.parse(fs.readFileSync(path.join(here,'models/wardrobe-additions-v1/'+set.family+'-build-report.json'))),accepted=JSON.parse(fs.readFileSync(path.join(here,'models/'+set.family+'-v2/build-report.json')));
 expect(!proof.inspection&&proof.acceptedGeometryUnchanged&&proof.acceptedModelSha256===accepted.export.sha256,'Accepted '+set.family+' geometry changed under additions');
 expect(proof.scriptSha256===sha(path.join(here,'prepare-wardrobe-additions-v1.py'))&&proof.fitSha256===sha(path.join(here,'models/wardrobe-additions-v1/fit.json')),'Wardrobe fitting proof is stale: '+set.family);
 const items=additionFit.items.filter(i=>i.family===set.family);
 expect(items.filter(i=>i.slot==='Head').length===7&&proof.items.length===(set.family==='sprig'?8:9),'Missing additions for '+set.family);
 expect(proof.items.every(i=>i.uvUnchanged&&i.weightsUnchanged&&i.fbxRoundTripError<1e-5&&(items.find(j=>j.id===i.id)?.slot!=='Head'||i.rigidHead)),'Fitted FBX UV, weights or rest geometry failed: '+set.family);
 const count=set.heads.length*set.bodies.length*set.scarves.length,affected=set.family==='sprig'?86:112;
 for(const view of ['front','home']){
  expect(set[view].length===count&&set[view].filter(Boolean).length===affected,'Incomplete additions coverage: '+set.family+'/'+view);
  for(const row of set[view].filter(Boolean))expect(row.length===5&&row.every(i=>i===-1||(additions.outputs[i]?.family===set.family&&additions.outputs[i]?.view===view)),'Wrong additions family or view: '+set.family);
 }
}
expect(Object.keys(additions.icons).length===17&&Object.values(additions.icons).every(i=>additions.outputs[i]?.view==='icon'),'Every new item needs an isolated icon');
for(const f of additions.outputs){
 const file=path.join(framesDir,f.file);listed.add(f.file);expect(fs.existsSync(file)&&sha(file)===f.sha256,f.file+' changed since additions bake');
 if(fs.existsSync(file)){const png=fs.readFileSync(file);expect(png[24]===8&&png[25]===6&&png.readUInt32BE(16)===f.crop[2]&&png.readUInt32BE(20)===f.crop[3],f.file+' must retain RGBA and its crop');}
}
// Every repaired garment and Drop addition uses the same validated FBX rest mesh in-game.
const finish=JSON.parse(fs.readFileSync(path.join(here,'wardrobe-finish-v1.json')));
for(const source of finish.sources)expect(fs.existsSync(path.join(repo,source.path))&&sha(path.join(repo,source.path))===source.sha256,source.path+' changed since garment repair bake');
expect(sha(path.join(repo,finish.data))===finish.dataSha256,'WardrobeFinishData is stale');
expect(finish.sets.length===4&&Object.keys(finish.icons).length===26,'All 19 garments and seven new Drop Head items must be present');
const baseProof=JSON.parse(fs.readFileSync(path.join(here,'models/wardrobe-finish-v1/base-validation.json')));
expect(baseProof.scriptSha256===sha(path.join(here,'validate-wardrobe-finish-v1.py'))&&baseProof.families.length===4&&baseProof.families.every(f=>f.checks.length===({sprig:9,bloom:10,twig:8,drop:17}[f.family])&&f.checks.every(c=>c.polygonCount>0&&c.worldGeometryAndUvUnchanged)),'Independent base / Head geometry and UV validation failed');
for(const set of finish.sets){
 const proof=JSON.parse(fs.readFileSync(path.join(here,'models/wardrobe-finish-v1/'+set.family+'-build-report.json')));
 expect(proof.baseGeometryUnchanged&&proof.scriptSha256===sha(path.join(here,'prepare-wardrobe-finish-v1.py'))&&proof.cleanupSha256===sha(path.join(here,'garment_cleanup.py')),'Garment geometry proof is stale: '+set.family);
 expect(proof.items.every(i=>i.fbxRoundTripError<1e-5&&(i.slot!=='Body'||i.normalizedWeights&&i.rimVertices>0&&i.vertices>0&&i.uvLoops>0)),'Garment FBX round trip, UVs, weights or rim failed: '+set.family);
 const size=set.heads.length*set.bodies.length*set.scarves.length;
 for(const view of ['front','home']){
  expect(set[view].length===size,'Repaired outfit table is incomplete: '+set.family+'/'+view);
  for(let head=0;head<set.heads.length;head++)for(let body=0;body<set.bodies.length;body++)for(let scarf=0;scarf<set.scarves.length;scarf++){
   const row=set[view][(head*set.bodies.length+body)*set.scarves.length+scarf],affected=body>0||set.family==='drop'&&head>=9;
   expect(Boolean(row)===affected,'Missing repaired outfit: '+set.family+'/'+view+'/'+head+'/'+body+'/'+scarf);
   if(row)expect(row.length===5&&row.every(i=>i===-1||finish.outputs[i]?.family===set.family&&finish.outputs[i]?.view===view),'Wrong repair layer family or view');
  }
 }
}
for(const f of finish.outputs){
 const file=path.join(framesDir,f.file);listed.add(f.file);expect(fs.existsSync(file)&&sha(file)===f.sha256,f.file+' changed since repair bake');
 if(fs.existsSync(file)){const png=fs.readFileSync(file);expect(png[24]===8&&png[25]===6&&png.readUInt32BE(16)===f.crop[2]&&png.readUInt32BE(20)===f.crop[3],f.file+' must retain RGBA and its crop');}
}
const folder = JSON.parse(fs.readFileSync(path.join(framesDir, 'manifest.json'), 'utf8'));
const folderFiles = new Map(folder.files.map(f => [f.path, f.sha256]));
for(const f of finish.outputs)expect(folderFiles.get(f.file)===f.sha256,'HeroMotion/manifest.json: '+f.file);
for(const f of additions.outputs)expect(folderFiles.get(f.file)===f.sha256,'HeroMotion/manifest.json: '+f.file);
for(const f of bloomExtras.outputs)expect(folderFiles.get(f.file)===f.sha256,'HeroMotion/manifest.json: '+f.file);
for(const f of eyewear.outputs)expect(folderFiles.get(f.file)===f.sha256,'HeroMotion/manifest.json: '+f.file);
for(const f of homeHeroes.outputs)expect(folderFiles.get(f.file)===f.sha256,'HeroMotion/manifest.json: '+f.file);
if(fs.existsSync(dropV2File))for(const f of JSON.parse(fs.readFileSync(dropV2File)).outputs)expect(folderFiles.get(f.file)===f.sha256,'HeroMotion/manifest.json: '+f.file);
if(fs.existsSync(sprigV2File))for(const f of JSON.parse(fs.readFileSync(sprigV2File)).outputs)expect(folderFiles.get(f.file)===f.sha256,'HeroMotion/manifest.json: '+f.file);
if(fs.existsSync(bloomV2File))for(const f of JSON.parse(fs.readFileSync(bloomV2File)).outputs)expect(folderFiles.get(f.file)===f.sha256,'HeroMotion/manifest.json: '+f.file);
if(fs.existsSync(v2File))for(const f of JSON.parse(fs.readFileSync(v2File)).outputs)expect(folderFiles.get(f.file)===f.sha256,'HeroMotion/manifest.json: '+f.file);
expect(folderFiles.size === listed.size && [...listed].every(n => folderFiles.has(n)), 'HeroMotion/manifest.json does not list the baked frames');
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
if(fs.existsSync(path.join(here,'products/rig-fix-v1/manifest.json')))await import('./rigging/check.mjs');
console.log(`heroanim check: OK (${listed.size} hero pictures, ${frames} legacy animation frames, ${layers.outputs.length} Home layers, ${backdrops.outputs.length} backgrounds finished)`);

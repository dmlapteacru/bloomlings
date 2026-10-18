// Check published authoring products without Blender, extraction or npm packages.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {fileURLToPath} from 'node:url';

const here=path.dirname(fileURLToPath(import.meta.url));
const repo=path.resolve(here,'../../..');
const products=path.resolve(here,'../products/rig-fix-v2');
const manifest=JSON.parse(fs.readFileSync(path.join(products,'manifest.json')));
const cuffSelections=JSON.parse(fs.readFileSync(path.join(here,'cuff_ownership.json'))).assets;
const sha=file=>crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
const expect=(ok,message)=>{if(!ok)throw Error(message);};
let files=0,parts=0;
expect(manifest.version===2&&manifest.package==='Bloomlings_RigFix_v2'&&manifest.files.length===4,'Incomplete rig package');
for(const [name,digest]of Object.entries(manifest.tools)){
 expect(sha(path.join(here,name))===digest,'Rig tool changed after validation: '+name);
}
for(const entry of [...manifest.files,...manifest.previews]){
 const file=path.join(products,entry.path);
 expect(fs.statSync(file).size===entry.bytes&&sha(file)===entry.sha256,'Published archive changed: '+entry.path);
}
for(const family of ['twig','sprig','bloom','drop']){
 const f=manifest.families[family];
 expect(f.complete&&f.bones===33&&f.clips.length===6&&f.sampledPoses===117&&f.roundtripPoses===18,family+' validation incomplete');
 expect(f.restMaxError<2e-6&&f.uvMaxError<1e-7&&f.panelMaxDrift<1e-4&&f.roundtripMaxMotionError<3e-4,family+' deformation gate failed');
 expect(Object.values(f.loopMaxErrors).every(e=>e<1e-4),family+' loop seam');
 expect(f.surfaceIntegritySha256===manifest.tools['surface_integrity.py'],family+' missing independent surface check');
 expect(f.assessmentScriptSha256===manifest.tools['assess.py'],family+' missing current pose assessment');
 expect(['Jacket','Coat','Overalls'].every(part=>{
  const c=f.cuffChecks?.[part];
  const selection=cuffSelections[family+'/'+part];
  return c&&Number.isInteger(c.sourceVertices)&&c.sourceVertices>=0&&c.maxTorsoWeight>=0&&c.maxTorsoWeight<1e-5
    &&c.authoredVertices===(selection?.vertexIds.length??0)
    &&c.originalZeroArmVertices===(selection?.originalZeroArmVertices??0)
    &&c.sourceVertices>=c.authoredVertices;
 }),family+' cuff ownership gate failed');
 for(const [part,c]of Object.entries(f.contactRepairs)){
  if(!c.geometryChanged)continue;
  expect(c.sourceCorrespondenceMaxError<2e-6&&c.retainedUvMaxError<1e-7,family+'/'+part+' source correspondence');
  expect(c.untouchedSourceVertices+c.movedSourceVertices===c.sourceVertices,family+'/'+part+' unaccounted edits');
  expect(c.maxSourceDisplacement>=0&&c.maxSourceDisplacement<=c.sourceDisplacementLimit+1e-7&&c.sourceDisplacementLimit<=.06,family+'/'+part+' contact displacement');
  expect(c.sourceAreaRemovedFraction>=0&&c.sourceAreaRemovedFraction<=.05,family+'/'+part+' excessive contact repair');
  const retouches=c.sourceAlbedoRetouch??[];
  expect(!cuffSelections[family+'/'+part]||retouches.length>0,family+'/'+part+' missing seam color validation');
  for(const color of retouches){
   const maskLimit={'sprig/Jacket':.09,'bloom/Coat':.025}[family+'/'+part];
   expect(color.outsideMaskMaxError<1e-6&&color.seamDistance===.022
     &&color.changedPixels>0&&color.changedPixels<=color.permittedPixels
     &&maskLimit!==undefined&&color.permittedPixels<maskLimit*color.width*color.height&&color.sourceTriangles>0,
     family+'/'+part+' albedo mask verification failed');
  }
 }
 for(const source of manifest.inputs[family])expect(sha(path.join(repo,source.path))===source.sha256,'Rig source changed: '+source.path);
 files+=f.fbxFiles;parts+=f.parts;
}
expect(files===63&&parts===66&&manifest.ownerImportCheck.complete,'Missing parts / owner import check');
expect(manifest.ownerImportCheck.package===manifest.package&&manifest.ownerImportCheck.previousReleasePreserved&&manifest.ownerImportCheck.staleRevisionRejected,'Missing release migration validation');
expect(manifest.ownerImportCheck.ownerActionsPreserved,'Owner action collision check missing');
console.log('rigging check: OK (4 heroes, 55 items, 63 FBXs, 24 actions; source, tools and products match)');

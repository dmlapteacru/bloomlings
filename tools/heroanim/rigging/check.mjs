// Check published authoring products without Blender, extraction or npm packages.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {fileURLToPath} from 'node:url';

const here=path.dirname(fileURLToPath(import.meta.url));
const repo=path.resolve(here,'../../..');
const products=path.resolve(here,'../products/rig-fix-v1');
const manifest=JSON.parse(fs.readFileSync(path.join(products,'manifest.json')));
const sha=file=>crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
const expect=(ok,message)=>{if(!ok)throw Error(message);};
let files=0,parts=0;
expect(manifest.package==='Bloomlings_RigFix_v1'&&manifest.files.length===4,'Incomplete rig package');
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
 for(const source of manifest.inputs[family])expect(sha(path.join(repo,source.path))===source.sha256,'Rig source changed: '+source.path);
 files+=f.fbxFiles;parts+=f.parts;
}
expect(files===63&&parts===66&&manifest.ownerImportCheck.complete,'Missing parts / owner import check');
console.log('rigging check: OK (4 heroes, 55 items, 63 FBXs, 24 actions; source, tools and products match)');

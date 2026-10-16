import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { sha256 } from './bake.mjs';
const repo=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'../..');
const dir=path.join(repo,'playtest/art/HeroLab');
const manifest=JSON.parse(fs.readFileSync(path.join(dir,'manifest.json')));
const check=(ok,message)=>{if(!ok)throw new Error(message);};
check(sha256(fs.readFileSync(path.join(repo,manifest.source)))===manifest.sourceSha256,'The lab model changed after baking');
check(sha256(fs.readFileSync(path.join(repo,manifest.data)))===manifest.dataSha256,'Frame data changed after baking');
const names=new Set();let bytes=0;
for(const f of manifest.files){
 check(!names.has(f.path),'Duplicate '+f.path);names.add(f.path);
 const data=fs.readFileSync(path.join(dir,f.path));
 check(sha256(data)===f.sha256,'Changed frame '+f.path);bytes+=data.length;
 check(fs.existsSync(path.join(repo,f.record)),'Missing provenance '+f.record);
}
check(bytes<4*1024*1024,'Lab exceeds its 4 MiB source-frame budget');
for(const f of fs.readdirSync(dir))if(f.endsWith('.png'))check(names.has(f),'Unlisted '+f);
for(const clip of ['look','hello','bow','bounce']){
 const frames=manifest.frames.filter(f=>f.clip===clip);
 check(frames.length===60,clip+': expected three seconds at 20 fps');
 check(new Set(frames.map(f=>f.sha256)).size>12,clip+': motion is visually static');
 check(frames[0].sha256===frames.at(-1).sha256,clip+': loop does not return to the reference pose');
 for(const f of frames){const [x,y,w,h]=f.crop;check(x>=4&&y>=4&&x+w<=manifest.cell[0]-4&&y+h<=manifest.cell[1]-4,clip+': clipped frame');}
}
console.log(`Hero lab check: ${names.size} frames, four moving clips with closed seams, ${bytes} bytes; source/data hashes OK.`);

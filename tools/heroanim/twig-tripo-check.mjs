// Verify actual exported GLBs: identical head geometry, UVs, color maps and world placement in both variants.
import fs from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {createHash} from 'node:crypto';
import assert from 'node:assert/strict';

const here=path.dirname(fileURLToPath(import.meta.url)),root=path.resolve(here,'../..'),out=path.join(root,'content/work/twig-tripo-study/build');
const sha=bytes=>createHash('sha256').update(bytes).digest('hex');
const recipe=JSON.parse(fs.readFileSync(path.join(here,'twig-tripo-fit.json')));
const report=JSON.parse(fs.readFileSync(path.join(out,'build-report.json')));
assert.equal(report.recipeSha256,sha(fs.readFileSync(path.join(here,'twig-tripo-fit.json'))));
assert.equal(report.scriptSha256,sha(fs.readFileSync(path.join(here,'twig-tripo-study.py'))));
for(const s of report.sources)assert.equal(sha(fs.readFileSync(path.join(here,'models/twig-tripo/source',s.file))),s.sha256);
for(const [name,p]of Object.entries(report.parts))assert.deepEqual(p.derived,p.source,name+' geometry / UV was changed');

const identity=()=>[1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1];
function multiply(a,b){const c=new Array(16).fill(0);for(let col=0;col<4;col++)for(let row=0;row<4;row++)for(let k=0;k<4;k++)c[col*4+row]+=a[k*4+row]*b[col*4+k];return c;}
function local(n){
 if(n.matrix)return n.matrix;
 const [x,y,z,w]=n.rotation??[0,0,0,1],[sx,sy,sz]=n.scale??[1,1,1],[tx,ty,tz]=n.translation??[0,0,0];
 return [(1-2*(y*y+z*z))*sx,2*(x*y+z*w)*sx,2*(x*z-y*w)*sx,0,2*(x*y-z*w)*sy,(1-2*(x*x+z*z))*sy,2*(y*z+x*w)*sy,0,2*(x*z+y*w)*sz,2*(y*z-x*w)*sz,(1-2*(x*x+y*y))*sz,0,tx,ty,tz,1];
}
function read(file){
 const bytes=fs.readFileSync(path.join(out,file));assert.equal(bytes.readUInt32LE(0),0x46546c67);assert.equal(bytes.readUInt32LE(4),2);
 const length=bytes.readUInt32LE(12),j=JSON.parse(bytes.subarray(20,20+length)),bin=bytes.subarray(28+length);
 const parent=new Map();j.nodes.forEach((n,i)=>(n.children??[]).forEach(child=>parent.set(child,i)));
 const world=i=>parent.has(i)?multiply(world(parent.get(i)),local(j.nodes[i])):local(j.nodes[i]);
 function view(index){const v=j.bufferViews[index];return bin.subarray(v.byteOffset??0,(v.byteOffset??0)+v.byteLength);}
 function attribute(index){const a=j.accessors[index],v=j.bufferViews[a.bufferView];assert.equal(v.byteStride,undefined,'Unexpected interleaved export');return sha(view(a.bufferView).subarray(a.byteOffset??0,(a.byteOffset??0)+a.count*({SCALAR:1,VEC2:2,VEC3:3,VEC4:4}[a.type])*({5120:1,5121:1,5122:2,5123:2,5125:4,5126:4}[a.componentType])));}
 function signature(name){
  const i=j.nodes.findIndex(n=>n.name===name);assert(i>=0,file+' missing '+name);const n=j.nodes[i];
  const primitives=j.meshes[n.mesh].primitives.map(p=>{const m=j.materials[p.material],texture=m.pbrMetallicRoughness?.baseColorTexture;return {positions:attribute(p.attributes.POSITION),normals:attribute(p.attributes.NORMAL),uv:attribute(p.attributes.TEXCOORD_0),indices:attribute(p.indices),colorImage:texture?sha(view(j.images[j.textures[texture.index].source].bufferView)):null,baseColor:m.pbrMetallicRoughness?.baseColorFactor??[1,1,1,1]};});
  return {primitives,world:world(i)};
 }
 const pivot=j.nodes.findIndex(n=>n.name==='TwigHero_HeadPivot');assert(pivot>=0);
 const p=world(pivot);assert(Math.abs(p[12]-recipe.headPivot[0])<1e-6&&Math.abs(p[13]-recipe.headPivot[2])<1e-6&&Math.abs(p[14]+recipe.headPivot[1])<1e-6,'Head pivot must survive export at the neck');
 return {j,signature};
}
for(const e of report.exports)assert.equal(sha(fs.readFileSync(path.join(out,e.file))),e.sha256,e.file+' changed after build');
const base=read('TwigHero_Base.glb');
for(const [file,kind]of [['TwigHero_AcornHead.glb','acorn'],['TwigHero_FlowerCapHead.glb','cap']]){
 const head=read(file);
 for(const name of report.headParts){const a=base.signature(name),b=head.signature(name);assert.deepEqual(b.primitives,a.primitives,file+' must reuse exactly the same face / ears / mouth / UV / paint');assert(b.world.every((v,i)=>Math.abs(v-a.world[i])<1e-6),file+' moved or rescaled the face');}
 for(const n of head.j.nodes.filter(n=>n.mesh!==undefined))assert(n.extras?.tripoSource==='base'?recipe.headParts.includes(n.extras.tripoPart):n.extras?.tripoSource===kind&&recipe[kind].parts.includes(n.extras.tripoPart),'Unexpected body / face from an accessory generation');
}
console.log('Twig Tripo: source archives intact; 14 segments retain geometry and UV; both heads reuse identical head, ears, mouth, color maps, world size and neck pivot.');

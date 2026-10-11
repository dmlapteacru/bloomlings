import fs from 'node:fs';
import path from 'node:path';

const source = path.resolve('tools/heroanim/models/heroes.glb');
const target = path.resolve('output/hero-regeneration/twig-rigged-source.glb');
const data = fs.readFileSync(source);
if (data.toString('ascii', 0, 4) !== 'glTF' || data.readUInt32LE(4) !== 2) throw new Error('Expected glTF 2 GLB');
const jsonLength = data.readUInt32LE(12);
const gltf = JSON.parse(data.toString('utf8', 20, 20 + jsonLength));
const binHeader = 20 + jsonLength;
if (data.toString('ascii', binHeader + 4, binHeader + 8) !== 'BIN\0') throw new Error('Expected BIN chunk');
const bin = data.subarray(binHeader + 8, binHeader + 8 + data.readUInt32LE(binHeader));

const twigRoot = gltf.nodes.findIndex(n => n.name === 'GLB_Twig_Rig');
const twigMesh = gltf.meshes.findIndex(m => m.name === 'GLB_ChatGPT_4_2026_23_55_22');
const twigSkin = gltf.skins.findIndex(s => s.name === 'GLB_Twig_Rig');
if ([twigRoot, twigMesh, twigSkin].some(i => i < 0)) throw new Error('Twig assets missing');

const nodeIds = [];
const visit = id => {
  if (nodeIds.includes(id)) return;
  nodeIds.push(id);
  for (const child of gltf.nodes[id].children ?? []) visit(child);
};
visit(twigRoot);
const nodeMap = new Map(nodeIds.map((id, i) => [id, i]));
const nodes = nodeIds.map(id => structuredClone(gltf.nodes[id]));
for (const node of nodes) {
  if (node.children) node.children = node.children.map(id => nodeMap.get(id));
  if (node.mesh !== undefined) {
    if (node.mesh !== twigMesh) throw new Error('Unexpected mesh under Twig root');
    node.mesh = 0;
  }
  if (node.skin !== undefined) {
    if (node.skin !== twigSkin) throw new Error('Unexpected skin under Twig root');
    node.skin = 0;
  }
}
nodes[0].translation = [0, 0, 0];

const mesh = structuredClone(gltf.meshes[twigMesh]);
const skin = structuredClone(gltf.skins[twigSkin]);
skin.joints = skin.joints.map(id => nodeMap.get(id));
if (skin.joints.some(id => id === undefined)) throw new Error('Twig joints outside root');
if (skin.skeleton !== undefined) skin.skeleton = nodeMap.get(skin.skeleton);

const accessorIds = new Set([skin.inverseBindMatrices]);
for (const primitive of mesh.primitives) {
  for (const id of Object.values(primitive.attributes)) accessorIds.add(id);
  if (primitive.indices !== undefined) accessorIds.add(primitive.indices);
  for (const morph of primitive.targets ?? []) for (const id of Object.values(morph)) accessorIds.add(id);
  if (primitive.material !== undefined && primitive.material !== gltf.meshes[twigMesh].primitives[0].material)
    throw new Error('Multiple Twig materials require extraction update');
  if (primitive.material !== undefined) primitive.material = 0;
}
const accessorList = [...accessorIds].sort((a, b) => a - b);
const accessorMap = new Map(accessorList.map((id, i) => [id, i]));
const accessors = accessorList.map(id => structuredClone(gltf.accessors[id]));
skin.inverseBindMatrices = accessorMap.get(skin.inverseBindMatrices);
for (const primitive of mesh.primitives) {
  for (const key of Object.keys(primitive.attributes)) primitive.attributes[key] = accessorMap.get(primitive.attributes[key]);
  if (primitive.indices !== undefined) primitive.indices = accessorMap.get(primitive.indices);
  for (const morph of primitive.targets ?? []) for (const key of Object.keys(morph)) morph[key] = accessorMap.get(morph[key]);
}

const material = structuredClone(gltf.materials[gltf.meshes[twigMesh].primitives[0].material]);
const textureRefs = [];
const textureSlots = [];
const findTextures = object => {
  if (!object || typeof object !== 'object') return;
  for (const [key, value] of Object.entries(object)) {
    if (key.endsWith('Texture') && value?.index !== undefined) {
      textureRefs.push(value.index);
      textureSlots.push(value);
    } else if (typeof value === 'object') findTextures(value);
  }
};
findTextures(material);
const textureIds = [...new Set(textureRefs)];
const textureMap = new Map(textureIds.map((id, i) => [id, i]));
for (const slot of textureSlots) slot.index = textureMap.get(slot.index);
const textures = textureIds.map(id => structuredClone(gltf.textures[id]));
const imageIds = [...new Set(textures.map(t => t.source))];
const imageMap = new Map(imageIds.map((id, i) => [id, i]));
const images = imageIds.map(id => structuredClone(gltf.images[id]));
const samplerIds = [...new Set(textures.map(t => t.sampler).filter(id => id !== undefined))];
const samplerMap = new Map(samplerIds.map((id, i) => [id, i]));
const samplers = samplerIds.map(id => structuredClone(gltf.samplers[id]));
for (const texture of textures) {
  texture.source = imageMap.get(texture.source);
  if (texture.sampler !== undefined) texture.sampler = samplerMap.get(texture.sampler);
}

const viewIds = new Set();
for (const accessor of accessors) {
  if (accessor.bufferView !== undefined) viewIds.add(accessor.bufferView);
  if (accessor.sparse) {
    viewIds.add(accessor.sparse.indices.bufferView);
    viewIds.add(accessor.sparse.values.bufferView);
  }
}
for (const image of images) if (image.bufferView !== undefined) viewIds.add(image.bufferView);
const viewList = [...viewIds].sort((a, b) => a - b);
const viewMap = new Map(viewList.map((id, i) => [id, i]));
const views = viewList.map(id => structuredClone(gltf.bufferViews[id]));
const chunks = [];
let offset = 0;
for (let i = 0; i < views.length; i++) {
  const original = gltf.bufferViews[viewList[i]];
  if (original.buffer !== 0) throw new Error('Unexpected multiple buffers');
  const bytes = bin.subarray(original.byteOffset ?? 0, (original.byteOffset ?? 0) + original.byteLength);
  views[i].buffer = 0;
  views[i].byteOffset = offset;
  chunks.push(bytes);
  offset += bytes.length;
  const pad = (4 - offset % 4) % 4;
  if (pad) { chunks.push(Buffer.alloc(pad)); offset += pad; }
}
for (const accessor of accessors) {
  if (accessor.bufferView !== undefined) accessor.bufferView = viewMap.get(accessor.bufferView);
  if (accessor.sparse) {
    accessor.sparse.indices.bufferView = viewMap.get(accessor.sparse.indices.bufferView);
    accessor.sparse.values.bufferView = viewMap.get(accessor.sparse.values.bufferView);
  }
}
for (const image of images) if (image.bufferView !== undefined) image.bufferView = viewMap.get(image.bufferView);
const outputBin = Buffer.concat(chunks);

const output = {
  asset: gltf.asset,
  scene: 0,
  scenes: [{ nodes: [0], name: 'Twig' }],
  nodes,
  meshes: [mesh],
  skins: [skin],
  accessors,
  bufferViews: views,
  buffers: [{ byteLength: outputBin.length }],
  materials: [material],
  textures,
  images,
  samplers,
};
const outputJson = Buffer.from(JSON.stringify(output), 'utf8');
const jsonPad = (4 - outputJson.length % 4) % 4;
const outputJsonPadded = Buffer.concat([outputJson, Buffer.alloc(jsonPad, 0x20)]);
const header = Buffer.alloc(12);
header.write('glTF', 0, 'ascii');
header.writeUInt32LE(2, 4);
header.writeUInt32LE(12 + 8 + outputJsonPadded.length + 8 + outputBin.length, 8);
const jsonHeader = Buffer.alloc(8);
jsonHeader.writeUInt32LE(outputJsonPadded.length, 0);
jsonHeader.write('JSON', 4, 'ascii');
const binChunkHeader = Buffer.alloc(8);
binChunkHeader.writeUInt32LE(outputBin.length, 0);
binChunkHeader.write('BIN\0', 4, 'ascii');
fs.writeFileSync(target, Buffer.concat([header, jsonHeader, outputJsonPadded, binChunkHeader, outputBin]));
console.log(`${target}: ${nodeIds.length} nodes, ${skin.joints.length} joints, ${mesh.primitives.length} mesh primitive, ${Math.round((12 + 8 + outputJsonPadded.length + 8 + outputBin.length) / 1024)} KiB`);

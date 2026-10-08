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
  for (const f of hero.frames) {
    const file = path.join(framesDir, f.name + '.png');
    listed.add(f.name + '.png');
    frames++;
    expect(fs.existsSync(file) && sha(file) === f.sha256, `${f.name}.png is missing or changed`);
  }
}
const folder = JSON.parse(fs.readFileSync(path.join(framesDir, 'manifest.json'), 'utf8'));
const folderFiles = new Map(folder.files.map(f => [f.path, f.sha256]));
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
console.log(`heroanim check: OK (${frames} hero frames of ${manifest.heroes.length} heroes, ${layers.outputs.length} Home layers, ${backdrops.outputs.length} backgrounds finished)`);

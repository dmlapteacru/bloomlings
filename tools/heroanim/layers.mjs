// Prepares the owner's layered Home picture (bloomlings_home_assets.zip, 2026-10-02; spec 005 FR-028) for both builds:
// every layer cropped to its visible bounds (so a decoded layer holds no empty rows; alpha under 6 / 255 is dust), the lotus cut out of the fountain's
// back layer (it is drawn again over Bloom, who stands behind it), one soft shadow cut out of the shadow sheet, the
// opaque garden re-encoded as JPEG (quality 90). Writes the pictures into the Backgrounds folder, their boxes into
// client/Assets/Bloomlings/UI/Design/HomeLayersData.cs and the hashes into layers.json.
// Usage: node layers.mjs <folder with 01_home_bg_back.png … 05_home_petals_overlay.png>
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { PNG } from 'pngjs';
import jpeg from 'jpeg-js';
import { sha256 } from './bake.mjs';

const here = path.dirname(fileURLToPath(import.meta.url));
const repo = path.resolve(here, '..', '..');
export const layersDir = path.join(repo, 'client', 'Assets', 'Bloomlings', 'Art', 'Backgrounds', 'Resources', 'Backgrounds');
export const layersDataFile = path.join(repo, 'client', 'Assets', 'Bloomlings', 'UI', 'Design', 'HomeLayersData.cs');
export const layersManifest = path.join(here, 'layers.json');

const inputs = {
  back: '01_home_bg_back.png',
  fountainBack: '02_home_fountain_back.png',
  fountainFront: '03_home_fountain_front.png',
  shadow: '04_home_soft_shadow.png',
  petals: '05_home_petals_overlay.png',
};

function bounds(png, x0 = 0, y0 = 0, x1 = png.width, y1 = png.height, min = 1) {
  let a = x1, b = y1, c = -1, d = -1;
  for (let y = y0; y < y1; y++) for (let x = x0; x < x1; x++) {
    if (png.data[(y * png.width + x) * 4 + 3] < min) continue;
    if (x < a) a = x; if (x > c) c = x; if (y < b) b = y; if (y > d) d = y;
  }
  return { x: a, y: b, w: c - a + 1, h: d - b + 1 };
}

function crop(png, r) {
  const out = new PNG({ width: r.w, height: r.h });
  for (let y = 0; y < r.h; y++) png.data.copy(out.data, y * r.w * 4, ((r.y + y) * png.width + r.x) * 4, ((r.y + y) * png.width + r.x + r.w) * 4);
  return out;
}

const write = p => PNG.sync.write(p, { deflateLevel: 9 });

// Fades a cut-out to nothing over its outer `margin` pixels, so its crop leaves no edge.
function fadeEdges(p, margin) {
  for (let y = 0; y < p.height; y++) for (let x = 0; x < p.width; x++) {
    const d = Math.min(x, y, p.width - 1 - x, p.height - 1 - y) / margin;
    if (d >= 1) continue;
    const t = Math.max(0, d), i = (y * p.width + x) * 4 + 3;
    p.data[i] = Math.round(p.data[i] * t * t * (3 - 2 * t));
  }
  return p;
}

// The lotus of the fountain's back layer: its pink petals (pink: blue above green, red above both), the largest such
// patch, and everything the petals enclose (the glowing middle petal), searched from the top and the sides of a box down
// to the leaves (whose bottom is left open), with a softened edge.
function lotus(src) {
  const X0 = 270, Y0 = 820, W = 340, H = 175;
  const m = new Uint8Array(W * H);
  const at = (x, y) => ((Y0 + y) * src.width + X0 + x) * 4;
  for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) {
    const i = at(x, y), r = src.data[i], g = src.data[i + 1], b = src.data[i + 2], a = src.data[i + 3];
    m[y * W + x] = a > 200 && r > 150 && b - g > -6 && r - g > 6 ? 1 : 0;
  }
  const label = new Int32Array(W * H);
  let best = 0, bestCount = 0, n = 0;
  for (let s = 0; s < W * H; s++) {
    if (!m[s] || label[s]) continue;
    n++;
    let count = 0;
    const stack = [s];
    label[s] = n;
    while (stack.length) {
      const p = stack.pop();
      count++;
      const x = p % W, y = (p / W) | 0;
      for (const [dx, dy] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) {
        const nx = x + dx, ny = y + dy;
        if (nx < 0 || ny < 0 || nx >= W || ny >= H) continue;
        const q = ny * W + nx;
        if (m[q] && !label[q]) { label[q] = n; stack.push(q); }
      }
    }
    if (count > bestCount) { bestCount = count; best = n; }
  }
  for (let s = 0; s < W * H; s++) m[s] = label[s] === best ? 1 : 0;
  const outside = new Uint8Array(W * H);
  const stack = [];
  for (let x = 0; x < W; x++) stack.push(x);
  for (let y = 0; y < H; y++) stack.push(y * W, y * W + W - 1);
  while (stack.length) {
    const p = stack.pop();
    if (outside[p] || m[p]) continue;
    outside[p] = 1;
    const x = p % W, y = (p / W) | 0;
    if (x > 0) stack.push(p - 1);
    if (x < W - 1) stack.push(p + 1);
    if (y > 0) stack.push(p - W);
    if (y < H - 1) stack.push(p + W);
  }
  // A 3 × 3 tent softens the cut edge; the bottom rows fade out over the leaves.
  const out = new PNG({ width: W, height: H });
  for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) {
    let sum = 0, weight = 0;
    for (let dy = -1; dy <= 1; dy++) for (let dx = -1; dx <= 1; dx++) {
      const nx = x + dx, ny = y + dy;
      const w = (2 - Math.abs(dx)) * (2 - Math.abs(dy));
      weight += w;
      if (nx >= 0 && ny >= 0 && nx < W && ny < H && !outside[ny * W + nx]) sum += w;
    }
    const fade = Math.min(1, (H - 1 - y) / 12);
    const i = at(x, y), o = (y * W + x) * 4;
    out.data[o] = src.data[i]; out.data[o + 1] = src.data[i + 1]; out.data[o + 2] = src.data[i + 2];
    out.data[o + 3] = Math.round(src.data[i + 3] * (sum / weight) * fade);
  }
  const b = bounds(out);
  return { picture: crop(out, b), box: { x: X0 + b.x, y: Y0 + b.y, w: b.w, h: b.h } };
}

function main() {
  const dir = process.argv[2];
  if (!dir) throw new Error('usage: node layers.mjs <folder of the owner\'s Home layers>');
  const read = name => { const bytes = fs.readFileSync(path.join(dir, name)); return { bytes, png: PNG.sync.read(bytes) }; };
  const src = Object.fromEntries(Object.entries(inputs).map(([k, f]) => [k, read(f)]));
  const { width, height } = src.back.png;
  for (const [k, v] of Object.entries(src)) if (v.png.width !== width || v.png.height !== height) throw new Error(`${inputs[k]}: not ${width} × ${height}`);

  const outputs = [];
  const put = (name, bytes, box, from) => {
    fs.writeFileSync(path.join(layersDir, name), bytes);
    outputs.push({ name, box, from, sha256: sha256(bytes), bytes: bytes.length });
  };

  const back = jpeg.encode({ data: src.back.png.data, width, height }, 90).data;
  put('home.jpg', back, { x: 0, y: 0, w: width, h: height }, inputs.back);
  for (const [key, name] of [['fountainBack', 'home-fountain-back.png'], ['fountainFront', 'home-fountain-front.png'], ['petals', 'home-petals.png']]) {
    const b = bounds(src[key].png, 0, 0, width, height, 6); // alpha under 6 / 255 is stray dust
    put(name, write(crop(src[key].png, b)), b, inputs[key]);
  }
  const l = lotus(src.fountainBack.png);
  put('home-lotus.png', write(l.picture), l.box, inputs.fountainBack + ' (the lotus)');
  // The shadow sheet holds four soft shadows; the front left one (the widest) serves every hero, scaled to its width.
  const s = bounds(src.shadow.png, 0, 880, Math.floor(width / 2), 1180, 10);
  put('home-shadow.png', write(fadeEdges(crop(src.shadow.png, s), 14)), s, inputs.shadow + ' (the front left shadow)');

  const name = { 'home.jpg': 'Back', 'home-fountain-back.png': 'FountainBack', 'home-lotus.png': 'Lotus', 'home-fountain-front.png': 'FountainFront', 'home-shadow.png': 'Shadow', 'home-petals.png': 'Petals' };
  const lines = [
    '// <auto-generated>',
    '// Written by tools/heroanim/layers.mjs from the owner\'s Home layers; do not edit. `node tools/heroanim/check.mjs` proves',
    '// it still matches the pictures.',
    '// </auto-generated>',
    'namespace Bloomlings.Client.UI.Design',
    '{',
    '    public static partial class HomeLayers',
    '    {',
    '        /// <summary>The layered picture\'s width in pixels (every layer\'s box is in its pixels).</summary>',
    `        public const int PictureWidth = ${width};`,
    '',
    '        /// <summary>The layered picture\'s height in pixels.</summary>',
    `        public const int PictureHeight = ${height};`,
    '',
  ];
  for (const o of outputs) {
    lines.push(`        /// <summary><c>${o.name}</c> (from <c>${o.from}</c>): its box in the picture.</summary>`);
    lines.push(`        public static readonly PictureBox ${name[o.name]} = new PictureBox("${o.name.replace(/\.(png|jpg)$/, '')}", ${o.box.x}, ${o.box.y}, ${o.box.w}, ${o.box.h});`);
    lines.push('');
  }
  lines.pop();
  lines.push('    }', '}');
  const data = lines.join('\n') + '\n';
  fs.writeFileSync(layersDataFile, data);
  fs.writeFileSync(layersManifest, JSON.stringify({
    inputs: Object.fromEntries(Object.entries(inputs).map(([k, f]) => [f, sha256(src[k].bytes)])),
    data: path.relative(repo, layersDataFile), dataSha256: sha256(Buffer.from(data)), outputs,
  }, null, 1) + '\n');
  for (const o of outputs) console.log(`${o.name}: ${o.box.w} × ${o.box.h} at ${o.box.x}, ${o.box.y}, ${(o.bytes / 1024).toFixed(0)} KB`);
}

main();

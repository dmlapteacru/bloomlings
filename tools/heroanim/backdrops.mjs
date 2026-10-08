// The backgrounds' finish (spec 005 FR-049, the owner, 2026-10-08: "on every screen blur the background pictures a little,
// just a touch, and darken them: they take too much focus"). The owner's backgrounds and the Home layers stay as delivered
// in backgrounds/ (the Home layers as layers.mjs and the hand fitting of SOURCE.md wrote them); this script writes what the
// game shows into the Backgrounds folder: every opaque picture blurred by a Gaussian of `finish.blur` of its width, every
// picture's color multiplied by `finish.dim` (its alpha untouched). The Home fountain's layers are dimmed only, so the
// heroes' feet stay sharp on their stones and the scene keeps one light; the heroes, the UI and the board are not touched.
// Deterministic: the same sources give the same bytes. The hashes go to backdrops.json; check.mjs proves them.
// Usage: node backdrops.mjs
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { PNG } from 'pngjs';
import jpeg from 'jpeg-js';
import { sha256 } from './bake.mjs';

const here = path.dirname(fileURLToPath(import.meta.url));
const repo = path.resolve(here, '..', '..');
/** The owner's pictures as delivered (layers.mjs writes the Home layers here). */
export const sourcesDir = path.join(here, 'backgrounds');
/** What the game shows (both builds embed this folder). */
export const backgroundsDir = path.join(repo, 'client', 'Assets', 'Bloomlings', 'Art', 'Backgrounds', 'Resources', 'Backgrounds');
export const backdropsManifest = path.join(here, 'backdrops.json');

/**
 * The finish: the Gaussian's sigma as a share of a picture's width (3 px of a 1080 px screen; the Home garden had 4 in
 * the owner's tuning of 2026-10-05), and the factor on every color channel (14% darker).
 */
export const finish = { blur: 3 / 1080, dim: 0.86 };

/** Every picture of the Backgrounds folder: whether it is blurred and dimmed (an opaque background), or dimmed only. */
export const pictures = [
  { name: 'home.jpg', blur: true, dim: true },
  { name: 'home-fountain-back.png', blur: false, dim: true },
  { name: 'home-lotus.png', blur: false, dim: true },
  { name: 'home-fountain-front.png', blur: false, dim: true },
  { name: 'home-shadow.png', blur: false, dim: false },
  { name: 'gameplay-daylight.jpg', blur: true, dim: true },
  { name: 'gameplay-pond.jpg', blur: true, dim: true },
  { name: 'gameplay-orchard.jpg', blur: true, dim: true },
  { name: 'gameplay-moonlit.jpg', blur: true, dim: true },
  { name: 'wardrobe.jpg', blur: true, dim: true },
  { name: 'win.jpg', blur: true, dim: true },
];

/** A separable Gaussian of `sigma` pixels over a picture's color (its alpha untouched), the edges clamped; in place. */
export function blur(img, sigma) {
  if (!(sigma > 0)) return img;
  const { width: w, height: h, data } = img;
  const r = Math.ceil(sigma * 3);
  const k = Array.from({ length: 2 * r + 1 }, (_, i) => Math.exp(-((i - r) ** 2) / (2 * sigma * sigma)));
  const sum = k.reduce((a, b) => a + b, 0);
  const kn = k.map(v => v / sum);
  const tmp = new Float32Array(w * h * 3);
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) for (let c = 0; c < 3; c++) {
    let v = 0;
    for (let i = -r; i <= r; i++) v += kn[i + r] * data[(y * w + Math.min(w - 1, Math.max(0, x + i))) * 4 + c];
    tmp[(y * w + x) * 3 + c] = v;
  }
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) for (let c = 0; c < 3; c++) {
    let v = 0;
    for (let i = -r; i <= r; i++) v += kn[i + r] * tmp[(Math.min(h - 1, Math.max(0, y + i)) * w + x) * 3 + c];
    data[(y * w + x) * 4 + c] = Math.max(0, Math.min(255, Math.round(v)));
  }
  return img;
}

/** Multiplies a picture's color channels by `k` (its alpha untouched); in place. */
export function dim(img, k) {
  const d = img.data;
  for (let i = 0; i < d.length; i += 4) for (let c = 0; c < 3; c++) d[i + c] = Math.round(d[i + c] * k);
  return img;
}

function main() {
  const out = [];
  for (const p of pictures) {
    const bytes = fs.readFileSync(path.join(sourcesDir, p.name));
    const jpg = p.name.endsWith('.jpg');
    let result = bytes;
    if (p.blur || p.dim) {
      const img = jpg ? jpeg.decode(bytes, { useTArray: true }) : PNG.sync.read(bytes);
      const pic = { width: img.width, height: img.height, data: Buffer.from(img.data) };
      if (p.blur) blur(pic, finish.blur * pic.width);
      if (p.dim) dim(pic, finish.dim);
      if (jpg) {
        result = Buffer.from(jpeg.encode(pic, 90).data);
      } else {
        const png = new PNG({ width: pic.width, height: pic.height });
        pic.data.copy(png.data);
        result = PNG.sync.write(png, { deflateLevel: 9 });
      }
    }
    fs.writeFileSync(path.join(backgroundsDir, p.name), result);
    out.push({ name: p.name, blur: p.blur, dim: p.dim, sourceSha256: sha256(bytes), sha256: sha256(result), bytes: result.length });
    console.log(`${p.name}: ${p.blur ? 'blurred, ' : ''}${p.dim ? 'dimmed' : 'as delivered'}, ${(result.length / 1024).toFixed(0)} KB`);
  }
  fs.writeFileSync(backdropsManifest, JSON.stringify({
    finish: { blur: Number(finish.blur.toFixed(6)), dim: finish.dim },
    sources: path.relative(repo, sourcesDir), outputs: out,
  }, null, 1) + '\n');
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) main();

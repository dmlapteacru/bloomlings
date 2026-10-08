// The backgrounds' saturation (spec 005 FR-031). From 2026-10-04 (the owner chose 70% after trying 60%, 80% and 70%)
// every owner background's mean saturation was brought to 70% of the animated heroes' mean, the heroes being 100%; since
// 2026-10-08 the owner keeps the backgrounds' colors as delivered (`ladder.background` null): the script only measures. The UI, the
// heroes, the characters and the board's pieces stay as they are. A picture's saturation is scaled in HSL with its
// lightness and hue kept (each channel moved toward the pixel's (max + min) / 2). A picture already at or under its
// share stays as it is, so a second run changes nothing. The Home layers are one scene: layers.mjs scales them together
// by home.jpg's factor (`sceneFactor`).
// Usage: node saturation.mjs   (writes the pictures in place and saturation.json, the measurements)
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { PNG } from 'pngjs';
import jpeg from 'jpeg-js';

const here = path.dirname(fileURLToPath(import.meta.url));
const repo = path.resolve(here, '..', '..');
const art = path.join(repo, 'client', 'Assets', 'Bloomlings', 'Art');

/**
 * The backgrounds' share of the heroes' mean saturation (spec 005 FR-031): null keeps the owner's colors as delivered
 * (since 2026-10-08; it was 0.7).
 */
export const ladder = { background: null };

/** The backgrounds (the Home layers are layers.mjs's), relative to the Art folder. */
export const pictures = {
  background: ['win.jpg', 'wardrobe.jpg', 'gameplay-daylight.jpg', 'gameplay-moonlit.jpg', 'gameplay-orchard.jpg', 'gameplay-pond.jpg']
    .map(f => `Backgrounds/Resources/Backgrounds/${f}`),
};

export function decode(bytes, jpg) {
  return jpg ? jpeg.decode(bytes, { useTArray: true }) : PNG.sync.read(bytes);
}

/** The mean HSL saturation of a picture's pixels at least half opaque, weighted by their opacity (every 4th pixel). */
export function meanSaturation(img) {
  const d = img.data;
  let sum = 0, weight = 0;
  for (let i = 0; i < d.length; i += 16) {
    const a = d[i + 3] / 255;
    if (a < 0.5) continue;
    const mx = Math.max(d[i], d[i + 1], d[i + 2]) / 255, mn = Math.min(d[i], d[i + 1], d[i + 2]) / 255;
    const l = (mx + mn) / 2, c = mx - mn;
    sum += (c === 0 ? 0 : c / (1 - Math.abs((2 * l) - 1))) * a;
    weight += a;
  }
  return weight > 0 ? sum / weight : 0;
}

/** Scales a picture's HSL saturation by `k` in place, its lightness and hue kept, its alpha untouched. */
export function scale(img, k) {
  const d = img.data;
  for (let i = 0; i < d.length; i += 4) {
    const l = (Math.max(d[i], d[i + 1], d[i + 2]) + Math.min(d[i], d[i + 1], d[i + 2])) / 2;
    for (let c = 0; c < 3; c++) d[i + c] = Math.max(0, Math.min(255, Math.round(l + (k * (d[i + c] - l)))));
  }
  return img;
}

/** The animated heroes' mean saturation: the mean of each hero's frames' means (every 10th frame). */
export function heroesMean() {
  const dir = path.join(art, 'Heroes', 'Resources', 'HeroMotion');
  const byHero = new Map();
  for (const f of fs.readdirSync(dir).filter(f => f.endsWith('.png')).sort()) {
    const hero = f.split('-')[0];
    if (!byHero.has(hero)) byHero.set(hero, []);
    byHero.get(hero).push(f);
  }
  const means = [...byHero.values()].map(list => {
    const picks = list.filter((_, i) => i % 10 === 0);
    return picks.reduce((s, f) => s + meanSaturation(decode(fs.readFileSync(path.join(dir, f)), false)), 0) / picks.length;
  });
  return means.reduce((a, b) => a + b, 0) / means.length;
}

/** The factor that brings a picture of mean saturation `mean` to `share` of the heroes' `heroes`: at most 1; 1 without a share. */
export function sceneFactor(mean, share, heroes) {
  if (share == null) return 1;
  return mean > 0 ? Math.min(1, (share * heroes) / mean) : 1;
}

function main() {
  const heroes = heroesMean();
  const report = { heroesMean: Number(heroes.toFixed(4)), ladder, pictures: [] };
  for (const [kind, files] of Object.entries(pictures)) {
    for (const file of files) {
      const full = path.join(art, file);
      const jpg = file.endsWith('.jpg');
      const img = decode(fs.readFileSync(full), jpg);
      const before = meanSaturation(img);
      const k = sceneFactor(before, ladder[kind], heroes);
      if (k < 0.98) {
        scale(img, k);
        fs.writeFileSync(full, jpg ? jpeg.encode({ data: img.data, width: img.width, height: img.height }, 90).data : PNG.sync.write(img, { deflateLevel: 9 }));
      }

      const after = k < 0.98 ? meanSaturation(img) : before;
      report.pictures.push({ file, class: kind, factor: Number((k < 0.98 ? k : 1).toFixed(3)), before: Number((before / heroes).toFixed(3)), after: Number((after / heroes).toFixed(3)) });
      console.log(`${file}: ${(100 * before / heroes).toFixed(0)}% → ${(100 * after / heroes).toFixed(0)}% of the heroes (×${(k < 0.98 ? k : 1).toFixed(2)})`);
    }
  }

  fs.writeFileSync(path.join(here, 'saturation.json'), JSON.stringify(report, null, 1) + '\n');
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  main();
}

// A 256-entry palette for a set of straight-alpha RGBA pictures and indexed PNG writing, so the hero frames stay small
// (one palette per hero, shared by all its frames, so a color never flickers from frame to frame).
import zlib from 'node:zlib';

const CRC = new Int32Array(256).map((_, n) => { let c = n; for (let k = 0; k < 8; k++) c = c & 1 ? 0xEDB88320 ^ (c >>> 1) : c >>> 1; return c; });
function crc32(buf) { let c = -1; for (let i = 0; i < buf.length; i++) c = CRC[(c ^ buf[i]) & 0xFF] ^ (c >>> 8); return (c ^ -1) >>> 0; }
function chunk(type, data) {
  const out = Buffer.alloc(12 + data.length);
  out.writeUInt32BE(data.length, 0);
  out.write(type, 4, 'ascii');
  data.copy(out, 8);
  out.writeUInt32BE(crc32(out.subarray(4, 8 + data.length)), 8 + data.length);
  return out;
}

// Median cut over the pixels' premultiplied colors (each opaque-ish pixel once per sample step), then a few rounds of
// k-means. Entry 0 is fully transparent.
export function palette(pictures, size = 256) {
  const samples = [];
  for (const p of pictures) {
    for (let i = 0; i < p.data.length; i += 4 * 7) {
      const a = p.data[i + 3];
      if (a === 0) continue;
      samples.push([p.data[i] * a / 255, p.data[i + 1] * a / 255, p.data[i + 2] * a / 255, a]);
    }
  }
  let boxes = [samples];
  while (boxes.length < size - 1) {
    let best = -1, bestRange = -1, bestAxis = 0;
    boxes.forEach((b, bi) => {
      if (b.length < 2) return;
      for (let axis = 0; axis < 4; axis++) {
        let lo = Infinity, hi = -Infinity;
        for (const s of b) { if (s[axis] < lo) lo = s[axis]; if (s[axis] > hi) hi = s[axis]; }
        const range = (hi - lo) * Math.sqrt(b.length);
        if (range > bestRange) { bestRange = range; best = bi; bestAxis = axis; }
      }
    });
    if (best < 0) break;
    const b = boxes[best].sort((x, y) => x[bestAxis] - y[bestAxis]);
    const mid = b.length >> 1;
    boxes.splice(best, 1, b.slice(0, mid), b.slice(mid));
  }
  let centers = boxes.map(b => { const c = [0, 0, 0, 0]; for (const s of b) for (let k = 0; k < 4; k++) c[k] += s[k]; return c.map(v => v / b.length); });
  for (let round = 0; round < 4; round++) {
    const sum = centers.map(() => [0, 0, 0, 0, 0]);
    const lut = new Map();
    for (const s of samples) {
      const key = (s[0] >> 2) << 18 | (s[1] >> 2) << 12 | (s[2] >> 2) << 6 | (s[3] >> 2);
      let j = lut.get(key);
      if (j === undefined) { j = nearest(centers, s); lut.set(key, j); }
      const t = sum[j]; for (let k = 0; k < 4; k++) t[k] += s[k]; t[4]++;
    }
    centers = centers.map((c, j) => sum[j][4] ? sum[j].slice(0, 4).map(v => v / sum[j][4]) : c);
  }
  return [[0, 0, 0, 0], ...centers.map(c => c.map(v => Math.round(v)))];
}

function nearest(centers, s) {
  let best = 0, bd = Infinity;
  for (let j = 0; j < centers.length; j++) {
    const c = centers[j];
    const d = (c[0] - s[0]) ** 2 + (c[1] - s[1]) ** 2 + (c[2] - s[2]) ** 2 + 1.5 * (c[3] - s[3]) ** 2;
    if (d < bd) { bd = d; best = j; }
  }
  return best;
}

// Indexes a picture on the palette (premultiplied entries) with Floyd–Steinberg dithering of the opaque parts; fully
// transparent pixels stay entry 0.
export function indexed(p, pal) {
  const W = p.width, H = p.height, idx = new Uint8Array(W * H);
  const err = new Float32Array((W + 2) * 2 * 4);
  const lut = new Map();
  const centers = pal.slice(1);
  for (let y = 0; y < H; y++) {
    const cur = (y & 1) * (W + 2) * 4, nxt = ((y + 1) & 1) * (W + 2) * 4;
    err.fill(0, nxt, nxt + (W + 2) * 4);
    for (let x = 0; x < W; x++) {
      const i = (y * W + x) * 4, a = p.data[i + 3];
      if (a === 0) { idx[y * W + x] = 0; continue; }
      const e = cur + (x + 1) * 4;
      const s = [p.data[i] * a / 255 + err[e], p.data[i + 1] * a / 255 + err[e + 1], p.data[i + 2] * a / 255 + err[e + 2], a + err[e + 3]].map(v => Math.min(255, Math.max(0, v)));
      const key = (s[0] >> 2) << 18 | (s[1] >> 2) << 12 | (s[2] >> 2) << 6 | (s[3] >> 2);
      let j = lut.get(key);
      if (j === undefined) { j = nearest(centers, s); lut.set(key, j); }
      idx[y * W + x] = j + 1;
      const c = centers[j];
      for (let k = 0; k < 4; k++) {
        const d = s[k] - c[k];
        err[e + 4 + k] += d * 7 / 16;
        err[nxt + x * 4 + k] += d * 3 / 16;
        err[nxt + (x + 1) * 4 + k] += d * 5 / 16;
        err[nxt + (x + 2) * 4 + k] += d / 16;
      }
    }
  }
  return idx;
}

// An 8-bit palette PNG: the palette's premultiplied entries turned back to straight colors, their alphas in tRNS.
export function writePng8(width, height, idx, pal) {
  const ihdr = Buffer.alloc(13);
  ihdr.writeUInt32BE(width, 0); ihdr.writeUInt32BE(height, 4);
  ihdr[8] = 8; ihdr[9] = 3; ihdr[10] = 0; ihdr[11] = 0; ihdr[12] = 0;
  const plte = Buffer.alloc(pal.length * 3), trns = Buffer.alloc(pal.length);
  pal.forEach((c, j) => {
    const a = c[3];
    for (let k = 0; k < 3; k++) plte[j * 3 + k] = a ? Math.min(255, Math.round(c[k] * 255 / a)) : 0;
    trns[j] = a;
  });
  const raw = Buffer.alloc((width + 1) * height);
  for (let y = 0; y < height; y++) { raw[y * (width + 1)] = 0; Buffer.from(idx.buffer, idx.byteOffset + y * width, width).copy(raw, y * (width + 1) + 1); }
  const idat = zlib.deflateSync(raw, { level: 9 });
  return Buffer.concat([Buffer.from([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]), chunk('IHDR', ihdr), chunk('PLTE', plte), chunk('tRNS', trns), chunk('IDAT', idat), chunk('IEND', Buffer.alloc(0))]);
}

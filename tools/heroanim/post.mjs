// The Home set's finish (spec 005 FR-036, the owner's tuning of 2026-10-05 in the Home constructor): the same filters the
// constructor ran over the heroes' frames, in its order, on a straight-alpha RGBA picture in place: a 3 × 3 sharpening
// kernel (centre 1 + 4a, its four neighbours −a) on the colors only, transparent neighbours counting as black, then the
// contrast about the middle grey, then the saturation (the CSS matrix, in sRGB). Alpha stays as it is.
// heroes.json `home`: { "sharpen": a, "contrast": c, "saturation": s }.
export function finish(png, { sharpen = 0, contrast = 1, saturation = 1 } = {}) {
  const { width: w, height: h, data } = png;
  const src = Float32Array.from(data);
  const at = (x, y, c) => (x < 0 || y < 0 || x >= w || y >= h || src[(y * w + x) * 4 + 3] === 0) ? 0 : src[(y * w + x) * 4 + c];
  const s = saturation;
  const m = [
    0.213 + 0.787 * s, 0.715 - 0.715 * s, 0.072 - 0.072 * s,
    0.213 - 0.213 * s, 0.715 + 0.285 * s, 0.072 - 0.072 * s,
    0.213 - 0.213 * s, 0.715 - 0.715 * s, 0.072 + 0.928 * s,
  ];
  for (let y = 0; y < h; y++) {
    for (let x = 0; x < w; x++) {
      const i = (y * w + x) * 4;
      if (data[i + 3] === 0) continue;
      const rgb = [0, 1, 2].map(c => {
        let v = sharpen > 0
          ? (1 + 4 * sharpen) * src[i + c] - sharpen * (at(x - 1, y, c) + at(x + 1, y, c) + at(x, y - 1, c) + at(x, y + 1, c))
          : src[i + c];
        v = Math.max(0, Math.min(255, v));
        return Math.max(0, Math.min(255, (v - 127.5) * contrast + 127.5));
      });
      for (let c = 0; c < 3; c++) {
        const v = m[c * 3] * rgb[0] + m[c * 3 + 1] * rgb[1] + m[c * 3 + 2] * rgb[2];
        data[i + c] = Math.max(0, Math.min(255, Math.round(v)));
      }
    }
  }
  return png;
}

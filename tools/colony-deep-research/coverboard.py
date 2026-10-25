"""A colony-flow.com level cover (the site's board picture, about 700-1100 px wide) → letter grid and palette.
Grid: cfboard's pitch; the beige panel inside the wooden frame (found from the picture's edges); the cube field (the
rows and columns where enough differs from the beige), its columns and rows from the pitch (the bottom row's front face
left out). Every cube is sampled at its top face; a beige or shadowed-beige face is no cube. Cells outside the site's
watermark band are clustered in Lab; a cell inside the band (a dark translucent box with white "Colony-Flow.com" text
across the middle) is sampled at the darkest quarter of its top face (the letters left out) and given the cluster that,
darkened by 0.75-1, explains it best. Light background cubes count as cubes (the late levels' pale grids).
usage: python3 -I coverboard.py <cover.webp|png> <out_prefix> [cluster_threshold]
writes <out_prefix>.json {w, h, rows (top first), palette [{letter, rgb, lab, cells}], band: cells repaired}"""
import sys, os, json
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(__file__))
import cfboard as B
src, out = sys.argv[1], sys.argv[2]; thr = float(sys.argv[3]) if len(sys.argv) > 3 else 9
png = out + '_cover.png'; Image.open(src).convert('RGB').save(png)
B.analyse(png); px, py = B.GEOM[3], B.GEOM[4]   # cfboard's pitch is reliable; its phase and extent are not used
a = np.asarray(Image.open(png)).astype(float); H, W = a.shape[:2]
L = B.lab(a)
# the panel: the beige margin inside the wooden frame, found walking in from the picture's edges (from the middle, a
# board's own orange or brown cubes would pass for wood)
bg = np.array([91.0, 1.0, 10.0])
def beige(l): return np.linalg.norm(l - bg) < 7
cy, cx = H // 2, W // 2
left = next(x for x in range(W // 3) if all(beige(L[cy + dy, x]) for dy in (-40, 0, 40)))
right = next(x for x in range(W - 1, 2 * W // 3, -1) if all(beige(L[cy + dy, x]) for dy in (-40, 0, 40)))
top = next(y for y in range(H // 3) if all(beige(L[y, cx + dx]) for dx in (-60, 0, 60)))
bottom = next(y for y in range(H - 1, 2 * H // 3, -1) if all(beige(L[y, cx + dx]) for dx in (-60, 0, 60)))
pad = 4; px0, px1, py0, py1 = left + pad, right - pad, top + pad, bottom - pad
panel = L[py0:py1, px0:px1]
bg = np.median(np.concatenate([panel[:6].reshape(-1, 3), panel[:, :6].reshape(-1, 3)]), 0)   # this picture's own beige
# the field: the span of the rows and columns where enough of the panel is not beige (its soft shading and the
# watermark's box over the margin stay below that); a full board fills it, a picture on an empty panel does not
mask = np.linalg.norm(panel - bg, axis=2) > 12
xsx = np.where(mask.mean(0) > .04)[0]; ysx = np.where(mask.mean(1) > .04)[0]
fx0, fx1, fy0, fy1 = xsx.min(), xsx.max(), ysx.min(), ysx.max()
plain = bool(np.mean(~mask[fy0:fy1, fx0:fx1]) > .05)
fx0 += px0; fx1 += px0; fy0 += py0; fy1 += py0
w = int(round((fx1 - fx0 + 1) / px)); h = int(round((fy1 - fy0 + 1 - .28 * py) / py))
px, py = (fx1 - fx0 + 1) / w, (fy1 - fy0 + 1 - .28 * py) / h
cells = {}
for j in range(h):
    for i in range(w):
        x0, y0 = fx0 + i * px, fy0 + j * py
        x1, x2, y1, y2 = int(x0 + px * .3), int(x0 + px * .7), int(y0 + py * .25), int(y0 + py * .6)
        patch = a[y1:y2, x1:x2].reshape(-1, 3)
        fy, fx = (y0 + py * .4) / H, (x0 + px * .5) / W
        band = 0.40 < fy < 0.60 and 0.12 < fx < 0.88
        if band:   # leave out the watermark's white letters: the darkest quarter of the top face
            lum = patch.sum(1); patch = patch[lum <= np.percentile(lum, 25)]
        med = np.median(patch, 0)
        lm = B.lab(med)
        if np.linalg.norm(lm[1:] - bg[1:]) < 8 and bg[0] - 30 < lm[0] < bg[0] + 5: continue   # empty panel (or a cube's shadow on it)
        cells[(i, j)] = (med, band)
GEOM = dict(field=[int(fx0), int(fy0), int(fx1), int(fy1)], pitch=[round(px, 2), round(py, 2)], plain=bool(plain))
lab = lambda c: B.lab(np.array(c, float))
cent, memb = [], []
for k, (med, band) in sorted(cells.items(), key=lambda kv: kv[0][::-1]):
    if band: continue
    l = lab(med)
    if cent:
        d = np.linalg.norm(np.array(cent) - l, axis=1); b = int(np.argmin(d))
        if d[b] < thr: memb[b].append(k); cent[b] = np.mean([lab(cells[x][0]) for x in memb[b]], 0); continue
    cent.append(l); memb.append([k])
order = [q for q in sorted(range(len(cent)), key=lambda q: -len(memb[q])) if len(memb[q]) >= 3][:52]
rgb = [np.mean([cells[k][0] for k in memb[q]], 0) for q in order]
letters = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz'
label = {}
def nearest(med, band):
    best, bq = 1e9, 0
    for n, c in enumerate(rgb):
        # the band is a dark translucent box (it multiplies a colour by about 0.85) with white letters on it
        cands = [c] if not band else [k * c for k in np.linspace(.75, 1, 6)]
        for cand in cands:
            d = np.linalg.norm(lab(cand) - lab(med))
            if d < best: best, bq = d, n
    return bq
for k, (med, band) in cells.items(): label[k] = letters[nearest(med, band)]
rows = [''.join(label.get((i, j), '.') for i in range(w)) for j in range(h)]
pal = [dict(letter=letters[n], rgb='#%02x%02x%02x' % tuple(int(v) for v in c), lab=[round(float(v)) for v in lab(c)],
            cells=sum(r.count(letters[n]) for r in rows)) for n, c in enumerate(rgb)]
json.dump(dict(w=int(w), h=int(h), rows=rows, palette=pal, band=sum(1 for v in cells.values() if v[1]), **GEOM), open(out + '.json', 'w'), indent=1)
print(w, 'x', h, 'colours', len(pal), 'band cells', sum(1 for v in cells.values() if v[1]))
for p in pal: print(p['letter'], p['cells'], p['rgb'], p['lab'])

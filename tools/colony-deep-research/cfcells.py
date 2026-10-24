"""Colony Flow board frame → letter grid, sampling every cube (unlike gridmap.py, which treats the most common colour
as background and so drops a sky that covers the board). The cream board frame is taken from the crop's corner;
every other cube is clustered in Lab. A cube with a printed "?" (a mystery tile) shows as '?'.
usage: python3 -I cfcells.py <frame.png> <x0,y0,x1,y1> <out_prefix> [cluster_threshold]
writes <out_prefix>_crop.png and <out_prefix>.json {w, h, rows (top first), palette, pitch}"""
import sys, json, os
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(__file__))
import cfboard as B
src, box, out = sys.argv[1], [int(v) for v in sys.argv[2].split(',')], sys.argv[3]
thr = float(sys.argv[4]) if len(sys.argv) > 4 else 12
crop = Image.open(src).convert('RGB').crop(box); crop.save(out + '_crop.png')
B.analyse(out + '_crop.png')
m, xs, ys, px, py, i0, i1, j0, j1, keep = B.GEOM
a = np.asarray(Image.open(out + '_crop.png').convert('RGB')).astype(float); H, W = a.shape[:2]
# cfboard's ranges stop short of the crop's edges: keep its pitch and phase, cover the whole crop
xs = np.arange(xs[0] - px * np.floor(xs[0] / px), W - px * 0.7, px); ys = np.arange(ys[0] - py * np.floor(ys[0] / py), H - py * 0.6, py)
frame = B.lab(np.median(a[2:8, 2:8].reshape(-1, 3), 0))
cells = {}
for j, y0 in enumerate(ys):
    for i, x0 in enumerate(xs):
        x1, x2 = int(x0 + px * 0.3), int(x0 + px * 0.7); y1, y2 = int(y0 + py * 0.25), int(y0 + py * 0.6)
        if x1 < 0 or y1 < 0 or x2 > W or y2 > H: continue
        patch = a[y1:y2, x1:x2].reshape(-1, 3); med = np.median(patch, 0); l = B.lab(med)
        if np.hypot(l[1] - frame[1], l[2] - frame[2]) < 7 and abs(l[0] - frame[0]) < 7: continue  # the board frame
        cells[(i, j)] = (l, med, patch.std(0).mean())
I = [k[0] for k in cells]; J = [k[1] for k in cells]
ia, ib, ja, jb = min(I), max(I), min(J), max(J)
# the bottom row of cubes shows front faces: drop a last row that is only darker copies of the row above
last = [k for k in cells if k[1] == jb]; above = {k[0]: cells[k][0] for k in cells if k[1] == jb - 1}
if last and np.mean([cells[k][0][0] - above.get(k[0], cells[k][0])[0] < -8 for k in last]) > .7: jb -= 1
cent, memb = [], []
for k in sorted(cells):
    if not (ia <= k[0] <= ib and ja <= k[1] <= jb): continue
    l, med, sd = cells[k]
    if sd > 38 and np.hypot(l[1], l[2]) < 12: memb.append(None) if False else None; cells[k] = (l, med, sd, '?'); continue
    if cent:
        d = np.linalg.norm(np.array(cent) - l, axis=1); b = int(np.argmin(d))
        if d[b] < thr: memb[b].append(k); cent[b] = np.mean([cells[x][0] for x in memb[b]], 0); continue
    cent.append(l.copy()); memb.append([k])
order = sorted(range(len(cent)), key=lambda q: -len(memb[q]))
letters = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ'; label = {}; pal = []
for n, q in enumerate(order):
    L = letters[n] if len(memb[q]) >= 2 else '!'
    for k in memb[q]: label[k] = L
    if L != '!': pal.append(dict(letter=L, cells=len(memb[q]), lab=[round(float(v)) for v in cent[q]], rgb='#%02x%02x%02x' % tuple(int(v) for v in np.mean([cells[k][1] for k in memb[q]], 0))))
rows = [''.join('?' if len(cells.get((i, j), ()))==4 else label.get((i, j), '.') for i in range(ia, ib + 1)) for j in range(ja, jb + 1)]
res = dict(w=ib - ia + 1, h=jb - ja + 1, rows=rows, palette=pal, pitch=[round(px, 2), round(py, 2)])
json.dump(res, open(out + '.json', 'w'), indent=1)
print(res['w'], 'x', res['h'], 'pitch', res['pitch'])
for p in pal: print(p['letter'], p['cells'], p['rgb'], p['lab'])
print('\n'.join(rows))

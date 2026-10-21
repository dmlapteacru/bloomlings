"""Turn a Colony Flow board crop into a letter grid, palette and colour-layer depths.
usage: python3 -I gridmap.py <frame.png> <x0,y0,x1,y1> <out_prefix> [cluster_threshold]"""
import sys, json, collections
import numpy as np
from PIL import Image
sys.path.insert(0, __import__('os').path.dirname(__file__))
import cfboard as B

src, box, out = sys.argv[1], [int(v) for v in sys.argv[2].split(',')], sys.argv[3]
thr = float(sys.argv[4]) if len(sys.argv) > 4 else 12
Image.open(src).convert('RGB').crop(box).save(out + '_crop.png')
r = B.analyse(out + '_crop.png')
m, xs, ys, px, py, i0, i1, j0, j1, keep = B.GEOM
W, H = i1 - i0 + 1, j1 - j0 + 1
cells = {(c[0] - i0, c[1] - j0): B.lab(c[2]) for c in keep}
rgb = {(c[0] - i0, c[1] - j0): c[2] for c in keep}
# cluster in Lab
cent, memb = [], []
for k, v in cells.items():
    if cent:
        d = np.linalg.norm(np.array(cent) - v, axis=1); b = int(np.argmin(d))
        if d[b] < thr: memb[b].append(k); cent[b] = np.mean([cells[x] for x in memb[b]], 0); continue
    cent.append(v.copy()); memb.append([k])
order = sorted(range(len(cent)), key=lambda i: -len(memb[i]))
letters = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ'
lab_of = {}
pal = []
for n, i in enumerate(order):
    lc = cent[i]
    shadow = np.hypot(lc[1], lc[2]) < 14 and 60 < lc[0] < 89
    if len(memb[i]) < 3 or shadow:
        for k in memb[i]: lab_of[k] = '.' if shadow else '?'
        continue
    L = letters[len(pal)]
    for k in memb[i]: lab_of[k] = L
    mean_rgb = np.mean([rgb[k] for k in memb[i]], 0)
    pal.append(dict(letter=L, cells=len(memb[i]), lab=[round(float(x)) for x in cent[i]], rgb='#%02x%02x%02x' % tuple(int(x) for x in mean_rgb)))
grid = [[lab_of.get((x, y), '.') for x in range(W)] for y in range(H)]
# colour-layer depth: 0-1 BFS from outside; stepping into a tile of a different colour than the previous tile costs 1
INF = 10**9
dist = {}
dq = collections.deque()
for y in range(H):
    for x in range(W):
        if x in (0, W - 1) or y in (0, H - 1) or grid[y][x] == '.':
            pass
# virtual outside: start from empty cells connected to border and from border tiles
start = []
seen = set()
q = collections.deque([(x, y) for y in range(-1, H + 1) for x in (-1, W)] + [(x, y) for x in range(-1, W + 1) for y in (-1, H)])
emp = set((x, y) for y in range(H) for x in range(W) if grid[y][x] == '.')
outside = set(q)
while q:
    x, y = q.popleft()
    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
        n = (x + dx, y + dy)
        if n in emp and n not in outside: outside.add(n); q.append(n)
best = {}
dq = collections.deque()
for (x, y) in outside:
    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
        n = (x + dx, y + dy)
        if 0 <= n[0] < W and 0 <= n[1] < H and grid[n[1]][n[0]] != '.' and best.get(n, INF) > 0:
            best[n] = 0; dq.append(n)
while dq:
    x, y = dq.popleft(); d = best[(x, y)]
    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
        n = (x + dx, y + dy)
        if not (0 <= n[0] < W and 0 <= n[1] < H): continue
        c = grid[n[1]][n[0]]
        if c == '.':
            nd = d  # enclosed empty cell
        else:
            nd = d + (0 if c == grid[y][x] else 1)
        if nd < best.get(n, INF):
            best[n] = nd
            (dq.appendleft if nd == d else dq.append)(n)
depth = collections.defaultdict(collections.Counter)
for (x, y), d in best.items():
    c = grid[y][x]
    if c != '.': depth[c][d] += 1
res = dict(gridW=int(W), gridH=int(H), tiles=sum(p['cells'] for p in pal), palette=pal,
           depth={k: {int(a): int(b) for a, b in sorted(v.items())} for k, v in sorted(depth.items())},
           rows=[''.join(row) for row in grid])
json.dump(res, open(out + '.json', 'w'), indent=1)
print(W, 'x', H, 'tiles', res['tiles'])
for p in pal: print(p['letter'], p['cells'], p['rgb'], p['lab'], 'depth', res['depth'].get(p['letter']))
print('\n'.join(res['rows']))

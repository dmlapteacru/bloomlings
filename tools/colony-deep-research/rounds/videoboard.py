"""A board read from a 640x360 video frame when the site has no cover (L37): the phone screen's board area enlarged
3x, every cube's top face sampled on a given grid, the samples clustered from seed colours named by the round's
letters ("?" = a grey mystery tile on the board).
usage: python3 -I videoboard.py <frame.png> <x0,y0,x1,y1 screen crop> <gx,pitch_x,gy,pitch_y (enlarged)> <w,h>
       <letter=#rrggbb,...> <out.json>
writes {w, h, rows (top first), palette {letter: hex}, cells {letter: n}}"""
import sys, json
import numpy as np
from PIL import Image
frame, crop, grid, size, seeds, out = sys.argv[1:7]
x0, y0, x1, y1 = [int(v) for v in crop.split(',')]
gx, px, gy, py = [float(v) for v in grid.split(',')]
w, h = [int(v) for v in size.split(',')]
letters = [s.split('=')[0] for s in seeds.split(',')]
C = np.array([[int(s.split('=')[1][i:i + 2], 16) for i in (1, 3, 5)] for s in seeds.split(',')], float)
a = np.asarray(Image.open(frame).convert('RGB').crop((x0, y0, x1, y1)).resize(((x1 - x0) * 3, (y1 - y0) * 3), Image.LANCZOS)).astype(float)
X = np.array([np.median(a[int(gy + py * (j + .42)) - 3:int(gy + py * (j + .42)) + 3, int(gx + px * (i + .5)) - 4:int(gx + px * (i + .5)) + 4].reshape(-1, 3), 0)
              for j in range(h) for i in range(w)])
for _ in range(20):   # k-means from the seeds
    lab = ((X[:, None] - C[None]) ** 2).sum(-1).argmin(1)
    C = np.array([X[lab == k].mean(0) if (lab == k).any() else C[k] for k in range(len(C))])
rows = [''.join(letters[lab[j * w + i]] for i in range(w)) for j in range(h)]
json.dump(dict(w=w, h=h, rows=rows, palette={l: '#%02x%02x%02x' % tuple(int(v) for v in C[k]) for k, l in enumerate(letters)},
               cells={l: int((lab == k).sum()) for k, l in enumerate(letters)}), open(out, 'w'), indent=1)
print('\n'.join(rows)); print({l: int((lab == k).sum()) for k, l in enumerate(letters)})

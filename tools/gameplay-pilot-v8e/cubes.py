#!/usr/bin/env python3
"""v8E pictures as the game's board draws them: every picture cell a soft cube, ported one to one from the client kit's
UiRaster.Cube (client/Assets/Bloomlings/UI/Design/VolumeRaster.cs, spec 005 FR-044), without icons; the cubes stand on
the board gap colour (GardenLook.BoardGap); the pilot's path is sand and its fence a row of stone blocks in the kit's
stone.* colours (an approximation of the game's stone border, not the kit's own raster), on the lawn.
usage: python3 cubes.py <levels.json> <out.png> [cell px] [ids, e.g. 1,2,3]   (default: all 30 on one sheet)
       python3 cubes.py <levels.json> <out.png> <cell px> <id> --single        (one board)"""
import json, math, re, sys
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFont
HERE = Path(__file__).parent; ROOT = HERE.parents[1]
pal = (HERE / 'palette.cjs').read_text()
codes = re.search(r"codes='([A-Z]+)'", pal).group(1)
COL = dict(zip(codes, re.findall(r"#[0-9A-Fa-f]{6}", pal.split('normal=[')[1].split(']')[0])))
hexrgb = lambda h: np.array([int(h[i:i + 2], 16) for i in (1, 3, 5)], float)
WHITE, BLACK = np.array([255., 255, 255]), np.zeros(3)
mix = lambda a, b, t: a + (b - a) * t
lighten = lambda c, t: mix(c, WHITE, t); darken = lambda c, t: mix(c, BLACK, t)
clamp01 = lambda v: np.clip(v, 0, 1)
def norm(x, y, z): n = math.sqrt(x * x + y * y + z * z); return x / n, y / n, z / n
LIGHT = norm(-0.45, -0.75, 1.1); HALF = norm(LIGHT[0], LIGHT[1], LIGHT[2] + 1)
BASE = HALF[2] ** 24; GLOSS = max(0.01, 1 - BASE)
LAWN_DARK = hexrgb('#64982D'); GAP = darken(LAWN_DARK, 0.55)
def rrect(x, y, l, t, r, b, rad):
    hx, hy = (r - l) / 2, (b - t) / 2; rad = min(rad, hx, hy)
    qx = np.abs(x - (l + r) / 2) - hx + rad; qy = np.abs(y - (t + b) / 2) - hy + rad
    return np.hypot(np.maximum(qx, 0), np.maximum(qy, 0)) + np.minimum(np.maximum(qx, qy), 0) - rad
def ellipse_distance(x, y, rx, ry):
    f = x * x / (rx * rx) + y * y / (ry * ry) - 1; gx = 2 * x / (rx * rx); gy = 2 * y / (ry * ry); g = np.hypot(gx, gy)
    return np.where(g < 1e-4, -min(rx, ry), f / np.maximum(g, 1e-9))
def segment(px, py, ax, ay, bx, by):
    dx, dy = bx - ax, by - ay; t = clamp01(((px - ax) * dx + (py - ay) * dy) / (dx * dx + dy * dy))
    return np.hypot(px - (ax + t * dx), py - (ay + t * dy))
def mixin(c, col, t): t = clamp01(t)[..., None]; return c + (col - c) * t
def cube(s, hexcol):
    """UiRaster.Cube for one tile of s px: RGB and coverage arrays."""
    col = hexrgb(hexcol); y, x = np.mgrid[0:s, 0:s] + 0.5
    m = s * 0.010; side = s * 0.1; r = s * 0.17; bevel = max(1.5, s * 0.12)
    body = rrect(x, y, m, m, s - m, s - m, r); cover = clamp01(0.5 - body)
    tb = s - m - side; top = rrect(x, y, m, m, s - m, tb, r)
    # top
    t = clamp01((y - m) / max(1, tb - m))[..., None]
    ct = mix(lighten(col, .12), darken(col, .04), t)
    tilt = 1 - clamp01(-top / bevel)
    gx = rrect(x + .75, y, m, m, s - m, tb, r) - rrect(x - .75, y, m, m, s - m, tb, r)
    gy = rrect(x, y + .75, m, m, s - m, tb, r) - rrect(x, y - .75, m, m, s - m, tb, r)
    g = np.hypot(gx, gy); gx = np.where(g < 1e-5, 0, gx / np.maximum(g, 1e-9)); gy = np.where(g < 1e-5, 0, gy / np.maximum(g, 1e-9))
    sin = np.minimum(.97, clamp01(tilt)); cos = np.sqrt(np.maximum(0, 1 - sin * sin)); nx, ny, nz = gx * sin, gy * sin, cos
    lit = nx * LIGHT[0] + ny * LIGHT[1] + nz * LIGHT[2] - LIGHT[2]
    shaded = np.where((lit > 0)[..., None], mixin(ct, lighten(col, .6), lit * 1.25), mixin(ct, darken(col, .5), -lit * .7))
    half = nx * HALF[0] + ny * HALF[1] + nz * HALF[2]
    spec = clamp01((np.power(np.maximum(0, half), 24) - BASE) / GLOSS)
    shaded = mixin(shaded, WHITE, spec * .75)
    ct = np.where((tilt > 0)[..., None], shaded, ct)
    dome = ellipse_distance(x - s / 2, y - (m + tb) * .46, s * .3, s * .26)
    sm = clamp01((dome + s * .04) / (s * .22)); ct = mixin(ct, lighten(col, .35), .18 * (1 - sm * sm * (3 - 2 * sm)))
    gx0, gy0 = m + s * .24, m + s * .13
    dash = segment(x, y, gx0 - s * .08, gy0 + s * .02, gx0 + s * .05, gy0 - s * .012) - s * .028
    ct = mixin(ct, WHITE, .55 * clamp01(.5 - dash / max(1, s * .02)))
    ct = mixin(ct, darken(col, .24), clamp01(top + .5) * .5)
    # front face
    t2 = clamp01((y - (tb - r)) / max(1, (s - m) - (tb - r)))[..., None]
    cf = mix(darken(col, .2), darken(col, .36), t2)
    sx = clamp01((x - m) / max(1, s - 2 * m)); cf = mixin(cf, lighten(col, .1), .18 * clamp01(1 - sx * 3)); cf = mixin(cf, darken(col, .55), .25 * clamp01((sx - .7) * 3.3))
    c = np.where((top < .5)[..., None], ct, cf)
    c = mixin(c, darken(col, .55), .55 * clamp01(1 + body / max(1, s * .02)))
    return c, cover
STONE_TOP, STONE_FACE, STONE_LIP, STONE_LINE = '#FCE8C6', '#F1D5A8', '#D9B585', '#7E6844'
PATH, PATH_DOT, WATER = '#E8D3A6', '#D8BE8C', '#5fb7e0'
def board(level, s, tiles={}):
    """The board: cubes on the gap colour, the path as sand, the fence as stone blocks (kit tokens stone.*), the gate
    as a dark arch in it, on the lawn."""
    W, H = level['w'], level['h']; rows = level['rowsBottomFirst'][::-1]; pad = s
    fence_row = all(ch == '#' for ch in rows[1][1:-1])
    img = np.zeros((H * s + 2 * pad, W * s + 2 * pad, 3))
    yy, xx = np.mgrid[0:img.shape[0], 0:img.shape[1]]; img[:] = mix(hexrgb('#9CC842'), LAWN_DARK, (yy / img.shape[0])[..., None] * .6)
    img[pad:pad + H * s, pad:pad + W * s] = GAP
    stones, gates = [], []
    for y, row in enumerate(rows):
        for x, ch in enumerate(row):
            Y, X = pad + y * s, pad + x * s; edge = x in (0, W - 1) or y in (0, H - 1)
            if ch == '#' and (edge or (y == 1 and fence_row)): stones.append((X, Y)); continue
            if ch == '#': img[Y:Y + s, X:X + s] = hexrgb(WATER); continue
            if ch == '.':
                if edge: stones.append((X, Y)); gates.append((X, Y, x, y))
                else:
                    img[Y:Y + s, X:X + s] = hexrgb(PATH)
                    if (x * 7 + y * 5) % 4 == 0: img[Y + s // 3:Y + s // 3 + max(1, s // 8), X + s // 2:X + s // 2 + max(1, s // 8)] = hexrgb(PATH_DOT)
                continue
            key = (s, ch)
            if key not in tiles: tiles[key] = cube(s, COL[ch])
            c, cov = tiles[key]; img[Y:Y + s, X:X + s] = mixin(img[Y:Y + s, X:X + s], c, cov)
    im = Image.fromarray(np.clip(img, 0, 255).astype(np.uint8)); d = ImageDraw.Draw(im); r = max(2, s // 5); lw = max(1, s // 14)
    for X, Y in stones:
        d.rounded_rectangle([X, Y, X + s - 1, Y + s - 1], r, fill=STONE_LIP)
        d.rounded_rectangle([X, Y, X + s - 1, Y + s - 1 - max(1, s // 6)], r, fill=STONE_FACE, outline=STONE_LINE, width=lw)
        d.line([X + r, Y + lw + 1, X + s - r, Y + lw + 1], fill=STONE_TOP, width=max(1, s // 10))
    for X, Y, x, y in gates:  # the gate: a dark arch opening toward the inside
        o = max(2, s // 6)
        if y == H - 1: d.pieslice([X + o, Y - s // 2, X + s - o, Y + s - o], 180, 360, fill='#3d3123'); d.rectangle([X + o, Y, X + s - o, Y + s - o], fill='#3d3123')
        else: d.ellipse([X + o, Y + o, X + s - o, Y + s - o], fill='#3d3123')
    return im
if __name__ == '__main__':
    levels = json.load(open(sys.argv[1]))['levels']; out = sys.argv[2]; s = int(sys.argv[3]) if len(sys.argv) > 3 else 14
    if '--single' in sys.argv:
        board(levels[int(sys.argv[4]) - 1], s).save(out); print(out); sys.exit()
    ids = [int(i) for i in sys.argv[4].split(',')] if len(sys.argv) > 4 else list(range(1, 31))
    F = ROOT / 'client/Assets/Bloomlings/UI/Fonts/Resources'; font = ImageFont.truetype(str(F / 'Nunito-SemiBold.ttf'), max(16, s + 4)); big = ImageFont.truetype(str(F / 'Nunito-ExtraBold.ttf'), 30)
    cols = 5 if len(ids) > 5 else len(ids); cw = 38 * s + 30; ch = 38 * s + 60
    sheet = Image.new('RGB', (cols * cw + 20, 70 + ((len(ids) + cols - 1) // cols) * ch), '#FFF8EA'); d = ImageDraw.Draw(sheet)
    d.text((20, 18), 'v8E · картинки кубиками, как на борде игры (без иконок)', font=big, fill='#34493d')
    for k, i in enumerate(ids):
        lv = levels[i - 1]; x, y = 20 + (k % cols) * cw, 70 + (k // cols) * ch
        d.text((x, y), f"L{i} · {lv['title']} · {lv['w']}×{lv['h']}", font=font, fill='#34493d'); sheet.paste(board(lv, s), (x, y + s + 14))
    sheet.save(out); print(out, sheet.size)

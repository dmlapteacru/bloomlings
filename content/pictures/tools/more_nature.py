"""More subjects for the levels: nature (more_subjects.py). Trees and plants, the sea and the weather, and small places
of a town and a playground, in the procedural style of garden_subjects.py and world_subjects.py: each draws with
picture_kit and returns (canvas, roles, themes). The shapes that make a subject (a crown against the sky, a trunk on
the grass, a pod's jaws and their teeth) come from different color groups, so they stay apart under any mapping, and
most subjects have two roles in three groups, so they can carry six distinct variants. The random stream moves each
picture's scene (sides, props, the time of day), and the big boards (from 300 cells) get more detail.
"""
import math

from picture_kit import (BLUE, BROWN, GREEN, PINK, box, cells, cloud, disc, dots, ground_rows, hills, lens, oval, path,
                         poly, rbox, regions, ring, role, scatter, seg, sky, star, start)


def mirror(cv):
    """Flips the drawing left to right."""
    for row in cv.g:
        row.reverse()


def mid(y):
    """The middle of the cell row at y, for shapes one row high."""
    return int(y) + 0.5


def fill_pockets(cv, c, into):
    """Gives the regions of role c that only `into` surrounds (the pockets a fold or a frame closes) to `into`, so such
    a line never adds a level of nesting."""
    seen = set()
    for y in range(cv.h):
        for x in range(cv.w):
            if cv.g[y][x] != c or (x, y) in seen:
                continue
            seen.add((x, y))
            region, stack, around = [], [(x, y)], set()
            while stack:
                a, b = stack.pop()
                region.append((a, b))
                for p, q in ((a + 1, b), (a - 1, b), (a, b + 1), (a, b - 1)):
                    if 0 <= p < cv.w and 0 <= q < cv.h:
                        if cv.g[q][p] != c:
                            around.add(cv.g[q][p])
                        elif (p, q) not in seen:
                            seen.add((p, q))
                            stack.append((p, q))
            if around <= {into}:
                for a, b in region:
                    cv.g[b][a] = into


# A maple leaf's outline from its tip round to its stem notch (degrees from the tip, share of its size); the other half
# mirrors it.
MAPLE = ((0, 1.0), (14, 0.7), (24, 0.8), (38, 0.5), (55, 0.84), (72, 1.0), (88, 0.7), (102, 0.5), (118, 0.7), (136, 0.82),
         (158, 0.5), (180, 0.3))


def maple_leaf(cv, x, y, size, c, turn=0.0):
    """A maple leaf `size` cells from its middle to its tip, the stem notch down (turned by `turn` degrees)."""
    outline = list(MAPLE) + [(360 - a, rr) for a, rr in reversed(MAPLE[1:-1])]
    poly(cv, [(x + math.sin(math.radians(a + turn)) * rr * size, y - math.cos(math.radians(a + turn)) * rr * size)
              for a, rr in outline], c)


def marks(cv, c, under, pts):
    """Puts role c on those of the cells `pts` that are of role `under`, skipping a cell that would cut a region of
    `under` in two, so a pattern of marks never closes a pocket."""
    for x, y in pts:
        if 0 <= x < cv.w and 0 <= y < cv.h and cv.g[y][x] == under:
            before = regions(cv, under)
            cv.g[y][x] = c
            if regions(cv, under) > before:
                cv.g[y][x] = under


def maple_sprite(cv, x, y, c):
    """A small maple leaf of five by five cells, centered on (x, y)."""
    for j, line in enumerate(('..x..', 'x.x.x', 'xxxxx', '.xxx.', '..x..')):
        for i, ch in enumerate(line):
            if ch == 'x':
                cv.put(int(x) - 2 + i, int(y) - 2 + j, c)


def tilted(cv, x, y, rx, ry, a, c):
    """An oval turned by `a` radians: rx across its long axis, which points along (sin a, -cos a)."""
    ca, sa = math.cos(a), math.sin(a)
    for gx, gy, px, py in cells(cv):
        dx, dy = px - x, py - y
        u, v = dx * ca + dy * sa, dx * sa - dy * ca
        if (u / rx) ** 2 + (v / ry) ** 2 <= 1.0:
            cv.g[gy][gx] = c


def leaflet(cv, x, y, a, size, c):
    """A heart-shaped leaflet whose point sits on (x, y) and whose lobes face away along angle `a` (radians)."""
    ca, sa = math.cos(a), math.sin(a)
    for gx, gy, px, py in cells(cv):
        dx, dy = px - x, py - y
        v = (dx * ca + dy * sa) / size - 1.0
        u = (dy * ca - dx * sa) / size
        if (u * u + v * v - 1) ** 3 - u * u * v ** 3 <= 0:
            cv.g[gy][gx] = c


# ---- Trees and plants ----

def oak_tree(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.12)
    hills(cv, 'g', gtop, 0.4, w * 1.4, r.uniform(0, 6))
    side = r.choice((-1, 1))
    tx = cx - side * w * 0.04
    cy = h * 0.3
    tw = max(1.1, w * 0.075)
    poly(cv, [(tx - tw, cy), (tx + tw, cy), (tx + tw * 1.8, gtop + 0.6), (tx - tw * 1.8, gtop + 0.6)], 't')
    seg(cv, tx, h * 0.6, tx + side * w * 0.44, h * 0.52, 't', 0.55 if not big else 0.75)
    seg(cv, tx, h * 0.56, tx - side * w * 0.24, cy + h * 0.1, 't', 0.6 if not big else 0.8)
    sx, dx = tx + side * w * 0.3, 1.0 if not big else 1.5
    for x in (sx - dx, sx + dx):
        seg(cv, x, h * 0.55, x, mid(h * 0.8), 'k', 0.45)
    box(cv, sx - dx - 0.5, mid(h * 0.8) - 0.3, sx + dx + 0.5, mid(h * 0.8) + 0.3, 'k')
    lumps = [(0.0, -0.09, 0.19), (-0.17, -0.05, 0.17), (0.17, -0.05, 0.17), (-0.26, 0.06, 0.13), (0.26, 0.06, 0.13),
             (-0.12, 0.1, 0.15), (0.12, 0.1, 0.15), (0.0, 0.04, 0.18)]
    for dx_, dy, rad in lumps:
        disc(cv, tx + dx_ * w, cy + dy * h + 0.6, rad * w, 'd')
    for dx_, dy, rad in lumps:
        disc(cv, tx + dx_ * w - 0.3, cy + dy * h - 0.2, rad * w * 0.88, 'c')
    if big:
        oval(cv, tx - side * 0.3, h * 0.66, 0.9, 1.3, 'o')
        for x, y in scatter(cv, 'a', 'c', 7, r, sep=3, area=(1, h * 0.1, w - 2, h * 0.42)):
            cv.put(x, y + 1, 'a')
        for x in (tx - side * w * 0.3, tx - side * w * 0.38):
            oval(cv, x, gtop + 1.3, 0.9, 0.8, 'a')
    else:
        scatter(cv, 'a', 'c', 4, r, sep=3, area=(1, h * 0.12, w - 2, h * 0.42))
    cloud(cv, w * 0.5 + side * w * 0.36, h * 0.07, s * 0.05 + 0.5, 'e')
    disc(cv, w * 0.5 - side * w * 0.4, h * 0.07, s * 0.08, 'u')
    return cv, [sky(), role('c', 'crown', 'Crown', GREEN), role('d', 'shade', 'Leaf shade', GREEN), role('a', 'acorns', 'Acorns', BROWN),
                role('t', 'trunk', 'Trunk and boughs', BROWN), role('k', 'swing', 'Swing', PINK), role('o', 'hollow', 'Hollow', PINK),
                role('g', 'grass', 'Grass', GREEN), role('u', 'sun', 'Sun', BROWN), role('e', 'cloud', 'Cloud', BLUE)], ['trees', 'park']


def willow_tree(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.2)
    box(cv, 0, gtop, w, h, 'g')
    side = r.choice((-1, 1))
    tx = cx + side * w * 0.06
    px, prx = cx - side * w * 0.3, w * 0.22
    oval(cv, px, gtop + (h - gtop) * 0.6, prx, (h - gtop) * 0.45, 'w')
    tw = max(0.9, w * 0.06)
    poly(cv, [(tx - tw, h * 0.3), (tx + tw, h * 0.3), (tx + tw * 1.6, gtop + 0.6), (tx - tw * 1.6, gtop + 0.6)], 't')
    rx, dy = w * 0.39, h * 0.3
    for x in range(w):
        rel = (x + 0.5 - tx) / rx
        if abs(rel) > 1:
            continue
        top = dy - h * 0.22 * math.sqrt(1 - rel * rel)
        for y in range(h):
            if top <= y + 0.5 <= dy:
                cv.g[y][x] = 'c'
        if (x + int(tx)) % 2:
            continue
        bottom = dy + h * (0.4 - 0.16 * rel * rel) + r.uniform(-1.0, 1.0)
        for y in range(int(dy), min(int(bottom), gtop - 1)):
            cv.g[y][x] = 'c'
    for k in range(2 if not big else 3):
        oval(cv, px + (k - 0.5) * prx * 0.7, gtop + (h - gtop) * 0.6, 0.9, 0.6, 'o')
    kx = min(w - 1.5, max(1.5, px - side * (prx + 0.2)))
    for k in range(2 if not big else 3):
        x = kx + k * side * 2.0
        seg(cv, x, gtop + 0.5, x, gtop - h * 0.1, 'k', 0.4)
        oval(cv, x, gtop - h * 0.1, 0.5, 1.2, 'k')
    if big:
        oval(cv, px + side * 1.0, gtop + (h - gtop) * 0.45, 1.6, 0.9, 'y')
        disc(cv, px + side * 2.2, gtop + (h - gtop) * 0.25, 0.85, 'y')
    disc(cv, w * 0.5 - side * w * 0.4, h * 0.07, s * 0.085, 'u')
    return cv, [sky(), role('c', 'canopy', 'Weeping branches', GREEN), role('k', 'reeds', 'Cattails', BROWN), role('t', 'trunk', 'Trunk', BROWN),
                role('g', 'grass', 'Grass', GREEN), role('w', 'pond', 'Pond', BLUE), role('o', 'lilies', 'Water lilies', PINK),
                role('y', 'duck', 'Duck', BROWN), role('u', 'sun', 'Sun', PINK)], ['trees', 'pond']


def maple_tree(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.12)
    hills(cv, 'g', gtop, 0.4, w * 1.3, r.uniform(0, 6))
    side = r.choice((-1, 1))
    tx = cx + side * w * 0.07
    ly, size = h * 0.36, w * 0.38
    tw = max(1.0, w * 0.06)
    poly(cv, [(tx - tw, ly), (tx + tw, ly), (tx + tw * 1.7, gtop + 0.6), (tx - tw * 1.7, gtop + 0.6)], 't')
    maple_leaf(cv, tx, ly, size, 'c', r.uniform(-5, 5))
    for a in ((0, 72, 288) if not big else (0, 72, 288, 136, 224)):
        t = math.radians(a)
        seg(cv, tx, ly + size * 0.3, tx + math.sin(t) * size * 0.62, ly - math.cos(t) * size * 0.62, 't', 0.45 if not big else 0.55)
    fx = w * 0.5 - side * w * 0.3
    if big:
        maple_leaf(cv, fx, h * r.uniform(0.6, 0.66), 3.4, 'f', r.uniform(-40, 40))
        maple_leaf(cv, w * 0.5 + side * w * 0.36, h * 0.68, 3.0, 'f', r.uniform(-40, 40))
        maple_sprite(cv, fx + side * 1.0, h * 0.06 + 2, 'f')
    else:
        maple_sprite(cv, fx, h * r.uniform(0.6, 0.66), 'f')
        poly(cv, [(w * 0.5 + side * w * 0.38, h * 0.64), (w * 0.5 + side * w * 0.38 + 1.2, h * 0.64 + 1.2),
                  (w * 0.5 + side * w * 0.38, h * 0.64 + 2.4), (w * 0.5 + side * w * 0.38 - 1.2, h * 0.64 + 1.2)], 'f')
    oval(cv, tx + side * w * 0.2, gtop + 0.3, w * 0.12, 1.3 if not big else 1.8, 'p')
    cloud(cv, w * 0.5 - side * w * 0.34, h * 0.07, s * 0.05 + 0.4, 'e')
    return cv, [sky(), role('c', 'crown', 'Maple crown', PINK), role('f', 'falling', 'Falling leaves', BROWN),
                role('t', 'trunk', 'Trunk and boughs', BROWN), role('p', 'pile', 'Leaf pile', PINK),
                role('g', 'grass', 'Grass', GREEN), role('e', 'cloud', 'Cloud', BLUE)], ['trees', 'autumn']


def bamboo(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.1)
    disc(cv, cx + r.uniform(-1, 1), h * 0.3, s * 0.2, 'u')
    n = 2 if not big else 3
    lw = 0.95 if not big else 1.05
    xs = [w * (0.28 + 0.44 * k) if n == 2 else w * (0.2 + 0.3 * k) for k in range(n)]
    xs = [x + r.uniform(-0.4, 0.4) for x in xs]
    tall = r.randrange(n)
    for k, x in enumerate(xs):
        top = -1 if k == tall else h * r.uniform(0.14, 0.24)
        seg(cv, x, gtop + 0.5, x, top, 'b', lw)
        room = min(x - 1.5, w - 1.5 - x, *[abs(x - o) - 2 * lw - 2.2 for o in xs if o != x])
        y = gtop - r.uniform(2.0, 3.0)
        j = 0
        while y > max(top + 2.0, 1.0):
            box(cv, x - lw - 0.2, y - 0.3, x + lw + 0.2, y + 0.3, 'n', only='b')
            if 0 < j < 4 and room > 0.8:
                d = 1 if (j + k) % 2 else -1
                L = min(s * 0.3, lw + room)
                lens(cv, x + d * lw * 0.6, y - 0.3, x + d * L, y + L * 0.3, 1.2 if not big else 1.5, 'l')
            j += 1
            y -= 3.5 if not big else 4.2
        if top > 0:
            for a in (-0.6, 0.6):
                lens(cv, x, top + 0.6, x + math.sin(a) * s * 0.2, top + 0.6 - math.cos(a) * s * 0.16, 1.2 if not big else 1.5, 'l')
    if big:
        sx = w * 0.5 + r.choice((-1, 1)) * w * 0.15
        poly(cv, [(sx - 1.3, gtop + 0.5), (sx, gtop - h * 0.12), (sx + 1.3, gtop + 0.5)], 'l')
        box(cv, sx - 1.3, gtop - h * 0.04, sx + 1.3, gtop - h * 0.04 + 0.6, 'n', only='l')
    box(cv, 0, gtop, w, h, 'd')
    for x in (w * 0.08, w * 0.92) if big else (w * 0.5,):
        oval(cv, x, gtop + 0.5, 1.4, 0.9, 'k')
    return cv, [sky(), role('b', 'stalk', 'Bamboo canes', GREEN), role('n', 'node', 'Cane rings', BROWN), role('l', 'leaf', 'Leaves', GREEN),
                role('d', 'soil', 'Soil', BROWN), role('k', 'stone', 'Stones', BLUE), role('u', 'sun', 'Sun', PINK)], ['plants', 'forest']


def rose(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.12)
    for x in range(1 if not big else 0, w, 3):
        poly(cv, [(x, h * 0.7), (x + 1.0, h * 0.66), (x + 2.0, h * 0.7), (x + 2.0, gtop + 0.5), (x, gtop + 0.5)], 'k')
    hills(cv, 'g', gtop, 0.4, w * 1.2, r.uniform(0, 6))
    side = r.choice((-1, 1))
    rx, by, R = cx + side * w * 0.04, h * 0.32, s * (0.32 if not big else 0.28)
    path(cv, [(rx, by + R), (rx - side * 0.8, h * 0.6), (rx, gtop + 0.5)], 'l', 0.5 if not big else 0.6)
    lens(cv, rx - side * 0.5, h * 0.58, rx - side * w * 0.3, h * 0.5, s * 0.14, 'l')
    lens(cv, rx - side * 0.3, h * 0.68, rx + side * w * 0.28, h * 0.6, s * 0.13, 'l')
    for k, y in enumerate((0.5, 0.64, 0.76) if big else (0.52, 0.74)):
        d = 1 if k % 2 else -1
        poly(cv, [(rx - side * 0.4 + d * 0.4, h * y - 0.7), (rx - side * 0.4 + d * 1.6, h * y), (rx - side * 0.4 + d * 0.4, h * y + 0.7)], 'h')
    poly(cv, [(rx - R * 0.9, by + R * 0.5), (rx, by + R * 1.25), (rx + R * 0.9, by + R * 0.5)], 'l')
    oval(cv, rx, by + R * 0.1, R, R * 0.9, 'p')
    for dx in (-0.55, 0.0, 0.55):
        disc(cv, rx + dx * R, by - R * 0.55, R * 0.42, 'p')
    pts = []
    turns, step = (1.5, 2.0) if not big else (1.8, 2.2)
    for k in range(60):
        th = k / 59 * turns * 2 * math.pi
        rr = 0.4 + step * th / (2 * math.pi)
        pts.append((rx + 0.3 + math.cos(th + 1.0) * rr, by - R * 0.15 + math.sin(th + 1.0) * rr * 0.85))
    path(cv, pts, 'q', 0.45 if not big else 0.5)
    fill_pockets(cv, 'p', 'q')
    if big:
        bx = rx - side * w * 0.3
        path(cv, [(rx - side * 0.6, h * 0.5), (bx, h * 0.38), (bx, h * 0.3)], 'l', 0.5)
        oval(cv, bx, h * 0.27, 1.2, 1.7, 'p')
        poly(cv, [(bx - 1.3, h * 0.28), (bx, h * 0.33), (bx + 1.3, h * 0.28), (bx, h * 0.31)], 'l')
        cloud(cv, w * 0.5 + side * w * 0.32, h * 0.1, s * 0.05 + 0.4, 'e')
    disc(cv, w * 0.5 - side * w * 0.38, h * 0.08, s * 0.085, 'u')
    return cv, [sky(), role('p', 'bloom', 'Rose', PINK), role('q', 'petal_line', 'Petal folds', PINK), role('l', 'stem', 'Stem and leaves', GREEN),
                role('h', 'thorns', 'Thorns', BROWN), role('k', 'fence', 'Fence', BROWN), role('g', 'grass', 'Grass', GREEN),
                role('u', 'sun', 'Sun', BROWN), role('e', 'cloud', 'Cloud', BLUE)], ['flowers', 'garden']


def dandelion(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.12)
    hills(cv, 'g', gtop, 0.4, w * 1.3, r.uniform(0, 6))
    side = r.choice((-1, 1))
    pr = s * (0.29 if not big else 0.26)
    px, py = min(w - pr - 2.2, max(pr + 2.2, cx + side * w * 0.1)), max(pr + 2.2, h * 0.3)
    path(cv, [(px, py), (px - side * 0.6, h * 0.62), (px - side * 0.2, gtop + 0.5)], 'l', 0.5)
    fx, fy = cx - side * w * 0.27, h * 0.58
    path(cv, [(fx, fy), (fx + side * 0.5, h * 0.75), (fx + side * 0.2, gtop + 0.5)], 'l', 0.5)
    for d in (-1, 1):
        for x0 in (px - side * 0.2, fx + side * 0.2):
            lens(cv, x0, gtop + 0.4, x0 + d * w * 0.2, gtop - h * 0.1, s * 0.1 + 0.3, 'l')
            poly(cv, [(x0 + d * w * 0.08, gtop - h * 0.03), (x0 + d * w * 0.13, gtop - h * 0.1), (x0 + d * w * 0.14, gtop - h * 0.04)], 'l')
    n = 12 if not big else 16
    for k in range(n):
        a = math.radians(k * 360 / n + 9)
        ex, ey = px + math.cos(a) * pr, py + math.sin(a) * pr
        seg(cv, px, py, ex, ey, 'p', 0.42)
        disc(cv, ex, ey, 0.6 if not big else 0.8, 'p')
    disc(cv, px, py, 0.9 if not big else 1.3, 'c')
    for k in range(3 if not big else 5):
        x = px - side * (pr + 1.5 + k * 2.2)
        y = py - pr * 0.5 + k * 1.6 * (1 if k % 2 else -0.6)
        if 1 <= x <= w - 2:
            seg(cv, x, y + 0.6, x - side * 0.6, y + 1.5, 'f', 0.4)
            path(cv, [(x - 0.9, y - 0.4), (x, y + 0.3), (x + 0.9, y - 0.4)], 'f', 0.45)
    for k in range(12):
        a = math.radians(k * 30)
        seg(cv, fx, fy, fx + math.cos(a) * s * 0.14, fy + math.sin(a) * s * 0.12, 'y', 0.45)
    disc(cv, fx, fy, s * 0.08 + 0.2, 'y')
    if big:
        cloud(cv, w * 0.5 - side * w * 0.3, h * 0.1, s * 0.05 + 0.4, 'e')
    return cv, [sky(), role('p', 'puff', 'Seed head', PINK), role('f', 'seeds', 'Flying seeds', PINK), role('y', 'head', 'Dandelion flower', BROWN),
                role('c', 'center', 'Seed head center', BROWN), role('l', 'stem', 'Stems and leaves', GREEN), role('g', 'grass', 'Grass', GREEN),
                role('e', 'cloud', 'Cloud', BLUE)], ['flowers', 'meadow']


def clover(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.14)
    hills(cv, 'g', gtop, 0.5, w * 1.2, r.uniform(0, 6))
    side = r.choice((-1, 1))
    kx, ky = cx + side * w * 0.04, h * 0.38
    path(cv, [(kx, ky), (kx + side * 1.0, h * 0.62), (kx - side * 0.3, gtop + 0.5)], 'c', 0.55 if not big else 0.7)
    four = r.random() < 0.75
    size = s * (0.15 if four else 0.17)
    turn = r.uniform(-15, 15)
    angles = (45, 135, 225, 315) if four else (-90, 30, 150)
    for a in angles:
        leaflet(cv, kx, ky, math.radians(a + turn), size, 'c')
    for a in angles:
        t = math.radians(a + turn + 180 / len(angles))
        seg(cv, kx + math.cos(t) * 1.2, ky + math.sin(t) * 1.2, kx + math.cos(t) * size * 3, ky + math.sin(t) * size * 3, 's', 0.45)
    if big:
        for a in angles:
            t = math.radians(a + turn)
            ox, oy = kx + math.cos(t) * size * 1.15, ky + math.sin(t) * size * 1.15
            nx, ny = -math.sin(t), math.cos(t)
            path(cv, [(ox + nx * size * 0.55 + math.cos(t) * 0.6, oy + ny * size * 0.55 + math.sin(t) * 0.6), (ox, oy),
                      (ox - nx * size * 0.55 + math.cos(t) * 0.6, oy - ny * size * 0.55 + math.sin(t) * 0.6)], 'm', 0.4)
    lt = math.radians(angles[1 if side > 0 else 0] + turn)
    bx, by = kx + math.cos(lt) * size * 1.55, ky + math.sin(lt) * size * 1.55
    oval(cv, bx, by, 1.4 if not big else 1.8, 1.1 if not big else 1.5, 'b')
    disc(cv, bx + math.cos(lt) * (1.2 if not big else 1.6), by + math.sin(lt) * (1.2 if not big else 1.6), 0.7, 'd')
    if big:
        for dx, dy in ((-0.6, 0.0), (0.6, 0.0), (0.0, 0.7)):
            cv.put(int(bx + dx), int(by + dy), 'd')
    for k, x in enumerate((w * 0.1, w * 0.9) if not big else (w * 0.08, w * 0.92, w * 0.5 - side * w * 0.3)):
        top = gtop - h * (0.12 + 0.04 * k)
        seg(cv, x, gtop + 0.5, x, top, 'c', 0.4)
        oval(cv, x, top - 0.6, 1.0, 1.3, 'f')
    disc(cv, w * 0.5 - side * w * 0.38, h * 0.08, s * 0.085, 'u')
    if big:
        cloud(cv, w * 0.5 + side * w * 0.3, h * 0.08, s * 0.05 + 0.4, 'e')
    return cv, [sky(), role('u', 'sun', 'Sun', BROWN), role('c', 'clover', 'Clover', GREEN), role('m', 'mark', 'Leaf marks', BLUE),
                role('b', 'ladybug', 'Ladybug', PINK), role('d', 'spots', 'Ladybug head and spots', BROWN), role('f', 'blossom', 'Clover blossoms', PINK),
                role('g', 'grass', 'Grass', GREEN), role('e', 'cloud', 'Cloud', BLUE)], ['plants', 'meadow']


def flytrap_head(cv, x, y, R, face, big):
    """An open trap: a round pod split by a V towards `face` (radians), its lips lined pink and its rim spiked."""
    half = math.radians(34)
    for gx, gy, px, py in cells(cv):
        dx, dy = px - x, py - y
        d = math.hypot(dx, dy)
        if d > R:
            continue
        off = abs((math.atan2(dy, dx) - face + math.pi) % (2 * math.pi) - math.pi)
        if off < half:
            cv.g[gy][gx] = 's'
        elif d * math.sin(off - half) < 1.1 and d > 0.5:
            cv.g[gy][gx] = 'i'
        else:
            cv.g[gy][gx] = 'j'
    for d in (-1, 1):
        for b in ((8, 28, 50) if not big else (6, 20, 34, 50, 66)):
            a = face + d * (half + math.radians(b))
            seg(cv, x + math.cos(a) * (R - 0.8), y + math.sin(a) * (R - 0.8), x + math.cos(a) * (R + 1.6), y + math.sin(a) * (R + 1.6), 't', 0.45)


def venus_flytrap(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.08)
    box(cv, 0, gtop, w, h, 'g')
    side = r.choice((-1, 1))
    pt = h * 0.74
    poly(cv, [(cx - w * 0.24, pt), (cx + w * 0.24, pt), (cx + w * 0.17, gtop + 0.5), (cx - w * 0.17, gtop + 0.5)], 'o')
    box(cv, cx - w * 0.27, pt - 0.6, cx + w * 0.27, pt + (0.8 if not big else 1.4), 'm')
    hx, hy, R = cx - side * w * 0.05, h * 0.4, s * (0.3 if not big else 0.26)
    path(cv, [(cx, pt - 0.5), (cx + side * 1.2, (hy + pt) / 2 + 1), (hx, hy + R * 0.6)], 'j', 0.55 if not big else 0.7)
    for d in (-1, 1):
        lens(cv, cx + d * 0.5, pt - 0.3, cx + d * w * 0.32, pt - h * 0.1, s * 0.08 + 0.4, 'j')
    flytrap_head(cv, hx, hy, R, math.radians(-90 + side * 12), big)
    if big:
        sx, sy = cx + side * w * 0.26, h * 0.5
        path(cv, [(cx + side * 0.5, pt - 0.5), (sx, (sy + pt) / 2), (sx, sy + 1.5)], 'j', 0.55)
        tilted(cv, sx, sy, 1.4, 2.6, side * 0.3, 'j')
        for k in range(4):
            yy = sy - 2.0 + k * 1.2
            seg(cv, sx - side * 0.3 + (k % 2) * 0.4, yy, sx + side * 1.6, yy - 0.8, 't', 0.38)
    if big:
        fx, fy = w * 0.5 + side * w * 0.36, h * 0.08
        oval(cv, fx, fy, 1.0, 0.6, 'f')
        for d in (-1, 1):
            cv.put(int(fx + d * 0.6), int(fy - 1.0), 'w')
        path(cv, [(fx - side * 1.5, fy + 0.8), (fx - side * 3, fy + 1.8), (fx - side * 4, fy + 1.2)], 'v', 0.35)
    else:
        oval(cv, cx, pt - 0.2, w * 0.18, 0.7, 'd')
    fill_pockets(cv, 's', 'i')
    return cv, [sky(), role('j', 'trap', 'Jaws and stems', GREEN), role('i', 'mouth', 'Inside of the jaws', PINK),
                role('t', 'teeth', 'Teeth', GREEN), role('o', 'pot', 'Pot', BROWN), role('d', 'soil', 'Soil', BROWN), role('m', 'rim', 'Pot rim', PINK),
                role('f', 'fly', 'Fly', BROWN), role('w', 'wings', 'Wings', BLUE), role('v', 'buzz', 'Flight path', BLUE),
                role('g', 'sill', 'Windowsill', GREEN)], ['plants', 'garden']


def pine_cone(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.1)
    winter = r.random() < 0.35
    box(cv, 0, gtop, w, h, 'w' if winter else 'g')
    side = r.choice((-1, 1))
    kx = w * 0.5 + side * w * 0.04
    bx0, bx1 = (w + 0.5, w * 0.1) if side > 0 else (-0.5, w * 0.9)
    by = h * 0.13
    path(cv, [(bx0, by - 0.6), ((bx0 + bx1) / 2, by), (bx1, by + 0.8)], 'k', 0.55 if not big else 0.75)
    tufts = (0.12, 0.62, 0.92) if not big else (0.1, 0.36, 0.64, 0.92)
    for t in tufts:
        x, y = bx0 + (bx1 - bx0) * t, by + 0.7 * t - 0.3
        for a in (-35, -145, 60, 95, 130):
            ta = math.radians(a + r.uniform(-5, 5))
            L = s * 0.16 if a > 0 else min(s * 0.16, (y - 1.3) / abs(math.sin(ta)))
            seg(cv, x, y, x + math.cos(ta) * L, y + math.sin(ta) * L * 0.85, 'n', 0.38)
        if winter:
            box(cv, x - s * 0.08, mid(y - 1.0) - 0.3, x + s * 0.08, mid(y - 1.0) + 0.3, 'e')
    top, bot = h * 0.26, h * 0.86 if not big else h * 0.8
    W = w * 0.25
    rows = 3.0 if not big else 3.2
    inside = set()
    for y in range(h):
        t = (y + 0.5 - top) / (bot - top)
        if not 0 <= t <= 1:
            continue
        hw = W * math.sqrt(1 - ((0.38 - t) / 0.38) ** 2) if t < 0.38 else W * (1 - ((t - 0.38) / 0.62) ** 1.6) ** 0.8
        hw += 0.7 * (((y + 0.5 - top) % rows) / rows)
        for x in range(w):
            if abs(x + 0.5 - kx) <= hw:
                cv.g[y][x] = 'c'
                inside.add((x, y))
    box(cv, kx - 0.5, by, kx + 0.5, top + 0.4, 'k')
    x0 = int(kx)
    for j in range(int((bot - top) / rows) + 1):
        y = int(top + 2.2 + j * rows)
        for x in range(x0 - 10 + (j % 2) * 2 + 1, x0 + 11, 5):
            marks(cv, 'q', 'c', [p for p in ((x - 2, y - 2), (x - 1, y - 1), (x, y), (x + 1, y - 1), (x + 2, y - 2)) if p in inside])
    if big:
        lx = w * 0.5 - side * w * 0.32
        oval(cv, lx, gtop - 0.8, 2.4, 1.5, 'c')
        marks(cv, 'q', 'c', [(int(lx) + d, int(gtop - 0.8)) for d in (-2, 0, 2)])
    if not winter:
        cloud(cv, w * 0.5 - side * w * 0.32, h * 0.4, s * 0.05 + 0.4, 'e')
    roles = [sky(), role('k', 'branch', 'Branch', BROWN), role('c', 'cone', 'Pine cone', BROWN), role('n', 'needles', 'Needles', GREEN),
             role('q', 'scales', 'Scale edges', PINK)]
    if winter:
        return cv, roles + [role('e', 'snow', 'Snow on the branch', BLUE), role('w', 'snowfield', 'Snow', BLUE)], ['plants', 'winter']
    return cv, roles + [role('e', 'cloud', 'Cloud', BLUE), role('g', 'moss', 'Moss', GREEN)], ['plants', 'forest']


# ---- The sea and the weather ----

def seashell(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    sea, sand = h * 0.4, h * 0.62
    hills(cv, 'w', sea, 0.3, w * 0.5, r.uniform(0, 6))
    hills(cv, 'd', sand, 0.5, w * 1.1, r.uniform(0, 6))
    side = r.choice((-1, 1))
    hx, hy, R = cx + side * w * 0.05, h * 0.86, s * 0.5
    n = 7 if not big else 9
    a0, span = math.radians(216), math.radians(108)
    for gx, gy, px, py in cells(cv):
        dx, dy = px - hx, py - hy
        a = math.atan2(dy, dx) % (2 * math.pi)
        if a0 <= a <= a0 + span:
            f = (a - a0) / span * n
            edge = R * (0.86 + 0.14 * math.sin(math.pi * (f % 1.0)) ** 0.5)
            if math.hypot(dx, dy) <= edge:
                cv.g[gy][gx] = 'a' if int(f) % 2 == 0 else 'b'
    ew = R * 0.36
    poly(cv, [(hx - ew, hy - R * 0.32), (hx - ew * 0.2, hy - R * 0.32), (hx, hy + 0.4), (hx - ew * 0.7, hy - 0.2)], 'a')
    poly(cv, [(hx + ew, hy - R * 0.32), (hx + ew * 0.2, hy - R * 0.32), (hx, hy + 0.4), (hx + ew * 0.7, hy - 0.2)], 'a')
    fx = w * 0.5 - side * w * 0.36
    star(cv, fx, h * 0.92, s * 0.1 + 0.4, 'f', ri=0.7 if not big else 0.9)
    gx = w * 0.5 + side * w * 0.42
    for k in range(2 if not big else 3):
        x = gx - k * 1.4 * side
        path(cv, [(x, h - 0.5), (x + 0.7, h * 0.9), (x - 0.3, h * 0.8)], 'k', 0.45)
    disc(cv, w * 0.5 - side * w * 0.36, h * 0.1, s * 0.09, 'u')
    if big:
        cloud(cv, w * 0.5 + side * w * 0.2, h * 0.12, s * 0.05 + 0.4, 'e')
        for x in (w * 0.5 - side * w * 0.16, w * 0.5 + side * w * 0.26):
            oval(cv, x, h * 0.95, 1.2, 0.7, 'o')
    return cv, [sky(), role('u', 'sun', 'Sun', BROWN), role('a', 'shell', 'Shell', PINK), role('b', 'ridge', 'Shell ridges', BROWN),
                role('f', 'starfish', 'Starfish', PINK), role('w', 'sea', 'Sea', BLUE), role('d', 'sand', 'Sand', BROWN),
                role('k', 'weed', 'Seaweed', GREEN), role('o', 'pebbles', 'Pebbles', GREEN), role('e', 'cloud', 'Cloud', BLUE)], ['sea', 'beach']


MORE_NATURE = [oak_tree, willow_tree, maple_tree, bamboo, rose, dandelion, clover, venus_flytrap, pine_cone, seashell]

# Expansion roles of these subjects (as expansions.ROLES): subject -> {group: [(roleId, new name or None), ...]}.
MORE_NATURE_ROLES = {
    'oak_tree': {'lime': [('sun', None)], 'red': [('swing', 'Red swing')]},
    'maple_tree': {'lime': [('falling', 'Yellow leaves')], 'red': [('crown', 'Red crown')]},
    'bamboo': {'red': [('sun', 'Red sun')]},
    'rose': {'lime': [('sun', None)], 'red': [('bloom', 'Red rose')]},
    'dandelion': {'lime': [('head', 'Yellow flower')]},
    'clover': {'lime': [('sun', None)], 'red': [('ladybug', None)]},
    'venus_flytrap': {'red': [('mouth', None)]},
    'seashell': {'lime': [('sun', None)], 'red': [('starfish', None)]},
}

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


def coral_reef(w, h, r):
    cv, s, cx, big = start(w, h, 'w')
    gtop = h - ground_rows(h, 0.14)
    hills(cv, 'd', gtop, 0.6, w * 0.9, r.uniform(0, 6))
    side = r.choice((-1, 1))

    def antler(x, y, a, L, k):
        ex, ey = x + math.sin(a) * L, y - math.cos(a) * L
        seg(cv, x, y, ex, ey, 'c', 0.6 if not big else 0.75)
        if k:
            for d in (-1, 1):
                antler(ex, ey, a + d * 0.55, L * 0.7, k - 1)

    antler(cx - side * w * 0.22, gtop + 0.5, side * 0.1, h * 0.17, 2 if not big else 3)
    fx, fy, R = cx + side * w * 0.24, gtop - h * 0.16, s * (0.22 if not big else 0.2)
    seg(cv, fx, gtop + 0.5, fx, fy, 'f', 0.5)
    for gx, gy, px, py in cells(cv):
        a = math.atan2(fy - py, px - fx)
        if 0.3 < a < math.pi - 0.3 and math.hypot(px - fx, py - fy) <= R:
            cv.g[gy][gx] = 'f'
    for a in (1.57,) if not big else (0.8, 1.25, 1.57, 1.9, 2.35):
        seg(cv, fx + math.cos(a) * 1.4, fy - math.sin(a) * 1.4, fx + math.cos(a) * (R + 0.6), fy - math.sin(a) * (R + 0.6), 'w', 0.4)
    bx = cx + side * w * 0.02
    for gx, gy, px, py in cells(cv):
        if py <= gtop + 0.6 and ((px - bx) / (w * 0.16)) ** 2 + ((py - gtop - 0.6) / (h * 0.11)) ** 2 <= 1.0:
            cv.g[gy][gx] = 'b'
    ring(cv, bx, gtop + 0.6, w * 0.1, w * 0.1 - 0.9, 'g', ry_scale=h * 0.11 / (w * 0.16))
    box(cv, 0, gtop + 0.7, w, h, 'd', only='g')
    for x in (w * 0.08, w * 0.94) if not big else (w * 0.06, w * 0.94, w * 0.5 + side * w * 0.42):
        path(cv, [(x, h - 0.5), (x + 0.8, gtop - h * 0.06), (x - 0.4, gtop - h * 0.16), (x + 0.5, gtop - h * 0.26)], 'g', 0.5)
    for k in range(1 if not big else 2):
        ox, oy = w * 0.5 + side * w * (0.12 - 0.3 * k), h * (0.24 + 0.18 * k)
        oval(cv, ox, oy, max(2.0, w * 0.13), max(1.4, h * 0.06), 'o')
        poly(cv, [(ox - side * w * 0.1, oy), (ox - side * w * 0.24, oy - max(1.4, h * 0.07)), (ox - side * w * 0.24, oy + max(1.4, h * 0.07))], 'o')
        if big:
            disc(cv, ox + side * w * 0.07, oy - 0.3, 0.7, 'u')
    for k in range(3 if not big else 4):
        disc(cv, w * 0.5 - side * w * (0.3 + 0.05 * (k % 2)), h * (0.4 - 0.1 * k), 0.6 + 0.15 * k, 'u')
    return cv, [('w', 'water', 'Water', BLUE, True), role('c', 'coral', 'Branching coral', PINK), role('f', 'fan', 'Fan coral', PINK),
                role('b', 'brain', 'Brain coral', GREEN), role('g', 'weed', 'Seaweed and coral folds', GREEN), role('o', 'fish', 'Fish', BROWN),
                role('d', 'sand', 'Sand', BROWN), role('u', 'bubbles', 'Bubbles and eyes', BLUE)], ['sea']


def iceberg(w, h, r):
    cv, s, cx, big = start(w, h, 'n')
    wl = h * 0.62
    for k, c in enumerate('a' if not big else 'ab'):
        ph = r.uniform(0, 6)
        pts = [(w * 0.1 + i * w * 0.8 / 12, h * (0.1 + 0.08 * k) + math.sin(i * 0.8 + ph) * h * 0.03) for i in range(13)]
        path(cv, pts, c, 0.6 if not big else 0.75)
    box(cv, 0, wl, w, h, 'w')
    side = r.choice((-1, 1))
    ix = cx + side * w * 0.06
    poly(cv, [(ix - w * 0.42, wl), (ix + w * 0.38, wl), (ix + w * 0.3, wl + h * 0.14), (ix + w * 0.04, wl + h * 0.28),
              (ix - w * 0.26, wl + h * 0.22)], 'u')
    poly(cv, [(ix - w * 0.34, wl + 0.4), (ix - w * 0.24, wl - h * 0.1), (ix - w * 0.1, wl - h * 0.15), (ix + w * 0.02 * side, wl - h * 0.3),
              (ix + w * 0.14, wl - h * 0.17), (ix + w * 0.26, wl - h * 0.08), (ix + w * 0.32, wl + 0.4)], 'i')
    mx = w * 0.5 - side * w * 0.36
    disc(cv, mx, h * 0.34, s * 0.08, 'm')
    for x, y in ((0.2, 0.04), (0.5, 0.3), (0.86, 0.34), (0.62, 0.05), (0.08, 0.44), (0.94, 0.08)):
        if abs(w * x - mx) > 2.5:
            cv.put(int(w * x), int(h * y), 'x')
    fx, fy = w * 0.5 + side * w * 0.3, h * 0.93
    oval(cv, fx, fy, 1.4, 0.8, 'f')
    poly(cv, [(fx + side * 1.0, fy), (fx + side * 2.4, fy - 1.0), (fx + side * 2.4, fy + 1.0)], 'f')
    if big:
        px, py = ix - w * 0.17, wl - h * 0.09
        oval(cv, px, py - 1.0, 1.3, 1.9, 'k')
        disc(cv, px, py - 3.2, 1.0, 'k')
        poly(cv, [(px + side * 0.8, py - 3.6), (px + side * 2.2, py - 3.2), (px + side * 0.8, py - 2.8)], 'f')
    return cv, [('n', 'sky', 'Polar sky', PINK, True), role('a', 'aurora', 'Aurora', GREEN), role('b', 'aurora2', 'Aurora glow', GREEN),
                role('w', 'sea', 'Sea', BLUE), role('i', 'ice', 'Iceberg', BLUE), role('u', 'under', 'Iceberg under water', BLUE),
                role('m', 'moon', 'Moon', BROWN), role('k', 'penguin', 'Penguin', BROWN), role('x', 'stars', 'Stars', BROWN),
                role('f', 'fish', 'Fish', PINK)], ['sea', 'winter']


def rain_cloud(w, h, r):
    cv, s, cx, big = start(w, h, 'n')
    gtop = h - ground_rows(h, 0.14)
    hills(cv, 'g', gtop, 0.5, w * 1.3, r.uniform(0, 6))
    side = r.choice((-1, 1))
    ccx, ccy = cx + side * w * 0.04, h * 0.24
    if r.random() < 0.6 or big:
        disc(cv, ccx + side * w * 0.28, ccy - h * 0.09, s * 0.14, 'u')
    for dx, dy, rad in ((-0.2, 0.03, 0.15), (0.0, -0.05, 0.2), (0.2, 0.02, 0.15)):
        disc(cv, ccx + dx * w, ccy + dy * h, rad * w, 'c')
    box(cv, ccx - w * 0.33, ccy, ccx + w * 0.33, ccy + h * 0.09, 'c')
    cb = ccy + h * 0.09
    box(cv, 0, cb - 1.0, w, cb, 'k', only='c')
    bolt = r.random() < 0.7
    bx = ccx + side * w * 0.1
    if bolt:
        poly(cv, [(bx - 0.4, cb - 0.4), (bx + 1.6, cb - 0.4), (bx + 0.6, cb + h * 0.1), (bx + 1.8, cb + h * 0.1),
                  (bx - 0.6, cb + h * 0.3), (bx + 0.1, cb + h * 0.14), (bx - 1.1, cb + h * 0.14)], 'y')
    k = 0
    y = cb + 1.6
    while y < gtop - 1.6:
        for x in [ccx - w * 0.3 + (k % 2) * 1.3 + j * 2.6 for j in range(int(w * 0.6 / 2.6) + 1)]:
            if not (bolt and bx - 2.0 < x < bx + 2.6):
                seg(cv, x + 0.3, y, x - 0.3, y + 0.9, 'r', 0.42)
        y += 2.4
        k += 1
    oval(cv, cx - side * w * 0.1, gtop + 0.6, w * 0.16, 0.9, 'p')
    for x in (w * 0.5 + side * w * 0.22, w * 0.5 + side * w * 0.36):
        seg(cv, x, gtop + 0.5, x, gtop - h * 0.06, 'b', 0.4)
        disc(cv, x, gtop - h * 0.07, 0.9, 'f')
    oval(cv, w * 0.5 - side * w * 0.4, gtop - 0.2, w * 0.1, h * 0.06, 'b')
    return cv, [('n', 'sky', 'Stormy sky', PINK, True), role('c', 'cloud', 'Rain cloud', BLUE), role('k', 'shade', 'Cloud underside', BLUE),
                role('r', 'rain', 'Raindrops', BLUE), role('p', 'puddle', 'Puddle', BLUE), role('y', 'bolt', 'Lightning', BROWN),
                role('u', 'sun', 'Sun', BROWN), role('g', 'grass', 'Grass', GREEN), role('b', 'bush', 'Bush and stems', GREEN),
                role('f', 'flowers', 'Flowers', PINK)], ['weather']


def comet(w, h, r):
    cv, s, cx, big = start(w, h, 'n')
    gtop = h - ground_rows(h, 0.12)
    side = r.choice((-1, 1))
    hills(cv, 'g', gtop, 0.7, w * 1.2, r.uniform(0, 6))
    hx, hy = cx + side * w * 0.18, h * r.uniform(0.34, 0.4)
    ex, ey = cx - side * w * 0.62, -h * 0.12
    L = math.hypot(ex - hx, ey - hy)
    ux, uy = (ex - hx) / L, (ey - hy) / L
    nx, ny = -uy, ux
    R = s * 0.12 + 0.3
    for c, k0, k1 in (('o', 1.15, s * 0.3), ('i', 0.65, s * 0.13)):
        poly(cv, [(hx + nx * R * k0, hy + ny * R * k0), (ex + nx * k1, ey + ny * k1), (ex - nx * k1, ey - ny * k1),
                  (hx - nx * R * k0, hy - ny * R * k0)], c)
    disc(cv, hx, hy, R, 'h')
    mx, my = cx + side * w * 0.36, h * 0.1
    disc(cv, mx, my, s * 0.08, 'm')
    if big:
        seg(cv, mx - s * 0.14, my + 0.6, mx + s * 0.14, my - 0.6, 'm', 0.4)
    spots = [(x, y) for y in range(1, gtop - 1) for x in range(1, w - 1) if cv.g[y][x] == 'n'
             and all(cv.g[y + b][x + a] == 'n' for a in (-1, 0, 1) for b in (-1, 0, 1))]
    r.shuffle(spots)
    placed = []
    for x, y in spots:
        if len(placed) >= 5 + (w * h) // 90:
            break
        if all(max(abs(x - a), abs(y - b)) >= 3 for a, b in placed):
            cv.g[y][x] = 'x'
            placed.append((x, y))
    ox = cx - side * w * 0.28
    disc(cv, ox, gtop + 0.2, s * 0.09 + 0.4, 'k')
    box(cv, ox - s * 0.12, gtop + 0.2, ox + s * 0.12, gtop + 1.2, 'k')
    seg(cv, ox, gtop - 0.4, ox + side * s * 0.16, gtop - s * 0.12, 'k', 0.45)
    return cv, [('n', 'night', 'Night sky', BLUE, True), role('h', 'head', 'Comet head', BROWN), role('i', 'tail', 'Inner tail', PINK),
                role('o', 'glow', 'Outer tail', GREEN), role('x', 'stars', 'Stars', BROWN), role('m', 'planet', 'Planet', BLUE),
                role('k', 'dome', 'Observatory', PINK), role('g', 'hill', 'Hill', GREEN)], ['sky', 'night']


def snowflake(w, h, r):
    cv, s, cx, big = start(w, h, 'n')
    gtop = h - ground_rows(h, 0.14)
    hills(cv, 'g', gtop, 0.6, w * 1.3, r.uniform(0, 6))
    side = r.choice((-1, 1))
    fx, fy = cx + r.uniform(-0.5, 0.5), h * 0.4
    R = min(s * (0.34 if not big else 0.36), fy - 1.0)
    turn = r.uniform(-8, 8)
    t = 0.55 if not big else 0.65
    for k in range(6):
        a = math.radians(90 + 60 * k + turn)
        ux, uy = math.cos(a), -math.sin(a)
        seg(cv, fx, fy, fx + ux * R, fy + uy * R, 'f', t)
        for at, L in ((0.55, 0.3),) if not big else ((0.45, 0.28), (0.75, 0.2)):
            px, py = fx + ux * R * at, fy + uy * R * at
            for d in (-1, 1):
                b = a + d * math.radians(55)
                seg(cv, px, py, px + math.cos(b) * R * L, py - math.sin(b) * R * L, 'f', t - 0.1)
    disc(cv, fx, fy, 1.2 if not big else 1.6, 'f')
    tx = w * 0.5 - side * w * 0.4
    for k in range(3):
        yb = gtop + 0.5 - k * h * 0.07
        half = w * (0.11 - k * 0.025)
        poly(cv, [(tx - half, yb), (tx, yb - h * 0.12), (tx + half, yb)], 'p')
    kx = w * 0.5 + side * w * 0.36
    box(cv, kx - w * 0.12, gtop - h * 0.13, kx + w * 0.12, gtop + 0.5, 'k')
    poly(cv, [(kx - w * 0.15, gtop - h * 0.13 + 0.4), (kx, gtop - h * 0.22), (kx + w * 0.15, gtop - h * 0.13 + 0.4)], 'o')
    box(cv, kx - 1.0, gtop - h * 0.1, kx + 1.0, gtop - h * 0.1 + 1.9, 'y')
    mx = w * 0.5 + side * w * 0.4
    disc(cv, mx, h * 0.08, s * 0.07, 'm')
    spots = [(x, y) for y in range(1, gtop - 1) for x in range(1, w - 1) if cv.g[y][x] == 'n'
             and math.hypot(x + 0.5 - fx, y + 0.5 - fy) > R + 1.5
             and all(cv.g[y + b][x + a] == 'n' for a in (-1, 0, 1) for b in (-1, 0, 1))]
    r.shuffle(spots)
    placed = []
    for x, y in spots:
        if len(placed) >= 4 + (w * h) // 100:
            break
        if all(max(abs(x - a), abs(y - b)) >= 3 for a, b in placed):
            cv.g[y][x] = 'x'
            placed.append((x, y))
    return cv, [('n', 'sky', 'Evening sky', PINK, True), role('f', 'flake', 'Snowflake', BLUE), role('x', 'flakes', 'Little flakes', BLUE),
                role('g', 'snow', 'Snow', BLUE), role('o', 'roof', 'Snowy roof', BLUE), role('p', 'pine', 'Pine', GREEN),
                role('m', 'moon', 'Moon', BROWN), role('k', 'cabin', 'Cabin', BROWN), role('y', 'window', 'Window', BROWN)], ['weather', 'winter']


# ---- Small places ----

def haystack(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.2)
    hills(cv, 't', h * 0.66, 0.6, w * 1.4, r.uniform(0, 6))
    box(cv, 0, gtop, w, h, 'g')
    side = r.choice((-1, 1))
    bx = cx - side * w * 0.3
    by = h * 0.62
    box(cv, bx - w * 0.1, by - h * 0.08, bx + w * 0.1, by + 0.5, 'b')
    poly(cv, [(bx - w * 0.13, by - h * 0.08 + 0.4), (bx, by - h * 0.15), (bx + w * 0.13, by - h * 0.08 + 0.4)], 'b')
    box(cv, bx - 0.6, by - h * 0.05, bx + 0.6, by + 0.5, 'x')
    hx, rx, top = cx + side * w * 0.1, w * 0.3, h * 0.3
    for gx, gy, px, py in cells(cv):
        if py <= gtop + 1.0:
            t = (gtop + 1.0 - py) / (gtop + 1.0 - top)
            if 0 <= t <= 1 and abs(px - hx) <= rx * (1 - t ** 2.2) ** 0.6:
                cv.g[gy][gx] = 'h'
    for k in (-2, -1, 0, 1, 2) if big else (-1, 0, 1):
        x1 = hx + k * rx * (0.32 if big else 0.42)
        path(cv, [(hx + k * 0.4, top + 2.0), (x1 * 0.6 + hx * 0.4, (top + gtop) / 2), (x1, gtop + 0.5)], 'k', 0.42)
    px0, py0 = hx - side * (rx + 0.6), gtop + 0.5
    px1, py1 = hx - side * (rx - w * 0.06), top + h * 0.06
    seg(cv, px0, py0, px1, py1, 'p', 0.45)
    ux, uy = (px1 - px0), (py1 - py0)
    L = math.hypot(ux, uy)
    ux, uy = ux / L, uy / L
    box(cv, px1 - 1.4, mid(py1) - 0.3, px1 + 1.4, mid(py1) + 0.3, 'p')
    for d in (-1.2, 0.0, 1.2):
        seg(cv, px1 + d, py1, px1 + d + ux * 1.6, py1 + uy * 1.6, 'p', 0.4)
    disc(cv, w * 0.5 + side * w * 0.38, h * 0.08, s * 0.085, 'u')
    cloud(cv, w * 0.5 - side * w * 0.28, h * 0.12, s * 0.05 + 0.4, 'e')
    if big:
        for x in (w * 0.5 + side * w * 0.42,):
            rbox(cv, x - 1.6, gtop - 1.6, x + 1.6, gtop + 0.6, 0.8, 'h')
    return cv, [sky(), role('u', 'sun', 'Sun', BROWN), role('k', 'straw', 'Straw lines', BROWN), role('h', 'hay', 'Haystack', BROWN),
                role('p', 'fork', 'Pitchfork', PINK), role('b', 'barn', 'Barn', PINK), role('x', 'barn_door', 'Barn door', BLUE),
                role('g', 'field', 'Field', GREEN), role('t', 'meadow', 'Far meadow', GREEN), role('e', 'cloud', 'Cloud', BLUE)], ['farm']


def mailbox(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.14)
    hills(cv, 'g', gtop, 0.4, w * 1.3, r.uniform(0, 6))
    side = r.choice((-1, 1))
    oval(cv, w * 0.5 - side * w * 0.36, gtop - 0.3, w * 0.15, h * 0.1, 'b')
    px = cx + side * w * 0.04
    box(cv, px - 0.9, h * 0.5, px + 0.9, gtop + 0.5, 'p')
    x0, x1, y0, y1 = px - w * 0.27, px + w * 0.27, h * 0.27, h * 0.5
    rbox(cv, x0, y0, x1, y1, (y1 - y0) * 0.45, 'm')
    fx = x1 if side < 0 else x0
    rbox(cv, fx - 1.2, y0 + 0.3, fx + 1.2, y1 - 0.2, 1.0, 'd')
    gx = x0 + 1.6 if side < 0 else x1 - 1.6
    seg(cv, gx, y1 - 1.2, gx, y0 - h * 0.12, 'f', 0.45)
    box(cv, gx - (2.6 if side < 0 else 0.0), y0 - h * 0.13, gx + (0.0 if side < 0 else 2.6), y0 - h * 0.13 + (1.4 if not big else 2.2), 'f')
    if big:
        box(cv, fx - side * 0.2 - 0.6, y0 + 1.5, fx - side * 0.2 + 0.6, y1 - 1.5, 'l')
    for k, x in enumerate((px - 2.2, px + 2.2) if not big else (px - 2.8, px + 2.8, px - 4.6)):
        seg(cv, x, gtop + 0.5, x, gtop - 1.4, 'b', 0.4)
        disc(cv, x, gtop - 1.8, 0.9, 'o')
    disc(cv, w * 0.5 - side * w * 0.38, h * 0.09, s * 0.085, 'u')
    cloud(cv, w * 0.5 - side * w * 0.06, h * 0.1, s * 0.05 + 0.4, 'e')
    return cv, [sky(), role('u', 'sun', 'Sun', BROWN), role('p', 'post', 'Post', BROWN), role('f', 'flag', 'Flag', BROWN),
                role('m', 'box', 'Mailbox', PINK), role('d', 'door', 'Mailbox door', BLUE), role('l', 'letter', 'Letter', GREEN),
                role('o', 'flowers', 'Flowers', PINK), role('g', 'lawn', 'Lawn', GREEN), role('b', 'bush', 'Bush and stems', GREEN),
                role('e', 'cloud', 'Cloud', BLUE)], ['town', 'cozy']


def phone_booth(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.1)
    box(cv, 0, gtop, w, h, 'v')
    side = r.choice((-1, 1))
    bx = cx + side * w * 0.12
    c0 = int(bx - w * 0.25 + 0.5)
    n = 2 if not big else 3
    pw = 2
    c1 = c0 + n * pw + n
    y0, y1 = int(h * 0.24), int(gtop)
    box(cv, c0, y0, c1 + 1, y1, 'b')
    mxx = (c0 + c1 + 1) / 2
    oval(cv, mxx, y0 + 0.3, (c1 + 1 - c0) / 2 + 0.2, h * 0.06, 'r')
    box(cv, mxx - 0.6, y0 - h * 0.08, mxx + 0.6, y0, 'r')
    box(cv, c0 + 1, y0 + 1.1, c1 - 0.1, y0 + 1.9, 'k')
    rows = (y1 - 1 - (y0 + 3)) // 3
    for j in range(rows):
        for i in range(n):
            x = c0 + 1 + i * (pw + 1)
            y = y0 + 3 + j * 3
            box(cv, x, y, x + pw - 0.1, y + 1.9, 'w')
    if big:
        box(cv, c0 + 1, y0 + 3 + 3, c0 + 1.9, y0 + 3 + 3 + 0.9, 'k')
    box(cv, c1 + 0.5, (y0 + y1) / 2, c1 + 0.9, (y0 + y1) / 2 + 1.0, 'k', only='b')
    tx = w * 0.5 - side * w * 0.32
    box(cv, tx - 0.6, h * 0.5, tx + 0.6, gtop + 0.5, 't')
    disc(cv, tx, h * 0.4, s * 0.17, 'g')
    oval(cv, w * 0.5 - side * w * 0.12, gtop - 0.2, w * 0.1, h * 0.06, 'u')
    cloud(cv, w * 0.5 - side * w * 0.3, h * 0.1, s * 0.05 + 0.4, 'e')
    return cv, [sky(), role('b', 'booth', 'Phone booth', PINK), role('r', 'crown', 'Booth roof', PINK), role('w', 'pane', 'Window panes', BLUE),
                role('k', 'sign', 'Sign and handle', BROWN), role('t', 'trunk', 'Tree trunk', BROWN), role('v', 'pavement', 'Pavement', BROWN),
                role('g', 'tree', 'Tree', GREEN), role('u', 'hedge', 'Hedge', GREEN), role('e', 'cloud', 'Cloud', BLUE)], ['town']


def street_lamp(w, h, r):
    cv, s, cx, big = start(w, h, 'n')
    gtop = h - ground_rows(h, 0.12)
    box(cv, 0, gtop, w, h, 'g')
    side = r.choice((-1, 1))
    lx = cx - side * w * 0.12
    ly = h * 0.22
    disc(cv, lx, ly, s * 0.27, 'o')
    box(cv, lx - 1.3, gtop - h * 0.07, lx + 1.3, gtop + 0.5, 'p')
    box(cv, lx - 0.5, ly, lx + 0.5, gtop, 'p')
    poly(cv, [(lx - 2.0, ly - h * 0.08), (lx + 2.0, ly - h * 0.08), (lx + 1.3, ly + h * 0.06), (lx - 1.3, ly + h * 0.06)], 'y')
    poly(cv, [(lx - 2.6, ly - h * 0.08 + 0.4), (lx, ly - h * 0.17), (lx + 2.6, ly - h * 0.08 + 0.4)], 'p')
    box(cv, lx - 1.5, ly + h * 0.06, lx + 1.5, ly + h * 0.06 + 0.9, 'p')
    if big:
        box(cv, lx - 0.5, ly - h * 0.08, lx + 0.5, ly + h * 0.06, 'p')
        box(cv, lx - 2.2, h * 0.44, lx + 2.2, h * 0.44 + 0.7, 'p')
    bx = w * 0.5 + side * w * 0.22
    box(cv, bx - w * 0.16, gtop - h * 0.08, bx + w * 0.16, gtop - h * 0.08 + 0.9, 'b')
    box(cv, bx - w * 0.16, gtop - h * 0.17, bx + w * 0.16, gtop - h * 0.17 + 0.9, 'b')
    for x in (bx - w * 0.13, bx + w * 0.13):
        seg(cv, x, gtop + 0.5, x, gtop - h * 0.17, 'b', 0.42)
    ux = w * 0.5 - side * w * 0.42
    oval(cv, ux, gtop - 0.3, w * 0.12, h * 0.09, 'u')
    for dx in (-1.0, 1.1):
        disc(cv, ux + dx, gtop - h * 0.07, 0.75, 'f')
    disc(cv, w * 0.5 + side * w * 0.36, h * 0.12, s * 0.08, 'm')
    disc(cv, w * 0.5 + side * w * 0.36 + side * s * 0.05, h * 0.12 - s * 0.03, s * 0.065, 'n')
    for x, y in ((0.1, 0.06), (0.62, 0.04), (0.88, 0.36), (0.12, 0.44), (0.56, 0.34)):
        xx = w * (x if side > 0 else 1 - x)
        if math.hypot(xx - lx, h * y - ly) > s * 0.32:
            cv.put(int(xx), int(h * y), 'x')
    return cv, [('n', 'sky', 'Evening sky', PINK, True), role('p', 'post', 'Lamp post', BLUE), role('o', 'glow', 'Glow', BROWN),
                role('b', 'bench', 'Bench', BROWN), role('y', 'light', 'Lamp light', BROWN), role('x', 'stars', 'Stars', BROWN),
                role('m', 'moon', 'Moon', BLUE), role('g', 'grass', 'Grass', GREEN), role('u', 'bush', 'Bush', GREEN),
                role('f', 'flowers', 'Flowers', PINK)], ['town', 'night']


# ---- The playground ----

def swing_set(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.12)
    hills(cv, 'g', gtop, 0.3, w * 1.5, r.uniform(0, 6))
    side = r.choice((-1, 1))
    top = h * 0.32
    sp = w * 0.07
    x0 = mid(1.5 + sp)
    x1 = w - x0
    for x, d in ((x0, -1), (x1, 1)):
        seg(cv, x + d * sp, gtop + 0.5, x, top, 'f', 0.5 if not big else 0.6)
        seg(cv, x, gtop + 0.5, x, top, 'f', 0.45 if not big else 0.55)
    seg(cv, x0, top, x1, top, 'f', 0.55 if not big else 0.7)
    chains = [(x0 + 1, x0 + 3), (x1 - 3, x1 - 1)]
    if big and x1 - x0 >= 14:
        c = mid(w / 2 - 0.5)
        chains.insert(1, (c - 1, c + 1))
    sy = mid(h * 0.72)
    for a, b in chains:
        oval(cv, (a + b) / 2, gtop + 0.4, 1.3, 0.8, 'd')
        for x in (a, b):
            seg(cv, x, top + 0.6, x, sy, 'c', 0.42)
        box(cv, a, sy - 0.3, b, sy + 0.3, 't')
    disc(cv, w * 0.5 + side * w * 0.38, h * 0.1, s * 0.085, 'u')
    cloud(cv, w * 0.5 - side * w * 0.26, h * 0.12, s * 0.05 + 0.4, 'e')
    if big:
        oval(cv, w * 0.5 - side * w * 0.46, gtop - 0.2, w * 0.05, h * 0.03, 'b')
    return cv, [sky(), role('u', 'sun', 'Sun', BROWN), role('f', 'frame', 'Frame', PINK), role('c', 'chain', 'Chains', BROWN),
                role('t', 'seat', 'Seats', PINK), role('d', 'patch', 'Worn ground', BROWN), role('g', 'grass', 'Grass', GREEN),
                role('b', 'bush', 'Bush', GREEN), role('e', 'cloud', 'Cloud', BLUE)], ['playground', 'park']


def playground_slide(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.12)
    hills(cv, 'g', gtop, 0.3, w * 1.5, r.uniform(0, 6))
    side = r.choice((-1, 1))
    tx, tw, ply = cx - side * w * 0.22, w * 0.11, h * 0.44
    for d in (-1, 1):
        seg(cv, tx + d * tw, gtop + 0.5, tx + d * tw, ply - h * 0.15, 'f', 0.45 if not big else 0.55)
    y = gtop - 1.6
    while y > ply + 1.2:
        seg(cv, tx - tw, y, tx + tw, y, 'f', 0.42)
        y -= 2.0 if not big else 2.4
    box(cv, tx - tw - 0.4, mid(ply) - 0.3, tx + tw + 0.4, mid(ply) + 0.3, 'f')
    poly(cv, [(tx - tw - 1.4, ply - h * 0.14), (tx, ply - h * 0.27), (tx + tw + 1.4, ply - h * 0.14)], 'r')
    pts = [(tx + side * (tw + 0.4), ply + 0.4), (tx + side * (tw + w * 0.17), ply + (gtop - ply) * 0.32),
           (tx + side * (tw + w * 0.33), gtop - 1.4), (tx + side * (tw + w * 0.5), gtop - 0.9)]
    path(cv, pts, 'c', 0.75 if not big else 0.95)
    if big:
        path(cv, [(x, y - 1.3) for x, y in pts[:3]], 'k', 0.4)
        seg(cv, pts[1][0], pts[1][1] + 0.5, pts[1][0], gtop + 0.5, 'f', 0.45)
    disc(cv, w * 0.5 + side * w * 0.36, h * 0.1, s * 0.085, 'u')
    cloud(cv, w * 0.5 - side * w * 0.12, h * 0.1, s * 0.05 + 0.4, 'e')
    oval(cv, w * 0.5 - side * w * 0.44, gtop - 0.2, w * 0.1, h * 0.06, 'b')
    return cv, [sky(), role('u', 'sun', 'Sun', BROWN), role('c', 'chute', 'Slide', BROWN), role('k', 'rim', 'Slide rim', BROWN),
                role('f', 'frame', 'Ladder and tower', PINK), role('r', 'roof', 'Roof', PINK), role('g', 'grass', 'Grass', GREEN),
                role('b', 'bush', 'Bush', GREEN), role('e', 'cloud', 'Cloud', BLUE)], ['playground', 'park']


def seesaw(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.14)
    hills(cv, 'g', gtop, 0.3, w * 1.5, r.uniform(0, 6))
    side = r.choice((-1, 1))
    ph = h * 0.2
    poly(cv, [(cx - w * 0.1, gtop + 0.5), (cx, gtop - ph), (cx + w * 0.1, gtop + 0.5)], 'v')
    L = w * 0.44
    a = math.asin(min(0.9, (ph - 0.6) / L))
    lo = (cx - side * L * math.cos(a), gtop - ph + L * math.sin(a))
    hi = (cx + side * L * math.cos(a), gtop - ph - L * math.sin(a))
    seg(cv, lo[0], lo[1], hi[0], hi[1], 'p', 0.6 if not big else 0.75)
    for e, f in ((lo, 0.84), (hi, 0.84)):
        x, y = cx + (e[0] - cx) * f, gtop - ph + (e[1] - gtop + ph) * f
        seg(cv, x, y - 0.5, x, y - 2.4, 'h', 0.42)
        seg(cv, x - 1.0, y - 2.4, x + 1.0, y - 2.4, 'h', 0.42)
    disc(cv, cx, gtop - ph, 0.8, 'v')
    for x in (w * 0.5 - side * w * 0.38, w * 0.5 + side * w * 0.12):
        disc(cv, x, gtop + 0.8, 0.9, 'o')
    oval(cv, w * 0.5 + side * w * 0.42, gtop - 0.2, w * 0.1, h * 0.07, 'b')
    disc(cv, w * 0.5 - side * w * 0.36, h * 0.1, s * 0.085, 'u')
    cloud(cv, w * 0.5 + side * w * 0.22, h * 0.14, s * 0.05 + 0.4, 'e')
    if big:
        bx, by = hi[0] - side * 1.2, hi[1] - 1.6
        oval(cv, bx, by, 1.2, 0.8, 'k')
        disc(cv, bx + side * 1.0, by - 0.8, 0.7, 'k')
    return cv, [sky(), role('u', 'sun', 'Sun', BROWN), role('v', 'pivot', 'Pivot', BROWN), role('h', 'handle', 'Handles', BROWN),
                role('p', 'plank', 'Plank', PINK), role('o', 'flowers', 'Flowers', PINK), role('k', 'bird', 'Bird', BLUE),
                role('g', 'grass', 'Grass', GREEN), role('b', 'bush', 'Bush', GREEN), role('e', 'cloud', 'Cloud', BLUE)], ['playground', 'park']


def dog_house(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.12)
    hills(cv, 'g', gtop, 0.3, w * 1.5, r.uniform(0, 6))
    side = r.choice((-1, 1))
    hx, hw, wy = cx - side * w * 0.08, w * 0.27, h * 0.47
    box(cv, hx - hw, wy, hx + hw, gtop + 0.5, 'h')
    if big:
        for y in (wy + h * 0.12, wy + h * 0.24):
            box(cv, hx - hw, mid(y) - 0.3, hx + hw, mid(y) + 0.3, 'n')
    poly(cv, [(hx - hw - 1.3, wy + 1.0), (hx, h * 0.2), (hx + hw + 1.3, wy + 1.0)], 'r')
    dw = hw * 0.46
    rbox(cv, hx - dw, wy + h * 0.12, hx + dw, gtop + 0.5, dw, 'd')
    if big:
        dy = wy + h * 0.27
        disc(cv, hx, dy, dw * 0.7, 'k')
        for d in (-1, 1):
            oval(cv, hx + d * dw * 0.72, dy + 0.3, 0.8, 1.6, 'k')
            cv.put(int(hx + d * 0.9), int(dy - 0.7), 'x')
        oval(cv, hx, dy + 1.0, 0.9, 0.6, 'x')
    kx = hx + side * (hw + w * 0.13)
    for gx, gy, px, py in cells(cv):
        if gtop - 1.3 <= py <= gtop + 0.6 and abs(px - kx) <= w * 0.09 - (gtop + 0.6 - py) * -0.4:
            cv.g[gy][gx] = 'b'
    ox = w * 0.5 + side * w * 0.36 if kx < w * 0.5 + side * w * 0.3 or side < 0 else w * 0.5 + side * w * 0.2
    box(cv, kx - 1.4, mid(gtop + 1.0) - 0.3, kx + 1.4, mid(gtop + 1.0) + 0.3, 'o')
    for d in (-1, 1):
        disc(cv, kx + d * 1.6, gtop + 1.5, 0.6, 'o')
    disc(cv, w * 0.5 + side * w * 0.36, h * 0.09, s * 0.085, 'u')
    cloud(cv, w * 0.5 - side * w * 0.3, h * 0.08, s * 0.05 + 0.4, 'e')
    oval(cv, w * 0.5 - side * w * 0.46, gtop - 0.2, w * 0.08, h * 0.06, 't')
    return cv, [sky(), role('u', 'sun', 'Sun', BROWN), role('h', 'house', 'Dog house', BROWN), role('n', 'planks', 'Plank lines', BROWN),
                role('r', 'roof', 'Roof', PINK), role('d', 'door', 'Doorway', BLUE), role('k', 'dog', 'Dog', BROWN), role('x', 'face', 'Eyes and nose', PINK),
                role('b', 'bowl', 'Bowl', PINK), role('o', 'bone', 'Bone', BLUE), role('g', 'grass', 'Grass', GREEN),
                role('t', 'bush', 'Bush', GREEN), role('e', 'cloud', 'Cloud', BLUE)], ['cozy', 'garden']


def water_tower(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.12)
    hills(cv, 'g', gtop, 0.4, w * 1.4, r.uniform(0, 6))
    side = r.choice((-1, 1))
    tx = cx + side * w * 0.05
    x0, x1, y0, y1 = tx - w * 0.24, tx + w * 0.24, h * 0.22, h * 0.5
    lt = 0.5
    for d in (-1, 1):
        seg(cv, tx + d * w * 0.17, y1, tx + d * w * 0.27, gtop + 0.5, 'l', lt)
        if big:
            seg(cv, tx + d * w * 0.06, y1, tx + d * w * 0.09, gtop + 0.5, 'l', 0.42)
    ya, yb = y1 + (gtop - y1) * 0.18, y1 + (gtop - y1) * 0.62
    xa, xb = w * 0.17 + (w * 0.1) * 0.18, w * 0.17 + (w * 0.1) * 0.62
    seg(cv, tx - xa, ya, tx + xb, yb, 'l', 0.42)
    seg(cv, tx + xa, ya, tx - xb, yb, 'l', 0.42)
    rbox(cv, x0, y0, x1, y1, 1.0, 't')
    oval(cv, tx, y1, w * 0.22, 0.9, 't')
    for y in (((y0 + y1) / 2,) if not big else (y0 + 1.8, (y0 + y1) / 2, y1 - 1.8)):
        box(cv, x0 + 0.6, mid(y) - 0.3, x1 - 0.6, mid(y) + 0.3, 'k', only='t')
    poly(cv, [(x0 - 0.9, y0 + 0.7), (tx, h * 0.08), (x1 + 0.9, y0 + 0.7)], 'r')
    seg(cv, tx, h * 0.08, tx, h * 0.04, 'r', 0.42)
    disc(cv, w * 0.5 - side * w * 0.38, h * 0.1, s * 0.085, 'u')
    cloud(cv, w * 0.5 + side * w * 0.3, h * 0.12, s * 0.05 + 0.4, 'e')
    for x in (w * 0.5 - side * w * 0.42, w * 0.5 + side * w * 0.44):
        oval(cv, x, gtop - 0.2, w * 0.08, h * 0.06, 'b')
    return cv, [sky(), role('u', 'sun', 'Sun', BROWN), role('t', 'tank', 'Tank', BROWN), role('k', 'hoops', 'Hoops', BLUE),
                role('r', 'roof', 'Roof', PINK), role('l', 'legs', 'Legs and braces', PINK), role('g', 'field', 'Field', GREEN),
                role('b', 'bush', 'Bushes', GREEN), role('e', 'cloud', 'Cloud', BLUE)], ['farm', 'town']


def beach_hut(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    sea, sand = h * 0.48, h * 0.72
    hills(cv, 'w', sea, 0.3, w * 0.5, r.uniform(0, 6))
    hills(cv, 'd', sand, 0.4, w * 1.2, r.uniform(0, 6))
    side = r.choice((-1, 1))

    def hut(x, half, y0, y1):
        c0 = int(x - half + 0.5)
        n = int(2 * half) // 2 * 2 + 1
        for gy in range(int(y0), int(y1)):
            for gx in range(c0, c0 + n):
                if 0 <= gx < w:
                    cv.g[gy][gx] = 'a' if (gx - c0) % 2 == 0 else 'b'
        xm = c0 + n / 2
        poly(cv, [(c0 - 1.2, int(y0) + 0.6), (xm, y0 - h * 0.14), (c0 + n + 1.2, int(y0) + 0.6)], 'r')
        rbox(cv, xm - 1.4, y0 + (y1 - y0) * 0.35, xm + 1.4, y1, 0.6, 'o')
        return xm

    xm = hut(cx + side * w * 0.12, w * 0.22, h * 0.42, sand + 0.5 + 1)
    seg(cv, xm, h * 0.28, xm, h * 0.17, 'f', 0.42)
    poly(cv, [(xm + 0.4, h * 0.16), (xm + 0.4 + max(1.8, w * 0.1), h * 0.19), (xm + 0.4, h * 0.23)], 'f')
    if big:
        hut(w * 0.5 - side * w * 0.3, w * 0.12, h * 0.52, sand + 1.5)
    for x in (w * 0.5 - side * w * 0.38, w * 0.5 + side * w * 0.44) if not big else (w * 0.5 - side * w * 0.08, w * 0.5 + side * w * 0.44, w * 0.06):
        for d in (-1, 0, 1):
            seg(cv, x, h - 1.0, x + d * 0.9, h - 2.6, 'g', 0.4)
    disc(cv, w * 0.5 - side * w * 0.36, h * 0.1, s * 0.09, 'u')
    if big:
        cloud(cv, w * 0.5 + side * w * 0.36, h * 0.08, s * 0.05 + 0.4, 'e')
    return cv, [sky(), role('u', 'sun', 'Sun', BROWN), role('r', 'roof', 'Roof', BROWN), role('d', 'sand', 'Sand', BROWN),
                role('a', 'stripe', 'Stripes', PINK), role('b', 'stripe2', 'Other stripes', GREEN), role('o', 'door', 'Door', BLUE),
                role('f', 'flag', 'Flag', PINK), role('w', 'sea', 'Sea', BLUE), role('g', 'grass', 'Beach grass', GREEN),
                role('e', 'cloud', 'Cloud', BLUE)], ['beach', 'sea']


MORE_NATURE = [oak_tree, willow_tree, maple_tree, bamboo, rose, dandelion, clover, venus_flytrap, pine_cone, seashell, coral_reef,
               iceberg, rain_cloud, comet, snowflake, haystack, mailbox, phone_booth, street_lamp, swing_set, playground_slide,
               seesaw, dog_house, water_tower, beach_hut]

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
    'coral_reef': {'lime': [('fish', 'Yellow fish')], 'red': [('coral', 'Red coral')]},
    'iceberg': {'lime': [('moon', None), ('stars', None)], 'red': [('fish', None)]},
    'rain_cloud': {'lime': [('bolt', None), ('sun', None)], 'red': [('flowers', None)]},
    'comet': {'lime': [('head', None), ('stars', None)]},
    'snowflake': {'lime': [('moon', None), ('window', 'Lit window')]},
    'haystack': {'lime': [('hay', None)], 'red': [('barn', None)]},
    'mailbox': {'lime': [('sun', None)], 'red': [('box', None)]},
    'phone_booth': {'lime': [('sign', 'Lit sign')], 'red': [('booth', None)]},
    'street_lamp': {'lime': [('light', None), ('glow', None)], 'red': [('flowers', None)]},
    'swing_set': {'lime': [('sun', None)], 'red': [('frame', 'Red frame')]},
    'playground_slide': {'lime': [('chute', 'Yellow slide')], 'red': [('roof', None)]},
    'seesaw': {'lime': [('sun', None)], 'red': [('plank', 'Red plank')]},
    'dog_house': {'lime': [('sun', None)], 'red': [('roof', None)]},
    'water_tower': {'lime': [('sun', None)], 'red': [('roof', None)]},
    'beach_hut': {'lime': [('sun', None)], 'red': [('stripe', 'Red stripes')]},
}

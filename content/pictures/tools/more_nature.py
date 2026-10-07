"""More subjects for the levels: nature (more_subjects.py). Trees and plants, the sea and the weather, and small places
of a town and a playground, in the procedural style of garden_subjects.py and world_subjects.py: each draws with
picture_kit and returns (canvas, roles, themes). The shapes that make a subject (a crown against the sky, a trunk on
the grass, a pod's jaws and their teeth) come from different color groups, so they stay apart under any mapping, and
most subjects have two roles in three groups, so they can carry six distinct variants. The random stream moves each
picture's scene (sides, props, the time of day), and the big boards (from 300 cells) get more detail.
"""
import math

from picture_kit import (BLUE, BROWN, GREEN, PINK, box, cloud, disc, dots, ground_rows, hills, lens, oval, path, poly,
                         rbox, ring, role, scatter, seg, sky, star, start)


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


def maple_sprite(cv, x, y, c):
    """A small maple leaf of five by five cells, centered on (x, y)."""
    for j, line in enumerate(('..x..', 'x.x.x', 'xxxxx', '.xxx.', '..x..')):
        for i, ch in enumerate(line):
            if ch == 'x':
                cv.put(int(x) - 2 + i, int(y) - 2 + j, c)


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


MORE_NATURE = [oak_tree, willow_tree, maple_tree, bamboo, rose]

# Expansion roles of these subjects (as expansions.ROLES): subject -> {group: [(roleId, new name or None), ...]}.
MORE_NATURE_ROLES = {
    'oak_tree': {'lime': [('sun', None)], 'red': [('swing', 'Red swing')]},
    'maple_tree': {'lime': [('falling', 'Yellow leaves')], 'red': [('crown', 'Red crown')]},
    'bamboo': {'red': [('sun', 'Red sun')]},
    'rose': {'lime': [('sun', None)], 'red': [('bloom', 'Red rose')]},
}

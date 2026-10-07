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


def maple_leaf(cv, x, y, size, c, turn=0.0):
    """A five-lobed maple leaf `size` cells from its middle to its tips, its stem notch down (turned by `turn`)."""
    pts = []
    for a, rr in ((0, 1.0), (35, 0.42), (70, 0.95), (100, 0.42), (130, 0.62), (180, 0.22), (230, 0.62), (260, 0.42),
                  (290, 0.95), (325, 0.42)):
        t = math.radians(a + turn)
        pts.append((x + math.sin(t) * rr * size, y - math.cos(t) * rr * size))
    poly(cv, pts, c)
    return [(x + math.sin(math.radians(a + turn)) * size * k, y - math.cos(math.radians(a + turn)) * size * k)
            for a, k in ((0, 0.9), (70, 0.85), (290, 0.85), (130, 0.5), (230, 0.5))]


# ---- Trees and plants ----

def oak_tree(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.12)
    hills(cv, 'g', gtop, 0.4, w * 1.4, r.uniform(0, 6))
    tx = cx + r.uniform(-0.06, 0.02) * w
    tw = max(1.2, w * 0.08)
    poly(cv, [(tx - tw, h * 0.45), (tx + tw, h * 0.45), (tx + tw * 1.9, gtop + 0.6), (tx - tw * 1.9, gtop + 0.6)], 't')
    seg(cv, tx, h * 0.6, tx - w * 0.24, h * 0.42, 't', 0.7 if not big else 0.9)
    seg(cv, tx, h * 0.56, tx + w * 0.26, h * 0.4, 't', 0.7 if not big else 0.9)
    cy = h * 0.3
    lumps = [(-0.3, 0.04, 0.19), (0.3, 0.02, 0.19), (0.0, -0.07, 0.25), (-0.17, -0.12, 0.18), (0.19, -0.13, 0.17),
             (-0.4, 0.12, 0.11), (0.41, 0.1, 0.11), (0.0, 0.07, 0.2)]
    for dx, dy, rad in lumps:
        disc(cv, tx + dx * w, cy + dy * h + 0.7, rad * w, 'd')
    for dx, dy, rad in lumps:
        disc(cv, tx + dx * w - 0.4, cy + dy * h - 0.2, rad * w * 0.86, 'c')
    sx = tx + w * 0.2
    by = h * 0.43
    for x in (sx - w * 0.07, sx + w * 0.07):
        seg(cv, x, by, x, h * 0.72, 'k', 0.4)
    box(cv, sx - w * 0.1, h * 0.72, sx + w * 0.1, h * 0.72 + 0.9, 'k')
    if big:
        disc(cv, tx - 0.2, h * 0.62, 1.0, 'o')
        for x, y in scatter(cv, 'a', 'c', 7, r, sep=3, area=(1, h * 0.12, w - 2, h * 0.45)):
            cv.put(x, y + 1, 'a')
        for x in (w * 0.1, w * 0.86):
            oval(cv, x, gtop + 1.3, 1.0, 0.8, 'a')
    else:
        scatter(cv, 'a', 'c', 4, r, sep=3, area=(1, h * 0.12, w - 2, h * 0.42))
    cloud(cv, w * 0.84, h * 0.07, s * 0.05 + 0.5, 'e')
    disc(cv, w * 0.1, h * 0.07, s * 0.08, 'u')
    if r.random() < 0.5:
        mirror(cv)
    return cv, [sky(), role('c', 'crown', 'Crown', GREEN), role('d', 'shade', 'Leaf shade', GREEN), role('t', 'trunk', 'Trunk and boughs', BROWN),
                role('a', 'acorns', 'Acorns', BROWN), role('k', 'swing', 'Swing', PINK), role('o', 'hollow', 'Hollow', PINK),
                role('g', 'grass', 'Grass', GREEN), role('u', 'sun', 'Sun', BROWN), role('e', 'cloud', 'Cloud', BLUE)], ['trees', 'park']


def willow_tree(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.18)
    box(cv, 0, gtop, w, h, 'g')
    side = r.choice((-1, 1))
    tx = cx + side * w * 0.06
    px = cx - side * w * 0.22
    oval(cv, px, gtop + (h - gtop) * 0.55, w * 0.3, (h - gtop) * 0.36, 'w')
    path(cv, [(tx - side * 0.4, gtop + 0.6), (tx, h * 0.55), (tx + side * 0.6, h * 0.3)], 't', 1.0 if not big else 1.3)
    rx, dy = w * 0.44, h * 0.3
    for x in range(w):
        px_ = x + 0.5
        rel = (px_ - tx) / rx
        if abs(rel) > 1:
            continue
        top = dy - h * 0.2 * math.sqrt(1 - rel * rel)
        for y in range(int(top), int(dy) + 1):
            if y + 0.5 >= top:
                cv.g[y][x] = 'c'
        if x % 2 == (0 if not big else 1) or abs(rel) > 0.92:
            continue
        bottom = dy + h * (0.36 - 0.14 * rel * rel) + r.uniform(-1.2, 1.0)
        for y in range(int(dy), min(int(bottom), gtop - 1)):
            cv.g[y][x] = 'c'
    for k in range(2 if not big else 3):
        lx = px + (k - 0.5) * w * 0.16
        oval(cv, lx, gtop + (h - gtop) * 0.5, 0.9, 0.6, 'o')
    kx = px + side * w * 0.3
    for k in range(2 if not big else 3):
        x = kx + k * side * 1.2
        seg(cv, x, gtop + 0.5, x, gtop - h * 0.1, 'k', 0.4)
        oval(cv, x, gtop - h * 0.1, 0.6, 1.1, 'k')
    if big:
        oval(cv, px - side * w * 0.08, gtop + (h - gtop) * 0.48, 1.5, 0.9, 'y')
        disc(cv, px - side * w * 0.08 + side * 1.2, gtop + (h - gtop) * 0.3, 0.8, 'y')
    disc(cv, w * (0.88 if side > 0 else 0.12), h * 0.06, s * 0.07, 'u')
    return cv, [sky(), role('c', 'canopy', 'Weeping branches', GREEN), role('t', 'trunk', 'Trunk', BROWN), role('g', 'grass', 'Grass', GREEN),
                role('w', 'pond', 'Pond', BLUE), role('o', 'lilies', 'Water lilies', PINK), role('k', 'reeds', 'Cattails', BROWN),
                role('y', 'duck', 'Duck', BROWN), role('u', 'sun', 'Sun', PINK)], ['trees', 'pond']


def maple_tree(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.12)
    hills(cv, 'g', gtop, 0.4, w * 1.3, r.uniform(0, 6))
    tx = cx + r.uniform(-0.04, 0.04) * w
    ly, size = h * 0.34, w * 0.44
    box(cv, tx - max(0.9, w * 0.05), ly + size * 0.1, tx + max(0.9, w * 0.05), gtop + 0.6, 't')
    tips = maple_leaf(cv, tx, ly, size, 'c', r.uniform(-6, 6))
    base = (tx, ly + size * 0.15)
    for x, y in tips[:3] if not big else tips:
        seg(cv, base[0], base[1], x, y, 't', 0.45)
    oval(cv, tx + w * r.choice((-0.3, 0.3)), gtop + 0.6, w * 0.14, 1.2, 'p')
    n = 3 if not big else 6
    for k in range(n):
        x = w * (0.12 + 0.76 * ((k * 0.618 + r.random() * 0.2) % 1.0))
        y = h * (0.7 + 0.12 * (k % 2)) if k < 3 else h * (0.06 + 0.08 * (k % 2))
        if big:
            maple_leaf(cv, x, y, 1.6, 'f', r.uniform(-40, 40))
        else:
            poly(cv, [(x, y - 1.1), (x + 1.0, y), (x, y + 1.1), (x - 1.0, y)], 'f')
    cloud(cv, w * 0.18, h * 0.07, s * 0.05 + 0.4, 'e')
    if r.random() < 0.5:
        mirror(cv)
    return cv, [sky(), role('c', 'crown', 'Maple crown', PINK), role('t', 'trunk', 'Trunk and veins', BROWN),
                role('f', 'falling', 'Falling leaves', BROWN), role('p', 'pile', 'Leaf pile', PINK),
                role('g', 'grass', 'Grass', GREEN), role('e', 'cloud', 'Cloud', BLUE)], ['trees', 'autumn']


def bamboo(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.1)
    sx = w * r.choice((0.3, 0.7))
    disc(cv, sx, h * 0.3, s * 0.2, 'u')
    for k, (x0, top, x1) in enumerate(((-0.1, 0.5, 0.35), (0.25, 0.42, 0.75), (0.6, 0.52, 1.1))):
        poly(cv, [(w * x0, gtop), (w * (x0 + x1) / 2, h * top), (w * x1, gtop)], 'm')
    box(cv, 0, gtop, w, h, 'd')
    n = 3 if not big else 4
    lw = 0.85 if not big else 1.05
    for k in range(n):
        x = (k + 0.5) * w / n + r.uniform(-0.4, 0.4)
        lean = r.uniform(-0.6, 0.6)
        seg(cv, x, gtop + 0.5, x + lean, -1, 'b', lw)
        y = gtop - r.uniform(2.0, 3.5)
        nodes = []
        while y > 0.5:
            t = (gtop + 0.5 - y) / (gtop + 1.5)
            xx = x + lean * t
            box(cv, xx - lw - 0.2, y - 0.25, xx + lw + 0.2, y + 0.25, 'n', only='b')
            nodes.append((xx, y))
            y -= 3.6 if not big else 4.2
        for j, (xx, y) in enumerate(nodes[1:4]):
            d = 1 if (j + k) % 2 else -1
            lens(cv, xx + d * 0.6, y, xx + d * s * 0.24, y - h * 0.06, 1.2 if not big else 1.5, 'l')
            if big:
                lens(cv, xx + d * 0.6, y, xx + d * s * 0.18, y + h * 0.03, 1.2, 'l')
    if big:
        for x in (w * 0.15, w * 0.62):
            oval(cv, x, gtop + 0.4, 1.4, 0.9, 'k')
    return cv, [sky(), role('b', 'stalk', 'Bamboo canes', GREEN), role('n', 'node', 'Cane rings', BROWN), role('l', 'leaf', 'Leaves', GREEN),
                role('d', 'soil', 'Soil', BROWN), role('k', 'stone', 'Stones', BLUE), role('m', 'mountain', 'Mountains', BLUE),
                role('u', 'sun', 'Sun', PINK)], ['plants', 'forest']


def rose(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.12)
    for x in range(1 if not big else 0, w, 3):
        rbox(cv, x, h * 0.62, x + 1.9, gtop + 0.5, 0.9, 'k')
    box(cv, 0, h * 0.7, w, h * 0.7 + 0.9, 'k')
    hills(cv, 'g', gtop, 0.4, w * 1.2, r.uniform(0, 6))
    side = r.choice((-1, 1))
    rx, by, R = cx + side * w * 0.04, h * 0.3, s * 0.25
    path(cv, [(rx, by + R), (rx - side * 0.8, h * 0.6), (rx, gtop + 0.5)], 'l', 0.5 if not big else 0.6)
    lens(cv, rx - side * 0.5, h * 0.58, rx - side * w * 0.3, h * 0.5, s * 0.14, 'l')
    lens(cv, rx - side * 0.3, h * 0.68, rx + side * w * 0.28, h * 0.6, s * 0.13, 'l')
    for k, y in enumerate((0.5, 0.64, 0.76) if big else (0.52, 0.72)):
        d = 1 if k % 2 else -1
        poly(cv, [(rx - side * 0.4 + d * 0.4, h * y - 0.6), (rx - side * 0.4 + d * 1.5, h * y), (rx - side * 0.4 + d * 0.4, h * y + 0.6)], 'h')
    poly(cv, [(rx - R * 0.9, by + R * 0.5), (rx, by + R * 1.2), (rx + R * 0.9, by + R * 0.5)], 'l')
    oval(cv, rx, by + R * 0.1, R, R * 0.9, 'p')
    for dx in (-0.55, 0.0, 0.55):
        disc(cv, rx + dx * R, by - R * 0.55, R * 0.42, 'p')
    pts = []
    for k in range(24):
        t = k / 23
        a = math.radians(200 + t * 470)
        rr = R * (0.12 + 0.55 * t)
        pts.append((rx + math.cos(a) * rr, by - R * 0.2 + math.sin(a) * rr * 0.8))
    path(cv, pts, 'q', 0.42 if not big else 0.5)
    path(cv, [(rx - R * 0.95, by + R * 0.05), (rx - R * 0.3, by + R * 0.55), (rx + R * 0.3, by + R * 0.55), (rx + R * 0.95, by + R * 0.05)], 'q', 0.42)
    if big:
        bx = rx - side * w * 0.3
        path(cv, [(rx - side * 0.6, h * 0.5), (bx, h * 0.38), (bx, h * 0.3)], 'l', 0.5)
        oval(cv, bx, h * 0.27, 1.2, 1.7, 'p')
        poly(cv, [(bx - 1.3, h * 0.28), (bx, h * 0.33), (bx + 1.3, h * 0.28), (bx, h * 0.31)], 'l')
    disc(cv, w * (0.12 if side > 0 else 0.88), h * 0.08, s * 0.08, 'u')
    if big:
        cloud(cv, w * (0.82 if side > 0 else 0.18), h * 0.1, s * 0.05 + 0.4, 'e')
    return cv, [sky(), role('p', 'bloom', 'Rose', PINK), role('q', 'petal_line', 'Petal folds', PINK), role('l', 'stem', 'Stem and leaves', GREEN),
                role('h', 'thorns', 'Thorns', BROWN), role('k', 'fence', 'Fence', BROWN), role('g', 'grass', 'Grass', GREEN),
                role('u', 'sun', 'Sun', BROWN), role('e', 'cloud', 'Cloud', BLUE)], ['flowers', 'garden']


MORE_NATURE = [oak_tree, willow_tree, maple_tree, bamboo, rose]

# Expansion roles of these subjects (as expansions.ROLES): subject -> {group: [(roleId, new name or None), ...]}.
MORE_NATURE_ROLES = {
    'oak_tree': {'lime': [('sun', None)]},
    'maple_tree': {'lime': [('falling', 'Yellow leaves')], 'red': [('crown', 'Red crown')]},
    'bamboo': {'red': [('sun', 'Red sun')]},
    'rose': {'lime': [('sun', None)], 'red': [('bloom', 'Red rose')]},
}

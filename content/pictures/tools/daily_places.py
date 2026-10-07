"""Daily Challenge subjects: places (daily_subjects.py). Landmarks, buildings and scenes the levels never show, drawn at
22 x 28 with picture_kit's helpers in the style of world_subjects.py: a clear silhouette against the background, the
shapes that define a subject in different color groups, and roles in three or four groups with two roles in most of
them, so every picture carries six distinct variants. Each picture takes its composition, props, time of day or season
and side from its seed, so the three pictures of a subject differ.
"""
import math

from picture_kit import (BLUE, BROWN, GREEN, PINK, box, canvas, cloud, disc, dots, ground_rows, hills, lens, oval, path,
                         poly, rbox, ring, role, scatter, seg, sky, star, start)


# ---- Shared pieces ----

def _mirror(cv):
    """Flips the drawing left to right."""
    for row in cv.g:
        row.reverse()


def _moon(cv, x, y, rad, c, bg):
    """A crescent: a disc with a disc of the background over its upper right."""
    disc(cv, x, y, rad, c)
    disc(cv, x + rad * 0.55, y - rad * 0.3, rad * 0.78, bg)


def _light(night, c='u', group=BROWN):
    return role(c, 'moon', 'Moon', group) if night else role(c, 'sun', 'Sun', group)


def _curved_roof(cv, x, y0, y1, top, eave, c, lift=1.3):
    """A roof from its ridge (y0, `top` half wide) to its eaves (y1, `eave` half wide) whose ends sweep up."""
    for gy in range(cv.h):
        for gx in range(cv.w):
            px, py = gx + 0.5, gy + 0.5
            u = abs(px - x)
            if u > eave:
                continue
            t = max(0.0, (u - top) / (eave - top))
            up = lift * (u / eave) ** 4
            if y0 + (y1 - y0 - 1.0) * t ** 0.8 - up <= py <= y1 - up:
                cv.g[gy][gx] = c


def _pine(cv, x, base, height, half, c):
    """A pine of three tiers standing on `base`."""
    for k in range(3):
        yb = base - k * height * 0.27
        hw = half * (1 - k * 0.22)
        poly(cv, [(x - hw, yb), (x, yb - height * 0.46), (x + hw, yb)], c)


def _tree(cv, x, base, rad, crown, trunk):
    """A round tree: a trunk and a disc of leaves."""
    box(cv, x - 0.5, base - rad * 1.4, x + 0.5, base + 0.5, trunk)
    disc(cv, x, base - rad * 1.6, rad, crown)


def _palm(cv, x, base, height, trunk, leaves, lean=1):
    tx, ty = x + lean * height * 0.22, base - height
    path(cv, [(x, base), (x + lean * height * 0.06, base - height * 0.5), (tx, ty)], trunk, 0.55)
    for a in (-170, -130, -50, -10, 30, 150):
        t = math.radians(a)
        lens(cv, tx, ty, tx + math.cos(t) * height * 0.45, ty + math.sin(t) * height * 0.3 + height * 0.1,
             height * 0.15, leaves)


CAMEL = ["........XX.",
         "...XX..XXXX",
         "..XXXX.XX..",
         ".XXXXXXXX..",
         "XXXXXXXXX..",
         ".X.X..X.X..",
         ".X.X..X.X..",
         ".X.X..X.X.."]

HORSE = [".....XX.",
         "....XXXX",
         "...XXX.X",
         "X.SSSX..",
         "XXXXXX..",
         ".XXXXX..",
         ".X.X..X.",
         ".X.X...X"]


def _sprite(cv, x0, y0, rows, marks, flip=False):
    """A small pixel figure (a camel, a horse) from rows of template characters, `marks` mapping each to a role."""
    for j, row in enumerate(rows):
        for i, ch in enumerate(row[::-1] if flip else row):
            if ch in marks:
                cv.put(int(x0) + i, int(y0) + j, marks[ch])


def _pagoda_roof(cv, x, y, half, c):
    """A pagoda roof three rows deep from row y over a wall `half` wide, the ends of its eaves turned up."""
    for k, hw in enumerate((half + 0.6, half + 2.2, half + 4.2)):
        box(cv, x - hw, y + k + 0.1, x + hw, y + k + 0.9, c)
    for d in (-1, 1):
        box(cv, x + d * (half + 3.8) - 0.5, y + 1.1, x + d * (half + 3.8) + 0.5, y + 1.9, c)


def _windows(cv, y0, y1, x, wall, c, step=2):
    """Windows every `step` cells along the rows of `wall` from y0 to y1 (every other row), in the run of wall cells
    through column x, leaving its edges solid."""
    for yy in range(int(y0), int(y1) + 1, 2):
        if not 0 <= yy < cv.h or cv.g[yy][int(x)] != wall:
            continue
        a = b = int(x)
        while a > 0 and cv.g[yy][a - 1] == wall:
            a -= 1
        while b < cv.w - 1 and cv.g[yy][b + 1] == wall:
            b += 1
        for xx in range(a + 1, b, step):
            if xx < b:
                cv.g[yy][xx] = c


def _outline(cv, inside, c):
    """Turns every cell of the roles in `inside` that touches another role (or the edge) into c: a frame round them."""
    marks = []
    for gy in range(cv.h):
        for gx in range(cv.w):
            if cv.g[gy][gx] in inside and any(not (0 <= gx + dx < cv.w and 0 <= gy + dy < cv.h)
                                               or cv.g[gy + dy][gx + dx] not in inside + c
                                               for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                marks.append((gx, gy))
    for gx, gy in marks:
        cv.g[gy][gx] = c


# ---- Landmarks ----

def pagoda(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.11)
    night = r.random() < 0.35
    tree = r.choice(('cherry', 'cherry', 'pine', 'none'))
    px = int(cx) + (1 if tree != 'none' else 0)
    back = r.choice(('mountain', 'hills', 'none')) if tree != 'pine' else 'none'
    if back == 'mountain':
        mx = w * r.uniform(0.3, 0.7)
        poly(cv, [(mx - w * 0.7, gtop), (mx - w * 0.13, h * 0.32), (mx + w * 0.13, h * 0.32), (mx + w * 0.7, gtop)], 'v')
    elif back == 'hills':
        hills(cv, 'v', h * 0.66, 1.6, w * 0.9, r.uniform(0, 6))
    box(cv, 0, gtop, w, h, 'g')
    tiers = r.choice((3, 4, 4))
    halves, wh = ((5, 3.5, 2), 3) if tiers == 3 else ((5, 4, 3, 2), 2)
    y = int(gtop) - 1
    box(cv, px - 6.6, y, px + 6.6, y + 0.9, 'k')
    for k, half in enumerate(halves):
        box(cv, px - half, y - wh, px + half, y, 'h')
        for o in (0.5, 1.5, 2.5, 3.5):
            if k == 0 and o == 0.5:
                box(cv, px - 1, y - wh, px + 1, y, 'i')
            elif (o - 1.5) % 2 == 0 and o < half - 0.6 or (half <= 2 and o == 0.5):
                for side in (-1, 1):
                    box(cv, px + side * o - 0.4, y - wh + (1 if k == 0 else 0), px + side * o + 0.4, y, 'i')
        y -= wh + 3
        _pagoda_roof(cv, px, y, half, 'r')
    seg(cv, px, y + 0.5, px, max(1.0, y - 3.6), 'k', 0.5)
    for k in range(2):
        box(cv, px - 1.6, y - 1.4 - k * 1.6, px + 1.6, y - 0.6 - k * 1.6, 'k')
    if tree == 'cherry':
        tx = w * 0.09
        seg(cv, tx, gtop + 0.5, tx + 0.3, gtop - h * 0.2, 't', 0.55)
        for dx, dy, rr in ((0, -0.31, 2.2), (-1.3, -0.24, 1.6), (1.3, -0.24, 1.6)):
            disc(cv, tx + dx, gtop + h * dy, rr, 'b')
    elif tree == 'pine':
        _pine(cv, w * 0.1, gtop + 0.5, h * 0.42, w * 0.12, 'p')
    sx = w * 0.88 if tree != 'none' else w * r.choice((0.12, 0.88))
    if night:
        _moon(cv, sx, h * 0.1, s * 0.11, 'u', 's')
        scatter(cv, 'x', 's', 7, r, sep=3, area=(0, 0, w - 1, h * 0.55))
    else:
        disc(cv, sx, h * 0.1, s * 0.09, 'u')
    if r.random() < 0.5:
        _mirror(cv)
    return cv, [sky(), role('r', 'roof', 'Roofs', PINK), role('h', 'wall', 'Walls', BROWN),
                role('i', 'window', 'Windows and door', BLUE), role('k', 'spire', 'Spire and steps', BROWN), _light(night),
                role('g', 'grass', 'Grass', GREEN), role('v', 'mountain', 'Mountain and hills', GREEN), role('t', 'trunk', 'Trunk', BROWN),
                role('b', 'blossom', 'Blossom', PINK), role('p', 'pine', 'Pine', GREEN),
                role('x', 'stars', 'Stars', BROWN)], ['places', 'travel']


def pyramid(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    time = r.choice(('day', 'dusk', 'night'))
    hz = h * 0.72
    if time == 'dusk':
        disc(cv, w * r.uniform(0.25, 0.75), hz - 1.5, s * 0.21, 'u')
    elif time == 'day':
        disc(cv, w * 0.84, h * 0.1, s * 0.1, 'u')
    else:
        _moon(cv, w * 0.82, h * 0.11, s * 0.12, 'u', 's')
    n = r.choice((1, 2, 3))
    spec = {1: [(cx + r.uniform(-1.5, 1.5), w * 0.48, h * 0.52)],
            2: [(w * 0.72, w * 0.3, h * 0.33), (w * 0.38, w * 0.42, h * 0.48)],
            3: [(w * 0.84, w * 0.2, h * 0.22), (w * 0.62, w * 0.28, h * 0.33), (w * 0.3, w * 0.38, h * 0.46)]}[n]
    for x, hw, ph in spec:
        top = hz - ph
        poly(cv, [(x - hw, hz + 0.5), (x, top), (x + hw, hz + 0.5)], 'p')
        poly(cv, [(x + hw * 0.2, hz + 0.5), (x, top), (x + hw, hz + 0.5)], 'q')
        for y in range(int(top) + 3, int(hz), 3):
            box(cv, 0, y, w, y + 0.6, 'q', only='p')
    x, hw, ph = spec[-1]
    rbox(cv, x - hw * 0.1 - 1.5, hz - 2.6, x - hw * 0.1 + 1.5, hz + 0.5, 1.2, 'd')
    hills(cv, 'e', hz + 0.6, 0.5, w * 1.3, r.uniform(0, 6))
    hills(cv, 'a', hz + 3.2, 0.9, w * 0.9, r.uniform(0, 6))
    prop = r.choice(('camel', 'oasis', 'none'))
    if prop == 'camel':
        _sprite(cv, w * r.uniform(0.1, 0.5), hz + 0.6, CAMEL, {'X': 'c'}, r.random() < 0.5)
    elif prop == 'oasis':
        ox = w * r.choice((0.16, 0.84))
        oval(cv, ox, h - 2.0, w * 0.17, 1.3, 'o')
        _palm(cv, ox - 1.5, h - 2.4, h * 0.3, 't', 'f', 1 if ox < cx else -1)
    if time == 'night':
        scatter(cv, 'x', 's', 8, r, sep=3, area=(0, 0, w - 1, h * 0.45))
    if r.random() < 0.5:
        _mirror(cv)
    return cv, [sky(), role('p', 'face', 'Sunny face', BROWN), role('q', 'shade', 'Shaded face and steps', BROWN),
                role('d', 'door', 'Entrance and pool', BLUE), role('o', 'pool', 'Oasis pool', BLUE),
                role('a', 'dune', 'Near dunes', GREEN), role('e', 'far_dune', 'Far dunes', GREEN),
                role('c', 'camel', 'Camel', BROWN), role('t', 'trunk', 'Palm trunk', BROWN), role('f', 'palm', 'Palm leaves', GREEN),
                _light(time == 'night', 'u', PINK), role('x', 'stars', 'Stars', BROWN)], ['places', 'desert']


def volcano(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    mode = r.choice(('erupt', 'smoke', 'night'))
    gtop = h - ground_rows(h, 0.14)
    vx = cx + r.uniform(-1.5, 1.5)
    yt, ch, bh = h * r.uniform(0.36, 0.42), w * 0.13, w * 0.66
    sx = w * r.choice((0.13, 0.87))
    if mode == 'night':
        _moon(cv, sx, h * 0.1, s * 0.11, 'u', 's')
    else:
        disc(cv, sx, h * 0.1, s * 0.09, 'u')
    left, right = [], []
    for k in range(9):
        t = k / 8
        d = ch + (bh - ch) * t ** 1.6
        left.append((vx - d, yt + (gtop + 1.5 - yt) * t))
        right.append((vx + d, yt + (gtop + 1.5 - yt) * t))
    poly(cv, left + right[::-1], 'm')

    def slope(f, t):
        return vx + f * (ch + (bh - ch) * t ** 1.6), yt + (gtop + 1.5 - yt) * t

    for f in (-0.62, 0.05, 0.58):
        path(cv, [slope(f, t) for t in (0.2, 0.55, 0.95)], 'n', 0.45)
    oval(cv, vx, yt, ch * 0.95, 0.9, 'l')
    ph = r.uniform(0, 6)
    for f in r.sample((-0.75, -0.35, 0.3, 0.7), 2 if mode == 'smoke' else 3):
        end = r.uniform(0.55, 0.95)
        pts = [(vx + f * ch * 0.8, yt)]
        pts += [(slope(f, t)[0] + 0.6 * math.sin(t * 9 + ph), slope(f, t)[1]) for t in (0.15, 0.3, 0.45, 0.6, 0.75, 0.9) if t <= end]
        path(cv, pts, 'l', 0.6)
    if r.random() < 0.5:
        hills(cv, 'w', gtop - 0.4, 0.35, w * 0.5, r.uniform(0, 6))
        oval(cv, vx, gtop + 0.2, bh * 0.85, 1.3, 'g')
    else:
        box(cv, 0, gtop, w, h, 'g')
        for x, lean in ((w * 0.1, 1), (w * 0.9, -1)):
            if r.random() < 0.7:
                _palm(cv, x, gtop + 0.5, h * r.uniform(0.24, 0.3), 't', 'p', lean)
    if mode == 'smoke':
        d = r.choice((-1, 1))
        for k in range(5):
            disc(cv, vx + d * k * 1.3, yt - 1.6 - k * 1.9, 1.1 + k * 0.38, 'c')
    else:
        by = yt - h * 0.1
        if mode == 'erupt':
            d = r.choice((-1, 1))
            for k in range(4):
                disc(cv, vx + d * (2.0 + k * 1.6), by - 3.6 - k * 1.5, 1.3 + k * 0.4, 'c')
        star(cv, vx, by, w * 0.23, 'f', ri=w * 0.1, points=9, rot=r.uniform(-90, -70))
        lens(cv, vx, yt + 0.5, vx, by, 2.6, 'f')
        for a in r.sample((-150, -125, -55, -30), 3):
            t = math.radians(a)
            disc(cv, vx + math.cos(t) * w * 0.36, by - math.sin(t) * -h * 0.2, 0.9, 'f')
        if mode == 'night':
            scatter(cv, 'x', 's', 8, r, sep=3, area=(0, 0, w - 1, h * 0.5))
    if r.random() < 0.5:
        _mirror(cv)
    return cv, [sky(), role('m', 'mountain', 'Volcano', BROWN), role('n', 'ridge', 'Ridges', BROWN),
                role('l', 'lava', 'Lava', PINK), role('f', 'fire', 'Eruption', PINK), role('c', 'smoke', 'Smoke', GREEN),
                role('g', 'ground', 'Jungle and beach', GREEN), role('p', 'palm', 'Palm leaves', GREEN),
                role('t', 'trunk', 'Palm trunks', BROWN), role('w', 'sea', 'Sea', BLUE),
                _light(mode == 'night', 'u', BROWN), role('x', 'stars', 'Stars', BROWN)], ['places', 'nature']


def waterfall(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    kind = r.choice(('tall', 'steps', 'twin'))
    top, pool = h * r.uniform(0.42, 0.46), h * 0.76
    if kind == 'tall':
        falls = [(cx + r.uniform(-1.5, 1.5), top - 1.2, pool, w * 0.13)]
    elif kind == 'twin':
        falls = [(w * 0.3, top - 1.0, pool, w * 0.08), (w * 0.7, top - 0.6, pool, w * 0.08)]
    else:
        mid = top + (pool - top) * 0.45
        falls = [(w * 0.36, top - 1.2, mid, w * 0.1), (w * 0.6, mid, pool, w * 0.12)]
    for x, y0, y1, hw in falls:
        if y0 < top:
            oval(cv, x, top - 0.4, hw + 2.6, 2.2, 'v')
    for x in (w * r.uniform(0.06, 0.14), w * r.uniform(0.86, 0.94)):
        _pine(cv, x, top + 0.2, h * r.uniform(0.24, 0.3), w * 0.11, 'p')
    hills(cv, 'k', top, 0.5, w * 0.4, r.uniform(0, 6))
    for k in range(3):
        y = top + 2.2 + k * 2.6 + r.uniform(-0.4, 0.4)
        x0 = r.uniform(0, w * 0.4)
        seg(cv, x0, y, x0 + w * r.uniform(0.25, 0.5), y + r.uniform(-0.6, 0.6), 'd', 0.45)
    if kind == 'steps':
        oval(cv, w * 0.5, mid, w * 0.22, 1.0, 'w')
    for x, y0, y1, hw in falls:
        box(cv, x - hw, y0, x + hw, y1, 'w')
        for k in range(max(1, int(hw))):
            fx = x - hw + 1 + k * 2
            a = r.uniform(y0 + 1, (y0 + y1) / 2)
            box(cv, fx - 0.5, a, fx + 0.5, a + (y1 - y0) * r.uniform(0.3, 0.5), 'f')
    box(cv, 0, pool, w, h, 'w')
    for x, y0, y1, hw in falls:
        if y1 >= pool:
            for d in (-1, 1):
                oval(cv, x + d * (hw + 0.5), pool + 0.4, 1.4, 0.9, 'f')
            box(cv, x - hw * 0.5, pool - 1, x + hw * 0.5, h, 'w')
    oval(cv, 0, h + 0.5, w * 0.38, h * 0.18, 'g')
    oval(cv, w, h + 0.5, w * 0.34, h * 0.16, 'g')
    spots = [x for x in (w * 0.24, w * 0.4, w * 0.6, w * 0.76) if all(abs(x - f[0]) > f[3] + 2.5 for f in falls)]
    for x in r.sample(spots, min(2, len(spots))):
        oval(cv, x, h * 0.86, 1.6, 1.0, 'o')
    scatter(cv, 'b', 'g', 4, r, sep=2)
    night = r.random() < 0.3
    if night:
        _moon(cv, w * r.choice((0.3, 0.7)), h * 0.1, s * 0.1, 'u', 's')
        scatter(cv, 'x', 's', 7, r, sep=3, area=(0, 0, w - 1, top - 3))
    else:
        disc(cv, w * r.choice((0.3, 0.7)), h * 0.1, s * 0.08, 'u')
    if r.random() < 0.5:
        _mirror(cv)
    return cv, [sky(), role('w', 'water', 'Water', BLUE), role('f', 'foam', 'Foam', BLUE), role('k', 'cliff', 'Cliffs', BROWN),
                role('d', 'ledge', 'Ledges', BROWN), _light(night), role('o', 'rock', 'Rocks', BROWN),
                role('v', 'hills', 'Forest hills', GREEN), role('p', 'pine', 'Pines', GREEN), role('g', 'bank', 'Banks', GREEN),
                role('b', 'flowers', 'Flowers', PINK), role('x', 'stars', 'Stars', BROWN)], ['places', 'nature']


def stone_bridge(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    kind = r.choice(('hump', 'three', 'two'))
    wl = h * 0.7
    hills(cv, 'v', wl - 0.6, 0.4, w * 0.8, r.uniform(0, 6))
    box(cv, 0, wl, w, h, 'w')
    if kind == 'hump':
        arches = [(cx, w * 0.31, h * 0.27)]
        hump = 3.2
    elif kind == 'two':
        arches = [(w * 0.29, w * 0.18, h * 0.18), (w * 0.71, w * 0.18, h * 0.18)]
        hump = 0.0
    else:
        arches = [(w * 0.15, w * 0.11, h * 0.13), (cx, w * 0.17, h * 0.18), (w * 0.85, w * 0.11, h * 0.13)]
        hump = 0.8
    crown = wl + 0.6 - max(ry for _, _, ry in arches)

    def deck(px):
        return crown - 2.1 + hump * ((px - cx) / (w * 0.5)) ** 2

    def inside(px, py, grow=0.0):
        return any(((px - ax) / (rx + grow)) ** 2 + ((py - wl - 0.6) / (ry + grow)) ** 2 <= 1.0 for ax, rx, ry in arches)

    for gy in range(h):
        for gx in range(w):
            px, py = gx + 0.5, gy + 0.5
            d = deck(px)
            if d <= py <= wl + 0.5 and not inside(px, py):
                cv.g[gy][gx] = 'a' if inside(px, py, 0.9) else 'b'
            elif d - 1.2 <= py < d:
                cv.g[gy][gx] = 'p'
    for y in (wl + 2.5, wl + 5.0):
        for k in range(3):
            x = w * r.uniform(0.1, 0.8)
            box(cv, x, y, x + r.uniform(1.5, 3.0), y + 0.6, 'q')
    for side in (0, 1):
        x0, sgn = (0, 1) if side == 0 else (w, -1)
        d = deck(w * 0.04 if side == 0 else w * 0.96)
        poly(cv, [(x0, d - 0.6), (x0 + sgn * w * 0.06, d), (x0 + sgn * w * 0.28, h), (x0, h)], 'g')
    for x in r.sample([w * 0.04, w * 0.96], 1):
        _tree(cv, x, deck(x) - 0.4, s * 0.12, 't', 'k')
    if r.random() < 0.55:
        for x in (w * 0.3, w * 0.7):
            y = deck(x) - 1.2
            seg(cv, x, y, x, y - 2.4, 'p', 0.5)
            disc(cv, x, y - 3.0, 0.9, 'l')
    elif r.random() < 0.7:
        bx = w * r.uniform(0.4, 0.6)
        poly(cv, [(bx - 2.6, h * 0.86), (bx + 2.6, h * 0.86), (bx + 1.8, h * 0.93), (bx - 1.8, h * 0.93)], 'l')
        seg(cv, bx, h * 0.86, bx, h * 0.76, 'l', 0.5)
    night = r.random() < 0.3
    if night:
        _moon(cv, w * 0.84, h * 0.1, s * 0.1, 'u', 's')
        scatter(cv, 'x', 's', 6, r, sep=3, area=(0, 0, w - 1, h * 0.3))
    else:
        disc(cv, w * r.choice((0.14, 0.86)), h * 0.08, s * 0.08, 'u')
    if r.random() < 0.5:
        _mirror(cv)
    return cv, [sky(), role('b', 'bridge', 'Bridge', BROWN), role('p', 'parapet', 'Parapet and posts', BROWN), _light(night),
                role('a', 'arch', 'Arch stones', PINK), role('w', 'river', 'River', BLUE), role('q', 'ripples', 'Ripples', BLUE),
                role('v', 'far_bank', 'Far bank', GREEN), role('t', 'tree', 'Tree', GREEN), role('g', 'bank', 'Near banks', GREEN),
                role('k', 'trunk', 'Trunk', BROWN), role('l', 'lamps', 'Lamps and boat', PINK),
                role('x', 'stars', 'Stars', BROWN)], ['places', 'river']


def clock_tower(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.1)
    night = r.random() < 0.35
    top = r.choice(('spire', 'dome', 'battlement'))
    sw, cw = w * 0.15, w * 0.22
    cy, cr = h * 0.47, w * 0.155
    st0, st1 = cy - cr - 1.6, cy + cr + 1.6
    if r.random() < 0.5:
        for x0, x1 in ((-0.5, w * 0.23), (w * 0.77, w + 0.5)):
            hy = gtop - h * r.uniform(0.16, 0.24)
            box(cv, x0, hy, x1, gtop + 0.5, 'e')
            poly(cv, [(x0 - 0.8, hy + 0.5), ((x0 + x1) / 2, hy - h * 0.1), (x1 + 0.8, hy + 0.5)], 'q')
            box(cv, (x0 + x1) / 2 - 1.0, hy + 2.0, (x0 + x1) / 2 + 1.0, hy + 3.8, 'i')
    else:
        for x in (w * 0.1, w * 0.9):
            _tree(cv, x, gtop, s * r.uniform(0.12, 0.15), 't', 'q')
    box(cv, 0, gtop, w, h, 'g')
    box(cv, cx - sw, st1, cx + sw, gtop + 0.5, 'h')
    box(cv, cx - cw, st0, cx + cw, st1, 'h')
    box(cv, cx - cw - 0.6, st1 - 0.4, cx + cw + 0.6, st1 + 0.6, 'c')
    disc(cv, cx, cy, cr + 0.9, 'c')
    disc(cv, cx, cy, cr, 'f')
    for a in (0, 90, 180, 270):
        t = math.radians(a)
        cv.put(int(cx + math.cos(t) * (cr - 0.7)), int(cy + math.sin(t) * (cr - 0.7)), 'k')
    for a, L in ((r.randrange(12) * 30 - 90, cr * 0.5), (r.choice((0, 90, 180, 270)) - 90, cr * 0.82)):
        t = math.radians(a)
        seg(cv, cx, cy, cx + math.cos(t) * L, cy + math.sin(t) * L, 'k', 0.45)
    for y in (st1 + 1.5, st1 + 4.5):
        box(cv, cx - 0.5, y, cx + 0.5, y + 1.8, 'i')
    rbox(cv, cx - 1.4, gtop - 3.2, cx + 1.4, gtop + 0.5, 1.0, 'd')
    if top == 'spire':
        poly(cv, [(cx - cw - 0.6, st0 + 0.5), (cx, h * 0.015), (cx + cw + 0.6, st0 + 0.5)], 'r')
        box(cv, cx - 0.5, st0 - 3.6, cx + 0.5, st0 - 2.0, 'i')
    elif top == 'dome':
        box(cv, cx - sw, st0 - 3.8, cx + sw, st0, 'h')
        for x in (cx - 1.4, cx + 1.4):
            rbox(cv, x - 0.6, st0 - 3.0, x + 0.6, st0 - 0.4, 0.5, 'i')
        for gy in range(h):
            for gx in range(w):
                px, py = gx + 0.5, gy + 0.5
                if py <= st0 - 3.6 and ((px - cx) / (sw + 0.8)) ** 2 + ((py - st0 + 3.6) / (h * 0.13)) ** 2 <= 1.0:
                    cv.g[gy][gx] = 'r'
        seg(cv, cx, st0 - 3.6 - h * 0.13, cx, h * 0.02, 'k', 0.5)
    else:
        for x in range(int(cx - cw), int(cx + cw) + 1, 2):
            box(cv, x, st0 - 1.0, x + 0.9, st0, 'h')
        seg(cv, cx, st0 - 0.8, cx, h * 0.04, 'k', 0.5)
        poly(cv, [(cx + 0.5, h * 0.04), (cx + 4.2, h * 0.08), (cx + 0.5, h * 0.13)], 'r')
    sx = w * r.choice((0.14, 0.86))
    if night:
        _moon(cv, sx, h * 0.1, s * 0.1, 'u', 's')
        scatter(cv, 'x', 's', 7, r, sep=3, area=(0, 0, w - 1, h * 0.5))
    else:
        disc(cv, sx, h * 0.09, s * 0.08, 'u')
    if r.random() < 0.5:
        _mirror(cv)
    return cv, [sky(), role('h', 'tower', 'Tower', BROWN), role('c', 'rim', 'Clock rim and ledge', PINK),
                role('f', 'face', 'Clock face', BLUE), role('k', 'hands', 'Hands and pole', BROWN), role('r', 'roof', 'Roof', PINK),
                role('i', 'window', 'Windows', BLUE), role('d', 'door', 'Door', PINK), role('e', 'house', 'Houses', PINK),
                role('q', 'house_roof', 'House roofs and trunks', BROWN), role('t', 'trees', 'Trees', GREEN),
                role('g', 'grass', 'Grass', GREEN), _light(night), role('x', 'stars', 'Stars', BROWN)], ['places', 'town']


def skyscraper(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.1)
    shape = r.choice(('setback', 'slab', 'crown'))
    night = r.random() < 0.4
    tx = cx + 0.5 + r.choice((-2, 0, 2))
    sx = w * (0.14 if tx > cx + 1 else 0.86 if tx < cx else r.choice((0.14, 0.86)))
    if night:
        _moon(cv, sx, h * 0.1, s * 0.1, 'u', 's')
    else:
        disc(cv, sx, h * 0.09, s * 0.08, 'u')
        cloud(cv, w - sx, h * 0.2, 1.1, 'c')

    lt, rt = h * r.uniform(0.4, 0.56), h * r.uniform(0.48, 0.64)
    box(cv, -0.5, lt, tx - 5.2, gtop + 0.5, 'n')
    _windows(cv, lt + 1, gtop - 2, (tx - 5.2) / 2, 'n', 'i')
    box(cv, tx + 5.2, rt, w + 0.5, gtop + 0.5, 'e')
    _windows(cv, rt + 1, gtop - 2, (tx + 5.2 + w) / 2, 'e', 'i')
    if shape == 'setback':
        parts = [(4.5, h * 0.44), (3.5, h * 0.28), (2.5, h * 0.17)]
    elif shape == 'slab':
        parts = [(4.5, h * 0.14)]
    else:
        parts = [(4.5, h * 0.27)]
    for half, y0 in parts:
        box(cv, tx - half, y0, tx + half, gtop + 0.5, 'b')
    _windows(cv, parts[-1][1] + 1, gtop - 2, tx, 'b', 'i')
    box(cv, tx - 1.5, gtop - 2.2, tx + 1.5, gtop + 0.5, 'k')
    if shape == 'setback':
        seg(cv, tx, h * 0.17, tx, h * 0.015, 'k', 0.5)
    elif shape == 'slab':
        for x in (tx - 2, tx + 2):
            seg(cv, x, h * 0.14, x, h * 0.04, 'k', 0.5)
    else:
        poly(cv, [(tx - 4.5, h * 0.27 + 0.5), (tx, h * 0.12), (tx + 4.5, h * 0.27 + 0.5)], 'k')
        seg(cv, tx, h * 0.13, tx, h * 0.015, 'k', 0.5)
    box(cv, 0, gtop, w, h, 'd')
    for x in range(1, w, 4):
        box(cv, x, gtop + 1.6, x + 1.9, gtop + 2.2, 'y')
    if night:
        scatter(cv, 'x', 's', 7, r, sep=3, area=(0, 0, w - 1, h * 0.5))
    if r.random() < 0.5:
        _mirror(cv)
    return cv, [sky(), role('b', 'tower', 'Tower', BROWN), role('k', 'top', 'Spire, crown and lobby', BROWN),
                role('i', 'window', 'Windows', BLUE), role('n', 'left', 'Left building', PINK), role('e', 'right', 'Right building', PINK),
                role('d', 'street', 'Street', GREEN), role('y', 'lines', 'Road lines', BROWN), role('c', 'cloud', 'Cloud', GREEN),
                _light(night, 'u', PINK), role('x', 'stars', 'Stars', BROWN)], ['places', 'city']


def carousel(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.1)
    layout = r.choice(('wide', 'left', 'right'))
    night = r.choice((False, True, False))
    wide = layout == 'wide'
    W = w * (0.45 if wide else 0.37)
    ox = cx + {'wide': 0, 'left': -1, 'right': 1}[layout] * w * 0.06
    box(cv, 0, gtop, w, h, 'g')
    if r.random() < 0.5:
        dots(cv, 'q', 'g', 3, 2, area=(0, gtop + 1, w - 1, h - 1))
    py0 = gtop - 2.6
    rbox(cv, ox - W - 0.4, py0, ox + W + 0.4, gtop + 0.4, 0.8, 'p')
    for x in range(int(ox - W) + 1, int(ox + W), 3):
        cv.put(x, int(py0 + 1.2), 'd')
    if not wide:
        bx = w * 0.14 if ox > cx else w * 0.86
        if r.random() < 0.5:
            box(cv, bx - 2.0, gtop - 6.0, bx + 2.0, gtop + 0.5, 'k')
            box(cv, bx - 1.2, gtop - 5.0, bx + 1.2, gtop - 3.2, 'w')
            for k in range(4):
                poly(cv, [(bx - 2.6 + k * 1.3, gtop - 6.0), (bx - 2.0 + k * 1.3, gtop - 8.4), (bx - 1.3 + k * 1.3, gtop - 6.0)],
                     'ab'[k % 2])
        else:
            _tree(cv, bx, gtop, s * 0.1, 'f', 'k')
    apex, eave = h * 0.13, h * 0.4
    box(cv, ox - 1.0, eave, ox + 1.0, py0, 'c')
    tops = [int(eave) + 1, int(eave) + 3]
    r.shuffle(tops)
    for x, y0 in zip((int(ox - W * 0.56) + 0.5, int(ox + W * 0.44) + 0.5), tops):
        seg(cv, x, eave, x, py0, 'k', 0.5)
        _sprite(cv, x - 3.5, y0 + 1, HORSE, {'X': 'x', 'S': 'e'})
    n = r.choice((6, 8))
    bell = r.choice((0.5, 0.75, 1.0))
    for gy in range(h):
        for gx in range(w):
            px, py = gx + 0.5, gy + 0.5
            if apex <= py <= eave:
                hw = W * ((py - apex) / (eave - apex)) ** bell
                if abs(px - ox) <= hw:
                    k = int(((px - ox) / max(hw, 0.01) + 1) / 2 * n)
                    cv.g[gy][gx] = 'a' if k % 2 == 0 else 'b'
    for k in range(n):
        x = ox - W + (k + 0.5) * 2 * W / n
        disc(cv, x, eave, W / n * 0.95, 'a' if k % 2 == 0 else 'b')
        cv.put(int(x), int(eave) - 1, 'd')
    seg(cv, ox, apex + 0.5, ox, 1.2, 'k', 0.5)
    poly(cv, [(ox + 0.4, 0.8), (ox + 4.2, 2.0), (ox + 0.4, 3.2)], 'f')
    sx = w * (0.12 if ox >= cx else 0.88)
    if night:
        _moon(cv, sx, h * 0.08, s * 0.09, 'u', 's')
        scatter(cv, 'z', 's', 6, r, sep=3, area=(0, 0, w - 1, h * 0.35))
    else:
        disc(cv, sx, h * 0.07, s * 0.07, 'u')
    if r.random() < 0.5:
        _mirror(cv)
    return cv, [sky(), role('a', 'stripe', 'Canopy stripes', PINK), role('b', 'stripe2', 'Other stripes', BROWN),
                role('c', 'column', 'Centre column', PINK), role('k', 'pole', 'Poles and booth', BROWN), _light(night),
                role('x', 'horse', 'Horses', GREEN), role('e', 'saddle', 'Saddles', PINK), role('p', 'platform', 'Platform', BROWN),
                role('d', 'lights', 'Lights', BLUE), role('w', 'window', 'Booth window', BLUE), role('f', 'flag', 'Flag and tree', GREEN), role('g', 'grass', 'Grass', GREEN),
                role('q', 'flowers', 'Lawn dots', BLUE), role('z', 'stars', 'Stars', BROWN)], ['places', 'fair']


# ---- Garden, fair and countryside ----

def circus_tent(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.11)
    night = r.choice((False, True, False))
    peaks = r.choice(([cx], [w * 0.3, w * 0.7]))
    x0, x1 = w * 0.08, w * 0.92
    eave = h * r.uniform(0.48, 0.54)
    apex = h * (0.13 if len(peaks) == 1 else 0.21)
    W = (x1 - x0) / 2 / len(peaks) + (0.9 if len(peaks) == 2 else 0.6)
    n = 6 if len(peaks) == 1 else 4
    box(cv, 0, gtop, w, h, 'g')
    for gy in range(h):
        for gx in range(w):
            px, py = gx + 0.5, gy + 0.5
            if eave <= py <= gtop + 0.5 and x0 <= px <= x1:
                cv.g[gy][gx] = 'ab'[int((px - cx) / 2 + 100) % 2]
            elif apex <= py < eave:
                for ax in peaks:
                    hw = W * ((py - apex) / (eave - apex)) ** 1.5
                    if abs(px - ax) <= hw:
                        cv.g[gy][gx] = 'ab'[int(((px - ax) / max(hw, 0.01) + 1) / 2 * n) % 2]
                        break
    box(cv, x0 - 0.9, eave - 0.6, x1 + 0.9, eave + 0.4, 'e')
    x = x0 - 0.4
    while x < x1:
        disc(cv, x + 1.0, eave + 0.6, 1.0, 'e')
        x += 2.0
    dw = w * 0.11
    rbox(cv, cx - dw, eave + 3.2, cx + dw, gtop + 0.5, dw, 'd')
    for d in (-1, 1):
        poly(cv, [(cx + d * (dw + 0.6), eave + 2.4), (cx + d * 0.2, eave + 2.4), (cx + d * (dw + 0.6), gtop - 1.5)], 'e')
    for ax in peaks:
        px = int(ax) + 0.5
        seg(cv, px, apex + 0.5, px, 1.2, 'k', 0.5)
        poly(cv, [(px + 0.4, 0.8), (px + 4.0, 1.9), (px + 0.4, 3.0)], 'f')
    sx = w * r.choice((0.12, 0.88)) if len(peaks) == 1 else cx
    if night:
        _moon(cv, sx, h * 0.12, s * 0.1, 'u', 's')
        scatter(cv, 'x', 's', 7, r, sep=3, area=(0, 0, w - 1, eave - 2))
    else:
        disc(cv, sx, h * 0.12, s * 0.08, 'u')
    if r.random() < 0.5:
        _mirror(cv)
    return cv, [sky(), role('a', 'stripe', 'Red stripes', PINK), role('b', 'stripe2', 'Yellow stripes', BROWN),
                role('e', 'trim', 'Valance and drapes', GREEN), role('d', 'door', 'Entrance', BLUE), role('k', 'pole', 'Poles', BROWN),
                role('f', 'flag', 'Flags', PINK), role('g', 'grass', 'Grass', GREEN), _light(night),
                role('x', 'stars', 'Stars', BROWN)], ['places', 'fair']


def greenhouse(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.12)
    plants = r.choice(('pots', 'vines', 'mixed'))
    disc(cv, w * r.choice((0.12, 0.88)), h * 0.09, s * 0.08, 'u')
    shape = r.choice(('gable', 'arch', 'wings'))
    for x in (w * 0.04, w * 0.96):
        if r.random() < 0.7:
            oval(cv, x, gtop - 0.6, s * 0.1, s * 0.09, 'b')
    box(cv, 0, gtop, w, h, 'g')
    x0, x1, wy = w * 0.12, w * 0.88, h * 0.5
    if shape == 'gable':
        box(cv, x0, wy, x1, gtop + 0.5, 'q')
        poly(cv, [(x0, wy + 0.5), (cx, h * 0.24), (x1, wy + 0.5)], 'q')
    elif shape == 'arch':
        box(cv, x0, wy, x1, gtop + 0.5, 'q')
        oval(cv, cx, wy + 0.5, (x1 - x0) / 2, h * 0.24, 'q')
    else:
        box(cv, x0, h * 0.62, x1, gtop + 0.5, 'q')
        poly(cv, [(x0, h * 0.62 + 0.5), (x0 + 2.5, h * 0.54), (x1 - 2.5, h * 0.54), (x1, h * 0.62 + 0.5)], 'q')
        box(cv, cx - w * 0.2, h * 0.42, cx + w * 0.2, gtop + 0.5, 'q')
        oval(cv, cx, h * 0.42 + 0.5, w * 0.2, h * 0.17, 'q')
    for k, x in enumerate((x0 + 2.2, x0 + 5.0, x1 - 5.0, x1 - 2.2)):
        tall = plants == 'vines' or (plants == 'mixed' and k % 2 == 1)
        box(cv, x - 1.0, gtop - 1.6, x + 1.0, gtop - 0.2, 'k')
        if tall:
            seg(cv, x, gtop - 1.6, x, h * 0.6, 'p', 0.5)
            for j in range(3):
                y = gtop - 3.2 - j * 2.2
                lens(cv, x, y, x + (1 if (j + k) % 2 else -1) * 1.9, y - 0.9, 1.1, 'p')
        else:
            disc(cv, x, gtop - 3.0, 1.6, 'p')
    scatter(cv, 'o', 'p', 5, r, sep=2)
    _outline(cv, 'qpko', 'f')
    for x in (cx - 4.5, cx + 4.5):
        box(cv, x - 0.5, 0, x + 0.5, gtop + 0.5, 'f', only='q')
    box(cv, 0, wy - 0.4, w, wy + 0.5, 'f', only='q')
    rbox(cv, cx - 1.6, gtop - 5.4, cx + 1.6, gtop + 0.5, 0.5, 'd')
    box(cv, cx - 0.6, gtop - 4.4, cx + 0.6, gtop - 2.6, 'q')
    scatter(cv, 'o', 'g', 4, r, sep=2, area=(0, gtop + 1, w - 1, h - 1))
    if r.random() < 0.5:
        rbox(cv, w * 0.01, gtop - 2.4, w * 0.1, gtop + 0.4, 0.4, 'd')
        seg(cv, w * 0.1, gtop - 1.2, w * 0.13, gtop - 3.4, 'd', 0.5)
    if r.random() < 0.5:
        _mirror(cv)
    return cv, [sky(), role('q', 'glass', 'Glass', BLUE), role('f', 'frame', 'Frame', BROWN), role('k', 'pot', 'Pots', BROWN),
                role('u', 'sun', 'Sun', BROWN), role('p', 'plants', 'Plants', GREEN), role('g', 'grass', 'Grass', GREEN),
                role('b', 'bush', 'Bushes', GREEN), role('d', 'door', 'Door and watering can', PINK), role('o', 'flowers', 'Flowers', PINK)],\
        ['places', 'garden']


def wishing_well(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.16)
    night = r.random() < 0.5
    roof = r.choice(('gable', 'hood'))
    wx = cx + r.choice((-1.5, 0, 1.5))
    rx, ty, by = w * 0.27, h * 0.62, gtop + 0.3
    for d in (-1, 1):
        if r.random() < 0.75:
            oval(cv, wx + d * (rx + 2.8), gtop - 0.5, 2.3, 1.8, 'v')
    hills(cv, 'g', gtop, 0.4, w * 1.4, r.uniform(0, 6))
    scatter(cv, 'f', 'v', 4, r, sep=2)
    pt = ty - h * 0.3
    for d in (-1, 1):
        x = int(wx + d * (rx - 0.9)) + 0.5
        box(cv, x - 0.5, pt, x + 0.5, ty - 0.5, 'p')
    ay = ty - h * 0.19
    box(cv, wx - rx + 0.9, ay - 0.45, wx + rx - 0.9, ay + 0.45, 'p')
    seg(cv, wx + rx - 0.4, ay, wx + rx + 1.3, ay + 1.4, 'p', 0.5)
    rope = int(wx) + 0.5
    if r.random() < 0.5:
        seg(cv, rope, ay + 0.5, rope, ay + 2.6, 'o', 0.5)
        poly(cv, [(rope - 1.5, ay + 2.6), (rope + 1.5, ay + 2.6), (rope + 1.0, ay + 4.6), (rope - 1.0, ay + 4.6)], 'b')
    else:
        seg(cv, rope, ay + 0.5, rope, ty - 1.2, 'o', 0.5)
        bx = wx + r.choice((-1, 1)) * (rx - 2.2)
        poly(cv, [(bx - 1.5, ty - 2.8), (bx + 1.5, ty - 2.8), (bx + 1.0, ty - 0.9), (bx - 1.0, ty - 0.9)], 'b')
    if roof == 'gable':
        poly(cv, [(wx - rx - 1.8, pt + 0.8), (wx, pt - h * 0.13), (wx + rx + 1.8, pt + 0.8)], 'r')
    else:
        for gy in range(h):
            for gx in range(w):
                px, py = gx + 0.5, gy + 0.5
                if py <= pt + 0.8 and ((px - wx) / (rx + 1.8)) ** 2 + ((py - pt - 0.8) / (h * 0.14)) ** 2 <= 1.0:
                    cv.g[gy][gx] = 'r'
    box(cv, wx - rx, ty, wx + rx, by, 'k')
    oval(cv, wx, by, rx, 1.0, 'k')
    oval(cv, wx, ty, rx, 1.6, 'k')
    for k, y in enumerate((ty + 1.8, ty + 3.8, ty + 5.8)):
        box(cv, wx - rx, y - 0.3, wx + rx, y + 0.3, 'm', only='k')
        x = wx - rx + 1.2 + (k % 2) * 1.5
        while x < wx + rx - 0.6:
            box(cv, x - 0.3, y - 2.0, x + 0.3, y - 0.3, 'm', only='k')
            x += 3.0
    oval(cv, wx, ty - 0.1, rx - 1.3, 0.85, 'w')
    sx = w * (0.14 if wx >= cx else 0.86)
    if night:
        _moon(cv, sx, h * 0.1, s * 0.1, 'u', 's')
        scatter(cv, 'x', 's', 8, r, sep=3, area=(0, 0, w - 1, h * 0.5))
    else:
        disc(cv, sx, h * 0.09, s * 0.08, 'u')
    if r.random() < 0.5:
        _mirror(cv)
    return cv, [sky(), role('p', 'post', 'Posts and crank', BROWN), role('o', 'rope', 'Rope', BROWN), _light(night),
                role('k', 'stones', 'Stones', BROWN), role('r', 'roof', 'Roof', PINK), role('b', 'bucket', 'Bucket', PINK),
                role('m', 'mortar', 'Mortar', PINK), role('f', 'flowers', 'Flowers', PINK), role('w', 'water', 'Water', BLUE),
                role('g', 'grass', 'Grass', GREEN), role('v', 'bush', 'Bushes', GREEN), role('x', 'stars', 'Stars', BROWN)],\
        ['places', 'garden']


def fountain(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.16)
    tiers = r.choice((2, 3))
    night = r.random() < 0.3
    back = r.choice(('hedge', 'trees', 'trees'))
    if back == 'hedge':
        rbox(cv, -1, gtop - h * 0.2, w + 1, gtop + 0.5, 1.5, 't')
    else:
        _tree(cv, w * r.choice((0.1, 0.9)), gtop, s * r.uniform(0.12, 0.15), 't', 'd')
    box(cv, 0, gtop, w, h, 'g')
    dots(cv, 'q', 'g', 3, 2, area=(0, gtop + 1, w - 1, h - 1))
    by0 = gtop - h * 0.12
    bx0, bx1 = w * 0.11, w * 0.89
    levels = [(h * 0.5, w * 0.27)] if tiers == 2 else [(h * 0.55, w * 0.27), (h * 0.37, w * 0.15)]
    box(cv, cx - 1.0, levels[-1][0], cx + 1.0, by0, 'd')
    oval(cv, cx, by0 - 0.7, (bx1 - bx0) / 2 - 1.2, 0.8, 'w')
    for k, (y, half) in enumerate(levels):
        below = by0 - 0.6 if k == 0 else levels[k - 1][0]
        for d in (-1, 1):
            path(cv, [(cx + d * half, y + 0.3), (cx + d * (half + 1.0), y + 1.3), (cx + d * (half + 1.4), below)], 'w', 0.5)
        poly(cv, [(cx - half, y), (cx + half, y), (cx + half * 0.45, y + 1.9), (cx - half * 0.45, y + 1.9)], 'k')
    top = levels[-1][0]
    jet = h * (0.16 if tiers == 2 else 0.11)
    seg(cv, cx, top, cx, top - jet, 'w', 0.6)
    for d in (-1, 1):
        path(cv, [(cx, top - jet), (cx + d * 1.6, top - jet - 0.6), (cx + d * 2.8, top - jet + 1.2)], 'w', 0.5)
    box(cv, bx0 + 0.6, by0, bx1 - 0.6, gtop + 0.5, 'k')
    rbox(cv, bx0, by0 - 0.6, bx1, by0 + 0.6, 0.6, 'd')
    scatter(cv, 'p', 's', 6, r, sep=2, area=(cx - 6, top - jet - 3, cx + 6, top - 1))
    if r.random() < 0.6:
        for x in r.sample((bx0 + 1.5, bx0 + 4.0, bx1 - 4.0, bx1 - 1.5), 2):
            box(cv, x - 1.0, by0 - 1.6, x + 0.4, by0 - 0.7, 'v')
            cv.put(int(x + 1.0), int(by0 - 1.6), 'v')
    sx = w * r.choice((0.14, 0.86))
    if night:
        _moon(cv, sx, h * 0.09, s * 0.1, 'u', 's')
        scatter(cv, 'x', 's', 7, r, sep=3, area=(0, 0, w - 1, h * 0.45))
    else:
        disc(cv, sx, h * 0.09, s * 0.08, 'u')
    if r.random() < 0.5:
        _mirror(cv)
    return cv, [sky('s', 'Evening sky', PINK), role('k', 'stone', 'Bowls and basin', BROWN), role('d', 'rim', 'Rim and pedestal', BROWN),
                _light(night), role('w', 'water', 'Water', BLUE), role('p', 'spray', 'Spray', BLUE), role('v', 'doves', 'Doves', BLUE),
                role('g', 'plaza', 'Lawn', GREEN), role('t', 'hedge', 'Hedge and trees', GREEN), role('q', 'tiles', 'Lawn dots', BROWN),
                role('x', 'stars', 'Stars', BROWN)], ['places', 'park']


def gazebo(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.12)
    roof = r.choice(('cone', 'dome', 'tiers'))
    night = r.random() < 0.3
    x0, x1 = w * 0.16, w * 0.84
    eave, deck = h * r.uniform(0.41, 0.45), gtop - 1.6
    E = (x1 - x0) / 2 + 2.2
    for x in (w * 0.03, w * 0.97):
        if r.random() < 0.7:
            oval(cv, x, gtop - 1.0, s * 0.12, s * 0.13, 't')
    box(cv, 0, gtop, w, h, 'g')
    box(cv, x0 - 1.2, deck, x1 + 1.2, gtop + 0.4, 'b')
    box(cv, cx - 2.5, gtop, cx + 2.5, gtop + 1.4, 'b')
    xs = [int(x) + 0.5 for x in (x0, x0 + (x1 - x0) / 3, x1 - (x1 - x0) / 3, x1)]
    for x in xs:
        box(cv, x - 0.5, eave, x + 0.5, deck, 'p')
    ry = deck - 3.0
    for a, b in ((xs[0], xs[1]), (xs[2], xs[3])):
        box(cv, a, ry - 0.45, b, ry + 0.45, 'l')
        for xx in range(int(a) + 2, int(b), 2):
            box(cv, xx, ry, xx + 0.9, deck, 'l')
    box(cv, x0, eave, x1, eave + 0.9, 'l')
    if roof == 'cone':
        apex = h * 0.1
        for gy in range(h):
            for gx in range(w):
                px, py = gx + 0.5, gy + 0.5
                if apex <= py <= eave + 0.5 and abs(px - cx) <= E * ((py - apex) / (eave - apex)) ** 1.3:
                    cv.g[gy][gx] = 'r'
    elif roof == 'dome':
        apex = h * 0.06
        poly(cv, [(cx - E * 0.62, eave - 1.6), (cx + E * 0.62, eave - 1.6), (cx + E, eave + 0.5), (cx - E, eave + 0.5)], 'r')
        oval(cv, cx, eave - h * 0.17, E * 0.62, h * 0.15, 'r')
        poly(cv, [(cx - 1.6, eave - h * 0.29), (cx, apex + 0.5), (cx + 1.6, eave - h * 0.29)], 'r')
    else:
        apex = h * 0.12
        poly(cv, [(cx - E * 0.55, eave - h * 0.12), (cx + E * 0.55, eave - h * 0.12), (cx + E, eave + 0.5), (cx - E, eave + 0.5)], 'r')
        box(cv, cx - 1.6, eave - h * 0.17, cx + 1.6, eave - h * 0.12, 'p')
        poly(cv, [(cx - E * 0.5, eave - h * 0.17 + 0.5), (cx, apex), (cx + E * 0.5, eave - h * 0.17 + 0.5)], 'r')
    for x in range(int(cx - E) + 1, int(cx + E), 2):
        cv.put(x, int(eave + 0.5), 'e')
    seg(cv, cx, apex + 0.5, cx, max(1.0, apex - 1.8), 'k', 0.6)
    if r.random() < 0.6:
        for x in (xs[1], xs[2]):
            disc(cv, x, eave + 2.6, 1.0, 'f')
    if r.random() < 0.6:
        for x in (xs[0], xs[3]):
            path(cv, [(x - 0.6, deck), (x + 0.6, deck - 2.5), (x - 0.6, deck - 5.0), (x + 0.6, deck - 7.5)], 'v', 0.5)
    scatter(cv, 'f', 't', 4, r, sep=2)
    sx = w * r.choice((0.12, 0.88))
    if night:
        _moon(cv, sx, h * 0.08, s * 0.09, 'u', 's')
        scatter(cv, 'x', 's', 6, r, sep=3, area=(0, 0, w - 1, h * 0.4))
    else:
        disc(cv, sx, h * 0.08, s * 0.075, 'u')
    if r.random() < 0.5:
        _mirror(cv)
    return cv, [sky(), role('r', 'roof', 'Roof', PINK), role('e', 'trim', 'Eave trim', PINK), role('f', 'flowers', 'Flowers', PINK),
                role('p', 'post', 'Posts', BROWN), role('l', 'rail', 'Railings and frieze', BROWN), _light(night),
                role('k', 'finial', 'Finial', BROWN), role('b', 'deck', 'Deck and steps', BLUE), role('g', 'lawn', 'Lawn', GREEN),
                role('t', 'bush', 'Bushes', GREEN), role('v', 'vine', 'Vines', GREEN), role('x', 'stars', 'Stars', BROWN)],\
        ['places', 'garden']


def log_cabin(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.13)
    season = r.choice(('summer', 'autumn', 'winter'))
    night = r.random() < 0.35
    hx = cx + r.choice((-1.5, 0, 1.5))
    hw, wy, apex = w * 0.29, h * 0.54, h * 0.25
    for x in (w * 0.07, w * 0.93):
        if abs(x - hx) > hw + 2.5:
            if season == 'autumn':
                _tree(cv, x, gtop, s * 0.13, 'a', 't')
            else:
                _pine(cv, x, gtop + 0.5, h * 0.42, w * 0.11, 'p')
    box(cv, 0, gtop, w, h, 'g')
    chx = hx + hw * 0.45
    box(cv, chx - 1.0, h * 0.2, chx + 1.0, wy, 'c')
    poly(cv, [(hx - hw, wy + 0.5), (hx, apex + 1.0), (hx + hw, wy + 0.5)], 'h')
    y = wy
    while y < gtop - 0.5:
        rbox(cv, hx - hw - 1.0, y, hx + hw + 1.0, y + 2.9, 1.2, 'h')
        box(cv, hx - hw, y + 2.0, hx + hw, y + 2.9, 'l')
        y += 3.0
    y = wy - 1.0
    while y > apex + 2.5:
        box(cv, 0, y, w, y + 0.9, 'l', only='h')
        y -= 3.0
    disc(cv, hx, wy - 2.6, 0.9, 'i')
    for d in (-1, 1):
        seg(cv, hx, apex, hx + d * (hw + 1.9), wy + 1.0, 'r', 0.9)
    if season == 'winter':
        for d in (-1, 1):
            seg(cv, hx, apex - 0.9, hx + d * (hw + 2.0), wy - 0.1, 'x', 0.55)
    rbox(cv, hx - 1.4, gtop - 5.2, hx + 1.4, gtop + 0.5, 0.6, 'd')
    for d in (-1, 1):
        box(cv, hx + d * 4.2 - 1.2, wy + 2.3, hx + d * 4.2 + 1.2, wy + 4.6, 'i')
    for k in range(3):
        disc(cv, chx + k * 1.4 * (1 if hx <= cx else -1), h * 0.16 - k * 1.5, 0.9 + k * 0.25, 'k')
    if season == 'summer':
        scatter(cv, 'f', 'g', 5, r, sep=2, area=(0, gtop + 1, w - 1, h - 1))
    elif season == 'winter':
        scatter(cv, 'z', 's', 8, r, sep=3, area=(0, 0, w - 1, h * 0.5))
    sx = w * (0.12 if hx > cx else 0.88 if hx < cx else r.choice((0.12, 0.88)))
    if night:
        _moon(cv, sx, h * 0.08, s * 0.09, 'u', 's')
        if season != 'winter':
            scatter(cv, 'z', 's', 7, r, sep=3, area=(0, 0, w - 1, h * 0.45))
    else:
        disc(cv, sx, h * 0.08, s * 0.075, 'u')
    if r.random() < 0.5:
        _mirror(cv)
    return cv, [sky(), role('h', 'logs', 'Logs', BROWN), role('l', 'lines', 'Log lines', BROWN), _light(night),
                role('c', 'chimney', 'Chimney', BROWN), role('t', 'trunk', 'Trunks', BROWN), role('r', 'roof', 'Roof', PINK),
                role('d', 'door', 'Door', PINK), role('a', 'autumn', 'Autumn leaves', PINK), role('f', 'flowers', 'Flowers', PINK),
                role('z', 'specks', 'Stars and snowflakes', PINK), role('i', 'window', 'Windows', BLUE),
                role('g', 'ground', 'Meadow' if season != 'winter' else 'Snow', GREEN), role('p', 'pine', 'Pines', GREEN),
                role('k', 'smoke', 'Smoke', GREEN), role('x', 'snow', 'Snow on the roof', GREEN)], ['places', 'forest']


def campfire(w, h, r):
    cv, s, cx, big = start(w, h, 'n')
    logs = r.choice(('teepee', 'cross'))
    prop = r.choice(('tent', 'log', 'none'))
    gtop = h * r.uniform(0.56, 0.62)
    mx = w * r.choice((0.16, 0.84))
    _moon(cv, mx, h * 0.1, s * 0.11, 'm', 'n')
    x = -1.0
    while x < w + 2:
        _pine(cv, x, gtop + 1, h * r.uniform(0.24, 0.36), 2.6, 'p')
        x += r.uniform(3.0, 4.2)
    box(cv, 0, gtop, w, h, 'g')
    fx, fy = cx, h * 0.84
    tx = w * 0.17 if mx > cx else w * 0.83
    if prop == 'tent':
        poly(cv, [(tx - w * 0.17, fy - 0.5), (tx, fy - h * 0.3), (tx + w * 0.17, fy - 0.5)], 'e')
        poly(cv, [(tx - w * 0.05, fy - 0.5), (tx, fy - h * 0.16), (tx + w * 0.05, fy - 0.5)], 'o')
    elif prop == 'log':
        rbox(cv, tx - 3.2, fy - 1.6, tx + 3.2, fy + 0.2, 0.8, 'l')
        disc(cv, tx + (3.0 if tx > cx else -3.0), fy - 0.7, 0.9, 'y')
    ring_pts = [(fx + math.cos(math.radians(a)) * 5.4, fy + math.sin(math.radians(a)) * 1.5) for a in range(0, 360, 30)]
    for x, y in ring_pts:
        if y < fy:
            disc(cv, x, y, 0.95, 'k')
    tall = h * r.uniform(0.38, 0.44)
    for dx, top, lean, wd in ((0, tall, 0.6, 5.0), (-1.6, tall * 0.72, -2.4, 3.4), (1.6, tall * 0.78, 2.6, 3.4),
                              (-3.0, tall * 0.45, -1.6, 2.4), (3.0, tall * 0.5, 1.8, 2.4)):
        lens(cv, fx + dx, fy, fx + dx + lean, fy - top, wd, 'f')
    lens(cv, fx, fy, fx + 0.4, fy - tall * 0.58, 3.4, 'y')
    lens(cv, fx - 0.8, fy, fx - 1.8, fy - tall * 0.38, 2.0, 'y')
    if logs == 'teepee':
        for d in (-1.0, -0.4, 0.4, 1.0):
            seg(cv, fx + d * 4.2, fy + 1.0, fx + d * 0.6, fy - 4.4, 'l', 0.6)
    else:
        seg(cv, fx - 4.6, fy + 1.2, fx + 4.6, fy - 1.0, 'l', 0.75)
        seg(cv, fx - 4.6, fy - 1.0, fx + 4.6, fy + 1.2, 'l', 0.75)
    for x, y in ring_pts:
        if y >= fy:
            disc(cv, x, y, 0.95, 'k')
    scatter(cv, 'z', 'n', 6, r, sep=2, area=(fx - 5, 1, fx + 5, fy - h * 0.42))
    scatter(cv, 'x', 'n', 7, r, sep=3, area=(0, 0, w - 1, gtop - 6))
    if r.random() < 0.5:
        _mirror(cv)
    return cv, [('n', 'night', 'Night sky', BLUE, True), role('k', 'stones', 'Stones', BLUE), role('o', 'tent_door', 'Tent door', BLUE),
                role('f', 'flame', 'Flames', PINK), role('z', 'sparks', 'Sparks', PINK), role('e', 'tent', 'Tent', PINK),
                role('y', 'core', 'Flame core', BROWN), role('l', 'logs', 'Logs', BROWN), role('m', 'moon', 'Moon', BROWN),
                role('x', 'stars', 'Stars', BROWN), role('p', 'pines', 'Pines', GREEN), role('g', 'clearing', 'Clearing', GREEN)],\
        ['places', 'camping']


def observatory(w, h, r):
    cv, s, cx, big = start(w, h, 'n')
    site = r.choice(('hill', 'peak', 'plain'))
    tilt = r.choice((-1, 1))
    ox = cx + r.uniform(-1.5, 1.5)
    mx = w * (0.16 if tilt > 0 else 0.84)
    disc(cv, mx, h * 0.12, s * 0.12, 'm')
    if r.random() < 0.5:
        disc(cv, mx + s * 0.07, h * 0.12 - s * 0.03, s * 0.09, 'n')
    if site == 'hill':
        oval(cv, ox, h * 1.02, w * 0.8, h * 0.3, 'g')
        gl = h * 0.72
    elif site == 'peak':
        poly(cv, [(-1, h), (-1, h * 0.86), (ox - 6.5, h * 0.66), (ox + 6.5, h * 0.66), (w + 1, h * 0.84), (w + 1, h)], 'g')
        gl = h * 0.66
    else:
        hills(cv, 'g', h * 0.8, 0.5, w * 1.2, r.uniform(0, 6))
        gl = h * 0.8
    for x in (w * 0.08, w * 0.92):
        if r.random() < 0.7:
            _pine(cv, x, h * 0.9 if site != 'peak' else h * 0.95, h * 0.26, w * 0.09, 'p')
    dy = gl - h * 0.18
    box(cv, ox - 5.0, dy, ox + 5.0, gl + 0.5, 'w')
    for gy in range(h):
        for gx in range(w):
            px, py = gx + 0.5, gy + 0.5
            if py <= dy + 0.5 and math.hypot(px - ox, py - dy - 0.5) <= 5.6:
                cv.g[gy][gx] = 'd'
                if abs(px - ox - tilt * 0.35 * (dy - py)) <= 0.8 and py > dy - 5.0:
                    cv.g[gy][gx] = 'k'
    box(cv, ox - 5.6, dy, ox + 5.6, dy + 0.9, 'k')
    seg(cv, ox, dy - 2.0, ox + tilt * 6.0, dy - 8.2, 't', 0.85)
    rbox(cv, ox - 1.2, gl - 3.2, ox + 1.2, gl + 0.5, 0.8, 'o')
    for d in (-1, 1):
        box(cv, ox + d * 3.2 - 0.5, dy + 1.8, ox + d * 3.2 + 0.5, dy + 2.8, 'o')
    if r.random() < 0.6:
        path(cv, [(w * (0.55 if tilt < 0 else 0.45) - tilt * 4, h * 0.06), (w * 0.5 + tilt * 1, h * 0.12)], 'z', 0.45)
        disc(cv, w * 0.5 + tilt * 1.2, h * 0.125, 0.9, 'z')
    scatter(cv, 'x', 'n', 9, r, sep=3, area=(0, 0, w - 1, dy - 2))
    if r.random() < 0.5:
        _mirror(cv)
    return cv, [('n', 'night', 'Night sky', PINK, True), role('k', 'slit', 'Slit and rim', PINK), role('d', 'dome', 'Dome', BLUE),
                role('m', 'moon', 'Moon', BLUE), role('o', 'door', 'Door and windows', BLUE), role('w', 'base', 'Base', BROWN),
                role('t', 'telescope', 'Telescope', BROWN), role('x', 'stars', 'Stars', BROWN), role('z', 'comet', 'Shooting star', BROWN),
                role('g', 'hill', 'Hill', GREEN), role('p', 'pines', 'Pines', GREEN)], ['places', 'night']


DAILY_PLACES = [pagoda, pyramid, volcano, waterfall, stone_bridge, clock_tower, skyscraper, carousel, circus_tent,
                greenhouse, wishing_well, fountain, gazebo, log_cabin, campfire, observatory]

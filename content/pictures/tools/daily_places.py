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

CROW = ["...XX.",
        "XXXXXX",
        ".XXXX.",
        "..X.X."]

BAT = ["..X.X..",
       "X.XXX.X",
       "XXXXXXX",
       ".X.X.X."]

FISH = ["..XX...",
        ".XXXX.X",
        "XXXXXXX",
        ".XXXX.X",
        "..XX..."]


def _sprite(cv, x0, y0, rows, marks, flip=False, on=None):
    """A small pixel figure (a camel, a horse) from rows of template characters, `marks` mapping each to a role; `on`
    limits it to cells of those roles."""
    for j, row in enumerate(rows):
        for i, ch in enumerate(row[::-1] if flip else row):
            x, y = int(x0) + i, int(y0) + j
            if ch in marks and 0 <= x < cv.w and 0 <= y < cv.h and (on is None or cv.g[y][x] in on):
                cv.g[y][x] = marks[ch]


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
                role('g', 'grass', 'Grass', GREEN), role('v', 'mountain', 'Mountain and hills', GREEN),
                role('t', 'trunk', 'Trunk', BROWN), role('b', 'blossom', 'Blossom', PINK), role('p', 'pine', 'Pine', GREEN),
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
                role('c', 'camel', 'Camel', BROWN), role('t', 'trunk', 'Palm trunk', BROWN),
                role('f', 'palm', 'Palm leaves', GREEN), _light(time == 'night', 'u', PINK),
                role('x', 'stars', 'Stars', BROWN)], ['places', 'desert']


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
        pts += [(slope(f, t)[0] + 0.6 * math.sin(t * 9 + ph), slope(f, t)[1])
                for t in (0.15, 0.3, 0.45, 0.6, 0.75, 0.9) if t <= end]
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
                role('t', 'trunk', 'Palm trunks', BROWN), role('w', 'sea', 'Sea', BLUE), _light(mode == 'night', 'u', BROWN),
                role('x', 'stars', 'Stars', BROWN)], ['places', 'nature']


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
        for _ in range(3):
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
                role('a', 'arch', 'Arch stones', PINK), role('w', 'river', 'River', BLUE),
                role('q', 'ripples', 'Ripples', BLUE), role('v', 'far_bank', 'Far bank', GREEN),
                role('t', 'tree', 'Tree', GREEN), role('g', 'bank', 'Near banks', GREEN), role('k', 'trunk', 'Trunk', BROWN),
                role('l', 'lamps', 'Lamps and boat', PINK), role('x', 'stars', 'Stars', BROWN)], ['places', 'river']


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
                role('f', 'face', 'Clock face', BLUE), role('k', 'hands', 'Hands and pole', BROWN),
                role('r', 'roof', 'Roof', PINK), role('i', 'window', 'Windows', BLUE), role('d', 'door', 'Door', PINK),
                role('e', 'house', 'Houses', PINK), role('q', 'house_roof', 'House roofs and trunks', BROWN),
                role('t', 'trees', 'Trees', GREEN), role('g', 'grass', 'Grass', GREEN), _light(night),
                role('x', 'stars', 'Stars', BROWN)], ['places', 'town']


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
                role('i', 'window', 'Windows', BLUE), role('n', 'left', 'Left building', PINK),
                role('e', 'right', 'Right building', PINK), role('d', 'street', 'Street', GREEN),
                role('y', 'lines', 'Road lines', BROWN), role('c', 'cloud', 'Cloud', GREEN), _light(night, 'u', PINK),
                role('x', 'stars', 'Stars', BROWN)], ['places', 'city']


def carousel(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.1)
    night = r.choice((False, True, False))
    layout = r.choice(('wide', 'left', 'right'))
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
                role('x', 'horse', 'Horses', GREEN), role('e', 'saddle', 'Saddles', PINK),
                role('p', 'platform', 'Platform', BROWN), role('d', 'lights', 'Lights', BLUE),
                role('w', 'window', 'Booth window', BLUE), role('f', 'flag', 'Flag and tree', GREEN),
                role('g', 'grass', 'Grass', GREEN), role('q', 'flowers', 'Lawn dots', BLUE),
                role('z', 'stars', 'Stars', BROWN)], ['places', 'fair']


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
                role('e', 'trim', 'Valance and drapes', GREEN), role('d', 'door', 'Entrance', BLUE),
                role('k', 'pole', 'Poles', BROWN), role('f', 'flag', 'Flags', PINK), role('g', 'grass', 'Grass', GREEN),
                _light(night), role('x', 'stars', 'Stars', BROWN)], ['places', 'fair']


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
                role('b', 'bush', 'Bushes', GREEN), role('d', 'door', 'Door and watering can', PINK),
                role('o', 'flowers', 'Flowers', PINK)], ['places', 'garden']


def wishing_well(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.16)
    night = r.random() < 0.5
    roof = r.choice(('cone', 'gable', 'hood'))
    scene = r.choice(('tree', 'fence', 'bushes'))
    wx = cx + r.choice((-1.5, 0, 1.5))
    rx, ty, by = w * 0.27, h * 0.62, gtop + 0.3
    if scene == 'fence':
        for x in range(0, w, 2):
            poly(cv, [(x, gtop - 4.4), (x + 0.5, gtop - 5.0), (x + 1, gtop - 4.4), (x + 1, gtop + 0.5), (x, gtop + 0.5)], 'p')
        box(cv, 0, gtop - 3.4, w, gtop - 2.8, 'p')
    elif scene == 'tree':
        tx = wx + (rx + 3.6) * r.choice((-1, 1))
        box(cv, tx - 0.8, h * 0.4, tx + 0.8, gtop + 0.5, 'o')
        disc(cv, tx, h * 0.3, s * 0.2, 'v')
        scatter(cv, 'f', 'v', 4, r, sep=2)
    else:
        for d in (-1, 1):
            oval(cv, wx + d * (rx + 2.8), gtop - 0.5, 2.3, 1.8, 'v')
        scatter(cv, 'f', 'v', 4, r, sep=2)
    hills(cv, 'g', gtop, 0.4, w * 1.4, r.uniform(0, 6))
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
    elif roof == 'cone':
        poly(cv, [(wx - rx - 1.2, pt + 0.8), (wx - rx * 0.4, pt - h * 0.06), (wx, pt - h * 0.22), (wx + rx * 0.4, pt - h * 0.06),
                  (wx + rx + 1.2, pt + 0.8)], 'r')
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
                role('g', 'grass', 'Grass', GREEN), role('v', 'bush', 'Bushes', GREEN),
                role('x', 'stars', 'Stars', BROWN)], ['places', 'garden']


def fountain(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.16)
    night = r.random() < 0.3
    back = r.choice(('hedge', 'trees', 'lamps'))
    jet_kind = r.choice(('ball', 'jet', 'arcs'))
    tiers = r.choice((2, 3))
    if back == 'hedge':
        rbox(cv, -1, gtop - h * 0.2, w + 1, gtop + 0.5, 1.5, 't')
    elif back == 'trees':
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
    if jet_kind == 'jet':
        seg(cv, cx, top, cx, top - jet, 'w', 0.6)
        for d in (-1, 1):
            path(cv, [(cx, top - jet), (cx + d * 1.6, top - jet - 0.6), (cx + d * 2.8, top - jet + 1.2)], 'w', 0.5)
    elif jet_kind == 'arcs':
        seg(cv, cx, top, cx, top - 1.5, 'w', 0.6)
        for d in (-1, 1):
            path(cv, [(cx, top - 1.5), (cx + d * 2.0, top - jet * 0.9), (cx + d * 4.5, top - jet * 0.7),
                      (cx + d * 5.5, top - 1.0)], 'w', 0.5)
    else:
        box(cv, cx - 0.5, top - 2.2, cx + 0.5, top, 'd')
        disc(cv, cx, top - 3.2, 1.6, 'd')
        for d in (-1, 1):
            path(cv, [(cx + d * 1.2, top - 3.6), (cx + d * 2.6, top - 3.0), (cx + d * 3.2, top - 0.6)], 'w', 0.5)
    box(cv, bx0 + 0.6, by0, bx1 - 0.6, gtop + 0.5, 'k')
    rbox(cv, bx0, by0 - 0.6, bx1, by0 + 0.6, 0.6, 'd')
    scatter(cv, 'p', 's', 6, r, sep=2, area=(cx - 6, top - jet - 3, cx + 6, top - 1))
    if r.random() < 0.6:
        for x in r.sample((bx0 + 1.5, bx0 + 4.0, bx1 - 4.0, bx1 - 1.5), 2):
            box(cv, x - 1.0, by0 - 1.6, x + 0.4, by0 - 0.7, 'v')
            cv.put(int(x + 1.0), int(by0 - 1.6), 'v')
    if back == 'lamps':
        for x in (w * 0.05, w * 0.95):
            box(cv, x - 0.5, gtop - h * 0.3, x + 0.5, gtop + 0.5, 'd')
            rbox(cv, x - 1.0, gtop - h * 0.3 - 2.0, x + 1.0, gtop - h * 0.3, 0.4, 'v')
    sx = w * r.choice((0.14, 0.86))
    if night:
        _moon(cv, sx, h * 0.09, s * 0.1, 'u', 's')
        scatter(cv, 'x', 's', 7, r, sep=3, area=(0, 0, w - 1, h * 0.45))
    else:
        disc(cv, sx, h * 0.09, s * 0.08, 'u')
    if r.random() < 0.5:
        _mirror(cv)
    return cv, [sky('s', 'Evening sky', PINK), role('k', 'stone', 'Bowls and basin', BROWN),
                role('d', 'rim', 'Rim and pedestal', BROWN), _light(night), role('w', 'water', 'Water', BLUE),
                role('p', 'spray', 'Spray', BLUE), role('v', 'doves', 'Doves and lamps', BLUE),
                role('g', 'plaza', 'Lawn', GREEN), role('t', 'hedge', 'Hedge and trees', GREEN),
                role('q', 'tiles', 'Lawn dots', BROWN), role('x', 'stars', 'Stars', BROWN)], ['places', 'park']


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
        poly(cv, [(cx - E * 0.55, eave - h * 0.12), (cx + E * 0.55, eave - h * 0.12), (cx + E, eave + 0.5),
                  (cx - E, eave + 0.5)], 'r')
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
    return cv, [sky(), role('r', 'roof', 'Roof', PINK), role('e', 'trim', 'Eave trim', PINK),
                role('f', 'flowers', 'Flowers', PINK), role('p', 'post', 'Posts', BROWN),
                role('l', 'rail', 'Railings and frieze', BROWN), _light(night), role('k', 'finial', 'Finial', BROWN),
                role('b', 'deck', 'Deck and steps', BLUE), role('g', 'lawn', 'Lawn', GREEN), role('t', 'bush', 'Bushes', GREEN),
                role('v', 'vine', 'Vines', GREEN), role('x', 'stars', 'Stars', BROWN)], ['places', 'garden']


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
                role('d', 'door', 'Door', PINK), role('a', 'autumn', 'Autumn leaves', PINK),
                role('f', 'flowers', 'Flowers', PINK), role('z', 'specks', 'Stars and snowflakes', PINK),
                role('i', 'window', 'Windows', BLUE), role('g', 'ground', 'Meadow' if season != 'winter' else 'Snow', GREEN),
                role('p', 'pine', 'Pines', GREEN), role('k', 'smoke', 'Smoke', GREEN),
                role('x', 'snow', 'Snow on the roof', GREEN)], ['places', 'forest']


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
    return cv, [('n', 'night', 'Night sky', BLUE, True), role('k', 'stones', 'Stones', BLUE),
                role('o', 'tent_door', 'Tent door', BLUE), role('f', 'flame', 'Flames', PINK),
                role('z', 'sparks', 'Sparks', PINK), role('e', 'tent', 'Tent', PINK), role('y', 'core', 'Flame core', BROWN),
                role('l', 'logs', 'Logs', BROWN), role('m', 'moon', 'Moon', BROWN), role('x', 'stars', 'Stars', BROWN),
                role('p', 'pines', 'Pines', GREEN), role('g', 'clearing', 'Clearing', GREEN)], ['places', 'camping']


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
    return cv, [('n', 'night', 'Night sky', PINK, True), role('k', 'slit', 'Slit and rim', PINK),
                role('d', 'dome', 'Dome', BLUE), role('m', 'moon', 'Moon', BLUE), role('o', 'door', 'Door and windows', BLUE),
                role('w', 'base', 'Base', BROWN), role('t', 'telescope', 'Telescope', BROWN),
                role('x', 'stars', 'Stars', BROWN), role('z', 'comet', 'Shooting star', BROWN),
                role('g', 'hill', 'Hill', GREEN), role('p', 'pines', 'Pines', GREEN)], ['places', 'night']


def market_stall(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.12)
    goods = r.choice(('fruit', 'flowers', 'harvest'))
    top = r.choice(('sign', 'bunting', 'none'))
    n = r.choice((6, 8))
    x0, x1 = w * 0.13, w * 0.87
    at, ab, ct = h * 0.28, h * 0.43, h * 0.68
    sx = w * r.choice((0.1, 0.9))
    disc(cv, sx, h * 0.07, s * 0.07, 'u')
    box(cv, 0, gtop, w, h, 'g')
    for x in (x0 + 0.5, x1 - 0.5):
        box(cv, x - 0.5, at, x + 0.5, gtop + 0.5, 'p')
    box(cv, x0 + 1.0, ab, x1 - 1.0, ct, 'w')
    W = (x1 - x0) / 2
    for gy in range(h):
        for gx in range(w):
            px, py = gx + 0.5, gy + 0.5
            if at <= py <= ab and abs(px - cx) <= W + 0.6 + (py - at) * 0.3:
                cv.g[gy][gx] = 'ab'[int((px - cx) / (2 * W / n) + 100) % 2]
    for k in range(n):
        x = cx - W + (k + 0.5) * 2 * W / n
        disc(cv, x, ab, W / n + 0.1, 'ab'[int((x - cx) / (2 * W / n) + 100) % 2])
    box(cv, x0, ct, x1, gtop + 0.5, 'c')
    box(cv, x0, ct + 2.6, x1, ct + 3.2, 'k', only='c')
    for i, x in enumerate((cx - 5.0, cx, cx + 5.0)):
        if goods == 'flowers':
            box(cv, x - 1.4, ct - 2.2, x + 1.4, ct, 'k')
            seg(cv, x, ct - 2.2, x, ct - 3.6, 'v', 0.5)
            for dx, dy, c in ((-1.3, -4.2, 'f'), (1.3, -4.0, 'o'), (0, -5.4, 'of'[i % 2])):
                disc(cv, x + dx, ct + dy, 1.1, c)
        else:
            box(cv, x - 2.0, ct - 1.6, x + 2.0, ct, 'k')
            oval(cv, x, ct - 1.8, 1.9, 1.3, 'fvo'[i])
    if goods == 'harvest':
        for x in (w * 0.28, w * 0.72):
            oval(cv, x, h - 2.0, 2.0, 1.3, 'y')
            seg(cv, x, h - 3.2, x + 0.6, h - 4.0, 'v', 0.5)
    if top == 'sign':
        rbox(cv, cx - 3.6, at - 4.4, cx + 3.6, at - 0.4, 0.6, 'k')
        disc(cv, cx, at - 2.4, 1.05, 'f')
    elif top == 'bunting':
        path(cv, [(x0 + 0.5, at - 0.2), (cx, at - 1.6), (x1 - 0.5, at - 0.2)], 'p', 0.45)
        for k in range(5):
            x = x0 + 2.4 + k * (x1 - x0 - 4.8) / 4
            y = at - 1.6 + abs(x - cx) / W * 1.4
            poly(cv, [(x - 0.9, y - 0.4), (x + 0.9, y - 0.4), (x, y - 2.4)], 'fo'[k % 2])
    if r.random() < 0.5:
        _mirror(cv)
    return cv, [sky(), role('w', 'back', 'Back cloth', BLUE), role('a', 'stripe', 'Awning stripes', PINK),
                role('f', 'apples', 'Apples and flowers', PINK), role('o', 'plums', 'Plums and blooms', PINK),
                role('b', 'stripe2', 'Other stripes', GREEN), role('v', 'greens', 'Greens', GREEN),
                role('g', 'ground', 'Ground', GREEN), role('p', 'posts', 'Posts', BROWN),
                role('c', 'counter', 'Counter', BROWN), role('k', 'crates', 'Crates and sign', BROWN),
                role('u', 'sun', 'Sun', BROWN), role('y', 'pumpkins', 'Pumpkins', BROWN)], ['places', 'market']


def scarecrow(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    field = r.choice(('wheat', 'corn', 'pumpkins'))
    dusk = r.random() < 0.35
    hat = r.choice(('brim', 'pointed'))
    gtop = h * 0.75
    sx = int(cx) + 0.5
    if dusk:
        disc(cv, w * r.choice((0.2, 0.8)), gtop, s * 0.2, 'u')
    else:
        disc(cv, w * r.choice((0.12, 0.88)), h * 0.08, s * 0.08, 'u')
    box(cv, 0, gtop, w, h, 'g')
    if field == 'wheat':
        for x in range(0, w, 2):
            if abs(x + 0.5 - sx) > 1.5:
                seg(cv, x + 0.5, gtop + 1.5, x + 0.5, gtop - 2.0, 'k', 0.5)
                oval(cv, x + 0.5, gtop - 2.6, 0.6, 1.3, 'y')
    elif field == 'corn':
        for x in (1.5, 5.0, 17.0, 20.5):
            seg(cv, x, gtop + 2, x, gtop - 7.0, 'k', 0.5)
            for j, d in enumerate((-1, 1, -1)):
                lens(cv, x, gtop - 1.5 - j * 2.0, x + d * 2.2, gtop - 2.8 - j * 2.0, 1.0, 'k')
            oval(cv, x + 0.9, gtop - 4.2, 0.6, 1.3, 'y')
    else:
        for x, y in ((3.0, h * 0.86), (8.0, h * 0.94), (15.0, h * 0.88), (19.5, h * 0.95)):
            oval(cv, x, y, 2.0, 1.3, 'y')
            seg(cv, x, y - 1.2, x + 0.6, y - 2.2, 'k', 0.5)
    box(cv, sx - 0.5, h * 0.4, sx + 0.5, gtop + 1.5, 'p')
    sy = h * 0.43
    droop = r.uniform(0, 1.4)
    for d in (-1, 1):
        ex = sx + d * w * 0.37
        poly(cv, [(sx + d * 2.4, sy), (ex, sy + droop), (ex, sy + droop + 2.3), (sx + d * 2.4, sy + 2.5)], 'c')
        poly(cv, [(ex, sy + droop + 0.2), (ex + d * 1.8, sy + droop - 0.7), (ex + d * 2.1, sy + droop + 1.2),
                  (ex + d * 1.7, sy + droop + 3.0), (ex, sy + droop + 2.1)], 'y')
    poly(cv, [(sx - 2.6, sy), (sx + 2.6, sy), (sx + 3.0, h * 0.66), (sx - 3.0, h * 0.66)], 'c')
    for k in range(5):
        x = sx - 2.6 + k * 1.3
        poly(cv, [(x - 0.7, h * 0.66 - 0.2), (x + 0.7, h * 0.66 - 0.2), (x, h * 0.66 + 1.7)], 'y')
    box(cv, sx - 2.0, h * 0.5, sx - 0.6, h * 0.55, 'q')
    box(cv, sx + 0.7, h * 0.58, sx + 2.1, h * 0.63, 'q')
    disc(cv, sx, h * 0.33, 2.4, 'f')
    for d in (-1, 1):
        cv.put(int(sx + d * 1.0), int(h * 0.31), 'e')
    box(cv, sx - 1.5, h * 0.355, sx + 1.5, h * 0.355 + 0.7, 'e')
    box(cv, sx - 4.0, h * 0.245, sx + 4.0, h * 0.245 + 0.9, 'h')
    if hat == 'brim':
        rbox(cv, sx - 2.3, h * 0.14, sx + 2.3, h * 0.25, 0.6, 'h')
    else:
        poly(cv, [(sx - 2.7, h * 0.25), (sx + 1.6, h * 0.035), (sx + 2.7, h * 0.25)], 'h')
    crows = r.choice(('arm', 'flying', 'both'))
    if crows in ('arm', 'both'):
        _sprite(cv, sx + w * 0.22, sy + droop * 0.6 - 3.6, CROW, {'X': 'b'})
    if crows in ('flying', 'both'):
        for x, y in ((w * 0.2, h * 0.12), (w * 0.33, h * 0.2)):
            path(cv, [(x - 1.4, y - 0.8), (x, y + 0.3), (x + 1.4, y - 0.8)], 'b', 0.5)
    if r.random() < 0.5:
        _mirror(cv)
    return cv, [sky(), role('q', 'patch', 'Patches', BLUE), role('c', 'shirt', 'Shirt', PINK), role('e', 'face', 'Face', PINK),
                role('p', 'pole', 'Pole', BROWN), role('b', 'crow', 'Crows', BROWN),
                role('y', 'straw', 'Straw and crops', BROWN), role('f', 'head', 'Sack head', BROWN),
                role('u', 'sun', 'Sun', BROWN), role('g', 'field', 'Field', GREEN),
                role('k', 'stalks', 'Stalks and leaves', GREEN), role('h', 'hat', 'Hat', GREEN)], ['places', 'farm']


def beehive(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    kind = r.choice(('skep', 'box', 'wild'))
    gtop = h - ground_rows(h, 0.14)
    hx = int(cx) + r.choice((-1, 0, 1))
    disc(cv, w * r.choice((0.12, 0.88)), h * 0.08, s * 0.08, 'u')
    back = r.choice(('hills', 'fence', 'none'))
    if back == 'hills':
        hills(cv, 'm', gtop - 2.5, 1.5, w * 0.9, r.uniform(0, 6))
    elif back == 'fence':
        for x in range(0, w, 2):
            poly(cv, [(x, gtop - 4.0), (x + 0.5, gtop - 4.6), (x + 1, gtop - 4.0), (x + 1, gtop), (x, gtop)], 'n')
        box(cv, 0, gtop - 3.0, w, gtop - 2.4, 'n')
    box(cv, 0, gtop, w, h, 'g')
    for x in (w * 0.08, w * 0.24, w * 0.76, w * 0.92):
        if r.random() < 0.7:
            seg(cv, x, gtop + 0.5, x, gtop - 3.0, 'v', 0.5)
            lens(cv, x, gtop - 1.0, x + 1.6, gtop - 2.0, 0.9, 'v')
            disc(cv, x, gtop - 3.6, 1.1, 'f')
    sy = gtop - 3.0
    if kind == 'skep':
        box(cv, hx - 6.5, sy, hx + 6.5, sy + 0.9, 't')
        for d in (-1, 1):
            box(cv, hx + d * 5.0 - 0.5, sy, hx + d * 5.0 + 0.5, gtop + 0.5, 't')
        for k, half in enumerate((6.2, 6.0, 5.4, 4.2)):
            y1 = sy - k * 3.0
            rbox(cv, hx - half, y1 - 3.0, hx + half, y1, 1.0, 'k')
            box(cv, hx - half + 0.6, y1 - 0.9, hx + half - 0.6, y1 - 0.1, 'l')
        oval(cv, hx, sy - 12.0, 2.6, 1.3, 'k')
        rbox(cv, hx - 1.3, sy - 2.8, hx + 1.3, sy - 0.8, 0.8, 'd')
        top = sy - 13.0
    elif kind == 'box':
        for d in (-1, 1):
            box(cv, hx + d * 4.0 - 0.5, sy, hx + d * 4.0 + 0.5, gtop + 0.5, 't')
        for k in range(3):
            y1 = sy - k * 3.6
            box(cv, hx - 5.0, y1 - 3.6, hx + 5.0, y1, 'k')
            box(cv, hx - 5.0, y1 - 0.8, hx + 5.0, y1, 'l')
        poly(cv, [(hx - 6.4, sy - 10.6), (hx, sy - 13.6), (hx + 6.4, sy - 10.6)], 'l')
        box(cv, hx - 2.5, sy - 1.9, hx + 2.5, sy - 1.1, 'd')
        top = sy - 13.6
    else:
        path(cv, [(-1, h * 0.1), (w * 0.4, h * 0.14), (w * 0.72, h * 0.12)], 't', 0.8)
        if r.random() < 0.5:
            box(cv, -1, h * 0.08, w * 0.08, gtop + 0.5, 't')
        for x, y in ((w * 0.72, h * 0.1), (w * 0.84, h * 0.14), (w * 0.62, h * 0.06)):
            disc(cv, x, y, 1.8, 'v')
        widths = r.choice(((2.2, 3.8, 4.8, 5.2, 5.0, 4.2, 3.0, 1.6), (2.4, 4.2, 5.4, 5.6, 4.8, 3.2)))
        for k, half in enumerate(widths):
            y0 = h * 0.15 + k * 2.0
            rbox(cv, hx - half, y0, hx + half, y0 + 2.0, 0.8, 'k')
            if 0 < k < len(widths) - 1:
                box(cv, hx - half + 0.6, y0 + 1.2, hx + half - 0.6, y0 + 1.9, 'l')
        rbox(cv, hx - 1.2, h * 0.15 + len(widths) * 2.0 - 4.8, hx + 1.2, h * 0.15 + len(widths) * 2.0 - 3.0, 0.6, 'd')
        top = h * 0.15
    scatter(cv, 'e', 's', 7, r, sep=2, area=(hx - 9, max(0, top - 3), hx + 9, sy))
    if r.random() < 0.5:
        _mirror(cv)
    return cv, [sky(), role('d', 'hole', 'Entrance', BLUE), role('k', 'hive', 'Hive', BROWN),
                role('t', 'stand', 'Stand and branch', BROWN), role('e', 'bees', 'Bees', BROWN), role('u', 'sun', 'Sun', BROWN),
                role('n', 'fence', 'Fence', BROWN), role('l', 'coils', 'Coils and lid', PINK),
                role('f', 'flowers', 'Flowers', PINK), role('g', 'grass', 'Grass', GREEN),
                role('v', 'leaves', 'Leaves and stems', GREEN), role('m', 'hills', 'Hills', GREEN)], ['places', 'garden']


def garden_gate(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.16)
    arch = r.choice(('roses', 'vine', 'none'))
    fence = r.choice(('hedge', 'pickets'))
    ajar = r.random() < 0.35
    night = r.random() < 0.25
    L = int(cx) - 6
    pt, yt = h * 0.46, h * 0.53
    hills(cv, 'g', gtop - 3.0, 0.5, w * 1.2, r.uniform(0, 6))
    if fence == 'hedge':
        for a, b in ((-1, L - 0.2), (L + 13.2, w + 1)):
            rbox(cv, a, h * 0.5, b, gtop + 0.5, 2.0, 'h')
        scatter(cv, 'f', 'h', 5, r, sep=2)
        if r.random() < 0.6:
            _tree(cv, w * r.choice((0.1, 0.9)), h * 0.52, s * 0.12, 'h', 'n')
    else:
        for x in list(range(0, L - 1, 2)) + list(range(L + 14, w, 2)):
            poly(cv, [(x, h * 0.63), (x + 0.5, h * 0.6), (x + 1, h * 0.63), (x + 1, gtop + 0.5), (x, gtop + 0.5)], 'k')
        for y in (h * 0.68, h * 0.8):
            box(cv, 0, y, L, y + 0.8, 'q')
            box(cv, L + 13, y, w, y + 0.8, 'q')
        scatter(cv, 'f', 'g', 5, r, sep=2, area=(0, gtop + 1, w - 1, h - 1))
    for x in (L, L + 11):
        box(cv, x, pt, x + 2, gtop + 0.5, 'k')
        box(cv, x - 0.3, pt - 0.9, x + 2.3, pt, 'q')
    if ajar:
        for x in range(L + 2, L + 7, 2):
            poly(cv, [(x, yt + 1.5), (x + 0.5, yt + 0.9), (x + 1, yt + 1.5), (x + 1, gtop + 1.4), (x, gtop + 1.4)], 'k')
        for y in (h * 0.62, h * 0.8):
            box(cv, L + 2, y, L + 7, y + 0.8, 'q')
    else:
        for x in range(L + 2, L + 11, 2):
            top = yt + 1.2 * ((x + 0.5 - (L + 6.5)) / 4.5) ** 2
            poly(cv, [(x, top + 1), (x + 0.5, top), (x + 1, top + 1), (x + 1, gtop + 0.5), (x, gtop + 0.5)], 'k')
        for y in (h * 0.62, h * 0.8):
            box(cv, L + 2, y, L + 11, y + 0.8, 'q')
        seg(cv, L + 2.5, h * 0.8, L + 10.5, h * 0.62 + 0.8, 'q', 0.5)
    for k, (y, rx) in enumerate(((gtop + 1.0, 1.8), (gtop + 2.7, 2.4))):
        oval(cv, L + 6.5 + (k - 0.5) * 0.8, y, rx, 0.75, 'p')
    if arch != 'none':
        ax, ay = L + 6.5, pt - 0.4
        for gy in range(h):
            for gx in range(w):
                d = math.hypot(gx + 0.5 - ax, gy + 0.5 - ay)
                if gy + 0.5 <= ay and 4.6 < d <= 6.4:
                    cv.g[gy][gx] = 'a'
        if arch == 'roses':
            for a in range(10, 180, 28):
                t = math.radians(a)
                disc(cv, ax + math.cos(t) * 5.5, ay - math.sin(t) * 5.5, 1.0, 'r')
        else:
            for a in range(20, 180, 40):
                t = math.radians(a)
                lens(cv, ax + math.cos(t) * 5.5, ay - math.sin(t) * 5.5, ax + math.cos(t) * 7.5, ay - math.sin(t) * 7.0, 1.0, 'a')
    else:
        x = L + 12 if r.random() < 0.5 else L + 1
        seg(cv, x, pt - 0.9, x, pt - 3.0, 'q', 0.5)
        rbox(cv, x - 1.1, pt - 5.0, x + 1.1, pt - 2.8, 0.5, 'r')
    sx = w * r.choice((0.12, 0.88))
    if night:
        _moon(cv, sx, h * 0.08, s * 0.09, 'u', 's')
        scatter(cv, 'x', 's', 6, r, sep=3, area=(0, 0, w - 1, h * 0.35))
    else:
        disc(cv, sx, h * 0.08, s * 0.075, 'u')
    if r.random() < 0.5:
        _mirror(cv)
    return cv, [sky(), role('p', 'path', 'Stepping stones', BLUE), role('k', 'gate', 'Gate and posts', BROWN),
                role('q', 'rails', 'Rails and caps', BROWN), role('n', 'trunk', 'Tree trunk', BROWN), _light(night),
                role('h', 'hedge', 'Hedges and tree', GREEN), role('a', 'arch', 'Leafy arch', GREEN),
                role('g', 'lawn', 'Lawn', GREEN), role('r', 'roses', 'Roses and lantern', PINK),
                role('f', 'flowers', 'Flowers', PINK), role('x', 'stars', 'Stars', BROWN)], ['places', 'garden']


def sunflower(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    kind = r.choice(('one', 'three', 'fence'))
    gtop = h - ground_rows(h, 0.1)
    if kind == 'one':
        heads = [(cx + r.uniform(-1.5, 1.5), h * 0.29, w * 0.3)]
    elif kind == 'three':
        heads = [(w * 0.2, h * 0.46, w * 0.16), (w * 0.52, h * 0.22, w * 0.2), (w * 0.81, h * 0.38, w * 0.17)]
    else:
        heads = [(w * 0.32, h * 0.27, w * 0.24), (w * 0.78, h * 0.47, w * 0.15)]
    if kind != 'three':
        for x, y in ((w * 0.82, h * 0.1), (w * 0.12, h * 0.16)) if kind == 'one' else ((w * 0.78, h * 0.12),):
            cloud(cv, x, y, 1.0, 'k')
    if kind == 'fence':
        for x in range(0, w, 2):
            poly(cv, [(x, gtop - 5.0), (x + 0.5, gtop - 5.6), (x + 1, gtop - 5.0), (x + 1, gtop), (x, gtop)], 'n')
        box(cv, 0, gtop - 3.8, w, gtop - 3.2, 'n')
    for x, y, R in heads:
        bend = r.uniform(-1.2, 1.2)
        seg(cv, x, y, x + bend, gtop + 0.5, 't', 0.55 + R * 0.03)
        for side, f in ((-1, 0.5), (1, 0.7)):
            ly = y + (gtop - y) * f
            lens(cv, x + bend * f, ly, x + bend * f + side * R * 0.95, ly - R * 0.35, R * 0.42, 'l')
    box(cv, 0, gtop, w, h, 'g')
    for x, y, R in heads:
        star(cv, x, y, R, 'p', ri=R * 0.72, points=14 if R > 5 else 10, rot=r.uniform(0, 30))
        disc(cv, x, y, R * 0.52, 'c')
        dots(cv, 'd', 'c', 2, 2, area=(x - R, y - R, x + R, y + R))
    scatter(cv, 'o', 'g', 5, r, sep=3, area=(0, gtop + 1, w - 1, h - 1))
    if kind == 'three':
        scatter(cv, 'e', 's', 5, r, sep=3, area=(0, 0, w - 1, h * 0.5))
    if r.random() < 0.5:
        _mirror(cv)
    return cv, [sky(), role('k', 'cloud', 'Clouds', BLUE), role('p', 'petals', 'Petals', BROWN),
                role('d', 'seeds', 'Seeds', BROWN), role('n', 'fence', 'Fence', BROWN), role('e', 'bees', 'Bees', BROWN),
                role('o', 'flowers', 'Little flowers', PINK), role('c', 'center', 'Seed head', PINK),
                role('t', 'stem', 'Stems', GREEN), role('l', 'leaves', 'Leaves', GREEN),
                role('g', 'grass', 'Grass', GREEN)], ['places', 'garden']


def lily_pond(w, h, r):
    cv, s, cx, big = start(w, h, 'w')
    by = h * r.uniform(0.2, 0.26)
    box(cv, 0, 0, w, by, 'k')
    box(cv, 0, by - 1.6, w, by + 0.4, 'b')
    far = r.choice(('trees', 'willow', 'hills'))
    if far == 'trees':
        for x in r.sample([w * k / 7 for k in range(1, 7)], 3):
            disc(cv, x, by - 2.4, 1.9, 't')
    elif far == 'willow':
        wx = w * r.uniform(0.25, 0.75)
        disc(cv, wx, by - 3.2, 3.2, 't')
        for dx in (-2.6, -1.2, 1.2, 2.6):
            seg(cv, wx + dx, by - 3.0, wx + dx * 1.2, by + 1.5, 't', 0.5)
    else:
        hills(cv, 't', by - 1.2, 1.2, w * 0.7, r.uniform(0, 6), only='k')
    disc(cv, w * r.choice((0.12, 0.88)), h * 0.06, s * 0.07, 'u')
    pads = []
    rows = [(by + 3.0, 3), (by + 7.5, 2), (by + 12.5, 3), (by + 17.5, 2)]
    for y, n in rows:
        if y > h - 1:
            continue
        for k in range(n):
            x = (k + 0.5) * w / n + r.uniform(-1.5, 1.5)
            rx = 1.5 + (y - by) * 0.11
            pads.append((x, y, rx, rx * (0.38 + 0.25 * (y - by) / (h - by))))
    for x, y, rx, ry in pads:
        oval(cv, x, y, rx, ry, 'p')
        d = r.choice((-1, 1))
        poly(cv, [(x, y), (x + d * rx * 1.1, y - ry * 0.5), (x + d * rx * 1.1, y + 0.1)], 'w')
    for x, y, rx, ry in r.sample(pads, 3):
        fy = y - ry * 0.4
        poly(cv, [(x - 1.6, fy), (x - 1.3, fy - 1.7), (x - 0.5, fy - 0.8), (x, fy - 2.3), (x + 0.5, fy - 0.8),
                  (x + 1.3, fy - 1.7),
                  (x + 1.6, fy), (x + 0.8, fy + 0.6), (x - 0.8, fy + 0.6)], 'f')
        cv.put(int(x), int(fy - 0.4), 'y')
    cxr = w * 0.08
    for k, dx in enumerate((0, 1.6, 3.0)):
        x = cxr + dx
        top = h * (0.5 + k * 0.05)
        seg(cv, x, h, x, top, 'r', 0.5)
        oval(cv, x, top + 1.0, 0.55, 1.5, 'c')
    lens(cv, cxr + 0.5, h, cxr + 4.5, h * 0.62, 1.4, 'r')
    if r.random() < 0.6:
        dx0, dy0 = w * r.uniform(0.45, 0.8), h * r.uniform(0.38, 0.55)
        seg(cv, dx0 - 2.0, dy0, dx0 + 1.5, dy0, 'q', 0.5)
        for d in (-1, 1):
            lens(cv, dx0, dy0, dx0 - 0.6, dy0 + d * 2.0, 1.0, 'q')
    if r.random() < 0.5:
        _mirror(cv)
    return cv, [('w', 'pond', 'Pond', BLUE, True), role('k', 'sky', 'Sky', BLUE), role('p', 'pads', 'Lily pads', GREEN),
                role('b', 'bank', 'Far bank', GREEN), role('t', 'trees', 'Trees', GREEN), role('r', 'reeds', 'Reeds', GREEN),
                role('f', 'lily', 'Water lilies', PINK), role('q', 'dragonfly', 'Dragonfly', PINK),
                role('c', 'cattails', 'Cattails', BROWN), role('y', 'centers', 'Lily centers', BROWN),
                role('u', 'sun', 'Sun', BROWN)], ['places', 'pond']


def cherry_blossom(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.16)
    shape = r.choice(('round', 'wide', 'weeping'))
    prop = r.choice(('bench', 'blanket', 'river'))
    night = r.random() < 0.25
    tx = cx + r.uniform(-2.5, 2.5)
    hills(cv, 'm', h * 0.66, 1.4, w * 0.9, r.uniform(0, 6))
    box(cv, 0, gtop, w, h, 'g')
    if prop == 'river':
        poly(cv, [(w * 0.55, gtop), (w * 0.75, gtop), (w + 1, h - 1.0), (w + 1, h), (w * 0.62, h)], 'k')
    path(cv, [(tx, gtop + 0.5), (tx - 0.4, h * 0.6), (tx + 0.3, h * 0.45)], 't', 0.95)
    for d in (-1, 1):
        path(cv, [(tx, h * 0.52), (tx + d * w * 0.18, h * 0.38), (tx + d * w * 0.28, h * 0.34)], 't', 0.55)
    if shape == 'round':
        blobs = [(0, 0.27, 0.26), (-0.2, 0.34, 0.17), (0.2, 0.34, 0.17), (-0.1, 0.19, 0.17), (0.12, 0.2, 0.17)]
    elif shape == 'wide':
        blobs = [(0, 0.28, 0.22), (-0.28, 0.34, 0.15), (0.28, 0.34, 0.15), (-0.14, 0.22, 0.15), (0.14, 0.22, 0.15)]
    else:
        blobs = [(0, 0.25, 0.24), (-0.19, 0.31, 0.17), (0.19, 0.31, 0.17)]
    for dx, y, rr in blobs:
        disc(cv, tx + dx * w, h * y, s * rr, 'c')
    if shape == 'weeping':
        for dx in (-0.36, -0.25, 0.25, 0.36):
            seg(cv, tx + dx * w, h * 0.32, tx + dx * w * 1.05, h * 0.56, 'c', 0.6)
    scatter(cv, 'q', 'c', 7, r, sep=3)
    for d in (-1, 1):
        seg(cv, tx + d * 1.0, h * 0.36, tx + d * w * 0.18, h * 0.28, 't', 0.45)
    if prop == 'bench':
        bx = tx + (w * 0.26 if tx < cx else -w * 0.26)
        box(cv, bx - 3.0, gtop - 2.2, bx + 3.0, gtop - 1.4, 'b')
        box(cv, bx - 3.0, gtop - 4.2, bx + 3.0, gtop - 3.4, 'b')
        for d in (-1, 1):
            box(cv, bx + d * 2.4 - 0.4, gtop - 4.2, bx + d * 2.4 + 0.4, gtop + 0.5, 'b')
    elif prop == 'blanket':
        bx = tx + (w * 0.25 if tx < cx else -w * 0.25)
        poly(cv, [(bx - 3.5, gtop + 1.0), (bx + 3.0, gtop + 0.6), (bx + 4.0, gtop + 2.6), (bx - 3.0, gtop + 3.0)], 'k')
        rbox(cv, bx - 0.8, gtop - 0.6, bx + 1.4, gtop + 1.2, 0.4, 'b')
    scatter(cv, 'f', 's', 6, r, sep=3, area=(0, h * 0.35, w - 1, gtop - 1))
    scatter(cv, 'f', 'g', 4, r, sep=3, area=(0, gtop + 1, w - 1, h - 1))
    sx = w * r.choice((0.1, 0.9))
    if night:
        _moon(cv, sx, h * 0.07, s * 0.09, 'u', 's')
    else:
        disc(cv, sx, h * 0.07, s * 0.075, 'u')
    if r.random() < 0.5:
        _mirror(cv)
    return cv, [sky(), role('k', 'water', 'Blanket and river', BLUE), role('c', 'blossom', 'Blossom', PINK),
                role('q', 'blossom2', 'Pale blossom', PINK), role('f', 'petals', 'Falling petals', PINK), _light(night),
                role('t', 'trunk', 'Trunk', BROWN), role('b', 'bench', 'Bench and basket', BROWN),
                role('g', 'grass', 'Grass', GREEN), role('m', 'hills', 'Hills', GREEN)], ['places', 'spring']


def bonsai(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    style = r.choice(('upright', 'slant', 'cascade'))
    deco = r.choice(('scroll', 'window', 'none'))
    fy = h * 0.88
    box(cv, 0, fy, w, h, 'f')
    tt = h * 0.76
    box(cv, w * 0.1, tt, w * 0.9, tt + 1.2, 'k')
    for x in (w * 0.15, w * 0.85):
        box(cv, x - 0.6, tt, x + 0.6, fy + 0.5, 'k')
    if deco == 'scroll':
        x = w * (0.82 if style != 'cascade' else 0.18)
        rbox(cv, x - 2.2, h * 0.06, x + 2.2, h * 0.5, 0.4, 'w')
        for y in (h * 0.05, h * 0.51):
            box(cv, x - 2.8, y - 0.4, x + 2.8, y + 0.5, 'k')
        disc(cv, x, h * 0.22, 1.3, 'm')
    elif deco == 'window':
        x = w * (0.8 if style != 'cascade' else 0.2)
        disc(cv, x, h * 0.22, 3.6, 'w')
        for d in (-1.2, 1.2):
            box(cv, x + d - 0.4, h * 0.22 - 3.6, x + d + 0.4, h * 0.22 + 3.6, 'k', only='w')
        box(cv, x - 3.6, h * 0.22 - 0.4, x + 3.6, h * 0.22 + 0.4, 'k', only='w')
    pt = h * 0.66
    poly(cv, [(w * 0.2, pt), (w * 0.8, pt), (w * 0.72, tt), (w * 0.28, tt)], 'o')
    box(cv, w * 0.18, pt - 0.4, w * 0.82, pt + 0.6, 'o')
    oval(cv, cx, pt - 0.4, w * 0.26, 1.0, 'm')
    if style == 'upright':
        trunk = [(cx, pt), (cx - 2.0, h * 0.54), (cx + 1.5, h * 0.42), (cx - 0.5, h * 0.28)]
        pads = [(cx - 4.5, h * 0.47, 3.6), (cx + 4.6, h * 0.36, 3.6), (cx - 0.5, h * 0.22, 4.2)]
    elif style == 'slant':
        trunk = [(cx - 2.5, pt), (cx + 0.5, h * 0.5), (cx + 5.0, h * 0.34)]
        pads = [(cx + 5.8, h * 0.27, 3.6), (cx + 0.2, h * 0.4, 3.4), (cx - 4.0, h * 0.48, 3.0)]
    else:
        trunk = [(cx - 3.0, pt), (cx - 2.0, h * 0.5), (cx + 3.0, h * 0.43), (cx + 7.0, h * 0.58), (cx + 8.0, h * 0.74)]
        pads = [(cx - 3.0, h * 0.38, 3.4), (cx + 3.5, h * 0.36, 3.0), (cx + 8.0, h * 0.73, 2.4)]
    path(cv, trunk, 't', 1.0)
    for x, y, rx in pads:
        seg(cv, trunk[-2][0], trunk[-2][1], x, y, 't', 0.5)
    for x, y, rx in pads:
        oval(cv, x, y, rx, 1.5, 'l')
        oval(cv, x - rx * 0.35, y - 0.9, rx * 0.55, 1.0, 'l')
        oval(cv, x + rx * 0.35, y - 0.7, rx * 0.5, 0.9, 'l')
    if r.random() < 0.35:
        scatter(cv, 'q', 'l', 6, r, sep=2)
    if r.random() < 0.5:
        _mirror(cv)
    return cv, [('b', 'wall', 'Wall', BLUE, True), role('o', 'pot', 'Pot', PINK), role('w', 'paper', 'Scroll and window', PINK),
                role('f', 'floor', 'Floor mat', PINK), role('q', 'blossom', 'Blossom', PINK),
                role('k', 'table', 'Table and rods', BROWN), role('t', 'trunk', 'Trunk', BROWN),
                role('l', 'leaves', 'Leaf pads', GREEN), role('m', 'moss', 'Moss', GREEN)], ['places', 'garden']


# ---- Wild places and far away ----

def mountain(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    kind = r.choice(('one', 'twin', 'range'))
    time = r.choice(('day', 'dawn', 'night'))
    base = h * 0.7
    if time == 'dawn':
        disc(cv, w * r.choice((0.2, 0.8)), h * 0.26, s * 0.13, 'u')
    elif time == 'day':
        disc(cv, w * r.choice((0.12, 0.88)), h * 0.08, s * 0.08, 'u')
    else:
        _moon(cv, w * r.choice((0.12, 0.88)), h * 0.09, s * 0.1, 'u', 's')
    pts, x, k = [(-1, base + 1)], -1.0, 0
    far = 0.1 if kind == 'one' else 0.0
    while x < w + 2:
        pts.append((x, h * (0.52 + far if k % 2 == 0 else 0.6 + far) + r.uniform(-0.8, 0.8)))
        x += r.uniform(2.6, 4.0)
        k += 1
    poly(cv, pts + [(w + 2, base + 1)], 'n')
    if kind == 'one':
        peaks = [(cx + r.uniform(-1.5, 1.5), h * 0.08, w * 0.52)]
    elif kind == 'twin':
        peaks = [(w * 0.3, h * 0.25, w * 0.4), (w * 0.68, h * 0.12, w * 0.46)]
    else:
        peaks = [(w * 0.16, h * 0.36, w * 0.28), (w * 0.84, h * 0.32, w * 0.28), (cx, h * 0.12, w * 0.42)]
    for x, top, hb in peaks:
        dh = base - top
        jag = [(x - hb, base + 1), (x - hb * 0.55, top + dh * 0.45), (x - hb * 0.42, top + dh * 0.5), (x, top),
               (x + hb * 0.33, top + dh * 0.38), (x + hb * 0.48, top + dh * 0.46), (x + hb, base + 1)]
        mask = canvas(w, h, '.')
        poly(mask, jag, '#')
        for gy in range(h):
            for gx in range(w):
                if mask.g[gy][gx] != '#':
                    continue
                px, py = gx + 0.5, gy + 0.5
                ridge = x + (py - top) / dh * hb * 0.15
                if py < top + dh * 0.36 + 1.0 * math.sin(px * 1.7):
                    cv.g[gy][gx] = 'x'
                else:
                    cv.g[gy][gx] = 'r' if px > ridge else 'm'
    for x, top, hb in peaks[-1:]:
        if r.random() < 0.5:
            cloud(cv, x + r.choice((-1, 1)) * hb * 0.5, top + (base - top) * 0.5, 1.2, 'c')
    x = -0.5
    while x < w + 1:
        _pine(cv, x, base + 2.4, r.uniform(4.0, 6.0), 1.3, 'p')
        x += r.uniform(1.8, 2.6)
    box(cv, 0, base + 2.0, w, h, 'g')
    front = r.choice(('lake', 'river', 'cabin'))
    if front == 'lake':
        oval(cv, w * r.uniform(0.3, 0.7), h * 0.9, w * 0.3, h * 0.06, 'w')
    elif front == 'river':
        path(cv, [(w * 0.4, base + 2.2), (w * 0.55, h * 0.82), (w * 0.4, h * 0.92), (w * 0.5, h)], 'w', 0.8)
    else:
        hx = w * r.choice((0.25, 0.75))
        box(cv, hx - 2.2, h * 0.86, hx + 2.2, h * 0.96, 'm')
        poly(cv, [(hx - 3.0, h * 0.86 + 0.5), (hx, h * 0.78), (hx + 3.0, h * 0.86 + 0.5)], 'n')
        box(cv, hx - 0.6, h * 0.9, hx + 0.6, h * 0.96, 'w')
        scatter(cv, 'x', 'g', 4, r, sep=3, area=(0, h * 0.8, w - 1, h - 1))
    if time == 'night':
        scatter(cv, 'z', 's', 7, r, sep=3, area=(0, 0, w - 1, h * 0.4))
    if r.random() < 0.5:
        _mirror(cv)
    return cv, [sky(), role('w', 'lake', 'Lake and river', BLUE), role('m', 'rock', 'Sunny rock', BROWN),
                role('r', 'shade', 'Shaded rock', BROWN), _light(time == 'night'), role('z', 'stars', 'Stars', BROWN),
                role('x', 'snow', 'Snow', PINK), role('n', 'far', 'Far peaks', PINK), role('g', 'meadow', 'Meadow', GREEN),
                role('p', 'forest', 'Forest', GREEN), role('c', 'cloud', 'Cloud', GREEN)], ['places', 'mountains']


def cave(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    night = r.random() < 0.35
    inside = r.choice(('crystals', 'eyes', 'bats', 'stream'))
    gtop = h - ground_rows(h, 0.12)
    mx = int(cx) + r.choice((-2, 0, 2))
    sx = w * (0.14 if mx > cx else 0.86 if mx < cx else r.choice((0.14, 0.86)))
    if night:
        _moon(cv, sx, h * 0.08, s * 0.09, 'u', 's')
    else:
        disc(cv, sx, h * 0.08, s * 0.075, 'u')
    poly(cv, [(-1, gtop + 1), (-1, h * r.uniform(0.52, 0.6)), (w * 0.14, h * 0.44), (w * 0.3, h * r.uniform(0.34, 0.39)),
              (w * 0.52, h * 0.33), (w * 0.72, h * r.uniform(0.35, 0.4)), (w * 0.88, h * 0.46), (w + 1, h * 0.56),
              (w + 1, gtop + 1)], 'k')
    for x0 in r.sample([w * k / 8 for k in range(1, 8)], 3):
        for gy in range(h):
            if cv.g[gy][int(x0)] == 'k':
                oval(cv, x0, gy + 0.6, 1.6, 0.8, 'f')
                break
    for k in range(3):
        x0 = w * r.uniform(0.05, 0.7)
        y0 = h * r.uniform(0.44, 0.5)
        path(cv, [(x0, y0), (x0 + 1.5, y0 + 1.0), (x0 + 3.0, y0 + 0.6)], 'l', 0.45)
    top = h * 0.55
    rbox(cv, mx - 4.6, top, mx + 4.6, gtop + 0.5, 4.6, 'd')
    for x in (mx - 3.0, mx - 1.0, mx + 1.0, mx + 3.0):
        yy = top + 4.6 - math.sqrt(max(0.0, 4.6 ** 2 - (x - mx) ** 2))
        poly(cv, [(x - 0.8, yy - 0.6), (x + 0.8, yy - 0.6), (x, yy + 1.9)], 'k')
    for x in (mx - 3.6, mx + 3.4):
        poly(cv, [(x - 0.9, gtop + 0.5), (x + 0.9, gtop + 0.5), (x, gtop - 1.8)], 'k')
    if inside == 'crystals':
        for x, ht in ((mx - 1.3, 3.2), (mx + 0.9, 4.4), (mx + 2.2, 2.6)):
            poly(cv, [(x, gtop - ht), (x + 1.0, gtop - ht * 0.45), (x, gtop + 0.4), (x - 1.0, gtop - ht * 0.45)], 'c')
    elif inside == 'stream':
        for x, ht in ((mx - 2.2, 2.6), (mx + 2.0, 3.4)):
            poly(cv, [(x, gtop - ht), (x + 0.9, gtop - ht * 0.45), (x, gtop + 0.4), (x - 0.9, gtop - ht * 0.45)], 'c')
    elif inside == 'eyes':
        for ex, ey in ((mx - 1.8, top + 3.6), (mx + 1.5, top + 5.8)):
            for d in (-1, 1):
                box(cv, ex + d * 0.9 - 0.4, ey, ex + d * 0.9 + 0.4, ey + 0.9, 'c')
    else:
        for k, (dx, dy) in enumerate(((-8.0, -6.0), (2.0, -9.0))):
            _sprite(cv, mx + dx, top + dy, BAT, {'X': 'b'})
        _sprite(cv, mx - 3, top + 2.2, BAT, {'X': 'c'})
    box(cv, 0, gtop, w, h, 'g')
    for d in (-1, 1):
        if r.random() < 0.7:
            for k in range(3):
                lens(cv, mx + d * 6.0, gtop + 0.5, mx + d * (6.0 + 1.2 * k), gtop - 3.0 + k * 0.8, 1.1, 'f')
    if inside == 'stream':
        path(cv, [(mx, gtop - 0.5), (mx + 1.5, gtop + 1.2), (mx - 0.5, gtop + 2.4), (mx + 1.0, h)], 'w', 0.9)
    else:
        poly(cv, [(mx - 2.0, gtop), (mx + 2.0, gtop), (mx + 3.5, h), (mx - 3.5, h)], 'p')
    if night:
        scatter(cv, 'x', 's', 6, r, sep=3, area=(0, 0, w - 1, h * 0.3))
    if r.random() < 0.5:
        _mirror(cv)
    return cv, [sky(), role('d', 'mouth', 'Cave mouth', BLUE), role('w', 'stream', 'Stream', BLUE), role('k', 'rock', 'Rock', BROWN),
                role('l', 'cracks', 'Cracks', BROWN), role('p', 'path', 'Path', BROWN), _light(night),
                role('c', 'glow', 'Crystals and eyes', PINK), role('b', 'bats', 'Bats', PINK),
                role('g', 'ground', 'Ground', GREEN), role('f', 'moss', 'Moss and ferns', GREEN),
                role('x', 'stars', 'Stars', BROWN)], ['places', 'adventure']


def _paper_lantern(cv, x, y, rx, ry, body, rib, cap):
    """A round paper lantern: its body with ribs, a cap above and below and a tassel."""
    oval(cv, x, y, rx, ry, body)
    for f in (-0.5, 0.0, 0.5):
        box(cv, 0, y + f * ry - 0.3, cv.w, y + f * ry + 0.3, rib, only=body)
    box(cv, x - rx * 0.5, y - ry - 0.7, x + rx * 0.5, y - ry + 0.5, cap)
    box(cv, x - rx * 0.5, y + ry - 0.5, x + rx * 0.5, y + ry + 0.7, cap)
    seg(cv, x, y + ry + 0.7, x, y + ry + 2.8, cap, 0.5)


def lantern(w, h, r):
    cv, s, cx, big = start(w, h, 'n')
    kind = r.choice(('one', 'string', 'eave'))
    mx = w * r.choice((0.14, 0.86))
    for x, wd, y in ((w * r.uniform(0.15, 0.3), w * 0.26, h * 0.84), (w * r.uniform(0.65, 0.85), w * 0.3, h * 0.79)):
        _curved_roof(cv, x, y - 2.6, y, wd * 0.35, wd, 'h', lift=1.2)
        box(cv, x - wd + 1.6, y, x + wd - 1.6, h, 'h')
        for d in (-1, 1):
            box(cv, x + d * wd * 0.35 - 0.6, y + 1.0, x + d * wd * 0.35 + 0.6, y + 2.6, 'm')
    box(cv, 0, h - 1.4, w, h, 'h')
    if kind == 'one':
        box(cv, -1, -0.5, w + 1, 1.3, 'c')
        seg(cv, int(cx) + 0.5, 1.3, int(cx) + 0.5, h * 0.18, 'c', 0.5)
        _paper_lantern(cv, int(cx) + 0.5, h * 0.42, w * 0.28, h * 0.22, 'l', 'r', 'k')
        for x, y in ((w * 0.13, h * 0.3), (w * 0.87, h * 0.38)):
            seg(cv, int(x) + 0.5, 1.3, int(x) + 0.5, y - 3.0, 'c', 0.5)
            _paper_lantern(cv, int(x) + 0.5, y, 2.2, 2.6, 'p', 'r', 'k')
        _moon(cv, mx, h * 0.62, s * 0.08, 'm', 'n')
    elif kind == 'string':
        def cord(x):
            return h * 0.12 + (x / w) * h * 0.06 + h * 0.14 * math.sin(math.pi * max(0.0, min(1.0, x / w)))
        path(cv, [(x, cord(x)) for x in [-1 + k * (w + 2) / 10 for k in range(11)]], 'c', 0.45)
        for k, x in enumerate((3.5, 8.5, 13.5, 18.5)):
            y = cord(x)
            seg(cv, x, y, x, y + 1.4, 'c', 0.5)
            _paper_lantern(cv, x, y + 4.6, 2.3, 2.8, 'lp'[k % 2], 'r', 'k')
        _moon(cv, mx, h * 0.62, s * 0.08, 'm', 'n')
    else:
        box(cv, -1, -0.5, w + 1, 1.6, 'c')
        for x in range(0, w + 1, 2):
            disc(cv, x, 1.6, 1.0, 'c')
        for k, x in enumerate((w * 0.28, w * 0.72)):
            px = int(x) + 0.5
            seg(cv, px, 2.4, px, h * 0.16, 'c', 0.5)
            _paper_lantern(cv, px, h * 0.34, w * 0.17, h * 0.15, 'lp'[k], 'r', 'k')
        _moon(cv, cx, h * 0.62, s * 0.09, 'm', 'n')
    scatter(cv, 'x', 'n', 8, r, sep=3, area=(0, 2, w - 1, h * 0.7))
    if r.random() < 0.5:
        _mirror(cv)
    return cv, [('n', 'night', 'Night sky', BLUE, True), role('l', 'lantern', 'Lantern', PINK),
                role('p', 'lantern2', 'Other lanterns', PINK), role('k', 'cap', 'Caps and tassels', BROWN),
                role('r', 'rib', 'Ribs', BROWN), role('m', 'moon', 'Moon and windows', BROWN),
                role('x', 'stars', 'Stars', BROWN), role('c', 'cord', 'Beam and cords', GREEN),
                role('h', 'roofs', 'Rooftops', GREEN)], ['places', 'festival']


def snow_globe(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    inner = r.choice(('tree', 'house', 'both'))
    deco = r.choice(('dots', 'window', 'garland'))
    ty = h * 0.8
    gx, gy, gr = cx, h * 0.4, w * 0.37
    if deco == 'dots':
        dots(cv, 'd', 'b', 4, 3, area=(0, 0, w - 1, ty - 1))
    elif deco == 'window':
        wx = w * r.choice((0.1, 0.9))
        rbox(cv, wx - 2.4, h * 0.03, wx + 2.4, h * 0.22, 0.6, 'n')
        box(cv, wx - 0.4, h * 0.03, wx + 0.4, h * 0.22, 'b')
    else:
        path(cv, [(-1, h * 0.03), (cx, h * 0.12), (w + 1, h * 0.03)], 'd', 0.5)
        for x in (w * 0.15, w * 0.32, w * 0.68, w * 0.85):
            disc(cv, x, h * 0.03 + 0.9 * h * 0.09 * (1 - abs(x - cx) / cx) + 1.4, 0.9, 'e')
    box(cv, 0, ty, w, h, 't')
    disc(cv, gx, gy, gr, 'q')
    for yy in range(h):
        for xx in range(w):
            px, py = xx + 0.5, yy + 0.5
            if cv.g[yy][xx] == 'q' and ((px - gx) / (gr * 0.95)) ** 2 + ((py - gy - gr * 0.75) / (gr * 0.42)) ** 2 <= 1.0:
                cv.g[yy][xx] = 'm'
    ground = gy + gr * 0.36
    if inner in ('tree', 'both'):
        tx = gx - (2.6 if inner == 'both' else 0)
        sc = 0.75 if inner == 'both' else 1.0
        for k in range(3):
            yb = ground - k * 2.2 * sc
            hw = (3.2 - k * 0.8) * sc
            poly(cv, [(tx - hw, yb), (tx, yb - 3.0 * sc), (tx + hw, yb)], 'f')
        star(cv, tx, ground - 7.2 * sc, 1.2, 'y', ri=0.6)
    if inner in ('house', 'both'):
        hx = gx + (3.0 if inner == 'both' else 0)
        hw = 2.6 if inner == 'both' else 3.4
        box(cv, hx - hw, ground - hw * 1.1, hx + hw, ground + 0.4, 'h')
        poly(cv, [(hx - hw - 0.8, ground - hw * 1.1 + 0.4), (hx, ground - hw * 2.1),
                  (hx + hw + 0.8, ground - hw * 1.1 + 0.4)], 'r')
    scatter(cv, 'x', 'q', 10, r, sep=2)
    for yy in range(h):
        for xx in range(w):
            d = math.hypot(xx + 0.5 - gx, yy + 0.5 - gy)
            a = math.degrees(math.atan2(yy + 0.5 - gy, xx + 0.5 - gx)) % 360
            if cv.g[yy][xx] == 'q' and gr * 0.68 < d <= gr * 0.86 and 200 <= a <= 250:
                cv.g[yy][xx] = 'o'
    poly(cv, [(gx - gr * 0.72, gy + gr * 0.78), (gx + gr * 0.72, gy + gr * 0.78), (gx + gr * 0.92, ty + 1.6),
              (gx - gr * 0.92, ty + 1.6)], 'k')
    box(cv, gx - gr, gy + gr + 1.0, gx + gr, gy + gr + 2.0, 'e', only='k')
    if r.random() < 0.5:
        _mirror(cv)
    return cv, [('b', 'wall', 'Wall', PINK, True), role('m', 'snow', 'Snow', PINK), role('x', 'flakes', 'Snowflakes', PINK),
                role('q', 'water', 'Globe', BLUE), role('o', 'shine', 'Shine', BLUE), role('t', 'table', 'Table', BLUE),
                role('n', 'window', 'Window', BLUE), role('k', 'base', 'Base', BROWN), role('h', 'house', 'House', BROWN),
                role('e', 'band', 'Band and baubles', BROWN), role('y', 'star', 'Star', BROWN),
                role('f', 'tree', 'Fir tree', GREEN), role('r', 'roof', 'Roof', GREEN),
                role('d', 'decor', 'Wall dots and garland', GREEN)], ['places', 'winter']


def aquarium(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    side = r.choice((-1, 1))
    kind = r.choice(('tank', 'tall', 'bowl'))
    fy = h * 0.9
    box(cv, 0, fy, w, h, 'l')
    if kind != 'tall':
        px = cx + side * w * 0.3
        rbox(cv, px - 2.4, h * 0.04, px + 2.4, h * 0.19, 0.4, 'z')
        box(cv, px - 1.6, h * 0.08, px + 1.6, h * 0.16, 'c')
        disc(cv, px + 0.6, h * 0.1, 0.9, 'e')
    if kind == 'bowl':
        by, R = h * 0.47, w * 0.35
        cut = by - R * 0.72
        box(cv, w * 0.14, h * 0.75, w * 0.86, h * 0.75 + 1.2, 'c')
        for x in (w * 0.2, w * 0.8):
            box(cv, x - 0.6, h * 0.75, x + 0.6, fy + 0.5, 'c')
        for yy in range(h):
            for xx in range(w):
                px, py = xx + 0.5, yy + 0.5
                if math.hypot(px - cx, py - by) <= R and cut <= py <= h * 0.75:
                    cv.g[yy][xx] = 'w'
        box(cv, cx - R * 0.72 - 0.4, cut - 0.9, cx + R * 0.72 + 0.4, cut + 0.1, 'k')
        x0, x1, y0, y1 = cx - R, cx + R, cut, h * 0.75
    else:
        x0, x1, y0 = (w * 0.07, w * 0.93, h * 0.3) if kind == 'tank' else (w * 0.2, w * 0.8, h * 0.13)
        y1 = h * 0.68
        box(cv, x0, y0, x1, y1, 'w')
        box(cv, x0 - 0.6, y0 - 1.2, x1 + 0.6, y0, 'k')
        box(cv, x0 + 1.0, y1, x1 - 1.0, fy + 0.5, 'c')
        box(cv, cx - 0.4, y1 + 1.2, cx + 0.4, fy - 0.8, 'l')
    hills(cv, 'g', y1 - 1.6, 0.5, 4.0, r.uniform(0, 6), only='w')
    for k in range(3):
        x = x0 + (x1 - x0) * (0.18 + k * 0.32) + r.uniform(-1, 1)
        ht = (y1 - y0) * r.uniform(0.4, 0.65)
        path(cv, [(x, y1 - 1.0), (x + 0.8, y1 - ht * 0.35), (x - 0.6, y1 - ht * 0.7), (x + 0.4, y1 - ht)], 'p', 0.55)
    if kind == 'tank' and r.random() < 0.6:
        hx = x0 + (x1 - x0) * r.choice((0.3, 0.7))
        box(cv, hx - 1.6, y1 - 5.6, hx + 1.6, y1 - 1.2, 'h')
        for x in (hx - 1.6, hx, hx + 1.2):
            box(cv, x, y1 - 6.6, x + 0.6, y1 - 5.6, 'h')
        box(cv, hx - 0.4, y1 - 3.0, hx + 0.4, y1 - 1.2, 'w')
    n = 3 if kind == 'tank' else 2
    for k in range(n):
        fx = x0 + (x1 - x0) * (0.12 + k * 0.7 / max(1, n - 1)) + r.uniform(-1, 1)
        fyy = y0 + (y1 - y0) * r.uniform(0.15, 0.5)
        _sprite(cv, fx - 3, fyy - 2, FISH, {'X': 'fe'[k % 2]}, r.random() < 0.5, on='wfe')
    scatter(cv, 'o', 'w', 6, r, sep=2, area=(x0, y0, x1, y0 + (y1 - y0) * 0.6))
    if r.random() < 0.5:
        _mirror(cv)
    return cv, [('b', 'wall', 'Wall', PINK, True), role('f', 'fish', 'Pink fish', PINK), role('o', 'bubbles', 'Bubbles', PINK),
                role('h', 'castle', 'Castle', PINK), role('w', 'water', 'Water', BLUE),
                role('z', 'picture', 'Picture frame', BLUE), role('e', 'goldfish', 'Goldfish', BROWN),
                role('g', 'gravel', 'Gravel', BROWN), role('l', 'floor', 'Floor and door line', BROWN),
                role('c', 'cabinet', 'Cabinet, table and picture', GREEN), role('k', 'lid', 'Lid and rim', GREEN),
                role('p', 'plants', 'Water plants', GREEN)], ['places', 'home']


def pier(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    time = r.choice(('day', 'sunset', 'night'))
    hz = h * r.uniform(0.5, 0.56)
    sunx = w * r.uniform(0.55, 0.8)
    if time == 'sunset':
        disc(cv, sunx, hz, s * 0.18, 'u')
    elif time == 'day':
        disc(cv, w * 0.84, h * 0.1, s * 0.08, 'u')
    else:
        _moon(cv, sunx, h * 0.12, s * 0.1, 'u', 's')
    if r.random() < 0.6:
        ix = w * r.uniform(0.6, 0.9)
        for yy in range(h):
            for xx in range(w):
                if yy + 0.5 <= hz + 0.3 and ((xx + 0.5 - ix) / (w * 0.17)) ** 2 + ((yy + 0.5 - hz - 0.3) / 1.8) ** 2 <= 1.0:
                    cv.g[yy][xx] = 'h'
    box(cv, 0, hz, w, h, 'w')
    if time != 'day':
        for k in range(4):
            y = hz + 1.5 + k * 1.6
            box(cv, sunx - 2.2 + k * 0.4, y, sunx + 1.6 - k * 0.4, y + 0.6, 'z')
    poly(cv, [(-1, h * 0.66), (w * 0.2, h * 0.72), (w * 0.4, h + 1), (-1, h + 1)], 'b')
    dy, end = h * 0.64, w * r.uniform(0.72, 0.84)
    box(cv, -1, dy, end, dy + 1.4, 'd')
    x = w * 0.2
    while x < end:
        box(cv, x - 0.5, dy + 1.4, x + 0.5, h * 0.88, 'p')
        x += 3.0
    for x in [k * 3.0 + 1.5 for k in range(int(end // 3.0) + 1) if k * 3.0 + 1.5 < end]:
        box(cv, x - 0.5, dy - 2.2, x + 0.5, dy, 'p')
    box(cv, -1, dy - 2.6, end, dy - 1.8, 'p')
    extra = r.choice(('lamp', 'hut', 'boat'))
    if extra == 'lamp':
        seg(cv, end - 1.0, dy, end - 1.0, dy - 6.5, 'p', 0.5)
        disc(cv, end - 1.0, dy - 7.2, 1.0, 'l')
    elif extra == 'hut':
        box(cv, end - 4.5, dy - 5.0, end - 0.5, dy, 'e')
        poly(cv, [(end - 5.3, dy - 4.8), (end - 2.5, dy - 7.6), (end + 0.3, dy - 4.8)], 'l')
        box(cv, end - 3.2, dy - 3.4, end - 1.8, dy - 1.8, 'w')
    if extra == 'boat' or r.random() < 0.4:
        bx = min(w - 3.5, end + 2.0)
        poly(cv, [(bx - 3.0, h * 0.78), (bx + 3.0, h * 0.78), (bx + 2.0, h * 0.84), (bx - 2.0, h * 0.84)], 'k')
        seg(cv, bx, h * 0.78, bx, h * 0.58, 'p', 0.5)
        poly(cv, [(bx + 0.5, h * 0.59), (bx + 0.5, h * 0.76), (bx + 3.0, h * 0.76)], 'k')
    for gx, gy in r.sample([(w * 0.2, h * 0.14), (w * 0.38, h * 0.22), (w * 0.6, h * 0.1), (w * 0.3, h * 0.32)], 2):
        path(cv, [(gx - 1.4, gy - 0.7), (gx, gy + 0.3), (gx + 1.4, gy - 0.7)], 'q', 0.5)
    if time == 'night':
        scatter(cv, 'x', 's', 6, r, sep=3, area=(0, 0, w - 1, hz - 2))
    if r.random() < 0.5:
        _mirror(cv)
    return cv, [sky(), role('w', 'sea', 'Sea', BLUE), role('d', 'deck', 'Deck', BROWN),
                role('p', 'posts', 'Posts and rails', BROWN), role('q', 'gulls', 'Gulls', BROWN),
                role('e', 'hut', 'Hut', BROWN), role('x', 'stars', 'Stars', BROWN),
                role('u', 'moon' if time == 'night' else 'sun', 'Moon' if time == 'night' else 'Sun', PINK),
                role('z', 'glints', 'Glints on the water', PINK), role('l', 'lamp', 'Lamp and hut roof', PINK),
                role('k', 'boat', 'Boat', PINK), role('b', 'beach', 'Beach', GREEN),
                role('h', 'island', 'Island', GREEN)], ['places', 'sea']


def space_station(w, h, r):
    cv, s, cx, big = start(w, h, 'n')
    kind = r.choice(('wings', 'wheel', 'tower'))
    ex, er = cx + r.choice((-1, 1)) * w * r.uniform(0.15, 0.3), w * 0.62
    ey = h + er - h * 0.22
    disc(cv, ex, ey, er, 'o')
    for k in range(4):
        bx, byy, br = ex + r.uniform(-er * 0.7, er * 0.7), ey - er + r.uniform(1.5, 4.5), r.uniform(1.5, 2.6)
        for yy in range(h):
            for xx in range(w):
                if cv.g[yy][xx] == 'o' and math.hypot(xx + 0.5 - bx, yy + 0.5 - byy) <= br:
                    cv.g[yy][xx] = 'e'
    disc(cv, w * r.choice((0.12, 0.88)), h * 0.08, s * 0.07, 'u')
    sy = h * 0.4

    def panel(x0, y0, x1, y1):
        box(cv, x0, y0, x1, y1, 'p')
        for yy in range(int(y0) + 1, int(y1), 2):
            box(cv, x0, yy, x1, yy + 0.9, 't', only='p')

    if kind == 'wings':
        box(cv, w * 0.04, sy - 0.5, w * 0.96, sy + 0.5, 't')
        for x in (w * 0.14, w * 0.32, w * 0.68, w * 0.86):
            panel(x - 1.6, sy - 6.5, x + 1.6, sy - 1.0)
            panel(x - 1.6, sy + 1.0, x + 1.6, sy + 6.5)
            box(cv, x - 0.5, sy - 6.5, x + 0.5, sy + 6.5, 't')
        rbox(cv, cx - 4.2, sy - 1.6, cx + 4.2, sy + 1.6, 1.2, 'm')
        rbox(cv, cx - 1.6, sy - 5.5, cx + 1.6, sy + 4.5, 1.2, 'm')
        spots = [(cx - 2.5, sy), (cx + 2.5, sy), (cx, sy - 3.5), (cx, sy + 2.5), (cx, sy - 1.0)]
    elif kind == 'wheel':
        ring(cv, cx, sy, 8.0, 6.0, 'm')
        for a in (0, 60, 120):
            t = math.radians(a)
            seg(cv, cx - math.cos(t) * 6.2, sy - math.sin(t) * 6.2, cx + math.cos(t) * 6.2, sy + math.sin(t) * 6.2, 't', 0.5)
        disc(cv, cx, sy, 2.2, 'm')
        panel(cx - 3.0, sy - 12.0, cx + 3.0, sy - 9.0)
        box(cv, cx - 0.5, sy - 9.0, cx + 0.5, sy - 7.8, 't')
        spots = [(cx + math.cos(math.radians(a)) * 7.0, sy + math.sin(math.radians(a)) * 7.0) for a in range(15, 360, 45)]
    else:
        for k, y in enumerate((h * 0.1, h * 0.28, h * 0.46)):
            rbox(cv, cx - 1.8, y, cx + 1.8, y + 4.4, 1.2, 'm')
            if k < 2:
                box(cv, cx - 0.6, y + 4.2, cx + 0.6, y + 5.2, 't')
        for y in (h * 0.2, h * 0.55):
            box(cv, cx - 9.0, y - 0.5, cx + 9.0, y + 0.5, 't')
            panel(cx - 9.0, y - 3.0, cx - 5.0, y - 0.5)
            panel(cx - 9.0, y + 0.5, cx - 5.0, y + 3.0)
            panel(cx + 5.0, y - 3.0, cx + 9.0, y - 0.5)
            panel(cx + 5.0, y + 0.5, cx + 9.0, y + 3.0)
        spots = [(cx, h * 0.1 + 2.2), (cx, h * 0.28 + 2.2), (cx, h * 0.46 + 2.2)]
    for x, y in spots:
        cv.put(int(x), int(y), 'w')
    cap = r.choice(((cx + w * 0.3, sy + h * 0.2), (cx - w * 0.3, sy - h * 0.25)))
    poly(cv, [(cap[0] - 1.2, cap[1] - 1.0), (cap[0] + 1.2, cap[1] - 1.0), (cap[0] + 0.6, cap[1] + 1.4),
              (cap[0] - 0.6, cap[1] + 1.4)], 'k')
    scatter(cv, 'x', 'n', 9, r, sep=3)
    if r.random() < 0.5:
        _mirror(cv)
    return cv, [('n', 'space', 'Space', PINK, True), role('p', 'panels', 'Solar panels', BLUE),
                role('o', 'ocean', 'Ocean', BLUE), role('w', 'windows', 'Windows', BLUE), role('u', 'moon', 'Moon', BLUE),
                role('t', 'truss', 'Truss and panel lines', BROWN), role('k', 'capsule', 'Capsule', BROWN),
                role('x', 'stars', 'Stars', BROWN), role('m', 'modules', 'Modules', GREEN),
                role('e', 'land', 'Land', GREEN)], ['places', 'space']


def planet(w, h, r):
    cv, s, cx, big = start(w, h, 'n')
    px, py = cx + r.uniform(-1.5, 1.5), h * r.uniform(0.42, 0.52)
    pr = w * r.uniform(0.23, 0.25)
    tilt = math.radians(r.uniform(10, 22)) * r.choice((-1, 1))
    A, B, ct, st = pr * 2.1, pr * 0.62, math.cos(tilt), math.sin(tilt)

    def frame(xx, yy):
        dx, dy = xx + 0.5 - px, yy + 0.5 - py
        u, v = dx * ct + dy * st, -dx * st + dy * ct
        return math.hypot(u / A, v / B), v

    def rings(front):
        for yy in range(h):
            for xx in range(w):
                d, v = frame(xx, yy)
                if 0.6 < d <= 1.0 and (v >= 0) == front:
                    cv.g[yy][xx] = 'r' if d > 0.8 else 'o'

    rings(False)
    disc(cv, px, py, pr, 'p')
    for yy in range(h):
        for xx in range(w):
            if cv.g[yy][xx] != 'p':
                continue
            if math.hypot(xx + 0.5 - (px - pr * 0.45), yy + 0.5 - (py - pr * 0.45)) > pr * 1.25:
                cv.g[yy][xx] = 'b'
            v = frame(xx, yy)[1]
            if any(abs(v - f * pr) < 0.7 for f in (-0.45, 0.35)):
                cv.g[yy][xx] = 'q'
    rings(True)
    spots = [(w * 0.14, h * 0.1), (w * 0.86, h * 0.12), (w * 0.14, h * 0.88), (w * 0.86, h * 0.9)]
    for x, y in r.sample(spots, 2):
        disc(cv, x, y, r.uniform(1.4, 2.0), 'm')
    if r.random() < 0.5:
        x0, y0 = w * r.uniform(0.2, 0.8), h * r.choice((0.08, 0.92))
        path(cv, [(x0 - 3.5, y0 - 1.0), (x0, y0)], 'z', 0.45)
        disc(cv, x0 + 0.5, y0 + 0.2, 0.9, 'z')
    scatter(cv, 'x', 'n', 9, r, sep=3)
    if r.random() < 0.5:
        _mirror(cv)
    return cv, [('n', 'space', 'Space', BLUE, True), role('p', 'planet', 'Planet', BROWN),
                role('b', 'shade', 'Shaded side', BROWN), role('x', 'stars', 'Stars', BROWN),
                role('z', 'comet', 'Shooting star', BROWN), role('q', 'bands', 'Bands', PINK),
                role('m', 'moons', 'Moons', PINK), role('r', 'ring', 'Outer ring', GREEN),
                role('o', 'ring2', 'Inner ring', GREEN)], ['places', 'space']


DAILY_PLACES = [pagoda, pyramid, volcano, waterfall, stone_bridge, clock_tower, skyscraper, carousel, circus_tent,
                greenhouse, wishing_well, fountain, gazebo, log_cabin, campfire, observatory, market_stall, scarecrow,
                beehive, garden_gate, sunflower, lily_pond, cherry_blossom, bonsai, mountain, cave, lantern, snow_globe,
                aquarium, pier, space_station, planet]

"""More subjects for the levels: food (more_subjects.py). Fruits, vegetables, treats and dishes in the style of
food_subjects.py: each stands on a table, a plate, a board or the ground, and the shapes that make it (a fruit against
its backdrop, seeds against flesh, a filling against its shell) come from different color groups, so they stay apart
under any mapping. The pictures of a subject differ in what they show (whole, cut open or a group), their props and
their side, and the big boards (from 300 cells) add detail where one more level of nesting fits.
"""
import math

from picture_kit import (BLUE, BROWN, GREEN, PINK, box, cells, cloud, disc, dots, heart, lens, oval, path, poly, rbox,
                         ring, role, scatter, seg, sky, star, start)


# ---- Helpers ----

def mirror(cv):
    """Flips the drawing left to right."""
    cv.g = [row[::-1] for row in cv.g]


def curve(p0, p1, bend, n=20):
    """Points of a quadratic curve from p0 to p1 whose middle lies `bend` cells to the right of the straight way."""
    (x0, y0), (x1, y1) = p0, p1
    L = math.hypot(x1 - x0, y1 - y0) or 1.0
    mx, my = (x0 + x1) / 2 - (y1 - y0) / L * bend * 2, (y0 + y1) / 2 + (x1 - x0) / L * bend * 2
    return [((1 - t) ** 2 * x0 + 2 * (1 - t) * t * mx + t * t * x1, (1 - t) ** 2 * y0 + 2 * (1 - t) * t * my + t * t * y1)
            for t in (k / n for k in range(n + 1))]


def tube(cv, pts, rad, c, only=None):
    """A thick line through pts, `rad` cells in radius (or rad(t) at the share t of the way); `only` limits it to the
    cells of those roles."""
    f = rad if callable(rad) else (lambda t: rad)
    steps = [math.hypot(b[0] - a[0], b[1] - a[1]) for a, b in zip(pts, pts[1:])]
    total = sum(steps) or 1.0
    marks, run = [], 0.0
    for (a, b), L in zip(zip(pts, pts[1:]), steps):
        n = max(1, int(L * 4))
        marks += [(a[0] + (b[0] - a[0]) * k / n, a[1] + (b[1] - a[1]) * k / n, f((run + L * k / n) / total)) for k in range(n)]
        run += L
    marks.append((pts[-1][0], pts[-1][1], f(1.0)))
    for x, y, px, py in cells(cv):
        if (only is None or cv.g[y][x] in only) and any((px - a) ** 2 + (py - b) ** 2 <= q * q for a, b, q in marks):
            cv.g[y][x] = c


def tilted(cv, cx, cy, rx, ry, ang, c, only=None):
    """An oval turned by `ang` degrees; `only` limits it to the cells of those roles."""
    ca, sa = math.cos(math.radians(ang)), math.sin(math.radians(ang))
    for x, y, px, py in cells(cv):
        u, v = (px - cx) * ca + (py - cy) * sa, -(px - cx) * sa + (py - cy) * ca
        if (u / rx) ** 2 + (v / ry) ** 2 <= 1.0 and (only is None or cv.g[y][x] in only):
            cv.g[y][x] = c


def bowl(cv, x, rim, bottom, rx, c, band=None):
    """A round bowl from its rim at row `rim` down to `bottom`, rx cells to each side, with a band of role `band` along
    its rim."""
    ry = bottom - rim - 0.8
    for xx, yy, px, py in cells(cv):
        if rim <= py and ((px - x) / rx) ** 2 + ((py - rim) / ry) ** 2 <= 1.0:
            cv.g[yy][xx] = c
    box(cv, x - rx * 0.32, bottom - 1.2, x + rx * 0.32, bottom, c)
    if band:
        box(cv, x - rx, rim, x + rx, rim + 0.9, band, only=c)


def cut_half(cv, x, y, rr, skin, face, ratio=0.5):
    """A half fruit cut face up: its skin below row y, its cut face an oval `ratio` as tall as wide over it (the face
    meets the backdrop, so what lies on it nests one level only)."""
    for xx, yy, px, py in cells(cv):
        if py >= y and math.hypot(px - x, py - y) <= rr:
            cv.g[yy][xx] = skin
    oval(cv, x, y, rr, rr * ratio, face)


def bean(cv, x, y, a, b, ang, c, wide=0.0, bend=0.0, only=None):
    """An egg or bean shape reaching `a` cells each way from (x, y) along `ang` degrees and `b` across, `wide` times
    wider at its start than at its end, its middle bent `bend` cells to its left (a mango's back); `only` limits it to
    the cells of those roles."""
    ca, sa = math.cos(math.radians(ang)), math.sin(math.radians(ang))
    for xx, yy, px, py in cells(cv):
        u, v = (px - x) * ca + (py - y) * sa, -(px - x) * sa + (py - y) * ca
        if abs(u) <= a and (only is None or cv.g[yy][xx] in only):
            t = u / a
            if abs(v + bend * (1 - t * t)) <= b * math.sqrt(1 - t * t) * (1 - wide * t):
                cv.g[yy][xx] = c


# The settings: a window over a table, a dotted wall with a shelf of jars over a table, a picnic blanket before a hill
# under a cloud.
SCENES = (
    ('wall', 'Wall', 'panes', 'Window panes', 'frame', 'Window frame', 'table', 'Table', 'cloth_dots', 'Cloth dots'),
    ('wall', 'Wall', 'wall_dots', 'Wall dots and jars', 'shelf', 'Shelf', 'table', 'Table', 'cloth_dots', 'Cloth dots'),
    ('sky', 'Sky', 'cloud', 'Cloud', 'hill', 'Hill', 'blanket', 'Picnic blanket', 'blanket_dots', 'Blanket dots'),
)


def scene(cv, r, w, h, kind, ty):
    """The setting of `kind` (SCENES), its table or blanket from row ty down: roles b (background), o (window panes,
    wall dots and jars, or a cloud), v (window frame, shelf or hill) at the top left, where the subjects keep their
    tall things away, t (table or blanket) and d (a row of dots on a table four rows deep or more, below the plates
    and clear of the rows that keep the table whole)."""
    if kind == 0:
        x0, y0 = w * r.uniform(0.03, 0.08), h * r.uniform(0.04, 0.08)
        x1, y1 = x0 + max(5.0, w * 0.3), y0 + max(5.0, h * 0.22)
        rbox(cv, x0, y0, x1, y1, 0.6, 'v')
        box(cv, x0 + 1, y0 + 1, x1 - 1, y1 - 1, 'o')
        box(cv, (x0 + x1) / 2 - 0.5, y0, (x0 + x1) / 2 + 0.5, y1, 'v')
        box(cv, x0, (y0 + y1) / 2 - 0.5, x1, (y0 + y1) / 2 + 0.5, 'v')
    elif kind == 1:
        step = r.choice((4, 5))
        dots(cv, 'o', 'b', step, step - 1, area=(0, 0, w - 1, ty - 3), offset=r.randrange(step))
        y = h * r.uniform(0.18, 0.22)
        box(cv, 0, y, w * 0.32, y + 0.9, 'v')
        poly(cv, [(w * 0.14, y + 0.9), (w * 0.26, y + 0.9), (w * 0.26, y + 2.8)], 'v')
        rbox(cv, w * 0.06, y - 3.2, w * 0.2, y - 0.1, 0.6, 'o')
    else:
        cloud(cv, w * r.uniform(0.2, 0.34), h * r.uniform(0.07, 0.11), min(w, h) * 0.07 + 0.5, 'o')
        oval(cv, w * r.uniform(-0.05, 0.1), ty + 1.0, w * r.uniform(0.4, 0.5), h * r.uniform(0.16, 0.2), 'v')
    box(cv, 0, ty, w, h, 't')
    top = int(math.ceil(ty - 0.5))
    if h - top >= 4:
        step, off = (4, 4, 3)[kind], r.randrange(4)
        spots = [x for x in range(1, w - 1) if (x + off) % step == 0]
        if len(spots) >= 5:
            for x in spots:
                cv.g[top + 2][x] = 'd'


def scene_roles(kind, groups):
    """The setting's roles by character, in the color groups of b, o, v, t and d."""
    n = SCENES[kind]
    return {c: (c, n[2 * k], n[2 * k + 1], g, k == 0) for k, (c, g) in enumerate(zip('bovtd', groups))}


def gap(cv, x, y, rr, only, c='b'):
    """Clears a disc of the roles in `only` back to the background, so a shape drawn in it stands apart."""
    tilted(cv, x, y, rr, rr, 0, c, only=only)


# ---- Fruit ----

def one_orange(cv, x, y, rr, big, leaves):
    """An orange: round, its peel dimpled, with a stalk at the top and up to two leaves."""
    disc(cv, x, y, rr, 'a')
    dots(cv, 'e', 'a', 3, 2, area=(x - rr, y - rr, x + rr, y + rr), offset=int(x) % 3)
    if big:
        tilted(cv, x - rr * 0.45, y - rr * 0.45, rr * 0.3, rr * 0.13, -45, 'h')
    seg(cv, x, y - rr * 0.9, x + 0.3, y - rr - 0.8, 'k', 0.5)
    if leaves:
        lens(cv, x + 0.3, y - rr - 0.5, x + rr * 1.15, y - rr * 1.4 - 0.4, rr * 0.42 + 0.3, 'l')
    if leaves > 1:
        lens(cv, x, y - rr - 0.5, x - rr * 0.75, y - rr * 1.55 - 0.5, rr * 0.34 + 0.3, 'l')


def half_wheel(cv, x, y, rr):
    """Half an orange slice standing on its cut edge at row y: peel, pith and segments between the pith's spokes, all
    reaching the cut edge, so nothing nests inside the slice."""
    x, y = int(x) + 0.5, round(y)
    for xx, yy, px, py in cells(cv):
        d = math.hypot(px - x, py - y)
        if py <= y and d <= rr:
            cv.g[yy][xx] = 'w' if rr - 1.9 < d <= rr - 1.0 else 'a'
    for a in (45, 90, 135):
        t = math.radians(a)
        seg(cv, x, y, x + math.cos(t) * (rr - 1.4), y - math.sin(t) * (rr - 1.4), 'w', 0.42 if a == 90 else 0.75)


def wheel(cv, x, y, rr):
    """The cut face of an orange (big boards: it nests one level deeper than the half slice): peel, the pith's ring and
    spokes, and eight segments."""
    x, y = int(x) + 0.5, int(y) + 0.5
    disc(cv, x, y, rr, 'a')
    ring(cv, x, y, rr - 0.9, rr - 1.8, 'w')
    for k in range(8):
        a = math.radians(k * 45)
        seg(cv, x, y, x + math.cos(a) * (rr - 1.4), y + math.sin(a) * (rr - 1.4), 'w', 0.42 if k % 2 == 0 else 0.75)


def orange(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    mode, kind = r.randrange(3), r.randrange(3)
    ty = h * r.uniform(0.84, 0.88)
    scene(cv, r, w, h, kind, ty)
    if mode == 0:  # one orange with two leaves
        oval(cv, cx, ty, w * 0.42, 1.8, 'p')
        rr = s * 0.31
        one_orange(cv, cx + w * 0.03, ty - rr - 0.4, rr, big, 2)
    elif mode == 1:  # a whole orange behind a cut one
        oval(cv, cx, ty, w * 0.42, 1.8, 'p')
        rr = s * 0.22
        one_orange(cv, cx + w * 0.22, ty - rr - h * 0.16, rr, big, 1)
        if big:
            hr = s * 0.32
            hx, hy = cx - w * 0.12, ty - hr - 0.2
            gap(cv, hx, hy, hr + 0.9, 'aeklh')
            disc(cv, hx + 1.3, hy - 0.2, hr, 'a')
            wheel(cv, hx, hy, hr)
        else:
            hr = s * 0.33
            hx = cx - w * 0.1
            gap(cv, hx, ty - 0.6, hr + 0.9, 'aeklh')
            half_wheel(cv, hx, ty - 0.6, hr)
    else:  # three oranges in a bowl
        rr = s * 0.2
        by = ty - h * 0.15
        for k, (dx, dy) in enumerate(((-0.19, 0.0), (0.19, 0.0), (0.0, -1.0))):
            x, y = cx + dx * w, by - rr * 0.3 + dy * rr * 1.55
            if k:
                gap(cv, x, y, rr + 0.8, 'aeklh')
            one_orange(cv, x, y, rr, big, 1 if k == 2 else 0)
        bowl(cv, cx, by, ty + 0.4, w * 0.4, 'p')
    if r.random() < 0.5:
        mirror(cv)
    S = scene_roles(kind, (BLUE, BLUE, PINK, PINK, PINK))
    return cv, [S['b'], role('a', 'orange', 'Oranges', BROWN), role('e', 'dimples', 'Dimples', BROWN), role('l', 'leaf', 'Leaves', GREEN),
                role('k', 'stalk', 'Stalks', GREEN), S['t'], S['v'], role('w', 'pith', 'Pith', PINK), role('p', 'plate', 'Plate and bowl', BLUE),
                role('h', 'shine', 'Shine', BLUE), S['o'], S['d']], ['food', 'fruit']


def one_plum(cv, x, y, rr, side, big, leaf=True):
    """A plum: a round oval with a soft crease down one side from its stalk, and a leaf."""
    oval(cv, x, y, rr * 0.92, rr, 'a')
    disc(cv, x + side * rr * 0.1, y + rr * 0.1, rr * 0.9, 'a')
    path(cv, curve((x + side * 0.3, y - rr * 0.7), (x + side * rr * 0.35, y + rr * 0.55), -side * rr * 0.18), 'e', 0.4)
    if big:
        tilted(cv, x - side * rr * 0.45, y - rr * 0.3, rr * 0.28, rr * 0.13, side * 55, 'h')
    seg(cv, x + side * 0.1, y - rr * 0.85, x - side * 0.4, y - rr - 1.2, 'k', 0.5)
    if leaf:
        lens(cv, x - side * 0.3, y - rr - 0.9, x - side * rr * 1.1, y - rr * 1.35 - 0.6, rr * 0.4 + 0.3, 'l')


def plum(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    mode = r.randrange(3)
    kind = 2 if mode == 2 else r.randrange(2)
    ty = h * r.uniform(0.84, 0.88)
    scene(cv, r, w, h, kind, ty)
    if mode == 0:  # one plum with its leaf
        oval(cv, cx, ty, w * 0.42, 1.8, 'p')
        rr = s * 0.31
        one_plum(cv, cx + w * 0.03, ty - rr - 0.4, rr, -1, big)
    elif mode == 1:  # a whole plum behind a cut half with its stone
        oval(cv, cx, ty, w * 0.42, 1.8, 'p')
        rr = s * 0.25
        one_plum(cv, cx + w * 0.16, ty - rr - h * 0.12, rr, 1, big)
        hx, hr = cx - w * 0.14, s * 0.27
        hy = ty - hr * 0.6
        gap(cv, hx, hy, hr + 0.9, 'aekhl')
        cut_half(cv, hx, hy, hr, 'a', 'f', 0.52)
        oval(cv, hx, hy, hr * 0.34, hr * 0.22, 'e')
    else:  # plums hanging from a branch, one fallen on the blanket
        y0 = h * r.uniform(0.07, 0.11)
        pts = curve((-0.5, y0 + h * 0.08), (w * 0.86, y0), -1.5)
        tube(cv, pts, 0.7 if not big else 1.0, 'k')
        rr = s * 0.18
        for k, u in enumerate((0.3, 0.7)):
            bx, by = pts[int(u * 20)]
            x, y = bx + (k - 0.5) * 1.0, by + rr + h * (0.12 + 0.08 * k)
            seg(cv, bx, by, x, y - rr, 'k', 0.45)
            one_plum(cv, x, y, rr, 1 - 2 * k, big, leaf=False)
        for u, a in ((0.12, -40), (0.5, 35), (0.88, -30)) if big else ((0.5, 35), (0.9, -30)):
            bx, by = pts[int(u * 20)]
            t = math.radians(a + 90)
            lens(cv, bx, by, bx + math.cos(t) * s * 0.28, by + math.sin(t) * s * 0.2, s * 0.12, 'l')
        one_plum(cv, cx + w * 0.22, ty - rr * 0.7, rr * 0.9, 1, big, leaf=False)
    if r.random() < 0.5:
        mirror(cv)
    S = scene_roles(kind, (BLUE, BLUE, GREEN, BROWN, PINK))
    return cv, [S['b'], role('e', 'crease', 'Crease and stone', PINK), role('a', 'plum', 'Plums', PINK), S['d'], S['t'],
                role('k', 'stalk', 'Stalks and branch', BROWN), role('f', 'flesh', 'Flesh', BROWN), role('l', 'leaf', 'Leaves', GREEN),
                role('p', 'plate', 'Plate', BLUE), role('h', 'bloom', 'Bloom', BLUE), S['o'], S['v']], ['food', 'fruit']


def berries(cv, x, y, cols, rows, rr, step):
    """A heap of blueberries, `rows` rows of them over (x, y), narrowing upwards, in turn of roles a and c, each with
    its crown."""
    out = []
    for row in range(rows):
        n = cols - row
        for k in range(n):
            out.append((x + (k - (n - 1) / 2) * step, y - row * step * 0.82, 'a' if (row + k) % 2 == 0 else 'c'))
    for bx, by, c in out:
        disc(cv, bx, by, rr, c)
    for bx, by, c in out:
        cv.put(int(bx), int(by - rr * 0.25), 'k')


def blueberries(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    mode, kind = r.randrange(3), r.randrange(2)
    ty = h * r.uniform(0.84, 0.88)
    scene(cv, r, w, h, kind, ty)
    rr, step = (1.25, 2.5) if not big else (1.5, 3.0)
    if mode == 0:  # a bowl heaped with berries, a sprig on top and two on the table
        by = ty - h * 0.2
        bx = cx - w * 0.06
        berries(cv, bx, by - 0.4, int(w * 0.62 / step), 3, rr, step)
        for t in (0.2, 0.75):
            lens(cv, bx + w * 0.02, by - step * 2.4, bx + w * (0.2 + 0.1 * t), by - step * 2.4 - h * 0.12 * (1 - t), s * 0.12, 'l')
        seg(cv, bx, by - step * 1.9, bx + 0.6, by - step * 2.6, 'n', 0.5)
        bowl(cv, bx, by, ty + 0.4, w * 0.36, 'p')
        for k in range(2):
            x = bx + w * 0.36 + k * step * 0.95
            disc(cv, x, ty - rr * 0.5 - 0.2, rr, 'c' if k else 'a')
            cv.put(int(x), int(ty - rr * 0.75 - 0.2), 'k')
    elif mode == 1:  # a punnet of berries
        x0, x1 = cx - w * 0.3, cx + w * 0.3
        top = ty - h * 0.25
        berries(cv, cx, top - 0.2, int(w * 0.6 / step) + 1, 3 if not big else 4, rr, step)
        poly(cv, [(x0 - 0.5, top), (x1 + 0.5, top), (x1 - 1.0, ty + 0.5), (x0 + 1.0, ty + 0.5)], 'p')
        n = 4 if not big else 5
        for k in range(1, n):
            x = x0 + k * (x1 - x0) / n
            seg(cv, x, top + 1.5, cx + (x - cx) * 0.85, ty - 0.6, 'q', 0.42)
        box(cv, x0 - 0.5, top, x1 + 0.5, top + 0.9, 'q')
        lens(cv, cx + w * 0.1, top - step * 2.2, cx + w * 0.38, top - step * 2.2 - h * 0.08, s * 0.12, 'l')
    else:  # one big berry close up, its crown showing, with a leaf and two small ones
        oval(cv, cx, ty, w * 0.42, 1.8, 'p')
        q = s * 0.27
        x, y = cx - w * 0.07, ty - q - 0.5
        lens(cv, x + q * 0.3, y - q * 0.85, x + w * 0.42, y - q - h * 0.1, s * 0.15, 'l')
        seg(cv, x + q * 0.1, y - q * 0.8, x + q * 0.4, y - q - 1.2, 'n', 0.5)
        disc(cv, x, y, q, 'a')
        star(cv, int(x) + 0.5, int(y - q * 0.3) + 0.5, q * 0.42, 'k', ri=q * 0.17)
        if big:
            tilted(cv, x - q * 0.5, y + q * 0.2, q * 0.28, q * 0.13, -60, 'h')
        for k in range(2):
            bx = x + q + 1.3 + k * step * 1.05
            gap(cv, bx, ty - rr - 0.4, rr + 0.8, 'a')
            disc(cv, bx, ty - rr - 0.4, rr, 'c' if k == 0 else 'a')
            cv.put(int(bx), int(ty - rr * 1.25 - 0.4), 'k')
    if r.random() < 0.5:
        mirror(cv)
    S = scene_roles(kind, (PINK, PINK, BROWN, GREEN, GREEN))
    return cv, [S['b'], role('a', 'berry', 'Blueberries', BLUE), role('c', 'berry2', 'Other blueberries', BLUE),
                role('k', 'crowns', 'Crowns', PINK), role('l', 'leaf', 'Leaves', GREEN), role('n', 'stem', 'Stems', GREEN),
                role('p', 'bowl', 'Bowl, punnet and plate', BROWN), role('q', 'slats', 'Punnet slats', BROWN),
                S['t'], S['d'], S['o'], S['v'], role('h', 'shine', 'Shine', PINK)], ['food', 'fruit']


def one_raspberry(cv, x, y, rr, big, leaf=0):
    """A raspberry: a rounded thimble of drupelets under a star of sepals and a stalk."""
    oval(cv, x, y + rr * 0.05, rr * 0.9, rr * 1.05, 'a')
    oval(cv, x, y - rr * 0.3, rr, rr * 0.72, 'a')
    dots(cv, 'e', 'a', 2, 2, area=(x - rr, y - rr * 1.1, x + rr, y + rr * 1.1), offset=int(x) % 2)
    top = y - rr * 0.95
    for a in (-165, -120, -60, -15):
        t = math.radians(a)
        lens(cv, x, top + 0.3, x + math.cos(t) * rr * 0.85, top + 0.3 + math.sin(t) * rr * 0.4, rr * 0.25 + 0.35, 'c')
    seg(cv, x, top, x + 0.3, top - rr * 0.45 - 0.6, 'c', 0.5)
    if leaf:
        lens(cv, x + 0.3, top - rr * 0.4, x + leaf * rr * 1.3, top - rr * 0.9, rr * 0.5 + 0.3, 'l')


def raspberry(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    mode = r.randrange(3)
    kind = 2 if mode == 2 else r.randrange(2)
    ty = h * r.uniform(0.84, 0.88)
    scene(cv, r, w, h, kind, ty)
    if mode == 0:  # one big raspberry with a leaf
        oval(cv, cx, ty, w * 0.42, 1.8, 'p')
        rr = s * 0.3
        one_raspberry(cv, cx + w * 0.03, ty - rr - 0.6, rr, big, 1)
    elif mode == 1:  # a bowl of raspberries
        by = ty - h * 0.16
        rr = s * 0.15
        for k, (dx, dy) in enumerate(((-0.2, 0.0), (0.2, 0.0), (0.02, -0.17))):
            x, y = cx + dx * w, by - rr * 0.5 + dy * h
            if k:
                gap(cv, x, y, rr + 0.8, 'aec')
            one_raspberry(cv, x, y, rr, big)
        lens(cv, cx + w * 0.06, by - h * 0.17 - rr * 1.2, cx + w * 0.38, by - h * 0.3, s * 0.12, 'l')
        bowl(cv, cx, by, ty + 0.4, w * 0.4, 'p')
    else:  # raspberries hanging from a cane, one on the blanket
        y0 = h * r.uniform(0.08, 0.12)
        pts = curve((-0.5, y0), (w * 0.86, y0 + h * 0.1), 1.2)
        tube(cv, pts, 0.6 if not big else 0.9, 'k')
        rr = s * 0.15
        for u in (0.3, 0.7):
            bx, by = pts[int(u * 20)]
            seg(cv, bx, by, bx, by + h * 0.08, 'k', 0.45)
            one_raspberry(cv, bx, by + h * 0.08 + rr * 1.1, rr, big)
        for u, a in ((0.5, 20), (0.9, -25)) + (((0.1, 30),) if big else ()):
            bx, by = pts[int(u * 20)]
            t = math.radians(a + 90)
            lens(cv, bx, by, bx + math.cos(t) * s * 0.26, by + math.sin(t) * s * 0.22, s * 0.14, 'l')
        one_raspberry(cv, cx + w * 0.2, ty - rr * 0.6, rr, big)
    if r.random() < 0.5:
        mirror(cv)
    S = scene_roles(kind, (BLUE, BLUE, GREEN, BROWN, BROWN))
    return cv, [S['b'], role('a', 'berry', 'Raspberries', PINK), role('e', 'drupelets', 'Drupelets', PINK), role('c', 'calyx', 'Sepals and stalks', GREEN),
                role('l', 'leaf', 'Leaves', GREEN), S['t'], role('k', 'cane', 'Cane', BROWN), role('p', 'plate', 'Plate and bowl', BLUE),
                S['d'], S['o'], S['v']], ['food', 'fruit']


def one_mango(cv, x, y, rr, ang, big, leaf=0):
    """A mango from its stalk end along `ang` degrees: a plump bean, rounder at the stalk and arched along its back,
    blushed near the stalk, with its stalk and a leaf (to the back's side when `leaf` is 1, the other way at -1)."""
    t = math.radians(ang)
    ux, uy = math.cos(t), math.sin(t)
    nx, ny = uy, -ux  # towards its back
    bean(cv, x, y, rr * 1.2, rr * 0.86, ang, 'a', wide=0.2, bend=rr * 0.22)
    tilted(cv, x - ux * rr * 0.6 + nx * rr * 0.4, y - uy * rr * 0.6 + ny * rr * 0.4, rr * 0.7, rr * 0.5, ang, 'q', only='a')
    sx, sy = x - ux * rr * 1.18 + nx * rr * 0.12, y - uy * rr * 1.18 + ny * rr * 0.12
    seg(cv, sx + ux * 0.5, sy + uy * 0.5, sx - ux * 1.2, sy - uy * 1.2, 'k', 0.5)
    if leaf:
        lx, ly = sx - ux * 0.8, sy - uy * 0.8
        lens(cv, lx, ly, lx + leaf * nx * rr * 1.3 - ux * rr * 0.4, ly + leaf * ny * rr * 1.3 - uy * rr * 0.4, rr * 0.42 + 0.3, 'l')


def mango_cheek(cv, x, y, rr):
    """A mango cheek cut into cubes and turned out: the skin under a dome of cubes (big boards)."""
    for xx, yy, px, py in cells(cv):
        if py >= y + rr * 0.15 and ((px - x) / (rr * 1.1)) ** 2 + ((py - y) / (rr * 0.75)) ** 2 <= 1.0:
            cv.g[yy][xx] = 'a'
    step = 2.5
    for xx, yy, px, py in cells(cv):
        if py < y + rr * 0.35 and ((px - x) / rr) ** 2 + ((py - y + rr * 0.1) / (rr * 0.8)) ** 2 <= 1.0:
            on = abs((px - x) / step - round((px - x) / step)) * step < 0.5 or abs((py - y) / step - round((py - y) / step)) * step < 0.5
            cv.g[yy][xx] = 'q' if on else 'f'


def mango(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    mode = r.randrange(3)
    kind = 2 if mode == 2 else r.randrange(2)
    ty = h * r.uniform(0.84, 0.88)
    scene(cv, r, w, h, kind, ty)
    if mode == 0:  # one mango leaning on the plate, with its leaf
        oval(cv, cx, ty, w * 0.42, 1.8, 'p')
        rr = s * 0.27
        one_mango(cv, cx + w * 0.03, ty - rr * 1.2, rr, r.uniform(105, 120), big, -1)
    elif mode == 1:  # one lying on the plate with a twig of leaves, or standing behind a cheek cut into cubes
        oval(cv, cx, ty, w * 0.42, 1.8, 'p')
        if big:
            rr = s * 0.21
            one_mango(cv, cx + w * 0.17, ty - rr * 1.7, rr, 100, big, -1)
            hr = s * 0.25
            hx, hy = cx - w * 0.12, ty - hr * 0.8
            gap(cv, hx, hy, hr + 1.0, 'aqkl')
            mango_cheek(cv, hx, hy, hr)
        else:
            rr = s * 0.25
            x, y = cx - w * 0.04, ty - rr * 0.95
            one_mango(cv, x, y, rr, 172, big)
            tx, ty2 = x + rr * 1.3, y - rr * 0.95
            for a in (-150, -95):
                t = math.radians(a)
                lens(cv, tx, ty2, tx + math.cos(t) * s * 0.36, ty2 + math.sin(t) * s * 0.26, s * 0.13, 'l')
    else:  # mangoes hanging on long stalks from a twig among long leaves
        tx, ty0 = cx - w * 0.06, h * r.uniform(0.14, 0.18)
        tube(cv, curve((cx - w * 0.3, -0.5), (tx, ty0), 1.0), 0.6 if not big else 0.8, 'k')
        for a in ((-170, -125, -55, -10) if big else (-160, -30)):
            t = math.radians(a)
            lens(cv, tx, ty0, tx + math.cos(t) * s * 0.4, ty0 + math.sin(t) * s * 0.22 + 1.0, s * 0.12 + 0.3, 'l')
        hang = ((0.0, 0.25),) if not big else ((-0.16, 0.2), (0.18, 0.2))
        for k, (dx, q) in enumerate(hang):
            rr = s * q
            x = tx + dx * w + 0.8
            y = ty0 + h * (0.06 + 0.08 * k) + rr * 1.2
            path(cv, curve((tx, ty0), (x, y - rr * 1.1), 0.5 - k), 'k', 0.45)
            one_mango(cv, x, y, rr, 84 + 10 * k, big)
    if r.random() < 0.5:
        mirror(cv)
    S = scene_roles(kind, (BLUE, BLUE, GREEN, PINK, PINK))
    return cv, [S['b'], role('a', 'mango', 'Mangoes', BROWN), role('k', 'stalk', 'Stalks and twig', BROWN), role('f', 'cubes', 'Cubes', BROWN),
                role('q', 'blush', 'Blush and cuts', PINK), S['t'], S['d'], role('l', 'leaf', 'Leaves', GREEN),
                role('p', 'plate', 'Plate', BLUE), S['o'], S['v']], ['food', 'fruit']


def one_pomegranate(cv, x, y, rr, big):
    """A pomegranate: round, with its crown of pointed sepals on a short neck."""
    disc(cv, x, y, rr, 'a')
    disc(cv, x + rr * 0.12, y + rr * 0.08, rr * 0.96, 'a')
    top = y - rr * 0.8
    cw = max(2.0, rr * 0.46)
    box(cv, x - cw * 0.6, top - rr * 0.3, x + cw * 0.6, top + 0.6, 'c')
    poly(cv, [(x - cw, top - rr * 0.62), (x - cw * 0.45, top - rr * 0.3), (x, top - rr * 0.68), (x + cw * 0.45, top - rr * 0.3),
              (x + cw, top - rr * 0.62), (x + cw * 0.6, top + 0.4), (x - cw * 0.6, top + 0.4)], 'c')
    if big:
        tilted(cv, x - rr * 0.45, y - rr * 0.25, rr * 0.28, rr * 0.13, -50, 'h')


def pomegranate_face(cv, x, y, rr, big):
    """The cut face of a pomegranate: its seeds packed in the pale pith, and on the big boards its rind round it (one
    level deeper; on the small ones the skin behind the face shows the rind)."""
    x, y = int(x) + 0.5, int(y) + 0.5
    if big:
        disc(cv, x, y, rr, 'a')
    disc(cv, x, y, rr - (0.9 if big else 0.0), 'w')
    for xx, yy, px, py in cells(cv):
        if cv.g[yy][xx] == 'w' and math.hypot(px - x, py - y) < rr - (2.0 if big else 1.0):
            k = (xx + (yy // 3) * 2) % 3
            if k < 2 and yy % 3 < 2:
                cv.g[yy][xx] = 'x'


def pomegranate(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    mode, kind = r.randrange(3), r.randrange(3)
    ty = h * r.uniform(0.84, 0.88)
    scene(cv, r, w, h, kind, ty)
    if mode == 0:  # one pomegranate with a leaf, and a few seeds
        oval(cv, cx, ty, w * 0.42, 1.8, 'p')
        rr = s * 0.3
        x, y = cx + w * 0.03, ty - rr - 0.4
        lens(cv, x + 0.8, y - rr * 0.95, x + w * 0.4, y - rr * 1.3, s * 0.13, 'l')
        one_pomegranate(cv, x, y, rr, big)
        scatter(cv, 'x', 'p', 3, r, sep=2)
    elif mode == 1:  # a whole one behind a half, its cut face to the front
        oval(cv, cx, ty, w * 0.42, 1.8, 'p')
        rr = s * 0.22
        one_pomegranate(cv, cx + w * 0.2, ty - rr - h * 0.18, rr, big)
        hr = s * 0.28
        hx, hy = cx - w * 0.1, ty - hr - 0.2
        gap(cv, hx, hy, hr + 0.9, 'ach')
        disc(cv, hx + 1.3, hy - 0.2, hr, 'a')
        pomegranate_face(cv, hx, hy, hr, big)
    else:  # a big and a small one, and loose seeds
        oval(cv, cx, ty, w * 0.42, 1.8, 'p')
        rr = s * 0.26
        one_pomegranate(cv, cx - w * 0.1, ty - rr - 0.6, rr, big)
        q = s * 0.17
        x = cx + w * 0.26
        gap(cv, x, ty - q - 0.3, q + 0.8, 'ach')
        one_pomegranate(cv, x, ty - q - 0.3, q, big)
        scatter(cv, 'x', 'p', 4, r, sep=2)
    if r.random() < 0.5:
        mirror(cv)
    S = scene_roles(kind, (BLUE, BLUE, BROWN, BROWN, GREEN))
    return cv, [S['b'], role('a', 'fruit', 'Pomegranates', PINK), role('c', 'crown', 'Crowns', PINK), role('x', 'arils', 'Seeds', PINK),
                S['t'], S['v'], role('w', 'pith', 'Pith', BROWN), S['d'], role('l', 'leaf', 'Leaf', GREEN), role('p', 'plate', 'Plate', BLUE),
                role('h', 'shine', 'Shine', BLUE), S['o']], ['food', 'fruit']


MORE_FOOD = [orange, plum, blueberries, raspberry, mango, pomegranate]

# Expansion roles of these subjects (as expansions.ROLES): subject -> {group: [(roleId, new name or None), ...]}.
MORE_FOOD_ROLES = {
    'plum': {'red': [('plum', 'Red plums')]},
    'raspberry': {'red': [('berry', None)]},
    'mango': {'lime': [('mango', None)], 'red': [('blush', None)]},
    'pomegranate': {'red': [('fruit', None), ('arils', None)]},
}

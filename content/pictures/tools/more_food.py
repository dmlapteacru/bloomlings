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


def scene(cv, r, w, h, kind, ty, decor=True):
    """The setting of `kind` (SCENES), its table or blanket from row ty down: roles b (background), o (window panes,
    wall dots and jars, or a cloud), v (window frame, shelf or hill) at the top left, where the subjects keep their
    tall things away (and which a branch across the top goes without: `decor`), t (table or blanket) and d (a row of dots on a table four rows deep or more, below the plates
    and clear of the rows that keep the table whole)."""
    if not decor:
        pass
    elif kind == 0:
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
    if kind == 2:
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
            cv.g[yy][xx] = 'w' if rr - 1.7 < d <= rr - 0.9 else 'a'
    for a in (45, 90, 135):
        t = math.radians(a)
        seg(cv, x, y, x + math.cos(t) * (rr - 1.2), y - math.sin(t) * (rr - 1.2), 'w', 0.42)


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
        rr = s * (0.22 if big else 0.25)
        one_orange(cv, cx + w * 0.2, ty - rr - h * (0.16 if big else 0.1), rr, big, 1)
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
    scene(cv, r, w, h, kind, ty, decor=mode != 2)
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
    scene(cv, r, w, h, kind, ty, decor=mode != 2)
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
    scene(cv, r, w, h, kind, ty, decor=mode != 2)
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
    """The cut face of a pomegranate: a mass of seeds flecked with pale pith, and on the big boards its rind round it
    (one level deeper; on the small ones the skin behind the face shows the rind)."""
    x, y = int(x) + 0.5, int(y) + 0.5
    if big:
        disc(cv, x, y, rr, 'a')
    disc(cv, x, y, rr - (0.9 if big else 0.0), 'x')
    dots(cv, 'w', 'x', 2, 2, area=(x - rr, y - rr, x + rr, y + rr), offset=int(x) % 2)


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


# ---- Vegetables ----

def one_onion(cv, x, y, rr, big, sprout=True):
    """An onion: a round bulb drawn up to a pointed neck, the lines of its skin from the neck down (stopping short of
    the roots, so the skin between them stays one piece), the roots below, and a green sprout or a dry tip."""
    disc(cv, x, y, rr, 'a')
    poly(cv, [(x - rr * 0.62, y - rr * 0.62), (x - 0.4, y - rr * 1.45), (x + 0.4, y - rr * 1.45), (x + rr * 0.62, y - rr * 0.62)], 'a')
    for k in ((-1, 1) if not big else (-1, 0, 1)):
        if k:
            path(cv, curve((x, y - rr * 1.25), (x, y + rr * 0.55), k * rr * 0.45), 'e', 0.4)
        else:
            seg(cv, x, y - rr * 1.2, x, y + rr * 0.55, 'e', 0.4)
    for dx in (-1, 0, 1):
        seg(cv, x + dx * rr * 0.3, y + rr * 0.85, x + dx * rr * 0.55, y + rr + 0.9, 'e', 0.4)
    if sprout:
        for a, L in ((-120, 1.2), (-80, 1.5)) if not big else ((-125, 1.1), (-95, 1.6), (-65, 1.25)):
            t = math.radians(a)
            lens(cv, x, y - rr * 1.3, x + math.cos(t) * rr * L, y - rr * 1.3 + math.sin(t) * rr * L, rr * 0.28 + 0.3, 'g')
    else:
        seg(cv, x, y - rr * 1.4, x + 0.7, y - rr * 1.4 - 1.3, 'e', 0.45)


def onion_half(cv, x, y, rr):
    """An onion cut in half, its cut face to the front and standing on the board at row y: its layers as nested
    arches, each reaching the board, so none nests in another."""
    x, y = int(x) + 0.5, round(y)
    for xx, yy, px, py in cells(cv):
        d = math.hypot(px - x, (py - y) * 0.85)
        if py <= y and d <= rr:
            cv.g[yy][xx] = 'a' if int((rr - d) / 1.15) % 2 == 0 else 'w'
    poly(cv, [(x - 1.0, y - rr / 0.85 + 0.6), (x, y - rr / 0.85 - 1.2), (x + 1.0, y - rr / 0.85 + 0.6)], 'a')


def onion(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    mode, kind = r.randrange(3), r.randrange(2)
    ty = h * r.uniform(0.84, 0.88)
    scene(cv, r, w, h, kind, ty)
    rbox(cv, cx - w * 0.42, ty - 1.4, cx + w * 0.42, ty + 0.6, 0.7, 'p')
    if mode == 0:  # one onion sprouting
        rr = s * 0.28
        one_onion(cv, cx + w * 0.02, ty - rr - 1.6, rr, big)
    elif mode == 1:  # a sprouting onion behind one cut in half
        rr = s * 0.21
        one_onion(cv, cx + w * 0.19, ty - rr - h * 0.14, rr, big)
        hr = s * 0.28
        hx = cx - w * 0.12
        gap(cv, hx, ty - 1.4, hr + 1.0, 'aewg')
        onion_half(cv, hx, ty - 1.4, hr)
    else:  # three onions, the front one sprouting
        rr = s * 0.17
        for k, (dx, dy) in enumerate(((-0.2, -0.1), (0.21, -0.12), (0.0, 0.0))):
            x, y = cx + dx * w, ty - rr - 1.6 + dy * h
            if k:
                gap(cv, x, y - rr * 0.3, rr * 1.25 + 0.8, 'aewg')
            one_onion(cv, x, y, rr, big, sprout=k == 2)
    if r.random() < 0.5:
        mirror(cv)
    S = scene_roles(kind, (BLUE, BLUE, BLUE, PINK, PINK))
    return cv, [S['b'], role('a', 'onion', 'Onions', PINK), role('e', 'lines', 'Skin lines, tips and roots', BROWN), S['o'], S['v'],
                role('p', 'board', 'Board', BLUE), role('w', 'layers', 'Inner layers', BLUE), role('g', 'sprout', 'Sprouts', GREEN),
                S['t'], S['d']], ['food', 'vegetables']


def one_garlic(cv, x, y, rr, big):
    """A bulb of garlic: plump cloves side by side drawn up to a narrow neck with a wispy tip, the lines between the
    cloves, and short roots under its flat base."""
    for dx, q in ((-0.5, 0.6), (0.5, 0.6), (-0.18, 0.72), (0.18, 0.72)):
        oval(cv, x + dx * rr, y, rr * q, rr * 0.86, 'a')
    poly(cv, [(x - rr * 0.48, y - rr * 0.5), (x - 0.45, y - rr * 1.3), (x + 0.45, y - rr * 1.3), (x + rr * 0.48, y - rr * 0.5)], 'a')
    seg(cv, x, y - rr * 1.25, x + 0.8, y - rr * 1.7, 'a', 0.45)
    for k in ((-1, 1) if not big else (-2, -1, 1, 2)):
        path(cv, curve((x + k * 0.25, y - rr * 0.85), (x + k * rr * 0.28, y + rr * 0.78), -k * rr * 0.12 * (3 - abs(k))), 'e', 0.4)
    for dx in (-0.4, 0.0, 0.4):
        seg(cv, x + dx * rr, y + rr * 0.75, x + dx * rr * 1.3, y + rr * 0.75 + 1.0, 'k', 0.4)


def clove(cv, x, y, q, ang):
    """A loose clove: a plump crescent narrowing to a point."""
    bean(cv, x, y, q * 1.25, q * 0.7, ang, 'a', wide=0.45, bend=q * 0.35)


def garlic(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    mode, kind = r.randrange(3), r.randrange(2)
    ty = h * r.uniform(0.84, 0.88)
    scene(cv, r, w, h, kind, ty)
    q = s * 0.09 + 0.3
    if mode == 0:  # a bulb and two loose cloves on a board
        oval(cv, cx, ty, w * 0.42, 1.8, 'p')
        rr = s * 0.28
        one_garlic(cv, cx - w * 0.06, ty - rr - 0.8, rr, big)
        clove(cv, cx + w * 0.3, ty - q * 0.8, q, -150)
    elif mode == 1:  # two bulbs and a clove
        oval(cv, cx, ty, w * 0.42, 1.8, 'p')
        rr = s * 0.21
        one_garlic(cv, cx + w * 0.17, ty - rr - h * 0.16, rr, big)
        x = cx - w * 0.12
        gap(cv, x, ty - rr - 0.8, rr * 1.15 + 0.8, 'aek')
        one_garlic(cv, x, ty - rr - 0.8, rr * 1.05, big)
        clove(cv, cx + w * 0.32, ty - q * 0.8, q, -150)
    else:  # a bulb in a small bowl, cloves beside it
        rr = s * 0.24
        by = ty - h * 0.12
        one_garlic(cv, cx - w * 0.08, by - rr * 0.35, rr, big)
        bowl(cv, cx - w * 0.08, by, ty + 0.4, w * 0.3, 'p')
        clove(cv, cx + w * 0.32, ty - q * 0.8, q, -150)
        if big:
            clove(cv, cx + w * 0.3, ty - q * 2.6, q, -30)
    if r.random() < 0.5:
        mirror(cv)
    S = scene_roles(kind, (BLUE, BLUE, PINK, PINK, PINK))
    return cv, [S['b'], S['t'], S['v'], role('a', 'garlic', 'Garlic', PINK), S['d'], role('e', 'lines', 'Clove lines', BROWN),
                role('k', 'roots', 'Roots', BROWN), role('p', 'board', 'Plate and bowl', GREEN), S['o']], ['food', 'vegetables']


def one_potato(cv, r, x, y, rr, ang, big):
    """A potato: a lumpy oval with a few eyes."""
    t = math.radians(ang)
    ux, uy = math.cos(t), math.sin(t)
    tilted(cv, x, y, rr * 1.25, rr * 0.8, ang, 'a')
    disc(cv, x + ux * rr * 0.55 - uy * rr * 0.12, y + uy * rr * 0.55 + ux * rr * 0.12, rr * 0.7, 'a')
    disc(cv, x - ux * rr * 0.5 + uy * rr * 0.15, y - uy * rr * 0.5 - ux * rr * 0.15, rr * 0.74, 'a')
    scatter(cv, 'e', 'a', 2 if not big else 3, r, sep=2, area=(x - rr, y - rr * 0.6, x + rr, y + rr * 0.6))


def potato(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    mode = r.randrange(3)
    kind = 2 if mode == 2 else r.randrange(2)
    ty = h * r.uniform(0.84, 0.88)
    if mode < 2:
        scene(cv, r, w, h, kind, ty)
    rr = s * 0.17
    if mode == 0:  # an open sack with potatoes heaped in its mouth, one on the floor
        y0 = ty - h * 0.42
        rbox(cv, cx - w * 0.28, y0 + 1.5, cx + w * 0.28, ty + 0.4, 2.0, 'k')
        for k, (dx, dy, a) in enumerate(((-0.14, 0.0, -15), (0.15, 0.0, 20), (0.0, -0.09, 5))):
            x, y = cx + dx * w, y0 + dy * h + 0.2
            if k:
                tilted(cv, x, y, rr * 1.3 + 0.8, rr * 0.85 + 0.8, a, 'b', only='ae')
            one_potato(cv, r, x, y, rr, a, big)
        rbox(cv, cx - w * 0.32, y0 + 0.8, cx + w * 0.32, y0 + 2.8, 0.9, 'n')
        seg(cv, cx - w * 0.2, y0 + h * 0.2, cx + w * 0.2, y0 + h * 0.22, 'n', 0.45)
        one_potato(cv, r, cx + w * 0.36, ty - rr * 0.5, rr * 0.85, 10, big)
    elif mode == 1:  # potatoes heaped in a basket, one on the table
        by = ty - h * 0.18
        for k, (dx, dy, a) in enumerate(((-0.16, 0.0, -10), (0.16, 0.0, 15), (0.0, -0.1, 0))):
            x, y = cx - w * 0.06 + dx * w, by - rr * 0.3 + dy * h
            if k:
                tilted(cv, x, y, rr * 1.3 + 0.8, rr * 0.85 + 0.8, a, 'b', only='ae')
            one_potato(cv, r, x, y, rr, a, big)
        bowl(cv, cx - w * 0.06, by, ty + 0.4, w * 0.36, 'k', 'n')
        one_potato(cv, r, cx + w * 0.34, ty - rr * 0.5, rr * 0.85, -10, big)
    else:  # a potato plant above the soil, its potatoes under it
        gy = h * r.uniform(0.5, 0.56)
        cloud(cv, w * 0.24, h * 0.09, s * 0.07 + 0.5, 'o')
        box(cv, 0, gy, w, h, 't')
        box(cv, 0, gy, w, gy + 0.9, 'v')
        sx = cx + w * 0.04
        seg(cv, sx, gy, sx, gy - h * 0.28, 'n', 0.6)
        for k, (a, L) in enumerate(((-150, 0.32), (-30, 0.34), (-115, 0.28), (-65, 0.3)) if big else ((-150, 0.32), (-30, 0.34), (-90, 0.22))):
            t = math.radians(a)
            y = gy - h * (0.12 + 0.07 * (k % 2) + 0.06 * (k // 2))
            lens(cv, sx, y, sx + math.cos(t) * s * L, y + math.sin(t) * s * L * 0.7, s * 0.14 + 0.3, 'l')
        if big:
            disc(cv, sx, gy - h * 0.3, s * 0.05 + 0.6, 'f')
        for dx, dy, a in ((-0.22, 0.2, -20), (0.2, 0.17, 15), (0.0, 0.34, 0)):
            x, y = sx + dx * w, gy + dy * (h - gy) * 2.2
            seg(cv, sx, gy + 0.5, x, y, 'k', 0.4)
            one_potato(cv, r, x, y, rr, a, big)
    if r.random() < 0.5:
        mirror(cv)
    if mode == 2:
        top = [sky('b'), role('t', 'soil', 'Soil', PINK), role('v', 'grass', 'Grass', GREEN), role('o', 'cloud', 'Cloud', BLUE)]
    else:
        S = scene_roles(kind, (BLUE, BLUE, PINK, PINK, PINK))
        top = [S['b'], S['t'], S['v'], S['d'], S['o']]
    return cv, top + [role('e', 'eyes', 'Eyes', BROWN), role('a', 'potato', 'Potatoes', BROWN), role('k', 'sack', 'Sack, basket and roots', GREEN),
                      role('n', 'rim', 'Rim, string and stem', GREEN), role('l', 'leaf', 'Leaves', GREEN), role('f', 'flower', 'Flower', BLUE)], ['food', 'vegetables']


def one_cucumber(cv, p0, p1, rr, bend):
    """A cucumber from its stalk end p0 to its blossom end p1: round-ended, with little bumps, its stalk and the dried
    flower at its tip."""
    pts = curve(p0, p1, bend)
    tube(cv, pts, lambda t: rr * (0.8 + 0.2 * math.sin(math.pi * t)), 'a')
    xs, ys = [p[0] for p in pts], [p[1] for p in pts]
    dots(cv, 'e', 'a', 3, 2, area=(min(xs) - rr, min(ys) - rr, max(xs) + rr, max(ys) + rr), offset=1)
    (ax, ay), (bx, by) = pts[0], pts[2]
    L = math.hypot(bx - ax, by - ay) or 1.0
    seg(cv, ax - (bx - ax) / L * rr * 0.5, ay - (by - ay) / L * rr * 0.5, ax - (bx - ax) / L * (rr * 0.8 + 0.8), ay - (by - ay) / L * (rr * 0.8 + 0.8), 'k', 0.5)
    (ax, ay), (bx, by) = pts[-3], pts[-1]
    L = math.hypot(bx - ax, by - ay) or 1.0
    disc(cv, bx + (bx - ax) / L * rr * 0.8, by + (by - ay) / L * rr * 0.8, rr * 0.45 + 0.4, 'y')


def cucumber_slice(cv, x, y, q, big):
    """A slice of cucumber, face on: its rind (a role of its own, so it can lie against a whole cucumber), pale flesh
    and, on the big boards, a ring of seeds."""
    disc(cv, x, y, q, 'c')
    disc(cv, x, y, q - 0.9, 'w')
    if big:
        for k in range(6):
            a = math.radians(k * 60 + 30)
            cv.put(int(x + math.cos(a) * q * 0.4), int(y + math.sin(a) * q * 0.4), 'x')


def cucumber(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    mode = r.randrange(3)
    kind = 2 if mode == 2 else r.randrange(2)
    ty = h * r.uniform(0.84, 0.88)
    scene(cv, r, w, h, kind, ty, decor=mode != 2)
    rr = s * 0.13 + 0.2
    q = s * 0.12 + 0.6
    if mode == 0:  # a cucumber across a board, slices in front
        rbox(cv, cx - w * 0.44, ty - 1.4, cx + w * 0.44, ty + 0.6, 0.7, 'p')
        one_cucumber(cv, (w * 0.3, ty - h * 0.42), (w * 0.72, ty - h * 0.22), rr, -1.0)
        for k in range(2 if not big else 3):
            x = cx - w * 0.2 + k * q * 1.5
            cucumber_slice(cv, x, ty - q - 0.9, q, big)
    elif mode == 1:  # two cucumbers side by side on a board, a slice in front
        rbox(cv, cx - w * 0.44, ty - 1.4, cx + w * 0.44, ty + 0.6, 0.7, 'p')
        one_cucumber(cv, (w * 0.38, ty - h * 0.48), (w * 0.72, ty - h * 0.36), rr * 0.9, -0.6)
        p0, p1 = (w * 0.3, ty - h * 0.34), (w * 0.7, ty - h * 0.16)
        tube(cv, curve(p0, p1, -0.6), rr * 1.05 + 0.9, 'b', only='aeky')
        one_cucumber(cv, p0, p1, rr, -0.6)
        cucumber_slice(cv, cx - w * 0.26, ty - q - 0.9, q, big)
    else:  # a cucumber hanging from its vine under a leaf, a flower open, a slice on a plate below
        vy = h * r.uniform(0.18, 0.22)
        pts = curve((-0.5, vy + h * 0.06), (w * 0.66, vy), -1.2)
        path(cv, pts, 'n', 0.55)
        for u, dy in ((0.3, 0.1),) if not big else ((0.2, 0.1), (0.62, 0.08)):
            px, py = pts[int(u * 20)]
            heart(cv, px, py + h * dy, s * 0.11 + 0.4, 'l')
        px, py = pts[20]
        path(cv, [(px, py), (px + 1.2, py - 0.8), (px + 1.6, py + 0.6), (px + 0.6, py + 1.0)], 'n', 0.45)
        fx, fy = pts[12]
        star(cv, fx, fy + 1.6, s * 0.06 + 0.6, 'y', ri=s * 0.03 + 0.3)
        hx, hy = pts[17]
        one_cucumber(cv, (hx, hy + rr + 1.2), (hx + w * 0.04, ty - h * 0.2), rr, 0.6)
        oval(cv, cx - w * 0.2, ty, w * 0.2, 1.4, 'p')
        cucumber_slice(cv, cx - w * 0.2, ty - q - 0.5, q, big)
    if r.random() < 0.5:
        mirror(cv)
    S = scene_roles(kind, (BLUE, BLUE, PINK, PINK, PINK))
    return cv, [S['b'], role('a', 'cucumber', 'Cucumbers', GREEN), role('c', 'rind', 'Slice rind', GREEN), role('e', 'bumps', 'Bumps', GREEN), role('l', 'leaf', 'Leaves', GREEN),
                role('x', 'seeds', 'Seeds', GREEN), role('w', 'flesh', 'Flesh', BLUE), role('p', 'board', 'Board and plate', BROWN),
                role('k', 'stalk', 'Stalks', BROWN), role('y', 'flower', 'Flowers', BROWN), role('n', 'vine', 'Vine and tendril', GREEN),
                S['t'], S['v'], S['d'], S['o']], ['food', 'vegetables']


def one_olive(cv, x, y, q, ang, c, pimento=False):
    """An olive, green or black; a green one stuffed with pimento shows it at one end."""
    tilted(cv, x, y, q * 1.25, q * 0.95, ang, c)
    if pimento:
        t = math.radians(ang)
        disc(cv, x + math.cos(t) * q * 0.75, y + math.sin(t) * q * 0.75, max(0.6, q * 0.38), 'x')


def olive_sprig(cv, x, y, L, ang, big):
    """A sprig of olive leaves: a twig with narrow leaves in pairs."""
    t = math.radians(ang)
    ux, uy = math.cos(t), math.sin(t)
    seg(cv, x, y, x + ux * L, y + uy * L, 'k', 0.45)
    for k in range(1, 3 if not big else 4):
        px, py = x + ux * L * k / (3 if not big else 4), y + uy * L * k / (3 if not big else 4)
        for side in (-1, 1):
            a = t + side * 0.7
            lens(cv, px, py, px + math.cos(a) * L * 0.45, py + math.sin(a) * L * 0.45, L * 0.13 + 0.4, 'l')
    lens(cv, x + ux * L, y + uy * L, x + ux * L * 1.4, y + uy * L * 1.4, L * 0.13 + 0.4, 'l')


def olives(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    mode = r.randrange(3)
    kind = 2 if mode == 1 else r.randrange(2)
    ty = h * r.uniform(0.84, 0.88)
    scene(cv, r, w, h, kind, ty, decor=mode != 1)
    q = s * 0.1 + 0.1
    if mode == 0:  # a bowl of green and black olives, a pick in them, a sprig beside
        by = ty - h * 0.18
        bx = cx - w * 0.06
        seg(cv, bx + w * 0.04, by - q * 2.0, bx + w * 0.16, by - h * 0.26, 'k', 0.45)
        one_olive(cv, bx + w * 0.13, by - h * 0.2, q * 1.1, -70, 'a', True)
        n = 3 if not big else 4
        for row in range(2):
            for k in range(n - row):
                x = bx + (k - (n - row - 1) / 2) * q * 2.6
                y = by - 0.2 - row * q * 1.7
                one_olive(cv, x, y, q, 15 * (k % 2 * 2 - 1), 'a' if (k + row) % 2 == 0 else 'q', (k + row) % 2 == 0)
        bowl(cv, bx, by, ty + 0.4, w * 0.34, 'p')
        olive_sprig(cv, cx + w * 0.26, ty - 0.6, s * 0.2, -75, big)
    elif mode == 1:  # olives hanging from a branch, a dish of stuffed ones on the blanket
        y0 = h * r.uniform(0.08, 0.12)
        pts = curve((-0.5, y0 + h * 0.08), (w * 0.84, y0), -1.2)
        tube(cv, pts, 0.5 if not big else 0.75, 'k')
        for u, side in ((0.15, 1), (0.42, -1), (0.62, 1), (0.88, -1)):
            px, py = pts[int(u * 20)]
            a = math.radians(side * 55 + 90 + (10 if side > 0 else -10))
            lens(cv, px, py, px + math.cos(a) * s * 0.3, py + math.sin(a) * s * 0.22, s * 0.07 + 0.5, 'l')
        for k, u in enumerate((0.3, 0.52, 0.76)):
            px, py = pts[int(u * 20)]
            seg(cv, px, py, px, py + q * 1.6, 'k', 0.4)
            one_olive(cv, px, py + q * 2.4, q, 90, 'a' if k % 2 == 0 else 'q')
        oval(cv, cx + w * 0.12, ty, w * 0.26, 1.4, 'p')
        for k in range(2):
            one_olive(cv, cx + w * 0.12 + (k - 0.5) * q * 2.6, ty - q - 0.4, q, -20 + 40 * k, 'a', True)
    else:  # two big stuffed olives on a pick, a sprig on the plate
        oval(cv, cx, ty, w * 0.42, 1.8, 'p')
        qq = s * 0.15
        p0, p1 = (cx - w * 0.38, ty - h * 0.08), (cx + w * 0.34, ty - h * 0.44)
        seg(cv, p0[0], p0[1], p1[0], p1[1], 'k', 0.5)
        ang = math.degrees(math.atan2(p1[1] - p0[1], p1[0] - p0[0]))
        for u in (0.38, 0.68):
            x, y = p0[0] + (p1[0] - p0[0]) * u, p0[1] + (p1[1] - p0[1]) * u
            gap(cv, x, y, qq * 1.25 + 0.8, 'ax')
            one_olive(cv, x, y, qq, ang, 'a', True)
        olive_sprig(cv, cx + w * 0.1, ty - 0.8, s * 0.22, -20, big)
    if r.random() < 0.5:
        mirror(cv)
    S = scene_roles(kind, (BLUE, BLUE, BROWN, PINK, PINK))
    return cv, [S['b'], role('a', 'olive', 'Green olives', GREEN), role('l', 'leaf', 'Leaves', GREEN), role('x', 'pimento', 'Pimento', PINK),
                role('q', 'black', 'Black olives', PINK), role('k', 'twig', 'Twigs and pick', BROWN), role('p', 'bowl', 'Bowl and plate', BROWN),
                S['t'], S['d'], S['o'], S['v']], ['food', 'vegetables']


def one_radish(cv, x, y, rr, big, lean=0.0, leaves=3, c='a'):
    """A radish: a round red root with its white tail below and a tuft of leaves fanning from its top, each with its
    midrib on the big boards."""
    top = y - rr * 0.85
    for k in range(leaves):
        a = math.radians(-90 + (k - (leaves - 1) / 2) * 30 + lean)
        L = rr * (2.3 if k == leaves // 2 else 1.9)
        lens(cv, x + math.cos(a) * 0.3, top + math.sin(a) * 0.3, x + math.cos(a) * L, top + math.sin(a) * L, rr * 0.75 + 0.3, 'l')
        if big:
            seg(cv, x + math.cos(a) * L * 0.2, top + math.sin(a) * L * 0.2, x + math.cos(a) * L * 0.8, top + math.sin(a) * L * 0.8, 'n', 0.4)
    disc(cv, x, y, rr, c)
    tube(cv, [(x, y + rr * 0.7), (x + 0.4, y + rr * 1.9)], lambda t: max(0.4, rr * 0.42 * (1 - t)), 'w')


def radish(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    mode = r.randrange(3)
    kind = 2 if mode == 2 else r.randrange(2)
    ty = h * r.uniform(0.84, 0.88)
    if mode < 2:
        scene(cv, r, w, h, kind, ty)
    if mode == 0:  # three radishes standing in a row on a board
        rbox(cv, cx - w * 0.44, ty - 1.4, cx + w * 0.44, ty + 0.6, 0.7, 'p')
        rr = s * 0.15
        for k, dx in enumerate((-0.24, 0.24, 0.0)):
            x, y = cx + dx * w, ty - rr * 2.2 - (0.8 if k == 2 else 0.0)
            one_radish(cv, x, y, rr, big, lean=dx * 60, leaves=2 if k < 2 else 3, c='c' if k == 2 else 'a')
    elif mode == 1:  # one big radish and two slices
        rbox(cv, cx - w * 0.44, ty - 1.4, cx + w * 0.44, ty + 0.6, 0.7, 'p')
        rr = s * 0.21
        one_radish(cv, cx - w * 0.12, ty - rr * 2.4, rr, big, lean=10)
        q = s * 0.1 + 0.6
        for k in range(2):
            x, y = cx + w * (0.2 + 0.08 * k), ty - q - 1.0 - k * q * 1.4
            gap(cv, x, y, q + 0.8, 'awln')
            disc(cv, x, y, q, 'a')
            disc(cv, x, y, q - 0.8, 'i')
    else:  # radishes growing in a bed, their red tops showing, one pulled out
        gy = h * r.uniform(0.7, 0.74)
        cloud(cv, w * 0.24, h * 0.09, s * 0.07 + 0.5, 'o')
        box(cv, 0, gy, w, h, 't')
        rr = s * 0.13
        for k, dx in enumerate((-0.28, 0.0, 0.28)):
            one_radish(cv, cx + dx * w, gy + rr * 0.1, rr, big, lean=dx * 40)
        box(cv, 0, gy + rr * 0.35, w, h, 't', only='aw')
        if big:
            one_radish(cv, cx + w * 0.1, h - 2.4, rr * 0.9, big, lean=80, leaves=2)
        dots(cv, 'd', 't', 4, 2, area=(0, gy + 2, w - 1, h - 2))
    if r.random() < 0.5:
        mirror(cv)
    if mode == 2:
        top = [sky('b'), role('t', 'soil', 'Soil', BROWN), role('d', 'pebbles', 'Pebbles', BROWN), role('o', 'cloud', 'Cloud', BLUE)]
    else:
        S = scene_roles(kind, (BLUE, BLUE, BROWN, GREEN, GREEN))
        top = [S['b'], S['o'], S['v'], S['t'], S['d']]
    return cv, top + [role('a', 'radish', 'Radishes', PINK), role('c', 'radish2', 'Radish behind', PINK), role('w', 'tail', 'Tails', PINK), role('i', 'inside', 'White inside', BLUE),
                      role('l', 'leaf', 'Leaves', GREEN), role('n', 'midribs', 'Midribs', GREEN), role('p', 'board', 'Board', BROWN)], ['food', 'vegetables']


def one_cabbage(cv, x, y, rr, big):
    """A cabbage: a round head in a cup of outer leaves, the leaves' rims and veins showing."""
    oval(cv, x, y + rr * 0.08, rr * 1.06, rr * 0.92, 'a')
    for a in (200, 250, 290, 340):
        t = math.radians(a)
        disc(cv, x + math.cos(t) * rr * 0.62, y + rr * 0.08 - math.sin(t) * rr * 0.5, rr * 0.5, 'a')
    disc(cv, x, y - rr * 0.22, rr * 0.6, 'i')
    path(cv, curve((x - rr * 0.5, y - rr * 0.5), (x + rr * 0.15, y - rr * 0.78), -rr * 0.12), 'e', 0.4)
    for side in (-1, 1):
        path(cv, curve((x, y + rr * 0.85), (x + side * rr * 0.85, y - rr * 0.1), side * rr * 0.2), 'e', 0.4)
    if big:
        for side in (-1, 1):
            path(cv, curve((x + side * rr * 0.1, y + rr * 0.85), (x + side * rr * 0.4, y + rr * 0.25), side * rr * 0.1), 'e', 0.4)


def cabbage_half(cv, x, y, rr):
    """Half a red cabbage, its cut face to the front and standing on the board at row y: the leaves' pale wavy lines,
    each reaching the board."""
    x, y = int(x) + 0.5, round(y)
    for xx, yy, px, py in cells(cv):
        d = math.hypot(px - x, py - y)
        if py <= y and d <= rr:
            a = math.atan2(y - py, px - x)
            wv = d + 0.5 * math.sin(a * 6 + d * 0.8)
            cv.g[yy][xx] = 'e' if wv % 2.2 < 0.75 and d < rr - 0.9 else 'q'


def cabbage(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    mode = r.randrange(3)
    kind = 2 if mode == 2 else r.randrange(2)
    ty = h * r.uniform(0.84, 0.88)
    if mode < 2:
        scene(cv, r, w, h, kind, ty)
        rbox(cv, cx - w * 0.44, ty - 1.4, cx + w * 0.44, ty + 0.6, 0.7, 'p')
    if mode == 0:  # one big cabbage on a board
        rr = s * 0.36
        one_cabbage(cv, cx + w * 0.02, ty - rr * 0.95 - 0.8, rr, big)
    elif mode == 1:  # a green cabbage behind half a red one
        rr = s * 0.27
        one_cabbage(cv, cx + w * 0.16, ty - rr - h * 0.1, rr, big)
        hr = s * 0.3
        hx = cx - w * 0.12
        gap(cv, hx, ty - 1.4, hr + 1.0, 'aie')
        cabbage_half(cv, hx, ty - 1.4, hr)
    else:  # cabbages in a row on the soil, a butterfly over them on the big boards
        gy = h * r.uniform(0.74, 0.78)
        cloud(cv, w * 0.24, h * 0.09, s * 0.07 + 0.5, 'o')
        box(cv, 0, gy, w, h, 't')
        dots(cv, 'd', 't', 4, 2, area=(0, gy + 2, w - 1, h - 2))
        rr = s * 0.25
        one_cabbage(cv, cx + w * 0.2, gy - rr * 0.75, rr * 0.9, big)
        gap(cv, cx - w * 0.14, gy - rr * 0.8, rr * 1.05 + 0.8, 'aie')
        one_cabbage(cv, cx - w * 0.14, gy - rr * 0.8, rr, big)
        if big:
            bx, by = w * 0.7, h * 0.24
            for side in (-1, 1):
                oval(cv, bx + side * 1.2, by, 1.2, 1.5, 'f')
            seg(cv, bx, by - 1.2, bx, by + 1.2, 'k', 0.45)
    if r.random() < 0.5:
        mirror(cv)
    if mode == 2:
        top = [sky('b'), role('t', 'soil', 'Soil', BROWN), role('d', 'pebbles', 'Pebbles', BROWN), role('o', 'cloud', 'Cloud', BLUE),
               role('f', 'butterfly', 'Butterfly', PINK), role('k', 'body', 'Butterfly body', BROWN)]
    else:
        S = scene_roles(kind, (BLUE, BLUE, GREEN, PINK, PINK))
        top = [S['b'], S['o'], S['t'], S['d']]
    return cv, [top[0], role('a', 'outer', 'Outer leaves', GREEN)] + ([S['v']] if mode < 2 else []) + [
        role('i', 'head', 'Head', GREEN), role('e', 'veins', 'Veins and cut lines', BLUE), role('q', 'red', 'Red cabbage', PINK),
        role('p', 'board', 'Board', BROWN)] + top[1:], ['food', 'vegetables']

MORE_FOOD = [orange, plum, blueberries, raspberry, mango, pomegranate, onion, garlic, potato, cucumber, olives, radish, cabbage]

# Expansion roles of these subjects (as expansions.ROLES): subject -> {group: [(roleId, new name or None), ...]}.
MORE_FOOD_ROLES = {
    'plum': {'red': [('plum', 'Red plums')]},
    'raspberry': {'red': [('berry', None)]},
    'mango': {'lime': [('mango', None)], 'red': [('blush', None)]},
    'pomegranate': {'red': [('fruit', None), ('arils', None)]},
    'onion': {'red': [('onion', 'Red onions')]},
    'cucumber': {'lime': [('flower', None)]},
    'olives': {'red': [('pimento', None)]},
    'radish': {'red': [('radish', None), ('radish2', None)]},
}

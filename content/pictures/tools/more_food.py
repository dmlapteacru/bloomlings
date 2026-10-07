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


# The settings: a window over a table, a dotted wall with a shelf of jars over a table, a picnic blanket before a hill
# under a cloud.
SCENES = (
    ('wall', 'Wall', 'panes', 'Window panes', 'frame', 'Window frame', 'table', 'Table', 'cloth_dots', 'Cloth dots'),
    ('wall', 'Wall', 'wall_dots', 'Wall dots and jars', 'shelf', 'Shelf', 'table', 'Table', 'cloth_dots', 'Cloth dots'),
    ('sky', 'Sky', 'cloud', 'Cloud', 'hill', 'Hill', 'blanket', 'Picnic blanket', 'blanket_dots', 'Blanket dots'),
)


def scene(cv, r, w, h, kind, ty):
    """The setting of `kind` (SCENES), its table or blanket from row ty down: roles b (background), o (window panes,
    wall dots and jars, or a cloud), v (window frame, shelf or hill), t (table or blanket) and d (its dots, kept off
    the bottom row's middle, where the board's entry is)."""
    if kind == 0:
        x0, y0 = w * r.uniform(0.03, 0.09), h * r.uniform(0.04, 0.08)
        x1, y1 = x0 + max(5.0, w * 0.3), y0 + max(5.0, h * 0.22)
        rbox(cv, x0, y0, x1, y1, 0.6, 'v')
        box(cv, x0 + 1, y0 + 1, x1 - 1, y1 - 1, 'o')
        box(cv, (x0 + x1) / 2 - 0.5, y0, (x0 + x1) / 2 + 0.5, y1, 'v')
        box(cv, x0, (y0 + y1) / 2 - 0.5, x1, (y0 + y1) / 2 + 0.5, 'v')
    elif kind == 1:
        step = r.choice((4, 5))
        dots(cv, 'o', 'b', step, step - 1, area=(0, 0, w - 1, ty - 3), offset=r.randrange(step))
        y = h * r.uniform(0.2, 0.24)
        box(cv, w * 0.6, y, w, y + 0.9, 'v')
        poly(cv, [(w * 0.68, y + 0.9), (w * 0.8, y + 0.9), (w * 0.68, y + 2.8)], 'v')
        rbox(cv, w * 0.66, y - 2.6, w * 0.76, y - 0.1, 0.5, 'o')
        rbox(cv, w * 0.82, y - 3.6, w * 0.95, y - 0.1, 0.6, 'o')
    else:
        cloud(cv, w * r.uniform(0.62, 0.8), h * r.uniform(0.07, 0.12), min(w, h) * 0.07 + 0.5, 'o')
        oval(cv, w * r.uniform(-0.05, 0.12), ty + 1.0, w * r.uniform(0.4, 0.55), h * r.uniform(0.16, 0.22), 'v')
    box(cv, 0, ty, w, h, 't')
    top = int(math.ceil(ty - 0.5))
    step, off = (4, 4, 3)[kind], r.randrange(4)
    for y in range(top + 1, h, 2):
        for x in range(w):
            if (x + off + (y - top) // 2 * 2) % step == 0 and not (y == h - 1 and abs(x + 0.5 - w / 2) < 1.5):
                cv.g[y][x] = 'd'


def scene_roles(kind, groups):
    """The setting's roles by character, in the color groups of b, o, v, t and d."""
    n = SCENES[kind]
    return {c: (c, n[2 * k], n[2 * k + 1], g, k == 0) for k, (c, g) in enumerate(zip('bovtd', groups))}


def gap(cv, x, y, rr, only, c='b'):
    """Clears a disc of the roles in `only` back to the background, so a shape drawn in it stands apart."""
    tilted(cv, x, y, rr, rr, 0, c, only=only)


# ---- Fruit ----

def one_orange(cv, x, y, rr, side, big, leaves):
    """An orange: round, its peel dimpled, with a stalk at the top and up to two leaves."""
    disc(cv, x, y, rr, 'a')
    dots(cv, 'e', 'a', 3, 2, area=(x - rr, y - rr, x + rr, y + rr), offset=int(x) % 3)
    if big:
        tilted(cv, x - rr * 0.45, y - rr * 0.45, rr * 0.3, rr * 0.13, -45, 'h')
    seg(cv, x, y - rr * 0.9, x + side * 0.3, y - rr - 0.8, 'k', 0.5)
    if leaves:
        lens(cv, x + side * 0.3, y - rr - 0.5, x + side * rr * 1.15, y - rr * 1.4 - 0.4, rr * 0.42 + 0.3, 'l')
    if leaves > 1:
        lens(cv, x, y - rr - 0.5, x - side * rr * 0.75, y - rr * 1.55 - 0.5, rr * 0.34 + 0.3, 'l')


def wheel(cv, x, y, rr, big):
    """The cut face of an orange: peel and flesh in one, the pith's spokes between eight segments, and on the big
    boards the pith's ring too (one more level of nesting)."""
    x, y = int(x) + 0.5, int(y) + 0.5
    disc(cv, x, y, rr, 'a')
    if big:
        ring(cv, x, y, rr - 0.9, rr - 1.8, 'w')
    for k in range(8):
        a = math.radians(k * 45)
        seg(cv, x, y, x + math.cos(a) * (rr - 1.4), y + math.sin(a) * (rr - 1.4), 'w', 0.42)


def orange(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    mode, kind = r.randrange(3), r.randrange(3)
    ty = h * r.uniform(0.84, 0.88)
    scene(cv, r, w, h, kind, ty)
    side = r.choice((-1, 1))
    if mode == 0:  # one orange with two leaves
        oval(cv, cx, ty, w * 0.44, 1.8, 'p')
        rr = s * 0.31
        one_orange(cv, cx, ty - rr - 0.4, rr, side, big, 2)
    elif mode == 1:  # a whole orange behind a cut half
        oval(cv, cx, ty, w * 0.46, 1.8, 'p')
        rr = s * 0.21
        one_orange(cv, cx + side * w * 0.2, ty - rr - h * 0.2, rr, side, big, 1)
        hr = s * 0.26
        hx, hy = cx - side * w * 0.12, ty - hr - 0.2
        gap(cv, hx, hy, hr + 0.9, 'aeklh')
        disc(cv, hx + side * 1.2, hy - 0.2, hr, 'a')
        wheel(cv, hx, hy, hr, big)
    else:  # three oranges in a bowl
        rr = s * 0.2
        by = ty - h * 0.15
        for k, (dx, dy) in enumerate(((-0.19, 0.0), (0.19, 0.0), (0.0, -1.0))):
            x, y = cx + dx * w, by - rr * 0.3 + dy * rr * 1.55
            if k:
                gap(cv, x, y, rr + 0.8, 'aeklh')
            one_orange(cv, x, y, rr, side, big, 1 if k == 2 else 0)
        bowl(cv, cx, by, ty + 0.4, w * 0.42, 'p')
    if r.random() < 0.5:
        mirror(cv)
    S = scene_roles(kind, (BLUE, BLUE, GREEN, PINK, PINK))
    return cv, [S['b'], role('a', 'orange', 'Oranges', BROWN), role('e', 'dimples', 'Dimples', BROWN), role('l', 'leaf', 'Leaves', GREEN),
                role('k', 'stalk', 'Stalks', GREEN), S['t'], S['d'], role('w', 'pith', 'Pith', PINK), role('p', 'plate', 'Plate and bowl', BLUE),
                role('h', 'shine', 'Shine', BLUE), S['o'], S['v']], ['food', 'fruit']


def one_plum(cv, x, y, rr, side, big, leaf=True):
    """A plum: a round oval with a crease down one side from its stalk, and a leaf."""
    oval(cv, x, y, rr * 0.9, rr, 'a')
    disc(cv, x + side * rr * 0.12, y + rr * 0.1, rr * 0.88, 'a')
    path(cv, curve((x + side * 0.2, y - rr * 0.8), (x + side * rr * 0.3, y + rr * 0.75), side * rr * 0.22), 'k', 0.42)
    if big:
        tilted(cv, x - side * rr * 0.45, y - rr * 0.35, rr * 0.28, rr * 0.13, side * 55, 'h')
    seg(cv, x + side * 0.1, y - rr * 0.85, x - side * 0.4, y - rr - 1.2, 'k', 0.5)
    if leaf:
        lens(cv, x - side * 0.3, y - rr - 0.9, x - side * rr * 1.1, y - rr * 1.35 - 0.6, rr * 0.4 + 0.3, 'l')


def plum(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    mode = r.randrange(3)
    kind = 2 if mode == 2 else r.randrange(2)
    ty = h * r.uniform(0.84, 0.88)
    scene(cv, r, w, h, kind, ty)
    side = r.choice((-1, 1))
    if mode == 0:  # one plum with its leaf
        oval(cv, cx, ty, w * 0.44, 1.8, 'p')
        rr = s * 0.3
        one_plum(cv, cx, ty - rr - 0.4, rr, side, big)
    elif mode == 1:  # two plums behind a cut half with its stone
        oval(cv, cx, ty, w * 0.46, 1.8, 'p')
        rr = s * 0.21
        one_plum(cv, cx + side * w * 0.22, ty - rr - h * 0.12, rr, side, big)
        x = cx - side * w * 0.16
        gap(cv, x, ty - rr - h * 0.2, rr + 0.8, 'akhl')
        one_plum(cv, x, ty - rr - h * 0.2, rr * 0.95, -side, big, leaf=False)
        hx, hy, hr = cx - side * w * 0.04, ty - 1.6, s * 0.24
        gap(cv, hx, hy - hr * 0.2, hr + 0.8, 'akhl')
        cut_half(cv, hx, hy - hr * 0.3, hr, 'a', 'f', 0.5)
        oval(cv, hx, hy - hr * 0.3, hr * 0.32, hr * 0.2, 'k')
    else:  # plums hanging from a branch, one fallen on the blanket
        y0 = h * r.uniform(0.08, 0.14)
        pts = curve((-0.5, y0 + h * 0.08), (w + 0.5, y0), -1.5)
        tube(cv, pts, 0.7 if not big else 1.0, 'k')
        rr = s * 0.17
        for k, u in enumerate((0.3, 0.68)):
            bx, by = pts[int(u * 20)]
            x, y = bx + (k - 0.5) * 1.0, by + rr + h * (0.12 + 0.08 * k)
            seg(cv, bx, by, x, y - rr, 'k', 0.45)
            one_plum(cv, x, y, rr, 1 - 2 * k, big, leaf=False)
        for u, a in ((0.12, -40), (0.5, 35), (0.88, -30)) if big else ((0.5, 35), (0.9, -30)):
            bx, by = pts[int(u * 20)]
            t = math.radians(a + 90)
            lens(cv, bx, by, bx + math.cos(t) * s * 0.28, by + math.sin(t) * s * 0.2, s * 0.12, 'l')
        one_plum(cv, cx + side * w * 0.22, ty - rr * 0.7, rr * 0.9, side, big, leaf=False)
    if r.random() < 0.5:
        mirror(cv)
    S = scene_roles(kind, (BLUE, BLUE, GREEN, BROWN, PINK))
    return cv, [S['b'], S['d'], role('a', 'plum', 'Plums', PINK), role('k', 'stalk', 'Stalks, branch and crease', BROWN), S['t'],
                role('l', 'leaf', 'Leaves', GREEN), role('f', 'flesh', 'Flesh', GREEN), role('p', 'plate', 'Plate', BLUE),
                role('h', 'bloom', 'Bloom', BLUE), S['o'], S['v']], ['food', 'fruit']


def berries(cv, x, y, cols, rows, rr, step, crown='k'):
    """A heap of blueberries, `rows` rows of them over (x, y) and narrowing upwards, in turn of roles a and c, each with
    its crown."""
    out = []
    for row in range(rows):
        n = cols - row
        for k in range(n):
            bx, by = x + (k - (n - 1) / 2) * step, y - row * step * 0.82
            out.append((bx, by, 'a' if (row + k) % 2 == 0 else 'c'))
    for bx, by, c in out:
        disc(cv, bx, by, rr, c)
    for bx, by, c in out:
        cv.put(int(bx), int(by - rr * 0.25), crown)
    return out


def blueberries(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    mode, kind = r.randrange(3), r.randrange(2)
    ty = h * r.uniform(0.84, 0.88)
    scene(cv, r, w, h, kind, ty)
    side = r.choice((-1, 1))
    rr, step = (1.25, 2.5) if not big else (1.5, 3.0)
    if mode == 0:  # a bowl heaped with berries, a sprig on top and a few on the table
        by = ty - h * 0.2
        bx = cx - side * w * 0.06
        berries(cv, bx, by - 0.4, int(w * 0.62 / step), 3, rr, step)
        for t in (0.2, 0.75):
            lens(cv, bx + side * w * 0.02, by - step * 2.4, bx + side * w * (0.2 + 0.1 * t), by - step * 2.4 - h * 0.12 * (1 - t), s * 0.12, 'l')
        seg(cv, bx, by - step * 1.9, bx + side * 0.6, by - step * 2.6, 'n', 0.5)
        bowl(cv, bx, by, ty + 0.4, w * 0.38, 'p')
        for k in range(2 if not big else 3):
            x = bx + side * (w * 0.4 + k * step * 0.9)
            disc(cv, x, ty - rr * 0.4 + (k % 2) * 0.5, rr, 'a' if k % 2 else 'c')
            cv.put(int(x), int(ty - rr * 0.6), 'k')
    elif mode == 1:  # a punnet of berries
        x0, x1 = cx - w * 0.3, cx + w * 0.3
        top = ty - h * 0.25
        berries(cv, cx, top - 0.2, int(w * 0.6 / step) + 1, 3 if not big else 4, rr, step)
        poly(cv, [(x0 - 0.5, top), (x1 + 0.5, top), (x1 - 1.0, ty + 0.5), (x0 + 1.0, ty + 0.5)], 'p')
        for k in range(1, 4 if not big else 5):
            x = x0 + k * (x1 - x0) / (4 if not big else 5)
            seg(cv, x, top + 1.5, cx + (x - cx) * 0.85, ty - 0.6, 'q', 0.42)
        box(cv, x0 - 0.5, top, x1 + 0.5, top + 0.9, 'q')
        lens(cv, cx + side * w * 0.1, top - step * 2.2, cx + side * w * 0.38, top - step * 2.2 - h * 0.08, s * 0.12, 'l')
    else:  # a sprig of berries lying on a plate
        oval(cv, cx, ty, w * 0.46, 1.8, 'p')
        y = ty - h * 0.24
        pts = curve((cx - side * w * 0.42, y - h * 0.22), (cx + side * w * 0.3, y + h * 0.08), side * 1.5)
        path(cv, pts, 'n', 0.5)
        for u, a in ((0.15, -60), (0.45, 50)) if not big else ((0.12, -60), (0.4, 50), (0.65, -45)):
            px, py = pts[int(u * 20)]
            t = math.radians(a - 90 + (0 if side > 0 else 180))
            lens(cv, px, py, px + math.cos(t) * s * 0.3, py + math.sin(t) * s * 0.24, s * 0.14, 'l')
        ex, ey = pts[-1]
        berries(cv, ex - side * 0.5, ey + 0.8, 3, 2, rr, step)
        mx, my = pts[12]
        berries(cv, mx - side * 1.0, my + step * 1.2, 2, 2, rr, step)
    if r.random() < 0.5:
        mirror(cv)
    S = scene_roles(kind, (PINK, PINK, BROWN, GREEN, GREEN))
    return cv, [S['b'], role('a', 'berry', 'Blueberries', BLUE), role('c', 'berry2', 'Other blueberries', BLUE),
                role('k', 'crowns', 'Crowns', PINK), role('l', 'leaf', 'Leaves', GREEN), role('n', 'stem', 'Stems', GREEN),
                role('p', 'bowl', 'Bowl, punnet and plate', BROWN), role('q', 'slats', 'Punnet slats', BROWN),
                S['t'], S['d'], S['o'], S['v']], ['food', 'fruit']


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
    side = r.choice((-1, 1))
    if mode == 0:  # one big raspberry with a leaf
        oval(cv, cx, ty, w * 0.44, 1.8, 'p')
        rr = s * 0.3
        one_raspberry(cv, cx, ty - rr - 0.6, rr, big, side)
    elif mode == 1:  # a bowl of raspberries
        by = ty - h * 0.16
        rr = s * 0.15
        for k, (dx, dy) in enumerate(((-0.2, 0.0), (0.2, 0.0), (0.0, -0.17))):
            x, y = cx + dx * w, by - rr * 0.5 + dy * h
            if k:
                gap(cv, x, y, rr + 0.8, 'aec')
            one_raspberry(cv, x, y, rr, big)
        lens(cv, cx + side * w * 0.04, by - h * 0.17 - rr * 1.2, cx + side * w * 0.36, by - h * 0.3, s * 0.12, 'l')
        bowl(cv, cx, by, ty + 0.4, w * 0.42, 'p')
    else:  # raspberries hanging from a cane, one on the blanket
        y0 = h * r.uniform(0.08, 0.12)
        pts = curve((-0.5, y0), (w + 0.5, y0 + h * 0.1), 1.2)
        tube(cv, pts, 0.6 if not big else 0.9, 'k')
        rr = s * 0.15
        for k, u in enumerate((0.3, 0.7)):
            bx, by = pts[int(u * 20)]
            seg(cv, bx, by, bx, by + h * 0.08, 'k', 0.45)
            one_raspberry(cv, bx, by + h * 0.08 + rr * 1.1, rr, big)
        for u, a in ((0.5, 20), (0.9, -25)) + (((0.1, 30),) if big else ()):
            bx, by = pts[int(u * 20)]
            t = math.radians(a + 90)
            lens(cv, bx, by, bx + math.cos(t) * s * 0.26, by + math.sin(t) * s * 0.22, s * 0.14, 'l')
        one_raspberry(cv, cx + side * w * 0.2, ty - rr * 0.6, rr, big)
    if r.random() < 0.5:
        mirror(cv)
    S = scene_roles(kind, (BLUE, BLUE, GREEN, BROWN, BROWN))
    return cv, [S['b'], role('a', 'berry', 'Raspberries', PINK), role('e', 'drupelets', 'Drupelets', PINK), role('c', 'calyx', 'Sepals and stalks', GREEN),
                role('l', 'leaf', 'Leaves', GREEN), role('k', 'cane', 'Cane', BROWN), role('p', 'plate', 'Plate and bowl', BLUE),
                S['t'], S['d'], S['o'], S['v']], ['food', 'fruit']


def one_mango(cv, x, y, rr, ang, big, leaf=0):
    """A mango lying `ang` degrees from level: a plump oval, fuller at its stalk end, blushed along its back, with a
    stalk and a leaf."""
    t = math.radians(ang)
    ux, uy = math.cos(t), math.sin(t)
    tilted(cv, x, y, rr * 1.2, rr * 0.85, ang, 'a')
    disc(cv, x - ux * rr * 0.45 + uy * rr * 0.1, y - uy * rr * 0.45 - ux * rr * 0.1, rr * 0.82, 'a')
    tilted(cv, x - ux * rr * 0.3 + uy * rr * 0.45, y - uy * rr * 0.3 - ux * rr * 0.45, rr * 0.8, rr * 0.42, ang, 'q', only='a')
    sx, sy = x - ux * rr * 1.1 + uy * rr * 0.35, y - uy * rr * 1.1 - ux * rr * 0.35
    seg(cv, sx, sy, sx - ux * 1.0 + uy * 1.0, sy - uy * 1.0 - ux * 1.0, 'k', 0.5)
    if leaf:
        lens(cv, sx - ux * 0.6 + uy * 0.8, sy - uy * 0.6 - ux * 0.8, sx + leaf * rr * 1.2, sy - rr * 0.75, rr * 0.4 + 0.3, 'l')


def mango_cheek(cv, x, y, rr):
    """A mango cheek cut into cubes and turned out: the skin under a dome of cubes."""
    for xx, yy, px, py in cells(cv):
        if py >= y + rr * 0.15 and ((px - x) / (rr * 1.1)) ** 2 + ((py - y) / (rr * 0.75)) ** 2 <= 1.0:
            cv.g[yy][xx] = 'a'
    step = 2.0 if rr < 4.5 else 2.5
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
    side = r.choice((-1, 1))
    if mode == 0:  # one mango with its leaf
        oval(cv, cx, ty, w * 0.46, 1.8, 'p')
        rr = s * 0.28
        one_mango(cv, cx + w * 0.04, ty - rr * 0.8, rr, r.uniform(-12, -4), big, 1)
    elif mode == 1:  # a whole mango behind a cheek cut into cubes
        oval(cv, cx, ty, w * 0.46, 1.8, 'p')
        rr = s * 0.21
        one_mango(cv, cx + w * 0.12, ty - h * 0.36, rr, -15, big, 1)
        hr = s * 0.25
        hx, hy = cx - w * 0.1, ty - hr * 0.8
        gap(cv, hx, hy, hr + 1.0, 'aqkl')
        mango_cheek(cv, hx, hy, hr)
    else:  # mangoes hanging on long stalks from a branch
        y0 = h * r.uniform(0.06, 0.1)
        pts = curve((-0.5, y0 + h * 0.06), (w + 0.5, y0), -1.0)
        tube(cv, pts, 0.6 if not big else 0.9, 'k')
        rr = s * 0.17
        for k, u in enumerate((0.3, 0.72)):
            bx, by = pts[int(u * 20)]
            y = by + h * (0.24 + 0.1 * k)
            path(cv, curve((bx, by), (bx + 0.6, y - rr), 0.6), 'k', 0.45)
            one_mango(cv, bx + 0.6, y + rr * 0.2, rr, 80, big)
        for u, a in ((0.12, 25), (0.5, -20), (0.55, 30), (0.92, -15)) if big else ((0.5, -20), (0.55, 30), (0.92, -15)):
            bx, by = pts[int(u * 20)]
            t = math.radians(a + 90)
            lens(cv, bx, by, bx + math.cos(t) * s * 0.36, by + math.sin(t) * s * 0.28, s * 0.1 + 0.3, 'l')
        one_mango(cv, cx + w * 0.2, ty - rr * 0.6, rr * 0.9, -8, big)
    if side < 0:
        mirror(cv)
    S = scene_roles(kind, (BLUE, BLUE, GREEN, PINK, PINK))
    return cv, [S['b'], role('a', 'mango', 'Mangoes', BROWN), role('k', 'stalk', 'Stalks and branch', BROWN), role('f', 'cubes', 'Cubes', BROWN),
                S['t'], S['d'], role('q', 'blush', 'Blush and cuts', PINK), role('l', 'leaf', 'Leaves', GREEN),
                role('p', 'plate', 'Plate', BLUE), S['o'], S['v']], ['food', 'fruit']


def one_pomegranate(cv, x, y, rr, big):
    """A pomegranate: round, with its crown of pointed sepals on a short neck."""
    disc(cv, x, y, rr, 'a')
    disc(cv, x + rr * 0.12, y + rr * 0.08, rr * 0.96, 'a')
    top = y - rr * 0.82
    cw = max(1.6, rr * 0.38)
    box(cv, x - cw * 0.6, top - rr * 0.25, x + cw * 0.6, top + 0.6, 'c')
    poly(cv, [(x - cw, top - rr * 0.5), (x - cw * 0.4, top - rr * 0.22), (x, top - rr * 0.58), (x + cw * 0.4, top - rr * 0.22),
              (x + cw, top - rr * 0.5), (x + cw * 0.6, top + 0.4), (x - cw * 0.6, top + 0.4)], 'c')
    if big:
        tilted(cv, x - rr * 0.45, y - rr * 0.3, rr * 0.28, rr * 0.13, -50, 'h')


def pomegranate(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    mode, kind = r.randrange(3), r.randrange(3)
    ty = h * r.uniform(0.84, 0.88)
    scene(cv, r, w, h, kind, ty)
    side = r.choice((-1, 1))
    if mode == 0:  # one pomegranate with a leaf, and a few seeds
        oval(cv, cx, ty, w * 0.44, 1.8, 'p')
        rr = s * 0.3
        y = ty - rr - 0.4
        lens(cv, cx + side * 0.8, y - rr * 0.95, cx + side * w * 0.42, y - rr * 1.25, s * 0.13, 'l')
        one_pomegranate(cv, cx, y, rr, big)
        scatter(cv, 'x', 'p', 3, r, sep=2)
    elif mode == 1:  # a whole one behind a half cut open, its seeds in the pith
        oval(cv, cx, ty, w * 0.46, 1.8, 'p')
        rr = s * 0.22
        one_pomegranate(cv, cx + side * w * 0.18, ty - rr - h * 0.18, rr, big)
        hr = s * 0.27
        hx, hy = cx - side * w * 0.1, ty - hr * 0.75
        gap(cv, hx, hy, hr + 0.9, 'ach')
        cut_half(cv, hx, hy, hr, 'a', 'w', 0.55)
        dots(cv, 'x', 'w', 2, 2, area=(hx - hr, hy - hr, hx + hr, hy + hr), offset=int(hx) % 2)
        scatter(cv, 'x', 'p', 3, r, sep=2)
    else:  # a big and a small one, and loose seeds
        oval(cv, cx, ty, w * 0.46, 1.8, 'p')
        rr = s * 0.26
        one_pomegranate(cv, cx - side * w * 0.1, ty - rr - 0.6, rr, big)
        q = s * 0.17
        x = cx + side * w * 0.27
        gap(cv, x, ty - q - 0.3, q + 0.8, 'ach')
        one_pomegranate(cv, x, ty - q - 0.3, q, big)
        scatter(cv, 'x', 'p', 4, r, sep=2)
    if r.random() < 0.5:
        mirror(cv)
    S = scene_roles(kind, (BLUE, BLUE, GREEN, BROWN, BROWN))
    return cv, [S['b'], role('a', 'fruit', 'Pomegranates', PINK), role('c', 'crown', 'Crowns', PINK), role('x', 'arils', 'Seeds', PINK),
                S['t'], S['d'], role('w', 'pith', 'Pith', BROWN), role('l', 'leaf', 'Leaf', GREEN), role('p', 'plate', 'Plate', BLUE),
                role('h', 'shine', 'Shine', BLUE), S['o'], S['v']], ['food', 'fruit']


MORE_FOOD = [orange, plum, blueberries, raspberry, mango, pomegranate]

# Expansion roles of these subjects (as expansions.ROLES): subject -> {group: [(roleId, new name or None), ...]}.
MORE_FOOD_ROLES = {
    'plum': {'red': [('plum', 'Red plums')]},
    'raspberry': {'red': [('berry', None)]},
    'mango': {'lime': [('mango', None)], 'red': [('blush', None)]},
    'pomegranate': {'red': [('fruit', None), ('arils', None)]},
}

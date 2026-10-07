"""Daily Challenge subjects: food (daily_subjects.py). Fruits, vegetables, treats and dishes the levels never show, in
the style of food_subjects.py: each stands on a table, a plate or the ground, and the shapes that make it (a fruit
against its backdrop, seeds against flesh, a filling between two buns) come from different color groups, so they stay
apart under any mapping. The three pictures of a subject differ in what they show (whole, cut open or a group), their
props and their side.
"""
import math

from picture_kit import (BLUE, BROWN, GREEN, PINK, box, cells, cloud, disc, dots, ground_rows, heart, hills, lens, oval,
                         path, poly, rbox, ring, role, scatter, seg, sky, star, start)


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


def tilted(cv, cx, cy, rx, ry, ang, c, nub=0.0, only=None):
    """An oval turned by `ang` degrees; `nub` adds a point of that radius at both ends of its long axis (a lemon)."""
    ca, sa = math.cos(math.radians(ang)), math.sin(math.radians(ang))
    for x, y, px, py in cells(cv):
        u, v = (px - cx) * ca + (py - cy) * sa, -(px - cx) * sa + (py - cy) * ca
        inside = (u / rx) ** 2 + (v / ry) ** 2 <= 1.0
        if nub and not inside:
            inside = min(math.hypot(u - rx, v), math.hypot(u + rx, v)) <= nub
        if inside and (only is None or cv.g[y][x] in only):
            cv.g[y][x] = c


def window(cv, x0, y0, x1, y1, frame, glass):
    """A window of four panes."""
    rbox(cv, x0, y0, x1, y1, 0.6, frame)
    box(cv, x0 + 1, y0 + 1, x1 - 1, y1 - 1, glass)
    box(cv, (x0 + x1) / 2 - 0.5, y0, (x0 + x1) / 2 + 0.5, y1, frame)
    box(cv, x0, (y0 + y1) / 2 - 0.5, x1, (y0 + y1) / 2 + 0.5, frame)


def bowl(cv, x, rim, bottom, rx, c, band=None):
    """A round bowl from its rim at row `rim` down to `bottom`, rx cells to each side, with a band of role `band` along
    its rim."""
    ry = bottom - rim - 0.8
    for xx, yy, px, py in cells(cv):
        if rim <= py and ((px - x) / rx) ** 2 + ((py - rim) / ry) ** 2 <= 1.0:
            cv.g[yy][xx] = c
    box(cv, x - rx * 0.32, bottom - 1.2, x + rx * 0.32, bottom, c)
    if band:
        box(cv, x - rx, rim, x + rx, rim + 0.9, band)


# The settings: a window over a table, a dotted wall with a shelf over a dotted cloth, a picnic blanket before a
# grassy hill under a cloud.
SETTINGS = (
    ('wall', 'Wall', 'panes', 'Window panes', 'frame', 'Window frame', 'table', 'Table', 'cloth_dots', 'Cloth dots'),
    ('wall', 'Wall', 'wall_dots', 'Wall dots and jar', 'shelf', 'Shelf', 'table', 'Table', 'cloth_dots', 'Cloth dots'),
    ('sky', 'Sky', 'cloud', 'Cloud', 'hill', 'Grassy hill', 'blanket', 'Picnic blanket', 'blanket_dots', 'Blanket dots'),
)


def setting(cv, r, w, h, kind, ty):
    """The food's setting of `kind` (SETTINGS), its table or blanket from row ty down: roles b (background), o (panes,
    wall dots and a jar, or a cloud), v (window frame, shelf or hill), t (table or blanket) and d (its dots)."""
    if kind == 0:
        x0, y0 = w * r.uniform(0.05, 0.12), h * r.uniform(0.05, 0.1)
        window(cv, x0, y0, x0 + w * 0.32, y0 + h * 0.24, 'v', 'o')
    elif kind == 1:
        step = r.choice((4, 5))
        dots(cv, 'o', 'b', step, step - 1, area=(0, 0, w - 1, ty - 3), offset=r.randrange(step))
        y = h * r.uniform(0.2, 0.26)
        box(cv, w * 0.6, y, w, y + 0.9, 'v')
        poly(cv, [(w * 0.7, y + 0.9), (w * 0.82, y + 0.9), (w * 0.7, y + 3.0)], 'v')
        rbox(cv, w * 0.8, y - 3.2, w * 0.94, y - 0.1, 0.6, 'o')
    else:
        cloud(cv, w * r.uniform(0.6, 0.8), h * r.uniform(0.07, 0.13), min(w, h) * 0.07 + 0.5, 'o')
        oval(cv, w * 0.1, ty + 1.0, w * 0.5, r.uniform(4.0, 5.5), 'v')
    box(cv, 0, ty, w, h, 't')
    step = (4, 3, 3)[kind]
    dots(cv, 'd', 't', step, 2, area=(0, ty + 1, w - 1, h - 1), offset=r.randrange(step))


def setting_roles(kind, groups):
    """The setting's roles by character, in the color groups of b, o, v, t and d."""
    n = SETTINGS[kind]
    return {c: (c, n[2 * k], n[2 * k + 1], g, k == 0) for k, (c, g) in enumerate(zip('bovtd', groups))}


# ---- Fruit ----

def one_banana(cv, p0, p1, bend, width, body, tip, gap=None):
    """A banana from its stem at p0 to its tip at p1; `gap` (role, roles) first clears a margin of the banana roles
    round it, so it stands apart from the bananas behind."""
    pts = curve(p0, p1, bend)
    rad = lambda t: max(0.6, width / 2 * math.sin(math.pi * (0.05 + 0.9 * t)) ** 0.55)  # noqa: E731
    if gap:
        tube(cv, pts, lambda t: rad(t) + 0.8, gap[0], only=gap[1])
    tube(cv, pts, rad, body)
    (ax, ay), (bx, by) = pts[0], pts[2]
    L = math.hypot(ax - bx, ay - by) or 1.0
    seg(cv, ax, ay, ax + (ax - bx) / L * 2.4, ay + (ay - by) / L * 2.4, tip, 0.75)
    disc(cv, pts[-1][0], pts[-1][1], 0.8, tip)


def banana(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    mode, kind = r.randrange(3), r.randrange(3)
    ty = h * r.uniform(0.83, 0.87)
    setting(cv, r, w, h, kind, ty)
    oval(cv, cx, ty, w * 0.44, 1.8, 'p')
    if mode == 0:  # one banana across the plate
        one_banana(cv, (w * 0.84, h * r.uniform(0.26, 0.34)), (w * 0.1, h * 0.56), -s * 0.2, s * 0.32, 'y', 'k')
        for k in range(3):
            x, y = w * (0.32 + k * 0.17), h * (0.66 - k * 0.04 * (k - 1))
            seg(cv, x - 0.8, y - 0.8, x + 0.8, y + 0.8, 'e', 0.35)
    elif mode == 1:  # a bunch
        crown = (w * 0.14, h * 0.42)
        ends = [(w * 0.84, ty - 2.6), (w * 0.95, h * 0.54), (w * 0.84, h * 0.3)]
        for k, end in enumerate(ends):
            one_banana(cv, crown, end, 2.6 - k * 0.3, s * 0.25, 'y', 'k', gap=('b', 'yk') if k else None)
        disc(cv, crown[0] - 0.6, crown[1], 1.4, 'k')
    else:  # peeled, standing on its end
        bx = cx + r.uniform(-1.0, 1.0)
        pts = curve((bx - 0.5, ty - 1.0), (bx + 2.0, h * 0.16), -1.2)
        tube(cv, pts[9:], 2.4, 'w')
        tube(cv, pts[:13], lambda t: 1.3 + 1.8 * t, 'y')
        px, py = pts[12]
        for side, (dx, dy) in ((-1, (6.6, 6.0)), (1, (6.2, 5.2))):
            lens(cv, px + side * 2.0, py, px + side * dx, py + dy, 3.0, 'y')
            disc(cv, px + side * dx, py + dy, 0.75, 'k')
        lens(cv, px + 0.2, py + 0.2, px + 1.2, py + 6.2, 2.8, 'e')
        box(cv, bx - 1.5, ty - 1.6, bx + 0.5, ty - 0.4, 'k')
    if r.random() < 0.5:
        mirror(cv)
    S = setting_roles(kind, (BLUE, BLUE, GREEN, BROWN, BROWN))
    return cv, [S['b'], S['t'], role('k', 'stem', 'Stem and tips', BROWN), role('y', 'banana', 'Banana', BROWN),
                role('e', 'peel_lines', 'Peel lines', BROWN), role('w', 'flesh', 'Banana flesh', PINK), role('p', 'plate', 'Plate', GREEN),
                S['v'], S['o'], S['d']], ['food', 'fruit']


def one_lemon(cv, x, y, rx, ry, ang, c, shine=None):
    """A lemon: an oval turned by `ang` degrees with a point at both ends."""
    tilted(cv, x, y, rx, ry, ang, c)
    t = math.radians(ang)
    dx, dy = math.cos(t) * (rx + 1.5), math.sin(t) * (rx + 1.5)
    lens(cv, x - dx, y - dy, x + dx, y + dy, ry * 1.1, c)
    if shine:
        tilted(cv, x - math.cos(t) * rx * 0.35 + math.sin(t) * ry * 0.45, y - math.sin(t) * rx * 0.35 - math.cos(t) * ry * 0.45,
               rx * 0.28, 0.7, ang, shine)


def lemon_cut(cv, x, y, rr, rind, pith):
    """The cut face of a lemon: rind, pith, and eight segments between the pith's spokes (four on a small slice)."""
    disc(cv, x, y, rr, rind)
    disc(cv, x, y, rr - 0.9, pith)
    disc(cv, x, y, rr - 1.8, rind)
    if rr < 5.2:
        x, y = int(x) + 0.5, int(y) + 0.5
        seg(cv, x - rr + 1.4, y, x + rr - 1.4, y, pith, 0.45)
        seg(cv, x, y - rr + 1.4, x, y + rr - 1.4, pith, 0.45)
        return
    for k in range(8):
        a = math.radians(k * 45 + 22.5)
        seg(cv, x, y, x + math.cos(a) * (rr - 1.6), y + math.sin(a) * (rr - 1.6), pith, 0.4)
    disc(cv, x, y, 0.7, pith)


def lemon(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    mode, kind = r.randrange(3), r.randrange(3)
    ty = h * r.uniform(0.83, 0.87)
    setting(cv, r, w, h, kind, ty)
    if mode == 0:  # one lemon with its leaves, and a slice one time in two
        oval(cv, cx, ty, w * 0.42, 1.8, 'p')
        wheel = r.random() < 0.5
        ang = r.uniform(-18, -8) if not wheel else r.uniform(-30, -22)
        x, ly = (cx, ty - h * 0.18) if not wheel else (cx + w * 0.1, ty - h * 0.3)
        top = (x + math.cos(math.radians(ang)) * w * 0.4, ly + math.sin(math.radians(ang)) * w * 0.4)
        seg(cv, top[0] - 0.6, top[1], top[0] + 0.4, top[1] - 2.0, 'k', 0.5)
        lens(cv, top[0], top[1] - 1.6, top[0] - w * 0.36, top[1] - h * 0.16, s * 0.2, 'l')
        if big:
            lens(cv, top[0], top[1] - 1.6, top[0] - w * 0.04, top[1] - h * 0.26, s * 0.15, 'l')
        one_lemon(cv, x, ly, w * 0.33, h * 0.16, ang, 'y', 'h')
        if wheel:
            disc(cv, cx - w * 0.2, ty - 5.4, 6.0, 'b')
            oval(cv, cx, ty, w * 0.42, 1.8, 'p')
            lemon_cut(cv, cx - w * 0.2, ty - 5.0, 5.0, 'y', 'w')
    elif mode == 1:  # a whole lemon behind a cut half
        oval(cv, cx, ty, w * 0.44, 1.8, 'p')
        lx, ly = cx + w * 0.17, ty - h * 0.3
        seg(cv, lx + w * 0.2, ly - h * 0.06, lx + w * 0.24, ly - h * 0.12, 'k', 0.5)
        lens(cv, lx + w * 0.23, ly - h * 0.11, lx - w * 0.05, ly - h * 0.24, s * 0.16, 'l')
        one_lemon(cv, lx, ly, w * 0.25, h * 0.12, 25, 'y', 'h')
        hx, hy, hr = cx - w * 0.12, ty - h * 0.17, s * 0.26
        disc(cv, hx, hy, hr + 0.9, 'b')
        oval(cv, hx + 1.4, hy + 0.2, hr, hr * 0.98, 'y')
        lens(cv, hx + hr - 1.0, hy + 0.2, hx + hr + 2.6, hy + 0.2, 2.2, 'y')
        lemon_cut(cv, hx, hy, hr, 'y', 'w')
    else:  # three lemons in a bowl
        by = ty - h * 0.2
        for k, (dx, dy, a) in enumerate(((-0.2, 0.0, -30), (0.21, 0.0, 30), (0.0, -0.11, 0))):
            x, y = cx + dx * w, by + dy * h
            if k:
                tilted(cv, x, y, w * 0.2 + 0.9, h * 0.1 + 0.9, a, 'b', only='yh')
            one_lemon(cv, x, y, w * 0.19, h * 0.095, a, 'y', 'h')
        seg(cv, cx + 0.5, by - h * 0.2, cx + 1.0, by - h * 0.25, 'k', 0.5)
        lens(cv, cx + 1.0, by - h * 0.24, cx + w * 0.3, by - h * 0.32, s * 0.15, 'l')
        bowl(cv, cx, by + h * 0.05, ty + 0.4, w * 0.44, 'p')
        dots(cv, 'w', 'p', 3, 2, area=(0, by + h * 0.05 + 1, w - 1, ty))
    if r.random() < 0.5:
        mirror(cv)
    S = setting_roles(kind, (BLUE, BLUE, GREEN, BROWN, BROWN))
    return cv, [S['b'], S['t'], role('k', 'stalk', 'Stalk', BROWN), role('y', 'lemon', 'Lemon', BROWN), role('h', 'shine', 'Shine', BROWN),
                role('w', 'pith', 'Pith and bowl dots', PINK), role('l', 'leaf', 'Leaves', GREEN), role('p', 'plate', 'Plate and bowl', BLUE),
                S['o'], S['v'], S['d']], ['food', 'fruit']


def one_peach(cv, x, y, rr, body, blush, side):
    """A peach: round with a dip at its stalk, blushed on one side, with a crease down the other."""
    disc(cv, x - rr * 0.18, y, rr * 0.92, body)
    disc(cv, x + rr * 0.18, y, rr * 0.92, body)
    disc(cv, x, y + rr * 0.08, rr * 0.95, body)
    oval(cv, x - side * rr * 0.35, y - rr * 0.2, rr * 0.4, rr * 0.48, blush)
    path(cv, [(x + side * rr * 0.05, y - rr * 0.85), (x + side * rr * 0.42, y - rr * 0.4), (x + side * rr * 0.5, y + rr * 0.35)], blush, 0.45)


def peach(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    mode, kind = r.randrange(3), r.randrange(3)
    ty = h * r.uniform(0.83, 0.87)
    setting(cv, r, w, h, kind, ty)
    oval(cv, cx, ty, w * 0.44, 1.8, 'p')
    side = r.choice((-1, 1))
    if mode == 0:  # one peach with leaves
        rr = s * 0.33
        x, y = cx, ty - rr - 0.6
        seg(cv, x, y - rr * 0.8, x + 0.6, y - rr * 1.25, 'k', 0.6)
        lens(cv, x + 0.4, y - rr * 1.15, x + w * 0.36, y - rr * 1.55, s * 0.2, 'l')
        lens(cv, x + 0.2, y - rr * 1.1, x - w * 0.2, y - rr * 1.75, s * 0.15, 'l')
        one_peach(cv, x, y, rr, 'a', 'q', side)
    elif mode == 1:  # two peaches
        rr = s * 0.25
        for k, (dx, dy) in enumerate(((0.2, -0.05), (-0.17, 0.0))):
            x, y = cx + dx * w, ty - rr - 0.6 + dy * h
            if k:
                disc(cv, x, y, rr + 0.9, 'b')
            one_peach(cv, x, y, rr, 'a', 'q', side)
            seg(cv, x, y - rr * 0.85, x + 0.5, y - rr * 1.3, 'k', 0.55)
            lens(cv, x + 0.4, y - rr * 1.2, x + side * w * 0.2, y - rr * 1.6, s * 0.13, 'l')
    else:  # a half with its stone, a whole one behind
        rr = s * 0.22
        one_peach(cv, cx + side * w * 0.2, ty - h * 0.3, rr, 'a', 'q', side)
        seg(cv, cx + side * w * 0.2, ty - h * 0.3 - rr * 0.85, cx + side * w * 0.2 + 0.5, ty - h * 0.3 - rr * 1.3, 'k', 0.55)
        lens(cv, cx + side * w * 0.2, ty - h * 0.3 - rr * 1.2, cx + side * w * 0.42, ty - h * 0.42 - rr, s * 0.13, 'l')
        hx, hy, hr = cx - side * w * 0.1, ty - h * 0.15, s * 0.27
        disc(cv, hx, hy, hr + 0.8, 'b')
        disc(cv, hx, hy, hr, 'q')
        disc(cv, hx, hy, hr - 1.0, 'n')
        oval(cv, hx, hy, hr * 0.36, hr * 0.5, 'q')
        seg(cv, hx, hy - hr * 0.4, hx, hy + hr * 0.4, 'k', 0.4)
    if r.random() < 0.5:
        mirror(cv)
    S = setting_roles(kind, (BLUE, BLUE, GREEN, PINK, PINK))
    return cv, [S['b'], role('a', 'peach', 'Peach', BROWN), role('k', 'stalk', 'Stalk and seam', BROWN), role('n', 'flesh', 'Flesh', BROWN),
                role('q', 'blush', 'Blush, crease and stone', PINK), role('l', 'leaf', 'Leaves', GREEN), role('p', 'plate', 'Plate', BLUE),
                S['t'], S['d'], S['o'], S['v']], ['food', 'fruit']


def coconut(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    mode = r.randrange(3)
    gtop = h * r.uniform(0.58, 0.62)
    box(cv, 0, gtop - h * 0.08, w, gtop, 'a')
    hills(cv, 'd', gtop, 0.4, w * 1.5, r.uniform(0, 6))
    px = w * r.uniform(0.84, 0.92)
    crown = (w * r.uniform(0.66, 0.74), h * r.uniform(0.12, 0.16))
    tube(cv, curve((px, gtop + 2.0), crown, -1.5), lambda t: 1.3 - 0.4 * t, 'k')
    for a in (-175, -130, -85, -40, 5):
        t = math.radians(a + r.uniform(-8, 8))
        end = (crown[0] + math.cos(t) * w * 0.34, crown[1] + math.sin(t) * w * 0.26 + 3.0)
        tube(cv, curve(crown, end, 1.6 if end[0] > crown[0] else -1.6), lambda k: 1.5 * (1 - k) + 0.4, 'l')
    for dx in (-0.9, 0.9):
        disc(cv, crown[0] + dx, crown[1] + 1.2, 1.0, 'c')
    if r.random() < 0.5:
        disc(cv, w * 0.15, h * 0.1, s * 0.09, 'u')
    else:
        star(cv, w * r.uniform(0.1, 0.3), h * r.uniform(0.82, 0.9), s * 0.11, 'x', ri=s * 0.045)
    cr = s * 0.22

    def whole(x, y, rr):
        disc(cv, x, y, rr, 'c')
        dots(cv, 'h', 'c', 3, 2, area=(x - rr, y - rr * 0.1, x + rr, y + rr))
        for ex, ey in ((-0.32, -0.5), (0.32, -0.5), (0.0, -0.2)):
            disc(cv, x + ex * rr, y + ey * rr, 0.75, 'e')

    def half(x, y, rr):
        for xx, yy, qx, qy in cells(cv):
            if qy >= y and math.hypot(qx - x, qy - y) <= rr:
                cv.g[yy][xx] = 'c'
        dots(cv, 'h', 'c', 3, 2, area=(x - rr, y + rr * 0.4, x + rr, y + rr))
        oval(cv, x, y, rr, rr * 0.32, 'w')
        oval(cv, x, y, rr * 0.62, rr * 0.16, 'm')

    gy = h * 0.82
    if mode == 0:  # a whole one and a half
        whole(cx - w * 0.22, gy - cr * 0.6, cr)
        half(cx + w * 0.18, gy - cr * 0.7, cr * 1.05)
    elif mode == 1:  # a drink with a straw and a paper umbrella
        x, y = cx - w * 0.08, gy - cr * 0.5
        seg(cv, x + cr * 0.2, y - cr * 0.8, x + w * 0.2, y - h * 0.3, 'r', 0.55)
        seg(cv, x - cr * 0.3, y - cr * 0.8, x - w * 0.12, y - h * 0.28, 'k', 0.4)
        poly(cv, [(x - w * 0.32, y - h * 0.26), (x - w * 0.12, y - h * 0.36), (x + w * 0.08, y - h * 0.26)], 'u')
        whole(x, y, cr * 1.3)
        oval(cv, x, y - cr * 0.95, cr * 0.7, cr * 0.28, 'w')
        oval(cv, x, y - cr * 0.95, cr * 0.42, cr * 0.15, 'm')
    else:  # a whole one behind the cut face of a half
        whole(cx + w * 0.2, gy - cr * 1.2, cr * 0.95)
        x, y, rr = cx - w * 0.1, gy - cr * 0.6, cr * 1.2
        disc(cv, x, y, rr + 0.8, 'd')
        disc(cv, x, y, rr, 'c')
        disc(cv, x, y, rr - 1.0, 'w')
        disc(cv, x, y, rr - 2.4, 'm')
    if r.random() < 0.5:
        mirror(cv)
    return cv, [sky(), role('a', 'sea', 'Sea', BLUE), role('m', 'milk', 'Coconut water', BLUE),
                role('k', 'trunk', 'Palm trunk and stick', BROWN), role('c', 'shell', 'Coconut shell', BROWN), role('u', 'sun', 'Sun and paper umbrella', BROWN),
                role('h', 'fibres', 'Fibres', BROWN), role('w', 'flesh', 'Coconut flesh', PINK), role('e', 'eyes', 'Coconut eyes', PINK),
                role('r', 'straw', 'Straw', PINK), role('x', 'starfish', 'Starfish', PINK),
                role('l', 'palm', 'Palm leaves', GREEN), role('d', 'sand', 'Sand', GREEN)], ['food', 'summer']


def avocado_half(cv, x, y, rr, ang, flesh, skin, pit=None, hollow=None):
    """A halved avocado, its narrow end turned `ang` degrees from straight up, with its stone or the stone's hollow
    (skin only when flesh is None: a whole one)."""
    t = math.radians(ang)
    top = (x + math.sin(t) * rr * 1.35, y - math.cos(t) * rr * 1.35)
    for c, d in ((skin, 0.0), (flesh, 1.0)):
        if c:
            tube(cv, [(x, y), top], lambda k: rr - d - rr * 0.5 * k ** 0.8, c)
    if pit:
        disc(cv, x, y + rr * 0.05, rr * 0.5, pit)
    elif hollow:
        disc(cv, x, y + rr * 0.05, rr * 0.45, hollow)


def avocado(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    mode, kind = r.randrange(3), r.randrange(2)
    ty = h * r.uniform(0.83, 0.87)
    setting(cv, r, w, h, kind, ty)
    if mode == 0:  # both halves
        oval(cv, cx, ty, w * 0.46, 1.8, 'p')
        avocado_half(cv, cx - w * 0.22, ty - h * 0.17, s * 0.2, -12, 'f', 'g', hollow='e')
        avocado_half(cv, cx + w * 0.21, ty - h * 0.17, s * 0.2, 12, 'f', 'g', pit='k', hollow='e')
    elif mode == 1:  # a whole one behind a half
        oval(cv, cx, ty, w * 0.46, 1.8, 'p')
        x, y = cx + w * 0.2, ty - h * 0.2
        avocado_half(cv, x, y, s * 0.2, 25, None, 'g')
        seg(cv, x + s * 0.24, y - s * 0.24, x + s * 0.3, y - s * 0.34, 'k', 0.5)
        disc(cv, x - s * 0.06, y - s * 0.06, 0.8, 'e')
        avocado_half(cv, cx - w * 0.12, ty - h * 0.17, s * 0.23, -15, None, 'b')
        avocado_half(cv, cx - w * 0.12, ty - h * 0.17, s * 0.21, -15, 'f', 'g', pit='k', hollow='e')
    else:  # a big half on a board
        rbox(cv, cx - w * 0.4, ty - 1.4, cx + w * 0.4, ty + 0.6, 0.7, 'p')
        avocado_half(cv, cx, ty - h * 0.21, s * 0.28, r.uniform(-20, 20), 'f', 'g', pit='k', hollow='e')
    if r.random() < 0.5:
        mirror(cv)
    S = setting_roles(kind, (BLUE, BLUE, GREEN, BROWN, BROWN))
    return cv, [S['b'], role('f', 'flesh', 'Flesh', GREEN), role('g', 'skin', 'Skin', GREEN), S['t'],
                role('k', 'stone', 'Stone and stalk', BROWN), role('e', 'hollow', 'Stone hollow and shine', BROWN),
                role('p', 'plate', 'Plate and board', PINK), S['o'], S['v'], S['d']], ['food', 'fruit']


# ---- Vegetables ----

def one_carrot(cv, p0, p1, rr, body, ridge, leaf, leaf2, bend=0.0, spread=35):
    """A carrot from its top at p0 to its tip at p1, with ridges and a tuft of leaves."""
    pts = curve(p0, p1, bend)
    tube(cv, pts, lambda t: max(0.45, rr * (1 - t ** 1.3)), body)
    for k in range(1, 4):
        x, y = pts[k * 4]
        nx, ny = pts[k * 4 + 1][1] - y, x - pts[k * 4 + 1][0]
        L = math.hypot(nx, ny) or 1.0
        q = rr * (1 - (k * 0.2) ** 1.3) * 0.8
        seg(cv, x - nx / L * q, y - ny / L * q, x + nx / L * q * 0.1, y + ny / L * q * 0.1, ridge, 0.35)
    (ax, ay), (bx, by) = pts[0], pts[3]
    L = math.hypot(ax - bx, ay - by) or 1.0
    ux, uy = (ax - bx) / L, (ay - by) / L
    for k, a in enumerate((-spread, 0, spread)):
        t = math.radians(a)
        dx, dy = ux * math.cos(t) - uy * math.sin(t), ux * math.sin(t) + uy * math.cos(t)
        lens(cv, ax - ux, ay - uy, ax + dx * rr * 2.8, ay + dy * rr * 2.8, rr * 0.9, leaf if k % 2 == 0 else leaf2)


def carrot(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    mode, kind = r.randrange(3), r.randrange(3)
    ty = h * r.uniform(0.83, 0.87)
    setting(cv, r, w, h, kind, ty)
    if mode == 0:  # one carrot on a board
        rbox(cv, cx - w * 0.44, ty - 1.4, cx + w * 0.44, ty + 0.6, 0.7, 'p')
        one_carrot(cv, (w * 0.3, h * 0.36), (w * 0.86, ty - 2.0), s * 0.17, 'c', 'r', 'l', 'g', bend=1.0)
    elif mode == 1:  # a bunch tied with string, standing on the plate
        oval(cv, cx, ty, w * 0.46, 1.8, 'p')
        spread = r.uniform(0.18, 0.24)
        for k in (0, 2, 1):
            q0, tip = (cx + (k - 1) * w * 0.12, h * 0.36 - 1.2 * (k == 1)), (cx + (k - 1) * w * spread, ty - 1.2)
            if k == 1:
                tube(cv, [q0, tip], lambda t: s * 0.14 * (1 - t ** 1.3) + 0.8, 'r', only='c')
            one_carrot(cv, q0, tip, s * 0.14, 'c', 'r', 'l', 'g', spread=28)
        box(cv, cx - w * 0.3, h * 0.4, cx + w * 0.3, h * 0.43, 'k', only='cr')
    else:  # two crossed on a plate
        oval(cv, cx, ty, w * 0.44, 1.8, 'p')
        a, b = r.uniform(0.14, 0.24), r.uniform(0.28, 0.36)
        one_carrot(cv, (w * a, h * b), (w * (1 - a), ty - 1.4), s * 0.13, 'c', 'r', 'l', 'g', bend=-0.6)
        tube(cv, curve((w * (1 - a), h * b), (w * a, ty - 1.4), 0.6), lambda t: s * 0.13 * (1 - t ** 1.3) + 0.8, 'r', only='c')
        one_carrot(cv, (w * (1 - a), h * b), (w * a, ty - 1.4), s * 0.13, 'c', 'r', 'l', 'g', bend=0.6)
    if r.random() < 0.5:
        mirror(cv)
    S = setting_roles(kind, (BLUE, BLUE, GREEN, PINK, PINK))
    return cv, [S['b'], role('c', 'carrot', 'Carrots', BROWN), role('r', 'ridges', 'Ridges', BROWN),
                role('l', 'leaves', 'Leaves', GREEN), role('g', 'leaves2', 'Other leaves', GREEN), S['t'], S['d'],
                role('k', 'string', 'String', PINK), role('p', 'board', 'Board and plate', BLUE), S['o'], S['v']], ['food', 'vegetables']


def corn_cob(cv, x, y0, y1, rr, ang, cob, groove, husk, husk2, silk):
    """A cob of corn from its stalk end at (x, y1), y1 - y0 long and leaning `ang` degrees, with rows of kernels, silk
    at its top and its husk folded back round the stalk end."""
    t = math.radians(ang)
    L = y1 - y0
    ux, uy = math.sin(t), -math.cos(t)
    top = (x + ux * L, y1 + uy * L)
    tube(cv, [top, (x, y1)], lambda k: rr * min(1.0, 0.6 + 0.7 * k), cob)
    across = abs(ux) > 0.7
    for yy in range(1, cv.h - 1):
        for xx in range(1, cv.w - 1):
            if cv.g[yy][xx] == cob and all(cv.g[yy + b][xx + a] in (cob, groove) for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                if (xx % 2 == 1 and yy % 3 != 0) if not across else (yy % 2 == 1 and xx % 3 != 0):
                    cv.g[yy][xx] = groove
    for k in (-1, 0, 1):
        seg(cv, top[0] + ux * 0.5, top[1] + uy * 0.5, top[0] + ux * 2.0 + k * uy * 1.2, top[1] + uy * 2.0 - k * ux * 1.2, silk, 0.4)
    px, py = -uy, ux
    for side, c in ((-1, husk), (1, husk2)):
        lens(cv, x - ux * 0.5, y1 - uy * 0.5, x + ux * L * 0.55 + side * px * rr * 1.5, y1 + uy * L * 0.55 + side * py * rr * 1.5, rr * 1.0, c)
    tube(cv, [(x, y1), (x - ux * 2.6, y1 - uy * 2.6)], 0.9, husk)


def corn(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    mode, kind = r.randrange(3), r.randrange(3)
    ty = h * r.uniform(0.83, 0.87)
    setting(cv, r, w, h, kind, ty)
    if mode == 0:  # one cob standing on a plate
        oval(cv, cx, ty, w * 0.44, 1.8, 'p')
        corn_cob(cv, cx + r.uniform(-1.5, 1.5), h * 0.12, ty - 3.2, s * 0.17, r.uniform(-15, 15), 'y', 'n', 'g', 'l', 'k')
    elif mode == 1:  # two cobs and a pat of butter
        oval(cv, cx, ty, w * 0.46, 1.8, 'p')
        corn_cob(cv, cx - w * 0.04, h * 0.18, ty - 3.0, s * 0.14, -22, 'y', 'n', 'g', 'l', 'k')
        tube(cv, [(cx + w * 0.04, ty - 3.0), (cx + w * 0.04 + 6.5, h * 0.22)], s * 0.14 + 1.0, 'b', only='yngl')
        corn_cob(cv, cx + w * 0.04, h * 0.18, ty - 3.0, s * 0.14, 22, 'y', 'n', 'l', 'g', 'k')
        rbox(cv, cx + w * 0.22, ty - 2.6, cx + w * 0.38, ty - 0.6, 0.4, 'u')
    else:  # a cob lying across a long plate, a smaller one behind
        oval(cv, cx, ty - 0.4, w * 0.46, 2.4, 'p')
        corn_cob(cv, w * 0.3, h * 0.38, h * 0.7, s * 0.11, 55, 'y', 'n', 'l', 'g', 'k')
        tube(cv, [(w * 0.16, ty - 4.2), (w * 0.88, ty - 6.5)], s * 0.16 + 1.0, 'b', only='yngl')
        corn_cob(cv, w * 0.16, ty - 20.0, ty - 4.2, s * 0.16, 74, 'y', 'n', 'g', 'l', 'k')
    if r.random() < 0.5:
        mirror(cv)
    S = setting_roles(kind, (BLUE, BLUE, GREEN, PINK, PINK))
    return cv, [S['b'], role('n', 'kernels', 'Kernel rows', BROWN), role('k', 'silk', 'Silk', BROWN), role('y', 'cob', 'Cob', BROWN),
                role('u', 'butter', 'Butter', BROWN), role('g', 'husk', 'Husk and stalk', GREEN), role('l', 'husk2', 'Inner husk', GREEN),
                role('p', 'plate', 'Plate', BLUE), S['t'], S['d'], S['o'], S['v']], ['food', 'vegetables']


def broccoli_head(cv, x, y, rr, head, bumps, stalk):
    """A head of broccoli whose stalk stands at (x, y): a wide dome of florets, each outlined in the bumps' role, on a
    short, thick stalk that branches into them."""
    hy = y - rr * 1.05
    tube(cv, [(x, y), (x, hy)], lambda t: rr * (0.4 - 0.12 * t), stalk)
    for side in (-1, 1):
        seg(cv, x, y - rr * 0.4, x + side * rr * 0.7, hy - rr * 0.05, stalk, rr * 0.14)
    florets = ((-0.5, -0.55, 0.44), (0.5, -0.55, 0.44), (0.0, -0.72, 0.42), (-0.95, -0.12, 0.36), (0.95, -0.12, 0.36),
               (-0.42, -0.08, 0.38), (0.42, -0.08, 0.38))
    for dx, dy, q in florets:
        fx, fy = x + dx * rr, hy + dy * rr
        disc(cv, fx, fy, q * rr + 0.5, bumps)
        disc(cv, fx, fy, q * rr - 0.2, head)


def broccoli(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    mode, kind = r.randrange(3), r.randrange(3)
    ty = h * r.uniform(0.83, 0.87)
    setting(cv, r, w, h, kind, ty)
    if mode == 0:  # one big head on a board
        rbox(cv, cx - w * 0.44, ty - 1.4, cx + w * 0.44, ty + 0.6, 0.7, 'p')
        broccoli_head(cv, cx + r.uniform(-1.0, 1.0), ty - 1.0, s * 0.33, 'g', 'h', 'q')
    elif mode == 1:  # a big and a small head on a plate
        oval(cv, cx, ty, w * 0.46, 1.8, 'p')
        broccoli_head(cv, cx + w * 0.27, ty - 0.8, s * 0.17, 'g', 'h', 'q')
        broccoli_head(cv, cx - w * 0.1, ty - 0.8, s * 0.26, 'g', 'h', 'q')
    else:  # florets in a bowl
        by = ty - h * 0.14
        broccoli_head(cv, cx, by + 3.5, s * 0.3, 'g', 'h', 'q')
        bowl(cv, cx, by, ty + 0.4, w * 0.44, 'p', 'e')
    if r.random() < 0.5:
        mirror(cv)
    S = setting_roles(kind, (PINK, PINK, BLUE, BROWN, BROWN))
    return cv, [S['b'], role('g', 'florets', 'Florets', GREEN), role('h', 'outlines', 'Floret outlines', GREEN), role('q', 'stalk', 'Stalk', GREEN),
                role('p', 'board', 'Board, plate and bowl', BLUE), role('e', 'rim', 'Bowl rim', BLUE),
                S['t'], S['d'], S['o'], S['v']], ['food', 'vegetables']


def tomato(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    mode, kind = r.randrange(3), r.randrange(3)
    ty = h * r.uniform(0.83, 0.87)
    setting(cv, r, w, h, kind, ty)

    def whole(x, y, rr):
        oval(cv, x, y, rr * 1.08, rr * 0.92, 'a')
        for dx in (-0.45, 0.45):
            disc(cv, x + dx * rr, y - rr * 0.35, rr * 0.55, 'a')
        tilted(cv, x - rr * 0.48, y - rr * 0.25, rr * 0.22, rr * 0.12, -40, 'h')
        star(cv, x, y - rr * 0.82, rr * 0.48, 'c', ri=rr * 0.16, points=5, rot=-90 + r.uniform(-15, 15))
        seg(cv, x, y - rr * 0.85, x + 0.6, y - rr * 1.25, 'c', 0.55)

    def cut(x, y, rr):
        disc(cv, x, y, rr, 'a')
        for k in range(3):
            a = math.radians(90 + k * 120)
            jx, jy = x + math.cos(a) * rr * 0.45, y + math.sin(a) * rr * 0.45
            disc(cv, jx, jy, rr * 0.33, 'j')
            for dx, dy in ((-0.5, 0.0), (0.5, 0.1), (0.0, -0.5)):
                cv.put(int(jx + dx * rr * 0.3), int(jy + dy * rr * 0.3), 's')

    if mode == 0:  # one big tomato
        oval(cv, cx, ty, w * 0.44, 1.8, 'p')
        rr = s * 0.32
        whole(cx, ty - rr * 0.9, rr)
    elif mode == 1:  # a whole one behind a cut half
        oval(cv, cx, ty, w * 0.46, 1.8, 'p')
        whole(cx + w * 0.18, ty - h * 0.24, s * 0.22)
        x, y, rr = cx - w * 0.13, ty - s * 0.24, s * 0.25
        disc(cv, x, y, rr + 0.9, 'b')
        disc(cv, x, y + 0.2, rr, 'a')
        cut(x, y, rr)
    else:  # cherry tomatoes on the vine, on a board
        rbox(cv, cx - w * 0.44, ty - 1.4, cx + w * 0.44, ty + 0.6, 0.7, 'p')
        vy = ty - h * r.uniform(0.36, 0.42)
        pts = curve((w * 0.04, vy + 2.0), (w * 0.96, vy + 1.0), -2.5)
        path(cv, pts, 'c', 0.55)
        for u, dy in ((0.2, 0.0), (0.5, -1.2), (0.8, 0.0)):
            px, py = pts[int(u * 20)]
            x, y = w * u, ty - 1.2 - s * 0.15 + dy
            seg(cv, px, py, x, y - s * 0.15, 'c', 0.45)
            whole(x, y, s * 0.15)
    if r.random() < 0.5:
        mirror(cv)
    S = setting_roles(kind, (BLUE, BLUE, GREEN, BROWN, BROWN))
    return cv, [S['b'], role('a', 'tomato', 'Tomato', PINK), role('c', 'calyx', 'Calyx and vine', GREEN), S['t'], S['d'],
                role('j', 'gel', 'Seed chambers', BROWN), role('s', 'seeds', 'Seeds', GREEN), role('p', 'plate', 'Plate and board', BLUE),
                role('h', 'shine', 'Shine', BLUE), S['o'], S['v']], ['food', 'vegetables']


def one_eggplant(cv, top, bottom, rr, bend, body, cap, stem, shine=None):
    """An eggplant from its stem at `top` to its round end at `bottom`, `rr` cells in radius there, under a green cap."""
    pts = curve(top, bottom, bend)
    tube(cv, pts, lambda t: rr * (0.42 + 0.58 * math.sin(math.pi / 2 * min(1.0, t * 1.25))), body)
    (ax, ay), (bx, by) = pts[0], pts[3]
    L = math.hypot(bx - ax, by - ay) or 1.0
    ux, uy = (bx - ax) / L, (by - ay) / L
    for a in (-55, -20, 20, 55):
        t = math.radians(a)
        dx, dy = ux * math.cos(t) - uy * math.sin(t), ux * math.sin(t) + uy * math.cos(t)
        lens(cv, ax - ux, ay - uy, ax + dx * rr * 1.25, ay + dy * rr * 1.25, rr * 0.5, cap)
    disc(cv, ax, ay, rr * 0.42, cap)
    tube(cv, [(ax, ay), (ax - ux * 2.4 - uy * 0.8, ay - uy * 2.4 + ux * 0.8)], 0.7, stem)
    if shine:
        x, y = pts[13]
        seg(cv, x - uy * rr * 0.5, y + ux * rr * 0.5, pts[17][0] - uy * rr * 0.55, pts[17][1] + ux * rr * 0.55, shine, 0.5)


def eggplant(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    mode, kind = r.randrange(3), r.randrange(3)
    ty = h * r.uniform(0.83, 0.87)
    setting(cv, r, w, h, kind, ty)
    if mode == 0:  # one big eggplant across a board
        rbox(cv, cx - w * 0.44, ty - 1.4, cx + w * 0.44, ty + 0.6, 0.7, 'p')
        one_eggplant(cv, (w * 0.2, h * r.uniform(0.2, 0.28)), (w * 0.66, ty - s * 0.26), s * 0.26, 1.5, 'e', 'c', 'k', 'h')
    elif mode == 1:  # a big and a small one on a plate
        oval(cv, cx, ty, w * 0.46, 1.8, 'p')
        a = r.uniform(0.26, 0.36)
        one_eggplant(cv, (w * 0.74, h * a), (w * 0.8, ty - s * 0.18), s * 0.18, -1.0, 'e', 'c', 'k', 'h')
        top = (w * r.uniform(0.1, 0.3), h * r.uniform(0.14, 0.24))
        tube(cv, curve(top, (w * 0.4, ty - s * 0.23), 1.2), lambda t: s * 0.23 + 0.9, 'b', only='eckh')
        one_eggplant(cv, top, (w * 0.4, ty - s * 0.23), s * 0.23, 1.2, 'e', 'c', 'k', 'h')
    else:  # a whole one and a half, cut lengthwise
        rbox(cv, cx - w * 0.44, ty - 1.4, cx + w * 0.44, ty + 0.6, 0.7, 'p')
        one_eggplant(cv, (w * 0.7, h * 0.16), (w * 0.72, ty - s * 0.2), s * 0.2, -1.0, 'e', 'c', 'k', 'h')
        top, bottom = (w * 0.22, h * 0.34), (w * 0.34, ty - s * 0.2)
        tube(cv, curve(top, bottom, 0.8), lambda t: s * 0.2 * (0.42 + 0.58 * math.sin(math.pi / 2 * min(1.0, t * 1.25))) + 0.9, 'b', only='eckh')
        one_eggplant(cv, top, bottom, s * 0.2, 0.8, 'e', 'c', 'k')
        tube(cv, curve(top, bottom, 0.8)[3:], lambda t: max(0.5, s * 0.2 * (0.5 + 0.5 * math.sin(math.pi / 2 * min(1.0, t * 1.4))) - 1.0), 'f')
        dots(cv, 'x', 'f', 2, 2, area=(0, h * 0.5, w - 1, ty))
    if r.random() < 0.5:
        mirror(cv)
    S = setting_roles(kind, (BLUE, BLUE, GREEN, PINK, PINK))
    return cv, [S['b'], S['t'], role('e', 'eggplant', 'Eggplant', PINK), role('h', 'shine', 'Shine', PINK), S['d'],
                role('c', 'cap', 'Cap', GREEN), role('k', 'stem', 'Stem', GREEN), role('f', 'flesh', 'Flesh', BROWN),
                role('x', 'seeds', 'Seeds', BROWN), role('p', 'board', 'Board and plate', BLUE), S['o'], S['v']], ['food', 'vegetables']


def one_chili(cv, top, tip, rr, bend, body, cap, stem):
    """A chili pepper from its cap at `top` to its curled tip."""
    pts = curve(top, tip, bend)
    tube(cv, pts, lambda t: max(0.5, rr * (1 - t ** 1.6)), body)
    (ax, ay), (bx, by) = pts[0], pts[2]
    L = math.hypot(bx - ax, by - ay) or 1.0
    ux, uy = (bx - ax) / L, (by - ay) / L
    tube(cv, [(ax - ux * 0.6, ay - uy * 0.6), (ax + ux * 0.3, ay + uy * 0.3)], rr * 0.8, cap)
    tube(cv, curve((ax - ux, ay - uy), (ax - ux * 3.0 + uy * 1.6, ay - uy * 3.0 - ux * 1.6), 0.8), 0.6, stem)


def chili_pepper(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    mode, kind = r.randrange(3), r.randrange(3)
    ty = h * r.uniform(0.83, 0.87)
    setting(cv, r, w, h, kind, ty)
    if mode == 0:  # one big chili on a plate
        oval(cv, cx, ty, w * 0.44, 1.8, 'p')
        one_chili(cv, (w * 0.2, h * r.uniform(0.22, 0.3)), (w * 0.84, ty - 2.6), s * 0.17, r.uniform(2.5, 4.0), 'a', 'c', 'k')
    elif mode == 1:  # a red and an orange one crossing
        oval(cv, cx, ty, w * 0.46, 1.8, 'p')
        one_chili(cv, (w * 0.82, h * 0.2), (w * 0.2, ty - 2.4), s * 0.14, -2.5, 'q', 'c', 'k')
        tube(cv, curve((w * 0.16, h * 0.3), (w * 0.86, ty - 2.2), 3.0), lambda t: max(0.5, s * 0.14 * (1 - t ** 1.6)) + 0.9, 'b', only='qck')
        one_chili(cv, (w * 0.16, h * 0.3), (w * 0.86, ty - 2.2), s * 0.14, 3.0, 'a', 'c', 'k')
    else:  # three side by side
        oval(cv, cx, ty, w * 0.46, 1.8, 'p')
        dy = r.uniform(0.0, 0.06) * h
        for k, c in enumerate('qaq'):
            top, tip = (w * 0.2, h * 0.24 + k * h * 0.15), (w * 0.86, h * 0.36 + k * h * 0.13 + dy)
            if k:
                tube(cv, curve(top, tip, 1.2), lambda t: max(0.5, s * 0.1 * (1 - t ** 1.6)) + 0.9, 'b', only='aqck')
            one_chili(cv, top, tip, s * 0.1, 1.2, c, 'c', 'k')
    if r.random() < 0.5:
        mirror(cv)
    S = setting_roles(kind, (BLUE, BLUE, GREEN, BROWN, BROWN))
    return cv, [S['b'], role('a', 'chili', 'Red chili', PINK), role('c', 'cap', 'Caps', GREEN), role('k', 'stem', 'Stems', GREEN),
                role('q', 'chili2', 'Orange chili', BROWN), S['t'], S['d'], role('p', 'plate', 'Plate', PINK), S['o'], S['v']], ['food', 'vegetables']


def pod(cv, p0, p1, width, shell, inside, pea, n, bend=0.0, open_=True):
    """A pea pod from its stalk end p0 to its tip p1, open with n peas in a row, or closed."""
    pts = curve(p0, p1, bend)
    tube(cv, pts, lambda t: max(0.6, width / 2 * math.sin(math.pi * (0.04 + 0.92 * t)) ** 0.7), shell)
    tube(cv, [(p0[0], p0[1]), (p0[0] - (pts[2][0] - p0[0]), p0[1] - (pts[2][1] - p0[1]))], 0.6, shell)
    if open_:
        tube(cv, pts[2:-2], lambda t: max(0.4, (width / 2 - 0.9) * math.sin(math.pi * (0.04 + 0.92 * t)) ** 0.7), inside)
        for k in range(n):
            x, y = pts[4 + int(k * 12 / max(1, n - 1))]
            disc(cv, x, y, min(1.6, width * 0.2), pea)


def pea_pod(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    mode, kind = r.randrange(3), r.randrange(2)
    ty = h * r.uniform(0.83, 0.87)
    setting(cv, r, w, h, kind, ty)
    if mode == 0:  # one big open pod
        oval(cv, cx, ty, w * 0.44, 1.8, 'p')
        pod(cv, (w * 0.08, h * r.uniform(0.3, 0.38)), (w * 0.94, h * 0.6), s * 0.38, 'g', 'i', 'e', 5, bend=2.5)
    elif mode == 1:  # an open and a closed pod, peas rolling
        oval(cv, cx, ty, w * 0.46, 1.8, 'p')
        pod(cv, (w * 0.86, h * 0.24), (w * 0.2, h * 0.42), s * 0.26, 'g', 'i', 'e', 0, bend=-1.5, open_=False)
        tube(cv, curve((w * 0.1, h * 0.5), (w * 0.9, h * 0.66), 2.0), lambda t: s * 0.17 + 0.9, 'b', only='gie')
        pod(cv, (w * 0.1, h * 0.5), (w * 0.9, h * 0.66), s * 0.34, 'g', 'i', 'e', 5, bend=2.0)
        for x in (0.3, 0.46, 0.64):
            disc(cv, w * x, ty - 1.6, s * 0.075, 'e')
    else:  # a bowl of peas and a pod
        by = ty - h * 0.18
        for k in range(17):
            row, col = k // 6, k % 6
            disc(cv, cx + (col - 2.5) * 2.8 + (row % 2) * 1.4, by - row * 2.2 + 0.6, 1.3, 'e' if (row + col) % 2 else 'g')
        bowl(cv, cx, by, ty + 0.4, w * 0.44, 'p')
        pod(cv, (w * 0.2, h * 0.14), (w * 0.86, h * 0.3), s * 0.3, 'g', 'i', 'e', 5, bend=1.5)
    if r.random() < 0.5:
        mirror(cv)
    S = setting_roles(kind, (BLUE, BLUE, GREEN, BROWN, BROWN))
    return cv, [S['b'], role('e', 'peas', 'Peas', GREEN), role('g', 'pod', 'Pod', GREEN), S['t'], S['d'],
                role('i', 'inside', 'Inside of the pod', BROWN), role('p', 'plate', 'Plate and bowl', PINK), S['o'], S['v']], ['food', 'vegetables']


# ---- Treats ----

def cookie_top(cv, r, x, y, rr, dough, chip, bite=None):
    """A chocolate chip cookie seen from above, a bite out of it when `bite` (the background's role) is given."""
    for xx, yy, px, py in cells(cv):
        a = math.atan2(py - y, px - x)
        if math.hypot(px - x, py - y) <= rr * (1 + 0.05 * math.sin(a * 7)):
            cv.g[yy][xx] = dough
    if bite:
        a = r.uniform(-1.2, -0.3)
        for k in (-1, 0, 1):
            disc(cv, x + math.cos(a + k * 0.35) * rr * 1.02, y + math.sin(a + k * 0.35) * rr * 1.02, rr * 0.24, bite)
    scatter(cv, chip, dough, int(rr * rr * 0.28), r, sep=2, area=(x - rr, y - rr, x + rr, y + rr))


def glass_of_milk(cv, x, y0, y1, rr, glass, milk):
    """A glass of milk from its rim at y0 to the table at y1."""
    rbox(cv, x - rr, y0, x + rr, y1, 0.6, glass)
    box(cv, x - rr + 1.0, y0 + 1.5, x + rr - 1.0, y1 - 1.0, milk)


def cookie(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    mode, kind = r.randrange(3), r.randrange(3)
    milk, bite = r.random() < 0.5, r.random() < 0.6
    ty = h * r.uniform(0.83, 0.87)
    setting(cv, r, w, h, kind, ty)
    px, rx = (cx - w * 0.12, w * 0.34) if milk else (cx, w * 0.44)
    if milk:
        glass_of_milk(cv, cx + w * 0.3, h * r.uniform(0.38, 0.46), ty - 0.6, s * 0.14, 'g', 'm')
    oval(cv, px, ty, rx, 1.8, 'p')
    if mode == 0:  # a stack
        n = r.choice((3, 4))
        for k in range(n):
            y = ty - 1.6 - k * 2.6
            oval(cv, px + r.uniform(-0.8, 0.8), y, rx * 0.78, 2.0, 'c')
            oval(cv, px, y - 0.7, rx * 0.68, 1.0, 'e')
            for dx in (-0.5, 0.0, 0.45):
                cv.put(int(px + dx * rx + k % 2), int(y + 0.8), 'k')
        cookie_top(cv, r, px, ty - 1.6 - n * 2.6 - rx * 0.4, rx * 0.72, 'c', 'k', bite='b' if bite else None)
    elif mode == 1:  # one big cookie
        cookie_top(cv, r, px, ty - rx * 0.95, rx * 0.95, 'c', 'k', bite='b' if bite else None)
        scatter(cv, 'c', 'p', 3, r, sep=3)
    else:  # three cookies, one leaning on the others
        q = rx * 0.5
        cookie_top(cv, r, px - q * 0.95, ty - q * 0.9, q, 'c', 'k')
        disc(cv, px + q * 0.95, ty - q * 0.9, q + 0.8, 'b')
        cookie_top(cv, r, px + q * 0.95, ty - q * 0.9, q, 'c', 'k')
        disc(cv, px, ty - q * 2.3, q + 0.8, 'b')
        cookie_top(cv, r, px, ty - q * 2.3, q, 'c', 'k', bite='b' if bite else None)
    if r.random() < 0.5:
        mirror(cv)
    S = setting_roles(kind, (BLUE, BLUE, GREEN, PINK, PINK))
    return cv, [S['b'], role('c', 'cookie', 'Cookies', BROWN), role('e', 'top', 'Cookie tops', BROWN), S['o'],
                role('p', 'plate', 'Plate', BLUE), role('k', 'chips', 'Chocolate chips', BLUE), role('m', 'milk', 'Milk', BLUE),
                role('g', 'glass', 'Glass', GREEN), S['t'], S['d'], S['v']], ['food', 'sweets']


def pie(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    kind, lattice, piece, steam = r.randrange(3), r.random() < 0.6, r.random() < 0.5, r.random() < 0.5
    ty = h * r.uniform(0.84, 0.88)
    setting(cv, r, w, h, kind, ty)
    px, py, rx, ry = cx, ty - h * 0.2, w * 0.42, h * 0.11
    if piece:
        px, rx = cx - w * 0.1, w * 0.36
    poly(cv, [(px - rx, py), (px + rx, py), (px + rx * 0.82, ty + 0.4), (px - rx * 0.82, ty + 0.4)], 'p')
    oval(cv, px, ty, rx * 0.82, 0.9, 'p')
    oval(cv, px, py, rx + 0.4, ry + 0.4, 'c')
    oval(cv, px, py, rx - 1.2, ry - 1.0, 'f')
    if lattice:
        for x, y, qx, qy in cells(cv):
            if cv.g[y][x] == 'f' and (abs(round((qx - px) / 2.6) * 2.6 - (qx - px)) < 0.6 or (qy - py) % 2.0 < 0.9):
                cv.g[y][x] = 'l'
    else:  # a closed top with a heart cut out
        oval(cv, px, py, rx - 1.2, ry - 1.0, 'l')
        heart(cv, px, py - 0.3, 1.6, 'f')
    if steam:
        for k in (-1, 1):
            path(cv, [(px + k * 3.0, py - ry - 1.5), (px + k * 3.0 + 0.8, py - ry - 3.5), (px + k * 3.0 - 0.4, py - ry - 5.5)], 'm', 0.45)
    dots(cv, 'e', 'c', 2, 1, area=(px - rx - 1, py - ry - 1, px + rx + 1, py + ry + 1))
    if piece:  # a slice on its own plate, its cut side showing the filling
        sx, sy = cx + w * 0.26, ty - 1.4
        oval(cv, sx, ty, w * 0.22, 1.4, 'q')
        poly(cv, [(sx - 4.4, sy), (sx + 4.4, sy - 5.6), (sx + 4.4, sy)], 'f')
        seg(cv, sx - 4.4, sy - 0.3, sx + 4.4, sy - 5.6, 'l', 0.7)
        box(cv, sx - 4.0, sy - 0.8, sx + 4.4, sy, 'c')
        rbox(cv, sx + 2.8, sy - 6.6, sx + 5.0, sy, 0.8, 'c')
    if r.random() < 0.5:
        mirror(cv)
    S = setting_roles(kind, (BLUE, BLUE, GREEN, BROWN, BROWN))
    return cv, [S['b'], role('c', 'crust', 'Crust', BROWN), role('l', 'lattice', 'Lattice and top', BROWN), role('e', 'crimp', 'Crimped edge', BROWN),
                role('f', 'filling', 'Cherry filling', PINK), role('m', 'steam', 'Steam', PINK), role('p', 'dish', 'Pie dish', BLUE),
                role('q', 'plate', 'Small plate', GREEN), S['t'], S['d'], S['o'], S['v']], ['food', 'sweets']


DAILY_FOOD = [banana, lemon, peach, coconut, avocado, carrot, corn, broccoli, tomato, eggplant, chili_pepper, pea_pod, cookie, pie]

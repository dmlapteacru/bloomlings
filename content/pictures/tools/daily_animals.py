"""Daily Challenge subjects: animals (daily_subjects.py). Thirty-two animals the levels never show, drawn with
picture_kit in the friendly, generic style of animal_subjects.py: a clear silhouette in a simple scene, eye whites and
pupils, and the colors that shape the animal (its body against the sky, its eyes against its face) from different color
groups, so they stay apart under any mapping. Each picture takes its pose, scene, props, time of day and the side the
animal faces from `r`, so the three pictures of a subject differ.
"""
import math

from animal_subjects import eye, eyes, pine
from picture_kit import (BLUE, BROWN, GREEN, PINK, box, cloud, disc, ground_rows, hills, lens, oval, path, poly, rbox,
                         ring, role, scatter, seg, sky, star, start)


def mirror(cv):
    """Turns the drawing to face the other way."""
    cv.g = [row[::-1] for row in cv.g]


def tube(cv, pts, r0, r1, c):
    """A tube along the polyline pts, its radius going from r0 to r1 (necks, tails, tentacles); c is a role or a
    function of the share of the length, for rings."""
    lengths = [math.hypot(b[0] - a[0], b[1] - a[1]) for a, b in zip(pts, pts[1:])]
    total, done = sum(lengths) or 1.0, 0.0
    for (a, b), L in zip(zip(pts, pts[1:]), lengths):
        n = max(1, int(L * 3))
        for k in range(n + 1):
            t = k / n
            u = (done + L * t) / total
            disc(cv, a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t, r0 + (r1 - r0) * u, c(u) if callable(c) else c)
        done += L


def stripe(cv, x0, y0, x1, y1, c, only, t=0.5):
    """A line of role c painted only over cells of the roles in `only` (stripes)."""
    for y in range(cv.h):
        for x in range(cv.w):
            if cv.g[y][x] not in only:
                continue
            px, py, dx, dy = x + 0.5, y + 0.5, x1 - x0, y1 - y0
            L = dx * dx + dy * dy
            u = 0 if L == 0 else max(0, min(1, ((px - x0) * dx + (py - y0) * dy) / L))
            if math.hypot(px - (x0 + u * dx), py - (y0 + u * dy)) <= t:
                cv.g[y][x] = c


def curve(pts, n=12):
    """n + 1 points along the Bezier curve with these control points."""
    out = []
    for k in range(n + 1):
        t = k / n
        q = list(pts)
        while len(q) > 1:
            q = [(a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t) for a, b in zip(q, q[1:])]
        out.append(q[0])
    return out


def leg(cv, x, top, bottom, c, hoof=None, wide=2):
    """A straight leg of `wide` cells from column x down to `bottom`, its last row of the hoof's role."""
    box(cv, x + 0.1, top, x + wide - 0.1, bottom, c)
    if hoof:
        box(cv, x + 0.1, bottom - 1.0, x + wide - 0.1, bottom, hoof)


def blot(cv, x, y, rad, c, only):
    """A disc of role c painted only over cells of the roles in `only` (spots and patches)."""
    for yy in range(cv.h):
        for xx in range(cv.w):
            if cv.g[yy][xx] in only and (xx + 0.5 - x) ** 2 + (yy + 0.5 - y) ** 2 <= rad * rad:
                cv.g[yy][xx] = c


def palm(cv, x, base, top, trunk, leaf, lean=1.0):
    """A palm tree from (x, base) to its crown at height `top`, leaning to the right by `lean` cells per ten rows."""
    tx = x + lean * (base - top) / 10
    path(cv, curve([(x, base), (x + (tx - x) * 0.2, (base + top) / 2), (tx, top)], 6), trunk, 0.7)
    for a in (-165, -125, -55, -15, 25, 155):
        t = math.radians(a)
        lens(cv, tx, top, tx + math.cos(t) * 5.6, top + math.sin(t) * 3.2 + 1.6, 1.9, leaf)


# ---- Horses and their kin ----

def equine(cv, bx, gtop, c, hoof, pose):
    """A horse facing right, its body centred on bx and its hooves on gtop, standing or prancing. Returns the eye, the
    crest of its neck (for the mane), the root of its tail and its forehead."""
    by = gtop - 10.5
    oval(cv, bx, by + 0.2, 5.6, 2.5, c)
    disc(cv, bx + 4.0, by - 0.5, 2.3, c)
    disc(cv, bx - 4.4, by - 0.4, 2.4, c)
    c0 = int(bx)
    for k, dx in enumerate((-5, -2, 2, 5)):
        if pose == 'prance' and k == 3:
            path(cv, [(dx + c0 + 1.0, by + 1.5), (dx + c0 + 1.6, by + 4.6), (dx + c0 + 3.4, by + 5.4)], c, 0.8)
            disc(cv, dx + c0 + 3.6, by + 5.5, 0.8, hoof)
        else:
            leg(cv, c0 + dx, by + 1.5, gtop + 0.5, c, hoof)
    if pose == 'graze':
        poly(cv, [(bx + 2.6, by - 2.0), (bx + 6.0, by - 0.6), (bx + 10.0, by + 5.2), (bx + 7.6, by + 6.0)], c)
        disc(cv, bx + 9.2, by + 5.6, 2.0, c)
        lens(cv, bx + 8.8, by + 5.4, bx + 11.4, gtop + 0.2, 3.2, c)
        disc(cv, bx + 10.8, gtop - 1.3, 1.3, c)
        poly(cv, [(bx + 8.0, by + 4.2), (bx + 6.4, by + 2.4), (bx + 8.9, by + 3.7)], c)
        return ((bx + 9.6, by + 4.6), [(bx + 7.8, by + 3.6), (bx + 5.4, by + 0.2), (bx + 2.6, by - 2.4)],
                (bx - 6.0, by - 1.4), (bx + 10.0, by + 4.0))
    poly(cv, [(bx + 2.4, by - 1.8), (bx + 6.6, by + 0.2), (bx + 8.4, by - 5.4), (bx + 5.4, by - 8.0)], c)
    disc(cv, bx + 7.4, by - 7.0, 2.2, c)
    lens(cv, bx + 7.0, by - 6.9, bx + 12.4, by - 3.6, 3.4, c)
    disc(cv, bx + 11.6, by - 4.0, 1.5, c)
    poly(cv, [(bx + 5.8, by - 8.4), (bx + 6.2, by - 11.4), (bx + 7.4, by - 8.8)], c)
    return ((bx + 7.7, by - 8.0), [(bx + 5.6, by - 8.8), (bx + 4.6, by - 6.4), (bx + 3.2, by - 3.8), (bx + 2.0, by - 2.4)],
            (bx - 6.0, by - 1.4), (bx + 7.6, by - 8.4))


def socks(cv, bx, gtop, c, legs=(0, 3)):
    """White socks above the hooves of the given legs of equine()."""
    for k in legs:
        x = int(bx) + (-5, -2, 2, 5)[k]
        box(cv, x + 0.1, gtop - 2.4, x + 1.9, gtop - 1.1, c)


def meadow(cv, r, c, gtop):
    hills(cv, c, gtop, 0.5, 22 * r.uniform(1.1, 1.6), r.uniform(0, 6))


def horse(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.14)
    meadow(cv, r, 'g', gtop)
    scene = r.choice(('fence', 'trees', 'barn'))
    if scene == 'fence':
        for x in range(1, w, 4):
            box(cv, x, h * 0.62, x + 0.9, gtop, 'f')
        for y in (h * 0.66, h * 0.76):
            box(cv, 0, y, w, y + 0.9, 'f')
    elif scene == 'trees':
        for x in (r.uniform(1, 3), r.uniform(19, 21)):
            disc(cv, x, gtop - 9, 3.0, 't')
            box(cv, x - 0.5, gtop - 7, x + 0.5, gtop, 'f')
    else:
        bx0 = r.choice((0.5, 15.5))
        box(cv, bx0, gtop - 6, bx0 + 6, gtop, 'f')
        poly(cv, [(bx0 - 0.8, gtop - 5.6), (bx0 + 3, gtop - 9), (bx0 + 6.8, gtop - 5.6)], 'm')
        box(cv, bx0 + 2, gtop - 4, bx0 + 4, gtop, 'm')
    pose = r.choice(('stand', 'prance', 'graze'))
    bx = 9.0
    (ex, ey), crest, (tx, ty), _ = equine(cv, bx, gtop, 'b', 'm', pose)
    tube(cv, [(tx + 0.4, ty), (tx - 0.8, ty + 1.0), (tx - 1.2, ty + 4.0), (tx - 0.8, ty + 7.6)], 1.0, 0.7, 'm')
    tube(cv, crest, 0.85, 0.85, 'm')
    eye(cv, ex, ey, big, 'w', 'm')
    socks(cv, bx, gtop, 'w', (0, 2) if pose == 'prance' else (0, 3))
    for x in r.sample((2.5, 6.5, 13.5, 19.5), 2):
        disc(cv, x, gtop + 1.8, 0.9, 'o')
    if r.random() < 0.5:
        disc(cv, r.choice((3.5, 8.0)), 3.4, 2.2, 'u')
    else:
        cloud(cv, r.choice((4.0, 9.0)), 3.6, 1.7, 'c')
    if r.random() < 0.5:
        mirror(cv)
    return cv, [sky(), role('b', 'horse', 'Horse', BROWN), role('m', 'mane', 'Mane, tail, hooves and eye', BROWN),
                role('u', 'sun', 'Sun', BROWN), role('w', 'eye_white', 'Eye white', BLUE), role('f', 'fence', 'Fence, barn and trunks', PINK),
                role('o', 'flowers', 'Flowers and cloud', PINK), role('c', 'cloud', 'Cloud', PINK),
                role('g', 'meadow', 'Meadow', GREEN), role('t', 'tree', 'Trees', GREEN)], ['animals', 'farm']


def unicorn(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.14)
    meadow(cv, r, 'g', gtop)
    scene = r.choice(('rainbow', 'moon', 'clouds'))
    ax, ay = r.uniform(3.5, 5.5), r.uniform(5.4, 6.2)
    if scene == 'rainbow':
        for rad, c in ((4.4, 'q'), (3.5, 'h'), (2.6, 'v')):
            ring(cv, ax, ay, rad, rad - 0.95, c)
        box(cv, 0, ay, ax + 6, ay + 6, 's', only='qhv')
        for x in (ax - 3.5, ax + 3.5):
            cloud(cv, x, ay + 0.1, 0.9, 'c')
    elif scene == 'moon':
        disc(cv, ax, ay - 1.5, 2.6, 'h')
        disc(cv, ax + 1.3, ay - 2.3, 2.2, 's')
    else:
        cloud(cv, ax, ay - 1.0, 1.4, 'c')
        cloud(cv, r.uniform(15, 18), 2.5, 1.0, 'c')
    pose = r.choice(('stand', 'prance', 'graze'))
    bx = 9.0
    (ex, ey), crest, (tx, ty), (fx, fy) = equine(cv, bx, gtop, 'b', 'q', pose)
    tube(cv, crest, 1.0, 0.8, 'q')
    tube(cv, [(tx + 0.4, ty), (tx - 0.8, ty + 1.2), (tx - 1.1, ty + 4.0), (tx - 0.6, ty + 7.6)], 1.0, 0.7, 'q')
    eye(cv, ex, ey, big, 'e', 'q')
    socks(cv, bx, gtop, 'e', (0, 2) if pose == 'prance' else (0, 3))
    hx, hy = (fx + 0.6, fy - 0.2)
    tip = (hx + 3.0, hy - 4.6) if pose != 'graze' else (hx + 4.8, hy - 1.6)
    poly(cv, [(hx - 1.0, hy + 0.4), (hx + 1.2, hy + 0.9), tip], 'h')
    for k in range(3 if scene != 'moon' else 6):
        star(cv, r.uniform(9, 20), r.uniform(1.5, 8.5), 1.5, 'c', ri=0.5, points=4)
    for x in (r.uniform(1, 5), r.uniform(15, 21)):
        disc(cv, x, gtop + 1.8, 0.9, 'o')
    if r.random() < 0.5:
        mirror(cv)
    return cv, [sky(), role('b', 'unicorn', 'Unicorn', PINK), role('q', 'mane', 'Mane, hooves, eye and rainbow', PINK),
                role('c', 'cloud', 'Clouds and sparkles', PINK), role('h', 'horn', 'Horn, moon and rainbow', BROWN),
                role('v', 'rainbow', 'Rainbow green', GREEN), role('e', 'eye_white', 'Eye white and socks', BLUE),
                role('g', 'meadow', 'Meadow', GREEN), role('o', 'flowers', 'Flowers', BLUE)], ['animals', 'fairy']


def zebra(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.14)
    meadow(cv, r, 'g', gtop)
    if r.random() < 0.5:
        disc(cv, r.choice((4.5, 17.0)), r.uniform(3.0, 5.0), 2.3, 'u')
    else:
        disc(cv, r.uniform(5, 17), gtop - 1.0, 4.2, 'u')
    for tx in r.sample((2.5, 19.0, 20.5), r.randint(1, 2)):
        ty = r.uniform(0.33, 0.42) * h
        box(cv, tx - 0.6, ty, tx + 0.6, gtop + 0.5, 'k')
        oval(cv, tx, ty, 4.0, 1.5, 'a')
    for k in range(r.randint(0, 3)):
        x, y = r.uniform(8, 14) + k * 2.2, r.uniform(2, 6)
        path(cv, [(x - 1.2, y - 0.6), (x, y + 0.2), (x + 1.2, y - 0.6)], 'k', 0.45)
    pose = r.choice(('stand', 'graze', 'prance'))
    bx = 9.0
    (ex, ey), crest, (rx_, ry_), _ = equine(cv, bx, gtop, 'z', 'k', pose)
    by = gtop - 10.5
    head = (lambda px, py: px > bx + 7.0 and py > by + 3.0) if pose == 'graze' else (lambda px, py: px > bx + 6.2 and py < by - 4.6)
    for y in range(h):
        for x in range(w):
            px, py = x + 0.5, y + 0.5
            if cv.g[y][x] != 'z':
                continue
            if head(px, py):
                stripe = False
            elif py > by + 2.5 and px < bx + 7.0:
                stripe = int(py) % 3 == 0
            elif px > bx + 3.0:
                stripe = int(py + px * (-0.8 if pose == 'graze' else 0.5)) % 3 == 0
            else:
                stripe = int(px + (py - by) * 0.3) % 3 == 0
            if stripe:
                cv.g[y][x] = 'k'
    if pose == 'graze':
        disc(cv, bx + 10.8, gtop - 1.3, 1.3, 'k')
    else:
        disc(cv, bx + 11.6, by - 4.0, 1.5, 'k')
    tube(cv, crest, 0.7, 0.7, 'k')
    path(cv, [(rx_ + 0.4, ry_), (rx_ - 0.6, ry_ + 2.0), (rx_ - 0.8, ry_ + 5.0)], 'z', 0.5)
    disc(cv, rx_ - 0.8, ry_ + 5.6, 0.9, 'k')
    eye(cv, ex, ey, big, None, 'k')
    if r.random() < 0.6:
        oval(cv, r.uniform(15, 18), gtop + 2.2, 4.0, 1.1, 'p')
    if r.random() < 0.5:
        mirror(cv)
    return cv, [('s', 'sky', 'Sunset sky', PINK, True), role('z', 'zebra', 'Zebra', BLUE), role('u', 'sun', 'Sun', BROWN),
                role('k', 'stripes', 'Stripes, mane and trunk', BROWN), role('p', 'pool', 'Water hole', BLUE),
                role('g', 'savanna', 'Savanna', GREEN), role('a', 'acacia', 'Acacia', GREEN)], ['animals', 'savanna']


def deer(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.14)
    dusk = r.random() < 0.35
    for x in r.sample((1.5, 4.5, 17.5, 20.5), r.randint(1, 4)):
        pine(cv, x, gtop, r.uniform(9, 15), r.uniform(5, 7), 't')
    meadow(cv, r, 'g', gtop)
    stag = r.random() < 0.5
    pose = 'stand' if stag else r.choice(('stand', 'graze'))
    bx, by = 9.0, gtop - 10.0
    oval(cv, bx, by, 5.0, 2.3, 'd')
    disc(cv, bx + 3.8, by - 0.4, 2.1, 'd')
    disc(cv, bx - 4.0, by - 0.2, 2.2, 'd')
    c0 = int(bx)
    for dx in (-5, -2, 1, 4):
        box(cv, c0 + dx + 0.1, by + 1, c0 + dx + 1.9, by + 4.5, 'd')
        box(cv, c0 + dx + 1.1, by + 4, c0 + dx + 1.9, gtop + 0.5, 'd')
        cv.put(c0 + dx + 1, gtop - 1, 'a')
    if pose == 'graze':
        poly(cv, [(bx + 2.6, by - 1.6), (bx + 5.2, by - 0.6), (bx + 8.6, by + 5.0), (bx + 6.6, by + 6.0)], 'd')
        hx, hy = bx + 8.0, by + 6.4
        disc(cv, hx, hy, 1.7, 'd')
        lens(cv, hx - 0.2, hy, hx + 1.6, gtop + 0.3, 2.4, 'd')
        lens(cv, hx - 0.6, hy - 1.0, hx - 2.4, hy - 2.6, 1.5, 'd')
        ex, ey = hx + 0.3, hy - 1.0
    else:
        poly(cv, [(bx + 2.4, by - 1.4), (bx + 5.2, by - 0.4), (bx + 6.8, by - 5.6), (bx + 4.6, by - 6.4)], 'd')
        hx, hy = bx + 6.0, by - 7.0
        disc(cv, hx, hy, 1.7, 'd')
        lens(cv, hx - 0.2, hy, hx + 3.8, hy + 1.6, 2.4, 'd')
        cv.put(int(hx + 3.4), int(hy + 1.2), 'a')
        lens(cv, hx - 0.8, hy - 1.0, hx - 3.4, hy - 2.4, 1.5, 'd')
        if not stag:
            lens(cv, hx + 0.2, hy - 1.2, hx + 0.8, hy - 4.0, 1.4, 'd')
        ex, ey = hx + 0.4, hy - 0.5
    if stag:
        for side in (-1, 1):
            x0, y0 = hx + 0.4 * side, hy - 1.4
            mx, my = x0 + side * 1.2, y0 - 2.6
            path(cv, [(x0, y0), (mx, my), (mx + side * 1.4, my - 2.6)], 'a', 0.5)
            seg(cv, mx, my, mx + side * 2.0, my - 0.6, 'a', 0.5)
            seg(cv, mx + side * 0.7, my - 1.4, mx - side * 0.4, my - 2.8, 'a', 0.5)
    else:
        scatter(cv, 'w', 'd', 7, r, sep=2, area=(bx - 4.5, by - 2.2, bx + 3.0, by + 0.5))
    disc(cv, bx - 5.8, by - 1.4, 1.0, 'w')
    eye(cv, ex, ey, big, 'w', 'a')
    for x in r.sample((3.0, 7.0, 15.5, 19.5), 2):
        disc(cv, x, gtop + 1.4, 1.2, 'f')
        box(cv, x - 0.4, gtop + 1.6, x + 0.4, gtop + 3, 'w')
    if dusk:
        disc(cv, r.uniform(9, 14), 2.8, 1.8, 'u')
        disc(cv, r.uniform(9, 14) + 1.2, 2.2, 1.6, 's')
    else:
        disc(cv, r.uniform(9, 14), 3.0, 2.2, 'u')
    if r.random() < 0.5:
        mirror(cv)
    return cv, [('s', 'sky', 'Dusk sky' if dusk else 'Sky', PINK if dusk else BLUE, True), role('d', 'deer', 'Deer', BROWN),
                role('a', 'antlers', 'Antlers, hooves and eye', BROWN), role('u', 'sun', 'Moon' if dusk else 'Sun', BROWN),
                role('w', 'white', 'Tail, spots, eye white and stems', BLUE), role('f', 'mushrooms', 'Mushrooms', BLUE if dusk else PINK),
                role('g', 'meadow', 'Meadow', GREEN), role('t', 'pines', 'Pines', GREEN)], ['animals', 'forest']


def cow(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.14)
    scene = r.choice(('barn', 'trees', 'hills'))
    if scene == 'hills':
        hills(cv, 't', gtop - 4, 1.6, 22 * r.uniform(0.8, 1.2), r.uniform(0, 6))
    meadow(cv, r, 'g', gtop)
    if scene == 'barn':
        box(cv, 0.5, gtop - 8, 7.5, gtop, 'n')
        poly(cv, [(-0.3, gtop - 7.6), (4.0, gtop - 11.5), (8.3, gtop - 7.6)], 't')
        box(cv, 2.6, gtop - 5, 5.4, gtop, 'm')
    elif scene == 'trees':
        for x in r.sample((1.5, 4.5, 8.0), 2):
            disc(cv, x, gtop - 10.5, 2.8, 't')
            box(cv, x - 0.5, gtop - 8.5, x + 0.5, gtop, 'n')
    for k in range(r.randint(1, 2)):
        cloud(cv, r.uniform(3, 7) + k * 9, r.uniform(2.2, 3.4), 1.2, 'c')
    top = gtop - 12.0
    rbox(cv, 1.5, top, 14.5, top + 6.4, 2.0, 'w')
    for x in (2, 5, 10, 13):
        leg(cv, x, top + 5.5, gtop + 0.5, 'w', 'k')
    oval(cv, 8.0, top + 6.6, 1.6, 1.0, 'm')
    path(cv, [(1.7, top + 0.6), (0.6, top + 2.4), (0.6, top + 6.0)], 'w', 0.45)
    disc(cv, 0.7, top + 6.6, 0.8, 'k')
    for k in range(r.randint(3, 4)):
        blot(cv, r.uniform(2.5, 11.5), r.uniform(top + 1, top + 5), r.uniform(1.4, 2.0), 'k', 'w')
    hx, hy = 16.6, top - 2.4 + r.uniform(0, 0.8)
    poly(cv, [(12.0, top + 0.4), (hx - 1.6, hy + 1.6), (hx + 1.8, hy + 2.0), (hx + 0.6, top + 4.6), (12.4, top + 5.0)], 'w')
    for side in (-1, 1):
        lens(cv, hx + side * 2.6, hy - 1.8, hx + side * 5.2, hy - 2.6, 1.8, 'w')
        lens(cv, hx + side * 3.0, hy - 1.9, hx + side * 4.6, hy - 2.4, 0.7, 'm')
        path(cv, [(hx + side * 1.6, hy - 3.6), (hx + side * 2.4, hy - 5.0), (hx + side * 2.0, hy - 6.2)], 'c', 0.5)
    rbox(cv, hx - 2.9, hy - 4.2, hx + 2.9, hy + 2.6, 2.2, 'w')
    if r.random() < 0.6:
        blot(cv, hx + r.choice((-1.6, 1.6)), hy - 2.4, 1.6, 'k', 'w')
    rbox(cv, hx - 2.8, hy + 0.2, hx + 2.8, hy + 3.8, 1.6, 'm')
    for side in (-1, 1):
        cv.put(int(hx + side * 1.2), int(hy + 1.6), 'k')
        eye(cv, hx + side * 1.4, hy - 2.6, big, 'c', 'k')
    disc(cv, hx, hy + 4.8, 1.0, 'y')
    for x in r.sample((3.5, 7.5, 11.5, 20.0), 2):
        disc(cv, x, gtop + 1.8, 0.9, 'y')
    if r.random() < 0.5:
        mirror(cv)
    return cv, [('s', 'sky', 'Morning sky', BROWN, True), role('w', 'cow', 'Cow', BLUE), role('m', 'muzzle', 'Muzzle, udder and barn door', PINK),
                role('k', 'spots', 'Spots, hooves and eyes', PINK), role('y', 'bell', 'Bell and flowers', BROWN),
                role('c', 'cloud', 'Clouds, horns and eye whites', BLUE), role('n', 'barn', 'Barn and trunks', PINK),
                role('g', 'meadow', 'Meadow', GREEN), role('t', 'trees', 'Trees, roof and hills', GREEN)], ['animals', 'farm']


def goat(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    for k in range(r.randint(1, 2)):
        px, ph = r.uniform(3, 19), r.uniform(9, 13)
        poly(cv, [(px - 9, 22), (px, 22 - ph), (px + 9, 22)], 'm')
        poly(cv, [(px - 2.4, 22 - ph + 2.4), (px, 22 - ph), (px + 2.4, 22 - ph + 2.4), (px + 1.0, 22 - ph + 3.2), (px - 1.0, 22 - ph + 2.6)], 'n')
    rt = r.uniform(19.5, 21.0)
    poly(cv, [(-1, h), (-1, rt + 0.8), (3, rt), (12, rt - 0.4), (17, rt + 0.6), (20, rt + 3.0), (23, rt + 5.0), (23, h)], 'r')
    if r.random() < 0.5:
        disc(cv, r.choice((3.5, 18.0)), 3.2, 2.2, 'u')
    else:
        cloud(cv, r.uniform(5, 17), 3.0, 1.4, 'n')
    bx, by = 8.5, rt - 6.0
    oval(cv, bx, by, 4.6, 2.5, 'b')
    disc(cv, bx + 3.2, by - 0.3, 2.2, 'b')
    c0 = int(bx)
    for dx in (-4, -2, 2, 4):
        box(cv, c0 + dx + 0.1, by + 1, c0 + dx + 1.9, by + 3.0, 'b')
        box(cv, c0 + dx + 0.1 + (0 if dx < 0 else 1), by + 2.5, c0 + dx + 0.9 + (0 if dx < 0 else 1), rt + 0.6, 'b')
        cv.put(c0 + dx + (0 if dx < 0 else 1), int(rt) - 1, 'h')
    lens(cv, bx - 4.0, by - 1.2, bx - 5.4, by - 3.6, 1.2, 'b')
    poly(cv, [(bx + 2.4, by - 1.2), (bx + 5.0, by + 0.4), (bx + 6.4, by - 3.6), (bx + 4.6, by - 4.6)], 'b')
    hx, hy = bx + 6.2, by - 5.0
    disc(cv, hx, hy, 1.6, 'b')
    poly(cv, [(hx - 1.0, hy - 1.2), (hx + 1.2, hy - 1.4), (hx + 4.0, hy + 1.6), (hx + 3.2, hy + 2.6), (hx + 0.4, hy + 1.6)], 'b')
    poly(cv, [(hx + 1.6, hy + 1.8), (hx + 2.8, hy + 2.2), (hx + 1.8, hy + 4.4)], 'h')
    tube(cv, [(hx - 0.2, hy - 1.0), (hx - 0.6, hy - 3.2), (hx - 2.4, hy - 4.6), (hx - 4.2, hy - 4.0), (hx - 4.8, hy - 2.6)], 0.9, 0.45, 'h')
    lens(cv, hx - 1.0, hy - 0.2, hx - 3.2, hy + 0.8, 1.3, 'b')
    eye(cv, hx + 0.2, hy - 0.8, big, 'n', 'h')
    for k in range(3):
        x = r.uniform(1, 21)
        y = rt + r.uniform(2, 5)
        if x < 2 or x > 20 or abs(x - bx) > 2:
            disc(cv, x, y, 0.9, 'f')
    if r.random() < 0.5:
        mirror(cv)
    return cv, [sky(), role('b', 'goat', 'Goat', BROWN), role('h', 'horns', 'Horns, beard and hooves', BROWN), role('u', 'sun', 'Sun', BROWN),
                role('n', 'snow', 'Snow, cloud and eye white', BLUE), role('m', 'mountains', 'Mountains', PINK),
                role('f', 'flowers', 'Flowers', PINK), role('r', 'rock', 'Grassy rock', GREEN)], ['animals', 'mountains']


def camel(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.12)
    if r.random() < 0.5:
        x0 = r.choice((3.0, 15.0))
        for dx, ph in ((0, 7.0), (4.6, 4.6)):
            poly(cv, [(x0 + dx - ph, gtop - 2), (x0 + dx, gtop - 2 - ph), (x0 + dx + ph, gtop - 2)], 'y')
    hills(cv, 'd', gtop - 1.5, 1.0, 22 * r.uniform(0.7, 1.1), r.uniform(0, 6))
    side = r.choice((2.5, 19.5))
    if r.random() < 0.5:
        palm(cv, side, gtop + 1, gtop - 12, 'k', 'l', 1.0 if side < 11 else -1.0)
        oval(cv, side + (5 if side < 11 else -5), gtop + 2.5, 3.2, 0.9, 'o')
    else:
        for x in (side, side + (3.5 if side < 11 else -3.5)):
            box(cv, x - 0.7, gtop - 7, x + 0.7, gtop + 0.5, 'l')
            box(cv, x - 2.2, gtop - 4.6, x + 0.7, gtop - 3.6, 'l')
            box(cv, x - 2.2, gtop - 6.4, x - 1.4, gtop - 3.6, 'l')
            cv.put(int(x), int(gtop - 7.4), 'q')
    disc(cv, r.uniform(4, 18), r.uniform(2.5, 4.0), 2.0, 'u')
    bx, by = 8.0, gtop - 12.0
    oval(cv, bx, by, 5.2, 2.2, 'c')
    for dx in (-2.5, 2.5):
        disc(cv, bx + dx, by - 2.0, 2.1, 'c')
    rbox(cv, bx - 1.0, by - 1.8, bx + 1.0, by + 1.4, 0.4, 'p')
    box(cv, bx - 1.0, by + 0.6, bx + 1.0, by + 1.4, 'q')
    c0 = int(bx)
    for dx in (-4, -2, 2, 4):
        box(cv, c0 + dx + 0.1, by + 1.0, c0 + dx + 1.9, by + 3.6, 'c')
        box(cv, c0 + dx + 0.1 + (1 if dx > 0 else 0), by + 3.0, c0 + dx + 0.9 + (1 if dx > 0 else 0), gtop + 0.5, 'c')
        cv.put(c0 + dx + (1 if dx > 0 else 0), int(by + 5.4), 'k')
        box(cv, c0 + dx + 0.1, gtop - 1.0, c0 + dx + 1.9, gtop, 'c')
    tube(cv, [(bx + 4.2, by - 0.4), (bx + 7.0, by + 1.2), (bx + 8.8, by - 0.2), (bx + 9.6, by - 3.6)], 1.4, 0.9, 'c')
    hx, hy = bx + 10.2, by - 4.8
    oval(cv, hx, hy, 1.8, 1.4, 'c')
    lens(cv, hx, hy + 0.2, hx + 3.2, hy + 1.0, 1.8, 'c')
    lens(cv, hx - 1.0, hy - 0.8, hx - 1.8, hy - 2.4, 1.0, 'c')
    eye(cv, hx + 0.2, hy - 1.0, big, 'e', 'k')
    path(cv, [(bx - 5.0, by - 0.6), (bx - 6.2, by + 1.4), (bx - 6.2, by + 4.0)], 'c', 0.45)
    disc(cv, bx - 6.2, by + 4.6, 0.8, 'k')
    if r.random() < 0.5:
        mirror(cv)
    return cv, [sky(), role('c', 'camel', 'Camel', BROWN), role('k', 'dark', 'Knees, eye, tail and trunk', BROWN),
                role('d', 'dunes', 'Dunes', BROWN), role('u', 'sun', 'Sun', BROWN), role('p', 'blanket', 'Blanket', PINK),
                role('q', 'tassels', 'Blanket stripe and cactus flowers', PINK), role('y', 'pyramids', 'Pyramids', PINK),
                role('e', 'eye_white', 'Eye white', BLUE), role('o', 'oasis', 'Oasis', BLUE), role('l', 'plants', 'Palm and cactus', GREEN)],\
        ['animals', 'desert']


def kangaroo(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.12)
    sunset = r.random() < 0.4
    scene = r.choice(('rock', 'tree', 'both'))
    if scene in ('rock', 'both'):
        x0 = r.choice((0.0, 14.0))
        rbox(cv, x0, gtop - 6, x0 + 8, gtop + 1, 2.4, 'q')
    meadow(cv, r, 'g', gtop)
    if scene in ('tree', 'both'):
        tx = r.choice((2.0, 19.5))
        seg(cv, tx, gtop, tx + 0.4, gtop - 10, 'n', 0.6)
        for dx, dy in ((-2.2, -10.5), (1.6, -12), (2.4, -9.6)):
            disc(cv, tx + dx, gtop + dy, 2.3, 't')
    disc(cv, r.uniform(3.5, 7.0), r.uniform(2.5, 4.0), 2.2, 'u')
    hop = r.random() < 0.45
    lift = 3.0 if hop else 0.0
    bx = 8.5
    fy = gtop - lift
    tube(cv, [(bx - 1.0, fy - 3.4), (bx - 4.4, fy - 1.4 - lift * 0.6), (bx - 7.8, fy - 0.4 - lift * 1.0)], 1.8, 0.7, 'k')
    oval(cv, bx, fy - 3.8, 2.9, 3.1, 'k')
    if hop:
        lens(cv, bx - 1.2, fy - 1.6, bx + 5.4, fy + 1.6, 2.0, 'k')
        oval(cv, bx + 2.0, gtop + 1.0, 4.6, 0.9, 'm')
    else:
        rbox(cv, bx - 1.0, fy - 1.6, bx + 5.4, fy + 0.4, 0.8, 'k')
    for dy, rad in ((-6.6, 3.0), (-9.4, 2.7), (-11.8, 2.2)):
        disc(cv, bx + 0.8 - dy * 0.12, fy + dy, rad, 'k')
    oval(cv, bx + 2.0, fy - 7.0, 1.7, 2.6, 'p')
    joey = r.random() < 0.5
    if joey:
        disc(cv, bx + 2.6, fy - 9.2, 1.2, 'k')
        lens(cv, bx + 2.0, fy - 9.8, bx + 1.4, fy - 11.8, 0.9, 'k')
        cv.put(int(bx + 3.0), int(fy - 9.4), 'n')
    hx, hy = bx + 3.6, fy - 15.4
    path(cv, [(bx + 2.4, fy - 12.6), (hx - 0.2, hy + 1.0)], 'k', 1.1)
    oval(cv, hx, hy, 2.4, 2.0, 'k')
    lens(cv, hx, hy + 0.2, hx + 4.0, hy + 1.3, 2.6, 'k')
    cv.put(int(hx + 3.6), int(hy + 1.0), 'n')
    for dx in (-1.4, 0.2):
        lens(cv, hx + dx, hy - 1.2, hx + dx - 0.9, hy - 5.6, 1.8, 'k')
        lens(cv, hx + dx - 0.1, hy - 1.8, hx + dx - 0.8, hy - 4.6, 0.7, 'p')
    path(cv, [(bx + 2.8, fy - 10.4), (bx + 4.6, fy - 9.0), (bx + 5.0, fy - 8.0)], 'k', 0.6)
    eye(cv, hx + 0.4, hy - 0.9, big, 'w', 'n')
    if r.random() < 0.5:
        mirror(cv)
    return cv, [('s', 'sky', 'Sunset sky' if sunset else 'Sky', PINK if sunset else BLUE, True), role('k', 'kangaroo', 'Kangaroo', BROWN),
                role('n', 'eye', 'Eyes, nose and trunk', BROWN), role('p', 'pouch', 'Pouch and ears', BROWN),
                role('w', 'eye_white', 'Eye white', BLUE if sunset else PINK), role('u', 'sun', 'Sun', BLUE if sunset else PINK),
                role('q', 'rock', 'Red rock', GREEN if sunset else PINK), role('m', 'shadow', 'Shadow', BLUE if sunset else PINK),
                role('g', 'grass', 'Grass', GREEN), role('t', 'tree', 'Gum tree', GREEN)], ['animals', 'outback']


def koala(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    pose = r.choice(('hug', 'sit', 'sleep'))
    tx = 15.8
    path(cv, [(tx + 0.4, h + 1), (tx, 14), (tx + 0.6, -1)], 'n', 2.2)
    for y in (r.uniform(2, 5), r.uniform(9, 12)):
        seg(cv, tx, y + 2, 21.5, y, 'n', 0.6)
        for k in range(3):
            a = math.radians(-50 + k * 50)
            lens(cv, 20.0, y - 0.4, 20.0 + math.cos(a) * 3.0, y - 0.4 + math.sin(a) * 2.6, 1.6, 'l')
    hills(cv, 'v', h - 2, 0.6, 22 * 0.7, r.uniform(0, 6))
    kx, hy = 9.0, r.uniform(7.6, 10.4)
    if pose == 'sit':
        by = hy + 13.4
        path(cv, [(tx, by + 0.6), (8.0, by), (1.6, by - 0.8)], 'n', 1.0)
        for k in range(3):
            a = math.radians(160 + k * 40)
            lens(cv, 2.4, by - 0.8, 2.4 + math.cos(a) * 2.8, by - 0.8 + math.sin(a) * 2.4, 1.5, 'l')
        oval(cv, kx, hy + 9.0, 4.2, 4.0, 'k')
        oval(cv, kx, hy + 9.6, 2.4, 2.8, 'f')
        for side in (-1, 1):
            oval(cv, kx + side * 2.0, hy + 15.4, 1.2, 1.7, 'k')
            path(cv, [(kx + side * 3.4, hy + 6.4), (kx + side * 1.4, hy + 8.6)], 'k', 1.0)
        lens(cv, kx + 1.6, hy + 8.4, kx + 4.6, hy + 3.4, 1.6, 'v')
    else:
        oval(cv, kx, hy + 9.4, 4.0, 4.4, 'k')
        oval(cv, kx - 0.4, hy + 10.0, 2.2, 3.0, 'f')
        for y0, y1 in ((6.6, 5.4), (12.8, 13.2)):
            path(cv, [(kx + 2.8, hy + y0), (tx - 0.6, hy + y1), (tx + 1.6, hy + y1 - 0.6)], 'k', 1.1)
            cv.put(int(tx + 2.0), int(hy + y1 - 0.6), 'e')
        path(cv, [(kx - 3.0, hy + 7.0), (kx - 4.2, hy + 9.6), (kx - 3.4, hy + 11.0)], 'k', 1.0)
        if pose == 'hug' and r.random() < 0.6:
            lens(cv, kx - 3.4, hy + 10.6, kx - 6.6, hy + 12.4, 1.5, 'v')
    for side in (-1, 1):
        disc(cv, kx + side * 5.0, hy - 3.2, 2.8, 'k')
        disc(cv, kx + side * 5.4, hy - 3.0, 1.6, 'f')
    disc(cv, kx, hy, 4.6, 'k')
    oval(cv, kx, hy + 1.0, 1.5, 2.0, 'e')
    if pose == 'sleep':
        for side in (-1, 1):
            path(cv, [(kx + side * 3.2, hy - 1.4), (kx + side * 2.3, hy - 0.8), (kx + side * 1.4, hy - 1.4)], 'e', 0.4)
        for k, (zx, zy) in enumerate(((kx + 3.0, hy - 6.4), (kx + 5.0, hy - 8.6))):
            sz = 0.8 + k * 0.3
            path(cv, [(zx - sz, zy - sz), (zx + sz, zy - sz), (zx - sz, zy + sz), (zx + sz, zy + sz)], 'c', 0.4)
    else:
        eyes(cv, kx, hy - 1.6, 2.3, big, None, 'e')
    path(cv, [(kx - 1.0, hy + 3.4), (kx, hy + 3.7), (kx + 1.0, hy + 3.4)], 'e', 0.4)
    if pose == 'hug' and r.random() < 0.6:
        bx = kx - 3.6
        disc(cv, bx, hy + 6.6, 1.8, 'k')
        for side in (-1, 1):
            disc(cv, bx + side * 1.6, hy + 5.0, 1.0, 'k')
        cv.put(int(bx), int(hy + 6.8), 'e')
    night = pose == 'sleep' or r.random() < 0.4
    mx = r.uniform(2.0, 4.5)
    if night:
        disc(cv, mx, 1.8, 1.5, 'm')
        disc(cv, mx + 0.9, 1.3, 1.2, 's')
        for k in range(4):
            cv.put(int(r.uniform(1, 13)), int(r.uniform(0, 3)), 'm')
    else:
        cloud(cv, r.uniform(4, 9), 1.6, 1.0, 'c')
    if r.random() < 0.5:
        mirror(cv)
    return cv, [('s', 'sky', 'Night sky' if night else 'Evening sky', PINK, True), role('c', 'cloud', 'Cloud and snores', PINK),
                role('k', 'koala', 'Koala', BLUE), role('f', 'fluff', 'Fluff', BLUE), role('n', 'tree', 'Tree', BROWN),
                role('e', 'nose', 'Nose, eyes and claws', BROWN), role('m', 'moon', 'Moon and stars', BROWN),
                role('l', 'leaves', 'Gum leaves', GREEN), role('v', 'sprig', 'Leaf sprig and bushes', GREEN)], ['animals', 'forest']


def monkey(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.1)
    for k in range(r.randint(2, 3)):
        x = r.uniform(1, 21)
        path(cv, [(x, 0), (x + r.uniform(-1, 1), 8), (x + r.uniform(-1, 1), 15)], 'v', 0.5)
        lens(cv, x, 10, x + 2.2, 8.6, 1.4, 'v')
    sit = r.random() < 0.45
    by = r.uniform(18.5, 19.5) if sit else r.uniform(3.0, 4.5)
    path(cv, [(-1, by + r.uniform(-1, 1)), (11, by), (18.5, by + r.uniform(-1, 1))], 'r', 1.0)
    for x in r.sample((2.5, 7.5, 13.5, 18.5), 3):
        for a in (-150, -90, -30):
            t = math.radians(a)
            lens(cv, x, by, x + math.cos(t) * 3.0, by + math.sin(t) * 2.6, 1.6, 'l')
    hills(cv, 'v', gtop, 0.6, 22 * 0.6, r.uniform(0, 6))
    mx = r.uniform(9.5, 11.5)
    hy = by - 9.6 if sit else by + 9.0
    if sit:
        for side in (-1, 1):
            path(cv, [(mx + side * 1.6, by - 1.0), (mx + side * 2.2, by + 2.4), (mx + side * 1.6, by + 4.0)], 'm', 0.8)
        path(cv, [(mx + 2.0, by - 0.6), (mx + 5.0, by + 1.6), (mx + 5.6, by + 4.6), (mx + 4.4, by + 6.2), (mx + 3.4, by + 5.0)], 'm', 0.55)
        path(cv, [(mx + 2.0, hy + 4.6), (mx + 3.4, by - 1.6), (mx + 4.6, by - 1.0)], 'm', 0.8)
    else:
        path(cv, [(mx + 2.2, hy + 3.4), (mx + 3.6, by + 3.0), (mx + 3.2, by + 0.4)], 'm', 0.8)
        for side in (-1, 1):
            path(cv, [(mx + side * 1.0, hy + 9.4), (mx + side * 2.0, hy + 12.0), (mx + side * 1.4, hy + 13.6)], 'm', 0.8)
        path(cv, [(mx + 2.2, hy + 9.0), (mx + 5.0, hy + 10.4), (mx + 7.0, hy + 8.4), (mx + 6.6, hy + 6.4), (mx + 5.2, hy + 6.8),
                  (mx + 5.4, hy + 8.0)], 'm', 0.55)
    oval(cv, mx, hy + 6.6, 2.8, 3.4, 'm')
    oval(cv, mx, hy + 7.0, 1.6, 2.4, 'f')
    path(cv, [(mx - 2.0, hy + 4.6), (mx - 4.2, hy + 6.0), (mx - 5.0, hy + 4.0)], 'm', 0.8)
    for k in range(5):
        t = k / 4
        disc(cv, mx - 5.6 + math.sin(t * 2.4) * 1.6, hy + 3.6 - t * 3.2, 0.75 - t * 0.15, 'y')
    for side in (-1, 1):
        disc(cv, mx + side * 3.6, hy, 1.5, 'm')
        disc(cv, mx + side * 3.6, hy, 0.8, 'f')
    disc(cv, mx, hy, 3.4, 'm')
    for side in (-1, 1):
        disc(cv, mx + side * 1.2, hy - 0.4, 1.6, 'f')
    oval(cv, mx, hy + 1.6, 2.4, 1.6, 'f')
    eyes(cv, mx, hy - 1.4, 1.3, big, 'w', 'k')
    path(cv, [(mx - 1.0, hy + 2.0), (mx, hy + 2.5), (mx + 1.0, hy + 2.0)], 'k', 0.4)
    for x in r.sample((2.0, 5.0, 17.0, 20.0), 2):
        disc(cv, x, gtop - 0.4, 1.0, 'o')
    if r.random() < 0.5:
        mirror(cv)
    return cv, [sky(), role('m', 'monkey', 'Monkey', BROWN), role('k', 'eye', 'Eyes and mouth', BROWN), role('y', 'banana', 'Banana', BROWN),
                role('w', 'eye_white', 'Eye whites', BLUE), role('f', 'face', 'Face, ears and belly', PINK), role('o', 'flowers', 'Flowers', PINK),
                role('r', 'branch', 'Branch', BROWN), role('l', 'leaves', 'Leaves', GREEN), role('v', 'vines', 'Vines and ferns', GREEN)],\
        ['animals', 'jungle']


def tiger(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.12)
    for x, y, a in r.sample(((0.0, 3.0, 20), (0.0, 9.0, -10), (22.0, 4.0, 160), (22.0, 10.0, 190)), r.randint(1, 3)):
        t = math.radians(a)
        lens(cv, x, y, x + math.cos(t) * 6.0, y + math.sin(t) * 6.0, 2.8, 'l')
        seg(cv, x, y, x + math.cos(t) * 4.6, y + math.sin(t) * 4.6, 'g', 0.35)
    meadow(cv, r, 'g', gtop)
    for k in range(r.randint(2, 6)):
        x = r.uniform(0.5, 21.5)
        lens(cv, x, gtop + 0.5, x + r.uniform(-1.5, 1.5), gtop - r.uniform(3, 5), 1.2, 'g')
    if r.random() < 0.6:
        disc(cv, r.choice((3.5, 18.5)), r.uniform(2.5, 4.0), 2.0, 'n')
    ringed = lambda u: 'k' if int(u * 7) % 2 else 't'
    lie = r.random() < 0.45
    if lie:
        tx = 9.0
        tube(cv, [(tx + 9.0, gtop - 3.0), (tx + 11.0, gtop - 5.0), (tx + 11.0, gtop - 8.4), (tx + 9.8, gtop - 10.0)], 1.0, 0.7, ringed)
        oval(cv, tx + 2.6, gtop - 3.4, 7.2, 3.4, 't')
        for x in range(int(tx + 1), int(tx + 10), 2):
            stripe(cv, x, gtop - 6.8, x + 0.8, gtop - 3.4, 'k', 't', 0.45)
        for dy in (-1.6, 0.0):
            rbox(cv, tx - 8.4, gtop - 1.4 + dy * 0.6, tx - 1.0, gtop + 0.4 + dy * 0.6, 0.8, 't')
        hx, hy, R = tx - 3.6, gtop - 8.8, 4.8
    else:
        tx = cx + r.uniform(-0.8, 0.8)
        side = r.choice((-1, 1))
        tube(cv, [(tx + side * 4.4, gtop - 1.5), (tx + side * 7.4, gtop - 2.6), (tx + side * 8.4, gtop - 6.4), (tx + side * 7.4, gtop - 9.0)],
             1.0, 0.7, ringed)
        oval(cv, tx, gtop - 5.4, 5.0, 5.4, 't')
        for sd in (-1, 1):
            oval(cv, tx + sd * 4.2, gtop - 1.6, 2.6, 1.8, 't')
            box(cv, tx + sd * 2.3 - 1.1, gtop - 8.0, tx + sd * 2.3 + 1.1, gtop + 0.5, 't')
            for y in (gtop - 5.0, gtop - 2.6):
                stripe(cv, tx + sd * 1.2, y, tx + sd * 3.4, y, 'k', 't', 0.45)
            for y in (gtop - 8.0, gtop - 5.0):
                stripe(cv, tx + sd * 5.2, y - 0.6, tx + sd * 3.8, y + 0.2, 'k', 't', 0.5)
        oval(cv, tx, gtop - 9.0, 1.2, 1.8, 'w')
        hx, hy, R = tx, gtop - 15.4, 5.2
    for sd in (-1, 1):
        disc(cv, hx + sd * R * 0.78, hy - R * 0.74, 1.9, 't')
        disc(cv, hx + sd * R * 0.78, hy - R * 0.72, 0.9, 'w')
        oval(cv, hx + sd * R * 0.72, hy + R * 0.36, 2.2, 1.8, 't')
    disc(cv, hx, hy, R, 't')
    for sd in (-1, 1):
        oval(cv, hx + sd * R * 0.7, hy + R * 0.42, 1.5, 1.1, 'w')
        oval(cv, hx + sd * 1.2, hy + R * 0.4, 1.5, 1.2, 'w')
        stripe(cv, hx + sd * (R + 0.2), hy - R * 0.28, hx + sd * (R - 1.6), hy - R * 0.14, 'k', 't', 0.5)
        stripe(cv, hx + sd * (R + 0.2), hy + R * 0.08, hx + sd * (R - 1.4), hy + R * 0.12, 'k', 't', 0.45)
    for dx in (-1.8, 0.0, 1.8):
        stripe(cv, hx + dx, hy - R, hx + dx * 0.8, hy - R * 0.55, 'k', 't', 0.45)
    oval(cv, hx, hy + R * 0.62, 1.0, 0.9, 'w')
    eyes(cv, hx, hy - R * 0.3, R * 0.42, big, 'e', 'k')
    poly(cv, [(hx - 1.0, hy + R * 0.12), (hx + 1.0, hy + R * 0.12), (hx, hy + R * 0.32)], 'n')
    if r.random() < 0.4:
        oval(cv, hx, hy + R * 0.62, 1.4, 1.0, 'n')
        for sd in (-1, 1):
            cv.put(int(hx + sd * 0.9), int(hy + R * 0.5), 'w')
    else:
        path(cv, [(hx - 1.2, hy + R * 0.52), (hx, hy + R * 0.42), (hx + 1.2, hy + R * 0.52)], 'k', 0.4)
    if r.random() < 0.5:
        mirror(cv)
    return cv, [sky(), role('t', 'tiger', 'Tiger', BROWN), role('n', 'nose', 'Nose, mouth and sun', PINK),
                role('k', 'stripes', 'Stripes and pupils', PINK), role('w', 'white', 'Muzzle, chest and ears', BLUE),
                role('e', 'eye', 'Eyes', GREEN), role('g', 'grass', 'Grass', GREEN), role('l', 'leaves', 'Jungle leaves', GREEN)],\
        ['animals', 'jungle']


def squirrel(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.12)
    meadow(cv, r, 'g', gtop)
    nx = r.choice((19.5, 21.0))
    box(cv, nx - 1.6, 0, nx + 1.6, gtop + 0.5, 'n')
    for y in (r.uniform(2, 5), r.uniform(8, 11)):
        disc(cv, nx - 2.0, y, 2.4, 'l')
    if r.random() < 0.5:
        disc(cv, nx - 0.4, r.uniform(12, 15), 0.9, 'k')
    for x in r.sample((1.5, 4.0, 15.5), 2):
        oval(cv, x, gtop - 1.2, 1.6, 0.9, 'm')
        box(cv, x - 0.4, gtop - 0.6, x + 0.4, gtop + 0.4, 'w')
    if r.random() < 0.5:
        disc(cv, r.uniform(4, 10), 2.8, 1.8, 'u')
    else:
        for k in range(3):
            lens(cv, r.uniform(2, 15), r.uniform(1.5, 8), r.uniform(2, 15) + 1.4, r.uniform(1.5, 8) + 1.0, 1.3, 'm')
    bx = r.uniform(9.5, 11.0)
    tail = curve([(bx - 2.0, gtop - 2.5), (bx - 8.5, gtop - 5.0), (bx - 7.5, gtop - 15.0), (bx - 3.0, gtop - 19.0), (bx - 1.0, gtop - 15.0)], 14)
    tube(cv, tail, 2.4, 1.5, 'q')
    oval(cv, bx, gtop - 6.2, 3.2, 4.6, 'b')
    oval(cv, bx + 1.3, gtop - 6.0, 1.5, 3.4, 'w')
    oval(cv, bx - 0.6, gtop - 2.4, 2.8, 2.0, 'b')
    rbox(cv, bx - 0.6, gtop - 1.2, bx + 3.8, gtop + 0.4, 0.6, 'b')
    hx, hy = bx + 1.6, gtop - 12.6
    disc(cv, hx, hy, 2.6, 'b')
    lens(cv, hx, hy + 0.2, hx + 3.8, hy + 1.2, 2.6, 'b')
    cv.put(int(hx + 3.5), int(hy + 0.9), 'k')
    lens(cv, hx - 1.0, hy - 1.8, hx - 1.4, hy - 4.8, 1.5, 'b')
    eye(cv, hx + 0.8, hy - 1.0, big, 'w', 'k')
    path(cv, [(bx + 1.6, gtop - 9.6), (bx + 3.4, gtop - 8.8)], 'b', 0.6)
    oval(cv, bx + 4.2, gtop - 7.8, 1.0, 1.2, 'a')
    box(cv, bx + 3.2, gtop - 9.6, bx + 5.2, gtop - 8.6, 'k')
    for x in r.sample((1.5, 3.5, 14.5, 17.0), 2):
        oval(cv, x, gtop + 1.6, 0.9, 1.1, 'a')
        box(cv, x - 0.9, gtop + 0.2, x + 0.9, gtop + 0.9, 'k')
    if r.random() < 0.5:
        mirror(cv)
    return cv, [sky(), role('b', 'squirrel', 'Squirrel', BROWN), role('k', 'dark', 'Eye, nose and acorn caps', BROWN),
                role('u', 'sun', 'Sun', BROWN), role('q', 'tail', 'Tail', BROWN), role('n', 'trunk', 'Tree trunk', BROWN),
                role('w', 'white', 'Belly, eye white and stems', BLUE), role('a', 'acorn', 'Acorns', GREEN),
                role('g', 'grass', 'Grass', GREEN), role('l', 'leaves', 'Leaves', GREEN), role('m', 'mushrooms', 'Mushrooms and leaves', PINK)],\
        ['animals', 'forest']


def raccoon(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.12)
    mx = r.choice((3.0, 18.5))
    disc(cv, mx, r.uniform(2.5, 3.5), 2.0, 'm')
    if r.random() < 0.5:
        disc(cv, mx + 1.2, 2.2, 1.7, 's')
    for k in range(r.randint(4, 7)):
        cv.put(int(r.uniform(1, 21)), int(r.uniform(0.5, 7)), 'm')
    meadow(cv, r, 'g', gtop)
    for x in r.sample((1.5, 4.0, 18.0, 20.5), 2):
        disc(cv, x, gtop - 1.0, 2.4, 'v')
    if r.random() < 0.5:
        rbox(cv, -1, gtop - 2.0, 23, gtop + 0.6, 1.0, 'b')
        for x in range(2, 22, 5):
            cv.put(x, int(gtop - 1), 'v')
        gtop -= 2.0
    rx = cx + r.uniform(-0.8, 0.8)
    side = r.choice((-1, 1))
    tube(cv, [(rx + side * 3.6, gtop - 2.4), (rx + side * 7.0, gtop - 3.2), (rx + side * 8.4, gtop - 6.6), (rx + side * 7.8, gtop - 9.6)],
         1.6, 1.1, lambda u: 'k' if int(u * 7) % 2 else 'r')
    oval(cv, rx, gtop - 5.2, 4.2, 5.0, 'r')
    oval(cv, rx, gtop - 4.8, 2.4, 3.4, 'w')
    for sd in (-1, 1):
        oval(cv, rx + sd * 2.4, gtop - 0.4, 1.6, 1.0, 'k')
        oval(cv, rx + sd * 1.8, gtop - 6.6, 1.1, 0.9, 'k')
    hy = gtop - 13.6
    for sd in (-1, 1):
        poly(cv, [(rx + sd * 1.8, hy - 2.8), (rx + sd * 4.4, hy - 6.6), (rx + sd * 5.4, hy - 1.8)], 'r')
        poly(cv, [(rx + sd * 3.0, hy - 3.0), (rx + sd * 4.3, hy - 5.2), (rx + sd * 4.8, hy - 2.6)], 'e')
    oval(cv, rx, hy, 5.8, 4.4, 'r')
    for sd in (-1, 1):
        poly(cv, [(rx + sd * 3.6, hy + 0.6), (rx + sd * 6.8, hy + 2.6), (rx + sd * 3.2, hy + 3.6)], 'w')
        oval(cv, rx + sd * 2.6, hy - 2.6, 2.0, 0.8, 'w')
    for sd in (-1, 1):
        oval(cv, rx + sd * 2.7, hy - 0.2, 2.4, 1.6, 'k')
    oval(cv, rx, hy + 2.4, 2.4, 1.8, 'w')
    box(cv, rx - 0.5, hy - 4.4, rx + 0.5, hy + 0.4, 'k')
    for sd in (-1, 1):
        disc(cv, rx + sd * 2.7, hy - 0.4, 1.1, 'w')
        box(cv, rx + sd * 2.7 - 0.4, hy - 0.8, rx + sd * 2.7 + 0.4, hy + 0.2, 'k')
    oval(cv, rx, hy + 1.6, 1.0, 0.7, 'k')
    path(cv, [(rx - 1.0, hy + 3.2), (rx, hy + 2.6), (rx + 1.0, hy + 3.2)], 'k', 0.4)
    if r.random() < 0.5:
        mirror(cv)
    return cv, [('s', 'sky', 'Night sky', PINK, True), role('e', 'inner_ear', 'Inner ears', PINK), role('r', 'raccoon', 'Raccoon', BLUE),
                role('w', 'white', 'Face and belly', BLUE), role('m', 'moon', 'Moon and stars', BROWN),
                role('k', 'mask', 'Mask, eyes, paws and rings', BROWN), role('b', 'log', 'Log', BROWN),
                role('g', 'grass', 'Grass', GREEN), role('v', 'bushes', 'Bushes', GREEN)], ['animals', 'night']


def hamster(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    fy = h - ground_rows(h, 0.14)
    box(cv, 0, fy, w, h, 'f')
    scene = r.choice(('wheel', 'house', 'bottle'))
    side = r.choice((-1, 1))
    if scene == 'wheel':
        wx = cx + side * 8.0
        seg(cv, wx, fy - 7.0, wx + side * -1.0, fy + 0.5, 'o', 0.6)
        ring(cv, wx, fy - 9.0, 6.6, 5.4, 'o')
        for a in (0, 60, 120):
            t = math.radians(a)
            seg(cv, wx - math.cos(t) * 5.6, fy - 9.0 - math.sin(t) * 5.6, wx + math.cos(t) * 5.6, fy - 9.0 + math.sin(t) * 5.6, 'o', 0.4)
    elif scene == 'house':
        hx0 = cx + side * 8.0
        rbox(cv, hx0 - 4.0, fy - 7.0, hx0 + 4.0, fy + 0.5, 3.0, 'o')
        rbox(cv, hx0 - 1.6, fy - 3.4, hx0 + 1.6, fy + 0.5, 1.5, 'b')
    else:
        bx0 = cx + side * 8.4
        rbox(cv, bx0 - 1.6, 3.0, bx0 + 1.6, 11.0, 0.8, 'o')
        box(cv, bx0 - 0.5, 11.0, bx0 + 0.5, 13.0, 'n')
    ox = cx - side * 7.6
    rbox(cv, ox - 3.0, fy - 2.0, ox + 3.0, fy + 0.5, 1.0, 'o')
    for k in range(3):
        cv.put(int(ox - 1.5 + k * 1.5), int(fy - 2.6), 'd')
    hx = cx + side * r.uniform(-1.2, 0.2)
    oval(cv, hx, fy - 5.2, 6.2, 5.6, 'h')
    oval(cv, hx, fy - 4.4, 3.8, 3.8, 'w')
    for sd in (-1, 1):
        oval(cv, hx + sd * 3.0, fy - 0.4, 1.6, 0.9, 'n')
    hy = fy - 12.4
    for sd in (-1, 1):
        disc(cv, hx + sd * 3.8, hy - 3.8, 1.7, 'h')
        disc(cv, hx + sd * 3.8, hy - 3.8, 0.9, 'n')
    disc(cv, hx, hy, 5.0, 'h')
    for sd in (-1, 1):
        disc(cv, hx + sd * 3.6, hy + 2.4, 2.4, 'w')
    oval(cv, hx, hy + 2.6, 2.4, 2.0, 'w')
    eyes(cv, hx, hy - 1.4, 2.4, big, None, 'e')
    disc(cv, hx, hy + 1.0, 0.7, 'n')
    path(cv, [(hx - 1.0, hy + 2.6), (hx, hy + 2.2), (hx + 1.0, hy + 2.6)], 'e', 0.4)
    lens(cv, hx, fy - 9.0, hx, fy - 5.0, 2.0, 'd')
    for sd in (-1, 1):
        disc(cv, hx + sd * 1.4, fy - 6.8, 0.9, 'n')
    return cv, [('b', 'wall', 'Wall', GREEN, True), role('f', 'floor', 'Bedding', GREEN), role('h', 'hamster', 'Hamster', BROWN),
                role('d', 'seed', 'Seeds', BROWN), role('w', 'white', 'Cheeks and belly', BLUE), role('o', 'toy', 'Wheel, house and bowl', BLUE),
                role('n', 'pink', 'Nose, ears and paws', PINK), role('e', 'eye', 'Eyes and mouth', PINK)], ['animals', 'cozy']


def hippo(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.14)
    wade = r.random() < 0.45
    meadow(cv, r, 'g', gtop)
    for x in r.sample((0.8, 2.4, 19.6, 21.2), 2):
        seg(cv, x, gtop + 0.5, x, gtop - r.uniform(6, 9), 'd', 0.45)
        oval(cv, x, gtop - 8.5, 0.6, 1.4, 'k')
    disc(cv, r.choice((3.5, 9.0)), r.uniform(2.8, 4.4), 2.2, 'u')
    if r.random() < 0.5:
        cloud(cv, r.uniform(13, 18), 3.0, 1.2, 'w')
    by = gtop - 7.8 + (1.6 if wade else 0)
    oval(cv, 8.5, by, 7.2, 4.6, 'p')
    for x0 in (1.8, 5.4, 10.2, 13.6):
        rbox(cv, x0, by + 2.4, x0 + 2.6, gtop + 0.5, 0.8, 'p')
    path(cv, [(1.4, by - 1.0), (0.4, by + 0.6)], 'p', 0.5)
    rbox(cv, 13.0, by - 6.6, 20.4, by + 0.4, 2.6, 'p')
    disc(cv, 19.0, by - 1.0, 2.8, 'p')
    for x, y in ((14.4, by - 6.6), (16.0, by - 7.0)):
        disc(cv, x, y, 1.0, 'p')
    disc(cv, 16.6, by - 5.6, 1.4, 'p')
    eye(cv, 16.6, by - 6.1, big, 'w', 'k')
    for x, y in ((19.6, by - 4.0), (21.0, by - 3.6)):
        cv.put(int(x), int(y), 'k')
    if r.random() < 0.5:
        poly(cv, [(15.6, by - 0.6), (21.8, by - 3.0), (21.8, by + 2.0)], 'm')
        for y in (by - 2.0, by + 1.0):
            cv.put(20, int(y), 'w')
    else:
        path(cv, [(16.0, by + 0.2), (18.6, by + 0.8), (21.0, by)], 'k', 0.45)
    if wade:
        hills(cv, 'r', by + 2.4, 0.4, 22 * 0.45, r.uniform(0, 6))
        for k in range(3):
            x = r.uniform(2, 20)
            seg(cv, x - 1.0, by + 4.6 + k * 1.6, x + 1.0, by + 4.6 + k * 1.6, 'w', 0.4)
        for x in r.sample((3.0, 9.0, 15.0), 2):
            oval(cv, x, gtop + 1.4, 1.4, 0.6, 'd')
    else:
        px = r.choice((2.0, 9.0))
        oval(cv, px + 5.0, gtop + 2.4, 6.0, 1.6, 'r')
        for x in (px + 1.4, px + 7.6):
            oval(cv, x, gtop + 2.2, 1.2, 0.6, 'd')
    if r.random() < 0.5:
        bx, byy = r.uniform(5, 9), by - 5.0
        oval(cv, bx, byy, 1.4, 1.0, 'y')
        disc(cv, bx + 1.2, byy - 0.8, 0.8, 'y')
        cv.put(int(bx + 2.2), int(byy - 0.8), 'k')
    if r.random() < 0.5:
        mirror(cv)
    return cv, [sky(), role('p', 'hippo', 'Hippo', PINK), role('m', 'mouth', 'Mouth', PINK), role('w', 'white', 'Teeth, eye white, ripples and cloud', BLUE),
                role('r', 'river', 'River', BLUE), role('u', 'sun', 'Sun', BROWN), role('k', 'dark', 'Eye, nostrils and cattails', BROWN),
                role('y', 'bird', 'Bird', BROWN), role('g', 'grass', 'Grass', GREEN), role('d', 'reeds', 'Reeds and lily pads', GREEN)],\
        ['animals', 'river']


def crocodile(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    wl = r.uniform(18.0, 19.0)
    hills(cv, 'r', 14.5, 0.3, 22 * 0.5, r.uniform(0, 6))
    if r.random() < 0.6:
        ix = r.uniform(3, 7)
        oval(cv, ix, 14.6, 4.0, 1.2, 'd')
        palm(cv, ix, 14.0, 6.0, 'k', 'l', 0.6)
    disc(cv, r.uniform(13, 18), r.uniform(2.2, 3.4), 2.0, 'u')
    open_ = r.random() < 0.55
    for x in (0.8, 2.8):
        disc(cv, x, wl + 0.2, 1.4, 'c')
        disc(cv, x, wl - 1.0, 0.7, 'q')
    lens(cv, 4.2, wl + 0.8, 21.4, wl + 0.4, 2.8, 'c')
    disc(cv, 5.6, wl - 1.4, 3.0, 'c')
    if open_:
        lens(cv, 4.6, wl - 2.4, 20.4, wl - 11.0, 3.6, 'c')
        poly(cv, [(7.0, wl - 1.0), (19.4, wl - 8.8), (21.0, wl - 0.2)], 'm')
        for k in range(5):
            t = 0.12 + k * 0.17
            x, y = 7.8 + 11.6 * t, wl - 2.2 - 7.2 * t
            poly(cv, [(x - 0.7, y - 0.2), (x + 0.7, y - 1.0), (x + 0.4, y + 1.0)], 'w')
            poly(cv, [(x - 0.4, wl + 0.2), (x + 0.8, wl + 0.2), (x + 0.2, wl - 1.4)], 'w')
        disc(cv, 20.2, wl - 11.6, 1.0, 'c')
        cv.put(20, int(wl - 12.2), 'k')
    else:
        lens(cv, 4.6, wl - 1.6, 21.6, wl - 1.2, 3.4, 'c')
        for x in range(8, 21, 2):
            cv.put(x, int(wl - 0.4), 'w')
            cv.put(x + 1, int(wl + 0.4), 'w')
        disc(cv, 20.6, wl - 3.0, 1.0, 'c')
        cv.put(20, int(wl - 3.4), 'k')
    disc(cv, 6.4, wl - 4.8, 2.0, 'c')
    eye(cv, 6.4, wl - 5.8, big, 'w', 'k')
    for x, y in ((2.6, wl - 3.0), (4.2, wl - 4.2), (0.8, wl - 2.2)):
        disc(cv, x, y, 0.75, 'q')
    hills(cv, 'r', wl + 1.4, 0.3, 22 * 0.4, r.uniform(0, 6), only='cqw')
    for k in range(r.randint(3, 5)):
        x, y = r.uniform(2, 20), r.uniform(wl + 3, 26.5)
        seg(cv, x - 1.0, y, x + 1.0, y, 'w', 0.4)
    if r.random() < 0.5:
        for x in r.sample((3.0, 9.0, 15.0, 19.0), 2):
            oval(cv, x, r.uniform(wl + 4, 26), 1.6, 0.7, 'l')
    if r.random() < 0.5:
        mirror(cv)
    return cv, [sky(), role('c', 'croc', 'Crocodile', GREEN), role('q', 'scutes', 'Back ridges', GREEN), role('m', 'mouth', 'Mouth', PINK),
                role('w', 'white', 'Teeth, eye white and ripples', BLUE), role('r', 'river', 'River', BLUE), role('d', 'sand', 'Island', BROWN),
                role('k', 'dark', 'Eye, nostril and palm trunk', BROWN), role('u', 'sun', 'Sun', PINK),
                role('l', 'leaves', 'Palm leaves and lily pads', GREEN)], ['animals', 'river']


def chameleon(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    tilt = r.uniform(-1.5, 1.0)
    b0, b1 = 19.0 - tilt, 16.5 + tilt
    path(cv, [(-1, b0), (11, (b0 + b1) / 2 + 0.6), (23, b1)], 'n', 1.0)
    for x in r.sample((14.0, 17.0, 20.0), 2):
        y = b0 + (b1 - b0) * x / 22
        seg(cv, x, y, x + 1.4, y + 3.0, 'n', 0.45)
        lens(cv, x + 1.4, y + 3.0, x + 4.0, y + 4.8, 1.8, 'l')
    for x, y, a in r.sample(((0.0, 2.0, 30), (22.0, 3.0, 150), (0.0, 8.0, -15), (22.0, 26.0, 200)), 2):
        t = math.radians(a)
        lens(cv, x, y, x + math.cos(t) * 5.6, y + math.sin(t) * 5.6, 2.6, 'l')
    for k in range(2):
        disc(cv, r.uniform(9, 20), r.uniform(23.5, 27), 1.0, 'o')
    by = (b0 + b1) / 2 - 3.8
    ccx, ccy = 4.0, by + 8.2
    spiral = [(ccx + (3.2 - 0.3 * t) * math.cos(-t), ccy + (3.2 - 0.3 * t) * math.sin(-t)) for t in [k * 0.4 for k in range(17)]]
    tube(cv, [(6.6, by + 1.2), (7.2, by + 5.0)] + spiral, 0.9, 0.5, 'c')
    oval(cv, 10.2, by, 5.0, 2.7, 'c')
    for k in range(5):
        x = 6.4 + k * 1.6
        poly(cv, [(x - 0.8, by - 2.4), (x, by - 3.6), (x + 0.8, by - 2.4)], 'c')
    poly(cv, [(13.0, by - 2.4), (15.0, by - 3.0), (19.4, by - 0.6), (19.0, by + 1.0), (14.0, by + 2.4)], 'c')
    poly(cv, [(12.8, by - 1.6), (14.2, by - 5.6), (16.4, by - 2.4)], 'c')
    for x in (12.4, 7.6):
        path(cv, [(x, by + 2.0), (x + 0.8, by + 3.6), (x + 1.8, by + 4.0)], 'c', 0.7)
    for k in range(4):
        disc(cv, 7.4 + k * 2.0, by + 0.4 + (k % 2) * 0.8, 0.6, 'q')
    disc(cv, 16.2, by - 1.0, 1.7, 'c')
    eye(cv, 16.2, by - 1.6, big, 'e', 'k')
    path(cv, [(19.0, by + 0.4), (16.6, by + 1.2)], 'k', 0.4)
    if r.random() < 0.6:
        fx, fy = r.uniform(19.5, 21.0), by - r.uniform(5.0, 8.0)
        seg(cv, 19.2, by + 0.2, fx - 0.4, fy + 0.8, 'q', 0.45)
        disc(cv, fx - 0.4, fy + 0.8, 0.8, 'q')
        cv.put(int(fx), int(fy), 'k')
        cv.put(int(fx) - 1, int(fy) - 1, 'e')
        cv.put(int(fx) + 1, int(fy) - 1, 'e')
    disc(cv, r.choice((3.5, 9.0)), r.uniform(2.6, 4.0), 2.0, 'u')
    if r.random() < 0.5:
        mirror(cv)
    return cv, [sky(), role('c', 'chameleon', 'Chameleon', GREEN), role('l', 'leaves', 'Leaves', GREEN), role('n', 'branch', 'Branch', BROWN),
                role('k', 'eye', 'Eye, mouth and fly', BROWN), role('u', 'sun', 'Sun', BROWN), role('q', 'spots', 'Spots and tongue', PINK),
                role('o', 'flowers', 'Flowers', PINK), role('e', 'eye_white', 'Eye and wings', BLUE)], ['animals', 'jungle']


def dinosaur(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.14)
    if r.random() < 0.6:
        vx = r.choice((4.0, 17.0))
        poly(cv, [(vx - 7, gtop), (vx - 1.6, gtop - 9.0), (vx + 1.6, gtop - 9.0), (vx + 7, gtop)], 'v')
        poly(cv, [(vx - 1.6, gtop - 9.0), (vx + 1.6, gtop - 9.0), (vx + 1.0, gtop - 7.0), (vx, gtop - 8.0), (vx - 1.0, gtop - 6.4)], 'l')
        for k in range(3):
            disc(cv, vx + k * 1.2 - 0.6, gtop - 10.6 - k * 1.8, 0.9 + k * 0.3, 'c')
    else:
        cloud(cv, r.uniform(4, 9), 3.0, 1.3, 'c')
    hills(cv, 'g', gtop, 0.6, 22 * r.uniform(0.9, 1.4), r.uniform(0, 6))
    for x in r.sample((1.5, 20.5, 18.5), 2):
        for a in (-150, -115, -65, -30):
            t = math.radians(a)
            lens(cv, x, gtop + 0.5, x + math.cos(t) * 3.4, gtop + 0.5 + math.sin(t) * 3.6, 1.4, 'f')
    kind = r.choice(('longneck', 'stego'))
    if kind == 'longneck':
        by = gtop - 8.0
        tube(cv, [(3.2, by - 1.0), (0.8, by + 1.0), (-0.6, by + 3.6)], 2.0, 0.8, 'd')
        oval(cv, 8.0, by, 6.0, 3.8, 'd')
        for x0 in (2.2, 5.6, 9.6, 12.6):
            box(cv, x0, by + 1.5, x0 + 2.0, gtop + 0.5, 'd')
        tube(cv, [(12.4, by - 2.0), (15.4, by - 6.4), (16.8, by - 11.6)], 2.0, 1.2, 'd')
        hx, hy = 18.0, by - 12.6
        oval(cv, hx, hy, 2.4, 1.5, 'd')
        for k in range(r.randint(4, 6)):
            blot(cv, r.uniform(4.5, 11.5), r.uniform(by - 2.8, by - 0.4), 0.9, 'p', 'd')
        path(cv, [(hx + 0.6, hy + 0.8), (hx + 1.8, hy + 0.6)], 'k', 0.4)
        eye(cv, hx - 0.4, hy - 1.0, big, 'e', 'k')
    else:
        by = gtop - 6.6
        for k in range(5):
            x = 4.4 + k * 2.6
            y = by - 3.4 - math.sin(math.pi * (k + 0.5) / 5) * 1.6
            poly(cv, [(x - 1.4, y + 1.6), (x, y - 2.4), (x + 1.4, y + 1.6)], 'q')
        tube(cv, [(3.4, by), (0.8, by + 1.4), (-0.2, by + 3.0)], 1.8, 0.8, 'd')
        for x, y in ((1.6, by - 0.4), (0.4, by + 0.8)):
            poly(cv, [(x - 0.6, y), (x - 1.0, y - 2.0), (x + 0.6, y)], 'q')
        oval(cv, 9.4, by, 6.4, 3.4, 'd')
        for x0 in (3.4, 6.2, 11.8, 14.0):
            box(cv, x0, by + 1.5, x0 + 2.0, gtop + 0.5, 'd')
        tube(cv, [(15.0, by + 0.4), (17.4, by + 1.4)], 1.6, 1.2, 'd')
        hx, hy = 18.6, by + 1.6
        oval(cv, hx, hy, 2.2, 1.5, 'd')
        for k in range(r.randint(3, 5)):
            blot(cv, r.uniform(5.5, 13.5), r.uniform(by - 1.0, by + 1.4), 0.9, 'p', 'd')
        path(cv, [(hx + 0.4, hy + 0.8), (hx + 1.8, hy + 0.6)], 'k', 0.4)
        eye(cv, hx + 0.2, hy - 1.0, big, 'e', 'k')
    if r.random() < 0.5:
        ex = r.choice((16.0, 19.0)) if kind == 'longneck' else r.choice((1.5, 20.5))
        for dx in (-1.0, 1.0):
            oval(cv, ex + dx, gtop + 1.4, 0.9, 1.2, 'e')
    disc(cv, r.choice((3.0, 9.0)) if kind == 'stego' else 3.0, 2.8, 2.0, 'u')
    if r.random() < 0.5:
        mirror(cv)
    return cv, [sky(), role('d', 'dino', 'Dinosaur', GREEN), role('p', 'spots', 'Spots', GREEN), role('f', 'ferns', 'Ferns', GREEN),
                role('g', 'ground', 'Ground', BROWN), role('k', 'eye', 'Eye and smile', BROWN), role('v', 'volcano', 'Volcano', BROWN),
                role('u', 'sun', 'Sun', BROWN), role('q', 'plates', 'Back plates', PINK), role('l', 'lava', 'Lava', PINK),
                role('e', 'eye_white', 'Eye white and eggs', BLUE), role('c', 'cloud', 'Smoke and cloud', PINK)], ['animals', 'dinosaurs']


def dragon(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.14)
    scene = r.choice(('castle', 'clouds', 'gold'))
    if scene == 'castle':
        kx = 16.8
        box(cv, kx, gtop - 7, kx + 4.6, gtop, 'c')
        for x in (kx, kx + 1.8, kx + 3.6):
            box(cv, x, gtop - 8, x + 0.9, gtop - 7, 'c')
        box(cv, kx + 1.8, gtop - 3.4, kx + 2.8, gtop - 1.0, 's')
    elif scene == 'clouds':
        cloud(cv, r.uniform(12, 15), 2.0, 1.0, 'c')
    hills(cv, 'g', gtop, 0.6, 22 * r.uniform(0.9, 1.3), r.uniform(0, 6))
    if scene == 'gold':
        for k in range(6):
            disc(cv, 15.6 + (k % 3) * 1.8 + (k // 3) * 0.9, gtop - 0.6 - (k // 3) * 1.4, 1.0, 'y')
    by = gtop - 6.6
    sx, sy = 8.4, by - 2.4
    wx, wy = 6.4, by - 12.0
    tips = [(0.6, by - 13.6), (0.4, by - 8.6), (2.4, by - 4.6)]
    vals = [(3.2, by - 10.4), (3.0, by - 6.4), (5.4, by - 3.2)]
    poly(cv, [(sx, sy), (wx, wy), tips[0], vals[0], tips[1], vals[1], tips[2], vals[2], (sx - 1.6, sy + 0.6)], 'w')
    seg(cv, sx, sy, wx, wy, 'q', 0.6)
    for tx_, ty_ in tips:
        seg(cv, wx, wy, tx_ + 0.4, ty_, 'q', 0.4)
    tube(cv, [(4.4, by + 0.4), (1.8, by + 2.2), (1.2, by + 4.6), (3.6, by + 5.6)], 1.4, 0.6, 'd')
    poly(cv, [(3.0, by + 5.0), (6.0, by + 4.6), (4.6, by + 7.4)], 'q')
    oval(cv, 8.8, by, 5.0, 3.0, 'd')
    box(cv, 5.4, by + 1.4, 13.0, by + 3.0, 'b', only='d')
    disc(cv, 5.8, by + 1.6, 2.4, 'd')
    rbox(cv, 4.2, gtop - 1.4, 8.4, gtop + 0.5, 0.6, 'd')
    rbox(cv, 11.2, by + 1.0, 13.4, gtop + 0.5, 0.6, 'd')
    rbox(cv, 11.2, gtop - 1.2, 14.6, gtop + 0.5, 0.5, 'd')
    neck = [(12.2, by - 1.2), (14.4, by - 5.0), (14.8, by - 8.4)]
    tube(cv, neck, 1.9, 1.4, 'd')
    for k in range(6):
        t = k / 5
        x, y = 5.0 + t * 8.6, by - 2.4 - t * t * 6.4
        poly(cv, [(x - 0.8, y + 0.6), (x - 0.5, y - 1.3), (x + 0.8, y + 0.2)], 'q')
    hx, hy = 15.4, by - 9.8
    for dx in (-1.6, -0.2):
        path(cv, [(hx + dx, hy - 1.6), (hx + dx - 1.0, hy - 3.2), (hx + dx - 2.2, hy - 3.6)], 'h', 0.45)
    oval(cv, hx, hy, 2.4, 1.9, 'd')
    lens(cv, hx + 0.2, hy + 0.2, hx + 5.0, hy - 1.0, 2.6, 'd')
    cv.put(int(hx + 4.4), int(hy - 1.2), 'k')
    eye(cv, hx - 0.2, hy - 1.2, big, 'e', 'k')
    path(cv, [(hx + 1.2, hy + 1.0), (hx + 3.8, hy + 0.4)], 'k', 0.4)
    if r.random() < 0.6:
        fx, fy = hx + 5.2, hy - 1.4
        lens(cv, fx - 0.6, fy + 0.6, fx + 1.6, fy - 7.2, 3.6, 'f')
        lens(cv, fx - 0.4, fy + 0.2, fx + 0.8, fy - 4.0, 1.6, 'y')
    else:
        for k in range(3):
            disc(cv, hx + 5.4 + k * 0.3, hy - 2.6 - k * 1.8, 0.6 + k * 0.25, 'c')
    if r.random() < 0.5:
        mirror(cv)
    return cv, [sky(), role('d', 'dragon', 'Dragon', GREEN), role('w', 'wing', 'Wing', PINK),
                role('q', 'spikes', 'Spikes, wing bones and tail tip', PINK), role('b', 'belly', 'Belly', BROWN), role('h', 'horns', 'Horns', BROWN),
                role('y', 'flame', 'Flame core and gold', BROWN), role('f', 'fire', 'Fire', BROWN), role('k', 'eye', 'Eye and nostril', BROWN),
                role('e', 'eye_white', 'Eye white', BLUE), role('c', 'castle', 'Castle, clouds and smoke', BLUE), role('g', 'hill', 'Hill', GREEN)],\
        ['animals', 'fairy']


def flamingo(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    wl = r.uniform(20.5, 22.0)
    hills(cv, 'w', wl, 0.3, 22 * 0.5, r.uniform(0, 6))
    if r.random() < 0.6:
        palm(cv, r.choice((2.0, 19.5)), wl + 0.5, r.uniform(6, 9), 't', 'l', r.choice((-0.8, 0.8)))
    for x in r.sample((1.0, 3.0, 17.5, 19.5, 21.0), 3):
        seg(cv, x, wl + 1.0, x + r.uniform(-0.6, 0.6), wl - r.uniform(3, 5), 'l', 0.45)
    disc(cv, r.choice((4.0, 16.0)), r.uniform(2.6, 4.2), 2.0, 'u')
    for k in range(r.randint(2, 4)):
        x, y = r.uniform(2, 20), r.uniform(wl + 2, 27)
        seg(cv, x - 1.0, y, x + 1.0, y, 'r', 0.4)
    fx = r.uniform(7.5, 9.0)
    by = wl - 8.4
    one_leg = r.random() < 0.6
    seg(cv, fx + 0.5, by + 2.0, fx + 0.5, wl + 1.0, 'p', 0.45)
    if one_leg:
        path(cv, [(fx - 0.5, by + 2.0), (fx + 0.4, by + 5.0), (fx - 2.2, by + 4.0)], 'p', 0.45)
    else:
        path(cv, [(fx - 0.5, by + 2.0), (fx - 1.4, wl + 1.0)], 'p', 0.45)
    oval(cv, fx, by, 4.2, 2.6, 'p')
    poly(cv, [(fx - 3.4, by - 1.0), (fx - 6.0, by - 2.4), (fx - 3.8, by + 1.6)], 'p')
    lens(cv, fx - 3.4, by - 0.6, fx + 2.6, by + 0.8, 2.2, 'q')
    feed = r.random() < 0.45
    if feed:
        neck = curve([(fx + 3.4, by - 1.0), (fx + 6.6, by - 4.0), (fx + 9.4, by + 0.0), (fx + 9.6, wl - 2.0)], 12)
        tube(cv, neck, 0.75, 0.7, 'p')
        hx, hy = fx + 9.6, wl - 2.4
        disc(cv, hx, hy, 1.3, 'p')
        poly(cv, [(hx - 0.6, hy + 0.6), (hx + 1.0, hy + 0.4), (hx + 0.2, wl + 0.8)], 'y')
        cv.put(int(hx + 0.2), int(wl + 0.2), 'k')
        cv.put(int(hx - 0.4), int(hy - 0.4), 'k')
    else:
        neck = curve([(fx + 3.0, by - 1.2), (fx + 6.4, by - 3.4), (fx + 2.2, by - 6.2), (fx + 3.6, by - 8.8), (fx + 5.6, by - 9.0)], 14)
        tube(cv, neck, 0.75, 0.7, 'p')
        hx, hy = fx + 5.6, by - 8.8
        disc(cv, hx, hy, 1.4, 'p')
        poly(cv, [(hx + 0.8, hy - 0.8), (hx + 3.6, hy + 0.2), (hx + 3.0, hy + 2.4), (hx + 1.0, hy + 0.8)], 'y')
        poly(cv, [(hx + 2.6, hy - 0.1), (hx + 3.6, hy + 0.2), (hx + 3.0, hy + 2.4), (hx + 2.4, hy + 1.2)], 'k')
        cv.put(int(hx), int(hy - 0.4), 'k')
    if r.random() < 0.5:
        sx, sy = r.choice((15.5, 18.0)), wl - 4.0
        seg(cv, sx, sy + 1.0, sx, wl + 0.6, 'p', 0.4)
        oval(cv, sx, sy, 1.6, 1.0, 'p')
        path(cv, [(sx + 1.2, sy - 0.4), (sx + 1.8, sy - 2.4), (sx + 1.2, sy - 3.6)], 'p', 0.45)
        cv.put(int(sx + 2.0), int(sy - 3.6), 'y')
    if r.random() < 0.5:
        mirror(cv)
    return cv, [sky(), role('p', 'flamingo', 'Flamingo', PINK), role('q', 'wing', 'Wing', PINK), role('y', 'beak', 'Beak', BROWN),
                role('k', 'dark', 'Beak tip and eye', BROWN), role('u', 'sun', 'Sun', BROWN), role('t', 'trunk', 'Palm trunk', BROWN),
                role('w', 'lagoon', 'Lagoon', BLUE), role('r', 'ripples', 'Ripples', GREEN), role('l', 'leaves', 'Palm and reeds', GREEN)],\
        ['animals', 'beach']


def peacock(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.14)
    meadow(cv, r, 'g', gtop)
    px = cx + r.choice((-1, 1)) * r.uniform(0.0, 2.0)
    fy = gtop - r.uniform(6.0, 7.5)
    sq = r.uniform(0.8, 1.05)
    R = min(r.uniform(8.6, 10.4), px - 0.8, 21.2 - px)
    cut = r.uniform(0.5, 2.5)
    for x in r.sample((1.5, 20.5), r.randint(0, 2)):
        disc(cv, x, gtop - 1.0, 2.2, 'g')
    for y in range(h):
        for x in range(w):
            dx, dy = x + 0.5 - px, (y + 0.5 - fy) / sq
            if dx * dx + dy * dy <= R * R and dy < cut:
                cv.g[y][x] = 'f'
    n = r.choice((7, 9))
    for k in range(1, n):
        a = math.radians(180 + 180 * k / n)
        seg(cv, px + math.cos(a) * R * 0.62, fy + math.sin(a) * R * 0.62 * sq, px + math.cos(a) * (R - 1.3), fy + math.sin(a) * (R - 1.3) * sq, 'd', 0.35)
    for rad, m in ((R - 2.0, n), (R * 0.4, n - 4)):
        for k in range(m):
            a = math.radians(180 + 180 * (k + 0.5) / m)
            ex, ey = px + math.cos(a) * rad, fy + math.sin(a) * rad * sq
            disc(cv, ex, ey, 1.1, 'o')
            cv.put(int(ex), int(ey), 'e')
    for sd in (-1, 1):
        lens(cv, px + sd * 1.0, fy - 1.0, px + sd * 3.4, fy + 3.4, 2.0, 'n')
    oval(cv, px, fy + 1.4, 2.4, 3.6, 'b')
    oval(cv, px, fy - 3.6, 1.2, 2.6, 'b')
    turn = r.choice((-1, 0, 1))
    hx, hy = px + turn * 0.6, fy - 6.4
    disc(cv, hx, hy, 1.7, 'b')
    for dx in (-1.0, 0.0, 1.0):
        seg(cv, hx + dx * 0.5, hy - 1.4, hx + dx * 1.2, hy - 3.4, 'b', 0.35)
        cv.put(int(hx + dx * 1.2), int(hy - 3.6), 'o')
    if turn:
        cv.put(int(hx - turn * 0.2), int(hy - 0.4), 'k')
        poly(cv, [(hx + turn * 1.0, hy - 0.4), (hx + turn * 2.6, hy + 0.2), (hx + turn * 1.0, hy + 0.8)], 'k')
    else:
        for sd in (-1, 1):
            cv.put(int(hx + sd * 0.8), int(hy - 0.4), 'k')
        cv.put(int(hx), int(hy + 0.6), 'k')
    for sd in (-1, 1):
        path(cv, [(px + sd * 0.8, fy + 4.6), (px + sd * 1.0, gtop + 0.4)], 'k', 0.4)
    for x in r.sample((1.5, 4.0, 17.5, 20.5), 2):
        disc(cv, x, gtop + 1.6, 0.9, 'p')
    if r.random() < 0.5:
        disc(cv, r.choice((2.5, 19.5)), 2.6, 1.8, 'u')
    return cv, [('s', 'sky', 'Dawn sky', PINK, True), role('p', 'flowers', 'Flowers', PINK), role('u', 'sun', 'Sun', PINK),
                role('f', 'fan', 'Tail fan', GREEN), role('d', 'shafts', 'Feather shafts', GREEN), role('g', 'lawn', 'Lawn', GREEN),
                role('o', 'eyespots', 'Eye spots and crest', BROWN), role('n', 'wings', 'Wings', BROWN), role('k', 'dark', 'Eyes, beak and legs', BROWN),
                role('b', 'peacock', 'Peacock', BLUE), role('e', 'spot_centers', 'Eye spot centres', BLUE)], ['animals', 'garden']


def swan(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    wl = r.uniform(17.5, 19.0)
    if r.random() < 0.5:
        disc(cv, r.uniform(4, 18), wl - 0.5, 3.6, 'u')
    else:
        disc(cv, r.choice((3.5, 18.0)), r.uniform(2.6, 4.0), 2.0, 'u')
    for x in r.sample((3.0, 9.0, 15.0, 19.0), 2):
        oval(cv, x, wl - 0.4, 3.6, 1.4, 't')
    hills(cv, 'p', wl, 0.3, 22 * 0.5, r.uniform(0, 6))
    for x in r.sample((0.8, 2.2, 19.8, 21.2), 2):
        seg(cv, x, h, x, wl - r.uniform(4, 6), 'l', 0.45)
        oval(cv, x, wl - 5.5, 0.6, 1.3, 'k')
    for k in range(r.randint(2, 3)):
        x, y = r.uniform(2, 20), r.uniform(wl + 5, 27)
        oval(cv, x, y, 1.6, 0.7, 'l')
        cv.put(int(x), int(y), 'o')
    sx = r.uniform(8.0, 9.5)
    by = wl + 0.4
    lifted = r.random() < 0.4
    oval(cv, sx, by, 6.0, 2.6, 'w')
    poly(cv, [(sx - 5.4, by - 0.6), (sx - 7.4, by - 3.0), (sx - 4.4, by - 1.6)], 'w')
    if lifted:
        poly(cv, [(sx - 4.6, by - 0.8), (sx - 3.4, by - 6.4), (sx - 0.4, by - 4.4), (sx + 2.4, by - 6.0), (sx + 3.0, by - 0.6)], 'w')
        for k in range(3):
            seg(cv, sx - 3.0 + k * 1.8, by - 1.0, sx - 3.2 + k * 2.2, by - 4.6 + (k == 1) * 0.8, 'q', 0.35)
    else:
        lens(cv, sx - 4.8, by - 0.8, sx + 3.0, by - 0.8, 2.6, 'q')
        lens(cv, sx - 4.0, by - 1.0, sx + 2.2, by - 0.9, 1.0, 'w')
    neck = curve([(sx + 4.4, by - 0.4), (sx + 7.6, by - 3.4), (sx + 3.6, by - 7.6), (sx + 5.4, by - 11.6)], 14)
    tube(cv, neck, 1.1, 0.8, 'w')
    hx, hy = sx + 5.8, by - 11.8
    disc(cv, hx, hy, 1.4, 'w')
    poly(cv, [(hx + 0.6, hy - 0.6), (hx + 3.6, hy + 0.8), (hx + 0.8, hy + 1.0)], 'y')
    cv.put(int(hx + 0.8), int(hy - 0.2), 'k')
    cv.put(int(hx - 0.2), int(hy - 0.4), 'k')
    if r.random() < 0.5:
        for k in range(r.randint(1, 2)):
            cx_ = sx - 9.0 - k * 4.0 if sx > 12 else sx + 9.0 + k * 3.6
            if 1 < cx_ < 21:
                oval(cv, cx_, by + 0.6, 1.6, 0.9, 'w')
                disc(cv, cx_ + 1.2, by - 0.8, 0.8, 'w')
                cv.put(int(cx_ + 1.9), int(by - 0.8), 'y')
    hills(cv, 'p', by + 1.8, 0.3, 22 * 0.4, r.uniform(0, 6), only='wq')
    if r.random() < 0.5:
        mirror(cv)
    return cv, [('s', 'sky', 'Sunset sky', PINK, True), role('o', 'lilies', 'Water lilies', PINK), role('w', 'swan', 'Swan', BLUE),
                role('q', 'feathers', 'Wing feathers', BLUE), role('y', 'beak', 'Beak', BROWN), role('k', 'dark', 'Eye, knob and cattails', BROWN),
                role('u', 'sun', 'Sun', BROWN), role('p', 'pond', 'Pond', GREEN), role('l', 'lily_pads', 'Lily pads and reeds', GREEN),
                role('t', 'trees', 'Far trees', GREEN)], ['animals', 'pond']


def toucan(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    for x, y, a in r.sample(((0.0, 1.0, 25), (22.0, 2.0, 155), (0.0, 7.0, -20), (22.0, 26.0, 200), (0.0, 26.0, -30)), 3):
        t = math.radians(a)
        lens(cv, x, y, x + math.cos(t) * 6.4, y + math.sin(t) * 6.4, 3.0, 'l')
        seg(cv, x, y, x + math.cos(t) * 5.0, y + math.sin(t) * 5.0, 'v', 0.35)
    for k in range(r.randint(1, 2)):
        x = r.uniform(14, 21)
        path(cv, [(x, -1), (x + r.uniform(-1, 1), 6), (x + r.uniform(-1, 1), 11)], 'v', 0.45)
    by = r.uniform(19.0, 20.5)
    path(cv, [(-1, by + 0.8), (8, by), (16.5, by - 0.6)], 'n', 1.0)
    lens(cv, 16.4, by - 0.6, 19.4, by - 2.4, 1.6, 'l')
    tx = r.uniform(7.0, 8.5)
    lens(cv, tx - 0.6, by - 3.0, tx - 2.2, by + 5.4, 2.6, 'k')
    oval(cv, tx, by - 5.8, 3.4, 4.8, 'k')
    oval(cv, tx + 1.0, by + 0.0, 1.2, 0.8, 'f')
    hx, hy = tx + 1.0, by - 11.4
    disc(cv, hx, hy, 2.8, 'k')
    oval(cv, tx + 2.0, by - 7.8, 1.8, 2.4, 'y')
    poly(cv, [(hx + 1.6, hy - 2.0), (hx + 6.0, hy - 2.4), (hx + 9.6, hy + 0.4), (hx + 9.4, hy + 1.6), (hx + 2.2, hy + 1.8)], 'b')
    poly(cv, [(hx + 8.0, hy - 0.8), (hx + 9.6, hy + 0.4), (hx + 9.4, hy + 1.6), (hx + 8.0, hy + 1.6)], 'k')
    path(cv, [(hx + 2.4, hy + 0.4), (hx + 8.8, hy + 0.6)], 'q', 0.35)
    box(cv, hx + 1.2, hy - 2.0, hx + 2.0, hy + 1.8, 'k')
    disc(cv, hx + 0.2, hy - 0.6, 1.1, 'e')
    cv.put(int(hx + 0.2), int(hy - 0.6), 'q')
    for dx in (0.4, 1.8):
        cv.put(int(tx + dx), int(by - 1.0), 'e')
    if r.random() < 0.6:
        for k in range(r.randint(1, 2)):
            x, y = r.uniform(1.5, 5.0), r.uniform(10, 16) + k * 4
            for a in range(5):
                t = math.radians(a * 72)
                disc(cv, x + math.cos(t) * 0.9, y + math.sin(t) * 0.9, 0.6, 'f')
    if r.random() < 0.5:
        mirror(cv)
    return cv, [sky(), role('f', 'flowers', 'Flowers and under-tail', PINK), role('k', 'toucan', 'Toucan', PINK),
                role('b', 'beak', 'Beak', BROWN), role('q', 'pupil', 'Beak line and pupil', BROWN), role('y', 'bib', 'Throat', BROWN),
                role('n', 'branch', 'Branch', BROWN), role('e', 'eye', 'Eye ring and feet', BLUE), role('l', 'leaves', 'Jungle leaves', GREEN),
                role('v', 'vines', 'Vines and leaf ribs', GREEN)], ['animals', 'jungle']


def beetle(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    a = r.uniform(-0.5, 0.5)
    seg(cv, cx + math.sin(a) * 12, 14 - math.cos(a) * 12, cx - math.sin(a) * 12, 14 + math.cos(a) * 12, 'v', 0.5)
    for k in range(-2, 3):
        y0 = 14 + k * 5.0
        for sd in (-1, 1):
            seg(cv, cx - math.sin(a) * (y0 - 14), y0, cx + sd * 8.4, y0 - 3.4, 'v', 0.4)
    corner = r.choice(((1.5, 1.5), (20.5, 1.5), (1.5, 26.5), (20.5, 26.5)))
    for k in range(5):
        t = math.radians(k * 72)
        disc(cv, corner[0] + math.cos(t) * 1.4, corner[1] + math.sin(t) * 1.4, 1.0, 'f')
    disc(cv, corner[0], corner[1], 0.8, 'c')
    for k in range(r.randint(2, 3)):
        x, y = r.choice((r.uniform(1.5, 4.5), r.uniform(17.5, 20.5))), r.uniform(5, 23)
        disc(cv, x, y, 0.9, 'd')
    bx, by = cx + r.uniform(-1.0, 1.0), r.uniform(15.0, 16.5)
    kind = r.choice(('stag', 'rhino', 'scarab'))
    for sd in (-1, 1):
        for y0, pts in ((by - 5.6, [(sd * 2.4, 0), (sd * 4.6, -1.6), (sd * 5.6, -4.0)]), (by - 1.0, [(sd * 3.6, 0), (sd * 6.6, -0.4), (sd * 7.6, -2.6)]),
                        (by + 3.4, [(sd * 3.6, 0), (sd * 6.4, 2.4), (sd * 7.0, 5.4)])):
            path(cv, [(bx + dx, y0 + dy) for dx, dy in pts], 'k', 0.45)
        path(cv, [(bx + sd * 1.2, by - 9.4), (bx + sd * 3.0, by - 10.6), (bx + sd * 3.4, by - 12.4)], 'k', 0.4)
    oval(cv, bx, by + 1.6, 4.4, 5.8, 'e')
    oval(cv, bx, by - 5.2, 3.4, 2.0, 'e')
    box(cv, bx - 3.6, by - 3.6, bx + 3.6, by - 3.0, 'b', only='e')
    seg(cv, bx, by - 2.6, bx, by + 7.2, 'k', 0.4)
    for sd in (-1, 1):
        lens(cv, bx + sd * 2.0, by - 1.6, bx + sd * 2.6, by + 3.4, 1.0, 'h')
    oval(cv, bx, by - 8.2, 2.0, 1.4, 'k')
    if kind == 'stag':
        for sd in (-1, 1):
            path(cv, [(bx + sd * 1.0, by - 9.2), (bx + sd * 2.2, by - 11.4), (bx + sd * 1.0, by - 13.6)], 'k', 0.55)
            cv.put(int(bx + sd * 2.0), int(by - 11.0), 'k')
    elif kind == 'rhino':
        path(cv, [(bx, by - 9.0), (bx, by - 11.6), (bx + 1.2, by - 13.0)], 'k', 0.6)
    else:
        for dx in (-1.2, 0.0, 1.2):
            cv.put(int(bx + dx), int(by - 9.6), 'k')
    if r.random() < 0.5:
        mirror(cv)
    return cv, [('b', 'leaf', 'Leaf', GREEN, True), role('v', 'veins', 'Leaf veins', GREEN), role('e', 'beetle', 'Beetle', BLUE),
                role('h', 'shine', 'Shine', BLUE), role('d', 'dew', 'Dew drops', BLUE), role('c', 'center', 'Flower centre', BROWN),
                role('k', 'dark', 'Head, legs and horns', BROWN), role('f', 'flower', 'Flower', PINK)], ['animals', 'insects']


def dragonfly(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    wl = h - ground_rows(h, 0.16)
    hills(cv, 'p', wl, 0.3, 22 * 0.5, r.uniform(0, 6))
    for x in r.sample((1.0, 2.6, 19.4, 21.0, 17.6), 3):
        top = wl - r.uniform(5, 9)
        seg(cv, x, h, x + r.uniform(-0.6, 0.6), top, 'r', 0.45)
        if r.random() < 0.6:
            oval(cv, x, top + 0.6, 0.6, 1.4, 'c')
    for k in range(r.randint(1, 2)):
        x = r.uniform(6, 16)
        oval(cv, x, wl + 2.4, 2.0, 0.8, 'r')
        disc(cv, x + 0.6, wl + 1.8, 0.8, 'o')
    disc(cv, r.choice((3.5, 18.5)), r.uniform(2.4, 3.6), 1.8, 'u')
    th = math.radians(r.uniform(-35, 35))
    d = (math.sin(th), math.cos(th))
    nrm = (math.cos(th), -math.sin(th))
    ox, oy = cx + r.uniform(-1.0, 1.0) - d[0] * 7, r.uniform(5.0, 6.5)
    P = lambda t, u=0.0: (ox + d[0] * t + nrm[0] * u, oy + d[1] * t + nrm[1] * u)
    for sd in (-1, 1):
        a0, a1 = P(2.0), P(1.0, sd * 9.4)
        lens(cv, a0[0], a0[1], a1[0], a1[1], 2.8, 'w')
        b0, b1 = P(3.4), P(5.6, sd * 8.6)
        lens(cv, b0[0], b0[1], b1[0], b1[1], 2.6, 'w')
        for p0, p1 in ((a0, a1), (b0, b1)):
            seg(cv, p0[0], p0[1], p0[0] + (p1[0] - p0[0]) * 0.85, p0[1] + (p1[1] - p0[1]) * 0.85, 'q', 0.3)
    tail = [P(3.6), P(15.5)]
    tube(cv, tail, 0.9, 0.55, lambda u: 'q' if int(u * 12) % 3 == 2 else 'b')
    x, y = P(2.6)
    disc(cv, x, y, 1.6, 'b')
    for sd in (-1, 1):
        x, y = P(0.2, sd * 0.9)
        disc(cv, x, y, 1.1, 'k')
    if r.random() < 0.5:
        mirror(cv)
    return cv, [('s', 'sky', 'Evening sky', PINK, True), role('o', 'lilies', 'Water lilies', PINK), role('w', 'wings', 'Wings', BLUE),
                role('p', 'pond', 'Pond', BLUE), role('b', 'dragonfly', 'Dragonfly', GREEN), role('r', 'reeds', 'Reeds and lily pads', GREEN),
                role('k', 'eyes', 'Eyes', BROWN), role('q', 'veins', 'Wing veins and body rings', BROWN), role('c', 'cattails', 'Cattails', BROWN),
                role('u', 'sun', 'Sun', BROWN)], ['animals', 'pond']


def caterpillar(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.1)
    hills(cv, 'g', gtop, 0.5, 22 * 0.8, r.uniform(0, 6))
    for x in r.sample((2.0, 5.0, 16.0, 19.5), 2):
        seg(cv, x, gtop, x, gtop - 4.0, 'g', 0.4)
        for k in range(5):
            t = math.radians(k * 72)
            disc(cv, x + math.cos(t) * 1.0, gtop - 4.6 + math.sin(t) * 1.0, 0.7, 'f')
        cv.put(int(x), int(gtop - 4.6), 'n')
    pose = r.choice(('wave', 'arch', 'rear'))
    ly = r.uniform(17.0, 18.5)
    lens(cv, 0.8, ly + 1.2, 21.4, ly - 0.8, 6.4, 'l')
    seg(cv, 0.8, ly + 1.2, 20.0, ly - 0.7, 'n', 0.4)
    for k in range(r.randint(1, 3)):
        disc(cv, r.uniform(3, 19), ly + r.uniform(-2.2, 2.2), 0.8, 's')
    n = 8
    pts = []
    for k in range(n):
        x = 2.6 + k * 2.1
        if pose == 'wave':
            y = ly - 2.4 - math.sin(k * 0.9) * 1.3
        elif pose == 'arch':
            y = ly - 2.4 - math.sin(math.pi * k / (n - 1)) * 6.0
        else:
            y = ly - 2.4 - max(0, k - 4.5) ** 1.6 * 1.4
        pts.append((x, y))
    for k, (x, y) in enumerate(pts):
        if pose != 'arch' or k in (0, 1, n - 2, n - 1):
            path(cv, [(x - 0.4, y + 1.2), (x - 0.6, y + 2.2)], 'k', 0.4)
        disc(cv, x, y, 1.7, 'c' if k % 2 == 0 else 'd')
        cv.put(int(x), int(y - 0.4), 'p')
    hx, hy = pts[-1][0] + 1.6, pts[-1][1] - 1.2
    disc(cv, hx, hy, 2.4, 'h')
    for sd in (-1, 1):
        path(cv, [(hx + sd * 1.0, hy - 2.0), (hx + sd * 1.8, hy - 4.2)], 'k', 0.4)
        cv.put(int(hx + sd * 1.8), int(hy - 4.6), 'k')
    eye(cv, hx + 0.6, hy - 1.2, big, 'e', 'k')
    path(cv, [(hx + 0.4, hy + 1.0), (hx + 1.4, hy + 1.2)], 'k', 0.4)
    if r.random() < 0.5:
        bx, byy = r.uniform(4, 9), r.uniform(4, 7)
        for sd in (-1, 1):
            disc(cv, bx + sd * 1.3, byy - 0.4, 1.2, 'f')
            disc(cv, bx + sd * 1.0, byy + 1.1, 0.8, 'f')
        seg(cv, bx, byy - 1.4, bx, byy + 1.6, 'k', 0.35)
    else:
        cloud(cv, r.uniform(4, 9), 3.4, 1.2, 'e')
    disc(cv, r.choice((17.0, 19.5)), r.uniform(2.6, 3.6), 1.8, 'u')
    if r.random() < 0.5:
        mirror(cv)
    return cv, [sky(), role('c', 'caterpillar', 'Caterpillar', GREEN), role('d', 'segments', 'Striped segments', GREEN),
                role('l', 'leaf', 'Leaf', GREEN), role('g', 'grass', 'Grass and stems', GREEN), role('h', 'head', 'Head', PINK),
                role('f', 'flowers', 'Flowers and butterfly', PINK), role('p', 'dots', 'Dots', BROWN), role('k', 'dark', 'Feet, eye and antennae', BROWN),
                role('n', 'vein', 'Leaf vein and flower centres', BROWN), role('u', 'sun', 'Sun', BROWN), role('e', 'eye_white', 'Eye white and cloud', BLUE)],\
        ['animals', 'garden']


def seal(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    sea = r.uniform(17.5, 19.0)
    hills(cv, 'w', sea, 0.35, 22 * 0.45, r.uniform(0, 6))
    if r.random() < 0.5:
        ix = r.choice((3.0, 18.0))
        poly(cv, [(ix - 3.6, sea), (ix - 1.0, sea - 5.0), (ix + 0.6, sea - 3.6), (ix + 2.0, sea - 6.4), (ix + 3.8, sea)], 'i')
    else:
        for k in range(2):
            x, y = r.uniform(3, 18), r.uniform(3, 7)
            path(cv, [(x - 1.2, y - 0.6), (x, y + 0.2), (x + 1.2, y - 0.6)], 'n', 0.45)
    disc(cv, r.choice((3.5, 18.0)), r.uniform(2.4, 3.6), 1.9, 'u')
    rt = sea + r.uniform(1.0, 2.0)
    poly(cv, [(1.0, h + 1), (1.6, rt + 1.2), (5.0, rt), (13.0, rt - 0.4), (18.0, rt + 0.6), (20.6, h + 1)], 'i')
    for x in (3.0, 17.0):
        seg(cv, x, h, x + 0.6, rt + 2.0, 'g', 0.45)
    prop = r.choices(('ball', 'fish', 'nap'), (5, 3, 2))[0]
    if prop == 'nap':
        tube(cv, [(3.0, rt - 1.4), (9.0, rt - 2.4)], 0.9, 2.6, 'k')
        tube(cv, [(9.0, rt - 2.4), (14.4, rt - 2.6)], 2.6, 2.2, 'k')
        hx, hy = 15.6, rt - 2.8
        disc(cv, hx, hy, 2.5, 'k')
        oval(cv, hx + 2.4, hy + 0.6, 1.6, 1.2, 'k')
        lens(cv, 10.6, rt - 1.0, 13.0, rt + 0.4, 1.6, 'n')
        path(cv, [(hx - 0.2, hy - 0.8), (hx + 1.0, hy - 0.6)], 'n', 0.35)
        cv.put(int(hx + 3.6), int(hy + 0.2), 'n')
        for sd in (-1, 1):
            seg(cv, hx + 3.0, hy + 1.0, hx + 4.8, hy + 1.0 + sd * 0.8, 'n', 0.3)
        tail = (2.6, rt - 1.4)
        for k in range(3):
            cv.put(int(hx + 1.0 + k * 1.2), int(hy - 3.8 - k * 1.6), 'e')
    else:
        tube(cv, [(3.0, rt - 1.2), (8.0, rt - 2.8)], 1.0, 3.0, 'k')
        tube(cv, [(8.0, rt - 2.8), (11.6, rt - 5.0), (12.4, rt - 9.4)], 3.0, 2.4, 'k')
        hx, hy = 12.8, rt - 11.6
        disc(cv, hx, hy, 2.6, 'k')
        oval(cv, hx + 2.0, hy - 1.0, 1.7, 1.3, 'k')
        lens(cv, 10.8, rt - 5.2, 14.6, rt - 2.0, 1.9, 'n')
        eye(cv, hx - 0.2, hy - 1.4, big, 'e', 'n')
        cv.put(int(hx + 3.2), int(hy - 1.8), 'n')
        for sd in (-1, 1):
            seg(cv, hx + 2.4, hy - 0.2, hx + 4.4, hy - 0.2 + sd * 0.8, 'n', 0.3)
        tail = (2.6, rt - 1.2)
        if prop == 'ball':
            bx, by = hx + 3.8, hy - 4.8
            disc(cv, bx, by, 2.4, 'p')
            for dx in (-0.9, 0.9):
                stripe(cv, bx + dx, by - 2.6, bx + dx, by + 2.6, 'y', 'p', 0.45)
        else:
            oval(cv, hx + 4.0, hy - 0.2, 1.6, 0.8, 'p')
            poly(cv, [(hx + 5.4, hy - 0.2), (hx + 6.8, hy - 1.2), (hx + 6.8, hy + 0.8)], 'p')
    for sd in (-1, 1):
        lens(cv, tail[0] + 0.6, tail[1], tail[0] - 1.4, tail[1] + sd * 1.4, 1.2, 'n')
    if r.random() < 0.5:
        mirror(cv)
    return cv, [sky(), role('k', 'seal', 'Seal', BROWN), role('n', 'dark', 'Flippers, nose and whiskers', BROWN), role('u', 'sun', 'Sun', BROWN),
                role('y', 'stripes', 'Ball stripes', BROWN), role('e', 'eye_white', 'Eye white and snores', BLUE), role('w', 'sea', 'Sea', BLUE),
                role('p', 'ball', 'Ball and fish', PINK), role('i', 'rock', 'Rocks', GREEN), role('g', 'weed', 'Seaweed', GREEN)], ['animals', 'sea']


def dolphin(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    sea = r.uniform(18.5, 20.0)
    hills(cv, 'w', sea, 0.35, 22 * 0.45, r.uniform(0, 6))
    if r.random() < 0.55:
        ix = r.choice((3.0, 18.5))
        oval(cv, ix, sea + 0.4, 3.4, 1.2, 'n')
        palm(cv, ix, sea, sea - 7.0, 'n', 'l', 0.6 if ix < 11 else -0.6)
    disc(cv, r.choice((4.0, 17.0)), r.uniform(2.4, 3.6), 1.9, 'u')
    for k in range(r.randint(0, 2)):
        x, y = r.uniform(8, 14), r.uniform(1.5, 4)
        path(cv, [(x - 1.2, y - 0.6), (x, y + 0.2), (x + 1.2, y - 0.6)], 'k', 0.45)
    lift = r.uniform(-1.0, 1.0)
    ty = sea + 0.6
    body = curve([(3.2, ty), (4.4, 10.0 + lift), (10.0, 6.4 + lift), (16.4, 10.6 + lift)], 14)
    tube(cv, body[:8], 0.6, 2.0, 'd')
    tube(cv, body[7:], 2.0, 1.6, 'd')
    hx, hy = body[-1]
    lens(cv, hx - 0.6, hy, hx + 3.8, hy + 2.4, 1.6, 'd')
    poly(cv, [(8.0, 7.2 + lift), (8.8, 3.6 + lift), (10.8, 6.4 + lift)], 'd')
    lens(cv, 12.2, 10.4 + lift, 11.0, 13.6 + lift, 1.5, 'd')
    belly = curve([(4.4, ty - 4.0), (6.0, 11.0 + lift), (10.6, 8.6 + lift), (15.6, 12.0 + lift)], 12)
    tube(cv, belly, 0.5, 0.9, 'b')
    for sd in (-1, 1):
        lens(cv, 3.2, ty, 3.2 + sd * 2.2, ty + 1.6, 1.4, 'd')
    cv.put(int(hx - 0.2), int(hy - 0.8), 'k')
    path(cv, [(hx + 0.6, hy + 1.2), (hx + 2.8, hy + 2.0)], 'k', 0.35)
    for k in range(r.randint(3, 5)):
        a = math.radians(r.uniform(200, 340))
        disc(cv, 3.2 + math.cos(a) * r.uniform(2, 3.5), ty - 0.6 + math.sin(a) * 1.4, 0.6, 'e')
    if r.random() < 0.4:
        sx = r.uniform(14, 18)
        tube(cv, curve([(sx - 3.0, sea + 0.6), (sx - 1.4, sea - 3.6), (sx + 2.0, sea - 2.4)], 6), 0.5, 1.1, 'd')
        lens(cv, sx + 1.6, sea - 2.2, sx + 3.4, sea - 1.2, 1.0, 'd')
    if r.random() < 0.5:
        mirror(cv)
    return cv, [sky(), role('d', 'dolphin', 'Dolphin', PINK), role('b', 'belly', 'Belly', PINK), role('w', 'sea', 'Sea', BLUE),
                role('e', 'splash', 'Splash', GREEN), role('k', 'eye', 'Eye, smile and gulls', BROWN), role('u', 'sun', 'Sun', BROWN),
                role('n', 'island', 'Island and palm trunk', BROWN), role('l', 'leaves', 'Palm leaves', GREEN)], ['animals', 'sea']


def jellyfish(w, h, r):
    cv, s, cx, big = start(w, h, 'w')
    gtop = h - ground_rows(h, 0.1)
    for x in r.sample((1.5, 3.5, 18.5, 20.5), 3):
        path(cv, [(x, gtop), (x + 0.8, gtop - 3), (x - 0.4, gtop - 6), (x + 0.5, gtop - 9)], 'g', 0.55)
    hills(cv, 'd', gtop, 0.5, 22 * 0.8, r.uniform(0, 6))
    for x in r.sample((6.0, 9.0, 13.0, 16.0), 1):
        for a in (-60, -90, -120):
            t = math.radians(a)
            seg(cv, x, gtop, x + math.cos(t) * 3.0, gtop + math.sin(t) * 3.0, 'c', 0.5)
    for k in range(r.randint(4, 7)):
        disc(cv, r.uniform(1, 21), r.uniform(1, 18), r.uniform(0.5, 0.9), 'o')
    jx, jy = cx + r.uniform(-2.0, 2.0), r.uniform(7.5, 9.0)
    R = r.uniform(5.2, 6.0)
    for k in range(5):
        x0 = jx - R * 0.7 + k * R * 0.35
        ph = r.uniform(0, 6)
        pts = [(x0 + math.sin(ph + t * 0.9) * 0.8, jy + 0.6 + t) for t in range(0, 11)]
        path(cv, pts, 'q', 0.4)
    for dx in (-1.0, 1.0):
        pts = [(jx + dx + math.sin(t * 0.8 + dx) * 0.9, jy + 1.0 + t * 0.9) for t in range(0, 9)]
        tube(cv, pts, 0.9, 0.5, 'j')
    for y in range(h):
        for x in range(w):
            dx, dy = (x + 0.5 - jx) / R, (y + 0.5 - jy) / (R * 0.9)
            if dx * dx + dy * dy <= 1.0 and dy <= 0.25:
                cv.g[y][x] = 'j'
    for k in range(7):
        disc(cv, jx - R + 0.5 + k * (2 * R - 1.0) / 6, jy + R * 0.2, 0.8, 'j')
    for k in range(3):
        cv.put(int(jx - 2.5 + k * 2.5), int(jy - R * 0.62), 'q')
    eyes(cv, jx, jy - 1.6, 1.8, big, 'e', 'k')
    path(cv, [(jx - 0.8, jy + 0.8), (jx, jy + 1.2), (jx + 0.8, jy + 0.8)], 'k', 0.35)
    for k in range(r.randint(1, 2)):
        sx, sy = r.choice((3.5, 18.5)), r.uniform(12, 18) + k * 2
        for y in range(h):
            for x in range(w):
                dx, dy = (x + 0.5 - sx) / 2.2, (y + 0.5 - sy) / 2.0
                if dx * dx + dy * dy <= 1.0 and dy <= 0.2:
                    cv.g[y][x] = 'j'
        for dx in (-1.0, 0.0, 1.0):
            path(cv, [(sx + dx, sy + 0.4), (sx + dx + 0.4, sy + 2.0), (sx + dx - 0.2, sy + 3.6)], 'q', 0.4)
        cv.put(int(sx), int(sy - 0.6), 'k')
    if r.random() < 0.5:
        mirror(cv)
    return cv, [('w', 'water', 'Water', BLUE, True), role('o', 'bubbles', 'Bubbles', BLUE), role('e', 'eye_white', 'Eye whites', BLUE),
                role('j', 'jelly', 'Jellyfish', PINK), role('q', 'tentacles', 'Tentacles and spots', PINK), role('d', 'sand', 'Sand', BROWN),
                role('k', 'eye', 'Eyes and smile', BROWN), role('g', 'weed', 'Seaweed', GREEN), role('c', 'coral', 'Coral', GREEN)], ['animals', 'sea']


def seahorse(w, h, r):
    cv, s, cx, big = start(w, h, 'w')
    gtop = h - ground_rows(h, 0.1)
    for x in r.sample((1.5, 3.5, 6.0, 18.5, 20.5), 3):
        path(cv, [(x, gtop), (x + 0.8, gtop - 4), (x - 0.4, gtop - 8), (x + 0.5, gtop - r.uniform(11, 15))], 'g', 0.6)
    hills(cv, 'd', gtop, 0.5, 22 * 0.8, r.uniform(0, 6))
    cx0 = r.choice((15.5, 18.0))
    for a in (-50, -80, -110, -135):
        t = math.radians(a)
        path(cv, [(cx0, gtop + 0.5), (cx0 + math.cos(t) * 2.4, gtop + math.sin(t) * 2.4), (cx0 + math.cos(t) * 4.0, gtop + math.sin(t) * 4.6)], 'c', 0.55)
    for k in range(r.randint(4, 6)):
        disc(cv, r.uniform(12, 21), r.uniform(1, 12), r.uniform(0.5, 0.9), 'o')
    ox, oy = r.uniform(-1.0, 0.5), r.uniform(-0.5, 1.0)
    P = lambda x, y: (x + ox, y + oy)
    lens(cv, *P(9.0, 13.0), *P(5.6, 15.8), 2.2, 'f')
    tube(cv, [P(11.6, 8.0), P(10.6, 10.6)], 1.8, 2.2, 'h')
    tube(cv, [P(10.6, 10.6), P(12.4, 13.6), P(11.8, 16.8), P(9.8, 19.4)], 2.6, 1.6, 'h')
    tail = [P(9.8, 19.4), P(8.4, 21.4)] + [P(9.6 + (1.8 - 0.25 * t) * math.cos(math.pi + t), 22.0 + (1.8 - 0.25 * t) * math.sin(math.pi + t)) for t in [k * 0.5 for k in range(11)]]
    tube(cv, tail, 1.3, 0.5, 'h')
    for y in (11.4, 13.4, 15.4, 17.4):
        stripe(cv, *P(10.0, y), *P(15.0, y + 0.4), 'r', 'h', 0.35)
    hx, hy = P(12.6, 6.4)
    disc(cv, hx, hy, 2.3, 'h')
    tube(cv, [(hx + 1.4, hy + 0.6), (hx + 5.0, hy + 1.8)], 0.9, 0.75, 'h')
    poly(cv, [(hx - 1.0, hy - 1.6), (hx - 0.2, hy - 3.8), (hx + 0.6, hy - 2.0), (hx + 1.4, hy - 3.4), (hx + 1.6, hy - 1.2)], 'h')
    lens(cv, hx - 1.6, hy + 0.8, hx - 3.4, hy + 1.8, 1.0, 'f')
    eye(cv, hx + 0.4, hy - 1.0, big, 'e', 'k')
    cv.put(int(hx + 5.2), int(hy + 1.6), 'k')
    if r.random() < 0.45:
        bx, by = r.choice((3.5, 17.5)), r.uniform(9, 14)
        tube(cv, [(bx, by), (bx - 0.4, by + 2.4), (bx + 0.4, by + 4.0)], 0.9, 0.5, 'h')
        disc(cv, bx + 0.2, by - 1.2, 1.1, 'h')
        seg(cv, bx + 0.8, by - 1.0, bx + 2.4, by - 0.6, 'h', 0.4)
        cv.put(int(bx + 0.4), int(by - 1.6), 'k')
    if r.random() < 0.5:
        mirror(cv)
    return cv, [('w', 'water', 'Water', BLUE, True), role('o', 'bubbles', 'Bubbles', BLUE), role('e', 'eye_white', 'Eye white', BLUE),
                role('h', 'seahorse', 'Seahorse', BROWN), role('k', 'eye', 'Eye and mouth', BROWN), role('r', 'ridges', 'Belly ridges', BROWN),
                role('d', 'sand', 'Sand', BROWN), role('f', 'fin', 'Fin and gill', PINK), role('c', 'coral', 'Coral', PINK),
                role('g', 'weed', 'Seaweed', GREEN)], ['animals', 'sea']


def starfish(w, h, r):
    sea_bed = r.random() < 0.45
    cv, s, cx, big = start(w, h, 's')
    if sea_bed:
        gtop = r.uniform(13.0, 15.0)
        for x in r.sample((1.5, 4.0, 18.0, 20.5), 2):
            path(cv, [(x, gtop + 1), (x + 0.8, gtop - 3), (x - 0.4, gtop - 7), (x + 0.5, gtop - 10)], 'l', 0.55)
        hills(cv, 'd', gtop, 0.6, 22 * 0.9, r.uniform(0, 6))
        for k in range(r.randint(4, 6)):
            disc(cv, r.uniform(1, 21), r.uniform(1, gtop - 2), r.uniform(0.5, 0.9), 'w')
    else:
        sea = r.uniform(10.5, 11.5)
        hills(cv, 'w', sea, 0.3, 22 * 0.5, r.uniform(0, 6))
        gtop = sea + r.uniform(2.2, 3.0)
        hills(cv, 'd', gtop, 0.6, 22 * 0.9, r.uniform(0, 6))
        disc(cv, r.choice((3.5, 18.0)), r.uniform(2.2, 3.2), 1.8, 'u')
        if r.random() < 0.5:
            bx = r.choice((2.5, 19.0))
            poly(cv, [(bx - 1.8, gtop + 1.0), (bx + 1.8, gtop + 1.0), (bx + 1.3, gtop + 4.0), (bx - 1.3, gtop + 4.0)], 'l')
            path(cv, [(bx - 1.6, gtop + 1.0), (bx, gtop - 1.0), (bx + 1.6, gtop + 1.0)], 'k', 0.35)
    sx, sy = cx + r.uniform(-1.0, 1.0), min(gtop + r.uniform(5.6, 7.0), 20.8)
    rot = r.uniform(-110, -70)
    ro = r.uniform(6.6, 7.4)
    star(cv, sx, sy, ro, 'a', ri=3.3, rot=rot)
    for k in range(5):
        a = math.radians(rot + k * 72)
        disc(cv, sx + math.cos(a) * (ro - 0.9), sy + math.sin(a) * (ro - 0.9), 1.0, 'a')
        for t in (0.55, 0.8):
            cv.put(int(sx + math.cos(a) * ro * t), int(sy + math.sin(a) * ro * t), 'q')
    disc(cv, sx, sy, 3.0, 'a')
    eyes(cv, sx, sy - 1.6, 1.4, big, 'e', 'k')
    path(cv, [(sx - 1.0, sy + 0.8), (sx, sy + 1.4), (sx + 1.0, sy + 0.8)], 'k', 0.35)
    for k in range(r.randint(1, 3)):
        x, y = r.choice((r.uniform(1.5, 4.0), r.uniform(18.0, 20.5))), r.uniform(gtop + 2, 27)
        poly(cv, [(x - 1.2, y + 0.6), (x, y - 1.2), (x + 1.2, y + 0.6)], 'h')
        cv.put(int(x), int(y), 'k')
    bg = ('s', 'water', 'Water', BLUE, True) if sea_bed else sky()
    return cv, [bg, role('w', 'sea', 'Bubbles' if sea_bed else 'Sea', BLUE), role('e', 'eye_white', 'Eye whites', BLUE),
                role('a', 'starfish', 'Starfish', PINK), role('q', 'bumps', 'Bumps', PINK), role('d', 'sand', 'Sand', BROWN),
                role('k', 'eye', 'Eyes, smile and bucket handle', BROWN), role('u', 'sun', 'Sun', BROWN), role('h', 'shells', 'Shells', GREEN),
                role('l', 'weed', 'Seaweed' if sea_bed else 'Bucket', GREEN)], ['animals', 'beach']


def lobster(w, h, r):
    cv, s, cx, big = start(w, h, 'w')
    gtop = h - ground_rows(h, 0.1)
    for x in r.sample((1.5, 3.5, 18.5, 20.5), 2):
        path(cv, [(x, gtop), (x + 0.8, gtop - 3), (x - 0.4, gtop - 6), (x + 0.5, gtop - 9)], 'g', 0.55)
    hills(cv, 'd', gtop, 0.5, 22 * 0.8, r.uniform(0, 6))
    for x in r.sample((2.0, 6.0, 16.0, 20.0), 2):
        oval(cv, x, gtop + 0.4, 2.0, 1.4, 'r')
    for k in range(r.randint(4, 6)):
        disc(cv, r.uniform(1, 21), r.uniform(1, 12), r.uniform(0.5, 0.9), 'o')
    lx, ly = cx + r.uniform(-1.0, 1.0), r.uniform(-0.5, 0.8)
    wave = r.uniform(-0.8, 0.8)
    for sd in (-1, 1):
        path(cv, [(lx + sd * 0.8, 8.4 + ly), (lx + sd * 3.6, 4.6 + ly), (lx + sd * (7.0 + wave), 1.6 + ly), (lx + sd * 10.0, 2.0 + ly)], 'q', 0.35)
        for k in range(3):
            y0 = 13.0 + k * 1.6 + ly
            path(cv, [(lx + sd * 2.2, y0), (lx + sd * 4.2, y0 + 0.6), (lx + sd * 5.0, y0 + 2.2)], 'q', 0.4)
        path(cv, [(lx + sd * 1.8, 10.6 + ly), (lx + sd * 4.6, 10.4 + ly), (lx + sd * 6.0, 8.0 + ly)], 'l', 0.95)
        cx_, cy_ = lx + sd * 6.6, 5.4 + ly
        oval(cv, cx_, cy_, 2.4, 3.2, 'l')
        seg(cv, cx_ + sd * 0.4, cy_ - 3.2, cx_ + sd * 0.1, cy_ - 0.4, 'w', 0.45)
        seg(cv, lx + sd * 0.8, 8.8 + ly, lx + sd * 1.2, 7.4 + ly, 'q', 0.35)
    oval(cv, lx, 11.6 + ly, 2.6, 3.6, 'l')
    for k, (y, rx) in enumerate(((15.8, 2.4), (17.6, 2.2), (19.4, 2.0), (21.0, 1.8))):
        oval(cv, lx, y + ly, rx, 1.0, 'l')
        box(cv, lx - rx, y + ly - 1.0, lx + rx, y + ly - 0.5, 'q', only='l')
    for a in (-60, -75, -90, -105, -120):
        t = math.radians(a)
        lens(cv, lx, 21.8 + ly, lx - math.cos(t) * 2.6, 21.8 + ly - math.sin(t) * 2.6, 1.4, 'l')
    eyes(cv, lx, 9.6 + ly, 1.3, big, 'e', 'k')
    path(cv, [(lx - 0.8, 12.4 + ly), (lx, 12.8 + ly), (lx + 0.8, 12.4 + ly)], 'k', 0.35)
    return cv, [('w', 'water', 'Water', BLUE, True), role('o', 'bubbles', 'Bubbles', BLUE), role('e', 'eye_white', 'Eye whites', BLUE),
                role('l', 'lobster', 'Lobster', PINK), role('q', 'legs', 'Legs, feelers and shell lines', PINK), role('d', 'sand', 'Sand', BROWN),
                role('k', 'eye', 'Eyes and smile', BROWN), role('r', 'rocks', 'Rocks', GREEN), role('g', 'weed', 'Seaweed', GREEN)], ['animals', 'sea']


DAILY_ANIMALS = [dragon, unicorn, dinosaur, kangaroo, koala, zebra, monkey, tiger, deer, squirrel, raccoon, flamingo, peacock, swan, seal, dolphin, jellyfish, seahorse, starfish, lobster, beetle, dragonfly, caterpillar, camel, hippo, crocodile, toucan, hamster, cow, horse, goat, chameleon]

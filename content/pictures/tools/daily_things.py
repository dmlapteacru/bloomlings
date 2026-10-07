"""Daily Challenge subjects: things (daily_subjects.py). Vehicles, space, music, toys, magic and the desk, drawn on
the 22 x 28 daily board with picture_kit's helpers in the style of world_subjects.py. Each subject returns (canvas,
roles, themes); its shapes that must stay apart (a vehicle against the sky, wheels against the body) take different
color groups, and its roles carry six distinct variants. A subject's three pictures differ in scene, props, time of
day or the way they face, all chosen with the picture's own random stream.
"""
import math

from picture_kit import (BLUE, BROWN, GREEN, PINK, box, cells, cloud, disc, dots, heart, hills, lens, oval, path,
                         poly, rbox, ring, role, scatter, seg, sky, star, start)


# ---- Helpers ----

def mirror(cv):
    """Turns the drawing to face the other way."""
    for row in cv.g:
        row.reverse()


def fill(cv, c, inside, only=None):
    """The cells whose centers pass `inside(px, py)`; `only` limits it to cells of those roles."""
    for x, y, px, py in cells(cv):
        if (only is None or cv.g[y][x] in only) and inside(px, py):
            cv.g[y][x] = c


def turn(cx, cy, deg):
    """A frame turned by `deg` degrees about (cx, cy): `to` maps local (u, v) onto the board, `back` the reverse."""
    a = math.radians(deg)
    ca, sa = math.cos(a), math.sin(a)

    def to(u, v):
        return cx + u * ca - v * sa, cy + u * sa + v * ca

    def back(x, y):
        return (x - cx) * ca + (y - cy) * sa, -(x - cx) * sa + (y - cy) * ca
    return to, back


def bar(cv, x0, y0, x1, y1, t, c):
    """A straight bar `t` cells across from (x0, y0) to (x1, y1), with square ends."""
    dx, dy = x1 - x0, y1 - y0
    L = math.hypot(dx, dy) or 1.0
    nx, ny = -dy / L * t / 2, dx / L * t / 2
    poly(cv, [(x0 + nx, y0 + ny), (x1 + nx, y1 + ny), (x1 - nx, y1 - ny), (x0 - nx, y0 - ny)], c)


def sun_moon(cv, x, y, rad, c, bg, night):
    """A sun, or by night a crescent moon cut from the sky."""
    disc(cv, x, y, rad, c)
    if night:
        disc(cv, x + rad * 0.55, y - rad * 0.35, rad * 0.8, bg)


def skyline(cv, r, c, win, base, lo, hi):
    """Town houses along a street, their tops between rows lo and hi, some gabled, with windows."""
    x = r.uniform(-2.0, 0.0)
    while x < cv.w:
        bw = r.choice((4, 5, 6))
        top = r.uniform(lo, hi)
        box(cv, x, top, x + bw, base, c)
        if r.random() < 0.4:
            poly(cv, [(x - 0.3, top + 0.3), (x + bw / 2, top - 2.4), (x + bw + 0.3, top + 0.3)], c)
        x += bw + r.choice((0, 1))
    dots(cv, win, c, 3, 2, area=(0, lo, cv.w - 1, base - 2))


def pines(cv, xs, base, size, c, trunk=None):
    """Fir trees of three tiers standing on row `base`."""
    for x in xs:
        if trunk:
            box(cv, x - 0.5, base - size * 0.5, x + 0.5, base + 0.5, trunk)
        for k in range(3):
            y = base - size * (0.35 + k * 0.42)
            half = size * (0.62 - k * 0.14)
            poly(cv, [(x - half, y + size * 0.2), (x, y - size * 0.55), (x + half, y + size * 0.2)], c)


GLYPHS = {
    'A': ('.#.', '#.#', '###', '#.#', '#.#'), 'B': ('##.', '#.#', '##.', '#.#', '##.'),
    'C': ('.##', '#..', '#..', '#..', '.##'), 'D': ('##.', '#.#', '#.#', '#.#', '##.'),
    'E': ('###', '#..', '##.', '#..', '###'), 'K': ('#.#', '#.#', '##.', '#.#', '#.#'),
    'N': ('#.#', '###', '###', '###', '#.#'), 'O': ('.#.', '#.#', '#.#', '#.#', '.#.'),
    '1': ('.#.', '##.', '.#.', '.#.', '###'), '2': ('##.', '..#', '.#.', '#..', '###'),
    '3': ('##.', '..#', '.#.', '..#', '##.'),
}


def glyph(cv, ch, x0, y0, c):
    """A letter or digit 3 cells wide and 5 high, its top left cell at (x0, y0)."""
    for j, row in enumerate(GLYPHS[ch]):
        for i, k in enumerate(row):
            if k == '#':
                cv.put(int(x0) + i, int(y0) + j, c)


# ---- Vehicles ----

def tram(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    night, town = r.random() < 0.4, r.random() < 0.62
    rail, wire = h - 3, h * 0.22
    if town:
        x = r.uniform(-3.0, 0.0)
        while x < w:
            bw, top = r.choice((4, 5)), r.uniform(h * 0.38, h * 0.48)
            box(cv, x, top, x + bw, rail, 'h')
            if r.random() < 0.5:
                poly(cv, [(x - 0.3, top + 0.3), (x + bw / 2, top - 2.2), (x + bw + 0.3, top + 0.3)], 'h')
            box(cv, x + 1.2, top + 1, x + bw - 1.2, top + 2, 'i')
            x += bw + r.choice((1, 2, 3))
    else:
        hills(cv, 'h', h * 0.58, 1.0, w * 1.2, r.uniform(0, 6))
        for x in (r.uniform(1, 5), r.uniform(9, 13), r.uniform(17, 21)):
            box(cv, x - 0.5, h * 0.45, x + 0.5, h * 0.58, 'k')
            disc(cv, x, h * 0.43, r.uniform(2.3, 3.0), 'q')
    box(cv, 0, rail + 1, w, h, 'd')
    left = r.random() < 0.5
    sun_moon(cv, w * (0.8 if left else 0.2), h * 0.1, 2.3, 'u', 's', night)
    box(cv, 0, wire, w, wire + 1, 'p')
    box(cv, 0 if left else w - 1, wire, 1 if left else w, rail + 1, 'p')
    for x in (4, 7, w - 7, w - 4):
        disc(cv, x, rail - 0.8, 1.3, 'k')
    rbox(cv, 1, rail - 10, w - 1, rail - 1, 1.5, 'b')
    box(cv, 0, rail - 10, w, rail - 5, 'c', only='b')
    box(cv, 3, rail - 11, w - 3, rail - 10, 'p')
    px = r.choice((7.5, cx, w - 7.5))
    path(cv, [(px, rail - 10.6), (px - 2, wire + 4.4), (px, wire + 1.4), (px + 2, wire + 4.4), (px, rail - 10.6)], 'p', 0.45)
    for x0 in (2, 6, 13, 17):
        box(cv, x0, rail - 9, x0 + 3, rail - 6, 'i')
    box(cv, cx - 1, rail - 9, cx + 1, rail - 1, 'i')
    for x in (1, w - 2):
        box(cv, x, rail - 4, x + 1, rail - 2, 'c')
    box(cv, 1, rail, w - 1, rail + 1, 'k')
    if night:
        scatter(cv, 'x', 's', 6, r, sep=3, area=(0, 0, w - 1, wire - 1))
    return cv, [('s', 'sky', 'Night sky' if night else 'Sky', BLUE, True), role('b', 'body', 'Lower body', PINK),
                role('c', 'upper', 'Upper body and lamps', BROWN), role('p', 'wire', 'Roof, pantograph, wire and pole', PINK),
                role('i', 'window', 'Windows', BLUE), role('k', 'wheel', 'Wheels and rails', BROWN),
                role('d', 'street', 'Street', GREEN), role('h', 'town', 'Houses' if town else 'Hills', GREEN),
                role('q', 'tree', 'Trees', GREEN), role('u', 'sun', 'Moon' if night else 'Sun', BROWN),
                role('x', 'star', 'Stars', BROWN)], ['vehicles', 'town']


def fire_truck(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    night, raised, town = r.random() < 0.2, r.random() < 0.5, r.random() < 0.75
    gtop = h - 4
    if town:
        skyline(cv, r, 'h', 'q', gtop, h * 0.3, h * 0.45)
    else:
        hills(cv, 'h', h * 0.46, 1.4, w * 1.1, r.uniform(0, 6))
        x = r.uniform(7, 14)
        disc(cv, x, h * 0.32, 2.6, 'h')
        box(cv, x - 0.5, h * 0.32, x + 0.5, h * 0.46, 'q')
    box(cv, 0, gtop, w, h, 'd')
    sun_moon(cv, w * (0.15 if raised else 0.5), h * 0.1, 2.2, 'u', 's', night)
    box(cv, 1.5, 13, 15.5, gtop - 2, 'b')
    poly(cv, [(15, gtop - 2), (15, 10.6), (18.2, 10.6), (20.5, 14), (20.5, gtop - 2)], 'b')
    poly(cv, [(16, 11.5), (18.0, 11.5), (19.8, 14.6), (16, 14.6)], 'i')
    box(cv, 1.5, 17, 20.5, 18, 'z', only='b')
    disc(cv, 8.5, 16.5, 2.2, 'i')
    disc(cv, 8.5, 16.5, 1.0, 'z')
    box(cv, 16.5, 9, 18.5, 10.5, 'z')
    box(cv, 19.5, 19, 20.5, gtop - 2, 'z')
    if raised:
        box(cv, 2.5, 11.5, 6.5, 13, 'l')
        x0, y0, x1, y1 = 4.5, 11.5, 17.5, 3.2
        L = math.hypot(x1 - x0, y1 - y0)
        nx, ny = -(y1 - y0) / L, (x1 - x0) / L
        for side in (-1, 1):
            seg(cv, x0 + nx * side, y0 + ny * side, x1 + nx * side, y1 + ny * side, 'l', 0.45)
        for k in range(1, int(L / 1.8) + 1):
            t = k * 1.8 / L
            px, py = x0 + (x1 - x0) * t, y0 + (y1 - y0) * t
            seg(cv, px - nx, py - ny, px + nx, py + ny, 'l', 0.4)
    else:
        box(cv, 1.5, 10, 15.5, 11, 'l')
        box(cv, 1.5, 12, 15.5, 13, 'l')
        for x in range(2, 16, 3):
            box(cv, x, 11, x + 1, 12, 'l')
    for x in (5.0, 17.0):
        disc(cv, x + 0.5, gtop - 1.5, 2.4, 'k')
        disc(cv, x + 0.5, gtop - 1.5, 1.0, 'm')
    if night:
        scatter(cv, 'x', 's', 6, r, sep=3, area=(0, 0, w - 1, h * 0.28))
    if r.random() < 0.5:
        mirror(cv)
    return cv, [('s', 'sky', 'Night sky' if night else 'Sky', BLUE, True), role('b', 'body', 'Fire truck', PINK),
                role('l', 'ladder', 'Ladder', BROWN), role('i', 'window', 'Window and hose reel', BLUE),
                role('z', 'trim', 'Stripe, siren and bumper', BROWN), role('k', 'tyre', 'Tyres', BROWN),
                role('m', 'hub', 'Hubs', GREEN), role('d', 'street', 'Street', GREEN),
                role('h', 'town', 'Houses' if town else 'Hills and tree', GREEN),
                role('q', 'detail', 'Windows' if town else 'Trunk', PINK), role('u', 'sun', 'Moon' if night else 'Sun', BROWN),
                role('x', 'star', 'Stars', BROWN)], ['vehicles', 'town']


def scooter(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    scene = r.randrange(3)
    gtop = h - 4
    if scene == 0:
        for x in (r.uniform(2, 5), r.uniform(15, 20)):
            box(cv, x - 0.6, h * 0.36, x + 0.6, gtop, 't')
            disc(cv, x, h * 0.3, r.uniform(3.0, 3.8), 'q')
    elif scene == 1:
        for x in range(1, w, 3):
            box(cv, x, h * 0.62, x + 1.4, gtop, 't')
            poly(cv, [(x, h * 0.62), (x + 0.7, h * 0.62 - 1.2), (x + 1.4, h * 0.62)], 't')
        box(cv, 0, h * 0.68, w, h * 0.68 + 0.9, 't')
        cloud(cv, w * 0.3, h * 0.14, 2.0, 'q')
    else:
        hills(cv, 'q', h * 0.6, 1.5, w * 0.9, r.uniform(0, 6))
        cloud(cv, w * 0.7, h * 0.12, 2.0, 'q')
    hills(cv, 'g', gtop, 0.5, w * 1.4, r.uniform(0, 6))
    disc(cv, w * (0.84 if scene != 1 else 0.82), h * 0.09, 2.0, 'u')
    for x in (r.uniform(1, 3), r.uniform(9, 12)):
        disc(cv, x, gtop - 0.2, 1.0, 'o')
    for x in (5.5, 17.5):
        disc(cv, x, gtop - 1.5, 2.3, 'k')
        disc(cv, x, gtop - 1.5, 1.0, 'm')
    rbox(cv, 4, gtop - 5, 17, gtop - 3.2, 0.7, 'b')
    path(cv, [(3.0, gtop - 1.6), (3.4, gtop - 3.6), (5.5, gtop - 4.4)], 'b', 0.5)
    bar(cv, 17.5, gtop - 1.5, 15.4, 6.2, 1.5, 'b')
    bar(cv, 12.4, 6.0, 18.6, 6.0, 1.0, 'b')
    for x in (12.5, 18.5):
        disc(cv, x, 6.5, 1.05, 'e')
    if r.random() < 0.5:
        mirror(cv)
    return cv, [sky(), role('b', 'scooter', 'Scooter', PINK), role('e', 'grip', 'Grips', BROWN), role('k', 'wheel', 'Wheels', BROWN),
                role('m', 'hub', 'Hubs', BLUE), role('g', 'grass', 'Grass', GREEN),
                role('q', 'green', ['Trees', 'Cloud', 'Hills and cloud'][scene], GREEN),
                role('t', 'wood', 'Trunks' if scene == 0 else 'Fence', BROWN), role('o', 'flowers', 'Flowers', PINK),
                role('u', 'sun', 'Sun', BROWN)], ['vehicles', 'park']


def motorcycle(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    flip, dusk, scene = r.random() < 0.8, r.random() < 0.5, int(r.random() * 3)
    gtop = h - 5
    if dusk:
        disc(cv, r.uniform(5, 17), gtop - 4.5, 3.6, 'u')
    else:
        disc(cv, w * r.choice((0.16, 0.84)), h * 0.1, 2.2, 'u')
    if scene == 0:
        hills(cv, 'h', gtop - 3.5, 1.2, w * 1.1, r.uniform(0, 6))
        for x in (r.uniform(1, 4), r.uniform(17, 21)):
            disc(cv, x, gtop - 6.5, 2.0, 'v')
    elif scene == 1:
        hills(cv, 'h', gtop - 1.5, 0.6, w * 1.6, r.uniform(0, 6))
        for x in (r.uniform(1.5, 3), r.uniform(18.5, 20.5)):
            box(cv, x - 0.6, gtop - 7, x + 0.6, gtop, 'v')
            box(cv, x - 2.2, gtop - 5.6, x - 1.2, gtop - 3.6, 'v')
            box(cv, x - 2.2, gtop - 4.2, x, gtop - 3.2, 'v')
            box(cv, x + 1.2, gtop - 6.4, x + 2.2, gtop - 4.6, 'v')
            box(cv, x, gtop - 5.0, x + 2.2, gtop - 4.0, 'v')
    else:
        skyline(cv, r, 'h', 'v', gtop, h * 0.42, h * 0.56)
    box(cv, 0, gtop, w, h, 'd')
    for x in range(1, w, 5):
        box(cv, x, gtop + 2, x + 2.5, gtop + 3, 'y')
    wy = gtop - 3.6
    for x in (4.5, 17.5):
        ring(cv, x, wy, 3.6, 2.2, 'k')
        disc(cv, x, wy, 2.2, 's')
        disc(cv, x, wy, 1.05, 'm')
    rbox(cv, 8, wy - 3.8, 12.6, wy + 0.8, 0.8, 'g')
    bar(cv, 9.5, wy + 1.6, 1.0, wy + 0.2, 1.1, 'g')
    bar(cv, 4.5, wy, 9, wy - 1.5, 0.9, 'f')
    bar(cv, 17.5, wy, 15.4, wy - 9.8, 1.1, 'f')
    bar(cv, 15.4, wy - 9.8, 12.8, wy - 10.8, 0.9, 'f')
    disc(cv, 12.5, wy - 10.5, 1.05, 'e')
    oval(cv, 11.8, wy - 6.2, 3.3, 1.8, 'b')
    poly(cv, [(13, wy - 7.9), (15.6, wy - 7.0), (15.6, wy - 4.8), (12, wy - 4.5)], 'b')
    poly(cv, [(1.6, wy - 7.4), (4.6, wy - 7.4), (4.6, wy - 5.4), (2.4, wy - 4.8)], 'b')
    rbox(cv, 4.2, wy - 7.2, 9.6, wy - 5.4, 0.8, 'e')
    fill(cv, 'b', lambda px, py: 3.7 < math.hypot(px - 17.5, py - wy) <= 4.6 and py < wy - 0.8 and px > 14.8)
    disc(cv, 17.6, wy - 7.4, 1.3, 'y')
    if flip:
        mirror(cv)
    return cv, [('s', 'sky', 'Evening sky' if dusk else 'Sky', BLUE, True), role('b', 'body', 'Tank, tail and fender', PINK),
                role('e', 'seat', 'Seat and grip', PINK), role('f', 'frame', 'Frame and fork', BROWN),
                role('g', 'engine', 'Engine and exhaust', BLUE), role('k', 'tyre', 'Tyres', BROWN), role('m', 'hub', 'Hubs', GREEN),
                role('y', 'light', 'Headlight and road lines', BROWN), role('d', 'road', 'Road', GREEN),
                role('h', 'scenery', ['Hills', 'Dunes', 'Town'][scene], GREEN),
                role('v', 'detail', ['Trees', 'Cacti', 'Windows'][scene], PINK if scene == 2 else GREEN),
                role('u', 'sun', 'Sun', BROWN)], ['vehicles', 'travel']


def skateboard(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    air, path_ = r.random() < 0.3, r.random() > 0.5
    gtop = h - 5
    if not path_:
        rx0, rtop = r.uniform(11.5, 13), gtop - r.uniform(6, 7.5)
        poly(cv, [(rx0, gtop), (w - 1.5, rtop), (w - 1.5, gtop)], 'r')
        box(cv, w - 1.5, rtop - 0.2, w, gtop, 'r')
        box(cv, w - 2.2, rtop - 1.0, w, rtop, 'c')
        for x in (w - 6.5, w - 3.5):
            box(cv, x, gtop - 2.5, x + 1, gtop, 'c', only='r')
        cloud(cv, w * r.uniform(0.2, 0.4), h * 0.13, 2.0, 'o')
    else:
        for x in (r.uniform(1.0, 3.0), r.uniform(8.0, 11.0)):
            disc(cv, x, gtop - 1.0, 2.4, 'o')
            disc(cv, x + 2.2, gtop - 0.6, 1.8, 'o')
        for x in (r.uniform(13.5, 15.5), r.uniform(18.5, 20.0)):
            poly(cv, [(x - 1.7, gtop + 0.6), (x - 0.4, gtop - 4.6), (x + 0.4, gtop - 4.6), (x + 1.7, gtop + 0.6)], 'r')
            box(cv, x - 1.3, gtop - 2.8, x + 1.3, gtop - 2.0, 'c', only='r')
    box(cv, 0, gtop, w, h, 'g')
    for x0 in range(1, w, 6):
        box(cv, x0, gtop + 2, x0 + 3, gtop + 3, 'q')
    disc(cv, w * 0.84, h * 0.08, 2.0, 'u')
    if air:
        bx, by, deg = 9.0, gtop - 12.0, r.uniform(-16, -8)
    else:
        bx, by, deg = 8.5 if not path_ else 6.5, gtop - 3.8, 0
    to, back = turn(bx, by, deg)

    def deck(px, py):
        u, v = back(px, py)
        k = max(0.0, abs(u) - 7.2)
        return k <= 2.2 and -1.0 - k * 0.9 <= v <= 1.0 - k * 0.9

    fill(cv, 'b', deck)
    fill(cv, 'k', lambda px, py: deck(px, py) and back(px, py)[1] < -max(0.0, abs(back(px, py)[0]) - 7.2) * 0.9)
    for side in (-1, 1):
        u = side * 5.0
        poly(cv, [to(u - 1.2, 1.0), to(u + 1.2, 1.0), to(u + 0.6, 2.2), to(u - 0.6, 2.2)], 't')
        disc(cv, *to(u, 3.0), 1.5, 'w')
    if air:
        for k in range(3):
            seg(cv, bx - 9.5 - k * 0.3, by + 2 + k * 1.6, bx - 7.0 - k * 0.3, by + 2.2 + k * 1.6, 'q', 0.4)
    if r.random() < 0.5:
        mirror(cv)
    return cv, [sky(), role('b', 'deck', 'Deck', PINK), role('k', 'grip', 'Grip tape', BROWN), role('t', 'truck', 'Trucks', BLUE),
                role('w', 'wheel', 'Wheels', GREEN), role('r', 'ramp', 'Cones' if path_ else 'Ramp', PINK),
                role('c', 'coping', 'Cone stripes' if path_ else 'Ramp edge and posts', BLUE),
                role('g', 'ground', 'Path' if path_ else 'Ground', BROWN), role('q', 'lines', 'Ground and motion lines', GREEN),
                role('o', 'cloud', 'Bushes' if path_ else 'Cloud', GREEN), role('u', 'sun', 'Sun', BROWN)], ['toys', 'park']


def sled(w, h, r):
    cv, s, cx, big = start(w, h, 'n')
    night, slope, cabin = r.random() < 0.4, r.uniform(0.08, 0.16), r.random() < 0.5
    top = h * 0.7

    def surface(x):
        return top + slope * (x - cx)
    fill(cv, 'w', lambda px, py: py >= surface(px))
    if cabin:
        hx = w - 2.6
        box(cv, hx - 2.2, surface(hx) - 4.6, hx + 2.2, surface(hx) + 0.5, 'k')
        poly(cv, [(hx - 3.2, surface(hx) - 4.2), (hx, surface(hx) - 7.4), (hx + 3.2, surface(hx) - 4.2)], 'v')
        box(cv, hx - 0.9, surface(hx) - 3.4, hx + 0.9, surface(hx) - 1.6, 't')
        pines(cv, [w - hx], surface(w - hx) - 0.2, 4.0, 'p', 'k')
    else:
        for x in r.sample([1.5, 4.0, 18.0, 20.5], 2 + int(r.random() * 2)):
            pines(cv, [x], surface(x) - 0.2, r.uniform(3.6, 4.4), 'p', 'k')
    sun_moon(cv, w * r.choice((0.2, 0.8)), h * 0.11, 2.4, 'u', 'n', night)
    scatter(cv, 'x', 'n', 10, r, sep=3, area=(0, 0, w - 1, top - 8))
    sx = cx - (2.8 if cabin else r.uniform(0.0, 1.0))
    deg = math.degrees(math.atan(slope))
    turned, back = turn(sx, surface(sx) - 0.2, deg)

    def to(u, v):
        return turned(u * 0.85, v)
    for u in (-6.5, -2.0, 2.5):
        bar(cv, *to(u, -3.2), *to(u, -0.6), 1.1, 'k')
    bar(cv, *to(-8.5, -0.6), *to(6.4, -0.6), 1.2, 'k')
    path(cv, [to(6.4 + 2.4 * math.cos(math.radians(a)), -3.0 + 2.4 * math.sin(math.radians(a))) for a in (90, 50, 10, -30, -70, -110)],
         'k', 0.6)
    poly(cv, [to(-8.8, -5.2), to(5.6, -5.2), to(5.6, -3.0), to(-8.8, -3.0)], 'b')
    path(cv, [to(7.4, -5.0), to(9.0, -3.0)] + ([] if cabin else [to(11.8, -2.4)]), 't', 0.45)
    for k in (0.4, 1.7):
        seg(cv, *to(-9.2, k), *to(-17.0, k), 'v', 0.35)
    if r.random() < 0.5:
        mirror(cv)
    return cv, [('n', 'sky', 'Night sky' if night else 'Winter sky', PINK, True), role('b', 'seat', 'Sled seat', BROWN),
                role('k', 'runner', 'Runners, posts, trunks and cabin', BROWN), role('t', 'rope', 'Rope and window', PINK),
                role('w', 'snow', 'Snow', BLUE), role('v', 'tracks', 'Tracks and snowy roof', BLUE), role('p', 'pine', 'Pines', GREEN),
                role('x', 'snowflake', 'Stars' if night else 'Snowflakes', BROWN if night else BLUE),
                role('u', 'sun', 'Moon' if night else 'Sun', BROWN)], ['vehicles', 'winter']


def zeppelin(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    dusk, ground = r.random() < 0.55, int(r.random() * 3)
    ey = r.uniform(8.0, 11.0)
    gtop = h - 5.5
    if ground == 0:
        hills(cv, 'h', gtop, 1.0, w * 1.1, r.uniform(0, 6))
        hills(cv, 'v', gtop + 3, 0.8, w * 0.7, r.uniform(0, 6), only='h')
    elif ground == 1:
        hills(cv, 'h', gtop + 0.5, 1.4, w * 1.6, r.uniform(0, 6))
        for x in range(r.randrange(3), w, 5):
            box(cv, x, 0, x + 2, h, 'v', only='h')
    else:
        skyline(cv, r, 'h', 'i', h, gtop - 1, gtop + 1.5)
    for x in (r.uniform(2, 6), r.uniform(14, 19)):
        cloud(cv, x, r.uniform(ey + 7.5, ey + 9.0), 1.6, 'c')
    sun_moon(cv, w * (0.84 if dusk else 0.16), gtop - 1 if dusk else h * 0.09, 2.8 if dusk else 2.2, 'u', 's', False)
    if not dusk:
        cloud(cv, w * 0.72, h * 0.07, 1.4, 'c')
    for sgn in (-1, 1):
        poly(cv, [(2.6, ey + sgn * 1.0), (6.6, ey + sgn * 2.4), (3.2, ey + sgn * 4.6), (1.8, ey + sgn * 4.6)], 'f')
    fill(cv, 'e', lambda px, py: ((px - cx - 0.8) / 9.0) ** 2 + ((py - ey) / (3.9 - (0.13 * (cx + 0.8 - px) if px < cx + 0.8 else 0))) ** 2 <= 1)
    fill(cv, 'f', lambda px, py: px > cx + 8.4, only='e')
    for x in (cx - 2.5, cx + 4.5):
        box(cv, x - 0.5, 0, x + 0.5, h, 'f', only='e')
    rbox(cv, cx - 1.6, ey + 3.6, cx + 3.8, ey + 5.8, 0.7, 'g')
    for x in (cx - 0.5, cx + 1.5, cx + 3.0):
        cv.put(int(x), int(ey + 4.7), 'i')
    if r.random() < 0.5:
        mirror(cv)
    return cv, [('s', 'sky', 'Evening sky' if dusk else 'Sky', BLUE, True), role('e', 'envelope', 'Airship', BROWN),
                role('f', 'fin', 'Fins, nose and bands', PINK), role('g', 'gondola', 'Gondola', PINK), role('i', 'window', 'Windows', BLUE),
                role('c', 'cloud', 'Clouds', GREEN), role('h', 'land', ['Hills', 'Fields', 'Town'][ground], GREEN),
                role('v', 'fields', 'Fields', GREEN), role('u', 'sun', 'Sun', BROWN)], ['vehicles', 'sky']


def ufo(w, h, r):
    cv, s, cx, big = start(w, h, 'n')
    beam, scene = r.random() < 0.78, int(r.random() * 3)
    ux, uy, deg = cx + r.uniform(-2.0, 2.0), r.uniform(7.5, 9.5), r.uniform(-12, 12)
    gtop = h - 5
    hills(cv, 'g', gtop, 0.8, w * 1.2, r.uniform(0, 6))
    if scene == 0:
        pines(cv, [r.uniform(1, 3), r.uniform(18, 21)], gtop, 3.4, 'q')
    elif scene == 1:
        for x in (r.uniform(1, 3), r.uniform(16, 18)):
            box(cv, x, gtop - 3, x + 3.4, gtop + 1, 'q')
            poly(cv, [(x - 0.6, gtop - 2.8), (x + 1.7, gtop - 5.2), (x + 4.0, gtop - 2.8)], 'q')
            box(cv, x + 1, gtop - 2, x + 2.4, gtop - 1, 'k')
    else:
        for x in (r.uniform(2, 4), r.uniform(17, 20)):
            box(cv, x - 0.6, gtop - 5.5, x + 0.6, gtop + 1, 'q')
            box(cv, x - 2.2, gtop - 4, x - 1.2, gtop - 2, 'q')
            box(cv, x - 2.2, gtop - 2.6, x, gtop - 1.6, 'q')
            box(cv, x + 1.2, gtop - 4.8, x + 2.2, gtop - 2.8, 'q')
            box(cv, x, gtop - 3.4, x + 2.2, gtop - 2.4, 'q')
    sun_moon(cv, w * (0.84 if ux < cx else 0.16), h * 0.09, 2.6, 'm', 'n', True)
    scatter(cv, 'x', 'n', 9, r, sep=3, area=(0, 0, w - 1, gtop - 4))
    to, back = turn(ux, uy, deg)

    def local(f):
        return lambda px, py: f(*back(px, py))
    if beam:
        poly(cv, [to(-3.0, 1.8), to(3.0, 1.8), (ux + 7.2, gtop + 1), (ux - 7.2, gtop + 1)], 'b')
        kx, ky = ux + r.uniform(-1, 1), gtop - 5.5
        rbox(cv, kx - 2.6, ky - 1.2, kx + 1.6, ky + 1.2, 0.8, 'c')
        rbox(cv, kx + 1.2, ky - 2.4, kx + 3.2, ky - 0.2, 0.6, 'c')
        for x in (kx - 2.0, kx + 0.6):
            box(cv, x, ky + 1.0, x + 0.9, ky + 2.6, 'c')
        box(cv, kx - 1.4, ky - 0.8, kx + 0.2, ky + 0.4, 'k')
    fill(cv, 'd', local(lambda u, v: v < -0.6 and (u / 4.4) ** 2 + ((v + 0.6) / 4.0) ** 2 <= 1))
    fill(cv, 'k', local(lambda u, v: v > 0 and (u / 4.6) ** 2 + ((v - 1.4) / 1.3) ** 2 <= 1))
    fill(cv, 'u', local(lambda u, v: (u / 9.6) ** 2 + (v / 2.6) ** 2 <= 1))
    fill(cv, 'k', local(lambda u, v: abs(v - 0.3) <= 0.55), only='u')
    for u in (-6.4, -3.2, 0.0, 3.2, 6.4):
        x, y = to(u, 0.3)
        cv.put(int(x), int(y), 'l')
    return cv, [('n', 'night', 'Night sky', PINK, True), role('u', 'saucer', 'Saucer', BLUE), role('b', 'beam', 'Beam', BLUE),
                role('k', 'rim', 'Rim, belly, cow spots and windows', BROWN), role('c', 'cow', 'Cow', BROWN),
                role('l', 'lights', 'Lights', PINK), role('d', 'dome', 'Dome', GREEN), role('g', 'hills', 'Hills', GREEN),
                role('m', 'moon', 'Moon', GREEN), role('x', 'stars', 'Stars', BROWN),
                role('q', 'scenery', ['Pines', 'Barns', 'Cacti'][scene], BLUE)], ['vehicles', 'space']


def note(cv, x, y, c):
    """An eighth note: its head at (x, y), its stem and flag above it."""
    disc(cv, x, y, 0.9, c)
    seg(cv, x + 0.6, y, x + 0.6, y - 2.6, c, 0.35)
    seg(cv, x + 0.6, y - 2.6, x + 1.6, y - 1.7, c, 0.35)


# ---- Space, pictures and sound ----


def satellite(w, h, r):
    cv, s, cx, big = start(w, h, 'n')
    ex, ey, er = r.choice(((cx, h + 9.0, 14.0), (-2.0, h + 1.0, 10.5), (w + 2.0, h + 1.0, 10.5)))
    disc(cv, ex, ey, er, 'o')
    for _ in range(7):
        lx, ly = ex + r.uniform(-er, er), ey - er * r.uniform(0.2, 0.95)
        rx, ry = r.uniform(1.8, 3.8), r.uniform(1.1, 2.0)
        fill(cv, 'l', lambda px, py: ((px - lx) / rx) ** 2 + ((py - ly) / ry) ** 2 <= 1, only='o')
    ring(cv, ex, ey, er + 1.0, er, 'a')
    scatter(cv, 'x', 'n', 12, r, sep=3)
    if r.random() < 0.5:
        sun_moon(cv, w * r.choice((0.14, 0.86)), h * 0.08, 2.4, 'l', 'n', True)
    sx, sy, deg = cx + r.uniform(-1.0, 1.0), r.uniform(10.0, 12.5), r.uniform(-14, 14)
    to, back = turn(sx, sy, deg)

    def local(f):
        return lambda px, py: f(*back(px, py))
    fill(cv, 'p', local(lambda u, v: 3.2 <= abs(u) <= 10.4 and abs(v) <= 2.1))
    fill(cv, 'k', local(lambda u, v: abs(abs(u) - 5.6) <= 0.32 or abs(abs(u) - 8.0) <= 0.32), only='p')
    fill(cv, 'k', local(lambda u, v: 1.8 <= abs(u) <= 3.3 and abs(v) <= 0.45))
    fill(cv, 'b', local(lambda u, v: abs(u) <= 2.0 and abs(v) <= 2.8))
    fill(cv, 'k', local(lambda u, v: abs(v - 0.9) <= 0.4), only='b')
    fill(cv, 'd', local(lambda u, v: v >= -6.2 and (u / 3.0) ** 2 + ((v + 6.2) / 2.0) ** 2 <= 1))
    fill(cv, 'd', local(lambda u, v: abs(u) <= 0.45 and -4.4 <= v <= -2.8))
    fill(cv, 'd', local(lambda u, v: abs(u) <= 0.4 and -7.8 <= v <= -6.2))
    return cv, [('n', 'space', 'Space', PINK, True), role('b', 'body', 'Body', BROWN), role('p', 'panel', 'Solar panels', BLUE),
                role('k', 'line', 'Panel lines, struts and band', PINK), role('d', 'dish', 'Dish', GREEN),
                role('o', 'ocean', 'Ocean', BLUE), role('l', 'land', 'Land and moon', GREEN), role('a', 'air', 'Air glow', BLUE),
                role('x', 'stars', 'Stars', BROWN)], ['space', 'things']


def telescope(w, h, r):
    cv, s, cx, big = start(w, h, 'n')
    scene = int(r.random() * 3)
    gtop = h - 4
    hills(cv, 'g', gtop - 1, 1.2, w * 1.3, r.uniform(0, 6))
    if scene == 0:
        pines(cv, [r.choice((1.8, 20.2))], gtop - 0.5, 3.2, 'q')
    elif scene == 1:
        hx = r.choice((2.5, 19.5))
        box(cv, hx - 2.2, gtop - 3.5, hx + 2.2, gtop + 0.5, 'q')
        poly(cv, [(hx - 2.9, gtop - 3.3), (hx, gtop - 6.0), (hx + 2.9, gtop - 3.3)], 'q')
        box(cv, hx - 0.6, gtop - 2.6, hx + 0.6, gtop - 1.2, 'y')
    else:
        x = r.uniform(-1.0, 0.5)
        while x < w:
            bw = r.choice((3, 4))
            box(cv, x, gtop - r.uniform(2.5, 5.0), x + bw, gtop + 0.5, 'q')
            x += bw + 0.2
    sun_moon(cv, w * 0.8, h * 0.11, 3.0, 'y', 'n', True)
    for x, y in ((0.12, 0.07), (0.5, 0.05), (0.62, 0.3)):
        sparkle(cv, w * x + r.uniform(-1, 1), h * y + r.uniform(-0.5, 0.5) + 1, 'y')
    if r.random() < 0.6:
        px, py = w * r.uniform(0.2, 0.34), h * r.uniform(0.3, 0.4)
        ring(cv, px, py, 3.4, 2.3, 'm', ry_scale=0.35)
        disc(cv, px, py, 1.7, 'm')
    scatter(cv, 'x', 'n', 8, r, sep=3, area=(0, 0, w - 1, h * 0.55))
    tx, ty = cx - 1.0, 15.0
    for ex in (tx - 5.0, tx + 0.6, tx + 4.6):
        bar(cv, tx, ty, ex, gtop - 0.2, 0.9, 'l')
    to, back = turn(tx + 1.0, ty - 2.0, -36)

    def local(f):
        return lambda px, py: f(*back(px, py))
    fill(cv, 't', local(lambda u, v: -7.0 <= u <= 7.2 and abs(v) <= 1.15 + (u + 7.0) * 0.06))
    fill(cv, 'k', local(lambda u, v: (5.8 <= u <= 7.4 and abs(v) <= 2.2) or (-1.0 <= u <= 0.0 and abs(v) <= 1.8)))
    fill(cv, 'k', local(lambda u, v: -8.6 <= u <= -6.8 and abs(v) <= 0.6))
    disc(cv, tx, ty, 1.1, 'k')
    if r.random() < 0.5:
        mirror(cv)
    return cv, [('n', 'night', 'Night sky', BLUE, True), role('t', 'tube', 'Tube', BROWN), role('k', 'rim', 'Rims, eyepiece and mount', PINK),
                role('l', 'tripod', 'Tripod', PINK), role('y', 'moon', 'Moon, stars and window', BROWN), role('x', 'specks', 'Tiny stars', BROWN),
                role('m', 'planet', 'Ringed planet', GREEN), role('g', 'ground', 'Hill', GREEN),
                role('q', 'detail', ['Pine', 'House', 'Town'][scene], BLUE)], ['space', 'night']


def camera(w, h, r):
    cv, s, cx, big = start(w, h, 'w')
    instant, decor = r.random() < 0.45, int(r.random() * 3)
    ty = h - 5
    if decor == 0:
        path(cv, [(0, 2.0), (cx, 4.0), (w, 2.0)], 'k', 0.4)
        for x in (4.0, cx, w - 4.0):
            dy = 1.0 if x == cx else 0.2
            rbox(cv, x - 2.0, 3.0 + dy, x + 2.0, 7.4 + dy, 0.3, 'o')
            box(cv, x - 1.2, 3.8 + dy, x + 1.2, 6.0 + dy, 'g', only='o')
    elif decor == 1:
        box(cv, 2, 1, 9, 6.5, 'o')
        box(cv, 3, 2, 8, 5.5, 'g')
        box(cv, 5, 2, 6, 5.5, 'o')
        disc(cv, w - 4, 3.6, 2.4, 'o')
        disc(cv, w - 4, 3.6, 1.2, 'g')
    else:
        for x in (3, 9, 15):
            rbox(cv, x, 1.5, x + 4, 6.5, 0.4, 'o')
    box(cv, 0, ty, w, h, 't')
    if instant:
        rbox(cv, 3, 9, 19, ty, 1.8, 'b')
        box(cv, 3, 9, 19, 13, 'c', only='b')
        rbox(cv, 13, 10, 17.5, 12.4, 0.5, 'g')
        lx, ly = 8.5, 17.2
        rbox(cv, 6, ty - 1.6, 14, ty - 0.6, 0.3, 'k')
    else:
        rbox(cv, 2.5, 11, 19.5, ty, 1.5, 'b')
        box(cv, 0, 11, w, 13.5, 'c', only='b')
        rbox(cv, 8, 8.2, 14, 11.5, 0.8, 'c')
        box(cv, 9.5, 9.2, 12.5, 10.5, 'g')
        rbox(cv, 3.5, 9.0, 7.0, 11.5, 0.4, 'k')
        box(cv, 4, 9.6, 6.5, 11, 'g')
        lx, ly = cx, 17.6
    box(cv, w - 6, 7.4 if instant else 9.4, w - 4, 9 if instant else 11, 'k')
    disc(cv, lx, ly, 4.8, 'l')
    disc(cv, lx, ly, 3.5, 'g')
    disc(cv, lx, ly, 1.6, 'p')
    box(cv, lx - 2.4, ly - 2.4, lx - 1.2, ly - 1.4, 'c', only='g')
    if r.random() < 0.5:
        mirror(cv)
    return cv, [('w', 'wall', 'Wall', GREEN, True), role('b', 'body', 'Camera', PINK), role('c', 'top', 'Top plate and glint', BLUE),
                role('l', 'ring', 'Lens ring', BROWN), role('g', 'glass', 'Glass and flash', BLUE), role('p', 'pupil', 'Lens center', PINK),
                role('k', 'button', 'Buttons, slot and string', BROWN), role('o', 'photo', 'Photos and frames', PINK),
                role('t', 'table', 'Table', BROWN)], ['things', 'hobby']


def radio(w, h, r):
    cv, s, cx, big = start(w, h, 'w')
    flip, many, style = r.random() < 0.5, r.random() < 0.5, int(r.random() * 3)
    ty = h - 5
    box(cv, 0, ty, w, h, 't')
    if style == 0:
        bx0, bx1 = 4, w - 4
        rbox(cv, bx0, 12, bx1, ty, 1.0, 'b')
        disc(cv, cx, 12.5, (bx1 - bx0) / 2, 'b')
        disc(cv, cx, 12.8, (bx1 - bx0) / 2 - 2.0, 'g')
        box(cv, bx0 + 2, 12.8, bx1 - 2, 16.5, 'g')
        for x in range(int(bx0 + 3), int(bx1 - 2), 2):
            box(cv, x, 0, x + 1, 16.5, 'b', only='g')
        rbox(cv, cx - 3.5, 18, cx + 3.5, 20.5, 0.6, 'd')
        seg(cv, cx - 0.6, 18.4, cx + 0.6, 20.2, 'k', 0.4)
        for x in (bx0 + 1.8, bx1 - 1.8):
            disc(cv, x, 20.5, 1.05, 'k')
    elif style == 1:
        rbox(cv, 2, 11, w - 2, ty, 1.8, 'b')
        path(cv, [(6, 11), (6.5, 8.2), (w - 6.5, 8.2), (w - 6, 11)], 'k', 0.5)
        bar(cv, w - 4, 11, w - 1.6, 2.4, 0.6, 'k')
        disc(cv, w - 1.6, 2.4, 0.9, 'k')
        rbox(cv, 3.6, 12.6, 11, ty - 1.6, 1.0, 'g')
        for y in range(14, int(ty - 2), 2):
            box(cv, 4.6, y, 10, y + 1, 'b', only='g')
        rbox(cv, 12.6, 12.6, w - 3.6, 16.6, 0.6, 'd')
        seg(cv, 15.5, 13, 15.5, 16.2, 'k', 0.4)
        for x in (14, w - 5):
            disc(cv, x + 0.5, 19.5, 1.05, 'k')
    else:
        rbox(cv, 3, 9, w - 3, ty, 1.4, 'b')
        bar(cv, 5, 9, 2.4, 1.6, 0.6, 'k')
        disc(cv, 2.4, 1.6, 0.9, 'k')
        rbox(cv, 5, 10.4, w - 5, 12.8, 0.4, 'd')
        for x in range(6, w - 5, 2):
            box(cv, x, 10.4, x + 1, 11.4, 'k', only='d')
        disc(cv, cx, 17.6, 4.4, 'g')
        ring(cv, cx, 17.6, 3.0, 2.0, 'b')
    for k in range(3 if many else 2):
        if style == 0:
            note(cv, r.choice((1.5, 3.0, w - 5.0, w - 3.5)), r.uniform(4.0, 9.5), 'm')
        else:
            note(cv, r.uniform(6.0, 14.0) if style == 2 else r.uniform(1.0, 12.0), r.uniform(3.5, 6.0), 'm')
    if flip:
        mirror(cv)
    return cv, [('w', 'wall', 'Wall', BLUE, True), role('b', 'body', 'Radio', PINK), role('g', 'grille', 'Speaker', BROWN),
                role('d', 'dial', 'Dial', BLUE), role('k', 'knob', 'Knobs, handle and aerial', BROWN), role('m', 'notes', 'Music notes', GREEN),
                role('t', 'table', 'Table', GREEN)], ['things', 'music']


def piano(w, h, r):
    cv, s, cx, big = start(w, h, 'w')
    fy = h - 4
    box(cv, 0, fy, w, h, 'f')
    for x in range(1, w, 4):
        box(cv, x, fy + 1.5, x + 2, fy + 2.5, 'v')
    x0, x1, top = 3, w - 3, 9
    box(cv, x0, top, x1, fy, 'b')
    box(cv, x0 - 1, top - 1, x1 + 1, top, 'b')
    rbox(cv, cx - 3.6, top + 1.0, cx + 3.6, top + 4.6, 0.3, 'm')
    for k, y in enumerate((top + 2.0, top + 3.6)):
        for x in range(int(cx - 3), int(cx + 3), 2):
            cv.put(x + k, int(y), 'n')
    kt = top + 5.5
    box(cv, x0 - 1, kt, x1 + 1, kt + 3, 'e')
    for x in range(x0 - 1, x1 + 1):
        if (x - x0 + 1) % 12 in (1, 3, 6, 8, 10):
            box(cv, x, kt, x + 1, kt + 2, 'k')
    box(cv, x0 - 1, kt + 3, x1 + 1, kt + 4, 'b')
    box(cv, x0 + 2, kt + 5.0, x1 - 2, fy - 1.2, 'k')
    box(cv, x0 + 3, kt + 6.0, x1 - 3, fy - 2.2, 'b')
    for x in (cx - 2, cx + 1):
        box(cv, x, fy - 1, x + 1, fy, 'k')
    ox = r.choice((x0 + 2.5, x1 - 2.5))
    prop = int(r.random() * 3)
    if prop == 0:
        poly(cv, [(ox - 1.4, top - 1), (ox + 1.4, top - 1), (ox + 0.9, top - 3.8), (ox - 0.9, top - 3.8)], 'v')
        for dx, dy in ((-1.3, -4.8), (1.1, -5.2), (-0.1, -6.4)):
            disc(cv, ox + dx, top + dy, 1.1, 'o')
    elif prop == 1:
        poly(cv, [(ox - 2.0, top - 1), (ox + 2.0, top - 1), (ox + 0.6, top - 6.5), (ox - 0.6, top - 6.5)], 'v')
        seg(cv, ox, top - 1.8, ox + 1.4, top - 6.0, 'm', 0.4)
    else:
        for dx in (-2.0, 0.0, 2.0):
            box(cv, ox + dx - 0.5, top - 3.6 + abs(dx) * 0.4, ox + dx + 0.5, top - 1, 'm')
            box(cv, ox + dx - 0.5, top - 5.6 + abs(dx) * 0.4, ox + dx + 0.5, top - 3.6 + abs(dx) * 0.4, 'o')
        box(cv, ox - 2.6, top - 1.8, ox + 2.6, top - 1, 'v')
    fx = x1 - 4.5 if ox < cx else x0 + 1
    rbox(cv, fx, 1.0, fx + 3.6, 5.0, 0.3, 'v')
    box(cv, fx + 0.8, 1.8, fx + 2.8, 4.2, 'm')
    if r.random() < 0.6:
        sx = cx + r.uniform(-1, 1)
        rbox(cv, sx - 4, fy - 4.5, sx + 4, fy - 3, 0.5, 'o')
        for x in (sx - 3.4, sx + 2.6):
            box(cv, x, fy - 3, x + 1, fy, 'o')
    return cv, [('w', 'wall', 'Wall', PINK, True), role('b', 'piano', 'Piano', BROWN), role('e', 'white', 'White keys', BLUE),
                role('k', 'black', 'Black keys and pedals', BROWN), role('m', 'sheet', 'Sheet music, candles and picture', BLUE),
                role('n', 'notes', 'Notes', PINK), role('o', 'stool', 'Stool, flowers and flames', PINK),
                role('v', 'vase', 'Vase, metronome, frame and rug', GREEN), role('f', 'floor', 'Floor', GREEN)], ['things', 'music']


def violin(w, h, r):
    cv, s, cx, big = start(w, h, 'w')
    stage = r.random() > 0.55
    fy = h - 2
    if stage:
        for side in (-1, 1):
            x = 0 if side < 0 else w
            poly(cv, [(x, 0), (x - side * 4.0, 0), (x - side * 2.2, h * 0.5), (x - side * 3.0, fy), (x, fy)], 'c')
        box(cv, 0, 0, w, 1.2, 'c')
    box(cv, 0, fy, w, h, 'g')
    tilt = r.uniform(9, 15) * r.choice((-1, 1))
    to, back = turn(cx, 13.2, tilt)

    def local(f):
        return lambda px, py: f(*back(px, py))

    def body(u, v):
        if (abs(u) - 5.6) ** 2 + (v - 3.8) ** 2 <= 4.0:
            return False
        return (u / 4.3) ** 2 + (v / 3.5) ** 2 <= 1 or (u / 5.4) ** 2 + ((v - 7.2) / 4.3) ** 2 <= 1 or (abs(u) <= 3.6 and 1.0 <= v <= 6.0)
    fill(cv, 'b', local(body))
    fill(cv, 'n', local(lambda u, v: (abs(u) <= 0.95 and -13.0 <= v <= -10.0) or u * u + (v + 13.4) ** 2 <= 2.0))
    fill(cv, 'n', local(lambda u, v: abs(v + 11.6) <= 0.45 and abs(u) <= 2.2))
    fill(cv, 'f', local(lambda u, v: (abs(u) <= 1.0 + max(0.0, v + 2) * 0.03 and -10.0 <= v <= 4.4) or (abs(u) <= 1.4 - (v - 8.0) * 0.2 and 8.0 <= v <= 11.2)))
    for side in (-1, 1):
        seg(cv, *to(side * 2.7, 4.2), *to(side * 2.5, 8.2), 'h', 0.42)
    fill(cv, 's', local(lambda u, v: abs(u) <= 2.2 and 6.6 <= v <= 7.4))
    seg(cv, *to(0, -10.0), *to(0, 7.0), 's', 0.3)
    bx = r.uniform(-0.5, 1.0)
    bar(cv, *to(-10.0, 10.8 + bx), *to(10.0, 2.6 + bx), 0.7, 'k')
    disc(cv, *to(-9.4, 11.2 + bx), 1.05, 'k')
    for x, y in ((0.1, 0.32), (0.88, 0.18), (0.86, 0.62)) if not stage else ((0.3, 0.12), (0.72, 0.1)):
        note(cv, w * x, h * y, 'm')
    return cv, [('w', 'wall', 'Wall', BLUE, True), role('b', 'body', 'Violin', BROWN), role('n', 'scroll', 'Scroll and pegs', BROWN),
                role('f', 'board', 'Fingerboard and tailpiece', PINK), role('h', 'fhole', 'Sound holes', PINK),
                role('s', 'string', 'Strings and bridge', BLUE), role('k', 'bow', 'Bow', GREEN), role('m', 'notes', 'Music notes', GREEN),
                role('c', 'curtain', 'Curtains', PINK), role('g', 'floor', 'Floor', BROWN)], ['things', 'music']


def trumpet(w, h, r):
    cv, s, cx, big = start(w, h, 'w')
    banner = r.random() < 0.4
    fy = h - 3
    if banner:
        hills(cv, 'f', fy - 1, 1.0, w * 1.3, r.uniform(0, 6))
        cloud(cv, w * r.uniform(0.2, 0.8), h * 0.1, 1.8, 'c')
    else:
        for side in (-1, 1):
            x = 0 if side < 0 else w
            poly(cv, [(x, 0), (x - side * 4.2, 0), (x - side * 2.4, h * 0.5), (x - side * 3.4, fy), (x, fy)], 'c')
        box(cv, 0, fy, w, h, 'f')
    tilt = r.uniform(-14, -2)
    ty = 10.0 if banner else 13.0
    to, back = turn(cx + 0.3, ty, tilt)

    def local(f):
        return lambda px, py: f(*back(px, py))
    if banner:
        fill(cv, 'p', local(lambda u, v: -5.4 <= u <= 1.6 and 4.6 <= v <= 11.5 - max(0.0, 1.6 - abs(u + 1.9)) * 1.4))
        star(cv, *to(-1.9, 7.6), 2.0, 'e')
        for u in (-5.0, 1.2):
            seg(cv, *to(u, 3.6), *to(u, 4.8), 'k', 0.4)
    fill(cv, 't', local(lambda u, v: (-9.6 <= u <= 1.8 and abs(v) <= 0.65) or (-5.4 <= u <= 1.8 and abs(v - 3.6) <= 0.65)))
    fill(cv, 't', local(lambda u, v: u < -5.4 and 1.2 <= math.hypot(u + 5.4, v - 1.8) <= 2.45))
    fill(cv, 't', local(lambda u, v: 1.5 <= u <= 10.4 and abs(v) <= 0.65 + 0.012 * (u - 1.5) ** 2.6))
    fill(cv, 'k', local(lambda u, v: 9.6 <= u <= 10.4 and abs(v) <= 0.65 + 0.012 * (u - 1.5) ** 2.6))
    fill(cv, 'k', local(lambda u, v: -10.8 <= u <= -9.4 and abs(v) <= 0.5 + (-9.4 - u) * 0.5))
    for u in (-3.4, -1.2, 1.0):
        fill(cv, 't', local(lambda uu, vv, u=u: abs(uu - u) <= 0.7 and -2.2 <= vv <= 4.4))
        fill(cv, 'k', local(lambda uu, vv, u=u: (abs(uu - u) <= 0.9 and -3.0 <= vv <= -2.1) or (abs(uu - u) <= 0.45 and -4.4 <= vv <= -2.9)))
    if not banner:
        for k in range(3):
            x, y = to(11.0, -3.5 - k * 2.6)
            note(cv, min(w - 2.0, x - k * 1.6), y + r.uniform(-0.6, 0.6), 'm')
    if r.random() < 0.5:
        mirror(cv)
    return cv, [('w', 'wall', 'Sky' if banner else 'Stage wall', BLUE, True), role('t', 'brass', 'Trumpet', BROWN),
                role('k', 'cap', 'Valve caps, bell rim and cords', BROWN), role('m', 'notes', 'Music notes', GREEN),
                role('c', 'curtain', 'Cloud' if banner else 'Curtains', PINK), role('f', 'floor', 'Hills' if banner else 'Stage', GREEN),
                role('p', 'banner', 'Banner', PINK), role('e', 'emblem', 'Star', GREEN)], ['things', 'music']


def harp(w, h, r):
    cv, s, cx, big = start(w, h, 'w')
    window, cloudy = r.random() < 0.5, r.random() < 0.4
    fy = h - 3
    if cloudy:
        for x, y, k in ((4, fy - 0.5, 2.6), (13, fy, 3.0), (w - 2, fy - 1, 2.2)):
            cloud(cv, x, y, k, 'f')
        box(cv, 0, fy + 1, w, h, 'f')
        scatter(cv, 'm', 'w', 8, r, sep=3, area=(0, 0, w - 1, h * 0.5))
    else:
        box(cv, 0, fy, w, h, 'f')
        if window:
            rbox(cv, 13, 9, 20, 18, 3.0, 'v')
            rbox(cv, 14, 10, 19, 17, 2.4, 'w')
            box(cv, 16, 10, 17, 17, 'v')
        else:
            px = 18.5
            poly(cv, [(px - 1.6, fy), (px + 1.6, fy), (px + 1.2, fy - 3), (px - 1.2, fy - 3)], 'v')
            for dx, dy in ((-1.2, -5), (1.0, -5.6), (0, -6.8)):
                disc(cv, px + dx, fy + dy, 1.4, 'm')
            seg(cv, px, fy - 3, px, fy - 5, 'v', 0.4)
    bar(cv, 4.6, fy - 0.5, 4.3, 4.4, 1.8, 'p')
    disc(cv, 4.4, 3.4, 1.5, 'p')
    rbox(cv, 3.0, fy - 1.6, 11.5, fy, 0.5, 'p')
    neck = [(4.4, 3.8), (7.5, 4.8), (11.0, 7.0), (14.5, 5.8), (17.4, 3.8), (19.4, 4.4)]
    path(cv, neck, 'p', 0.8)

    def neck_y(x):
        for (a, b), (c, d) in zip(neck, neck[1:]):
            if a <= x <= c:
                return b + (d - b) * (x - a) / (c - a)
        return neck[-1][1]
    poly(cv, [(18.0, 4.4), (20.0, 5.0), (10.6, fy - 0.5), (6.2, fy - 0.5)], 'b')
    for x in range(6, 18, 2):
        top = neck_y(x + 0.5) + 0.6
        bottom = (fy - 0.5) - (x + 0.5 - 6.2) * (fy - 0.5 - 4.4) / (18.0 - 6.2)
        box(cv, x, top, x + 1, bottom, 's')
    if not cloudy:
        for x, y in ((0.86, 0.12), (0.68, 0.2)):
            note(cv, w * x, h * y, 'm')
    if r.random() < 0.5:
        mirror(cv)
    return cv, [('w', 'wall', 'Sky' if cloudy else 'Wall', BLUE, True), role('p', 'frame', 'Pillar and neck', BROWN),
                role('b', 'soundbox', 'Soundbox', PINK), role('s', 'string', 'Strings', BROWN),
                role('f', 'floor', 'Clouds' if cloudy else 'Floor', GREEN), role('m', 'notes', 'Stars' if cloudy else 'Notes and flowers', PINK),
                role('v', 'window', 'Window frame' if window else 'Vase', GREEN)], ['things', 'music']


def sparkle(cv, x, y, c):
    """A twinkle of five cells: a small plus."""
    box(cv, x - 1.5, y - 0.5, x + 1.5, y + 0.5, c)
    box(cv, x - 0.5, y - 1.5, x + 0.5, y + 1.5, c)


# ---- Toys and play ----


def yo_yo(w, h, r):
    cv, s, cx, big = start(w, h, 'w')
    yx, yy = cx + r.uniform(-3.0, 3.0), r.uniform(14.5, 16.5)
    k = r.random()
    pattern = 0 if k < 0.4 else 1 if k < 0.75 else 2
    sx = cx + r.uniform(-2.5, 2.5)
    fy = h - 4
    box(cv, 0, fy, w, h, 'f')
    dots(cv, 'q', 'w', 4, 4, area=(0, 1, w - 1, fy - 2))
    ring(cv, sx, 1.8, 1.7, 0.7, 'k')
    for side in (-1, 1):
        a0 = 150 if side < 0 else -30
        for a in range(a0, a0 + 60, 6):
            t = math.radians(a)
            x, y = yx + math.cos(t) * 7.6, yy + math.sin(t) * 7.6
            if 0 <= x < w and 0 <= y < fy:
                cv.put(int(x), int(y), 'm')
    disc(cv, yx, yy, 6.2, 'y')
    disc(cv, yx, yy, 4.8, 'z')
    if pattern == 0:
        star(cv, yx, yy, 4.2, 'x', ri=1.9, rot=r.uniform(-90, -54))
    elif pattern == 1:
        heart(cv, yx, yy + 0.2, 2.9, 'x')
    else:
        fill(cv, 'x', lambda px, py: (math.atan2(py - yy, px - yx) * 2 / math.pi) % 1 < 0.5, only='z')
    seg(cv, sx, 2.6, yx, yy, 'k', 0.4)
    disc(cv, yx, yy, 1.05, 'k')
    if r.random() < 0.5:
        bx = r.choice((3.5, w - 3.5))
        disc(cv, bx, fy - 2.0, 2.2, 'y')
        box(cv, bx - 2.4, fy - 2.5, bx + 2.4, fy - 1.5, 'z', only='y')
    return cv, [('w', 'wall', 'Wall', GREEN, True), role('q', 'dots', 'Wall dots', BLUE), role('y', 'rim', 'Yo-yo and ball', PINK),
                role('z', 'face', 'Yo-yo face and ball stripe', BROWN), role('x', 'pattern', 'Pattern', PINK), role('k', 'string', 'String', BLUE),
                role('m', 'motion', 'Motion lines', BROWN), role('f', 'floor', 'Floor', BROWN)], ['toys']


def spinning_top(w, h, r):
    cv, s, cx, big = start(w, h, 'w')
    gores, window = r.random() < 0.5, r.random() < 0.3
    fy = h - 5
    box(cv, 0, fy, w, h, 'f')
    for x in range(int(r.random() * 3), w, 4):
        box(cv, x, fy + 1, x + 0.9, h, 'o')
    if window:
        wx = r.choice((1.5, w - 7.5))
        box(cv, wx, 1.5, wx + 6, 8.5, 'o')
        box(cv, wx + 1, 2.5, wx + 5, 7.5, 'm')
        box(cv, wx + 2.5, 2.5, wx + 3.5, 7.5, 'o')
    else:
        for x in range(1, w, 3):
            poly(cv, [(x, 1.6 + 0.3 * math.sin(x)), (x + 2.2, 1.6 + 0.3 * math.sin(x)), (x + 1.1, 4.0)], 'z' if x % 2 else 'q')
    tx = cx + r.uniform(-1.5, 1.5)
    tilt = r.uniform(6, 14) * r.choice((-1, 1))
    to, back = turn(tx, fy - 0.3, tilt)

    def local(f):
        return lambda px, py: f(*back(px, py))

    def body(u, v):
        return (u / 7.6) ** 2 + ((v + 11.0) / 4.0) ** 2 <= 1 or (-11.0 <= v <= -1.6 and abs(u) <= 7.6 * (v + 1.6) / -9.4)
    fill(cv, 'b', local(body))
    if gores:
        fill(cv, 'z', local(lambda u, v: body(u, v) and v < -1.6 and int((math.asin(max(-1.0, min(1.0, u / 7.8))) / math.pi + 0.5) * 6) % 2 == 0))
    else:
        fill(cv, 'z', local(lambda u, v: body(u, v) and -11.8 <= v <= -10.2))
        fill(cv, 'q', local(lambda u, v: body(u, v) and -8.2 <= v <= -7.0))
        fill(cv, 'z', local(lambda u, v: body(u, v) and -5.0 <= v <= -4.0))
    fill(cv, 'h', local(lambda u, v: (abs(u) <= 0.9 and -18.0 <= v <= -14.4) or u * u + (v + 18.4) ** 2 <= 1.6))
    fill(cv, 'h', local(lambda u, v: abs(u) <= 0.7 and -2.2 <= v <= 0.2))
    for side in (-1, 1):
        for k in range(2):
            seg(cv, tx + side * (9.2 + k * 0.8), fy - 12 + k * 3, tx + side * (9.2 + k * 0.8), fy - 9 + k * 3, 'm', 0.4)
    oval(cv, tx, fy + 0.6, 3.4, 0.7, 'k')
    if r.random() < 0.5:
        mirror(cv)
    return cv, [('w', 'wall', 'Wall', BLUE, True), role('b', 'top', 'Spinning top', PINK), role('z', 'stripe', 'Stripes and bunting', BROWN),
                role('q', 'band', 'Middle band and bunting', GREEN), role('h', 'handle', 'Handle and tip', BROWN),
                role('m', 'motion', 'Motion lines and window', BLUE), role('k', 'shadow', 'Shadow', GREEN),
                role('o', 'planks', 'Floor lines and frame', BROWN), role('f', 'floor', 'Floor', GREEN)], ['toys']


def rocking_horse(w, h, r):
    cv, s, cx, big = start(w, h, 'w')
    flip, window, toy = r.random() < 0.5, r.random() < 0.5, r.random() < 0.5
    fy = h - 4
    box(cv, 0, fy, w, h, 'f')
    rx = cx + r.uniform(-1.0, 1.0)
    oval(cv, rx, fy + 1.6, 9.0, 1.6, 'q')
    if window:
        wx = r.choice((1.5, w - 7.5))
        box(cv, wx, 1.5, wx + 6, 8.5, 'r')
        box(cv, wx + 1, 2.5, wx + 5, 7.5, 'q')
        box(cv, wx + 2.5, 2.5, wx + 3.5, 7.5, 'r')
    else:
        path(cv, [(0, 1.0), (cx, 3.0), (w, 1.0)], 'r', 0.4)
        for k, x in enumerate(range(2, w - 1, 3)):
            y = 1.0 + 2.0 * (1 - ((x + 0.5 - cx) / cx) ** 2)
            poly(cv, [(x - 0.2, y), (x + 2.2, y), (x + 1.0, y + 2.6)], 'm' if k % 2 else 'q')
    rock = r.uniform(-7, 7)
    to, back = turn(rx, fy - 1.0, rock)

    def local(f):
        return lambda px, py: f(*back(px, py))
    fill(cv, 'r', local(lambda u, v: abs(u) <= 10.0 and 16.4 <= math.hypot(u, v + 18.0) <= 17.9 and v > -6))
    for a, b in (((-4.0, -9.2), (-6.4, -2.2)), ((4.0, -9.2), (6.4, -2.2)), ((-2.6, -9.2), (-3.4, -1.6)), ((2.6, -9.2), (3.4, -1.6))):
        bar(cv, *to(*a), *to(*b), 1.3, 'h')
    fill(cv, 'h', local(lambda u, v: (u / 6.2) ** 2 + ((v + 10.4) / 2.8) ** 2 <= 1))
    fill(cv, 'h', local(lambda u, v: -17.0 <= v <= -10.0 and abs(u - (4.6 + (v + 10.0) / -7.0 * 2.4)) <= 1.7))
    fill(cv, 'h', local(lambda u, v: ((u - 7.6) / 2.8) ** 2 + ((v + 17.0) / 1.7) ** 2 <= 1 or ((u - 9.4) / 1.5) ** 2 + ((v + 16.2) / 1.3) ** 2 <= 1))
    poly(cv, [to(5.6, -18.0), to(6.0, -20.4), to(7.2, -18.2)], 'h')
    fill(cv, 'm', local(lambda u, v: -18.6 <= v <= -10.0 and abs(u - (3.0 + (v + 10.0) / -8.6 * 2.6)) <= 1.0))
    fill(cv, 'm', local(lambda u, v: u < -5.6 and -12.8 <= v <= -5.0 and abs(u - (-6.2 - (v + 12.0) * 0.32)) <= 1.1))
    fill(cv, 's', local(lambda u, v: -2.4 <= u <= 1.8 and -13.6 <= v <= -11.8))
    fill(cv, 's', local(lambda u, v: -1.6 <= u <= 1.0 and -11.8 <= v <= -9.4))
    x, y = to(8.0, -17.6)
    cv.put(int(x), int(y), 'e')
    scatter(cv, 'e', 'h', 5, r, sep=2, area=(rx - 6, fy - 14, rx + 5, fy - 8))
    if toy:
        bx = r.choice((2.0, w - 2.0))
        disc(cv, bx, fy - 1.8, 1.9, 'm')
        box(cv, bx - 2.0, fy - 2.3, bx + 2.0, fy - 1.3, 's', only='m')
    if flip:
        mirror(cv)
    return cv, [('w', 'wall', 'Wall', BLUE, True), role('h', 'horse', 'Horse', BROWN), role('m', 'mane', 'Mane, tail and flags', PINK),
                role('s', 'saddle', 'Saddle and stripe', GREEN), role('r', 'rocker', 'Rockers and window frame', PINK),
                role('e', 'eye', 'Eye and dapples', BROWN), role('q', 'rug', 'Rug, window and flags', BLUE), role('f', 'floor', 'Floor', GREEN)], ['toys', 'cozy']


def jack_in_the_box(w, h, r):
    cv, s, cx, big = start(w, h, 'w')
    k = r.random()
    deco = 0 if k < 0.12 else 1 if k < 0.5 else 2
    hat, flip, lean = r.random() < 0.6, r.random() < 0.5, r.uniform(-1.6, 1.6)
    fy = h - 3
    box(cv, 0, fy, w, h, 'g')
    bx0, bx1, by = 5, w - 5, 19
    box(cv, bx0, by, bx1, fy, 'b')
    box(cv, bx0 - 0.6, by - 0.6, bx1 + 0.6, by + 0.5, 's')
    bar(cv, bx0 - 0.2, by - 0.8, bx0 - 5.0, by - 6.0, 1.8, 'b')
    bar(cv, bx0 - 0.9, by - 1.6, bx0 - 5.7, by - 6.8, 0.5, 's')
    if deco == 0:
        star(cv, cx, by + 3.0, 2.6, 'x')
    elif deco == 1:
        for x in (bx0 + 2.5, cx, bx1 - 2.5):
            poly(cv, [(x, by + 1.0), (x + 1.6, by + 3.0), (x, by + 5.0), (x - 1.6, by + 3.0)], 'x')
    else:
        heart(cv, cx, by + 3.0, 2.3, 'x')
    seg(cv, bx1 + 0.4, by + 2.5, bx1 + 2.8, by + 2.5, 's', 0.45)
    seg(cv, bx1 + 2.8, by + 2.5, bx1 + 2.8, by + 4.6, 's', 0.45)
    disc(cv, bx1 + 2.8, by + 5.0, 1.0, 's')
    hx = cx + lean
    pts = [(cx, by - 0.6)]
    for k in range(4):
        pts.append((cx + lean * (k + 1) / 5 + (2.3 if k % 2 == 0 else -2.3), by - 1.5 - k * 1.25))
    pts.append((hx, by - 6.2))
    path(cv, pts, 's', 0.4)
    for k in range(5):
        disc(cv, hx - 3.6 + k * 1.8, by - 6.4, 1.15, 'q')
    hy = by - 9.6
    disc(cv, hx, hy, 3.3, 'f')
    for side in (-1, 1):
        box(cv, hx + side * 1.4 - 0.5, hy - 1.6, hx + side * 1.4 + 0.5, hy + 0.2, 's')
    disc(cv, hx, hy + 0.9, 1.05, 'q')
    box(cv, hx - 1.5, hy + 2.2, hx + 1.5, hy + 2.9, 'q', only='f')
    if hat:
        poly(cv, [(hx - 3.0, hy - 2.4), (hx + 3.0, hy - 2.4), (hx + 1.4, hy - 7.6)], 'c')
        disc(cv, hx + 1.4, hy - 8.0, 1.05, 'x')
    else:
        for side in (-1, 1):
            poly(cv, [(hx, hy - 2.8), (hx + side * 1.5, hy - 3.6), (hx + side * 5.4, hy - 6.2), (hx + side * 3.4, hy - 1.8)], 'c')
            disc(cv, hx + side * 5.6, hy - 6.4, 1.05, 'x')
    if flip:
        mirror(cv)
    return cv, [('w', 'wall', 'Wall', GREEN, True), role('b', 'box', 'Box and lid', PINK), role('x', 'deco', 'Box pattern and pompoms', BROWN),
                role('s', 'spring', 'Spring, rims, crank and eyes', BLUE), role('q', 'collar', 'Collar, nose and smile', PINK),
                role('f', 'face', 'Face', BROWN), role('c', 'hat', 'Hat', BLUE), role('g', 'floor', 'Floor', BROWN)], ['toys']


def building_blocks(w, h, r):
    cv, s, cx, big = start(w, h, 'w')
    layout = int(r.random() * 3)
    fy = h - 3
    box(cv, 0, fy, w, h, 'f')
    if layout == 0:
        spots = [(1, 0, 'a'), (8, 0, 'b'), (15, 0, 'd'), (4.5, 1, 'c'), (11.5, 1, 'c'), (8, 2, 'e')]
    elif layout == 1:
        spots = [(1, 0, 'a'), (1, 1, 'c'), (1, 2, 'b'), (9, 0, 'b'), (9, 1, 'd')]
    else:
        spots = [(2, 0, 'b'), (9, 0, 'a'), (6, 1, 'c'), (14.5, 0, 'c')]
    letters = r.sample('ABCDEK123', len(spots))
    for (x, k, c), ch in zip(spots, letters):
        x0, y0 = int(x), fy - 7 * (k + 1)
        box(cv, x0, y0, x0 + 5.9, y0 + 6.9, c)
        glyph(cv, ch, x0 + 1.5, y0 + 1, 'l')
    if layout == 1:
        poly(cv, [(8.6, fy - 14), (15.4, fy - 14), (12, fy - 18.5)], 'c')
        disc(cv, 18.5, fy - 2.6, 2.6, 'd')
        box(cv, 15.6, fy - 3.1, 21.4, fy - 2.1, 'e', only='d')
    elif layout == 2:
        poly(cv, [(5.6, fy - 14), (12.4, fy - 14), (9, fy - 18.5)], 'e')
        disc(cv, 17.0, fy - 9.4, 2.4, 'd')
        box(cv, 14.0, fy - 9.9, 20.0, fy - 8.9, 'e', only='d')
    return cv, [('w', 'wall', 'Wall', BLUE, True), role('a', 'block_a', 'Pink blocks', PINK), role('b', 'block_b', 'Green blocks', GREEN),
                role('c', 'block_c', 'Orange blocks and roof', BROWN), role('d', 'block_d', 'Violet blocks and ball', PINK),
                role('e', 'block_e', 'Top block, roof and stripe', GREEN), role('l', 'letters', 'Letters', BLUE), role('f', 'floor', 'Floor', BROWN)], ['toys']


# ---- Magic and time ----


def compass(w, h, r):
    cv, s, cx, big = start(w, h, 'm')
    ox, ang, right, river = r.uniform(-1.5, 1.5), r.uniform(-30, 30), r.random() < 0.5, r.random() < 0.7
    sx = w if right else 0
    if river:
        path(cv, [(sx, 0), (sx + (-1 if right else 1) * 5, 5), (sx + (-1 if right else 1) * 3, 11), (sx + (-1 if right else 1) * 6, 17)], 'w', 1.2)
        oval(cv, sx + (-1 if right else 1) * 6.5, 19.0, 3.4, 2.0, 'w')
    else:
        fill(cv, 'w', lambda px, py: math.hypot(px - sx, py) <= 8.5 + math.sin(py * 1.3) * 0.6)
    for k in range(3):
        x, y = r.uniform(3, w - 3), r.uniform(20, h - 3)
        oval(cv, x, y, r.uniform(2.0, 3.0), r.uniform(1.4, 1.9), 'g')
    for x0 in ((2.0, 5.5) if right else (13.0, 16.5)):
        poly(cv, [(x0, 9.6), (x0 + 1.8, 6.4), (x0 + 3.6, 9.6)], 'g')
    pts = [(r.uniform(2, 5), h - 2.5), (r.uniform(6, 9), 22.0), (r.uniform(13, 16), 24.5), (w - 3.5, r.uniform(19, 21))]
    for (a, b), (c, d) in zip(pts, pts[1:]):
        n = int(math.hypot(c - a, d - b) / 2.2)
        for k in range(n):
            t = (k + 0.25) / n
            seg(cv, a + (c - a) * t, b + (d - b) * t, a + (c - a) * (t + 0.35 / n), b + (d - b) * (t + 0.35 / n), 'p', 0.45)
    ex, ey = pts[-1]
    seg(cv, ex - 1.2, ey - 1.2, ex + 1.2, ey + 1.2, 'p', 0.5)
    seg(cv, ex - 1.2, ey + 1.2, ex + 1.2, ey - 1.2, 'p', 0.5)
    ccx, ccy, R = cx + ox, 12.8, 7.0
    ring(cv, ccx, ccy - R - 0.6, 1.6, 0.6, 'c')
    disc(cv, ccx, ccy, R, 'c')
    disc(cv, ccx, ccy, R - 1.2, 'f')
    to, back = turn(ccx, ccy, ang)
    for k in range(4):
        poly(cv, [to(*rot) for rot in (((0, -5.6), (0.9, -4.4), (-0.9, -4.4)), ((5.6, 0), (4.4, 0.9), (4.4, -0.9)),
                                       ((0, 5.6), (0.9, 4.4), (-0.9, 4.4)), ((-5.6, 0), (-4.4, 0.9), (-4.4, -0.9)))[k]], 'c')
    poly(cv, [to(0, -4.4), to(1.9, 0), to(-1.9, 0)], 'n')
    poly(cv, [to(0, 4.4), to(1.9, 0), to(-1.9, 0)], 'c')
    disc(cv, ccx, ccy, 0.8, 'c')
    return cv, [('m', 'map', 'Map', BROWN, True), role('w', 'sea', 'River' if river else 'Sea', BLUE), role('g', 'land', 'Islands and mountains', GREEN),
                role('p', 'path', 'Path and X', PINK), role('c', 'case', 'Case, points and needle tail', BLUE),
                role('f', 'face', 'Face', BROWN), role('n', 'needle', 'North needle', PINK)], ['things', 'travel']


def hourglass(w, h, r):
    cv, s, cx, big = start(w, h, 'w')
    fy = h - 3
    box(cv, 0, fy, w, h, 't')
    k = r.random()
    prop = 0 if k < 0.5 else 1 if k < 0.7 else 2
    if prop == 0:
        wx = r.choice((1, w - 6))
        box(cv, wx, 2, wx + 5, 9, 'v')
        box(cv, wx + 1, 3, wx + 4, 8, 'h')
        box(cv, wx + 2, 3, wx + 3, 8, 'v')
    elif prop == 1:
        bx = r.choice((0.5, w - 4.5))
        for k, c in enumerate('vsv'):
            box(cv, bx, fy - 2 * (k + 1), bx + 4, fy - 2 * k - 0.1, c)
    else:
        for x, y in ((3, 3), (w - 4, 6), (5, 9)):
            sparkle(cv, x + r.uniform(-1, 1), y, 'h')
    hx = cx + (r.uniform(-1.0, 1.0) if prop != 1 else (2.0 if bx < cx else -2.0))
    top, bot = 2.0, fy - 0.2
    rbox(cv, hx - 7, top, hx + 7, top + 2.0, 0.6, 'k')
    rbox(cv, hx - 7, bot - 2.0, hx + 7, bot, 0.6, 'k')
    for side in (-1, 1):
        box(cv, hx + side * 5.8 - 0.5, top + 2, hx + side * 5.8 + 0.5, bot - 2, 'k')
    ym = (top + bot) / 2
    half = ym - top - 2.0

    def bulb(px, py, inset=0.0):
        t = abs(py - ym) / half
        if t > 1.0 - inset / half:
            return False
        hw = 0.8 + 4.0 * math.sin(math.pi / 2 * min(1.0, t * 1.15)) ** 0.9 - 1.2 * max(0.0, t - 0.8) / 0.2
        return abs(px - hx) <= hw - inset
    fill(cv, 'g', bulb)
    f = r.choice((0.25, 0.5, 0.8))
    level = ym - 1.0 - (half - 2.0) * f
    fill(cv, 's', lambda px, py: py < ym and py >= level and bulb(px, py, 1.0))
    pile = (half - 1.6) * (1.0 - f) + 1.0
    fill(cv, 's', lambda px, py: py > ym and bulb(px, py, 1.0) and py >= bot - 2.0 - 1.0 - pile * (1.0 - ((px - hx) / 4.0) ** 2))
    box(cv, hx - 0.5, ym - 1.0, hx + 0.5, bot - 3.0 - pile, 's', only='gs')
    box(cv, hx - 3.4, top + 3.0, hx - 2.4, top + 5.5, 'h', only='g')
    return cv, [('w', 'wall', 'Wall', PINK, True), role('k', 'frame', 'Frame', BROWN), role('g', 'glass', 'Glass', BLUE),
                role('s', 'sand', 'Sand and book', BROWN), role('h', 'shine', 'Shine and window', BLUE),
                role('v', 'detail', 'Window frame and books', GREEN), role('t', 'table', 'Table', GREEN)], ['things', 'time']


def candle(w, h, r):
    cv, s, cx, big = start(w, h, 'n')
    window, kind = r.random() < 0.6, int(r.random() * 3)
    ty = h - 4
    box(cv, 0, ty, w, h, 't')
    if window:
        wx = r.choice((1.5, w - 8.5))
        rbox(cv, wx, 1.5, wx + 7, 9.5, 0.6, 'o')
        box(cv, wx + 1, 2.5, wx + 6, 8.5, 'n')
        box(cv, wx + 3, 2.5, wx + 4, 8.5, 'o')
        box(cv, wx + 1, 5, wx + 6, 6, 'o')
        disc(cv, wx + 2.2, 3.8, 1.05, 'y')

    def one(x, top, bottom, half):
        rbox(cv, x - half, top, x + half, bottom, 0.4, 'c')
        for k in range(int(2 * half)):
            if r.random() < 0.5:
                box(cv, x - half + k, top, x - half + k + 0.9, top + r.uniform(1.0, 3.0), 'c')
        box(cv, x - 0.5, top - 1.2, x + 0.5, top, 'k')
        fill(cv, 'f', lambda px, py: (py >= top - 3.6 and math.hypot(px - x, py - (top - 3.0)) <= 1.9) or
             (top - 7.8 <= py < top - 3.6 and abs(px - x) <= 1.9 * ((py - (top - 7.8)) / 4.2) ** 0.8))
        oval(cv, x, top - 3.2, 0.8, 1.4, 'y')
    if kind == 0:
        one(cx, 12.0, ty - 1.4, 2.4)
        oval(cv, cx, ty - 0.9, 6.4, 1.3, 'h')
        ring(cv, cx + 6.8, ty - 2.0, 1.7, 0.7, 'h')
    elif kind == 1:
        for x, top in ((cx - 5.5, 16.0), (cx, 11.0), (cx + 5.5, 14.0)):
            one(x, top, ty - 1.2, 1.7)
        rbox(cv, 1.5, ty - 1.6, w - 1.5, ty, 0.6, 'h')
    else:
        one(cx, 10.0, 17.0, 1.7)
        oval(cv, cx, 17.6, 3.0, 0.9, 'h')
        box(cv, cx - 0.8, 17.6, cx + 0.8, ty - 2.0, 'h')
        disc(cv, cx, 20.5, 1.4, 'h')
        poly(cv, [(cx - 4.0, ty), (cx + 4.0, ty), (cx + 1.4, ty - 2.2), (cx - 1.4, ty - 2.2)], 'h')
    return cv, [('n', 'wall', 'Evening wall', BLUE, True), role('c', 'wax', 'Candle', PINK), role('k', 'wick', 'Wick', BROWN),
                role('f', 'flame', 'Flame', BROWN), role('y', 'core', 'Flame core and moon', PINK), role('h', 'holder', 'Holder', GREEN),
                role('o', 'window', 'Window frame', GREEN), role('t', 'table', 'Table', BROWN)], ['things', 'cozy']


# ---- Magic, prizes and the desk ----


def magic_wand(w, h, r):
    cv, s, cx, big = start(w, h, 'n')
    flip, cloudy, size, scene = r.random() < 0.5, r.random() < 0.6, r.uniform(4.4, 5.4), int(r.random() * 3)
    gtop = h - 4
    if scene == 0:
        hills(cv, 'g', gtop, 1.0, w * 1.2, r.uniform(0, 6))
        kx = r.uniform(2.0, 5.0)
        for x, top in ((kx, gtop - 6), (kx + 3.0, gtop - 4), (kx + 6.0, gtop - 6.5)):
            box(cv, x - 1.0, top, x + 1.0, gtop + 1, 'c')
            poly(cv, [(x - 1.5, top + 0.2), (x, top - 2.4), (x + 1.5, top + 0.2)], 'c')
        box(cv, kx - 1, gtop - 3.5, kx + 6, gtop + 1, 'c')
    elif scene == 1:
        box(cv, 0, gtop, w, h, 't')
        bx = r.uniform(2.0, 4.0)
        poly(cv, [(bx, gtop + 0.2), (bx + 4.5, gtop - 1.2), (bx + 9.0, gtop + 0.2), (bx + 9.0, gtop + 1.4), (bx, gtop + 1.4)], 'c')
        poly(cv, [(bx + 0.6, gtop - 0.2), (bx + 4.5, gtop - 1.6), (bx + 4.5, gtop - 0.4), (bx + 0.6, gtop + 0.6)], 'g')
        poly(cv, [(bx + 4.5, gtop - 1.6), (bx + 8.4, gtop - 0.2), (bx + 8.4, gtop + 0.6), (bx + 4.5, gtop - 0.4)], 'g')
    else:
        for x in (3.0, 10.0, 17.5):
            cloud(cv, x + r.uniform(-1, 1), gtop + 1.0, 2.2, 'g')
        box(cv, 0, gtop + 2, w, h, 'g')
    sun_moon(cv, w * 0.16, h * 0.1, 2.4, 'x', 'n', True)
    if cloudy:
        cloud(cv, w * 0.62, h * 0.09, 1.4, 'o')
    sx, sy = 15.0, 8.6
    bar(cv, 3.6, gtop - 1.0, sx - 1.0, sy + 2.0, 1.5, 'k')
    bar(cv, 3.6, gtop - 1.0, 5.4, gtop - 3.6, 1.7, 'w')
    star(cv, sx, sy, size, 'y', ri=size * 0.45, rot=-90 + r.uniform(-12, 12))
    star(cv, sx, sy, size * 0.42, 'q', ri=size * 0.2)
    bx, by = sx - 3.0, sy + 4.4
    poly(cv, [(bx, by), (bx - 2.4, by - 1.4), (bx - 2.0, by + 1.4)], 'r')
    poly(cv, [(bx, by), (bx + 1.4, by + 2.4), (bx - 1.2, by + 2.2)], 'r')
    for k, (dx, dy) in enumerate(((-7.0, -3.0), (-9.5, 2.0), (5.0, 6.5), (3.8, -6.0))):
        sparkle(cv, sx + dx + r.uniform(-0.5, 0.5), sy + dy, 'x')
    if flip:
        mirror(cv)
    return cv, [('n', 'night', 'Night sky', BLUE, True), role('k', 'wand', 'Wand', PINK), role('w', 'grip', 'Grip', BROWN),
                role('y', 'star', 'Star', BROWN), role('q', 'glow', 'Star glow', PINK), role('r', 'ribbon', 'Ribbon', PINK),
                role('x', 'sparkle', 'Moon and sparkles', BROWN), role('o', 'cloud', 'Cloud', BLUE),
                role('g', 'ground', ['Hills', 'Book pages', 'Clouds'][scene], GREEN),
                role('c', 'detail', 'Castle' if scene == 0 else 'Book cover', GREEN), role('t', 'table', 'Table', BROWN)], ['things', 'magic']


def wizard_hat(w, h, r):
    cv, s, cx, big = start(w, h, 'n')
    scene, flip = int(r.random() * 3), r.random() < 0.5
    fy = h - 4
    if scene == 0:
        box(cv, 0, fy, w, h, 't')
        for k, (x0, x1, c) in enumerate(((3.0, 19.0, 'v'), (4.0, 18.0, 'm'), (2.5, 17.5, 'v'))):
            box(cv, x0, fy - 2.4 * (k + 1), x1, fy - 2.4 * k - 0.1, c)
            box(cv, x1 - 1.6, fy - 2.4 * (k + 1), x1 - 0.8, fy - 2.4 * k - 0.1, 'y')
        by = fy - 7.6
    elif scene == 1:
        box(cv, 0, fy, w, h, 't')
        wx = r.choice((1.0, w - 8.0))
        rbox(cv, wx, 2.0, wx + 7.0, 10.0, 0.6, 'v')
        box(cv, wx + 1, 3.0, wx + 6, 9.0, 'n')
        box(cv, wx + 3, 3.0, wx + 4, 9.0, 'v')
        disc(cv, wx + 2.0, 4.6, 1.05, 'y')
        by = fy - 0.6
    else:
        for x in (3.0, 11.0, 19.0):
            cloud(cv, x + r.uniform(-1, 1), fy + 1.0, 2.4, 'm')
        box(cv, 0, fy + 2, w, h, 'm')
        scatter(cv, 'x', 'n', 10, r, sep=3, area=(0, 0, w - 1, fy - 3))
        by = fy - 4.0
    for k in range(2 if scene != 2 else 0):
        sparkle(cv, r.choice((2.5, w - 2.5)) if k == 0 else r.uniform(8, 14), r.uniform(2.5, 5.0), 'y')
    pts = [(cx - 6.0, by - 0.6), (cx - 4.8, by - 6.2), (cx - 3.0, by - 11.0), (cx - 0.6, by - 14.4), (cx + 2.6, by - 16.4),
           (cx + 6.6, by - 15.6), (cx + 3.4, by - 14.4), (cx + 1.8, by - 11.4), (cx + 3.6, by - 6.2), (cx + 6.0, by - 0.6)]
    poly(cv, pts, 'h')
    oval(cv, cx, by, 9.6, 1.9, 'b')
    box(cv, 0, by - 3.4, w, by - 1.6, 'k', only='h')
    rbox(cv, cx - 1.4, by - 3.8, cx + 1.4, by - 1.2, 0.3, 'q')
    box(cv, cx - 0.5, by - 3.0, cx + 0.5, by - 2.0, 'k')
    star(cv, cx + 6.8, by - 15.8, 1.9, 'y')
    star(cv, cx - 1.6, by - 7.0, 1.8, 'y')
    disc(cv, cx + 1.6, by - 10.6, 1.4, 'y')
    disc(cv, cx + 2.2, by - 11.0, 1.2, 'h')
    if flip:
        mirror(cv)
    return cv, [('n', 'night', 'Night sky', BLUE, True), role('h', 'hat', 'Hat', PINK), role('b', 'brim', 'Brim', PINK),
                role('k', 'band', 'Band', BROWN), role('q', 'buckle', 'Buckle', GREEN), role('y', 'stars', 'Stars and moons', BROWN),
                role('x', 'specks', 'Tiny stars', BROWN), role('v', 'books', 'Books' if scene == 0 else 'Window frame', GREEN),
                role('m', 'detail', 'Book' if scene == 0 else 'Clouds', BLUE), role('t', 'table', 'Table', GREEN)], ['things', 'magic']


def trophy(w, h, r):
    cv, s, cx, big = start(w, h, 'w')
    k = r.random()
    layout = 0 if k < 0.12 else 1 if k < 0.5 else 2
    fy = h - 3
    box(cv, 0, fy, w, h, 'f')
    if layout == 0:
        for x0, x1, top, ch in ((cx - 4, cx + 4, fy - 7, '1'), (0, cx - 4, fy - 5, '2'), (cx + 4, w, fy - 3.4, '3')):
            box(cv, x0, top, x1, fy, 'b')
            glyph(cv, ch, (x0 + x1) / 2 - 1, top + 1, 'n')
        base, sc = fy - 7, 0.82
    elif layout == 1:
        box(cv, 1, fy - 3.4, w - 1, fy - 2.0, 'b')
        box(cv, 3, fy - 2.0, 4, fy, 'b')
        box(cv, w - 4, fy - 2.0, w - 3, fy, 'b')
        scatter(cv, 'x', 'w', 14, r, sep=3, area=(0, 0, w - 1, fy - 5))
        base, sc = fy - 3.4, 0.9
    else:
        base, sc = fy, 0.95

    def at(dx, dy):
        return cx + dx * sc, base + dy * sc
    rbox(cv, *at(-4.8, -6.0), *at(4.8, 0), 0.6, 'p' if layout else 'c')
    if layout:
        box(cv, *at(-3.0, -4.6), *at(3.0, -1.4), 'c')
    poly(cv, [at(-3.4, -6.0), at(3.4, -6.0), at(1.4, -8.0), at(-1.4, -8.0)], 'c')
    box(cv, *at(-0.8, -10.6), *at(0.8, -7.8), 'c')
    oval(cv, *at(0, -9.6), 1.8 * sc, 0.9, 'c')
    top = -20.6
    fill(cv, 'c', lambda px, py: py >= at(0, top + 1.0)[1] and ((px - cx) / (6.2 * sc)) ** 2 + ((py - at(0, top + 1.0)[1]) / (9.4 * sc)) ** 2 <= 1)
    box(cv, *at(-6.6, top + 0.2), *at(6.6, top + 1.4), 'c')
    for side in (-1, 1):
        hx, hy = at(side * 6.4, top + 4.4)
        fill(cv, 'c', lambda px, py, hx=hx, hy=hy, side=side: 1.3 < math.hypot(px - hx, py - hy) <= 2.6 and (px - cx) * side > 5.8 * sc)
    star(cv, *at(0, top + 4.8), 2.4, 's')
    if layout == 2:
        for side in (-1, 1):
            for k in range(5):
                a = math.radians(200 - k * 26) if side < 0 else math.radians(-20 + k * 26)
                lx, ly = cx + math.cos(a) * 9.0, base - 9.0 - math.sin(a) * 9.0
                lens(cv, lx, ly, lx + side * 1.6, ly - 2.0, 1.4, 'l')
        oval(cv, cx, fy + 0.3, 7.0, 0.8, 'p')
    if layout != 0 and r.random() < 0.5:
        for x in (r.uniform(1.5, 3.5), r.uniform(18.5, 20.5)):
            y = r.uniform(4.0, 8.0)
            oval(cv, x, y, 1.5, 1.9, 's')
            seg(cv, x, y + 1.9, x + 0.5, y + 6.0, 'n', 0.35)
    return cv, [('w', 'wall', 'Wall', BLUE, True), role('c', 'cup', 'Trophy', BROWN), role('s', 'star', 'Star and balloons', PINK),
                role('p', 'base', 'Base', PINK), role('b', 'stand', 'Podium' if layout == 0 else 'Shelf and number', GREEN),
                role('n', 'digits', 'Numbers', BLUE), role('x', 'confetti', 'Confetti', BROWN), role('l', 'laurel', 'Laurels', GREEN),
                role('f', 'floor', 'Floor', BROWN)], ['things', 'party']


def paint_palette(w, h, r):
    cv, s, cx, big = start(w, h, 'w')
    flip, scene = r.random() < 0.5, int(r.random() * 3)
    fy = h - 3
    box(cv, 0, fy, w, h, 'f')
    if scene == 0:
        for a, b in (((5, fy), (9, 2)), ((17, fy), (13, 2)), ((11, 2), (11, fy))):
            bar(cv, *a, *b, 1.0, 'f')
        box(cv, 3, 3, 19, 11, 'e')
        disc(cv, 15.5, 5.5, 1.6, 'a')
        hills(cv, 'g', 9.0, 0.8, 12, 1.0, only='e')
    elif scene == 1:
        for x, c in ((2.5, 'a'), (6.5, 'b'), (w - 6.5, 'g'), (w - 2.5, 'v')):
            rbox(cv, x - 1.6, fy - 4.0, x + 1.6, fy, 0.5, 'e')
            box(cv, x - 1.6, fy - 4.6, x + 1.6, fy - 3.6, c)
    else:
        for x0, y0 in ((1.5, 1.5), (13.5, 2.5)):
            box(cv, x0, y0, x0 + 7, y0 + 5.5, 'h')
            box(cv, x0 + 1, y0 + 1, x0 + 6, y0 + 4.5, 'e')
            disc(cv, x0 + 4.5, y0 + 2.2, 1.0, 'a')
            hills(cv, 'g', y0 + 4.0, 0.6, 6, 0.5, only='e')
    deg = r.uniform(-14, 14)
    to, back = turn(cx, 16.0, deg)

    def local(f):
        return lambda px, py: f(*back(px, py))
    fill(cv, 'p', local(lambda u, v: (u / 9.6) ** 2 + (v / 6.6) ** 2 <= 1 and math.hypot(u + 9.0, v - 3.4) > 2.6))
    fill(cv, 'w', local(lambda u, v: math.hypot(u + 5.2, v - 1.6) <= 1.4))
    colours = ['a', 'b', 'g', 'v', 'c', 'a']
    r.shuffle(colours)
    for (u, v), c in zip(((-4.6, -3.4), (-0.8, -4.4), (3.0, -4.2), (6.4, -2.4), (7.0, 1.4), (2.6, 3.0)), colours):
        x, y = to(u, v)
        disc(cv, x, y, 1.6 + r.random() * 0.4, c)
    bar(cv, *to(-2.0, 5.6), *to(10.6, -1.2), 1.2, 'h')
    bar(cv, *to(-2.0, 5.6), *to(-3.6, 6.5), 1.4, 'y')
    poly(cv, [to(-3.4, 5.5), to(-4.0, 7.6), to(-6.6, 8.2), to(-4.8, 6.0)], 'a')
    if flip:
        mirror(cv)
    return cv, [('w', 'wall', 'Wall', GREEN, True), role('p', 'palette', 'Palette', BROWN), role('a', 'red', 'Red paint', PINK),
                role('v', 'violet', 'Violet paint', PINK), role('b', 'blue', 'Blue paint', BLUE), role('c', 'cyan', 'Light blue paint', BLUE),
                role('g', 'green', 'Green paint and hills', GREEN), role('h', 'brush', 'Brush handle and frames', BLUE),
                role('y', 'ferrule', 'Ferrule', BROWN), role('e', 'canvas', 'Canvas and jars', BLUE), role('f', 'floor', 'Floor and easel', BROWN)], ['things', 'hobby']


def book_stack(w, h, r):
    cv, s, cx, big = start(w, h, 'w')
    flip, lamp, apple = r.random() < 0.5, r.random() < 0.3, r.random() < 0.3
    fy = h - 4
    box(cv, 0, fy, w, h, 't')
    if lamp:
        lx = w - 3.0
        oval(cv, lx, fy - 0.5, 2.4, 0.9, 'l')
        bar(cv, lx, fy - 0.6, lx - 1.0, fy - 9.0, 0.8, 'l')
        bar(cv, lx - 1.0, fy - 9.0, lx - 4.4, fy - 12.0, 0.8, 'l')
        poly(cv, [(lx - 6.8, fy - 10.6), (lx - 2.8, fy - 13.8), (lx - 1.4, fy - 11.6), (lx - 4.6, fy - 8.6)], 'l')
    else:
        wx = r.choice((1.5, w - 8.5))
        rbox(cv, wx, 1.5, wx + 7, 9.5, 0.5, 'l')
        box(cv, wx + 1, 2.5, wx + 6, 8.5, 'w')
        box(cv, wx + 3, 2.5, wx + 4, 8.5, 'l')
        box(cv, wx + 1, 5, wx + 6, 6, 'l')
    y = fy
    x0 = cx - 1.5
    books = [('a', 3), ('b', 2), ('c', 3), ('d', 2), ('a', 2)]
    widths = [17, 15, 16, 13, 14]
    r.shuffle(widths)
    top_book = None
    for k, ((c, t), bw) in enumerate(zip(books, widths)):
        bx = x0 - bw / 2 + r.uniform(-1.4, 1.4)
        if k in (1, 3):
            box(cv, bx, y - t, bx + bw, y - 0.1, c)
            box(cv, bx + 0.6, y - t + 0.9, bx + bw, y - 1.0, 'e')
        else:
            box(cv, bx, y - t, bx + bw, y - 0.1, c)
            for sx in (bx + 1.5, bx + bw - 2.5):
                box(cv, sx, y - t, sx + 0.9, y - 0.1, 'k')
            if t == 3:
                box(cv, bx + 4.5, y - 2.0, bx + bw - 4.5, y - 1.1, 'k')
        top_book = (bx, bx + bw, y - t)
        y -= t
    tx = (top_book[0] + top_book[1]) / 2 + r.uniform(-2.5, 2.5)
    if apple:
        disc(cv, tx, y - 2.4, 2.4, 'a')
        seg(cv, tx, y - 4.6, tx + 0.4, y - 5.8, 'k', 0.4)
        lens(cv, tx + 0.4, y - 5.2, tx + 2.8, y - 6.0, 1.2, 'b')
    else:
        rbox(cv, tx - 2.0, y - 4.2, tx + 2.0, y, 0.6, 'c')
        ring(cv, tx + 2.6, y - 2.2, 1.5, 0.6, 'c')
        for k in (-0.8, 0.8):
            path(cv, [(tx + k, y - 5.0), (tx + k + 0.6, y - 6.2), (tx + k, y - 7.4)], 'e', 0.4)
    if flip:
        mirror(cv)
    return cv, [('w', 'wall', 'Wall', BLUE, True), role('a', 'red', 'Red books' + (' and apple' if apple else ''), PINK),
                role('b', 'green', 'Green books' + (' and leaf' if apple else ''), GREEN), role('c', 'violet', 'Violet book' + ('' if apple else ' and cup'), PINK),
                role('d', 'moss', 'Moss book', GREEN), role('e', 'pages', 'Pages' + ('' if apple else ' and steam'), BLUE),
                role('k', 'band', 'Spine bands and stem', BROWN), role('l', 'lamp', 'Lamp' if lamp else 'Window frame', GREEN),
                role('t', 'desk', 'Desk', BROWN)], ['things', 'cozy']


def pencil_cup(w, h, r):
    cv, s, cx, big = start(w, h, 'w')
    flip, stripes, extra = r.random() < 0.5, r.random() < 0.5, int(r.random() * 3)
    fy = h - 4
    box(cv, 0, fy, w, h, 't')
    pencils = [(-16, 13.5, 'a'), (-5, 15.0, 'y'), (6, 14.0, 'v'), (17, 12.5, 'g')]
    for deg, L, c in pencils:
        to, back = turn(cx + deg * 0.12, fy - 3.0, deg)
        fill(cv, c, lambda px, py, back=back, L=L: -0.95 <= back(px, py)[0] <= 0.95 and -(L - 2.6) <= back(px, py)[1] <= 0)
        fill(cv, 'o', lambda px, py, back=back, L=L: -(L) <= back(px, py)[1] < -(L - 2.6) and
             abs(back(px, py)[0]) <= 0.95 * (L + back(px, py)[1]) / 2.6)
        fill(cv, c, lambda px, py, back=back, L=L: -(L) <= back(px, py)[1] < -(L - 1.0) and
             abs(back(px, py)[0]) <= 0.95 * (L + back(px, py)[1]) / 2.6)
    if extra == 0:
        to, back = turn(cx + 1.0, fy - 3.0, 1.0)
        fill(cv, 'r', lambda px, py: abs(back(px, py)[0]) <= 0.9 and -17.0 <= back(px, py)[1] <= 0)
        for k in range(3, 17, 2):
            fill(cv, 'k', lambda px, py, k=k: -0.9 <= back(px, py)[0] <= 0.1 and abs(back(px, py)[1] + k) <= 0.4, only='r')
    elif extra == 1:
        for side in (-1, 1):
            ring(cv, cx + side * 1.8, fy - 17.5, 1.7, 0.7, 'r')
        seg(cv, cx - 0.4, fy - 15.8, cx, fy - 10.0, 'k', 0.45)
        seg(cv, cx + 0.4, fy - 15.8, cx, fy - 10.0, 'k', 0.45)
    else:
        bar(cv, cx + 0.5, fy - 3.0, cx + 0.5, fy - 14.0, 0.9, 'r')
        box(cv, cx, fy - 15.6, cx + 1.0, fy - 14.0, 'k')
        poly(cv, [(cx - 0.4, fy - 15.6), (cx + 1.4, fy - 15.6), (cx + 1.0, fy - 18.0), (cx + 0.5, fy - 18.6), (cx, fy - 18.0)], 'a')
    rbox(cv, cx - 5.0, fy - 9.0, cx + 5.0, fy, 0.8, 'c')
    box(cv, cx - 5.4, fy - 9.6, cx + 5.4, fy - 8.6, 'k')
    if stripes:
        for y in (fy - 6.6, fy - 3.6):
            box(cv, cx - 5.0, y, cx + 5.0, y + 1.0, 'p', only='c')
    else:
        heart(cv, cx, fy - 4.4, 2.2, 'p')
    px = r.choice((2.5, w - 2.5))
    rbox(cv, px - 2.2, fy - 1.6, px + 2.2, fy, 0.4, 'p')
    if r.random() < 0.5:
        wx = r.choice((1.5, w - 7.5))
        rbox(cv, wx, 1.5, wx + 6, 7.5, 0.4, 'r')
        box(cv, wx + 1, 2.5, wx + 5, 6.5, 'w')
    if flip:
        mirror(cv)
    return cv, [('w', 'wall', 'Wall', BLUE, True), role('c', 'cup', 'Cup', GREEN), role('p', 'pattern', 'Cup pattern and eraser', PINK),
                role('a', 'red', 'Red pencil', PINK), role('v', 'violet', 'Violet pencil', PINK), role('y', 'yellow', 'Yellow pencil', BROWN),
                role('g', 'green', 'Green pencil', GREEN), role('o', 'wood', 'Sharpened wood', BROWN),
                role('r', 'tool', ['Ruler', 'Scissors', 'Brush'][extra] + ' and frame', BLUE), role('k', 'marks', 'Rim and marks', BROWN),
                role('t', 'desk', 'Desk', BROWN)], ['things', 'school']


def typewriter(w, h, r):
    cv, s, cx, big = start(w, h, 'w')
    scene, flip = int(r.random() * 3), r.random() < 0.5
    fy = h - 4
    box(cv, 0, fy, w, h, 't')
    if scene == 0:
        lx = 2.5
        oval(cv, lx, fy - 0.5, 2.2, 0.9, 'l')
        bar(cv, lx, fy - 0.6, lx + 0.6, fy - 9.0, 0.8, 'l')
        poly(cv, [(lx - 1.6, fy - 8.4), (lx + 3.2, fy - 8.4), (lx + 2.2, fy - 11.4), (lx - 0.6, fy - 11.4)], 'l')
    elif scene == 1:
        rbox(cv, w - 4.6, fy - 4.4, w - 1.2, fy, 0.5, 'l')
        ring(cv, w - 5.2, fy - 2.4, 1.4, 0.5, 'l')
        for k in (-0.8, 0.8):
            path(cv, [(w - 2.9 + k, fy - 5.0), (w - 2.3 + k, fy - 6.2), (w - 2.9 + k, fy - 7.4)], 'x', 0.4)
    else:
        for k in range(3):
            box(cv, 0.5, fy - 2.0 * (k + 1), 4.0 + k * 0.4, fy - 2.0 * k - 0.1, 'l' if k % 2 == 0 else 'x')
    rbox(cv, cx - 5.0, 2.0, cx + 5.0, 11.0, 0.3, 'p')
    for k in range(3):
        box(cv, cx - 3.8, 3.5 + k * 1.8, cx + 3.8 - (2.5 if k == 2 else 0), 4.3 + k * 1.8, 'x')
    poly(cv, [(4.6, 12.0), (w - 4.6, 12.0), (w - 1.6, fy - 0.6), (w - 2.4, fy), (2.4, fy), (1.6, fy - 0.6)], 'b')
    rbox(cv, 2.6, 9.6, w - 2.6, 12.2, 0.8, 'r')
    for x in (2.2, w - 2.2):
        disc(cv, x, 10.9, 1.4, 'r')
    path(cv, [(3.4, 10.0), (2.2, 7.8), (0.8, 7.6)], 'r', 0.45)
    fill(cv, 'r', lambda px, py: py >= 12.0 and ((px - cx) / 5.0) ** 2 + ((py - 12.0) / 2.6) ** 2 <= 1)
    for k, y in enumerate((16.0, 18.0, 20.0)):
        for x in range(4 + k % 2, w - 4, 2):
            cv.put(x, int(y), 'k')
    rbox(cv, 6.5, fy - 2.4, w - 6.5, fy - 1.2, 0.4, 'r')
    if flip:
        mirror(cv)
    return cv, [('w', 'wall', 'Wall', PINK, True), role('b', 'body', 'Typewriter', GREEN), role('p', 'paper', 'Paper', BLUE),
                role('x', 'text', 'Typed lines, steam and book', PINK), role('r', 'roller', 'Roller, lever and space bar', BROWN),
                role('k', 'keys', 'Keys', BLUE), role('l', 'detail', ['Lamp', 'Cup', 'Books'][scene], GREEN), role('t', 'desk', 'Desk', BROWN)], ['things', 'desk']


def bell(w, h, r):
    first, mode = r.random(), int(r.random() * 3)
    if mode == 0:
        cv, s, cx, big = start(w, h, 's')
    else:
        cv, s, cx, big = start(w, h, 'w')

    def shape(u, v, W, H):
        """A bell `W` across at the lip and `H` high, its crown at v = 0, in local cells."""
        if not 0 <= v <= H:
            return False
        t = v / H
        hw = W / 2 * (0.42 + 0.14 * t + 0.44 * t ** 5)
        if t < 0.14:
            hw *= math.sqrt(max(0.0, 1 - ((0.14 - t) / 0.14) ** 2))
        return abs(u) <= hw

    def one(bx, by, W, H, deg, body, band):
        to, back = turn(bx, by, deg)
        fill(cv, body, lambda px, py: shape(*back(px, py), W, H))
        fill(cv, band, lambda px, py: shape(*back(px, py), W, H) and (back(px, py)[1] >= H - 1.4 or abs(back(px, py)[1] - H * 0.36) <= 0.5))
        disc(cv, *to(0, H + 0.9), 1.3, 'c')
        return to
    if mode == 0:
        hills(cv, 'g', h - 4, 0.8, w * 1.2, r.uniform(0, 6))
        cloud(cv, r.uniform(6, 16), h * 0.62, 1.6, 'o')
        for x0 in (0, w - 3.5):
            box(cv, x0, 7.0, x0 + 3.5, h, 't')
        fill(cv, 't', lambda px, py: py < 9.5 and math.hypot(px - cx, py - 9.5) >= 7.5 and py >= 3.0)
        poly(cv, [(-1.0, 3.4), (cx, -2.5), (w + 1.0, 3.4)], 'r')
        box(cv, 3.5, 4.4, w - 3.5, 5.6, 'w')
        for y in range(8, h, 3):
            box(cv, 0, y, 3.5, y + 0.6, 'k', only='t')
            box(cv, w - 3.5, y, w, y + 0.6, 'k', only='t')
        box(cv, 3.5, h - 2.0, w - 3.5, h, 'w')
        to = one(cx, 5.6, 11.0, 10.5, (first - 0.5) * 24, 'b', 'k')
        box(cv, cx - 1.0, 5.4, cx + 1.0, 6.4, 'w')
        roles = [sky(), role('t', 'tower', 'Bell tower', PINK), role('r', 'roof', 'Roof', PINK), role('k', 'band', 'Bands and brick lines', BROWN),
                 role('w', 'beam', 'Beam and ledge', GREEN), role('b', 'bell', 'Bell', BROWN), role('c', 'clapper', 'Clapper', BLUE),
                 role('o', 'cloud', 'Cloud', BLUE), role('g', 'hills', 'Hills', GREEN)]
        themes = ['things', 'town']
    elif mode == 1:
        fy = h - 4
        box(cv, 0, fy, w, h, 't')
        bx = cx + (first - 0.5) * 6
        deg = r.uniform(-16, 16)
        to = one(bx, fy - 12.8, 11.0, 10.6, deg, 'b', 'k')
        _, back = turn(bx, fy - 12.8, deg)
        fill(cv, 'h', lambda px, py: abs(back(px, py)[0]) <= 0.9 and -5.0 <= back(px, py)[1] <= 0.4)
        disc(cv, *to(0, -5.6), 1.6, 'h')
        for side in (-1, 1):
            for k in range(2):
                x = bx + side * (7.4 + k * 1.4)
                seg(cv, x, fy - 9.0 - k * 1.2, x + side * 0.9, fy - 11.4 - k * 1.2, 'z', 0.45)
        roles = [('w', 'wall', 'Wall', GREEN, True), role('b', 'bell', 'Bell', BROWN), role('k', 'band', 'Bands', PINK),
                 role('c', 'clapper', 'Clapper', BLUE), role('h', 'handle', 'Handle', PINK), role('z', 'ring', 'Ringing', BLUE),
                 role('t', 'table', 'Table', BROWN)]
        themes = ['things', 'school']
    else:
        fy = h - 3
        box(cv, 0, fy, w, h, 't')
        scatter(cv, 'x', 'w', 10, r, sep=3, area=(0, 0, w - 1, fy - 2))
        for side in (-1, 1):
            one(cx + side * 4.6, 7.4, 9.0, 9.0, -side * 16, 'b', 'k')
        for side in (-1, 1):
            poly(cv, [(cx, 5.4), (cx + side * 4.4, 3.0), (cx + side * 4.4, 8.0)], 'r')
            poly(cv, [(cx, 6.0), (cx + side * 1.4, 11.0), (cx + side * 3.2, 10.0)], 'r')
        disc(cv, cx, 5.6, 1.4, 'r')
        for side in (-1, 1):
            for k in range(2):
                lens(cv, cx + side * (2.0 + k * 0.6), 21.0 + k * 1.2, cx + side * (7.5 - k * 0.8), 19.6 + k * 2.8, 2.4, 'l')
        for dx in (-1.0, 1.0, 0.0):
            disc(cv, cx + dx, 21.0 - abs(dx), 1.05, 'r')
        roles = [('w', 'wall', 'Winter wall', BLUE, True), role('b', 'bell', 'Bells', BROWN), role('k', 'band', 'Bands', BROWN),
                 role('c', 'clapper', 'Clappers', BLUE), role('r', 'bow', 'Bow and berries', PINK), role('l', 'holly', 'Holly leaves', GREEN),
                 role('x', 'snow', 'Snowflakes', PINK), role('t', 'floor', 'Snowy sill', GREEN)]
        themes = ['things', 'winter']
    return cv, roles, themes

DAILY_THINGS = [tram, fire_truck, scooter, motorcycle, skateboard, sled, zeppelin, ufo, satellite, telescope, camera, radio,
                piano, violin, trumpet, harp, yo_yo, spinning_top, rocking_horse, jack_in_the_box, building_blocks, compass,
                hourglass, candle, magic_wand, wizard_hat, trophy, paint_palette, book_stack, pencil_cup, typewriter, bell]

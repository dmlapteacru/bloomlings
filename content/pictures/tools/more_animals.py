"""More animal subjects for the levels (more_subjects.py, the owner, 2026-10-07): friendly, generic animals in the style of
animal_subjects.py, each drawn with picture_kit and returning (canvas, roles, themes). The colors that shape an animal
(its body against the sky or the water, its eyes against its face) come from different color groups, so they stay apart
under any mapping. An animal stands on the ground, the sea floor or a branch that shares the board's entry, so its face
stays within the nesting depth targets. On the big boards (from 300 cells) they get eye whites, pupils and more scenery,
and each picture's random stream turns the animal round and moves its scenery.
"""
import math

from animal_subjects import eye, eyes, pine
from picture_kit import (BLUE, BROWN, GREEN, PINK, box, cloud, disc, ground_rows, hills, lens, oval, path, poly, rbox,
                         role, scatter, seg, sky, star, start)


def flip(cv):
    """Mirrors the drawing, so the animal faces the other way."""
    cv.g = [row[::-1] for row in cv.g]


def sun(cv, x, y, s, c):
    """A sun (or moon) of at least five cells, a cell clear of the edges, so the sky round it stays one region."""
    rad = s * 0.07 + 0.6
    disc(cv, min(max(x, rad + 1.0), cv.w - rad - 1.0), max(y, rad + 1.0), rad, c)


def alpaca(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.12)
    for mx, top in ((w * r.uniform(0.08, 0.28), h * 0.44), (w * r.uniform(0.66, 0.86), h * 0.52)):
        poly(cv, [(mx - w * 0.36, gtop + 0.5), (mx, top), (mx + w * 0.36, gtop + 0.5)], 'm')
        box(cv, 0, 0, w, top + h * 0.09, 'w', only='m')
    hills(cv, 'g', gtop, 0.4, w * 1.4, r.uniform(0, 6))
    for x in (w * 0.9,) + ((w * 0.06,) if big else ()):
        oval(cv, x, gtop - 0.3, s * 0.1, s * 0.07, 'v')
    sun(cv, w * 0.12, h * 0.1, s, 'u')
    ax, by = cx - w * 0.12, gtop - h * 0.22
    for dx in (-0.16, -0.07, 0.07, 0.15):
        box(cv, ax + dx * w - 0.5, by, ax + dx * w + 0.5, gtop + 0.4, 'a')
        cv.put(int(ax + dx * w), int(gtop) - 1, 'k')
    oval(cv, ax, by, w * 0.22, h * 0.08, 'a')
    for k in range(10):
        a = math.radians(k * 36)
        disc(cv, ax + math.cos(a) * w * 0.19, by + math.sin(a) * h * 0.065, s * 0.08 + 0.2, 'a')
    disc(cv, ax - w * 0.24, by - h * 0.05, s * 0.07 + 0.2, 'a')
    nx = ax + w * 0.19
    hx, hy = nx + s * 0.09, h * 0.25
    rbox(cv, nx - s * 0.09, hy, nx + s * 0.09, by, 0.8, 'a')
    oval(cv, hx, hy, s * 0.16, s * 0.11, 'a')
    oval(cv, hx + s * 0.16, hy + s * 0.04, s * 0.09, s * 0.08, 'a')
    for dx in (-0.1, 0.02):
        seg(cv, hx + dx * s, hy - s * 0.06, hx + dx * s - 0.3, hy - s * 0.19, 'a', 0.45)
    eye(cv, hx + s * 0.03, hy - s * 0.06, big, 'e', 'k')
    cv.put(int(hx + s * 0.23), int(hy + s * 0.02), 'k')
    box(cv, ax - w * 0.1, by - h * 0.1, ax + w * 0.08, by + h * 0.02, 'b', only='a')
    if big:
        box(cv, ax - w * 0.1, by - h * 0.03, ax + w * 0.08, by - h * 0.03 + 0.6, 'w', only='b')
    if r.random() < 0.5:
        flip(cv)
    return cv, [sky(), role('u', 'sun', 'Sun', BROWN), role('a', 'alpaca', 'Alpaca', BROWN), role('k', 'eye', 'Eye, nose and hooves', PINK),
                role('e', 'eye_white', 'Eye white', GREEN), role('b', 'blanket', 'Blanket', PINK),
                role('m', 'mountains', 'Mountains', PINK), role('w', 'snow', 'Snowy peaks and blanket stripe', BLUE),
                role('g', 'grass', 'Grass', GREEN), role('v', 'shrubs', 'Shrubs', GREEN)], ['animals', 'mountains']


def bat(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.1)
    hills(cv, 'g', gtop, 0.6, w * 0.9, r.uniform(0, 6))
    for x in (w * 0.12, w * 0.88) if not big else (w * 0.1, w * 0.62, w * 0.9):
        oval(cv, x, gtop - 0.2, s * 0.09, s * 0.07, 'v')
    mx = w * r.choice((0.16, 0.84))
    disc(cv, mx, h * 0.12, s * 0.12, 'm')
    bx, by = int(cx) + 0.5, h * r.uniform(0.46, 0.52)
    edge = [(0.43, -0.13), (0.4, 0.04), (0.33, -0.01), (0.27, 0.08), (0.2, 0.03), (0.13, 0.11), (0.05, 0.06)]
    for side in (-1, 1):
        poly(cv, [(bx, by - h * 0.08), (bx + side * w * 0.2, by - h * 0.16)] + [(bx + side * w * a, by + h * b) for a, b in edge], 'w')
    oval(cv, bx, by + h * 0.02, s * 0.12, s * 0.16, 'b')
    hy = by - s * 0.2
    disc(cv, bx, hy, s * 0.14, 'b')
    for side in (-1, 1):
        poly(cv, [(bx + side * s * 0.02, hy - s * 0.08), (bx + side * s * 0.15, hy - s * 0.3), (bx + side * s * 0.15, hy - s * 0.02)], 'b')
        cv.put(int(bx + side * s * 0.05), int(by + s * 0.18), 'k')
    eyes(cv, bx, hy - s * 0.02, 1.0, big, 'e', 'k')
    if big:
        for side in (-1, 1):
            cv.put(int(bx + side * 0.8), int(hy + s * 0.1), 'e')
        mx2 = w - mx
        for side in (-1, 1):
            lens(cv, mx2, h * 0.22, mx2 + side * s * 0.14, h * 0.19, s * 0.06 + 0.4, 'w')
        disc(cv, mx2, h * 0.22, 0.8, 'b')
    scatter(cv, 'x', 's', 5 + (w * h) // 70, r, sep=3, area=(0, 0, w - 1, h * 0.34))
    return cv, [('s', 'night', 'Night sky', BLUE, True), role('w', 'wings', 'Wings', PINK), role('b', 'bat', 'Bat', PINK),
                role('k', 'eye', 'Eyes and feet', BROWN), role('e', 'eye_white', 'Eye whites and fangs', BLUE),
                role('m', 'moon', 'Moon', BROWN), role('x', 'stars', 'Stars', BROWN),
                role('g', 'hills', 'Hills', GREEN), role('v', 'bushes', 'Bushes', GREEN)], ['animals', 'night']


def beaver(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.14)
    hills(cv, 'g', gtop, 0.4, w * 1.4, r.uniform(0, 6))
    tx = w * 0.13
    disc(cv, tx + 0.4, h * 0.2, s * 0.2, 'l')
    box(cv, tx - 1.3, h * 0.28, tx + 1.3, gtop + 0.5, 'o')
    for y0, y1 in ((h * 0.58, h * 0.65), (h * 0.71, h * 0.65)):
        for side in (-1, 1):
            poly(cv, [(tx + side * 1.4, y0), (tx + side * 1.4, y1 + (y1 - y0) * 0.5), (tx + side * 0.4, y1)], 's')
    bx = float(int(w * 0.52))
    oval(cv, bx + w * 0.26, gtop - h * 0.07, w * 0.17, h * 0.065, 'p')
    if big:
        for k, x in enumerate(range(int(bx + w * 0.16), int(bx + w * 0.42))):
            cv.put(x, int(gtop - h * 0.07) - 1 + k % 2, 'm') if cv.g[int(gtop - h * 0.07) - 1 + k % 2][x] == 'p' else None
    oval(cv, bx, gtop - h * 0.17, w * 0.2, h * 0.18, 'b')
    oval(cv, bx, gtop - h * 0.14, w * 0.11, h * 0.11, 'm')
    for side in (-1, 1):
        oval(cv, bx + side * w * 0.12, gtop - 0.4, w * 0.08, 1.1, 'b')
        disc(cv, bx + side * w * 0.09, gtop - h * 0.27, 1.0, 'b')
        for k in (0, 1):
            cv.put(int(bx + side * w * 0.12 + side * (k + 0.5)), int(gtop) - 1, 't')
    hy = h * 0.37
    for side in (-1, 1):
        disc(cv, bx + side * s * 0.17, hy - s * 0.15, 0.75, 'b')
    disc(cv, bx, hy, s * 0.2, 'b')
    for side in (-1, 1):
        disc(cv, bx + side * s * 0.07, hy + s * 0.08, s * 0.07 + 0.2, 'm')
    eyes(cv, bx, hy - s * 0.15, 1.5, big, 'e', 'k')
    box(cv, bx - 1.0, hy + s * 0.04 - 0.5, bx + 1.0, hy + s * 0.04 + 0.4, 'k')
    ty = int(hy + s * 0.04) + 1.5
    box(cv, bx - 1.0, ty, bx + 1.0, ty + (1.0 if not big else 2.0), 't')
    for x in (tx - 2.0, tx + 2.4):
        box(cv, x - 0.5, gtop - 0.5, x + 0.5, gtop - 0.5, 'o')
    sun(cv, w * 0.86, h * 0.1, s, 'u')
    if r.random() < 0.5:
        flip(cv)
    return cv, [sky(), role('u', 'sun', 'Sun', BROWN), role('b', 'beaver', 'Beaver', BROWN), role('m', 'muzzle', 'Cheeks and belly', BROWN),
                role('o', 'tree', 'Gnawed tree', BROWN), role('k', 'eye', 'Eyes and nose', PINK), role('p', 'tail', 'Tail', PINK),
                role('t', 'teeth', 'Teeth and claws', BLUE), role('e', 'eye_white', 'Eye whites', GREEN),
                role('l', 'leaves', 'Leaves', GREEN), role('g', 'grass', 'Grass', GREEN)], ['animals', 'forest']


def otter(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    sea = h * 0.6
    hills(cv, 'w', sea, 0.35, w * 0.5, r.uniform(0, 6))
    for x in (w * 0.24, w * 0.76) if not big else (w * 0.12, w * 0.46, w * 0.84):
        path(cv, [(x, h), (x + 0.8, h * 0.9), (x - 0.4, h * 0.8), (x + 0.5, h * 0.72)], 'l', 0.55)
    ox, oy = cx + w * 0.08, sea - 0.5
    lens(cv, ox + w * 0.26, oy + 0.2, ox + w * 0.46, oy + h * 0.04, s * 0.1, 'o')
    oval(cv, ox, oy, w * 0.28, h * 0.08, 'o')
    box(cv, ox - w * 0.16, oy - h * 0.09, ox + w * 0.12, oy - h * 0.025, 'f', only='o')
    for dx in (0.18, 0.25):
        oval(cv, ox + w * dx, oy - h * 0.07, 0.7, h * 0.05, 'o')
    hx, hy = int(ox - w * 0.3) + 0.5, oy - h * 0.12
    if big:
        for side in (-1, 1):
            disc(cv, hx + side * s * 0.15, hy - s * 0.14, 0.8, 'o')
    oval(cv, hx, hy, s * 0.2, s * 0.18, 'o')
    oval(cv, hx, hy + s * 0.09, s * 0.14, s * 0.075, 'f')
    eyes(cv, hx, hy - s * 0.11, 1.0 if not big else 1.6, big, None, 'k')
    box(cv, hx - 0.5, hy + s * 0.05, hx + 0.5, hy + s * 0.05 + 0.6, 'k')
    if big:
        for side in (-1, 1):
            seg(cv, hx + side * s * 0.16, hy + s * 0.1, hx + side * s * 0.27, hy + s * 0.08, 'k', 0.4)
    px = hx + s * 0.32
    oval(cv, px, oy - h * 0.08, s * 0.1, s * 0.07, 'x')
    for side in (-1, 1):
        disc(cv, px + side * s * 0.09, oy - h * 0.05, 0.8, 'o')
    sun(cv, w * 0.84, h * 0.12, s, 'u')
    if big:
        cloud(cv, w * 0.24, h * 0.14, s * 0.05 + 0.4, 'c')
    if r.random() < 0.5:
        flip(cv)
    return cv, [sky(), role('w', 'sea', 'Sea', BLUE), role('u', 'sun', 'Sun', BROWN), role('o', 'otter', 'Otter', BROWN),
                role('f', 'face', 'Face and belly', BROWN), role('k', 'eye', 'Eyes and nose', PINK), role('x', 'urchin', 'Sea urchin', PINK), role('l', 'kelp', 'Kelp', GREEN), role('c', 'cloud', 'Cloud', BLUE)], ['animals', 'sea']


def polar_bear(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    sea = h * 0.62
    hills(cv, 'w', sea, 0.3, w * 0.5, r.uniform(0, 6))
    itop = h * 0.75
    poly(cv, [(w * 0.08, itop), (w * 0.92, itop), (w * 1.04, h + 1), (-w * 0.04, h + 1)], 'i')
    ix = w * 0.14
    poly(cv, [(ix - w * 0.16, sea + 0.6), (ix - w * 0.04, sea - h * 0.14), (ix + w * 0.04, sea - h * 0.1), (ix + w * 0.16, sea + 0.6)], 'a')
    bx, by = w * 0.36, itop - h * 0.26
    for dx in (-0.19, -0.08, 0.08, 0.18):
        box(cv, bx + dx * w - 0.6, by, bx + dx * w + 0.6, itop + 0.5, 'b')
        cv.put(int(bx + dx * w), int(itop) - 1, 'k')
    oval(cv, bx, by, w * 0.27, h * 0.125, 'b')
    disc(cv, bx + w * 0.14, by - h * 0.06, s * 0.14, 'b')
    disc(cv, bx - w * 0.27, by - h * 0.05, 0.8, 'b')
    hx, hy = bx + w * 0.38, by + h * 0.02
    path(cv, [(bx + w * 0.2, by - h * 0.04), (hx - s * 0.08, hy - s * 0.02)], 'b', s * 0.1)
    oval(cv, hx, hy, s * 0.13, s * 0.1, 'b')
    oval(cv, hx + s * 0.13, hy + s * 0.03, s * 0.08, s * 0.06, 'b')
    disc(cv, hx - s * 0.06, hy - s * 0.1, 0.75, 'b')
    if big:
        eye(cv, hx + s * 0.02, hy - s * 0.06, big, None, 'k')
        path(cv, [(hx + s * 0.08, hy + s * 0.08), (hx + s * 0.16, hy + s * 0.07)], 'k', 0.4)
    else:
        cv.put(int(hx + s * 0.03), int(hy - s * 0.03), 'k')
    cv.put(int(hx + s * 0.2), int(hy + s * 0.02), 'k')
    fx = w * 0.8
    oval(cv, fx, itop + h * 0.12, w * 0.08, 1.0, 'f')
    poly(cv, [(fx - w * 0.06, itop + h * 0.12), (fx - w * 0.13, itop + h * 0.08), (fx - w * 0.13, itop + h * 0.16)], 'f')
    cv.put(int(fx + w * 0.03), int(itop + h * 0.12), 'k')
    sun(cv, w * 0.84, h * 0.13, s, 'u')
    scatter(cv, 'x', 's', 4 + (w * h) // 60, r, sep=3, area=(0, 0, w - 1, by - h * 0.12))
    if r.random() < 0.5:
        flip(cv)
    return cv, [sky(), role('b', 'bear', 'Polar bear', PINK), role('x', 'snow', 'Snowflakes', PINK), role('k', 'eye', 'Eye, nose and claws', BROWN),
                role('w', 'sea', 'Sea', BLUE), role('i', 'ice', 'Ice floe', GREEN), role('a', 'iceberg', 'Iceberg', GREEN),
                role('f', 'fish', 'Fish', BROWN), role('u', 'sun', 'Sun', BROWN)], ['animals', 'winter']


def moose(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.12)
    hills(cv, 'g', gtop, 0.4, w * 1.4, r.uniform(0, 6))
    for x in (w * 0.08, w * 0.92) + ((w * 0.2,) if big else ()):
        pine(cv, x, gtop, h * r.uniform(0.36, 0.44), s * 0.22, 't')
    if big:
        oval(cv, w * 0.82, gtop + 1.2, w * 0.18, 1.4, 'w')
    sun(cv, w * r.choice((0.12, 0.88)), h * 0.42, s, 'u')
    mx = int(cx) + 0.5
    oval(cv, mx, gtop - h * 0.16, w * 0.25, h * 0.11, 'm')
    for dx in (-0.19, -0.08, 0.08, 0.19):
        box(cv, mx + dx * w - 0.6, gtop - h * 0.17, mx + dx * w + 0.6, gtop + 0.4, 'm')
    hy = h * 0.38
    oval(cv, mx, hy, s * 0.13, s * 0.15, 'm')
    oval(cv, mx, hy + s * 0.16, s * 0.17, s * 0.1, 'm')
    lens(cv, mx, hy + s * 0.22, mx, hy + s * 0.38, s * 0.08, 'm')
    for side in (-1, 1):
        lens(cv, mx + side * s * 0.08, hy - s * 0.08, mx + side * s * 0.25, hy - s * 0.04, s * 0.09, 'm')
        lens(cv, mx + side * s * 0.08, hy - s * 0.14, mx + side * s * 0.42, hy - s * 0.3, s * 0.18, 'a')
        for k in range(3):
            x0 = mx + side * s * (0.18 + k * 0.1)
            seg(cv, x0, hy - s * (0.24 + k * 0.04), x0 + side * s * 0.02, hy - s * (0.36 + k * 0.05), 'a', 0.5)
    eyes(cv, mx, hy - s * 0.05, 1.0 if not big else 2.0, big, 'e', 'k')
    for side in (-1, 1):
        cv.put(int(mx + side * 1.0), int(hy + s * 0.19), 'k')
    return cv, [sky(), role('m', 'moose', 'Moose', BROWN), role('a', 'antlers', 'Antlers', BROWN), role('k', 'eye', 'Eyes and nostrils', PINK),
                role('e', 'eye_white', 'Eye whites', BLUE), role('t', 'pine', 'Pines', GREEN), role('g', 'grass', 'Grass', GREEN),
                role('w', 'lake', 'Lake', BLUE), role('u', 'sun', 'Sun', PINK)], ['animals', 'forest']


def ostrich(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.12)
    hills(cv, 'g', gtop, 0.4, w * 1.5, r.uniform(0, 6))
    tx = w * 0.86
    box(cv, tx - 0.5, h * 0.4, tx + 0.5, gtop + 0.5, 't')
    seg(cv, tx, h * 0.5, tx - w * 0.08, h * 0.4, 't', 0.45)
    oval(cv, tx - w * 0.04, h * 0.36, w * 0.12, h * 0.06, 'a')
    sun(cv, w * 0.12, h * 0.1, s, 'u')
    ox, oy = cx - w * 0.12, h * 0.48
    for k, side in enumerate((-1, 1)):
        knee = (ox + side * w * 0.07 + w * 0.05, oy + h * 0.2)
        path(cv, [(ox + side * w * 0.07, oy + h * 0.06), knee, (knee[0] - w * 0.03, gtop - 0.5)], 'n', 0.5)
        seg(cv, knee[0] - w * 0.03, gtop - 0.5, knee[0] - w * 0.03 + 1.6, gtop - 0.5, 'k', 0.5)
    oval(cv, ox, oy, w * 0.24, h * 0.11, 'b')
    for a in (0.0, 0.5):
        lens(cv, ox - w * 0.16, oy - h * 0.02, ox - w * 0.33, oy - h * (0.1 + a * 0.08), s * 0.14, 'p')
    lens(cv, ox - w * 0.1, oy + h * 0.03, ox + w * 0.1, oy + h * 0.01, s * 0.1, 'p')
    hx, hy = ox + w * 0.24, h * 0.16
    path(cv, [(ox + w * 0.15, oy - h * 0.04), (ox + w * 0.22, oy - h * 0.18), (hx - 0.3, hy + 1)], 'n', 0.55)
    oval(cv, hx, hy, s * 0.1, s * 0.075, 'n')
    poly(cv, [(hx + s * 0.06, hy - 0.2), (hx + s * 0.26, hy + 0.5), (hx + s * 0.06, hy + 1.1)], 'k')
    if big:
        eye(cv, hx + s * 0.01, hy - s * 0.04, big, 'e', 'k', rad=0.8)
    else:
        cv.put(int(hx + s * 0.02), int(hy), 'k')
    if r.random() < 0.5:
        flip(cv)
    return cv, [sky(), role('u', 'sun', 'Sun', BROWN), role('b', 'ostrich', 'Ostrich', BROWN), role('p', 'plumes', 'Plumes', PINK),
                role('n', 'neck', 'Neck and legs', PINK), role('k', 'beak', 'Beak, eye and toes', BROWN), role('e', 'eye_white', 'Eye white', BLUE),
                role('t', 'trunk', 'Acacia trunk', BROWN), role('a', 'acacia', 'Acacia', GREEN), role('g', 'grass', 'Savanna grass', GREEN)], ['animals', 'savanna']


def pelican(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    sea = h * 0.68
    hills(cv, 'w', sea, 0.3, w * 0.5, r.uniform(0, 6))
    x0 = int(w / 2) + 0.5
    box(cv, x0 - 1.0, h * 0.64, x0 + 1.0, h, 'o')
    if big:
        for y in (h * 0.78, h * 0.9):
            box(cv, x0 - 1.0, y, x0 + 1.0, y + 0.5, 'k', only='o')
    bx, by = x0 - w * 0.1, h * 0.53
    poly(cv, [(bx - w * 0.18, by - h * 0.02), (bx - w * 0.34, by + h * 0.04), (bx - w * 0.18, by + h * 0.07)], 'b')
    oval(cv, bx, by, w * 0.22, h * 0.11, 'b')
    lens(cv, bx - w * 0.2, by - h * 0.01, bx + w * 0.12, by - h * 0.04, s * 0.14, 'v')
    lens(cv, bx - w * 0.24, by + h * 0.01, bx - w * 0.12, by - h * 0.01, s * 0.07, 'k')
    for dx in (-0.6, 1.2):
        box(cv, x0 + dx - 0.5, h * 0.62, x0 + dx + 0.4, h * 0.65, 'y')
    hx, hy = bx + w * 0.14, h * 0.25
    path(cv, [(bx + w * 0.14, by - h * 0.04), (bx + w * 0.2, h * 0.38), (hx, hy + 1)], 'b', 0.8 if not big else 1.0)
    disc(cv, hx, hy, s * 0.11, 'b')
    tip = (hx + w * 0.38, hy + h * 0.08)
    seg(cv, hx + s * 0.06, hy - 0.2, tip[0], tip[1], 'y', 0.5)
    poly(cv, [(hx + s * 0.04, hy + 0.4), tip, (hx + w * 0.24, hy + h * 0.16), (hx + w * 0.08, hy + h * 0.16), (hx + s * 0.04, hy + h * 0.1)], 'y')
    eye(cv, hx + s * 0.01, hy - s * 0.05, big, 'e', 'k', rad=0.8) if big else cv.put(int(hx + s * 0.02), int(hy - s * 0.03), 'k')
    fx = w * 0.82
    oval(cv, fx, h * 0.84, w * 0.08, 1.0, 'f')
    poly(cv, [(fx - w * 0.06, h * 0.84), (fx - w * 0.13, h * 0.8), (fx - w * 0.13, h * 0.88)], 'f')
    if big:
        cloud(cv, w * 0.24, h * 0.1, s * 0.05 + 0.4, 'c')
    if r.random() < 0.5:
        flip(cv)
    return cv, [sky(), role('w', 'sea', 'Sea', BLUE), role('b', 'pelican', 'Pelican', PINK), role('v', 'wing', 'Wing', PINK),
                role('y', 'beak', 'Beak and feet', BROWN), role('o', 'post', 'Post', BROWN), role('k', 'eye', 'Eye and wing tips', BROWN),
                role('e', 'eye_white', 'Eye white', GREEN), role('f', 'fish', 'Fish', GREEN), role('c', 'cloud', 'Cloud', BLUE)], ['animals', 'sea']


def puffin(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    sea = h * 0.56
    hills(cv, 'w', sea, 0.3, w * 0.5, r.uniform(0, 6))
    gtop = h * 0.78
    hills(cv, 'g', gtop, 0.6, w * 1.2, r.uniform(0, 6))
    rbox(cv, w * 0.74, gtop - h * 0.1, w * 1.1, h * 0.9, 1.5, 'c')
    for x in (w * 0.1, w * 0.62) + ((w * 0.36,) if big else ()):
        disc(cv, x, gtop + h * 0.08, 0.9, 'k')
    sun(cv, w * 0.82, h * 0.12, s, 'u')
    px = int(cx) + 0.5 - w * 0.08
    oval(cv, px, gtop - h * 0.15, w * 0.17, h * 0.17, 'p')
    oval(cv, px + w * 0.05, gtop - h * 0.13, w * 0.09, h * 0.11, 'f')
    hx, hy = px + w * 0.03, gtop - h * 0.38
    disc(cv, hx, hy, s * 0.16, 'p')
    oval(cv, hx + s * 0.05, hy + s * 0.02, s * 0.1, s * 0.09, 'f')
    poly(cv, [(hx + s * 0.12, hy - s * 0.09), (hx + s * 0.22, hy - s * 0.06), (hx + s * 0.36, hy + s * 0.05),
              (hx + s * 0.2, hy + s * 0.12), (hx + s * 0.12, hy + s * 0.1)], 'y')
    if big:
        seg(cv, hx + s * 0.2, hy - s * 0.05, hx + s * 0.2, hy + s * 0.1, 'u', 0.4)
    eye(cv, hx + s * 0.06, hy - s * 0.04, big, None, 'k')
    for dx in (-0.06, 0.08):
        box(cv, px + dx * w - 0.8, gtop - 0.6, px + dx * w + 0.8, gtop + 0.4, 'y')
    if r.random() < 0.5:
        flip(cv)
    return cv, [sky(), role('w', 'sea', 'Sea', BLUE), role('p', 'puffin', 'Puffin', PINK), role('f', 'face', 'Face and belly', BLUE),
                role('k', 'eye', 'Eye and sea pinks', PINK), role('y', 'beak', 'Beak and feet', BROWN), role('c', 'rock', 'Rock', BROWN),
                role('u', 'sun', 'Sun and beak stripe', BROWN), role('g', 'grass', 'Cliff grass', GREEN)], ['animals', 'sea']


def hummingbird(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    sx = w * 0.86
    path(cv, [(sx + 0.6, h + 1), (sx - 0.4, h * 0.72), (sx + 0.4, h * 0.52), (sx - 0.3, h * 0.36)], 'l', 0.55)
    for y, side in ((0.82, -1), (0.62, 1)) + (((0.48, -1),) if big else ()):
        lens(cv, sx, h * y, sx + side * s * 0.22, h * y - s * 0.1, s * 0.1, 'l')
    fx, fy = sx - 0.3, h * 0.34
    poly(cv, [(fx + 0.5, fy + 0.6), (fx - w * 0.12, fy - h * 0.1), (fx - w * 0.17, fy - h * 0.12), (fx - w * 0.15, fy + h * 0.02),
              (fx - w * 0.18, fy + h * 0.1), (fx - w * 0.1, fy + h * 0.07)], 'f')
    if big:
        for k in range(3):
            disc(cv, w * (0.1 + k * 0.08), h * (0.92 - (k % 2) * 0.04), 1.0, 'f')
        lens(cv, w * 0.06, h, w * 0.18, h * 0.84, s * 0.1, 'l')
    bx, by = w * 0.22, h * 0.5
    hx, hy = bx + w * 0.1, by - h * 0.12
    seg(cv, hx + s * 0.08, hy + 0.2, fx - w * 0.15, fy - 0.2, 'k', 0.45)
    lens(cv, bx - w * 0.12, by + h * 0.18, hx, hy + s * 0.04, s * 0.2, 'b')
    for side in (-1, 1):
        lens(cv, bx - w * 0.1, by + h * 0.15, bx - w * 0.14 + side * w * 0.05, by + h * 0.3, s * 0.07, 'b')
    disc(cv, hx, hy, s * 0.11, 'b')
    oval(cv, hx + s * 0.03, hy + s * 0.1, s * 0.07, s * 0.05, 'r')
    lens(cv, bx + w * 0.02, by - h * 0.06, bx - w * 0.12, by - h * 0.34, s * 0.14, 'w')
    lens(cv, bx + w * 0.06, by - h * 0.06, bx + w * 0.08, by - h * 0.3, s * 0.1, 'w')
    if big:
        eye(cv, hx + s * 0.02, hy - s * 0.05, big, 'e', 'k', rad=0.7)
    else:
        cv.put(int(hx + s * 0.03), int(hy - s * 0.02), 'k')
    sun(cv, w * 0.86, h * 0.1, s, 'u')
    cloud(cv, w * 0.5, h * 0.1, s * 0.05 + 0.35, 'c')
    if r.random() < 0.5:
        flip(cv)
    return cv, [sky(), role('c', 'cloud', 'Cloud', BLUE), role('b', 'bird', 'Hummingbird', GREEN), role('l', 'leaves', 'Stem and leaves', GREEN),
                role('w', 'wings', 'Wings', GREEN), role('r', 'throat', 'Throat', PINK), role('f', 'flower', 'Flowers', PINK),
                role('k', 'beak', 'Beak and eye', BROWN), role('e', 'eye_white', 'Eye white', BLUE), role('u', 'sun', 'Sun', BROWN)], ['animals', 'garden']


MORE_ANIMALS = [alpaca, bat, beaver, otter, polar_bear, moose, ostrich, pelican, puffin, hummingbird]

# Expansion roles of these subjects (as expansions.ROLES): subject -> {group: [(roleId, new name or None), ...]}.
MORE_ANIMALS_ROLES = {
    'alpaca': {'lime': [('sun', None)], 'red': [('blanket', 'Red blanket')]},
    'bat': {'lime': [('moon', None), ('stars', None)]},
    'beaver': {'lime': [('sun', None)]},
    'otter': {'lime': [('sun', None)], 'red': [('urchin', 'Red sea urchin')]},
    'polar_bear': {'lime': [('sun', None)], 'red': [('fish', 'Red fish')]},
    'moose': {'lime': [('sun', None)]},
    'ostrich': {'lime': [('sun', None)]},
    'pelican': {'lime': [('beak', 'Yellow beak and feet')]},
    'puffin': {'lime': [('sun', None)], 'red': [('beak', 'Red beak and feet')]},
    'hummingbird': {'lime': [('sun', None)], 'red': [('flower', 'Red flowers')]},
}

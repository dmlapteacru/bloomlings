"""More animal subjects for the levels (more_subjects.py, the owner, 2026-10-07): friendly, generic animals in the style of
animal_subjects.py, each drawn with picture_kit and returning (canvas, roles, themes). The colors that shape an animal
(its body against the sky or the water, its eyes against its face) come from different color groups, so they stay apart
under any mapping. An animal stands on the ground, the sea floor or a branch that shares the board's entry, so its face
stays within the nesting depth targets. On the big boards (from 300 cells) they get eye whites, pupils and more scenery,
and each picture's random stream turns the animal round and moves its scenery.
"""
import math

from animal_subjects import eye, eyes, pine
from picture_kit import (BLUE, BROWN, GREEN, PINK, box, cloud, disc, dots, ground_rows, hills, lens, oval, path, poly,
                         rbox, role, scatter, seg, sky, star, start)


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
            seg(cv, x0, hy - s * (0.24 + k * 0.04), x0 + side * s * 0.02, max(1.2, hy - s * (0.36 + k * 0.05)), 'a', 0.5)
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


def woodpecker(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.12)
    hills(cv, 'g', gtop, 0.4, w * 1.4, r.uniform(0, 6))
    tx0, tx1 = w * 0.64, w * 0.9
    box(cv, tx0, 0, tx1, gtop + 0.5, 't')
    poly(cv, [(tx0, gtop - 1.5), (tx0 - w * 0.08, gtop + 0.5), (tx1, gtop + 0.5)], 't')
    disc(cv, (tx0 + tx1) / 2 + w * 0.1, -h * 0.03, s * 0.28, 'l')
    oval(cv, (tx0 + tx1) / 2 + 0.3, h * 0.66, 0.9, 1.4, 'k')
    sun(cv, w * 0.14, h * 0.1, s, 'u')
    bx, by = tx0 - w * 0.2, h * 0.5
    lens(cv, bx + w * 0.06, by + h * 0.12, tx0 + 0.2, by + h * 0.32, s * 0.13, 'p')
    oval(cv, bx, by, w * 0.15, h * 0.16, 'p')
    oval(cv, bx + w * 0.07, by + h * 0.02, w * 0.06, h * 0.11, 'w')
    hx, hy = bx + w * 0.02, by - h * 0.18
    disc(cv, hx, hy, s * 0.14, 'p')
    poly(cv, [(hx + s * 0.08, hy - s * 0.06), (tx0 + 0.8, hy + 0.3), (hx + s * 0.08, hy + s * 0.07)], 'y')
    poly(cv, [(hx - s * 0.12, hy - s * 0.04), (hx - s * 0.02, hy - s * 0.18), (hx + s * 0.08, hy - s * 0.12), (hx - s * 0.28, hy)], 'c')
    if big:
        eye(cv, hx + s * 0.03, hy - s * 0.04, big, 'e', 'k')
    else:
        cv.put(int(hx + s * 0.04), int(hy - s * 0.02), 'k')
    if big:
        dots(cv, 'k', 'p', 2, 2, area=(bx - w * 0.12, by - h * 0.08, bx - w * 0.02, by + h * 0.1))
    for dy in (-0.05, 0.03):
        seg(cv, tx0 - 1.0, by + h * (0.12 + dy), tx0 + 0.3, by + h * (0.12 + dy), 'y', 0.4)
    if r.random() < 0.5:
        flip(cv)
    return cv, [sky(), role('u', 'sun', 'Sun', BROWN), role('t', 'trunk', 'Tree trunk', BROWN), role('c', 'crest', 'Crest', BROWN),
                role('p', 'bird', 'Woodpecker', PINK), role('y', 'beak', 'Beak and claws', PINK), role('w', 'belly', 'Belly', GREEN),
                role('k', 'eye', 'Eye, wing spots and tree hole', BLUE), role('e', 'eye_white', 'Eye white', GREEN),
                role('l', 'leaves', 'Leaves', GREEN), role('g', 'grass', 'Grass', GREEN)], ['animals', 'forest']


def _meerkat(cv, x, base, tall, big):
    """One meerkat standing up on watch, its snout to the right: `tall` cells from its feet to its ears."""
    k = tall / 10
    path(cv, [(x - k * 0.8, base - 0.6), (x - k * 2.4, base - 0.5), (x - k * 3.0, base - k * 1.4)], 'm', 0.45)
    oval(cv, x, base - k * 3.3, k * 1.5, k * 3.3, 'm')
    oval(cv, x + k * 0.5, base - k * 3.0, k * 0.8, k * 2.3, 'b')
    oval(cv, x + k * 0.3, base - 0.4, k * 1.6, 0.8, 'm')
    hy = base - k * 7.8
    oval(cv, x + k * 0.2, hy + k * 1.2, k * 1.0, k * 1.2, 'm')
    oval(cv, x + k * 0.2, hy, k * 1.5, k * 1.25, 'm')
    poly(cv, [(x + k * 0.8, hy - k * 0.9), (x + k * 3.0, hy + k * 0.4), (x + k * 0.8, hy + k * 1.0)], 'm')
    lens(cv, x + k * 0.6, base - k * 5.8, x + k * 1.7, base - k * 4.4, k * 0.9, 'm')
    if big:
        eye(cv, x + k * 0.8, hy - k * 0.5, big, None, 'k')
    else:
        cv.put(int(x + k * 0.8), int(hy - k * 0.2), 'k')
    cv.put(int(x - k * 0.9), int(hy - k * 0.5), 'k')
    cv.put(int(x + k * 2.8), int(hy + k * 0.3), 'k')


def meerkat(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.14)
    hills(cv, 'g', gtop, 0.5, w * 1.3, r.uniform(0, 6))
    sun(cv, w * 0.86, h * 0.1, s, 'u')
    for x in (w * 0.06, w * 0.94):
        oval(cv, x, gtop - 0.2, s * 0.1, s * 0.08, 'v')
    oval(cv, w * 0.5, gtop + 1.4, w * 0.12, 1.2, 'h')
    xs = (w * 0.3, w * 0.66) if not big else (w * 0.22, w * 0.5, w * 0.76)
    talls = (h * 0.7, h * 0.56) if not big else (h * 0.6, h * 0.68, h * 0.48)
    for x, t in zip(xs, talls):
        _meerkat(cv, x, gtop + 0.4, t, big)
    if r.random() < 0.5:
        flip(cv)
    return cv, [sky(), role('u', 'sun', 'Sun', BROWN), role('m', 'meerkat', 'Meerkats', BROWN), role('b', 'belly', 'Bellies', BROWN),
                role('k', 'eye', 'Eye patches, ears and noses', PINK), role('h', 'burrow', 'Burrow', BLUE), role('g', 'ground', 'Savanna', GREEN),
                role('v', 'bushes', 'Bushes', GREEN)], ['animals', 'savanna']


def shark(w, h, r):
    cv, s, cx, big = start(w, h, 'w')
    gtop = h - ground_rows(h, 0.1)
    for x in (w * 0.08, w * 0.9) + ((w * 0.74,) if big else ()):
        path(cv, [(x, gtop), (x + 0.8, h * 0.86), (x - 0.4, h * 0.76), (x + 0.5, h * 0.66)], 'g', 0.55)
    hills(cv, 'd', gtop, 0.5, w * 0.8, r.uniform(0, 6))
    sy = h * r.uniform(0.38, 0.44)
    poly(cv, [(w * 0.5, sy - h * 0.08), (w * 0.56, sy - h * 0.26), (w * 0.68, sy - h * 0.09)], 'k')
    lens(cv, w * 0.24, sy, w * 0.05, sy - h * 0.2, s * 0.13, 'k')
    lens(cv, w * 0.24, sy, w * 0.09, sy + h * 0.14, s * 0.11, 'k')
    poly(cv, [(w * 0.96, sy + h * 0.02), (w * 0.82, sy - h * 0.09), (w * 0.56, sy - h * 0.1), (w * 0.2, sy - h * 0.04),
              (w * 0.2, sy + h * 0.04), (w * 0.56, sy + h * 0.1), (w * 0.82, sy + h * 0.09)], 'k')
    for y in range(h):
        for x in range(w):
            if cv.g[y][x] == 'k' and y + 0.5 > sy + h * 0.04 + (x + 0.5 - w * 0.9) * 0.05:
                cv.g[y][x] = 'b'
    poly(cv, [(w * 0.62, sy + h * 0.06), (w * 0.54, sy + h * 0.2), (w * 0.7, sy + h * 0.08)], 'k')
    if big:
        eye(cv, w * 0.8, sy - h * 0.05, big, None, 'e')
    else:
        cv.put(int(w * 0.8), int(sy - h * 0.03), 'e')
    path(cv, [(w * 0.76, sy + h * 0.05), (w * 0.84, sy + h * 0.06), (w * 0.9, sy + h * 0.04)], 'e', 0.4)
    for dx in ((0.68,) if not big else (0.66, 0.7)):
        seg(cv, w * dx, sy - h * 0.03, w * dx, sy + h * 0.02, 'e', 0.4)
    for k, (x, y) in enumerate(((0.16, 0.74), (0.66, 0.7)) + (((0.4, 0.82),) if big else ())):
        oval(cv, w * x, h * y, w * 0.06, 0.9, 'f')
        poly(cv, [(w * (x - 0.04), h * y), (w * (x - 0.1), h * y - 1.0), (w * (x - 0.1), h * y + 1.0)], 'f')
    for k in range(3):
        disc(cv, w * (0.9 - (k % 2) * 0.04), h * (0.22 - k * 0.07), 0.6 + k * 0.12, 'x')
    if r.random() < 0.5:
        flip(cv)
    return cv, [('w', 'water', 'Water', BLUE, True), role('k', 'shark', 'Shark', PINK), role('b', 'belly', 'Belly', PINK),
                role('e', 'eye', 'Eye, smile and gills', BROWN), role('d', 'sand', 'Sand', BROWN), role('f', 'fish', 'Little fish', GREEN),
                role('g', 'weed', 'Seaweed', GREEN), role('x', 'bubbles', 'Bubbles', BLUE)], ['animals', 'sea']


def clownfish(w, h, r):
    cv, s, cx, big = start(w, h, 'w')
    gtop = h - ground_rows(h, 0.1)
    hills(cv, 'd', gtop, 0.5, w * 0.8, r.uniform(0, 6))
    ax = cx + w * r.uniform(-0.06, 0.06)
    n = 6 if not big else 8
    for k in range(n):
        x = ax - w * 0.3 + k * w * 0.6 / (n - 1)
        lean = (k - (n - 1) / 2) * 0.3
        tip = (x + lean * 1.5, gtop - h * (0.2 + 0.04 * (k % 2)))
        path(cv, [(x, gtop - 0.5), (x + lean - 0.5, gtop - h * 0.12), tip], 'a', 0.55)
        disc(cv, tip[0], tip[1], 0.75, 'q')
    oval(cv, ax, gtop - 0.2, w * 0.3, h * 0.06, 'a')
    cxr = w * r.choice((0.1, 0.9))
    for a in (-0.5, 0.0, 0.5):
        seg(cv, cxr, gtop, cxr + math.sin(a) * w * 0.1, gtop - h * 0.24, 'c', 0.5)
    fx, fy = cx + w * 0.02, h * 0.36
    poly(cv, [(fx - w * 0.2, fy), (fx - w * 0.36, fy - h * 0.1), (fx - w * 0.36, fy + h * 0.1)], 'o')
    oval(cv, fx - w * 0.02, fy - h * 0.1, w * 0.14, h * 0.05, 'o')
    oval(cv, fx, fy, w * 0.25, h * 0.11, 'o')
    lens(cv, fx + w * 0.02, fy + h * 0.08, fx - w * 0.06, fy + h * 0.16, s * 0.1, 'o')
    for dx in (0.12, -0.04, -0.21):
        box(cv, fx + dx * w - 0.6, 0, fx + dx * w + 0.6, h, 'z', only='o')
    eye(cv, fx + w * 0.17, fy - h * 0.04, big, None, 'e')
    for k in range(3):
        disc(cv, fx + w * 0.3 + (k % 2) * 0.8, fy - h * (0.1 + k * 0.07), 0.6 + k * 0.12, 'e')
    yx, yy = w * (0.84 if cxr < cx else 0.16), h * 0.14
    oval(cv, yx, yy, w * 0.07, h * 0.04 + 0.3, 'f')
    poly(cv, [(yx + w * 0.05, yy), (yx + w * 0.11, yy - h * 0.04), (yx + w * 0.11, yy + h * 0.04)], 'f')
    if r.random() < 0.5:
        flip(cv)
    return cv, [('w', 'water', 'Water', BLUE, True), role('o', 'clownfish', 'Clownfish', BROWN), role('z', 'stripes', 'Stripes', PINK),
                role('e', 'eye', 'Eye and bubbles', BLUE), role('a', 'anemone', 'Anemone', GREEN), role('q', 'tips', 'Anemone tips', GREEN),
                role('c', 'coral', 'Coral', PINK), role('f', 'fish', 'Little fish', BROWN), role('d', 'sand', 'Sand', BROWN)], ['animals', 'sea']


def pufferfish(w, h, r):
    cv, s, cx, big = start(w, h, 'w')
    gtop = h - ground_rows(h, 0.1)
    for x in (w * 0.08, w * 0.92) + ((w * 0.76,) if big else ()):
        path(cv, [(x, gtop), (x + 0.8, h * 0.86), (x - 0.4, h * 0.76), (x + 0.5, h * 0.66)], 'g', 0.55)
    hills(cv, 'd', gtop, 0.5, w * 0.8, r.uniform(0, 6))
    cxr = w * r.choice((0.2, 0.8))
    for a in (-0.5, 0.0, 0.5):
        seg(cv, cxr, gtop, cxr + math.sin(a) * w * 0.08, gtop - h * 0.14, 'c', 0.5)
    R = s * 0.27
    px, py = int(cx) + 0.5, gtop - R - s * 0.08
    n = 12 if not big else 16
    for k in range(n):
        a = math.radians(k * 360 / n + 15)
        d = 0.35
        poly(cv, [(px + math.cos(a - d) * R * 0.9, py + math.sin(a - d) * R * 0.9), (px + math.cos(a) * (R + s * 0.13), py + math.sin(a) * (R + s * 0.13)),
                  (px + math.cos(a + d) * R * 0.9, py + math.sin(a + d) * R * 0.9)], 'p')
    disc(cv, px, py, R, 'p')
    for side in (-1, 1):
        lens(cv, px + side * R * 0.75, py + R * 0.15, px + side * (R + s * 0.12), py - R * 0.05, s * 0.12, 'k')
    dots(cv, 'n', 'p', 3, 2, area=(px - R, py + R * 0.1, px + R, py + R * 0.7))
    for side in (-1, 1):
        disc(cv, px + side * R * 0.42, py - R * 0.3, R * 0.3 + 0.2, 'e')
        cv.put(int(px + side * R * 0.42), int(py - R * 0.3), 'n')
        if big:
            cv.put(int(px + side * R * 0.42), int(py - R * 0.3) + 1, 'n')
    oval(cv, px, py + R * 0.38, 1.0, 0.8 if not big else 1.0, 'n')
    for k in range(3):
        disc(cv, w * (0.1 + (k % 2) * 0.05), h * (0.3 - k * 0.08), 0.6 + k * 0.12, 'o')
    return cv, [('w', 'water', 'Water', BLUE, True), role('p', 'puffer', 'Pufferfish', BROWN), role('k', 'fins', 'Fins', BROWN),
                role('e', 'eye', 'Eyes', GREEN), role('n', 'pupil', 'Pupils, mouth and spots', PINK), role('c', 'coral', 'Coral', PINK),
                role('g', 'weed', 'Seaweed', GREEN), role('d', 'sand', 'Sand', BROWN), role('o', 'bubbles', 'Bubbles', BLUE)], ['animals', 'sea']


def ringed(cv, pts, rad, a, b, step):
    """A thick line along `pts` banded in rings of `a` and `b`, each `step` cells long (a ringed tail)."""
    done = 0.0
    for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
        n = max(1, int(math.hypot(x1 - x0, y1 - y0) / 0.25))
        for k in range(n):
            t = k / n
            disc(cv, x0 + (x1 - x0) * t, y0 + (y1 - y0) * t, rad, a if int(done / step) % 2 == 0 else b)
            done += math.hypot(x1 - x0, y1 - y0) / n
    disc(cv, pts[-1][0], pts[-1][1], rad, a if int(done / step) % 2 == 0 else b)


def narwhal(w, h, r):
    cv, s, cx, big = start(w, h, 'w')
    for x0, y1, x1 in ((-1, h * 0.07, w * r.uniform(0.3, 0.42)), (w * r.uniform(0.56, 0.66), h * 0.05, w + 1)):
        rbox(cv, x0, -1, x1, y1, 1.0, 'i')
    gtop = h - ground_rows(h, 0.1)
    for x in (w * 0.1, w * 0.86) + ((w * 0.7,) if big else ()):
        path(cv, [(x, gtop), (x + 0.8, h * 0.86), (x - 0.4, h * 0.78)], 'g', 0.55)
    hills(cv, 'd', gtop, 0.5, w * 0.8, r.uniform(0, 6))
    nx, ny = cx - w * 0.06, h * r.uniform(0.5, 0.56)
    for side in (-1, 1):
        lens(cv, nx - w * 0.34, ny, nx - w * 0.46, ny + side * h * 0.09, s * 0.12, 'n')
    poly(cv, [(nx - w * 0.1, ny - h * 0.07), (nx - w * 0.36, ny - 0.4), (nx - w * 0.36, ny + 0.6), (nx - w * 0.1, ny + h * 0.07)], 'n')
    oval(cv, nx, ny, w * 0.28, h * 0.09, 'n')
    disc(cv, nx + w * 0.22, ny - h * 0.01, s * 0.15, 'n')
    for y in range(h):
        for x in range(w):
            dx, dy = (x + 0.5 - nx - w * 0.02) / (w * 0.28), (y + 0.5 - ny - h * 0.02) / (h * 0.06)
            if dy >= 0 and dx * dx + dy * dy <= 1.0 and cv.g[y][x] == 'n':
                cv.g[y][x] = 'b'
    if big:
        dots(cv, 'm', 'n', 3, 2, area=(nx - w * 0.24, ny - h * 0.08, nx + w * 0.12, ny))
    hx, hy = nx + w * 0.22 + s * 0.12, ny - h * 0.04
    tip = (w - 1.5, max(h * 0.12, hy - (w - 1.5 - hx) * 1.1))
    seg(cv, hx, hy, tip[0], tip[1], 't', 0.5)
    if big:
        for k in range(1, 6):
            cv.put(int(hx + (tip[0] - hx) * k / 6), int(hy + (tip[1] - hy) * k / 6), 'y')
    eye(cv, nx + w * 0.24, ny - h * 0.05, big, None, 'k')
    path(cv, [(nx + w * 0.24, ny + h * 0.02), (nx + w * 0.3, ny + h * 0.02), (nx + w * 0.34, ny)], 'k', 0.4)
    for x, y in ((w * 0.2, h * 0.24), (w * 0.4, h * 0.8)) + (((w * 0.7, h * 0.2),) if big else ()):
        oval(cv, x, y, w * 0.06, 0.9, 'f')
        poly(cv, [(x + w * 0.04, y), (x + w * 0.1, y - 0.9), (x + w * 0.1, y + 0.9)], 'f')
    for k in range(3):
        disc(cv, nx + w * 0.06 + (k % 2) * 0.8, ny - h * (0.16 + k * 0.07), 0.6 + k * 0.12, 'x')
    if r.random() < 0.5:
        flip(cv)
    return cv, [('w', 'water', 'Arctic sea', BLUE, True), role('n', 'narwhal', 'Narwhal', PINK), role('b', 'belly', 'Belly', PINK),
                role('m', 'spots', 'Spots', BLUE), role('x', 'bubbles', 'Bubbles', BLUE), role('t', 'tusk', 'Tusk', BROWN),
                role('y', 'spiral', 'Tusk spiral', BROWN), role('k', 'eye', 'Eye and smile', BROWN), role('d', 'seabed', 'Seabed', BROWN),
                role('i', 'ice', 'Ice', GREEN), role('g', 'weed', 'Seaweed', GREEN), role('f', 'fish', 'Fish', GREEN)], ['animals', 'winter']


def sloth(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.12)
    hills(cv, 'g', gtop, 0.4, w * 1.4, r.uniform(0, 6))
    by = h * 0.3
    tx = w * 0.08
    box(cv, tx - 0.9, by, tx + 0.9, gtop + 0.5, 'r')
    path(cv, [(tx, by + 0.4), (w * 0.5, by), (w * 0.84, by - 0.9)], 'r', 0.7 if not big else 0.9)
    for x, y, a in ((tx + 0.4, by - 0.6, -2.0), (w * 0.82, by - 1.2, -0.5), (w * 0.52, by - 0.4, -1.4)) + (((w * 0.3, by - 0.2, -2.4),) if big else ()):
        lens(cv, x, y, x + math.cos(a) * s * 0.28, y + math.sin(a) * s * 0.2, s * 0.13, 'l')
    for x, y in ((w * 0.2, by - 2.2), (w * 0.72, by - 2.8)) + (((w * 0.44, by - 3.0),) if big else ()):
        disc(cv, x, y, 0.9 if not big else 1.1, 'p')
    sun(cv, w * 0.86, h * 0.08, s, 'u')
    sx = int(w * 0.46) + 0.5
    for dx in ((-0.16, 0.14) if not big else (-0.18, -0.08, 0.08, 0.18)):
        seg(cv, sx + dx * w, by + h * 0.14, sx + dx * w + 0.3, by + 0.6, 'b', 0.6 if not big else 0.75)
        cv.put(int(sx + dx * w + 0.3), int(by) - 1, 'k')
    oval(cv, sx, by + h * 0.2, w * 0.24, h * 0.075, 'b')
    hx, hy = sx + w * 0.25, by + h * 0.27
    disc(cv, hx, hy, s * 0.17, 'b')
    oval(cv, hx, hy + s * 0.01, s * 0.14, s * 0.11, 'f')
    for side in (-1, 1):
        lens(cv, hx + side * s * 0.02, hy - s * 0.03, hx + side * s * 0.13, hy + s * 0.03, s * 0.06 + 0.3, 'k')
    cv.put(int(hx), int(hy + s * 0.05), 'k')
    if big:
        path(cv, [(hx - s * 0.05, hy + s * 0.08), (hx + s * 0.05, hy + s * 0.08)], 'k', 0.4)
    for x in (w * 0.3, w * 0.7) + ((w * 0.5,) if big else ()):
        oval(cv, x, gtop - 0.2, s * 0.09, s * 0.07, 'v')
    if r.random() < 0.5:
        flip(cv)
    return cv, [('s', 'sky', 'Jungle sky', BLUE, True), role('u', 'sun', 'Sun', BROWN), role('b', 'sloth', 'Sloth', BROWN),
                role('f', 'face', 'Face', BROWN), role('k', 'stripes', 'Eye stripes, nose and claws', PINK),
                role('r', 'branch', 'Mossy tree', GREEN), role('l', 'leaves', 'Leaves', GREEN), role('p', 'flowers', 'Flowers', PINK),
                role('v', 'bushes', 'Bushes', GREEN), role('g', 'grass', 'Grass', GREEN)], ['animals', 'jungle']


def rhino(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.14)
    hills(cv, 'g', gtop, 0.4, w * 1.5, r.uniform(0, 6))
    oval(cv, w * 0.14, gtop + 0.8, w * 0.16, 1.2, 'w')
    tx = w * 0.12
    box(cv, tx - 0.5, h * 0.3, tx + 0.5, gtop, 't')
    oval(cv, tx + w * 0.02, h * 0.28, w * 0.13, h * 0.05, 'a')
    sun(cv, w * 0.86, h * 0.1, s, 'u')
    bx, by = cx - w * 0.04, gtop - h * 0.2
    for dx in (-0.2, -0.09, 0.08, 0.19):
        rbox(cv, bx + dx * w - 0.8, by, bx + dx * w + 0.8, gtop + 0.5, 0.5, 'r')
        cv.put(int(bx + dx * w), int(gtop) - 1, 'k')
    oval(cv, bx, by, w * 0.3, h * 0.12, 'r')
    disc(cv, bx - w * 0.31, by - h * 0.02, 0.7, 'r')
    hx, hy = bx + w * 0.3, by + h * 0.02
    oval(cv, hx, hy, s * 0.14, s * 0.11, 'r')
    oval(cv, hx + s * 0.1, hy + s * 0.04, s * 0.08, s * 0.08, 'r')
    lens(cv, hx - s * 0.08, hy - s * 0.08, hx - s * 0.1, hy - s * 0.2, s * 0.07, 'r')
    poly(cv, [(hx + s * 0.04, hy - s * 0.04), (hx + s * 0.26, hy - s * 0.34), (hx + s * 0.2, hy - s * 0.0)], 'h')
    poly(cv, [(hx - s * 0.06, hy - s * 0.08), (hx + s * 0.0, hy - s * 0.2), (hx + s * 0.04, hy - s * 0.06)], 'h')
    cv.put(int(hx - s * 0.02), int(hy - s * 0.02), 'k')
    if big:
        cv.put(int(hx - s * 0.02), int(hy - s * 0.02) + 1, 'k')
        for dx in (-0.12, 0.12):
            seg(cv, bx + dx * w, by - h * 0.1, bx + dx * w - 0.6, by + h * 0.08, 'v', 0.4)
    if big:
        ox = bx - w * 0.04
        oval(cv, ox, by - h * 0.13, s * 0.06 + 0.3, s * 0.05 + 0.3, 'o')
        poly(cv, [(ox + s * 0.05, by - h * 0.14), (ox + s * 0.12, by - h * 0.13), (ox + s * 0.05, by - h * 0.12)], 'o')
    if r.random() < 0.5:
        flip(cv)
    return cv, [sky(), role('r', 'rhino', 'Rhino', PINK), role('v', 'folds', 'Skin folds', PINK), role('h', 'horn', 'Horns', BROWN),
                role('k', 'eye', 'Eye and toenails', BROWN), role('o', 'bird', 'Oxpecker', BROWN), role('u', 'sun', 'Sun', BROWN),
                role('t', 'trunk', 'Acacia trunk', BROWN), role('a', 'acacia', 'Acacia', GREEN), role('w', 'waterhole', 'Waterhole', BLUE),
                role('g', 'grass', 'Savanna grass', GREEN)], ['animals', 'savanna']


def gorilla(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.12)
    hills(cv, 'g', gtop, 0.4, w * 1.4, r.uniform(0, 6))
    for x, y, a in ((w * 0.02, h * 0.36, 0.2), (w * 0.98, h * 0.42, 2.9)) + (((w * 0.02, h * 0.1, 0.5),) if big else ()):
        lens(cv, x, y, x + math.cos(a) * s * 0.3, y + math.sin(a) * s * 0.18, s * 0.14, 'l')
    bx, by = w * r.choice((0.16, 0.84)), h * 0.06
    for k in range(3):
        lens(cv, bx - 1.0 + k, by + 0.4, bx - 1.6 + k * 1.4, by + h * 0.16, s * 0.1, 'y')
    disc(cv, w - bx, h * 0.12, 0.9 if not big else 1.1, 'p')
    gx = int(cx) + 0.5
    for side in (-1, 1):
        oval(cv, gx + side * w * 0.12, gtop - h * 0.06, w * 0.1, h * 0.07, 'k')
        path(cv, [(gx + side * w * 0.22, gtop - h * 0.38), (gx + side * w * 0.33, gtop - h * 0.18), (gx + side * w * 0.33, gtop - 0.6)], 'k', s * 0.08 + 0.2)
        oval(cv, gx + side * w * 0.33, gtop - 0.6, s * 0.08, 0.9, 'k')
    oval(cv, gx, gtop - h * 0.2, w * 0.23, h * 0.16, 'k')
    oval(cv, gx, gtop - h * 0.35, w * 0.3, h * 0.09, 'k')
    oval(cv, gx, gtop - h * 0.22, w * 0.11, h * 0.09, 'f')
    hy = gtop - h * 0.52
    oval(cv, gx, hy - s * 0.06, s * 0.13, s * 0.14, 'k')
    disc(cv, gx, hy, s * 0.17, 'k')
    oval(cv, gx, hy + s * 0.05, s * 0.13, s * 0.11, 'f')
    box(cv, gx - s * 0.15, hy - s * 0.04, gx + s * 0.15, hy - s * 0.04 + 0.6, 'k', only='f')
    eyes(cv, gx, hy - s * 0.02, 1.0 if not big else 2.0, big, None, 'e')
    for side in (-1, 1):
        cv.put(int(gx + side * 0.6), int(hy + s * 0.09), 'e')
    if big:
        path(cv, [(gx - s * 0.06, hy + s * 0.13), (gx + s * 0.06, hy + s * 0.13)], 'e', 0.4)
    if r.random() < 0.5:
        flip(cv)
    return cv, [sky(), role('k', 'gorilla', 'Gorilla', PINK), role('f', 'face', 'Face and chest', BROWN), role('e', 'eye', 'Eyes and nostrils', BLUE),
                role('y', 'banana', 'Bananas', BROWN), role('l', 'leaves', 'Jungle leaves', GREEN), role('p', 'flowers', 'Flower', PINK),
                role('g', 'grass', 'Grass', GREEN)], ['animals', 'jungle']


def lemur(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.12)
    hills(cv, 'g', gtop, 0.4, w * 1.4, r.uniform(0, 6))
    tx = w * 0.92
    box(cv, tx - 0.6, h * 0.3, tx + 0.6, gtop, 't')
    disc(cv, tx - w * 0.04, h * 0.18, s * 0.17, 'l')
    for x, y in ((tx - w * 0.12, h * 0.22), (tx + w * 0.0, h * 0.1)):
        disc(cv, x, y, 0.8 if not big else 1.0, 'o')
    sun(cv, w * 0.12, h * 0.1, s, 'u')
    lx = int(w * 0.38) + 0.5
    ringed(cv, [(lx + w * 0.12, gtop - 0.8), (lx + w * 0.34, gtop - h * 0.08), (lx + w * 0.42, h * 0.5), (lx + w * 0.32, h * 0.34),
                (lx + w * 0.36, h * 0.2)], 0.8 if not big else 1.05, 'p', 'k', 1.3 if not big else 1.6)
    oval(cv, lx, gtop - h * 0.16, w * 0.18, h * 0.17, 'p')
    oval(cv, lx, gtop - h * 0.12, w * 0.1, h * 0.08, 'f')
    for side in (-1, 1):
        oval(cv, lx + side * w * 0.1, gtop - 0.5, w * 0.08, 0.9, 'p')
    hy = gtop - h * 0.44
    for side in (-1, 1):
        poly(cv, [(lx + side * s * 0.08, hy - s * 0.14), (lx + side * s * 0.24, hy - s * 0.3), (lx + side * s * 0.24, hy - s * 0.06)], 'p')
    disc(cv, lx, hy, s * 0.21, 'p')
    oval(cv, lx, hy + s * 0.05, s * 0.17, s * 0.13, 'f')
    for side in (-1, 1):
        disc(cv, lx + side * (1.1 if not big else 1.6), hy - s * 0.01, 0.9 if not big else 1.2, 'k')
        if big:
            cv.put(int(lx + side * 1.6), int(hy - s * 0.01), 'e')
    poly(cv, [(lx - s * 0.06, hy + s * 0.07), (lx + s * 0.06, hy + s * 0.07), (lx, hy + s * 0.18)], 'k')
    if r.random() < 0.5:
        flip(cv)
    return cv, [sky(), role('u', 'sun', 'Sun', BROWN), role('p', 'lemur', 'Lemur', PINK), role('f', 'face', 'Face and belly', BLUE),
                role('k', 'rings', 'Tail rings, eye patches and nose', BROWN), role('e', 'eye', 'Amber eyes', BROWN),
                role('t', 'trunk', 'Tree trunk', BROWN), role('l', 'leaves', 'Leaves', GREEN), role('o', 'fruit', 'Fruit', PINK),
                role('g', 'grass', 'Grass', GREEN)], ['animals', 'jungle']


MORE_ANIMALS = [alpaca, bat, beaver, otter, polar_bear, moose, ostrich, pelican, puffin, hummingbird,
                woodpecker, meerkat, shark, clownfish, pufferfish, narwhal, sloth, rhino, gorilla, lemur]

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
    'woodpecker': {'lime': [('sun', None)], 'red': [('crest', 'Red crest')]},
    'meerkat': {'lime': [('sun', None)]},
    'shark': {'lime': [('fish', 'Yellow fish')]},
    'clownfish': {'lime': [('fish', 'Yellow fish')], 'red': [('coral', 'Red coral')]},
    'pufferfish': {'lime': [('puffer', 'Yellow pufferfish')], 'red': [('coral', 'Red coral')]},
    'narwhal': {'lime': [('fish', 'Yellow fish')]},
    'sloth': {'lime': [('sun', None)], 'red': [('flowers', 'Red flowers')]},
    'rhino': {'lime': [('sun', None)]},
    'gorilla': {'lime': [('banana', None)], 'red': [('flowers', 'Red flowers')]},
    'lemur': {'lime': [('sun', None)], 'red': [('fruit', 'Red fruit')]},
}

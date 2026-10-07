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


def alpaca(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.12)
    for mx, top in ((w * r.uniform(0.08, 0.28), h * 0.44), (w * r.uniform(0.66, 0.86), h * 0.52)):
        poly(cv, [(mx - w * 0.36, gtop + 0.5), (mx, top), (mx + w * 0.36, gtop + 0.5)], 'm')
        box(cv, 0, 0, w, top + h * 0.09, 'w', only='m')
    hills(cv, 'g', gtop, 0.4, w * 1.4, r.uniform(0, 6))
    for x in (w * 0.9,) + ((w * 0.06,) if big else ()):
        oval(cv, x, gtop - 0.3, s * 0.1, s * 0.07, 'v')
    disc(cv, w * 0.12, h * 0.1, s * 0.09, 'u')
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
        disc(cv, bx + side * s * 0.15, hy - s * 0.14, s * 0.03 + 0.6, 'b')
    disc(cv, bx, hy, s * 0.2, 'b')
    for side in (-1, 1):
        disc(cv, bx + side * s * 0.07, hy + s * 0.08, s * 0.07 + 0.2, 'm')
    eyes(cv, bx, hy - s * 0.1, 1.5, big, 'e', 'k')
    box(cv, bx - 1.0, hy + s * 0.02 - 0.5, bx + 1.0, hy + s * 0.02 + 0.4, 'k')
    ty = int(hy + s * 0.13) + 0.5
    box(cv, bx - 1.0, ty, bx + 1.0, ty + (1.0 if not big else 2.0), 't')
    for x in (tx - 2.0, tx + 2.4):
        box(cv, x - 0.5, gtop - 0.5, x + 0.5, gtop - 0.5, 'o')
    disc(cv, w * 0.86, h * 0.1, s * 0.085, 'u')
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
    for side in (-1, 1):
        disc(cv, hx + side * s * 0.16, hy - s * 0.14, 0.8, 'o')
    disc(cv, hx, hy, s * 0.2, 'o')
    oval(cv, hx, hy + s * 0.04, s * 0.15, s * 0.13, 'f')
    eyes(cv, hx, hy - s * 0.04, 1.0, big, None, 'k')
    box(cv, hx - 0.5, hy + s * 0.08, hx + 0.5, hy + s * 0.08 + 0.6, 'k')
    if big:
        for side in (-1, 1):
            seg(cv, hx + side * s * 0.16, hy + s * 0.1, hx + side * s * 0.27, hy + s * 0.08, 'k', 0.4)
    px = hx + s * 0.32
    oval(cv, px, oy - h * 0.08, s * 0.1, s * 0.07, 'x')
    for side in (-1, 1):
        disc(cv, px + side * s * 0.09, oy - h * 0.05, 0.8, 'o')
    disc(cv, w * 0.84, h * 0.12, s * 0.085, 'u')
    if big:
        cloud(cv, w * 0.24, h * 0.14, s * 0.05 + 0.4, 'c')
    if r.random() < 0.5:
        flip(cv)
    return cv, [sky(), role('w', 'sea', 'Sea', BLUE), role('u', 'sun', 'Sun', BROWN), role('o', 'otter', 'Otter', BROWN),
                role('f', 'face', 'Face and belly', BROWN), role('k', 'eye', 'Eyes and nose', PINK), role('x', 'urchin', 'Sea urchin', PINK), role('l', 'kelp', 'Kelp', GREEN), role('c', 'cloud', 'Cloud', BLUE)], ['animals', 'sea']


def polar_bear(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    sea = h * 0.5
    hills(cv, 'w', sea, 0.3, w * 0.5, r.uniform(0, 6))
    itop = h * 0.74
    poly(cv, [(w * 0.08, itop), (w * 0.92, itop), (w * 1.04, h + 1), (-w * 0.04, h + 1)], 'i')
    bx, by = cx - w * 0.12, itop - h * 0.16
    for dx in (-0.19, -0.08, 0.08, 0.18):
        rbox(cv, bx + dx * w - 0.9, by, bx + dx * w + 0.9, itop + 0.5, 0.6, 'b')
    oval(cv, bx, by, w * 0.26, h * 0.1, 'b')
    disc(cv, bx + w * 0.14, by - h * 0.03, s * 0.14, 'b')
    hx, hy = bx + w * 0.34, by - h * 0.02
    oval(cv, bx + w * 0.25, by - h * 0.02, s * 0.12, s * 0.09, 'b')
    oval(cv, hx, hy, s * 0.12, s * 0.1, 'b')
    oval(cv, hx + s * 0.12, hy + s * 0.04, s * 0.09, s * 0.06, 'b')
    disc(cv, hx - s * 0.05, hy - s * 0.1, 0.85, 'b')
    disc(cv, bx - w * 0.26, by - h * 0.04, 0.8, 'b')
    eye(cv, hx + s * 0.02, hy - s * 0.06, big, None, 'k')
    cv.put(int(hx + s * 0.2), int(hy + s * 0.02), 'k')
    if big:
        path(cv, [(hx + s * 0.08, hy + s * 0.09), (hx + s * 0.16, hy + s * 0.08)], 'k', 0.4)
    fx = w * 0.84
    oval(cv, fx, itop + h * 0.1, w * 0.08, 1.0, 'f')
    poly(cv, [(fx - w * 0.06, itop + h * 0.1), (fx - w * 0.13, itop + h * 0.06), (fx - w * 0.13, itop + h * 0.14)], 'f')
    cv.put(int(fx + w * 0.03), int(itop + h * 0.09), 'k')
    ix = w * 0.8
    poly(cv, [(ix - w * 0.16, sea + 0.6), (ix - w * 0.06, sea - h * 0.12), (ix + w * 0.02, sea - h * 0.08), (ix + w * 0.14, sea + 0.6)], 'a')
    disc(cv, w * 0.2, h * 0.16, s * 0.08, 'u')
    scatter(cv, 'x', 's', 4 + (w * h) // 60, r, sep=3, area=(0, h * 0.2, w - 1, sea - 2))
    if r.random() < 0.5:
        flip(cv)
    return cv, [sky(), role('b', 'bear', 'Polar bear', PINK), role('x', 'snow', 'Snowflakes', PINK), role('k', 'eye', 'Eye and nose', BROWN),
                role('w', 'sea', 'Sea', BLUE), role('i', 'ice', 'Ice floe', GREEN), role('a', 'iceberg', 'Iceberg', GREEN),
                role('f', 'fish', 'Fish', BROWN), role('u', 'sun', 'Sun', BROWN)], ['animals', 'winter']


MORE_ANIMALS = [alpaca, bat, beaver, otter, polar_bear]

# Expansion roles of these subjects (as expansions.ROLES): subject -> {group: [(roleId, new name or None), ...]}.
MORE_ANIMALS_ROLES = {
    'alpaca': {'lime': [('sun', None)], 'red': [('blanket', 'Red blanket')]},
    'bat': {'lime': [('moon', None), ('stars', None)]},
    'beaver': {'lime': [('sun', None)]},
    'otter': {'lime': [('sun', None)], 'red': [('urchin', 'Red sea urchin')]},
    'polar_bear': {'lime': [('sun', None)], 'red': [('fish', 'Red fish')]},
}

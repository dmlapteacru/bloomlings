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
    for mx, top in ((w * r.uniform(0.1, 0.3), h * 0.34), (w * r.uniform(0.62, 0.85), h * 0.44)):
        half, k = w * 0.4, h * 0.11
        cap = half * k / (gtop + 0.5 - top)
        poly(cv, [(mx - half, gtop + 0.5), (mx, top), (mx + half, gtop + 0.5)], 'm')
        poly(cv, [(mx - cap, top + k), (mx, top - 0.3), (mx + cap, top + k)], 'w')
    hills(cv, 'g', gtop, 0.4, w * 1.4, r.uniform(0, 6))
    for x in (w * 0.9,) + ((w * 0.06,) if big else ()):
        oval(cv, x, gtop - 0.3, s * 0.1, s * 0.07, 'v')
    disc(cv, w * 0.12, h * 0.1, s * 0.09, 'u')
    ax, by = cx - w * 0.1, gtop - h * 0.21
    for dx in (-0.17, -0.08, 0.07, 0.16):
        box(cv, ax + dx * w - 0.5, by, ax + dx * w + 0.5, gtop + 0.4, 'a')
        cv.put(int(ax + dx * w), int(gtop) - 1, 'k')
    oval(cv, ax, by, w * 0.22, h * 0.09, 'a')
    for k in range(10):
        a = math.radians(k * 36)
        disc(cv, ax + math.cos(a) * w * 0.19, by + math.sin(a) * h * 0.07, s * 0.08 + 0.2, 'a')
    disc(cv, ax - w * 0.24, by - h * 0.05, s * 0.07 + 0.2, 'a')
    nx = ax + w * 0.18
    rbox(cv, nx - s * 0.08, h * 0.2, nx + s * 0.08, by, 0.8, 'a')
    hx, hy = nx + s * 0.03, h * 0.18
    oval(cv, hx, hy, s * 0.12, s * 0.09, 'a')
    oval(cv, hx + s * 0.13, hy + s * 0.04, s * 0.08, s * 0.06, 'a')
    for dx in (-0.07, 0.03):
        seg(cv, hx + dx * s, hy - s * 0.06, hx + dx * s - 0.3, hy - s * 0.2, 'a', 0.45)
    eye(cv, hx + s * 0.04, hy - s * 0.05, big, 'e', 'k')
    cv.put(int(hx + s * 0.2), int(hy + s * 0.03), 'k')
    box(cv, ax - w * 0.1, by - h * 0.1, ax + w * 0.08, by + h * 0.02, 'b', only='a')
    if big:
        box(cv, ax - w * 0.1, by - h * 0.02, ax + w * 0.08, by - h * 0.02 + 0.6, 'w', only='b')
    if r.random() < 0.5:
        flip(cv)
    return cv, [sky(), role('a', 'alpaca', 'Alpaca', BROWN), role('k', 'eye', 'Eye, nose and hooves', PINK),
                role('e', 'eye_white', 'Eye white', GREEN), role('b', 'blanket', 'Blanket', PINK),
                role('m', 'mountains', 'Mountains', PINK), role('w', 'snow', 'Snowy peaks', BLUE),
                role('g', 'grass', 'Grass', GREEN), role('v', 'shrubs', 'Shrubs', GREEN), role('u', 'sun', 'Sun', BROWN)], ['animals', 'mountains']


def bat(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.1)
    hills(cv, 'g', gtop, 0.6, w * 0.9, r.uniform(0, 6))
    for x in (w * 0.12, w * 0.88) if not big else (w * 0.1, w * 0.62, w * 0.9):
        oval(cv, x, gtop - 0.2, s * 0.09, s * 0.07, 'v')
    mx = w * r.choice((0.16, 0.84))
    disc(cv, mx, h * 0.13, s * 0.12, 'm')
    bx, by = int(cx) + 0.5, h * r.uniform(0.44, 0.5)
    edge = [(0.47, -0.13), (0.44, 0.04), (0.36, -0.01), (0.29, 0.08), (0.21, 0.03), (0.13, 0.11), (0.05, 0.06)]
    for side in (-1, 1):
        poly(cv, [(bx, by - h * 0.08), (bx + side * w * 0.22, by - h * 0.16)] + [(bx + side * w * a, by + h * b) for a, b in edge], 'w')
        if big:
            for a, b in edge[2::2]:
                seg(cv, bx + side * w * 0.08, by - h * 0.06, bx + side * w * a, by + h * b - 0.6, 'b', 0.4)
    oval(cv, bx, by + h * 0.02, s * 0.12, s * 0.16, 'b')
    hy = by - s * 0.2
    disc(cv, bx, hy, s * 0.13, 'b')
    for side in (-1, 1):
        poly(cv, [(bx + side * s * 0.02, hy - s * 0.08), (bx + side * s * 0.15, hy - s * 0.3), (bx + side * s * 0.15, hy - s * 0.02)], 'b')
        cv.put(int(bx + side * s * 0.05), int(by + s * 0.18), 'k')
    eyes(cv, bx, hy - s * 0.05, 1.0, big, 'e', 'k')
    if big:
        for side in (-1, 1):
            cv.put(int(bx + side * 0.8), int(hy + s * 0.1), 'e')
        mx2 = w - mx
        for side in (-1, 1):
            lens(cv, mx2, h * 0.2, mx2 + side * s * 0.14, h * 0.17, s * 0.06 + 0.4, 'w')
        disc(cv, mx2, h * 0.2, 0.8, 'b')
    scatter(cv, 'x', 's', 5 + (w * h) // 70, r, sep=3, area=(0, 0, w - 1, h * 0.36))
    return cv, [('s', 'night', 'Night sky', BLUE, True), role('b', 'bat', 'Bat', PINK), role('w', 'wings', 'Wings', PINK),
                role('k', 'eye', 'Eyes and feet', BROWN), role('e', 'eye_white', 'Eye whites and fangs', BLUE),
                role('m', 'moon', 'Moon', BROWN), role('x', 'stars', 'Stars', BROWN),
                role('g', 'hills', 'Hills', GREEN), role('v', 'bushes', 'Bushes', GREEN)], ['animals', 'night']


def beaver(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.14)
    hills(cv, 'g', gtop, 0.4, w * 1.4, r.uniform(0, 6))
    if big:
        oval(cv, w * 0.9, gtop + 1.0, w * 0.2, 1.6, 'w')
    tx = w * 0.12
    for y0, y1, half in ((h * 0.3, h * 0.62, 1.4), (h * 0.7, gtop + 0.5, 1.4)):
        box(cv, tx - half, y0, tx + half, y1, 'o')
    poly(cv, [(tx - 1.4, h * 0.6), (tx + 1.4, h * 0.6), (tx, h * 0.66)], 'o')
    poly(cv, [(tx - 1.4, h * 0.72), (tx + 1.4, h * 0.72), (tx, h * 0.66)], 'o')
    disc(cv, tx + 0.6, h * 0.2, s * 0.2, 'l')
    for dx in (-1.6, 2.2):
        cv.put(int(tx + dx), int(gtop) - 1, 'o')
    bx = float(int(w * 0.5))
    oval(cv, bx + w * 0.26, gtop - 0.3, w * 0.17, h * 0.055, 'p')
    oval(cv, bx, gtop - h * 0.17, w * 0.21, h * 0.19, 'b')
    oval(cv, bx, gtop - h * 0.14, w * 0.11, h * 0.12, 'm')
    for side in (-1, 1):
        oval(cv, bx + side * w * 0.12, gtop - 0.4, w * 0.08, 1.1, 'b')
        disc(cv, bx + side * w * 0.1, gtop - h * 0.26, 1.0, 'b')
    hy = h * 0.38
    for side in (-1, 1):
        disc(cv, bx + side * s * 0.16, hy - s * 0.15, s * 0.05 + 0.4, 'b')
    disc(cv, bx, hy, s * 0.2, 'b')
    oval(cv, bx, hy + s * 0.08, s * 0.12, s * 0.08, 'm')
    eyes(cv, bx, hy - s * 0.09, 1.5, big, 'e', 'k')
    box(cv, bx - 1.0, hy + s * 0.02 - 0.5, bx + 1.0, hy + s * 0.02 + 0.4, 'k')
    box(cv, bx - 1.0, hy + s * 0.13, bx + 1.0, hy + s * 0.13 + (1.6 if not big else 2.4), 't')
    if big:
        for y in (gtop - 1.0,):
            for x in range(int(bx + w * 0.14), int(bx + w * 0.4), 2):
                cv.put(x, int(y), 'k')
    disc(cv, w * 0.86, h * 0.1, s * 0.085, 'u')
    if r.random() < 0.5:
        flip(cv)
    return cv, [sky(), role('b', 'beaver', 'Beaver', BROWN), role('m', 'muzzle', 'Muzzle and belly', BROWN),
                role('k', 'eye', 'Eyes and nose', PINK), role('t', 'teeth', 'Teeth', BLUE), role('e', 'eye_white', 'Eye whites', GREEN),
                role('p', 'tail', 'Tail', PINK), role('o', 'tree', 'Gnawed tree', BROWN), role('l', 'leaves', 'Leaves', GREEN),
                role('w', 'pond', 'Pond', BLUE), role('g', 'grass', 'Grass', GREEN), role('u', 'sun', 'Sun', BROWN)], ['animals', 'forest']


def otter(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    sea = h * 0.58
    hills(cv, 'w', sea, 0.35, w * 0.5, r.uniform(0, 6))
    for x in (w * 0.2, w * 0.8) if not big else (w * 0.12, w * 0.46, w * 0.84):
        path(cv, [(x, h), (x + 0.8, h * 0.9), (x - 0.4, h * 0.8), (x + 0.5, h * 0.72)], 'l', 0.55)
    ox, oy = cx + w * 0.06, sea - 0.4
    lens(cv, ox + w * 0.24, oy, ox + w * 0.44, oy + h * 0.06, s * 0.1, 'o')
    oval(cv, ox, oy, w * 0.3, h * 0.075, 'o')
    oval(cv, ox - w * 0.02, oy - 0.7, w * 0.2, h * 0.04, 'f')
    for dx in (0.2, 0.27):
        oval(cv, ox + w * dx, oy - h * 0.07, 0.8, h * 0.05, 'o')
    hx, hy = int(ox - w * 0.3) + 0.5, oy - h * 0.09
    for side in (-1, 1):
        disc(cv, hx + side * s * 0.14, hy - s * 0.13, 0.8, 'o')
    disc(cv, hx, hy, s * 0.17, 'o')
    oval(cv, hx, hy + s * 0.06, s * 0.12, s * 0.08, 'f')
    eyes(cv, hx, hy - s * 0.09, 1.0, big, 'e', 'k')
    cv.put(int(hx), int(hy + s * 0.03), 'k')
    if big:
        for side in (-1, 1):
            seg(cv, hx + side * s * 0.13, hy + s * 0.07, hx + side * s * 0.24, hy + s * 0.05, 'k', 0.4)
    px = ox - w * 0.06
    star(cv, px, oy - h * 0.08, s * 0.1 + 0.3, 'x', ri=0.8)
    for side in (-1, 1):
        disc(cv, px + side * s * 0.1, oy - h * 0.05, 0.8, 'o')
    disc(cv, w * 0.84, h * 0.12, s * 0.085, 'u')
    if big:
        cloud(cv, w * 0.24, h * 0.14, s * 0.05 + 0.4, 'c')
    if r.random() < 0.5:
        flip(cv)
    return cv, [sky(), role('w', 'sea', 'Sea', BLUE), role('o', 'otter', 'Otter', BROWN), role('f', 'face', 'Face and belly', BROWN),
                role('k', 'eye', 'Eyes and nose', PINK), role('e', 'eye_white', 'Eye whites', GREEN), role('x', 'starfish', 'Starfish', PINK),
                role('l', 'kelp', 'Kelp', GREEN), role('u', 'sun', 'Sun', BROWN), role('c', 'cloud', 'Cloud', BLUE)], ['animals', 'sea']


def polar_bear(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    sea = h * 0.52
    hills(cv, 'w', sea, 0.3, w * 0.5, r.uniform(0, 6))
    itop = h * 0.74
    poly(cv, [(w * 0.06, itop), (w * 0.94, itop), (w * 1.04, h + 1), (-w * 0.04, h + 1)], 'i')
    path(cv, [(-1, h * 0.14), (w * 0.3, h * 0.08), (w * 0.6, h * 0.14), (w + 1, h * 0.08)], 'a', 0.5 if not big else 0.8)
    bx, by = cx - w * 0.08, itop - h * 0.15
    for dx in (-0.22, -0.1, 0.08, 0.2):
        rbox(cv, bx + dx * w - 0.9, by, bx + dx * w + 0.9, itop + 0.5, 0.6, 'b')
    oval(cv, bx, by, w * 0.28, h * 0.1, 'b')
    disc(cv, bx + w * 0.16, by - h * 0.03, s * 0.15, 'b')
    hx, hy = bx + w * 0.32, by - h * 0.06
    oval(cv, hx, hy, s * 0.13, s * 0.09, 'b')
    oval(cv, hx + s * 0.12, hy + s * 0.03, s * 0.08, s * 0.06, 'b')
    disc(cv, hx - s * 0.06, hy - s * 0.08, 0.8, 'b')
    disc(cv, bx - w * 0.28, by - h * 0.03, 0.8, 'b')
    eye(cv, hx + s * 0.02, hy - s * 0.05, big, None, 'k')
    cv.put(int(hx + s * 0.2), int(hy + s * 0.01), 'k')
    for dx in (-0.22, -0.1, 0.08, 0.2):
        cv.put(int(bx + dx * w), int(itop) - 1 if big else -1, 'k')
    fx = w * 0.84
    oval(cv, fx, itop + h * 0.08, w * 0.07, 0.9, 'f')
    poly(cv, [(fx - w * 0.05, itop + h * 0.08), (fx - w * 0.11, itop + h * 0.05), (fx - w * 0.11, itop + h * 0.11)], 'f')
    disc(cv, w * 0.14, h * 0.24, s * 0.08, 'u')
    scatter(cv, 'x', 's', 4 + (w * h) // 60, r, sep=3, area=(0, h * 0.18, w - 1, sea - 2))
    if r.random() < 0.5:
        flip(cv)
    return cv, [('s', 'sky', 'Polar sky', PINK, True), role('b', 'bear', 'Polar bear', BLUE), role('k', 'eye', 'Eye and nose', BROWN),
                role('w', 'sea', 'Sea', BLUE), role('x', 'snow', 'Snowflakes', BLUE), role('i', 'ice', 'Ice floe', GREEN),
                role('a', 'aurora', 'Northern lights', GREEN), role('f', 'fish', 'Fish', BROWN), role('u', 'sun', 'Sun', BROWN)], ['animals', 'winter']


MORE_ANIMALS = [alpaca, bat, beaver, otter, polar_bear]

# Expansion roles of these subjects (as expansions.ROLES): subject -> {group: [(roleId, new name or None), ...]}.
MORE_ANIMALS_ROLES = {
    'alpaca': {'lime': [('sun', None)], 'red': [('blanket', 'Red blanket')]},
    'bat': {'lime': [('moon', None), ('stars', None)]},
    'beaver': {'lime': [('sun', None)]},
    'otter': {'lime': [('sun', None)], 'red': [('starfish', None)]},
    'polar_bear': {'lime': [('sun', None)], 'red': [('fish', 'Red fish')]},
}

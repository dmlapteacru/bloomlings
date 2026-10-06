"""Animal subjects for the bigger boards (the owner, 2026-10-06): friendly, generic animals in the procedural style of
sketch_pictures.py, each drawn with picture_kit and returning (canvas, roles, themes). The animals stand on the ground,
so their faces stay within the nesting depth targets; the colors that shape an animal (its body against the sky, its
eyes against its face) come from different color groups, so they stay apart under any mapping. On the big boards (from
300 cells) they get eye whites, pupils and more scenery.
"""
import math

from picture_kit import (BLUE, BROWN, GREEN, PINK, box, cloud, disc, ground_rows, hills, lens, oval, path, poly, rbox,
                         role, scatter, seg, sky, star, start)


def eyes(cv, x, y, dx, big, white, pupil, small=None, rad=1.0):
    """Two eyes at x +- dx, each two cells tall: of `small` (the pupil's role when not given) on the regular boards,
    pupils in eye whites on the big ones."""
    for side in (-1, 1):
        eye(cv, x + side * dx, y, big, white, pupil if big else (small or pupil), rad)


def eye(cv, x, y, big, white, pupil, rad=1.0):
    """One eye: two cells of the pupil's role, in an eye white on the big boards (when the subject has one)."""
    ex, ey = int(x), int(y)
    if big and white:
        disc(cv, ex + 0.5, ey + 1.0, rad + 0.3, white)
    cv.put(ex, ey, pupil)
    cv.put(ex, ey + 1, pupil)


def pine(cv, x, base, height, width, c, trunk=None):
    for k in range(3):
        yb = base - k * height * 0.28
        half = width * (1 - k * 0.25) / 2
        poly(cv, [(x - half, yb), (x, yb - height * 0.42), (x + half, yb)], c)
    if trunk:
        box(cv, x - 0.5, base, x + 0.5, base + 1.2, trunk)


def cat(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    fy = h * 0.88
    box(cv, 0, fy, w, h, 'f')
    rbox(cv, w * 0.68, h * 0.14, w * 0.92, h * 0.4, 0.3, 'i')
    box(cv, w * 0.8 - 0.5, h * 0.14, w * 0.8 + 0.5, h * 0.4, 'b')
    box(cv, w * 0.68, h * 0.27 - 0.5, w * 0.92, h * 0.27 + 0.5, 'b')
    bx = cx - w * 0.08
    path(cv, [(bx + w * 0.2, fy - 1.0), (bx + w * 0.36, fy - 1.4), (bx + w * 0.4, h * 0.62), (bx + w * 0.32, h * 0.52)], 'c', 0.7 if not big else 0.9)
    oval(cv, bx, h * 0.68, w * 0.24, h * 0.22, 'c')
    hy = h * 0.38
    disc(cv, bx, hy, s * 0.22, 'c')
    for side in (-1, 1):
        poly(cv, [(bx + side * s * 0.06, hy - s * 0.16), (bx + side * s * 0.2, hy - s * 0.34), (bx + side * s * 0.22, hy - s * 0.08)], 'c')
        poly(cv, [(bx + side * s * 0.11, hy - s * 0.17), (bx + side * s * 0.19, hy - s * 0.27), (bx + side * s * 0.19, hy - s * 0.14)], 'n')
    eyes(cv, bx, hy - s * 0.02, s * 0.09, big, 'e', 'k')
    poly(cv, [(bx - 0.8, hy + s * 0.07), (bx + 0.8, hy + s * 0.07), (bx, hy + s * 0.12)], 'n')
    for k in (-1, 0, 1):
        seg(cv, bx + k * s * 0.06, hy - s * 0.21, bx + k * s * 0.06, hy - s * 0.14, 'k', 0.4)
    for y in (0.6, 0.7, 0.8) if big else (0.62, 0.74):
        seg(cv, bx - w * 0.24, h * y, bx - w * 0.16, h * y, 'k', 0.45)
        seg(cv, bx + w * 0.24, h * y, bx + w * 0.16, h * y, 'k', 0.45)
    for side in (-1, 1):
        rbox(cv, bx + side * w * 0.08 - 1.0, fy - 2.0, bx + side * w * 0.08 + 1.0, fy + 0.4, 0.8, 'c')
    disc(cv, w * 0.84, fy - s * 0.1, s * 0.1, 'y')
    path(cv, [(w * 0.84 - s * 0.08, fy - s * 0.12), (w * 0.84 + s * 0.06, fy - s * 0.06)], 'n', 0.4)
    seg(cv, w * 0.84 - s * 0.1, fy - 0.2, w * 0.6, fy - 0.2, 'y', 0.4)
    return cv, [('b', 'wall', 'Wall', BLUE, True), role('c', 'cat', 'Cat', BROWN), role('k', 'stripes', 'Stripes and eyes', BROWN),
                role('n', 'nose', 'Nose and ears', PINK), role('e', 'eye', 'Eye whites', GREEN), role('y', 'yarn', 'Yarn', PINK),
                role('i', 'window', 'Window', BLUE), role('f', 'floor', 'Floor', GREEN)], ['animals', 'cozy']


def dog(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.12)
    hills(cv, 'g', gtop, 0.4, w * 1.4, r.uniform(0, 6))
    bx = cx + w * 0.02
    path(cv, [(bx - w * 0.2, gtop - 1), (bx - w * 0.34, h * 0.66), (bx - w * 0.36, h * 0.56)], 'd', 0.7)
    oval(cv, bx, h * 0.68, w * 0.24, h * 0.2, 'd')
    hy = h * 0.38
    oval(cv, bx, hy, s * 0.22, s * 0.2, 'd')
    for side in (-1, 1):
        oval(cv, bx + side * s * 0.24, hy + s * 0.02, s * 0.08, s * 0.17, 'r')
    oval(cv, bx + s * 0.08, hy - s * 0.06, s * 0.08, s * 0.07, 'r')
    eyes(cv, bx, hy - s * 0.07, s * 0.09, big, 'w', 'n')
    oval(cv, bx, hy + s * 0.08, s * 0.14, s * 0.09, 'm')
    oval(cv, bx, hy + s * 0.04, s * 0.05 + 0.3, s * 0.03 + 0.3, 'n')
    oval(cv, bx, hy + s * 0.17, 1.2, 1.4, 't')
    if big:
        box(cv, bx - w * 0.18, hy + s * 0.22, bx + w * 0.18, hy + s * 0.22 + 1.6, 'c', only='d')
    for side in (-1, 1):
        rbox(cv, bx + side * w * 0.09 - 1.0, gtop - 2.2, bx + side * w * 0.09 + 1.0, gtop + 0.5, 0.8, 'd')
    rbox(cv, w * 0.76, gtop - 1.0, w * 0.94, gtop + 0.2, 0.5, 'o')
    for x in (w * 0.76, w * 0.94):
        disc(cv, x, gtop - 0.9, 0.9, 'o')
    disc(cv, w * 0.12, h * 0.1, s * 0.085, 'u')
    return cv, [sky(), role('d', 'dog', 'Dog', BROWN), role('r', 'ears', 'Ears and patch', BROWN),
                role('w', 'eye_white', 'Eye whites', GREEN), role('m', 'muzzle', 'Muzzle', BROWN), role('n', 'nose', 'Eyes and nose', PINK),
                role('t', 'tongue', 'Tongue', PINK), role('c', 'collar', 'Collar', GREEN), role('o', 'bone', 'Bone', BLUE),
                role('g', 'grass', 'Grass', GREEN), role('u', 'sun', 'Sun', BROWN)], ['animals', 'garden']


def rabbit(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.12)
    hills(cv, 'g', gtop, 0.5, w * 1.3, r.uniform(0, 6))
    bx = cx - w * 0.06
    oval(cv, bx, h * 0.7, w * 0.22, h * 0.18, 'b')
    hy = h * 0.46
    disc(cv, bx, hy, s * 0.19, 'b')
    for side in (-1, 1):
        oval(cv, bx + side * s * 0.1, hy - s * 0.36, s * 0.07, s * 0.2, 'b')
        oval(cv, bx + side * s * 0.1, hy - s * 0.35, s * 0.03 + 0.2, s * 0.14, 'n')
    eyes(cv, bx, hy - s * 0.02, s * 0.08, big, 'w', 'e')
    poly(cv, [(bx - 0.8, hy + s * 0.06), (bx + 0.8, hy + s * 0.06), (bx, hy + s * 0.11)], 'n')
    path(cv, [(bx - 1.2, hy + s * 0.15), (bx, hy + s * 0.12), (bx + 1.2, hy + s * 0.15)], 'e', 0.4)
    oval(cv, bx, h * 0.72, w * 0.11, h * 0.1, 'm')
    for side in (-1, 1):
        oval(cv, bx + side * w * 0.13, gtop - 0.5, w * 0.08, 1.2, 'b')
    kx = w * 0.82
    poly(cv, [(kx - s * 0.08, gtop - s * 0.3), (kx + s * 0.08, gtop - s * 0.3), (kx, gtop + 0.4)], 'c')
    for a in (-0.4, 0.0, 0.4):
        lens(cv, kx, gtop - s * 0.3, kx + math.sin(a) * s * 0.18, gtop - s * 0.3 - math.cos(a) * s * 0.18, s * 0.07, 'l')
    for x in (w * 0.08, w * 0.62) if not big else (w * 0.06, w * 0.6, w * 0.94):
        disc(cv, x, gtop + 1.2, 0.9, 'f')
    return cv, [sky(), role('b', 'bunny', 'Bunny', BROWN), role('m', 'belly', 'Belly', BROWN), role('n', 'inner_ear', 'Ears and nose', PINK),
                role('e', 'eye', 'Eyes and mouth', BLUE), role('w', 'eye_white', 'Eye whites', GREEN), role('c', 'carrot', 'Carrot', BROWN),
                role('l', 'carrot_top', 'Carrot top', GREEN), role('f', 'flowers', 'Flowers', PINK), role('g', 'grass', 'Grass', GREEN)], ['animals', 'garden']


def bear(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.12)
    hills(cv, 'g', gtop, 0.4, w * 1.4, r.uniform(0, 6))
    for x in (w * 0.08, w * 0.92) + ((w * 0.2,) if big else ()):
        pine(cv, x, gtop, h * 0.42, s * 0.24, 't')
    bx = cx
    oval(cv, bx, h * 0.68, w * 0.26, h * 0.21, 'b')
    oval(cv, bx, h * 0.7, w * 0.14, h * 0.13, 'm')
    hy = h * 0.37
    for side in (-1, 1):
        disc(cv, bx + side * s * 0.19, hy - s * 0.17, s * 0.09, 'b')
        disc(cv, bx + side * s * 0.19, hy - s * 0.17, s * 0.045 + 0.2, 'm')
    disc(cv, bx, hy, s * 0.23, 'b')
    oval(cv, bx, hy + s * 0.09, s * 0.11, s * 0.08, 'm')
    eyes(cv, bx, hy - s * 0.04, s * 0.09, big, 'e', 'k')
    oval(cv, bx, hy + s * 0.05, s * 0.05 + 0.3, 0.8, 'k')
    for side in (-1, 1):
        oval(cv, bx + side * w * 0.12, gtop - 0.6, w * 0.09, 1.3, 'b')
    px = w * 0.8
    rbox(cv, px - s * 0.11, gtop - s * 0.22, px + s * 0.11, gtop + 0.5, 1.2, 'p')
    box(cv, px - s * 0.13, gtop - s * 0.25, px + s * 0.13, gtop - s * 0.25 + 1.0, 'y')
    box(cv, px - s * 0.06 - 0.5, gtop - s * 0.25, px - s * 0.06 + 0.5, gtop - s * 0.25 + 2.0, 'y')
    return cv, [sky(), role('b', 'bear', 'Bear', BROWN), role('m', 'muzzle', 'Muzzle and belly', BROWN), role('k', 'face', 'Eyes and nose', PINK),
                role('e', 'eye_white', 'Eye whites', BLUE), role('p', 'pot', 'Honey pot', BLUE), role('y', 'honey', 'Honey', BROWN),
                role('t', 'pine', 'Pines', GREEN), role('g', 'grass', 'Grass', GREEN)], ['animals', 'forest']


def panda(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    gtop = h - ground_rows(h, 0.12)
    for x in (w * 0.1, w * 0.88) + ((w * 0.76,) if big else ()):
        box(cv, x - 0.5, h * 0.06, x + 0.5, gtop, 'j')
        if big:
            for y in range(int(h * 0.12), int(gtop) - 1, 4):
                box(cv, x - 0.5, y, x + 0.5, y + 0.5, 'l')
        lens(cv, x + (0.6 if x < cx else -0.6), h * 0.26, x + (s * 0.28 if x < cx else -s * 0.28), h * 0.17, s * 0.11, 'l')
    hills(cv, 'd', gtop, 0.4, w * 1.4, r.uniform(0, 6))
    bx = cx
    oval(cv, bx, h * 0.68, w * 0.23, h * 0.19, 'w')
    for side in (-1, 1):
        oval(cv, bx + side * w * 0.16, h * 0.64, w * 0.07, h * 0.11, 'k')
        oval(cv, bx + side * w * 0.12, gtop - 0.6, w * 0.09, 1.4, 'k')
    hy = h * 0.37
    for side in (-1, 1):
        disc(cv, bx + side * s * 0.19, hy - s * 0.16, s * 0.08, 'k')
    disc(cv, bx, hy, s * 0.21, 'w')
    for side in (-1, 1):
        oval(cv, bx + side * s * 0.09, hy - s * 0.01, s * 0.06 + 0.3, s * 0.08 + 0.3, 'k')
        if big:
            disc(cv, bx + side * s * 0.09, hy - s * 0.02, 0.75, 'e')
    oval(cv, bx, hy + s * 0.1, s * 0.05 + 0.3, 0.8, 'k')
    seg(cv, bx + w * 0.17, h * 0.58, bx + w * 0.3, h * 0.42, 'j', 0.6)
    lens(cv, bx + w * 0.3, h * 0.42, bx + w * 0.4, h * 0.34, s * 0.08, 'l')
    if big:
        for x in (w * 0.2, w * 0.66):
            disc(cv, x, gtop + 1.5, 1.05, 'f')
    return cv, [('b', 'forest', 'Bamboo forest', GREEN, True), role('w', 'panda', 'Panda', BLUE), role('k', 'patches', 'Patches', PINK),
                role('e', 'eye', 'Eyes', BLUE), role('j', 'bamboo', 'Bamboo', GREEN), role('l', 'leaves', 'Bamboo leaves and rings', BROWN),
                role('d', 'ground', 'Ground', BROWN), role('f', 'flowers', 'Flowers', PINK)], ['animals', 'forest']


def penguin(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    sea = h * 0.7
    hills(cv, 'w', sea, 0.3, w * 0.5, r.uniform(0, 6))
    poly(cv, [(w * 0.1, sea - 0.6), (w * 0.9, sea - 0.6), (w * 0.98, h + 1), (w * 0.02, h + 1)], 'i')
    px = cx - w * 0.04
    oval(cv, px, h * 0.5, w * 0.24, h * 0.3, 'p')
    oval(cv, px, h * 0.56, w * 0.16, h * 0.23, 'b')
    for side in (-1, 1):
        lens(cv, px + side * w * 0.2, h * 0.38, px + side * w * 0.32, h * 0.64, s * 0.1, 'p')
        oval(cv, px + side * w * 0.09, sea + 0.2, w * 0.08, 1.0, 'o')
    for side in (-1, 1):
        oval(cv, px + side * s * 0.12, h * 0.31, s * 0.05 + 0.3, s * 0.06 + 0.3, 'y')
    eyes(cv, px, h * 0.28, s * 0.07, big, 'b', 'p', small='b')
    poly(cv, [(px - 1.0, h * 0.35), (px + 1.0, h * 0.35), (px, h * 0.41)], 'o')
    if big:
        fx = w * 0.82
        oval(cv, fx, sea - 1.5, w * 0.07, 1.1, 'f')
        poly(cv, [(fx + w * 0.05, sea - 1.5), (fx + w * 0.11, sea - 2.6), (fx + w * 0.11, sea - 0.4)], 'f')
    scatter(cv, 'x', 's', 5 + (w * h) // 60, r, sep=3, area=(0, 0, w - 1, sea - 3))
    return cv, [sky(), role('p', 'penguin', 'Penguin', PINK), role('b', 'belly', 'Belly', BLUE), role('y', 'cheek', 'Cheeks', BROWN),
                role('o', 'beak', 'Beak and feet', BROWN), role('i', 'ice', 'Ice floe', GREEN),
                role('w', 'sea', 'Sea', BLUE), role('f', 'fish', 'Fish', GREEN), role('x', 'snow', 'Snowflakes', PINK)], ['animals', 'winter']


def fox(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.12)
    hills(cv, 'g', gtop, 0.4, w * 1.4, r.uniform(0, 6))
    for x in (w * 0.92,) + ((w * 0.08,) if big else ()):
        pine(cv, x, gtop, h * 0.46, s * 0.26, 't')
    fx = cx - w * 0.04
    lens(cv, fx + w * 0.16, gtop - 0.5, fx + w * 0.44, h * 0.44, s * 0.24, 'f')
    lens(cv, fx + w * 0.36, h * 0.52, fx + w * 0.44, h * 0.44, s * 0.12, 'w')
    oval(cv, fx, h * 0.66, w * 0.2, h * 0.2, 'f')
    oval(cv, fx, h * 0.68, w * 0.1, h * 0.14, 'w')
    hy = h * 0.4
    poly(cv, [(fx - s * 0.26, hy - s * 0.1), (fx + s * 0.26, hy - s * 0.1), (fx, hy + s * 0.2)], 'f')
    disc(cv, fx, hy - s * 0.06, s * 0.17, 'f')
    for side in (-1, 1):
        poly(cv, [(fx + side * s * 0.06, hy - s * 0.18), (fx + side * s * 0.22, hy - s * 0.38), (fx + side * s * 0.24, hy - s * 0.12)], 'k')
        poly(cv, [(fx + side * s * 0.03, hy + s * 0.02), (fx + side * s * 0.24, hy - s * 0.1), (fx, hy + s * 0.18)], 'w')
    eyes(cv, fx, hy - s * 0.06, s * 0.08, big, 'b', 'e')
    disc(cv, fx, hy + s * 0.17, 0.8, 'e')
    for side in (-1, 1):
        rbox(cv, fx + side * w * 0.08 - 0.9, gtop - 2.0, fx + side * w * 0.08 + 0.9, gtop + 0.5, 0.6, 'k')
    for x, y in ((0.12, 0.2), (0.3, 0.12), (0.62, 0.18)) if big else ((0.14, 0.18), (0.6, 0.14)):
        lens(cv, w * x, h * y, w * x + 1.6, h * y + 1.2, 1.4, 'l')
    return cv, [sky(), role('f', 'fox', 'Fox', BROWN), role('w', 'white', 'Chest and tail tip', BLUE), role('k', 'dark', 'Ears and paws', BROWN),
                role('e', 'eye', 'Eyes and nose', PINK), role('b', 'eye_white', 'Eye whites', GREEN), role('l', 'leaves', 'Falling leaves', PINK),
                role('t', 'pine', 'Pines', GREEN), role('g', 'grass', 'Grass', GREEN)], ['animals', 'forest']


def owlets(w, h, r):
    cv, s, cx, big = start(w, h, 'n')
    by = h - ground_rows(h, 0.12)
    rbox(cv, -2, by, w + 2, h + 2, 1.5, 'r')
    for x, a in ((w * 0.06, 0.7), (w * 0.94, 2.4)):
        lens(cv, x, by, x + math.cos(a) * s * 0.22, by - abs(math.sin(a)) * s * 0.2 - 1, s * 0.1, 'l')
    for k, (ox, c) in enumerate(((w * 0.29, 'o'), (w * 0.71, 'q'))):
        ry = h * 0.24
        oy = by - ry + 0.6
        for side in (-1, 1):
            poly(cv, [(ox + side * w * 0.04, oy - ry * 0.8), (ox + side * w * 0.16, oy - ry * 1.18), (ox + side * w * 0.17, oy - ry * 0.55)], c)
        oval(cv, ox, oy, w * 0.18, ry, c)
        oval(cv, ox, oy + ry * 0.35, w * 0.1, ry * 0.45, 'b')
        for side in (-1, 1):
            disc(cv, ox + side * w * 0.08, oy - ry * 0.32, s * 0.09 + 0.2, 'e')
            ex = int(ox + side * w * 0.08)
            cv.put(ex, int(oy - ry * 0.32), 'k')
            if big:
                cv.put(ex, int(oy - ry * 0.32) + 1, 'k')
        poly(cv, [(ox - 0.8, oy - ry * 0.12), (ox + 0.8, oy - ry * 0.12), (ox, oy + ry * 0.08)], 'k')
    disc(cv, w * 0.84, h * 0.1, s * 0.09, 'm')
    for x, y in ((0.1, 0.08), (0.3, 0.05), (0.56, 0.12), (0.92, 0.3), (0.06, 0.32)):
        cv.put(int(w * x), int(h * y), 'x')
    return cv, [('n', 'dusk', 'Dusk sky', PINK, True), role('o', 'owl', 'Brown owlet', BROWN), role('q', 'owl2', 'Green owlet', GREEN),
                role('b', 'belly', 'Bellies', BROWN), role('e', 'eye', 'Eyes', BLUE), role('k', 'pupil', 'Pupils and beaks', PINK),
                role('r', 'branch', 'Branch', BROWN), role('l', 'leaves', 'Leaves', GREEN), role('m', 'moon', 'Moon', BLUE),
                role('x', 'stars', 'Stars', BLUE)], ['animals', 'night']


def turtle(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.14)
    hills(cv, 'g', gtop, 0.4, w * 1.4, r.uniform(0, 6))
    tx, ty = cx - w * 0.05, gtop - h * 0.07
    for dx in (-0.2, -0.07, 0.1, 0.22):
        rbox(cv, tx + dx * w - 0.9, ty - 0.5, tx + dx * w + 0.9, gtop + 0.5, 0.6, 'b')
    path(cv, [(tx + w * 0.24, ty - h * 0.02), (tx + w * 0.32, ty - h * 0.08), (tx + w * 0.36, ty - h * 0.14)], 'b', 0.9)
    oval(cv, tx + w * 0.38, ty - h * 0.15, s * 0.13, s * 0.1, 'b')
    poly(cv, [(tx - w * 0.3, ty), (tx - w * 0.4, ty + 0.8), (tx - w * 0.28, ty + 1.2)], 'b')
    for y in range(h):
        for x in range(w):
            dx, dy = (x + 0.5 - tx) / (w * 0.31), (y + 0.5 - ty) / (h * 0.27)
            if dy <= 0 and dx * dx + dy * dy <= 1.0:
                cv.g[y][x] = 'h'
    box(cv, tx - w * 0.33, ty - 0.6, tx + w * 0.33, ty + 0.5, 'p')
    for k in range(3 if not big else 4):
        a = math.radians(200 + k * (140 / ((3 if not big else 4) - 1)))
        oval(cv, tx + math.cos(a) * w * 0.17, ty - h * 0.08 + math.sin(a) * h * 0.08, s * 0.05 + 0.25, s * 0.04 + 0.25, 'p')
    eye(cv, tx + w * 0.4, ty - h * 0.19, big, None, 'k')
    path(cv, [(tx + w * 0.4, ty - h * 0.12), (tx + w * 0.46, ty - h * 0.13)], 'k', 0.4)
    for k, x in enumerate((w * 0.08,) if not big else (w * 0.06, w * 0.2)):
        y = h * (0.42 + 0.08 * k)
        seg(cv, x, gtop, x, y, 'g', 0.45)
        disc(cv, x, y, s * 0.08 + 0.2, 'f')
        cv.put(int(x), int(y), 'k')
    disc(cv, w * 0.86, h * 0.1, s * 0.09, 'u')
    if big:
        cloud(cv, w * 0.26, h * 0.14, s * 0.05 + 0.4, 'c')
    return cv, [sky(), role('h', 'shell', 'Shell', BROWN), role('p', 'plates', 'Shell plates', BROWN), role('b', 'skin', 'Head and legs', GREEN),
                role('k', 'eye', 'Eye, smile and flower hearts', BROWN), role('f', 'flowers', 'Flowers', PINK), role('g', 'grass', 'Grass', GREEN),
                role('u', 'sun', 'Sun', PINK), role('c', 'cloud', 'Cloud', BLUE)], ['animals', 'garden']


def whale(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    sea = h * 0.56
    hills(cv, 'w', sea, 0.4, w * 0.45, r.uniform(0, 6))
    wx, wy = cx - w * 0.04, sea + h * 0.06
    oval(cv, wx, wy, w * 0.36, h * 0.15, 'k')
    poly(cv, [(wx + w * 0.3, wy - h * 0.02), (wx + w * 0.46, wy - h * 0.16), (wx + w * 0.48, wy - h * 0.04), (wx + w * 0.38, wy + h * 0.04)], 'k')
    for y in range(h):
        for x in range(w):
            dx, dy = (x + 0.5 - (wx - w * 0.06)) / (w * 0.28), (y + 0.5 - (wy + h * 0.05)) / (h * 0.09)
            if dy >= 0 and dx * dx + dy * dy <= 1.0 and cv.g[y][x] == 'k':
                cv.g[y][x] = 'b'
    eye(cv, wx - w * 0.2, wy - h * 0.06, big, None, 'e')
    path(cv, [(wx - w * 0.3, wy + h * 0.02), (wx - w * 0.18, wy + h * 0.05), (wx - w * 0.08, wy + h * 0.03)], 'e', 0.4)
    sx = wx - w * 0.06
    seg(cv, sx, wy - h * 0.14, sx, wy - h * 0.26, 'p', 0.5)
    for side in (-1, 1):
        path(cv, [(sx, wy - h * 0.26), (sx + side * w * 0.08, wy - h * 0.32), (sx + side * w * 0.14, wy - h * 0.26)], 'p', 0.5)
    for x in (w * 0.1, w * 0.7) if not big else (w * 0.1, w * 0.5, w * 0.8):
        oval(cv, x, h * 0.88, w * 0.07, 1.1, 'f')
        poly(cv, [(x + w * 0.05, h * 0.88), (x + w * 0.11, h * 0.84), (x + w * 0.11, h * 0.92)], 'f')
    disc(cv, w * 0.86, h * 0.1, s * 0.085, 'u')
    if big:
        path(cv, [(w * 0.2 - 1, h * 0.12), (w * 0.2, h * 0.13), (w * 0.2 + 1, h * 0.12)], 'u', 0.4)
    return cv, [sky(), role('k', 'whale', 'Whale', PINK), role('b', 'belly', 'Belly', GREEN), role('e', 'eye', 'Eye and smile', BROWN),
                role('p', 'spout', 'Spout', BLUE), role('w', 'sea', 'Sea', BLUE), role('f', 'fish', 'Fish', GREEN),
                role('u', 'sun', 'Sun and gull', BROWN)], ['animals', 'sea']


def octopus(w, h, r):
    cv, s, cx, big = start(w, h, 'w')
    gtop = h - ground_rows(h, 0.1)
    for x in (w * 0.08, w * 0.92) + ((w * 0.8,) if big else ()):
        path(cv, [(x, gtop), (x + 0.8, h * 0.84), (x - 0.4, h * 0.72), (x + 0.5, h * 0.6)], 'g', 0.55)
    hills(cv, 'd', gtop, 0.5, w * 0.9, r.uniform(0, 6))
    ox, oy = cx, h * 0.36
    n = 6 if not big else 8
    for k in range(n):
        x0 = ox - w * 0.2 + k * (w * 0.4 / (n - 1))
        side = -1 if k < n / 2 else 1
        pts = [(x0, oy + h * 0.1), (x0 + side * w * 0.03, oy + h * 0.28), (x0 + side * w * 0.08, oy + h * 0.38), (x0 + side * w * 0.12, oy + h * 0.35)]
        path(cv, pts, 'o', 0.75 if not big else 0.95)
    oval(cv, ox, oy, w * 0.24, h * 0.2, 'o')
    for dx, dy in ((-0.12, -0.12), (0.1, -0.15)) if not big else ((-0.12, -0.12), (0.1, -0.15), (0.0, -0.08), (0.16, -0.02), (-0.17, 0.0)):
        disc(cv, ox + dx * w, oy + dy * h, 1.0, 'q')
    eyes(cv, ox, oy + h * 0.06, s * 0.09, big, 'e', 'k')
    path(cv, [(ox - s * 0.06, oy + h * 0.12), (ox, oy + h * 0.14), (ox + s * 0.06, oy + h * 0.12)], 'k', 0.4)
    for k in range(4):
        disc(cv, w * (0.9 - (k % 2) * 0.04), h * (0.46 - k * 0.14), 0.6 + k * 0.1, 'x')
    if big:
        star(cv, w * 0.3, gtop + 0.6, 1.6, 'k', ri=0.6)
    return cv, [('w', 'water', 'Water', BLUE, True), role('o', 'octopus', 'Octopus', PINK), role('q', 'spots', 'Spots', PINK),
                role('e', 'eye', 'Eyes', BLUE), role('k', 'pupil', 'Eyes, smile and starfish', BROWN), role('x', 'bubbles', 'Bubbles', BLUE),
                role('g', 'weed', 'Seaweed', GREEN), role('d', 'sand', 'Sand', BROWN)], ['animals', 'sea']


def elephant(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.14)
    hills(cv, 'g', gtop, 0.4, w * 1.6, r.uniform(0, 6))
    tx = w * 0.1
    box(cv, tx - 0.5, h * 0.4, tx + 0.5, gtop + 0.5, 't')
    oval(cv, tx, h * 0.37, w * 0.11, h * 0.05, 'a')
    ex, ey = cx - w * 0.06, gtop - h * 0.24
    for dx in (-0.2, -0.08, 0.1, 0.2):
        rbox(cv, ex + dx * w - 1.1, ey, ex + dx * w + 1.1, gtop + 0.5, 0.8, 'e')
    oval(cv, ex, ey, w * 0.3, h * 0.16, 'e')
    hx, hy = ex + w * 0.26, ey - h * 0.1
    disc(cv, hx, hy, s * 0.17, 'e')
    path(cv, [(hx + s * 0.1, hy + s * 0.05), (hx + s * 0.2, hy + s * 0.25), (hx + s * 0.22, hy + s * 0.42), (hx + s * 0.3, hy + s * 0.46)], 'e', 0.9 if not big else 1.2)
    oval(cv, hx - s * 0.08, hy + s * 0.03, s * 0.12, s * 0.16, 'r')
    eye(cv, hx + s * 0.08, hy - s * 0.07, big, None, 'k')
    for dx in (-0.2, -0.08, 0.1, 0.2):
        cv.put(int(ex + dx * w), int(gtop - 0.5), 'k')
    path(cv, [(hx + s * 0.06, hy + s * 0.12), (hx + s * 0.16, hy + s * 0.2), (hx + s * 0.24, hy + s * 0.18)], 't', 0.5)
    path(cv, [(ex - w * 0.3, ey), (ex - w * 0.36, ey + h * 0.08)], 'e', 0.4)
    disc(cv, w * 0.86, h * 0.12, s * 0.1, 'u')
    return cv, [('s', 'sky', 'Sunset sky', PINK, True), role('e', 'elephant', 'Elephant', BLUE), role('r', 'ear', 'Ear', BLUE),
                role('k', 'eye', 'Eye and toenails', BROWN), role('t', 'tusk', 'Tusk and tree', BROWN), role('a', 'acacia', 'Acacia', GREEN),
                role('g', 'savanna', 'Savanna', BROWN), role('u', 'sun', 'Sun', PINK)], ['animals', 'savanna']


def giraffe(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.1)
    box(cv, 0, gtop, w, h, 'g')
    tx = w * 0.84
    box(cv, tx - 0.6, h * 0.42, tx + 0.6, gtop + 0.5, 'k')
    oval(cv, tx, h * 0.4, w * 0.14, h * 0.06, 'a')
    gx, gy = cx - w * 0.12, gtop - h * 0.3
    for dx in (-0.12, -0.04, 0.06, 0.13):
        box(cv, gx + dx * w - 0.5, gy, gx + dx * w + 0.5, gtop + 0.5, 'b')
    oval(cv, gx, gy, w * 0.2, h * 0.08, 'b')
    nx, ny = gx + w * 0.16, h * 0.16
    poly(cv, [(gx + w * 0.06, gy - h * 0.04), (gx + w * 0.18, gy), (nx + 1.2, ny), (nx - 0.6, ny)], 'b')
    oval(cv, nx + w * 0.04, ny - h * 0.01, s * 0.13, s * 0.09, 'b')
    for side in (-0.6, 0.8):
        seg(cv, nx + side, ny - s * 0.06, nx + side, ny - s * 0.13, 'k', 0.4)
    eye(cv, nx + w * 0.06, ny - h * 0.04, big, None, 'k')
    scatter(cv, 'p', 'b', 5 + (w * h) // 45, r, sep=2, area=(0, 0, w - 1, gy + h * 0.06))
    path(cv, [(gx - w * 0.2, gy), (gx - w * 0.26, gy + h * 0.1)], 'k', 0.4)
    for x in range(int(gx - w * 0.12), int(gx + w * 0.14) + 1):
        if cv.g[int(gtop) - 1][x] == 'b':
            cv.g[int(gtop) - 1][x] = 'k'
    disc(cv, w * 0.12, h * 0.1, s * 0.085, 'u')
    return cv, [sky(), role('b', 'giraffe', 'Giraffe', BROWN), role('p', 'spots', 'Spots', BROWN), role('k', 'horns', 'Horns, eye, hooves and tree', BROWN),
                role('a', 'acacia', 'Acacia', GREEN), role('g', 'grass', 'Grass', GREEN),
                role('u', 'sun', 'Sun', PINK)], ['animals', 'savanna']


def pig(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.14)
    hills(cv, 'g', gtop, 0.4, w * 1.4, r.uniform(0, 6))
    oval(cv, cx, gtop + 0.8, w * 0.36, h * 0.06, 'm')
    for x in range(0, w, 3):
        box(cv, x, h * 0.58, x + 0.9, gtop, 'f')
    box(cv, 0, h * 0.62, w, h * 0.62 + 0.8, 'f')
    px, py = cx - w * 0.04, gtop - h * 0.16
    for dx in (-0.16, -0.06, 0.08, 0.17):
        rbox(cv, px + dx * w - 0.8, py, px + dx * w + 0.8, gtop + 0.5, 0.5, 'p')
    oval(cv, px, py, w * 0.27, h * 0.15, 'p')
    hx, hy = px + w * 0.24, py - h * 0.08
    disc(cv, hx, hy, s * 0.15, 'p')
    poly(cv, [(hx - s * 0.08, hy - s * 0.1), (hx - s * 0.02, hy - s * 0.24), (hx + s * 0.04, hy - s * 0.1)], 'n')
    oval(cv, hx + s * 0.13, hy + s * 0.03, s * 0.06 + 0.3, s * 0.07 + 0.3, 'n')
    cv.put(int(hx + s * 0.1), int(hy + s * 0.03), 'k')
    cv.put(int(hx + s * 0.18), int(hy + s * 0.03), 'k')
    eye(cv, hx + s * 0.02, hy - s * 0.09, big, None, 'k')
    path(cv, [(hx + s * 0.02, hy + s * 0.12), (hx + s * 0.1, hy + s * 0.14)], 'k', 0.4)
    path(cv, [(px - w * 0.27, py - 0.5), (px - w * 0.33, py - h * 0.05), (px - w * 0.3, py - h * 0.09), (px - w * 0.36, py - h * 0.1)], 'n', 0.45)
    disc(cv, w * 0.12, h * 0.1, s * 0.085, 'u')
    return cv, [sky(), role('p', 'pig', 'Pig', PINK), role('n', 'snout', 'Snout, ear and tail', PINK),
                role('k', 'nostril', 'Eye, nostrils and mouth', BROWN), role('m', 'mud', 'Mud', BROWN), role('f', 'fence', 'Fence', BROWN),
                role('g', 'grass', 'Grass', GREEN), role('u', 'sun', 'Sun', BLUE)], ['animals', 'farm']


def chick(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.12)
    hills(cv, 'g', gtop, 0.4, w * 1.4, r.uniform(0, 6))
    ex, ey = cx, gtop - h * 0.08
    oval(cv, ex, ey, w * 0.24, h * 0.13, 'q')
    box(cv, 0, ey - h * 0.14, w, ey, 's', only='q')
    poly(cv, [(ex - w * 0.24, ey), (ex - w * 0.16, ey - h * 0.05), (ex - w * 0.08, ey), (ex, ey - h * 0.05), (ex + w * 0.08, ey),
              (ex + w * 0.16, ey - h * 0.05), (ex + w * 0.24, ey)], 'q')
    cy = ey - h * 0.16
    disc(cv, cx, cy, s * 0.2, 'c')
    disc(cv, cx, cy - s * 0.26, s * 0.15, 'c')
    for side in (-1, 1):
        lens(cv, cx + side * s * 0.12, cy - s * 0.02, cx + side * s * 0.28, cy - s * 0.12, s * 0.09, 'w')
    eyes(cv, cx, cy - s * 0.32, s * 0.07, big, 'x', 'b')
    poly(cv, [(cx - 0.9, cy - s * 0.24), (cx + 0.9, cy - s * 0.24), (cx, cy - s * 0.16)], 'b')
    path(cv, [(cx, cy - s * 0.41), (cx + 0.6, cy - s * 0.46), (cx - 0.3, cy - s * 0.5)], 'w', 0.45)
    for x in (w * 0.1, w * 0.86) if not big else (w * 0.08, w * 0.88, w * 0.72):
        seg(cv, x, gtop, x, gtop - h * 0.1, 'g', 0.45)
        disc(cv, x, gtop - h * 0.11, s * 0.07, 'f')
    disc(cv, w * 0.86, h * 0.1, s * 0.085, 'u')
    return cv, [sky(), role('c', 'chick', 'Chick', BROWN), role('w', 'wing', 'Wings and tuft', BROWN), role('b', 'beak', 'Beak and eyes', PINK),
                role('x', 'eye_white', 'Eye whites', GREEN), role('q', 'shell', 'Egg shell', BLUE),
                role('f', 'flowers', 'Flowers', PINK), role('g', 'grass', 'Grass', GREEN), role('u', 'sun', 'Sun', PINK)], ['animals', 'farm']


def crab(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    hills(cv, 'w', h * 0.5, 0.3, w * 0.5, r.uniform(0, 6))
    hills(cv, 'd', h * 0.66, 0.6, w * 1.3, r.uniform(0, 6))
    bx, by = cx, h * 0.72
    for side in (-1, 1):
        for k in range(3):
            seg(cv, bx + side * w * 0.14, by + k * 1.1 - 0.5, bx + side * w * 0.24, by + k * 1.3 + 1.0, 'c', 0.45)
        path(cv, [(bx + side * w * 0.18, by - 0.5), (bx + side * w * 0.28, by - h * 0.12), (bx + side * w * 0.32, by - h * 0.2)], 'c', 0.6)
        disc(cv, bx + side * w * 0.33, by - h * 0.24, s * 0.09, 'p')
        if big:
            poly(cv, [(bx + side * w * 0.33, by - h * 0.24), (bx + side * w * 0.44, by - h * 0.32), (bx + side * w * 0.42, by - h * 0.22)], 's')
        seg(cv, bx + side * s * 0.06, by - h * 0.08, bx + side * s * 0.07, by - h * 0.15, 'c', 0.45)
        disc(cv, bx + side * s * 0.07, by - h * 0.17, s * 0.04 + 0.4, 'e')
        cv.put(int(bx + side * s * 0.07), int(by - h * 0.17), 'k')
    oval(cv, bx, by, w * 0.18, h * 0.09, 'c')
    path(cv, [(bx - s * 0.06, by + 0.4), (bx, by + 1.0), (bx + s * 0.06, by + 0.4)], 'k', 0.4)
    if big:
        star(cv, w * 0.88, h * 0.78, s * 0.08 + 0.5, 'q', ri=0.8)
        disc(cv, w * 0.1, h * 0.8, s * 0.06 + 0.3, 'o')
    disc(cv, w * 0.84, h * 0.12, s * 0.085, 'u')
    return cv, [sky(), role('c', 'crab', 'Crab', PINK), role('p', 'claw', 'Claws', PINK), role('e', 'eye', 'Eyes', BLUE), role('k', 'pupil', 'Pupils and smile', BROWN),
                role('q', 'starfish', 'Starfish', PINK), role('o', 'shell', 'Shell', GREEN), role('w', 'sea', 'Sea', BLUE),
                role('d', 'sand', 'Sand', BROWN), role('u', 'sun', 'Sun', BROWN)], ['animals', 'beach']


def parrot(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    for x, y, a in ((0.04, 0.12, 0.5), (0.96, 0.2, 2.6)) + (((0.04, 0.5, -0.4), (0.96, 0.52, 3.6)) if big else ()):
        lens(cv, w * x, h * y, w * x + math.cos(a) * s * 0.36, h * y + math.sin(a) * s * 0.22, s * 0.16, 'l')
    by = h * 0.74
    gtop = h - ground_rows(h, 0.1)
    hills(cv, 'm', gtop, 0.5, w * 0.9, r.uniform(0, 6))
    px, py = cx - w * 0.04, h * 0.44
    path(cv, [(-1, by + 0.6), (w * 0.45, by), (w * 0.78, by - 0.6)], 'r', 0.8 if not big else 1.1)
    poly(cv, [(px - w * 0.08, py + h * 0.12), (px + w * 0.06, py + h * 0.12), (px + w * 0.01, gtop - 0.8), (px - w * 0.1, gtop - 0.8)], 'y')
    oval(cv, px, py, w * 0.17, h * 0.2, 'p')
    disc(cv, px + w * 0.05, py - h * 0.2, s * 0.17, 'p')
    lens(cv, px - w * 0.11, py - h * 0.1, px + w * 0.02, py + h * 0.2, s * 0.18, 'w')
    poly(cv, [(px + w * 0.14, py - h * 0.29), (px + w * 0.32, py - h * 0.21), (px + w * 0.26, py - h * 0.1), (px + w * 0.14, py - h * 0.13)], 'b')
    eye(cv, px + w * 0.08, py - h * 0.25, big, 'e', 'k')
    for dx in (-0.04, 0.04):
        seg(cv, px + dx * w, py + h * 0.18, px + dx * w, by - 0.6, 'k', 0.45)
    disc(cv, w * 0.84, h * 0.86, s * 0.06 + 0.4, 'f')
    if big:
        disc(cv, w * 0.12, h * 0.86, s * 0.06 + 0.4, 'f')
    return cv, [sky(), role('l', 'leaves', 'Leaves', GREEN), role('r', 'branch', 'Branch', BROWN), role('p', 'parrot', 'Parrot', PINK),
                role('w', 'wing', 'Wing', BLUE), role('y', 'tail', 'Tail', BROWN), role('b', 'beak', 'Beak', BROWN),
                role('k', 'eye', 'Eye and feet', PINK), role('e', 'eye_white', 'Eye white', GREEN), role('f', 'flowers', 'Flowers', PINK),
                role('m', 'ferns', 'Ferns', GREEN)], ['animals', 'jungle']


def sheep(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.14)
    hills(cv, 'g', gtop, 0.5, w * 1.4, r.uniform(0, 6))
    for x in (w * 0.08, w * 0.92):
        oval(cv, x, gtop - 0.4, s * 0.1, s * 0.08, 'v')
    sx, sy = cx - w * 0.04, gtop - h * 0.2
    for dx in (-0.14, -0.05, 0.06, 0.14):
        box(cv, sx + dx * w - 0.5, sy, sx + dx * w + 0.5, gtop + 0.5, 'f')
    for k in range(7 if not big else 9):
        a = math.radians(k * 360 / (7 if not big else 9))
        disc(cv, sx + math.cos(a) * w * 0.17, sy + math.sin(a) * h * 0.09, s * 0.12, 'w')
    oval(cv, sx, sy, w * 0.2, h * 0.1, 'w')
    hx, hy = sx + w * 0.26, sy - h * 0.08
    oval(cv, hx, hy, s * 0.1, s * 0.13, 'f')
    poly(cv, [(hx - s * 0.08, hy - s * 0.06), (hx - s * 0.22, hy - s * 0.02), (hx - s * 0.1, hy + s * 0.02)], 'f')
    disc(cv, hx - s * 0.02, hy - s * 0.12, s * 0.08, 'w')
    eye(cv, hx + s * 0.03, hy - s * 0.04, big, None, 'w')
    for x in (w * 0.2, w * 0.7) if not big else (w * 0.18, w * 0.5, w * 0.8):
        disc(cv, x, gtop + 1.4, 0.9, 'o')
    cloud(cv, w * 0.2, h * 0.14, s * 0.07 + 0.4, 'c')
    return cv, [sky(), role('w', 'wool', 'Wool', PINK), role('f', 'face', 'Face and legs', BROWN),
                role('o', 'flowers', 'Flowers', BROWN), role('v', 'bush', 'Bushes', GREEN),
                role('g', 'meadow', 'Meadow', GREEN), role('c', 'cloud', 'Cloud', BLUE)], ['animals', 'farm']


def lion(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.12)
    hills(cv, 'g', gtop, 0.4, w * 1.4, r.uniform(0, 6))
    lx = cx
    oval(cv, lx, h * 0.7, w * 0.22, h * 0.18, 'b')
    path(cv, [(lx + w * 0.18, gtop - 1), (lx + w * 0.34, gtop - 1.6), (lx + w * 0.38, h * 0.6)], 'b', 0.6)
    disc(cv, lx + w * 0.38, h * 0.58, 1.0, 'm')
    hy = h * 0.38
    for k in range(10):
        a = math.radians(150 + k * 26.7)
        disc(cv, lx + math.cos(a) * s * 0.24, hy + math.sin(a) * s * 0.24, s * 0.1, 'm')
    for y in range(h):
        for x in range(w):
            dx, dy = x + 0.5 - lx, y + 0.5 - hy
            if dx * dx + dy * dy <= (s * 0.27) ** 2 and (dy < s * 0.1 or abs(dx) > s * 0.12):
                cv.g[y][x] = 'm'
    disc(cv, lx, hy, s * 0.17, 'b')
    box(cv, lx - s * 0.14, hy, lx + s * 0.14, h * 0.6, 'b')
    for side in (-1, 1):
        disc(cv, lx + side * s * 0.13, hy - s * 0.15, s * 0.05 + 0.3, 'b')
    oval(cv, lx, hy + s * 0.1, s * 0.08, s * 0.07, 'z')
    eyes(cv, lx, hy - s * 0.08, s * 0.07, big, 'x', 'n')
    poly(cv, [(lx - 1.0, hy + s * 0.01), (lx + 1.0, hy + s * 0.01), (lx, hy + s * 0.07)], 'n')
    for side in (-1, 1):
        rbox(cv, lx + side * w * 0.08 - 1.0, gtop - 2.0, lx + side * w * 0.08 + 1.0, gtop + 0.5, 0.8, 'b')
    disc(cv, w * 0.86, h * 0.1, s * 0.085, 'u')
    return cv, [sky(), role('b', 'lion', 'Lion', BROWN), role('m', 'mane', 'Mane', BROWN), role('z', 'muzzle', 'Muzzle', BLUE),
                role('x', 'eye_white', 'Eye whites', BLUE), role('n', 'nose', 'Eyes and nose', PINK),
                role('g', 'grass', 'Grass', GREEN), role('u', 'sun', 'Sun', BROWN)], ['animals', 'savanna']


def mouse(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    fy = h * 0.86
    box(cv, 0, fy, w, h, 'f')
    rbox(cv, w * 0.66, h * 0.58, w * 0.92, fy + 1, w * 0.13, 'h')
    mx, my = cx - w * 0.12, fy - h * 0.14
    path(cv, [(mx - w * 0.18, my + h * 0.08), (mx - w * 0.32, my + h * 0.04), (mx - w * 0.36, my - h * 0.08), (mx - w * 0.3, my - h * 0.14)], 't', 0.45)
    oval(cv, mx, my, w * 0.2, h * 0.13, 'm')
    hx, hy = mx + w * 0.16, my - h * 0.1
    disc(cv, hx, hy, s * 0.13, 'm')
    poly(cv, [(hx + s * 0.08, hy - s * 0.06), (hx + s * 0.28, hy + s * 0.04), (hx + s * 0.08, hy + s * 0.1)], 'm')
    for side in (-1, 1):
        disc(cv, hx + side * s * 0.1 - s * 0.02, hy - s * 0.15, s * 0.09, 'm')
        disc(cv, hx + side * s * 0.1 - s * 0.02, hy - s * 0.15, s * 0.045 + 0.2, 't')
    disc(cv, hx + s * 0.27, hy + s * 0.04, 0.8, 't')
    eye(cv, hx + s * 0.06, hy - s * 0.06, big, None, 'k')
    for side in (-1, 1):
        rbox(cv, mx + side * w * 0.1 - 0.8, fy - 1.4, mx + side * w * 0.1 + 0.8, fy + 0.4, 0.5, 'm')
    kx = w * 0.84
    poly(cv, [(kx - s * 0.22, fy + 0.3), (kx + s * 0.14, fy + 0.3), (kx + s * 0.14, fy - s * 0.3)], 'c')
    for dx, dy in ((0.02, -0.06), (0.08, -0.14), (-0.06, -0.02)):
        cv.put(int(kx + dx * s), int(fy + dy * s), 'k')
    return cv, [('b', 'wall', 'Wall', GREEN, True), role('m', 'mouse', 'Mouse', BLUE), role('t', 'pink', 'Ears, nose and tail', PINK),
                role('c', 'cheese', 'Cheese', BROWN), role('k', 'holes', 'Eye and cheese holes', BROWN),
                role('h', 'hole', 'Mouse hole', PINK), role('f', 'floor', 'Floor', GREEN)], ['animals', 'cozy']


ANIMAL_SUBJECTS = [cat, dog, rabbit, bear, panda, penguin, fox, owlets, turtle, whale, octopus, elephant, giraffe, pig,
                   chick, crab, parrot, sheep, lion, mouse]

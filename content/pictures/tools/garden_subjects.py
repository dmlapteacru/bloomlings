"""The garden subjects of sketch_pictures.py redrawn for the bigger boards (the owner, 2026-10-06: regular boards of
224-288 cells from Level 11, big boards up to 22 x 28 from Level 525). Same subjects and names as the first set, so
their ids continue (frog_06 follows frog_05), with more detail where the cells allow it and roles spread over three or
four color groups, so that most of them can carry six distinct variants. The first set in sketch_pictures.py is left
as it is: its pictures stay byte-identical.
"""
import math

from picture_kit import (BLUE, BROWN, GREEN, PINK, box, disc, dots, ground_rows, heart, hills, lens, oval, path, poly,
                         rbox, ring, role, scatter, seg, sky, start)


def flower_pot(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gr = ground_rows(h, 0.07)
    box(cv, 0, h - gr, w, h, 'g')
    pt, pb = h * 0.66, h - gr
    fy = h * 0.3
    seg(cv, cx, pt, cx, fy, 'l', 0.6 if big else 0.5)
    lens(cv, cx, h * 0.57, cx - w * 0.28, h * 0.47, s * 0.13, 'l')
    lens(cv, cx, h * 0.5, cx + w * 0.28, h * 0.41, s * 0.13, 'l')
    if big:
        for side in (-1, 1):
            path(cv, [(cx, h * 0.55), (cx + side * w * 0.2, h * 0.45), (cx + side * w * 0.27, h * 0.33)], 'l', 0.5)
            oval(cv, cx + side * w * 0.27, h * 0.31, s * 0.07, s * 0.09, 'p')
    poly(cv, [(cx - w * 0.25, pt), (cx + w * 0.25, pt), (cx + w * 0.18, pb), (cx - w * 0.18, pb)], 'o')
    box(cv, cx - w * 0.25 - 0.8, pt - (1.6 if big else 1.0), cx + w * 0.25 + 0.8, pt + (0.9 if big else 0.4), 'm')
    if big:
        box(cv, cx - w * 0.2, pt + h * 0.12, cx + w * 0.2, pt + h * 0.12 + 1, 'm')
    d = s * 0.15
    for k in range(6):
        a = math.radians(k * 60 + 90)
        disc(cv, cx + math.cos(a) * d, fy + math.sin(a) * d, s * 0.1, 'p')
    disc(cv, cx, fy, s * 0.09, 'c')
    disc(cv, w * 0.87, h * 0.08, s * 0.085, 'u')
    return cv, [sky(), role('p', 'petal', 'Petals', PINK), role('c', 'center', 'Center', BROWN),
                role('l', 'leaf', 'Leaves', GREEN), role('o', 'pot', 'Pot', BROWN), role('m', 'rim', 'Pot rim', PINK),
                role('g', 'sill', 'Windowsill', GREEN), role('u', 'sun', 'Sun', BROWN)], ['flowers', 'garden']


def daisy_field(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    phase = r.uniform(0, 6)
    n = 3 if w < 16 else 4 if w < 19 else 5
    pr = 1.75 if not big else 2.3
    for i in range(n):
        x = (i + 0.5) * w / n + r.uniform(-0.4, 0.4)
        top = h * (0.22 + 0.26 * ((i * 0.618 + r.random() * 0.3) % 1.0))
        seg(cv, x, top, x, h, 't', 0.5)
        if big:
            lens(cv, x, h * 0.8, x + (1 if i % 2 else -1) * 2.6, h * 0.72, 1.4, 't')
        petal = 'p' if i % 2 == 0 else 'q'
        if big:
            for k in range(8):
                a = math.radians(k * 45)
                disc(cv, x + math.cos(a) * 1.5, top + math.sin(a) * 1.5, 1.0, petal)
        else:
            disc(cv, x, top, pr, petal)
        disc(cv, x, top, 0.8 if not big else 1.05, 'c')
    hills(cv, 'g', h - ground_rows(h, 0.14), 0.7, w * 0.9, phase)
    disc(cv, w * 0.12, h * 0.08, s * 0.09, 'u')
    return cv, [sky(), role('g', 'grass', 'Grass', GREEN), role('t', 'stem', 'Stems', GREEN),
                role('p', 'petal', 'Petals', PINK), role('q', 'petal2', 'Other petals', PINK),
                role('c', 'center', 'Centers', BROWN), role('u', 'sun', 'Sun', BROWN)], ['flowers', 'garden']


def tulip_bed(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    soil = ground_rows(h, 0.12)
    if big:
        for x in range(1, w, 3):
            rbox(cv, x, h * 0.6, x + 1.9, h - soil, 0.9, 'k')
    box(cv, 0, h - soil, w, h, 'd')
    n = max(3, int(w / 3.4))
    cw, ch = (1.3, 1.6) if not big else (1.7, 2.1)
    for i in range(n):
        x = (i + 0.5) * w / n
        top = h * r.uniform(0.28, 0.42)
        seg(cv, x, top + 1, x, h - soil, 'l', 0.5)
        lens(cv, x, h - soil, x + (-1 if i % 2 else 1) * w * 0.12, h - soil - h * 0.18, 1.3 if not big else 1.8, 'f')
        c = 'p' if i % 2 == 0 else 'q'
        poly(cv, [(x - cw, top - ch), (x - cw * 0.35, top - ch * 0.35), (x, top - ch * 1.05), (x + cw * 0.35, top - ch * 0.35),
                  (x + cw, top - ch), (x + cw * 1.05, top + ch * 0.2), (x + cw * 0.6, top + ch), (x - cw * 0.6, top + ch),
                  (x - cw * 1.05, top + ch * 0.2)], c)
    disc(cv, w * 0.86, h * 0.09, s * 0.09, 'u')
    return cv, [sky(), role('p', 'tulip', 'Tulips', PINK), role('q', 'tulip2', 'Other tulips', PINK),
                role('l', 'stem', 'Stems', GREEN), role('f', 'leaf', 'Leaves', GREEN), role('d', 'soil', 'Soil', BROWN),
                role('u', 'sun', 'Sun', BROWN), role('k', 'fence', 'Fence', BLUE)], ['flowers', 'garden']


def fruit_tree(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.12)
    hills(cv, 'g', gtop, 0.5, w * 1.2, r.uniform(0, 6))
    box(cv, cx - (1.0 if not big else 1.6), h * 0.5, cx + (1.0 if not big else 1.6), gtop + 1, 't')
    if big:
        seg(cv, cx, h * 0.6, cx - w * 0.18, h * 0.45, 't', 0.6)
        seg(cv, cx, h * 0.58, cx + w * 0.17, h * 0.44, 't', 0.6)
    disc(cv, cx, h * 0.3, s * 0.32, 'c')
    disc(cv, cx - s * 0.2, h * 0.41, s * 0.2, 'c')
    disc(cv, cx + s * 0.2, h * 0.41, s * 0.2, 'c')
    fruit = scatter(cv, 'f', 'c', 6 + (w * h) // 90, r, sep=3, area=(1, 1, w - 2, h * 0.58))
    if big:
        for x, y in fruit:
            box(cv, x, y, x + 1.9, y + 1.9, 'f', only='cf')
    k = 'b'
    bx = cx + w * 0.26
    rbox(cv, bx - s * 0.13, gtop - s * 0.12, bx + s * 0.13, gtop + 0.9, 0.8, k)
    for i in range(3):
        cv.put(int(bx - 1 + i), int(gtop - s * 0.12) - 1, 'f')
    if big:
        scatter(cv, 'q', 'g', 4, r, sep=3, area=(0, gtop + 1, w - 1, h - 2))
    disc(cv, w * 0.1, h * 0.08, s * 0.08, 'u')
    return cv, [sky(), role('c', 'crown', 'Crown', GREEN), role('f', 'fruit', 'Fruit', PINK),
                role('t', 'trunk', 'Trunk', BROWN), role('g', 'grass', 'Grass', GREEN), role('b', 'basket', 'Basket', BROWN),
                role('u', 'sun', 'Sun', BROWN), role('q', 'flowers', 'Flowers', PINK)], ['fruit', 'garden']


def apple(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    ty = h * 0.8
    box(cv, 0, ty, w, h, 'd')
    oval(cv, cx, ty, w * 0.36, 1.3 if not big else 1.8, 'p')
    dots(cv, 'e', 'd', 4, 2, area=(0, ty, w - 1, h - 1))
    rr = s * 0.34
    cy = ty - rr * 0.95
    oval(cv, cx, cy, rr, rr * 0.95, 'a')
    disc(cv, cx - rr * 0.42, cy - rr * 0.35, rr * 0.6, 'a')
    disc(cv, cx + rr * 0.42, cy - rr * 0.35, rr * 0.6, 'a')
    disc(cv, cx, cy - rr * 1.02, rr * 0.24, 'b')
    seg(cv, cx, cy - rr * 0.75, cx + 0.8, cy - rr * 1.3, 't', 0.55)
    lens(cv, cx + 0.7, cy - rr * 1.05, cx + rr * 0.95, cy - rr * 1.4, s * 0.16, 'l')
    if big:
        oval(cv, cx - rr * 0.5, cy - rr * 0.25, rr * 0.13, rr * 0.28, 'h')
    return cv, [('b', 'background', 'Background', BLUE, True), role('a', 'apple', 'Apple', BROWN),
                role('t', 'stalk', 'Stalk', BROWN), role('l', 'leaf', 'Leaf', GREEN), role('p', 'plate', 'Plate', PINK),
                role('d', 'cloth', 'Tablecloth', GREEN), role('e', 'dots', 'Cloth dots', BLUE), role('h', 'shine', 'Shine', PINK)], ['fruit']


def pear(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    ty = h * 0.82
    box(cv, 0, ty, w, h, 'k')
    oval(cv, cx, ty, w * 0.36, 1.3 if not big else 1.8, 'd')
    dots(cv, 'm', 'k', 4, 2, area=(0, ty, w - 1, h - 1))
    poly(cv, [(cx - s * 0.14, h * 0.32), (cx + s * 0.14, h * 0.32), (cx + s * 0.3, h * 0.58), (cx - s * 0.3, h * 0.58)], 'p')
    disc(cv, cx, ty - s * 0.3, s * 0.31, 'p')
    disc(cv, cx, h * 0.34, s * 0.17, 'p')
    seg(cv, cx, h * 0.19, cx - 0.4, h * 0.08, 't', 0.55)
    lens(cv, cx - 0.2, h * 0.14, cx - s * 0.36, h * 0.06, s * 0.13, 'l')
    if big:
        oval(cv, cx + s * 0.14, ty - s * 0.36, s * 0.05, s * 0.12, 'h')
    return cv, [('b', 'background', 'Background', PINK, True), role('p', 'pear', 'Pear', GREEN),
                role('l', 'leaf', 'Leaf', GREEN), role('t', 'stalk', 'Stalk', BROWN), role('d', 'plate', 'Plate', BLUE),
                role('k', 'table', 'Table', BROWN), role('m', 'dots', 'Table dots', BROWN),
                role('h', 'shine', 'Shine', BLUE)], ['fruit']


def cherries(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    ty = h * 0.86
    box(cv, 0, ty, w, h, 'd')
    oval(cv, cx, ty, w * 0.38, 1.3 if not big else 1.8, 'p')
    top = (cx + s * 0.06, h * 0.2)
    cr = s * (0.15 if not big else 0.165)
    pts = [(cx - s * 0.25, ty - cr - 0.6), (cx + s * 0.26, ty - cr - 1.6)]
    for x, y in pts:
        path(cv, [(x, y), ((x + top[0]) / 2 + 0.4, (y + top[1]) / 2), top], 't', 0.5 if not big else 0.6)
    lens(cv, top[0], top[1], top[0] + s * 0.36, top[1] - h * 0.06, s * 0.15, 'l')
    lens(cv, top[0], top[1], top[0] - s * 0.28, top[1] - h * 0.1, s * 0.12, 'l')
    if big:
        seg(cv, top[0] + s * 0.06, top[1] - h * 0.03, top[0] + s * 0.3, top[1] - h * 0.055, 'v', 0.4)
    for x, y in pts:
        disc(cv, x, y, cr, 'c')
        if big:
            box(cv, x - cr * 0.6, y - cr * 0.6, x - cr * 0.1, y - cr * 0.3, 'h')
    return cv, [('b', 'background', 'Background', BLUE, True), role('c', 'cherry', 'Cherries', PINK),
                role('t', 'stalk', 'Stalks', GREEN), role('l', 'leaf', 'Leaves', GREEN), role('p', 'plate', 'Plate', BROWN),
                role('d', 'table', 'Table', BROWN), role('h', 'shine', 'Shine', BLUE), role('v', 'vein', 'Leaf vein', PINK)], ['fruit']


def strawberry(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    ty = h * 0.87
    box(cv, 0, ty, w, h, 'k')
    oval(cv, cx, ty, w * 0.38, 1.3 if not big else 1.8, 'p')
    top, bot = h * 0.27, ty - 0.6
    half = w * 0.38
    for y in range(h):
        py = y + 0.5
        t = (py - top) / (bot - top)
        if 0 <= t <= 1:
            hw = half * (1 - t) ** 0.55 * min(1.0, 0.62 + 2.2 * t)
            for x in range(w):
                if abs(x + 0.5 - cx) <= hw:
                    cv.g[y][x] = 's'
    n = 5 if not big else 7
    for k in range(n):
        x = cx - half * 0.95 + (k + 0.5) * (2 * half * 0.95) / n
        poly(cv, [(x - half * 0.95 / n, top - 0.3), (x + half * 0.95 / n, top - 0.3), (x, top + s * (0.12 if k % 2 else 0.17))], 'l')
    box(cv, cx - half * 0.7, top - 1.2, cx + half * 0.7, top - 0.1, 'l')
    seg(cv, cx, top - 0.6, cx + 0.8, top - h * 0.1, 'l', 0.55)
    scatter(cv, 'd', 's', 6 + (w * h) // 50, r, sep=2, area=(0, top + s * 0.2, w - 1, bot - 1))
    fx, fy = w * 0.85, h * 0.12
    for k in range(5):
        a = math.radians(k * 72 - 90)
        disc(cv, fx + math.cos(a) * s * 0.07, fy + math.sin(a) * s * 0.07, s * 0.06, 'f')
    if big:
        disc(cv, fx, fy, 1.0, 'o')
    return cv, [('b', 'background', 'Background', BLUE, True), role('s', 'berry', 'Berry', PINK),
                role('d', 'seed', 'Seeds', BROWN), role('l', 'leaves', 'Leaves', GREEN), role('p', 'plate', 'Plate', BROWN),
                role('k', 'cloth', 'Tablecloth', GREEN), role('f', 'blossom', 'Blossom', PINK),
                role('o', 'blossom_center', 'Blossom center', BROWN)], ['fruit']


def grapes(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    ty = h * 0.9
    box(cv, 0, ty, w, h, 'k')
    rows = [4, 3, 3, 2, 1] if not big else [5, 4, 4, 3, 2, 1]
    gr = (w * 0.74) / (2 * rows[0])
    y0 = h * 0.32
    seg(cv, cx, y0, cx + 0.6, h * 0.12, 't', 0.55)
    lens(cv, cx + 0.4, h * 0.2, cx + s * 0.42, h * 0.12, s * 0.16, 'l')
    path(cv, [(cx, h * 0.15), (cx - s * 0.18, h * 0.08), (cx - s * 0.32, h * 0.14), (cx - s * 0.38, h * 0.24)], 'v', 0.45)
    for row, n in enumerate(rows):
        for i in range(n):
            x = cx + (i - (n - 1) / 2) * gr * 1.9
            y = y0 + row * min(gr * 1.75, (ty - gr - 1 - y0) / (len(rows) - 1))
            disc(cv, x, y, gr * 1.08, 'g' if row % 2 == 0 else 'q')
    return cv, [('b', 'background', 'Background', BLUE, True), role('g', 'grape', 'Grapes', PINK),
                role('q', 'grape2', 'Dark grapes', PINK), role('t', 'stalk', 'Stalk', BROWN),
                role('l', 'leaf', 'Leaf', GREEN), role('v', 'vine', 'Vine', GREEN), role('k', 'table', 'Table', BROWN)], ['fruit']


def pumpkin(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.16)
    box(cv, 0, gtop, w, h, 'g')
    if big:
        for x in range(0, w, 3):
            box(cv, x, gtop - h * 0.2, x + 0.9, gtop, 'k')
        box(cv, 0, gtop - h * 0.16, w, gtop - h * 0.16 + 0.9, 'k')
    py = h * 0.62
    rx, ry = w * 0.42, h * 0.24
    for k, off in enumerate((-0.55, 0.55, -0.25, 0.25, 0.0)):
        oval(cv, cx + off * rx, py, rx * 0.5, ry, 'p')
    for off in (-0.4, 0.4, 0.0) if not big else (-0.62, -0.31, 0.31, 0.62, 0.0):
        x = cx + off * rx
        path(cv, [(x, py - ry * 0.75), (x + off * 0.6, py), (x, py + ry * 0.75)], 'r', 0.4)
    box(cv, cx - 0.9, py - ry - h * 0.08, cx + 0.9, py - ry + 0.6, 't')
    path(cv, [(cx + 0.8, py - ry - 0.5), (cx + w * 0.2, py - ry - h * 0.06), (cx + w * 0.3, py - ry + 0.5)], 'v', 0.5)
    lens(cv, cx - 0.5, py - ry - 0.3, cx - w * 0.3, py - ry - h * 0.05, s * 0.13, 'v')
    for x in (w * 0.1, w * 0.9) if not big else (w * 0.08, w * 0.92, w * 0.5):
        disc(cv, x, gtop + 1.2, 1.1, 'o')
    disc(cv, w * 0.86, h * 0.1, s * 0.09, 'u')
    return cv, [sky(), role('p', 'pumpkin', 'Pumpkin', BROWN), role('r', 'rib', 'Ribs', BROWN),
                role('t', 'stem', 'Stem', GREEN), role('v', 'vine', 'Vine and leaf', GREEN), role('g', 'grass', 'Grass', GREEN),
                role('o', 'flowers', 'Flowers', PINK), role('u', 'sun', 'Sun', PINK), role('k', 'fence', 'Fence', BLUE)], ['fruit', 'garden']


def mushroom(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.12)
    hills(cv, 'g', gtop, 0.5, w, r.uniform(0, 6))
    mx = cx - w * 0.06
    capb = h * 0.48
    oval(cv, mx, capb, w * 0.42, h * 0.3, 'c')
    box(cv, 0, capb, w, capb + h * 0.32, 's', only='c')
    if big:
        box(cv, mx - w * 0.36, capb - 0.9, mx + w * 0.36, capb, 'i', only='c')
    rbox(cv, mx - s * 0.14, capb - 0.1, mx + s * 0.14, gtop + 1, 1.0, 't')
    for dx, dy, rad in ((-0.22, -0.13, 0.07), (0.12, -0.19, 0.06), (0.27, -0.07, 0.05), (-0.03, -0.06, 0.05)):
        disc(cv, mx + dx * w, capb + dy * h, s * rad + 0.3, 'd')
    sx = w * 0.84
    sb = gtop - h * 0.12
    oval(cv, sx, sb, w * 0.13, h * 0.08, 'k')
    box(cv, 0, sb, w, sb + 3, 's', only='k')
    rbox(cv, sx - 0.9, sb - 0.1, sx + 0.9, gtop + 1, 0.6, 't')
    for x in (w * 0.1, w * 0.62) if not big else (w * 0.06, w * 0.6, w * 0.32):
        for side in (-1, 1):
            lens(cv, x, gtop + 1, x + side * s * 0.12, gtop - h * 0.1, s * 0.09, 'f')
    return cv, [sky(), role('c', 'cap', 'Cap', PINK), role('d', 'dot', 'Dots', PINK), role('t', 'stem', 'Stems', BROWN),
                role('i', 'gills', 'Gills', BROWN), role('k', 'small_cap', 'Small cap', BROWN), role('g', 'grass', 'Grass', GREEN),
                role('f', 'fern', 'Ferns', GREEN)], ['forest', 'garden']


def butterfly(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.1)
    box(cv, 0, gtop, w, h, 'g')
    for x in (w * 0.12, w * 0.88) if not big else (w * 0.1, w * 0.9, w * 0.3, w * 0.7):
        seg(cv, x, gtop, x, gtop - h * 0.08, 'g', 0.5)
        disc(cv, x, gtop - h * 0.09, s * 0.07, 'f')
    by = h * 0.42
    for side in (-1, 1):
        oval(cv, cx + side * w * 0.2, by - h * 0.1, w * 0.19, h * 0.16, 'w')
        oval(cv, cx + side * w * 0.15, by + h * 0.13, w * 0.14, h * 0.11, 'v')
        disc(cv, cx + side * w * 0.23, by - h * 0.11, s * 0.075, 'd')
        disc(cv, cx + side * w * 0.16, by + h * 0.14, s * 0.05 + 0.3, 'd')
        path(cv, [(cx + side * 0.3, by - h * 0.2), (cx + side * w * 0.12, by - h * 0.32)], 'b', 0.45)
        disc(cv, cx + side * w * 0.13, by - h * 0.33, 0.8, 'b')
    rbox(cv, cx - 0.9, by - h * 0.2, cx + 0.9, by + h * 0.24, 0.8, 'b')
    return cv, [sky(), role('w', 'wing', 'Wings', PINK), role('v', 'lower_wing', 'Lower wings', PINK),
                role('d', 'spot', 'Spots', BLUE), role('b', 'body', 'Body', BROWN), role('g', 'grass', 'Grass', GREEN),
                role('f', 'flower', 'Flowers', BROWN)], ['insects', 'garden']


def bee(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.09)
    box(cv, 0, gtop, w, h, 'g')
    fx = w * 0.18
    seg(cv, fx, gtop, fx, h * 0.72, 'l', 0.5)
    lens(cv, fx, gtop - 1, fx + s * 0.22, gtop - h * 0.1, s * 0.09, 'l')
    for k in range(6):
        a = math.radians(k * 60)
        disc(cv, fx + math.cos(a) * s * 0.1, h * 0.7 + math.sin(a) * s * 0.1, s * 0.075, 'f')
    disc(cv, fx, h * 0.7, s * 0.06 + 0.2, 'c')
    by = h * 0.44
    bx = cx + w * 0.02
    oval(cv, bx - w * 0.08, by - h * 0.15, w * 0.13, h * 0.11, 'i')
    oval(cv, bx + w * 0.1, by - h * 0.16, w * 0.12, h * 0.1, 'i')
    oval(cv, bx, by, w * 0.25, h * 0.14, 'y')
    for k in range(3):
        x = bx - w * 0.13 + k * w * 0.1
        box(cv, x - 0.5, by - h * 0.17, x + 0.5 + (0.5 if big else 0), by + h * 0.17, 'k', only='y')
    disc(cv, bx + w * 0.27, by - 0.2, s * 0.12, 'k')
    poly(cv, [(bx - w * 0.23, by - 0.8), (bx - w * 0.36, by), (bx - w * 0.23, by + 0.8)], 'k')
    path(cv, [(bx + w * 0.3, by - s * 0.1), (bx + w * 0.34, by - h * 0.17), (bx + w * 0.4, by - h * 0.2)], 'k', 0.45)
    if big:
        disc(cv, bx + w * 0.31, by - 0.8, 0.8, 'i')
    return cv, [sky(), role('i', 'wing', 'Wings', BLUE), role('y', 'body', 'Body', BROWN), role('k', 'stripe', 'Stripes and head', BROWN),
                role('f', 'flower', 'Flower', PINK), role('c', 'flower_center', 'Flower center', BROWN),
                role('l', 'leaf', 'Stem and leaf', GREEN), role('g', 'grass', 'Grass', GREEN)], ['insects', 'garden']


def snail(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.14)
    hills(cv, 'g', gtop, 0.4, w * 1.4, r.uniform(0, 6))
    lens(cv, w * 0.62, gtop + 0.5, w * 0.98, gtop - h * 0.24, s * 0.22, 'l')
    rbox(cv, w * 0.12, gtop - h * 0.1, w * 0.86, gtop + 0.4, 1.2, 'b')
    for dx in (0.0, 0.1):
        seg(cv, w * (0.78 + dx), gtop - h * 0.08, w * (0.8 + dx * 1.3), gtop - h * 0.24, 'b', 0.45)
    hx, hy, hr = w * 0.42, gtop - h * 0.22, s * 0.27
    disc(cv, hx, hy, hr, 'h')
    pts = []
    for k in range(30):
        t = k / 29
        a = t * math.pi * 2.3
        rr = hr * (0.72 - 0.6 * t)
        pts.append((hx + math.cos(a) * rr, hy + math.sin(a) * rr))
    path(cv, pts, 'c', 0.5 if not big else 0.6)
    for x in (w * 0.08, w * 0.3) if not big else (w * 0.06, w * 0.26, w * 0.48):
        disc(cv, x, h * 0.2 if x > w * 0.2 else h * 0.12, s * 0.07, 'f')
    disc(cv, w * 0.88, h * 0.09, s * 0.08, 'u')
    return cv, [sky(), role('h', 'shell', 'Shell', BROWN), role('c', 'spiral', 'Spiral', BROWN),
                role('b', 'body', 'Body', PINK), role('l', 'leaf', 'Leaf', GREEN), role('g', 'grass', 'Grass', GREEN),
                role('f', 'blossom', 'Blossoms', PINK), role('u', 'sun', 'Sun', BLUE)], ['animals', 'garden']


def ladybug(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    lens(cv, w * 0.42, h + 3, w * 0.76, -1.5, w * 0.74, 'l')
    if big:
        for k in range(3):
            t = 0.25 + k * 0.22
            x, y = w * (0.42 + 0.34 * t), h + 3 - (h + 4.5) * t
            seg(cv, x - 1.4, y - 0.8, x - w * 0.2, y - h * 0.06, 'v', 0.45)
            seg(cv, x + 1.4, y - 0.8, x + w * 0.2, y - h * 0.1, 'v', 0.45)
    by = h * 0.56
    rx, ry = w * 0.29, h * 0.23
    for side in (-1, 1):
        for k in (-0.5, 0.05, 0.6):
            seg(cv, cx + side * rx * 0.8, by + k * ry, cx + side * (rx + s * 0.08), by + k * ry + 0.6, 'h', 0.45)
    disc(cv, cx, by - ry - s * 0.01, s * 0.14, 'h')
    for side in (-1, 1):
        seg(cv, cx + side * 0.6, by - ry - s * 0.12, cx + side * s * 0.16, by - ry - s * 0.26, 'h', 0.4)
    oval(cv, cx, by, rx, ry, 'b')
    seg(cv, cx, by - ry, cx, by + ry, 'h', 0.45)
    for dx, dy in ((-0.5, -0.4), (0.5, -0.4), (-0.55, 0.25), (0.55, 0.25), (-0.22, 0.65), (0.22, 0.65)):
        disc(cv, cx + dx * rx, by + dy * ry, s * 0.06 + 0.25, 'd')
    if big:
        for side in (-1, 1):
            disc(cv, cx + side * s * 0.06, by - ry - s * 0.05, 0.75, 'e')
    oval(cv, w * 0.3, h * 0.88, s * 0.06 + 0.3, s * 0.08 + 0.3, 'w')
    oval(cv, w * 0.86, h * 0.3, s * 0.06 + 0.3, s * 0.08 + 0.3, 'w')
    fx, fy = w * 0.13, h * 0.12
    for k in range(5):
        a = math.radians(k * 72 - 90)
        disc(cv, fx + math.cos(a) * s * 0.06, fy + math.sin(a) * s * 0.06, s * 0.055, 'f')
    return cv, [sky(), role('l', 'leaf', 'Leaf', GREEN), role('v', 'vein', 'Leaf veins', GREEN), role('b', 'shell', 'Shell', PINK),
                role('d', 'dot', 'Dots', BROWN), role('h', 'head', 'Head and legs', BROWN), role('w', 'dew', 'Dew drops', BLUE),
                role('e', 'eye', 'Eyes', BLUE), role('f', 'flower', 'Flower', PINK)], ['insects', 'garden']


def fish(w, h, r):
    cv, s, cx, big = start(w, h, 'w')
    gtop = h - ground_rows(h, 0.1)
    for x in (w * 0.1, w * 0.86) + ((w * 0.7,) if big else ()):
        path(cv, [(x, gtop), (x + 0.8, h * 0.84), (x - 0.4, h * 0.74), (x + 0.5, h * 0.66)], 'g', 0.55)
    seg(cv, w * 0.26, gtop, w * 0.24, h * 0.8, 'k', 0.6)
    disc(cv, w * 0.25, h * 0.79, s * 0.08, 'k')
    hills(cv, 'd', gtop, 0.5, w * 0.8, r.uniform(0, 6))
    fy = h * 0.4
    poly(cv, [(w * 0.62, fy), (w * 0.9, fy - h * 0.15), (w * 0.82, fy), (w * 0.9, fy + h * 0.15)], 't')
    poly(cv, [(w * 0.32, fy - h * 0.12), (w * 0.5, fy - h * 0.25), (w * 0.56, fy - h * 0.1)], 't')
    oval(cv, w * 0.44, fy, w * 0.28, h * 0.14, 'f')
    for x in (0.36, 0.5) if not big else (0.32, 0.45, 0.58):
        box(cv, w * x - 0.5, 0, w * x + 0.6, h, 'e', only='f')
    disc(cv, w * 0.26, fy - h * 0.03, 0.9 if not big else 1.05, 'k')
    for k in range(3):
        disc(cv, w * (0.16 - k * 0.03), fy - h * (0.12 + k * 0.07), 0.75 + k * 0.15, 'o')
    return cv, [('w', 'water', 'Water', BLUE, True), role('f', 'fish', 'Fish', BROWN), role('t', 'fin', 'Fins and tail', PINK),
                role('e', 'stripe', 'Stripes', BLUE), role('k', 'eye', 'Eye and coral', PINK),
                role('g', 'weed', 'Weed', GREEN), role('d', 'sand', 'Sand', BROWN), role('o', 'bubble', 'Bubbles', BLUE)], ['animals', 'sea']


def bird(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    by = h * 0.7
    path(cv, [(-1, by + 1.2), (w * 0.45, by), (w * 0.82, by - 1.4)], 'r', 0.8 if not big else 1.1)
    seg(cv, w * 0.62, by - 0.6, w * 0.78, by - h * 0.14, 'r', 0.5)
    for x, y, a in ((w * 0.12, by - 0.6, -0.4), (w * 0.78, by - h * 0.15, 0.5), (w * 0.3, by + 0.8, 0.3), (w * 0.84, by - 1.2, -0.2)):
        lens(cv, x, y, x + math.cos(a) * s * 0.26, y - math.sin(a + 1.2) * s * 0.18, s * 0.12, 'l')
    disc(cv, w * 0.18, by - h * 0.06, s * 0.06 + 0.3, 'f')
    bx, byy = w * 0.44, h * 0.5
    poly(cv, [(bx - w * 0.2, byy), (bx - w * 0.4, byy - h * 0.06), (bx - w * 0.36, byy + h * 0.04)], 'n')
    oval(cv, bx, byy, w * 0.24, h * 0.15, 'b')
    disc(cv, bx + w * 0.16, byy - h * 0.15, s * 0.14, 'b')
    poly(cv, [(bx + w * 0.26, byy - h * 0.17), (bx + w * 0.4, byy - h * 0.13), (bx + w * 0.26, byy - h * 0.09)], 'k')
    oval(cv, bx - w * 0.04, byy - h * 0.01, w * 0.14, h * 0.08, 'n')
    for dx in (-0.04, 0.06):
        seg(cv, bx + dx * w, byy + h * 0.13, bx + dx * w, by - 0.4, 'k', 0.45)
    if big:
        disc(cv, bx + w * 0.19, byy - h * 0.17, 0.9, 'e')
    disc(cv, w * 0.86, h * 0.1, s * 0.08, 'u')
    return cv, [sky(), role('b', 'bird', 'Bird', PINK), role('n', 'wing', 'Wing and tail', PINK), role('k', 'beak', 'Beak and legs', BROWN),
                role('r', 'branch', 'Branch', BROWN), role('l', 'leaves', 'Leaves', GREEN), role('f', 'blossom', 'Blossom', PINK),
                role('u', 'sun', 'Sun', BROWN), role('e', 'eye', 'Eye', BLUE)], ['animals', 'garden']


def frog(w, h, r):
    cv, s, cx, big = start(w, h, 'w')
    for k in range(2 if not big else 3):
        y = h * (0.12 + k * 0.09)
        seg(cv, w * (0.36 + 0.2 * k), y, w * (0.5 + 0.2 * k), y, 'v', 0.45)
    py = h * 0.76
    oval(cv, cx, py, w * 0.4, h * 0.12, 'p')
    poly(cv, [(cx + w * 0.06, py), (cx + w * 0.44, py - h * 0.07), (cx + w * 0.44, py + h * 0.02)], 'w')
    fy = h * 0.58
    oval(cv, cx, fy, w * 0.3, h * 0.16, 'f')
    for side in (-1, 1):
        oval(cv, cx + side * w * 0.26, py - 0.6, w * 0.1, h * 0.05, 'f')
        disc(cv, cx + side * w * 0.15, fy - h * 0.17, s * 0.13, 'f')
        disc(cv, cx + side * w * 0.15, fy - h * 0.18, s * 0.085, 'e')
        if big:
            disc(cv, cx + side * w * 0.15, fy - h * 0.17, 1.0, 'k')
    oval(cv, cx, fy + h * 0.07, w * 0.17, h * 0.07, 'y')
    path(cv, [(cx - w * 0.14, fy - h * 0.03), (cx - w * 0.06, fy), (cx + w * 0.06, fy), (cx + w * 0.14, fy - h * 0.03)], 'm', 0.45)
    for x in (w * 0.07,) if not big else (w * 0.06, w * 0.94):
        seg(cv, x, h, x, h * 0.36, 'r', 0.5)
        oval(cv, x, h * 0.36, 0.9, h * 0.06, 'k')
    ox, oy = w * 0.84, h * 0.9
    for k in range(5):
        a = math.radians(k * 72 - 90)
        disc(cv, ox + math.cos(a) * s * 0.06, oy + math.sin(a) * s * 0.05, s * 0.055 + 0.15, 'o')
    return cv, [('w', 'pond', 'Pond', BLUE, True), role('p', 'pad', 'Lily pad', GREEN), role('f', 'frog', 'Frog', GREEN),
                role('e', 'eye', 'Eyes', BLUE), role('y', 'belly', 'Belly', BROWN), role('m', 'mouth', 'Smile', BROWN),
                role('r', 'reed', 'Reeds', BROWN), role('k', 'cattail', 'Cattails and pupils', BROWN), role('o', 'blossom', 'Blossom', PINK),
                role('v', 'ripple', 'Ripples', BLUE)], ['animals', 'pond']


def watering_can(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.12)
    box(cv, 0, gtop, w, h, 'g')
    for x in (w * 0.8, w * 0.92) if not big else (w * 0.72, w * 0.84, w * 0.95):
        seg(cv, x, gtop, x, gtop - h * 0.12, 'g', 0.45)
        disc(cv, x, gtop - h * 0.13, s * 0.07, 'f')
        cv.put(int(x), int(gtop - h * 0.13), 'o')
    rbox(cv, w * 0.12, h * 0.42, w * 0.58, h * 0.78, 1.2, 'c')
    seg(cv, w * 0.56, h * 0.68, w * 0.82, h * 0.4, 'c', 0.7)
    oval(cv, w * 0.84, h * 0.38, s * 0.08, s * 0.08, 'h')
    path(cv, [(w * 0.18, h * 0.42), (w * 0.24, h * 0.24), (w * 0.46, h * 0.24), (w * 0.52, h * 0.42)], 'h', 0.5)
    box(cv, w * 0.12, h * 0.52, w * 0.58, h * 0.52 + (1 if big else 0.4), 'h')
    for k in range(4 if big else 3):
        cv.put(int(w * 0.84 + (k % 2) * 1.2), int(h * (0.48 + k * 0.08)), 'd')
        cv.put(int(w * 0.9 + (k % 2)), int(h * (0.46 + k * 0.08)), 'd')
    return cv, [sky(), role('c', 'can', 'Can', GREEN), role('h', 'handle', 'Handle and rose', BROWN),
                role('d', 'drops', 'Drops', BLUE), role('g', 'grass', 'Grass', GREEN), role('f', 'flowers', 'Flowers', PINK),
                role('o', 'flower_center', 'Flower centers', BROWN)], ['tools', 'garden']


def birdhouse(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.08)
    box(cv, 0, gtop, w, h, 'g')
    box(cv, cx - 0.9, h * 0.62, cx + 0.9, gtop, 'p')
    for k in range(2 if not big else 3):
        lens(cv, cx, h * (0.72 + k * 0.08), cx + (1 if k % 2 else -1) * w * 0.2, h * (0.68 + k * 0.08), s * 0.09, 'l')
    rbox(cv, w * 0.24, h * 0.32, w * 0.76, h * 0.64, 0.5, 'h')
    poly(cv, [(w * 0.14, h * 0.34), (w * 0.5, h * 0.1), (w * 0.86, h * 0.34)], 'r')
    disc(cv, cx, h * 0.45, 0.95 if not big else 1.4, '.')
    seg(cv, cx - 1, h * 0.54, cx + 1, h * 0.54, 'p', 0.5)
    bx, by = w * 0.7, h * 0.27
    oval(cv, bx, by - 0.2, s * 0.1, s * 0.07, 'b')
    disc(cv, bx + s * 0.08, by - s * 0.07, s * 0.06 + 0.15, 'b')
    disc(cv, w * 0.12, h * 0.08, s * 0.08, 'u')
    return cv, [sky(), role('r', 'roof', 'Roof', PINK), role('h', 'house', 'House', BROWN), role('p', 'pole', 'Pole and perch', BROWN),
                role('g', 'grass', 'Grass', GREEN), role('l', 'leaves', 'Leaves', GREEN), role('b', 'bird', 'Bird', PINK),
                role('u', 'sun', 'Sun', BLUE)], ['cozy', 'garden']


def mug(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    ty = h * 0.88
    box(cv, 0, ty, w, h, 'd')
    mx0, mx1 = w * 0.16, w * 0.66
    ring(cv, mx1, h * 0.6, s * 0.17, s * 0.08, 'm')
    rbox(cv, mx0, h * 0.4, mx1, ty, 1.0, 'm')
    box(cv, mx0 + 0.6, h * 0.4, mx1 - 0.6, h * 0.4 + (1 if not big else 1.6), 't')
    hx = (mx0 + mx1) / 2
    heart(cv, hx, h * 0.63, s * 0.1 + 0.2, 'e')
    for k in range(2):
        x = hx - w * 0.08 + k * w * 0.16
        path(cv, [(x, h * 0.36), (x + 0.8, h * 0.28), (x - 0.4, h * 0.2), (x + 0.4, h * 0.1)], 'v', 0.45)
    kx = w * 0.84
    disc(cv, kx, ty - s * 0.08, s * 0.1, 'k')
    dots(cv, 'x', 'b', 4, 3, area=(0, 1, w - 1, ty - 2))
    return cv, [('b', 'background', 'Wall', GREEN, True), role('m', 'mug', 'Mug', PINK), role('t', 'tea', 'Tea', BROWN),
                role('e', 'heart', 'Heart', BLUE), role('v', 'steam', 'Steam', BLUE), role('d', 'table', 'Table', BROWN),
                role('k', 'cookie', 'Cookie', BROWN), role('x', 'pattern', 'Wallpaper dots', GREEN)], ['cozy']


def umbrella(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.1)
    box(cv, 0, gtop, w, h, 'g')
    oval(cv, w * 0.3, gtop + 0.3, w * 0.2, 1.0, 'p')
    for x in (w * 0.86,) if not big else (w * 0.86, w * 0.08):
        seg(cv, x, gtop, x, gtop - h * 0.08, 'g', 0.45)
        disc(cv, x, gtop - h * 0.09, s * 0.07, 'f')
    uy = h * 0.42
    ur = w * 0.4
    n = 5 if big else 4
    for y in range(h):
        for x in range(w):
            dx, dy = x + 0.5 - cx, y + 0.5 - uy
            if dy <= 0 and dx * dx / (ur * ur) + dy * dy / ((h * 0.3) ** 2) <= 1.0:
                a = math.degrees(math.atan2(-dy, dx))
                cv.g[y][x] = 'u' if int(a // (180 / n)) % 2 == 0 else 'v'
    for k in range(n):
        x = cx - ur + (k + 0.5) * 2 * ur / n
        disc(cv, x, uy, ur / n * 0.95, 's')
    seg(cv, cx, h * 0.12, cx, h * 0.04, 'h', 0.45)
    seg(cv, cx, uy - 1, cx, h * 0.8, 'h', 0.5)
    path(cv, [(cx, h * 0.8), (cx - 0.5, h * 0.85), (cx - 1.6, h * 0.84), (cx - 1.9, h * 0.78)], 'h', 0.5)
    scatter(cv, 'd', 's', 5 + (w * h) // 50, r, sep=3, area=(0, uy + 1, w - 1, gtop - 2))
    scatter(cv, 'd', 's', 3, r, sep=3, area=(0, 0, w - 1, h * 0.12))
    return cv, [sky(), role('u', 'canopy', 'Canopy', PINK), role('v', 'panel', 'Panels', PINK), role('h', 'handle', 'Handle', BROWN),
                role('d', 'rain', 'Raindrops', BLUE), role('g', 'grass', 'Grass', GREEN), role('p', 'puddle', 'Puddle', BLUE),
                role('f', 'flowers', 'Flowers', BROWN)], ['cozy']


def cottage(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.12)
    box(cv, 0, gtop, w, h, 'g')
    wy = h * 0.46
    rbox(cv, w * 0.18, wy, w * 0.82, gtop + 0.5, 0.3, 'h')
    box(cv, w * 0.64, h * 0.18, w * 0.64 + max(1.8, w * 0.1), wy - 1, 'c')
    poly(cv, [(w * 0.1, wy + 0.6), (w * 0.5, h * 0.15), (w * 0.9, wy + 0.6)], 'r')
    for k in range(2 if not big else 3):
        disc(cv, w * 0.7 + k * 1.2 + 0.6, h * 0.13 - k * h * 0.035, 0.75 + k * 0.2, 'k')
    for x0 in (0.24, 0.6) if not big else (0.23, 0.6):
        box(cv, w * x0, wy + h * 0.08, w * (x0 + 0.16), wy + h * 0.21, 'i')
        if big:
            box(cv, w * (x0 + 0.08) - 0.5, wy + h * 0.08, w * (x0 + 0.08) + 0.5, wy + h * 0.21, 'h')
        box(cv, w * x0 - 0.4, wy + h * 0.22, w * (x0 + 0.16) + 0.4, wy + h * 0.26, 'f')
    rbox(cv, w * 0.43, wy + h * 0.2, w * 0.57, gtop + 0.4, 0.9, 'd')
    for x in (w * 0.22, w * 0.78):
        oval(cv, x, gtop - 0.2, s * 0.1, s * 0.08, 'b')
    return cv, [sky(), role('r', 'roof', 'Roof', BROWN), role('h', 'wall', 'Walls', PINK), role('i', 'window', 'Windows', BLUE),
                role('d', 'door', 'Door', BROWN), role('f', 'flowerbox', 'Flower boxes', PINK), role('c', 'chimney', 'Chimney', BROWN),
                role('k', 'smoke', 'Smoke', BLUE), role('g', 'grass', 'Grass', GREEN), role('b', 'bush', 'Bushes', GREEN)], ['cozy', 'garden']


def sailboat(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    sea = h * 0.7
    if big:
        ix = w * 0.12
        path(cv, [(ix, sea + 0.5), (ix + 0.6, sea - h * 0.12), (ix + 1.4, sea - h * 0.2)], 'k', 0.5)
        for a in (-0.5, 0.4, 1.3):
            lens(cv, ix + 1.4, sea - h * 0.2, ix + 1.4 + math.cos(a) * s * 0.18, sea - h * 0.2 + math.sin(a) * s * 0.08 - 1, s * 0.07, 'g')
        oval(cv, ix, sea + 0.8, w * 0.16, h * 0.05, 'g')
    hills(cv, 'w', sea + 0.6, 0.4, w * 0.5, r.uniform(0, 6), only='s')
    bx = w * 0.54
    poly(cv, [(bx - w * 0.36, sea - h * 0.06), (bx + w * 0.38, sea - h * 0.06), (bx + w * 0.26, sea + h * 0.08),
              (bx - w * 0.26, sea + h * 0.08)], 'h')
    box(cv, bx - w * 0.36, sea - h * 0.06, bx + w * 0.38, sea - h * 0.06 + 1.0, 'm')
    seg(cv, bx, sea - h * 0.07, bx, h * 0.1, 'k', 0.45)
    poly(cv, [(bx - 0.6, h * 0.12), (bx - 0.6, sea - h * 0.1), (bx - w * 0.34, sea - h * 0.1)], 'a')
    poly(cv, [(bx + 0.6, h * 0.2), (bx + 0.6, sea - h * 0.1), (bx + w * 0.32, sea - h * 0.1)], 'j')
    poly(cv, [(bx + 0.5, h * 0.05), (bx + w * 0.16, h * 0.08), (bx + 0.5, h * 0.11)], 'f')
    disc(cv, w * 0.86, h * 0.1, s * 0.085, 'u')
    return cv, [sky(), role('a', 'sail', 'Sail', PINK), role('j', 'jib', 'Front sail', PINK), role('h', 'hull', 'Hull', BROWN),
                role('m', 'stripe', 'Hull stripe', GREEN), role('k', 'mast', 'Mast', BROWN), role('f', 'flag', 'Flag', PINK),
                role('w', 'sea', 'Sea', BLUE), role('u', 'sun', 'Sun', BROWN), role('g', 'island', 'Island', GREEN)], ['sea']


def cactus(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    poly(cv, [(-1, h * 0.7), (w * 0.08, h * 0.54), (w * 0.3, h * 0.54), (w * 0.38, h * 0.7)], 'm')
    poly(cv, [(w * 0.64, h * 0.7), (w * 0.72, h * 0.58), (w * 0.94, h * 0.58), (w + 1, h * 0.7)], 'm')
    gtop = h * 0.8
    hills(cv, 'd', h * 0.72, 0.6, w * 1.6, r.uniform(0, 6))
    cw = w * 0.09 if not big else w * 0.1
    rbox(cv, cx - cw, h * 0.18, cx + cw, gtop, cw, 'c')
    rbox(cv, cx - w * 0.33, h * 0.36, cx - w * 0.33 + cw * 1.6, h * 0.56, cw * 0.8, 'c')
    box(cv, cx - w * 0.33 + cw * 0.5, h * 0.5, cx, h * 0.56, 'c')
    rbox(cv, cx + w * 0.33 - cw * 1.6, h * 0.28, cx + w * 0.33, h * 0.48, cw * 0.8, 'c')
    box(cv, cx, h * 0.42, cx + w * 0.33 - cw * 0.5, h * 0.48, 'c')
    seg(cv, cx, h * 0.24, cx, gtop - 1, 'k', 0.4)
    if big:
        seg(cv, cx - w * 0.33 + cw * 0.8, h * 0.4, cx - w * 0.33 + cw * 0.8, h * 0.5, 'k', 0.4)
        seg(cv, cx + w * 0.33 - cw * 0.8, h * 0.32, cx + w * 0.33 - cw * 0.8, h * 0.42, 'k', 0.4)
    for k in range(5):
        a = math.radians(k * 72 - 90)
        disc(cv, cx + math.cos(a) * s * 0.07, h * 0.15 + math.sin(a) * s * 0.06, s * 0.05 + 0.2, 'f')
    for x in (w * 0.14, w * 0.84):
        oval(cv, x, h * 0.86, s * 0.09, s * 0.06, 'o')
    disc(cv, w * 0.86, h * 0.1, s * 0.09, 'u')
    return cv, [sky(), role('c', 'cactus', 'Cactus', GREEN), role('k', 'rib', 'Ribs', GREEN), role('f', 'flower', 'Flower', PINK),
                role('o', 'rock', 'Rocks', BROWN), role('d', 'sand', 'Sand', BROWN), role('m', 'mesa', 'Mesas', PINK),
                role('u', 'sun', 'Sun', BROWN)], ['desert', 'garden']


def hedgehog(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.14)
    hills(cv, 'g', gtop, 0.4, w * 1.3, r.uniform(0, 6))
    hx, hy = w * 0.42, gtop - h * 0.14
    ax, ay = hx - w * 0.04, hy - h * 0.28
    disc(cv, ax, ay, s * 0.1, 'a')
    seg(cv, ax, ay - s * 0.1, ax + 0.4, ay - s * 0.16, 'n', 0.4)
    lens(cv, ax + 0.3, ay - s * 0.12, ax + s * 0.18, ay - s * 0.2, s * 0.08, 'l')
    n = 9 if not big else 13
    for k in range(n):
        a = math.radians(170 + k * (190 / (n - 1)))
        seg(cv, hx + math.cos(a) * w * 0.2, hy + math.sin(a) * h * 0.12,
            hx + math.cos(a) * w * 0.41, hy + math.sin(a) * h * 0.27, 'k', 0.5 if not big else 0.6)
    oval(cv, hx, hy, w * 0.33, h * 0.17, 'k')
    poly(cv, [(hx + w * 0.16, hy - h * 0.08), (hx + w * 0.5, hy + h * 0.06), (hx + w * 0.18, hy + h * 0.16)], 'f')
    disc(cv, hx + w * 0.49, hy + h * 0.06, 0.8, 'n')
    for dx in (-0.16, 0.12):
        box(cv, hx + dx * w - 0.6, hy + h * 0.12, hx + dx * w + 0.6, gtop + 0.5, 'f')
    if big:
        disc(cv, hx + w * 0.3, hy - 0.2, 0.75, 'n')
    disc(cv, w * 0.86, h * 0.1, s * 0.08, 'u')
    return cv, [sky(), role('k', 'spikes', 'Spikes', BROWN), role('f', 'face', 'Face and feet', BROWN), role('n', 'nose', 'Nose and eye', PINK),
                role('g', 'grass', 'Grass', GREEN), role('a', 'apple', 'Apple', PINK), role('l', 'leaf', 'Leaf', GREEN),
                role('u', 'sun', 'Sun', BROWN)], ['animals', 'garden']


def owl(w, h, r):
    cv, s, cx, big = start(w, h, 'n')
    by = h - ground_rows(h, 0.12)
    rbox(cv, -2, by, w + 2, h + 2, 1.5, 'r')
    for x, a in ((w * 0.1, 0.6), (w * 0.86, 2.2), (w * 0.7, -0.6)):
        lens(cv, x, by, x + math.cos(a) * s * 0.2, by - abs(math.sin(a)) * s * 0.18 - 1, s * 0.1, 'l')
    oy = by - h * 0.32
    for side in (-1, 1):
        poly(cv, [(cx + side * w * 0.12, oy - h * 0.26), (cx + side * w * 0.3, oy - h * 0.36), (cx + side * w * 0.3, oy - h * 0.2)], 'o')
    oval(cv, cx, oy, w * 0.3, h * 0.32, 'o')
    box(cv, cx - w * 0.3, oy, cx + w * 0.3, by + 0.5, 'o', only='n')
    oval(cv, cx, oy + h * 0.12, w * 0.18, h * 0.17, 'b')
    for side in (-1, 1):
        disc(cv, cx + side * w * 0.13, oy - h * 0.12, s * 0.12, 'e')
        disc(cv, cx + side * w * 0.12, oy - h * 0.12, s * 0.05 + 0.3, 'k')
    poly(cv, [(cx - 0.8, oy - h * 0.05), (cx + 0.8, oy - h * 0.05), (cx, oy + h * 0.02)], 'k')
    disc(cv, w * 0.84, h * 0.12, s * 0.1, 'm')
    disc(cv, w * 0.84 - s * 0.05, h * 0.12 - s * 0.04, s * 0.08, 'n')
    for x, y in ((0.12, 0.1), (0.3, 0.06), (0.62, 0.05), (0.08, 0.36), (0.92, 0.4)):
        cv.put(int(w * x), int(h * y), 'x')
    return cv, [('n', 'night', 'Night sky', PINK, True), role('o', 'owl', 'Owl', BROWN), role('b', 'belly', 'Belly', BROWN),
                role('e', 'eye', 'Eyes', BLUE), role('k', 'pupil', 'Pupils and beak', PINK), role('r', 'branch', 'Branch', GREEN),
                role('l', 'leaves', 'Leaves', GREEN), role('m', 'moon', 'Moon', BLUE), role('x', 'stars', 'Stars', BROWN)], ['animals', 'night']


GARDEN_SUBJECTS = [flower_pot, daisy_field, tulip_bed, fruit_tree, apple, pear, cherries, strawberry, grapes, pumpkin,
                   mushroom, butterfly, bee, snail, ladybug, fish, bird, frog, watering_can, birdhouse, mug, umbrella,
                   cottage, sailboat, cactus, hedgehog, owl]

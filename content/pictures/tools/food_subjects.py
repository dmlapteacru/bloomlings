"""Food and treat subjects for the bigger boards (the owner, 2026-10-06), in the procedural style of sketch_pictures.py:
each draws with picture_kit and returns (canvas, roles, themes). Treats stand on a table or the ground, so their
decorations (sprinkles, seeds, candles) stay within the nesting depth targets, and they get more decoration on the big
boards (from 300 cells).
"""
import math

from picture_kit import (BLUE, BROWN, GREEN, PINK, box, cloud, disc, dots, ground_rows, heart, hills, lens, oval, path,
                         poly, rbox, ring, role, scatter, seg, sky, star, start)


def cake(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    ty = h * 0.9
    box(cv, 0, ty, w, h, 't')
    rbox(cv, cx - w * 0.36, ty - 1.2, cx + w * 0.36, ty + 0.5, 0.6, 'k')
    box(cv, cx - 0.9, ty - 2.2, cx + 0.9, ty - 1.0, 'k')
    layers = 2 if not big else 3
    top = h * 0.42
    lh = (ty - 2.2 - top) / layers
    for k in range(layers):
        y0 = top + k * lh
        rbox(cv, cx - w * (0.3 - k * 0.0), y0, cx + w * (0.3 - k * 0.0), y0 + lh, 0.6, 'c')
        box(cv, cx - w * 0.3, y0, cx + w * 0.3, y0 + lh * 0.3, 'm')
        for x in range(int(cx - w * 0.28), int(cx + w * 0.28) + 1, 2):
            box(cv, x, y0 + lh * 0.3, x + 0.9, y0 + lh * 0.3 + 1.0, 'm')
    dots(cv, 'x', 'c', 3, 2, area=(0, top, w - 1, ty))
    n = 3 if not big else 5
    for k in range(n):
        x = cx - w * 0.2 + k * (w * 0.4 / (n - 1))
        box(cv, x - 0.5, top - h * 0.12, x + 0.5, top, 'd')
        oval(cv, x, top - h * 0.15, 0.7, 1.2, 'f')
    disc(cv, cx, top - 0.6, s * 0.05 + 0.5, 'r')
    for k, (x, y) in enumerate(((0.12, 0.2), (0.88, 0.14), (0.1, 0.5), (0.9, 0.44))):
        if k < 2 or big:
            star(cv, w * x, h * y, s * 0.05 + 0.5, 'f', ri=0.5)
    return cv, [('b', 'wall', 'Wall', BLUE, True), role('c', 'sponge', 'Sponge', BROWN), role('m', 'cream', 'Cream', PINK),
                role('x', 'sprinkles', 'Sprinkles', GREEN), role('d', 'candles', 'Candles', BLUE), role('f', 'flames', 'Flames and stars', BROWN),
                role('r', 'cherry', 'Cherry', PINK), role('k', 'stand', 'Cake stand', GREEN), role('t', 'table', 'Table', BROWN)], ['food', 'party']


def ice_cream(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    ty = h * 0.9
    box(cv, 0, ty, w, h, 't')
    oval(cv, cx, ty, w * 0.22, 1.2, 'n')
    ct = h * 0.5
    poly(cv, [(cx - w * 0.2, ct), (cx + w * 0.2, ct), (cx, ty - 0.4)], 'c')
    for y in range(int(ct), h):
        for x in range(w):
            if cv.g[y][x] == 'c' and (x + y) % 3 == 0:
                cv.g[y][x] = 'w'
    scoops = [(-0.12, 0.02, 'p'), (0.12, 0.02, 'g'), (0.0, -0.17, 'v')]
    for dx, dy, c in scoops:
        disc(cv, cx + dx * w, ct + dy * h - h * 0.02, s * 0.17, c)
    for dx, dy, c in scoops[:2]:
        for k in range(2):
            disc(cv, cx + dx * w + (k - 0.5) * s * 0.14, ct + h * 0.04, 0.9, c)
    disc(cv, cx + w * 0.02, ct - h * 0.34, s * 0.07 + 0.3, 'r')
    seg(cv, cx + w * 0.03, ct - h * 0.37, cx + w * 0.08, ct - h * 0.42, 'r', 0.4)
    scatter(cv, 'x', 'pgv', 6 + (w * h) // 60, r, sep=2)
    if big:
        for x, y in ((0.12, 0.16), (0.86, 0.22), (0.1, 0.62), (0.9, 0.6)):
            heart(cv, w * x, h * y, s * 0.04 + 0.4, 'x')
    return cv, [('b', 'background', 'Background', BLUE, True), role('c', 'cone', 'Cone', BROWN), role('w', 'waffle', 'Waffle lines', BROWN),
                role('p', 'strawberry', 'Strawberry scoop', PINK), role('g', 'mint', 'Mint scoop', GREEN), role('v', 'grape', 'Grape scoop', PINK),
                role('r', 'cherry', 'Cherry', BROWN), role('x', 'sprinkles', 'Sprinkles and hearts', BLUE), role('n', 'napkin', 'Napkin', GREEN),
                role('t', 'table', 'Table', BROWN)], ['food', 'summer']


def cupcake(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    ty = h * 0.88
    box(cv, 0, ty, w, h, 't')
    wt = h * 0.58
    poly(cv, [(cx - w * 0.3, wt), (cx + w * 0.3, wt), (cx + w * 0.22, ty + 0.5), (cx - w * 0.22, ty + 0.5)], 'w')
    n = 4 if not big else 6
    for k in range(1, n):
        x = cx - w * 0.3 + k * (w * 0.6 / n)
        seg(cv, x, wt + 0.5, cx + (x - cx) * 0.74, ty, 'k', 0.45)
    for k, (dx, dy, rr) in enumerate(((-0.18, 0.0, 0.17), (0.18, 0.0, 0.17), (0.0, -0.02, 0.2), (0.0, -0.17, 0.15), (0.03, -0.28, 0.08))):
        disc(cv, cx + dx * w, wt + dy * h - 0.4, s * rr, 'f')
    path(cv, [(cx - w * 0.22, wt - h * 0.02), (cx, wt - h * 0.06), (cx + w * 0.22, wt - h * 0.02)], 'g', 0.45)
    if big:
        path(cv, [(cx - w * 0.14, wt - h * 0.15), (cx, wt - h * 0.18), (cx + w * 0.14, wt - h * 0.15)], 'g', 0.45)
    scatter(cv, 'x', 'f', 5 + (w * h) // 60, r, sep=2)
    disc(cv, cx + w * 0.02, wt - h * 0.36, s * 0.07 + 0.3, 'c')
    seg(cv, cx + w * 0.03, wt - h * 0.39, cx + w * 0.1, wt - h * 0.44, 'c', 0.4)
    for x, y in ((0.12, 0.18), (0.88, 0.26)):
        disc(cv, w * x, h * y, s * 0.05 + 0.3, 'o')
    return cv, [('b', 'wall', 'Wall', BLUE, True), role('w', 'wrapper', 'Wrapper', BROWN), role('k', 'pleats', 'Pleats', BROWN),
                role('f', 'frosting', 'Frosting', PINK), role('g', 'swirl', 'Swirl', PINK), role('x', 'sprinkles', 'Sprinkles', BLUE),
                role('c', 'cherry', 'Cherry', GREEN), role('o', 'bubbles', 'Bubbles', GREEN), role('t', 'table', 'Table', BROWN)], ['food', 'party']


def lollipop(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.12)
    hills(cv, 'g', gtop, 0.4, w * 1.4, r.uniform(0, 6))
    ly, lr = h * 0.36, s * 0.32
    seg(cv, cx, ly, cx, gtop + 0.5, 'k', 0.6 if not big else 0.9)
    arms = 'abc' if big else 'ab'
    for y in range(h):
        for x in range(w):
            dx, dy = x + 0.5 - cx, y + 0.5 - ly
            d = math.hypot(dx, dy)
            if d <= lr:
                a = (math.atan2(dy, dx) + math.pi) / (2 * math.pi)
                t = (a + d / lr * 0.6) % 1.0
                cv.g[y][x] = arms[int(t * len(arms)) % len(arms)]
    for side in (-1, 1):
        poly(cv, [(cx, ly + lr + 1.5), (cx + side * w * 0.14, ly + lr + 0.3), (cx + side * w * 0.14, ly + lr + 2.8)], 'r')
    disc(cv, cx, ly + lr + 1.5, 0.9, 'r')
    for x in (w * 0.1, w * 0.88) if not big else (w * 0.08, w * 0.9, w * 0.24, w * 0.76):
        disc(cv, x, gtop + 1.2, 1.0, 'f')
    cloud(cv, w * 0.84, h * 0.08, s * 0.05 + 0.4, 'm')
    return cv, [sky(), role('a', 'swirl', 'Pink swirl', PINK), role('b', 'swirl2', 'Orange swirl', BROWN), role('c', 'swirl3', 'Violet swirl', PINK),
                role('k', 'stick', 'Stick', BLUE), role('r', 'bow', 'Bow', GREEN), role('f', 'flowers', 'Flowers', BROWN),
                role('m', 'cloud', 'Cloud', BLUE), role('g', 'grass', 'Grass', GREEN)], ['food', 'sweets']


def teapot(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    ty = h * 0.88
    box(cv, 0, ty, w, h, 'd')
    tx, tyy = cx - w * 0.08, h * 0.6
    ring(cv, tx - w * 0.26, tyy, s * 0.16, s * 0.06, 't')
    path(cv, [(tx + w * 0.16, tyy + h * 0.05), (tx + w * 0.32, tyy - h * 0.04), (tx + w * 0.42, tyy - h * 0.16)], 't', 1.1 if not big else 1.4)
    oval(cv, tx, tyy, w * 0.25, h * 0.19, 't')
    box(cv, tx - w * 0.2, ty - 0.6, tx + w * 0.2, ty + 0.4, 't')
    oval(cv, tx, tyy - h * 0.2, w * 0.16, h * 0.045, 'l')
    disc(cv, tx, tyy - h * 0.25, 1.0, 'l')
    for dx, dy in ((-0.08, 0.0), (0.08, 0.06)) if not big else ((-0.14, -0.04), (0.1, -0.06), (0.0, 0.08), (-0.12, 0.1), (0.14, 0.08)):
        cv.put(int(tx + dx * w), int(tyy + dy * h), 'p')
        for k in range(5):
            a = math.radians(k * 72 - 90)
            cv.put(int(tx + dx * w + math.cos(a) * 1.0), int(tyy + dy * h + math.sin(a) * 1.0), 'p')
    cxp = w * 0.84
    rbox(cv, cxp - s * 0.1, ty - s * 0.16, cxp + s * 0.1, ty - 0.4, 0.8, 'c')
    oval(cv, cxp, ty - 0.3, s * 0.15, 0.8, 'c')
    for k in range(2):
        x = cxp - 0.5 + k
        path(cv, [(x, ty - s * 0.2), (x + 0.6, ty - s * 0.3), (x - 0.3, ty - s * 0.4)], 'v', 0.4)
    for k in range(3):
        x = tx + w * 0.42 + (k - 1) * 0.9
        cv.put(int(x), int(tyy - h * 0.22 - k * 1.2), 'v')
    return cv, [('b', 'wall', 'Wall', GREEN, True), role('t', 'teapot', 'Teapot', BLUE), role('l', 'lid', 'Lid', BLUE),
                role('p', 'pattern', 'Flower pattern', PINK), role('c', 'cup', 'Cup', PINK), role('v', 'steam', 'Steam', GREEN),
                role('d', 'table', 'Table', BROWN)], ['food', 'cozy']


def donut(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    ty = h * 0.86
    box(cv, 0, ty, w, h, 't')
    oval(cv, cx, ty, w * 0.42, 1.4, 'l')
    dy, dr = h * 0.55, s * 0.36
    ring(cv, cx, dy, dr, dr * 0.34, 'd')
    for y in range(h):
        for x in range(w):
            px, py = x + 0.5 - cx, y + 0.5 - dy
            d = math.hypot(px, py)
            wave = 0.85 + 0.08 * math.sin(math.atan2(py, px) * 7)
            if py < dr * 0.15:
                wave = 1.01
            if dr * 0.42 < d <= dr * wave and cv.g[y][x] == 'd':
                cv.g[y][x] = 'i'
    disc(cv, cx, dy, dr * 0.34, '.')
    for c in 'xyz':
        scatter(cv, c, 'i', 3 + (w * h) // 100, r, sep=2, area=(0, 0, w - 1, dy - 1))
    for x, y in ((0.14, 0.14), (0.86, 0.2)):
        star(cv, w * x, h * y, s * 0.05 + 0.5, 'z', ri=0.5)
    return cv, [('b', 'background', 'Background', BLUE, True), role('d', 'dough', 'Dough', BROWN), role('i', 'icing', 'Icing', PINK),
                role('x', 'sprinkle_green', 'Green sprinkles', GREEN), role('y', 'sprinkle_blue', 'Blue sprinkles', BLUE),
                role('z', 'sprinkle_gold', 'Gold sprinkles and stars', BROWN), role('l', 'plate', 'Plate', GREEN),
                role('t', 'table', 'Table', PINK)], ['food', 'sweets']


def watermelon(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h * 0.78
    box(cv, 0, gtop, w, h, 'b')
    dots(cv, 'd', 'b', 3, 2, area=(0, gtop + 1, w - 1, h - 1))
    my, mr = h * 0.4, w * 0.42
    for y in range(h):
        for x in range(w):
            px, py = x + 0.5 - cx, y + 0.5 - my
            d = math.hypot(px, py / 0.95)
            if py >= 0 and d <= mr:
                cv.g[y][x] = 'r' if d > mr - (1.0 if not big else 1.6) else 'w' if d > mr - (1.9 if not big else 2.8) else 'f'
    scatter(cv, 'k', 'f', 5 if not big else 9, r, sep=2)
    if big:
        sx = w * 0.84
        poly(cv, [(sx - s * 0.12, gtop + 0.5), (sx + s * 0.12, gtop + 0.5), (sx, gtop - s * 0.28)], 'f')
        box(cv, sx - s * 0.12, gtop - 0.5, sx + s * 0.12, gtop + 0.5, 'r')
    disc(cv, w * 0.86, h * 0.1, s * 0.085, 'u')
    return cv, [sky(), role('f', 'flesh', 'Flesh', PINK), role('w', 'pale_rind', 'Pale rind', GREEN), role('r', 'rind', 'Rind', GREEN),
                role('k', 'seeds', 'Seeds', BROWN), role('b', 'blanket', 'Picnic blanket', PINK), role('d', 'blanket_dots', 'Blanket dots', BROWN),
                role('u', 'sun', 'Sun', BLUE)], ['food', 'summer']


def pineapple(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    ty = h * 0.88
    box(cv, 0, ty, w, h, 't')
    oval(cv, cx, ty, w * 0.36, 1.3, 'q')
    py, prx, pry = h * 0.6, w * 0.24, h * 0.26
    oval(cv, cx, py, prx, pry, 'p')
    dots(cv, 'h', 'p', 2, 2, area=(0, 0, w - 1, h - 1))
    top = py - pry
    for k, a in enumerate((-60, -30, 0, 30, 60) if not big else (-70, -45, -20, 0, 20, 45, 70)):
        t = math.radians(a - 90)
        L = s * (0.3 if abs(a) < 35 else 0.22)
        lens(cv, cx, top + 1.0, cx + math.cos(t) * L, top + 1.0 + math.sin(t) * L, s * 0.08 + 0.4, 'c' if k % 2 == 0 else 'g')
    for x, y in ((0.12, 0.14), (0.88, 0.2), (0.1, 0.46)):
        disc(cv, w * x, h * y, s * 0.05 + 0.3, 'o')
    return cv, [('b', 'wall', 'Wall', BLUE, True), role('p', 'fruit', 'Pineapple', BROWN), role('h', 'scales', 'Scales', BROWN),
                role('c', 'crown', 'Crown', GREEN), role('g', 'crown2', 'Inner leaves', GREEN), role('q', 'plate', 'Plate', BLUE),
                role('t', 'table', 'Table', PINK), role('o', 'dots', 'Wall dots', PINK)], ['food', 'summer']


def popsicle(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.14)
    hills(cv, 'd', gtop, 0.4, w * 1.4, r.uniform(0, 6))
    for k, (x0, cols) in enumerate(((w * 0.3, 'abc'), (w * 0.7, 'e'))):
        top, bot = h * (0.18 + 0.1 * k), h * (0.66 + 0.04 * k)
        hw = w * 0.13
        seg(cv, x0, bot, x0, gtop + 0.5, 'k', 0.7)
        rbox(cv, x0 - hw, top, x0 + hw, bot, hw * 0.9, cols[0])
        if len(cols) == 3:
            box(cv, x0 - hw, top + (bot - top) * 0.33, x0 + hw, top + (bot - top) * 0.66, cols[1], only=cols[0])
            box(cv, x0 - hw, top + (bot - top) * 0.66, x0 + hw, bot, cols[2], only=cols[0])
        else:
            disc(cv, x0 + hw, top + 1.0, s * 0.08, 's')
            for dx in (-0.4, 0.5):
                box(cv, x0 + dx * hw * 1.6 - 0.5, bot - 0.2, x0 + dx * hw * 1.6 + 0.5, bot + 1.6, cols[0])
    disc(cv, w * 0.86, h * 0.1, s * 0.085, 'u')
    if big:
        cloud(cv, w * 0.18, h * 0.08, s * 0.05 + 0.4, 'm')
    return cv, [sky(), role('a', 'top', 'Pink top', PINK), role('b', 'middle', 'Orange middle', BROWN), role('c', 'bottom', 'Green bottom', GREEN),
                role('e', 'grape', 'Grape popsicle', PINK), role('k', 'stick', 'Sticks', BROWN), role('d', 'sand', 'Sand', GREEN),
                role('u', 'sun', 'Sun', BLUE), role('m', 'cloud', 'Cloud', BLUE)], ['food', 'summer']


def honey_pot(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.12)
    hills(cv, 'g', gtop, 0.4, w * 1.4, r.uniform(0, 6))
    px, py = cx - w * 0.04, gtop - h * 0.2
    oval(cv, px, py, w * 0.28, h * 0.2, 'p')
    box(cv, px - w * 0.2, py + h * 0.1, px + w * 0.2, gtop + 0.5, 'p')
    rbox(cv, px - w * 0.22, py - h * 0.22, px + w * 0.22, py - h * 0.15, 0.8, 'p')
    box(cv, px - w * 0.27, py - h * 0.04, px + w * 0.27, py + h * 0.04, 'l', only='p')
    for k in range(3 if not big else 5):
        x = px - w * 0.14 + k * (w * 0.28 / (2 if not big else 4))
        box(cv, x - 0.5, py - h * 0.16, x + 0.5, py - h * 0.16 + (2 + k % 2) * (1 if not big else 1.5), 'h')
    box(cv, px - w * 0.2, py - h * 0.19, px + w * 0.2, py - h * 0.15, 'h')
    seg(cv, px + w * 0.08, py - h * 0.2, px + w * 0.3, py - h * 0.42, 'k', 0.5)
    for k in range(2 if not big else 3):
        bx, by = w * (0.78 - k * 0.5 * (k % 2)), h * (0.18 + k * 0.14)
        oval(cv, bx, by, s * 0.08, s * 0.06, 'y')
        box(cv, bx - 0.5, by - s * 0.06, bx + 0.5, by + s * 0.06, 'k', only='y')
        oval(cv, bx - 0.4, by - s * 0.1, s * 0.06 + 0.3, s * 0.05 + 0.3, 'w')
    for x in (w * 0.08, w * 0.9) if not big else (w * 0.06, w * 0.9, w * 0.74):
        disc(cv, x, gtop + 1.0, 1.0, 'f')
    return cv, [sky(), role('p', 'pot', 'Pot', BLUE), role('l', 'label', 'Label', PINK), role('h', 'honey', 'Honey', BROWN),
                role('k', 'dipper', 'Dipper and stripes', BROWN), role('y', 'bee', 'Bees', BROWN), role('w', 'wings', 'Wings', GREEN),
                role('f', 'flowers', 'Flowers', PINK), role('g', 'grass', 'Grass', GREEN)], ['food', 'garden']


def pancakes(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    ty = h * 0.88
    box(cv, 0, ty, w, h, 't')
    oval(cv, cx, ty - 0.4, w * 0.42, h * 0.05, 'l')
    n = 4 if not big else 5
    ph = (h * 0.32) / n
    for k in range(n):
        y = ty - 1.0 - k * ph
        oval(cv, cx + (k % 2 - 0.5) * 0.6, y - ph / 2, w * 0.32, ph / 2 + 0.3, 'p')
        box(cv, 0, y - 0.4, w, y + 0.4, 'e', only='p')
    top = ty - 1.0 - n * ph
    oval(cv, cx, top + 0.6, w * 0.24, 1.2, 's')
    for x in (cx - w * 0.2, cx + w * 0.16) + ((cx - w * 0.05,) if big else ()):
        box(cv, x - 0.5, top + 0.6, x + 0.5, top + ph * 1.6, 's')
    rbox(cv, cx - w * 0.08, top - 2.0, cx + w * 0.08, top + 0.2, 0.3, 'u')
    for k, (dx, c) in enumerate(((-0.18, 'r'), (0.18, 'q'), (0.28, 'r'), (-0.28, 'q'))):
        if k < 2 or big:
            disc(cv, cx + dx * w, ty - 1.2, s * 0.05 + 0.4, c)
    for x, y in ((0.14, 0.16), (0.86, 0.22)):
        disc(cv, w * x, h * y, s * 0.05 + 0.3, 'q')
    return cv, [('b', 'wall', 'Wall', BLUE, True), role('p', 'pancake', 'Pancakes', BROWN), role('e', 'edge', 'Pancake edges', BROWN),
                role('s', 'syrup', 'Syrup', BROWN), role('u', 'butter', 'Butter', GREEN), role('r', 'strawberry', 'Strawberries', PINK),
                role('q', 'blueberry', 'Blueberries', BLUE), role('l', 'plate', 'Plate', PINK), role('t', 'table', 'Table', GREEN)], ['food', 'cozy']


FOOD_SUBJECTS = [cake, ice_cream, cupcake, lollipop, teapot, donut, watermelon, pineapple, popsicle, honey_pot, pancakes]

"""World subjects for the long run to Level 5000+ (the owner, 2026-10-06: "a house, a castle, a ship, a boat, a ball:
anything at all"): buildings, vehicles, toys and things, in the procedural style of sketch_pictures.py. Each subject
draws on a Canvas with picture_kit and returns (canvas, roles, themes); roles are (char, roleId, name, colorGroup,
isBackground) in the four launch color groups. Every subject has roles in three or four groups with two roles in most
of them, so it can carry six distinct variants, and it adds detail on the big boards (from 300 cells).
"""
import math

from picture_kit import (BLUE, BROWN, GREEN, PINK, box, cloud, disc, dots, ground_rows, heart, hills, lens, oval,
                         path, poly, rbox, ring, role, scatter, seg, sky, star, start)


# ---- Buildings ----

def house(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.12)
    hills(cv, 'g', gtop, 0.4, w * 1.5, r.uniform(0, 6))
    wy = h * 0.47
    box(cv, w * 0.2, wy, w * 0.8, gtop + 0.5, 'h')
    box(cv, w * 0.62, h * 0.17, w * 0.62 + max(1.8, w * 0.09), wy - 1, 'c')
    if big:
        for k in range(3):
            disc(cv, w * 0.68 + k * 1.1, h * 0.12 - k * h * 0.035, 0.8 + k * 0.2, 'k')
    poly(cv, [(w * 0.1, wy + 0.5), (cx, h * 0.15), (w * 0.9, wy + 0.5)], 'r')
    dw = max(1.0, w * 0.08)
    rbox(cv, cx - dw, wy + h * 0.2, cx + dw, gtop + 0.5, 0.8, 'd')
    for x0 in (w * 0.25, w * 0.61):
        x1 = x0 + w * 0.14
        box(cv, x0, wy + h * 0.07, x1, wy + h * 0.2, 'i')
        if big:
            box(cv, (x0 + x1) / 2 - 0.5, wy + h * 0.07, (x0 + x1) / 2 + 0.5, wy + h * 0.2, 'h')
            box(cv, x0, wy + h * 0.135 - 0.5, x1, wy + h * 0.135 + 0.5, 'h')
    poly(cv, [(cx - dw * 0.8, gtop + 0.5), (cx + dw * 0.8, gtop + 0.5), (cx + dw * 1.6, h), (cx - dw * 1.6, h)], 'p')
    for x in (w * 0.2, w * 0.8):
        oval(cv, x, gtop - 0.4, s * 0.1, s * 0.08, 'b')
    if big:
        for x in (w * 0.17, w * 0.23, w * 0.77, w * 0.83):
            cv.put(int(x), int(gtop - 0.4 - s * 0.05), 'o')
    disc(cv, w * 0.12, h * 0.09, s * 0.085, 'u')
    return cv, [sky(), role('h', 'wall', 'Walls', BROWN), role('r', 'roof', 'Roof', PINK), role('c', 'chimney', 'Chimney', BROWN),
                role('i', 'window', 'Windows', BLUE), role('d', 'door', 'Door', PINK), role('g', 'lawn', 'Lawn', GREEN),
                role('b', 'bush', 'Bushes', GREEN), role('p', 'path', 'Path', BROWN), role('u', 'sun', 'Sun', BROWN),
                role('k', 'smoke', 'Smoke', BLUE), role('o', 'flowers', 'Flowers', PINK)], ['buildings', 'cozy']


def castle(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.1)
    hills(cv, 'v', h * 0.8, 0.8, w * 0.9, r.uniform(0, 6))
    box(cv, 0, gtop, w, h, 'g')
    kx0, kx1, ky = w * 0.26, w * 0.74, h * 0.5
    box(cv, kx0, ky, kx1, gtop + 0.5, 'k')
    for x in range(int(kx0), int(kx1) + 1, 2):
        box(cv, x, ky - 1.0, x + 0.9, ky, 'k')
    tw = max(2.6, w * 0.17)
    for tx in (w * 0.1, w * 0.9 - tw):
        box(cv, tx, h * 0.34, tx + tw, gtop + 0.5, 't')
        poly(cv, [(tx - 0.6, h * 0.35), (tx + tw / 2, h * 0.14), (tx + tw + 0.6, h * 0.35)], 'r')
        seg(cv, tx + tw / 2, h * 0.14, tx + tw / 2, h * 0.06, 'p', 0.4)
        poly(cv, [(tx + tw / 2 + 0.4, h * 0.05), (tx + tw / 2 + 0.4 + max(1.6, w * 0.08), h * 0.075), (tx + tw / 2 + 0.4, h * 0.1)], 'f')
        box(cv, tx + tw / 2 - 0.5, h * 0.42, tx + tw / 2 + 0.5, h * 0.42 + (1.0 if not big else 2.0), 'i')
    if big:
        mw = w * 0.14
        box(cv, cx - mw / 2, h * 0.36, cx + mw / 2, ky, 't')
        poly(cv, [(cx - mw / 2 - 0.6, h * 0.37), (cx, h * 0.2), (cx + mw / 2 + 0.6, h * 0.37)], 'r')
    gw = w * 0.11
    rbox(cv, cx - gw, h * 0.68, cx + gw, gtop + 0.5, gw, 'd')
    if big:
        for x in (cx - gw * 0.5, cx + gw * 0.5):
            seg(cv, x, h * 0.72, x, gtop, 'p', 0.4)
    for x in (cx - w * 0.15, cx + w * 0.15):
        box(cv, x - 0.5, h * 0.56, x + 0.5, h * 0.56 + (1.0 if not big else 1.8), 'i')
    return cv, [sky(), role('k', 'keep', 'Keep', BROWN), role('t', 'tower', 'Towers', BROWN), role('r', 'roof', 'Roofs', PINK),
                role('f', 'flag', 'Flags', PINK), role('p', 'pole', 'Poles and gate bars', BROWN), role('d', 'gate', 'Gate', BROWN),
                role('i', 'window', 'Windows', BLUE), role('g', 'grass', 'Grass', GREEN), role('v', 'hills', 'Hills', GREEN)], ['buildings', 'fairy']


def lighthouse(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    sea = h * 0.76
    hills(cv, 'w', sea, 0.35, w * 0.45, r.uniform(0, 6))
    oval(cv, cx, h * 0.98, w * 0.36, h * 0.18, 'k')
    oval(cv, cx, h * 0.84, w * 0.25, h * 0.05, 'g')
    poly(cv, [(cx - w * 0.11, h * 0.32), (cx + w * 0.11, h * 0.32), (cx + w * 0.17, h * 0.84), (cx - w * 0.17, h * 0.84)], 't')
    for y in (0.44, 0.62) if not big else (0.42, 0.56, 0.7):
        box(cv, 0, h * y, w, h * y + (1.0 if not big else 1.6), 'b', only='t')
    box(cv, cx - w * 0.17, h * 0.3, cx + w * 0.17, h * 0.3 + 0.9, 'b')
    box(cv, cx - w * 0.08, h * 0.19, cx + w * 0.08, h * 0.3, 'l')
    poly(cv, [(cx - w * 0.13, h * 0.2), (cx, h * 0.08), (cx + w * 0.13, h * 0.2)], 'r')
    poly(cv, [(cx + w * 0.09, h * 0.23), (w * 0.88, h * 0.15), (w * 0.88, h * 0.27)], 'y')
    poly(cv, [(cx - w * 0.09, h * 0.23), (w * 0.12, h * 0.15), (w * 0.12, h * 0.27)], 'y')
    rbox(cv, cx - 0.9, h * 0.75, cx + 0.9, h * 0.84, 0.8, 'd')
    if big:
        for x, y in ((0.2, 0.08), (0.78, 0.06)):
            path(cv, [(w * x - 1, h * y), (w * x, h * y + 0.8), (w * x + 1, h * y)], 'd', 0.4)
    return cv, [sky(), role('t', 'tower', 'Tower', PINK), role('b', 'band', 'Bands', BROWN), role('l', 'light', 'Lamp', BROWN),
                role('r', 'roof', 'Roof', PINK), role('y', 'beam', 'Light beams', BROWN), role('d', 'door', 'Door and gulls', GREEN),
                role('k', 'rock', 'Rocks', BROWN), role('g', 'grass', 'Grass', GREEN), role('w', 'sea', 'Sea', BLUE)], ['buildings', 'sea']


def windmill(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.14)
    hills(cv, 'g', gtop, 0.5, w * 1.2, r.uniform(0, 6))
    poly(cv, [(cx - w * 0.13, h * 0.36), (cx + w * 0.13, h * 0.36), (cx + w * 0.2, gtop + 0.5), (cx - w * 0.2, gtop + 0.5)], 'm')
    poly(cv, [(cx - w * 0.17, h * 0.37), (cx, h * 0.22), (cx + w * 0.17, h * 0.37)], 'r')
    hx, hy = cx, h * 0.3
    L = min(w * 0.42, h * 0.36)
    for a in (45, 135, 225, 315):
        t = math.radians(a)
        ex, ey = hx + math.cos(t) * L, hy + math.sin(t) * L
        if big:
            n = (-math.sin(t), math.cos(t))
            poly(cv, [(hx + math.cos(t) * L * 0.3, hy + math.sin(t) * L * 0.3), (ex, ey),
                      (ex + n[0] * 2.2, ey + n[1] * 2.2), (hx + math.cos(t) * L * 0.3 + n[0] * 2.2, hy + math.sin(t) * L * 0.3 + n[1] * 2.2)], 'v')
        seg(cv, hx, hy, ex, ey, 'b', 0.6)
    disc(cv, hx, hy, 0.9, 'b')
    rbox(cv, cx - 0.9, h * 0.72, cx + 0.9, gtop + 0.5, 0.8, 'd')
    box(cv, cx - 0.5, h * 0.5, cx + 0.5, h * 0.5 + (1.0 if not big else 1.8), 'i')
    for x in (w * 0.08, w * 0.9) if not big else (w * 0.06, w * 0.16, w * 0.84, w * 0.94):
        disc(cv, x, gtop + 1.0, 0.9, 'f')
    return cv, [sky(), role('m', 'mill', 'Mill', BROWN), role('r', 'cap', 'Cap', PINK), role('b', 'blade', 'Blades', BROWN),
                role('v', 'sail', 'Sails', PINK), role('d', 'door', 'Door', BLUE), role('i', 'window', 'Window', BLUE),
                role('f', 'flowers', 'Flowers', PINK), role('g', 'field', 'Field', GREEN)], ['buildings', 'farm']


def barn(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.12)
    box(cv, 0, gtop, w, h, 'g')
    if big:
        sx = w * 0.88
        rbox(cv, sx - w * 0.07, h * 0.3, sx + w * 0.07, gtop + 0.5, 1.2, 'o')
        oval(cv, sx, h * 0.3, w * 0.07, h * 0.04, 'r')
    x0, x1 = w * 0.14, w * 0.76 if big else w * 0.86
    bx = (x0 + x1) / 2
    bw = x1 - x0
    poly(cv, [(x0, h * 0.44), (x0 + bw * 0.2, h * 0.24), (x1 - bw * 0.2, h * 0.24), (x1, h * 0.44), (x1, gtop + 0.5), (x0, gtop + 0.5)], 'b')
    path(cv, [(x0 - 0.6, h * 0.45), (x0 + bw * 0.2, h * 0.23), (x1 - bw * 0.2, h * 0.23), (x1 + 0.6, h * 0.45)], 'r', 0.6)
    dx0, dx1 = bx - bw * 0.22, bx + bw * 0.22
    box(cv, dx0, h * 0.6, dx1, gtop + 0.5, 'd')
    for a, b in ((dx0, dx1), (dx1, dx0)):
        seg(cv, a + (0.5 if a < b else -0.5), h * 0.6 + 0.5, b + (-0.5 if a < b else 0.5), gtop, 'x', 0.45)
    box(cv, dx0, h * 0.6, dx1, h * 0.6 + 0.9, 'x')
    box(cv, bx - bw * 0.1, h * 0.36, bx + bw * 0.1, h * 0.48, 'i')
    for x in (w * 0.08,) if not big else (w * 0.06,):
        oval(cv, x + 0.5, gtop - 0.6, s * 0.1, s * 0.09, 'y')
    return cv, [sky(), role('b', 'barn', 'Barn', PINK), role('r', 'trim', 'Roof trim', BROWN), role('d', 'door', 'Doors', BROWN),
                role('x', 'beam', 'Beams', BLUE), role('i', 'loft', 'Loft', BROWN), role('y', 'hay', 'Hay', BROWN),
                role('o', 'silo', 'Silo', BLUE), role('g', 'grass', 'Grass', GREEN)], ['buildings', 'farm']


def tent(w, h, r):
    cv, s, cx, big = start(w, h, 'n')
    gtop = h - ground_rows(h, 0.14)
    hills(cv, 'g', gtop, 0.4, w * 1.4, r.uniform(0, 6))
    for x in (w * 0.08, w * 0.92) if not big else (w * 0.06, w * 0.94, w * 0.17):
        for k in range(3):
            y = gtop - h * (0.08 + k * 0.08)
            poly(cv, [(x - s * (0.12 - k * 0.025), y + h * 0.04), (x, y - h * 0.08), (x + s * (0.12 - k * 0.025), y + h * 0.04)], 'p')
        box(cv, x - 0.5, gtop - h * 0.05, x + 0.5, gtop + 0.5, 'k')
    tx = cx + w * 0.04
    poly(cv, [(tx - w * 0.32, gtop + 0.5), (tx, h * 0.3), (tx + w * 0.32, gtop + 0.5)], 't')
    poly(cv, [(tx - w * 0.1, gtop + 0.5), (tx, h * 0.55), (tx + w * 0.1, gtop + 0.5)], 'd')
    seg(cv, tx, h * 0.3, tx, h * 0.19, 'k', 0.4)
    poly(cv, [(tx + 0.4, h * 0.19), (tx + max(1.8, w * 0.1), h * 0.215), (tx + 0.4, h * 0.25)], 'f')
    fx = w * 0.18 if not big else w * 0.3
    if big:
        seg(cv, fx - 1.5, gtop + 0.6, fx + 1.5, gtop - 0.2, 'k', 0.5)
        seg(cv, fx - 1.5, gtop - 0.2, fx + 1.5, gtop + 0.6, 'k', 0.5)
        poly(cv, [(fx - 1.2, gtop - 0.6), (fx - 0.4, gtop - 2.6), (fx, gtop - 1.4), (fx + 0.5, gtop - 3.2), (fx + 1.2, gtop - 0.6)], 'x')
    disc(cv, w * 0.84, h * 0.11, s * 0.1, 'm')
    disc(cv, w * 0.84 - s * 0.05, h * 0.11 - s * 0.03, s * 0.08, 'n')
    for x, y in ((0.1, 0.08), (0.3, 0.16), (0.52, 0.06), (0.66, 0.2), (0.16, 0.32), (0.92, 0.3)):
        cv.put(int(w * x), int(h * y), 'x')
    return cv, [('n', 'night', 'Night sky', BLUE, True), role('t', 'tent', 'Tent', PINK), role('d', 'door', 'Door', BROWN),
                role('k', 'pole', 'Pole and logs', BROWN), role('f', 'flag', 'Flag', PINK), role('x', 'stars', 'Stars and fire', BROWN),
                role('m', 'moon', 'Moon', BLUE), role('p', 'pine', 'Pines', GREEN), role('g', 'meadow', 'Meadow', GREEN)], ['camping', 'night']


def tree_house(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.1)
    box(cv, 0, gtop, w, h, 'g')
    box(cv, cx - max(1.0, w * 0.06), h * 0.45, cx + max(1.0, w * 0.06), gtop + 0.5, 't')
    disc(cv, cx, h * 0.3, s * 0.33, 'c')
    disc(cv, cx - s * 0.25, h * 0.4, s * 0.19, 'c')
    disc(cv, cx + s * 0.25, h * 0.4, s * 0.19, 'c')
    box(cv, w * 0.28, h * 0.42, w * 0.72, h * 0.6, 'h')
    box(cv, w * 0.24, h * 0.6, w * 0.76, h * 0.6 + 0.9, 't')
    poly(cv, [(w * 0.22, h * 0.43), (cx, h * 0.26), (w * 0.78, h * 0.43)], 'r')
    box(cv, w * 0.36, h * 0.47, w * 0.47, h * 0.55, 'i')
    rbox(cv, w * 0.55, h * 0.48, w * 0.64, h * 0.6, 0.6, 'd')
    for x in (w * 0.7, w * 0.8):
        seg(cv, x, h * 0.61, x, gtop, 'l', 0.4)
    for y in range(int(h * 0.64), int(gtop), 2):
        seg(cv, w * 0.7, y + 0.5, w * 0.8, y + 0.5, 'l', 0.4)
    if big:
        for x in (w * 0.2, w * 0.32):
            seg(cv, x, h * 0.48, x, h * 0.78, 'd', 0.4)
        box(cv, w * 0.18, h * 0.78, w * 0.34, h * 0.78 + 0.9, 'd')
    disc(cv, w * 0.88, h * 0.08, s * 0.08, 'u')
    return cv, [sky(), role('c', 'leaves', 'Leaves', GREEN), role('t', 'trunk', 'Trunk and deck', BROWN), role('h', 'hut', 'Hut', BROWN),
                role('r', 'roof', 'Roof', PINK), role('i', 'window', 'Window', BLUE), role('d', 'door', 'Door and swing', PINK),
                role('l', 'ladder', 'Ladder', BROWN), role('g', 'grass', 'Grass', GREEN), role('u', 'sun', 'Sun', BLUE)], ['buildings', 'forest']


# ---- Vehicles ----

def ship(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    sea = h * 0.72
    hills(cv, 'w', sea, 0.4, w * 0.5, r.uniform(0, 6))
    poly(cv, [(w * 0.04, sea - h * 0.12), (w * 0.96, sea - h * 0.12), (w * 0.82, sea + h * 0.07), (w * 0.18, sea + h * 0.07)], 'h')
    box(cv, 0, sea - h * 0.12, w, sea - h * 0.12 + 1.0, 'b', only='h')
    box(cv, w * 0.22, sea - h * 0.25, w * 0.72, sea - h * 0.12, 'c')
    if big:
        box(cv, w * 0.3, sea - h * 0.33, w * 0.6, sea - h * 0.25, 'c')
    for k in range(4 if big else 3):
        x = w * 0.3 + k * (w * 0.36 / (3 if big else 2))
        disc(cv, x, sea - h * 0.185, 0.75, 'i')
    top = sea - h * (0.33 if big else 0.25)
    for x0 in (0.3, 0.5):
        box(cv, w * x0, top - h * 0.16, w * x0 + max(1.8, w * 0.1), top, 'f')
        box(cv, w * x0, top - h * 0.16, w * x0 + max(1.8, w * 0.1), top - h * 0.16 + 0.9, 'k')
    cloud(cv, w * 0.26, h * 0.12, s * 0.07 + 0.6, 'm')
    if big:
        cloud(cv, w * 0.76, h * 0.1, s * 0.06 + 0.5, 'm')
        ring(cv, w * 0.84, sea - h * 0.05, 1.5, 0.6, 'o')
    return cv, [sky(), role('h', 'hull', 'Hull', BROWN), role('b', 'stripe', 'Stripe', GREEN), role('c', 'cabin', 'Cabin', PINK),
                role('i', 'portholes', 'Portholes', BLUE), role('f', 'funnel', 'Funnels', BROWN), role('k', 'funnel_top', 'Funnel tops', PINK),
                role('m', 'smoke', 'Smoke', GREEN), role('o', 'buoy', 'Life buoy', PINK), role('w', 'sea', 'Sea', BLUE)], ['vehicles', 'sea']


def rowboat(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    lake = h * 0.62
    hills(cv, 'w', lake, 0.3, w * 0.6, r.uniform(0, 6))
    ix = w * 0.9
    oval(cv, ix, lake + 0.6, w * 0.14, 1.4, 'g')
    if big:
        box(cv, ix - 0.5, lake - h * 0.12, ix + 0.5, lake, 'o')
        disc(cv, ix, lake - h * 0.14, s * 0.09, 'g')
    oval(cv, w * 0.7, h * 0.9, w * 0.12, h * 0.035, 'p')
    if big:
        oval(cv, w * 0.42, h * 0.94, w * 0.09, h * 0.03, 'p')
    by = lake - h * 0.02
    rx, ry = w * 0.34, h * 0.18
    seg(cv, cx - rx * 0.25, by - ry * 1.0, cx - rx * 1.25, by + ry * 1.5, 'o', 0.45)
    seg(cv, cx + rx * 0.25, by - ry * 1.0, cx + rx * 1.25, by + ry * 1.5, 'o', 0.45)
    for y in range(h):
        for x in range(w):
            dx, dy = (x + 0.5 - cx) / rx, (y + 0.5 - by) / ry
            if dy >= -0.3 and dx * dx + dy * dy <= 1.0:
                cv.g[y][x] = 'b'
    poly(cv, [(cx - rx * 0.92, by - ry * 0.3), (cx - rx * 1.1, by - ry * 0.85), (cx - rx * 0.7, by - ry * 0.3)], 'b')
    poly(cv, [(cx + rx * 0.92, by - ry * 0.3), (cx + rx * 1.1, by - ry * 0.85), (cx + rx * 0.7, by - ry * 0.3)], 'b')
    box(cv, 0, by - ry * 0.3, w, by - ry * 0.3 + 1.0, 'm', only='b')
    if big:
        box(cv, 0, by + ry * 0.3, w, by + ry * 0.3 + 0.9, 'm', only='b')
    disc(cv, w * 0.82, h * 0.13, s * 0.09, 'u')
    cloud(cv, w * 0.3, h * 0.16, s * 0.07 + 0.5, 'c')
    return cv, [sky(), role('b', 'boat', 'Boat', BROWN), role('m', 'rim', 'Stripes', PINK), role('o', 'oar', 'Oars and trunk', BROWN),
                role('w', 'lake', 'Lake', BLUE), role('g', 'island', 'Island', GREEN), role('p', 'pad', 'Lily pads', GREEN),
                role('u', 'sun', 'Sun', PINK), role('c', 'cloud', 'Cloud', BLUE)], ['vehicles', 'lake']


def car(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.12)
    box(cv, 0, gtop, w, h, 'd')
    if big:
        for x in range(1, w, 4):
            box(cv, x, gtop + 1.2, x + 1.9, gtop + 1.9, 'y')
    cy = gtop - h * 0.12
    rbox(cv, w * 0.06, cy - h * 0.12, w * 0.94, cy + h * 0.06, 1.0, 'c')
    poly(cv, [(w * 0.22, cy - h * 0.11), (w * 0.33, cy - h * 0.28), (w * 0.67, cy - h * 0.28), (w * 0.8, cy - h * 0.11)], 'c')
    poly(cv, [(w * 0.28, cy - h * 0.12), (w * 0.36, cy - h * 0.25), (w * 0.48, cy - h * 0.25), (w * 0.48, cy - h * 0.12)], 'i')
    poly(cv, [(w * 0.54, cy - h * 0.12), (w * 0.54, cy - h * 0.25), (w * 0.65, cy - h * 0.25), (w * 0.74, cy - h * 0.12)], 'i')
    if big:
        seg(cv, w * 0.51, cy - h * 0.1, w * 0.51, cy + h * 0.04, 'k', 0.4)
        box(cv, w * 0.42, cy - h * 0.05, w * 0.46, cy - h * 0.05 + 0.6, 'k')
    for x in (0.26, 0.74):
        disc(cv, w * x, cy + h * 0.07, s * 0.12, 't')
        disc(cv, w * x, cy + h * 0.07, s * 0.05 + 0.3, 'm')
    box(cv, w * 0.89, cy - h * 0.08, w * 0.95, cy - h * 0.02, 'l')
    box(cv, w * 0.05, cy - h * 0.08, w * 0.1, cy - h * 0.03, 'l')
    cloud(cv, w * 0.72, h * 0.14, s * 0.07 + 0.5, 'k')
    return cv, [sky(), role('c', 'body', 'Body', PINK), role('i', 'window', 'Windows', BLUE), role('t', 'tyre', 'Tyres', BROWN),
                role('m', 'hub', 'Hubs', GREEN), role('l', 'lamp', 'Lamps', BROWN), role('k', 'trim', 'Door and cloud', PINK),
                role('d', 'road', 'Road', GREEN), role('y', 'lines', 'Road lines', BROWN)], ['vehicles', 'town']


def bus(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.12)
    box(cv, 0, gtop, w, h, 'd')
    by0, by1 = h * 0.34, gtop - h * 0.1
    rbox(cv, w * 0.04, by0, w * 0.96, by1, 1.0, 'b')
    n = 3 if not big else 4
    for k in range(n):
        x = w * 0.08 + k * (w * 0.64 / n)
        box(cv, x, by0 + h * 0.05, x + w * 0.64 / n - 1.0, by0 + h * 0.17, 'i')
    box(cv, w * 0.76, by0 + h * 0.05, w * 0.9, by1 - 0.5, 'e')
    box(cv, w * 0.04, by0 + h * 0.22, w * 0.74, by0 + h * 0.22 + (1.0 if not big else 1.6), 'z')
    for x in (0.24, 0.74):
        disc(cv, w * x, by1 + 0.4, s * 0.11, 't')
        if big:
            disc(cv, w * x, by1 + 0.4, 0.9, 'm')
    box(cv, w * 0.9, by1 - h * 0.07, w * 0.96, by1 - h * 0.02, 'l')
    disc(cv, w * 0.14, h * 0.12, s * 0.085, 'u')
    return cv, [sky(), role('b', 'bus', 'Bus', BROWN), role('i', 'window', 'Windows', BLUE), role('e', 'door', 'Door', BLUE),
                role('z', 'stripe', 'Stripe', PINK), role('t', 'wheel', 'Wheels', BROWN), role('m', 'hub', 'Hubs', GREEN),
                role('l', 'light', 'Light', PINK), role('d', 'road', 'Road', GREEN), role('u', 'sun', 'Sun', PINK)], ['vehicles', 'town']


def train(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.1)
    box(cv, 0, gtop, w, h, 'g')
    box(cv, w * 0.04, gtop - 0.9, w * 0.96, gtop, 'k')
    wy = gtop - 1.0
    ex0, ex1 = (w * 0.06, w * 0.62) if not big else (w * 0.3, w * 0.74)
    box(cv, ex0, h * 0.46, ex1, wy - h * 0.06, 'e')
    box(cv, ex1 - 0.5, h * 0.3, ex1 + w * 0.24, wy - h * 0.06, 'c')
    poly(cv, [(ex1 - 1.0, h * 0.3), (ex1 + w * 0.26, h * 0.3), (ex1 + w * 0.24, h * 0.27), (ex1 - 0.5, h * 0.27)], 'k')
    box(cv, ex1 + w * 0.04, h * 0.34, ex1 + w * 0.18, h * 0.44, 'i')
    fx = ex0 + w * 0.1
    box(cv, fx - max(0.9, w * 0.05), h * 0.3, fx + max(0.9, w * 0.05), h * 0.46, 'f')
    box(cv, fx - max(1.4, w * 0.07), h * 0.27, fx + max(1.4, w * 0.07), h * 0.3, 'f')
    cloud(cv, fx + 1, h * 0.17, s * 0.06 + 0.5, 'm')
    if big:
        cloud(cv, fx + 4, h * 0.08, s * 0.05 + 0.4, 'm')
        box(cv, w * 0.03, h * 0.54, w * 0.26, wy - h * 0.06, 'c')
        box(cv, w * 0.03, h * 0.52, w * 0.26, h * 0.54 + 0.4, 'k')
        box(cv, w * 0.26, wy - h * 0.1, w * 0.3, wy - h * 0.08, 'k')
    poly(cv, [(ex0, wy - h * 0.06), (ex0 - w * 0.05, wy), (ex0, wy)], 'k')
    wheels = (0.16, 0.36, 0.56, 0.76) if not big else (0.08, 0.2, 0.4, 0.58, 0.8)
    for x in wheels:
        disc(cv, w * x, wy - h * 0.035, s * 0.07 + 0.25, 't')
    return cv, [sky(), role('e', 'boiler', 'Boiler', PINK), role('c', 'cab', 'Cab and tender', BROWN), role('i', 'window', 'Window', BLUE),
                role('f', 'funnel', 'Funnel', BROWN), role('m', 'steam', 'Steam', BLUE), role('t', 'wheel', 'Wheels', PINK),
                role('k', 'rail', 'Rails and roof', BROWN), role('g', 'grass', 'Grass', GREEN)], ['vehicles', 'travel']


def tractor(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.14)
    hills(cv, 'f', gtop, 0.4, w * 1.6, r.uniform(0, 6))
    dots(cv, 'q', 'f', 3, 2, area=(0, gtop + 1, w - 1, h - 1))
    box(cv, w * 0.16, h * 0.44, w * 0.62, gtop - h * 0.12, 'b')
    box(cv, w * 0.46, h * 0.2, w * 0.8, h * 0.56, 'c')
    box(cv, w * 0.52, h * 0.26, w * 0.74, h * 0.42, 'i')
    box(cv, w * 0.22, h * 0.28, w * 0.28, h * 0.44, 'x')
    if big:
        box(cv, w * 0.42, h * 0.17, w * 0.84, h * 0.2, 'x')
    disc(cv, w * 0.68, gtop - h * 0.12, s * 0.2, 't')
    disc(cv, w * 0.68, gtop - h * 0.12, s * 0.07 + 0.2, 'h')
    disc(cv, w * 0.22, gtop - h * 0.06, s * 0.11, 't')
    disc(cv, w * 0.22, gtop - h * 0.06, 0.8, 'h')
    disc(cv, w * 0.12, h * 0.1, s * 0.085, 'u')
    return cv, [sky(), role('b', 'body', 'Body', GREEN), role('c', 'cab', 'Cab', GREEN), role('i', 'window', 'Window', BLUE),
                role('x', 'exhaust', 'Exhaust and roof', BROWN), role('t', 'tyre', 'Tyres', BROWN), role('h', 'hub', 'Hubs', PINK),
                role('f', 'field', 'Field', BROWN), role('q', 'sprouts', 'Sprouts', GREEN), role('u', 'sun', 'Sun', PINK)], ['vehicles', 'farm']


def airplane(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    hills(cv, 'h', h * 0.9, 0.6, w * 0.8, r.uniform(0, 6))
    py = h * 0.46
    fh = max(1.6, h * 0.08)
    poly(cv, [(w * 0.08, py - fh * 0.6), (w * 0.12, py - h * 0.24), (w * 0.24, py - h * 0.24), (w * 0.3, py - fh * 0.6)], 't')
    rbox(cv, w * 0.08, py - fh, w * 0.84, py + fh, fh, 'p')
    oval(cv, w * 0.82, py, w * 0.1, fh * 0.95, 'p')
    poly(cv, [(w * 0.4, py), (w * 0.62, py), (w * 0.5, py + h * 0.24), (w * 0.36, py + h * 0.24)], 'g')
    poly(cv, [(w * 0.06, py + 0.2), (w * 0.24, py + 0.2), (w * 0.16, py + h * 0.1)], 'g')
    for k in range(3 if not big else 5):
        disc(cv, w * 0.3 + k * w * (0.34 / (2 if not big else 4)), py - fh * 0.3, 0.7, 'i')
    poly(cv, [(w * 0.72, py - fh * 0.2), (w * 0.8, py - fh * 0.9), (w * 0.86, py - fh * 0.2)], 'i')
    seg(cv, w * 0.94, py - h * 0.12, w * 0.94, py + h * 0.12, 'r', 0.5)
    cloud(cv, w * 0.76, h * 0.16, s * 0.07 + 0.5, 'c')
    cloud(cv, w * 0.24, h * 0.76, s * 0.06 + 0.5, 'c')
    return cv, [sky(), role('p', 'plane', 'Plane', PINK), role('g', 'wing', 'Wings', BROWN), role('t', 'tail', 'Tail', BROWN),
                role('i', 'window', 'Windows', BLUE), role('r', 'propeller', 'Propeller', BROWN), role('c', 'cloud', 'Clouds', GREEN),
                role('h', 'hills', 'Hills', GREEN)], ['vehicles', 'sky']


def hot_air_balloon(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    hills(cv, 'g', h * 0.88, 0.7, w * 1.1, r.uniform(0, 6))
    for x0 in (w * 0.1, w * 0.62):
        box(cv, x0, h * 0.91, x0 + w * 0.22, h - 1.6, 'v', only='g')
    by = h * 0.36
    rx, ry = w * 0.34, h * 0.26
    n = 5 if not big else 7
    for y in range(h):
        for x in range(w):
            px, py = x + 0.5, y + 0.5
            dx, dy = (px - cx) / rx, (py - by) / ry
            inside = dx * dx + dy * dy <= 1.0 or (py > by and abs(px - cx) <= rx * (1.0 - (py - by) / (ry * 1.6)) and py < by + ry * 1.25)
            if inside:
                u = (math.asin(max(-1.0, min(1.0, dx))) / math.pi + 0.5) * n
                cv.g[y][x] = 'a' if int(u) % 2 == 0 else 'q'
    box(cv, cx - rx, by + ry * 0.5, cx + rx, by + ry * 0.5 + 0.9, 'k', only='aq')
    bt = by + ry * 1.25
    for side in (-1, 1):
        seg(cv, cx + side * rx * 0.3, bt, cx + side * rx * 0.25, bt + h * 0.08, 'k', 0.4)
    rbox(cv, cx - w * 0.09, bt + h * 0.08, cx + w * 0.09, bt + h * 0.16, 0.6, 'b')
    cloud(cv, w * 0.8, h * 0.72, s * 0.05 + 0.4, 'c')
    if big:
        cloud(cv, w * 0.2, h * 0.14, s * 0.05 + 0.4, 'c')
        rbox(cv, w * 0.74, h * 0.83, w * 0.84, h * 0.9, 0.3, 'b')
        poly(cv, [(w * 0.72, h * 0.83), (w * 0.79, h * 0.78), (w * 0.86, h * 0.83)], 'k')
    return cv, [sky(), role('a', 'balloon', 'Balloon', PINK), role('q', 'gore', 'Balloon stripes', BROWN), role('k', 'rope', 'Ropes and band', BROWN),
                role('b', 'basket', 'Basket', PINK), role('c', 'cloud', 'Clouds', BLUE), role('g', 'hills', 'Hills', GREEN),
                role('v', 'fields', 'Fields', GREEN)], ['vehicles', 'sky']


def rocket(w, h, r):
    cv, s, cx, big = start(w, h, 'n')
    disc(cv, w * 0.16, h * 0.78, s * 0.14, 'q')
    seg(cv, w * 0.16 - s * 0.24, h * 0.8, w * 0.16 + s * 0.24, h * 0.76, 'o', 0.5)
    disc(cv, w * 0.84, h * 0.12, s * 0.09, 'm')
    bx = cx + w * 0.06
    poly(cv, [(bx, h * 0.06), (bx + w * 0.15, h * 0.26), (bx + w * 0.15, h * 0.68), (bx - w * 0.15, h * 0.68), (bx - w * 0.15, h * 0.26)], 'b')
    poly(cv, [(bx, h * 0.06), (bx + w * 0.15, h * 0.26), (bx - w * 0.15, h * 0.26)], 'c')
    disc(cv, bx, h * 0.39, s * 0.09, 'i')
    if big:
        ring(cv, bx, h * 0.39, s * 0.09, s * 0.05, 'c')
    poly(cv, [(bx - w * 0.15, h * 0.48), (bx - w * 0.3, h * 0.74), (bx - w * 0.15, h * 0.68)], 'c')
    poly(cv, [(bx + w * 0.15, h * 0.48), (bx + w * 0.3, h * 0.74), (bx + w * 0.15, h * 0.68)], 'c')
    poly(cv, [(bx - w * 0.1, h * 0.69), (bx + w * 0.1, h * 0.69), (bx, h * 0.96)], 'f')
    poly(cv, [(bx - w * 0.05, h * 0.69), (bx + w * 0.05, h * 0.69), (bx, h * 0.84)], 'y')
    for x, y in ((0.1, 0.08), (0.3, 0.2), (0.62, 0.06), (0.12, 0.42), (0.86, 0.36), (0.9, 0.62), (0.42, 0.9), (0.78, 0.86)):
        cv.put(int(w * x), int(h * y), 'x')
    return cv, [('n', 'space', 'Space', PINK, True), role('b', 'body', 'Body', BLUE), role('c', 'nose', 'Nose and fins', GREEN),
                role('i', 'window', 'Window', PINK), role('f', 'flame', 'Flame', BROWN), role('y', 'flame_core', 'Flame core', BROWN),
                role('x', 'stars', 'Stars', BROWN), role('q', 'planet', 'Planet', GREEN), role('o', 'ring', 'Planet ring', BLUE),
                role('m', 'moon', 'Moon', BLUE)], ['vehicles', 'space']


def submarine(w, h, r):
    cv, s, cx, big = start(w, h, 'w')
    gtop = h - ground_rows(h, 0.1)
    for x in (w * 0.1, w * 0.88) + ((w * 0.7,) if big else ()):
        path(cv, [(x, gtop), (x + 0.8, h * 0.84), (x - 0.4, h * 0.74), (x + 0.5, h * 0.66)], 'k', 0.55)
    hills(cv, 'd', gtop, 0.5, w * 0.9, r.uniform(0, 6))
    sy = h * 0.5
    oval(cv, w * 0.46, sy, w * 0.38, h * 0.14, 'b')
    rbox(cv, w * 0.36, sy - h * 0.25, w * 0.56, sy - h * 0.1, 0.8, 't')
    path(cv, [(w * 0.5, sy - h * 0.25), (w * 0.5, sy - h * 0.36), (w * 0.58, sy - h * 0.36)], 'p', 0.45)
    for k in range(3 if not big else 4):
        disc(cv, w * (0.24 + k * (0.42 / (2 if not big else 3))), sy, s * 0.05 + 0.4, 'i')
    poly(cv, [(w * 0.82, sy), (w * 0.96, sy - h * 0.12), (w * 0.96, sy + h * 0.12)], 't')
    for k in range(4):
        disc(cv, w * (0.1 - (k % 2) * 0.03), sy - h * (0.04 + k * 0.08), 0.6 + k * 0.12, 'o')
    if big:
        oval(cv, w * 0.76, h * 0.18, w * 0.06, h * 0.025, 'f')
        poly(cv, [(w * 0.81, h * 0.18), (w * 0.86, h * 0.15), (w * 0.86, h * 0.21)], 'f')
    return cv, [('w', 'water', 'Deep water', BLUE, True), role('b', 'hull', 'Hull', BROWN), role('t', 'tower', 'Tower and fin', BROWN),
                role('p', 'periscope', 'Periscope', PINK), role('i', 'portholes', 'Portholes', BLUE), role('o', 'bubbles', 'Bubbles', PINK),
                role('k', 'weed', 'Weed', GREEN), role('d', 'sand', 'Sand', GREEN), role('f', 'fish', 'Fish', PINK)], ['vehicles', 'sea']


def helicopter(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.1)
    box(cv, 0, gtop, w, h, 'g')
    hy = h * 0.5
    oval(cv, w * 0.38, hy, w * 0.25, h * 0.15, 'b')
    oval(cv, w * 0.28, hy - h * 0.03, w * 0.1, h * 0.08, 'i')
    poly(cv, [(w * 0.58, hy - h * 0.05), (w * 0.9, hy - h * 0.03), (w * 0.9, hy + 0.2), (w * 0.58, hy + h * 0.05)], 'b')
    rbox(cv, w * 0.86, hy - h * 0.12, w * 0.93, hy + h * 0.04, 0.6, 't')
    box(cv, w * 0.36, hy - h * 0.22, w * 0.42, hy - h * 0.14, 'm')
    box(cv, w * 0.06, hy - h * 0.24, w * 0.74, hy - h * 0.22, 'r')
    for dx in (-0.1, 0.1):
        seg(cv, w * (0.38 + dx), hy + h * 0.13, w * (0.38 + dx * 1.2), gtop - h * 0.05, 'k', 0.4)
    box(cv, w * 0.18, gtop - h * 0.06, w * 0.6, gtop - h * 0.06 + 0.9, 'k')
    if big:
        box(cv, w * 0.42, hy - h * 0.02, w * 0.5, hy + h * 0.06, 'i')
    cloud(cv, w * 0.8, h * 0.16, s * 0.07 + 0.4, 'c')
    return cv, [sky(), role('b', 'body', 'Body', PINK), role('i', 'window', 'Windows', BLUE), role('t', 'tail', 'Tail rotor', BROWN),
                role('m', 'mast', 'Mast', BROWN), role('r', 'rotor', 'Rotor', BROWN), role('k', 'skid', 'Skids', PINK),
                role('g', 'grass', 'Grass', GREEN), role('c', 'cloud', 'Cloud', GREEN)], ['vehicles', 'sky']


def bicycle(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.12)
    box(cv, 0, gtop, w, h, 'g')
    for x in (w * 0.06, w * 0.94):
        oval(cv, x, gtop - 0.6, s * 0.11, s * 0.09, 'v')
    wy = gtop - s * 0.22
    for x in (0.25, 0.75):
        ring(cv, w * x, wy, s * 0.21, s * 0.13, 'w')
        disc(cv, w * x, wy, 0.8, 'h')
        if big:
            for a in (0, 60, 120):
                t = math.radians(a)
                seg(cv, w * x - math.cos(t) * s * 0.13, wy - math.sin(t) * s * 0.13,
                    w * x + math.cos(t) * s * 0.13, wy + math.sin(t) * s * 0.13, 'h', 0.35)
    jx, jy = w * 0.47, wy - s * 0.22
    for a, b in (((0.25, 0), (0.47, -0.22)), ((0.47, -0.22), (0.7, -0.24)), ((0.7, -0.24), (0.75, 0)),
                 ((0.47, -0.22), (0.5, 0)), ((0.25, 0), (0.5, 0))):
        seg(cv, w * a[0], wy + s * a[1], w * b[0], wy + s * b[1], 'f', 0.45)
    rbox(cv, jx - w * 0.07, jy - 1.4, jx + w * 0.05, jy - 0.4, 0.4, 'e')
    seg(cv, w * 0.7, wy - s * 0.24, w * 0.68, wy - s * 0.38, 'e', 0.4)
    seg(cv, w * 0.64, wy - s * 0.38, w * 0.74, wy - s * 0.38, 'e', 0.45)
    rbox(cv, w * 0.74, wy - s * 0.36, w * 0.9, wy - s * 0.22, 0.5, 'k')
    for x in (w * 0.78, w * 0.86):
        disc(cv, x, wy - s * 0.4, 0.9, 'p')
    disc(cv, w * 0.84, h * 0.12, s * 0.09, 'u')
    return cv, [sky(), role('w', 'tyre', 'Tyres', BROWN), role('h', 'hub', 'Hubs and spokes', BROWN), role('f', 'frame', 'Frame', PINK),
                role('e', 'seat', 'Seat and bars', BROWN), role('k', 'basket', 'Basket', BLUE), role('p', 'flowers', 'Flowers', PINK),
                role('u', 'sun', 'Sun', BROWN), role('g', 'path', 'Path', GREEN), role('v', 'bush', 'Bushes', GREEN)], ['vehicles', 'park']


# ---- Toys and play ----

def beach_ball(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    hills(cv, 'w', h * 0.64, 0.3, w * 0.5, r.uniform(0, 6))
    hills(cv, 'd', h * 0.8, 0.6, w * 1.2, r.uniform(0, 6))
    bx, by, br = cx - w * 0.08, h * 0.66, s * 0.27
    for y in range(h):
        for x in range(w):
            dx, dy = x + 0.5 - bx, y + 0.5 - by
            if dx * dx + dy * dy <= br * br:
                a = (math.degrees(math.atan2(dy, dx)) + 360) % 360
                cv.g[y][x] = 'abcabc'[int(a // 60)]
    disc(cv, bx, by, s * 0.06 + 0.3, 'u')
    px = w * 0.82
    seg(cv, px, h * 0.3, px, h * 0.84, 'k', 0.45)
    for y in range(h):
        for x in range(w):
            dx, dy = x + 0.5 - px, y + 0.5 - h * 0.3
            if dy <= 0 and dx * dx / (w * 0.18) ** 2 + dy * dy / (h * 0.12) ** 2 <= 1.0:
                cv.g[y][x] = 'a' if int((math.degrees(math.atan2(-dy, dx))) // 45) % 2 == 0 else 'u'
    disc(cv, w * 0.12, h * 0.1, s * 0.085, 'u')
    if big:
        star(cv, w * 0.5, h * 0.92, s * 0.06 + 0.6, 'a', ri=0.6)
    return cv, [sky(), role('a', 'red', 'Red panels', PINK), role('b', 'yellow', 'Yellow panels', BROWN), role('c', 'green', 'Green panels', GREEN),
                role('u', 'sun', 'Sun and stripes', PINK), role('k', 'pole', 'Pole', BROWN), role('w', 'sea', 'Sea', BLUE),
                role('d', 'sand', 'Sand', GREEN)], ['toys', 'beach']


def kite(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.16)
    hills(cv, 'g', gtop, 1.0, w * 1.3, r.uniform(0, 6))
    hills(cv, 'h', gtop + 2, 0.6, w * 0.8, r.uniform(0, 6), only='g')
    kx, ky = w * 0.58, h * 0.28
    poly(cv, [(kx, h * 0.06), (kx + w * 0.25, ky), (kx, h * 0.54), (kx - w * 0.25, ky)], 'k')
    poly(cv, [(kx, h * 0.06), (kx + w * 0.25, ky), (kx, ky)], 'q')
    poly(cv, [(kx, h * 0.54), (kx - w * 0.25, ky), (kx, ky)], 'q')
    pts = [(kx, h * 0.54), (kx - w * 0.06, h * 0.64), (kx - w * 0.16, h * 0.7), (w * 0.22, gtop - 1)]
    path(cv, pts, 't', 0.4)
    for k in range(3):
        x, y = kx - w * 0.04 - k * w * 0.06, h * (0.6 + k * 0.045)
        poly(cv, [(x - 0.9, y - 0.8), (x + 0.9, y + 0.8), (x - 0.9, y + 0.8), (x + 0.9, y - 0.8)], 'b')
    cloud(cv, w * 0.16, h * 0.18, s * 0.07 + 0.5, 'c')
    if big:
        poly(cv, [(w * 0.18, h * 0.32), (w * 0.26, h * 0.4), (w * 0.18, h * 0.48), (w * 0.1, h * 0.4)], 'b')
        path(cv, [(w * 0.18, h * 0.48), (w * 0.16, h * 0.6), (w * 0.2, gtop - 1)], 't', 0.4)
    return cv, [sky(), role('k', 'kite', 'Kite', PINK), role('q', 'panel', 'Panels', BROWN), role('t', 'string', 'String', BROWN),
                role('b', 'bows', 'Bows', PINK), role('c', 'cloud', 'Cloud', BLUE), role('g', 'hill', 'Hill', GREEN),
                role('h', 'far_hill', 'Meadow', GREEN)], ['toys', 'park']


def teddy_bear(w, h, r):
    cv, s, cx, big = start(w, h, 'p')
    fy = h * 0.86
    box(cv, 0, fy, w, h, 'f')
    oval(cv, cx, h * 0.66, w * 0.26, h * 0.19, 'b')
    for side in (-1, 1):
        oval(cv, cx + side * w * 0.26, h * 0.6, w * 0.09, h * 0.1, 'b')
        oval(cv, cx + side * w * 0.17, fy - 0.8, w * 0.1, h * 0.07, 'b')
        oval(cv, cx + side * w * 0.17, fy - 0.8, w * 0.05, h * 0.04, 'm')
        disc(cv, cx + side * w * 0.2, h * 0.17, s * 0.09, 'b')
        disc(cv, cx + side * w * 0.2, h * 0.17, s * 0.045 + 0.2, 'm')
    oval(cv, cx, h * 0.33, w * 0.22, h * 0.16, 'b')
    oval(cv, cx, h * 0.4, w * 0.1, h * 0.07, 'm')
    disc(cv, cx, h * 0.37, 0.8, 'n')
    for side in (-1, 1):
        disc(cv, cx + side * w * 0.09, h * 0.29, 0.75, 'n')
    oval(cv, cx, h * 0.7, w * 0.13, h * 0.11, 'm')
    poly(cv, [(cx, h * 0.5), (cx - w * 0.14, h * 0.46), (cx - w * 0.14, h * 0.55)], 'r')
    poly(cv, [(cx, h * 0.5), (cx + w * 0.14, h * 0.46), (cx + w * 0.14, h * 0.55)], 'r')
    for k, (x, c) in enumerate(((w * 0.1, 'k'), (w * 0.88, 'q'))):
        rbox(cv, x - s * 0.08, fy - s * 0.15, x + s * 0.08, fy + 0.4, 0.4, c)
    if big:
        rbox(cv, w * 0.88 - s * 0.07, fy - s * 0.29, w * 0.88 + s * 0.07, fy - s * 0.15, 0.4, 'k')
    dots(cv, 'u', 'f', 3, 2, area=(0, fy + 1, w - 1, h - 1))
    return cv, [('p', 'wall', 'Wall', BLUE, True), role('b', 'bear', 'Bear', BROWN), role('m', 'muzzle', 'Muzzle and paws', BROWN),
                role('n', 'nose', 'Nose and eyes', BROWN), role('r', 'bow', 'Bow', PINK), role('f', 'floor', 'Floor', GREEN),
                role('u', 'rug', 'Floor dots', GREEN), role('k', 'block', 'Blocks', PINK), role('q', 'block2', 'Blue block', BLUE)], ['toys', 'cozy']


def rubber_duck(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    dots(cv, 'x', 'b', 4, 4, area=(0, 1, w - 1, h * 0.5))
    ty = h * 0.62
    rbox(cv, -2, ty, w + 2, h + 3, 2.0, 't')
    hills(cv, 'w', ty + 1.2, 0.35, w * 0.4, r.uniform(0, 6), only='t')
    for k in range(5 if not big else 7):
        disc(cv, w * (0.08 + k * 0.84 / (4 if not big else 6)), ty + 1.0, 0.8 + (k % 2) * 0.4, 'o')
    dx = cx - w * 0.04
    oval(cv, dx, ty + 0.2, w * 0.3, h * 0.11, 'y')
    poly(cv, [(dx - w * 0.3, ty - 0.4), (dx - w * 0.38, ty - h * 0.08), (dx - w * 0.22, ty - h * 0.03)], 'y')
    disc(cv, dx + w * 0.16, ty - h * 0.15, s * 0.15, 'y')
    poly(cv, [(dx + w * 0.26, ty - h * 0.17), (dx + w * 0.42, ty - h * 0.13), (dx + w * 0.26, ty - h * 0.1)], 'k')
    disc(cv, dx + w * 0.19, ty - h * 0.19, 0.8, 'e')
    oval(cv, dx - w * 0.04, ty - 0.2, w * 0.14, h * 0.05, 'g')
    return cv, [('b', 'tiles', 'Bath tiles', BLUE, True), role('x', 'grout', 'Tile dots', GREEN), role('t', 'tub', 'Tub', PINK),
                role('w', 'water', 'Water', BLUE), role('o', 'foam', 'Foam', GREEN), role('y', 'duck', 'Duck', BROWN),
                role('k', 'beak', 'Beak', BROWN), role('e', 'eye', 'Eye', PINK), role('g', 'wing', 'Wing', BROWN)], ['toys', 'bath']


def sandcastle(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    hills(cv, 'w', h * 0.66, 0.3, w * 0.5, r.uniform(0, 6))
    hills(cv, 'd', h * 0.8, 0.4, w * 1.4, r.uniform(0, 6))
    x0, x1 = w * 0.2, w * 0.74
    box(cv, x0, h * 0.54, x1, h * 0.84, 'c')
    for x in range(int(x0), int(x1) + 1, 2):
        box(cv, x, h * 0.54 - 1.0, x + 0.9, h * 0.54, 'c')
    tx0, tx1 = cx - w * 0.17, cx + w * 0.07
    box(cv, tx0, h * 0.32, tx1, h * 0.54, 'c')
    for x in range(int(tx0), int(tx1) + 1, 2):
        box(cv, x, h * 0.32 - 1.0, x + 0.9, h * 0.32, 'c')
    rbox(cv, cx - w * 0.1, h * 0.66, cx, h * 0.84, w * 0.05, 'g')
    box(cv, cx - w * 0.08, h * 0.4, cx - w * 0.02, h * 0.46, 'g')
    seg(cv, cx - w * 0.05, h * 0.31, cx - w * 0.05, h * 0.14, 'p', 0.4)
    poly(cv, [(cx - w * 0.04, h * 0.14), (cx + w * 0.12, h * 0.18), (cx - w * 0.04, h * 0.22)], 'f')
    bx = w * 0.86
    poly(cv, [(bx - s * 0.1, h * 0.7), (bx + s * 0.1, h * 0.7), (bx + s * 0.07, h * 0.84), (bx - s * 0.07, h * 0.84)], 'k')
    seg(cv, bx - s * 0.1, h * 0.7, bx, h * 0.62, 'p', 0.4)
    if big:
        star(cv, w * 0.12, h * 0.9, s * 0.06 + 0.6, 'f', ri=0.6)
        disc(cv, w * 0.6, h * 0.93, 0.9, 'k')
    disc(cv, w * 0.86, h * 0.1, s * 0.085, 'u')
    return cv, [sky(), role('c', 'castle', 'Sand castle', BROWN), role('g', 'gate', 'Gate and window', BROWN), role('p', 'pole', 'Stick and handle', PINK),
                role('f', 'flag', 'Flag and starfish', PINK), role('k', 'bucket', 'Bucket', BLUE), role('w', 'sea', 'Sea', BLUE),
                role('d', 'beach', 'Beach', GREEN), role('u', 'sun', 'Sun', BROWN)], ['toys', 'beach']


def robot(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    fy = h * 0.92
    box(cv, 0, fy, w, h, 'f')
    hx0, hx1 = w * 0.3, w * 0.7
    rbox(cv, hx0, h * 0.14, hx1, h * 0.38, 1.0, 'm')
    for side in (-1, 1):
        disc(cv, cx + side * w * 0.09, h * 0.24, s * 0.06 + 0.3, 'e')
        box(cv, cx + side * w * 0.2 - 0.5, h * 0.2, cx + side * w * 0.2 + 0.5 + (0 if side > 0 else 0), h * 0.3, 'a')
    box(cv, cx - w * 0.08, h * 0.32, cx + w * 0.08, h * 0.32 + 0.8, 'k')
    seg(cv, cx, h * 0.14, cx, h * 0.07, 'k', 0.4)
    disc(cv, cx, h * 0.06, 0.9, 'p')
    box(cv, cx - 0.9, h * 0.38, cx + 0.9, h * 0.42, 'a')
    rbox(cv, w * 0.24, h * 0.42, w * 0.76, h * 0.74, 0.8, 'm')
    rbox(cv, w * 0.36, h * 0.48, w * 0.64, h * 0.64, 0.5, 'p')
    heart(cv, cx, h * 0.555, s * 0.06 + 0.3, 'e')
    rbox(cv, w * 0.1, h * 0.44, w * 0.2, h * 0.66, 0.6, 'a')
    rbox(cv, w * 0.8, h * 0.44, w * 0.9, h * 0.66, 0.6, 'a')
    rbox(cv, w * 0.31, h * 0.74, w * 0.43, fy + 0.3, 0.4, 'a')
    rbox(cv, w * 0.57, h * 0.74, w * 0.69, fy + 0.3, 0.4, 'a')
    if big:
        for x in (w * 0.4, w * 0.5, w * 0.6):
            cv.put(int(x), int(h * 0.69), 'k')
    return cv, [('b', 'wall', 'Wall', PINK, True), role('m', 'metal', 'Body', BLUE), role('e', 'eye', 'Eyes and heart', BROWN),
                role('k', 'mouth', 'Mouth and aerial', BROWN), role('p', 'panel', 'Panel and light', GREEN), role('a', 'limb', 'Arms and legs', BLUE),
                role('f', 'floor', 'Floor', BROWN)], ['toys']


# ---- Things ----

def crown(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    fy = h * 0.88
    box(cv, 0, fy, w, h, 'f')
    rbox(cv, w * 0.1, h * 0.68, w * 0.9, fy + 0.5, 2.0, 'p')
    for x in (w * 0.1, w * 0.9):
        disc(cv, x, fy - 0.2, s * 0.06 + 0.3, 't')
    x0, x1, yb, yt = w * 0.18, w * 0.82, h * 0.68, h * 0.24
    n = 3 if not big else 5
    pts = [(x0, yb)]
    for k in range(n):
        xa = x0 + k * (x1 - x0) / n
        xb = xa + (x1 - x0) / n
        top = yt if k % 2 == 0 else yt + h * 0.06
        pts += [(xa, yb - h * 0.16), ((xa + xb) / 2, top), (xb, yb - h * 0.16)]
    pts.append((x1, yb))
    poly(cv, pts, 'c')
    for k in range(n):
        xa = x0 + k * (x1 - x0) / n
        xb = xa + (x1 - x0) / n
        disc(cv, (xa + xb) / 2, (yt if k % 2 == 0 else yt + h * 0.06) - 0.4, 1.0, 'j')
    box(cv, x0, yb - h * 0.1, x1, yb, 'k')
    for k, x in enumerate((0.3, 0.5, 0.7)):
        disc(cv, w * x, yb - h * 0.05, s * 0.04 + 0.35, 'e' if k == 1 else 'j')
    for x, y in ((0.1, 0.14), (0.88, 0.2), (0.08, 0.44), (0.92, 0.5), (0.3, 0.08), (0.72, 0.1)):
        cv.put(int(w * x), int(h * y), 'x')
    return cv, [('b', 'wall', 'Wall', BLUE, True), role('c', 'crown', 'Crown', BROWN), role('k', 'band', 'Band', BROWN),
                role('j', 'ruby', 'Rubies and balls', PINK), role('e', 'sapphire', 'Sapphire', BLUE), role('p', 'cushion', 'Cushion', PINK),
                role('t', 'tassel', 'Tassels', BROWN), role('f', 'floor', 'Floor', GREEN), role('x', 'sparkle', 'Sparkles', GREEN)], ['things', 'fairy']


def treasure_chest(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    gtop = h - ground_rows(h, 0.12)
    for x in (w * 0.07, w * 0.93) + ((w * 0.82,) if big else ()):
        path(cv, [(x, gtop), (x + 0.8, h * 0.84), (x - 0.4, h * 0.72), (x + 0.5, h * 0.6)], 's', 0.55)
    hills(cv, 'd', gtop, 0.5, w * 0.9, r.uniform(0, 6))
    x0, x1 = w * 0.17, w * 0.83
    seam = h * 0.5
    box(cv, x0, seam, x1, gtop + 0.6, 'w')
    for y in range(h):
        for x in range(w):
            dx, dy = (x + 0.5 - cx) / ((x1 - x0) / 2), (y + 0.5 - seam) / (h * 0.17)
            if dy <= 0 and dx * dx + dy * dy <= 1.0:
                cv.g[y][x] = 'w'
    box(cv, x0, seam - 0.5, x1, seam + 0.5, 'k')
    for x in (x0 + w * 0.1, x1 - w * 0.1):
        box(cv, x - 0.6, seam - h * 0.17, x + 0.6, gtop + 0.6, 'k', only='w')
    rbox(cv, cx - w * 0.06, seam - h * 0.02, cx + w * 0.06, seam + h * 0.1, 0.5, 'l')
    for k in range(3 if not big else 5):
        x = w * 0.08 + k * 1.6
        disc(cv, x, gtop + 0.4, 0.9, 'k' if k % 2 == 0 else 'j')
    disc(cv, w * 0.9, gtop + 0.4, 0.9, 'j')
    scatter(cv, 'o', 'b', 4, r, sep=3, area=(1, 1, w - 2, h * 0.26))
    return cv, [('b', 'water', 'Water', BLUE, True), role('w', 'wood', 'Wood', BROWN), role('k', 'gold', 'Bands and gold', BROWN),
                role('l', 'lock', 'Lock', BLUE), role('j', 'gems', 'Gems', PINK), role('o', 'bubbles', 'Bubbles', PINK),
                role('s', 'weed', 'Weed', GREEN), role('d', 'sand', 'Sand', GREEN)], ['things', 'sea']


def guitar(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    fy = h * 0.92
    box(cv, 0, fy, w, h, 'f')
    gx = cx - w * 0.04
    disc(cv, gx, h * 0.72, s * 0.27, 'g')
    disc(cv, gx, h * 0.5, s * 0.2, 'g')
    box(cv, gx - 1.0, h * 0.08, gx + 1.0, h * 0.42, 'n')
    rbox(cv, gx - 1.6, h * 0.02, gx + 1.6, h * 0.12, 0.6, 'n')
    for y in (h * 0.05, h * 0.09):
        cv.put(int(gx - 2), int(y), 'k')
        cv.put(int(gx + 2), int(y), 'k')
    disc(cv, gx, h * 0.52, s * 0.07 + 0.3, 'k')
    box(cv, gx - s * 0.1, h * 0.79, gx + s * 0.1, h * 0.79 + 0.9, 'k')
    seg(cv, gx, h * 0.08, gx, h * 0.79, 's', 0.35)
    if big:
        oval(cv, gx + s * 0.16, h * 0.64, s * 0.05, s * 0.08, 'p')
    for x, y in ((0.8, 0.2), (0.86, 0.42), (0.16, 0.26)):
        disc(cv, w * x, h * y, 0.9, 'm')
        seg(cv, w * x + 0.6, h * y, w * x + 0.6, h * y - h * 0.08, 'm', 0.35)
    return cv, [('b', 'wall', 'Wall', PINK, True), role('g', 'body', 'Body', BROWN), role('n', 'neck', 'Neck', BROWN),
                role('k', 'hole', 'Sound hole and bridge', BLUE), role('s', 'string', 'Strings', BLUE), role('p', 'guard', 'Pick guard', PINK),
                role('m', 'notes', 'Music notes', GREEN), role('f', 'floor', 'Floor', GREEN)], ['things', 'music']


def drum(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    fy = h * 0.88
    box(cv, 0, fy, w, h, 'f')
    dx0, dx1 = w * 0.16, w * 0.84
    top, bot = h * 0.42, fy - 0.2
    box(cv, dx0, top, dx1, bot, 'd')
    oval(cv, cx, bot, (dx1 - dx0) / 2, h * 0.06, 'd')
    n = 4 if not big else 6
    for k in range(n):
        xa = dx0 + k * (dx1 - dx0) / n
        xb = xa + (dx1 - dx0) / n
        path(cv, [(xa, top + h * 0.06), ((xa + xb) / 2, bot - h * 0.02), (xb, top + h * 0.06)], 'z', 0.45)
    box(cv, dx0, bot - 0.9, dx1, bot, 'k')
    oval(cv, cx, top, (dx1 - dx0) / 2 + 0.4, h * 0.075, 'k')
    oval(cv, cx, top, (dx1 - dx0) / 2 - 0.6, h * 0.05, 'e')
    seg(cv, w * 0.26, h * 0.12, w * 0.54, h * 0.3, 't', 0.55)
    seg(cv, w * 0.74, h * 0.12, w * 0.46, h * 0.3, 't', 0.55)
    disc(cv, w * 0.26, h * 0.12, 0.9, 't')
    disc(cv, w * 0.74, h * 0.12, 0.9, 't')
    for x, y in ((0.08, 0.3), (0.92, 0.26)):
        disc(cv, w * x, h * y, 0.9, 'm')
        seg(cv, w * x + 0.6, h * y, w * x + 0.6, h * y - h * 0.08, 'm', 0.35)
    return cv, [('b', 'wall', 'Wall', BLUE, True), role('d', 'drum', 'Drum', PINK), role('z', 'cord', 'Cords', PINK),
                role('k', 'rim', 'Rims', BROWN), role('e', 'head', 'Drum head', BLUE), role('t', 'stick', 'Sticks', BROWN),
                role('m', 'notes', 'Music notes', GREEN), role('f', 'floor', 'Floor', GREEN)], ['things', 'music']


def snowman(w, h, r):
    cv, s, cx, big = start(w, h, 'n')
    gtop = h - ground_rows(h, 0.16)
    hills(cv, 'g', gtop, 0.6, w * 1.4, r.uniform(0, 6))
    for x in (w * 0.06, w * 0.94) + ((w * 0.16,) if big else ()):
        for k in range(3):
            y = gtop - h * (0.04 + k * 0.07)
            poly(cv, [(x - s * (0.1 - k * 0.02), y + h * 0.04), (x, y - h * 0.08), (x + s * (0.1 - k * 0.02), y + h * 0.04)], 'p')
    u = min(w * 1.3, (gtop - 1.0) / 1.0)
    if big:
        rb, rm, rh = 0.2 * u, 0.15 * u, 0.12 * u
        yb = gtop - rb * 0.8
        ym = yb - rb - rm * 0.8
        hy = ym - rm - rh * 0.8
        disc(cv, cx, yb, rb, 'b')
    else:
        rm, rh = 0.29 * u, 0.19 * u
        ym = gtop - rm * 0.8
        hy = ym - rm - rh * 0.75
    disc(cv, cx, ym, rm, 'b')
    disc(cv, cx, hy, rh, 'b')
    box(cv, cx - rh * 1.15, hy - rh * 0.95, cx + rh * 1.15, hy - rh * 0.95 + 1.0, 'k')
    box(cv, cx - rh * 0.7, hy - rh * 0.95 - u * 0.11, cx + rh * 0.7, hy - rh * 0.95, 'k')
    for side in (-1, 1):
        cv.put(int(cx + side * rh * 0.45), int(hy - rh * 0.25), 'k')
    poly(cv, [(cx, hy - 0.4), (cx + rh * 1.6, hy + 0.3), (cx, hy + 1.1)], 'c')
    box(cv, cx - rh * 1.1, hy + rh * 0.8, cx + rh * 1.1, hy + rh * 0.8 + 1.0, 'f')
    box(cv, cx + rh * 0.3, hy + rh * 0.8, cx + rh * 0.9, hy + rh * 0.8 + 2.4, 'f')
    for k in range(2 if not big else 3):
        cv.put(int(cx), int(ym - rm * 0.1 + k * 1.6), 'k')
    for side in (-1, 1):
        seg(cv, cx + side * rm * 0.9, ym, cx + side * min(rm * 1.55, w * 0.3), ym - rm * 0.7, 'a', 0.45)
    scatter(cv, 'x', 'n', 5 + (w * h) // 80, r, sep=3, area=(0, 0, w - 1, gtop - 2))
    scatter(cv, 'x', 'g', 3 + (w * h) // 120, r, sep=3, area=(0, gtop + 1, w - 1, h - 2))
    return cv, [('n', 'sky', 'Evening sky', PINK, True), role('b', 'snowman', 'Snowman', BLUE), role('k', 'hat', 'Hat, eyes and buttons', BROWN),
                role('c', 'nose', 'Carrot nose', BROWN), role('f', 'scarf', 'Scarf', PINK), role('a', 'arm', 'Twig arms', BROWN),
                role('x', 'snow', 'Snowflakes', BLUE), role('g', 'hill', 'Hill', GREEN), role('p', 'pine', 'Pines', GREEN)], ['things', 'winter']


def rainbow(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.14)
    bands = 'abcde' if not big else 'abcdeq'
    ro = w * 0.46
    bw = max(1.0, ro * 0.13) if not big else ro * 0.11
    acy = gtop + 0.5
    for k, c in enumerate(bands):
        for y in range(h):
            for x in range(w):
                d = math.hypot(x + 0.5 - cx, y + 0.5 - acy)
                if ro - (k + 1) * bw < d <= ro - k * bw and y + 0.5 < acy:
                    cv.g[y][x] = c
    hills(cv, 'g', gtop, 0.6, w * 1.1, r.uniform(0, 6))
    cloud(cv, w * 0.16, h * 0.12, s * 0.07 + 0.4, 'k')
    if big:
        cloud(cv, w * 0.62, h * 0.08, s * 0.05 + 0.4, 'k')
    disc(cv, w * 0.88, h * 0.1, s * 0.085, 'u')
    if big:
        for x in (w * 0.3, w * 0.5, w * 0.7):
            disc(cv, x, gtop + 1.5, 0.9, 'a')
    return cv, [sky(), role('a', 'red', 'Red band and flowers', PINK), role('b', 'orange', 'Orange band', BROWN), role('c', 'green', 'Green band', GREEN),
                role('d', 'blue', 'Blue band', BLUE), role('e', 'violet', 'Violet band', PINK), role('q', 'yellow', 'Yellow band', BROWN),
                role('k', 'cloud', 'Clouds', BLUE), role('g', 'hills', 'Hills', GREEN), role('u', 'sun', 'Sun', BROWN)], ['things', 'sky']


def moon_and_stars(w, h, r):
    cv, s, cx, big = start(w, h, 'n')
    gtop = h - ground_rows(h, 0.16)
    hills(cv, 'h', gtop - 1.5, 1.0, w * 0.9, r.uniform(0, 6))
    hills(cv, 'g', gtop, 0.6, w * 1.3, r.uniform(0, 6))
    mx, my, mr = w * 0.44, h * 0.32, s * 0.27
    disc(cv, mx, my, mr, 'm')
    disc(cv, mx + mr * 0.55, my - mr * 0.2, mr * 0.78, 'n')
    if big:
        disc(cv, mx - mr * 0.5, my - mr * 0.1, 0.75, 'k')
        path(cv, [(mx - mr * 0.62, my + mr * 0.3), (mx - mr * 0.42, my + mr * 0.42)], 'k', 0.4)
    for x, y, rr in ((0.82, 0.16, 0.1), (0.84, 0.5, 0.08), (0.16, 0.14, 0.07)):
        star(cv, w * x, h * y, s * rr + 0.6, 'y')
    for x, y in ((0.42, 0.07), (0.62, 0.3), (0.1, 0.36), (0.94, 0.08), (0.64, 0.58), (0.22, 0.6)):
        cv.put(int(w * x), int(h * y), 'x')
    hx = w * 0.76
    box(cv, hx - s * 0.1, gtop - s * 0.16, hx + s * 0.1, gtop + 0.5, 'o')
    poly(cv, [(hx - s * 0.14, gtop - s * 0.15), (hx, gtop - s * 0.3), (hx + s * 0.14, gtop - s * 0.15)], 'k')
    box(cv, hx - 0.9, gtop - s * 0.11, hx + 0.1, gtop - s * 0.11 + 1.0, 'y')
    if big:
        cloud(cv, w * 0.2, h * 0.62, s * 0.05 + 0.4, 'c')
    return cv, [('n', 'night', 'Night sky', PINK, True), role('m', 'moon', 'Moon', BROWN), role('y', 'star', 'Stars and window', BROWN),
                role('x', 'specks', 'Tiny stars', BLUE), role('c', 'cloud', 'Cloud', BLUE), role('k', 'roof', 'Roof and smile', PINK),
                role('o', 'house', 'House', BLUE), role('g', 'hills', 'Hills', GREEN), role('h', 'far_hills', 'Far hills', GREEN)], ['things', 'night']


def gift_box(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    fy = h * 0.88
    box(cv, 0, fy, w, h, 'f')
    x0, x1 = w * 0.1, w * 0.64
    top = h * 0.42
    box(cv, x0, top, x1, fy + 0.5, 'x')
    box(cv, x0 - 0.6, top - h * 0.07, x1 + 0.6, top, 'x')
    dots(cv, 'd', 'x', 3, 3, area=(x0, top - h * 0.07, x1, fy))
    gx = (x0 + x1) / 2
    box(cv, gx - 0.9, top - h * 0.07, gx + 0.9, fy + 0.5, 'o')
    box(cv, x0, (top + fy) / 2 - 0.5, x1, (top + fy) / 2 + 0.5, 'o')
    for side in (-1, 1):
        oval(cv, gx + side * w * 0.09, top - h * 0.12, w * 0.09, h * 0.05, 'o')
    disc(cv, gx, top - h * 0.1, 0.9, 'o')
    sx0, sx1 = w * 0.68, w * 0.94
    stop = h * 0.64
    box(cv, sx0, stop, sx1, fy + 0.5, 'y')
    box(cv, (sx0 + sx1) / 2 - 0.5, stop, (sx0 + sx1) / 2 + 0.5, fy + 0.5, 'k')
    for side in (-1, 1):
        disc(cv, (sx0 + sx1) / 2 + side * 1.0, stop - 0.8, 0.9, 'k')
    seg(cv, x1, top + 1, x1 + w * 0.06, top + h * 0.1, 't', 0.4)
    rbox(cv, x1 + w * 0.02, top + h * 0.1, x1 + w * 0.1, top + h * 0.17, 0.3, 't')
    scatter(cv, 'c', 'b', 6 + (w * h) // 60, r, sep=3, area=(0, 0, w - 1, top - h * 0.14))
    return cv, [('b', 'wall', 'Wall', GREEN, True), role('x', 'box', 'Box', PINK), role('d', 'dots', 'Dots', PINK),
                role('o', 'ribbon', 'Ribbon and bow', BLUE), role('y', 'small_box', 'Small box', BLUE), role('k', 'small_ribbon', 'Small ribbon', GREEN),
                role('t', 'tag', 'Tag', BROWN), role('f', 'floor', 'Floor', BROWN), role('c', 'confetti', 'Confetti', BROWN)], ['things', 'party']


def balloons(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.1)
    box(cv, 0, gtop, w, h, 'g')
    kx, ky = cx, gtop - h * 0.12
    rbox(cv, kx - s * 0.12, ky, kx + s * 0.12, gtop + 0.5, 0.5, 'k')
    spots = [(-0.22, 0.26, 'a'), (0.2, 0.22, 'b'), (0.0, 0.14, 'c'), (-0.04, 0.4, 'd')]
    if big:
        spots += [(0.3, 0.42, 'e'), (-0.32, 0.46, 'b')]
    br = s * 0.13
    for dx, y, c in spots:
        bx, by = cx + dx * w, h * y
        path(cv, [(bx, by + br * 1.15), (bx + dx * 2, (by + ky) / 2), (kx, ky)], 't', 0.4)
    for dx, y, c in spots:
        bx, by = cx + dx * w, h * y
        oval(cv, bx, by, br, br * 1.2, c)
        poly(cv, [(bx - 0.7, by + br * 1.15 + 0.6), (bx + 0.7, by + br * 1.15 + 0.6), (bx, by + br * 1.0)], c)
    cloud(cv, w * 0.84, h * 0.08, s * 0.06 + 0.4, 'b' if not big else 'e')
    return cv, [sky(), role('a', 'pink', 'Pink balloon', PINK), role('b', 'blue', 'Blue balloons', BLUE), role('c', 'green', 'Green balloon', GREEN),
                role('d', 'orange', 'Orange balloon', BROWN), role('e', 'violet', 'Violet balloon', PINK), role('t', 'string', 'Strings', BROWN),
                role('k', 'weight', 'Weight', GREEN), role('g', 'grass', 'Grass', GREEN)], ['things', 'party']


def alarm_clock(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    fy = h * 0.86
    box(cv, 0, fy, w, h, 't')
    cy, cr = h * 0.52, s * 0.32
    for side in (-1, 1):
        seg(cv, cx + side * cr * 0.5, cy + cr * 0.8, cx + side * cr * 0.8, fy + 0.4, 'k', 0.6)
        disc(cv, cx + side * cr * 0.75, cy - cr * 0.9, s * 0.12, 'e')
    disc(cv, cx, cy, cr, 'k')
    disc(cv, cx, cy, cr - (1.0 if not big else 1.6), 'f')
    for k in range(12 if big else 4):
        a = math.radians(k * (30 if big else 90))
        cv.put(int(cx + math.cos(a) * (cr - 2.4)), int(cy + math.sin(a) * (cr - 2.4)), 'm')
    seg(cv, cx, cy, cx, cy - cr * 0.6, 'h', 0.45)
    seg(cv, cx, cy, cx + cr * 0.45, cy + cr * 0.1, 'h', 0.45)
    disc(cv, cx, cy, 0.8, 'h')
    for side in (-1, 1):
        for k in range(2):
            x = cx + side * (cr * 0.75 + s * 0.16 + k * 1.3)
            seg(cv, x, cy - cr * 0.95 - k * 0.6, x + side * 0.8, cy - cr * 1.25 - k * 0.6, 'z', 0.4)
    return cv, [('b', 'wall', 'Wall', GREEN, True), role('k', 'frame', 'Frame and legs', PINK), role('f', 'face', 'Clock face', BLUE),
                role('h', 'hands', 'Hands', BROWN), role('m', 'marks', 'Hour marks', PINK), role('e', 'bell', 'Bells', BROWN),
                role('z', 'ring', 'Ringing', BLUE), role('t', 'table', 'Table', GREEN)], ['things', 'cozy']


def igloo(w, h, r):
    cv, s, cx, big = start(w, h, 'n')
    gtop = h - ground_rows(h, 0.14)
    for k, c in enumerate('ab'):
        pts = []
        for i in range(13):
            x = w * 0.08 + i * (w * 0.84) / 12
            pts.append((x, h * (0.1 + k * 0.07) + math.sin(i * 0.9 + k) * h * 0.03))
        path(cv, pts, c, 0.6 if not big else 0.9)
    hills(cv, 'g', gtop, 0.4, w * 1.4, r.uniform(0, 6))
    iy, ir = gtop + 0.5, w * 0.4
    for y in range(h):
        for x in range(w):
            if y + 0.5 <= iy and math.hypot((x + 0.5 - cx) / ir, (y + 0.5 - iy) / (ir * 0.9)) <= 1.0:
                cv.g[y][x] = 'i'
    rows = 3 if not big else 4
    for k in range(1, rows):
        yy = iy - k * ir * 0.9 / rows
        box(cv, 0, yy - 0.4, w, yy + 0.4, 'l', only='i')
    rbox(cv, cx - w * 0.1, iy - h * 0.17, cx + w * 0.1, iy, w * 0.1, 'd')
    for x, y in ((0.06, 0.04), (0.36, 0.26), (0.7, 0.28), (0.94, 0.06), (0.14, 0.36), (0.88, 0.38)):
        cv.put(int(w * x), int(h * y), 'x')
    return cv, [('n', 'night', 'Polar night', PINK, True), role('a', 'aurora', 'Aurora', GREEN), role('b', 'aurora2', 'Aurora glow', GREEN),
                role('i', 'igloo', 'Igloo', BLUE), role('l', 'block', 'Block lines', PINK), role('d', 'door', 'Door', BROWN),
                role('x', 'stars', 'Stars', BROWN), role('g', 'snow', 'Snow', BLUE)], ['things', 'winter']


def palm_island(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    sea = h * 0.62
    hills(cv, 'w', sea, 0.3, w * 0.5, r.uniform(0, 6))
    oval(cv, cx, h * 0.92, w * 0.42, h * 0.2, 'd')
    tx = cx - w * 0.08
    pts = [(tx, h * 0.8), (tx + w * 0.04, h * 0.6), (tx + w * 0.12, h * 0.42), (tx + w * 0.2, h * 0.3)]
    path(cv, pts, 't', 0.7 if not big else 0.9)
    top = pts[-1]
    for a in (-160, -120, -60, -20, 20, 160):
        t = math.radians(a)
        lens(cv, top[0], top[1], top[0] + math.cos(t) * s * 0.34, top[1] + math.sin(t) * s * 0.2 + s * 0.08, s * 0.1, 'l')
    for dx in (-0.8, 0.8):
        disc(cv, top[0] + dx, top[1] + 1.2, 0.8, 'c')
    oval(cv, w * 0.8, h * 0.8, s * 0.1, s * 0.07, 'b')
    disc(cv, w * 0.84, h * 0.14, s * 0.1, 'u')
    if big:
        star(cv, w * 0.3, h * 0.88, s * 0.05 + 0.6, 'x', ri=0.6)
        poly(cv, [(w * 0.08, sea - 1.5), (w * 0.2, sea - 1.5), (w * 0.17, sea + 0.2), (w * 0.11, sea + 0.2)], 'c')
        poly(cv, [(w * 0.14, sea - 1.6), (w * 0.14, sea - h * 0.12), (w * 0.2, sea - 1.8)], 'x')
    return cv, [sky(), role('w', 'sea', 'Sea', BLUE), role('d', 'sand', 'Sand', BROWN), role('t', 'trunk', 'Trunk', BROWN),
                role('l', 'leaves', 'Palm leaves', GREEN), role('c', 'coconut', 'Coconuts and boat', BROWN), role('b', 'bush', 'Bush', GREEN),
                role('u', 'sun', 'Sun', PINK), role('x', 'starfish', 'Starfish and sail', PINK)], ['things', 'beach']


def fir_tree(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    fy = h * 0.88
    box(cv, 0, fy, w, h, 'f')
    box(cv, cx - max(1.0, w * 0.06), fy - h * 0.1, cx + max(1.0, w * 0.06), fy + 0.5, 't')
    tiers = 3 if not big else 4
    for k in range(tiers):
        yb = fy - h * 0.08 - k * (h * 0.66 / tiers) * 0.85
        half = w * (0.4 - k * 0.08)
        poly(cv, [(cx - half, yb), (cx, yb - h * 0.66 / tiers * 1.25), (cx + half, yb)], 'g')
        if k < tiers - 1:
            seg(cv, cx - half * 0.7, yb - h * 0.03, cx + half * 0.6, yb - h * 0.1, 'k', 0.45)
    for k in range(6 if not big else 10):
        sp = scatter(cv, 'o' if k % 2 == 0 else 'e', 'g', 1, r, sep=2, area=(cx - w * 0.34, h * 0.24, cx + w * 0.34, fy - h * 0.1))
        if big:
            for x, y in sp:
                if cv.g[y][x + 1 if x + 1 < w else x] == 'g':
                    cv.put(x + 1, y, 'o' if k % 2 == 0 else 'e')
    star(cv, cx, h * 0.1, s * 0.1 + 0.5, 'y')
    for x0, c, k in ((w * 0.08, 'p', 'k'), (w * 0.72, 'q', 'o')):
        box(cv, x0, fy - s * 0.18, x0 + s * 0.2, fy + 0.5, c)
        box(cv, x0 + s * 0.1 - 0.5, fy - s * 0.18, x0 + s * 0.1 + 0.5, fy + 0.5, k)
    return cv, [('b', 'wall', 'Wall', BLUE, True), role('g', 'tree', 'Tree', GREEN), role('k', 'garland', 'Garland', BROWN),
                role('o', 'bauble', 'Red baubles', PINK), role('e', 'bauble2', 'Blue baubles', BLUE), role('y', 'star', 'Star', BROWN),
                role('t', 'trunk', 'Trunk', BROWN), role('p', 'present', 'Present', PINK), role('q', 'present2', 'Green present', GREEN),
                role('f', 'floor', 'Floor', BROWN)], ['things', 'winter']


def ferris_wheel(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.1)
    box(cv, 0, gtop, w, h, 'g')
    cx = int(w / 2) + 0.5
    hy, R = int(h * 0.42) + 0.5, min(w * 0.36, h * 0.31)
    for side in (-1, 1):
        seg(cv, cx, hy, cx + side * R * 0.7, gtop + 0.5, 'k', 0.6)
    angles = (0, 90, 180, 270) if not big else (0, 45, 90, 135, 180, 225, 270, 315)
    for d in angles:
        a = math.radians(d)
        seg(cv, cx, hy, cx + math.cos(a) * R, hy + math.sin(a) * R, 'k', 0.5)
    ring(cv, cx, hy, R + 0.7, R - 0.7, 'r')
    disc(cv, cx, hy, 1.2, 'k')
    for k, d in enumerate(angles):
        a = math.radians(d + 180 / len(angles))
        x, y = cx + math.cos(a) * (R + 1.0), hy + math.sin(a) * (R + 1.0)
        rbox(cv, x - 0.95, y - 0.6, x + 0.95, y + 1.3, 0.4, 'abc'[k % 3])
    if big:
        bx = w * 0.1
        box(cv, bx - 1.5, gtop - 3.5, bx + 1.5, gtop + 0.5, 'o')
        poly(cv, [(bx - 2.2, gtop - 3.4), (bx, gtop - 5.4), (bx + 2.2, gtop - 3.4)], 'r')
    cloud(cv, w * 0.84, h * 0.1, s * 0.05 + 0.4, 'm')
    return cv, [sky(), role('r', 'rim', 'Rim and roof', PINK), role('k', 'frame', 'Spokes and stand', BROWN),
                role('a', 'cabin', 'Orange cabins', BROWN), role('b', 'cabin2', 'Blue cabins', BLUE), role('c', 'cabin3', 'Green cabins', GREEN),
                role('o', 'booth', 'Ticket booth', PINK), role('m', 'cloud', 'Cloud', BLUE), role('g', 'grass', 'Grass', GREEN)], ['things', 'park']


WORLD_SUBJECTS = [house, castle, lighthouse, windmill, barn, tent, tree_house, ship, rowboat, car, bus, train, tractor,
                  airplane, hot_air_balloon, rocket, submarine, helicopter, bicycle, beach_ball, kite, teddy_bear,
                  rubber_duck, sandcastle, robot, crown, treasure_chest, guitar, drum, snowman, rainbow, moon_and_stars,
                  gift_box, balloons, alarm_clock, igloo, palm_island, fir_tree, ferris_wheel]

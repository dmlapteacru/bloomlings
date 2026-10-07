"""More subjects for the levels: things (more_subjects.py). Vehicles and machines, sport and play, the desk, music and
small things of the house, drawn with picture_kit's helpers in the style of world_subjects.py. Each subject returns
(canvas, roles, themes); shapes that must stay apart (a vehicle against the sky, wheels against the body) take
different color groups, and the roles spread over three or four groups, so every picture can carry six distinct
variants. Vehicles keep a free column at both sides, so the sky meets the ground and their details stay within the
nesting depth targets. A subject's pictures differ in scene, props and the way they face, all chosen with the
picture's own random stream, and they add detail on the big boards (from 300 cells).
"""
import math

from picture_kit import (BLUE, BROWN, GREEN, PINK, box, cells, cloud, disc, dots, ground_rows, heart, hills, lens,
                         oval, path, poly, rbox, ring, role, seg, sky, star, start)


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


def bar(cv, x0, y0, x1, y1, t, c):
    """A straight bar `t` cells across from (x0, y0) to (x1, y1), with square ends."""
    dx, dy = x1 - x0, y1 - y0
    L = math.hypot(dx, dy) or 1.0
    nx, ny = -dy / L * t / 2, dx / L * t / 2
    poly(cv, [(x0 + nx, y0 + ny), (x1 + nx, y1 + ny), (x1 - nx, y1 - ny), (x0 - nx, y0 - ny)], c)


def tube(cv, pts, r0, r1, c):
    """A round tube along the polyline `pts`, its radius going from r0 at the start to r1 at the end."""
    lengths = [math.hypot(b[0] - a[0], b[1] - a[1]) for a, b in zip(pts, pts[1:])]
    total = sum(lengths) or 1.0
    run = 0.0
    for (a, b), L in zip(zip(pts, pts[1:]), lengths):
        n = max(1, int(L / 0.3))
        for k in range(n + 1):
            t = k / n
            disc(cv, a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t, r0 + (r1 - r0) * (run + L * t) / total, c)
        run += L


def wheel(cv, x, y, rad, tyre, hub):
    disc(cv, x, y, rad, tyre)
    disc(cv, x, y, max(0.75, rad * 0.42), hub)


def plus(cv, x, y, arm, thick, c):
    """A plus sign centred on (x, y), its arms `arm` cells from the middle and `thick` cells across."""
    box(cv, x - arm, y - thick / 2, x + arm, y + thick / 2, c)
    box(cv, x - thick / 2, y - arm, x + thick / 2, y + arm, c)


def note(cv, x, y, c, big=False):
    """A music note: a round head at (x, y) and its stem."""
    disc(cv, x, y, 0.9 if not big else 1.1, c)
    seg(cv, x + 0.6, y, x + 0.6, y - (2.2 if not big else 3.0), c, 0.35)


def town(cv, r, c, win, base, lo, hi, widths=(3, 4)):
    """Town houses along a street, their tops between rows lo and hi, with windows where a house is wide enough."""
    x = r.uniform(-2.0, 0.0)
    while x < cv.w:
        bw = r.choice(widths)
        box(cv, x, r.uniform(lo, hi), x + bw, base, c)
        x += bw + r.choice((1, 2))
    if win:
        dots(cv, win, c, 2, 2, area=(0, lo, cv.w - 1, base - 1))


# ---- Vehicles and machines ----

def ambulance(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.14)
    hills(cv, 'h', gtop - h * r.uniform(0.2, 0.26), 0.8, w * r.uniform(0.9, 1.4), r.uniform(0, 6))
    box(cv, 0, gtop, w, h, 'd')
    if big:
        for x in range(1, w, 4):
            box(cv, x, gtop + 1.2, x + 1.9, gtop + 1.9, 'y')
    wr = s * 0.11 + 0.2
    wy = gtop - wr + 0.6
    x0, x1 = w * 0.07, w * 0.93
    xm = x0 + (x1 - x0) * 0.64
    by0, by1 = gtop - h * 0.52, wy
    disc(cv, int(xm - 2.0) + (0.5 if not big else 0.0), by0, 1.5 if not big else 2.3, 'l')
    rbox(cv, x0, by0, xm, by1, 0.8, 'b')
    ct = by0 + (by1 - by0) * 0.3
    cm = ct + (by1 - ct) * 0.45
    poly(cv, [(xm - 0.5, ct), (x1 - w * 0.14, ct), (x1, cm), (x1, by1), (xm - 0.5, by1)], 'b')
    poly(cv, [(xm + 0.6, ct + 0.6), (x1 - w * 0.15, ct + 0.6), (x1 - w * 0.05, cm), (xm + 0.6, cm)], 'i')
    if big:
        box(cv, x0 + 1.0, by0 + 1.2, x0 + w * 0.12, by0 + h * 0.1, 'i')
        box(cv, x0, by1 - (by1 - by0) * 0.18 - 0.9, x1, by1 - (by1 - by0) * 0.18, 'z', only='b')
    mx, my = (x0 + xm) / 2, (by0 + by1) / 2 - 0.4
    if big:
        plus(cv, int(mx) + 1, int(my), 3.4, 1.9, 'x')
    else:
        plus(cv, int(mx) + 0.5, int(my) + 0.5, 1.0, 0.9, 'x')
    for x in (x0 + w * 0.14, x1 - w * 0.15):
        wheel(cv, x, wy, wr, 't', 'm')
    disc(cv, w * r.choice((0.14, 0.5)), h * 0.09, s * 0.085, 'u')
    if r.random() < 0.5:
        mirror(cv)
    return cv, [sky(), role('b', 'body', 'Ambulance', PINK), role('i', 'window', 'Windows', BLUE),
                role('x', 'cross', 'Cross', BROWN), role('z', 'stripe', 'Stripe', GREEN), role('l', 'light', 'Beacon', BROWN),
                role('t', 'tyre', 'Tyres', BROWN), role('m', 'hub', 'Hubs', GREEN), role('d', 'road', 'Road', GREEN),
                role('y', 'lines', 'Road lines', BROWN), role('h', 'hills', 'Hills', GREEN), role('u', 'sun', 'Sun', BROWN)], ['vehicles', 'town']


def taxi(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.14)
    city = r.random() < 0.6
    if city:
        town(cv, r, 'h', 'q', gtop, h * 0.3, h * 0.45, (3, 4) if not big else (4, 5))
    else:
        hills(cv, 'h', gtop - h * 0.22, 0.8, w * 1.2, r.uniform(0, 6))
    box(cv, 0, gtop, w, h, 'd')
    if big:
        for x in range(1, w, 4):
            box(cv, x, gtop + 1.2, x + 1.9, gtop + 1.9, 'y')
    cy = gtop - h * 0.12
    x0, x1 = w * 0.07, w * 0.93
    wb = cy - h * 0.12
    rbox(cv, x0, wb, x1, cy + h * 0.06, 1.0, 'b')
    poly(cv, [(w * 0.2, wb + 0.4), (w * 0.32, cy - h * 0.29), (w * 0.68, cy - h * 0.29), (w * 0.82, wb + 0.4)], 'b')
    poly(cv, [(w * 0.27, wb), (w * 0.35, cy - h * 0.26), (w * 0.48, cy - h * 0.26), (w * 0.48, wb)], 'i')
    poly(cv, [(w * 0.54, wb), (w * 0.54, cy - h * 0.26), (w * 0.65, cy - h * 0.26), (w * 0.75, wb)], 'i')
    fill(cv, 'k', lambda px, py: wb + 1.0 <= py < wb + (2.0 if not big else 3.0) and (int(px) + int(py)) % 2 == 0, only='b')
    rbox(cv, cx - 1.6, cy - h * 0.29 - (1.4 if not big else 2.0), cx + 1.6, cy - h * 0.29 + 0.2, 0.4, 'l')
    for x in (x0 + w * 0.17, x1 - w * 0.17):
        wheel(cv, x, cy + h * 0.07, s * 0.12 + 0.1, 't', 'm')
    disc(cv, w * r.choice((0.12, 0.88)), h * 0.09, s * 0.085, 'u')
    if r.random() < 0.5:
        mirror(cv)
    return cv, [sky(), role('b', 'body', 'Taxi', BROWN), role('k', 'checks', 'Checks', GREEN), role('i', 'window', 'Windows', BLUE),
                role('l', 'light', 'Roof sign', PINK), role('t', 'tyre', 'Tyres', PINK), role('m', 'hub', 'Hubs', BLUE),
                role('d', 'road', 'Road', GREEN), role('y', 'lines', 'Road lines', BROWN),
                role('h', 'town', 'Houses' if city else 'Hills', GREEN), role('q', 'lit', 'House windows', BLUE),
                role('u', 'sun', 'Sun', BROWN)], ['vehicles', 'town']


def race_car(w, h, r):
    cv, s, cx, big = start(w, h, 'd')
    vw = 1 if not big else 2
    phase = r.randrange(4)
    for x0 in (0, w - vw):
        box(cv, x0, 0, x0 + vw - 0.5, h, 'g')
    for y in range(h):
        c = 'k' if (y + phase) // 2 % 2 == 0 else 'y'
        cv.put(vw, y, c)
        cv.put(w - vw - 1, y, c)
    if big:
        for x in range(vw + 2, w - vw - 2):
            for y in (h - 3, h - 2):
                if (x + y) % 2 == 0:
                    cv.put(x, y, 'n')
    gx = int(cx) + (0.5 if w % 2 else 0.0)
    y0 = h * r.uniform(0.04, 0.08)
    y1 = y0 + h * (0.84 if not big else 0.8)
    L = y1 - y0
    tw, th = (2.0, 0.17) if not big else (2.4, 0.16)
    dx = min(w * 0.16, w / 2 - vw - 2.0 - tw)
    for yy, hh in ((0.16, th), (0.66, th * 1.15)):
        for side in (-1, 1):
            xa = gx + side * dx
            rbox(cv, min(xa, xa + side * tw), y0 + L * yy, max(xa, xa + side * tw), y0 + L * (yy + hh), 0.4, 't')
    for yy in (0.24, 0.74):
        box(cv, gx - dx, y0 + L * yy - 0.45, gx + dx, y0 + L * yy + 0.45, 'b')
    poly(cv, [(gx - 0.6, y0 + L * 0.06), (gx + 0.6, y0 + L * 0.06), (gx + dx * 0.5, y0 + L * 0.36), (gx + dx * 0.95, y0 + L * 0.46),
              (gx + dx * 0.95, y0 + L * 0.82), (gx + dx * 0.6, y0 + L * 0.94), (gx - dx * 0.6, y0 + L * 0.94),
              (gx - dx * 0.95, y0 + L * 0.82), (gx - dx * 0.95, y0 + L * 0.46), (gx - dx * 0.5, y0 + L * 0.36)], 'b')
    box(cv, gx - dx - tw * 0.6, y0, gx + dx + tw * 0.6, y0 + (0.9 if not big else 1.8), 'w')
    box(cv, gx - dx - tw * 0.4, y1 - (0.9 if not big else 1.8), gx + dx + tw * 0.4, y1, 'w')
    disc(cv, gx, y0 + L * 0.52, 1.25 if not big else 1.8, 'e')
    disc(cv, gx, y0 + L * 0.27, 0.9 if not big else 1.2, 'n')
    if big:
        box(cv, gx - 0.5, y0 + L * 0.62, gx + 0.5, y0 + L * 0.88, 'n')
    return cv, [('d', 'track', 'Track', BLUE, True), role('b', 'body', 'Race car', PINK), role('w', 'wing', 'Wings', GREEN),
                role('t', 'tyre', 'Tyres', BROWN), role('e', 'helmet', 'Helmet', BROWN), role('n', 'number', 'Number and start line', BLUE),
                role('k', 'kerb', 'Red kerb', PINK), role('y', 'kerb2', 'Orange kerb', BROWN), role('g', 'verge', 'Grass', GREEN)], ['vehicles', 'sport']


def dump_truck(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.14)
    raised = r.random() < 0.45
    hills(cv, 'h', gtop - h * 0.14, 0.8, w * 1.2, r.uniform(0, 6))
    box(cv, 0, gtop, w, h, 'd')
    wr = s * 0.13 + 0.2
    wy = gtop - wr + 0.6
    ch = wy - wr * 0.9
    bx0, bx1, bt, bb = w * 0.07, w * 0.64, gtop - h * 0.46, ch - 0.1
    hx, hy = bx0 + w * 0.1, bb
    a = math.radians(-24 if raised else 0)

    def turned(p):
        dx, dy = p[0] - hx, p[1] - hy
        return hx + dx * math.cos(a) - dy * math.sin(a), hy + dx * math.sin(a) + dy * math.cos(a)
    if raised:
        lx, ly = turned((bx0 + w * 0.12, bt))
        oval(cv, lx, ly + 0.4, w * 0.13, h * 0.07, 'l')
        for k in range(2 if not big else 3):
            disc(cv, bx0 - 0.4 + (k % 2) * 0.9, bb + 1.2 + k * 1.6, 0.8, 'l')
    else:
        oval(cv, (bx0 + bx1) / 2, bt, (bx1 - bx0) * 0.42, h * 0.08 + 0.4, 'l')
    box(cv, bx0, ch, w * 0.93, ch + 1.0, 'b')
    rbox(cv, w * 0.71, gtop - h * 0.42, w * 0.93, ch + 0.5, 0.6, 'b')
    box(cv, w * 0.71 + 1.0, gtop - h * 0.38, w * 0.89, gtop - h * 0.28, 'i')
    pts = [(bx0, bt), (bx1 + w * 0.04, bt), (bx1, bb), (bx0 + w * 0.12, bb), (bx0, bb - (bb - bt) * 0.45)]
    poly(cv, [turned(p) for p in pts], 'b')
    poly(cv, [turned(p) for p in ((bx1, bt), (bx1 + w * 0.16, bt), (bx1 + w * 0.16, bt + 0.9), (bx1, bt + 1.8))], 'b')
    for k in range(1, 3 if not big else 4):
        x = bx0 + w * 0.06 + k * (bx1 - bx0 - w * 0.06) / (3 if not big else 4)
        bar(cv, *turned((x, bt + 1.0)), *turned((x - 0.3, bb - 0.6)), 0.9, 'r')
    for x in ((0.24, 0.4, 0.8) if big else (0.28, 0.8)):
        wheel(cv, w * x, wy, wr, 't', 'm')
    disc(cv, w * 0.86 if raised else w * r.choice((0.14, 0.86)), h * 0.09, s * 0.085, 'u')
    if r.random() < 0.5:
        mirror(cv)
    return cv, [sky(), role('b', 'truck', 'Dump truck', PINK), role('r', 'ribs', 'Ribs', PINK), role('i', 'window', 'Window', BLUE),
                role('l', 'load', 'Load of earth', BROWN), role('t', 'tyre', 'Tyres', BROWN), role('m', 'hub', 'Hubs', BLUE),
                role('d', 'ground', 'Ground', GREEN), role('h', 'hills', 'Hills', GREEN), role('u', 'sun', 'Sun', BROWN)], ['vehicles', 'work']


def excavator(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.12)
    if big:
        hills(cv, 'h', gtop - h * 0.12, 0.6, w * 1.1, r.uniform(0, 6))
    box(cv, 0, gtop, w, h, 'd')
    oval(cv, w * 0.84, gtop + 0.6, w * 0.2, h * 0.13, 'p')
    tt, tb = gtop - h * 0.14, gtop + 0.4
    rbox(cv, w * 0.07, tt, w * 0.62, tb, (tb - tt) / 2, 't')
    n = 3 if not big else 4
    for k in range(n):
        disc(cv, w * 0.15 + k * (w * 0.4 / (n - 1)), (tt + tb) / 2, 0.8 if not big else 1.1, 'w')
    hy0 = gtop - h * 0.3
    rbox(cv, w * 0.07, hy0, w * 0.58, tt + 0.4, 0.8, 'b')
    rbox(cv, w * 0.1, gtop - h * 0.52, w * 0.37, hy0 + 0.5, 0.6, 'b')
    box(cv, w * 0.1 + 1.0, gtop - h * 0.48, w * 0.33, gtop - h * 0.36, 'i')
    ex, ey = w * 0.74, gtop - h * 0.66
    tx, ty = w * 0.86, gtop - h * 0.3
    bx0, by0 = w * 0.44, hy0 + 0.6
    bar(cv, bx0, by0, ex, ey, 1.6 if not big else 2.2, 'b')
    bar(cv, ex, ey, tx, ty, 1.2 if not big else 1.6, 'b')
    disc(cv, ex, ey, 1.0 if not big else 1.3, 'b')
    if big:
        L = math.hypot(ex - bx0, ey - by0)
        nx, ny = (ey - by0) / L * 1.9, -(ex - bx0) / L * 1.9
        bar(cv, bx0 + (ex - bx0) * 0.15 - nx, by0 + (ey - by0) * 0.15 - ny, bx0 + (ex - bx0) * 0.7 - nx, by0 + (ey - by0) * 0.7 - ny, 0.9, 'c')
    q = 1.0 if not big else 1.4
    poly(cv, [(tx - 1.0 * q, ty - 0.6 * q), (tx + 1.8 * q, ty - 0.4 * q), (tx + 1.6 * q, ty + 2.6 * q), (tx - 1.6 * q, ty + 2.4 * q)], 'k')
    if big:
        for k in range(3):
            cv.put(int(tx - 1.6 * q + k * 1.4), int(ty + 2.4 * q + 1), 'k')
    disc(cv, w * 0.12, h * 0.09, s * 0.085, 'u')
    if r.random() < 0.5:
        mirror(cv)
    return cv, [sky(), role('b', 'body', 'Excavator', BROWN), role('i', 'window', 'Window', BLUE), role('k', 'bucket', 'Bucket', PINK),
                role('c', 'piston', 'Piston', PINK), role('t', 'tracks', 'Tracks', BLUE), role('w', 'wheels', 'Track wheels', BROWN),
                role('p', 'dirt', 'Dirt heap', BROWN), role('d', 'ground', 'Ground', GREEN), role('h', 'hills', 'Hills', GREEN),
                role('u', 'sun', 'Sun', PINK)], ['vehicles', 'work']


def cement_mixer(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.14)
    box(cv, 0, gtop, w, h, 'd')
    if big:
        for x in (w * 0.05, w * 0.95):
            poly(cv, [(x - 1.2, gtop + 0.5), (x, gtop - 3.2), (x + 1.2, gtop + 0.5)], 'h')
            box(cv, x - 1.0, gtop - 1.6, x + 1.0, gtop - 1.0, 'u', only='h')
    wr = s * 0.12 + 0.2
    wy = gtop - wr + 0.6
    ch = wy - wr * 0.9
    box(cv, w * 0.07, ch, w * 0.93, ch + 1.0, 'b')
    rbox(cv, w * 0.72, gtop - h * 0.42, w * 0.93, ch + 0.5, 0.6, 'b')
    box(cv, w * 0.72 + 1.0, gtop - h * 0.38, w * 0.89, gtop - h * 0.28, 'i')
    dcx, dcy = w * 0.39, ch - h * 0.16
    rx, ry = w * 0.3, h * 0.15
    a = math.radians(14)
    ca, sa = math.cos(a), math.sin(a)
    bands = 1.0 if not big else 1.4

    def drum(px, py):
        return ((px - dcx) * ca + (py - dcy) * sa) / rx, (-(px - dcx) * sa + (py - dcy) * ca) / ry
    box(cv, w * 0.56, dcy + ry * 0.3, w * 0.64, ch, 'b')
    box(cv, w * 0.12, dcy, w * 0.2, ch, 'b')
    for x, y, px, py in cells(cv):
        u, v = drum(px, py)
        v /= 1.0 - 0.3 * max(0.0, -u)
        if abs(u) ** 2.6 + v * v <= 1.0:
            cv.g[y][x] = 'z' if ((u - 0.35 * v) * bands + 0.25) % 1.0 < 0.42 else 'r'
    ox, oy = dcx - rx * ca, dcy - rx * sa
    poly(cv, [(ox - 0.2, oy - ry * 0.7), (ox + 1.2, oy - ry * 0.5), (ox + 1.2, oy + ry * 0.5), (ox - 0.2, oy + ry * 0.7)], 'b')
    bar(cv, ox + 0.4, oy + ry * 0.6, ox - 0.4, oy + ry * 0.6 + h * 0.12, 1.0, 'b')
    for x in ((0.22, 0.38, 0.8) if big else (0.27, 0.8)):
        wheel(cv, w * x, wy, wr, 't', 'm')
    disc(cv, w * 0.86, h * 0.09, s * 0.085, 'u')
    if r.random() < 0.5:
        mirror(cv)
    return cv, [sky(), role('b', 'body', 'Cab and frame', PINK), role('i', 'window', 'Window', BLUE), role('r', 'drum', 'Drum', BROWN),
                role('z', 'stripe', 'Drum stripes', GREEN), role('t', 'tyre', 'Tyres', BROWN), role('m', 'hub', 'Hubs', BLUE),
                role('d', 'ground', 'Ground', GREEN), role('h', 'cones', 'Cones', GREEN), role('u', 'sun', 'Sun and cone stripes', PINK)], ['vehicles', 'work']


def canoe(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    shore = h * r.uniform(0.46, 0.52)
    box(cv, 0, shore, w, h, 'w')
    hills(cv, 'f', shore - 1.0, 0.6, w * 0.7, r.uniform(0, 6), only='s')
    for k in range(3 if not big else 5):
        x = (k + r.uniform(0.2, 0.8)) * w / (3 if not big else 5)
        poly(cv, [(x - 1.5, shore - 0.4), (x, shore - h * r.uniform(0.12, 0.18)), (x + 1.5, shore - 0.4)], 'f')
    yt = h * 0.64
    hl, depth = w * 0.42, h * 0.13 + 0.6
    jx = int(cx - w * 0.06) + 0.5
    rbox(cv, jx - 1.6, yt - h * 0.16, jx + 1.6, yt + 1.0, 0.8, 'j')
    disc(cv, jx, yt - h * 0.16 - 1.2, 1.25 if not big else 1.6, 'e')

    def hull(px, py):
        t = (px - cx) / hl
        return abs(t) <= 1.0 and yt - 2.2 * t ** 4 <= py <= yt + depth * (1.0 - t * t) ** 0.6
    fill(cv, 'c', hull)
    fill(cv, 'k', lambda px, py: abs(px - cx) < hl * 0.8 and yt - 2.2 * ((px - cx) / hl) ** 4 <= py < yt - 2.2 * ((px - cx) / hl) ** 4 + 1.0, only='c')
    px0, py0, px1, py1 = jx - w * 0.22, yt - h * 0.26, jx + w * 0.22, yt + h * 0.16
    bar(cv, px0, py0, px1, py1, 0.8 if not big else 1.1, 'p')
    lens(cv, px1 - (px1 - px0) * 0.14, py1 - (py1 - py0) * 0.14, px1 + (px1 - px0) * 0.1, py1 + (py1 - py0) * 0.1, 2.0 if not big else 2.8, 'p')
    for x in (w * 0.12, w * 0.86) if not big else (w * 0.1, w * 0.3, w * 0.86):
        oval(cv, x, h - 1.6, s * 0.1, 0.9 if not big else 1.3, 'r')
    if big:
        for x in (w * 0.04, w * 0.08):
            seg(cv, x, shore + 0.5, x + 0.4, shore - h * 0.08, 'r', 0.45)
    disc(cv, w * r.choice((0.14, 0.86)), h * 0.1, s * 0.085, 'u')
    if r.random() < 0.5:
        mirror(cv)
    return cv, [sky(), role('w', 'lake', 'Lake', BLUE), role('f', 'shore', 'Far shore', GREEN), role('c', 'canoe', 'Canoe', PINK),
                role('k', 'rim', 'Rim', BROWN), role('j', 'jacket', 'Life jacket', GREEN), role('e', 'head', 'Head', BROWN),
                role('p', 'paddle', 'Paddle', BROWN), role('r', 'pads', 'Lily pads and reeds', GREEN), role('u', 'sun', 'Sun', PINK)], ['vehicles', 'lake']


def glider(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    hills(cv, 'h', h * 0.88, 0.7, w * r.uniform(0.8, 1.2), r.uniform(0, 6))
    if big:
        for x0 in (w * 0.08, w * 0.6):
            box(cv, x0, h * 0.92, x0 + w * 0.26, h - 1.6, 'v', only='h')
    cloud(cv, w * r.uniform(0.15, 0.25), h * r.uniform(0.6, 0.7), s * 0.06 + 0.5, 'c')
    cloud(cv, w * r.uniform(0.75, 0.85), h * r.uniform(0.5, 0.62), s * 0.05 + 0.4, 'c')
    gx = int(cx) + (0.5 if not big else 0.0)
    wy = int(h * 0.36) + 0.5
    span = w / 2 - 1.2
    root, tip = (1.05, 0.45) if not big else (1.5, 0.95)
    poly(cv, [(gx - span, wy - tip), (gx, wy - root), (gx + span, wy - tip), (gx + span, wy + tip), (gx, wy + root), (gx - span, wy + tip)], 'p')
    fill(cv, 'x', lambda px, py: abs(px - gx) > span - (1.8 if not big else 2.6), only='p')
    fat, thin = (1.05, 0.55) if not big else (1.55, 0.75)
    tube(cv, [(gx, h * 0.11), (gx, h * 0.16)], fat * 0.75, fat, 'f')
    tube(cv, [(gx, h * 0.16), (gx, h * 0.36)], fat, fat, 'f')
    tube(cv, [(gx, h * 0.36), (gx, h * 0.48)], fat, thin, 'f')
    tube(cv, [(gx, h * 0.48), (gx, h * 0.78)], thin, thin, 'f')
    oval(cv, gx, h * 0.21, 0.6 if not big else 1.0, 1.4 if not big else 2.2, 'i')
    box(cv, gx - w * 0.17, h * 0.75 - 0.5, gx + w * 0.17, h * 0.75 + (0.5 if not big else 1.2), 'f')
    disc(cv, w * r.choice((0.12, 0.88)), h * 0.1, s * 0.085, 'u')
    return cv, [sky(), role('p', 'wing', 'Wings', PINK), role('f', 'fuselage', 'Body and tail', PINK), role('x', 'tips', 'Wing tips', BROWN),
                role('i', 'canopy', 'Canopy', BLUE), role('c', 'cloud', 'Clouds', BLUE), role('h', 'hills', 'Hills', GREEN),
                role('v', 'fields', 'Fields', GREEN), role('u', 'sun', 'Sun', BROWN)], ['vehicles', 'sky']


def parachute(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    hills(cv, 'g', h * 0.9, 0.6, w * 1.2, r.uniform(0, 6))
    cloud(cv, w * r.choice((0.16, 0.84)), h * 0.64, s * 0.06 + 0.4, 'c')
    pcx = cx + r.uniform(-0.6, 0.6)
    cy0, rx, ry = h * 0.36, w * 0.39, h * 0.26
    n = 5 if not big else 7
    for x, y, px, py in cells(cv):
        dx = (px - pcx) / rx
        if abs(dx) > 1.0:
            continue
        u = (math.asin(dx) / math.pi + 0.5) * n
        edge = cy0 + 0.9 * math.sin(math.pi * (u % 1.0))
        if py <= edge and (py > cy0 or dx * dx + ((py - cy0) / ry) ** 2 <= 1.0):
            cv.g[y][x] = 'a' if int(u) % 2 == 0 else 'q'
    jy = h * 0.68
    for k in range(n + 1):
        lx = pcx + rx * 0.96 * math.sin((k / n - 0.5) * math.pi)
        seg(cv, lx, cy0 + 0.6, pcx + (0.6 if lx > pcx else -0.6), jy - 1.0, 'k', 0.4)
    disc(cv, pcx, jy - 1.6, 1.0 if not big else 1.4, 'e')
    rbox(cv, pcx - 1.0, jy - 0.6, pcx + 1.0, jy + 1.8, 0.4, 'j')
    for side in (-1, 1):
        seg(cv, pcx + side * 0.5, jy + 1.8, pcx + side * 0.9, jy + 3.6, 'j', 0.45)
    if big:
        ring(cv, w * 0.7, h * 0.95, 2.4, 1.2, 't', 0.45)
    disc(cv, w * 0.88 if pcx < cx else w * 0.12, h * 0.08, s * 0.07, 'u')
    return cv, [sky(), role('a', 'canopy', 'Canopy', PINK), role('q', 'gore', 'Canopy stripes', BROWN), role('k', 'lines', 'Lines', GREEN),
                role('j', 'jumper', 'Jumper', PINK), role('e', 'helmet', 'Helmet', BROWN), role('c', 'cloud', 'Cloud', BLUE),
                role('g', 'field', 'Field', GREEN), role('t', 'target', 'Landing mark', BROWN), role('u', 'sun', 'Sun', BROWN)], ['sport', 'sky']


# ---- Sport and play ----

def roller_skate(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.14)
    box(cv, 0, gtop, w, h, 'd')
    for x in (w * 0.08, w * 0.5, w * 0.92) if not big else (w * 0.06, w * 0.3, w * 0.5, w * 0.94):
        poly(cv, [(x - 1.2, gtop + 0.6), (x - 0.6, gtop - 1.0), (x, gtop + 0.2), (x + 0.6, gtop - 1.0), (x + 1.2, gtop + 0.6)], 'q')
    wr = s * 0.11 + 0.2
    wy = gtop - wr + 0.4
    sy = wy - wr - 0.4
    x0, x1 = w * 0.14, w * 0.9
    rbox(cv, x0, h * 0.14, w * 0.5, sy, 1.0, 'b')
    rbox(cv, x0, sy - h * 0.2, x1, sy, 2.0, 'b')
    box(cv, x0, h * 0.14, w * 0.5, h * 0.14 + (1.0 if not big else 1.8), 'c')
    lx = w * 0.46
    for y in range(int(h * 0.14) + 2, int(sy - h * 0.12)):
        cv.put(int(lx) - (y % 2), y, 'l')
    for k in range(3 if not big else 4):
        cv.put(int(w * 0.52 + k * 1.2) + (k % 2), int(sy - h * 0.18 + 1 + k * 0.3), 'l')
    box(cv, x0, sy - 0.1, x1 - 0.5, sy + 0.9, 'o')
    rbox(cv, x1 - 2.2, sy, x1, sy + 1.9, 0.5, 'o')
    for x in (w * 0.28, w * 0.66):
        wheel(cv, x, wy, wr, 'w', 'e')
    disc(cv, w * 0.84, h * 0.1, s * 0.085, 'u')
    if r.random() < 0.5:
        mirror(cv)
    return cv, [sky(), role('b', 'boot', 'Boot', PINK), role('c', 'collar', 'Collar', BROWN), role('l', 'laces', 'Laces', BLUE),
                role('o', 'sole', 'Sole and toe stop', BROWN), role('w', 'wheel', 'Wheels', GREEN), role('e', 'hub', 'Hubs', PINK),
                role('d', 'path', 'Path', BROWN), role('q', 'grass', 'Grass tufts', GREEN), role('u', 'sun', 'Sun', PINK)], ['toys', 'park']


def surfboard(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    sea = h * 0.57
    hills(cv, 'w', sea, 0.3, w * 0.5, r.uniform(0, 6))
    path(cv, [(0, sea + 0.4), (w * 0.3, sea + 0.1), (w * 0.6, sea + 0.5), (w, sea + 0.2)], 'f', 0.5)
    hills(cv, 'd', h * 0.8, 0.6, w * 1.3, r.uniform(0, 6))
    side = r.choice((-1, 1))
    bx = cx - side * w * 0.12
    tilt = side * w * r.uniform(0.0, 0.08)
    lens(cv, bx, h * 0.95, bx + tilt, h * 0.07, w * 0.26, 'b')
    seg(cv, bx + tilt * 0.9, h * 0.13, bx + tilt * 0.2, h * 0.74, 'z', 0.4)
    if big:
        fill(cv, 'q', lambda px, py: abs(py - (h * 0.26 + 1.0)) <= 1.0, only='b')
    px = cx + side * w * 0.3
    top = (px - side * 0.4, h * 0.3)
    path(cv, [(px, h * 0.82), (px - side * 0.6, h * 0.56), top], 't', 0.6 if not big else 0.8)
    for a in (-160, -120, -60, -20, 20, 160):
        t = math.radians(a)
        lens(cv, top[0], top[1], top[0] + math.cos(t) * s * 0.3, top[1] + math.sin(t) * s * 0.18 + s * 0.07, s * 0.1, 'p')
    disc(cv, cx - side * w * 0.38, h * 0.1, s * 0.085, 'u')
    if big:
        star(cv, cx + side * w * 0.1, h * 0.9, s * 0.05 + 0.6, 'x', ri=0.6)
    return cv, [sky(), role('b', 'board', 'Surfboard', PINK), role('z', 'stripe', 'Stripe', BROWN), role('q', 'band', 'Band', GREEN),
                role('w', 'sea', 'Sea', BLUE), role('f', 'foam', 'Foam', GREEN), role('d', 'sand', 'Sand', BROWN),
                role('t', 'trunk', 'Palm trunk', BROWN), role('p', 'leaves', 'Palm leaves', GREEN), role('x', 'starfish', 'Starfish', PINK),
                role('u', 'sun', 'Sun', BROWN)], ['sport', 'beach']


def tennis_racket(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    ctop = h * 0.74
    box(cv, 0, ctop - (1.5 if not big else 2.4), w, ctop, 'h')
    box(cv, 0, ctop, w, h, 'c')
    box(cv, 0, ctop + (h - ctop) * 0.45, w, ctop + (h - ctop) * 0.45 + 0.9, 'l')
    hx, hy = cx, h * 0.3
    rx, ry = w * 0.3, h * 0.25
    t = 1.0 if not big else 1.5
    oval(cv, hx, hy, rx, ry, 'f')
    ty0, ty1 = hy + ry * 0.75, h * 0.68
    poly(cv, [(hx - rx * 0.62, ty0), (hx + rx * 0.62, ty0), (hx + 1.0, ty1), (hx - 1.0, ty1)], 'f')
    if big:
        pw = rx * 0.62 - (rx * 0.62 - 1.0) * (hy + ry + 0.4 - ty0) / (ty1 - ty0) - 1.6
        poly(cv, [(hx - pw, hy + ry + 0.4), (hx + pw, hy + ry + 0.4), (hx, h * 0.6)], 's')
    oval(cv, hx, hy, rx - t, ry - t, 's')
    xo, yo = int(hx) % 3, int(hy) % 3
    fill(cv, 'k', lambda px, py: ((int(px) - xo) % 3 == 0 or (int(py) - yo) % 3 == 0) and
         ((px - hx) / (rx - t)) ** 2 + ((py - hy) / (ry - t)) ** 2 <= 1.0)
    box(cv, hx - 1.0, h * 0.68, hx + 1.0, h, 'g')
    side = r.choice((-1, 1))
    bx, br = hx + side * w * 0.3, s * 0.12 + 0.3
    disc(cv, bx, ctop + 1.0, br, 'b')
    fill(cv, 'z', lambda px, py: abs(math.hypot(px - (bx - side * br * 1.3), py - (ctop + 1.0)) - br * 1.05) <= 0.5, only='b')
    if big:
        disc(cv, hx - side * w * 0.36, h * 0.12, br * 0.9, 'b')
    cloud(cv, w * (0.16 if side > 0 else 0.84), h * 0.62, s * 0.05 + 0.3, 'o')
    return cv, [sky(), role('f', 'frame', 'Frame', PINK), role('k', 'strings', 'Strings', BROWN), role('g', 'grip', 'Grip', BROWN),
                role('b', 'ball', 'Ball', BROWN), role('z', 'seam', 'Seam', PINK), role('c', 'court', 'Court', GREEN),
                role('l', 'line', 'Court line', BLUE), role('h', 'hedge', 'Hedge', GREEN), role('o', 'cloud', 'Cloud', BLUE)], ['sport']


def basketball(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.14)
    box(cv, 0, gtop, w, h, 'c')
    if big:
        for x in range(1, w, 4):
            box(cv, x, gtop + 1.4, x + 2.4, gtop + 2.2, 'l')
    px = w * 0.24
    box(cv, px - 0.5, h * 0.3, px + 0.5, gtop + 0.5, 'p')
    bx0, bx1, by0, by1 = w * 0.06, w * 0.46, h * 0.05, h * 0.3
    rbox(cv, bx0, by0, bx1, by1, 0.5, 'p')
    mx0, mx1 = w * 0.16, w * 0.36
    box(cv, mx0, by0 + h * 0.08, mx1, by1 - 0.4, 'o')
    box(cv, mx0 + 1.0, by0 + h * 0.08 + 1.0, mx1 - 1.0, by1 - 1.4, 'p')
    rx0, rx1 = w * 0.1, w * 0.42
    box(cv, rx0, by1, rx1, by1 + 0.9, 'o')
    ny = by1 + h * 0.15
    fill(cv, 'n', lambda px, py: by1 + 0.9 < py <= ny and (rx0 + (py - by1) * 0.25 <= px <= rx1 - (py - by1) * 0.25) and
         ((int(px) + int(py)) % 2 == 0 or px < rx0 + (py - by1) * 0.25 + 1.0 or px > rx1 - (py - by1) * 0.25 - 1.0 or py > ny - 1.0))
    bx, br = w * 0.68, s * 0.24
    by = gtop - br + 0.6
    disc(cv, bx, by, br, 'b')
    fill(cv, 'z', lambda px, py: abs(px - bx) <= 0.45 or abs(py - by) <= 0.45 or
         abs(math.hypot(px - bx + br * 1.6, py - by) - br * 1.0) <= 0.45 or abs(math.hypot(px - bx - br * 1.6, py - by) - br * 1.0) <= 0.45, only='b')
    disc(cv, w * 0.86, h * 0.1, s * 0.085, 'u')
    if r.random() < 0.5:
        mirror(cv)
    return cv, [sky(), role('b', 'ball', 'Basketball', BROWN), role('z', 'seam', 'Seams', PINK), role('p', 'board', 'Board and pole', PINK),
                role('o', 'rim', 'Rim and square', BROWN), role('n', 'net', 'Net', GREEN), role('c', 'court', 'Court', GREEN),
                role('l', 'line', 'Court lines', BLUE), role('u', 'sun', 'Sun', BLUE)], ['sport']


def backpack(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    fy = h * 0.88
    box(cv, 0, fy, w, h, 'f')
    bx = int(cx) + (0.5 if w % 2 else 0.0) + r.choice((-1, 0, 1))
    x0, x1, top = bx - w * 0.31, bx + w * 0.31, h * 0.24
    ring(cv, bx, top + 0.4, s * 0.12, s * 0.12 - 1.0, 'k')
    if big:
        for side in (-1, 1):
            rbox(cv, bx + side * w * 0.31 - w * 0.08, h * 0.52, bx + side * w * 0.31 + w * 0.08, fy + 0.4, 1.0, 'p')
    rbox(cv, x0, top, x1, fy + 0.4, w * 0.2, 'g')
    py0 = h * 0.56
    rbox(cv, x0 + w * 0.07, py0, x1 - w * 0.07, fy - 0.6, 1.0, 'p')
    box(cv, x0 + w * 0.07 + 0.6, py0 + 0.6, x1 - w * 0.07 - 0.6, py0 + 1.4, 'z')
    box(cv, bx - 0.5, py0 + 1.5, bx + 0.5, py0 + 2.4, 'z')
    for side in (-1, 1):
        box(cv, bx + side * w * 0.15 - 0.5, top + h * 0.05, bx + side * w * 0.15 + 0.5, py0 - 0.1, 'k')
    if big:
        star(cv, bx, h * 0.73, s * 0.08 + 0.4, 'x', ri=0.8)
    ox = bx + r.choice((-1, 1)) * w * 0.41
    rbox(cv, ox - s * 0.11, fy - 1.9, ox + s * 0.11, fy + 0.4, 0.3, 'o')
    rbox(cv, ox - s * 0.08, fy - 3.4 - (0.8 if big else 0.0), ox + s * 0.08, fy - 1.9, 0.3, 'x')
    return cv, [('b', 'wall', 'Wall', BLUE, True), role('g', 'bag', 'Backpack', PINK), role('p', 'pocket', 'Pockets', GREEN),
                role('z', 'zip', 'Zip', BLUE), role('k', 'strap', 'Handle and straps', BROWN), role('x', 'book2', 'Book and patch', PINK),
                role('o', 'book', 'Book', GREEN), role('f', 'floor', 'Floor', BROWN)], ['things', 'school']


def desk_lamp(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    fy = h * 0.82
    box(cv, 0, fy, w, h, 'd')
    wx0, wx1, wy0, wy1 = w * 0.06, w * 0.38, h * 0.06, h * 0.34
    box(cv, wx0, wy0, wx1, wy1, 'f')
    box(cv, wx0 + 1.0, wy0 + 1.0, wx1 - 1.0, wy1 - 1.0, 'i')
    box(cv, (wx0 + wx1) / 2 - 0.5, wy0, (wx0 + wx1) / 2 + 0.5, wy1, 'f')
    lx = w * 0.24
    ex, ey = w * 0.44, h * 0.44
    jx, jy = w * 0.62, h * 0.2
    a = math.radians(70)
    L = s * 0.22
    sx, sy = jx + math.cos(a) * L, jy + math.sin(a) * L
    nx, ny = -math.sin(a), math.cos(a)
    wide = s * 0.18
    fill(cv, 'v', lambda px, py: py > sy and abs(px - sx - (py - sy) * 0.36) <= wide * 0.75 + (py - sy) * 0.25, only='b')
    oval(cv, lx, fy, w * 0.15, 1.4 if not big else 2.0, 'l')
    bar(cv, lx, fy - 1.0, ex, ey, 0.9 if not big else 1.3, 'l')
    bar(cv, ex, ey, jx, jy, 0.9 if not big else 1.3, 'l')
    poly(cv, [(jx + nx * 0.9, jy + ny * 0.9), (jx - nx * 0.9, jy - ny * 0.9), (sx - nx * wide, sy - ny * wide), (sx + nx * wide, sy + ny * wide)], 'l')
    disc(cv, sx + math.cos(a) * 0.4, sy + math.sin(a) * 0.4, wide * 0.5, 'u')
    for x, y in ((ex, ey), (jx, jy)):
        disc(cv, x, y, 0.8 if not big else 1.1, 'j')
    bkx = sx + (fy - sy) * 0.36
    rbox(cv, bkx - w * 0.17, fy - (1.6 if not big else 2.6), bkx + w * 0.15, fy + 0.4, 0.3, 'o')
    if big:
        box(cv, bkx - w * 0.15, fy - 1.6, bkx + w * 0.13, fy - 1.0, 'e')
    if r.random() < 0.5:
        mirror(cv)
    return cv, [('b', 'wall', 'Wall', BLUE, True), role('l', 'lamp', 'Lamp', PINK), role('j', 'joint', 'Joints', BROWN),
                role('u', 'bulb', 'Bulb', BROWN), role('v', 'beam', 'Light', GREEN), role('o', 'book', 'Book', PINK),
                role('e', 'pages', 'Pages', BLUE), role('f', 'frame', 'Window frame', BROWN), role('i', 'window', 'Window', BLUE),
                role('d', 'desk', 'Desk', BROWN)], ['things', 'desk']


def wheelbarrow(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.14)
    hills(cv, 'g', gtop, 0.4, w * 1.4, r.uniform(0, 6))
    wr = s * 0.12 + 0.3
    wx, wy = w * 0.76, gtop - wr + 0.6
    tt, tb = gtop - h * 0.42, gtop - h * 0.24
    oval(cv, w * 0.55, tt, w * 0.28, h * 0.06 + 0.6, 'l')
    for k in range(2 if not big else 4):
        x = w * (0.42 + k * (0.24 / (1 if not big else 3)))
        lean = (-1 if k % 2 == 0 else 1) * w * 0.08
        lens(cv, x, tt - 0.6, x + lean, tt - h * 0.17, 1.8 if not big else 2.2, 'q')
        if big:
            disc(cv, x + lean, tt - h * 0.18, 1.2, 'o')
    bar(cv, wx, wy, w * 0.04, tt + h * 0.04, 0.9 if not big else 1.2, 'k')
    poly(cv, [(w * 0.2, tt), (w * 0.9, tt), (w * 0.72, tb), (w * 0.32, tb)], 'x')
    box(cv, w * 0.18, tt - 0.4, w * 0.92, tt + 0.6, 'x')
    bar(cv, w * 0.36, tb - 0.5, w * 0.32, gtop + 0.4, 0.9 if not big else 1.2, 'k')
    wheel(cv, wx, wy, wr, 'w', 'm')
    for x in (w * 0.08, w * 0.2) if not big else (w * 0.06, w * 0.14, w * 0.94):
        disc(cv, x, gtop + 0.6, 0.9, 'o')
    disc(cv, w * 0.86, h * 0.1, s * 0.085, 'u')
    if r.random() < 0.5:
        mirror(cv)
    return cv, [sky(), role('x', 'tray', 'Tray', PINK), role('k', 'frame', 'Handles and leg', BROWN), role('w', 'wheel', 'Wheel', BLUE),
                role('m', 'hub', 'Hub', BROWN), role('l', 'soil', 'Soil', BROWN), role('q', 'plants', 'Plants', GREEN),
                role('o', 'flowers', 'Flowers', PINK), role('g', 'grass', 'Grass', GREEN), role('u', 'sun', 'Sun', BROWN)], ['tools', 'garden']


def pinwheel(w, h, r):
    cv, s, cx, big = start(w, h, 's')
    gtop = h - ground_rows(h, 0.12)
    hills(cv, 'g', gtop, 0.4, w * 1.4, r.uniform(0, 6))
    cloud(cv, w * r.choice((0.2, 0.8)), h * 0.08, s * 0.06 + 0.4, 'c')
    px, py = int(cx) + 0.5, h * 0.36
    box(cv, px - 0.5, py, px + 0.5, h, 'k')
    R = s * 0.4
    rot = r.uniform(0, 90)
    for k, c in enumerate('aqey'):
        a = rot + k * 90
        pts = [(px, py)]
        for da, rr in ((0, R), (25, R * 0.92), (50, R * 0.72), (75, R * 0.48), (90, R * 0.2)):
            t = math.radians(a + da)
            pts.append((px + math.cos(t) * rr, py + math.sin(t) * rr))
        poly(cv, pts, c)
    disc(cv, px, py, 0.8 if not big else 1.2, 'm')
    for x in (w * 0.1, w * 0.86) if not big else (w * 0.08, w * 0.22, w * 0.8, w * 0.92):
        disc(cv, x, gtop + 0.4, 0.9, 'o')
    return cv, [sky(), role('a', 'blade', 'Pink blade', PINK), role('q', 'blade2', 'Green blade', GREEN), role('e', 'blade3', 'Violet blade', PINK),
                role('y', 'blade4', 'Orange blade', BROWN), role('m', 'pin', 'Pin', BLUE), role('k', 'stick', 'Stick', BROWN),
                role('c', 'cloud', 'Cloud', BLUE), role('o', 'flowers', 'Flowers', PINK), role('g', 'grass', 'Grass', GREEN)], ['toys', 'park']


# ---- Music and the house ----

def music_box(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    fy = h * 0.88
    box(cv, 0, fy, w, h, 't')
    x0, x1, top = w * 0.14, w * 0.86, h * 0.62
    lt = top - h * 0.22
    poly(cv, [(x0 + 0.5, top), (x1 - 0.5, top), (x1 - w * 0.05, lt), (x0 + w * 0.05, lt)], 'x')
    poly(cv, [(x0 + 1.5, top - 0.4), (x1 - 1.5, top - 0.4), (x1 - w * 0.05 - 1.0, lt + 1.0), (x0 + w * 0.05 + 1.0, lt + 1.0)], 'v')
    box(cv, x0, top, x1, fy + 0.4, 'x')
    ty = top + (fy - top) * 0.5
    box(cv, x0, ty - 0.45, x1, ty + 0.45, 'g')
    box(cv, x1, ty - 0.45, x1 + 1.4, ty + 0.45, 'g')
    box(cv, x1 + 0.6, ty - 1.5, x1 + 1.4, ty + 1.5, 'g')
    dx = int(cx) + (0.5 if w % 2 else 0.0)
    k = 1.0 if not big else 1.4
    oval(cv, dx, top - 0.2, 2.0 * k, 0.8, 'g')
    seg(cv, dx, top - 0.6, dx, top - h * 0.1, 'p', 0.45)
    oval(cv, dx, top - h * 0.12, 2.4 * k, 0.9 * k, 'p')
    box(cv, dx - 0.5 * k, top - h * 0.22, dx + 0.5 * k, top - h * 0.12, 'p')
    disc(cv, dx, top - h * 0.25, 1.0 * k, 'p')
    path(cv, [(dx - 0.6, top - h * 0.18), (dx - 2.0 * k, top - h * 0.26), (dx - 0.4, top - h * 0.31)], 'p', 0.4)
    path(cv, [(dx + 0.6, top - h * 0.18), (dx + 2.0 * k, top - h * 0.26), (dx + 0.4, top - h * 0.31)], 'p', 0.4)
    for j, (x, y) in enumerate(((0.12, 0.2), (0.86, 0.14), (0.9, 0.42), (0.08, 0.46))):
        if j < 2 or big:
            note(cv, w * x, h * y, 'n', big)
    return cv, [('b', 'wall', 'Wall', BLUE, True), role('x', 'box', 'Box and lid', BROWN), role('v', 'lining', 'Lining', BLUE),
                role('g', 'trim', 'Trim and key', PINK), role('p', 'dancer', 'Dancer', PINK), role('n', 'notes', 'Music notes', GREEN),
                role('t', 'table', 'Table', GREEN)], ['things', 'music']


def xylophone(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    fy = h * 0.86
    box(cv, 0, fy, w, h, 'f')
    n = 5 if not big else (6 if w < 20 else 7)
    bw, gap = (2, 0) if not big else (2, 1)
    total = n * bw + (n - 1) * gap
    x0 = int((w - total) / 2)
    yc = h * 0.5
    H0, H1 = h * 0.5, h * 0.26
    tops, bots = [], []
    for k in range(n):
        hh = H0 + (H1 - H0) * k / (n - 1)
        tops.append(yc - hh / 2)
        bots.append(yc + hh / 2)
    xa, xb = x0 - 1, x0 + total + 1
    poly(cv, [(xa, tops[0] - 1.0), (xb, tops[-1] - 1.0), (xb, bots[-1] + 1.0), (xa, bots[0] + 1.0)], 'k')
    for side in (0, 1):
        fx = xa + 1.0 if side == 0 else xb - 1.0
        box(cv, fx - 0.9, (bots[0] if side == 0 else bots[-1]) + 0.5, fx + 0.9, fy + 0.4, 'k')
    for k in range(n):
        x = x0 + k * (bw + gap)
        rbox(cv, x, tops[k], x + bw, bots[k], 0.3, 'acegyz'[k % 6])
        if big:
            for y in (tops[k] + 1.6, bots[k] - 1.6):
                cv.put(x + 1, int(y), 'm')
    side = r.choice((-1, 1))
    for j in range(2):
        hx = cx + side * (w * 0.12 + j * w * 0.12)
        hy = yc - h * 0.05 + j * h * 0.06
        ex, ey = hx + side * w * 0.3, hy + h * 0.32
        bar(cv, hx, hy, ex, ey, 0.6 if not big else 0.9, 'k')
        disc(cv, hx, hy, 1.0 if not big else 1.4, 'm')
    for j, (x, y) in enumerate(((0.14, 0.12), (0.84, 0.1), (0.5, 0.06))):
        if j < 2 or big:
            note(cv, w * x, h * y, 'n', big)
    return cv, [('b', 'wall', 'Wall', BLUE, True), role('a', 'bar1', 'Red bar', PINK), role('c', 'bar2', 'Green bar', GREEN),
                role('e', 'bar3', 'Blue bar', BLUE), role('g', 'bar4', 'Violet bar', PINK), role('y', 'bar5', 'Mint bar', GREEN),
                role('z', 'bar6', 'Sky blue bar', BLUE), role('k', 'frame', 'Frame and sticks', BROWN),
                role('m', 'mallet', 'Mallet heads and nails', BROWN), role('n', 'notes', 'Music notes', PINK),
                role('f', 'floor', 'Floor', GREEN)], ['things', 'music']


def accordion(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    fy = h * 0.86
    box(cv, 0, fy, w, h, 'f')
    top, bot = h * 0.34, fy + 0.4
    lx0, lx1, rx0, rx1 = w * 0.04, w * 0.27, w * 0.68, w * 0.96
    for x in range(int(lx1) - 1, int(rx0) + 2):
        tall = x % 2 == 0
        box(cv, x, top + (1.0 if tall else 2.0), x + 0.9, bot - (1.4 if tall else 2.4), 'p' if tall else 'q')
    rbox(cv, lx0, top, lx1, bot, 0.6, 'c')
    rbox(cv, rx0, top, rx1, bot, 0.6, 'c')
    kx0, kx1 = rx0 + 0.9, rx1 - 0.7
    box(cv, kx0, top + 1.0, kx1, bot - 1.6, 'w')
    for j, y in enumerate(range(int(top + 1.0) + 1, int(bot - 1.6) - 1, 2)):
        if j % 3 != 2:
            box(cv, kx0, y, kx0 + max(2.0, (kx1 - kx0) * 0.55), y + 0.9, 'k')
    dots(cv, 'o', 'c', 2, 2, area=(lx0, top + 1, lx1, bot - 2))
    seg(cv, lx1 - 0.5, top - 1.2, rx0 + 0.5, top - 1.2, 'k', 0.45)
    for x in (lx1 - 0.5, rx0 + 0.5):
        seg(cv, x, top - 1.2, x, top, 'k', 0.45)
    for j, (x, y) in enumerate(((0.16, 0.12), (0.5, 0.16), (0.84, 0.1), (0.3, 0.26))):
        if j < 3 or big:
            note(cv, w * x, h * y, 'n', big)
    if r.random() < 0.5:
        mirror(cv)
    return cv, [('b', 'wall', 'Wall', BLUE, True), role('c', 'case', 'Case', PINK), role('p', 'bellows', 'Bellows', BROWN),
                role('q', 'fold', 'Folds', GREEN), role('w', 'keys', 'Keys', BLUE), role('k', 'black', 'Black keys and strap', BROWN),
                role('o', 'buttons', 'Buttons', BLUE), role('n', 'notes', 'Music notes', GREEN), role('f', 'floor', 'Floor', GREEN)], ['things', 'music']


def saxophone(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    fy = h * 0.92
    box(cv, 0, fy, w, h, 'f')
    for side in (-1, 1):
        x = 0 if side < 0 else w
        poly(cv, [(x, 0), (x - side * w * 0.16, 0), (x - side * w * 0.05, h * 0.5), (x - side * w * 0.09, fy), (x, fy)], 'r')
    k = 1.0 if not big else 1.4
    bx = w * 0.38
    tube(cv, [(w * 0.22, h * 0.09), (w * 0.3, h * 0.11), (bx, h * 0.2)], 0.55 * k, 0.75 * k, 's')
    tube(cv, [(bx, h * 0.2), (bx + w * 0.02, h * 0.68)], 0.8 * k, 1.15 * k, 's')
    tube(cv, [(bx + w * 0.02, h * 0.68), (bx + w * 0.07, h * 0.82), (bx + w * 0.2, h * 0.86), (bx + w * 0.32, h * 0.8), (bx + w * 0.36, h * 0.66)], 1.15 * k, 1.45 * k, 's')
    tube(cv, [(bx + w * 0.36, h * 0.66), (bx + w * 0.39, h * 0.48)], 1.45 * k, 2.2 * k, 's')
    oval(cv, bx + w * 0.4, h * 0.46, 2.7 * k, 1.0 * k, 's')
    oval(cv, bx + w * 0.4, h * 0.46, 1.7 * k, 0.6 * k + 0.1, 'e')
    disc(cv, w * 0.21, h * 0.085, 0.9 * k, 'm')
    for j in range(3 if not big else 5):
        cv.put(int(bx + w * 0.01 * j), int(h * (0.3 + j * 0.34 / (2 if not big else 4))), 'k')
    for j, (x, y) in enumerate(((0.74, 0.16), (0.62, 0.28), (0.52, 0.08))):
        if j < 2 or big:
            note(cv, w * x, h * y, 'n', big)
    if r.random() < 0.5:
        mirror(cv)
    return cv, [('b', 'wall', 'Stage', BLUE, True), role('s', 'sax', 'Saxophone', BROWN), role('e', 'bell', 'Bell', BROWN),
                role('k', 'keys', 'Keys', BLUE), role('m', 'mouth', 'Mouthpiece', PINK), role('r', 'curtain', 'Curtains', PINK),
                role('n', 'notes', 'Music notes', GREEN), role('f', 'floor', 'Floor', GREEN)], ['things', 'music']


def headphones(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    fy = h * 0.86
    box(cv, 0, fy, w, h, 'f')
    hcx, hcy = cx, h * 0.54
    ro = w * 0.4
    t = 1.8 if not big else 2.6
    fill(cv, 'h', lambda px, py: py <= hcy and ro - t < math.hypot(px - hcx, py - hcy) <= ro)
    fill(cv, 'c', lambda px, py: py <= hcy - ro * 0.8 and ro - t - 1.0 < math.hypot(px - hcx, py - hcy) <= ro - t + 0.2)
    cw = 1.9 if not big else 2.6
    for side in (-1, 1):
        ux = hcx + side * (ro - t / 2)
        rbox(cv, ux - cw, hcy - 0.6, ux + cw, fy + 0.4, cw * 0.7, 'u')
        rbox(cv, ux - side * cw - 0.8, hcy + 0.2, ux - side * cw + 0.8, fy - 0.8, 0.5, 'c')
    path(cv, [(hcx - ro + t / 2, fy + 0.6), (hcx - ro * 0.3, fy + 1.2), (hcx + ro * 0.1, fy + 0.6), (hcx + ro * 0.5, fy + 1.4)], 'k', 0.45)
    for j, (x, y) in enumerate(((0.5, 0.42), (0.14, 0.12), (0.86, 0.1))):
        if j < 2 or big:
            note(cv, w * x, h * y, 'n', big)
    return cv, [('b', 'wall', 'Wall', BLUE, True), role('h', 'band', 'Headband', PINK), role('c', 'cushion', 'Cushions', BROWN),
                role('u', 'cup', 'Ear cups', PINK), role('k', 'cable', 'Cable', GREEN), role('n', 'notes', 'Music notes', GREEN),
                role('f', 'desk', 'Desk', BROWN)], ['things', 'music']


def light_bulb(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    box(cv, 0, 0, w, 0.9 if not big else 1.9, 'c')
    bx = int(cx) + 0.5
    gy, gr = h * 0.58, s * 0.3
    by0, by1 = h * 0.22, gy - gr * 0.72
    seg(cv, bx, 0, bx, by0, 'k', 0.5)
    for k in range(1, 8):
        a = math.radians(-90 + 45 * k)
        bar(cv, bx + math.cos(a) * (gr + 1.2), gy + math.sin(a) * (gr + 1.2), bx + math.cos(a) * (gr + 3.3), gy + math.sin(a) * (gr + 3.3), 1.0, 'y')
    disc(cv, bx, gy, gr, 'g')
    poly(cv, [(bx - 1.6, by1 - 0.5), (bx + 1.6, by1 - 0.5), (bx + gr * 0.8, gy - gr * 0.5), (bx - gr * 0.8, gy - gr * 0.5)], 'g')
    rbox(cv, bx - 1.8, by0, bx + 1.8, by1, 0.5, 'm')
    fill(cv, 'q', lambda px, py: by0 + 1.0 < py < by1 - 0.4 and int(py) % 2 == 0, only='m')
    path(cv, [(bx - 0.6, by1 + 0.6), (bx - 0.6, gy - 0.6)], 'p', 0.4)
    path(cv, [(bx + 0.6, by1 + 0.6), (bx + 0.6, gy - 0.6)], 'p', 0.4)
    path(cv, [(bx - 1.6, gy), (bx - 0.8, gy - 1.0), (bx, gy), (bx + 0.8, gy - 1.0), (bx + 1.6, gy)], 'p', 0.4)
    if big:
        mx, my = w * r.choice((0.16, 0.84)), h * 0.3
        for side in (-1, 1):
            oval(cv, mx + side * 1.2, my, 1.2, 0.9, 'o')
        seg(cv, mx, my - 1.0, mx, my + 1.2, 'p', 0.45)
    return cv, [('b', 'wall', 'Wall', BLUE, True), role('g', 'glass', 'Glass', BROWN), role('y', 'rays', 'Light rays', BROWN),
                role('p', 'filament', 'Filament', PINK), role('m', 'base', 'Base', PINK), role('q', 'thread', 'Thread', GREEN),
                role('k', 'cord', 'Cord', GREEN), role('c', 'ceiling', 'Ceiling', BLUE), role('o', 'moth', 'Moth wings', GREEN)], ['things', 'house']


def clip(cv, x, y, upright, c, bg, big):
    """A paper clip: a rounded loop with a slot, upright or lying, its top left cell at (x, y)."""
    a, b = (3, 5) if not big else (3, 7)
    if not upright:
        a, b = b, a
    rbox(cv, x, y, x + a, y + b, 0.8, c)
    box(cv, x + 1.0, y + 1.0, x + a - 1.0, y + b - 1.0, bg)


def magnet(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    fy = h * 0.88
    box(cv, 0, fy, w, h, 'f')
    mcx, mcy = cx, h * 0.3
    ro, ri = w * 0.36, w * 0.15
    a = math.radians(r.uniform(-12, 12))
    ca, sa = math.cos(a), math.sin(a)
    legs = h * 0.22

    def local(px, py):
        dx, dy = px - mcx, py - mcy
        return dx * ca + dy * sa, -dx * sa + dy * ca

    def u_shape(px, py):
        u, v = local(px, py)
        if v <= 0:
            return ri < math.hypot(u, v) <= ro
        return ri < abs(u) <= ro and v <= legs
    fill(cv, 'm', u_shape)
    fill(cv, 't', lambda px, py: local(px, py)[1] > legs - (2.0 if not big else 3.0), only='m')
    mid = (ro + ri) / 2
    for side in (-1, 1):
        for da in (-35, 0, 35):
            t = math.radians(90 + side * da)
            for d in (1.4, 2.4) if not big else (1.6, 2.6, 3.6):
                u, v = side * mid + math.cos(t) * d * side, legs + math.sin(t) * d
                cv.put(int(mcx + u * ca - v * sa), int(mcy + u * sa + v * ca), 'v')
    for k, x in enumerate((0.14, 0.62) if not big else (0.1, 0.34, 0.6)):
        clip(cv, int(w * x), int(fy) - 3, False, 'c', 'b', big)
    return cv, [('b', 'wall', 'Wall', BLUE, True), role('m', 'magnet', 'Magnet', PINK), role('t', 'tips', 'Tips', GREEN),
                role('v', 'field', 'Pull', BROWN), role('c', 'clips', 'Paper clips', PINK), role('f', 'table', 'Table', GREEN)], ['things', 'science']


def piggy_bank(w, h, r):
    cv, s, cx, big = start(w, h, 'b')
    fy = h * 0.86
    box(cv, 0, fy, w, h, 'f')
    pcx, pcy = cx - w * 0.04, h * 0.56
    rx, ry = w * 0.36, h * 0.2
    for dx in (-0.2, 0.18):
        rbox(cv, pcx + dx * w - 1.1, pcy, pcx + dx * w + 1.1, fy + 0.4, 0.4, 'p')
    oval(cv, pcx, pcy, rx, ry, 'p')
    poly(cv, [(pcx + rx * 0.2, pcy - ry * 0.8), (pcx + rx * 0.5, pcy - ry * 1.45), (pcx + rx * 0.62, pcy - ry * 0.7)], 'n')
    oval(cv, pcx + rx + 0.4, pcy + 0.2, 1.3 if not big else 1.8, 1.9 if not big else 2.6, 'n')
    for dy in (-0.6, 0.9) if not big else (-0.9, 1.3):
        cv.put(int(pcx + rx + 0.4), int(pcy + 0.2 + dy), 'k')
    cv.put(int(pcx + rx * 0.62), int(pcy - ry * 0.35), 'k')
    box(cv, pcx - 1.6, pcy - ry + 1.0, pcx + 1.0, pcy - ry + 1.9, 'k')
    path(cv, [(pcx - rx, pcy - 0.4), (pcx - rx - 1.2, pcy - 1.0), (pcx - rx - 1.0, pcy - 2.2), (pcx - rx - 0.2, pcy - 1.8)], 'p', 0.45)
    disc(cv, int(pcx) + 0.5, pcy - ry - 2.0, 1.0 if not big else 1.4, 'c')
    side = r.choice((-1, 1))
    for k in range(2 if not big else 3):
        disc(cv, int(cx + side * w * (0.3 + k * 0.08)) + 0.5, fy - 0.6, 1.0 if not big else 1.3, 'c')
    if big:
        heart(cv, pcx - rx * 0.3, pcy + ry * 0.25, 1.2, 'o')
    return cv, [('b', 'wall', 'Wall', BLUE, True), role('p', 'pig', 'Piggy bank', PINK), role('n', 'snout', 'Snout and ear', PINK),
                role('c', 'coins', 'Coins', BROWN), role('k', 'slot', 'Slot, eye and nostrils', BROWN), role('o', 'heart', 'Heart', BLUE),
                role('f', 'floor', 'Floor', GREEN)], ['things', 'house']


MORE_THINGS = [ambulance, taxi, race_car, dump_truck, excavator, cement_mixer, canoe, glider, parachute, roller_skate,
               surfboard, tennis_racket, basketball, backpack, desk_lamp, wheelbarrow, pinwheel, music_box, xylophone,
               accordion, saxophone, headphones, light_bulb, magnet, piggy_bank]

# Expansion roles of these subjects (as expansions.ROLES): subject -> {group: [(roleId, new name or None), ...]}.
MORE_THINGS_ROLES = {}

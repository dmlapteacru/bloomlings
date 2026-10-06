"""More base-picture subjects for the long run to Level 5000+ (the owner, 2026-10-06: "a house, a castle, a ship, a
boat, a ball: anything at all"). Same procedural style and rules as sketch_pictures.py: each subject draws on a Canvas
and returns (canvas, roles, themes); roles are (char, roleId, name, colorGroup, isBackground) in the four launch color
groups. Most have six or seven roles over three or four groups, so the later bands' six-variant levels can use them.
"""
import math

GREEN, PINK, BLUE, BROWN = 'green', 'pink_purple', 'blue_cyan', 'brown_orange'


def _ground(cv, c, rows=1):
    cv.rect(0, cv.h - rows, cv.w - 1, cv.h - 1, c)


def _sky(char='s', name='Sky'):
    return (char, 'sky', name, BLUE, True)


# ---- Buildings ----

def house(w, h, r):
    cv = cv0(w, h, 's')
    _ground(cv, 'g', 2)
    cv.rect(int(w * 0.18), int(h * 0.45), int(w * 0.82), h - 3, 'h')
    cv.poly([(w * 0.08, h * 0.47), (w * 0.5, h * 0.12), (w * 0.92, h * 0.47)], 'r')
    cv.rect(int(w * 0.66), int(h * 0.16), int(w * 0.74), int(h * 0.3), 'c')
    cv.rect(int(w * 0.26), int(h * 0.52), int(w * 0.4), int(h * 0.64), 'i')
    cv.rect(int(w * 0.56), int(h * 0.56), int(w * 0.7), h - 3, 'd')
    cv.ellipse(w * 0.12, h * 0.1, w * 0.1, h * 0.07, 'u')
    return cv, [_sky(), ('h', 'wall', 'Walls', BROWN, False), ('r', 'roof', 'Roof', PINK, False), ('c', 'chimney', 'Chimney', BROWN, False),
                ('i', 'window', 'Window', BLUE, False), ('d', 'door', 'Door', PINK, False), ('g', 'lawn', 'Lawn', GREEN, False), ('u', 'sun', 'Sun', BROWN, False)], ['buildings', 'cozy']


def castle(w, h, r):
    cv = cv0(w, h, 's')
    _ground(cv, 'g', 1)
    cv.rect(int(w * 0.12), int(h * 0.42), int(w * 0.88), h - 2, 'k')
    for x in (0.08, 0.72):
        cv.rect(int(w * x), int(h * 0.26), int(w * x) + max(2, int(w * 0.2)), h - 2, 't')
        cv.poly([(w * x - 0.4, h * 0.27), (w * x + w * 0.1 + 0.5, h * 0.06), (w * x + w * 0.2 + 1.4, h * 0.27)], 'r')
    for k in range(4):
        cv.rect(int(w * (0.3 + k * 0.12)), int(h * 0.36), int(w * (0.3 + k * 0.12)), int(h * 0.41), 'k')
    cv.ellipse(w * 0.5, h * 0.78, w * 0.12, h * 0.14, 'd')
    cv.rect(int(w * 0.38), int(h * 0.78), int(w * 0.62), h - 2, 'd')
    cv.rect(int(w * 0.46), int(h * 0.14), int(w * 0.48), int(h * 0.36), 'p')
    cv.rect(int(w * 0.49), int(h * 0.14), int(w * 0.6), int(h * 0.2), 'f')
    return cv, [_sky(), ('k', 'keep', 'Keep', BROWN, False), ('t', 'tower', 'Towers', BROWN, False), ('r', 'roof', 'Roofs', PINK, False),
                ('d', 'gate', 'Gate', BLUE, False), ('f', 'flag', 'Flag', PINK, False), ('p', 'pole', 'Pole', BROWN, False), ('g', 'grass', 'Grass', GREEN, False)], ['buildings', 'fairy']


def lighthouse(w, h, r):
    cv = cv0(w, h, 's')
    cv.rect(0, int(h * 0.8), w - 1, h - 1, 'w')
    cv.poly([(w * 0.2, h * 0.86), (w * 0.8, h * 0.86), (w * 0.9, h - 1), (w * 0.1, h - 1)], 'k')
    cv.poly([(w * 0.36, h * 0.3), (w * 0.64, h * 0.3), (w * 0.7, h * 0.86), (w * 0.3, h * 0.86)], 't')
    for y in (0.42, 0.62):
        cv.rect(int(w * 0.3), int(h * y), int(w * 0.7), int(h * y) + 1, 'b')
    cv.rect(int(w * 0.38), int(h * 0.18), int(w * 0.62), int(h * 0.29), 'l')
    cv.poly([(w * 0.32, h * 0.19), (w * 0.5, h * 0.06), (w * 0.68, h * 0.19)], 'b')
    cv.line(w * 0.66, h * 0.22, w * 0.98, h * 0.14, 'l', 0.4)
    return cv, [_sky(), ('t', 'tower', 'Tower', PINK, False), ('b', 'band', 'Bands', BROWN, False), ('l', 'light', 'Light', BROWN, False),
                ('k', 'rock', 'Rocks', GREEN, False), ('w', 'sea', 'Sea', BLUE, False)], ['buildings', 'sea']


def windmill(w, h, r):
    cv = cv0(w, h, 's')
    _ground(cv, 'g', 2)
    cv.poly([(w * 0.36, h * 0.35), (w * 0.64, h * 0.35), (w * 0.72, h - 2), (w * 0.28, h - 2)], 'm')
    cv.poly([(w * 0.32, h * 0.36), (w * 0.5, h * 0.22), (w * 0.68, h * 0.36)], 'r')
    cx, cy = w * 0.5, h * 0.3
    for a in (45, 135, 225, 315):
        t = math.radians(a)
        cv.line(cx, cy, cx + math.cos(t) * w * 0.44, cy + math.sin(t) * h * 0.26, 'b', 0.65)
    cv.rect(int(w * 0.45), int(h * 0.7), int(w * 0.55), h - 3, 'd')
    cv.ellipse(w * 0.12, h * 0.86, w * 0.1, h * 0.06, 'f')
    return cv, [_sky(), ('m', 'mill', 'Mill', BROWN, False), ('r', 'cap', 'Cap', PINK, False), ('b', 'blade', 'Blades', BROWN, False),
                ('d', 'door', 'Door', BLUE, False), ('f', 'flowers', 'Flowers', PINK, False), ('g', 'field', 'Field', GREEN, False)], ['buildings', 'farm']


def barn(w, h, r):
    cv = cv0(w, h, 's')
    _ground(cv, 'g', 2)
    cv.poly([(w * 0.12, h * 0.42), (w * 0.3, h * 0.22), (w * 0.7, h * 0.22), (w * 0.88, h * 0.42), (w * 0.88, h - 3), (w * 0.12, h - 3)], 'b')
    cv.poly([(w * 0.08, h * 0.44), (w * 0.3, h * 0.2), (w * 0.7, h * 0.2), (w * 0.92, h * 0.44), (w * 0.86, h * 0.44), (w * 0.68, h * 0.26), (w * 0.32, h * 0.26), (w * 0.14, h * 0.44)], 'r')
    cv.rect(int(w * 0.36), int(h * 0.58), int(w * 0.64), h - 3, 'd')
    cv.line(w * 0.36, h * 0.58, w * 0.64, h - 3, 'x', 0.35)
    cv.line(w * 0.64, h * 0.58, w * 0.36, h - 3, 'x', 0.35)
    cv.rect(int(w * 0.44), int(h * 0.34), int(w * 0.56), int(h * 0.46), 'i')
    cv.ellipse(w * 0.1, h * 0.86, w * 0.1, h * 0.08, 'y')
    return cv, [_sky(), ('b', 'barn', 'Barn', PINK, False), ('r', 'trim', 'Roof trim', BROWN, False), ('d', 'door', 'Door', BROWN, False),
                ('x', 'beam', 'Beams', PINK, False), ('i', 'loft', 'Loft', BLUE, False), ('y', 'hay', 'Hay', BROWN, False), ('g', 'grass', 'Grass', GREEN, False)], ['buildings', 'farm']


def tent(w, h, r):
    cv = cv0(w, h, 'n')
    _ground(cv, 'g', 2)
    cv.poly([(w * 0.08, h - 3), (w * 0.5, h * 0.25), (w * 0.92, h - 3)], 't')
    cv.poly([(w * 0.38, h - 3), (w * 0.5, h * 0.55), (w * 0.62, h - 3)], 'd')
    cv.line(w * 0.5, h * 0.25, w * 0.5, h * 0.12, 'p', 0.4)
    cv.rect(int(w * 0.51), int(h * 0.12), int(w * 0.62), int(h * 0.17), 'f')
    for k in range(5):
        cv.put(int(w * (0.1 + k * 0.2)), int(h * (0.08 + (k % 2) * 0.1)), 'm')
    cv.ellipse(w * 0.84, h * 0.12, w * 0.08, h * 0.07, 'm')
    return cv, [('n', 'night', 'Night sky', BLUE, True), ('t', 'tent', 'Tent', PINK, False), ('d', 'door', 'Door', BROWN, False), ('p', 'pole', 'Pole', BROWN, False),
                ('f', 'flag', 'Flag', PINK, False), ('m', 'stars', 'Moon and stars', BROWN, False), ('g', 'meadow', 'Meadow', GREEN, False)], ['camping', 'night']


def tree_house(w, h, r):
    cv = cv0(w, h, 's')
    _ground(cv, 'g', 1)
    cv.rect(int(w * 0.44), int(h * 0.5), int(w * 0.56), h - 2, 't')
    cv.ellipse(w * 0.5, h * 0.3, w * 0.46, h * 0.24, 'c')
    cv.rect(int(w * 0.28), int(h * 0.42), int(w * 0.72), int(h * 0.6), 'h')
    cv.poly([(w * 0.22, h * 0.43), (w * 0.5, h * 0.28), (w * 0.78, h * 0.43)], 'r')
    cv.rect(int(w * 0.36), int(h * 0.47), int(w * 0.46), int(h * 0.55), 'i')
    cv.line(w * 0.7, h * 0.6, w * 0.82, h - 2, 'l', 0.4)
    return cv, [_sky(), ('c', 'leaves', 'Leaves', GREEN, False), ('t', 'trunk', 'Trunk', BROWN, False), ('h', 'hut', 'Hut', BROWN, False),
                ('r', 'roof', 'Roof', PINK, False), ('i', 'window', 'Window', BLUE, False), ('l', 'ladder', 'Ladder', PINK, False), ('g', 'grass', 'Grass', GREEN, False)], ['buildings', 'forest']


# ---- Vehicles ----

def ship(w, h, r):
    cv = cv0(w, h, 's')
    cv.rect(0, int(h * 0.74), w - 1, h - 1, 'w')
    cv.poly([(w * 0.04, h * 0.58), (w * 0.96, h * 0.58), (w * 0.82, h * 0.8), (w * 0.18, h * 0.8)], 'h')
    cv.rect(int(w * 0.22), int(h * 0.44), int(w * 0.74), int(h * 0.57), 'c')
    for k in range(3):
        cv.put(int(w * (0.3 + k * 0.16)), int(h * 0.5), 'i')
    cv.rect(int(w * 0.32), int(h * 0.24), int(w * 0.44), int(h * 0.43), 'f')
    cv.rect(int(w * 0.54), int(h * 0.28), int(w * 0.66), int(h * 0.43), 'f')
    cv.ellipse(w * 0.3, h * 0.12, w * 0.12, h * 0.06, 'k')
    cv.rect(int(w * 0.04), int(h * 0.62), int(w * 0.96), int(h * 0.64), 'b')
    return cv, [_sky(), ('h', 'hull', 'Hull', BROWN, False), ('b', 'stripe', 'Stripe', PINK, False), ('c', 'cabin', 'Cabin', PINK, False),
                ('i', 'portholes', 'Portholes', BLUE, False), ('f', 'funnel', 'Funnels', BROWN, False), ('k', 'smoke', 'Smoke', GREEN, False), ('w', 'sea', 'Sea', BLUE, False)], ['vehicles', 'sea']


def rowboat(w, h, r):
    cv = cv0(w, h, 's')
    cv.rect(0, int(h * 0.62), w - 1, h - 1, 'w')
    cv.poly([(w * 0.06, h * 0.5), (w * 0.94, h * 0.5), (w * 0.78, h * 0.72), (w * 0.22, h * 0.72)], 'b')
    cv.rect(int(w * 0.08), int(h * 0.5), int(w * 0.92), int(h * 0.53), 'm')
    cv.line(w * 0.18, h * 0.38, w * 0.04, h * 0.84, 'o', 0.45)
    cv.line(w * 0.82, h * 0.38, w * 0.96, h * 0.84, 'o', 0.45)
    for k in range(3):
        cv.line(w * (0.15 + k * 0.32), h * 0.9, w * (0.27 + k * 0.32), h * 0.9, 'v', 0.4)
    cv.ellipse(w * 0.82, h * 0.14, w * 0.1, h * 0.08, 'u')
    cv.ellipse(w * 0.22, h * 0.16, w * 0.14, h * 0.06, 'c')
    return cv, [_sky(), ('b', 'boat', 'Boat', BROWN, False), ('m', 'rim', 'Rim', PINK, False), ('o', 'oar', 'Oars', BROWN, False),
                ('w', 'lake', 'Lake', BLUE, False), ('v', 'wave', 'Waves', GREEN, False), ('u', 'sun', 'Sun', PINK, False), ('c', 'cloud', 'Cloud', GREEN, False)], ['vehicles', 'lake']


def car(w, h, r):
    cv = cv0(w, h, 's')
    _ground(cv, 'd', 2)
    cv.rect(int(w * 0.06), int(h * 0.5), int(w * 0.94), int(h * 0.74), 'c')
    cv.poly([(w * 0.22, h * 0.5), (w * 0.32, h * 0.3), (w * 0.68, h * 0.3), (w * 0.8, h * 0.5)], 'c')
    cv.poly([(w * 0.28, h * 0.48), (w * 0.35, h * 0.34), (w * 0.48, h * 0.34), (w * 0.48, h * 0.48)], 'i')
    cv.poly([(w * 0.54, h * 0.48), (w * 0.54, h * 0.34), (w * 0.66, h * 0.34), (w * 0.74, h * 0.48)], 'i')
    for x in (0.26, 0.74):
        cv.ellipse(w * x, h * 0.76, w * 0.11, h * 0.1, 't')
        cv.put(int(w * x), int(h * 0.76), 'm')
    cv.rect(int(w * 0.88), int(h * 0.54), int(w * 0.94), int(h * 0.6), 'l')
    return cv, [_sky(), ('c', 'body', 'Body', PINK, False), ('i', 'window', 'Windows', BLUE, False), ('t', 'tyre', 'Tyres', BROWN, False),
                ('m', 'hub', 'Hubs', GREEN, False), ('l', 'lamp', 'Lamp', BROWN, False), ('d', 'road', 'Road', GREEN, False)], ['vehicles', 'town']


def bus(w, h, r):
    cv = cv0(w, h, 's')
    _ground(cv, 'd', 2)
    cv.rect(int(w * 0.04), int(h * 0.3), int(w * 0.96), int(h * 0.76), 'b')
    for k in range(4):
        x = int(w * (0.08 + k * 0.2))
        cv.rect(x, int(h * 0.36), x + max(1, int(w * 0.14)), int(h * 0.5), 'i')
    cv.rect(int(w * 0.04), int(h * 0.56), int(w * 0.96), int(h * 0.6), 'z')
    for x in (0.22, 0.78):
        cv.ellipse(w * x, h * 0.78, w * 0.1, h * 0.09, 't')
    cv.rect(int(w * 0.86), int(h * 0.62), int(w * 0.94), int(h * 0.7), 'l')
    return cv, [_sky(), ('b', 'bus', 'Bus', BROWN, False), ('i', 'window', 'Windows', BLUE, False), ('z', 'stripe', 'Stripe', PINK, False),
                ('t', 'wheel', 'Wheels', BROWN, False), ('l', 'light', 'Light', PINK, False), ('d', 'road', 'Road', GREEN, False)], ['vehicles', 'town']


def train(w, h, r):
    cv = cv0(w, h, 's')
    _ground(cv, 'g', 1)
    cv.rect(0, h - 3, w - 1, h - 3, 'k')
    cv.rect(int(w * 0.08), int(h * 0.44), int(w * 0.62), int(h * 0.78), 'e')
    cv.rect(int(w * 0.6), int(h * 0.26), int(w * 0.92), int(h * 0.78), 'c')
    cv.rect(int(w * 0.66), int(h * 0.32), int(w * 0.86), int(h * 0.46), 'i')
    cv.rect(int(w * 0.16), int(h * 0.24), int(w * 0.28), int(h * 0.43), 'f')
    cv.ellipse(w * 0.2, h * 0.12, w * 0.14, h * 0.07, 'm')
    for x in (0.2, 0.44, 0.76):
        cv.ellipse(w * x, h * 0.82, w * 0.09, h * 0.08, 't')
    return cv, [_sky(), ('e', 'boiler', 'Boiler', PINK, False), ('c', 'cab', 'Cab', BROWN, False), ('i', 'window', 'Window', BLUE, False),
                ('f', 'funnel', 'Funnel', BROWN, False), ('m', 'steam', 'Steam', GREEN, False), ('t', 'wheel', 'Wheels', PINK, False), ('k', 'rail', 'Rails', BROWN, False), ('g', 'grass', 'Grass', GREEN, False)], ['vehicles', 'travel']


def tractor(w, h, r):
    cv = cv0(w, h, 's')
    _ground(cv, 'f', 2)
    cv.rect(int(w * 0.18), int(h * 0.42), int(w * 0.62), int(h * 0.7), 'b')
    cv.rect(int(w * 0.46), int(h * 0.2), int(w * 0.78), int(h * 0.56), 'c')
    cv.rect(int(w * 0.52), int(h * 0.26), int(w * 0.72), int(h * 0.42), 'i')
    cv.rect(int(w * 0.24), int(h * 0.26), int(w * 0.3), int(h * 0.41), 'x')
    cv.ellipse(w * 0.68, h * 0.72, w * 0.18, h * 0.16, 't')
    cv.ellipse(w * 0.68, h * 0.72, w * 0.06, h * 0.05, 'h')
    cv.ellipse(w * 0.22, h * 0.78, w * 0.11, h * 0.1, 't')
    return cv, [_sky(), ('b', 'body', 'Body', GREEN, False), ('c', 'cab', 'Cab', GREEN, False), ('i', 'window', 'Window', BLUE, False),
                ('x', 'exhaust', 'Exhaust', BROWN, False), ('t', 'tyre', 'Tyres', BROWN, False), ('h', 'hub', 'Hub', PINK, False), ('f', 'field', 'Field', BROWN, False)], ['vehicles', 'farm']


def airplane(w, h, r):
    cv = cv0(w, h, 's')
    cv.ellipse(w * 0.5, h * 0.52, w * 0.44, h * 0.1, 'p')
    cv.poly([(w * 0.42, h * 0.5), (w * 0.62, h * 0.5), (w * 0.5, h * 0.18), (w * 0.4, h * 0.18)], 'g')
    cv.poly([(w * 0.42, h * 0.54), (w * 0.62, h * 0.54), (w * 0.5, h * 0.84), (w * 0.4, h * 0.84)], 'g')
    cv.poly([(w * 0.06, h * 0.5), (w * 0.12, h * 0.3), (w * 0.2, h * 0.3), (w * 0.2, h * 0.5)], 't')
    for k in range(4):
        cv.put(int(w * (0.3 + k * 0.12)), int(h * 0.5), 'i')
    cv.ellipse(w * 0.8, h * 0.2, w * 0.14, h * 0.06, 'c')
    cv.ellipse(w * 0.2, h * 0.86, w * 0.16, h * 0.06, 'c')
    return cv, [_sky(), ('p', 'plane', 'Plane', PINK, False), ('g', 'wing', 'Wings', BROWN, False), ('t', 'tail', 'Tail', BROWN, False),
                ('i', 'window', 'Windows', BLUE, False), ('c', 'cloud', 'Clouds', GREEN, False)], ['vehicles', 'sky']


def hot_air_balloon(w, h, r):
    cv = cv0(w, h, 's')
    cv.ellipse(w * 0.5, h * 0.34, w * 0.4, h * 0.28, 'a')
    for x in (0.36, 0.64):
        cv.line(w * x, h * 0.08, w * x, h * 0.6, 'b', 0.6)
    cv.poly([(w * 0.3, h * 0.56), (w * 0.7, h * 0.56), (w * 0.56, h * 0.7), (w * 0.44, h * 0.7)], 'a')
    cv.line(w * 0.42, h * 0.7, w * 0.44, h * 0.8, 'k', 0.35)
    cv.line(w * 0.58, h * 0.7, w * 0.56, h * 0.8, 'k', 0.35)
    cv.rect(int(w * 0.4), int(h * 0.8), int(w * 0.6), int(h * 0.9), 'k')
    cv.ellipse(w * 0.14, h * 0.86, w * 0.14, h * 0.06, 'c')
    cv.ellipse(w * 0.88, h * 0.62, w * 0.12, h * 0.05, 'c')
    return cv, [_sky(), ('a', 'balloon', 'Balloon', PINK, False), ('b', 'band', 'Bands', BROWN, False), ('k', 'basket', 'Basket', BROWN, False),
                ('c', 'cloud', 'Clouds', GREEN, False)], ['vehicles', 'sky']


def rocket(w, h, r):
    cv = cv0(w, h, 'n')
    cv.poly([(w * 0.5, h * 0.06), (w * 0.66, h * 0.28), (w * 0.66, h * 0.7), (w * 0.34, h * 0.7), (w * 0.34, h * 0.28)], 'b')
    cv.poly([(w * 0.5, h * 0.06), (w * 0.66, h * 0.26), (w * 0.34, h * 0.26)], 'c')
    cv.ellipse(w * 0.5, h * 0.4, w * 0.09, h * 0.06, 'i')
    cv.poly([(w * 0.34, h * 0.5), (w * 0.18, h * 0.76), (w * 0.34, h * 0.7)], 'f')
    cv.poly([(w * 0.66, h * 0.5), (w * 0.82, h * 0.76), (w * 0.66, h * 0.7)], 'f')
    cv.poly([(w * 0.38, h * 0.72), (w * 0.62, h * 0.72), (w * 0.5, h * 0.95)], 'x')
    for k in range(6):
        cv.put(int(w * (0.08 + (k * 0.37) % 0.9)), int(h * (0.1 + (k * 0.29) % 0.8)), 'm')
    return cv, [('n', 'space', 'Space', PINK, True), ('b', 'body', 'Body', BLUE, False), ('c', 'nose', 'Nose', PINK, False), ('i', 'window', 'Window', BLUE, False),
                ('f', 'fin', 'Fins', BROWN, False), ('x', 'flame', 'Flame', BROWN, False), ('m', 'stars', 'Stars', GREEN, False)], ['vehicles', 'space']


def submarine(w, h, r):
    cv = cv0(w, h, 'w')
    _ground(cv, 'd', 1)
    cv.ellipse(w * 0.48, h * 0.55, w * 0.42, h * 0.16, 'b')
    cv.rect(int(w * 0.4), int(h * 0.3), int(w * 0.58), int(h * 0.42), 't')
    cv.line(w * 0.52, h * 0.3, w * 0.52, h * 0.16, 'p', 0.35)
    for k in range(3):
        cv.ellipse(w * (0.3 + k * 0.18), h * 0.55, w * 0.05, h * 0.04, 'i')
    cv.poly([(w * 0.88, h * 0.55), (w * 0.98, h * 0.42), (w * 0.98, h * 0.68)], 't')
    for k in range(4):
        cv.put(int(w * (0.16 + k * 0.2)), int(h * (0.18 - (k % 2) * 0.06)), 'o')
    cv.line(w * 0.1, h * 0.95, w * 0.14, h * 0.74, 'k', 0.45)
    return cv, [('w', 'water', 'Deep water', BLUE, True), ('b', 'hull', 'Hull', BROWN, False), ('t', 'tower', 'Tower', BROWN, False), ('p', 'periscope', 'Periscope', PINK, False),
                ('i', 'portholes', 'Portholes', BLUE, False), ('o', 'bubbles', 'Bubbles', PINK, False), ('k', 'weed', 'Weed', GREEN, False), ('d', 'sand', 'Sand', BROWN, False)], ['vehicles', 'sea']


def helicopter(w, h, r):
    cv = cv0(w, h, 's')
    cv.ellipse(w * 0.4, h * 0.5, w * 0.26, h * 0.16, 'b')
    cv.ellipse(w * 0.3, h * 0.46, w * 0.1, h * 0.08, 'i')
    cv.rect(int(w * 0.6), int(h * 0.46), int(w * 0.9), int(h * 0.52), 'b')
    cv.rect(int(w * 0.86), int(h * 0.36), int(w * 0.92), int(h * 0.56), 't')
    cv.rect(int(w * 0.38), int(h * 0.26), int(w * 0.42), int(h * 0.34), 'm')
    cv.rect(int(w * 0.04), int(h * 0.24), int(w * 0.8), int(h * 0.26), 'r')
    cv.rect(int(w * 0.22), int(h * 0.72), int(w * 0.62), int(h * 0.74), 'k')
    cv.line(w * 0.3, h * 0.64, w * 0.28, h * 0.72, 'k', 0.35)
    cv.line(w * 0.52, h * 0.64, w * 0.54, h * 0.72, 'k', 0.35)
    _ground(cv, 'g', 1)
    return cv, [_sky(), ('b', 'body', 'Body', PINK, False), ('i', 'window', 'Window', BLUE, False), ('t', 'tail', 'Tail rotor', BROWN, False),
                ('m', 'mast', 'Mast', BROWN, False), ('r', 'rotor', 'Rotor', BROWN, False), ('k', 'skid', 'Skids', PINK, False), ('g', 'grass', 'Grass', GREEN, False)], ['vehicles', 'sky']


def bicycle(w, h, r):
    cv = cv0(w, h, 's')
    _ground(cv, 'g', 2)
    for x in (0.24, 0.76):
        cv.ellipse(w * x, h * 0.66, w * 0.2, h * 0.17, 'w')
        cv.ellipse(w * x, h * 0.66, w * 0.12, h * 0.1, 's')
        cv.put(int(w * x), int(h * 0.66), 'h')
    cv.line(w * 0.24, h * 0.66, w * 0.46, h * 0.44, 'f', 0.45)
    cv.line(w * 0.46, h * 0.44, w * 0.7, h * 0.44, 'f', 0.45)
    cv.line(w * 0.7, h * 0.44, w * 0.76, h * 0.66, 'f', 0.45)
    cv.line(w * 0.46, h * 0.44, w * 0.5, h * 0.66, 'f', 0.45)
    cv.rect(int(w * 0.38), int(h * 0.36), int(w * 0.5), int(h * 0.39), 'e')
    cv.line(w * 0.7, h * 0.44, w * 0.66, h * 0.3, 'e', 0.4)
    cv.ellipse(w * 0.84, h * 0.12, w * 0.1, h * 0.07, 'u')
    return cv, [_sky(), ('w', 'tyre', 'Tyres', BROWN, False), ('h', 'hub', 'Hubs', PINK, False), ('f', 'frame', 'Frame', PINK, False),
                ('e', 'seat', 'Seat and bars', BROWN, False), ('u', 'sun', 'Sun', BROWN, False), ('g', 'path', 'Path', GREEN, False)], ['vehicles', 'park']


# ---- Toys and play ----

def beach_ball(w, h, r):
    cv = cv0(w, h, 's')
    cv.rect(0, int(h * 0.76), w - 1, h - 1, 'd')
    cv.rect(0, int(h * 0.62), w - 1, int(h * 0.75), 'w')
    cx, cy, rx, ry = w * 0.5, h * 0.46, w * 0.36, h * 0.27
    for y in range(h):
        for x in range(w):
            dx, dy = (x + 0.5 - cx) / rx, (y + 0.5 - cy) / ry
            if dx * dx + dy * dy <= 1.0:
                a = (math.degrees(math.atan2(dy, dx)) + 360) % 360
                cv.put(x, y, 'abcabc'[int(a // 60)])
    cv.ellipse(cx, cy, w * 0.07, h * 0.05, 'c')
    cv.ellipse(w * 0.12, h * 0.12, w * 0.1, h * 0.07, 'u')
    return cv, [_sky(), ('a', 'red', 'Red panels', PINK, False), ('b', 'yellow', 'Yellow panels', BROWN, False), ('c', 'green', 'Green panels', GREEN, False),
                ('w', 'sea', 'Sea', BLUE, False), ('d', 'sand', 'Sand', BROWN, False), ('u', 'sun', 'Sun', PINK, False)], ['toys', 'beach']


def kite(w, h, r):
    cv = cv0(w, h, 's')
    _ground(cv, 'g', 2)
    cx, cy = w * 0.56, h * 0.3
    cv.poly([(cx, h * 0.06), (cx + w * 0.26, cy), (cx, h * 0.56), (cx - w * 0.26, cy)], 'k')
    cv.poly([(cx, h * 0.06), (cx + w * 0.26, cy), (cx, cy)], 'q')
    cv.poly([(cx, h * 0.56), (cx - w * 0.26, cy), (cx, cy)], 'q')
    cv.line(cx, h * 0.56, w * 0.3, h * 0.78, 't', 0.35)
    cv.line(w * 0.3, h * 0.78, w * 0.14, h - 3, 't', 0.35)
    for k in range(3):
        cv.put(int(w * (0.46 - k * 0.07)), int(h * (0.62 + k * 0.06)), 'b')
    cv.ellipse(w * 0.14, h * 0.18, w * 0.12, h * 0.05, 'c')
    return cv, [_sky(), ('k', 'kite', 'Kite', PINK, False), ('q', 'panel', 'Panels', BROWN, False), ('t', 'string', 'String', BROWN, False),
                ('b', 'bows', 'Bows', PINK, False), ('c', 'cloud', 'Cloud', GREEN, False), ('g', 'hill', 'Hill', GREEN, False)], ['toys', 'park']


def teddy_bear(w, h, r):
    cv = cv0(w, h, 'p')
    cv.rect(0, int(h * 0.88), w - 1, h - 1, 'f')
    cv.ellipse(w * 0.5, h * 0.66, w * 0.3, h * 0.22, 'b')
    cv.ellipse(w * 0.5, h * 0.34, w * 0.24, h * 0.18, 'b')
    for x in (0.28, 0.72):
        cv.ellipse(w * x, h * 0.18, w * 0.09, h * 0.07, 'b')
    cv.ellipse(w * 0.5, h * 0.42, w * 0.1, h * 0.07, 'm')
    cv.put(int(w * 0.5), int(h * 0.4), 'n')
    cv.ellipse(w * 0.5, h * 0.68, w * 0.14, h * 0.12, 'm')
    cv.rect(int(w * 0.4), int(h * 0.5), int(w * 0.6), int(h * 0.53), 'r')
    return cv, [('p', 'wall', 'Wall', BLUE, True), ('b', 'bear', 'Bear', BROWN, False), ('m', 'muzzle', 'Muzzle and belly', BROWN, False), ('n', 'nose', 'Nose', BROWN, False),
                ('r', 'ribbon', 'Ribbon', PINK, False), ('f', 'floor', 'Floor', GREEN, False)], ['toys', 'cozy']


def rubber_duck(w, h, r):
    cv = cv0(w, h, 'w')
    cv.rect(0, int(h * 0.78), w - 1, h - 1, 't')
    cv.ellipse(w * 0.46, h * 0.62, w * 0.38, h * 0.18, 'y')
    cv.ellipse(w * 0.62, h * 0.34, w * 0.2, h * 0.16, 'y')
    cv.poly([(w * 0.8, h * 0.36), (w * 0.98, h * 0.4), (w * 0.8, h * 0.44)], 'b')
    cv.put(int(w * 0.66), int(h * 0.3), 'e')
    cv.ellipse(w * 0.36, h * 0.58, w * 0.14, h * 0.07, 'g')
    for k in range(4):
        cv.ellipse(w * (0.12 + k * 0.26), h * 0.12, w * 0.06, h * 0.05, 'o')
    return cv, [('w', 'bath', 'Bath tiles', BLUE, True), ('y', 'duck', 'Duck', BROWN, False), ('b', 'beak', 'Beak', BROWN, False), ('e', 'eye', 'Eye', PINK, False),
                ('g', 'wing', 'Wing', BROWN, False), ('o', 'bubbles', 'Bubbles', PINK, False), ('t', 'water', 'Water', GREEN, False)], ['toys', 'bath']


def sandcastle(w, h, r):
    cv = cv0(w, h, 's')
    cv.rect(0, int(h * 0.82), w - 1, h - 1, 'd')
    cv.rect(int(w * 0.12), int(h * 0.5), int(w * 0.88), int(h * 0.82), 'c')
    cv.rect(int(w * 0.32), int(h * 0.28), int(w * 0.68), int(h * 0.5), 'c')
    for x in (0.12, 0.42, 0.72):
        cv.rect(int(w * x), int(h * 0.44), int(w * x) + 1, int(h * 0.49), 'c')
    cv.rect(int(w * 0.44), int(h * 0.62), int(w * 0.56), int(h * 0.82), 'g')
    cv.line(w * 0.5, h * 0.28, w * 0.5, h * 0.1, 'p', 0.35)
    cv.poly([(w * 0.52, h * 0.1), (w * 0.7, h * 0.14), (w * 0.52, h * 0.18)], 'f')
    cv.ellipse(w * 0.9, h * 0.9, w * 0.08, h * 0.06, 'h')
    cv.rect(0, int(h * 0.72), int(w * 0.08), int(h * 0.81), 'w')
    return cv, [_sky(), ('c', 'castle', 'Sand castle', BROWN, False), ('g', 'gate', 'Gate', BROWN, False), ('p', 'pole', 'Stick', PINK, False),
                ('f', 'flag', 'Flag', PINK, False), ('h', 'shell', 'Shell', PINK, False), ('w', 'sea', 'Sea', BLUE, False), ('d', 'beach', 'Beach', GREEN, False)], ['toys', 'beach']


def robot(w, h, r):
    cv = cv0(w, h, 'b')
    cv.rect(0, int(h * 0.92), w - 1, h - 1, 'f')
    cv.rect(int(w * 0.28), int(h * 0.16), int(w * 0.72), int(h * 0.4), 'm')
    cv.rect(int(w * 0.36), int(h * 0.22), int(w * 0.42), int(h * 0.28), 'e')
    cv.rect(int(w * 0.58), int(h * 0.22), int(w * 0.64), int(h * 0.28), 'e')
    cv.rect(int(w * 0.4), int(h * 0.33), int(w * 0.6), int(h * 0.35), 'k')
    cv.line(w * 0.5, h * 0.16, w * 0.5, h * 0.06, 'k', 0.35)
    cv.rect(int(w * 0.22), int(h * 0.44), int(w * 0.78), int(h * 0.74), 'm')
    cv.rect(int(w * 0.38), int(h * 0.5), int(w * 0.62), int(h * 0.64), 'p')
    cv.rect(int(w * 0.08), int(h * 0.46), int(w * 0.18), int(h * 0.66), 'a')
    cv.rect(int(w * 0.82), int(h * 0.46), int(w * 0.92), int(h * 0.66), 'a')
    cv.rect(int(w * 0.3), int(h * 0.76), int(w * 0.42), int(h * 0.91), 'a')
    cv.rect(int(w * 0.58), int(h * 0.76), int(w * 0.7), int(h * 0.91), 'a')
    return cv, [('b', 'wall', 'Wall', PINK, True), ('m', 'metal', 'Body', BLUE, False), ('e', 'eye', 'Eyes', BROWN, False), ('k', 'mouth', 'Mouth and aerial', BROWN, False),
                ('p', 'panel', 'Panel', GREEN, False), ('a', 'limb', 'Arms and legs', BLUE, False), ('f', 'floor', 'Floor', BROWN, False)], ['toys']


def cv0(w, h, fill):
    from sketch_pictures import Canvas
    return Canvas(w, h, fill)


WORLD_SUBJECTS = [house, castle, lighthouse, windmill, barn, tent, tree_house, ship, rowboat, car, bus, train, tractor,
                  airplane, hot_air_balloon, rocket, submarine, helicopter, bicycle, beach_ball, kite, teddy_bear,
                  rubber_duck, sandcastle, robot]

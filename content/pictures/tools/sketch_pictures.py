#!/usr/bin/env python3
"""Procedural sketches of garden-world base pictures (T094).

Writes content/pictures/src/<id>.grid.txt and <id>.meta.json for `pictures import`. Every sidecar is a draft, and
`pictures import` approves each one that passes the automated picture checks (PictureChecks; FR-084 as amended on
2026-10-06: the owner approved the procedural style, so no person reviews each picture). Re-running the script with the
same seed rewrites the same files; it never touches pictures whose sidecar is already approved.

Roles use only the four launch color groups (green, pink_purple, blue_cyan, brown_orange) so that every picture can be
mapped with launch variants (FR-006). Bands (the Level Band Guidelines as amended on 2026-10-05, bigger boards from
Level 1): early 12 x 12-13 (L11-25), early_mid 12-13 x 13-14 (L26-50), core 13-14 x 14-16 (L51-100). Every subject has
at least five color roles, so the core band, whose levels need five variants, can use any of them.
"""
import json
import math
import os
import random
import sys

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'src')

GREEN, PINK, BLUE, BROWN = 'green', 'pink_purple', 'blue_cyan', 'brown_orange'

# Version 2 since 2026-10-05: the sketches were redrawn at the bigger band sizes.
VERSION = 2

# The smallest pod (BandGuidelines.MinPodSize): every color role keeps at least this many cells, so each variant a
# mapping gives it can fill a pod.
MIN_ROLE_CELLS = 5


class Canvas:
    def __init__(self, w, h, fill):
        self.w, self.h = w, h
        self.g = [[fill] * w for _ in range(h)]  # top row first

    def put(self, x, y, c):
        if 0 <= x < self.w and 0 <= y < self.h:
            self.g[y][x] = c

    def rect(self, x0, y0, x1, y1, c):
        for y in range(max(0, y0), min(self.h, y1 + 1)):
            for x in range(max(0, x0), min(self.w, x1 + 1)):
                self.g[y][x] = c

    def ellipse(self, cx, cy, rx, ry, c):
        for y in range(self.h):
            for x in range(self.w):
                if ((x + 0.5 - cx) / rx) ** 2 + ((y + 0.5 - cy) / ry) ** 2 <= 1.0:
                    self.g[y][x] = c

    def poly(self, pts, c):
        for y in range(self.h):
            for x in range(self.w):
                px, py, inside = x + 0.5, y + 0.5, False
                j = len(pts) - 1
                for i in range(len(pts)):
                    xi, yi = pts[i]
                    xj, yj = pts[j]
                    if (yi > py) != (yj > py) and px < (xj - xi) * (py - yi) / (yj - yi) + xi:
                        inside = not inside
                    j = i
                if inside:
                    self.g[y][x] = c

    def line(self, x0, y0, x1, y1, c, t=0.6):
        for y in range(self.h):
            for x in range(self.w):
                px, py = x + 0.5, y + 0.5
                dx, dy = x1 - x0, y1 - y0
                L = dx * dx + dy * dy
                u = 0 if L == 0 else max(0, min(1, ((px - x0) * dx + (py - y0) * dy) / L))
                if math.hypot(px - (x0 + u * dx), py - (y0 + u * dy)) <= t:
                    self.g[y][x] = c

    def holes(self, bg, rng, target=0.88):
        """Open cells in the background: corners first, then random background cells, down to the target."""
        for (x, y) in [(0, 0), (self.w - 1, 0), (0, self.h - 1), (self.w - 1, self.h - 1)]:
            if self.g[y][x] == bg:
                self.g[y][x] = '.'
        cells = [(x, y) for y in range(self.h) for x in range(self.w) if self.g[y][x] == bg and y < self.h - 1]
        rng.shuffle(cells)
        while self.occupancy() > target and cells:
            x, y = cells.pop()
            self.g[y][x] = '.'

    def grow_small_roles(self, bg, minimum):
        """Grows every role under `minimum` cells (a variant that small cannot make a pod, BandGuidelines.MinPodSize):
        it takes the nearest neighbouring cells, background first, then those of roles with cells to spare."""
        def counts():
            out = {}
            for row in self.g:
                for c in row:
                    out[c] = out.get(c, 0) + 1
            return out

        for role in sorted(c for c, n in counts().items() if c not in ('.', bg) and n < minimum):
            while True:
                n = counts()
                if n.get(role, 0) >= minimum:
                    break
                cells = [(x, y) for y in range(self.h) for x in range(self.w) if self.g[y][x] == role]
                cx = sum(x for x, _ in cells) / len(cells)
                cy = sum(y for _, y in cells) / len(cells)
                best = None
                for y in range(self.h):
                    for x in range(self.w):
                        c = self.g[y][x]
                        if c == role or c == '.' or (c != bg and n[c] <= minimum + 1):
                            continue
                        if not any(0 <= x + dx < self.w and 0 <= y + dy < self.h and self.g[y + dy][x + dx] == role
                                   for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                            continue
                        key = (0 if c == bg else 1, (x + 0.5 - cx - 0.5) ** 2 + (y + 0.5 - cy - 0.5) ** 2, y, x)
                        if best is None or key < best[0]:
                            best = (key, x, y)
                if best is None:
                    break
                self.g[best[2]][best[1]] = role

    def occupancy(self):
        return sum(1 for r in self.g for c in r if c != '.') / (self.w * self.h)

    def rows(self):
        return [''.join(r) for r in self.g]


def ground(cv, c, rows=1):
    cv.rect(0, cv.h - rows, cv.w - 1, cv.h - 1, c)


# ---- Subjects: each returns (canvas, roles, themes). Roles: (char, roleId, name, colorGroup, isBackground). ----

def flower_pot(w, h, r):
    cv = Canvas(w, h, 's')
    cx = w / 2
    cv.poly([(cx - w * 0.28, h * 0.62), (cx + w * 0.28, h * 0.62), (cx + w * 0.2, h - 1), (cx - w * 0.2, h - 1)], 'o')
    cv.line(cx, h * 0.62, cx, h * 0.38, 'l', 0.55)
    cv.ellipse(cx - w * 0.18, h * 0.5, w * 0.12, h * 0.06, 'l')
    cv.ellipse(cx, h * 0.26, w * 0.22 + r.random() * 0.4, h * 0.16, 'p')
    cv.ellipse(cx, h * 0.26, w * 0.08, h * 0.06, 'c')
    return cv, [('s', 'sky', 'Sky', BLUE, True), ('p', 'petal', 'Petals', PINK, False), ('c', 'center', 'Center', BROWN, False), ('l', 'leaf', 'Leaves', GREEN, False), ('o', 'pot', 'Pot', BROWN, False)], ['flowers', 'garden']


def daisy_field(w, h, r):
    cv = Canvas(w, h, 's')
    ground(cv, 'g', 2 + r.randint(0, 1))
    n = 2 + r.randint(0, 1)
    for i in range(n):
        x = (i + 0.5) * w / n + r.uniform(-0.5, 0.5)
        top = h * r.uniform(0.25, 0.45)
        cv.line(x, top, x, h - 1, 't', 0.5)
        cv.ellipse(x, top, 1.6, 1.4, 'p')
        cv.put(int(x), int(top), 'c')
    return cv, [('s', 'sky', 'Sky', BLUE, True), ('g', 'grass', 'Grass', GREEN, False), ('t', 'stem', 'Stems', GREEN, False), ('p', 'petal', 'Petals', PINK, False), ('c', 'center', 'Centers', BROWN, False)], ['flowers', 'garden']


def tulip_bed(w, h, r):
    cv = Canvas(w, h, 's')
    ground(cv, 'd', 2)
    n = max(2, w // 3)
    for i in range(n):
        x = (i + 0.5) * w / n
        top = h * r.uniform(0.3, 0.45)
        cv.line(x, top + 1, x, h - 2, 'l', 0.5)
        cv.poly([(x - 1.3, top - 1), (x - 0.2, top), (x + 1.3, top - 1), (x + 1.1, top + 1.2), (x - 1.1, top + 1.2)], 'p')
    cv.ellipse(w * 0.86, h * 0.1, w * 0.09, h * 0.07, 'u')
    return cv, [('s', 'sky', 'Sky', BLUE, True), ('p', 'tulip', 'Tulips', PINK, False), ('l', 'stem', 'Stems', GREEN, False), ('d', 'soil', 'Soil', BROWN, False), ('u', 'sun', 'Sun', BROWN, False)], ['flowers', 'garden']


def fruit_tree(w, h, r):
    cv = Canvas(w, h, 's')
    ground(cv, 'g')
    cv.rect(int(w / 2) - 1, int(h * 0.55), int(w / 2), h - 2, 't')
    cv.ellipse(w / 2, h * 0.35, w * 0.42, h * 0.3, 'c')
    for _ in range(3 + r.randint(0, 3)):
        cv.put(int(w / 2 + r.uniform(-w * 0.3, w * 0.3)), int(h * r.uniform(0.2, 0.5)), 'f')
    return cv, [('s', 'sky', 'Sky', BLUE, True), ('c', 'crown', 'Crown', GREEN, False), ('f', 'fruit', 'Fruit', PINK, False), ('t', 'trunk', 'Trunk', BROWN, False), ('g', 'grass', 'Grass', GREEN, False)], ['fruit', 'garden']


def apple(w, h, r):
    cv = Canvas(w, h, 'b')
    cv.rect(int(w * 0.08), int(h * 0.9), int(w * 0.92), h - 1, 'p')
    cv.ellipse(w / 2, h * 0.56, w * 0.4, h * 0.34, 'a')
    cv.line(w / 2, h * 0.24, w / 2 + 0.6, h * 0.1, 't', 0.5)
    cv.ellipse(w / 2 + w * 0.2, h * 0.14, w * 0.15, h * 0.06, 'l')
    return cv, [('b', 'background', 'Background', BLUE, True), ('a', 'apple', 'Apple', BROWN, False), ('t', 'stalk', 'Stalk', BROWN, False), ('l', 'leaf', 'Leaf', GREEN, False), ('p', 'plate', 'Plate', PINK, False)], ['fruit']


def pear(w, h, r):
    cv = Canvas(w, h, 'b')
    cv.rect(int(w * 0.08), int(h * 0.92), int(w * 0.92), h - 1, 'd')
    cv.ellipse(w / 2, h * 0.66, w * 0.38, h * 0.26, 'p')
    cv.ellipse(w / 2, h * 0.38, w * 0.22, h * 0.2, 'p')
    cv.line(w / 2, h * 0.2, w / 2, h * 0.08, 't', 0.5)
    cv.ellipse(w / 2 - w * 0.18, h * 0.12, w * 0.14, h * 0.05, 'l')
    return cv, [('b', 'background', 'Background', PINK, True), ('p', 'pear', 'Pear', GREEN, False), ('l', 'leaf', 'Leaf', GREEN, False), ('t', 'stalk', 'Stalk', BROWN, False), ('d', 'plate', 'Plate', BLUE, False)], ['fruit']


def cherries(w, h, r):
    cv = Canvas(w, h, 'b')
    cv.rect(int(w * 0.08), int(h * 0.93), int(w * 0.92), h - 1, 'p')
    cv.line(w * 0.3, h * 0.7, w * 0.55, h * 0.2, 't', 0.5)
    cv.line(w * 0.7, h * 0.72, w * 0.55, h * 0.2, 't', 0.5)
    cv.ellipse(w * 0.3, h * 0.72, w * 0.17, h * 0.15, 'c')
    cv.ellipse(w * 0.7, h * 0.74, w * 0.17, h * 0.15, 'c')
    cv.ellipse(w * 0.66, h * 0.16, w * 0.18, h * 0.07, 'l')
    return cv, [('b', 'background', 'Background', BLUE, True), ('c', 'cherry', 'Cherries', PINK, False), ('t', 'stalk', 'Stalks', GREEN, False), ('l', 'leaf', 'Leaf', GREEN, False), ('p', 'plate', 'Plate', BROWN, False)], ['fruit']


def strawberry(w, h, r):
    cv = Canvas(w, h, 'b')
    cv.rect(int(w * 0.08), int(h * 0.92), int(w * 0.92), h - 1, 'p')
    cv.poly([(w * 0.12, h * 0.3), (w * 0.88, h * 0.3), (w * 0.5, h * 0.93)], 's')
    cv.ellipse(w / 2, h * 0.34, w * 0.38, h * 0.12, 's')
    for i in range(4 + r.randint(0, 2)):
        cv.put(int(w * r.uniform(0.3, 0.7)), int(h * r.uniform(0.4, 0.7)), 'd')
    cv.poly([(w * 0.25, h * 0.2), (w * 0.5, h * 0.28), (w * 0.75, h * 0.2), (w * 0.5, h * 0.08)], 'l')
    return cv, [('b', 'background', 'Background', BLUE, True), ('s', 'berry', 'Berry', PINK, False), ('d', 'seed', 'Seeds', BROWN, False), ('l', 'leaves', 'Leaves', GREEN, False), ('p', 'plate', 'Plate', BROWN, False)], ['fruit']


def grapes(w, h, r):
    cv = Canvas(w, h, 'b')
    for row, n in enumerate([4, 3, 3, 2, 1]):
        for i in range(n):
            cv.ellipse(w / 2 + (i - (n - 1) / 2) * w * 0.18, h * (0.35 + row * 0.13), w * 0.1, h * 0.07, 'g')
    cv.line(w / 2, h * 0.3, w / 2 + 0.5, h * 0.1, 't', 0.5)
    cv.ellipse(w * 0.72, h * 0.16, w * 0.16, h * 0.07, 'l')
    cv.line(w * 0.18, h * 0.08, w * 0.36, h * 0.2, 'v', 0.45)
    return cv, [('b', 'background', 'Background', BLUE, True), ('g', 'grape', 'Grapes', PINK, False), ('t', 'stalk', 'Stalk', BROWN, False), ('l', 'leaf', 'Leaf', GREEN, False), ('v', 'vine', 'Vine', GREEN, False)], ['fruit']


def pumpkin(w, h, r):
    cv = Canvas(w, h, 's')
    ground(cv, 'g', 2)
    cv.ellipse(w / 2, h * 0.62, w * 0.44, h * 0.28, 'p')
    cv.line(w / 2, h * 0.36, w / 2, h * 0.9, 'r', 0.35)
    cv.rect(int(w / 2) - 1, int(h * 0.26), int(w / 2), int(h * 0.34), 't')
    cv.ellipse(w * 0.68, h * 0.27, w * 0.13, h * 0.05, 'f')
    return cv, [('s', 'sky', 'Sky', BLUE, True), ('p', 'pumpkin', 'Pumpkin', BROWN, False), ('r', 'rib', 'Ribs', BROWN, False), ('t', 'stem', 'Stem', GREEN, False), ('g', 'grass', 'Grass', GREEN, False), ('f', 'leaf', 'Leaf', GREEN, False)], ['fruit', 'garden']


def mushroom(w, h, r):
    cv = Canvas(w, h, 's')
    ground(cv, 'g', 2)
    cv.rect(int(w * 0.38), int(h * 0.45), int(w * 0.62), h - 2, 't')
    cv.ellipse(w / 2, h * 0.42, w * 0.46, h * 0.26, 'c')
    cv.rect(0, int(h * 0.42), w - 1, int(h * 0.44), 's')
    for _ in range(3):
        cv.put(int(w / 2 + r.uniform(-w * 0.3, w * 0.3)), int(h * r.uniform(0.22, 0.36)), 'd')
    return cv, [('s', 'sky', 'Sky', BLUE, True), ('c', 'cap', 'Cap', PINK, False), ('d', 'dot', 'Dots', PINK, False), ('t', 'stem', 'Stem', BROWN, False), ('g', 'grass', 'Grass', GREEN, False)], ['forest', 'garden']


def butterfly(w, h, r):
    cv = Canvas(w, h, 's')
    ground(cv, 'g', 1)
    cv.ellipse(w * 0.28, h * 0.35, w * 0.25, h * 0.22, 'w')
    cv.ellipse(w * 0.72, h * 0.35, w * 0.25, h * 0.22, 'w')
    cv.ellipse(w * 0.32, h * 0.68, w * 0.18, h * 0.16, 'w')
    cv.ellipse(w * 0.68, h * 0.68, w * 0.18, h * 0.16, 'w')
    for (x, y) in [(0.25, 0.33), (0.75, 0.33), (0.32, 0.68), (0.68, 0.68)]:
        cv.ellipse(w * x, h * y, w * 0.07, h * 0.06, 'd')
    cv.rect(int(w / 2) - (1 if w % 2 == 0 else 0), int(h * 0.2), int(w / 2), int(h * 0.85), 'b')
    return cv, [('s', 'sky', 'Sky', BLUE, True), ('w', 'wing', 'Wings', PINK, False), ('d', 'spot', 'Spots', PINK, False), ('b', 'body', 'Body', BROWN, False), ('g', 'grass', 'Grass', GREEN, False)], ['insects', 'garden']


def bee(w, h, r):
    cv = Canvas(w, h, 's')
    ground(cv, 'g', 1)
    cv.ellipse(w * 0.35, h * 0.3, w * 0.2, h * 0.16, 'i')
    cv.ellipse(w * 0.62, h * 0.3, w * 0.2, h * 0.16, 'i')
    cv.ellipse(w / 2, h * 0.6, w * 0.34, h * 0.24, 'y')
    for k in range(3):
        cv.rect(int(w * (0.3 + k * 0.17)), int(h * 0.4), int(w * (0.3 + k * 0.17)), int(h * 0.82), 'k')
    cv.ellipse(w * 0.12, h * 0.88, w * 0.1, h * 0.07, 'f')
    return cv, [('s', 'sky', 'Sky', BLUE, True), ('i', 'wing', 'Wings', BLUE, False), ('y', 'body', 'Body', BROWN, False), ('k', 'stripe', 'Stripes', BROWN, False), ('f', 'flower', 'Flower', PINK, False), ('g', 'grass', 'Grass', GREEN, False)], ['insects', 'garden']


def snail(w, h, r):
    cv = Canvas(w, h, 's')
    ground(cv, 'g', 2)
    cv.rect(int(w * 0.15), int(h * 0.7), int(w * 0.9), h - 3, 'b')
    cv.line(w * 0.85, h * 0.7, w * 0.92, h * 0.5, 'b', 0.45)
    cv.ellipse(w * 0.45, h * 0.52, w * 0.3, h * 0.26, 'h')
    cv.ellipse(w * 0.45, h * 0.52, w * 0.13, h * 0.11, 'c')
    return cv, [('s', 'sky', 'Sky', BLUE, True), ('h', 'shell', 'Shell', BROWN, False), ('c', 'spiral', 'Spiral', BROWN, False), ('b', 'body', 'Body', GREEN, False), ('g', 'grass', 'Grass', GREEN, False)], ['animals', 'garden']


def ladybug(w, h, r):
    cv = Canvas(w, h, 'l')
    cv.ellipse(w / 2, h * 0.55, w * 0.36, h * 0.32, 'b')
    cv.ellipse(w / 2, h * 0.24, w * 0.18, h * 0.1, 'h')
    cv.line(w / 2, h * 0.3, w / 2, h * 0.86, 'h', 0.4)
    for _ in range(4):
        cv.put(int(w / 2 + r.uniform(-w * 0.25, w * 0.25)), int(h * r.uniform(0.42, 0.75)), 'd')
    cv.ellipse(w * 0.86, h * 0.12, w * 0.08, h * 0.06, 'w')
    return cv, [('l', 'leaf', 'Leaf', GREEN, True), ('b', 'shell', 'Shell', PINK, False), ('d', 'dot', 'Dots', BROWN, False), ('h', 'head', 'Head', BROWN, False), ('w', 'dew', 'Dew drop', BLUE, False)], ['insects', 'garden']


def fish(w, h, r):
    cv = Canvas(w, h, 'w')
    ground(cv, 'd', 1)
    cv.ellipse(w * 0.45, h * 0.45, w * 0.3, h * 0.2, 'f')
    cv.poly([(w * 0.7, h * 0.45), (w * 0.95, h * 0.25), (w * 0.95, h * 0.65)], 't')
    for x in (0.12, 0.85):
        cv.line(w * x, h * 0.95, w * x + r.uniform(-1, 1), h * 0.6, 'k', 0.45)
    return cv, [('w', 'water', 'Water', BLUE, True), ('f', 'fish', 'Fish', BROWN, False), ('t', 'tail', 'Tail', BROWN, False), ('k', 'weed', 'Weed', GREEN, False), ('d', 'sand', 'Sand', BROWN, False)], ['animals', 'sea']


def bird(w, h, r):
    cv = Canvas(w, h, 's')
    cv.rect(0, int(h * 0.72), w - 1, int(h * 0.76), 'r')
    cv.ellipse(w * 0.45, h * 0.5, w * 0.26, h * 0.2, 'b')
    cv.ellipse(w * 0.62, h * 0.32, w * 0.14, h * 0.12, 'b')
    cv.poly([(w * 0.74, h * 0.3), (w * 0.9, h * 0.34), (w * 0.74, h * 0.38)], 'k')
    cv.ellipse(w * 0.4, h * 0.46, w * 0.14, h * 0.08, 'n')
    cv.ellipse(w * 0.15, h * 0.88, w * 0.2, h * 0.1, 'l')
    return cv, [('s', 'sky', 'Sky', BLUE, True), ('b', 'bird', 'Bird', BLUE, False), ('n', 'wing', 'Wing', PINK, False), ('k', 'beak', 'Beak', BROWN, False), ('r', 'branch', 'Branch', BROWN, False), ('l', 'leaves', 'Leaves', GREEN, False)], ['animals', 'garden']


def frog(w, h, r):
    cv = Canvas(w, h, 'w')
    cv.ellipse(w / 2, h * 0.7, w * 0.46, h * 0.16, 'p')
    cv.ellipse(w / 2, h * 0.52, w * 0.28, h * 0.2, 'f')
    cv.ellipse(w * 0.36, h * 0.32, w * 0.09, h * 0.08, 'f')
    cv.ellipse(w * 0.64, h * 0.32, w * 0.09, h * 0.08, 'f')
    cv.put(int(w * 0.8), int(h * 0.15), 'o')
    cv.ellipse(w * 0.82, h * 0.18, w * 0.08, h * 0.07, 'o')
    cv.line(w * 0.1, h * 0.95, w * 0.12, h * 0.35, 'r', 0.45)
    return cv, [('w', 'pond', 'Pond', BLUE, True), ('p', 'pad', 'Lily pad', GREEN, False), ('f', 'frog', 'Frog', GREEN, False), ('o', 'blossom', 'Blossom', PINK, False), ('r', 'reed', 'Reed', BROWN, False)], ['animals', 'pond']


def watering_can(w, h, r):
    cv = Canvas(w, h, 's')
    cv.rect(int(w * 0.2), int(h * 0.4), int(w * 0.7), int(h * 0.85), 'c')
    cv.line(w * 0.7, h * 0.55, w * 0.95, h * 0.3, 'c', 0.5)
    cv.line(w * 0.25, h * 0.4, w * 0.45, h * 0.2, 'h', 0.45)
    cv.line(w * 0.45, h * 0.2, w * 0.62, h * 0.4, 'h', 0.45)
    for k in range(3):
        cv.put(int(w * 0.9) + (k % 2), int(h * (0.45 + k * 0.12)), 'd')
    ground(cv, 'g', 1)
    return cv, [('s', 'sky', 'Sky', BLUE, True), ('c', 'can', 'Can', GREEN, False), ('h', 'handle', 'Handle', BROWN, False), ('d', 'drops', 'Drops', BLUE, False), ('g', 'grass', 'Grass', GREEN, False)], ['tools', 'garden']


def birdhouse(w, h, r):
    cv = Canvas(w, h, 's')
    ground(cv, 'g', 1)
    cv.rect(int(w / 2) - 1 + (w % 2), int(h * 0.62), int(w / 2), h - 2, 'p')
    cv.rect(int(w * 0.25), int(h * 0.3), int(w * 0.75), int(h * 0.62), 'h')
    cv.poly([(w * 0.15, h * 0.32), (w * 0.5, h * 0.08), (w * 0.85, h * 0.32)], 'r')
    cv.ellipse(w / 2, h * 0.45, 0.9, 0.9, '.')
    return cv, [('s', 'sky', 'Sky', BLUE, True), ('r', 'roof', 'Roof', PINK, False), ('h', 'house', 'House', BROWN, False), ('p', 'pole', 'Pole', BROWN, False), ('g', 'grass', 'Grass', GREEN, False)], ['cozy', 'garden']


def mug(w, h, r):
    cv = Canvas(w, h, 'b')
    cv.rect(int(w * 0.2), int(h * 0.4), int(w * 0.68), int(h * 0.9), 'm')
    cv.ellipse(w * 0.76, h * 0.62, w * 0.14, h * 0.14, 'm')
    cv.ellipse(w * 0.76, h * 0.62, w * 0.06, h * 0.06, 'b')
    cv.rect(int(w * 0.24), int(h * 0.42), int(w * 0.64), int(h * 0.48), 't')
    for k in range(2):
        cv.line(w * (0.35 + k * 0.2), h * 0.35, w * (0.4 + k * 0.2), h * 0.1, 'v', 0.4)
    cv.rect(0, int(h * 0.92), w - 1, h - 1, 'd')
    return cv, [('b', 'background', 'Background', GREEN, True), ('m', 'mug', 'Mug', PINK, False), ('t', 'tea', 'Tea', BROWN, False), ('v', 'steam', 'Steam', BLUE, False), ('d', 'table', 'Table', BROWN, False)], ['cozy']


def umbrella(w, h, r):
    cv = Canvas(w, h, 's')
    cv.ellipse(w / 2, h * 0.42, w * 0.46, h * 0.3, 'u')
    cv.rect(0, int(h * 0.42), w - 1, h - 1, 's')
    for k in range(1, 4):
        cv.line(w / 2, h * 0.14, w * k / 4, h * 0.42, 'v', 0.4)
    cv.line(w / 2, h * 0.42, w / 2, h * 0.85, 'h', 0.45)
    cv.line(w / 2, h * 0.85, w / 2 - 1.2, h * 0.85, 'h', 0.45)
    ground(cv, 'g', 1)
    return cv, [('s', 'sky', 'Sky', BLUE, True), ('u', 'canopy', 'Canopy', PINK, False), ('v', 'rib', 'Ribs', PINK, False), ('h', 'handle', 'Handle', BROWN, False), ('g', 'grass', 'Grass', GREEN, False)], ['cozy']


def cottage(w, h, r):
    cv = Canvas(w, h, 's')
    ground(cv, 'g', 2)
    cv.rect(int(w * 0.2), int(h * 0.45), int(w * 0.8), h - 3, 'h')
    cv.poly([(w * 0.1, h * 0.47), (w * 0.5, h * 0.15), (w * 0.9, h * 0.47)], 'r')
    cv.rect(int(w * 0.3), int(h * 0.55), int(w * 0.4), int(h * 0.65), 'i')
    cv.rect(int(w * 0.55), int(h * 0.6), int(w * 0.65), h - 3, 'd')
    return cv, [('s', 'sky', 'Sky', BLUE, True), ('r', 'roof', 'Roof', BROWN, False), ('h', 'wall', 'Walls', PINK, False), ('i', 'window', 'Window', BLUE, False), ('d', 'door', 'Door', BROWN, False), ('g', 'grass', 'Grass', GREEN, False)], ['cozy', 'garden']


def sailboat(w, h, r):
    cv = Canvas(w, h, 's')
    cv.rect(0, int(h * 0.7), w - 1, h - 1, 'w')
    cv.poly([(w * 0.2, h * 0.68), (w * 0.8, h * 0.68), (w * 0.7, h * 0.82), (w * 0.3, h * 0.82)], 'h')
    cv.poly([(w * 0.5, h * 0.1), (w * 0.5, h * 0.64), (w * 0.2, h * 0.64)], 'a')
    cv.poly([(w * 0.55, h * 0.2), (w * 0.55, h * 0.64), (w * 0.82, h * 0.64)], 'a')
    cv.ellipse(w * 0.88, h * 0.1, w * 0.08, h * 0.06, 'u')
    return cv, [('s', 'sky', 'Sky', BLUE, True), ('a', 'sail', 'Sails', PINK, False), ('h', 'hull', 'Hull', BROWN, False), ('w', 'sea', 'Sea', BLUE, False), ('u', 'sun', 'Sun', BROWN, False)], ['sea']


def cactus(w, h, r):
    cv = Canvas(w, h, 's')
    ground(cv, 'd', 1)
    cv.poly([(w * 0.25, h * 0.72), (w * 0.75, h * 0.72), (w * 0.68, h - 1), (w * 0.32, h - 1)], 'o')
    cv.rect(int(w * 0.42), int(h * 0.2), int(w * 0.58), int(h * 0.72), 'c')
    cv.rect(int(w * 0.22), int(h * 0.38), int(w * 0.3), int(h * 0.55), 'c')
    cv.rect(int(w * 0.3), int(h * 0.5), int(w * 0.42), int(h * 0.55), 'c')
    cv.rect(int(w * 0.7), int(h * 0.3), int(w * 0.78), int(h * 0.48), 'c')
    cv.ellipse(w / 2, h * 0.16, w * 0.1, h * 0.06, 'f')
    return cv, [('s', 'sky', 'Sky', BLUE, True), ('c', 'cactus', 'Cactus', GREEN, False), ('f', 'flower', 'Flower', PINK, False), ('o', 'pot', 'Pot', BROWN, False), ('d', 'sand', 'Sand', BROWN, False)], ['desert', 'garden']


def hedgehog(w, h, r):
    cv = Canvas(w, h, 's')
    ground(cv, 'g', 2)
    cv.ellipse(w * 0.45, h * 0.62, w * 0.36, h * 0.24, 'k')
    cv.poly([(w * 0.7, h * 0.5), (w * 0.95, h * 0.7), (w * 0.7, h * 0.82)], 'f')
    for k in range(4):
        cv.put(int(w * (0.2 + k * 0.14)), int(h * 0.38), 'k')
    cv.ellipse(w * 0.42, h * 0.36, w * 0.08, h * 0.06, 'a')
    return cv, [('s', 'sky', 'Sky', BLUE, True), ('k', 'spikes', 'Spikes', BROWN, False), ('f', 'face', 'Face', BROWN, False), ('g', 'grass', 'Grass', GREEN, False), ('a', 'apple', 'Apple', PINK, False)], ['animals', 'garden']


def owl(w, h, r):
    cv = Canvas(w, h, 'n')
    cv.rect(0, int(h * 0.82), w - 1, int(h * 0.86), 'r')
    cv.ellipse(w / 2, h * 0.5, w * 0.32, h * 0.32, 'o')
    cv.ellipse(w / 2, h * 0.6, w * 0.18, h * 0.2, 'b')
    cv.ellipse(w * 0.38, h * 0.36, w * 0.09, h * 0.08, 'e')
    cv.ellipse(w * 0.62, h * 0.36, w * 0.09, h * 0.08, 'e')
    cv.ellipse(w * 0.85, h * 0.12, w * 0.08, h * 0.07, 'm')
    return cv, [('n', 'night', 'Night sky', PINK, True), ('o', 'owl', 'Owl', BROWN, False), ('b', 'belly', 'Belly', BROWN, False), ('e', 'eye', 'Eyes', BLUE, False), ('r', 'branch', 'Branch', GREEN, False), ('m', 'moon', 'Moon', BLUE, False)], ['animals', 'night']


SUBJECTS = [flower_pot, daisy_field, tulip_bed, fruit_tree, apple, pear, cherries, strawberry, grapes, pumpkin, mushroom,
            butterfly, bee, snail, ladybug, fish, bird, frog, watering_can, birdhouse, mug, umbrella, cottage, sailboat,
            cactus, hedgehog, owl]

# Subjects whose roles can carry five distinct variants (at most two per color group): the core band's levels need five.
FIVE_VARIANTS = [s for s in SUBJECTS if s is not fish]

BANDS = [
    ('early', 16, [(12, 12), (12, 13)], SUBJECTS),
    ('early_mid', 26, [(12, 13), (13, 13), (12, 14), (13, 14)], SUBJECTS),
    ('core', 52, [(13, 14), (14, 14), (13, 15), (14, 15), (13, 16), (14, 16)], SUBJECTS),
    # Added 2026-10-05: Levels 51-100 use 50 distinct pictures with five variants, and the 52 above ran out by L93.
    ('core', 16, [(14, 15), (13, 16), (14, 16), (13, 15)], FIVE_VARIANTS),
]


def main(seed=2026):
    rng = random.Random(seed)
    os.makedirs(ROOT, exist_ok=True)
    counter = {}
    written = 0
    for band, count, sizes, subjects in BANDS:
        for i in range(count):
            subject = subjects[(written + i * 7) % len(subjects)]
            w, h = sizes[i % len(sizes)]
            r = random.Random(rng.randrange(1 << 30))
            cv, roles, themes = subject(w, h, r)
            bg = roles[0][0]
            cv.grow_small_roles(bg, MIN_ROLE_CELLS)
            cv.holes(bg, r, target=r.uniform(0.84, 0.92))
            used = {c for row in cv.rows() for c in row}
            roles = [role for role in roles if role[0] in used]
            name = subject.__name__
            counter[name] = counter.get(name, 0) + 1
            pid = f'{name}_{counter[name]:02d}'
            meta_path = os.path.join(ROOT, pid + '.meta.json')
            if os.path.exists(meta_path) and '"approved"' in open(meta_path).read():
                continue
            meta = {
                'id': pid, 'version': VERSION,
                'subject': name.replace('_', ' ').capitalize() + ' ' + str(counter[name]),
                'width': w, 'height': h,
                'legend': {role[0]: role[1] for role in roles},
                'roles': [dict({'roleId': role[1], 'name': role[2], 'colorGroup': role[3]}, **({'isBackground': True} if role[4] else {})) for role in roles],
                'finishedLook': {'mode': 'auto'},
                'tags': {'themes': themes, 'bands': [band]},
                'review': {'status': 'draft', 'notes': 'Procedural sketch (content/pictures/tools/sketch_pictures.py); approved on import by the automated picture checks (FR-084 as amended).'},
                'source': {'kind': 'generated', 'origin': 'content/pictures/tools/sketch_pictures.py', 'licence': 'owned'},
            }
            with open(meta_path, 'w') as f:
                json.dump(meta, f, indent=2, sort_keys=True)
                f.write('\n')
            with open(os.path.join(ROOT, pid + '.grid.txt'), 'w') as f:
                f.write('\n'.join(cv.rows()) + '\n')
        written += count
    print(f'wrote {written} picture sketches to {os.path.normpath(ROOT)}')


if __name__ == '__main__':
    main(int(sys.argv[1]) if len(sys.argv) > 1 else 2026)

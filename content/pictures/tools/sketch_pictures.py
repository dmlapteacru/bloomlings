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

Since 2026-10-06 (the owner's bigger boards: 224-288 cells from Level 11, and from Level 525 a big board of 289-616
cells, at most 22 x 28, every 25th level) two more bands follow, drawn by the subject modules next to this script
(garden_subjects.py redraws the garden subjects above with more detail, world_subjects.py adds buildings, vehicles, toys
and things, animal_subjects.py animals, food_subjects.py food; picture_kit.py holds their drawing helpers):
regular 14-16 x 16-18 (four pictures of every subject) and big 17-22 x 20-28 (one of every subject). Their sketches are
tidied (no stray single cells), their holes keep the background share and nesting depth inside the picker's structure
targets, and the script stops with a list of problems unless every picture meets its band's targets (nesting depth,
background share, role sizes, occupancy, variants: all carry five and six distinct variants). The new bands come after
the first ones, so the first pictures keep their random stream and stay byte-identical.

`--expansions` also writes the expansion variants' pictures (expansions.py, FR-060): 178 regular pictures with a lime
role (Vine joins at L45), 164 with a red role (Berry, L200) and 97 big ones with both where the subject has them. Each
moves the roles of the subject that read yellow or dark red into those groups, and a drawing that misses its band's
targets at its size tries the band's next sizes. Until the automated picture checks accept a role whose color group
only has an expansion variant, `pictures import` leaves these pictures drafts, so the default run leaves them out.
`--out <folder>` writes the sketches elsewhere than content/pictures/src.

Since 2026-10-07 a second set of subjects follows for the levels (more_subjects.py: the owner's "more subjects for the
5000 levels"), four regular pictures and one big one of each, after every band above with a random stream of its own;
`--no-more` leaves it out, and `--more --module more_<group>` checks one module at every size and writes review sheets.
`--daily` writes the Daily Challenge's own pictures (daily_subjects.py, three of each subject at 22 x 28, theme `daily`).
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


# ---- The bigger boards (the owner, 2026-10-06): regular boards of 224-288 cells from Level 11, and from Level 525 a
# big board of 289-616 cells (at most 22 x 28) every 25th level. The subject modules draw with picture_kit, which
# takes Canvas from this module (registered under its name also when it runs as a script).
sys.modules.setdefault('sketch_pictures', sys.modules[__name__])
import expansions  # noqa: E402
import picture_kit  # noqa: E402
from animal_subjects import ANIMAL_SUBJECTS  # noqa: E402
from food_subjects import FOOD_SUBJECTS  # noqa: E402
from garden_subjects import GARDEN_SUBJECTS  # noqa: E402
from world_subjects import WORLD_SUBJECTS  # noqa: E402

# The redrawn garden subjects, then the world (buildings, vehicles, toys, things), animals and food. Pictures take
# subject (written + 7i) and size i in turn, so the count stays coprime with 7 and with the 9 regular sizes: every
# subject then comes once in each run of len(NEW_SUBJECTS) pictures, and in four different sizes (97 since 2026-10-06).
NEW_SUBJECTS = GARDEN_SUBJECTS + WORLD_SUBJECTS + ANIMAL_SUBJECTS + FOOD_SUBJECTS

REGULAR_SIZES = [(14, 16), (15, 16), (14, 17), (15, 17), (16, 16), (16, 17), (14, 18), (15, 18), (16, 18)]
BIG_SIZES = [(17, 20), (18, 22), (19, 24), (20, 24), (21, 26), (22, 28), (18, 21), (20, 25)]
assert math.gcd(len(NEW_SUBJECTS), 7 * len(REGULAR_SIZES)) == 1, 'keep the subject count coprime with 7 and the sizes'

# How the new bands finish and check each sketch: the picker's structure targets (nesting depth, background share in
# per mille, PicturePicker and the band profiles), the fewest distinct variants every picture carries, and the share of
# pictures that carry six. The holes never take the background below `min_background`.
REGULAR = {'version': 1, 'depth': (2, 4), 'background': (250, 650), 'min_background': 0.27, 'variants': 5, 'six': 0.6}
BIG = {'version': 1, 'depth': (2, 5), 'background': (250, 650), 'min_background': 0.27, 'variants': 6, 'six': 1.0}

BANDS = [
    ('early', 16, [(12, 12), (12, 13)], SUBJECTS),
    ('early_mid', 26, [(12, 13), (13, 13), (12, 14), (13, 14)], SUBJECTS),
    ('core', 52, [(13, 14), (14, 14), (13, 15), (14, 15), (13, 16), (14, 16)], SUBJECTS),
    # Added 2026-10-05: Levels 51-100 use 50 distinct pictures with five variants, and the 52 above ran out by L93.
    ('core', 16, [(14, 15), (13, 16), (14, 16), (13, 15)], FIVE_VARIANTS),
    # Added 2026-10-06: the bigger boards. New entries go last, so the bands above keep their random stream.
    ('regular', 4 * len(NEW_SUBJECTS), REGULAR_SIZES, NEW_SUBJECTS, REGULAR),
    ('big', len(NEW_SUBJECTS), BIG_SIZES, NEW_SUBJECTS, BIG),
]

# The expansion variants' pictures (expansions.py): regular pictures with a lime role (Vine, from L45) or a red role
# (Berry, from L200), two of each subject that has one, and a big picture of every subject with its lime and red roles
# (big levels come from L525, after both). They come after the bands above, so those keep their random stream, and
# only `--expansions` writes them: the automated picture checks (PictureChecks) approve a role only when its color
# group has a launch variant, so `pictures import` would leave them drafts until that check also accepts the groups of
# the pool's expansion variants.
LIME_SUBJECTS = expansions.subjects(NEW_SUBJECTS, expansions.LIME)
RED_SUBJECTS = expansions.subjects(NEW_SUBJECTS, expansions.RED)
for _subjects in (LIME_SUBJECTS, RED_SUBJECTS):
    assert math.gcd(len(_subjects), 7 * len(REGULAR_SIZES)) == 1, 'keep each subject count coprime with 7 and the sizes'
REGULAR_LIME = dict(REGULAR, recolor=(expansions.LIME,))
REGULAR_RED = dict(REGULAR, recolor=(expansions.RED,))
BIG_EXPANSION = dict(BIG, recolor=(expansions.LIME, expansions.RED))
EXPANSION_BANDS = [
    ('regular', 2 * len(LIME_SUBJECTS), REGULAR_SIZES, LIME_SUBJECTS, REGULAR_LIME),
    ('regular', 2 * len(RED_SUBJECTS), REGULAR_SIZES, RED_SUBJECTS, REGULAR_RED),
    ('big', len(NEW_SUBJECTS), BIG_SIZES, NEW_SUBJECTS, BIG_EXPANSION),
]

# Color groups with one variant (an expansion's): a mapping gives all their roles that variant.
SINGLE_VARIANT_GROUPS = {expansions.LIME, expansions.RED}


def finish(cv, roles, r, style):
    """The new bands' finish: stray single cells go, small roles grow to a pod, then the holes open, never taking the
    background below `min_background` nor the nesting depth past the band's."""
    bg = roles[0][0]
    picture_kit.tidy(cv)
    cv.grow_small_roles(bg, MIN_ROLE_CELLS)
    share = sum(row.count(bg) for row in cv.g) / (cv.w * cv.h)
    target = min(0.94, max(r.uniform(0.84, 0.92), 1.0 - (share - style['min_background'])))
    picture_kit.open_holes(cv, bg, r, target, style['depth'][1])


def structure(cv, roles):
    """Nesting depth and background share (per mille) as StructureMetrics computes them on import."""
    bg = sum(row.count(roles[0][0]) for row in cv.g)
    return picture_kit.nesting_depth(cv), bg * 1000 // (cv.w * cv.h)


def variants(roles):
    """The fewest and most distinct variants a mapping can give the roles (at most two per launch color group, one per
    expansion group)."""
    groups = {}
    for role in roles:
        groups[role[3]] = groups.get(role[3], 0) + 1
    return len(groups), sum(min(1 if g in SINGLE_VARIANT_GROUPS else 2, k) for g, k in groups.items())


def sketch(subject, w, h, seed, style, where):
    """One picture: the drawing, finished, and what keeps it from its band's targets (new bands only)."""
    r = random.Random(seed)
    cv, roles, themes = subject(w, h, r)
    if style and style.get('recolor'):
        roles = expansions.recolor(subject, roles, style['recolor'])
    bg = roles[0][0]
    if style:
        finish(cv, roles, r, style)
    else:
        cv.grow_small_roles(bg, MIN_ROLE_CELLS)
        cv.holes(bg, r, target=r.uniform(0.84, 0.92))
    used = {c for row in cv.rows() for c in row}
    roles = [role for role in roles if role[0] in used]
    issues, six, held = [], False, []
    if style:
        depth, share = structure(cv, roles)
        fewest, most = variants(roles)
        six = fewest <= 6 <= most
        small = [role[1] for role in roles if sum(row.count(role[0]) for row in cv.g) < MIN_ROLE_CELLS]
        occupancy = sum(1 for row in cv.g for c in row if c != '.') * 1000 // (w * h)
        if not style['depth'][0] <= depth <= style['depth'][1]:
            issues.append(f'{where}: nesting depth {depth}')
        if not style['background'][0] <= share <= style['background'][1]:
            issues.append(f'{where}: background {share}‰')
        if not fewest <= style['variants'] <= most:
            issues.append(f'{where}: carries {fewest}-{most} variants, not {style["variants"]}')
        if small or not 750 <= occupancy <= 950:
            issues.append(f'{where}: small roles {small}, occupancy {occupancy}‰')
        if style.get('recolor'):
            held = sorted({role[3] for role in roles} & set(style['recolor']))
            if not held:
                issues.append(f'{where}: no {" or ".join(style["recolor"])} role left')
    return cv, roles, themes, issues, six, held


def main(seed=2026, with_expansions=False, root=ROOT, more=True):
    os.makedirs(root, exist_ok=True)
    counter = {}
    written = 0
    problems = []

    def draw(bands, rng):
        nonlocal written
        for band, count, sizes, subjects, *style in bands:
            style = style[0] if style else None
            six = 0
            carried = {}
            for i in range(count):
                subject = subjects[(written + i * 7) % len(subjects)]
                seed_i = rng.randrange(1 << 30)
                # The expansion bands try the band's next sizes when a drawing misses its targets at this one.
                tries = len(sizes) if style and style.get('recolor') else 1
                for k in range(tries):
                    w, h = sizes[(i + k) % len(sizes)]
                    cv, roles, themes, issues, ok_six, held = sketch(subject, w, h, seed_i, style, f'{band} #{i} {subject.__name__} {w}x{h}')
                    if not issues:
                        break
                problems.extend(issues)
                six += ok_six
                for group in held:
                    carried[group] = carried.get(group, 0) + 1
                name = subject.__name__
                counter[name] = counter.get(name, 0) + 1
                pid = f'{name}_{counter[name]:02d}'
                meta_path = os.path.join(root, pid + '.meta.json')
                if os.path.exists(meta_path) and '"approved"' in open(meta_path).read():
                    continue
                meta = {
                    'id': pid, 'version': style['version'] if style else VERSION,
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
                with open(os.path.join(root, pid + '.grid.txt'), 'w') as f:
                    f.write('\n'.join(cv.rows()) + '\n')
            if style and six < style['six'] * count:
                problems.append(f'{band}: {six} of {count} pictures carry six variants, fewer than {style["six"]:.0%}')
            if style:
                extra = ''.join(f', {n} with a {g} role' for g, n in sorted(carried.items()))
                print(f'{band}: {count} pictures, {six} carry six variants{extra}')
            written += count

    draw(BANDS + (EXPANSION_BANDS if with_expansions else []), random.Random(seed))
    if more:
        # The second set of subjects comes after every band above, with a random stream of its own.
        more_regular, more_expansion = more_bands()
        draw(more_regular + (more_expansion if with_expansions else []), random.Random(f'more:{seed}'))
    print(f'wrote {written} picture sketches to {os.path.normpath(root)}')
    if problems:
        print('\n'.join(problems), file=sys.stderr)
        sys.exit(1)


# The second set of subjects for the levels (the owner, 2026-10-07: "more subjects for the 5000 levels, spread over
# them"): more_subjects.py, subjects that neither the levels nor the Daily Challenge drew before, with four regular
# pictures and one big picture of each, as the subjects above, and with `--expansions` their lime and red pictures
# (more_subjects.MORE_ROLES, as expansions.ROLES). They come after every band above with a random stream of their own,
# so every picture above stays byte-identical. They are imported only when drawn, so a module being written cannot
# stop the other modes.
def more_bands():
    from more_subjects import MORE_ROLES, MORE_SUBJECTS
    expansions.ROLES.update(MORE_ROLES)
    lime = expansions.subjects(MORE_SUBJECTS, expansions.LIME)
    red = expansions.subjects(MORE_SUBJECTS, expansions.RED)
    either = [s for s in MORE_SUBJECTS if s.__name__ in MORE_ROLES]
    for subjects in (MORE_SUBJECTS, lime, red):
        assert not subjects or math.gcd(len(subjects), 7) == 1, f'{len(subjects)} subjects: keep the count coprime with 7'
    regular = [
        ('regular', 4 * len(MORE_SUBJECTS), REGULAR_SIZES, MORE_SUBJECTS, REGULAR),
        ('big', len(MORE_SUBJECTS), BIG_SIZES, MORE_SUBJECTS, BIG),
    ]
    extra = [
        ('regular', 2 * len(lime), REGULAR_SIZES, lime, REGULAR_LIME),
        ('regular', 2 * len(red), REGULAR_SIZES, red, REGULAR_RED),
        ('big', len(either), BIG_SIZES, either, BIG_EXPANSION),
    ]
    return [b for b in regular if b[1]], [b for b in extra if b[1]]


def defined_subjects(prefix):
    """The subject functions defined in the modules <prefix>_*.py next to this script (read, not imported, so a module
    being written cannot stop a check), the aggregator <prefix>_subjects.py aside."""
    import glob
    import re
    here = os.path.dirname(os.path.abspath(__file__))
    names = []
    for path in sorted(glob.glob(os.path.join(here, prefix + '_*.py'))):
        if os.path.basename(path) != prefix + '_subjects.py':
            names += re.findall(r'^def (\w+)\(w, h, r\)', open(path).read(), re.M)
    return names


def main_more(seed=2026, root=ROOT, only=None, png=None, module=None):
    """Draws a module's subjects of the second set at every regular and big size (and their lime and red pictures at one
    size of each), reports what misses the band targets, and writes a PNG sheet per subject: the check an author runs
    while writing a module. `main` draws the pictures themselves."""
    import importlib
    import zlib
    mod = importlib.import_module(module)
    subjects = getattr(mod, 'MORE_' + module[len('more_'):].upper())
    roles_table = getattr(mod, 'MORE_' + module[len('more_'):].upper() + '_ROLES', {})
    expansions.ROLES.update(roles_table)
    names = [s.__name__ for s in subjects]
    taken = {s.__name__ for s in SUBJECTS} | {s.__name__ for s in NEW_SUBJECTS} | set(defined_subjects('daily'))
    problems = [f'more: {n} is a subject already (the levels or the Daily Challenge)' for n in names if n in taken]
    problems += [f'more: {n} is listed twice' for n in sorted(set(names)) if names.count(n) > 1]
    problems += [f'more: {n} in the roles table is not a subject of {module}' for n in roles_table if n not in names]
    os.makedirs(root, exist_ok=True)
    written = 0
    for subject in subjects:
        name = subject.__name__
        if only and name not in only:
            continue
        ids, six = [], 0
        cases = [(w, h, REGULAR, 'r') for w, h in REGULAR_SIZES] + [(w, h, BIG, 'b') for w, h in BIG_SIZES]
        if name in roles_table:
            groups = sorted(roles_table[name])
            cases += [(15, 17, dict(REGULAR, recolor=(g,)), g[0]) for g in groups]
            cases += [(22, 28, dict(BIG, recolor=tuple(groups)), 'x')]
        for w, h, style, tag in cases:
            seed_k = zlib.crc32(f'{seed}:{name}:{w}x{h}:{tag}'.encode())
            where = f'more {name} {w}x{h}' + ('' if tag in 'rb' else f' ({tag})')
            try:
                cv, roles, themes, issues, ok_six, held = sketch(subject, w, h, seed_k, style, where)
            except Exception as error:  # a subject under construction: report it and go on
                problems.append(f'{where}: {type(error).__name__}: {error}')
                continue
            problems += issues
            if tag == 'r':
                six += ok_six
            elif style['variants'] == 6 and not ok_six:
                problems.append(f'{where}: cannot carry six variants')
            pid = f'{name}_{w}x{h}_{tag}'
            meta = {
                'id': pid, 'version': 1, 'subject': name, 'width': w, 'height': h,
                'legend': {role[0]: role[1] for role in roles},
                'roles': [dict({'roleId': role[1], 'name': role[2], 'colorGroup': role[3]}, **({'isBackground': True} if role[4] else {})) for role in roles],
                'tags': {'themes': themes},
            }
            with open(os.path.join(root, pid + '.meta.json'), 'w') as f:
                json.dump(meta, f, indent=2, sort_keys=True)
            with open(os.path.join(root, pid + '.grid.txt'), 'w') as f:
                f.write('\n'.join(cv.rows()) + '\n')
            ids.append(pid)
            written += 1
        if six < 6:
            problems.append(f'more {name}: only {six} of the 9 regular sizes carry six variants (6 at least)')
        if png and ids:
            import preview_png
            os.makedirs(png, exist_ok=True)
            preview_png.sheet(os.path.join(png, name + '.png'), ids, root, cell=10, columns=9)
    print(f'more: drew {written} pictures of {len(subjects) if not only else len(only)} subjects to {os.path.normpath(root)}')
    if problems:
        print('\n'.join(problems), file=sys.stderr)
        sys.exit(1)


# The Daily Challenge's own pictures (the owner, 2026-10-07: every day a new picture, of a subject the levels never show,
# on the biggest board): the new subjects of daily_subjects.py, DAILY_PICTURES pictures of each at DAILY_SIZE, with the
# 'daily' theme, which keeps them out of the catalog's levels (PicturePicker.DailyTheme) and is the only theme the daily
# profile takes. Only `--daily` writes them, each from its own seed (the run's seed, the subject and the number), so they
# never move the catalog's pictures, and `--only` redraws one subject alone. They meet the big band's targets.
DAILY_SIZE = (22, 28)
DAILY = dict(BIG)
DAILY_PICTURES = 3
DAILY_THEME = 'daily'


def main_daily(seed=2026, root=ROOT, only=None, png=None, module=None):
    import importlib
    import zlib
    if module:
        # One module's subjects alone (daily_animals, …), so a module can be drawn while another is being written.
        DAILY_SUBJECTS = getattr(importlib.import_module(module), 'DAILY_' + module[len('daily_'):].upper())
    else:
        from daily_subjects import DAILY_SUBJECTS
    names = [s.__name__ for s in DAILY_SUBJECTS]
    taken = {s.__name__ for s in SUBJECTS} | {s.__name__ for s in NEW_SUBJECTS} | set(defined_subjects('more'))
    problems = [f'daily: {n} is a subject of the levels already' for n in names if n in taken]
    problems += [f'daily: {n} is listed twice' for n in sorted(set(names)) if names.count(n) > 1]
    os.makedirs(root, exist_ok=True)
    written = 0
    for subject in DAILY_SUBJECTS:
        name = subject.__name__
        if only and name not in only:
            continue
        ids = []
        for k in range(1, DAILY_PICTURES + 1):
            w, h = DAILY_SIZE
            seed_k = zlib.crc32(f'{seed}:{name}:{k}'.encode())
            try:
                cv, roles, themes, issues, six, held = sketch(subject, w, h, seed_k, DAILY, f'daily {name} #{k}')
            except Exception as error:  # a subject under construction: report it and go on
                problems.append(f'daily {name} #{k}: {type(error).__name__}: {error}')
                continue
            problems += issues
            if not six:
                problems.append(f'daily {name} #{k}: cannot carry six variants')
            pid = f'{name}_{k:02d}'
            meta_path = os.path.join(root, pid + '.meta.json')
            if os.path.exists(meta_path) and '"approved"' in open(meta_path).read():
                ids.append(pid)
                continue
            meta = {
                'id': pid, 'version': DAILY['version'],
                'subject': name.replace('_', ' ').capitalize() + ' ' + str(k),
                'width': w, 'height': h,
                'legend': {role[0]: role[1] for role in roles},
                'roles': [dict({'roleId': role[1], 'name': role[2], 'colorGroup': role[3]}, **({'isBackground': True} if role[4] else {})) for role in roles],
                'finishedLook': {'mode': 'auto'},
                'tags': {'themes': list(themes) + [DAILY_THEME], 'bands': ['daily']},
                'review': {'status': 'draft', 'notes': 'Procedural sketch for the Daily Challenge (content/pictures/tools/sketch_pictures.py --daily); approved on import by the automated picture checks (FR-084 as amended).'},
                'source': {'kind': 'generated', 'origin': 'content/pictures/tools/sketch_pictures.py', 'licence': 'owned'},
            }
            with open(meta_path, 'w') as f:
                json.dump(meta, f, indent=2, sort_keys=True)
                f.write('\n')
            with open(os.path.join(root, pid + '.grid.txt'), 'w') as f:
                f.write('\n'.join(cv.rows()) + '\n')
            ids.append(pid)
            written += 1
        if png and ids:
            import preview_png
            os.makedirs(png, exist_ok=True)
            preview_png.sheet(os.path.join(png, name + '.png'), ids, root, cell=12, columns=DAILY_PICTURES)
    print(f'daily: wrote {written} picture sketches of {len(DAILY_SUBJECTS) if not only else len(only)} subjects to {os.path.normpath(root)}')
    if problems:
        print('\n'.join(problems), file=sys.stderr)
        sys.exit(1)


if __name__ == '__main__':
    # sketch_pictures.py [seed] [--expansions] [--no-more] [--out <folder>]
    # sketch_pictures.py --daily [seed] [--module daily_<group>] [--only <subject>[,<subject>...]] [--png <folder>] [--out <folder>]
    args = sys.argv[1:]
    out = ROOT
    if '--out' in args:
        out = args[args.index('--out') + 1]
        del args[args.index('--out'):args.index('--out') + 2]
    if '--more' in args:
        # sketch_pictures.py --more --module more_<group> [seed] [--only <subject>[,...]] [--png <folder>] [--out <folder>]
        args.remove('--more')
        only = png = None
        if '--only' in args:
            only = set(args[args.index('--only') + 1].split(','))
            del args[args.index('--only'):args.index('--only') + 2]
        if '--png' in args:
            png = args[args.index('--png') + 1]
            del args[args.index('--png'):args.index('--png') + 2]
        module = args[args.index('--module') + 1]
        del args[args.index('--module'):args.index('--module') + 2]
        main_more(int(args[0]) if args else 2026, out, only, png, module)
        sys.exit(0)
    if '--daily' in args:
        args.remove('--daily')
        only = png = None
        if '--only' in args:
            only = set(args[args.index('--only') + 1].split(','))
            del args[args.index('--only'):args.index('--only') + 2]
        if '--png' in args:
            png = args[args.index('--png') + 1]
            del args[args.index('--png'):args.index('--png') + 2]
        module = None
        if '--module' in args:
            module = args[args.index('--module') + 1]
            del args[args.index('--module'):args.index('--module') + 2]
        main_daily(int(args[0]) if args else 2026, out, only, png, module)
        sys.exit(0)
    with_expansions = '--expansions' in args
    more = '--no-more' not in args
    args = [a for a in args if a not in ('--expansions', '--no-more')]
    main(int(args[0]) if args else 2026, with_expansions, out, more)

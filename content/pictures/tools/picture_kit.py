"""Drawing helpers for the picture subjects of the bigger boards (the owner, 2026-10-06): continuous shapes on the
Canvas of sketch_pictures.py, so a subject scales from 14 x 16 to 22 x 28 and can add detail where the cells allow it.

Coordinates are in cells, x to the right and y down, with cell (x, y) covering [x, x + 1) x [y, y + 1); a shape takes
the cells whose centers it covers. `tidy` removes stray single cells from a drawing (a cell none of whose eight
neighbours shares its role), except in roles that are only specks on purpose (stars, seeds, raindrops).
"""
import math
from collections import deque

GREEN, PINK, BLUE, BROWN = 'green', 'pink_purple', 'blue_cyan', 'brown_orange'


def canvas(w, h, fill):
    from sketch_pictures import Canvas  # imported here: sketch_pictures imports the subject modules
    return Canvas(w, h, fill)


def start(w, h, fill):
    """A canvas filled with the background, its short side, its middle column and whether it is a big board."""
    return canvas(w, h, fill), min(w, h), w / 2, w * h >= 300


def sky(char='s', name='Sky', group=BLUE, role='sky'):
    """The background role (roles[0] of every subject)."""
    return (char, role, name, group, True)


def role(char, role_id, name, group):
    return (char, role_id, name, group, False)


def cells(cv):
    for y in range(cv.h):
        for x in range(cv.w):
            yield x, y, x + 0.5, y + 0.5


def disc(cv, cx, cy, r, c):
    cv.ellipse(cx, cy, r, r, c)


def oval(cv, cx, cy, rx, ry, c):
    cv.ellipse(cx, cy, rx, ry, c)


def box(cv, x0, y0, x1, y1, c, only=None):
    """The cells whose centers lie in [x0, x1] x [y0, y1]; `only` limits it to cells of those roles."""
    for x, y, px, py in cells(cv):
        if x0 <= px <= x1 and y0 <= py <= y1 and (only is None or cv.g[y][x] in only):
            cv.g[y][x] = c


def rbox(cv, x0, y0, x1, y1, rad, c):
    """A box with rounded corners of radius `rad`."""
    for x, y, px, py in cells(cv):
        if x0 <= px <= x1 and y0 <= py <= y1:
            dx = max(x0 + rad - px, 0, px - (x1 - rad))
            dy = max(y0 + rad - py, 0, py - (y1 - rad))
            if dx * dx + dy * dy <= rad * rad:
                cv.g[y][x] = c


def poly(cv, pts, c):
    cv.poly(pts, c)


def seg(cv, x0, y0, x1, y1, c, t=0.6):
    cv.line(x0, y0, x1, y1, c, t)


def path(cv, pts, c, t=0.6):
    for (a, b), (d, e) in zip(pts, pts[1:]):
        cv.line(a, b, d, e, c, t)


def lens(cv, x0, y0, x1, y1, width, c):
    """A leaf shape: a lens from (x0, y0) to (x1, y1), `width` cells across at its middle."""
    dx, dy = x1 - x0, y1 - y0
    L = math.hypot(dx, dy) or 1.0
    nx, ny = -dy / L, dx / L
    pts = []
    for k in range(9):
        t = k / 8
        off = math.sin(math.pi * t) * width / 2
        pts.append((x0 + dx * t + nx * off, y0 + dy * t + ny * off))
    for k in range(7, 0, -1):
        t = k / 8
        off = math.sin(math.pi * t) * width / 2
        pts.append((x0 + dx * t - nx * off, y0 + dy * t - ny * off))
    cv.poly(pts, c)


def ring(cv, cx, cy, ro, ri, c, ry_scale=1.0):
    for x, y, px, py in cells(cv):
        d = math.hypot(px - cx, (py - cy) / ry_scale)
        if ri < d <= ro:
            cv.g[y][x] = c


def star(cv, cx, cy, ro, c, ri=None, points=5, rot=-90):
    ri = ro * 0.45 if ri is None else ri
    pts = []
    for k in range(points * 2):
        a = math.radians(rot + k * 180 / points)
        rr = ro if k % 2 == 0 else ri
        pts.append((cx + math.cos(a) * rr, cy + math.sin(a) * rr))
    cv.poly(pts, c)


def heart(cv, cx, cy, s, c):
    """A heart about 2s wide, centered on (cx, cy)."""
    for x, y, px, py in cells(cv):
        u, v = (px - cx) / s, -(py - cy) / s + 0.25
        if (u * u + v * v - 1) ** 3 - u * u * v ** 3 <= 0:
            cv.g[y][x] = c


def cloud(cv, cx, cy, s, c):
    """A puffy cloud about 3.2s wide and 1.6s high."""
    disc(cv, cx - s * 0.8, cy + s * 0.15, s * 0.65, c)
    disc(cv, cx + s * 0.1, cy - s * 0.15, s * 0.85, c)
    disc(cv, cx + s * 0.95, cy + s * 0.2, s * 0.6, c)
    box(cv, cx - s * 1.4, cy + s * 0.1, cx + s * 1.5, cy + s * 0.8, c)


def hills(cv, c, base, amp, period, phase=0.0, only=None):
    """Fills every cell below a sine line at height `base` (cells from the top)."""
    for x, y, px, py in cells(cv):
        top = base - amp * math.sin(2 * math.pi * px / period + phase)
        if py >= top and (only is None or cv.g[y][x] in only):
            cv.g[y][x] = c


def ground_rows(h, share=0.12, least=2):
    return max(least, int(round(h * share)))


def regions(cv, c):
    """The number of 4-connected regions of role c."""
    seen = set()
    n = 0
    for y in range(cv.h):
        for x in range(cv.w):
            if cv.g[y][x] != c or (x, y) in seen:
                continue
            n += 1
            seen.add((x, y))
            stack = [(x, y)]
            while stack:
                a, b = stack.pop()
                for p, q in ((a + 1, b), (a - 1, b), (a, b + 1), (a, b - 1)):
                    if 0 <= p < cv.w and 0 <= q < cv.h and (p, q) not in seen and cv.g[q][p] == c:
                        seen.add((p, q))
                        stack.append((p, q))
    return n


def scatter(cv, c, on, n, r, sep=2, area=None):
    """Up to n cells of role c on cells of the roles in `on`, at least `sep` cells apart (Chebyshev); a cell whose
    change would split the region under it is skipped, so specks never cut a shape into pieces."""
    x0, y0, x1, y1 = area or (0, 0, cv.w - 1, cv.h - 1)
    spots = [(x, y) for y in range(max(0, int(y0)), min(cv.h, int(y1) + 1))
             for x in range(max(0, int(x0)), min(cv.w, int(x1) + 1)) if cv.g[y][x] in on]
    r.shuffle(spots)
    placed = []
    for x, y in spots:
        if len(placed) >= n:
            break
        if all(max(abs(x - a), abs(y - b)) >= sep for a, b in placed):
            under = cv.g[y][x]
            before = regions(cv, under)
            cv.g[y][x] = c
            if regions(cv, under) > before:
                cv.g[y][x] = under
                continue
            placed.append((x, y))
    return placed


def dots(cv, c, on, dx, dy, area=None, offset=0):
    """A pattern of single dots every `dx` columns and `dy` rows (alternate rows shifted by half a step), placed only
    where the cell and its eight neighbours are of the roles in `on`, so a pattern never touches another shape."""
    x0, y0, x1, y1 = area or (0, 0, cv.w - 1, cv.h - 1)
    for k, y in enumerate(range(int(y0), int(y1) + 1, dy)):
        for x in range(int(x0) + offset + (k % 2) * (dx // 2), int(x1) + 1, dx):
            if 0 < x < cv.w - 1 and 0 < y < cv.h - 1 and all(
                    cv.g[y + b][x + a] in on for a in (-1, 0, 1) for b in (-1, 0, 1)):
                cv.g[y][x] = c


def _largest_groups(cv):
    """The largest 8-connected group of cells of each role."""
    seen = [[False] * cv.w for _ in range(cv.h)]
    best = {}
    for y in range(cv.h):
        for x in range(cv.w):
            if seen[y][x]:
                continue
            c = cv.g[y][x]
            seen[y][x] = True
            stack, n = [(x, y)], 0
            while stack:
                a, b = stack.pop()
                n += 1
                for dy in (-1, 0, 1):
                    for dx in (-1, 0, 1):
                        p, q = a + dx, b + dy
                        if 0 <= p < cv.w and 0 <= q < cv.h and not seen[q][p] and cv.g[q][p] == c:
                            seen[q][p] = True
                            stack.append((p, q))
            best[c] = max(best.get(c, 0), n)
    return best


def tidy(cv, keep=''):
    """Replaces stray single cells (none of the eight neighbours shares the role) by the most common neighbour.
    Holes stay, and so do the roles in `keep` and roles that are only small specks (stars, seeds, raindrops: no group
    of three cells or more), which are drawn as single cells on purpose."""
    changed = True
    while changed:
        changed = False
        groups = _largest_groups(cv)
        for y in range(cv.h):
            for x in range(cv.w):
                c = cv.g[y][x]
                if c in '.#' or c in keep or groups.get(c, 0) < 3:
                    continue
                around = [cv.g[y + dy][x + dx] for dy in (-1, 0, 1) for dx in (-1, 0, 1)
                          if (dx or dy) and 0 <= x + dx < cv.w and 0 <= y + dy < cv.h]
                if c in around:
                    continue
                side = [cv.g[y + dy][x + dx] for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))
                        if 0 <= x + dx < cv.w and 0 <= y + dy < cv.h and cv.g[y + dy][x + dx] != '.']
                if side:
                    cv.g[y][x] = max(sorted(set(side)), key=side.count)
                    changed = True


def nesting_depth(cv):
    """The nesting depth StructureMetrics computes on import: the regions (4-connected areas of one role) passed from
    the bottom-center entry to reach the deepest one; open cells cost nothing to cross."""
    w, h = cv.w, cv.h
    g = [c for row in reversed(cv.g) for c in row]  # bottom row first, as BasePicture.Grid
    comp = [-1] * (w * h)
    n = 0
    for i in range(w * h):
        if g[i] in '.#' or comp[i] >= 0:
            continue
        comp[i], stack, c = n, [i], g[i]
        while stack:
            j = stack.pop()
            x = j % w
            for k in (j - w, j + w, j - 1 if x > 0 else -1, j + 1 if x < w - 1 else -1):
                if 0 <= k < w * h and comp[k] < 0 and g[k] == c:
                    comp[k] = n
                    stack.append(k)
        n += 1
    inf = 1 << 30
    cost = [inf] * (w * h)
    ex = w // 2
    if g[ex] == '#':
        return 1
    cost[ex] = 0 if g[ex] == '.' else 1
    dq = deque([ex])
    while dq:
        j = dq.popleft()
        x = j % w
        here_open = g[j] == '.'
        for k in (j - w, j - 1 if x > 0 else -1, j + 1 if x < w - 1 else -1, j + w):
            if not 0 <= k < w * h or g[k] == '#':
                continue
            step = g[k] != '.' and (here_open or comp[k] != comp[j])
            c = cost[j] + step
            if c < cost[k]:
                cost[k] = c
                if step:
                    dq.append(k)
                else:
                    dq.appendleft(k)
    return max([1] + [cost[i] for i in range(w * h) if g[i] not in '.#' and cost[i] < inf])


def open_holes(cv, bg, r, target, max_depth):
    """Opens background cells as holes (the open garden), corners first and then at random, down to the target
    occupancy, like Canvas.holes; a hole that would push the nesting depth past `max_depth` (or past the drawing's own,
    when that is deeper) is skipped, so the holes never split the background into deeper pockets."""
    limit = max(max_depth, nesting_depth(cv))
    corners = [(0, 0), (cv.w - 1, 0), (0, cv.h - 1), (cv.w - 1, cv.h - 1)]
    rest = [(x, y) for y in range(cv.h) for x in range(cv.w) if cv.g[y][x] == bg and y < cv.h - 1]
    r.shuffle(rest)
    total = cv.w * cv.h
    filled = sum(1 for row in cv.g for c in row if c != '.')
    for k, (x, y) in enumerate(corners + rest):
        if k >= len(corners) and filled / total <= target:
            break
        if cv.g[y][x] != bg:
            continue
        cv.g[y][x] = '.'
        if nesting_depth(cv) > limit:
            cv.g[y][x] = bg
        else:
            filled -= 1

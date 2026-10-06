"""The four loading-screen concepts: each has a boot loader (app start -> Home) and a level-to-level transition
(Win "Next" -> Level 13). Usage: GD=<style-branch checkout> python3 variants.py <out_dir> [names...]"""
import functools
import os
import random
import subprocess
import sys

from lib import *

HOME = HomeStage()
LEVEL_WON, LEVEL_NEXT = 12, 13
CREAM_BG = vgrad(W, H, [(0, PARCH_TOP), (1, PARCH_BOTTOM)])


def radial_cream():
    ys, xs = np.mgrid[0:H, 0:W]
    d = np.sqrt(((xs - W / 2) / W) ** 2 + ((ys - H * 0.47) / H) ** 2 * 1.2)
    k = np.clip(d / 0.75, 0, 1)[..., None]
    a = np.array(lighten(PARCH_TOP, 0.4), dtype=np.float32)
    b = np.array(PARCH_EDGE, dtype=np.float32)
    arr = (a * (1 - k) + b * k).astype(np.uint8)
    return Image.fromarray(arr, "RGB").convert("RGBA")


RADIAL = radial_cream()


def veil(img, color, alpha):
    out = img.copy()
    paste(out, solid(W, H, color), 0, 0, alpha)
    return out


def crossfade(a, b, k):
    if k <= 0:
        return a.copy()
    if k >= 1:
        return b.copy()
    return Image.blend(a, b, k)


def home_frame(t, ui=1.0, heroes=1.0, petals=0.0, ui_rise=0.0):
    f = HOME.draw(t, hero_alpha=heroes, petals=petals)
    if ui > 0:
        paste(f, home_ui(LEVEL_NEXT), 0, ui_rise, ui)
    return f


def press_of(t, at=0.3):
    return math.sin(math.pi * seg(t, at, at + 0.18)) if at <= t <= at + 0.18 else 0.0


def win_frame(t):
    return win_screen(t + 1.0, LEVEL_WON, press=press_of(t))


def game_frame(tiles=1.0):
    return gameplay_screen(LEVEL_NEXT, tiles=tiles)


def loading_dots(t):
    n = int(t * 3) % 4
    return "Loading" + "." * n


def text_center(f, s, size, cx, cy, color=INK_TITLE, alpha=1.0, stroke=3, stroke_fill=PARCH_TOP, scale=1.0):
    if s.startswith("Loading"):
        full = text_sprite("Loading...", size, color, stroke=stroke, stroke_fill=stroke_fill)
        part = text_sprite(s, size, color, stroke=stroke, stroke_fill=stroke_fill)
        paste(f, part, cx - full.width / 2, cy - full.height / 2, alpha)
        return
    blit(f, text_sprite(s, size, color, stroke=stroke, stroke_fill=stroke_fill), cx, cy, scale=scale, alpha=alpha)


# ======================================================================= 1. The wooden sign

def progress_bar(f, cx, cy, w, h, p, alpha=1.0, t=0.0):
    L = layer(w + 80, h + 80)
    ox, oy = 40, 40
    soft_shadow(L, (ox, oy + 6, ox + w, oy + h + 6), h / 2, alpha=0.35, blur=6)
    rrect(L, (ox, oy, ox + w, oy + h), h / 2, [(0, WOOD_DARK_TOP), (1, WOOD_DARK)], WOOD_DARK_LINE, 3)
    inner = (ox + 9, oy + 9, ox + w - 9, oy + h - 9)
    rrect(L, inner, (h - 18) / 2, [(0, darken(PARCH_EDGE, 0.15)), (1, PARCH_BOTTOM)])
    fw = (inner[2] - inner[0]) * clamp01(p)
    if fw > h - 18:
        rrect(L, (inner[0], inner[1], inner[0] + fw, inner[3]), (h - 18) / 2, [(0, BTN_TOP), (0.6, BTN), (1, BTN_EDGE)])
        rrect(L, (inner[0] + 8, inner[1] + 4, inner[0] + fw - 8, inner[1] + 10), 3, (255, 255, 255), alpha=0.4)
    leaf = icon("field", "leaf", round(h * 1.45))
    blit(L, leaf, inner[0] + max(fw, h * 0.3), oy + h / 2 - 2, rot=-12 + 10 * math.sin(t * 7))
    paste(f, L, cx - w / 2 - ox, cy - h / 2 - oy, alpha)


def boot_progress(t, start, end):
    """A loader's progress with the small plateaus real loading has."""
    x = seg(t, start, end)
    steps = [(0, 0), (0.25, 0.32), (0.4, 0.38), (0.65, 0.7), (0.78, 0.74), (1, 1)]
    xs, ys = zip(*steps)
    return float(np.interp(ease_in_out(x) * 0.15 + x * 0.85, xs, ys))


def sign_boot(t):
    # The splash (garden, fountain, heroes rising in, the logo) with a wooden progress bar; then Home.
    heroes = ease_out_cubic(seg(t, 0.2, 0.8))
    leave = ease_in_out(seg(t, 2.9, 3.15))
    to_home = ease_out_cubic(seg(t, 3.1, 3.5))
    f = home_frame(t, ui=to_home, heroes=heroes, petals=1 - to_home, ui_rise=60 * (1 - to_home))
    k = seg(t, 0.15, 0.7)
    blit(f, logo(590), W / 2, 0.153 * H, scale=0.7 + 0.3 * ease_out_back(k), alpha=clamp01(k * 2) * (1 - leave))
    bar_in = ease_out_cubic(seg(t, 0.45, 0.8))
    p = boot_progress(t, 0.6, 2.8)
    a = bar_in * (1 - leave)
    progress_bar(f, W / 2, 1290 + 40 * (1 - bar_in), 470, 52, p, alpha=a, t=t)
    text_center(f, loading_dots(t), 40, W / 2, 1375 + 40 * (1 - bar_in), INK_TITLE, alpha=a)
    f = crossfade(CREAM_BG, f, ease_out_cubic(seg(t, 0.0, 0.35)))
    return f


def hanging_sign(label, w, h, rope):
    """A sprite whose center is the ropes' top (the pivot), so rotating it swings the sign."""
    total = rope + h + 30
    L = layer(w + 60, total * 2)
    top = total  # pivot at the center
    d = ImageDraw.Draw(L)
    for sx in (0.22, 0.78):
        x = 30 + w * sx
        d.line((x, top - 4, x, top + rope + 14), fill=WOOD_LINE + (255,), width=7)
        d.line((x - 1, top - 4, x - 1, top + rope + 14), fill=WOOD_GRAIN + (255,), width=3)
    pl = plank(w, h, label, round(h * 0.5))
    paste(L, pl, 30 - 12, top + rope - 12 + 6)
    for sx in (0.22, 0.78):
        x = 30 + w * sx
        ellipse(L, (x - 10, top + rope + 4, x + 10, top + rope + 24), WOOD_LINE)
        ellipse(L, (x - 6, top + rope + 7, x + 6, top + rope + 19), WOOD_EDGE)
    return L


SIGN = None


def hoppers(f, t, cy, alpha):
    for i, ic in enumerate(("leaf", "flower", "drop")):
        ph = (t * 1.6 - i * 0.18) % 1.0
        hop = math.sin(math.pi * clamp01(ph / 0.45)) if ph < 0.45 else 0.0
        sq = 1 + 0.12 * (1 - clamp01(ph / 0.08)) if ph < 0.08 else 1.0
        x = W / 2 + (i - 1) * 110
        ellipse(f, (x - 34 + hop * 8, cy + 40, x + 34 - hop * 8, cy + 54), SHADOW + (int(60 * alpha),))
        blit(f, char2d(ic, "happy", 96), x, cy - hop * 46, sx=sq, sy=2 - sq, alpha=alpha)


def sign_transition(t):
    global SIGN
    if SIGN is None:
        SIGN = hanging_sign("Level %d" % LEVEL_NEXT, 440, 150, 460)
    blur_bg = veil(background("gameplay-daylight.jpg", 14), PARCH_TOP, 0.25)
    cover = ease_in_out(seg(t, 0.55, 0.9))
    uncover = ease_in_out(seg(t, 2.15, 2.55))
    base = crossfade(win_frame(t), blur_bg, cover)
    if uncover > 0:
        base = crossfade(blur_bg, game_frame(tiles=seg(t, 2.3, 2.9)), uncover)
    f = base
    # The sign drops in on its ropes, swings, then is pulled up.
    drop = spring(seg(t, 0.62, 2.0) * 1.4, freq=1.6, damp=4.5) if t >= 0.62 else 0.0
    up = ease_in_back(seg(t, 1.95, 2.3), s=0.8) * 1.3
    rest_y = 0.43 * H - 460 - 75  # the pivot so the sign's middle rests at 43% of H
    pivot_y = rest_y - (1 - drop) * 900 - up * 1100
    swing = 7 * math.exp(-2.6 * seg(t, 0.62, 3)) * math.sin((t - 0.62) * 5.2) if t >= 0.62 else 0
    if t >= 0.6 and up < 1:
        blit(f, SIGN, W / 2, pivot_y, rot=swing)
    hop_a = seg(t, 0.95, 1.2) * (1 - seg(t, 1.9, 2.1))
    if hop_a > 0:
        hoppers(f, t, 0.6 * H, hop_a)
    return f


# ======================================================================= 2. The lotus

def lotus_ring(f, cx, cy, r, lit, t, alpha=1.0, spin=0.0, n=12):
    for i in range(n):
        a = -math.pi / 2 + i * 2 * math.pi / n + spin
        x, y = cx + math.cos(a) * r, cy + math.sin(a) * r
        on = clamp01(lit * n - i)
        pop = 1 + 0.35 * math.sin(math.pi * on) if 0 < on < 1 else 1.0
        c = mix(LOTUS_TIP, LOTUS_FILL, on)
        P = layer(40, 56)
        ellipse(P, (2, 2, 38, 54), mix(LOTUS_LINE, PARCH_LINE, 1 - on) if on < 1 else LOTUS_LINE)
        ellipse(P, (5, 5, 35, 51), [(0, lighten(c, 0.35)), (1, c)])
        blit(f, P, x, y, scale=0.62 * pop, rot=-math.degrees(a) - 90, alpha=alpha)


def lotus_sprite(size):
    return asset_scaled("Icons/Resources/Icons/currency-lotus.png", size, size)


def iris(base, cover, cx, cy, r):
    """base inside the circle of radius r, cover outside, a lotus-pink rim on the edge."""
    if r <= 0:
        out = cover.copy()
        return out
    s = 2
    m = Image.new("L", (W // s, H // s), 0)
    ImageDraw.Draw(m).ellipse(((cx - r) / s, (cy - r) / s, (cx + r) / s, (cy + r) / s), fill=255)
    m = m.resize((W, H), Image.BILINEAR)
    out = Image.composite(base, cover, m)
    ring = layer(W, H)
    d = ImageDraw.Draw(ring)
    d.ellipse((cx - r - 6, cy - r - 6, cx + r + 6, cy + r + 6), outline=LOTUS_FILL + (255,), width=10)
    d.ellipse((cx - r - 8, cy - r - 8, cx + r + 8, cy + r + 8), outline=LOTUS_LINE + (255,), width=3)
    out.alpha_composite(ring)
    return out


DIAG = math.hypot(W, H) * 0.62


def lotus_boot(t):
    cx, cy = W / 2, 0.5 * H
    cover = RADIAL.copy()
    k = seg(t, 0.1, 0.6)
    blit(cover, logo(560), W / 2, 0.19 * H, scale=0.75 + 0.25 * ease_out_back(k), alpha=clamp01(k * 2))
    lk = seg(t, 0.3, 0.8)
    breathe = 1 + 0.04 * math.sin(t * 4.2)
    lit = boot_progress(t, 0.7, 2.75)
    lotus_ring(cover, cx, cy, 150, lit, t, alpha=ease_out_cubic(seg(t, 0.5, 0.9)))
    blit(cover, lotus_sprite(230), cx, cy, scale=ease_out_back(lk) * breathe, rot=4 * math.sin(t * 2.1),
         alpha=clamp01(lk * 2))
    text_center(cover, loading_dots(t), 40, cx, cy + 250, INK_SOFT, alpha=ease_out_cubic(seg(t, 0.7, 1.0)))
    open_k = ease_in_cubic(seg(t, 2.95, 3.5))
    if open_k <= 0:
        return crossfade(solid(W, H, PARCH_TOP), cover, ease_out_cubic(seg(t, 0, 0.3)))
    out = iris(home_frame(t), cover, cx, cy, open_k * DIAG)
    blit(out, lotus_sprite(230), cx, cy, scale=1 + open_k * 1.4, alpha=1 - open_k)
    return out


def lotus_transition(t):
    cx, cy = W / 2, 0.47 * H
    close_k = ease_in_cubic(seg(t, 0.55, 1.05))
    open_k = ease_in_cubic(seg(t, 2.05, 2.55))
    cover = RADIAL.copy()
    lk = seg(t, 0.95, 1.4)
    if lk > 0 and open_k < 1:
        lotus_ring(cover, cx, cy, 150, 1.0, t, alpha=seg(t, 1.1, 1.4) * 0.9, spin=t * 2.2, n=10)
        blit(cover, lotus_sprite(230), cx, cy, scale=ease_out_back(lk) * (1 + 0.04 * math.sin(t * 4)),
             rot=-30 * (1 - ease_out_cubic(lk)), alpha=clamp01(lk * 2))
        tk = ease_out_cubic(seg(t, 1.15, 1.5))
        text_center(cover, "Level %d" % LEVEL_NEXT, 72, cx, cy + 250 + 30 * (1 - tk), INK_TITLE, alpha=tk)
    if close_k < 1:
        return iris(win_frame(t), cover, cx, cy, (1 - close_k) * DIAG)
    if open_k <= 0:
        return cover
    out = iris(game_frame(), cover, cx, cy, open_k * DIAG)
    blit(out, lotus_sprite(230), cx, cy, scale=1 + open_k * 1.4, alpha=1 - open_k)
    return out


# ======================================================================= 3. The tile cascade

GRID = 90
COLS, ROWS = W // GRID, H // GRID + 1
rng = random.Random(7)
TILE_KIND = [[rng.choice(list(VARIANTS)) for _ in range(COLS)] for _ in range(ROWS)]


@functools.lru_cache(maxsize=None)
def cream_tile(size, vid):
    """A cream sticker tile with the variant's icon: calm enough to fill the screen."""
    L = layer(size, size)
    r = round(size * 0.16)
    rrect(L, (0, 0, size, size), r, CREAM_LINE)
    rrect(L, (2, 2, size - 2, size - 2), r - 2, CREAM_LIP)
    rrect(L, (2, 2, size - 2, size - 8), r - 2, [(0, CREAM_TOP), (1, CREAM_FACE)])
    s = round(size * 0.58)
    paste(L, with_alpha(icon("field", VARIANTS[vid][1], s), 0.9), (size - s) / 2, (size - s) / 2 - 4)
    return L


def cascade(f, k_in, k_out):
    """Covers the screen with candy tiles from the top left (k_in 0..1), then removes them in the same order."""
    span = 0.45
    for r in range(ROWS):
        for c in range(COLS):
            order = (c + r) / (COLS + ROWS - 2)
            a = clamp01((k_in - order * (1 - span)) / span)
            b = clamp01((k_out - order * (1 - span)) / span)
            if a <= 0 or b >= 1:
                continue
            s = ease_out_back(a) * (1 - ease_in_back(b) if b > 0 else 1)
            if s <= 0.02:
                continue
            tile = cream_tile(GRID - 4, TILE_KIND[r][c])
            blit(f, tile, c * GRID + GRID / 2 + (W - COLS * GRID) / 2, r * GRID + GRID / 2, scale=min(s, 1.15),
                 rot=(1 - a) * -25 + b * 25)


def mini_board(f, t, cx, cy, lit_count, wave=0.0, alpha=1.0):
    cell = 116
    cols, rows = 4, 2
    b = round(cell * 0.32)
    w, h = cols * cell + 2 * b, rows * cell + 2 * b
    L = layer(w + 24, h + 30)
    soft_shadow(L, (12, 20, 12 + w, 20 + h), b * 1.2, alpha=0.4, blur=9)
    rrect(L, (12, 12, 12 + w, 12 + h), b * 1.2, [(0, STONE_TOP), (0.5, STONE_FACE), (1, STONE_LIP)], STONE_LINE, 3)
    rrect(L, (12 + b - 3, 12 + b - 3, 12 + w - b + 3, 12 + h - b + 3), 6, STONE_LINE)
    rrect(L, (12 + b, 12 + b, 12 + w - b, 12 + h - b), 4, [(0, LAWN_LIGHT), (1, LAWN_DARK)])
    order = ["leaf", "flower", "water", "acorn", "moss", "violet_bud", "dew", "wood"]
    for i, vid in enumerate(order):
        k = clamp01(lit_count - i)
        if k <= 0:
            continue
        x, y = i % 4, i // 4
        bob = math.sin(max(0.0, wave * 2 * math.pi - (x + y) * 0.7)) * 10 if wave > 0 else 0
        blit(L, candy(cell, vid), 12 + b + x * cell + cell / 2, 12 + b + y * cell + cell / 2 - bob,
             scale=ease_out_back(clamp01(k * 1.6)))
    paste(f, L, cx - L.width / 2, cy - L.height / 2, alpha)


def tiles_boot(t):
    bg = veil(background("home.jpg", 12), PARCH_TOP, 0.3)
    f = bg.copy()
    k = seg(t, 0.1, 0.6)
    blit(f, logo(590), W / 2, 0.22 * H, scale=0.75 + 0.25 * ease_out_back(k), alpha=clamp01(k * 2))
    lit = 8 * boot_progress(t, 0.55, 2.6) + 0.0
    mini_board(f, t, W / 2, 0.52 * H, lit, wave=seg(t, 2.6, 3.0), alpha=ease_out_cubic(seg(t, 0.3, 0.6)))
    text_center(f, loading_dots(t), 40, W / 2, 0.66 * H, INK_TITLE, alpha=ease_out_cubic(seg(t, 0.5, 0.8)))
    f = crossfade(solid(W, H, PARCH_TOP), f, ease_out_cubic(seg(t, 0, 0.3)))
    kin = seg(t, 2.95, 3.4)
    kout = seg(t, 3.45, 3.95)
    if kout > 0:
        f = home_frame(t)
    if kin > 0:
        cascade(f, kin, kout)
    return f


def tiles_transition(t):
    kin = seg(t, 0.55, 1.15)
    kout = seg(t, 2.05, 2.65)
    f = win_frame(t) if kin < 1 and kout <= 0 else game_frame()
    if kin >= 1 and kout <= 0:
        f = background("gameplay-daylight.jpg").copy()
    cascade(f, kin, kout)
    pk = seg(t, 1.05, 1.4)
    pout = seg(t, 1.9, 2.15)
    if pk > 0 and pout < 1:
        card = parchment(440, 150, r=40)
        blit(card, text_sprite("Level %d" % LEVEL_NEXT, 74, INK_TITLE, stroke=2, stroke_fill=PARCH_TOP), 232, 84)
        s = ease_out_back(pk) * (1 - ease_in_back(pout)) * (1 + 0.025 * math.sin(t * 6))
        blit(f, card, W / 2, 0.45 * H, scale=s)
    return f


# ======================================================================= 4. The heroes and the petal gust

FAMS = ("sprig", "bloom", "drop", "twig")


def hero_row(f, t, cy, alpha=1.0):
    xs = (0.17, 0.39, 0.615, 0.835)
    for i, fam in enumerate(FAMS):
        ph = (t * 0.85 - i * 0.2) % 1.0
        air = clamp01((ph - 0.05) / 0.3)
        hop = math.sin(math.pi * air) if 0 < air < 1 else 0.0
        squash = 0.0
        if 0 <= ph < 0.06:
            squash = math.sin(math.pi * ph / 0.06)
        elif 0.35 <= ph < 0.43:
            squash = math.sin(math.pi * (ph - 0.35) / 0.08)
        cell = 250 / hero_fill(fam) * (0.92 if fam == "twig" else 1.0)
        x = W * xs[i]
        sw = 100 * (1 - 0.35 * hop)
        ellipse(f, (x - sw, cy - 14, x + sw, cy + 14), SHADOW + (int(70 * alpha),))
        draw_hero(f, fam, "idle", t + i * 0.9, x, cy + 6, cell, alpha=alpha, rise=hop * 90,
                  sx=1 + 0.08 * squash, sy=1 - 0.08 * squash)


def petal_sprite(kind, size):
    if kind == "leaf":
        return icon("field", "leaf", size)
    # A petal: a rounded base and a soft point (a lens with one blunt end), pale tip to pink base.
    w, h = size, round(size * 1.5)
    s = 4
    m = Image.new("L", (w * s, h * s), 0)
    pts = []
    for i in range(64):
        a = 2 * math.pi * i / 64
        x = math.sin(a)
        y = -math.cos(a)
        r = 1.0 - 0.32 * max(0.0, -y) ** 2
        pts.append(((0.5 + 0.48 * x * r * (0.75 + 0.25 * (1 - y) / 2)) * w * s, (0.5 + 0.48 * y) * h * s))
    ImageDraw.Draw(m).polygon(pts, fill=255)
    m = m.resize((w, h), Image.LANCZOS)
    body = vgrad(w, h, [(0, LOTUS_TIP), (0.6, lighten(LOTUS_FILL, 0.3)), (1, LOTUS_FILL)])
    P = masked(body, m)
    vein = layer(w, h)
    ImageDraw.Draw(vein).line((w / 2, h * 0.25, w / 2, h * 0.9), fill=LOTUS_LINE + (90,), width=max(1, w // 14))
    P.alpha_composite(vein)
    return masked(P, m)


PETALS = []
prng = random.Random(3)
for i in range(150):
    PETALS.append(dict(kind="leaf" if prng.random() < 0.28 else "petal", size=prng.randint(30, 62),
                       y0=prng.uniform(-0.1, 1.45) * H, delay=prng.uniform(0, 0.3), speed=prng.uniform(0.9, 1.2),
                       spin=prng.uniform(-300, 300), rot0=prng.uniform(0, 360), wob=prng.uniform(0, 6.28)))
PETAL_CACHE = {}


def gust(f, k, alpha=1.0):
    """A gust of petals and leaves across the screen; k 0..1 (the middle covers the screen most)."""
    for p in PETALS:
        x = k * 1.3 - p["delay"]
        if x <= 0 or x >= 1:
            continue
        px = -160 + (W + 320) * (x * p["speed"])
        py = p["y0"] - x * 0.55 * H + math.sin(x * 8 + p["wob"]) * 40
        key = (p["kind"], p["size"])
        if key not in PETAL_CACHE:
            PETAL_CACHE[key] = petal_sprite(*key)
        blit(f, PETAL_CACHE[key], px, py, rot=p["rot0"] + p["spin"] * x, alpha=alpha)


def heroes_boot(t):
    bg = veil(background("home.jpg", 6), PARCH_TOP, 0.15)
    f = bg.copy()
    k = seg(t, 0.1, 0.6)
    blit(f, logo(590), W / 2, 0.18 * H, scale=0.75 + 0.25 * ease_out_back(k), alpha=clamp01(k * 2))
    hero_row(f, t, 0.6 * H, alpha=ease_out_cubic(seg(t, 0.25, 0.7)))
    p = boot_progress(t, 0.6, 2.8)
    bar_in = ease_out_cubic(seg(t, 0.45, 0.8))
    progress_bar(f, W / 2, 0.72 * H + 30 * (1 - bar_in), 420, 46, p, alpha=bar_in, t=t)
    text_center(f, loading_dots(t), 38, W / 2, 0.775 * H + 30 * (1 - bar_in), INK_TITLE, alpha=bar_in)
    f = crossfade(solid(W, H, PARCH_TOP), f, ease_out_cubic(seg(t, 0, 0.3)))
    g = seg(t, 2.8, 3.75)
    if g > 0:
        swap = ease_in_out(seg(t, 3.05, 3.35))
        f = crossfade(f, home_frame(t), swap)
        gust(f, g)
    return f


def heroes_transition(t):
    g_in = seg(t, 0.45, 1.35)
    g_out = seg(t, 1.8, 2.7)
    cover = ease_in_out(seg(t, 0.7, 1.0))
    uncover = ease_in_out(seg(t, 1.95, 2.25))
    soft = veil(background("win.jpg", 14), PARCH_TOP, 0.35)
    f = crossfade(win_frame(t), soft, cover)
    if uncover > 0:
        f = crossfade(soft, game_frame(tiles=seg(t, 2.1, 2.7)), uncover)
    pk = seg(t, 1.0, 1.35)
    pout = seg(t, 1.8, 2.05)
    if pk > 0 and pout < 1:
        s = ease_out_back(pk) * (1 - ease_in_back(pout))
        L = layer(W, 700)
        pl = plank(420, 140, "Level %d" % LEVEL_NEXT, 70)
        paste(L, pl, (W - pl.width) / 2, 380)
        # The next level's hero hops on the sign.
        ph = (t - 1.0) * 1.8 % 1.0
        hop = math.sin(math.pi * clamp01(ph / 0.55)) if ph < 0.55 else 0
        cell = 330 / hero_fill("bloom")
        draw_hero(L, "bloom", "idle", t, W / 2, 400, cell, rise=hop * 70)
        blit(f, L, W / 2, 0.42 * H, scale=s)
    gust(f, g_in)
    gust(f, g_out)
    return f


# ======================================================================= output

CONCEPTS = {
    "1-sign": ("Вывеска", sign_boot, 4.0, sign_transition, 3.5),
    "2-lotus": ("Лотос", lotus_boot, 4.0, lotus_transition, 3.4),
    "3-tiles": ("Плитки", tiles_boot, 4.4, tiles_transition, 3.4),
    "4-heroes": ("Герои и лепестки", heroes_boot, 4.2, heroes_transition, 3.3),
}


def render(fn, seconds, folder):
    os.makedirs(folder, exist_ok=True)
    frames = []
    n = round(seconds * FPS)
    for i in range(n):
        im = finish(fn(i / FPS))
        im.save("%s/%03d.png" % (folder, i))
        frames.append(im)
    return frames


def encode(folder, out):
    subprocess.run(["ffmpeg", "-loglevel", "error", "-y", "-framerate", str(FPS), "-i", folder + "/%03d.png", "-vf",
                    "split[a][b];[a]palettegen=max_colors=256:stats_mode=full[p];[b][p]paletteuse=dither=bayer:bayer_scale=4",
                    "-loop", "0", out], check=True)


if __name__ == "__main__":
    out = sys.argv[1]
    names = sys.argv[2:] or list(CONCEPTS)
    for name in names:
        title, boot, bs, trans, ts = CONCEPTS[name]
        for kind, fn, secs in (("boot", boot, bs), ("level", trans, ts)):
            folder = "%s/frames/%s-%s" % (out, name, kind)
            render(fn, secs, folder)
            encode(folder, "%s/%s-%s.gif" % (out, name, kind))
            print(name, kind, os.path.getsize("%s/%s-%s.gif" % (out, name, kind)) // 1024, "KB", flush=True)

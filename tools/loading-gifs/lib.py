"""Shared drawing kit for the loading-screen GIF concepts.

Everything is drawn from the game's own assets on the style branch (GD env var): backgrounds, the logo, the animated
hero frames, the field/variant icons, the Nunito font, and the design tokens of DesignTokens.cs. Frames are drawn at
2x (720 x 1560) and scaled down to 360 x 780 for the GIF.
"""
import functools
import json
import math
import os
import re

import numpy as np
from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageFont

GD = os.environ["GD"]
ART = GD + "/client/Assets/Bloomlings/Art"
DESIGN = GD + "/client/Assets/Bloomlings/UI/Design"
FONT = GD + "/client/Assets/Bloomlings/UI/Fonts/Resources/Nunito-ExtraBold.ttf"
PICTURES = GD + "/content/pictures/lib"

W, H = 720, 1560
FPS = 25


def hx(s):
    s = s.lstrip("#")
    return tuple(int(s[i:i + 2], 16) for i in (0, 2, 4))


def lighten(c, k):
    return tuple(round(v + (255 - v) * k) for v in c[:3])


def darken(c, k):
    return tuple(round(v * (1 - k)) for v in c[:3])


def mix(a, b, k):
    return tuple(round(a[i] + (b[i] - a[i]) * k) for i in range(3))


# Design tokens (DesignTokens.cs on the style branch).
WOOD_LIGHT, WOOD_MID, WOOD_GRAIN, WOOD_EDGE, WOOD_LINE = hx("#FBE2BC"), hx("#F1CD98"), hx("#C99863"), hx("#DDB27C"), hx("#8B5A2B")
WOOD_DARK, WOOD_DARK_TOP, WOOD_DARK_LINE = hx("#8A5634"), hx("#A86F45"), hx("#4A2A14")
STONE_TOP, STONE_FACE, STONE_LIP, STONE_LINE = hx("#FCE8C6"), hx("#F1D5A8"), hx("#D9B585"), hx("#7E6844")
PARCH_TOP, PARCH_BOTTOM, PARCH_EDGE, PARCH_LINE = hx("#FFF8E8"), hx("#F5E4C3"), hx("#EBCB9A"), hx("#B48552")
CREAM_FACE, CREAM_TOP, CREAM_LIP, CREAM_LINE = hx("#FCE7C8"), hx("#FFF6E6"), hx("#E6C69B"), hx("#C79F6F")
INK_BROWN, INK_TITLE, INK_SOFT = hx("#3A2416"), hx("#6E3416"), hx("#7B5A3A")
LOTUS_FILL, LOTUS_TIP, LOTUS_LINE = hx("#F7739F"), hx("#FFE4EE"), hx("#D14F7A")
BTN, BTN_TOP, BTN_EDGE, LEAF_LINE = hx("#62B83A"), hx("#ADE162"), hx("#378F24"), hx("#2F6B22")
LAWN_LIGHT, LAWN_DARK = hx("#9CC842"), hx("#64982D")
BADGE = hx("#3B2A1A")
SHADOW = hx("#3C2814")

# The 8 launch variants (VariantCatalog on the style branch): color and icon id.
VARIANTS = {
    "leaf": (hx("#99D323"), "leaf"), "moss": (hx("#0FB198"), "moss"),
    "flower": (hx("#FF3B89"), "flower"), "violet_bud": (hx("#7F3CC4"), "bud"),
    "water": (hx("#3485E7"), "drop"), "dew": (hx("#61DAE1"), "dew"),
    "wood": (hx("#9B4904"), "log"), "acorn": (hx("#CF7F20"), "acorn"),
}
GROUPS = {"green": ["leaf", "moss"], "pink_purple": ["flower", "violet_bud"], "blue_cyan": ["water", "dew"],
          "brown_orange": ["wood", "acorn"]}


# ---------------------------------------------------------------- easing

def clamp01(x):
    return 0.0 if x < 0 else 1.0 if x > 1 else x


def seg(t, a, b):
    """0..1 progress of t through [a, b]."""
    return clamp01((t - a) / (b - a)) if b > a else float(t >= b)


def ease_out_cubic(x):
    return 1 - (1 - x) ** 3


def ease_in_cubic(x):
    return x ** 3


def ease_in_out(x):
    return 4 * x ** 3 if x < 0.5 else 1 - (-2 * x + 2) ** 3 / 2


def ease_out_back(x, s=1.70158):
    x -= 1
    return x * x * ((s + 1) * x + s) + 1


def ease_in_back(x, s=1.70158):
    return x * x * ((s + 1) * x - s)


def spring(x, freq=2.6, damp=5.0):
    """0 -> 1 with a damped overshoot (x in seconds-ish units of 0..1)."""
    if x <= 0:
        return 0.0
    return 1 - math.exp(-damp * x) * math.cos(freq * 2 * math.pi * x)


# ---------------------------------------------------------------- raster helpers

def layer(w, h):
    return Image.new("RGBA", (max(1, int(w)), max(1, int(h))), (0, 0, 0, 0))


def solid(w, h, c, a=255):
    return Image.new("RGBA", (max(1, int(w)), max(1, int(h))), tuple(c[:3]) + (a,))


@functools.lru_cache(maxsize=None)
def rr_mask(w, h, r):
    s = 4
    m = Image.new("L", (w * s, h * s), 0)
    ImageDraw.Draw(m).rounded_rectangle([0, 0, w * s - 1, h * s - 1], max(0, r) * s, fill=255)
    return m.resize((w, h), Image.LANCZOS)


@functools.lru_cache(maxsize=None)
def ellipse_mask(w, h):
    s = 4
    m = Image.new("L", (w * s, h * s), 0)
    ImageDraw.Draw(m).ellipse([0, 0, w * s - 1, h * s - 1], fill=255)
    return m.resize((w, h), Image.LANCZOS)


def vgrad(w, h, stops):
    w, h = max(1, int(w)), max(1, int(h))
    ys = np.linspace(0, 1, h)
    out = np.zeros((h, 3))
    pos = [p for p, _ in stops]
    for ch in range(3):
        out[:, ch] = np.interp(ys, pos, [c[ch] for _, c in stops])
    arr = np.repeat(out[:, None, :], w, axis=1).astype(np.uint8)
    return Image.fromarray(arr, "RGB").convert("RGBA")


def with_alpha(img, alpha):
    if alpha >= 0.999:
        return img
    img = img.copy()
    a = np.asarray(img.getchannel("A"), dtype=np.float32) * max(0.0, alpha)
    img.putalpha(Image.fromarray(a.astype(np.uint8), "L"))
    return img


def paste(dst, src, x, y, alpha=1.0):
    """Alpha-composites src onto dst at (x, y), clipped to dst."""
    x, y = int(round(x)), int(round(y))
    if alpha <= 0.004:
        return
    sx0, sy0 = max(0, -x), max(0, -y)
    sx1, sy1 = min(src.width, dst.width - x), min(src.height, dst.height - y)
    if sx1 <= sx0 or sy1 <= sy0:
        return
    part = src.crop((sx0, sy0, sx1, sy1)) if (sx0, sy0, sx1, sy1) != (0, 0, src.width, src.height) else src
    dst.alpha_composite(with_alpha(part, alpha), (x + sx0, y + sy0))


def masked(src, mask):
    """src (RGBA) with its alpha multiplied by mask (L)."""
    out = src.copy()
    out.putalpha(ImageChops.multiply(src.getchannel("A"), mask))
    return out


def fill_shape(dst, box, mask_fn, fill, alpha=1.0):
    x0, y0, x1, y1 = [int(round(v)) for v in box]
    w, h = x1 - x0, y1 - y0
    if w <= 0 or h <= 0:
        return
    if isinstance(fill, list):
        src = vgrad(w, h, fill)
    elif isinstance(fill, Image.Image):
        src = fill.resize((w, h)) if fill.size != (w, h) else fill
    else:
        src = solid(w, h, fill[:3], fill[3] if len(fill) > 3 else 255)
    paste(dst, masked(src, mask_fn(w, h)), x0, y0, alpha)


def rrect(dst, box, r, fill, outline=None, ow=0, alpha=1.0):
    x0, y0, x1, y1 = box
    if outline is not None and ow > 0:
        fill_shape(dst, box, lambda w, h: rr_mask(w, h, int(r)), outline, alpha)
        x0, y0, x1, y1 = x0 + ow, y0 + ow, x1 - ow, y1 - ow
        r = max(1, r - ow)
    fill_shape(dst, (x0, y0, x1, y1), lambda w, h: rr_mask(w, h, int(r)), fill, alpha)


def ellipse(dst, box, fill, alpha=1.0):
    fill_shape(dst, box, ellipse_mask, fill, alpha)


def soft_shadow(dst, box, r, alpha=0.3, blur=10, color=SHADOW):
    x0, y0, x1, y1 = [int(v) for v in box]
    pad = blur * 3
    L = layer(x1 - x0 + 2 * pad, y1 - y0 + 2 * pad)
    rrect(L, (pad, pad, pad + x1 - x0, pad + y1 - y0), r, color)
    L = L.filter(ImageFilter.GaussianBlur(blur))
    paste(dst, L, x0 - pad, y0 - pad, alpha)


@functools.lru_cache(maxsize=None)
def font(size):
    return ImageFont.truetype(FONT, int(size))


def text_sprite(s, size, fill, stroke=0, stroke_fill=None, shadow=None):
    """A text sprite; shadow = (dx, dy, color, alpha)."""
    f = font(size)
    l, t, r, b = f.getbbox(s, stroke_width=stroke)
    pad = stroke + 8 + (max(abs(shadow[0]), abs(shadow[1])) if shadow else 0)
    w, h = r - l + 2 * pad, b - t + 2 * pad
    L = layer(w, h)
    if shadow:
        S = layer(w, h)
        ImageDraw.Draw(S).text((pad - l + shadow[0], pad - t + shadow[1]), s, font=f, fill=tuple(shadow[2]) + (255,),
                               stroke_width=stroke, stroke_fill=tuple(shadow[2]) + (255,))
        paste(L, S, 0, 0, shadow[3])
    T = layer(w, h)
    ImageDraw.Draw(T).text((pad - l, pad - t), s, font=f, fill=tuple(fill) + (255,), stroke_width=stroke,
                           stroke_fill=tuple(stroke_fill or fill) + (255,))
    L.alpha_composite(T)
    return L


def blit(dst, spr, cx, cy, scale=1.0, rot=0.0, alpha=1.0, sx=None, sy=None):
    """Draws a sprite centered at (cx, cy), scaled (or squashed with sx/sy), rotated in degrees."""
    if alpha <= 0.004:
        return
    fx = scale if sx is None else scale * sx
    fy = scale if sy is None else scale * sy
    if fx <= 0.01 or fy <= 0.01:
        return
    s = spr
    if abs(fx - 1) > 1e-3 or abs(fy - 1) > 1e-3:
        s = s.resize((max(1, round(s.width * fx)), max(1, round(s.height * fy))), Image.BICUBIC)
    if abs(rot) > 0.05:
        s = s.rotate(rot, Image.BICUBIC, expand=True)
    paste(dst, s, cx - s.width / 2, cy - s.height / 2, alpha)


def blit_at(dst, spr, x, y, w=None, h=None, alpha=1.0):
    if w is not None or h is not None:
        if w is None:
            w = spr.width * h / spr.height
        if h is None:
            h = spr.height * w / spr.width
        spr = spr.resize((max(1, round(w)), max(1, round(h))), Image.LANCZOS)
    paste(dst, spr, x, y, alpha)


# ---------------------------------------------------------------- assets

@functools.lru_cache(maxsize=None)
def asset(path):
    return Image.open(ART + "/" + path).convert("RGBA")


@functools.lru_cache(maxsize=None)
def asset_scaled(path, w, h):
    return asset(path).resize((w, h), Image.LANCZOS)


def cover(path, w=W, h=H):
    im = asset(path)
    s = max(w / im.width, h / im.height)
    sw, sh = round(im.width * s), round(im.height * s)
    im = im.resize((sw, sh), Image.LANCZOS)
    return im.crop(((sw - w) // 2, (sh - h) // 2, (sw - w) // 2 + w, (sh - h) // 2 + h))


@functools.lru_cache(maxsize=None)
def background(name, blur=0):
    im = cover("Backgrounds/Resources/Backgrounds/" + name)
    return im.filter(ImageFilter.GaussianBlur(blur)) if blur else im


@functools.lru_cache(maxsize=None)
def icon(kind, icon_id, size):
    """kind: field | variant (the owner's icons)."""
    return asset("Icons/Resources/Icons/%s-%s.png" % (kind, icon_id)).resize((size, size), Image.LANCZOS)


@functools.lru_cache(maxsize=None)
def char2d(icon_id, mood, size):
    return asset("Characters/Resources/Characters/2d/%s-%s.png" % (icon_id, mood)).resize((size, size), Image.LANCZOS)


@functools.lru_cache(maxsize=None)
def logo(width):
    im = asset("Brand/Resources/Brand/logo.png")
    return im.resize((width, round(im.height * width / im.width)), Image.LANCZOS)


# ---------------------------------------------------------------- heroes (tools/heroanim frames)

def _clip_table():
    src = open(DESIGN + "/HeroMotionData.cs").read()
    table = {}
    for m in re.finditer(r'new ClipData\("(\w+)", "(\w+)",\s*new short\[\]\s*\{([^}]*)\}', src):
        nums = [int(v) for v in re.findall(r"-?\d+", m.group(3))]
        table[(m.group(1), m.group(2))] = [tuple(nums[i:i + 4]) for i in range(0, len(nums), 4)]
    return table


CLIPS = _clip_table()
CELL_W, CELL_H, FOOT = 448, 504, 0.9
HERO_FPS = 24


@functools.lru_cache(maxsize=4096)
def hero_frame(family, clip, index, scale_key):
    """The frame placed in its cell, scaled; scale_key = cell height in px."""
    crops = CLIPS[(family, clip)]
    x, y, w, h = crops[index % len(crops)]
    im = Image.open("%s/Heroes/Resources/HeroMotion/%s-%s-%02d.png" % (ART, family, clip, index % len(crops))).convert("RGBA")
    s = scale_key / CELL_H
    cell = layer(round(CELL_W * s), scale_key)
    im = im.resize((max(1, round(w * s)), max(1, round(h * s))), Image.LANCZOS)
    paste(cell, im, x * s, y * s)
    return cell


def hero_index(family, clip, t):
    n = len(CLIPS[(family, clip)])
    return int(t * HERO_FPS) % n


def draw_hero(dst, family, clip, t, feet_x, feet_y, cell_h, alpha=1.0, rise=0.0, sx=1.0, sy=1.0):
    cell_h = int(round(cell_h))
    fr = hero_frame(family, clip, hero_index(family, clip, t), cell_h)
    if sx != 1.0 or sy != 1.0:
        fr = fr.resize((max(1, round(fr.width * sx)), max(1, round(fr.height * sy))), Image.BICUBIC)
    paste(dst, fr, feet_x - fr.width / 2, feet_y - fr.height * FOOT - rise, alpha)


def hero_fill(family, clip="idle"):
    return ((FOOT * CELL_H) - CLIPS[(family, clip)][0][1]) / CELL_H


def hero_seam_width(family, clip="idle"):
    return CLIPS[(family, clip)][0][2] / CELL_W


# ---------------------------------------------------------------- kit elements

def plank(w, h, label, size, rot_grain=0):
    """The wooden sign (Home's level plaque, the win's title)."""
    L = layer(w + 24, h + 30)
    soft_shadow(L, (12, 20, 12 + w, 20 + h), h * 0.32, alpha=0.35, blur=7)
    rrect(L, (12, 12, 12 + w, 12 + h), h * 0.32, [(0, WOOD_LIGHT), (0.55, WOOD_MID), (1, WOOD_EDGE)], WOOD_LINE, 4)
    d = ImageDraw.Draw(L)
    for i, fy in enumerate((0.3, 0.72)):
        yy = 12 + h * fy
        d.arc((12 + w * 0.08, yy - 6, 12 + w * 0.5, yy + 6), 200, 340, fill=WOOD_GRAIN + (110,), width=2)
        d.arc((12 + w * 0.55, yy - 4, 12 + w * 0.92, yy + 8), 190, 330, fill=WOOD_GRAIN + (90,), width=2)
    rrect(L, (12 + 10, 12 + 7, 12 + w - 10, 12 + 12), 3, (255, 255, 255), alpha=0.35)
    t = text_sprite(label, size, INK_TITLE, stroke=2, stroke_fill=WOOD_LIGHT)
    blit(L, t, 12 + w / 2, 12 + h / 2 + 2)
    return L


def green_button(w, h, label, size, press=0.0):
    """The primary button: a wood rim, a green raised face, a white label."""
    L = layer(w + 24, h + 30)
    soft_shadow(L, (12, 22, 12 + w, 22 + h), h * 0.45, alpha=0.35, blur=8)
    rrect(L, (12, 12, 12 + w, 12 + h), h * 0.45, [(0, WOOD_LIGHT), (1, WOOD_EDGE)], WOOD_LINE, 4)
    inset = 11
    lip = round(h * 0.09 * (1 - press))
    top = 12 + inset + round(h * 0.06 * press)
    rrect(L, (12 + inset, top, 12 + w - inset, 12 + h - inset), h * 0.4, BTN_EDGE, LEAF_LINE, 3)
    rrect(L, (12 + inset + 3, top + 3, 12 + w - inset - 3, 12 + h - inset - 3 - lip), h * 0.38,
          [(0, BTN_TOP), (0.55, BTN), (1, darken(BTN, 0.05))])
    rrect(L, (12 + inset + h * 0.2, top + 8, 12 + w - inset - h * 0.2, top + 8 + h * 0.16), h * 0.08, (255, 255, 255),
          alpha=0.35)
    t = text_sprite(label, size, (255, 255, 255), stroke=5, stroke_fill=LEAF_LINE, shadow=(0, 4, LEAF_LINE, 0.8))
    blit(L, t, 12 + w / 2, top + (h - inset * 2 - lip) / 2 + inset - 6)
    return L


def parchment(w, h, r=None, line=PARCH_LINE):
    r = r if r is not None else min(w, h) * 0.18
    L = layer(w + 24, h + 30)
    soft_shadow(L, (12, 20, 12 + w, 20 + h), r, alpha=0.3, blur=8)
    rrect(L, (12, 12, 12 + w, 12 + h), r, [(0, PARCH_TOP), (1, PARCH_BOTTOM)], line, 3)
    rrect(L, (12 + 6, 12 + 6, 12 + w - 6, 12 + h - 6), r - 5, (0, 0, 0, 0))
    return L


def cream_round(d, glyph=None):
    L = layer(d + 20, d + 26)
    soft_shadow(L, (10, 16, 10 + d, 16 + d), d / 2, alpha=0.3, blur=6)
    ellipse(L, (10, 10, 10 + d, 10 + d), CREAM_LINE)
    ellipse(L, (13, 13, 7 + d, 7 + d), CREAM_LIP)
    ellipse(L, (13, 13, 7 + d, 1 + d), [(0, CREAM_TOP), (1, CREAM_FACE)])
    if glyph is not None:
        blit(L, glyph, 10 + d / 2, 8 + d / 2)
    return L


def cream_pill(w, h, content=None):
    L = layer(w + 20, h + 26)
    soft_shadow(L, (10, 16, 10 + w, 16 + h), h / 2, alpha=0.3, blur=6)
    rrect(L, (10, 10, 10 + w, 10 + h), h / 2, CREAM_LINE)
    rrect(L, (13, 13, 7 + w, 7 + h), h / 2 - 3, CREAM_LIP)
    rrect(L, (13, 13, 7 + w, 3 + h), h / 2 - 3, [(0, CREAM_TOP), (1, CREAM_FACE)])
    return L


@functools.lru_cache(maxsize=None)
def candy(size, vid, flat=False, symbol=True, light=0.0):
    """The candy tile (look.md §3.1): a satin face, a lip, a crisp outline, the field icon as its symbol."""
    c = VARIANTS[vid][0]
    if light:
        c = lighten(c, light)
    L = layer(size, size)
    r = max(3, round(size * 0.07))
    ow = max(1, round(size * 0.03))
    rrect(L, (0, 0, size, size), r, darken(c, 0.45))
    lip = 0 if flat else max(1, round(size * 0.07))
    if lip:
        rrect(L, (ow, ow, size - ow, size - ow), r - 1, darken(c, 0.28))
    rrect(L, (ow, ow, size - ow, size - ow - lip), r - 1,
          [(0, lighten(c, 0.36)), (0.6, c), (1, darken(c, 0.14))])
    rrect(L, (size * 0.1, size * 0.06, size * 0.9, size * 0.24), size * 0.06, (255, 255, 255), alpha=0.18)
    if symbol:
        s = round(size * 0.62)
        ic = icon("field", VARIANTS[vid][1], s)
        if light:
            ic = with_alpha(ic, 0.55)
        paste(L, ic, (size - s) / 2, (size - s) / 2 - size * 0.03 - lip / 2)
    return L


def picture(name):
    p = json.load(open(PICTURES + "/" + name + ".json"))
    used = {}
    roles = []
    for r in p["roles"]:
        g = r["colorGroup"]
        n = used.get(g, 0)
        used[g] = n + 1
        roles.append(GROUPS[g][n % 2])
    return p["grid"], roles


@functools.lru_cache(maxsize=None)
def board(name, cell, finished=False):
    """A level board: candy tiles in a stone border over the lawn (finished: flat tiles, the restored look)."""
    grid, roles = picture(name)
    rows, cols = len(grid), len(grid[0])
    b = round(cell * 0.32)
    w, h = cols * cell + 2 * b, rows * cell + 2 * b
    L = layer(w + 24, h + 30)
    soft_shadow(L, (12, 20, 12 + w, 20 + h), b * 1.2, alpha=0.4, blur=9)
    rrect(L, (12, 12, 12 + w, 12 + h), b * 1.2, [(0, STONE_TOP), (0.5, STONE_FACE), (1, STONE_LIP)], STONE_LINE, 3)
    rrect(L, (12 + b - 3, 12 + b - 3, 12 + w - b + 3, 12 + h - b + 3), 6, STONE_LINE)
    rrect(L, (12 + b, 12 + b, 12 + w - b, 12 + h - b), 4, [(0, LAWN_LIGHT), (1, LAWN_DARK)])
    for y, row in enumerate(grid):
        for x, v in enumerate(row):
            if v < 0:
                continue
            t = candy(cell, roles[v], flat=finished, symbol=not finished, light=0.0)
            paste(L, t, 12 + b + x * cell, 12 + b + y * cell)
    return L


def board_cells(name, cell):
    """Each tile's top-left in the board sprite (for the pop-in animations)."""
    grid, roles = picture(name)
    b = round(cell * 0.32)
    out = []
    for y, row in enumerate(grid):
        for x, v in enumerate(row):
            if v >= 0:
                out.append((x, y, roles[v], 12 + b + x * cell, 12 + b + y * cell))
    return out, len(grid[0]), len(grid)


def board_empty(name, cell):
    grid, _ = picture(name)
    empty = [[-1] * len(grid[0]) for _ in grid]
    key = "_empty_" + name
    rows, cols = len(grid), len(grid[0])
    b = round(cell * 0.32)
    w, h = cols * cell + 2 * b, rows * cell + 2 * b
    L = layer(w + 24, h + 30)
    soft_shadow(L, (12, 20, 12 + w, 20 + h), b * 1.2, alpha=0.4, blur=9)
    rrect(L, (12, 12, 12 + w, 12 + h), b * 1.2, [(0, STONE_TOP), (0.5, STONE_FACE), (1, STONE_LIP)], STONE_LINE, 3)
    rrect(L, (12 + b - 3, 12 + b - 3, 12 + w - b + 3, 12 + h - b + 3), 6, STONE_LINE)
    rrect(L, (12 + b, 12 + b, 12 + w - b, 12 + h - b), 4, [(0, LAWN_LIGHT), (1, LAWN_DARK)])
    return L


def badge(n, d=34):
    L = layer(d + 6, d + 6)
    ellipse(L, (0, 0, d + 6, d + 6), lighten(PARCH_TOP, 0.5))
    ellipse(L, (3, 3, d + 3, d + 3), BADGE)
    t = text_sprite(str(n), round(d * 0.62), (255, 255, 255))
    blit(L, t, (d + 6) / 2, (d + 6) / 2 + 1)
    return L


def pod(w, h, vid, count):
    """A Source pod: a wooden frame wider than tall, the variant's icon in the middle, its count in a corner."""
    L = layer(w + 20, h + 26)
    soft_shadow(L, (10, 16, 10 + w, 16 + h), h * 0.2, alpha=0.35, blur=6)
    rrect(L, (10, 10, 10 + w, 10 + h), h * 0.2, [(0, WOOD_DARK_TOP), (1, WOOD_DARK)], WOOD_DARK_LINE, 3)
    rrect(L, (10 + w * 0.12, 10 + h * 0.12, 10 + w * 0.88, 10 + h * 0.86), h * 0.14, [(0, PARCH_TOP), (1, PARCH_BOTTOM)],
          WOOD_DARK_LINE, 2)
    ic = icon("variant", VARIANTS[vid][1], round(h * 0.62))
    paste(L, ic, 10 + (w - ic.width) / 2, 10 + (h - ic.height) / 2 - 1)
    bd = badge(count, round(h * 0.3))
    paste(L, bd, 10 + w - bd.width + 4, 10 + h - bd.height + 4)
    return L


def slot(size, vid=None, count=None):
    L = layer(size + 20, size + 40)
    soft_shadow(L, (10, 16, 10 + size, 16 + size), size * 0.2, alpha=0.25, blur=5)
    rrect(L, (10, 10, 10 + size, 10 + size), size * 0.2, CREAM_LINE)
    rrect(L, (13, 13, 7 + size, 7 + size), size * 0.2 - 3, CREAM_LIP)
    rrect(L, (13, 13, 7 + size, 3 + size), size * 0.2 - 3, [(0, CREAM_TOP), (1, CREAM_FACE)])
    if vid:
        t = candy(round(size * 0.66), vid, symbol=True)
        paste(L, t, 10 + (size - t.width) / 2, 10 + size * 0.1)
        ct = text_sprite(str(count), round(size * 0.22), INK_BROWN)
        blit(L, ct, 10 + size / 2, 10 + size * 0.86)
    return L


def gear_glyph(d, c):
    s = 4
    L = Image.new("L", (d * s, d * s), 0)
    g = ImageDraw.Draw(L)
    cx = cy = d * s / 2
    for i in range(8):
        a = i * math.pi / 4
        pts = []
        for da, r in ((-0.2, 0.36), (-0.13, 0.5), (0.13, 0.5), (0.2, 0.36)):
            pts.append((cx + math.cos(a + da) * r * d * s, cy + math.sin(a + da) * r * d * s))
        g.polygon(pts, fill=255)
    g.ellipse((cx - 0.38 * d * s, cy - 0.38 * d * s, cx + 0.38 * d * s, cy + 0.38 * d * s), fill=255)
    g.ellipse((cx - 0.15 * d * s, cy - 0.15 * d * s, cx + 0.15 * d * s, cy + 0.15 * d * s), fill=0)
    m = L.resize((d, d), Image.LANCZOS)
    return masked(solid(d, d, c), m)


def speed_glyph(d, c):
    s = 4
    L = Image.new("L", (d * s, d * s), 0)
    g = ImageDraw.Draw(L)
    for ox in (0.08, 0.5):
        g.polygon([(ox * d * s, 0.22 * d * s), ((ox + 0.42) * d * s, 0.5 * d * s), (ox * d * s, 0.78 * d * s)], fill=255)
    m = L.resize((d, d), Image.LANCZOS)
    return masked(solid(d, d, c), m)


# ---------------------------------------------------------------- screens

class HomeStage:
    """The layered Home of spec 005 (HomeLayers.cs): the garden, the fountain, the four heroes, the petals."""

    PW, PH = 852, 1846
    PLACEMENT = {"bloom": (0.5, 0.49, 0.40), "drop": (0.735, 0.532, 0.36), "sprig": (0.235, 0.56, 0.40),
                 "twig": (0.85, 0.568, 0.31)}
    PHASE = {"bloom": 1.3, "drop": 2.6, "twig": 0.7, "sprig": 0.0}

    def __init__(self):
        s = max(W / self.PW, H / self.PH)
        cw, ch = self.PW * s, self.PH * s
        cl, ct = (W - cw) / 2, (H - ch) / 2
        ax, ay = W / 2, H * 0.6
        k = 0.9
        self.pic = (ax + (cl - ax) * k, ay + (ct - ay) * k, ax + (cl + cw - ax) * k, ay + (ct + ch - ay) * k)
        self.ps = (self.pic[2] - self.pic[0]) / self.PW
        self.back = background("home.jpg")
        self.layers = {}
        for name, (x, y, w, h) in {"fountain_back": (0, 700, 852, 540), "fountain_front": (0, 987, 852, 342),
                                   "lotus": (294, 835, 269, 159), "petals": (13, 166, 827, 1048)}.items():
            fn = "home-" + name.replace("_", "-") + ".png"
            img = asset("Backgrounds/Resources/Backgrounds/" + fn)
            box = self.place(x, y, w, h)
            self.layers[name] = (img.resize((round(box[2] - box[0]), round(box[3] - box[1])), Image.LANCZOS), box)
        self.shadow_img = asset("Backgrounds/Resources/Backgrounds/home-shadow.png")

    def place(self, x, y, w, h):
        p, s = self.pic, self.ps
        return (p[0] + x * s, p[1] + y * s, p[0] + (x + w) * s, p[1] + (y + h) * s)

    def hero_geometry(self, fam):
        x, feet, height = self.PLACEMENT[fam]
        pw = self.pic[2] - self.pic[0]
        ph = self.pic[3] - self.pic[1]
        cell_h = height * 1.05 * pw / max(0.05, hero_fill(fam))
        return self.pic[0] + x * pw, self.pic[1] + feet * ph, cell_h

    def _layer(self, dst, name, alpha=1.0, dx=0.0, dy=0.0):
        img, box = self.layers[name]
        paste(dst, img, box[0] + dx, box[1] + dy, alpha)

    def _hero(self, dst, fam, t, alpha):
        fx, fy, cell_h = self.hero_geometry(fam)
        sw = cell_h * CELL_W / CELL_H * hero_seam_width(fam)
        sh = sw * 175 / 410
        sh_img = self.shadow_img.resize((max(1, round(sw)), max(1, round(sh))), Image.LANCZOS)
        paste(dst, sh_img, fx - sw / 2, fy + sh * 0.08 - sh / 2, alpha)
        draw_hero(dst, fam, "idle", t + self.PHASE[fam], fx, fy, cell_h, alpha)

    def draw(self, t, hero_alpha=1.0, petals=1.0, background_img=None):
        f = (background_img or self.back).copy()
        self._layer(f, "fountain_back")
        for fam in ("drop", "bloom"):
            self._hero(f, fam, t, hero_alpha)
        self._layer(f, "lotus")
        for fam in ("sprig", "twig"):
            self._hero(f, fam, t, hero_alpha)
        self._layer(f, "fountain_front")
        if petals > 0:
            down = (t * 22) % self.PH
            side = 14 * math.sin(t * 2 * math.pi / 7)
            self._layer(f, "petals", petals, side * self.ps, down * self.ps)
            self._layer(f, "petals", petals, side * self.ps, (down - self.PH) * self.ps)
        return f


@functools.lru_cache(maxsize=None)
def home_ui(level):
    """Home's controls (ReferenceHomeRegions): the header row, the level plaque, Play, the teaser and the bottom menu."""
    L = layer(W, H)
    gear = gear_glyph(50, INK_SOFT)
    paste(L, cream_round(94, gear), 29 - 10, 39 - 10)
    pill = cream_pill(317, 76)
    paste(L, pill, (W - 317) / 2 - 10, 48 - 10)
    blit(L, asset_scaled("Icons/Resources/Icons/currency-lotus.png", 74, 74), (W - 317) / 2 + 40, 48 + 36)
    blit(L, text_sprite("1 240", 40, INK_BROWN), W / 2 + 6, 48 + 37)
    plus = layer(56, 56)
    ellipse(plus, (0, 0, 56, 56), BTN_EDGE)
    ellipse(plus, (3, 3, 53, 50), [(0, BTN_TOP), (1, BTN)])
    blit(plus, text_sprite("+", 40, (255, 255, 255), stroke=2, stroke_fill=LEAF_LINE), 28, 25)
    paste(L, plus, (W + 317) / 2 - 64, 48 + 10)
    av = asset_scaled("Avatars/Resources/Avatars/sprig_default.jpg", 84, 84)
    av = masked(av, ellipse_mask(84, 84))
    ring = cream_round(94)
    paste(L, ring, W - 29 - 94 - 10, 39 - 10)
    paste(L, av, W - 29 - 94 + 5, 39 + 3)
    # The plaque and Play.
    paste(L, plank(288, 100, "Level %d" % level, 50), (W - 288) / 2 - 12, 1040 - 12)
    paste(L, green_button(490, 180, "Play", 92), (W - 490) / 2 - 12, 1150 - 12)
    # The teaser.
    t = parchment(330, 58, r=29)
    paste(L, t, (W - 330) / 2 - 12, 1350 - 12)
    blit(L, text_sprite("4 levels to reward", 28, INK_SOFT), W / 2, 1350 + 29)
    # The bottom menu: a wooden bar with the five places.
    bar = layer(W, 150)
    rrect(bar, (-20, 20, W + 20, 170), 30, [(0, WOOD_DARK_TOP), (1, WOOD_DARK)], WOOD_DARK_LINE, 4)
    ellipse(bar, (W / 2 - 66, 0, W / 2 + 66, 132), WOOD_DARK_LINE)
    ellipse(bar, (W / 2 - 61, 5, W / 2 + 61, 127), [(0, WOOD_LIGHT), (1, WOOD_EDGE)])
    for i, n in enumerate(("shop", "wardrobe", "home", "collection", "leaderboard")):
        x = W * (0.1 + i * 0.2)
        size = 96 if n == "home" else 76
        ic = asset_scaled("Icons/Resources/Icons/nav-%s.png" % n, size, size)
        blit(bar, ic, x, 66 if n == "home" else 92)
    paste(L, bar, 0, H - 150)
    return L


def win_hero_cell():
    return 560


@functools.lru_cache(maxsize=None)
def win_static(level, pic_name):
    """The win card's still parts: the garden, the title sign, the finished picture."""
    f = background("win.jpg").copy()
    b = board(pic_name, 38, finished=True)
    paste(f, b, (W - b.width) / 2, 330)
    paste(f, plank(470, 170, "", 10), (W - 470) / 2 - 12, 120 - 12)
    blit(f, text_sprite("Level", 64, INK_TITLE, stroke=2, stroke_fill=WOOD_LIGHT), W / 2, 120 + 52)
    blit(f, text_sprite("complete!", 64, INK_TITLE, stroke=2, stroke_fill=WOOD_LIGHT), W / 2, 120 + 118)
    # The pedestal.
    ellipse(f, (W / 2 - 150, 1080, W / 2 + 150, 1150), STONE_LINE)
    ellipse(f, (W / 2 - 146, 1082, W / 2 + 146, 1140), [(0, STONE_TOP), (1, STONE_FACE)])
    # The reward pill.
    pill = cream_pill(220, 84)
    paste(f, pill, (W - 220) / 2 - 10, 1170 - 10)
    blit(f, asset_scaled("Icons/Resources/Icons/currency-lotus.png", 78, 78), W / 2 - 54, 1170 + 40)
    blit(f, text_sprite("+20", 50, INK_BROWN), W / 2 + 30, 1170 + 40)
    return f


def win_screen(t, level=12, pic_name="butterfly_01", press=0.0):
    f = win_static(level, pic_name).copy()
    draw_hero(f, "sprig", "winidle", t, W / 2, 1112, win_hero_cell())
    btn = green_button(470, 150, "Next", 76, press=press)
    paste(f, btn, (W - 470) / 2 - 12, 1290 - 12)
    return f


GAME_CELL = 46


@functools.lru_cache(maxsize=None)
def gameplay_chrome(level):
    """Everything on the level screen except the board's tiles."""
    f = background("gameplay-daylight.jpg").copy()
    pause = text_sprite("II", 40, INK_SOFT)
    paste(f, cream_round(84, pause), 28 - 10, 50 - 10)
    paste(f, plank(270, 84, "Level %d" % level, 46), (W - 270) / 2 - 12, 50 - 12)
    sp = cream_pill(126, 78)
    paste(f, sp, W - 28 - 126 - 10, 52 - 10)
    blit(f, text_sprite("1×", 36, INK_SOFT), W - 28 - 86, 52 + 37)
    blit(f, speed_glyph(40, INK_SOFT), W - 28 - 42, 52 + 38)
    eb = board_empty("flower_pot_02", GAME_CELL)
    paste(f, eb, (W - eb.width) / 2, 170)
    # The waiting slots.
    for i in range(5):
        paste(f, slot(112), 40 + i * 128 - 10, 880 - 10)
    # The boosters.
    for i, n in enumerate(("extra_slot", "shuffle", "return", "bloom_burst")):
        ic = asset_scaled("Icons/Resources/Icons/booster-%s.png" % n, 70, 70)
        x = 70 + i * 160
        paste(f, cream_round(104, ic), x - 10, 1040 - 10)
        bd = badge(2, 30)
        paste(f, bd, x + 72, 1040 + 70)
    # The source pods: 4 columns, 3 rows.
    cols = [("water", 9, "acorn", 6, "flower", 7), ("leaf", 8, "dew", 5, "moss", 6), ("flower", 6, "wood", 9, "water", 5),
            ("moss", 7, "flower", 4, "leaf", 6)]
    for c, col in enumerate(cols):
        for r in range(3):
            vid, n = col[r * 2], col[r * 2 + 1]
            p = pod(140, 100, vid, n)
            alpha = 1.0 if r == 0 else 0.92
            paste(f, p, 32 + c * 168 - 10, 1180 + r * 118 - 10, alpha)
    return f


def gameplay_screen(level=13, pic_name="flower_pot_02", tiles=1.0, chrome_alpha=1.0):
    """The level screen; tiles in 0..1 pops the board's tiles in, row by row from the top."""
    f = gameplay_chrome(level).copy()
    eb_w = board_empty(pic_name, GAME_CELL).width
    bx, by = (W - eb_w) / 2, 170
    cells, cols, rows = board_cells(pic_name, GAME_CELL)
    for (x, y, vid, px, py) in cells:
        k = clamp01((tiles * (rows + 6) - y * 0.9 - x * 0.12) / 3.0)
        if k <= 0:
            continue
        s = ease_out_back(k)
        t = candy(GAME_CELL, vid)
        blit(f, t, bx + px + GAME_CELL / 2, by + py + GAME_CELL / 2, scale=s)
    return f


# ---------------------------------------------------------------- output

def finish(frame):
    return frame.convert("RGB").resize((W // 2, H // 2), Image.LANCZOS)

"""MOCK ONLY (the owner's latch request of 2026-11-02): composites the latch look stills on real level frames.

Input: the folder written by the LatchHeroMock tool (frames, geometry.json, the upright key and the padlock pieces).
Output: product/previews/latch-hero-mock-2026-11-02/ (or the folder given).

  python3 tools/mockups/latch-hero/compose.py <frames dir> [out dir]

A: our in-level 2D acorn character (2d/acorn-happy) holding a padlock in the held pod's colour, in the pod's place.
B: the Twig V2 hero render (3d/twig.png, a pre-rendered 3D picture): needs the owner's constitution amendment (VII).
"""
import json
import math
import os
import sys

from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
SRC = sys.argv[1]
OUT = sys.argv[2] if len(sys.argv) > 2 else os.path.join(ROOT, "product", "previews", "latch-hero-mock-2026-11-02")
os.makedirs(OUT, exist_ok=True)

CHAR = os.path.join(ROOT, "client", "Assets", "Bloomlings", "Art", "Characters", "Resources", "Characters")
ACORN = os.path.join(CHAR, "2d", "acorn-happy.png")
TWIG = os.path.join(CHAR, "3d", "twig.png")
FONT = os.path.join(ROOT, "client", "Assets", "Bloomlings", "UI", "Fonts", "Resources", "Nunito-ExtraBold.ttf")
FONT_SEMI = os.path.join(ROOT, "client", "Assets", "Bloomlings", "UI", "Fonts", "Resources", "Nunito-SemiBold.ttf")

g = json.load(open(os.path.join(SRC, "geometry.json")))
cell = g["keyCell"][2] - g["keyCell"][0]
col = g["column"]
place = g["pods"]["%d,0" % col]["place"]
pod_w = place[2] - place[0]
right_place = g["pods"]["%d,0" % (col + 1)]["place"]


def load(name):
    return Image.open(os.path.join(SRC, name)).convert("RGBA")


def font(size, semi=False):
    return ImageFont.truetype(FONT_SEMI if semi else FONT, size)


def scaled(im, height):
    w = max(1, round(im.width * height / im.height))
    return im.resize((w, round(height)), Image.LANCZOS)


def paste_center(base, im, cx, cy):
    base.alpha_composite(im, (round(cx - im.width / 2), round(cy - im.height / 2)))


def paste_bottom(base, im, cx, bottom):
    base.alpha_composite(im, (round(cx - im.width / 2), round(bottom - im.height)))


# ---------- heroes ----------

def acorn_hero(open_lock):
    """The 2D acorn on top of the padlock whose arms come out from under his sides (the game's latch padlock, body in
    the pod's colour, brass shackle)."""
    acorn = Image.open(ACORN).convert("RGBA")
    lock = load("piece-padlock-open-arms.png" if open_lock else "piece-padlock-arms.png")
    canvas = Image.new("RGBA", (512, 460), (0, 0, 0, 0))
    s = 250
    canvas.alpha_composite(lock.resize((s, s), Image.LANCZOS), (256 - s // 2, 205 - round(0.265 * s)))
    canvas.alpha_composite(acorn, (128, 0))
    return canvas.crop(canvas.getbbox())


def twig_hero(open_lock):
    """The Twig V2 stock render (Acorn Hat) with the padlock in front of his belly, his hands over its sides."""
    twig = Image.open(TWIG).convert("RGBA")
    lock = load("piece-padlock-open.png" if open_lock else "piece-padlock.png")
    canvas = twig.copy()
    s = 280
    top = 418 - round(0.53 * s)
    canvas.alpha_composite(lock.resize((s, s), Image.LANCZOS), (256 - s // 2, top))
    # the hands in front of the padlock's sides
    for hx, hy in ((158, 428), (354, 428)):
        mask = Image.new("L", twig.size, 0)
        ImageDraw.Draw(mask).ellipse((hx - 34, hy - 30, hx + 34, hy + 32), fill=255)
        mask = mask.filter(ImageFilter.GaussianBlur(3))
        hand = twig.copy()
        alpha = Image.composite(hand.getchannel("A"), Image.new("L", twig.size, 0), mask)
        hand.putalpha(alpha)
        canvas.alpha_composite(hand)
    return canvas.crop(canvas.getbbox())


def shadow(base, cx, bottom, width):
    layer = Image.new("RGBA", base.size, (0, 0, 0, 0))
    h = width * 0.2
    ImageDraw.Draw(layer).ellipse((cx - width / 2, bottom - h / 2, cx + width / 2, bottom + h / 2), fill=(20, 30, 20, 95))
    base.alpha_composite(layer.filter(ImageFilter.GaussianBlur(width * 0.06)))


def erase_pod(frame, backdrop):
    """The held pod's place shows the background (the hero stands there instead of the pod)."""
    box = (round(place[0] - 10), round(place[1] - 10), round(place[2] + 10), round(place[3] + 14))
    frame.paste(backdrop.crop(box), box[:2])


def hero_in_place(frame, hero, height_cells, cx=None, bottom=None, angle=0.0):
    h = pod_w * height_cells
    im = scaled(hero, h)
    if angle:
        im = im.rotate(angle, resample=Image.BICUBIC, expand=True)
    cx = (place[0] + place[2]) / 2 if cx is None else cx
    bottom = place[3] + 8 if bottom is None else bottom
    shadow(frame, cx, bottom - 4, min(im.width, pod_w) * 0.85)
    paste_bottom(frame, im, cx, bottom)
    return im


# ---------- key ----------

KEY = load("piece-key-upright.png")
KEY = KEY.crop(KEY.getbbox())
KEY_SHAPE_LEN = 800.0  # the piece's key length in px
KEY_LEN_CELLS = 4.0     # standing key: about 4 cells tall, 1.75 cells wide (the reference's proportions, readable at phone scale)


def key_image(scale=1.0, angle=0.0):
    length = cell * KEY_LEN_CELLS * scale
    im = KEY.resize((round(KEY.width * length / KEY_SHAPE_LEN), round(KEY.height * length / KEY_SHAPE_LEN)), Image.LANCZOS)
    if angle:
        im = im.rotate(angle, resample=Image.BICUBIC, expand=True)
    return im


def key_center():
    k = g["keyCell"]
    return (k[0] + k[2]) / 2, (k[1] + k[3]) / 2


def place_key(frame):
    cx, cy = key_center()
    # a soft light under the key so it reads on cubes as well as on the surface
    im = key_image()
    glow = Image.new("RGBA", frame.size, (0, 0, 0, 0))
    ImageDraw.Draw(glow).ellipse((cx - im.width * 0.55, cy - im.height * 0.5, cx + im.width * 0.55, cy + im.height * 0.5), fill=(255, 246, 216, 110))
    frame.alpha_composite(glow.filter(ImageFilter.GaussianBlur(cell * 0.6)))
    paste_center(frame, im, cx, cy)


# ---------- effects ----------

def star(draw, cx, cy, r, color):
    pts = []
    for i in range(8):
        a = math.pi / 4 * i - math.pi / 2
        rr = r if i % 2 == 0 else r * 0.28
        pts.append((cx + rr * math.cos(a), cy + rr * math.sin(a)))
    draw.polygon(pts, fill=color)


def sparkles(frame, cx, cy, radius, n, seed, size=1.0):
    layer = Image.new("RGBA", frame.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    import random
    rnd = random.Random(seed)
    for i in range(n):
        a = (i / n) * math.tau + rnd.uniform(-0.3, 0.3)
        r = radius * rnd.uniform(0.75, 1.15)
        x, y = cx + r * math.cos(a), cy + r * math.sin(a)
        s = rnd.uniform(10, 20) * size
        star(d, x, y, s + 4, (255, 210, 90, 255))
        star(d, x, y, s, (255, 252, 230, 255))
    glow = layer.filter(ImageFilter.GaussianBlur(6))
    frame.alpha_composite(glow)
    frame.alpha_composite(layer)


def ring_glow(frame, box, color=(255, 246, 200), strength=1.0, grow=14):
    layer = Image.new("RGBA", frame.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    l, t, r, b = box
    d.rounded_rectangle((l - grow, t - grow, r + grow, b + grow), radius=(r - l) * 0.22, outline=color + (round(230 * strength),), width=round(grow * 0.9))
    frame.alpha_composite(layer.filter(ImageFilter.GaussianBlur(grow * 0.55)))


def bezier(p0, p1, p2, t):
    return ((1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * p1[0] + t * t * p2[0], (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * p1[1] + t * t * p2[1])


def label(frame, text, sub=None, warn=False):
    """A caption plate in the frame's bottom (below the tray, over the path)."""
    d = ImageDraw.Draw(frame)
    f = font(46)
    w = d.textlength(text, font=f)
    fs = font(32, semi=True)
    ws = d.textlength(sub, font=fs) if sub else 0
    bw = max(w, ws) + 60
    bh = 84 + (48 if sub else 0)
    x0 = (frame.width - bw) / 2
    y0 = 2205 - bh / 2
    fill = (178, 52, 40, 235) if warn else (255, 248, 230, 240)
    ink = (255, 255, 255) if warn else (74, 47, 22)
    layer = Image.new("RGBA", frame.size, (0, 0, 0, 0))
    ImageDraw.Draw(layer).rounded_rectangle((x0, y0, x0 + bw, y0 + bh), radius=26, fill=fill, outline=(74, 47, 22, 200), width=4)
    frame.alpha_composite(layer)
    d.text(((frame.width - w) / 2, y0 + 14), text, font=f, fill=ink)
    if sub:
        d.text(((frame.width - ws) / 2, y0 + 72), sub, font=fs, fill=ink)


# ---------- the stills ----------

base = load("frame-base.png")
nokey = load("frame-nokey.png")
after = load("frame-after.png")
backdrop = load("backdrop.png")
HERO_A = 1.27   # hero height in pod widths
HERO_B = 1.22

# A: the 2D acorn
a = nokey.copy()
erase_pod(a, backdrop)
place_key(a)
hero_in_place(a, acorn_hero(False), HERO_A)
a_clean = a.copy()
label(a, "A: 2D-жёлудь игры", "вертикальный ключ · герой вместо пода")
a.convert("RGB").save(os.path.join(OUT, "mock-A-acorn-2d.png"))

# B: the Twig V2 render
b = nokey.copy()
erase_pod(b, backdrop)
place_key(b)
hero_in_place(b, twig_hero(False), HERO_B)
b_clean = b.copy()
label(b, "B: герой Twig 3D", "нужно изменение конституции (VII)", warn=True)
b.convert("RGB").save(os.path.join(OUT, "mock-B-twig-3d.png"))

# the game's own frame today, for comparison
now = base.copy()
now_clean = base.copy()
label(now, "Сейчас в игре", "ключ лёжа · жёлудь на поде")
now.convert("RGB").save(os.path.join(OUT, "now-in-game-L%d.png" % g["level"]))

# ---------- the opening strip (A) ----------

lock_cx = (place[0] + place[2]) / 2
lock_cy = place[3] - pod_w * HERO_A * 0.3   # the padlock's body in the hero picture
k0 = key_center()

# 1: the key on the board
f1 = a_clean.copy()

# 2: the key flying to the padlock (midway, smaller, with a trail)
f2 = nokey.copy()
erase_pod(f2, backdrop)
ring_glow(f2, (lock_cx - pod_w * 0.3, lock_cy - pod_w * 0.25, lock_cx + pod_w * 0.3, lock_cy + pod_w * 0.25), strength=0.9, grow=12)
hero_in_place(f2, acorn_hero(False), HERO_A)
ctrl = (900, 1200)
end = (lock_cx, lock_cy)
t_mid = 0.42
trail = Image.new("RGBA", f2.size, (0, 0, 0, 0))
td = ImageDraw.Draw(trail)
pts = [bezier(k0, ctrl, end, t) for t in [i / 40 * t_mid for i in range(41)]]
for i in range(1, len(pts)):
    w = 4 + 26 * (i / len(pts))
    td.line([pts[i - 1], pts[i]], fill=(255, 246, 216, round(60 + 195 * i / len(pts))), width=round(w * 1.3))
f2.alpha_composite(trail.filter(ImageFilter.GaussianBlur(9)))
for i, t in enumerate((0.15, 0.24, 0.33)):
    x, y = bezier(k0, ctrl, end, t)
    nx, ny = bezier(k0, ctrl, end, t + 0.02)
    ga = math.degrees(math.atan2(ny - y, nx - x))
    ghost = key_image(1.0 - (0.42 - t) * 0.5, angle=-(ga - 90))
    ghost.putalpha(ghost.getchannel("A").point(lambda v, k=i: round(v * (0.18 + 0.14 * k))))
    paste_center(f2, ghost, x, y)
mx, my = bezier(k0, ctrl, end, t_mid)
dx, dy = bezier(k0, ctrl, end, t_mid + 0.02)
angle = math.degrees(math.atan2(dy - my, dx - mx))  # travel direction; the upright key's tip points down (90°)
kim = key_image(0.95, angle=-(angle - 90))
paste_center(f2, kim, mx, my)
sparkles(f2, mx, my, cell * 1.6, 5, 3, 0.7)

# 3: the padlock open, the hero stepping aside, the pod's cube in its place
f3 = nokey.copy()
ring_glow(f3, place, strength=1.0, grow=16)
gap_cx = (place[2] + right_place[0]) / 2 + 8
acorn_alone = Image.open(ACORN).convert("RGBA")
acorn_alone = acorn_alone.crop(acorn_alone.getbbox())
hero_in_place(f3, acorn_alone, 0.66, cx=gap_cx + 6, bottom=place[3] - 6, angle=-10)
open_lock = load("piece-padlock-open.png")
open_lock = open_lock.crop(open_lock.getbbox())
ol = scaled(open_lock, pod_w * 0.5).rotate(14, resample=Image.BICUBIC, expand=True)
paste_center(f3, ol, place[2] - pod_w * 0.08, place[1] + pod_w * 0.02)
sparkles(f3, place[2] - pod_w * 0.08, place[1] + pod_w * 0.02, pod_w * 0.38, 5, 11, 0.8)
sparkles(f3, (place[0] + place[2]) / 2, (place[1] + place[3]) / 2, pod_w * 0.72, 8, 7)

steps = [(f1, "1 · ключ стоит на поле"), (f2, "2 · ключ летит к замку"), (f3, "3 · замок открыт, под на месте")]
for fr, _ in steps:
    pass

scale = 0.5
fw, fh = round(1080 * scale), round(2340 * scale)
gap = 24
title_h = 90
strip = Image.new("RGB", (gap + 3 * (fw + gap), title_h + fh + gap + 70), (250, 245, 234))
sd = ImageDraw.Draw(strip)
sd.text((gap, 22), "A · открытие замка: ключ — замок — под", font=font(44), fill=(74, 47, 22))
for i, (fr, cap) in enumerate(steps):
    x = gap + i * (fw + gap)
    strip.paste(fr.convert("RGB").resize((fw, fh), Image.LANCZOS), (x, title_h))
    sd.text((x + 6, title_h + fh + 14), cap, font=font(34), fill=(74, 47, 22))
strip.save(os.path.join(OUT, "mock-A-opening-strip.png"))
for i, (fr, _) in enumerate(steps):
    fr.convert("RGB").save(os.path.join(OUT, "mock-A-opening-%d.png" % (i + 1)))

# ---------- close-ups ----------

def crop_zoom(fr, box, zoom):
    c = fr.crop(tuple(round(v) for v in box))
    return c.resize((round(c.width * zoom), round(c.height * zoom)), Image.LANCZOS).convert("RGB")


kx, ky = k0
key_box = (kx - 150, ky - 120, kx + 150, ky + 120)
tray_box = (0, place[1] - 70, place[2] + 200, place[3] + 40)

# ---------- contact sheet ----------

cw = 520
ch = round(2340 * cw / 1080)
zoom_key = 1.4
zoom_tray = 1.25
cards = [
    (now_clean, "Сейчас в игре (L%d)" % g["level"], "ключ лежит наискосок, жёлудь сидит на поде", None),
    (a_clean, "A: 2D-жёлудь игры", "ключ стоит вертикально; вместо пода — жёлудь с замком цвета пода", None),
    (b_clean, "B: герой Twig 3D", "нужно изменение конституции: 3D-рендер внутри уровня сейчас запрещён (VII)", (178, 52, 40)),
]
pad = 28
sheet_w = pad + 3 * (cw + pad) + 460 + pad
row1 = 110 + ch + 130
closeup_h = round(240 * zoom_key) + 40 + round((tray_box[3] - tray_box[1]) * zoom_tray) + 40
row1 = max(row1, 110 + closeup_h * 1)
strip_small_w = sheet_w - 2 * pad
strip_img = Image.open(os.path.join(OUT, "mock-A-opening-strip.png"))
strip_small = strip_img.resize((strip_small_w, round(strip_img.height * strip_small_w / strip_img.width)), Image.LANCZOS)
sheet_h = 110 + ch + 140 + 60 + 2 * (round(240 * zoom_key) + round((tray_box[3] - tray_box[1]) * zoom_tray) + 80) // 2 + strip_small.height + pad
sheet = Image.new("RGB", (sheet_w, 110 + ch + 190 + strip_small.height + 130), (250, 245, 234))
sd = ImageDraw.Draw(sheet)
sd.text((pad, 26), "Замок с героем и вертикальный ключ — макет на кадре игры (L%d), 2026-11-02" % g["level"], font=font(46), fill=(74, 47, 22))


def wrap(text, f, width):
    words, lines, cur = text.split(), [], ""
    for w in words:
        t = (cur + " " + w).strip()
        if sd.textlength(t, font=f) > width and cur:
            lines.append(cur)
            cur = w
        else:
            cur = t
    lines.append(cur)
    return lines


for i, (fr, title, sub, warn) in enumerate(cards):
    x = pad + i * (cw + pad)
    sheet.paste(fr.convert("RGB").resize((cw, ch), Image.LANCZOS), (x, 110))
    sd.text((x, 110 + ch + 12), title, font=font(36), fill=warn or (74, 47, 22))
    for j, line in enumerate(wrap(sub, font(26, True), cw)):
        sd.text((x, 110 + ch + 58 + j * 32), line, font=font(26, True), fill=warn or (90, 66, 40))

# close-ups column: A key, A tray, B tray
x = pad + 3 * (cw + pad)
y = 110
sd.text((x, y - 4), "Крупно", font=font(34), fill=(74, 47, 22))
y += 44
for fr, box, z, cap in ((a_clean, key_box, 1.5, "ключ стоит в пустом месте картинки"), (a_clean, tray_box, 1.0, "A: жёлудь вместо пода"), (b_clean, tray_box, 1.0, "B: Twig вместо пода")):
    c = crop_zoom(fr, box, z)
    if c.width > 460:
        c = c.resize((460, round(c.height * 460 / c.width)), Image.LANCZOS)
    sheet.paste(c, (x, y))
    y += c.height + 6
    sd.text((x, y), cap, font=font(24, True), fill=(90, 66, 40))
    y += 40

ys = 110 + ch + 190
sheet.paste(strip_small, (pad, ys))
SOURCES = ("Источники: кадр игры (preview-рендер L%d, решение уровня до замка); ключ — форма UiRaster.RoundKey, поставлена вертикально; замок — UiRaster.Padlock, корпус цвета пода, дужка латунь; A — 2d/acorn-happy; B — 3d/twig.png (Twig V2, шапка-жёлудь)." % g["level"])
for j, line in enumerate(wrap(SOURCES, font(24, True), sheet_w - 2 * pad)):
    sd.text((pad, ys + strip_small.height + 14 + j * 32), line, font=font(24, True), fill=(110, 90, 60))
sheet.save(os.path.join(OUT, "contact-sheet.png"))

# close-up file of its own
cl = Image.new("RGB", (3 * 470 + 40, 900), (250, 245, 234))
cd = ImageDraw.Draw(cl)
xx = 10
for fr, box, z, cap in ((a_clean, key_box, 1.5, "ключ (A и B одинаково)"), (a_clean, tray_box, 1.6, "A: 2D-жёлудь"), (b_clean, tray_box, 1.6, "B: Twig 3D (конституция)")):
    c = crop_zoom(fr, box, z)
    c = c.resize((460, round(c.height * 460 / c.width)), Image.LANCZOS)
    cl.paste(c, (xx, 60))
    cd.text((xx, 10), cap, font=font(32), fill=(74, 47, 22))
    xx += 470
cl.save(os.path.join(OUT, "mock-closeups.png"))
print("written to", OUT)

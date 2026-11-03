"""Composes the "in hands" walker mock (2026-11-03) from the preview's frames.

Frames: `dotnet run -c Release --project playtest/preview -- --walker-video <level> <seconds> <dir>/<name> <look> <from> <style id>`
with the looks default, hands-creature and hands-heroes (names `<level>-<look>-<style>`; the "now" frames of L61 and L1 are the
earlier mock's `<level>-default`), and `--walker-strips <dir>` for the strips. Usage:
    python3 compose.py <frames dir> <now frames dir> <strips dir> <out dir>
"""
import os
import subprocess
import sys

from PIL import Image, ImageDraw, ImageFont

FONT = "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"
BG = (40, 34, 30)
CROP = {61: (560, 1140), 1: (560, 960), 4: (600, 1090)}
FIGURES = [
    ("now", "Сейчас", "Munchers / стиль как в игре"),
    ("hands-creature", "Существо с ручками-ножками", "в руки + шутка"),
    ("hands-heroes", "Наши герои (эксперимент)", "в руки + шутка"),
]
STYLES = [
    ("blossom", "Цветение", "кубик в руках раскрывается цветком"),
    ("fireflies", "Светлячки", "кубик светится и разлетается светлячками"),
    ("bubbles", "Пузыри", "выдувает пузырь, тот улетает и лопается"),
    ("pushers", "Толкатели", "толкает большой кубик-ящик домой"),
    ("fireworks", "Фейерверк", "кубик шипит и взлетает ракетой"),
    ("parade", "Парад конфетти", "жонглирует, кубик взрывается конфетти"),
]
GAGS = {"chomp": "Жуёт в 3 укуса", "squish": "Сжимает — лопается", "toss": "Подбрасывает — бац по голове",
        "shake": "Трясёт как подарок — пуф"}
STYLE_NAMES = {"clear." + k: v for k, v, _ in STYLES}


def font(size):
    return ImageFont.truetype(FONT, size)


def board_box(frames):
    left, top, width, height, cell = (float(v) for v in open(os.path.join(frames, "board.txt")).read().split())
    pad = 28
    return int(left - pad), int(top - pad), int(left + width + pad), int(top + height + pad + 70)


def panel(img, level, box, label, sub, width):
    left, top, right, bottom = box
    scale = width / (right - left)
    board = img.crop((left, top, right, bottom)).resize((width, int((bottom - top) * scale)), Image.LANCZOS)
    zoom = 2 * scale
    cw, ch = int(width / zoom), int(width * 0.58 / zoom)
    cx, cy = CROP[level]
    crop = img.crop((cx - cw // 2, cy - ch // 2, cx - cw // 2 + cw, cy - ch // 2 + ch)).resize((width, int(ch * zoom)), Image.LANCZOS)
    head = int(width * 0.12)
    out = Image.new("RGB", (width, head + board.height + 6 + crop.height + 4), BG)
    draw = ImageDraw.Draw(out)
    draw.text((10, 4), label, font=font(max(12, width // 24)), fill=(255, 244, 220))
    draw.text((10, 4 + width // 20), sub, font=font(max(10, width // 36)), fill=(230, 210, 180))
    out.paste(board, (0, head))
    y = head + board.height + 6
    out.paste(crop, (0, y))
    draw.text((8, y + 4), "×2", font=font(max(12, width // 30)), fill=(255, 255, 255), stroke_width=2, stroke_fill=(0, 0, 0))
    sx, sy = (cx - cw // 2 - left) * scale, (cy - ch // 2 - top) * scale + head
    draw.rectangle((sx, sy, sx + cw * scale, sy + ch * scale), outline=(255, 255, 255), width=2)
    return out


def even(img):
    w, h = img.size
    return img.crop((0, 0, w - w % 2, h - h % 2))


def row(panels):
    h = max(p.height for p in panels)
    w = sum(p.width for p in panels) + 4 * (len(panels) - 1)
    out = Image.new("RGB", (w, h), BG)
    x = 0
    for p in panels:
        out.paste(p, (x, 0))
        x += p.width + 4
    return out


def encode(pattern, out):
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-framerate", "25", "-i", pattern, "-c:v", "libx264", "-preset", "slow",
                    "-crf", "26", "-pix_fmt", "yuv420p", "-movflags", "+faststart", out], check=True)


def frames_of(d):
    return sorted(f for f in os.listdir(d) if f.startswith("frame-"))


def main(frames_dir, now_dir, strips_dir, out_dir):
    os.makedirs(out_dir, exist_ok=True)
    tmp = os.path.join(frames_dir, "_composed")
    os.makedirs(tmp, exist_ok=True)
    sheet_rows = []

    # Side by side on L61 and L1: today | the creature | the heroes (Munchers).
    for level in (61, 1):
        dirs = [os.path.join(now_dir, f"{level}-default"), os.path.join(frames_dir, f"{level}-hands-creature-munchers"),
                os.path.join(frames_dir, f"{level}-hands-heroes-munchers")]
        boxes = [board_box(d) for d in dirs]
        names = frames_of(dirs[0])
        names = names[:min(len(frames_of(d)) for d in dirs)]
        out = os.path.join(tmp, f"hands-{level}")
        os.makedirs(out, exist_ok=True)
        for i, name in enumerate(names):
            panels = [panel(Image.open(os.path.join(d, name)).convert("RGB"), level, b, f"L{level} · {f[1]}", f[2], 540)
                      for d, b, f in zip(dirs, boxes, FIGURES)]
            frame = even(row(panels))
            frame.save(os.path.join(out, name))
            if i == len(names) * 3 // 5:
                sheet_rows.append(frame)
        encode(os.path.join(out, "frame-%04d.png"), os.path.join(out_dir, f"hands-L{level}.mp4"))

    # The six other styles on L4, 4 s each: one video a figure, and one with the three side by side.
    seq = {f[0]: os.path.join(tmp, "styles-" + f[0]) for f in FIGURES}
    cmp_dir = os.path.join(tmp, "styles-compare")
    for d in list(seq.values()) + [cmp_dir]:
        os.makedirs(d, exist_ok=True)
    n = 0
    for key, name, sub in STYLES:
        dirs = {f[0]: os.path.join(frames_dir, f"4-{'default' if f[0] == 'now' else f[0]}-{key}") for f in FIGURES}
        boxes = {k: board_box(d) for k, d in dirs.items()}
        names = frames_of(dirs["now"])[:100]
        for i, fname in enumerate(names):
            panels = []
            for fig, flabel, _ in FIGURES:
                img = Image.open(os.path.join(dirs[fig], fname)).convert("RGB")
                short = {"now": "сейчас", "hands-creature": "существо", "hands-heroes": "наши герои"}[fig]
                p = even(panel(img, 4, boxes[fig], f"{name} · {short}", "как в игре" if fig == "now" else sub, 540))
                p.save(os.path.join(seq[fig], "frame-%04d.png" % n))
                panels.append(p)
            frame = even(row(panels))
            frame.save(os.path.join(cmp_dir, "frame-%04d.png" % n))
            if i == 70:
                sheet_rows.append(frame)
            n += 1
    for fig, _, _ in FIGURES:
        encode(os.path.join(seq[fig], "frame-%04d.png"), os.path.join(out_dir, "styles-" + fig.replace("hands-", "") + ".mp4"))
    encode(os.path.join(cmp_dir, "frame-%04d.png"), os.path.join(out_dir, "styles-compare.mp4"))

    # The contact sheet: a frame of each, a row a clip (smaller).
    w = sheet_rows[0].width // 2
    small = [r.resize((w, r.height * w // r.width), Image.LANCZOS) for r in sheet_rows]
    sheet = Image.new("RGB", (w, sum(s.height for s in small) + 6 * len(small)), BG)
    y = 0
    for s in small:
        sheet.paste(s, (0, y))
        y += s.height + 6
    sheet.save(os.path.join(out_dir, "contact-sheet.png"), optimize=True)

    # The strips, labelled.
    for strip, title in (("gags-strip", "Шутки Munchers «в руки»: кадры акта (0,11 с)"),
                         ("styles-strip", "Другие стили «в руки»: кадры акта (0,11 с)")):
        labelled(os.path.join(strips_dir, strip + ".png"), os.path.join(strips_dir, strip + ".rows.txt"),
                 os.path.join(out_dir, strip + ".png"), title)
    creatures = Image.open(os.path.join(strips_dir, "creatures-sheet.png")).convert("RGB")
    head = 60
    out = Image.new("RGB", (creatures.width, creatures.height + head), BG)
    out.paste(creatures, (0, head))
    ImageDraw.Draw(out).text((12, 12), "Существа: бег 8 кадров · стоит боком · лицом · держит · руки поверх кубика",
                             font=font(28), fill=(255, 244, 220))
    out.resize((out.width * 2 // 3, out.height * 2 // 3), Image.LANCZOS).save(os.path.join(out_dir, "creatures-sheet.png"), optimize=True)


def labelled(png, rows_txt, out_png, title):
    img = Image.open(png).convert("RGB")
    rows = [r.strip() for r in open(rows_txt, encoding="utf-8") if r.strip()]
    rh = img.height // len(rows)
    lw, head = 470, 60
    out = Image.new("RGB", (img.width + lw, img.height + head), BG)
    out.paste(img, (lw, head))
    draw = ImageDraw.Draw(out)
    draw.text((12, 12), title, font=font(30), fill=(255, 244, 220))
    for i, r in enumerate(rows):
        what, fig = r.rsplit(" ", 1)
        name = GAGS.get(what) or STYLE_NAMES.get(what, what)
        draw.text((12, head + i * rh + rh // 2 - 30), name, font=font(22), fill=(255, 244, 220))
        draw.text((12, head + i * rh + rh // 2 + 4), "существо" if fig == "creature" else "наши герои", font=font(20), fill=(230, 210, 180))
    out = out.resize((out.width * 3 // 5, out.height * 3 // 5), Image.LANCZOS)
    out.save(out_png, optimize=True)


if __name__ == "__main__":
    main(*sys.argv[1:5])

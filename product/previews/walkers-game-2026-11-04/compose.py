"""Composes the game's walker clips of 2026-11-04 (Settings «Ходоки»: Classic, Creatures, Heroes; Munchers eating aside;
the paid Head Bonk) from the preview's frames.

Frames: `dotnet run -c Release --project playtest/preview -- --walker-video <level> 11 <dir>/<level>-<kind>-<style> <kind> <from> clear.<style>`
with the kinds classic, creatures, heroes (the game's own path: the Settings choice), the styles munchers and bonk;
`--walker-strips <dir>` for the strips. Usage:
    python3 compose.py <frames dir> <strips dir> <out dir>
"""
import os
import subprocess
import sys

from PIL import Image, ImageDraw, ImageFont

FONT = "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"
BG = (40, 34, 30)
CROP = {61: (560, 1140), 1: (560, 960)}
KINDS = [("classic", "Классические"), ("creatures", "Существа"), ("heroes", "Наши герои")]
STYLES = {"munchers": "Munchers: в руки, в сторону, 3 укуса", "bonk": "Об голову (платный стиль)"}


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
    out = Image.new("RGB", (sum(p.width for p in panels) + 4 * (len(panels) - 1), h), BG)
    x = 0
    for p in panels:
        out.paste(p, (x, 0))
        x += p.width + 4
    return out


def encode(pattern, out):
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-framerate", "25", "-i", pattern, "-c:v", "libx264", "-preset", "slow",
                    "-crf", "26", "-pix_fmt", "yuv420p", "-movflags", "+faststart", out], check=True)


def labelled(png, rows_txt, out_png, title, names):
    img = Image.open(png).convert("RGB")
    rows = [r.strip() for r in open(rows_txt, encoding="utf-8") if r.strip()]
    rh = img.height // len(rows)
    lw, head = 470, 60
    out = Image.new("RGB", (img.width + lw, img.height + head), BG)
    out.paste(img, (lw, head))
    draw = ImageDraw.Draw(out)
    draw.text((12, 12), title, font=font(30), fill=(255, 244, 220))
    for i, r in enumerate(rows):
        what, kind = r.rsplit(" ", 1)
        draw.text((12, head + i * rh + rh // 2 - 30), names[what], font=font(22), fill=(255, 244, 220))
        draw.text((12, head + i * rh + rh // 2 + 4), dict(KINDS)[kind], font=font(20), fill=(230, 210, 180))
    out.resize((out.width * 3 // 5, out.height * 3 // 5), Image.LANCZOS).save(out_png, optimize=True)


def main(frames_dir, strips_dir, out_dir):
    os.makedirs(out_dir, exist_ok=True)
    tmp = os.path.join(frames_dir, "_composed")
    sheet = []
    for level, style in ((61, "munchers"), (1, "munchers"), (61, "bonk")):
        dirs = [os.path.join(frames_dir, f"{level}-{k}-{style}") for k, _ in KINDS]
        boxes = [board_box(d) for d in dirs]
        names = sorted(f for f in os.listdir(dirs[0]) if f.startswith("frame-"))
        out = os.path.join(tmp, f"{style}-{level}")
        os.makedirs(out, exist_ok=True)
        for i, name in enumerate(names):
            panels = [panel(Image.open(os.path.join(d, name)).convert("RGB"), level, b, f"L{level} · {label}",
                            "Munchers как сейчас" if style == "munchers" and kind == "classic" else STYLES[style], 540)
                      for d, b, (kind, label) in zip(dirs, boxes, KINDS)]
            frame = even(row(panels))
            frame.save(os.path.join(out, name))
            if i == len(names) * 3 // 5:
                sheet.append(frame)
        encode(os.path.join(out, "frame-%04d.png"), os.path.join(out_dir, f"{style}-L{level}.mp4"))

    w = sheet[0].width // 2
    small = [r.resize((w, r.height * w // r.width), Image.LANCZOS) for r in sheet]
    contact = Image.new("RGB", (w, sum(s.height for s in small) + 6 * len(small)), BG)
    y = 0
    for s in small:
        contact.paste(s, (0, y))
        y += s.height + 6
    contact.save(os.path.join(out_dir, "contact-sheet.png"), optimize=True)

    labelled(os.path.join(strips_dir, "game-strip.png"), os.path.join(strips_dir, "game-strip.rows.txt"), os.path.join(out_dir, "game-strip.png"),
             "Ходоки в игре: кадры акта (0,11 с)", {"aside": "Munchers: в сторону и 3 укуса", "bonk": "Об голову"})
    store = Image.open(os.path.join(strips_dir, "store-bonk.png")).convert("RGB")
    store.resize((store.width // 2, store.height // 2), Image.LANCZOS).save(os.path.join(out_dir, "store-bonk-preview.png"), optimize=True)


if __name__ == "__main__":
    main(*sys.argv[1:4])

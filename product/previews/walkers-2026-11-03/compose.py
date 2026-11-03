"""Composes the walker mock's clips (2026-11-03) from the preview's frames.

Frames come from `dotnet run -c Release --project playtest/preview -- --walker-video <level> 11 <dir>/<level>-<look> <look> <from>`
(looks: default, contrast, creature). Each clip: the board at phone scale, then a x2 crop where the walkers work,
labelled in Russian; plus a side-by-side comparison per level and a contact sheet. Usage:
    python3 compose.py <frames dir> <out dir>
"""
import os
import subprocess
import sys

from PIL import Image, ImageDraw, ImageFont

FONT = "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"
WIDTH = 540
LOOKS = [
    ("default", "1 · Сейчас", "Munchers как в игре"),
    ("contrast", "2 · Предложение 1+2+3", "размер 6% доски · контур · тень · уносит кубик"),
    ("creature", "3 · Вариант 4: существо с бегом", "как 2 + листик и капля: объём, ножки, бег 8 кадров"),
]
# The x2 crop's centre per level (screen pixels of the 1080-wide frame), where the walkers pick their cubes up.
CROP = {61: (560, 1140), 1: (560, 960)}


def board_box(frames):
    left, top, width, height, cell = (float(v) for v in open(os.path.join(frames, "board.txt")).read().split())
    pad = 28
    return int(left - pad), int(top - pad), int(left + width + pad), int(top + height + pad + 70), cell


def panel(img, level, box, label, sub, width):
    left, top, right, bottom, _ = box
    scale = width / (right - left)
    board = img.crop((left, top, right, bottom)).resize((width, int((bottom - top) * scale)), Image.LANCZOS)
    zoom = 2 * scale
    cw, ch = int(width / zoom), int(width * 0.58 / zoom)
    cx, cy = CROP[level]
    crop = img.crop((cx - cw // 2, cy - ch // 2, cx - cw // 2 + cw, cy - ch // 2 + ch)).resize((width, int(ch * zoom)), Image.LANCZOS)
    head = 64
    out = Image.new("RGB", (width, head + board.height + 6 + crop.height + 4), (40, 34, 30))
    draw = ImageDraw.Draw(out)
    draw.text((12, 6), f"L{level} · {label}", font=ImageFont.truetype(FONT, 22), fill=(255, 244, 220))
    draw.text((12, 36), sub, font=ImageFont.truetype(FONT, 15), fill=(230, 210, 180))
    out.paste(board, (0, head))
    y = head + board.height + 6
    out.paste(crop, (0, y))
    draw.text((8, y + 4), "×2", font=ImageFont.truetype(FONT, 18), fill=(255, 255, 255), stroke_width=2, stroke_fill=(0, 0, 0))
    # Where the x2 crop is, on the board.
    sx, sy = (cx - cw // 2 - left) * scale, (cy - ch // 2 - top) * scale + head
    draw.rectangle((sx, sy, sx + cw * scale, sy + ch * scale), outline=(255, 255, 255), width=2)
    if out.height % 2:
        out = out.crop((0, 0, out.width, out.height - 1))
    return out


def encode(pattern, out):
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-framerate", "25", "-i", pattern, "-c:v", "libx264", "-preset", "slow",
                    "-crf", "24", "-pix_fmt", "yuv420p", "-movflags", "+faststart", out], check=True)


def main(frames_dir, out_dir):
    os.makedirs(out_dir, exist_ok=True)
    tmp = os.path.join(frames_dir, "_composed")
    sheet_tiles = []
    for level in (61, 1):
        names = sorted(f for f in os.listdir(os.path.join(frames_dir, f"{level}-default")) if f.startswith("frame-"))
        boxes = {look: board_box(os.path.join(frames_dir, f"{level}-{look}")) for look, _, _ in LOOKS}
        for d in ("one", "cmp"):
            os.makedirs(os.path.join(tmp, f"{level}-{d}"), exist_ok=True)
        clips = {look: os.path.join(tmp, f"{level}-{look}") for look, _, _ in LOOKS}
        for c in clips.values():
            os.makedirs(c, exist_ok=True)
        pick = len(names) * 3 // 5
        for i, name in enumerate(names):
            panels = []
            for look, label, sub in LOOKS:
                img = Image.open(os.path.join(frames_dir, f"{level}-{look}", name)).convert("RGB")
                p = panel(img, level, boxes[look], label, sub, WIDTH)
                p.save(os.path.join(clips[look], name))
                panels.append(p)
                if i == pick:
                    sheet_tiles.append(p)
            h = max(p.height for p in panels)
            cmp = Image.new("RGB", (WIDTH * 3 + 8, h), (40, 34, 30))
            for k, p in enumerate(panels):
                cmp.paste(p, (k * (WIDTH + 4), 0))
            cmp.resize((cmp.width * 2 // 3 // 2 * 2, h * 2 // 3 // 2 * 2), Image.LANCZOS).save(os.path.join(tmp, f"{level}-cmp", name))
        for look, _, _ in LOOKS:
            encode(os.path.join(clips[look], "frame-%04d.png"), os.path.join(out_dir, f"L{level}-{look}.mp4"))
        encode(os.path.join(tmp, f"{level}-cmp", "frame-%04d.png"), os.path.join(out_dir, f"L{level}-compare.mp4"))

    # The contact sheet: one frame of each look, L61 on top, L1 below.
    w = WIDTH * 3 + 8
    h61 = max(t.height for t in sheet_tiles[:3])
    h1 = max(t.height for t in sheet_tiles[3:])
    sheet = Image.new("RGB", (w, h61 + h1 + 8), (40, 34, 30))
    for k, t in enumerate(sheet_tiles):
        sheet.paste(t, ((k % 3) * (WIDTH + 4), 0 if k < 3 else h61 + 8))
    sheet.save(os.path.join(out_dir, "contact-sheet.png"), optimize=True)


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])

import sys, glob, os
from PIL import Image, ImageDraw, ImageFont
src, out, t0, step, per = sys.argv[1], sys.argv[2], float(sys.argv[3]), float(sys.argv[4]), int(sys.argv[5])
files = sorted(glob.glob(src + '/*.png'))
f = ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf', 20)
cols = 4
for s in range(0, len(files), per):
    chunk = files[s:s + per]
    ims = [Image.open(p) for p in chunk]
    w, h = ims[0].size
    rows = (len(ims) + cols - 1) // cols
    sheet = Image.new('RGB', (cols * w, rows * h), (255, 255, 255))
    d = ImageDraw.Draw(sheet)
    for i, (p, im) in enumerate(zip(chunk, ims)):
        x, y = (i % cols) * w, (i // cols) * h
        sheet.paste(im, (x, y))
        idx = int(os.path.basename(p)[:-4])
        d.rectangle([x, y, x + 80, y + 24], fill=(0, 0, 0)); d.text((x + 3, y + 1), f"{t0 + (idx - 1) * step:5.0f}s", fill=(255, 255, 0), font=f)
        d.rectangle([x, y, x + w - 1, y + h - 1], outline=(0, 0, 0))
    sheet.save(f"{out}_{s // per:02d}.jpg", quality=85)

import sys, os, glob
from PIL import Image, ImageDraw, ImageFont
src, out, step, per = sys.argv[1], sys.argv[2], float(sys.argv[3]), int(sys.argv[4])
files = sorted(glob.glob(src + '/*.jpg'))
f = ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf', 22)
cols = 6
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
        t = (idx - 1) * step
        d.rectangle([x, y, x + 92, y + 28], fill=(0, 0, 0)); d.text((x + 4, y + 2), f"{t:6.1f}s", fill=(255, 255, 0), font=f)
    sheet.save(f"{out}_{s // per:02d}.jpg", quality=80)

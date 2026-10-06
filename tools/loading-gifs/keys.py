import sys
from variants import *
name, kind = sys.argv[1], sys.argv[2]
times = [float(v) for v in sys.argv[3].split(",")]
title, boot, bs, trans, ts = CONCEPTS[name]
fn = boot if kind == "boot" else trans
ims = [finish(fn(t)).resize((240, 520), Image.LANCZOS) for t in times]
sheet = Image.new("RGB", (len(ims) * 245, 545), (255, 255, 255))
d = ImageDraw.Draw(sheet)
for i, im in enumerate(ims):
    sheet.paste(im, (i * 245, 0))
    d.text((i * 245 + 5, 525), "t=%.2f" % times[i], fill=(0, 0, 0))
sheet.save("keys-%s-%s.png" % (name, kind))

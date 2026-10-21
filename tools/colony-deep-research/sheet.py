import sys, csv
from PIL import Image, ImageDraw, ImageFont
base = sys.argv[1]; out = sys.argv[2]
rows = {int(r['level']): r for r in csv.DictReader(open(base + '/data/levels.csv'))}
labels = {int(r['level']): (r['label_updated'] or r['label_original']) for r in csv.DictReader(open(sys.argv[3]))}
levels = [int(x) for x in sys.argv[4].split(',')]
W, H, cols = 300, 300, 4
n = len(levels); rws = (n + cols - 1) // cols
sheet = Image.new('RGB', (cols * W, rws * (H + 44)), (250, 246, 236))
d = ImageDraw.Draw(sheet)
try: f = ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf', 16)
except Exception: f = ImageFont.load_default()
for i, L in enumerate(levels):
    im = Image.open(f'{base}/raw/boards/{L}.webp').convert('RGB'); im.thumbnail((W - 10, H - 10))
    x, y = (i % cols) * W, (i // cols) * (H + 44)
    sheet.paste(im, (x + (W - im.width) // 2, y + 40 + (H - im.height) // 2))
    r = rows.get(L, {})
    t = f"L{L} {labels.get(L,'?')}  {r.get('board_gridW','?')}x{r.get('board_gridH','?')}  {r.get('board_colors','?')} col."
    d.text((x + 8, y + 12), t, fill=(40, 40, 40), font=f)
sheet.save(out, quality=85)

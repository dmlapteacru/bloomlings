#!/usr/bin/env python3
"""v8 → v8C pictures side by side, in the pilot's normal palette (art-before-after.png)."""
import json, re, subprocess
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
ROOT = Path(__file__).resolve().parents[2]
old = json.loads((ROOT / 'content/pilot-v8/levels.json').read_text())['levels']
new = json.loads((ROOT / 'content/pilot-v8c/levels.json').read_text())['levels']
palette = (ROOT / 'tools/gameplay-pilot-v8c/palette.cjs').read_text()
codes = re.search(r"codes='([A-Z]+)'", palette).group(1)
colours = re.findall(r"#[0-9A-Fa-f]{6}", palette.split('normal=[')[1].split(']')[0])
fill = dict(zip(codes, colours)); fill['.'] = '#F7F1E1'; fill['#'] = '#8A8A8A'
changed = [n['id'] for n, o in zip(new, old) if n['rowsBottomFirst'] != o['rowsBottomFirst']]
font = ImageFont.truetype(str(ROOT / 'client/Assets/Bloomlings/UI/Fonts/Resources/Nunito-ExtraBold.ttf'), 22)
small = ImageFont.truetype(str(ROOT / 'client/Assets/Bloomlings/UI/Fonts/Resources/Nunito-SemiBold.ttf'), 18)
S, P = 12, 24
def board(level):
    rows = level['rowsBottomFirst'][::-1]; im = Image.new('RGB', (level['w'] * S, level['h'] * S), 'white'); d = ImageDraw.Draw(im)
    for y, r in enumerate(rows):
        for x, c in enumerate(r):
            d.rectangle([x * S, y * S, x * S + S - 2, y * S + S - 2], fill=fill[c])
    return im
cols = 2; cw = 36 * S * 2 + P * 3; ch = 36 * S + 90
sheet = Image.new('RGB', (cols * cw, ((len(changed) + cols - 1) // cols) * ch), '#FFF8EA'); d = ImageDraw.Draw(sheet)
for k, i in enumerate(changed):
    o, n = old[i - 1], new[i - 1]; x0, y0 = (k % cols) * cw, (k // cols) * ch
    d.text((x0 + P, y0 + 10), f"L{i}", font=font, fill='#34493d')
    for j, (lv, tag) in enumerate(((o, 'v8 · ' + o['title']), (n, 'v8C · ' + n['title']))):
        im = board(lv); x = x0 + P + j * (36 * S + P); sheet.paste(im, (x, y0 + 70))
        mats = len(set(''.join(lv['rowsBottomFirst'])) - set('.#'))
        d.text((x, y0 + 42), f"{tag} · {mats} цв.", font=small, fill='#34493d')
out = ROOT / 'product/previews/gameplay-pilot-v8c/art-before-after.png'; out.parent.mkdir(parents=True, exist_ok=True); sheet.save(out)
print(out.relative_to(ROOT), changed)

#!/usr/bin/env python3
"""v8E art proof sheet: the 16-colour palette, v8D vs v8E pictures, and Colony Flow's L20 cover for scale.
usage: python3 art-proof.py <proof.json from art-proof.mjs> <out.png> [cf-cover.png]
       python3 art-proof.py --contact <proof.json> <out.png>   (all v8E pictures, five per row)"""
import json, re, sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
ROOT = Path(__file__).resolve().parents[2]
pal = (Path(__file__).parent / 'palette.cjs').read_text()
codes = re.search(r"codes='([A-Z]+)'", pal).group(1)
names = re.findall(r"'([^']+)'", pal.split('names=[')[1].split(']')[0])
heroes = re.findall(r"'([a-z]+)'", pal.split('heroes=[')[1].split(']')[0])
PAL = dict(zip(codes, re.findall(r"#[0-9A-Fa-f]{6}", pal.split('normal=[')[1].split(']')[0]))); PAL['.'] = '#F7F1E1'; PAL['#'] = '#8A8A8A'
F = ROOT / 'client/Assets/Bloomlings/UI/Fonts/Resources'
big, mid, sm = (ImageFont.truetype(str(F / n), s) for n, s in (('Nunito-ExtraBold.ttf', 26), ('Nunito-ExtraBold.ttf', 20), ('Nunito-SemiBold.ttf', 17)))
rgb = lambda h: tuple(int(h[i:i + 2], 16) for i in (1, 3, 5))
def board(lv, S=11):
    im = Image.new('RGB', (lv['w'] * S, lv['h'] * S), '#FFF8EA'); d = ImageDraw.Draw(im)
    for y, r in enumerate(lv['rowsBottomFirst'][::-1]):
        for x, c in enumerate(r):
            b = rgb(PAL[c]); x0, y0 = x * S, y * S
            d.rectangle([x0, y0 + 1, x0 + S - 2, y0 + S - 1], fill=tuple(int(v * .78) for v in b))
            d.rectangle([x0, y0, x0 + S - 2, y0 + S - 3], fill=b)
            d.rectangle([x0 + 2, y0 + 1, x0 + S - 4, y0 + 2], fill=tuple(int(v + (255 - v) * .35) for v in b))
    return im
if sys.argv[1] == '--contact':
    data = json.load(open(sys.argv[2])); S, cols = 10, 5; cw, ch = 36 * S + 20, 36 * S + 44
    sheet = Image.new('RGB', (cols * cw, 70 + ((len(data) + cols - 1) // cols) * ch), '#FFF8EA'); d = ImageDraw.Draw(sheet)
    d.text((10, 16), 'v8E · все 30 картинок (поле по роли, цвета вводятся по одному)', font=big, fill='#34493d')
    for k, e in enumerate(data):
        lv = e['new']; x, y = (k % cols) * cw + 10, 70 + (k // cols) * ch
        sheet.paste(board(lv, S), (x, y + 30))
        d.text((x, y + 4), f"L{e['id']} · {lv['title']} · {lv['w']}×{lv['h']} · {len(set(''.join(lv['rowsBottomFirst'])) - set('.#'))} цв.", font=sm, fill='#34493d')
    sheet.save(sys.argv[3]); print(sys.argv[3], sheet.size); sys.exit()
data = json.load(open(sys.argv[1])); rows = (len(data) + 1) // 2
sheet = Image.new('RGB', (1900, 230 + rows * 440 + (500 if len(sys.argv) > 3 else 0)), '#FFF8EA'); d = ImageDraw.Draw(sheet)
d.text((30, 20), 'v8E · проба картинок: 16 цветов (по 4 на героя), поле по сложности, объём и ровный контур', font=big, fill='#34493d')
for h, hero in enumerate(('sprig', 'bloom', 'drop', 'twig')):
    x = 30 + h * 470; d.text((x, 70), hero.capitalize(), font=mid, fill='#34493d')
    for i, k in enumerate([i for i, v in enumerate(heroes) if v == hero]):
        cx = x + i * 112; d.rounded_rectangle([cx, 102, cx + 100, 152], 8, fill=PAL[codes[k]]); d.text((cx + 4, 156), names[k] + (' ·new' if k >= 9 else ''), font=sm, fill='#34493d')
for k, e in enumerate(data):
    x0, y0 = 30 + (k % 2) * 940, 200 + (k // 2) * 440
    for j, (lv, tag) in enumerate(((e['old'], 'было v8D'), (e['new'], 'v8E'))):
        xx = x0 + j * (36 * 11 + 30); sheet.paste(board(lv), (xx, y0 + 30))
        d.text((xx, y0), f"L{e['id']} {tag} · {lv['w']}×{lv['h']} · {len(set(''.join(lv['rowsBottomFirst'])) - set('.#'))} цв.", font=mid, fill='#34493d')
if len(sys.argv) > 3:
    y = 200 + rows * 440; d.text((30, y), 'Для сравнения: Colony Flow L20 Super Hard (36×36, 9 цветов)', font=mid, fill='#34493d')
    sheet.paste(Image.open(sys.argv[3]).convert('RGB').resize((520, 451)), (30, y + 30))
sheet.save(sys.argv[2]); print(sys.argv[2], sheet.size)

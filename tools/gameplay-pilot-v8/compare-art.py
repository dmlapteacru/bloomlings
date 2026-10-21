"""Before/after rendered directly from gameplay cells at the same scale."""
import json
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
old = json.loads((ROOT / 'content/pilot-v7/levels.json').read_text())['levels']
new = json.loads((ROOT / 'content/pilot-v8/levels.json').read_text())['levels']
colors = dict(G='#78C04A', P='#EB799C', B='#36B7DA', A='#A45F28', C='#F5E6BD', D='#334B48', V='#7851A5', Y='#F4C744', O='#EF8A32')
colors.update({'.': '#e4ead7', '#': '#b6d6cc'})
font = ImageFont.truetype(str(ROOT / 'client/Assets/Bloomlings/UI/Fonts/Resources/Nunito-SemiBold.ttf'), 17)
im = Image.new('RGB', (1260, 990), '#fffaf0')
draw = ImageDraw.Draw(im)
for tile, i in enumerate([3,4,14,15,16,18,22,25,27]):
    x, y = (tile % 3)*420, (tile // 3)*330
    draw.text((x+14, y+8), f'L{i+1} · {new[i]["title"]}', font=font, fill='#34493d')
    for j, level in enumerate((old[i], new[i])):
        draw.text((x+14+j*205, y+34), 'v7' if j == 0 else 'v8', font=font, fill='#34493d')
        cell = min(190 // level['w'], 260 // level['h'])
        ox = x+10+j*205+(190-level['w']*cell)//2
        oy = y+64+(250-level['h']*cell)//2
        for ry, row in enumerate(reversed(level['rowsBottomFirst'])):
            for rx, c in enumerate(row):
                draw.rectangle((ox+rx*cell,oy+ry*cell,ox+(rx+1)*cell-1,oy+(ry+1)*cell-1),fill=colors[c])
path = ROOT / 'product/previews/gameplay-pilot-v8/art-before-after.png'
im.save(path)
print(path)

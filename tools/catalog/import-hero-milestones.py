#!/usr/bin/env python3
"""Read the owned hero atlas into four exact 36x36 authoring grids (no image editing).

Run from the repository root, then `pictures import --src content/work/hero-art/src
--out content/work/hero-art/library`. Review with the finished-board painter before staging.
"""
import json
from collections import Counter
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / 'content/launch-art'
OUT = ROOT / 'content/work/hero-art'

# Flat source colors are quantized to the game's existing exact target variants.
PALETTES = {
    'sprig': [('sky', '#b0e5cd', 'blue_cyan', 'dew'), ('leaf', '#7db63b', 'green', 'leaf'),
              ('eyes', '#235d3f', 'brown_orange', 'wood'), ('face', '#eff7af', 'lime', 'vine'),
              ('cheeks', '#f28aa2', 'pink_purple', 'flower')],
    'bloom': [('sky', '#b0e5cd', 'green', 'moss'), ('petals', '#f17899', 'pink_purple', 'flower'),
              ('shadow', '#e45a7c', 'pink_purple', 'violet_bud'), ('face', '#fff0c7', 'lime', 'vine'),
              ('eyes', '#235d3f', 'brown_orange', 'wood')],
    'drop': [('sky', '#b0e5cd', 'green', 'moss'), ('body', '#54bdd7', 'blue_cyan', 'dew'),
             ('shadow', '#32acce', 'blue_cyan', 'water'), ('eyes', '#235d3f', 'brown_orange', 'wood'),
             ('shine', '#fff0c7', 'lime', 'vine'), ('cheeks', '#f28aa2', 'pink_purple', 'flower')],
    'twig': [('sky', '#b0e5cd', 'blue_cyan', 'dew'), ('cap', '#db942f', 'brown_orange', 'acorn'),
             ('leaf', '#7db63b', 'green', 'leaf'), ('face', '#fff0c7', 'lime', 'vine'),
             ('eyes', '#235d3f', 'brown_orange', 'wood'), ('cheeks', '#f28aa2', 'pink_purple', 'flower')],
}

def main():
    image = Image.open(ART / 'references/hero-milestones.png').convert('RGB')
    # These are measured panel bounds in the 1254-square source, excluding the white gutters.
    bounds = [(29,25,614,617), (642,25,1225,617), (29,640,614,1227), (642,640,1225,1227)]
    book = json.loads((ART / 'compositions.json').read_text())
    incoming = {}
    source = OUT / 'src'
    source.mkdir(parents=True, exist_ok=True)
    for (hero, palette), (level, box) in zip(PALETTES.items(), zip((100,500,1000,2000), bounds)):
        colors = [tuple(bytes.fromhex(c[1][1:])) for c in palette]
        def classify(rgb):
            # Leaf veins belong to the same broad green mass; only the dark face marks use Wood.
            if hero == 'sprig' and rgb[0] < 130 and rgb[1] > 90 and rgb[1] > rgb[0] * 1.15:
                return 1
            return min(range(len(colors)), key=lambda i: sum((a-b)**2 for a,b in zip(rgb,colors[i])))
        x0,y0,x1,y1 = box
        grid = []
        for y in range(36):
            row = []
            for x in range(36):
                if x in (0,35) or y in (0,35):
                    row.append('.')
                    continue
                samples = [classify(image.getpixel((int(x0+(x+sx)*(x1-x0)/36), int(y0+(y+sy)*(y1-y0)/36))))
                           for sy in (.25,.5,.75) for sx in (.25,.5,.75)]
                row.append(str(Counter(samples).most_common(1)[0][0]))
            grid.append(row)
        # A continuous sky margin keeps the initial foreground pods out of reach.
        for y in range(1,35):
            grid[y][1] = grid[y][34] = '0'
        for x in range(1,35):
            grid[1][x] = grid[34][x] = '0'
        counts = Counter(c for row in grid for c in row)
        if any(counts[str(i)] < 5 for i in range(len(palette))):
            raise ValueError(f'{hero}: a source color cannot fill a legal pod: {counts}')
        cid = 'hero_' + hero
        metadata = dict(id=cid, version=1, subject=hero.title(), width=36, height=36,
            roles=[dict(roleId=r, name=r, colorGroup=g, **({'isBackground':True} if i==0 else {}))
                   for i,(r,c,g,v) in enumerate(palette)],
            legend={str(i):p[0] for i,p in enumerate(palette)}, finishedLook=dict(mode='auto'),
            tags=dict(themes=['launch-original','hero-milestone'], bands=['big']),
            review=dict(status='draft'), source=dict(kind='generated_edited',licence='owned',
                origin=f'content/launch-art/references/hero-milestones.png panel {list(PALETTES).index(hero)+1}; exact 36x36 grid'))
        (source/(cid+'.grid.txt')).write_text('\n'.join(''.join(row) for row in grid)+'\n')
        (source/(cid+'.meta.json')).write_text(json.dumps(metadata,indent=2)+'\n')
        entry = dict(book['compositions'][level-1])
        entry['previewMapping'] = {r:v for r,c,g,v in palette}
        incoming[level] = entry
        print(cid, counts)
    for level,entry in incoming.items():
        book['compositions'][level-1] = entry
    (OUT/'compositions.candidate.json').write_text(json.dumps(book,ensure_ascii=False,indent=2)+'\n')

if __name__ == '__main__':
    main()

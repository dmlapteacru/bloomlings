#!/usr/bin/env python3
"""Authoring data importer: read a generated atlas into exact-cell text grids, never edit the reference image.
Pillow is needed only by the artist; local level generation uses the checked-in grids/JSON and .NET alone.
"""
import argparse
import colorsys
import json
from collections import Counter
from pathlib import Path
from PIL import Image


class ColorMassError(ValueError):
    def __init__(self, cid, counts, minimum, maximum):
        self.composition_id = cid
        self.counts = counts
        self.minimum = minimum
        self.maximum = maximum
        super().__init__(f'{cid}: inadequate target-cell color masses: {dict(counts)}')


def blocked_pod_capacity(grid):
    """How many minimum-size pods have a completely hidden color at the default entry, before overlays."""
    from collections import deque
    height, width = len(grid), len(grid[0])
    pending = deque([(width//2-1,height-1)])
    visited = set(pending)
    available = set()
    while pending:
        x,y = pending.popleft()
        for xx,yy in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)):
            if not (0 <= xx < width and 0 <= yy < height):
                continue
            value = grid[yy][xx]
            if value != '.':
                available.add(value)
            elif (xx,yy) not in visited:
                visited.add((xx,yy));pending.append((xx,yy))
    counts = Counter(c for row in grid for c in row if c != '.')
    return sum(n//5 for c,n in counts.items() if c not in available)


def gutters(image, axis):
    n, m = (image.width, image.height) if axis == 0 else (image.height, image.width)
    runs = []
    for p in range(n):
        hits = 0
        for q in range(0, m, 3):
            r, g, b = image.getpixel((p, q) if axis == 0 else (q, p))
            hits += r > 220 and g > 215 and b > 190 and abs(r - g) < 22
        if hits / len(range(0, m, 3)) > .9:
            if runs and p == runs[-1][-1] + 1:
                runs[-1].append(p)
            else:
                runs.append([p])
    return [(r[0], r[-1]) for r in runs if len(r) >= 4]


def panel_boundaries(image, columns, rows):
    """Validate every row separately: image models can offset vertical gutters between rows.
    An outer gutter may be cropped off; internal missing panels/gutters are never invented.
    """
    def edges(runs, size, count):
        runs=list(runs)
        if runs and runs[0][0] > size/count*.5:
            runs.insert(0,(-1,-1))
        if runs and size-1-runs[-1][1] > size/count*.5:
            runs.append((size,size))
        if len(runs)!=count+1:
            raise ValueError(f'expected {count} panels; found {len(runs)-1}')
        widths=[runs[i+1][0]-runs[i][1]for i in range(count)]
        if min(widths)<size/count*.6 or max(widths)>size/count*1.4:
            raise ValueError('Uneven or missing panels; inspect the source atlas')
        return runs
    ys=edges(gutters(image,1),image.height,rows)
    row_columns=[]
    for row in range(rows):
        strip=image.crop((0,max(0,ys[row][1]+2),image.width,min(image.height,ys[row+1][0]-2)))
        row_columns.append(edges(gutters(strip,0),image.width,columns))
    return row_columns,ys


def classify(rgb, role_count, red=False, adaptive=False, green_cuts=(.59, .76)):
    r, g, b = rgb
    hue, saturation, value = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
    hue *= 360
    if 150 <= hue <= 240 and b > r:
        return 's'
    if role_count >= 5 and 38 <= hue < 78 and g >= r * .75 and value > .55:
        return 'y'
    if 65 <= hue < 170 and g > r * .9:
        if adaptive:
            return 'm' if value < green_cuts[0] else 'y' if value > green_cuts[1] else 'g'
        return 'm' if role_count >= 6 and value < .58 else 'g'
    if ((hue >= 260 or hue < 20) and b > g * .7 and r > g * 1.15) or (red and (hue < 15 or hue > 345) and r > g * 1.5):
        return 'q' if adaptive and not red and value < .81 else 'p'
    return 'a'


def import_batch(batch_path, plan_path, registry_path, output, selected_levels=None):
    batch = json.loads(batch_path.read_text())
    plan = json.loads(plan_path.read_text())
    book = json.loads(registry_path.read_text())
    reference = Path(batch['referenceFile'])
    image = Image.open(reference).convert('RGB')
    columns, rows = batch['columns'], batch['rows']
    row_columns, ys = panel_boundaries(image, columns, rows)
    if len(batch['entries']) > columns * rows:
        raise ValueError('more entries than atlas panels')
    output.mkdir(parents=True, exist_ok=True)
    source = output / 'src'
    source.mkdir(exist_ok=True)
    existing = {e['launchLevel']: e for e in book['compositions']}
    for index, entry in enumerate(batch['entries']):
        n, cid = entry['level'], entry['compositionId']
        if selected_levels is not None and n not in selected_levels:
            continue
        slot = plan['slots'][n - 1]
        width, height = slot['width'], slot['height']
        col, row = index % columns, index // columns
        xs = row_columns[row]
        x0, x1 = xs[col][1] + 2, xs[col + 1][0] - 2
        y0, y1 = ys[row][1] + 2, ys[row + 1][0] - 2
        roles = batch['paletteRoles']
        red = entry.get('red', False)
        cuts = (.59, .76)
        if entry.get('adaptiveShades'):
            values = []
            for y in range(y0, y1, 2):
                for x in range(x0, x1, 2):
                    rgb = image.getpixel((x,y))
                    hue, sat, value = colorsys.rgb_to_hsv(*(v/255 for v in rgb))
                    if 65 <= hue*360 < 170 and rgb[1] > rgb[0]*.9:
                        values.append(value)
            if values:
                values.sort()
                centers = [values[0], values[len(values)//2], values[-1]]
                for _ in range(12):
                    clusters = [[], [], []]
                    for value in values:
                        k = min(range(3), key=lambda j: abs(value-centers[j]))
                        clusters[k].append(value)
                    centers = sorted(sum(c)/len(c) if c else centers[i] for i,c in enumerate(clusters))
                cuts = ((centers[0]+centers[1])/2, (centers[1]+centers[2])/2)
        role = lambda rgb: classify(rgb, roles, red, entry.get('adaptiveShades', False), cuts)
        foreground = []
        for y in range(y0, y1, 2):
            for x in range(x0, x1, 2):
                rgb = image.getpixel((x, y))
                if role(rgb) != 's' and not (rgb[0] > 225 and rgb[1] > 215 and rgb[2] > 185):
                    foreground.append((x, y))
        if not foreground:
            raise ValueError(f'{cid}: empty foreground')
        bx0, bx1 = min(x for x, y in foreground), max(x for x, y in foreground)
        by0, by1 = min(y for x, y in foreground), max(y for x, y in foreground)
        scale = min((width - 2) / (bx1 - bx0 + 1), (height - 2) / (by1 - by0 + 1)) * entry.get('zoom', 1)
        cw, ch, cx, cy = width / scale, height / scale, (bx0 + bx1) / 2, (by0 + by1) / 2
        grid = []
        for gy in range(height):
            line = []
            for gx in range(width):
                samples = []
                for sy in (.25, .5, .75):
                    for sx in (.25, .5, .75):
                        x, y = round(cx - cw / 2 + (gx + sx) * cw / width), round(cy - ch / 2 + (gy + sy) * ch / height)
                        samples.append(role(image.getpixel((x, y))) if x0 <= x < x1 and y0 <= y < y1 else 's')
                line.append(Counter(samples).most_common(1)[0][0])
            grid.append(line)
        for gy, margin in [(0, 3), (1, 2), (2, 1), (height - 3, 1), (height - 2, 2), (height - 1, 3)]:
            for gx in range(margin):
                grid[gy][gx] = grid[gy][width - 1 - gx] = '.'
        if width * height > 500:
            for x in range(width):
                grid[0][x] = grid[-1][x] = '.'
            for y in range(height):
                grid[y][0] = grid[y][-1] = '.'
        grid[-1][width // 2 - 1] = grid[-1][width // 2] = '.'
        clearance = entry.get('skyHalo') or blocked_pod_capacity(grid) < 5
        if clearance:
            original = [r[:] for r in grid]
            for y in range(height):
                for x in range(width):
                    if original[y][x] not in ('.','s') and any(
                        not (0 <= yy < height and 0 <= xx < width) or original[yy][xx] == '.'
                        for yy,xx in ((y-1,x),(y+1,x),(y,x-1),(y,x+1))):
                        grid[y][x] = 's'
        counts = Counter(''.join(''.join(r) for r in grid))
        # Adaptive source shades are optional palette subdivisions, not extra progression variants.
        # Collapse the smallest subdivision back into its original color when the slot budget is full.
        for shade, base in sorted((('m','g'),('q','p')), key=lambda pair: counts[pair[0]]):
            if sum(counts[c] > 0 for c in ('s','g','m','a','p','q','y')) <= slot['variantCount'][1]:
                break
            if counts[shade] and counts[base]:
                for line in grid:
                    for x, color in enumerate(line):
                        if color == shade:
                            line[x] = base
                counts[base] += counts[shade]
                counts[shade] = 0
        minimum = slot['variantCount'][0]
        # Discard optional sub-cell flecks before trying to enlarge real accents. A detached one-pixel
        # artifact has no adjacent foreground to borrow; it must not block an otherwise complete palette.
        for color, count in list(counts.items()):
            mandatory = (color == 'y' and n in (45, 46)) or (color == 'p' and red)
            if color in ('.', 's') or mandatory or not 0 < count < 5:
                continue
            if sum(v >= 5 for k, v in counts.items() if k != '.') < minimum:
                continue
            for y in range(height):
                for x in range(width):
                    if grid[y][x] == color:
                        neighbors = [grid[yy][xx] for yy, xx in ((y-1,x),(y+1,x),(y,x-1),(y,x+1))
                                     if 0 <= yy < height and 0 <= xx < width and grid[yy][xx] not in (color, '.')]
                        grid[y][x] = Counter(neighbors).most_common(1)[0][0] if neighbors else 's'
            counts = Counter(''.join(''.join(r) for r in grid))
        # A genuine sub-cell accent may leave 1–4 tiles after sampling. Give that existing patch the smallest
        # playable footprint (five tiles), borrowing adjacent foreground only. This edits the authored cell data,
        # never the reference image; no absent color or new illustration is manufactured.
        expanded = {}
        for color in ('g', 'm', 'a', 'p', 'q', 'y'):
            if not 0 < counts[color] < 5:
                continue
            expanded[color] = 5 - counts[color]
            while counts[color] < 5:
                candidates = []
                for y in range(height):
                    for x in range(width):
                        donor = grid[y][x]
                        if donor in (color, '.', 's') or counts[donor] <= 5:
                            continue
                        adjacent = sum(grid[yy][xx] == color for yy, xx in
                            ((y-1,x),(y+1,x),(y,x-1),(y,x+1)) if 0 <= yy < height and 0 <= xx < width)
                        if adjacent:
                            candidates.append((-adjacent, -counts[donor], y, x))
                if not candidates:
                    raise ValueError(f'{cid}: accent {color} cannot get a readable footprint')
                _, _, y, x = min(candidates)
                counts[grid[y][x]] -= 1
                grid[y][x] = color
                counts[color] += 1
        # Sub-cell accents may disappear on a small board, only when the remaining palette fits progression.
        for c, count in list(counts.items()):
            mandatory = (c == 'y' and n in (45, 46)) or (c == 'p' and red)
            if c != '.' and not mandatory and count < 5 and sum(v >= 5 for k, v in counts.items() if k != '.') >= minimum:
                for y in range(height):
                    for x in range(width):
                        if grid[y][x] == c:
                            adjacent = [grid[yy][xx] for yy, xx in [(y-1,x),(y+1,x),(y,x-1),(y,x+1)]
                                        if 0 <= yy < height and 0 <= xx < width and grid[yy][xx] not in (c, '.')]
                            grid[y][x] = Counter(adjacent).most_common(1)[0][0] if adjacent else 's'
        counts = Counter(''.join(''.join(r) for r in grid))
        green = ['leaf', 'moss', 'leaf', 'moss'][n % 4] if roles < 6 else 'leaf'
        orange = ['acorn', 'wood', 'wood', 'acorn'][n % 4]
        pink = ['flower', 'violet_bud', 'violet_bud', 'flower'][n % 4]
        info = {'s': ('sky', 'blue_cyan', 'dew'), 'g': ('leaf', 'green', green), 'm': ('shadow', 'green', 'moss'),
                'a': ('wood', 'brown_orange', orange), 'p': ('petal', 'red' if red else 'pink_purple', 'berry' if red else pink),
                'q': ('petal_shadow', 'pink_purple', 'violet_bud'),
                'y': ('light', 'lime', 'vine')}
        if 'palette' in entry:
            info = {c: (rid, group, entry['palette'].get(rid, variant)) for c, (rid, group, variant) in info.items()}
        if counts['m'] and counts['g']:
            info['g'] = ('leaf', 'green', 'leaf')
            info['m'] = ('shadow', 'green', 'moss')
        if counts['q'] and counts['p']:
            info['p'] = ('petal', 'pink_purple', 'flower')
        used = [c for c in info if counts[c] > 0]
        if not minimum <= len(used) <= slot['variantCount'][1] or any(counts[c] < 5 for c in used):
            raise ColorMassError(cid, counts, minimum, slot['variantCount'][1])
        metadata = {'id': cid, 'version': 1, 'subject': entry.get('displayName', entry['title']), 'width': width, 'height': height,
                    'roles': [{'roleId': info[c][0], 'name': info[c][0], 'colorGroup': info[c][1],
                               **({'isBackground': True} if c == 's' else {})} for c in used],
                    'legend': {c: info[c][0] for c in used}, 'finishedLook': {'mode': 'auto'},
                    'tags': {'themes': ['launch-original'], 'bands': ['regular' if width * height <= 288 else 'big']},
                    'review': {'status': 'draft'}, 'source': {'kind': 'generated_edited', 'licence': 'owned',
                    'origin': f'{reference.as_posix()} panel {index+1}; quantized exact target-cell authoring grid'
                              + (f'; minimum accent cell repair {expanded}' if expanded else '')
                              + ('; one-cell entry clearance' if clearance else '')}}
        (source / (cid + '.grid.txt')).write_text('\n'.join(''.join(r) for r in grid) + '\n')
        (source / (cid + '.meta.json')).write_text(json.dumps(metadata, ensure_ascii=False, indent=2) + '\n')
        existing[n] = {'compositionId': cid, 'pictureId': cid, 'version': 1, 'title': entry['title'],
                       'width': width, 'height': height, 'artStatus': 'generated-awaiting-quality-check',
                       'approvalBasis': 'owner-approved production direction; individual grid requires production checks',
                       'launchLevel': n, 'previewMapping': {info[c][0]: info[c][2] for c in used}}
    book['compositions'] = [existing[n] for n in sorted(existing)]
    (output / 'compositions.candidate.json').write_text(json.dumps(book, ensure_ascii=False, indent=2) + '\n')
    print(f"Imported {len(batch['entries'])} source grids into {output}; production review remains required.")


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--batch', type=Path, required=True)
    p.add_argument('--plan', type=Path, default=Path('content/launch-art/plan.json'))
    p.add_argument('--registry', type=Path, default=Path('content/launch-art/compositions.json'))
    p.add_argument('--out', type=Path, required=True)
    args = p.parse_args()
    import_batch(args.batch, args.plan, args.registry, args.out)


if __name__ == '__main__':
    main()

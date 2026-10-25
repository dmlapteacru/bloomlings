"""Board colours (coverboard.py clusters) → the capsule colours of a transcribed round (check-round.mjs format).
A cluster starts at the capsule colour whose name (the round's legend) is nearest in Lab, then moves to another colour
within reach when that brings the cells of each colour closer to the capsules' total of that colour (shading splits one
colour into several clusters; similar names can swap). Prints the accounting; what stays off is in the round's notes
(board boxes and crates, "?" tiles, recognition near the watermark).
usage: python3 -I map-colours.py <board.json> <round.json> <out.json>
writes {w, h, rows (round letters, "." no cube), legend {letter: {name, hex}}, accounting {letter: {board, capsules}}}"""
import sys, json, re
import numpy as np
board, rnd = json.load(open(sys.argv[1])), json.load(open(sys.argv[2]))
def lab(rgb):
    c = np.asarray(rgb, float) / 255.0
    c = np.where(c > 0.04045, ((c + 0.055) / 1.055) ** 2.4, c / 12.92)
    M = np.array([[0.4124, 0.3576, 0.1805], [0.2126, 0.7152, 0.0722], [0.0193, 0.1192, 0.9505]])
    xyz = c @ M.T / np.array([0.9505, 1.0, 1.089])
    f = np.where(xyz > 0.008856, np.cbrt(xyz), 7.787 * xyz + 16 / 116)
    return np.array([116 * f[1] - 16, 500 * (f[0] - f[1]), 200 * (f[1] - f[2])])
NAMES = [  # longest first: the first name found in a legend entry gives its estimated colour
    ('light grey', (205, 205, 212)), ('light pink', (250, 190, 215)), ('hot pink', (240, 60, 150)), ('medium pink', (245, 110, 170)),
    ('light blue', (110, 200, 245)), ('sky blue', (90, 190, 245)), ('pale light blue', (200, 230, 250)), ('dark green', (40, 125, 70)),
    ('light green', (170, 230, 90)), ('light yellow', (250, 235, 160)), ('pale yellow', (250, 235, 170)), ('light orange', (250, 190, 110)),
    ('dark red', (170, 30, 50)), ('dark blue', (50, 60, 150)), ('dark purple', (70, 50, 90)), ('red-brown', (140, 55, 50)),
    ('brick', (170, 70, 55)), ('ice', (205, 235, 250)), ('crimson', (205, 35, 65)), ('maroon', (125, 45, 55)), ('rust', (175, 75, 45)),
    ('brown', (155, 85, 50)), ('amber', (245, 175, 40)), ('gold', (245, 190, 40)), ('orange', (245, 135, 40)), ('yellow', (250, 220, 60)),
    ('cream', (250, 238, 180)), ('peach', (250, 185, 150)), ('skin', (250, 190, 155)), ('salmon', (250, 140, 120)), ('coral', (250, 120, 110)),
    ('fuchsia', (230, 50, 170)), ('magenta', (235, 60, 160)), ('pink', (250, 120, 180)), ('lavender', (190, 165, 235)), ('violet', (140, 90, 210)),
    ('purple', (150, 90, 190)), ('indigo', (70, 65, 160)), ('navy', (55, 60, 130)), ('teal', (40, 170, 165)), ('turquoise', (60, 210, 200)),
    ('cyan', (80, 210, 240)), ('aqua', (120, 225, 235)), ('blue', (60, 130, 230)), ('lime', (165, 230, 70)), ('green', (75, 190, 85)),
    ('white', (242, 246, 248)), ('grey', (180, 180, 190)), ('gray', (180, 180, 190)), ('black', (60, 50, 70)), ('dark', (65, 52, 75)), ('red', (232, 60, 80))]
def estimate(name):
    n = name.lower()
    for k, rgb in NAMES:
        if re.search(r'\b' + re.escape(k) + r'\b', n): return rgb
    raise SystemExit('no colour for legend name ' + name)
caps = {}
for col in rnd['columns'].values():
    for t in col:
        m = re.match(r'^([A-Za-z])(\d+)', t)
        if m: caps[m[1]] = caps.get(m[1], 0) + int(m[2])
letters = sorted(caps)
est = {k: lab(estimate(rnd['legend'][k])) for k in letters}
clusters = [(p['letter'], lab([int(p['rgb'][i:i + 2], 16) for i in (1, 3, 5)]), p['cells'], p['rgb']) for p in board['palette']]
dist = {(c, k): float(np.linalg.norm(l - est[k])) for c, l, _, _ in clusters for k in letters}
assign = {c: min(letters, key=lambda k: dist[(c, k)]) for c, _, _, _ in clusters}
def error(a):
    cells = {k: 0 for k in letters}
    for c, _, n, _ in clusters: cells[a[c]] += n
    return sum(abs(cells[k] - caps[k]) for k in letters), cells
best, _ = error(assign); moved = True
while moved:   # one cluster at a time, to a colour within reach of its picture colour, while the totals improve
    moved = False
    for c, l, n, _ in clusters:
        near = min(dist[(c, k)] for k in letters)
        for k in letters:
            if k == assign[c] or dist[(c, k)] > max(35, near * 1.8): continue
            trial = dict(assign); trial[c] = k; e, _ = error(trial)
            if e < best: best, assign, moved = e, trial, True
_, cells = error(assign)
rows = [''.join('.' if ch == '.' else assign[ch] for ch in r) for r in board['rows']]
hexes = {k: max(((n, h) for c, _, n, h in clusters if assign[c] == k), default=(0, None))[1] for k in letters}
out = dict(w=board['w'], h=board['h'], rows=rows, legend={k: {'name': rnd['legend'][k], **({'hex': hexes[k]} if hexes[k] else {})} for k in letters},
           accounting={k: {'board': cells[k], 'capsules': caps[k]} for k in letters}, offBy=best,
           clusters={c: assign[c] for c, _, _, _ in clusters})
json.dump(out, open(sys.argv[3], 'w'), indent=1)
print(f"{board['w']}x{board['h']} off by {best} of {sum(caps.values())}:", ' '.join(f"{k}{cells[k]}/{caps[k]}" for k in letters if cells[k] != caps[k]) or 'exact')

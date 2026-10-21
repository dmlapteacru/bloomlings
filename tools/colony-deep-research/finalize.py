"""Rename grid letters to mnemonic symbols, patch L5's watermark from the video grid, recompute colour-layer depth."""
import json, collections, sys
G = '/tmp/claude-0/-home-user-bloomlings/17da13b7-11ad-5226-8b0f-bc66792e8f73/scratchpad/grids/'
NAMES = {
 'L1': {'A': ('Y', 'жёлтый'), 'B': ('O', 'оранжевый'), 'C': ('W', 'белый')},
 'L2': {'A': ('C', 'голубой'), 'B': ('P', 'розовый'), 'C': ('Y', 'жёлтый')},
 'L3': {'A': ('R', 'малиновый'), 'B': ('#', 'тёмный контур'), 'C': ('L', 'лайм'), 'D': ('G', 'зелёный'), 'E': ('s', 'лососевый (блик)'), 'F': ('W', 'белый')},
 'L4': {'A': ('R', 'красный'), 'B': ('C', 'голубой'), 'C': ('#', 'тёмный контур'), 'D': ('Y', 'жёлтый'), 'E': ('G', 'зелёный'), 'F': ('p', 'персиковый'), 'G': ('W', 'белый'), 'H': ('b', 'коричневый')},
 'L5': {'A': ('O', 'оранжевый'), 'B': ('V', 'фиолетовый'), 'C': ('C', 'голубой'), 'D': ('c', 'кремовый'), 'E': ('#', 'тёмный'), 'F': ('P', 'розовый'), 'G': ('l', 'светло-розовый'), 'H': ('Y', 'жёлтый'), 'I': ('G', 'зелёный'), 'J': ('G', 'зелёный'), 'M': ('M', 'пурпурный')},
 'L10': {'A': ('S', 'голубой (небо)'), 'B': ('#', 'тёмный контур'), 'C': ('W', 'белый'), 'D': ('O', 'оранжевый'), 'E': ('P', 'розовый'), 'F': ('T', 'бирюзовый'), 'G': ('Y', 'жёлтый'), 'H': ('G', 'зелёный'), 'I': ('c', 'кремовый'), 'J': ('R', 'красный'), 'K': ('g', 'тёмно-зелёный')},
}
def depth(rows):
    H, W = len(rows), len(rows[0]); INF = 10**9
    best = {}; dq = collections.deque()
    out = set()
    q = collections.deque([(x, y) for y in range(-1, H + 1) for x in (-1, W)] + [(x, y) for x in range(W) for y in (-1, H)])
    out |= set(q)
    while q:
        x, y = q.popleft()
        for dx, dy in ((1,0),(-1,0),(0,1),(0,-1)):
            n = (x+dx, y+dy)
            if 0 <= n[0] < W and 0 <= n[1] < H and rows[n[1]][n[0]] == '.' and n not in out: out.add(n); q.append(n)
    for (x, y) in out:
        for dx, dy in ((1,0),(-1,0),(0,1),(0,-1)):
            n = (x+dx, y+dy)
            if 0 <= n[0] < W and 0 <= n[1] < H and rows[n[1]][n[0]] != '.' and n not in best: best[n] = 0; dq.append(n)
    while dq:
        x, y = dq.popleft(); d = best[(x, y)]
        for dx, dy in ((1,0),(-1,0),(0,1),(0,-1)):
            n = (x+dx, y+dy)
            if not (0 <= n[0] < W and 0 <= n[1] < H): continue
            c = rows[n[1]][n[0]]
            nd = d if c == '.' else d + (0 if c == rows[y][x] else 1)
            if nd < best.get(n, INF):
                best[n] = nd; (dq.appendleft if nd == d else dq.append)(n)
    res = collections.defaultdict(collections.Counter)
    for (x, y), d in best.items():
        if rows[y][x] != '.': res[rows[y][x]][d] += 1
    return res
out = {}
for L, names in NAMES.items():
    src = 'siteL5' if L == 'L5' else L
    d = json.load(open(G + src + '.json'))
    rows = [list(r) for r in d['rows']]
    if L == 'L5':
        v = json.load(open(G + 'L5.json'))['rows']
        vmap = {'A': 'A', 'B': 'C', 'C': 'D', 'D': 'B', 'E': 'E', 'F': 'F', 'G': 'G', 'H': 'H', 'I': 'I', 'J': 'M'}
        for y in range(len(rows)):
            for x in range(len(rows[0])):
                if rows[y][x] in '?KL':
                    vc = v[y][x + 1] if y < len(v) and x + 1 < len(v[y]) else '.'
                    rows[y][x] = vmap.get(vc, 'D')
        rows = rows[:24]
    # drop trailing empty rows/cols
    while rows and all(c == '.' for c in rows[-1]): rows.pop()
    pal = {p['letter']: p for p in d['palette']}
    sym = [[(names[c][0] if c in names else ('.' if c == '.' else '?')) for c in r] for r in rows]
    rows_s = [''.join(r) for r in sym]
    dep = depth(rows_s)
    legend = {}
    for letter, (s, nm) in names.items():
        if s in legend: continue
        hexes = [pal[l]['rgb'] for l, (s2, _) in names.items() if s2 == s and l in pal]
        cnt = sum(r.count(s) for r in rows_s)
        if cnt: legend[s] = dict(name=nm, hex=hexes[0] if hexes else '', cells=cnt, depth={int(k): int(v) for k, v in sorted(dep[s].items())})
    out[L] = dict(w=len(rows_s[0]), h=len(rows_s), tiles=sum(v['cells'] for v in legend.values()), legend=legend, rows=rows_s)
json.dump(out, open(G + 'final.json', 'w'), ensure_ascii=False, indent=1)
for L, v in out.items():
    print(L, v['w'], 'x', v['h'], 'tiles', v['tiles'])
    for s, e in sorted(v['legend'].items(), key=lambda t: -t[1]['cells']): print('  ', s, e['name'], e['cells'], e['depth'])

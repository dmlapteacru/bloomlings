import json, sys
sys.path.insert(0, '/tmp/claude-0/-home-user-bloomlings/17da13b7-11ad-5226-8b0f-bc66792e8f73/scratchpad/scripts')
import importlib.util
spec = importlib.util.spec_from_file_location('fin', '/tmp/claude-0/-home-user-bloomlings/17da13b7-11ad-5226-8b0f-bc66792e8f73/scratchpad/scripts/finalize_depth.py'); fin = importlib.util.module_from_spec(spec); spec.loader.exec_module(fin)
G = json.load(open('/tmp/claude-0/-home-user-bloomlings/17da13b7-11ad-5226-8b0f-bc66792e8f73/scratchpad/grids/final.json'))
T = json.load(open('/tmp/claude-0/-home-user-bloomlings/17da13b7-11ad-5226-8b0f-bc66792e8f73/scratchpad/cfsim/levels.json'))
out = {}
for L in ['L1', 'L2', 'L3', 'L4', 'L5', 'L10']:
    g = G[L]; t = T[L]
    rows = list(g['rows'])
    if L == 'L5':  # the simulator's one-cell accounting fix (lightpink -> pink next to pink)
        rows = [list(r) for r in rows]; done = False
        for y in range(len(rows)):
            for x in range(len(rows[0])):
                if not done and rows[y][x] == 'l' and any(0 <= y+dy < len(rows) and 0 <= x+dx < len(rows[0]) and rows[y+dy][x+dx] == 'P' for dx, dy in ((1,0),(-1,0),(0,1),(0,-1))):
                    rows[y][x] = 'P'; done = True
        rows = [''.join(r) for r in rows]
        g['legend']['P']['cells'] += 1; g['legend']['l']['cells'] -= 1
    dep = fin.depth(rows)
    for sym in g['legend']: g['legend'][sym]['depth'] = {int(k): v for k, v in dep[sym].items()}
    pods = {}
    for col in t['pods'].values():
        for p in col: pods.setdefault(p[0], []).append(int(p[1:]))
    lines = ['```', *(' '.join(r) for r in rows), '```']
    tab = ['| Символ | Цвет | Клеток | Капсулы этого цвета | Слои (слой: клеток) | Глубже первого слоя* |', '|---|---|---:|---|---|---:|']
    for s, e in sorted(g['legend'].items(), key=lambda kv: -kv[1]['cells']):
        d = e['depth']; d3 = sum(v for k, v in d.items() if int(k) >= 3)
        acc = 0; mn = None
        for k in sorted(d):
            acc += d[k]
            if mn is None and acc >= 0.1 * e['cells']: mn = k
        deeper = sum(v for k, v in d.items() if int(k) > mn)
        lay = ', '.join(f'{k}: {v}' for k, v in sorted(d.items()))
        tab.append(f"| `{s}` | {e['name']} | {e['cells']} | {' + '.join(map(str, pods.get(s, [])))} = {sum(pods.get(s, []))} | {lay} | {round(100*deeper/e['cells'])}% |")
    trayw = max(len(c) for c in t['pods'].values())
    cols = list(t['pods'].values())
    tr = ['```', '        ' + '   '.join(f'кол.{i+1}' for i in range(len(cols)))]
    for r in range(trayw):
        tr.append(f'ряд {r+1:<3} ' + '   '.join(f'{(c[r] if r < len(c) else ""):>5}' for c in cols))
    tr.append('```')
    out[L] = dict(ascii='\n'.join(lines), table='\n'.join(tab), tray='\n'.join(tr), w=g['w'], h=g['h'], tiles=g['tiles'], colors=len(g['legend']), pods=sum(len(c) for c in cols))
json.dump(out, open('/tmp/claude-0/-home-user-bloomlings/17da13b7-11ad-5226-8b0f-bc66792e8f73/scratchpad/grids/snips.json', 'w'), ensure_ascii=False, indent=1)
for L, v in out.items(): print(L, v['w'], v['h'], v['tiles'], v['colors'], v['pods']); print(v['table']); print(v['tray'])

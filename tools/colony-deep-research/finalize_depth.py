import collections
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

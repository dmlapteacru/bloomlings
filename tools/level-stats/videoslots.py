"""Directly seen buffer and progress data from the Colony Flow recordings: for every recorded attempt, measured from the
video frame by frame (4 per second), (a) which waiting slots hold a capsule and (b) the share of the board's cube area
still present. The data is what the video shows, for a statistics report that compares it with a model replay.
usage: python3 -I videoslots.py --yt DIR --old DIR --xrec DIR [--out DIR] [--shots DIR] [L19 L1 ...]  (no ids: all)
  --yt: the colony-flow.com videos (640x360, file names from rounds/site/L<n>.json "video"); --old: the owner's
  recordings of 2026-10-20 (level-*.mp4); --xrec: XRecorder_2026102*.mp4; --out (default: observed/ next to this file);
  --shots: also writes a spot-check picture per attempt at the start, two taps and the end (plates and cube mask drawn).
Rounds: tools/colony-deep-research/rounds/site/L<n>.json, rounds/owner-1020/L<n>.json (times from the start of the
attempt's first file, later files offset by the durations of the earlier ones) and product/audits/colony-flow-rounds/
levels.json (levels 20, 25, 26).
Method. Frames: site videos cropped to the phone strip (x 222-418, 196x360), owner recordings scaled to 368x826, the
XRecorder files used as they are (368x816); 4 frames a second, times in the round file's time base.
Slots: the plates are found where 5 or 6 equal near-white squares stand in a row while all slots are empty (searched on
every frame, so the booster's sixth slot is picked up; a layout from the start of the attempt, else from its end). A
plate is filled when more than 30% of its box (80% x 90% of the plate) differs by more than 14 (max channel) from its
empty colour: a white capsule's digits, grey side and raised face count, an ant covers a few %. A slot filled for one
frame between two empty ones (a capsule flying over it, an ant) is dropped; the grey flash of a finishing capsule
counts as filled until it is gone. A frame where the background beside the plates changes (a full-screen card, the
tutorial's dimming, the win screen) gives null.
Board: the cream panel above the slots. A won attempt gives a clean plate: the per-pixel median of the 3 frames of its
last 8 s with the most panel-coloured pixels; a lost or unfinished attempt borrows the clean plate of a won attempt
with the same plate layout (same level, else the previous level of the same recording layout), else uses its first
frame and the panel colour alone. A pixel inside the panel box is board content when it differs (> 22) from both the
clean plate and the panel colour in the first two measured frames; it is cleared for good once it matches either for
3 frames in a row, so ants and carried cubes crossing a cleared part never count again. share = content pixels left /
content pixels at the start; boxes, jars and padlocks on the board count as content. Pixel share, not cell count.
Validation: rises = slots turning filled whose capsule colour was not in the row a frame earlier (the row closes up
for a linked group), at least the rise of the filled count; a tap is matched when, within -0.75..+1.25 s, as many
capsules arrive as its group has; taps still short then take colour swaps of a full slot (a slot whose steady capsule colour changes by
more than 45 within 0.75 s without an empty frame: one capsule finished and the next landed between two frames).
Output: <out>/L<n>.json {level, source, videos, fps, layout: {attemptK: {frame, crop, plates [cx, cy, w, h], plateBox,
panel [y0, y1, x0, x1], panelColour, boardReference, boardPixelsAtStart}}, attempts: [{attempt, outcome, start, end,
capacity, slots: [[t, filled, "01101"]...] (null when not measurable), board: [[t, share]...] every 0.5 s (null under
a card), validation: {taps, tapsWithRise, tapsMatchedBySwap, tapsWithoutRise [[t, group, seen]], riseWithoutTap,
notes}}]}."""
import sys, os, json, glob, subprocess
import numpy as np
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__)); REPO = os.path.dirname(os.path.dirname(HERE))
RND = os.path.join(REPO, 'tools/colony-deep-research/rounds')
FPS, DT = 4, 0.25
GEOM = {'site': ('crop=196:360:222:0', 196, 360), 'owner': ('scale=368:826', 368, 826), 'xrec': ('null', 368, 816)}
DUR = {'level-1-4.mp4': 138.94, 'level-4~2.mp4': 96.20, 'level-5.mp4': 103.72, 'level-10-fail-part1.mp4': 56.55,
       'level-10-fail-part2.mp4': 65.49, 'level-10-done-part1.mp4': 57.39, 'level-10-done-part2.mp4': 67.86,
       'level-10-done-part3.mp4': 103.55}
args = sys.argv[1:]; opt = {}
while args and args[0].startswith('--'): opt[args[0][2:]] = args[1]; args = args[2:]
OUT = opt.get('out', os.path.join(HERE, 'observed'))

def group_sizes(L, r):
    """capsules taken by every tap of a site / owner-1020 round (a linked group whole), replaying the tray"""
    import re
    cols = {c: [t for t in col] for c, col in L['columns'].items()}
    link = lambda t: (re.match(r'^[A-Za-z#]\d+\??:([a-z])', t) or [None, None])[1]
    groups = {}
    for c, col in cols.items():
        for i, t in enumerate(col):
            if link(t): groups.setdefault(link(t), []).append((c, i))
    heads = {c: 0 for c in cols}; out = {}
    steps = sorted([(t[0], 1, k, t[1]) for k, t in enumerate(r['taps'])] + [(u[0], 0, -1, u[1]) for u in r.get('unlocks', [])])
    for t, kind, k, c in steps:
        if kind == 0: heads[c] += 1; continue
        top = cols[c][heads[c]] if heads[c] < len(cols[c]) else None
        g = groups[link(top)] if top and link(top) else [(c, heads[c])]
        for gc, gi in g: heads[gc] = max(heads[gc], gi + 1)
        out[k] = len(g)
    return [out.get(k, 1) for k in range(len(r['taps']))]

def levels():
    """(id, source, geometry, attempts) for every recorded level; an attempt lists its parts as (file, offset)"""
    res = []
    for f in sorted(glob.glob(os.path.join(RND, 'site/L*.json'))):
        L = json.load(open(f)); atts = []
        for r in L['rounds']:
            atts.append(dict(attempt=r['attempt'], outcome=r['outcome'], start=r['start'], end=r['end'],
                             parts=[(os.path.join(opt.get('yt', '.'), L['video']), 0.0)],
                             taps=[(t[0], g) for t, g in zip(r['taps'], group_sizes(L, r))]))
        res.append((f'L{L["level"]}', 'site', 'site', [L['video']], atts))
    for f in sorted(glob.glob(os.path.join(RND, 'owner-1020/L*.json'))):
        L = json.load(open(f)); atts = []; vids = []
        for r in L['rounds']:
            off, parts = 0.0, []
            for v in r['videos']: parts.append((os.path.join(opt.get('old', '.'), v), off)); off += DUR[v]
            vids += [v for v in r['videos'] if v not in vids]
            atts.append(dict(attempt=r['attempt'], outcome=r['outcome'], start=r['start'], end=r['end'], parts=parts,
                             taps=[(t[0], g) for t, g in zip(r['taps'], group_sizes(L, r)) if 'before the recording' not in (t[2:] or [''])[0]],
                             unseen=sum(1 for t in r['taps'] if 'before the recording' in (t[2:] or [''])[0])))
        res.append((f'L{L["level"]}', 'owner', 'owner', vids, atts))
    for L in json.load(open(os.path.join(REPO, 'product/audits/colony-flow-rounds/levels.json')))['levels']:
        v = L['source']['video']
        if not v.startswith('XRecorder'): continue
        atts = [dict(attempt=r['attempt'], outcome=r['outcome'], start=r['start'], end=r['end'],
                     parts=[(os.path.join(opt.get('xrec', '.'), v), 0.0)], taps=[(t['t'], len(t['take'])) for t in r['taps']])
                for r in L['rounds']]
        res.append((f'L{L["level"]}', 'owner', 'xrec', [v], atts))
    return res

def frames(parts, geom, a, b):
    """yields (t, frame) every 0.25 s for a <= t <= b across the attempt's files"""
    vf, W, H = GEOM[geom]
    for i, (path, off) in enumerate(parts):
        end = parts[i + 1][1] if i + 1 < len(parts) else 1e9
        lo, hi = max(a, off), min(b, end - 0.01)
        if hi < lo: continue
        ss = max(0.0, np.floor((lo - off) * FPS) / FPS)
        p = subprocess.Popen(['ffmpeg', '-v', 'error', '-ss', f'{ss:.2f}', '-i', path, '-t', f'{hi - off - ss + 0.3:.2f}',
                              '-vf', f'fps={FPS},{vf}', '-f', 'rawvideo', '-pix_fmt', 'rgb24', '-'], stdout=subprocess.PIPE)
        k = 0
        while True:
            buf = p.stdout.read(W * H * 3)
            if len(buf) < W * H * 3: break
            t = round(off + ss + k * DT, 2); k += 1
            if lo - 1e-6 <= t <= hi + 1e-6: yield t, np.frombuffer(buf, np.uint8).reshape(H, W, 3).astype(np.int16)
        p.stdout.close(); p.wait()

def find_plates(f):
    """the waiting plates when all are empty: 5 or 6 equal near-white squares in a row -> [(cx, cy, w, h)] or None"""
    H, W = f.shape[:2]
    wh = (f.min(2) > 235) & (f.max(2) - f.min(2) < 14)
    pad = np.pad(wh, ((0, 0), (3, 3)))   # close specks of up to 3 px inside a plate
    wh = wh | (np.any([pad[:, 3 - k:pad.shape[1] - 3 - k] for k in (1, 2, 3)], 0) & np.any([pad[:, 3 + k:pad.shape[1] - 3 + k] for k in (1, 2, 3)], 0))
    y0, y1 = int(H * .3), int(H * .85)
    d = np.diff(np.pad(wh[y0:y1].astype(np.int8), ((0, 0), (1, 1))), axis=1)
    rows = []
    for j in np.nonzero(np.isin((d == 1).sum(1), (5, 6)))[0]:
        s, e = np.nonzero(d[j] == 1)[0], np.nonzero(d[j] == -1)[0]
        ln = e - s; gaps = s[1:] - e[:-1]
        if ln.min() >= W * .07 and ln.max() - ln.min() <= max(3, .2 * ln.mean()) and gaps.min() > 0 and gaps.max() - gaps.min() <= max(4, .5 * gaps.mean()):
            rows.append((y0 + j, (s + e) / 2, ln.mean()))
    best, run = None, []
    for r in rows + [None]:
        if r and run and r[0] == run[-1][0] + 1 and len(r[1]) == len(run[-1][1]) and np.abs(r[1] - run[-1][1]).max() <= 2: run.append(r); continue
        if run and len(run) >= .6 * run[0][2] and (best is None or len(run) > len(best)): best = run
        run = [r] if r else []
    if not best: return None
    m = best[len(best) // 2]
    return [(float(x), (best[0][0] + best[-1][0]) / 2, float(m[2]), float(len(best))) for x in m[1]]

def plate_box(p):
    cx, cy, w, h = p
    return int(cy - .45 * h), int(cy + .45 * h) + 1, int(cx - .4 * w), int(cx + .4 * w) + 1

def panel_colour(f, ytop):
    """the cream panel colour: the most common light, warm-grey colour above the slots"""
    a = f[:ytop].reshape(-1, 3)
    m = (a.min(1) > 180) & (a[:, 0] - a[:, 2] < 60) & (a[:, 0] - a[:, 2] > 10) & (a.max(1) - a.min(1) > 8)
    if m.sum() < 50: return np.array([-999, -999, -999])   # no panel in sight (a dark card)
    q = (a[m] // 4).astype(np.int32); key = q[:, 0] * 4096 + q[:, 1] * 64 + q[:, 2]
    k = np.bincount(key).argmax()
    return np.array([k // 4096, k // 64 % 64, k % 64]) * 4 + 2

def near(f, c, tol): return np.abs(f - c).max(-1) <= tol

def panel_box(f, P, ytop, tol=10):
    m = near(f[:ytop], P, tol); H, W = m.shape
    rows = np.nonzero(m.sum(1) > W * .15)[0]
    if not len(rows): return None
    cols = np.nonzero(m[rows[0]:rows[-1] + 1].sum(0) > (rows[-1] - rows[0]) * .15)[0]
    return [int(rows[0]), int(rows[-1]) + 1, int(cols[0]), int(cols[-1]) + 1]

TOL, LATCH = 22, 3
LAST, LASTCLEAN = {}, {}

def start_layout(att, geom):
    """the plates from the first 4 s of an attempt (all slots empty), or None"""
    for t, f in frames(att['parts'], geom, att['start'], att['start'] + 4):
        d = find_plates(f)
        if d: return layout_of(f, d)

def clean_plate(att, geom, lay):
    """for a won attempt: (panel colour, panel box, clean plate) from the emptiest frames of its last 8 s"""
    fr = list(frames(att['parts'], geom, max(att['start'], att['end'] - 8), att['end']))
    for t, f in fr:
        d = find_plates(f)
        if d and not lay: lay = layout_of(f, d)
    if not lay: return None
    pl = lay['plates']
    ytop = int(min(p[1] - p[3] for p in pl)) - 4
    score = [near(f[:ytop], panel_colour(f, ytop), 10).sum() for t, f in fr]
    best = np.argsort(score)[-3:]
    cp = np.median(np.stack([fr[i][1] for i in best]), 0).astype(np.int16)
    P = panel_colour(cp, ytop); box = panel_box(cp, P, ytop)
    return box and dict(P=P, box=box, cp=cp, t=[fr[i][0] for i in sorted(best)], layout=lay)

def layout_of(f, plates):
    """plates, their empty colours and the background beside them (to notice a full-screen card)"""
    side = side_band(f, plates)
    return dict(plates=plates, empty=[np.median(f[slice(*plate_box(p)[:2]), slice(*plate_box(p)[2:])].reshape(-1, 3), 0) for p in plates],
                side=side, side_ref=f[side].copy())

def measure(att, geom, ref, lay=None, shots=None, tag=''):
    """slots and board of one attempt; lay: a layout known beforehand (from the end of the attempt or another one)"""
    t0, t1 = att['start'], att['end']
    plates, empty, pend = None, None, None; ncover = 0
    if lay: plates, empty, side, side_ref = lay['plates'], lay['empty'], lay['side'], lay['side_ref']
    used = [[t0, len(plates)]] if lay else []   # [time, plates] whenever the layout changes
    slots, board, raw, times, cols = [], [], [], [], []
    init = None; shot_t = set()
    if shots:
        tt = [t for t, g in att['taps']]
        shot_t = {round(t0 + .5, 2), round(t1, 2)} | {round(np.floor(tt[i] * 4) / 4 + 1.0, 2) for i in (len(tt) // 3, 2 * len(tt) // 3) if tt}
    for t, f in frames(att['parts'], geom, t0, t1):
        det = find_plates(f)
        if det and (plates is None or len(det) != len(plates) or max(abs(a[0] - b[0]) + abs(a[1] - b[1]) for a, b in zip(det, plates)) > 4):
            if pend and len(pend) == len(det) and max(abs(a[0] - b[0]) for a, b in zip(det, pend)) <= 2 or plates is None:
                plates, pend = det, None; L = layout_of(f, det)
                if not used or used[-1][1] != len(det) or used[-1][0] != t0: used.append([t, len(det)])
                empty, side, side_ref = L['empty'], L['side'], L['side_ref']
            else: pend = det
        else: pend = None
        times.append(t)
        cover = plates is not None and np.abs(f[side] - side_ref).mean() > 25   # a full-screen card or the win screen
        ncover += cover
        if plates and not cover:
            fill, col = [], []
            for p, e in zip(plates, empty):
                y0, y1, x0, x1 = plate_box(p)
                fr = (np.abs(f[y0:y1, x0:x1] - e).max(-1) > 14).mean(); fill.append(int(fr > .3))
                cx, cy, w, h = p   # the capsule face colour, from the middle of the plate
                col.append(np.median(f[int(cy - .3 * h):int(cy + .3 * h) + 1, int(cx - .3 * w):int(cx + .3 * w) + 1].reshape(-1, 3), 0))
            raw.append(fill); cols.append(col)
        else: raw.append(None); cols.append(None)
        if ref is None: ref = make_ref(f, plates)
        if ref and not cover:
            y0, y1, x0, x1 = ref['box']; g = f[y0:y1, x0:x1]
            bg = near(g, ref['P'], TOL) | near(g, ref['plate'], TOL)
            if init is None: init = ~bg & ref['count']; first = True
            elif first: init &= ~bg; left = init.copy(); runs = np.zeros(init.shape, np.int8); first = False
            else: runs = np.where(bg, runs + 1, 0); left &= runs < LATCH
        if ref and init is not None and not first and len(times) % 2 == 1:   # every 0.5 s
            board.append([t, None if cover else round(float(left.sum() / max(1, init.sum())), 4)])
        if round(t, 2) in shot_t: snapshot(f, plates, ref, None if init is None or first else left, raw[-1], f'{shots}/{tag}_{t:.2f}.png')
    if board and board[0][0] > t0 + 1e-6: board.insert(0, [round(t0, 2), 1.0])
    # a slot filled for a single frame between empty ones is a capsule flying over it or an ant: dropped
    for i, t in enumerate(times):
        if raw[i] is None: slots.append([t, None, None]); continue
        s = list(raw[i])
        if 0 < i < len(raw) - 1 and raw[i - 1] and raw[i + 1] and len(raw[i - 1]) == len(s) == len(raw[i + 1]):
            s = [0 if v and not raw[i - 1][k] and not raw[i + 1][k] else v for k, v in enumerate(s)]
        s = ''.join(map(str, s)); slots.append([t, s.count('1'), s])
    return slots, board, plates, ref, (None if init is None else int(init.sum())), ncover, layout_of_last(plates, empty, side, side_ref), (swaps(times, raw, cols), cols), used

def swaps(times, raw, cols):
    """times a slot stays filled but its capsule colour changes from one steady colour to another (> 45) within
    0.75 s: a capsule finished and the next one landed in the same slot between two frames (validation only)"""
    out = []
    n = max((len(r) for r in raw if r), default=0)
    for k in range(n):
        runs, cur = [], None   # steady runs: [first frame, last frame, colour]
        for i, (r, c) in enumerate(zip(raw, cols)):
            if not r or len(r) != n or not r[k]: cur = None; runs.append(None); continue
            if cur and np.abs(c[k] - cur[2]).max() <= 20: cur[1] = i; continue
            cur = [i, i, c[k]]; runs.append(cur)
        runs = [r for r in runs if r is None or r[1] > r[0]]   # steady for 2 frames or more
        for a, b in zip(runs, runs[1:]):
            if a and b and b[0] - a[1] <= 3 and np.abs(a[2] - b[2]).max() > 45: out.append(times[b[0]])
    return sorted(out)

def layout_of_last(plates, empty, side, side_ref):
    return plates and dict(plates=plates, empty=empty, side=side, side_ref=side_ref)

def side_band(f, plates):
    """the background left of the first plate and right of the last (an overlay changes it)"""
    cx, cy, w, h = plates[0]; m = np.zeros(f.shape[:2], bool)
    m[int(cy - h / 2):int(cy + h / 2), 1:max(2, int(cx - w / 2) - 2)] = True
    cx = plates[-1][0]; m[int(cy - h / 2):int(cy + h / 2), min(f.shape[1] - 2, int(cx + w / 2) + 2):f.shape[1] - 1] = True
    return m

def make_ref(f, plates):
    """board reference from the start frame alone (no won attempt to take a clean plate from)"""
    if not plates: return None
    ytop = int(min(p[1] - p[3] for p in plates)) - 4
    P = panel_colour(f, ytop); box = panel_box(f, P, ytop, 14)
    if not box: return None
    y0, y1, x0, x1 = box; m = 3
    box = [y0 + m, y1 - m, x0 + m, x1 - m]
    return dict(P=P, box=box, plate=np.broadcast_to(P, (box[1] - box[0], box[3] - box[2], 3)),
                count=np.ones((box[1] - box[0], box[3] - box[2]), bool), source='start frame')

def ref_from_clean(c):
    y0, y1, x0, x1 = c['box']; plate = c['cp'][y0:y1, x0:x1]
    return dict(P=c['P'], box=c['box'], plate=plate, count=near(plate, c['P'], TOL), source=f'clean plate {[float(x) for x in c["t"]]}')

def snapshot(f, plates, ref, left, fill, path):
    im = Image.fromarray(f.clip(0, 255).astype(np.uint8)).convert('RGB'); d = ImageDraw.Draw(im)
    if ref and left is not None:
        y0, y1, x0, x1 = ref['box']; a = np.array(im)
        sub = a[y0:y1, x0:x1]; sub[left] = (sub[left] * .4 + np.array([0, 255, 255]) * .6).astype(np.uint8); im = Image.fromarray(a); d = ImageDraw.Draw(im)
        d.rectangle([x0, y0, x1, y1], outline=(0, 0, 255))
    for i, p in enumerate(plates or []):
        y0, y1, x0, x1 = plate_box(p)
        d.rectangle([x0, y0, x1, y1], outline=(0, 200, 0) if fill and fill[i] else (255, 0, 0))
    im = im.resize((im.width * 2, im.height * 2)); im.save(path)

def validate(slots, taps, note, sw_cols):
    sw, cols = sw_cols
    rises = []   # capsules added: slots turning filled that hold a new colour, at least the count's rise
    for i, ((ta, na, sa), (tb, nb, sb)) in enumerate(zip(slots, slots[1:])):
        if sa is None or sb is None or len(sa) != len(sb): continue
        n01 = sum(1 for x, y in zip(sa, sb) if x == '0' and y == '1')
        # capsules whose colour was not in the row a frame earlier (the row closes up for a linked group, so a
        # capsule moving to another slot is no arrival)
        before = [cols[i][k] for k in range(len(sa)) if sa[k] == '1']; new = 0
        for k in range(len(sb)):
            if sb[k] != '1': continue
            d = [np.abs(cols[i + 1][k] - c).max() for c in before]
            if d and min(d) <= 30: before.pop(int(np.argmin(d)))
            else: new += 1
        rises += [tb] * max(nb - na, min(n01, new), 0)
    # first the rises of the filled count, then the colour swaps of a full slot for the taps still short
    used = [False] * len(rises); usw = [False] * len(sw); got = [0] * len(taps)
    for pool, flags in ((rises, used), (sw, usw)):
        for j, (t, g) in enumerate(taps):
            for i, r in enumerate(pool):
                if not flags[i] and t - .75 <= r <= t + 1.25 and got[j] < g: flags[i] = True; got[j] += 1
    return dict(taps=len(taps), tapsWithRise=sum(1 for j, (t, g) in enumerate(taps) if got[j] >= g), tapsMatchedBySwap=sum(1 for f in usw if f),
            tapsWithoutRise=[[t, g, got[j]] for j, (t, g) in enumerate(taps) if got[j] < g],
            riseWithoutTap=[r for r, u in zip(rises, used) if not u], notes=note)

def main():
    os.makedirs(OUT, exist_ok=True)
    if 'shots' in opt: os.makedirs(opt['shots'], exist_ok=True)
    for lid, source, geom, vids, atts in levels():
        if args and lid not in args: continue
        lays = {a['attempt']: start_layout(a, geom) for a in atts}
        cleans = {a['attempt']: clean_plate(a, geom, lays[a['attempt']]) for a in atts if a['outcome'] == 'won'}
        cleans = {k: c for k, c in cleans.items() if c}
        same = lambda p, q: p and q and len(p) == len(q) and max(abs(u[0] - v[0]) + abs(u[1] - v[1]) for u, v in zip(p, q)) <= 4
        res = dict(level=int(lid[1:]), source=source, videos=vids, fps=FPS, layout={}, attempts=[])
        for a in atts:
            # a lost or unfinished attempt borrows the clean plate of a won attempt of the same level when its
            # plates stand at the same place, else the board is taken from its first frame alone
            k0 = a['attempt']; c = cleans.get(k0); lay = lays[k0]; note = []
            if not c:
                k = next((k for k, cc in cleans.items() if same(cc['layout']['plates'], lay and lay['plates'])), None)
                if k: c = cleans[k]; note.append(f'board reference from the clean plate of attempt {k} (same screen layout)')
                else: note.append('board reference from the first frame (no clean plate of the same layout)')
            lay = lay or (c and c['layout'])
            if not lay and LAST.get(geom): lay = LAST[geom]; note.append('slot layout taken from the previous level of the same recording layout (slots not empty at the start)')
            if not c and LASTCLEAN.get(geom) and same(LASTCLEAN[geom]['layout']['plates'], lay and lay['plates']):
                c = LASTCLEAN[geom]; note[0] = f'board reference from the clean plate of {LASTCLEAN[geom]["lid"]} (same recording layout: same plates and panel)'
            for cc in cleans.values(): LASTCLEAN[geom] = dict(cc, lid=lid)
            slots, board, plates, ref, npx, ncover, last, sw, used = measure(a, geom, ref_from_clean(c) if c else None, lay, opt.get('shots'), f'{lid}_a{a["attempt"]}')
            if last and len(last['plates']) == 5: LAST[geom] = last
            if ncover: note.append(f'{ncover} frames covered by a card or the win screen (slots null, board held)')
            caps = sorted({len(s[2]) for s in slots if s[2]})
            if a.get('unseen'): note.append(f'{a["unseen"]} taps made before the recording started are left out of the tap check')
            v = validate(slots, a['taps'], '; '.join(note), sw)
            res['layout'][f'attempt{a["attempt"]}'] = dict(
                frame=GEOM[geom][1:], crop=GEOM[geom][0], plates=[[round(p[0], 1), round(p[1], 1), round(p[2], 1), round(p[3], 1)] for p in plates or []],
                plateBox='centre, 80% of the plate width x 90% of its height', panel=ref and ref['box'], panelColour=ref and [int(x) for x in ref['P']],
                boardReference=ref and ref['source'], boardPixelsAtStart=npx, layoutChanges=used)
            res['attempts'].append(dict(attempt=a['attempt'], outcome=a['outcome'], start=a['start'], end=a['end'],
                                        capacity=caps[-1] if caps else None, slots=slots, board=board, validation=v))
            bb = [b for t, b in board if b is not None]; b0, b1 = (bb[0], bb[-1]) if bb else (None, None)
            print(f'{lid} a{a["attempt"]} {a["outcome"]}: {len(slots)} frames, caps {caps}, taps {v["tapsWithRise"]}/{v["taps"]}, '
                  f'extra rises {len(v["riseWithoutTap"])}, board {b0} -> {b1} ({ref and ref["source"]})', flush=True)
        with open(os.path.join(OUT, f'{lid}.json'), 'w') as fo: json.dump(res, fo, separators=(',', ':'))

if __name__ == '__main__': main()

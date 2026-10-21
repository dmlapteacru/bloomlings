"""Render slot timelines as text bars: '█' = the pod cleared tiles in this window, '░' = it sat in its slot without progress."""
import sys, re
def parse_slots(path):
    rows = []
    for line in open(path):
        t, s = line.strip().split('|'); rows.append((int(t), s.split()))
    pods = []  # dict(label, slot, series{t:count})
    cur = {}
    for t, slots in rows:
        for i, tok in enumerate(slots):
            if tok == '—':
                cur.pop(i, None); continue
            m = re.match(r'([A-Za-z]+)(\d+)', tok); c, n = m.group(1), int(m.group(2))
            p = cur.get(i)
            if p is None or p['c'] != c or n > p['last']:
                p = dict(c=c, slot=i + 1, series={}, last=n); pods.append(p); cur[i] = p
            p['series'][t] = n; p['last'] = n
    return pods
def render(pods, t_end, step=2, labels=None):
    out = ['%-15s|%s' % ('t, с', ''.join(('|' if t % 10 == 0 else ' ') for t in range(0, t_end + 1, step)))]
    for k, p in enumerate(pods):
        ser = p['series']; ts = sorted(ser)
        full = {}
        for t in range(ts[0], ts[-1] + 1):
            full[t] = ser[t] if t in ser else full[t - 1]
        def stalled(t):
            a, b = full.get(t - 2), full.get(t + 2)
            if a is None: a = full[t]
            if b is None: b = full[t]
            return a == full[t] == b and full[t] > 0
        line = ''
        for w in range(0, t_end + 1, step):
            sec = [t for t in range(w, w + step) if t in full]
            line += ' ' if not sec else ('░' if all(stalled(t) for t in sec) else '█')
        label = labels[k] if labels else '%s%d сл.%d' % (p['c'], max(ser.values()), p['slot'])
        out.append('%-15s|%s' % (label, line))
    return '\n'.join(out)
if __name__ == '__main__':
    pods = parse_slots(sys.argv[1]); print(render(pods, int(sys.argv[2])))

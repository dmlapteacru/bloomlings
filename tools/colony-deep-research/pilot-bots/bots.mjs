// Bot battery for a gameplay pilot (v7/v8 live timeline, patient player: every tap fully settles before the next).
// usage: node bots.mjs <pilot dir: tools/gameplay-pilot-vN> <levels.json> <out.json> [ids]
import fs from 'fs';
import { createRequire } from 'module';
const [toolDir, levelsPath, outPath, idsArg] = process.argv.slice(2);
const require = createRequire(import.meta.url);
const R = require(toolDir + '/idle-rules.cjs');
const levels = JSON.parse(fs.readFileSync(levelsPath, 'utf8')).levels.filter(l => !idsArg || idsArg.split(',').includes(String(l.id)));
const rng = seed => { let s = seed >>> 0 || 1; return () => { s ^= s << 13; s ^= s >>> 17; s ^= s << 5; return (s >>> 0) / 4294967296; }; };
const label = (L, p) => { const q = L.pods[p]; return (q.mystery === 'sealed' ? '?' : '') + q.color + q.count + (q.group ? '~' : ''); };
const known = (s, a) => s.meta.groups[a.pod].every(p => R.view(s).colours[p] !== null);
const visible = s => new Set(R.reach(s).targets.map(i => s.board[i]));
const matches = (s, a) => { const v = visible(s), col = R.view(s).colours; return s.meta.groups[a.pod].some(p => col[p] !== null && v.has(R.CHANNELS.indexOf(col[p]))); };
const P = {
  random: (s, m, r) => m[Math.floor(r() * m.length)],
  visible: (s, m, r) => { const f = m.filter(a => a.type === 'bridge' || (a.type === 'tap' && matches(s, a))); const c = f.length ? f : m; return c[Math.floor(r() * c.length)]; },
  leftmost: (s, m) => m[0],
  rightmost: (s, m) => { const t = m.filter(a => a.type === 'tap'); return t.length ? t[t.length - 1] : m[0]; },
  leftVisible: (s, m) => m.find(a => a.type === 'bridge') || m.find(a => a.type === 'tap' && matches(s, a)) || m[0],
  biggestVisible: (s, m) => { const b = m.find(a => a.type === 'bridge'); if (b) return b; const f = m.filter(a => a.type === 'tap' && matches(s, a)); const c = f.length ? f : m; return c.reduce((x, a) => (a.type === 'tap' && s.remaining[a.pod] > s.remaining[x.pod] ? a : x)); },
  smallestVisible: (s, m) => { const b = m.find(a => a.type === 'bridge'); if (b) return b; const f = m.filter(a => a.type === 'tap' && matches(s, a)); const c = f.length ? f : m; return c.reduce((x, a) => (a.type === 'tap' && s.remaining[a.pod] < s.remaining[x.pod] ? a : x)); },
  mostWorkNow: (s, m) => { let best = m[0], bw = -1; for (const a of m) { if (a.type === 'tap' && !known(s, a)) continue; const n = R.step(s, a).state; const w = s.remaining.reduce((x, y) => x + y, 0) - n.remaining.reduce((x, y) => x + y, 0) + (n.status === 'Won' ? 1e6 : 0); if (w > bw) { bw = w; best = a; } } return best; },
};
const RANDOM = new Set(['random', 'visible']);
function play(L, pol, seed) { const r = rng(seed); let s = R.create(L); const taps = []; let guard = 0; while (s.status === 'Playing' && guard++ < 400) { const m = R.legal(s); const a = pol(s, m, r); taps.push(a.type === 'tap' ? label(L, a.pod) : 'bridge' + a.choice); s = R.step(s, a).state; } return { won: s.status === 'Won', taps }; }
function exactRandom(L, budget = 600000) { const memo = new Map(); let n = 0; const v = s => { if (s.status === 'Won') return 1; if (s.status !== 'Playing') return 0; const k = R.signature(s); if (memo.has(k)) return memo.get(k); if (++n > budget) throw 0; const m = R.legal(s); let p = 0; for (const a of m) p += v(R.step(s, a).state) / m.length; memo.set(k, p); return p; }; try { return { p: v(R.create(L)), states: memo.size }; } catch { return { p: null, states: memo.size }; } }
const winMemo = new Map();
function winnable(s, budget = 150000) { // full-information solver over the same patient steps
  let n = 0; const memo = new Map();
  const v = s => { if (s.status === 'Won') return true; if (s.status !== 'Playing') return false; const k = R.signature(s); if (winMemo.has(k)) return winMemo.get(k); if (memo.has(k)) return memo.get(k); if (++n > budget) throw 0; let inc = false; for (const a of R.legal(s)) { const r = v(R.step(s, a).state); if (r === true) { memo.set(k, true); winMemo.set(k, true); return true; } } memo.set(k, false); winMemo.set(k, false); return false; };
  try { return v(s); } catch { return null; }
}
function fatal(L, mode, n = 200) {
  const f = {}, at = {}; let lost = 0, unknown = 0;
  for (let i = 0; i < n; i++) { const r = rng(101 + i * 7919); let s = R.create(L), k = 0;
    while (s.status === 'Playing') { const m = R.legal(s); const a = P[mode](s, m, r); const next = R.step(s, a).state; k++;
      const w0 = winnable(s), w1 = winnable(next); if (w0 === null || w1 === null) { unknown++; break; }
      if (w0 && !w1) { const lb = a.type === 'tap' ? label(L, a.pod) : 'bridge'; f[lb] = (f[lb] || 0) + 1; at[k] = (at[k] || 0) + 1; lost++; break; } s = next; } }
  return { lost, n, unknown, top: Object.entries(f).sort((a, b) => b[1] - a[1]).slice(0, 4), at };
}
// colour depth from the entries (our access rule): 0-1 BFS, entering a different colour costs 1
function depth(L) {
  const W = L.w, H = L.h, g = L.rowsBottomFirst.join(''); const INF = 1e9; const d = new Array(W * H).fill(INF); const dq = [];
  for (const e of (L.entries || [Math.floor(W / 2)])) { if (g[e] !== '#') { d[e] = 0; dq.push(e); } }
  while (dq.length) { const i = dq.shift(); const x = i % W, y = (i / W) | 0;
    for (const [dx, dy] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) { const nx = x + dx, ny = y + dy; if (nx < 0 || ny < 0 || nx >= W || ny >= H) continue; const j = ny * W + nx; if (g[j] === '#') continue;
      const c = (g[j] === '.' || g[i] === '.' || g[j] === g[i]) ? 0 : 1; if (d[i] + c < d[j]) { d[j] = d[i] + c; c ? dq.push(j) : dq.unshift(j); } } }
  const per = {};
  for (let i = 0; i < g.length; i++) { if ('.#'.includes(g[i])) continue; (per[g[i]] ||= []).push(d[i]); }
  const out = {}; for (const [c, a] of Object.entries(per)) { a.sort((x, y) => x - y); const first = a[Math.floor(a.length * 0.1)]; out[c] = { cells: a.length, deep: +(a.filter(x => x > first).length / a.length).toFixed(2), layers: [...new Set(a)].length }; }
  return out;
}
const res = [];
for (const L of levels) {
  const t0 = Date.now(); const runs = {};
  for (const [k, pol] of Object.entries(P)) { const n = RANDOM.has(k) ? 400 : 1; let w = 0, ex = null; for (let i = 0; i < n; i++) { const r = play(L, pol, 17 + i * 7919); if (r.won) w++; else if (!ex) ex = r.taps.join(' '); } runs[k] = { rate: +(w / n).toFixed(3), firstLoss: ex }; }
  const ex = exactRandom(L);
  const fr = fatal(L, 'random', 150), fv = fatal(L, 'visible', 150);
  const dep = depth(L); const deepColours = Object.values(dep).filter(v => v.deep >= 0.25).length;
  const sizes = L.pods.map(p => p.count).sort((a, b) => a - b);
  const row = { id: L.id, title: L.title, cls: L.difficulty, size: `${L.w}x${L.h}`, colours: Object.keys(dep).length, deepColours, pods: L.pods.length, median: sizes[sizes.length >> 1], small: sizes.filter(s => s <= 12).length, pairs: new Set(L.pods.filter(p => p.group).map(p => p.group)).size, sealed: L.pods.filter(p => p.mystery === 'sealed').length, hero: !!L.hero, runs, exact: ex, fatalRandom: fr, fatalVisible: fv, depth: dep, secs: (Date.now() - t0) / 1000 };
  res.push(row);
  console.log(JSON.stringify({ id: row.id, cls: row.cls, colours: row.colours, deep: deepColours, rand: runs.random.rate, exact: ex.p === null ? null : +ex.p.toFixed(3), vis: runs.visible.rate, L: runs.leftmost.rate, R: runs.rightmost.rate, LV: runs.leftVisible.rate, BV: runs.biggestVisible.rate, SV: runs.smallestVisible.rate, MW: runs.mostWorkNow.rate, fatalR: fr.top, fatalAt: Object.keys(fr.at).slice(0, 6), secs: row.secs }));
  fs.writeFileSync(outPath, JSON.stringify(res, null, 1));
}

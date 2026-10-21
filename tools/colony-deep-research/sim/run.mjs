// Replays Colony Flow levels transcribed from the owner's videos in the pilot-v4 rules (perimeter access, exact accounting).
import fs from 'fs';
import R from './rules.cjs';
import { solve } from './search.mjs';
const G = JSON.parse(fs.readFileSync(process.argv[2], 'utf8'));
const T = JSON.parse(fs.readFileSync(process.argv[3], 'utf8'));
const only = process.argv[4] ? process.argv[4].split(',') : Object.keys(T);
const rng = seed => { let s = seed >>> 0 || 1; return () => { s ^= s << 13; s ^= s >>> 17; s ^= s << 5; return (s >>> 0) / 4294967296; }; };
function build(name) {
  const g = G[name], t = T[name];
  let rows = g.rows.map(r => r.split(''));
  // exact accounting fixes for single-cell recognition errors
  const need = {};
  for (const col of Object.values(t.pods)) for (const p of col) { const c = p[0], n = +p.slice(1); need[c] = (need[c] || 0) + n; }
  const have = {}; rows.flat().forEach(c => { if (c !== '.') have[c] = (have[c] || 0) + 1; });
  const over = Object.keys(have).filter(c => have[c] > (need[c] || 0)), under = Object.keys(need).filter(c => need[c] > (have[c] || 0));
  const fixes = [];
  for (const u of under) for (const o of over) {
    while (have[o] > need[o] && need[u] > have[u]) {
      let done = false;
      for (let y = 0; y < rows.length && !done; y++) for (let x = 0; x < rows[0].length && !done; x++) {
        if (rows[y][x] !== o) continue;
        if ([[1,0],[-1,0],[0,1],[0,-1]].some(([dx,dy]) => rows[y+dy]?.[x+dx] === u)) { rows[y][x] = u; have[o]--; have[u]++; fixes.push(`${o}->${u}@${x},${y}`); done = true; }
      }
      if (!done) break;
    }
  }
  const syms = [...new Set(Object.keys(need))];
  const map = Object.fromEntries(syms.map((s, i) => [s, R.CHANNELS[i]]));
  const W = rows[0].length + 2, H = rows.length + 2;
  const padded = [Array(W).fill('.').join('')].concat(rows.map(r => '.' + r.map(c => c === '.' ? '.' : map[c]).join('') + '.'), [Array(W).fill('.').join('')]);
  const pods = [], tray = [];
  for (const col of Object.values(t.pods)) { tray.push(col.map(p => { pods.push({ color: map[p[0]], count: +p.slice(1), label: p }); return pods.length - 1; })); }
  const level = { id: 1, w: W, h: H, rowsBottomFirst: padded.slice().reverse(), entries: [Math.floor(W / 2)], capacity: t.capacity, pods, tray };
  return { level, fixes, map, have, need };
}
const legal = s => R.legal(s).filter(a => a.type === 'tap');
const visibleColours = s => new Set(R.reach(s).targets.map(i => s.board[i]));
const policies = {
  random: (s, r) => { const m = legal(s); return m[Math.floor(r() * m.length)]; },
  visible: (s, r) => { let m = legal(s); const v = visibleColours(s); const f = m.filter(a => v.has(s.meta.colours[a.pod])); if (f.length) m = f; return m[Math.floor(r() * m.length)]; },
  leftmost: s => legal(s)[0],
  rightmost: s => { const m = legal(s); return m[m.length - 1]; },
  leftVisible: s => { const m = legal(s); const v = visibleColours(s); return m.find(a => v.has(s.meta.colours[a.pod])) || m[0]; },
  biggestVisible: s => { const m = legal(s); const v = visibleColours(s); const f = m.filter(a => v.has(s.meta.colours[a.pod])); const c = f.length ? f : m; return c.reduce((b, a) => s.remaining[a.pod] > s.remaining[b.pod] ? a : b); },
  mostWorkNow: s => { const m = legal(s); let best = m[0], bw = -1; for (const a of m) { const n = R.step(s, a).state; const w = s.remaining.reduce((x, y) => x + y, 0) - n.remaining.reduce((x, y) => x + y, 0); if (w > bw) { bw = w; best = a; } } return best; },
};
function play(level, pol, seed) { const r = rng(seed); let s = R.create(level); const taps = []; while (s.status === 'Playing') { const a = pol(s, r); taps.push(level.pods[a.pod].label); s = R.step(s, a).state; } return { won: s.status === 'Won', status: s.status, taps }; }
function exactRandom(level, budget = 3e6) {
  const memo = new Map(); let nodes = 0;
  const visit = s => { if (s.status === 'Won') return 1; if (s.status !== 'Playing') return 0; const k = R.signature(s); if (memo.has(k)) return memo.get(k); if (++nodes > budget) throw new Error('budget'); const m = legal(s); let p = 0; for (const a of m) p += visit(R.step(s, a).state) / m.length; memo.set(k, p); return p; };
  try { return { p: visit(R.create(level)), states: memo.size }; } catch (e) { return { p: null, states: memo.size }; }
}
function decisions(level, budget = 400000) {
  // along one solver witness: at every step, how many legal taps keep the level winnable
  const res = solve(R.create(level), { budget }); if (!res.winning) return { solvable: res.winning, steps: [] };
  let s = R.create(level); const steps = [];
  for (const a of res.actions) {
    const m = legal(s); let ok = 0; const losing = [];
    for (const b of m) { const w = solve(R.step(s, b).state, { budget: budget / 4 }).winning; if (w) ok++; else losing.push(level.pods[b.pod].label + (w === null ? '?' : '')); }
    steps.push({ tap: level.pods[a.pod].label, legal: m.length, keepWin: ok, losing });
    s = R.step(s, a).state;
  }
  return { solvable: true, steps };
}
const out = {};
for (const name of only) {
  const { level, fixes } = build(name);
  const errs = R.validate ? R.validate(level).filter(e => !['dimensions/id', 'grid'].includes(e)) : [];
  const runs = {};
  for (const [k, pol] of Object.entries(policies)) {
    const n = ['random', 'visible'].includes(k) ? 2000 : 1; let w = 0; let ex = null;
    for (let i = 0; i < n; i++) { const r = play(level, pol, 17 + i * 7919); if (r.won) w++; else if (!ex) ex = r.taps.join(' '); }
    runs[k] = { wins: w, n, rate: +(w / n).toFixed(3), firstLoss: ex };
  }
  const ex = exactRandom(level);
  const dec = decisions(level);
  out[name] = { size: `${level.w - 2}x${level.h - 2}`, pods: level.pods.length, fixes, errs, runs, exactRandom: ex, decisions: dec };
  console.log(name, JSON.stringify({ fixes, errs, rates: Object.fromEntries(Object.entries(runs).map(([k, v]) => [k, v.rate])), exactRandom: ex.p, states: ex.states, solvable: dec.solvable, forced: dec.steps.filter(x => x.keepWin < x.legal).length + '/' + dec.steps.length }));
}
fs.writeFileSync(process.argv[5] || 'out.json', JSON.stringify(out, null, 1));

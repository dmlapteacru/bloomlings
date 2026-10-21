// Same pilot picture, CF-style trays: every colour split into 20-50 pods (sometimes a 4-10 one), shuffled into 3 columns.
import fs from 'fs';
import R from './rules.cjs';
import { solve } from './search.mjs';
const lv = JSON.parse(fs.readFileSync('/home/user/bloomlings/content/pilot-v4/levels.json', 'utf8')).levels;
const id = +process.argv[2], tries = +process.argv[3] || 150;
const base = lv.find(l => l.id === id);
const rng = seed => { let s = seed >>> 0 || 1; return () => { s ^= s << 13; s ^= s >>> 17; s ^= s << 5; return (s >>> 0) / 4294967296; }; };
const legal = s => R.legal(s).filter(a => a.type === 'tap');
const vis = s => new Set(R.reach(s).targets.map(i => s.board[i]));
const cnt = {}; for (const c of base.rowsBottomFirst.join('')) if (!'.#'.includes(c)) cnt[c] = (cnt[c] || 0) + 1;
function make(seed) {
  const r = rng(seed); const pods = [];
  for (const [c, n] of Object.entries(cnt)) {
    let left = n;
    while (left > 0) {
      let k = r() < 0.15 ? 4 + Math.floor(r() * 7) : 20 + Math.floor(r() * 31);
      if (left - k < 4) k = left; pods.push({ color: c, count: Math.min(k, left), label: c + Math.min(k, left) }); left -= Math.min(k, left);
    }
  }
  const order = pods.map((_, i) => i); for (let i = order.length - 1; i > 0; i--) { const j = Math.floor(r() * (i + 1)); [order[i], order[j]] = [order[j], order[i]]; }
  const tray = [[], [], []]; order.forEach((p, i) => tray[i % 3].push(p));
  return { ...base, pods, tray, hero: undefined, capacity: 5 };
}
function rate(level, mode, n) {
  let w = 0;
  for (let i = 0; i < n; i++) { const r = rng(7 + i * 7919); let s = R.create(level);
    while (s.status === 'Playing') { let m = legal(s); if (mode === 'left') m = [m[0]]; if (mode === 'right') m = [m[m.length - 1]]; if (mode === 'visible') { const v = vis(s); const f = m.filter(a => v.has(s.meta.colours[a.pod])); if (f.length) m = f; } s = R.step(s, m[Math.floor(r() * m.length)]).state; }
    if (s.status === 'Won') w++; }
  return w / n;
}
const res = [];
for (let seed = 1; seed <= tries; seed++) {
  const L = make(seed); if (R.validate(L).filter(e => e !== 'dimensions/id').length) continue;
  const sol = solve(R.create(L), { budget: 200000 }); if (sol.winning !== true) continue;
  const m = { seed, pods: L.pods.length, random: rate(L, 'random', 300), visible: rate(L, 'visible', 300), left: rate(L, 'left', 1), right: rate(L, 'right', 1) };
  res.push(m);
}
const hard = res.filter(m => m.random >= 0.12 && m.random <= 0.35 && m.visible <= 0.45 && !m.left && !m.right).sort((a, b) => a.visible - b.visible);
const normal = res.filter(m => m.random >= 0.9).sort((a, b) => b.random - a.random);
console.log(JSON.stringify({ id, title: base.title, colours: cnt, candidates: res.length, hard: hard.slice(0, 3), normal: normal.slice(0, 2), dist: { solvable: res.length, hardWindow: hard.length, normalWindow: normal.length } }));
if (hard[0]) { const L = make(hard[0].seed); fs.writeFileSync(`retray-${id}-hard.json`, JSON.stringify({ seed: hard[0].seed, tray: L.tray.map(c => c.map(p => L.pods[p].label)) })); }
if (normal[0]) { const L = make(normal[0].seed); fs.writeFileSync(`retray-${id}-normal.json`, JSON.stringify({ seed: normal[0].seed, tray: L.tray.map(c => c.map(p => L.pods[p].label)) })); }

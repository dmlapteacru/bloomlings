// Proposal sketch for our L5: a ringed flower in hero materials, our access rule (two bottom arches), CF-style pod splitting.
import fs from 'fs';
import R from './rules.cjs';
import { solve } from './search.mjs';
const top = [
'WWWWWWWWWWWWWWWWWWWW','WWWWWWWDDDDDDWWWWWWW','WWWWWWDPPPPPPDWWWWWW','WWWDDDPPRRRRPPDDDWWW','WWDPPPDPRRRRPDPPPDWW',
'WDPPRRPDPRRPDPRRPPDW','WDPRRRRPDDDDPRRRRPDW','WDPRRRPDYYYYDPRRRPDW','WWDPPPDYYYYYYDPPPDWW','WWWDDDYYYDDYYYDDDWWW',
'WWDPPPDYYYYYYDPPPDWW','WDPRRRPDYYYYDPRRRPDW','WDPRRRRPDDDDPRRRRPDW','WDPPRRPDPRRPDPRRPPDW','WWDPPPDPRRRRPDPPPDWW',
'WWWDDDPPRRRRPPDDDWWW','WWWWWWDPPLLPPDWWWWWW','WWWWWWWDDLLDDWWWWWWW','WWWWWWWWWLLWWWWWWWWW','WWWWWWWWWLLWWWWWWWWW'];
const map = { W: 'A', D: 'B', P: 'C', R: 'D', Y: 'E', L: 'F' };
const rows = top.slice().reverse().map(r => r.split('').map(c => map[c]).join(''));
const cnt = {}; for (const c of top.join('')) cnt[c] = (cnt[c] || 0) + 1;
const rng = seed => { let s = seed >>> 0 || 1; return () => { s ^= s << 13; s ^= s >>> 17; s ^= s << 5; return (s >>> 0) / 4294967296; }; };
const legal = s => R.legal(s).filter(a => a.type === 'tap');
const vis = s => new Set(R.reach(s).targets.map(i => s.board[i]));
const [lo, hi, vmin] = process.argv.slice(2).map(Number);
function make(seed) {
  const r = rng(seed), pods = [];
  for (const [c, n] of Object.entries(cnt)) { let left = n; while (left > 0) { let k = r() < 0.2 ? 4 + Math.floor(r() * 7) : 15 + Math.floor(r() * 26); if (left - k < 4) k = left; k = Math.min(k, left); pods.push({ color: map[c], count: k, label: c + k }); left -= k; } }
  const order = pods.map((_, i) => i); for (let i = order.length - 1; i > 0; i--) { const j = Math.floor(r() * (i + 1)); [order[i], order[j]] = [order[j], order[i]]; }
  const tray = [[], [], []]; order.forEach((p, i) => tray[i % 3].push(p));
  return { id: 5, w: 20, h: 20, rowsBottomFirst: rows, entries: [4, 15], capacity: 5, pods, tray };
}
function rate(L, mode, n) { let w = 0; for (let i = 0; i < n; i++) { const r = rng(7 + i * 7919); let s = R.create(L); while (s.status === 'Playing') { let m = legal(s); if (mode === 'left') m = [m[0]]; if (mode === 'right') m = [m[m.length - 1]]; if (mode === 'visible') { const v = vis(s); const f = m.filter(a => v.has(s.meta.colours[a.pod])); if (f.length) m = f; } s = R.step(s, m[Math.floor(r() * m.length)]).state; } if (s.status === 'Won') w++; } return w / n; }
const ok = [];
for (let seed = 1; seed <= 400 && ok.length < 3; seed++) {
  const L = make(seed); if (solve(R.create(L), { budget: 200000 }).winning !== true) continue;
  const m = { seed, random: rate(L, 'random', 400), visible: rate(L, 'visible', 400), left: rate(L, 'left', 1), right: rate(L, 'right', 1) };
  if (m.random >= lo && m.random <= hi && m.visible >= vmin) ok.push({ ...m, tray: L.tray.map(c => c.map(p => L.pods[p].label)) });
}
console.log(JSON.stringify({ cnt, ok }, null, 0));

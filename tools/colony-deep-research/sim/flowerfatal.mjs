import R from './rules.cjs';
import { solve } from './search.mjs';
const top = ['WWWWWWWWWWWWWWWWWWWW','WWWWWWWDDDDDDWWWWWWW','WWWWWWDPPPPPPDWWWWWW','WWWDDDPPRRRRPPDDDWWW','WWDPPPDPRRRRPDPPPDWW','WDPPRRPDPRRPDPRRPPDW','WDPRRRRPDDDDPRRRRPDW','WDPRRRPDYYYYDPRRRPDW','WWDPPPDYYYYYYDPPPDWW','WWWDDDYYYDDYYYDDDWWW','WWDPPPDYYYYYYDPPPDWW','WDPRRRPDYYYYDPRRRPDW','WDPRRRRPDDDDPRRRRPDW','WDPPRRPDPRRPDPRRPPDW','WWDPPPDPRRRRPDPPPDWW','WWWDDDPPRRRRPPDDDWWW','WWWWWWDPPLLPPDWWWWWW','WWWWWWWDDLLDDWWWWWWW','WWWWWWWWWLLWWWWWWWWW','WWWWWWWWWLLWWWWWWWWW'];
const map = { W: 'A', D: 'B', P: 'C', R: 'D', Y: 'E', L: 'F' };
const tr = [["D8","W7","P39","R21","Y18","L8","W23","W23"],["W19","D36","W4","R4","D5","W40","R7","P31"],["R24","W23","W6","Y8","D29","P8","W9"]];
const pods = [], tray = tr.map(c => c.map(l => { pods.push({ color: map[l[0]], count: +l.slice(1), label: l }); return pods.length - 1; }));
const L = { id: 5, w: 20, h: 20, rowsBottomFirst: top.slice().reverse().map(r => r.split('').map(c => map[c]).join('')), entries: [4, 15], capacity: 5, pods, tray };
const rng = seed => { let s = seed >>> 0 || 1; return () => { s ^= s << 13; s ^= s >>> 17; s ^= s << 5; return (s >>> 0) / 4294967296; }; };
const memo = new Map(); const win = s => { const k = R.signature(s); if (memo.has(k)) return memo.get(k); const w = solve(s, { budget: 200000 }).winning; memo.set(k, w); return w; };
const f = {}, at = {}; let lost = 0;
for (let i = 0; i < 300; i++) { const r = rng(101 + i * 7919); let s = R.create(L), k = 0;
  while (s.status === 'Playing') { const m = R.legal(s); const a = m[Math.floor(r() * m.length)]; const n = R.step(s, a).state; k++; if (win(s) && !win(n)) { f[pods[a.pod].label] = (f[pods[a.pod].label] || 0) + 1; at[k] = (at[k] || 0) + 1; lost++; break; } s = n; } }
console.log(lost, JSON.stringify(Object.entries(f).sort((a, b) => b[1] - a[1]).slice(0, 6)), JSON.stringify(at));

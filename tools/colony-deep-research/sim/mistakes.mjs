// For random / visible-colour play: which tap first makes the level unwinnable, and at which move number.
import fs from 'fs';
import R from './rules.cjs';
import { solve } from './search.mjs';
const src = fs.readFileSync('run.mjs', 'utf8');
const G = JSON.parse(fs.readFileSync(process.argv[2], 'utf8')), T = JSON.parse(fs.readFileSync(process.argv[3], 'utf8'));
const buildSrc = src.slice(src.indexOf('function build'), src.indexOf('const legal'));
const build = new Function('G', 'T', 'R', buildSrc + '; return build;')(G, T, R);
const rng = seed => { let s = seed >>> 0 || 1; return () => { s ^= s << 13; s ^= s >>> 17; s ^= s << 5; return (s >>> 0) / 4294967296; }; };
const legal = s => R.legal(s).filter(a => a.type === 'tap');
const vis = s => new Set(R.reach(s).targets.map(i => s.board[i]));
const memo = new Map();
const win = s => { const k = R.signature(s); if (memo.has(k)) return memo.get(k); const w = solve(s, { budget: 200000 }).winning; memo.set(k, w); return w; };
for (const name of process.argv[4].split(',')) {
  const { level } = build(name);
  for (const mode of ['random', 'visible']) {
    const firsts = {}, at = {}; let lost = 0, n = 300;
    for (let i = 0; i < n; i++) {
      const r = rng(101 + i * 7919); let s = R.create(level), k = 0, slotsAt = null;
      while (s.status === 'Playing') {
        let m = legal(s); if (mode === 'visible') { const v = vis(s); const f = m.filter(a => v.has(s.meta.colours[a.pod])); if (f.length) m = f; }
        const a = m[Math.floor(r() * m.length)]; const next = R.step(s, a).state; k++;
        if (win(s) && !win(next)) { const lab = level.pods[a.pod].label; firsts[lab] = (firsts[lab] || 0) + 1; at[k] = (at[k] || 0) + 1; lost++; break; }
        s = next;
      }
    }
    const top = Object.entries(firsts).sort((a, b) => b[1] - a[1]).slice(0, 6).map(([k, v]) => `${k}×${v}`).join(', ');
    const when = Object.entries(at).sort((a, b) => +a[0] - +b[0]).map(([k, v]) => `${k}:${v}`).join(' ');
    console.log(`${name} ${mode}: lost ${lost}/${n}; first fatal tap: ${top}; at move#: ${when}`);
  }
}

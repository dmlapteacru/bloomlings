// Our curated L1-10 in the same engine, as a cross-check against the Core audit (exact random-tap win rates).
import fs from 'fs';
import R from './rules.cjs';
const root = '/home/user/bloomlings/content/';
const rng = seed => { let s = seed >>> 0 || 1; return () => { s ^= s << 13; s ^= s >>> 17; s ^= s << 5; return (s >>> 0) / 4294967296; }; };
const legal = s => R.legal(s).filter(a => a.type === 'tap');
for (let n = 1; n <= 10; n++) {
  const L = JSON.parse(fs.readFileSync(root + `curated/level-${String(n).padStart(4, '0')}.json`, 'utf8'));
  const P = JSON.parse(fs.readFileSync(root + `pictures/lib/${L.picture.id}.json`, 'utf8'));
  const variants = [...new Set(Object.values(L.mapping))];
  const ch = Object.fromEntries(variants.map((v, i) => [v, R.CHANNELS[i]]));
  const roleVariant = P.roles.map(r => L.mapping[r.roleId]);
  const rows = P.grid.map(r => r.map(c => c < 0 ? '.' : ch[roleVariant[c]]).join(''));
  const idx = Object.fromEntries(L.pods.map((p, i) => [p.id, i]));
  const level = { id: n, w: P.width, h: P.height, rowsBottomFirst: rows, capacity: L.slots.count,
    entries: L.entries.map(e => e.y * P.width + e.x),
    pods: L.pods.map(p => ({ color: ch[p.variantId], count: p.count })), tray: L.tray.stacks.map(s => s.map(id => idx[id])) };
  const memo = new Map();
  const visit = s => { if (s.status === 'Won') return 1; if (s.status !== 'Playing') return 0; const k = R.signature(s); if (memo.has(k)) return memo.get(k); const m = legal(s); let p = 0; for (const a of m) p += visit(R.step(s, a).state) / m.length; memo.set(k, p); return p; };
  console.log(`L${n} ${L.difficulty.class} ${P.width}x${P.height} pods ${L.pods.length} exactRandom ${(visit(R.create(level)) * 100).toFixed(1)}% errs ${R.validate(level).join(',')}`);
}

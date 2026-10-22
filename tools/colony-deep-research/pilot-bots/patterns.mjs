// "Mindless" tapping bots: look at the screen and tap in a fixed pattern, ignoring colours and the board.
// usage: node patterns.mjs <pilot tool dir> <levels.json> [quality.json with random rates]
import fs from 'fs';
import { createRequire } from 'module';
const [toolDir, levelsPath] = process.argv.slice(2);
const R = createRequire(import.meta.url)(toolDir + '/idle-rules.cjs');
const levels = JSON.parse(fs.readFileSync(levelsPath, 'utf8')).levels;
const columnOf = (s, pod) => s.level.tray.findIndex((stack, c) => stack[s.heads[c]] === pod);
// Each pattern yields an endless column order; the bot taps the first column in that order it can tap.
const cycle = order => k => order[k % order.length];
const PATTERNS = {
  'колонка слева до конца': () => [0, 1, 2, 3, 4, 5],
  'колонка справа до конца': n => [...Array(n).keys()].reverse(),
  'ряд слева направо': n => k => k % n,
  'ряд справа налево': n => k => n - 1 - (k % n),
  'змейка': n => k => { const r = k % (2 * n); return r < n ? r : 2 * n - 1 - r; },
  'середина, потом края': n => cycle([Math.floor(n / 2), ...[...Array(n).keys()].filter(c => c !== Math.floor(n / 2))]),
  'по две из колонки': n => k => Math.floor(k / 2) % n,
  'самое большое число': 'big',
  'самое маленькое число': 'small',
};
function play(level, name) {
  let s = R.create(level), k = 0, g = 0; const n = level.tray.length, p = PATTERNS[name];
  while (s.status === 'Playing' && g++ < 500) {
    const m = R.legal(s); let a = m.find(x => x.type === 'bridge');
    if (!a) {
      const taps = m.filter(x => x.type === 'tap');
      if (p === 'big' || p === 'small') a = taps.reduce((x, y) => (p === 'big' ? s.remaining[y.pod] > s.remaining[x.pod] : s.remaining[y.pod] < s.remaining[x.pod]) ? y : x);
      else {
        const f = p(n), order = Array.isArray(f) ? f : null;
        const byCol = new Map(taps.map(t => [columnOf(s, t.pod), t]));
        if (order) a = order.map(c => byCol.get(c)).find(Boolean);
        else { for (let j = 0; j < 4 * n && !a; j++) { a = byCol.get(f(k + j)); if (a) k += j; } k++; }
        a ||= taps[0];
      }
    }
    s = R.step(s, a).state;
  }
  return s.status === 'Won';
}
const names = Object.keys(PATTERNS);
const rows = levels.map(l => { const wins = names.filter(nm => play(l, nm)); return { id: l.id, title: l.title, cls: l.difficulty, wins: wins.length, of: names.length, which: wins }; });
console.log(JSON.stringify({ patterns: names, rows }));

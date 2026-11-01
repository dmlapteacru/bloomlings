// Writes reference-rounds.json: what the reference round engine (tools/rounds/engine/, JS) does on the placed round
// levels, for RoundReferenceTests (the C# port must match it exactly). Run from the repository root:
//   node core/tests/Bloomlings.Core.Tests/Rounds/Fixtures/export-reference.cjs [first] [last]
// It takes a few minutes (every recorded random game is replayed). Regenerate only after a reviewed change of the
// reference engine or of the levels in content/rounds/levels/, and commit the fixture with that change.
//
// Per level:
// - solution: the saved route replayed patiently (all work settles before each command, as measure.mjs solve / trace.mjs);
//   per command window the SHA-256 of the canonical event lines and of the extended signature right after the command;
//   the outcome, the end time and the final extended signature;
// - timed: the same route on trace.mjs's running clock with fractional-millisecond delays (idle time kept apart, a refused
//   tap waits in 100 ms chunks), so commands land while work is in flight;
// - random: every game of the level's recorded random test (measure.mjs randomGame, seed seedBase*7919+n*104729+1):
//   [seed, won 1/0, end µs, first 16 hex digits of the final signature's SHA-256, status letter];
// - solver: the patient search (level-stats/solve.mjs route, budget 200000): nodes and whether it finds the saved route.
// Hidden cells (17 §8 «Скрытые клетки», 2026-11-01), under `cells`: the small level of tools/rounds/test/cells.test.mjs
// (far cells behind a bridge, a deep cell, a start-front cell) and placed levels L5, L13 and L16 given hidden cells by a
// fixed rule (every colour cell whose index hashes to 0 mod 7); each with the same solution, timed and solver entries, 40
// random games (seedBase 1), the cells veiled after the start reveal, and the solution's cellReveal lines in order (the
// small level: its whole canonical log).
// Latch + acorn key (17 §8 «Засов и жёлудь-ключ», 2026-11-01), under `latches`: the small level of
// tools/rounds/test/latch.test.mjs in four forms (a hidden pod behind the latch that a clear opens, the key reached before
// the latch comes on top so a commit opens it, a latch open at the start, a pair whose partner stands behind the latch)
// and placed levels L14, L16 (two latches, a bridge), L17 (hidden pods) and L28 (links) given latches by a fixed rule (in
// the longest column(s) at depth 2 or 3, each key an empty cell the start board does not reach, chosen by a hash); each
// with the route the patient solver finds (the saved routes may tap a held pod), the same solution, timed and solver
// entries, 40 random games (seedBase 1), the latches closed after create, and the solution's latchOpen lines with the
// line before each (the small levels: the whole canonical log).
// Times are the reference's milliseconds × 1000, rounded (all of them are whole milliseconds here).
'use strict';
const fs = require('fs'), path = require('path'), crypto = require('crypto');
const repo = path.resolve(__dirname, '../../../../..');
const R = require(path.join(repo, 'tools/rounds/engine/rules.cjs'));
const { Timeline } = require(path.join(repo, 'tools/rounds/engine/timeline.cjs'));
const idle = require(path.join(repo, 'tools/rounds/engine/idle-rules.cjs'));
const first = Number(process.argv[2] || 1), last = Number(process.argv[3] || 20);

const us = ms => Math.round(ms * 1000);
const sha = s => 'sha256:' + crypto.createHash('sha256').update(s, 'utf8').digest('hex');
const fingerprint = f => crypto.createHash('sha256').update(fs.readFileSync(path.join(repo, 'tools/rounds/engine', f))).digest('hex');
const rng = seed => { let s = seed >>> 0 || 1; return () => { s ^= s << 13; s ^= s >>> 17; s ^= s << 5; return (s >>> 0) / 4294967296; }; };

// The canonical log (contract conformance-fixture.md): commit results, then the timeline's events; no assign.
class Log {
  constructor() { this.lines = []; this.all = []; this.cellOf = new Map(); }
  commit(t, events) {
    const at = us(t.now);
    for (const e of events) {
      if (e.type === 'commit') this.lines.push(`${at} commit pod=${e.pod} slot=${e.slot}`);
      else if (e.type === 'reveal') this.lines.push(`${at} reveal pod=${e.pod} mode=${e.mode}`);
      else if (e.type === 'bridge') this.lines.push(`${at} bridge cell=${e.cell}`);
      else if (e.type === 'cellReveal') this.lines.push(`${us(e.at)} cellReveal cell=${e.cell}`);
      else if (e.type === 'latchOpen') this.lines.push(`${e.at != null ? us(e.at) : at} latchOpen latch=${e.latch}`);
      else throw Error('unknown commit event ' + e.type);
    }
    this.timeline(t);
  }
  timeline(t) {
    for (const e of t.lastEvents) {
      const at = us(e.at);
      if (e.type === 'assign') continue;
      if (e.type === 'depart') { const p = e.plan; this.cellOf.set(p[0], p[2]); this.lines.push(`${at} depart id=${p[0]} pod=${p[1]} cell=${p[2]} arrive=${us(p[5])} clear=${us(p[6])} done=${us(p[7])} route=${p[8]}`); }
      else if (e.type === 'arrive') this.lines.push(`${at} arrive id=${e.id}`);
      else if (e.type === 'clear') this.lines.push(`${at} clear id=${e.id} cell=${this.cellOf.get(e.id)}`);
      else if (e.type === 'return') this.lines.push(`${at} return id=${e.id}`);
      else if (e.type === 'complete') this.lines.push(`${at} complete pod=${e.pod}`);
      else if (e.type === 'cellReveal') this.lines.push(`${at} cellReveal cell=${e.cell}`);
      // latch + key: a clear opened a latch; the pods it exposes reveal by the depth rule at that clear's time
      else if (e.type === 'latchOpen') this.lines.push(`${at} latchOpen latch=${e.latch}`);
      else if (e.type === 'reveal') this.lines.push(`${at} reveal pod=${e.pod} mode=${e.mode}`);
      else throw Error('unknown event ' + e.type);
    }
  }
  take() { const s = this.lines.map(l => l + '\n').join(''); this.all.push(...this.lines); this.lines = []; return s; }
}
// The extended signature: signature(state)|now|steps (at:type:id-or-pod).
const signature = t => R.signature(t.state) + '|' + us(t.now) + '|' + t.steps.map(e => `${us(e.at)}:${e.type}:${e.id ?? e.pod}`).join(',');
const command = (a, atUs) => a.type === 'tap' ? { type: 'tap', pod: a.pod, atUs } : { type: 'bridge', choice: a.choice, atUs };

function solution(level, route) {
  const t = new Timeline(R.create(level, level.capacity || 5)), log = new Log(), windows = [];
  const settle = () => { while (t.pending) { t.advance(Math.max(0, t.steps[0].at - t.now)); log.timeline(t); } };
  for (let k = 0; k < route.length; k++) {
    settle();
    if (k > 0) windows[k - 1].events = sha(log.take());
    const at = us(t.now), r = t.commit(route[k]);
    if (!r.accepted) throw Error('refused ' + JSON.stringify(route[k]) + ' ' + r.reason);
    log.commit(t, r.events);
    windows.push({ command: command(route[k], at), signature: sha(signature(t)) });
  }
  settle();
  windows[windows.length - 1].events = sha(log.take());
  return { windows, outcome: t.state.status, endUs: us(t.now), finalSignature: sha(signature(t)), lines: log.all };
}

function timed(level, route) {
  // whole microseconds, fed to the reference as fractional milliseconds (research R2)
  const delaysUs = route.map((_, k) => ((k * 7919) % 2900) * 1000 + 123 + (k % 7) * 37);
  const t = new Timeline(R.create(level, level.capacity || 5)), log = new Log(); let idleMs = 0;
  const clock = () => t.now + idleMs, nextAt = () => t.steps[0].at + idleMs;
  const advanceTo = target => {
    while (t.pending && t.steps[0].at + idleMs <= target + 1e-7) { t.advance(Math.max(0, t.steps[0].at - t.now)); log.timeline(t); }
    if (target > clock()) { if (t.pending) { t.advance(target - clock()); log.timeline(t); } else idleMs += target - clock(); }
  };
  let refused = 0, atUs = 0; const windows = [];
  for (let k = 0; k < route.length; k++) {
    atUs += delaysUs[k];
    const at = atUs / 1000;
    if (at > clock()) advanceTo(at);
    let g = 0; while (t.check(route[k]) && t.pending && g++ < 2000) advanceTo(Math.min(nextAt(), clock() + 100));
    windows.push(sha(log.take()));
    const r = t.commit(route[k]); if (!r.accepted) { refused++; continue; } log.commit(t, r.events);
  }
  while (t.pending) advanceTo(nextAt());
  windows.push(sha(log.take()));
  return { delaysUs, outcome: t.state.status, endUs: us(clock()), idleUs: us(idleMs), refused, finalSignature: sha(signature(t)), windows };
}

const isStateEvent = e => e.type !== 'return' && e.type !== 'arrive';
function randomGame(level, seed) {
  const t = new Timeline(R.create(level, level.capacity || 5)), r = rng(seed); let guard = 0, won = false;
  const settleNow = () => { while (t.pending && t.steps[0].at <= t.now + 1e-7) t.advance(0); };
  const waitEvent = () => { while (t.pending) { const e = t.steps[0]; t.advance(Math.max(0, e.at - t.now)); if (isStateEvent(e)) break; } settleNow(); };
  while (guard++ < 200000) {
    settleNow(); if (t.state.status === 'Won') { won = true; break; }
    const moves = R.legal(t.state).filter(a => !t.check(a)), canWait = t.steps.some(isStateEvent);
    if (!moves.length) { if (t.pending) { waitEvent(); continue; } break; }
    const k = Math.floor(r() * (moves.length + (canWait ? 1 : 0)));
    if (k === moves.length) { waitEvent(); continue; }
    t.commit(moves[k]);
  }
  return [seed, won ? 1 : 0, us(t.now), sha(signature(t)).slice(7, 23), t.state.status[0]];
}

function solverRoute(level, budget) {
  const memo = new Map(); let n = 0;
  const go = s => {
    if (s.status === 'Won') return []; if (s.status !== 'Playing') return null;
    const k = idle.signature(s); if (memo.has(k)) return memo.get(k); if (++n > budget) throw 0;
    for (const a of idle.legal(s)) { const r = go(idle.step(s, a).state); if (r) { const o = [a, ...r]; memo.set(k, o); return o; } }
    memo.set(k, null); return null;
  };
  try { const r = go(idle.create(level, level.capacity || 5)); return { win: !!r, route: r, nodes: n }; }
  catch (e) { if (e !== 0) throw e; return { win: null, route: null, nodes: n }; }
}

const definitionOf = d => ({ w: d.w, h: d.h, rowsBottomFirst: d.rowsBottomFirst, entries: d.entries, capacity: d.capacity, pods: d.pods, tray: d.tray,
  ...(d.hero ? { hero: d.hero } : {}), ...(d.hiddenCells ? { hiddenCells: d.hiddenCells } : {}),
  ...(d.latches ? { latches: d.latches.map(l => ({ column: l.column, depth: l.depth, key: l.key })) } : {}) });
// hidden cells by a fixed rule: every colour cell whose index hashes to 0 mod 7
const pickCells = d => { const grid = d.rowsBottomFirst.join(''), out = [];
  for (let i = 0; i < grid.length; i++) if (R.CHANNELS.includes(grid[i]) && (Math.imul(i, 40503) >>> 0) % 7 === 0) out.push(i);
  return out; };
async function cellCases() {
  const L = await import(path.join(repo, 'tools/rounds/lib.mjs'));
  const pic = { id: 'T', title: 'test', rows: ['AAAAAAA', 'AAAAAAA', '##b####', 'GGGGGGG', 'GGGGGGG', 'GGGGGGG'], w: 7, h: 6 };
  const board = L.boardOf(pic), idx = (x, y) => (board.h - 1 - (y + 2)) * board.w + x + 2;
  const cases = [{ name: 'small', definition: { w: board.w, h: board.h, rowsBottomFirst: board.rowsBottomFirst, entries: board.entries, capacity: 5,
    pods: [{ color: 'G', count: 21 }, { color: 'A', count: 14 }], tray: [[0], [1]], hero: board.hero,
    hiddenCells: [idx(2, 1), idx(3, 1), idx(2, 0), idx(3, 4), idx(0, 3)].sort((a, b) => a - b) },
    route: [{ type: 'tap', pod: 0 }, { type: 'bridge', choice: 0 }, { type: 'tap', pod: 1 }], full: true }];
  for (const n of [5, 13, 16]) {
    const Lv = JSON.parse(fs.readFileSync(path.join(repo, `content/rounds/levels/L${String(n).padStart(4, '0')}.json`), 'utf8'));
    const d = { ...Lv.definition }; delete d.hiddenCells; d.hiddenCells = pickCells(d);
    cases.push({ name: `L${String(n).padStart(4, '0')}+cells`, definition: definitionOf(d), route: Lv.solution.route, full: false });
  }
  return cases.map(c => {
    const d = c.definition, errors = R.validate({ id: 1, ...d });
    if (errors.length) throw Error(c.name + ': ' + errors.join(', '));
    const s0 = R.create(d, d.capacity || 5), sol = solution(d, c.route), games = [];
    for (let i = 0; i < 40; i++) games.push(randomGame(d, 1 * 7919 + i * 104729 + 1));
    const sv = solverRoute(d, 200000);
    return { name: c.name, definition: d, route: c.route,
      veiledAtStart: Array.from(s0.veiled).flatMap((v, i) => v ? [i] : []),
      reveals: sol.lines.filter(l => / cellReveal /.test(l)), ...(c.full ? { lines: sol.lines } : {}),
      solution: { windows: sol.windows, outcome: sol.outcome, endUs: sol.endUs, finalSignature: sol.finalSignature },
      timed: timed(d, c.route), random: { seedBase: 1, runs: games.length, wins: games.reduce((a, g) => a + g[1], 0), games },
      solver: { win: sv.win, nodes: sv.nodes, sameAsSaved: !!sv.route && JSON.stringify(sv.route) === JSON.stringify(c.route) } };
  });
}

// latches by a fixed rule: in the longest columns (ties: the lower index), the first at depth 2, a second at depth 3;
// each key an empty non-entry cell the start board does not reach, the hole picked by a hash of the level number
function pickLatches(d, n, count) {
  const s = R.create(d, d.capacity || 5), dist = R.reach(s).distance, grid = d.rowsBottomFirst.join(''), entries = d.entries || [Math.floor(d.w / 2)];
  const holes = []; for (let i = 0; i < grid.length; i++) if (grid[i] === '.' && !(dist[i] > 0) && !entries.includes(i)) holes.push(i);
  const columns = d.tray.map((c, i) => i).sort((a, b) => d.tray[b].length - d.tray[a].length || a - b);
  const out = [];
  for (let k = 0; k < count; k++) out.push({ column: columns[k], depth: 2 + k, key: holes[(Math.imul(n * 7 + k * 13, 40503) >>> 0) % holes.length] });
  if (new Set(out.map(l => l.key)).size !== out.length) out[1].key = holes.find(h => h !== out[0].key);
  return out;
}
async function latchCases() {
  const L = await import(path.join(repo, 'tools/rounds/lib.mjs'));
  // the small level of tools/rounds/test/latch.test.mjs: G (20 cells) over A (14), one hole in the G area at picture (1, 1)
  const toy = ({ pods = [{ color: 'A', count: 7 }, { color: 'A', count: 7 }, { color: 'G', count: 10 }, { color: 'G', count: 10 }], tray = [[0], [1], [2, 3]], latches } = {}) => {
    const b = L.boardOf({ id: 'T', title: 't', rows: ['GGGGGGG', 'G.GGGGG', 'GGGGGGG', 'AAAAAAA', 'AAAAAAA'], w: 7, h: 5 }), at = (x, y) => (b.h - 1 - (y + 2)) * b.w + x + 2;
    return { w: b.w, h: b.h, rowsBottomFirst: b.rowsBottomFirst, entries: b.entries, capacity: 5, pods, tray, latches: latches ? latches(at, b) : [{ column: 2, depth: 1, key: at(1, 1) }] };
  };
  const cases = [
    { name: 'small', definition: toy({ pods: [{ color: 'A', count: 7 }, { color: 'A', count: 7 }, { color: 'G', count: 10 }, { color: 'G', count: 10, mystery: 'depth' }] }), full: true },
    { name: 'small-commit', definition: toy({ tray: [[0], [2], [1, 3]] }), full: true },
    { name: 'small-start', definition: toy({ latches: (at, b) => [{ column: 2, depth: 0, key: b.w + 1 }] }), full: true },
    { name: 'small-pair', definition: toy({ pods: [{ color: 'A', count: 7 }, { color: 'A', count: 7, group: 'P' }, { color: 'G', count: 10 }, { color: 'G', count: 10, group: 'P' }], tray: [[0, 1], [2, 3]],
      latches: at => [{ column: 1, depth: 1, key: at(1, 1) }] }), full: true },
  ];
  for (const [n, count] of [[14, 1], [16, 2], [17, 1], [28, 1]]) {
    const Lv = JSON.parse(fs.readFileSync(path.join(repo, `content/rounds/levels/L${String(n).padStart(4, '0')}.json`), 'utf8'));
    const d = { ...Lv.definition }; d.latches = pickLatches(d, n, count);
    cases.push({ name: `L${String(n).padStart(4, '0')}+latch`, definition: definitionOf(d), full: false });
  }
  return cases.map(c => {
    const d = c.definition, errors = R.validate({ id: 1, ...d });
    if (errors.length) throw Error(c.name + ': ' + errors.join(', '));
    const sv = solverRoute(d, 200000);
    if (!sv.win) throw Error(c.name + ': the patient solver finds no route (' + sv.win + ', ' + sv.nodes + ' nodes)');
    const route = sv.route, s0 = R.create(d, d.capacity || 5), sol = solution(d, route), games = [];
    for (let i = 0; i < 40; i++) games.push(randomGame(d, 1 * 7919 + i * 104729 + 1));
    const opens = []; sol.lines.forEach((l, i) => { if (/ latchOpen /.test(l)) opens.push(sol.lines[i - 1] || '', l); });
    return { name: c.name, definition: d, route,
      latchedAtStart: Array.from(s0.latched).flatMap((v, i) => v ? [i] : []),
      opens, ...(c.full ? { lines: sol.lines } : {}),
      solution: { windows: sol.windows, outcome: sol.outcome, endUs: sol.endUs, finalSignature: sol.finalSignature },
      timed: timed(d, route), random: { seedBase: 1, runs: games.length, wins: games.reduce((a, g) => a + g[1], 0), games },
      solver: { win: sv.win, nodes: sv.nodes, sameAsSaved: true } };
  });
}

const out = {
  format: 'bloomlings-round-reference-1',
  note: 'Generated by export-reference.cjs from the reference round engine; do not edit by hand.',
  engine: Object.fromEntries(['rules.cjs', 'timeline.cjs', 'palette.cjs', 'clock.cjs'].map(f => [f.replace('.cjs', ''), fingerprint(f)])),
  levels: [],
};
for (let n = first; n <= last; n++) {
  const file = `content/rounds/levels/L${String(n).padStart(4, '0')}.json`;
  const L = JSON.parse(fs.readFileSync(path.join(repo, file), 'utf8')), d = L.definition, t0 = Date.now();
  const definition = definitionOf(d);
  const games = []; let wins = 0;
  for (let i = 0; i < L.random.runs; i++) { const g = randomGame(d, L.random.seedBase * 7919 + i * 104729 + 1); wins += g[1]; games.push(g); }
  if (wins !== L.random.wins) throw Error(`${file}: the reference now wins ${wins}, the level records ${L.random.wins}`);
  const sv = solverRoute(d, 200000);
  out.levels.push({
    file, number: n, cls: L.cls, intro: L.intro, definition, route: L.solution.route,
    solution: (({ lines, ...rest }) => rest)(solution(d, L.solution.route)), timed: timed(d, L.solution.route),
    random: { seedBase: L.random.seedBase, runs: L.random.runs, wins, games },
    solver: { win: sv.win, nodes: sv.nodes, sameAsSaved: !!sv.route && JSON.stringify(sv.route) === JSON.stringify(L.solution.route) },
  });
  console.error(`${file}: random ${wins}/${L.random.runs}, solver ${sv.win} in ${sv.nodes} nodes, ${Date.now() - t0} ms`);
}
cellCases().then(async cases => {
  out.cells = cases;
  for (const c of cases) console.error(`${c.name}: ${c.veiledAtStart.length} veiled at the start, ${c.reveals.length} revealed, ${c.solution.outcome}, random ${c.random.wins}/${c.random.runs}`);
  out.latches = await latchCases();
  for (const c of out.latches) console.error(`${c.name}: ${c.latchedAtStart.length} closed at the start, ${c.opens.length / 2} opened, ${c.solution.outcome}, random ${c.random.wins}/${c.random.runs}, solver ${c.solver.nodes} nodes`);
  fs.writeFileSync(path.join(__dirname, 'reference-rounds.json'), JSON.stringify(out) + '\n');
});

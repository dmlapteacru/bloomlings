/* Deterministic, engine-only slice for the browser playtest. No DOM or randomness.
 * Ordinary moves follow Core/Board/Reachability.cs and Simulation/Settler.cs.
 * The one-use Sprig bridge is a pilot extension, checked separately against a
 * harness compiled from those same C# sources. Production rules are untouched.
 */
(function (root, factory) {
  const api = factory();
  if (typeof module !== 'undefined' && module.exports) module.exports = api;
  else root.BloomPilotRules = api;
})(typeof globalThis !== 'undefined' ? globalThis : this, function () {
  'use strict';
  const CHANNELS = 'GPBACD';
  const OPEN = -1, WALL = -2, CAPACITY = 5;
  const prepared = new WeakMap();

  function prepare(level) {
    if (prepared.has(level)) return prepared.get(level);
    const n = level.w * level.h;
    const neighbours = new Int16Array(n * 4).fill(-1);
    for (let i = 0; i < n; i++) {
      const x = i % level.w, y = Math.floor(i / level.w), at = i * 4;
      neighbours[at] = y > 0 ? i - level.w : -1;
      neighbours[at + 1] = x > 0 ? i - 1 : -1;
      neighbours[at + 2] = x + 1 < level.w ? i + 1 : -1;
      neighbours[at + 3] = y + 1 < level.h ? i + level.w : -1;
    }
    const board = Int8Array.from(level.rowsBottomFirst.join(''), c => c === '.' ? OPEN : c === '#' ? WALL : CHANNELS.indexOf(c));
    const colours = Int8Array.from(level.pods, p => CHANNELS.indexOf(p.color));
    const entries = level.entries || [Math.floor(level.w / 2)];
    const entrySet = new Set(entries);
    const result = { board, neighbours, colours, entries, entrySet };
    prepared.set(level, result);
    return result;
  }

  function create(level) {
    const meta = prepare(level);
    const state = { level, meta, board: meta.board.slice(),
      remaining: Int16Array.from(level.pods, p => p.count),
      heads: new Int16Array(level.tray.length), slots: new Int16Array(CAPACITY).fill(-1),
      ages: new Int16Array(level.pods.length), serial: 0, bridge: -1, status: 'Playing', _reach: null };
    state.status = status(state);
    return state;
  }

  function clone(state) {
    return { ...state, board: state.board.slice(), remaining: state.remaining.slice(),
      heads: state.heads.slice(), slots: state.slots.slice(), ages: state.ages.slice(), _reach: state._reach };
  }

  function reach(state) {
    if (state._reach) return state._reach;
    const { board, meta } = state, n = board.length;
    const distance = new Int16Array(n).fill(-1), queue = new Int16Array(n);
    let head = 0, tail = 0;
    for (const entry of meta.entries) if (board[entry] === OPEN && distance[entry] < 0) {
      distance[entry] = 1; queue[tail++] = entry;
    }
    while (head < tail) {
      const i = queue[head++], next = distance[i] + 1;
      for (let d = 0; d < 4; d++) {
        const j = meta.neighbours[i * 4 + d];
        if (j >= 0 && board[j] === OPEN && distance[j] < 0) { distance[j] = next; queue[tail++] = j; }
      }
    }
    const targets = [], targetDistance = new Int16Array(n);
    for (let i = 0; i < n; i++) if (board[i] >= 0) {
      let best = meta.entrySet.has(i) ? 1 : 32767;
      for (let d = 0; d < 4; d++) {
        const j = meta.neighbours[i * 4 + d];
        if (j >= 0 && distance[j] > 0) best = Math.min(best, distance[j] + 1);
      }
      if (best < 32767) { targets.push(i); targetDistance[i] = best; }
    }
    targets.sort((a, b) => targetDistance[a] - targetDistance[b] || a - b);
    state._reach = { targets, distance, targetDistance };
    return state._reach;
  }

  function bridgeChoices(state) {
    if (!state.level.hero || state.bridge >= 0) return [];
    const open = reach(state).distance;
    return state.level.hero.choices.map((c, i) => ({ ...c, choice: i }))
      .filter(c => state.board[c.cell] === WALL && open[c.anchor] > 0);
  }

  function topPods(state) {
    return state.level.tray.map((stack, c) => stack[state.heads[c]]).filter(p => p !== undefined);
  }

  function status(state) {
    if (!state.board.some(c => c >= 0)) return 'Won';
    if (bridgeChoices(state).length) return 'Playing';
    if (!state.slots.includes(-1)) return 'Jammed';
    return topPods(state).length ? 'Playing' : 'Stuck';
  }

  function legal(state) {
    if (state.status === 'Won') return [];
    const moves = [];
    if (state.slots.includes(-1)) for (const pod of topPods(state)) moves.push({ type: 'tap', pod });
    for (const choice of bridgeChoices(state)) moves.push({ type: 'bridge', choice: choice.choice });
    return moves;
  }

  function check(state, action) {
    if (!action || state.status === 'Won') return 'finished';
    if (action.type === 'bridge') return bridgeChoices(state).some(c => c.choice === action.choice) ? null : 'bridge-not-ready';
    if (action.type !== 'tap' || !Number.isInteger(action.pod)) return 'unknown-action';
    if (!state.slots.includes(-1)) return 'no-slot';
    return topPods(state).includes(action.pod) ? null : 'not-exposed';
  }

  function settle(state, events) {
    for (let round = 1; ; round++) {
      const available = reach(state).targets;
      const claims = [], claimed = new Uint8Array(state.board.length);
      const occupied = Array.from(state.slots).filter(p => p >= 0).sort((a, b) => state.ages[a] - state.ages[b]);
      for (const pod of occupied) {
        let count = state.remaining[pod];
        for (const cell of available) if (count && !claimed[cell] && state.board[cell] === state.meta.colours[pod]) {
          claimed[cell] = 1; claims.push([pod, cell]); count--;
        }
      }
      if (!claims.length) break;
      for (const [pod, cell] of claims) {
        state.remaining[pod]--; state.board[cell] = OPEN;
        if (events) events.push({ type: 'clear', round, pod, cell });
      }
      state._reach = null;
      for (const pod of occupied) if (state.remaining[pod] === 0) {
        const slot = state.slots.indexOf(pod); state.slots[slot] = -1;
        if (events) events.push({ type: 'complete', round, pod, slot });
      }
    }
    state.status = status(state);
  }

  function step(state, action, captureEvents = false) {
    const reason = check(state, action);
    if (reason) return { accepted: false, reason, state, events: [] };
    const next = clone(state), events = captureEvents ? [] : null;
    if (action.type === 'tap') {
      const column = next.level.tray.findIndex((stack, c) => stack[next.heads[c]] === action.pod);
      next.heads[column]++; const slot = next.slots.indexOf(-1);
      next.slots[slot] = action.pod; next.ages[action.pod] = ++next.serial;
      if (events) events.push({ type: 'commit', pod: action.pod, slot });
    } else {
      const bridge = next.level.hero.choices[action.choice];
      next.board[bridge.cell] = OPEN; next.bridge = action.choice; next._reach = null;
      if (events) events.push({ type: 'bridge', cell: bridge.cell });
    }
    settle(next, events);
    return { accepted: true, state: next, events: events || [] };
  }

  function signature(state) {
    const board = Array.from(state.board, c => c === OPEN ? '.' : c === WALL ? '#' : CHANNELS[c]).join('');
    const active = Array.from(state.slots).filter(p => p >= 0).sort((a, b) => state.ages[a] - state.ages[b]);
    return [board, Array.from(state.remaining).join(','), Array.from(state.heads).join(','),
      Array.from(state.slots).join(','), active.join(','), String(state.bridge), state.status].join('|');
  }

  function validate(level) {
    const errors = [];
    if (!Number.isInteger(level.id) || level.id < 1 || level.w < 7 || level.h < 8 || level.w > 36 || level.h > 36) errors.push('dimensions/id');
    if (level.rowsBottomFirst.length !== level.h || level.rowsBottomFirst.some(r => r.length !== level.w || /[^GPBACD.#]/.test(r))) errors.push('grid');
    if (level.tray.length < 2 || level.tray.length > 6) errors.push('columns');
    const seen = level.tray.flat();
    if (seen.length !== level.pods.length || new Set(seen).size !== seen.length || seen.some(p => !Number.isInteger(p) || p < 0 || p >= level.pods.length)) errors.push('tray partition');
    const cells = new Array(CHANNELS.length).fill(0), counts = cells.slice();
    for (const c of level.rowsBottomFirst.join('')) if (CHANNELS.includes(c)) cells[CHANNELS.indexOf(c)]++;
    for (const pod of level.pods) {
      const c = CHANNELS.indexOf(pod.color);
      if (c < 0 || !Number.isInteger(pod.count) || pod.count < 1) errors.push('pod');
      else counts[c] += pod.count;
    }
    if (cells.some((n, c) => n !== counts[c])) errors.push('exact accounting');
    const n = level.w * level.h;
    if ((level.entries || [Math.floor(level.w / 2)]).some(i => !Number.isInteger(i) || i < 0 || i >= n)) errors.push('entry');
    if (level.hero) for (const choice of level.hero.choices) {
      const grid = level.rowsBottomFirst.join('');
      if (!Number.isInteger(choice.cell) || !Number.isInteger(choice.anchor) || choice.cell < 0 || choice.cell >= n || choice.anchor < 0 || choice.anchor >= n || grid[choice.cell] !== '#' || Math.abs(choice.cell - choice.anchor) !== level.w) errors.push('bridge');
    }
    return errors;
  }

  function assertAccounting(state) {
    const cells = new Array(CHANNELS.length).fill(0), pods = cells.slice();
    for (const c of state.board) if (c >= 0) cells[c]++;
    state.remaining.forEach((n, p) => pods[state.meta.colours[p]] += n);
    if (cells.some((n, c) => n !== pods[c])) throw new Error('Work was created or lost.');
    const live = []; // full tray, not only the exposed heads
    state.level.tray.forEach((stack, c) => live.push(...stack.slice(state.heads[c])));
    live.push(...Array.from(state.slots).filter(p => p >= 0));
    if (new Set(live).size !== live.length || live.some(p => state.remaining[p] <= 0) || state.remaining.some((n, p) => n > 0 && !live.includes(p))) throw new Error('Pod location invariant failed.');
    return true;
  }

  return { CHANNELS, CAPACITY, OPEN, WALL, create, clone, reach, bridgeChoices, topPods,
    legal, check, step, signature, validate, assertAccounting, settle };
});

/* THE FAST TWIN of tools/rounds/engine/rules.cjs (tools/rounds/perf/README.md; the owner, 2026-11-02: generation is too
 * slow). The same rules, line for line, with faster code in the hot paths only: reach() (the BFS of every clear) collects
 * the targets from the reached cells and sorts them by one numeric key, the entry test is a byte mask, canTap tests a pod's
 * place in its column directly, signature() builds the board string from bytes. Every result is the reference's: the
 * reference stays the fingerprinted engine (conformance, export, the game), this file is used by measure.mjs only, and only
 * while the reference files still have the fingerprints in tools/rounds/perf/fast-engine.cjs (else measure.mjs falls back to
 * the reference). tools/rounds/perf/verify.mjs proves the identity. Never edit the rules here: change the reference, then
 * port the change and verify again.
 */
/* The round engine rules (tools/rounds/engine.mjs, product/17_LEVEL_GENERATION_RULES.md): grown from the pilot rules below, changed for 17 §1/§2/§8; a bridge anchor is any orthogonal neighbour of its site (§8, 2026-10-28); hidden cells and the latch with its acorn key (§8, 2026-11-01). */
/* Pilot v9 (v8 rules; v8D widened the depth-mystery domain in validate, v9 has 16 colours, 4 per hero). Grid helpers and v5 settled reference rules (not the live v6 engine). Native exact-colour work and atomic admission, with explicit test
 * adapters: 3/4/5 places, pair co-reservation, reveal on reaching the top, bridge.
 * No DOM, timing or runtime randomness. Presentation is in timeline.cjs.
 */
(function (root, factory) {
  const api = factory();
  module.exports = api;
})(typeof globalThis !== 'undefined' ? globalThis : this, function () {
  'use strict';
  const CHANNELS = 'GPBACDVYOLMRKNSETUFHXWZJ';
  const OPEN = -1, WALL = -2, CAPACITY = 5;
  // view() shows a hidden cell not revealed yet as HIDDEN (17 §8 «Скрытые клетки»); never a board value of the rules
  const HIDDEN = -3;
  const prepared = new WeakMap(), boards = new Map();

  function prepare(level) {
    if (prepared.has(level)) return prepared.get(level);
    const n = level.w * level.h;
    // fast twin: the board's parts (neighbours, board, entry mask, the start reach) are kept per board (size, rows,
    // entries), shared by every level object on it (the candidates of one picture, the games of one test); never changed
    const entries0 = level.entries || [Math.floor(level.w / 2)], rows = level.rowsBottomFirst.join(''), bkey = level.w + 'x' + level.h + '|' + rows + '|' + entries0.join(',');
    let shared = boards.get(bkey);
    if (shared) { boards.delete(bkey); boards.set(bkey, shared); }
    else {
    const neighbours = new Int16Array(n * 4).fill(-1);
    for (let i = 0; i < n; i++) {
      const x = i % level.w, y = Math.floor(i / level.w), at = i * 4;
      neighbours[at] = y > 0 ? i - level.w : -1;
      neighbours[at + 1] = x > 0 ? i - 1 : -1;
      neighbours[at + 2] = x + 1 < level.w ? i + 1 : -1;
      neighbours[at + 3] = y + 1 < level.h ? i + level.w : -1;
    }
    const board = Int8Array.from(rows, c => c === '.' ? OPEN : c === '#' ? WALL : CHANNELS.indexOf(c));
    const entryMask = new Uint8Array(n); for (const e of entries0) if (e >= 0 && e < n) entryMask[e] = 1;
    shared = { neighbours, board, entryMask, start: null };
    boards.set(bkey, shared); if (boards.size > 64) boards.delete(boards.keys().next().value);
    }
    const { neighbours, board, entryMask } = shared;
    const colours = Int8Array.from(level.pods, p => CHANNELS.indexOf(p.color));
    const entries = level.entries || [Math.floor(level.w / 2)];
    const entrySet = new Set(entries);
    const groups = level.pods.map((p,i) => p.group ? level.pods.map((other,j) => other.group===p.group?j:-1).filter(j=>j>=0) : [i]);
    // hidden cells (17 §8 «Скрытые клетки», the owner, 2026-11-01): level.hiddenCells lists cell indices (y*w+x, rows
    // bottom first); hidden is their mask, or null on a level without hidden cells
    let hidden = null;
    if (level.hiddenCells && level.hiddenCells.length) { hidden = new Uint8Array(n); for (const i of level.hiddenCells) hidden[i] = 1; }
    // latch + acorn key (17 §8 «Засов и жёлудь-ключ», the owner, 2026-11-01): level.latches lists {column, depth, key};
    // latchCols[c] are the indices of column c's latches by increasing depth; null on a level without latches
    let latches = null, latchCols = null;
    if (level.latches && level.latches.length) {
      latches = level.latches.map(l => ({ column: l.column, depth: l.depth, key: l.key }));
      latchCols = level.tray.map((_, c) => latches.map((l, i) => l.column === c ? i : -1).filter(i => i >= 0).sort((a, b) => latches[a].depth - latches[b].depth || a - b));
    }
    // fast twin: the entry test as a byte mask, every place of each pod in the tray ([column, index] pairs; one for a valid
    // tray), the board's byte codes for signature()
    const places = level.pods.map(() => []);
    level.tray.forEach((stack, c) => stack.forEach((p, k) => { if (Number.isInteger(p) && p >= 0 && p < places.length) places[p].push(c, k); }));
    const result = { board, neighbours, colours, entries, entrySet, groups, hidden, latches, latchCols, entryMask, places, shared };
    prepared.set(level, result);
    return result;
  }

  // Latch + acorn key (17 §8 «Засов и жёлудь-ключ», the owner, 2026-11-01). A latch is an element of its column's queue,
  // not a pod: no quota, no place. It stands after the first `depth` pods of its column and blocks every pod behind it
  // (they are not top, cannot be tapped, a partner behind it is not exposed, depth mysteries behind it stay hidden).
  // state.latched[i] = 1 while latch i is closed (state.latched is null on a level without latches; state.latchedLeft
  // counts the ones). A closed latch opens at the first moment it is its column's top (heads[column] === depth) and its
  // key cell (an empty '.' cell of the board) is reachable from an entry, reach().distance[key] > 0: the same open path
  // the targets use. Heads only grow and open cells never close, so latched is a function of heads and the board
  // (latched[i] = 0 exactly when heads > depth, or heads === depth and the key is reached): the signature leaves it out.
  // blockAt(state, c): the depth of column c's first closed latch (pods at that index and behind are latched), or Infinity
  function blockAt(state,c) {
    if (!state.latched) return Infinity;
    for (const i of state.meta.latchCols[c]) if (state.latched[i]) return state.meta.latches[i].depth;
    return Infinity;
  }
  // after every change that can make a latch open (a tap's commits; a round of clears, one clear on the timeline, a
  // bridge, each after its cell reveals): every closed latch now top with its key reached opens, in increasing latch
  // order, each as {type:'latchOpen', latch} (with `at` when the caller gives the model time). Opening changes no reach,
  // and a column's next latch stands at least one pod deeper, so one pass finds all. Returns how many opened.
  function openLatches(state,events,at) {
    if (!state.latched || !state.latchedLeft) return 0;
    const ls = state.meta.latches;let opened = 0, distance = null;
    for (let i = 0; i < ls.length; i++) {
      if (!state.latched[i] || state.heads[ls[i].column] !== ls[i].depth) continue;
      distance = distance || reach(state).distance;
      if (distance[ls[i].key] > 0) {
        state.latched[i] = 0; state.latchedLeft--; opened++;
        if (events) events.push(at === undefined ? { type: 'latchOpen', latch: i } : { type: 'latchOpen', at, latch: i });
      }
    }
    return opened;
  }
  // latched recomputed from heads and the board (create, a restored checkpoint): a latch passed by its column's heads is
  // open, the rest closed, then the opening rule (silent: part of that state)
  function syncLatches(state) {
    const ls = state.meta.latches;
    if (!ls) { state.latched = null; state.latchedLeft = 0; return; }
    state.latched = Uint8Array.from(ls, l => state.heads[l.column] > l.depth ? 0 : 1);
    state.latchedLeft = state.latched.reduce((a, b) => a + b, 0);
    openLatches(state, null);
  }
  // the columns whose top is a closed latch (17 §8: with 3 columns at most one at a time, with 4 at most two)
  function blockedColumns(state) {
    if (!state.latched || !state.latchedLeft) return [];
    return state.level.tray.map((_, c) => c).filter(c => state.heads[c] === blockAt(state, c));
  }

  function revealTops(state,events) {
    for(const pod of topPods(state))if(!state.revealed[pod]&&state.level.pods[pod].mystery!=='sealed'){state.revealed[pod]=1;if(events)events.push({type:'reveal',pod});}
  }

  // Hidden cells (17 §8 «Скрытые клетки», the owner, 2026-11-01): a picture cell whose material is fixed but shown as a
  // neutral «?» until it reveals. state.veiled[i] = 1 while cell i is hidden and not revealed yet (state.veiled is null on a
  // level without hidden cells; state.veiledLeft counts the ones). A hidden cell reveals when it gets an orthogonal
  // neighbour that is open and reachable from an entry, or is itself an entry cell: exactly when it becomes a target
  // (reach().targets). Open cells never close, so a target stays a target until it is cleared: the reveal never changes
  // targeting or any other rule, and veiled is a function of the board (veiled[i] = hidden[i] && board[i] >= 0 && i is not
  // a target), so the signature leaves it out. After every board change (a round of clears, one clear on the timeline, a
  // bridge) revealCells reveals every such cell, in increasing cell order, each as {type:'cellReveal', cell} (with `at`
  // when the caller gives the model time). The start reveal in create emits nothing: it is part of the start state.
  function revealCells(state,events,at) {
    if (!state.veiled || !state.veiledLeft) return;
    // fast twin: the targets taken colour by colour (found is sorted by index below, so the order does not count)
    const found = [];
    for (const l of targetsByColour(state)) if (l) for (const i of l) if (state.veiled[i]) found.push(i);
    found.sort((a, b) => a - b);
    for (const i of found) { state.veiled[i] = 0; state.veiledLeft--; if (events) events.push(at === undefined ? { type: 'cellReveal', cell: i } : { type: 'cellReveal', at, cell: i }); }
  }
  // veiled recomputed from the board (create, a restored checkpoint): every hidden colour cell, then the reveal
  function syncCells(state) {
    const hidden = state.meta.hidden;
    if (!hidden) { state.veiled = null; state.veiledLeft = 0; return; }
    state.veiled = Uint8Array.from(hidden, (m, i) => m && state.board[i] >= 0 ? 1 : 0);
    state.veiledLeft = state.veiled.reduce((a, b) => a + b, 0);
    revealCells(state, null);
  }
  // what a player may know about cell i: false only for a hidden cell not revealed yet
  function known(state,i) { return !state.veiled || !state.veiled[i]; }

  function create(level,capacity=level.capacity||CAPACITY) {
    if(!Number.isInteger(capacity)||capacity<3||capacity>5)throw new Error('Capacity must be 3, 4 or 5');
    const meta = prepare(level);
    const state = { level, meta, board: meta.board.slice(),
      remaining: Int16Array.from(level.pods, p => p.count),
      heads: new Int16Array(level.tray.length), slots: new Int16Array(capacity).fill(-1),
      ages: new Int16Array(level.pods.length), revealed: Uint8Array.from(level.pods,p=>p.mystery?0:1), serial: 0, bridge: -1, status: 'Playing', _reach: null };
    // fast twin: the start board's reach is the board's, made once (lent: never changed in place)
    state._reach = meta.shared.start || (meta.shared.start = lend(reach(state), 3));
    // a latch on top at the start whose key is already reached opens silently (before the tops reveal)
    syncLatches(state);
    revealTops(state);
    syncCells(state);
    state.status = status(state);
    return state;
  }

  function clone(state) {
    return { ...state, board: state.board.slice(), remaining: state.remaining.slice(),
      heads: state.heads.slice(), slots: state.slots.slice(), ages: state.ages.slice(), revealed: state.revealed.slice(), veiled: state.veiled ? state.veiled.slice() : null, latched: state.latched ? state.latched.slice() : null, _reach: lend(state._reach, 3) };
  }
  // fast twin: a reach shared with a clone (or a courier's route) is never changed in place by openCell
  function lend(r, bits) { if (r) r.lent = (r.lent || 0) | bits; return r; }

  // fast twin: the same BFS (entries in order, then up, left, right, down), so distance and parent are the reference's;
  // the targets are the colour cells beside a reached cell or on an entry, found from the reached cells, with the same
  // targetDistance, ordered by (targetDistance, index) through one numeric key (td × 65536 + index; boards are ≤ 48×48)
  function reach(state) {
    if (state._reach) return state._reach;
    const { board, meta } = state, n = board.length, nb = meta.neighbours, em = meta.entryMask;
    const distance = new Int16Array(n).fill(-1), parent = new Int16Array(n).fill(-1), queue = new Int16Array(n);
    let head = 0, tail = 0;
    for (const entry of meta.entries) if (board[entry] === OPEN && distance[entry] < 0) {
      distance[entry] = 1; queue[tail++] = entry;
    }
    while (head < tail) {
      const i = queue[head++], next = distance[i] + 1, at = i * 4;
      let j = nb[at];
      if (j >= 0 && board[j] === OPEN && distance[j] < 0) { distance[j] = next; parent[j] = i; queue[tail++] = j; }
      j = nb[at + 1];
      if (j >= 0 && board[j] === OPEN && distance[j] < 0) { distance[j] = next; parent[j] = i; queue[tail++] = j; }
      j = nb[at + 2];
      if (j >= 0 && board[j] === OPEN && distance[j] < 0) { distance[j] = next; parent[j] = i; queue[tail++] = j; }
      j = nb[at + 3];
      if (j >= 0 && board[j] === OPEN && distance[j] < 0) { distance[j] = next; parent[j] = i; queue[tail++] = j; }
    }
    const targetDistance = new Int16Array(n), found = [];
    for (const e of meta.entries) if (board[e] >= 0 && targetDistance[e] === 0) { targetDistance[e] = 1; found.push(e); }
    for (let q = 0; q < tail; q++) {
      const i = queue[q], cand = distance[i] + 1, at = i * 4;
      for (let d = 0; d < 4; d++) {
        const j = nb[at + d];
        if (j >= 0 && board[j] >= 0) {
          const t = targetDistance[j];
          if (t === 0) { targetDistance[j] = cand; found.push(j); } else if (cand < t) targetDistance[j] = cand;
        }
      }
    }
    const keys = new Float64Array(found.length);
    for (let k = 0; k < found.length; k++) keys[k] = targetDistance[found[k]] * 65536 + found[k];
    keys.sort();
    const targets = new Array(keys.length);
    for (let k = 0; k < keys.length; k++) targets[k] = keys[k] % 65536;
    state._reach = { targets, distance, targetDistance, parent, lent: 0, byColour: null };
    return state._reach;
  }

  // fast twin: one cell opens (a courier's clear, a bridge) and reach() is derived from the one before instead of a new BFS.
  // Open cells never close, so distances only fall: the cell gets min(reached open neighbour + 1, 1 on an entry), and
  // from it the distances that fall spread (a FIFO from one source, each cell final at its first update); a colour cell
  // beside a cell whose distance fell gets that distance + 1 as its targetDistance when lower (a new target when it had
  // none); the opened cell leaves the targets. The targets stay ordered by (targetDistance, index). distance and
  // targetDistance are exact (graph distances). parent depends on the BFS's order: it is the reference's BFS, made on
  // demand over the reached cells (distance > 0: exactly the open cells the BFS on this board visits, so the same order).
  // The arrays are reused when nobody else holds them (lent: 1 distance, 2 targetDistance; a clone lends both, a
  // courier's route the distance), else copied.
  let stamp = new Uint32Array(0), tick = 0;
  function parentOf(meta, distance) {
    const n = distance.length, nb = meta.neighbours, parent = new Int16Array(n).fill(-1), seen = new Uint8Array(n), queue = new Int16Array(n);
    let head = 0, tail = 0;
    for (const e of meta.entries) if (distance[e] > 0 && !seen[e]) { seen[e] = 1; queue[tail++] = e; }
    while (head < tail) {
      const i = queue[head++], at = i * 4;
      for (let d = 0; d < 4; d++) { const j = nb[at + d]; if (j >= 0 && distance[j] > 0 && !seen[j]) { seen[j] = 1; parent[j] = i; queue[tail++] = j; } }
    }
    return parent;
  }
  // the targets of a derived reach, from its per-colour lists, on first use: ordered by (targetDistance, index)
  function joinTargets(byColour, td) {
    let n = 0; for (const l of byColour) if (l) n += l.length;
    const keys = new Float64Array(n); let k = 0;
    for (const l of byColour) if (l) for (let q = 0; q < l.length; q++) keys[k++] = td[l[q]] * 65536 + l[q];
    keys.sort();
    const out = new Array(n); for (let q = 0; q < n; q++) out[q] = keys[q] % 65536;
    return out;
  }
  class LazyReach {
    constructor(meta, byColour, distance, targetDistance) { this.distance = distance; this.targetDistance = targetDistance; this.lent = 0; this.byColour = byColour; this.meta = meta; this._targets = null; this._parent = null; }
    get targets() { return this._targets || (this._targets = joinTargets(this.byColour, this.targetDistance)); }
    get parent() { return this._parent || (this._parent = parentOf(this.meta, this.distance)); }
  }
  function openCell(state, cell) {
    const old = state._reach, board = state.board, was = board[cell];
    board[cell] = OPEN;
    if (!old) { state._reach = null; return; }
    const oldLists = old.byColour || targetsByColour({ _reach: old, board: (() => { const b = board.slice(); b[cell] = was; return b; })() });
    const meta = state.meta, nb = meta.neighbours, n = board.length, lent = old.lent || 0;
    const distance = lent & 1 ? old.distance.slice() : old.distance, td = lent & 2 ? old.targetDistance.slice() : old.targetDistance;
    const changed = [];
    let dc = meta.entryMask[cell] ? 1 : 32767;
    for (let d = 0; d < 4; d++) { const j = nb[cell * 4 + d]; if (j >= 0 && distance[j] > 0 && distance[j] + 1 < dc) dc = distance[j] + 1; }
    if (dc < 32767 && (distance[cell] < 0 || dc < distance[cell])) {
      distance[cell] = dc; changed.push(cell);
      for (let q = 0; q < changed.length; q++) {
        const i = changed[q], next = distance[i] + 1, at = i * 4;
        for (let d = 0; d < 4; d++) { const j = nb[at + d]; if (j >= 0 && board[j] === OPEN && (distance[j] < 0 || distance[j] > next)) { distance[j] = next; changed.push(j); } }
      }
    }
    const wasTarget = td[cell] !== 0;
    td[cell] = 0;
    // targets that are new or whose targetDistance falls
    if (stamp.length < n) stamp = new Uint32Array(n);
    if (++tick === 0xffffffff) { stamp.fill(0); tick = 1; }
    const fresh = [];
    for (let q = 0; q < changed.length; q++) {
      const i = changed[q], cand = distance[i] + 1, at = i * 4;
      for (let d = 0; d < 4; d++) {
        const j = nb[at + d];
        if (j >= 0 && board[j] >= 0) {
          const t = td[j];
          if (t === 0 || cand < t) { td[j] = cand; if (stamp[j] !== tick) { stamp[j] = tick; fresh.push(j); } }
        }
      }
    }
    // the per-colour lists: the opened cell leaves its colour's list, each colour with fresh cells merges them in by key;
    // the other colours' lists are shared with the old reach (never changed in place)
    const lists = oldLists.slice();
    if (wasTarget && was >= 0 && lists[was]) lists[was] = lists[was].filter(c => c !== cell);
    if (fresh.length) {
      const keys = new Float64Array(fresh.length);
      for (let k = 0; k < fresh.length; k++) keys[k] = board[fresh[k]] * 4294967296 + td[fresh[k]] * 65536 + fresh[k];
      keys.sort();
      let k = 0;
      while (k < keys.length) {
        const col = Math.floor(keys[k] / 4294967296), base = col * 4294967296, ol = lists[col] || [], out = [];
        for (let q = 0; q < ol.length; q++) {
          const c = ol[q];
          if (stamp[c] === tick) continue;
          const key = base + td[c] * 65536 + c;
          while (k < keys.length && keys[k] < key) out.push(keys[k++] % 65536);
          out.push(c);
        }
        while (k < keys.length && keys[k] < base + 4294967296) out.push(keys[k++] % 65536);
        lists[col] = out;
      }
    }
    state._reach = new LazyReach(meta, lists, distance, td);
  }

  function routeTo(state,cell) {
    const r=reach(state);if(state.meta.entrySet.has(cell))return [cell];
    let best=-1;
    for(let d=0;d<4;d++){const j=state.meta.neighbours[cell*4+d];if(j>=0&&r.distance[j]>0&&(best<0||r.distance[j]<r.distance[best]))best=j;}
    if(best<0)throw Error('No route to target');
    const path=[cell];for(let i=best;i>=0;i=r.parent[i])path.push(i);return path.reverse();
  }

  // fast twin: the targets of each colour in the targets' order (an array by colour), kept on the reach object (a function
  // of the board, like the reach itself)
  function targetsByColour(state) {
    const r = reach(state);
    if (r.byColour) return r.byColour;
    const out = [], t = r.targets, b = state.board;
    for (let k = 0; k < t.length; k++) { const c = t[k], col = b[c]; (out[col] || (out[col] = [])).push(c); }
    return (r.byColour = out);
  }

  // fast twin: routeTo(state, cell).length without building the route (an entry: 1; else the distance of the nearest
  // reached neighbour, the first in direction order among equals, + 1), with routeTo's error
  function routeLength(state,cell) {
    const r=reach(state);if(state.meta.entryMask[cell])return 1;
    let best=-1;
    for(let d=0;d<4;d++){const j=state.meta.neighbours[cell*4+d];if(j>=0&&r.distance[j]>0&&(best<0||r.distance[j]<r.distance[best]))best=j;}
    if(best<0)throw Error('No route to target');
    return r.distance[best]+1;
  }

  function bridgeChoices(state) {
    if (!state.level.hero || state.bridge >= 0) return [];
    const open = reach(state).distance;
    return state.level.hero.choices.map((c, i) => ({ ...c, choice: i }))
      .filter(c => state.board[c.cell] === WALL && open[c.anchor] > 0);
  }

  function topPods(state) {
    if (state.latched && state.latchedLeft) return state.level.tray.map((stack, c) => state.heads[c] < blockAt(state, c) ? stack[state.heads[c]] : undefined).filter(p => p !== undefined);
    return state.level.tray.map((stack, c) => stack[state.heads[c]]).filter(p => p !== undefined);
  }

  function status(state) {
    if (!state.board.some(c => c >= 0)) return 'Won';
    if (bridgeChoices(state).length) return 'Playing';
    if (!state.slots.includes(-1)) return 'Jammed';
    return topPods(state).some(p=>canTap(state,p)===null) ? 'Playing' : 'Stuck';
  }

  // Repack only for an accepted linked admission; keep current left-to-right order.
  function compactSlots(state) {
    const held=Array.from(state.slots).filter(p=>p>=0);
    state.slots.fill(-1);state.slots.set(held);
  }

  // fast twin: "pod is one of topPods" tested on the pod's own places (a pod is a top when its column's head is its index,
  // before the column's first closed latch); a single pod has no partner to test
  function isTop(state,pod) {
    const pl = state.meta.places[pod];
    for (let k = 0; k < pl.length; k += 2) { const c = pl[k], at = pl[k + 1]; if (state.heads[c] === at && at < blockAt(state, c)) return true; }
    return false;
  }
  function canTap(state,pod) {
    if(!Number.isInteger(pod)||pod<0||pod>=state.level.pods.length)return 'unknown-pod';
    const group=state.meta.groups[pod];
    if(!isTop(state,pod))return 'not-exposed';
    if(group.length>1){const heads=topPods(state);
     if(group.some(p=>!heads.includes(p)&&!state.level.tray.some((stack,c)=>{const at=stack.indexOf(p);return at>=state.heads[c]&&at<blockAt(state,c)&&stack.slice(state.heads[c],at).every(q=>group.includes(q));})))return 'partner-not-exposed';}
    let free=0;const sl=state.slots;for(let i=0;i<sl.length;i++)if(sl[i]<0)free++;
    if(free<group.length)return group.length>1?'pair-needs-two-slots':'no-slot';
    return null;
  }

  function legal(state) {
    if (state.status !== 'Playing') return [];
    const moves = [],seen=new Set();
    for(const pod of topPods(state))if(canTap(state,pod)===null){const first=state.meta.groups[pod][0];if(!seen.has(first)){seen.add(first);moves.push({type:'tap',pod:first});}}
    for (const choice of bridgeChoices(state)) moves.push({ type: 'bridge', choice: choice.choice });
    return moves;
  }

  function check(state, action) {
    if (!action || state.status !== 'Playing') return 'finished';
    if (action.type === 'bridge') return bridgeChoices(state).some(c => c.choice === action.choice) ? null : 'bridge-not-ready';
    if (action.type !== 'tap' || !Number.isInteger(action.pod)) return 'unknown-action';
    return canTap(state,action.pod);
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
      const routes=events?claims.map(([,cell])=>routeTo(state,cell)):null;
      for (let ci=0;ci<claims.length;ci++) {
        const [pod,cell]=claims[ci];
        state.remaining[pod]--; state.board[cell] = OPEN;
        if (events) events.push({ type: 'clear', round, pod, cell, route:routes[ci] });
      }
      state._reach = null;
      revealCells(state, events);
      // latches whose key this round's clears reached (their pods behind then reveal by the depth rule)
      if (openLatches(state, events)) revealTops(state, events);
      for(const pod of occupied)if(state.remaining[pod]===0&&state.slots.includes(pod)){
        const group=state.meta.groups[pod];
        if(group.some(p=>state.remaining[p]>0))continue;
        for(const member of group){const slot=state.slots.indexOf(member);if(slot>=0){state.slots[slot]=-1;if(events)events.push({type:'complete',round,pod:member,slot});}}
      }
    }
    state.status = status(state);
  }

  function step(state, action, captureEvents = false) {
    const reason = check(state, action);
    if (reason) return { accepted: false, reason, state, events: [] };
    const next = clone(state), events = captureEvents ? [] : null;
    if (action.type === 'tap') {
      if(next.meta.groups[action.pod].length>1)compactSlots(next);
      for(const pod of next.meta.groups[action.pod]){
        const column=next.level.tray.findIndex((stack,c)=>stack[next.heads[c]]===pod);
        next.heads[column]++;const slot=next.slots.indexOf(-1);
        next.slots[slot]=pod;next.ages[pod]=++next.serial;
        if(events)events.push({type:'commit',pod,slot});
        if(!next.revealed[pod]){next.revealed[pod]=1;if(events)events.push({type:'reveal',pod});}
      }
      // a latch the tap brought to its column's top with its key already reached
      openLatches(next, events);
    } else {
      const bridge = next.level.hero.choices[action.choice];
      next.board[bridge.cell] = OPEN; next.bridge = action.choice; next._reach = null;
      if (events) events.push({ type: 'bridge', cell: bridge.cell });
      revealCells(next, events);
      openLatches(next, events);
    }
    revealTops(next,events);
    settle(next, events);
    return { accepted: true, state: next, events: events || [] };
  }

  // fast twin: the board string from bytes (OPEN '.', WALL '#', a colour its letter), the same string
  const CODE = new Uint8Array(CHANNELS.length + 2); CODE[0] = 35; CODE[1] = 46; for (let c = 0; c < CHANNELS.length; c++) CODE[c + 2] = CHANNELS.charCodeAt(c);
  function signature(state) {
    const b = state.board, buf = Buffer.allocUnsafe(b.length); for (let i = 0; i < b.length; i++) buf[i] = CODE[b[i] + 2];
    const board = buf.latin1Slice(0, b.length);
    const active = Array.from(state.slots).filter(p => p >= 0).sort((a, b) => state.ages[a] - state.ages[b]);
    return [board, Array.from(state.remaining).join(','), Array.from(state.heads).join(','),
      Array.from(state.slots).join(','), active.join(','), Array.from(state.revealed).join(''), String(state.bridge), state.status].join('|');
  }

  // Public observation: never exposes a hidden colour, including for bots.
  function view(state){
    return {board:state.veiled?Array.from(state.board,(c,i)=>state.veiled[i]?HIDDEN:c):Array.from(state.board),heads:Array.from(state.heads),slots:Array.from(state.slots),remaining:Array.from(state.remaining),
      colours:state.level.pods.map((p,i)=>state.revealed[i]?p.color:null),groups:state.level.pods.map(p=>p.group||null),
      bridge:state.bridge,status:state.status,...(state.latched?{latched:Array.from(state.latched)}:{})};
  }
  function observation(state){return JSON.stringify(view(state));}
  function queueEmpty(state){return state.level.tray.every((s,c)=>state.heads[c]===s.length);}

  function validate(level) {
    const errors = [];
    if (!Number.isInteger(level.id) || level.id < 1 || level.w < 7 || level.h < 8 || level.w > 48 || level.h > 48) errors.push('dimensions/id');
    if (level.rowsBottomFirst.length !== level.h || level.rowsBottomFirst.some(r => r.length !== level.w || new RegExp('[^'+CHANNELS+'.#]').test(r))) errors.push('grid');
    if (level.tray.length < 2 || level.tray.length > 6) errors.push('columns');
    if(level.capacity!==undefined&&![4,5].includes(level.capacity))errors.push('capacity');
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
    const groups=new Map();
    level.pods.forEach((p,i)=>{if(p.group){if(!groups.has(p.group))groups.set(p.group,[]);groups.get(p.group).push(i);}});
    for(const members of groups.values()){
      if(members.length!==2)errors.push('only pairs in this pilot');
      const columns=members.map(p=>level.tray.findIndex(s=>s.includes(p)));
      if(new Set(columns).size!==members.length)errors.push('pair in same column');
      const depths=members.map((p,i)=>level.tray[columns[i]].indexOf(p));
      if(new Set(depths).size!==1)errors.push('pair at different initial depths');
    }
    for(const p of level.pods)if(p.mystery!==undefined&&![true,'depth','sealed'].includes(p.mystery))errors.push('unknown mystery mode');
    const hidden=level.pods.map((p,i)=>p.mystery?i:-1).filter(i=>i>=0);
    // v8D: 1-3 depth capsules (each reveals on reaching the top); up to 6 since the v9 playtest (L10 has five). Sealed
    // capsules keep the v6 two-world domain.
    if(hidden.some(i=>level.pods[i].mystery!=='depth')){if(hidden.length!==2||level.pods[hidden[0]].count!==level.pods[hidden[1]].count||level.pods[hidden[0]].color===level.pods[hidden[1]].color)errors.push('two equal-count mystery colours required');}
    else if(hidden.length>6)errors.push('at most six depth mysteries');
    const n = level.w * level.h;
    // hidden cells (17 §8, 2026-11-01): sorted unique colour-cell indices, never on a level with hidden pods
    if (level.hiddenCells !== undefined) {
      const hc = level.hiddenCells, grid = level.rowsBottomFirst.join('');
      if (!Array.isArray(hc) || !hc.length || hc.some((c, k) => !Number.isInteger(c) || c < 0 || c >= n || (k > 0 && c <= hc[k - 1]))) errors.push('hidden cells');
      else if (hc.some(c => !CHANNELS.includes(grid[c]))) errors.push('hidden cell not a colour cell');
      if (level.pods.some(p => p.mystery)) errors.push('hidden cells with hidden pods');
    }
    if ((level.entries || [Math.floor(level.w / 2)]).some(i => !Number.isInteger(i) || i < 0 || i >= n)) errors.push('entry');
    // latch + acorn key (17 §8, 2026-11-01): each {column, depth, key}: an existing column, at least one pod behind it,
    // distinct depths in one column, the key an empty board cell (not an entry), each key its own; then the cheap
    // necessary check that every key can be reached at all (latchKeysBlocked)
    if (level.latches !== undefined) {
      const ls = level.latches, grid = level.rowsBottomFirst.join(''), entries = level.entries || [Math.floor(level.w / 2)];
      if (!Array.isArray(ls) || !ls.length) errors.push('latches');
      else {
        let ok = true;
        for (const l of ls) {
          const stack = l && Number.isInteger(l.column) ? level.tray[l.column] : null;
          if (!stack || !Number.isInteger(l.depth) || l.depth < 0 || l.depth >= stack.length) { errors.push('latch'); ok = false; continue; }
          if (ls.some(o => o !== l && o.column === l.column && o.depth === l.depth)) { errors.push('latch'); ok = false; }
          if (!Number.isInteger(l.key) || l.key < 0 || l.key >= n || grid[l.key] !== '.' || entries.includes(l.key) || ls.some(o => o !== l && o.key === l.key)) { errors.push('latch key'); ok = false; }
        }
        if (ok && errors.indexOf('tray partition') < 0 && errors.indexOf('grid') < 0) {
          if (latchKeysBlocked(level).length) errors.push('latch key unreachable');
          // at the start no more columns blocked than §8 allows (3 columns: 1, 4: 2, 5: 3)
          else if (startBlocked(level) > Math.max(1, level.tray.length - 2)) errors.push('latches block too many columns');
        }
      }
    }
    if (level.hero) for (const choice of level.hero.choices) {
      const grid = level.rowsBottomFirst.join('');
      if (!Number.isInteger(choice.cell) || !Number.isInteger(choice.anchor) || choice.cell < 0 || choice.cell >= n || choice.anchor < 0 || choice.anchor >= n || grid[choice.cell] !== '#' || !(Math.abs(choice.cell - choice.anchor) === level.w || (Math.abs(choice.cell - choice.anchor) === 1 && Math.floor(choice.cell / level.w) === Math.floor(choice.anchor / level.w)))) errors.push('bridge');
    }
    return errors;
  }

  // A cheap necessary check for latches (17 §8: a key never needs a material wholly locked behind its own latch, latches
  // never form a cycle): starting with every latch closed, a material all of whose pods stand behind closed latches (at or
  // after the depth of their column's first closed latch) is blocked and its cells are walls; every other colour cell
  // counts as clearable, open cells and bridge sites as passable; each closed latch whose key that reaches is opened, until
  // nothing changes. Returns the latches never opened (empty: the keys may all be reached; the solver decides the rest).
  function latchKeysBlocked(level) {
    const ls = level.latches || [], w = level.w, n = level.w * level.h, grid = level.rowsBottomFirst.join('');
    const sites = new Set(level.hero ? level.hero.choices.map(c => c.cell) : []), entries = level.entries || [Math.floor(level.w / 2)];
    const open = new Uint8Array(ls.length);
    for (;;) {
      const first = level.tray.map((_, c) => Math.min(Infinity, ...ls.filter((l, i) => !open[i] && l.column === c).map(l => l.depth)));
      const free = new Set();level.tray.forEach((stack, c) => stack.forEach((p, k) => { if (k < first[c]) free.add(level.pods[p].color); }));
      const pass = i => grid[i] === '.' || sites.has(i) || (CHANNELS.includes(grid[i]) && grid[i] !== '.' && grid[i] !== '#' && free.has(grid[i]));
      const seen = new Uint8Array(n), q = [];
      for (const e of entries) if (e >= 0 && e < n && pass(e) && !seen[e]) { seen[e] = 1; q.push(e); }
      while (q.length) { const i = q.pop(), x = i % w;
        for (const j of [i - w, i + w, x > 0 ? i - 1 : -1, x < w - 1 ? i + 1 : -1]) if (j >= 0 && j < n && !seen[j] && pass(j)) { seen[j] = 1; q.push(j); } }
      let changed = false;
      ls.forEach((l, i) => { if (!open[i] && seen[l.key]) { open[i] = 1; changed = true; } });
      if (!changed) break;
    }
    return ls.map((_, i) => i).filter(i => !open[i]);
  }
  // the columns blocked at the start: a latch at depth 0 whose key the start board does not reach
  function startBlocked(level) {
    const s = { level, meta: prepare(level), board: prepare(level).board.slice(), _reach: null };
    const d = reach(s).distance;return new Set((level.latches || []).filter(l => l.depth === 0 && !(d[l.key] > 0)).map(l => l.column)).size;
  }

  function assertAccounting(state,allowReturning=false) {
    const cells = new Array(CHANNELS.length).fill(0), pods = cells.slice();
    for (const c of state.board) if (c >= 0) cells[c]++;
    state.remaining.forEach((n, p) => pods[state.meta.colours[p]] += n);
    if (cells.some((n, c) => n !== pods[c])) throw new Error('Work was created or lost.');
    const live = []; // full tray, not only the exposed heads
    state.level.tray.forEach((stack, c) => live.push(...stack.slice(state.heads[c])));
    live.push(...Array.from(state.slots).filter(p => p >= 0));
    if(new Set(live).size!==live.length||live.some(p=>state.remaining[p]<0||state.remaining[p]===0&&(!state.slots.includes(p)||(!allowReturning&&state.meta.groups[p].every(q=>state.remaining[q]===0))))||state.remaining.some((n,p)=>n>0&&!live.includes(p)))throw new Error('Pod location invariant failed.');
    return true;
  }

  return { CHANNELS, CAPACITY, OPEN, WALL, HIDDEN, openCell, routeLength, lend, targetsByColour, revealCells, syncCells, known, blockAt, openLatches, syncLatches, blockedColumns, latchKeysBlocked, create, clone, reach, routeTo, bridgeChoices, topPods,
    legal, check, canTap, compactSlots, step, signature, observation, view, queueEmpty, validate, assertAccounting, settle };
});

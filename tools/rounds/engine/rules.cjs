/* The round engine rules (tools/rounds/engine.mjs, product/17_LEVEL_GENERATION_RULES.md): grown from the pilot rules below, changed for 17 §1/§2/§8; a bridge anchor is any orthogonal neighbour of its site (§8, 2026-10-28); hidden cells and the latch with its acorn key (§8, 2026-11-01); the Drop water channel and the Twig wooden shutters (§8, 2026-11-05). */
/* Pilot v9 (v8 rules; v8D widened the depth-mystery domain in validate, v9 has 16 colours, 4 per hero). Grid helpers and v5 settled reference rules (not the live v6 engine). Native exact-colour work and atomic admission, with explicit test
 * adapters: 3/4/5 places, pair co-reservation, reveal on reaching the top, bridge.
 * No DOM, timing or runtime randomness. Presentation is in timeline.cjs.
 */
(function (root, factory) {
  const api = factory();
  if (typeof module !== 'undefined' && module.exports) module.exports = api;
  else root.BloomPilotRules = api;
})(typeof globalThis !== 'undefined' ? globalThis : this, function () {
  'use strict';
  const CHANNELS = 'GPBACDVYOLMRKNSETUFHXWZJ';
  const OPEN = -1, WALL = -2, CAPACITY = 5;
  // view() shows a hidden cell not revealed yet as HIDDEN (17 §8 «Скрытые клетки»); never a board value of the rules
  const HIDDEN = -3;
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
    // water channel (17 §8 «Drop Водный канал», the owner, 2026-11-05): level.channel = {spring, exits: [{cell, route, rideUs}]};
    // null on a level without one
    let channel = null;
    if (level.channel) channel = { spring: level.channel.spring, exits: level.channel.exits.map(e => ({ cell: e.cell, route: e.route.slice(), rideUs: e.rideUs })) };
    // wooden shutters (17 §8 «Twig Деревянные створки», the owner, 2026-11-05): level.shutters = [{cells, material, count}];
    // shutCell[i] is the shutter over cell i or -1, shutStart[m] the cells of material m on the start board; null without
    let shutters = null, shutCell = null, shutStart = null;
    if (level.shutters && level.shutters.length) {
      shutters = level.shutters.map(s => ({ cells: s.cells.slice(), material: CHANNELS.indexOf(s.material), count: s.count }));
      shutCell = new Int16Array(n).fill(-1); shutters.forEach((s, i) => { for (const c of s.cells) if (c >= 0 && c < n) shutCell[c] = i; });
      shutStart = new Int32Array(CHANNELS.length); for (const c of board) if (c >= 0) shutStart[c]++;
    }
    const result = { board, neighbours, colours, entries, entrySet, groups, hidden, latches, latchCols, channel, shutters, shutCell, shutStart };
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

  // Wooden shutters (17 §8 «Twig Деревянные створки», the owner, 2026-11-05). A shutter lies over a rectangle of colour
  // cells and counts the work of one exact material actually done anywhere since the start (a clear, a burst of the game's
  // booster): while it is closed its cells are no targets (their work waits; they are colour cells, so never a passage
  // either). state.shut[i] = 1 while shutter i is closed (null on a level without shutters; state.shutLeft counts the
  // ones). Work done of material m = its start cells − its cells on the board now, so shut is a function of the board
  // (shut[i] = 0 exactly when that is at least count): the signature leaves it out. shutterLeft(state, i) is the count
  // still shown on it (0 once open).
  function materialLeft(state) { const left = new Int32Array(CHANNELS.length); for (const c of state.board) if (c >= 0) left[c]++; return left; }
  function shutterLeft(state,i) {
    const s = state.meta.shutters[i]; if (!state.shut[i]) return 0;
    return Math.max(0, s.count - (state.meta.shutStart[s.material] - materialLeft(state)[s.material]));
  }
  // after every change of the board's colour cells (a round of clears, one clear on the timeline, a burst; first of all,
  // before the cell reveals): every closed shutter whose count is reached opens, in increasing shutter order, each as
  // {type:'shutterOpen', shutter} (with `at` when the caller gives the model time); its cells become targets (the reach is
  // dropped). Returns how many opened.
  function openShutters(state,events,at) {
    if (!state.shut || !state.shutLeft) return 0;
    const ss = state.meta.shutters; let opened = 0, left = null;
    for (let i = 0; i < ss.length; i++) {
      if (!state.shut[i]) continue;
      left = left || materialLeft(state);
      if (state.meta.shutStart[ss[i].material] - left[ss[i].material] >= ss[i].count) {
        state.shut[i] = 0; state.shutLeft--; opened++;
        if (events) events.push(at === undefined ? { type: 'shutterOpen', shutter: i } : { type: 'shutterOpen', at, shutter: i });
      }
    }
    if (opened) state._reach = null;
    return opened;
  }
  // shut recomputed from the board (create, a restored checkpoint), silently; drops the reach (targets depend on it)
  function syncShutters(state) {
    const ss = state.meta.shutters;
    if (!ss) { state.shut = null; state.shutLeft = 0; return; }
    const left = materialLeft(state);
    state.shut = Uint8Array.from(ss, s => state.meta.shutStart[s.material] - left[s.material] >= s.count ? 0 : 1);
    state.shutLeft = state.shut.reduce((a, b) => a + b, 0);
    state._reach = null;
  }
  // whether cell i is under a closed shutter
  function shutAt(state,i) { return !!(state.shut && state.meta.shutCell[i] >= 0 && state.shut[state.meta.shutCell[i]]); }

  // Water channel (17 §8 «Drop Водный канал», the owner, 2026-11-05). The spring and both exits are empty '.' cells; each
  // exit keeps its route (spring … exit, drawn over the cubes) and its ride time rideUs. The channel is ready at the first
  // moment the spring is reached by an open path from an active entry (reach().distance[spring] > 0: the same search the
  // targets use); then the player chooses an exit (the command {type:'channel', exit}: free, no place, no pause; the
  // status stays Playing while it can be chosen). state.channel is the chosen exit or -1 (in the signature on a channel
  // level). The chosen exit becomes an additional active entry: reach() starts from the entries in their order and then
  // from it, so every walker departs from the nearest active entry by path (ties: the earlier source in that order). A
  // walker whose route starts at the exit rides the channel: rideUs more out and rideUs more back (timeline.cjs). The
  // unchosen exit never becomes an entry (its pocket may join the open area by ordinary clearing). state.springReached
  // (1 once the spring is reached; a function of the board and the choice) only times the event {type:'channelReady'}
  // (with `at` on the timeline): after every board change, after the cell reveals and latches.
  function channelChoices(state) {
    const ch = state.meta.channel;
    if (!ch || state.channel >= 0 || !(reach(state).distance[ch.spring] > 0)) return [];
    return ch.exits.map((e, i) => ({ exit: i, cell: e.cell }));
  }
  function readyChannel(state,events,at) {
    const ch = state.meta.channel;
    if (!ch || state.springReached || !(reach(state).distance[ch.spring] > 0)) return false;
    state.springReached = 1;
    if (events && state.channel < 0) events.push(at === undefined ? { type: 'channelReady' } : { type: 'channelReady', at });
    return true;
  }
  // springReached recomputed from the board and the choice (create, a restored checkpoint), silently
  function syncChannel(state) {
    if (!state.meta.channel) return;
    state.springReached = 0; readyChannel(state, null);
  }
  // the ride of a walker on this route: the chosen exit's rideUs (in model ms) when the route starts at the chosen exit
  function rideOf(state,route) {
    const ch = state.meta.channel;
    return ch && state.channel >= 0 && route[0] === ch.exits[state.channel].cell ? ch.exits[state.channel].rideUs / 1000 : 0;
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
    const found = [];
    for (const i of reach(state).targets) if (state.veiled[i]) found.push(i);
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
    // water channel and shutters (17 §8, 2026-11-05): no exit chosen; every shutter closed (each counts at least 1)
    if (meta.channel) { state.channel = -1; state.springReached = 0; }
    syncShutters(state);
    // a latch on top at the start whose key is already reached opens silently (before the tops reveal)
    syncLatches(state);
    revealTops(state);
    syncCells(state);
    syncChannel(state);
    state.status = status(state);
    return state;
  }

  function clone(state) {
    return { ...state, board: state.board.slice(), remaining: state.remaining.slice(),
      heads: state.heads.slice(), slots: state.slots.slice(), ages: state.ages.slice(), revealed: state.revealed.slice(), veiled: state.veiled ? state.veiled.slice() : null, latched: state.latched ? state.latched.slice() : null, ...(state.shut ? { shut: state.shut.slice() } : {}), _reach: state._reach };
  }

  function reach(state) {
    if (state._reach) return state._reach;
    const { board, meta } = state, n = board.length;
    const distance = new Int16Array(n).fill(-1), parent = new Int16Array(n).fill(-1), queue = new Int16Array(n);
    let head = 0, tail = 0;
    for (const entry of meta.entries) if (board[entry] === OPEN && distance[entry] < 0) {
      distance[entry] = 1; queue[tail++] = entry;
    }
    // the chosen channel exit: an active entry after the level's entries (17 §8 «Drop Водный канал»)
    if (meta.channel && state.channel >= 0) { const x = meta.channel.exits[state.channel].cell; if (board[x] === OPEN && distance[x] < 0) { distance[x] = 1; queue[tail++] = x; } }
    while (head < tail) {
      const i = queue[head++], next = distance[i] + 1;
      for (let d = 0; d < 4; d++) {
        const j = meta.neighbours[i * 4 + d];
        if (j >= 0 && board[j] === OPEN && distance[j] < 0) { distance[j] = next; parent[j] = i; queue[tail++] = j; }
      }
    }
    const targets = [], targetDistance = new Int16Array(n);
    for (let i = 0; i < n; i++) if (board[i] >= 0) {
      // a cell under a closed shutter is no target (17 §8 «Twig Деревянные створки»)
      if (state.shut && meta.shutCell[i] >= 0 && state.shut[meta.shutCell[i]]) continue;
      let best = meta.entrySet.has(i) ? 1 : 32767;
      for (let d = 0; d < 4; d++) {
        const j = meta.neighbours[i * 4 + d];
        if (j >= 0 && distance[j] > 0) best = Math.min(best, distance[j] + 1);
      }
      if (best < 32767) { targets.push(i); targetDistance[i] = best; }
    }
    targets.sort((a, b) => targetDistance[a] - targetDistance[b] || a - b);
    state._reach = { targets, distance, targetDistance, parent };
    return state._reach;
  }

  function routeTo(state,cell) {
    const r=reach(state);if(state.meta.entrySet.has(cell))return [cell];
    let best=-1;
    for(let d=0;d<4;d++){const j=state.meta.neighbours[cell*4+d];if(j>=0&&r.distance[j]>0&&(best<0||r.distance[j]<r.distance[best]))best=j;}
    if(best<0)throw Error('No route to target');
    const path=[cell];for(let i=best;i>=0;i=r.parent[i])path.push(i);return path.reverse();
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
    if (channelChoices(state).length) return 'Playing';
    if (!state.slots.includes(-1)) return 'Jammed';
    return topPods(state).some(p=>canTap(state,p)===null) ? 'Playing' : 'Stuck';
  }

  // Repack only for an accepted linked admission; keep current left-to-right order.
  function compactSlots(state) {
    const held=Array.from(state.slots).filter(p=>p>=0);
    state.slots.fill(-1);state.slots.set(held);
  }

  function canTap(state,pod) {
    if(!Number.isInteger(pod)||pod<0||pod>=state.level.pods.length)return 'unknown-pod';
    const heads=topPods(state),group=state.meta.groups[pod];
    if(!heads.includes(pod))return 'not-exposed';
    if(group.some(p=>!heads.includes(p)&&!state.level.tray.some((stack,c)=>{const at=stack.indexOf(p);return at>=state.heads[c]&&at<blockAt(state,c)&&stack.slice(state.heads[c],at).every(q=>group.includes(q));})))return 'partner-not-exposed';
    if(state.slots.filter(p=>p<0).length<group.length)return group.length>1?'pair-needs-two-slots':'no-slot';
    return null;
  }

  function legal(state) {
    if (state.status !== 'Playing') return [];
    const moves = [],seen=new Set();
    for(const pod of topPods(state))if(canTap(state,pod)===null){const first=state.meta.groups[pod][0];if(!seen.has(first)){seen.add(first);moves.push({type:'tap',pod:first});}}
    for (const choice of bridgeChoices(state)) moves.push({ type: 'bridge', choice: choice.choice });
    for (const choice of channelChoices(state)) moves.push({ type: 'channel', exit: choice.exit });
    return moves;
  }

  function check(state, action) {
    if (!action || state.status !== 'Playing') return 'finished';
    if (action.type === 'bridge') return bridgeChoices(state).some(c => c.choice === action.choice) ? null : 'bridge-not-ready';
    if (action.type === 'channel') return channelChoices(state).some(c => c.exit === action.exit) ? null : 'channel-not-ready';
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
      // shutters whose count this round's clears reached (17 §8), before the cell reveals
      openShutters(state, events);
      revealCells(state, events);
      // latches whose key this round's clears reached (their pods behind then reveal by the depth rule)
      if (openLatches(state, events)) revealTops(state, events);
      // the channel whose spring this round's clears reached
      readyChannel(state, events);
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
    } else if (action.type === 'channel') {
      // the chosen exit becomes an active entry (17 §8 «Drop Водный канал»)
      next.channel = action.exit; next._reach = null;
      if (events) events.push({ type: 'channelOpen', exit: action.exit });
      revealCells(next, events);
      openLatches(next, events);
    } else {
      const bridge = next.level.hero.choices[action.choice];
      next.board[bridge.cell] = OPEN; next.bridge = action.choice; next._reach = null;
      if (events) events.push({ type: 'bridge', cell: bridge.cell });
      revealCells(next, events);
      openLatches(next, events);
      readyChannel(next, events);
    }
    revealTops(next,events);
    settle(next, events);
    return { accepted: true, state: next, events: events || [] };
  }

  function signature(state) {
    const board = Array.from(state.board, c => c === OPEN ? '.' : c === WALL ? '#' : CHANNELS[c]).join('');
    const active = Array.from(state.slots).filter(p => p >= 0).sort((a, b) => state.ages[a] - state.ages[b]);
    return [board, Array.from(state.remaining).join(','), Array.from(state.heads).join(','),
      Array.from(state.slots).join(','), active.join(','), Array.from(state.revealed).join(''), String(state.bridge),
      // the chosen channel exit, only on a channel level (17 §8, 2026-11-05)
      ...(state.meta.channel ? [String(state.channel)] : []), state.status].join('|');
  }

  // Public observation: never exposes a hidden colour, including for bots.
  function view(state){
    return {board:state.veiled?Array.from(state.board,(c,i)=>state.veiled[i]?HIDDEN:c):Array.from(state.board),heads:Array.from(state.heads),slots:Array.from(state.slots),remaining:Array.from(state.remaining),
      colours:state.level.pods.map((p,i)=>state.revealed[i]?p.color:null),groups:state.level.pods.map(p=>p.group||null),
      bridge:state.bridge,status:state.status,...(state.latched?{latched:Array.from(state.latched)}:{}),
      ...(state.meta.channel?{channel:state.channel,channelReady:channelChoices(state).length>0}:{}),...(state.shut?{shutters:state.meta.shutters.map((_,i)=>shutterLeft(state,i))}:{})};
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
    // water channel (17 §8, 2026-11-05): the spring and two exits, each an empty non-entry cell of its own (never a latch
    // key); each exit's route goes from the spring to the exit by orthogonal steps without a repeat (it may pass over any
    // cell: it is drawn over the cubes) and its ride rideUs is whole milliseconds, 1 ms–10 s; then the cheap check that the
    // spring can be reached at all (objectsBlocked)
    const grid0 = level.rowsBottomFirst.join(''), entries0 = level.entries || [Math.floor(level.w / 2)], keys0 = (Array.isArray(level.latches) ? level.latches : []).map(l => l && l.key);
    let objectsOk = errors.indexOf('tray partition') < 0 && errors.indexOf('grid') < 0 && errors.indexOf('latch') < 0 && errors.indexOf('latch key') < 0 && errors.indexOf('latches') < 0;
    const emptyCell = c => Number.isInteger(c) && c >= 0 && c < n && grid0[c] === '.' && !entries0.includes(c) && !keys0.includes(c);
    const beside = (a, b) => Math.abs(a - b) === level.w || (Math.abs(a - b) === 1 && Math.floor(a / level.w) === Math.floor(b / level.w));
    if (level.channel !== undefined) {
      const ch = level.channel;
      if (!ch || typeof ch !== 'object' || !emptyCell(ch.spring) || !Array.isArray(ch.exits) || ch.exits.length !== 2) { errors.push('channel'); objectsOk = false; }
      else {
        const cells = [ch.spring];
        for (const e of ch.exits) {
          if (!e || !emptyCell(e.cell) || cells.includes(e.cell)) { errors.push('channel exit'); objectsOk = false; continue; }
          cells.push(e.cell);
          const r = e.route;
          if (!Array.isArray(r) || r.length < 2 || r[0] !== ch.spring || r[r.length - 1] !== e.cell || new Set(r).size !== r.length
            || r.some((c, k) => !Number.isInteger(c) || c < 0 || c >= n || (k > 0 && !beside(c, r[k - 1])))) { errors.push('channel route'); objectsOk = false; }
          if (!Number.isInteger(e.rideUs) || e.rideUs < 1000 || e.rideUs > 10000000 || e.rideUs % 1000 !== 0) { errors.push('channel ride'); objectsOk = false; }
        }
      }
    }
    // wooden shutters (17 §8, 2026-11-05): each a full rectangle of colour cells (sorted unique indices; never an entry or a
    // hidden cell, never under another shutter), one material code and a count ≥ 1; then the cheap check that every count
    // can be reached without the cells under the shutters still closed (objectsBlocked: no shutter waits for itself or in
    // a cycle)
    if (level.shutters !== undefined) {
      const ss = level.shutters, under = new Set(), hiddenSet = new Set(Array.isArray(level.hiddenCells) ? level.hiddenCells : []);
      if (!Array.isArray(ss) || !ss.length) { errors.push('shutters'); objectsOk = false; }
      else for (const sh of ss) {
        const cs = sh && sh.cells;
        if (!Array.isArray(cs) || !cs.length || cs.some((c, k) => !Number.isInteger(c) || c < 0 || c >= n || (k > 0 && c <= cs[k - 1]))) { errors.push('shutter'); objectsOk = false; continue; }
        const xs = cs.map(c => c % level.w), ys = cs.map(c => Math.floor(c / level.w)), x0 = Math.min(...xs), x1 = Math.max(...xs), y0 = Math.min(...ys), y1 = Math.max(...ys);
        if ((x1 - x0 + 1) * (y1 - y0 + 1) !== cs.length || cs.some(c => !CHANNELS.includes(grid0[c]) || entries0.includes(c) || hiddenSet.has(c) || under.has(c))) { errors.push('shutter'); objectsOk = false; }
        for (const c of cs) under.add(c);
        if (typeof sh.material !== 'string' || sh.material.length !== 1 || !CHANNELS.includes(sh.material) || !Number.isInteger(sh.count) || sh.count < 1) { errors.push('shutter'); objectsOk = false; }
      }
    }
    if (objectsOk && (level.channel !== undefined || level.shutters !== undefined)) {
      const blocked = objectsBlocked(level);
      if (level.channel !== undefined && !blocked.spring) errors.push('channel spring unreachable');
      if (blocked.shutters.length) errors.push('shutter count unreachable');
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
  function latchKeysBlocked(level) { return objectsBlocked(level).latches; }
  // The same cheap fixed point for every board object (17 §8, 2026-11-05: a shutter never waits for its own cells or in a
  // cycle, the channel's spring can be reached): a cell under a closed shutter is a wall; once the spring is flooded both
  // exits flood too (either may be chosen); a closed shutter opens when the flooded cells of its material (not under a
  // closed shutter) are at least its count. Without shutters and a channel it is exactly the latch check above. Returns
  // {latches, shutters} never opened and whether the spring was reached.
  function objectsBlocked(level) {
    const ls = level.latches || [], ss = level.shutters || [], ch = level.channel || null, w = level.w, n = level.w * level.h, grid = level.rowsBottomFirst.join('');
    const sites = new Set(level.hero ? level.hero.choices.map(c => c.cell) : []), entries = level.entries || [Math.floor(level.w / 2)];
    const open = new Uint8Array(ls.length), opened = new Uint8Array(ss.length), cover = new Int32Array(n).fill(-1);
    ss.forEach((s, k) => { for (const c of s.cells) if (c >= 0 && c < n) cover[c] = k; });
    let spring = false;
    for (;;) {
      const first = level.tray.map((_, c) => Math.min(Infinity, ...ls.filter((l, i) => !open[i] && l.column === c).map(l => l.depth)));
      const free = new Set();level.tray.forEach((stack, c) => stack.forEach((p, k) => { if (k < first[c]) free.add(level.pods[p].color); }));
      const pass = i => (cover[i] < 0 || opened[cover[i]]) && (grid[i] === '.' || sites.has(i) || (CHANNELS.includes(grid[i]) && grid[i] !== '.' && grid[i] !== '#' && free.has(grid[i])));
      const seen = new Uint8Array(n), q = [];
      const sources = ch && spring ? [...entries, ...ch.exits.map(e => e.cell)] : entries;
      for (const e of sources) if (e >= 0 && e < n && pass(e) && !seen[e]) { seen[e] = 1; q.push(e); }
      while (q.length) { const i = q.pop(), x = i % w;
        for (const j of [i - w, i + w, x > 0 ? i - 1 : -1, x < w - 1 ? i + 1 : -1]) if (j >= 0 && j < n && !seen[j] && pass(j)) { seen[j] = 1; q.push(j); } }
      let changed = false;
      ls.forEach((l, i) => { if (!open[i] && seen[l.key]) { open[i] = 1; changed = true; } });
      if (ch && !spring && seen[ch.spring]) { spring = true; changed = true; }
      // a flooded cell is never under a closed shutter, so it counts as clearable work of its material
      ss.forEach((s, k) => { if (opened[k]) return; let got = 0; for (let i = 0; i < n; i++) if (seen[i] && grid[i] === s.material) got++;
        if (got >= s.count) { opened[k] = 1; changed = true; } });
      if (!changed) break;
    }
    return { latches: ls.map((_, i) => i).filter(i => !open[i]), shutters: ss.map((_, k) => k).filter(k => !opened[k]), spring };
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

  return { CHANNELS, CAPACITY, OPEN, WALL, HIDDEN, revealCells, syncCells, known, blockAt, openLatches, syncLatches, blockedColumns, latchKeysBlocked, objectsBlocked,
    openShutters, syncShutters, shutterLeft, shutAt, channelChoices, readyChannel, syncChannel, rideOf, create, clone, reach, routeTo, bridgeChoices, topPods,
    legal, check, canTap, compactSlots, step, signature, observation, view, queueEmpty, validate, assertAccounting, settle };
});

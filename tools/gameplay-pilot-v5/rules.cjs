/* Pilot v5. Native exact-colour work and atomic admission, with explicit test
 * adapters: 3/4/5 places, pair co-reservation, reveal on reaching the top, bridge.
 * No DOM, timing or runtime randomness. Presentation is in timeline.cjs.
 */
(function (root, factory) {
  const api = factory();
  if (typeof module !== 'undefined' && module.exports) module.exports = api;
  else root.BloomPilotRules = api;
})(typeof globalThis !== 'undefined' ? globalThis : this, function () {
  'use strict';
  const CHANNELS = 'GPBACDV';
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
    const groups = level.pods.map((p,i) => p.group ? level.pods.map((other,j) => other.group===p.group?j:-1).filter(j=>j>=0) : [i]);
    const result = { board, neighbours, colours, entries, entrySet, groups };
    prepared.set(level, result);
    return result;
  }

  function revealTops(state,events) {
    for(const pod of topPods(state))if(!state.revealed[pod]){state.revealed[pod]=1;if(events)events.push({type:'reveal',pod});}
  }

  function create(level,capacity=level.capacity||CAPACITY) {
    if(!Number.isInteger(capacity)||capacity<3||capacity>5)throw new Error('Capacity must be 3, 4 or 5');
    const meta = prepare(level);
    const state = { level, meta, board: meta.board.slice(),
      remaining: Int16Array.from(level.pods, p => p.count),
      heads: new Int16Array(level.tray.length), slots: new Int16Array(capacity).fill(-1),
      ages: new Int16Array(level.pods.length), revealed: Uint8Array.from(level.pods,p=>p.mystery?0:1), serial: 0, bridge: -1, status: 'Playing', _reach: null };
    revealTops(state);
    state.status = status(state);
    return state;
  }

  function clone(state) {
    return { ...state, board: state.board.slice(), remaining: state.remaining.slice(),
      heads: state.heads.slice(), slots: state.slots.slice(), ages: state.ages.slice(), revealed: state.revealed.slice(), _reach: state._reach };
  }

  function reach(state) {
    if (state._reach) return state._reach;
    const { board, meta } = state, n = board.length;
    const distance = new Int16Array(n).fill(-1), parent = new Int16Array(n).fill(-1), queue = new Int16Array(n);
    let head = 0, tail = 0;
    for (const entry of meta.entries) if (board[entry] === OPEN && distance[entry] < 0) {
      distance[entry] = 1; queue[tail++] = entry;
    }
    while (head < tail) {
      const i = queue[head++], next = distance[i] + 1;
      for (let d = 0; d < 4; d++) {
        const j = meta.neighbours[i * 4 + d];
        if (j >= 0 && board[j] === OPEN && distance[j] < 0) { distance[j] = next; parent[j] = i; queue[tail++] = j; }
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
    return state.level.tray.map((stack, c) => stack[state.heads[c]]).filter(p => p !== undefined);
  }

  function status(state) {
    if (!state.board.some(c => c >= 0)) return 'Won';
    if (bridgeChoices(state).length) return 'Playing';
    if (!state.slots.includes(-1)) return 'Jammed';
    return topPods(state).some(p=>canTap(state,p)===null) ? 'Playing' : 'Stuck';
  }

  function canTap(state,pod) {
    if(!Number.isInteger(pod)||pod<0||pod>=state.level.pods.length)return 'unknown-pod';
    const heads=topPods(state),group=state.meta.groups[pod];
    if(!heads.includes(pod))return 'not-exposed';
    if(group.some(p=>!heads.includes(p)))return 'partner-not-exposed';
    if(state.slots.filter(p=>p<0).length<group.length)return group.length>1?'pair-needs-two-slots':'no-slot';
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
      for(const pod of next.meta.groups[action.pod]){
        const column=next.level.tray.findIndex((stack,c)=>stack[next.heads[c]]===pod);
        next.heads[column]++;const slot=next.slots.indexOf(-1);
        next.slots[slot]=pod;next.ages[pod]=++next.serial;
        if(events)events.push({type:'commit',pod,slot});
        if(!next.revealed[pod]){next.revealed[pod]=1;if(events)events.push({type:'reveal',pod});}
      }
    } else {
      const bridge = next.level.hero.choices[action.choice];
      next.board[bridge.cell] = OPEN; next.bridge = action.choice; next._reach = null;
      if (events) events.push({ type: 'bridge', cell: bridge.cell });
    }
    revealTops(next,events);
    settle(next, events);
    return { accepted: true, state: next, events: events || [] };
  }

  function signature(state) {
    const board = Array.from(state.board, c => c === OPEN ? '.' : c === WALL ? '#' : CHANNELS[c]).join('');
    const active = Array.from(state.slots).filter(p => p >= 0).sort((a, b) => state.ages[a] - state.ages[b]);
    return [board, Array.from(state.remaining).join(','), Array.from(state.heads).join(','),
      Array.from(state.slots).join(','), active.join(','), Array.from(state.revealed).join(''), String(state.bridge), state.status].join('|');
  }

  // Public observation: never exposes a hidden colour, including for bots.
  function view(state){
    return {board:Array.from(state.board),heads:Array.from(state.heads),slots:Array.from(state.slots),remaining:Array.from(state.remaining),
      colours:state.level.pods.map((p,i)=>state.revealed[i]?p.color:null),groups:state.level.pods.map(p=>p.group||null),
      bridge:state.bridge,status:state.status};
  }
  function observation(state){return JSON.stringify(view(state));}
  function queueEmpty(state){return state.level.tray.every((s,c)=>state.heads[c]===s.length);}

  function validate(level) {
    const errors = [];
    if (!Number.isInteger(level.id) || level.id < 1 || level.w < 7 || level.h < 8 || level.w > 36 || level.h > 36) errors.push('dimensions/id');
    if (level.rowsBottomFirst.length !== level.h || level.rowsBottomFirst.some(r => r.length !== level.w || /[^GPBACDV.#]/.test(r))) errors.push('grid');
    if (level.tray.length < 2 || level.tray.length > 6) errors.push('columns');
    if(level.capacity!==undefined&&![3,4,5].includes(level.capacity))errors.push('capacity');
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
    const hidden=level.pods.map((p,i)=>p.mystery?i:-1).filter(i=>i>=0);
    if(hidden.length){if(hidden.length!==2||level.pods[hidden[0]].count!==level.pods[hidden[1]].count||level.pods[hidden[0]].color===level.pods[hidden[1]].color)errors.push('two equal-count mystery colours required');}
    const n = level.w * level.h;
    if ((level.entries || [Math.floor(level.w / 2)]).some(i => !Number.isInteger(i) || i < 0 || i >= n)) errors.push('entry');
    if (level.hero) for (const choice of level.hero.choices) {
      const grid = level.rowsBottomFirst.join('');
      if (!Number.isInteger(choice.cell) || !Number.isInteger(choice.anchor) || choice.cell < 0 || choice.cell >= n || choice.anchor < 0 || choice.anchor >= n || grid[choice.cell] !== '#' || Math.abs(choice.cell - choice.anchor) !== level.w) errors.push('bridge');
    }
    return errors;
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

  return { CHANNELS, CAPACITY, OPEN, WALL, create, clone, reach, routeTo, bridgeChoices, topPods,
    legal, check, canTap, step, signature, observation, view, queueEmpty, validate, assertAccounting, settle };
});

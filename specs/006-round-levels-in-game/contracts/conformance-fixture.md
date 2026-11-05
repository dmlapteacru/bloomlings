# Contract: Conformance Fixture (`round-conformance.v1`)

One JSON file per round level: `content/game/rounds/conformance/L####.json`. It is written only by the JS exporter
(`tools/rounds/export.mjs`), which runs the reference round engine named by `engine`. The C# conformance tests and
`rounds validate` replay the same inputs in `Bloomlings.Core.Rounds` and must match it exactly (spec 006 FR-030–FR-033).

## Shape

```json
{
  "format": "round-conformance.v1",
  "number": 5,
  "definitionVersion": 1,
  "engine": {"rules": "…", "timeline": "…", "palette": "…", "clock": "…"},
  "solution": {
    "windows": [{"command": {"type": "tap", "pod": 2, "atUs": 0}, "events": "sha256:…", "signature": "sha256:…"}],
    "outcome": "Won", "endUs": 84500000, "finalSignature": "sha256:…"
  },
  "random": {
    "policy": "17§5 extended", "seedBase": 1, "runs": 100, "bridge": 1,
    "games": [{"seed": 7920, "outcome": "Won", "endUs": 61250000, "finalSignature": "sha256:…"}],
    "wins": 45
  },
  "owner": [{
    "source": "product/playtests/pilot-v12-owner-2026-10-27/results.json", "attempt": "…id…",
    "commands": [{"type": "tap", "pod": 1, "atUs": 787200}],
    "outcome": "won", "endUs": 90514000, "finalSignature": "sha256:…"
  }]
}
```

A command (in `solution.windows[].command` and `owner[].commands`) is a tap `{"type": "tap", "pod", "atUs"}`, a bridge
`{"type": "bridge", "choice", "atUs"}` or, since 2026-11-05, the water channel's exit `{"type": "channel", "exit", "atUs"}`.

## Canonical event log

Each event is one line, UTF-8, `\n`-terminated. Times are integer µs. Events of one window (from a command up to the
next command, or to the end) are hashed together with SHA-256.

| Event | Line |
|---|---|
| commit | `t commit pod=<p> slot=<s>` |
| reveal | `t reveal pod=<p> mode=<depth\|sealed>` (the commit's time; a depth reveal of a pod exposed by a latch that a clear opened carries that clear's time) |
| bridge | `t bridge cell=<c>` |
| cellReveal | `t cellReveal cell=<c>` (hidden cells, rules 17 §8, 2026-11-01: right after the clear or bridge that revealed it, at that change's time; several in increasing cell order) |
| latchOpen | `t latchOpen latch=<i>` (latch + acorn key, rules 17 §8, 2026-11-01: `i` indexes the level's `latches`; at the time of the change that opened it — a commit, after the commit and sealed-reveal lines, a clear, after that clear's cellReveal lines, or a bridge, after its cellReveal lines; several in increasing latch order; the depth reveals of the pods it exposes follow it at the same time, also when a clear opened it) |
| channelReady | `t channelReady` (the Drop water channel, rules 17 §8, 2026-11-05, spec 006 FR-069: its spring was reached by an open path from an active entry for the first time; at the time of the clear or bridge that reached it, after that change's cellReveal and latchOpen lines; never at the start) |
| channelOpen | `t channelOpen exit=<i>` (the command `channel exit=<i>`'s own line at the commit's time; `i` indexes `board.channel.exits`; the cellReveal and latchOpen lines the new entry causes follow at that time) |
| shutterOpen | `t shutterOpen shutter=<i>` (the wooden shutters, rules 17 §8, 2026-11-05, spec 006 FR-070: `i` indexes `board.shutters`; at the time of the clear that made the work of its material reach its count, right after that clear's line and before its cellReveal lines; several in increasing order) |
| depart | `t depart id=<j> pod=<p> cell=<c> arrive=<µs> clear=<µs> done=<µs> route=<len>` (a route that starts at the chosen channel exit includes its ride twice: `arrive` = start + ride + walk, `done` = clear + walk back + ride) |
| arrive | `t arrive id=<j>` |
| clear | `t clear id=<j> cell=<c>` |
| return | `t return id=<j>` |
| complete | `t complete pod=<p>` |

Order within a moment is the reference's emission order (research R3). The reference's `assign` event is omitted,
because it always precedes its `depart`.

## State signature

The reference's `signature(state)` string, extended with the timeline:
`board|remaining|heads|slots|activeByAge|revealed|bridge|status|now|steps`
- `steps` is `at:type:id-or-pod` joined by commas, in step order;
- `now` is in µs;
- columns appear only if a booster changed them, which never happens in fixtures.
- latches add nothing: a latch is open exactly when its column's head has passed it, or stands at it with its key
  cell reached from an entry, so it follows from `heads` and `board`.
- hidden cells add nothing: which hidden cells are still unrevealed follows from the board (a hidden colour cell that is
  not a target), so the signature is the same as without them.
- the water channel adds its chosen exit (or -1) after `bridge`, on a channel level only:
  `…|revealed|bridge|channel|status|now|steps`; whether its spring is reached follows from the board and the choice.
- shutters add nothing: a shutter is open exactly when the work done of its material (its start cells minus its cells on
  the board) is at least its count, so it follows from `board`.

The signature is hashed with SHA-256.

## Replay rules

- **Solution**: commit each command at its `atUs`. Before a command, advance the timeline to `atUs`; it must be
  accepted. After the last command, advance until nothing is pending. Compare the events hash and the signature per
  window, then the outcome and `endUs`.
- **Random games**: the policy of research R5 with the recorded seed. Compare outcome, `endUs`, final signature.
  `wins` must equal the count of won games, and the proof's share.
  - A bridge level's class test plays its best bridge only (rules 17 §5 as amended on 2026-10-30): `random.bridge`
    (optional, equal to the proof's `randomTest.bridge`) is the only bridge choice the bot may build. Every other bridge
    action is left out of the moves before the choice (reference `measure.mjs randomTest`'s `keep`, C#
    `RoundRandomPolicy.KeepBridge`). Absent: every bridge may be built.
- **Owner attempts**:
  - advance to each `atUs` and commit; a refused command is recorded as refused, as in the pilot;
  - stop where the attempt ended (won, jammed, or left at `endUs`);
  - compare the outcome and the final signature;
  - the outcome must also equal the one recorded in the results file. A difference caused by µs rounding is reported
    as a fixture error, not hidden.

## Failure report

`L0005 solution window 7 (tap pod 11 at 26962200 µs): events differ` followed by both canonical logs of that window,
first differing line marked. `rounds validate` exits non-zero.

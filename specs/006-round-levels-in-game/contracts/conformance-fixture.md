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

## Canonical event log

Each event is one line, UTF-8, `\n`-terminated. Times are integer µs. Events of one window (from a command up to the
next command, or to the end) are hashed together with SHA-256.

| Event | Line |
|---|---|
| commit | `t commit pod=<p> slot=<s>` |
| reveal | `t reveal pod=<p> mode=<depth\|sealed>` |
| bridge | `t bridge cell=<c>` |
| depart | `t depart id=<j> pod=<p> cell=<c> arrive=<µs> clear=<µs> done=<µs> route=<len>` |
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

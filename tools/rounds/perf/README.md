# Faster measures (the owner, 2026-11-02)

Level generation spent almost all its time playing games: the random test (100–400 games), the calm test (200–400), the
quick estimate, the opening test, the forced share, the bridge measures and the strategies. This folder makes
`measure.mjs` faster **without changing any result**: every measure returns, byte for byte, what it returned before.

| Part | What it does | Why the result cannot change |
| --- | --- | --- |
| `fast-rules.cjs`, `fast-timeline.cjs`, `fast-idle.cjs` | The **fast twin** of `tools/rounds/engine/` (rules, timeline, idle adapter): the same rules line for line, faster code in the hot paths | `verify.mjs` plays every game on both engines and compares every event, state, step list and courier route |
| `fast-engine.cjs` | Loads the twin only while the reference files have the SHA-256 it was verified against (`VERIFIED`) | A changed reference engine makes `measure.mjs` play on the reference again (with a warning) until the change is ported and verified |
| `engines.mjs` | The engine choice of `measure.mjs` and the workers; `ROUNDS_ENGINE=reference` forces the reference | — |
| `games.mjs`, `pool.mjs`, `worker.mjs` | The games of a test on worker threads (`ROUNDS_THREADS`, default cores − 1; `0` = one thread) | Each game depends only on the level, the test's parameters and its own seed; results are put back in index order |
| `early.mjs` | Early stopping of the random test against the full test, on placed levels and bank candidates | Off by default (see below) |
| `bench.mjs`, `verify.mjs`, `identity.mjs` | Timings with a result-by-result comparison; the identity proof | — |

The reference engine stays the only fingerprinted engine (conformance fixtures, `export.mjs`, the game's C# port); it is
not changed here, so the engine fingerprints in levels, proofs and fixtures stay valid. Never change rules in the twin:
change the reference, port the change to the `fast-*.cjs` files, run `node tools/rounds/perf/verify.mjs --fixtures`, then
put the new SHA-256 into `VERIFIED` (the test `test/perf.test.mjs` fails until then).

## What the twin does differently (only how, never what)

- `reach()`: the same BFS (so `distance` and `parent` are the reference's), targets collected from the reached cells and
  ordered by one numeric key (targetDistance × 65536 + index) instead of a full scan and a comparator sort.
- `openCell()`: a single opened cell (a courier's clear, a bridge) derives the next reach from the last one: distances
  only fall, so they spread from the opened cell; the targets are kept per colour and merged; `parent` (which depends on
  the BFS order) is the reference BFS over the reached cells, made only when a route is read.
- A courier's route is made when someone reads it (`couriers`, `snapshot()`): from the board of its departure (the board
  now with the cells opened since closed again); its length, which the times need, is the distance of its nearest reached
  neighbour + 1, as `routeTo` gives.
- The step list is kept sorted by binary insertion (the order is total, so it is the list the reference's sort gives);
  pending checks and releases are flags; dispatch filters the frontier once per colour and skips pods that cannot leave
  and already wait for a check; `assert()` runs on a restored checkpoint and with `ROUNDS_FAST_ASSERT=1` (it only throws).
- Per board (size, rows, entries): neighbours, the board bytes, the entry mask and the start reach, shared by all level
  objects on it (all games of a test, all candidates of a picture).
- `signature()` builds the board string from bytes.

## Early stopping of the random test

`randomTest(level, {early: true})` (or `ROUNDS_RANDOM_EARLY=1`) stops at the first checkpoint (every 20 games from 40,
and at 100 and 400) where the 95 % Wilson interval crosses no class border (with `zone: [lo, hi]` also neither end of the
zone); runs, wins, share and class are those of the games played, and the result carries `early`. It is **off by
default**: the game's proofs accept only 100 or 400 runs (`core/src/Bloomlings.Content/Schemas/round-proof.schema.json`,
`RoundSetValidator`: "the interval of 100 runs crosses a class border, so the test needs 400 runs"), so a level measured
with early stopping cannot be exported. Using it for placed levels needs the owner's change of 17 §5 and of the game's
proof contract; until then it is for screening and studies (`early.mjs`).

## Commands

```sh
node tools/rounds/perf/bench.mjs --levels 20,27,50,26 --out /tmp/new.json --compare /tmp/old.json   # times + identical results
node tools/rounds/perf/verify.mjs --levels 1-50 --bank 40 --fixtures    # the identity proof (about an hour)
node tools/rounds/perf/early.mjs --levels 1-50 --bank all --out /tmp/early.json
ROUNDS_THREADS=0 node tools/rounds/levels.mjs …    # one thread; ROUNDS_ENGINE=reference: the reference engine
```

When `rounds.mjs run --jobs N` starts N processes, each starts its own worker threads; results stay the same, but
`ROUNDS_THREADS=1` for the children avoids running more threads than cores.

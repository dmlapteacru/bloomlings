# Golden replay cases

Each `*.golden.json` pins the exact outcome of one command sequence on one level (SC-005). The same files run in
`dotnet test` (`core/tests/Bloomlings.Core.Tests/Golden/GoldenReplayTests.cs`) and in the Unity EditMode tests
(`client/Assets/Bloomlings/Tests/EditMode/GoldenReplayEditModeTests.cs`), so .NET, Mono and IL2CPP builds must agree.

## Format

Canonical JSON (sorted keys, two-space indent), read and written by `Bloomlings.Content.Golden.GoldenCase`:

| Field | Meaning |
|---|---|
| `name`, `description` | What the case shows |
| `definition` | A `level-definition.v1` document |
| `picture` | The `base-picture.v1` document it references |
| `commands` | Command texts in order: `tap:<podId>`, `extra_slot`, `shuffle`, `return:<slot>`, `burst:<variant>`, `restart` |
| `contentVersion`, `shuffleNodeBudget` | The `SessionOptions` of the replay (Shuffle salt and node budget, R10) |
| `expectedEventsDigest` | FNV-1a 64 of the replay log, as 16 hex digits |
| `expectedStateHash` | Final Zobrist state hash, as 16 hex digits |
| `expectedStatus` | `playing`, `won`, `jammed` or `stuck` |

The replay log has one line per command (`> tap:p1`, or `> tap:p1 rejected:NoFreeSlot`), followed by the canonical
line of each event (`Bloomlings.Core.Simulation.EventText`). Each case has a matching `*.events.txt` with that log, so
a rules change shows up as a readable diff.

## Changing rules

A failing golden case means the rules changed. If the change is intended:

1. Run `BLOOMLINGS_GOLDEN_REGEN=1 dotnet test core/Bloomlings.sln --filter GoldenReplayTests`.
2. Review the diff of every `*.events.txt` and golden file.
3. Commit them together with the rules change.

Never regenerate to make an unexplained failure go away.

## Round level cases (spec 006)

`rounds/rounds-*.golden.json` pin replays on the round rules (`Bloomlings.Core.Rounds`): saved solutions, a hasty
route with overlapping work, and play with all four boosters in live time. They live in `rounds/` so that the readers
of `*.golden.json` above (the Unity EditMode tests, `playtest/check`) never meet them. Each holds the reference engine
definition it was made from (`source`), the commands with their model time in µs on the timeline's own clock
(`tap pod=3 at=0`, `bridge choice=1 at=…`, `extra-slot at=…`, `return pod=4 at=…`, `shuffle at=…`,
`burst code=G at=…`) and the expected `expectedStatus`, `expectedEndUs`, `expectedSignatureSha256` (the extended
signature of `RoundEvents.Signature`) and `expectedEventsSha256` (the replay log). The matching
`rounds-*.events.txt` is that log: the canonical event lines of `contracts/conformance-fixture.md` (plus the booster
lines `extra-slot`, `recall`, `shuffle`, `burst`, `burst-clear`) and one `> command` line per command.

`GoldenReplayTests.ReplayRound` runs them, and the same `BLOOMLINGS_GOLDEN_REGEN=1` command rewrites their expected
values and logs. Without boosters a change here also shows in the conformance tests against the reference engine
(`core/tests/Bloomlings.Core.Tests/Rounds/RoundReferenceTests.cs`); a rules change that moves away from the reference is a
bug, not a reason to regenerate.

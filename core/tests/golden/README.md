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

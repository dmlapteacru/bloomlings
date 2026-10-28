# Acceptance of spec 006 (T063)

Run on 2026-10-28 on `claude/great-darwin-6qrpj8` (after the Unity merge ef828ca2). Baseline:
[`baseline.md`](baseline.md).

## Commands of quickstart.md

| Command | Result |
|---|---|
| `dotnet build core/Bloomlings.sln` | 0 warnings, 0 errors |
| `dotnet test core/Bloomlings.sln` | Core 350/350, Solver 22/22, Pipeline 79/80, Content 3054/3058 (1 skipped), Generator 141/145. The 8 failures are all legacy ones from the baseline (Pipeline 1, Content 3 curated dev levels, Generator 4); none is new. |
| `dotnet test client/DotnetCheck/...` | 740 pass, 9 fail: exactly the 9 legacy failures of the baseline |
| `node --test tools/rounds/test/*.test.mjs` | 12/12 |
| `rounds validate` | 20 levels, 20 pictures, 0 errors, 0 warnings |
| `dotnet run --project playtest/check` | ALL OK (every round solution played frame by frame at ×1, ×2 and uneven frames, ending on the rules' state) |
| `playtest/preview -- --rounds` | all checks pass (run by the level-screen stream; frames `rounds-*.png`) |
| `android-apk.yml` (manual) | full playtest APK built (run 40615801583); tester APK: see T055 |

## Success criteria

| Criterion | Status | Evidence |
|---|---|---|
| SC-001 conformance of L1–L20 | Met | `rounds validate` and `RoundConformanceTests`: 20 solutions window by window, 4100 random games, win counts equal to the proofs |
| SC-002 the owner's 23 attempts | Met | all 23 replay to the same refusals, outcome, end time and final signature |
| SC-003 the owner's phone check | Waiting for the owner | the APK is published in the transfer log; `ownerConfirmedAt` is empty |
| SC-004 the owner's decisions of 2026-10-27 | Met in the playtest, to be seen on the phone | `preview --rounds` checks: no icons on cubes, live-time clearing, front link tubes with split colours, speed ×2, Auto ×2 with the HUD gone, intro cards and reminders, reveal glow, symbols mode |
| SC-005 a deliberate break fails | Met | `AChangedReleaseLag_FailsAndNamesTheLevelAndTheWindow` (299 ms release lag) |
| SC-006 a wave transfers without hand edits | Met for L1–L20 | export, import, validate, publish, log by scripts; the only fixes were build files of the APKs, not content |
| SC-007 same outcome at every speed and after a restore | Met | `RoundDeterminismTests`; `playtest/check` at ×1, ×2 and uneven frames |
| SC-008 boosters in live work, accounting reconciles | Met in tests, to be seen on the phone | `RoundBoostersTests`, golden replays with all four boosters (L4, L19), the jam card in `preview --rounds` |
| SC-009 a 48×48 board plays smoothly | Met in the recorded harness, to be measured on the phone | `--perf` round 48×48 segment: mean 1.6 ms, p95 1.5 ms per recorded frame (not device times) |

## Open after the owner's check

- T055: the owner's confirmation on the phone (`transfer-log.mjs confirmed 1 20`).
- T056: finish the transfer skill from that run.
- Unity: everything compiles and passes under DotnetCheck; the Editor and device checks are listed in the Unity stream's
  report (Play opens Level 1, board fit, tinted walkers, tubes, Auto, boosters, win, symbols switch, frame time).

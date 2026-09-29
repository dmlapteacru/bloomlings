# Quickstart validation (T156)

**Feature**: 001-core-game-mvp · **Guide**: `quickstart.md` §1–8 · **Run on**: 2026-09-29, in a Linux cloud container
without Unity, devices or live services.

| § | Scenario | Result | Evidence |
|---|---|---|---|
| 1 | Core rules and determinism | Pass | `dotnet test core/Bloomlings.sln`: Core 115, Content 199, Solver 14, Generator 10, Pipeline 14; all green. It covers the pre-lock rule tests, the FsCheck properties and the golden corpus. |
| 2 | Solver and level validation | Pass on the committed levels | `pictures validate`: 107 pictures, 13 usable, 0 errors. `validate --defs content/curated`: 10 levels, 10 solved, 0 errors; 10 warnings, all the provisional readability approval. `score`: 0 violations. `content/catalog/` is still empty; see T095 and T153. |
| 3 | Generating a band | Pass (preview) | Levels 11–100 generated from draft pictures into `content/work/preview/`: 84 of 90 levels, and only `picture-approved` fails. L95–L100 need more pictures (T094). The byte-identical rerun is covered by `GeneratorTests.SameProfileAndSeed_GiveByteIdenticalDefinitions`. |
| 4 | Publishing and content stability | Pass | `daily generate --count 3`: 3 entries. `publish --catalog content/curated --daily … --allow-draft`: 10 levels, 13 pictures, 3 daily entries, 3 packs. `diff`: an unchanged catalog passes (exit 0); a level whose seed changed without a `definitionVersion` bump fails (exit 1). The manifest schema and pack hashes are covered by `PackWriterTests`. |
| 5 | Client in the Unity Editor | Not run (needs Unity 6.3) | The client scripts compile against stubs, and 107 engine-free EditMode tests pass (`client/DotnetCheck`). The Editor walkthrough is still open (T025, T052, T067). |
| 6 | Determinism on devices | Not run (needs devices) | The IL2CPP player is ready: **Tools/Bloomlings/Device Tests**, or `android-apk.yml` with target `golden-replays` (T151, T152). |
| 7 | Services and monetization | Logic passes; live services not run | Unit tests cover these rows: cloud merge (`SaveMergeTests`, `CloudSaveAndLeaderboardTests`), leaderboard encoding and queue (`LeaderboardScoreTests`), Daily Challenge (`MilestoneAndDailyChallengeTests`), store ledger, ads policy and daily reward (US6 tests). Still open: live UGS, store, ads and Firebase projects, and the SDK compile. |
| 8 | Playtest measures | Not run (needs players) | Template in `checklists/playtest-results.md` (T155). |

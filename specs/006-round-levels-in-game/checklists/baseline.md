# Baseline before spec 006 code (T001)

Date: 2026-10-27. SDK: .NET 10.0.113 (Ubuntu archive package `dotnet-sdk-10.0`; `builds.dotnet.microsoft.com` is
blocked from the cloud container, NuGet works). Commit: b072e9af (constitution 1.1.0), before any `Rounds` code.

`dotnet build core/Bloomlings.sln`: succeeded, 0 warnings.

`dotnet test core/Bloomlings.sln`:

| Test project | Passed | Failed | Total |
|---|---:|---:|---:|
| Bloomlings.Core.Tests | 148 | 0 | 148 |
| Bloomlings.Solver.Tests | 22 | 0 | 22 |
| Bloomlings.Pipeline.Tests | 20 | 1 | 21 |
| Bloomlings.Content.Tests | 2978 | 4 | 2982 |
| Bloomlings.Generator.Tests | 141 | 4 | 145 |

The 9 failures exist before spec 006 and are not caused by it (they concern legacy content and contracts):

- Pipeline: `CompleteRealHundredStillChecksTheDifficultyMix` (expected 0, was 2).
- Content: `Level_IsCanonicalAndLoads` for `curated/dev/level-dev-001.json`, `-002`, `-003`;
  `TheEmbeddedSchemas_AreTheContractsUnchanged("player-save.schema.json")` (the embedded schema and spec 001's
  contract differ in `dependentRequired`).
- Generator: `ExpansionPictures_AreApproved_AndKeptOutUntilTheirVariantJoins`,
  `ThePicturePicker_DrawsEverySizeAboutEvenly`, `MissingClassesDoNotMoveTriplePracticeToALaterSuperHard(True)`,
  `DailyPictures_AreTheChallengesOwn_AndTheLevelsNeverShowThem`.

Spec 006 must not add failures: every later run is compared with this table.

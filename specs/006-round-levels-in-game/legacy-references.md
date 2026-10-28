# What still uses the legacy rules (T062)

Spec 006 made `Bloomlings.Core.Rounds` the game's rules. The legacy deterministic simulation
(`core/src/Bloomlings.Core/Simulation/`) still builds because the code below uses it. This list is the input for a
follow-up spec that retires it. Generated on 2026-10-28 with
`grep -rl "Bloomlings.Core.Simulation" --include=*.cs` (outside build outputs and worktrees).

What ships to players uses only the round rules: the full playtest and the tester open round levels on `RoundPlay`
(`playtest/design/RoundLevelScreen.cs`, `playtest/android/TesterView.cs`), and the Unity client routes every level
through `GameFlow.StartRoundLevel`. The legacy references in clients are the old level screen, its painters and the
design board's preview fixtures, kept compiling until the retirement spec removes them (Unity's gameplay views move to
the round rules in T058/T059).

| Area | Files | Why it still uses the legacy rules |
|---|---:|---|
| `core/src/Bloomlings.Core` | 24 | the simulation itself, its timeline and level model |
| `core/src/Bloomlings.Content` | 3 | legacy level definitions, catalog and daily pool readers |
| `core/src/Bloomlings.Solver` | 4 | the legacy solver (validation of legacy catalog levels) |
| `core/src/Bloomlings.Generator` | 3 | the legacy generator and its pictures pipeline |
| `core/src/Bloomlings.Pipeline` | 3 | legacy generate / validate / publish commands |
| `core/tests/Bloomlings.Core.Tests` | 10 | tests and golden replays of the legacy path |
| `core/tests/Bloomlings.Content.Tests` | 2 | tests and golden replays of the legacy path |
| `core/tests/Bloomlings.Solver.Tests` | 3 | tests and golden replays of the legacy path |
| `core/tests/Bloomlings.Generator.Tests` | 5 | tests and golden replays of the legacy path |
| `client/Assets/Bloomlings` | 26 | the legacy gameplay views and services, still compiled; round levels route to the round entry point |
| `playtest` | 16 | the legacy level screen, its painters and the design board preview fixtures |
| `tools` | 8 | old pilot checks and the gameplay audit (research tools) |

## Files

- `client/Assets/Bloomlings/Gameplay/Board/BoardView.cs`
- `client/Assets/Bloomlings/Gameplay/Board/SpecialView.cs`
- `client/Assets/Bloomlings/Gameplay/GameplayController.cs`
- `client/Assets/Bloomlings/Gameplay/Slots/SlotPlaces.cs`
- `client/Assets/Bloomlings/Gameplay/Slots/SlotRowView.cs`
- `client/Assets/Bloomlings/Gameplay/Timeline/EventTimeline.cs`
- `client/Assets/Bloomlings/Gameplay/Timeline/TimelinePlayer.cs`
- `client/Assets/Bloomlings/Gameplay/Tray/PodView.cs`
- `client/Assets/Bloomlings/Gameplay/Tray/TrayView.cs`
- `client/Assets/Bloomlings/Meta/DailyChallenge/DailyChallengeService.cs`
- `client/Assets/Bloomlings/Meta/Profile/AchievementAttempt.cs`
- `client/Assets/Bloomlings/Services/Backend/LeaderboardClient.cs`
- `client/Assets/Bloomlings/Services/Content/CatalogService.cs`
- `client/Assets/Bloomlings/Services/Economy/Online/WinEvidence.cs`
- `client/Assets/Bloomlings/Tests/EditMode/CloudSaveAndLeaderboardTests.cs`
- `client/Assets/Bloomlings/Tests/EditMode/EventTimelineTests.cs`
- `client/Assets/Bloomlings/Tests/EditMode/GuestWinArchiveTests.cs`
- `client/Assets/Bloomlings/Tests/EditMode/GuideTourTests.cs`
- `client/Assets/Bloomlings/Tests/EditMode/PodLinksTests.cs`
- `client/Assets/Bloomlings/Tests/EditMode/TimelineReplayTests.cs`
- `client/Assets/Bloomlings/Tests/EditMode/WinEvidenceTests.cs`
- `client/Assets/Bloomlings/UI/Design/GuideTour.cs`
- `client/Assets/Bloomlings/UI/Design/PodLinkRules.cs`
- `client/Assets/Bloomlings/UI/Design/PodLinks.cs`
- `client/Assets/Bloomlings/UI/Screens/JamScreen.cs`
- `client/Assets/Bloomlings/UI/Screens/WinScreen.cs`
- `core/src/Bloomlings.Content/Golden/GoldenCase.cs`
- `core/src/Bloomlings.Content/Golden/GoldenRunner.cs`
- `core/src/Bloomlings.Content/Validation/ValidationRecord.cs`
- `core/src/Bloomlings.Core/Boosters/BloomBurst.cs`
- `core/src/Bloomlings.Core/Boosters/ExtraSlot.cs`
- `core/src/Bloomlings.Core/Boosters/RecoveryCheck.cs`
- `core/src/Bloomlings.Core/Boosters/Return.cs`
- `core/src/Bloomlings.Core/Boosters/ShufflePlanner.cs`
- `core/src/Bloomlings.Core/Mechanics/KeysAndLocks.cs`
- `core/src/Bloomlings.Core/Mechanics/Mystery.cs`
- `core/src/Bloomlings.Core/Mechanics/Specials.cs`
- `core/src/Bloomlings.Core/Search/StateSearch.cs`
- `core/src/Bloomlings.Core/Simulation/CommandResult.cs`
- `core/src/Bloomlings.Core/Simulation/CommandText.cs`
- `core/src/Bloomlings.Core/Simulation/Commands.cs`
- `core/src/Bloomlings.Core/Simulation/EventText.cs`
- `core/src/Bloomlings.Core/Simulation/Events.cs`
- `core/src/Bloomlings.Core/Simulation/IRoundHook.cs`
- `core/src/Bloomlings.Core/Simulation/LevelSession.cs`
- `core/src/Bloomlings.Core/Simulation/LevelState.cs`
- `core/src/Bloomlings.Core/Simulation/LevelStatus.cs`
- `core/src/Bloomlings.Core/Simulation/LevelView.cs`
- `core/src/Bloomlings.Core/Simulation/MysteryAssignment.cs`
- `core/src/Bloomlings.Core/Simulation/RejectReason.cs`
- `core/src/Bloomlings.Core/Simulation/SessionOptions.cs`
- `core/src/Bloomlings.Core/Simulation/Settler.cs`
- `core/src/Bloomlings.Core/Simulation/StatusEvaluator.cs`
- `core/src/Bloomlings.Generator/AuthoredTrayBuilder.cs`
- `core/src/Bloomlings.Generator/LevelGenerator.cs`
- `core/src/Bloomlings.Generator/TrayBuilder.cs`
- `core/src/Bloomlings.Pipeline/Commands/CatalogCommands.cs`
- `core/src/Bloomlings.Pipeline/Launch/LaunchInstaller.cs`
- `core/src/Bloomlings.Pipeline/Validation/CatalogValidator.cs`
- `core/src/Bloomlings.Solver/FairnessChecker.cs`
- `core/src/Bloomlings.Solver/HiddenLayerFairness.cs`
- `core/src/Bloomlings.Solver/ISolver.cs`
- `core/src/Bloomlings.Solver/Solver.cs`
- `core/tests/Bloomlings.Content.Tests/ContentFolderTests.cs`
- `core/tests/Bloomlings.Content.Tests/PackWriterTests.cs`
- `core/tests/Bloomlings.Core.Tests/Board/BoardBuilderTests.cs`
- `core/tests/Bloomlings.Core.Tests/Board/CountedStackTests.cs`
- `core/tests/Bloomlings.Core.Tests/Fixtures/RandomLevels.cs`
- `core/tests/Bloomlings.Core.Tests/Fixtures/RuleLevels.cs`
- `core/tests/Bloomlings.Core.Tests/Properties/InvariantProperties.cs`
- `core/tests/Bloomlings.Core.Tests/Properties/SearchPathProperties.cs`
- `core/tests/Bloomlings.Core.Tests/Rules/BoosterTests.cs`
- `core/tests/Bloomlings.Core.Tests/Rules/CoreLoopTests.cs`
- `core/tests/Bloomlings.Core.Tests/Rules/MechanicsTests.cs`
- `core/tests/Bloomlings.Core.Tests/Rules/ShuffleLookTests.cs`
- `core/tests/Bloomlings.Generator.Tests/ArtPaletteTests.cs`
- `core/tests/Bloomlings.Generator.Tests/BigLevelTests.cs`
- `core/tests/Bloomlings.Generator.Tests/ExtendedBoardTests.cs`
- `core/tests/Bloomlings.Generator.Tests/LaunchProductionTests.cs`
- `core/tests/Bloomlings.Generator.Tests/MechanicReadabilityTests.cs`
- `core/tests/Bloomlings.Solver.Tests/FairnessTests.cs`
- `core/tests/Bloomlings.Solver.Tests/HiddenLayerFairnessTests.cs`
- `core/tests/Bloomlings.Solver.Tests/SolverTests.cs`
- `playtest/android/LevelAnimator.cs`
- `playtest/android/TesterView.cs`
- `playtest/check/Program.cs`
- `playtest/design/BoardPainter.cs`
- `playtest/design/BoosterBarPainter.cs`
- `playtest/design/ClearPainter.cs`
- `playtest/design/EndCards.cs`
- `playtest/design/GuidePainter.cs`
- `playtest/design/LevelScreen.cs`
- `playtest/design/PodPainter.cs`
- `playtest/design/RoundLevelScreen.cs`
- `playtest/design/RoundPainter.cs`
- `playtest/design/SlotPainter.cs`
- `playtest/preview/Fixtures.cs`
- `playtest/preview/MosaicPilotPreview.cs`
- `playtest/preview/Perf.cs`
- `tools/gameplay-audit/DoorAudit.cs`
- `tools/gameplay-audit/ExactAudit.cs`
- `tools/gameplay-audit/Program.cs`
- `tools/gameplay-pilot-v2/check/Program.cs`
- `tools/gameplay-pilot-v3/check/Program.cs`
- `tools/gameplay-pilot-v4/check/Program.cs`
- `tools/gameplay-pilot-v5/check/Program.cs`
- `tools/gameplay-pilot/check/Program.cs`

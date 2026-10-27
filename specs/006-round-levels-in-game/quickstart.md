# Quickstart: Validate Round Levels in the Game (spec 006)

Runnable scenarios that prove the feature end to end. Formats: `contracts/`. Entities: `data-model.md`.

## Prerequisites

- Node 22: the round tools and the exporter.
- .NET 10 SDK pinned by `core/global.json`: core, pipeline, tests, playtest.
- For the APK: the manual workflow `android-apk.yml`, or a local .NET for Android build.

## 1. Export L1–L20 from the round tools (JS)

```sh
node tools/rounds/rounds.mjs confirm 1 20 --sample product/playtests/pilot-v12-owner-2026-10-27/results.json
node tools/rounds/export.mjs 1 20 --out content/game/rounds/export/L0001-L0020 \
     --owner-results product/playtests/pilot-v12-owner-2026-10-27/results.json
```

Expected:
- 20 levels;
- 20 pictures (P0001–P0003 locked plus 17);
- 20 proofs;
- 20 fixtures;
- `export.json` with the engine fingerprints;
- L18 carries its `exception`.

The exporter refuses if any entry check fails, for example when a picture is not approved.

## 2. Import and validate in the game's pipeline (C#)

```sh
dotnet run --project core/src/Bloomlings.Pipeline -- rounds import content/game/rounds/export/L0001-L0020
dotnet run --project core/src/Bloomlings.Pipeline -- rounds validate
```

Expected:
- all checks pass;
- conformance replays 20 solutions, every random game (100 or 400 per level) and the 23 owner attempts;
- zero differences (SC-001, SC-002).

## 3. Rule tests and determinism

```sh
dotnet test core/Bloomlings.sln --filter "FullyQualifiedName~Rounds"
```

Expected:
- the ordering tests of research R3 pass;
- the booster rules pass (Extra Slot, Return, Shuffle, Bloom Burst in live time);
- the golden replays pass;
- the same commands give the same final state at ×1, ×2 and Auto, and before and after a restore (SC-007).

Then, as a deliberate break: set the release lag to 299 ms in a scratch copy and rerun `rounds validate`. It must fail
and name a level and its first differing event (SC-005).

## 4. Publish and build the playtest

```sh
dotnet run --project core/src/Bloomlings.Pipeline -- rounds publish --out content/game/rounds/published
dotnet run --project playtest/check                     # animator replays the round solutions to the rules' state
dotnet run --project playtest/preview -- --rounds       # the presentation checks of contracts/presentation.md
```

Then build the full playtest APK with the round content (manual workflow) and install it.

## 5. The owner's check on the phone

- Play L1 → L20.
- Check the rules: L5 jams and wins as in pilot v12; hidden pods reveal on top (L6, L8); links (L11); the bridge (L16).
- Check the presentation:
  - no icons on cubes;
  - the clear animation in live time;
  - front link tubes with split colours;
  - Auto ×2 with the HUD gone;
  - the speed-up button;
  - introduction cards and reminders;
  - the reveal glow;
  - the symbols mode in Settings.
- After L20, "new levels soon". The tester's ◀ ▶ and its level jump work.
- Confirm the sample. The transfer log gets `ownerConfirmedAt`.

## 6. Done

The criteria SC-001 to SC-009 of the spec are met. The path is written down as the skill `bloomlings-round-transfer`.

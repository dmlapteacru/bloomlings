# Quickstart: validating the Bloomlings launch game

This guide lists runnable validation scenarios that prove the feature works end to end. The commands refer to the
project layout in [plan.md](plan.md#project-structure). They become runnable as the corresponding tasks land. Until
then, this guide is the acceptance checklist for `/speckit-tasks`.

> Development gate: full implementation starts only after the product docs are locked (`product/LOCKED_CONCEPT_v0.5.md`).

## Prerequisites

| Tool | Version | Used for |
|---|---|---|
| .NET SDK | 10 LTS | Core, solver, generator, pipeline, tests |
| Unity | 6.3 LTS (`6000.3.x`, pinned in `client/ProjectSettings/ProjectVersion.txt`) | Client |
| Android SDK/NDK, Xcode | as required by Unity 6.3 | Device builds (IL2CPP) |
| Aseprite or any indexed-PNG editor | – | Picture authoring |

## 1. Core rules and determinism (US1, FR-003 to FR-026, SC-005)

```bash
dotnet test core/Bloomlings.sln
```

Expected:

- All rule tests pass, including every pre-lock test from doc 01 §20:
  - two siblings of one family are active at once;
  - a Leaf pod ignores Moss;
  - per-variant accounting holds;
  - partial completion works;
  - same-variant priority goes to the oldest slot;
  - a layered reveal can cross families;
  - a full buffer that is still progressing is not a jam;
  - a true jam is detected;
  - a key opens its lock;
  - connected pods can mix variants.
- The property tests (FsCheck) report no counterexample for these invariants:
  - accounting always reconciles;
  - counts are never negative;
  - replaying the same commands gives the same `StateHash`.
- The golden replay corpus in `core/tests/golden/` matches the expected hashes.

## 2. Solver and level validation (US3, FR-080 to FR-083, SC-004, SC-012)

```bash
dotnet run --project core/src/Bloomlings.Pipeline -- pictures validate --lib content/pictures/lib
dotnet run --project core/src/Bloomlings.Pipeline -- validate --catalog content/catalog --json > validation.json
dotnet run --project core/src/Bloomlings.Pipeline -- score --catalog content/catalog
```

Expected:

- Validation exits with code 0.
- Every level is `solvable` with a stored trace.
- Every non-tutorial level has a jam witness.
- No level has an `unknown` result.
- Levels 1–100 use 100 distinct base pictures, and no picture repeats within 50 levels.
- `score` reports, per 100 levels from L11 on, 15–25 Hard and 6–10 Super Hard levels.

## 3. Generating a band (US3, FR-079)

```bash
dotnet run --project core/src/Bloomlings.Pipeline -- generate \
  --profile content/profiles/band-501-1000.json --levels 501-750 --seed 42 --out content/work/b501
dotnet run --project core/src/Bloomlings.Pipeline -- validate --defs content/work/b501
dotnet run --project core/src/Bloomlings.Pipeline -- review --defs content/work/b501 --out content/work/b501/review
```

Expected:

- Running the same seed twice produces byte-identical definitions.
- Every accepted level passes validation, and each rejected candidate records a reason.
- The review pack shows the board rendering and the finished picture, and the subject is recognizable (SC-015
  pre-check).

## 4. Publishing and content stability (FR-075 to FR-078, SC-011)

```bash
dotnet run --project core/src/Bloomlings.Pipeline -- publish --catalog content/catalog --content-version 1 --out build/content
dotnet run --project core/src/Bloomlings.Pipeline -- diff --from 1 --to 2
```

Expected:

- The manifest validates against [content-manifest.schema.json](contracts/content-manifest.schema.json).
- The pack hashes match.
- `diff` fails if any shipped level changes without a `definitionVersion` bump.

## 5. Client in the Unity Editor (US1, US2)

1. Open `client/` with Unity 6.3 LTS and load the `Boot` scene.
2. Press Play.
   - Expected: Level 1 starts directly, with no sign-in and a guided first tap (FR-045).
3. Commit pods.
   - Expected: Bloomlings walk from the Garden Entry. Tiles clear, and the finished picture shows through. Pods count
     down and leave their slots.
4. Jam the level on purpose.
   - Expected: the Jam screen keeps the board visible and offers the eligible recoveries and Restart.
5. Win the level.
   - Expected: the finished picture is revealed, the reward is shown, then Next.
6. Toggle 2× speed and replay the same taps.
   - Expected: the same outcome (FR-069).
7. Play through L10 in `Tools → Bloomlings → Fast Progress` (editor-only).
   - Expected: every unlock in the roadmap fires at its level (FR-031).

## 6. Determinism on devices (SC-005, SC-011)

- Build the IL2CPP development builds for Android and iOS, with the `RunGoldenReplays` test scene.
- Expected: every hash in the golden corpus equals the `dotnet test` results.

## 7. Services and monetization (US5, US6, US7)

| Scenario | Steps | Expected |
|---|---|---|
| Offline play | Airplane mode; play 3 levels, use an owned booster | Everything works; store and ads are shown as unavailable (FR-074) |
| Interstitial rules | Test ads; play L1–30 without Remove Ads | No interstitial before L11, during a level or right after a fail; caps respected (SC-013) |
| Remove Ads + restore | Sandbox purchase, reinstall, Restore Purchases | The entitlement returns; interstitials stay off (FR-054) |
| Cloud merge | Progress on device A to L20 and on device B to L15, then sync | Both end at L20 with the union of entitlements; no ledger purchase is lost (R15) |
| Leaderboard | Complete L10 and L11 online | The rank updates; an earlier finisher of the same level ranks higher (FR-062) |
| Daily Challenge | Set the device date to two different UTC days | Two different puzzles, each the same on two devices (FR-064) |

## 8. Playtest measures (SC-001 to SC-003, SC-006, SC-007, SC-014, SC-015)

These are run with external playtesters using the analytics events in
[analytics-events.md](contracts/analytics-events.md) plus a survey.

**Minimum sample:** 30 new players for the onboarding measures and 15 for the glance and recognition tests.

**Targets:**

| Criterion | Target |
|---|---|
| SC-001 | 90% finish L1 within 2 minutes |
| SC-002 | 80% can explain the jam |
| SC-003 | Readability 95% |
| SC-006 | Duration medians within the band targets |
| SC-007 | Win rates within the band targets |
| SC-014 | Survey score ≥ 4 out of 5 |
| SC-015 | Recognition 80% |

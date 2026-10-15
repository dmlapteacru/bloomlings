# Counted stacks and readable board mechanics

[Review sheet](mechanics-review.jpg) contains actual desktop renders at the 19.5:9 phone shape, reduced for comparison.
The four columns show L5004 before play, the same stack after actual worker arrivals (9 → 5), L5005's 3×3 gate and
L5006's 3×3 Fountain with its variant condition. These reuse engineering pictures, not new launch artwork.

Rules, compatibility limits, local generation recipes and three further feature proposals are recorded in
[product 16](../../16_MECHANICS_AND_BOARD_READABILITY.md). Reproduce the screens with:

```sh
dotnet run --project playtest/preview -c Release -- --frames 96,97,98,99 --out content/work/mechanics-review/screens
```

Each frame is checked at 16:9, 19.5:9 and 21:9. Stack counters are sparse, public and updated at worker arrivals;
Fountain conditions and their matching board cells are visible. New readable profiles generate 2×2 or 3×3 objects.
Old production definitions retain their existing geometry. The 2500 production levels and picture library are
unchanged; the owner must generate replacement production content locally. Tester fixtures are L5004–5006.

## Validation · 2026-10-15

- Core/content/solver/generator/pipeline: 2734 passed, zero failed. Includes public-stack fairness, state copies,
  hashes, boosters, pack round trips and mechanics across eight board sizes through 36×36.
- Animator replay: 172 normal/rapid runs, zero failed; Unity timeline replay checks also passed.
- Client .NET checks: 484 passed, three pre-existing failures, reproduced before this change:
  `TheWinScreen_StacksSignPictureHeroRewardAndNext`, `PodGrid_InTheTrayArea_IsTheReferenceGrid`, and
  `TheStoneBorder_RingsTheGrid_WithCornersAndRuns`. New counter geometry and replay checks pass.
- Backend: eight passed. Android client and tester compile successfully; only existing documentation warnings.
- Desktop preview and loading checks pass. Production packaging excludes dev assets; tester includes six dev
  levels and three dev pictures. No APK was built.

These are desktop and rules checks. Actual phone readability and Unity Editor/uGUI behavior still require playtesting.

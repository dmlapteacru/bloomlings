# Art pilot 2 · 2026-10-15

- [Six actual game boards](mosaic-pilot.jpg).
- [36×36 bakery: start → partial clear → win → earned Collection](story_lilypad_bakery-journey.jpg).
- [30×32 jellyfish airship: the same four game states](story_jellyfish_garden_airship-journey.jpg).
- [Concept reference](../../../content/pictures/pilot-v2/references/concepts-v2.png), with more detail than the final grids.

The JPEGs come directly from the desktop game renderer; they are not image-generation mockups. The phone screens
are 540×1170 (19.5:9), replayed using accepted pod taps and captured after the real win/Collection award. The overview
uses the same board painter at a larger presentation size. All six grids and recipes remain isolated from production.
Original sources, palette constraints, art statuses and local reproduction:
[pilot-v2 README](../../../content/pictures/pilot-v2/README.md).

## Validation

- All six imported pictures pass automated checks. All six Normal levels pass solver/validation and score checks,
  with booster-free wins and jam witnesses. The six warnings are the existing provisional readability sign-off.
- `build-art-pilot.ps1 -Pilot content/pictures/pilot-v2 -Out content/work/art-pilot-v2-repeat -Render` completed;
  a second build reproduces all six definitions and certificates exactly. All 24 full game-state captures pass.
- The original three-scene pilot also reproduces its definitions and certificates exactly.
- Composition audit: no structural copy among the nine pilot grids, ignoring mirror/recolor/simple scaling.
  The six-scene registry and its sample catalog pass the standard audit; the audit's three unit tests pass.
- Full core solution: 2744 passed, zero failed (including ten new palette/certificate cases). The final focused art
  suite passes twelve cases, adding two generation checks for fixed orientation and palette with different seeds.
- Client: 484 passed, three existing failures: `TheWinScreen_StacksSignPictureHeroRewardAndNext`,
  `PodGrid_InTheTrayArea_IsTheReferenceGrid`, `TheStoneBorder_RingsTheGrid_WithCornersAndRuns`.
- Animator: 172 runs, zero failed. Backend: eight passed. Preview builds successfully. No APK was built.

All six compositions were approved by the owner on 2026-10-15. Actual phone readability has not been playtested here. The 2500 launch compositions,
production quotas/pacing and one-time assignment still need completion; this pilot does not claim release coverage.

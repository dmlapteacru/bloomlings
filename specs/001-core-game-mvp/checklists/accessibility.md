# Accessibility pass (T149)

**Feature**: 001-core-game-mvp · **Requirements**: FR-005, FR-072, SC-003 · **Date**: 2026-09-29

Automated checks were run in the repository. Checks marked open need the Unity Editor, a device or a person.

## 1. Readability tool on every variant pair (automated, done)

`bloomlings-pipeline readability` was re-run; it reproduces `content/readability/pairs-report.json` byte for byte.

- [x] 66 pairs checked, i.e. every pair of the 12 variants; the pool expansions only add variants from this catalog.
- [x] All 66 pairs have distinct icons and a CIEDE2000 distance of at least 10 under normal vision, protanopia, deuteranopia and tritanopia.
- [x] 7 pairs have a grayscale lightness difference below 8 L*. In grayscale their icons carry the identity, so these pairs get the manual grayscale/icon test first:

| Pair | Min ΔE2000 | Grayscale ΔL* |
|---|---|---|
| mist + bark | 30.26 | 1.4 |
| moss + berry | 17.4 | 2.57 |
| flower + bark | 15.48 | 3.61 |
| dew + vine | 32.24 | 4.39 |
| flower + mist | 13.49 | 5.02 |
| water + acorn | 48.75 | 5.9 |
| violet_bud + berry | 34.94 | 7.34 |

The closest pairs by color (lowest minimum ΔE2000) also go early in the manual tests:

| Pair | Min ΔE2000 | Weakest vision |
|---|---|---|
| leaf + vine | 10.7 | protanopia |
| water + mist | 10.83 | protanopia |
| acorn + berry | 10.93 | tritanopia |
| leaf + bark | 11.05 | deuteranopia |
| wood + berry | 11.44 | protanopia |
| flower + mist | 13.49 | protanopia |

## 2. Other automated checks (done)

- [x] Cosmetics cannot reduce readability (FR-063): worn items use neutral tints (HSV saturation ≤ 0.3) and never change the variant tint or icon (`WardrobeCollectionThemeTests.Cosmetics_PassTheReadabilityCheck`).
- [x] Background themes stay light (relative luminance > 0.7), so tile frames and pods keep their contrast (`Themes_RotateByBand_AndMirrorTheRoadmapFile`).
- [x] Hue never carries meaning alone: every variant has its own icon shape (`distinctIcons` is true for all pairs).

## 3. Colorblind-simulation screenshots of 6-variant boards (open, Unity Editor)

- [ ] Capture a 6-variant level (for example a band-0051-0100 level with six roles) under normal vision and under protanopia, deuteranopia and tritanopia simulation. Use a Game view color-blindness filter or a post-process simulation with the matrices from `ColorScience.Simulate`.
- [ ] Store the screenshots next to this file (`accessibility/`) and note any pair that is hard to tell apart.

## 4. Pod counts at the smallest supported screen (open, device or device simulator)

- [ ] On the smallest supported phone (iPhone SE 2nd gen, 4.7"), load the largest tray of the catalog and check that every pod count is legible at arm's length (FR-072, SC-003).
- [ ] Repeat with the largest Waiting Slot counts (two-digit counts).

## 5. Manual FR-005 checks for every approved pair (open, person)

For each pair in `content/readability/approved-pairs.json` (currently provisional), a tester tells the two variants apart:
on a pod in the tray, in a Waiting Slot, and on moving Bloomling characters, at 1× and at 2× speed. When every pair passes,
set `status` to `approved` and `reviewer` in `approved-pairs.json`.

| Pair | Tray pod | Waiting Slot | Moving 1× | Moving 2× | Tester |
|---|---|---|---|---|---|
| acorn + bark | [ ] | [ ] | [ ] | [ ] | |
| acorn + berry | [ ] | [ ] | [ ] | [ ] | |
| acorn + mist | [ ] | [ ] | [ ] | [ ] | |
| acorn + vine | [ ] | [ ] | [ ] | [ ] | |
| berry + bark | [ ] | [ ] | [ ] | [ ] | |
| berry + mist | [ ] | [ ] | [ ] | [ ] | |
| dew + acorn | [ ] | [ ] | [ ] | [ ] | |
| dew + bark | [ ] | [ ] | [ ] | [ ] | |
| dew + berry | [ ] | [ ] | [ ] | [ ] | |
| dew + mist | [ ] | [ ] | [ ] | [ ] | |
| dew + vine | [ ] | [ ] | [ ] | [ ] | |
| dew + wood | [ ] | [ ] | [ ] | [ ] | |
| flower + acorn | [ ] | [ ] | [ ] | [ ] | |
| flower + bark | [ ] | [ ] | [ ] | [ ] | |
| flower + berry | [ ] | [ ] | [ ] | [ ] | |
| flower + dew | [ ] | [ ] | [ ] | [ ] | |
| flower + mist | [ ] | [ ] | [ ] | [ ] | |
| flower + vine | [ ] | [ ] | [ ] | [ ] | |
| flower + violet_bud | [ ] | [ ] | [ ] | [ ] | |
| flower + water | [ ] | [ ] | [ ] | [ ] | |
| flower + wood | [ ] | [ ] | [ ] | [ ] | |
| leaf + acorn | [ ] | [ ] | [ ] | [ ] | |
| leaf + bark | [ ] | [ ] | [ ] | [ ] | |
| leaf + berry | [ ] | [ ] | [ ] | [ ] | |
| leaf + dew | [ ] | [ ] | [ ] | [ ] | |
| leaf + flower | [ ] | [ ] | [ ] | [ ] | |
| leaf + mist | [ ] | [ ] | [ ] | [ ] | |
| leaf + moss | [ ] | [ ] | [ ] | [ ] | |
| leaf + vine | [ ] | [ ] | [ ] | [ ] | |
| leaf + violet_bud | [ ] | [ ] | [ ] | [ ] | |
| leaf + water | [ ] | [ ] | [ ] | [ ] | |
| leaf + wood | [ ] | [ ] | [ ] | [ ] | |
| mist + bark | [ ] | [ ] | [ ] | [ ] | |
| moss + acorn | [ ] | [ ] | [ ] | [ ] | |
| moss + bark | [ ] | [ ] | [ ] | [ ] | |
| moss + berry | [ ] | [ ] | [ ] | [ ] | |
| moss + dew | [ ] | [ ] | [ ] | [ ] | |
| moss + flower | [ ] | [ ] | [ ] | [ ] | |
| moss + mist | [ ] | [ ] | [ ] | [ ] | |
| moss + vine | [ ] | [ ] | [ ] | [ ] | |
| moss + violet_bud | [ ] | [ ] | [ ] | [ ] | |
| moss + water | [ ] | [ ] | [ ] | [ ] | |
| moss + wood | [ ] | [ ] | [ ] | [ ] | |
| vine + bark | [ ] | [ ] | [ ] | [ ] | |
| vine + berry | [ ] | [ ] | [ ] | [ ] | |
| vine + mist | [ ] | [ ] | [ ] | [ ] | |
| violet_bud + acorn | [ ] | [ ] | [ ] | [ ] | |
| violet_bud + bark | [ ] | [ ] | [ ] | [ ] | |
| violet_bud + berry | [ ] | [ ] | [ ] | [ ] | |
| violet_bud + dew | [ ] | [ ] | [ ] | [ ] | |
| violet_bud + mist | [ ] | [ ] | [ ] | [ ] | |
| violet_bud + vine | [ ] | [ ] | [ ] | [ ] | |
| violet_bud + water | [ ] | [ ] | [ ] | [ ] | |
| violet_bud + wood | [ ] | [ ] | [ ] | [ ] | |
| water + acorn | [ ] | [ ] | [ ] | [ ] | |
| water + bark | [ ] | [ ] | [ ] | [ ] | |
| water + berry | [ ] | [ ] | [ ] | [ ] | |
| water + dew | [ ] | [ ] | [ ] | [ ] | |
| water + mist | [ ] | [ ] | [ ] | [ ] | |
| water + vine | [ ] | [ ] | [ ] | [ ] | |
| water + wood | [ ] | [ ] | [ ] | [ ] | |
| wood + acorn | [ ] | [ ] | [ ] | [ ] | |
| wood + bark | [ ] | [ ] | [ ] | [ ] | |
| wood + berry | [ ] | [ ] | [ ] | [ ] | |
| wood + mist | [ ] | [ ] | [ ] | [ ] | |
| wood + vine | [ ] | [ ] | [ ] | [ ] | |

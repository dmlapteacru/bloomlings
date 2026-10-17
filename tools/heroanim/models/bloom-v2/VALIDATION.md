# Bloom V2 integration validation — 2026-10-17

- Both original ZIPs are preserved byte-for-byte with SHA-256 pins and valid ZIP CRCs.
- Preparation imported all 16 source meshes (base + 15 items), verifying unchanged world vertices and UVs.
  Four source actions and the knitted scarf's three garment fit morphs are retained in the original delivery.
- `node tools/heroanim/check.mjs`: passed. Every generated layer is lossless RGBA8 with pinned dimensions / hashes;
  all 15 delivered item IDs occur in the icon set exactly once. The retired legacy publisher cannot restore Bloom.
- Client suite: **501 passed, 9 failed, 0 skipped, 510 total**. Bloom tests cover all 240 saved outfit combinations /
  216 distinct looks, save/reload, family restrictions, slot-local Default, independent scarves and stock petals.
  Existing hero cache, sprite resource, motion timing, foot line and frame bounds checks pass for the new Bloom.
- Playtest: **9 fixtures passed** at 19.5:9: four Home outfits, four Wardrobe categories and stable shelf previews.
  Real taps equip / reset items; the main stand changes while shelf picture pixels stay identical across fitting
  other categories. Both scarf alternatives appear on Home, with clothing and glasses.
- All modular sprites decode through the same packaged full-color PNG path as the playtest, with pixels and
  dimensions compared against the originals. No missing new image was reported.
- Navigation: **11 cold destinations, 116 staged frames, 150 resources**; the previous complete page remains until
  the next is ready. **100 live-preview frames** continue with delayed transient effects.
- Runtime: **255 cropped sprites, 5,198,153 bytes added**; **132 old motion frames, 7,067,971 bytes retired** outside
  runtime (net reduction: 1,869,818 bytes). All wearables draw in four layers at most, with on-demand caching.
- Bloom's common camera was uniformly reduced to 94% around the same foot line so the stock petal halo fits
  the logical cell. Source geometry and proportions remain unchanged. Default never exposes the bald scaffold.
- No APK, level generation, skeletal action selection or source Blender master was created.

The nine full-suite failures are the same pre-existing failures documented, and reproduced against an earlier
baseline, in `../sprig-v2/VALIDATION.md`. They were not disabled or altered in this integration:

- `ATapOnTheMemberOnTop_WaitsForItsPartner_AndSaysSo`
- `Level54_HasAPairApart_EachUnderItsColumnsPlusN`
- `TheGroups_TakeThePaletteInTheirOrder`
- `TheFreeFrames_AreOwnedFromLevelOne_AndCanBePickedAndShown`
- `TheWinScreen_StacksSignPictureHeroRewardAndNext`
- `Replays_ShowEveryChangeInTheRulesOrder_AndEndOnTheRulesState`
- `TwoQuickTaps_ShowOnTwoPlates_AndTheirWavesPlaySideBySide`
- `PodGrid_InTheTrayArea_IsTheReferenceGrid`
- `TheStoneBorder_RingsTheGrid_WithCornersAndRuns`

Reproduce the render / interaction / packaging checks:

```sh
dotnet run --project playtest/preview -- --frames 146,147,148,149,150,151,152,153,154 --shape 19.5x9 --out content/work/bloom-v2/screens
```

The initial run also found that Bloom's taller petal halo exceeded the legacy frame bounds. The bake camera was
corrected, and the final suite passed both frame-bound and foot-line assertions. Its static-pose uniqueness rule
now recognizes Bloom alongside the other modular heroes; geometry bounds and fit assertions remain enforced.

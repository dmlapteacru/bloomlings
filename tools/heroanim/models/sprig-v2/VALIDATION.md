# Sprig V2 and Wardrobe validation — 2026-10-17

- `node tools/heroanim/check.mjs`: passed; original deliveries, unchanged world geometry / UVs, four source actions,
  three scarf fit morphs and all generated runtime frames are pinned.
- Client targeted tests: **61 passed, 0 failed**. Includes all 144 saved Sprig outfits, category-local Default,
  fitted item visibility, independent slots, save/reload, localization and the existing Twig behavior.
- Playtest preview: **23 fixtures passed** at 19.5:9. Home stock / winter / summer, all Wardrobe categories,
  real shelf taps, free Twig fitting, paid purchase confirmation, scroll, Shop without Cosmetics, Animations and
  the actual solved L12 victory with the new Sprig outfit.
- Packaging checks decoded all Sprig RGBA layers and compared their pixels against the embedded originals.
- Navigation loading: **11 cold destinations, 116 staged frames, 150 resources**, with the previous complete page
  retained until the next was ready. Another 100 animation-preview frames continued while transient effects were delayed.
- Runtime PNGs: **3,288,169 bytes added**, **17,405,388 bytes retired** (384 legacy motion frames archived outside runtime).
- No APK or level catalog was generated.

The complete client suite reports **499 passed, 9 failed**. An isolated compilation of the previous commit's client
sources (`50fa00fa`) reproduced all nine failures. They concern existing linked-pod level expectations, the older
free-profile-frame assertion, previous win / tray / border layout expectations and replay pictures absent from the
installed picture library. They were not suppressed or altered in this change:

- `ATapOnTheMemberOnTop_WaitsForItsPartner_AndSaysSo`
- `Level54_HasAPairApart_EachUnderItsColumnsPlusN`
- `TheGroups_TakeThePaletteInTheirOrder`
- `TheFreeFrames_AreOwnedFromLevelOne_AndCanBePickedAndShown`
- `TheWinScreen_StacksSignPictureHeroRewardAndNext`
- `Replays_ShowEveryChangeInTheRulesOrder_AndEndOnTheRulesState`
- `TwoQuickTaps_ShowOnTwoPlates_AndTheirWavesPlaySideBySide`
- `PodGrid_InTheTrayArea_IsTheReferenceGrid`
- `TheStoneBorder_RingsTheGrid_WithCornersAndRuns`

To reproduce the screen checks (screens are local outputs, outside runtime):

```sh
dotnet run --project playtest/preview -- --frames 17,26,42,43,49,55,56,108,109,110,112,120,125,129,135,136,137,138,139,140,141,142,143 --shape 19.5x9 --out content/work/sprig-v2/screens
```

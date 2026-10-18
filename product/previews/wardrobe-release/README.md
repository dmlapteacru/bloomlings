# Wardrobe: release assortment and economy

[Баланс, открытия, полный список предметов](../../wardrobe-economy.md).

- [Head на L40: бесплатный старт, цены, замки по уровню](wardrobe-l40.jpg)
- [Body Bloom на L40](wardrobe-body-l40.jpg)
- [Settings: отдельная строка петалей](settings-petals.jpg)

Generated from the real shared playtest UI with fixtures 224, 225 and 230. Capture size: 1080×2340; review JPEGs: 720×1560. The same flows also pass at 1080×1920. Fixtures 223–232 cover the L40 gate, all four availability stages, a real card purchase/cancellation/reload, locked fitting, and wallet controls from Home and gameplay Pause.

Validation: 107 focused client tests passed (including all accepted hero-layer combinations); 17 long-phone and 5 short-phone render fixtures passed. The render harness also verified cold navigation (11 destinations, 121 staged frames, 159 resources), transient previews, all registered hero pictures, safe areas and touch targets. No APK was built.

The full client suite: **536 passed / 9 failed / 545 total**. All nine failures were reproduced with the pre-change client sources at `6fa7537b`; they concern existing pod-link showcases, Home/win layout bounds, timeline replay, tray-grid geometry and stone-border ordering. They are outside this Wardrobe change and were not suppressed or altered:

- `ATapOnTheMemberOnTop_WaitsForItsPartner_AndSaysSo`
- `Level54_HasAPairApart_EachUnderItsColumnsPlusN`
- `TheGroups_TakeThePaletteInTheirOrder`
- `TheHeroes_StandOnTheFountain_BetweenTheLogoAndThePlaque_OnEveryShape`
- `TheWinScreen_StacksSignPictureHeroRewardAndNext`
- `Replays_ShowEveryChangeInTheRulesOrder_AndEndOnTheRulesState`
- `TwoQuickTaps_ShowOnTwoPlates_AndTheirWavesPlaySideBySide`
- `PodGrid_InTheTrayArea_IsTheReferenceGrid`
- `TheStoneBorder_RingsTheGrid_WithCornersAndRuns`

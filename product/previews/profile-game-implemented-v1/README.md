# Implemented game Profile

2026-10-13, `codex/shop-garden-body`. Actual shared-layout Skia renders of the implementation; approved generated references are in `../profile-game-concepts-v1/`.

## Screens

- `60-profile-game-19.5x9.png`: Profile landing page with player card and independent achievement cards, current names/progress/check/lock states.
- `57-profile-game-avatar-19.5x9.png`: all 14 existing avatar portraits remain in the vertical list. Green card prices and the footer reuse the Shop's purchase skin, Lilita One typography and dark green text outline. Tapping a portrait selects; tapping a price opens the existing purchase confirmation.
- `65-profile-game-frame-19.5x9.png`: larger two-column owned-frame previews, actual free frame styles plus generated Daisy/Ivy overlays.
- `58-profile-game-badge-19.5x9.png`: badges use **the same three-column cell geometry as avatars**, with smaller emblems, real localized labels, owned choices and explicit No badge.
- `59-profile-game-name-19.5x9.png`: one native text field, current name rules and Save.
- Matching `16x9` captures demonstrate the short-screen layout and body-only scroll.
- `61-profile-game-avatar-buy-19.5x9.png`: actual price-button purchase confirmation.
- `62-profile-game-badges-scroll-16x9.png`: last choices in an earned-badge inventory larger than the old fixed 14-item capacity.
- `63-profile-game-achievements-last-19.5x9.png`: final achievement page.
- `64-profile-game-store-return-19.5x9.png`: returning from Shop retains the pending frame/tab.

## Shared implementation

`ProfileGameDesign` owns the fixed header, cream segmented tabs, profile summary, preview/avatar/name/level rectangles, content viewport and footer. None of the summary geometry depends on the active tab. Both native Unity and the Skia/Android renderer use it. The four editor captures with the same settings (57 / 58 / 59 / 65) have a pixel-identical summary crop `(38,470)-(1042,731)` at 19.5x9; source images were compared read-only, without modifying them.

Unity uses a full-page editor with `ScrollRect`/`RectMask2D`, clamped vertical scrolling and fixed navigation, summary and Save. Price controls and achievement arrows have enlarged transparent touch targets without enlarging the visible artwork. Profile metadata and all 23 achievements keep their existing services and pagination. Draft selections persist between tabs and while visiting Shop; Back cancels the draft. Save/confirmation, insufficient Petals, name cleaning and No badge behavior remain service-backed.

The existing profile background and every hero/avatar image are reused. New production art: blank gold card skin plus transparent Daisy/Ivy frame overlays. Existing pink/blue/selected green Shop skins and green purchase skin are reused through nine-slice. Runtime atlas crops preserve the unchanged source PNGs and alpha. Exact prompts/provenance and original output filenames are in `client/Assets/Bloomlings/Art/Shop/SOURCE.md`; dimensions/hashes in `Resources/Shop/manifest.json`. Generation used built-in OpenAI ImageGen.

## Verification

- **74 .NET tests passed**, compiling native client scripts against Unity API stubs. Includes profile/achievement services, avatar pricing/ownership/save/name/No badge, economy, localization, preview models and safe-area layout checks across phone shapes.
- **42 rendered fixture/shape combinations passed**: 39, 40, 41, 57–65 plus Shop regressions 17/42 at 16x9, 19.5x9 and 21x9. Main profile was rechecked after final completed-progress styling.
- Scripted fixtures verify drag safety, reachable final avatars/badges, green price confirmation/cancellation, save of No badge and badge selection, all achievement pages, fixed tab behavior, pending values across all tabs, and draft restoration after visiting Shop.
- Visually reviewed the actual landing page, four editor tabs, short screens, Daisy/Ivy frames and confirmation. Redundant captures are omitted from the committed gallery after the full batch passes.
- Unity Editor/device/APK execution was not performed. Temporary offline .NET project files were removed after checks.

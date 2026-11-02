# Implementation Plan: Scene Album

**Branch**: spec 007-scene-album (on `claude/great-darwin-6qrpj8`) | **Date**: 2026-11-02 | **Spec**: [spec.md](spec.md)

## Summary

A new bottom-menu page, the Scene Album, in place of the Collection (switched off, kept as ballast). An engine-free
service derives each object's state from the won levels through the published round levels' pictures; a kit layout
(`AlbumLook`) places the page, the object sheet and the motion for both builds; the owner's layered pack becomes client
resources and a catalog JSON. Existing components are reused: the pages' header and back, the name plate, the popups'
card frame, the raised ‹ › buttons, the green primary button, the raised rows, the green check ball, the alert badge, the
locked notice, the swipe gesture code. New components: the scene card, the medallions, the golden outline and the bottom
sheet's layout.

## Technical Context

**Language/Version**: C# 9 for the client kit and services (Unity 6.3, compiled by `client/DotnetCheck` too), latest C#
for the playtest; Python 3 + Pillow for the one-off pack import (`tools/album/import_scene_pack.py`).
**Primary Dependencies**: existing only (Newtonsoft via `Bloomlings.Content.Json`, SkiaSharp in the preview).
**Storage**: the save's new optional `album` section (own `version` 1; `player-save.schema.json` updated); the catalog
`client/Assets/Bloomlings/Meta/Album/Resources/AlbumCatalog.json`; pictures in
`client/Assets/Bloomlings/Art/Album/Resources/Album/` (a JPEG background and cropped PNG objects a scene, full resolution:
on a 1080 × 2340 phone the card shows the 853 × 1844 canvas at about 0.8, so no downscale; the cropped objects keep the
memory small; no JPEG previews shipped).
**Testing**: `AlbumServiceTests` (client EditMode, run by DotnetCheck), the reference-layout tests, preview frames
271–275 with their checks, `playtest/check`.
**Target Platform**: the full playtest APK and the Unity client; the level tester ignores the album.
**Constraints**: flat 2D only (constitution VII): greyscale copies are made once from pixels on the CPU, no shader or
3D; offline; no purchase in the album; deterministic rules untouched.

## Constitution Check

- I Colony Flow structure, II fair monetization: the reward is a one-time gift; no buying of missing objects. ✔
- III deterministic rules: the album reads progress only. ✔
- VII flat 2D: pictures only (owner's PNG/JPEG layers, CPU-made grey copies and silhouettes). ✔
- Workflow: spec → plan → tasks in this folder; docs updated (CLAUDE.md, product/13, OPEN_QUESTIONS). ✔

## Project Structure

```text
client/Assets/Bloomlings/Meta/Album/        AlbumCatalog.cs, AlbumService.cs, Resources/AlbumCatalog.json
client/Assets/Bloomlings/Services/Save/     PlayerSave.AlbumData, SaveSerializer (album section), SaveMerge (unions)
client/Assets/Bloomlings/UI/Design/         AlbumLook.cs (page, sheet, hit test, lift, reveal, snap), BottomNav (Album),
                                            HomeLook (Album, AlbumNews), AssetSlots (ui.album.*)
client/Assets/Bloomlings/UI/                AlbumArt.cs, UiKitNav (the "!"), Screens/AlbumScreen.cs, HomeScreen (model)
client/Assets/Bloomlings/App/               Boot (service), Home/HomeController (menu, Continue playing)
client/Assets/Bloomlings/Art/Album/         Resources/Album/*.jpg|png, SOURCE.md
playtest/design/                            AlbumScreen.cs, DesignApp (Screen.Album, state, actions), PlaytestMeta, KitNav
playtest/preview/                           Fixtures.Album (frames 271–275), NavigationLoadingChecks
tools/album/import_scene_pack.py            the pack → resources + catalog
```

## Decisions

- **Binding** by picture code, level = lowest published round level holding the picture (any version); without round
  content (the preview's legacy frames) the pack's level hint.
- **Shown scenes**: the first always; then each while an object is open or within 30 levels (`PreviewWindow`).
- **Card width**: the scene is contain-fitted at full height between the name plate and the progress strip, so the card
  is narrower than the mockup's (the owner's rule: never cropped); the medallions moved to a full-width progress strip
  under the card for big touch targets.
- **Reward**: Remote Config `economy.album.sceneReward` (50, open question); through `EconomyService.Grant`; greyed in
  the server-authoritative economy until a server grant exists.
- **Locked look**: the playtest draws the grey copy under an 18 % black silhouette tint (= 82 % brightness); Unity bakes
  `AlbumLook.LockedPixels` once per layer.
- **Analytics**: none (the contract has no album event).

## Complexity Tracking

None beyond the two-build twin pattern the project already uses.

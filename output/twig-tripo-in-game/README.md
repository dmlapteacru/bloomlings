# Segmented Tripo Twig in the game

Actual playtest page captures at 1080×2340, using the shared client outfit selection and baked art:

- `home-acorn.png`, `home-cap.png`: the same base / face with the saved headwear.
- `shop.png`: Shop → Cosmetics → Twig, two free hats and bare Default. Choose / Remove switches one Hat slot.
- `wardrobe.png`: the selected cap in the Wardrobe preview.
- `win.png`: the selected cap after solving L13 and advancing progression.

The new source has no skeleton, so this equipment pilot uses its static pose with existing 2D breathing / pose
movement. The Tripo color maps remain provisional. Source models are not runtime assets. Profile avatar pictures
remain independently selected. No APK was built here.

## Verification (2026-10-17)

- 48 client checks passed: Twig / Sprig equipment, save round-trip, family fitting, free selection before Wardrobe
  unlock, gameplay exclusion, hero metadata / motion, Wardrobe and localization.
- `node tools/heroanim/check.mjs` and `twig-tripo-check.mjs` passed: sources, identical shared face / ears / UV / paint /
  scale, fitted exports and all runtime art hashes.
- Preview fixtures 108, 112–117 passed with original images at 19.5:9 and packed assets at 16:9, 19.5:9 and 21:9.
  They tap the actual Shop cards, equip / swap / remove, restart the application, draw Home / Wardrobe and solve L13
  through victory. Sprig equipment remains usable.
- All five packed Twig payloads equal the original full-color PNG bytes (438,562 bytes total).
- Each preview run also checked all 900 legacy frames plus five full-color Twig images, 11 cold navigation
  destinations over 117 staged draws (the previous complete page stays until resources are ready), and 100 live
  effect frames with delayed effects. These are desktop renderer / loading checks, not device performance measurements.

Reproduce from repository root:

```sh
node tools/heroanim/check.mjs
node tools/heroanim/twig-tripo-check.mjs
dotnet test client/DotnetCheck/Bloomlings.Client.DotnetCheck.csproj --no-restore --filter 'FullyQualifiedName~TwigGearTests|FullyQualifiedName~SprigGearTests|FullyQualifiedName~HeroFramesTests|FullyQualifiedName~HeroMotionTests|FullyQualifiedName~Wardrobe|FullyQualifiedName~LocalizationTests'
dotnet run --project playtest/preview --no-restore -- --frames 108,112,113,114,115,116,117 --out content/work/twig-tripo-in-game
```

Source assembly / Blender import: `tools/heroanim/models/twig-tripo/README.md`.
Active sprite bake: `tools/heroanim/bake-twig-tripo.mjs`.

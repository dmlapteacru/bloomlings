# Segmented Tripo Twig in the game

Actual playtest page captures at 1080×2340, using the shared client outfit selection and baked art:

- `home-acorn.png`, `home-cap.png`: the same base / face with the saved headwear.
- `shop.png`: Shop → Cosmetics → Twig, two free hats and bare Default. Choose / Remove switches one Hat slot.
- `wardrobe.png`: the selected cap in the Wardrobe preview.
- `win.png`: the selected cap after solving L13 and advancing progression.

The new source has no skeleton, so this equipment pilot uses its static pose with existing 2D breathing / pose
movement. The Tripo color maps remain provisional. Source models are not runtime assets. Profile avatar pictures
remain independently selected. No APK was built here.

## Verification after the head correction (2026-10-17)

The shared head / ears / mouth and both hats are 12% narrower on the derived render copy. Source / Blender exchange
files remain intact. Body pixels below the head, height and foot position match the preceding bake exactly.

- Client equipment / save / art metadata / motion / localization checks passed.
- `node tools/heroanim/check.mjs` and `twig-tripo-check.mjs` passed.
- Current preview fixtures 112–117 passed at 19.5:9: Shop taps, save / reload, remove, Home (bare / acorn / cap),
  Wardrobe and a real solved L13 victory. The run also checked 900 legacy frames and five full-color Twig images,
  11 cold destinations (117 staged frames), and 100 live-effect frames with delayed dependencies.
- These are desktop renderer / loading checks; no APK is built here.

Reproduce from repository root:

```sh
node tools/heroanim/check.mjs
node tools/heroanim/twig-tripo-check.mjs
dotnet test client/DotnetCheck/Bloomlings.Client.DotnetCheck.csproj --no-restore --filter 'FullyQualifiedName~TwigGearTests|FullyQualifiedName~SprigGearTests|FullyQualifiedName~HeroFramesTests|FullyQualifiedName~HeroMotionTests|FullyQualifiedName~Wardrobe|FullyQualifiedName~LocalizationTests'
dotnet run --project playtest/preview --no-restore -- --frames 112,113,114,115,116,117 --shape 19.5x9 --out content/work/twig-tripo-in-game
```

Source assembly / Blender import: `tools/heroanim/models/twig-tripo/README.md`.
Active sprite bake: `tools/heroanim/bake-twig-tripo.mjs`.

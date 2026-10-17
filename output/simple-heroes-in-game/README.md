# Four simplified heroes in the game

Actual game captures at 1080×2340 after the material / light correction:

- `home.png`: all four new bases, tested with an existing saved Sprig / Twig outfit.
- `wardrobe-sprig.png`, `wardrobe-bloom.png`, `wardrobe-drop.png`, `wardrobe-twig.png`: each new hero on the stand.
- `shop.png`: new Sprig base; accessories fitted to the previous models are withheld.
- `win.png`: the new Twig after solving L13 and advancing progression.

`lighting-comparison.png` compares real mesh renders before / after the viewer-brightness correction, with individual
before / after sprites for Bloom and Twig. Original color maps already contain illumination. The final render uses
75% original albedo plus 25% restrained soft diffuse light; the extra physical / specular shading is removed. There
is no dark UI overlay, source texture repaint, geometry deformation or global exposure increase. The original
source proportions are retained, including the new Twig (the earlier model's X=0.88 correction is not applied).

The source models have no rig / actions. This visual trial uses their static poses and existing subtle 2D breathing.
Previous fitted cap / glasses / backpack parts stay in save / service data and their archived art remains; they are
not shown on the new models or offered in the trial's shelves until refitted. Generic 2D cosmetics and independently
selected profile avatars / frames / badges retain their behavior. No APK is built here.

## Verification, 2026-10-17

- 48 client checks passed: equipment services and save round-trip, hiding unfitted legacy parts without erasing
  selections, art inventory / metadata, hero motion, Wardrobe and localization.
- `node tools/heroanim/check.mjs` passed, including all four new PNG hashes, generated metadata, source archives,
  bake scripts and saved modeling reference hashes, alongside archived animation / equipment art.
- Final packed-asset fixtures 108, 110, 113, 115, 116, 118, 119 passed at 19.5:9. Earlier fixtures also exercised a
  fresh L1 Home, application reload with previous fitted items, the Twig Shop and a solved L12 Sprig win. Before the
  final light adjustment, the same seven layout scenarios passed at 16:9, 19.5:9 and 21:9; geometry / crop sizes did
  not change in the light adjustment.
- All four final packed PNG payloads match the runtime masters byte for byte: 411,390 bytes total. Sources / GLBs
  stay outside runtime asset folders; Android packages only `HeroMotion/*.png`.
- The preview also checked 900 legacy animation frames, five archived full-color Twig images and all four new
  full-color bases. Nine cold destinations retained the previous complete page through staged loading; switching
  between already warmed base heroes used the cache immediately. 116 staged frames, 151 resources; 100 live-effect
  frames continued while transient effects were delayed.
- These are desktop renderer / packaging / loading checks, not device performance measurements.

Source and rebuild: `tools/heroanim/models/simple-tripo/README.md`.

```sh
node tools/heroanim/check.mjs
dotnet run --project playtest/preview --no-restore -- --frames 108,110,111,112,113,114,115,116,117,118,119 --shape 19.5x9 --out content/work/simple-tripo/in-game
```

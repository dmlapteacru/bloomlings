# Home heroes facing the lotus

The layered Home now uses real angled renders of the four current modular heroes:
Sprig +30°, Bloom 0°, Drop −25°, Twig −30° around their common upright pivot.
The gentle three-quarter turns preserve both eyes, matching the old fountain composition.
The front row faces inward; the back row faces the flower from behind. Selected
headwear, glasses, clothes and scarves use the same angle and depth masks as the body.
Frontal Wardrobe, shelf previews, portraits and victory views are retained.

The render recipe is `../../bake-home-heroes.mjs`; angles are in
`../../home-heroes-look.json`. Prepared GLBs come from the existing V2 preparation
scripts. No source FBX, rig, texture UV or owner Blender scene was changed.
Identical pixels at identical crop coordinates share one PNG. Only the currently
worn layers load on Home; this adds no runtime model or 3D rendering.

Validation:

- 62 targeted client tests pass: all four families' current outfits / Home view lookup,
  stable save IDs, fallback identities, retired resource exclusion, avatars and profiles.
- 14 actual game preview fixtures at 19.5:9 cover stock / winter / summer Home, all four
  Wardrobe stands, profile / edit / leaderboard portraits, medals, Settings and No Ads.
- The character tool validates all 57 required pictures and 13 adopted owner pictures.
- Source / originality tests and `node tools/heroanim/check.mjs` verify source records,
  every current output hash, image dimensions, generated C# and runtime manifests.

`home-stock.jpg`, `home-winter.jpg`, `home-summer.jpg` are reduced actual game renders.
Full PNGs are in ignored `content/work/hero-adoption/screens/`. The owner accepted the
quartet permanently; retired models, prototype frame libraries and old motion lab are
removed from the current tree. The portraits and promo scene now depict current heroes.
No APK was built and no levels were regenerated.

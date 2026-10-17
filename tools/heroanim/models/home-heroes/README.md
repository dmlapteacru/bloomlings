# Home heroes facing the lotus

The layered Home now uses real angled renders of the four current modular heroes:
Sprig +70°, Bloom +15°, Drop −50°, Twig −65° around their common upright pivot.
The front row faces inward; the back row faces the flower from behind. Selected
headwear, glasses, clothes and scarves use the same angle and depth masks as the body.
Frontal Wardrobe, shelf previews, portraits and victory views are retained.

The render recipe is `../../bake-home-heroes.mjs`; angles are in
`../../home-heroes-look.json`. Prepared GLBs come from the existing V2 preparation
scripts. No source FBX, rig, texture UV or owner Blender scene was changed.
Identical pixels at identical crop coordinates share one PNG. Only the currently
worn layers load on Home; this adds no runtime model or 3D rendering.

Validation:

- 29 targeted client tests pass, including all four families' outfit combinations,
  unchanged saves, resource enumeration, hero motion and existing outfit rules.
- 8 actual game preview fixtures at 19.5:9 pass: stock, winter and summer Home,
  Wardrobe Head / Body, stable shelves, and Sprig / Twig victory outfits.
- The preview also passes PNG decoding / alpha checks and the existing loading and
  navigation checks (`checks: OK`).
- `node tools/heroanim/check.mjs` verifies sources, generated frame data, image
  dimensions, pixel hashes and the complete runtime manifest.

`home-stock.jpg`, `home-winter.jpg`, `home-summer.jpg` are reduced actual game
preview renders. Full PNGs are in ignored `content/work/home-heroes/screens/`.
The screenshots verify placement and outfit persistence; on-device visual review
remains the owner's next step. No APK was built and no levels were regenerated.

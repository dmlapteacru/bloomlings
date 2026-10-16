# Twig in the game: base and two free hats

Actual game-screen renders using the new bundled sprites, not image-generation mockups.

- [Shop: free Acorn Hat and Daisy Cap](112-twig-gear-shop-19.5x9.png)
- [Home: default-style acorn hat](113-twig-acorn-home-19.5x9.png)
- [Home: olive/rust daisy cap](114-twig-cap-home-19.5x9.png)
- [Actual L13 win with the cap retained](116-twig-gear-win-19.5x9.png)

Open **Shop → Cosmetics → Twig**. Choose/Remove toggles one mutually exclusive Hat slot; Default restores the bare
hero. Both items are free for this pilot and available when Shop opens, without waiting for the later Wardrobe unlock.
Selection persists through restart and is shared with Home, Wardrobe and Twig's win/milestone views. The normal
gameplay figures and separately selected avatar pictures are preserved.

## Source and appearance

The owner's four original FBX exports are saved, renamed `TwigHero…`, under
[`tools/heroanim/models/twig/source`](../../tools/heroanim/models/twig/source/).
Final base/headwear references and the cap's four views are in
[`tools/heroanim/references/twig`](../../tools/heroanim/references/twig/).
The owner's Blender master remains the sole authoring project; no second master was created.

These FBXs have no UV maps or texture images. The current appearance is a **temporary vertex-colour material pass**
in Twig's warm game palette, with wood limbs/ears and olive/rust cap colors. It is not final texture paint, and the old
UV atlas is not applied to an incompatible mesh. Geometry, topology and skin weights remain unchanged.

The animation-only export has real actions but different rest axes on import. Their source is preserved for later
retargeting. This equipment stage uses the base pose plus subtle 2D breathing; full actions remain a separate feature.

## Checks

- 49 focused client tests passed, including save round-trip, fitting restrictions, switching/removal, gameplay exclusion,
  existing Sprig gear, Wardrobe behavior/layout, hero-motion contracts, save merge and localization. Unity scripts
  compile against the project's .NET API stubs.
- Actual preview fixtures 112–116 passed on 16:9, 19.5:9 and 21:9: free shop taps, no spending, app restart, removal,
  both Home variants, Twig Wardrobe and a solver-completed level win. Existing fixtures 108, 26, 74, 75 also passed
  on all three shapes: Sprig gear, paid shop confirmation, scroll/cancel and confirmed Wardrobe purchase.
- Preview navigation checks passed for nine cold destinations and delayed resources. These are desktop runtime
  checks, not measurements on a physical Android phone. No APK was built here.
- `node tools/heroanim/check.mjs` verifies source FBX/recipe hashes, all runtime images, generated crop data and the
  saved reference images. Five new runtime PNG8 images total **131,088 bytes**. FBXs, GLB, references and these
  screenshots never enter the APK.

Reproduce from the repository root:

```bash
blender -b --factory-startup -t 2 --python-exit-code 1 --python tools/heroanim/prepare-twig-gear.py
node tools/heroanim/bake-twig-gear.mjs
node tools/heroanim/check.mjs
dotnet run --project playtest/preview -- --frames 112,113,114,115,116 --out content/work/twig-game-review
```

# Twig in the game: base and two free hats

**Archived pilot.** On 2026-10-17 the owner requested the original Twig back. Current captures and behavior are in
[`../twig-restored/`](../twig-restored/); these screenshots document the retired replacement model.

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

These FBXs have no UV maps or texture images. The initial coarse vertex paint was replaced by a clean front color
pass: bright cream-gold face, soft curved brows, brown irises, tidy eye whites and softer warm light. A derived planar
UV layer carries the new front paint; rear/hidden surfaces keep palette materials. This targets the current frontal
pose, not final all-angle PBR texture coverage. The body geometry and weights are preserved.

The quality pass (2026-10-17) adds filtered render normals, studio lighting, separate material roughness and 3×
supersampling before reduction to the same delivery sizes. Full RGBA colors replace palette / dither noise, and Unity
keeps these five static images without block compression. The source color image itself is retained. Actual comparison:
[`../twig-quality/`](../twig-quality/). Remaining surface / UV work: [hero-surface-quality.md](../../product/plans/hero-surface-quality.md).

The default acorn hat is slightly wider/deeper, raised and given more crown height in the derived render copy, so
it leaves more forehead visible and looks less flattened. The exact fit is in `twig-materials.json`; original FBX
files and the owner's Blender master remain untouched. Details: `tools/heroanim/textures/twig/README.md`.

The animation-only export has real actions but different rest axes on import. Their source is preserved for later
retargeting. This equipment stage uses the base pose plus subtle 2D breathing; full actions remain a separate feature.

## Checks

The quality pass reran fixtures 112–116 on all three shapes and passed the asset check. Body coordinates
and all skin weights are verified against the source; only the recorded acorn fit changes derived coordinates.
Twenty-five focused hero / gear / importer tests pass; the frame inventory now explicitly includes the existing
14 modular Sprig / Twig images alongside the 900 legacy animation frames. The equipment integration checks below
remain documented for the unchanged selection/save behavior.

- 49 focused client tests passed, including save round-trip, fitting restrictions, switching/removal, gameplay exclusion,
  existing Sprig gear, Wardrobe behavior/layout, hero-motion contracts, save merge and localization. Unity scripts
  compile against the project's .NET API stubs.
- Actual preview fixtures 112–116 passed on 16:9, 19.5:9 and 21:9: free shop taps, no spending, app restart, removal,
  both Home variants, Twig Wardrobe and a solver-completed level win. Existing fixtures 108, 26, 74, 75 also passed
  on all three shapes: Sprig gear, paid shop confirmation, scroll/cancel and confirmed Wardrobe purchase.
- Preview navigation checks passed for 11 cold destinations and delayed resources, including full-color Twig Home / cosmetics.
  These are desktop runtime checks, not measurements on a physical Android phone. No APK was built here.
- `node tools/heroanim/check.mjs` verifies source FBX/recipe hashes, all runtime images, generated crop data and the
  saved reference images. Five runtime RGBA8 PNG images total **479,663 bytes**. FBXs, GLB, references and these
  screenshots never enter the APK.

Reproduce from the repository root:

```bash
blender -b --factory-startup -t 2 --python-exit-code 1 --python tools/heroanim/prepare-twig-gear.py
node tools/heroanim/bake-twig-gear.mjs
node tools/heroanim/check.mjs
dotnet run --project playtest/preview -- --frames 112,113,114,115,116 --out content/work/twig-game-review
```

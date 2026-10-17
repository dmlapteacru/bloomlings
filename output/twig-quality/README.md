# Twig quality pilot — 2026-10-17

`before-after.png` compares actual delivered sprites at the same display size. `comparison.html` is a self-contained
comparison with acorn, cap and bare variants. The left column is revision `21568621`; the right is the quality pass.
No image-generation illustration is substituted for the real model render.

Five runtime PNGs: 124,390 → 479,663 bytes; unchanged delivery dimensions. All five decoded RGBA images together are
1,872,960 bytes before platform overhead. The current pass reduces surface noise through render normals, removes
palette/dither noise, adds studio lighting and distinct roughness, and uses 3× supersampling with alpha-safe reduction.
Source FBXs / body positions / weights / topology stay intact; the previous derived acorn fit is retained.

Validation: 25 focused client tests passed; five actual Twig screen fixtures passed on 16:9, 19.5:9 and 21:9. The five
images went through the APK asset packer and six fixtures (including existing Sprig gear) passed with its shipped bytes
on 19.5:9. Preview checks verify all five Twig images' dimensions / decoded pixels against their masters, 11 cold
destinations (including new Twig states) and 100 live-preview frames with delayed effects. `heroanim/check.mjs` passes
for source hashes, runtime output / crop data, the 900 legacy frames and existing art. No APK or device run was made.

Current actual game captures are in `../twig-in-game/`. Workflow, limits of the provisional front projection, and the
next all-angle UV / surface work: [hero-surface-quality.md](../../product/plans/hero-surface-quality.md).

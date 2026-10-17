# Original Twig restored — 2026-10-17

Actual screenshots from the game client:

- [Home](113-twig-restored-home-19.5x9.png)
- [Wardrobe](115-twig-restored-wardrobe-19.5x9.png)
- [Solved-level win](116-twig-restored-win-19.5x9.png)

The original Twig animation set from `Heroes.glb` is active again. Its model, texture, frame pixels and motions were
not rebaked. The replacement FBX pilot no longer overrides Home / celebration rendering. The two free headwear items
fitted only to that replacement are removed from the catalog until compatible versions exist. Old saved item IDs are
retained but resolve to no worn hat, so they cannot bring the replacement back or paint an unfitted hat on the old
hero. Other items, progress and Sprig's modular equipment retain their behavior.

Twenty-five focused tests passed, including the original motion set, obsolete saved headwear and Sprig gear. Preview
fixtures 108 and 112–116 passed on 16:9, 19.5:9 and 21:9. Recorded sprite calls prove Home / Win draw `twig-idle-*` /
`twig-win-*` rather than `twigmod-*`. Navigation checks passed for 10 cold destinations and 100 live-preview frames
with delayed effects; source / frame / background checks passed. No APK was built here.

The proposed bald body and two head references are in
[`tools/heroanim/references/twig-legacy/`](../../tools/heroanim/references/twig-legacy/). These are for future modeling,
not the assets currently drawn in these screenshots.

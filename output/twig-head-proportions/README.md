# Twig: narrower shared head

Owner feedback, 2026-10-17: the newly integrated Tripo head looked too wide / flat compared with the previous Twig.
The 448×504 runtime cell preserves its aspect ratio; the new base face itself is broader. The derived render copy
now applies X scale 0.88 to `TwigHero_HeadPivot`, with ears, mouth and both hat groups following together.
The body, camera, height and foot line remain unchanged. No runtime sprite scaling change is introduced.

`comparison.png` places equal-scale crops of actual Home captures side by side. `home-before.png` /
`home-after.png` and `wardrobe-before.png` / `wardrobe-after.png` preserve the complete page captures. The left side
is commit d1496635's integration; the right side is the corrected bake. This is a rendered game comparison, not
an image-generation mockup. Current Shop / cap / victory captures are in `../twig-tripo-in-game/`.

The original Tripo source, fitted GLBs and existing Blender import ZIP remain intact. The adjustment is recorded in
`look.headWidthScale` in `tools/heroanim/bake-twig-tripo.mjs`; apply the same shared-head X scale once in the owner's
master later if adopting it there. Body pixels below the head (logical Y>=390), total height and foot line match
the preceding bake exactly. At logical Y=230, visible head width changes 249→220 px (bare/cap), 255→224 px (acorn).

Verification: `heroanim/check.mjs` and `twig-tripo-check.mjs` passed; preview fixtures 112–117 passed at 19.5:9,
including actual Shop taps / save reload / remove, all three Home states, Wardrobe and a solved L13 victory.
The preview also checked 900 legacy frames + 5 full-color Twig sprites, 11 cold destinations over 117 staged frames,
and 100 live-effect frames with delayed dependencies. No APK is built here.

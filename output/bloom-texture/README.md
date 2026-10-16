# Bloom texture prototype — 2026-10-16

`comparison.png`: actual current production frame on the left; candidate render on the right, large and reduced.
`breathing-and-jump.mp4`: the same comparison over all 132 frames, 24 fps: original 4 s Breathing + 1.5 s SmallJump.
`before.png` / `after.png`: transparent first-frame renders at the same 448×504 cell size.
`comparison.html`: editable review sheet; image paths are relative to this directory.
`verification.json`: provenance, geometry/animation checks and measured packed-frame payload.

The candidate uses a cleaned frontal paint reference baked onto the existing UVs, neutral lighting and less dithering.
It still uses the owner's actual original mesh and skeleton, not a separately generated animated illustration.
The owner subsequently approved installing the candidate in the game. Bloom’s 132 production frames are now
replaced by this appearance; the comparison retains the historical before images. These review artifacts and source
textures live outside APK asset globs. See `integration.json` for adoption checks.

Measured with the actual lossless asset packer, using all 132 frames cropped to the same production bounds:
current Bloom 6,612,519 bytes; candidate 5,716,494 bytes (13.55% less). This is Bloom's frame payload, not total APK size.
Decoded dimensions are identical. No APK was built. All production frame manifests, five Home layers, 19 backgrounds and the separate 241-frame Sprig animation lab
pass integrity checks. The 768 production frames belonging to the other three heroes remain unchanged.

This is a first appearance pass: eyes/lips inherit the existing model geometry and some original baked shading;
large turns/rear surfaces need further painting before adopting a final full-view material.
Source, prompt and reproducible commands: `../../tools/heroanim/textures/bloom-study/README.md`.

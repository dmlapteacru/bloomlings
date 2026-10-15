# Home fountain and light refinement (2026-10-15)

`before-after.jpg` compares actual renders of the same progressed Home state before and after the refinement.

Four Home images changed: the courtyard, transparent basin, separate lotus and transparent front corners. The basin and corners now use light ivory stone, mint-turquoise water and calmer jade foliage. The background has readable broad garden forms and gentle atmospheric distance instead of obvious bokeh blobs. Home's extra blur and 0.86 darkening pass were removed; its source light now reaches the game as authored. Other page backgrounds and the original hero/shadow placements are unchanged.

Dimensions are unchanged: garden 852 × 1846, basin 804 × 413, front 852 × 250, lotus 296 × 217. The front middle opening remains transparent. The four runtime files add about 232.5 KB before Android lossless packing. No additional textures or animation frames were added.

Validation: background/layer source hashes, original canvas/placement, transparent front opening, Android packed-image dimensions and alpha, Home render checks on three phone shapes, focused Home/hero tests, and Android C# compilation. No APK was built.

Source/provenance: `tools/artgen/models/home-fountain-2026-10-15.md`; deterministic finish: `node tools/heroanim/backdrops.mjs`; verification: `node tools/heroanim/check.mjs`.

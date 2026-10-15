# Fruit gameplay backgrounds — 2026-10-15

The owner requested three additional fruit-themed gameplay gardens. OpenAI ImageGen generated these from the project’s
owned `tools/heroanim/backgrounds/gameplay-orchard.jpg` reference. They preserve the Shop-inspired jade/teal/mint palette,
with a quiet center for the board, restrained fruit accents at the margins and warm ivory stone at the bottom.
The existing peach orchard remains; these add distinct pear, citrus and plum compositions.

Production sources in `tools/heroanim/backgrounds/` are opaque RGB, normalized to 852×1846, JPEG quality 90, optimized,
without chroma subsampling. The unchanged `backdrops.mjs` finish applies a slight blur and 0.86 color factor and records
source/runtime hashes in `tools/heroanim/backdrops.json`. Unity Resources and both playtest asset globs include the runtime
JPEGs automatically. Original generator PNGs remain execution artifacts and are not packed into the game.

| Scene | Generator output | Original PNG SHA-256 |
|---|---|---|
| Pear Garden | `exec-d8d462fa-2478-497c-870e-7f2de73a0051.png` | `92ef6e25fb525512483fb42d74ab29750cdd84ec6ef4e5224bf3cfb0f64b2128` |
| Citrus Grove | `exec-d82d936a-8121-4183-9794-dc107862e70b.png` | `4b510d91d2d7d600694bca92584c909d2064a83142e5041266d861bea3632738` |
| Plum Garden | `exec-e1e3d3ab-fb4e-4ef4-a1dd-3deda4fd57a9.png` | `3770276269e635b6f082f0106b933485d87f17946b4267b76e40623a50f99979` |

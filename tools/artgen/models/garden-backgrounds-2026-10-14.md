# Bloomlings garden backgrounds — 2026-10-14

Generated with OpenAI ImageGen for this project at the owner's request. The owner approved the six-scene concept sheet in conversation, then asked for implementation on `codex/shop-garden-body`. These are project-generated assets, with no imported third-party scene or stock material.

References: the project's existing `Art/Shop/Resources/Shop/background.png` and the approved concept sheet (generator output `exec-e61256b9-93f5-4aad-afdb-74c7275f144e.png`). The original Home fountain, lotus, front bushes, shadows and all heroes remain unchanged.

Art direction: polished rounded casual game garden; shaded teal and jade with mint atmospheric depth, warm ivory stone and honey wood, sparse pink and ivory edge blossoms. Distinct locations: open courtyard, wooden dressing pavilion, pond terrace, ceremonial avenue, outdoor botanical archive, shaded gameplay clearing. Reserve a quiet broad center for UI and board. No saturated blue sky, tall central arches, busy center foliage or baked UI. Home has no painted fountain because the existing layered fountain remains in front. Four gameplay theme variants share the palette with pond reeds, orchard fruit or silver-mint moonlight at the edges. Victory uses a low circular stone dais (its standing point at 0.615 image height).

Production preparation: opaque RGB, normalized to the existing 852 × 1846 canvas, JPEG quality 90, optimized, no chroma subsampling, using Pillow. The committed JPEGs in `tools/heroanim/backgrounds/` are the authoritative production inputs. `node tools/heroanim/backdrops.mjs` deterministically applies the existing slight blur (sigma 3/1080 of width) and 0.86 color factor, writes the Resources images, and records source/output SHA-256 in `tools/heroanim/backdrops.json`. Original generator PNGs and the concept sheet are retained in the execution artifacts rather than bundled in the game.

| Production source | ImageGen output | Original PNG SHA-256 |
|---|---|---|
| `tools/heroanim/backgrounds/home.jpg` | `exec-d091b23f-04a4-4859-9ddf-6a708d679bae.png` | `1c9e5848869f8cfb79dbb5ddd0107f950184d2c2549118fd527efad669e1da59` |
| `tools/heroanim/backgrounds/wardrobe.jpg` | `exec-9b1e867b-33d6-45b6-b489-73048ecf511e.png` | `384c3a8695667e4ff0c07920e73616931aa6e4624e7e5cf01d079b764e254d93` |
| `tools/heroanim/backgrounds/profile.jpg` | `exec-002b0d73-fec0-446f-b9e9-4a1e4e43540f.png` | `a6a5c7f4a67668db7fd65d9a6454fc8d35f3344727aaca30d0ce646bc24dacc9` |
| `tools/heroanim/backgrounds/leaderboard.jpg` | `exec-2388995d-2f6e-4c01-90a1-79ef974aa131.png` | `4113fab0ae2762cf3d0e7aecc4d53df3f42c740bc31f2bca24971158030a35cf` |
| `tools/heroanim/backgrounds/collection.jpg` | `exec-02f515d0-27b3-4c50-9a18-e7db0669dbb7.png` | `21b3143a58b4b5dfdae4ea8b70be76270937f46ce625e1d21a0a9b61a87b7b4d` |
| `tools/heroanim/backgrounds/gameplay-daylight.jpg` | `exec-adebce60-4eab-405d-8d19-2ee23b88a0f9.png` | `e64ab59ddf5d5f8fe411645e1d3549c79466ade021ed3d2fbe1df3abda5461e6` |
| `tools/heroanim/backgrounds/gameplay-moonlit.jpg` | `exec-9d3c0484-b860-492f-90ee-98fb9e7b555f.png` | `555dfac2659e88ec0d8405656cf5e88ac98bb5a5aa127c547ba29f0a964ad90e` |
| `tools/heroanim/backgrounds/gameplay-orchard.jpg` | `exec-65f627d6-f065-4848-9425-c689b6fa5432.png` | `9d4005d4f4b764b6933ebb6051efec2585ade03553a5f1a7b587c3deb70ce3f2` |
| `tools/heroanim/backgrounds/gameplay-pond.jpg` | `exec-6feba401-1b93-45f3-bea6-50e3e25656dd.png` | `f67792ac9031e4f8be84401dbeae603df578bdb536884dbeb81579cb391a8aed` |
| `tools/heroanim/backgrounds/win.jpg` | `exec-ebaa23a0-648c-4424-b95c-e4395d170d06.png` | `e83586fc5211a586b4bf0289227fa172801769161c47dec083b832e5cd489a0f` |

Runtime files use exactly the same filenames under `client/Assets/Bloomlings/Art/Backgrounds/Resources/Backgrounds/`. The Android and preview project globs include them automatically. No transparent full-screen duplicates or animation frames were added.

## Additional gameplay scenes, 2026-10-15

Owner requested two more backgrounds matching the approved palette. ImageGen edited the owned daylight reference into Fern Glade (`exec-3af0f7cf-561e-425c-b754-08d2ff6a6578.png`) and Willow Brook (`exec-69353082-d8fd-449d-84a2-1a69955b5792.png`). Both reserve a broad featureless teal center. Production sources are opaque 852×1846 optimized JPEGs, prepared with the same normalization and deterministic finish described above. Runtime images total about 370 KiB; full-resolution generator PNGs are not APK assets.

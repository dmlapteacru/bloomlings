# Garden objects v2 — 2026-10-14

OpenAI ImageGen artwork generated for Bloomlings for the user's approved Home option A.
Reference: `product/previews/home-promo-concepts-v1/home-options-a-b.png`, **left option**.
No external stock images. SHA-256 and original generated-image filenames are in `garden-promos-v2.json`.
Originals remain in `/workspace/generated_images/`; production derivatives use transparent RGBA PNG.

Prompts: a short ivory sandstone pedestal with blank honey-wood caption, two small white flowers and restrained leaves;
a friendly front-facing green leafy Sprig with one hand raised; a closed lavender scrapbook with gold corners and one
pink lotus; its open ivory-page pose; exactly one warm golden four-point sparkle; and a honey-wood 3×3 leaf puzzle board
with an empty tile, rising-sun emblem and blank caption. All isolated on transparent backgrounds in the game's soft 3D style.

Production: ImageMagick Lanczos downsampling with alpha, metadata stripped, PNG32. Platforms 724×543, props and challenge
512×512, glint 128×128. Both platforms share identical content. Existing crossed AD sign and flower stamp are reused.
The Android build also runs the existing verified lossless asset packer; no new animation-frame sheets are added.
Labels remain live Lilita One text; none are baked into the sprites. Native and Android/Skia share `HomePromo` geometry.

## Revision 3 — more expressive scenes, 2026-10-14

User requested the sign-shoving gag back and a clear physical stamping action. New OpenAI ImageGen edits use the
previous purple album as reference: a real honey-wood stamp handle with lavender collar, gold foot and purple rubber pad;
and an open landscape album with blank ivory pages, no printed mark. Source names/SHA in the JSON manifest (updated to
the current files). PNG32 production derivatives 512×512. Existing original Sprig pushing pose is copied byte-for-byte
from repository HEAD as `promo-noads-sprig-push.png`; its owner provenance remains
`tools/artgen/models/owner-pictures.md`. `promo-daily-imprint.png` is a byte-for-byte copy of the existing owned
`Icons/currency-lotus.png`; runtime scale flattens it onto the page. No generated animation frames.

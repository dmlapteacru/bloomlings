# Illustrated store body

Generated for Bloomlings with the built-in OpenAI ImageGen tool in this task, using the owner's generated shop concept as the visual reference. The PNG files are project-generated art. No third-party game artwork was used as image input. Existing Bloomlings booster icons remain in use.

Reference: `exec-3d43445f-43d1-4bef-a6d9-102a2cd1f019.png` supplied by the owner in the conversation.

## Asset prompt specifications

- **offer-atlas.png:** transparent 3-column, 2-row sheet, with equal cells; polished cartoon garden shop illustrations. Top row: smiling berry character and basket of pink flower petals, orange television with a crossed-out ads symbol, cloth pouch full of pink petals. Bottom row: wicker basket of pink petals, purple and gold treasure chest full of pink petals, blank golden leaf badge. Separate objects, generous transparent space, no prices, quantities or UI labels. Output `exec-db90a3e5-2ca0-4030-b0fb-47bd318cfc9f.png`.
- **background.png:** soft emerald and teal garden background, foliage and flowers around the edges, luminous quiet center, cartoon mobile game finish, no UI, lettering or cards. Output `exec-dec66d9e-c608-4fcd-ae40-f40014ba86e8.png`.
- **price-button.png:** isolated horizontal green cartoon purchase button, bright lime face, upper highlight, dark green lower lip, rounded corners, transparent background, no text, symbols, leaves or currency; suitable for nine-slice stretching. Output `exec-078ebb89-5ce3-4b4b-a6de-f796b5d23fbf.png`.

The source PNGs were copied unmodified into `Resources/Shop/`. `manifest.json` records their SHA-256 hashes. Atlas cell extraction and button cropping happen in code, preserving source alpha.

## Rendering contract

- `ShopArt.Get` creates sprites from textures at runtime. The price-button source crop is `(109, 100, 1985, 505)` in top-down coordinates; nine-slice borders are left/right 350 and top/bottom 190 pixels.
- Unity uses `Image.Type.Sliced`. Border scale follows button height; prices are separate localized TMP labels, using Lilita One under SIL OFL 1.1.
- Card, ribbon and booster-price capsule surfaces remain procedural. Offer illustrations and the green money-price button are bitmap assets. The full-screen reference is never used as a flattened UI.
- `ShopArtImporter` preserves original texture dimensions, disables NPOT resizing and mipmaps, and clamps edges. This matters because slice rectangles use original pixel coordinates.
- Shared `ShopBodyDesign` supplies the layout to Unity and the Skia/Android playtest. The latter renders the same nine source regions for the price skin.
- Money offers show a coming-soon message. No platform product or entitlement is created by these preview buttons. Petals purchases retain existing confirmation and economy logic.

Generated assets may be used as part of this project. Font licensing is recorded separately in `client/THIRD_PARTY_NOTICES.md`.

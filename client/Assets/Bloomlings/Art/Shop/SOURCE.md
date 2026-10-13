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
- The Best Sale card is now a complete illustrated bitmap base (`hero-card-v2.png`), including the berry, basket, cream panel and dimensional red wraparound frame. Text, reward icons, counts and the price button are separate live layers. Other card surfaces and booster-price capsules remain procedural.
- `ShopArtImporter` preserves original texture dimensions, disables NPOT resizing and mipmaps, and clamps edges. This matters because slice rectangles use original pixel coordinates.
- Shared `ShopBodyDesign` supplies the layout to Unity and the Skia/Android playtest. The latter renders the same nine source regions for the price skin.
- Money offers show a coming-soon message. No platform product or entitlement is created by these preview buttons. Petals purchases retain existing confirmation and economy logic.

## Revision 2: vertical content and larger illustrations

The store body is one vertical list. Unity uses `ScrollRect` with a `RectMask2D` viewport; only its content is moved. The existing header, wallet, tabs and bottom navigation are siblings outside that viewport. The playtest uses continuous vertical gesture deltas and the same viewport clip and layout. The Shop tab has no horizontal page turns or page arrows; Cosmetics and Animations keep their existing navigation.

Selected new ImageGen assets (copied without image processing):

- `booster-atlas-v2.png`: four regenerated icons in a transparent 2-by-2 sheet: blue plus coin, green shuffle coin, yellow return coin and pink flower. Shop-only use; gameplay icons are unchanged.
- `hero-card-v2.png`: full illustrated Best Sale card base. Selected output `exec-32d41885-6ef6-4f67-85a3-0168ba21810c.png`. Two alternate generated variants were reviewed and discarded.
- `noads-basket-v2.png`: basket of pink Petals with a prominent No Ads badge; used by the 1,200-Petals No Ads bundle.

Exact generation prompts and source output filenames are in `prompts-v2.json`. The source reference is the owner's generated concept. All runtime files, dimensions and hashes are recorded in `Resources/Shop/manifest.json`.

Green price buttons remain nine-slice skins with narrower proportions and greater vertical padding. Booster prices now use larger white lettering with the same brown outline and extrusion as other shop prices. The wider No Ads bundle and all offer illustrations use more of their cards' area.

Generated assets may be used as part of this project. Font licensing is recorded separately in `client/THIRD_PARTY_NOTICES.md`.

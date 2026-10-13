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

## Revision 3: frames and featured-card alignment

Group headings and green dividers are removed. Procedural `ShopBodyRaster` rims distinguish Petals (gold), boosters (blue), bundles (pink) and No Ads (mint). These are code-generated UI surfaces. The first illustrated card retains its own red frame.

Code-built bundles now contain a framed reward inset with padding around the booster grid. The featured grid and footer overlays use the aspect-fitted bounds of the 1704-by-923 card PNG. This corrects the previous mismatch between the outer layout rectangle and the artwork's fitted rectangle. The featured booster icons fit inside the illustrated inner panel, and the caption and price button are raised into the red footer.

The leaf carries live, rotated `BEST SELLER` text and three alpha-tinted silhouette layers behind the original leaf to create a luminous rim. No source PNG is modified. General outlined lettering is heavier; money prices use a dark green outline. Booster prices use brown text without an outline and a more rounded, visibly framed price capsule.

## Revision 4: subtle depth and consistent artwork placement

Code-built cards, reward panels and price capsules now use a soft, even perimeter shadow and a shallow bevel. They no longer inherit `UiRaster.RaisedRow`'s lower slab, which was 6% of the card height.

Mixed offers reserve separate picture and reward-panel columns, with space around each panel. Quantity labels follow the fitted square booster icon's lower edge so they overlap it slightly even in narrow panels. Standalone booster cards follow the compact proportions of the owner's booster-row reference: the row is 0.245 of the content width, and icons occupy about half of each card's width.

The two tilted Best Seller lines share their horizontal center, weight and tighter line spacing. Petals illustrations use measured sprite crops with a small transparent gutter, aligning the visible artwork bottoms before quantity labels are overlaid. The source PNGs are unchanged. `ShopBodyDesign.AtlasCrop` supplies the same rectangles to Unity, Skia and Android.

## Revision 5: smooth rounded rims and purchase padding

Procedural card rims now use a continuous curved profile with directional highlights, soft shading and 2-by-2 sampling per pixel. This replaces the hard concentric color steps. Reward panels have a thicker rounded rim and a gently raised face; booster-price capsules use the same molded treatment. The narrow perimeter shadow remains.

Code-built bundle buttons are lifted to leave 5.5% of the card height below them, with additional left padding for the artwork. Booster-price capsules leave 7.5% below them. The compact booster row is now 0.255 of the content width, preserving button height with the extra padding. Existing illustrated assets and nine-slice money buttons are unchanged.

## Revision 6: compact reward insets and centered badge

Code-built reward panels now derive their height from two rows of square booster icons and overlapping count labels. Their outer content padding is equal in physical pixels on all four sides, avoiding excess vertical whitespace at different card widths. Bundle panels have a larger right margin; the 1,200-Petals No Ads card retains its existing right margin. The featured illustrated reward panel keeps its existing layout.

The leaf label is now `BEST SALE`. Both centered lines are positioned along one rotated center axis, retaining the existing tilt and glow. Source PNGs are unchanged.

## Revision 7: booster price-button volume and currency placement

Booster-price capsules have a brighter upper-face highlight, a soft lower-face shade and a slightly thicker molded rim. This extra face curvature is specific to the price capsule; reward panels retain their previous treatment.

`ShopBodyDesign.ButtonPetal` supplies a larger square currency icon centered between the inner left rim and the price-label region. Unity and the Skia/Android painter use the same helper and label region. The two tilted `BEST SALE` lines move left together onto the center of the leaf artwork. Source PNGs are unchanged.

## Booster alignment, gameplay revision 5

`ShopBodyDesign.BoosterCrop` supplies measured opaque-subject bounds (alpha >= 160 plus a 3-pixel gutter) for the four booster atlas cells. Unity, Skia and Android slice these rectangles, so the return/flower icons no longer sit above center due to excess transparent space below them. Gameplay uses a centered 80%-of-card icon box; shop reward icons keep their existing boxes. The source PNG is unchanged.

Generated assets may be used as part of this project. Font licensing is recorded separately in `client/THIRD_PARTY_NOTICES.md`.

## Cosmetics and Animations catalog (2026-10-13)

The approved concepts are in `product/previews/shop-tabs-concepts-v1/`. Production assets were generated with the **built-in OpenAI ImageGen tool**, with those two concepts as style references. Existing heroes are reused; no new heroes were generated.

- `Resources/Shop/catalog-frames-v1.png`, 2172 × 724 RGBA: pink Cosmetics, blue Animations, green/gold selected Animations skins in one transparent atlas. Selected output: `exec-effad4dd-db16-426a-a09d-121d6677bb74.png`.
- `Resources/Shop/catalog-accessories-v1.png`, 1254 × 1254 RGBA: Bucket Hat, Bell Cap, Bramble Frame and Watering Can Badge. Selected output: `exec-3cb0a7a0-447c-4123-835b-759ad1e8443f.png`.

Both source PNGs are copied **unchanged**, preserving transparency. Runtime crops and nine-slice border ratios are defined in `ShopCatalogDesign`. Unity, Skia and Android load the same crops. Card faces contain no baked labels, buttons, price values or heroes.

The Cosmetics family row is fixed outside the vertical viewport. The catalog contains Default plus the actual items still for sale for the selected family. Animations displays six cards and a wide Confetti Parade card; each uses the existing clearing choreography and hero art. Catalog-only board geometry, tile colors and preview phase do not change gameplay defaults. Purchases retain the current service prices, confirmation, ownership and level gates.

### Exact frame prompt

```text
Use case: stylized-concept. Production UI frame-skin atlas for Bloomlings. Reference1 shows approved Cosmetics cards, reference2 approved Animations cards; use only card MATERIALS AND SHAPES, not characters or board content. Generate ONE transparent atlas containing exactly THREE equal SQUARE blank card skins in one horizontal row, equal square cells with small transparent gutters, front-facing, full silhouettes visible. Left: cosmetics card, narrow warm petal-pink/coral raised rounded rim, ivory highlight on its inside edge, creamy pale parchment interior; small lush green leaf + white daisy + pink petal decoration anchored at BOTH lower side corners, exactly like cosmetics reference, leaving center and bottom middle entirely empty for dynamic hero, live title and separate purchase button. Middle: animation card, narrow soft azure-blue raised rim with ivory contour highlight, same warm cream interior, no flowers or other ornament. Right: chosen-animation card, narrow rich leaf-green raised rim with restrained pale golden glow just outside, same cream interior, no flowers or extra ornament. Each skin: smooth broadly rounded corners radius about 12% of width, subtle volumetric lower edge, tight evenly distributed soft shadow, gentle warm interior light, consistent thickness and scale. Cream center uninterrupted; NO inset preview well, NO inner box, NO baked price button, NO numerals, check marks, locks, lettering, heroes, board cells, item or animation illustration, currency, watermark or scene background. Flower clusters stay within the bottom corner regions: at most 20% width and 23% height, with decorative petals above the rim, leaving a lot of blank cream middle. All three frames the exact same square dimensions. Polished rich 3D cartoon garden-game UI, matching references closely. Transparent background. Wide landscape atlas about 3:1 total aspect ratio.
```

### Exact accessory prompt

```text
Use case: stylized-concept. Transparent production accessory atlas for Bloomlings garden game. Reference image is the approved Cosmetics screen; match its warm polished cartoon material, especially the Bucket Hat and Bell Cap. Produce exactly FOUR standalone ITEMS in a 2 columns x 2 rows atlas, equal square cells, generous transparent gutters, consistent front-facing soft 3D cartoon rendering. NO CHARACTERS OR HEROES anywhere. Exact order: top-left a neutral taupe cloth BUCKET HAT with a soft round crown, slightly drooping brim, small stitched seam and a tiny green leaf emblem, lower opening empty so it can sit on an existing character's head. Top-right a creamy white BELL FLOWER CAP: a downward bell-shaped ivory flower with softly scalloped petals at its open lower edge, small green stem and leaf curving up from the top, wearable as a cap on an existing hero, as on reference Bell Cap. Bottom-left a BRAMBLE PROFILE FRAME: a rounded square empty center, warm sculpted brown twig border with a few thorn tips and small green leaves, small pale daisies at two corners, a cohesive thin frame, see-through interior. Bottom-right a WATERING CAN BADGE: small toy-like muted pale blue watering can with a cream/gold flower emblem, on a subtle cream medallion with fine golden rim. Keep hat silhouettes wide enough for top-of-head overlays; neutral fabric/ivory base colors with green sprig detail. Bright upper-left lighting, soft tiny shadows, smooth rich game texture and readable silhouettes at small size. Do not include heads, faces, eyes, bodies, people, creatures, lettering, prices, numeral, background scenery, card or purchase button, watermark. All four centered with no overlaps. Preserve full objects. Genuine transparent background, including the frame's center.
```

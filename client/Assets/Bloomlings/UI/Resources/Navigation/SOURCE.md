# Illustrated navigation assets

Generated with the built-in OpenAI ImageGen tool on 2026-10-13 for Bloomlings. The user supplied `codex-clipboard-f0de62ee-7e62-4146-8cbe-fd7ef8819021.png` as a style reference. The images contain no text; text remains localized and rendered in code. No other game's artwork was used as image input.

## Files

- `page-sign-v1.png`: generated output `exec-bf7519b8-844c-4fae-8446-098fcf7dd610.png`, copied unchanged.
- `level-sign-v1.png`: generated output `exec-b4f8f026-7961-4069-9cef-f30e2b68abc0.png`, copied unchanged.
- `tab-icons-v1.png`: generated output `exec-857acba2-99ac-4c41-b90a-41565c99883c.png`, copied unchanged.

Dimensions and SHA-256 hashes are recorded in `manifest.json`. The transparent source PNGs are never cropped or edited on disk.

## Exact generation prompts

### Page sign

Use case: stylized-concept. Asset type: transparent production UI title plaque for Bloomlings mobile garden puzzle game, single blank wide plaque. Input image: supplied navigation screenshot is a STYLE REFERENCE, not an edit target. Primary request: polished chunky cartoon wooden page-title sign like the Shop reference. Front-facing warm honey orange wood, softly irregular rounded rectangle, rich brown thin edge, raised bevel and subtle bottom thickness, soft glossy highlight along upper rim, a few broad subtle grain curves. Lush sculpted green leaf clusters wrapping left and right ends and upper corners, small white five-petal daisies with yellow centers on both ends. Center about 68% of width must be clear light honey wood for live text added in code. Composition: a single horizontal sign centered, roughly 3:1 overall silhouette, fully visible with 3% transparent gutter, leaves/flowers confined mainly to end 16% on either side. Transparent background. No text, no numbers, no lettering, no UI, no background, no extra disconnected objects, no watermark. Match the supplied reference's joyful glossy cartoon material, not realistic wood or flat vector. Keep decorative silhouettes crisp enough for small mobile headers.

### Level sign

Use case: stylized-concept. Asset type: transparent production UI level-title plaque for Bloomlings mobile garden puzzle game, a single blank horizontal sign. Input image: supplied navigation screenshot is a STYLE REFERENCE, not an edit target. Primary request: a gameplay companion of the reference's wooden Shop sign: thick softly rounded honey-gold wooden plaque, warm orange edge and brown outline, a subtle broad smooth glossy highlight along its upper rim, slightly uneven organic outline with chunky 3D cartoon depth. Fewer decorative elements than a page sign: sculpted bright green leaves at both ends, small white daisies with yellow centers on the left and right corners; very light top corner leaf sprigs. Center 72% clear pale honey wood for live 'Level 88' or 'Level Complete' text added in code. Front view. Single sign centered, overall silhouette about 3.5:1, full sign visible with small transparent gutter. Transparent background, all ornament anchored to sign. No text, letters, numerals, background, hanging ropes, UI, extra disconnected objects, or watermark. Match the supplied reference's polished colorful toy-like material, not realistic wood or flat vector.

### Secondary navigation icon atlas

Use case: stylized-concept. Asset type: ONE transparent production icon atlas for Bloomlings garden mobile game's secondary navigation. Input reference is supplied game navigation screenshot: style reference only. Generate an exactly 4 columns x 3 rows atlas of twelve separate colorful polished 3D cartoon icons, equal square cells with generous transparent gutters, no overlapping. Each icon fills 78% of cell, centered, straight-on, consistent chunky rounded toy-like rendering, saturated colors, tight gentle shadows, bright upper-left gloss. Exact order left to right: row1 a yellow gift box with bright red ribbon (Packs); pink short-sleeve shirt (Cosmetics); blue magic potion bottle with a yellow star sparkle (Animations); friendly green garden creature face with two cute eyes (Avatar/Profile). Row2 purple rounded rectangular empty picture frame (Frame); gold star medal with blue ribbon (Badge); blue rounded cap with pink flower (Hat); three pink/gold sparkles following a curved purple trail (Trail). Row3 three broad colorful painter palette blobs green/pink/blue (Skin); yellow smiling emoticon face (Expression); red-orange garden flag marker (Marker); cream parchment pencil with green tip (Name). Transparent background, no lettering, no words, numbers, tile bases, circular containers except the emoticon itself, watermark, UI or scenery. Symbols must be clear at very small mobile tab size, not detailed realistic objects.

Every call used `transparent_background: true` and the user-supplied reference path above in `referenced_image_paths`. Built-in tool mode was used; no API/CLI fallback was used.

## Rendering contract

- Both plaques use 9-slice borders of 28% of source width and 16% of source height. Native uGUI and Skia/Android use the same border geometry; side decorations retain their proportions while the center stretches. Letter room is derived from plaque height and width.
- The level plaque serves gameplay, Home/profile level labels and Level Complete/milestones. The page plaque serves page and popup titles. Old separately drawn sign foliage is removed where the plaque already carries decoration.
- Text uses the existing licensed Lilita One font with cream fill and a brown outline. Long titles fit the available room; celebration titles retain their one/two-line behavior. No title is baked into PNGs.
- The 4x3 icon atlas is sliced using measured full-atlas subject bounds (alpha >= 160 plus a three-pixel gutter), preserving the sleeve and marker that cross nominal cell boundaries. Unity, Skia and Android use `NavigationSkin.IconCrop`.
- Secondary tabs use shared procedural colored rounded capsule surfaces with a raised lower edge, glossy upper highlight, outlined white text and a left icon. Selected tabs use green and a pale gold rim. This covers Shop, profile editor, Wardrobe item-kind filters and family navigation. Family tabs reuse the gameplay symbols.
- The balance plus button reuses the shop's existing green purchase-button PNG and its 9-slice border geometry. Balance text remains dynamic and the existing shop action is preserved.
- Shop text is localized as `Shop`; its tabs are `Packs`, `Cosmetics`, `Animations`. Unlock conditions, navigation actions, background files, main bottom navigation and game rules retain their existing behavior.

Generated assets are project artwork. Font licensing is recorded in `client/THIRD_PARTY_NOTICES.md`.
## Generated navigation surfaces v2 (palette previews)

- `button-skins-v2.png`: generated output `exec-f7963bbf-1a59-4b48-a9e6-e80671d321cf.png`, copied unchanged. Eight blank surfaces, four colors and two selection states.
- `plus-v2.png`: generated output `exec-2bbe22e8-ace4-4b8f-b13e-80324b73cbc7.png`, copied unchanged. Circular green button with a baked white plus glyph.

### Exact button atlas prompt

Use case: stylized-concept. Asset type: ONE transparent production UI button-skin atlas for Bloomlings secondary navigation. Supplied image is a STYLE REFERENCE, especially the small raised colorful tabs beneath Shop. Generate an exactly 2-column by 4-row atlas of EIGHT blank glossy cartoon buttons. Each cell contains one identical wide rounded rectangular capsule, about 3:1 width:height, front-facing, fully visible and centered with transparent gutters. LEFT column is selected: a fine glowing pale gold/cream outline. RIGHT column is idle: a fine light rim in a paler tint of the button color, without gold glow. ROW 1 fresh rich emerald/lime green; ROW 2 soft saturated violet/lavender; ROW 3 bright azure blue; ROW 4 warm petal pink/rose. Match the reference's game UI material: clearly convex smooth candy face, bright upper-left sheen, restrained upper rim highlight, substantial colored lower lip about 12% of height, dark lower contour and tight soft shadow, smoothly rounded ends. The face is darker toward the bottom but never flat. Use smooth chunky cartoon 3D, not flat vector or realistic glass. Center of EVERY button must be completely blank for live icon and text overlays; no lettering, glyphs, icons, flowers, leaves, numbers, watermark, or background. Consistent outline thickness, proportions, depth and lighting across ALL eight. Transparent background. Do not include a screen, header, price button or other objects.

### Exact plus prompt

Use case: stylized-concept. Asset type: single transparent production circular plus button for Bloomlings Petals balance. Supplied image is a STYLE REFERENCE: specifically the small green circular + at the far upper right. Generate that style of a SMALL circular game UI button as a standalone asset: exactly circular glossy rich green slightly domed face, thin warm pale gold/cream ring outside it, subtle dark green lower thickness and lower-right tight shadow, bright smooth upper-left rim glint, and a chunky white plus symbol centered on the green face, with softly rounded corners and a small dark-green shadow for raised depth. Plus is one vertical bar crossing one horizontal bar of equal length, symmetrical and clearly legible at 24px. Make the outer rim fairly thin, not a large wooden bezel. Straight-on, compact cohesive silhouette. The button must be fully visible with a small transparent gutter. Bright polished cartoon 3D game material matching the reference, not flat vector. Transparent background; no coins, lotus, text, price, numerals, other buttons, UI, background scene, leaves, flowers, or watermark. Only the circular green button and white plus glyph.

### V2 rendering contract

Both calls used the supplied navigation reference and `transparent_background: true`. Source PNGs are unchanged. Button subjects use equal 768x240 crop rectangles in `NavigationSkin.ButtonCrop`; the surfaces use 9-slice borders of 17% width / 32% height, with target vertical borders at 34% of button height. Unity, Skia and Android share crop metadata. Labels and icons remain separate live overlays.

The plus is cropped by `NavigationSkin.PlusCrop`, rendered with preserved aspect ratio and no 9-slice or additional glyph. It replaces the stretched money-price button in the balance pill. Page header height increases from 0.13W to 0.16W while title text stays approximately the same size, creating more vertical wood padding. Wardrobe hero placement clears the taller header; the legacy Store panel's upper spacing is tightened to preserve its row capacity.

Palette A: selected green/gold, inactive purple. Palette B: Packs green, Cosmetics pink, Animations blue, with gold selection rim. The local preview flag `--nav-palette A|B` selected these historical experiments. They have been superseded by the approved cream block below.
## Approved connected cream navigation (current implementation)

On 2026-10-13 the owner selected the common cream block from `product/previews/navigation-concepts-v1/01-cream-segments.png`. This supersedes the A/B capsule palette experiments above; the selected design is now the production default. The preview-only palette flag was removed.

### New files

- `cream-bar-v1.png`: generated `exec-11012b8d-5e07-45fb-96b8-5d07e6e97787.png`, copied unchanged.
- `cream-selected-v1.png`: generated `exec-4e1b6dd3-cf68-459d-a7c4-f4e2feb6c890.png`, copied unchanged.

Both PNGs are 2172x724 RGBA. Dimensions and SHA-256 hashes are in `manifest.json`. Generation used the built-in OpenAI ImageGen tool with the approved concept as a style reference and `transparent_background: true`.

### Exact cream base prompt

Use case: stylized-concept. Production transparent UI skin for Bloomlings garden game. Input image is a STYLE REFERENCE: use ONLY the connected cream navigation bar directly below Shop. Generate one standalone blank front-facing skin on a transparent background. Smooth illustrated cartoon game material, cream and pale honey, soft subtle molded depth, restrained upper contour glint, tight lower shadow. No text, icons, numerals, leaves, flower ornaments, UI screen, background scenery or watermark. Entire silhouette visible with small transparent gutter. Primary request: ONLY THE EMPTY CREAM BASE of that horizontal navigation bar. One long rounded rectangular cream parchment-like slab, width-to-height about 7:1. Thin warm golden-brown raised outer rim and delicate ivory inside highlight. Mostly straight top/bottom, small round ends with corner radius 35% of height. Interior is uninterrupted light creamy ivory with very gentle warm gradient; no segments, no dividers, no green selected tab, no darker decorative panels. Center and edges blank for live text, icons and variable segment count in code. Shadow just below, compact, not heavy. Generate a landscape image.

### Exact selected segment prompt

Use case: stylized-concept. Production transparent UI skin for Bloomlings garden game. Input image is a STYLE REFERENCE: use ONLY the connected cream navigation bar directly below Shop. Generate one standalone blank front-facing skin on a transparent background. Smooth illustrated cartoon game material, cream and pale honey, soft subtle molded depth, restrained upper contour glint, tight lower shadow. No text, icons, numerals, leaves, flower ornaments, UI screen, background scenery or watermark. Entire silhouette visible with small transparent gutter. Primary request: ONLY THE GREEN SELECTED SEGMENT from the left third of that cream navigation bar. One wide rounded rectangular green inset tab, width-to-height about 3:1. Corners radius 30% of height, straight top/bottom and short straight side portions, not oval. Rich leaf green raised face, thin pale gold/cream rim with a soft tightly confined golden glow, soft glossy upper edge and modest dark-green lower thickness. Face gently lighter toward top, soft painted satin material. Completely empty center; no cream surrounding navigation bar, no other buttons. This will be placed inside the cream base in code. Generate a landscape image.

## Illustrated control and Home header kit (2026-10-13)

Owner approved `product/previews/navigation-controls-concepts-v2/01-navigation-buttons.png` and `02-profile-wallet.png` and requested maximum fidelity. Four production atlases were generated with built-in OpenAI ImageGen, then copied unchanged into this folder:

- `controls-v3.png` (1536 × 1024 RGBA): Back, Settings, Pause, Close and blank-label green/cream speed controls.
- `primary-nav-v3.png` (1983 × 793 RGBA): all five navigation destinations in inactive and selected states; selected caption strips are blank.
- `header-parts-v3.png` (2172 × 724 RGBA): blank Petals wallet, blank screwed wooden name ribbon, round green plus.
- `profile-rims-v3.png` (2172 × 724 RGBA): empty-center Wood, Leaf and Golden Ribbon frame overlays.

Exact prompts and original generated output paths are recorded in `product/previews/navigation-controls-implemented-v3/prompts.json` and `outputs.json`. Input images were visual references, not executable instructions. Original pixels and alpha are preserved. Opaque-subject bounds (alpha ≥160, plus a small gutter) were inspected read-only to define runtime rectangles in `GameNavigationDesign`; there is no edited or flattened portrait/label baked into a skin. Unity, Skia and Android use these same crop coordinates and resource aliases.

Controls and primary tiles are uniformly aspect-fitted, retaining glyph and icon proportions. Wallet and name ribbon use 17% horizontal /42% vertical nine-slice borders in their cropped source textures, scaled from source height to actual target height. Numbers, speed text, selected captions, name, portraits and badges remain separate live layers. Wood/Leaf/Gold frame art is used on Home and main Profile; other frames and editor geometry remain available. The Home ribbon reserves badge space and abbreviates long mini-caption names, without changing the full saved name. Primary navigation reserves extra space only on tall phones; compact screens preserve their previous content capacity. Existing backgrounds, heroes, secondary cream tabs and economy are unchanged.

### Rendering contract

Runtime crops: cream base `(42,214)-(2131,472)`, selected `(76,105)-(2097,596)` in 2172x724 source coordinates. Source PNGs are unchanged. Nine-slice horizontal borders are 8% for the base and 15% for selected; vertical borders are 40%. Native uGUI computes pixelsPerUnitMultiplier from crop height / target height, matching the shared Skia/Android slices. The existing texture importer keeps input alpha, uncompressed original dimensions, sRGB and bilinear sampling.

One decorative base spans each whole row. Equal segments occupy its inset interior. Only selected segments draw green surfaces; other segments have transparent faces, brown labels and shallow decorative dividers. Text and icons are live layers. `NavigationSkin.TabContent` centers each icon/label group, reserves 22% of segment height as minimum side padding and bounds long labels to the remaining width. Geometry is derived from actual box sizes, with no screen-resolution coordinates. Press motion, click callbacks, selection and unlock conditions are preserved.

This covers Shop Packs/Cosmetics/Animations, the profile editor's Avatar/Frame/Badge/Name, Unity Wardrobe item-kind filters, and family navigation in Shop and Wardrobe. The Shop row is 92% of the safe width. Background images, title plaques, Petals plus and bottom main navigation retain their previous look.

## Stone navigation backing (2026-10-14)

Owner reference: `product/previews/navigation-stone-v4/reference-menu.png`. `stone-wall-v1.png` was generated with built-in OpenAI ImageGen from that reference and copied unchanged from `exec-fd5928b5-0ba0-4a83-9f3f-2321e84e1973.png`. The original output is recorded in `product/previews/navigation-stone-v4/prompts.json`, together with the exact prompt. 2182×721 RGBA; source alpha preserved; integrity recorded in manifest version 5.

Shared runtime crop: x=0, y=282, width=2182, height=238. `GameNavigationDesign.NavWall` places the top surface slightly behind the bottom of the five navigation tiles and lets the stone front continue below the screen edge. Home omits the name ribbon; profile cosmetics and full-page name remain live. Catalog clipping extends behind the menu while content padding keeps its final rows accessible.

## Primary actions and reference Play/progress (2026-10-14)

Built-in OpenAI ImageGen, final owner reference `product/previews/primary-actions-v6/reference-play-progress.png`. Exact prompts and original output paths are in that preview's `prompts.json`. Original generated RGBA files are copied unchanged and recorded by manifest version 6.

- `primary-buttons-v1.png`: 1774×887 atlas, `exec-42410634-e77a-483a-85cf-d7a030c974dc.png`. Floral crop (74,14)-(1701,444), plain crop (223,481)-(1551,865). Nine-slice X=25% floral /20% plain, Y=30%.
- `play-reference-v2.png`: 1961×802, `exec-799fe101-3d0d-43cf-87bb-427f4c2b2d7c.png`. Runtime crop (18,79)-(1942,715), nine-slice X=25%, Y=30%. Dedicated rounded floral Play; the rejected organic silhouette is not used. Live cream lettering has a brown outline.
- `reward-progress-v2.png`: 2030×775, `exec-1c12a125-3a1f-4171-bb48-a00add1243ac.png`. Runtime crop (65,175)-(1959,609), nine-slice X=28%, Y=30%. Banner/gift are illustrated; remaining-level text and seven progress leaves are live.

Shared crops and material borders are consumed by Unity, Skia and Android. Native GardenButton owns callbacks, press/spring, breath and disabled fade. The Save action uses the primary skin; Petals purchase price buttons retain their purchase material. Bottom navigation reuses `stone-wall-v1.png` in overlapping rows behind every tile and gap, with a front ledge. Shop top padding/viewport extends content behind fixed tabs; their foreground surfaces shield input.

## Shared wooden bottom console — approved A (2026-10-14)

Owner selected `product/previews/bottom-navigation-concepts-v1/a-wood-console.png` and authorized implementation. This replaces the five cream tiles and their stone backing. Two production skins were generated with built-in OpenAI ImageGen and copied unchanged; exact prompts, output paths and crop/border definitions are in `product/previews/bottom-navigation-wood-implemented-v1/prompts.json`.

- `wood-console-v1.png`, 1997×787 RGBA, original `exec-6fef43ad-a105-4b7b-9a8b-354adc11f7e6.png`: blank honey-wood console with continuous upper lip and small corner daisies. Runtime crop (12,155), size 1974×456; nine-slice X=10%, Y=20%.
- `wood-selected-v1.png`, 1159×1356 RGBA, original `exec-2ce238e8-0073-4767-ab61-968a6eed83e4.png`: blank green inset with a slender gold rim. Runtime crop (97,107), size 959×1100; nine-slice X=25%, Y=25%.

Manifest version 7 records unchanged file dimensions and SHA-256. Exterior/source alpha is preserved. A separate fully opaque backing covers the entire menu region, including gaps and the bottom safe-area inset; source alpha cannot reveal scrolling content. Only the decorative flowers overhang the straight lip. The reserved page/menu boundary is unchanged.

All five existing `Icons/nav-*.png` subjects are reused. The owner's follow-up enlarges the icons to fill their segments and the selected plaque to rise slightly above the console; only the active destination displays a live localized caption. It uses the separate green skin and white Lilita One lettering, matching the font family of Play and page titles. The raised active touch region also shields scroll gestures. Four subtle dividers are runtime layers. Lock badges, callbacks, tap/drag discrimination and press motion are preserved. Unity and the shared Skia/Android renderer consume the same geometry, crop coordinates and nine-slice borders. Icons and names are not baked into the panel, so every destination can be selected independently.

## Calm popup family — 2026-10-14

`popup-frame-v1.png` and `popup-title-v1.png` are original RGBA ImageGen outputs, copied unchanged. References: approved `product/previews/popup-redesign-concepts-v1/pause.png` and `product/previews/popup-redesign-concepts-v2/daily.png`. Both generations requested `transparent_background: true`. The frame has no decorations; the separate blank title supports quiet Daily / Jam / No Ads / Daily Challenge windows. Pause, Settings and purchase keep the existing small floral header.

Frame request: “Create a production-ready game UI asset, using the attached approved Bloomlings popup concepts as exact material/style reference. Output ONE blank window panel only, isolated on transparent background, centered, large landscape rounded rectangle roughly 1100 by 780 within the canvas. Honey-gold wood border, subtly beveled and polished, softly rounded corners. Border thin around 30 pixels, restrained wood grain. Inside warm light ivory cream nearly flat and evenly illuminated, no gradient hotspot. Outer soft small shadow. NO title, NO header, NO flowers, NO leaves, NO characters, NO text, NO icons, NO buttons. This is a reusable 9-slice UI background: straight horizontal and vertical edges, uniform border thickness, large completely blank interior, corners unobstructed. Match the approved panel family especially calm Daily reward, warm game materials and rounded toy-like bevels. Do NOT draw a full screen or backdrop; only the blank panel.” Original: `exec-d9538522-8a4d-43fe-8100-eef87af16331.png`.

Header request: “Production game UI asset for Bloomlings matching attached approved calm Daily popup. Generate ONE horizontal BLANK honey-gold wooden header sign, rounded rectangle, polished subtly beveled toy-like wood, understated subtle wood grain, broad completely blank center. Aspect approximately 5:1; isolated centered filling canvas width with small transparent gutter. No text no letters no numbers no flowers no leaves no decoration no screws no separate items no UI background. Warm golden highlights along upper lip, restrained dark brown edge and short soft shadow. This will be a 9-slice title plaque for quiet game popups. Transparent background.” Original: `exec-b3ad18f6-6146-4176-b7f6-27fa4211e9fc.png`.

`PopupDesign` defines runtime crops and slice borders shared by Unity, Android and Skia. No text is baked in. Rounded corners remain stable as the window/content changes height; headings and prices are fitted live. Dimensions and checksums are in manifest v8.

## Sound and Haptics symbols — 2026-10-14

`settings-controls-v1.png`: original RGBA ImageGen output `exec-bd87fbcb-83b9-4cf2-b9a4-1baace890d11.png`, copied unchanged. Reference: `product/previews/popups-implemented-v1/19-settings-19.5x9.png`, transparent background. Two runtime crops in `PopupDesign.SettingsCrop` show a speaker with sound waves and a phone with vibration marks. No floral metaphors. Dimensions and checksum in manifest v9.

Prompt: “Create one production-ready transparent game UI ICON ATLAS for Bloomlings matching the attached warm polished game screenshot style. Exactly TWO equally sized square cells arranged horizontally, wide canvas aspect 2:1. LEFT CELL: an unmistakable speaker/sound icon, cream and golden honey body, three simple dark brown sound waves on its right, beveled rounded chunky toy material, restrained shine. RIGHT CELL: an unmistakable haptics vibration icon: upright little rounded smartphone, pale cream face and honey-gold rim, simple screen with one tiny green dot and TWO dark brown zigzag vibration strokes outside its sides. Each symbol centered in its cell occupying about 78% of cell, with generous transparent gutters and no element crossing cell boundary. Consistent glossy 3D cartoon material, warm wooden-game palette, clear silhouette readable at small 40px sizes. NO flowers, NO leaves, NO music notes, NO labels, NO text, NO letters, NO numbers, NO tile backgrounds or enclosing panels. ONLY two standalone symbols on transparent background. Not a full screen.”

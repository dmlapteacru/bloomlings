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

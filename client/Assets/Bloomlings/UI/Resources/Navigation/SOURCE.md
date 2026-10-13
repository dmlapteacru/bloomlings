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

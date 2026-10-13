# Illustrated gameplay pods

Generated for Bloomlings using the built-in OpenAI ImageGen tool on 2026-10-13. The owner supplied `exec-ed1a588c-9e0c-44b4-bb94-2ac14b584acd.png` as a style reference. No third-party game's artwork was used as image input. Selected output: `exec-9a8e3e0a-dad0-4f20-9816-fce11cee2e4a.png`. The PNG is copied unmodified into `Resources/Gameplay/pod-icons-v1.png`; its dimensions and SHA-256 are in `manifest.json`. This is project-generated artwork for use in Bloomlings.

## Exact generation prompt

Use case: stylized-concept. Asset type: transparent production sprite atlas for Bloomlings mobile puzzle game's pod icons. Generate one atlas, 4 columns x 3 rows of EXACTLY 12 individual glossy 3D cartoon garden symbols, equally sized square cells, consistent scale (each object fills about 76% of its cell), centered per cell, plenty of transparent gutters, strictly no overlaps between cells. Match the chunky polished candy-like icons in the supplied gameplay reference (reference only; do not reproduce the UI, background, tiles, numbers, border or buttons). Straight-on view, rich saturated color, softly rounded sculpted volume, bright upper left highlight, colored contour and gentle tight shadow. Transparent background, no tile bases or badges behind objects. Exact cell order left to right: ROW 1: bright green single curved leaf; green moss mound with three rounded tufts; pink five-petal flower with yellow center; violet purple CLOSED flower bud. ROW 2: glossy vivid blue water droplet; pale icy cyan dewdrop with a tiny sparkle; golden orange cut wooden stump with circular growth rings; warm brown acorn with a tan cap. ROW 3: curled green vine tendril; red glossy pair of berries with two green leaves; pale lavender mist swirl (clearly a curl of fog, not a drop); dark reddish brown thick bark segment with grain. Keep all 12 visibly distinct in silhouette as well as color. No lettering, no additional items. Output a landscape 4x3 atlas with equal square cells. Reference image role: style only, especially the big blue droplet, leaf, flower, berry and stump in the source queue.

Generation arguments: `transparent_background: true`; `referenced_image_paths`: the owner-supplied concept above.

## Rendering contract

- The atlas has 4 by 3 cells, each exactly 362 pixels square. `GameplaySkin.PodCell` defines the variant order; Unity, Skia and Android slice the same cells.
- Unity preserves the original dimensions, alpha and bilinear filtering through the art importer. One atlas texture serves all sprites. The playtest decodes one shared atlas; stuck-slot grey copies keep the same silhouettes.
- These icons appear on source pods, waiting-slot sticker tiles and pod flights. Board-cell symbols retain their existing artwork and rules.
- Boosters reuse `Shop/booster-atlas-v2.png`; no new booster artwork was generated.
- Pod surfaces are procedural `GameplaySkin.Pod`: a smooth rim in the exact catalog variant color and a lighter raised face. The waiting look keeps its color and softens the icon/count. Counts use the existing licensed Lilita One font with white fill and a brown outline.
- Adjacent pods overlap by 18% of the smaller pod's height. Both renderers draw deeper pods first. Only exposed pods take taps; lower badges and counts remain clear of the overlap. `+N` occupies the lower left, the link mark sits beside it, and the count remains lower right. Existing animations and core rules are preserved.
- The board has a continuous rounded cream stone rim under smaller molded stone blocks with tight seams, 0.52 cell thick. The same geometry is used by Unity and the playtest.
- The shared solid tray panel now covers only the waiting slots. Boosters and source stacks stand over the original backdrop. Background image files and top-navigation layout are unchanged.

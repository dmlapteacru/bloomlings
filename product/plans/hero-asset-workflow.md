# Hero assets: one authoring source

Owner decision, 2026-10-17. This supersedes the earlier texture-work allocation in the animation plan.

The owner maintains one master Blender project containing the heroes, rigs, UVs, accessories and their fitted positions.
The owner creates/edits geometry and equipment, including alternate head states. The assistant supplies texture maps
and, in the separate animation feature, compatible actions for import into that same project. Review assemblies and
runtime bakes are generated outputs, never a second master that the owner must synchronize.

## Exchange

- Export the hero and accessories with the same rest pose, scale, origin, bone names/hierarchy and fitted transforms.
  Preserve UVs and material slots when requesting replacement textures. Include the current rigged hero with an animation request.
- Return textures as separate image files for the existing UVs/material slots; identify base color, roughness and normal
  maps explicitly. Do not bake lighting or reflections into the base color. Geometry/UV edits require a fresh export.
- Return animations as clearly named actions on the supplied skeleton, with frame rate and frame range documented.
  Import them into the master, inspect attachments and motion there, then export the approved result. Do not replace
  the owner's mesh, rig, weights or fitted accessories with an independently maintained version.
- The game stays 2D: approved models are rendered offline. Source FBX/GLB/Blender files never enter the APK. Keep
  accessory layers independent instead of baking every outfit combination. Future animated layers must share the
  hero's exact pose/camera and depth occlusion for each frame.

## Current Sprig equipment pilot

The supplied `SprigHero` exports are assembled by `tools/heroanim/sprig-outfit-study.py` for inspection. The runtime
bake `node tools/heroanim/bake-sprig-gear.mjs` reads that derived GLB and writes nine cropped PNG8 sprites, generated
crop data and `sprig-gear.json` with hashes. Original embedded textures/materials are retained in this first integration;
new assistant-painted textures have not been delivered by this pilot.

- Shop → Cosmetics → Sprig lists Daisy Cap, Garden Glasses and Leaf Backpack as free items. Each toggles independently.
- The cap uses the owner's `CapOn` head state exclusively. Removing it restores the natural leaf crown. Glasses and
  backpack remain independent; their occlusion against the body/cap is baked into the small overlay sprites.
- Equipment persists across restart and is shared by Home, the wardrobe and Sprig's win/milestone views. Profile and
  leaderboard avatars retain their separately chosen pictures, frames and badges.
- Free items are accessible when the normal Shop opens, without waiting for the paid Wardrobe's L40 unlock. They are
  Sprig-only and meta-only; gameplay figures/rules are unchanged. Older cosmetics and the other three heroes remain.
- The nine runtime images total 117,205 bytes. This equipment pilot uses one rest pose with a subtle shared breath.
  Skeletal actions, win/loss performances, Home interactions, tap-to-select actions and paid/event emotes are deferred
  to the separate animation feature in `hero-animation-plan.md`.

Verification: `SprigGearTests` covers all eight outfit combinations, family fitting, independent slots, save round-trip
and gameplay exclusion. Preview fixtures 108–111 exercise real shop taps, app restart, removal, Home, Wardrobe and a
solved level's win. `node tools/heroanim/check.mjs` checks every runtime image against the bake record.

## Twig equipment pilot

**Superseded on 2026-10-17:** the owner prefers the old in-game Twig. The original `Heroes.glb` animation set is active
again and the replacement-only Acorn Hat / Daisy Cap items are removed from the catalog. Saved IDs are preserved but
ignored while absent. The following describes the archived pilot; its sources remain available for study.

Owner delivery: `tools/heroanim/models/twig/source/TwigHero…fbx`. The base and two fitted, Head-weighted accessories
are retained without geometry edits. Acorn Hat and Daisy Cap are free Twig-only items in Shop → Cosmetics → Twig;
one Hat slot switches them, and Remove/Default restores the bare model. The shared modular hero renderer now supports
Sprig and Twig. Existing profile avatar pictures remain separate from the fitted in-game hero.

The export has no UV maps/textures. The initial vertex-colour pass is now replaced on visible front surfaces by a
clean projected front texture on a derived planar UV layer. Rear surfaces keep palette colors; no mismatched legacy
atlas is forced onto the mesh. The acorn fit is lifted/relaxed in the render copy; original source FBXs are retained. The animation-only FBX imports with different rest axes; preserve its
actions for later retargeting and use the base rest pose plus subtle breathing for the equipment pilot.

Final references are saved in `tools/heroanim/references/twig/`: the bare short-body base, default acorn head and four
olive/rust baseball-cap views. Use these for future authoring; keep the same face, nose, open smile and two low ears.
The files are references rather than another geometry master. Future final texture work needs a stable UV layout
from the owner's master or an agreed unwrap returned for import into it.

## Current Twig identity and next heads

Use the old in-game Twig, keeping its face, short body, golden-orange limbs and cream leaf bib. Current references:
`tools/heroanim/references/twig-legacy/` contains a bald full-body base and two matching head images (default acorn /
olive daisy baseball cap), plus an actual frontal render of the original source model for comparison. They are
image-generation authoring references, not new rigged meshes or final textures. The game stays on the original model
until the owner generates / fits the new base and heads in the same Blender master. Preserve head origin, neck join,
scale and facial attachment landmarks across head variants; do not change the face to fit a hat.

### Segmented Tripo trial (2026-10-17)

The owner's next three segmented exports make a shared face possible: take the base from the first archive and only
headwear from the other two. `tools/heroanim/models/twig-tripo/` records the extraction and fitting. The derived base
and both whole-head GLBs retain exactly the same head / ear / mouth geometry, UV and scale; separate headwear-only
GLBs are also delivered. Five files import into Blender with embedded color textures and a common head pivot. This
is a static authoring exchange without a skeleton, not a new master project or an in-game replacement. Current review:
`output/twig-tripo-study/`.

### Segmented Tripo trial enabled in the game (2026-10-17)

At the owner's request, the assembled shared-face Tripo Twig now replaces the old meta-screen Twig. The active
`tools/heroanim/bake-twig-tripo.mjs` renders the real fitted model into five RGBA8 PNGs totaling 426,263 bytes after the head-width correction below.
Home, Wardrobe, Shop preview and victory share the saved outfit through `HeroGearArt`. Shop → Cosmetics → Twig
has free Acorn Hat and Daisy Cap cards; one Hat slot selects either, and Remove / Default shows the bare base.
The existing Wardrobe unlock stays; free Shop gear can be tested earlier. Both item IDs from the old pilot are reused,
so existing selections resolve again. Sprig gear, other heroes and separately chosen profile avatars are retained.

This static source has no rig; the pilot uses 2D breathing and pose movement, pending the animation feature.
Source Tripo paint is provisional. Only small baked sprites are shipped, with full-color Android loading / warming;
FBXs, GLBs and source ZIPs stay authoring-only. No APK is built here. Actual page captures and verification are in
`output/twig-tripo-in-game/`.

### Twig head proportion correction (2026-10-17)

Owner feedback: the new Tripo Twig face is wider / flatter than the previous version. Its render-only shared head
pivot now uses X scale 0.88, with ears, mouth and both hats following the same transform. Body proportions, camera,
height and foot position stay unchanged; no runtime sprite stretching or new gameplay logic is introduced. The
original Tripo source and earlier Blender exchange remain intact. Record the same transform in the owner's master
if adopting it there later; current bake setting is `look.headWidthScale` in `bake-twig-tripo.mjs`.

### Current simplified four-hero trial (2026-10-17)

The owner generated simpler models from the approved clean-color references and requested them in the game.
All four are now the base meta-screen heroes; source record and reproducible importer / bake instructions:
`tools/heroanim/models/simple-tripo/README.md`. Original archives and reference pictures remain in the repository.
The real meshes / base colors are rendered offline; four full-color sprites total 411,390 bytes. Source proportions
are preserved through uniform scaling, including the new Twig; the earlier X=0.88 correction is archived.

The source has no rig. Use existing subtle 2D breathing for this visual trial, pending the separate animation feature.
Previous model-specific cap / glasses / backpack pieces are withheld from Shop / Wardrobe and the displayed outfit
until refitted; old service / save selections remain intact. Generic 2D cosmetics and independent profile avatar /
frame / badge choices stay supported. No source models ship in APKs and no APK is built here. Current captures:
`output/simple-heroes-in-game/`.

### Current Twig shared base and two fitted hats (2026-10-17)

The owner's next delivery replaces only Twig's complete base and supplies acorn / panama head generations. Keep the
complete bald base unchanged and use only extracted headwear from the other generations. Current source / fit / bake:
`tools/heroanim/models/twig-modular/README.md`. Preserve the latest light-color treatment and keep one face / ears /
body across outfits. Five static sprites / icons total 371,238 bytes; the current face / body comparison regions are
pixel-identical when headwear changes. The source models are unrigged; actions remain a later feature.

Free Acorn Hat (`hat.twig_acorn`, existing ID) and Panama Hat (`hat.twig_panama`) are available in Shop → Cosmetics →
Twig; one exclusive Hat slot, Remove / Default returns to bald. Existing selections / application restart are
supported, including an earlier saved Acorn choice. Home / Wardrobe / victory share the selection; fitted headwear
stays out of gameplay figures. Old unrefitted Daisy Cap / Sprig equipment IDs remain saved but withheld. The other
three simplified models and independent profile appearances remain. Current captures: `output/twig-modular-in-game/`.


### Twig V2 aligned wardrobe trial (2026-10-17)

Use the owner's `TwigHeroV2.zip` as the current Twig source, preserving the single Blender master on their machine.
All nine accessory parts are already fitted. Seven Head variants are exclusive (including Hair and the combined
HatHair); Glasses and Scarf are separate optional saved slots. Scarf is appended to cosmetic kinds without changing
existing enum values or save keys; the equipment dictionary uses `twig.scarf`. All parts are temporarily free.
Keep render-only PNG layers in the game and rig / actions / source FBXs offline. Existing Idle / Joy / Walk / Wave
sources are retained for the later action / animation feature. Details: `tools/heroanim/models/twig-v2/README.md`.

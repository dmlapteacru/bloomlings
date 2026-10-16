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

Owner delivery: `tools/heroanim/models/twig/source/TwigHero…fbx`. The base and two fitted, Head-weighted accessories
are retained without geometry edits. Acorn Hat and Daisy Cap are free Twig-only items in Shop → Cosmetics → Twig;
one Hat slot switches them, and Remove/Default restores the bare model. The shared modular hero renderer now supports
Sprig and Twig. Existing profile avatar pictures remain separate from the fitted in-game hero.

The export has no UV maps/textures, so the initial appearance uses temporary vertex-colour materials. No mismatched
legacy atlas is forced onto the new mesh. The animation-only FBX imports with different rest axes; preserve its
actions for later retargeting and use the base rest pose plus subtle breathing for the equipment pilot.

Final references are saved in `tools/heroanim/references/twig/`: the bare short-body base, default acorn head and four
olive/rust baseball-cap views. Use these for future authoring; keep the same face, nose, open smile and two low ears.
The files are references rather than another geometry master. Future final texture work needs a stable UV layout
from the owner's master or an agreed unwrap returned for import into it.

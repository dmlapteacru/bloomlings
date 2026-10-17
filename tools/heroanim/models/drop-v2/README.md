# Drop V2 — FBX handoff

This is an **offline authoring handoff**, not a runtime Drop replacement. The owner's existing `.blend` remains the maintained master. `content/work/drop-v2/*-cache.blend` files are disposable build intermediates and are deliberately excluded from Git / the ZIP.

## Inventory

The planned [reference set](../../references/drop-v2/SET.md) has **15 items**, all accounted for:

| Slot | Items | Count |
| --- | --- | ---: |
| Head | NativeDrop (complete original droplet head), HairSideWave, HairTwinRipples, HairWhirlpool, BucketHat, Cap, Beret, EarmuffHair, XmasHat | 9 |
| Face | Glasses, reused from Twig, fitted to both face variants, cream frame | 1 |
| Body | Jacket (winter), Coat (autumn), Overalls (spring/summer) | 3 |
| Accessories / neck | ScarfStripe (compact), ScarfKnit (chunky) | 2 |

A small skinned neck connector closes the head/body join; source base vertices are unchanged.

The authoring base contains a separate rounded bald `Base_Head`. **Hide it when NativeDrop is visible**. All other head items keep the rounded base face, eyes and mouth. NativeDrop replaces the complete head because its droplet top was not segmented. This is one Head item, not an additional hair shell.

Original owner deliveries remain unchanged under `source/`; `source-manifest.json` pins them and the prior base, fitted overalls and reused glasses. Derived meshes retain their original UV layouts and albedo resolution. No AI repaint / generated replacement geometry is involved. Non-color normal/metal/roughness overlays are removed from derived preview materials; original sources remain intact.

## Rebuild

Run from the repository root using Blender 4.3.2 (2 worker threads keeps shared cloud resources predictable):

```bash
blender -b --factory-startup -t 2 --python-exit-code 1 --python tools/heroanim/prepare-drop-v2.py
blender -b --factory-startup -t 2 --python-exit-code 1 --python tools/heroanim/rig-drop-v2.py
blender -b --factory-startup -t 2 --python-exit-code 1 --python tools/heroanim/export-drop-v2.py
blender -b --factory-startup -t 2 --python-exit-code 1 --python tools/heroanim/validate-drop-v2.py
node tools/heroanim/review-drop-v2.mjs
node tools/heroanim/review-drop-v2.mjs --animated --motion-only
node tools/heroanim/review-drop-v2.mjs --animated --video-only
python tools/heroanim/encode-drop-v2-video.py
python tools/heroanim/package-drop-v2.py
```

The scripts assert source hashes before processing, keep all base rest vertices / UVs unchanged and use one 23-bone skeleton. Head parts follow Head rigidly. Short limbs use conservative skinning; garment weights are continuous anatomical functions rather than discontinuous closest-body assignment. Scarves follow the chest and include three fit shape keys for the three garments.

Original actions: Idle, Walk (in place), Wave, Joy, Sad, 24 fps. IK controls are baked into the exported FBX actions; no third-party rig add-on is required. Clap is deliberately excluded: the generated sleeve topology stretches under the large shoulder turn needed to bring the short hands together. It needs a separate shoulder/sleeve pass before shipping.

These are simple body motions, without facial / individual-finger animation or cloth simulation.

`validate-drop-v2.py` evaluates 75 poses across all 23 meshes, checks normalized skin weights / rigid head lengths, and reimports every delivered FBX into an empty scene. It checks skeleton/rest coordinates, UV loops, scarf shape keys and animated vertex positions at 15 exported poses. Actual previews are rendered from the exported skinned GLB using Three.js, not generated illustrations.

## Owner import

Use the **single final ZIP** linked by the handoff manifest. Extract it fully. In your existing Blender master, open `Import_Drop.py` in the Text Editor and Run Script. It imports all 17 FBXs, merges identical armatures into **one shared rig**, preserves five actions, packs only imported textures and adds persistent custom properties on `DropHeroV2Rig`:

- `head_style` 0–8 in the Head order listed above.
- `body_style` 0 none, 1 Jacket, 2 Coat, 3 Overalls.
- `scarf_style` 0 none, 1 ScarfKnit, 2 ScarfStripe.
- `glasses` 0 / 1.

Visibility drivers keep only one item per slot. Scarf fitting keys follow `body_style` automatically. Choose `Drop_*` actions in the Dope Sheet / Action Editor. Each item FBX also imports independently, but importing manually creates duplicate skeletons; repoint its armature modifier to the common rig without changing rest pose or applying individual-part scaling.

Do not put FBXs, source ZIPs, Blender caches or the handoff ZIP under mobile Resources. Runtime integration / sprite baking is a separate next step.

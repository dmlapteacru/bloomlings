# Accepted hero rig repair

Offline authoring tools for the four accepted V2 heroes. The owner's Blender project
is the single editable master. Original deliveries are read-only inputs; all `.blend`
files here are disposable caches under ignored `content/work/rigging/`.

The repair retains the accepted hero proportions, main albedo textures, headwear,
glasses and scarf fit morphs. Clothing panels and hems lose accidental arm / leg
influences; cuffs follow the forearm rather than wrist twists. Local arm / torso
contacts in generated bodies and clothing are separated where the fused surface prevents motion.
These changes retain the visible fabric and record their source-surface / UV mapping;
the repair does not replace complete sleeves with generic geometry. Unmodified parts
retain their rest vertices, topology and UVs.
Any local smoothing of contact vertices is recorded separately with its affected region
and maximum displacement; the unchanged-region rest error does not describe those edits.
Surface shading follows the accepted game finish: nonmetallic, roughness .68, without
the generated normal / roughness maps. Original color atlases are retained. Sprig's
Jacket and Bloom's Coat also use a local copy with cleaned baked occlusion on the inner
contact seam. The outer colored cuffs and main panels keep their appearance. Face / UV
masks record this exception; independent checks verify that pixels outside those masks
stay unchanged.

The same existing main bones remain. Eight half-angle support bones improve linear
FBX skinning at shoulders, elbows, knees and ankles. Planted foot IK has explicit
knee poles, calibrated to each imported rest pose. Drop's thick arms use their axial
coordinate rather than lateral width for weights. Headwear and glasses stay rigid on
Head; scarves use Chest and keep their independent garment-fit morphs.

Six baseline actions at 24 fps: **Idle, Walk, Wave, Joy, Sad, Curiosity**. Idle and Walk
loop; the four other actions return to rest. Each family has restrained individual
timing/amplitude. Three diagnostic actions exercise arm lift, forward reach and body
twist; these are not exported as gameplay actions. This is body animation, without a
new facial / individual-finger rig or cloth simulation.

## Rebuild

Run from the repository root with Blender 4.3.2, Node 22+ and the pinned dependencies
in `tools/heroanim/node_modules`. No APK or level catalog generation is involved.

```sh
python3 -m unittest discover -s tools/heroanim/rigging -p 'test_*.py'
blender -b --factory-startup -t 2 --python-exit-code 1 --python tools/heroanim/rigging/assemble.py -- twig
blender -b --factory-startup -t 2 --python-exit-code 1 --python tools/heroanim/rigging/build.py -- twig
blender -b --factory-startup -t 2 --python-exit-code 1 --python tools/heroanim/rigging/body_masks.py -- twig
blender -b --factory-startup -t 2 --python-exit-code 1 --python tools/heroanim/rigging/assess.py -- twig
blender -b --factory-startup -t 2 --python-exit-code 1 --python tools/heroanim/rigging/export.py -- twig
blender -b --factory-startup -t 2 --python-exit-code 1 --python tools/heroanim/rigging/roundtrip.py -- twig
node tools/heroanim/rigging/review.mjs twig --quick
```

Repeat for `sprig`, `bloom`, `drop`. `review.mjs --video` renders actual skinned motion
frames; it does not synthesize an illustration. All runtime renderers remain 2D.
The existing production sprites continue using their pinned source records; this
authoring repair does not silently replace or expand the mobile frame library.

`assess.py` compares the same 117 poses on source and corrected skinning, and verifies
rest-surface coordinates, interpolated UVs, weight normalization and maximum four
influences. Local contact corrections are recorded separately from unmodified parts.
Source vertices confidently assigned to forearms / hands, plus reviewed full cuff
selections for Sprig's Jacket and Bloom's Coat, are checked for accidental torso weights.
The full selections are guarded by source mesh / UV / albedo hashes, including cuff
vertices whose original weights incorrectly assigned them to the chest. The release
summary records counts and maximum torso weights.
`roundtrip.py` imports every exported FBX into an empty scene and checks identical
bones / rest hierarchy, UVs, embedded albedos, fit morphs and animated vertex positions
at 18 nontrivial sample poses. Same-position seam vertices are matched by both their
coordinate and bone weights. A successful export by itself is not validation.
Animation-only FBXs explicitly carry the same BindPose as their meshes; the native
exporter otherwise omits that block and can turn a slightly solved IK pose into a
different rest skeleton. No duplicate base mesh is added to the animation file.

## Owner handoff

The complete extracted package contains four folders of FBXs and
`Import_Rigged_Heroes.py`. Open the script in Blender's Text Editor and Run Script.
It imports into a new **Bloomlings_RigFix_v2** collection, merges matching armatures,
packs only the imported images and installs an optional **N → Bloomlings** review panel.
Choose a hero, Head / Body / Neck / Glasses and an action. Hide the old hero collection
when comparing; the script never deletes or edits the owner's existing meshes. A prior
RigFix V1 collection stays separate from V2. Reuse requires the exact same inventory
hash, so an older revision cannot silently appear as the updated rig.
Use Material Preview or Rendered shading for outfits: skin masks hide the enclosed
body regions covered by each garment. Open armholes, cuffs and hands retain the
visible skin they need; mask regions depend on the hero's actual garment fit.
The masks change material visibility, never body vertices.

Re-running the script reuses these imported rigs. Actions are retimed to the existing
scene FPS. Legacy and slotted Actions (Blender 4.4+ / 5.x) are supported by the importer;
the automated export / reimport validation runs in the available Blender 4.3.2.
The normal state plays portable baked FK actions; **Editable leg IK** enables the
restored authoring constraints. Half-angle helper drivers are restored in both cases.
The baseline authoring mesh may be bald, but stock Head is always selected in review.

For the actual before / after review, run `review.mjs FAMILY --quick --outfits` for
all four families and `review.mjs FAMILY --detail --after-only` for their underarm side views.
Run `review.mjs twig --video --all-actions` for the motion demo, then
`python tools/heroanim/rigging/compose_review.py` (Pillow and FFmpeg). Source hashes prevent
composing previews from stale model renders. The video shows all six body actions;
the gallery also exercises bare arms, all garments and bulky neck / head combinations.
`outfit-checks.jpg` shows all four families bare and in each garment under the same pose.
`side-checks.jpg` shows the three garments from the side during Wave, so the contact
surfaces can be reviewed beyond the frontal silhouette.

Accept the corrected parts into the same master, rather than maintaining a second
`.blend`. These repair recipes describe the original V2 deliveries; already repaired
FBXs should retain their weights when authoring the next actions, not be repaired twice.

The reviewed products are in [products/rig-fix-v2](../products/rig-fix-v2/README.md).
After pulling the repository, assemble one ZIP without Blender or dependencies:

```sh
node tools/heroanim/rigging/check.mjs
python tools/heroanim/rigging/package.py
```

Output: `content/work/rigging/Bloomlings_RigFix_v2.zip`. To publish new products after
rebuilding all four families, run `verify_owner_import.py` with Blender, then
`python tools/heroanim/rigging/package.py --publish`. Publishing rejects stale source,
rig, FBX and owner-import checks.

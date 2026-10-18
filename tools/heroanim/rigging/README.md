# Accepted hero rig repair

Offline authoring tools for the four accepted V2 heroes. The owner's Blender project
is the single editable master. Original deliveries are read-only inputs; all `.blend`
files here are disposable caches under ignored `content/work/rigging/`.

The repair preserves hero rest vertices / UVs, source headwear, glasses and scarf fit
morphs. Generated garments with fused armpits need more than repainting weights:
their broken internal sleeve / side connections are replaced by overlapping inner
fabric and clean sleeve tubes. Surviving torso UV loops and material slots are kept.
Sleeve fabric colors come from the delivered atlas, with rounded cream cuffs.

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
retained rest coordinates, UVs, weight normalization and maximum four influences.
`roundtrip.py` imports every exported FBX into an empty scene and checks identical
bones / rest hierarchy, UVs, embedded albedos, fit morphs and animated vertex positions
at 18 nontrivial sample poses. Same-position seam vertices are matched by both their
coordinate and bone weights. A successful export by itself is not validation.

## Owner handoff

The complete extracted package contains four folders of FBXs and
`Import_Rigged_Heroes.py`. Open the script in Blender's Text Editor and Run Script.
It imports into a new **Bloomlings_RigFix_v1** collection, merges matching armatures,
packs only the imported images and installs an optional **N → Bloomlings** review panel.
Choose a hero, Head / Body / Neck / Glasses and an action. Hide the old hero collection
when comparing; the script never deletes or edits the owner's existing meshes.

Re-running the script reuses these imported rigs. Actions are retimed to the existing
scene FPS. Legacy and slotted Actions (Blender 4.4+ / 5.x) are supported by the importer;
the automated export / reimport validation runs in the available Blender 4.3.2.
The normal state plays portable baked FK actions; **Editable leg IK** enables the
restored authoring constraints. Half-angle helper drivers are restored in both cases.
The baseline authoring mesh may be bald, but stock Head is always selected in review.

Accept the corrected parts into the same master, rather than maintaining a second
`.blend`. These repair recipes describe the original V2 deliveries; already repaired
FBXs should retain their weights when authoring the next actions, not be repaired twice.

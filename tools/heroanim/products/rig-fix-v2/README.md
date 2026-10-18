# Rig repair V2 — authoring handoff

**Paused, unpublished checkpoint.** The archives and manifest described below have
not been published. See [current status](../../rigging/STATUS.md) before resuming.

Four accepted V2 heroes, 55 equipment items, 63 FBXs and six body actions per hero:
Idle, Walk, Wave, Joy, Sad and Curiosity. The four archives form one package; they
are split only to keep each tracked file below the Git file size limit.

From the repository root, after pulling this branch:

```powershell
node tools/heroanim/rigging/check.mjs
python tools/heroanim/rigging/package.py
```

Extract `content/work/rigging/Bloomlings_RigFix_v2.zip`. In your existing Blender
master, open the included `Import_Rigged_Heroes.py` in Text Editor and Run Script.
Use **N → Bloomlings** to choose hero, equipment and action. Material Preview or
Rendered shading shows the skin masks beneath the enclosed garment panels. Stock headwear is selected
by default. The importer adds a collection, preserves existing objects and FPS,
and reuses the same revision when run again. A previous V1 collection remains separate;
hide it while reviewing V2. Changed package contents cannot silently reuse an older rig.

`manifest.json` pins source archives, tools, products and numerical validation.
Each hero folder includes its `rig-report.json`, FBX roundtrip report and validation
summary, so local mesh repairs and their verification travel with the FBXs. Sprig's
Jacket and Bloom's Coat include a masked cleanup of baked occlusion inside their
contact seams; pixels outside those masks retain their source values.
See [repair instructions](../../rigging/README.md) for rebuild and test details.
`rig-comparison.jpg` compares all four heroes; `baseline-actions.mp4` shows the repaired
body actions on Twig. `outfit-checks.jpg` exercises bare bodies and all three garments
under the same arm-lift pose. `side-checks.jpg` shows all three garments from the side
during Wave, exposing the repaired underarm contacts. These previews are also in the assembled ZIP's `Review`
folder. All six actions per hero are included in the FBXs and importer.
These FBXs are offline authoring assets. The existing game still uses its current
2D sprites; new motion needs a separate sprite bake and playback integration.

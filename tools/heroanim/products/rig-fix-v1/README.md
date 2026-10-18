# Rig repair V1 — authoring handoff

Four accepted V2 heroes, 55 equipment items, 63 FBXs and six body actions per hero:
Idle, Walk, Wave, Joy, Sad and Curiosity. The four archives form one package; they
are split only to keep each tracked file below the Git file size limit.

From the repository root, after pulling this branch:

```powershell
node tools/heroanim/rigging/check.mjs
python tools/heroanim/rigging/package.py
```

Extract `content/work/rigging/Bloomlings_RigFix_v1.zip`. In your existing Blender
master, open the included `Import_Rigged_Heroes.py` in Text Editor and Run Script.
Use **N → Bloomlings** to choose hero, equipment and action. Material Preview or
Rendered shading shows the skin masks beneath sleeves. Stock headwear is selected
by default. The importer adds a collection, preserves existing objects and FPS,
and reuses its imported rigs when run again.

`manifest.json` pins source archives, tools, products and numerical validation.
See [repair instructions](../../rigging/README.md) for rebuild and test details.
`rig-comparison.jpg` compares all four heroes; `baseline-actions.mp4` shows the repaired
arm movement on Twig. All six actions per hero are included in the FBXs and importer.
These FBXs are offline authoring assets. The existing game still uses its current
2D sprites; new motion needs a separate sprite bake and playback integration.

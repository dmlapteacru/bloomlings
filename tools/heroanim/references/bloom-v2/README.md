# Bloom modular modeling baseline

Offline Tripo / Meshy modeling references prepared on 2026-10-17 from the approved simplified Bloom.
The current in-game Bloom is unchanged; these pictures are not runtime assets or fitted meshes.

- `base-full-front.png`: bald modeling scaffold, pale pink common skull / face, short coral body and heart belly mark.
- `with-default-petals-front.png`: the approved simplified Bloom, retained unchanged from `../simple-heroes/bloom.png`.
- `heads-three-hairstyles.png`: stock five-petal halo, side-swept petal bob, curled petal crop (left to right).
- `set/`: five additional Head choices, one Neck scarf and three Clothes references, one PNG per item.
- [SET.md](SET.md): complete item list and modeling instructions.
- `manifest.json`: SHA-256 pins for all original reference PNGs.
- `generation-prompts.json`: generation instructions and reference dependencies.

There are 13 planned definitions: 8 Head (including the default flower halo), 1 reused round Glasses,
1 Neck and 3 Clothes. The bald base is only a modeling scaffold; the final stock Bloom wears its flower halo.
The owner-maintained Blender base is the authoritative editable master. Fit generated wearable geometry to that
base; generated pictures cannot guarantee identical geometry or accessory alignment. No second maintained `.blend`.

Reference files stay outside application Resources. No new model, cosmetic catalog entry, texture bake, APK,
animation or level generation is part of this reference set. Reuse the existing Twig round glasses mesh and adjust
its material for Bloom when fitted models are integrated.

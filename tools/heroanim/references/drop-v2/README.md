# Drop modular modeling baseline

Offline Tripo / Meshy references prepared on 2026-10-17 from the approved simplified Drop.
The current in-game Drop is unchanged; these pictures are not runtime assets or fitted meshes.

- `base-full-front.png`: rounded bald modeling scaffold with the common face and short slender separate body.
- `with-default-crest-front.png`: unchanged approved simplified Drop from `../simple-heroes/drop.png`.
- `set/`: four water hairstyles, five headwear choices, two scarves and three seasonal clothes, one PNG per item.
- [SET.md](SET.md): complete 15-item list, shapes and modeling instructions in Russian.
- `manifest.json`: SHA-256 pins for all 16 original reference PNGs.
- `generation-prompts.json`: generation prompts and reference dependencies.

There are 15 planned definitions: 9 Head (4 hairstyles including the default teardrop crest, 5 hats),
1 reused round Glasses, 2 Neck and 3 Clothes. Each hat includes its own compact water forelock and side locks;
hat and those locks are one Head item. The bald skull is only a modeling scaffold. The stock game character
keeps its teardrop crest. Water forms are smooth opaque sculpted blue shapes, not transparent liquid.

The owner-maintained Blender base is the authoritative editable master. Extract generated wearable geometry
and fit it to that common base; AI pictures cannot guarantee identical skull geometry or glasses alignment.
No second maintained `.blend`. Reuse the existing Twig round glasses mesh; fit and recolor it for Drop later.
Scarves remain separate from Body; chunky scarf garment fits can be authored in the same Blender master.

References stay outside application Resources. No model replacement, catalog item, texture bake, animation,
APK or level generation is part of this reference set.

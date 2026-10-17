> Historical references. Current approved identity / modeling baseline: [../twig-v2/README.md](../twig-v2/README.md).

# Twig: original in-game identity, bald base and two head references

Owner direction, 2026-10-17: the old model looks better; restore it in the game and use it as the basis for a bald
hero and then two head variants. This supersedes the replacement Twig FBX pilot's visual direction.

- `TwigHero_Original_Render_Front.png`: actual frontal render of the unchanged Twig mesh from `models/heroes.glb`,
  using the original bake's color grade, lights and rest animation pose. This is the restored in-game model.
- `TwigHero_Base_Front.png`: image-generation edit of that render, removing the complete hat / stem / leaf / upper
  curled hat ornament, completing a smooth bald dome and retaining two small low ears. Keep the old face, very short
  body, golden-orange limbs, cream leaf bib, open smile and subtle nose. This is a modeling reference, not a new mesh.
- `TwigHero_AcornHead_Front.png`: the same base head and two low ears, with a segmented acorn dome / rolled rim,
  woody stem and one green leaf. Generated from the bald base plus the actual original model render.
- `TwigHero_CapHead_Front.png`: the same base face / ears with an olive crown, rust-brown projecting baseball bill,
  small top button and five-petal daisy on the wearer's left. Generated from the bald base plus the earlier cap
  design reference; only that cap's design was used, not its previous replacement face.

The last three pictures are proposed authoring references for Tripo / the owner's one Blender master. They are not
UV maps, exports, fitted geometry, automatic head replacements or a second master project. The restored game still
uses the existing original hat-bearing model and its original animation frames. New bald / head meshes must be
generated and fitted before integration. Future head variants must keep the same head origin, neck join, scale,
Head-bone attachment and face landmarks for glasses. These source images never enter the APK.

`manifest.json` records file hashes and the original mesh source. The older `references/twig/` folder is retained as
history of the retired FBX experiment; use this folder for the current identity direction.

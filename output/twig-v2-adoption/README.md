# TwigHeroV2 permanent adoption

Owner approval: 2026-10-17, after testing the delivered V2 model and accessories in game.

The active Twig source is `tools/heroanim/models/twig-v2/source/TwigHeroV2.zip`. The owner's Blender project stays
the only editable master. Twelve original modeling references (including the bald base) and four new identity-art
masters are preserved with hashes in `tools/heroanim/references/twig-v2/`; its contact sheet indexes the modeling set.

All Twig meta-screen states / compatibility motion lookups and `SimpleHeroArt.Frame(Twig)` now use V2. Three avatars
retain their old IDs / themes / prices but depict V2. The hero medal, solo / cheer / blank and four-hero group fallbacks
also use V2. Acorn and Panama item IDs stay valid; the retired flower cap resolves to Panama. No progress, purchases,
profile frame, badge or chosen avatar is reset. All nine delivered V2 accessories remain free as previously requested.

263 former Twig motion / fitted / simplified PNGs (11,631,125 bytes) and four helper / generated C# files are retired
under `tools/heroanim/archive/twig/`, outside Unity and APK resources. Old Twig publishers reject production writes.
The legacy motion publisher retains other publishers' manifest entries when rebaking another family. The old source
archives remain historical records. The L2000 Twig mosaic is deliberately unchanged, per owner instruction.

The Home fitting scale preserves the approved V2 size, with a small inward shift for 21:9. No skeletal-animation /
action-selection feature, second Blender master, or APK build is included.

Validation:

- 91 focused client tests pass: outfit combinations, compatibility aliases, save / reload / merge, profile assets,
  frame cache, animation timing and Home layout across screen shapes.
- `node tools/heroanim/check.mjs` passes: current references, sources / build geometry proof, every runtime output,
  archived PNG hashes, and absence of retired Twig resources.
- `tools/artgen check` passes: 57 character pictures, including 10 adopted owner images. Its project now links the
  art contract explicitly, so adding application screens no longer breaks this offline verification tool.
- The actual APK asset packer validates 41 changed / V2 images: 2,659,935 bytes before packing,
  2,371,630 bytes after. All 33 V2 layered sprites remain byte-identical, including alpha.
- Packed fixtures 73, 100, 113, 114, 124–128 pass at 16:9, 19.5:9 and 21:9 (27 captures): saved / reloaded
  equipment, victory, Shop item selection and scrolling, seasonal Home outfits, Wardrobe, hero medals and customized
  leaderboard avatars. Loading checks pass 11 cold destinations, 123 staged frames and 157 resources; 100 live
  preview frames continue while 9,147 transient-effect draws are delayed.
- Desktop renders / packing checks do not measure phone frame times. Source ZIPs, models and reference images stay
  outside app resources.

The review JPEGs are captured from the actual game renderer using packed image assets. Full-resolution test captures
and logs remain in ignored `content/work/twig-v2-adoption/`.

# Twig V2 clothes and warm accessories — 2026-10-17

The owner supplied four additional FBXs separately after the expanded ZIP exceeded the transfer limit. Original
bytes are retained under `tools/heroanim/models/twig-v2/source/additions-2026-10-17/`; the source manifest records
hashes and override / addition roles. The old source ZIP remains intact; the owner's Blender master is unchanged.

The new rounded glasses replaces the previous frame / lens mesh under the same item ID. It is a smaller single
frame with clear openings; no tinted lens plane or texture hides the eyes. `twig-v2-look.json` gives it a soft brown
material and makes Panama fabric warm brown while retaining cream trim, daisy, original UV detail and geometry.
The other seven base / Head images are byte-identical to the approved version. Shader grading is offline only.

Three free fitted Clothes choices are added: green / cream Winter Jacket, orange-brown Autumn Coat and cream / green
Garden Overalls. Clothes is independent from Head, Glasses and Neck. Its enum / save kind is appended, preserving
old serialized values and selected items. Removal, Default and reload work on every slot. Shared four-layer hosts
show the selection on Home, Shop, Wardrobe and victory. Meta-only wearables remain out of puzzle workers. L2000's
mosaic remains unchanged; no levels or APK are generated.

Full current inventory: `tools/heroanim/models/twig-v2/ITEMS.md` — **12 items: 7 Head, 1 Glasses, 3 Clothes, 1 Neck**.
Including empty slots, the 128 outfits use 35 RGBA sprites totaling 1,674,582 bytes. Source-bound separation checks
permit shared garment masks; four scarf masks account for each garment / bare body. Only four visible layers load.

Validation:

- 69 focused client tests passed (wardrobe, cache, motion, save / localization), followed by the five Twig tests
  with exact garment / scarf-mask assertions. The combination test covers all 128 outfits and serialized reload,
  individual removal, gameplay exclusion and Default. All twelve free items fit only Twig and cost no Petals.
- Actual game fixtures 124, 125, 129–133 passed at 16:9, 19.5:9 and 21:9: 21 distinct screen captures. They exercise
  Shop equip / remove / Default / fixed-header scrolling, victory, seasonal Home combinations and three unobscured
  garment fittings. The fitting-only captures rerun 131 without scarf for visual review.
- The actual APK asset packer validates 43 V2 / identity images. All 35 V2 sprites remain byte-identical, including
  alpha. Cold navigation passes 11 destinations / 123 staged frames / 157 resources; 100 live-preview frames
  continue while 9,147 effect draws are delayed.
- `node tools/heroanim/check.mjs` and `git diff --check` pass, including source hashes, original geometry / UV proof,
  layer separation, references and output manifests. Desktop checks do not measure phone frame times.

The review JPEGs come from the game's renderer using packed assets. `items-sheet.jpg` displays the exact published
item-icon renders. Full-size captures / logs remain in ignored `content/work/twig-v2-clothing/`.

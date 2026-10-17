# TwigHeroV2 — installed items

Twelve fitted items. Hairstyles, hats, hair-with-hat and earmuffs-with-hair share one exclusive Head slot. The bald
base is a default state, not an item. Clothes, glasses and neckwear are independent. All twelve remain free for testing.

| Slot | Count | Delivered items |
| --- | ---: | --- |
| Head | 7 | Acorn Hat; Panama Hat; Woodland Hair; Acorn Hat & Hair; Autumn Beret; Winter Earmuffs; Holiday Hat |
| Glasses | 1 | Twig Glasses — updated round warm-brown frame, open lens holes |
| Clothes | 3 | Winter Jacket (green, cream trim); Autumn Coat (orange-brown, leaf pockets); Garden Overalls (cream, green shirt) |
| Neck | 1 | Golden Scarf |
| Backpack / other | 0 | None fitted to the current Twig base |
| **Total** | **12** | 128 combinations including empty slots |

| Delivered FBX suffix | Stable game ID |
| --- | --- |
| PumpkinHat | `hat.twig_acorn` |
| BucketHat | `hat.twig_panama` |
| Hair | `hat.twig_hair` |
| HatHair | `hat.twig_hat_hair` |
| Beret | `hat.twig_beret` |
| EarmuffHair | `hat.twig_earmuffs` |
| XmasHat | `hat.twig_xmas` |
| Glasses | `glasses.twig_round` |
| Jacket | `clothes.twig_jacket` |
| Coat | `clothes.twig_coat` |
| Overalls | `clothes.twig_overalls` |
| Scarf | `scarf.twig_knit` |

`source/additions-2026-10-17/` preserves the four additional owner FBXs unchanged. The new Glasses replaces the
original mesh, retaining the existing selected item ID; the three clothing meshes add exclusive choices in `twig.clothes`.
`source-manifest.json` pins their hashes and replacement / addition roles. The original ZIP is retained unchanged.

Accessory materials are authored in `../../twig-v2-look.json`: warm brown / cream glasses and brown Panama fabric.
The new glasses are a single frame mesh, without lens planes or a lens texture. The Panama keeps its cream band,
daisy, texture detail and shading. The material recipe is applied during the offline bake, leaving source files,
geometry, UVs, alignment and the base face / body textures untouched.

35 cropped RGBA sprites (1,674,582 bytes) cover all 128 outfits without duplicating full outfits: eight bases,
three garments, four garment-aware scarf masks, eight head-aware glasses and twelve Shop icons. The build report
proves vertical separation allowing garment masks to be reused across headwear. Only four selected sprite layers
load at a time. Default clears all worn accessories, while removing one slot preserves the rest. Meta-only clothing
stays out of core puzzle workers. The L2000 mosaic remains unchanged.

# Sprig — complete modular reference set

Prepared for the next Tripo / Blender modeling pass. These modeling references now accompany the delivered fitted SprigHeroV2.
The current in-game preview and source files are documented in `../../models/sprig-v2/README.md`.

**13 planned fitted definitions:** 8 Head (including the stock leaf style), 1 reused Glasses, 1 Neck, 3 Clothes.
The bald base is a modeling scaffold, never the intended in-game default. Hairstyles and headwear share one slot.

| Slot | Reference | Design |
| --- | --- | --- |
| Head — stock | `heads-three-hairstyles.png`, 1 | Soft rounded leaf cap with a small forelock |
| Head | `heads-three-hairstyles.png`, 2 | Compact side-part leaf crop |
| Head | `heads-three-hairstyles.png`, 3 | Curled leaf forelock |
| Head | `set/head-panama.png` | Warm sage bucket hat, cream ribbon, simple daisy |
| Head | `set/head-garden-cap.png` | Soft olive garden cap, short rounded visor, leaf emblem |
| Head | `set/head-beret.png` | Low tilted moss-green beret with a light leaf pin |
| Head | `set/head-winter-earmuffs.png` | Cream ear pads, olive band, restrained rounded leaf hair |
| Head | `set/head-holiday-hat.png` | Red holiday hat, cream cuff / pompom, small green leaf |
| Glasses | Existing Twig round frame | Reuse the current mesh; adjust material for Sprig later |
| Neck | `set/neck-scarf.png` | Neat cream scarf with sage accents and short chest-length ends |
| Clothes — winter | `set/clothes-winter-jacket.png` | Cream padded jacket, sage accents, large simple toggles |
| Clothes — autumn | `set/clothes-autumn-coat.png` | Warm ochre garden coat with leaf-shaped pockets |
| Clothes — spring / summer | `set/clothes-garden-overalls.png` | Sage overalls over a cream short-sleeved shirt |

Heads are illustrated on the approved common bald head. Clothes and scarf are illustrated on the short-bodied bald
base to make segmentation easier. The forehead / skull / body shape should continue to come from one owner-maintained
Blender base, while the generated wearable parts are aligned onto it; the rendered illustrations guide shape / color.
The earmuffs include their quiet hair in one Head variant. Neckwear and Clothes remain independent of Head.

Use broad rounded leaf / fabric volumes, large readable features and clean surfaces rather than fine veins, fabric
weave or fur strands. Color is a reference at this stage; actual texture / material work follows the supplied meshes.
No second Blender master, new runtime models, level generation or APK build is included.

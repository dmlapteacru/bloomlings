# tools/heroanim

Pre-renders the owner's animated heroes (FBX or glTF) into flat frame pictures for Home, the splash, the win and the milestone,
and prepares the owner's layered Home picture (spec 005 FR-028). The game never loads a 3D model (constitution VII):
both builds play the frames (`HeroMotion`, `HomeLayers` in the design kit). Source record and processing: `SOURCE.md`.

Requires Node 22 and Chromium for Playwright (`PLAYWRIGHT_BROWSERS_PATH`; the cloud sessions have it in
`/opt/pw-browsers`).

```sh
cd tools/heroanim
npm ci                               # three 0.160.0, playwright-core 1.56.1, pngjs, jpeg-js
node bake.mjs                        # legacy motion library for Sprig / Bloom / Drop only
node layers.mjs <folder>             # the owner's Home layers (01_home_bg_back.png … 04_home_soft_shadow.png) into backgrounds/
node backdrops.mjs                   # the shown backgrounds: backgrounds/ blurred and darkened into the Backgrounds folder
node saturation.mjs                  # measures the backgrounds' saturation (no muting since 2026-10-08)
node check.mjs                       # must pass before committing hero frames or Home layers (no npm packages needed)
```

| File | What |
|---|---|
| `heroes.json` | the bake: cell, foot line, fill and margin, frames per second, the clips' lengths on screen, supersampling, camera, light, blend times, and per hero its model, its clips (the idle, the reaction and maybe the win's `win`: one clip, or a list baked as `win`, `win2`), its head bones (`head`, `top`; Mixamo's by default; `topOffset` moves the top point that far along the top bone's axes, for a rig without a bone at the head's top), its ground point (`ground: "model"` for a model placed away from the origin: the middle of its posed bounds' base), its own clip lengths (null: the clips' own), its own `light` (any of `hemi`, `key`, `fill`, `rim`, and `exposure`, a factor on every light, and `lift`, a share of the albedo added as glow to open the shadows), its own `color` grade of the texture (`gamma` under 1 lightens the midtones, `gain`, `saturation`, `warm`) and its turn (`yaw`, toward the fountain's middle on Home; a hero with `winYaw` is baked a second time at that turn for the win, where it stands alone facing the player: its celebrations and its idle there, `winidle`) |
| `models/heroes.glb` | the owner's historical four heroes in one Blender file (`SOURCE.md`); `heroes.json` `mesh` picks each one, `breathe` adds a breath an idle lacks and `synth` makes stand-in clips for a hero delivered without any (historical Twig); the bake still reads Meshy FBX and single `.glb` files |
| `bake.mjs`, `page.html`, `serve.mjs`, `png8.mjs`, `post.mjs` | the renderer (three.js in headless Chromium), the fit and crop, the palette PNG writer, the Home set's finish (`heroes.json` `home`: sharpening, contrast, saturation; the owner's Home tuning of 2026-10-05) |
| `layers.mjs` | the Home layers: crops, the lotus cut-out, the shadow, the JPEG garden (blurred by `gardenBlur`: 0 since 2026-10-08, the owner's garden comes blurred), the scene's saturation (`saturation.mjs`'s share); written into `backgrounds/` |
| `backgrounds/`, `backdrops.mjs` | every owner background and Home layer as delivered, and the finish the game shows (spec 005 FR-049, the owner, 2026-10-08): the opaque pictures blurred by a Gaussian of 3 / 1080 of their width and every picture 14% darker (`finish`), the Home fountain's layers darkened only; a new or changed background goes into `backgrounds/` with its name, then `node backdrops.mjs` |
| `saturation.mjs` | the backgrounds' saturation (spec 005 FR-031): since 2026-10-08 the owner keeps their colors as delivered (`ladder.background` null), so it measures each owner background against the animated heroes' mean and changes nothing; from 2026-10-04 it brought each to 70% of that mean, lightness and hue kept; `saturation.json` holds the last run's measurements |
| `check.mjs` | hashes of every output against `manifest.json`, `layers.json` and `backdrops.json`, the generated kit files, the models and `heroes.json` |
| `manifest.json`, `layers.json`, `backdrops.json` | what the last bake, the last layer run and the last finish wrote |

Outputs:
- `client/Assets/Bloomlings/Art/Heroes/Resources/HeroMotion/<family>-<idle|react|win|win2>-<NN>.png` and the folder's
  `manifest.json` (the originality test's list, `client/THIRD_PARTY_NOTICES.md`);
- `client/Assets/Bloomlings/UI/Design/HeroMotionData.cs` (per frame: its crop in the 448 × 504 cell and the head points
  the hats follow);
- `tools/heroanim/backgrounds/home*.{jpg,png}` and `client/Assets/Bloomlings/UI/Design/HomeLayersData.cs` (the
  layers' boxes);
- `client/Assets/Bloomlings/Art/Backgrounds/Resources/Backgrounds/*.{jpg,png}` (`backdrops.mjs`: what both builds show).

The bake is deterministic on one machine (the same hashes twice); another GPU emulation may differ in the last bits, so
`check.mjs` compares the committed files with the manifest rather than re-rendering them.

For the owner-requested four-motion Sprig experiment, the Settings test viewer, and the recommended modular accessory workflow, see [LAB.md](LAB.md). Its separate source and frame set do not rebake the production heroes.

For the Bloom texture cleanup prototype, projected back onto the original animated mesh and compared at production
resolution, see [textures/bloom-study/README.md](textures/bloom-study/README.md). It writes review artifacts only.

## Current production identities (2026-10-17)

TwigHeroV2 is the permanent Twig source, approved in game by the owner. Use
[models/twig-v2/README.md](models/twig-v2/README.md) for delivery, slots and rebuild instructions.
The owner's Blender project remains the sole editable master. This repository retains the supplied ZIP unchanged,
conversion scripts and baked 2D game assets; no second Blender master is maintained.

[references/twig-v2/README.md](references/twig-v2/README.md) indexes the bald base, default hat, Panama, hair,
glasses and seasonal references, plus current avatar / achievement art. Their SHA-256 hashes are checked.

Sprig, Bloom and Drop retain the simplified Tripo sources in [models/simple-tripo/](models/simple-tripo/README.md).
`bake-simple-heroes.mjs` publishes only those three; `SimpleHeroArt.Frame(Twig)` delegates to V2. The legacy
`bake.mjs` configuration also contains only those three families and preserves other publishers' manifest entries.

The former Twig motion frames, fitted sprites and generated C# data are retired under
[archive/twig/](archive/twig/README.md), outside Unity / APK resources. Old Twig publishers reject production
writes; `--review` is for historical offline study only. The historical source archives and earlier studies are
retained as provenance, not current modeling guidance. Old default-hat / Panama IDs remain valid. The retired
`hat.twig_flower_cap` ID resolves to the current Panama so an existing selection stays usable.

Current static bases retain subtle 2D breathing. Authoring skeletal actions and action-selection UI are separate
future work. Runtime 3D remains prohibited. The L2000 Twig mosaic is deliberately unchanged at the owner's request.

To rebuild the identity pictures after a V2 bake:

```sh
node tools/heroanim/bake-twig-blank.mjs
python tools/heroanim/pack-twig-identity.py
python tools/pack_badges.py --only hero_twig
node tools/heroanim/check.mjs
```

Only runtime images are published: three stable-ID avatars, the hero medal, solo / fallback / group art. Reference
PNGs, FBXs, ZIPs and conversion GLBs remain outside application resources.

Current additional delivery: 12 Twig items / 128 outfits / 35 sprites, including three Clothes choices. Full inventory and source mapping: [models/twig-v2/ITEMS.md](models/twig-v2/ITEMS.md). Warm accessory materials are in `twig-v2-look.json`.

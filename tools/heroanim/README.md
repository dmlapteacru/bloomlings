# tools/heroanim

Pre-renders the owner's animated heroes (FBX or glTF) into flat frame pictures for Home, the splash, the win and the milestone,
and prepares the owner's layered Home picture (spec 005 FR-028). The game never loads a 3D model (constitution VII):
both builds play the frames (`HeroMotion`, `HomeLayers` in the design kit). Source record and processing: `SOURCE.md`.

Requires Node 22 and Chromium for Playwright (`PLAYWRIGHT_BROWSERS_PATH`; the cloud sessions have it in
`/opt/pw-browsers`).

```sh
cd tools/heroanim
npm ci                               # three 0.160.0, playwright-core 1.56.1, pngjs, jpeg-js
node bake.mjs                        # all four heroes, about 6 minutes; --only <family> re-bakes one
node layers.mjs <folder>             # the owner's Home layers (01_home_bg_back.png … 04_home_soft_shadow.png)
node saturation.mjs                  # the other backgrounds at 70% of the heroes' saturation
node check.mjs                       # must pass before committing hero frames or Home layers (no npm packages needed)
```

| File | What |
|---|---|
| `heroes.json` | the bake: cell, foot line, fill and margin, frames per second, the clips' lengths on screen, supersampling, camera, light, blend times, and per hero its model, its clips (the idle, the reaction and maybe the win's `win`: one clip, or a list baked as `win`, `win2`), its head bones (`head`, `top`; Mixamo's by default; `topOffset` moves the top point that far along the top bone's axes, for a rig without a bone at the head's top), its ground point (`ground: "model"` for a model placed away from the origin: the middle of its posed bounds' base), its own clip lengths (null: the clips' own), its own `light` (any of `hemi`, `key`, `fill`, `rim`, and `exposure`, a factor on every light, and `lift`, a share of the albedo added as glow to open the shadows), its own `color` grade of the texture (`gamma` under 1 lightens the midtones, `gain`, `saturation`, `warm`) and its turn (`yaw`, toward the fountain's middle on Home; a hero with `winYaw` is baked a second time at that turn for the win, where it stands alone facing the player: its celebrations and its idle there, `winidle`) |
| `models/heroes.glb` | the owner's four heroes in one Blender file (`SOURCE.md`); `heroes.json` `mesh` picks each one, `breathe` adds a breath an idle lacks and `synth` makes stand-in clips for a hero delivered without any (Twig, for now); the bake still reads Meshy FBX and single `.glb` files |
| `bake.mjs`, `page.html`, `serve.mjs`, `png8.mjs`, `post.mjs` | the renderer (three.js in headless Chromium), the fit and crop, the palette PNG writer, the Home set's finish (`heroes.json` `home`: sharpening, contrast, saturation; the owner's Home tuning of 2026-10-05) |
| `layers.mjs` | the Home layers: crops, the lotus cut-out, the shadow, the JPEG garden (blurred by `gardenBlur`: 0 since 2026-10-08, the owner's garden comes blurred), the scene's saturation (`saturation.mjs`'s share) |
| `saturation.mjs` | the backgrounds' saturation (spec 005 FR-031): each owner background to 70% of the animated heroes' mean, lightness and hue kept; idempotent; `saturation.json` holds the last run's measurements |
| `check.mjs` | hashes of every output against `manifest.json` and `layers.json`, the generated kit files, the models and `heroes.json` |
| `manifest.json`, `layers.json` | what the last bake and the last layer run wrote |

Outputs:
- `client/Assets/Bloomlings/Art/Heroes/Resources/HeroMotion/<family>-<idle|react|win|win2>-<NN>.png` and the folder's
  `manifest.json` (the originality test's list, `client/THIRD_PARTY_NOTICES.md`);
- `client/Assets/Bloomlings/UI/Design/HeroMotionData.cs` (per frame: its crop in the 448 × 504 cell and the head points
  the hats follow);
- `client/Assets/Bloomlings/Art/Backgrounds/Resources/Backgrounds/home*.{jpg,png}` and
  `client/Assets/Bloomlings/UI/Design/HomeLayersData.cs` (the layers' boxes).

The bake is deterministic on one machine (the same hashes twice); another GPU emulation may differ in the last bits, so
`check.mjs` compares the committed files with the manifest rather than re-rendering them.

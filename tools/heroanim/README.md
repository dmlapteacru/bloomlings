# tools/heroanim

Pre-renders the owner's animated FBX heroes into flat frame pictures for Home, the splash, the win and the milestone,
and prepares the owner's layered Home picture (spec 005 FR-028). The game never loads a 3D model (constitution VII):
both builds play the frames (`HeroMotion`, `HomeLayers` in the design kit). Source record and processing: `SOURCE.md`.

Requires Node 22 and Chromium for Playwright (`PLAYWRIGHT_BROWSERS_PATH`; the cloud sessions have it in
`/opt/pw-browsers`).

```sh
cd tools/heroanim
npm ci                               # three 0.160.0, playwright-core 1.56.1, pngjs, jpeg-js
node bake.mjs                        # all four heroes, about 6 minutes; --only <family> re-bakes one
node layers.mjs <folder>             # the owner's Home layers (01_home_bg_back.png … 05_home_petals_overlay.png)
node check.mjs                       # must pass before committing hero frames or Home layers (no npm packages needed)
```

| File | What |
|---|---|
| `heroes.json` | the bake: cell, foot line, fill and margin, frames per second, the clips' lengths on screen, supersampling, camera, light, blend times, and per hero its model, its clips (the idle, the reaction and maybe the win's `win`), its head bones (`head`, `top`; Mixamo's by default), its own clip lengths (null: the clips' own), its own `light` (any of `hemi`, `key`, `fill`, `rim`, and `exposure`, a factor on every light, and `lift`, a share of the albedo added as glow to open the shadows), its own `color` grade of the texture (`gamma` under 1 lightens the midtones, `gain`, `saturation`, `warm`) and its turn (`yaw`) |
| `models/*.fbx`, `models/*.glb` | the owner's models (`SOURCE.md`): the Meshy FBX heroes and Twig's Blender `.glb` |
| `bake.mjs`, `page.html`, `serve.mjs`, `png8.mjs` | the renderer (three.js in headless Chromium), the fit and crop, the palette PNG writer |
| `layers.mjs` | the Home layers: crops, the lotus cut-out, the shadow, the JPEG garden |
| `check.mjs` | hashes of every output against `manifest.json` and `layers.json`, the generated kit files, the models and `heroes.json` |
| `manifest.json`, `layers.json` | what the last bake and the last layer run wrote |

Outputs:
- `client/Assets/Bloomlings/Art/Heroes/Resources/HeroMotion/<family>-<idle|react>-<NN>.png` and the folder's
  `manifest.json` (the originality test's list, `client/THIRD_PARTY_NOTICES.md`);
- `client/Assets/Bloomlings/UI/Design/HeroMotionData.cs` (per frame: its crop in the 448 × 504 cell and the head points
  the hats follow);
- `client/Assets/Bloomlings/Art/Backgrounds/Resources/Backgrounds/home*.{jpg,png}` and
  `client/Assets/Bloomlings/UI/Design/HomeLayersData.cs` (the layers' boxes).

The bake is deterministic on one machine (the same hashes twice); another GPU emulation may differ in the last bits, so
`check.mjs` compares the committed files with the manifest rather than re-rendering them.

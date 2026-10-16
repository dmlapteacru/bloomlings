# Modular Sprig in the game

Actual game-screen renders using the bundled runtime sprites, not concept mockups. Owner decision and source workflow:
[one Blender master](../../product/plans/hero-asset-workflow.md). The supplied embedded textures are retained for this
equipment pilot; assistant-authored texture maps and the full animation/action feature are separate follow-up work.

## Try it

Build the darwin branch normally. Open Shop → Cosmetics → Sprig. Daisy Cap, Garden Glasses and Leaf Backpack are
free. Choose/Remove toggles each item independently and saves it. Default clears the outfit. The normal Shop unlock
is retained; the free items don't require the later L40 Wardrobe unlock. In Wardrobe they share the Accessories filter.

The cap switches the owner's prepared `CapOn` head state. The same saved outfit appears on Home, Wardrobe and Sprig's
win/milestone views. Avatar pictures, profile frames and badges remain separately selected. Other heroes are retained.
This first equipment integration uses a rest pose and subtle breathing; full skeletal actions are deferred.

## Screens

- [Shop: three free accessories](108-sprig-gear-shop-19.5x9.png)
- [Home: equipped Sprig beside the fountain](109-sprig-gear-home-19.5x9.png)
- [Wardrobe: independent accessory slots](110-sprig-gear-wardrobe-19.5x9.png)
- [Actual level win: equipment retained](111-sprig-gear-win-19.5x9.png)

## Validation

- 46 focused client tests: equipment combinations, family fitting, save serialization/merge, existing Wardrobe layout,
  hero-motion contracts and localization. The client scripts compile against the project's .NET Unity API stubs.
- Preview fixtures 108–111 on 16:9, 19.5:9 and 21:9: actual shop taps, no spending/confirmation, app save/reload,
  independent removal/re-equipping, Home, Wardrobe and a solver-completed L12 win. All passed.
- Existing purchase/scroll/navigation and animation-lock fixtures 26–28, 43, 74, 75, 88 checked. Fixture 27 was also
  rerun on all three aspect ratios; catalog tests locate the starter by ID and scroll it fully into view.
- Preview navigation checks passed: nine cold destinations, previous page retained until complete; delayed resources
  keep live previews running. This is desktop runtime verification, not a physical Android device performance test.
- `node tools/heroanim/check.mjs` verifies bake hashes and bundled files. Nine new cropped PNG8 images total
  **117,205 bytes**; 3D sources and these review screenshots are not included in the APK. No APK was built here.

Reproduce: `dotnet run --project playtest/preview -- --frames 108,109,110,111 --out content/work/sprig-gear-preview`.
Runtime bake: `node tools/heroanim/bake-sprig-gear.mjs`; record: `tools/heroanim/sprig-gear.json`.

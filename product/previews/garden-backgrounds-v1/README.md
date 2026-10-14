# Approved garden background implementation

`overview.jpg` compares six actual 1080 × 2340 screen renders: Home, Wardrobe, Profile, Leaderboard, Collections and Gameplay. Only the overview is reduced for review; source renders remain full size in the task artifacts.

The Shop background is the palette reference and remains unchanged. The full change includes the profile editor, all four gameplay theme backgrounds, victory/milestone and splash/transition cover. Home keeps the original fountain layers and heroes.

Validation:

- 35 focused client tests passed (background asset slots, Home layers, owner-picture placement and lotus iris behavior).
- 3 existing originality/resource-provenance checks passed.
- 16 screen states rendered and checked at 19.5:9; 11 more at each of 16:9 and 21:9 using Android's packed image bytes.
- Fixture 90 checks page-to-background routing, retained Home layers, theme/win asset availability and that the garden cover does not repaint the opening iris.
- Android C# compiled; the asset-packing target produced and verified 1,099 image assets. All 14 background files are listed once, decode successfully and retain their dimensions; JPEG bytes match the Resources inputs.
- The Shop screen PNG is byte-identical to the previous render.
- Background files total 2.906 MB (previously 2.696 MB), an increase of about 210 KB. Generator PNGs are not shipped. No APK was built and no device FPS measurement is claimed.

Reproduce main renders after activating the repository toolchain:

```sh
dotnet run --project playtest/preview -c Release -- --out /tmp/bloomlings-garden-review --frames 1,3,5,6,15,17,20,39,40,44,47,73,86,87,88,90 --shape 19.5x9
node tools/heroanim/check.mjs
```

Art provenance and rebuild instructions: `client/Assets/Bloomlings/Art/Backgrounds/README.md`.

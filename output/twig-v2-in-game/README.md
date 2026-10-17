# Twig V2 and nine free accessories

Actual game captures at 1080×2340 using packed runtime images from the owner's `TwigHeroV2.zip`.

- `variants.png`: the common base and seven exclusive Head parts, rendered from the supplied geometry.
- `home-base.png`: all four current heroes, Twig without accessories.
- `shop-top.png`: the first free Twig items.
- `shop-accessories.png`: glasses and scarf, selected through actual Shop cards after scrolling.
- `home-beret-scarf.png`: the saved beret and independent scarf on Home.
- `home-earmuffs-glasses-scarf.png`: the saved winter headwear, glasses and scarf together.
- `wardrobe-holiday-scarf.png`: the holiday hat and scarf on the fitting stand.
- `win-panama-glasses-scarf.png`: a real solved L13 victory, advancing to L14 and retaining all three selections.

The archive includes PumpkinHat, BucketHat, Hair, HatHair (combined hat + hair), Beret, EarmuffHair, XmasHat, Glasses
and Scarf. The first seven replace the Head slot; glasses and scarf are independent. All are free for this trial in
Shop → Cosmetics → Twig, available before L40. Removing one keeps the others, Default resets every slot, and both
actions survive restart. Current other heroes, independent profile avatars and core gameplay workers keep their behavior.
The Cosmetics scroll viewport begins below the fixed family row, so a long equipment list cannot paint behind the
hero tabs. Unity and the playtest use the same geometry; the Shop fixture compares the fixed header before / after
scrolling and selecting the last accessories.

## Verification

- 78 client tests passed, including all 32 Head × Glasses × Scarf combinations across save / reload, slot removal,
  free selection before L40, wrong-family rejection, existing save / merge, Wardrobe, art metadata and localization.
- `node tools/heroanim/check.mjs` passed: original archive / conversion / output hashes, unchanged source world
  geometry and UV checks, all 33 full-color PNG crops and the archived hero libraries.
- All 33 packed PNGs match their runtime masters byte for byte: 1,614,285 bytes total. Android assets include PNGs
  only; the ~33 MB delivery ZIP and derived FBX / GLB data stay outside the APK.
- Packed fixtures 109, 120, 124–128 passed at 16:9, 19.5:9 and 21:9 (21 screen captures), including actual selection
  of every delivered item through Shop, individual removal, default reset, reload, Home, Wardrobe and victory.
- After the Cosmetics clipping correction, fixture 125 passed again on all three shapes, including pixel comparison
  of the fixed family row before and after deep scrolling. The updated Shop capture includes that correction.
- Staged loading passed 11 cold destinations, 123 frames and 157 resources; the previous complete page remains until
  the destination is ready. 100 live-preview frames continued while 9,147 transient-effect draws were delayed.

The four supplied Idle / Joy / Walk / Wave actions and rig are retained in the source archive for the later animation
feature. This trial uses static rest-pose images with the existing subtle 2D breathing. No second Blender master or
APK was built. These desktop rendering / packaging checks do not measure device frame times.

Source mapping and rebuild: `tools/heroanim/models/twig-v2/README.md`.

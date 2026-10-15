# Loading and level transition performance — 2026-10-15

The Android painter now prepares procedural RGBA pictures, fallback backdrops, shape masks and ordinary bundled sprites on one low-priority producer. Cache misses enqueue work and return to drawing; disk reads, rasterization, decoding, premultiplication and bitmap preparation happen on the producer. Completion requests another frame. An owner background that is still decoding uses a flat temporary fill instead of generating a second garden. The UI thread alone adopts results into its drawing caches, so it cannot race a cache eviction or a recycled bitmap.

`PictureWorkQueue` deduplicates pending requests, limits ready results to 32 MiB, releases the oldest unused result and permits preparing it again after eviction. It measures Android bitmap allocations rather than guessing their size. The disk store keeps at most 8 MiB of pending RGBA writes; under disk pressure it can skip a cache write without losing the image in memory. Existing picture/sprite/hero caches retain their byte budgets. Android memory-pressure callbacks release older screens while protecting images drawn in the last two frames.

The splash follows actual preparation progress; its former three-second forced-open timer is removed. Play and Next use the closed lotus iris while the new level's resources prepare. Covered gameplay is not drawn. A level warms its win graphics in advance; the win prepares the next board. Prepared level instances are reused and their intro clocks restart when activated. Lotus ring petals scale with transforms around a fixed mask size.

The board rim previously shaded and supersampled an entire board-sized texture although its interior is covered by the grid. It now uses a rounded scanline gradient; individual fence stones and the dense cube tiles remain. A local desktop sample of the same 936 × 984 underlay took 1,124 ms before; direct calls after the change took 24–38 ms. These are C# raster timings, **not phone frame rates**. Android additionally performs this work off the UI thread. Insignificant floating-point changes in the rim ratio no longer create extra texture keys.

A win batches progression, milestones, collection, achievements and reward saves into one atomic, flushed snapshot. Save durability, backup recovery and cloud-save notification semantics remain; the batch emits one successful-write notification.

Validation:

- 70 client tests: save/batch/recovery, raster/volume/row determinism, reference layouts and phone controls.
- `dotnet run --project playtest/check -c Release`: golden animator replays, meta persistence, background execution/deduplication, budget eviction/requeue, error recovery and memory-pressure eviction.
- `dotnet run --project playtest/preview -c Release -- --frames 1,7,15,16,44,45,46,47 --shape 19.5x9`: gameplay/win/milestone/iris renders plus loading gates. A deliberately stalled producer must keep the splash closed beyond the old timeout and keep Next closed until ready; layouts are also exercised while sprites exist but their size/alpha is still pending.
- `dotnet run --project playtest/preview -c Release -- --perf --perf-warm --perf-serial --shape 19.5x9`: the journey now waits for resource gates before playing and fails if it does not actually win. Its Skia sprite path remains synchronous and does not measure Android's background decoding.

There is no usable Android device/emulator acceleration in this environment. Check real frame timing on a phone with `adb logcat -s Bloomlings`; the APK already logs frames over 34 ms. Release packaging checks must still verify the signed APK and all 1,096 packed image payloads.

## Collection follow-up

The gallery now caches completed mosaics separately from their live frames, captions and close control. Android records the mosaic once, then flattens the completed recording on the graphics producer; incomplete recordings never enter the bitmap cache. New resource requests invalidate a recording even if the producer finishes before its readiness check. Snapshot results use a 16 MiB LRU cache and release older entries under Android memory pressure. Keys include content version, level, picture id/version and mapping hash; placement is excluded so scrolling reuses a thumbnail. Covered rows skip their mosaic work.

For the same six-picture gallery, the desktop frame harness went from 3,381 draw operations per frame to 240 (about 93% fewer). This is an operation count, not a device FPS measurement. Fixture 87 checks warm-gallery draw cost and that the detail's close cross restores the exact scroll position. The Android changes were checked with `dotnet msbuild playtest/android -t:Compile`; no APK was built for this follow-up.

Purchase-size preview overrides remain available: `--purchase-size 0.85`, `0.75`, or `0.5` changes the rendered price faces; Production uses the approved 20% reduction outside Shop. The preview retains original touch areas, keeps three avatar columns, and keeps gameplay booster artwork centered. Render frames 17, 40, 86 and 88 for each scale, then run `--compare-purchases <parent-folder>` with `current`, `minus-15`, `minus-25`, and `minus-50` folders. The four Shop PNGs were checked byte-for-byte identical.

The approved gameplay booster row has no price labels. Empty tiles show only a refill plus, which immediately confirms the purchase of a pack (including Return and Bloom Burst before target selection). Confirm adds charges without using the booster; Cancel/Back spend nothing. Existing charges and guided free uses retain their normal use flow. Fixture 89 checks all four booster kinds and duplicate confirmation. Shop sizes remain unchanged; the non-Shop price faces use `ProfileGameDesign.PurchaseScale = 0.80`.

## Garden backgrounds (2026-10-14)

Distinct Home, Wardrobe, Profile/editor, Leaderboard and Collection/detail backgrounds now share the Shop's teal/jade/mint palette. All four gameplay themes and the win backdrop are restyled too. Splash reuses the Home courtyard, clipped outside the iris opening (Android/Skia clip paths; Unity's existing inverse-hole stencil). Transitions retain the same timings and lotus animation with a matching teal cover. Original Home fountain/hero layers remain separate and unchanged.

The 14 production background files total 2.906 MB versus 2.696 MB before this change: three additional page scenes add about 210 KB overall. Opaque backgrounds retain the existing 852 × 1846 canvas and JPEG format; original generator PNGs are not shipped. Existing asynchronous decode, byte-bounded sprite caches and memory-pressure eviction remain in use. Fixture 90 checks page routing, separate Home layers and the presence of every theme/win asset through the bundled loader. Android packing and compilation can be verified without building an APK.

## Complete purchase sizing (2026-10-15)

The original 20% change covered the runtime Wardrobe/avatar price pills, but omitted the profile editor's primary Buy, the confirmation Buy and Unity price pills. This follow-up applies the same 0.80 width/height and proportional type size to those purchase actions, and to paid Jam choices. Save/Cancel and owned-charge/free actions keep their existing presentation. Hit regions retain their original size; fixture 89 confirms a refill from the outer edge of the original Buy target, outside the smaller visible face.

The owner additionally requested 20% smaller actions in Shop Cosmetics and Animations. The shared catalog layout now reduces those action faces (including the wide last animation card); the entire card remains tappable and scrollable. Shop Packs remains unchanged. Android and Unity use the same layout constants. No source artwork or APK is regenerated for this adjustment.

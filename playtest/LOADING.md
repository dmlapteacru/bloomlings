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

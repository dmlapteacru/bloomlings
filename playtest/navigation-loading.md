# Complete navigation frames and phone texture sizing — 2026-10-15

Cold menu navigation used to expose the destination immediately while one background worker decoded sprites and
rasterized surfaces. Each missing draw returned without painting, so backgrounds, borders and illustrations appeared
one at a time. Home's speculative warmup reached only a few destinations; it was not a readiness check for navigation.

Android now records candidate frames as command lists and publishes only complete frames. While a visible sprite,
surface or mask is pending, it retains the previous complete command list and draws a small progress ring. Cache ages
advance on publication, protecting retained bitmap references. There is no additional full-screen screenshot bitmap.
Animation/coasting clocks pause during preparation; physical gesture timestamps remain independent, and an existing
swipe can finish and resume coasting after reveal. Hidden-page taps are blocked. Visible requests overtake queued idle
work, including a request first queued as speculative. Unrelated speculative requests do not hold a complete frame.
Short-lived clearing particles are excluded: an asynchronously prepared particle must not pause gameplay or a live
Shop preview. Static board tiles, card surfaces and controls still participate in readiness.

Idle menu pages prepare all main destinations. Shop also prepares resources in a strip 0.6 viewport heights above and
below the visible content. The first viewport, tab changes, overlays and newly exposed scroll content all use the same
publication rule. A genuinely cold page can still take time to prepare; the change removes partial-frame presentation
and reduces the amount of work, rather than promising instant first loads on every phone.

`PhoneTextureProfile` sizes Android build derivatives according to use. Masters, logical names, source manifests,
save identifiers and the 900 indexed hero frames are unchanged. Small images are not enlarged. Atlas crops/nine-slice
borders remain proportional to decoded sizes. PNG/WebP encoding is checked against resized pixels; resized JPEGs use
quality 90. Resolution reduction is a quality tradeoff, not lossless resizing. Home's sharp background is retained.

Measured against the shipping derivatives at `e4c2e948`, default `GameAssetMaxEdge=1024`:

| Texture family | Previous RGBA MiB | New RGBA MiB |
|---|---:|---:|
| Backgrounds | 62.6 | 49.1 |
| Shop | 43.8 | 36.6 |
| Navigation | 39.6 | 33.7 |
| Characters | 28.8 | 25.1 |
| Icons | 24.2 | 15.1 |
| Decorations | 13.6 | 7.0 |
| Avatars | 7.9 | 3.5 |
| Gameplay atlas | 6.0 | 3.0 |
| Badges | 5.8 | 5.8 |
| Logo | 2.0 | 0.8 |
| **All ordinary textures** | **234.19** | **179.70** |

This is a **23.27% reduction in the sum of ordinary RGBA texture sizes**, not measured process RAM or VRAM; the runtime
loads selected images into bounded caches, and native/GPU allocations have additional costs. Compressed image payloads
(all 1,099 images, including heroes) decrease from **66.69 to 62.74 decimal MB**. These are asset bytes, not APK sizes.

Validation: the Android compile target succeeds; no APK was built. The packer verifies all shipping dimensions and
PNG/WebP/palette round trips. NUnit exercises PNG/JPEG packing and cache reuse, retained-frame lifetime over 200 delayed
draws, priority promotion, deduplication, eviction and worker faults. Real menu layouts are tested with one dependency
released per draw: 9 cold destinations, 120 staged draws, 152 resources; no partial page is published. The quick-swipe
fixture also verifies velocity with a paused scene clock and coasting after release. Sixteen previews use actual packed
assets, covering menus, packs, cosmetics, profile, settings, scroll, gameplay and demos; the preview checks all 900 hero
frames. Desktop checks do not measure Android render-thread/GPU performance.
An additional 100-frame live-preview check with delayed effect resources verifies that transient particles never hold
the page's publication gate.

Useful phone check: cold launch, immediately open Shop, switch tabs, swipe to Petals packs, then visit each menu and
return. Repeat once warm. `adb logcat -s Bloomlings` reports `ready frame on ...: N ms preparing behind retained page`
and the existing slow-frame diagnostics. Preparation may remain visible on a slow device, but individual assets should
not appear sequentially over a blank page. Compare RAM/GPU and timing on the same phone/build configuration.

Reproduce packing and desktop checks using [the asset shipping instructions](../tools/assetpack/README.md).

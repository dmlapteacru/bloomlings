# Shipping game assets

`playtest/android` imports `playtest/OptimizeAssets.targets`. The packer builds phone derivatives under
`obj/<configuration>/net10.0-android/packed-images` and replaces image items before Android computes its asset paths.
Original artwork, Unity resources, their manifests and saved cosmetic identifiers stay unchanged.

- Atlas PNGs in Navigation, Shop and Gameplay: maximum edge 1024 px, premultiplied bilinear filtering, then lossless WebP when smaller.
  Transparent edges are filtered in premultiplied space. This reduces resolution; it is not a claim of pixel-identical
  full-resolution artwork. Set `-p:GameAssetMaxEdge=1536` (or 8192 to retain these masters' dimensions) for higher density.
- Individual icons and decorations: 384 px; badges and avatars: 256 px; characters: 512 px (group artwork: 1024 px);
  logo: 768 px. Single selected-tab/plus/profile-frame/offer images have separate limits in `PhoneTextureProfile.cs`.
  Atlas cells retain their larger budget because several logical images share one sheet.
- Full-screen gardens: maximum edge 1600 px. The sharp Home courtyard retains 852 × 1846; fountain layers are unchanged.
  Resized JPEGs use quality 90; unresized JPEGs are copied verbatim. No image is enlarged.
  These limits scale with `GameAssetMaxEdge / 1024`; 8192 retains the masters' sizes for comparison.
- Hero frames: BPF1 (Brotli-packed 256-color palette and indices) only when smaller than the original PNG. No resize,
  color quantization, dropped frames, animation timing changes or increased decoded memory: still one byte per pixel.
  `PalettePng.Decode` accepts both PNG and BPF1. All 900 frames are independently checked against master PNGs by the preview.
- Every new encoding is decoded and checked against its input pixels. WebP may discard RGB at completely transparent
  pixels; visible straight RGB and alpha must match exactly. The UI check uses the resized input.
- Content hashes plus recipe version/settings reuse unchanged outputs. Only image payloads ship; cache stamps,
  input manifests and the size report never become Android assets.
- `report.json` includes source/shipped dimensions and RGBA byte estimates, independently of compressed file bytes.
  Its all-texture sum is a sizing comparison, not a claim that every image is resident at once.

Asset filenames are stable logical resource keys. Their `.png` suffix is preserved for existing callers, regardless
of whether the payload is PNG, WebP or (for heroes only) BPF1. Android/Skia inspect headers; heroes use the palette decoder.
Changing extensions must not break `PainterBase.SpriteResource` or the extensionless hero/background/avatar aliases.
All atlas crops and nine-slice borders must scale with decoded dimensions; `ShopBodyDesign.PriceCrop` handles the
price button that previously used hard-coded source coordinates.

Build with `dotnet publish playtest/android -c Release`. `-p:OptimizeGameAssets=false` restores original asset packaging
for comparisons. Local builds and the manual `android-apk` workflow default to ARM64. The workflow also offers ARM32,
x64 and universal choices; it ships one selected APK. For a local universal build, pass
`'-p:RuntimeIdentifiers=android-arm64;android-arm;android-x64'`. For ARM32/x64, pass `-p:RuntimeIdentifiers=android-arm`
or `-p:RuntimeIdentifiers=android-x64`. AOT remains enabled.

The APK workflow stamps the version name with the first eight commit characters and uses UTC Unix seconds for
the Android version code, so a fresh CI build can update a local review build. For matching local builds, pass
`-p:ApplicationDisplayVersion=0.1.0-<commit>` and `-p:ApplicationVersion=<UTC-Unix-seconds>`.

Android's regular sprite cache now uses the existing `PictureCache<T>` LRU with a 64 MiB budget. It evicts old screens
at frame boundaries, protects the current/previous two frames, and can temporarily exceed the budget for visible artwork.
Hero/picture cache budgets remain separate. The Android asset index includes navigation, shop and gameplay folders.

Android records each candidate frame as a display list. If any visible sprite, raster or mask is still being prepared,
it keeps the last complete display list and shows an asset-free progress ring. It publishes the new page only when all
its visible dependencies are present; this also covers tab changes, overlays and newly scrolled content. It does not
allocate a full-screen RGBA screenshot per frame. Animation clocks pause during this wait, input is blocked, and cache
ages advance only on publication so the retained page's bitmaps cannot be evicted or reused underneath it.
Visible requests (including already queued ones) overtake idle preload jobs. Idle menus prepare all main destinations;
Shop additionally warms the next/previous content strip outside the viewport. Speculative preload is not itself a
reason to hide an otherwise complete frame. Short-lived clearing particles are also excluded from this gate so a
new effect size cannot pause the entire game or a live preview; the board, cards and controls remain required.

Validation:

```sh
dotnet test tools/assetpack-check -c Release
dotnet run --project playtest/preview -c Release -- --out /tmp/bloomlings-packed \
  --frames 2,3,5,6,17,19,21,26,37,38,49,63,67,73,79 --shape 19.5x9 \
  --packed-assets "$PWD/playtest/android/obj/Release/net10.0-android/packed-images"
python3 tools/assetpack/apk-size.py path/to/Signed.apk --out /tmp/apk-size.json
```

The packer reports source/shipping bytes in decimal MB; the APK audit reports MiB (1024² bytes).
Pixel/desktop render checks do not replace profiling or testing on an Android device.

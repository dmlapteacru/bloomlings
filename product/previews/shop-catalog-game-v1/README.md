# Implemented Shop catalog

2026-10-13, branch `codex/shop-garden-body`. These are actual Skia playtest renders of the shared implementation, not generated screen concepts. The approved references are in `../shop-tabs-concepts-v1/`.

## Screens

- `26-store-cosmetics-19.5x9.png`: first catalog viewport, existing Sprig hero.
- `56-store-cosmetics-bloom-19.5x9.png`: existing Bloom hero, same accessories and fixed family navigation.
- `55-store-cosmetics-bottom-19.5x9.png`: lower catalog after the fixture buys Bramble Frame (owned items leave the for-sale list).
- `42-store-animations-19.5x9.png`: all seven live effects, Fireflies owned and chosen.
- `43-store-animations-locked-19.5x9.png`: paid styles locked before level 40, with their actual previews.
- Matching `16x9` captures show short-screen scrolling.
- `49-purchase-confirm-19.5x9.png`: existing purchase confirmation from an animation card.
- `17-store-19.5x9.png` and `53-store-packs-19.5x9.png`: Packs regression captures.

## Implementation

`ShopCatalogDesign` shares card proportions, preview wells, labels, action geometry and runtime atlas crops between native Unity and Skia/Android. Cosmetic cards have pink rims and flowers; animation cards have blue rims and a green/gold chosen state. Generated skins are nine-sliced. Labels, prices, buttons, checks and previews remain live elements. Existing green purchase-button art is reused.

Cosmetics retains Default plus all ten catalog items, existing prices, family filtering and actual for-sale ownership rules. Animations retains all seven styles, existing prices, Choose/Chosen/Buy/Locked states and actual effect choreography. The wider Confetti Parade board is a catalog-only preview; gameplay preview defaults and timing are unchanged. Existing heroes are reused. Only Bucket Hat, Bell Cap, Bramble Frame, Watering Can Badge and the card skins received generated art.

All Shop tabs reuse the existing Shop background image. Header, secondary navigation, family navigation and bottom navigation stay outside the vertical scroll viewport. Money products retain the existing coming-soon stubs.

Asset provenance, exact prompts, original output filenames and rendering contract: `client/Assets/Bloomlings/Art/Shop/SOURCE.md`. SHA-256 hashes and dimensions: `client/Assets/Bloomlings/Art/Shop/Resources/Shop/manifest.json`. Built-in OpenAI ImageGen was used; source PNGs and transparency are preserved unchanged.

## Verification

- .NET compilation against Unity API stubs and **59 tests passed**, including default clearing previews, catalog loop coordinates/effect slots, clearing transactions, economy, localization and reference layouts.
- **27 rendered fixtures passed**: 17, 26, 42, 43, 49, 53, 54, 55, 56 at 16x9, 19.5x9 and 21x9. The renderer checks text/layout, slots, touch targets and scripted interactions.
- Catalog interactions check that drags never buy, family changes reset the scroll, lower profile items remain reachable, cancellation spends nothing, and a confirmed Bramble Frame purchase spends the existing 350 Petals exactly once.
- Reviewed actual tall/short screenshots, Bloom accessories, lower items and locked styles. Curated captures are retained here; the full render batch passed before removing redundant PNGs.
- Unity Editor/device/APK execution was not performed. Temporary offline project files were used with locally cached .NET dependencies and removed after verification.

# Illustrated navigation, revision 1

Actual shared-code Skia renders, not concept mockups or Unity device captures.

## Changes

- Two generated transparent wooden plaques: page/popup titles and level/celebration titles. Both use matching 9-slice geometry in Unity and Skia/Android. Titles and level numbers remain live localized Lilita One text with cream fill and brown outline.
- New generated 12-icon navigation atlas. Crops follow full subject bounds rather than rigid cell edges, preserving the shirt sleeve and marker.
- Raised colored capsules replace second-level tab surfaces: Shop tabs, profile editor tabs, Wardrobe item-kind filters and family navigation. Selected capsules are green with a pale gold rim/glow; others use cool violet/blue tones. Icons sit at the left of outlined white labels. Family navigation reuses the pod symbols.
- Store is localized as Shop. Its tabs are Packs, Cosmetics, Animations; the existing unlock conditions and actions remain in place.
- Petals balance uses outlined live text; the plus reuses the existing green money-purchase PNG with the shop button's slice geometry.
- Larger page-header plaques contain their own decorations. A small extra profile achievement gap keeps the pager's minimum touch targets clear of cards on 16:9 screens.

## Verification

- Unity runtime/UI importer compiled against .NET API stubs: zero warnings/errors.
- Existing reference-layout, pod-link and localization tests: 37 passed.
- Sixteen screen fixtures rendered across 16:9, 19.5:9 and 21:9. All passed after correcting the profile pager/card overlap on 16:9 and rerunning that fixture across all three shapes. The updated profile captures replace the earlier files.
- Inspected Shop, gameplay, Level Complete, profile editor, Cosmetics and Wardrobe.
- Screens include Home, Daily Reward, Leaderboard, Collection, gameplay, jam, pause, win, milestone, Shop, Cosmetics, Wardrobe, profile, editor/frames and Animations.

Background PNGs, main bottom navigation and game rules are unchanged. Unity Editor/device and Android APK builds were not run. Generated source artwork, exact prompts and hashes are recorded in `client/Assets/Bloomlings/UI/Resources/Navigation/SOURCE.md` and `manifest.json`.

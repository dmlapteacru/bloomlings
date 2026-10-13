# Illustrated navigation — implemented

Approved references: `../navigation-controls-concepts-v2/01-navigation-buttons.png` and `02-profile-wallet.png`. These captures are renders from the shared game code, not generated mockups.

## Production assets and fidelity

Four transparent ImageGen atlases reproduce the approved silhouettes, sculpted rims, raised glyphs, color palette and lighting. Original generated PNGs are copied unchanged into `client/Assets/Bloomlings/UI/Resources/Navigation/`. Exact prompts and original output paths are recorded here; dimensions/hashes and provenance are in the Navigation art records. Atlas rectangles are cropped at runtime from shared `GameNavigationDesign` coordinates. The source RGBA pixels are retained; this is a faithful recreation from the approved references, not a measured claim of zero pixel difference from the generated concept boards.

- Back/Settings/Pause/Close and speed use illustrated glyph/button faces. Speed has cream and green states and a live 2x label. Native controls retain button callbacks, click sound and press motion.
- All five primary navigation destinations use inactive/selected illustrated tiles. Selected captions are live and localized. Unlocks, padlocks, destinations and active-place behavior remain intact. The secondary cream segmented navigation keeps its approved style.
- Petals wallet and profile name ribbon use nine-slice only in their plain stretchable centers; separate plus/lotus/numbers remain live. Large grouped balances fit both Home and the narrow page header.
- Home avatar/frame/badge are live layers; no mandatory arch, baked portrait or forced badge. Wood/Leaf/Gold frame skins reproduce the approved examples; other available frames retain their existing art. The same illustrated frame material is used on main Profile. Name ribbon sits against the portrait, badge draws above it, and name text reserves badge space. Long mini-caption names ellipsize for readability; full Profile name and stored data remain unchanged.
- Original game backgrounds, character portraits, heroes, promos, gameplay, shop products and economy are retained. A taller menu reserve on tall phones accommodates the reference's larger tiles; short phones retain a compact reserve and the existing content capacity.

## Verification

Native client compilation against Unity API stubs and 61 relevant .NET tests passed. Cached NUnit 4 and prebuilt core DLLs were used with temporary local build projects; production project files remain unchanged. Existing responsive layout tests were updated for the new menu reserve.

36 fixture/shape checks passed: 2,3,5,7,17,20,60,66,67,68,69,70 on 16x9,19.5x9,21x9. These cover early locked navigation, progressed Home, Leaderboard, ordinary/fast gameplay, Shop, popup Close, full Profile, Wood with no badge, Leaf with Sprout, Gold with Watering Can, and a 120000 balance in the narrow Shop header. The new live-cosmetic fixtures verify profile opening and restoration of the current name after returning Home. Tall and short captures, all three profile combinations, balance and both speed states were visually reviewed. Art manifest integrity and source alpha were checked. Unity Editor/device/APK execution was not performed, so exact Unity/device pixel parity is not claimed.

Curated captures retain Home examples on tall/short phones, shared page/navigation and gameplay controls, and the large wallet. Redundant render-batch copies are removed after validation.

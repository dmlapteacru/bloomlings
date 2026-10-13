# Vertical garden store, revision 2

Actual shared-layout PNG renders using the Skia playtest painter, not concept screenshots or Unity device captures.

- `17-store-19.5x9.png`: top of the shop, showing the illustrated Best Sale card, larger No Ads cards and narrower/taller nine-slice purchase buttons.
- `53-store-packs-19.5x9.png`: scrolled currency packs and regenerated booster icons with larger outlined prices.
- `54-store-body-purchases-19.5x9.png`: scrolled content after buying a three-charge booster, also showing disabled prices.
- `board-sheet.png`: all three store fixtures at 16:9, 19.5:9 and 21:9.

## Verification

Unity runtime scripts and the texture importer compile against .NET Unity stubs with zero warnings/errors. This verifies API use, not rendering on a device.

The three preview fixtures cover all three phone shapes. Final result: `checks: OK`. Fixture 54 verifies that a money preview grants nothing; a vertical drag starting on a price moves only content and cannot buy; header and bottom navigation retain their positions; a horizontal drag does not change scroll offset or page; header/navigation are outside the scroll area; booster confirmation and cancellation preserve their previous behavior; confirmed buying grants three Extra Slot charges for 1,200 Petals; scrolling clamps at both ends.

The Android painter consumes the same layout and sprites. Its device build was not run because no Android workload is installed in this environment.

Assets and exact generation prompts: `client/Assets/Bloomlings/Art/Shop/SOURCE.md` and `prompts-v2.json`. Original revision previews remain under `product/previews/shop-body/` for comparison.

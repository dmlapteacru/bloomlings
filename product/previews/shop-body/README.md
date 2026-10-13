# Garden store body preview

These are rendered from the shared layout and actual runtime PNG layers, using the Skia playtest painter. They are not flattened concept mockups or Unity device captures.

- `17-store-19.5x9.png`: first page, including the Best Sale offer, both Remove Ads offers, small currency packs and boosters.
- `53-store-packs-19.5x9.png`: second page, including medium/large bundles and large currency packs.
- `board-sheet.png`: the three store fixtures across 16:9, 19.5:9 and 21:9.

Only the Shop tab's body and backdrop changed. Existing header, wallet, tab buttons, footer arrows and bottom navigation retain their previous implementation. Cosmetics and Animations retain their previous body rendering. Short screens have a third page for boosters.

## Verification

- Unity runtime scripts and the new texture importer compile against the repository's .NET Unity stubs: zero warnings/errors. This is not a Unity Editor or device build.
- Preview fixtures 17, 53 and 54 render at all three screen shapes; final result: `checks: OK`. Checks cover registered assets, available sprites, text fitting, safe areas and touch targets.
- Fixture 54 exercises the money-purchase stub without granting currency or Remove Ads, booster confirmation, cancellation without spending, and a confirmed purchase charging 1,200 Petals and granting three Extra Slot charges.
- For this machine's cached dependencies, temporary untracked check projects referenced existing core DLLs and cached packages; those temporary files are not part of the implementation.
- A shared PictureStore cache eviction check failed intermittently during one run and passed on subsequent runs without changes to cache code. The shop scenarios passed.

Art, prompt specifications and slicing coordinates are recorded in `client/Assets/Bloomlings/Art/Shop/SOURCE.md`. Monetary prices are preview copy until platform products are connected.

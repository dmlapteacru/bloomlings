# Garden store, revision 6

Actual shared-layout Skia renders; not concept mockups or Unity device captures.

- `17-store-19.5x9.png`: centered tilted `BEST SALE` badge and tighter No Ads reward-panel padding.
- `53-store-packs-19.5x9.png`: compact bundle reward panels with equal content padding and a larger right margin.
- `54-store-body-purchases-19.5x9.png`: scrolled content after a three-charge booster purchase.
- `board-sheet.png`: the three fixtures at 16:9, 19.5:9 and 21:9.

Reward-panel height follows square icon rows plus their overlapping count labels. Equal padding is measured in physical layout units rather than separate width/height percentages. The 1,200-Petals No Ads bundle retains its previous outer right margin. Featured-card rewards, navigation, purchase logic and source PNGs remain unchanged.

Final result: `checks: OK` on all three shapes, including purchase, cancellation, fixed navigation, vertical scrolling and featured-card containment. Runtime code compiled against the repository's .NET Unity API stubs with zero warnings/errors. Device rendering was not tested.

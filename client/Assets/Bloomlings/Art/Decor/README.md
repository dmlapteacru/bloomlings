# Decor (the owner's leaf pictures)

Drop the leaf pictures of `specs/005-reference-look/pictures.md` (section D) into `Resources/Decor/` with their exact
names (transparent PNG; the names are in `OwnerPictures`):

| File | Size | Drawn |
|---|---|---|
| `ivy.png` | 512 × 512 | over the LEFT end of a wooden sign (the level sign, the Wardrobe and Store banners), its middle on the plank's end; mirrored for the right end |
| `flowers.png` | 512 × 512 | on the top-left of the "Level complete!" sign; mirrored for the other end |
| `button-leaves.png` | 256 × 256 | on the top-left corner of the Play and Next buttons; turned half way for the bottom-right corner |
| `logo-leaves.png` | 512 × 512 | only while the logo picture is missing: behind the left end of the drawn wordmark; mirrored for the right |

- Unity loads them from `Resources/Decor/` (`OwnerArt.Decor`, used by `UiKit.IvyCluster`, `UiKit.FlowerCluster`,
  `UiKit.Decoration` and `UiKit.WoodLogo`; mirrored with a negative `localScale.x`).
- The playtest embeds the folder and draws a picture in the drawn cluster's box (`Kit.IvyCluster`,
  `Kit.FlowerCluster`, `Kit.Decoration`, `Kit.WoodLogo`), mirrored with `IPainter.PushSquash(-1, 1, …)`.

While a picture is missing, both builds draw the code-drawn leaves instead. Each picture needs a row in
`client/THIRD_PARTY_NOTICES.md` (its path, and its source record with the tool, author and licence as the licence
file); the originality test fails without it.

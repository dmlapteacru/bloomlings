# Icons (the owner's booster icons, variant icons and lotus)

The owner's icon pictures of `specs/005-reference-look/pictures.md`, in `Resources/Icons/` with their exact names
(transparent PNG, the icon only with a small margin: the game draws the box, the tile, the badge and the price):

- Booster icons (section D): `booster-extra_slot.png`, `booster-shuffle.png`, `booster-return.png` and
  `booster-bloom_burst.png` (512 × 512; `OwnerPictures.BoosterIcon`).
- Variant icons (section G): the detailed `variant-{leaf,moss,flower,bud,drop,dew,log,acorn}.png` (512 × 512, G9–G16;
  `OwnerPictures.VariantIcon`) for the sticker tiles of pods, slots, the jam row and flights, and the simplified
  `field-{…}.png` (256 × 256, G17–G24; `OwnerPictures.FieldIcon`) for the board tiles and the finished picture.
- The Petals lotus: `currency-lotus.png` (256 × 256; `OwnerPictures.CurrencyLotus`).

How the builds use them:

- Unity loads them from `Resources/Icons/` (`OwnerArt.Icon`, used by `UiKit.BoosterIcon`, `UiKit.PetalIcon` and the
  candy tiles' `CandyTileView`; `OwnerIconImporter` imports them with mipmaps). A stuck tile's grey copy and the
  finished picture's baked icons are read back once from the texture (`OwnerArt.TileIcon`, `OwnerArt.IconPixels`).
- The playtest embeds the folder (`playtest/android` and `playtest/preview`) and draws a picture in the icon's box
  (`Kit.BoosterIcon`, `Kit.Petal`, `Kit.CandyTile`); a disabled booster's picture fades instead of turning grey, a stuck
  tile's icon is a grey copy the painter makes (`icon/variant-{id}#grey`).
- A candy tile with its icon is the drawn face without its symbol (`UiRaster.TileFace`) and the picture over the face's
  middle (`OwnerPictures.TileIconBox`; spec 005 contracts/look.md §3.11).

While a picture is missing, both builds draw the code-drawn icon, symbol or lotus instead. Each picture needs a row in
`client/THIRD_PARTY_NOTICES.md` (its path, and its source record with the tool, author and licence as the licence file);
the originality test fails without it.

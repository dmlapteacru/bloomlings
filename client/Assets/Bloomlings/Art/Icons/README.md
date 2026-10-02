# Icons (the owner's booster icons)

Drop the booster icon pictures of `specs/005-reference-look/pictures.md` (section D) into `Resources/Icons/` with
their exact names: `booster-extra_slot.png`, `booster-shuffle.png`, `booster-return.png` and `booster-bloom_burst.png`
(512 × 512, transparent PNG, the icon only with about 6% margin: the game draws the cream box, the count badge and the
price). The names are in `OwnerPictures.BoosterIcon`.

- Unity loads them from `Resources/Icons/` (`OwnerArt.Icon`, used by `UiKit.BoosterIcon`).
- The playtest embeds the folder (`playtest/android` and `playtest/preview`) and draws a picture in the icon's box
  (`Kit.BoosterIcon`); a disabled booster's picture fades instead of turning grey.

While a picture is missing, both builds draw the code-drawn icon (`GardenLook.BoosterIcon`) instead. Each picture needs
a row in `client/THIRD_PARTY_NOTICES.md` (its path, and its source record with the tool, author and licence as the
licence file); the originality test fails without it.

# Backgrounds (the owner's pictures)

Drop the background pictures of `specs/005-reference-look/pictures.md` (section B) into `Resources/Backgrounds/` with
their exact names: `home.png`, `splash.png`, `wardrobe.png`, `win.png`, `gameplay-daylight.png`, `gameplay-pond.png`,
`gameplay-orchard.png` and `gameplay-moonlit.png` (1080 × 2340, opaque PNG). The names are in `OwnerPictures`.

- Unity loads them from `Resources/Backgrounds/` (`OwnerArt`); `wardrobe.png` is Unity only (the playtest has no
  Wardrobe screen).
- The playtest embeds the folder (`playtest/android` and `playtest/preview`) and cover-fits each picture
  (`Visuals.Background`).

Since spec 005 FR-049 (2026-10-08) the pictures here are the finish of the owner's as delivered: put a new or changed
picture into `tools/heroanim/backgrounds/` and run `node tools/heroanim/backdrops.mjs`, which writes it here a little
blurred and darker (and `node tools/heroanim/check.mjs` must pass).

While a picture is missing, both builds draw the code-drawn backdrop (`BackdropRaster`) instead (for `win.png`, the
`BackdropScene.Win` garden: the gameplay garden blurred and lightened with a warm glow in the middle). Each picture needs a
row in `client/THIRD_PARTY_NOTICES.md` (its path, and its source record with the tool, author and licence as the
licence file); the originality test fails without it.

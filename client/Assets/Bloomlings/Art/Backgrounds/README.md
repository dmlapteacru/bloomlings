# Backgrounds (the owner's pictures)

Drop the background pictures of `specs/005-reference-look/pictures.md` (section B) into `Resources/Backgrounds/` with
their exact names: `home.png`, `splash.png`, `wardrobe.png`, `gameplay-daylight.png`, `gameplay-pond.png`,
`gameplay-orchard.png` and `gameplay-moonlit.png` (1080 × 2340, opaque PNG). The names are in `OwnerPictures`.

- Unity loads them from `Resources/Backgrounds/`.
- The playtest embeds the folder (`playtest/android` and `playtest/preview`) and cover-fits each picture
  (`Visuals.Background`).

While a picture is missing, both builds draw the code-drawn backdrop (`BackdropRaster`) instead.

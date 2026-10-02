# Brand (the owner's logo)

Drop the logo picture of `specs/005-reference-look/pictures.md` (section C) into `Resources/Brand/`: `logo.png`
(1200 × 440, transparent). The names are in `OwnerPictures`.

- Unity loads it from `Resources/Brand/` (`OwnerArt`).
- The playtest embeds the folder and fits the logo into the wordmark's place (`Visuals.Logo`).

While `logo.png` is missing, both builds draw the wooden wordmark letters (`ui.logo.wood`) instead. The picture needs a
row in `client/THIRD_PARTY_NOTICES.md` (its path, and its source record with the tool, author and licence as the
licence file); the originality test fails without it. The optional
`tagline.png` (C2, slot `brand.tagline`) is kept for later: neither build shows a tagline yet.

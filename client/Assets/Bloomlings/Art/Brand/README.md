# Brand (the owner's logo)

Drop the logo pictures of `specs/005-reference-look/pictures.md` (section C) into `Resources/Brand/`: `logo.png`
(1200 × 440, transparent) and optionally `tagline.png` (1000 × 80, transparent). The names are in `OwnerPictures`.

- Unity loads them from `Resources/Brand/`.
- The playtest embeds the folder and fits the logo into the wordmark's place (`Visuals.Logo`).

While `logo.png` is missing, both builds draw the wooden wordmark letters (`ui.logo.wood`) instead.

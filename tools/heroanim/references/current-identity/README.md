# Accepted hero identity artwork

Generated for Bloomlings with OpenAI ImageGen on the owner's request to permanently
replace all previous heroes, using these exact current production renders as references:
`sprigv2-base-hairleafy.png`, `bloomv2-base-hairpetals.png`, `dropv2-base-nativedrop.png`.
The existing accepted Twig medal supplied the shared gold / blue / floral frame style.
No third-party artwork was used. The owner's models and texture geometry were not altered.

`avatar-atlas.png` is a four-by-four equal-cell atlas. The first eleven cells (four Sprig,
four Bloom, three Drop) retain the catalog's existing themes: default, ladybug, dewdrops,
daisy crown; default, ribbon, pearl tiara, jeweled crown; default, sailor, bubble / shell.
Cells beyond these eleven are unused drafts and are not packaged. Publication crops inside
the grid dividers and resamples to the existing 384px avatar size, preserving all IDs,
prices, owned avatars, frames and saved choices.

`props-atlas.png` has three-by-two equal cells with generous transparent packing margins:
three matching current-character medals in the top row; waving and pushing Sprig in the
bottom row; empty final cell. The generating prompt explicitly requested pure transparent
background, no haze or glow, complete isolated silhouettes, and small assets centered within
each cell. Raw PNG RGB under zero alpha may contain matte colors; runtime preserves and
uses the alpha. The packer extracts whole silhouettes, trims only packing space, retains
soft alpha edges, and encodes 256px medals / 512px promo sprites.

`blanks/*.png` are actual offline renders of the accepted GLBs with flat face shaders,
not generated alternative characters. Their model hashes / render code are pinned in
`../../current-blanks.json`. `../../pack-current-heroes.py` publishes all derivatives.
SHA-256 inputs, outputs and dimensions are in `../../current-identity.json`; the accepted
Twig identity is independently pinned in `../../twig-identity.json`.

# Home navigation and header proposals

Owner request: propose a redesign of navigation buttons, the Petals wallet and the Home profile plaque. Concept-only, no runtime changes.

Baseline `03-home-progressed-19.5x9.png` was freshly rendered from the current playtest code (Level 88); render checks passed. The implemented Profile is the material reference. The garden backdrop, existing heroes/fountain, promos, daily challenge, Play, level sign and milestone teaser are outside the proposed change.

## Variants

1. `01-stone.png`: individual ivory stone navigation pedestals, raised coral Home selection; ivory/gold Petals wallet and separate green round plus; compact framed portrait and wooden name tag.
2. `02-wood.png`: a low light wooden ledge supporting open navigation icons, raised wooden Home medallion; cream/wood wallet and green round plus; miniature stone portrait arch with a name ribbon.

Navigation destinations/order remain Shop, Wardrobe, Home, Leaderboard, Collection. Only selected Home is captioned to keep the small menu readable. Profile uses existing portrait/customization and opens Profile; name text is illustrative, not a new action. There are no new stats, rewards, currencies or game mechanics. Generated concepts are not replacement character files.

## Production approach after selection

Generate blank skin assets for the wallet, plus, control face and selected navigation foundation. Draw balance, name and glyphs independently. Slice the wallet's uninterrupted middle for large balances; preserve decorative silhouettes with uniform scale. Reuse the existing destination icons and actual avatar/frame/badge catalog. Share layout and safe-area logic between Unity and the playtest; keep header/menu fixed and allow balanced touch targets without making decorative faces oversized.

Built-in OpenAI ImageGen; exact prompts in `prompts.json`, original output paths in `outputs.json`. Full screen concepts may contain generated approximations; implementation must preserve the existing production backdrop and character art.

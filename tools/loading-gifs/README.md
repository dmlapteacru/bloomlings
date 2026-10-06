# tools/loading-gifs

Renders the loading-screen concepts as GIFs for review: four variants, each with a boot loader (app start → Home) and
a level-to-level transition (Win "Next" → the next level). A dev tool for picking a direction; nothing here ships.

It draws from the game's own assets in the reference look (spec 005): backgrounds, the logo, the animated hero frames
(`tools/heroanim`), the field/variant icons, the Nunito font and the design tokens. `GD` points at the repository root
(or any checkout that has them). The owner chose `2-lotus` (spec 005 FR-039, contracts/look.md §6.13), which the game
now plays; the GIFs stay as the record of the choice.

```sh
cd tools/loading-gifs
GD=../.. python3 variants.py out                  # 8 GIFs: out/<variant>-boot.gif, out/<variant>-level.gif
GD=../.. python3 variants.py out 2-lotus          # one variant
GD=../.. python3 compare.py                       # out/compare-boot.gif, out/compare-level.gif (4 side by side)
GD=../.. python3 keys.py 1-sign level 0.3,1.0,2.2 # a keyframe sheet for quick checks
```

Needs Python 3 with Pillow and NumPy, and ffmpeg. Frames are drawn at 720 × 1560 and saved at 360 × 780, 25 fps.

| Variant | Boot | Between levels |
|---|---|---|
| `1-sign` | the splash (garden, fountain, heroes, logo) with a wooden progress bar, then Home | a "Level N" wooden sign drops on ropes over the blurred garden, three Bloomlings hop |
| `2-lotus` | the logo and the lotus on parchment, a ring of petals fills; an iris opens on Home | an iris closes on the lotus, "Level N", the iris opens on the level |
| `3-tiles` | the logo and a small board whose 8 candy tiles pop in one by one; a tile cascade to Home | cream tiles cascade over the screen, a "Level N" card, the tiles leave |
| `4-heroes` | the four heroes hop in turn over a progress bar; a petal gust to Home | a petal gust, Bloom hops on a "Level N" sign, a second gust reveals the level |

The gameplay, Win and Home screens in the GIFs are approximations drawn from the assets, not the client's own render.

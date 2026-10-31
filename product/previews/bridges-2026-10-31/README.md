# Bridges, water and link tubes (2026-10-31)

The owner, after playing the bridge levels on the phone: the links' tube twice as thick; the water round the pictures
must stand out clearly (it blended in, especially by blue cubes); the bridges bigger (hard to see, unclear that one must
be chosen); and the sites shown by class: «На нормалах можем показать над кубиками, на хардах 50-50 в волнах… на супер
хардах, чтоб юзер типа первый раз искал их… Можем показывать один над кубиками, который будет проигрышным».

- Link tube: `LinkTube.CubeThicknessShare` 0.15 → 0.3, `ThicknessShare` 0.2 → 0.4.
- Water: deep water in one tone (`water.deep` #1F6688, was #3B98BC) with light ripples, and a cream stone bank with a dark
  line on every side that meets a cube or the surface (`RoundBoardLayout.Banks`, `BankShare` 0.18 of a cell).
- Bridge: a wooden footbridge with rails (`UiRaster.FootBridge`), 0.85 of a cell onto each bank (was 0.22) and 1.5 cells
  wide (was 0.78), over the cubes. Shown and not yet buildable: faint (0.72); buildable: whole, its glow outline pulsing
  (the owner's follow-up of the same day: every 1.1 s it swells from 0.1 to 0.24 of a cell round the bridge and from
  alpha 0.45 to 1; `bridge-pulse.gif`); built: whole.
- Visibility (17 §8 as amended on 2026-10-31; `rounds.mjs bridges`, level data `shown: false` for a hidden site):
  Normal both shown (L16, L18, L23, L33, L43); Hard alternating in the wave (L35 both shown, L50 both hidden); Super Hard
  hidden, the proven losing bridge shown as a lure (L30, L45: bridge 0 shown, the deciding bridge 1 hidden).
- Content v4: L30, L45, L50 at definitionVersion 2; `rounds validate` 50 levels, 0 errors.

Previews: `L30-before-after.png` (the board), `L35-L50-L30-start.png` (the three kinds at the start),
`bridge-states.png` (shown, buildable, shown upward, built), `bridge-pulse.gif` (a buildable bridge's outline pulsing).

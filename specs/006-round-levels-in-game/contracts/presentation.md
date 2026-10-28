# Contract: Round Level Presentation (both builds)

What the full playtest and the Unity client show for a round level (spec 006 FR-040–FR-048). It is engine-free and
lives in the kit `client/Assets/Bloomlings/UI/Design/`, linked into the playtest. Presentation never changes rules: it
reads the timeline's state, events and courier positions.

| Element | Contract |
|---|---|
| Board box (owner, 2026-10-28) | One fixed board box for every level of a screen (`ReferenceGameplayRegions.RoundBoard`): roughly square (at most `RoundBoardMaxAspect` 1.12 times as wide as tall on a short screen, else a square), at most 0.86 W wide, centred, its bottom on the tray's top; it is taken from the fullest layout (a badge, the booster row, the tallest pods), so neither the picture, the columns, the badge nor the boosters move or resize it. `RoundBoardLayout.Outer` is that box. |
| Frame and gate | The rows' outer wall is never drawn per cell: it is the game's stone frame on the fixed box's edge, always the same thickness (`BoardLayout.Rim` × `Unit`, `Unit` = the box's shorter side / `FrameUnits` 22). The gate (`entries`) is the existing entry arch set in that frame in line with its inside cell, sized by `Unit` (`BoardLayout.ArchAt`); the walkers set off from the middle of the frame there. |
| Surface (owner, 2026-10-28) | Inside the frame one solid neutral surface (`board.surface`, `UiRaster.BoardSurface`: `board.surface` with a soft `board.surface_shade` along its top and left edges), like Colony Flow's board: no grid of cells, no grass, no cell tiles under the picture. The walkway ring and the picture's background (`.`) are the surface itself. |
| Cube size (owner, 2026-10-28) | The cells inside the wall (walkway ring and picture, (w − 2) × (h − 2)) take the largest cube that fits the surface, keeping the picture's proportions, centred (`RoundBoardLayout.Cell`, `Grid`): a big matrix gets smaller cubes, a small one bigger cubes, the picture stays as large as the box allows. Water, stone walls, planks, glows, Bloom Burst's rings and the walkers all derive from that cube size. |
| Strips | A stream (`#` inside the wall) is water; a wall the level's overlay laid over the picture (`board.overlay`) is a stone wall (`board.wall`), and so is the ring cell it closes. A strip cell on the walkway ring is carried on over the surface to the frame in cell-sized pieces (`RoundBoardLayout.StripPieces`), so a strip never stops short of the frame. A built bridge is a plank over its site turned to its crossing (up and down, or left and right); a buildable site glows toward its near bank. |
| Cubes | The game's existing soft cube drawing (`UiRaster.Cube`), filled with the material's normal colour. No icon on the cube, unless the symbols mode is on (then the material's symbol, centred). |
| Clearing | Each cube plays the existing clear ("eating") animation at its `clear` event time; once eaten, the neutral surface shows and nothing else (no imprint, no pale cleared cell; owner, 2026-10-28). The clearing style's walkers follow their courier route from `depart` to `arrive`, act until `clear`, and return until `done`. |
| Walkers | The family hero of the pod's material, tinted in the material's colour, for every clearing style and in the Store previews. |
| Finished picture | The win's and the Collection's picture keeps its thin stone frame, its empty cells showing the same neutral surface (no grass). |
| Pods and places | The existing wooden pod frames and cream places. The count is shown; a hidden pod shows "?" until revealed. A just-revealed pod glows for about 1.6 s. |
| Links | A tube attached to the front faces of the two linked pods, in the columns and in the places. Its half nearer each pod is that pod's colour. It is not drawn above or on top of the pods. A tap refused for a missing partner shows the reason and pulses the partner. |
| Speed | A normal speed-up control (×1 / ×2). Auto starts when every column is empty and no bridge is left to choose, then shows ×2 and hides the whole HUD (top bar, buttons, columns area chrome, booster bar); the board stays. |
| Introductions | The first meeting with hidden pods, links or the bridge shows its explanation card. The second meeting shows a short reminder card. |
| Symbols mode | A Settings switch, off by default, saved in the player's settings. When on, every material's symbol shows on cubes, pods and places. |
| End | After the last transferred level: a "new levels soon" card. |

Engine-free render checks (preview frames) cover:
- no icon on cubes with symbols off, and symbols on cubes with symbols on;
- a link tube on a linked pair in the columns and in the places;
- HUD absent under Auto;
- the introduction cards on L6/L8, L11/L13, L16/L18;
- a 48×48 board fitting the phone frame;
- the owner's board of 2026-10-28: a 12×12, a 32×32 and a 48×48 board mid-clear in the same box, on the neutral surface
  (`board.surface`, no `tile.grass`, no `tile.ground`), the cube size falling as the matrix grows
  (`rounds-play-board-small-big.png`).

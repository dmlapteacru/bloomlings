# Contract: Round Level Presentation (both builds)

What the full playtest and the Unity client show for a round level (spec 006 FR-040–FR-048). It is engine-free and
lives in the kit `client/Assets/Bloomlings/UI/Design/`, linked into the playtest. Presentation never changes rules: it
reads the timeline's state, events and courier positions.

| Element | Contract |
|---|---|
| Frame and gate | The rows' outer ring and wall are drawn as the game's existing stone frame. The gate (`entries`) is the existing entry arch. A stream (`#` inside the frame) is water; a built bridge is a plank over it. |
| Cubes | The game's existing soft cube drawing (`UiRaster.Cube`), filled with the material's normal colour. No icon on the cube, unless the symbols mode is on (then the material's symbol, centred). |
| Clearing | Each cube plays the existing clear ("eating") animation at its `clear` event time. The clearing style's walkers follow their courier route from `depart` to `arrive`, act until `clear`, and return until `done`. |
| Walkers | The family hero of the pod's material, tinted in the material's colour, for every clearing style and in the Store previews. |
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
- a 48×48 board fitting the phone frame.

# Clearing animations: readable timing (2026-10-31)

The owner's note: in round levels the heroes are small and the clearing animation is too fast to be seen, yet the styles
are meant to be sold. The cause: the round rules give a walker's act at its cube 0.1 s (the walk out takes at least
0.65 s, home at least 0.4 s); the styles were drawn for the earlier timing, where Munchers acted 0.73 s.

Proposal (presentation only, the rules and every outcome stay):

- the act is shown until the rules' clear plus `ClearStyles.LiveLinger` = 0.35 s (0.45 s at the cube instead of 0.1 s);
- the walk out and home keep the rules' pace; the walk home simply starts and ends 0.35 s later, so a trip looks
  0.35 s longer and nothing flies;
- at its cube the walker pops up to `ClearStyles.ActPop` = 1.5× its size and shrinks back as it leaves;
- Munchers chews more slowly on that longer act, so its three bites and crumbs are seen.

Videos (top: now, bottom: proposal), rendered by `playtest/preview -- --clear-video <level> <seconds> <dir>`:

- `L1-compare-x1.mp4`: L1 (cubes 47 px) at ×1;
- `L1-compare-half-speed.mp4`: the same at half speed;
- `L22-compare-x1.mp4`: L22 (36 × 34, cubes 26 px) at ×1;
- `L1-act-frames.png`: the proposal on L1, every second frame (15 fps).

## Second pass, the same day: «Сделай подольше, герои всё ещё мелкие»

- `LiveLinger` 0.35 → 0.6 s: the act at the cube is shown 0.7 s (the rules' 0.1 s plus the linger), about the 0.73 s
  Munchers was drawn for; the walk home keeps its pace, 0.6 s later.
- The walker is `LiveWalkScale` = 1.25× larger on its way and `ActPop` = 2× at its cube (was 1× and 1.5×).
- `L1-compare2-x1.mp4`, `L22-compare2-x1.mp4` (top: now, bottom: second proposal); `L1-act-frames-2.png`.

## Decision, the same day: «V4 без роста»; walkers stay the 2D characters, the acorn instead of the log

- Size: the walker is `LiveWalkScale` = 1.575 × `WalkerSize` = 1.17 cubes on its way and at its cube alike (no growth;
  between the reviews' 0.93 and 1.4). Colony Flow's ants measured on three of its videos: about 2–2.5 cubes long and
  1 cube wide (the cube 5–6.5 px at 640 × 360, an ant 11–14 × 6 px). The act at the cube stays 0.7 s.
- The walkers stay the materials' 2D characters, tinted in the cube's colour; Twig's is the acorn, no longer the log
  (`RoundLook.WalkerVariant`): `walkers-tinted-acorn.png` (each family in its four materials, the game's 55 % tint).
- `L1-final-v4flat-acorn.mp4`: L1 at ×1 as it is now.
- The other option the owner asked for: flat 2D walkers drawn after the meta screens' heroes, to be tinted in the cube's
  colour: `mock-heroes-concept.png` (sketches; source `mock-heroes-concept.html`, SVG). Not in the game until approved.

## The same day: «желудя надо чуть шире сделать … бревно можно совсем убрать»

- The acorn (tools/artgen `Characters2D`): the nut 62 wide instead of 44, the cap 76 instead of 60, rounder:
  `acorn-wider.png` (before / after, in Twig's four materials).
- The log is gone: Wood's character is the acorn's (`CharacterArt.CharacterIcon`), its pictures removed from the set (53
  pictures), the Store's Pushers preview shows the acorn. The log's symbol on legacy tiles stays.

## The owner's batch, the same day (pods and the gate)

- Colony Flow's ants scale with the cubes too: measured on four levels, an ant's body is about 1.4 cubes long whatever
  the board (5.6 px at 3.5 px cubes, 9 px at 6.6 px cubes), so ours keep their size in cubes.
- A pod waiting in its place keeps its colour, paler with its number half clear, as the columns' waiting pods (no neutral
  grey): `pods-waiting-places-L22.png`.
- Pods and places have no side face (the board's cubes keep theirs).
- A hidden pod is the reference game's neutral cube: dark grey with a big white "?" and four small ones, the same in every
  row: `pods-hidden-L6.png`.
- The gate: a walker sets off from a standstill and slows down into the gate, growing and fading out of the arch and
  back into it (0.25 s), and walks home at its way out's pace (the rules' way back is quicker): `L22-gate-x1.mp4`,
  `gate-frames-L22.png`. Presentation only; the last walker is home a little later than the rules' end.

## The same day: «Размер героев при уменьшении кубиков делаем больше: 1.6, а на больших 1.4»

- `ClearStyles.WalkerCubes`: 1.4 cubes on the biggest cubes (19 or fewer across the board box, L1) to 1.6 on the smallest
  (46 or more, the 48-wide boards), linearly in between; the hosts pass the board box width over the cube
  (`ClearWalk.Live(…, cubesAcross)`): `walker-size-by-cubes.png` (L1, L22, L27).

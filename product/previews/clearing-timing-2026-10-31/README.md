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

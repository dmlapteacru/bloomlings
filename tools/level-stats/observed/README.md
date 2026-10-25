# Observed buffer and board progress (Colony Flow recordings)

What the videos themselves show, frame by frame: how many waiting slots hold a capsule, and how much of the board's
cube area is still there. One file per level, made by `tools/level-stats/videoslots.py` (the full method is in its
docstring). The data is meant to be compared with a model replay. Nothing in it is taken from the round files except
the attempt's start and end and, for the check, the tap times.

```
python3 -I tools/level-stats/videoslots.py --yt <dir of videoplayback_*.mp4> --old <dir of level-*.mp4> \
  --xrec <dir of XRecorder_*.mp4> [--shots <dir>] [L19 L1 ...]
```

The videos are not in the repository. The colony-flow.com captures and the owner's recordings of 2026-10-20 were
downloaded by earlier work; the XRecorder files come from
`https://pub-dc09455922e746b0979cdaa7317edd10.r2.dev/<file>`. A full run takes about 25 minutes.

## Format

`L<n>.json`: `{level, source: "site"|"owner", videos, fps: 4, layout: {attemptK: {...}}, attempts: [...]}`

- `attempts[].slots`: `[t, filled, "01101"]` for every frame (4 a second) from `start` to `end`. `t` uses the round
  file's time base: seconds from the start of the attempt's first file, and for an attempt that spans several files
  the later files are offset by the durations in `INSTRUCTIONS.md`. Videos at 2x game speed keep video seconds. When
  a full-screen card, the tutorial's dimming or the win/loss screen covers the slot row, the entry is `[t, null, null]`.
  The string gives the slots from left to right. The row closes up when a linked group arrives, so the position of a
  slot does not identify a capsule. Treat `filled` as the measure.
- `attempts[].board`: `[t, share]` every 0.5 s. `share` is the pixel share of the board content still present, 1.0 at
  the first measured frame. It is `null` under a card, and the share there is held.
- `attempts[].validation`: `taps` (taps in the round file seen on video), `tapsWithRise`, `tapsMatchedBySwap`,
  `tapsWithoutRise` `[[t, group, seen]]`, `riseWithoutTap` (times), `notes`.
- `layout.attemptK`: `frame` / `crop` (the frame the coordinates refer to), `plates` `[cx, cy, w, h]` (the last
  layout), `layoutChanges` `[[t, plates]]`, `plateBox`, `panel` `[y0, y1, x0, x1]`, `panelColour`, `boardReference`
  (which frames the clean plate came from), `boardPixelsAtStart`.

## Method

### Layouts

| source | frame | plates (5 slots) | panel |
|---|---|---|---|
| site, 28 levels | phone strip x 222-418 of 640x360 (196x360) | found per video: plate centre y 238-255, about 24x21 px | per video, e.g. L19 [16,166,12,183] |
| owner 2026-10-20 (L1-L5, L10) | scaled to 368x826 | x 71.5 128 184 240.5 296.5, y 533, 45x37 px (L3 after the booster: 6 plates, x 43.5-324.5) | [134,406,28,342] for every level |
| owner XRecorder (L20, L25, L26) | 368x816 as recorded | x 72.5 128.5 184 240.5 296.5, y 526 | [132,400,30,340] |

The site videos do not share one layout. The panel size and the height of the slot row change from level to level,
so the tool looks for both in each video. It finds the plates where 5 or 6 equal near-white squares stand in a row
while every slot is empty, and it searches again on every frame. That is how it picks up the sixth slot that the
Extra Slot booster adds in owner L3 at 81 s.

### Slots

- **Filled or empty.** A plate is filled when more than 30% of its box (80% of the plate's width x 90% of its height)
  differs from the empty plate's colour by more than 14 in the strongest channel. An empty plate reads 0-5%. A white
  capsule reads 60-90%: its face is nearly as white as an empty plate, but its digits, grey side and raised face count.
  A coloured capsule reads 95-100%.
- **One-frame blips.** A slot that is filled for a single frame between two empty ones is set to empty. These are
  capsules flying over the plate and ants crossing it.
- **Finishing capsules.** A capsule's grey finishing flash, and the capsule popping up off its plate, count as filled
  until the plate is white again. That adds 1-2 frames.
- **Covered frames.** A frame where the background beside the plates changes by more than 25 on average gives null.
  This covers cards, the tutorial and the win or loss screen.
- **No other smoothing.** A median filter was tried. It erased real gaps of one frame, where a slot was freed and
  refilled within 0.5 s, so it is not used.

### Board

- **Clean plate.** Each won attempt yields a clean plate: the per-pixel median of the 3 frames from its last 8 s that
  have the most panel-coloured pixels. This picks frames that avoid the site's "Colony Flow" logo overlay and the win
  screen. The panel box comes from the clean plate.
- **Attempts that were not won.** A lost attempt uses the clean plate of the won attempt of the same level when its
  plates stand at the same place (L10 a1 uses a2; L25 a1 uses a2). L5 is unfinished and has no won attempt, so it
  uses the clean plate of L4: every owner level of 2026-10-20 has the same panel and plates.
- **Content and clearing.** A pixel is content when, in the first two measured frames, it differs by more than 22 from
  both the clean plate and the panel colour. It counts as cleared for good once it matches either one for 3 frames in
  a row. Because clearing is permanent, ants walking on cleared parts and cubes being carried over them never count
  again.
- **Share.** `share` = content left / content at the start. Boxes, jars, chocolate blocks and padlocks count as
  content. It is a share of pixels, not of cells. The cubes are drawn at equal size, so it tracks the cell share, apart
  from the 3D side faces of the cubes on the bottom edge of each run.
- **Timing.** The share lags what happens on the board by 0.5-0.75 s, because of the 3-frame confirmation. It also
  lags the capsule counters: at every win the board reaches 0 between 2.0 and 3.0 s (median 2.75 s, 33 wins) after
  the last slot empties. A capsule's counter drops as its ants set off, and the cube goes when they arrive.

### Validation

- **Rises.** A rise is a slot turning filled whose capsule colour was not in the row a frame earlier, because the row
  closing up is not an arrival. The number of rises in a frame is at least the rise of the filled count.
- **Matching taps.** A tap is matched when, within -0.75..+1.25 s of its time in the round file, as many capsules
  arrive as its linked group has. Group sizes come from replaying the tray of the round file, or from `take` in
  `levels.json`.
- **Swaps.** Taps still short can then take swaps. A swap is a slot that stays filled while its steady capsule colour
  changes by more than 45 within 0.75 s: one capsule finished and the next landed in the same slot between two frames.
  This happens often when the slots are full, and 71 taps are matched this way.
- **Left out.** Owner L5 lists 4 taps made before the recording started. They are not checked.

## Results (taps matched / taps; swaps in brackets)

All attempts: **1071 of 1106 taps matched** (71 of them by a colour swap), 12 rises without a tap.

| level | src | attempt | start-end | frames / null | cap | taps | taps without rise | rise without tap | board start -> end |
|---|---|---|---|---|---|---|---|---|---|
| L1 | owner | 1 won | 0.0-31.5 | 127 / 19 | 5 | 8/9 (1) | 2.25 | - | 1.00 -> 0.000 |
| L2 | owner | 1 won | 35.0-72.5 | 151 / 2 | 5 | 9/9 (0) | - | - | 1.00 -> 0.000 |
| L3 | owner | 1 won | 75.25-130.75 | 223 / 23 | 5, then 6 | 12/12 (0) | - | - | 1.00 -> 0.000 |
| L4 | owner | 1 won | 137.0-230.19 | 374 / 2 | 5 | 23/24 (1) | 151.19 | - | 1.00 -> 0.000 |
| L5 | owner | 1 unfinished | 0.0-103.72 | 415 / 0 | 5 | 10/11 (0) | 20.0 | - | 1.00 -> 0.441 |
| L10 | owner | 1 lost | 0.5-118.55 | 473 / 17 | 5 | 16/16 (0) | - | - | 1.00 -> 0.586 |
| L10 | owner | 2 won | 0.0-228.8 | 914 / 0 | 5 | 29/29 (1) | - | - | 1.00 -> 0.004 |
| L20 | owner | 1 won | 0-397.75 | 1592 / 1 | 5 | 36/36 (2) | - | - | 1.00 -> 0.000 |
| L25 | owner | 1 lost | 0-76 | 305 / 0 | 5 | 9/9 (0) | - | - | 1.00 -> 0.718 |
| L25 | owner | 2 won | 83-309.5 | 907 / 0 | 5 | 26/26 (1) | - | 137.75 | 1.00 -> 0.004 |
| L26 | owner | 1 won | 0-315.5 | 1263 / 0 | 5 | 32/32 (1) | - | - | 1.00 -> 0.004 |
| L19 | site | 1 won | 1.5-66.0 | 259 / 1 | 5 | 18/20 (0) | 35.25 (pair, 1 seen), 53.75 | 48.5 | 1.00 -> 0.000 |
| L27 | site | 1 won | 2.5-136.25 | 536 / 3 | 5 | 16/16 (0) | - | - | 1.00 -> 0.000 |
| L34 | site | 1 won | 0.0-215.5 | 863 / 2 | 5 | 30/30 (3) | - | - | 1.00 -> 0.000 |
| L37 | site | 1 won | 1.75-100.5 | 396 / 2 | 5 | 24/24 (0) | - | - | 1.00 -> 0.000 |
| L40 | site | 1 won | 2.0-152.0 | 601 / 3 | 5 | 20/21 (2) | 69.0 | - | 1.00 -> 0.000 |
| L50 | site | 1 won | 3.5-311 | 1231 / 4 | 5 | 29/29 (1) | - | 112.0 | 1.00 -> 0.000 |
| L55 | site | 1 won | 6-216.75 | 844 / 2 | 5 | 35/37 (2) | 161, 169.5 | - | 1.00 -> 0.000 |
| L67 | site | 1 won | 4.5-134.25 | 520 / 2 | 5 | 33/33 (2) | - | - | 1.00 -> 0.000 |
| L100 | site | 1 won | 2.5-171.75 | 678 / 2 | 5 | 18/18 (1) | - | - | 1.00 -> 0.000 |
| L109 | site | 1 won | 1.0-109.0 | 433 / 2 | 5 | 24/26 (6) | 52.25, 95.5 | - | 1.00 -> 0.000 |
| L113 | site | 1 won | 4.75-152.5 | 592 / 2 | 5 | 18/19 (1) | 38.75 | - | 1.00 -> 0.000 |
| L115 | site | 1 won | 4.25-210.25 | 825 / 2 | 5 | 18/18 (2) | - | - | 1.00 -> 0.000 |
| L145 | site | 1 won | 1.5-283.75 | 1130 / 4 | 5 | 25/25 (1) | - | 21.75, 143.5, 162.5 | 1.00 -> 0.000 |
| L165 | site | 1 won | 3-301.5 | 1195 / 3 | 5 | 28/29 (1) | 290.75 | - | 1.00 -> 0.000 |
| L185 | site | 1 won | 2.25-528.25 | 2105 / 2 | 5 | 45/46 (1) | 121.25 | - | 1.00 -> 0.000 |
| L192 | site | 1 won | 0.25-185.75 | 743 / 3 | 5 | 28/30 (0) | 33.75, 49.25 | - | 1.00 -> 0.000 |
| L194 | site | 1 won | 3.25-196 | 772 / 2 | 5 | 27/29 (2) | 11.25, 61 | - | 1.00 -> 0.000 |
| L214 | site | 1 won | 4.0-112.75 | 436 / 2 | 5 | 39/42 (3) | 11.75, 80.75, 102.75 | - | 1.00 -> 0.000 |
| L245 | site | 1 won | 1.5-146.75 | 582 / 2 | 5 | 50/51 (0) | 66.5 | 132.5 | 1.00 -> 0.000 |
| L266 | site | 1 won | 1.0-164.75 | 656 / 2 | 5 | 40/43 (5) | 13.25, 92.75, 94.5 | - | 1.00 -> 0.000 |
| L290 | site | 1 won | 2.0-490.0 | 1953 / 2 | 5 | 23/23 (0) | - | - | 1.00 -> 0.000 |
| L350 | site | 1 won | 2-284.5 | 1131 / 2 | 5 | 39/40 (3) | 261.75 | 250.5 | 1.00 -> 0.000 |
| L358 | site | 1 won | 3.0-116.75 | 456 / 2 | 5 | 35/38 (0) | 39.0, 48.5, 70.5 | - | 1.00 -> 0.000 |
| L533 | site | 1 won | 3.75-183.5 | 720 / 2 | 5 | 31/33 (8) | 51.75, 70.5 | - | 1.00 -> 0.000 |
| L757 | site | 1 won | 5.5-362.75 | 1430 / 7 | 5 | 42/43 (1) | 55.75 | 300.0 | 1.00 -> 0.000 |
| L790 | site | 1 won | 3.5-642.5 | 2557 / 5 | 5 | 57/58 (6) | 202.25 | 407.75 | 1.00 -> 0.000 |
| L1170 | site | 1 won | 2.0-160.25 | 634 / 2 | 5 | 26/27 (6) | 17.0 | - | 1.00 -> 0.000 |
| L1270 | site | 1 won | 5.25-396.0 | 1564 / 12 | 5 | 43/45 (7) | 233.0 (pair, 1 seen), 296.75 | 273.5, 342.0 | 1.00 -> 0.000 |

"null" counts the frames without a slot reading: the site win screen at the round's `end` (1-4 frames, more where
the video's logo fades in), the L1 tutorial dimming (0-2.0 s, and 12.25-14.0 s for the "Use Speed Up" hint), the L3 booster card and spotlight
(75.25-81 s), and the L10 a1 loss card.

## Spot checks (frames cut with ffmpeg and compared by eye)

- **Slots, L245** (site, 12 random frames): 11 agree. One frame reads a finishing capsule's grey plate as filled.
- **Slots, L20** (XRecorder, 12 frames): all 12 agree, including two mid-flight frames read as their landing state.
- **Slots, L3** (owner, 10 frames on the 6-slot layout): all 10 agree.
- **Slots, L19** (34-36 s, 47-56 s, every frame): the closing-up of the row, the pairs and the white capsules all read
  correctly.
- **Board masks** at mid-game on L19, L245, L1270, L290, L790, L185, L55, L20, L25 and L26: the "remaining" mask
  covers exactly the cubes and boxes left. Cleared parts, ants on cleared parts and the site watermark are not counted.

## Known failure cases

- **A slot freed and refilled between two frames (0.25 s).** No empty frame is seen. The colour-swap check recovers
  most of these, but not when the new capsule has the old one's colour. This explains most of the 35 taps without a
  rise, when they happen with all slots full (L109, L358, L533, L214). The slots data itself is right in these cases:
  the slot really is filled in every frame.
- **Arrivals hidden by a simultaneous change.** A capsule can land while another finishes, or a white capsule can fly
  in over a finishing one (L19 34.0 s). The arrival can then go unseen, which is why L19 35.25 and L1270 233.0 each
  show one of the pair's two capsules. When the row closes up for a pair while a capsule is sliding, one extra rise
  can be counted (L19 48.5).
- **Round time errors.** The round files' tap times are transcriptions: ±0.5 s, and some are later or earlier than
  the video by more than 1 s (L19 52.25 is placed while all 5 slots are still full). A "tap without rise" next to a
  "rise without tap" a second or two away is usually this.
- **Taps the video does not support.** L5 has taps at 19.7 and 20.0, but only one capsule arrives (19.75). L1 2.25
  comes right after the tutorial's dimmed frames (null until 2.0 s), so its rise is not seen.
- **Board share is pixels, not cells.** The cubes' 3D side faces and shadows at the lower edge of each run of cubes
  count with them. A cube colour within 22 of the cream panel would be invisible to the measure; none was seen.
- **Board residue at a win.** On the XRecorder and L10 a2 wins the share ends at 0.004 instead of 0: pixels that ants
  kept covering since their cubes went.
- **Board for L5.** The attempt starts mid-play, so 1.0 means the board as it is when the recording starts. Its
  reference is L4's clean plate, borrowed on the evidence that every owner level of 2026-10-20 shares the panel and
  plates.
- **Covered frames.** Under the L3 booster card and the L10 loss card the board is not updated (null).

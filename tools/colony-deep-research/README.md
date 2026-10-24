# Colony Flow deep research (2026-10-20)

Scripts behind [the deep study](../../product/audits/colony-flow-deep-study-2026-10-20.md). They were run from a
scratch folder, so the paths inside point there; the raw downloads (pages, board previews, frames, reviews, video
titles, about 540 MB) were not kept in the repository. Outputs are in `product/audits/colony-flow-2026-10-20/`.

- `levels/` — crawl of colony-flow.com level pages (`fetch_pages.sh`, `parse_pages.py`), board previews through the
  site's own image endpoint (`fetch_imgs.sh`), grid size / colours / fill from each preview (`board.py`), first-frame UI
  colour (`frame1.py`), tray columns (`tray.py`), numbered objects and "?" tiles (`objects.py`), text features
  (`features.py`), aggregation (`aggregate.py`, `aggregate2.py`) → `levels.csv`, `boards.csv`, `tables.md`.
- `meta/` — Google Play reviews (`gprev.py`, `themes.py`), video-title difficulty labels (`yt.py`, `ytpl.py`,
  `labels.py`) → `hard-labels.csv`, mechanic keywords (`mech.py`).
- `sheet.py` — contact sheets `cf-hard.jpg`, `cf-normal.jpg`:
  `python3 -I sheet.py <cf-levels dir> <out.jpg> <hard-labels.csv> 5,10,15,...`.

Run Python with `-I` on downloaded content. The fan site is not the game: its texts often contradict its frames, so
mechanics and sizes come from the images.

## Owner's recordings (level-by-level deep dive)

Used by [colony-flow-levels-deep-dive-2026-10-20.md](../../product/audits/colony-flow-levels-deep-dive-2026-10-20.md).
Paths inside point at the scratch folder they ran from; the videos stay in the owner's bucket.

- `video-sheets.py`, `traysheet.py` — contact sheets of frames (whole screen every 2 s; slots + tray every 1 s).
- `gridmap.py` (+ `cfboard.py`) — a board frame → letter grid, palette and colour layers; `finalize.py` names the
  colours and patches L5's watermark from the video; `mdsnips.py` writes the per-level tables.
- `sim/` — the pilot-v4 rules with more colour channels (`rules.cjs`, `search.mjs`), Colony Flow trays as transcribed
  (`levels.json`); `run.mjs` (bots, exact random-tap probability, solver), `mistakes.mjs` (first fatal tap),
  `ours.mjs` (our curated L1–10 in the same engine: reproduces the Core audit's 79.8% / 67.4%), `retray.mjs`
  (CF-style trays for pilot pictures), `flower.mjs` / `flowerfatal.mjs` (the proposed L5 sketch).
- `timeline/` — the slot bars of L4, L5 and L10.

## Reconstructed levels and rounds (2026-10-24)

All recorded levels (L1–L5, L10, L20, L25, L26) as text in [`product/audits/colony-flow-rounds/`](../../product/audits/colony-flow-rounds/README.md).

- `cfcells.py` — a board frame → letter grid that keeps every cube (gridmap.py treats the most common colour as background, which drops a sky covering the whole board); grey "?" board tiles show as `?`.
- `slottaps.py`, `traytaps.py` — tap times from a recording: rises in the number of filled waiting slots, and bursts of movement in the tray.
- `rounds/data-l25-l26.mjs` — L25 and L26 as transcribed from frames every 0.5 s; `rounds/align20.mjs` — L20's tap times (detected events matched to the owner's tap order, `rounds/detected/`); `rounds/build.mjs` — writes `levels.json` and the storyboards, checks the colour accounting and replays every round on its tray.

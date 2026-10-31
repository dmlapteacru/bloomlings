# Smoother board pictures (2026-10-31)

The owner, on the first character drafts: «все картинки выглядят странно, пиксельно … хотелось бы рисунки плавнее, чтоб
не такие резкие углы лиц или других частей, предметов».

- The drafts built heads and bodies from rectangles. Now big shapes are curves (circles, ellipses, bezier paths), the
  viewBox is the grid in cells (1 unit = 1 cell), characters use 1100–1400 cells, and small features (eyes, nose,
  mouth) are cell-aligned blocks of at least 2×3 cells (a small circle turns into a «+» on the grid).
- `tools/rounds/svg2cells.mjs` now smooths one-cell spikes and notches off every edge (a picture's `smooth: false`
  keeps the raw grid).

`before-after.png`: four characters, the first boxy drafts above, the smooth redraw below; `sample-cell-grid.svg.mjs`:
the cell-grid draft of two of them.

**The owner's answer** (same day): «Со смайликом стало лучше. Но остальные стали хуже сильно … Видимо когда ты мне даешь
картинки, там еще нет сетки … включай сетку». The smooth redraw is rejected except the round Колобок; drawings stay as
the first drafts, the converter smooths only a picture with `smooth: true`, and contact sheets now draw the cell grid.

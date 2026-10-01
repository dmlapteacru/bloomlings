# Contract: Painter labels with volume (delta to spec 002 `contracts/painter.md`)

The playtest painter keeps every operation of spec 002. Three things change.

## 1. Text gains an optional look

| Operation | Change |
|---|---|
| `Text(text, cx, cy, style, color, maxWidth = 0, sizeScale = 1, look = null)` | new optional `TextLook? look` |
| `TextLeft(text, x, cy, style, color, maxWidth = 0, sizeScale = 1, look = null)` | same |

- **`look == null`.** The text is drawn as today: one fill in `color`, plus the style's translucent outline when
  `style.Outline > 0`. Every existing call keeps compiling and drawing the same way.
- **`look != null`.** `color` is ignored. The text is drawn back to front, at the fitted size (the shrink to
  `maxWidth` happens first):
  1. a soft shadow: the text in black at `look.ShadowAlpha`, offset down by `(ExtrudeEm + 0.05) × size`, blurred 0.06
     em, where the host can blur (Skia: a mask filter; Android: `BlurMaskFilter`);
  2. the extrusion: the text in `look.Outline`, stroked `OutlineEm × size × 2` wide with round joins, drawn at
     y + k·step for k = 1…n, where `step = max(1 px, ExtrudeEm × size / 4)` and `n = 4`;
  3. the outline: the same stroke at y;
  4. the fill: the text with a vertical linear gradient from `FillTop` (at the cap top) to `FillBottom` (at the
     baseline).
- **Measuring.** The text box used for hit checks and recording is the fill's box, so layouts do not move.

## 2. The typeface

- **Bold and regular.** `style.Bold` selects Nunito ExtraBold; otherwise the host uses Nunito SemiBold.
- **Hosts.** `AndroidPainter` loads both from the embedded resources once per process, by writing them to the app's
  cache directory and calling `Typeface.CreateFromFile`. `SkiaPainter` loads them with `SKTypeface.FromStream`.
- **Fallback.** If a font cannot be loaded, the host uses its previous system typeface and logs once. Text never
  disappears.

## 3. Press and time

| Member | Meaning |
|---|---|
| `PushSquash(sx, sy, cx, cy)` | a non-uniform scale about a point until `PopTransform` (the press squash); hit boxes follow it |
| `Released(box)` | seconds since a finger lifted inside the box, or −1 (the spring-back of `GardenLook.PressDepth`) |
| `Now` | the host's clock in seconds (breath, glow); the APK sets it before each frame and each touch |

A plain look (`TextLook.Plain`) draws its light emboss 0.05 em under the text, then the fill.

## Recording

The preview's text record is unchanged: the fill box and the shown string. Checks (safe area, overlaps) work as before.

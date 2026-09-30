# Contract: Playtest Painter

The full playtest draws its designed screens (`playtest/design/`) against `IPainter` (research R3), so the same screen
code runs:
- in the APK, through `AndroidPainter` over `Android.Graphics.Canvas`;
- in the preview tool, through `SkiaPainter` over SkiaSharp.

Screens never touch Android or Skia types.

## Interface

Coordinates are in pixels of the target surface. Colors are `Rgba` from `DesignTokens`.

| Operation | Meaning |
|---|---|
| `Width`, `Height` | surface size in pixels |
| `Scale` | pixels per reference unit (`Width / 1080`, capped) |
| `FillRect(rect, color)` | a plain rectangle |
| `FillRound(rect, radius, color)` | a rounded rectangle |
| `FillRoundGradient(rect, radius, top, bottom)` | a rounded rectangle with a vertical gradient |
| `StrokeRound(rect, radius, width, color, dash?)` | an outline; `dash` = (on, off) lengths for the danger frame |
| `FillCircle(cx, cy, r, color)` | a disc |
| `StrokeCircle(cx, cy, r, width, color)` | a ring |
| `Line(x0, y0, x1, y1, width, color)` | a line with round caps (links, tick marks) |
| `Shape(id, rect, color)` | the `ShapeLibrary` mask of `id` fitted into `rect`, tinted |
| `ShapeOf(key, sdf, rect, color)` | a composite mask (a skin on a family body), cached by `key` |
| `Text(text, cx, cy, style, color, maxWidth?, outline?)` | centered text in a `TypeToken` style; shrinks to `maxWidth` down to the style's minimum |
| `TextLeft(text, x, cy, style, color, maxWidth?)` | left-aligned text |
| `MeasureText(text, style)` | its width at the style's size |
| `PushClip(rect)`, `PopClip()` | clip to a rectangle |
| `PushAlpha(a)`, `PopAlpha()` | multiply alpha for faded elements |
| `PushTransform(dx, dy, scale, cx, cy)`, `PopTransform()` | translate and scale about a point (press and pop motion) |
| `Backdrop(rect, colors, scene, key)` | the garden backdrop of a theme (`BackdropRaster`), cached as an image by `key` |
| `Hit(rect, action)` | a touch target (in the current transform) |
| `Pressed(rect)` | whether a finger is down inside `rect` (pressed looks) |
| `Mark(slotId)` | records that an asset slot is drawn procedurally here (preview only; a no-op on the phone) |

The finished picture (win card, Collection) is drawn by `BoardPainter.Picture` from the level's cells with
`FillRound`; it needs no painter operation. `PainterBase` holds the bookkeeping both painters share: targets, the
finger, the alpha and transform stacks, and dispatch to the topmost target.

## Input

- Screens register touch targets with `Hit(rect, action)` while drawing.
- The host dispatches a tap to the topmost target under the finger, as the current playtest does.
- Every target is at least `size.touch_min` reference units on its shorter side. The preview tool checks this (FR-027).

## Recording (preview tool only)

`SkiaPainter` records:
- every `Shape` id and every text draw;
- every hit target, with its rectangle.

The preview tool uses the record to:
- check that every shape id is a registered asset slot (asset-slots rule 2);
- check that no two hit targets overlap;
- check that no text or target leaves the safe area (SC-007);
- list the slots used by frames 1–17 (asset-slots rule 3).

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
| `Picture(key, rect, render)` | an engine-free RGBA picture (spec 005 `UiRaster`: wood, stone, candy tiles) rendered at the rect's size rounded up to multiples of 8, cached by `key@WxH`, stretched with filtering; callers `Mark` its slot |
| `Sprite(name, rect)`, `HasSprite(name)`, `SpriteSize(name)` | an embedded picture fitted into `rect` (aspect kept), whether it is embedded, and its pixel size or none: character pictures (`2d/leaf-happy`), and the owner's pictures of spec 005 `pictures.md` (`bg/home`, `brand/logo`; `Visuals.Background` cover-fits a background, else draws the stand-in) |
| `Hit(rect, action)` | a touch target (in the current transform) |
| `Scroll(rect, previous, next)` | a page that scrolls (spec 005 FR-041): a drag that starts in `rect` never taps, and a swipe turns the page with `previous` or `next`; a later full-screen target (a card's scrim) covers it |
| `Pressed(rect)` | whether a finger is down inside `rect` (pressed looks) |
| `Mark(slotId)` | records that an asset slot is drawn procedurally here (preview only; a no-op on the phone) |

The finished picture (win card, Collection) is drawn by `BoardPainter.Picture` from the level's cells with
`FillRound`; it needs no painter operation. `PainterBase` holds the bookkeeping both painters share: targets, the
finger, the alpha and transform stacks, and dispatch to the topmost target.

## Input

- Screens register touch targets with `Hit(rect, action)` while drawing.
- The host dispatches a tap to the topmost target under the finger, as the current playtest does.
- Tap or scroll (spec 005 FR-041): the host passes the finger's down, moves and lift to `PainterBase.TouchDown`,
  `TouchMove` and `TouchUp` (with the screen's `Dpi`). A finger that goes down in a `Scroll` area and moves farther
  than `touch.slop` (10 dp) drags: it taps nothing, nothing looks pressed, and a drag of `touch.swipe` (40 dp) turns the
  page. Anywhere else the lift taps where the finger is, as before (`TouchGesture`).
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

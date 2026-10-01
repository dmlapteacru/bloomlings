# Contract: Fonts

## Files

| File | Weight | Role | Source |
|---|---|---|---|
| `client/Assets/Bloomlings/UI/Fonts/Resources/Nunito-ExtraBold.ttf` | 800 | display: every `TypeStyle` with `Bold = true` | `@expo-google-fonts/nunito` 0.4.2, `800ExtraBold/Nunito_800ExtraBold.ttf` |
| `client/Assets/Bloomlings/UI/Fonts/Resources/Nunito-SemiBold.ttf` | 600 | body: every `TypeStyle` with `Bold = false` | same package, `600SemiBold/Nunito_600SemiBold.ttf` |
| `client/Assets/Bloomlings/UI/Fonts/Resources/OFL.txt` | — | license (SIL OFL 1.1, "Copyright 2014 The Nunito Project Authors") | the same package, `LICENSE_FONT` |

Rules:

1. These are the only new non-code files of spec 003 (SC-006). The asset inventory lists them under typography.
2. Both files cover Basic Latin, Latin-1, Cyrillic, "×", "−" and the no-break space (tested by reading the `cmap`
   table).
3. The files are never modified (OFL: no renaming of a modified font). The license file ships with them.

## Loading

| Host | How | Fallback |
|---|---|---|
| Unity client | `UiFonts.Display` and `UiFonts.Body` call `Resources.Load<Font>("Nunito-ExtraBold")` and `("Nunito-SemiBold")`, then `TMP_FontAsset.CreateFontAsset(font)` once. `UiKit.Style` sets `label.font` by `TypeStyle.Bold`. | `null` → the label keeps the TextMeshPro default font. |
| Full playtest (Android) | The csproj embeds both files (`fonts/Nunito-*.ttf`). `AndroidPainter` copies them to `CacheDir` on first use, then calls `Typeface.CreateFromFile`. | `Typeface.Default` and its bold variant, as before. |
| Preview tool | The csproj embeds the same files; `SkiaPainter` calls `SKTypeface.FromStream`. | DejaVu Sans, as before. |
| Level tester APK | Unchanged: it does not compile `playtest/design/` and keeps the system font. | — |

## Materials (Unity)

`UiFonts.Material(font, look)` returns a shared material per (font asset, `TextLook`). It is a copy of the font
asset's material with:

- the outline: `_OutlineColor` and `_OutlineWidth`;
- the underlay: keyword `UNDERLAY_ON`, `_UnderlayColor` = the look's outline color, `_UnderlayOffsetY` = −(extrusion
  in the font asset's units), `_UnderlaySoftness` = 0.

A label with a look sets `fontSharedMaterial` to it, and sets the vertex gradient (`enableVertexGradient`,
`colorGradient`) from the look's fill. Labels without a look keep the font asset's default material.

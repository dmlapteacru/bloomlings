# Research: Cartoon UI Style — the "Garden" direction

Decisions behind [plan.md](plan.md). Each decision gives what was chosen, why, and what else was considered.

## R1. The font: Nunito, two static weights, one file set for all hosts

- **Decision.**
  - Ship `Nunito-SemiBold.ttf` (600) for body and caption text, and `Nunito-ExtraBold.ttf` (800) for every bold style:
    buttons, titles, pills, counts, the level text.
  - Put them in `client/Assets/Bloomlings/UI/Fonts/Resources/`, with `OFL.txt` next to them.
  - Take the files from the npm package `@expo-google-fonts/nunito` 0.4.2, with the OFL text that ships in the same
    package (`LICENSE_FONT`).
  - The playtest and the preview embed the same two files.
- **Why.**
  - The owner chose Nunito (spec Clarifications, 1A): rounded letters, OFL, and Latin plus Cyrillic.
  - The cmap of the files was checked: A, Ж, я, Ё, ×, − and the no-break space are present.
  - Two static weights (about 130 KB each) are smaller and simpler to load on three hosts than the variable font.
- **Alternatives.**
  - **Fredoka** (used in the mockup) has no Cyrillic.
  - **The variable font** needs variation support in TextMeshPro, Android and Skia.
  - **Black (900)** reads heavier than the reference; ExtraBold matches the mockup's weight.

## R2. Loading the font in Unity: a runtime TextMeshPro font asset

- **Decision.**
  - A new `UiFonts` loads the two `Font` objects with `Resources.Load<Font>`.
  - It creates the dynamic `TMP_FontAsset`s at first use, with `TMP_FontAsset.CreateFontAsset(font)`.
  - `UiKit.Style` assigns them by `TypeStyle.Bold`.
  - If loading fails, labels keep the TextMeshPro default font, so the game never shows missing text.
- **Why.** A TextMeshPro font asset made in the Editor is a generated binary asset, and it cannot be built here.
  Runtime dynamic assets work in player builds as long as the TTF import keeps "Include Font Data", which is the
  default.
- **Alternatives.** An Editor script that bakes the font asset on first open was rejected. It is possible, but it
  depends on an Editor step the cloud session cannot verify. The runtime path works without one.

## R3. Sentence case

- **Decision.**
  - `TypeStyle.Upper` becomes false for Title caps, Level home, Level pill, Button large, Button and Button secondary.
  - Badge (HARD, SUPER HARD) stays uppercase.
  - The strings in `Strings_en.csv` are already in sentence case ("Play", "Resume"), so no string changes.
- **Why.** The owner's answer 2A (spec FR-009). Spec 002 FR-005's uppercase is replaced, and the deviation is recorded
  in the spec's Assumptions.

## R4. The label look (volume) as a token, drawn by each host

- **Decision.**
  - A `TextLook` record holds:
    - the fill (top color, bottom color);
    - the outline (color, width in em);
    - the extrusion (color, depth in em);
    - a soft shadow alpha.
  - The kit derives a look from the element's color set: a light cream gradient fill, the set's outline color, and an
    extrusion of 0.09 em. On cream faces the look is plain: dark brown, with no outline and no extrusion.
  - Each host draws it:

| Host | Fill | Outline | Extrusion |
|---|---|---|---|
| Android | Linear gradient shader | Stroke pass | Text drawn 3–4 times, stepping down in the outline color |
| Skia (preview) | Gradient shader | Stroke | Same passes as Android |
| TextMeshPro | Vertex gradient | Outline | Hard underlay offset down (`UNDERLAY_ON`, softness 0) |

- **Why.**
  - It matches the approved mockup, where the extrusion was 4 stacked text shadows plus a ring.
  - TextMeshPro's single underlay gives the same short extrusion. Its soft shadow is dropped on Unity, where only one
    underlay exists.
- **Performance.** TextMeshPro materials are shared per (font, look), with no `fontMaterial` instance per label (R12).

## R5. The plate and the raised button

- **Decision.** Every button, pill and round button is drawn as layers, back to front:
  1. a soft drop shadow;
  2. the plate's thickness: a darker cream copy shifted down by `garden.plate_depth`;
  3. the plate's outline: brown, the full box;
  4. the plate: cream with a slight gradient, inset by the outline width;
  5. the button's outline: the set's line color, inset by `garden.plate_inset`;
  6. the button's face: a gradient from top to face, inset by the outline width, with its lip drawn as a darker band
     along the bottom edge;
  7. the highlight: a white pill across the upper third, at 50% alpha.

  On press, the face's top moves down to the lip, and the plate stays. In Unity, each layer is a rounded `Image`
  (sliced rounded sprites already exist). In the playtest, each layer is a `FillRound` or `FillRoundGradient`.
- **Why.** These are the layers of the owner's reference button and of the approved mockup. Drawing them with shapes
  keeps the look tunable from tokens and replaceable by art later (FR-001).
- **Alternatives.**
  - **Baked 9-slice sprites per button** are an art asset, and would not follow the tokens.
  - **A Unity UI `Outline` effect** on one image gives a hard, blocky outline on rounded shapes.

## R6. Color sets

- **Decision.**
  - `ColorSet(Face, Top, Lip, Line)` is derived from one base color:
    - Top = base lightened 0.18;
    - Lip = base darkened 0.25;
    - Line = base darkened 0.42.
  - Named sets: green (primary), cream (secondary), white (icon), blue (level), dark (2×), red (close and HARD),
    purple (SUPER HARD), and the four boosters.
  - The plate, its outline and the wood are named garden tokens.
- **Why.** FR-008: one place to tune. The mockup's hand-picked colors are within a few steps of these derivations.
  Tests check that the Line color keeps 3:1 against a white label fill (FR-025).

## R7. Decoration: leaves and flowers as shapes

- **Decision.**
  - One new shape cluster, `ui.deco.garden` (three lens-shaped leaves and a 5-petal flower, drawn part by part in their
    colors), and the `ui.play` triangle. Every shape is a registered slot, so the cluster is one slot.
  - A cluster is three leaves and one flower, at the top-left and bottom-right corners of a main action button.
  - The cluster's box is `garden.deco_size`, about 0.75 of the button height. It never takes touch input and never
    covers the label.
  - The slot `ui.deco.garden` registers it for the asset inventory.
  - A design switch, `garden.decorations`, turns all clusters off together.
- **Why.** FR-011a and FR-030.
- **Alternatives.** An image asset was rejected: no art files.

## R8. Main-button proportions

- **Decision.**
  - New sizes:
    - PLAY: `size.play` 540 × 204 (from 700 × 150);
    - card primaries (RESUME, NEXT, CLAIM, CONTINUE): `size.card_primary` 620 × 140;
    - card secondaries: `size.card_secondary_width` 580, centered.
  - `ScreenLayout.Home` takes the new PLAY box; the cards center their buttons.
- **Why.** FR-011: the owner asked for buttons shorter and taller, about 2.6 : 1 for PLAY. The preview's layout
  checks (no overlap, safe area, touch size) still have to pass on 16:9, 19.5:9 and 21:9.

## R9. The volumetric 2D board and cells

- **Decision.**
  - **Cells.** The lip grows from 8 to 14 units. The face gets a gradient (top 0.1 lighter) and a highlight band (white,
    40% alpha, top quarter). A 2-unit outline in the cell's own darker shade.
  - **Board.** The panel becomes a wooden frame: a 6-unit brown outline, a thickness below and a cream surface.
  - **Empty ground cells.** They stay flat.
- **Why.** FR-023. The symbol keeps its place and size, and its 3:1 contrast is measured against the face color; the
  highlight stays above the symbol's box. Neighboring cells keep their gap.
- **Alternatives.** A 3D-rendered board was compared on the canvas and rejected (spec Clarifications).

## R10. Pods and slots

- **Decision.**
  - **Pods.** The lip goes to 20 units, with a bevel (an inner top light and a bottom shade) and a highlight. Stacks
    keep their visible layers.
  - **Slots.** The slots become sunk wells: a darker top band and an inner shadow, in a frame like the board's. The
    danger slot keeps its red dashed frame.
- **Why.** FR-022. They are 2D and not 3D (spec Clarifications).

## R11. Booster tiles

- **Decision.**
  - **Shape.** The tile shape of the mockup (FR-031, [contracts/booster-tile.md](contracts/booster-tile.md)).
  - **States.** They come from data both clients already have:
    - charges and price from the economy;
    - `Targeting` (Return, Bloom Burst) for the selected state;
    - `Applicable` and `Affordable` (`LevelSession.Check`) for the disabled state.
  - **Selected.** It replaces today's colored ring with the golden ring and a raise of 12 units.
- **Why.** The owner picked "Плитка" from six variants. No rule or economy data is added.

## R12. Performance

- **Decision.**
  - Unity: about 7 images per button. The label materials are cached per (font, look) in `UiFonts`, with no per-label
    `fontMaterial`. The decoration sprites are rasterized once per size, as all shapes are.
  - Playtest: the typeface is created once. The gradient shaders are cached per (colors, height). The extrusion passes
    run only for labels on colored faces.
- **Why.** SC-005: 30+ fps on the reference low-end device. The playtest check measures no frame time, so the phone
  checklist keeps the device check.

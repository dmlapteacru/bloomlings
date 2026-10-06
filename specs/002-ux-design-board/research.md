# Research: UX Design Board

**Feature**: `specs/002-ux-design-board` | **Date**: 2026-09-30

The spec has no open questions. The owner answered FR-003 (the Unity client and the full playtest APK get the design;
the tester stays minimal) and FR-011 (symbols on tiles). The decisions below settle how the design is built without
art assets, how the two clients share it, and how it is checked without a device.

## R1. One engine-free design kit shared by both clients

- **Decision**: Put everything that defines the look but needs no engine into `client/Assets/Bloomlings/UI/Design/`,
  engine-free:
  - the visual tokens;
  - the shape library;
  - the shape rasterizer;
  - the screen layouts;
  - the number format;
  - the asset slot registry.

  The Unity client uses it directly. The full playtest links it like the other engine-free client files
  (`Playtest.Shared.props`), never copying it.
- **Rationale**: The design must look the same in both builds (FR-003). Two hand-kept palettes or icon sets would drift.
  The repository already shares engine-free client code with the playtest (save, progression, economy, ink contrast),
  so this follows an existing, tested pattern. It also keeps the kit testable under .NET (`client/DotnetCheck`).
- **Alternatives considered**:
  - Separate palettes and icons per client: rejected, because they would drift and be tested twice.
  - Moving the kit into `core/`: rejected. `core/` holds rules, content and tools; presentation must never live there
    (constitution III).

## R2. Icons and shapes: shared signed distance functions, rasterized per client

- **Decision**: Move the signed distance functions of `ProceduralSprites` into an engine-free `ShapeLibrary` using
  `System.MathF`. Each shape has a stable id (for example `icon.variant.leaf`, `ui.pause`, `booster.shuffle`).
  - An engine-free `ShapeRaster` turns a shape into an anti-aliased alpha mask of a given size.
  - Unity wraps the mask in a `Texture2D`/`Sprite`, keeping the current caching.
  - The playtest wraps it in an Android `Bitmap` and tints it with the paint color.
  - The library also gets the new UI shapes of the board:
    - pause, home, restart, play and close;
    - chevron, plus, gift, trophy and medal;
    - ad (video), shirt (Wardrobe), grid (Collection) and sun (Daily Challenge);
    - the Petal symbol, a Bloomling face and a garden stone.
- **Rationale**: Every placeholder shape exists once and is the same on both clients. The existing SDF approach
  already gives every variant its own shape (FR-005, FR-072) with no asset file (FR-002). Rasterized masks are fast
  to draw every frame.
- **Alternatives considered**:
  - Android `Path` drawings: rejected, because every shape would be written twice.
  - Vector drawables, SVG or icon fonts: rejected, because they are asset files, which FR-002 forbids for now.

## R3. A painter interface in the playtest, and PNG previews without a device

- **Decision**: Draw the full playtest's designed screens against a small `IPainter` interface. It offers:
  - rounded rectangles, with fill, gradient and outline;
  - circles and dashed outlines;
  - shape masks;
  - text, with size, weight, alignment and outline;
  - clipping and alpha.

  There are two implementations:
  - `AndroidPainter`, over `Android.Graphics.Canvas`, in the APK;
  - `SkiaPainter`, in a new console tool, `playtest/preview`.

  The tool renders every board frame (1–17) at 16:9, 19.5:9 and 21:9 into PNG files. The screens are engine-free
  (they only see `IPainter`, the core's `LevelView`, `LevelAnimator` and the linked client services), so the preview
  and the APK run the same screen code.
- **Rationale**:
  - The Android SDK and Unity are not available where the work is done. The previews make the design visible and
    reviewable side by side with the board (SC-001), and let tests catch cut-off or overlapping elements across
    aspect ratios (SC-007).
  - SkiaSharp was checked in this environment: it restores, runs on Linux and draws text with the system fonts.
- **Alternatives considered**:
  - SkiaSharp inside the APK: rejected. It adds about 10 MB of native libraries, and its Android build cannot be
    verified before a manual CI run.
  - No previews: rejected. The design could then only be judged on a phone after a manual build.

## R4. The tester keeps its minimal view

- **Decision**:
  - The current `GameView` becomes the tester's view, `TesterView`. It keeps its minimal look and quick loop: ◀ ▶,
    free boosters, instant results.
  - Its Home, progression and meta branches are removed, because the designed screens replace them in the full
    playtest.
  - The designed screens live where only the full playtest compiles them: the tester compiles only
    `playtest/android/*.cs`.
  - `MainActivity` picks the view by flavor.
- **Rationale**: This is what FR-003 asks for. It keeps one owner per screen and no dead branches.
- **Alternatives considered**: Giving the tester the new look (option C of the question): rejected by the owner.

## R5. Layout: shared normalized regions from the safe area

- **Decision**: An engine-free `ScreenLayout` computes the regions of each screen from the screen size and its safe
  insets.
  - **Gameplay (frame 7 order):** top bar, board, slots, tray, booster bar.
    - Fixed-height bands, scaled by width, hold the top bar, the slots, the tray and the booster bar.
    - The board gets the rest.
    - On tall phones the spare height goes around the board. On short phones the board shrinks first. The bands keep
      a minimum size, so counts stay readable.
  - **Home, cards and bottom sheets** follow the same approach.
  - Both clients place their views in these regions: Unity through normalized anchors, the playtest through pixel
    rectangles.
- **Rationale**: One layout rule, tested once for 16:9 to 21:9 (SC-007, the edge cases). The order satisfies spec 001
  FR-068: the tray at the bottom with a compact booster bar, which frame 7 puts last.
- **Alternatives considered**:
  - Hand-tuned fractions per client, as today: rejected, because they cannot be tested and would drift.
  - Unity layout groups: rejected, because they are Unity-only and the playtest could not share them.

## R6. Visual tokens sampled from the board

- **Decision**: Name every color, radius, type style, spacing and elevation in `DesignTokens`, with values sampled
  from the board. The full list is in [`contracts/design-tokens.md`](contracts/design-tokens.md). Among them:
  - cream panels;
  - the green primary button with a darker lower edge;
  - cream secondary buttons;
  - the white round icon buttons;
  - the dark 2× pill;
  - the sky-blue level pill;
  - the HARD red and SUPER HARD purple;
  - the Petal pink;
  - the medal gold, silver and bronze;
  - the danger red;
  - the dimmed backdrop.

  Theme backgrounds keep `ThemeRotation`. The backdrop derives its sky and meadow tints from each theme's colors.
- **Rationale**: FR-005 requires one visual language. Tokens let final art replace placeholders in one place (the spec's
  "Visual token" entity). The existing theme data stays the source for FR-066.
- **Alternatives considered**: Keeping `UiTheme` as the only palette: rejected. It is Unity-only and does not cover the
  board's components.

## R7. Tiles, pods and slots keep the readability rules

- **Decision**:
  - **Tiles** keep the variant colors that passed the readability checks. The picture-first mosaic needs them,
    because the picture shows through the tile colors. The tile style becomes the board's style: a rounded "raised"
    tile with a lighter top and a darker lower edge, the variant symbol in `InkContrast` ink, and no faces (FR-011).
  - **Pods** use the board's card:
    - a light tint of the variant color;
    - the family silhouette in the variant color, with a small face;
    - the variant symbol large on the body, in ink;
    - the count in a pill at the bottom.

    Prominence stays symbol, color, count, silhouette (spec 001 FR-012).
  - **Slots** follow frame 13, with the danger slot's red dashed frame.
- **Rationale**: Spec 001 FR-005, FR-070 and FR-072 and SC-004 keep holding. The board's pale tile tints would wash out
  the picture and are not needed for the look. This is recorded as a deviation.
- **Alternatives considered**: Pale tile tints with symbols in the variant color: rejected. Light variants fall below 3:1
  contrast on their own tint.

## R8. Backgrounds without art

- **Decision**: A procedural garden backdrop:
  - a vertical sky gradient;
  - two or three soft rolling hills in meadow greens;
  - rounded bush clusters at the sides;
  - small blossom dots.

  Everything is tinted from the level band's theme and kept light (FR-008). It is drawn once per screen size and
  cached as a texture or bitmap.
- **Rationale**: This suggests the board's garden without art, and stays behind the board without competing with it
  (constitution VII).
- **Alternatives considered**: Flat colors, as today: rejected, because they do not read as a garden.

## R9. Text and fonts

- **Decision**:
  - Use the platform's bold sans-serif for now: TextMeshPro's default font in Unity, the system bold typeface on
    Android and in the preview.
  - Titles and buttons are uppercase, bold, with a soft dark outline or shadow, as on the board.
  - Numbers are grouped with a thin space every three digits ("1 240"), using an engine-free `NumberText`.
  - Longer localized text shrinks to fit its shape (auto-size), never overflowing.
  - The rounded display font and body font are listed in the asset inventory.
- **Rationale**: No font asset is added (FR-002). The edge cases on grouping and translations are covered.
- **Alternatives considered**: Bundling an open-license rounded font now: deferred to the asset inventory. It is an
  asset.

## R10. Asset inventory kept in sync with the placeholders

- **Decision**:
  - Every placeholder is drawn through a registered asset slot id. The registry is `AssetSlots`, engine-free. Each
    entry holds:
    - a category;
    - the frames that use it;
    - its states;
    - a size class;
    - a readability duty;
    - a launch priority;
    - a description.
  - The inventory document, `specs/002-ux-design-board/asset-inventory.md`, is generated from the registry by the
    preview tool.
  - A test checks that every shape and backdrop drawn by either client resolves to a registered slot, and that every
    slot is used.
  - Audio slots list the synthesized `SoundCue`s and the per-theme music.
- **Rationale**: SC-003 requires the inventory to match the placeholders 100% both ways. A generated document with a test
  cannot drift.
- **Alternatives considered**: A hand-written list: rejected, because nothing would keep it complete.

## R11. Motion

- **Decision**: Keep the existing calm animations (`UiFx`, the slot and pod animations, `LevelAnimator`), and add only:
  - button press scaling;
  - popup pop-in, and the bottom sheet sliding up;
  - the win card rising after the reveal.

  Every duration scales with the 2× setting where it runs during play.
- **Rationale**: FR-028. There is no new gameplay timing, and the rules stay untouched (constitution III).
- **Alternatives considered**: None.

## R12. Placeholder online data

- **Decision**:
  - The Leaderboard keeps its offline behaviour: the last known list with a notice. In the playtest, which has no
    server, it shows only the player's own row and neighbours built from local progress, clearly labelled "offline".
  - Store real-money rows show "unavailable" when purchases are not initialized (always in the playtest).
  - Daily Reward and Collection run on their engine-free services, linked into the playtest.
- **Rationale**: "Everything server-side is deferred" (the owner's rule). The spec's edge cases define the offline
  looks.
- **Alternatives considered**: Fake global players in the playtest: rejected, because invented data would mislead
  playtests.

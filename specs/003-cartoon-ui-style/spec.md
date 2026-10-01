# Feature Specification: Cartoon UI Style — game-like garden buttons and a volumetric 2D board

**Feature Branch**: `003-cartoon-ui-style`

**Created**: 2026-09-30

**Status**: Draft

**Input**: User description: "ты можешь програмно кнопки и другие элементы где возможно сделать более "мультяшно/как в
играх" что-ли, посмотри на Colony Flow для вдохновления"
(Programmatically, and wherever possible, make the buttons and other elements look more cartoony and game-like. Look
at Colony Flow for inspiration.)

**Builds on**: spec 002 (`specs/002-ux-design-board/spec.md`). Spec 002 set the screens, their layout and the visual
language of the design board, all drawn without art assets. This feature keeps every screen, layout, element order
and state of spec 002. It changes how the elements are drawn and how they move: from a soft, flat look to the chunky,
volumetric, bouncy look of casual mobile games, in Bloomlings' garden theme.

**Mockup** (for review before implementation): the interactive Design canvas
[Bloomlings Cartoon UI](https://claude.ai/artifact/DcKztGLXTntBcooCuJGqik) (private to the owner). Its pages:
- "Бустеры": six booster shapes in every state and in a level ([`booster-variants.jpg`](booster-variants.jpg));
- "Садовый": the chosen direction on the main screens ([`garden-direction.jpg`](garden-direction.jpg),
  [`garden-play-button.png`](garden-play-button.png));
- "Варианты стиля": the five 2D directions and the 3D and hybrid comparisons ([`style-variants.jpg`](style-variants.jpg));
- "Макет экранов": the first cartoon pass next to the spec 002 look ([`mockup-before-after.jpg`](mockup-before-after.jpg)).

**Chosen direction — "Garden"** (product owner, 2026-09-30 and 2026-10-01, see Clarifications):
- **Buttons.** Every button lies on a flat cream plate with a thin brown outline, and is itself slightly raised, with
  its own lip and highlight. The model is the owner's reference button.
- **Labels.** Rounded letters with a little volume.
- **Main buttons.** Shorter and taller than in spec 002. Leaves and white flowers decorate them.
- **Gameplay.** The board, its cells, the pods and the slots are volumetric but drawn in 2D.
- **Boosters.** They are tiles.

3D and 2D + 3D hybrids were compared and not adopted, so doc 12 and the constitution stay unchanged.

**Inspiration, not copying**: *Colony Flow!* (ABI Games) is the gameplay reference (constitution I). Its interface, like
most casual puzzle games of its kind, uses:
- chunky buttons with a thick darker lip;
- a glossy highlight on the upper half;
- bold white labels with a dark outline;
- sticker-like badges and counters;
- popups that pop in with a small bounce.

This feature takes those general genre conventions and applies them to Bloomlings' own palette, shapes and garden
theme. It MUST NOT reuse Colony Flow's graphics, icons, colors as a scheme, or layouts (constitution I).

## Clarifications

### Session 2026-09-30

- Q: Which gameplay elements get the cartoon style? → A: The menus and cards, plus the pods, the Waiting Slots, the
  booster buttons and the top bar. Board tiles get only a thin outline. (Superseded on 2026-10-01: the board and its
  cells are volumetric 2D, FR-023.)
- Q: Does drawn depth (lip, gloss, drop shadow) fit the locked "flat 2D" direction of doc 12 §1 and constitution VII?
  → A: Yes. Depth drawn in the plane, with no perspective, 3D or isometric view, counts as flat 2D. The documents stay
  unchanged, and the reading is recorded in FR-005.
- Before implementation, the product owner reviews an interactive mockup of the style and may adjust it. The mockup
  shows the components and the main screens, lets them tap to feel the motion, and lets them tune the recipe
  (outline, lip, gloss, shadow, bounce) and compare it with the spec 002 look.

### Session 2026-10-01

- Q: Which style direction? → A: "Garden", built like the owner's reference button: a flat cream plate with a thin brown
  outline, and a slightly raised button on it (FR-006, FR-007). The owner also compared five 2D directions, 3D buttons
  and two 2D + 3D hybrids, and chose 2D.
- Q: 3D or 2D for the gameplay? → A: The board and its cells are volumetric, drawn in 2D (FR-023). Pods are volumetric
  2D, never 3D, so they read well in the tray (FR-022). A 3D-rendered board was shown for comparison only.
- Q: Main button proportions? → A: Shorter and taller than in spec 002, about 2.6 : 1 for PLAY. Labels get a little
  volume, kept balanced, and the ▶ on PLAY is as tall as the letters (FR-009, FR-010, FR-011).
- Q: Motion? → A: The press and spring-back of the mockup are approved as they are (FR-017).
- Q: Booster buttons? → A: The tile shape (FR-031): a rounded-square plate, a colored tile with a large icon, and the
  charges in a round dark badge in the corner.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Buttons and controls feel like a game (Priority: P1)

A player on Home or in any card sees chunky, toy-like controls:
- **PLAY.** The PLAY button is a flat cream plate with a thin brown outline. A green, slightly raised pill lies on it,
  with a darker green outline, its own lip and a soft highlight. Leaves and white flowers sit at two of its corners.
- **Label.** "Play ▶" uses rounded letters with a little volume: a light fill, a thin dark green outline and a short
  extrusion below. The ▶ is as tall as the letters.
- **Press.** On touch, the button sinks into its lip while the plate stays. On release, it springs back with a small
  bounce.

Secondary buttons, round icon buttons, pills, tabs and toggles follow the same recipe in their own colors.

**Why this priority**: Buttons are touched on every screen and every session. They carry most of the "this is a game"
feel the request asks for, and they are shared by every other element.

**Independent Test**: Open Home, the pause card and the Store. Compare each control with the before/after comparison
sheet. Then touch and release each control and watch the press and the spring-back.

**Acceptance Scenarios**:

1. **Given** Home, **Then** the PLAY button shows all its parts:
   - the plate;
   - the raised button with its outline and lip;
   - the highlight;
   - the drop shadow;
   - the volumetric label and ▶;
   - the leaves and flowers.
2. **Given** any enabled button, **When** the player touches it, **Then** it sinks into its lip at once. **When** they
   release it, **Then** it springs back with one small overshoot and runs its action.
3. **Given** a disabled button, **Then** it keeps its outline and shape, loses its gloss and color (greyed), and does
   not squash on touch.
4. **Given** the round buttons (Pause, Settings, 2×, close), **Then** each is a round plate with a raised disc on it,
   with an outline, a lip and a highlight. Its icon is a light glyph on a colored disc, or a dark glyph on a light disc.
5. **Given** the Petals pill, the level pill and the tabs and toggles in Settings and the Store, **Then** they use the
   same outline, lip and gloss recipe. The selected tab and the "on" toggle stand out by shape and position as well as
   by color.

---

### User Story 2 - Popups, badges and rewards pop like a game (Priority: P2)

A player pauses, jams, wins, claims the Daily Reward or reaches a milestone:
- **Cards.** Each popup card is cream paper in a wooden frame. Its header is a colored band shaped like a button on a
  plate, and carries the title in large volumetric letters. Its close button is a red round button on a plate.
- **Pop-in.** The card pops in with one small bounce. The jam sheet slides up and settles with a small bounce.
- **Rewards.** Petals earned count up from 0 to the amount, and a short sparkle burst plays when a reward is claimed.
- **Stickers.** Badges (HARD, SUPER HARD, counts and prices) look like outlined stickers.

**Why this priority**: These moments are the game's emotional beats: pause, fail, success, reward. A game-like look
here makes them feel rewarding, but they are seen less often than buttons.

**Independent Test**: Trigger each card (pause, jam, win, milestone, Daily Reward, Leaderboard, Collection, Store,
Settings). Compare each with the comparison sheet, and watch each open and close.

**Acceptance Scenarios**:

1. **Given** any popup card, **Then** it shows:
   - the wooden frame;
   - the header band with its title in volumetric letters;
   - a soft shadow;
   - its red round close button, where closing is allowed.
2. **Given** a card or sheet opens, **Then** it reaches its place with a single small overshoot and settles within
   0.35 s.
3. **Given** a win, **When** the reward shows, **Then** the Petals amount counts up to the value, and a short sparkle
   burst plays. NEXT becomes usable at once; the count-up never blocks it.
4. **Given** a Hard or Super Hard level, **Then** its badge is an outlined sticker in red or purple under the level
   pill. Count and price badges on boosters are outlined stickers too.
5. **Given** a claimable reward (Daily Reward CLAIM, milestone CONTINUE) or PLAY on Home, **Then** that one primary
   button breathes gently while it waits. No other element loops.

---

### User Story 3 - Volumetric gameplay pieces and board, still readable (Priority: P3)

A player plays a level. The touchable gameplay pieces match the style of the buttons:
- the pods in the tray;
- the Waiting Slots;
- the booster tiles;
- the top-bar pills.

The board is volumetric too, but drawn in 2D:
- **Board.** It sits in a wooden frame.
- **Cells.** Each cell is a small raised block in its own color, with a thicker lip, a bevel and a soft highlight.
- **Slots.** The slots are sunk wells.

Nothing is 3D. Pods stay 2D so they read well in the tray and in stacks.

**Why this priority**: The gameplay screen is where players spend most of their time, but it carries the readability
rules. It is styled last, and only as far as readability allows.

**Independent Test**: Open a Normal, a Hard and a Super Hard level and compare them with the sheet. Put pods and
slots into every state of frames 12 and 13 (spec 002), and pass each booster unlock. Every state must still read as
in spec 002.

**Acceptance Scenarios**:

1. **Given** the tray, **Then** each exposed pod is a volumetric card: a thick lip, a bevel and a highlight. Its variant
   symbol, color, count and family silhouette keep spec 001 FR-012's order of prominence.
2. **Given** the pod states of frame 12, **Then** each state is still distinguishable by shape or symbol:
   - next in stack;
   - pressed;
   - locked;
   - mystery;
   - connected.
3. **Given** the slot states of frame 13, **Then** each still reads by shape or symbol:
   - empty;
   - working;
   - stuck;
   - locked;
   - danger.

   The danger slot keeps its red dashed frame.
4. **Given** the board, **Then** the lip, bevel and highlight of a cell never cover its symbol. Every symbol keeps at
   least 3:1 contrast on its cell, and neighboring cells never look joined.
5. **Given** the booster bar, **Then** each booster is a tile (FR-031). It squashes on touch like the other buttons,
   and shows its charges, its price, its selected state or its disabled state.

---

### Edge Cases

- **Small elements.** Badges, count pills and small icons have too little room for thick outlines. Outline width,
  lip depth and gloss scale with the element's size, and none of them drops a symbol or number below its readable
  size.
- **Long text.** Longer translated labels (for example, German) shrink to fit, together with their outline. The
  outline never overflows the button.
- **Themes.** On every level-band backdrop, light and dark, an element's outline and shadow keep it separate from the
  background.
- **Fast input.** Rapid taps: the squash and the spring-back never delay or drop a tap. A tap on a button that is
  still springing back counts at once.
- **2× speed.** The 2× setting speeds up gameplay animations only (spec 001 FR-069). Button presses and card pops
  keep their own short timing.
- **Colorblind players.** Selected tabs, "on" toggles, disabled buttons and every gameplay state stay distinguishable
  without color, by shape, symbol, position or outline.
- **Low-end phones.** The extra layers (outline, lip, gloss, shadow) must not make screens stutter.
- **Level tester APK.** It keeps its minimal look, as in spec 002.

## Requirements *(mandatory)*

### Functional Requirements

#### A. Scope and principles

- **FR-001**: The game's interface elements MUST take on a cartoony, casual-game look, drawn from shapes, gradients,
  outlines and text alone. The feature MUST add no art or audio asset files, and no font file unless FR-009 allows
  one. Every styled element MUST stay
  replaceable by final art later, with no change of layout or behavior (spec 002 FR-002).
- **FR-002**: The style MUST apply to the same builds as spec 002: the Unity game client and the full playtest APK. The
  level tester APK keeps its minimal look.
- **FR-003**: The feature is presentation only. It MUST NOT change:
  - any rule, level, economy value, unlock level or reward;
  - the screens, their element order or their layout (spec 002 frames);
  - the outcome of any tap sequence.
- **FR-004**: The look MUST be Bloomlings' own:
  - its palette (cream, soft greens, sky blues, blossom pinks);
  - its garden theme;
  - its shapes and symbols.

  Only general genre conventions are taken from Colony Flow. No Colony Flow graphic, icon, color scheme or layout is
  reused (constitution I).
- **FR-005**: The style MUST stay within the locked visual direction: 2D, light, calm, minimal, low eye strain,
  board-dominant, and no 3D or isometric drift (doc 12 §1, constitution VII). Drawn depth counts as flat 2D, as long
  as it is drawn in the plane with no perspective, no 3D models and no isometric view. This covers the lip, the
  gloss band and the drop shadow. Doc 12 and the constitution stay unchanged.

#### B. The garden recipe (all styled elements)

- **FR-006 Plate**: Every button, pill and round button MUST lie on a flat plate:
  - the plate is cream, with a thin brown outline;
  - it has a small visible thickness below it and a soft drop shadow;
  - its edge shows evenly around the button.
- **FR-007 Raised button**: The button on the plate MUST have:
  - a face in its color, lighter at the top;
  - its own outline in a darker shade of that color, never pure black;
  - a lip: a darker band along its bottom edge;
  - a soft highlight band across its upper part.

  A pressed button sinks into its lip while the plate stays.
- **FR-008 Colors**: Each element color MUST come as one set of related shades: face, lighter top, lip and outline.
  Every set is derived the same way from one base color. The cream of the plates, the brown of the outlines and the
  wood of the frames MUST also be named values, so the whole style can be tuned in one place.
- **FR-009 Labels**:
  - **Font and case.** Labels use rounded bold letters and sentence case ("Play", "Resume").
    [NEEDS CLARIFICATION: a rounded font is a font file, which FR-001 does not allow yet, and Fredoka has no Cyrillic.
    Add a rounded font with Cyrillic, and which one?]
    [NEEDS CLARIFICATION: spec 002 FR-005 asks for uppercase titles. Switch to sentence case as on the reference button?]
  - **On colored faces.** Labels MUST have a little volume, kept balanced:
    - a light fill with a slight vertical gradient;
    - a thin outline in the face's dark hue;
    - a short extrusion below, of about 9% of the letter height;
    - a soft shadow.
  - **On cream faces.** Labels stay dark brown, with no extrusion.
  - **Titles.** Titles on card headers use the colored-face style at display size.
- **FR-010 Icons**:
  - On colored faces, icons are light glyphs. On light faces, they are dark glyphs.
  - The ▶ on PLAY MUST be as tall as the letters and follow the label's style.
  - The Petal symbol MUST get an outline and look the same everywhere (spec 002 FR-006).
- **FR-011 Shapes and proportions**:
  - **Main action buttons** (PLAY, NEXT, RESUME, CLAIM, CONTINUE) MUST be shorter and taller than in spec 002, about
    2.6 : 1 for PLAY.
  - **Card list buttons** are narrower and centered.
  - **Element order** stays as in spec 002 (FR-003), and the layout checks keep passing (FR-026).
- **FR-011a Decoration**: Leaves and white flowers MUST appear only on the main action buttons. They never appear on
  gameplay pieces. They can be switched off as one setting, and they never cover a label or a touch target.

#### C. Components

- **FR-012 Buttons**: The primary (green), secondary (cream) and dark buttons and the round icon buttons MUST follow the
  full recipe. Every button MUST have these states:
  - **enabled:** full recipe;
  - **pressed:** the button sunk into its lip, the plate in place;
  - **disabled:** greyed, with no highlight and no press.

  The states MUST differ by shape (lip depth, highlight) as well as by color.
- **FR-013 Pills and counters**: The Petals pill, the level pill and the 2× pill MUST follow the recipe. The Petals
  "+" MUST be a small round green button on its own plate.
- **FR-014 Badges**: Badges MUST be outlined and readable at their small size:
  - **HARD and SUPER HARD:** small raised pills on plates;
  - **counts:** white numbers on a round dark brown badge with a cream ring;
  - **prices:** a cream tag with the Petal symbol and the number.
- **FR-015 Cards and the sheet**: Popup cards and the jam sheet MUST have:
  - cream paper in a wooden frame, with a thickness below and a soft shadow;
  - a colored header band shaped like a button on a plate, with the title in volumetric letters;
  - a red round close button on a plate, where closing is allowed.

  The card's content area stays cream, so text and rows stay readable. The dimmed backdrop of spec 002 FR-007 stays.
- **FR-016 Tabs, toggles and rows**:
  - **Tabs.** The selected tab is a raised button on a plate; the others are sunk.
  - **Toggles.** A toggle is a chunky outlined track with a raised knob.
  - **List rows.** Rows (Store, Leaderboard) are outlined, rounded panels. The player's own leaderboard row is a
    raised highlight.

#### D. Motion

- **FR-017 Press**: On touch, an enabled element MUST squash (shrink and sink into its lip) within the same frame. On
  release, it MUST spring back with one small overshoot, as in the approved mockup. The press MUST NOT delay, drop or
  repeat the action.
- **FR-018 Pop-in**: Cards, badges and reward icons MUST appear with one small overshoot, and settle within 0.35 s. The
  sheet slides up and settles with one small bounce. Closing is a quick shrink or slide, with no bounce.
- **FR-019 Idle**: Only one element per screen may loop, and only while it waits for the player: PLAY on Home, or a
  claimable reward's button. It breathes gently, with a scale change of at most 4% and a period of at least 1.2 s.
  Nothing may flash more than 3 times per second.
- **FR-020 Rewards**: Earned Petals MUST count up to their value, and a short sparkle burst MUST play on a win and on a
  claim. The effect uses the existing placeholder effects. It MUST NOT delay the next button.
- **FR-021 Timing**: UI motion keeps its own short timing, independent of the 2× speed setting. Motion stays
  presentation only: it never influences the rules (constitution III).

#### E. Gameplay pieces (User Story 3)

- **FR-022**: The pods and the Waiting Slots MUST be volumetric, drawn in 2D and never 3D:
  - pods have a thick lip, a bevel and a highlight, and stacks show their layers;
  - slots are sunk wells in a frame like the board's.

  They keep:
  - the states of spec 002 FR-012 (pods) and FR-013 (slots);
  - spec 001 FR-012's order of prominence: variant symbol, variant color, count, family silhouette.

  The top-bar pills follow the recipe of FR-006 and FR-007.
- **FR-023**: The board and its cells MUST be volumetric but drawn in 2D:
  - the board sits in a wooden frame;
  - each cell is a raised block in its own color, with a thicker lip, a bevel and a soft highlight.

  The board stays board-dominant and calm. The lip, bevel and highlight MUST NOT cover a cell's symbol. They MUST NOT
  make neighboring cells look joined or partially occupied (constitution II).
- **FR-031 Booster tiles**: Each booster MUST be a tile: a rounded-square cream plate, a raised tile in the booster's
  color, and a large light icon. It MUST show these states, each by shape or symbol as well as by color:
  - **charges:** a round dark brown badge in the top corner with "×N";
  - **no charges:** a cream price tag below, with the Petal symbol and the price, and a small green "+" in the corner
    (spec 002 FR-014);
  - **selected:** raised, with a pulsing golden ring, while the booster waits for its target. Only Return (a waiting
    pod, spec 001 FR-045) and Bloom Burst (a visible variant, spec 001 FR-050) have one;
  - **disabled:** greyed and not pressable when it can have no effect (spec 001 FR-046);
  - **pressed:** squashed, as in FR-017.

#### F. Readability, accessibility and performance

- **FR-024**: Every readability rule of spec 001 and spec 002 MUST keep passing:
  - each variant identified by color and symbol;
  - symbols and counts with at least 3:1 contrast on their background;
  - every state shown by shape or symbol, never by color alone;
  - the grayscale and colorblind checks of all 8 launch variants.

  The gloss and the outline MUST NOT lower a symbol's or a count's contrast below those limits.
- **FR-025**: Every label on a colored face MUST be legible through its outline and extrusion:
  - the contrast between the label's fill and its outline MUST reach 4.5:1 for body text and 3:1 for large text;
  - the outline MUST form a visible ring around every letter at the smallest size the label is drawn.
- **FR-026**: Touch targets, the safe area and the element order MUST stay as in spec 002. The minimum touch size and
  the no-overlap and in-safe-area checks keep passing, with outlines and shadows counted as part of the element.
- **FR-027**: Screens MUST stay as smooth as in spec 002 on the reference low-end device.

#### G. Consistency and review

- **FR-028**: All new style values MUST be named in the shared design values, and both builds MUST use them:
  - the plate inset, its outline and thickness;
  - the outline widths;
  - the lip depth;
  - the highlight;
  - the shadow;
  - the label volume;
  - the decoration;
  - the motion curves.
- **FR-029**: A before/after comparison sheet MUST show every spec 002 frame in the old and the new style side by
  side, for the product owner's review.
- **FR-030**: The asset inventory of spec 002 MUST note, for each UI entry, that the final art follows this style:
  - plate, raised button, lip and highlight;
  - volumetric labels;
  - leaves and flowers.

  It adds a slot for the leaves-and-flowers decoration, and no other.

### Key Entities *(include if feature involves data)*

- **Style recipe**: the layers that make an element look like the reference button, with their sizes scaled to the
  element:
  - the plate;
  - the raised button with its outline and lip;
  - the highlight;
  - the shadow;
  - the volumetric label.
- **Color set**: the face, top, lip and outline shades derived from one base color, plus the named plate, outline and
  wood colors.
- **Motion curve**: a named press, pop, sheet, idle or count-up motion, with its duration, overshoot and scale.
- **Component family**: a group of elements that share the recipe:
  - buttons, pills and badges;
  - cards and the sheet;
  - tabs, toggles and rows;
  - the gameplay pieces: pods, slots, booster tiles, top-bar pills, and the board with its cells.
- **Comparison sheet**: the before/after images of every frame, used for the review.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In the before/after review, the product owner rates the new look "more game-like" for every component
  family, and accepts it for at least 90% of the spec 002 frames.
- **SC-002**: In the comparison sheet:
  - 100% of the component families of FR-012 to FR-016, FR-022 and FR-031 show every part of the recipe;
  - every booster state of FR-031 is shown;
  - the board and cells show their volumetric 2D look (FR-023).
- **SC-003**: The readability results of spec 002 still hold:
  - symbols and counts reach at least 3:1 contrast on every variant color;
  - every label reaches the FR-025 contrast against its outline;
  - all 8 launch variants stay distinguishable in grayscale and in simulated protanopia, deuteranopia and tritanopia;
  - in 10 first-time players, at least 9 tell apart 5 variants on one board within 5 seconds.
- **SC-004**: Every press shows its squash in the frame of the touch. Every pop-in settles within 0.35 s. No tap is
  lost or delayed in a test of 20 rapid taps.
- **SC-005**: Gameplay stays at 30 frames per second or more, with no hitch longer than 100 ms, on the reference
  low-end device (spec 002 SC-005).
- **SC-006**: No new art or audio asset files are added. At most one rounded font family is added, if FR-009's font
  question is answered yes.
- **SC-007**: No screen shows a cut-off or overlapping element, and every touch target keeps its minimum size, on three
  aspect ratios from 16:9 to 21:9.
- **SC-008**: A first-time player still finds and taps PLAY within 3 seconds of Home appearing, in 9 of 10 attempts.
- **SC-009**: The same tap sequences end in the same rules state as before the feature (0 differences in the playtest
  replay check).

## Assumptions

- **No screenshots.** Colony Flow's store pages could not be viewed from the session where this spec was written: the
  network policy blocks them. The style follows the general conventions of casual puzzle games of its kind. The product
  owner may share screenshots during clarification to steer the details.
- **Font.** "Programmatically" means no art files. The reference look depends on a rounded font. Whether a font file
  is added is the open question in FR-009. Until it is answered, the system bold font is the fallback.
- **No 3D.** The 3D buttons and the 2D + 3D hybrids were compared and not adopted. Doc 12 §1 and constitution VII stay
  unchanged.
- **Same builds.** The Unity client and the full playtest APK get the style; the tester stays minimal. This follows the
  spec 002 decision (spec 002 Clarifications).
- **Layout.** Spec 002's frames still define the layout, element order and states. This feature changes:
  - the drawing style;
  - the motion;
  - the proportions of the main action buttons (FR-011).

  The spec 002 review (SC-001) is about layout and order, so it is not affected.
- **Board faces.** Tiles keep variant symbols, never faces (spec 001 FR-005, spec 002 FR-011).
- **Header bands.** The colored header band on cards is still "a title at the top" (spec 002 FR-007), so it is not a
  deviation.
- **Booster selection.** The selected state follows the existing rules: Return chooses a waiting pod (spec 001
  FR-045), and Bloom Burst chooses a visible variant (spec 001 FR-050). The other boosters act at once.
- **Existing effects.** Sparkles and the other effects use the existing placeholder effects, with no new effect
  assets.

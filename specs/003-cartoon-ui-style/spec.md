# Feature Specification: Cartoon UI Style — game-like buttons and elements, drawn without assets

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
glossy, bouncy look of casual mobile games.

**Mockup** (for review before implementation): the interactive Design canvas
[Bloomlings Cartoon UI](https://claude.ai/artifact/DcKztGLXTntBcooCuJGqik) (private to the owner) and its still
[`mockup-before-after.jpg`](mockup-before-after.jpg). The canvas redraws the recorded spec 002 screens in the new style,
has the Tweaks of the recipe (outline, lip, gloss, shadow, bounce, breathing, tile outline, font) and a "как сейчас"
switch, and shows the current look under each screen.

**Open decision — style direction**: before the recipe is fixed, the product owner compares five 2D directions
(Мармелад, Наклейка, Мягкий пластилин, Игрушка, Садовый) and, for comparison only, 3D buttons and two 2D + 3D hybrids
(a 3D board with a 2D interface; all gameplay objects in 3D with 2D menus). They are on the canvas page "Варианты
стиля" and in [`style-variants.jpg`](style-variants.jpg). This spec's recipe (FR-006 to FR-011) matches direction 1
until the choice is made. A 3D or hybrid choice conflicts with doc 12 §1 ("flat 2D … no 3D/isometric drift") and
constitution VII ("strictly flat 2D"). It would need a constitution amendment (a MAJOR version bump through a PR) and
a doc 12 change before this spec could adopt it.

**Owner's lean (2026-09-30, not final)**: the garden direction, built like the owner's reference button: a flat cream
plate with a thin brown outline, and a slightly raised button with its own lip and highlight laid on it; a rounded font
and sentence-case labels ("Play ▶"); leaves and white flowers on the main buttons only. The board, its cells and the
pods are volumetric but stay 2D (a thick lip, a bevel and a highlight). Pods must not be 3D, so they read well in the
tray. A 3D-rendered board is shown for comparison only. See the canvas page "Садовый" and
[`garden-direction.jpg`](garden-direction.jpg).

**Booster buttons (open, 2026-10-01)**: the owner wants the feature/item/boost buttons reworked. Six garden-style
experiments are on the canvas page "Бустеры" and in [`booster-variants.jpg`](booster-variants.jpg): token, tile, seed
packet, bubble, capsule and leaf. Each is shown in every state: charges, no charges with the Petal price, selected,
not usable now and pressed.

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
  booster buttons and the top bar. Board tiles get only a thin outline (User Story 3, FR-022, FR-023).
- Q: Does drawn depth (lip, gloss, drop shadow) fit the locked "flat 2D" direction of doc 12 §1 and constitution VII?
  → A: Yes. Depth drawn in the plane, with no perspective, 3D or isometric view, counts as flat 2D. The documents stay
  unchanged, and the reading is recorded in FR-005.
- Before implementation, the product owner reviews an interactive mockup of the style and may adjust it. The mockup
  shows the components and the main screens, lets them tap to feel the motion, and lets them tune the recipe
  (outline, lip, gloss, shadow, bounce) and compare it with the spec 002 look.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Buttons and controls feel like a game (Priority: P1)

A player on Home or in any card sees chunky, toy-like controls:
- The PLAY button is a thick green pill with a darker outline, a deep lower lip, a glossy highlight across its upper
  half and a soft shadow under it.
- Its label "PLAY" is bold white with a dark green outline.
- On touch, the button squashes down into its lip. On release, it springs back with a small bounce.

Secondary buttons, round icon buttons, pills, tabs and toggles follow the same recipe in their own colors.

**Why this priority**: Buttons are touched on every screen and every session. They carry most of the "this is a game"
feel the request asks for, and they are shared by every other element.

**Independent Test**: Open Home, the pause card and the Store. Compare each control with the before/after comparison
sheet. Then touch and release each control and watch the press and the spring-back.

**Acceptance Scenarios**:

1. **Given** Home, **Then** the PLAY button shows all five parts:
   - the outline;
   - the lower lip;
   - the gloss highlight;
   - the drop shadow;
   - the outlined label.
2. **Given** any enabled button, **When** the player touches it, **Then** it sinks into its lip at once. **When** they
   release it, **Then** it springs back with one small overshoot and runs its action.
3. **Given** a disabled button, **Then** it keeps its outline and shape, loses its gloss and color (greyed), and does
   not squash on touch.
4. **Given** the round buttons (Pause, Settings, 2×, close), **Then** each is a raised disc with an outline, a lip and
   a gloss, and its icon reads as a sticker: a light glyph with a dark outline, or a dark glyph on a light disc.
5. **Given** the Petals pill, the level pill and the tabs and toggles in Settings and the Store, **Then** they use the
   same outline, lip and gloss recipe. The selected tab and the "on" toggle stand out by shape and position as well as
   by color.

---

### User Story 2 - Popups, badges and rewards pop like a game (Priority: P2)

A player pauses, jams, wins, claims the Daily Reward or reaches a milestone:
- **Cards.** Each popup card has a thick outline and a colored header band that carries its title in large outlined
  letters. Its close button is a round outlined button.
- **Pop-in.** The card pops in with one small bounce. The jam sheet slides up and settles with a small bounce.
- **Rewards.** Petals earned count up from 0 to the amount, and a short sparkle burst plays when a reward is claimed.
- **Stickers.** Badges (HARD, SUPER HARD, counts and prices) look like outlined stickers.

**Why this priority**: These moments are the game's emotional beats: pause, fail, success, reward. A game-like look
here makes them feel rewarding, but they are seen less often than buttons.

**Independent Test**: Trigger each card (pause, jam, win, milestone, Daily Reward, Leaderboard, Collection, Store,
Settings). Compare each with the comparison sheet, and watch each open and close.

**Acceptance Scenarios**:

1. **Given** any popup card, **Then** it shows:
   - a thick outline;
   - a header band with its title in outlined letters;
   - a soft shadow;
   - its round outlined close button, where closing is allowed.
2. **Given** a card or sheet opens, **Then** it reaches its place with a single small overshoot and settles within
   0.35 s.
3. **Given** a win, **When** the reward shows, **Then** the Petals amount counts up to the value, and a short sparkle
   burst plays. NEXT becomes usable at once; the count-up never blocks it.
4. **Given** a Hard or Super Hard level, **Then** its badge is an outlined sticker in red or purple under the level
   pill. Count and price badges on boosters are outlined stickers too.
5. **Given** a claimable reward (Daily Reward CLAIM, milestone CONTINUE) or PLAY on Home, **Then** that one primary
   button breathes gently while it waits. No other element loops.

---

### User Story 3 - Gameplay pieces in the same style, with a calm board (Priority: P3)

A player plays a level. The touchable gameplay pieces match the cartoon style of the buttons:
- the pods in the tray;
- the Waiting Slots;
- the booster buttons;
- the top-bar pills.

The board itself stays calm and readable, because it is where the player looks all the time. Board tiles get only
a thin outline in a darker shade of their own color: no gloss, no drop shadow and no deeper lip.

**Why this priority**: The gameplay screen is where players spend most of their time, but it carries the readability
rules. It is styled last, and only as far as readability allows.

**Independent Test**: Open a Normal, a Hard and a Super Hard level and compare them with the sheet. Put pods and
slots into every state of frames 12 and 13 (spec 002), and pass each booster unlock. Every state must still read as
in spec 002.

**Acceptance Scenarios**:

1. **Given** the tray, **Then** each exposed pod is a raised, outlined card with a gloss. Its variant symbol, color,
   count and family silhouette keep spec 001 FR-012's order of prominence.
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
4. **Given** the board, **Then** no outline, gloss or shadow covers a tile's symbol, and every symbol keeps at least
   3:1 contrast on its tile.
5. **Given** the booster bar, **Then** each booster is a raised, outlined disc with a gloss and a sticker badge. It
   squashes on touch like the other buttons.

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
  outlines and text alone. The feature MUST add no art, font or audio asset files. Every styled element MUST stay
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

#### B. The cartoon recipe (all styled elements)

- **FR-006 Outline**: Every styled element MUST have an outline in a darker shade of its own color, never pure black.
  This covers buttons, pills, badges, cards, the sheet, tabs, toggles, and the gameplay pieces of FR-022. The outline
  has a thick width for large elements and a thin width for small ones.
- **FR-007 Depth**: Every raised element MUST have:
  - a lower lip in a darker shade, deeper than today's edge;
  - a gloss highlight: a lighter, rounded band across its upper part;
  - a soft drop shadow beneath it.

  A pressed element sinks into its lip, and its shadow shrinks.
- **FR-008 Colors**: Each element color MUST come as one set of related shades:
  - face;
  - lighter top;
  - darker lip;
  - darkest outline;
  - gloss.

  Every set is derived the same way from one base color, so the whole style can be tuned in one place.
- **FR-009 Labels**:
  - **On colored faces**, labels MUST be bold and white (or cream), with a dark outline in the face's hue and a small
    drop shadow.
  - **On cream faces**, labels stay dark.
  - **Titles** on card header bands use the same outlined style at display size.
  - **Numbers** on counters and badges use the same outlined style.
- **FR-010 Icons**: Icons on buttons MUST read as stickers:
  - either a light glyph with a dark outline on a colored face;
  - or a dark glyph on a light face.

  The Petal currency symbol MUST get an outline and a gloss dot, and look the same everywhere (spec 002 FR-006).
- **FR-011 Shapes**: Corners MUST be rounder and shapes chunkier than in spec 002. Buttons, pills and cards keep
  their sizes and positions (FR-003), and grow no larger than their spec 002 boxes.

#### C. Components

- **FR-012 Buttons**: The primary (green), secondary (cream) and dark buttons and the round icon buttons MUST follow the
  full recipe. Every button MUST have these states:
  - **enabled:** full recipe;
  - **pressed:** sunk into the lip;
  - **disabled:** greyed, with no gloss and no press.

  The states MUST differ by shape (lip depth, gloss) as well as by color.
- **FR-013 Pills and counters**: The Petals pill, the level pill and the 2× pill MUST follow the recipe. The Petals
  "+" MUST be a small round green outlined button on the pill.
- **FR-014 Badges**: The HARD and SUPER HARD badges, count badges and price tags MUST be outlined stickers: a bold
  outline, a small gloss, and an outlined number or word.
- **FR-015 Cards and the sheet**: Popup cards and the jam sheet MUST have:
  - a thick outline;
  - a colored header band with the title in outlined display letters;
  - a soft shadow;
  - the round outlined close button, where closing is allowed.

  The card's content area stays cream, so text and rows stay readable. The dimmed backdrop of spec 002 FR-007 stays.
- **FR-016 Tabs, toggles and rows**:
  - **Tabs.** The selected tab is raised and glossy; the others are sunk.
  - **Toggles.** A toggle is a chunky outlined track with a raised knob.
  - **List rows.** Rows (Store, Leaderboard) are outlined, rounded panels. The player's own leaderboard row is a
    raised highlight.

#### D. Motion

- **FR-017 Press**: On touch, an enabled element MUST squash (shrink and sink) within the same frame. On release, it
  MUST spring back with one small overshoot. The press MUST NOT delay, drop or repeat the action.
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

- **FR-022**: The pods, the Waiting Slots, the booster buttons and the top-bar pills MUST follow the recipe. They keep:
  - the states of spec 002 FR-012 (pods) and FR-013 (slots);
  - spec 001 FR-012's order of prominence: variant symbol, variant color, count, family silhouette.
- **FR-023**: Board tiles MUST stay calm and board-dominant. They get only a thin outline in a darker shade of their
  own color, with no gloss, no drop shadow and no deeper lip. The outline MUST NOT cover a tile's symbol, and MUST
  NOT make neighboring tiles look joined or partially occupied (constitution II).

#### F. Readability, accessibility and performance

- **FR-024**: Every readability rule of spec 001 and spec 002 MUST keep passing:
  - each variant identified by color and symbol;
  - symbols and counts with at least 3:1 contrast on their background;
  - every state shown by shape or symbol, never by color alone;
  - the grayscale and colorblind checks of all 8 launch variants.

  The gloss and the outline MUST NOT lower a symbol's or a count's contrast below those limits.
- **FR-025**: Every label on a colored face MUST be legible through its outline. The contrast between the label's fill
  and its outline MUST reach 4.5:1 for body text and 3:1 for large text, and the outline MUST be wide enough to form a
  visible ring around every letter at the smallest size the label is drawn.
- **FR-026**: Touch targets, the safe area and the element order MUST stay as in spec 002. The minimum touch size and
  the no-overlap and in-safe-area checks keep passing, with outlines and shadows counted as part of the element.
- **FR-027**: Screens MUST stay as smooth as in spec 002 on the reference low-end device.

#### G. Consistency and review

- **FR-028**: All new style values MUST be named in the shared design values, and both builds MUST use them. These are
  the outline widths, the lip depth, the gloss strength, the shadow and the motion curves.
- **FR-029**: A before/after comparison sheet MUST show every spec 002 frame in the old and the new style side by
  side, for the product owner's review.
- **FR-030**: The asset inventory of spec 002 MUST note, for each UI entry, that the final art follows this style
  (outline, lip, gloss, outlined labels). It adds no new asset slots.

### Key Entities *(include if feature involves data)*

- **Style recipe**: the set of layers that makes an element look cartoony (outline, lip, gloss, shadow, outlined
  label), with its sizes scaled to the element.
- **Color set**: the face, top, lip, outline and gloss shades derived from one base color.
- **Motion curve**: a named press, pop, sheet, idle or count-up motion, with its duration, overshoot and scale.
- **Component family**: a group of elements that share the recipe: buttons, pills, badges, cards and the sheet, tabs,
  toggles, rows, and the gameplay pieces (pods, slots, booster buttons, top-bar pills).
- **Comparison sheet**: the before/after images of every frame, used for the review.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In the before/after review, the product owner rates the new look "more game-like" for every component
  family, and accepts it for at least 90% of the spec 002 frames.
- **SC-002**: 100% of the component families of FR-012 to FR-016 and FR-022 show every part of the recipe in the
  comparison sheet, and board tiles show only their thin outline (FR-023).
- **SC-003**: The readability results of spec 002 still hold:
  - symbols and counts reach at least 3:1 contrast on every variant color;
  - every label reaches the FR-025 contrast against its outline;
  - all 8 launch variants stay distinguishable in grayscale and in simulated protanopia, deuteranopia and tritanopia;
  - in 10 first-time players, at least 9 tell apart 5 variants on one board within 5 seconds.
- **SC-004**: Every press shows its squash in the frame of the touch. Every pop-in settles within 0.35 s. No tap is
  lost or delayed in a test of 20 rapid taps.
- **SC-005**: Gameplay stays at 30 frames per second or more, with no hitch longer than 100 ms, on the reference
  low-end device (spec 002 SC-005).
- **SC-006**: 0 new art, font or audio asset files are added.
- **SC-007**: No screen shows a cut-off or overlapping element, and every touch target keeps its minimum size, on three
  aspect ratios from 16:9 to 21:9.
- **SC-008**: A first-time player still finds and taps PLAY within 3 seconds of Home appearing, in 9 of 10 attempts.
- **SC-009**: The same tap sequences end in the same rules state as before the feature (0 differences in the playtest
  replay check).

## Assumptions

- **No screenshots.** Colony Flow's store pages could not be viewed from the session where this spec was written: the
  network policy blocks them. The style follows the general conventions of casual puzzle games of its kind. The product
  owner may share screenshots during clarification to steer the details.
- **No font files.** "Programmatically" means no asset files. The game keeps the system bold font. A rounded display
  font would strengthen the look, and stays in the asset inventory's typography entry for later.
- **Same builds.** The Unity client and the full playtest APK get the style; the tester stays minimal. This follows the
  spec 002 decision (spec 002 Clarifications).
- **Layout unchanged.** Spec 002's frames still define the layout, order and states. This feature changes only the
  drawing style and the motion. The spec 002 review (SC-001) is about layout, so it is not affected.
- **Board faces.** Tiles keep variant symbols, never faces (spec 001 FR-005, spec 002 FR-011).
- **Header bands.** The colored header band on cards is still "a title at the top" (spec 002 FR-007), so it is not a
  deviation.
- **Existing effects.** Sparkles and the other effects use the existing placeholder effects, with no new effect
  assets.

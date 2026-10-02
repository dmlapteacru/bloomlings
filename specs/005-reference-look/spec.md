# Feature Specification: Reference Look — the owner's design reference in every element

**Feature Branch**: `005-reference-look`

**Created**: 2026-10-02

**Status**: Draft

**Input**: User description (owner, 2026-10-02, with the design reference `reference.jpg`): "Это идеал дизайна, к
которому мы хотим прийти. Проанализируй каждый пиксель, каждую кнопку, попап, лэйаут и заимплементи все. 3д модели
героев добавятся позже, я их сделаю. Если есть бэкграунд картинки тоже сделаются потом. Сделаешь список картинок что
надо сделать. Остальное сделай сам." And: "Отсюда надо взять то как выглядят кнопки, лэйаут, геймплей, именно
визуально, не то как располагаются элементы, то как элементы расположены у нас и так хорошо, но визуально как выглядят
элементы надо сделать из дизайна; особенно то как выглядит борд; так же скрины прохождения уровня (празднование);
кнопки все; фичи, айтемы; весь визуал!"

## Context

The owner's reference (`reference.jpg`, 1536 × 1024) shows five phone screens and four strips:

1. **Home**: a 3D diorama of the four families around a lotus fountain in a garden with stone arches, the wooden
   "Bloomlings" logo with leaves, a wooden "Level 88" plaque and a big green PLAY button in a wooden rim. A round cream
   settings button and a Petals pill (pink lotus, amount, green "+") sit on top.
2. **Gameplay**: a wooden "Level 88" sign with ivy between a cream Pause button and a cream "2x ▶▶" button. The board
   is a picture made of saturated candy tiles, each with a small embossed symbol. A stone border frames it on a lawn
   with flowers, and little Bloomlings walk around it and out of a stone arch. Below: a parchment tray with five cream
   Waiting Slots (a variant tile and its count below it, the empty slot dashed), four cream booster tiles with dark
   green count badges, and the Source Tray of wooden framed pods (variant tile, count) with stacked frames behind.
3. **Win**: a wooden "Level Complete!" sign with white flowers and leaves, the finished picture, a celebrating 3D
   Bloom on a stone pedestal with light rays and petals, a "+50" reward pill and a green "Next Level" button in a
   wooden rim.
4. **Jam**: a parchment card "No More Space!" with a subtitle, an inset row of the slot contents, a 2 × 2 grid of
   booster buttons (green Extra Slot and Shuffle, blue Return and Bloom Burst, each with an icon on top and a cream
   cost pill below: lotus and price, or ▶ Free), a cream "Restart Level" button and a cream round close button.
5. **Wardrobe**: a back arrow, a wooden "Wardrobe" banner, the Petals pill, a 3D Sprig on a stone pedestal with ‹ ›
   arrows, a parchment name card (name tab, title, description), four family tabs with small hero heads, outfit cards
   (picture, name; the worn one green with a check) and a footer line.

The strips show the four 3D heroes, the eight variant tiles (saturated rounded squares with a symbol), the UI elements
(PLAY, pressed, orange Next, round Pause and 2x, a booster tile, a source pod with a wooden frame and a handle, the
dashed Waiting Slot) and the environment mood (soft light, natural materials, friendly shapes, gentle animation,
premium casual).

The owner's instruction separates two things:
- **What to take from the reference:** how every element looks (buttons, board, tiles, pods, slots, boosters, popups,
  the celebration, signs, cards, pills, badges, icons, materials).
- **What to keep:** our layouts, the position and order of elements on every screen (spec 002 frames), and every rule.

The owner makes the 3D hero models and the background pictures later. This feature delivers everything else in code
and lists the pictures the owner should make (`pictures.md`).

## Clarifications

### Session 2026-10-02

The owner asked for no further questions. These decisions were taken from the reference and recorded in
`research.md`; each one can be revised by the owner.

- Q: Do pods and Waiting Slots keep the spec 004 characters? → A: No. As in the reference, they show the variant's
  tile (a saturated rounded square with its symbol) with the plain count below it. The 2D characters stay as the
  walking Bloomlings and on the Bloomlings sheet; the 3D heroes stay on the meta screens. This replaces spec 004 FR-008,
  FR-009 and FR-012 (research D3).
- Q: Which colors? → A: The reference's saturated "candy" colors. The variant catalog's colors change accordingly,
  keeping every pair readable under the three simulated color vision deficiencies (spec 001 FR-005, research D1).
- Q: The reference draws Water and Dew with the same drop and Wood and Acorn with the same acorn. → A: We keep a
  distinct symbol per variant (spec 001 FR-005: hue alone never carries meaning).
- Q: The reference's booster row shows a trowel and a pinwheel, but its jam card shows Extra Slot, Shuffle, Return and
  Bloom Burst. → A: We use the jam card's four icons everywhere (they are our four boosters).
- Q: The reference writes "PLAY" and "LEVEL 88" in capitals. → A: Labels stay in sentence case (spec 003 FR-025).

### Session 2026-10-02 (owner review of the first result)

The owner compared the first result with the reference ("но оно очень сильно отличается") and decided:
- Q: Do the layouts stay ours? → A: No. Win, Home and gameplay MUST also take the reference's **layout**, not only the
  look of the elements: "лэйаут в win, home, в геймплее". This replaces the "layouts stay" part of FR-002 for these
  screens (FR-020 to FR-025).
- Q: Where does the jam card go? → A: In the middle of the screen, as in the reference ("jam по середине экрана").
- Q: Is the gameplay colorful enough? → A: No ("не такое красочное все"): the board, the tray and the background must
  be as rich as the reference.
- Q: Item and booster icons, board icons? → A: They differ from the reference. The owner will supply pictures of the
  leaves and of the booster icons ("картинки я тебе дам, листочков, иконки бафов"); everything else — every button,
  the layouts, the board and its icons, the boxes the boosters sit in — is ours to build in code (FR-026, FR-027).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - The board and the tray look like the reference (Priority: P1)

A player opens a level. The board is a picture of saturated candy tiles, each with a small embossed symbol, framed by a
stone border on a lawn. A wooden sign shows the level between cream round buttons. The tray below is parchment: cream
Waiting Slots, cream booster tiles with green count badges, and wooden framed pods holding a variant tile and its count.

**Why this priority**: the owner named the board first ("особенно то как выглядит борд"), and gameplay is what the
player sees most.

**Independent Test**: render preview frames 7–9 and 12–14 and compare them side by side with the reference's gameplay
screen and UI strip; play a level in the playtest and see every state.

**Acceptance Scenarios**:

1. **Given** a level, **When** it opens, **Then** every target tile is a saturated rounded square in its variant's
   color with a bevel (lighter top, darker lip, thin darker outline, gloss) and its symbol embossed in a darker shade,
   and the tiles nearly touch, like the reference.
2. **Given** the board, **When** it is drawn, **Then** a border of cream stone blocks surrounds the grid on a lawn, and
   the Garden Entry is a small stone arch on its side of the board.
3. **Given** the tray, **When** it is drawn, **Then** each exposed pod is a wooden frame with a cream inner panel, the
   variant tile and the count below it; queued pods are dimmed; locked, mystery and connected pods keep their meaning.
4. **Given** the Waiting Slots, **When** they are drawn, **Then** an empty slot is a cream plate with a dashed inner
   outline, a filled slot shows the variant tile and the count below, and the working, stuck, locked, danger and extra
   states stay distinct.
5. **Given** the booster bar, **When** it is drawn, **Then** each booster is a cream rounded tile with its colored icon
   and a dark green count badge with a white ring, or a cream price pill with the lotus when it has no charges.
6. **Given** the top bar, **When** it is drawn, **Then** Pause and the speed button are cream rounded buttons with brown
   glyphs and the level is a wooden sign with ivy at both ends.

---

### User Story 2 - Buttons, cards and popups look like the reference (Priority: P2)

Every button is a glossy raised face: green for the main action, blue and orange where the reference uses them, cream
for secondary actions and icon buttons. Main buttons sit in a wooden rim. Cards are parchment with a brown outline; the
jam card shows the slot contents and the booster choices as big colored buttons with cost pills.

**Why this priority**: the owner asked for "кнопки все" and the popups.

**Independent Test**: render frames 10, 11, 17–20 and the kit sheet frame and compare with the reference's jam card and
UI strip.

**Acceptance Scenarios**:

1. **Given** any main button (Play, Next, Resume, Continue, Claim), **When** it is drawn, **Then** it is a glossy green
   pill with a lighter top, a darker lip, a highlight band and white letters outlined in dark green, in a wooden rim.
2. **Given** a pressed button, **When** the finger is down, **Then** the face sinks into its lip and darkens, like the
   reference's "Pressed".
3. **Given** the jam, **When** it opens, **Then** a parchment sheet shows "No more space!", the subtitle, an inset row
   of the slot contents (tile and count), one big colored button per recovery choice with its icon on top and a cost
   pill below (lotus and price, ×N charges, or ▶ Free for a rescue), and a cream Restart button.
4. **Given** any card (Pause, Settings, Store, Daily reward, Collection, Leaderboard), **When** it opens, **Then** it is
   parchment with a brown outline, a wooden sign or brown title, and a cream round close button with a brown ✕.
5. **Given** the Petals currency, **When** it is shown, **Then** its symbol is a pink lotus.

---

### User Story 3 - The celebration and the meta screens look like the reference (Priority: P3)

Winning a level shows a wooden "Level complete!" sign with flowers, the finished picture in full color, the
celebrating heroes on a stone pedestal with light rays and falling petals, a reward pill and a green Next button in a
wooden rim. Home shows the wooden logo, a wooden level plaque and the big Play button. The Wardrobe and Store use the
wooden banner, parchment cards, family tabs and outfit cards.

**Why this priority**: the owner named the celebration ("скрины прохождения уровня (празднование)").

**Independent Test**: render frames 1–3, 15, 16 and 24 and compare with the reference's Home, Win and Wardrobe.

**Acceptance Scenarios**:

1. **Given** a won level, **When** the win card appears, **Then** it shows the wooden sign with flower clusters, the
   finished picture as full-color tiles in a stone frame, the heroes on a stone pedestal with rays and petals, the
   reward pill with the lotus, and Next in a wooden rim.
2. **Given** Home, **When** it is drawn, **Then** the logo has wooden letters with leaves, the level is on a wooden
   plaque, and Play is the big green button in a wooden rim.
3. **Given** the Wardrobe (Unity) or the Store's cosmetics (playtest), **When** they are drawn, **Then** they use the
   wooden banner, the parchment name card, family tabs with hero pictures and outfit cards whose worn item is green
   with a check.

---

### User Story 4 - The owner knows which pictures to make (Priority: P4)

The owner gets a list of the pictures that replace drawn stand-ins (3D heroes, backgrounds, logo), with sizes, where
each appears and which asset slot receives it.

**Independent Test**: read `pictures.md`; every listed slot exists in the asset slot registry and the generated
inventory.

**Acceptance Scenarios**:

1. **Given** the list, **When** the owner reads it, **Then** each picture has a name, a size, a format, the screens that
   use it and its slot id.

---

### Edge Cases

- A board of 4 × 4 and a board of 12 × 16 both keep the stone border, the tiles and the walkers readable; very small
  cells drop the gloss and keep the symbol.
- Mystery tiles and pods show "?" on a lilac tile until revealed; layered tiles keep the next-layer peek; keys, locks,
  stones and specials keep their garden objects.
- Bloom Burst targeting keeps its ring on every candidate tile.
- Disabled buttons are greyed with the same shapes.
- Very long translated labels shrink to the style's minimum inside the new shapes.
- The level tester keeps its minimal look.
- A missing generated or owner picture falls back to the drawn stand-in.

## Requirements *(mandatory)*

### Functional Requirements

#### A. Scope

- **FR-001**: The feature restyles every element of both builds that draw the designed screens: the Unity client and
  the full playtest. The level tester keeps its minimal look. It amends the looks (never the states) of spec 003
  FR-006, FR-009 (titles), FR-013, FR-014, FR-015, FR-022, FR-023 and FR-031, and replaces spec 004 FR-008, FR-009
  and FR-012; those specs carry the marks.
- **FR-002**: The feature is presentation only. It MUST NOT change any rule, level, mapping, economy value, unlock,
  reward or tap outcome. Screens keep the spec 002 layouts, except where this spec changes what an element shows
  (FR-010, FR-013) and except the gameplay, jam, win, Home and Wardrobe layouts, which follow the reference
  (FR-020 to FR-025, owner's decision of 2026-10-02).
- **FR-003**: Every color and size MUST come from design tokens (no literal colors in screens), and every drawn
  stand-in MUST be a registered asset slot (CLAUDE.md, spec 002 FR-005).
- **FR-004**: Gameplay stays flat 2D (constitution VII). 3D pictures appear only on Home, the win and milestone
  screens, the Wardrobe and the profile.

#### B. Palette and materials

- **FR-005**: The launch variants MUST use the saturated reference palette (research D1). Every pair of variants MUST
  stay a readability candidate (CIEDE2000 ≥ 10 under normal vision and simulated protanopia, deuteranopia and
  tritanopia), checked by the pipeline's `readability` command.
- **FR-006**: The kit MUST provide the reference's materials as engine-free pictures drawn by both builds: light wood
  (signs, rims), dark wood (pod frames), stone (board border, pedestal, arch) and parchment (cards, tray), plus the
  candy tile (FR-010). They MUST be deterministic: the same size and inputs give the same pixels.

#### C. Buttons and controls

- **FR-007**: Buttons MUST be glossy raised faces as in the reference: a face with a lighter top, a darker lip, a
  highlight band, an outline in a darker shade and a soft shadow. Main actions are green with white letters outlined in
  dark green, and sit in a light wood rim. Secondary actions and icon buttons are cream with brown glyphs and letters.
  Jam choices are big green or blue rounded buttons with the icon above the label. The pressed state sinks the face
  into its lip.
- **FR-008**: The level label in gameplay and the titles of the win card, the Wardrobe and the Store are wooden signs
  (light wood with grain, rounded ends, dark brown letters with a light emboss); the gameplay sign and the Wardrobe
  banner carry ivy leaves at both ends, the win sign carries white flower clusters.
- **FR-009**: Count badges are dark green discs with a white ring and white digits; cost pills are cream with a brown
  outline and the pink lotus (or ▶ Free, or ×N charges); the Petals pill is cream with the lotus and a green "+".

#### D. Board and tray

- **FR-010**: Board target tiles MUST be saturated candy tiles: the variant color with a bevel (lighter top band,
  darker lip, thin darker outline, gloss at the top) and the variant symbol embossed in a darker shade with a light
  edge, nearly touching their neighbors. Characters no longer stand on board tiles (replaces spec 004 FR-012). Restored
  ground shows the finished picture as pale flat cells.
- **FR-011**: The board MUST sit inside a border of cream stone blocks on a lawn; each Garden Entry is a stone arch on
  its side of the board; walkers stay the spec 004 2D characters.
- **FR-012**: The tray, slot row and booster bar MUST sit on parchment as in the reference.
- **FR-013**: Pods MUST be wooden frames holding the variant tile with the plain count below it, and Waiting Slots
  cream plates holding the same; empty slots show a dashed inner outline. This replaces the characters of spec 004
  FR-008 and FR-009. All states of spec 002 FR-012 and FR-013 and spec 003 FR-022a stay distinct: queued pods dimmed,
  pressed sunk, locked with a lock, mystery "?", connected with a link, stuck greyed with the hourglass, danger dashed
  red, extra slot with the green "+".
- **FR-014**: Booster tiles MUST be cream rounded tiles with the booster's colored icon (Extra Slot: a white "+" on a
  blue disc; Shuffle: two turning arrows; Return: a yellow back arrow; Bloom Burst: a pink flower) and the badge or
  cost pill of FR-009. Selected and disabled states stay (spec 003 FR-031).

#### E. Popups, celebration and meta

- **FR-015**: Cards and the jam sheet MUST be parchment with a brown outline and a cream round close button with a
  brown ✕. The jam sheet MUST show the reference's content in our layout: title, subtitle, the inset row of slot
  contents, the recovery choices as big colored buttons with cost pills, the free rescue and Restart.
- **FR-016**: The win card MUST show the wooden sign with flowers, the finished picture as full-color tiles in a stone
  frame, the heroes on a stone pedestal with light rays and falling petals, the reward pill and Next in a wooden rim.
  Pause MUST stay usable over it, so Home, Restart and Settings stay reachable as before (FR-002).
- **FR-017**: Home MUST show the wooden logo letters with leaves, the level on a wooden plaque and the big Play button;
  the Wardrobe, Store and the other meta cards use the same signs, parchment, tabs and cards.
- **FR-018**: The Petals symbol MUST be the pink lotus everywhere it appears.

#### F. Pictures from the owner

- **FR-019**: `pictures.md` MUST list every picture the owner makes (3D heroes and poses, backgrounds, logo), each
  with its slot id, size, format and screens. Each listed slot MUST exist in the asset slot registry with the drawn
  stand-in used until the picture arrives.

#### G. Reference layouts (owner's review, 2026-10-02)

- **FR-020**: The gameplay screen MUST follow the reference layout (contracts/look.md §6.1): the top bar; the board in
  its stone border on the lawn, wide and full of color; the lawn strip with the Garden Entry arch below it; then one
  parchment tray to the bottom of the screen holding, in this order, the row of five Waiting Slots, the row of four
  big booster boxes, and the row of Source stacks. This keeps spec 001 FR-068 (board in the center, entry and slots
  below it, the stacked tray with its booster bar at the bottom).
- **FR-021**: Each Source stack MUST be drawn as one big pod in a single row, as a deck: the exposed pod in front with
  its tile and count, and up to two buried pods as wooden frames peeking above it, each showing a band of its variant
  color with its small symbol (identity never by hue alone, spec 001 FR-072); deeper stacks show a "+N" badge. Only the
  exposed pod is selectable (spec 001 FR-011).
- **FR-022**: The jam card MUST be a centered modal card over the dimmed gameplay (contracts/look.md §6.2): the cream
  round close button over its top-right corner when the rules allow closing, the title, the subtitle, the well with the
  slot contents, the choices as a two-column grid of big colored buttons with cost pills below them, and the Restart
  button.
- **FR-023**: The win screen MUST be the reference's full-screen celebration (contracts/look.md §6.3), not a card: the
  wooden sign with flowers at the top, the finished picture large in its stone frame, the celebrating hero (or the
  group) on a stone pedestal overlapping the picture's foot with rays and petals, the reward pill on the pedestal,
  and the big Next button in its wood rim at the bottom. The gameplay top bar is not shown on it.
- **FR-024**: Home MUST follow the reference layout (contracts/look.md §6.4): settings at the top left, the Petals pill
  at the top right, the logo across the top, the diorama (the owner's Home picture, or the heroes on a pedestal with
  the lotus fountain) in the middle, the wooden level plaque, and the big Play button below it. Our other Home entries
  (Wardrobe, Collection, Daily Challenge, rank, milestone teaser, free booster) stay reachable as small cream round
  buttons and pills along the sides and the bottom.
- **FR-025**: The Wardrobe MUST follow the reference layout (contracts/look.md §6.5) in both builds; the playtest gets
  a Wardrobe screen (equipping through the shared `WardrobeService`) instead of only the Store's cosmetics tab.
- **FR-026**: Board tile icons MUST be the reference's "gem" icons: the variant symbol about 56% of the tile with a
  thick dark outline, a glossy fill in a shade of the tile color and a highlight (contracts/look.md §3.1.2).
- **FR-027**: The booster icons and the leaf decorations (sign ivy, win-sign flowers, button corner leaves, logo
  leaves) MUST be replaceable by the owner's pictures (pictures.md D): when a picture file exists, both builds draw it
  instead of the drawn icon or leaves.

### Key Entities

- **Material picture**: an engine-free RGBA picture of a material (wood, stone, parchment, candy tile) rendered at a
  pixel size by the kit and cached by each build.
- **Tile look**: a variant's candy tile in one of two styles: board (small, embossed symbol) and sticker (pods, slots,
  jam row: a detailed symbol with a light edge).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Side by side with the reference, a reviewer recognizes every listed element (board, tiles, stone border,
  slots, boosters, pods, top bar, buttons, jam card, win card, Home, Wardrobe/Store) in the preview frames.
- **SC-002**: All 66 variant pairs stay readability candidates (minimum CIEDE2000 ≥ 10 under the three simulated
  deficiencies).
- **SC-003**: Every existing test suite passes (core, client check, backend, playtest check, preview checks, art
  check), and the preview's touch-target and safe-area checks pass for every frame.
- **SC-004**: No screen file uses a literal color or size, and every new stand-in is a registered asset slot that
  appears in the regenerated asset inventory.
- **SC-005**: `pictures.md` lists every owner picture with its slot.

## Assumptions

- The owner's later 3D heroes and backgrounds will be delivered as PNG files at the listed sizes, and the builds will
  show them through the existing picture slots.
- The jam keeps our recovery choices (spec 001); the reference's Shuffle button in the jam card is shown only if the
  rules offer Shuffle there.
- The playtest has no Wardrobe screen; its Store cosmetics tab takes the Wardrobe look.

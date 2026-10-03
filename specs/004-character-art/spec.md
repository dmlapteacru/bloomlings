# Feature Specification: Character Art — cartoon 2D Bloomlings in play, 3D heroes on Home

**Feature Branch**: `004-character-art`

**Created**: 2026-10-01

**Status**: Draft

**Input**: User description: "Character art for Bloomlings (owner's decisions of 2026-10-01, from two reference images
and the concept sheets): (1) Gameplay stays 2D but cartoony and pretty: every target variant is a character whose whole
shape is the variant symbol, with a face […] no separate badge, so the face is never confused with a symbol. Moods:
happy, asleep while queued in the tray, worried when stuck in a Waiting Slot. They appear in pods (with an "xN" count),
Waiting Slots, walkers and on the board. (2) Board (owner chose option 2): board cells are light tiles with the
variant's character standing on each […] (3) Home, the win screen and other meta screens (Wardrobe, profile) show the
same four families as beautiful 3D-rendered heroes […] (4) The art is produced by generator scripts kept in the
repository […] (5) It replaces the code-drawn kawaii Bloomlings of spec 003 FR-032 and spec 003 SC-006's "no new art
files"."

## Context

The owner reviewed a series of character sheets in the session of 2026-10-01:
- six 2D styles;
- the code-drawn kawaii figures of spec 003 FR-032, which the owner rejected after seeing them in the game;
- several 3D-rendered styles;
- two reference images of their own.

The first reference image is a win screen with four 3D-rendered heroes on a stone pedestal: a green bean wrapped in a
leaf, a pink flower with a face, a glossy water drop with little arms, and a wooden stump. The second is a gameplay
screen. In it, every character's whole shape is its variant (a leaf-drop, a flower, a drop, a stump) with a simple face.
The characters stand on light board tiles, fill the pods with an "xN" count, and sit in the Waiting Slots.

The concept sheets of that review are kept here:
- `concept-gameplay-2d.jpg`: the eight 2D characters, their moods, and the tray, slots and both board options;
- `concept-home-3d.jpg`: the 3D heroes on Home and on the win screen.

The owner then decided:
- **Gameplay:** 2D, cartoony and pretty, never fancy 3D.
- **Home and the meta screens:** the same families as beautiful 3D heroes.
- **Board:** option 2, light tiles with a character on each, as in the reference.
- **Process:** a new spec (this one).

## Clarifications

### Session 2026-10-01

- Q: Which character look in gameplay? → A: 2D, as in the owner's second reference. Each variant is a character whose
  whole shape is the variant's symbol, with a face. There is no separate badge, so a symbol is never mistaken for a
  mouth at small sizes. This was the problem the owner saw with the badge of spec 003 FR-032.
- Q: What does the board show? → A: Option 2 of the concept sheet: light tiles with the variant's character on each,
  as in the reference. Option 1 (colored tiles with a white silhouette) was not chosen.
- Q: 3D? → A: Yes, on Home, the win screen and the other meta screens, as pre-rendered pictures in the style of the
  owner's first reference. Gameplay stays 2D. The owner approves this as an exception to doc 12 §1 ("flat 2D, no 3D").
- Q: How is the art made? → A: By generator tools kept in the repository. They write picture files that are recorded
  as the project's own art. Both builds use them.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Every variant is its own cartoon character in the tray and the slots (Priority: P1)

A player opens a level and sees the Source Tray. Each pod holds a character whose whole shape tells the variant: a
green leaf-drop with a sprout leaf, a pink five-petal flower, a blue drop, a wooden stump, and so on. Each has a small
friendly face. The count reads "x6" in the pod's corner. Pods still waiting in their stack are muted, and their
characters are asleep. The player taps a pod, and its character moves to a Waiting Slot. While it can work it is
happy. When it is stuck, it is greyed and worried. Its Bloomlings walk to their tiles as small copies of the same
character.

**Why this priority**: The tray and the slots are where the player decides on every tap. The owner's main complaint
was there: the badge on the old figures looked like a mouth. Fixing it here delivers the visible value on its own.

**Independent Test**: Play levels 1–12 in the full playtest. Every pod and slot shows the variant's character with no
badge, with the "xN" count and the right mood. The variant can be named at the smallest pod size.

**Acceptance Scenarios**:

1. **Given** a level with Leaf, Flower, Water and Wood pods, **When** the tray is shown, **Then** each pod shows that
   variant's character (leaf-drop, flower, drop, stump) in its color, with a face and no separate symbol, and its count
   as "xN".
2. **Given** a stack of three pods, **When** the tray is shown, **Then** the exposed pod's character is awake and
   bright, and the two queued below are muted and asleep. Each shape still tells its variant.
3. **Given** a pod in a Waiting Slot with reachable work, **When** its Bloomlings work, **Then** its character is happy.
   **When** it has no reachable work, **Then** the slot greys and the character looks worried, with the hourglass
   state of spec 002 FR-013.
4. **Given** two variants of one family (Leaf and Moss, or Water and Dew), **When** they are side by side in the tray,
   **Then** their shapes differ, not just their colors: a leaf-drop vs a moss cushion, a pointed drop vs a round
   dewdrop.
5. **Given** a locked, mystery or connected pod, **When** it is shown, **Then** it keeps its spec 002 FR-012 state
   (padlock, "?", link) as before.

---

### User Story 2 - A board of characters on light tiles (Priority: P2)

The board shows light tiles. On each target tile stands the character of that cell's variant, as in the owner's
reference. Each tile is lightly tinted toward its variant's color, so the level's picture still reads at a glance. When
a Bloomling clears a tile, the character leaves, and the finished picture shows beneath, as before.

**Why this priority**: The owner chose this board look. It makes the board match the characters in the tray. It comes
after US1 because the tray characters are reused on the board.

**Independent Test**: Preview the gameplay frames and play a level. Every target tile shows its character on a light
tinted tile. The picture is recognizable, cleared cells show the finished picture, and stones, keys, hidden layers,
mystery tiles and specials still read.

**Acceptance Scenarios**:

1. **Given** a level start, **When** the board is shown, **Then** every target tile is a light tile tinted toward its
   variant's color, with that variant's character on it, fully inside its cell.
2. **Given** the board of a picture level, **When** a player looks at it for the first time, **Then** the subject reads
   from the arrangement of tints and characters (spec 001 FR-006).
3. **Given** a tile being cleared, **When** its Bloomling arrives, **Then** the character leaves the tile and the
   finished picture shows in that cell. The open cell looks clearly different from an active tile (spec 001 FR-007).
4. **Given** stones, keys, a hidden layer peek, a mystery tile or a special, **When** they are on the board, **Then**
   they look as before and are never confused with a character tile.

---

### User Story 3 - 3D heroes on Home, the win screen and the meta screens (Priority: P3)

A player opens Home and sees the four families as beautifully lit 3D heroes on a stone pedestal in warm garden light:
- Sprig: a bean wrapped in a big leaf;
- Bloom: a flower with a face;
- Drop: a glossy water drop with little arms;
- Twig: a wooden stump with a twig.

After a win, the same heroes celebrate above the reward. The Wardrobe and the profile show the families as single 3D
heroes, still wearing the player's cosmetics.

**Why this priority**: It makes the first and the last screens of every session feel premium. It is independent of
gameplay.

**Independent Test**: Open Home early and later, win a level, open the Wardrobe and the profile. Each shows the 3D
heroes, the screens keep their layout and buttons, and a worn hat or skin still shows on the hero.

**Acceptance Scenarios**:

1. **Given** Home, **When** it opens, **Then** the hero area shows the four families as 3D heroes on a stone pedestal.
   Level N, PLAY and the other Home elements keep their places (spec 002 frames 2–3).
2. **Given** a won level, **When** the win screen shows, **Then** the 3D heroes celebrate above the reward, and the
   Next button and the reward work as before.
3. **Given** a worn hat, skin or expression, **When** the Home hero, the Wardrobe or the profile shows a family,
   **Then** the cosmetic shows on the 3D hero (FR-063 of spec 001).
4. **Given** gameplay, **When** a level is played, **Then** no 3D picture appears in the level. The level stays 2D.

---

### Edge Cases

- **Smallest sizes.** The largest board (14×16 cells) on the narrowest supported phone gives the smallest character.
  So do the queued pods. The shape alone must still tell the variant, and the face must never look like a symbol.
- **Same-family pairs.** Leaf/Moss, Flower/Violet Bud, Water/Dew and Wood/Acorn must differ in shape as well as in
  color (constitution II).
- **Grayscale and colorblindness.** In grayscale, every pair of launch variants must still differ by shape.
- **Expansion variants.** Vine, Berry, Mist and Bark need characters too, even before levels use them.
- **Muted and stuck looks.** The muted queued look and the grey stuck look must keep the shape readable.
- **Cosmetics.** Outfits are worn by walkers and the Home hero (spec 001 FR-063). A worn expression must not draw a
  second face over the drawn one.
- **Hidden information.** Mystery pods and mystery tiles keep showing "?" only, never a character, until revealed
  (spec 001 FR-013).
- **Board fit.** A character must never spill into a neighboring cell or make a cell look partly occupied
  (constitution II).
- **Missing art.** If a character picture fails to load, the game must still show a readable variant: its color and
  symbol in the spec 002 look. It must never show an empty cell.
- **App size and memory.** The pictures must not make the app noticeably bigger or slower to start.
- **Level tester.** It keeps its minimal look and does not need the new art.

## Requirements *(mandatory)*

### Functional Requirements

#### A. Scope

- **FR-001**: This feature replaces the drawn Bloomling figures in both builds: the Unity client and the full playtest.
  This includes the code-drawn kawaii figures of spec 003 FR-032. The level tester keeps its minimal look.
- **FR-002**: The feature is presentation only. It MUST NOT change:
  - any rule, level, mapping, economy value, unlock level or reward;
  - the outcome of any tap sequence;
  - screen layouts and element order (spec 002 frames), except where FR-008 and FR-012 change what a pod and a board
    tile show.
- **FR-003**: Gameplay MUST stay 2D. 3D-rendered pictures MAY appear only on Home, the win and milestone screens, the
  Wardrobe and the profile (FR-017).

#### B. The gameplay characters

- **FR-004**: Every target variant MUST have its own character, whose whole shape is the variant's symbol:

  | Variant | Family | Character |
  |---|---|---|
  | Leaf | Sprig | a leaf-drop with a sprout leaf |
  | Moss | Sprig | a puffy moss cushion |
  | Flower | Bloom | a five-petal flower |
  | Violet Bud | Bloom | a closed tulip bud with green sepals |
  | Water | Drop | a pointed water drop |
  | Dew | Drop | a round dewdrop with a sparkle |
  | Wood | Twig | a stump with rings on top |
  | Acorn | Twig | an acorn with a cap |
  | Vine (expansion) | Sprig | a curling vine sprout |
  | Berry (expansion) | Bloom | a round berry cluster with a leafy calyx |
  | Mist (expansion) | Drop | a soft puffy cloud |
  | Bark (expansion) | Twig | a bark chunk with grooves |

  Within a family, the two variants MUST differ in shape, not only in color. The shape becomes the variant's icon
  (spec 001 FR-005, FR-072).
- **FR-005**: Each character MUST be drawn in one consistent 2D cartoon style, as in the owner's second reference:
  - a fill in the variant color with a soft light-to-dark gradient;
  - an outline in a darker shade of the same color, never black;
  - a soft highlight;
  - a simple face: dot eyes with a tiny highlight, a small mouth and pink blush.

  Dark variant colors MAY be drawn a little lighter so the face reads, keeping the hue.
- **FR-006**: A character MUST NOT carry a separate badge or symbol. Its shape is the symbol, so a symbol can never be
  read as a mouth.
- **FR-007**: Each character MUST have these moods:

  | Mood | Look | Where |
  |---|---|---|
  | happy | open eyes, a smile | exposed pods, working slots, walkers, board tiles |
  | asleep | closed eyes, muted colors | pods still queued in their stack (spec 003 FR-022a) |
  | worried | open eyes, a small frown, greyed colors | a stuck pod in a Waiting Slot (spec 002 FR-013) |
  | blank | no face | a worn cosmetic expression draws the face (FR-016) |

#### C. Pods, slots and walkers

- **FR-008** *(replaced by spec 005 FR-013: a pod is a wooden frame holding its variant's candy tile with the plain
  count below it; kept here as history)*: A pod MUST show its variant's character, large and centered, and the remaining count as "xN" in the
  corner. The count MUST reach 3:1 contrast against the pod (spec 001 FR-072). This replaces the count pill and the
  symbol of spec 003 FR-022.

  The pod keeps every state of spec 002 FR-012 and spec 003 FR-022a: exposed, queued (muted, asleep), pressed,
  locked, mystery and connected. Spec 001 FR-012's order of prominence becomes: the character (icon and color in one),
  then the count, then the family. The icon and the color are carried by the same shape.
- **FR-009** *(replaced by spec 005 FR-013: a Waiting Slot is a cream plate holding the variant's candy tile and the
  plain count; kept here as history)*: A Waiting Slot holding a pod MUST show the same character and the "xN" count, with the working and stuck
  states of spec 002 FR-013 (happy vs worried and greyed, with the hourglass).
- **FR-010**: Walking Bloomlings MUST be small copies of their variant's character, happy, with a soft ground shadow.
  They wear their family's outfit (spec 001 FR-063).
- **FR-011**: Mystery pods and mystery tiles MUST keep showing "?" and never a character until they are revealed (spec
  001 FR-013).

#### D. The board

- **FR-012** *(replaced by spec 005 FR-010: every target tile is a saturated candy tile with its variant symbol
  embossed, and no character stands on it; kept here as history)*: Every target tile MUST be a light tile, lightly tinted toward its variant's color, with that variant's
  character (happy) standing on it, fully inside its cell. This replaces:
  - the colored tiles with symbols of spec 002 FR-011;
  - spec 001 FR-005's "tiles show simple target symbols, never character faces";
  - doc 12 §7 "no hero faces in target tiles".

  This is the owner's decision (Clarifications).
- **FR-013** *(amended by spec 005 FR-010: the candy tiles' colors follow the mapping; no character stands on a
  tile)*: The board MUST still read as the level's picture (spec 001 FR-006). The tints and the characters'
  colors MUST follow the role-to-variant mapping, so that the subject is recognizable from the first second.
- **FR-014** *(amended by spec 005 FR-010: the candy tile shrinks away instead of a character leaving)*: Clearing MUST
  keep the restoration reveal of spec 001 FR-007. The character leaves, the finished picture
  shows in the cell, and open cells stay clearly distinct from active tiles. Stones, keys, hidden-layer peeks, mystery
  tiles and specials MUST keep their spec 002 and spec 003 looks.
- **FR-015**: Every cell MUST stay unambiguous (constitution II). A character MUST NOT overlap a neighboring cell or
  hide a cell's state.

#### E. 3D heroes on the meta screens

- **FR-016**: Each family MUST have a 3D hero in the style of the owner's first reference:

  | Family | Hero |
  |---|---|
  | Sprig | a bean wrapped in a big leaf |
  | Bloom | a flower with a face in its center |
  | Drop | a glossy water drop with little arms |
  | Twig | a wooden stump with a twig |

  The heroes are pre-rendered pictures with warm garden light. They MUST show worn cosmetics (hat, skin, trail,
  expression) as the current figures do (spec 001 FR-063). A worn expression replaces the drawn face.
- **FR-017**: Each meta screen MUST use the 3D heroes as follows:
  - **Home:** the four families together on a stone pedestal in the hero area. Later, when the player has a hero,
    their family's hero in its outfit is shown. (Spec 005 FR-024 and FR-028: over the owner's layered Home picture,
    Home and the splash show the four owner heroes animated, pre-rendered from the owner's FBX models.)
  - **Win and milestone screens:** the heroes celebrating above the reward.
  - **Wardrobe and profile:** single heroes.

  The layouts, buttons and texts of these screens stay as in specs 002 and 003.
- **FR-018**: The 3D heroes MUST NOT appear inside a level (FR-003).

#### F. Making the art

- **FR-019**: All character art MUST be produced by generator tools kept in the repository. Rebuilding from the same
  tools MUST give the same pictures, so any change is a reviewed change to the tools. The pictures MUST be recorded
  as the project's own art in the originality record (constitution I, spec 001 FR-091).
- **FR-020**: Every character picture MUST be a registered asset slot (spec 002 FR-002, asset inventory), so final art
  from an artist can replace it one to one.
- **FR-021**: If a picture is missing or fails to load, the game MUST fall back to a readable variant look (spec 002
  color and symbol) and log it. It MUST never show an empty cell or pod.

#### G. Readability and review

- **FR-022**: The spec 001 readability tests MUST pass with the new characters for every pair of launch variants:
  - grayscale/icon;
  - small size;
  - color distance;
  - pod;
  - slot;
  - moving character.

  The shape alone MUST tell the variant in grayscale.
- **FR-023**: A review sheet MUST show:
  - every character in every mood;
  - the tray, the slots and the board in a level;
  - the 3D heroes on Home and on the win screen.

  The sheet is used for the owner's approval.

### Key Entities

- **Variant character**: the 2D character of one target variant: its shape, colors, and its pictures for each mood.
- **Character mood**: happy, asleep, worried or blank, and where each is used.
- **3D hero**: the pre-rendered 3D picture of one family, alone or with the others on the pedestal, with a blank-face
  version for worn expressions.
- **Art generator**: the repository tools that draw the 2D characters and render the 3D heroes. They write the
  pictures, and each picture is tied to an asset slot.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The owner accepts the 2D characters, the board and the 3D heroes on the review sheet and on a phone.
- **SC-002**: In 10 first-time players, at least 9 name the variant of a pod from its character at the smallest pod
  size within 2 seconds. None reads a character's face as a variant symbol.
- **SC-003**: In grayscale, every pair of the 8 launch variants is told apart by shape alone in 100% of checks at board
  and pod size.
- **SC-004**: At least 8 of 10 players recognize the subject of a picture level from the board at level start.
- **SC-005**: Counts reach at least 3:1 contrast on every pod and slot.
- **SC-006**: Gameplay stays at 30 frames per second or more, with no hitch longer than 100 ms, on the reference
  low-end device (spec 002 SC-005). Home shows its heroes within 1 second of opening.
- **SC-007**: The new art adds at most 6 MB to the installed app.
- **SC-008**: Rebuilding the art from the repository tools gives identical pictures. The same tap sequences end in the
  same rules state as before the feature (0 differences in the playtest replay check).

## Assumptions

- **Tile tint.** The owner chose light tiles with characters (option 2). This spec tints each tile lightly toward its
  variant's color instead of using one plain cream, so the board keeps reading as the level's picture (spec 001 FR-006,
  constitution I "picture-first levels"). The owner can ask for plain cream tiles.
- **"xN" count.** The count is written "xN" in the pod's corner, as in the reference, instead of the dark pill.
- **Four 3D heroes.** One 3D hero per family, as the owner described ("the same four families"). There are no
  per-variant 3D heroes. Gameplay already shows each variant as its 2D character.
- **Expansion variants.** Their 2D characters are made now with the launch set, as lower priority. Their 3D look is not
  needed, because 3D heroes are per family.
- **Constitution VII.** Amended to v1.0.2 on 2026-10-01 with the owner's approval. What the player plays and
  navigates stays flat 2D, with no 3D scene, camera or perspective view. Pre-rendered 3D illustrations may appear as
  flat pictures on meta screens only, never inside a level (FR-003, FR-017, FR-018).
- **Doc 12.** Doc 12 §1 (flat 2D) and §7 (no faces on target tiles) change for this feature, by the owner's decisions
  above. The locked document itself changes only through the owner.
- **Own art.** The tools and their output are the project's own work. The owner's reference images are a style
  direction only. They are not shipped or copied, and no Colony Flow material is used (spec 001 FR-091).
- **Spec 003.** This spec replaces spec 003 FR-032 (code-drawn kawaii figures, belly badge) and SC-006 ("no new art
  files"). The rest of the Garden look stays.
- **Readability evidence.** SC-002 to SC-004 need people and devices. They are tracked in the playtest checklists of
  spec 001, like the earlier readability sign-off.

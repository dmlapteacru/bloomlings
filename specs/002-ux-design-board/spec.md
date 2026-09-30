# Feature Specification: UX Design Board — the game's visual design without art assets

**Feature Branch**: `002-ux-design-board`

**Created**: 2026-09-30

**Status**: Draft

**Input**: User description: "использовать изображение как референс для дизайна игры. заимплементировать дизайн на сколько
возможно без использования ассетов пока что. после, надо идентифицировать какие ассеты необходимы (список)."
(Use the attached image as the reference for the game's design. Implement the design as far as possible without art
assets for now. Afterwards, identify which assets are needed, as a list.)

**Reference**: [`ux-design-board.webp`](ux-design-board.webp), the "Mobile Game UX Design Board" with 17 numbered
frames:

| # | Frame | # | Frame |
|---|---|---|---|
| 1 | Splash | 10 | Jam (bottom sheet) |
| 2 | Home (early levels) | 11 | Pause menu |
| 3 | Home (progressed) | 12 | Pod states |
| 4 | Daily Reward (popup) | 13 | Waiting slot states |
| 5 | Leaderboard | 14 | Booster bar (progressive unlock) |
| 6 | Collection | 15 | Win screen |
| 7 | Gameplay (normal) | 16 | Milestone win |
| 8 | Gameplay (hard) | 17 | Store |
| 9 | Gameplay (super hard) | | |

This feature changes how the game looks and how its screens are arranged. It adds no gameplay: the rules, content,
economy values and unlock roadmap stay those of spec 001 (`specs/001-core-game-mvp/spec.md`).

## Clarifications

*None yet. Two questions are open under Requirements (FR-003 and FR-011).*

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Play a level in the board's visual style (Priority: P1)

A player opens a level and sees the gameplay screen of frames 7–9:
- a top bar with a round Pause button, a "LEVEL N" pill with a HARD (red) or SUPER HARD (purple) badge under it when
  the level has that class, and a round 2× button;
- the board over a soft garden backdrop;
- the row of Waiting Slots;
- the Source Tray;
- the booster bar at the bottom.

Pods, slots and boosters use the board's shapes, colors and states (frames 12–14). Everything stays readable: each
variant keeps its own color and symbol, and every state shows by shape or symbol as well as color.

**Why this priority**: The gameplay screen is where players spend almost all their time. Its look sets the game's
first impression and carries the readability rules.

**Independent Test**: Open a Normal, a Hard and a Super Hard level. Compare each with frames 7, 8 and 9: the top bar,
the board area, the slots, the tray and the booster bar are in the same order and style. Put a pod into each state of
frames 12 and 13, and pass each booster unlock level of frame 14. Check every state against its frame.

**Acceptance Scenarios**:

1. **Given** a Normal level, **When** it opens, **Then**:
   - the top bar shows the Pause button, the "LEVEL N" pill and the 2× button, as in frame 7;
   - below the board come the five slots, then the tray, then the booster bar.
2. **Given** a Hard or Super Hard level, **When** it opens, **Then** a HARD or SUPER HARD badge sits under the level
   pill, in the color and wording of frames 8 and 9.
3. **Given** pods in the tray, **Then** each state looks like frame 12:
   - the exposed pod is bright and raised;
   - the next pod in a stack is greyed;
   - a tapped pod shows a pressed look for that moment;
   - a locked pod shows a padlock;
   - a mystery pod shows "?" and its count;
   - connected pods are joined by a link.
4. **Given** the slots, **Then** each state looks like frame 13:
   - an empty slot is a soft empty tile;
   - a working pod is bright;
   - a stuck (waiting) pod is greyed;
   - a locked slot shows a padlock;
   - the last free usable slot is outlined in a red dashed "danger" frame with the "4/5" idea.
5. **Given** a player below Level 3, **Then** the booster bar is hidden. From each booster's unlock level (spec 001
   roadmap: L3 Extra Slot, L4 Shuffle, L6 Return, L9 Bloom Burst) its round button appears with a count badge, in
   frame 14's order.
6. **Given** a Super Hard level with obstacles, **Then** stones, specials and locked cells use the board's garden-object
   look (frame 9) and never hide the tile state beneath.

---

### User Story 2 - Move through the game's flow screens in the board's style (Priority: P2)

A player launches the game, sees the splash (frame 1), then:
- on the first launch, goes straight into Level 1;
- later, reaches Home, in the early look (frame 2) or the progressed look (frame 3).

They play, pause (frame 11), may jam and see the jam bottom sheet (frame 10), and win (frame 15). On a milestone level
they also see the milestone screen (frame 16).

**Why this priority**: These screens frame every session: start, stop, fail and success. They must feel as calm and
clear as the board shows.

**Independent Test**: Launch fresh, then again after a few wins. Pause, jam and win a level, and reach a milestone
level (fast-forward). Compare each screen with its frame.

**Acceptance Scenarios**:

1. **Given** the app starts, **Then** a splash with the game's wordmark over the garden backdrop shows while the game
   loads (frame 1). It never adds a tap before play.
2. **Given** an early player (before the long-run features unlock), **Then** Home shows only:
   - the Petals pill;
   - the Settings button;
   - "LEVEL N";
   - the large green PLAY button (frame 2).
3. **Given** a progressed player, **Then** Home also shows (frame 3):
   - a Bloomling hero;
   - the Wardrobe button;
   - "N levels to reward" with a gift;
   - the rank row "Rank #N >";
   - the Daily Challenge card with its reward.

   Each shows only once its feature is unlocked.
4. **Given** the player pauses, **Then** the pause card shows:
   - RESUME (primary);
   - RESTART;
   - SETTINGS;
   - HOME;
   - a close button (frame 11).
5. **Given** the slots jam, **Then** a bottom sheet rises (frame 10) showing:
   - "NO MOVES LEFT" and "Use a booster to continue";
   - the usable recovery boosters, each with its cost;
   - the Free rescue offer when one is available;
   - Restart.
6. **Given** a win, **Then** the finished picture is revealed, followed by:
   - the Petals earned;
   - NEXT (primary);
   - the optional "×2 reward" (frame 15).
7. **Given** a milestone win, **Then** a milestone card shows (frame 16):
   - "LEVEL N — Milestone reached!";
   - each reward as an icon with its amount (for example, a cosmetic, Petals, booster charges);
   - CONTINUE.

---

### User Story 3 - Use the meta screens in the board's style (Priority: P3)

A player claims the Daily Reward (frame 4), checks the Leaderboard (frame 5), browses the Collection (frame 6) and buys
from the Store (frame 17). Each is a rounded card or popup with a close button, a bold title and the board's rows,
tiles and buttons.

**Why this priority**: These screens support long-run motivation but are visited less often than play.

**Independent Test**: Unlock each feature (fast-forward), open it and compare it with its frame.

**Acceptance Scenarios**:

1. **Given** a Daily Reward is due, **Then** the popup shows (frame 4):
   - "Daily Rewards";
   - "Day N";
   - a reward picture with "+N Petals";
   - CLAIM;
   - the optional ad bonus "Get +N".
2. **Given** the Leaderboard, **Then** it lists (frame 5):
   - the top ranks with gold, silver and bronze medals;
   - a gap marker;
   - the player's neighbours, with the player's row highlighted as "You";
   - offline, the last known list with a notice.
3. **Given** the Collection, **Then** finished pictures show as framed tiles in a grid (frame 6). Opening one shows it
   larger with its name and "Completed at Level N". The Collection never selects a level.
4. **Given** the Store, **Then** it shows (frame 17):
   - the Petals balance pill with "+";
   - rows with an icon, a name and a price with the Petal symbol.

   The spec 001 Store contents stay: Petal packs, boosters, Remove Ads, the starter pack and cosmetics after L40.

---

### User Story 4 - Know which art assets are needed (Priority: P2)

The product owner receives a complete list of the art and audio assets needed to replace the placeholder visuals, so
art production can be planned and ordered.

**Why this priority**: The design is built from placeholders first. The asset list turns the board into a production
plan, and the user asked for it explicitly.

**Independent Test**: Take any placeholder visual in the game and find its entry in the list. Take any list entry and
find where the game uses it.

**Acceptance Scenarios**:

1. **Given** the implemented placeholder design, **When** the asset list is delivered, **Then** every placeholder has
   an entry. Each entry states:
   - its category;
   - where it is used (screen and frame number);
   - its states or variants;
   - its size class;
   - its priority for launch.
2. **Given** an entry, **Then** it says whether the asset must stay readable at small sizes (variant icons, pod and
   slot art) and whether it must pass the readability checks of spec 001.

### Edge Cases

- Tall and short phones (from 16:9 to 20:9 and above) keep every frame's element order, with no element cut off or
  overlapping the board.
- Large numbers stay legible and grouped as on the board ("1 240"):
  - Petals in the thousands;
  - level numbers up to 5000;
  - ranks in the hundreds of thousands.
- A level with 6 variants, a 6th slot (Extra Slot) and up to 6 stacks keeps the board's spacing without shrinking
  counts below readable size.
- Longer translated text (for example, German) fits buttons, pills and badges or shrinks legibly. It never
  overflows its shape.
- Offline:
  - the Leaderboard shows its last known data with a notice;
  - the Store marks real-money items unavailable;
  - the Daily Challenge card hides if its content is missing.
- A player who has not unlocked a feature never sees its button, card or badge.
- Colorblind players: every state and every variant stays distinguishable without color (symbols, shapes, outlines).

## Requirements *(mandatory)*

### Functional Requirements

#### A. Scope and principles

- **FR-001**: The game's screens, components and states MUST follow the 17 frames of the design board, in layout,
  element order, visual hierarchy, shapes, colors and wording. Where the board and spec 001 disagree, spec 001 wins and
  the deviation MUST be recorded (see Assumptions).
- **FR-002**: The design MUST be built without art or audio asset files for now. Every visual is drawn from shapes,
  gradients, outlines, text and the existing synthesized sounds. Each placeholder MUST be replaceable later by an asset
  without changing layout or behavior.
- **FR-003**: The design MUST apply to [NEEDS CLARIFICATION: which builds get the new design — the Unity game client
  only; the Unity client and the full playtest APK (the level tester APK stays as it is); or all three builds?].
- **FR-004**: The design MUST NOT change any rule, level, economy value, unlock level or reward of spec 001, and MUST
  NOT add gameplay. Numbers shown on the board (for example "+200 Petals" at Level 100, Return at Level 5) are
  illustrative; the game shows its real values.

#### B. Visual language (all frames)

- **FR-005**: The game MUST use one consistent visual language taken from the board:
  - a light, warm palette (cream panels, soft greens, sky blues, blossom pinks);
  - rounded cards and tiles with soft shadows;
  - bold, rounded, uppercase titles;
  - green primary buttons with a darker lower edge;
  - cream secondary buttons;
  - round icon buttons (Settings, Pause, 2×, close);
  - pill counters (Petals with "+", Level).
- **FR-006**: Currency and rewards MUST use one Petal symbol everywhere: Home, Store, rewards, costs and badges.
- **FR-007**: Popups (Daily Reward, Leaderboard, Collection, Pause, Store, Milestone) MUST share one card style: a
  title at the top, a round close button (where closing is allowed), and a dimmed backdrop. The jam screen MUST be a
  bottom sheet (frame 10).
- **FR-008**: Screen backgrounds MUST suggest the garden setting of the board (sky, greenery, soft distance) with
  placeholders until art exists. Backgrounds MUST follow the level band's theme (spec 001 FR-066) and MUST stay light
  enough that the board keeps its contrast.

#### C. Gameplay screen (frames 7–9, 12–14)

- **FR-009**: The gameplay screen MUST be ordered as in frame 7:
  - the top bar (round Pause, the "LEVEL N" pill, round 2×);
  - the board;
  - the Waiting Slots row;
  - the Source Tray;
  - the booster bar at the bottom.

  This satisfies spec 001 FR-068.
- **FR-010**: Hard and Super Hard levels MUST show a small badge under the level pill: "HARD" in red, "SUPER HARD" in
  purple (frames 8, 9). Normal levels show none.
- **FR-011**: Board tiles MUST keep the picture-first mosaic of spec 001 FR-006, in the board's rounded tile style.
  Tiles MUST show [NEEDS CLARIFICATION: the design board draws some tiles with Bloomling faces, while spec 001 FR-005
  and the locked docs 11 §6 and 12 §7 require simple variant symbols and never character faces on tiles — keep
  symbols (faces only on pods, slots and walkers), or change FR-005 to allow faces on tiles?].
- **FR-012**: Pods MUST show the frame 12 states:
  - **exposed:** bright and raised;
  - **next in stack:** greyed, smaller;
  - **pressed:** momentary press look;
  - **locked:** padlock over the pod;
  - **mystery:** "?" with its count;
  - **connected:** a visible link between the members.

  Each pod MUST keep spec 001 FR-012's order of prominence: variant symbol, variant color, count, family silhouette.
- **FR-013**: Slots MUST show the frame 13 states:
  - **empty:** a soft tile;
  - **working:** the pod bright;
  - **stuck/waiting:** the pod greyed;
  - **locked:** a padlock;
  - **danger:** the last free usable slot in a red dashed frame.

  The 6th slot from Extra Slot MUST read as an added slot.
- **FR-014**: The booster bar MUST follow frame 14:
  - it is hidden before the first booster unlocks;
  - each booster appears at its spec 001 unlock level as a round icon button;
  - a count badge shows the booster's charges;
  - with no charge left, the button MUST still show the booster's Petal price, so buying stays clear (spec 001 FR-048).
- **FR-015**: Stones, keys, locks, layered tiles, mystery tiles and specials (Gate, Fountain, Chest, Statue, Bridge)
  MUST use placeholder garden-object shapes in the board's style (frame 9), and MUST never cover the tile's symbol or
  count.

#### D. Flow screens (frames 1–3, 10, 11, 15, 16)

- **FR-016**: A splash (frame 1) MUST show the Bloomlings wordmark over the garden backdrop while the game loads. It
  MUST NOT require a tap. The first launch then continues straight into Level 1 (spec 001 US2).
- **FR-017**: Home MUST follow frame 2 for early players and frame 3 once the long-run features unlock.
  - **Frame 2, always shown:**
    - the Petals pill with "+" to the Store;
    - Settings;
    - "LEVEL N";
    - the large PLAY button.
  - **Frame 3, each once unlocked:**
    - a Bloomling hero;
    - the Wardrobe button;
    - the milestone teaser "N levels to reward" with a gift;
    - the rank row;
    - the Daily Challenge card with "New today" and its reward.

  Collection and other buttons MUST keep the same style.
- **FR-018**: The pause card MUST offer RESUME (primary), RESTART, SETTINGS and HOME, with a close button (frame 11).
- **FR-019**: The jam bottom sheet MUST show (frame 10):
  - the title "NO MOVES LEFT" with "Use a booster to continue";
  - the usable recovery boosters with their costs;
  - the Free rescue button when a rescue is offered;
  - Restart.

  It follows spec 001 FR-027.
- **FR-020**: The win screen MUST follow frame 15:
  - the finished picture;
  - the Petals earned with the Petal symbol;
  - NEXT as the primary button;
  - the optional "×2 reward" rewarded-ad button.

  The win sequence of spec 001 FR-025 stays: reveal, then reward, then Next.
- **FR-021**: A milestone win MUST add the milestone card of frame 16 after the win: "LEVEL N", "Milestone reached!",
  one icon with an amount per reward (cosmetic, Petals, booster charges), and CONTINUE.

#### E. Meta screens (frames 4, 5, 6, 17)

- **FR-022**: The Daily Reward popup MUST follow frame 4: "Daily Rewards", "Day N", a reward picture, "+N" with the
  Petal symbol, CLAIM, and the optional ad bonus "Get +N".
- **FR-023**: The Leaderboard MUST follow frame 5:
  - the top three with gold, silver and bronze medals;
  - avatar placeholders and names;
  - each row's score (the highest completed level);
  - a gap marker;
  - the player's neighbourhood with the player's own row highlighted as "You".
- **FR-024**: The Collection MUST follow frame 6: a grid of framed finished pictures and a detail view with the name
  and "Completed at Level N". It is never a level selector (spec 001 FR-065).
- **FR-025**: The Store MUST follow frame 17: the Petals balance pill, and one row per item with an icon, a name and a
  price with the Petal symbol (or the real-money price). It keeps the tabs and contents of spec 001.

#### F. Readability, accessibility and feel

- **FR-026**: The new look MUST keep every readability rule of spec 001 (FR-005, FR-070, FR-072):
  - each variant identified by color and symbol;
  - counts readable;
  - symbols and counts with at least 3:1 contrast on their background;
  - every state shown by shape or symbol, never by color alone.
- **FR-027**: Buttons and interactive tiles MUST be at least the platform's minimum touch size. Primary actions (PLAY,
  NEXT, CONTINUE, CLAIM, RESUME) MUST be reachable one-handed in the lower part of the screen.
- **FR-028**: Motion MUST stay calm and short, as the board suggests: presses, pops and sheet slides. The 2× speed
  still changes only animation speed (spec 001 FR-069).

#### G. Asset inventory (User Story 4)

- **FR-029**: After the placeholder design is implemented, the project MUST produce an asset inventory. The inventory
  MUST list every art and audio asset needed to replace the placeholders, grouped at least into these categories:
  - brand (wordmark, app icon, splash illustration);
  - backgrounds per theme and screen;
  - Bloomling characters (four families, hero poses, the animation set of doc 12 §2, variant accents);
  - variant symbols (8 launch, 4 expansion);
  - board tiles and overlays (tile base, layer peek, mystery, stone, key, lock, open ground);
  - specials (Gate, Fountain, Chest, Statue, Bridge);
  - pods and slots in every state;
  - booster icons;
  - UI kit (buttons, pills, badges, cards, popup and bottom-sheet frames, close, tabs, toggles, medals, avatars, ad
    icon, gift, checkmarks);
  - currency and reward art (Petal symbol, reward basket, milestone rewards);
  - Collection frames;
  - cosmetics (skins, hats, trails, expressions, frames, badges, markers);
  - visual effects (sparkles, petal bursts, confetti, droplets, shuffle swirl, burst);
  - typography (display and body fonts);
  - audio (music per theme and every sound cue).
- **FR-030**: Each inventory entry MUST state:
  - what it is;
  - where it is used (screen and board frame);
  - its states or variants;
  - its size class;
  - whether it must pass the readability checks;
  - its launch priority (needed for launch, or can follow).

### Key Entities *(include if feature involves data)*

- **Design frame**: one of the 17 numbered frames of the board, the reference a screen, component or state is
  compared with.
- **Visual token**: a named color, corner radius, shadow, type style or spacing used across frames. It keeps the
  language consistent and lets final art replace placeholders in one place.
- **Placeholder**: a visual drawn without an asset, standing in for a future asset. It has the same size and position
  the asset will have.
- **Asset inventory entry**: a needed asset, with its category, use, states or variants, size class, readability duty
  and launch priority. It is linked to the placeholders it replaces.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Each of the 17 board frames has a counterpart in the game (screen, popup, component or state), or a
  recorded deviation with its reason. In a side-by-side review the product owner accepts at least 15 of the 17 as
  matching in layout, element order and hierarchy.
- **SC-002**: The game runs its full flow (splash, Home, play, pause, jam, win, milestone, Daily Reward, Leaderboard,
  Collection, Store) with no art or audio asset file added by this feature.
- **SC-003**: Every placeholder visual in the game has an entry in the asset inventory, and every entry points to at
  least one place in the game (100% both ways).
- **SC-004**: The readability checks of spec 001 still pass on the new look:
  - symbols and counts reach at least 3:1 contrast on every variant color;
  - all 8 launch variants stay distinguishable in grayscale and in simulated protanopia, deuteranopia and tritanopia;
  - in 10 first-time players, at least 9 tell apart 5 variants on one board within 5 seconds.
- **SC-005**: Gameplay stays smooth with the new look on the reference low-end device: at least 30 frames per second,
  and no hitch longer than 100 ms during a level (spec 001 SC-008).
- **SC-006**: A first-time player finds and taps PLAY within 3 seconds of Home appearing, in 9 of 10 attempts.
- **SC-007**: No screen shows a cut-off or overlapping element on phones from 16:9 to 21:9, checked on at least three
  aspect ratios.

## Assumptions

- The design board is the visual reference, not a rule change:
  - its numbers are illustrative ("+12" per win, "+200" at Level 100, a rank of 1 284, prices of 40/40/50/60);
  - its Return at Level 5 is illustrative too: spec 001 keeps Return at Level 6.
- The board's header line "Match and merge Bloomlings" is marketing wording. There is no merge mechanic; the core loop
  of spec 001 stays.
- The board is still a picture-first mosaic (spec 001 FR-006). The board's tile look is applied to picture tiles; the
  garden backdrop frames the board and never replaces the picture.
- Leaderboard names and avatars are placeholders until the server side is built. Offline, the last known list shows
  with a notice.
- The splash is shown while the game loads. It is not an extra screen to tap through.
- Placeholders use the synthesized sounds already in the game until audio assets exist.
- The asset inventory is delivered after the placeholder implementation, as its own document next to this spec.
- The level tester APK keeps its minimal look unless FR-003 decides otherwise.

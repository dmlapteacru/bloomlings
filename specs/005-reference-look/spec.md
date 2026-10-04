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

### Session 2026-10-02 (the owner's delivery: layered Home and animated heroes)

The owner sent the Home picture in layers and the four heroes as animated models (translated from Russian): "I'm
sending the assets for the Home screen, split into several layers. I'm also sending you the heroes (the files are named
a little differently, but you'll understand). They must be placed the same way as in the reference. The heroes have
animations embedded. Use them. […] Also for the win screen, use the animations too, when the hero appears there."
With it came an updated reference (`4.webp`: Home with Sprig at the left, Bloom behind and above the lotus, Drop at the
right back, Twig at the right front; the win with one hero on the pedestal) and an idle animation guide (`3.webp`).
- Q: What was delivered? → A: `bloomlings_home_assets.zip`, the Home picture in five layers (the garden, the
  fountain's back, the fountain's front, a sheet of soft shadows, drifting petals) with a README giving their order,
  and four rigged FBX models made with Meshy AI, one per family, each with its animation clips
  (`tools/heroanim/SOURCE.md` maps the files to the families).
- Q: Which animations? → A: the owner's table:

  | Hero | Constant | Reaction A | Duration |
  |---|---|---|---|
  | Sprig | breathing + sway | curious head tilt | Idle 4 s / Tilt 2–2.5 s |
  | Bloom | breathing + soft sway | happy bounce | Idle 4 s / Bounce 2 s |
  | Drop | breathing + soft body sway | soft buoyant bounce | Idle 4 s / Bounce 2 s |
  | Twig | breathing + sway | head tilt + tiny bounce | Idle 4 s / Reaction 2 s |

- Q: Where do the heroes stand? → A: "the same way as in the reference": on the painted fountain, Sprig at the left,
  Bloom behind the lotus, Drop at the right back, Twig at the right front. This lifts FR-024's deferral (FR-028).
- Q: And the win? → A: "the win too": the level's hero plays its reaction as it appears, then idles. The milestone
  screen shows the same hero, so it does the same. (Since 2026-10-03 the celebrating hero is Twig, the
  clarification "Twig celebrates" below, and Twig and Sprig by turns since Sprig's model, "Sprig's Blender model".)
- Q: A 3D model in the game? → A: No (constitution VII; decision in research D18). The models never enter the game:
  `tools/heroanim` renders them offline into flat frame pictures, shown on meta screens only.
- Q: When does a hero react on Home? → A: Decided here (research D18): they take turns, one every 6 s (Bloom first,
  1.5 s after Home opens), each at its idle's seam, and a tap on a hero makes it react at once.
- The updated reference differs from `reference.jpg` in four more places that this delivery does not cover; they are
  open with the owner (`pictures.md` H).

### Session 2026-10-03 (the owner's gameplay rule: pods one after another)

The owner reviewed the tray's decks and wrote: "Карточки выбора должны идти друг за другом, а не друг на друге, вне
зависимости от дизайна игры, это правило геймплея. Так чтоб было видно 3-4 ряда. Подстрой размеры боксов." (The choice
cards must go one after another, not on top of each other, whatever the game's design: it is a gameplay rule. So that
3-4 rows are visible. Adjust the boxes' sizes.)
- Q: Which cards? → A: The Spirit Pods of the Source Tray. The deck of FR-021 (the exposed pod in front, the next ones
  as frames peeking above it) drew a stack's pods on each other. Each stack MUST now be a column: the exposed pod in the
  top row and the next pods under it, each fully visible, never overlapping, as spec 003 FR-022a had it. The rule holds
  whatever the look, so a later restyle MUST keep it (FR-021 amended).
- Q: How many rows? → A: "3-4": four when the safe area is at least 1.95 times as tall as it is wide (19.5:9 phones
  and taller), three on shorter screens (16:9, and 18:9 with a status bar). Then the board keeps at least a third of
  the safe height on every phone (research D20).
- Q: Which boxes change? → A: The pods become wider than tall, with the tile at the left and the count at the right.
  The Waiting Slots, the booster boxes and the lines between the rows shrink so four rows fit (FR-021, contracts/look.md
  §6.1).
- Q: Does a rule change? → A: No. Only the exposed pod takes taps (spec 001 FR-011). The waiting pods show what spec
  001 FR-013 allows: the variant and the count, or "?" and the count for a mystery pod. This is presentation only
  (FR-002).

### Session 2026-10-03 (the owner's choice of the pod's look: "E")

The owner reviewed the columns and wrote: "Цифры можно не такие большие; фокус на иконку, цифры можно в углу где-то или
предложи варианты. Сгенерируй варианты, я выберу." (The digits need not be so big; the focus on the icon, the digits
can go in a corner somewhere, or suggest options. Generate options, I will choose.) Six mock-ups of the real tray were
rendered (research D21); the owner answered "E".
- Q: Which look? → A: "E": the owner's icon of the variant alone, larger, over the middle of the tinted panel (no candy
  tile under it); the count in small dark digits with a white outline at the panel's bottom right corner; the frame
  narrowed to 1.3 times its height and centered in its column. The "+N" disc moves to the top left corner, away from
  the count, and a connected pod's link ring to the top right (FR-013, FR-021, contracts/look.md §3.7).
- Q: What about pods without the icon? → A: A hidden mystery pod keeps its "?" tile and a locked pod its padlock, both
  centered; a variant whose owner picture is missing shows its sticker tile there.
- Q: Does a rule change? → A: No. The columns, rows, taps and touch boxes of the previous session stay; presentation
  only (FR-002).

### Session 2026-10-03 (Twig celebrates; Twig's new model)

The owner sent a new Twig and wrote: "Replace the twig fbx with this one, and let it be Twig in the celebration after
the round."
- Q: Who celebrates a won level? → A: Twig, on every win and on the milestone that follows it (`CharacterArt.Celebrant`),
  instead of the family of the level's main variant (FR-028, pictures.md A7). The group still stands in while Twig's
  frames and picture are missing.
- Q: Is the new Twig in? → A: Yes, from its third file. The first (`twig.fbx` from Blender 4.2, its own rig, the clips
  `Twig_Breathing` 3 s, `Twig_SmallBounce` 1.5 s and `Twig_WinCheer` 3 s with an eyelid shape key) carried no colors,
  the second (`Twig_v2.fbx`) named its textures without embedding them; the owner's `Twig.glb` embeds them and replaces
  the Meshy Twig (research D23).
- Q: Which clips, how long? → A: The file's own: `Twig_Breathing` is Twig's idle (3 s) and `Twig_SmallBounce` its
  reaction (1.5 s) on Home; `Twig_WinCheer` (3 s, a jump with the arms spread) is its celebration on the win and the
  milestone, then it breathes (`MotionClip.Win`, `HeroMotionPlayer.Celebrate`). A hero without its own cheer reacts
  there instead.
- Q: Twig looks dark, "as if in shadow"? → A: Its brown texture under the shared light came out darker than the
  others (mean brightness 0.46 against about 0.62). Of six variants the owner chose "H": an even light and a lighter,
  warmer grade of its texture, Twig only (research D23).

### Session 2026-10-04 (the owner's bottom menu, wooden)

The owner, with `bloomlings_bottom_nav_icons_clean.zip` (five icons) and a picture of five bar styles: "We also need to
add a bottom menu. You will find the icons in the zip. On the picture you will find variants. Try the wooden variant."
- Q: Which variant? → A: The wooden one, the picture's second row: a wide warm brown wooden plank across the bottom of
  the screen with wood grain and rounded ends, green vine curls with small white flowers around both ends, thin
  vertical grooves between the five places, the icons on the plank, and the active place in a raised round wooden
  medallion (a lighter wood disc in a darker rim, with vines and two small white flowers) rising over the plank's top.
  Drawn from the kit's wood and leaves (`UiRaster.NavBar`, `NavMedallion`), the owner's five icons on it (pictures.md
  D9–D13), in both builds (FR-030, contracts/look.md §6.7).
- Q: Which places, in which order? → A: Shop, Wardrobe, Home, Leaderboard, Collection. A place shows only once its
  feature is unlocked (a player never sees the button of a locked feature): the Shop from L12, the Wardrobe from L40,
  the Leaderboard from L10, the Collection once a picture is won; Home always. The shown places keep their order and
  share the bar's width evenly, so early on Home stands alone. (Changed the same day by the owner's last question
  below: all five places always show, a locked one with a padlock.)
- Q: Which place is raised, and where does a tap go? → A: The screen's own place: Home on Home, the Shop on the Store
  page, the Wardrobe on the Wardrobe; a tap on it does nothing. The Shop opens the Store page, the Wardrobe the
  Wardrobe, Home returns to Home, and the Leaderboard and the Collection open their cards over Home (from the Store page
  or the Wardrobe, Home first). The click sounds as on every button. (Changed the same day by the owner's last question
  below: the Leaderboard and the Collection open their own pages, and every place raises its medallion on its page.)
- Q: Where does it show? → A: On Home, the Store page and the Wardrobe, always (with all five places since the
  question "always visible" below; Home alone early on before it; and on the Leaderboard and Collection pages since the
  last question); not in
  gameplay, on the win, milestone, jam or pause cards, nor on the splash.
- Q: What leaves Home? → A: The buttons that now do the same thing, so nothing is doubled (the owner had disliked two
  Wardrobe buttons): the Store side button, the Wardrobe button (the profile avatar with its shirt badge; the avatar
  stays in the Wardrobe's profile tab), the Collection side button and the rank pill in the top row (the Leaderboard
  place opens what it opened). Settings, the Petals pill (its "+" still opens the Store), the Daily Challenge's side
  button and the rest stay. Unity's Home demos of the Leaderboard, the Store and the Wardrobe point at the menu's places.
- Q: How does Home make room? → A: The plaque, Play and the teaser row move up so Play and the teaser row end above
  the medallion's top with a small gap; Play keeps the reference's height unless the plaque would rise above 60% of the
  height (on 16:9 phones), then it is a little shorter, so the heroes on the fountain stay in view. The Store page's
  list and the Wardrobe's cards and footer end above the menu too (on 21:9 the Store's cosmetics show three rows of
  cards instead of four). The playtest's dev row (−1, +1, +10, Reset), which lay in that band, moves into the Settings
  card opened from Home.
- Q (the owner's review on the phone, the same day): "Remove the branches to the right and left of the menu itself. And
  make the menu icons bigger: they must take more of the menu's plank, and the free room on the plank must be
  minimal." → A: The plank loses the vines at its ends (the medallion keeps its own leaves and flowers, which lie on
  it, not beside the menu). The plank grows from `0.12W` to `0.14W`, the places share its whole length (`0.04W` to
  `0.96W` instead of `0.12W` to `0.88W`), and each icon is the plank's full height, so the owner's pictures nearly
  touch its top and bottom (their own margin is about 4% a side): 151 px instead of 111 px on a 1080 px wide phone.
  The medallion grows to `0.2W` with its icon 163 px, a little larger than the plank's, and rises `0.03W` instead of
  `0.05W`, so the menu's top stays `0.17W` above the safe bottom and Home, the Store page and the Wardrobe keep their
  layout.
- Q (the owner, the same day): "The menu's places must always be visible. But if some things are available only from a
  level, then on entering the menu's place the page must say that it is only available after reaching level N." → A:
  The five places always show, in their order and sharing the plank as five, from Level 1 (`BottomNav.Order`; which
  are open is `BottomNav.IsOpen`, as the places showed before: Home always, the Shop from L12, the Wardrobe from L40,
  the Leaderboard from L10, the Collection with its first picture). A locked place keeps the owner's icon, unchanged
  and still tappable, with a small padlock badge at its lower right (the outfit cards' cream badge with the brown
  padlock, 0.34 of the icon, inside the plank's band; the raised place never has one). A tap on it opens its page or
  card as usual, which then shows the locked notice instead of its content: the place's icon large with the padlock
  badge, "Available from level N" in brown title letters and "Keep playing to unlock it!" under it (contracts/look.md
  §6.7). The Store page keeps its garden, header and parchment panel, the notice in place of the tabs and rows; the
  Wardrobe keeps its garden and header, the page's lighter panel holding the notice in place of the hero, the name
  card, the tabs, the cards and the footer; the Leaderboard and Collection cards keep their title and close button,
  the notice in place of the ranks or the pictures (pages since the next question: their panel holds the notice as
  the Store page's does). N comes from each build's own roadmap (`UnlockRoadmap.LevelOf`:
  12, 40 and 10), and the Collection's is 2 (its first picture comes with Level 1's win). A locked Store page is not a
  Store visit: no `store_open`. Rules, the economy and unlocks stay as they are (FR-002).
- Q (the owner, the same day, translated from Russian): "All the menu's places must be a separate page. Not popups."
  → A: The Leaderboard and the Collection become full-screen pages like the Store page and the Wardrobe, in both
  builds (FR-030, contracts/look.md §6.8 and §6.9), on the Store page's frame so the four pages line up: the
  Wardrobe's garden, the page header (the back button, the wooden "Leaderboard" or "Collection" banner with ivy, the
  Petals pill, its "+" opening the Store page once it is open), the parchment panel and the bottom menu with their
  place raised in the medallion. No card opens over Home anymore. The Leaderboard page holds what its card showed:
  the rank rows (as many lines as the data has and the page fits: eight on every phone from 16:9, ten on 19.5:9), the
  player's own row highlighted, the offline or empty line and Refresh. The Collection page holds the count and the
  framed finished pictures, newest first, three to a row and as large as fit, a page at a time between the page arrows
  (twelve a page on 16:9, fifteen on 19.5:9 and 21:9 since the owner's choice below); a tap on a picture shows
  its detail on the page (the picture large, its name and its level), the back button and the system back return to
  the grid and a second back leaves the page; it is never a level selector. From any page a tap on another place goes
  straight to that place's page (or Home); back and the Android system back return to Home (the Store page opened from
  a page returns to that page). Locked, each page shows the locked notice in its panel, as the locked Store page does.
  The data, the rules and the analytics events the cards sent on open (`leaderboard_view`, `collection_open`) stay
  (FR-002).
- Q: Home lost its rank pill to the menu, but spec 001 FR-058 and `product/08` §2 want the leaderboard rank on Home
  after L10. Bring it back, or a rank badge on the menu's Leaderboard place? → A: Neither (the owner, the same day):
  "The rank on Home, no. Only on the Leaderboard page." The rank shows only on the Leaderboard page; spec 001 FR-058 is
  amended with the owner's decision.
- Q: On tall phones the pages leave room: under the Leaderboard's "You" row and over the Collection's page arrows.
  Leaderboard: A, the status line and Refresh right under the rows, or B, the rows filling the panel; Collection: A, as
  many rows as fit with the arrows right under them, or B, the same rows of larger frames? → A: The Leaderboard B, the
  Collection A (the owner, after the preview of 16:9, 19.5:9 and 21:9). The Leaderboard's rows fill the panel: the
  playtest's offline page shows as many placeholder ranks as fit, Unity reads five players above and below the player
  and the top eleven. The Collection shows as many rows as fit, its frames a little smaller (down to 0.84 of their
  full size) when one more row then fits, the page arrows right under the last row: twelve a page on 16:9, fifteen on
  19.5:9 and 21:9 (contracts/look.md §6.8, §6.9).

### Session 2026-10-04 (the owner's notes: the Wardrobe's header on one line; the Store as a page)

The owner: "In the Wardrobe, the screen's header: the elements there are not on one line. They need aligning." and
"The Store must be a separate page, not a popup." and "On Home there are two Wardrobe buttons; keep one, the one with
the hero's icon, but in its bottom right corner put the clothes icon on that round chip."
- Q: Home's Wardrobe button? → A: The profile avatar (the hero's portrait) is Home's one Wardrobe button, the left
  column's first; the shirt button is gone. A green shirt badge sits at the avatar's bottom right (`Kit.IconBadge` /
  `UiKit.IconBadge`: the count badge's disc with a white `ui.shirt`, 0.34 of the avatar), and the profile badge moves to
  its bottom left. Both builds open the Wardrobe from it (Unity: its default page; the Profile tab is inside;
  contracts/look.md §6.4).
- Q: Which line? → A: The back button's middle: the back button, the wooden banner and the Petals pill now share it
  (the banner sat about 0.04 W lower before, 46 px on a 1080 × 2340 phone). The banner's plank is `0.1W` tall, so with
  its ivy it stands about as tall as the back button, and it spans the room between the back button and the Petals
  pill's box less its ivy clusters' reach, so the leaves touch neither on any phone from 16:9 to 21:9; "Wardrobe" keeps
  about 95% of its title size. One kit header (`ScreenLayout.PageHeader`) serves the Wardrobe and the Store page in both
  builds (FR-025, FR-029; contracts/look.md §6.5). Unity's ivy clusters now sit where the playtest's do (both from the
  kit's `GardenLook.IvyBox`).
- Q: What does the Store page hold? → A: Everything the popup offered, on a full-screen page like the Wardrobe
  (FR-029, contracts/look.md §6.6): the Wardrobe's garden, the page header with the "Store" banner, then a parchment
  panel to the bottom of the screen with the Shop / Cosmetics tabs, the Shop's rows (boosters for Petals; the starter
  pack, the booster bundle and Remove Ads, "Unavailable" while purchases are off), larger and filling the page, and the
  cosmetics' outfit cards, as many rows of three as the page holds, with the page arrows when more remain. Prices, the
  economy and every tap's outcome stay (FR-002).
- Q: Where does its back go? → A: Where the Store was opened: Home (its Store button, its Petals "+"), or the Wardrobe
  (its Petals "+"). In the playtest the Android back closes the page, and the Wardrobe, as their back buttons do (it
  left the app before); elsewhere it does what the system does.

### Session 2026-10-04 (the owner's Settings switch for Home's falling petals)

The owner: "Add a button to Settings to remove the petals from Home."
- Q: Which petals, and how? → A: Home's falling petals (the owner's petals layer drifting over the layered Home,
  `bg.home.petals`, and the drawn stand-in's falling petals, `Kit.FallingPetals`), not the Petals currency. Settings gets
  a fifth switch, "Falling petals: On/Off" (`settings.petals`), in both builds, on by default; off, Home shows no
  petals, at once, also when Settings is open over it. The splash keeps them (it fades into Home), and so do the win and
  the milestone. It is saved with the other settings: the save's settings gain an optional `homePetals` (default true,
  so older saves show the petals; spec 001's `player-save.schema.json` and data model, the owner's request). Presentation
  only (FR-002).

### Session 2026-10-04 (the owner's Home header)

The owner (translated from Russian): "Home's header must be like this: `[Settings]     [ Petals 5090 + ]     [Avatar]`.
The Avatar will lead to a separate profile page, we will do that later." The owner's picture shows one row at the top
of Home: at the left the cream round Settings button with the gear; in the middle a large Petals pill (the pink lotus,
the grouped amount "5 090" and the round green "+") with a pink flower and leaves on its top-left corner and another on
its bottom-right corner; at the right a round avatar as large as Settings, a character portrait in a light round frame
with a small flower on it.
- Q: Where does each stand? → A: On one row, on Settings' middle line, from 2.5% of H (FR-024, contracts/look.md
  §6.4, `ReferenceHomeRegions`): Settings `0.13W` at `0.04W` from the left, as before; the Avatar its mirror, `0.13W`
  at `0.04W` from the safe right edge (`Avatar`); between them the Petals pill's box, larger, `0.44W × 0.105W` (it was
  `0.38W × 0.095W` at `0.02W` from the right), centered on the safe area's middle, `0.11W` clear of each. The pill fits
  its amount as before (§3.4, `PetalsPillParts`) and stands centered in its box with its "+" (`align` 0.5). The logo,
  the Daily Challenge (now under the Avatar) and everything below stay where they were; the logo picture's letters still
  start under the row.
- Q: The flowers on the pill? → A: The Play button's leaves and flower (the owner's sprig, pictures.md D7,
  `ui.deco.garden`; `Kit.Decoration` / `UiKit.PetalsPill(decorate: true)`): one on the pill's rounded left end at its
  top, one turned half way on the "+"'s edge at its bottom right (on the pill's right end while the Store is locked and
  the "+" is hidden). They are larger for the pill's height than on a main button (1.2 of it, the bottom-right one 0.9
  of that; `GardenLook.PillDecorationBoxes`) so the flower reads at the pill's size, and never a touch target. The "+"
  still opens the Store page once the Store is open. The pages' Petals pills (the page header) stay as they are.
- Q: What does the Avatar show, and what does a tap do? → A: The profile avatar as Home showed it before the bottom menu
  (FR-061): the player's hero (Bloom, `CharacterArt.ProfileHero`, Unity's `ProfileAvatar.HeroFamily`; in its outfit once
  the Wardrobe is open) on a domed cream disc with a soft green middle, in the chosen profile frame with the profile
  badge at its bottom left; without the shirt badge, since the Wardrobe is the bottom menu's place. It shows from Level
  1 in both builds. A tap presses it like a round button and clicks; until the profile page comes, the playtest then
  says "Profile coming soon" (`home.profile_soon`) in Home's toast, and Unity's Home, which has no toast, only presses
  and clicks (`HomeFeatureActions.OnProfile` is where the profile page will open). The animated heroes' taps stay clear
  of it: the playtest cuts them clear of its touch box, and in Unity its button lies above the stage and keeps its taps.
  Presentation only (FR-002).

### Session 2026-10-04 (the owner's calm backgrounds)

The owner sent `bloomlings_calm_backgrounds.zip` (eight calmer backgrounds: "fewer flowers, less visual noise and
calmer saturation so Bloomlings characters and UI remain the focal point"): "Use them instead of the existing ones."
- Q: Which goes where? → A: By their names, in both builds (the files keep their names, pictures.md B): Home's garden
  `home.jpg` ← `01_home_calm_garden` (through `tools/heroanim/layers.mjs`, under the same fountain layers: its open
  middle takes the fountain and the heroes), the win `win.jpg` ← `02_win_calm_garden_glow` (its round stone stage at
  0.60 of the picture's height, `OwnerPictures.WinStageShare`, was 0.58), the Wardrobe and the Store page
  `wardrobe.jpg` ← `08_wardrobe_calm_garden`, and the gameplay themes: daylight ← `04_lotus_courtyard`, pond ←
  `06_lily_pond`, orchard ← `03_orchard`, moonlit ← `07_evening_fireflies`. `05_home_lotus_fountain_calm` (a garden
  with the fountain painted in) is not used: the heroes stand between the fountain's layers.

### Session 2026-10-04 (the owner's saturation note: the backgrounds at 70%)

The owner: "Background 55–65% saturation → UI 65–75% → heroes 100%."
- Q: Against what? → A: Against the animated heroes, the 100% step (the mean HSL saturation of their frames, 0.656).
  Measured that way the owner's backgrounds sat at 86–105% of the heroes' (the evening garden at 58%) and the UI
  pictures at 119–147%. The ladder as asked (backgrounds 60%, the UI pictures and colors 70%) was built and shown;
  the owner: "Bring it back as it was, and then try lowering only the background to 80%, and show." After 80% and
  70% were shown side by side: "We keep 70." So only the backgrounds change: each owner background is scaled to 70%
  of the heroes' mean saturation (the win ×0.66, the Wardrobe and the pages' garden ×0.68, the gameplay themes ×0.71
  to ×0.82, the layered Home as one scene ×0.73; the evening garden stays, it is under 70% already), lightness and hue
  kept (FR-031, contracts/look.md §1.4). The UI, the heroes, the characters and the board stay as they are.

### Session 2026-10-04 (Twig and Sprig toned down)

The owner, on the APK with the new Sprig: "Now you can really see that the chestnut is somehow too bright. It needs
to be reduced. And Sprig too."
- Q: How much? → A: To the other two heroes' level: of three toned-down variants each (A, B, C), "B": Twig's light and
  grade softened (brightness 0.73 → 0.62, saturation 0.73 → 0.59), Sprig's texture graded (0.69 → 0.65, saturation
  0.79 → 0.64), against Bloom's 0.63 and Drop's 0.61 (research D25). Their colors stay; only the glare goes.

### Session 2026-10-03 (Sprig's Blender model; Twig and Sprig take turns celebrating)

The owner sent a Blender Sprig and wrote: "Replace the hero. Add it to the round's celebration, let it take turns with
Twig. Use celebrate/clap there, alternate."
- Q: Is the new Sprig in? → A: Yes, from its third file (`Sprig_Complete.glb`): the first two (the same
  `Sprig_character_Model.glb` twice) held a blank color texture on the rigged mesh, the colors only on an unrigged copy.
  It replaces the Meshy Sprig (research D24).
- Q: Which clips on Home? → A: `Sprig_Breathing` (4 s) its idle and `Sprig_Wave` (3 s) its reaction, at their own
  lengths (proposed on 2026-10-03, not objected to); `Sprig_LookAround`, `Sprig_SmallJump` and `Sprig_ThumbsUp` are not
  used.
- Q: Who celebrates? → A: Twig and Sprig take turns level by level: Twig on odd levels (L1, L3, …), Sprig on even ones,
  on the win and the milestone after it (`CharacterArt.Celebrants`, `CelebrantOf`). Sprig alternates its two
  celebrations on its own turns: `Sprig_Celebrate` (2 s) on L2, L6, …, `Sprig_Clap` (3 s) on L4, L8, …
  (`CharacterArt.CelebrationTurn`, `HeroMotion.WinClip`, `MotionClip.Win2`). Twig keeps its cheer. By the level
  number, so a replayed or restored level shows the same celebrant in both builds.

### Session 2026-10-03 (the owner's 60 fps models)

The owner sent the four heroes again, exported by Meshy at 60 fps ("Animation_all_frame_rate_60"): "I'm giving you new
fbx, the same heroes but 60 fps. Just replace them and build the APK."
- Q: What do the new files hold? → A: The same models and clips, but the clips' frames are stamped 1/60 s apart
  instead of 1/24 s: the idle holds its 96 frames over 1.6 s instead of 4 s, the reactions their 48 over 0.8 s
  (research D22). Taken as stamped, every hero would move 2.5 times faster than the owner's table; there are no
  in-between frames.
- Q: So what changes? → A: The files replace the earlier ones (`tools/heroanim/models/`), each clip keeps the length of
  the owner's table (`heroes.json` `idleSeconds` 4, `reactSeconds` 2), and the bake takes every frame of the motion:
  24 frames a second instead of 12 (96 idle and 48 reaction frames a hero), twice as smooth on screen. 60 frames a
  second of flat pictures would be five times the frames (about 57 MB and 175 MB of palette pictures to cycle), too
  heavy for a phone (research D22).

### Session 2026-10-03 (the owner's batch: no arch, the Petals pill, slower and side by side clearing)

The owner: "The initial board clearing speed must be halved. What is the arch under the board in gameplay? Remove it.
It seems there is a bug with the queue of picked pods: I pick two in a row and they land in the same slot. Sometimes,
even in two slots, they do not seem to work in parallel, as if there were a hidden queue anyway. The currency chip
must be fixed: at 0 it shows crooked, somewhere in the middle, the lotus itself too far left."
- Q: The arch? → A: Removed in both builds (FR-011, contracts/look.md §3 "Garden Entry", §6.1): no picture marks an
  entry; the Bloomlings set off from the stone border beside the entry cell. The arch's room goes to the board, which
  grew by about 15%. **Flagged conflict:** spec 001 puts "the Garden Entry … below the board" (FR-068, the gameplay
  screen) and FR-021 wants a tile choice "players can anticipate" by the distance to an entry; with no marker the
  entry shows only by where the Bloomlings come from. A subtle marker (a lighter border stone, a small path) is the
  owner's call.
- Q: The Petals pill? → A: It fits its amount, the lotus fully inside its left end and the amount right after it, the
  same in both builds (contracts/look.md §3 "Petals pill"); Unity also lays it out again on every change, since a label
  it could not measure yet stayed centered.
- Q: The clearing speed and the queue? → A: Presentation only, the rules were right (spec 001 research R4, amendment
  of 2026-10-03): the clearing pace is halved, and the waves of different taps play side by side, each pod showing in a
  slot free on screen.

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
   no arch marks the Garden Entry (amended on 2026-10-03: the Bloomlings set off from the border beside the entry cell).
3. **Given** the tray, **When** it is drawn, **Then** each exposed pod is a wooden frame with a cream inner panel, the
   variant tile and the count beside it. The next pods of its stack stand under it in its column (three or four rows,
   never overlapping), muted but readable. Locked, mystery and connected pods keep their meaning (FR-021).
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
4. **Given** any card (Pause, Settings, Daily reward), **When** it opens, **Then** it is parchment with a brown
   outline, a wooden sign or brown title, and a cream round close button with a brown ✕. The Store, the Leaderboard
   and the Collection are pages since 2026-10-04 (FR-029, FR-030), not cards.
5. **Given** the Petals currency, **When** it is shown, **Then** its symbol is a pink lotus.

---

### User Story 3 - The celebration and the meta screens look like the reference (Priority: P3)

Winning a level shows a wooden "Level complete!" sign with flowers, the finished picture in full color, the
celebrating heroes on a stone pedestal with light rays and falling petals, a reward pill and a green Next button in a
wooden rim. Home shows the wooden logo, a wooden level plaque and the big Play button. The Wardrobe and Store use the
wooden banner, parchment cards, family tabs and outfit cards; the Leaderboard and Collection pages the same banner on
the Store page's parchment panel.

**Why this priority**: the owner named the celebration ("скрины прохождения уровня (празднование)").

**Independent Test**: render frames 1–3, 15, 16 and 24 (and 5, 6, 17, 20, 26 to 28) and compare with the reference's
Home, Win and Wardrobe, and the bottom menu with the owner's wooden variant.

**Acceptance Scenarios**:

1. **Given** a won level, **When** the win card appears, **Then** it shows the wooden sign with flower clusters, the
   finished picture as full-color tiles in a stone frame, the heroes on a stone pedestal with rays and petals, the
   reward pill with the lotus, and Next in a wooden rim.
2. **Given** Home, **When** it is drawn, **Then** Settings, the centered Petals pill with its flowered corners and the
   profile avatar stand on one header row, the logo has wooden letters with leaves, the level is on a wooden plaque,
   and Play is the big green button in a wooden rim.
3. **Given** the Wardrobe or the Store page's cosmetics, **When** they are drawn, **Then** they use the wooden banner
   (with the back button and the Petals pill on one line), the parchment name card (the Wardrobe), family tabs with
   hero pictures and outfit cards whose worn item is green with a check.
4. **Given** Home over the owner's layered picture, **When** it shows, **Then** the four heroes stand on the painted
   fountain where the reference shows them, each breathing and swaying in its idle loop, one reacting every few
   seconds in turn, and a tap on a hero makes it react at once while a tap on any button still does what it did.
6. **Given** Home, the Store page, the Wardrobe, the Leaderboard page or the Collection page (FR-030), **When** it
   shows, **Then** the wooden bottom menu lies across the screen's bottom with its five places from Level 1 (Shop,
   Wardrobe, Home, Leaderboard, Collection; a locked one with a padlock badge on its icon), the screen's own place
   raised in the medallion; a tap on another place opens the Store page, the Wardrobe, Home, the Leaderboard page or
   the Collection page straight away (no card over Home), a locked one its page saying "Available from level N" (12,
   40, 10, and 2 for the Collection) instead of its content; back and the system back return from the Leaderboard and
   Collection pages to Home (from a Collection picture's detail, to the grid first); and Home shows no Store, Wardrobe
   or Collection side button and no rank pill.
5. **Given** a won level, **When** the win (or the milestone) shows its celebrating hero (Twig), **Then** the hero plays its
   reaction as it appears and then idles for as long as the screen shows.

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
- A family whose animated frames are missing shows its still hero on Home and its still celebrating picture (else the
  group) on the win; over the owner's garden without its fountain layers, Home shows the garden alone, with no heroes.
- A screen shape from 16:9 to 21:9 keeps the four heroes on the fountain, inside the screen, under the logo and above
  the level plaque.
- A stack with fewer pods than the tray's rows leaves its lower rows empty. A stack with more pods shows "+N" on its
  last shown pod. An emptied stack shows a sunk well where its exposed pod stood. Six stacks (`SourceTray.MaxStacks`)
  still make one row of columns: their pods get narrower and shorter; each place stays at least 1.45 times as wide as
  tall, so the 1.3:1 frame keeps its shape.

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
- **FR-004**: Gameplay stays flat 2D (constitution VII). 3D pictures appear only on Home (and the splash, which shows
  Home's stage), the win and milestone screens, the Wardrobe and the profile; the animated heroes are pre-rendered flat
  frames too (FR-028).

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
- **FR-011** *(amended on 2026-10-03: no arch)*: The board MUST sit inside a border of cream stone blocks on a lawn;
  a Garden Entry has no picture, its Bloomlings set off from the stone border beside the entry cell; walkers stay the
  spec 004 2D characters.
- **FR-012**: The tray, slot row and booster bar MUST sit on parchment as in the reference.
- **FR-013** *(pod layout amended on 2026-10-03 by FR-021 and the owner's choice "E": the icon first, the count in a
  corner)*: Pods MUST be wooden frames holding the variant's icon over the middle of the panel with the small count at
  its bottom right corner, and Waiting Slots cream plates holding the tile with the count below it; empty slots show a dashed inner outline. This replaces the characters of spec 004
  FR-008 and FR-009. All states of spec 002 FR-012 and FR-013 and spec 003 FR-022a stay distinct: waiting pods muted,
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
- **FR-017** *(amended on 2026-10-04: the Store is a page, FR-029; one Wardrobe button, the avatar; then the bottom
  menu, FR-030)*: Home MUST show the wooden logo letters with leaves, the level on a wooden plaque and the big Play
  button; its Wardrobe entry is the bottom menu's Wardrobe place once the Wardrobe opens (the profile avatar with a
  shirt badge was Home's one Wardrobe button until the bottom menu, and it stays in the Wardrobe's profile tab; since
  the owner's header request of 2026-10-04 the avatar, without the shirt badge, stands at the right of Home's header
  row for the profile page to come, FR-024); the Wardrobe and Store pages and the other meta cards use the same signs, parchment, tabs and cards.
- **FR-018**: The Petals symbol MUST be the pink lotus everywhere it appears.

#### F. Pictures from the owner

- **FR-019**: `pictures.md` MUST list every picture the owner makes (3D heroes and poses, backgrounds, logo), each
  with its slot id, size, format and screens. Each listed slot MUST exist in the asset slot registry with the drawn
  stand-in used until the picture arrives.

#### G. Reference layouts (owner's review, 2026-10-02)

- **FR-020**: The gameplay screen MUST follow the reference layout (contracts/look.md §6.1): the top bar; the board in
  its stone border on the lawn, wide and full of color; a thin lawn strip below it (no arch since 2026-10-03); then one
  parchment tray to the bottom of the screen holding, in this order, the row of five Waiting Slots, the row of four
  booster boxes, and the Source stacks, one column each (FR-021). This keeps spec 001 FR-068 (board in the center,
  entry and slots below it, the stacked tray with its booster bar at the bottom).
- **FR-021** *(amended on 2026-10-03 by the owner's gameplay rule: columns replace the deck)*: Each Source stack MUST
  be drawn as a column of pods, one after another and never on each other, whatever the look: the exposed pod in the
  top row and the next pods of the stack in the rows below it. Each pod is fully visible, so the player reads what each
  choice uncovers (as spec 003 FR-022a had it).
  - **Rows**: the tray MUST show four rows when the safe area is at least 1.95 times as tall as it is wide
    (`ReferenceGameplayRegions.FourRowsAspect`; 19.5:9 phones and taller), and three rows on shorter screens.
  - **Pods**: each pod is a wooden frame 1.3 times as wide as it is tall, centered in a place at least 1.45 times as
    wide as tall, with the owner's icon of the variant over the middle of its tinted panel and the count's small
    outlined digits at the panel's bottom right corner (`PodChip`; the owner's choice "E" of 2026-10-03). The exposed
    pod is taller than the waiting ones (`0.13W` against `0.1W`, contracts/look.md §6.1).
  - **Selection**: the exposed pod is bright and the only one selectable (spec 001 FR-011). Its touch box is at least
    the touch minimum.
  - **Waiting pods**: they are muted, but their variant symbol and count stay readable (spec 001 FR-013; identity is
    never carried by hue alone, spec 001 FR-072). A mystery pod shows only "?" and its count. Locked and connected
    pods keep their marks.
  - **Deeper and empty stacks**: a stack deeper than the rows shows a "+N" disc on its last shown pod, where N is the
    number of pods not drawn. An emptied stack shows a sunk well where its exposed pod stood.
  - **Motion**: when the exposed pod leaves for its slot, the pods under it slide up one row. When Return puts a pod
    back on top, its column slides down.
  - **Sizes**: to fit four rows, the slot row (`0.16W`, plates `0.14W`), the booster row (`0.18W`, boxes `0.16W`) and
    the gaps holding the lines between the rows (`0.03W`) are smaller than the reference's (`0.19W`, `0.23W`,
    `0.04W`).

  This is presentation only (FR-002): no rule, event or tap outcome changes.
- **FR-022**: The jam card MUST be a centered modal card over the dimmed gameplay (contracts/look.md §6.2): the cream
  round close button over its top-right corner when the rules allow closing, the title, the subtitle, the well with the
  slot contents, the choices as a two-column grid of big colored buttons with cost pills below them, and the Restart
  button.
- **FR-023**: The win screen MUST be the reference's full-screen celebration (contracts/look.md §6.3), not a card: the
  wooden sign with flowers at the top, the finished picture large in its stone frame, the celebrating hero (or the
  group) on a stone pedestal overlapping the picture's foot with rays and petals, the reward pill on the pedestal,
  and the big Next button in its wood rim at the bottom. The gameplay top bar is not shown on it.
- **FR-024** *(amended on 2026-10-04: the bottom menu, FR-030; the owner's header row)*: Home MUST follow the reference
  layout (contracts/look.md §6.4): the header row on one line (settings at the top left, the large Petals pill centered
  between it and the avatar with the Play button's leaves and flower on its top-left and bottom-right corners, and the
  profile avatar, as large as settings, at the top right; a tap on the avatar opens the profile page once it exists,
  until then it says "Profile coming soon" where Home has a toast), the logo across the top, the diorama (the owner's Home picture, or the heroes on a pedestal with the lotus fountain) in the middle, the wooden
  level plaque, and the big Play button below it, all above the bottom menu. Our other Home entries stay reachable: the
  Daily Challenge as a small cream round side button, the milestone teaser and the free booster as pills under Play,
  and the Store, the Wardrobe, the Leaderboard (the rank) and the Collection as the bottom menu's places (FR-030; they
  were side buttons and the rank pill before). Over the owner's Home picture the heroes were first deferred by the
  owner (2026-10-02: placing them around the painted fountain is hard); the owner's layered Home and animated heroes of
  the same day bring them back (FR-028): over the layered picture, Home and the splash stand the four animated heroes
  on the painted fountain; the drawn stand-in (without the picture) keeps its still heroes; over an owner picture
  without the fountain layers (a splash picture of its own, B6), no heroes show (`HomeStage.ShowsHeroes`).
- **FR-025** *(amended on 2026-10-04: the header on one line; the bottom menu)*: The Wardrobe MUST follow the reference
  layout (contracts/look.md §6.5) in both builds; the playtest gets a Wardrobe screen (equipping through the shared
  `WardrobeService`) instead of only the Store's cosmetics tab. Its header (the back button, the wooden banner with ivy
  and the Petals pill) MUST stand on one line, the back button's middle, with the banner's leaves clear of the back
  button and the Petals pill on every phone from 16:9 to 21:9 (`ScreenLayout.PageHeader`, shared with the Store page).
  Its cards, footer and page arrows MUST end above the bottom menu, which shows over the panel's foot with the
  Wardrobe in its medallion (FR-030).
- **FR-026**: Board tile icons MUST be the reference's "gem" icons: the variant symbol about 56% of the tile with a
  thick dark outline, a glossy fill in a shade of the tile color and a highlight (contracts/look.md §3.1.2).
- **FR-027**: The booster icons and the leaf decorations (sign ivy, win-sign flowers, button corner leaves, logo
  leaves) MUST be replaceable by the owner's pictures (pictures.md D): when a picture file exists, both builds draw it
  instead of the drawn icon or leaves.

#### H. The owner's layered Home and animated heroes (owner's delivery, 2026-10-02)

- **FR-028**: Home and the splash MUST draw the owner's layered Home over the garden (contracts/look.md §6.4): the
  fountain's back, Drop and Bloom on their soft shadows, the lotus again (Bloom stands behind it), Sprig and Twig on
  theirs, the fountain's front over the heroes' feet and the petals drifting, then the UI on top, every layer in the
  box the backdrop cover-fits the garden into. The four heroes MUST stand where the reference shows them (Sprig at the
  left, Bloom behind the lotus, Drop at the right back, Twig at the right front) and move as the owner's table says:
  each loops its 4 s idle from its own phase, they take turns to play their 2 s reaction (one every 6 s, each starting
  on its idle's first pose), and a tap on a hero makes it react at once, cross-fading from the idle frame it
  interrupts; a hero MUST never take a tap from Play, the side buttons, Settings, the Petals pill, the avatar (since
  2026-10-04), the plaque or the bottom menu (FR-030). Once
  the Wardrobe is open each hero wears its outfit (trail, skin, the expression on a badge, the hat turned with the
  head). The splash shows the same stage and motion, so it turns into Home without a jump. The win and the milestone
  MUST show the level's celebrant as its animated hero on the pedestal: Twig and Sprig by turns, level by level
  (`CharacterArt.CelebrantOf`; the owner's choices of 2026-10-03, which replaced the level's main family), Sprig
  alternating its celebrate and clap on its turns, its own celebration playing first when it has one: its reaction from the moment it appears, then
  its idle for as long as the screen shows (the still celebrating picture, then the group, while the frames are
  missing). The heroes MUST be pre-rendered flat frames (`tools/heroanim`, research D18): no 3D model, scene or camera
  in the game (constitution VII). The Wardrobe, the profile, gameplay and the milestone's group keep the still
  pictures. A build MUST NOT keep all frames decoded: it loads a frame when first drawn and keeps a bounded cache.

#### I. Pages (the owner's notes, 2026-10-04)

- **FR-029**: The Store MUST be a full-screen page, not a popup card, in both builds (contracts/look.md §6.6,
  `ScreenLayout.ReferenceStore`): over the Wardrobe's garden, the Wardrobe's header with the "Store" banner, then a
  parchment panel to the bottom of the screen with the Shop / Cosmetics tabs and the item rows or outfit cards filling
  the page's width and height, a page of them at a time between page arrows. Everything the card offered MUST stay
  (FR-002): buying boosters for Petals, the real-money rows (unavailable while purchases are off), the cosmetics with
  their states, the Petals pill. The bottom menu's Shop (FR-030; Home's Store button before it) and the Petals "+" of
  Home and the other pages (the Wardrobe; the Leaderboard and the Collection since they are pages) open it; its back
  returns to where it was opened, and in the playtest the Android back closes it (and the other pages). Its list MUST
  end above the bottom menu, which shows over the panel's foot with the Shop in its medallion.

#### J. The bottom menu (the owner's request, 2026-10-04)

- **FR-030** *(amended on 2026-10-04: every place always shows; a locked one says its level; every place a page)*:
  Home and the four pages (the Store, the Wardrobe, the Leaderboard and the Collection) MUST show the owner's wooden
  bottom menu in both builds
  (contracts/look.md §6.7, `ScreenLayout.BottomNav`): a warm brown wooden plank across the screen's bottom (its plank
  about `0.14W` tall on the safe bottom, the wood running on behind the bottom inset) with grain, rounded ends and thin
  grooves between the places, and no vines at its ends (the owner's review of 2026-10-04); the places' icons (the
  owner's pictures, pictures.md D9–D13; a drawn glyph while one is missing) on it, each the plank's full height and the
  places sharing its whole length, so little of the plank is left free; and the screen's own place in a raised round
  wooden medallion (a lighter wood disc in a darker rim with vines and two small white flowers, about `0.2W`, rising
  about `0.03W` over the plank's top, its icon a little larger than the plank's). The menu's top stays `0.17W` above
  the safe bottom. The places, left to right, are Shop, Wardrobe, Home, Leaderboard
  and Collection; all five MUST always show, sharing the bar's width evenly (the owner's request of 2026-10-04). A place
  is open once its feature is unlocked (the Shop from L12, the Wardrobe from L40, the Leaderboard from L10, the
  Collection once a picture is won; Home always); a locked place MUST keep the owner's icon, unchanged and tappable,
  with a small padlock badge at its lower right inside the plank's band (about 0.34 of the icon; never on the
  medallion's place). A tap on the Shop opens the Store page, on the Wardrobe the Wardrobe, on Home returns to Home, on
  the Leaderboard or the Collection opens its page (the owner's request of 2026-10-04: "All the menu's places must be a
  separate page. Not popups."), straight from any page and with the click; a tap on the medallion's place does nothing.
  The Leaderboard and the Collection MUST be full-screen pages on the Store page's frame (contracts/look.md §6.8, §6.9;
  `ScreenLayout.ReferenceLeaderboard`, `ReferenceCollection`): the Wardrobe's garden, the page header with their
  banner and the Petals pill, the parchment panel and the bottom menu with their place raised. The Leaderboard page
  MUST hold the rank rows (as many lines as the data has and the page fits, at least eight on every phone from 16:9,
  keeping the player's own row in view, filling the panel's height), the player's own row highlighted, the offline or
  empty line and Refresh; the Collection page the count line and the framed finished pictures, newest first, three to
  a row, as many rows as fit (the frames a little smaller when one more row then fits), with the page arrows right
  under them when they take more than one page, and a picture's detail (the picture large, its name and level) on the
  page. Back and the Android system back MUST return from either page to Home, from a Collection
  picture's detail to its grid first; the Store page opened from a page returns to it. The Collection is never a level
  selector. Their data, rules and analytics events (`leaderboard_view`, `collection_open`) stay as the cards had them.
  A locked place's page MUST show the locked notice instead of its content:
  the place's icon with the padlock badge, "Available from level N" and "Keep playing to unlock it!" (contracts/look.md
  §6.7), N from the build's own roadmap (12, 40, 10) or 2 for the Collection: the locked Store page keeps its garden,
  header and parchment panel, the notice in place of its tabs, rows, page arrows and offline line; the locked Wardrobe
  keeps its garden and header, the page's lighter panel holding the notice in place of the hero, name card, tabs,
  cards and footer; the locked Leaderboard and Collection pages keep their garden, header and parchment panel, the
  notice in place of the ranks or the pictures. A locked Store page
  MUST NOT send `store_open` (it is not a Store visit). Every place's touch box MUST be at least the touch minimum,
  inside the safe area and clear of the screen's other buttons. The menu MUST NOT show in gameplay, on the win, milestone, jam or pause cards, or
  on the splash. Home MUST NOT keep a button the menu doubles: its Store, Wardrobe and Collection side buttons and its
  rank pill are removed. The menu changes no rule, economy value, unlock or tap outcome inside a screen (FR-002); it
  only adds these ways between Home and its pages.

#### K. The backgrounds' saturation (the owner's note, 2026-10-04)

- **FR-031**: The owner's backgrounds (the gameplay themes, the win, the Wardrobe and the pages' garden, the layered
  Home with its layers as one scene) MUST keep at most 70% of the animated heroes' mean HSL saturation (the owner's
  choice after 60%, 80% and 70% were shown; contracts/look.md §1.4), scaled offline by `tools/heroanim/saturation.mjs`
  (and `layers.mjs` for the Home layers) with their lightness and hue kept. The UI, the heroes, the 2D characters and
  the board's pieces keep their saturation. Presentation only (FR-002).

### Key Entities

- **Material picture**: an engine-free RGBA picture of a material (wood, stone, parchment, candy tile) rendered at a
  pixel size by the kit and cached by each build.
- **Tile look**: a variant's candy tile in one of two styles: board (small, embossed symbol) and sticker (pods, slots,
  jam row: a detailed symbol with a light edge).
- **Hero frame**: one pre-rendered picture of a hero's idle or reaction, cropped from a 448 × 504 cell whose feet line
  is at 90% of its height, with its crop and two head points in the kit (`HeroMotion`).
- **Home layer**: one picture of the owner's layered Home with its box in the 852 × 1846 picture (`HomeLayers`).
- **Page header**: the header row of the four pages, the Wardrobe, the Store, the Leaderboard and the Collection
  (`PageHeader`): the back button, the banner and the Petals pill's box on one line.
- **Bottom menu**: the wooden bar of Home and the four pages (`BottomNavRegions`): its plank, the shown places
  (`NavPlace`) in order with their icons and touch boxes, and the medallion over the active place.
- **Pod chip**: one pod of the tray's grid at a depth of its stack's column (`ReferenceGameplayRegions.Pod`, `Chip`).
  It has a frame 1.3 times as wide as tall centered in its place, an inner panel, the icon and the tile's square over
  the panel's middle, the count at the panel's bottom right corner and the "+N" disc over the frame's top left corner
  (`PodChip`).

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
- **SC-006**: Every Meshy family has a 4 s idle loop (96 frames at 24 fps) and a 2 s reaction (48 frames), Twig its
  Blender clips (a 3 s idle, a 1.5 s reaction and the win's 3 s cheer) and Sprig its (a 4 s idle, a 3 s wave and the
  win's 2 s celebrate and 3 s clap), all starting and
  ending on the idle's first pose, as the owner's table says (`HeroMotionTests`).
- **SC-007**: The 756 hero frames take under 32 MB in the repository (612 under 27 MB before Sprig's model), and `node tools/heroanim/check.mjs` verifies every
  frame, every Home layer and the generated kit files against the last bake.
- **SC-008**: On every screen shape from 16:9 to 21:9 the four heroes' seam pictures lie inside the screen, under the
  logo and above the level plaque, each about its measured height, with its shadow under its feet (`HeroMotionTests`).
- **SC-009**: In the preview and on a device, a tap on a hero makes it react and a tap on Play, a side button,
  Settings, the Petals pill, the plaque or the bottom menu does what it did before FR-028 (and a tap on the header's
  avatar what FR-024 says).
- **SC-011**: On every screen shape from 16:9 to 21:9 and for one to five shown places (the screens show all five since
  2026-10-04), the bottom menu lies across the screen's bottom with its plank on the safe bottom, its places in order
  and evenly spread, the medallion over the active place rising above the plank and inside the screen, every other
  place's touch box at least the touch minimum inside the safe area and a locked place's padlock badge inside its icon
  and the plank's band; Home's Play and teaser row and the four pages' content end above its top; the Leaderboard and
  Collection pages keep their regions in order on the Store page's frame with every target reachable, eight rank lines
  fitting and three frames to a row; the locked notice keeps its parts in order inside its area
  (`ReferenceLayoutTests`); and in the preview its places open what FR-030 says, page to page, back returning to Home
  (frames 5, 6, 17, 20 and 27; locked, frames 29 to 31).
- **SC-010**: These hold on every screen shape from 16:9 to 21:9, with two to six Source stacks, with or without
  boosters, a badge or a bottom entry (`ReferenceLayoutTests` checks them on its phone shapes):
  - no two pods of the tray overlap;
  - four rows show from a safe aspect of 1.95 and three below it;
  - the board keeps at least a third of the safe height;
  - each exposed pod's touch box is at least the touch minimum and stays inside the safe area, clear of the other
    pressed controls.

  In the preview, every pod shown in the gameplay frames shows its variant and count, or "?" and its count.

## Assumptions

- The owner's later 3D heroes and backgrounds will be delivered as PNG files at the listed sizes, and the builds will
  show them through the existing picture slots. The animated heroes came as FBX models instead (2026-10-02); they are
  pre-rendered into PNG frames offline (FR-028), never loaded as models.
- The jam keeps our recovery choices (spec 001); the reference's Shuffle button in the jam card is shown only if the
  rules offer Shuffle there.
- The playtest gets a Wardrobe screen (FR-025, owner's review); its Store cosmetics tab keeps the Wardrobe look for the
  items sold there.

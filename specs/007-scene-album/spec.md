# Feature Specification: Scene Album

**Feature Branch**: `claude/great-darwin-6qrpj8` (spec 007-scene-album)

**Created**: 2026-11-02

**Status**: Implemented (both builds; the owner's look check pending); the chest of 2026-11-02 implemented

**Input**: The owner, 2026-11-02 (translated): «Implement this page in the game. The Scene Album replaces the existing
Collection menu item. For now just switch Collection off, do not delete it, but write down somewhere that it is ballast to
decide later (reuse or delete). The album opens after a certain round. A card shows one composition with references of
pictures from levels; when the player reaches that level and wins it, that object opens in the card; before that they are
grey. Keep the game's buttons, UX and directions; the mockup is only a reference of layout and look. If it is a new element
(the card, the horizontal swipe), it should look like this. Everything else we have — reuse (the settings button, the back
button, ‹ › of the profile editor), reuse them with an arrow for the swipe. Make new components only where needed.»

Sources: the owner's Scene Album UX (README of 2026-11-02, authoritative for behaviour), the mockup of three states (layout
reference only), the open-album menu icon, the layered scene pack `scene-album-layered-v1` (two scenes, forest and
airmail, each a background and four objects on one 853 × 1844 canvas), the content-unlocks plan of 2026-11-01 (album
scenes at L27, one big scene of 3–5 campaign pictures, one chest a scene, early wins count, binding by explicit picture
correspondence) and `product/13_UNLOCK_AND_MILESTONE_ROADMAP.md`.

## User Scenarios & Testing

### User Story 1 - See the scene fill in as I win (Priority: P1)

From L27 the bottom menu's Album opens a big scene card. Objects whose campaign level I have won are in colour, the
others grey; wins before L27 count at once.

**Independent Test**: jump to L69, open the Album: the Forest Glade is 4 / 4, the Air Mail Garden 1 / 4 (the windmill).

**Acceptance Scenarios**:

1. **Given** a player on L26, **When** they tap Album, **Then** the page says "Available from level 27" and the menu
   place wears the padlock.
2. **Given** a player on L27, **When** they open the album, **Then** the forest scene (L1, L4, L7, L17) shows 4 / 4 and
   its four objects play the colour's return once.
3. **Given** a player who wins L54, **When** they next open the album, **Then** the windmill comes back to colour
   (0.5 s) and the menu showed one "!" until then.

### User Story 2 - Find out where a missing object comes from (Priority: P1)

**Acceptance Scenarios**:

1. **Given** a grey object, **When** I tap it (its opaque pixels, not its transparent area) or its medallion, **Then** it
   gets a thin golden outline and a bottom sheet (about a third of the screen) shows its colour preview, name, "Unlocks
   after Level N", "Next level: M" and the green "Continue playing", which starts level M (not N).
2. **Given** the sheet, **When** I tap ×, swipe it down or tap outside, **Then** it closes and the scene and carousel
   position stay. If the sheet would cover the object, the card shrinks over it while it is open.
3. **Given** a coloured object, **When** I tap it, **Then** the sheet shows its name, the check and "Level N completed",
   with no unlock offer. Nothing is ever bought in the sheet.

### User Story 3 - Leaf through scenes (Priority: P2)

A horizontal swipe or the existing raised ‹ › buttons (the profile/Wardrobe arrows) turn one card at a time with a snap;
the neighbours' edges show; "Scene 2 / N" tells the place; no cycling at the ends; a swipe never opens a sheet. The album
opens on (1) a scene with a new reveal or an unclaimed reward, else (2) the last viewed incomplete scene, else (3) the
nearest incomplete one. Future scenes appear gradually: a scene shows once one of its objects is within 30 levels.

### User Story 4 - Claim the finished scene's chest once (Priority: P2)

The owner's change of 2026-11-02 (translated): «I want the progress (1 of 4, 4 of 4 …) to look like a progress bar. Keep the
object medallions, just add a progress bar below them, and at the end of the bar there will be a chest — as if it is a
little chest we give the player. When you tap the chest, a small tooltip pops up above the chest showing what we give, e.g.
200 Petals, one Return booster and one new-slot booster. Everything should look like a chest.»

Under the medallions a progress bar fills with the opened objects ("1 / 4" on it; its fill grows as a reveal comes back to
colour) and runs into the scene chest (the owner's picture) at its right end. While the scene fills, the chest stays in full colour (the owner, 2026-11-02: always active, no Claim yet)
and a tap shows its tooltip: the prizes as big icons with only their amounts (200, ×1, ×1; no title, no words — the owner, 2026-11-02), without Claim. At 4 / 4 the card wears the green check, the
caption says "Scene complete" and the chest glows and hops gently; a tap opens the tooltip with the contents and a green
Claim (a gift: no purchase confirmation). After the claim the tooltip and the caption read "Reward claimed" and the chest
wears the check ball; the card stays to look at; the claim is restored from the save and never paid twice (also across a
cloud merge). The tooltip closes on a tap outside or a second tap on the chest and never blocks the swipe.

### Edge Cases

- A picture no published level holds yet: the object stays grey and its sheet says "Arrives with new levels".
- Past the last published level: "Continue playing" leads to "new levels soon" as Play does.
- A server-authoritative economy has no album grant yet: the tooltip's Claim stays greyed and nothing is marked claimed
  (open point).
- A save older than this feature has no `album` section: everything open counts as new once (the first-fill animation).
- A save whose `album` section is newer than the app is refused like a newer save.

## Requirements

- **FR-001** The Scene Album replaces the Collection in the bottom menu (Shop, Wardrobe, Home, Leaderboard, Album), with
  the owner's open-album icon (`Icons/nav-album.png`, slot `icon.nav.album`).
- **FR-002** The Collection is switched off, not deleted: its screens, service and save data stay and wins still record
  its pictures. It is **ballast** to reuse or delete later (product/OPEN_QUESTIONS.md).
- **FR-003** The album opens at L27 (`system.album` in `content/roadmap/unlock-roadmap.json` and `UnlockRoadmap.Default`);
  before that the menu place shows the padlock and the locked notice, like the other locked places.
- **FR-004** Opening priority: news (a new reveal or an unclaimed reward), then the last viewed incomplete scene, then the
  nearest incomplete scene.
- **FR-005** The catalog (`Meta/Album/Resources/AlbumCatalog.json`, from the owner's pack by `tools/album/import_scene_pack.py`)
  binds each object to a picture code; the level is the lowest published round level holding that picture. An object is
  open when that level is won (`highestCompletedLevel ≥ level`); nothing about openness is stored.
- **FR-006** Future scenes are shown gradually (30 levels ahead), never a far scene before a near one.
- **FR-007** The card: the scene contain-fitted, never cropped, background always in colour; locked layers greyscale at
  82 % brightness; open layers in colour; the popups' wooden card frame; neighbours' edges visible.
- **FR-008** Swipe and arrows turn one card with a snap; no cycling; the arrows reuse the raised ‹ › buttons.
- **FR-009** Tap on a locked object (alpha hit test) or its medallion: golden outline and the object sheet with
  "Unlocks after Level N", "Next level: M", "Continue playing" (starts the next available level).
- **FR-010** Tap on an open object: name, check, "Level N completed".
- **FR-011** The sheet closes by ×, swipe down or a tap outside, and lifts/shrinks the card when it would cover the object.
- **FR-012** A complete scene's reward is its chest (the owner's change of 2026-11-02): one bundle of Petals, Return charges
  and Extra Slot charges (Remote Config `economy.album.chest.petals` 200, `economy.album.chest.return` 1,
  `economy.album.chest.slot` 1, after the owner's example; it was `economy.album.sceneReward`, 50 Petals), claimed once
  through the economy service in one grant, no purchase confirmation. The chest (the owner's picture `Album/chest.png`)
  stands at the right end of a progress bar under the medallions (a groove pressed into the strip, the green fill growing
  with the reveals, "n / 4" on it): in full colour while the scene fills (no Claim), glowing and hopping gently when ready, with the check
  ball once claimed. A tap on it toggles a tooltip above it (the guides' parchment bubble) listing the contents with the
  Petals lotus and the booster icons, and the green Claim when ready, "Reward claimed" after, else how it opens; a tap
  outside closes it; a swipe still turns the card. The old reward strip is gone; "Scene complete" stays as the caption.
- **FR-013** New reveals play a short colour return (0.5 s crossfade, staggered) on the next album open.
- **FR-014** The save's optional `album` section (own `version` 1): claimed scenes, seen objects per scene, last viewed
  scene; merged as unions.
- **FR-015** Player-facing text in `Strings_en.csv`; no analytics events (the contract has none for the album yet).
- **FR-016** One unobtrusive "!" on the menu's Album (the Daily scene's alert badge) while a reveal or reward is new.
- **FR-017** Both builds (the full playtest and Unity); the level tester ignores the album. Flat 2D pictures only.

## Success Criteria

- Preview frames 271–276 render and their checks pass (1 / 4 with grey objects and the chest in colour, a locked object's
  sheet, 4 / 4 with the glowing chest, after the claim, the locked place before L27, the chest's tooltip with Claim).
- The engine-free tests (`AlbumServiceTests`) cover the binding, the retroactive count, the order rule, the reward once,
  the save section and its migration.

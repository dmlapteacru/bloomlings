# Tasks: Scene Album

**Input**: [spec.md](spec.md), [plan.md](plan.md). All tasks done on 2026-11-02 unless marked open.

## Phase 1: Setup

- [x] T001 Import the owner's pack: `tools/album/import_scene_pack.py` → `client/Assets/Bloomlings/Art/Album/Resources/Album/`
  (JPEG backgrounds, cropped PNG objects) and `client/Assets/Bloomlings/Meta/Album/Resources/AlbumCatalog.json`
- [x] T002 The menu icon `Icons/nav-album.png` (512 px, 8-bit palette, as D9–D13); source record `Art/Album/SOURCE.md`;
  notices in `client/THIRD_PARTY_NOTICES.md`; import settings in `Editor/OwnerIconImporter.cs`

## Phase 2: Foundational

- [x] T003 `AlbumCatalog`, `AlbumService` (binding, open state, shown scenes, opening rule, news, viewed, claim once)
- [x] T004 Save: `AlbumData`, the `album` section (own version, migration hook, refusal of newer), merge unions, schema
- [x] T005 Roadmap `system.album` at L27 in `content/roadmap/unlock-roadmap.json` and `UnlockRoadmap.Default`; Home demo
- [x] T006 Remote Config `economy.album.sceneReward` (50) in `RemoteConfigKeys` and `backend/remote-config/defaults.json`
- [x] T007 Kit: `AlbumLook` (page, sheet, fit, layer boxes, alpha hit test, lift, reveal, snap, outline, locked pixels),
  `BottomNav` Album place, `HomeLook.Album`/`AlbumNews`, asset slots `ui.album.*` and `icon.nav.album`

## Phase 3: US1 + US3 — the page (P1/P2)

- [x] T008 [P] Playtest `AlbumScreen` + `DesignApp` (open, turn, swipe, arrows, reveals, back, warm-up)
- [x] T009 [P] Unity `AlbumScreen`, `AlbumArt`, `HomeController` and `Boot` wiring, menu "!" in `BottomNavView`

## Phase 4: US2 — the object sheet (P1)

- [x] T010 Alpha hit test, medallion taps, golden outline, sheet (×, swipe down, tap outside), lift, Continue playing

## Phase 5: US4 — the reward (P2)

- [x] T011 Reward strip with Claim (no confirmation) and "Reward claimed"

## Phase 6: Polish

- [x] T012 Strings (`album.*`, `demo.album`), `AlbumServiceTests`, reference-layout tests for the new menu
- [x] T013 Preview frames 271–275, updated fixtures (Collection opened directly as ballast), inventory regenerated
- [x] T014 Docs: CLAUDE.md, product/13, product/OPEN_QUESTIONS.md (reward amount, Collection ballast)
- [ ] T015 (open) The owner's look check on the APK; scene names/object names confirmation; reward amount
- [ ] T016 (open) A server-side album grant for the authoritative economy (Claim is greyed there now)

# Bloomlings Unity client

Unity **6.3 LTS** (`6000.3.x`) project. Presentation and services only; the rules live in `core/` and are
consumed as local UPM packages (`Packages/manifest.json` → `file:../../core/src/...`).

## First open (completes task T008)

The repository only contains what can be written without the Editor. On the first open with Unity 6.3 LTS:

1. Open `client/` in Unity Hub with the newest `6000.3.x` LTS patch. Unity creates `ProjectSettings/`,
   including `ProjectVersion.txt`. Record the patch in `specs/001-core-game-mvp/research.md` (R1) and commit it.
2. Add the built-in packages through Package Manager, keeping the versions the Editor proposes:
   Universal RP, uGUI (includes TextMeshPro), Localization, Addressables and Test Framework.
3. Player Settings:
   - orientation portrait only;
   - IL2CPP with ARM64;
   - iOS minimum 15.0 and Android minimum API level 26;
   - company and product name "Bloomlings".
4. Create a URP 2D Renderer asset at `Assets/Settings/URP-2D.asset` and assign it in Graphics and Quality settings.
5. Commit the generated `.meta` files, including those Unity creates inside `core/src/Bloomlings.*`.
   They give the shared packages stable GUIDs.
6. Import the TextMeshPro essentials (**Window > TextMeshPro > Import TMP Essential Resources**); all UI text uses
   TextMeshPro.
7. Run **Tools/Bloomlings/Create All Scenes**. It writes `Assets/Bloomlings/Scenes/Boot.unity`, `Home.unity` and
   `Gameplay.unity` as build indexes 0, 1 and 2. This completes T025 and the scene part of T052.
8. Optionally run **Tools/Bloomlings/Create Variant Visuals Asset** (T039) to get an editable
   `Art/Variants/VariantVisuals.asset`; without it the placeholder visuals are used.

## Playing in the Editor

Press Play in the Boot scene for the real flow: the first launch (no save) starts Level 1 directly with the guided
first tap; later launches open Home (Level N, Play/Continue). Without published packs, Boot reads the curated levels
straight from the repository's `content/curated/` folder. The save lives in
`Application.persistentDataPath/save/player_save_v1.json`; delete it to replay the first launch.
**Tools/Bloomlings/Fast Progress** sets the highest completed level and fires every unlock on the way.


**Tools/Bloomlings/Play Dev Level** opens the Gameplay scene and plays a level straight from the repository's
`content/curated/dev/` folder (or any level file you choose), without going through Boot. All screens, tiles, pods
and Bloomling workers are built from code with procedural placeholder art (`Art/Procedural/ProceduralSprites.cs`) and
the generated character pictures of spec 004 (`Art/Characters/Resources/Characters/`, made by `tools/artgen`, loaded by
`Art/Characters/CharacterSprites.cs`; `Editor/CharacterArtImporter.cs` sets their import settings), so no prefab is
needed yet. The owner's animated heroes on Home and the win are pre-rendered frames (`Art/Heroes/Resources/HeroMotion/`,
made by `tools/heroanim`, imported by `Editor/HeroMotionImporter.cs`; "Reference look" below).

## Verification status

`client/DotnetCheck` compiles every client script against minimal Unity API stubs and runs the engine-free EditMode
tests (save, progression, golden replays) under .NET; `core-tests.yml` runs it with the core tests when started by
hand:

```sh
dotnet test client/DotnetCheck/Bloomlings.Client.DotnetCheck.csproj
```

Extend `UnityStubs.cs` when the client starts using a new Unity API. The scripts have not been compiled or run in the
Editor yet. On first open, run the EditMode tests (`GoldenReplayEditModeTests` must pass: SC-005) and play the three
dev levels (quickstart §5 steps 1–6) before building on top of them.

## Content at runtime

`Boot` loads `Assets/StreamingAssets/content/manifest.json` and its packs, verified by SHA-256 (`BundledContentLoader`).
To bundle a catalog, run the pipeline's `publish` (it writes `build/content/` at the repository root), then
**Tools/Bloomlings/Import Published Content**, which verifies the packs and copies them into StreamingAssets. For a
playtest build of levels whose pictures are not approved yet, publish with `--allow-draft`.

Without a manifest, the Editor and development builds fall back to loose files in
`Assets/StreamingAssets/content/dev/pictures/*.json` and `dev/levels/*.json` (not on Android, where StreamingAssets
cannot be listed), and the Editor without that folder reads `content/curated/` from the repository. Release builds
leave the `dev/` folder out and fail without a manifest (`ReleaseContentBuildStep`; if a build was interrupted, run
**Tools/Bloomlings/Restore Dev Content Folder**).

Newer content is downloaded when Remote Config sets `content.manifestUrl` (HTTPS): `ContentUpdateService` checks it
after the game is playable, verifies every pack, installs the version atomically under
`Application.persistentDataPath/content/`, and activates it for the next attempt. The next launch starts from the
newest valid version this app can read.

## Online services (US6, US7)

Store, ads, consent, Remote Config, sign-in, cloud save and the leaderboard sit behind interfaces with offline
fallbacks, so the game runs without any of them (FR-074). Each SDK integration is its own assembly under `Assets/Bloomlings/Integrations/`, compiled only when its
package is installed (asmdef `versionDefines` + `defineConstraints`), and registers itself in `ServiceProviders` before
the first scene loads. To enable them, add through Package Manager, keeping the versions it proposes:

| Package | Enables | Assembly |
|---|---|---|
| Authentication (`com.unity.services.authentication` ≥ 3.0) | Anonymous sign-in in the background, optional Apple / Google Play Games linking (FR-087) | `Bloomlings.Integrations.Ugs` |
| Remote Config (`com.unity.remote-config` ≥ 4.0) | Remote tuning (FR-085) | `Bloomlings.Integrations.Ugs` |
| Cloud Save (`com.unity.services.cloudsave` ≥ 3.0) | Cloud copy of the save, merged on reconnect (FR-087, R15) | `Bloomlings.Integrations.Ugs` |
| Leaderboards (`com.unity.services.leaderboards` ≥ 2.0) and Cloud Code (`com.unity.services.cloudcode` ≥ 2.0) | Global leaderboard with server-side checks (FR-062) | `Bloomlings.Integrations.Ugs` |
| In-App Purchasing (`com.unity.purchasing` ≥ 5.0) and Cloud Code | Store, validated purchases (FR-051, FR-089) | `Bloomlings.Integrations.Iap` |
| Firebase Analytics (`com.google.firebase.analytics` ≥ 12.0) and Crashlytics (`com.google.firebase.crashlytics` ≥ 12.0), from the Firebase Unity SDK tarballs | Analytics events and crash reports with the R14 custom keys (FR-086) | `Bloomlings.Integrations.Firebase` |
| Google Mobile Ads (`com.google.ads.mobile` ≥ 9.0, from the OpenUPM registry already in `manifest.json`) | Rewarded and interstitial ads, UMP consent (FR-052, FR-053, FR-090) | `Bloomlings.Integrations.Ads` |

Analytics and crash reporting start only after consent (FR-090): `GameAnalytics` keeps events on the device until
then. With Firebase, also turn automatic collection off in the native configuration
(`firebase_analytics_collection_enabled` and `firebase_crashlytics_collection_enabled` set to `false` in
`AndroidManifest.xml` and `Info.plist`), and add `google-services.json` / `GoogleService-Info.plist` from the Firebase
console.

Each UGS package turns on its part of `Bloomlings.Integrations.Ugs` on its own (one `versionDefines` entry per
package); Authentication is required for all of them. Linking Apple or Google Play Games also needs the platform
sign-in plugins, which register their token sources in `ServiceProviders.AppleIdToken` and
`ServiceProviders.GooglePlayGamesAuthCode`; without them Settings shows no link buttons.

On iOS, add Unity's iOS 14 Advertising Support package (`com.unity.ads.ios-support`; it turns on `BLOOMLINGS_ATT` by
itself) and an `NSUserTrackingUsageDescription` to ask for ATT after the UMP form. Consent is read from the TCF values the
UMP form stores (`Services/Consent/TcfConsent.cs`): an EEA refusal is never treated as consent, and Settings shows
"Privacy options" where the rules require it. Fill the release ad unit ids in
`Integrations/GoogleMobileAds/GoogleMobileAdsService.cs`: development builds use Google's test units, and a release build
without its ids shows no ads. Set the store product ids to match
`Services/Purchases/Resources/ProductCatalog.json`, and deploy `backend/` (see `backend/README.md`). The integration code
has not been compiled against the SDKs yet: fix any API drift on first open.

## Android APK from GitHub Actions (Unity)

For an APK without the Unity licence, `android-apk.yml` builds the temporary playtest client in `playtest/`.


`.github/workflows/unity-apk.yml` builds the APK with GameCI and Unity `6000.3.25f1` (pinned in
`ProjectSettings/ProjectVersion.txt`). Run it by hand: **Actions → unity-apk → Run workflow**. It never runs on push,
so commits spend no Actions minutes. It keeps only the newest APK: after each successful run the older artifacts are
deleted, and each one expires after 7 days. Choose `game` (the playtest APK with the curated Levels 1–10) or
`golden-replays` (the device determinism player).

One-time setup: add these repository secrets under Settings → Secrets and variables → Actions:
- `UNITY_EMAIL` and `UNITY_PASSWORD`: the Unity account;
- `UNITY_LICENSE`: the full contents of the activated `.ulf` file (for a Personal licence, activate once in Unity Hub
  and copy `Unity_lic.ulf`), or `UNITY_SERIAL` for a Pro licence.

Never paste these into a chat or a commit.

The build method `Bloomlings.Client.Editor.CiBuild.Build` performs the first-open steps the Editor would need:
- it imports the TextMeshPro essentials;
- it applies the Android player settings (portrait, IL2CPP ARM64, API 26+, APK);
- it creates the scenes;
- it imports the packs the workflow publishes to `build/content/`.

## Device determinism check

**Tools/Bloomlings/Device Tests/Prepare Golden Replays** copies `core/tests/golden/` into `StreamingAssets/golden/`
(gitignored, left out of release builds) and creates `Assets/Bloomlings/Tests/Device/RunGoldenReplays.unity`. **Build
Golden Replays (Android/iOS)** builds that scene alone with IL2CPP. On each reference device the player logs
`[GoldenReplay] RESULT PASS n/n corpus=<digest> …`. Every case must pass, and the corpus digest must be the same on
every device (SC-005, SC-011).

## Design (spec 002)

The screens follow the UX design board (`specs/002-ux-design-board/ux-design-board.webp`), built without art assets.
The engine-free kit in `Assets/Bloomlings/UI/Design/` holds everything that defines the look, and the playtest links it:
- `DesignTokens`: colors, radii, type, spacing, elevation and motion (`contracts/design-tokens.md`);
- `ShapeLibrary` and `ShapeRaster`: every placeholder shape as a signed distance function, keyed by its asset slot id;
- `BackdropRaster`: the garden backdrop per level band theme;
- `ScreenLayout` and `HomeLook`: the regions of the gameplay screen, Home, cards and the jam sheet;
- `AssetSlots`: the registry the asset inventory (`specs/002-ux-design-board/asset-inventory.md`) is generated from.

Unity wraps the kit in `ProceduralSprites` (sprites), `UiTheme` (Unity colors), `UiKit` (the board's pills, raised
buttons, round icon buttons, badges, cards and bottom sheet) and `BackdropView`. The canvas matches the screen width at
1080 units, so token sizes map one to one. Final art replaces a placeholder by its slot id without layout changes.

## Garden look (spec 003)

The cartoon look of `specs/003-cartoon-ui-style/` extends the same kit (`DesignTokens.Garden`, `GardenLook`):
- `UiKit.Garden` builds a button as layered images: the cream plate (shadow, thickness, brown outline, gradient) and
  the raised face (outline, lip, gradient top, highlight). Its `GardenButton` component presses into the lip and
  springs back with one overshoot, breathes when it is the screen's waiting button, and greys out when not
  interactable; `VerticalGradient` draws the gradients, `DecorationLayout` places the leaves and flowers on PLAY and
  the main card buttons.
- `UiFonts` makes runtime TextMeshPro font assets from `UI/Fonts/Resources/Nunito-ExtraBold.ttf` and
  `Nunito-SemiBold.ttf` (SIL OFL, `OFL.txt` beside them), and one shared material per font and label look (outline
  plus a hard underlay for the extrusion). If a font cannot be loaded, labels keep the TextMeshPro default font.
- Cards, the jam sheet, the board, pods, slots and booster tiles took the reference look of spec 005 (below); the
  booster tiles keep every state.

Check on a device that the labels use Nunito with their outline and extrusion, and that the profiler shows no new
material per label.

## Reference look (spec 005)

`specs/005-reference-look/` restyles every screen after the owner's reference (`reference.jpg`), keeping the spec 002
layouts, the order of elements and every rule (recipes in `contracts/look.md`):
- `UiRaster` (kit, engine-free) renders the materials as straight-alpha RGBA pictures, deterministic: wood planks and
  pod frames, stone blocks, the arch, the pedestal and the candy tiles. `ProceduralSprites.Picture` turns them into
  cached sprites (9-sliced where needed, one per key and size); `PicturePixels` flips the rows and bleeds the edges.
- `UiKit` (`UiKit.cs`, `UiKitGarden.cs`, `UiKitGameplay.cs`, `UiKitTray.cs`, `UiKitCards.cs`, `UiKitMeta.cs`,
  `UiKitViews.cs`) holds the twins of the playtest's `Kit.*` components under the same names (`WoodSign`,
  `PrimaryButton` in its wood rim, `SpeedPill`, `ChoiceButton`, `CountBadge`, `CostPill`, `PetalsPill`, `Paper`, `Card`,
  `PodFrame`, `SlotPlate`, `BoosterTile`, `StoneBorder`, `StoneArch`, `StonePedestal`, `LightRays`, `FallingPetals`,
  `WoodLogo`, `OutfitCard`).
- The board is candy tiles in a stone border on a lawn: `BoardLayout` (kit) places the grid, the border and the arch
  entries for `BoardView`, and `BoardPictures` draws the restored ground, stone obstacles and the finished picture (win,
  Collection). Waiting Slots are cream plates holding the variant's candy tile with its plain count below it; the 2D
  characters stay as the walkers.
- The Source Tray's pods stand in columns, one after another and never on each other (a gameplay rule of the owner,
  2026-10-03; spec 005 FR-021, `contracts/look.md` §3.7 and §6.1). `GameplayHud.PodGrid` gives `TrayView` the kit's
  `ReferenceGameplayRegions` (`Pod`, `Chip`, `Shows`) in the tray's canvas units. There is one column per stack, with
  three rows, or four from a safe aspect of 1.95. Each pod is a `PodView` drawn by `UiKit.GridPod` (`UiKitTray.cs`):
  a wooden frame wider than tall, the candy tile at the left and the plain count at the right.
  - The exposed pod is bright and the only one that takes a tap, through a touch box of at least `size.touch_min`.
  - The waiting pods under it are muted but show their variant and count. "+N" sits on the last shown pod, and an
    emptied stack shows a sunk well.
  - The pods slide up a row in `PodView.SlideSeconds` when the exposed one leaves, and down when Return puts one back.
- The owner's pictures (`specs/005-reference-look/pictures.md`) load through `OwnerArt` from
  `Art/Backgrounds/Resources/Backgrounds/`, `Art/Brand/Resources/Brand/`, `Art/Icons/Resources/Icons/` (booster icons)
  and `Art/Decor/Resources/Decor/` (leaves, mirrored with a negative `localScale`) by the names in `OwnerPictures`; the 3D
  heroes and the optional celebrating heroes load from `Art/Characters/Resources/Characters/3d/` (`CharacterSprites`,
  `HeroPictures`), where `tools/artgen -- adopt` records them. The drawn stand-in shows while a file is missing.
- The owner's animated heroes and layered Home (spec 005 FR-028, `contracts/look.md` §3.12, §6.3, §6.4):
  `tools/heroanim` pre-renders the four FBX heroes into flat frames in `Art/Heroes/Resources/HeroMotion/` (no model
  enters the game, constitution VII), and the Home picture comes as `home.jpg` plus the `home-*.png` layers in
  `Art/Backgrounds/Resources/Backgrounds/`. `HeroFrames` loads a family's frames one at a time the first time they show
  and unloads them when no view holds the family; `HeroMotionView` shows one hero in its frame cell (its pose, the
  cross-fade, the outfit); `HomeLayersView` draws the layered Home with the four heroes, its touch boxes and the
  drifting petals, shared by the splash and Home so Home takes over without a jump; `HeroPictures.StageOf` picks the
  layered Home, the drawn stand-in or no heroes; the win and the milestone show the level's animated hero
  (`HeroPictures`). `Editor/HeroMotionImporter` sets the import of the frames and the `home-*.png` layers.

Check in the Editor (the client check covers the logic, not the look):
- The import settings: select a few files of `Art/Heroes/Resources/HeroMotion/` and the `home-*.png` layers. They
  should be Single sprites with a Full Rect mesh, no mipmaps, alpha is transparency, clamp, bilinear, no power-of-two
  scaling, not readable, compressed (the frames at normal quality, the layers at high quality). If they were imported
  before the importer existed, reimport the two folders. Compare a frame with its PNG for banding or dark fringes.
- The Home stage's sibling order (Home with the owner's pictures, Play mode): under the stage's `Layers`,
  `FountainBack`, then `ShadowDrop`, `HeroDrop`, `ShadowBloom`, `HeroBloom`, then `Lotus`, then `ShadowSprig`,
  `HeroSprig`, `ShadowTwig`, `HeroTwig`, then `FountainFront`, `Petals`, `PetalsAbove` and the four `Touch*` boxes;
  the logo, the buttons, the plaque, Play and the pills come after the stage, above it. Bloom's feet hide behind the
  lotus, Sprig's and Twig's behind the fountain's front flowers.
- Taps: a press on a hero makes it react at once (no click sound); Play, the side buttons, Settings, the Petals pill
  and the plaque keep their taps where they overlap a hero; the splash's heroes take none.
- The motion: each hero breathes in its 4 s idle, one reacts every 6 s in turn (Bloom first), the petals drift
  smoothly; the splash's heroes fade in and Home continues their motion; the win's hero reacts as it lands, then idles;
  the profiler shows the frames of at most the families on screen loaded.

## Localization

Player-facing text is table-driven (R18): code asks `Loc.T("key")`. The English source is
`Assets/Bloomlings/UI/Localization/Resources/Strings_en.csv`. With the Localization package installed, run
**Tools/Bloomlings/Localization/Import English Strings** to build the `UI` string table collection from it, then add
locales there. At runtime the selected locale's table is used first, then the bundled English. `LocalizationTests`
fails on any literal left in the UI and on any key missing from the CSV.

## Compiler settings

Each assembly folder, including the shared packages under `core/src/`, has a `csc.rsp` with `-nullable:enable`, so
Unity compiles the code with the same nullable context as `dotnet build`.

The assembly definitions in `Assets/Bloomlings/` (T009) reference `Unity.TextMeshPro` and `Unity.Localization`.
Until step 2 is done, Unity reports those references as missing.

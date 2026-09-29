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
and Bloomling workers are built from code with procedural placeholder art (`Art/Procedural/ProceduralSprites.cs`), so
no prefab or sprite asset is needed yet.

## Verification status

`client/DotnetCheck` compiles every client script against minimal Unity API stubs and runs the engine-free EditMode
tests (save, progression, golden replays) under .NET; CI runs it with the core tests:

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

On iOS, add Unity's iOS 14 Advertising Support package and the `BLOOMLINGS_ATT` scripting define to ask for ATT before
personalized ads. Fill the release ad unit ids in `Integrations/GoogleMobileAds/GoogleMobileAdsService.cs`
(development builds use Google's test units), set the store product ids to match
`Services/Purchases/Resources/ProductCatalog.json`, and deploy `backend/` (see `backend/README.md`). The integration code
has not been compiled against the SDKs yet: fix any API drift on first open.

## Compiler settings

Each assembly folder, including the shared packages under `core/src/`, has a `csc.rsp` with `-nullable:enable`, so
Unity compiles the code with the same nullable context as `dotnet build`.

The assembly definitions in `Assets/Bloomlings/` (T009) reference `Unity.TextMeshPro` and `Unity.Localization`.
Until step 2 is done, Unity reports those references as missing.

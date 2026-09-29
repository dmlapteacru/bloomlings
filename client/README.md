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

## Compiler settings

Each assembly folder, including the shared packages under `core/src/`, has a `csc.rsp` with `-nullable:enable`, so
Unity compiles the code with the same nullable context as `dotnet build`.

The assembly definitions in `Assets/Bloomlings/` (T009) reference `Unity.TextMeshPro` and `Unity.Localization`.
Until step 2 is done, Unity reports those references as missing.

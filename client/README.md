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

The assembly definitions in `Assets/Bloomlings/` (T009) reference `Unity.TextMeshPro` and `Unity.Localization`.
Until step 2 is done, Unity reports those references as missing.

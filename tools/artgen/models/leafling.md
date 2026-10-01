# Source record: the Leafling model (experiment)

| Field | Value |
|---|---|
| File | `tools/artgen/models/leafling.fbx` (binary FBX 7.4, 19 MB) |
| Original name | `Meshy_AI_Leafling_1001214215_generate.fbx` |
| Made by | the project owner, with Meshy AI (meshy.ai), on 2026-10-01; exported through Meshy's Blender FBX writer |
| Content | one sculpted mesh (329,940 vertices, 659,956 triangles) with normals; no texture, UV, material color, rig or animation |
| Used as | `client/Assets/Bloomlings/Art/Experiments/Resources/Characters/experiments/leafling.png`, painted and rendered by `tools/artgen` (`Leafling.cs`): skin and leaf colors, eyes, mouth, brows and blush are added by the tool |
| Where it shows | Home only, as a pre-rendered flat picture (constitution VII): beside the group early on, beside the player's hero later |
| Licence | Meshy's terms for the owner's Meshy plan. **To confirm by the owner before any release:** on a paid plan the owner holds the rights to the output; on the free plan Meshy outputs are shared under CC BY 4.0, which needs a credit to Meshy. |
| Status | experiment (spec 004 research R17); not part of the project's own art (`tools/artgen/OWNERSHIP.md`) |

The game client never loads the FBX: Unity would show a live 3D model only through a 3D camera, which the
constitution (VII) does not allow on the screens the player navigates. The picture is regenerated with
`dotnet run --project tools/artgen -- build --only experiments` and checked with `-- check --only experiments`.

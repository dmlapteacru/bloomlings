# Ownership of the generated character art

The pictures in `client/Assets/Bloomlings/Art/Characters/Resources/Characters/` (the 2D variant characters, the 3D
family heroes and the group picture) are the Bloomlings project's own work. `tools/artgen` draws them from code in this
repository:
- the 2D characters are vector shapes, gradients and faces written in `Characters2D.cs`;
- the 3D heroes are signed-distance shapes rendered by the raymarcher in `Heroes3D.cs` and `Sdf.cs`.

No third-party picture, model, texture, font or other material goes into them. The fonts of the review sheet are the
game's bundled Nunito files (SIL OFL 1.1, see `client/THIRD_PARTY_NOTICES.md`); they are not part of the pictures.

The owner's reference images (screenshots of other games shown in conversation) set a direction only: chubby cartoon
characters on light tiles, and lit heroes on a stone pedestal. No reference image, and no Colony Flow! (ABI Games)
material, was traced, copied or used as input.

`manifest.json` in the art folder lists every generated file with its SHA-256. The originality test of the content
suite accepts exactly those files, and `dotnet run --project tools/artgen -- check` proves they still come from this
tool.

The owner's own 3D pictures (spec 005 `pictures.md` section A) may sit in the same folder. They are not covered by this
record: `adopt` lists each one in `manifest.json` with `"source": "owner"` and its own source record (`"record"`, for
example `models/owner-pictures.md`: the tool, the author and the licence), and the originality test accepts it only
while that record exists (README, "The owner's pictures").

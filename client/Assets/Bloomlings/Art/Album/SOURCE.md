# Scene Album pictures (spec 007-scene-album)

The project owner's pictures, delivered on 2026-11-02 in the conversation:

- `scene-album-layered-v1` (unpacked to `content/work/uploads-1102/scene-album/`, not kept in the repository): two scenes,
  `forest` and `airmail`, each a `background.png` and four transparent object PNGs on one 853 × 1844 canvas, with
  `scene.json` (layer order, object → level / picture, visibleRect). The pack's README: the backgrounds were regenerated
  without the collectible objects and the objects extracted / reconstructed by a generator from the owner's approved
  pictures, then recomposed and aligned (the owner's generated art).
- the bottom menu's open-album icon (1254 × 1254 PNG with alpha).

Processing (`tools/album/import_scene_pack.py`, deterministic): each background saved as JPEG (quality 90, no chroma
subsampling) at full size as `Resources/Album/<scene>-background.jpg`; each object layer cropped to its alpha bounds plus
2 px as `Resources/Album/<scene>-<object>.png`, its place on the canvas kept in
`Meta/Album/Resources/AlbumCatalog.json` (`rect`); the pack's JPEG previews are not shipped. The icon: alpha below 8
dropped as noise, trimmed, fitted into 512 × 512 with a 20 px margin and saved with an 8-bit palette as
`Art/Icons/Resources/Icons/nav-album.png` (as the other menu icons).

Licence: the owner's pictures, for Bloomlings (the owner's statement, 2026-11-02). Rights of any reference images used to
generate them are the owner's to confirm, as for the other owner pictures.

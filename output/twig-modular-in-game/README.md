# Shared Twig base and two fitted hats

2026-10-17. These captures use the actual game renderer and sprites baked from the owner's three Tripo models.

- `variants.png`: bare base, acorn hat and panama, with the same camera, body scale and foot line.
- `home-base.png`, `home-acorn.png`, `home-panama.png`: the saved selection on Home.
- `shop.png`: free Acorn Hat and Panama Hat under Shop → Cosmetics → Twig.
- `wardrobe-base.png`, `wardrobe-panama.png`: the shared base and selected accessory on the stand.
- `win-panama.png`: a real solved L13 win, advancing progression to L14, retaining the panama.

Only the headwear is extracted from the two separate head generations. The complete bald base is unchanged:
world-coordinate delta below 1e-7, identical UVs, and zero different pixels in fixed visible-face / body comparison
regions across all three baked states. Original archives and modeling references are preserved in the repository.
The derived GLBs can be imported into the owner's Blender master; no second editable Blender master is maintained.

Both hats use the single Hat slot, can be equipped for free before the regular L40 Wardrobe unlock, survive save /
reload, and can be removed back to the bald base. Existing old Daisy Cap / Sprig fitted parts stay saved but are
withheld until refitted. Independent profile avatars and core gameplay figures retain their behavior.

## Initial integration verification

- 50 client tests passed: equipment / save round-trip, free selection, exclusive slot, removal, hidden unfitted parts,
  art metadata, hero motion, Wardrobe and localization.
- `node tools/heroanim/check.mjs` passed, checking source / reference / bake / output hashes, RGBA crops, shared-base
  proof and archived hero art.
- Raw game fixtures 108, 113, 115, 118–124 passed at 19.5:9, including actual Shop taps, removal / restart and win.
- Final packed-asset fixtures 113, 120–124 passed at 16:9, 19.5:9 and 21:9 (18 screen captures).
- Loading checks retained the previous complete page across 11 cold destinations, 120 staged frames and 154
  resources. 100 live-preview frames continued through 9,147 delayed transient-effect draws.
- The five packed PNG payloads match their full-color runtime masters byte for byte: 371,238 bytes total.

This is an unrigged visual trial using static poses and existing subtle 2D breathing. Source ZIPs / FBXs / GLBs stay
outside Android assets. No APK was built; desktop renderer / packaging checks do not measure device frame times.

Source mapping and rebuild commands: `tools/heroanim/models/twig-modular/README.md`.

## Lower, closer fit

Both hats are now 3.23% narrower and 0.035 world units lower (about 12 pixels in the logical camera cell).
The complete bald base, head proportions, original paint, lighting, camera and foot position remain unchanged.
`fit-before-after.png` compares the previous fit on the left and the updated fit on the right, at identical scale.
The current five PNGs total 363,426 bytes. Updated source / fit / output hashes and shared-base pixel checks pass
`node tools/heroanim/check.mjs`. Original delivery ZIPs and references are unchanged.
The five updated game captures (fixtures 120–124) passed at 19.5:9, including Shop selection, both hats on Home,
Wardrobe and a solved win. Loading checks again passed 11 cold destinations / 120 staged frames / 154 resources,
with 100 live-preview frames continuing while transient effects were delayed. No application logic changed.

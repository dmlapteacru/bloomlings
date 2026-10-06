# tools/appicon: the launcher icons

Cuts the owner's app icon picture (`source/app-icon.png`, spec 005 pictures.md C3: a 1254 × 1254 rounded square on
white) into the launcher icons of both builds. Dev-only; it never ships.

```
dotnet run --project tools/appicon
```

It writes:

- the Unity client's textures in `client/Assets/Bloomlings/Art/Brand/AppIcon/`, which `CiBuild.ApplyIcons` sets as the
  default icon and as Android's adaptive, round and legacy icons:
  - `app-icon.png` (1024, full-bleed);
  - `app-icon-adaptive-background.png` and `app-icon-adaptive-foreground.png` (432, the foreground transparent);
  - `app-icon-round.png` and `app-icon-legacy.png` (432);
- the playtest's mipmaps in `playtest/icon/` (linked by `playtest/Playtest.Shared.props`, so both APKs show it):
  - `ic_launcher.png`, `ic_launcher_round.png` and `ic_launcher_background.png` per density;
  - `mipmap-anydpi-v26/ic_launcher*.xml`, the adaptive icon.

How the picture is cut:

- **Full-bleed square.** The picture inside its rounded corners, 96 px in from each side, so no white corner is left.
  The stores and the launchers mask it themselves; the 1024 file also serves the Play Store's 512 icon, downscaled at
  upload.
- **Adaptive background.** The full-bleed square at 0.71 of the 108 dp layer, so the masked 72 dp in the middle shows
  its middle 94%. A blurred, enlarged copy fills the rest of the layer, which only shows when the launcher moves it.
- **Round and legacy icons.** The full-bleed square in a circle, or in a rounded square (radius 22%), with a 4%
  transparent margin.

After replacing the source, run the tool and commit its outputs. Every texture under `client/Assets` needs its row in
`client/THIRD_PARTY_NOTICES.md` (the originality test).

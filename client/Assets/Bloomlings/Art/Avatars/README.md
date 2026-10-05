# Avatars (the owner's profile pictures)

The owner's avatar pictures of `specs/005-reference-look/pictures.md` (section I; spec 005 FR-037), in
`Resources/Avatars/` by their `AvatarCatalog` names: `{family}_{look}.jpg`, opaque 384 × 384 JPEG (quality 88; the
owner's 1254 × 1254 PNG files scaled down, 24 MB → 540 KB). The four `*_default.jpg` are free; the other ten are sold
for Petals in three tiers (`AvatarTier`: 300, 600 and 1200 by default, Remote Config `economy.price.avatar*`).

How the builds use them:

- Unity loads them from `Resources/Avatars/` (`OwnerArt.Avatar`) and shows them in a round mask
  (`ProfileAvatar`; `OwnerIconImporter` imports them with mipmaps, at most 512 px).
- The playtest embeds the folder (`playtest/android` and `playtest/preview`, `avatars/{name}`) and draws a picture in a
  round clip (`Kit.AvatarPicture`, `IPainter.PushClipRound`).

While a picture is missing, both builds draw the profile hero instead. Each picture needs a row in
`client/THIRD_PARTY_NOTICES.md` and in the source record `tools/artgen/models/owner-pictures.md`; the originality test
fails without it, and `ProfileServiceTests` checks that every avatar has its picture at 384 × 384.

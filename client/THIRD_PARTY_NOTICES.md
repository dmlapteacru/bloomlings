# Third-party files in the client

Every imported art, audio or font file under `client/Assets/` needs a licence record here before it lands
(`specs/001-core-game-mvp/checklists/originality.md`). `OriginalityTests.Client_HasNoImportedArtAudioOrFonts` fails
for any such file that is not listed below with its licence file.

| File | What | Source | Licence | Licence file |
|---|---|---|---|---|
| `client/Assets/Bloomlings/UI/Fonts/Resources/Nunito-ExtraBold.ttf` | Nunito ExtraBold (800), the display font of spec 003 | npm `@expo-google-fonts/nunito` 0.4.2, `800ExtraBold/Nunito_800ExtraBold.ttf`, unmodified | SIL Open Font License 1.1, Copyright 2014 The Nunito Project Authors | `client/Assets/Bloomlings/UI/Fonts/Resources/OFL.txt` |
| `client/Assets/Bloomlings/UI/Fonts/Resources/Nunito-SemiBold.ttf` | Nunito SemiBold (600), the body font of spec 003 | npm `@expo-google-fonts/nunito` 0.4.2, `600SemiBold/Nunito_600SemiBold.ttf`, unmodified | SIL Open Font License 1.1, Copyright 2014 The Nunito Project Authors | `client/Assets/Bloomlings/UI/Fonts/Resources/OFL.txt` |

The OFL lets the fonts be bundled with the game, as long as the licence ships with them (it does, as a Resources text
asset) and a modified font is not sold or renamed as Nunito. The files are never modified.

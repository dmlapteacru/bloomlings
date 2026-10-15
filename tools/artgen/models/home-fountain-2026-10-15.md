# Home courtyard and fountain light refinement — 2026-10-15

Generated with OpenAI ImageGen for Bloomlings at the owner's request: the newly approved garden backgrounds stay, while the Home fountain must match the new courtyard, the obvious blur must be evened out, and the scene must be lighter. All references are existing project artwork.

The courtyard retains its paving, side-post composition and mint/jade garden, with readable broad tree forms and gentle atmospheric distance instead of smeared depth-of-field blobs. There is no open blue sky. The basin, front-corner stone clusters and separate lotus were edited from their corresponding existing transparent layers: lighter warm ivory stone, mint-turquoise water, jade foliage, blush/ivory flowers, diffuse morning light and coherent surface shading. The basin remains empty at its center because the lotus is a separate foreground layer. The middle gap between front corners remains transparent. No characters or interface artwork is baked into these images.

Production preparation: the opaque courtyard is normalized to the existing 852 × 1846 canvas and encoded as optimized RGB JPEG quality 90 without chroma subsampling. PNGs retain straight RGBA transparency, use the visible alpha bounds (threshold 6/255, the existing Home import convention) and are normalized to the original 804 × 413, 852 × 250 and 296 × 217 layer sizes with alpha-aware Lanczos resampling. The original layer coordinates, hero feet, shadows, animation cells and draw order are unchanged. The committed images in `tools/heroanim/backgrounds/` are the production inputs. Original generator PNGs remain in execution artifacts and are not bundled.

Home and its three new foreground layers now bypass the old additional Gaussian blur and 0.86 color multiplier in `tools/heroanim/backdrops.mjs`. Their authored soft shading remains. Other backgrounds keep the same finish and bytes. All source/output hashes remain verified by `node tools/heroanim/check.mjs`.

| Source / runtime basename | ImageGen output | Original PNG SHA-256 |
|---|---|---|
| `home.jpg` | `exec-983ceaac-fe39-4db8-9b6e-d7f73087907a.png` | `7eec67cca10df992b5aac1c8c5275659619bbd2a3efd75946fdc921910ea62a4` |
| `home-fountain-back.png` | `exec-f21dfaff-4d17-4e81-9fdc-e2500d83ea04.png` | `4b7f8140bfa2d0e19f34282c77120e438fd29489a1d862d074af38328e52a72c` |
| `home-fountain-front.png` | `exec-539c0066-af90-446e-8e17-76ac14b934ff.png` | `a0b3a99ef17c6fc05aed27e63a63116dab4758e4aad1e70152be741c8dfe6c3a` |
| `home-lotus.png` | `exec-c8074ca1-56dd-433f-b12e-c6ab6ac7f4fe.png` | `8d449ddd358dcca438e35d5d835a307a0860655e76f07ae985acf20f94cb862e` |

Project-generated artwork; no imported third-party or stock material. Source images are in `tools/heroanim/backgrounds/`; runtime counterparts are in `client/Assets/Bloomlings/Art/Backgrounds/Resources/Backgrounds/`.

## Lotus color refinement — 2026-10-15

The owner found the initial blush/ivory lotus too pale. OpenAI ImageGen edited the previous lotus (`exec-7a7dae9c-bed5-4994-a961-89633ef450bf.png`) into richer rose/coral pink petals with deeper folds and a warm golden center. The rounded petal structure, jade leaf base, transparent background, 296 × 217 production canvas and original placement are retained. The same alpha threshold and resampling preparation described above is used; no extra blur or dimming is applied.

## Fountain side contour repair — 2026-10-15

The owner noticed straight clipped sides on the fountain stones. ImageGen edited the prior foreground corners and basin into complete rounded rock/leaf silhouettes. Production preparation keeps a transparent safety margin after thresholding and alpha-aware resampling: 12 px horizontally / 4 px vertically in the 852 × 250 foreground canvas, and 6 px / 4 px in the 804 × 413 basin canvas. The center foreground gap remains transparent. Layer coordinates, lotus, hero placement and draw order are unchanged. The new input SHA-256 values are recorded above and in `layers.json`; source and runtime hashes remain checked.

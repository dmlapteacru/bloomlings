# Bloomlings achievement badge source

The 23 source PNGs in `output/badges-v1/` were generated for this Bloomlings project with OpenAI ImageGen on 2026-10-11. The art follows Bloomlings' own garden palette and characters. Colony Flow informed the game concept; no artwork from that game was used as image input or copied.

`tools/pack_badges.py` scales the source art to 256 × 256 transparent PNGs in `client/Assets/Bloomlings/Art/Badges/Resources/Badges/`. The generated `manifest.json` records each runtime file and SHA-256. The runtime files are project-owned art; they contain no third-party font or image material. The original high-resolution files remain outside the Unity client and APK.

Four hero milestone medals were generated with OpenAI ImageGen on 2026-10-15 from the project’s own hero poses and medal style. Source atlas: `output/hero-milestones/badges-atlas.png`. Equal quadrants are packed through the same 256 px runtime pipeline. Sprig/Bloom/Drop/Twig correspond to levels 100/500/1000/2000. There are now 27 runtime badges.


## Permanent Twig V2 identity migration — 2026-10-17

The owner approved TwigHeroV2 in game and requested full replacement of the previous Twig. Current modeling references
are preserved under `tools/heroanim/references/twig-v2/` with hashes. Full-resolution portrait / medal sources in its
`identity/` folder were generated with OpenAI image generation from the approved new Twig reference. New runtime
avatars retain their three previous IDs, prices and themes; `pack-twig-identity.py` downsamples them to 384px JPEGs.
`pack_badges.py --only hero_twig` overrides the historical medal atlas with the new source, producing its 256px RGBA
badge without changing achievement rules. Solo / cheer / group fallback pictures use current baked production sprites;
Twig's blank-face fallback is derived from its actual mesh in an offline renderer without changing source texture.
The old Twig frame library / fitted states are retired outside Resources in `tools/heroanim/archive/twig/` (263 PNGs,
11,631,125 bytes). Only TwigHeroV2 is active; no previous Twig publisher may write to production. Existing saves map
the retired flower cap to Panama without spending or resetting progress. The owner explicitly keeps L2000's mosaic
unchanged. No source models / references enter APK resources. No APK is built here.

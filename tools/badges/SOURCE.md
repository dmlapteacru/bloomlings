# Bloomlings achievement badge source

The 23 source PNGs in `output/badges-v1/` were generated for this Bloomlings project with OpenAI ImageGen on 2026-10-11. The art follows Bloomlings' own garden palette and characters. Colony Flow informed the game concept; no artwork from that game was used as image input or copied.

`tools/pack_badges.py` scales the source art to 256 × 256 transparent PNGs in `client/Assets/Bloomlings/Art/Badges/Resources/Badges/`. The generated `manifest.json` records each runtime file and SHA-256. The runtime files are project-owned art; they contain no third-party font or image material. The original high-resolution files remain outside the Unity client and APK.

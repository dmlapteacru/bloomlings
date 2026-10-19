# Early-game competitor research and v4 proposal

Public fan-guide observations, our exact v3 field baseline and separately labelled owner v2 playtest data. No APK, game rules, production art, economy or existing pilot is changed by these tools.

```bash
python3 tools/colony-early-research/capture.py /tmp/colony-flow-round2
python3 tools/colony-early-research/derive.py
python3 tools/colony-early-research/build.py
node tools/colony-early-research/browser-check.mjs
```

`capture.py` requires lxml/Pillow and writes fresh public pages/media to `/tmp`. It reuses the earlier local image cache only when the URL on the fresh page and the prior byte hash match; otherwise it downloads the image. The checked-in `product/audits/colony-flow-early-2026-10-19` snapshot contains extracted text, URLs, original page hashes, image hashes and selected screenshots. Raw temporary HTML is not executable input. Page hashes may change when the public site updates.

`derive.py` regenerates our baseline, the proposed 30-row progression and a deliberately unapproved palette diagnostic. Its CVD approximation uses linear sRGB, severity-100 matrices from Machado et al. (2009), D65 CIELAB and Euclidean ΔE76. It does not compute CIEDE2000 or certify accessibility. The current green/amber risk is intentionally exposed instead of silently declaring this a finished palette.

`build.py` creates the offline mobile HTML. Case stills and their sources are research illustrations only, never imported into game assets. The timer is arithmetic for a proposed work model, not a playable level or measured competitor speed. The roadmap is a proposal, not generated/certified definitions.

The HTML/JSON SHA-256 values and mobile browser checks live in `product/previews/gameplay-v4-plan/validation.json`. A later playable v4 requires new definitions, the seventh material in both simulators and validators, an agreed timing model and actual human playtesting.

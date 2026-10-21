#!/usr/bin/env python3
"""Render authored board data for visual review; never modifies game assets."""
import json
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "product/previews/gameplay-pilot-v7/art-contact.png"
levels = json.loads((ROOT / "content/pilot-v7/levels.json").read_text())["levels"]
colours = {"G": "#78C04A", "P": "#EB799C", "B": "#36B7DA", "A": "#A45F28",
           "C": "#F5E6BD", "D": "#334B48", "V":"#7851A5", "#": "#b6d6cc", ".": "#e4ead7"}
font = ImageFont.truetype(str(ROOT / "client/Assets/Bloomlings/UI/Fonts/Resources/Nunito-SemiBold.ttf"), 13)
image = Image.new("RGB", (1320, 1390), "#fffaf0")
draw = ImageDraw.Draw(image)
for i, level in enumerate(levels):
    x, y = (i % 6) * 220 + 10, (i // 6) * 278 + 25
    cell = min(200 // level["w"], 242 // level["h"])
    ox = x + (200 - level["w"] * cell) // 2
    for ry, row in enumerate(reversed(level["rowsBottomFirst"])):
        for rx, code in enumerate(row):
            draw.rectangle((ox + rx * cell, y + ry * cell, ox + (rx + 1) * cell - 1,
                            y + (ry + 1) * cell - 1), fill=colours[code])
    draw.text((x, y - 21), f'L{level["id"]} · {level["title"]}', font=font, fill="#34493d")
OUT.parent.mkdir(parents=True, exist_ok=True)
image.save(OUT)
print(OUT)

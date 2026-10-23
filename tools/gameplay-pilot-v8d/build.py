#!/usr/bin/env python3
"""Build a single offline HTML. Does not install levels into the game."""
import base64
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SOURCE = Path(__file__).resolve().parent
OUT = ROOT / "product/previews/gameplay-pilot-v8d/index.html"


def data_uri(path, mime):
    return f"data:{mime};base64," + base64.b64encode(path.read_bytes()).decode("ascii")


def script_json(value):
    return json.dumps(value, ensure_ascii=False, separators=(",", ":")).replace("<", "\\u003c")


def main():
    dataset = json.loads((ROOT / "content/pilot-v8d/levels.json").read_text())
    assert [level["id"] for level in dataset["levels"]] == list(range(1, 31))
    rules = (SOURCE / "rules.cjs").read_text()
    dataset["rulesHash"] = hashlib.sha256(rules.encode()).hexdigest()
    timeline = (SOURCE / "timeline.cjs").read_text()
    dataset["timelineHash"] = hashlib.sha256(timeline.encode()).hexdigest()
    palette = (SOURCE / "palette.cjs").read_text()
    dataset["paletteHash"] = hashlib.sha256(palette.encode()).hexdigest()
    clock = (SOURCE / "clock.cjs").read_text()
    dataset["clockHash"] = hashlib.sha256(clock.encode()).hexdigest()
    heroes = ROOT / "client/Assets/Bloomlings/Art/Heroes/Resources/HeroMotion"
    assets = {name: data_uri(heroes / f"homehero-{name}-001.png", "image/png")
              for name in ("sprig", "bloom", "drop", "twig")}
    assets["garden"] = data_uri(ROOT / "client/Assets/Bloomlings/Art/Backgrounds/Resources/Backgrounds/gameplay-orchard.jpg", "image/jpeg")
    fonts = ROOT / "client/Assets/Bloomlings/UI/Fonts/Resources"
    css = (SOURCE / "style.css").read_text().replace("__FONT__", data_uri(fonts / "Nunito-SemiBold.ttf", "font/ttf"))
    css = css.replace("__FONT_BOLD__", data_uri(fonts / "Nunito-ExtraBold.ttf", "font/ttf"))
    replacements = {"/*__STYLE__*/": css, "/*__ASSETS__*/": script_json(assets),
                    "/*__DATA__*/": script_json(dataset), "/*__RULES__*/": rules,
                    "/*__UI__*/": (SOURCE / "ui.js").read_text(), "/*__TIMELINE__*/": timeline, "/*__PALETTE__*/": palette, "/*__CLOCK__*/": clock}
    page = (SOURCE / "page.html").read_text()
    for marker, value in replacements.items():
        assert page.count(marker) == 1, marker
        page = page.replace(marker, value)
    assert "/*__" not in page
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(page)
    print(f"Built {OUT.relative_to(ROOT)}: {OUT.stat().st_size:,} bytes. 30 levels, embedded assets, no network dependencies.")


if __name__ == "__main__":
    main()

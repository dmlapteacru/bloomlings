"""Make small, reproducible runtime badge assets from the full-size concept art.

The originals stay in output/badges-v1; only these 256 px PNGs enter Unity
Resources and the playtest packages.
"""

from pathlib import Path
import hashlib
import json
from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "output" / "badges-v1"
DESTINATION = ROOT / "client" / "Assets" / "Bloomlings" / "Art" / "Badges" / "Resources" / "Badges"
SIZE = 256


def pack() -> None:
    DESTINATION.mkdir(parents=True, exist_ok=True)
    source_bytes = 0
    packed_bytes = 0
    count = 0
    entries = []
    for category in ("achievements", "prestige", "collections"):
        for source in sorted((SOURCE / category).glob("*.png")):
            with Image.open(source) as original:
                image = original.convert("RGBA")
            image.thumbnail((SIZE, SIZE), Image.Resampling.LANCZOS)
            canvas = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
            canvas.alpha_composite(image, ((SIZE - image.width) // 2, (SIZE - image.height) // 2))
            # Keep smooth translucent outlines. PNG optimization removes redundant data
            # without baking lossy artifacts into the only runtime copy.
            target = DESTINATION / source.name
            canvas.save(target, format="PNG", optimize=True, compress_level=9)
            source_bytes += source.stat().st_size
            packed_bytes += target.stat().st_size
            count += 1
            entries.append({
                "path": target.name,
                "width": SIZE,
                "height": SIZE,
                "sha256": hashlib.sha256(target.read_bytes()).hexdigest(),
                "slot": "profile.achievement." + target.stem,
                "source": "owner",
                "record": "tools/badges/SOURCE.md",
            })
    if count != 23 or packed_bytes > 3_000_000:
        raise ValueError(f"Expected 23 badges within 3 MB; got {count} and {packed_bytes:,} bytes")
    (DESTINATION / "manifest.json").write_text(
        json.dumps({"tool": "tools/pack_badges.py", "version": 1, "files": entries}, indent=2) + "\n",
        encoding="utf-8",
    )
    print(f"{count} badges: {source_bytes:,} -> {packed_bytes:,} bytes ({packed_bytes / source_bytes:.1%})")


if __name__ == "__main__":
    pack()

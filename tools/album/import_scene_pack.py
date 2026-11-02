#!/usr/bin/env python3
"""Imports the owner's layered Scene Album pack into the client (spec 007-scene-album).

Usage: python3 -I tools/album/import_scene_pack.py <pack dir> [<nav icon png>]

<pack dir> is an unpacked `scene-album-layered-v1` pack (manifest.json and one folder per scene with background.png and
one full-canvas transparent PNG per object). For every scene it writes, under
client/Assets/Bloomlings/Art/Album/Resources/Album/ (one flat folder, as the playtest's asset folders are listed):
  - <scene>-background.jpg  the opaque background at full size (quality 90, no chroma subsampling);
  - <scene>-<object>.png    each object layer cropped to its alpha bounds plus 2 px (the crop's place on the canvas is kept in
                    the catalog's `rect`, so the layers still line up exactly; the README of the pack allows cropping
                    together with the offset).
The JPEG previews of the pack are not shipped. Then it writes the catalog
client/Assets/Bloomlings/Meta/Album/Resources/AlbumCatalog.json (scenes in TITLES order, objects in the pack's
layerOrder, each bound to its picture code; the level is looked up in the published round levels at runtime).
With <nav icon png> it also writes the bottom menu's icon Icons/nav-album.png (alpha below 8 dropped as noise, trimmed,
fitted into 512 x 512 with a 20 px margin, 8-bit palette, as the other menu icons D9-D13).
"""
import json
import os
import sys

from PIL import Image

# The scenes' and objects' localization keys and English names (Strings_en.csv carries the same names).
TITLES = {
    "forest": ("Forest Glade", {"owl": "Owl", "hive": "Beehive", "fox": "Fox", "butterfly": "Butterfly"}),
    "airmail": ("Air Mail Garden", {"mill": "Windmill", "airship": "Airship", "fox": "Postman Fox", "bicycle": "Flower Bicycle"}),
}


def main(pack, icon=None):
    repo = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
    out = os.path.join(repo, "client/Assets/Bloomlings/Art/Album/Resources/Album")
    manifest = json.load(open(os.path.join(pack, "manifest.json"), encoding="utf-8"))
    by_id = {s["id"]: s for s in manifest["scenes"]}
    scenes = []
    for sid in TITLES:
        scene = by_id[sid]
        os.makedirs(out, exist_ok=True)
        w, h = scene["canvas"]["width"], scene["canvas"]["height"]
        background = Image.open(os.path.join(pack, sid, scene["background"])).convert("RGB")
        assert background.size == (w, h), sid
        background.save(os.path.join(out, sid + "-background.jpg"), quality=90, optimize=True, subsampling=0)
        objects = {o["id"]: o for o in scene["objects"]}
        entries = []
        for oid in scene["layerOrder"]:
            o = objects[oid]
            layer = Image.open(os.path.join(pack, sid, o["file"])).convert("RGBA")
            assert layer.size == (w, h) and o["offset"] == [0, 0], (sid, oid)
            left, top, right, bottom = layer.getchannel("A").getbbox()
            box = (max(0, left - 2), max(0, top - 2), min(w, right + 2), min(h, bottom + 2))
            layer.crop(box).save(os.path.join(out, sid + "-" + oid + ".png"), optimize=True)
            entries.append({
                "id": oid,
                "title": "album.object." + sid + "." + oid,
                "picture": o["picture"],
                "levelHint": o["level"],
                "layer": sid + "-" + oid,
                "rect": [box[0], box[1], box[2] - box[0], box[3] - box[1]],
            })
        scenes.append({"id": sid, "title": "album.scene." + sid, "canvas": [w, h], "background": sid + "-background", "objects": entries})
    catalog = {
        "format": "bloomlings-album-catalog.v1",
        "source": "the owner's scene-album-layered-v1 pack of 2026-11-02 (tools/album/import_scene_pack.py)",
        "locked": {"saturation": 0, "brightness": 0.82},
        "scenes": scenes,
    }
    target = os.path.join(repo, "client/Assets/Bloomlings/Meta/Album/Resources/AlbumCatalog.json")
    os.makedirs(os.path.dirname(target), exist_ok=True)
    with open(target, "w", encoding="utf-8", newline="\n") as f:
        json.dump(catalog, f, indent=2)
        f.write("\n")
    if icon:
        o = Image.open(icon).convert("RGBA")
        alpha = o.getchannel("A").point(lambda v: v if v >= 8 else 0)
        o.putalpha(alpha)
        o = o.crop(alpha.getbbox())
        s = min(472 / o.width, 472 / o.height)
        o = o.resize((round(o.width * s), round(o.height * s)), Image.LANCZOS)
        canvas = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
        canvas.paste(o, ((512 - o.width) // 2, (512 - o.height) // 2), o)
        canvas.quantize(colors=255, method=Image.Quantize.FASTOCTREE, dither=Image.Dither.NONE).save(
            os.path.join(repo, "client/Assets/Bloomlings/Art/Icons/Resources/Icons/nav-album.png"), optimize=True)


if __name__ == "__main__":
    main(*sys.argv[1:])

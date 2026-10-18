"""Read the accepted deliveries into one disposable rig per family, preserving rest geometry and UVs.

Usage: blender -b --factory-startup -t 2 --python-exit-code 1 --python tools/heroanim/rigging/assemble.py -- twig
The owner's Blender project remains the master; no original delivery is overwritten.
"""
import bpy
import hashlib
import json
import sys
import zipfile
from pathlib import Path

REPO = Path(__file__).resolve().parents[3]
TOOLS = REPO / "tools/heroanim"
FAMILY = sys.argv[sys.argv.index("--") + 1]
assert FAMILY in ("twig", "sprig", "bloom", "drop")
OUT = REPO / "content/work/rigging" / FAMILY
OUT.mkdir(parents=True, exist_ok=True)
SOURCE = TOOLS / "models" / (FAMILY + "-v2")
SHA = lambda p: hashlib.sha256(Path(p).read_bytes()).hexdigest()
report = {"family": FAMILY, "sources": [], "parts": [], "skeleton": []}


def unpack(archive, digest, destination):
    assert SHA(archive) == digest, (archive, "changed source")
    report["sources"].append({"path": str(archive.relative_to(REPO)), "sha256": digest})
    with zipfile.ZipFile(archive) as z:
        assert all((destination / e.filename).resolve().is_relative_to(destination.resolve()) for e in z.infolist())
        z.extractall(destination)


if FAMILY == "drop":
    pin = json.loads((SOURCE / "manifest.json").read_text())
    archive = SOURCE / "products/DropHeroV2_FBX.zip"
    digest = next(e["sha256"] for e in pin["files"] if e["path"] == "products/DropHeroV2_FBX.zip")
    unpack(archive, digest, OUT / "source")
    folder = OUT / "source/DropHeroV2"
    delivery = json.loads((folder / "inventory.json").read_text())
    sources = [(Path(e["path"]).stem.removeprefix("DropHeroV2_"), folder / e["path"], e["sha256"])
               for e in delivery["files"] if "@Anims" not in e["path"]]
    animation = folder / "FBX/DropHeroV2@Anims.fbx"
else:
    record = json.loads((SOURCE / "source-manifest.json").read_text())
    for entry in record.get("archives", [record.get("archive")]):
        unpack(SOURCE / entry["path"], entry["sha256"], OUT / "source")
    manifest = OUT / "source" / record["exportManifest"]
    folder = manifest.parent
    delivery = json.loads(manifest.read_text())
    sources = [("base", folder / delivery["model"], None)] + [
        (i["id"], folder / i["file"], None) for i in delivery["items"]]
    for entry in record.get("additions", []):
        path = SOURCE / entry["path"]
        assert SHA(path) == entry["sha256"]
        report["sources"].append({"path": str(path.relative_to(REPO)), "sha256": entry["sha256"]})
        sources = [s for s in sources if s[0] != entry["id"]] + [(entry["id"], path, entry["sha256"])]
    animation = folder / delivery["anims"]

bpy.ops.wm.read_factory_settings(use_empty=True)
arm = None
for key, file, digest in sources:
    if digest:
        assert SHA(file) == digest
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=str(file), use_anim=False)
    added = set(bpy.data.objects) - before
    rigs = [o for o in added if o.type == "ARMATURE"]
    assert len(rigs) == 1
    rig = rigs[0]
    rig.data.pose_position = "REST"
    rig.animation_data_clear()
    if arm is None:
        arm = rig
        arm.name = FAMILY.title() + "HeroV2Rig"
        arm.data.name = FAMILY.title() + "HeroV2Skeleton"
    else:
        assert set(b.name for b in arm.data.bones) == set(b.name for b in rig.data.bones)
        assert max((a - b).length for bone in arm.data.bones
                   for a, b in zip(bone.matrix_local, rig.data.bones[bone.name].matrix_local)) < 1e-5
    for obj in [o for o in added if o.type == "MESH"]:
        obj["part_key"] = obj.name.removeprefix("DropHeroV2_") if FAMILY == "drop" else key
        obj.name = FAMILY.title() + "HeroV2_" + obj["part_key"]
        world = obj.matrix_world.copy()
        obj.parent = arm
        obj.matrix_world = world
        mods = [m for m in obj.modifiers if m.type == "ARMATURE"]
        assert len(mods) == 1
        mods[0].object = arm
        positions = [tuple(obj.matrix_world @ v.co) for v in obj.data.vertices]
        uv = [tuple(v.uv) for v in obj.data.uv_layers.active.data]
        report["parts"].append({"key": obj["part_key"], "name": obj.name,
                                "vertices": len(positions), "geometrySha256": SHA(file),
                                "worldVerticesSha256": hashlib.sha256(repr(positions).encode()).hexdigest(),
                                "uvSha256": hashlib.sha256(repr(uv).encode()).hexdigest(),
                                "shapeKeys": [k.name for k in obj.data.shape_keys.key_blocks] if obj.data.shape_keys else []})
    for obj in added:
        if obj.type != "MESH" and obj != arm:
            bpy.data.objects.remove(obj, do_unlink=True)

# Actions are retained for comparison, never mistaken for the corrected action set.
before = set(bpy.data.objects)
bpy.ops.import_scene.fbx(filepath=str(animation), use_anim=True)
for obj in set(bpy.data.objects) - before:
    bpy.data.objects.remove(obj, do_unlink=True)
for action in list(bpy.data.actions):
    if not any("pose.bones" in f.data_path for f in action.fcurves):
        bpy.data.actions.remove(action)
        continue
    suffix = action.name.split("_")[-1]
    action.name = "Source_" + FAMILY.title() + "_" + suffix
    action.use_fake_user = True
report["sourceActions"] = [{"name": a.name, "frames": list(a.frame_range)} for a in bpy.data.actions]
for b in arm.data.bones:
    report["skeleton"].append({"name": b.name, "parent": b.parent.name if b.parent else None,
                              "matrix": [list(v) for v in b.matrix_local]})
arm.animation_data_clear()
arm.data.pose_position = "REST"
bpy.context.scene.render.fps = 24
bpy.ops.wm.save_as_mainfile(filepath=str(OUT / "source-cache.blend"))
(OUT / "assembly.json").write_text(json.dumps(report, indent=2) + "\n")
print("ASSEMBLED", FAMILY, len(report["parts"]), "parts,", len(report["sourceActions"]), "source actions", flush=True)

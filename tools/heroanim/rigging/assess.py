"""Measure source/corrected skinning under exactly the same diagnostic and baseline poses."""
import bpy
import json
import hashlib
import sys
from pathlib import Path

import numpy as np

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
from skinning import canonical
from surface_integrity import verify as verify_surface, attribute

FAMILY = sys.argv[sys.argv.index("--") + 1]
ROOT = HERE.parents[2] / "content/work/rigging" / FAMILY


def weights_of(obj):
    names = [g.name for g in obj.vertex_groups]
    a = np.zeros((len(obj.data.vertices), len(names)), dtype=np.float32)
    for v in obj.data.vertices:
        for g in v.groups:
            a[v.index, g.group] = g.weight
    return names, a


def assign(obj, names, weights):
    for g in list(obj.vertex_groups):
        obj.vertex_groups.remove(g)
    for column, name in enumerate(names):
        group = obj.vertex_groups.new(name=name)
        for i in np.flatnonzero(weights[:, column] > 0):
            group.add([int(i)], float(weights[i, column]), "REPLACE")


def coords(obj):
    ev = obj.evaluated_get(bpy.context.evaluated_depsgraph_get())
    mesh = ev.to_mesh()
    a = np.empty(len(mesh.vertices) * 3, dtype=np.float32)
    mesh.vertices.foreach_get("co", a)
    a = a.reshape(-1, 3)
    m = np.array(ev.matrix_world)
    a = a @ m[:3, :3].T + m[:3, 3]
    ev.to_mesh_clear()
    return a


bpy.ops.wm.open_mainfile(filepath=str(ROOT / "rig-cache.blend"))
arm = next(o for o in bpy.context.scene.objects if o.type == "ARMATURE")
parts = {o["part_key"]: o for o in bpy.context.scene.objects if o.type == "MESH"}
fixed = {key: weights_of(o) for key, o in parts.items()}
arm.data.pose_position = "REST"
bpy.context.view_layer.update()
original = {}
before = set(bpy.data.objects)
with bpy.data.libraries.load(str(ROOT / "source-cache.blend"), link=False) as (data_from, data_to):
    wanted = {p['name'] for p in json.loads((ROOT / "assembly.json").read_text())["parts"]}
    data_to.objects = [name for name in data_from.objects if name in wanted]
for obj in data_to.objects:
    bpy.context.scene.collection.objects.link(obj)
    w = obj.matrix_world.copy()
    obj.parent = arm
    obj.matrix_world = w
    for modifier in obj.modifiers:
        if modifier.type == "ARMATURE":
            modifier.object = arm
    if obj.data.shape_keys:
        for key in obj.data.shape_keys.key_blocks:
            key.value = 0
    original[obj['part_key']] = obj
for obj in set(bpy.data.objects) - before:
    if obj.type != 'MESH':
        bpy.data.objects.remove(obj, do_unlink=True)
bpy.context.view_layer.update()
rest = {key: coords(o) for key, o in parts.items()}
source_rest = {key: (coords(o), np.array([v.uv[:] for v in o.data.uv_layers.active.data])) for key, o in original.items()}
edges, lengths, core, head, sole = {}, {}, {}, {}, {}
cuff = {}
from cuff_ownership import profile as cuff_profile
report = {"family": FAMILY, "restMaxError": 0, "uvMaxError": 0, "parts": {}, "measurements": {}, "summary": {}}
rig_report=json.loads((ROOT/'rig-report.json').read_text())
contracts={p['key']:p.get('contactRepair') for p in rig_report['parts']}
report['assessmentScriptSha256']=hashlib.sha256(Path(__file__).read_bytes()).hexdigest()
report['surfaceIntegritySha256']=hashlib.sha256((HERE/'surface_integrity.py').read_bytes()).hexdigest()
report['rigReportSha256']=hashlib.sha256((ROOT/'rig-report.json').read_bytes()).hexdigest()
for key, obj in parts.items():
    a = rest[key]
    error, uv_error, contact_check = verify_surface(obj, original[key], a, source_rest[key][0], contracts[key], FAMILY)
    report["restMaxError"] = max(report["restMaxError"], error)
    report["uvMaxError"] = max(report["uvMaxError"], uv_error)
    names, w = fixed[key]
    assert np.isfinite(w).all() and (w >= 0).all()
    assert np.max(np.abs(w.sum(axis=1) - 1)) < 1e-5
    assert np.max((w > 0).sum(axis=1)) <= 4
    assert {n for i, n in enumerate(names) if np.any(w[:, i] > 0)} <= {b.name for b in arm.data.bones if b.use_deform}
    edge = np.array([e.vertices[:] for e in obj.data.edges])
    length = np.linalg.norm(a[edge[:, 0]] - a[edge[:, 1]], axis=1)
    keep = length > .0001
    edges[key], lengths[key] = edge[keep], length[keep]
    u, d, z = canonical(a, FAMILY).T
    width = .125 if FAMILY == "drop" else .16
    core[key] = (np.abs(u) <= width) & (z > .14) & (z < .40) if key in ("Jacket", "Coat", "Overalls") else np.zeros(len(a), bool)
    head[key] = np.array([names.index("Head") in np.flatnonzero(row > .99999) if "Head" in names else False for row in w])
    sole[key] = (z < .025) & (key == "base" or key.startswith("Base_Leg_"))
    cuff[key] = np.zeros(len(a), bool)
    authored = cuff_profile(original[key], FAMILY, key)
    if key in ('Jacket', 'Coat', 'Overalls'):
        source_names, source_weights = weights_of(original[key])
        fore = sum((source_weights[:, i] for i, n in enumerate(source_names) if n.startswith(('Forearm.', 'Hand.'))), np.zeros(len(source_weights)))
        source_indices = np.flatnonzero(fore > .98)
        used = np.zeros(len(a), bool)
        used[np.unique(np.concatenate([np.array(p.vertices[:]) for p in obj.data.polygons]))] = True
        cuff[key][source_indices] = True
        if authored is not None:
            assert contracts[key]['authoredCuffOwnership'] == authored
            authored_ids = authored['vertexIds']
            source_arm = sum((source_weights[:, i] for i, n in enumerate(source_names)
                              if n.startswith(('UpperArm.', 'Forearm.', 'Hand.'))), np.zeros(len(source_weights)))
            assert int((source_arm[authored_ids] < 1e-6).sum()) == authored['originalZeroArmVertices']
            assert np.all(used[authored_ids]), 'An authored cuff vertex was removed'
            cuff[key][authored_ids] = True
        cuff[key] &= used
    torso_columns = [i for i, n in enumerate(names) if n in ('Hips','Spine','Chest')]
    cuff_torso = float(w[cuff[key]][:, torso_columns].sum(1).max(initial=0))
    assert cuff_torso < 1e-5, (FAMILY, key, 'source cuff pinned to torso', cuff_torso)
    hand_columns = [i for i, n in enumerate(names) if n.startswith('Hand.')]
    assert float(w[cuff[key]][:, hand_columns].sum(1).max(initial=0)) < 1e-5
    report["parts"][key] = {"vertices": len(a), "restMaxError": error, "uvMaxError": uv_error,
                            "coreVertices": int(core[key].sum()), "rigidHeadVertices": int(head[key].sum()), "contactRepair": contact_check, "sourceCuffVertices": int(cuff[key].sum()), "cuffMaxTorsoWeight": cuff_torso,
                            "authoredCuffVertices": len(authored['vertexIds']) if authored else 0,
                            "authoredCuffOriginallyZeroArmVertices": authored['originalZeroArmVertices'] if authored else 0}

for version, objects in [("before", original), ("after", parts)]:
    active_rest = {key: coords(o) for key, o in objects.items()}
    active_edges, active_lengths, active_core, active_sole, active_cuff_edges = {}, {}, {}, {}, {}
    for key, obj in objects.items():
        a = active_rest[key]
        e = np.array([e.vertices[:] for e in obj.data.edges])
        length = np.linalg.norm(a[e[:, 0]] - a[e[:, 1]], axis=1)
        valid = length > .0001
        active_edges[key], active_lengths[key] = e[valid], length[valid]
        active_cuff_edges[key] = np.all(cuff[key][:len(a)][e[valid]], axis=1)
        u, d, z = canonical(a, FAMILY).T
        active_core[key] = (np.abs(u) <= (.11 if FAMILY == "drop" else .15)) & (z > .14) & (z < .40) if key in ("Jacket", "Coat", "Overalls") else np.zeros(len(a), bool)
        if version == 'after' and 'rig_arm_ownership' in obj.data.attributes:
            active_core[key] &= attribute(obj.data, 'rig_arm_ownership') < .001
        active_sole[key] = (z < .025) & (key == "base" or key.startswith("Base_Leg_"))
    arm.data.pose_position = "POSE"
    measurements = []
    for action in sorted(bpy.data.actions, key=lambda a: a.name):
        arm.animation_data.action = action
        selected = sorted(set(round(float(v)) for v in np.linspace(*action.frame_range, 3 if '--quick' in sys.argv else 13)))
        for frame in selected:
            bpy.context.scene.frame_set(frame)
            bpy.context.view_layer.update()
            row = {"clip": action.name, "frame": frame, "parts": {}}
            for key, obj in objects.items():
                a = coords(obj)
                assert np.isfinite(a).all()
                e = active_edges[key]
                length = np.linalg.norm(a[e[:, 0]] - a[e[:, 1]], axis=1)
                ratio = length / active_lengths[key]
                p = {"p01": float(np.quantile(ratio, .01)), "p99": float(np.quantile(ratio, .99)),
                     "max": float(ratio.max()), "min": float(ratio.min()),
                     "maxEdgeLengthChange": float(np.max(np.abs(length - active_lengths[key])))}
                if active_cuff_edges[key].any():
                    p['cuffMaxEdgeLengthChange'] = float(np.max(np.abs(length-active_lengths[key])[active_cuff_edges[key]]))
                if "QA_ArmLift" in action.name or "QA_Forward" in action.name:
                    mask = active_core[key]
                    p["torsoPanelDrift"] = float(np.linalg.norm(a[mask] - active_rest[key][mask], axis=1).max()) if mask.any() else 0.0
                if active_sole[key].any():
                    p["soleMinZ"] = float(a[active_sole[key], 2].min())
                row["parts"][key] = p
            measurements.append(row)
    report["measurements"][version] = measurements
    summary = {}
    for clip in sorted({r["clip"] for r in measurements}):
        rows = [(r["frame"], key, p) for r in measurements if r["clip"] == clip for key, p in r["parts"].items()]
        summary[clip] = {"worstP99": sorted([{"part": key, "frame": f, **p} for f, key, p in rows], key=lambda p: p["p99"], reverse=True)[:5],
                         "maxPanelDrift": max(p.get("torsoPanelDrift", 0) for _, _, p in rows),
                         "maxEdgeLengthChange": max(p["maxEdgeLengthChange"] for _, _, p in rows)}
    report["summary"][version] = summary
    print("ASSESSED", FAMILY, version, len(measurements), "poses", flush=True)
    arm.data.pose_position = 'REST'
    bpy.context.view_layer.update()
report['loopMaxErrors']={}
arm.data.pose_position='POSE'
for clip in ('Idle','Walk'):
    action=next(a for a in bpy.data.actions if a.name==FAMILY.title()+'_'+clip)
    arm.animation_data.action=action
    bpy.context.scene.frame_set(int(action.frame_range[0]));bpy.context.view_layer.update()
    first={key:coords(obj) for key,obj in parts.items()}
    bpy.context.scene.frame_set(int(action.frame_range[1]));bpy.context.view_layer.update()
    error=max(float(np.linalg.norm(coords(obj)-first[key],axis=1).max())for key,obj in parts.items())
    assert error<1e-4,(FAMILY,clip,'loop seam',error)
    report['loopMaxErrors'][clip]=error
panel=max(p.get('torsoPanelDrift',0)for row in report['measurements']['after']for p in row['parts'].values())
assert panel<1e-4,(FAMILY,'arm pulls protected torso panel',panel)
report['protectedPanelMaxDrift']=panel
report['qualityGatePassed']=True
report["complete"] = True
(ROOT / "assessment.json").write_text(json.dumps(report, indent=2) + "\n")
for version, summary in report["summary"].items():
    for clip, result in summary.items():
        worst = result["worstP99"][0]
        print(version, clip, worst["part"], "p99", round(worst["p99"], 3), "panel", round(result["maxPanelDrift"], 5), flush=True)

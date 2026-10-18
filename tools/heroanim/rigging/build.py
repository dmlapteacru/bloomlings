"""Repair the four accepted rigs and author baseline actions on their existing bones.

Generated Blender files are disposable caches, not another maintained master. Originals,
identity, exterior UVs, fitted positions and scarf fit morphs are preserved.
Local arm/torso contact corrections are explicitly recorded in the build report.
"""
import bpy
import hashlib
import json
import math
import sys
from pathlib import Path

import numpy as np
from mathutils import Quaternion, Vector

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
from skinning import fields, owned_fields, canonical, world_vector, smooth
from contact_blender import repair as repair_contact
from legs import LegSurface

REPO = HERE.parents[2]
FAMILY = sys.argv[sys.argv.index("--") + 1]
OUT = REPO / "content/work/rigging" / FAMILY
TITLE = FAMILY.title()
bpy.ops.wm.open_mainfile(filepath=str(OUT / "source-cache.blend"))
arm = next(o for o in bpy.context.scene.objects if o.type == "ARMATURE")
parts = {o["part_key"]: o for o in bpy.context.scene.objects if o.type == "MESH"}
for obj in parts.values():
    if obj.data.shape_keys:
        for key in obj.data.shape_keys.key_blocks:
            key.value = 0.0
arm.data.pose_position = "REST"
bpy.context.view_layer.update()
report = {"version": 2, "family": FAMILY, "fps": 24, "parts": [], "clips": [], "rig": arm.name}
report['tools'] = {name:hashlib.sha256((HERE/name).read_bytes()).hexdigest()
                   for name in ('build.py','skinning.py','legs.py','contact.py','contact_blender.py',
                                'cuff_ownership.py','cuff_ownership.json','contact_lining.py')}
report['assemblySha256'] = hashlib.sha256((OUT/'assembly.json').read_bytes()).hexdigest()


def positions(obj):
    return np.array([obj.matrix_world @ v.co for v in obj.data.vertices])


def reset():
    for p in arm.pose.bones:
        p.rotation_mode = "XYZ" if "Blend." in p.name else "QUATERNION"
        p.location = (0, 0, 0)
        p.rotation_quaternion = (1, 0, 0, 0)
        p.scale = (1, 1, 1)


arm.animation_data_clear()
for a in list(bpy.data.actions):
    bpy.data.actions.remove(a)

# Extra deform bones approximate half-angle volume support in ordinary linear FBX
# skinning. Existing bone names, hierarchy and rest matrices remain unchanged.
bpy.ops.object.select_all(action="DESELECT")
arm.select_set(True)
bpy.context.view_layer.objects.active = arm
bpy.ops.object.mode_set(mode="EDIT")
helpers = {}
for side in ("L", "R"):
    if 'KneePole.'+side not in arm.data.edit_bones:
        shin = arm.data.edit_bones['Shin.'+side]
        pole = arm.data.edit_bones.new('KneePole.'+side)
        pole.head = shin.head + Vector(world_vector((0,.35,0),FAMILY))
        pole.tail = pole.head + Vector((0,0,.05))
        pole.parent = arm.data.edit_bones['Root']
        pole.use_deform = False
    for helper, target in [("ShoulderBlend.", "UpperArm."), ("ElbowBlend.", "Forearm."),
                           ("KneeBlend.", "Shin."), ("AnkleBlend.", "Foot.")]:
        source = arm.data.edit_bones[target + side]
        b = arm.data.edit_bones.new(helper + side)
        b.matrix = source.matrix.copy()
        b.length = source.length
        b.parent = source.parent
        b.use_deform = True
        helpers[b.name] = source.name
bpy.ops.object.mode_set(mode="OBJECT")
for name, target in helpers.items():
    p = arm.pose.bones[name]
    p.rotation_mode = "XYZ"
    for axis, transform in enumerate(("ROT_X", "ROT_Y", "ROT_Z")):
        driver = p.driver_add("rotation_euler", axis).driver
        v = driver.variables.new()
        v.name = "joint"
        v.type = "TRANSFORMS"
        v.targets[0].id = arm
        v.targets[0].bone_target = target
        v.targets[0].transform_space = "LOCAL_SPACE"
        v.targets[0].transform_type = transform
        v.targets[0].rotation_mode = "XYZ"
        driver.expression = "joint * 0.5"
report["volumeHelpers"] = helpers

# The owner skeletons already have IK / pole controls. FBX carries their baked result,
# not Blender constraints; rebuild the controls for the editable authoring handoff.
for side in ("L", "R"):
    for name in ("Shin." + side, "Foot." + side):
        for c in list(arm.pose.bones[name].constraints):
            arm.pose.bones[name].constraints.remove(c)
    shin = arm.pose.bones["Shin." + side]
    ik = shin.constraints.new("IK")
    ik.name = "Planted leg"
    ik.target = arm
    ik.subtarget = "FootIK." + side
    ik.chain_count = 2
    ik.use_stretch = False
    ik.pole_target = arm
    ik.pole_subtarget = 'KneePole.'+side
    keep = arm.pose.bones["Foot." + side].constraints.new("COPY_ROTATION")
    keep.name = "Level sole"
    keep.target = arm
    keep.subtarget = "FootIK." + side
    keep.owner_space = "POSE"
    keep.target_space = "POSE"

# Calibrate the imported bone roll against the actual rest knee. Explicit poles
# prevent the IK solver choosing a different knee plane on a loop or a new frame.
visibility = [(m,m.show_viewport) for obj in parts.values() for m in obj.modifiers if m.type=='ARMATURE']
for modifier,_ in visibility:
    modifier.show_viewport=False
arm.data.pose_position='POSE'
reset()
pole_angles={}
for side in ('L','R'):
    ik=next(c for c in arm.pose.bones['Shin.'+side].constraints if c.type=='IK')
    knee=arm.data.bones['Thigh.'+side].tail_local.copy()
    def error(angle):
        ik.pole_angle=float(angle)
        bpy.context.view_layer.update()
        return (arm.pose.bones['Thigh.'+side].tail-knee).length_squared
    coarse=np.linspace(-math.pi,math.pi,73)
    best=min(coarse,key=error)
    fine=np.linspace(best-math.pi/36,best+math.pi/36,81)
    best=float(min(fine,key=error))
    ik.pole_angle=best
    assert error(best)<1e-8,(FAMILY,side,'IK rest knee mismatch',error(best))
    pole_angles[side]=best
for modifier,visible in visibility:
    modifier.show_viewport=visible
arm.data.pose_position='REST'
report['poleAngles']=pole_angles

for b in arm.data.bones:
    if b.name == "Root" or b.name.startswith(("FootIK.", "KneePole.", "socket_")) or b.name == "Crown":
        b.use_deform = False
arm.show_in_front = True
arm.data.display_type = "STICK"
axis_vectors = [world_vector(v, FAMILY) for v in [(1, 0, 0), (0, 1, 0), (0, 0, 1)]]
rig_rotation = arm.matrix_world.to_quaternion()
axes = {p.name: [p.bone.matrix_local.to_quaternion().inverted() @ (rig_rotation.inverted() @ Vector(a))
                 for a in axis_vectors] for p in arm.pose.bones}


def rotate(name, axis, degrees):
    p = arm.pose.bones[name]
    p.rotation_quaternion = Quaternion(axes[name][axis], math.radians(float(degrees))) @ p.rotation_quaternion


def move(name, delta):
    v = rig_rotation.inverted() @ Vector(world_vector(delta, FAMILY))
    arm.pose.bones[name].location += arm.data.bones[name].matrix_local.to_quaternion().inverted() @ v


def envelope(t, seconds, fade=.55):
    return float(smooth(0, fade, t) * (1.0 - smooth(seconds - fade, seconds, t)))


def pose(clip, t, seconds):
    reset()
    u = t / seconds
    wave = math.sin(math.tau * u)
    personality = {"sprig": 1.08, "bloom": .94, "drop": 1.14, "twig": .85}[FAMILY]
    if clip.startswith("QA_"):
        amount = math.sin(math.pi * u) ** 2
        if clip == "QA_ArmLift":
            for side, sign in [("L", 1), ("R", -1)]:
                rotate("UpperArm." + side, 1, sign * 65 * amount)
                rotate("Forearm." + side, 1, sign * 40 * amount)
                rotate("Hand." + side, 0, 15 * amount)
        elif clip == "QA_Forward":
            for side in ("L", "R"):
                rotate("UpperArm." + side, 0, -40 * amount)
                rotate("Forearm." + side, 0, -45 * amount)
        elif clip == "QA_Twist":
            rotate("Chest", 2, 15 * wave)
            rotate("Head", 2, -15 * wave)
            rotate("Chest", 0, 8 * amount)
        return
    if clip == "Idle":
        rotate("Chest", 0, .55 * personality * wave)
        rotate("Chest", 1, .40 * math.sin(math.tau * u * 2))
        rotate("Head", 2, 1.1 * wave)
        if "Tail" in arm.pose.bones:
            rotate("Tail", 2, 2 * wave)
    elif clip == "Walk":
        phase = math.tau * u * 2
        move("Hips", (0, 0, -.008 + .002 * math.cos(2 * phase)))
        rotate("Hips", 1, 1.1 * math.sin(phase))
        rotate("Chest", 2, 1.8 * math.sin(phase))
        rotate("Head", 2, -.8 * math.sin(phase))
        for side, offset in [("L", 0), ("R", math.pi)]:
            p = phase + offset
            move("FootIK." + side, (0, .020 * personality * math.cos(p), .014 * max(0, math.sin(p)) ** 1.5))
            rotate("UpperArm." + side, 0, 12 * math.cos(p))
            rotate("Forearm." + side, 0, -3 + 3 * math.cos(p))
    elif clip == "Wave":
        e = envelope(t, seconds, .65)
        rotate("UpperArm.L", 1, 53 * e)
        rotate("Forearm.L", 1, (37 + 10 * math.sin(math.tau * (t - .65) * 1.55 * personality)) * e)
        rotate("Hand.L", 0, 7 * math.sin(math.tau * (t - .65) * 1.55 * personality) * e)
        rotate("Head", 1, -3 * e)
        rotate("Head", 2, -3 * e)
        rotate("Chest", 1, -1.2 * e)
    elif clip == "Joy":
        e = envelope(t, seconds, .35)
        bounce = max(0, math.sin(math.tau * t * 1.35)) ** 2
        move("Root", (0, 0, .030 * personality * bounce * e))
        move("Hips", (0, 0, -.004 * (1 - bounce) * e))
        for side, sign in [("L", 1), ("R", -1)]:
            rotate("UpperArm." + side, 1, sign * (28 + 9 * bounce) * e)
            rotate("Forearm." + side, 1, sign * 16 * e)
        rotate("Head", 1, 3 * math.sin(math.tau * t * .8) * e)
    elif clip == "Sad":
        e = envelope(t, seconds, .85)
        rotate("Chest", 0, 3 * e)
        rotate("Head", 0, 6 * e)
        rotate("Head", 1, 2.5 * e)
        move("Hips", (0, 0, -.004 * e))
        for side, sign in [("L", 1), ("R", -1)]:
            rotate("UpperArm." + side, 1, -sign * 4 * e)
            rotate("Forearm." + side, 0, -3 * e)
    elif clip == "Curiosity":
        e = envelope(t, seconds, .6)
        look = math.sin(math.tau * u - .3) * e
        rotate("Head", 2, 9 * look)
        rotate("Head", 1, -4 * e)
        rotate("Chest", 2, 2.5 * look)
    else:
        raise ValueError(clip)


scene = bpy.context.scene
scene.render.fps = 24
arm.data.pose_position = "POSE"
arm.animation_data_create()
durations = {"Idle": 4.0, "Walk": 1.667, "Wave": 3.2, "Joy": 2.5, "Sad": 3.8, "Curiosity": 3.4,
             "QA_ArmLift": 2.0, "QA_Forward": 2.0, "QA_Twist": 2.0}
for clip, duration in durations.items():
    count = round(duration * 24)
    seconds = count / 24
    action = bpy.data.actions.new(TITLE + "_" + clip)
    action.use_fake_user = True
    arm.animation_data.action = action
    for frame in range(count + 1):
        scene.frame_set(frame + 1)
        pose(clip, frame / 24, seconds)
        for p in arm.pose.bones:
            p.keyframe_insert("location", frame=frame + 1, group=p.name)
            if p.name not in helpers:
                p.keyframe_insert("rotation_quaternion", frame=frame + 1, group=p.name)
    for fc in action.fcurves:
        for k in fc.keyframe_points:
            k.interpolation = "LINEAR"
    report["clips"].append({"name": action.name, "seconds": seconds, "frames": count + 1,
                            "loop": clip in ("Idle", "Walk"), "qaOnly": clip.startswith("QA_")})
arm.animation_data.action = None
reset()
scene.frame_set(1)
arm.data.pose_position = "REST"

# Review materials mirror the accepted game look; preserve all original UVs and albedo.
for mat in bpy.data.materials:
    if not mat.use_nodes:
        continue
    bsdf = next((n for n in mat.node_tree.nodes if n.type == "BSDF_PRINCIPLED"), None)
    if not bsdf:
        continue
    for name in ("Normal", "Metallic", "Roughness"):
        for link in list(bsdf.inputs[name].links):
            mat.node_tree.links.remove(link)
    bsdf.inputs["Metallic"].default_value = 0
    bsdf.inputs["Roughness"].default_value = .68
    keep = {l.from_node for l in bsdf.inputs["Base Color"].links}
    for node in list(mat.node_tree.nodes):
        if node.type == "TEX_IMAGE" and node not in keep:
            mat.node_tree.nodes.remove(node)


def export_review(name):
    arm.data.pose_position = "POSE"
    arm.animation_data.action = None
    reset()
    bpy.context.scene.frame_set(1)
    bpy.ops.export_scene.gltf(filepath=str(OUT / name), export_format="GLB", export_animations=True,
                             export_animation_mode="ACTIONS", export_force_sampling=True,
                             export_anim_single_armature=True, export_morph_animation=False)


export_review("before.glb")
arm.data.pose_position = "REST"
leg_surface = LegSurface(parts)
for key, obj in parts.items():
    before = positions(obj)
    original = {g.name: np.zeros(len(before)) for g in obj.vertex_groups}
    group_names = [g.name for g in obj.vertex_groups]
    for vertex in obj.data.vertices:
        for g in vertex.groups:
            original[group_names[g.group]][vertex.index] = g.weight
    if key == 'Overalls':
        original, report['shortsLegVerticesTransferred'] = leg_surface.transfer(obj, original)
    original, ownership, contact = repair_contact(obj, FAMILY, key, original)
    repaired = positions(obj)
    names, weights = owned_fields(repaired, FAMILY, key, original, ownership) if ownership is not None else fields(repaired, FAMILY, key, original=original, helpers=True)
    assert set(names) <= set(b.name for b in arm.data.bones)
    for g in list(obj.vertex_groups):
        obj.vertex_groups.remove(g)
    for column, name in enumerate(names):
        group = obj.vertex_groups.new(name=name)
        for i in np.flatnonzero(weights[:, column] > 0.0):
            group.add([int(i)], float(weights[i, column]), "REPLACE")
    for m in obj.modifiers:
        if m.type == "ARMATURE":
            m.use_deform_preserve_volume = False
    if not contact or not contact["geometryChanged"]:
        assert np.max(np.abs(positions(obj) - before)) < 1e-8
    report["parts"].append({"key": key, "vertices": len(repaired), "sourceVertices": len(before), "contactRepair": contact, "groups": names,
                            "maxInfluences": int((weights > 0).sum(axis=1).max()),
                            "maxNormalizationError": float(np.abs(weights.sum(axis=1) - 1).max())})
export_review("after.glb")
arm.data.pose_position = "REST"
reset()
scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT / "rig-cache.blend"))
(OUT / "rig-report.json").write_text(json.dumps(report, indent=2) + "\n")
print("REPAIRED", FAMILY, len(parts), "parts, six baseline actions and three stress clips", flush=True)

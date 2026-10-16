"""Author four original Sprig motions on the existing owner's rig, without changing its mesh or weights.

blender --background --factory-startup --python tools/heroanim/author-lab.py
Output: models/sprig-lab.glb, editable skeletal animation source for the isolated playtest lab.
"""
import bpy
import math
import json
from pathlib import Path
from mathutils import Quaternion, Vector

HERE = Path(__file__).resolve().parent
FPS = 20
SECONDS = 3

def smooth(t):
    t = max(0, min(1, t))
    return t*t*(3-2*t)

def envelope(t, start=.1, end=.9):
    return smooth((t-start)/.18) * (1-smooth((t-end+.18)/.18))

def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(HERE/'models/heroes.glb'))
    arm = next(o for o in bpy.data.objects if o.type=='ARMATURE' and 'Sprig' in o.name)
    mesh = next(o for o in bpy.data.objects if o.type=='MESH' and any(m.type=='ARMATURE' and m.object==arm for m in o.modifiers))
    idle = next(a for a in bpy.data.actions if 'Sprig_Breathing' in a.name)
    arm.animation_data.action = idle
    bpy.context.scene.frame_set(1)
    rest = {b.name: (b.location.copy(), b.rotation_quaternion.copy().normalized(), b.scale.copy()) for b in arm.pose.bones}
    # Retain only this hero and its armature. The owner's source is never overwritten.
    for o in list(bpy.data.objects):
        if o not in (arm,mesh):
            bpy.data.objects.remove(o, do_unlink=True)
    arm.animation_data_clear()
    for action in list(bpy.data.actions):
        bpy.data.actions.remove(action)
    arm.animation_data_create()
    scene = bpy.context.scene
    scene.render.fps = FPS
    scene.frame_start, scene.frame_end = 1, FPS*SECONDS+1
    local_axes = {b.name: [b.bone.matrix_local.to_quaternion().inverted() @ Vector(axis)
                         for axis in ((1,0,0),(0,1,0),(0,0,1))] for b in arm.pose.bones}

    def reset():
        for b in arm.pose.bones:
            b.rotation_mode = 'QUATERNION'
            b.location, b.rotation_quaternion, b.scale = rest[b.name]

    def turn(name, axis, degrees):
        b = arm.pose.bones[name]
        b.rotation_quaternion = b.rotation_quaternion @ Quaternion(local_axes[name][axis], math.radians(degrees))

    actions = []
    for name in ('Idle','Look','Hello','Bow','Bounce'):
        action = bpy.data.actions.new('Lab_Sprig_'+name)
        arm.animation_data.action = action
        for frame in range(1, FPS*SECONDS+2):
            scene.frame_set(frame)
            reset()
            t = (frame-1)/(FPS*SECONDS)
            e = envelope(t)
            if name == 'Look':
                look = 18*math.sin(2*math.pi*t)*e
                turn('Head',2,look)
                turn('Head',1,4*math.sin(4*math.pi*t)*e)
                turn('Chest',2,look*.18)
            elif name == 'Hello':
                turn('UpperArm.R',1,65*e)
                turn('Forearm.R',1,25*e)
                turn('Hand.R',0,18*math.sin(6*math.pi*t)*e)
                turn('Head',1,-4*e)
                turn('Head',2,-5*e)
            elif name == 'Bow':
                bow = math.sin(math.pi*t)**2 * e
                turn('Chest',0,12*bow)
                turn('Spine',0,5*bow)
                turn('Head',0,8*bow)
                turn('UpperArm.L',1,-8*bow)
                turn('UpperArm.R',1,8*bow)
            elif name == 'Bounce':
                hop = max(0, math.sin(4*math.pi*t))*e
                arm.pose.bones['Root'].location.z += .032*hop
                sway = 4*math.sin(4*math.pi*t)*e
                turn('Chest',1,sway)
                turn('Head',1,-sway*.55)
                turn('UpperArm.L',1,-18*hop)
                turn('UpperArm.R',1,18*hop)
            for b in arm.pose.bones:
                b.keyframe_insert('location',frame=frame,group=b.name)
                b.keyframe_insert('rotation_quaternion',frame=frame,group=b.name)
                b.keyframe_insert('scale',frame=frame,group=b.name)
        for curve in action.fcurves:
            for point in curve.keyframe_points:
                point.interpolation = 'LINEAR'
        actions.append(action)
    arm.animation_data.action = None
    for action in actions:
        track = arm.animation_data.nla_tracks.new()
        track.name = action.name
        track.strips.new(action.name,1,action)
        track.mute = True
    reset()
    for o in bpy.context.selected_objects:
        o.select_set(False)
    arm.select_set(True); mesh.select_set(True)
    bpy.context.view_layer.objects.active = arm
    # NLA tracks isolate every authored action; the loop seam is the unchanged source rest pose.
    for track in arm.animation_data.nla_tracks:
        track.mute = False
    bpy.ops.export_scene.gltf(filepath=str(HERE/'models/sprig-lab.glb'), export_format='GLB',
        use_selection=True, export_animations=True, export_animation_mode='NLA_TRACKS',
        export_force_sampling=True, export_frame_range=False, export_skins=True)
    (HERE/'lab-source.json').write_text(json.dumps(dict(hero='sprig',source='models/heroes.glb',
        model='models/sprig-lab.glb',fps=FPS,seconds=SECONDS,clips=[a.name for a in actions],
        meshVertices=len(mesh.data.vertices),bones=len(arm.data.bones),
        meshAndWeightsChanged=False),indent=2)+'\n')
    print('HERO LAB: authored', [a.name for a in actions])

if __name__ == '__main__':
    main()

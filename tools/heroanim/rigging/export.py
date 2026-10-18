"""Export corrected, portable FBX parts and six baked actions; no runtime asset changes."""
import bpy
import hashlib
import json
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
FAMILY = sys.argv[sys.argv.index('--') + 1]
ROOT = HERE.parents[2] / 'content/work/rigging' / FAMILY
OUT = ROOT / 'handoff'
FBX = OUT / 'FBX'
FBX.mkdir(parents=True, exist_ok=True)
TITLE = FAMILY.title() + 'HeroV2'
bpy.ops.wm.open_mainfile(filepath=str(ROOT / 'rig-cache.blend'))
arm = next(o for o in bpy.context.scene.objects if o.type == 'ARMATURE')
parts = {o['part_key']: o for o in bpy.context.scene.objects if o.type == 'MESH'}
for action in list(bpy.data.actions):
    if '_QA_' in action.name:
        bpy.data.actions.remove(action)
assert len(bpy.data.actions) == 6
arm.animation_data.action = None
arm.data.pose_position = 'REST'
for p in arm.pose.bones:
    p.location = (0, 0, 0)
    p.rotation_quaternion = (1, 0, 0, 0)
    p.scale = (1, 1, 1)
bpy.context.scene.frame_set(1)
report = {'family': FAMILY, 'rig': arm.name, 'fps': 24, 'files': [],
          'clips': [c for c in json.loads((ROOT / 'rig-report.json').read_text())['clips'] if not c['qaOnly']],
          'helpers': json.loads((ROOT / 'rig-report.json').read_text())['volumeHelpers'],
          'poleAngles': json.loads((ROOT / 'rig-report.json').read_text())['poleAngles']}


def export(name, objects, animated=False):
    bpy.ops.object.select_all(action='DESELECT')
    arm.select_set(True)
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = arm
    arm.data.pose_position = 'POSE' if animated else 'REST'
    file = FBX / (name + '.fbx')
    bpy.ops.export_scene.fbx(filepath=str(file), use_selection=True, object_types={'ARMATURE', 'MESH'},
                             axis_forward='-Z', axis_up='Y', add_leaf_bones=False,
                             use_custom_props=True,
                             use_armature_deform_only=False, use_mesh_modifiers=False,
                             bake_anim=animated, bake_anim_use_all_bones=True,
                             bake_anim_use_nla_strips=False, bake_anim_use_all_actions=True,
                             bake_anim_force_startend_keying=True, bake_anim_step=1,
                             bake_anim_simplify_factor=0, path_mode='COPY', embed_textures=True)
    report['files'].append({'path': 'FBX/' + file.name, 'sha256': hashlib.sha256(file.read_bytes()).hexdigest(),
                            'bytes': file.stat().st_size, 'parts': [o['part_key'] for o in objects]})


base = [o for key, o in parts.items() if key == 'base' or key.startswith('Base_')]
export(TITLE, base)
for key, obj in sorted(parts.items()):
    if obj not in base:
        export(TITLE + '_' + key, [obj])
export(TITLE + '@Anims', [], True)
(OUT / 'inventory.json').write_text(json.dumps(report, indent=2) + '\n')
print('EXPORTED', FAMILY, len(report['files']), 'FBXs, 6 baseline actions', flush=True)

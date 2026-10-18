"""Export corrected, portable FBX parts and six baked actions; no runtime asset changes."""
import bpy
from io_scene_fbx import export_fbx_bin
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
# Generated contact atlases are packed. Give them distinct portable filenames:
# the native exporter deduplicates embedded media by the resolved filepath.
for image in bpy.data.images:
    if image.packed_file and not image.filepath:
        image.filepath_raw = '//' + image.name

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
          'animationBindPose': True,
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
    # Blender emits BindPose only for skinned meshes. An animation-only FBX then
    # imports the evaluated IK pose as a different rest skeleton. Emit the same
    # native BindPose block for this armature, including exact rest matrices for
    # all bones. Animation sampling stays entirely unchanged; no dummy mesh.
    native_writer=export_fbx_bin.fbx_data_object_elements
    native_scene=export_fbx_bin.fbx_data_from_scene
    def bind_scene(scene,depsgraph,settings):
        data=native_scene(scene,depsgraph,settings)
        data.templates[b'BindPose']=export_fbx_bin.fbx_template_def_pose(scene,settings,nbr_users=1)
        return data._replace(templates_users=sum(t.nbr_users for t in data.templates.values()))
    def rest_writer(root,obj,scene_data):
        native_writer(root,obj,scene_data)
        if not obj.is_bone and obj.type=='ARMATURE':
            bones=[b for b in scene_data.data_bones if b.armature==obj]
            assert len(bones)==len(arm.data.bones)
            export_fbx_bin.fbx_data_bindpose_element(root,obj,obj.bdata.data,scene_data,arm_obj=obj,bones=bones)
    if animated:
        export_fbx_bin.fbx_data_object_elements=rest_writer
        export_fbx_bin.fbx_data_from_scene=bind_scene
    try:
        bpy.ops.export_scene.fbx(filepath=str(file), use_selection=True, object_types={'ARMATURE', 'MESH'},
                             axis_forward='-Z', axis_up='Y', add_leaf_bones=False,
                             use_custom_props=True,
                             use_armature_deform_only=False, use_mesh_modifiers=False,
                             bake_anim=animated, bake_anim_use_all_bones=True,
                             bake_anim_use_nla_strips=False, bake_anim_use_all_actions=True,
                             bake_anim_force_startend_keying=True, bake_anim_step=1,
                                 bake_anim_simplify_factor=0, path_mode='COPY', embed_textures=True)
    finally:
        export_fbx_bin.fbx_data_object_elements=native_writer
        export_fbx_bin.fbx_data_from_scene=native_scene
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

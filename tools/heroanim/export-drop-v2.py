"""Export owner FBX parts with one identical skeleton and embedded albedo; runtime unchanged."""
import bpy,json,hashlib
from pathlib import Path
HERE=Path(__file__).resolve().parent;ROOT=HERE.parent.parent/'content/work/drop-v2';OUT=ROOT/'handoff';FBX=OUT/'FBX';FBX.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'rig-cache.blend'))
arm=bpy.data.objects['DropHeroV2Rig'];meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];actions=list(bpy.data.actions);assert len(actions)==5
arm.animation_data.action=None;arm.data.pose_position='REST';bpy.context.scene.frame_set(1)
report={'version':1,'files':[],'animationNames':[a.name for a in actions]}
def export(name,objects,animated=False):
 bpy.ops.object.select_all(action='DESELECT');arm.select_set(True)
 for o in objects:o.select_set(True)
 bpy.context.view_layer.objects.active=arm;arm.data.pose_position='POSE' if animated else 'REST'
 dest=FBX/(name+'.fbx')
 bpy.ops.export_scene.fbx(filepath=str(dest),use_selection=True,object_types={'ARMATURE','MESH'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,use_armature_deform_only=False,use_mesh_modifiers=False,bake_anim=animated,bake_anim_use_all_bones=True,bake_anim_use_nla_strips=False,bake_anim_use_all_actions=True,bake_anim_force_startend_keying=True,bake_anim_step=1,bake_anim_simplify_factor=0,path_mode='COPY',embed_textures=True)
 report['files'].append({'path':'FBX/'+dest.name,'sha256':hashlib.sha256(dest.read_bytes()).hexdigest(),'meshes':[o.name for o in objects]})
export('DropHeroV2',[o for o in meshes if o.name.startswith('DropHeroV2_Base_')])
for o in sorted(meshes,key=lambda o:o.name):
 if not o.name.startswith('DropHeroV2_Base_'):export(o.name,[o])
export('DropHeroV2@Anims',[],True)
# Inspection GLB uses the same evaluated skeleton and five actions. Never bundled into the game.
arm.data.pose_position='POSE';arm.animation_data.action=None
for p in arm.pose.bones:p.location=(0,0,0);p.rotation_quaternion=(1,0,0,0);p.scale=(1,1,1)
bpy.context.scene.frame_set(1);bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.gltf(filepath=str(ROOT/'DropHeroV2.glb'),export_format='GLB',export_animations=True,export_animation_mode='ACTIONS',export_force_sampling=True,export_anim_single_armature=True,export_morph_animation=False)
(OUT/'inventory.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print('EXPORTED',len(report['files']),'FBX files',flush=True)

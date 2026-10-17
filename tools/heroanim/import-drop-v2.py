"""Run from Blender's Text Editor after extracting the full handoff ZIP. Does not replace your .blend."""
import bpy,json
from pathlib import Path
PACKAGE=Path(__file__).resolve().parent
if not (PACKAGE/'inventory.json').exists():
 # Repository development copy: delivered copy lives alongside inventory.json.
 PACKAGE=Path(__file__).resolve().parents[2]/'content/work/drop-v2/handoff'
assert (PACKAGE/'inventory.json').exists(),'Extract the whole ZIP, then open its Import_Drop.py in Blender.'
assert not bpy.data.collections.get('DropV2_Assembly'),'DropV2_Assembly is already present. Remove it before importing again.'
assert not any(o.name.startswith('DropHeroV2_') or o.name=='DropHeroV2Rig' for o in bpy.data.objects),'DropHeroV2 names already exist. Rename / move the prior Drop copy before importing.'
collection=bpy.data.collections.new('DropV2_Assembly');bpy.context.scene.collection.children.link(collection)
shared=None;parts={};imported_actions=[];idle_action=None
for entry in json.loads((PACKAGE/'inventory.json').read_text())['files']:
 before=set(bpy.data.objects);old_actions=set(bpy.data.actions)
 bpy.ops.import_scene.fbx(filepath=str(PACKAGE/entry['path']),use_anim=True)
 added=set(bpy.data.objects)-before;rig=next(o for o in added if o.type=='ARMATURE')
 if shared is None:shared=rig;shared.name='DropHeroV2Rig';shared.show_in_front=True
 for o in added:
  if o.type=='ARMATURE' and o!=shared:continue
  for c in list(o.users_collection):c.objects.unlink(o)
  collection.objects.link(o)
  if o.type=='MESH':
   for mod in o.modifiers:
    if mod.type=='ARMATURE':mod.object=shared
   world=o.matrix_world.copy();o.parent=shared;o.matrix_world=world;parts[o.name.removeprefix('DropHeroV2_')]=o
 if rig!=shared:bpy.data.objects.remove(rig,do_unlink=True)
 for action in set(bpy.data.actions)-old_actions:
  action.use_fake_user=True
  for name in ['Idle','Walk','Wave','Joy','Sad']:
   if 'Drop_'+name in action.name:
    action.name='Drop_'+name;imported_actions.append(action)
    if name=='Idle':idle_action=action
heads=['NativeDrop','HairSideWave','HairTwinRipples','HairWhirlpool','BucketHat','Cap','Beret','EarmuffHair','XmasHat']
bodies=['none','Jacket','Coat','Overalls'];scarves=['none','ScarfKnit','ScarfStripe']
for name,default,maximum,description in [('head_style',0,8,'0 Капля; 1 Боковая волна; 2 Две волны; 3 Водоворот; 4 Панама; 5 Кепка; 6 Берет; 7 Ушки; 8 Новый год'),('body_style',0,3,'0 Без одежды; 1 Зимняя куртка; 2 Осеннее пальто; 3 Комбинезон'),('scarf_style',0,2,'0 Без шарфа; 1 Объёмный вязаный; 2 Полосатый'),('glasses',0,1,'0 Без очков; 1 Очки')]:
 shared[name]=default;shared.id_properties_ui(name).update(min=0,max=maximum,description=description)
def drive(owner,prop,expression,control):
 f=owner.driver_add(prop);f.driver.type='SCRIPTED';v=f.driver.variables.new();v.name='choice';v.type='SINGLE_PROP';v.targets[0].id=shared;v.targets[0].data_path='["'+control+'"]';f.driver.expression=expression
for i,name in enumerate(heads):
 for prop in ['hide_viewport','hide_render']:drive(parts[name],prop,'choice != '+str(i),'head_style')
for prop in ['hide_viewport','hide_render']:drive(parts['Base_Head'],prop,'choice == 0','head_style')
for category,names in [('body_style',bodies),('scarf_style',scarves)]:
 for i,name in enumerate(names[1:],1):
  for prop in ['hide_viewport','hide_render']:drive(parts[name],prop,'choice != '+str(i),category)
for prop in ['hide_viewport','hide_render']:drive(parts['Glasses'],prop,'choice == 0','glasses')
for name in scarves[1:]:
 for i,body in enumerate(bodies[1:],1):drive(parts[name].data.shape_keys.key_blocks['Over_'+body],'value','1.0 if choice == '+str(i)+' else 0.0','body_style')
shared.animation_data_create();shared.animation_data.action=idle_action;shared.data.pose_position='POSE'
bpy.context.scene.render.fps=24;bpy.context.scene.frame_start=1;bpy.context.scene.frame_end=73;bpy.context.scene.frame_set(1)
# Pack only newly imported materials' images into the owner's master file.
images={node.image for o in parts.values() for mat in o.data.materials if mat and mat.use_nodes for node in mat.node_tree.nodes if node.type=='TEX_IMAGE' and node.image}
for image in images:
 if not image.packed_file:image.pack()
bpy.ops.object.select_all(action='DESELECT');shared.select_set(True);bpy.context.view_layer.objects.active=shared;shared.update_tag();bpy.context.view_layer.update()
print('Imported Drop: 1 shared rig,',len(parts),'meshes,',len(imported_actions),'actions. Select rig > Object properties > Custom Properties.')

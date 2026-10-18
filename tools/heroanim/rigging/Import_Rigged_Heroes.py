"""Open this file in Blender's Text Editor and Run Script after extracting the whole ZIP.
Imports only into a new collection; existing owner objects, images and scene timing are kept.
Running it again reuses these rigs and reinstalls the optional review panel.
"""
import bpy
import hashlib
import json
import math
import re
from pathlib import Path

PACKAGE = 'Bloomlings_RigFix_v1'
FAMILIES = ('twig','sprig','bloom','drop')
HEADS = {
    'twig':['PumpkinHat','Hair','HatHair','BucketHat','Beret','EarmuffHair','XmasHat'],
    'sprig':['HairLeafy','HairRosette','HairSwirl','BucketHat','Cap','Beret','EarmuffHair','XmasHat'],
    'bloom':['HairPetals','HairBob','HairPeach','HairRose','BucketHat','Cap','Beret','EarmuffHair','XmasHat'],
    'drop':['NativeDrop','HairSideWave','HairTwinRipples','HairWhirlpool','BucketHat','Cap','Beret','EarmuffHair','XmasHat']}
BODIES = ['none','Jacket','Coat','Overalls']
SCARVES = {'twig':['none','Scarf'],'sprig':['none','Scarf'],
           'bloom':['none','ScarfKnit','ScarfStripe'],'drop':['none','ScarfKnit','ScarfStripe']}
space=getattr(bpy.context,'space_data',None)
ROOT=Path(space.text.filepath).resolve().parent if space and space.type=='TEXT_EDITOR' and space.text.filepath else Path(__file__).resolve().parent
RIGS={o.get('rig_fix_family'):o for o in bpy.data.objects if o.type=='ARMATURE' and o.get('rig_fix_package')==PACKAGE}
COLLECTIONS={}
scene=bpy.context.scene
old_timing=(scene.render.fps,scene.render.fps_base,scene.frame_start,scene.frame_end,scene.frame_current)
frame_rate=scene.render.fps/scene.render.fps_base
parent=bpy.data.collections.get(PACKAGE)
if parent is None:
    parent=bpy.data.collections.new(PACKAGE)
    scene.collection.children.link(parent)


def driver(owner,path,index,rig,prop,expression):
    fc=owner.driver_add(path,index) if index is not None else owner.driver_add(path)
    d=fc.driver
    for v in list(d.variables):
        d.variables.remove(v)
    var=d.variables.new();var.name='style';var.type='SINGLE_PROP'
    var.targets[0].id=rig;var.targets[0].data_path='["'+prop+'"]'
    d.expression=expression


def property(rig,name,value,maximum,description):
    rig[name]=value
    rig.id_properties_ui(name).update(min=0,max=maximum,description=description)


def action_curves(action):
    # Owner exports identify Blender 5.2. Keep both legacy and slotted Action APIs.
    if hasattr(action,'fcurves') and not getattr(action,'is_action_layered',False):
        return list(action.fcurves)
    curves=[]
    for layer in action.layers:
        for strip in layer.strips:
            if not hasattr(strip,'channelbag'):
                continue
            for slot in action.slots:
                bag=strip.channelbag(slot)
                if bag:
                    curves.extend(bag.fcurves)
    return curves


for family in FAMILIES:
    if family in RIGS:
        COLLECTIONS[family]=next(c for c in RIGS[family].users_collection if c.name.startswith(PACKAGE))
        continue
    inventory=json.loads((ROOT/family/'inventory.json').read_text())
    collection=bpy.data.collections.new(PACKAGE+'_'+family)
    parent.children.link(collection);COLLECTIONS[family]=collection
    rig=None;parts={};new_images=set(bpy.data.images);new_actions=set(bpy.data.actions)
    for entry in inventory['files']:
        file=ROOT/family/entry['path']
        assert hashlib.sha256(file.read_bytes()).hexdigest()==entry['sha256'],str(file)+' changed'
        before=set(bpy.data.objects)
        bpy.ops.import_scene.fbx(filepath=str(file),use_anim=True,use_image_search=False)
        added=set(bpy.data.objects)-before
        current=next(o for o in added if o.type=='ARMATURE')
        if rig is None:
            rig=current
            rig.name=PACKAGE+'_'+family+'_Rig'
            for c in list(rig.users_collection):
                c.objects.unlink(rig)
            collection.objects.link(rig)
        else:
            assert set(b.name for b in current.data.bones)==set(b.name for b in rig.data.bones)
            assert max((a-b).length for bone in rig.data.bones for a,b in zip(bone.matrix_local,current.data.bones[bone.name].matrix_local))<1e-5
        for obj in added:
            if obj.type!='MESH':
                continue
            key=obj.get('part_key') or re.sub(r'\.\d{3}$','',obj.name).removeprefix(family.title()+'HeroV2_')
            world=obj.matrix_world.copy();obj.parent=rig;obj.matrix_world=world
            for mod in obj.modifiers:
                if mod.type=='ARMATURE':
                    mod.object=rig
            for c in list(obj.users_collection):
                c.objects.unlink(obj)
            collection.objects.link(obj)
            obj.name=PACKAGE+'_'+family+'_'+key;parts[key]=obj
        if current!=rig:
            bpy.data.objects.remove(current,do_unlink=True)
    actions={}
    for action in set(bpy.data.actions)-new_actions:
        clip=next((c for c in ('Idle','Walk','Wave','Joy','Sad','Curiosity') if action.name.endswith(family.title()+'_'+c)),None)
        if not clip:
            continue
        action.name=PACKAGE+'_'+family+'_'+clip;action.use_fake_user=True
        for fc in action_curves(action):
            for point in fc.keyframe_points:
                for coordinate in (point.co,point.handle_left,point.handle_right):
                    coordinate.x=1+(coordinate.x-1)*(frame_rate/24)
        action['source_fps']=24;actions[clip]=action.name
    assert len(actions)==6
    rig['rig_fix_package']=PACKAGE;rig['rig_fix_family']=family;rig['review_actions']=json.dumps(actions)
    property(rig,'head_style',0,len(HEADS[family])-1,'Headwear / hairstyle; stock is always present')
    property(rig,'body_style',0,3,'0 none, 1 Jacket, 2 Coat, 3 Overalls')
    property(rig,'scarf_style',0,len(SCARVES[family])-1,'Independent neck accessory')
    property(rig,'glasses',0,1,'Face accessory')
    property(rig,'live_ik',0,1,'Optional editable planted-foot IK; off plays the portable baked actions')
    for key,obj in parts.items():
        for mat in obj.data.materials:
            if mat and '_RigMask_' in mat.name:
                bs=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
                for link in list(bs.inputs['Alpha'].links):mat.node_tree.links.remove(link)
                if hasattr(mat,'surface_render_method'):mat.surface_render_method='DITHERED'
                elif hasattr(mat,'blend_method'):mat.blend_method='HASHED'
                expression='0 if style == 1 or style == 2 else 1' if '_RigMask_forearm_' in mat.name else '1 if style == 0 else 0'
                driver(bs.inputs['Alpha'],'default_value',None,rig,'body_style',expression)
        if key in HEADS[family]:
            expression='style != '+str(HEADS[family].index(key));prop='head_style'
        elif key in BODIES:
            expression='style != '+str(BODIES.index(key));prop='body_style'
        elif key in SCARVES[family]:
            expression='style != '+str(SCARVES[family].index(key));prop='scarf_style'
        elif key=='Glasses':
            expression='style == 0';prop='glasses'
        elif family=='drop' and key=='Base_Head':
            expression='style == 0';prop='head_style'
        else:
            continue
        for path in ('hide_render','hide_viewport'):
            driver(obj,path,None,rig,prop,expression)
        if key.startswith('Scarf') and obj.data.shape_keys:
            for body in BODIES[1:]:
                block=obj.data.shape_keys.key_blocks.get('Over_'+body)
                if block:
                    driver(block,'value',None,rig,'body_style','style == '+str(BODIES.index(body)))
    for name,target in inventory['helpers'].items():
        p=rig.pose.bones[name];p.rotation_mode='XYZ'
        for axis,transform in enumerate(('ROT_X','ROT_Y','ROT_Z')):
            d=p.driver_add('rotation_euler',axis).driver
            v=d.variables.new();v.name='joint';v.type='TRANSFORMS';v.targets[0].id=rig
            v.targets[0].bone_target=target;v.targets[0].transform_space='LOCAL_SPACE'
            v.targets[0].transform_type=transform;v.targets[0].rotation_mode='XYZ';d.expression='joint * .5'
    for side in ('L','R'):
        ik=rig.pose.bones['Shin.'+side].constraints.new('IK');ik.name='Optional planted IK'
        ik.target=rig;ik.subtarget='FootIK.'+side;ik.chain_count=2;ik.use_stretch=False
        ik.pole_target=rig;ik.pole_subtarget='KneePole.'+side;ik.pole_angle=inventory['poleAngles'][side]
        driver(ik,'influence',None,rig,'live_ik','style')
        sole=rig.pose.bones['Foot.'+side].constraints.new('COPY_ROTATION');sole.name='Optional level sole'
        sole.target=rig;sole.subtarget='FootIK.'+side;sole.owner_space='POSE';sole.target_space='POSE'
        driver(sole,'influence',None,rig,'live_ik','style')
    for b in rig.data.bones:
        if b.name=='Root' or b.name=='Crown' or b.name.startswith(('FootIK.','KneePole.','socket_')):
            b.use_deform=False
        if 'Blend.' in b.name or b.name=='Crown' or b.name.startswith('socket_'):
            b.hide=True
    rig.animation_data_create();rig.animation_data.action=None;rig.data.pose_position='POSE'
    rig.show_in_front=True;rig.data.display_type='STICK'
    for image in set(bpy.data.images)-new_images:
        if not image.packed_file and image.source=='FILE':
            image.pack()
    RIGS[family]=rig

scene.render.fps,scene.render.fps_base,scene.frame_start,scene.frame_end,frame=old_timing
scene.frame_set(frame)


def select_family(self,context):
    active=context.scene.bloomlings_rig_family
    for family,collection in COLLECTIONS.items():
        collection.hide_viewport=family!=active;collection.hide_render=family!=active


class BLOOMLINGS_OT_RigAction(bpy.types.Operator):
    bl_idname='bloomlings.rig_action';bl_label='Play baseline action'
    clip:bpy.props.StringProperty()
    def execute(self,context):
        rig=RIGS[context.scene.bloomlings_rig_family]
        action=bpy.data.actions[json.loads(rig['review_actions'])[self.clip]]
        rig.animation_data.action=action
        if hasattr(action,'slots') and len(action.slots):
            rig.animation_data.action_slot=action.slots[0]
        context.scene.frame_start=math.floor(action.frame_range[0]);context.scene.frame_end=math.ceil(action.frame_range[1])
        context.scene.frame_set(context.scene.frame_start)
        if context.screen and not bpy.app.background and not context.screen.is_animation_playing:
            bpy.ops.screen.animation_play()
        return {'FINISHED'}


class BLOOMLINGS_PT_RigReview(bpy.types.Panel):
    bl_label='Rig & animation review';bl_space_type='VIEW_3D';bl_region_type='UI';bl_category='Bloomlings'
    def draw(self,context):
        layout=self.layout;layout.prop(context.scene,'bloomlings_rig_family',text='Hero')
        family=context.scene.bloomlings_rig_family;rig=RIGS[family]
        for prop,label,choices in [('head_style','Head',HEADS[family]),('body_style','Body',BODIES),('scarf_style','Neck',SCARVES[family])]:
            item=choices[int(rig[prop])]
            layout.prop(rig,'["'+prop+'"]',text=label+' · '+('Acorn hat' if item=='PumpkinHat' else item))
        layout.prop(rig,'["glasses"]',text='Glasses')
        grid=layout.grid_flow(columns=2,align=True)
        for clip in ('Idle','Walk','Wave','Joy','Sad','Curiosity'):
            grid.operator('bloomlings.rig_action',text=clip).clip=clip
        layout.prop(rig,'["live_ik"]',text='Editable leg IK')
        layout.label(text='Material Preview shows outfit masks',icon='MATERIAL_DATA')


for cls in (BLOOMLINGS_OT_RigAction,BLOOMLINGS_PT_RigReview):
    previous=getattr(bpy.types,cls.__name__,None)
    if previous:
        bpy.utils.unregister_class(previous)
    bpy.utils.register_class(cls)
bpy.types.Scene.bloomlings_rig_family=bpy.props.EnumProperty(name='Hero',items=[(f,f.title(),'')for f in FAMILIES],update=select_family)
scene.bloomlings_rig_family='twig'
select_family(None,bpy.context)
print('IMPORTED',len(RIGS),'corrected rigs; N panel → Bloomlings. Existing scene objects retained.')

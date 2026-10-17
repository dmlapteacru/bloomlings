"""Shared portable Drop skeleton, fitted weights and five original actions. No runtime game changes."""
import bpy,json,math,sys,hashlib
import numpy as np
from pathlib import Path
from mathutils import Vector,Quaternion
HERE=Path(__file__).resolve().parent;ROOT=HERE.parent.parent/'content/work/drop-v2';ROOT.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'assembly-cache.blend'))
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];parts={o.name.removeprefix('DropHeroV2_'):o for o in meshes}
for a in list(bpy.data.actions):bpy.data.actions.remove(a)
data=bpy.data.armatures.new('DropHeroV2Skeleton');arm=bpy.data.objects.new('DropHeroV2Rig',data);bpy.context.collection.objects.link(arm)
bpy.context.view_layer.objects.active=arm;arm.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
def bone(n,h,t,parent=None,deform=True):
 b=data.edit_bones.new(n);b.head=h;b.tail=t;b.use_deform=deform
 if parent:b.parent=data.edit_bones[parent]
bone('Root',(.04,0,0),(.04,0,.05),deform=False)
bone('Hips',(.04,0,.14),(.04,0,.19),'Root');bone('Spine',(.04,0,.19),(.04,0,.26),'Hips');bone('Chest',(.04,0,.26),(.02,0,.35),'Spine')
bone('Neck',(.02,0,.35),(.015,0,.38),'Chest');bone('Head',(.015,0,.38),(0,0,.84),'Neck')
bone('Tail',(-.085,0,.17),(-.145,0,.17),'Hips')
for side,sgn in [('L',1),('R',-1)]:
 bone('UpperArm.'+side,(.035,sgn*.085,.32),(.035,sgn*.17,.235),'Chest')
 bone('Forearm.'+side,(.035,sgn*.17,.235),(.035,sgn*.224,.178),'UpperArm.'+side)
 bone('Hand.'+side,(.035,sgn*.224,.178),(.04,sgn*.263,.16),'Forearm.'+side)
 bone('Thigh.'+side,(.04,sgn*.082,.15),(.06,sgn*.082,.10),'Hips')
 bone('Shin.'+side,(.06,sgn*.082,.10),(.045,sgn*.082,.055),'Thigh.'+side)
 bone('Foot.'+side,(.045,sgn*.082,.055),(.12,sgn*.082,.04),'Shin.'+side)
 bone('FootIK.'+side,(.045,sgn*.082,.055),(.12,sgn*.082,.04),'Root',False)
bone('socket_head',(0,0,.95),(0,0,1.02),'Head',False);bone('socket_face',(.29,0,.65),(.30,0,.65),'Head',False)
bpy.ops.object.mode_set(mode='OBJECT');arm.show_in_front=True;data.display_type='STICK'
def smooth(a,b,x):
 v=max(0,min(1,(x-a)/(b-a)));return v*v*(3-2*v)
def torso(z):
 chest=smooth(.23,.31,z);spine=smooth(.15,.235,z)
 return {'Chest':chest,'Spine':(1-chest)*spine,'Hips':(1-chest)*(1-spine)}
weights={};rigid={'NativeDrop','HairSideWave','HairTwinRipples','HairWhirlpool','BucketHat','Cap','Beret','EarmuffHair','XmasHat','Glasses','Base_Head'}
def normalize(w):
 w=dict(sorted(((k,v) for k,v in w.items() if v>1e-7),key=lambda i:-i[1])[:4]);s=sum(w.values());assert s>0;return {k:v/s for k,v in w.items()}
for key,o in parts.items():
 if not key.startswith('Base_'):continue
 rows=[]
 for v in o.data.vertices:
  x,y,z=v.co;ay=abs(y);side='L' if y>=0 else 'R'
  if key=='Base_Head':w={'Head':1}
  elif key=='Base_Tail':w={'Tail':1}
  elif 'Arm_' in key:
   a=smooth(.075,.12,ay);elbow=smooth(.14,.205,ay);hand=max(smooth(.205,.24,ay),smooth(.18,.215,ay)*(1-smooth(.19,.245,z)))
   w={'Chest':1-a,'UpperArm.'+side:a*(1-elbow),'Forearm.'+side:a*elbow*(1-hand),'Hand.'+side:a*elbow*hand}
  elif 'Leg_' in key:
   hip=smooth(.105,.14,z)
   w={'Hips':hip,'Foot.'+side:1-hip}
  else:w=torso(z)
  rows.append(normalize(w))
 weights[key]=rows
# Continuous anatomical regions avoid nearest-surface jumps at tight armpits.
# Short toy limbs stay rigid below the shoulder / hidden hip cap; no finger articulation.
for key,o in parts.items():
 if key in weights:continue
 if key in rigid:weights[key]=[{'Head':1} for v in o.data.vertices];continue
 rows=[]
 for v in o.data.vertices:
  x,y,z=v.co;side='L' if y>=0 else 'R';ay=abs(y)
  if key.startswith('Scarf'):w={'Chest':1}
  elif key=='Overalls':
   leg=1-smooth(.075,.145,z);left=smooth(-.035,.035,y)
   w={n:value*(1-leg) for n,value in torso(z).items()};w['Foot.L']=leg*left;w['Foot.R']=leg*(1-left)
  else:
   outer=smooth(.17,.205,ay);front=1-smooth(.08,.15,x);back=smooth(-.10,-.04,x)
   sleeve=smooth(.105,.17,ay)*smooth(.105,.17,z)*(outer+(1-outer)*front*back)
   w={n:value*(1-sleeve) for n,value in torso(z).items()};elbow=smooth(.145,.215,ay);w['UpperArm.'+side]=sleeve*(1-elbow);w['Forearm.'+side]=sleeve*elbow
  rows.append(normalize(w))
 weights[key]=rows
report=dict(version=1,bones=[b.name for b in data.bones],parts=[],clips=[])
for key,o in parts.items():
 for g in list(o.vertex_groups):o.vertex_groups.remove(g)
 groups={n:o.vertex_groups.new(name=n) for n in sorted({n for row in weights[key] for n in row})}
 for i,row in enumerate(weights[key]):
  for n,w in row.items():groups[n].add([i],w,'REPLACE')
 mod=o.modifiers.new('Drop shared skin','ARMATURE');mod.object=arm;mod.use_deform_preserve_volume=False;o.parent=arm
 assert all(abs(sum(w.values())-1)<1e-7 and len(w)<=4 for w in weights[key])
 report['parts'].append(dict(key=key,vertices=len(o.data.vertices),maxInfluences=max(map(len,weights[key])),rigidToHead=key in rigid,weightGroups=sorted(groups)))
for side in ['L','R']:
 ik=arm.pose.bones['Shin.'+side].constraints.new('IK');ik.name='Grounded Drop leg';ik.target=arm;ik.subtarget='FootIK.'+side;ik.chain_count=2;ik.use_stretch=False
 keep=arm.pose.bones['Foot.'+side].constraints.new('COPY_ROTATION');keep.target=arm;keep.subtarget='FootIK.'+side;keep.owner_space='POSE';keep.target_space='POSE'
scene=bpy.context.scene;scene.render.fps=24
axes={b.name:[b.bone.matrix_local.to_quaternion().inverted()@Vector(a) for a in [(1,0,0),(0,1,0),(0,0,1)]] for b in arm.pose.bones}
def reset():
 for p in arm.pose.bones:p.rotation_mode='QUATERNION';p.location=(0,0,0);p.rotation_quaternion=(1,0,0,0);p.scale=(1,1,1)
def rotate(name,axis,deg):
 p=arm.pose.bones[name];p.rotation_quaternion=Quaternion(axes[name][axis],math.radians(deg))@p.rotation_quaternion
def move(name,delta):arm.pose.bones[name].location+=data.bones[name].matrix_local.to_quaternion().inverted()@Vector(delta)
def envelope(t,start,end,fade=.25):return smooth(start,start+fade,t)*(1-smooth(end-fade,end,t))
CLIPS={'Idle':3,'Walk':2,'Wave':4,'Joy':3,'Sad':4}
arm.animation_data_create()
for clip,duration in CLIPS.items():
 action=bpy.data.actions.new('Drop_'+clip);action.use_fake_user=True;arm.animation_data.action=action;count=round(duration*24)
 for frame in range(count+1):
  scene.frame_set(frame+1);reset();t=frame/24;u=frame/count;s=math.sin(math.tau*u)
  rotate('Chest',1,.4*s);rotate('Head',0,.9*s);rotate('Tail',2,3*s)
  if clip=='Walk':
   phase=math.tau*u*2;move('Hips',(0,0,-.0015+.001*math.cos(2*phase)));rotate('Hips',2,1.3*math.sin(phase))
   for side,sgn in [('L',1),('R',-1)]:
    ph=phase+(0 if sgn==1 else math.pi);move('FootIK.'+side,(.017*math.cos(ph),0,.012*max(0,math.sin(ph))))
    rotate('UpperArm.'+side,1,6*math.cos(ph))
  elif clip=='Wave':
   e=envelope(t,.35,3.65,.65);rotate('UpperArm.L',0,(10+3*math.sin(t*math.tau*1.4))*e);rotate('Forearm.L',0,(12+3*math.sin(t*math.tau*1.4))*e);rotate('Head',0,-2*e)
  elif clip=='Joy':
   e=envelope(t,.2,2.8,.3);bounce=max(0,math.sin(t*math.tau*1.6))**2;move('Root',(0,0,.012*bounce*e))
   for side,sgn in [('L',1),('R',-1)]:rotate('UpperArm.'+side,0,sgn*(12+3*bounce)*e)
   rotate('Head',0,2.5*math.sin(t*math.tau*.8)*e)
  elif clip=='Sad':
   e=envelope(t,.25,3.75,.75);rotate('Chest',1,2.0*e);rotate('Head',1,5*e);rotate('Head',0,2.5*e);move('Hips',(0,0,-.002*e))
   for side,sgn in [('L',1),('R',-1)]:rotate('UpperArm.'+side,0,-sgn*3*e)
  for p in arm.pose.bones:
   p.keyframe_insert('location',frame=frame+1,group=p.name);p.keyframe_insert('rotation_quaternion',frame=frame+1,group=p.name)
 for f in action.fcurves:
  for k in f.keyframe_points:k.interpolation='LINEAR'
 report['clips'].append(dict(name=action.name,seconds=duration,frames=count+1))
arm.animation_data.action=None;reset();scene.frame_set(1);data.pose_position='REST'
(ROOT/'rig-report.json').write_text(json.dumps(report,indent=2)+'\n')
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'rig-cache.blend'))
# The cache is generated from originals, not an independently maintained Blender master.
print('RIGGED',len(parts),'meshes,',len(data.bones),'bones,',len(CLIPS),'clips',flush=True)

"""Owner Drop assets: fit to the unchanged common base before rigging. Build cache is disposable."""
import bpy,bmesh,json,zipfile,hashlib,sys
import numpy as np
from pathlib import Path
from mathutils import Vector,Matrix
from mathutils.bvhtree import BVHTree
HERE=Path(__file__).resolve().parent;sys.path.insert(0,str(HERE))
from drop_v2_features import features
REPO=HERE.parent.parent;SOURCE=HERE/'models/drop-v2';OUT=REPO/'content/work/drop-v2';OUT.mkdir(parents=True,exist_ok=True)
record=json.loads((SOURCE/'source-manifest.json').read_text());sources={}
for e in record['files']:
 p=(SOURCE/e['path']).resolve();assert hashlib.sha256(p.read_bytes()).hexdigest()==e['sha256']
 if p.suffix=='.zip':
  dest=OUT/'source'/e['key'];dest.mkdir(parents=True,exist_ok=True)
  with zipfile.ZipFile(p) as z:
   for f in z.infolist():assert (dest/f.filename).resolve().is_relative_to(dest.resolve())
   z.extractall(dest)
  sources[e['key']]=next(dest.glob('*.fbx'))
 else:sources[e['key']]=p
bpy.ops.wm.read_factory_settings(use_empty=True)
report=dict(version=1,sourceFiles=record['files'],parts=[],fits=[],landmarks={})
def load(key):
 before=set(bpy.data.objects);bpy.ops.import_scene.fbx(filepath=str(sources[key]))
 added=set(bpy.data.objects)-before;meshes=[o for o in added if o.type=='MESH']
 for o in meshes:
  w=o.matrix_world.copy();o.parent=None;o.matrix_world=w
  for m in list(o.modifiers):o.modifiers.remove(m)
  o.data.transform(w,shape_keys=True);o.matrix_world=Matrix.Identity(4);o.data.update()
 for o in added:
  if o.type!='MESH':bpy.data.objects.remove(o,do_unlink=True)
 return meshes
def discard(objects):
 for o in objects:bpy.data.objects.remove(o,do_unlink=True)
def join(objects,name):
 bpy.ops.object.select_all(action='DESELECT')
 for o in objects:o.select_set(True)
 bpy.context.view_layer.objects.active=objects[0]
 if len(objects)>1:bpy.ops.object.join()
 o=bpy.context.view_layer.objects.active;o.name='DropHeroV2_'+name;return o
def clean():
 for m in list(bpy.data.materials):
  if not m.users:bpy.data.materials.remove(m);continue
  if not m.use_nodes:continue
  bs=next((n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
  if not bs:continue
  keep={l.from_node for l in bs.inputs['Base Color'].links}
  for name in ['Normal','Metallic','Roughness']:
   for link in list(bs.inputs[name].links):m.node_tree.links.remove(link)
  for n in list(m.node_tree.nodes):
   if n.type=='TEX_IMAGE' and n not in keep:m.node_tree.nodes.remove(n)
  bs.inputs['Roughness'].default_value=.68;bs.inputs['Metallic'].default_value=0
 for im in list(bpy.data.images):
  if not im.users:bpy.data.images.remove(im)
def proxy(objects):
 vertices=[];polys=[]
 for o in objects:
  off=len(vertices);vertices.extend(v.co.copy() for v in o.data.vertices);polys.extend(tuple(i+off for i in p.vertices) for p in o.data.polygons)
 return BVHTree.FromPolygons(vertices,polys,all_triangles=False)
def fit_surface(o,targets,clearance=.003,max_distance=.06,smooth=12):
 tree=proxy(targets);before=[v.co.copy() for v in o.data.vertices]
 def project():
  for v in o.data.vertices:
   q,n,_,d=tree.find_nearest(v.co)
   if q is not None and d<max_distance and (v.co-q).dot(n)<clearance:v.co=q+n*clearance
 for _ in range(6):project()
 delta=[v.co-before[v.index] for v in o.data.vertices];adj=[set() for v in o.data.vertices]
 for e in o.data.edges:
  a,b=e.vertices;adj[a].add(b);adj[b].add(a)
 for _ in range(smooth):delta=[d.lerp(sum((delta[j] for j in adj[i]),Vector())/len(adj[i]),.6) if adj[i] else d for i,d in enumerate(delta)]
 for v in o.data.vertices:v.co=before[v.index]+delta[v.index]
 for _ in range(2):project()
 o.data.update();return max((v.co-before[v.index]).length for v in o.data.vertices)
base=load('base');base_names={'tripo_part_1':'Body','tripo_part_2':'Leg_R','tripo_part_3':'Arm_R','tripo_part_5':'Tail','tripo_part_6':'Leg_L','tripo_part_7':'Arm_L','tripo_part_9':'Head'}
for o in base:o.name='DropHeroV2_Base_'+base_names[o.name]
head=next(o for o in base if o.name.endswith('_Head'));body=[o for o in base if not o.name.endswith(('_Head','_Tail'))]
original={o.name:[tuple(v.co) for v in o.data.vertices] for o in base};uvs={o.name:[tuple(l.uv) for l in o.data.uv_layers.active.data] for o in base}
target=features([head]);report['landmarks']['base']=target
specs=[('HairSideWave','head',['tripo_part_2']),('HairTwinRipples','head_2',['tripo_part_1','tripo_part_3','tripo_part_4','tripo_part_6']),('HairWhirlpool','head_1',['tripo_part_0','tripo_part_2','tripo_part_4']),('BucketHat','cute_blue_1',['tripo_part_5']),('Cap','character',['tripo_part_8']),('Beret','beret',['tripo_part_10']),('EarmuffHair','character_2',['tripo_part_3']),('XmasHat','emoji',['tripo_part_5']),('NativeDrop','droplet',None)]
parts={}
for name,key,selection in specs:
 imported=load(key);chosen=imported if selection is None else [o for o in imported if o.name in selection]
 assert chosen and (selection is None or len(chosen)==len(selection)),(name,[o.name for o in imported])
 face=imported if selection is None else [o for o in imported if o not in chosen]
 f=features(face);report['landmarks'][name]=f
 sy=(target['left'][1]-target['right'][1])/(f['left'][1]-f['right'][1]);te=(np.array(target['left'])+target['right'])/2;se=(np.array(f['left'])+f['right'])/2
 sz=(te[2]-target['mouth'][2])/(se[2]-f['mouth'][2]);sz=float(np.clip(sz,sy*.8,sy*1.25))
 scale=np.array([sy,sy,sz]);offset=te-se*scale
 for o in chosen:
  for v in o.data.vertices:v.co=Vector(np.array(v.co)*scale+offset)
 discard([o for o in imported if o not in chosen]);obj=join(chosen,name)
 correction=0 if name=='NativeDrop' else fit_surface(obj,[head],.002,.055,8)
 parts[name]=obj;report['fits'].append(dict(key=name,source=key,scale=scale.tolist(),offset=offset.tolist(),surfaceCorrection=correction))
 clean();print('FIT',name,scale.tolist(),flush=True)
for name,key,selection,sc,off in [('Coat','character_4','tripo_part_19',(1,.95,.90),(.005,0,0)),('Jacket','cute_cartoon_3','tripo_part_24',(1,.91,.85),(.005,0,0)),('ScarfStripe','character_1','tripo_part_1',(1,.98,.96),(.005,0,-.005)),('ScarfKnit','cute_blue','tripo_part_0',(1,.90,.74),(.005,0,0))]:
 imported=load(key);chosen=next(o for o in imported if o.name==selection);discard([o for o in imported if o!=chosen]);chosen.name='DropHeroV2_'+name
 for v in chosen.data.vertices:v.co=Vector((v.co.x*sc[0]+off[0],v.co.y*sc[1]+off[1],v.co.z*sc[2]+off[2]))
 corr=fit_surface(chosen,body,.004,.055,16);parts[name]=chosen;report['fits'].append(dict(key=name,source=key,scale=sc,offset=off,surfaceCorrection=corr));clean();print('FIT',name,flush=True)
imported=load('overalls');assert len(imported)==1;imported[0].name='DropHeroV2_Overalls';parts['Overalls']=imported[0]
for name in ['ScarfKnit','ScarfStripe']:
 obj=parts[name];rest=[v.co.copy() for v in obj.data.vertices];obj.shape_key_add(name='Basis')
 for garment in ['Jacket','Coat','Overalls']:
  for v in obj.data.vertices:v.co=rest[v.index]
  fit_surface(obj,[*body,parts[garment]],.005,.065,10)
  key=obj.shape_key_add(name='Over_'+garment)
  for v in obj.data.vertices:key.data[v.index].co=v.co
 for v in obj.data.vertices:v.co=rest[v.index]
 obj.data.update()
imported=load('glasses');assert len(imported)==1;glasses=imported[0]
rot=Matrix.Rotation(np.pi/2,4,'Z');glasses.data.transform(rot)
pts=np.array([tuple(v.co) for v in glasses.data.vertices]);sy=.68/(pts[:,1].max()-pts[:,1].min());pupil=(np.array(target['left'])+target['right'])/2
center=np.array([np.quantile(pts[:,0],.92),(pts[:,1].max()+pts[:,1].min())/2,(pts[:,2].max()+pts[:,2].min())/2]);offset=pupil-center*sy;offset[0]+=.025
for v in glasses.data.vertices:v.co=Vector(np.array(v.co)*sy+offset)
glasses.name='DropHeroV2_Glasses'
# Preserve the round frame cross-section and move its front clear of both cheeks.
# Nearest-surface projection alone leaves the back of a rim buried in the face.
for v in glasses.data.vertices:
 t=max(0,min(1,(v.co.x-.10)/.10));v.co.x+=.060*t*t*(3-2*t)
fit_surface(glasses,[head,parts['NativeDrop']],.003,.025,4);parts['Glasses']=glasses
for m in glasses.data.materials:
 bs=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
 for link in list(bs.inputs['Base Color'].links):m.node_tree.links.remove(link)
 bs.inputs['Base Color'].default_value=(.65,.56,.36,1)
clean()
for o in base:
 assert original[o.name]==[tuple(v.co) for v in o.data.vertices]
 assert uvs[o.name]==[tuple(l.uv) for l in o.data.uv_layers.active.data]
report['baseGeometryUnchanged']=True;report['baseUvUnchanged']=True
# A hidden neck connector closes the donor head / short-body join without stretching either source.
bpy.ops.mesh.primitive_uv_sphere_add(segments=20,ring_count=12,location=(.02,0,.35))
neck=bpy.context.object;neck.name='DropHeroV2_Base_Neck';neck.scale=(.062,.065,.060)
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
for polygon in neck.data.polygons:polygon.use_smooth=True
mat=bpy.data.materials.new('Drop neck blue');mat.use_nodes=True;bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(.025,.37,.75,1);bs.inputs['Roughness'].default_value=.68;neck.data.materials.append(mat)
report['addedConnector']='Base_Neck';base.append(neck)
for k,o in parts.items():report['parts'].append(dict(key=k,name=o.name,vertices=len(o.data.vertices),faces=len(o.data.polygons),bounds=[[min(v.co[i] for v in o.data.vertices),max(v.co[i] for v in o.data.vertices)] for i in range(3)],shapeKeys=[k.name for k in o.data.shape_keys.key_blocks] if o.data.shape_keys else []))
(OUT/'assembly-report.json').write_text(json.dumps(report,indent=2)+'\n')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'assembly-cache.blend'))
# Export one common-space inspection GLB; recipes toggle meshes without fitting each bounding box separately.
bpy.ops.object.select_all(action='SELECT');bpy.ops.export_scene.gltf(filepath=str(OUT/'assembly.glb'),export_format='GLB',export_animations=False)
print('Prepared',len(parts),'Drop items; common base unchanged',flush=True)

"""Import the owner's aligned V2 FBX parts without refitting or maintaining another Blender master."""
import bpy, json, hashlib, zipfile
from pathlib import Path
from mathutils import Vector, Matrix

HERE=Path(__file__).resolve().parent; ROOT=HERE.parent.parent
SOURCE=HERE/'models/sprig-v2'; OUT=ROOT/'content/work/sprig-v2/build'; OUT.mkdir(parents=True,exist_ok=True)
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
record=json.loads((SOURCE/'source-manifest.json').read_text())
unpacked=OUT/'source'; unpacked.mkdir(parents=True,exist_ok=True)
for archive_record in record['archives']:
 archive=SOURCE/archive_record['path']; assert sha(archive)==archive_record['sha256']
 with zipfile.ZipFile(archive) as z:
  for f in z.infolist(): assert (unpacked/f.filename).resolve().is_relative_to(unpacked.resolve())
  z.extractall(unpacked)
delivery=json.loads((unpacked/record['exportManifest']).read_text()); folder=(unpacked/record['exportManifest']).parent
bpy.ops.wm.read_factory_settings(use_empty=True)
report=dict(version=1,scriptSha256=sha(__file__),archives=record['archives'],parts=[],clips=delivery['clips'])
sources=[('base',folder/delivery['model'])]+[(i['id'],folder/i['file']) for i in delivery['items']]
for addition in record.get('additions',[]):
 path=SOURCE/addition['path']; assert sha(path)==addition['sha256']
 sources=[s for s in sources if s[0]!=addition['id']]+[(addition['id'],path)]
report['additions']=record.get('additions',[])
for key,path in sources:
 previous=set(bpy.data.objects); bpy.ops.import_scene.fbx(filepath=str(path))
 added=set(bpy.data.objects)-previous; meshes=[o for o in added if o.type=='MESH']; rigs=[o for o in added if o.type=='ARMATURE']
 assert len(meshes)==1 and len(rigs)==1,'Inspect changed export structure'
 obj=meshes[0]; rig=rigs[0]; rig.data.pose_position='REST'; bpy.context.view_layer.update()
 # The current game uses the owner's rest mesh; preserve world geometry / UV, remove only the bake copy's rig.
 world=obj.matrix_world.copy(); before=[tuple(world@v.co) for v in obj.data.vertices]
 uv=[tuple(l.uv) for l in obj.data.uv_layers.active.data] if obj.data.uv_layers.active else None
 obj.parent=None; obj.matrix_world=world
 for modifier in list(obj.modifiers): obj.modifiers.remove(modifier)
 obj.data.transform(obj.matrix_world, shape_keys=True); obj.matrix_world=Matrix.Identity(4); obj.name='SprigV2_'+key
 assert max(abs(v.co[i]-before[n][i]) for n,v in enumerate(obj.data.vertices) for i in range(3))<1e-7
 assert uv==([tuple(l.uv) for l in obj.data.uv_layers.active.data] if obj.data.uv_layers.active else None)
 materials=[]
 for mat in obj.data.materials:
  bsdf=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
  materials.append(dict(name=mat.name,color=list(bsdf.inputs['Base Color'].default_value),alpha=bsdf.inputs['Alpha'].default_value,transmission=bsdf.inputs['Transmission Weight'].default_value))
  for name in ['Normal','Metallic','Roughness']:
   for link in list(bsdf.inputs[name].links): mat.node_tree.links.remove(link)
  bsdf.inputs['Metallic'].default_value=0;bsdf.inputs['Roughness'].default_value=.68
 for extra in added:
  if extra!=obj and extra.name in bpy.data.objects: bpy.data.objects.remove(extra,do_unlink=True)
 bounds=[[min(v.co[i] for v in obj.data.vertices),max(v.co[i] for v in obj.data.vertices)] for i in range(3)]
 report['parts'].append(dict(key=key,file=path.name,sha256=sha(path),vertices=len(obj.data.vertices),faces=len(obj.data.polygons),worldGeometryUnchanged=True,uvUnchanged=True,bounds=bounds,materials=materials,shapeKeys=[k.name for k in obj.data.shape_keys.key_blocks] if obj.data.shape_keys else []))
# Front orthographic layers can share masks when their vertical extents cannot intersect.
parts={p['key']:p for p in report['parts']}
heads=[i['id'] for i in delivery['items'] if i['slot']=='Head']
clothes=['Jacket','Coat','Overalls']
cloth_top=max(parts[k]['bounds'][2][1] for k in clothes)
head_bottom=min(parts[k]['bounds'][2][0] for k in heads)
report['layerSeparation']=dict(clothingTop=cloth_top,headwearBottom=head_bottom,
 scarfTop=parts['Scarf']['bounds'][2][1],glassesBottom=parts['Glasses']['bounds'][2][0],
 headwearClearsClothesAndScarf=head_bottom>max(cloth_top,parts['Scarf']['bounds'][2][1]),
 glassesClearClothes=parts['Glasses']['bounds'][2][0]>cloth_top)
# The baker checks separation before choosing shared masks.
anim=folder/delivery['anims']; before=set(bpy.data.objects); bpy.ops.import_scene.fbx(filepath=str(anim))
report['animationSource']=dict(file=anim.name,sha256=sha(anim),actions=[dict(name=a.name,range=list(a.frame_range)) for a in bpy.data.actions])
assert len(report['animationSource']['actions'])==len(delivery['clips'])
for o in set(bpy.data.objects)-before: bpy.data.objects.remove(o,do_unlink=True)
path=OUT/'SprigHeroV2.glb'; bpy.ops.export_scene.gltf(filepath=str(path),export_format='GLB',export_animations=False)
report['export']=dict(file=path.name,sha256=sha(path),bytes=path.stat().st_size)
(OUT/'build-report.json').write_text(json.dumps(report,indent=2)+'\n')
print('Prepared V2 shared base and',len(report['parts'])-1,'aligned parts')

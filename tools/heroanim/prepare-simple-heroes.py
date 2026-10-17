"""Import the owner's four simplified Tripo FBXs for offline 2D rendering; no editable master or runtime 3D."""
import hashlib,json,zipfile
from pathlib import Path
import bpy
from mathutils import Vector
HERE=Path(__file__).resolve().parent
ROOT=HERE.parent.parent
SOURCE=HERE/'models/simple-tripo'
OUT=ROOT/'content/work/simple-tripo/build'
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
OUT.mkdir(parents=True,exist_ok=True)
record=json.loads((SOURCE/'source-manifest.json').read_text())
report=dict(version=1,scriptSha256=sha(__file__),sources=[],models=[],originalSourcesChanged=False)
for entry in record['files']:
 bpy.ops.wm.read_factory_settings(use_empty=True)
 archive=SOURCE/'source'/entry['file'];assert sha(archive)==entry['sha256']
 extracted=OUT/'extracted'/entry['family'];extracted.mkdir(parents=True,exist_ok=True)
 with zipfile.ZipFile(archive) as z:
  for item in z.infolist():assert (extracted/item.filename).resolve().is_relative_to(extracted.resolve())
  z.extractall(extracted)
 fbx=next(extracted.glob('*.fbx'));bpy.ops.import_scene.fbx(filepath=str(fbx))
 meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];rigs=[o for o in bpy.context.scene.objects if o.type=='ARMATURE']
 assert meshes and not rigs,'Expected a static unrigged model'
 family=entry['family'];root=bpy.data.objects.new('SimpleHero_'+family,None);bpy.context.scene.collection.objects.link(root)
 points=[o.matrix_world@Vector(v) for o in meshes for v in o.bound_box]
 low=Vector(tuple(min(p[i] for p in points) for i in range(3)));high=Vector(tuple(max(p[i] for p in points) for i in range(3)))
 size=high-low;assert size.z>0
 for i,obj in enumerate(meshes):
  world=obj.matrix_world.copy();obj.parent=root;obj.matrix_world=world;obj.name='SimpleHero_'+family+'_part_'+str(i)
  assert obj.data.uv_layers.active is not None,'Missing source UV'
  for mat in obj.data.materials:
   bsdf=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
   assert bsdf.inputs['Base Color'].links,'Missing source albedo'
   for name in ['Normal','Metallic','Roughness']:
    for link in list(bsdf.inputs[name].links):mat.node_tree.links.remove(link)
   bsdf.inputs['Metallic'].default_value=0
   bsdf.inputs['Roughness'].default_value=.68
 # One UNIFORM normalization, preserving source proportions, front toward Blender -Y.
 scale=1/size.z;root.scale=(scale,scale,scale)
 root.location=(-(low.x+high.x)*.5*scale,-(low.y+high.y)*.5*scale,-low.z*scale)
 bpy.context.view_layer.update()
 glb=OUT/(family+'.glb')
 bpy.ops.object.select_all(action='DESELECT');root.select_set(True)
 for o in meshes:o.select_set(True)
 bpy.ops.export_scene.gltf(filepath=str(glb),export_format='GLB',use_selection=True,export_animations=False)
 item=dict(family=family,file=glb.name,sha256=sha(glb),bytes=glb.stat().st_size,meshParts=len(meshes),triangles=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in meshes),rigged=False,sourceBounds=dict(min=list(low),max=list(high)),uniformScale=scale)
 report['sources'].append(dict(**entry,fbxSha256=sha(fbx)))
 report['models'].append(item)
(OUT/'build-report.json').write_text(json.dumps(report,indent=2)+'\n')
print('Prepared four static textured hero models:',[(m['family'],m['triangles']) for m in report['models']])

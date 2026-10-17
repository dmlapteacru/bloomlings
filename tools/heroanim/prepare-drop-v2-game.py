"""Prepare the verified Drop FBX handoff for 2D game baking; owner's .blend remains authoritative."""
import bpy,json,zipfile,hashlib,math
from pathlib import Path
from mathutils import Matrix
HERE=Path(__file__).resolve().parent;REPO=HERE.parent.parent;SOURCE=HERE/'models/drop-v2';OUT=REPO/'content/work/drop-v2/game';OUT.mkdir(parents=True,exist_ok=True)
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
archive=SOURCE/'products/DropHeroV2_FBX.zip';pin=json.loads((SOURCE/'manifest.json').read_text());expected=next(e['sha256'] for e in pin['files'] if e['path']=='products/DropHeroV2_FBX.zip');assert sha(archive)==expected
folder=OUT/'source'
with zipfile.ZipFile(archive) as z:
 for f in z.infolist():assert (folder/f.filename).resolve().is_relative_to(folder.resolve())
 z.extractall(folder)
folder=folder/'DropHeroV2';inventory=json.loads((folder/'inventory.json').read_text());bpy.ops.wm.read_factory_settings(use_empty=True)
report={'version':1,'scriptSha256':sha(__file__),'archive':'tools/heroanim/models/drop-v2/products/DropHeroV2_FBX.zip','archiveSha256':expected,'parts':[],'frontRotationDegrees':-90,'animationClips':inventory['animationNames']}
rotation=Matrix.Rotation(-math.pi/2,4,'Z')
for entry in inventory['files']:
 if '@Anims' in entry['path']:continue
 path=folder/entry['path'];assert sha(path)==entry['sha256'];before=set(bpy.data.objects);bpy.ops.import_scene.fbx(filepath=str(path),use_anim=False);added=set(bpy.data.objects)-before
 for rig in [o for o in added if o.type=='ARMATURE']:rig.data.pose_position='REST'
 bpy.context.view_layer.update()
 for o in added:
  if o.type!='MESH':continue
  w=o.matrix_world.copy();original=[rotation@w@v.co for v in o.data.vertices];uv=[tuple(l.uv) for l in o.data.uv_layers.active.data]
  o.parent=None;o.matrix_world=w
  for m in list(o.modifiers):o.modifiers.remove(m)
  o.data.transform(rotation@w,shape_keys=True);o.matrix_world=Matrix.Identity(4)
  assert max((v.co-original[v.index]).length for v in o.data.vertices)<2e-7
  assert uv==[tuple(l.uv) for l in o.data.uv_layers.active.data]
  for mat in o.data.materials:
   bs=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
   for slot in ['Normal','Metallic','Roughness']:
    for link in list(bs.inputs[slot].links):mat.node_tree.links.remove(link)
   bs.inputs['Metallic'].default_value=0;bs.inputs['Roughness'].default_value=.68
  report['parts'].append({'key':o.name.removeprefix('DropHeroV2_'),'vertices':len(o.data.vertices),'sourceGeometryUnderGlobalRotation':True,'uvUnchanged':True,'shapeKeys':[k.name for k in o.data.shape_keys.key_blocks] if o.data.shape_keys else []})
 for o in added:
  if o.type!='MESH':bpy.data.objects.remove(o,do_unlink=True)
assert len(report['parts'])==23
# Only albedo images enter the derived GLB; embedded FBX source images are never changed.
for mat in bpy.data.materials:
 if not mat.users or not mat.use_nodes:continue
 bs=next((n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
 keep={l.from_node for l in bs.inputs['Base Color'].links} if bs else set()
 for n in list(mat.node_tree.nodes):
  if n.type=='TEX_IMAGE' and n not in keep:mat.node_tree.nodes.remove(n)
for im in list(bpy.data.images):
 if not im.users:bpy.data.images.remove(im)
file=OUT/'DropHeroV2.glb';bpy.ops.export_scene.gltf(filepath=str(file),export_format='GLB',export_animations=False)
report['export']={'file':file.name,'sha256':sha(file)};(OUT/'build-report.json').write_text(json.dumps(report,indent=2)+'\n');print('Prepared verified Drop game assembly, 23 meshes',flush=True)

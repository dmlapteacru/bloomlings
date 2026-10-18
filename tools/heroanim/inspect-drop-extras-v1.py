"""Disposable visual inventory of the nine owner ZIPs, with pinned sources."""
import bpy,json,zipfile,hashlib,shutil
from pathlib import Path
R=Path(__file__).resolve().parents[2];S=R/'tools/heroanim/models/drop-extras-v1';O=R/'content/work/drop-extras-v1/inspect'
O.mkdir(parents=True,exist_ok=True)
report=[]
sources=json.loads((S/'source-manifest.json').read_text())
for i,entry in enumerate(sources):
 p=S/entry['path'];dest=p
 assert hashlib.sha256(p.read_bytes()).hexdigest()==entry['sha256']
 bpy.ops.wm.read_factory_settings(use_empty=True)
 with zipfile.ZipFile(p) as z:
  assert all((O/f.filename).resolve().is_relative_to(O.resolve()) for f in z.infolist());z.extractall(O)
 fbx=next(O.glob('*.fbx'));bpy.ops.import_scene.fbx(filepath=str(fbx));parts=[]
 for obj in bpy.data.objects:
  if obj.type!='MESH':continue
  w=obj.matrix_world.copy();obj.parent=None;obj.data.transform(w);obj.matrix_world.identity()
  for mod in list(obj.modifiers):obj.modifiers.remove(mod)
  pts=[v.co for v in obj.data.vertices];parts.append(dict(name=obj.name,verts=len(pts),bounds=[[min(v[a] for v in pts),max(v[a] for v in pts)] for a in range(3)]))
 for mat in bpy.data.materials:
  if not mat.use_nodes:continue
  bs=next((n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
  if not bs:continue
  keep={l.from_node for l in bs.inputs['Base Color'].links}
  for n in list(mat.node_tree.nodes):
   if n.type=='TEX_IMAGE' and n not in keep:mat.node_tree.nodes.remove(n)
 for image in list(bpy.data.images):
  if not image.users:bpy.data.images.remove(image)
 bpy.ops.export_scene.gltf(filepath=str(O/('item-%02d.glb'%(i+1))),export_format='GLB',export_animations=False)
 report.append(dict(key='item-%02d'%(i+1),originalName=p.name,path=str(dest.relative_to(S)),sha256=hashlib.sha256(p.read_bytes()).hexdigest(),parts=parts))
 for child in O.iterdir():
  if child.suffix=='.fbx':child.unlink()
  elif child.is_dir():shutil.rmtree(child)
(O/'inventory.json').write_text(json.dumps(report,indent=2)+'\n')

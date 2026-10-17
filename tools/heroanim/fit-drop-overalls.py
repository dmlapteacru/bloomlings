"""Fit only the delivered garment to the unchanged owner base; disposable offline scene."""
import bpy,json,hashlib,math,bmesh,zipfile
from pathlib import Path
from mathutils import Vector,Matrix
from mathutils.bvhtree import BVHTree
HERE=Path(__file__).resolve().parent
REPO=HERE.parent.parent
SOURCE=HERE/'models/drop-clothing-study'
ROOT=REPO/'content/work/drop-clothing-fit';ROOT.mkdir(parents=True,exist_ok=True)
source_record=json.loads((SOURCE/'source-manifest.json').read_text())
for entry in source_record['files']:
 archive=SOURCE/entry['path'];assert hashlib.sha256(archive.read_bytes()).hexdigest()==entry['sha256']
 dest=ROOT/'source'/entry['key'];dest.mkdir(parents=True,exist_ok=True)
 with zipfile.ZipFile(archive) as z:
  assert z.testzip() is None
  for f in z.infolist():assert (dest/f.filename).resolve().is_relative_to(dest.resolve())
  z.extractall(dest)
bpy.ops.wm.read_factory_settings(use_empty=True)
def load(key):
 before=set(bpy.data.objects);path=next((ROOT/'source'/key).glob('*.fbx'))
 bpy.ops.import_scene.fbx(filepath=str(path))
 result=[o for o in set(bpy.data.objects)-before if o.type=='MESH']
 for o in result:
  w=o.matrix_world.copy();o.parent=None;o.matrix_world=w;o.data.transform(w);o.matrix_world=Matrix.Identity(4)
 return result
base=load('a');original={o.name:[tuple(v.co) for v in o.data.vertices] for o in base}
base_uv={o.name:[tuple(l.uv) for l in o.data.uv_layers.active.data] for o in base}
donor=load('b');garment=next(o for o in donor if o.name=='tripo_part_21')
for o in donor:
 if o!=garment:bpy.data.objects.remove(o,do_unlink=True)
garment.name='Drop_Overalls_Fitted'
original_uv=[tuple(l.uv) for l in garment.data.uv_layers.active.data]
body=[o for o in base if o.name not in ['tripo_part_9','tripo_part_5']]
trees=[]
for o in body:
 bm=bmesh.new();bm.from_mesh(o.data);bm.normal_update();trees.append((o.name,BVHTree.FromBMesh(bm)));bm.free()
before=[v.co.copy() for v in garment.data.vertices]
for v in garment.data.vertices:v.co=Vector((v.co.x+.005,v.co.y*.96,v.co.z*.80))
affine=[v.co.copy() for v in garment.data.vertices]
clearance=.004
def penetration(p):
 hits=[]
 for name,tree in trees:
  q,n,idx,d=tree.find_nearest(p)
  if q is not None:hits.append((d,q,n,name))
 d,q,n,name=min(hits,key=lambda x:x[0])
 signed=(p-q).dot(n)
 if d<.055 and signed<clearance:return (clearance-signed,q,n,name)
 return None
moved=set();max_push=0
for it in range(8):
 count=0
 for v in garment.data.vertices:
  hit=penetration(v.co)
  if hit:
   amount,q,n,name=hit
   if amount>1e-5:
    v.co=q+n*clearance;moved.add(v.index);max_push=max(max_push,(v.co-affine[v.index]).length);count+=1
 if not count:break
# Smooth the deformation field rather than the authored garment detail.
adj=[set() for _ in garment.data.vertices]
for e in garment.data.edges:
 a,b=e.vertices;adj[a].add(b);adj[b].add(a)
delta=[v.co-affine[v.index] for v in garment.data.vertices]
for _ in range(16):
 new=[]
 for i,d in enumerate(delta):
  avg=sum((delta[j] for j in adj[i]),Vector())/len(adj[i]) if adj[i] else d
  new.append(d.lerp(avg,.6))
 delta=new
for v in garment.data.vertices:v.co=affine[v.index]+delta[v.index]
for _ in range(2):
 for v in garment.data.vertices:
  hit=penetration(v.co)
  if hit and hit[0]>1e-5:v.co=hit[1]+hit[2]*clearance
garment.data.update()
for o in base:
 assert original[o.name]==[tuple(v.co) for v in o.data.vertices]
 assert base_uv[o.name]==[tuple(l.uv) for l in o.data.uv_layers.active.data]
assert original_uv==[tuple(l.uv) for l in garment.data.uv_layers.active.data]
def clean_materials(objects):
 for o in objects:
  for m in o.data.materials:
   bsdf=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
   for name in ['Normal','Roughness','Metallic']:
    for link in list(bsdf.inputs[name].links):m.node_tree.links.remove(link)
   bsdf.inputs['Roughness'].default_value=.68;bsdf.inputs['Metallic'].default_value=0
clean_materials([*base,garment])
def export_glb(name,objects):
 bpy.ops.object.select_all(action='DESELECT')
 for o in objects:o.select_set(True)
 bpy.ops.export_scene.gltf(filepath=str(ROOT/(name+'.glb')),export_format='GLB',use_selection=True,export_animations=False)
export_glb('a',base);export_glb('fitted',base+[garment]);export_glb('garment',[garment])
saved=[v.co.copy() for v in garment.data.vertices]
for v in garment.data.vertices:v.co=affine[v.index]
export_glb('affine',base+[garment])
for v in garment.data.vertices:v.co=before[v.index]
export_glb('unfitted',base+[garment])
for v in garment.data.vertices:v.co=saved[v.index]

report=dict(scriptSha256=hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),sourceArchives=source_record['files'],baseGeometryUnchanged=True,baseUvUnchanged=True,garmentUvUnchanged=True,garmentVertices=len(garment.data.vertices),garmentFaces=len(garment.data.polygons),affine=dict(xOffset=.005,yScale=.96,zScale=.8),clearance=clearance,correctedVertices=len(moved),largestCorrection=max_push,iterations=it+1)
bm=bmesh.new();bm.from_mesh(garment.data);report['garmentBoundaryEdges']=sum(e.is_boundary for e in bm.edges);report['garmentNonManifoldEdges']=sum(not e.is_manifold for e in bm.edges);bm.free()
(ROOT/'fit-report.json').write_text(json.dumps(report,indent=2));print(report)

# Export the separate garment in the original base coordinates, with its albedo embedded.
export=ROOT/'delivery';export.mkdir(exist_ok=True)
bpy.ops.object.select_all(action='DESELECT');garment.select_set(True)
fbx=export/'Drop_Overalls_Fitted.fbx'
bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,object_types={'MESH'},bake_anim=False,
 path_mode='COPY',embed_textures=True,axis_forward='-Z',axis_up='Y',use_mesh_modifiers=False)
exported=[v.co.copy() for v in garment.data.vertices]
uv_values=sorted((round(l.uv.x,6),round(l.uv.y,6)) for l in garment.data.uv_layers.active.data)
from io_scene_fbx import parse_fbx
fbx_tree,fbx_version=parse_fbx.parse(str(fbx))
def embedded_images(node):
 found=[]
 if node.id==b'Video':
  for child in node.elems:
   if child.id==b'Content' and child.props and child.props[0]:found.append(hashlib.sha256(child.props[0]).hexdigest())
 for child in node.elems:found.extend(embedded_images(child))
 return found
packed=embedded_images(fbx_tree);assert packed,'FBX must include its texture bytes'
basecolor=next((ROOT/'source'/'b').rglob('_basecolor.jpg'))
assert hashlib.sha256(basecolor.read_bytes()).hexdigest() in packed
# Reimport the FBX and check world coordinates, UVs, and that the file contains only one garment.
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(fbx),use_image_search=False)
roundtrip=[o for o in bpy.context.scene.objects if o.type=='MESH'];assert len(roundtrip)==1
from mathutils.kdtree import KDTree
points=[o.matrix_world@v.co for o in roundtrip for v in o.data.vertices]
assert len(points)==len(exported)
tree=KDTree(len(points))
for i,v in enumerate(points):tree.insert(v,i)
tree.balance();error=max(tree.find(v)[2] for v in exported);assert error<2e-6,error
assert uv_values==sorted((round(l.uv.x,6),round(l.uv.y,6)) for l in roundtrip[0].data.uv_layers.active.data)
report['export']=dict(file=fbx.name,sha256=hashlib.sha256(fbx.read_bytes()).hexdigest(),bytes=fbx.stat().st_size,
 meshObjects=1,worldCoordinateError=error,uvRoundTripUnchanged=True,embeddedImageSha256=packed,rigged=False)
(ROOT/'fit-report.json').write_text(json.dumps(report,indent=2)+'\n');print('FBX roundtrip verified:',report['export'])

"""Derive interchangeable headwear from the owner's three unrigged single-mesh Tripo exports.
No second Blender master. Keep the bald base / UV / paint unchanged, trim only accessory copies.
"""
import hashlib,json,zipfile,colorsys
import numpy as np
from pathlib import Path
import bpy,bmesh
from mathutils import Vector,Matrix
HERE=Path(__file__).resolve().parent;ROOT=HERE.parent.parent
SOURCE=HERE/'models/twig-modular';OUT=ROOT/'content/work/twig-modular/build'
FIT=json.loads((HERE/'twig-modular-fit.json').read_text());OUT.mkdir(parents=True,exist_ok=True)
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
bpy.ops.wm.read_factory_settings(use_empty=True)
report=dict(version=1,scriptSha256=sha(__file__),fitSha256=sha(HERE/'twig-modular-fit.json'),sources=[],parts=[],exports=[])
objects={}
for entry in json.loads((SOURCE/'source-manifest.json').read_text())['files']:
 archive=SOURCE/'source'/entry['file'];assert sha(archive)==entry['sha256']
 folder=OUT/'extracted'/entry['key'];folder.mkdir(parents=True,exist_ok=True)
 with zipfile.ZipFile(archive) as z:
  for f in z.infolist():assert (folder/f.filename).resolve().is_relative_to(folder.resolve())
  z.extractall(folder)
 previous=set(bpy.data.objects);fbx=next(folder.glob('*.fbx'));bpy.ops.import_scene.fbx(filepath=str(fbx))
 imported=set(bpy.data.objects)-previous;meshes=[o for o in imported if o.type=='MESH']
 assert len(meshes)==1 and not any(o.type=='ARMATURE' for o in imported),'Reinspect changed export structure'
 obj=meshes[0];obj.name='TwigModular_'+entry['key']
 sourcePosition=np.asarray([tuple(obj.matrix_world@v.co) for v in obj.data.vertices],dtype=np.float32) if entry['key']=='base' else None
 sourceUv=np.asarray([tuple(loop.uv) for loop in obj.data.uv_layers.active.data],dtype=np.float32) if entry['key']=='base' else None
 # Bake only the FBX import transform into the derived mesh for predictable cut coordinates.
 obj.data.transform(obj.matrix_world);obj.matrix_world=Matrix.Identity(4)
 sourceVertices=len(obj.data.vertices);sourceFaces=len(obj.data.polygons)
 for mat in obj.data.materials:
  bsdf=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED');assert bsdf.inputs['Base Color'].links
  for name in ['Normal','Metallic','Roughness']:
   for link in list(bsdf.inputs[name].links):mat.node_tree.links.remove(link)
  bsdf.inputs['Metallic'].default_value=0;bsdf.inputs['Roughness'].default_value=.68
 if entry['key']!='base':
  fit=FIT[entry['key']];a,b,c,d=fit['cutPlane'];normal=Vector((a,b,c));point=normal*(d/normal.length_squared)
  bm=bmesh.new();bm.from_mesh(obj.data)
  bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),plane_co=point,plane_no=normal,clear_inner=True,clear_outer=False,dist=1e-6)
  # The exports weld the hat to another face. Remove that face by its painted color / height,
  # then retain the connected hat region; never substitute its features for the shared base face.
  texture=next(n.image for n in obj.data.materials[0].node_tree.nodes if n.type=='TEX_IMAGE' and n.image and 'basecolor' in n.image.name.lower())
  sample=texture.copy();sample.colorspace_settings.name='Non-Color'
  width,height=sample.size;pixels=np.asarray(sample.pixels[:],dtype=np.float32).reshape(height,width,4)
  uv_layer=bm.loops.layers.uv.active
  remove=[]
  bm.normal_update()
  for face in bm.faces:
   center=sum((v.co for v in face.verts),Vector())/len(face.verts);signed=a*center.x+b*center.y+c*center.z
   uv=sum((loop[uv_layer].uv for loop in face.loops),Vector((0,0)))/len(face.loops)
   rgb=pixels[min(height-1,max(0,int(uv.y*height))),min(width-1,max(0,int(uv.x*width))),:3]
   hue,saturation,value=colorsys.rgb_to_hsv(*map(float,rgb))
   if entry['key']=='acorn':keep=signed>.41 or (center.x/.40)**2+(center.y/.36)**2>1.05 or face.normal.z<-.15
   else:keep=signed>.40 or face.normal.z<-.25 or (hue>.17 and saturation>.15) or (signed>.36 and rgb[1]>.90*rgb[0] and rgb[2]>.65*rgb[0])
   if not keep:remove.append(face)
  bmesh.ops.delete(bm,geom=remove,context='FACES')
  bpy.data.images.remove(sample)
  remaining=set(bm.faces);components=[]
  while remaining:
   start=remaining.pop();component=[start];todo=[start]
   while todo:
    current=todo.pop()
    for edge in current.edges:
     for adjacent in edge.link_faces:
      if adjacent in remaining:remaining.remove(adjacent);component.append(adjacent);todo.append(adjacent)
   components.append(component)
  largest=max(components,key=len)
  bmesh.ops.delete(bm,geom=[f for comp in components if comp is not largest for f in comp],context='FACES')
  # Remove loose remnants; only the connected upper accessory surface is retained.
  bmesh.ops.delete(bm,geom=[v for v in bm.verts if not v.link_faces],context='VERTS')
  bm.to_mesh(obj.data);bm.free();obj.data.update()
  obj.scale=fit['scale'];obj.location=fit['translation']
 else:
  assert len(obj.data.vertices)==sourceVertices and len(obj.data.polygons)==sourceFaces
  derivedPosition=np.asarray([tuple(obj.matrix_world@v.co) for v in obj.data.vertices],dtype=np.float32)
  derivedUv=np.asarray([tuple(loop.uv) for loop in obj.data.uv_layers.active.data],dtype=np.float32)
  assert np.max(np.abs(derivedPosition-sourcePosition))<1e-7 and np.array_equal(derivedUv,sourceUv),'Base geometry / UV changed'
  report['baseGeometryCheck']=dict(maxWorldCoordinateDelta=float(np.max(np.abs(derivedPosition-sourcePosition))),uvSha256=hashlib.sha256(sourceUv.tobytes()).hexdigest(),uvUnchanged=True)
 objects[entry['key']]=obj
 for extra in imported:
  if extra!=obj and extra.name in bpy.data.objects:bpy.data.objects.remove(extra,do_unlink=True)
 report['sources'].append(dict(**entry,fbxSha256=sha(fbx),rigged=False))
 report['parts'].append(dict(key=entry['key'],sourceVertices=sourceVertices,derivedVertices=len(obj.data.vertices),sourceFaces=sourceFaces,derivedFaces=len(obj.data.polygons),baseGeometryChanged=False if entry['key']=='base' else None))
root=bpy.data.objects.new('TwigModular',None);bpy.context.scene.collection.objects.link(root)
bpy.context.view_layer.update()
for key,obj in objects.items():
 world=obj.matrix_world.copy();obj.parent=root;obj.matrix_world=world
bpy.context.view_layer.update()
for name,keys in [('TwigHero_Modular.glb',['base','acorn','panama']),('TwigHero_Base.glb',['base']),('TwigHero_AcornHat.glb',['acorn']),('TwigHero_PanamaHat.glb',['panama'])]:
 bpy.ops.object.select_all(action='DESELECT');root.select_set(True)
 for k in keys:objects[k].select_set(True)
 path=OUT/name;bpy.ops.export_scene.gltf(filepath=str(path),export_format='GLB',use_selection=True,export_animations=False)
 report['exports'].append(dict(file=name,sha256=sha(path),bytes=path.stat().st_size))
(OUT/'build-report.json').write_text(json.dumps(report,indent=2)+'\n')
print('Assembled unchanged base and trimmed headwear',report['parts'])

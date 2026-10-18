"""Blender adapter for local contact surfaces and independently connected sleeves.

The diagnostic source mapping is kept in cache attributes and summarized in the
handoff report. It is not needed by the exported FBX skeleton or skinning.
"""
import bpy
import numpy as np
import math
import hashlib
from contact import separate_sleeves, ownership_field, split_contacts, open_contact_seams
from skinning import canonical, smooth
from cuff_ownership import profile as cuff_profile
from contact_lining import repair_lining

def sampler(obj):
 images={}
 for mi,mat in enumerate(obj.data.materials):
  bs=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED');ls=list(bs.inputs['Base Color'].links)
  if ls and ls[0].from_node.type=='TEX_IMAGE':
   im=ls[0].from_node.image;a=np.empty(im.size[0]*im.size[1]*4,np.float32);im.pixels.foreach_get(a);images[mi]=a.reshape(im.size[1],im.size[0],4)
 def sample(uv,mi):
  if mi not in images:return np.array([.5,.5,.5])
  a=images[mi];h,w=a.shape[:2];return a[int(np.clip(uv[1]*h,0,h-1)),int(np.clip(uv[0]*w,0,w-1)),:3].copy()
 return sample

def apply_atlas(mesh,data,name):
 charts=data['charts'];tile=128;columns=math.ceil(math.sqrt(len(charts)));size=2**math.ceil(math.log2(columns*tile));columns=size//tile;a=np.ones((size,size,4),np.float32);alluv={}
 for i,chart in enumerate(charts):
  x=i%columns*tile;y=i//columns*tile;pts=chart['points'][:,1:3];lo=pts.min(0);hi=pts.max(0);span=np.maximum(hi-lo,.0001);span*=1.08;lo=(lo+hi-span)*.5
  gy,gx=np.mgrid[0:tile,0:tile];q=np.c_[gx.ravel(),gy.ravel()]/(tile-1)*span+lo;delta=q[:,None]-pts[None];dist=(delta*delta).sum(2)
  count=min(8,len(pts));idx=np.argpartition(dist,count-1,axis=1)[:,:count];d=np.take_along_axis(dist,idx,axis=1);w=1/np.maximum(d,1e-9);w/=w.sum(1)[:,None];colors=(chart['colors'][idx]*w[:,:,None]).sum(1)
  mix=np.clip(np.sqrt(d.min(1))/.02,0,.85);colors=colors*(1-mix[:,None])+chart['centercolor']*mix[:,None]
  a[y:y+tile,x:x+tile,:3]=colors.reshape(tile,tile,3)
  for fi in chart['faces']:
   coords=data['points'][data['faces'][fi],1:3];alluv[fi]=((coords-lo)/span*(tile-1)+[x+.5,y+.5])/size
 im=bpy.data.images.new(name+'.png',width=size,height=size,alpha=True);im.colorspace_settings.name='sRGB';im.pixels.foreach_set(a.ravel());im.pack()
 mat=bpy.data.materials.new(name);mat.use_nodes=True;bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=.68;tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=im;mat.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color']);mesh.materials.append(mat);mi=len(mesh.materials)-1
 for fi,uv in alluv.items():
  po=mesh.polygons[int(fi)];po.material_index=mi
  for li,xy in zip(po.loop_indices,uv):mesh.uv_layers.active.data[li].uv=xy

 return {'sha256': hashlib.sha256(bytes(im.packed_file.data)).hexdigest(), 'width': size, 'height': size, 'charts': len(charts)}


def attribute(mesh, name, kind, domain, values):
    layer = mesh.attributes.new(name, kind, domain)
    property_name = 'vector' if kind == 'FLOAT_VECTOR' else 'value'
    layer.data.foreach_set(property_name, np.asarray(values).ravel())


def repair(obj, family, part, original):
    if part not in ('base', 'Jacket', 'Coat', 'Overalls'):
        return original, None, None
    old = obj.data
    assert old.shape_keys is None, (part, 'Contact repair must preserve authoring morphs')
    old.calc_loop_triangles()
    transform = np.array(obj.matrix_world)
    local = np.array([v.co[:] for v in old.vertices])
    world = local @ transform[:3, :3].T + transform[:3, 3]
    triangles = np.array([t.vertices[:] for t in old.loop_triangles])
    separate = separate_sleeves(world, triangles, original, family) if part != 'base' else None
    field = separate[0] if separate is not None else ownership_field(world, triangles, original, family, part)
    authored_cuff = cuff_profile(obj, family, part)
    if authored_cuff is not None:
        field[authored_cuff['vertexIds']] = 1
    points = canonical(world, family)
    loops = np.array([t.loops[:] for t in old.loop_triangles])
    uv = np.array([v.uv[:] for v in old.uv_layers.active.data])
    normals = np.array([n.vector[:] for n in old.corner_normals]) @ np.linalg.inv(transform[:3, :3])
    normals = canonical(normals, family)
    normals /= np.maximum(np.linalg.norm(normals, axis=1)[:, None], 1e-10)
    material_ids = np.array([t.material_index for t in old.loop_triangles])
    protected = (points[:, 2] >= .39) | (points[:, 2] <= .17)
    if part == 'base':
        protected |= original.get('Head', np.zeros(len(points))) > .999
    existing_seams = open_contact_seams(points, triangles, loops, uv, normals, material_ids, field) if separate is not None else None
    if separate is not None:
        assert len(existing_seams) > 30, (part, 'Unrecognized open sleeve attachment')
    data = split_contacts(points, triangles, loops, uv, normals, material_ids, field,
                          1-smooth(.27, .335, points[:, 2]), sampler(obj), protected, body=part == 'base', existing_seams=existing_seams)
    albedo_retouch = repair_lining(obj, data, family, part) if authored_cuff is not None else None
    count = len(data['points'])
    source_count = len(points)
    repaired_world = canonical(data['points'], family)
    repaired_local = (repaired_world-transform[:3, 3]) @ np.linalg.inv(transform[:3, :3]).T
    mesh = bpy.data.meshes.new(old.name+'_ContactRepair')
    mesh.from_pydata(repaired_local, [], data['faces'])
    mesh.update()
    layer = mesh.uv_layers.new(name=old.uv_layers.active.name)
    layer.data.foreach_set('uv', np.asarray(data['uv'], dtype=np.float32).ravel())
    for material in old.materials:
        mesh.materials.append(material)
    for polygon, material in zip(mesh.polygons, data['material_ids']):
        polygon.material_index = int(material)
        polygon.use_smooth = True
    local_normals = canonical(data['normals'], family) @ transform[:3, :3]
    local_normals /= np.maximum(np.linalg.norm(local_normals, axis=1)[:, None], 1e-10)
    mesh.normals_split_custom_set(local_normals.tolist())
    atlas = apply_atlas(mesh, data, family+'_'+part+'_ContactAlbedo')
    obj.data = mesh

    source_vertex = np.r_[np.arange(source_count), np.full(count-source_count, -1)]
    edge_a, edge_b = np.full(count, -1), np.full(count, -1)
    edge_t = np.zeros(count)
    offset = np.zeros((count, 3))
    for i, delta in data['displacements'].items():
        offset[i, 0] = delta
    for i, a, b, fraction, side in data['mapping']:
        edge_a[i], edge_b[i], edge_t[i] = a, b, fraction
    attribute(mesh, 'rig_source_vertex', 'INT', 'POINT', source_vertex)
    attribute(mesh, 'rig_source_triangle', 'INT', 'FACE', data['source_faces'])
    attribute(mesh, 'rig_source_edge_a', 'INT', 'POINT', edge_a)
    attribute(mesh, 'rig_source_edge_b', 'INT', 'POINT', edge_b)
    attribute(mesh, 'rig_source_edge_t', 'FLOAT', 'POINT', edge_t)
    attribute(mesh, 'rig_contact_offset', 'FLOAT_VECTOR', 'POINT', canonical(offset, family))
    attribute(mesh, 'rig_arm_ownership', 'FLOAT', 'POINT', data['tags'])
    obj['contact_repair_kind'] = 'separateSleeveClosure' if separate is not None else 'localContactSurface'
    obj['rig_source_vertex_count'] = source_count
    obj['rig_source_triangle_count'] = len(triangles)

    expanded = {n: np.r_[w, np.zeros(count-source_count)] for n, w in original.items()}
    for i, a, b, fraction, side in data['mapping']:
        for w in expanded.values():
            w[i] = (1-fraction)*w[a]+fraction*w[b]
    # Interior vertices use anatomical torso/arm fields. They must not inherit
    # a head weight or an unrelated hand weight from the opposite end of a rim.
    moved = np.flatnonzero(np.linalg.norm(offset[:source_count], axis=1) > 1e-8)
    source_area = np.linalg.norm(np.cross(points[triangles[:, 1]]-points[triangles[:, 0]],
                                         points[triangles[:, 2]]-points[triangles[:, 0]]), axis=1)*.5
    exterior = data['source_faces'] >= 0
    raw_points = data['points']-offset
    f = data['faces'][exterior]
    area = np.linalg.norm(np.cross(raw_points[f[:, 1]]-raw_points[f[:, 0]], raw_points[f[:, 2]]-raw_points[f[:, 0]]), axis=1)*.5
    retained = np.bincount(data['source_faces'][exterior], weights=area, minlength=len(triangles))
    removed = np.flatnonzero(retained < source_area-1e-10)
    interior_ids = np.unique(data['faces'][~exterior])
    changed_ids = np.unique(np.r_[moved, interior_ids, np.arange(source_count, count)])
    bounds = np.stack([data['points'][changed_ids].min(0), data['points'][changed_ids].max(0)])
    report = {'method': obj['contact_repair_kind'], 'geometryChanged': True,
              'sourceVertices': source_count, 'sourceTriangles': len(triangles),
              'movedSourceVertexIds': moved.tolist(),
              'movedSourceSideOffsets': offset[moved, 0].tolist(),
              'untouchedSourceVertices': source_count-len(moved),
              'maxSourceDisplacement': float(np.linalg.norm(offset[:source_count], axis=1).max()),
              'sourceDisplacementLimit': .035,
              'removedOrClippedSourceTriangleIds': removed.tolist(),
              'sourceAreaRemoved': float(np.maximum(source_area-retained, 0).sum()),
              'sourceAreaRemovedFraction': float(np.maximum(source_area-retained, 0).sum()/source_area.sum()),
              'addedSeamVertices': len(data['mapping']),
              'addedInteriorVertices': count-source_count-len(data['mapping']),
              'interiorTriangles': int((~exterior).sum()),
              'retainedExteriorTriangles': int(exterior.sum()),
              'contactBoundsCanonical': bounds.tolist(), 'interiorAlbedo': atlas,
              'sourceUvPreserved': True}
    if separate is not None:
        report['sourceComponents'] = separate[1]['sourceComponents']
        report['sleeves'] = separate[1]['sleeves']
        report['sourceContactBoundaryEdges'] = len(existing_seams)
    if authored_cuff is not None:
        report['authoredCuffOwnership'] = authored_cuff
        report['sourceAlbedoRetouch'] = albedo_retouch
    assert report['maxSourceDisplacement'] <= .0350001
    assert not np.any(np.linalg.norm(offset[:source_count][protected], axis=1) > 1e-8)
    if old.users == 0:
        bpy.data.meshes.remove(old)
    return expanded, data['tags'].astype(float), report

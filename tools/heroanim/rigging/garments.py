"""Close overlapping shoulder seams instead of stretching a generated jacket's side panel.

The existing visible faces, coordinates, UV loops, materials and corner normals are kept.
Only seam vertices are duplicated; hidden inward caps close each independently skinned part.
"""
import bpy
import numpy as np
from mathutils import Vector
from skinning import canonical, world_vector, fields
from sleeves import append_sleeves


def split_sleeves(obj, family, original):
    assert not obj.data.shape_keys, "Garment fit morphs require an explicit seam transfer"
    old = obj.data
    world = np.array([obj.matrix_world @ v.co for v in old.vertices])
    canon = canonical(world, family)
    old_uv = old.uv_layers.active
    image_pixels = {}

    def face_color(face):
        mat = old.materials[face.material_index]
        bs = next((n for n in mat.node_tree.nodes if n.type == 'BSDF_PRINCIPLED'), None)
        if not bs:
            return np.array([0., 0., 0.])
        links = bs.inputs['Base Color'].links
        if not links or links[0].from_node.type != 'TEX_IMAGE':
            return np.array(bs.inputs['Base Color'].default_value[:3])
        image = links[0].from_node.image
        if image.name not in image_pixels:
            pixels = np.empty(len(image.pixels), dtype=np.float32)
            image.pixels.foreach_get(pixels)
            image_pixels[image.name] = pixels.reshape(image.size[1], image.size[0], 4)
        pixels = image_pixels[image.name]
        tex = np.mean([old_uv.data[i].uv[:] for i in face.loop_indices], axis=0) % 1
        return pixels[min(int(tex[1] * image.size[1]), image.size[1]-1), min(int(tex[0] * image.size[0]), image.size[0]-1), :3]
    names, weights = fields(world, family, obj["part_key"], original=original)
    scores = []
    for side in ("L", "R"):
        cols = [i for i, n in enumerate(names) if n in ("UpperArm." + side, "Forearm." + side)]
        scores.append(weights[:, cols].sum(axis=1))
    face_zones = []
    cuff_zones = []
    for face in old.polygons:
        s = [float(a[list(face.vertices)].mean()) for a in scores]
        zone = 1 + int(s[1] > s[0]) if max(s) > .35 else 0
        center = canon[list(face.vertices)].mean(axis=0)
        # Generated cuffs can reach inside the spatial armpit boundary. Keep the
        # entire cream cuff with its sleeve, including the hidden inner underside.
        cuff = 0
        if obj['part_key'] in ('Jacket', 'Coat') and abs(center[0]) > (.13 if family == 'drop' else .16) and .10 < center[2] < .30 and -.10 < center[1] < .17:
            r,g,b = face_color(face)
            if r>.60 and g>.48 and b>.30 and r>=g*.95 and g>=b*.98 and r-g<.25 and g-b<.25:
                zone = 1 if center[0] > 0 else 2
                cuff = zone
        if obj['part_key'] in ('Jacket','Coat') and center[2]>.305:
            r,g,b=face_color(face)
            if r>.60 and g>.48 and b>.30 and r>=g*.95 and g>=b*.98 and r-g<.25 and g-b<.25:
                zone=0  # The whole collar belongs to Chest, including its outer tips.
        face_zones.append(zone)
        cuff_zones.append(cuff)
    # Nearest-surface source weights can alternate across a single vertex. Smooth
    # those label islands before making a cut; a seam must be a ring, never a bow tie.
    incident = [[] for _ in old.vertices]
    edge_faces = {}
    for p in old.polygons:
        ids = list(p.vertices)
        for v in ids:
            incident[v].append(p.index)
        for a, b in zip(ids, ids[1:] + ids[:1]):
            edge_faces.setdefault(tuple(sorted((a, b))), []).append(p.index)
    for _ in range(64):
        degree = {}
        for (a, b), adjacent in edge_faces.items():
            if len(adjacent) != 2 or face_zones[adjacent[0]] == face_zones[adjacent[1]]:
                continue
            for zone in (face_zones[adjacent[0]], face_zones[adjacent[1]]):
                degree[(a, zone)] = degree.get((a, zone), 0) + 1
                degree[(b, zone)] = degree.get((b, zone), 0) + 1
        bad = sorted({v for (v, _), n in degree.items() if n > 2})
        if not bad:
            break
        for v in bad:
            labels = [face_zones[i] for i in incident[v]]
            cuffs = [cuff_zones[i] for i in incident[v] if cuff_zones[i]]
            majority = max((1, 2),key=cuffs.count) if cuffs else max((0, 1, 2), key=lambda k: (labels.count(k), -k))
            for p in incident[v]:
                face_zones[p] = majority
    else:
        raise AssertionError((obj.name, "cannot form a manifold shoulder seam"))
    repaired_underarms = []
    if obj['part_key'] in ('Jacket', 'Coat'):
        for face in old.polygons:
            center = canon[list(face.vertices)].mean(axis=0)
            if face_zones[face.index] == 0 and abs(center[0]) > (.115 if family=='drop' else .15) and center[2] < .30 and -.13 < center[1] < .22:
                face_zones[face.index] = -1
                repaired_underarms.append(face.index)
    assert all(face_zones.count(zone) > 30 for zone in (1, 2)), (family, obj.name, "missing sleeve")
    coords, mapping, zones, faces, uv_faces, material_ids, smooth_faces, normals = [], [], [], [], [], [], [], []
    vertex_map, source_edges = {}, {}
    source_uv_indices = []

    def vertex(index, zone):
        key = (index, zone)
        if key not in vertex_map:
            vertex_map[key] = len(coords)
            coords.append(tuple(old.vertices[index].co))
            mapping.append(index)
            zones.append(zone)
        return vertex_map[key]

    for p, zone in zip(old.polygons, face_zones):
        if zone != 0:
            continue
        ids = list(p.vertices)
        faces.append(tuple(vertex(i, zone) for i in ids))
        uv = [tuple(old_uv.data[i].uv) for i in p.loop_indices]
        uv_faces.append(uv)
        material_ids.append(p.material_index)
        smooth_faces.append(p.use_smooth)
        normals.extend(tuple(old.corner_normals[i].vector) for i in p.loop_indices)
        source_uv_indices.extend(p.loop_indices)
        for j, a in enumerate(ids):
            b = ids[(j + 1) % len(ids)]
            source_edges.setdefault(tuple(sorted((a, b))), []).append((zone, a, b, uv[j], uv[(j + 1) % len(ids)], p.material_index))

    outer_faces = len(faces)
    caps = []
    for zone in (0, 1, 2):
        edges = [next(e for e in es if e[0] == zone) for es in source_edges.values()
                 if len(es) == 2 and es[0][0] != es[1][0] and any(e[0] == zone for e in es)]
        # A garment can have several seam loops. Follow connectivity rather than
        # bridging unrelated shoulder / decorative islands with one giant fan.
        pending = list(edges)
        while pending:
            component = [pending.pop()]
            boundary = {component[0][1], component[0][2]}
            changed = True
            while changed:
                changed = False
                for i in range(len(pending) - 1, -1, -1):
                    if pending[i][1] in boundary or pending[i][2] in boundary:
                        edge = pending.pop(i)
                        component.append(edge)
                        boundary.update((edge[1], edge[2]))
                        changed = True
            degree = {i: 0 for i in boundary}
            for e in component:
                degree[e[1]] += 1; degree[e[2]] += 1
            assert all(v <= 2 for v in degree.values()), (obj.name, zone, "non-manifold shoulder seam", sorted(degree.values()))
            source_ids = sorted(boundary)
            middle = world[source_ids].mean(axis=0)
            lateral = canonical(middle[None], family)[0, 0]
            push = Vector(world_vector((-.018 * (1 if lateral > 0 else -1), 0, 0), family))
            center = obj.matrix_world.inverted() @ (Vector(middle) + push)
            index = len(coords)
            coords.append(tuple(center)); mapping.append(-1); zones.append(zone)
            caps.append({"vertex": index, "sources": source_ids, "zone": zone})
            for _, a, b, ua, ub, mat in component:
                faces.append((vertex(b, zone), vertex(a, zone), index))
                uv_faces.append([ub, ua, tuple((np.array(ua) + np.array(ub)) * .5)])
                material_ids.append(mat); smooth_faces.append(True)
    # A small inner shoulder shell covers the skin/cloth join when the arm opens.
    # It sits inside the accepted garment at rest and follows the half-angle bone.
    socket_faces_start = len(faces)
    if obj['part_key'] in ('Jacket', 'Coat'):
        shoulder = .085 if family == 'drop' else .1752
        low, high = canon[:,2].min()+.004, min(.385,canon[:,2].max()-.008)
        body_points=[]
        for body in bpy.context.scene.objects:
            if body.type=='MESH' and body.get('part_key') in ('base','Base_Body'):
                p=canonical(np.array([body.matrix_world@v.co for v in body.data.vertices]),family)
                body_points.extend(p[(np.abs(p[:,0])<.12)&(p[:,2]>.16)&(p[:,2]<.28)])
        base_depth=np.array(body_points)[:,1]
        back,front=np.quantile(base_depth,[.02,.98])+np.array([-.006,.006])
        body_center=np.array([0,(front+back)/2,(low+high)/2])
        body_radii=np.array([.178 if family=='drop' else .195,(front-back)/2,(high-low)/2])
        shells=[(5,body_center,body_radii)]
        for zone,center,radii in shells:
            first = len(coords)
            rings,segments = 10,16
            coords.append(tuple(obj.matrix_world.inverted()@Vector(world_vector(center+np.array([0,0,radii[2]]),family))))
            mapping.append(-1);zones.append(zone)
            for ring in range(1,rings):
                latitude=np.pi*ring/rings
                for segment in range(segments):
                    longitude=2*np.pi*segment/segments
                    radial=np.sin(latitude)**(.5 if zone==5 else 1)
                    vertical=np.sign(np.cos(latitude))*abs(np.cos(latitude))**(.5 if zone==5 else 1)
                    p=center+radii*np.array([radial*np.cos(longitude),radial*np.sin(longitude),vertical])
                    coords.append(tuple(obj.matrix_world.inverted()@Vector(world_vector(p,family))))
                    mapping.append(-1);zones.append(zone)
            bottom=len(coords)
            coords.append(tuple(obj.matrix_world.inverted()@Vector(world_vector(center-np.array([0,0,radii[2]]),family))))
            mapping.append(-1);zones.append(zone)
            for segment in range(segments):
                faces.append((first,first+1+(segment+1)%segments,first+1+segment));uv_faces.append([(0,0)]*3);material_ids.append(0);smooth_faces.append(True)
                last=first+1+(rings-2)*segments
                faces.append((last+segment,last+(segment+1)%segments,bottom));uv_faces.append([(0,0)]*3);material_ids.append(0);smooth_faces.append(True)
            for ring in range(rings-2):
                for segment in range(segments):
                    a=first+1+ring*segments+segment;b=first+1+ring*segments+(segment+1)%segments
                    c=b+segments;d=a+segments
                    faces.append((a,b,c,d));uv_faces.append([(0,0)]*4);material_ids.append(0);smooth_faces.append(True)
    arm_parameters,sleeve_faces,cuff_faces=append_sleeves(obj,family,coords,mapping,zones,faces,uv_faces,material_ids,smooth_faces)
    sleeve_faces=set(sleeve_faces);cuff_faces=set(cuff_faces)
    mesh = bpy.data.meshes.new(old.name + "_shoulder_seams")
    mesh.from_pydata(coords, [], faces)
    mesh.update()
    for mat in old.materials:
        mesh.materials.append(mat)
    # A newly exposed inner surface must use the garment fabric. Interpolating UVs
    # across unrelated atlas islands paints pale cuff-shaped triangles on a jacket.
    samples = []
    sample_areas=[]
    for face, zone in zip(old.polygons, face_zones):
        center = canon[list(face.vertices)].mean(axis=0)
        if zone != 0 or abs(center[0]) > .12 or not (.18 < center[2] < .32) or center[1] < .08:
            continue
        samples.append(face_color(face))
        sample_areas.append(face.area)
    if samples:
        import colorsys
        hsv=np.array([colorsys.rgb_to_hsv(*c) for c in samples])
        areas=np.array(sample_areas)
        saturated=hsv[:,1]>.15
        bins=(hsv[:,0]*12).astype(int)%12
        totals=np.bincount(bins,weights=areas*saturated,minlength=12)
        selected=(bins==np.argmax(totals))&saturated if totals.max()>0 else np.ones(len(samples),bool)
        color=np.average(np.array(samples)[selected],axis=0,weights=areas[selected])
    else:
        color=np.array([.20,.25,.15])
    if image_pixels:
        # Image.pixels contains the encoded PNG values; material factors are linear.
        color=np.where(color<=.04045,color/12.92,((color+.055)/1.055)**2.4)
    lining = bpy.data.materials.new(obj.name + '_RigInner')
    lining.use_nodes = True
    lining.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value = (*color.tolist(), 1)
    lining.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value = .68
    mesh.materials.append(lining)
    lining_index = len(mesh.materials) - 1
    shirt_index=lining_index
    if obj['part_key']=='Overalls':
        shirt_samples=[face_color(face)for face,zone in zip(old.polygons,face_zones)if zone in (1,2)]
        shirt_color=np.median(shirt_samples,axis=0)
        shirt_color=np.where(shirt_color<=.04045,shirt_color/12.92,((shirt_color+.055)/1.055)**2.4)
        shirt=lining.copy();shirt.name=obj.name+'_ShirtFabric'
        shirt.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=(*shirt_color.tolist(),1)
        mesh.materials.append(shirt);shirt_index=len(mesh.materials)-1
    cuff=lining.copy();cuff.name=obj.name+'_Cuff'
    cuff_color=np.array([.96,.88,.74]) if family in ('twig','sprig') else np.array([.98,.88,.84]) if family=='bloom' else np.array([.85,.93,.97])
    cuff_color=np.where(cuff_color<=.04045,cuff_color/12.92,((cuff_color+.055)/1.055)**2.4)
    cuff.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=(*cuff_color.tolist(),1)
    mesh.materials.append(cuff);cuff_index=len(mesh.materials)-1
    uv = mesh.uv_layers.new(name=old_uv.name)
    for p, tex, mat, smooth in zip(mesh.polygons, uv_faces, material_ids, smooth_faces):
        p.material_index = mat if p.index < outer_faces else lining_index
        if p.index in sleeve_faces:
            p.material_index=shirt_index
        if p.index in cuff_faces:
            p.material_index=cuff_index
        p.use_smooth = smooth
        for loop, value in zip(p.loop_indices, tex):
            uv.data[loop].uv = value
    mesh.update()
    old_loop_count = len(source_uv_indices)
    assert all(tuple(uv.data[i].uv) == tuple(old_uv.data[source_uv_indices[i]].uv) for i in range(old_loop_count))
    normals.extend(tuple(n.vector) for n in mesh.corner_normals[old_loop_count:])
    mesh.normals_split_custom_set(normals)
    obj.data = mesh
    obj["closed_shoulder_seams"] = True
    transferred = {}
    mapping = np.array(mapping)
    for name, values in original.items():
        a = values[np.maximum(mapping, 0)].copy()
        for cap in caps:
            a[cap["vertex"]] = values[cap["sources"]].mean()
        transferred[name] = a
    return transferred, np.array(zones), {"sourceVertices": len(old.vertices), "vertices": len(coords),
                                          "visibleFacesKept": outer_faces, "capFaces": len(faces) - outer_faces,
                                          "seamLoops": len(caps), "newToOld": mapping.tolist(),
                                          "zones": zones, "oldUvLoops": old_loop_count,
                                          "sourceUvIndices":source_uv_indices,"removedInternalUnderarmFaces":repaired_underarms,
                                          "innerFabricColor":color.tolist(),
                                          "replacedSleeveFaces":sum(z in (1,2)for z in face_zones),
                                          "armParameters":arm_parameters,
                                          "shoulderShellFaces": len(faces)-socket_faces_start,
                                          "visibleCornerNormalsKept": True}

"""Geometry repairs for segmented garments. Never process hero skin or accessory meshes.

Work in metre-sized world coordinates. Keep material/UV/deform custom data on
retained faces; interpolate that data onto repairs. Neck, sleeve and hem openings
remain openings. Only small accidental holes are capped.
"""
import bmesh
import numpy as np
from mathutils import Vector

def repair_garment(obj, settings=None):
    settings=settings or {}; mesh=obj.data; before=(len(mesh.vertices),len(mesh.polygons))
    assert not mesh.shape_keys, 'Repair the rest garment before adding shape keys'
    bm=bmesh.new();bm.from_mesh(mesh)
    bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=0.000002)
    bmesh.ops.dissolve_degenerate(bm,edges=list(bm.edges),dist=0.0000001)
    # Delete tiny disconnected reconstruction scraps, retaining larger buttons,
    # pockets, straps, petals and other intentionally separate decorations.
    pending=set(bm.verts);scraps=0
    while pending:
        v=pending.pop();component={v};stack=[v]
        while stack:
            for edge in stack.pop().link_edges:
                for other in edge.verts:
                    if other in pending:pending.remove(other);component.add(other);stack.append(other)
        if len(component)>24:continue
        points=[v.co for v in component]
        span=Vector(tuple(max(p[i] for p in points)-min(p[i] for p in points) for i in range(3)))
        if span.length<.018:
            scraps+=len(component);bmesh.ops.delete(bm,geom=list(component),context='VERTS')
    # Some donor neck fragments remain connected to the cloth. Their blue/green
    # skin pixels are useful only as a selection aid: remove those actual faces,
    # restricted to the authored neck region, rather than editing the texture.
    leaks=[];mode=settings.get('neckSkin');uv_layer=bm.loops.layers.uv.active
    if mode and uv_layer:
        textures={}
        for index,mat in enumerate(mesh.materials):
            node=next((n for n in mat.node_tree.nodes if n.type=='TEX_IMAGE' and n.image and 'normal' not in n.name.lower() and 'rough' not in n.image.name.lower() and 'metal' not in n.image.name.lower()),None)
            bs=next((n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
            if bs and bs.inputs['Base Color'].links:node=bs.inputs['Base Color'].links[0].from_node
            if node and node.type=='TEX_IMAGE':
                im=node.image;w,h=im.size;pixels=np.array(im.pixels[:],dtype=np.float32).reshape(h,w,4);textures[index]=(pixels,w,h)
        for f in bm.faces:
            c=f.calc_center_median()
            if c.z<settings.get('skinMinZ',.31) or abs(c.y if mode=='blue' else c.x)>settings.get('skinHalfWidth',.12):continue
            tex=textures.get(f.material_index)
            if tex is None:continue
            pixels,w,h=tex;colors=[]
            for loop in f.loops:
                uv=loop[uv_layer].uv;colors.append(pixels[min(h-1,max(0,int(uv.y*h))),min(w-1,max(0,int(uv.x*w))),:3])
            r,g,b=np.median(colors,axis=0)
            if (mode=='blue' and b>r*1.5 and b>g*1.06) or (mode=='green' and g>r*1.13 and g>b*1.25):leaks.append(f)
        bmesh.ops.delete(bm,geom=leaks,context='FACES')
        loose=[v for v in bm.verts if not v.link_faces]
        if loose:bmesh.ops.delete(bm,geom=loose,context='VERTS')
    top=settings.get('topCut')
    cut_faces=0
    if top is not None:
        result=bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=1e-7,plane_co=(0,0,top),plane_no=(0,0,1),clear_outer=True,clear_inner=False)
        cut_faces=len(result['geom_cut'])
    # Relax only a narrow band along cut borders. Interior pockets/buttons and
    # fabric folds stay fixed. Boundary movement tapers out over three edge rings.
    boundary={v for e in bm.edges if e.is_boundary for v in e.verts};band={v:0 for v in boundary};front=boundary
    for ring in [1,2,3]:
        following={other for v in front for e in v.link_edges for other in e.verts if other not in band}
        band.update({v:ring for v in following});front=following
    original={v:v.co.copy() for v in band}
    for _ in range(settings.get('iterations',24)):
        nxt={}
        for v,ring in band.items():
            neighbours=[e.other_vert(v) for e in v.link_edges if ring or e.is_boundary]
            if len(neighbours)<2:continue
            factor=[.55,.24,.08,.02][ring]
            nxt[v]=v.co.lerp(sum((n.co for n in neighbours),Vector())/len(neighbours),factor)
        for v,co in nxt.items():v.co=co
    # Cap only short, closed accidental tears, with UVs copied from the adjacent
    # cloth. Do not fill the intended neck, arm, waist or leg openings.
    todo={e for e in bm.edges if e.is_boundary};filled=0;intentional=0
    while todo:
        seed=todo.pop();edges={seed};verts=set(seed.verts);stack=list(seed.verts)
        while stack:
            for e in stack.pop().link_edges:
                if e in todo:
                    todo.remove(e);edges.add(e)
                    for v in e.verts:
                        if v not in verts:verts.add(v);stack.append(v)
        extent=Vector(tuple(max(v.co[i] for v in verts)-min(v.co[i] for v in verts) for i in range(3))).length
        closed=all(sum(e in edges for e in v.link_edges)==2 for v in verts)
        if closed and extent<.028 and len(edges)<48:
            adjacent={f for e in edges for f in e.link_faces}
            ref=next(iter(adjacent),None);uv_layer=bm.loops.layers.uv.active
            uv_by_vertex={l.vert:l[uv_layer].uv.copy() for f in adjacent for l in f.loops} if uv_layer else {}
            new=bmesh.ops.holes_fill(bm,edges=list(edges),sides=0)['faces']
            for f in new:
                if ref:f.material_index=ref.material_index;f.smooth=True
                if uv_layer:
                    for loop in f.loops:
                        if loop.vert in uv_by_vertex:loop[uv_layer].uv=uv_by_vertex[loop.vert]
            filled+=len(new)
        else:intentional+=1
    # Give exposed fabric edges an inward return, rather than leaving a paper-thin
    # sawtooth silhouette. UV and skin weights are copied onto each return vertex.
    bm.normal_update();boundary_edges=[e for e in bm.edges if e.is_boundary];rim={};uv=bm.loops.layers.uv.active
    deform=bm.verts.layers.deform.active
    records=[(v,v.co-v.normal*.0015,dict(v[deform]) if deform else {}) for v in {v for e in boundary_edges for v in e.verts}]
    for v,co,weights in records:
        n=bm.verts.new(co)
        if deform:
            for group,weight in weights.items():n[deform][group]=weight
        rim[v]=n
    for e in boundary_edges:
        a,b=e.verts;ref=e.link_faces[0];f=bm.faces.new((a,b,rim[b],rim[a]));f.material_index=ref.material_index;f.smooth=True
        if uv:
            data={l.vert:l[uv].uv.copy() for l in ref.loops}
            for loop,source in zip(f.loops,[a,b,b,a]):loop[uv].uv=data[source]
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(mesh);bm.free();mesh.update()
    assert len(mesh.uv_layers.active.data)==len(mesh.loops)
    assert all(np.isfinite(v.co[:]).all() for v in mesh.vertices)
    # Every added vertex inherits a normalized deform dictionary from its border.
    assert all(v.groups and abs(sum(g.weight for g in v.groups)-1)<1e-4 for v in mesh.vertices)
    return dict(beforeVertices=before[0],beforeFaces=before[1],vertices=len(mesh.vertices),faces=len(mesh.polygons),removedScrapVertices=scraps,removedNeckSkinFaces=len(leaks),trimIntersections=cut_faces,filledAccidentalHoleFaces=filled,openingsRetained=intentional,rimVertices=len(rim),boundaryBandVertices=len(band),uvLoops=len(mesh.loops),normalizedWeights=True)

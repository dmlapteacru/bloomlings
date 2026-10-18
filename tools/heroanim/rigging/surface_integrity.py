"""Independently verify the source correspondence of a local contact repair."""
import hashlib
import base64
import zlib
from pathlib import Path
import numpy as np
from skinning import canonical


def verify_albedo_retouch(mesh, original, contract, family, part, source_rest, rest, triangles, source_loops, source_uv, seam):
    records = contract.get('sourceAlbedoRetouch', [])
    if not records:
        return {}, []
    assert (family, part) in {('sprig', 'Jacket'), ('bloom', 'Coat')}
    def image(material):
        bs = next(n for n in material.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
        return next(link.from_node.image for link in bs.inputs['Base Color'].links if link.from_node.type == 'TEX_IMAGE')
    def digest(im):
        return hashlib.sha256(bytes(im.packed_file.data)).hexdigest()
    def pixels(im):
        values = np.empty(im.size[0]*im.size[1]*4, np.float32)
        im.pixels.foreach_get(values)
        return values.reshape(im.size[1], im.size[0], 4)
    images = {digest(image(m)): image(m) for m in mesh.materials}
    checked, summaries = {}, []
    for row in records:
        assert row['seamDistance'] == .022
        assert row['armSelection'] == ('inwardNormal' if family == 'sprig' else 'innerSide')
        old = image(original.materials[row['sourceMaterial']])
        assert digest(old) == row['sourceAlbedoSha256']
        new = images[row['modifiedAlbedoSha256']]
        width, height = old.size[:]
        assert list(new.size) == [width, height] and [row['width'], row['height']] == [width, height]
        packed = zlib.decompress(base64.b64decode(row['uvMask']))
        assert hashlib.sha256(packed).hexdigest() == row['uvMaskSha256']
        mask = np.unpackbits(np.frombuffer(packed, np.uint8))[:width*height].reshape(height, width).astype(bool)
        # The accepted Sprig texture is 1024²; Bloom is 2048² with a different
        # UV allocation. The independently checked source-face region is shared.
        assert mask.sum() == row['permittedPixels']
        assert mask.mean() < (.09 if family == 'sprig' else .025)
        before, after = pixels(old), pixels(new)
        delta = abs(after-before)
        changed = np.any(delta > 1e-6, axis=2)
        assert not np.any(changed & ~mask), 'Source albedo changed outside the recorded UV mask'
        assert delta[:, :, 3].max() < 1e-6, 'Albedo repair changed transparency'
        assert changed.sum() == row['changedPixels']
        assert abs(float(delta.max())-row['maxChannelChange']) < 1e-6
        ids = row['sourceTriangleIds']
        assert ids == sorted(set(ids)) and min(ids) >= 0 and max(ids) < len(triangles)
        # The declared UV mask is itself constrained by real source geometry.
        # It cannot authorize painting unrelated panels, trim, pockets or heads.
        centers = canonical(source_rest[triangles[ids]].mean(1), family)
        assert centers[:, 2].min() > .14 and centers[:, 2].max() < .39
        assert abs(centers[:, 0]).min() > .08 and abs(centers[:, 0]).max() < .31
        assert abs(centers[:, 1]-.03).max() < .17
        boundary = rest[seam]
        current = rest[triangles[ids]]
        midpoint = current.mean(1)
        radius = np.linalg.norm(current-midpoint[:, None], axis=2).max(1)
        nearest = np.empty(len(ids))
        for start in range(0, len(ids), 128):
            nearest[start:start+128] = np.sqrt(((midpoint[start:start+128, None]-boundary[None])**2).sum(2).min(1))
        assert np.all(nearest <= .022+radius+2e-6), 'Retouched face is outside the contact seam'
        # Independently rasterize complete source UV triangles using cross
        # products. The painter uses clipped triangles and a different rasterizer.
        coverage = np.zeros((height, width), bool)
        for si in ids:
            xy = source_uv[source_loops[si]]*[width, height]-.5
            lo = np.maximum(np.floor(xy.min(0)).astype(int)-1, 0)
            hi = np.minimum(np.ceil(xy.max(0)).astype(int)+1, [width-1, height-1])
            yy, xx = np.mgrid[lo[1]:hi[1]+1, lo[0]:hi[0]+1]
            q = np.stack([xx, yy], -1)
            cross = lambda a, b: a[..., 0]*b[..., 1]-a[..., 1]*b[..., 0]
            a, b, c = xy
            area = cross(b-a, c-a)
            if abs(area) < 1e-10:
                continue
            first = cross(q-a, c-a)/area
            second = cross(b-a, q-a)/area
            # A tiny tolerance covers float32 UV storage at the triangle edge.
            inside = (first >= -1e-4) & (second >= -1e-4) & (first+second <= 1.0001)
            coverage[lo[1]:hi[1]+1, lo[0]:hi[0]+1] |= inside
        assert not np.any(mask & ~coverage), 'Declared color mask escapes the inner source faces'
        allowed = dict(row, sourceTriangleIds=set(ids))
        checked[row['modifiedAlbedoSha256']] = allowed
        summaries.append({k: row[k] for k in ('sourceAlbedoSha256', 'modifiedAlbedoSha256',
                          'width', 'height', 'changedPixels', 'permittedPixels', 'uvMaskSha256',
                          'maxChannelChange', 'seamDistance', 'armSelection')})
        summaries[-1]['sourceTriangles'] = len(ids)
        summaries[-1]['outsideMaskMaxError'] = float(delta[~mask].max(initial=0))
    return checked, summaries


def attribute(mesh, name, vector=False):
    layer = mesh.attributes[name]
    return np.array([v.vector[:] if vector else v.value for v in layer.data])


def verify(obj, source, rest, source_rest, contract, family):
    mesh, original = obj.data, source.data
    uv = np.array([v.uv[:] for v in mesh.uv_layers.active.data])
    source_uv = np.array([v.uv[:] for v in original.uv_layers.active.data])
    if not contract or not contract['geometryChanged']:
        assert rest.shape == source_rest.shape and uv.shape == source_uv.shape
        error, uv_error = float(abs(rest-source_rest).max()), float(abs(uv-source_uv).max())
        assert error < 2e-6 and uv_error < 1e-7
        return error, uv_error, None
    original.calc_loop_triangles()
    triangles = np.array([t.vertices[:] for t in original.loop_triangles])
    source_loops = np.array([t.loops[:] for t in original.loop_triangles])
    source_id = attribute(mesh, 'rig_source_vertex').astype(int)
    source_face = attribute(mesh, 'rig_source_triangle').astype(int)
    edge_a = attribute(mesh, 'rig_source_edge_a').astype(int)
    edge_b = attribute(mesh, 'rig_source_edge_b').astype(int)
    edge_t = attribute(mesh, 'rig_source_edge_t')
    offset = attribute(mesh, 'rig_contact_offset', True)
    n = len(source_rest)
    assert np.array_equal(source_id[:n], np.arange(n)) and np.all(source_id[n:] == -1)
    assert n == contract['sourceVertices'] and len(triangles) == contract['sourceTriangles']
    moved = np.flatnonzero(np.linalg.norm(offset[:n], axis=1) > 1e-8)
    assert moved.tolist() == contract['movedSourceVertexIds']
    assert contract['untouchedSourceVertices'] == n-len(moved)
    head = source.vertex_groups.get('Head')
    if head is not None:
        protected = {v.index for v in original.vertices for g in v.groups if g.group == head.index and g.weight > .999}
        assert not protected.intersection(moved.tolist()), 'Head vertices moved by contact repair'
    assert contract['sourceDisplacementLimit'] == .035
    assert np.linalg.norm(offset, axis=1).max() <= .0350001
    canonical_offset = canonical(offset, family)
    assert abs(canonical_offset[:, 1:]).max() < 1e-8
    assert np.allclose(canonical_offset[moved, 0], contract['movedSourceSideOffsets'], atol=1e-8, rtol=0)
    assert abs(np.linalg.norm(offset[:n], axis=1).max()-contract['maxSourceDisplacement']) < 1e-8
    error = float(abs(rest[:n]-source_rest-offset[:n]).max())
    raw = rest-offset
    seam = edge_a >= 0
    assert seam.sum() == contract['addedSeamVertices']
    expected = source_rest[edge_a[seam]]*(1-edge_t[seam,None])+source_rest[edge_b[seam]]*edge_t[seam,None]
    seam_error = float(abs(raw[seam]-expected).max(initial=0))
    error = max(error, seam_error)
    assert error < 2e-6, ('source correspondence', error)
    assert np.all((source_face >= -1) & (source_face < len(triangles)))
    exterior = source_face >= 0
    assert (~exterior).sum() == contract['interiorTriangles']
    assert exterior.sum() == contract['retainedExteriorTriangles']
    def albedo(material):
        bs = next(n for n in material.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
        image = next(link.from_node.image for link in bs.inputs['Base Color'].links if link.from_node.type == 'TEX_IMAGE')
        data = bytes(image.packed_file.data) if image.packed_file else Path(image.filepath).read_bytes()
        return hashlib.sha256(data).hexdigest()
    old_colors = [albedo(m) for m in original.materials]
    colors = [albedo(m) for m in mesh.materials]
    assert contract['interiorAlbedo']['sha256'] in colors
    source_materials = np.array([t.material_index for t in original.loop_triangles])
    retouches, retouch_summary = verify_albedo_retouch(mesh, original, contract, family,
        obj.get('part_key'), source_rest, rest, triangles, source_loops, source_uv, seam)
    uv_error = 0.
    # Every exterior corner must retain its source triangle's barycentric UV,
    # including clipped seam vertices and vertices with recorded local offsets.
    for polygon in mesh.polygons:
        si = source_face[polygon.index]
        if si < 0:
            continue
        color = colors[polygon.material_index]
        if color != old_colors[source_materials[si]]:
            allowed = retouches.get(color)
            assert allowed is not None and si in allowed['sourceTriangleIds'], 'Exterior albedo changed'
            assert allowed['sourceMaterial'] == source_materials[si]
        tri = triangles[si]
        bary = np.zeros((len(polygon.vertices), 3))
        for j, vi in enumerate(polygon.vertices):
            if source_id[vi] >= 0:
                positions = np.flatnonzero(tri == source_id[vi])
                assert len(positions) == 1
                bary[j, positions[0]] = 1
            else:
                ia, ib = np.flatnonzero(tri == edge_a[vi]), np.flatnonzero(tri == edge_b[vi])
                assert len(ia) == 1 and len(ib) == 1
                bary[j, ia[0]], bary[j, ib[0]] = 1-edge_t[vi], edge_t[vi]
        expected_uv = bary @ source_uv[source_loops[si]]
        uv_error = max(uv_error, float(abs(expected_uv-uv[list(polygon.loop_indices)]).max()))
    assert uv_error < 1e-7, ('retained source UV', uv_error)
    polygons = np.array([p.vertices[:] for p in mesh.polygons])
    source_area = .5*np.linalg.norm(np.cross(source_rest[triangles[:,1]]-source_rest[triangles[:,0]], source_rest[triangles[:,2]]-source_rest[triangles[:,0]]), axis=1)
    retained_faces = polygons[exterior]
    retained_area = .5*np.linalg.norm(np.cross(raw[retained_faces[:,1]]-raw[retained_faces[:,0]], raw[retained_faces[:,2]]-raw[retained_faces[:,0]]), axis=1)
    retained_by_source = np.bincount(source_face[exterior], weights=retained_area, minlength=len(triangles))
    removed_area = np.maximum(source_area-retained_by_source, 0)
    removed_fraction = float(removed_area.sum()/source_area.sum())
    assert removed_fraction <= .05, ('Contact repair removed too much source surface', removed_fraction)
    assert abs(removed_fraction-contract['sourceAreaRemovedFraction']) < 1e-5
    assert abs(float(removed_area.sum())-contract['sourceAreaRemoved']) < 2e-6
    declared_removed = np.zeros(len(triangles), bool)
    declared_removed[contract['removedOrClippedSourceTriangleIds']] = True
    assert removed_area[~declared_removed].max(initial=0) < 2e-9
    changed = np.unique(np.r_[moved, np.unique(polygons[~exterior]), np.arange(n, len(rest))])
    contact = canonical(rest[changed], family)
    bounds = np.stack([contact.min(0), contact.max(0)])
    assert np.allclose(bounds, contract['contactBoundsCanonical'], atol=2e-6, rtol=0)
    # Drop's jacket has a lower inner cuff than the three shared humanoid bodies.
    # This exception covers that garment only; moved source vertices remain above .17.
    contact_floor = {('drop', 'Jacket'): .14, ('bloom', 'Coat'): .15}.get((family, obj.get('part_key')), .16)
    assert contact[:,2].min() > contact_floor and contact[:,2].max() < .405
    assert abs(contact[:,0]).max() < .31 and abs(contact[:,1]).max() < .20
    source_contact = canonical(source_rest[moved], family)
    assert source_contact[:,2].min(initial=1) > .17 and source_contact[:,2].max(initial=0) < .39
    return error, uv_error, {'method': contract['method'], 'sourceVertices': n,
        'untouchedSourceVertices': n-len(moved), 'movedSourceVertices': len(moved),
        'maxSourceDisplacement': float(np.linalg.norm(offset[:n], axis=1).max()),
        'addedSeamVertices': int(seam.sum()), 'interiorTriangles': int((~exterior).sum()),
        'sourceCorrespondenceMaxError': error, 'retainedUvMaxError': uv_error,
        'sourceAreaRemovedFraction': removed_fraction,
        'contactBoundsCanonical': bounds.tolist(), 'sourceAlbedoRetouch': retouch_summary}

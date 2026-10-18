"""Reviewed cuff vertex selections for two malformed source weight assignments.

These are authored selections on the accepted source meshes, not a runtime color
classifier. Geometry, UV and albedo fingerprints reject a different delivery.
"""
import hashlib
import json
from pathlib import Path

import numpy as np

MANIFEST = Path(__file__).with_name('cuff_ownership.json')


def mesh_fingerprint(mesh):
    mesh.calc_loop_triangles()
    arrays = [np.array([v.co[:] for v in mesh.vertices], dtype='<f4'),
              np.array([t.vertices[:] for t in mesh.loop_triangles], dtype='<i4'),
              np.array([t.loops[:] for t in mesh.loop_triangles], dtype='<i4'),
              np.array([v.uv[:] for v in mesh.uv_layers.active.data], dtype='<f4')]
    digest = hashlib.sha256()
    for array in arrays:
        digest.update(json.dumps(array.shape).encode())
        digest.update(array.tobytes())
    return digest.hexdigest()


def albedo_image(material):
    shader = next(n for n in material.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
    return next(link.from_node.image for link in shader.inputs['Base Color'].links
                if link.from_node.type == 'TEX_IMAGE')


def image_hash(image):
    data = bytes(image.packed_file.data) if image.packed_file else Path(image.filepath).read_bytes()
    return hashlib.sha256(data).hexdigest()


def profile(obj, family, part):
    row = json.loads(MANIFEST.read_text())['assets'].get(family + '/' + part)
    if row is None:
        return None
    assert len(obj.data.vertices) == row['sourceVertices']
    assert mesh_fingerprint(obj.data) == row['sourceMeshSha256'], (family, part, 'Cuff selection source changed')
    assert [image_hash(albedo_image(m)) for m in obj.data.materials] == row['sourceAlbedoSha256']
    ids = row['vertexIds']
    assert ids == sorted(set(ids)) and min(ids) >= 0 and max(ids) < len(obj.data.vertices)
    return row

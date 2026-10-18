"""Remove rest-pose occlusion only from the exposed inner contact seam.

Original exterior materials remain assigned everywhere else. A separate image
copy and material cover the recorded inner faces; their UVs do not change.
"""
import base64
import hashlib
import zlib

import bpy
import numpy as np

from cuff_ownership import albedo_image, image_hash
from skinning import smooth


def image_pixels(image):
    width, height = image.size[:]
    pixels = np.empty(width * height * 4, np.float32)
    image.pixels.foreach_get(pixels)
    return pixels.reshape(height, width, 4)


def repair_lining(obj, data, family, part):
    assert (family, part) in {('sprig', 'Jacket'), ('bloom', 'Coat')}
    points, faces, source_faces = data['points'], data['faces'], data['source_faces']
    tags = data['tags']
    normals = data['normals'].reshape(-1, 3, 3)
    uv = data['uv'].reshape(-1, 3, 2)
    color = np.median([chart['centercolor'] for chart, cap in zip(data['charts'], data['caps'])
                       if cap['side'] == 0], axis=0)
    # The interior is the garment's main fabric. A colored cuff must not tint
    # the whole lining, and its baked rest-pose occlusion must not be extended.
    for chart in data['charts']:
        chart['centercolor'] = color.copy()
        chart['colors'] = np.tile(color, (len(chart['colors']), 1))

    distance = {}
    for side in (0, 1):
        ids = np.unique([i for cap in data['caps'] if cap['side'] == side for i in cap['boundary']])
        boundary = points[ids]
        d = np.empty(len(points))
        for start in range(0, len(points), 256):
            d[start:start+256] = np.sqrt(((points[start:start+256, None]-boundary[None])**2).sum(2).min(1))
        distance[side] = d

    eligible = []
    for fi, ids in enumerate(faces):
        if source_faces[fi] < 0:
            continue
        side = tags[ids[0]]
        center = points[ids].mean(0)
        d = distance[side][ids]
        if d.min() >= .022 or not .16 < center[2] < .37:
            continue
        if side == 1:
            # Sprig's inside normals are usable. Bloom's folded input has
            # unreliable normals, so protect its outer cuff by position instead.
            if family == 'sprig' and normals[fi, :, 0].mean()*np.sign(center[0]) > -.10:
                continue
            if family == 'bloom' and abs(center[0]) > .235:
                continue
        elif abs(center[0]) < .12 or abs(center[1]-.03) > .105:
            continue
        eligible.append((fi, 1-smooth(0, .022, d)))

    records = []
    for mi in np.unique(data['material_ids'][[fi for fi, weights in eligible]]):
        source_material = obj.data.materials[int(mi)]
        original = albedo_image(source_material)
        pixels = image_pixels(original)
        height, width = pixels.shape[:2]
        mask = np.zeros((height, width), np.float32)
        affected = []
        output_faces = []
        for fi, weights in eligible:
            if data['material_ids'][fi] != mi:
                continue
            xy = uv[fi]*[width, height]-.5
            low = np.maximum(np.floor(xy.min(0)).astype(int), 0)
            high = np.minimum(np.ceil(xy.max(0)).astype(int), [width-1, height-1])
            gy, gx = np.mgrid[low[1]:high[1]+1, low[0]:high[0]+1]
            q = np.stack([gx, gy], -1)
            a, b, c = xy
            matrix = np.column_stack([b-a, c-a])
            if abs(np.linalg.det(matrix)) < 1e-10:
                continue
            local = (q-a) @ np.linalg.inv(matrix).T
            bary = np.stack([1-local.sum(-1), local[..., 0], local[..., 1]], -1)
            values = np.where(np.all(bary >= -1e-7, axis=-1), np.clip(bary @ weights, 0, 1), 0)
            region = mask[low[1]:high[1]+1, low[0]:high[0]+1]
            np.maximum(region, values, out=region)
            affected.append(int(source_faces[fi]))
            output_faces.append(fi)
        edited = pixels.copy()
        edited[:, :, :3] = pixels[:, :, :3]*(1-mask[:, :, None])+color*mask[:, :, None]
        name = family+'_'+part+'_InnerContactClean_'+str(mi)
        image = bpy.data.images.new(name+'.png', width=width, height=height, alpha=True)
        image.colorspace_settings.name = 'sRGB'
        image.pixels.foreach_set(edited.ravel())
        image.pack()
        material = source_material.copy()
        material.name = name
        shader = next(n for n in material.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
        texture = next(link.from_node for link in shader.inputs['Base Color'].links
                       if link.from_node.type == 'TEX_IMAGE')
        texture.image = image
        obj.data.materials.append(material)
        data['material_ids'][output_faces] = len(obj.data.materials)-1
        actual = image_pixels(image)
        changed = np.any(abs(actual-pixels) > 1e-6, axis=2)
        permitted = mask > 0
        assert not np.any(changed & ~permitted), 'Image packing changed an unedited pixel'
        packed_mask = np.packbits(permitted.ravel()).tobytes()
        records.append({'sourceMaterial': int(mi), 'sourceAlbedoSha256': image_hash(original),
                        'modifiedAlbedoSha256': image_hash(image), 'width': width, 'height': height,
                        'sourceTriangleIds': sorted(set(affected)),
                        'uvMaskEncoding': 'zlib-base64-packed-bits-big-endian',
                        'uvMask': base64.b64encode(zlib.compress(packed_mask, 9)).decode(),
                        'uvMaskSha256': hashlib.sha256(packed_mask).hexdigest(),
                        'permittedPixels': int(permitted.sum()), 'changedPixels': int(changed.sum()),
                        'maxChannelChange': float(abs(actual-pixels).max()),
                        'seamDistance': .022, 'mainFabricColor': color.tolist(),
                        'armSelection': 'inwardNormal' if family == 'sprig' else 'innerSide'})
    return records

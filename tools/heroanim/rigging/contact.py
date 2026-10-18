"""Local ownership and contact repair for the accepted meshes.

A garment's indexed face connectivity takes precedence over proximity: coincident
vertices can belong to a separate sleeve. Only genuinely connected arm/torso
contacts require cutting and a small new interior surface. Exterior UVs remain
on their source triangles; all local position changes are recorded explicitly.
"""
import numpy as np
from skinning import canonical, smooth


def connected_components(vertex_count, triangles):
    parent = np.arange(vertex_count)
    def find(i):
        while parent[i] != i:
            parent[i] = parent[parent[i]]
            i = int(parent[i])
        return i
    for a, b in np.concatenate([triangles[:, [0, 1]], triangles[:, [1, 2]]]):
        a, b = find(int(a)), find(int(b))
        if a != b:
            parent[a] = b
    roots = np.array([find(i) for i in range(vertex_count)])
    return np.unique(roots, return_inverse=True)[1]


def separate_sleeves(points, triangles, original, family):
    """Recognize existing independent sleeves without welding coincident surfaces."""
    labels = connected_components(len(points), triangles)
    if labels.max() == 0:
        return None
    p = canonical(points, family)
    arm = sum((w for n, w in original.items() if n.startswith(('UpperArm.', 'Forearm.', 'Hand.'))), np.zeros(len(p)))
    ownership = np.zeros(len(p))
    sleeves = []
    for label in np.unique(labels):
        ids = np.flatnonzero(labels == label)
        q = p[ids]
        if arm[ids].mean() > .25 and (np.all(q[:, 0] > 0) or np.all(q[:, 0] < 0)):
            ownership[ids] = 1
            sleeves.append({'component': int(label), 'side': 'L' if q[:, 0].mean() > 0 else 'R',
                            'vertices': int(len(ids)), 'sourceArmMean': float(arm[ids].mean())})
    if {r['side'] for r in sleeves} != {'L', 'R'} or np.max(arm[ownership == 0], initial=0) > 1e-5:
        return None
    return ownership, {'method': 'separateSleeveComponents', 'sourceComponents': int(labels.max()+1), 'sleeves': sleeves}


def mesh_smooth(points, triangles, source, family, strength=4):
    _, inverse = np.unique(np.round(points, 6), axis=0, return_inverse=True)
    count = int(inverse.max()) + 1
    multiplicity = np.bincount(inverse)
    p = np.column_stack([np.bincount(inverse, weights=points[:, k])/multiplicity for k in range(3)])
    edges = np.unique(np.sort(inverse[np.concatenate([triangles[:, [0, 1]], triangles[:, [1, 2]], triangles[:, [2, 0]]])], axis=1), axis=0)
    edges = edges[edges[:, 0] != edges[:, 1]]
    edge_weight = 1/np.maximum(np.linalg.norm(p[edges[:, 0]]-p[edges[:, 1]], axis=1), .0001)
    ii, jj = np.r_[edges[:, 0], edges[:, 1]], np.r_[edges[:, 1], edges[:, 0]]
    ww = np.r_[edge_weight, edge_weight]
    degree = np.bincount(ii, weights=ww, minlength=count)
    target = np.bincount(inverse, weights=source)/multiplicity
    fixed = abs(canonical(p, family)[:, 0]) < (.11 if family == 'drop' else .15)
    value = target.copy(); value[fixed] = 0
    for _ in range(180):
        neighbor = np.bincount(ii, weights=ww*value[jj], minlength=count)/np.maximum(degree, 1e-12)
        updated = (target + strength*neighbor)/(1+strength)
        updated[fixed] = 0
        if np.max(abs(updated-value)) < 1e-10:
            value = updated
            break
        value = updated
    return smooth(.10, .85, value)[inverse]


def ownership_field(points, triangles, original, family, part):
    source = sum((w for n, w in original.items() if n.startswith(('UpperArm.', 'Forearm.', 'Hand.'))), np.zeros(len(points)))
    p = canonical(points, family)
    face_normals = np.cross(points[triangles[:, 1]]-points[triangles[:, 0]], points[triangles[:, 2]]-points[triangles[:, 0]])
    normals = np.zeros_like(points)
    for k in range(3):
        np.add.at(normals, triangles[:, k], face_normals)
    normals /= np.maximum(np.linalg.norm(normals, axis=1)[:, None], 1e-10)
    normals = canonical(normals, family)
    start = np.array([.085, .035, .32]) if family == 'drop' else np.array([.1752, .03, .3245])
    end = np.array([.263, .035, .16]) if family == 'drop' else np.array([.2806, .03, .143])
    axis = end-start
    q = p.copy(); q[:, 0] = abs(q[:, 0]); normals[:, 0] *= np.where(p[:, 0] < 0, -1, 1)
    along = (q-start) @ axis / (axis @ axis)
    radial = q-start-along[:, None]*axis
    radial /= np.maximum(np.linalg.norm(radial, axis=1)[:, None], 1e-10)
    score = np.sum(radial*normals, axis=1)
    if family == 'drop' and part == 'Overalls':
        source = smooth(.13, .205, abs(p[:, 0])) * smooth(.16, .20, p[:, 2])
    contact = (1-smooth(.15 if family == 'drop' else .19, .18 if family == 'drop' else .22, abs(p[:, 0])))
    contact *= (1-smooth(.29, .335, p[:, 2]))*smooth(.005, .03, (start+along[:, None]*axis)[:, 0]-abs(p[:, 0]))
    source *= 1-contact+contact*smooth(-.05, .45, score)
    result = mesh_smooth(points, triangles, source, family)
    if part == 'base':
        result[(original.get('Head', np.zeros(len(p))) > .999) | (p[:, 2] > .40)] = 0
    return result


def quadratic(points, triangles, normals, field, center, side, loops=None):
    """Fit the adjacent torso side in the depth/height contact chart."""
    sign = np.sign(center[0]); p = points.copy(); p[:, 0] *= sign
    if loops is None:
        face = np.cross(points[triangles[:, 1]]-points[triangles[:, 0]], points[triangles[:, 2]]-points[triangles[:, 0]])
        n = np.zeros_like(points)
        for k in range(3): np.add.at(n, triangles[:, k], -face)
    else:
        n = np.zeros_like(points)
        np.add.at(n, triangles.ravel(), normals[loops].reshape(-1, 3))
    n /= np.maximum(np.linalg.norm(n, axis=1)[:, None], 1e-10); n[:, 0] *= sign
    delta = points-center
    near = (np.linalg.norm(delta, axis=1) < .14) & (p[:, 0] > .05) & (points[:, 2] > .17) & (points[:, 2] < .38) & (abs(points[:, 1]-.03) < .145)
    keep = near & (field < .10) & (n[:, 0] > .25)
    if keep.sum() < 20: keep = near & (field < .20) & (n[:, 0] > .05)
    if keep.sum() < 6:
        return lambda q: (np.array(q), np.broadcast_to([sign, 0., 0.], np.shape(q))), int(keep.sum())
    yz = (points[keep, 1:3]-center[1:3])/.1
    matrix = np.c_[np.ones(len(yz)), yz[:, 0], yz[:, 1], yz[:, 0]**2, yz[:, 0]*yz[:, 1], yz[:, 1]**2]
    weight = 1/(np.linalg.norm(delta[keep], axis=1)+.02)**2
    coef = np.linalg.lstsq(matrix*weight[:, None], p[keep, 0]*weight, rcond=None)[0]
    def project(q):
        q = np.array(q); d = (q[..., 1:3]-center[1:3])/.1; y, z = d[..., 0], d[..., 1]
        x = coef[0]+coef[1]*y+coef[2]*z+coef[3]*y*y+coef[4]*y*z+coef[5]*z*z
        out = q.copy(); out[..., 0] = sign*x
        normal = np.stack([np.ones_like(x)*sign, -(coef[1]+2*coef[3]*y+coef[4]*z)/.1, -(coef[2]+coef[4]*y+2*coef[5]*z)/.1], axis=-1)
        normal /= np.maximum(np.linalg.norm(normal, axis=-1)[..., None], 1e-10)
        return out, normal
    return project, int(keep.sum())

def torso_side(points, field, center):
    """Estimate the body side from unambiguous front/back torso and lower flank."""
    sign = np.sign(center[0])
    p = points.copy(); p[:, 0] *= sign
    depth = p[:, 1]-.03
    keep = (p[:, 0] > .04) & (p[:, 2] > .16) & (p[:, 2] < .38) & (field < .01)
    keep &= (abs(depth) > .075) | (p[:, 2] < .205)
    yz = (p[keep, 1:3]-[.03, .25])/.1
    matrix = np.c_[np.ones(len(yz)), yz[:, 0], yz[:, 1], yz[:, 0]**2, yz[:, 0]*yz[:, 1], yz[:, 1]**2]
    coefficients = np.linalg.lstsq(matrix, p[keep, 0]**2, rcond=None)[0]
    def project(q):
        q = np.asarray(q); y, z = np.moveaxis((q[..., 1:3]-[.03, .25])/.1, -1, 0)
        c = coefficients
        x = np.sqrt(np.maximum(c[0]+c[1]*y+c[2]*z+c[3]*y*y+c[4]*y*z+c[5]*z*z, .0001))
        result = q.copy(); result[..., 0] = x*sign
        normal = np.stack([np.ones_like(x)*sign, -(c[1]+2*c[3]*y+c[4]*z)/(.2*x), -(c[2]+c[4]*y+2*c[5]*z)/(.2*x)], axis=-1)
        normal /= np.maximum(np.linalg.norm(normal, axis=-1)[..., None], 1e-10)
        return result, normal
    return project, int(keep.sum())

def open_contact_seams(points, triangles, loops, uv, normals, material_ids, ownership):
    """Find the original open sleeve/root contours by matching adjacent pieces."""
    coordinates = [tuple(v) for v in np.round(points, 6)]
    edges = {}
    for ti, (ids, ls) in enumerate(zip(triangles, loops)):
        for j in range(3):
            a, b = int(ids[j]), int(ids[(j+1)%3])
            side = int(ownership[a] > .5)
            key = (side, tuple(sorted((coordinates[a], coordinates[b]))))
            edges.setdefault(key, []).append((a, b, uv[ls[j]], uv[ls[(j+1)%3]], int(material_ids[ti]), side, normals[ls[j]], normals[ls[(j+1)%3]]))
    boundaries = [rows[0] for rows in edges.values() if len(rows) == 1]
    distance = np.full(len(points), np.inf)
    for side in (0, 1):
        source_ids = np.unique([v for row in boundaries if row[5] == side for v in row[:2]])
        target_ids = np.unique([v for row in boundaries if row[5] != side for v in row[:2]])
        target = points[target_ids]
        for start in range(0, len(source_ids), 128):
            ids = source_ids[start:start+128]
            distance[ids] = np.sqrt(((points[ids,None]-target[None])**2).sum(2).min(1))
    matched = [max(distance[row[0]], distance[row[1]]) < .003 for row in boundaries]
    # The shirt can have a longer open armhole than the separate short sleeve.
    # Close the complete existing torso contour, including its lower exposed arc.
    parent = {}
    def find(key):
        parent.setdefault(key, key)
        while parent[key] != key:
            parent[key] = parent[parent[key]]; key = parent[key]
        return key
    for row in boundaries:
        a, b = (row[5], coordinates[row[0]]), (row[5], coordinates[row[1]])
        parent[find(a)] = find(b)
    groups = {}
    for i, row in enumerate(boundaries):
        groups.setdefault(find((row[5], coordinates[row[0]])), []).append(i)
    extend = set()
    for ids in groups.values():
        if boundaries[ids[0]][5] == 0 and sum(matched[i] for i in ids) >= 10:
            extend.update(ids)
    return [row for i, row in enumerate(boundaries) if matched[i] or i in extend]

def split_contacts(points,triangles,loops,uv,normals,material_ids,field,contact_factor=None, sample=None, protected=None, body=False, existing_seams=None):
    if protected is None: protected=np.zeros(len(points),dtype=bool)
    if contact_factor is None:contact_factor=np.ones(len(points))
    vertices=points.tolist();values=field.tolist();tags=(field>.5).astype(int).tolist()
    faces=[];tex=[];norm=[];mats=[];edgecache={};seams=list(existing_seams or []);mapping=[];source_faces=[]
    signed_fields=[field-(.5-.45*contact_factor),field-(.5+.22*contact_factor)]
    for ti,(ids,ls,mi) in enumerate(zip(triangles,loops,material_ids)):
        pieces=[]
        for side in (0,1):
            signed=signed_fields[side]
            inside=signed>=0 if side else signed<=0
            if not inside[ids].any():continue
            poly=[]
            for i in range(3):
                j=(i+1)%3;vi,vj=int(ids[i]),int(ids[j]);si,sj=inside[vi],inside[vj]
                if si:poly.append((vi,uv[ls[i]],normals[ls[i]],False))
                if si!=sj:
                    t=-signed[vi]/(signed[vj]-signed[vi]);key=(min(vi,vj),max(vi,vj),side)
                    if key not in edgecache:
                        edgecache[key]=len(vertices);vertices.append(((1-t)*points[vi]+t*points[vj]).tolist());values.append((1-t)*field[vi]+t*field[vj]);tags.append(side);mapping.append((len(vertices)-1,vi,vj,t,side))
                    poly.append((edgecache[key],(1-t)*uv[ls[i]]+t*uv[ls[j]],(1-t)*normals[ls[i]]+t*normals[ls[j]],True))
            for i,a in enumerate(poly):
                b=poly[(i+1)%len(poly)]
                if a[3] and b[3]:seams.append((a[0],b[0],a[1],b[1],int(mi),side,a[2],b[2]))
            pieces.append(poly)
        for poly in pieces:
            for k in range(1,len(poly)-1):
                row=[poly[0],poly[k],poly[k+1]];faces.append([e[0]for e in row]);tex.extend([e[1]for e in row]);norm.extend([e[2]for e in row]);mats.append(mi);source_faces.append(ti)
    # Each seam side is closed by a local interior surface. Original exterior
    # UVs remain on their source triangles; local flank offsets are recorded.
    coord=lambda i:tuple(np.round(vertices[i],6))
    parents={}
    def find(k):
        parents.setdefault(k,k)
        while parents[k]!=k:parents[k]=parents[parents[k]];k=parents[k]
        return k
    def union(a,b):
        aa,bb=find(a),find(b)
        if aa!=bb:parents[aa]=bb
    for a,b,ua,ub,mi,side,na,nb in seams:union((side,coord(a)),(side,coord(b)))
    groups={}
    for segment in seams:groups.setdefault(find((segment[5],coord(segment[0]))),[]).append(segment)
    caps=[];cap_colors=[];cap_face_ids=[];charts=[];overrides={};displacements={}
    source_centers=points[triangles].mean(1)
    source_normals=normals[loops].mean(1)
    source_ownership=field[triangles].mean(1)
    for group in groups.values():
        # Some accepted garments have open cloth boundaries. Close the contact
        # chart's open chain explicitly; otherwise a fan leaves a missing wedge.
        starts={coord(seg[0]):(seg[0],seg[2],seg[6],seg[4])for seg in group}
        ends={coord(seg[1]):(seg[1],seg[3],seg[7],seg[4])for seg in group}
        start_only=[starts[k]for k in starts.keys()-ends.keys()];end_only=[ends[k]for k in ends.keys()-starts.keys()]
        while start_only and end_only:
            b,ub,nb,mi=end_only.pop();j=min(range(len(start_only)),key=lambda j:np.linalg.norm(np.array(vertices[b])-vertices[start_only[j][0]]));a,ua,na,_=start_only.pop(j)
            group.append((b,a,ub,ua,mi,group[0][5],nb,na))
        unique={coord(i):i for seg in group for i in seg[:2]}
        if len(unique)<3:continue
        source_ids=list(unique.values());bp=np.array(vertices)[source_ids];center=bp.mean(0);side=group[0][5]
        outward=np.array([np.sign(center[0])*(1 if side==0 else -1),0,0.])
        # Round only the newly exposed contact surface. Its rim stays on the source cloth.
        bulge=min(.018 if side==0 else .024,float(np.ptp(bp[:,1]))*.15,float(np.ptp(bp[:,2]))*.15)
        project=None
        if side==0 and len(source_ids)>20:
            project,_=torso_side(points,field,center) if body else quadratic(points,triangles,normals,field,center,side,loops)
            current=np.array(vertices);lo=bp.min(0)-.045;hi=bp.max(0)+.045
            candidates=np.flatnonzero((np.array(tags)==0)&(current[:,2]>.17)&(current[:,2]<.39)&np.all((current>=lo)&(current<=hi),axis=1))
            candidates=np.array([i for i in candidates if i>=len(points) or not protected[i]],dtype=int)
            if len(candidates):
                dist=np.sqrt(((current[candidates,None]-bp[None])**2).sum(2).min(1));blend=np.clip(1-dist/.035,0,1);blend=blend*blend*(3-2*blend)
                fitted,nf=project(current[candidates]);change=np.clip(fitted[:,0]-current[candidates,0],-.035,.035)*blend
                # Only remove the fused contact ridge; never inflate an accepted torso panel.
                change=change if body else np.where(change*np.sign(center[0])<0,change,0)
                for idx,dx,weight,normal in zip(candidates,change,blend,nf):
                    if abs(dx)>1e-8:
                        dx=float(np.clip(dx,-.035-displacements.get(int(idx),0.),.035-displacements.get(int(idx),0.)));vertices[idx][0]+=dx;displacements[int(idx)]=displacements.get(int(idx),0.)+float(dx);overrides[int(idx)]=(float(weight),normal)
            bp=np.array(vertices)[source_ids];center=bp.mean(0)
            fitted,nf=project(center);fitted[0]=np.clip(fitted[0],center[0]-.035,center[0]+.035);center=fitted;outward=nf;bulge=0
        else:center+=outward*bulge
        ci=len(vertices);vertices.append(center.tolist());values.append(.5);tags.append(side)
        caps.append({'vertex':ci,'boundary':source_ids,'side':side,'faces':len(group)})
        samples=np.array([sample(seg[2],seg[4])for seg in group])if sample else np.ones((len(group),3))*.5
        # Existing albedo remains untouched; the interior takes the dominant adjacent cloth color.
        colors=samples.copy();centers=np.array([colors[np.argmin(colors.mean(1))],colors[np.argmax(colors.mean(1))]])
        for _ in range(12):
            labels=((colors[:,None]-centers[None])**2).sum(2).argmin(1)
            for k in range(2):
                if np.any(labels==k):centers[k]=np.median(colors[labels==k],0)
        dominant=colors[labels==np.bincount(labels,minlength=2).argmax()];cc=np.quantile(dominant,.65,axis=0)
        # Contact UVs contain baked occlusion from the rest pose. New exposed
        # lining takes its interior color from nearby visible exterior fabric;
        # the original rim texels remain at the boundary of the new chart.
        if sample:
            delta=source_centers-center
            distance=np.linalg.norm(delta,axis=1)
            visible=(distance>.04)&(distance<.12)&(abs(delta[:,2])<.055)&(abs(delta[:,1])>.025)
            visible &= source_ownership<.05 if side==0 else source_ownership>.95
            if side==0:visible &= source_normals[:,0]*np.sign(center[0])>.2
            ids=np.flatnonzero(visible)
            if len(ids)>10:
                exterior_colors=np.array([sample(uv[loops[i]].mean(0),material_ids[i]) for i in ids])
                brightness=exterior_colors.mean(1)
                keep=brightness>=np.quantile(brightness,.35)
                cc=np.median(exterior_colors[keep],axis=0)

        chart={'faces':[],'points':np.array([vertices[seg[0]]for seg in group]),'colors':samples,'centercolor':cc};charts.append(chart)
        rings={};vertexnorm={};vertexcolor={ci:cc};vertexnorm[ci]=outward
        for a,b,ua,ub,mi,sd,na,nb in group:
            for idx,uv0,n0 in [(a,ua,na),(b,ub,nb)]:
                k=coord(idx)
                if k in rings:continue
                color=sample(uv0,mi)if sample else cc;vertexcolor[idx]=color;vertexnorm[idx]=n0
                ring=[]
                # Two interior rings allow a smooth rounded socket instead of a flat fan.
                for r in (.67,.33):
                    pos=(1-r)*(center-outward*bulge)+r*np.array(vertices[idx])+outward*bulge*(1-r*r)
                    if project:
                        fitted,nf=project(pos);pos[0]=r*r*pos[0]+(1-r*r)*np.clip(fitted[0],pos[0]-.035,pos[0]+.035)
                    vi=len(vertices);vertices.append(pos.tolist());values.append(.5);tags.append(side);ring.append(vi)
                    vertexnorm[vi]=project(pos)[1] if project else r*n0+(1-r)*outward;vertexcolor[vi]=r*color+(1-r)*cc
                rings[k]=ring
        def add(row,mi):
            # Coincident UV seam endpoints can share an interior ring. Do not
            # emit repeated-index faces which Blender's exporter later removes.
            if len(set(row)) < 3:
                return
            cap_face_ids.append(len(faces));chart['faces'].append(len(faces));faces.append(row);tex.extend([np.zeros(2)]*3);mats.append(mi);source_faces.append(-1)
            ns=np.array([vertexnorm[v]for v in row]);ns/=np.maximum(np.linalg.norm(ns,axis=1)[:,None],1e-12);norm.extend(ns);cap_colors.append([vertexcolor[v]for v in row])
        for a,b,ua,ub,mi,sd,na,nb in group:
            # UV duplicates may share a geometric ring while retaining their exterior loops.
            for idx,u0,n0 in [(a,ua,na),(b,ub,nb)]:
                vertexnorm[idx]=n0;vertexcolor[idx]=sample(u0,mi)if sample else cc
            ar,br=rings[coord(a)],rings[coord(b)]
            add([b,a,ar[0]],mi);add([b,ar[0],br[0]],mi)
            add([br[0],ar[0],ar[1]],mi);add([br[0],ar[1],br[1]],mi)
            add([br[1],ar[1],ci],mi)
    faces=np.array(faces);norm=np.array(norm)
    for li,vi in enumerate(faces.ravel()):
        if vi in overrides:
            weight,nn=overrides[vi];norm[li]=(1-weight)*norm[li]+weight*nn;norm[li]/=max(np.linalg.norm(norm[li]),1e-10)
    return dict(points=np.array(vertices),field=np.array(values),tags=np.array(tags),faces=np.array(faces),uv=np.array(tex),normals=np.array(norm),material_ids=np.array(mats),mapping=mapping,caps=caps,source_faces=np.array(source_faces),cap_colors=np.array(cap_colors),cap_face_ids=np.array(cap_face_ids),charts=charts,displacements=displacements)

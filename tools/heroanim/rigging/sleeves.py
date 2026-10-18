"""Clean overlapping sleeve tubes for generated garments with unusable fused armpits.

Each cross-section has one axial skin coordinate. Existing torso UVs stay untouched;
new sleeve fabric uses the garment's sampled color and a separate rounded cuff.
"""
import bpy
import numpy as np
from mathutils import Vector
from skinning import canonical, world_vector


def append_sleeves(obj,family,coords,mapping,zones,faces,uv_faces,material_ids,smooth_faces):
    rig=obj.parent
    parameters=[-1.0]*len(coords)
    cuff_faces=[]
    sleeve_faces=[]
    short=obj['part_key']=='Overalls'
    body_objects=[o for o in bpy.context.scene.objects if o.type=='MESH' and
                  (o.get('part_key')=='base' or o.get('part_key','').startswith('Base_Arm_'))]
    for suffix,sign,zone in [('L',1,6),('R',-1,7)]:
        start,elbow,wrist=[canonical(np.array([rig.matrix_world@rig.data.bones[name+'.'+suffix].head_local]),family)[0]
                           for name in ['UpperArm','Forearm','Hand']]
        axis=wrist-start
        length=np.linalg.norm(axis)
        direction=axis/length
        front=np.array([0.,1.,0.]);front-=direction*np.dot(direction,front);front/=np.linalg.norm(front)
        cross=np.cross(direction,front);cross/=np.linalg.norm(cross)
        points=[]
        for body in body_objects:
            if body.get('part_key','').startswith('Base_Arm_') and not body['part_key'].endswith('_'+suffix):
                continue
            groups={g.index:g.name for g in body.vertex_groups}
            for vertex in body.data.vertices:
                weights={groups[g.group]:g.weight for g in vertex.groups}
                arm=sum(w for n,w in weights.items() if n.endswith('.'+suffix) and n.startswith(('UpperArm','Forearm','Hand','ShoulderBlend','ElbowBlend')))
                if arm>.3 and weights.get('Hand.'+suffix,0)<.45:
                    points.append(tuple(body.matrix_world@vertex.co))
        cloud=canonical(np.array(points),family)
        projection=(cloud-start)@axis/np.dot(axis,axis)
        rows,segments=12,24
        end=.42 if short else .80
        values=np.linspace(.02,end,rows+1)
        first=len(coords)
        for inner in (False,True):
            for row,t in enumerate(values):
                center=start+axis*t
                nearby=cloud[np.abs(projection-t)<.085]
                if len(nearby)>8:
                    delta=nearby-center
                    rd=float(np.quantile(np.abs(delta@front),.97))+.008
                    rc=float(np.quantile(np.abs(delta@cross),.97))+.008
                else:
                    rd,rc=.065,.050
                rd=np.clip(rd,.038,.115);rc=np.clip(rc,.038,.078)
                if row>=rows-1 and not short:
                    rd=min(rd,.063);rc=min(rc,.059)
                puff=1+.035*np.sin(np.pi*t/end*4)**2 if obj['part_key']=='Jacket' else 1
                if inner:
                    rd-=.004;rc-=.004
                for segment in range(segments):
                    theta=2*np.pi*segment/segments
                    point=center+puff*(front*np.cos(theta)*rd+cross*np.sin(theta)*rc)
                    coords.append(tuple(obj.matrix_world.inverted()@Vector(world_vector(point,family))))
                    mapping.append(-1);zones.append(zone);parameters.append(float(t))
        layer=(rows+1)*segments
        def face(ids,cuff=False):
            index=len(faces)
            faces.append(tuple(ids));uv_faces.append([(0,0)]*len(ids));material_ids.append(0);smooth_faces.append(True)
            sleeve_faces.append(index)
            if cuff:
                cuff_faces.append(index)
        for inner in (False,True):
            offset=first+(layer if inner else 0)
            for row in range(rows):
                for segment in range(segments):
                    a=offset+row*segments+segment;b=offset+row*segments+(segment+1)%segments
                    c=b+segments;d=a+segments
                    face((a,b,c,d) if inner else (a,d,c,b),not short and row>=rows-2)
        for segment in range(segments):
            a=first+segment;b=first+(segment+1)%segments
            face((a,b,b+layer,a+layer))
            a+=rows*segments;b+=rows*segments
            face((a,a+layer,b+layer,b),not short)
    return parameters,sleeve_faces,cuff_faces

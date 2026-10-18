"""Interpolate shorts skin weights on actual leg triangles, rather than nearest vertices."""
import numpy as np
from mathutils.bvhtree import BVHTree
from skinning import smooth


class LegSurface:
    def __init__(self, parts):
        self.names = ['Hips','Spine','Chest'] + [name+'.'+side for side in ('L','R') for name in ('Thigh','Shin','Foot')]
        vertices, rows, faces = [], [], []
        for key,obj in parts.items():
            if key != 'base' and not key.startswith('Base_Leg_'):
                continue
            names = {g.index:g.name for g in obj.vertex_groups}
            weights=np.zeros((len(obj.data.vertices),len(self.names)))
            for v in obj.data.vertices:
                for g in v.groups:
                    if names[g.group] in self.names:
                        weights[v.index,self.names.index(names[g.group])]=g.weight
            offset=len(vertices)
            vertices.extend(tuple(obj.matrix_world@v.co) for v in obj.data.vertices)
            rows.extend(weights)
            leg=weights[:,3:].sum(axis=1)
            obj.data.calc_loop_triangles()
            for tri in obj.data.loop_triangles:
                ids=list(tri.vertices)
                if leg[ids].mean()>.2 and max(vertices[offset+i][2] for i in ids)<.18:
                    faces.append(tuple(offset+i for i in ids))
        assert faces,'No fitted leg surface'
        self.vertices=np.array(vertices)
        self.weights=np.array(rows)
        self.faces=np.array(faces)
        self.tree=BVHTree.FromPolygons(vertices,faces,all_triangles=True)

    def transfer(self,obj,original):
        result={n:a.copy() for n,a in original.items()}
        for n in self.names[3:]:
            result.setdefault(n,np.zeros(len(obj.data.vertices)))
        touched=0
        for vertex in obj.data.vertices:
            point=obj.matrix_world@vertex.co
            if point.z>=.16:
                continue
            nearest,_,index,distance=self.tree.find_nearest(point)
            assert nearest is not None
            ids=self.faces[index]
            a,b,c=self.vertices[ids]
            u,v=b-a,c-a
            q=np.array(nearest)-a
            d00,d01,d11=np.dot(u,u),np.dot(u,v),np.dot(v,v)
            denominator=d00*d11-d01*d01
            if denominator<1e-15:
                continue
            j=(d11*np.dot(q,u)-d01*np.dot(q,v))/denominator
            k=(d00*np.dot(q,v)-d01*np.dot(q,u))/denominator
            bary=np.clip([1-j-k,j,k],0,1)
            bary/=bary.sum()
            target=bary@self.weights[ids]
            amount=float(1-smooth(.125,.16,point.z))
            for column,n in enumerate(self.names[3:],start=3):
                result[n][vertex.index]=(1-amount)*result[n][vertex.index]+amount*target[column]
            touched+=1
        return result,touched

import bpy,json,numpy as np
from pathlib import Path
from mathutils import Matrix
def features(objects):
 points=np.array([tuple(v.co) for o in objects for v in o.data.vertices]);lo=points.min(0);hi=points.max(0);size=hi-lo;mid=(lo+hi)/2
 rows=[]
 for o in objects:
  uv=o.data.uv_layers.active
  if not uv:continue
  images={}
  for i,m in enumerate(o.data.materials):
   bs=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');link=bs.inputs['Base Color'].links
   if not link or link[0].from_node.type!='TEX_IMAGE':continue
   im=link[0].from_node.image;px=np.empty(len(im.pixels),dtype=np.float32);im.pixels.foreach_get(px);images[i]=(px.reshape(im.size[1],im.size[0],4),im.size[:])
  for poly in o.data.polygons:
   p=poly.center
   if p.x<mid[0]+size[0]*.1:continue
   f=(p.z-lo[2])/size[2]
   if not .1<f<.7:continue
   if poly.material_index not in images:continue
   im,(w,h)=images[poly.material_index];us=np.mean([tuple(uv.data[i].uv) for i in poly.loop_indices],axis=0)
   c=im[int(np.clip(us[1]*h,0,h-1)),int(np.clip(us[0]*w,0,w-1)),:3];r,g,b=c
   dark=max(c)<.32 or (r>.08 and r>g*1.2 and r>b*1.08 and g<.45)
   if dark:rows.append((p.x,p.y,p.z,max(poly.area,1e-10),f))
 if not rows:raise ValueError('No face features')
 rows=np.array(rows)
 def avg(mask):
  x=rows[mask];assert len(x)>5,('Missing landmarks',lo,hi,len(x));return np.average(x[:,:3],axis=0,weights=x[:,3]).tolist()
 left=avg((rows[:,1]>mid[1]+size[1]*.07)&(rows[:,1]<mid[1]+size[1]*.4)&(rows[:,4]>.24))
 right=avg((rows[:,1]<mid[1]-size[1]*.07)&(rows[:,1]>mid[1]-size[1]*.4)&(rows[:,4]>.24))
 mouth=avg((abs(rows[:,1]-mid[1])<size[1]*.09)&(rows[:,4]<.4))
 return dict(left=left,right=right,mouth=mouth,bounds=[lo.tolist(),hi.tolist()])

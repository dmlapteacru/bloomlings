# Read a sheet of cell pictures back into cells (step 1 of 2), here the owner's example of 2026-10-26 (levels 4–10).
# python3 extract.py <sheet.png> <cells.json>. P: each panel's crop box (x0, x1, y0, y1); MASK: boxes to blank (titles,
# badges). Cell seams are the light lines between cells; each cell's median colour is sampled, near-white is empty.
import numpy as np, json, sys
from PIL import Image
im=np.asarray(Image.open(sys.argv[1]).convert('RGB')).astype(float)
P={4:(30,370,160,540),5:(430,730,150,540),6:(780,1135,170,525),7:(1170,1505,160,545),8:(25,500,628,962),9:(530,1000,612,958),10:(1045,1490,605,975)}
MASK={5:[(645,760,130,160)],9:[(530,700,595,642)],10:[(1400,1510,600,636),(1030,1240,595,642)]}
def seams(br,col,axis):
    # seam score: lighter than the pixels 2 away on both sides, only on colourful pixels
    if axis==1: a=br[:,2:-2]-(br[:,:-4]+br[:,4:])/2; m=col[:,2:-2]; s=(np.clip(a,0,None)*m).sum(0)/(m.sum(0)+1)
    else: a=br[2:-2]-(br[:-4]+br[4:])/2; m=col[2:-2]; s=(np.clip(a,0,None)*m).sum(1)/(m.sum(1)+1)
    s=np.convolve(s,[.25,.5,.25],'same'); peaks=[]
    for i in np.argsort(-s):
        if s[i]<s.max()*.18: break
        if all(abs(i-j)>=7 for j in peaks): peaks.append(i)
    return sorted(p+2 for p in peaks)
def fill(ps,lo,hi,pitch):
    # fill gaps (outside the colour area or missed seams) with the typical pitch
    ps=sorted(ps);d=np.median(np.diff(ps));out=[ps[0]]
    for p in ps[1:]:
        g=p-out[-1];n=max(1,round(g/d))
        out+= [out[-1]+g*i/n for i in range(1,n+1)]
    while out[0]-d>lo: out.insert(0,out[0]-d)
    while out[-1]+d<hi: out.append(out[-1]+d)
    return out,d
res={}
for k,(x0,x1,y0,y1) in P.items():
    c=im[y0:y1,x0:x1].copy()
    for (a,b,cc,dd) in MASK.get(k,[]): c[max(0,cc-y0):max(0,dd-y0),max(0,a-x0):max(0,b-x0)]=255
    br=c.sum(2);sat=c.max(2)-c.min(2);col=((sat>45)|(br<450)).astype(float)
    xs,dx=fill(seams(br,col,1),0,x1-x0,0);ys,dy=fill(seams(br,col,0),0,y1-y0,0)
    rows=[]
    for j in range(len(ys)-1):
        r=[]
        for i in range(len(xs)-1):
            a,b=int(xs[i]+2),int(xs[i+1]-1);cc,dd=int(ys[j]+2),int(ys[j+1]-1)
            if b<=a or dd<=cc: r.append(None);continue
            px=c[cc:dd,a:b].reshape(-1,3);med=np.median(px,0)
            s=med.max()-med.min()
            r.append(None if (s<30 and med.sum()>660) else [int(v) for v in med])
        rows.append(r)
    res[k]={'dx':dx,'dy':dy,'rows':rows}
    print(k,'pitch',round(dx,2),round(dy,2),'grid',len(xs)-1,'x',len(ys)-1,'cells',sum(v is not None for r in rows for v in r))
json.dump(res,open(sys.argv[2],'w'))

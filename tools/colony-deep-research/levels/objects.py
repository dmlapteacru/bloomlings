"""Visual detectors on board previews: grey '?' mystery tiles and big outlined numbers (doors/chocolate/counters).
usage: python3 -I objects.py <scripts_dir> <boards_dir> <out_csv> [levels]"""
import sys; sys.path.insert(0, sys.argv[1])
import board as b, numpy as np, glob, os, csv
from PIL import Image
from collections import deque
def components(mask):
    H,W=mask.shape; seen=np.zeros_like(mask,bool); out=[]
    ys,xs=np.nonzero(mask)
    for y0,x0 in zip(ys,xs):
        if seen[y0,x0]: continue
        q=deque([(y0,x0)]); seen[y0,x0]=True; pts=[]
        while q:
            y,x=q.popleft(); pts.append((y,x))
            for dy,dx in ((1,0),(-1,0),(0,1),(0,-1)):
                yy,xx=y+dy,x+dx
                if 0<=yy<H and 0<=xx<W and mask[yy,xx] and not seen[yy,xx]: seen[yy,xx]=True; q.append((yy,xx))
        out.append(np.array(pts))
    return out
def analyse(path):
    r=b.analyse(path); m,xs,ys,px,py,i0,i1,j0,j1,keep=b.GEOM
    a=np.asarray(Image.open(path).convert('RGB')).astype(float); H0,W0,_=a.shape
    # mystery tiles
    myst=0
    for c in keep:
        x0=int(m+xs[c[0]]); y0=int(m+ys[c[1]]); cell=a[y0:y0+int(py),x0:x0+int(px)]
        if cell.size==0: continue
        l=b.lab(c[2])
        if abs(l[1])<7 and abs(l[2])<7 and 35<l[0]<72:
            wh=(cell.min(2)>225).mean()
            if wh>0.04: myst+=1
    # big outlined numbers: white blobs whose surrounding ring is mostly very dark
    sub=a; white=(sub.min(2)>195)&((sub.max(2)-sub.min(2))<22); dark=sub.max(2)<60
    nums=0; maxh=0
    for comp in components(white):
        if len(comp)<20 or len(comp)>20000: continue
        y0,x0=comp.min(0); y1,x1=comp.max(0); hgt=y1-y0+1; wid=x1-x0+1
        if hgt<0.6*py or hgt>4.5*py or wid>2.5*hgt: continue
        fill=len(comp)/(hgt*wid)
        yy0,yy1=max(0,y0-4),min(sub.shape[0],y1+5); xx0,xx1=max(0,x0-4),min(sub.shape[1],x1+5)
        box=np.zeros((yy1-yy0,xx1-xx0),bool); box[comp[:,0]-yy0,comp[:,1]-xx0]=True
        dil=box.copy()
        for _ in range(3):
            d2=dil.copy(); d2[1:]|=dil[:-1]; d2[:-1]|=dil[1:]; d2[:,1:]|=dil[:,:-1]; d2[:,:-1]|=dil[:,1:]; dil=d2
        ring=dil&~box
        frac=dark[yy0:yy1,xx0:xx1][ring].mean() if ring.any() else 0
        if frac>0.45 and fill<0.75: nums+=1; maxh=max(maxh,hgt/py)
    return dict(level=int(os.path.basename(path)[:-5]),mysteryCells=myst,numberGlyphs=nums,maxGlyphHeightCells=round(maxh,2))
if __name__=='__main__':
    d,out=sys.argv[2],sys.argv[3]
    lv=[int(x) for x in sys.argv[4:]] or sorted(int(os.path.basename(f)[:-5]) for f in glob.glob(d+'/*.webp'))
    rows=[analyse(f'{d}/{n}.webp') for n in lv]
    if len(lv)<30:
        for r in rows: print(r)
    with open(out,'w',newline='') as fh:
        w=csv.DictWriter(fh,list(rows[0].keys())); w.writeheader(); [w.writerow(x) for x in rows]

"""Count Source-tray columns in first walkthrough frames using the fixed, centred column layout."""
from PIL import Image; import numpy as np, sys, glob, os, csv, collections
d,out=sys.argv[1],sys.argv[2]
rows=[]
for f in sorted(glob.glob(d+'/*.webp'),key=lambda p:int(os.path.basename(p)[:-5])):
    n=int(os.path.basename(f)[:-5])
    a=np.asarray(Image.open(f).convert('RGB')).astype(float); H,W,_=a.shape
    asp=H/W
    if 1.7<asp<1.9: y1=0.772; pitch=0.144
    elif 1.45<asp<1.6: y1=0.805; pitch=0.141
    else: rows.append(dict(level=n,format='other',trayCols='')); continue
    # background: peach colour sampled between the slots row and tray at the far left/right margins is unreliable (decor), use row mode
    best=None; scores={}
    for yy in (y1-0.012,y1,y1+0.012):
        y=int(yy*H); row=a[y-2:y+3].mean(0)
        q=(row//10).astype(int); k=q[:,0]*10000+q[:,1]*100+q[:,2]; vals,cnt=np.unique(k,return_counts=True); kk=vals[np.argmax(cnt)]
        bg=np.array([kk//10000,(kk//100)%100,kk%100])*10+5
        pe=row[(row[:,0]>200)&(row[:,1]>150)&(row[:,1]<225)&(row[:,2]>85)&(row[:,2]<185)&(row[:,0]-row[:,2]>50)]
        if len(pe)>0.08*W: bg=np.median(pe,0)
        def ispod(xc,hw=0.012):
            x0,x1=int((xc-hw)*W),int((xc+hw)*W)
            if x0<0 or x1>W: return False
            p=a[y-3:y+4,x0:x1].reshape(-1,3)
            return np.median(np.abs(p-bg).max(1))>28
        for c in range(1,7):
            xs=[0.5+(i-(c-1)/2)*pitch for i in range(c)]
            if all(ispod(x) for x in xs):
                outside=[0.5-((c+1)/2)*pitch,0.5+((c+1)/2)*pitch]
                gaps=[(xs[i]+xs[i+1])/2 for i in range(c-1)]
                gapbg=sum(not ispod(g,0.005) for g in gaps)
                if not any(ispod(x) for x in outside if 0.04<x<0.96) and (c==1 or gapbg>=0.5*len(gaps)):
                    scores[c]=scores.get(c,0)+1
    tc=max(scores,key=lambda c:(scores[c],c)) if scores else ''
    rows.append(dict(level=n,format='%.2f'%asp,trayCols=tc))
with open(out,'w',newline='') as fh:
    w=csv.DictWriter(fh,['level','format','trayCols']); w.writeheader(); [w.writerow(x) for x in rows]
print(collections.Counter(r['trayCols'] for r in rows))

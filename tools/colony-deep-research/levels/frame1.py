"""Classify first walkthrough frames: UI accent colour (pause button / bottom bar) and tray column count."""
from PIL import Image; import numpy as np, sys, glob, os, csv, colorsys
d,out=sys.argv[1],sys.argv[2]
rows=[]
def hue_name(rgb):
    r,g,b=[x/255 for x in rgb]; h,s,v=colorsys.rgb_to_hsv(r,g,b); h*=360
    if s<0.25: return 'grey'
    if h<20 or h>=335: return 'red'
    if h<50: return 'orange'
    if h<75: return 'yellow'
    if h<165: return 'green'
    if h<200: return 'cyan'
    if h<255: return 'blue'
    if h<300: return 'purple'
    return 'magenta'
for f in sorted(glob.glob(d+'/*.webp'),key=lambda p:int(os.path.basename(p)[:-5])):
    n=int(os.path.basename(f)[:-5])
    a=np.asarray(Image.open(f).convert('RGB')).astype(float); H,W,_=a.shape
    asp=H/W
    r=dict(level=n,w=W,h=H,aspect=round(asp,3))
    if not (1.7<asp<1.9): r['note']='cropped/zoomed'; rows.append(r); continue
    # pause button: top-left box; take strongly saturated pixels
    reg=a[int(0.01*H):int(0.06*H),int(0.01*W):int(0.11*W)].reshape(-1,3)
    mx=reg.max(1); mn=reg.min(1); sat=(mx-mn)/np.maximum(mx,1)
    sel=reg[(sat>0.45)&(mx>90)]
    r['pauseRGB']='/'.join(str(int(x)) for x in np.median(sel,0)) if len(sel)>30 else ''
    r['pause']=hue_name(np.median(sel,0)) if len(sel)>30 else 'none'
    reg2=a[int(0.975*H):H,int(0.0*W):int(0.04*W)].reshape(-1,3)
    r['barRGB']='/'.join(str(int(x)) for x in np.median(reg2,0)); r['bar']=hue_name(np.median(reg2,0))
    rows.append(r)
keys=['level','w','h','aspect','pause','pauseRGB','bar','barRGB','note']
with open(out,'w',newline='') as fh:
    w=csv.DictWriter(fh,keys); w.writeheader(); [w.writerow(x) for x in rows]
import collections
print(collections.Counter((x.get('pause'),x.get('bar')) for x in rows))

import sys; sys.path.insert(0, sys.argv[1])
import board as b
from PIL import Image, ImageDraw
d,out=sys.argv[2],sys.argv[3]
for n in map(int,sys.argv[4:]):
    r=b.analyse(f'{d}/{n}.webp'); m,xs,ys,px,py,i0,i1,j0,j1,keep=b.GEOM
    im=Image.open(f'{d}/{n}.webp').convert('RGB'); dr=ImageDraw.Draw(im)
    for c in keep:
        x=m+xs[c[0]]+px/2; y=m+ys[c[1]]+py*0.42
        dr.ellipse([x-2,y-2,x+2,y+2],fill=(255,0,255))
    x0=m+xs[i0]; x1=m+xs[i1]+px; y0=m+ys[j0]; y1=m+ys[j1]+py
    dr.rectangle([x0,y0,x1,y1],outline=(0,255,0),width=2)
    dr.text((10,10),f"L{n} {r['gridW']}x{r['gridH']} col={r['colors']}",fill=(0,0,0))
    im.save(f'{out}/ov{n}.png')
    print(n, r['gridW'], r['gridH'], r['colors'], r['fill'])

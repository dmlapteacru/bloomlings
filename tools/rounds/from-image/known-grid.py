# Read a sheet of cell pictures whose captions give each matrix size (the owner's sheet of levels 4–8, 2026-10-26):
# the cell pitch is the colour bounds over the stated columns and rows, each cell its median colour, near-white empty.
# python3 known-grid.py <sheet.png> <cells.json>; P: crop box, columns, rows, stated cells per panel.
import numpy as np, json, sys
from PIL import Image
im=np.asarray(Image.open(sys.argv[1]).convert('RGB')).astype(float)
# panel crop and the caption's matrix size (columns, rows) and cell count
P={4:([95,325,180,430],24,28,562),5:([450,790,145,460],38,38,553),6:([55,365,625,830],35,22,554),7:([485,745,630,825],29,21,554),8:([90,325,1045,1265],25,24,551)}
res={}
for k,((x0,x1,y0,y1),W,H,N) in P.items():
    c=im[y0:y1,x0:x1];sat=c.max(2)-c.min(2);br=c.sum(2);col=(sat>40)|(br<500)
    ys,xs=np.where(col);bx0,bx1,by0,by1=xs.min(),xs.max()+1,ys.min(),ys.max()+1
    best=None
    # pitch from the bounds, assuming the matrix is the tight box of its colour cells
    pw=(bx1-bx0)/W;ph=(by1-by0)/H
    rows=[]
    for j in range(H):
        r=[]
        for i in range(W):
            cx=bx0+(i+.5)*pw;cy=by0+(j+.5)*ph;a,b=int(cx-pw*.25),int(cx+pw*.25)+1;cc,dd=int(cy-ph*.25),int(cy+ph*.25)+1
            px=c[cc:dd,a:b].reshape(-1,3);med=np.median(px,0);s=med.max()-med.min()
            r.append(None if (s<30 and med.sum()>660) else [int(v) for v in med])
        rows.append(r)
    n=sum(v is not None for r in rows for v in r)
    res[k]={'rows':rows};print(k,'pitch %.2f×%.2f'%(pw,ph),'cells',n,'caption',N)
json.dump(res,open(sys.argv[2],'w'))

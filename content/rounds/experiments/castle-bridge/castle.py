# Castle beyond a moat, after v8E L22's bridge recipe: the picture on the far bank, under it two crossed strips
# (left Q, right P) split by a divider, the moat row with two bridge sites, and on the near bank two beds (left P,
# right Q) with a dark path between them; each site's anchor stands in its bed.
import math,json,sys
W=30
P,Q='O','M'          # beds: pumpkins left, moss right; far strips crossed
pic_h=21
g=[['S']*W for _ in range(pic_h)]
def put(x,y,c):
    if 0<=x<W and 0<=y<pic_h: g[y][x]=c
def box(x0,y0,x1,y1,c):
    for y in range(y0,y1+1):
        for x in range(x0,x1+1): put(x,y,c)
# sun
for y in range(0,6):
    for x in range(11,19):
        if (x-14.5)**2+(y-2.5)**2<=5.2: put(x,y,'Y')
# hill
for y in range(12,pic_h):
    for x in range(W):
        if ((x-14.5)/17.5)**2+((y-25)/12.5)**2<=1: put(x,y,'G')
# moss texture on the hill (inner colour)
for y in range(15,pic_h):
    for x in range(W):
        pass
# winding path up the hill to the gate
for y in range(16,pic_h):
    cx=14.5+math.sin(y*.9)*1.6
    for x in range(W):
        if abs(x-cx)<=1.0 and g[y][x]=='G': put(x,y,'E')
# towers
def tower(x0,x1,ytop,ybot):
    box(x0,ytop,x1,ybot,'C')
    for y in range(ytop,ybot+1): put(x0,y,'D'); put(x1,y,'D')
    for x in range(x0,x1+1): put(x,ybot,'D')
    # cone roof
    cx=(x0+x1)/2; h=5
    for y in range(ytop-h,ytop):
        half=(y-(ytop-h)+1)*((x1-x0)/2+1)/h
        for x in range(x0-1,x1+2):
            if abs(x-cx)<=half: put(x,y,'R')
    put(int(round(cx)),ytop-h-1,'D'); put(int(round(cx))+1,ytop-h-1,'R')  # flag pole + flag
tower(3,8,7,17); tower(21,26,7,17)
# keep with battlements
box(9,9,20,17,'C')
for x in range(9,21): put(x,17,'D')
for x in range(9,21):
    put(x,8,'C' if x%2==1 else 'S'); put(x,9,'D')
for y in range(9,18): put(9,y,'D'); put(20,y,'D')
# windows (lit, inside the walls)
for (x,y) in [(5,10),(23,10),(5,13),(23,13),(11,11),(17,11)]:
    box(x,y,x+1,y+1,'Y')
# gate arch with a dark door inside
for y in range(12,17):
    for x in range(13,17):
        if y>12 or x in (14,15): put(x,y,'E')
for y in range(14,17):
    for x in range(14,16): put(x,y,'D')
rows=[''.join(r) for r in g]
mid=W//2
far=[''.join(('#' if x in (mid-1,mid,0,W-1) else (Q if x<mid-1 else P)) for x in range(W)) for _ in range(int(__import__('os').environ.get('FAR','6')))]
xl,xr=round(W*.25),round(W*.75)
moat=''.join('b' if x in (xl,xr) else '#' for x in range(W))
near=[''.join(('D' if x in (mid-1,mid) else (P if x<mid-1 else Q)) for x in range(W)) for _ in range(int(__import__('os').environ.get('NEAR','3')))]
allr=rows+far+[moat]+near
json.dump({'rows':allr},sys.stdout)

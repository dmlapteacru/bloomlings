# L65 «Снежный шар с ёлкой» (Ярмарки и праздники): a snow globe on a wooden stand: inside a decorated fir in the snow under
# a night sky with falling snow. The globe's glass is the contour (17 §8 «Контур вместо прямой полосы»: an outline from
# the picture's own meaning); both bridges cross up into the globe from its stand, over a gold band (A) on the left and
# the stand's wood (E) on the right; round the globe the lavender wall (H).
import math,json,sys
sys.path.insert(0,'.')
from contour import finish
W,H=32,35
g=[['H']*W for _ in range(H)]
far=[[False]*W for _ in range(H)]
def fill(pred,c,mask=None):
    for y in range(H):
        for x in range(W):
            if pred(x+.5,y+.5) and (mask is None or mask[y][x]): g[y][x]=c
CX,CY,R=16,15.5,14.2
sphere=lambda x,y:(x-CX)**2+(y-CY)**2<=R*R
# the stand: a trapezoid under the globe, the gold band across its top
stand=lambda x,y:y>=27.5 and abs(x-16)<=9+(y-27.5)*.9
fill(stand,'E')
fill(lambda x,y:stand(x,y) and y<=29.6,'A')
for y in range(H):
    for x in range(W):
        if sphere(x+.5,y+.5): far[y][x]=True
fill(lambda x,y:True,'N',far)                                       # night inside the glass
fill(lambda x,y:y>=23.3+1.2*math.sin(x*.45),'W',far)                # snow ground
tree=lambda x,y:4.9<=y<=22.4 and abs(x-16)<=.8+((y-4.9)%5.9)*1.15+((y-4.9)//5.9)*2.4
fill(tree,'M',far)
fill(lambda x,y:tree(x,y) and ((x+y*1.7)%5)<1.3,'G',far)
fill(lambda x,y:abs(x-16)<=1.2 and 21.9<=y<=24.3,'E',far)            # trunk
for (cx,cy,c) in [(15,8.9,'R'),(17.5,10.4,'Y'),(13,13.4,'Y'),(18,14.9,'R'),(20.5,13.4,'R'),(11,16.9,'R'),(16,17.4,'Y'),(21.5,19.4,'R'),(9.5,20.5,'Y'),(13.5,20.7,'R'),(18.8,21.3,'Y')]:
    fill(lambda x,y,cx=cx,cy=cy:(x-cx)**2+(y-cy)**2<=1.1 and tree(x,y),c,far)
fill(lambda x,y:abs(x-16)+abs(y-4.1)<=1.8,'Y',far)                  # the star
for (x,y) in [(6,9),(9,5),(24,6),(26,11),(5,15),(27,17),(21,3),(12,3),(4,20),(28,22),(23,9),(7,12)]:
    if far[y][x] and g[y][x]=='N': g[y][x]='W'                      # falling snow
g.insert(0,['H']*W);far.insert(0,[False]*W);H+=1     # a row of wall above the glass, so the contour closes round its top
rows,anchors=finish(g,far,[('bottom',10),('bottom',22)],fill='N')
rows=[list(r) for r in rows]
(ax1,ay1),(ax2,ay2)=anchors
for y in range(H):
    for x in range(W):
        if rows[y][x] in 'HAE' and y>=ay1 and abs(x-ax1)<=1.3: rows[y][x]='A'     # the gold step to the left gate
        if rows[y][x] in 'HAE' and y>=ay2 and abs(x-ax2)<=1.3: rows[y][x]='E'     # the wooden step to the right gate
json.dump({'rows':[''.join(r) for r in rows]},sys.stdout)

# L75 «Замок на острове» (Фольклор и легенды; the owner's amendment of 2026-11-03: Super Hard bridge + links + three
# latches): a fairy-tale castle on an island, its moat drawn as the contour (17 §8 «Контур вместо прямой полосы»), two
# bridges on two sides (Super Hard, 17 §8: sites on different strips): up from the gravel road (X) onto the gate, and from
# the right meadow onto the orchard. Three empty spots for the keys (17 §8 «Засов»: a key lies in an empty cell enclosed by
# work): the gate's archway and the courtyard well inside the walls (behind the bridges), and a fairy ring of mushrooms in
# the near meadow.
import math,json,sys
sys.path.insert(0,'.')
from contour import finish
W,H=36,37
OY=3   # everything drawn 3 rows lower than its coordinates, so the keep's flag fits
g=[['S']*W for _ in range(H)]
far=[[False]*W for _ in range(H)]
def fill(pred,c,mask=None):
    for y in range(H):
        for x in range(W):
            if pred(x+.5,y+.5-OY) and (mask is None or mask[y][x]): g[y][x]=c
def put(x,y,c):
    y+=OY
    if 0<=x<W and 0<=y<H: g[y][x]=c
# the sky with two clouds, the hills and the meadow
fill(lambda x,y:((x-6.5)/3.6)**2+((y+0.6)/1.1)**2<=1 or ((x-9.5)/2.4)**2+((y+1.3)/1.0)**2<=1,'W')
fill(lambda x,y:((x-29)/3.4)**2+((y+0.4)/1.0)**2<=1,'W')
fill(lambda x,y:y>=19+1.0*math.sin(x*.33),'G')                       # the meadow
# the island: an oval of moss under the castle (the far region)
island=lambda x,y:((x-17.5)/13.6)**2+((y-19.6)/8.6)**2<=1
# the castle: a curtain wall, two side towers, the keep with its tall tower
wall=lambda x,y:7.0<=x<=28.0 and 15.0<=y<=23.4
ltower=lambda x,y:4.6<=x<=9.4 and 9.6<=y<=23.4
rtower=lambda x,y:25.6<=x<=30.4 and 9.6<=y<=23.4
keep=lambda x,y:13.4<=x<=21.6 and 6.6<=y<=15.2
spire=lambda x,y:15.6<=x<=19.4 and 2.6<=y<=7.0
def cone(cx,base,half,top):
    return lambda x,y:top<=y<=base and abs(x-cx)<=half*(y-top)/(base-top)
lroof,rroof,kroof=cone(7.0,9.8,3.2,3.6),cone(28.0,9.8,3.2,3.6),cone(17.5,2.8,2.6,-1.0)
castle=lambda x,y:wall(x,y) or ltower(x,y) or rtower(x,y) or keep(x,y) or spire(x,y)
roofs=lambda x,y:lroof(x,y) or rroof(x,y) or kroof(x,y)
for y in range(H):
    for x in range(W):
        X,Y=x+.5,y+.5-OY
        if island(X,Y) or castle(X,Y) or roofs(X,Y): far[y][x]=True
fill(lambda x,y:True,'M',far)                                         # the island's moss
fill(lambda x,y:castle(x,y),'X',far)                                  # grey stone
fill(lambda x,y:(wall(x,y) and y<=16.0 and int(x)%2==0) or ((ltower(x,y) or rtower(x,y)) and y<=10.6 and int(x)%2==1),'C',far)  # pale battlements
fill(lambda x,y:roofs(x,y),'R',far)                                   # red cone roofs
fill(lambda x,y:castle(x,y) and (int(y)%3==0) and int(x+int(y)//3)%4==0,'C',far)   # a few pale stones
# windows (dark), the gate (brown door frame)
for (wx,wy) in [(6,12),(7,12),(28,12),(29,12),(6,17),(7,17),(28,17),(29,17),(15,9),(16,9),(19,9),(20,9),(17,4),(18,4),(17,5),(18,5)]:
    put(wx,wy,'D')
fill(lambda x,y:14.5<=x<=20.5 and 18.0<=y<=23.4 and ((x-17.5)**2/9+(y-20.6)**2/7.5<=1),'D',far)   # the gate's dark arch
# the orchard on the island's right: two round trees with apples
for cx,cy in [(27.6,24.4),(23.0,25.6)]:
    fill(lambda x,y,cx=cx,cy=cy:((x-cx)/2.1)**2+((y-cy)/1.7)**2<=1,'L',far)
    put(int(cx),int(cy),'R')
rows,anchors=finish(g,far,[('bottom',17),('right',22+OY)],fill=None)
rows=[list(r) for r in rows]
(ax1,ay1),(ax2,ay2)=anchors
for y in range(H):
    for x in range(W):
        if rows[y][x]=='G' and y>=ay1 and abs(x-ax1)<=1.3+(y-ay1)*.35: rows[y][x]='X'      # the gravel road to the gate
# the key spots: the gate's archway (inside the wooden frame), the courtyard well (a ring of pale stone round it) and a
# fairy ring of mushrooms in the near meadow on the left
for (x,y) in [(17,21),(18,21),(17,22),(18,22)]:
    rows[y+OY][x]='.'
for (x,y) in [(11,19),(10,18),(12,18),(10,20),(12,20),(11,17),(11,21),(9,19),(13,19)]:
    if rows[y+OY][x] in 'XCM': rows[y+OY][x]='C'
rows[19+OY][11]='.'
for dx,dy in [(-1,-1),(0,-1),(1,-1),(-1,0),(1,0),(-1,1),(0,1),(1,1)]:
    rows[29+OY+dy][5+dx]='R' if (dx+dy)%2 else 'W'
rows[29+OY][5]='.'
# two more fairy rings in the near meadow (right of the road, and low on the left), each round an empty spot
for (cx,cy) in [(30,31+OY),(9,32+OY)]:
    for dx,dy in [(-1,-1),(0,-1),(1,-1),(-1,0),(1,0),(-1,1),(0,1),(1,1)]:
        rows[cy+dy][cx+dx]='R' if (dx+dy)%2 else 'W'
    rows[cy][cx]='.'
json.dump({'rows':[''.join(r) for r in rows]},sys.stdout)

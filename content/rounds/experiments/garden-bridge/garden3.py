# Japanese garden beyond a pond, drawn as one scene (the owner, 2026-10-30: «часть за мостом … частью общей картины … одно
# большое целое»): the pocket recipe stays (left moss, right azaleas behind the pond, crossed with the near beds; the pockets
# closed at the picture's edges and split by a divider, open only upward), but the parts are drawn as the garden: the
# divider is a stream running from the foot of the mountain into the pond, the moss is a rounded mound under the sakura,
# the azaleas a row of bushes before the pagoda, the pond widens between the two bridges, and moss and azaleas grow in the
# upper picture too.
import math,json,sys
W,H=32,32
g=[['S']*W for _ in range(H)]
def put(x,y,c):
    if 0<=x<W and 0<=y<H: g[y][x]=c
def fill(pred,c,y0=0,y1=H):
    for y in range(y0,y1):
        for x in range(W):
            if pred(x+.5,y+.5): put(x,y,c)
# sun, Mount Fuji with a snow cap
fill(lambda x,y:(x-15.5)**2+(y-3.2)**2<=7,'Y')
fill(lambda x,y:y>=4 and abs(x-11)<=(y-3)*1.15 and y<=18,'N')
fill(lambda x,y:y>=4 and abs(x-11)<=(y-3)*1.15 and y<=7.2+0.8*math.sin(x*1.9),'C')
# ground from y 16
fill(lambda x,y:y>=16.5+0.6*math.sin(x*0.45),'G',0,22)
# pagoda (three tiers)
def roof(xc,half,y0):
    for x in range(W):
        d=abs(x+.5-xc)
        if d<=half+1 and d>half-.5: put(x,y0-1,'D')
        if d<=half: put(x,y0,'D')
        if d<=half-1: put(x,y0-1,'D')
def body(xc,half,y0,y1):
    for y in range(y0,y1+1):
        for x in range(W):
            if abs(x+.5-xc)<=half: put(x,y,'R')
    for dx in (-1.5,1.5): put(int(xc+dx),y0,'Y')
xc=24.5
for y in range(0,3): put(24,y,'Y')
roof(xc,3.5,4); body(xc,2.5,5,6)
roof(xc,5,8); body(xc,3.5,9,10)
roof(xc,6.5,12); body(xc,4.5,13,16)
for y in range(14,17):
    for x in (24,25): put(x,y,'D')
for x in (21,28): put(x,14,'Y')
# sakura on the left
for (cx,cy,r) in [(4,9.5,3.2),(7.5,8.5,3.4),(2.5,12.5,2.6),(6,12,3)]:
    fill(lambda x,y,cx=cx,cy=cy,r=r:(x-cx)**2+(y-cy)**2<=r*r,'P')
for y in range(13,18):
    for x in (5,6): put(x,y,'D')
for (x,y) in [(3,10),(8,7),(6,9),(2,13),(9,10),(4,7)]: put(x,y,'C')
# moss and azaleas in the upper garden too: a moss clump at the sakura's foot, azalea bushes at the pagoda's foot
fill(lambda x,y:((x-3)/2.6)**2+((y-17.6)/1.3)**2<=1,'M',15,19)
fill(lambda x,y:((x-8.8)/1.8)**2+((y-17.8)/1.1)**2<=1,'M',15,19)
fill(lambda x,y:((x-20.3)/1.9)**2+((y-17)/1.2)**2<=1,'K',15,19)
fill(lambda x,y:((x-29.3)/2.0)**2+((y-17)/1.2)**2<=1,'K',15,19)
# --- the pocket zone, rows 19-27: the moss mound (left), the azalea bushes (right), the stream between them
top_moss=lambda x:20.2-1.6*math.sin((x-.5)/14*math.pi)          # a rounded mound, highest in its middle
bush=lambda x:min(21.0,*[19.6+((x-c)/2.4)**2*1.4 for c in (19.5,24,28.5)])  # three bushes
for y in range(19,28):
    for x in range(1,15):
        if y+.5>=top_moss(x+.5): put(x,y,'M')
    for x in range(17,31):
        if y+.5>=bush(x+.5): put(x,y,'K')
# the stream from the mountain's foot down into the pond, and the pond's edges round the far bank
for y in range(17,28):
    for x in (15,16): put(x,y,'#')
for y in range(19,28):
    put(0,y,'#'); put(W-1,y,'#')
# the pond: row 28, wider between the bridges (rows 27 and 29)
for x in range(W): put(x,28,'#')
for x in range(11,21): put(x,27,'#')
for x in range(12,20): put(x,29,'#')
xl,xr=8,24
put(xl,28,'b'); put(xr,28,'b')
# near bank, rows 29-31: azaleas (left), moss (right), the stone path from the gate to the pond
for y in range(29,32):
    for x in range(W):
        if g[y][x]=='#': continue
        put(x,y,'C' if x in (15,16) else ('K' if x<15 else 'M'))
# azalea flowers: a few darker blossoms in the bushes (sakura pink)
for (x,y) in [(19,22),(23,21),(27,22),(21,24),(25,25),(29,24),(18,26),(26,23)]: put(x,y,'P')
rows=[''.join(r) for r in g]
json.dump({'rows':rows},sys.stdout)

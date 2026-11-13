# L175 «Воздушный шар над лугом» (Воздушный транспорт; wave L151–L200, Hard bridge+links, one deciding bridge, 17 §8
# «Контур вместо прямой полосы»): a striped hot-air balloon with its wicker basket, outlined by the neutral line (the
# subject behind the contour); two bridge sites on two sides: up from the meadow path (X) onto the basket, and from the
# right, off a tall poplar (M), onto the balloon's side.
import math,json,sys
sys.path.insert(0,'.')
from contour import finish
W,H=34,38
g=[['S']*W for _ in range(H)]
far=[[False]*W for _ in range(H)]
def fill(pred,c,mask=None):
    for y in range(H):
        for x in range(W):
            if pred(x+.5,y+.5) and (mask is None or mask[y][x]): g[y][x]=c
# the sky with clouds, the hills and the meadow
fill(lambda x,y:y>=32+0.8*math.sin(x*.4+1),'G')                     # the meadow
# the balloon: an envelope (a circle on a taper), ropes and the basket
env=lambda x,y:((x-15.5)/9.6)**2+((y-11.5)/9.8)**2<=1 or (y>=11.5 and y<=24.2 and abs(x-15.5)<=9.6*(1-(y-11.5)/15.5)+0.2)
skirt=lambda x,y:23.6<=y<=25.2 and abs(x-15.5)<=2.6
ropes=lambda x,y:25.2<y<27.6 and (abs(x-13.4)<0.55 or abs(x-17.6)<0.55)
basket=lambda x,y:27.4<=y<=31.4 and 12.2<=x<=18.8
subject=lambda x,y:env(x,y) or skirt(x,y) or ropes(x,y) or basket(x,y)
for y in range(H):
    for x in range(W):
        if subject(x+.5,y+.5): far[y][x]=True
# the gaps between the ropes are the subject's own sky
fill(lambda x,y:True,'S',far)
# stripes: red, sun, petal and tangerine panels following the round shape
def stripe(x,y):
    a=math.atan2(x-15.5,max(0.6,y-0.5)); k=int((a+1.6)/0.4)
    return 'RYO'[k%3]
for y in range(H):
    for x in range(W):
        X,Y=x+.5,y+.5
        if far[y][x] and env(X,Y): g[y][x]=stripe(X,Y)
fill(lambda x,y:env(x,y) and 5.4<=y<=6.6,'C',far)                    # a pearl band round the crown
fill(lambda x,y:env(x,y) and 17.2<=y<=18.2,'C',far)                  # and one round the waist
fill(lambda x,y:skirt(x,y),'E',far)                                    # the dark skirt
fill(lambda x,y:ropes(x,y),'E',far)                                    # bark-brown ropes
fill(lambda x,y:basket(x,y),'E',far)
fill(lambda x,y:basket(x,y) and (int(x)+int(y))%3==0,'Y',far)         # sunny wicker weave
fill(lambda x,y:basket(x,y) and y<=28.2,'O',far)                      # the basket's rim
# a tall poplar on the right of the meadow (the right bridge's approach) and a gravel path from the entry
fill(lambda x,y:((x-30.2)/2.3)**2+((y-22.5)/6.2)**2<=1,'M')
fill(lambda x,y:29.6<=x<=30.8 and 27<=y<=32,'E')
rows,anchors=finish(g,far,[('bottom',15),('right',13)],fill=None)
rows=[list(r) for r in rows]
(ax1,ay1),(ax2,ay2)=anchors
for y in range(H):
    for x in range(W):
        if rows[y][x]=='G' and y>=ay1 and abs(x-ax1)<=1.2+(y-ay1)*.3: rows[y][x]='X'   # the gravel path to the basket
json.dump({'rows':[''.join(r) for r in rows]},sys.stdout)

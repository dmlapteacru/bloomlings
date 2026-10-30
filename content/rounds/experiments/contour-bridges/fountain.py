# L23 «Фонтан на площади» (Городские пространства, motif: фонтан): a tiered city fountain on a square, seen from the
# front; its stone silhouette is the contour (17 §8). Inside the basin's water, the column, the upper bowl and the jet;
# outside the sky, the lawn with flower beds, a gravel path up to the basin's left rim and a tree whose crown reaches the
# upper bowl (both bridges cross right: from the path onto the basin's water, from the crown onto the bowl).
import math,json,sys
sys.path.insert(0,'.')
from contour import finish
W,H=32,30
g=[['S']*W for _ in range(H)]
far=[[False]*W for _ in range(H)]
def fill(pred,c,mask=None):
    for y in range(H):
        for x in range(W):
            if pred(x+.5,y+.5) and (mask is None or mask[y][x]): g[y][x]=c
near=lambda: [[not f for f in r] for r in far]
# near: the lawn from the horizon, flower beds in it, the path from the lower left corner to the basin's left rim
fill(lambda x,y:y>=19,'G')
for (bx,by) in [(4,27.6),(28,27.6)]:
    fill(lambda x,y,bx=bx,by=by:((x-bx)/3.0)**2+((y-by)/1.3)**2<=1,'R')
# far: the fountain (basin, column, bowl, jet) right of the middle
CX=20
basin=lambda x,y:abs(x-CX)<=8.6-(y-21)*.25 and 21<=y<=27
column=lambda x,y:abs(x-CX)<=1.6 and 12.5<=y<=21
bowl=lambda x,y:abs(x-CX)<=6.0-(y-10)*.9 and 10<=y<=13
jet=lambda x,y:abs(x-CX)<=1.0+(9.5-y)*.12 and 4<=y<10
for y in range(H):
    for x in range(W):
        X,Y=x+.5,y+.5
        if basin(X,Y) or column(X,Y) or bowl(X,Y) or jet(X,Y): far[y][x]=True
fill(lambda x,y:True,'C',far)                                          # pale stone
fill(lambda x,y:basin(x,y) and y<=23.6,'B',far)                        # the basin's water
fill(lambda x,y:bowl(x,y) and y<=10.9,'B',far)                         # the bowl's water
fill(lambda x,y:jet(x,y),'S',far)                                      # the jet (light water)
fill(lambda x,y:(x-CX)**2+(y-4.4)**2<=1.3,'Y',far)                     # a golden finial in the jet
for (cx,cy) in [(15,22.6),(18,23.2),(23,22.8),(26,23.2)]:
    fill(lambda x,y,cx=cx,cy=cy:(x-cx)**2+(y-cy)**2<=.5,'Y',far)       # coins in the water
fill(lambda x,y:abs(y-25.5)<=.5 and basin(x,y) and int(x)%3==0,'A',far)  # a carved band on the basin
# the tree on the left: trunk and a round crown reaching the bowl
fill(lambda x,y:abs(x-6)<=1.0 and 13<=y<=20,'E')
fill(lambda x,y:((x-7.6)/6.6)**2+((y-9.6)/5.2)**2<=1,'M',near())
fill(lambda x,y:6<=y<=12.6 and 7.6<=x<=14.0,'M',near())                # the crown spreads towards the bowl
# the gravel path from the lower left to the basin's rim
fill(lambda x,y:y>=21.2 and abs((y-21.2)-(11.5-x)*.62)<=1.4 and x<=11.6,'A',near())
fill(lambda x,y:21.2<=y<=23.6 and 9<=x<=11.6,'A',near())
rows,anchors=finish(g,far,[('left',22),('left',11)],fill=None)
json.dump({'rows':rows},sys.stdout)

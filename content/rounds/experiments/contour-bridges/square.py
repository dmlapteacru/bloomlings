# L23 «Сквер с беседкой» (Городские пространства, motif: беседка): a city garden square behind a wrought-iron fence — the
# fence is the contour (17 §8); inside a white gazebo with a red roof, gravel paths, flower beds and two trees; outside
# the street and the yellow pavement before the two gates (both bridges cross up), the sky above the trees.
import math,json,sys
sys.path.insert(0,'.')
from contour import finish,edge_site
W,H=30,30
g=[['S']*W for _ in range(H)]
far=[[False]*W for _ in range(H)]
def fill(pred,c,mask=None):
    for y in range(H):
        for x in range(W):
            if pred(x+.5,y+.5) and (mask is None or mask[y][x]): g[y][x]=c
# near: the street (asphalt) and the yellow pavement on the right
fill(lambda x,y:y>=26,'D')
fill(lambda x,y:y>=26 and x>=16,'Y')
# far: the square (rounded rectangle x 3..26, y 5..24)
sq=lambda x,y:3<=x<=27 and 5<=y<=25 and not ((x<6 or x>24) and (y<8 or y>22) and math.hypot(min(abs(x-6),abs(x-24)),min(abs(y-8),abs(y-22)))>3)
for y in range(H):
    for x in range(W):
        if sq(x+.5,y+.5): far[y][x]=True; g[y][x]='G'
# trees at the two upper corners (round crowns over the fence line are part of the square)
for (cx,cy,r) in [(6.5,8.5,3.4),(23.5,8.5,3.4),(5.5,14.5,2.4),(24.5,14.5,2.4)]:
    fill(lambda x,y,cx=cx,cy=cy,r=r:(x-cx)**2+(y-cy)**2<=r*r,'M',far)
# gravel paths from the gates to the gazebo
fill(lambda x,y:(abs(x-10)<=1.1 and y>=17) or (abs(x-20)<=1.1 and y>=17) or (10<=x<=21 and 16<=y<=18.2),'A',far)
# flower beds along the bottom between the paths and at the sides
fill(lambda x,y:12<=x<=18 and 20<=y<=24,'P',far)
fill(lambda x,y:(4<=x<=8 or 22<=x<=26) and 19<=y<=23,'P',far)
# the gazebo: a pearl floor and pillars, a red roof (a low dome with a finial)
fill(lambda x,y:10<=x<=20 and 14<=y<=15.6,'C',far)
fill(lambda x,y:(abs(x-11)<=.5 or abs(x-15.5)<=.5 or abs(x-19.5)<=.5) and 10<=y<=14,'C',far)
# a bench inside the gazebo
fill(lambda x,y:13<=x<=18 and 12.5<=y<=13.5,'A',far)
fill(lambda x,y:y>=6 and y<=10.2 and abs(x-15.2)<=1.2+(y-6)*1.55,'R',far)
fill(lambda x,y:abs(x-15.2)<=.6 and 5<=y<6,'R',far)
rows,anchors=finish(g,far,[('bottom',10),('bottom',24)],fill='G')
json.dump({'rows':rows},sys.stdout)

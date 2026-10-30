# L50 «Витражная роза» (Фигуры, знаки и эмблемы, motif: витражный знак): a round rose window in a church gable; its stone
# ring is the contour (17 §8); inside leaded tracery, a gold heart, a ring of red petals and blue and plum lobes; outside
# the pale stone wall under the sky, ivy climbing from the ground to the window's lower left and a restorer's wooden ladder
# leaning to its lower right (both bridges cross up), the wooden church door between them.
import math,json,sys
sys.path.insert(0,'.')
from contour import finish
W,H=30,36
CX,CY=15,15
g=[['S']*W for _ in range(H)]
far=[[False]*W for _ in range(H)]
def fill(pred,c,mask=None):
    for y in range(H):
        for x in range(W):
            if pred(x+.5,y+.5) and (mask is None or mask[y][x]): g[y][x]=c
# near: the gable wall (sky only in the upper corners)
fill(lambda x,y:y>=abs(x-15)*0.75-5,'C')
# far: the window
R=11.6
for y in range(H):
    for x in range(W):
        if math.hypot(x+.5-CX,y+.5-CY)<=R: far[y][x]=True
def polar(x,y):
    r=math.hypot(x-CX,y-CY);a=math.degrees(math.atan2(y-CY,x-CX))%360
    return r,a
def spoke(a,n,off=0):
    k=((a-off)%(360/n));return min(k,360/n-k)
def rose(x,y):
    r,a=polar(x,y)
    if r<=2.6: return 'Y'
    if r<=3.5: return 'D'
    if r<=7.0:
        return 'D' if spoke(a,8,22.5)*math.pi/180*r<=.5 else 'R'
    if r<=7.9: return 'D'
    if spoke(a,12)*math.pi/180*r<=.6: return 'D'
    return 'B' if int(((a+15)%360)//30)%2==0 else 'V'
for y in range(H):
    for x in range(W):
        if far[y][x]: g[y][x]=rose(x+.5,y+.5)
# ivy: a stem from the ground curling up to the window's lower left, with leaves
def ivy(x,y):
    if y<23: return False
    sx=4.2+2.2*math.sin((y-23)*.55)+ (33-y)*0.0
    stem=abs(x-sx)<=.8
    leaf=any(math.hypot(x-(sx+s*1.6),y-yy)<=1.15 for s,yy in [(1,25.5),(-1,28),(1,30.5),(-1,33),(1,23.7)])
    return stem or leaf
fill(ivy,'M')
fill(lambda x,y:24<=y<=26.2 and 4<=x<=9.6,'M')           # the vine reaches along under the window
# the ladder: two rails leaning from the ground to the window's lower right, rungs between them
def rail(x,y,x0):
    return abs(x-(x0+(y-26)*0.28))<=.55 and y>=25.6
fill(lambda x,y:rail(x,y,21.6) or rail(x,y,25.2) or (y>=26 and int(y)%2==1 and 21.6+(y-26)*0.28<=x<=25.2+(y-26)*0.28),'E')
# the church door under the window: a pointed wooden arch
fill(lambda x,y:abs(x-15)<=3.2 and (y>=31 or (y>=28.4 and abs(x-15)<=3.2-(31-y)*1.25+0.6)),'E')
rows,anchors=finish(g,far,[('bottom',8),('bottom',22)],fill=None)
json.dump({'rows':rows},sys.stdout)

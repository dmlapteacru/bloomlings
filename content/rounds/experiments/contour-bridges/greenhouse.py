# L35 «Теплица с тюльпанами» (Цветы и букеты, motif: тюльпан): a glass greenhouse full of tulips in a garden; its glass
# walls are the contour (17 §8); inside rows of red, yellow and pink tulips on soil; outside the lawn, the sky and a gravel
# path up to the left front door; the lawn reaches the right door (both bridges cross up).
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
# near: the sky, the lawn from the horizon down, a gravel path from the bottom edge to the left door
fill(lambda x,y:y>=17,'G')
fill(lambda x,y:y>=24 and abs(x-(10.5-(y-24)*0.35))<=1.3+(y-24)*0.35,'A')
# far: the greenhouse (a glass house: a gable roof over walls), soil beds, tulips
house=lambda x,y:6<=x<=26 and 10<=y<=24
roof=lambda x,y:3<=y<10 and abs(x-16)<=(y-2.2)*1.6 and 6<=x<=26
for y in range(H):
    for x in range(W):
        if house(x+.5,y+.5) or roof(x+.5,y+.5): far[y][x]=True
fill(lambda x,y:True,'C',far)                          # the glass panes (pale)
fill(lambda x,y:abs((x-6)%5)<=.5 or abs(y-10)<=.5 or abs(y-17)<=.5,'D',far)   # the frame bars
fill(lambda x,y:y>=22.5,'E',far)                       # soil
# tulips, one in each pane: a cup with a notch over a two-cell stem; heads red, yellow and pink
TUL=['X..X','XXXX','XXXX','.XX.']
def tulip(x0,y0,c,stem):
    for dy,r in enumerate(TUL):
        for dx,ch in enumerate(r):
            if ch=='X': g[y0+dy][x0+dx]=c
    for dy in range(stem):
        g[y0+4+dy][x0+1]=g[y0+4+dy][x0+2]='G'
for i,c in enumerate('RYPR'): tulip(7+5*i,11,c,2)
for i,c in enumerate('YPRY'): tulip(7+5*i,18,c,1)
rows,anchors=finish(g,far,[('bottom',10),('bottom',22)],fill=None)
json.dump({'rows':rows},sys.stdout)

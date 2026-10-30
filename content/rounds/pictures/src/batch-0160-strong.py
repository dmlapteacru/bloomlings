# New "strong" pictures for the open generator positions of wave L21-L50 (colours hidden inside the subject, 17 §2):
# a geode, alchemist's flasks, an apple tree, a sewing box. python3 strong.py <name> > <name>.json
import math,json,sys
def canvas(W,H,bg):
    g=[[bg]*W for _ in range(H)]
    def fill(pred,c,mask=None):
        for y in range(H):
            for x in range(W):
                if pred(x+.5,y+.5) and (mask is None or mask(x+.5,y+.5)): g[y][x]=c
    return g,fill
def geode():
    W,H=30,28;g,fill=canvas(W,H,'S')
    cx,cy=15,14.5
    shell=lambda x,y:((x-cx)/13)**2+((y-cy)/11.6)**2+.06*math.sin(math.atan2(y-cy,x-cx)*7)<=1
    fill(shell,'E')
    for (sx,sy) in [(5,10),(7,19),(23,20),(25,9),(14,25),(18,4),(4,15),(26,15)]:
        fill(lambda x,y,sx=sx,sy=sy:(x-sx)**2+(y-sy)**2<=.7,'D',shell)                    # dark grains in the crust
    band=lambda x,y:((x-cx)/10.6)**2+((y-cy)/9.2)**2<=1
    fill(band,'C')                                                                       # a pale quartz band
    cav=lambda x,y:((x-cx)/8.9)**2+((y-cy)/7.5)**2<=1
    fill(cav,'V')                                                                        # amethyst
    # crystal points of the amethyst pointing into the hollow, lighter tips
    for k in range(14):
        a=k*2*math.pi/14;px,py=cx+7.2*math.cos(a),cy+6.0*math.sin(a)
        fill(lambda x,y,px=px,py=py,a=a:abs((x-px)*math.sin(a)-(y-py)*math.cos(a))<=1.1-.35*abs((x-px)*math.cos(a)+(y-py)*math.sin(a)) and -1.6<=-((x-px)*math.cos(a)+(y-py)*math.sin(a))<=1.6,'P',cav)
    fill(lambda x,y:((x-cx)/4.2)**2+((y-cy)/3.4)**2<=1,'N')                              # the deep hollow
    fill(lambda x,y:((x-cx-.6)/2.0)**2+((y-cy+.4)/1.3)**2<=1,'Y')                        # a citrine glint
    # a crack through the crust at the upper left lets the quartz band show from outside
    fill(lambda x,y:abs((x-cx)-(y-cy))<=1.1 and (x-cx)+(y-cy)<-11,'C',shell)
    return g
def flasks():
    W,H=32,28;g,fill=canvas(W,H,'N')
    fill(lambda x,y:y>=23,'E')                                                           # the wooden shelf
    fill(lambda x,y:y>=25.5,'A')                                                         # its front edge
    def flask(cx,neckw,body,liquid,level,round_=True):
        if round_:
            bodyf=lambda x,y:((x-cx)/body)**2+((y-(23-body))/body)**2<=1
        else:
            bodyf=lambda x,y:abs(x-cx)<=body*(y-(23-2*body))/(2*body)+.6 and 23-2*body<=y<23
        top=23-2*body
        neck=lambda x,y:abs(x-cx)<=neckw and top-5<=y<=top+.8
        glass=lambda x,y:bodyf(x,y) or neck(x,y)
        fill(glass,'C')
        inner=lambda x,y:(((x-cx)/(body-1))**2+((y-(23-body))/(body-1))**2<=1 if round_ else abs(x-cx)<=body*(y-(23-2*body))/(2*body)-.6 and top+1.2<=y<22.2) and y>=level
        fill(inner,liquid)
        for (bx,by) in [(cx-1.5,level+2.2),(cx+1.3,level+3.6),(cx-.2,level+5)]:
            fill(lambda x,y,bx=bx,by=by:(x-bx)**2+(y-by)**2<=.55,'C',inner)              # bubbles
        fill(lambda x,y:abs(x-cx)<=neckw+.4 and top-6.6<=y<top-4.9,'O')                  # the cork
    flask(7,1.1,5.2,'L',15.5)
    flask(16.5,1.0,6.6,'P',13.0,round_=False)
    flask(26,1.1,4.6,'B',16.5)
    return g
def tree():
    W,H=30,32;g,fill=canvas(W,H,'S')
    fill(lambda x,y:y>=25.5+.6*math.sin(x*.4),'G')                                       # the lawn
    fill(lambda x,y:abs(x-15)<=1.8+(y-17)*.12 and 15<=y<=27,'E')                         # the trunk
    fill(lambda x,y:abs(x-15-(y-21)*1.1)<=.8 and 17<=y<=21,'E')                          # a branch
    crown=lambda x,y:any(((x-a)/b)**2+((y-c)/d)**2<=1 for a,c,b,d in [(15,10,11,7.4),(8,13,5.6,4.6),(22,13,5.6,4.6),(15,5,7,4.4)])
    fill(crown,'M')
    inner=lambda x,y:any(((x-a)/b)**2+((y-c)/d)**2<=1 for a,c,b,d in [(15,10,9,5.6),(9,13,3.8,3.0),(21,13,3.8,3.0),(15,5.6,5.2,2.8)])
    fill(inner,'L')
    for (ax,ay,c) in [(10,9,'R'),(15,7,'R'),(20,9,'Y'),(12,13,'Y'),(18,12.5,'R'),(8,14,'R'),(22,14,'R'),(15,11,'Y'),(14,4.5,'R'),(17,4.8,'Y')]:
        fill(lambda x,y,ax=ax,ay=ay:(x-ax)**2+(y-ay)**2<=1.6,c,inner)                     # apples among the leaves
    for (bx,by) in [(6,10.6),(24,10.6),(11,3.4),(19,3.6)]:
        fill(lambda x,y,bx=bx,by=by:(x-bx)**2+(y-by)**2<=.7,'K',crown)                    # a few blossoms on the edge
    fill(lambda x,y:abs(x-21.5)<=2.6 and 26.2<=y<=28.8 and not (abs(x-21.5)<=1.6 and y<27),'A')   # a basket of fallen apples
    fill(lambda x,y:((x-20.8)**2+(y-26.4)**2<=.7) or ((x-22.4)**2+(y-26.4)**2<=.7),'R')
    return g
def sewing():
    W,H=32,28;g,fill=canvas(W,H,'K')
    box=lambda x,y:3<=x<=29 and 12<=y<=25
    fill(box,'E')                                                                        # the wooden box
    fill(lambda x,y:3<=x<=29 and 5<=y<12 and abs(x-16)<=13-(12-y)*0.2,'A')               # the open lid behind
    fill(lambda x,y:5<=x<=27 and 6.5<=y<11,'O')                                          # the lid's lining
    trays=[(5,13,10,18),(11.5,13,20.5,18),(22,13,27,18),(5,19.2,16,24),(17.5,19.2,27,24)]
    for (x0,y0,x1,y1) in trays: fill(lambda x,y,x0=x0,y0=y0,x1=x1,y1=y1:x0<=x<=x1 and y0<=y<=y1,'C')   # compartments
    def spool(cx,cy,c):
        fill(lambda x,y:abs(x-cx)<=.6 and abs(y-cy)<=1.6,c)
        fill(lambda x,y:abs(x-cx)<=1.0 and abs(abs(y-cy)-2.0)<=.5,'A')
    for cx,c in [(13,'R'),(16,'B'),(19,'Y')]: spool(cx,15.5,c)
    fill(lambda x,y:((x-7.5)/2.0)**2+((y-15.5)/1.8)**2<=1,'R')                           # a pincushion
    for (px,py) in [(6.8,14.6),(8.2,15.2),(7.4,16.3)]: g[int(py)][int(px)]='Y'          # pin heads
    fill(lambda x,y:((x-24.5)/1.9)**2+((y-15.5)/1.9)**2<=1,'B')                          # buttons
    fill(lambda x,y:((x-24.5)/.7)**2+((y-15.5)/.7)**2<=1,'C')
    for k,c in enumerate('LRY'):                                                          # balls of yarn
        fill(lambda x,y,k=k:((x-(7.6+k*3.4))/1.8)**2+((y-21.6)/1.8)**2<=1,c)
    fill(lambda x,y:20<=x<=26 and abs(y-21.6)<=.6,'B')                                   # a tape measure
    fill(lambda x,y:20<=x<=26 and abs(y-21.6)<=.6 and int(x)%2==0,'Y')
    return g
f={'geode':geode,'flasks':flasks,'tree':tree,'sewing':sewing}[sys.argv[1]]
json.dump({'rows':[''.join(r) for r in f()]},sys.stdout)

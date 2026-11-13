# Shared helper for contour bridge pictures (17 §8 «Контур вместо прямой полосы»): given a picture grid and the mask of
# its far region (the subject behind the contour), draw the neutral line round it, fold in near pockets the line would
# shut in, put bridge sites with exactly one near neighbour (the anchor) and return the rows.
from collections import deque
D4=((1,0),(-1,0),(0,1),(0,-1))
def finish(g,far,sites,fill='S'):
    H,W=len(g),len(g[0])
    while True:
        line=[[not far[y][x] and any(0<=x+dx<W and 0<=y+dy<H and far[y+dy][x+dx] for dx,dy in D4) for x in range(W)] for y in range(H)]
        op=lambda x,y:not far[y][x] and not line[y][x]
        seen=[[False]*W for _ in range(H)];q=deque((x,y) for y in range(H) for x in range(W) if (x in (0,W-1) or y in (0,H-1)) and op(x,y))
        for x,y in q: seen[y][x]=True
        while q:
            x,y=q.popleft()
            for dx,dy in D4:
                u,v=x+dx,y+dy
                if 0<=u<W and 0<=v<H and op(u,v) and not seen[v][u]: seen[v][u]=True;q.append((u,v))
        shut=[(x,y) for y in range(H) for x in range(W) if op(x,y) and not seen[y][x]]
        inner=[(x,y) for y in range(H) for x in range(W) if line[y][x] and not any(0<=x+dx<W and 0<=y+dy<H and op(x+dx,y+dy) and seen[y+dy][x+dx] for dx,dy in D4)]
        if not shut and not inner: break
        for x,y in shut+inner:
            far[y][x]=True
            if fill: g[y][x]=fill        # fill=None keeps the background's own colour (sky or sea) in the pocket
    for y in range(H):
        for x in range(W):
            if line[y][x]: g[y][x]='#'
    # sites: (x, y, direction of the crossing); the anchor is the near cell opposite the crossing
    out=[]
    # a site given as (side, column or row) is resolved now, on the subject after folding
    sites=[edge_site(far,**({'col':s[1]} if s[0] in ('bottom','top') else {'row':s[1]}),side=s[0]) if isinstance(s[0],str) else s for s in sites]
    for (x,y,d) in sites:
        dx,dy={'up':(0,1),'down':(0,-1),'left':(1,0),'right':(-1,0)}[d]
        ax,ay=x+dx,y+dy
        assert line[y][x], f'site {(x,y)} is not on the line'
        assert not far[ay][ax] and not line[ay][ax], f'anchor {(ax,ay)} is not open'
        assert far[y-dy][x-dx], f'far cell {(x-dx,y-dy)} is not in the subject'
        g[y][x]='b'
        for ex,ey in D4:
            u,v=x+ex,y+ey
            if (u,v)!=(ax,ay) and 0<=u<W and 0<=v<H and not far[v][u]: g[v][u]='#'
        out.append((ax,ay))
    return [''.join(r) for r in g],out
def edge_site(far,col=None,row=None,side='bottom'):
    # the line cell just outside the subject at a column (bottom/top) or a row (left/right)
    H,W=len(far),len(far[0])
    if side=='bottom': return (col,max(y for y in range(H) if far[y][col])+1,'up')
    if side=='top': return (col,min(y for y in range(H) if far[y][col])-1,'down')
    if side=='left': return (min(x for x in range(W) if far[row][x])-1,row,'right')
    if side=='right': return (max(x for x in range(W) if far[row][x])+1,row,'left')

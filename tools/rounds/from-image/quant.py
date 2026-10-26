# Map the read cells onto the pilot palette and write a picture module (step 2 of 2).
# python3 quant.py <cells.json> <module.mjs>. Colours are clustered per picture (ΔE 15); MAP gives each cluster its
# material, chosen by hand per picture.
import json, sys, numpy as np, functools
codes='GPBACDVYOLMRKNSE'
hexes=['#6DBE45','#EB799C','#36B7DA','#A45F28','#F5E6BD','#334B48','#7851A5','#F6CB3E','#EF8A32','#CDEB5E','#2F7D45','#D8394B','#F9C6D6','#2C5AA8','#A8E2F5','#6A3A1E']
def lab(rgb):
    c=np.asarray(rgb,float)/255;c=np.where(c<=.04045,c/12.92,((c+.055)/1.055)**2.4)
    M=np.array([[.4124,.3576,.1805],[.2126,.7152,.0722],[.0193,.1192,.9505]]);xyz=c@M.T/np.array([.9505,1,1.089])
    f=np.where(xyz>.008856,np.cbrt(xyz),7.787*xyz+16/116);return np.stack([116*f[...,1]-16,500*(f[...,0]-f[...,1]),200*(f[...,1]-f[...,2])],-1)
PL=lab([[int(h[i:i+2],16) for i in (1,3,5)] for h in hexes])
res=json.load(open(sys.argv[1]));names={4:'Воздушный шар',5:'Ракета',6:'Гриб',7:'Мороженое',8:'Рыбка',9:'Домик',10:'Кораблик'}
out=[]
# the owner's colours, cluster by cluster (order as found), mapped by hand to the nearest material that keeps the
# picture readable; shades the palette has no match for fold into their neighbour
MAP={4:'YBRCAO',5:'RCCBNRBOYY',6:'RCEAG',7:'CPGYE',8:'BNORCD',9:'RECACBSG',10:'EBCYRABN'}
for k in sorted(res,key=int):
    rows=res[k]['rows'];cells=[(j,i,v) for j,r in enumerate(rows) for i,v in enumerate(r) if v];L=lab([v for _,_,v in cells])
    # greedy clusters by colour, then fold tiny ones into the nearest big one
    cent=[];mem=[]
    for n,x in enumerate(L):
        d=[np.linalg.norm(x-c) for c in cent]
        if d and min(d)<15: m=int(np.argmin(d));mem[m].append(n);cent[m]=L[mem[m]].mean(0)
        else: cent.append(x.copy());mem.append([n])
    big=[m for m in range(len(cent)) if len(mem[m])>=6]
    lab_of=np.zeros(len(L),int)
    for n,x in enumerate(L): lab_of[n]=big[int(np.argmin([np.linalg.norm(x-cent[m]) for m in big]))]
    cl=sorted(set(lab_of));C=[L[lab_of==m].mean(0) for m in cl];S=[int((lab_of==m).sum()) for m in cl]
    # distinct palette codes for the clusters, least total colour distance (weighted by size)
    D=[[np.linalg.norm(c-p) for p in PL] for c in C]
    @functools.lru_cache(None)
    def best(i,used):
        if i==len(C): return (0,())
        r=None
        for p in range(16):
            if used>>p&1: continue
            s,t=best(i+1,used|1<<p);s+=D[i][p]*S[i]**.5
            if r is None or s<r[0]: r=(s,(p,)+t)
        return r
    pick=[codes.index(c) for c in MAP[int(k)]]
    g=[['.']*len(rows[0]) for _ in rows]
    for n,(j,i,_) in enumerate(cells): g[j][i]=codes[pick[cl.index(lab_of[n])]]
    # trim empty border
    ys=[j for j in range(len(g)) if any(c!='.' for c in g[j])];xs=[i for i in range(len(g[0])) if any(g[j][i]!='.' for j in range(len(g)))]
    g=[r[xs[0]:xs[-1]+1] for r in g[ys[0]:ys[-1]+1]]
    out.append({'title':names[int(k)],'forLevel':int(k),'rows':[''.join(r) for r in g]})
    print(k,names[int(k)],'clusters→',' '.join(f"{codes[p]}({S[n]})" for n,p in enumerate(pick)))
open(sys.argv[2],'w').write(
"// The owner's example sheet of 2026-10-26 (levels 4–10), read back cell by cell from the image: each panel's cell\n"
"// seams found, each cell's median colour sampled, empty cells left empty, the picture's colours clustered and each\n"
"// cluster mapped by hand to a material of the pilot palette (MAP in the converter).\n"
"export default {theme:'reference',pictures:"+json.dumps(out,ensure_ascii=False,indent=1)+"};\n")

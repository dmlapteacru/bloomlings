"""Estimate grid size, colors and fill of Colony Flow board previews.
usage: python3 -I board.py <boards_dir> <out_csv> [levels...]"""
from PIL import Image; import numpy as np, sys, os, glob, csv, json

def lab(rgb):
    c=np.asarray(rgb,float)/255.0
    c=np.where(c>0.04045,((c+0.055)/1.055)**2.4,c/12.92)
    M=np.array([[0.4124,0.3576,0.1805],[0.2126,0.7152,0.0722],[0.0193,0.1192,0.9505]])
    xyz=c@M.T/np.array([0.9505,1.0,1.089])
    f=np.where(xyz>0.008856,np.cbrt(xyz),7.787*xyz+16/116)
    return np.stack([116*f[...,1]-16,500*(f[...,0]-f[...,1]),200*(f[...,1]-f[...,2])],-1)

def acorr(p):
    p=p-p.mean(); n=len(p)
    f=np.fft.rfft(p,2*n); r=np.fft.irfft(f*np.conj(f))[:n]; return r/r[0]

def refine(r,p0):
    pe=float(p0)
    for k in range(2,14):
        c=k*pe; a,b=int(c-0.3*pe),int(c+0.3*pe)+1
        a=max(a,1); b=max(b,a+2)
        if b>=len(r)-1: break
        jj=a+int(np.argmax(r[a:b]))
        if r[jj]<0.25: break
        y0,y1,y2=r[jj-1],r[jj],r[jj+1]; den=(y0-2*y1+y2); d=0.5*(y0-y2)/den if den!=0 else 0
        pe=(jj+d)/k
    return pe

def pitch(profile, maxlag):
    r=acorr(profile)
    peaks=[i for i in range(5,min(maxlag,len(r)-2)) if r[i]>r[i-1] and r[i]>=r[i+1] and r[i]>0.05]
    if not peaks: return None,0
    top=max(r[i] for i in peaks)
    p0=next(i for i in peaks if r[i]>=0.6*top)
    return refine(r,p0), float(r[p0])

def pitch_near(profile, expect):
    r=acorr(profile); lo,hi=max(2,int(expect*0.88)),int(expect*1.12)+1; hi=max(hi,lo+2)
    j=lo+int(np.argmax(r[lo:hi]))
    return refine(r,j), float(r[j])

RATIO=0.865
BGC=5
BGL=8
def phase(profile,p):
    best=(-1,0)
    for ph in np.arange(0,p,0.25):
        idx=np.arange(ph,len(profile)-1,p)
        v=np.interp(idx,np.arange(len(profile)),profile).mean()
        if v>best[0]: best=(v,ph)
    return best[1]

def analyse(path):
    im=Image.open(path).convert('RGB'); a=np.asarray(im).astype(float)
    H,W,_=a.shape; m=int(0.06*min(H,W)); a=a[m:H-m,m:W-m]; H,W,_=a.shape
    g=a.mean(2)
    # background reference: most common quantised colour in a thin strip just inside the frame
    strip=np.concatenate([a[:int(0.015*H)].reshape(-1,3),a[-int(0.015*H):].reshape(-1,3),a[:,:int(0.015*W)].reshape(-1,3),a[:,-int(0.015*W):].reshape(-1,3)])
    q=(strip//8).astype(int); keys,cnt=np.unique(q[:,0]*10000+q[:,1]*100+q[:,2],axis=0,return_counts=True)
    k=keys[np.argmax(cnt)]; bgq=np.array([k//10000,(k//100)%100,k%100]); sel=(q==bgq).all(1)
    bg=strip[sel].mean(0)
    px,cx=pitch(np.abs(np.diff(g,axis=1)).sum(0),int(W/6))
    py,cy=pitch(np.abs(np.diff(g,axis=0)).sum(1),int(H/6))
    if not px or not py: return dict(error='no pitch')
    fixed=''
    PX=np.abs(np.diff(g,axis=1)).sum(0); PY=np.abs(np.diff(g,axis=0)).sum(1)
    rX,rY=acorr(PX),acorr(PY)
    def contrast(r,p):
        ks=[k for k in range(1,40) if (k+0.5)*p<min(len(r)-1,6*p+1)]
        on=np.mean([np.interp(k*p,np.arange(len(r)),r) for k in ks]); off=np.mean([np.interp((k-0.5)*p,np.arange(len(r)),r) for k in ks])
        return on-off
    cands=[('',px,py)]
    ay,_=pitch_near(PY,px*RATIO); cands.append(('y',px,ay))
    ax,_=pitch_near(PX,py/RATIO); cands.append(('x',ax,py))
    if px/2>=7 and py/2>=6:
        hx,_=pitch_near(PX,px/2); hy,_=pitch_near(PY,py/2); cands.append(('half',hx,hy))
    best=None
    for tag,a_,b_ in cands:
        if not (0.8<b_/a_<0.93): continue
        sc=contrast(rX,a_)+contrast(rY,b_)
        if best is None or sc>best[0]+1e-9: best=(sc,tag,a_,b_)
    if best: _,fixed,px,py=best
    phx=phase(np.abs(np.diff(g,axis=1)).sum(0),px); phy=phase(np.abs(np.diff(g,axis=0)).sum(1),py)
    # vertical phase: choose the offset whose sampling band [0.25,0.6] lands on the lit top faces (brightest)
    fg=(np.abs(a-bg).max(2)>20)
    lum=(g*fg).sum(1)/np.maximum(fg.sum(1),1); wgt=fg.sum(1)
    best=(-1,phy)
    for ph in np.arange(0,py,0.5):
        tot=0; wsum=0
        for y0 in np.arange(ph-py,H,py):
            lo,hi=int(max(0,y0+0.25*py)),int(min(H,y0+0.6*py))
            if hi<=lo: continue
            tot+=(lum[lo:hi]*wgt[lo:hi]).sum(); wsum+=wgt[lo:hi].sum()
        v=tot/max(wsum,1)
        if v>best[0]: best=(v,ph)
    phy=best[1]
    x0s=phx-px*np.floor((phx+px*0.3)/px); y0s=phy-py*np.floor((phy+py*0.25)/py)
    xs=np.arange(x0s,W-px*0.7,px); ys=np.arange(y0s,H-py*0.6,py)
    cells=[];
    for j,y0 in enumerate(ys):
        for i,x0 in enumerate(xs):
            x1,x2=int(x0+px*0.3),int(x0+px*0.7); y1,y2=int(y0+py*0.25),int(y0+py*0.6)
            if x1<0 or y1<0 or x2>W or y2>H: continue
            patch=a[y1:y2,x1:x2].reshape(-1,3)
            med=np.median(patch,0); sd=patch.std(0).mean()
            full=a[max(0,int(y0)):int(y0+py),max(0,int(x0)):int(x0+px)]
            fsd=full.reshape(-1,3).std(0).mean()
            d=np.abs(med-bg).max()
            cells.append((i,j,med,d,fsd,(m+y0+py*0.42)/(H+2*m),(m+x0+px*0.5)/(W+2*m)))
    bgl=lab(bg)
    def isbg(med):
        l=lab(med); return np.hypot(l[1]-bgl[1],l[2]-bgl[2])<BGC and abs(l[0]-bgl[0])<BGL
    tile=[c for c in cells if not isbg(c[2])]
    if not tile: return dict(error='no tiles')
    I=np.array([c[0] for c in tile]); J=np.array([c[1] for c in tile])
    # trim stray columns/rows with very few tiles (frame shadow, watermark)
    def span(v,other_n):
        vals,cnt=np.unique(v,return_counts=True); ok=vals[cnt>=1]
        return ok.min(),ok.max()
    i0,i1=span(I,0); j0,j1=span(J,0)
    keep=[c for c in tile if i0<=c[0]<=i1 and j0<=c[1]<=j1]
    # the last row of cubes shows its front face below the top faces: drop that band if it is a darker copy of the row above
    frontdrop=0
    for _ in range(2):
        last={c[0]:c[2] for c in keep if c[1]==j1}; above={c[0]:c[2] for c in keep if c[1]==j1-1}
        common=[i for i in last if i in above]
        if last and len(common)>=0.9*len(last):
            La=lab(np.array([last[i] for i in common])); Lb=lab(np.array([above[i] for i in common]))
            dL=La[:,0]-Lb[:,0]; dab=np.hypot(La[:,1]-Lb[:,1],La[:,2]-Lb[:,2])
            if np.median(dL)<-8 and np.mean((dL<-5)&(dab<30))>=0.7:
                keep=[c for c in keep if c[1]!=j1]; j1-=1; frontdrop+=1; continue
        break
    gw,gh=i1-i0+1,j1-j0+1
    fill=len(keep)/(gw*gh)
    # colours: exclude watermark band
    inband=lambda c: 0.41<c[5]<0.60 and 0.18<c[6]<0.82
    def cluster(L,thr=11):
        cent=[];memb=[]
        for v in L:
            if cent:
                dd=np.linalg.norm(np.array(cent)-v,axis=1); b_=int(np.argmin(dd))
                if dd[b_]<thr: memb[b_].append(v); cent[b_]=np.mean(memb[b_],0); continue
            cent.append(v.copy()); memb.append([v])
        changed=True
        while changed:
            changed=False
            for x in range(len(cent)):
                for y in range(x+1,len(cent)):
                    if np.linalg.norm(cent[x]-cent[y])<thr:
                        memb[x]+=memb[y]; cent[x]=np.mean(memb[x],0); del cent[y]; del memb[y]; changed=True; break
                if changed: break
        return cent,memb
    out_=[c[2] for c in keep if not inband(c)]; in_=[c[2] for c in keep if inband(c)]
    L=lab(np.array(out_)) if out_ else np.zeros((0,3))
    cent,memb=cluster(L)
    n=len(L); minc=max(3,0.006*n)
    big=[(c,len(mm)) for c,mm in zip(cent,memb) if len(mm)>=minc]
    big.sort(key=lambda t:-t[1])
    # colours seen only under the semi-transparent watermark: count a band cluster that matches nothing outside (darkened copies matched loosely)
    extra=0
    if in_:
        Lb=lab(np.array(in_)); cb,mb=cluster(Lb)
        for c,mm in zip(cb,mb):
            if len(mm)<max(3,0.006*(n+len(Lb))): continue
            if big:
                dd=min(np.linalg.norm(c-bc) for bc,_ in big)
                # watermark darkens and greys: compare also against outside colours darkened by ~15-35 L
                dd2=min(np.hypot(np.hypot(c[1]-bc[1]*0.8,c[2]-bc[2]*0.8),max(0,abs(bc[0]-c[0])-35)) for bc,_ in big)
                if min(dd,dd2)<14: continue
            extra+=1
    dark=sum(s for c,s in big if c[0]<38)/n
    white=sum(s for c,s in big if c[0]>84 and np.hypot(c[1],c[2])<14)/n
    cream=sum(s for c,s in big if c[0]>78 and 10<=c[2]<=35 and abs(c[1])<12)/n
    globals()['GEOM']=(m,xs,ys,px,py,i0,i1,j0,j1,keep)
    return dict(imgW=im.size[0],imgH=im.size[1],pitchX=round(px,2),pitchY=round(py,2),acX=round(cx,2),acY=round(cy,2),
                gridW=int(gw),gridH=int(gh),cells=int(gw*gh),tileCells=len(keep),fill=round(fill,3),
                colors=len(big)+extra,colorsOutsideWatermark=len(big),watermarkExtra=extra,darkShare=round(dark,3),whiteShare=round(white,3),creamShare=round(cream,3),
                frontDrop=frontdrop,pitchFixed=fixed,palette=';'.join('%d/%d/%d:%d'%(round(c[0]),round(c[1]),round(c[2]),s) for c,s in big))

if __name__=='__main__':
    d,out=sys.argv[1],sys.argv[2]
    lv=[int(x) for x in sys.argv[3:]] or sorted(int(os.path.basename(f)[:-5]) for f in glob.glob(d+'/*.webp'))
    rows=[]
    for n in lv:
        try: r=analyse(f'{d}/{n}.webp')
        except Exception as e: r=dict(error=repr(e))
        r['level']=n; rows.append(r)
        if len(lv)<20: print(n,{k:v for k,v in r.items() if k!='palette'})
    keys=['level','imgW','imgH','pitchX','pitchY','acX','acY','gridW','gridH','cells','tileCells','fill','colors','colorsOutsideWatermark','watermarkExtra','frontDrop','pitchFixed','darkShare','whiteShare','creamShare','palette','error']
    with open(out,'w',newline='') as f:
        w=csv.DictWriter(f,keys); w.writeheader(); [w.writerow(r) for r in rows]

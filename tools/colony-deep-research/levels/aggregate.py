import csv, json, statistics as st, sys, collections
W=sys.argv[1]
T={int(r['level']):r for r in csv.DictReader(open(W+'/data/text_features.csv'))}
B={int(r['level']):r for r in csv.DictReader(open(W+'/data/boards.csv'))}
F={int(r['level']):r for r in csv.DictReader(open(W+'/data/frame1.csv'))}
TR={int(r['level']):r for r in csv.DictReader(open(W+'/data/tray.csv'))}
levels=sorted(T)
bands=[(1,10),(11,20),(21,50),(51,100),(101,200),(201,300),(301,400),(401,500),(501,600),(601,700),(701,800),(801,900),(901,1000),(1001,1100),(1101,1200),(1201,1317)]
def q(v,p):
    v=sorted(v); 
    if not v: return ''
    k=(len(v)-1)*p; f=int(k); c=min(f+1,len(v)-1); return v[f]+(v[c]-v[f])*(k-f)
def fmt(x): return ('%g'%round(x,1)) if x!='' else '-'
out=[]
out.append('### Board grid (cells wide x tall, from preview image) and colours by band\n')
out.append('| Band | n | width med (range) | height med (range) | cells med (p10-p90) | fill med | colours med (range) | colours incl. watermark med | dark outline share>=5% | white share>=5% | tray cols med (range) |')
out.append('|---|---|---|---|---|---|---|---|---|---|---|')
for a,b in bands:
    L=[n for n in levels if a<=n<=b and n in B and not B[n]['error']]
    if not L: continue
    w=[int(B[n]['gridW']) for n in L]; h=[int(B[n]['gridH']) for n in L]; c=[int(B[n]['cells']) for n in L]
    fl=[float(B[n]['fill']) for n in L]; co=[int(B[n]['colorsOutsideWatermark']) for n in L]; ca=[int(B[n]['colors']) for n in L]
    dk=sum(float(B[n]['darkShare'])>=0.05 for n in L); wh=sum(float(B[n]['whiteShare'])>=0.05 for n in L)
    tc=[int(TR[n]['trayCols']) for n in L if n in TR and TR[n]['trayCols']]
    out.append(f'| {a}-{b} | {len(L)} | {fmt(st.median(w))} ({min(w)}-{max(w)}) | {fmt(st.median(h))} ({min(h)}-{max(h)}) | {fmt(st.median(c))} ({fmt(q(c,.1))}-{fmt(q(c,.9))}) | {st.median(fl):.2f} | {fmt(st.median(co))} ({min(co)}-{max(co)}) | {fmt(st.median(ca))} | {dk}/{len(L)} | {wh}/{len(L)} | {fmt(st.median(tc)) if tc else "-"} ({min(tc) if tc else ""}-{max(tc) if tc else ""}) |')
# mechanics
mk=['m_mystery_pod','m_mystery_tile','m_linked','m_key','m_locked_pod','m_locked_tile','m_chocolate','m_hungry_door','m_chest_or_box_counter','m_counter_word','m_bomb','m_ice','m_conveyor','m_portal','m_wall_obstacle','m_no_special_claim']
b50=[(i,i+49) for i in range(1,1317,50)]
out.append('\n### Text mechanic keywords: first level, share of sampled levels per band\n')
hdr='| Keyword | first L | total levels | '+' | '.join(f'{a}-{b}' for a,b in b50)+' |'
out.append(hdr); out.append('|'+'---|'*(3+len(b50)))
ns=[len([n for n in levels if a<=n<=b]) for a,b in b50]
out.append('| (levels sampled) | | | '+' | '.join(str(x) for x in ns)+' |')
for k in mk:
    has=[n for n in levels if int(T[n][k])>0]
    first=has[0] if has else '-'
    cells=[]
    for (a,b),nn in zip(b50,ns):
        x=len([n for n in has if a<=n<=b]); cells.append(f'{round(100*x/nn)}' if nn else '-')
    out.append(f'| {k[2:]} | {first} | {len(has)} | '+' | '.join(cells)+' |')
dk=['d_hard_level','d_super_hard','d_very_hard','d_tricky','d_challenging','d_difficult','d_easy','d_beginner','d_boss','d_difficulty_word']
out.append('\n### Text difficulty words (levels containing the word)\n')
out.append('| Word | levels | first L | examples |'); out.append('|---|---|---|---|')
for k in dk:
    has=[n for n in levels if int(T[n][k])>0]
    out.append(f'| {k[2:]} | {len(has)} | {has[0] if has else "-"} | {", ".join(map(str,has[:12]))} |')
# pod numbers by band
out.append('\n### Pod numbers mentioned in text, by band (all mentions pooled)\n')
out.append('| Band | levels with nums | mentions | p10 | median | p90 | max | per-level median of max |'); out.append('|---|---|---|---|---|---|---|---|')
for a,b in bands:
    L=[n for n in levels if a<=n<=b]
    vals=[int(x) for n in L for x in T[n]['podNums'].split(';') if x]
    mx=[max(int(x) for x in T[n]['podNums'].split(';') if x) for n in L if T[n]['podNums']]
    if not vals: continue
    out.append(f'| {a}-{b} | {len(mx)}/{len(L)} | {len(vals)} | {fmt(q(vals,.1))} | {fmt(st.median(vals))} | {fmt(q(vals,.9))} | {max(vals)} | {fmt(st.median(mx))} |')
out.append('\n### Obstacle counter numbers mentioned (doors/chocolate/chests), by band\n')
out.append('| Band | levels | median | max |'); out.append('|---|---|---|---|')
for a,b in bands:
    L=[n for n in levels if a<=n<=b]
    vals=[int(x) for n in L for x in T[n]['counterNums'].split(';') if x and int(x)>=10]
    if vals: out.append(f'| {a}-{b} | {len([n for n in L if T[n]["counterNums"]])} | {fmt(st.median(vals))} | {max(vals)} |')
# difficulty UI colour
out.append('\n### In-game UI accent colour in first frame (pause button + bottom bar)\n')
out.append('| Band | full-screen frames | blue | purple | red | which levels are non-blue |'); out.append('|---|---|---|---|---|---|')
for a,b in [(1,50),(51,100),(101,150),(151,200),(201,250),(251,300),(301,370)]:
    L=[n for n in levels if a<=n<=b and F[n]['pause'] not in ('',None)]
    col=lambda n: 'purple' if F[n]['pause'] in ('purple','magenta') else F[n]['pause']
    c=collections.Counter(col(n) for n in L)
    nb=[f"{n}{'p' if col(n)=='purple' else 'R'}" for n in L if col(n)!='blue']
    out.append(f'| {a}-{b} | {len(L)} | {c["blue"]} | {c["purple"]} | {c["red"]} | {" ".join(nb)} |')
# colour vs board/tray
out.append('\n### Board/tray metrics by UI colour (L1-370, full-screen frames)\n')
out.append('| UI colour | n | cells med | colours med | tray cols med | pod text median of max | linked % | mystery % |'); out.append('|---|---|---|---|---|---|---|---|')
for cname in ('blue','purple','red'):
    L=[n for n in levels if n<=370 and F[n]['pause'] and (('purple' if F[n]['pause'] in ('purple','magenta') else F[n]['pause'])==cname)]
    L2=[n for n in L if n>=11]
    c=[int(B[n]['cells']) for n in L2]; co=[int(B[n]['colorsOutsideWatermark']) for n in L2]
    tc=[int(TR[n]['trayCols']) for n in L2 if TR[n]['trayCols']]
    mx=[max(int(x) for x in T[n]['podNums'].split(';') if x) for n in L2 if T[n]['podNums']]
    li=sum(int(T[n]['m_linked'])>0 for n in L2); my=sum((int(T[n]['m_mystery_pod'])+int(T[n]['m_mystery_tile']))>0 for n in L2)
    out.append(f'| {cname} (L11+) | {len(L2)} | {fmt(st.median(c))} | {fmt(st.median(co))} | {fmt(st.median(tc))} | {fmt(st.median(mx)) if mx else "-"} | {round(100*li/len(L2))} | {round(100*my/len(L2))} |')
open(W+'/data/tables.md','w').write('\n'.join(out))
print('\n'.join(out))

import csv, sys, collections, statistics as st
W=sys.argv[1]
T={int(r['level']):r for r in csv.DictReader(open(W+'/data/text_features.csv'))}
O={int(r['level']):r for r in csv.DictReader(open(W+'/data/board_objects.csv'))}
F={int(r['level']):r for r in csv.DictReader(open(W+'/data/frame1.csv'))}
B={int(r['level']):r for r in csv.DictReader(open(W+'/data/boards.csv'))}
TR={int(r['level']):r for r in csv.DictReader(open(W+'/data/tray.csv'))}
L=sorted(T)
vnum=lambda n:int(O[n]['numberGlyphs'])>=2
vmys=lambda n:int(O[n]['mysteryCells'])>=8
out=['### Visual detectors on board previews, share of sampled levels per band (%)\n','| Band | n | numbered object (door/chocolate/counter) | grey ? tiles on board |','|---|---|---|---|']
for a,b in [(1,50),(51,100),(101,150),(151,200),(201,250),(251,300),(301,400),(401,500),(501,600),(601,800),(801,1000),(1001,1317)]:
    LL=[n for n in L if a<=n<=b]
    out.append(f'| {a}-{b} | {len(LL)} | {round(100*sum(vnum(n) for n in LL)/len(LL))} | {round(100*sum(vmys(n) for n in LL)/len(LL))} |')
f1=[n for n in L if vnum(n)][:1]; f2=[n for n in L if vmys(n)][:1]
out.append(f'\nFirst detected: numbered object L{f1[0]}, ? tiles L{f2[0]}.\n')
# agreement
tn=lambda n: any(int(T[n][k])>0 for k in ('m_chocolate','m_hungry_door','m_chest_or_box_counter'))
tm=lambda n: int(T[n]['m_mystery_tile'])>0
vn=[n for n in L if vnum(n)]; vm=[n for n in L if vmys(n)]
out.append(f'Text agreement: of {len(vn)} boards with a detected numbered object, the text names chocolate/door/chest/box in {sum(tn(n) for n in vn)} ({round(100*sum(tn(n) for n in vn)/len(vn))}%). Of {len(vm)} boards with detected ? tiles, the text names hidden food/hidden tiles/question-mark tiles in {sum(tm(n) for n in vm)} ({round(100*sum(tm(n) for n in vm)/len(vm))}%); it names any hidden/question/mystery item in {sum(int(T[n]["m_question_any"])>0 for n in vm)}.')
nn=[n for n in L if not vnum(n) and n<=300]
out.append(f'Reverse: of {len(nn)} L1-300 boards with no detected number, text still names chocolate/hungry door in {sum(int(T[n]["m_chocolate"])+int(T[n]["m_hungry_door"])>0 for n in nn)}.')
# by colour
out.append('\n### Visual mechanics by UI colour (L11-370)\n'); out.append('| UI colour | n | numbered object % | ? tiles % | text linked % | text mystery pod % | text key % |'); out.append('|---|---|---|---|---|---|---|')
for c in ('blue','purple','red'):
    LL=[n for n in L if 11<=n<=370 and F.get(n,{}).get('pause') and (('purple' if F[n]['pause'] in ('purple','magenta') else F[n]['pause'])==c)]
    pct=lambda f: round(100*sum(f(n) for n in LL)/len(LL))
    out.append(f'| {c} | {len(LL)} | {pct(vnum)} | {pct(vmys)} | {pct(lambda n:int(T[n]["m_linked"])>0)} | {pct(lambda n:int(T[n]["m_mystery_pod"])>0)} | {pct(lambda n:int(T[n]["m_key"])>0)} |')
open(W+'/data/tables2.md','w').write('\n'.join(out)); print('\n'.join(out))
# merged per-level CSV
keys_b=['gridW','gridH','cells','fill','colors','colorsOutsideWatermark','darkShare','whiteShare','creamShare','pitchX','pitchY']
keys_t=['nColors','nPodNums','podMin','podMedian','podMax','counterMax','m_mystery_pod','m_mystery_tile','m_linked','m_key','m_locked_pod','m_locked_tile','m_chocolate','m_hungry_door','m_chest_or_box_counter','m_bomb','m_ice','m_portal','m_no_special_claim','d_tricky','d_challenging','d_difficult','d_easy']
with open(W+'/data/levels.csv','w',newline='') as fh:
    w=csv.writer(fh); w.writerow(['level','uiColour','trayCols']+['board_'+k for k in keys_b]+['vis_mysteryCells','vis_numberGlyphs']+['text_'+k for k in keys_t])
    for n in L:
        col=F.get(n,{}).get('pause') or ''; col='purple' if col=='magenta' else col
        w.writerow([n,col,TR.get(n,{}).get('trayCols','')]+[B[n][k] for k in keys_b]+[O[n]['mysteryCells'],O[n]['numberGlyphs']]+[T[n][k] for k in keys_t])

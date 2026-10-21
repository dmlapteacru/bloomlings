import sys,re,glob,os,collections
K={
'linked/connected':r'connected box|linked|chained box|connected pair|dual color',
'mystery ?':r'question mark|mystery|hidden box|unknown',
'hidden board tiles':r'question mark tiles|hidden tiles|covered tiles|fog|blank tiles',
'key':r'\bkeys?\b',
'lock(ed box)':r'\block(ed)? box|padlock|\blocks?\b',
'chocolate/brown block':r'chocolate|brown (barrier|block|obstacle)|crate|wooden (box|block|barrier)',
'door':r'\bdoors?\b',
'counter tile':r'counter|number on (the )?(tile|block)|countdown',
'ice':r'\bice\b|frozen|icy',
'bomb':r'\bbomb',
'wall':r'\bwalls?\b',
'conveyor':r'conveyor|belt',
'portal/tunnel':r'portal|tunnel|teleport',
'rope/chain':r'\brope|\bchains?\b',
'cage/glass':r'\bcage|\bglass',
'egg/larva':r'\begg|larva',
'pipe':r'\bpipe',
'spider/enemy':r'spider|beetle|enemy',
'gate/barrier':r'\bgates?\b|barrier',
'hard label':r'super hard|hard level',
}
rows=[]
for f in glob.glob(sys.argv[1]+'/L*.txt'):
    n=int(os.path.basename(f)[1:-4]); t=open(f).read()
    i=t.find('Walkthrough Solution & Guides'); j=t.find('Leave a Comment')
    t=t[i:j] if i>0 and j>0 else t
    rows.append((n,t))
rows.sort()
print('pages',len(rows))
for k,p in K.items():
    hits=[n for n,t in rows if re.search(p,t,re.I)]
    print(f'{k:22s} n={len(hits):3d} first={hits[:12]}')

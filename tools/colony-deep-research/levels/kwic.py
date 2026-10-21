import json,re,sys
d=json.load(open(sys.argv[1])); pat=re.compile(sys.argv[2],re.I); lim=int(sys.argv[3]) if len(sys.argv)>3 else 30
k=0
for r in d:
    for m in pat.finditer(r['article']):
        a=r['article']; print(r['level'],'|',a[max(0,m.start()-110):m.end()+110].replace('\n',' ')); k+=1
        if k>=lim: sys.exit()

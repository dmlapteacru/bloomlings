import sys
L=[int(x) for x in open(sys.argv[1])]
S=set(L)
sel=[x for x in L if x<=300]
sel+=[x for x in L if 300<x<=600 and x%5==0]
# 601+: listed level nearest each multiple of 10
for t in range(610,1320,10):
    c=min(L,key=lambda x:abs(x-t))
    if c>600 and abs(c-t)<=3: sel.append(c)
sel.append(max(L))
sel=sorted(set(sel))
print('\n'.join(map(str,sel)))

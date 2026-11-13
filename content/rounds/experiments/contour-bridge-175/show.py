# print a rows JSON as text with counts (a quick look while drawing)
import json,sys,collections
r=json.load(open(sys.argv[1]))['rows'];c=collections.Counter(ch for row in r for ch in row if ch not in '.#b')
print('\n'.join(r));print(len(r[0]),'x',len(r),sum(c.values()),'cells',len(c),'colours',dict(c.most_common()))

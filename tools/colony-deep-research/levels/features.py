"""Extract text features per level from cleaned walkthrough articles."""
import json, re, sys, csv, collections
d=json.load(open(sys.argv[1])); out_csv=sys.argv[2]; out_json=sys.argv[3]
UNITS={w:i for i,w in enumerate('zero one two three four five six seven eight nine ten eleven twelve thirteen fourteen fifteen sixteen seventeen eighteen nineteen'.split())}
TENS={w:10*(i+2) for i,w in enumerate('twenty thirty forty fifty sixty seventy eighty ninety'.split())}
def words2num(t):
    toks=re.split(r'(\W+)',t); out=[]; i=0
    def isnum(w): w=w.lower(); return w in UNITS or w in TENS or w in ('hundred','thousand')
    while i<len(toks):
        w=toks[i]
        if isnum(w) and w.lower()!='zero':
            val=0; cur=0; j=i; last=i
            while j<len(toks):
                lw=toks[j].lower()
                if lw in UNITS: cur+=UNITS[lw]; last=j
                elif lw in TENS: cur+=TENS[lw]; last=j
                elif lw=='hundred': cur=max(cur,1)*100; last=j
                elif lw=='thousand': val+=max(cur,1)*1000; cur=0; last=j
                elif re.fullmatch(r'[\s-]+',toks[j]) or lw=='and' and j+2<len(toks) and isnum(toks[j+2]): pass
                else: break
                j+=1
            out.append(str(val+cur)); i=last+1
        else: out.append(w); i+=1
    return ''.join(out)
BASE=['white','black','red','orange','yellow','green','blue','cyan','teal','purple','violet','pink','magenta','brown','beige','tan','cream','grey','gray','lime','navy','peach','maroon','lavender','gold','golden','turquoise','indigo','olive','mint','salmon','crimson','silver']
MOD=['light','dark','medium','bright','pale','deep','hot','neon','pastel','sky','royal','baby','forest','lime','olive','mint']
color_re=re.compile(r'\b(?:(%s)[ -])?(%s)\b'%('|'.join(MOD),'|'.join(BASE)),re.I)
def colors(t):
    s=set()
    for m in color_re.finditer(t):
        base=m.group(2).lower().replace('gray','grey').replace('golden','gold')
        mod=(m.group(1) or '').lower()
        if base in ('gold','silver') : continue  # mostly objects (gold medal, golden key)
        s.add((mod+' '+base).strip())
    return s
MECH={
 'mystery_pod': r'hidden box|mystery box|question[- ]mark (?:block|box|pod)s?|blocks? (?:with|marked with|showing|displaying) (?:a |large )?question marks?|grey (?:question )?blocks?',
 'mystery_tile': r'hidden food|hidden tiles?|question[- ]mark tiles?|tiles? (?:with|marked with|covered by|displaying) (?:a |small )?question marks?|grey hidden',
 'question_any': r'question marks?|mystery|hidden',
 'linked': r'connected box|connected block|connected pair|linked (?:together|blocks?|pair|boxes)|blocks? linked|are linked|linked directly|physically linked|connected chain',
 'key': r'\b(?:yellow|blue|green|red|golden|gold|pink|purple|orange|white|cyan|silver|black|light blue|dark blue)\s+keys?\b|\bkeys? (?:sits?|icons?|symbols?|tiles?|items?)\b|\b(?:collect|grab|reach|secure|free|unlock|get)(?:s|ing|ed)? (?:the |both |all |two |three )?(?:\w+ )?keys?\b|\bkeys? (?:on|in) the board',
 'locked_pod': r'locked (?:block|box|pod|crate)s?|\block (?:symbols?|icons?|block)|padlock|blocks? (?:remain|stay)s? (?:completely )?(?:locked|frozen)|locked tray',
 'locked_tile': r'locked (?:\w+ )?tiles?|lock(?:ed)? (?:grey |gray )?tiles?',
 'chocolate': r'chocolate',
 'hungry_door': r'hungry doors?',
 'door_any': r'\bdoors?\b',
 'chest_or_box_counter': r'wooden box(?:es)?|wooden crate|\b(?:giant|massive|large|big) (?:numbered )?(?:crate|chest)s?\b|numbered (?:chest|crate|wooden)|\bchest stamped|\bchest (?:labeled|marked|requires|with the number)',
 'counter_word': r'\bcounters?\b|countdown|health number|starting count|requirement of|requires? \d+ ants',
 'bomb': r'\bbombs?\b',
 'ice': r'\bice (?:block|tile)s?\b|\bfrozen (?:block|tile)s? on the board',
 'conveyor': r'conveyor',
 'portal': r'\bportals?\b',
 'wall_obstacle': r'stone wall|brick wall|wall block|barrier block|obstacle block',
 'barrier_word': r'barriers?|obstacles?',
}
DIFF={
 'hard_level': r'\bhard level\b|\bhard stage\b|\bhard puzzle\b',
 'super_hard': r'super hard',
 'very_hard': r'very hard|extremely hard|incredibly hard',
 'tricky': r'\btricky\b',
 'challenging': r'\bchalleng',
 'difficult': r'\bdifficult',
 'easy': r'\beasy\b',
 'beginner': r'\bbeginner',
 'boss': r'\bboss\b',
 'skull': r'\bskull',
 'difficulty_word': r'\bdifficulty\b',
}
EXCL=re.compile(r'\b(door|doors|chocolate|chest|crate|counter|requirement|requires|require|slot|slots|level|step|steps|key|keys|lock|minutes|seconds|percent|times|rows?|columns?|layers?|tiles|pixels?|sections?|sides?|corners?|waiting|spaces?|stage|guide)\b',re.I)
POD_AFTER=re.compile(r'^\s*(?:[a-z]+\s+){0,3}?(?:blocks?|boxes|box|pods?)\b|^\s*ants?\b',re.I)
def numbers(t):
    t2=words2num(t)
    pods=[]; ctr=[]
    for m in re.finditer(r'\b(\d{1,4})\b',t2):
        v=int(m.group(1)); before=t2[max(0,m.start()-45):m.start()]; after=t2[m.end():m.end()+40]
        if re.search(r'(level|step|lv\.?)\s*$',before,re.I): continue
        win_b=' '.join(re.findall(r'\w+',before)[-5:]); win_a=' '.join(re.findall(r'\w+',after)[:4])
        if re.search(r'door|chocolate|chest|crate|counter|requirement|requires|wooden box|starting count|health',win_b+' '+win_a,re.I):
            ctr.append(v); continue
        colb=re.search(r'(%s)\s*$'%'|'.join(BASE),before,re.I)
        pa=POD_AFTER.search(after)
        hold=re.search(r'(holding|hold|holds|counts? of|labeled(?: with)?|capacity of|with a|marked|showing|number)\s*(up to\s*)?$',before,re.I)
        nxt=(re.findall(r'\w+',after)[:1] or [''])[0].lower()
        if nxt in ('of','more','other','remaining','additional','different','separate','extra','new','times','at'): continue
        if pa and re.match(r'^\s*(?:[a-z]+\s+){0,3}?(?:blocks|boxes)\b',after,re.I) and v<=10: continue
        if (pa and not EXCL.search(' '.join(re.findall(r'\w+',after)[:1]))) or (colb and re.match(r'\s*(blocks?|boxes|box|and|,)',after,re.I)) or (hold and re.match(r'\s*(ants?|blocks?|and|,|\.)',after,re.I)):
            if 1<=v<=500: pods.append(v)
    return pods,ctr
rows=[]; full={}
for r in d:
    t=r['article']; tl=t.lower()
    pods,ctr=numbers(t)
    cs=colors(t)
    f=dict(level=r['level'],articleChars=len(t),nColors=len(cs),colors=';'.join(sorted(cs)),
           nPodNums=len(pods),podMin=min(pods) if pods else '',podMedian=sorted(pods)[len(pods)//2] if pods else '',podMax=max(pods) if pods else '',
           podNums=';'.join(map(str,pods)),counterNums=';'.join(map(str,ctr)),counterMax=max(ctr) if ctr else '')
    for k,p in MECH.items(): f['m_'+k]=len(re.findall(p,t,re.I))
    f['m_no_special_claim']=int(bool(re.search(r'(does not|doesn.t) (contain|have|feature|include) any special|no special mechanics|without any special',t,re.I)))
    for k,p in DIFF.items(): f['d_'+k]=len(re.findall(p,t,re.I))
    rows.append(f)
keys=list(rows[0].keys())
with open(out_csv,'w',newline='') as fh:
    w=csv.DictWriter(fh,keys); w.writeheader(); [w.writerow(x) for x in rows]
json.dump(rows,open(out_json,'w'),indent=0)
print(len(rows))

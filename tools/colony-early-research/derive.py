"""Derive our exact baseline and proposal data; competitor observations stay separate."""
import json,math,itertools
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'product/audits/colony-flow-early-2026-10-19'
def read(p):return json.loads((ROOT/p).read_text())
def write(name,value):(OUT/name).write_text(json.dumps(value,ensure_ascii=False,indent=2)+'\n')
levels=read('content/pilot-v3/levels.json')['levels']
owner=read('product/playtests/pilot-v2-owner-2026-10-19/results.json')['attempts']
baseline=[]
for l in levels:
 cells=''.join(l['rowsBottomFirst']);counts={c:cells.count(c) for c in sorted(set(cells)-set('.#'))}
 attempts=[a for a in owner if a['level']==l['id']]
 baseline.append({'level':l['id'],'grid':[l['w'],l['h']],'materialCounts':counts,'materialCount':len(counts),'workCells':sum(counts.values()),'dominantShare':round(max(counts.values())/sum(counts.values()),4),'capsules':len(l['pods']),'capacity':l.get('capacity',5),'ownerV2Attempts':len(attempts),'ownerV2ActiveSeconds':round(sum(a['elapsedMs'] for a in attempts)/1000,3),'ownerV2Notes':[a['note'] for a in attempts if a.get('note')]})
write('bloomlings-baseline.json',{'catalogVersion':'pilot-30-v3','humanTimingVersion':'pilot-30-v2','levels':baseline,'limits':['v3 has not been human-tested in the supplied records. V2 timings are not v3 timings.','V2 and v3 L1–9/L17/L25/L26 differ in board or queue. L15/L22 are retained control puzzles.']})
certs=read('content/pilot-v3/certificates.json')
examples=[]
for level in [15,22]:
 for p in next(c for c in certs if c['id']==level)['decisionProofs']:
  examples.append({'level':level,'prefix':p['prefix'],'afterActions':p['at'],'winningAlternative':p['correct'],'fatalAlternative':p['wrong'],'winningImmediateClears':p['goodClears'],'fatalImmediateClears':p['badClears'],'sharesMaterial':p['sameColourChoice'],'proofNodes':p['proofNodes']})
write('decision-examples.json',{'source':'content/pilot-v3/certificates.json; verified by the existing v3 native/JS checks','scope':'Counterfactuals in these exact prefix states only; not universal advice about a colour or bridge bank.','examples':examples})

# Proposal, not generated definitions or measured competitor timings.
rows=[
(1,16,18,3,'15–25','3 героя и 3 материала; простой цветной силуэт','Normal',8,10,'—'),
(2,18,20,3,'20–35','Bloom присоединяется; всего открыты 4 базовых цвета','Normal',9,12,'—'),
(3,20,22,4,'25–45','Жемчужный материал Drop; контрастные большие детали','Normal',10,13,'—'),
(4,24,26,6,'60–100','Тёмный контур Twig; первое богатое поле и длинная работа','Normal',12,16,'—'),
(5,24,26,6,'65–110','Первая заметная задача: внешняя область, внутренний цвет, резерв','Challenge',13,17,'—'),
(6,24,26,5,'45–75','Скрытая глубина; известный верх, фиксированное содержимое','Teach',12,16,'hidden'),
(7,24,26,6,'55–90','Закрепление скрытых; два пути раскрытия рисунка','Normal',13,17,'hidden'),
(8,24,26,5,'40–70','Одна связка одинакового материала, оба места до конца','Teach',12,16,'pair'),
(9,24,26,6,'60–100','Связка двух разных материалов и цена её ожидания','Normal',14,18,'pair'),
(10,26,28,6,'80–130','Первый кандидат Hard на знакомых цветах и связке','Hard candidate',16,20,'pair'),
(11,24,26,5,'40–75','Передышка; сливовый материал Bloom, всего 7 открытых','Normal',13,17,'—'),
(12,26,28,6,'65–105','Скрытая глубина и знакомая связка вместе','Normal',15,19,'hidden+pair'),
(13,26,28,6,'55–90','Свободный силуэт, отдельные детали и счётчики','Normal',14,18,'—'),
(14,26,28,6,'65–110','Подготовить материал перед долгой парой','Normal',16,20,'pair'),
(15,22,24,5,'Measure','Контроль: существующие поле, очередь и группы L15','Hard control',20,20,'pair'),
(16,24,26,5,'40–70','Передышка без новых оттенков и правил','Normal',12,16,'—'),
(17,26,28,6,'55–95','Читаемый сапог: узнаваемость важнее мелких деталей','Normal',15,19,'hidden'),
(18,26,28,6,'65–110','Знакомая одноцветная связка пересекает две области','Normal',16,20,'pair'),
(19,28,30,6,'70–110','Подготовка внутренней области, без мостика','Normal',16,21,'hidden+pair'),
(20,24,26,5,'45–80','Sprig впервые выращивает один мостик; простая демонстрация','Teach',12,16,'bridge'),
(21,26,28,6,'65–105','Практика выбора между двумя берегами','Normal',15,19,'bridge'),
(22,25,27,6,'Measure','Контроль: существующие поле, очередь, связки и два берега L22','Hard control',22,22,'bridge+pair'),
(23,26,28,5,'45–80','Передышка: красивый силуэт и читаемая работа','Normal',14,18,'—'),
(24,28,30,6,'75–120','Мост открывает работу ожидающей связке','Normal',17,22,'bridge+pair'),
(25,26,28,6,'80–130','Котёнок: новый кандидат Hard только после проверки','Hard candidate',18,22,'pair'),
(26,28,30,6,'60–100','Подсолнух: крупные лепестки, глубина очереди','Normal',16,20,'hidden'),
(27,28,30,6,'75–120','Второй вход и разные направления доступа, без новой механики','Normal',18,22,'pair'),
(28,26,28,5,'45–80','Передышка; никаких новых цветов','Normal',14,18,'—'),
(29,28,30,6,'90–140','Сочетание знакомых зависимостей, кандидат Hard','Hard candidate',20,24,'bridge+pair'),
(30,28,30,6,'65–110','Завершение главы с четырьмя героями; без новой механики','Normal',18,22,'hidden')]
proposal=[]
for n,w,h,col,sec,goal,tier,lo,hi,mechanics in rows:proposal.append({'level':n,'grid':[w,h],'activeMaterials':col,'unlockedMaterials':3 if n==1 else 4 if n==2 else 5 if n==3 else 6 if n<=10 else 7,'targetSeconds1x':sec,'goal':goal,'tier':tier,'capsuleRange':[lo,hi],'mechanics':mechanics,'capacity':5,'status':'proposal; not generated or certified'})
write('v4-proposal.json',{'version':'v4-plan-1','levels':proposal,'timingStatus':'Design targets for active successful attempts at explicit 1x, not competitor or human measurements; auto2x tail logged separately.','controlLevels':[15,22],'paletteLimit':7,'capacityStatus':'Five for palette/tempo experiment; v3 early 3/4 comparisons preserved separately, not an approved release unlock.'})
palette=[('G','Лист','Sprig','#5B9A43','♧'),('P','Коралловый лепесток','Bloom','#E46F95','✿'),('B','Лазурь','Drop','#36B7DA','◆'),('A','Янтарь','Twig','#C4873E','●'),('C','Жемчуг','Drop','#F7E8BB','◇'),('D','Тёмная земля','Twig','#334B48','▰'),('V','Слива','Bloom','#8056AE','✦')]
matrices={'normal':[[1,0,0],[0,1,0],[0,0,1]],'protan':[[.152286,1.052583,-.204868],[.114503,.786281,.099216],[-.003882,-.048116,1.051998]],'deutan':[[.367322,.860646,-.227968],[.280085,.672501,.047413],[-.011820,.042940,.968881]],'tritan':[[1.255528,-.076749,-.178779],[-.078411,.930809,.147602],[.004733,.691367,.303900]]}
def linear(s):return s/12.92 if s<=.04045 else ((s+.055)/1.055)**2.4
def gamma(s):return s*12.92 if s<=.0031308 else 1.055*s**(1/2.4)-.055
def rgb(hex):return [linear(int(hex[i:i+2],16)/255) for i in [1,3,5]]
def transform(c,m):return [max(0,min(1,sum(a*b for a,b in zip(row,c)))) for row in m]
def lab(c):
 x,y,z=[sum(a*b for a,b in zip(row,c))/white for row,white in zip([[.4124564,.3575761,.1804375],[.2126729,.7151522,.072175],[.0193339,.119192,.9503041]],[.95047,1,1.08883])]
 f=lambda t:t**(1/3) if t>(6/29)**3 else t/(3*(6/29)**2)+4/29
 x,y,z=map(f,[x,y,z]);return [116*y-16,500*(x-y),200*(y-z)]
colours=[]
for key,name,hero,hex,mark in palette:
 c=rgb(hex);lum=sum(a*b for a,b in zip([.2126,.7152,.0722],c));black=(lum+.05)/.05;white=1.05/(lum+.05)
 variants={mode:'#'+''.join(f'{round(gamma(v)*255):02X}' for v in transform(c,m)) for mode,m in matrices.items()}
 colours.append({'id':key,'name':name,'family':hero,'hex':hex,'symbol':mark,'text':'#000000' if black>=white else '#FFFFFF','textContrast':round(max(black,white),2),'views':variants})
pairs={}
for mode,m in matrices.items():
 pairs[mode]=sorted([{'pair':[a[0],b[0]],'deltaE76':round(math.dist(lab(transform(rgb(a[3]),m)),lab(transform(rgb(b[3]),m))),2)} for a,b in itertools.combinations(palette,2)],key=lambda p:p['deltaE76'])
write('palette-audit.json',{'palette':colours,'pairs':pairs,'model':'sRGB linear transform; Machado 2009 severity-100 matrices; CIELAB D65 Euclidean deltaE76 (not CIEDE2000).','limits':['Diagnostic approximation, not clinical simulation or an accessibility certification.','Numeric colour separation is not proof of legibility on phones or under colour-vision deficiency.','Hero portrait and material symbol on capsules remain; optional symbols/patterns on tiles are required for ambiguous pairs.'],'minimumNormalDeltaE76':pairs['normal'][0],'cvdRiskPairsUnder20':{k:[p for p in v if p['deltaE76']<20] for k,v in pairs.items() if k!='normal'}})
print('Baseline, 30-row proposal and 7-material palette diagnostics written.')
print('Palette closest pairs',[(m,x[0]) for m,x in pairs.items()])

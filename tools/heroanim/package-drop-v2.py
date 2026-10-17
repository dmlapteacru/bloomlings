"""One owner ZIP, with final FBXs, importer, evidence and Russian instructions; no second .blend."""
import json,shutil,hashlib,zipfile
from pathlib import Path
from PIL import Image,ImageDraw
HERE=Path(__file__).resolve().parent;ROOT=HERE.parent.parent/'content/work/drop-v2';MODEL=HERE/'models/drop-v2';OUT=ROOT/'handoff';PRODUCT=MODEL/'products';PRODUCT.mkdir(exist_ok=True);(OUT/'previews').mkdir(exist_ok=True)
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
validation=json.loads((ROOT/'validation-report.json').read_text());smoke=json.loads((ROOT/'import-report.json').read_text());assert validation['complete'] and smoke['complete'] and smoke['actions']==5
inventory=json.loads((OUT/'inventory.json').read_text());assert len(inventory['files'])==17 and len(inventory['animationNames'])==5
for entry in inventory['files']:assert sha(OUT/entry['path'])==entry['sha256']
shutil.copyfile(HERE/'import-drop-v2.py',OUT/'Import_Drop.py')
text='''# Drop V2 — финальный набор FBX

Распакуй ZIP полностью. В своём основном .blend открой в Text Editor файл Import_Drop.py из этой папки и нажми Run Script. Проверено в Blender 4.3.2. Второй .blend не нужен.

Будут импортированы один общий риг, базовый герой, все вещи и пять анимаций. Текстуры упаковываются в твой .blend. Скрипт добавляет коллекцию DropV2_Assembly и не перезаписывает другие герои. Если она уже существует, повторный импорт останавливается.

Выбери DropHeroV2Rig > Object Properties > Custom Properties:

head_style:
0 — родная цельная голова-капля (дефолт);
1 — боковая волна;
2 — парные завитки;
3 — водоворот;
4 — панама;
5 — кепка;
6 — берет;
7 — зимние наушники с волосами;
8 — новогодняя шапка с волосами.

body_style: 0 без одежды, 1 зимняя куртка, 2 осеннее пальто, 3 комбинезон.
scarf_style: 0 без шарфа, 1 объёмный вязаный, 2 компактный полосатый.
glasses: 0 выключены, 1 включены.

Посадка шарфа поверх выбранной одежды переключается автоматически. Одновременно виден один предмет на слот. Лысая округлая Base_Head скрывается только при выборе цельной NativeDrop; под другими уборами лицо остаётся общим.

Всего 15 вещей: 9 Head + 1 Face + 3 Body + 2 Accessories. Ничего из согласованного списка не отсутствует. Очки — согласованная готовая оправа Twig, с кремовым материалом. Отдельная съёмная капля не нужна: NativeDrop заменяет всю голову. Добавлена небольшая шея для стыка головы с телом; исходные вершины базы не растягивались.

Анимации выбираются в Dope Sheet > Action Editor, 24 fps:
Drop_Idle 1–73; Drop_Walk 1–49 (на месте); Drop_Wave 1–97;
Drop_Joy 1–73; Drop_Sad 1–97.
Пять спокойных движений тела, без мимики, отдельных пальцев и симуляции ткани. Хлопки исключены: при сведении коротких рук сгенерированная сетка рукавов слишком растягивается. Для них нужна отдельная доработка плеч/рукавов.

В папке FBX — 17 файлов: базовый герой, 15 вещей, отдельный @Anims со всеми пятью действиями. При ручном импорте каждый FBX создаёт одинаковый скелет: переназначь Armature-модификатор на общий риг и удали дубликаты скелетов. Для удобства лучше использовать Import_Drop.py. Не центрируй и не масштабируй вещи по отдельности — они уже находятся в общих координатах.

Проверки: 75 поз тела на всех 23 мешах, веса нормализованы (не более 4 костей на вершину), головы жёсткие. Все 17 FBX повторно импортированы; проверены скелет, UV, встроенные albedo-текстуры, shape keys шарфов и 15 поз анимации. Проверен сам скрипт импорта и переключатели вещей. Детали в validation.json и import-check.json. Превью сняты с настоящих экспортированных моделей.
'''
(OUT/'README_RU.md').write_text(text,encoding='utf-8')
shutil.copyfile(ROOT/'validation-report.json',OUT/'validation.json');shutil.copyfile(ROOT/'import-report.json',OUT/'import-check.json')
def sheet(files,dest,columns):
 cellw,cellh=240,340;im=Image.new('RGB',(columns*cellw,((len(files)+columns-1)//columns)*cellh),'#f4f0e6');draw=ImageDraw.Draw(im)
 for i,(path,label) in enumerate(files):
  assert path.exists(),path
  pic=Image.open(path);pic.thumbnail((cellw,315));x=i%columns*cellw;y=i//columns*cellh;im.paste(pic,(x,y));draw.text((x+6,y+318),label,fill='#253f51')
 im.save(dest,quality=92)
heads=['NativeDrop','HairSideWave','HairTwinRipples','HairWhirlpool','BucketHat','Cap','Beret','EarmuffHair','XmasHat'];recipes=heads+[b+'-'+s for b in ['Jacket','Coat','Overalls'] for s in ['none','ScarfKnit','ScarfStripe']]
sheet([(ROOT/'fit-review'/(n+'-90.png'),n) for n in recipes],OUT/'previews/looks.jpg',6)
clips=['Idle','Walk','Wave','Joy','Sad'];sheet([(ROOT/'motion-review'/(c+'-'+b+'-0.45.png'),c+' / '+b) for b in ['Jacket','Coat','Overalls'] for c in clips],OUT/'previews/motions.jpg',5)
files=[OUT/e['path'] for e in inventory['files']]+[OUT/f for f in ['Import_Drop.py','README_RU.md','inventory.json','validation.json','import-check.json','previews/looks.jpg','previews/motions.jpg']]
if (ROOT/'motions.mp4').exists():shutil.copyfile(ROOT/'motions.mp4',OUT/'previews/motions.mp4');files.append(OUT/'previews/motions.mp4')
zip_path=PRODUCT/'DropHeroV2_FBX.zip'
with zipfile.ZipFile(zip_path,'w',compression=zipfile.ZIP_DEFLATED,compresslevel=6) as z:
 for p in files:z.write(p,'DropHeroV2/'+str(p.relative_to(OUT)))
shutil.copyfile(OUT/'validation.json',MODEL/'validation.json');shutil.copyfile(OUT/'import-check.json',MODEL/'import-check.json');shutil.copyfile(OUT/'inventory.json',MODEL/'inventory.json');shutil.copyfile(OUT/'previews/looks.jpg',MODEL/'looks.jpg');shutil.copyfile(OUT/'previews/motions.jpg',MODEL/'motions.jpg')
source=json.loads((MODEL/'source-manifest.json').read_text());pins=[MODEL/e['path'] for e in source['files']]+[MODEL/'source-manifest.json',MODEL/'README.md',MODEL/'validation.json',MODEL/'import-check.json',MODEL/'inventory.json',MODEL/'looks.jpg',MODEL/'motions.jpg',zip_path]+[HERE/n for n in ['drop_v2_features.py','prepare-drop-v2.py','rig-drop-v2.py','export-drop-v2.py','validate-drop-v2.py','import-drop-v2.py','test-drop-v2-import.py','review-drop-v2.mjs','encode-drop-v2-video.py','package-drop-v2.py']]
import os
record={'version':1,'purpose':'Offline Drop FBX handoff; no runtime game changes','items':{'Head':9,'Face':1,'Body':3,'Accessories':2},'clips':5,'posesChecked':75,'roundtripPoses':15,'files':[{'path':os.path.relpath(p,MODEL),'sha256':sha(p)} for p in pins]}
(MODEL/'manifest.json').write_text(json.dumps(record,indent=2)+'\n')
print('PACKAGED',zip_path,zip_path.stat().st_size,'bytes',flush=True)

"""Publish checked per-family ZIPs; reassemble one complete owner ZIP using Python only."""
import argparse
import hashlib
import json
import zipfile
from pathlib import Path

HERE=Path(__file__).resolve().parent
REPO=HERE.parents[2]
WORK=REPO/'content/work/rigging'
PRODUCTS=REPO/'tools/heroanim/products/rig-fix-v1'
FAMILIES=('twig','sprig','bloom','drop')
SHA=lambda file:hashlib.sha256(Path(file).read_bytes()).hexdigest()
TEXT='''Bloomlings — исправленные риги V1

1. Распакуй весь ZIP в одну папку.
2. В своём Blender открой Import_Rigged_Heroes.py через Text Editor → Open → Run Script.
3. Появится новая коллекция Bloomlings_RigFix_v1. Старые объекты скрипт не удаляет.
   Для сравнения скрой прежнюю коллекцию героя.
4. В окне 3D нажми N → Bloomlings. Выбери героя, Head, Body, Neck, Glasses и действие.
5. Обычный режим проигрывает запечённые переносимые анимации. Editable leg IK включает
   восстановленные контроллеры ног для редактирования новых движений.

Внутри: четыре героя, все 55 предметов, 63 FBX, 24 действия (по 6 на героя):
Idle / Walk / Wave / Joy / Sad / Curiosity. Исходная частота — 24 fps;
импортёр подстраивает Actions к FPS твоего проекта. Default Head всегда надет.

Лица, тела, UV героев, головные уборы, очки и fit-морфы шарфов сохранены.
У одежды исправлены веса и проблемные стыки: новые ровные рукава/манжеты,
внутренняя ткань и опорные кости. Это анимация тела; новая лицевая мимика,
отдельные пальцы и симуляция ткани в этот пакет не входят.

Добавляй результат в тот же мастер .blend. Генерируемые служебные .blend не нужны.
Повторный запуск импортёра переиспользует уже добавленные риги и возвращает панель.
Проверки экспорта и повторного импорта выполнены в Blender 4.3.2; импортёр также
учитывает Action Slots в Blender 4.4+ / 5.x. Существующие объекты и FPS сохраняются.

Игра продолжает использовать прежнюю проверенную 2D-библиотеку. Этот пакет —
основа для последующего запекания новой анимации в игровые спрайты.
'''


def write(z,name,data):
    info=zipfile.ZipInfo(name,date_time=(2026,10,18,0,0,0))
    info.compress_type=zipfile.ZIP_DEFLATED
    info.external_attr=0o100644<<16
    z.writestr(info,data,compress_type=zipfile.ZIP_DEFLATED,compresslevel=9)


def publish():
    PRODUCTS.mkdir(parents=True,exist_ok=True)
    manifest={'version':1,'package':'Bloomlings_RigFix_v1','families':{},'tools':{},'files':[],'inputs':{}}
    owner=json.loads((WORK/'owner-import-check.json').read_text())
    assert owner.get('complete') and owner['importerSha256']==SHA(HERE/'Import_Rigged_Heroes.py'),'Owner import validation incomplete/stale'
    manifest['ownerImportCheck']=owner
    for family in FAMILIES:
        root=WORK/family
        rig=json.loads((root/'rig-report.json').read_text())
        assessment=json.loads((root/'assessment.json').read_text())
        roundtrip=json.loads((root/'roundtrip.json').read_text())
        inventory=json.loads((root/'handoff/inventory.json').read_text())
        assert owner['inventories'][family]==SHA(root/'handoff/inventory.json'),family+' owner test stale'
        manifest['inputs'][family]=json.loads((root/'assembly.json').read_text())['sources']
        for entry in manifest['inputs'][family]:
            assert SHA(REPO/entry['path'])==entry['sha256'],entry['path']+' changed'
        assert assessment.get('qualityGatePassed') and assessment.get('complete'),family+' assessment incomplete'
        assert roundtrip.get('complete') and len(roundtrip['samples'])==18,family+' FBX roundtrip incomplete'
        assert assessment['rigReportSha256']==roundtrip['rigReportSha256']==SHA(root/'rig-report.json'),family+' stale rig validation'
        assert roundtrip['inventorySha256']==SHA(root/'handoff/inventory.json'),family+' stale FBX validation'
        for name,digest in rig['tools'].items():
            assert SHA(HERE/name)==digest,(family,name,'stale build')
        summary={'bones':roundtrip['boneCount'],'parts':len(rig['parts']),'fbxFiles':len(inventory['files']),
                 'clips':inventory['clips'],'restMaxError':assessment['restMaxError'],'uvMaxError':assessment['uvMaxError'],
                 'panelMaxDrift':assessment['protectedPanelMaxDrift'],'loopMaxErrors':assessment['loopMaxErrors'],
                 'roundtripMaxMotionError':roundtrip['maxMotionError'],'sampledPoses':len(assessment['measurements']['after']),
                 'roundtripPoses':len(roundtrip['samples']),'complete':True}
        archive=PRODUCTS/(family+'-rigged-v1.zip')
        with zipfile.ZipFile(archive,'w') as z:
            for entry in inventory['files']:
                file=root/'handoff'/entry['path']
                assert SHA(file)==entry['sha256']
                write(z,family+'/'+entry['path'],file.read_bytes())
            write(z,family+'/inventory.json',(root/'handoff/inventory.json').read_bytes())
            write(z,family+'/validation.json',(json.dumps(summary,indent=2)+'\n').encode())
        assert archive.stat().st_size<95*2**20,'Split artifact exceeds the Git file limit'
        manifest['files'].append({'path':archive.name,'sha256':SHA(archive),'bytes':archive.stat().st_size})
        manifest['families'][family]=summary
    for file in sorted(HERE.glob('*')):
        if file.is_file():
            manifest['tools'][file.name]=SHA(file)
    (PRODUCTS/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
    print('Published 4 validated family ZIPs')


def assemble():
    manifest=json.loads((PRODUCTS/'manifest.json').read_text())
    for file in manifest['files']:
        assert SHA(PRODUCTS/file['path'])==file['sha256'],file['path']+' changed'
    assert SHA(HERE/'Import_Rigged_Heroes.py')==manifest['tools']['Import_Rigged_Heroes.py'],'Importer changed after validation'
    folder=WORK/'owner-package'
    folder.mkdir(parents=True,exist_ok=True)
    archive=WORK/'Bloomlings_RigFix_v1.zip'
    with zipfile.ZipFile(archive,'w') as z:
        for file in manifest['files']:
            with zipfile.ZipFile(PRODUCTS/file['path']) as source:
                for entry in source.infolist():
                    target=(folder/entry.filename).resolve()
                    assert target.is_relative_to(folder.resolve())
                    data=source.read(entry)
                    target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(data)
                    write(z,'Bloomlings_RigFix_v1/'+entry.filename,data)
        for name,data in [('Import_Rigged_Heroes.py',(HERE/'Import_Rigged_Heroes.py').read_bytes()),('README_RU.txt',TEXT.encode())]:
            (folder/name).write_bytes(data);write(z,'Bloomlings_RigFix_v1/'+name,data)
    print(str(archive.relative_to(REPO)),round(archive.stat().st_size/2**20,1),'MiB')


if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--publish',action='store_true',help='Require fresh validation and publish family archives')
    args=parser.parse_args()
    if args.publish:
        publish()
    assemble()

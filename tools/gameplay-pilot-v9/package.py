#!/usr/bin/env python3
"""Package the self-contained pilot for phone download; no APK or production install."""
from pathlib import Path
from zipfile import ZipFile, ZipInfo, ZIP_DEFLATED
from hashlib import sha256

root = Path(__file__).resolve().parents[2]
folder = root / 'product/previews/gameplay-pilot-v9'
page = (folder / 'index.html').read_bytes()
readme = '''Bloomlings v9 — сводная итерация всех 30 уровней

Распакуй ZIP и открой index.html в браузере телефона.
Сеть не нужна. Все 30 уровней доступны в меню.
Все 30 уровней проверены. Изменения относительно первой v9 отмечены в меню.
V8 сохранена без изменений. Первоначальная v9 сохранена отдельно как архив.
Это сводная версия: свежая база v8E + проверенные улучшения v9.

После игры: Уровни → Результаты → скачать JSON.
Пожалуйста, оцени также длинные паузы без выбора.
Это отдельный пилот, он не заменяет игру или предыдущие версии.
'''.encode('utf-8')
archive = folder / 'Bloomlings-pilot-v9.zip'
with ZipFile(archive, 'w', compression=ZIP_DEFLATED, compresslevel=9) as z:
    for name, body in [('index.html', page), ('README.txt', readme)]:
        info = ZipInfo(name, (2026, 10, 24, 0, 0, 0))
        info.compress_type = ZIP_DEFLATED
        info.external_attr = 0o644 << 16
        z.writestr(info, body)
with ZipFile(archive) as z:
    assert z.testzip() is None
    assert z.read('index.html') == page
print(archive.relative_to(root), archive.stat().st_size, sha256(archive.read_bytes()).hexdigest())

# Bloomlings v7 — более узнаваемые картинки

Открыть `index.html` в браузере телефона. Все ресурсы встроены; сеть не нужна. Новые рисунки и очереди в L1–9, остальные 21 уровень равны v6. Правила, скорость, размеры бордов и появления механик сохранены.

Начать на 1×. После прохождения оценить узнаваемость и скачать JSON через меню результатов. У v7 отдельное сохранение. Основной UI игры и производственный каталог не затрагиваются.

План: `product/plans/gameplay-pilot-v7-plan.md`.

Из корня репозитория:

```sh
node tools/gameplay-pilot-v7/generate.mjs
node tools/gameplay-pilot-v7/verify.mjs
node tools/gameplay-pilot-v7/clock-check.mjs
python3 tools/gameplay-pilot-v7/build.py
node tools/gameplay-pilot-v7/browser-smoke.mjs
node tools/gameplay-pilot-v7/browser-check.mjs
node tools/gameplay-pilot-v7/storage-check.mjs
python3 tools/gameplay-pilot-v7/contact-sheet.py
python3 tools/gameplay-pilot-v7/compare-art.py
```

Node, Python/Pillow, Chromium `/usr/bin/chromium`, установленный `tools/heroanim/node_modules/playwright-core`. Браузерные проверки работают через временный loopback HTTP. Постоянного сервера не требуется.

`generate.mjs` пересоздаёт только пилот v7. Победные сертификаты доказывают прохождение с ожиданием окончания работы между решениями; произвольные времена параллельного ввода могут менять исход. Уровни L10–30 сохраняют исходные данные v6; это отдельно проверяется. Автоматические времена и доли побед не заменяют отзыв игрока.

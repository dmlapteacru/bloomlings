# Bloomlings v8 — 30 уровней

Открыть `index.html` в браузере телефона. Всё встроено, сеть не нужна. Жёлтый и оранжевый материалы; закрытые капсулы раскрываются после отправки; новая пчела и лейка; пересобраны 17 очередей. Скорость прежняя. L10/L22 сохранены.

Начать с 1×. Все уровни доступны через меню. Оценить различимость новых цветов, рисунки и влияние закрытых капсул. После теста скачать JSON через результаты. APK не нужен.

План и ограничения проверки честности: `product/plans/gameplay-pilot-v8-plan.md`.

Из корня репозитория:

```sh
node tools/gameplay-pilot-v8/generate.mjs
node tools/gameplay-pilot-v8/verify.mjs
node tools/gameplay-pilot-v8/control-check.mjs
node tools/gameplay-pilot-v8/clock-check.mjs
python3 tools/gameplay-pilot-v8/build.py
node tools/gameplay-pilot-v8/browser-smoke.mjs
node tools/gameplay-pilot-v8/browser-check.mjs
node tools/gameplay-pilot-v8/storage-check.mjs
python3 tools/gameplay-pilot-v8/contact-sheet.py
python3 tools/gameplay-pilot-v8/compare-art.py
```

Нужны Node, Python/Pillow, Chromium `/usr/bin/chromium` и установленный `tools/heroanim/node_modules/playwright-core`. Браузерные проверки поднимают и закрывают временный loopback HTTP. Производственный каталог не затрагивается.

Общая стратегия проверяется для обеих перестановок двух равных по объёму скрытых лотов, с ожиданием окончания работы между решениями. Это не обещание победы после произвольных действий. Простые стратегии — диагностика авторинга, не статистика игроков. Красный и глубокий синий оставлены на будущий тест.

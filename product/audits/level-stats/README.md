## Отчёт

`report.html` — один файл без сети и сервера: открыть в браузере на компьютере или телефоне. Пересборка:

```
node tools/level-stats/assemble.mjs   # out/levels/*.json → out/data.json, levels.csv
node tools/level-stats/report.mjs     # out/data.json (+ conclusions.md, unknowns.md) → report.html
```

Вкладки: Обзор (фильтры, таблица, CSV, ключевые графики), Уровень, Стратегии, Сравнение (CF / v8E / v9), Похожесть, Время,
Полнота, Выводы (`conclusions.md` и `unknowns.md`, если они есть). У каждого числа — метка источника (раскладка, запись,
повтор, модель, стратегия); «неизвестно» не равно нулю. Каждый график сохраняется в SVG или PNG.

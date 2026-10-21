"""Small standalone, phone-readable owner review and proposed colour swatches."""
import json
from html import escape
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'product/playtests/pilot-v7-owner-2026-10-21'
r = json.loads((OUT / 'review.json').read_text())
s = r['summary']
palette = [
    ('Sprig', [('Лист', '#78C04A')], ('Жёлтый', '#F4C744'), 'Подсолнух, пчела, лимон. Предлагаю добавить первым.'),
    ('Twig', [('Янтарь', '#A45F28'), ('Земля', '#334B48')], ('Оранжевый', '#EF8A32'), 'Тыква, морковь, лиса. Второй новый материал для следующего теста.'),
    ('Bloom', [('Лепесток', '#EB799C'), ('Слива', '#7851A5')], ('Красный', '#D84B43'), 'Мухомор, ягоды, божья коровка. Кандидат на последующее расширение.'),
    ('Drop', [('Вода', '#36B7DA'), ('Жемчуг', '#F5E6BD')], ('Синий', '#2754B4'), 'Кит, зонтик, ночные сюжеты. Проверить различимость с фиолетовым.'),
]
cards = []
for hero, old, new, why in palette:
    swatches = ''.join(f'<div class="swatch"><i style="background:{color}"></i><span>{escape(name)}</span><small>Сейчас</small></div>' for name, color in old)
    swatches += f'<div class="swatch candidate"><i style="background:{new[1]}"></i><span>{escape(new[0])}</span><small>Кандидат</small></div>'
    cards.append(f'<article><h3>{hero}</h3><div class="swatches">{swatches}</div><p>{why}</p></article>')
hidden = ''.join(f'<tr><td>L{x["level"]}</td><td>{100*x["hiddenWork"]/x["totalWork"]:.1f}%</td><td>{" / ".join(str(round(e["clearedFraction"]*100))+"%" for e in x["reveals"])}</td></tr>' for x in r['hiddenLevels'])
notes = ''.join(f'<article><h3>L{a["level"]}</h3><blockquote>{escape(a["note"])}</blockquote></article>' for a in r['attempts'] if a['note'])
html = f'''<!doctype html><html lang="ru"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Bloomlings — разбор v7 и предложение v8</title>
<style>*{{box-sizing:border-box}}body{{margin:0;background:#f4f2e8;color:#293a32;font:17px/1.55 system-ui,sans-serif}}main{{max-width:980px;margin:auto;padding:24px 16px 64px}}h1{{font-size:clamp(27px,5vw,42px);line-height:1.15}}h2{{margin-top:36px;font-size:25px}}h3{{margin:0 0 12px}}p{{margin:12px 0}}.tag{{font-size:13px;font-weight:700;letter-spacing:.06em;color:#596b5d}}.grid{{display:grid;grid-template-columns:repeat(auto-fit,minmax(min(280px,100%),1fr));gap:14px}}article,.callout{{padding:20px;border-radius:18px;background:#fffef8;border:1px solid #dbe1d2}}.callout{{background:#e7eddc}}.stats{{display:grid;grid-template-columns:repeat(2,1fr);gap:12px}}.stats strong{{display:block;font-size:30px}}.swatches{{display:flex;gap:12px;flex-wrap:wrap}}.swatch{{flex:1;min-width:72px;display:grid;gap:5px;text-align:center;font-size:14px}}.swatch i{{height:72px;border-radius:12px;border:1px solid #0002}}small{{color:#667064}}.candidate i{{outline:2px dashed #6a7865;outline-offset:3px}}table{{border-collapse:collapse;width:100%;font-size:15px;background:#fffef8}}th,td{{text-align:left;padding:10px 8px;border-bottom:1px solid #dbe1d2}}blockquote{{margin:0;font-size:16px}}li{{margin:12px 0}}code{{overflow-wrap:anywhere}}.muted{{font-size:14px;color:#667064}}</style>
<main><p class="tag">BLOOMLINGS · ОТЧЁТ 21 ОКТЯБРЯ · ПРЕДЛОЖЕНИЕ, НЕ БИЛД V8</p><h1>Цветов можно больше.<br>Неизвестность должна влиять на выбор.</h1>
<p>Отчёт v7 проверен воспроизведением действий. 41 попытка: 30 побед, 8 поражений, 3 прерванных повтора. Прошли проверку все 611 принятых действий и 20 154 рейса.</p>
<div class="stats"><article><strong>29 / 30</strong>картинок узнаются</article><article><strong>30 / 30</strong>оценок темпа «хорошо»</article><article><strong>16 / 30</strong>«думал об очереди»</article><article><strong>25 / 30</strong>побед с первой попытки</article></div>
<p>Интересно: L10, L20, L22, L29. Скучно: L8, L16, L23, L26, L28. Единственный явно неудачный рисунок — торт L15. Лейка L17 тоже требует правки: похожа на чайник.</p>
<div class="callout"><strong>Что уже работает</strong><p>На L10 было полезно отправить один розовый лот и дать ему закончить вместо одновременного запуска двух. Сохраняем эту конкуренцию за доступные клетки и текущую скорость. L10 и L22 — контрольные задачи.</p></div>
<h2>Набор цветов: 7 → 9 → до 11</h2><p>По одному новому материалу каждому герою — 11 всего. Сначала предлагаю добавить жёлтый и оранжевый: они дают больше новых узнаваемых сюжетов. Остальные два — после проверки на телефоне. На отдельной картинке используем только нужные цвета, а не всю палитру сразу.</p><div class="grid">{''.join(cards)}</div>
<p class="muted">Это образцы-кандидаты. В игру они ещё не добавлены. Каждый материал сохраняет отдельный символ. Красный/розовый и синий/фиолетовый нужно отдельно проверить на маленьких клетках и при нарушениях цветового зрения.</p>
<h2>Почему «?» почти незаметны</h2><p>Сейчас цвет скрыт только в глубине очереди. Наверху он открывается до выбора: неизвестный лот нельзя отправить, не узнав цвет. Всего два маленьких скрытых лота на каждом из восьми уровней; на Hard их нет. Все восемь пройдены с первой попытки.</p>
<table><thead><tr><th>Уровень</th><th>Доля работы «?»</th><th>Очищено при раскрытии</th></tr></thead><tbody>{hidden}</tbody></table>
<p>Это не только позднее раскрытие: на L17 оба цвета открылись до очистки первой клетки. Важно, какую цену имеет раскрытие и нужно ли оставлять под него место.</p>
<h2>Предлагаемый новый вариант</h2><ol><li>Закрытая капсула остаётся «?» даже наверху. Число и связка видны, цвет открывается после отправки на место.</li><li>Первое знакомство — безопасное. Затем важно заранее сохранить резерв, потому что часть работы нового цвета может быть глубоко.</li><li>Содержимое фиксировано при повторе. До раскрытия должна существовать безопасная общая стратегия для допустимых вариантов: проигрыш от обязательного угадывания не принимаем.</li><li>На L25 попробовать скрытый цвет одного участника связки. L10/L22 пока сохранить.</li></ol>
<p class="muted">Это отдельная предлагаемая механика. До выпуска нужны изменения решателя, UI и проверки честности; в v7 такого поведения нет.</p>
<h2>Следующие правки по приоритету</h2><div class="callout"><p><strong>1.</strong> Добавить жёлтый и оранжевый там, где они помогают предмету читаться.</p><p><strong>2.</strong> Заменить торт L15, например пчёлкой; изменить форму лейки L17.</p><p><strong>3.</strong> Перестроить скучные L8/16/23/26/28 и усилить L25.</p><p><strong>4.</strong> Проверить содержательную неизвестность. Скорость глобально не менять.</p></div>
<h2>Твои заметки</h2><div class="grid">{notes}</div>
<p class="muted">Один опытный игрок, а не статистика аудитории. Записано 51:04 на все попытки; медиана победной — 81 с. Время смешивает 1× и 2×, с L14 ручной 2× остаётся включённым. Названия картинок видны в игре, поэтому узнаваемость не измерена слепым тестом. Полный исходник в репозитории сохранён без изменений.</p></main></html>'''
(OUT / 'index.html').write_text(html)
shared = Path('/workspace/shared')
shared.mkdir(exist_ok=True)
(shared / 'Bloomlings-v7-review-v8-proposal.html').write_text(html)
print('Built standalone owner review and proposed palette.')

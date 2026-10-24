# Сводная v9: проверка всех 30 уровней

V8 не изменена. База — сохранённая копия v8E 51c7bd76; первая v9 доступна в `gameplay-pilot-v9-initial`. Это отдельный браузерный эксперимент.

Ниже отмечено, что перестроено и что сохранено после проверки. «Риск / узкие» — решения на исходном маршруте планировщика, видящего три ряда: есть проигрышная альтернатива / остаётся только один выигрышный ход в модели с ожиданием. Это не доля побед человека.

| L | Что сделано | Риск / узкие | Опора Colony |
|---|---|---|---|
| 1 · Ёлочка | Сохранён ясный ранний силуэт | 0 / 0 | L01 |
| 2 · Цветок | Сохранены крупные лепестки и центр | 0 / 0 | L02 |
| 3 · Мороженое | Сохранено читаемое мороженое | 0 / 0 | L03 |
| 4 · Бабочка | Принята отдельная бабочка из первой v9 | 0 / 0 | L04 |
| 5 · Жёлудь Twig | Сохранено знакомство с объёмом команды | 0 / 0 | L05 |
| 6 · Грибок | Сохранена передышка и первое раскрытие ? | 0 / 0 | L04 |
| 7 · Клубника на скатерти | Принята новая очередь v8E | 0 / 0 | L04 |
| 8 · Чашка на завтраке | Принята новая обучающая очередь v8E; проверен быстрый маршрут | 2 / 2 | L20 |
| 9 · Пончик | Сохранены последовательные цветовые кольца | 1 / 1 | L05 |
| 10 · Золотая рыбка | Приняты умеренные лоты и пять ? из первой v9 | 8 / 3 | L10 |
| 11 · Виноград | Принята обновлённая очередь винограда | 0 / 0 | L03 |
| 12 · Сова при луне | Замкнут контур совы, пересобрана очередь | 2 / 2 | L25 |
| 13 · Зонтик в луже | Сохранены крупный купол/крюк, добавлено решение | 1 / 1 | L25 |
| 14 · Чайник с узором | Сохранена задача с несколькими фронтами | 3 / 3 | L04 |
| 15 · Пчёлка над цветком | Сохранён сильный человеческий ориентир | 9 / 4 | L25 |
| 16 · Воздушный змей | Крупный змей с цельными цветными панелями | 0 / 0 | L02 |
| 17 · Садовая лейка | Пересобраны квоты и очередь лейки | 2 / 2 | L26 |
| 18 · Грибной домик | Пересобраны крыша, стены и дверь по фронтам | 3 / 3 | L04 |
| 19 · Цветок на окне | Сокращён срок удержания остатка | 3 / 2 | L05 |
| 20 · Тыква у ручья | Сохранён заработанный переход через ручей | 3 / 1 | L10 |
| 21 · Груша у ручья | Сохранена понятная развилка берега | 1 / 1 | L04 |
| 22 · Домик у ручья | Сохранён сильный Super Hard | 5 / 3 | L20 |
| 23 · Воздушный шар | Сохранена передышка с крупными панелями | 1 / 0 | L02 |
| 24 · Улитка у ручья | Принята новая очередь улитки | 2 / 0 | L05,L10 |
| 25 · Котёнок с клубком | Пересобран Hard с проверкой узких решений | 5 / 5 | L25 |
| 26 · Подсолнух у забора | Пересобраны квоты/очередь подсолнуха | 1 / 0 | L26 |
| 27 · Кит с садом | Сохранён сильный человеческий ориентир | 4 / 3 | L20 |
| 28 · Яблоко на столе | Пересобраны порции тела и бликов | 0 / 0 | L03 |
| 29 · Садовый фонарь | Пересобран Super Hard и фоновая квота | 8 / 4 | L20 |
| 30 · Зайчик с морковкой | Сохранён спокойный читаемый финал | 0 / 0 | L03,L10 |

## Правила построения, взятые из наблюдений Colony

- **Сначала доступный объём, потом квота.** На CF L1 снаружи сначала только оранжевый; на L20 часть фонового цвета остаётся внутри. Сравниваем число на капсуле с реальной доступной областью, а не с общим числом клеток этого цвета.
- **Остаток должен иметь понятное освобождение.** CF L5 показывает вложение занятого места ради внутреннего материала; L25 показывает и обратную сторону — почти весь раунд удерживаются два места. Оставляем зависимость, сокращаем расстояние до освобождающего цвета.
- **Связка — обязательство по двум фронтам.** Одна готовая половина не освобождает место. Важны положение напарника, доступ к обоим материалам и возможность провести нужную команду через оставшееся место.
- **Заполненные места могут означать хороший поток.** На CF L4 пять мест продолжают работать. Отличаем это от долгой фазы, где из пяти работают только одно-два; отдельно считаем добровольно оставленное свободное место.
- **Трудность должна менять правильный выбор.** CF L10: оранжевый открывает нужный белый, а розовый ведёт к затору. В наших Hard проверяем альтернативы и распределение решений по ходу очистки; один проигрышный первый тап не делает весь раунд сложным.
- **Крупные массы и умеренные порции.** Контур → тело → акценты даёт читаемую картинку и последовательность фронтов. CF L20 использует в основном порции около 30, а не один огромный фон. Числа не копируем без учёта нашего размера поля и скорости.

## Что именно переносим в каждый уровень

- **L1:** L01: O42 → Y45 → W12; only orange initially exposed. Проверка конструкции: Clear outside material, then interior accents; recognizable silhouette.
- **L2:** L02: pink111 and cyan112 outside, yellow25 inside. Проверка конструкции: Broad early petal/stem work and later center payoff.
- **L3:** L03: dark54 outline before body/highlights. Проверка конструкции: Recognizable outline-to-body transition.
- **L4:** L04: large roof/walls/door regions. Проверка конструкции: Distinct wing masses and interior markings; moderate onboarding workload.
- **L5:** L05: outer purple45 below initially blocked dark25. Проверка конструкции: One visible parked investment should expose productive work.
- **L6:** L04: red15 starts late; peach spots finish later. Проверка конструкции: Short body/spot residue with prompt release.
- **L7:** L04: peach44 spots embedded in red roof. Проверка конструкции: Small seed/highlight remainders release before final cleanup.
- **L8:** L20: paired background lots have available work. Проверка конструкции: Same-color pair introduces two-place commitment without near-round-long stall.
- **L9:** L05: successive colored rings. Проверка конструкции: Outer icing, body and hole establish the next useful material.
- **L10:** L10: tap16 orange30 opens white49; pink25 jams. Проверка конструкции: Reveal changes the next commitment before the last place is spent.
- **L11:** L03: outline before colored interior. Проверка конструкции: Clear grape masses, short release and a relief round.
- **L12:** L25: white appears outside and in buried facial details. Проверка конструкции: Moon/body/face residue has a legible and bounded dependency.
- **L13:** L25: one material outside and behind another. Проверка конструкции: Pearl rain and pearl canopy spots create an actual repeated-color dependency.
- **L14:** L04: body, doorway, deepest detail. Проверка конструкции: Pot body and inset detail give successive fronts.
- **L15:** L25: dark/green pair releases after head opens. Проверка конструкции: Preserve supported puzzle; inspect paired body/outline release.
- **L16:** L02: several broad outside regions. Проверка конструкции: Coherent kite panels offer parallel work and relief.
- **L17:** L26: brown25 has only two outer connected brown cells. Проверка конструкции: Judge reachable volume, not mere color presence.
- **L18:** L04: mushroom house: roof, outline, walls, door. Проверка конструкции: Direct subject comparison with short roof residue and later door.
- **L19:** L05: cream16 leaves two buried cells for about42 seconds. Проверка конструкции: Repeated frame/flower material must release sooner than that anti-pattern.
- **L20:** L10: choose material that opens the next required one. Проверка конструкции: Bridge applies our own crossing mechanic to an unlocking choice.
- **L21:** L04: multiple fronts and five occupied working places. Проверка конструкции: Readable forgiving crossing; full occupancy can be productive.
- **L22:** L20: linked commitments occupy four places. Проверка конструкции: Preserve strong puzzle while measuring one-place pipeline intervals.
- **L23:** L02: broad independently available regions. Проверка конструкции: Colored balloon panels form a satisfying relief.
- **L24:** L05,L10: rings plus an unlocking choice. Проверка конструкции: Shell layers establish material order; crossing opens required work.
- **L25:** L25: W28+W51 spans sparkles/face/seeds; dark30+green22 waits for eyes. Проверка конструкции: Shorten dependency distance and retain primary-route risk points.
- **L26:** L26: visible material can have little reachable volume. Проверка конструкции: Quota-aware petals, outline and fence ordering.
- **L27:** L20: background500 sustains work while links wait. Проверка конструкции: Sea/background work releases linked body details; preserve human anchor.
- **L28:** L03: outline, fruit body, highlights. Проверка конструкции: Moderate body packets and a short relief cleanup.
- **L29:** L20: median30, max38 packets versus oversized background. Проверка конструкции: Measure and disclose night-quota waiting; unlocking choice is our adaptation.
- **L30:** L03,L10: readable outline/body and flowing last phase. Проверка конструкции: Recognizable face/body with uncomplicated completion.

## Проверки и границы

Все уровни имеют выигрышный маршрут с тремя видимыми рядами. Для каждого отдельно проверены ожидание между командами, быстрые нажатия и сохранение одного места про запас, на ×1/×2; автоматический хвост считается на ×2,5. Неуспешный темп остаётся неуспешным в отчёте — его время не подменяется оценкой другого маршрута.

Сохранение места про запас само может заставлять ждать. Поэтому учитываются и его собственные задержки, и непрерывные фазы с одним-двумя работающими местами, даже когда нажатия разрешены. Порог развилок проверяется на исходном маршруте до подбора вариантов темпа: опасный обходной маршрут не используется для искусственного завышения сложности.

Сохранённые варианты с явными замечаниями по темпу: L10 (forced-one-two-working-places); L15 (continuous-no-input, forced-one-two-working-places); L21 (continuous-no-input). Их положительная человеческая оценка важна; эти паузы остаются предметом сравнения, а не объявляются исправленными.

[Подробные геометрия, квоты, развилки и все расписания](pilot-v9-synthesis-review-2026-10-24.json).

Раскадровки и очереди источника: [Colony Flow rounds](colony-flow-rounds/README.md). Картинки и последовательности наши; мосты — наша механика применения принципа открытия нужного фронта. Неполные L25/L26 соперника не выдаются за точные решённые симуляции. Удержание и интерес требуют человеческого теста.

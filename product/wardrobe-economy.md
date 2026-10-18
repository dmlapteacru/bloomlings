# Wardrobe: ассортимент и цены

Решение владельца: все вещи за петали; редкие дорогие. Открытие по уровню разрешает покупку, но не выдаёт вещь бесплатно. Wardrobe открывается на текущем уровне 40 (после завершения 39).

## Открытия

| Текущий уровень | Доступно из 89 | Доля |
|---|---:|---:|
| 40 | 55 | 62% |
| 60 | 69 | 78% |
| 80 | 84 | 94% |
| 100 | 89 | 100% |

У каждого героя бесплатны одна альтернативная причёска, круглые очки и комбинезон: всего 12 вариантов. Стандартный облик — отдельная карточка Default, без дубля той же головы на витрине. На игровом герое всегда остаётся стандартная причёска/головной убор.

Сохраняем Head, Face, Body, Accessories. Скины, выражения, следы, старые процедурные шапки и неподогнанный рюкзак убраны. Старые ID и владение остаются в сохранении для совместимости, но устаревшие вещи не продаются, не выдаются и не рисуются. Награды этапов теперь выдают актуальные украшения профиля, сохраняя суммы петалей и бусты.

## Стоимость и экономика

| Группа | Петали |
|---|---:|
| Простой шарф, причёски, дополнительные очки | 250–500 |
| Обычные головные уборы, лёгкая одежда | 650 |
| Походные и необычные повседневные шляпы | 850 |
| Осенняя одежда, платье | 1000 |
| Зимние наушники | 1200 |
| Зимняя одежда | 1500 |
| Новогодние шапки | 2000 |
| Уникальные причёски и шляпы | 2500 |
| Корона Bloom | 3500 |

Весь текущий гардероб стоит **73 000 петалей**: Sprig 16 050, Bloom 21 150, Drop 17 200, Twig 18 600. Десять особенных головных уборов стоят 2000–3500; при этом 44 платные вещи стоят не больше 650.

Чистая обычная победа приносит 20 петалей, Hard — 30, Super Hard — 40. Поэтому обычная шляпа за 650 — примерно 33 обычные победы, уникальная за 2500 — 125. Ежедневные награды, Daily Challenge, этапы и покупки петалей сокращают накопление; бусты конкурируют за тот же баланс. Доход уровней не увеличиваем.

Точный валовой доход установленного каталога при всех чистых победах (до ежедневных наград, рекламы, доната и расходов):

| Достигнутый текущий уровень | Петали за победы и этапы |
|---|---:|
| 40 | 950 |
| 60 | 1520 |
| 80 | 2050 |
| 100 | 2510 |

Это баланс стартового релиза, а не обещание срока накопления: ежедневные награды зависят от календаря, использование бустов и покупки — от игрока. К L100 открывается возможность выбора всего ассортимента, но не бюджет на его полную покупку. Платных эксклюзивов и отдельной премиум-валюты нет; покупка петалей ускоряет получение желаемого образа. После релиза нужны данные по остаткам валюты, покупкам, использованию бустов и сроку накопления на дорогие вещи.

## Примерка, покупка и сохранения

- Нажатие на ненадетую купленную/бесплатную вещь сразу сохраняет выбор своего слота.
- Неприобретённая вещь, включая закрытую по уровню, примеряется только на главном стенде. Карточки показывают предметы отдельно и остаются неизменными.
- Смена категории, героя или выход сбрасывает временную примерку.
- Зелёная кнопка с петалями вызывает подтверждение. Только успешное подтверждение списывает валюту, выдаёт вещь и сохраняет её на герое.
- Вместо цены закрытой вещи показан замок и нужный уровень. Купить её через API до открытия тоже нельзя.
- Уже купленные вещи и действительно надетые предметы прежней бесплатной тестовой версии сохраняются. Весь старый пробный каталог автоматически не выдаётся. Снижение уровня тестовыми кнопками не отнимает приобретённые вещи.

## Тестовые настройки

В Settings на главной и из паузы: **−100 / +100 / +1000 / Reset** петалей. Нижняя граница 0, верхняя Int32.MaxValue. Изменения сохраняются сразу; Reset в строке петалей обнуляет только валюту. Строка уровней сохранена. Эти средства тестирования не являются обычным способом получения валюты игроком.

## Полный действующий список

Источник истины: `client/Assets/Bloomlings/Meta/Wardrobe/Resources/CosmeticCatalog.json`. Цены и уровни ниже соответствуют этому файлу.

### Sprig

| ID | Вещь | Слот | Уровень | Петали |
|---|---|---|---:|---:|
| `hat.sprig_daisy` | Daisy Cap | Head | 40 | 650 |
| `glasses.sprig_round` | Round Leaf Glasses | Face | 40 | 0 |
| `hat.sprig_rosette` | Leaf Rosette | Head | 40 | 0 |
| `hat.sprig_swirl` | Leaf Swirl | Head | 40 | 350 |
| `hat.sprig_panama` | Garden Panama | Head | 40 | 650 |
| `hat.sprig_beret` | Garden Beret | Head | 40 | 650 |
| `hat.sprig_earmuffs` | Winter Earmuffs | Head | 80 | 1200 |
| `hat.sprig_xmas` | Holiday Hat | Head | 100 | 2000 |
| `scarf.sprig_knit` | Garden Scarf | Accessories | 40 | 350 |
| `clothes.sprig_jacket` | Winter Jacket | Body | 80 | 1500 |
| `clothes.sprig_coat` | Autumn Coat | Body | 60 | 1000 |
| `clothes.sprig_overalls` | Garden Overalls | Body | 40 | 0 |
| `glasses.sprig_square` | Square Garden Glasses | Face | 40 | 350 |
| `hat.sprig_bowler` | Bowler Hat | Head | 60 | 850 |
| `hat.sprig_explorer` | Explorer Hat | Head | 60 | 850 |
| `hat.sprig_clover` | Clover Hair | Head | 80 | 2500 |
| `hat.sprig_sprout` | Sprout Hair | Head | 40 | 350 |
| `hat.sprig_headscarf` | Garden Headscarf | Head | 60 | 850 |
| `hat.sprig_nightcap` | Dreamy Nightcap | Head | 40 | 650 |
| `clothes.sprig_explorer_vest` | Explorer Vest | Body | 40 | 650 |
| `hat.sprig_boater` | Ribbon Boater | Head | 40 | 650 |

### Bloom

| ID | Вещь | Слот | Уровень | Петали |
|---|---|---|---:|---:|
| `hat.bloom_bob` | Petal Bob | Head | 40 | 0 |
| `hat.bloom_peach` | Petal Curl | Head | 40 | 350 |
| `hat.bloom_rose` | Rosebud Hair | Head | 80 | 2500 |
| `hat.bloom_panama` | Flower Panama | Head | 40 | 650 |
| `hat.bloom_cap` | Flower Cap | Head | 40 | 650 |
| `hat.bloom_beret` | Flower Beret | Head | 40 | 650 |
| `hat.bloom_earmuffs` | Winter Earmuffs | Head | 80 | 1200 |
| `hat.bloom_xmas` | Holiday Hat | Head | 100 | 2000 |
| `glasses.bloom_round` | Pink Petal Glasses | Face | 40 | 0 |
| `scarf.bloom_knit` | Chunky Flower Scarf | Accessories | 80 | 500 |
| `scarf.bloom_stripe` | Striped Garden Scarf | Accessories | 40 | 250 |
| `clothes.bloom_jacket` | Winter Jacket | Body | 80 | 1500 |
| `clothes.bloom_coat` | Autumn Coat | Body | 60 | 1000 |
| `clothes.bloom_overalls` | Garden Overalls | Body | 40 | 0 |
| `glasses.bloom_heart` | Heart Glasses | Face | 40 | 450 |
| `hat.bloom_boater` | Ribbon Boater | Head | 40 | 650 |
| `hat.bloom_bucket_green` | Green Bucket Hat | Head | 40 | 650 |
| `hat.bloom_crown` | Petal Crown | Head | 100 | 3500 |
| `clothes.bloom_dress_purple` | Purple Garden Dress | Body | 40 | 1000 |
| `hat.bloom_flower_hat` | Flower Hat | Head | 60 | 850 |
| `hat.bloom_headscarf` | Garden Headscarf | Head | 60 | 850 |
| `hat.bloom_nightcap` | Dreamy Nightcap | Head | 40 | 650 |
| `hat.bloom_summer_panama` | Summer Panama | Head | 40 | 650 |
| `clothes.bloom_top_flower` | Flower Top | Body | 40 | 650 |

### Drop

| ID | Вещь | Слот | Уровень | Петали |
|---|---|---|---:|---:|
| `hat.drop_side_wave` | Side Wave | Head | 40 | 0 |
| `hat.drop_twin_ripples` | Twin Ripples | Head | 40 | 350 |
| `hat.drop_whirlpool` | Whirlpool Hair | Head | 40 | 350 |
| `hat.drop_panama` | Seaside Panama | Head | 40 | 650 |
| `hat.drop_cap` | Wave Cap | Head | 40 | 650 |
| `hat.drop_beret` | Seaside Beret | Head | 40 | 650 |
| `hat.drop_earmuffs` | Winter Earmuffs | Head | 80 | 1200 |
| `hat.drop_xmas` | Holiday Hat | Head | 100 | 2000 |
| `glasses.drop_round` | Drop Glasses | Face | 40 | 0 |
| `scarf.drop_knit` | Chunky Seaside Scarf | Accessories | 80 | 500 |
| `scarf.drop_stripe` | Striped Seaside Scarf | Accessories | 40 | 250 |
| `clothes.drop_jacket` | Winter Jacket | Body | 80 | 1500 |
| `clothes.drop_coat` | Autumn Coat | Body | 60 | 1000 |
| `clothes.drop_overalls` | Garden Overalls | Body | 40 | 0 |
| `hat.drop_nightcap` | Starry Nightcap | Head | 40 | 650 |
| `clothes.drop_sailor_top` | Sailor Top | Body | 40 | 650 |
| `hat.drop_shell` | Seashell Hair | Head | 80 | 2500 |
| `hat.drop_sailor_hat` | Sailor Hat | Head | 40 | 650 |
| `hat.drop_headscarf` | Navy Bandana | Head | 60 | 850 |
| `clothes.drop_mint_shirt` | Mint Shirt | Body | 40 | 650 |
| `hat.drop_explorer` | Explorer Helmet | Head | 60 | 850 |
| `hat.drop_boater` | Ribbon Boater | Head | 40 | 650 |
| `hat.drop_rain_hat` | Sunshine Rain Hat | Head | 40 | 650 |

### Twig

| ID | Вещь | Слот | Уровень | Петали |
|---|---|---|---:|---:|
| `hat.twig_panama` | Panama Hat | Head | 40 | 650 |
| `hat.twig_hair` | Woodland Hair | Head | 40 | 0 |
| `hat.twig_hat_hair` | Acorn Hat & Hair | Head | 40 | 650 |
| `hat.twig_beret` | Autumn Beret | Head | 40 | 650 |
| `hat.twig_earmuffs` | Winter Earmuffs | Head | 80 | 1200 |
| `hat.twig_xmas` | Holiday Hat | Head | 100 | 2000 |
| `glasses.twig_round` | Chestnut Glasses | Face | 40 | 0 |
| `scarf.twig_knit` | Golden Scarf | Accessories | 40 | 350 |
| `clothes.twig_jacket` | Winter Jacket | Body | 80 | 1500 |
| `clothes.twig_coat` | Autumn Coat | Body | 60 | 1000 |
| `clothes.twig_overalls` | Garden Overalls | Body | 40 | 0 |
| `glasses.twig_octagon` | Octagon Glasses | Face | 40 | 450 |
| `clothes.twig_explorer_vest` | Explorer Vest | Body | 40 | 650 |
| `hat.twig_pinecone` | Pinecone Hair | Head | 80 | 2500 |
| `hat.twig_headscarf` | Garden Headscarf | Head | 60 | 850 |
| `hat.twig_mushroom` | Mushroom Cap | Head | 80 | 2500 |
| `hat.twig_nightcap` | Dreamy Nightcap | Head | 40 | 650 |
| `clothes.twig_explorer_shirt` | Explorer Shirt | Body | 40 | 650 |
| `hat.twig_boater` | Ribbon Boater | Head | 40 | 650 |
| `hat.twig_bowler` | Bowler Hat | Head | 60 | 850 |
| `hat.twig_explorer` | Explorer Hat | Head | 60 | 850 |

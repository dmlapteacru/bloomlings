# Sprig — нейтральное лицо для пробного Retexture

Владелец показал результат Tripo с бордовым контуром рта, подчёркнутыми глазами и тёмной точкой
на носу. По скриншоту это похоже на нежелательную интерпретацию лица при текстурировании;
отделить цветовую карту от normal/roughness и геометрии можно только по самому экспорту.

`sprig-texture-reference-neutral-face.png` убирает румянец и красный цвет из рта, оставляет
тёмно-коричневую полость и маленький персиковый язык внутри. Край рта и нос — цвета кожи.
Это тестовый вариант для более однозначного переноса лица, не гарантия точного результата Tripo.

Короткое положительное описание, если интерфейс позволяет добавить текст:
“Simple cartoon plant face. Skin-colored mouth rim, dark brown mouth cavity with a tiny peach
tongue inside, plain pistachio cheeks, clean simple eyes. Preserve the existing facial features,
geometry and UV layout.”

Если есть отдельное поле negative prompt: `lipstick, human lips, makeup, eyeliner, eyelashes,
nostrils, red mouth outline`. Если сервис снова меняет черты, надёжнее точечная правка цветовой
карты по существующим UV, чем повторная генерация всего лица. Для такой правки нужен экспорт
именно неудачного результата или его карты текстур.

ImageGen, 2026-10-16; источник и точный промпт — `prompt.json`, размеры и SHA-256 — `manifest.json`.
Модель, риг и игровые ассеты не менялись.

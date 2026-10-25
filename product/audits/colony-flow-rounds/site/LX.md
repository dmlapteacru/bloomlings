# Colony Flow LX (level unknown) — капибара (Normal)

Видео: [videoplayback.mp4](https://pub-dc09455922e746b0979cdaa7317edd10.r2.dev/videoplayback.mp4) (видео с colony-flow.com, 640×360).

Первый кадр: [start/LX.jpg](start/LX.jpg).

## Поле

Обложки нет; поле видно только на первом кадре (мелко), не восстановлено.

## Очередь: 25 капсул, 3 колонок, 5 мест ожидания

Верх колонки — первая строка. `?` — капсула закрыта, пока не станет верхней; `:x` — связка x (один тап берёт всю связку); `#цвет` — замок (не тапается, уходит, когда собран ключ на поле).

| Буква | Цвет | Клеток в капсулах |
|---|---|---:|
| `M` | maroon / dark red-brown (shading) | 55 |
| `B` | brown (capybara body) | 195 |
| `O` | orange-yellow | 6 |
| `D` | dark purple-black (outline) | 85 |
| `G` | lime green (grass) | 200 |
| `W` | white (flowers) | 35 |

```
ряд      c1     c2     c3
  1     M15    B30    G28
  2     B25    B24    G28
  3      O2    B28    B30
  4   D30:a    M20     O4
  5   G30:a    G30    B28
  6     W15    G24    D25
  7     B30    D30    G30
  8     G30    W10    W10
  9                   M20
```

## Механики

- Vertical link: c1 rows 4-5 (dark 30 above green 30) joined by a short green bar in one column, visible from 42.25; the lower one came out with the upper one at 58.25 and both kept two joined slots.
- Board (start frame only, no cover): two grey patches of hidden '?' tiles on the capybara's lower body (left and right). No locks, keys or boxes seen.

## Попытка 1: победа, 1.75–100.5 с, 24 тапов

| Время, с | Колонка | Берёт | Заметка |
|---:|---|---|---|
| 2.25 | c3 | c3 G28 |  |
| 2.75 | c3 | c3 G28 |  |
| 19 | c3 | c3 B30 |  |
| 19.5 | c3 | c3 O4 |  |
| 20 | c3 | c3 B28 |  |
| 20.5 | c3 | c3 D25 |  |
| 21.5 | c3 | c3 G30 | all 5 slots full (30/1/28/22/30) |
| 25.25 | c3 | c3 W10 | c3 white 10; c3's last capsule (maroon 20) stays on top until 87.75 |
| 30.25 | c1 | c1 M15 |  |
| 37.25 | c2 | c2 B30 |  |
| 38.25 | c2 | c2 B24 |  |
| 39.5 | c2 | c2 B28 |  |
| 42.25 | c1 | c1 B25 |  |
| 55.75 | c1 | c1 O2 |  |
| 58.25 | c1 | c1 D30 + c1 G30 | pair a: vertical link in c1, dark 30 on top of green 30; one tap took both |
| 63 | c1 | c1 W15 |  |
| 68 | c2 | c2 M20 |  |
| 75.5 | c2 | c2 G30 |  |
| 79.75 | c2 | c2 G24 |  |
| 82.25 | c2 | c2 D30 |  |
| 83.25 | c2 | c2 W10 | c2 empties; columns re-centre |
| 85.25 | c1 | c1 B30 |  |
| 86.25 | c1 | c1 G30 | c1 empties |
| 87.75 | c3 | c3 M20 | tray empty |

События:

- 11 с — green slots finish; long static stretch 11-18.5 with all slots empty
- 100 с — all slots empty
- 100.5 с — win screen, +20 coins

## Неуверенно

- Level number unknown (unnamed video); difficulty assumed Normal (no Hard marker seen)
- M (maroon, c1 M15, c2 M20, c3 M20) vs B (brown): told apart by the clearly redder, darker shade
- c1 row 3 O2 and c3 row 4 O4: orange-yellow, read as the same colour
- The c2 brown 30 tap (37.25) fell outside the event sheets; read from extra frames 34-38 s
- Tap times within ±0.25 s

Раскадровка: лоток двигался 32 раз (кадры 4 в секунду, `ytsheets.py`); времена в `levels.json` → `storyboard.trayMoves`.

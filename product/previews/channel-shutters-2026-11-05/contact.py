"""Contact sheet of the channel and shutters look mock of 2026-11-05 (ChannelShutterMock.cs).

Usage: python3 contact.py <mock dir> [--captioned]   (writes <mock dir>/contact-sheet.png and adds a Russian caption bar to every *-x2.png)
"""
import os
import sys

from PIL import Image, ImageDraw, ImageFont

FONT = "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"
FONT_R = "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"
BG = (40, 34, 30)
INK = (255, 244, 220)
SUB = (225, 205, 175)

CH = {
    "1-start": "1 · Старт: подход, спящий родник, два выхода бледно",
    "2-choice": "2 · Подход расчищен: выбор выхода, рука-подсказка",
    "3-water": "3 · Выбран внутренний: вода бежит по каналу",
    "4-open": "4 · Выход открыт: ходоки выходят из него",
    "5-outer": "5 · Другой выбор: внешний выход у правого края",
    "option-faint-routes": "Опция: оба пути бледным пунктиром до выбора",
}
SH = {
    "1-closed": "1 · Закрыты: цвет материала и счётчик 30",
    "2-counting": "2 · Счёт идёт: зелёные клетки где угодно, искры, «щелчок»",
    "3-opening": "3 · Счётчик 0: створки открываются, пыль и листья",
    "4-open": "4 · Открыты: под ними обычные кубики",
    "5-symbols": "5 · Режим символов: символ материала на счётчике",
    "6-small-cubes": "6 · Мелкие кубики (доска 48×45, как L61)",
    "6-big-cubes": "6 · Крупные кубики (доска 31×36)",
}
TITLES = {
    "channel-A": "Канал A: открытый ручей в каменных берегах · выходы-родники",
    "channel-B": "Канал B: деревянный жёлоб на опорах · выходы-арки",
    "shutters-A": "Створки A: двустворчатые двери · круглая табличка",
    "shutters-B": "Створки B: крышка из досок · бирка-кубик на гвозде",
}


def font(size, bold=True):
    return ImageFont.truetype(FONT if bold else FONT_R, size)


def caption_of(name):
    for prefix, table in (("channel-", CH), ("shutters-", SH)):
        if name.startswith(prefix):
            group = name[: len(prefix) + 1]
            key = name[len(prefix) + 2:]
            return TITLES[group], table.get(key, key)
    return name, ""


def add_caption(path):
    img = Image.open(path).convert("RGB")
    title, sub = caption_of(os.path.basename(path)[:-len("-x2.png")])
    bar = 120
    out = Image.new("RGB", (img.width, img.height + bar), BG)
    out.paste(img, (0, bar))
    d = ImageDraw.Draw(out)
    d.text((24, 14), title, font=font(42), fill=INK)
    d.text((24, 66), sub + "   (×2)", font=font(34, False), fill=SUB)
    out.save(path, optimize=True)


CROPS = {"channel": (110, 330, 1968, 1700), "shutters-A-6": None, "shutters-B-6": None, "shutters": (240, 120, 1840, 1300)}


def crop_of(name):
    for key in ("shutters-A-6", "shutters-B-6", "channel", "shutters"):
        if name.startswith(key):
            return CROPS[key]
    return None


def tile(path, width, bar):
    img = Image.open(path).convert("RGB")
    img = img.crop((0, bar, img.width, img.height))
    box = crop_of(os.path.basename(path))
    if box:
        img = img.crop(box)
    h = int(img.height * width / img.width)
    img = img.resize((width, h), Image.LANCZOS)
    title, sub = caption_of(os.path.basename(path)[:-len("-x2.png")])
    words = sub.split(" ")
    lines, cur = [], ""
    f = font(19, False)
    for w in words:
        test = (cur + " " + w).strip()
        if f.getlength(test) > width - 16 and cur:
            lines.append(cur)
            cur = w
        else:
            cur = test
    lines.append(cur)
    head = 12 + 26 * len(lines)
    out = Image.new("RGB", (width, h + 64), BG)
    out.paste(img, (0, 64))
    d = ImageDraw.Draw(out)
    for i, line in enumerate(lines[:2]):
        d.text((8, 6 + i * 26), line, font=f, fill=SUB)
    return out


BAR = 0


def main(root, captioned=False):
    global BAR
    BAR = 120 if captioned else 0
    names = sorted(f for f in os.listdir(root) if f.endswith("-x2.png"))
    raw = {}
    for f in names:
        raw[f] = os.path.join(root, f)
    rows = [
        ("channel-A", ["1-start", "2-choice", "3-water", "4-open", "5-outer", "option-faint-routes"]),
        ("channel-B", ["1-start", "2-choice", "3-water", "4-open", "5-outer", "option-faint-routes"]),
        ("shutters-A", ["1-closed", "2-counting", "3-opening", "4-open", "5-symbols", "6-small-cubes", "6-big-cubes"]),
        ("shutters-B", ["1-closed", "2-counting", "3-opening", "4-open", "5-symbols", "6-small-cubes", "6-big-cubes"]),
    ]
    width = 520
    gap = 10
    built = []
    for group, keys in rows:
        tiles = [tile(raw[f"{group}-{k}-x2.png"], width, BAR) for k in keys if f"{group}-{k}-x2.png" in raw]
        h = max(t.height for t in tiles)
        line = Image.new("RGB", (len(tiles) * (width + gap) + gap, h + 64), BG)
        ImageDraw.Draw(line).text((gap, 12), TITLES[group], font=font(30), fill=INK)
        for i, t in enumerate(tiles):
            line.paste(t, (gap + i * (width + gap), 60))
        built.append(line)
    head = 110
    W = max(b.width for b in built)
    H = head + sum(b.height + gap for b in built)
    sheet = Image.new("RGB", (W, H), (28, 24, 21))
    d = ImageDraw.Draw(sheet)
    d.text((gap, 14), "Bloomlings · макет вида: водный канал Drop (L101/L103) и деревянные створки Twig (L121/L123) · 05.11.2026", font=font(34), fill=INK)
    d.text((gap, 62), "Рисунок игры: кубики E3, вода и каменные берега ручья, арка входа, дерево табличек и мостика, кубики подов с числом. В движке ещё ничего нет.", font=font(22, False), fill=SUB)
    y = head
    for b in built:
        sheet.paste(b, (0, y))
        y += b.height + gap
    sheet.save(os.path.join(root, "contact-sheet.png"), optimize=True)
    if not captioned:
        for f in names:
            add_caption(raw[f])


if __name__ == "__main__":
    main(sys.argv[1], len(sys.argv) > 2 and sys.argv[2] == "--captioned")

#!/usr/bin/env python3
"""PNG previews of picture sketches, for a person or an agent to look at (no image library needed: zlib and struct).

Each role is drawn in a shade of its color group (green, pink_purple, blue_cyan, brown_orange; lime and red for the
expansion groups), the background in a pale shade of its group, an empty cell white and a stone grey, so the subject
reads as it will on a board before any variant is mapped.

    preview_png.py <out.png> <id> [<id> ...] [--src <folder>] [--cell <px>] [--columns <n>]

draws one picture, or a contact sheet of several (labelled by position only), from <folder>/<id>.grid.txt and
<id>.meta.json (default content/pictures/src).
"""
import json
import os
import struct
import sys
import zlib

SRC = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'src')

SHADES = {
    'green': ['#43a047', '#1b5e20', '#9ccc65', '#2e7d32', '#c5e1a5'],
    'pink_purple': ['#ec407a', '#8e24aa', '#f48fb1', '#6a1b9a', '#f8bbd0'],
    'blue_cyan': ['#1e88e5', '#00acc1', '#0d47a1', '#80deea', '#4fc3f7'],
    'brown_orange': ['#fb8c00', '#6d4c41', '#ffca28', '#a1887f', '#e65100'],
    'lime': ['#c0ca33', '#9e9d24'],
    'red': ['#e53935', '#b71c1c'],
}
PALE = {'green': '#e8f5e9', 'pink_purple': '#fce4ec', 'blue_cyan': '#e3f2fd', 'brown_orange': '#fff3e0', 'lime': '#f9fbe7', 'red': '#ffebee'}


def rgb(hex_color):
    h = hex_color.lstrip('#')
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))


def colors_of(meta):
    """Each grid character's color: its role's group shade, the n-th role of a group taking the n-th shade."""
    by_role = {role['roleId']: role for role in meta['roles']}
    used = {}
    colors = {'.': (255, 255, 255), '#': (150, 150, 150)}
    for char, role_id in sorted(meta['legend'].items(), key=lambda kv: [r['roleId'] for r in meta['roles']].index(kv[1]) if kv[1] in by_role else 99):
        role = by_role.get(role_id)
        if role is None:
            continue
        group = role['colorGroup']
        if role.get('isBackground'):
            colors[char] = rgb(PALE.get(group, '#ffffff'))
            continue
        n = used.get(group, 0)
        used[group] = n + 1
        shades = SHADES.get(group, ['#000000'])
        colors[char] = rgb(shades[n % len(shades)])
    return colors


def load(src, pid):
    with open(os.path.join(src, pid + '.meta.json')) as f:
        meta = json.load(f)
    with open(os.path.join(src, pid + '.grid.txt')) as f:
        rows = [line.rstrip('\n') for line in f if line.strip()]
    return meta, rows


def write_png(path, width, height, pixels):
    raw = b''.join(b'\x00' + bytes(v for px in pixels[y * width:(y + 1) * width] for v in px) for y in range(height))

    def chunk(kind, data):
        return struct.pack('>I', len(data)) + kind + data + struct.pack('>I', zlib.crc32(kind + data) & 0xFFFFFFFF)

    png = b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', width, height, 8, 2, 0, 0, 0))
    png += chunk(b'IDAT', zlib.compress(raw, 9)) + chunk(b'IEND', b'')
    with open(path, 'wb') as f:
        f.write(png)


def sheet(out, ids, src=SRC, cell=10, columns=6, gap=8):
    pictures = [load(src, pid) for pid in ids]
    cols = min(columns, len(pictures))
    rows_n = (len(pictures) + cols - 1) // cols
    tile_w = max(len(rows[0]) for _, rows in pictures) * cell
    tile_h = max(len(rows) for _, rows in pictures) * cell
    width = cols * tile_w + (cols + 1) * gap
    height = rows_n * tile_h + (rows_n + 1) * gap
    pixels = [(60, 60, 60)] * (width * height)
    for i, (meta, rows) in enumerate(pictures):
        colors = colors_of(meta)
        ox = gap + (i % cols) * (tile_w + gap)
        oy = gap + (i // cols) * (tile_h + gap)
        for y, line in enumerate(rows):
            for x, char in enumerate(line):
                color = colors.get(char, (0, 0, 0))
                for dy in range(cell - (1 if cell > 4 else 0)):
                    base = (oy + y * cell + dy) * width + ox + x * cell
                    for dx in range(cell - (1 if cell > 4 else 0)):
                        pixels[base + dx] = color
    write_png(out, width, height, pixels)


if __name__ == '__main__':
    args = sys.argv[1:]
    options = {'--src': SRC, '--cell': '10', '--columns': '6'}
    for key in list(options):
        if key in args:
            options[key] = args[args.index(key) + 1]
            del args[args.index(key):args.index(key) + 2]
    if len(args) < 2:
        print(__doc__)
        sys.exit(2)
    sheet(args[0], args[1:], options['--src'], int(options['--cell']), int(options['--columns']))
    print(f'wrote {args[0]} ({len(args) - 1} pictures)')

#!/usr/bin/env python3
"""Read-only art registry check. This detects structural copies, not semantic originality."""
import argparse
import json
from pathlib import Path
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]


def signature(grid):
    """Ignore role colors, orientation, empty margins and repeated rows/columns from nearest scaling."""
    rows = [list(row) for row in grid]
    while rows and all(v == -1 for v in rows[0]):
        rows.pop(0)
    while rows and all(v == -1 for v in rows[-1]):
        rows.pop()
    if not rows:
        raise ValueError('empty composition')
    while all(row[0] == -1 for row in rows):
        rows = [row[1:] for row in rows]
    while all(row[-1] == -1 for row in rows):
        rows = [row[:-1] for row in rows]
    rows = [row for i, row in enumerate(rows) if i == 0 or row != rows[i - 1]]
    columns = list(zip(*rows))
    columns = [col for i, col in enumerate(columns) if i == 0 or col != columns[i - 1]]
    rows = [list(row) for row in zip(*columns)]

    def label(shape):
        labels = {}
        values = []
        for row in shape:
            for cell in row:
                values.append(cell if cell < 0 else labels.setdefault(cell, len(labels)))
        return len(shape), len(shape[0]), tuple(values)

    candidates = []
    for shape in (rows, list(zip(*rows))):
        for flip_y in (False, True):
            for flip_x in (False, True):
                oriented = shape[::-1] if flip_y else shape
                candidates.append(label([row[::-1] if flip_x else row for row in oriented]))
    return min(candidates)


def check(manifest, library, catalog=None, complete=False, target=2500):
    entries = manifest['compositions']
    errors = []
    compositions, pictures, grids = {}, {}, {}
    if manifest.get('launchCompositionTarget') != target:
        errors.append(f'registry target must match launch scope ({target})')
    policy = manifest.get('policy', {})
    if policy.get('oneCompositionPerLaunchLevel') is not True or policy.get('resizeMirrorRecolorAreNewComposition') is not False:
        errors.append('registry must require one use and reject resize/mirror/recolor as new compositions')
    for e in entries:
        cid, pid = e['compositionId'], e['pictureId']
        if cid in compositions:
            errors.append(f'duplicate compositionId: {cid}')
        compositions[cid] = e
        if pid in pictures:
            errors.append(f'picture registered twice: {pid}')
        pictures[pid] = cid
        p = json.loads((library / f'{pid}.json').read_text(encoding='utf-8'))
        if (p['id'], p['version'], p['width'], p['height']) != (pid, e['version'], e['width'], e['height']):
            errors.append(f'{pid}: registry differs from picture identity/dimensions')
        grid = p['grid']
        if len(grid) != p['height'] or any(len(row) != p['width'] for row in grid):
            errors.append(f'{pid}: invalid grid dimensions')
            continue
        shape = signature(grid)
        if shape in grids and grids[shape] != pid:
            errors.append(f'{pid}: structural copy of {grids[shape]} (color/orientation/nearest scaling ignored)')
        grids[shape] = pid
        if complete and e.get('artStatus') != 'approved':
            errors.append(f'{cid}: visual approval is missing')
    if complete and len(compositions) != target:
        errors.append(f'need {target} unique compositions; registry has {len(compositions)}')
    if catalog is not None:
        source = catalog / 'levels' if (catalog / 'levels').is_dir() else catalog
        if not source.is_dir():
            raise ValueError(f'catalog does not exist: {source}')
        numbers, used = set(), {}
        for f in sorted(source.glob('level-*.json')):
            # Reserve files need not be parsed for the launch audit.
            if int(f.stem.removeprefix('level-')) > target:
                continue
            level = json.loads(f.read_text(encoding='utf-8'))
            n = level['levelNumber']
            if not 1 <= n <= target:
                continue
            if n in numbers:
                errors.append(f'duplicate level number: {n}')
            numbers.add(n)
            ref = level['picture']
            pid = ref['id']
            cid = pictures.get(pid)
            if cid is None:
                errors.append(f'L{n}: picture {pid} has no registered composition')
                continue
            if ref['version'] != compositions[cid]['version']:
                errors.append(f'L{n}: picture version differs from registry')
            if cid in used:
                errors.append(f'L{n}: composition {cid} already used by L{used[cid]}')
            used[cid] = n
        if complete and numbers != set(range(1, target + 1)):
            errors.append(f'catalog must contain every level L1–{target}; found {len(numbers)}')
        if complete and set(used) != set(compositions):
            errors.append('catalog and registry must use exactly the same compositions')
    elif complete:
        errors.append('--complete requires --catalog to audit actual level references')
    return errors


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--registry', type=Path, default=ROOT / 'content/pictures/pilot/compositions.json')
    parser.add_argument('--lib', type=Path, default=ROOT / 'content/pictures/pilot/lib')
    parser.add_argument('--catalog', type=Path)
    parser.add_argument('--complete', action='store_true', help='Require approved artwork and exact launch coverage; the three-scene pilot intentionally fails.')
    args = parser.parse_args()
    try:
        target = int(ET.parse(ROOT / 'content/launch.props').findtext('.//LaunchLevelCount'))
        manifest = json.loads(args.registry.read_text(encoding='utf-8'))
        errors = check(manifest, args.lib, args.catalog, args.complete, target)
    except (OSError, ValueError, KeyError, TypeError, ET.ParseError) as exc:
        parser.exit(2, f'composition check: {exc}\n')
    for error in errors:
        print(error)
    if errors:
        return 1
    print(f"composition check: {len(manifest['compositions'])} distinct registered grids; "
          + ('complete launch coverage verified.' if args.complete else f'pilot/library only; launch target {target} is not yet fulfilled.'))
    return 0


if __name__ == '__main__':
    raise SystemExit(main())

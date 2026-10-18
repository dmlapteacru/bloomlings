#!/usr/bin/env python3
"""Summarize measured logical play; never edits level, picture or runtime files."""
import argparse
import collections
import csv
import hashlib
import json
from pathlib import Path
import statistics
import subprocess

ROOT = Path(__file__).resolve().parents[2]
p = argparse.ArgumentParser(__doc__)
p.add_argument('--input', type=Path, default=ROOT / 'content/work/gameplay-audit')
p.add_argument('--out', type=Path, default=ROOT / 'content/work/gameplay-audit/report')
p.add_argument('--expected', type=int, default=2500)
a = p.parse_args()

def documents(pattern):
    for path in sorted(a.input.glob(pattern)):
        for line in path.read_text(encoding='utf-8').splitlines():
            if line.strip():
                yield json.loads(line)

def selected(pattern):
    result = {}
    for row in documents(pattern):
        n = row['level']
        if n > a.expected:
            continue
        if n in result:
            old = result[n]
            if (old['definitionHash'], old['pictureHash']) != (row['definitionHash'], row['pictureHash']):
                raise ValueError(f'Mixed input snapshots at L{n}')
            if sum(v['runs'] for v in old['policies']) >= sum(v['runs'] for v in row['policies']):
                continue
        result[n] = row
    if set(result) != set(range(1, a.expected + 1)):
        raise ValueError(f'{pattern}: expected complete L1–{a.expected}, got {len(result)} levels')
    return result

full = selected('full-*.jsonl')
sequential = selected('sequential-*.jsonl')
rows = []
visible_hashes = set()
for n in range(1, a.expected + 1):
    measured, seq = full[n], sequential[n]
    if (measured['definitionHash'], measured['pictureHash']) != (seq['definitionHash'], seq['pictureHash']):
        raise ValueError(f'Policies measured different inputs at L{n}')
    level_path = ROOT / (f'content/curated/level-{n:04}.json' if n <= 10 else f'content/catalog/levels/level-{n:05}.json')
    definition = json.loads(level_path.read_text(encoding='utf-8'))
    picture_path = ROOT / f"content/pictures/lib/{definition['picture']['id']}.json"
    picture_bytes = picture_path.read_bytes()
    if hashlib.sha256(picture_bytes).hexdigest() != measured['pictureHash']:
        raise ValueError(f'Picture changed after measurement: L{n}')
    picture = json.loads(picture_bytes)
    record_path = ROOT / f'content/catalog/validation/level-{n:05}.json'
    validation = json.loads(record_path.read_text(encoding='utf-8')) if record_path.exists() else {}
    if validation and validation['definitionHash'] != measured['definitionHash']:
        raise ValueError(f'Validation differs from measurement: L{n}')
    metrics = validation.get('metrics', {})
    raw = []
    for line in picture['grid']:
        colors = [v if v < 0 else definition['mapping'][picture['roles'][v]['roleId']] for v in line]
        raw.append(colors[::-1] if definition['picture'].get('mirror') == 'horizontal' else colors)
    visible_hashes.add(hashlib.sha256(json.dumps(raw, separators=(',', ':')).encode()).hexdigest())
    row = {
        'level': n, 'class': measured['difficulty'], 'score': measured['score'],
        'width': measured['Width'], 'height': measured['Height'], 'picture': measured['Id'],
        'pods': measured['pods'], 'work': measured['work'], 'variants': measured['variants'],
        'medianPod': statistics.median(v['count'] for v in definition['pods']),
        'podShareAtMost12': sum(v['count'] <= 12 for v in definition['pods']) / len(definition['pods']),
        'symbolsOnCells': picture['width'] <= 24 and picture['height'] <= 24,
        'logicalBoardLook': definition.get('boardLook', 'peek'),
        'unsafeChoicePermilleStored': metrics.get('unsafeChoicePermille', ''),
        'peakBufferStored': metrics.get('peakBuffer', ''),
        'metricCompletenessStored': 'not-recorded',
        'mechanics': '|'.join(definition.get('mechanics', [])),
        'definitionHash': measured['definitionHash'], 'pictureHash': measured['pictureHash'],
    }
    for policy in measured['policies']:
        if policy['policy'] not in ('random', 'visible-frontier'):
            continue
        if policy['stalled']:
            raise ValueError(f"Incomplete {policy['policy']} at L{n}")
        key = policy['policy'].replace('-', '_')
        row[key + '_wins'] = policy['wins']
        row[key + '_runs'] = policy['runs']
    for policy in seq['policies']:
        if policy['stalled'] or policy['runs'] != 1:
            raise ValueError(f'Incomplete deterministic strategy at L{n}')
        row[policy['policy'].replace('-', '_') + '_win'] = policy['wins']
    exact = measured.get('exact')
    row['exactTutorialRandomWinProbability'] = exact['WinProbability'] if exact else ''
    rows.append(row)

doors_by_key = {}
for row in documents('doors-*.jsonl'):
    key = (row['level'], row['gate'])
    if row['level'] > a.expected:
        continue
    if row['definitionHash'] != full[row['level']]['definitionHash']:
        raise ValueError(f'Door audit measured different input at L{key[0]}')
    if row['error'] or not row['originalWon'] or not row['accountingSame']:
        raise ValueError(f'Door counterfactual lacks a valid original/control: {row}')
    doors_by_key[key] = row
doors = [doors_by_key[k] for k in sorted(doors_by_key)]

def class_summary(group):
    result = {}
    for cls in ('Normal', 'Hard', 'SuperHard'):
        batch = [r for r in group if r['class'] == cls]
        if not batch:
            continue
        item = {'levels': len(batch), 'meanPods': statistics.mean(r['pods'] for r in batch), 'maxPods': max(r['pods'] for r in batch)}
        for policy in ('leftmost', 'rightmost', 'cycle', 'visible_largest'):
            wins = sum(r[policy + '_win'] for r in batch)
            item[policy] = {'wins': wins, 'levels': len(batch), 'rate': wins / len(batch)}
        for policy in ('random', 'visible_frontier'):
            item[policy] = {
                'wins': sum(r[policy + '_wins'] for r in batch),
                'runs': sum(r[policy + '_runs'] for r in batch),
                # Equal weight per level: early levels have 256 trials, later ones 16.
                'macroRate': statistics.mean(r[policy + '_wins'] / r[policy + '_runs'] for r in batch),
            }
        result[cls] = item
    return result

fingerprint = hashlib.sha256('\n'.join(f"{r['level']}:{r['definitionHash']}:{r['pictureHash']}" for r in rows).encode()).hexdigest()
summary = {
    'sourceRevision': subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=ROOT, text=True).strip(),
    'inputFingerprint': fingerprint,
    'levels': len(rows), 'distinctMappedBaseGrids': len(visible_hashes),
    'randomRollouts': sum(r['random_runs'] + r['visible_frontier_runs'] for r in rows),
    'deterministicRollouts': len(rows) * 4,
    'all': class_summary(rows), 'afterTutorial': class_summary(rows[10:]),
    'firstTen': [{k: r[k] for k in ('level', 'class', 'score', 'width', 'height', 'pods', 'exactTutorialRandomWinProbability')} for r in rows[:10]],
    'doors': {
        'tested': len(doors), 'validOriginals': sum(r['originalWon'] for r in doors),
        'sameTraceWithClosedFootprint': sum(r['provedUnneeded'] for r in doors),
        'keyDoors': sum(r['keyDoor'] for r in doors),
        'keyDoorsSameTrace': sum(r['keyDoor'] and r['provedUnneeded'] for r in doors),
        'unprovenNecessityLevels': [r['level'] for r in doors if not r['provedUnneeded']],
    },
    'bands': {},
    'limitations': [
        'Logical core, settled commands; no phone timing, paid items, boosts or rescues.',
        'Macro rates give each level equal weight. Monte Carlo results are not human first-attempt rates.',
        'Door experiment suppresses only its administrative trigger goal/key pair and keeps its footprint blocked as stone.',
        'An unsuccessful frozen-door replay does not prove no alternate solution exists.',
        'Distinct mapped grids do not establish distinct semantic ideas or human art quality.',
        'Colony Flow origin could not be reached: cloud proxy CONNECT denied with HTTP 403; no live competitor claims.',
    ],
}
for lo, hi in ((1,10),(11,25),(26,50),(51,100),(101,300),(301,1000),(1001,2500)):
    batch = [r for r in rows if lo <= r['level'] <= hi]
    if batch:
        summary['bands'][f'{lo}-{hi}'] = class_summary(batch)
a.out.mkdir(parents=True, exist_ok=True)
(a.out / 'summary.json').write_text(json.dumps(summary, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
for name, data in (('levels.csv', rows), ('doors.csv', doors)):
    if data:
        with (a.out / name).open('w', encoding='utf-8', newline='') as f:
            writer = csv.DictWriter(f, fieldnames=list(data[0]), lineterminator='\n')
            writer.writeheader()
            writer.writerows(data)
print(f"Report: {len(rows)} levels, {summary['randomRollouts']} random rollouts, {summary['deterministicRollouts']} deterministic rollouts")
print(f"Doors: {summary['doors']['sameTraceWithClosedFootprint']}/{len(doors)} same solution with permanent closed footprint")
print('Input fingerprint:', fingerprint)

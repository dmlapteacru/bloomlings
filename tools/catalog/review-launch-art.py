#!/usr/bin/env python3
"""Prepare a batch for finished-board review, then stage only the reviewed and fully checked artwork.
Run from the repository root. Never generates puzzles or edits reference images.
"""
import argparse
import importlib.util
import json
import math
import os
import re
import shutil
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PIPELINE = ROOT / 'core/artifacts/bin/Bloomlings.Pipeline/release/bloomlings-pipeline.dll'
PREVIEW = ROOT / 'playtest/preview/bin/Release/net10.0/Bloomlings.Playtest.Preview.dll'
REGISTRY = ROOT / 'content/launch-art/compositions.json'
PLAN = ROOT / 'content/launch-art/plan.json'

def write(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_suffix(path.suffix + '.tmp')
    temporary.write_text(json.dumps(value, ensure_ascii=False, indent=2) + '\n')
    os.replace(temporary, path)

def command(*args):
    subprocess.run([str(a) for a in args], cwd=ROOT, check=True)

def main(attempt=0):
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--batch', type=int, required=True)
    p.add_argument('--levels', help='Optional comma-separated batch levels for individual corrections; source panel order stays fixed.')
    p.add_argument('--apply', action='store_true', help='Stage after inspecting this batch with the finished-board painter.')
    p.add_argument('--detect-layout', action='store_true', help='Record an unambiguous atlas layout with exactly the requested number of panels.')
    p.add_argument('--use-source-shades', action='store_true', help='If needed, preserve actual green/pink source shades as separate playable roles.')
    p.add_argument('--fit-background', action='store_true', help='Try bounded sampling zoom and one-cell clearance for the existing profile limits; review remains required.')
    p.add_argument('--dotnet', default='dotnet')
    args = p.parse_args()
    os.chdir(ROOT)
    batch_path = ROOT / f'content/launch-art/batches/atlas-{args.batch:04}.json'
    batch = json.loads(batch_path.read_text())
    work = ROOT / f'content/work/launch-atlas-{args.batch:04}-import'
    selected_levels = {int(n) for n in args.levels.split(',')} if args.levels else None
    if selected_levels is not None and (not selected_levels or not selected_levels <= {e['level'] for e in batch['entries']}):
        raise SystemExit('--levels must select existing entries from this batch.')
    if not args.apply:
        spec = importlib.util.spec_from_file_location('atlas', ROOT / 'tools/catalog/import-art-atlas.py')
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        if args.detect_layout:
            from PIL import Image
            image = Image.open(batch['referenceFile']).convert('RGB')
            try:
                module.panel_boundaries(image, batch['columns'], batch['rows'])
            except ValueError as error:
                raise SystemExit(f'Ambiguous panel layout: {error}; inspect the source atlas.')
        while True:
            try:
                module.import_batch(batch_path, PLAN, REGISTRY, work, selected_levels)
                break
            except module.ColorMassError as error:
                entry = next(e for e in batch['entries'] if e['compositionId'] == error.composition_id)
                if not args.use_source_shades or entry.get('adaptiveShades') or sum(v > 0 for k, v in error.counts.items() if k != '.') >= error.minimum:
                    raise
                entry['adaptiveShades'] = True
                write(batch_path, batch)
                print(f'{error.composition_id}: preserve actual source shades, then recheck all cell masses.')
        command(args.dotnet, PIPELINE, 'pictures', 'import', '--src', work / 'src', '--out', work / 'library')
    candidate_path = work / 'compositions.candidate.json'
    if not candidate_path.exists():
        raise SystemExit('Prepare and visually review this batch before --apply.')
    current = json.loads(REGISTRY.read_text())
    candidate = json.loads(candidate_path.read_text())
    levels = {e['level'] for e in batch['entries']}
    if args.levels:
        selected = {int(n) for n in args.levels.split(',')}
        if not selected or not selected <= levels:
            raise SystemExit('--levels must select existing entries from this batch.')
        levels = selected
    incoming = {e['launchLevel']: e for e in candidate['compositions'] if e['launchLevel'] in levels}
    if set(incoming) != levels:
        raise SystemExit('Candidate is missing batch assignments.')
    old = {e['launchLevel']: e for e in current['compositions']}
    for n, e in incoming.items():
        if n in old and old[n]['pictureId'] != e['pictureId']:
            raise SystemExit(f'L{n} already has different production art.')
        e['artStatus'] = 'approved'
        e['approvalBasis'] = 'production quality checks and finished-board review under owner-approved art direction'
        old[n] = e
    merged = dict(current, compositions=[old[n] for n in sorted(old)])
    review_library = work / 'review-library'
    review_library.mkdir(exist_ok=True)
    for e in merged['compositions']:
        src = (work / 'library' if e['launchLevel'] in levels else Path(current['libraryDirectory'])) / (e['pictureId'] + '.json')
        shutil.copy2(src, review_library / src.name)
    verification = dict(merged, libraryDirectory=str(review_library))
    write(work / 'verification.json', verification)
    status = subprocess.run([args.dotnet,str(PIPELINE),'launch','status','--registry',str(work/'verification.json'),
        '--plan',str(PLAN),'--json'],cwd=ROOT,text=True,capture_output=True)
    print(status.stderr, end='')
    report = json.loads(status.stdout)
    if status.returncode:
        changed = False
        unhandled = []
        if not args.apply and args.fit_background and attempt < 5:
            for problem in report.get('artErrors',[]):
                bg = re.fullmatch(r'L(\d+): background (\d+) outside (\d+)–(\d+)',problem)
                nesting = re.fullmatch(r'L(\d+): nesting 1 outside 2–5',problem)
                n = int((bg or nesting)[1]) if bg or nesting else -1
                entry = next((e for e in batch['entries'] if e['level']==n),None)
                if bg and entry:
                    value, low, high = map(int,bg.groups()[1:])
                    target = low+20 if value<low else high-20
                    factor = max(.85,min(1.15,math.sqrt((1000-target)/(1000-value))))
                    entry['zoom'] = round(entry.get('zoom',1)*factor,5)
                    changed = True
                elif nesting and entry and not entry.get('skyHalo'):
                    entry['skyHalo'] = True
                    changed = True
                else:
                    unhandled.append(problem)
        if not changed or unhandled:
            raise SystemExit('Batch quality gate failed: '+json.dumps(report,ensure_ascii=False))
        write(batch_path,batch)
        return main(attempt+1)
    preview = dict(verification, compositions=[incoming[n] for n in sorted(incoming)])
    write(work / 'preview.json', preview)
    if not args.apply:
        command(args.dotnet, 'run', '--project', 'playtest/preview', '-c', 'Release', '--no-build', '--',
                '--art-library', work / 'preview.json', '--out', work / 'review')
        print(f'Inspect {work}/review/art-00001-02500.png before --apply.')
        return
    target = Path(current['libraryDirectory'])
    target.mkdir(parents=True, exist_ok=True)
    source_target = ROOT / 'content/launch-art/src'
    source_target.mkdir(exist_ok=True)
    for e in incoming.values():
        shutil.copy2(work / 'library' / (e['pictureId'] + '.json'), target / (e['pictureId'] + '.json'))
        for suffix in ('.grid.txt', '.meta.json'):
            shutil.copy2(work / 'src' / (e['pictureId'] + suffix), source_target / (e['pictureId'] + suffix))
    write(REGISTRY, merged)
    queue_path = ROOT / 'content/launch-art/production-briefs.json'
    if queue_path.exists():
        queue = json.loads(queue_path.read_text())
        for e in queue['entries']:
            if e['level'] in incoming:
                e['state'] = 'registered'
                e['actualPalette'] = incoming[e['level']]['previewMapping']
                e['targetGridReviewRevision'] = 2
        write(queue_path, queue)
    batch['targetGridReviewRevision'] = 2
    write(batch_path, batch)
    command(args.dotnet, PIPELINE, 'launch', 'plan')
    print(f'Staged {len(incoming)} reviewed source compositions; puzzles are generated separately on the owner computer.')

if __name__ == '__main__':
    main()

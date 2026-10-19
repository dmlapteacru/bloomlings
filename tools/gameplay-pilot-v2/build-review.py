#!/usr/bin/env python3
"""Build the read-only v2 playtest report; does not rebuild the playable HTML."""
import base64
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SOURCE = Path(__file__).resolve().parent
OUT = ROOT / 'product/playtests/pilot-v2-owner-2026-10-19'


def embedded(value):
    return json.dumps(value, ensure_ascii=False, separators=(',', ':')).replace('<', '\\u003c')


report = json.loads((OUT / 'review.json').read_text())
catalog = json.loads((ROOT / 'content/pilot-v2/levels.json').read_text())
rules = (SOURCE / 'rules.cjs').read_text()
assert report['catalogHash'] == catalog['hash']
assert report['rulesHash'] == hashlib.sha256(rules.encode()).hexdigest()
assert report['sourceHash'] == hashlib.sha256((OUT / 'results.json').read_bytes()).hexdigest()
plan = (ROOT / 'product/plans/gameplay-pilot-v3-plan.md').read_bytes()
page = (SOURCE / 'review-page.html').read_text()
for marker, value in {
    '/*__REPORT__*/': embedded(report), '/*__CATALOG__*/': embedded(catalog),
    '/*__RULES__*/': rules,
    '/*__PLAN_URI__*/': 'data:text/markdown;charset=utf-8;base64,' + base64.b64encode(plan).decode(),
}.items():
    assert page.count(marker) == 1, marker
    page = page.replace(marker, value)
assert '/*__' not in page
(OUT / 'index.html').write_text(page)
print(f'Built {OUT.relative_to(ROOT)}/index.html: {len(page.encode()):,} bytes; offline, read-only.')

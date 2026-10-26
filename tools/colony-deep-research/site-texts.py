"""Which Colony Flow extras the colony-flow.com level guides mention, level by level (1-1317).

The guides are third-party texts written per level; they are hints, not proof (some claims contradict the videos,
e.g. a crate "accepts any colour"). A mention counts only in a sentence without a negation ("there are no hidden
boxes" does not count). Pages after L600 are about a third as long, so their mention rates are lower.

python3 -I tools/colony-deep-research/site-texts.py crawl <out.jsonl>   # fetch the guide texts (not kept in the repo)
python3 -I tools/colony-deep-research/site-texts.py scan <in.jsonl> <out.json>
"""
import concurrent.futures as cf, html, json, os, re, subprocess, sys, collections

TERMS = {
    'hidden_capsule': r'hidden box|question mark (?:block|box)',
    'link': r'connected box|connected block|linked (?:block|box)',
    'lock_key': r'\bkeys?\b[^.]{0,200}\b(?:lock|locked|unlock)|\b(?:lock|locked|unlock)[^.]{0,200}\bkeys?\b',
    'crate_hungry_door': r'hungry door',
    'chocolate': r'chocolate (?:block|bar|cover|tile|piece|square)|chocolate[^.]{0,60}\b\d{2,3}\b (?:hits|ants)',
    'board_hidden_tiles': r'hidden food',
    'several_keys': r'\b(?:two|three|four|both|all|every single) (?:\w+ )?keys\b',
}
NEG = re.compile(r"\b(no|not|without|never|doesn't|does not|isn't|aren't)\b")


def fetch(n):
    s = subprocess.run(['curl', '-sS', '-m', '40', '-L', f'https://colony-flow.com/level/{n}'], capture_output=True).stdout.decode('utf-8', 'replace')
    ps = [html.unescape(re.sub('<[^>]+>', '', p)).strip() for p in re.findall(r'<p[^>]*>(.*?)</p>', s, re.S)]
    return {'level': n, 'text': [p for p in ps if len(p) > 40 and 'AI Screenshot Solver' not in p and 'watch the video above' not in p]}


def main():
    if sys.argv[1] == 'crawl':
        with open(sys.argv[2], 'w') as f, cf.ThreadPoolExecutor(6) as ex:
            for d in ex.map(fetch, range(1, 1318)):
                f.write(json.dumps(d, ensure_ascii=False) + '\n')
        return
    pages = sorted((json.loads(l) for l in open(sys.argv[2])), key=lambda d: d['level'])
    out = {'pages': len(pages), 'wordsPerPage': {}, 'terms': {}}
    for b in range(14):
        g = [p for p in pages if (p['level'] - 1) // 100 == b]
        if g:
            out['wordsPerPage'][f'{b*100+1}-{b*100+len(g)}'] = round(sum(len(' '.join(p['text']).split()) for p in g) / len(g))
    for k, pat in TERMS.items():
        lv = [p['level'] for p in pages if any(re.search(pat, s) and not NEG.search(s) for s in re.split(r'(?<=[.!?])\s+', ' '.join(p['text']).lower()))]
        c = collections.Counter((x - 1) // 100 for x in lv)
        out['terms'][k] = {'levels': lv, 'count': len(lv), 'first': lv[:5], 'per100': [c.get(i, 0) for i in range(14)]}
    json.dump(out, open(sys.argv[3], 'w'), ensure_ascii=False)
    for k, v in out['terms'].items():
        print(f"{k:20s} {v['count']:4d} first {v['first']} per100 {v['per100']}")


if __name__ == '__main__':
    main()

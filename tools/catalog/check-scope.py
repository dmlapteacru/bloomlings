#!/usr/bin/env python3
"""Exercise exact range selection and resume/certification with a fake pipeline; never generate game levels."""
import argparse
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import unittest

REPO = Path(__file__).resolve().parents[2]
parser = argparse.ArgumentParser()
parser.add_argument('--powershell', default=shutil.which('pwsh'))
options, rest = parser.parse_known_args()


class ScopeChecks(unittest.TestCase):
    def runners(self, root=REPO):
        yield ['bash', str(root / 'tools/catalog/build-catalog.sh')], False
        if options.powershell:
            yield [options.powershell, '-NoProfile', '-File', str(root / 'tools/catalog/build-catalog.ps1')], True

    def test_exact_plans_and_shell_parity(self):
        for limit in (50, 2401, 2500, 5000):
            plans = []
            for command, ps in self.runners():
                args = ['-Plan', '-ToLevel', str(limit)] if ps else ['--plan', '--to-level', str(limit)]
                result = subprocess.run(command + args, text=True, capture_output=True, check=True)
                lines = result.stdout.strip().splitlines()
                self.assertEqual(lines[0], f'launch 1-{limit}')
                bands = [line.split() for line in lines[1:]]
                self.assertEqual(int(bands[-1][2]), limit)
                previous = 10
                for _, first, last, _, _, segments in bands:
                    self.assertEqual(int(first), previous + 1)
                    self.assertGreaterEqual(int(segments), 1)
                    self.assertLessEqual(int(segments), max(1, (int(last) - int(first) + 1) // 50))
                    previous = int(last)
                plans.append(lines)
            if len(plans) == 2:
                self.assertEqual(*plans)

    def test_default_boundary_and_invalid_requests(self):
        for command, ps in self.runners():
            result = subprocess.run(command + (['-Plan'] if ps else ['--plan']), capture_output=True, text=True, check=True)
            self.assertEqual(result.stdout.splitlines()[0], 'launch 1-2500')
            for value in ('0', '9', '5001'):
                # PowerShell uses zero as the omitted/default sentinel.
                if ps and value == '0':
                    continue
                args = ['-Plan', '-ToLevel', value] if ps else ['--plan', '--to-level', value]
                self.assertNotEqual(subprocess.run(command + args, capture_output=True).returncode, 0)

    def test_resume_preserves_reserve_and_certifies_only_selected_range(self):
        for score_exit in (0, 1):
            for _, ps in self.runners():
                with self.subTest(powershell=ps, score_exit=score_exit), tempfile.TemporaryDirectory(prefix='bloomlings scope ') as tmp:
                    root = Path(tmp)
                    for name in ('tools/catalog/build-catalog.sh', 'tools/catalog/build-catalog.ps1', 'content/launch.props'):
                        dest = root / name
                        dest.parent.mkdir(parents=True, exist_ok=True)
                        shutil.copyfile(REPO / name, dest)
                    for kind in ('levels', 'validation'):
                        folder = root / 'content/catalog' / kind
                        folder.mkdir(parents=True)
                        for n in range(1, 52):
                            (folder / f'level-{n:05}.json').write_text('reserved-or-existing')
                    (root / 'content/catalog/build-manifest.jsonl').write_text(json.dumps({'band': 'old-5000', 'levels': '1-5000'}, separators=(',', ':')) + '\n')
                    work = root / 'content/work/check'
                    (work / 'pipe').mkdir(parents=True)
                    (work / 'pipe/bloomlings-pipeline.dll').touch()
                    mock = root / 'bin/dotnet'
                    mock.parent.mkdir()
                    mock.write_text('#!' + sys.executable + '\n' + '''import json, os, pathlib, sys
a = sys.argv[2:]
assert a[0] in ('validate', 'score'), a
key = '--catalog' if a[0] == 'validate' else '--defs'
folder = pathlib.Path(a[a.index(key) + 1]) / 'levels'
assert sorted(p.name for p in folder.glob('*.json')) == [f'level-{n:05}.json' for n in range(1, 51)]
with open(os.environ['SCOPE_CALLS'], 'a') as log: log.write(a[0] + '\\n')
print(json.dumps({'errors': 0, 'issues': []}))
sys.exit(int(os.environ['SCORE_EXIT']) if a[0] == 'score' else 0)
''')
                    mock.chmod(0o755)
                    env = dict(os.environ, PATH=str(mock.parent) + os.pathsep + os.environ['PATH'],
                               SCOPE_CALLS=str(root / 'calls.txt'), SCORE_EXIT=str(score_exit))
                    command = [options.powershell, '-NoProfile', '-File', str(root / 'tools/catalog/build-catalog.ps1'), '-ToLevel', '50', '-NoBuild'] if ps else [
                        'bash', str(root / 'tools/catalog/build-catalog.sh'), '--to-level', '50', '--no-build']
                    command += ['-Work' if ps else '--work', 'content/work/check']
                    result = subprocess.run(command, env=env, capture_output=True, text=True, timeout=60)
                    self.assertEqual(result.returncode == 0, score_exit == 0, result.stdout + result.stderr)
                    self.assertEqual((root / 'calls.txt').read_text().splitlines(), ['validate', 'score'])
                    self.assertEqual((root / 'content/catalog/levels/level-00051.json').read_text(), 'reserved-or-existing')


if __name__ == '__main__':
    unittest.main(argv=[sys.argv[0]] + rest)

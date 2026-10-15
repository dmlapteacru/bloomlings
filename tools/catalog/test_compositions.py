"""Regression cases for the art-copy/coverage gate; no production content is modified."""
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

spec = importlib.util.spec_from_file_location('compositions', Path(__file__).with_name('check-compositions.py'))
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class CompositionTests(unittest.TestCase):
    def test_copies_ignore_colors_mirrors_rotations_and_nearest_resize(self):
        grid = [[-1, 0, 0], [1, 0, 2], [1, 2, 2], [1, 1, -1]]
        recolor = [[{0: 9, 1: 4, 2: 7}.get(v, v) for v in row] for row in grid]
        scaled = [[v for v in row for _ in range(2)] for row in recolor for _ in range(3)]
        self.assertEqual(module.signature(grid), module.signature([row[::-1] for row in scaled]))
        self.assertEqual(module.signature(grid), module.signature(list(zip(*grid))))
        different = [row[:] for row in grid]
        different[2][1] = 0
        self.assertNotEqual(module.signature(grid), module.signature(different))

    def test_complete_checks_actual_references_and_visual_approval(self):
        with tempfile.TemporaryDirectory() as tmp:
            folder = Path(tmp)
            entry = {'compositionId': 'scene', 'pictureId': 'art', 'version': 1, 'width': 2, 'height': 2, 'artStatus': 'approved'}
            registry = {'launchCompositionTarget': 1, 'policy': {'oneCompositionPerLaunchLevel': True, 'resizeMirrorRecolorAreNewComposition': False}, 'compositions': [entry]}
            (folder / 'art.json').write_text(json.dumps({'id': 'art', 'version': 1, 'width': 2, 'height': 2, 'grid': [[0, 1], [2, 0]]}))
            level = {'levelNumber': 1, 'picture': {'id': 'art', 'version': 1}}
            (folder / 'level-00001.json').write_text(json.dumps(level))
            self.assertEqual([], module.check(registry, folder, folder, complete=True, target=1))
            entry['artStatus'] = 'pilot-awaiting-owner-review'
            self.assertTrue(any('approval' in e for e in module.check(registry, folder, folder, True, 1)))
            entry['artStatus'] = 'approved'
            level['picture']['id'] = 'missing'
            (folder / 'level-00001.json').write_text(json.dumps(level))
            self.assertTrue(any('no registered' in e for e in module.check(registry, folder, folder, True, 1)))

    def test_same_scene_cannot_be_used_again_outside_legacy_window(self):
        with tempfile.TemporaryDirectory() as tmp:
            folder = Path(tmp)
            entry = {'compositionId': 'scene', 'pictureId': 'art', 'version': 1, 'width': 2, 'height': 2}
            registry = {'launchCompositionTarget': 2500, 'policy': {'oneCompositionPerLaunchLevel': True, 'resizeMirrorRecolorAreNewComposition': False}, 'compositions': [entry]}
            (folder / 'art.json').write_text(json.dumps({'id': 'art', 'version': 1, 'width': 2, 'height': 2, 'grid': [[0, 1], [2, 0]]}))
            for n in (1, 601):
                (folder / f'level-{n:05}.json').write_text(json.dumps({'levelNumber': n, 'picture': {'id': 'art', 'version': 1, 'mirror': 'horizontal' if n == 601 else 'none'}}))
            self.assertTrue(any('already used by L1' in e for e in module.check(registry, folder, folder)))


if __name__ == '__main__':
    unittest.main()

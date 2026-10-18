"""Regression cases for generated garments with coincident, separate sleeves."""
import unittest

import numpy as np

from contact import separate_sleeves, split_contacts
from skinning import world_vector, owned_fields


class SourceSleeveTests(unittest.TestCase):
    # Each sleeve shares a position with the torso, but has its own vertex ID.
    # An inner cuff at x=.15 must stay with its sleeve despite a torso-width gate.
    points = np.array([
        [-.15, 0, .30], [.15, 0, .30], [0, 0, .15],
        [.15, 0, .30], [.24, 0, .22], [.25, .05, .28],
        [-.15, 0, .30], [-.24, 0, .22], [-.25, .05, .28],
    ])
    faces = np.array([[0, 1, 2], [3, 4, 5], [6, 8, 7]])
    source_weights = {
        'Chest': np.array([1., 1., 1., 0, 0, 0, 0, 0, 0]),
        'Forearm.L': np.array([0., 0, 0, .94, .94, .94, 0, 0, 0]),
        'Forearm.R': np.array([0., 0, 0, 0, 0, 0, .94, .94, .94]),
    }

    def test_coincident_parts_keep_the_whole_sleeve_with_the_arm(self):
        for family in ('twig', 'sprig', 'bloom', 'drop'):
            with self.subTest(family=family):
                world = np.array([world_vector(p, family) for p in self.points])
                ownership, report = separate_sleeves(world, self.faces, self.source_weights, family)
                np.testing.assert_array_equal(ownership, [0, 0, 0, 1, 1, 1, 1, 1, 1])
                self.assertEqual(report['sourceComponents'], 3)
                self.assertEqual({r['side'] for r in report['sleeves']}, {'L', 'R'})

    def test_a_real_surface_bridge_requires_contact_repair(self):
        # Adding an indexed bridge genuinely joins the left sleeve to the torso.
        # It must not turn that entire garment component into an arm.
        joined = np.vstack([self.faces, [1, 4, 5]])
        self.assertIsNone(separate_sleeves(self.points, joined, self.source_weights, 'twig'))

    def test_coincident_uv_seam_keeps_exportable_contact_faces(self):
        # The source seam has distinct vertex IDs at the same position. Interior
        # rings may share those positions; FBX must not silently lose their faces.
        points = np.array([[.2, -.02, .25], [.2, .02, .25], [.2, .02, .25],
                           [.2, .02, .30], [.2, -.02, .30]])
        faces = np.array([[0, 1, 3], [0, 3, 4]])
        loops = np.arange(6).reshape(2, 3)
        uv = np.array([[0., 0], [1., 0], [1., 1], [0., 0], [1., 1], [0., 1]])
        normals = np.tile([1., 0, 0], (6, 1))
        rim = [(i, (i+1) % 5, np.zeros(2), np.zeros(2), 0, 1,
                np.array([1., 0, 0]), np.array([1., 0, 0])) for i in range(5)]
        result = split_contacts(points, faces, loops, uv, normals, np.zeros(2, int),
                                np.ones(5), existing_seams=rim)
        exterior = result['source_faces'] >= 0
        np.testing.assert_array_equal(result['faces'][exterior], faces)
        np.testing.assert_array_equal(result['uv'][:6], uv)
        self.assertGreater(int((~exterior).sum()), 0)
        self.assertTrue(all(len(set(row)) == 3 for row in result['faces']))
        self.assertEqual(len(result['faces'])*3, len(result['uv']))
        self.assertEqual(len(result['faces'])*3, len(result['normals']))
        self.assertEqual(len(result['faces']), len(result['material_ids']))
        self.assertTrue(np.isfinite(result['points']).all())

    def test_identified_cuff_overrides_incorrect_source_torso_weights(self):
        for family in ('twig', 'sprig', 'bloom', 'drop'):
            for garment in ('Jacket', 'Coat', 'Overalls'):
                with self.subTest(family=family, garment=garment):
                    points = np.array([world_vector(p, family) for p in
                                       [(.15, -.03, .23), (.30, .03, .22)]])
                    names, weights = owned_fields(points, family, garment,
                                                  {'Chest': np.ones(2)}, np.ones(2))
                    torso = [i for i,n in enumerate(names) if n in ('Hips', 'Spine', 'Chest')]
                    hand = [i for i,n in enumerate(names) if n.startswith('Hand.')]
                    arm = [i for i,n in enumerate(names) if n.startswith(
                           ('UpperArm.', 'Forearm.', 'ShoulderBlend.', 'ElbowBlend.'))]
                    np.testing.assert_allclose(weights.sum(1), 1, atol=1e-7)
                    np.testing.assert_allclose(weights[:, arm].sum(1), 1, atol=1e-7)
                    self.assertEqual(float(weights[:, torso].sum()), 0)
                    self.assertEqual(float(weights[:, hand].sum()), 0)
                    names, weights = owned_fields(points, family, garment,
                                                  {'Forearm.L': np.ones(2)}, np.zeros(2))
                    torso = [i for i,n in enumerate(names) if n in ('Hips', 'Spine', 'Chest')]
                    np.testing.assert_allclose(weights[:, torso].sum(1), 1, atol=1e-7)


if __name__ == '__main__':
    unittest.main()

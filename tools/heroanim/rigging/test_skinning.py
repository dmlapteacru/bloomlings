"""Regression checks for the clothing deformation defects, independent of Blender."""
import unittest
import numpy as np
from skinning import fields, world_vector, normalized, joint_blends


class SkinningTests(unittest.TestCase):
    def sample(self, family, part, points, head=None, original=None, helpers=False, zones=None):
        world = np.array([world_vector(v, family) for v in points])
        if original is None:
            x = np.array(points)[:, 0]
            original = {"Forearm.L": (x >= 0) * .6, "Hand.L": (x >= 0) * .4,
                        "Forearm.R": (x < 0) * .6, "Hand.R": (x < 0) * .4}
            if head is not None:
                original = {n: w * (1-head) for n, w in original.items()}
                original['Head'] = head
        return fields(world, family, part, original=original, helpers=helpers, zones=zones)

    def test_front_back_and_centre_panels_do_not_follow_arms(self):
        for family in ("twig", "sprig", "bloom", "drop"):
            width = .124 if family == "drop" else .159
            points = [(x, d, z) for x in [-width, 0, width]
                      for d in [-.16, .03, .19] for z in [.16, .24, .32, .38]]
            for part in ("Jacket", "Coat", "Overalls"):
                names, values = self.sample(family, part, points)
                arm = [i for i, n in enumerate(names) if n.startswith(("UpperArm", "Forearm", "Hand"))]
                self.assertEqual(0.0, float(values[:, arm].max()), (family, part))

    def test_coat_and_jacket_hems_do_not_follow_feet_or_knees(self):
        for family in ("twig", "sprig", "bloom", "drop"):
            points = [(x, d, z) for x in [-.19, -.10, 0, .10, .19]
                      for d in [-.10, .08, .19] for z in [.07, .10, .13, .17]]
            for part in ("Jacket", "Coat"):
                names, values = self.sample(family, part, points, original={'Foot.L':np.ones(len(points))})
                legs = [i for i, n in enumerate(names) if n.startswith(("Thigh", "Shin", "Foot"))]
                self.assertEqual(0.0, float(values[:, legs].max()), (family, part))

    def test_cuffs_follow_forearms_and_do_not_twist_with_the_hand(self):
        for family in ("twig", "sprig", "bloom", "drop"):
            x, z = (.26, .21) if family == "drop" else (.29, .25)
            names, w = self.sample(family, "Jacket", [(x, .035, z)])
            self.assertGreater(float(w[0, names.index("Forearm.L")]), .99)
            self.assertEqual(0.0, float(w[0, names.index("Hand.L")]))
            self.assertEqual(0.0, float(w[0, names.index("Forearm.R")]))

    def test_headwear_glasses_and_scarves_have_single_rigid_parent(self):
        points = [(0, 0, .33), (.30, .20, .70)]
        for family in ("twig", "sprig", "bloom", "drop"):
            for part in ("Glasses", "BucketHat", "HairPetals", "NativeDrop", "Scarf", "ScarfKnit"):
                names, w = self.sample(family, part, points)
                self.assertEqual(["Chest" if part.startswith("Scarf") else "Head"], names)
                np.testing.assert_array_equal(w, np.ones((2, 1)))

    def test_weights_are_normalized_mirrored_and_portable(self):
        random = np.random.default_rng(781)
        points = random.uniform([0, -.20, 0], [.40, .30, 1.1], (2000, 3))
        for family in ("twig", "sprig", "bloom", "drop"):
            for part in ("base", "Jacket", "Coat", "Overalls"):
                head = np.where(points[:, 2] > .37, 1.0, 0.0)
                names, w = self.sample(family, part, points, head, helpers=True)
                mirror = points.copy(); mirror[:, 0] *= -1
                mirror_names, mirror_w = self.sample(family, part, mirror, head, helpers=True)
                self.assertTrue(np.isfinite(w).all() and np.all(w >= 0))
                np.testing.assert_allclose(w.sum(axis=1), 1, atol=1e-7)
                self.assertLessEqual(int((w > 0).sum(axis=1).max()), 4)
                for i, name in enumerate(names):
                    mirrored_name = name[:-1] + ("R" if name.endswith(".L") else "L") if name.endswith((".L", ".R")) else name
                    np.testing.assert_allclose(w[:, i], mirror_w[:, mirror_names.index(mirrored_name)], atol=1e-7)

    def test_closed_sleeves_do_not_share_arm_weights_with_the_torso(self):
        for family in ('twig','sprig','bloom','drop'):
            points=[(.18,.03,.25),(.24,.03,.26),(-.24,.03,.26)]
            names,w=self.sample(family,'Jacket',points,zones=np.array([0,1,2]),helpers=True)
            arm=[i for i,n in enumerate(names)if n.startswith(('UpperArm','Forearm','Hand','ShoulderBlend','ElbowBlend'))]
            self.assertEqual(0,float(w[0,arm].sum()))
            self.assertAlmostEqual(1,float(w[1,arm].sum()))
            self.assertAlmostEqual(1,float(w[2,arm].sum()))

    def test_half_angle_support_reduces_joint_volume_loss(self):
        names,w=normalized(joint_blends({'Chest':np.array([.5]),'UpperArm.L':np.array([.5])}))
        rotations={'Chest':0,'UpperArm.L':np.pi/2,'ShoulderBlend.L':np.pi/4}
        position=sum(w[0,i]*np.array([np.cos(rotations.get(n,0)),np.sin(rotations.get(n,0))])for i,n in enumerate(names))
        self.assertGreater(float(np.linalg.norm(position)),.85)

    def test_drop_arm_cross_section_does_not_stretch_between_different_bones(self):
        start=np.array([.085,.32]);axis=np.array([.178,-.16]);perp=np.array([.16,.178]);perp/=np.linalg.norm(perp)
        for fraction in [.25,.50,.70,.95]:
            center=start+axis*fraction
            points=[(p[0],.035,p[1])for p in [center+perp*.025,center-perp*.025]]
            names,w=self.sample('drop','Base_Arm_L',points,helpers=True)
            np.testing.assert_allclose(w[0],w[1],atol=1e-6)

    def test_clean_sleeve_ring_has_uniform_weights_and_no_hand_ownership(self):
        for family in ('twig','sprig','bloom','drop'):
            points=np.array([world_vector(v,family) for v in [(.20,.01,.29),(.25,.09,.24)]])
            original={'Chest':np.ones(2)}
            names,w=fields(points,family,'Jacket',original=original,helpers=True,zones=np.array([6,6]),arm_parameters=np.array([.5,.5]))
            np.testing.assert_allclose(w[0],w[1],atol=1e-7)
            self.assertEqual(0,float(w[:,names.index('Hand.L')].max()))


if __name__ == "__main__":
    unittest.main()

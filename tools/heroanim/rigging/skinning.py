"""Anatomical weight fields in the accepted heroes' common side/front/up coordinates.

Torso ownership is explicit. Nearest-surface transfer across the narrow armpit must never
give a front panel, pocket, collar or coat hem a hand/arm/leg influence.
"""
import numpy as np


def smooth(lo, hi, value):
    t = np.clip((np.asarray(value) - lo) / (hi - lo), 0.0, 1.0)
    return t * t * (3.0 - 2.0 * t)


def canonical(points, family):
    a = np.asarray(points)
    return a[:, [1, 0, 2]] if family == "drop" else a * np.array([1.0, -1.0, 1.0])


def world_vector(value, family):
    x, y, z = value
    return (y, x, z) if family == "drop" else (x, -y, z)


def normalized(rows):
    names = list(rows)
    values = np.stack([np.asarray(rows[n]) for n in names], axis=1)
    assert np.isfinite(values).all() and values.min() >= -1e-8
    values = np.maximum(values, 0.0)
    # FBX and the future 2D bake use exactly the same four-weight, linear skinning.
    order = np.argsort(-values, axis=1, kind="stable")
    keep = np.zeros_like(values, dtype=bool)
    np.put_along_axis(keep, order[:, :4], True, axis=1)
    values[~keep] = 0.0
    values[values < 1e-6] = 0.0
    total = values.sum(axis=1)
    assert total.min() > 0.0, "Unweighted vertex"
    values /= total[:, None]
    return names, values


def torso(z, family):
    chest_low, chest_high = (.205, .305) if family == "drop" else (.205, .315)
    chest = smooth(chest_low, chest_high, z)
    spine = smooth(.13, .215, z)
    return {"Hips": (1.0 - chest) * (1.0 - spine),
            "Spine": (1.0 - chest) * spine, "Chest": chest}


def anatomical_arm(points,family,original,suffix):
    x,depth,z=canonical(points,family).T
    side=np.abs(x)
    if family=='drop':
        start=np.array([.085,.32]);elbow=np.array([.17,.235]);wrist=np.array([.224,.178]);end=np.array([.263,.16])
    else:
        start=np.array([.1752,.3245]);elbow=np.array([.2336,.2596]);wrist=np.array([.2726,.2077]);end=np.array([.2806,.143])
    axis=end-start;length2=np.dot(axis,axis)
    t=((side-start[0])*axis[0]+(z-start[1])*axis[1])/length2
    elbow_at=np.dot(elbow-start,axis)/length2;wrist_at=np.dot(wrist-start,axis)/length2
    names=['UpperArm.'+suffix,'Forearm.'+suffix,'Hand.'+suffix]
    amount=sum((original.get(n,np.zeros(len(x)))for n in names),np.zeros(len(x)))
    # Preserve the supplied arm/torso boundary and all other anatomical regions.
    # Repartition only the existing arm influence along its actual centre line.
    bend=smooth(elbow_at-.14,elbow_at+.14,t)
    previous_hand=np.divide(original.get(names[2],np.zeros(len(x))),amount,out=np.zeros(len(x)),where=amount>1e-8)
    hand=np.maximum(smooth(wrist_at-.10,wrist_at+.10,t),previous_hand)
    return {names[0]:amount*(1-bend),names[1]:amount*bend*(1-hand),names[2]:amount*bend*hand}


def joint_blends(rows):
    """A half-angle deform bone preserves volume without FBX-incompatible dual quaternions."""
    rows = {n: np.array(v, dtype=float, copy=True) for n, v in rows.items()}
    count = len(next(iter(rows.values())))
    for side in ("L", "R"):
        for parent, child, helper in [("Chest", "UpperArm.", "ShoulderBlend."),
                                      ("UpperArm.", "Forearm.", "ElbowBlend."),
                                      ("Thigh.", "Shin.", "KneeBlend."),
                                      ("Shin.", "Foot.", "AnkleBlend.")]:
            a = parent if parent == "Chest" else parent + side
            b = child + side
            wa = rows.get(a, np.zeros(count))
            wb = rows.get(b, np.zeros(count))
            total = wa + wb
            mixed = np.divide(wa * wb, total, out=np.zeros(count), where=total > 1e-8)
            rows[a] = wa - mixed
            rows[b] = wb - mixed
            rows[helper + side] = rows.get(helper + side, np.zeros(count)) + 2.0 * mixed
    return rows


def fields(points, family, part, original=None, helpers=False):
    """Return conservative, continuous weights without changing a rest vertex or UV loop."""
    x, depth, z = canonical(points, family).T
    side = np.abs(x)
    count = len(x)
    ones = np.ones(count)
    if part not in ("base", "DropHeroV2", "Base_Body", "Base_Neck", "Base_Arm_L", "Base_Arm_R",
                    "Base_Leg_L", "Base_Leg_R", "Base_Head", "Base_Tail", "Jacket", "Coat", "Overalls"):
        return normalized({"Chest" if part.startswith("Scarf") else "Head": ones})
    if part == "Base_Head":
        return normalized({"Head": ones})
    if part == "Base_Tail":
        return normalized({"Tail": ones})

    clothes = part in ("Jacket", "Coat", "Overalls")
    if original is not None:
        if not clothes:
            if part=='base' or part.startswith('Base_Arm_'):
                rows={n:value.copy()for n,value in original.items()}
                for suffix in ('L','R'):
                    rows.update(anatomical_arm(points,family,original,suffix))
                return normalized(joint_blends(rows) if helpers else rows)
            # The supplied toy hands, feet and face already have a useful anatomical
            # segmentation. Keep it instead of reassigning their underside by distance.
            return normalized(joint_blends(original) if helpers else original)
        shoulder = .085 if family == "drop" else .1752
        elbow = .17 if family == "drop" else .2336
        inner = .125 if family == "drop" else .16
        sleeve_gate = smooth(inner, inner + .045, side)
        arm_depth = .035 if family == "drop" else .030
        sleeve_gate *= 1.0 - smooth(.180, .235, np.abs(depth - arm_depth))
        arms = {}
        arm_total = np.zeros(count)
        for suffix, mask in [("L", x >= 0), ("R", x < 0)]:
            upper = original.get("UpperArm." + suffix, np.zeros(count)) * mask
            fore = (original.get("Forearm." + suffix, np.zeros(count)) + original.get("Hand." + suffix, np.zeros(count))) * mask
            if family == "drop" and part == "Overalls":
                # The old Drop overall delivery had no arm weights, including its
                # short sleeves. Give the sleeve its actual shoulder ownership.
                amount = smooth(.13, .205, side) * mask
                bend = smooth(elbow - .025, elbow + .025, side)
                upper, fore = amount * (1 - bend), amount * bend
            arms["UpperArm." + suffix] = upper * sleeve_gate
            arms["Forearm." + suffix] = fore * sleeve_gate
            arms["Hand." + suffix] = np.zeros(count)
            arm_total += arms["UpperArm." + suffix] + arms["Forearm." + suffix]
        legs = {n: np.zeros(count) for n in original if n.startswith(("Thigh.", "Shin.", "Foot."))}
        if part == "Overalls":
            legs = {n: original[n].copy() for n in legs}
        leg_total = sum(legs.values(), np.zeros(count))
        available = 1.0 - arm_total
        scale = np.minimum(1.0, np.divide(available, leg_total, out=np.ones(count), where=leg_total > 1e-8))
        legs = {n: value * scale for n, value in legs.items()}
        body = np.maximum(0.0, 1.0 - arm_total - leg_total * scale)
        rows = {n: value * body for n, value in torso(z, family).items()}
        rows.update(arms)
        rows.update(legs)
        return normalized(joint_blends(rows) if helpers else rows)

    raise ValueError("Body and garment repair requires the supplied anatomical weights")


def owned_fields(points, family, part, original, ownership, helpers=True):
    """Skin a verified sleeve/body ownership without a position gate across its cuff."""
    x, depth, z = canonical(points, family).T
    amount = np.asarray(ownership, dtype=float)
    count = len(amount)
    clothes = part in ('Jacket', 'Coat', 'Overalls')
    arms = {}
    for suffix, mask in [('L', x >= 0), ('R', x < 0)]:
        hand = np.zeros(count)
        if not clothes:
            previous = sum((original.get(n+suffix, np.zeros(count)) for n in ('UpperArm.', 'Forearm.', 'Hand.')), np.zeros(count))
            hand = np.divide(original.get('Hand.'+suffix, np.zeros(count)), previous, out=np.zeros(count), where=previous > 1e-8)
        rows = anatomical_arm(points, family, {'UpperArm.'+suffix: amount*mask*(1-hand), 'Hand.'+suffix: amount*mask*hand}, suffix)
        if clothes:
            rows['Forearm.'+suffix] += rows['Hand.'+suffix]
            rows['Hand.'+suffix] *= 0
        arms.update(rows)
    if clothes:
        legs = {n: w.copy() for n, w in original.items() if part == 'Overalls' and n.startswith(('Thigh.', 'Shin.', 'Foot.'))}
        leg_total = sum(legs.values(), np.zeros(count))
        scale = np.minimum(1, np.divide(1-amount, leg_total, out=np.ones(count), where=leg_total > 1e-8))
        legs = {n: w*scale for n, w in legs.items()}
        rows = {n: w*np.maximum(0, 1-amount-leg_total*scale) for n, w in torso(z, family).items()}
        rows.update(legs)
    else:
        others = {n: w for n, w in original.items() if not n.startswith(('UpperArm.', 'Forearm.', 'Hand.'))}
        total = sum(others.values(), np.zeros(count))
        rows = {n: np.divide(w, total, out=np.zeros(count), where=total > 1e-8)*(1-amount) for n, w in others.items()}
        for n, w in torso(z, family).items():
            rows[n] = rows.get(n, np.zeros(count)) + np.where(total < 1e-8, (1-amount)*w, 0)
    rows.update(arms)
    return normalized(joint_blends(rows) if helpers else rows)

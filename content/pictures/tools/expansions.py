"""The expansion variants' color roles (FR-060; the roadmap adds Vine, ColorGroup.Lime #FFFF3C, at L45 and Berry,
ColorGroup.Red #810F00, at L200). A role may only map to a variant of its own color group (FR-006), so a level can show
Vine or Berry only on a picture with a lime or a red role. Each subject names the roles that read yellow (Vine is a
bright yellow: suns, stars, honey, a yellow duck) or dark red (cherries, roofs, a red car), and sketch_pictures.py
draws expansion pictures with those roles moved to the lime or red group (`python3 sketch_pictures.py --expansions`).

Lime and red each hold one variant, so such a role always maps to Vine or Berry, and a picture with one of them carries
five color groups: five distinct variants at least, as the regular levels from L45 use.
"""

LIME, RED = 'lime', 'red'

# subject -> {group: [(roleId, new name or None), ...]}
ROLES = {
    'flower_pot': {LIME: [('center', None)], RED: [('petal', 'Red petals')]},
    'daisy_field': {LIME: [('center', None)], RED: [('petal2', 'Red petals')]},
    'tulip_bed': {LIME: [('tulip2', 'Yellow tulips')], RED: [('tulip', 'Red tulips')]},
    'fruit_tree': {LIME: [('sun', None)], RED: [('fruit', 'Apples')]},
    'apple': {RED: [('apple', None)]},
    'pear': {LIME: [('pear', None)]},
    'cherries': {RED: [('cherry', None)]},
    'strawberry': {LIME: [('seed', None)], RED: [('berry', None)]},
    'grapes': {RED: [('grape2', 'Red grapes')]},
    'pumpkin': {LIME: [('flowers', 'Pumpkin flowers')]},
    'mushroom': {LIME: [('small_cap', 'Yellow cap')], RED: [('cap', None)]},
    'butterfly': {LIME: [('flower', 'Flowers')], RED: [('wing', None)]},
    'bee': {LIME: [('body', None)], RED: [('flower', None)]},
    'snail': {LIME: [('sun', None)], RED: [('blossom', None)]},
    'ladybug': {LIME: [('flower', None)], RED: [('shell', None)]},
    'fish': {LIME: [('fish', None)], RED: [('fin', None)]},
    'bird': {LIME: [('sun', None)], RED: [('bird', None)]},
    'frog': {LIME: [('belly', None)], RED: [('blossom', None)]},
    'watering_can': {LIME: [('flower_center', None)], RED: [('flowers', None)]},
    'birdhouse': {LIME: [('sun', None)], RED: [('roof', None)]},
    'mug': {RED: [('mug', None)]},
    'umbrella': {LIME: [('canopy', None)], RED: [('panel', None)]},
    'cottage': {LIME: [('window', 'Lit windows')], RED: [('roof', None)]},
    'sailboat': {LIME: [('sun', None)], RED: [('jib', 'Red sail')]},
    'cactus': {LIME: [('sun', None)], RED: [('flower', None)]},
    'hedgehog': {LIME: [('sun', None)], RED: [('apple', None)]},
    'owl': {LIME: [('stars', None)]},
    'house': {LIME: [('sun', None)], RED: [('roof', None)]},
    'castle': {LIME: [('window', 'Lit windows')], RED: [('roof', None)]},
    'lighthouse': {LIME: [('light', None), ('beam', None)], RED: [('tower', None)]},
    'windmill': {LIME: [('flowers', None)], RED: [('cap', None)]},
    'barn': {LIME: [('hay', None)], RED: [('barn', None)]},
    'tent': {LIME: [('stars', None)], RED: [('flag', None)]},
    'tree_house': {LIME: [('sun', None)], RED: [('roof', None)]},
    'ship': {LIME: [('stripe', None)], RED: [('buoy', None), ('funnel_top', None)]},
    'rowboat': {LIME: [('sun', None)], RED: [('boat', None)]},
    'car': {LIME: [('lamp', None)], RED: [('body', None)]},
    'bus': {LIME: [('bus', None)], RED: [('stripe', None)]},
    'train': {LIME: [('wheel', None)], RED: [('boiler', None)]},
    'tractor': {LIME: [('sun', None)], RED: [('body', None)]},
    'airplane': {LIME: [('plane', None)], RED: [('tail', None)]},
    'hot_air_balloon': {LIME: [('gore', None)], RED: [('balloon', None)]},
    'rocket': {LIME: [('flame_core', None), ('stars', None)], RED: [('nose', None)]},
    'submarine': {LIME: [('portholes', 'Lit portholes')], RED: [('periscope', None)]},
    'helicopter': {LIME: [('body', None)], RED: [('skid', None)]},
    'bicycle': {LIME: [('sun', None)], RED: [('frame', None)]},
    'beach_ball': {LIME: [('yellow', None)], RED: [('red', None)]},
    'kite': {LIME: [('panel', None)], RED: [('kite', None)]},
    'teddy_bear': {LIME: [('block2', 'Yellow block')], RED: [('bow', None)]},
    'rubber_duck': {LIME: [('duck', None), ('wing', None)]},
    'sandcastle': {LIME: [('sun', None)], RED: [('bucket', None)]},
    'robot': {LIME: [('eye', None)], RED: [('mouth', None)]},
    'crown': {LIME: [('crown', None), ('band', None)], RED: [('ruby', None)]},
    'treasure_chest': {LIME: [('gold', None)], RED: [('gems', None)]},
    'guitar': {LIME: [('notes', None)], RED: [('body', None)]},
    'drum': {LIME: [('rim', None)], RED: [('drum', None)]},
    'snowman': {RED: [('scarf', None)]},
    'rainbow': {LIME: [('yellow', None), ('sun', None)], RED: [('red', None)]},
    'moon_and_stars': {LIME: [('moon', None), ('star', None)], RED: [('roof', None)]},
    'gift_box': {LIME: [('ribbon', 'Golden ribbon')], RED: [('box', None)]},
    'balloons': {LIME: [('orange', 'Yellow balloon')], RED: [('pink', 'Red balloon')]},
    'alarm_clock': {LIME: [('bell', None)], RED: [('frame', None)]},
    'igloo': {LIME: [('stars', None)]},
    'palm_island': {LIME: [('sand', None)], RED: [('sun', None), ('starfish', None)]},
    'fir_tree': {LIME: [('star', None)], RED: [('bauble', None)]},
    'ferris_wheel': {LIME: [('cabin', 'Yellow cabins')], RED: [('rim', None)]},
    'cat': {LIME: [('eye', None), ('window', 'Sunny window')], RED: [('yarn', None)]},
    'dog': {LIME: [('sun', None)], RED: [('tongue', None)]},
    'rabbit': {RED: [('flowers', None)]},
    'bear': {LIME: [('honey', None)], RED: [('pot', None)]},
    'panda': {LIME: [('leaves', None), ('flowers', None)]},
    'penguin': {LIME: [('cheek', None)]},
    'fox': {LIME: [('leaves', None)], RED: [('fox', None)]},
    'owlets': {LIME: [('eye', None)]},
    'turtle': {LIME: [('sun', None)], RED: [('flowers', None)]},
    'whale': {LIME: [('sun', None)]},
    'octopus': {RED: [('octopus', None)]},
    'elephant': {LIME: [('sun', None)]},
    'giraffe': {LIME: [('giraffe', None)]},
    'pig': {LIME: [('sun', None)]},
    'chick': {LIME: [('chick', None)], RED: [('flowers', None)]},
    'crab': {LIME: [('sun', None)], RED: [('crab', None), ('claw', None)]},
    'parrot': {LIME: [('tail', None)], RED: [('parrot', None)]},
    'sheep': {LIME: [('flowers', None)]},
    'lion': {LIME: [('sun', None)]},
    'mouse': {LIME: [('cheese', None)]},
    'cake': {LIME: [('flames', None)], RED: [('cherry', None)]},
    'ice_cream': {LIME: [('cone', None)], RED: [('cherry', None)]},
    'cupcake': {LIME: [('wrapper', None)], RED: [('cherry', None)]},
    'lollipop': {LIME: [('swirl2', 'Yellow swirl')], RED: [('swirl', 'Red swirl')]},
    'teapot': {RED: [('cup', None)]},
    'donut': {LIME: [('sprinkle_gold', None)], RED: [('icing', None)]},
    'watermelon': {LIME: [('sun', None)], RED: [('flesh', None)]},
    'pineapple': {LIME: [('fruit', None)], RED: [('table', None)]},
    'popsicle': {LIME: [('middle', 'Yellow middle')], RED: [('top', 'Red top')]},
    'honey_pot': {LIME: [('honey', None)], RED: [('label', None)]},
    'pancakes': {LIME: [('butter', None)], RED: [('strawberry', None)]},
}


def subjects(all_subjects, group):
    """The subjects with a role of `group`, in the order of `all_subjects`."""
    return [s for s in all_subjects if group in ROLES.get(s.__name__, {})]


def recolor(subject, roles, groups):
    """The roles with the subject's roles of `groups` moved to those groups (and renamed where the table says)."""
    moved = {}
    for group in groups:
        for role_id, name in ROLES.get(subject.__name__, {}).get(group, []):
            moved[role_id] = (group, name)
    out = []
    for char, role_id, name, group, background in roles:
        if role_id in moved:
            group, new_name = moved[role_id]
            name = new_name or name
        out.append((char, role_id, name, group, background))
    return out

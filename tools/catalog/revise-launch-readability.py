#!/usr/bin/env python3
"""Apply the owner's readability ceiling without moving slots or replacing semantic composition identities."""
import json
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2] / 'content/launch-art'
POLICY = (
    'Readability revision 3: EXTREMELY SIMPLE chunky low-resolution GAME ICONS, not illustrations. '
    'Draw EACH entire panel on a 16 by 18 logical-pixel grid, then enlarge with nearest-neighbor edges. '
    'The subject itself is about 12 by 14 big square pixels. Every visible edge follows that coarse grid. '
    'ONE recognizable outer silhouette, just 5 to 8 broad flat color regions, at most ONE large identifying feature. '
    'The feature is one solid simple shape, never a picture inside a picture. '
    'Omit all interior scenes, miniature objects, windows with contents, textures, ornament, speckles, '
    'thin lines, fine outlines, highlights, shadows, leaf veins, clock ticks, strings, spokes and scenery. '
    'Do not add a leaf base, flower decorations or secondary props to fill space. '
    'Animals have solid body patches and a single square eye, no fur or feathers. '
    'One or two chunky notches can suggest a feature; no intricate contours. '
    'A bigger target board does NOT permit more detail. Leave a clean cyan margin on all sides. '
)
HOME = {'seed_bank':'one huge seed-shaped window','greenhouse':'one large window containing a single broad leaf',
 'bakery':'one oversized round bun in a single window','post_office':'one huge envelope-shaped window',
 'library':'one big open-book-shaped door','train_station':'one broad rail arch cut into the main object',
 'music_room':'one huge horn integrated into the roof','observatory':'one oversized telescope through the roof',
 'tea_house':'one broad cup-shaped window','clock_tower':'one huge round clock face',
 'pottery_studio':'one huge clay-pot-shaped door','flower_shop':'one giant blossom above the doorway'}
COSMIC = {'seed_harvest':'one oversized seed scoop attached to the main silhouette',
 'floating_music':'one oversized horn attached to the main silhouette','tide_machine':'one broad water wheel inside the main silhouette',
 'kite_port':'one huge kite-shaped sail attached to the main silhouette','cloud_fishing':'one oversized fish-shaped cloud on a short thick hook',
 'lantern_forge':'one giant lantern fused into the main silhouette','petal_carousel':'one broad three-petal rotor inside the silhouette',
 'rain_arch':'one thick waterfall-shaped opening','leaf_ferry':'one broad leaf-shaped boat hull',
 'garden_stair':'one broad three-step staircase cut into the silhouette','parcel_route':'one oversized envelope attached as a sail',
 'season_clock':'one broad four-leaf clock face'}
WORLD = {'river_market':'one oversized striped canopy over a single broad blue band',
 'tide_pool':'one enormous curled shell filling the opening','desert_oasis':'one huge palm silhouette filling the opening',
 'star_garden':'one enormous star-shaped flower filling the opening'}
META = {'river_pour':'one thick river-shaped spout attached to the object',
 'island_unfold':'one broad island-shaped opening containing one big palm',
 'flower_machine':'one giant flower-shaped gear integrated into the object',
 'stairs_spill':'one broad three-step staircase growing from the object',
 'paper_flight':'one pair of broad folded-paper wings attached to the object',
 'lamp_garden':'one large glowing blossom filling the main opening',
 'cloud_portal':'one broad cloud-shaped opening through the object',
 'seed_symphony':'one giant seed-shaped musical key integrated into the object',
 'rainbow_wheel':'one giant three-petal wheel integrated into the object',
 'water_swing':'one oversized leaf swing inside the object',
 'moon_blossom':'one oversized glowing blossom attached to the object',
 'tidal_bridge':'one broad leaf-shaped drawbridge attached to the object'}
HOME.update({'bee_hotel':'one huge three-cell honeycomb window','boat_dock':'one broad leaf-shaped landing platform',
 'clockmaker':'one enormous clock-face window','flower_market':'one giant blossom-shaped doorway',
 'kite_workshop':'one giant kite-shaped roof','lighthouse':'one broad glowing lamp opening',
 'music_pavilion':'one enormous horn-shaped opening','planetarium':'one large ringed-planet-shaped window',
 'pottery_kiln':'one broad glowing arch opening','puppet_theater':'one oversized puppet-face window',
 'tea_stand':'one giant cup-shaped doorway','tiny_circus':'one broad striped tent roof',
 'train_station':'one giant locomotive-shaped window','weaving_workshop':'one broad two-color woven panel'})
WORLD.update({'canyon_bridge':'one huge arch-shaped opening with a single thick bridge',
 'cloud_port':'one broad cloud-shaped platform','coral_harbor':'one enormous three-branch coral motif filling the opening',
 'crystal_cave':'one enormous faceted-crystal-shaped window','floating_island':'one broad floating-island-shaped base',
 'lantern_orchard':'one giant lantern-shaped window','lotus_harbor':'one huge lotus motif filling the opening',
 'mountain_railway':'one oversized locomotive-shaped opening','mushroom_forest':'one giant mushroom-shaped window',
 'river_market':'one oversized striped canopy integrated into the main roof',
 'snow_garden':'one giant snowflake-shaped window','sunflower_village':'one enormous sunflower-shaped roof',
 'waterfall_garden':'one thick waterfall-shaped spout'})
MACHINE = {'bellows_engine':'one broad folded bellows integrated into its side',
 'butterfly_press':'one huge butterfly-wing-shaped clamp','canal_lock':'one broad water gate cut into its body',
 'clockwork':'one giant gear inside its body','cloud_mill':'one huge cloud-shaped spout',
 'drawbridge':'one broad hinged leaf-shaped flap','fountain_pump':'one thick arch of water from a single spout',
 'hand_pump':'one oversized pump handle on top','leaf_elevator':'one broad leaf-shaped platform attached to its side',
 'rain_machine':'one huge raindrop-shaped window','seed_press':'one oversized seed-shaped pressing plate',
 'seed_sorter':'one huge seed-shaped slide','water_organ':'three thick organ pipes integrated into its rim',
 'waterwheel':'one huge paddle wheel attached to its side','weaving_loom':'one broad woven panel with three bold stripes',
 'wind_turbine':'one giant three-blade leaf rotor'}
TRAVEL = {'bicycle':'two solid round wheels', 'airship':'one solid balloon body and one plain basket',
 'cable_car':'one thick overhead cable', 'canoe':'one broad boat hull', 'caravan':'two solid wheels',
 'ferry':'one broad boat hull', 'glider':'two broad wings', 'hot_air_balloon':'one solid balloon body and one plain basket',
 'locomotive':'two solid wheels and one broad chimney', 'rocket':'two broad fins',
 'sailboat':'one triangular sail', 'scooter':'two solid wheels', 'sled':'two thick curved runners',
 'submarine':'one large round porthole', 'train':'two solid wheels and one broad chimney',
 'tram':'two solid wheels', 'unicycle':'one solid round wheel', 'wheelbarrow':'one solid round wheel'}

def simplify(entry):
    old = entry.get('originalArtBrief', entry.get('artBrief',''))
    if not old:
        return
    entry.setdefault('originalArtBrief', old)
    key = entry.get('conceptKey','')
    noun, _, function = key.partition(':')
    noun = noun.replace('_',' ')
    direction = entry.get('direction','')
    cue = None
    if direction == 'impossible_homes': cue = HOME.get(function)
    elif direction == 'natural_mechanisms': cue = MACHINE.get(function)
    elif direction == 'celestial_gardens': cue = COSMIC.get(function)
    elif direction == 'pocket_worlds': cue = WORLD.get(function)
    elif direction == 'surreal_still_life': cue = META.get(function)
    elif direction == 'unusual_travel': cue = TRAVEL.get(function)
    if direction == 'little_professions' and ', using ' in old:
        cue = old.split(', using ',1)[1].split('. The animal',1)[0].split(' and ',1)[0]
    if direction == 'recognizable_objects':
        brief = f'{function.replace("_", " ")}: plain iconic silhouette with large solid color patches.'
    elif cue and noun:
        cue = cue.replace('window containing a single broad leaf','leaf-shaped opening')
        cue = cue.replace('window','opening').replace('filling the opening','shape')
        cue = cue.replace('inside the main silhouette','shape').replace('inside the silhouette','shape')
        cue = cue.replace('inside its body','on its body').replace('integrated into','on')
        brief = f'{noun} with {cue}; one simple toy-like icon.'
    else:
        brief = old.split(' One big main form',1)[0].split(' One large enclosing',1)[0]
        brief = brief.replace('miniature ', '').replace('tiny ', '').replace('three oversized seed bins','one oversized seed window')
        brief = brief.replace('five oversized steps','three broad steps').replace('five huge steps','three broad steps')
        brief = entry.get('artBrief',brief).split('. ',1)[0]
        brief += '. Reduce to one coarse silhouette and one solid identifying shape.'
    entry['artBrief'] = brief
    entry['readabilityRevision'] = 3

book_path = ROOT/'production-briefs.json'
book = json.loads(book_path.read_text())
for entry in book['entries']:
    simplify(entry)
book['readabilityRevision'] = 3
book['readabilityPolicy'] = POLICY
book_path.write_text(json.dumps(book,ensure_ascii=False,indent=2)+'\n')
by_level = {e['level']:e for e in book['entries']}
for path in sorted((ROOT/'batches').glob('atlas-*.json')):
    batch = json.loads(path.read_text())
    # Repairs carry explicit color-region instructions; preserve their reviewed briefs.
    if batch.get('repairBatch'):
        continue
    for e in batch['entries']:
        q = by_level[e['level']]
        for k in ('originalArtBrief','artBrief','readabilityRevision'):
            if k in q:e[k]=q[k]
        if 'readabilityRevision' not in e:
            simplify(e)
    batch['readabilityRevision'] = 3
    reference = ROOT.parents[1]/batch['referenceFile']
    batch['sourceReadabilityRevision'] = batch.get('sourceReadabilityRevision',1) if reference.exists() else 3
    path.write_text(json.dumps(batch,ensure_ascii=False,indent=2)+'\n')
    prompt_path = path.with_suffix('.prompt.txt')
    archive = path.with_suffix('.original.prompt.txt')
    if prompt_path.exists() and not archive.exists():archive.write_text(prompt_path.read_text())
    colors = batch['paletteRoles']
    palette = 'pale cyan, green, orange, '+('red' if any(e.get('red') for e in batch['entries']) else 'pink')
    if colors>=5:palette+=', lemon yellow'
    if colors>=6:palette+=', dark green'
    style = (f'Create an atlas of {len(batch["entries"])} distinct original cozy garden puzzle illustrations. '
        f'EXACTLY {batch["columns"]} COLUMNS and {batch["rows"]} ROWS with wide straight ivory gutters. '
        f'Each panel is flat pale cyan, using ONLY {colors} flat colors: {palette}. '
        'Give every palette color at least one broad structural patch, not speckles. No black, white, gradients, text or labels. '
        +POLICY+'Subjects in strict row-major order: ')
    text = style+' '.join(f'{i+1}. {e["artBrief"]}' for i,e in enumerate(batch['entries']))
    if batch['columns']*batch['rows']>len(batch['entries']):text+=' Unused final cells stay blank cyan.'
    prompt_path.write_text(text+'\n')
print('Readability revision 3 recorded. Composition identities and frozen geometry are unchanged; existing sources still require final-grid review.')

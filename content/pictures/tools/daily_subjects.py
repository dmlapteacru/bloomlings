"""The Daily Challenge's subjects (the owner, 2026-10-07): 128 subjects the levels never show, each drawn three times
at 22 x 28 by `sketch_pictures.py --daily`. Four modules hold them: animals, places, things and food. Each subject is a
function (w, h, r) -> (canvas, roles, themes) drawn with picture_kit's helpers, as the subjects of the levels are.
"""
from daily_animals import DAILY_ANIMALS
from daily_food import DAILY_FOOD
from daily_places import DAILY_PLACES
from daily_things import DAILY_THINGS

DAILY_SUBJECTS = DAILY_ANIMALS + DAILY_PLACES + DAILY_THINGS + DAILY_FOOD

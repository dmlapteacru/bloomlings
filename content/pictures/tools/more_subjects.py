"""The second set of subjects for the levels (the owner, 2026-10-07: "more subjects for the 5000 levels, spread over
them"): 100 subjects that neither the first set nor the Daily Challenge draws, four regular pictures and one big
picture of each by `sketch_pictures.py`, after the first set. Four modules hold them: animals, nature (plants,
weather and small places), things (vehicles, toys, music, tools) and food. Each subject is a function
(w, h, r) -> (canvas, roles, themes) drawn with picture_kit's helpers, as the first set's are, and each module's
roles table names the roles its expansion pictures move to the lime and red groups (as expansions.ROLES).
"""
from more_animals import MORE_ANIMALS, MORE_ANIMALS_ROLES
from more_food import MORE_FOOD, MORE_FOOD_ROLES
from more_nature import MORE_NATURE, MORE_NATURE_ROLES
from more_things import MORE_THINGS, MORE_THINGS_ROLES

MORE_SUBJECTS = MORE_ANIMALS + MORE_NATURE + MORE_THINGS + MORE_FOOD
MORE_ROLES = {**MORE_ANIMALS_ROLES, **MORE_NATURE_ROLES, **MORE_THINGS_ROLES, **MORE_FOOD_ROLES}

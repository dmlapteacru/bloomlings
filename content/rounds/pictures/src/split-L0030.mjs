// split-L0030: one whole approved picture split by an inserted neutral strip (tools/rounds/bridge-scene.mjs make --mode split,
// 17 §8 «Одна цельная картинка», the owner, 2026-10-29): no picture cell lost, two Sprig sites on the strip, the level's
// strip drawn as the board's surface (definition.strip 'gap'); each one passed the builder's checks with the planned bridge
// kind (evidence with the found candidate level in split-L0030.json). Drafts for the owner's sheet: pictures.mjs render / sheet.
export default {
 "theme": "bridge-splits",
 "name": "split-L0030",
 "pictures": [
  {
   "title": "Маяк на скале — мост",
   "family": "Дома и архитектура",
   "subject": "полосатый маяк на скале",
   "source": "оригинальный",
   "forLevel": 30,
   "scene": {
    "from": "P0014",
    "mode": "split",
    "side": "two",
    "corner": "tr",
    "at": [
     8,
     14
    ],
    "sites": [
     [
      18,
      14
     ],
     [
      8,
      13
     ]
    ],
    "share": 0.281,
    "kind": "single",
    "cls": "Super Hard",
    "recipe": "bridge+links+hidden",
    "win": [
     8,
     13
    ],
    "lose": [
     18,
     14
    ],
    "queue": {
     "gate": 8,
     "delay": 0
    }
   },
   "rows": [
    "........#SSSSSSSSSSSSSSSSSSS",
    "SSSSSSSS#SSSSSDSSSSSSSSSSSSS",
    "SSSSSSSS#SSSSSDSSSSSSSSSSSSS",
    "SSSSSSSS#SSSSRRRSSSSSSSSSSSS",
    "SSSSSSSS#SSRRRRRRRSSSSSSSSSS",
    "SSSSSSSS#SRRRRRRRRRSSSSYYYYY",
    "YYYYYSSS#YDDDDDDDDDYYYYYYYYY",
    "YYYYYYYY#YDDYYYYYDDYYYYYYYYY",
    "YYYYYYYY#YDDYYYYYDDYYYYYYYYY",
    "YYYYYYYY#YDDYYYYYDDYYYYYYYYY",
    "YYYYYYYY#DDDDDDDDDDDDSSSYYYY",
    "YYYYSSSD#DDDDDDDDDDDDSSSSSSS",
    "SSSSSSSD#SRRRRRRRRRSSSSSSSSS",
    "SSSSSSSSbSRRRRRRRRRSSSSSSSSS",
    "SSSSSSSS##########b#########",
    "SSSSSSSSSRRRRRRRRRSSSSSSSSS.",
    "SSSSSSSSSCCCCCCCCCSSSSSSSSS.",
    "SSSSSSSSSCCCCCCCCCSSSSSSSSS.",
    "SSSSSSSSSCCCCDCCCCSSSSSSSSS.",
    "SSSSSSSSRRRRRDRRRRRSSSSSSSS.",
    "SSSSSSSSRRRRRRRRRRRSSSSSSSS.",
    "SSSSSSSSRRRRRRRRRRRSSSSSSSS.",
    "SSSSSSSSRRRRRRRRRRRSSSSSSSS.",
    "SSSSSSSSCCCCCCCCCCCSSSSSSSS.",
    "SSSSSSSSCCCCCCCCCCCSSSSSSSS.",
    "SSSSSSSSCCCCCDCCCCCSSSSSSSS.",
    "SSSSSSSSRRRRDDDRRRRSSSSSSSS.",
    "SSSSSSSRRRRRDDDRRRRRSSSSSSS.",
    "SSSSSSSRRRRRDDDRRRRRSSSSSSS.",
    "SSSSSSAAAAAAAAAAAAAAAASSSSS.",
    "SSSSSAAAAAAAAAAAAAAAAASSSSS.",
    "BBBBAAAAAAAAAAAAAAAAEEEBBBB.",
    "BBBAAAAAAAAAAAAAAAAAEEEEBCC.",
    "BBBAAAAAAAAAAAAAAAAEEEEEEBB.",
    "BBBBBBBBBBBBBBBBBBBBBBBBBBB.",
    "BBCCCBBBBCCCBBBBCCCBBBBCCCB."
   ]
  }
 ]
};

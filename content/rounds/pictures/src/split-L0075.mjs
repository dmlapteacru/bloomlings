// split-L0075: one whole approved picture split by an inserted neutral strip (tools/rounds/bridge-scene.mjs make --mode split,
// 17 §8 «Одна цельная картинка», the owner, 2026-10-29): no picture cell lost, two Sprig sites on the strip, the level's
// strip drawn as the board's surface (definition.strip 'gap'); each one passed the builder's checks with the planned bridge
// kind (evidence with the found candidate level in split-L0075.json). Drafts for the owner's sheet: pictures.mjs render / sheet.
export default {
 "theme": "bridge-splits",
 "name": "split-L0075",
 "pictures": [
  {
   "title": "Маяк на скале — мост",
   "family": "Дома и архитектура",
   "subject": "полосатый маяк на скале",
   "source": "оригинальный",
   "forLevel": 75,
   "scene": {
    "from": "P0014",
    "mode": "split",
    "side": "two",
    "corner": "tl",
    "at": [
     19,
     13
    ],
    "sites": [
     [
      9,
      13
     ],
     [
      19,
      12
     ]
    ],
    "share": 0.261,
    "kind": "single",
    "cls": "Super Hard",
    "recipe": "bridge+links+cells",
    "win": [
     19,
     12
    ],
    "lose": [
     9,
     13
    ],
    "queue": {
     "gate": 5,
     "delay": 1
    }
   },
   "rows": [
    "SSSSSSSSSSSSSSSSSSS#........",
    "SSSSSSSSSSSSSDSSSSS#SSSSSSSS",
    "SSSSSSSSSSSSSDSSSSS#SSSSSSSS",
    "SSSSSSSSSSSSRRRSSSS#SSSSSSSS",
    "SSSSSSSSSSRRRRRRRSS#SSSSSSSS",
    "YYYYYSSSSRRRRRRRRRS#SSSSSSSS",
    "YYYYYYYYYDDDDDDDDDY#SSSYYYYY",
    "YYYYYYYYYDDYYYYYDDY#YYYYYYYY",
    "YYYYYYYYYDDYYYYYDDY#YYYYYYYY",
    "YYYYYYYYYDDYYYYYDDY#YYYYYYYY",
    "YYYYSSSDDDDDDDDDDDD#YYYYYYYY",
    "SSSSSSSDDDDDDDDDDDD#DSSSYYYY",
    "SSSSSSSSSRRRRRRRRRSbDSSSSSSS",
    "#########b##########SSSSSSSS",
    ".SSSSSSSSSRRRRRRRRRSSSSSSSSS",
    ".SSSSSSSSSRRRRRRRRRSSSSSSSSS",
    ".SSSSSSSSSCCCCCCCCCSSSSSSSSS",
    ".SSSSSSSSSCCCCCCCCCSSSSSSSSS",
    ".SSSSSSSSSCCCCDCCCCSSSSSSSSS",
    ".SSSSSSSSRRRRRDRRRRRSSSSSSSS",
    ".SSSSSSSSRRRRRRRRRRRSSSSSSSS",
    ".SSSSSSSSRRRRRRRRRRRSSSSSSSS",
    ".SSSSSSSSRRRRRRRRRRRSSSSSSSS",
    ".SSSSSSSSCCCCCCCCCCCSSSSSSSS",
    ".SSSSSSSSCCCCCCCCCCCSSSSSSSS",
    ".SSSSSSSSCCCCCDCCCCCSSSSSSSS",
    ".SSSSSSSSRRRRDDDRRRRSSSSSSSS",
    ".SSSSSSSRRRRRDDDRRRRRSSSSSSS",
    ".SSSSSSSRRRRRDDDRRRRRSSSSSSS",
    ".SSSSSSAAAAAAAAAAAAAAAASSSSS",
    ".SSSSSAAAAAAAAAAAAAAAAASSSSS",
    ".BBBBAAAAAAAAAAAAAAAAEEEBBBB",
    ".BBBAAAAAAAAAAAAAAAAAEEEEBCC",
    ".BBBAAAAAAAAAAAAAAAAEEEEEEBB",
    ".BBBBBBBBBBBBBBBBBBBBBBBBBBB",
    ".BBCCCBBBBCCCBBBBCCCBBBBCCCB"
   ]
  }
 ]
};

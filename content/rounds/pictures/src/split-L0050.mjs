// split-L0050: one whole approved picture split by an inserted neutral strip (tools/rounds/bridge-scene.mjs make --mode split,
// 17 §8 «Одна цельная картинка», the owner, 2026-10-29): no picture cell lost, two Sprig sites on the strip, the level's
// strip drawn as the board's surface (definition.strip 'gap'); each one passed the builder's checks with the planned bridge
// kind (evidence with the found candidate level in split-L0050.json). Drafts for the owner's sheet: pictures.mjs render / sheet.
export default {
 "theme": "bridge-splits",
 "name": "split-L0050",
 "pictures": [
  {
   "title": "Динозаврик из яйца — мост",
   "family": "Доисторический мир",
   "subject": "динозаврик вылупляется из яйца",
   "source": "оригинальный",
   "forLevel": 50,
   "scene": {
    "from": "P0028",
    "mode": "split",
    "side": "up",
    "at": 22,
    "sites": [
     [
      10,
      22
     ],
     [
      21,
      22
     ]
    ],
    "share": 0.464,
    "kind": "better",
    "cls": "Hard",
    "recipe": "bridge+links+hidden",
    "win": [
     21,
     22
    ],
    "lose": [
     10,
     22
    ],
    "queue": {
     "gate": 0,
     "delay": 1
    },
    "rules": {
     "share": [
      0.25,
      0.5
     ]
    }
   },
   "rows": [
    "....................................",
    "....................................",
    "...........CCCCCCCC.................",
    ".........COOOCCOOOCC................",
    "........CCOOOCCCOCCC................",
    "........CCCCCCCCCGCCC...............",
    ".........CCGCCGCCGCCCGG.............",
    ".........CGGCGGGGGGGGGGG............",
    "........MGGGGGGGGGGGGGGG............",
    ".....MMMGGGGGGGGGGGGGCDGGGG.........",
    "......MMGGGGGGGGGGGGGDDGGGGGG.......",
    "........GGGGGGGGGGGGGDDGGGGGGG......",
    "........GGGGGGGGGGGGGGGGGGGGDG......",
    "........MGGGGGGGGGGGGGGGGGGGGG......",
    "......MMMGGGGGGGGGGGGGGGGGGGGG......",
    "....MMMMMMGGGGGGGGGGGGGGGGGGGG......",
    ".......MMMM.GGGGGGGGGDGGGGDGG.......",
    "..........MMGGGGGGGLLLDDDD..........",
    "............GGGGGGLLLLLL............",
    "..........MMGGGGGLLLLLLL............",
    "........MMMMGGGGGLLLLLLL............",
    ".......MMMMMGGGGGLLLLLLL.YY.........",
    "##########b##########b##############",
    ".......GGGGMGGGGGLLLLLLLGGGG........",
    "......GGGGGYGGCCGLLLCCLLGGGG........",
    "......CGGGCYGCCCCLLCCCCLGGGCCY......",
    "......CCCCCCCCCCCCCCCCCCCCCCCY......",
    ".....CCCCCCCCCCCCCCCCCCCCCCCCCY.....",
    ".....CCCCOOCCCCCCCCCCCCCOOOOCCY.....",
    ".....CCCOOOOCCCCCCCCCCCCOOOOCCY.....",
    ".....CCCOOOOCCCCCCCCCCCCOOOOCCY.....",
    ".....CCCCOOCCCCCCCCCCCCCCCCCCYY.....",
    ".....CCCCCCCCCCCCOOOCCCCCCCCCYY.....",
    ".....CCCCCCCCCCCOOOOOCCCCCCCYYY.....",
    "......CCCCCCCCCCOOOOOCCCCCCCYY......",
    "......CCCCCCCCCCOOOOOCCCCCCYYY......",
    ".......CCCCCOOCCCOOOCCCOOOYYY.......",
    "........CCCOOOOCCCCCCCCOOOYY........",
    ".........CCOOOOCCCCCCCYOOOY.........",
    "............OOCCCCYYYYYYY...........",
    "..............YYYYYYYY.............."
   ]
  }
 ]
};

// split-L0033: one whole approved picture split by an inserted neutral strip (tools/rounds/bridge-scene.mjs make --mode split,
// 17 §8 «Одна цельная картинка», the owner, 2026-10-29): no picture cell lost, two Sprig sites on the strip, the level's
// strip drawn as the board's surface (definition.strip 'gap'); each one passed the builder's checks with the planned bridge
// kind (evidence with the found candidate level in split-L0033.json). Drafts for the owner's sheet: pictures.mjs render / sheet.
export default {
 "theme": "bridge-splits",
 "name": "split-L0033",
 "pictures": [
  {
   "title": "Букет тюльпанов в вазе — мост",
   "family": "Цветы и букеты",
   "subject": "пять тюльпанов двух цветов в круглой стеклянной вазе с листьями",
   "source": "оригинальный",
   "forLevel": 33,
   "scene": {
    "from": "P0062",
    "mode": "split",
    "side": "left",
    "at": 18,
    "sites": [
     [
      18,
      6
     ],
     [
      18,
      29
     ]
    ],
    "share": 0.444,
    "kind": "better",
    "cls": "Normal",
    "recipe": "bridge+hidden",
    "win": [
     18,
     6
    ],
    "lose": [
     18,
     29
    ],
    "queue": {
     "gate": 0,
     "delay": 1
    },
    "rules": {
     "noHold": true
    }
   },
   "rows": [
    "...............PP.#RR.PP...............",
    "...............PPP#RRPPP...............",
    "...............PPR#RRRPP...............",
    "...............PRR#RRRRP...............",
    "......OO.YY.OO.PRR#RRRRP.OO.YY.OO......",
    "......OOOYYOOO.PRR#RRRRP.OOOYYOOO......",
    "......OOYYYYOO.PRRbRRRRP.OOYYYYOO......",
    "......OYYYYYYO..RR#RRRR..OYYYYYYO......",
    "......OYYYYYYO...R#RRR...OYYYYYYO......",
    "......OYYYYYYO....#MM....OYYYYYYO......",
    "......OYYYYYYO....#MM....OYYYYYYO......",
    ".......YYYYYY.....#MM.....YYYYYY.......",
    "PP.RR.PPYYYY......#MM......YYYYPP.RR.PP",
    "PPPRRPPP..MM......#MM......MM..PPPRRPPP",
    "PPRRRRPP..MMM.....#MM.....MMM..PPRRRRPP",
    "PRRRRRRP...MM.....#MM.....MM...PRRRRRRP",
    "PRRRRRRP....MM....#MM....MM....PRRRRRRP",
    "PRRRRRRP....MMM...#MM...MMM....PRRRRRRP",
    "PRRRRRRP.....MM...#MM...MM.....PRRRRRRP",
    ".RRRRRR.......MM..#MM..MM.......RRRRRR.",
    "..RRRRMM......MMM.#MM.MMM......MMRRRR..",
    ".....MMMMM.....MM.#MM.MM.....MMMMM.....",
    ".......GGGGGM...MM#MMMM...MGGGGG.......",
    "........GGGGGGM.MM#MMMM.MGGGGGG........",
    ".........GGGGGBBBB#BBBBBBGGGGG.........",
    "..........GGGGBBBB#BBBBBBGGGG..........",
    "............GGGSSS#MMSSSGGG............",
    "..............SSSS#SSSSSS..............",
    "............SSSSSS#SSSSSSSS............",
    "...........SSSSSSSbSSSSSSSSS...........",
    "..........SSSSSSSS#SSSSSSSSSS..........",
    ".........SSSSSSSSS#SSSSSSSSSSS.........",
    ".........SSSSSSSSS#SSSSSSSSSSS.........",
    ".........SSSSSSSSS#SSSSSSSSSSS.........",
    "........SSSSSSSSSS#SSSSSSSSSSSS........",
    "........BBBBBBBBBB#BBBBBBBBBBBB........",
    ".........BBBBBBBBB#BBBBBBBBBBB.........",
    ".........BBBBBBBBB#BBBBBBBBBBB.........",
    ".........BBBBBBBBB#BBBBBBBBBBB.........",
    "..........BBBBBBBB#BBBBBBBBBB..........",
    "...........BBBBBBB#BBBBBBBBB...........",
    "............BBBBBB#BBBBBBBB............",
    "..............BBBB#BBBBBB.............."
   ]
  }
 ]
};

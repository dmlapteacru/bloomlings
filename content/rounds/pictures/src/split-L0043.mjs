// split-L0043: one whole approved picture split by an inserted neutral strip (tools/rounds/bridge-scene.mjs make --mode split,
// 17 §8 «Одна цельная картинка», the owner, 2026-10-29): no picture cell lost, two Sprig sites on the strip, the level's
// strip drawn as the board's surface (definition.strip 'gap'); each one passed the builder's checks with the planned bridge
// kind (evidence with the found candidate level in split-L0043.json). Drafts for the owner's sheet: pictures.mjs render / sheet.
export default {
 "theme": "bridge-splits",
 "name": "split-L0043",
 "pictures": [
  {
   "title": "Кит — мост",
   "family": "Морские и пресноводные обитатели",
   "subject": "голубой кит с фонтаном",
   "source": "оригинальный",
   "forLevel": 43,
   "scene": {
    "from": "P0011",
    "mode": "split",
    "side": "right",
    "at": 23,
    "sites": [
     [
      23,
      3
     ],
     [
      23,
      15
     ]
    ],
    "share": 0.281,
    "kind": "better",
    "cls": "Normal",
    "recipe": "bridge+hidden",
    "win": [
     23,
     3
    ],
    "lose": [
     23,
     15
    ],
    "queue": {
     "gate": 4,
     "delay": 3
    }
   },
   "rows": [
    "SSSSSSSSSSSSSSSSSSSSSSS#SSSSSSSSS",
    "SSCCCCSSSSCCCCSSSSSSSSS#SSSSSSSSS",
    "SCCCCCCSSCCCCCCSSSSSSSS#SSSSSSSSS",
    "SCCSSCCCCCCSSCCSSSSSSSSbSSSSSSSSS",
    "SCCSSSCCCCSSSCCSSSSSSNN#NSSSSSNNN",
    "SSSSSSCCCCSSSSSSSSSSSSN#NNSSSNNNN",
    "SSSSSSSCCSSSSSSSSSSSSSS#NNNNNNNNS",
    "SSSSSSSCCSSSSSSSSSSSSSS#SNNNNNNSS",
    "SSSSSSSCCSSSSSSSSSSSSSS#SSNNNNSSS",
    "SSSSSSNNNNNNNNSSSSSSSSS#SSNNNNSSS",
    "SSSSNNNNNNNNNNNNNSSSSSS#SNNNNSSSS",
    "BBBNNNNNNNNNNNNNNNNNBBB#BNNNNBBBB",
    "BBNNNNNNNNNNNNNNNNNNNNB#NNNNNBBBB",
    "BNNNNCDNNNNNNNNNNNNNNNN#NNNNBBBBB",
    "SNNNNDDNNNNNNNNNNNNNNNN#NNNNSSSBB",
    "BNNNNNNNKKKNNNNNNNNNNNNbNNNBBBBBB",
    "BNNNNNNNNNNNNNNNNNNNNNN#NNBBBBBBB",
    "BNCCCCCCCCCCNNNNNNNNNNN#NBBBBBBBB",
    "BCCCCCCCCNNNCCCNNNNNNNN#BBBBBBBBB",
    "BBCCCCCCCCNNCCCCCNNNNNS#SBBBBBBSS",
    "BBBCCCCCCCCCCCCCCCNNNBB#BBBBBBBBB",
    "BBBBBCCCCCCCCCCCCCBBBBB#BBBBBBBBB",
    "BBBBBBBBCCCCCCCBBBBBBBB#BBBBBBBBB",
    "BBBBBBBBBBBBBBBBBBBBBBB#BBBBBBBBB",
    "BBBBBBSSSBBBBBBSSSBBBBB#BSSSBBBBB",
    "BBBBBBBBBBBBBBBBBBBBBBB#BBBBBBBBB",
    "BBBBBBBBBBBBBBBBBBBBBBB#BBBBBBBBB",
    "BBBBBBBBBBBBBBBBBBBBBBB#BBBBBBBBB"
   ]
  }
 ]
};

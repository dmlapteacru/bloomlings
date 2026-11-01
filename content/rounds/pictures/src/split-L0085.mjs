// split-L0085: one whole approved picture split by an inserted neutral strip (tools/rounds/bridge-scene.mjs make --mode split,
// 17 §8 «Одна цельная картинка», the owner, 2026-10-29): no picture cell lost, two Sprig sites on the strip, the level's
// strip drawn as the board's surface (definition.strip 'gap'); each one passed the builder's checks with the planned bridge
// kind (evidence with the found candidate level in split-L0085.json). Drafts for the owner's sheet: pictures.mjs render / sheet.
export default {
 "theme": "bridge-splits",
 "name": "split-L0085",
 "pictures": [
  {
   "title": "Палатка у костра — мост",
   "family": "Походы и отдых на природе",
   "subject": "треугольная палатка с открытым пологом рядом с маленьким костром",
   "source": "оригинальный",
   "forLevel": 85,
   "scene": {
    "from": "P0023",
    "mode": "split",
    "side": "left",
    "at": 11,
    "sites": [
     [
      11,
      2
     ],
     [
      11,
      14
     ]
    ],
    "share": 0.344,
    "kind": "better",
    "cls": "Hard",
    "recipe": "bridge+links+cells",
    "win": [
     11,
     2
    ],
    "lose": [
     11,
     14
    ],
    "queue": {
     "gate": 2,
     "delay": 1
    }
   },
   "rows": [
    "VVVVVVVVVVV#VVVVVVVVVVVVVVVVVVVVV",
    "VVVVVVVVVVV#VVVVVVCVVVVVVVVVVVVVV",
    "VVCCCCVVVVVbVVVVVCCCVVVVVVVVVVCVV",
    "VCCCCCCVVVV#VVVVVVCVVVVVVVVVVCCCV",
    "VCCCCCCVVDV#DVVVVVVVVVVVVVVVVVCVV",
    "VCCCCCCVVDB#DVVVVVVVVVVVVVVVVVVVV",
    "VVCCCCCVVVB#VVVVVVVVVVVCVVVVVVVVV",
    "VVVCCCVVVBB#BVVVVVVVVVCCCVVVVVVVV",
    "VVVVVVVVVBD#BVVVVVVVVVVCVVVVVVVVV",
    "VVVVVVVVBBD#BBVVVVVVVVVVVVVVRVVVV",
    "VVVVVVVVBCD#BBBVVVVVVVVVVVVRRVVVV",
    "VVCVVVVBBCD#BBBVVVVVVVVVVVVRRVVVV",
    "VCCCVVBBCDD#DBBBVVVVVVVVVVRRORVVV",
    "VVCVVVBBCDD#DBBBVVVVVVVVVVRRORVVV",
    "VVVVVBBCCDDbDBBBBVVVVVVVVVROORVVV",
    "MMMMMBBCCDD#DBBBBMMMMMMMMRROOORMM",
    "MMMMBBBCCDD#DDBBBBMMMMMMMRROOORMM",
    "MMMMBBCCDDD#DDBBBBMMMMMMRROYOORRM",
    "MMMBBBCCDDD#DDBBBBBMMMMMRROYYOORM",
    "MMBBBCCCDDD#DDBBBBBBMMMMROOYYOORM",
    "MMBBBCCCDDD#DDBBBBBBMMMMCOOYYOORM",
    "MBBBCCCDDDD#DDDBBBBBBMMCCCCCYCCCC",
    "MBBBCCCDDDD#DDDBBBBBBMMMCCCCCCCCM",
    "BBBCCCCDDDD#DDDBBBBBBBMCCCCCCCCCC",
    "MMMMMMMMMMM#MMMMMMMMMMMCCCCMMCCCC",
    "MMMMMMMMMMM#MMMMMMMMMMMMMMMMMMMMM",
    "MMMMMMMMMMM#MMMMMMMMMMMMMMMMMMMMM",
    "MMMMMMMMMMM#MMMMMMMMMMMMMMMMMMMMM"
   ]
  }
 ]
};

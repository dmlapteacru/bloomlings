// The round engine of product/17_LEVEL_GENERATION_RULES.md: tools/rounds/engine/ (rules.cjs, timeline.cjs,
// idle-rules.cjs, palette.cjs, clock.cjs). It grew from the pilot's rules and timeline (v8 base, the movement profile of
// §12) and is now our own code, changed only for 17:
// - slot positions of §1: a single pod takes the first free place, a release leaves a hole, only an accepted group
//   compacts to the left;
// - a group is available when no other pod stands before any of its members (§8: members may stand over each other);
// - boards up to 48×48 with the frame (pictures up to 1400 cells, §2);
// - 4 or 5 places (§1; the repair of the fifth place, §8, is not played yet).
// Mechanics it plays: hidden pods (reveal on reaching the top), linked groups, Sprig's bridge. Others are listed in
// lib.mjs SUPPORTED and refused by the level builder until the engine plays them. The playable pilot
// (tools/gameplay-pilot-v10) embeds these same files.
import {createRequire} from 'node:module';
const req=createRequire(import.meta.url);
let cached=null;
export async function engine(){
 if(!cached)cached={R:req('./engine/rules.cjs'),T:req('./engine/timeline.cjs'),idle:req('./engine/idle-rules.cjs')};
 return cached;}

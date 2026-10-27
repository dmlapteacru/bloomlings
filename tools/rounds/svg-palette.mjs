// The pilot palette for SVG drafts of round pictures (tools/rounds/svg2cells.mjs): C.G is Лист's hex, C.P Лепесток's …
// An SVG draft uses only these fills (flat areas, no gradients); anything else maps to the nearest of them.
import {PALETTE,CODES} from './lib.mjs';
export const C=Object.fromEntries([...CODES].map(c=>[c,PALETTE[c].color]));
export const NAMES=Object.fromEntries([...CODES].map(c=>[c,PALETTE[c].name]));

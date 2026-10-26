// Batch 11, environment versions (the owner, 2026-10-26: some pictures carry their natural environment so the board is
// fuller): ten pictures of batch 11 redrawn with sea, sky, space, rink or meadow round them, in five parts
// (batch-0011-env/env-a … env-e, two each).
const parts=await Promise.all(['a','b','c','d','e'].map(k=>import(`./batch-0011-env/env-${k}.mjs`)));
export default {theme:'batch-0011-env',pictures:parts.flatMap(p=>p.default.pictures)};

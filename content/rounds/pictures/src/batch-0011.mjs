// Batch 11 (P0011–P0060): 50 pictures for positions 11–60 from the theme catalog
// (product/design/Bloomlings-picture-theme-catalog.md), drawn by the skill bloomlings-board-picture in ten parts
// (batch-0011/part-01 … part-10, five pictures each, in level order). Each part's entries carry the catalog tags.
const parts=await Promise.all(Array.from({length:10},(_,i)=>import(`./batch-0011/part-${String(i+1).padStart(2,'0')}.mjs`)));
export default {theme:'batch-0011',pictures:parts.flatMap(p=>p.default.pictures)};

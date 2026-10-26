// Batch 61 (P0061–P0110): 50 pictures, positions 61–100 plus ten cartoon-universe references (101–110), from the theme catalog
// (product/design/Bloomlings-picture-theme-catalog.md), drawn by the skill bloomlings-board-picture in ten parts
// (batch-0061/part-01 … part-10, five pictures each, in number order). Each part's entries carry the catalog tags;
// parts 09–10 are third-party universes (source 'сторонняя вселенная', with reference): not for release until rights are confirmed.
const parts=await Promise.all(Array.from({length:10},(_,i)=>import(`./batch-0061/part-${String(i+1).padStart(2,'0')}.mjs`)));
export default {theme:'batch-0061',pictures:parts.flatMap(p=>p.default.pictures)};

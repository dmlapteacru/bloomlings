// put new rows into the trial copy's picture (approved there only), with its measured N, colours and volumes
process.env.ROUNDS_DIR=process.env.ROUNDS_DIR||'/tmp/claude-0/-home-user-bloomlings/17da13b7-11ad-5226-8b0f-bc66792e8f73/scratchpad/rounds-trial3';
const fs=await import('node:fs');const L=await import('/home/user/bloomlings/tools/rounds/lib.mjs');
const [id,rowsFile]=process.argv.slice(2),f=`${process.env.ROUNDS_DIR}/pictures/${id}.json`;
const p=JSON.parse(fs.readFileSync(f)),rows=JSON.parse(fs.readFileSync(rowsFile)).rows;
Object.assign(p,{rows,w:rows[0].length,h:rows.length});const m=L.measure(p);Object.assign(p,{N:m.N,colours:m.colours,volumes:m.volumes});
fs.writeFileSync(f,JSON.stringify(p));const mf=`${process.env.ROUNDS_DIR}/pictures/manifest.json`,man=JSON.parse(fs.readFileSync(mf));
const e=man.pictures.find(x=>x.id===id);Object.assign(e,{w:p.w,h:p.h,N:p.N,colours:p.colours});fs.writeFileSync(mf,JSON.stringify(man,null,1));console.log(id,p.w,p.h,p.N,p.colours);

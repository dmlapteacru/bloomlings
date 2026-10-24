// Compatibility entry point: never restore the retired six-level prototype.
import {spawnSync} from 'node:child_process';
const [command,...args]=process.argv.slice(2);
if(!['level','search','audit','assemble'].includes(command))throw Error('Usage: generate.mjs level ID [from] [bans] | audit | assemble');
const result=spawnSync(process.execPath,[new URL('./synthesis.mjs',import.meta.url).pathname,command==='level'?'search':command,...args],{stdio:'inherit'});
if(result.error)throw result.error;
process.exit(result.status??1);

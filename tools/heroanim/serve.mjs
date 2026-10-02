// A small static server for the render page: three.js from node_modules, the FBX loader with the embedded-texture fix,
// the models and the page. Only bake.mjs uses it.
import http from 'node:http';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));
const three = path.join(here, 'node_modules', 'three');

const types = { '.js': 'text/javascript', '.html': 'text/html', '.fbx': 'application/octet-stream', '.png': 'image/png' };

// three 0.160's FBXLoader picks an embedded image's type from its file name. Meshy's merged FBX files name their
// textures after a ".fbm" folder, so the loader drops them; this patch takes the type from the bytes instead.
function patchedLoader() {
  let s = fs.readFileSync(path.join(three, 'examples', 'jsm', 'loaders', 'FBXLoader.js'), 'utf8');
  s = s.replace("from '../curves/NURBSCurve.js'", "from '/three/examples/jsm/curves/NURBSCurve.js'")
    .replace("from '../libs/fflate.module.js'", "from '/three/examples/jsm/libs/fflate.module.js'");
  const anchor = "\t\t\tdefault:\n\n\t\t\t\tconsole.warn( 'FBXLoader: Image type \"' + extension + '\" is not supported.' );\n\t\t\t\treturn;";
  if (!s.includes(anchor)) throw new Error('FBXLoader changed: the embedded-image patch no longer applies');
  return s.replace(anchor, [
    '\t\t\tdefault: {',
    "\t\t\t\tconst b = typeof content === 'string' ? null : new Uint8Array( content );",
    "\t\t\t\tif ( b && b[ 0 ] === 0x89 && b[ 1 ] === 0x50 ) { type = 'image/png'; break; }",
    "\t\t\t\tif ( b && b[ 0 ] === 0xFF && b[ 1 ] === 0xD8 ) { type = 'image/jpeg'; break; }",
    "\t\t\t\tconsole.warn( 'FBXLoader: Image type \"' + extension + '\" is not supported.' );",
    '\t\t\t\treturn;',
    '\t\t\t}',
  ].join('\n'));
}

export function serve() {
  const loader = patchedLoader();
  const server = http.createServer((req, res) => {
    const url = decodeURIComponent(new URL(req.url, 'http://x').pathname);
    let file = null;
    if (url === '/fbxloader.js') {
      res.writeHead(200, { 'content-type': types['.js'] });
      res.end(loader);
      return;
    }
    if (url === '/page.html') file = path.join(here, 'page.html');
    else if (url.startsWith('/three/')) file = path.join(three, url.slice('/three/'.length));
    else if (url.startsWith('/models/')) file = path.join(here, 'models', path.basename(url));
    if (!file || !file.startsWith(here) || !fs.existsSync(file)) {
      res.writeHead(404);
      res.end();
      return;
    }
    res.writeHead(200, { 'content-type': types[path.extname(file)] || 'application/octet-stream' });
    fs.createReadStream(file).pipe(res);
  });
  return new Promise(resolve => server.listen(0, '127.0.0.1', () => resolve({ server, port: server.address().port })));
}

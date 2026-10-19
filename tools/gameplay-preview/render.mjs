// Optional screenshots and a phone-sized reading PDF from the same standalone HTML.
// Requires the repository's existing tools/heroanim/node_modules/playwright-core.
import { chromium } from '../heroanim/node_modules/playwright-core/index.mjs';
import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '../..');
const output = resolve(root, 'product/previews/gameplay-evolution');
const html = await readFile(resolve(output, 'index.html'));
const server = createServer((request, response) => {
  if (request.url !== '/') { response.writeHead(404); response.end(); return; }
  response.writeHead(200, { 'Content-Type': 'text/html; charset=utf-8' });
  response.end(html);
});
await new Promise(done => server.listen(0, '127.0.0.1', done));
let browser;
try {
  browser = await chromium.launch({
    executablePath: process.env.BLOOMLINGS_CHROMIUM || '/usr/bin/chromium',
    headless: true,
    args: ['--no-sandbox', '--disable-dev-shm-usage']
  });
  const page = await browser.newPage({
    viewport: { width: 390, height: 844 }, deviceScaleFactor: 2,
    isMobile: true, hasTouch: true, reducedMotion: 'reduce'
  });
  const errors = [];
  page.on('pageerror', error => errors.push(error.message));
  await page.goto(`http://127.0.0.1:${server.address().port}/`);
  await page.evaluate(() => document.fonts.ready);
  await page.locator('#stagePicker [data-stage="4"]').click();
  await page.locator('.art-gallery').screenshot({ path: resolve(output, 'examples.png') });
  await page.locator('.phone-card').screenshot({ path: resolve(output, 'level20.png') });
  await page.locator('#fullPlan > summary').click();
  const check = await page.evaluate(() => ({
    overflow: document.documentElement.scrollWidth > innerWidth,
    missingImages: [...document.images].filter(image => !image.complete || image.naturalWidth === 0).length
  }));
  if (check.overflow || check.missingImages || errors.length) {
    throw new Error(JSON.stringify({ ...check, errors }));
  }
  await page.locator('#fullPlan > summary').click();
  await page.pdf({
    path: resolve(output, 'reading.pdf'), width: '410px', height: '890px',
    margin: { top: '16px', bottom: '20px', left: '14px', right: '14px' },
    printBackground: true
  });
  console.log('Rendered examples.png, level20.png, reading.pdf from the standalone HTML.');
} finally {
  if (browser) await browser.close();
  await new Promise(done => server.close(done));
}

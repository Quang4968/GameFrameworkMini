// npm run build -- <playable> [--network applovin,google]
// Builds src/main.ts once per network (the adapter is chosen at compile time), inlines the bundle into one
// HTML per network and writes dist/<network>/. Google also gets a zip. Ends with dist/size-report.md.
import fs from 'node:fs';
import path from 'node:path';
import { build } from 'vite';
import { strToU8, zipSync } from 'fflate';
import { kb, parseArgs, playableDir, readNetworks, runtimeConfig, selectedNetworks, viteConfig } from './lib/common.mjs';

const args = parseArgs();
const name = args._[0];
const dir = playableDir(name);
const spec = readNetworks();
const config = runtimeConfig(dir);
const dist = path.join(dir, 'dist');
fs.mkdirSync(dist, { recursive: true });

const GOOGLE_EXIT_API = 'https://tpc.googlesyndication.com/pagead/gadgets/html5/api/exitapi.js';

function html(network, js) {
  const net = spec.networks[network];
  const head = [];
  if (network === 'google') {
    const orientation = config.orientation === 'both' ? 'portrait,landscape' : config.orientation;
    head.push(`<meta name="ad.orientation" content="${orientation}">`);
    head.push(`<script type="text/javascript" src="${GOOGLE_EXIT_API}"></script>`);
  }
  if (net.includeMraidJs) head.push('<script src="mraid.js"></script>');
  // A literal "</script" inside the bundle would close the inline tag early.
  const safeJs = js.replace(/<\/(script)/gi, '<\\/$1');
  return `<!DOCTYPE html><html lang="en"><head><meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1,maximum-scale=1,user-scalable=no,viewport-fit=cover">
<title>${config.name}</title>${head.join('')}
<style>html,body{margin:0;padding:0;width:100%;height:100%;overflow:hidden;background:${config.background}}</style>
</head><body><script>${safeJs}</script></body></html>`;
}

const sizes = {};
for (const network of selectedNetworks(args)) {
  const result = await build(viteConfig(dir, network));
  const output = (Array.isArray(result) ? result[0] : result).output;
  const chunk = output.find((o) => o.type === 'chunk' && o.isEntry);
  const extra = output.filter((o) => o !== chunk);
  if (extra.length) console.warn(`WARN ${network}: ${extra.map((o) => o.fileName).join(', ')} not inlined (CSS import?) -> dropped`);

  const page = html(network, chunk.code);
  const outDir = path.join(dist, network);
  fs.rmSync(outDir, { recursive: true, force: true });
  fs.mkdirSync(outDir, { recursive: true });
  fs.writeFileSync(path.join(outDir, 'index.html'), page);

  let deliverable = path.join(outDir, 'index.html');
  if (spec.networks[network].format === 'zip') {
    deliverable = path.join(outDir, `${name}-${network}.zip`);
    fs.writeFileSync(deliverable, zipSync({ 'index.html': strToU8(page) }, { level: 9 }));
  }
  sizes[network] = { file: path.relative(dir, deliverable), bytes: fs.statSync(deliverable).size, js: chunk.code.length };
  console.log(`built ${network}: ${kb(sizes[network].bytes)} -> ${sizes[network].file}`);
}

const lastPath = path.join(dir, '.last-sizes.json');
const last = fs.existsSync(lastPath) ? JSON.parse(fs.readFileSync(lastPath, 'utf8')) : {};
const rows = Object.entries(sizes).map(([network, s]) => {
  const max = spec.networks[network].maxBytes;
  const delta = last[network] ? `${s.bytes - last[network].bytes >= 0 ? '+' : ''}${kb(s.bytes - last[network].bytes)}` : '-';
  const verdict = s.bytes > max ? 'OVER LIMIT' : s.bytes > spec.targetBytes ? 'over target' : 'ok';
  return `| ${network} | ${s.file} | ${kb(s.bytes)} | ${delta} | ${kb(max)} | ${verdict} |`;
});
fs.writeFileSync(
  path.join(dist, 'size-report.md'),
  `# ${name} sizes\n\nTarget ${kb(spec.targetBytes)}; spec table verified ${spec.verifiedAt}.\n\n` +
    `| Network | File | Size | vs last build | Limit | Verdict |\n| --- | --- | --- | --- | --- | --- |\n${rows.join('\n')}\n`,
);
fs.writeFileSync(lastPath, JSON.stringify({ ...last, ...sizes }, null, 2));
console.log(`report -> ${path.relative(dir, path.join(dist, 'size-report.md'))}; next: npm run audit -- ${name}`);

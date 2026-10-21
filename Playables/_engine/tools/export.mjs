// npm run export -- <playable> [--to <folder>]
// Hand-over from the last build: dist/export/ holds only the files the networks take,
// <playable>_<network>.html (or .zip for zip networks), also zipped as dist/<playable>_all-networks.zip; the same
// files go to the team's delivery folder <root>/<game>/<pa>/ (root and game from art-map.yaml `export`,
// PLAYABLE_EXPORT_ROOT overrides root on another machine, pa from scenario.yaml `meta.pa`; --to wins).
// Reports never travel with them: the audit verdict prints here and is kept as dist/AUDIT.txt next to
// dist/size-report.md. Run `npm run build` first.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { spawnSync } from 'node:child_process';
import { zipSync } from 'fflate';
import { kb, parseArgs, playableDir, readNetworks, readScenario, selectedNetworks } from './lib/common.mjs';
import { readArtMap } from './lib/art.mjs';

const args = parseArgs();
const name = args._[0];
const dir = playableDir(name);
const spec = readNetworks();
const dist = path.join(dir, 'dist');
const out = path.join(dist, 'export');
fs.rmSync(out, { recursive: true, force: true });
fs.mkdirSync(out, { recursive: true });

const rows = [];
for (const network of selectedNetworks(args)) {
  const folder = path.join(dist, network);
  const zip = fs.existsSync(folder) && fs.readdirSync(folder).find((f) => f.endsWith('.zip'));
  const source = spec.networks[network].format === 'zip' ? zip && path.join(folder, zip) : path.join(folder, 'index.html');
  if (!source || !fs.existsSync(source)) {
    console.log(`FIX  ${network}: no build -> npm run build -- ${name}`);
    continue;
  }
  const file = `${name}_${network}${path.extname(source)}`;
  fs.copyFileSync(source, path.join(out, file));
  rows.push(`${network.padEnd(10)} ${file.padEnd(44)} ${kb(fs.statSync(source).size).padStart(8)}   test: ${spec.networks[network].test}`);
}

const audit = spawnSync(process.execPath, [path.join(path.dirname(fileURLToPath(import.meta.url)), 'audit.mjs'), name], { encoding: 'utf8' });
if (!audit.stdout) throw new Error(`audit did not run: ${audit.stderr || audit.error}`);
const verdict = audit.status === 0 ? 'READY TO UPLOAD' : 'NOT READY TO UPLOAD (fix the FIX lines below first)';
fs.writeFileSync(path.join(dist, 'AUDIT.txt'), `${name}: ${verdict}\n\n${rows.join('\n')}\n\n${audit.stdout}`);

const files = Object.fromEntries(fs.readdirSync(out).map((f) => [f, new Uint8Array(fs.readFileSync(path.join(out, f)))]));
const all = path.join(dist, `${name}_all-networks.zip`);
fs.writeFileSync(all, zipSync(files, { level: 9 }));
const fixes = audit.stdout.split('\n').filter((l) => l.startsWith('FIX'));
console.log(`${rows.join('\n')}\n\n${verdict}${fixes.length ? `\n${fixes.join('\n')}` : ''}\nexport -> ${path.relative(dir, out)}/ and ${path.relative(dir, all)} (${kb(fs.statSync(all).size)}); reports in dist/AUDIT.txt, dist/size-report.md`);

const target = args.to ? String(args.to) : deliveryFolder();
if (target) {
  fs.mkdirSync(target, { recursive: true });
  for (const f of fs.readdirSync(out)) fs.copyFileSync(path.join(out, f), path.join(target, f));
  console.log(`delivered -> ${target}`);
}

function deliveryFolder() {
  const cfg = readArtMap().export ?? {};
  const root = process.env.PLAYABLE_EXPORT_ROOT || cfg.root;
  const pa = readScenario(dir).meta?.pa;
  if (!root || !cfg.game) return console.log('WARN no art-map `export: { root, game }`: package left in dist/ only'), null;
  if (!pa) return console.log(`WARN scenario.yaml meta.pa is empty: set the ad id (e.g. PA5) to deliver to ${path.join(root, cfg.game)}/<pa>/`), null;
  return path.join(root, cfg.game, String(pa));
}

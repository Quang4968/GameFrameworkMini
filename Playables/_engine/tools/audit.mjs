// npm run audit -- <playable> [--network ...]
// Read-only gate over dist/: one OK / WARN / FIX line per check, exit 1 on any FIX. Checks what the networks
// reject on upload: size, external requests, missing host hooks, Google's zip rules, placeholder store links;
// and the house rule that every bundled asset is the game's own (assets/manifest.json source in Assets/).
import fs from 'node:fs';
import { spawnSync } from 'node:child_process';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { unzipSync } from 'fflate';
import { REPO, kb, parseArgs, playableDir, readNetworks, readScenario, runtimeConfig, selectedNetworks } from './lib/common.mjs';
import { readArtMap, readManifest } from './lib/art.mjs';

const args = parseArgs();
const dir = playableDir(args._[0]);
const spec = readNetworks();
const config = runtimeConfig(dir);
let fixes = 0;
const ok = (m) => console.log(`OK   ${m}`);
const warn = (m) => console.log(`WARN ${m}`);
const fix = (m) => {
  fixes++;
  console.log(`FIX  ${m}`);
};

// Literal text the networks' upload validators search for (Unity Ads: "Your playable is missing mraid.open()").
const HOOKS = {
  mraid: /\bmraid\b/,
  'mraid.open': /\bmraid\.open\(/,
  'mraid.getState': /\bmraid\.getState\(/,
  viewableChange: /["']viewableChange["']/,
  gameReady: /\bgameReady\b/,
  gameEnd: /\bgameEnd\b/,
  install: /\binstall\(/,
  'ExitApi.exit': /ExitApi/,
  'exitapi.js': /exitapi\.js/,
  'ad.orientation': /name="ad\.orientation"/,
};
// Strings that look like URLs but never load anything (XML namespaces, docs links in error messages).
const HARMLESS = /^https?:\/\/(www\.w3\.org|github\.com|pixijs\.|developer\.mozilla\.org)/;

const age = (Date.now() - Date.parse(spec.verifiedAt)) / 86400000;
if (age > 90) warn(`networks.json verified ${spec.verifiedAt} (${age.toFixed(0)} days ago) -> re-check each 'source' link`);

const manifest = readManifest(dir);
const optDir = path.join(dir, 'assets', 'opt');
if (fs.existsSync(optDir)) {
  const files = fs.readdirSync(optDir);
  const orphans = files.filter((f) => !String(manifest[f.replace(/\.[^.]+$/, '')]?.source ?? '').startsWith('Assets/'));
  if (orphans.length) fix(`not from the Unity project: ${orphans.join(', ')} -> add the art to Assets/ and art-map.yaml, then npm run pull`);
  else ok(`all ${files.length} bundled assets come from the Unity project`);
  const gone = Object.entries(manifest).filter(([, e]) => !fs.existsSync(path.join(REPO, String(e.source).split('#')[0])));
  if (gone.length) warn(`sources moved or deleted in the project: ${gone.map(([k]) => k).join(', ')} -> update art-map.yaml, npm run pull`);
}
// Art the project itself marks as not releasable (a README_PROTOTYPE_ONLY / DO_NOT_SHIP file in its folder or above).
const NOT_RELEASABLE = /^README.*(PROTOTYPE|DO_?NOT_?SHIP)/i;
const blocked = (source) => {
  for (let d = path.dirname(path.join(REPO, String(source).split('#')[0])); d.startsWith(path.join(REPO, 'Assets')); d = path.dirname(d))
    if (fs.readdirSync(d).some((f) => NOT_RELEASABLE.test(f))) return path.relative(REPO, d);
  return null;
};
const flagged = Object.entries(manifest).flatMap(([k, e]) => [e.source, ...(e.meshes ?? []), ...(e.materials ?? [])].filter(Boolean).map((src) => [k, blocked(src)])).filter(([, d]) => d);
if (flagged.length)
  fix(`not releasable (the project marks ${[...new Set(flagged.map(([, d]) => d))].join(', ')} prototype-only): ${[...new Set(flagged.map(([k]) => k))].join(', ')} -> fine for internal review, replace the art before uploading`);

// Full gameplay: every asset the game's gameplay scene uses is a role or an explained exclusion in the art map.
const scene = readArtMap().scene;
if (scene) {
  const deps = spawnSync(process.execPath, [path.join(path.dirname(fileURLToPath(import.meta.url)), 'deps.mjs'), scene], { encoding: 'utf8' });
  const gaps = (deps.stdout.match(/^ {2}MISSING .*$/gm) ?? []).map((l) => l.replace(/^ {2}MISSING /, ''));
  if (!/files followed/.test(deps.stdout)) fix(`could not check ${scene} coverage: ${(deps.stderr || deps.error || 'deps.mjs gave no result').toString().trim().split('\n')[0]}`);
  else if (gaps.length) fix(`art map misses ${gaps.length} asset(s) of ${scene}: ${gaps.slice(0, 4).join(', ')}${gaps.length > 4 ? ', ...' : ''} -> npm run deps -- ${scene}`);
  else ok(`art map covers every asset of ${scene}`);
} else warn('art-map.yaml has no `scene`: cannot check the playable covers the full gameplay');

const literal = Object.entries(readScenario(dir).texts ?? {}).filter(([, v]) => typeof v === 'string' && !v.startsWith('@'));
if (literal.length) warn(`texts not from the game's localization: ${literal.map(([k]) => k).join(', ')} (use '@key' when the game has the string)`);

for (const network of selectedNetworks(args)) {
  const net = spec.networks[network];
  const page = path.join(dir, 'dist', network, 'index.html');
  if (!fs.existsSync(page)) {
    fix(`${network}: no build -> npm run build -- ${args._[0]}`);
    continue;
  }
  const htmlText = fs.readFileSync(page, 'utf8');
  let bytes = fs.statSync(page).size;

  if (net.format === 'zip') {
    const zip = fs.readdirSync(path.dirname(page)).find((f) => f.endsWith('.zip'));
    if (!zip) fix(`${network}: zip missing -> rebuild`);
    else {
      const zipPath = path.join(path.dirname(page), zip);
      bytes = fs.statSync(zipPath).size;
      const count = Object.keys(unzipSync(fs.readFileSync(zipPath))).length;
      if (net.maxFiles && count > net.maxFiles) fix(`${network}: ${count} files in zip > ${net.maxFiles}`);
    }
  }

  if (bytes > net.maxBytes) fix(`${network}: ${kb(bytes)} > limit ${kb(net.maxBytes)} -> shrink assets (npm run assets) or drop audio`);
  else if (bytes > spec.targetBytes) warn(`${network}: ${kb(bytes)} over target ${kb(spec.targetBytes)} (limit ${kb(net.maxBytes)})`);
  else ok(`${network}: ${kb(bytes)}`);

  const allowed = new Set(net.allowedUrls ?? []);
  const loads = [...htmlText.matchAll(/(?:src|href)\s*=\s*["']?((?:https?:)?\/\/[^"'\s>]+)/gi)].map((m) => m[1]).filter((u) => !allowed.has(u));
  const cssUrls = [...htmlText.matchAll(/url\(\s*["']?((?:https?:)?\/\/[^)"']+)/gi)].map((m) => m[1]);
  if (loads.length + cssUrls.length) fix(`${network}: external resource ${[...loads, ...cssUrls].join(', ')} -> inline it`);
  else ok(`${network}: no external resources`);

  const store = new Set([config.store.ios, config.store.android]);
  const strings = [...new Set([...htmlText.matchAll(/["'`](https?:\/\/[^"'`\s]+)/g)].map((m) => m[1]))].filter(
    (u) => !HARMLESS.test(u) && !allowed.has(u) && !store.has(u),
  );
  if (strings.length) warn(`${network}: URL strings in bundle (check none is fetched): ${strings.slice(0, 5).join(', ')}`);

  const missing = (net.requires ?? []).filter((h) => !HOOKS[h]?.test(htmlText));
  if (missing.length) fix(`${network}: missing ${missing.join(', ')} -> check _engine/sdk/networks/${network === 'unity' || network === 'applovin' ? 'mraid' : network}.ts`);
  else ok(`${network}: hooks ${(net.requires ?? []).join(', ')}`);

  if (network === 'google' && /localStorage/.test(htmlText)) fix('google: localStorage is not allowed in Google HTML5 ads');

  const missingStores = config.platforms.filter((p) => !/^https:\/\//.test(config.store[p] ?? ''));
  if (net.cta.includes('storeUrl') && missingStores.length)
    fix(`${network}: ${missingStores.map((p) => `meta.store.${p}`).join(' / ')} missing in scenario.yaml (meta.platforms: ${config.platforms.join(', ')}; mraid.open needs them)`);
}

console.log(fixes ? `\n${fixes} FIX -> not ready to upload` : `\nready: test on ${selectedNetworks(args).map((n) => spec.networks[n].test).join(' / ')}`);
process.exit(fixes ? 1 : 0);

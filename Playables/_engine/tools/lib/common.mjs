// Shared by every tool: paths, argument parsing, scenario.yaml, networks.json and the one Vite config used
// for both `dev` and `build`. Run tools from Playables/ through npm (`npm run build -- <playable>`).
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { parse } from 'yaml';
import { readArtMap, readManifest, readStrings, resolveText } from './art.mjs';

export const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../../..');
export const ENGINE = path.join(ROOT, '_engine');
export const REPO = path.resolve(ROOT, '..');

/** `node x.mjs name --network unity,google --port 5180` -> { _: ['name'], network: 'unity,google', port: '5180' } */
export function parseArgs(argv = process.argv.slice(2)) {
  const out = { _: [] };
  for (let i = 0; i < argv.length; i++) {
    const a = argv[i];
    if (!a.startsWith('--')) out._.push(a);
    else if (argv[i + 1] && !argv[i + 1].startsWith('--')) out[a.slice(2)] = argv[++i];
    else out[a.slice(2)] = true;
  }
  return out;
}

export function fail(message) {
  console.error(`FIX ${message}`);
  process.exit(1);
}

export function playableDir(name) {
  if (!name) fail('playable name missing -> npm run <tool> -- <playable>');
  const dir = path.join(ROOT, name);
  if (!fs.existsSync(path.join(dir, 'scenario', 'scenario.yaml'))) fail(`${name}/scenario/scenario.yaml not found -> npm run new -- ${name}`);
  return dir;
}

export function readScenario(dir) {
  return parse(fs.readFileSync(path.join(dir, 'scenario', 'scenario.yaml'), 'utf8')) ?? {};
}

export function readNetworks() {
  return JSON.parse(fs.readFileSync(path.join(ROOT, 'networks.json'), 'utf8'));
}

export function selectedNetworks(args) {
  const all = Object.keys(readNetworks().networks);
  if (!args.network) return all;
  const picked = String(args.network).split(',');
  for (const n of picked) if (!all.includes(n)) fail(`unknown network '${n}' -> one of ${all.join(', ')}`);
  return picked;
}

/**
 * The runtime half of scenario.yaml (see _engine/config.ts) plus what the game supplies: texts resolved from its
 * localization ('@key'), colours / fonts / role params from art-map.yaml, pivots and slices from the manifest.
 */
export function runtimeConfig(dir) {
  const scenario = readScenario(dir);
  const art = readArtMap();
  const strings = readStrings(art);
  const meta = scenario.meta ?? {};
  const texts = Object.fromEntries(Object.entries(scenario.texts ?? {}).map(([k, v]) => [k, resolveText(v, strings)]));
  const roles = Object.fromEntries(
    Object.entries(art.roles ?? {}).map(([k, { file, maxSize, format, quality, bitrate, ...params }]) => [k, params]),
  );
  const sprites = Object.fromEntries(
    Object.entries(readManifest(dir))
      .filter(([, e]) => e.kind === 'image')
      .map(([k, e]) => [k, { anchor: e.anchor, slice: e.slice, size: e.size, density: e.density ?? 1 }]),
  );
  return {
    name: meta.name ?? 'playable',
    orientation: meta.orientation ?? 'portrait',
    design: meta.design ?? { width: 1080, height: 1920 },
    background: meta.background ?? art.colors?.background ?? '#1d2433',
    store: { ios: meta.store?.ios ?? '', android: meta.store?.android ?? '' },
    platforms: meta.platforms ?? ['android', 'ios'],
    tuning: { maxPlaySec: 30, idleHintSec: 4, ...(scenario.tuning ?? {}) },
    texts,
    art: {
      colors: art.colors ?? {},
      layout: art.layout ?? {},
      fonts: Object.fromEntries(Object.entries(art.fonts ?? {}).map(([k, f]) => [k, f.family ?? k])),
      roles,
    },
    level: scenario.level,
    sprites,
  };
}

function configPlugin(dir) {
  const id = 'virtual:playable-config';
  return {
    name: 'playable-config',
    resolveId: (source) => (source === id ? `\0${id}` : null),
    load: (resolved) => (resolved === `\0${id}` ? `export default ${JSON.stringify(runtimeConfig(dir))};` : null),
  };
}

export function viteConfig(dir, network) {
  return {
    root: dir,
    configFile: false,
    logLevel: 'warn',
    clearScreen: false,
    // @game: this game's shared playable kit (Playables/_game), next to the per-game art map.
    resolve: { alias: { '@engine': ENGINE, '@game': path.join(ROOT, '_game') } },
    define: { __NETWORK__: JSON.stringify(network) },
    plugins: [configPlugin(dir)],
    esbuild: { legalComments: 'none' },
    // 3D models are assets like images: inlined as data URLs, read by _engine/three.
    assetsInclude: ['**/*.fbx', '**/*.glb', '**/*.mesh'],
    server: { host: true, fs: { allow: [ROOT] } },
    build: {
      write: false,
      target: 'es2018',
      // Everything inlines: a playable may not load a second file.
      assetsInlineLimit: 1e9,
      cssCodeSplit: false,
      modulePreload: false,
      reportCompressedSize: false,
      rollupOptions: {
        input: path.join(dir, 'src', 'main.ts'),
        output: { format: 'iife', inlineDynamicImports: true, entryFileNames: 'game.js' },
      },
    },
  };
}

export function kb(bytes) {
  return `${(bytes / 1024).toFixed(0)} KB`;
}

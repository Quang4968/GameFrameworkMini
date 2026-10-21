// npm run deps -- <Assets/.../Scene.unity | prefab | asset> [more roots] [--all]
// Everything a gameplay scene shows comes from somewhere it references: this follows every guid from the roots
// through prefabs, ScriptableObjects, materials and animator files, and lists the leaf assets (textures, meshes,
// models, audio, fonts, shaders) with whether Playables/art-map.yaml covers them. A playable reproduces the
// full gameplay, so every visual or audio leaf of the flow it shows belongs in the art map; MISSING lines are the
// to-do list: add each to a role, or to art-map `excluded` with the reason it is not part of the ad's flow.
// Assets a script loads by path or address are not seen here: check the views that draw the flow.
// --all also lists leaves under Packages and TextMesh Pro.
import fs from 'node:fs';
import path from 'node:path';
import { REPO, fail, parseArgs } from './lib/common.mjs';
import { readArtMap } from './lib/art.mjs';
import { guidToPath } from './lib/unity.mjs';

const args = parseArgs();
if (!args._.length) fail('usage: npm run deps -- Assets/_Dev/Scenes/MainGame.unity [more roots]');

const YAML = /\.(unity|prefab|asset|mat|controller|overrideController|anim|playable|spriteatlas|mask)$/;
const KINDS = [
  ['texture', /\.(png|jpe?g|psd|tga|tiff?|exr|webp)$/i],
  ['model', /\.(fbx|obj|blend|glb|gltf)$/i],
  ['audio', /\.(mp3|ogg|wav|aif+|m4a)$/i],
  ['font', /\.(ttf|otf)$/i],
  ['shader', /\.(shader|shadergraph|hlsl)$/i],
];

const seen = new Set();
const leaves = new Map();
const queue = [...args._];
while (queue.length) {
  const file = queue.shift();
  if (seen.has(file)) continue;
  seen.add(file);
  const abs = path.join(REPO, file);
  if (!fs.existsSync(abs) || fs.statSync(abs).isDirectory()) continue;
  const kind = KINDS.find(([, re]) => re.test(file))?.[0];
  if (kind) {
    leaves.set(file, kind);
    continue;
  }
  if (!YAML.test(file)) continue;
  const text = fs.readFileSync(abs, 'utf8');
  if (file.endsWith('.asset') && /^--- !u!43 /m.test(text.slice(0, 400))) leaves.set(file, 'mesh');
  for (const m of text.matchAll(/guid: ([0-9a-f]{32})/g)) {
    const p = guidToPath(m[1]);
    if (p && !p.endsWith('.cs') && !seen.has(p)) queue.push(p);
  }
}

const art = readArtMap();
const covered = new Set([
  ...Object.values(art.roles ?? {}).map((r) => r.file).filter(Boolean),
  ...Object.values(art.fonts ?? {}).map((f) => f.file),
]);
// A prefab role covers everything it pulls in (meshes, materials, their textures).
const viaPrefab = new Set();
const crawl = [...covered].filter((f) => f.endsWith('.prefab'));
while (crawl.length) {
  const file = crawl.pop();
  if (viaPrefab.has(file) || !YAML.test(file)) continue;
  viaPrefab.add(file);
  for (const m of fs.readFileSync(path.join(REPO, file), 'utf8').matchAll(/guid: ([0-9a-f]{32})/g)) {
    const p = guidToPath(m[1]);
    if (!p || p.endsWith('.cs')) continue;
    if (YAML.test(p)) crawl.push(p);
    else viaPrefab.add(p);
  }
}
const excluded = art.excluded ?? {};

const vendor = (f) => !args.all && /^(Packages\/|Assets\/TextMesh Pro\/|Assets\/SDK\/|Assets\/Plugins\/)/.test(f);
let missing = 0;
for (const [kind] of [...KINDS, ['mesh']]) {
  const files = [...leaves].filter(([f, k]) => k === kind && !vendor(f)).map(([f]) => f).sort();
  if (!files.length) continue;
  console.log(`\n${kind} (${files.length})`);
  for (const f of files) {
    const ok = covered.has(f) || viaPrefab.has(f) || kind === 'shader';
    const why = excluded[f];
    if (!ok && !why) missing++;
    console.log(`${ok ? '  ok      ' : why ? '  skip    ' : '  MISSING '}${f}${why && !ok ? `  (${why})` : ''}`);
  }
}
console.log(`\n${seen.size} files followed; ${missing} leaf asset(s) neither in art-map.yaml roles nor in its \`excluded\` list (shaders are ported by hand)`);
process.exitCode = missing ? 1 : 0;

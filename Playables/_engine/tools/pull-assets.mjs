// npm run pull -- <playable>
// Copies what scenario.yaml `assets` asks for out of the Unity project into <playable>/assets/ and records each
// file's source in assets/manifest.json (audit refuses a bundled file with no source in Assets/):
//   use:   [role, ...]          roles of Playables/art-map.yaml (pivot and 9-slice read from the texture's .meta)
//   fonts: [key, ...]           fonts of art-map.yaml
//   unity: { name: path[#Sprite] }  one-off game files not in the art map (repo-relative)
//   data:  { name: path }       level JSON and other data, imported from src as ../../assets/data/<name>.json
// `Path.png#SpriteName` (a sprite inside a sliced sheet) is listed into Temp/playable-export.txt for
// .claude/skills/playable-ads/scripts/export_sprites.cs, run through the Editor.
import fs from 'node:fs';
import path from 'node:path';
import sharp from 'sharp';
import { REPO, fail, parseArgs, playableDir, readScenario } from './lib/common.mjs';
import { readArtMap, readSpriteMeta, writeManifest } from './lib/art.mjs';
import { readMeshAsset, readPrefabParts, serializeParts } from './lib/unity.mjs';

const dir = playableDir(parseArgs()._[0]);
const assets = readScenario(dir).assets ?? {};
const art = readArtMap();
const raw = path.join(dir, 'assets', 'raw');
const data = path.join(dir, 'assets', 'data');
fs.rmSync(raw, { recursive: true, force: true });
fs.mkdirSync(raw, { recursive: true });
fs.mkdirSync(data, { recursive: true });

const IMAGE = /\.(png|jpe?g|psd|tga|tiff?)$/i;
const FONT = /\.(ttf|otf|woff2?)$/i;
const MODEL = /\.(fbx|glb)$/i;
const manifest = {};
const sprites = [];
let missing = 0;

const isUnityMesh = (file) => file.endsWith('.asset') && /^--- !u!43 /m.test(fs.readFileSync(file, 'utf8').slice(0, 400));

/** A prefab or Mesh .asset becomes <name>.mesh (parts, transforms, material values) plus its textures. */
async function convertUnityModel(name, source, extra) {
  const parts = source.endsWith('.prefab')
    ? readPrefabParts(source)
    : [{ mesh: readMeshAsset(source), meshPath: source, material: null, chain: [] }];
  const texName = (i) => `${name}_tex${i}`;
  fs.writeFileSync(path.join(raw, `${name}.mesh`), serializeParts(parts, texName));
  manifest[name] = { source, kind: 'model', ...extra, meshes: parts.map((p) => p.meshPath), materials: parts.map((p) => p.material?.path ?? null) };
  console.log(`OK   ${name} <- ${source} (${parts.length} mesh part${parts.length > 1 ? 's' : ''})`);
  for (const [i, p] of parts.entries())
    if (p.material?.texture) await copy(texName(i), p.material.texture, raw, { opt: { format: 'png', maxSize: 512 } });
}

async function copy(name, source, toDir, extra = {}) {
  const from = path.join(REPO, source);
  if (!source.startsWith('Assets/') || !fs.existsSync(from)) {
    missing++;
    return console.log(`FIX  ${name}: ${source} not found in the Unity project (paths start with Assets/)`);
  }
  if (source.endsWith('.prefab') || isUnityMesh(from)) return convertUnityModel(name, source, extra);
  fs.copyFileSync(from, path.join(toDir, name + path.extname(source)));
  const entry = { source, ...extra };
  if (IMAGE.test(source)) {
    const { width, height } = await sharp(from).metadata();
    Object.assign(entry, { kind: 'image', size: [width, height] }, readSpriteMeta(from));
  } else if (FONT.test(source)) entry.kind = 'font';
  else if (MODEL.test(source)) entry.kind = 'model';
  else if (toDir === data) entry.kind = 'data';
  else entry.kind = 'audio';
  manifest[name] = entry;
  console.log(`OK   ${name} <- ${source}${entry.slice ? ` (9-slice ${entry.slice.join('/')})` : ''}`);
}

for (const role of assets.use ?? []) {
  const entry = art.roles?.[role];
  if (!entry?.file) {
    missing++;
    console.log(`FIX  ${role}: no role '${role}' with a file in Playables/art-map.yaml`);
    continue;
  }
  const { file, maxSize, format, quality, bitrate } = entry;
  await copy(role, file, raw, { role, opt: { maxSize, format, quality, bitrate } });
}
for (const key of assets.fonts ?? []) {
  const font = art.fonts?.[key];
  if (!font?.file) {
    missing++;
    console.log(`FIX  font ${key}: not in art-map.yaml fonts`);
  } else await copy(key, font.file, raw, { family: font.family ?? key });
}
for (const [name, source] of Object.entries(assets.unity ?? {})) {
  const [file, sprite] = String(source).split('#');
  if (!sprite) await copy(name, file, raw);
  else {
    sprites.push(`${name}|${file}|${sprite}`);
    manifest[name] = { source: String(source), kind: 'image', anchor: [0.5, 0.5] };
  }
}
for (const [name, source] of Object.entries(assets.data ?? {})) await copy(name, String(source), data);

writeManifest(dir, manifest);
if (sprites.length) {
  fs.mkdirSync(path.join(REPO, 'Temp'), { recursive: true });
  fs.writeFileSync(path.join(REPO, 'Temp', 'playable-export.txt'), [raw, ...sprites].join('\n'));
  console.log(`\n${sprites.length} sprite(s) need the Editor:\n  unity command eval_file --file .claude/skills/playable-ads/scripts/export_sprites.cs`);
}
if (missing) fail(`${missing} source(s) missing`);
console.log(`next: npm run assets -- ${path.basename(dir)}`);

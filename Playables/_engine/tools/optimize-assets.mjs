// npm run assets -- <playable>
// assets/raw/* -> assets/opt/* within budget. Images: fit inside maxSize, WebP (or PNG/JPG). Audio: mono MP3
// through ffmpeg. Fonts and 3D models (FBX/GLB, .mesh converted from Unity prefabs): copied. Options, later wins: defaults < the role's line in art-map.yaml < scenario.yaml
//   assets: { maxSize: 1024, quality: 80, overrides: { bg: { maxSize: 1280, format: jpg, quality: 70 } } }
// Records each image's `density` (output px / source px) in assets/manifest.json, so game code keeps measuring
// in the game's source pixels whatever the compression did. Budget: what the inlined assets may weigh so the
// HTML stays under networks.json targetBytes after base64 (+33%) and the ~700KB of engine + Pixi.
import fs from 'node:fs';
import path from 'node:path';
import { spawnSync } from 'node:child_process';
import sharp from 'sharp';
import { kb, parseArgs, playableDir, readNetworks, readScenario } from './lib/common.mjs';
import { readManifest, writeManifest } from './lib/art.mjs';

const dir = playableDir(parseArgs()._[0]);
const cfg = readScenario(dir).assets ?? {};
const manifest = readManifest(dir);
const raw = path.join(dir, 'assets', 'raw');
const opt = path.join(dir, 'assets', 'opt');
fs.rmSync(opt, { recursive: true, force: true });
fs.mkdirSync(opt, { recursive: true });

const ENGINE_BYTES = 700_000;
const budget = Math.floor(((readNetworks().targetBytes - ENGINE_BYTES) * 3) / 4);
const hasFfmpeg = spawnSync('ffmpeg', ['-version']).status === 0;
const IMAGE = /\.(png|jpe?g|webp|psd|tga|tiff?)$/i;
const AUDIO = /\.(wav|mp3|ogg|m4a|aac|aif+)$/i;
const FONT = /\.(ttf|otf|woff2?)$/i;
const MODEL = /\.(fbx|glb|mesh)$/i;
const defined = (o = {}) => Object.fromEntries(Object.entries(o).filter(([, v]) => v !== undefined && v !== null));

const rows = [];
let total = 0;
for (const file of fs.existsSync(raw) ? fs.readdirSync(raw).filter((f) => !f.startsWith('.')) : []) {
  const name = file.replace(/\.[^.]+$/, '');
  const src = path.join(raw, file);
  const o = { maxSize: cfg.maxSize ?? 1024, quality: cfg.quality ?? 80, ...defined(manifest[name]?.opt), ...(cfg.overrides?.[name] ?? {}) };
  let out;
  if (IMAGE.test(file)) {
    const format = o.format ?? 'webp';
    out = path.join(opt, `${name}.${format === 'jpg' ? 'jpg' : format}`);
    let img = sharp(src).resize({ width: o.maxSize, height: o.maxSize, fit: 'inside', withoutEnlargement: true });
    if (format === 'png') img = img.png({ palette: true, quality: o.quality, compressionLevel: 9 });
    else if (format === 'jpg') img = img.flatten({ background: '#000000' }).jpeg({ quality: o.quality, mozjpeg: true });
    else img = img.webp({ quality: o.quality, alphaQuality: 90, effort: 6 });
    const info = await img.toFile(out);
    if (manifest[name]?.size) manifest[name].density = info.width / manifest[name].size[0];
  } else if (MODEL.test(file)) {
    out = path.join(opt, file);
    fs.copyFileSync(src, out);
  } else if (FONT.test(file)) {
    out = path.join(opt, file);
    fs.copyFileSync(src, out);
    if (fs.statSync(out).size > 150_000) console.log(`WARN ${file}: ${kb(fs.statSync(out).size)} font; a subset (only the glyphs used) would save most of it`);
  } else if (AUDIO.test(file)) {
    out = path.join(opt, `${name}.mp3`);
    if (hasFfmpeg) {
      const r = spawnSync('ffmpeg', ['-y', '-loglevel', 'error', '-i', src, '-ac', '1', '-ar', '44100', '-b:a', o.bitrate ?? '48k', out]);
      if (r.status !== 0) {
        console.log(`FIX  ${file}: ffmpeg failed ${r.stderr}`);
        continue;
      }
    } else if (/\.(mp3|ogg)$/i.test(file)) {
      out = path.join(opt, file);
      fs.copyFileSync(src, out);
      console.log(`WARN ${file}: copied as-is (no ffmpeg: brew install ffmpeg to make it mono mp3${/ogg$/i.test(file) ? '; Safari before 17 cannot play Ogg' : ''})`);
    } else {
      console.log(`FIX  ${file}: needs ffmpeg to become mp3 -> brew install ffmpeg`);
      continue;
    }
  } else {
    console.log(`WARN ${file}: not an image, font, model or audio file, skipped`);
    continue;
  }
  const after = fs.statSync(out).size;
  total += after;
  rows.push([path.basename(out), fs.statSync(src).size, after]);
}

writeManifest(dir, manifest);
rows.sort((a, b) => b[2] - a[2]);
for (const [file, before, after] of rows) console.log(`${file.padEnd(28)} ${kb(before).padStart(9)} -> ${kb(after).padStart(8)}`);
const line = `assets ${kb(total)} / budget ${kb(budget)} (base64 in HTML ~${kb((total * 4) / 3)})`;
console.log(total > budget ? `FIX  ${line} -> lower maxSize/quality of the top files, or format: jpg for opaque art` : `OK   ${line}`);

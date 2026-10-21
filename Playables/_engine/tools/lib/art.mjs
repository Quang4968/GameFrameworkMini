// The game side of a playable: Playables/art-map.yaml (what each visual role is in the Unity project), the
// game's localization file, Unity .meta reading (pivot, 9-slice border) and the per-playable asset manifest
// (assets/manifest.json: every bundled file -> its source in Assets/, written by pull, updated by assets).
import fs from 'node:fs';
import path from 'node:path';
import { parse } from 'yaml';
import { REPO, ROOT } from './common.mjs';

export function readArtMap() {
  const file = path.join(ROOT, 'art-map.yaml');
  return fs.existsSync(file) ? (parse(fs.readFileSync(file, 'utf8')) ?? {}) : {};
}

/** Localization as key -> text. Accepts `{ entries: [{ k, v }] }` (this framework's format) or a flat object. */
export function readStrings(art = readArtMap()) {
  if (!art.strings) return {};
  const data = JSON.parse(fs.readFileSync(path.join(REPO, art.strings), 'utf8'));
  if (Array.isArray(data.entries)) return Object.fromEntries(data.entries.map((e) => [e.k, e.v]));
  return data;
}

/** '@key' or '@key|arg0|arg1' -> the game's own string with {0}, {1} filled; anything else is a literal. */
export function resolveText(value, strings) {
  if (typeof value !== 'string' || !value.startsWith('@')) return value;
  const [key, ...args] = value.slice(1).split('|');
  if (!(key in strings)) throw new Error(`text '${value}': key '${key}' not in the game's localization`);
  return args.reduce((s, a, i) => s.replaceAll(`{${i}}`, a), strings[key]);
}

const ALIGNMENT = [[0.5, 0.5], [0, 1], [0.5, 1], [1, 1], [0, 0.5], [1, 0.5], [0, 0], [0.5, 0], [1, 0]];

/** Pivot and 9-slice border from a Unity texture .meta, converted to Pixi: anchor y from the top, slice [l, t, r, b]. */
export function readSpriteMeta(sourceFile) {
  const metaPath = `${sourceFile}.meta`;
  if (!fs.existsSync(metaPath)) return {};
  const meta = fs.readFileSync(metaPath, 'utf8');
  const vec = (name) => {
    const m = meta.match(new RegExp(`${name}: \\{x: ([-\\d.e]+), y: ([-\\d.e]+)(?:, z: ([-\\d.e]+), w: ([-\\d.e]+))?\\}`));
    return m ? m.slice(1).filter((v) => v !== undefined).map(Number) : null;
  };
  const alignment = Number(meta.match(/^\s*alignment: (\d+)/m)?.[1] ?? 0);
  const pivot = alignment === 9 ? vec('spritePivot') : ALIGNMENT[alignment] ?? [0.5, 0.5];
  const border = vec('spriteBorder');
  const out = { anchor: [pivot[0], 1 - pivot[1]] };
  if (border && border.some((v) => v > 0)) out.slice = [border[0], border[3], border[2], border[1]];
  return out;
}

export function manifestPath(dir) {
  return path.join(dir, 'assets', 'manifest.json');
}

export function readManifest(dir) {
  const file = manifestPath(dir);
  return fs.existsSync(file) ? JSON.parse(fs.readFileSync(file, 'utf8')) : {};
}

export function writeManifest(dir, manifest) {
  fs.writeFileSync(manifestPath(dir), JSON.stringify(manifest, null, 2));
}

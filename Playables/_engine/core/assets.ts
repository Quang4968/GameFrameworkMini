// Every file in <playable>/assets/opt/ (written by tools/optimize-assets.mjs) is bundled as a base64 data URL
// and keyed by its file name without extension: assets/opt/rail_h.webp -> sprite('rail_h').
// The glob path is root-relative, and the Vite root is the playable folder (tools/lib/common.mjs).
// sprite() and nineSlice() measure in the game's source pixels: the Unity pivot, 9-slice border and size come
// from config.sprites, and `density` undoes whatever downscale the asset step applied.
import { Assets, Container, NineSliceSprite, Sprite, type Texture, loadTextures } from 'pixi.js';
import config from 'virtual:playable-config';

const files = import.meta.glob('/assets/opt/*.{webp,png,jpg,jpeg,mp3,ogg,m4a,ttf,otf,woff,woff2,fbx,glb,mesh}', {
  eager: true,
  import: 'default',
}) as Record<string, string>;

const IMAGE = /\.(webp|png|jpe?g)$/i;
const FONT = /\.(ttf|otf|woff2?)$/i;
const AUDIO = /\.(mp3|ogg|m4a)$/i;
const textures = new Map<string, Texture>();

function key(path: string): string {
  return path.slice(path.lastIndexOf('/') + 1).replace(/\.[^.]+$/, '');
}

export function audioUrls(): Record<string, string> {
  const out: Record<string, string> = {};
  for (const [path, url] of Object.entries(files)) if (AUDIO.test(path)) out[key(path)] = url;
  return out;
}

export async function loadTexturesFromBundle(): Promise<void> {
  // Pixi's decode workers start from blob URLs, which some ad webviews block.
  loadTextures.config!.preferWorkers = false;
  await Promise.all(
    Object.entries(files)
      .filter(([path]) => IMAGE.test(path))
      .map(async ([path, url]) => textures.set(key(path), await Assets.load<Texture>(url))),
  );
}

/** Registers the bundled game fonts under their art-map family names; text drawn before this falls back. */
export async function loadFonts(): Promise<void> {
  await Promise.all(
    Object.entries(files)
      .filter(([path]) => FONT.test(path))
      .map(async ([path, url]) => {
        const face = new FontFace(config.art.fonts[key(path)] ?? key(path), `url(${url})`);
        try {
          document.fonts.add(await face.load());
        } catch {
          console.warn('[playable] font failed:', key(path));
        }
      }),
  );
}

/** Data URL of any bundled file by name (3D models and textures for _engine/three). */
export function bundledUrl(name: string): string {
  const hit = Object.entries(files).find(([path]) => key(path) === name);
  if (!hit) throw new Error(`[playable] nothing named '${name}' in assets/opt`);
  return hit[1];
}

export function texture(name: string): Texture {
  const t = textures.get(name);
  if (!t) throw new Error(`[playable] no texture '${name}' in assets/opt`);
  return t;
}

/** Bundled name of a texture loaded by this module, or null for generated ones (text, render textures). */
export function textureName(t: Texture): string | null {
  for (const [name, own] of textures) if (own === t || own.source === t.source) return name;
  return null;
}

export function hasTexture(name: string): boolean {
  return textures.has(name);
}

/** Sprite at the game's pivot, one unit = one source pixel. Multiply its scale for the size you need. */
export function sprite(name: string): Sprite {
  const s = new Sprite(texture(name));
  const info = config.sprites[name];
  s.anchor.set(...(info?.anchor ?? [0.5, 0.5]));
  s.scale.set(1 / (info?.density ?? 1));
  return s;
}

/** Size in source pixels of a bundled image, before any downscale. */
export function sourceSize(name: string): [number, number] {
  const info = config.sprites[name];
  return info?.size ?? [texture(name).width, texture(name).height];
}

/** 9-slice of a bundled image with the game's border, `width` x `height` design units, centred on (0, 0). */
export function nineSlice(name: string, width: number, height: number, borderScale = 1): Container {
  const info = config.sprites[name];
  const d = info?.density ?? 1;
  const [l, t, r, b] = (info?.slice ?? [0, 0, 0, 0]).map((v) => v * d);
  const s = new NineSliceSprite({ texture: texture(name), leftWidth: l, topHeight: t, rightWidth: r, bottomHeight: b });
  // NineSliceSprite draws borders at texture pixels; scale the whole sprite so borders land at borderScale/density.
  const k = borderScale / d;
  s.width = width / k;
  s.height = height / k;
  s.scale.set(k);
  s.position.set(-width / 2, -height / 2);
  const holder = new Container();
  holder.addChild(s);
  return holder;
}

/** Params of an art-map role, e.g. role<{ pxPerCell: number }>('rail_h').pxPerCell. */
export function role<T = Record<string, unknown>>(name: string): T {
  return (config.art.roles[name] ?? {}) as T;
}

/** CSS family of a bundled game font (art-map `fonts` key); falls back while the font is not bundled. */
export function font(key: string, fallback = 'Arial'): string {
  return config.art.fonts[key] ?? fallback;
}

export function color(name: string, fallback = '#ffffff'): string {
  return config.art.colors[name] ?? fallback;
}

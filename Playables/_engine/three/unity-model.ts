// Models converted from Unity prefabs / Mesh assets by `npm run pull` (<name>.mesh): every part's geometry with
// its in-prefab transform baked in, still in Unity's left-handed space, plus its material values. Put the group
// under a parent scaled (1, 1, -1) to enter a right-handed Three scene; the mirror also turns Unity's clockwise
// front faces into Three's. toonFromMaterial maps the game's Toon material values onto toonMaterial.
import { BufferAttribute, BufferGeometry, Group, Matrix4, Mesh, MeshBasicMaterial, Quaternion, Vector3 } from 'three';
import { bundledUrl } from '../core/assets';
import type { ToonColor, ToonOptions } from './toon';

interface Transform {
  position: number[];
  rotation: number[];
  scale: number[];
}

interface PartData {
  positions: string;
  normals: string | null;
  uvs: string | null;
  indices: string;
  wideIndices: boolean;
  chain: Transform[];
  texture: string | null;
  floats: Record<string, number>;
  colors: Record<string, number[]>;
}

export interface UnityPart {
  mesh: Mesh;
  /** Bundled texture name of the part's main texture. */
  texture: string | null;
  floats: Record<string, number>;
  colors: Record<string, number[]>;
}

function bytes(b64: string): ArrayBuffer {
  const bin = atob(b64);
  const out = new Uint8Array(bin.length);
  for (let i = 0; i < bin.length; i++) out[i] = bin.charCodeAt(i);
  return out.buffer;
}

export function loadUnityModel(name: string): { group: Group; parts: UnityPart[] } {
  const url = bundledUrl(name);
  const data = JSON.parse(new TextDecoder().decode(bytes(url.slice(url.indexOf(',') + 1)))) as { parts: PartData[] };
  const group = new Group();
  const parts = data.parts.map((p) => {
    const geo = new BufferGeometry();
    geo.setAttribute('position', new BufferAttribute(new Float32Array(bytes(p.positions)), 3));
    if (p.normals) geo.setAttribute('normal', new BufferAttribute(new Float32Array(bytes(p.normals)), 3));
    if (p.uvs) geo.setAttribute('uv', new BufferAttribute(new Float32Array(bytes(p.uvs)), 2));
    geo.setIndex(new BufferAttribute(p.wideIndices ? new Uint32Array(bytes(p.indices)) : new Uint16Array(bytes(p.indices)), 1));
    // Leaf first: v_prefab = M_top * ... * M_leaf * v.
    for (const t of p.chain) {
      const m = new Matrix4().compose(new Vector3(...t.position), new Quaternion(...t.rotation), new Vector3(...t.scale));
      geo.applyMatrix4(m);
    }
    const mesh = new Mesh(geo, new MeshBasicMaterial());
    group.add(mesh);
    return { mesh, texture: p.texture, floats: p.floats, colors: p.colors };
  });
  return { group, parts };
}

/** The game's Toon material (Game/Toon) as toonMaterial options; light and shadow colour come from the scene. */
export function toonFromMaterial(part: UnityPart): Omit<ToonOptions, 'shadowColor' | 'toLight'> & { outline: number; outlineColor: ToonColor } {
  const f = part.floats;
  const c = part.colors;
  return {
    color: c._Color ?? [1, 1, 1],
    channels: f._UseColorMap ? [c._RChannel ?? [1, 1, 1], c._GChannel ?? [1, 1, 1], c._BChannel ?? [1, 1, 1]] : undefined,
    steps: Math.max(1, f._Steps ?? 1),
    lightCut: f._LightCut ?? 1,
    outline: f._OutlineWidth ?? 0,
    outlineColor: c._OutlineColor ?? [0, 0, 0],
  };
}

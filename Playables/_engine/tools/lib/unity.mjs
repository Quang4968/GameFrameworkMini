// Reading Unity's own serialized files (text YAML), so a playable can take any game asset as the game stores it:
// guid -> path, Mesh .asset vertex/index buffers, and a prefab's mesh parts with their in-prefab transforms and
// material (texture + floats + colours). Output stays in Unity's left-handed space; _engine/three converts.
import fs from 'node:fs';
import path from 'node:path';
import { REPO } from './common.mjs';

let guidIndex;
/** guid -> repo-relative asset path, over every importer settings file under Assets/. */
export function guidToPath(guid) {
  if (!guidIndex) {
    guidIndex = new Map();
    const SKIP = new Set(['Library', 'Temp', 'Logs', 'node_modules']);
    const walk = (dir) => {
      for (const e of fs.readdirSync(dir, { withFileTypes: true })) {
        if (SKIP.has(e.name)) continue;
        const p = path.join(dir, e.name);
        if (e.isDirectory()) walk(p);
        else if (e.name.endsWith('.meta')) {
          const g = fs.readFileSync(p, 'utf8').match(/^guid: ([0-9a-f]{32})/m)?.[1];
          if (g) guidIndex.set(g, path.relative(REPO, p).slice(0, -5));
        }
      }
    };
    walk(path.join(REPO, 'Assets'));
  }
  return guidIndex.get(guid) ?? null;
}

/** Splits a Unity YAML file into its objects: { classId, fileId, body }. */
export function unityObjects(text) {
  const out = [];
  const re = /^--- !u!(\d+) &(-?\d+)[^\n]*\n([\s\S]*?)(?=^--- !u!|(?![\s\S]))/gm;
  for (const m of text.matchAll(re)) out.push({ classId: Number(m[1]), fileId: m[2], body: m[3] });
  return out;
}

const num = (s) => Number(s);
function vec(body, key) {
  const m = body.match(new RegExp(`${key}: \\{x: ([-\\d.eE]+), y: ([-\\d.eE]+), z: ([-\\d.eE]+)(?:, w: ([-\\d.eE]+))?\\}`));
  return m ? m.slice(1).filter((v) => v !== undefined).map(num) : null;
}

/** A Mesh .asset (class 43): positions, normals, uv0 and indices as typed arrays, Unity space. */
export function readMeshAsset(file) {
  const text = fs.readFileSync(path.join(REPO, file), 'utf8');
  const mesh = unityObjects(text).find((o) => o.classId === 43);
  if (!mesh) throw new Error(`${file}: no Mesh object`);
  const b = mesh.body;
  const count = num(b.match(/m_VertexCount: (\d+)/)[1]);
  const wide = num(b.match(/m_IndexFormat: (\d+)/)?.[1] ?? 0) === 1;
  const ibHex = b.match(/m_IndexBuffer: ([0-9a-f]*)/)[1];
  const ib = Buffer.from(ibHex, 'hex');
  const indices = wide ? new Uint32Array(ib.buffer.slice(ib.byteOffset, ib.byteOffset + ib.length)) : new Uint16Array(ib.buffer.slice(ib.byteOffset, ib.byteOffset + ib.length));
  // Channels: 0 position, 1 normal, 2 tangent, 3 colour, 4..11 uv0..uv7. Only float32 stream 0 is handled.
  const channels = [...b.matchAll(/- stream: (\d+)\s+offset: (\d+)\s+format: (\d+)\s+dimension: (\d+)/g)].map((m) => ({ stream: num(m[1]), offset: num(m[2]), format: num(m[3]), dim: num(m[4]) & 0xf }));
  const data = Buffer.from(b.match(/_typelessdata: ([0-9a-f]*)/)[1], 'hex');
  const stride = channels.filter((c) => c.stream === 0).reduce((s, c) => s + c.dim * 4, 0);
  const read = (ch) => {
    const c = channels[ch];
    if (!c || !c.dim) return null;
    if (c.format !== 0 || c.stream !== 0) throw new Error(`${file}: channel ${ch} is not float32 in stream 0`);
    const out = new Float32Array(count * c.dim);
    for (let v = 0; v < count; v++) for (let k = 0; k < c.dim; k++) out[v * c.dim + k] = data.readFloatLE(v * stride + c.offset + k * 4);
    return out;
  };
  return { positions: read(0), normals: read(1), uvs: read(4), indices };
}

/** Material: main texture path, floats and colours (sRGB, as Unity shows them). */
export function readMaterial(file) {
  const text = fs.readFileSync(path.join(REPO, file), 'utf8');
  const tex = text.match(/- _MainTex:\s+m_Texture: \{fileID: \d+, guid: ([0-9a-f]{32})/)?.[1];
  const floats = Object.fromEntries([...text.matchAll(/^\s+- (_\w+): ([-\d.eE]+)$/gm)].map((m) => [m[1], num(m[2])]));
  const colors = Object.fromEntries([...text.matchAll(/^\s+- (_\w+): \{r: ([-\d.eE]+), g: ([-\d.eE]+), b: ([-\d.eE]+), a: ([-\d.eE]+)\}/gm)].map((m) => [m[1], m.slice(2, 6).map(num)]));
  return { texture: tex ? guidToPath(tex) : null, floats, colors };
}

/**
 * A prefab's mesh parts: for every MeshFilter, its Mesh .asset with the transform chain up to the prefab root
 * baked in (root's own transform excluded, as when the game places the prefab), plus its first material.
 */
export function readPrefabParts(file) {
  const objs = unityObjects(fs.readFileSync(path.join(REPO, file), 'utf8'));
  const byId = new Map(objs.map((o) => [o.fileId, o]));
  const goOf = (o) => o.body.match(/m_GameObject: \{fileID: (-?\d+)\}/)?.[1];
  const transforms = objs.filter((o) => o.classId === 4);
  const transformOfGo = (go) => transforms.find((t) => goOf(t) === go);
  const parts = [];
  for (const mf of objs.filter((o) => o.classId === 33)) {
    const meshGuid = mf.body.match(/m_Mesh: \{fileID: \d+, guid: ([0-9a-f]{32})/)?.[1];
    const meshPath = meshGuid && guidToPath(meshGuid);
    if (!meshPath?.endsWith('.asset')) throw new Error(`${file}: mesh ${meshGuid} is not a Mesh .asset (${meshPath}); export FBX meshes as models instead`);
    const go = goOf(mf);
    const renderer = objs.find((o) => o.classId === 23 && goOf(o) === go);
    const matGuid = renderer?.body.match(/m_Materials:\s+- \{fileID: \d+, guid: ([0-9a-f]{32})/)?.[1];
    const chain = [];
    for (let t = transformOfGo(go); t; ) {
      const father = t.body.match(/m_Father: \{fileID: (-?\d+)\}/)?.[1];
      if (!father || father === '0') break; // the root: the game sets its transform when it places the prefab
      chain.push({ position: vec(t.body, 'm_LocalPosition'), rotation: vec(t.body, 'm_LocalRotation'), scale: vec(t.body, 'm_LocalScale') });
      t = byId.get(father);
    }
    parts.push({ mesh: readMeshAsset(meshPath), meshPath, material: matGuid ? { path: guidToPath(matGuid), ...readMaterial(guidToPath(matGuid)) } : null, chain });
  }
  return parts;
}

/** Serialises parts for the bundle: typed arrays as base64, texture as a bundled name. */
export function serializeParts(parts, textureName) {
  const b64 = (a) => (a ? Buffer.from(a.buffer, a.byteOffset, a.byteLength).toString('base64') : null);
  return JSON.stringify({
    unity: true,
    parts: parts.map((p, i) => ({
      positions: b64(p.mesh.positions),
      normals: b64(p.mesh.normals),
      uvs: b64(p.mesh.uvs),
      indices: b64(p.mesh.indices),
      wideIndices: p.mesh.indices instanceof Uint32Array,
      chain: p.chain,
      texture: p.material?.texture ? textureName(i) : null,
      floats: p.material?.floats ?? {},
      colors: p.material?.colors ?? {},
    })),
  });
}

// Copies a Pixi subtree into a Three.js group lying on the z = 0 plane, so a 2D scene the playable already
// draws can be filmed by a tilted 3D camera (the way a Unity game draws its board with sprites in a 3D world).
// Every visible Sprite and Text becomes a quad with the same texture, corners, tint and alpha; the ones `billboard`
// picks become camera-facing THREE.Sprites standing on their pivot (trees). Quads are centred on themselves so
// their scale animates around their middle. World units: `unit` design pixels, origin at the design centre,
// y up.
import { type Container, Matrix, RenderTexture, Sprite, Text } from 'pixi.js';
import {
  BufferAttribute,
  BufferGeometry,
  CanvasTexture,
  Color,
  Group,
  Mesh,
  MeshBasicMaterial,
  type Object3D,
  SRGBColorSpace,
  Sprite as ThreeSprite,
  SpriteMaterial,
  type Texture,
} from 'three';
import type { PlayableContext } from '../core/app';
import { textureName } from '../core/assets';
import { loadTexture } from './stage';

export interface MirrorOptions {
  /** Design pixels per world unit. */
  unit: number;
  skip?(o: Container): boolean;
  /** Texture names drawn as upright billboards instead of flat quads. */
  billboard?(name: string): boolean;
}

export interface Mirrored {
  group: Group;
  /** Pixi object -> its Three copy, to animate one piece (scale a tile away, flash a rail). */
  of: Map<Container, Object3D>;
}

const textures = new Map<string, Texture>();
function threeTexture(name: string): Texture {
  if (!textures.has(name)) textures.set(name, loadTexture(name));
  return textures.get(name)!;
}

function alphaOf(o: Container): number {
  let a = 1;
  for (let p: Container | null = o; p; p = p.parent) a *= p.alpha;
  return a;
}

export function mirrorToThree(ctx: PlayableContext, subtree: Container, opts: MirrorOptions): Mirrored {
  const { root, config, app } = ctx;
  // World transforms are current after a render. Render off screen: presenting a frame here would flash
  // whatever the caller just made visible for the copy.
  const scratch = RenderTexture.create({ width: 1, height: 1 });
  app.renderer.render({ container: app.stage, target: scratch });
  scratch.destroy(true);
  const toRoot = root.worldTransform.clone().invert();
  const W = config.design.width / 2;
  const H = config.design.height / 2;
  const world = (x: number, y: number) => [(x - W) / opts.unit, (H - y) / opts.unit] as const;
  const group = new Group();
  const of = new Map<Container, Object3D>();
  let order = 0;

  const add = (o: Container, texture: Texture, name: string | null) => {
    const m = new Matrix().copyFrom(toRoot).append(o.worldTransform);
    const b = o.getLocalBounds();
    const tint = new Color((o as Sprite).tint ?? 0xffffff);
    if (name && opts.billboard?.(name)) {
      const s = o as Sprite;
      const foot = m.apply({ x: 0, y: 0 });
      const top = m.apply({ x: 0, y: -b.height });
      const [fx, fy] = world(foot.x, foot.y);
      const sprite = new ThreeSprite(new SpriteMaterial({ map: texture, color: tint, alphaTest: 0.5, opacity: alphaOf(o) }));
      sprite.center.set(s.anchor.x, 1 - s.anchor.y);
      const h = Math.hypot(top.x - foot.x, top.y - foot.y) / opts.unit;
      sprite.scale.set((h * b.width) / b.height, h, 1);
      sprite.position.set(fx, fy, 0);
      sprite.renderOrder = 10_000 + order++;
      group.add(sprite);
      of.set(o, sprite);
      return;
    }
    const corners = [m.apply({ x: b.minX, y: b.minY }), m.apply({ x: b.maxX, y: b.minY }), m.apply({ x: b.maxX, y: b.maxY }), m.apply({ x: b.minX, y: b.maxY })].map((p) => world(p.x, p.y));
    const cx = corners.reduce((s, c) => s + c[0], 0) / 4;
    const cy = corners.reduce((s, c) => s + c[1], 0) / 4;
    const geo = new BufferGeometry();
    geo.setAttribute('position', new BufferAttribute(new Float32Array(corners.flatMap(([x, y]) => [x - cx, y - cy, 0])), 3));
    geo.setAttribute('uv', new BufferAttribute(new Float32Array([0, 1, 1, 1, 1, 0, 0, 0]), 2));
    geo.setIndex([0, 3, 2, 0, 2, 1]);
    const mat = new MeshBasicMaterial({ map: texture, color: tint, transparent: true, opacity: alphaOf(o), depthTest: false, depthWrite: false });
    const mesh = new Mesh(geo, mat);
    mesh.position.set(cx, cy, 0);
    mesh.renderOrder = order++;
    group.add(mesh);
    of.set(o, mesh);
  };

  const walk = (o: Container) => {
    if (!o.visible || alphaOf(o) <= 0 || opts.skip?.(o)) return;
    if (o instanceof Text) {
      const copy = new Text({ text: o.text, style: o.style });
      copy.anchor.copyFrom(o.anchor);
      const canvas = app.renderer.extract.canvas({ target: copy, resolution: 2 }) as HTMLCanvasElement;
      const t = new CanvasTexture(canvas);
      t.colorSpace = SRGBColorSpace;
      add(o, t, null);
      copy.destroy();
    } else if (o instanceof Sprite) {
      const name = textureName(o.texture);
      if (name) add(o, threeTexture(name), name);
    }
    for (const child of o.children) walk(child as Container);
  };
  walk(subtree);
  return { group, of };
}

// A Three.js scene shown inside the Pixi stage, for moments the game shows in 3D (its win camera tilt with 3D
// models). Three renders to its own canvas; `view` is a Pixi sprite of that canvas, so Pixi UI still layers above
// and below it. Renders on the Pixi ticker, so it pauses with the ad. The orthographic camera starts matching
// the Pixi root exactly (world unit = `unit` design px, origin at the design centre, y up), so swapping the 2D
// scene for its 3D copy is seamless. Import only from playables that need 3D: Three.js adds ~600KB.
import { type Rectangle, Sprite, Texture as PixiTexture } from 'pixi.js';
import {
  LoadingManager,
  NearestFilter,
  OrthographicCamera,
  SRGBColorSpace,
  Scene,
  type Group,
  type Texture,
  TextureLoader,
  WebGLRenderer,
} from 'three';
import { FBXLoader } from 'three/examples/jsm/loaders/FBXLoader.js';
import type { PlayableContext } from '../core/app';
import { bundledUrl } from '../core/assets';

export class ThreeStage {
  readonly scene = new Scene();
  readonly renderer: WebGLRenderer;
  readonly camera = new OrthographicCamera(-1, 1, 1, -1, 0.1, 1000);
  readonly view: Sprite;
  /** Visible half height in world units at zoom 1 (the Pixi view). */
  baseHalfHeight = 1;
  private readonly tick = () => this.render();

  constructor(private readonly ctx: PlayableContext, readonly unit: number) {
    this.renderer = new WebGLRenderer({ antialias: true, alpha: true, preserveDrawingBuffer: true });
    this.renderer.localClippingEnabled = true;
    this.renderer.outputColorSpace = SRGBColorSpace;
    this.renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
    this.view = new Sprite(PixiTexture.from(this.renderer.domElement));
    this.view.visible = false;
    this.camera.up.set(0, 1, 0);
    this.camera.position.set(0, 0, 10);
    this.camera.lookAt(0, 0, 0);
    this.fit(ctx.view);
    ctx.onResize((v) => this.fit(v));
    // Inspection hook for the preview and QA scripts, like __PIXI_APP__.
    (globalThis as { __THREE_STAGE__?: ThreeStage }).__THREE_STAGE__ = this;
  }

  /** World point of a design-space point (z = 0 plane). */
  toWorld(x: number, y: number): [number, number] {
    const { width, height } = this.ctx.config.design;
    return [(x - width / 2) / this.unit, (height / 2 - y) / this.unit];
  }

  /** Orthographic zoom relative to the Pixi view: 1.3 shows 1.3x more, like a larger orthographicSize. */
  setZoom(zoom: number): void {
    const h = this.baseHalfHeight * zoom;
    const aspect = this.ctx.view.width / this.ctx.view.height;
    this.camera.top = h;
    this.camera.bottom = -h;
    this.camera.left = -h * aspect;
    this.camera.right = h * aspect;
    this.camera.updateProjectionMatrix();
  }

  /** Renders every Pixi frame into `view`. */
  start(): void {
    this.view.visible = true;
    this.render();
    this.ctx.app.ticker.add(this.tick);
  }

  stop(): void {
    this.ctx.app.ticker.remove(this.tick);
  }

  private render(): void {
    this.renderer.render(this.scene, this.camera);
    this.view.texture.source.update();
  }

  private fit(v: Rectangle): void {
    const scale = this.ctx.root.scale.x;
    this.renderer.setSize(Math.round(v.width * scale), Math.round(v.height * scale), false);
    this.view.texture.source.resize(this.renderer.domElement.width, this.renderer.domElement.height);
    this.view.position.set(v.x, v.y);
    this.view.width = v.width;
    this.view.height = v.height;
    this.baseHalfHeight = v.height / 2 / this.unit;
    this.setZoom(1);
  }
}

function dataUrlToBuffer(url: string): ArrayBuffer {
  const bin = atob(url.slice(url.indexOf(',') + 1));
  const bytes = new Uint8Array(bin.length);
  for (let i = 0; i < bin.length; i++) bytes[i] = bin.charCodeAt(i);
  return bytes.buffer;
}

/** A bundled FBX by name, parsed in memory (no request). Its own texture paths are ignored: assign maps. */
export function loadModel(name: string): Group {
  // Own manager: the FBX names its textures by file path, and loading them would be a request, so they are
  // redirected to nothing here (maps are assigned by the caller). The default manager stays untouched.
  const manager = new LoadingManager();
  manager.setURLModifier(() => 'data:,');
  const loader = new FBXLoader(manager);
  return loader.parse(dataUrlToBuffer(bundledUrl(name)), '');
}

/** A bundled image as a Three texture; `pixelated` keeps palette textures (one colour per texel) crisp. */
export function loadTexture(name: string, pixelated = false): Texture {
  const t = new TextureLoader().load(bundledUrl(name));
  t.colorSpace = SRGBColorSpace;
  if (pixelated) t.magFilter = t.minFilter = NearestFilter;
  return t;
}


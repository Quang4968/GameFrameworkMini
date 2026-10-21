// Tutorial hand: taps, double-taps or drags along a path, looping until hide(). With `frames` (texture names,
// finger raised -> pressed) a press plays them like the game's hand; without, the hand squashes. `ripple`
// spawns that texture as an expanding ring under the fingertip at the bottom of each press. `anchor` is the
// fingertip in the texture (0..1). Falls back to a drawn pointer when no hand art is bundled.
import { Container, Graphics, Sprite, type Texture } from 'pixi.js';
import { hasTexture, texture } from './assets';
import { ease, type Tweener } from './tween';

export interface Point {
  x: number;
  y: number;
}

export interface HandOptions {
  frames?: string[];
  ripple?: string;
  /** Ripple diameter in design units. */
  rippleSize?: number;
  anchor?: Point;
  rotation?: number;
  scale?: number;
}

export class TutorialHand {
  readonly view = new Container();
  private readonly art: Sprite | Graphics;
  private readonly frames: Texture[];
  private run?: AbortController;
  private readonly baseScale: number;

  constructor(private readonly tween: Tweener, private readonly opts: HandOptions = {}) {
    this.baseScale = opts.scale ?? 1;
    this.frames = (opts.frames ?? []).filter(hasTexture).map(texture);
    const still = this.frames[0] ?? (hasTexture('hand') ? texture('hand') : undefined);
    if (still) {
      const s = new Sprite(still);
      s.anchor.set(opts.anchor?.x ?? 0.2, opts.anchor?.y ?? 0.05);
      s.rotation = opts.rotation ?? 0;
      this.art = s;
    } else {
      this.art = new Graphics().circle(0, 0, 34).fill({ color: 0xffffff, alpha: 0.85 }).stroke({ width: 6, color: 0x222222 });
    }
    this.view.addChild(this.art);
    this.view.visible = false;
    this.view.eventMode = 'none';
  }

  /** Loops a tap on `at` until hide(). */
  showTap(at: Point): void {
    const signal = this.restart();
    this.view.position.set(at.x, at.y);
    void this.tween.loop(async () => {
      await this.press(220, signal);
      await this.tween.wait(350, signal);
    }, signal);
  }

  /** Loops a double tap on `at` until hide(). */
  showDoubleTap(at: Point): void {
    const signal = this.restart();
    this.view.position.set(at.x, at.y);
    void this.tween.loop(async () => {
      await this.press(110, signal);
      await this.press(110, signal);
      await this.tween.wait(650, signal);
    }, signal);
  }

  /** Loops a drag through `path` (design coordinates) until hide(). */
  showDrag(path: Point[], msPerSegment = 380): void {
    if (path.length < 2) return;
    const signal = this.restart();
    void this.tween.loop(async () => {
      this.view.position.set(path[0].x, path[0].y);
      this.view.alpha = 0;
      this.setFrame(this.frames.length - 1);
      await this.tween.to(this.view, { alpha: 1 }, 150, { signal });
      for (const p of path.slice(1)) await this.tween.to(this.view.position, { x: p.x, y: p.y }, msPerSegment, { ease: ease.sineInOut, signal });
      await this.tween.to(this.view, { alpha: 0 }, 150, { signal, delay: 200 });
      this.setFrame(0);
      await this.tween.wait(300, signal);
    }, signal);
  }

  hide(): void {
    this.run?.abort();
    this.view.visible = false;
  }

  /** Down then up over 2 x `ms`. */
  private async press(ms: number, signal: AbortSignal): Promise<void> {
    if (this.frames.length > 1) {
      const step = ms / (this.frames.length - 1);
      for (let i = 1; i < this.frames.length; i++) {
        await this.tween.wait(step, signal);
        this.setFrame(i);
      }
      this.spawnRipple();
      for (let i = this.frames.length - 2; i >= 0; i--) {
        await this.tween.wait(step, signal);
        this.setFrame(i);
      }
      return;
    }
    const down = this.baseScale * 0.8;
    await this.tween.to(this.view.scale, { x: down, y: down }, ms, { ease: ease.quadOut, signal });
    this.spawnRipple();
    await this.tween.to(this.view.scale, { x: this.baseScale, y: this.baseScale }, ms, { ease: ease.quadOut, signal });
  }

  private setFrame(i: number): void {
    if (this.art instanceof Sprite && this.frames[i]) this.art.texture = this.frames[i];
  }

  private spawnRipple(): void {
    if (!this.opts.ripple || !hasTexture(this.opts.ripple)) return;
    const ring = new Sprite(texture(this.opts.ripple));
    ring.anchor.set(0.5);
    const full = (this.opts.rippleSize ?? 200) / ring.texture.width / this.baseScale;
    ring.scale.set(full * 0.3);
    this.view.addChildAt(ring, 0);
    void Promise.all([
      this.tween.to(ring.scale, { x: full, y: full }, 450, { ease: ease.quadOut }),
      this.tween.to(ring, { alpha: 0 }, 450, { ease: ease.quadOut }),
    ]).then(() => ring.destroy());
  }

  private restart(): AbortSignal {
    this.run?.abort();
    this.run = new AbortController();
    this.view.visible = true;
    this.view.alpha = 1;
    this.view.scale.set(this.baseScale);
    this.setFrame(0);
    return this.run.signal;
  }
}

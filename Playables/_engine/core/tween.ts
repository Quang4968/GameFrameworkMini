// Minimal tween + wait on the Pixi ticker, so both stop when the ad is paused (app.ticker.stop()).
// Kept in-house: a tween library costs 30-70KB of the 5MB budget for features a playable never uses.
import type { Ticker } from 'pixi.js';

export type Ease = (t: number) => number;

export const ease = {
  linear: (t: number) => t,
  quadIn: (t: number) => t * t,
  quadOut: (t: number) => 1 - (1 - t) * (1 - t),
  quadInOut: (t: number) => (t < 0.5 ? 2 * t * t : 1 - Math.pow(-2 * t + 2, 2) / 2),
  sineInOut: (t: number) => -(Math.cos(Math.PI * t) - 1) / 2,
  backOut: (t: number) => 1 + 2.70158 * Math.pow(t - 1, 3) + 1.70158 * Math.pow(t - 1, 2),
  backIn: (t: number) => 2.70158 * t * t * t - 1.70158 * t * t,
  cubicInOut: (t: number) => (t < 0.5 ? 4 * t * t * t : 1 - Math.pow(-2 * t + 2, 3) / 2),
  quartInOut: (t: number) => (t < 0.5 ? 8 * t * t * t * t : 1 - Math.pow(-2 * t + 2, 4) / 2),
} satisfies Record<string, Ease>;

type NumericKeys<T> = { [K in keyof T]: T[K] extends number ? K : never }[keyof T];

export interface TweenOptions {
  ease?: Ease;
  delay?: number;
  /** Resolves early (without snapping to the end) once aborted. */
  signal?: AbortSignal;
  /** Runs after every step, for values that drive something else (a position along a path). */
  onUpdate?: () => void;
}

export class Tweener {
  constructor(private readonly ticker: Ticker) {}

  /** Animates numeric fields of `target` to `to` over `ms`. `tween(sprite.scale, { x: 1.2, y: 1.2 }, 200)`. */
  to<T extends object>(target: T, to: Partial<Pick<T, NumericKeys<T>>>, ms: number, opts: TweenOptions = {}): Promise<void> {
    const keys = Object.keys(to) as Array<NumericKeys<T>>;
    const from = keys.map((k) => target[k] as unknown as number);
    const easeFn = opts.ease ?? ease.quadOut;
    let elapsed = -(opts.delay ?? 0);
    return new Promise((resolve) => {
      const tick = (t: Ticker) => {
        if (opts.signal?.aborted) return finish();
        elapsed += t.deltaMS;
        if (elapsed < 0) return;
        const p = ms <= 0 ? 1 : Math.min(1, elapsed / ms);
        const e = easeFn(p);
        keys.forEach((k, i) => {
          (target as Record<NumericKeys<T>, number>)[k] = from[i] + ((to[k] as unknown as number) - from[i]) * e;
        });
        opts.onUpdate?.();
        if (p >= 1) finish();
      };
      const finish = () => {
        this.ticker.remove(tick);
        resolve();
      };
      this.ticker.add(tick);
    });
  }

  /** Waits `ms` of game time (pauses with the ad). */
  wait(ms: number, signal?: AbortSignal): Promise<void> {
    return this.to({ v: 0 }, { v: 1 }, ms, { ease: ease.linear, signal });
  }

  /** Repeats `step` until aborted; for pulses, idle bobbing, tutorial loops. */
  async loop(step: () => Promise<void>, signal: AbortSignal): Promise<void> {
    while (!signal.aborted) await step();
  }
}

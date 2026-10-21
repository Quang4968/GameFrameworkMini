// What every network adapter shares: the interface the game sees and the pause/visibility plumbing.
import type { NetworkName, PlayableConfig } from '../config';

export interface AdNetwork {
  readonly name: NetworkName;
  /** Resolves when the host is ready and the ad is on screen; nothing may play before it. */
  ready(): Promise<void>;
  /** Host hid/backgrounded the ad (or muted it): stop the ticker and audio. */
  onPause(cb: () => void): void;
  onResume(cb: () => void): void;
  /** The level is over and the end card is showing. Call once. */
  gameEnd(): void;
  /** Open the store. Only ever from a user tap; auto-redirect gets the creative rejected. */
  cta(): void;
}

export function storeUrl(config: PlayableConfig): string {
  const ua = navigator.userAgent || '';
  const own = /iPhone|iPad|iPod|Macintosh/i.test(ua) ? config.store.ios : config.store.android;
  // An ad bought for one platform may still be served to the other: fall back to the link it has.
  return own || config.store.android || config.store.ios;
}

/** Pause/resume on tab visibility; every adapter adds it on top of the host's own signals. */
export function watchVisibility(pause: () => void, resume: () => void): void {
  document.addEventListener('visibilitychange', () => (document.hidden ? pause() : resume()));
}

export class Listeners {
  private readonly items: Array<() => void> = [];
  add(cb: () => void): void {
    this.items.push(cb);
  }
  emit(): void {
    for (const cb of this.items) cb();
  }
}

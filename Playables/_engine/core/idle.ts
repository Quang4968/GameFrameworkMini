// Calls `onIdle` once the player has not touched for `ms` of game time. kick() on every input restarts it;
// game time means the countdown stops while the ad is paused.
import type { Tweener } from './tween';

export class IdleTimer {
  private run?: AbortController;

  constructor(
    private readonly tween: Tweener,
    private readonly ms: number,
    private readonly onIdle: () => void,
  ) {}

  kick(): void {
    this.run?.abort();
    const run = (this.run = new AbortController());
    void this.tween.wait(this.ms, run.signal).then(() => {
      if (!run.signal.aborted) this.onIdle();
    });
  }

  stop(): void {
    this.run?.abort();
  }
}

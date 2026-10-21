// Runs the beats of scenario.yaml in order. Each beat is one awaited function with the same id as in the YAML,
// so a reviewer can map screenshot -> beat -> code. `tuning.maxPlaySec` after the first touch cuts straight to
// the beat with id 'endcard' (every network wants the store reachable in bounded time).
import type { PlayableContext } from '../core/app';

export interface Beat {
  id: string;
  /** `signal` aborts when the play timer runs out; long beats pass it to tweens and waits. */
  run(ctx: PlayableContext, signal: AbortSignal): Promise<void>;
}

export async function runBeats(ctx: PlayableContext, beats: Beat[]): Promise<void> {
  const endIndex = beats.findIndex((b) => b.id === 'endcard');
  if (endIndex < 0) throw new Error("[playable] scenario has no 'endcard' beat");
  const timeUp = new AbortController();

  void ctx.firstTouch.then(() => ctx.tween.wait(ctx.config.tuning.maxPlaySec * 1000)).then(() => timeUp.abort());
  const timeout = new Promise<'timeout'>((resolve) => timeUp.signal.addEventListener('abort', () => resolve('timeout')));

  for (let i = 0; i < endIndex; i++) {
    const result = await Promise.race([beats[i].run(ctx, timeUp.signal), timeout]);
    if (result === 'timeout') break;
  }
  timeUp.abort();
  for (const beat of beats.slice(endIndex)) await beat.run(ctx, timeUp.signal);
}

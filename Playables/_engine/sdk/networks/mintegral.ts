// Mintegral host API: the page announces gameReady(), the host calls window.gameStart() to begin and
// window.gameClose() when it closes the ad; the page reports gameEnd() and opens the store with install().
// Every call is guarded: the names come from third-party docs, confirm them in the Mintegral test tool.
import { type AdNetwork, Listeners, watchVisibility } from '../host';

type HostFn = (() => void) | undefined;
interface MintegralWindow {
  gameReady?: HostFn;
  gameEnd?: HostFn;
  install?: HostFn;
  gameStart?: () => void;
  gameClose?: () => void;
}

export function createMintegralNetwork(): AdNetwork {
  const w = window as unknown as MintegralWindow;
  const paused = new Listeners();
  const resumed = new Listeners();
  watchVisibility(() => paused.emit(), () => resumed.emit());
  let ended = false;

  return {
    name: 'mintegral',
    ready: () =>
      new Promise<void>((resolve) => {
        let started = false;
        const start = () => {
          if (started) return;
          started = true;
          resolve();
        };
        w.gameStart = start;
        w.gameClose = () => paused.emit();
        w.gameReady?.();
        // A host that never calls gameStart (local test, preview tool) must not leave a black screen.
        setTimeout(start, 1500);
      }),
    onPause: (cb) => paused.add(cb),
    onResume: (cb) => resumed.add(cb),
    gameEnd: () => {
      if (ended) return;
      ended = true;
      w.gameEnd?.();
    },
    cta: () => w.install?.(),
  };
}

// AppLovin (MRAID 2.0) and Unity Ads (MRAID 3.0). Both: wait for `ready`, then for the ad to be viewable,
// CTA through mraid.open(storeUrl). Unity rejects a playable that starts before viewableChange(true).
// Every call goes through the global `mraid` by name: the networks' upload validators search the file for the
// literal `mraid.open(` (and friends), and a global keeps its name through minification where a local alias
// (`const m = window.mraid`) becomes `r.open(...)` and fails validation.
import type { PlayableConfig } from '../../config';
import { type AdNetwork, Listeners, storeUrl, watchVisibility } from '../host';

interface Mraid {
  getState(): string;
  isViewable(): boolean;
  addEventListener(event: string, cb: (...args: unknown[]) => void): void;
  removeEventListener(event: string, cb: (...args: unknown[]) => void): void;
  open(url: string): void;
}

declare const mraid: Mraid | undefined;
const hasMraid = () => typeof mraid !== 'undefined' && !!mraid;

export function createMraidNetwork(name: 'applovin' | 'unity', config: PlayableConfig): AdNetwork {
  const paused = new Listeners();
  const resumed = new Listeners();
  watchVisibility(() => paused.emit(), () => resumed.emit());

  function whenViewable(done: () => void): void {
    if (mraid!.isViewable()) return done();
    const onChange = (viewable: unknown) => {
      if (!viewable) return;
      mraid!.removeEventListener('viewableChange', onChange);
      done();
    };
    mraid!.addEventListener('viewableChange', onChange);
  }

  return {
    name,
    ready: () =>
      new Promise<void>((resolve) => {
        if (!hasMraid()) return resolve(); // opened outside an ad container (local test)
        const start = () => {
          mraid!.addEventListener('viewableChange', (viewable: unknown) => (viewable ? resumed.emit() : paused.emit()));
          mraid!.addEventListener('audioVolumeChange', (volume: unknown) => (volume === 0 ? paused.emit() : resumed.emit()));
          whenViewable(resolve);
        };
        if (mraid!.getState() === 'loading') mraid!.addEventListener('ready', start);
        else start();
      }),
    onPause: (cb) => paused.add(cb),
    onResume: (cb) => resumed.add(cb),
    gameEnd: () => {},
    cta: () => {
      const url = storeUrl(config);
      if (hasMraid()) mraid!.open(url);
      else window.open(url);
    },
  };
}

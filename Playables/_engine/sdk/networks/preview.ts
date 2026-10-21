// `npm run dev`: no host. Logs every call so the author sees when gameEnd and the CTA fire.
import type { PlayableConfig } from '../../config';
import { type AdNetwork, Listeners, storeUrl, watchVisibility } from '../host';

export function createPreviewNetwork(config: PlayableConfig): AdNetwork {
  const paused = new Listeners();
  const resumed = new Listeners();
  watchVisibility(() => paused.emit(), () => resumed.emit());

  return {
    name: 'preview',
    ready: () => Promise.resolve(),
    onPause: (cb) => paused.add(cb),
    onResume: (cb) => resumed.add(cb),
    gameEnd: () => console.info('[playable] gameEnd'),
    cta: () => console.info('[playable] cta ->', storeUrl(config)),
  };
}

// Google Ads App campaigns: no lifecycle API; the head loads exitapi.js (tools/build.mjs adds it) and the
// CTA calls ExitApi.exit(), which opens the final URL set in the campaign. Without exitapi.js Google adds its
// own Install button over the ad.
import { type AdNetwork, Listeners, watchVisibility } from '../host';

export function createGoogleNetwork(): AdNetwork {
  const paused = new Listeners();
  const resumed = new Listeners();
  watchVisibility(() => paused.emit(), () => resumed.emit());

  return {
    name: 'google',
    ready: () => Promise.resolve(),
    onPause: (cb) => paused.add(cb),
    onResume: (cb) => resumed.add(cb),
    gameEnd: () => {},
    cta: () => (window as unknown as { ExitApi?: { exit(): void } }).ExitApi?.exit(),
  };
}

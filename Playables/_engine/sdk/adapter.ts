// The one seam between a playable and the ad host. Game code calls only `AdNetwork`; the network is fixed
// at build time through `__NETWORK__`, so the minifier drops every other adapter from the bundle.
import type { PlayableConfig } from '../config';
import type { AdNetwork } from './host';
import { createMraidNetwork } from './networks/mraid';
import { createMintegralNetwork } from './networks/mintegral';
import { createGoogleNetwork } from './networks/google';
import { createPreviewNetwork } from './networks/preview';

export type { AdNetwork } from './host';

export function createAdNetwork(config: PlayableConfig): AdNetwork {
  if (__NETWORK__ === 'applovin' || __NETWORK__ === 'unity') return createMraidNetwork(__NETWORK__, config);
  if (__NETWORK__ === 'mintegral') return createMintegralNetwork();
  if (__NETWORK__ === 'google') return createGoogleNetwork();
  return createPreviewNetwork(config);
}

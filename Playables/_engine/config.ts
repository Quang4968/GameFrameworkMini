// Runtime half of scenario.yaml (`meta`, `tuning`, `texts`) plus the game's art map and sprite data, injected at
// build time as `virtual:playable-config` (tools/lib/common.mjs runtimeConfig). Beats and level notes stay in
// the YAML for the author.
export type Orientation = 'portrait' | 'landscape' | 'both';

export interface PlayableConfig {
  name: string;
  orientation: Orientation;
  /** Design resolution the game lays out in; the root is scaled to fit the screen. */
  design: { width: number; height: number };
  background: string;
  store: { ios: string; android: string };
  /** Platforms the ad is bought for (scenario meta.platforms); only their store links are required. */
  platforms: Array<'android' | 'ios'>;
  tuning: {
    /** Seconds after the first touch before the end card is forced. */
    maxPlaySec: number;
    /** Seconds without input before the tutorial hand comes back. */
    idleHintSec: number;
    [key: string]: number;
  };
  /** Resolved strings: '@key' in scenario.yaml becomes the game's own localized text (may hold <color=#hex>). */
  texts: Record<string, string>;
  /** From Playables/art-map.yaml: named colours, font key -> CSS family, per-role params (sizes, tints). */
  art: {
    colors: Record<string, string>;
    /** Game-wide measures (px per world unit, board offsets) the playable reproduces. */
    layout: Record<string, number>;
    fonts: Record<string, string>;
    roles: Record<string, Record<string, unknown>>;
  };
  /** scenario.yaml `level` as written; the game kit validates and reads it (Playables/_game/level.ts). */
  level?: unknown;
  /** Per bundled image, read from the Unity texture importer settings via assets/manifest.json. */
  sprites: Record<string, SpriteInfo>;
}

export interface SpriteInfo {
  /** Pixi anchor (Unity pivot, y flipped). */
  anchor: [number, number];
  /** 9-slice border in source pixels: left, top, right, bottom. */
  slice?: [number, number, number, number];
  /** Source size in the Unity project, pixels. */
  size: [number, number];
  /** Bundled pixels per source pixel (< 1 when the asset step downscaled it). */
  density: number;
}

export type NetworkName = 'applovin' | 'unity' | 'mintegral' | 'google' | 'preview';

declare global {
  const __NETWORK__: NetworkName;
}

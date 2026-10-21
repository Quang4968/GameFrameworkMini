// Entry point every playable calls once from src/main.ts. Boots Pixi, loads the bundled textures, waits for the
// ad host (`AdNetwork.ready`), wires pause/resume, then hands the game a `PlayableContext`.
// The game lays out in design coordinates on `root`; `view` is the visible part of that space, which grows on
// one axis when the screen aspect differs from the design (landscape on a portrait design, tablets).
import { Application, Container, Rectangle } from 'pixi.js';
import config from 'virtual:playable-config';
import type { PlayableConfig } from '../config';
import { type AdNetwork, createAdNetwork } from '../sdk/adapter';
import { audioUrls, loadFonts, loadTexturesFromBundle } from './assets';
import { Audio } from './audio';
import { Tweener } from './tween';

export interface PlayableContext {
  app: Application;
  config: PlayableConfig;
  ad: AdNetwork;
  audio: Audio;
  tween: Tweener;
  root: Container;
  view: Rectangle;
  onResize(cb: (view: Rectangle) => void): void;
  /** Resolves on the first touch anywhere: the play timer and audio may start only after it. */
  firstTouch: Promise<void>;
}

export function startPlayable(create: (ctx: PlayableContext) => Promise<void> | void): void {
  boot(create).catch((e) => console.error('[playable] boot failed', e));
}

async function boot(create: (ctx: PlayableContext) => Promise<void> | void): Promise<void> {
  const ad = createAdNetwork(config);
  const app = new Application();
  await app.init({
    resizeTo: window,
    background: config.background,
    antialias: true,
    autoDensity: true,
    resolution: Math.min(window.devicePixelRatio || 1, 2),
    preference: 'webgl',
  });
  app.canvas.style.display = 'block';
  app.canvas.style.touchAction = 'none';
  document.body.appendChild(app.canvas);
  // Pixi devtools hook; also lets the preview and QA scripts inspect the stage.
  (globalThis as { __PIXI_APP__?: Application }).__PIXI_APP__ = app;

  const audio = new Audio();
  audio.register(audioUrls());
  await Promise.all([loadTexturesFromBundle(), loadFonts()]);

  const root = new Container();
  app.stage.addChild(root);
  app.stage.eventMode = 'static';
  const view = new Rectangle();
  const resizeListeners: Array<(view: Rectangle) => void> = [];
  const layout = () => {
    const { width: dw, height: dh } = config.design;
    const sw = app.screen.width;
    const sh = app.screen.height;
    const scale = Math.min(sw / dw, sh / dh);
    root.scale.set(scale);
    root.position.set((sw - dw * scale) / 2, (sh - dh * scale) / 2);
    view.x = -root.position.x / scale;
    view.y = -root.position.y / scale;
    view.width = sw / scale;
    view.height = sh / scale;
    app.stage.hitArea = app.screen;
    for (const cb of resizeListeners) cb(view);
  };
  app.renderer.on('resize', layout);
  layout();

  const firstTouch = new Promise<void>((resolve) => {
    const onDown = () => {
      app.canvas.removeEventListener('pointerdown', onDown);
      void audio.unlock();
      resolve();
    };
    app.canvas.addEventListener('pointerdown', onDown);
  });

  ad.onPause(() => {
    app.ticker.stop();
    audio.pause();
  });
  ad.onResume(() => {
    app.ticker.start();
    audio.resume();
  });

  app.ticker.stop();
  await ad.ready();
  app.ticker.start();

  await create({
    app,
    config,
    ad,
    audio,
    tween: new Tweener(app.ticker),
    root,
    view,
    onResize: (cb) => resizeListeners.push(cb),
    firstTouch,
  });
}

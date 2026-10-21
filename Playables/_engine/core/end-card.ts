// Standard end card: dimmed backdrop, the game's `logo` when bundled, optional title, a pulsing CTA button. Pass
// the game's own button (`button: Container`) to keep its look; a string draws a plain one. A tap anywhere on
// the card opens the store (the common practice on every network); the card reports gameEnd() once when shown.
import { Container, Graphics, Text } from 'pixi.js';
import type { PlayableContext } from './app';
import { hasTexture, sprite } from './assets';
import { ease } from './tween';

export interface EndCardOptions {
  title?: string | Container;
  button: string | Container;
  buttonColor?: number;
  dimAlpha?: number;
}

/** Rounded store button; the caller decides what a tap does (always `ctx.ad.cta()`). */
export function ctaButton(text: string, color = 0x2ecc71): Container {
  const button = new Container();
  const label = new Text({ text, style: { fontFamily: 'Arial', fontSize: 56, fontWeight: 'bold', fill: 0xffffff } });
  label.anchor.set(0.5);
  const w = Math.max(420, label.width + 120);
  button.addChild(new Graphics().roundRect(-w / 2, -60, w, 120, 60).fill(color), label);
  return button;
}

export async function showEndCard(ctx: PlayableContext, opts: EndCardOptions): Promise<void> {
  const { root, view, tween, ad } = ctx;
  const card = new Container();
  const dim = new Graphics();
  card.addChild(dim);

  const cx = ctx.config.design.width / 2;
  const cy = ctx.config.design.height / 2;
  let y = cy - 300;
  if (hasTexture('logo')) {
    const logo = sprite('logo');
    logo.anchor.set(0.5);
    logo.scale.set(logo.scale.x * Math.min(1, (ctx.config.design.width * 0.8) / logo.width));
    logo.position.set(cx, y);
    card.addChild(logo);
    y += logo.height / 2 + 60;
  }
  if (opts.title instanceof Container) {
    opts.title.position.set(cx, Math.max(y, cy - 120));
    card.addChild(opts.title);
  } else if (opts.title) {
    const title = new Text({ text: opts.title, style: { fontFamily: 'Arial', fontSize: 64, fontWeight: 'bold', fill: 0xffffff, align: 'center', wordWrap: true, wordWrapWidth: ctx.config.design.width * 0.8 } });
    title.anchor.set(0.5);
    title.position.set(cx, Math.max(y, cy - 120));
    card.addChild(title);
  }

  const button = typeof opts.button === 'string' ? ctaButton(opts.button, opts.buttonColor) : opts.button;
  button.position.set(cx, cy + 300);
  card.addChild(button);

  const fit = () => dim.clear().rect(view.x, view.y, view.width, view.height).fill({ color: 0x000000, alpha: opts.dimAlpha ?? 0.7 });
  fit();
  ctx.onResize(fit);

  card.eventMode = 'static';
  card.cursor = 'pointer';
  card.on('pointertap', () => ad.cta());
  root.addChild(card);

  card.alpha = 0;
  ad.gameEnd();
  await tween.to(card, { alpha: 1 }, 300);
  const pulse = new AbortController();
  void tween.loop(async () => {
    await tween.to(button.scale, { x: 1.08, y: 1.08 }, 450, { ease: ease.sineInOut, signal: pulse.signal });
    await tween.to(button.scale, { x: 1, y: 1 }, 450, { ease: ease.sineInOut, signal: pulse.signal });
  }, pulse.signal);
}

// Template game: tap the circle tuning.tapsToWin times, then the end card. One function per beat in
// scenario.yaml, same ids; replace the bodies, keep the shape.
import { Container, Graphics, Text } from 'pixi.js';
import type { PlayableContext } from '@engine/core/app';
import { TutorialHand } from '@engine/core/hand';
import { showEndCard } from '@engine/core/end-card';
import { ease } from '@engine/core/tween';
import type { Beat } from '@engine/scenario/beats';

export function createBeats(ctx: PlayableContext): Beat[] {
  const { root, config, tween } = ctx;
  const cx = config.design.width / 2;
  const cy = config.design.height / 2;

  const board = new Container();
  const target = new Graphics().circle(0, 0, 160).fill(0xf5a623);
  target.position.set(cx, cy);
  target.eventMode = 'static';
  board.addChild(target);
  const title = new Text({ text: config.texts.hook, style: { fontFamily: 'Arial', fontSize: 72, fontWeight: 'bold', fill: 0xffffff } });
  title.anchor.set(0.5);
  title.position.set(cx, cy - 500);
  board.addChild(title);
  const hand = new TutorialHand(tween);
  root.addChild(board, hand.view);

  const nextTap = () => new Promise<void>((resolve) => target.once('pointertap', () => resolve()));

  return [
    {
      id: 'hook',
      run: async () => {
        hand.showTap({ x: cx + 40, y: cy + 40 });
        await nextTap();
        hand.hide();
      },
    },
    {
      id: 'play',
      run: async (_ctx, signal) => {
        for (let taps = 1; taps < config.tuning.tapsToWin && !signal.aborted; taps++) {
          void tween.to(target.scale, { x: 1 - taps * 0.2, y: 1 - taps * 0.2 }, 200, { ease: ease.backOut });
          // Game time, not setTimeout: the hint must not fire while the ad is paused.
          const idle = new AbortController();
          void tween.wait(config.tuning.idleHintSec * 1000, idle.signal).then(() => {
            if (!idle.signal.aborted) hand.showTap({ x: cx + 40, y: cy + 40 });
          });
          await nextTap();
          idle.abort();
          hand.hide();
        }
        await tween.to(target, { alpha: 0 }, 250);
      },
    },
    {
      id: 'endcard',
      run: async () => {
        hand.hide();
        await showEndCard(ctx, { title: config.texts.win, button: config.texts.cta });
      },
    },
  ];
}

// Word-wrapped text that understands the game's TextMeshPro colour tags (`<color=#1B79DD>word</color>`) and
// line breaks, so localized strings render as in the game. One Pixi Text per word, laid out centred on (0, 0).
// Other TMP tags are stripped.
import { CanvasTextMetrics, Container, Text, TextStyle, type TextStyleOptions } from 'pixi.js';

interface Word {
  text: string;
  color?: string;
  breakBefore: boolean;
  /** Touches the previous word across a tag (`<color>tracks</color>!`): no space between them. */
  glued: boolean;
}

function parse(markup: string): Word[] {
  const words: Word[] = [];
  const colors: string[] = [];
  let breakNext = false;
  let spaceBefore = true;
  for (const part of markup.split(/(<color=#[0-9a-fA-F]{6,8}>|<\/color>|<[^>]+>|\n)/)) {
    if (!part) continue;
    const open = part.match(/^<color=(#[0-9a-fA-F]{6})/);
    if (open) colors.push(open[1]);
    else if (part === '</color>') colors.pop();
    else if (part === '\n') {
      breakNext = true;
      spaceBefore = true;
    } else if (!part.startsWith('<')) {
      part.split(/(\s+)/).forEach((w) => {
        if (/^\s+$/.test(w)) spaceBefore = true;
        else if (w) {
          words.push({ text: w, color: colors[colors.length - 1], breakBefore: breakNext, glued: !spaceBefore && !breakNext && words.length > 0 });
          breakNext = false;
          spaceBefore = false;
        }
      });
    }
  }
  return words;
}

export interface RichTextOptions {
  style: TextStyleOptions;
  /** Wrap width in design units. */
  width: number;
  lineHeight: number;
}

export class RichText extends Container {
  private readonly style: TextStyle;

  constructor(private readonly opts: RichTextOptions, markup = '') {
    super();
    this.style = new TextStyle(opts.style);
    this.text = markup;
  }

  set text(markup: string) {
    for (const child of this.removeChildren()) child.destroy();
    const space = CanvasTextMetrics.measureText(' ', this.style).width;
    const lines: Text[][] = [[]];
    let x = 0;
    for (const word of parse(markup)) {
      const t = new Text({ text: word.text, style: word.color ? { ...this.opts.style, fill: word.color } : this.style });
      t.anchor.set(0, 0.5);
      const line = lines[lines.length - 1];
      const gap = word.glued ? 0 : space;
      if (line.length && (word.breakBefore || (!word.glued && x + gap + t.width > this.opts.width))) {
        lines.push([]);
        x = 0;
      } else if (line.length) x += gap;
      t.x = x;
      x += t.width;
      lines[lines.length - 1].push(t);
    }
    const top = -((lines.length - 1) * this.opts.lineHeight) / 2;
    lines.forEach((line, i) => {
      const last = line[line.length - 1];
      const w = last ? last.x + last.width : 0;
      for (const t of line) {
        t.x -= w / 2;
        t.y = top + i * this.opts.lineHeight;
        this.addChild(t);
      }
    });
  }
}

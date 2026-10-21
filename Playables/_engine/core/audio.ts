// Web Audio from inlined data URLs. Silent until the first touch (every network requires it) and while the ad
// is paused. A failed decode only drops that sound: audio is never allowed to block the playable.
export class Audio {
  private ctx?: AudioContext;
  private readonly buffers = new Map<string, AudioBuffer>();
  private readonly pending = new Map<string, string>();
  private unlocked = false;
  private paused = false;
  private music?: AudioBufferSourceNode;

  /** Registers sounds; they decode on the first touch, when an AudioContext may legally start. */
  register(urls: Record<string, string>): void {
    for (const [name, url] of Object.entries(urls)) this.pending.set(name, url);
  }

  async unlock(): Promise<void> {
    if (this.unlocked) return;
    this.unlocked = true;
    const Ctor = window.AudioContext ?? (window as unknown as { webkitAudioContext?: typeof AudioContext }).webkitAudioContext;
    if (!Ctor) return;
    this.ctx = new Ctor();
    await Promise.all(
      [...this.pending].map(async ([name, url]) => {
        try {
          this.buffers.set(name, await this.ctx!.decodeAudioData(dataUrlToBuffer(url)));
        } catch {
          console.warn('[playable] audio decode failed:', name);
        }
      }),
    );
    if (this.paused) void this.ctx.suspend();
  }

  play(name: string, opts: { volume?: number; loop?: boolean } = {}): void {
    const buffer = this.buffers.get(name);
    if (!this.ctx || !buffer || this.paused) return;
    const src = this.ctx.createBufferSource();
    src.buffer = buffer;
    src.loop = opts.loop ?? false;
    const gain = this.ctx.createGain();
    gain.gain.value = opts.volume ?? 1;
    src.connect(gain).connect(this.ctx.destination);
    src.start();
    if (src.loop) {
      this.music?.stop();
      this.music = src;
    }
  }

  pause(): void {
    this.paused = true;
    void this.ctx?.suspend();
  }

  resume(): void {
    this.paused = false;
    void this.ctx?.resume();
  }
}

/** Decodes a base64 data URL in place: no fetch(), which network scanners read as an outbound request. */
function dataUrlToBuffer(url: string): ArrayBuffer {
  const bin = atob(url.slice(url.indexOf(',') + 1));
  const bytes = new Uint8Array(bin.length);
  for (let i = 0; i < bin.length; i++) bytes[i] = bin.charCodeAt(i);
  return bytes.buffer;
}

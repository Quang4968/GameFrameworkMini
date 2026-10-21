// npm run dev -- <playable> [--port 5173]
// Vite dev server with hot reload on <playable>/index.html, network 'preview' (CTA and gameEnd log to console).
import { createServer } from 'vite';
import { parseArgs, playableDir, viteConfig } from './lib/common.mjs';

const args = parseArgs();
const dir = playableDir(args._[0]);
const config = viteConfig(dir, 'preview');
const server = await createServer({ ...config, server: { ...config.server, port: Number(args.port ?? 5173) } });
await server.listen();
server.printUrls();

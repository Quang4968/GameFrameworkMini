// npm run new -- <playable> [--bare]
// Copies the game's template (Playables/_game/template: the game kit, every asset the gameplay shows, a working
// flow) to Playables/<playable>/, or the engine's bare _template when the game has none or with --bare.
// Stamps the name into scenario.yaml.
import fs from 'node:fs';
import path from 'node:path';
import { ROOT, fail, parseArgs } from './lib/common.mjs';

const args = parseArgs();
const name = args._[0];
if (!name || !/^[a-z0-9]+(-[a-z0-9]+)*$/.test(name)) fail('name must be kebab-case, e.g. npm run new -- tracks-hook-v1');
const target = path.join(ROOT, name);
if (fs.existsSync(target)) fail(`${name} already exists`);

const gameTemplate = path.join(ROOT, '_game', 'template');
const from = !args.bare && fs.existsSync(gameTemplate) ? gameTemplate : path.join(ROOT, '_template');
fs.cpSync(from, target, {
  recursive: true,
  filter: (src) => !/(node_modules|dist|assets\/opt|\.last-sizes\.json)/.test(path.relative(ROOT, src)),
});
const scenario = path.join(target, 'scenario', 'scenario.yaml');
fs.writeFileSync(scenario, fs.readFileSync(scenario, 'utf8').replace(/^(\s*name:\s*).*$/m, `$1${name}`));
console.log(`created ${name}/ from ${path.relative(ROOT, from)} -> drop the storyboard screenshots into ${name}/scenario/refs/`);

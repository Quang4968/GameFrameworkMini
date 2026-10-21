// npm run guid -- <guid | Assets/path> [...]
// Building art-map.yaml means following Unity references: a prefab or ScriptableObject names its sprites by
// guid. A guid prints the asset path; an asset path prints its guid and every scene/prefab/asset/material that
// references it. Read-only.
import fs from 'node:fs';
import path from 'node:path';
import { REPO, fail, parseArgs } from './lib/common.mjs';

const queries = parseArgs()._;
if (!queries.length) fail('usage: npm run guid -- <guid | Assets/path> [...]');

const SKIP = new Set(['Library', 'Temp', 'Logs', 'node_modules']);
const metas = [];
const users = [];
(function walk(dir) {
  for (const e of fs.readdirSync(dir, { withFileTypes: true })) {
    if (SKIP.has(e.name)) continue;
    const p = path.join(dir, e.name);
    if (e.isDirectory()) walk(p);
    else if (e.name.endsWith('.meta')) metas.push(p);
    else if (/\.(prefab|unity|asset|mat|controller|anim)$/.test(e.name)) users.push(p);
  }
})(path.join(REPO, 'Assets'));

const guidOf = (metaFile) => fs.readFileSync(metaFile, 'utf8').match(/^guid: ([0-9a-f]{32})/m)?.[1];
const rel = (p) => path.relative(REPO, p);

for (const q of queries) {
  if (/^[0-9a-f]{32}$/.test(q)) {
    const hit = metas.find((m) => guidOf(m) === q);
    console.log(`${q} -> ${hit ? rel(hit).replace(/\.meta$/, '') : 'not found'}`);
    continue;
  }
  const metaFile = path.join(REPO, `${q}.meta`);
  if (!fs.existsSync(metaFile)) {
    console.log(`${q}: no such asset`);
    continue;
  }
  const guid = guidOf(metaFile);
  const refs = users.filter((u) => fs.readFileSync(u, 'utf8').includes(guid)).map(rel);
  console.log(`${q}\n  guid ${guid}\n  used by ${refs.length ? refs.join('\n          ') : 'nothing (unreferenced)'}`);
}

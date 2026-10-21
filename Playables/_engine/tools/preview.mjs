// npm run preview -- <playable> [--port 5180]
// Serves dist/<network>/index.html with a fake host injected (MRAID, Mintegral callbacks, ExitApi) and an
// on-screen log, so the built file can be clicked through like on the network. The injection is served only,
// never written to dist. Key P toggles viewable/pause (MRAID) to test pause and resume.
import fs from 'node:fs';
import http from 'node:http';
import path from 'node:path';
import { parseArgs, playableDir, readNetworks } from './lib/common.mjs';

const args = parseArgs();
const dir = playableDir(args._[0]);
const port = Number(args.port ?? 5180);
const networks = Object.keys(readNetworks().networks);

// Driving a build for QA, in design coordinates (the 1080x1920 space the game lays out in):
//   await __qa.live()                       keep frames coming when the browser pane is hidden (no rAF)
//   __qa.speed(0.3)                         slow motion; 0.0001 freezes for a screenshot, 1 resumes
//   await __qa.tap(x, y) / __qa.doubleTap(x, y)
//   await __qa.drag([[x, y], [x, y], ...], msPerSegment)
//   __qa.at('cell', row, col)               a point game code registered with qaHook (see _engine/core/qa.ts)
//   await __qa.wait(ms)
const QA = `<script>(function(){
function app(){return window.__PIXI_APP__;}
function wait(ms){return new Promise(function(r){setTimeout(r,ms);});}
function client(x,y){var a=app(),root=a.stage.children[0],r=a.canvas.getBoundingClientRect(),p=root.toGlobal({x:x,y:y});return [r.left+p.x*r.width/a.screen.width,r.top+p.y*r.height/a.screen.height];}
function ev(t,x,y){var c=client(x,y);app().canvas.dispatchEvent(new PointerEvent(t,{clientX:c[0],clientY:c[1],bubbles:true,pointerId:1,pointerType:'touch',isPrimary:true,button:0,buttons:t==='pointerup'?0:1}));}
window.__qa={
 live:async function(){var a=app(),t=a.ticker.lastTime;await wait(300);if(a.ticker.lastTime===t&&!window.__qaTick)window.__qaTick=setInterval(function(){a.ticker.update(performance.now());},16);return window.__qaTick?'ticking by hand (pane hidden)':'rAF running';},
 speed:function(k){app().ticker.speed=k;},
 at:function(name){var h=(window.__QA_HOOKS__||{})[name];if(!h)throw new Error('no qaHook '+name);var p=h.apply(null,[].slice.call(arguments,1));return [p.x,p.y];},
 tap:async function(x,y){ev('pointerdown',x,y);await wait(40);ev('pointerup',x,y);},
 doubleTap:async function(x,y){await this.tap(x,y);await wait(90);await this.tap(x,y);},
 drag:async function(points,ms){ms=ms||120;ev('pointerdown',points[0][0],points[0][1]);for(var i=1;i<points.length;i++){var a=points[i-1],b=points[i];for(var s=1;s<=6;s++){await wait(ms/6);ev('pointermove',a[0]+(b[0]-a[0])*s/6,a[1]+(b[1]-a[1])*s/6);}}var l=points[points.length-1];ev('pointerup',l[0],l[1]);},
 wait:wait
};
})();</script>`;

const MOCK = `<script>(function(){
var log=document.createElement('div');log.style.cssText='position:fixed;left:0;right:0;bottom:0;max-height:30%;overflow:hidden;font:12px monospace;color:#0f0;background:rgba(0,0,0,.6);pointer-events:none;z-index:9;padding:4px';
function say(m){console.info('[host]',m);(document.body||document.documentElement).appendChild(log);log.textContent=m+'\\n'+log.textContent;}
var listeners={},state='loading',viewable=true;
function emit(e,v){(listeners[e]||[]).forEach(function(cb){cb(v)});}
window.mraid={getState:function(){return state},isViewable:function(){return viewable},
 addEventListener:function(e,cb){(listeners[e]=listeners[e]||[]).push(cb)},
 removeEventListener:function(e,cb){listeners[e]=(listeners[e]||[]).filter(function(x){return x!==cb})},
 open:function(u){say('mraid.open '+u)},getVersion:function(){return '3.0'}};
setTimeout(function(){state='default';say('mraid ready');emit('ready');},300);
document.addEventListener('keydown',function(k){if(k.key==='p'){viewable=!viewable;say('viewableChange '+viewable);emit('viewableChange',viewable);}});
window.gameReady=function(){say('gameReady');setTimeout(function(){say('host gameStart');window.gameStart&&window.gameStart();},300)};
window.gameEnd=function(){say('gameEnd')};
window.install=function(){say('install')};
window.ExitApi={exit:function(){say('ExitApi.exit')}};
})();</script>${QA}`;

function index() {
  const links = networks
    .filter((n) => fs.existsSync(path.join(dir, 'dist', n, 'index.html')))
    .map((n) => `<li><a href="/${n}/">${n}</a></li>`)
    .join('');
  return `<!DOCTYPE html><meta charset="utf-8"><title>Preview</title><body style="font:16px sans-serif;padding:16px">
<h3>${path.basename(dir)}</h3><ul>${links || '<li>no build: npm run build -- ' + path.basename(dir) + '</li>'}</ul>
<p>Open a network at phone size (browser devtools or the in-app browser's mobile viewport). Key P toggles pause.</p>`;
}

http
  .createServer((req, res) => {
    const network = (req.url ?? '/').split('/').filter(Boolean)[0];
    const file = network && path.join(dir, 'dist', network, 'index.html');
    if (!file || !fs.existsSync(file)) {
      res.writeHead(network ? 404 : 200, { 'Content-Type': 'text/html; charset=utf-8' });
      return res.end(index());
    }
    const page = fs
      .readFileSync(file, 'utf8')
      .replace(/<script[^>]*exitapi\.js[^>]*><\/script>/, '')
      .replace('<head>', `<head>${MOCK}`);
    res.writeHead(200, { 'Content-Type': 'text/html; charset=utf-8', 'Cache-Control': 'no-store' });
    res.end(page);
  })
  .listen(port, () => console.log(`preview http://localhost:${port}/ (networks: ${networks.join(', ')})`));

// Named points a QA script can aim at in the preview (`__qa.at('cell', 0, 1)` -> the design-space centre of a
// cell), registered by game code. Costs nothing on the networks: the preview host is what calls them.
type Hook = (...args: number[]) => { x: number; y: number };

export function qaHook(name: string, fn: Hook): void {
  const g = globalThis as { __QA_HOOKS__?: Record<string, Hook> };
  (g.__QA_HOOKS__ ??= {})[name] = fn;
}

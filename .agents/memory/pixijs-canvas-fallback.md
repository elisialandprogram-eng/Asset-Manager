---
name: PixiJS v8 Canvas2D fallback
description: How to make PixiJS v8 work in Replit where the GPU is fully disabled.
---

## Rule
Always init PixiJS v8 with `preference: 'webgl'` inside a try/catch that retries with `preference: 'canvas'`.

## Why
Replit's sandbox disables the GPU at OS level (`GL_VENDOR=Disabled`). PixiJS v8 does NOT automatically fall back to Canvas2D if `preference: 'webgl'` is set and WebGL context creation fails — it throws. The explicit retry is required.

## How to apply
```typescript
try {
  await app.init({ ...base, preference: 'webgl' });
} catch {
  await app.init({ ...base, preference: 'canvas' } as Parameters<Application['init']>[0]);
}
```
The type cast is needed because TS overloads differ between webgl/canvas preference shapes.

## Observed behaviour in Replit
- `CanvasRenderer-*.js` chunk is loaded (not WebGLRenderer)
- `browserAll-*.js` and `webworkerAll-*.js` also load
- No console errors when the fallback path is used
- WebGL errors appear when fallback is missing

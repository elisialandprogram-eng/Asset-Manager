---
name: R3F Black Canvas Fix
description: True root cause of black canvas in Replit preview + definitive fix pattern. Critical: R3F canvases will always be black in Replit's sandboxed preview iframe due to GPU lockout.
---

## The True Root Cause (Confirmed from Browser Console)

```
GL_VENDOR = Disabled, GL_RENDERER = Disabled, Sandboxed = yes
BindToCurrentSequence failed
THREE.WebGLRenderer: Error creating WebGL context.
```

Replit's preview iframe runs in a fully sandboxed container where the GPU/graphics driver is disabled at the OS level. `canvas.getContext('webgl2')` returns null. When Three.js (via R3F) attempts to construct a WebGLRenderer, it throws `Error: Error creating WebGL context.` as an **unhandled Promise rejection**.

**Why React Error Boundaries don't catch it:**
React Error Boundaries only catch synchronous exceptions thrown during render, in lifecycle methods, and constructors. They do NOT catch:
- Unhandled Promise rejections
- Errors in async code / event handlers
- Errors thrown inside Three.js WebGLRenderer constructor (which runs in an internal async path in R3F v9)

Result: `ThreeErrorBoundary` wraps the Canvas but never fires. The canvas stays black. No visible error UI.

## The Definitive Fix Pattern

```typescript
// 1. Synchronous WebGL probe — cached at module level so it's called once per page load
let _cached: boolean | null = null;
export function detectWebGL(): boolean {
  if (_cached !== null) return _cached;
  try {
    const canvas = document.createElement('canvas');
    const ctx = canvas.getContext('webgl2') ?? canvas.getContext('webgl');
    if (!ctx) { _cached = false; return false; }
    const renderer = ctx.getParameter(ctx.RENDERER) as string | null;
    if (!renderer || renderer.toLowerCase().includes('disabled')) {
      _cached = false; return false;
    }
    _cached = true; return true;
  } catch { _cached = false; return false; }
}

// 2. Gate every R3F Canvas behind the probe
export function KingdomScene3D(props) {
  if (!detectWebGL()) return <KingdomScene2D {...props} />;   // 2D fallback
  return <ThreeErrorBoundary><Canvas>...</Canvas></ThreeErrorBoundary>;
}
```

## 2D Fallback Components
- `KingdomScene2D.tsx` — Palace hero card + building tile grid, fully interactive, HTML/CSS only
- `WorldMap2D.tsx` — HTML5 2D Canvas terrain (using noiseUtils) + SVG markers for kingdoms/spawns/crystals

## Window Error Listeners (for diagnosis)
Added to `main.tsx`:
```javascript
window.addEventListener('error', e => reportClientError({ type: 'error', message: e.message, ... }))
window.addEventListener('unhandledrejection', e => reportClientError({ type: 'unhandledrejection', ... }))
function reportClientError(payload) { navigator.sendBeacon('/api/client-error', ...) }
```
Server endpoint: `POST /api/client-error` (routes/clientErrors.ts) — logs to pino.

## Why: How to Apply
Call `detectWebGL()` BEFORE mounting any R3F Canvas. The probe itself is safe — `canvas.getContext('webgl2')` returns null silently in the sandboxed env without throwing the WebGL context error. Only Three.js WebGLRenderer instantiation triggers the error.

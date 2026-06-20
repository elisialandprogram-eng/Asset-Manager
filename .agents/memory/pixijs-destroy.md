---
name: PixiJS v8 destroy error fix
description: How to safely destroy a PixiJS v8 Application, especially when init may have been interrupted.
---

## Rule
Null the app reference before calling `app.destroy()`, and wrap the destroy call in a try/catch.

## Why
`app.destroy()` throws `"this._cancelResize is not a function"` if the Application was never fully initialized (e.g. React StrictMode double-invoke runs the cleanup before the second mount's async init completes, or the destroy is called very shortly after init starts).

## How to apply
```typescript
destroy(destroyCanvas = false): void {
  if (!this.app) return;
  const appRef = this.app;
  this.app = null; // null first — re-entrant calls become no-ops
  try {
    appRef.destroy(destroyCanvas, { children: true });
  } catch {
    // Swallow internal PixiJS resize-observer / GPU teardown errors
  }
}
```

## Symptom
Browser console shows `TypeError: this._cancelResize is not a function` posted to `/api/client-error`. Scene still renders correctly — this is purely a cleanup-path bug.

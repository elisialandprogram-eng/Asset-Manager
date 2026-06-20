---
name: PixiJS v8 Text style gotchas
description: Known pitfalls with the PixiJS v8 Text constructor and TextStyle options.
---

## Rule: alpha is NOT a TextStyle property
`alpha` is a display-object property, not a style option. Setting it inside the style object causes TS2769 ("does not exist in type TextStyle | TextStyleOptions").

**Wrong:**
```typescript
new Text({ text: '+', style: { fill: '#9a7848', fontSize: 18, alpha: 0.5 } });
```
**Correct:**
```typescript
const txt = new Text({ text: '+', style: { fill: '#9a7848', fontSize: 18 } });
txt.alpha = 0.5;
```

## Rule: v8 Text constructor is options-object form
PixiJS v8 changed the constructor:
- v7: `new Text(text, style)`  
- v8: `new Text({ text, style })` — the `CanvasTextOptions` overload

Both overloads technically exist in v8, but the options-object form is preferred and TypeScript resolves it first.

## Rule: stroke in TextStyle is an object
```typescript
// Correct v8 style:
stroke: { color: '#000000', width: 3 }
```
Not `strokeThickness` (v7).

## Rule: dropShadow is an object in v8
```typescript
dropShadow: { color: 0x000000, blur: 4, distance: 2, angle: Math.PI / 4, alpha: 0.8 }
```
Avoid it if possible — use `stroke` instead for simpler outlines.

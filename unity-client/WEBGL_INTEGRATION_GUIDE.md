# Eternal Kingdoms — WebGL Integration Guide

> Covers the complete process for building, embedding, and communicating
> between the Unity WebGL client and the React portal.

---

## 1. Unity WebGL Build Settings

### Player Settings (Project Settings → Player → WebGL)

| Setting | Development | Staging | Production |
|---------|-------------|---------|------------|
| Compression Format | Disabled | Gzip | Brotli |
| Exception Support | Full With Stacktrace | Explicitly Thrown | None |
| Memory Size | 256 MB | 512 MB | 512 MB |
| Strip Engine Code | No | No | Yes |
| Publishing Settings → Decompression Fallback | No | Yes | Yes |

### Build Define Symbols

| Environment | Define |
|-------------|--------|
| Development | `UNITY_ENV_DEV` |
| Staging | `UNITY_ENV_STAGING` |
| Production | `UNITY_ENV_PRODUCTION` |

Set under Project Settings → Player → Other Settings → Script Define Symbols.

### Template

Use a **Minimal** WebGL template. The portal provides its own layout — the Unity build should expose only `canvas` with no Unity splash branding.

Custom template path: `Assets/WebGLTemplates/EKMinimal/index.html`

```html
<!DOCTYPE html>
<html>
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  <title>Eternal Kingdoms</title>
  <style>
    * { margin: 0; padding: 0; box-sizing: border-box; }
    body { background: #000; overflow: hidden; }
    #unity-canvas { width: 100vw; height: 100vh; display: block; }
  </style>
</head>
<body>
  <canvas id="unity-canvas"></canvas>
  <script src="Build/{{{ LOADER_FILENAME }}}"></script>
  <script>
    var buildUrl = "Build";
    var config = {
      dataUrl: buildUrl + "/{{{ DATA_FILENAME }}}",
      frameworkUrl: buildUrl + "/{{{ FRAMEWORK_FILENAME }}}",
      codeUrl: buildUrl + "/{{{ CODE_FILENAME }}}",
      streamingAssetsUrl: "StreamingAssets",
      companyName: "EternalKingdoms",
      productName: "Eternal Kingdoms",
      productVersion: "0.1.0",
    };
    var canvas = document.querySelector("#unity-canvas");
    createUnityInstance(canvas, config).then(function(instance) {
      window.unityInstance = instance;
      // Notify portal that Unity is ready
      window.parent.postMessage({ type: "ek_unity_ready" }, "*");
    });
  </script>
</body>
</html>
```

---

## 2. JWT Handoff Process

When the React portal launches Unity (via iframe), it must pass the JWT so Unity starts authenticated.

### Method A — URL Fragment (recommended for same-origin)

```
/unity/?token=<jwt>
```

In the Unity WebGL template, read the fragment on load:

```javascript
// In index.html before createUnityInstance
var token = window.location.hash.replace('#token=', '');
if (token) {
  sessionStorage.setItem('ek_pending_token', token);
}
```

Unity's `AuthManager.Initialize()` reads `sessionStorage` via JSLib:

```csharp
// Assets/Plugins/WebGL/AuthBridge.jslib
mergeInto(LibraryManager.library, {
  GetPendingToken: function() {
    var token = sessionStorage.getItem('ek_pending_token') || '';
    sessionStorage.removeItem('ek_pending_token');
    var bufferSize = lengthBytesUTF8(token) + 1;
    var buffer = _malloc(bufferSize);
    stringToUTF8(token, buffer, bufferSize);
    return buffer;
  }
});
```

```csharp
// In AuthManager.cs (WebGL-conditional path)
#if UNITY_WEBGL && !UNITY_EDITOR
[DllImport("__Internal")]
private static extern string GetPendingToken();
#endif
```

### Method B — postMessage (cross-origin iframe)

Portal sends token after iframe load:

```javascript
// React portal
iframeRef.current.contentWindow.postMessage({
  type: 'ek_auth_token',
  token: localStorage.getItem('ek_token')
}, '*');
```

Unity listens in the WebGL template:

```javascript
window.addEventListener('message', function(e) {
  if (e.data.type === 'ek_auth_token' && window.unityInstance) {
    window.unityInstance.SendMessage('AuthManager', 'ReceiveTokenFromPortal', e.data.token);
  }
});
```

Unity C# method `ReceiveTokenFromPortal(string token)` is added to `AuthManager`:

```csharp
public void ReceiveTokenFromPortal(string token)
{
    if (!string.IsNullOrEmpty(token))
    {
        StartCoroutine(ValidateAndApplyToken(token));
    }
}
```

---

## 3. Iframe Embedding in the React Portal

The portal embeds Unity on `/kingdom` and `/world` routes.

```tsx
// artifacts/eternal-kingdoms/src/pages/KingdomPage.tsx
const token = localStorage.getItem('ek_token');
const unityUrl = `${UNITY_BASE_URL}#token=${encodeURIComponent(token)}`;

return (
  <iframe
    src={unityUrl}
    style={{ width: '100%', height: '100%', border: 'none' }}
    allow="fullscreen"
    title="Eternal Kingdoms"
  />
);
```

### Environment URLs

| Environment | UNITY_BASE_URL |
|-------------|----------------|
| Development | `http://localhost:8090/unity/` |
| Production | `https://play.eternalkingdoms.com/unity/` |

---

## 4. Fullscreen Launch

Unity can request fullscreen from within a WebGL build:

```csharp
// C# — request fullscreen
#if UNITY_WEBGL && !UNITY_EDITOR
[DllImport("__Internal")]
private static extern void RequestFullscreen();
#endif
```

```javascript
// JSLib
mergeInto(LibraryManager.library, {
  RequestFullscreen: function() {
    var canvas = document.getElementById('unity-canvas');
    if (canvas.requestFullscreen) canvas.requestFullscreen();
  }
});
```

The portal also provides a fullscreen button that posts a message to the iframe.

---

## 5. Build Commands

### Development WebGL Build

```bash
# From Unity Editor menu:
# File → Build Settings → WebGL → Switch Platform → Build

# Or via CLI (Unity installed at /Applications/Unity/Hub/Editor/6.x.x):
/path/to/Unity \
  -batchmode \
  -quit \
  -projectPath ./unity-client \
  -buildTarget WebGL \
  -executeMethod BuildPipeline.BuildWebGL \
  -logFile build_dev.log
```

### CI/CD Build Script

```bash
#!/bin/bash
# build_webgl.sh — called by GitHub Actions / Jenkins

UNITY_PATH="/opt/unity/Editor/Unity"
PROJECT_PATH="$(pwd)/unity-client"
BUILD_PATH="$(pwd)/dist/webgl"
LOG_PATH="$(pwd)/build_webgl.log"

echo "Building WebGL..."
$UNITY_PATH \
  -batchmode -quit \
  -projectPath "$PROJECT_PATH" \
  -buildTarget WebGL \
  -customBuildPath "$BUILD_PATH" \
  -customBuildName "EternalKingdoms" \
  -logFile "$LOG_PATH"

if [ $? -ne 0 ]; then
  echo "Build FAILED. See $LOG_PATH"
  cat "$LOG_PATH"
  exit 1
fi

echo "Build complete: $BUILD_PATH"
```

---

## 6. Deployment

| Platform | Host | Path |
|----------|------|------|
| WebGL | CDN / static hosting (Vercel, Cloudflare Pages, S3+CloudFront) | `play.eternalkingdoms.com/unity/` |
| Android | Google Play | Internal test track |
| iOS | App Store | TestFlight |

### WebGL Hosting Requirements

- Must serve `.br` files with `Content-Encoding: br` header (Brotli)
- Must serve `.data` with `application/octet-stream`
- Must serve `.js` with `application/javascript`
- CORS: allow `eternalkingdoms.com` origin for the portal iframe

---

## 7. Cross-Platform Auth Token Storage

| Platform | Storage | Key |
|----------|---------|-----|
| WebGL | `sessionStorage` (from URL fragment) + `PlayerPrefs` | `ek_token` |
| Android | `PlayerPrefs` (encrypted in release) | `ek_token` |
| iOS | `PlayerPrefs` (Keychain in release) | `ek_token` |

---

## 8. Portal ↔ Unity Message Protocol

All messages use `window.postMessage` / `SendMessage` pattern.

| Direction | Type | Payload | Handler |
|-----------|------|---------|---------|
| Portal → Unity | `ek_auth_token` | `{ token: string }` | `AuthManager.ReceiveTokenFromPortal` |
| Portal → Unity | `ek_navigate` | `{ scene: string, kingdomId?: string }` | `SceneController.HandlePortalNavigate` |
| Unity → Portal | `ek_unity_ready` | `{}` | Portal shows Unity iframe |
| Unity → Portal | `ek_logout` | `{}` | Portal clears JWT, navigates to login |
| Unity → Portal | `ek_open_dashboard` | `{}` | Portal navigates to `/dashboard` |

---

## 9. WebGL Performance Targets

| Metric | Target |
|--------|--------|
| Initial load time | < 15 seconds (Brotli, CDN) |
| Memory footprint | < 400 MB |
| Kingdom scene FPS | 60 FPS desktop, 30 FPS mobile |
| World scene FPS | 45+ FPS desktop |
| Initial API call time | < 2 seconds on login |

---

## 10. Known WebGL Constraints

| Constraint | Mitigation |
|------------|-----------|
| No threading | Unity WebGL is single-threaded. Avoid `async/await` patterns that assume threads. Use coroutines. |
| No native sockets | Socket.IO via WebSocket API is supported. Use `WebSocketClient.cs` bridge. |
| No file system | PlayerPrefs only. No `File.WriteAllText`. |
| Brotli requires HTTPS | Always deploy to HTTPS for production Brotli builds. |
| iOS Safari memory limit | WebGL memory set to 512 MB — stays under Safari 1 GB limit. |

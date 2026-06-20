# Unity Client — Build Pipeline

## Target Platforms

| Platform | Status | Notes |
|----------|--------|-------|
| WebGL | Primary | Embedded in portal via iframe or direct URL |
| Android | Planned | Google Play distribution |
| iOS | Planned | App Store distribution |

## WebGL Build

### Unity Settings

```
Build Target:         WebGL
Template:             Minimal (custom template with portal CSS)
Compression Format:   Brotli (production), Disabled (development)
Memory Size:          512 MB (configurable per device capability)
Exception Support:    None (production), Full With Stacktrace (development)
```

### Embedding in Portal

The React portal will embed the WebGL build via an `<iframe>` on the `/kingdom` and `/world` routes once builds are available. The iframe src points to the hosted WebGL build URL.

```html
<iframe
  src="https://play.eternalkingdoms.com/unity/"
  style="width:100%;height:100%;border:none"
  allow="autoplay; fullscreen"
/>
```

### Auth Token Handoff

The portal passes the JWT to Unity via `postMessage` on iframe load:

```javascript
// Portal (React)
unityIframe.contentWindow.postMessage({ type: 'ek_auth', token: localStorage.getItem('ek_token') }, '*');

// Unity (JSLib plugin)
window.addEventListener('message', (e) => {
  if (e.data.type === 'ek_auth') SendMessage('AuthManager', 'ReceiveToken', e.data.token);
});
```

## Android Build

```
Target SDK:    Android 8.0+ (API 26)
Scripting Backend: IL2CPP
Architecture:  ARM64
Build System:  Gradle
```

## iOS Build

```
Target OS:     iOS 14+
Scripting Backend: IL2CPP
Architecture:  ARM64
Xcode Version: Latest stable
```

## CI/CD Pipeline (Planned)

| Step | Tool | Trigger |
|------|------|---------|
| Build WebGL | Unity Cloud Build / GitHub Actions | Push to `unity-client/main` |
| Upload to CDN | AWS S3 + CloudFront | On successful build |
| Android APK | Unity Cloud Build | Tag `android-*` |
| iOS IPA | Unity Cloud Build (macOS) | Tag `ios-*` |
| Version bump | Semantic versioning | PR merge to main |

## Version Convention

```
{major}.{minor}.{patch}-{platform}

Examples:
  1.0.0-webgl
  1.0.0-android
  1.0.0-ios
```

Major version increments require a backend API version review.

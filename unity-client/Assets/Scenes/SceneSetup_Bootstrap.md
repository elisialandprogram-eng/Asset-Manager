# Bootstrap.unity — Scene Setup Guide

## Purpose
Entry point for the entire application. Runs manager initialization and routes to Login or World based on stored JWT.

## Hierarchy

```
Bootstrap (Scene root)
├── Bootstrap Managers (GameObject — DontDestroyOnLoad host)
│   ├── BootstrapManager              ← scene entry point
│   ├── GameManager
│   ├── ConfigManager
│   ├── SaveManager
│   ├── AddressablesManager
│   ├── SceneController
│   ├── CoroutineRunner
│   ├── Networking/
│   │   └── NetworkManager
│   ├── Authentication/
│   │   └── AuthManager
│   ├── UI/
│   │   ├── UIManager
│   │   ├── PopupManager
│   │   └── NotificationManager
│   └── Managers/
│       ├── AudioManager
│       └── SettingsManager
│
├── UI Canvas (Bootstrap)
│   ├── LoadingPanel                  ← shown during initialization
│   │   ├── Logo Image
│   │   ├── LoadingSpinner
│   │   └── StatusLabel (TMP)
│   └── ErrorPanel                   ← shown if initialization fails
│       ├── ErrorLabel (TMP)
│       └── RetryButton
│
└── Main Camera                       ← minimal camera, not used for gameplay
```

## BootstrapManager Configuration
- `initTimeoutSeconds` = 30

## ConfigManager Configuration
Assign ScriptableObject assets:
- `developmentConfig` → Resources/Environments/Development.asset
- `stagingConfig` → Resources/Environments/Staging.asset
- `productionConfig` → Resources/Environments/Production.asset

## Important
This is the ONLY scene where manager GameObjects are created.
All other scenes rely on DontDestroyOnLoad persistence.
Never add managers to non-Bootstrap scenes.

## Build Settings
Bootstrap must be scene index 0 in Build Settings.
Scene order: Bootstrap(0), Login(1), Loading(2), World(3), Kingdom(4)

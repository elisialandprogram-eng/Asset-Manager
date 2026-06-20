# Login.unity — Scene Setup Guide

## Purpose
Login and register flow. Authenticates the player with the backend and stores JWT.

## Hierarchy

```
Login (Scene root)
├── Main Camera (orthographic, pointing at UI)
│
├── Canvas_Login (Screen Space — Overlay)
│   ├── Background Image              ← full-screen game art / atmospheric bg
│   ├── Logo Image                    ← Eternal Kingdoms logo
│   │
│   ├── LoginPanel (GameObject)       ← LoginController attached here
│   │   ├── TitleLabel ("Welcome Back", TMP)
│   │   ├── EmailField (TMP_InputField)
│   │   │   └── Placeholder: "Email address"
│   │   ├── PasswordField (TMP_InputField)
│   │   │   ├── Placeholder: "Password"
│   │   │   └── ContentType: Password
│   │   ├── RememberMeRow
│   │   │   ├── RememberMeToggle
│   │   │   └── RememberMeLabel ("Remember me")
│   │   ├── LoginButton               ← OnClick → LoginController.OnLoginClicked()
│   │   │   └── Label: "Enter the Kingdom"
│   │   ├── ErrorLabel (TMP)          ← hidden by default
│   │   └── LoadingOverlay (GameObject)
│   │       └── LoadingSpinner
│   │
│   └── VersionLabel (TMP)            ← "Alpha 0.1" bottom-right
│
└── EventSystem
```

## LoginController Inspector Bindings
| Field | Target |
|-------|--------|
| emailField | EmailField TMP_InputField |
| passwordField | PasswordField TMP_InputField |
| rememberMeToggle | RememberMeToggle Toggle |
| loginButton | LoginButton Button |
| errorLabel | ErrorLabel TMP |
| loadingOverlay | LoadingOverlay GameObject |

## Dev Account (pre-filled in dev builds)
- Email: dev@eternalkingdoms.com
- Password: Rcbk123@#

## On Success
LoginController calls SceneController.GoToWorld() which loads World.unity.

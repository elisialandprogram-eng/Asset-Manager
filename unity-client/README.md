# Eternal Kingdoms — Unity Client

This directory is the root of the Eternal Kingdoms Unity 6 LTS game client.

## Overview

The Unity client is the primary game experience for Eternal Kingdoms. It delivers the full isometric 3D world map, interactive kingdom gameplay, real-time combat, and all player-facing game systems.

The React web application (`artifacts/eternal-kingdoms/`) serves as the **account and portal frontend** only — handling login, registration, dashboard statistics, and kingdom management. All visual gameplay runs here in Unity.

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Engine | Unity 6 LTS |
| Render Pipeline | Universal Render Pipeline (URP) |
| Target Platforms | WebGL, Android, iOS |
| Backend API | Express 5 (existing `artifacts/api-server/`) |
| Database | Supabase PostgreSQL |
| Realtime | WebSockets / Socket.IO |
| Auth | JWT (Bearer token — same as portal) |

## Documentation Index

| File | Contents |
|------|----------|
| `UNITY_ARCHITECTURE.md` | Overall Unity project architecture and design principles |
| `UNITY_NETWORKING.md` | API communication, WebSocket realtime, auth token flow |
| `UNITY_WORLD_SYSTEM.md` | World map — grid spec, chunk streaming, camera, entities |
| `UNITY_KINGDOM_SYSTEM.md` | Kingdom layout — fixed building node system, rings |
| `UNITY_ART_PIPELINE.md` | Art style, asset naming, URP materials, addressables |
| `UNITY_BUILD_PIPELINE.md` | Build targets, CI/CD, WebGL embed, mobile distribution |
| `UNITY_API_CONTRACT.md` | Full backend API audit — endpoints, schemas, auth, gaps |

## Getting Started (Unity Developer)

1. Install **Unity 6 LTS** with URP package and WebGL Build Support.
2. Open this directory as a Unity project.
3. Configure `Assets/Scripts/Core/Config/ApiConfig.cs` with your backend URL.
4. Ensure `SESSION_SECRET` and `DATABASE_URL` are set in the backend `.env`.
5. Press Play — the client will authenticate via the portal JWT token.

## Dev Account

Use the existing dev account provisioned by the backend seeder:

```
Email:    dev@eternalkingdoms.com
Password: Rcbk123@#
```

This account has admin role and pre-seeded resources.

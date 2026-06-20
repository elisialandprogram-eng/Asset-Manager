# Unity Client — Networking

## HTTP API Client

All backend communication uses `UnityWebRequest` wrapped in `ApiClient.cs`.

### Auth Token Flow

1. On login, `POST /api/auth/login` returns `{ token, user }`.
2. `AuthManager` stores the JWT in `PlayerPrefs` under key `ek_token`.
3. Every subsequent API request includes `Authorization: Bearer <token>` header.
4. On `401` response, `AuthManager` clears the token and returns to Login scene.

### Request Pattern

```csharp
// GET example
var result = await ApiClient.Get<KingdomStateDto>("/api/kingdoms/{id}/state");

// POST example
var result = await ApiClient.Post<ConstructResponseDto>(
    "/api/kingdoms/{id}/construct",
    new ConstructRequestDto { buildingType = "farm" }
);
```

### Polling Strategy

| Data | Poll Interval | Trigger |
|------|--------------|---------|
| Kingdom state (resources, buildings) | 8s | `InvokeRepeating` in `KingdomManager` |
| Upgrade queue | 8s | `InvokeRepeating` in `KingdomManager` |
| World map entities | 60s | `InvokeRepeating` in `WorldManager` |
| Monster spawns | 30s | `InvokeRepeating` in `WorldManager` |

## WebSocket (Socket.IO) — Phase 8

Realtime events will use Socket.IO. The backend Socket.IO server listens on the same Express port.

### Planned Event Channels

| Event | Direction | Payload |
|-------|-----------|---------|
| `resource_tick` | Server → Client | Updated resources for kingdom |
| `upgrade_complete` | Server → Client | Building ID + new level |
| `construction_complete` | Server → Client | Building ID + type |
| `attack_incoming` | Server → Client | Attacker kingdom ID + ETA |
| `battle_result` | Server → Client | Battle report summary |
| `alliance_message` | Server → Client | Chat message |

### Connection Management

```csharp
// WebSocketClient.cs
webSocket.On("resource_tick", (data) => {
    ResourceManager.Instance.ApplyTick(data);
});
```

Auth: send JWT in the Socket.IO handshake `auth` object:
```javascript
{ token: "Bearer <jwt>" }
```

## Error Handling

| HTTP Status | Action |
|-------------|--------|
| 400 | Show validation error toast |
| 401 | Clear token, go to Login |
| 403 | Show "Access denied" toast |
| 404 | Show "Not found" toast |
| 429 | Backoff + retry after `Retry-After` header |
| 500 | Show "Server error" toast, log to Sentry |

## Backend URL Configuration

Configure `Assets/Scripts/Core/Config/ApiConfig.cs`:

```csharp
public static class ApiConfig
{
    public const string BaseUrl = "https://api.eternalkingdoms.com";  // production
    // public const string BaseUrl = "http://localhost:5000";         // local dev
    public const int TimeoutSeconds = 15;
    public const int MaxRetries = 3;
}
```

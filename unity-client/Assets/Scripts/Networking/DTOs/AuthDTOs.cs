using System;

namespace EternalKingdoms.Networking.DTOs
{
    // ── POST /api/auth/login ──────────────────────────────────────────────────

    [Serializable]
    public class LoginRequestDto
    {
        public string email;
        public string password;
    }

    [Serializable]
    public class LoginResponseDto
    {
        public string token;
        public UserDto user;
    }

    // ── GET /api/auth/me ──────────────────────────────────────────────────────

    [Serializable]
    public class MeResponseDto
    {
        public UserDto user;
    }

    // ── Shared ────────────────────────────────────────────────────────────────

    [Serializable]
    public class UserDto
    {
        public string id;
        public string username;
        public string email;
        public string role;       // "player" | "moderator" | "admin"
        public string worldId;
        public string createdAt;
        public string lastLoginAt;
    }
}

using UnityEngine;
using System;
using System.Collections;
using EternalKingdoms.Networking.DTOs;

namespace EternalKingdoms.Networking
{
    /// <summary>
    /// Handles all authentication API calls.
    /// POST /api/auth/login
    /// POST /api/auth/register (future)
    /// GET  /api/auth/me
    /// </summary>
    public class AuthService
    {
        private readonly ApiClient _api;
        public AuthService(ApiClient api) => _api = api;

        public IEnumerator Login(
            string email,
            string password,
            Action<LoginResponseDto> onSuccess,
            Action<ApiError> onError)
        {
            var body = new LoginRequestDto { email = email, password = password };
            yield return _api.Post<LoginResponseDto>("/api/auth/login", body, onSuccess, onError);
        }

        public IEnumerator GetMe(
            Action<MeResponseDto> onSuccess,
            Action<ApiError> onError)
        {
            yield return _api.Get<MeResponseDto>("/api/auth/me", onSuccess, onError, requireAuth: true);
        }
    }
}

using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Text;

namespace EternalKingdoms.Networking
{
    /// <summary>
    /// Central HTTP wrapper for all backend API calls.
    ///
    /// Features:
    /// - JWT Bearer token injection (auto-sourced from AuthManager)
    /// - Configurable timeout and retry policy (exponential backoff)
    /// - Request/response logging (debug builds only)
    /// - Centralised 401 handling → triggers AuthManager.HandleUnauthorized()
    /// - Environment-agnostic base URL (set by ConfigManager)
    ///
    /// Usage:
    ///   yield return ApiClient.Get&lt;MyDto&gt;("/api/kingdoms/mine", onSuccess, onError);
    ///   yield return ApiClient.Post&lt;ResponseDto&gt;("/api/..", body, onSuccess, onError);
    /// </summary>
    public class ApiClient
    {
        private readonly string _baseUrl;
        private readonly float _timeoutSeconds;
        private readonly int _maxRetries;

        // Injected externally by AuthManager after login
        private string _bearerToken;

        public ApiClient(string baseUrl, float timeoutSeconds = 15f, int maxRetries = 3)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _timeoutSeconds = timeoutSeconds;
            _maxRetries = maxRetries;
        }

        public void SetBearerToken(string token) => _bearerToken = token;
        public void ClearBearerToken() => _bearerToken = null;

        // ── GET ───────────────────────────────────────────────────────────────

        public IEnumerator Get<T>(
            string path,
            Action<T> onSuccess = null,
            Action<ApiError> onError = null,
            bool requireAuth = false)
        {
            yield return SendRequest<T>(UnityWebRequest.kHttpVerbGET, path, null, onSuccess, onError, requireAuth);
        }

        // ── POST ──────────────────────────────────────────────────────────────

        public IEnumerator Post<T>(
            string path,
            object body,
            Action<T> onSuccess = null,
            Action<ApiError> onError = null,
            bool requireAuth = false)
        {
            string json = body != null ? JsonUtility.ToJson(body) : "{}";
            yield return SendRequest<T>(UnityWebRequest.kHttpVerbPOST, path, json, onSuccess, onError, requireAuth);
        }

        // ── PUT ───────────────────────────────────────────────────────────────

        public IEnumerator Put<T>(
            string path,
            object body,
            Action<T> onSuccess = null,
            Action<ApiError> onError = null,
            bool requireAuth = false)
        {
            string json = body != null ? JsonUtility.ToJson(body) : "{}";
            yield return SendRequest<T>(UnityWebRequest.kHttpVerbPUT, path, json, onSuccess, onError, requireAuth);
        }

        // ── Core Request Handler ──────────────────────────────────────────────

        private IEnumerator SendRequest<T>(
            string method,
            string path,
            string jsonBody,
            Action<T> onSuccess,
            Action<ApiError> onError,
            bool requireAuth)
        {
            string url = _baseUrl + path;
            int attempt = 0;

            while (attempt <= _maxRetries)
            {
                attempt++;
                using var request = BuildRequest(method, url, jsonBody);

                LogRequest(method, url, attempt);

                yield return request.SendWebRequest();

                // Handle network error (no server response) — retry
                if (request.result == UnityWebRequest.Result.ConnectionError ||
                    request.result == UnityWebRequest.Result.DataProcessingError)
                {
                    Debug.LogWarning($"[ApiClient] Network error on attempt {attempt}/{_maxRetries}: {request.error}");
                    if (attempt <= _maxRetries)
                    {
                        yield return new WaitForSeconds(Mathf.Pow(2, attempt - 1)); // exponential backoff
                        continue;
                    }
                    onError?.Invoke(new ApiError(0, "Network error: " + request.error));
                    yield break;
                }

                // 401 — clear session and redirect to login
                if (request.responseCode == 401)
                {
                    Debug.LogWarning("[ApiClient] 401 Unauthorized — clearing session.");
                    Authentication.AuthManager.Instance?.HandleUnauthorized();
                    onError?.Invoke(new ApiError(401, "Unauthorized"));
                    yield break;
                }

                // 4xx / 5xx — do not retry, invoke error callback
                if (request.responseCode >= 400)
                {
                    string errorBody = request.downloadHandler?.text ?? "";
                    LogResponse((int)request.responseCode, errorBody, isError: true);
                    var apiErr = ParseError(errorBody, (int)request.responseCode);
                    onError?.Invoke(apiErr);
                    yield break;
                }

                // 2xx success
                string responseText = request.downloadHandler?.text ?? "{}";
                LogResponse((int)request.responseCode, responseText);
                T result = JsonUtility.FromJson<T>(responseText);
                onSuccess?.Invoke(result);
                yield break;
            }
        }

        private UnityWebRequest BuildRequest(string method, string url, string jsonBody)
        {
            UnityWebRequest request;
            if (method == UnityWebRequest.kHttpVerbGET)
            {
                request = UnityWebRequest.Get(url);
            }
            else
            {
                byte[] bodyBytes = jsonBody != null ? Encoding.UTF8.GetBytes(jsonBody) : Array.Empty<byte>();
                request = new UnityWebRequest(url, method);
                request.uploadHandler = new UploadHandlerRaw(bodyBytes);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
            }

            if (!string.IsNullOrEmpty(_bearerToken))
                request.SetRequestHeader("Authorization", "Bearer " + _bearerToken);

            request.SetRequestHeader("Accept", "application/json");
            request.timeout = (int)_timeoutSeconds;
            return request;
        }

        private ApiError ParseError(string body, int statusCode)
        {
            try
            {
                var parsed = JsonUtility.FromJson<ApiErrorResponse>(body);
                return new ApiError(statusCode, parsed?.error ?? parsed?.message ?? $"HTTP {statusCode}");
            }
            catch
            {
                return new ApiError(statusCode, $"HTTP {statusCode}");
            }
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void LogRequest(string method, string url, int attempt)
        {
            Debug.Log($"[ApiClient] {method} {url} (attempt {attempt})");
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void LogResponse(int status, string body, bool isError = false)
        {
            if (isError)
                Debug.LogWarning($"[ApiClient] ← {status} ERROR: {body}");
            else
                Debug.Log($"[ApiClient] ← {status} OK ({body.Length} chars)");
        }

        [Serializable]
        private class ApiErrorResponse { public string error; public string message; }
    }

    // ── Shared error type ─────────────────────────────────────────────────────

    public class ApiError
    {
        public int StatusCode { get; }
        public string Message { get; }
        public ApiError(int code, string message) { StatusCode = code; Message = message; }
        public override string ToString() => $"ApiError({StatusCode}): {Message}";
    }
}

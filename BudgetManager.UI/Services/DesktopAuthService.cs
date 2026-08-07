using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BudgetManager.Domain;
using BudgetManager.Queries.Common;
using Microsoft.Extensions.Options;

namespace BudgetManager.UI.Services
{
    /// <summary>
    /// Thin desktop sign-in. The client only knows the API address; the API owns all identity-provider
    /// configuration and the client secret. Flow:
    ///   1. GET {api}/auth/config to learn the authorize endpoint, client id and scopes.
    ///   2. Open the system browser (OpenID Connect Authorization Code + PKCE) and catch the redirect
    ///      on a loopback listener to obtain the authorization code.
    ///   3. POST the code to {api}/auth/token; the API exchanges it with the provider (holding the
    ///      secret) and returns the tokens. Refresh works the same way.
    ///
    /// Implements <see cref="IApiTokenProvider"/> so the API store's HttpClient attaches the bearer
    /// automatically. The provider's id_token (a JWT the API validates) is used as the bearer.
    /// </summary>
    public sealed class DesktopAuthService : IApiTokenProvider
    {
        private readonly IOptions<Settings> _settings;
        private readonly SemaphoreSlim _gate = new(1, 1);
        // A dedicated client (NOT the API entity-store client, to avoid the bearer handler recursing).
        private static readonly HttpClient Http = new();

        private string? _accessToken;
        private string? _idToken;
        private string? _refreshToken;
        private DateTime _expiresAtUtc = DateTime.MinValue;

        public DesktopAuthService(IOptions<Settings> settings)
        {
            _settings = settings;
            // Restore a persisted refresh token so sign-in survives an app restart (mode switches
            // restart the app). GetAccessTokenAsync will silently exchange it for a fresh token.
            _refreshToken = LoadRefreshToken();
        }

        /// <summary>Raised when sign-in state changes so the UI can refresh (e.g. button label).</summary>
        public event Action? StateChanged;

        /// <summary>True when an API address is set (the only thing the client needs to sign in).</summary>
        public bool IsConfigured => !string.IsNullOrWhiteSpace(_settings.Value.ApiBaseUrl);

        /// <summary>True while a non-expired token is held (or a refresh token can renew one).</summary>
        public bool IsSignedIn =>
            (BearerToken is not null && DateTime.UtcNow < _expiresAtUtc) || _refreshToken is not null;

        // The API validates the provider's id_token (a JWT). Google's access token is opaque, so the
        // id_token is used as the bearer; falls back to access_token for providers that issue a JWT one.
        private string? BearerToken => _idToken ?? _accessToken;

        /// <summary>The signed-in user's email (or name) read from the id_token, if available.</summary>
        public string? SignedInEmail =>
            ReadClaim(_idToken, "email") ?? ReadClaim(_idToken, "name");

        private string ResolveApiBase(string? overrideUrl = null)
        {
            var url = !string.IsNullOrWhiteSpace(overrideUrl) ? overrideUrl : _settings.Value.ApiBaseUrl;
            if (string.IsNullOrWhiteSpace(url))
                throw new InvalidOperationException("An API address is required to sign in.");
            return url.EndsWith('/') ? url : url + "/";
        }

        /// <summary>
        /// Whether the API at the given address (or the configured one) requires sign-in. Used to
        /// gate online mode: the desktop should not go online against a protected server unless the
        /// user is authenticated.
        /// </summary>
        public async Task<bool> ServerRequiresAuthAsync(string? apiBaseUrl = null, CancellationToken cancellationToken = default)
        {
            var config = await Http.GetFromJsonAsync<AuthConfig>(
                ResolveApiBase(apiBaseUrl) + "auth/config", cancellationToken);
            return config?.Enabled == true;
        }

        public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            if (!IsConfigured) return null;

            await _gate.WaitAsync(cancellationToken);
            try
            {
                if (BearerToken is not null && DateTime.UtcNow < _expiresAtUtc)
                    return BearerToken;

                if (_refreshToken is not null)
                {
                    try { await RefreshCoreAsync(cancellationToken); }
                    catch { ClearCore(); }
                }

                return (BearerToken is not null && DateTime.UtcNow < _expiresAtUtc) ? BearerToken : null;
            }
            finally
            {
                _gate.Release();
            }
        }

        /// <summary>
        /// Runs the interactive browser sign-in. Throws on error/cancellation. An explicit
        /// <paramref name="apiBaseUrl"/> lets the caller sign in against a not-yet-active address
        /// (e.g. while still offline, before switching online).
        /// </summary>
        public async Task SignInAsync(string? apiBaseUrl = null, CancellationToken cancellationToken = default)
        {
            string apiBase = ResolveApiBase(apiBaseUrl);

            var config = await Http.GetFromJsonAsync<AuthConfig>(apiBase + "auth/config", cancellationToken);
            if (config is null || !config.Enabled)
                throw new InvalidOperationException("This server does not require sign-in.");
            if (string.IsNullOrWhiteSpace(config.AuthorizationEndpoint) || string.IsNullOrWhiteSpace(config.ClientId))
                throw new InvalidOperationException("The server's sign-in configuration is incomplete.");

            // PKCE pair.
            string verifier = Base64Url(RandomNumberGenerator.GetBytes(32));
            string challenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
            string state = Base64Url(RandomNumberGenerator.GetBytes(16));

            // Loopback redirect on a free port (native clients register http://127.0.0.1 as callback).
            int port = GetFreePort();
            string redirectUri = $"http://127.0.0.1:{port}/";

            string scope = string.IsNullOrWhiteSpace(config.Scopes) ? "openid profile email" : config.Scopes!;
            string url =
                $"{config.AuthorizationEndpoint}?response_type=code" +
                $"&client_id={Uri.EscapeDataString(config.ClientId!)}" +
                $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                $"&scope={Uri.EscapeDataString(scope)}" +
                $"&code_challenge={challenge}&code_challenge_method=S256" +
                $"&state={state}" +
                // Google returns a refresh token only with access_type=offline; prompt=consent
                // ensures one is issued again on subsequent sign-ins.
                "&access_type=offline&prompt=consent";

            using var listener = new HttpListener();
            listener.Prefixes.Add(redirectUri);
            listener.Start();

            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });

            // Wait for the browser redirect (with a generous timeout so a closed tab doesn't hang).
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromMinutes(5));
            var contextTask = listener.GetContextAsync();
            var completed = await Task.WhenAny(contextTask, Task.Delay(Timeout.Infinite, timeout.Token));
            if (completed != contextTask)
                throw new TimeoutException("Sign-in timed out or was cancelled.");

            var context = await contextTask;
            var query = context.Request.QueryString;
            string? code = query["code"];
            string? returnedState = query["state"];
            string? error = query["error"];

            await WriteBrowserResponseAsync(context, error is null && code is not null);
            listener.Stop();

            if (error is not null)
                throw new InvalidOperationException($"Sign-in failed: {error} {query["error_description"]}");
            if (string.IsNullOrEmpty(code) || returnedState != state)
                throw new InvalidOperationException("Sign-in failed: invalid authorization response.");

            await _gate.WaitAsync(cancellationToken);
            try
            {
                // ExchangeAsync raises StateChanged on success.
                await ExchangeAsync(apiBase, new { code, codeVerifier = verifier, redirectUri }, cancellationToken);
            }
            finally
            {
                _gate.Release();
            }
        }

        /// <summary>Clears cached tokens (local sign-out).</summary>
        public void SignOut()
        {
            ClearCore();
            StateChanged?.Invoke();
        }

        // ---- internals ------------------------------------------------------------------------

        private Task RefreshCoreAsync(CancellationToken cancellationToken) =>
            ExchangeAsync(ResolveApiBase(), new { refreshToken = _refreshToken }, cancellationToken);

        private async Task ExchangeAsync(string apiBase, object requestBody, CancellationToken cancellationToken)
        {
            using var response = await Http.PostAsJsonAsync(apiBase + "auth/token", requestBody, cancellationToken);
            string body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Sign-in failed ({(int)response.StatusCode}): {body}");

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            _accessToken = root.TryGetProperty("access_token", out var at) ? at.GetString() : null;
            // The id_token is the JWT the API validates (the access token may be opaque).
            if (root.TryGetProperty("id_token", out var it) && it.ValueKind == JsonValueKind.String)
                _idToken = it.GetString();
            // A refresh response may omit the refresh token; keep the existing one when so.
            if (root.TryGetProperty("refresh_token", out var rt) && rt.ValueKind == JsonValueKind.String)
                _refreshToken = rt.GetString();

            int expiresIn = root.TryGetProperty("expires_in", out var ei) && ei.TryGetInt32(out var v) ? v : 3600;
            // Refresh a minute early to avoid using a token that expires mid-request.
            _expiresAtUtc = DateTime.UtcNow.AddSeconds(Math.Max(30, expiresIn - 60));

            // Persist the refresh token so the session survives an app restart.
            SaveRefreshToken(_refreshToken);

            // Notify the UI (covers both interactive sign-in and silent refresh, e.g. at startup).
            StateChanged?.Invoke();
        }

        /// <summary>Reads a string claim from a JWT's payload without validating the signature.</summary>
        private static string? ReadClaim(string? jwt, string claim)
        {
            if (string.IsNullOrEmpty(jwt)) return null;
            var parts = jwt.Split('.');
            if (parts.Length < 2) return null;
            try
            {
                using var doc = JsonDocument.Parse(Base64UrlDecodeToString(parts[1]));
                return doc.RootElement.TryGetProperty(claim, out var value) && value.ValueKind == JsonValueKind.String
                    ? value.GetString()
                    : null;
            }
            catch
            {
                return null;
            }
        }

        private static string Base64UrlDecodeToString(string segment)
        {
            string s = segment.Replace('-', '+').Replace('_', '/');
            switch (s.Length % 4) { case 2: s += "=="; break; case 3: s += "="; break; }
            return Encoding.UTF8.GetString(Convert.FromBase64String(s));
        }

        private void ClearCore()
        {
            _accessToken = null;
            _idToken = null;
            _refreshToken = null;
            _expiresAtUtc = DateTime.MinValue;
            SaveRefreshToken(null);
        }

        // ---- refresh-token persistence (DPAPI, current user) ----------------------------------

        private string TokenFilePath =>
            Path.Combine(JsonStoreLocation.EnsureDirectory(_settings.Value), "auth.dat");

        private void SaveRefreshToken(string? token)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                {
                    if (File.Exists(TokenFilePath)) File.Delete(TokenFilePath);
                    return;
                }
                byte[] protectedBytes = ProtectedData.Protect(
                    Encoding.UTF8.GetBytes(token), optionalEntropy: null, scope: DataProtectionScope.CurrentUser);
                File.WriteAllBytes(TokenFilePath, protectedBytes);
            }
            catch
            {
                // Persistence is best-effort; a failure just means the user signs in again next time.
            }
        }

        private string? LoadRefreshToken()
        {
            try
            {
                if (!File.Exists(TokenFilePath)) return null;
                byte[] unprotected = ProtectedData.Unprotect(
                    File.ReadAllBytes(TokenFilePath), optionalEntropy: null, scope: DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(unprotected);
            }
            catch
            {
                return null; // Unreadable/foreign token: treat as signed out.
            }
        }

        private static int GetFreePort()
        {
            var l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }

        private static async Task WriteBrowserResponseAsync(HttpListenerContext context, bool success)
        {
            string message = success
                ? "Signed in. You can close this tab and return to BudgetManager."
                : "Sign-in did not complete. You can close this tab and return to BudgetManager.";
            byte[] bytes = Encoding.UTF8.GetBytes(
                $"<html><body style='font-family:Segoe UI,Arial,sans-serif;padding:2rem'>{message}</body></html>");
            context.Response.ContentType = "text/html";
            context.Response.ContentLength64 = bytes.Length;
            await context.Response.OutputStream.WriteAsync(bytes);
            context.Response.Close();
        }

        private static string Base64Url(byte[] bytes) =>
            Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        /// <summary>Sign-in config returned by the API (camelCase JSON).</summary>
        private sealed record AuthConfig(
            bool Enabled, string? AuthorizationEndpoint, string? ClientId, string? Scopes, string? Audience);
    }
}

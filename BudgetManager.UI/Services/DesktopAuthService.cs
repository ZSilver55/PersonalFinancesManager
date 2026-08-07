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

        public DesktopAuthService(IOptions<Settings> settings) => _settings = settings;

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

        private string ApiBase
        {
            get
            {
                var url = _settings.Value.ApiBaseUrl
                          ?? throw new InvalidOperationException("An API address is required to sign in.");
                return url.EndsWith('/') ? url : url + "/";
            }
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

        /// <summary>Runs the interactive browser sign-in. Throws on error/cancellation.</summary>
        public async Task SignInAsync(CancellationToken cancellationToken = default)
        {
            if (!IsConfigured)
                throw new InvalidOperationException("Set the API address in Settings first.");

            var config = await Http.GetFromJsonAsync<AuthConfig>(ApiBase + "auth/config", cancellationToken);
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
                await ExchangeAsync(new { code, codeVerifier = verifier, redirectUri }, cancellationToken);
            }
            finally
            {
                _gate.Release();
            }

            StateChanged?.Invoke();
        }

        /// <summary>Clears cached tokens (local sign-out).</summary>
        public void SignOut()
        {
            ClearCore();
            StateChanged?.Invoke();
        }

        // ---- internals ------------------------------------------------------------------------

        private Task RefreshCoreAsync(CancellationToken cancellationToken) =>
            ExchangeAsync(new { refreshToken = _refreshToken }, cancellationToken);

        private async Task ExchangeAsync(object requestBody, CancellationToken cancellationToken)
        {
            using var response = await Http.PostAsJsonAsync(ApiBase + "auth/token", requestBody, cancellationToken);
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
        }

        private void ClearCore()
        {
            _accessToken = null;
            _idToken = null;
            _refreshToken = null;
            _expiresAtUtc = DateTime.MinValue;
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

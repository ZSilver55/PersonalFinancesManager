using System.Text.Json;

namespace BudgetManager.Api.Auth
{
    /// <summary>
    /// Server-side owner of the identity-provider configuration and secrets. The desktop client
    /// stays "thin": it only knows the API address, asks the API what to show the user (authorize
    /// endpoint, client id, scopes), and lets the API perform the code/refresh token exchange so
    /// the client secret never leaves the server.
    ///
    /// Reads the "Auth" configuration section: Enabled, Authority, ClientId, ClientSecret, Audience,
    /// Scopes. Currently targets Google (Authority https://accounts.google.com).
    /// </summary>
    public sealed class AuthProviderService
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;

        private string? _authorizationEndpoint;
        private string? _tokenEndpoint;

        public AuthProviderService(HttpClient http, IConfiguration config)
        {
            _http = http;
            _config = config;
        }

        public bool Enabled => _config.GetValue<bool>("Auth:Enabled");
        private string? Authority => _config["Auth:Authority"];
        private string? ClientId => _config["Auth:ClientId"];
        private string? ClientSecret => _config["Auth:ClientSecret"];
        private string? Audience => _config["Auth:Audience"];

        /// <summary>Full scope string sent to the provider (defaults plus any configured extras).</summary>
        public string Scopes
        {
            get
            {
                var scopes = new List<string> { "openid", "profile", "email" };
                foreach (var part in (_config["Auth:Scopes"] ?? "")
                         .Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    if (!scopes.Contains(part)) scopes.Add(part);
                return string.Join(' ', scopes);
            }
        }

        /// <summary>Config the desktop needs to start the browser sign-in (no secret is exposed).</summary>
        public async Task<AuthConfigResponse> GetConfigAsync(CancellationToken cancellationToken = default)
        {
            if (!Enabled || string.IsNullOrWhiteSpace(Authority) || string.IsNullOrWhiteSpace(ClientId))
                return new AuthConfigResponse { Enabled = false };

            await EnsureDiscoveryAsync(cancellationToken);
            return new AuthConfigResponse
            {
                Enabled = true,
                AuthorizationEndpoint = _authorizationEndpoint,
                ClientId = ClientId,
                Scopes = Scopes,
                Audience = Audience
            };
        }

        /// <summary>Exchanges an authorization code for tokens; returns the raw provider JSON.</summary>
        public Task<(int status, string json)> ExchangeCodeAsync(
            string code, string codeVerifier, string redirectUri, CancellationToken cancellationToken = default)
        {
            var fields = new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["client_id"] = ClientId ?? "",
                ["code"] = code,
                ["redirect_uri"] = redirectUri,
                ["code_verifier"] = codeVerifier
            };
            if (!string.IsNullOrWhiteSpace(ClientSecret)) fields["client_secret"] = ClientSecret!;
            return PostTokenAsync(fields, cancellationToken);
        }

        /// <summary>Refreshes tokens using a refresh token; returns the raw provider JSON.</summary>
        public Task<(int status, string json)> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            var fields = new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = ClientId ?? "",
                ["refresh_token"] = refreshToken
            };
            if (!string.IsNullOrWhiteSpace(ClientSecret)) fields["client_secret"] = ClientSecret!;
            return PostTokenAsync(fields, cancellationToken);
        }

        private async Task<(int status, string json)> PostTokenAsync(
            Dictionary<string, string> fields, CancellationToken cancellationToken)
        {
            await EnsureDiscoveryAsync(cancellationToken);
            using var response = await _http.PostAsync(_tokenEndpoint, new FormUrlEncodedContent(fields), cancellationToken);
            string body = await response.Content.ReadAsStringAsync(cancellationToken);
            return ((int)response.StatusCode, body);
        }

        private async Task EnsureDiscoveryAsync(CancellationToken cancellationToken)
        {
            if (_authorizationEndpoint is not null && _tokenEndpoint is not null) return;
            if (string.IsNullOrWhiteSpace(Authority))
                throw new InvalidOperationException("Auth:Authority is not configured.");

            string url = Authority!.TrimEnd('/') + "/.well-known/openid-configuration";
            string json = await _http.GetStringAsync(url, cancellationToken);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            _authorizationEndpoint = root.GetProperty("authorization_endpoint").GetString();
            _tokenEndpoint = root.GetProperty("token_endpoint").GetString();
        }
    }

    /// <summary>What the desktop needs to begin sign-in. Deliberately excludes the client secret.</summary>
    public sealed class AuthConfigResponse
    {
        public bool Enabled { get; set; }
        public string? AuthorizationEndpoint { get; set; }
        public string? ClientId { get; set; }
        public string? Scopes { get; set; }
        public string? Audience { get; set; }
    }

    /// <summary>Token request from the desktop: either an auth code (+verifier+redirect) or a refresh token.</summary>
    public sealed class TokenExchangeRequest
    {
        public string? Code { get; set; }
        public string? CodeVerifier { get; set; }
        public string? RedirectUri { get; set; }
        public string? RefreshToken { get; set; }
    }
}
